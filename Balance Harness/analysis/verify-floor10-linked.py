"""Read-only independent audit of all floor-10 linked calibration settings."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
from pathlib import Path

spec = importlib.util.spec_from_file_location('floor10_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
require, sha, read, member = io.require, io.sha, io.read, io.member
MULTIPLIERS = [1, 1.5, 2, 3, 4, 6, 8, 12]
VERSION = 'tower-floor10-linked-calibration-v1'


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
    require(declaration['multipliers'] == MULTIPLIERS and declaration['health'] == [round(1.64 * m, 6) for m in MULTIPLIERS]
            and declaration['offense'] == [round(.92 * m, 6) for m in MULTIPLIERS], 'Changed linked grid')
    require(all(declaration[k] == v for k, v in dict(cases=3, profiles=7, cells=21, samples=32, variants=8,
                maximumFights=5376, qualificationFightsIncluded=672, maximumSeconds=900, maximumBytes=2 * 1073741824, newSeeds=0, retries=0).items()), 'Changed envelope')
    for name, digest in {**q['inputHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed pinned input: ' + name)
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0 and process['seconds'] <= 900, 'Owner did not complete and drain')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Completion differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=5376, completed=5376, retries=0), 'Combat accounting differs')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 5377)), 'Attempt journal differs')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', variants=8, cells=168, historicalInputsMatched=672), 'Preparation differs')
    protocol = read(study / 'protocol.json')
    require(protocol['multipliers'] == MULTIPLIERS and protocol['maximumFights'] == 5376 and protocol['maximumSeconds'] == 840, 'Native protocol differs')
    source = Path(q['source'])
    require(q['sourcePin'] == sha(source / 'files.json') == '79fd4c8f4db16d4b98dd3b78dd0a53224499a0a85ed6ba92cc0cceeacffc5abe', 'Changed prior baseline')
    source_files = read(source / 'files.json')

    def source_json(name):
        require(sha(member(source, name)) == source_files[name], 'Changed source member: ' + name)
        return read(source / name)

    scope = read(study / 'source-scope.json')
    old_scope = source_json('scope.json')
    require({**scope, 'algorithm': old_scope['algorithm'], 'execution': old_scope['execution'], 'contentHashes': old_scope['contentHashes']} == old_scope, 'Changed non-execution scope')
    require(scope['contentHashes'] == q['currentContentHashes'], 'Changed current content binding')
    require(scope['algorithm'] == VERSION and scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Wrong execution')
    for name, digest in q['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong captured runtime')
    for name, digest in read(study / 'runtime-files.json').items():
        require(sha(member(study / 'executable', name)) == digest, 'Changed captured runtime file')
    seeds = source_json('request.json')['seeds']
    require(len(seeds) == len(set(seeds)) == 32, 'Wrong historical seeds')
    cells = read(study / 'cells.json')
    require(cells == [c for c in source_json('cells.json') if c['floor'] == 10], 'Changed team/gear family or composition order')
    require(len(cells) == 21 and all(c['essenceSlots'] == 6 and c['purpose'] == 'intended-progression' for c in cells), 'Missing cohorts')
    require(sha(source / 'study/trials.jsonl') == source_files['study/trials.jsonl'], 'Changed baseline ledger')
    old_trials = [json.loads(line) for line in (source / 'study/trials.jsonl').read_text().splitlines() if json.loads(line)['stage'].startswith('floor10-')]
    require(len(old_trials) == 672, 'Wrong prior trial count')
    floor_file = 'world-tower/tower-floors.json'
    historical_tower = source_json('content/Data/' + floor_file)
    original_tower = read(Path(q['apiRoot']) / 'Data' / floor_file)
    require(sha(Path(q['apiRoot']) / 'Data' / floor_file) == scope['contentHashes'][floor_file], 'Changed current floor content')
    expected_current = copy.deepcopy(historical_tower)
    next(f for f in expected_current['floors'] if f['floorNumber'] == 11)['guardianScaling'].update(health=6.525, offense=8.37)
    require(original_tower == expected_current, 'Only the checked floor-11 application may differ from the historical source')
    for name, digest in scope['contentHashes'].items():
        require(sha(Path(q['apiRoot']) / 'Data' / name) == digest, 'Changed current gameplay content')
        if name != floor_file:
            require(digest == old_scope['contentHashes'][name], 'Unrelated content differs')
    rows, summaries, baseline, cache_keys = [], [], {}, set()
    matched = 0
    for i, multiplier in enumerate(MULTIPLIERS):
        variant = study / f'variant-{i:02}'
        variant_scope = read(variant / 'scope.json')
        require({**variant_scope, 'contentHashes': scope['contentHashes']} == scope and set(variant_scope['contentHashes']) == set(scope['contentHashes']), 'Changed variant scope')
        require(read(variant / 'study/scope.json') == variant_scope, 'Battle scope differs')
        for name, digest in variant_scope['contentHashes'].items():
            require(sha(variant / 'content/Data' / name) == digest == sha(variant / 'study/content/Data' / name), 'Content copy differs')
            if i == 0 or name != floor_file:
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
            require(old_trials[n]['stage'] == key and old_trials[n]['seed'] == seed and old_trials[n]['recipe'] == trial['recipe'], 'Baseline pairing differs')
            require(trial['cacheKey'] not in cache_keys, 'Duplicate cross-setting cache identity')
            cache_keys.add(trial['cacheKey'])
            raw = json.loads(gzip.decompress((trial_root / 'battles' / (trial['id'] + '.json.gz')).read_bytes()))
            require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
            require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Report outcome differs')
            if i == 0:
                old_name = 'study/battles/' + old_trials[n]['id'] + '.json.gz'
                require(sha(source / old_name) == source_files[old_name], 'Changed baseline report')
                require(trial['inputHash'] == old_trials[n]['inputHash'] and raw == json.loads(gzip.decompress((source / old_name).read_bytes())), 'Baseline full report parity differs')
                matched += 1
            else:
                require(trial['inputHash'] != old_trials[n]['inputHash'], 'Scaling missing from input identity')
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
    require(matched == 672 and len(cache_keys) == 5376, 'Incomplete parity or cache checks')
    result = read(study / 'result.json')
    require(result['seconds'] <= 840, 'Native time cap exceeded')
    require(result['status'] == 'Floor10CalibrationComplete' and result['fights'] == 5376 and result['runtimeParityReports'] == 672, 'Incomplete result')
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
    audit = dict(status='Verified', authenticatedFiles=len(files), studyBytes=size, fights=5376, newFights=0, newSeeds=0,
                 allRecipesPreserved=True, onlyFloor10HealthAndOffenseChanged=True, baselineInputsAndFullReportsMatched=672,
                 decision=result['decision'], selected=selected, archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
