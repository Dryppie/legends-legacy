"""Read-only independent audit of the repeating-equipment checkpoint baseline."""
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
    require(q['version'] == declaration['version'] == 'tower-equipment-cycle-screen-v1', 'Wrong version')
    require(all(declaration[k] == v for k, v in dict(cases=19, profiles=7, samples=32, maximumFights=4480,
                screenFights=4256, qualificationFights=224, newSeeds=0, retries=0).items()), 'Changed envelope')
    for name, digest in {**q['inputHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed input: ' + name)
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Owner did not complete and drain')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Completion differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=4480, completed=4480, retries=0), 'Combat accounting differs')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 4481)), 'Attempt journal differs')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', cells=133, qualificationCells=7, knownInputMatches=224), 'Preparation differs')
    qualification = read(study / 'runtime-qualification.json')
    require(qualification['status'] == 'Matched' and qualification['inputs'] == qualification['fullReports'] == 224, 'Incomplete qualification')
    source = Path(q['source'])
    require(q['sourcePin'] == sha(source / 'files.json') == 'c488b7987ca1ac44dbc06ad43203aff01d03ebe32dedd4bc09d4d3c98a8e0dda', 'Changed prior baseline')
    source_files = read(source / 'files.json')

    def old(name):
        require(sha(source / name) == source_files[name], 'Changed source member: ' + name)
        return read(source / name)

    scope = read(study / 'scope.json')
    original_scope = old('scope.json')
    require(scope['settings'] == original_scope['settings'] and scope['contentHashes'] == original_scope['contentHashes'], 'Changed combat scope')
    require(scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Wrong execution')
    for name, digest in q['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong captured runtime')
    for name, digest in scope['contentHashes'].items():
        require(all(sha(study / prefix / name) == digest for prefix in ('content/Data', 'study/content/Data', 'qualification/content/Data')), 'Wrong captured content')
    seeds = q['seeds']
    previous = old('request.json')
    require(len(seeds) == len(set(seeds)) == 32 and seeds == previous['seeds'], 'Changed historical schedule')
    source_cells = old('cells.json')
    profiles = read(study / 'profiles.json')
    require(profiles == read(Path(q['profiles']))['profiles'], 'Changed profiles')
    require(len(q['cases']) == len(previous['cases']) == 19, 'Missing cases')
    draft = read(study / 'budget.json')
    require(draft == read(Path(q['budget'])) and draft['equipmentCycle']['length'] == 10, 'Changed budget')
    require([(b['firstFloor'], b['lastFloor'], b['rarity'], b['quality'], b['rank']) for b in draft['equipmentCycle']['bands']] ==
            [(1, 3, 'Rare', 'Standard', 2), (4, 6, 'Epic', 'Fine', 3), (7, 9, 'Unique', 'Exceptional', 4), (10, 10, 'Legendary', 'Masterpiece', 5)], 'Not the requested curve')
    teams = Path(declaration['teamPreparation'])
    require(sha(teams / 'files.json') == declaration['teamPreparationManifestSha256'] == '7fd4df8ad67f6f49a1239049866f7758d2205e7efe78e48282a1d9b12964ddfc', 'Changed preparation packet')
    team_files = read(teams / 'files.json')
    for case, prior in zip(q['cases'], previous['cases'], strict=True):
        require(all(case[k] == prior[k] for k in ('id', 'purpose', 'sourceId', 'sourceContext')), 'Changed retained family')
        expected = copy.deepcopy(prior['scenario'])
        floor = expected['floorNumber']; position = (floor - 1) % 10 + 1
        band = next(b for b in draft['equipmentCycle']['bands'] if b['firstFloor'] <= position <= b['lastFloor'])
        require(case['budget'] == {**prior['budget'], 'rank': band['rank'], 'quality': band['quality']}, 'Changed level/tier/Essence budget')
        if case['purpose'] == 'intended-progression':
            require(case['budget'] == next(b for b in draft['budgets'] if b['priorityFloor'] == floor), 'Wrong intended budget')
        for member in expected['party']:
            build = member['build']; build.setdefault('identityEquipment', copy.deepcopy(build['equipment']))
            build.update(rank=band['rank'], quality=band['quality'])
            for item in build['equipment']:
                item['definitionId'] = item['definitionId'].rsplit('.rarity.', 1)[0] + '.rarity.' + band['rarity'].lower()
        expected['seeds'] = []
        expected['assumptions'] = [
            f"Equipment at cycle position {position}: {band['rarity']}, {band['quality']}, rank {band['rank']}. Level, tier, rolls and ordered Essences retain their source values. Hypothetical ownership; no styles.",
            'New equipment budget: prior strength claims do not transfer. Seed-free input; requires a separately declared evaluation schedule.',
            *['Source assumption (prior budget): ' + a for a in expected['assumptions']]]
        require(case['scenario'] == expected, 'Curve changed unrelated team properties')
        name = case['id'] + '.json'
        require(sha(teams / name) == team_files[name] and read(teams / name) == expected, 'Changed prepared team')
    cells = read(study / 'cells.json')
    require([(c['case'], c['profile']) for c in cells] == [(c['id'], p) for c in q['cases'] for p in ['baseline'] + [p['id'] for p in profiles]], 'Cell schedule differs')
    definitions = {a['id']: a for a in read(study / 'content/Data/equipment/equipment-starters.v4.json')['items']}
    for cell, prior_cell in zip(cells, source_cells, strict=True):
        case = next(c for c in q['cases'] if c['id'] == cell['case'])
        expected = copy.deepcopy(case['scenario']); expected['seeds'] = seeds
        require(all(cell[k] == prior_cell[k] for k in ('case', 'purpose', 'floor', 'essenceSlots', 'profile')), 'Changed paired cell metadata')
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
        require(cell['scenario'] == expected, 'Gear profile changed other party properties')
    require(read(study / 'qualification-cells.json') == source_cells[:7], 'Qualification recipes differ')
    require(sha(source / 'study/trials.jsonl') == source_files['study/trials.jsonl'], 'Changed prior ledger')
    old_trials = [json.loads(line) for line in (source / 'study/trials.jsonl').read_text().splitlines()]
    require(len(old_trials) == 4256, 'Wrong prior trial count')
    old_reports = []
    for trial in old_trials:
        name = 'study/battles/' + trial['id'] + '.json.gz'
        require(sha(source / name) == source_files[name], 'Changed prior report')
        old_reports.append(json.loads(gzip.decompress((source / name).read_bytes())))
    observations = {}
    for folder, expected_count in (('qualification', 224), ('study', 4256)):
        trials = [json.loads(line) for line in (study / folder / 'trials.jsonl').read_text().splitlines()]
        require(len(trials) == len({t['id'] for t in trials}) == expected_count, 'Wrong trial count')
        for ordinal, trial in enumerate(trials):
            cell = source_cells[ordinal // 32] if folder == 'qualification' else cells[ordinal // 32]
            seed, key = seeds[ordinal % 32], (cell['case'], cell['profile'])
            require(trial['id'] == f'trial-{ordinal+1:06}' and trial['stage'] == '/'.join(key) and trial['seed'] == seed, 'Trial schedule differs')
            require(read(study / folder / 'recipes' / (trial['recipe'] + '.json')) == cell['scenario'], 'Trial recipe differs')
            report = json.loads(gzip.decompress((study / folder / 'battles' / (trial['id'] + '.json.gz')).read_bytes()))
            require(report['battle']['seed'] == seed and report['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
            require(report['succeeded'] == (report['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome differs')
            prior_trial = old_trials[ordinal]
            require(prior_trial['stage'] == trial['stage'] and prior_trial['seed'] == seed, 'Prior pairing differs')
            if folder == 'qualification':
                require(trial['inputHash'] == prior_trial['inputHash'] and report == old_reports[ordinal], 'Runtime parity failed')
            else:
                observations.setdefault(key, []).append(report)
    result = read(study / 'result.json')
    require(result['status'] == 'EquipmentCycleScreenComplete' and result['fights'] == 4480 and result['screenFights'] == 4256 and result['runtimeParityReports'] == 224, 'Incomplete result')
    require(result['balanceAcceptance'] == 'NotAssessedHistoricalSeeds' and all(result[k] == 0 for k in ('newSeeds', 'confirmedTeams', 'searchRuns', 'retries')), 'Unexpected strength or accounting claim')
    for i, (row, cell) in enumerate(zip(result['rows'], cells, strict=True)):
        values = observations[(cell['case'], cell['profile'])]
        pairs = list(zip(values, observations[(cell['case'], 'baseline')], strict=True))
        prior = old_reports[i * 32:(i + 1) * 32]
        prior_pairs = list(zip(values, prior, strict=True))
        wins = sum(r['succeeded'] for r in values)
        expected = {k: cell[k] for k in ('case', 'purpose', 'floor', 'essenceSlots', 'profile')}
        expected.update(wins=wins, samples=32, draws=sum(r['battle']['summary']['contentOutcome'] == 'Draw' for r in values),
                        gainedWins=sum(c['succeeded'] and not b['succeeded'] for c, b in pairs), lostWins=sum(not c['succeeded'] and b['succeeded'] for c, b in pairs),
                        priorBudgetWins=sum(r['succeeded'] for r in prior), gainedFromPriorBudget=sum(c['succeeded'] and not b['succeeded'] for c, b in prior_pairs),
                        lostFromPriorBudget=sum(not c['succeeded'] and b['succeeded'] for c, b in prior_pairs),
                        observation='ObservedAboveCeiling' if wins > 16 else 'ObservedViable' if wins >= 4 else 'ObservedBelowMinimum')
        require(all(row[k] == v for k, v in expected.items()), 'Reconstructed row differs')
        require(math.isclose(row['meanGuardianHealth'], sum(r['guardianHealthRemainingPercent'] for r in values)/32, abs_tol=1e-6), 'Health mean differs')
        require(math.isclose(row['meanDurationSeconds'], sum(r['battle']['summary']['durationSeconds'] for r in values)/32, abs_tol=1e-6), 'Duration mean differs')
    audit = dict(status='Verified', authenticatedFiles=len(files), cases=19, cells=133, fights=4480, screenFights=4256,
                 runtimeParityReports=224, newFights=0, newSeeds=0, retainedFamilyAndGearPreserved=True,
                 allPriorBudgetPairsVerified=True, archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
