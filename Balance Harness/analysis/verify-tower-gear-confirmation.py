"""Independently audit the sealed floor-13 gear confirmation without combat."""
import argparse
import gzip
import hashlib
import importlib.util
import json
import math
from pathlib import Path

spec = importlib.util.spec_from_file_location('gear_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
read, sha, require, member = io.read, io.sha, io.require, io.member
PROFILES = ['resistance-and-health', 'baseline', 'armor-and-health']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--owner', type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    owner, receipt = args.owner.resolve(), args.receipt.resolve()
    q = read(owner / 'request.json')
    study, source, history_source = [Path(q[k]).resolve() for k in ('output', 'source', 'historySource')]
    require(not receipt.is_relative_to(study) and not receipt.exists(), 'Choose a new receipt outside the sealed study')
    require(q['version'] == 'tower-floor13-gear-confirmation-v1', 'Unexpected confirmation version')
    require(sha(study / 'files.json') == args.manifest_pin, 'Changed archive manifest')
    manifest = read(study / 'files.json')
    require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(manifest) | {'files.json'}, 'Changed file inventory')
    for name, digest in manifest.items():
        require(sha(member(study, name)) == digest, 'Changed archive member: ' + name)
    declaration, completion = read(owner / 'declaration.json'), read(owner / 'completion.json')
    require(declaration['profiles'] == PROFILES and declaration['samples'] == 512 and declaration['maximumFights'] == 1536, 'Wrong frozen design')
    require(sha(owner / 'request.json') == declaration['requestSha256'] and read(study / 'request.json') == q, 'Changed request')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Wrong completion binding')
    process = read(owner / 'process.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Owner did not complete and drain')
    finished = read(study / 'completion.json')
    require(finished['status'] == 'Complete' and finished['attempts'] == finished['completed'] == 1536 and finished['retries'] == 0, 'Wrong fight accounting')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 1537)), 'Attempt journal differs')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', teams=3, historicalInputsMatched=96, historyCount=833546), 'Missing source input parity')
    require(sha(source / 'files.json') == q['sourceManifestHash'] and sha(history_source / 'files.json') == q['historyManifestHash'], 'Changed source pins')
    for root, names in [(source, ('scope.json', 'cells.json')), (history_source, ('seed-ledger.json',))]:
        saved = read(root / 'files.json')
        for name in names:
            require(sha(root / name) == saved[name], 'Changed source member')
    history_files = read(study / 'history-files.json')
    require(all(history_files.get(name) == digest for name, digest in q['requiredHistory'].items()), 'Lost required history pins')
    for name, digest in history_files.items():
        require(sha(Path(name)) == digest, 'Historical reservations changed')
    for name, digest in q['recoveryHashes'].items():
        require(sha(Path(name)) == digest, 'Recovery receipt changed')
    prior = read(history_source / 'seed-ledger.json')
    history = set(prior['historical'] + prior['reserved'])
    require(len(history) == 833546, 'Wrong starting exclusion union')
    used, panel, rejections = set(history), [], 0
    for block in range(1, 3):
        path = study / f'allocation-{block}'
        seeds = read(path / 'seeds.json')
        require(seeds['historical'] == sorted(used), 'Wrong block exclusions')
        allocator = read(path / 'reservation-intent.json')['allocator']
        require(allocator == dict(algorithm='sha256-us-int32le-reject-v1', domain=q['version'] + f'/block-{block}',
                                 master=q['master'], firstCount=1, secondCount=255, maximumCandidatesPerStage=100000), 'Wrong allocator')
        journal = iter(json.loads(line) for line in (path / 'allocation-journal.jsonl').read_text().splitlines())
        candidates = rejected = 0
        for stage, count in [('first', 1), ('second', 255)]:
            values, ordinal = [], 0
            while len(values) < count:
                require(ordinal < 100000, 'Unbounded allocation')
                require(next(journal) == dict(kind='Start', stage=stage, ordinal=ordinal, value=None, accepted=None), 'Wrong allocation start')
                digest = hashlib.sha256('\x1f'.join(map(str, [allocator['domain'], q['master'], stage, ordinal])).encode()).digest()
                candidate = int.from_bytes(digest[:4], 'little', signed=True)
                accepted = candidate not in used
                require(next(journal) == dict(kind='Candidate', stage=stage, ordinal=ordinal, value=candidate, accepted=accepted), 'Wrong derivation/rejection')
                candidates += 1
                if accepted:
                    used.add(candidate)
                    values.append(candidate)
                else:
                    rejected += 1
                ordinal += 1
            require(values == seeds[stage], 'Wrong accepted panel')
            panel.extend(values)
        require(next(journal, None) is None, 'Extended allocation journal')
        reservation = read(path / 'reservation.json')
        require((reservation['candidates'], reservation['rejections']) == (candidates, rejected), 'Wrong reservation counts')
        require(read(path / 'seed-ledger.json') == dict(reservationState='Complete', historical=seeds['historical'], first=seeds['first'], second=seeds['second']), 'Wrong block ledger')
        require(read(path / 'history-input.json') == dict(reservationState='Complete', reserved=seeds['first'] + seeds['second']), 'Incomplete block')
        rejections += rejected
    require(len(panel) == len(set(panel)) == 512 and not history.intersection(panel), 'Panel is not fresh')
    require(read(study / 'confirmation-seeds.json') == panel, 'Changed combat panel')
    require(read(study / 'seed-ledger.json') == dict(reservationState='Complete', historical=sorted(history), reserved=panel), 'Wrong final ledger')
    require(read(study / 'history-input.json') == dict(reservationState='Complete', reserved=panel), 'Incomplete final reservation')
    scope = read(study / 'scope.json')
    require(scope == {**read(source / 'scope.json'), 'algorithm': q['version']}, 'Captured scope changed')
    for name, digest in scope['execution']['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Captured executable differs')
    for name, digest in scope['contentHashes'].items():
        require(sha(study / 'content/Data' / name) == sha(study / 'study/content/Data' / name) == digest, 'Captured content differs')
    teams, cells = read(study / 'teams.json'), read(source / 'cells.json')
    require([t['profile'] for t in teams] == PROFILES, 'Wrong profile order')
    for team in teams:
        old = next(c for c in cells if c['floor'] == 13 and c['profile'] == team['profile'])
        require(team == {**old, 'scenario': {**old['scenario'], 'seeds': []}}, 'Frozen loadout changed')
    trials = [json.loads(line) for line in (study / 'study/trials.jsonl').read_text().splitlines()]
    require(len(trials) == 1536 and len({t['id'] for t in trials}) == 1536, 'Wrong trial count')
    observations = {profile: [] for profile in PROFILES}
    for ordinal, trial in enumerate(trials):
        team, seed = teams[ordinal // 512], panel[ordinal % 512]
        require(trial['id'] == f'trial-{ordinal + 1:06}' and trial['stage'] == team['profile'] and trial['seed'] == seed, 'Trial schedule differs')
        recipe = read(study / 'study/recipes' / (trial['recipe'] + '.json'))
        require(recipe == {**team['scenario'], 'seeds': panel}, 'Trial recipe differs')
        report = json.loads(gzip.decompress((study / 'study/battles' / (trial['id'] + '.json.gz')).read_bytes()))
        require(report['battle']['seed'] == seed and report['battle']['scenarioId'] == recipe['id'], 'Report binding differs')
        require(report['succeeded'] == (report['battle']['summary']['contentOutcome'] == 'Victory'), 'Report outcome differs')
        observations[team['profile']].append(report)
    result = read(study / 'result.json')
    require(result['status'] == 'Complete' and result['fights'] == 1536 and result['freshValues'] == 512
            and result['candidate'] == PROFILES[0] and result['retries'] == 0, 'Wrong result accounting')
    require([row['id'] for row in result['rows']] == PROFILES, 'Result rows differ')
    for row in result['rows']:
        reports = observations[row['id']]
        require(row['samples'] == 512 and row['wins'] == sum(r['succeeded'] for r in reports), 'Win count differs')
        require(math.isclose(row['meanGuardianHealth'], sum(r['guardianHealthRemainingPercent'] for r in reports)/512, abs_tol=1e-6), 'Health average differs')
    passes = []
    for contrast, reference in zip(result['contrasts'], PROFILES[1:], strict=True):
        pairs = list(zip(observations[PROFILES[0]], observations[reference], strict=True))
        gains = sum(c['succeeded'] and not r['succeeded'] for c, r in pairs)
        losses = sum(not c['succeeded'] and r['succeeded'] for c, r in pairs)
        numerator = sum(math.comb(gains+losses, k) for k in range(gains, gains+losses+1))
        denominator = 2**(gains+losses)
        require(contrast['referenceId'] == reference and (contrast['gainedWins'], contrast['lostWins']) == (gains, losses), 'Paired contrast differs')
        require((int(contrast['tailNumerator']), int(contrast['tailDenominator'])) == (numerator, denominator), 'Exact tail differs')
        require(math.isclose(contrast['oneSidedPValue'], numerator/denominator, rel_tol=1e-12) and contrast['observedGain'] == (gains-losses)/512, 'Reported statistic differs')
        passes.append(gains-losses >= 26 and 40*numerator <= denominator)
        require(contrast['qualifies'] == passes[-1], 'Contrast decision differs')
    require(result['decision'] == ('GearProfileImprovementConfirmed' if all(passes) else 'GearProfileImprovementNotDemonstrated'), 'Final decision differs')
    audit = dict(status='Verified', fights=1536, authenticatedFiles=len(manifest),
                 studyBytes=sum(p.stat().st_size for p in study.rglob('*') if p.is_file()), freshValues=512,
                 historicalExclusions=len(history), totalExclusions=len(used), allocationRejections=rejections,
                 decision=result['decision'], sourceRecipesUnchanged=True, sourceExecutionMatched=True,
                 historicalInputsMatched=96, newFights=0, resultSha256=sha(study / 'result.json'), archiveManifestSha256=args.manifest_pin)
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
