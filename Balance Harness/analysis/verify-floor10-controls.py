"""Independently reconstruct the frozen five-Essence screen and ordinary acquisition arithmetic; zero fights."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
from pathlib import Path
import sys

spec = importlib.util.spec_from_file_location('floor10_control_launch', Path(__file__).with_name('screen-floor10-controls.py'))
launch = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = launch
spec.loader.exec_module(launch)
io = launch.io


def load_report(root, name):
    return json.loads(gzip.decompress((root / 'battles' / (name + '.json.gz')).read_bytes()))


def reconstruct_cells(parents, panel):
    cells = [dict(id=p['case'] + '/' + p['profile'], sourceCase=p['case'], profile=p['profile'], level=50,
                  removedIndex=None, scenario={**copy.deepcopy(p['scenario']), 'seeds': panel}) for p in parents]
    for p in parents:
        for level in (40, 50):
            for removed in range(6):
                s = copy.deepcopy(p['scenario'])
                s['seeds'] = panel[:32]
                s['assumptions'].append(f"Controlled five-Essence ablation: remove original position {removed + 1}; level {level}; equipment tier capped to that level's eligibility; retained Essence order and actor/item/surviving Essence identities. Hypothetical gear ownership; historical diagnostic only.")
                for actor in s['party']:
                    b = actor['build']
                    io.require(len(b['essenceIds']) == 6 and b['characterLevel'] == 50 and b['tier'] == 2
                               and b['rank'] == 5 and b['quality'] == 'Masterpiece', 'Unexpected parent budget')
                    io.require('identityProgression' not in b and 'identityEssenceIndices' not in b, 'Unexpected parent identity map')
                    b['identityProgression'] = dict(characterLevel=b['characterLevel'], essenceIds=b.get('identityEssenceIds', b['essenceIds']), tier=b['tier'])
                    b.pop('identityEssenceIds', None)
                    b['identityEssenceIndices'] = [i for i in range(6) if i != removed]
                    b['essenceIds'] = [x for i, x in enumerate(b['essenceIds']) if i != removed]
                    b['characterLevel'] = level
                    b['tier'] = 1 if level == 40 else 2
                cells.append(dict(id=p['case'] + '/' + p['profile'] + f'/level-{level}/remove-{removed + 1}',
                    sourceCase=p['case'], profile=p['profile'], level=level, removedIndex=removed, scenario=s))
    return cells


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--owner', type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    owner = io.unlinked(args.owner.absolute())
    q = io.read(owner / 'request.json'); declaration = io.read(owner / 'declaration.json')
    output, source, api = [io.unlinked(Path(q[k])) for k in ('output', 'source', 'apiRoot')]
    receipt = io.unlinked(args.receipt.absolute())
    io.require(not receipt.exists() and not receipt.is_relative_to(output), 'New receipt outside the archive required')
    io.require(q['version'] == declaration['version'] == launch.VERSION and source == launch.SOURCE
               and io.sha(owner / 'request.json') == declaration['requestSha256'], 'Changed declaration')
    io.require(all(declaration[k] == v for k, v in dict(cells=273, references=21, controls=252, samples=32, levels=[40, 50],
        tiers=[1, 2], maximumFights=8736, qualificationFights=672, controlFights=8064, historicalInputs=5376,
        maximumSeconds=1260, maximumNativeSeconds=1200, maximumBytes=2*1073741824, newSeeds=0, retries=0).items()), 'Changed envelope')
    for name, digest in q['inputHashes'].items():
        io.require(io.sha(Path(name)) == digest, 'Changed frozen input: ' + name)
    io.require(io.sha(output / 'files.json') == args.manifest_pin, 'Changed output manifest')
    files = io.read(output / 'files.json')
    io.require({p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Changed inventory')
    for name, digest in files.items():
        io.require(io.sha(io.member(output, name)) == digest, 'Changed output member: ' + name)
    io.require(q == io.read(output / 'request.json'), 'Native request differs')
    io.require(io.sha(source / 'files.json') == q['sourcePin'] == launch.PIN, 'Changed confirmation')
    source_files = io.read(source / 'files.json')

    def source_json(name):
        io.require(io.sha(source / name) == source_files[name], 'Changed source member: ' + name)
        return io.read(source / name)

    prior = source_json('scope.json'); scope = io.read(output / 'scope.json')
    io.require(scope['algorithm'] == launch.VERSION and scope['settings'] == prior['settings']
               and scope['reportStorage'] == 'gzip-json-v1' and scope['contentHashes'] == q['contentHashes']
               and scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Changed native scope')
    for name, digest in q['contentHashes'].items():
        io.require(io.sha(api / 'Data' / name) == io.sha(output / 'content/Data' / name) == digest, 'Changed current/captured content')
        io.require(digest == (launch.FLOOR_PIN if name == 'world-tower/tower-floors.json' else prior['contentHashes'][name]), 'Unconfirmed content')
    for extra in ('equipment-ordinary.v1.json', 'equipment-upgrades.v1.json'):
        io.require(io.sha(api / 'Data/equipment' / extra) == io.sha(output / 'content/Data/equipment' / extra), 'Changed acquisition input')
    for name, digest in q['assemblyHashes'].items():
        io.require(io.sha(output / 'executable' / (name + '.dll')) == digest, 'Captured runtime differs')
    for name, digest in io.read(output / 'runtime-files.json').items():
        io.require(io.sha(io.member(output / 'executable', name)) == digest, 'Changed runtime member')
    panel = source_json('confirmation-seeds.json')
    io.require(len(panel) == len(set(panel)) == 256 and panel[:32] == declaration['seeds'] == io.read(output / 'seeds.json'), 'Changed seed schedule')
    parents = source_json('cells.json')
    cases = ['floor10-authored', 'floor10-retained-1', 'floor10-retained-2']
    profiles = ['baseline', 'precision', 'ability-haste', 'restorer-specialization', 'armor-and-health', 'resistance-and-health', 'health-and-regeneration']
    io.require([(p['case'], p['profile']) for p in parents] == [(c, p) for c in cases for p in profiles], 'Missing reference family')
    cells = reconstruct_cells(parents, panel)
    io.require(cells == io.read(output / 'cells.json') and len(cells) == 273, 'Changed removal, order, budget, gear or identities')
    io.require(io.read(output / 'preflight.json') == dict(prepared=273, historicalInputsMatched=5376, fights=0), 'Incomplete preparation')
    io.require(io.read(output / 'runtime-qualification.json') == dict(status='Matched', inputs=5376, fullReports=672), 'Incomplete qualification')
    io.require(io.sha(source / 'study/trials.jsonl') == source_files['study/trials.jsonl'], 'Changed source trials')
    original = [json.loads(l) for l in (source / 'study/trials.jsonl').read_text().splitlines()]
    matches = [json.loads(l) for l in (output / 'input-matches.jsonl').read_text().splitlines()]
    io.require(len(original) == 5376 and matches == [dict(Id=t['id'], InputHash=t['inputHash']) for t in original], 'Incomplete original input parity')
    attempts = [json.loads(l) for l in (output / 'attempts.jsonl').read_text().splitlines()]
    io.require(attempts == [dict(attempt=i*32+j+1, cell=c['id'], seed=s) for i,c in enumerate(cells) for j,s in enumerate(panel[:32])], 'Changed attempt schedule')
    trials = [json.loads(l) for l in (output / 'trials.jsonl').read_text().splitlines()]
    io.require(len(trials) == len({t['cacheKey'] for t in trials}) == 8736, 'Incomplete fights or duplicate cache identities')
    rows = []; baselines = {}; actor_ids = {}
    for i, cell in enumerate(cells):
        reports = []
        for j, seed in enumerate(panel[:32]):
            trial = trials[i*32+j]
            io.require((trial['id'], trial['stage'], trial['seed']) == (f'trial-{i*32+j+1:06}', cell['id'], seed), 'Changed trial binding')
            io.require(io.read(output / 'recipes' / (trial['recipe'] + '.json')) == cell['scenario'], 'Changed archived recipe')
            raw = load_report(output, trial['id']); reports.append(raw)
            io.require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Changed report binding')
            io.require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome mismatch')
            if i < 21:
                old = original[i*256+j]
                name = 'study/battles/' + old['id'] + '.json.gz'
                io.require(io.sha(source / name) == source_files[name], 'Changed qualification report')
                io.require(trial['inputHash'] == old['inputHash'] and trial['recipe'] == old['recipe']
                           and raw == load_report(source / 'study', old['id']), 'Original input/full-report parity differs')
        parent_id = cell['sourceCase'] + '/' + cell['profile']
        wins = [r['succeeded'] for r in reports]
        participants = reports[0]['battle']['preparedParticipants']
        current_ids = [p['slot'] for p in participants if p['slot']['side'] == 'Friendly']
        io.require(len(current_ids) == 15 and all(p['level'] == cell['level'] for p in participants if p['slot']['side'] == 'Friendly'), 'Changed party or level')
        if cell['removedIndex'] is None:
            baselines[parent_id] = wins; actor_ids[parent_id] = current_ids
        io.require(current_ids == actor_ids[parent_id], 'Changed actor identity or placement')
        before = baselines[parent_id]
        rows.append(dict(id=cell['id'], sourceCase=cell['sourceCase'], profile=cell['profile'], level=cell['level'],
            tier=cell['scenario']['party'][0]['build']['tier'], removedIndex=cell['removedIndex'],
            essenceSlots=6 if cell['removedIndex'] is None else 5, wins=sum(wins),
            draws=sum(r['battle']['summary']['contentOutcome']=='Draw' for r in reports), samples=32,
            gainedWins=sum(a and not b for a,b in zip(wins,before)), lostWins=sum(not a and b for a,b in zip(wins,before)),
            meanGuardianHealth=sum(r['guardianHealthRemainingPercent'] for r in reports)/32,
            meanDurationSeconds=sum(r['battle']['summary']['durationSeconds'] for r in reports)/32))
    controls = rows[21:]
    summary = dict(bestReferenceWins=max(r['wins'] for r in rows[:21]),
        bestLevel40ControlWins=max(r['wins'] for r in controls if r['level']==40),
        bestLevel50ControlWins=max(r['wins'] for r in controls if r['level']==50),
        controlsAtOrAboveMinimum=sum(r['wins']>=4 for r in controls),
        decision='ObservedLowerEssenceBreach' if any(r['wins']>=4 for r in controls) else 'NoObservedLowerEssenceBreach')
    result = io.read(output / 'result.json')
    io.require(all(result[k] == v for k,v in dict(status='Floor10ControlScreenComplete', fights=8736, qualificationReports=672,
        historicalInputsMatched=5376, controlFights=8064, samples=32, cells=273, newSeeds=0, retries=0, confirmedTeams=0,
        balanceAcceptance='NotAssessedHistoricalSeeds', summary=summary).items()) and result['seconds'] <= 1200, 'Wrong result or budget')
    for actual, expected in zip(result['rows'], rows, strict=True):
        for key, value in expected.items():
            io.require(math.isclose(actual[key], value, rel_tol=1e-10, abs_tol=1e-10) if key.startswith('mean') else actual[key] == value, 'Reconstructed row differs: ' + key)
    pool = next(p for p in io.read(output / 'content/Data/equipment/equipment-ordinary.v1.json') if p['equipmentTier'] == 2)
    price = next(p for p in io.read(output / 'content/Data/equipment/equipment-upgrades.v1.json')['tiers'] if p['tier']==2)
    acquisitions = io.read(output / 'acquisition.json')
    io.require(len(acquisitions)==21, 'Incomplete acquisition audit')
    # The plain/specialized item definitions are generated from this exact live release.
    equipment = io.read(output / 'content/Data/equipment/equipment-starters.v4.json')
    base_items = {item['id']: item for item in equipment['items']}
    for cell, entry in zip(cells[:21], acquisitions, strict=True):
        audit = entry['audit']; gear = [e for actor in cell['scenario']['party'] for e in actor['build']['equipment']]
        slots = 0
        for item in gear:
            definition = item['definitionId']; io.require(definition.endswith('.rarity.legendary'), 'Wrong rarity')
            stem = definition.removesuffix('.rarity.legendary'); archetype, _, specialization = stem.partition('.spec.')
            io.require(archetype in base_items and (not specialization or specialization in base_items[archetype]['specializationIds']), 'Definition unavailable in ordinary pool')
            slots += 2 if base_items[archetype]['equipmentType'] == 'TwoHanded' else 1
        chance = pool['dungeonEquipment']['rarities']['champion']['legendary'] * pool['dungeonEquipment']['qualities']['masterpiece']
        start = pool['dungeonEquipment']['rank']
        io.require(entry['id']==cell['id'] and audit['characters']==15 and audit['items']==len(gear) and audit['occupiedSlots']==slots==120
                   and audit['tier']==2 and audit['missingOrdinaryDefinitions']==0, 'Acquisition counts differ')
        for key,value in dict(championLegendaryMasterpiecePerEquipmentDrop=chance,
            championLegendaryMasterpiecePerCompletionAtMasteryZero=chance*pool['dungeonEquipment']['dropChance'],
            championLegendaryMasterpiecePerCompletionAtMasteryTen=chance*min(1,pool['dungeonEquipment']['dropChance']+.5),
            expectedDropsIgnoringFitAndRolls=len(gear)/chance).items():
            io.require(math.isclose(audit[key],value,rel_tol=1e-12), 'Acquisition probability differs')
        io.require(audit['reinforcementFromDungeonRank']==dict(startingRank=start,parts=sum(price['rankPartCosts'][start:5])*slots,
            cinders=sum(price['rankCinderCosts'][start:5])*slots), 'Reinforcement arithmetic differs')
    process=io.read(owner/'process.json'); completion=io.read(owner/'completion.json')
    io.require(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=1260,'Owner did not finish and drain')
    io.require(io.read(output/'completion.json')==dict(status='Complete',attempts=8736,completed=8736,retries=0),'Native accounting differs')
    io.require(completion==dict(status='Complete',fights=8736,newSeeds=0,retries=0,archiveManifestSha256=args.manifest_pin,
        resultSha256=io.sha(output/'result.json'),decision=summary['decision']),'Owner accounting differs')
    size=sum(p.stat().st_size for p in output.rglob('*') if p.is_file());io.require(size<=2*1073741824,'Storage exceeded')
    verified=dict(status='Verified',authenticatedFiles=len(files)+1,studyBytes=size,fights=8736,qualificationReports=672,
        historicalInputsMatched=5376,controlFights=8064,allRecipesPreserved=True,acquisitionRowsVerified=21,
        newSeeds=0,auditFights=0,summary=summary,archiveManifestSha256=args.manifest_pin,resultSha256=io.sha(output/'result.json'))
    io.write(receipt,verified);print(json.dumps(verified,indent=2))


if __name__ == '__main__':
    main()
