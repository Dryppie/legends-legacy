"""Read-only audit of the calibrated floor-13 search and all held-out teams."""
import argparse
import gzip
import hashlib
import importlib.util
import json
import math
from pathlib import Path


def helpers():
    spec = importlib.util.spec_from_file_location('floor13_audit_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


io = helpers()
require, read, sha = io.require, io.read, io.sha


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--owner', type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    owner, receipt = args.owner.resolve(), args.receipt.resolve()
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    study = Path(q['output']).resolve()
    require(not receipt.exists() and not receipt.is_relative_to(study), 'New receipt outside the sealed study required')
    require(sha(study / 'files.json') == args.manifest_pin, 'Changed archive manifest')
    files = read(study / 'files.json')
    require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Changed inventory')
    for name, digest in files.items():
        require(sha(io.member(study, name)) == digest, 'Changed archive file: ' + name)
    require(sha(owner / 'request.json') == declaration['requestSha256'] and read(study / 'request.json') == q, 'Request mismatch')
    require(q['floor'] == 13 and q['version'] == 'affinity-floor13-baseline-evaluation-v1', 'Wrong experiment')
    require(declaration['maximumFights'] == 1168 and declaration['freshValues'] == 237, 'Wrong declaration')
    require(read(study / 'completion.json')['attempts'] == read(study / 'completion.json')['completed'] == 1168, 'Incomplete run')
    require([json.loads(l)['attempt'] for l in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 1169)), 'Wrong attempt journal')
    process = read(owner / 'process.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Owner not drained')
    complete = read(owner / 'completion.json')
    require(complete['archiveManifestSha256'] == args.manifest_pin and complete['resultSha256'] == sha(study / 'result.json'), 'Wrong closeout')
    require(sha(Path(q['plan'])) == q['planHash'] and sha(Path(q['handoff'])) == q['handoffHash']
            and sha(Path(q['historyLedger'])) == q['historyLedgerHash'] and sha(Path(q['screen'])) == q['screenHash'], 'Changed pinned input')
    screen_root = Path(q['screen']).parent
    require(sha(screen_root / 'files.json') == declaration['screenManifestSha256'] == 'eaeeeabeb78af89fb242419179c2dd17c147634c36f10e119babae4adef15abc', 'Wrong reference screen')
    screen = read(Path(q['screen']))
    require(screen['eligible'] and screen['benchmarkReference'] == q['benchmarkReference'] == 1
            and max(r['wins'] for r in screen['rows']) <= 28, 'Reference gate failed')
    history_json = read(Path(q['historyLedger']))
    history = set(history_json['historical'] + history_json['reserved'])
    require(len(history) == 834058, 'Wrong history union')
    seeds = read(study / 'seeds.json')
    values = seeds['first'] + seeds['second']
    require(seeds['historical'] == sorted(history) and len(values) == len(set(values)) == 237 and not history.intersection(values), 'Invalid fresh reservation')
    require(read(study / 'seed-ledger.json') == dict(reservationState='Complete', historical=sorted(history), first=seeds['first'], second=seeds['second']), 'Wrong final ledger')
    require(read(study / 'history-input.json') == dict(reservationState='Complete', reserved=values), 'Incomplete registry reservation')
    allocator = read(study / 'reservation-intent.json')['allocator']
    require(allocator['domain'] == q['version'] and allocator['master'] == q['master'] == 2026092813, 'Allocator changed')
    journal = iter(json.loads(l) for l in (study / 'allocation-journal.jsonl').read_text().splitlines())
    used, candidates, rejected = set(history), 0, 0
    for stage in ('first', 'second'):
        accepted_values, ordinal = [], 0
        while len(accepted_values) < len(seeds[stage]):
            require(ordinal < allocator['maximumCandidatesPerStage'], 'Allocator limit exceeded')
            require(next(journal) == dict(kind='Start', stage=stage, ordinal=ordinal, value=None, accepted=None), 'Allocation start differs')
            digest = hashlib.sha256('\x1f'.join(map(str, [allocator['domain'], allocator['master'], stage, ordinal])).encode()).digest()
            value = int.from_bytes(digest[:4], 'little', signed=True)
            accepted = value not in used
            require(next(journal) == dict(kind='Candidate', stage=stage, ordinal=ordinal, value=value, accepted=accepted), 'Allocation derivation differs')
            candidates += 1
            if accepted:
                used.add(value); accepted_values.append(value)
            else:
                rejected += 1
            ordinal += 1
        require(accepted_values == seeds[stage], 'Accepted allocation changed')
    require(next(journal, None) is None, 'Extra allocation records')
    reservation = read(study / 'reservation.json')
    require((reservation['candidates'], reservation['rejections']) == (candidates, rejected), 'Allocation accounting differs')
    plan, source_plan = read(study / 'plan.json'), read(Path(q['plan']))
    require(all(plan[k] == source_plan[k] for k in ('policy', 'version', 'selectionPolicyVersion')), 'Supported policy changed')
    scope = plan['racing']['scope']
    require(scope['budget'] == declaration['budget'] and scope['requiredPartySize'] == 10, 'Budget changed')
    require(plan['racing']['benchmarkReferenceId'] == scope['stages']['selectionPrimaryReferenceId'] == 'floor13-reference-1', 'Benchmark changed')
    require(plan['racing']['rootSeed'] == values[0], 'Wrong generation seed')
    panels = plan['racing']['panels']
    search_seeds = [s for p in panels for s in p['seeds']]
    require(search_seeds == values[1:109] and [len(p['seeds']) for p in panels] == [8, 8, 8, 8, 16, 60], 'Search schedule changed')
    heldout = read(study / 'heldout-seeds.json')
    require(heldout == values[109:] and len(heldout) == 128 and not set(heldout).intersection(search_seeds), 'Held-out schedule changed')
    require(set(heldout).issubset(scope['excludedCombatSeeds']), 'Held-out seeds leaked into proposal schedules')
    captured = read(study / 'captured-scope.json')
    require(captured['settings'] == read(screen_root / 'scope.json')['settings']
            and captured['contentHashes'] == read(screen_root / 'scope.json')['contentHashes']
            and captured['execution'] == screen['execution'], 'Captured context drift')
    for name, digest in captured['contentHashes'].items():
        require(sha(study / 'content/Data' / name) == digest, 'Wrong content')
    for name, digest in captured['execution']['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong runtime')
    projected = [c for c in read(screen_root / 'cells.json') if c['form'] == 'projected']
    for reference, original in zip(scope['references'], projected, strict=True):
        require(reference['scenario']['party'] == original['scenario']['party'], 'Prepared reference changed')
    freeze = read(study / 'heldout-freeze.json')
    reference_ids = {s['party']['id'] for s in scope['starts']}
    require(len(freeze) == 5 and len(reference_ids) == 3, 'Wrong finalist family')
    observations = {f['party']['id']: {} for f in freeze}
    for f in freeze:
        require(f['role'] == ('existing-reference' if f['party']['id'] in reference_ids else 'generated-finalist'), 'Wrong candidate provenance')
        require(len(f['scenario']['party']) == 10 and f['scenario']['seeds'] == heldout, 'Wrong held-out party')
        for member, baseline in zip(f['scenario']['party'], scope['contexts'][0]['characterTemplates'], strict=True):
            require(len(member['build']['essenceIds']) == 7 and member['build']['equipment'] == baseline['build']['equipment'], 'Gear or Essence budget changed')
    search_reports = []
    for folder, count in (('search', 528), ('heldout', 640)):
        trials = [json.loads(l) for l in (study / folder / 'trials.jsonl').read_text().splitlines()]
        require(len(trials) == len({t['id'] for t in trials}) == count, 'Wrong trial count')
        for n, t in enumerate(trials):
            raw = json.loads(gzip.decompress((study / folder / 'battles' / (t['id'] + '.json.gz')).read_bytes()))
            scenario = read(io.member(study / folder / 'recipes', t['recipe'] + '.json'))
            require(raw['battle']['seed'] == t['seed'] and raw['battle']['scenarioId'] == scenario['id'] and scenario['floorNumber'] == 13, 'Report binding mismatch')
            require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome mismatch')
            if folder == 'search':
                require(t['seed'] in search_seeds, 'Unexpected search seed')
                search_reports.append(raw)
            else:
                require(t['stage'] == freeze[n // 128]['party']['id'] and t['seed'] == heldout[n % 128]
                        and scenario == freeze[n // 128]['scenario'], 'Changed held-out schedule')
                observations[t['stage']][t['seed']] = raw
    result, summary = read(study / 'result.json'), read(study / 'search-summary.json')
    require(result['status'] == 'Complete' and result['fights'] == 1168 and result['summary'] == summary, 'Wrong result')
    validation = search_reports[-120:]
    gains = sum(a['succeeded'] and not b['succeeded'] for a, b in zip(validation[:60], validation[60:], strict=True))
    losses = sum(not a['succeeded'] and b['succeeded'] for a, b in zip(validation[:60], validation[60:], strict=True))
    tail = sum(math.comb(gains+losses, k) for k in range(gains, gains+losses+1))
    passed = gains > losses and 20*tail <= 2**(gains+losses)
    require((summary['gainedWins'], summary['lostWins']) == (gains, losses), 'Validation pairing differs')
    require(summary['status'] == ('ChallengerNeedsConfirmation' if passed else 'BenchmarkRetained')
            and summary['needsIndependentConfirmation'] == passed
            and summary['selectedId'] == (summary['challengerId'] if passed else summary['benchmarkId']), 'Validation gate differs')
    wins = lambda key: sum(r['succeeded'] for r in observations[key].values())
    health = lambda key: sum(r['guardianHealthRemainingPercent'] for r in observations[key].values())/128
    strongest = sorted(reference_ids, key=lambda key: (-wins(key), health(key), key))[0]
    require(result['strongestMeasuredReferenceId'] == strongest, 'Strongest reference differs')
    def paired(key, other):
        return (sum(observations[key][s]['succeeded'] and not observations[other][s]['succeeded'] for s in heldout),
                sum(not observations[key][s]['succeeded'] and observations[other][s]['succeeded'] for s in heldout))
    for row in result['rows']:
        key = row['id']
        require(len(observations[key]) == row['samples'] == 128 and row['wins'] == wins(key), 'Held-out wins differ')
        require(math.isclose(row['meanGuardianHealth'], health(key), abs_tol=1e-6), 'Health mean differs')
        require((row['gainedWins'], row['lostWins']) == paired(key, summary['benchmarkId']), 'Benchmark contrast differs')
        require((row['gainedWinsAgainstStrongestReference'], row['lostWinsAgainstStrongestReference']) == paired(key, strongest), 'Strongest-reference contrast differs')
    io.write(receipt, dict(status='Verified', fights=1168, authenticatedFiles=len(files), freshValues=237,
                          historicalExclusions=len(history), totalExclusions=len(used), allocationRejections=rejected,
                          validationGainedWins=gains, validationLostWins=losses, validationPassed=passed,
                          policyUnchanged=True, calibratedContextMatched=True, newFights=0,
                          archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json')))
    print(json.dumps(read(receipt), indent=2))


if __name__ == '__main__':
    main()
