"""Read-only independent audit of all floor-10 refined calibration settings."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
from pathlib import Path

spec = importlib.util.spec_from_file_location('refined_floor10_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
require, sha, read, member = io.require, io.sha, io.read, io.member
MULTIPLIERS = [6, 6.25, 6.5, 6.75, 7, 7.25, 7.5, 7.75, 8]
EXECUTION_ORDER = [0, 8, 1, 2, 3, 4, 5, 6, 7]
BOUNDARIES = {0: 5, 8: 6}
VERSION = 'tower-floor10-refined-calibration-v1'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('study', 'owner', 'receipt'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args = parser.parse_args()
    study, owner, receipt = args.study.resolve(), args.owner.resolve(), args.receipt.resolve()
    require(not receipt.exists() and not receipt.is_relative_to(study), 'Choose a new receipt outside the sealed study')
    require(sha(study / 'files.json') == args.manifest_pin, 'Changed manifest')
    files = read(study / 'files.json')
    require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Archive inventory differs')
    for name, digest in files.items():
        require(sha(member(study, name)) == digest, 'Changed archive member: ' + name)
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    require(q == read(study / 'request.json') and sha(owner / 'request.json') == declaration['requestSha256'], 'Request binding differs')
    require(q['version'] == declaration['version'] == VERSION, 'Wrong version')
    require(declaration['executionOrder'] == EXECUTION_ORDER, 'Changed execution order')
    require(declaration['multipliers'] == MULTIPLIERS and declaration['health'] == [round(1.64 * m, 6) for m in MULTIPLIERS]
            and declaration['offense'] == [round(.92 * m, 6) for m in MULTIPLIERS], 'Changed linked grid')
    require(all(declaration[k] == v for k, v in dict(cases=3, profiles=7, cells=21, samples=32, variants=9,
                maximumFights=6048, qualificationFightsIncluded=1344, maximumSeconds=900, maximumBytes=2 * 1073741824, newSeeds=0, retries=0).items()), 'Changed envelope')
    for name, digest in {**q['inputHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed pinned input: ' + name)
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0 and process['seconds'] <= 900, 'Owner did not complete and drain')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Completion differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=6048, completed=6048, retries=0), 'Combat accounting differs')
    attempts = [json.loads(line) for line in (study / 'attempts.jsonl').read_text().splitlines()]
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', variants=9, cells=189, historicalInputsMatched=1344), 'Preparation differs')
    protocol = read(study / 'protocol.json')
    require(protocol['multipliers'] == MULTIPLIERS and protocol['executionOrder'] == EXECUTION_ORDER and protocol['maximumFights'] == 6048 and protocol['maximumSeconds'] == 840, 'Native protocol differs')
    source = Path(q['source'])
    require(q['sourcePin'] == sha(source / 'files.json') == 'c67912fac6fa2f72b82676859c9f2cb525585d0cd6200f604b86d808c432bcf1', 'Changed prior baseline')
    source_files = read(source / 'files.json')

    def source_json(name):
        require(sha(member(source, name)) == source_files[name], 'Changed source member: ' + name)
        return read(source / name)

    scope = read(study / 'source-scope.json')
    old_scope = source_json('source-scope.json')
    require({**scope, 'algorithm': old_scope['algorithm'], 'execution': old_scope['execution'], 'contentHashes': old_scope['contentHashes']} == old_scope, 'Changed non-execution scope')
    require(scope['contentHashes'] == q['currentContentHashes'], 'Changed current content binding')
    require(scope['algorithm'] == VERSION and scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Wrong execution')
    for name, digest in q['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong captured runtime')
    for name, digest in read(study / 'runtime-files.json').items():
        require(sha(member(study / 'executable', name)) == digest, 'Changed captured runtime file')
    cells = read(study / 'cells.json')
    require(cells == [c for c in source_json('cells.json') if c['floor'] == 10], 'Changed team/gear family or composition order')
    require(len(cells) == 21 and all(c['essenceSlots'] == 6 and c['purpose'] == 'intended-progression' for c in cells), 'Missing cohorts')
    seeds = cells[0]['scenario']['seeds']
    require(len(seeds) == len(set(seeds)) == 32 and all(c['scenario']['seeds'] == seeds for c in cells), 'Wrong historical seed panel')
    expected_attempts = [dict(attempt=n + 1, variant=i, stage=c['case'] + '/' + c['profile'], seed=seed)
                         for n, (i, c, seed) in enumerate((i, c, seed) for i in EXECUTION_ORDER for c in cells for seed in seeds)]
    require(attempts == expected_attempts, 'Both boundary panels must finish before any intermediate setting')
    old_trials = {}
    for boundary, original_index in BOUNDARIES.items():
        name = f'variant-{original_index:02}/study/trials.jsonl'
        require(sha(source / name) == source_files[name], 'Changed boundary ledger')
        old_trials[boundary] = [json.loads(line) for line in (source / name).read_text().splitlines()]
        require(len(old_trials[boundary]) == 672, 'Incomplete boundary panel')
    floor_file = 'world-tower/tower-floors.json'
    original_tower = source_json('variant-00/content/Data/' + floor_file)
    require(sha(source / 'variant-00/content/Data' / floor_file) == scope['contentHashes'][floor_file], 'Changed captured baseline')
    require(scope['contentHashes'] == old_scope['contentHashes'], 'Changed current content')
    require(read(Path(q['apiRoot']) / 'Data' / floor_file) == original_tower, 'Changed live Tower content')
    for name, digest in scope['contentHashes'].items():
        require(sha(Path(q['apiRoot']) / 'Data' / name) == digest, 'Changed current gameplay content')
    rows, summaries, baseline, cache_keys = [], [], {}, set()
    matched = 0
    for i in EXECUTION_ORDER:
        multiplier = MULTIPLIERS[i]
        variant = study / f'variant-{i:02}'
        variant_scope = read(variant / 'scope.json')
        require({**variant_scope, 'contentHashes': scope['contentHashes']} == scope and set(variant_scope['contentHashes']) == set(scope['contentHashes']), 'Changed variant scope')
        require(read(variant / 'study/scope.json') == variant_scope, 'Battle scope differs')
        for name, digest in variant_scope['contentHashes'].items():
            require(sha(variant / 'content/Data' / name) == digest == sha(variant / 'study/content/Data' / name), 'Content copy differs')
            if name != floor_file:
                require(digest == scope['contentHashes'][name], 'Unselected content changed: ' + name)
        expected = copy.deepcopy(original_tower)
        scaling = next(f for f in expected['floors'] if f['floorNumber'] == 10)['guardianScaling']
        require(scaling['health'] == 1.64 and scaling['offense'] == .92, 'Wrong source scaling')
        scaling.update(health=round(1.64 * multiplier, 6), offense=round(.92 * multiplier, 6))
        require(read(variant / 'content/Data' / floor_file) == expected, 'Changed more than floor-10 Health/Power')
        trial_root = variant / 'study'
        nested = read(trial_root / 'files.json')
        require({p.relative_to(trial_root).as_posix() for p in trial_root.rglob('*') if p.is_file()} == set(nested) | {'files.json'}, 'Native inventory differs')
        require(all(files[f'variant-{i:02}/study/{name}'] == digest for name, digest in nested.items()), 'Native manifest differs')
        trials = [json.loads(line) for line in (trial_root / 'trials.jsonl').read_text().splitlines()]
        require(len(trials) == 672, 'Incomplete setting')
        observations = {}
        for n, trial in enumerate(trials):
            cell, seed = cells[n // 32], seeds[n % 32]
            key = cell['case'] + '/' + cell['profile']
            require(trial['id'] == f'trial-{n+1:06}' and trial['stage'] == key and trial['seed'] == seed, 'Trial schedule differs')
            require(read(member(trial_root / 'recipes', trial['recipe'] + '.json')) == cell['scenario'], 'Changed party or Essence order')
            require(all(t[n]['stage'] == key and t[n]['seed'] == seed and t[n]['recipe'] == trial['recipe'] for t in old_trials.values()), 'Boundary pairing differs')
            require(trial['cacheKey'] not in cache_keys, 'Duplicate cross-setting cache identity')
            cache_keys.add(trial['cacheKey'])
            raw = json.loads(gzip.decompress((trial_root / 'battles' / (trial['id'] + '.json.gz')).read_bytes()))
            require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
            require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Report outcome differs')
            if i in BOUNDARIES:
                old = old_trials[i][n]
                old_name = f"variant-{BOUNDARIES[i]:02}/study/battles/{old['id']}.json.gz"
                require(sha(source / old_name) == source_files[old_name], 'Changed boundary report')
                require(trial['inputHash'] == old['inputHash'] and raw == json.loads(gzip.decompress((source / old_name).read_bytes())), 'Boundary full-report parity differs')
                matched += 1
            else:
                require(all(trial['inputHash'] != t[n]['inputHash'] for t in old_trials.values()), 'Intermediate scaling missing from input identity')
            observations.setdefault(key, []).append(raw)
        for cell in cells:
            key = cell['case'] + '/' + cell['profile']; values = observations[key]
            wins = [r['succeeded'] for r in values]
            if i == 0:
                baseline[key] = wins
            pairs = list(zip(wins, baseline[key], strict=True))
            rows.append(dict(variant=i, multiplier=multiplier, **{k: cell[k] for k in ('case', 'purpose', 'essenceSlots', 'profile')},
                             wins=sum(wins), draws=sum(r['battle']['summary']['contentOutcome'] == 'Draw' for r in values), samples=32,
                             meanGuardianHealth=sum(r['guardianHealthRemainingPercent'] for r in values)/32,
                             meanDurationSeconds=sum(r['battle']['summary']['durationSeconds'] for r in values)/32,
                             gainedWins=sum(a and not b for a, b in pairs), lostWins=sum(not a and b for a, b in pairs)))
        current = [r for r in rows if r['variant'] == i]
        best = max(r['wins'] for r in current)
        summary = dict(variant=i, multiplier=multiplier, health=round(1.64 * multiplier, 6), offense=round(.92 * multiplier, 6),
                       bestIntendedWins=best, intendedAboveCeiling=sum(r['wins'] > 16 for r in current),
                       bestIntendedCells=[r['case'] + '/' + r['profile'] for r in current if r['wins'] == best],
                       eligible=4 <= best <= 16)
        require(read(variant / 'summary.json') == summary, 'Variant summary differs')
        summaries.append(summary)
    summaries.sort(key=lambda s: s["variant"])
    require(matched == 1344 and len(cache_keys) == 6048, 'Incomplete parity or cache checks')
    result = read(study / 'result.json')
    require(result['seconds'] <= 840, 'Native time cap exceeded')
    require(result['status'] == 'RefinedFloor10CalibrationComplete' and result['fights'] == 6048 and result['runtimeParityReports'] == 1344, 'Incomplete result')
    require(result['balanceAcceptance'] == 'NotAssessedHistoricalSeeds' and all(result[k] == 0 for k in ('newSeeds', 'confirmedTeams', 'searchRuns', 'retries')), 'Unexpected acceptance or accounting')
    for actual, expected in zip(result['rows'], rows, strict=True):
        means = ('meanGuardianHealth', 'meanDurationSeconds')
        require(all(actual[k] == v for k, v in expected.items() if k not in means), 'Reconstructed outcome counts differ')
        require(all(math.isclose(actual[k], expected[k], abs_tol=1e-6) for k in means), 'Reconstructed means differ')
    selected = next((s for s in summaries if s['eligible']), None)
    require(result['summaries'] == summaries and result['selected'] == selected, 'Selection differs')
    require(result['decision'] == ('DiagnosticCalibrationCandidate' if selected else 'NoSettingInGrid'), 'Decision differs')
    size = sum(p.stat().st_size for p in study.rglob('*') if p.is_file())
    require(size <= 2 * 1073741824, 'Storage cap exceeded')
    audit = dict(status='Verified', authenticatedFiles=len(files), studyBytes=size, fights=6048, newFights=0, newSeeds=0,
                 allRecipesPreserved=True, onlyFloor10HealthAndOffenseChanged=True, boundaryInputsAndFullReportsMatched=1344,
                 decision=result['decision'], selected=selected, archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
