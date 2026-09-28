"""Read-only independent audit of the 4,256-fight progression checkpoint screen."""
import argparse
import copy
import gzip
import hashlib
import json
import math
from pathlib import Path


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


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
        path = (study / name).resolve()
        require(path.is_relative_to(study) and sha(path) == digest, 'Changed or escaping archive member: ' + name)
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    require(q == read(study / 'request.json') and sha(owner / 'request.json') == declaration['requestSha256'], 'Request binding differs')
    require(q['version'] == declaration['version'] == 'tower-progression-checkpoint-screen-v1', 'Wrong version')
    require(all(declaration[k] == v for k, v in dict(cases=19, profiles=7, samples=32, maximumFights=4256, newSeeds=0, retries=0).items()), 'Changed envelope')
    for name, digest in {**q['inputHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed input: ' + name)
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Owner did not complete and drain')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Completion differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=4256, completed=4256, retries=0), 'Combat accounting differs')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 4257)), 'Attempt journal differs')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', cells=133, knownInputMatches=224), 'Preparation differs')
    qualification = read(study / 'runtime-qualification.json')
    require(qualification['status'] == 'Matched' and qualification['inputs'] == qualification['fullReports'] == 224, 'Incomplete qualification')
    source = Path(q['source'])
    require(q['sourcePin'] == sha(source / 'files.json') == 'f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746', 'Changed qualification source')
    source_files = read(source / 'files.json')

    def old(name):
        require(sha(source / name) == source_files[name], 'Changed qualification member: ' + name)
        return read(source / name)

    scope = read(study / 'scope.json')
    original_scope = old('scope.json')
    require(scope['settings'] == original_scope['settings'] and scope['contentHashes'] == original_scope['contentHashes'], 'Changed combat scope')
    require(scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Wrong execution')
    for name, digest in q['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong captured runtime')
    for name, digest in scope['contentHashes'].items():
        require(sha(study / 'content/Data' / name) == sha(study / 'study/content/Data' / name) == digest, 'Wrong captured content')
    seeds = q['seeds']
    require(len(seeds) == len(set(seeds)) == 32 and seeds == old('request.json')['seeds'], 'Changed historical schedule')
    source_cells = [c for c in old('cells.json') if c['floor'] == 10]
    profiles = read(study / 'profiles.json')
    require(profiles == read(Path(q['profiles']))['profiles'], 'Changed profiles')
    require(len(q['cases']) == 19, 'Missing cases')
    # Reconstruct every imported party from its historical source, preserving all positions/order.
    root = Path(__file__).resolve().parents[2]
    historical = root / 'TestResults/balance/tower-whole-party-20260910'
    contexts = {s: read(historical / f'studies/slots-{s}/contexts.json')['0'] for s in (6, 7)}
    reports = {s: read(historical / f'studies/slots-{s}/party-search.json') for s in (6, 7)}
    seven = q['cases'][3:15]
    require([c['sourceId'] for c in seven] == [c['id'] for c in reports[7]['selection']], 'Not every seven-slot finalist is retained')
    draft = read(root / 'LL/tools/BalanceHarness/Fixtures/tower-progression-budget-draft.json')
    for i, case in enumerate(q['cases']):
        slots, floor = case['budget']['essenceSlots'], case['budget']['priorityFloor']
        if i == 0:
            expected = copy.deepcopy(source_cells[0]['scenario'])
        elif slots == 4:
            role = case['id'].split('-')[-1]
            expected = read(root / f'Balance Harness/Boss-Expansion-Recipes-20260911/serevin-4-slots-{role}.json')
        else:
            choice = next(c for c in reports[slots]['selection'] if c['id'] == case['sourceId'])
            if slots == 7:
                evidence = next(e for e in reports[7]['confirmation'] if e['id'] == choice['id'])
                expected_context = sorted((e for e in evidence['cells'] if e['floor'] == 11), key=lambda e: (-sum(e['clears']), e['context']))[0]['context']
                require(case['sourceContext'] == expected_context, 'Changed historical context selection')
            else:
                require(choice['id'].startswith(('097a5be24985', 'd340c925dc73')) and case['sourceContext'] == 'slots-6--balanced', 'Changed six-slot control')
            expected = copy.deepcopy(next(s for s in contexts[slots][case['sourceContext']] if s['floorNumber'] == floor))
            for p in expected['party']:
                if str(p['partySlot']) in choice['builds']:
                    p['build']['essenceIds'] = choice['builds'][str(p['partySlot'])]
        expected['seeds'] = []
        require(case['scenario'] == expected, 'Imported party differs from source')
        require(case['purpose'] == ('intended-progression' if i < 15 else 'diagnostic'), 'Cohort purpose differs')
        if i < 15:
            require(case['budget'] == next(b for b in draft['budgets'] if b['priorityFloor'] == floor), 'Intended budget differs')
    cells = read(study / 'cells.json')
    require([(c['case'], c['profile']) for c in cells] == [(c['id'], p) for c in q['cases'] for p in ['baseline'] + [p['id'] for p in profiles]], 'Cell schedule differs')
    definitions = {a['id']: a for a in read(study / 'content/Data/equipment/equipment-starters.v4.json')['items']}
    for cell in cells:
        case = next(c for c in q['cases'] if c['id'] == cell['case'])
        expected = copy.deepcopy(case['scenario']); expected['seeds'] = seeds
        require((cell['purpose'], cell['floor'], cell['essenceSlots']) == (case['purpose'], case['budget']['priorityFloor'], case['budget']['essenceSlots']), 'Changed cell budget metadata')
        if cell['profile'] != 'baseline':
            profile = next(p for p in profiles if p['id'] == cell['profile'])
            for member in expected['party']:
                if profile['partySlots'] and member['partySlot'] not in profile['partySlots']:
                    continue
                build = member['build']; build.setdefault('identityEquipment', copy.deepcopy(build['equipment']))
                for item in build['equipment']:
                    if item['slot'] in profile['specializations']:
                        identity, rarity = item['definitionId'].split('.rarity.', 1)
                        base = identity.split('.spec.', 1)[0]
                        specialization = profile['specializations'][item['slot']]
                        require(specialization in definitions[base]['specializationIds'], 'Unknown specialization')
                        item['definitionId'] = base + '.spec.' + specialization + '.rarity.' + rarity
        require(cell['scenario'] == expected, 'Gear changed other party properties')
    require([c['scenario'] for c in cells[:7]] == [c['scenario'] for c in source_cells], 'Qualification recipes differ')
    require(sha(source / 'study/trials.jsonl') == source_files['study/trials.jsonl'], 'Changed qualification ledger')
    old_trials = [json.loads(line) for line in (source / 'study/trials.jsonl').read_text().splitlines() if json.loads(line)['stage'].startswith('10/')]
    trials = [json.loads(line) for line in (study / 'study/trials.jsonl').read_text().splitlines()]
    require(len(trials) == 4256 and len({t['id'] for t in trials}) == 4256, 'Wrong trial count')
    observations = {}
    for ordinal, trial in enumerate(trials):
        cell, seed = cells[ordinal // 32], seeds[ordinal % 32]
        key = (cell['case'], cell['profile'])
        require(trial['id'] == f'trial-{ordinal+1:06}' and trial['stage'] == '/'.join(key) and trial['seed'] == seed, 'Trial schedule differs')
        require(read(study / 'study/recipes' / (trial['recipe'] + '.json')) == cell['scenario'], 'Trial recipe differs')
        report = json.loads(gzip.decompress((study / 'study/battles' / (trial['id'] + '.json.gz')).read_bytes()))
        require(report['battle']['seed'] == seed and report['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
        require(report['succeeded'] == (report['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome differs')
        observations.setdefault(key, []).append(report)
        if ordinal < 224:
            old_trial = old_trials[ordinal]
            name = 'study/battles/' + old_trial['id'] + '.json.gz'
            require(sha(source / name) == source_files[name], 'Changed qualification report')
            require(trial['inputHash'] == old_trial['inputHash'] and report == json.loads(gzip.decompress((source / name).read_bytes())), 'Runtime parity failed')
    result = read(study / 'result.json')
    require(result['status'] == 'CheckpointScreenComplete' and result['fights'] == 4256 and result['runtimeParityReports'] == 224, 'Incomplete result')
    require(result['balanceAcceptance'] == 'NotAssessedHistoricalSeeds' and all(result[k] == 0 for k in ('newSeeds', 'confirmedTeams', 'searchRuns', 'retries')), 'Unexpected strength or accounting claim')
    for row, cell in zip(result['rows'], cells, strict=True):
        values = observations[(cell['case'], cell['profile'])]
        pairs = list(zip(values, observations[(cell['case'], 'baseline')], strict=True))
        wins = sum(r['succeeded'] for r in values)
        expected = {k: cell[k] for k in ('case', 'purpose', 'floor', 'essenceSlots', 'profile')}
        expected.update(wins=wins, samples=32, draws=sum(r['battle']['summary']['contentOutcome'] == 'Draw' for r in values),
                        gainedWins=sum(c['succeeded'] and not b['succeeded'] for c, b in pairs), lostWins=sum(not c['succeeded'] and b['succeeded'] for c, b in pairs),
                        observation='ObservedAboveCeiling' if wins > 16 else 'ObservedViable' if wins >= 4 else 'ObservedBelowMinimum')
        require(all(row[k] == v for k, v in expected.items()), 'Reconstructed row differs')
        require(math.isclose(row['meanGuardianHealth'], sum(r['guardianHealthRemainingPercent'] for r in values)/32, abs_tol=1e-6), 'Health mean differs')
        require(math.isclose(row['meanDurationSeconds'], sum(r['battle']['summary']['durationSeconds'] for r in values)/32, abs_tol=1e-6), 'Duration mean differs')
    audit = dict(status='Verified', authenticatedFiles=len(files), cases=19, cells=133, fights=4256, runtimeParityReports=224,
                 newFights=0, newSeeds=0, allRetainedSevenSlotFinalistsPresent=True, importedRecipesAndGearPreserved=True,
                 archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
