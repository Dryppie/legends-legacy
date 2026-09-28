"""Read-only reconstruction of a sealed gear coverage screen; runs zero fights."""
import argparse
import gzip
import hashlib
import json
import math
from pathlib import Path


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def member(root, name):
    path = (root / name).resolve()
    require(path.is_relative_to(root), 'Archive member escapes its root')
    return path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('study', 'owner', 'receipt'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args = parser.parse_args()
    study, owner, receipt = args.study.resolve(), args.owner.resolve(), args.receipt.resolve()
    require(not receipt.is_relative_to(study) and not receipt.exists(), 'Choose a new receipt outside the sealed study')
    require(sha(study / 'files.json') == args.manifest_pin, 'Changed archive manifest')
    manifest = read(study / 'files.json')
    actual = {p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()}
    require(actual == set(manifest) | {'files.json'}, 'Archive inventory differs')
    for name, digest in manifest.items():
        require(sha(member(study, name)) == digest, 'Changed archive file: ' + name)
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    require(q == read(study / 'request.json') and sha(owner / 'request.json') == declaration['requestSha256'], 'Request binding differs')
    process = read(owner / 'process.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Owner did not complete and drain')
    completion = read(owner / 'completion.json')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Completion binding differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=1344, completed=1344, retries=0), 'Wrong combat accounting')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 1345)), 'Attempt journal differs')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', cells=42, knownFloor15VariantMatched=True), 'Preflight missing')
    for name, digest in q['inputHashes'].items():
        require(sha(Path(name)) == digest, 'Changed pinned input: ' + name)
    source = Path(q['source'])
    require(sha(source / 'files.json') == q['sourcePin'], 'Changed source manifest')
    source_files = read(source / 'files.json')
    for name in ('scope.json', 'seed-ledger.json', 'variants.json'):
        require(sha(source / name) == source_files[name], 'Changed source member')
    scope = read(study / 'scope.json')
    old_scope = read(source / 'scope.json')
    require(scope['settings'] == old_scope['settings'] and scope['contentHashes'] == old_scope['contentHashes'], 'Content/settings drift')
    require(scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Execution binding differs')
    for name, digest in q['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong captured executable')
    for name, digest in scope['contentHashes'].items():
        require(sha(study / 'content/Data' / name) == sha(study / 'study/content/Data' / name) == digest, 'Wrong captured content')
    seeds = q['seeds']
    require(len(seeds) == len(set(seeds)) == 32 and set(seeds) <= set(read(source / 'seed-ledger.json')['historical']), 'Not the historical diagnostic seed panel')
    profiles = read(study / 'profiles.json')
    require(profiles == read(Path(q['profiles']))['profiles'], 'Changed profile definitions')
    cells = read(study / 'cells.json')
    require([(c['floor'], c['profile']) for c in cells] == [(c['floor'], p) for c in q['cases'] for p in ['baseline'] + [p['id'] for p in profiles]], 'Cell schedule differs')
    archetypes = {item['id']: item for item in read(study / 'content/Data/equipment/equipment-starters.v4.json')['items']}
    for cell in cells:
        original = next(c['scenario'] for c in q['cases'] if c['floor'] == cell['floor'])
        scenario = cell['scenario']
        profile = next((p for p in profiles if p['id'] == cell['profile']), None)
        require(scenario['seeds'] == seeds, 'Changed seed schedule')
        require({k: v for k, v in scenario.items() if k not in ('party', 'seeds')} == {k: v for k, v in original.items() if k not in ('party', 'seeds')}, 'Scenario metadata changed')
        for before, after in zip(original['party'], scenario['party'], strict=True):
            b, a = before['build'], after['build']
            require({k: v for k, v in before.items() if k != 'build'} == {k: v for k, v in after.items() if k != 'build'}, 'Party positions or doctrine changed')
            require({k: v for k, v in b.items() if k not in ('equipment', 'identityEquipment')} == {k: v for k, v in a.items() if k not in ('equipment', 'identityEquipment')}, 'Essences or nongear build fields changed')
            targeted = profile and (not profile['partySlots'] or before['partySlot'] in profile['partySlots'])
            require(a.get('identityEquipment') == (b.get('identityEquipment', b['equipment']) if targeted else b.get('identityEquipment')), 'Identity pin changed')
            for old, new in zip(b['equipment'], a['equipment'], strict=True):
                wanted = profile['specializations'].get(old['slot']) if targeted else None
                if wanted:
                    base, rarity = old['definitionId'].split('.rarity.')
                    base = base.split('.spec.')[0]
                    require(wanted in archetypes[base]['specializationIds'], 'Specialization outside the catalog')
                    require(new == {**old, 'definitionId': base + '.spec.' + wanted + '.rarity.' + rarity}, 'Gear changes more than specialization')
                else:
                    require(new == old, 'Unselected item changed')
    known = next(v['scenario'] for v in read(source / 'variants.json') if v['id'] == 'armor-and-health')
    require({**next(c['scenario'] for c in cells if c['floor'] == 15 and c['profile'] == 'armor-and-health'), 'seeds': []} == known, 'Known floor-15 benchmark drift')
    trials = [json.loads(line) for line in (study / 'study/trials.jsonl').read_text().splitlines()]
    require(len(trials) == 1344 and len({t['id'] for t in trials}) == 1344, 'Wrong raw trial count')
    observations = {}
    for ordinal, trial in enumerate(trials):
        cell, seed = cells[ordinal // 32], seeds[ordinal % 32]
        key = (cell['floor'], cell['profile'])
        require(trial['id'] == f'trial-{ordinal+1:06}' and trial['stage'] == f'{key[0]}/{key[1]}' and trial['seed'] == seed, 'Trial schedule differs')
        require(read(study / 'study/recipes' / (trial['recipe'] + '.json')) == cell['scenario'], 'Recipe differs')
        report = json.loads(gzip.decompress((study / 'study/battles' / (trial['id'] + '.json.gz')).read_bytes()))
        require(report['battle']['seed'] == seed and report['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
        require(report['succeeded'] == (report['battle']['summary']['contentOutcome'] == 'Victory'), 'Report outcome differs')
        observations.setdefault(key, []).append((report['succeeded'], report['guardianHealthRemainingPercent']))
    result = read(study / 'result.json')
    require(result['status'] == 'CoverageComplete' and result['fights'] == 1344 and all(result[k] == 0 for k in ('newSeeds', 'confirmedTeams', 'searchRuns', 'retries')), 'Wrong result accounting')
    require(len(result['rows']) == 42, 'Wrong row count')
    for row, cell in zip(result['rows'], cells, strict=True):
        values = observations[(cell['floor'], cell['profile'])]
        pairs = list(zip(values, observations[(cell['floor'], 'baseline')], strict=True))
        wins = sum(w for w, h in values)
        expected = dict(floor=cell['floor'], profile=cell['profile'], wins=wins, samples=32,
                        gainedWins=sum(c[0] and not b[0] for c, b in pairs), lostWins=sum(not c[0] and b[0] for c, b in pairs),
                        classification='LowWinReference' if wins < 4 else 'CeilingReference' if wins > 28 else 'FollowUpCandidate')
        require(all(row[k] == v for k, v in expected.items()), 'Reconstructed row differs')
        require(math.isclose(row['meanGuardianHealth'], sum(h for w, h in values)/32, abs_tol=1e-6), 'Health mean differs')
    eligible = [r['floor'] for r in result['rows'] if r['profile'] == 'armor-and-health' and r['classification'] == 'FollowUpCandidate']
    require(result['followUpFloors'] == eligible, 'Follow-up selection rule differs')
    audit = dict(status='Verified', authenticatedFiles=len(manifest), fights=1344, newFights=0, freshSeeds=0,
                 allEssenceRecipesPreserved=True, specializationsOnly=True, knownFloor15VariantMatched=True,
                 followUpFloors=eligible, archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
