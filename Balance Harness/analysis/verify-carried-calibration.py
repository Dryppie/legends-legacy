"""Read-only independent audit of the carried-equipment floor-11 calibration."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
from pathlib import Path

spec = importlib.util.spec_from_file_location('carried_calibration_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
require, sha, read, member = io.require, io.sha, io.read, io.member
MULTIPLIERS = [1, 2, 4, 8]
VERSION = 'tower-floor11-carried-calibration-v1'


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
    require(declaration['multipliers'] == MULTIPLIERS and declaration['health'] == [round(6.525 * m, 6) for m in MULTIPLIERS]
            and declaration['offense'] == [round(8.37 * m, 6) for m in MULTIPLIERS], 'Changed linked grid')
    require(all(declaration[k] == v for k, v in dict(cells=344, retainedCells=112, levelControls=4, additions=228, samples=32, variants=4, importedCells=228, addedArmorCells=116,
                maximumFights=44032, qualificationFightsIncluded=7296, maximumSeconds=1860, maximumBytes=2 * 1073741824, newSeeds=0, retries=0).items()), 'Changed envelope')
    for name, digest in {**q['inputHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed pinned input: ' + name)
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0 and process['seconds'] <= 1860, 'Owner did not complete and drain')
    require(completion['status'] == 'Complete' and completion['fights'] == 44032 and completion['newSeeds'] == completion['retries'] == 0, 'Owner accounting differs')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Completion differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=44032, completed=44032, retries=0), 'Combat accounting differs')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 44033)), 'Attempt journal differs')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', variants=4, cells=1376, historicalInputsMatched=7296, addedArmorCells=116), 'Preparation differs')
    protocol = read(study / 'protocol.json')
    require(protocol['version'] == VERSION and protocol['samples'] == 32 and protocol['cells'] == 344 and protocol['newSeeds'] == protocol['retries'] == 0, 'Native envelope differs')
    require(protocol['maximumBytes'] == 2*1073741824, 'Changed native storage limit')
    require(protocol['multipliers'] == MULTIPLIERS and protocol['maximumFights'] == 44032 and protocol['maximumSeconds'] == 1800, 'Native protocol differs')
    source = Path(q['source'])
    require(q['sourcePin'] == sha(source / 'files.json') == '76e028152ed3ccfb09e5e3bf60ff5ae3acabcb0a8f6db18a395806ed19f256f1', 'Changed prior baseline')
    source_files = read(source / 'files.json')
    require(sha(source / 'result.json') == source_files['result.json'] and read(source / 'result.json')['status'] == 'CarriedEquipmentScreenComplete', 'Incomplete source screen')

    def source_json(name):
        require(sha(member(source, name)) == source_files[name], 'Changed source member: ' + name)
        return read(source / name)

    scope = read(study / 'source-scope.json')
    old_scope = source_json('scope.json')
    require({**scope, 'algorithm': old_scope['algorithm'], 'execution': old_scope['execution']} == old_scope, 'Changed non-execution scope')
    require(scope['algorithm'] == VERSION and scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Wrong execution')
    require(sha(Path(q['runtime']).parents[1] / 'EssenceSystem.Tests/release/EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Changed tested assembly')
    for name, digest in q['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong captured runtime')
    for name, digest in read(study / 'runtime-files.json').items():
        require(sha(member(study / 'executable', name)) == digest, 'Changed captured runtime file')
    seeds = source_json('seeds.json')
    require(len(seeds) == len(set(seeds)) == 32 and seeds == declaration['seeds'], 'Wrong historical seeds')
    retained = source_json('carried-cells.json')
    cells = read(study / 'cells.json')
    require(len(retained) == 228 and cells[:228] == retained, 'Changed retained team/gear family or order')
    definitions = source_json('content/Data/essences/essences.json')['essences']
    monsters = {e['id']: e['sourceMonsterId'].lower() for e in definitions}
    expanded = []
    for source_case in ('floor11-six-1', 'floor11-six-2'):
        parent = next(c for c in retained if c['id'] == source_case + '/armor-and-health')
        used = {monsters[e] for actor in parent['scenario']['party'] for e in actor['build']['essenceIds']}
        options = sorted(e['id'] for e in definitions if e['sourceMonsterId'].lower() not in used)
        require(len(options) == len(set(options)) == 57, 'Changed legal uniform additions')
        for addition in [None, *options]:
            scenario = copy.deepcopy(parent['scenario'])
            for actor in scenario['party']:
                b = actor['build']
                require(b['characterLevel'] == 50 and len(b['essenceIds']) == 6 and b['rank'] == 5
                        and b['quality'] == 'Masterpiece' and b['tier'] == 2, 'Wrong armor parent budget')
                require(all(e['definitionId'].endswith('.rarity.legendary') for e in b['equipment']), 'Wrong parent gear')
                b['identityProgression'] = b.get('identityProgression') or dict(characterLevel=b['characterLevel'], essenceIds=copy.deepcopy(b.get('identityEssenceIds', b['essenceIds'])))
                b.pop('identityEssenceIds', None)
                b['characterLevel'] = 60
                if addition is not None:
                    b['essenceIds'].append(addition)
            scenario['assumptions'].append('Controlled progression comparison: level 60; appended Essence ' + (addition or 'none') + '; original six Essences, gear and instance identities retained. No transferred strength claim.')
            expanded.append(dict(id=source_case + '/armor/' + ('add/' + addition if addition else 'level60-six'),
                                 sourceCase=source_case, kind='seventh-addition' if addition else 'level-control',
                                 addition=addition, profile='armor-and-health', scenario=scenario))
    require(len(expanded) == 116 and cells[228:] == expanded, 'Changed armor upgrades, gear, identities or Essence order')
    require([sum(c['kind'] == k for c in cells) for k in ('retained-control', 'level-control', 'seventh-addition')] == [112, 4, 228], 'Missing source families')
    require(all(c['scenario']['seeds'] == retained[0]['scenario']['seeds'] and c['scenario']['floorNumber'] == 11 for c in cells), 'Changed seeds or floor')
    require(cells[0]['scenario']['seeds'][:32] == seeds, 'Changed execution panel')
    require(len(cells) == len({c['id'] for c in cells}) == 344
            and [sum(len(c['scenario']['party'][0]['build']['essenceIds']) == slots and c['scenario']['party'][0]['build']['characterLevel'] == level for c in cells)
                 for slots, level in [(7, 60), (6, 50), (6, 60), (4, 30)]] == [312, 14, 4, 14], 'Missing cohorts')
    expected_attempts = [dict(attempt=n+1, variant=i, stage=c['id'], seed=seed)
                         for n, (i, c, seed) in enumerate((i, c, seed) for i in range(4) for c in cells for seed in seeds)]
    require([json.loads(line) for line in (study / 'attempts.jsonl').read_text().splitlines()] == expected_attempts, 'Changed variant/cell/seed schedule')
    require(read(study / 'runtime-qualification.json') == dict(status='Matched', inputs=7296, fullReports=7296), 'Incomplete baseline qualification')
    require(sha(source / 'carried/trials.jsonl') == source_files['carried/trials.jsonl'], 'Changed baseline ledger')
    old_trials = [json.loads(line) for line in (source / 'carried/trials.jsonl').read_text().splitlines()]
    require(len(old_trials) == 7296, 'Wrong prior trial count')
    floor_file = 'world-tower/tower-floors.json'
    original_tower = read(source / 'content/Data' / floor_file)
    require(sha(source / 'content/Data' / floor_file) == scope['contentHashes'][floor_file], 'Changed source tower')
    rows, summaries, baseline, cache_keys = [], [], {}, set()
    matched = 0; baseline_hashes = []
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
        scaling = next(f for f in expected['floors'] if f['floorNumber'] == 11)['guardianScaling']
        require(scaling['health'] == 6.525 and scaling['offense'] == 8.37, 'Wrong source scaling')
        scaling.update(health=round(6.525 * multiplier, 6), offense=round(8.37 * multiplier, 6))
        require(read(variant / 'content/Data' / floor_file) == expected, 'Changed more than floor-11 Health/Power')
        trial_root = variant / 'study'
        nested = read(trial_root / 'files.json')
        require({p.relative_to(trial_root).as_posix() for p in trial_root.rglob('*') if p.is_file()} == set(nested) | {'files.json'}, 'Native inventory differs')
        require(all(files[f'variant-{i:02}/study/{name}'] == digest for name, digest in nested.items()), 'Native manifest differs')
        trials = [json.loads(line) for line in (trial_root / 'trials.jsonl').read_text().splitlines()]
        require(len(trials) == 11008, 'Incomplete setting')
        observations = {}
        for n, trial in enumerate(trials):
            cell, seed = cells[n // 32], seeds[n % 32]
            key = cell['id']
            require(trial['id'] == f'trial-{n+1:06}' and trial['stage'] == key and trial['seed'] == seed, 'Trial schedule differs')
            require(read(member(trial_root / 'recipes', trial['recipe'] + '.json')) == cell['scenario'], 'Changed party or Essence order')
            if n < 7296:
                require(old_trials[n]['stage'] == key and old_trials[n]['seed'] == seed and old_trials[n]['recipe'] == trial['recipe'], 'Baseline pairing differs')
            require(trial['cacheKey'] not in cache_keys, 'Duplicate cross-setting cache identity')
            cache_keys.add(trial['cacheKey'])
            raw = json.loads(gzip.decompress((trial_root / 'battles' / (trial['id'] + '.json.gz')).read_bytes()))
            require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
            require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Report outcome differs')
            if i == 0 and n < 7296:
                old_name = 'carried/battles/' + old_trials[n]['id'] + '.json.gz'
                require(sha(source / old_name) == source_files[old_name], 'Changed baseline report')
                require(trial['inputHash'] == old_trials[n]['inputHash'] and raw == json.loads(gzip.decompress((source / old_name).read_bytes())), 'Baseline full report parity differs')
                matched += 1
            elif i > 0:
                require(trial['inputHash'] != baseline_hashes[n], 'Scaling missing from input identity')
            if i == 0:
                baseline_hashes.append(trial['inputHash'])
            observations.setdefault(key, []).append(raw)
        for cell in cells:
            key = cell['id']; values = observations[key]
            wins = [r['succeeded'] for r in values]
            if i == 0:
                baseline[key] = wins
            pairs = list(zip(wins, baseline[key], strict=True))
            rows.append(dict(variant=i, multiplier=multiplier, **{k: cell[k] for k in ('id', 'sourceCase', 'kind', 'addition', 'profile')},
                             level=cell['scenario']['party'][0]['build']['characterLevel'],
                             essenceSlots=len(cell['scenario']['party'][0]['build']['essenceIds']),
                             wins=sum(wins), draws=sum(r['battle']['summary']['contentOutcome'] == 'Draw' for r in values), samples=32,
                             meanGuardianHealth=sum(r['guardianHealthRemainingPercent'] for r in values)/32,
                             meanDurationSeconds=sum(r['battle']['summary']['durationSeconds'] for r in values)/32,
                             gainedWins=sum(a and not b for a, b in pairs), lostWins=sum(not a and b for a, b in pairs)))
        current = [r for r in rows if r['variant'] == i]
        intended = [r for r in current if r['essenceSlots'] == 7]
        best = max(r['wins'] for r in intended)
        six50 = max(r['wins'] for r in current if r['essenceSlots'] == 6 and r['level'] == 50)
        six60 = max(r['wins'] for r in current if r['essenceSlots'] == 6 and r['level'] == 60)
        four = max(r['wins'] for r in current if r['essenceSlots'] == 4)
        band, separated = 4 <= best <= 16, max(six50, six60, four) < 4
        summary = dict(variant=i, multiplier=multiplier, health=round(6.525 * multiplier, 6), offense=round(8.37 * multiplier, 6),
                       bestIntendedWins=best, bestSixLevel50Wins=six50, bestSixLevel60Wins=six60, bestFourWins=four, intendedAboveCeiling=sum(r['wins'] > 16 for r in intended),
                       bestIntendedCells=[r['id'] for r in intended if r['wins'] == best],
                       observedIntendedBand=band, observedLowerBudgetSeparation=separated, eligible=band and separated)
        require(read(variant / 'summary.json') == summary, 'Variant summary differs')
        summaries.append(summary)
    require(matched == 7296 and len(cache_keys) == 44032, 'Incomplete parity or cache checks')
    result = read(study / 'result.json')
    require(result['status'] == 'CarriedCalibrationComplete' and result['fights'] == 44032 and result['runtimeParityReports'] == 7296 and result['seconds'] <= 1800, 'Incomplete result')
    require(result['balanceAcceptance'] == 'NotAssessedHistoricalSeeds' and all(result[k] == 0 for k in ('newSeeds', 'confirmedTeams', 'searchRuns', 'retries')), 'Unexpected acceptance or accounting')
    for actual, expected in zip(result['rows'], rows, strict=True):
        means = ('meanGuardianHealth', 'meanDurationSeconds')
        require(all(actual[k] == v for k, v in expected.items() if k not in means), 'Reconstructed outcome counts differ')
        require(all(math.isclose(actual[k], expected[k], abs_tol=1e-6) for k in means), 'Reconstructed means differ')
    selected = next((s for s in summaries if s['eligible']), None)
    require(result['summaries'] == summaries and result['selected'] == selected, 'Selection differs')
    require(result['decision'] == ('DiagnosticCalibrationCandidate' if selected else 'NoSeparatingSettingInGrid'), 'Decision differs')
    size = sum(p.stat().st_size for p in study.rglob('*') if p.is_file())
    require(size <= 2 * 1073741824, 'Storage cap exceeded')
    audit = dict(status='Verified', authenticatedFiles=len(files), studyBytes=size, fights=44032, newFights=0, newSeeds=0,
                 all228ImportedRecipesPreserved=True, armorUpgradesReconstructed=116, onlyFloor11HealthAndOffenseChanged=True, baselineInputsAndFullReportsMatched=7296,
                 decision=result['decision'], selected=selected, archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
