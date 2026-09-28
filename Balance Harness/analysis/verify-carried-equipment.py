"""Authenticate and independently reconstruct the complete carried-equipment screen."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
from pathlib import Path

spec = importlib.util.spec_from_file_location('carried_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
require, sha, read, member = io.require, io.sha, io.read, io.member
VERSION = 'tower-floor11-carried-equipment-screen-v1'
SOURCE_PIN = '41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378'
FLOOR10_PIN = '95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944'
FLOOR_PIN = 'dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124'
ASSUMPTION = 'Carried floor-10 equipment: Legendary / Masterpiece / rank 5 at the retained tier, with the same specializations and rolls. No gear downgrade at floor 11. Hypothetical ownership; no transferred strength claim.'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('owner', 'receipt'):
        parser.add_argument('--' + flag, type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args = parser.parse_args()
    owner, receipt = args.owner.absolute(), args.receipt.absolute()
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    output, source, floor10, api = [Path(q[k]) for k in ('output', 'source', 'floor10Source', 'apiRoot')]
    require(not receipt.exists() and not receipt.is_relative_to(output), 'New audit receipt outside sealed output required')
    require(q['version'] == declaration['version'] == VERSION and q == read(output / 'request.json')
            and sha(owner / 'request.json') == declaration['requestSha256'], 'Changed request')
    require(sha(output / 'files.json') == args.manifest_pin, 'Changed output manifest')
    files = read(output / 'files.json')
    require({p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Output inventory differs')
    for name, digest in files.items():
        require(sha(member(output, name)) == digest, 'Changed output member: ' + name)
    for name, digest in {**q['inputHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed input: ' + name)
    require(all(declaration[k] == v for k, v in dict(cells=228, samples=32, maximumFights=14592,
                qualificationFights=7296, screenFights=7296, maximumSeconds=1260, maximumBytes=2*1073741824,
                newSeeds=0, retries=0, executedModes=['baseline', 'carried']).items()), 'Changed envelope')
    protocol = read(output / 'protocol.json')
    require(all(protocol[k] == v for k, v in dict(version=VERSION, cells=228, samples=32, maximumFights=14592,
                qualificationFights=7296, screenFights=7296, maximumSeconds=1200, maximumBytes=2*1073741824,
                newSeeds=0, retries=0).items()), 'Changed native protocol')
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0
            and process['seconds'] <= 1260, 'Owner did not complete and drain')
    require(completion['status'] == 'Complete' and completion['fights'] == 14592 and completion['newSeeds'] == completion['retries'] == 0
            and completion['archiveManifestSha256'] == args.manifest_pin
            and completion['resultSha256'] == sha(output / 'result.json'), 'Completion differs')
    require(read(output / 'completion.json') == dict(status='Complete', attempts=14592, completed=14592, retries=0), 'Incomplete combat')
    require(sha(source / 'files.json') == q['sourcePin'] == SOURCE_PIN
            and sha(floor10 / 'files.json') == FLOOR10_PIN, 'Wrong source')
    source_files = read(source / 'files.json')

    def source_json(name):
        require(sha(member(source, name)) == source_files[name], 'Changed source member: ' + name)
        return read(source / name)

    prior_scope = source_json('scope.json')
    scope = read(output / 'scope.json')
    require(scope == {**prior_scope, 'algorithm': VERSION, 'execution': scope['execution'], 'contentHashes': q['contentHashes']}
            and scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Settings/content/execution binding differs')
    for name, digest in scope['execution']['assemblyHashes'].items():
        require(sha(output / 'executable' / (name + '.dll')) == digest, 'Captured executable differs')
    for name, digest in read(output / 'runtime-files.json').items():
        require(sha(member(output / 'executable', name)) == digest, 'Runtime inventory differs')
    for name, digest in scope['contentHashes'].items():
        require(sha(output / 'content/Data' / name) == sha(api / 'Data' / name) == digest, 'Current/captured content changed')
        require(digest == (FLOOR_PIN if name == 'world-tower/tower-floors.json' else prior_scope['contentHashes'][name]), 'Unconfirmed content')
    expected_floors = source_json('content/Data/world-tower/tower-floors.json')
    next(f for f in expected_floors['floors'] if f['floorNumber'] == 10)['guardianScaling'].update(health=12.71, offense=7.13)
    require(read(output / 'content/Data/world-tower/tower-floors.json') == expected_floors, 'Changed more than the applied floor-10 values')
    panel = source_json('confirmation-seeds.json'); seeds = panel[:32]
    require(len(panel) == len(set(panel)) == 256 and seeds == declaration['seeds'] == read(output / 'seeds.json'), 'Not the fixed historical panel')
    cells = source_json('cells.json')
    for cell in cells:
        cell['scenario']['seeds'] = panel
    require(cells == read(output / 'baseline-cells.json') and len(cells) == len({c['id'] for c in cells}) == 228, 'Changed baseline family')
    require([sum(c['kind'] == k for c in cells) for k in ('retained-control', 'level-control', 'seventh-addition')] == [112, 2, 114], 'Missing source cohort')
    carried = copy.deepcopy(cells)
    for cell in carried:
        s = cell['scenario']; s['assumptions'].append(ASSUMPTION)
        require(s['floorNumber'] == 11 and len(s['party']) == 10, 'Changed floor or party size')
        for actor in s['party']:
            b = actor['build']
            require(b['rank'] == 2 and b['quality'] == 'Standard', 'Wrong reference gear')
            b.setdefault('identityEquipment', copy.deepcopy(b['equipment']))
            b.update(rank=5, quality='Masterpiece')
            for item in b['equipment']:
                require(item['definitionId'].endswith('.rarity.rare') and not item['useNativeStyle'] and item['activeStyleId'] is None, 'Unexpected gear/styles')
                item['definitionId'] = item['definitionId'][:-4] + 'legendary'
    require(carried == read(output / 'carried-cells.json'), 'Carry conversion changed other recipe fields, order or tiers')
    ten_files = read(floor10 / 'files.json')
    require(sha(floor10 / 'cells.json') == ten_files['cells.json'], 'Changed floor-10 recipes')
    ten_cells = read(floor10 / 'cells.json'); matched_ten = matched_progression = 0
    for cell in carried:
        b = cell['scenario']['party'][0]['build']
        if b['characterLevel'] == 50 and len(b['essenceIds']) == 6:
            parent = next(c for c in ten_cells if c['case'] == cell['sourceCase'].replace('floor11-six-', 'floor10-retained-') and c['profile'] == cell['profile'])
            require(cell['scenario']['party'] == parent['scenario']['party'][:10], 'Changed carried floor-10 party members')
            matched_ten += 1
        if cell['kind'] in ('level-control', 'seventh-addition'):
            party = copy.deepcopy(next(c for c in carried if c['id'] == cell['sourceCase'] + '/resistance-and-health')['scenario']['party'])
            for actor in party:
                build = actor['build']
                build['identityProgression'] = dict(characterLevel=build['characterLevel'], essenceIds=build.get('identityEssenceIds', build['essenceIds']))
                build.pop('identityEssenceIds', None); build['characterLevel'] = 60
                if cell['addition']:
                    build['essenceIds'].append(cell['addition'])
            require(party == cell['scenario']['party'], 'Changed carried progression/instance identity')
            matched_progression += 1
    require((matched_ten, matched_progression) == (14, 116), 'Incomplete carry-forward links')
    require(read(output / 'preflight.json') == dict(status='PreparedNoFights', cells=456, historicalInputsMatched=7296,
            floor10PartiesMatched=14, progressionPartiesMatched=116), 'Incomplete preflight')
    require(read(output / 'runtime-qualification.json') == dict(status='Matched', inputs=7296, fullReports=7296), 'Incomplete baseline parity')
    require(sha(source / 'study/trials.jsonl') == source_files['study/trials.jsonl'], 'Changed original trials')
    original = [json.loads(line) for line in (source / 'study/trials.jsonl').read_text().splitlines()]
    require(len(original) == 58368, 'Incomplete historical family')
    old_trials = [t for i in range(228) for t in original[i*256:i*256+32]]
    expected_attempts = [dict(attempt=n+1, mode=mode, stage=c['id'], seed=seed)
                         for n, (mode, c, seed) in enumerate((mode, c, seed) for mode in ('baseline', 'carried') for c in cells for seed in seeds)]
    require([json.loads(line) for line in (output / 'attempts.jsonl').read_text().splitlines()] == expected_attempts, 'Baseline must finish before carried combat')
    baseline, rows, keys = {}, [], set()
    for mode, current in [('baseline', cells), ('carried', carried)]:
        study = output / mode; nested = read(study / 'files.json')
        require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(nested) | {'files.json'}, 'Nested inventory differs')
        require(all(files[mode + '/' + n] == h for n, h in nested.items()) and read(study / 'scope.json') == scope, 'Nested scope/manifest differs')
        for name, digest in scope['contentHashes'].items():
            require(sha(study / 'content/Data' / name) == digest, 'Study content differs')
        trials = [json.loads(line) for line in (study / 'trials.jsonl').read_text().splitlines()]
        require(len(trials) == 7296, 'Incomplete panel')
        observations = {c['id']: [] for c in current}
        for n, trial in enumerate(trials):
            cell, seed = current[n//32], seeds[n%32]
            require(trial['id'] == f'trial-{n+1:06}' and trial['stage'] == cell['id'] and trial['seed'] == seed, 'Changed trial schedule')
            require(read(member(study / 'recipes', trial['recipe'] + '.json')) == cell['scenario'], 'Changed exact recipe')
            require(trial['cacheKey'] not in keys, 'Duplicate cache identity'); keys.add(trial['cacheKey'])
            raw = json.loads(gzip.decompress((study / 'battles' / (trial['id'] + '.json.gz')).read_bytes()))
            require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
            require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome mismatch')
            if mode == 'baseline':
                old = old_trials[n]; name = 'study/battles/' + old['id'] + '.json.gz'
                require(sha(source / name) == source_files[name], 'Changed saved baseline report')
                require(trial['inputHash'] == old['inputHash'] and trial['recipe'] == old['recipe']
                        and raw == json.loads(gzip.decompress((source / name).read_bytes())), 'Baseline input/full-report parity differs')
            observations[cell['id']].append(raw)
        for cell in current:
            values = observations[cell['id']]; wins = [v['succeeded'] for v in values]
            if mode == 'baseline':
                baseline[cell['id']] = wins
                continue
            prior = baseline[cell['id']]; pairs = list(zip(wins, prior, strict=True)); build = cell['scenario']['party'][0]['build']
            rows.append(dict(id=cell['id'], kind=cell['kind'], profile=cell['profile'], level=build['characterLevel'],
                essenceSlots=len(build['essenceIds']), baselineWins=sum(prior), wins=sum(wins),
                draws=sum(v['battle']['summary']['contentOutcome'] == 'Draw' for v in values), samples=32,
                gainedWins=sum(a and not b for a, b in pairs), lostWins=sum(not a and b for a, b in pairs),
                meanGuardianHealth=sum(v['guardianHealthRemainingPercent'] for v in values)/32,
                meanDurationSeconds=sum(v['battle']['summary']['durationSeconds'] for v in values)/32))
    require(len(keys) == 14592, 'Incomplete cache identity accounting')
    require([sum(r['essenceSlots'] == slots and r['level'] == level for r in rows)
             for slots, level in [(7, 60), (6, 50), (6, 60), (4, 30)]] == [198, 14, 2, 14], 'Missing budget cohort')
    intended = [r for r in rows if r['essenceSlots'] == 7]; controls = [r for r in rows if r['essenceSlots'] != 7]
    above = sum(r['wins'] > 16 for r in intended); breach = sum(r['wins'] >= 4 for r in controls)
    viable = sum(4 <= r['wins'] <= 16 for r in intended)
    summary = dict(intendedCells=198, controlCells=30, bestIntendedWins=max(r['wins'] for r in intended),
                   bestControlWins=max(r['wins'] for r in controls), intendedAboveCeiling=above,
                   controlsAtOrAboveMinimum=breach, intendedWithObservedViability=viable,
                   decision='ObservedLowerBudgetBreach' if breach else 'ObservedCeilingBreach' if above else
                       'NoObservedViableTeam' if not viable else 'CandidateNeedsFreshConfirmation')
    result = read(output / 'result.json')
    require(result['status'] == 'CarriedEquipmentScreenComplete' and result['fights'] == 14592
            and result['screenFights'] == result['runtimeParityReports'] == 7296 and result['seconds'] <= 1200
            and all(result[k] == 0 for k in ('newSeeds', 'confirmedTeams', 'searchRuns', 'retries'))
            and result['balanceAcceptance'] == 'NotAssessedHistoricalSeeds', 'Incomplete or overstated result')
    for actual, expected in zip(result['rows'], rows, strict=True):
        means = ('meanGuardianHealth', 'meanDurationSeconds')
        require(all(actual[k] == v for k, v in expected.items() if k not in means), 'Reconstructed paired counts differ')
        require(all(math.isclose(actual[k], expected[k], abs_tol=1e-6) for k in means), 'Reconstructed means differ')
    require(result['summary'] == summary and completion['decision'] == summary['decision'], 'Screen decision differs')
    size = sum(p.stat().st_size for p in output.rglob('*') if p.is_file())
    require(size <= 2*1073741824, 'Storage limit exceeded')
    audit = dict(status='Verified', authenticatedFiles=len(files), outputBytes=size, fights=14592,
                 baselineInputsAndReportsMatched=7296, carriedFights=7296, floor10PartiesMatched=14,
                 progressionPartiesMatched=116, all228CellsRetained=True, newSeeds=0, auditFights=0,
                 summary=summary, archiveManifestSha256=args.manifest_pin, resultSha256=sha(output / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
