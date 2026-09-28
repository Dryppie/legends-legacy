"""Independently reconstruct current-runtime parity and complete carried-equipment fresh confirmation."""
import argparse
import gzip
import hashlib
import importlib.util
import json
import math
from pathlib import Path
from statistics import NormalDist

spec = importlib.util.spec_from_file_location('floor11_family_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
require, sha, read, member = io.require, io.sha, io.read, io.member
VERSION = 'tower-floor11-carried-confirmation-v1'


def interval(wins):
    n, family = 256, 344
    z = NormalDist().inv_cdf(1 - .025 / family)
    p = wins / n
    denominator = 1 + z * z / n
    center = (p + z * z / (2 * n)) / denominator
    width = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / denominator
    return dict(rate=p, lower=max(0, center-width), upper=min(1, center+width), confidence=1-.05/family)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('owner', 'receipt'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args = parser.parse_args()
    owner, receipt = args.owner.resolve(), args.receipt.resolve()
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    study, source, history_source = [Path(q[k]).resolve() for k in ('output', 'source', 'historySource')]
    require(not receipt.exists() and not receipt.is_relative_to(study), 'New receipt outside sealed study required')
    require(q['version'] == declaration['version'] == VERSION and read(study / 'request.json') == q
            and sha(owner / 'request.json') == declaration['requestSha256'], 'Changed request')
    require(q['master'] == 2026092817, 'Changed declared confirmation master')
    require(sha(study / 'files.json') == args.manifest_pin, 'Changed manifest')
    files = read(study / 'files.json')
    require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Archive inventory differs')
    for name, digest in files.items():
        require(sha(member(study, name)) == digest, 'Changed archive member: ' + name)
    for name, digest in {**q['inputHashes'], **q['recoveryHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed frozen input: ' + name)
    require(all(declaration[k] == v for k, v in dict(cells=344, intendedCells=312, controlCells=32, samples=256,
                freshSeeds=256, maximumFights=99072, qualificationFights=11008, confirmationFights=88064,
                maximumSeconds=4260, maximumBytes=4*1073741824,
                retries=0, intervalPolicy='bonferroni-wilson-95-v1', intervalFamilySize=344).items()), 'Changed envelope')
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0 and process['seconds'] <= 4260, 'Owner did not drain')
    require(completion['status'] == 'Complete' and completion['archiveManifestSha256'] == args.manifest_pin
            and completion['resultSha256'] == sha(study / 'result.json') and completion['fights'] == 99072 and completion['confirmationFights'] == 88064
            and completion['runtimeParityReports'] == 11008
            and completion['freshSeeds'] == 256 and completion['retries'] == 0, 'Completion differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=99072, completed=99072, retries=0), 'Incomplete combat')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 99073)), 'Attempt journal differs')
    protocol = read(study / 'protocol.json')
    require(all(protocol[k] == v for k, v in dict(version=VERSION, cells=344, samples=256, maximumFights=99072, qualificationFights=11008, confirmationFights=88064,
                maximumSeconds=4200, maximumBytes=4*1073741824, retries=0, freshSeeds=256).items()), 'Native protocol differs')
    require(sha(source / 'files.json') == q['sourcePin']
            and sha(history_source / 'files.json') == q['historyPin']
            == '95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944', 'Changed source')
    source_files = read(source / 'files.json')

    def source_json(name):
        require(sha(member(source, name)) == source_files[name], 'Changed source member: ' + name)
        return read(source / name)

    source_result = source_json('result.json')
    grid = [2, 2.25, 2.5, 2.75, 3, 3.5]
    require(source_result['status'] == 'RefinedCarriedCalibrationComplete' and source_result['fights'] == 66048
            and [s['multiplier'] for s in source_result['summaries']] == grid, 'Wrong calibration')
    choices = [s for s in source_result['summaries'] if s['eligible']]
    require(choices and choices[0] == source_result['selected'], 'Lowest eligible candidate required')
    selected = choices[0]
    require(selected['variant'] == grid.index(selected['multiplier'])
            and selected['health'] == round(6.525 * selected['multiplier'], 6)
            and selected['offense'] == round(8.37 * selected['multiplier'], 6)
            and selected == declaration['selected'] == read(study / 'selected.json'), 'Changed selected setting')
    variant = f"variant-{selected['variant']:02}"
    scope = read(study / 'scope.json')
    prior_scope = source_json(variant + '/scope.json')
    require({**scope, 'algorithm': prior_scope['algorithm'], 'execution': prior_scope['execution']} == prior_scope,
            'Changed settings/content scope')
    require(scope['algorithm'] == VERSION and scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Wrong current runtime')
    require(sha(Path(q['runtime']).parents[1] / 'EssenceSystem.Tests/release/EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Changed test assembly')
    for name, digest in scope['execution']['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong runtime')
    for name, digest in read(study / 'runtime-files.json').items():
        require(sha(member(study / 'executable', name)) == digest, 'Runtime inventory differs')
    for name, digest in scope['contentHashes'].items():
        require(sha(study / 'content/Data' / name) == sha(study / 'study/content/Data' / name) == digest, 'Changed selected content')
    require(read(study / 'study/scope.json') == scope, 'Nested archive scope differs')
    nested = read(study / 'study/files.json')
    require({p.relative_to(study / 'study').as_posix() for p in (study / 'study').rglob('*') if p.is_file()} == set(nested) | {'files.json'}, 'Nested inventory differs')
    require(all(files['study/' + name] == digest for name, digest in nested.items()), 'Nested manifest differs')
    prior_files = read(history_source / 'files.json')
    require(sha(history_source / 'seed-ledger.json') == prior_files['seed-ledger.json'], 'Changed historical ledger')
    prior_result = read(history_source / 'result.json')
    require(sha(history_source / 'result.json') == prior_files['result.json']
            and prior_result == read(study / 'prior-result.json')
            and prior_result['status'] == 'Floor10FamilyConfirmationComplete'
            and prior_result['assessment'] == declaration['historyAssessment'] == 'Pass'
            and prior_result['fights'] == 5376 and prior_result['exclusionUnion'] == 835063
            and declaration['historyManifest'] == q['historyPin'], 'Seed history source lost or changed')
    require(sha(history_source / 'selected.json') == prior_files['selected.json']
            and read(history_source / 'selected.json')['multiplier'] == 7.75, 'Wrong seed history setting')
    prior = read(history_source / 'seed-ledger.json')
    history = set(prior['historical'] + prior['first'] + prior['second'])
    require(len(history) == 835063, 'Wrong exclusion union')
    history_files = read(study / 'history-files.json')
    require(all(history_files.get(n) == h for n, h in q['requiredHistory'].items()), 'Lost required history')
    for name, digest in history_files.items():
        require(sha(Path(name)) == digest, 'Changed historical reservation')
    seeds = read(study / 'seeds.json')
    require(seeds['historical'] == sorted(history), 'Wrong allocation exclusions')
    allocator = read(study / 'reservation-intent.json')['allocator']
    require(allocator == dict(algorithm='sha256-us-int32le-reject-v1', domain=VERSION + '/block-1', master=q['master'],
                             firstCount=1, secondCount=255, maximumCandidatesPerStage=100000), 'Wrong allocator')
    used, panel, candidates, rejections = set(history), [], 0, 0
    journal = iter(json.loads(line) for line in (study / 'allocation-journal.jsonl').read_text().splitlines())
    for stage, count in [('first', 1), ('second', 255)]:
        values, ordinal = [], 0
        while len(values) < count:
            require(ordinal < 100000, 'Allocation exceeded limit')
            require(next(journal) == dict(kind='Start', stage=stage, ordinal=ordinal, value=None, accepted=None), 'Unmatched allocation start')
            digest = hashlib.sha256('\x1f'.join(map(str, [allocator['domain'], q['master'], stage, ordinal])).encode()).digest()
            value = int.from_bytes(digest[:4], 'little', signed=True)
            accepted = value not in used
            require(next(journal) == dict(kind='Candidate', stage=stage, ordinal=ordinal, value=value, accepted=accepted), 'Derivation/rejection differs')
            candidates += 1
            if accepted:
                used.add(value); values.append(value)
            else:
                rejections += 1
            ordinal += 1
        require(values == seeds[stage], 'Accepted schedule differs')
        panel.extend(values)
    require(next(journal, None) is None, 'Extra allocation events')
    reservation = read(study / 'reservation.json')
    require(reservation['status'] == 'Complete' and (reservation['candidates'], reservation['rejections']) == (candidates, rejections), 'Reservation accounting differs')
    require(len(panel) == len(set(panel)) == 256 and not history.intersection(panel), 'Panel not fresh')
    require(read(study / 'confirmation-seeds.json') == panel
            and read(study / 'history-input.json') == dict(reservationState='Complete', reserved=panel)
            and read(study / 'seed-ledger.json') == dict(reservationState='Complete', historical=sorted(history), first=seeds['first'], second=seeds['second']), 'Incomplete durable reservation')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', cells=344, historyCount=len(history), currentRuntimeRequiresFullReportQualification=True), 'Preflight differs')
    cells = read(study / 'cells.json')
    expected_cells = [{**c, 'scenario': {**c['scenario'], 'seeds': []}} for c in source_json('cells.json')]
    require(cells == expected_cells and len(cells) == len({c['id'] for c in cells}) == 344, 'Changed complete recipe family')
    require([sum(c['kind'] == k for c in cells) for k in ('retained-control', 'level-control', 'seventh-addition')] == [112, 4, 228], 'Dropped source family')
    require(set(source_json('cells.json')[0]['scenario']['seeds']).issubset(history), 'Historical screen seeds missing from exclusions')
    original_cells = source_json('cells.json')
    require(read(study / 'qualification-cells.json') == original_cells, 'Changed historical qualification recipes')
    require(all(set(c['scenario']['seeds']).issubset(history) for c in original_cells), 'Historical seeds missing from exclusions')
    historical_panel = original_cells[0]['scenario']['seeds'][:32]
    require(len(historical_panel) == len(set(historical_panel)) == 32
            and all(c['scenario']['seeds'][:32] == historical_panel for c in original_cells), 'Changed historical replay panel')
    qualification = study / 'qualification'
    require(read(qualification / 'scope.json') == scope, 'Qualification scope differs')
    qualification_files = read(qualification / 'files.json')
    require({p.relative_to(qualification).as_posix() for p in qualification.rglob('*') if p.is_file()}
            == set(qualification_files) | {'files.json'}, 'Qualification inventory differs')
    require(all(files['qualification/' + n] == h for n, h in qualification_files.items()), 'Qualification manifest differs')
    for name, digest in scope['contentHashes'].items():
        require(sha(qualification / 'content/Data' / name) == digest, 'Qualification content differs')
    old_ledger = variant + '/study/trials.jsonl'
    require(sha(source / old_ledger) == source_files[old_ledger], 'Changed baseline ledger')
    old_trials = [json.loads(line) for line in (source / old_ledger).read_text().splitlines()]
    replays = [json.loads(line) for line in (qualification / 'trials.jsonl').read_text().splitlines()]
    require(len(old_trials) == len(replays) == len({t['cacheKey'] for t in replays}) == 11008, 'Incomplete runtime qualification')
    for n, (trial, old) in enumerate(zip(replays, old_trials, strict=True)):
        cell, seed = original_cells[n // 32], historical_panel[n % 32]
        require(trial['id'] == old['id'] == f'trial-{n+1:06}' and trial['stage'] == old['stage'] == cell['id']
                and trial['seed'] == old['seed'] == seed and trial['inputHash'] == old['inputHash']
                and trial['recipe'] == old['recipe'], 'Historical input/recipe binding differs')
        require(read(member(qualification / 'recipes', trial['recipe'] + '.json')) == cell['scenario'], 'Changed replay recipe')
        old_name = variant + '/study/battles/' + old['id'] + '.json.gz'
        require(sha(source / old_name) == source_files[old_name], 'Changed historical report')
        raw = json.loads(gzip.decompress((qualification / 'battles' / (trial['id'] + '.json.gz')).read_bytes()))
        previous_raw = json.loads(gzip.decompress((source / old_name).read_bytes()))
        require(raw == previous_raw and raw['battle']['seed'] == seed
                and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Full historical report parity differs')
    require(read(study / 'runtime-qualification.json') == dict(status='Matched', inputs=11008, fullReports=11008,
            completedBeforeReservation=True), 'Qualification receipt differs')
    expected_attempts = []
    for phase, family, values in [('qualification', original_cells, historical_panel), ('confirmation', cells, panel)]:
        for cell in family:
            for seed in values:
                expected_attempts.append(dict(attempt=len(expected_attempts)+1, phase=phase, stage=cell['id'], seed=seed))
    require([json.loads(line) for line in (study / 'attempts.jsonl').read_text().splitlines()] == expected_attempts,
            'Changed qualification/confirmation schedule')
    trials = [json.loads(line) for line in (study / 'study/trials.jsonl').read_text().splitlines()]
    require(len(trials) == len({t['cacheKey'] for t in trials}) == 88064, 'Incomplete trials or duplicate cache identity')
    require(not {t['cacheKey'] for t in trials}.intersection(t['cacheKey'] for t in replays), 'Reused historical cache entry')
    counts = {c['id']: [0, 0] for c in cells}
    for n, trial in enumerate(trials):
        cell, seed = cells[n // 256], panel[n % 256]
        require(trial['id'] == f'trial-{n+1:06}' and trial['stage'] == cell['id'] and trial['seed'] == seed, 'Trial schedule differs')
        require(read(member(study / 'study/recipes', trial['recipe'] + '.json')) == {**cell['scenario'], 'seeds': panel}, 'Recipe changed')
        raw = json.loads(gzip.decompress((study / 'study/battles' / (trial['id'] + '.json.gz')).read_bytes()))
        require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
        require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome differs')
        counts[cell['id']][0] += raw['succeeded']
        counts[cell['id']][1] += raw['battle']['summary']['contentOutcome'] == 'Draw'
    result = read(study / 'result.json')
    require(result['status'] == 'CarriedConfirmationComplete' and result['fights'] == 99072
            and result['confirmationFights'] == 88064 and result['runtimeParityReports'] == 11008 and result['freshSeeds'] == 256
            and result['historicalSeeds'] == len(history) and result['exclusionUnion'] == len(used) and result['retries'] == 0, 'Result accounting differs')
    require(result['selectedFactor'] == selected['multiplier'] and result['historyAssessment'] == 'Pass'
            and result['historyManifest'] == q['historyPin'], 'Changed follow-up context')
    expected_rows = []
    for row, cell in zip(result['rows'], cells, strict=True):
        wins, draws = counts[cell['id']]
        expected = dict(id=cell['id'], level=cell['scenario']['party'][0]['build']['characterLevel'],
                        essenceSlots=len(cell['scenario']['party'][0]['build']['essenceIds']), wins=wins, draws=draws, samples=256)
        require(all(row[k] == v for k, v in expected.items()), 'Counts or budgets differ')
        bounds = interval(wins)
        require(all(math.isclose(row['adjusted'][k], v, abs_tol=1e-8) for k, v in bounds.items()), 'Independent simultaneous Wilson bounds differ')
        expected_rows.append({**expected, 'adjusted': bounds})
    require([sum(r['essenceSlots'] == slots and r['level'] == level for r in expected_rows)
             for slots, level in [(7, 60), (6, 50), (6, 60), (4, 30)]] == [312, 14, 4, 14], 'Missing progression controls')
    intended = [r for r in expected_rows if r['essenceSlots'] == 7]
    controls = [r for r in expected_rows if r['essenceSlots'] != 7]
    failed = any(r['wins'] > 128 for r in intended) or all(r['adjusted']['upper'] < .1 for r in intended) or any(r['wins']/256 >= .1 for r in controls)
    passed = all(r['adjusted']['upper'] <= .5 for r in intended) and any(r['adjusted']['lower'] >= .1 for r in intended) and all(r['adjusted']['upper'] < .1 for r in controls)
    assessment = 'Fail' if failed else 'Pass' if passed else 'Inconclusive'
    require(result['assessment'] == completion['assessment'] == assessment, 'Assessment differs')
    size = sum(p.stat().st_size for p in study.rglob('*') if p.is_file())
    require(size <= 4*1073741824 and result['seconds'] <= 4200, 'Storage cap exceeded')
    audit = dict(status='Verified', authenticatedFiles=len(files), studyBytes=size, fights=99072, confirmationFights=88064, runtimeParityReports=11008, freshSeeds=256,
                 historicalExclusions=len(history), exclusionUnion=len(used), allocationRejections=rejections,
                 allRecipesPreserved=True, currentRuntimeQualified=True, simultaneousBoundsVerified=True,
                 assessment=assessment, selectedFactor=selected['multiplier'], preservedHistoryAssessment='Pass', newFights=0, archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
