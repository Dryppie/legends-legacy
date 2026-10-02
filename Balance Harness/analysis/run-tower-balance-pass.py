"""Bounded Tower-only preparation, composition/gear screen, supported search and confirmation.

Build first with build/run-tests.ps1. Every phase uses new output paths, immutable
inputs, disjoint reserved panels and archived production combat reports.
"""
import argparse
import gzip
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
from statistics import NormalDist
from types import SimpleNamespace
import sys
import time

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-balance-pass-v1'
BASE_LEDGER = ROOT / 'TestResults/tower-fifth-return-combat-owner-20260929/seed-ledger.json'
BASE_PIN = '5feb38e45e17328e9cac768139c7b9133fe20b0803a82fbab43d99912c9fd1cc'
EARNED = ROOT / 'TestResults/tower-fifth-return-combat-study-20260929'
EARNED_PIN = 'dd754b30a0ce0a26032fb0149d64984c2ef41b55f458212a0a4e2529d9adfd50'


def check(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write(path, value):
    with Path(path).open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')


def scaled_guardian(original, health_factor, offense_factor, penetration_factor=1):
    check(all(math.isfinite(x) and 0.25 <= x <= 16 for x in (health_factor, offense_factor)),
          'Health and offense factors must be finite and between 0.25 and 16')
    check(math.isfinite(penetration_factor) and 1 <= penetration_factor <= 128,
          'Penetration factor must be finite and between 1 and 128')
    result = {**original, 'health': round(original['health']*health_factor, 10),
              'offense': round(original['offense']*offense_factor, 10)}
    if penetration_factor != 1:
        check('penetration' in original and math.isfinite(original['penetration']) and original['penetration'] > 0,
              'A finite positive authored penetration multiplier is required')
        result['penetration'] = round(original['penetration']*penetration_factor, 10)
        check(math.isfinite(result['penetration']) and result['penetration'] > original['penetration'],
              'Penetration multiplier must increase finitely')
    return result


def validate_penetration_mode(mode, source, factor, health, offense, ability, imports, current, projections):
    if factor != 1:
        check(mode in ('screen', 'confirm') and source is not None and health == 1 and 0.25 <= offense < 1 and
              ability is None and not imports and not current and not projections,
              'Penetration requires a fixed source screen/confirm with lower offense and no other modifications')


def verify_penetration_candidate(source_api, candidate_api, plan):
    """Prove that only one floor's offense and penetration multipliers changed."""
    check(set(plan) == {'source', 'sourceManifestSha256', 'floor', 'offenseFactor', 'penetrationFactor'},
          'Unexpected penetration plan fields')
    validate_penetration_mode('screen', plan['source'], plan['penetrationFactor'], 1,
                              plan['offenseFactor'], None, [], False, [])
    check(plan['penetrationFactor'] > 1, 'Penetration plan must change penetration')
    before = read(source_api / 'Data/world-tower/tower-floors.json')
    floors = [f for f in before['floors'] if f['floorNumber'] == plan['floor']]
    check(len(floors) == 1, 'Exactly one target floor required')
    floors[0]['guardianScaling'] = scaled_guardian(floors[0]['guardianScaling'], 1,
                                                  plan['offenseFactor'], plan['penetrationFactor'])
    check(read(candidate_api / 'Data/world-tower/tower-floors.json') == before,
          'Penetration candidate changed unrelated Tower content')
    source_files = {p.relative_to(source_api / 'Data').as_posix(): sha(p) for p in (source_api / 'Data').rglob('*.json')}
    candidate_files = {p.relative_to(candidate_api / 'Data').as_posix(): sha(p) for p in (candidate_api / 'Data').rglob('*.json')}
    check(source_files.keys() == candidate_files.keys(), 'Penetration candidate catalog set changed')
    check(all(candidate_files[p] == h for p, h in source_files.items() if p != 'world-tower/tower-floors.json'),
          'Penetration candidate changed another catalog')
    check(sha(source_api / 'appsettings.json') == sha(candidate_api / 'appsettings.json'),
          'Penetration candidate changed settings')
    return dict(floor=plan['floor'], offenseFactor=plan['offenseFactor'], penetrationFactor=plan['penetrationFactor'])


def authenticate(root):
    files = read(root / 'files.json')
    for name, pin in files.items():
        check(sha(root / name) == pin, 'Changed source member: ' + name)
    return files


def append_references(cells, evaluated, source):
    """Keep exact recipes, including identity fields; merge only identical scenarios."""
    references = [c for c in evaluated if c['origin'] == 'retained-reference']
    check(len(references) == 3, 'Expected all three saved search references')
    check(all(c['origin'] in ('retained-reference', 'generated-finalist') for c in evaluated),
          'Unknown search nominee origin')
    def key(cell):
        return json.dumps(cell['scenario'], sort_keys=True, separators=(',', ':'))
    existing = {key(c): c['id'] for c in cells}
    coverage = []
    for cell in references:
        scenario = cell['scenario']
        check(scenario['floorNumber'] == cells[0]['scenario']['floorNumber'] and not scenario['seeds'],
              'Reference must be seed-free and from the same floor')
        signature = key(cell)
        added = signature not in existing
        if added:
            clone = json.loads(json.dumps(cell))
            clone.update(id='projected-reference/' + hashlib.sha256(signature.encode()).hexdigest(),
                         origin='projected-reference')
            check(all(c['id'] != clone['id'] for c in cells), 'Reference identifier collision')
            cells.append(clone)
            existing[signature] = clone['id']
        coverage.append(dict(source=str(source), sourceId=cell['id'], cellId=existing[signature], added=added))
    return coverage


def composition_key(cell):
    """Count actual slot builds, ignoring labels, identities and Essence ordering."""
    return tuple(sorted((m['partySlot'], tuple(sorted(m['build']['essenceIds'])))
                        for m in cell['scenario']['party']))


def append_gear_reference(cells, source_id, template_id):
    """Project one saved composition onto saved equipment, preserving its raw build."""
    lookup = {c['id']: c for c in cells}
    check(len(lookup) == len(cells) and source_id in lookup and template_id in lookup,
          'Unique saved source and equipment template required')
    source, template = lookup[source_id], lookup[template_id]
    check(source['scenario']['floorNumber'] == template['scenario']['floorNumber'] and
          not source['scenario']['seeds'] and not template['scenario']['seeds'],
          'Equipment projection requires seed-free recipes on the same floor')
    clone = json.loads(json.dumps(source))
    party, gear_party = clone['scenario']['party'], template['scenario']['party']
    check(len(party) == len(gear_party), 'Equipment projection cannot change party size')
    for member, equipment_member in zip(party, gear_party, strict=True):
        build, gear_build = member['build'], equipment_member['build']
        check(member['partySlot'] == equipment_member['partySlot'] and
              all(build[k] == gear_build[k] for k in ('characterLevel', 'tier', 'attributeRollMultiplier', 'rank', 'quality')),
              'Equipment projection cannot change positions or budgets')
        build['equipment'] = json.loads(json.dumps(gear_build['equipment']))
    signature = json.dumps(clone['scenario'], sort_keys=True, separators=(',', ':'))
    existing = next((c for c in cells if c['scenario'] == clone['scenario']), None)
    if existing is None:
        clone.update(id='gear-reference/' + hashlib.sha256(signature.encode()).hexdigest(),
                     gear=template['gear'], origin='gear-reference-projection')
        check(clone['id'] not in lookup, 'Equipment reference identifier collision')
        cells.append(clone)
    else:
        check(existing['gear'] == template['gear'], 'Existing exact recipe has a different gear label')
    return dict(sourceId=source_id, templateId=template_id, cellId=(existing or clone)['id'], added=existing is None)


def append_fixed_support_reference(cells, source_id, donor_id, party_slot):
    """Copy only a saved slot's ordered Essences; retain raw identities and all controls."""
    lookup = {c['id']: c for c in cells}
    check(len(lookup) == len(cells) and source_id in lookup and donor_id in lookup,
          'Unique saved source and Essence donor required')
    source, donor = lookup[source_id], lookup[donor_id]
    check(source['scenario']['floorNumber'] == donor['scenario']['floorNumber'] == cells[0]['scenario']['floorNumber']
          and not source['scenario']['seeds'] and not donor['scenario']['seeds'],
          'Essence projection requires seed-free recipes on the same floor')
    party, donor_party = source['scenario']['party'], donor['scenario']['party']
    slots = [m['partySlot'] for m in party]
    donor_slots = [m['partySlot'] for m in donor_party]
    check(type(party_slot) is int and party_slot > 0 and party_slot in slots and
          all(type(s) is int and s > 0 for s in slots + donor_slots) and len(slots) == len(set(slots)) and
          slots == donor_slots,
          'Essence projection requires unique matching party positions and a saved slot')
    index = slots.index(party_slot)
    build, donor_build = party[index]['build'], donor_party[index]['build']
    budget = ('characterLevel', 'tier', 'attributeRollMultiplier', 'rank', 'quality')
    check(all(k in build and k in donor_build and build[k] == donor_build[k] for k in budget),
          'Essence projection cannot change slot budgets')
    old, new = build['essenceIds'], donor_build['essenceIds']
    check(isinstance(old, list) and isinstance(new, list) and old and len(old) == len(new) and
          all(isinstance(e, str) and e for e in old + new) and len(set(old)) == len(old) and len(set(new)) == len(new),
          'Essence projection requires equal nonempty unique Essence counts')
    check(set(old) != set(new), 'Essence projection must change actual Essences, not their order')
    clone = json.loads(json.dumps(source))
    clone['scenario']['party'][index]['build']['essenceIds'] = list(new)
    signature = json.dumps(clone['scenario'], sort_keys=True, separators=(',', ':'))
    existing = next((c for c in cells if c['scenario'] == clone['scenario']), None)
    if existing is None:
        # These stable identifiers retain the already frozen fixed-support proposal.
        clone.update(id='fixed-support/' + hashlib.sha256(signature.encode()).hexdigest(),
                     composition='fixed-support/' + qualification_module().signature(composition_key(clone)),
                     origin='proposed-fixed-support-projection')
        check(clone['id'] not in lookup, 'Essence reference identifier collision')
        cells.append(clone)
    else:
        check(existing['gear'] == source['gear'], 'Existing exact recipe has a different gear label')
    return dict(sourceId=source_id, donorId=donor_id, partySlot=party_slot, cellId=(existing or clone)['id'],
                added=existing is None, replacedEssenceIds=list(old), newEssenceIds=list(new))


def select_search_references(cells, rows, gear=None):
    lookup = {c['id']: c for c in cells}
    check(len(lookup) == len(cells) and len({r['id'] for r in rows}) == len(rows)
          and set(lookup) == {r['id'] for r in rows}, 'Measured cells and rows must match uniquely')
    check(all(r['gear'] == lookup[r['id']]['gear'] for r in rows), 'Measured gear differs from recipe')
    ranked = sorted(rows, key=lambda r: (-r['wins'], r['meanGuardianHealth'], r['id']))
    check(bool(ranked), 'Measured references required')
    selected_gear = gear if gear is not None else ranked[0]['gear']
    chosen, seen = [], set()
    for row in ranked:
        if row['gear'] != selected_gear:
            continue
        cell = lookup[row['id']]
        key = composition_key(cell)
        if key not in seen:
            seen.add(key)
            chosen.append(cell)
        if len(chosen) == 3:
            break
    check(len(chosen) == 3, 'Three distinct measured reference compositions required at selected gear')
    return chosen, 1


def append_search_finalists(cells, evaluated, source):
    """Preserve exact nominees plus gear projections; deduplicate only exact recipes."""
    finalists = [c for c in evaluated if c['origin'] == 'generated-finalist']
    check(len(finalists) == 2, 'Expected both generated search finalists')
    templates = [c for c in cells if c['composition'] == cells[0]['composition']]
    profiles = {c['gear']: c for c in templates}
    check(len(profiles) == len(templates), 'Ambiguous frozen gear templates')
    def key(cell):
        return json.dumps(cell['scenario'], sort_keys=True, separators=(',', ':'))
    existing = {key(c): c['id'] for c in cells}
    coverage = []
    def append(cell, source_id, kind):
        scenario = cell['scenario']
        check(scenario['floorNumber'] == cells[0]['scenario']['floorNumber'] and not scenario['seeds'],
              'Finalist must be seed-free and from the same floor')
        signature = key(cell)
        added = signature not in existing
        if added:
            clone = json.loads(json.dumps(cell))
            clone['id'] = 'generated-finalist/' + hashlib.sha256(signature.encode()).hexdigest()
            check(all(c['id'] != clone['id'] for c in cells), 'Finalist identifier collision')
            cells.append(clone)
            existing[signature] = clone['id']
        coverage.append(dict(source=str(source), sourceId=source_id, cellId=existing[signature],
                             kind=kind, gear=cell['gear'], added=added))
    for cell in finalists:
        append(cell, cell['id'], 'exact-finalist')
        for gear, template in profiles.items():
            clone = json.loads(json.dumps(cell))
            clone['gear'] = gear
            for member, gear_member in zip(clone['scenario']['party'], template['scenario']['party'], strict=True):
                build, frozen = member['build'], gear_member['build']
                check(member['partySlot'] == gear_member['partySlot'] and
                      all(build[k] == frozen[k] for k in ('characterLevel', 'tier', 'attributeRollMultiplier')),
                      'Gear projection cannot change positions, level, tier or rolls')
                for field in ('equipment', 'rank', 'quality'):
                    build[field] = frozen[field]
            append(clone, cell['id'], 'gear-projection')
    return coverage


def validate_current_content(source, api, floor):
    """Refresh other floors without silently changing the studied floor or catalogs."""
    for path in (source / 'content/Data').rglob('*.json'):
        relative = path.relative_to(source / 'content/Data')
        current = api / 'Data' / relative
        if relative.as_posix() == 'world-tower/tower-floors.json':
            before, after = read(path), read(current)
            check({k: v for k, v in before.items() if k != 'floors'} ==
                  {k: v for k, v in after.items() if k != 'floors'}, 'Tower metadata changed')
            check(next(f for f in before['floors'] if f['floorNumber'] == floor) ==
                  next(f for f in after['floors'] if f['floorNumber'] == floor), 'Target floor changed')
        else:
            check(sha(path) == sha(current), 'Current catalog differs: ' + str(relative))


def history():
    check(sha(BASE_LEDGER) == BASE_PIN, 'Changed predecessor ledger')
    ledger = read(BASE_LEDGER)
    pins = {str(BASE_LEDGER): BASE_PIN}
    def linked(entry):
        path = Path(entry['archive'])
        check(sha(path) == entry['sha256'], 'Changed seed ancestor')
        pins[str(path)] = entry['sha256']
        return read(path)
    original, dungeon = linked(ledger['historical']), linked(ledger['dungeon'])
    values = set(original['historical'] + original['first'] + original['second'] + dungeon['reserved'] + ledger['reserved'])
    for entry in ledger['preceding']:
        values.update(linked(entry)['reserved'])
    check(len(values) == 885164, 'Incomplete inherited exclusions')
    for path in sorted((ROOT / 'TestResults').glob('tower-balance-pass-*-owner-*/seed-ledger.json')):
        pins[str(path)] = sha(path)
        new = read(path)['reserved']
        check(not values.intersection(new), 'Overlapping Tower balance panels')
        values.update(new)
    return values, pins


def audit(output):
    files = authenticate(output)
    q, result = read(output / 'request.json'), read(output / 'result.json')
    diagnostic_paths = [p for p in q['inputHashes'] if Path(p).name in
        ('eight-item-diagnostic-provenance.json', 'kodoku-offense-diagnostic-provenance.json', 'kodoku-offense-refinement-diagnostic-provenance.json', 'kodoku-midpoint-diagnostic-provenance.json', 'ni-restoration-diagnostic-provenance.json', 'ni-restoration-offense-diagnostic-provenance.json')]
    check(bool(diagnostic_paths) == bool(q.get('diagnosticVersion')) and len(diagnostic_paths) <= 1, 'Diagnostic marker/provenance mismatch')
    diagnostic_module(q.get('diagnosticVersion')).audit_request(SimpleNamespace(authenticate=authenticate, ability_module=ability_module), output, q)
    ni_family = [Path(p) for p in q['inputHashes'] if Path(p).name == 'ni-restoration-family-provenance.json']
    check(len(ni_family) <= 1, 'Ambiguous Ni family provenance')
    if ni_family:
        p = read(ni_family[0]); source = Path(p['source']); proposal = Path(p['proposal'])
        check(q['mode'] == 'prepare' and sha(ni_family[0]) == q['inputHashes'][str(ni_family[0])] and
              sha(source/'files.json') == p['sourceManifestSha256'] and sha(proposal) == p['proposalSha256'], 'Changed Ni preparation provenance')
        authenticate(source)
        check(read(output/'cells.json') == ni_restoration_module().admit(proposal, source), 'Ni preparation changed raw recipes or controls')
    eight_provenance = [Path(p) for p in q['inputHashes'] if Path(p).name == 'eight-item-family-provenance.json']
    check(len(eight_provenance) <= 1, 'Ambiguous eight-item family provenance')
    if eight_provenance:
        path = eight_provenance[0]; p = read(path); source = Path(p['source']); proposal = Path(p['proposal'])
        check(q['mode'] == 'prepare' and sha(path) == q['inputHashes'][str(path)] and sha(proposal) == p['proposalSha256']
              and sha(source/'files.json') == p['sourceManifestSha256'], 'Changed eight-item preparation provenance')
        authenticate(source)
        check(read(output/'cells.json') == eight_item_module().admit(proposal, source), 'Eight-item preparation changed recipes or controls')
    family_provenance = [Path(p) for p in q['inputHashes'] if Path(p).name == 'one-healer-family-provenance.json']
    check(len(family_provenance) <= 1, 'Ambiguous one-healer family provenance')
    if family_provenance:
        path = family_provenance[0]
        check(q['mode'] == 'prepare' and sha(path) == q['inputHashes'][str(path)], 'Changed or non-preparation family provenance')
        p = read(path); source = Path(p['source']); proposal = Path(p['proposal'])
        check(sha(proposal) == p['proposalSha256'] and sha(source/'files.json') == p['sourceManifestSha256'], 'Changed family source')
        authenticate(source)
        check(read(output/'cells.json') == one_healer_module().admit(proposal, source), 'One-healer preparation lost controls or changed recipes')
    support_provenance = [Path(path) for path in q['inputHashes'] if Path(path).name == 'fixed-support-provenance.json']
    check(len(support_provenance) <= 1, 'Ambiguous fixed support provenance')
    if support_provenance:
        provenance_path = support_provenance[0]
        check(q['mode'] == 'prepare' and sha(provenance_path) == q['inputHashes'][str(provenance_path)],
              'Changed fixed support provenance or non-preparation mode')
        provenance = read(provenance_path)
        source = Path(provenance['source'])
        check(sha(source / 'files.json') == provenance['sourceManifestSha256'], 'Changed fixed support source')
        authenticate(source)
        expected = read(source / ('evaluation-cells.json' if (source / 'evaluation-cells.json').exists() else 'cells.json'))
        saved_ids = {c['id'] for c in expected}
        for entry in provenance['projections']:
            check(entry['sourceId'] in saved_ids and entry['donorId'] in saved_ids, 'Chained support projection')
            check(append_fixed_support_reference(expected, entry['sourceId'], entry['donorId'], entry['partySlot']) == entry,
                  'Fixed support projection receipt differs')
        check(read(output / 'cells.json') == expected, 'Fixed support family lost controls or changed recipes')
        check(read(output / 'scope.json')['contentHashes'] == read(source / 'scope.json')['contentHashes'] and
              read(output / 'scope.json')['settings'] == read(source / 'scope.json')['settings'],
              'Fixed support projection changed catalogs or combat settings')
    ability_provenance = [Path(path) for path in q['inputHashes']
                          if Path(path).name == 'ability-candidate-provenance.json']
    check(len(ability_provenance) <= 1, 'Ambiguous ability candidate provenance')
    ability_verification = None
    if ability_provenance:
        provenance_path = ability_provenance[0]
        check(sha(provenance_path) == q['inputHashes'][str(provenance_path)], 'Changed ability provenance')
        provenance = read(provenance_path)
        source = Path(provenance['source'])
        check(sha(source / 'files.json') == provenance['sourceManifestSha256'], 'Changed ability source')
        authenticate(source)
        check(read(output / 'cells.json') == read(source / 'cells.json'), 'Ability trial changed its recipe family')
        check(read(output / 'scope.json')['settings'] == read(source / 'scope.json')['settings'],
              'Ability trial changed combat settings')
        if provenance['plan'].get('version') == 'tower-ni-restoration-offense-v1':
            check(q.get('diagnosticVersion') == 'floor9-restoration-offense-diagnostic-v1', 'Ni offense calibration is diagnostic only')
        ability_verification = ability_module().verify(source / 'content', output / 'content', provenance['plan'], q['floor'])
    penetration_provenance = [Path(path) for path in q['inputHashes']
                              if Path(path).name == 'penetration-candidate-provenance.json']
    check(len(penetration_provenance) <= 1 and not (penetration_provenance and ability_provenance),
          'Ambiguous or mixed penetration provenance')
    penetration_verification = None
    if penetration_provenance:
        path = penetration_provenance[0]
        check(q['mode'] in ('screen', 'confirm') and sha(path) == q['inputHashes'][str(path)],
              'Changed penetration provenance or invalid mode')
        plan = read(path); source = Path(plan['source'])
        check(plan['floor'] == q['floor'] and sha(source / 'files.json') == plan['sourceManifestSha256'],
              'Changed penetration source or floor')
        authenticate(source)
        check(read(output / 'cells.json') == read(source / 'cells.json'), 'Penetration trial changed its recipe family')
        check(read(output / 'scope.json')['settings'] == read(source / 'scope.json')['settings'],
              'Penetration trial changed combat settings')
        penetration_verification = verify_penetration_candidate(source / 'content', output / 'content', plan)
    health_provenance = [Path(path) for path in q['inputHashes']
                         if Path(path).name == 'health-pressure-candidate-provenance.json']
    check(len(health_provenance) <= 1 and not (health_provenance and (ability_provenance or penetration_provenance)),
          'Ambiguous or mixed health pressure provenance')
    health_verification = None
    if health_provenance:
        path = health_provenance[0]
        check(q['mode'] in ('screen', 'confirm') and sha(path) == q['inputHashes'][str(path)], 'Changed health pressure provenance')
        provenance = read(path); source = Path(provenance['source'])
        check(sha(source / 'files.json') == provenance['sourceManifestSha256'], 'Changed health pressure source')
        authenticate(source)
        check(not any(Path(p).name in ('ability-candidate-provenance.json', 'penetration-candidate-provenance.json',
                  'health-pressure-candidate-provenance.json', 'recovery-pressure-candidate-provenance.json') for p in read(source / 'request.json')['inputHashes']),
              'Chained health pressure candidate')
        check(read(output / 'cells.json') == read(source / 'cells.json'), 'Health pressure trial changed family')
        check(read(output / 'scope.json')['settings'] == read(source / 'scope.json')['settings'], 'Combat settings changed')
        health_verification = health_pressure_module().verify(source / 'content', output / 'content', provenance['plan'], q['floor'])
    recovery_provenance = [Path(path) for path in q['inputHashes']
                           if Path(path).name == 'recovery-pressure-candidate-provenance.json']
    check(len(recovery_provenance) <= 1 and not (recovery_provenance and (ability_provenance or penetration_provenance or health_provenance)),
          'Ambiguous or mixed recovery pressure provenance')
    recovery_verification = None
    if recovery_provenance:
        path = recovery_provenance[0]
        check(q['mode'] in ('screen', 'confirm') and sha(path) == q['inputHashes'][str(path)], 'Changed recovery pressure provenance')
        provenance = read(path); source = Path(provenance['source'])
        check(sha(source / 'files.json') == provenance['sourceManifestSha256'], 'Changed recovery pressure source')
        authenticate(source)
        check(not any(Path(p).name in ('ability-candidate-provenance.json', 'penetration-candidate-provenance.json',
                  'health-pressure-candidate-provenance.json', 'recovery-pressure-candidate-provenance.json')
                  for p in read(source / 'request.json')['inputHashes']), 'Chained recovery pressure candidate')
        check(read(output / 'cells.json') == read(source / 'cells.json'), 'Recovery pressure trial changed family')
        check(read(output / 'scope.json')['settings'] == read(source / 'scope.json')['settings'], 'Combat settings changed')
        recovery_verification = recovery_pressure_module(provenance['plan']['version']).verify(source / 'content', output / 'content', provenance['plan'], q['floor'])
    check(read(output / 'completion.json')['status'] == 'Complete', 'Incomplete native operation')
    if q['mode'] == 'prepare':
        check(result['status'] == 'PreparedNoFights' and result['fights'] == 0, 'Preparation ran fights')
        return dict(status='VerifiedPreparation', files=len(files), fights=0)
    cells = read(output / ('evaluation-cells.json' if q['mode'] == 'search' else 'cells.json'))
    trials = [json.loads(line) for line in (output / 'evaluation/trials.jsonl').read_text().splitlines()]
    check(len(trials) == len(cells) * len(q['seeds']), 'Wrong evaluation count')
    for i, (cell, row) in enumerate(zip(cells, result['rows'], strict=True)):
        wins, health, seconds = 0, 0, 0
        for j, seed in enumerate(q['seeds']):
            t = trials[i * len(q['seeds']) + j]
            check(t['stage'] == cell['id'] and t['seed'] == seed, 'Changed schedule')
            recipe = read(output / 'evaluation/recipes' / (t['recipe'] + '.json'))
            check(recipe == {**cell['scenario'], 'seeds': q['seeds']}, 'Changed composition or budget')
            raw = json.loads(gzip.decompress((output / 'evaluation/battles' / (t['id'] + '.json.gz')).read_bytes()))
            check(raw['battle']['seed'] == seed, 'Changed battle seed')
            check(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Inconsistent victory')
            wins += raw['succeeded']; health += raw['guardianHealthRemainingPercent']; seconds += raw['displayDurationSeconds']
        n = len(q['seeds'])
        check(row['id'] == cell['id'] and row['wins'] == wins and row['samples'] == n, 'Wrong reported wins')
        check(abs(row['meanGuardianHealth'] - health/n) < 1e-6 and abs(row['meanSeconds'] - seconds/n) < 1e-6, 'Wrong means')
    check(result['fights'] == q['maximumFights'] == read(output / 'completion.json')['completed'], 'Wrong fight accounting')
    verified = dict(status='Verified', files=len(files), fights=result['fights'], evaluationFights=len(trials),
                    nativeSearchReconstruction=q['mode'] == 'search', resultSha256=sha(output / 'result.json'))
    if ability_verification is not None:
        verified['abilityCandidate'] = ability_verification
    if penetration_verification is not None:
        verified['penetrationCandidate'] = penetration_verification
    if health_verification is not None:
        verified['healthPressureCandidate'] = health_verification
    if recovery_verification is not None:
        verified['recoveryPressureCandidate'] = recovery_verification
    if q['mode'] == 'confirm':
        z = NormalDist().inv_cdf(1-0.05/(2*len(cells)))
        bounds = []
        for row in result['rows']:
            n, proportion = row['samples'], row['wins']/row['samples']
            denominator = 1+z*z/n
            center = (proportion+z*z/(2*n))/denominator
            radius = z*((proportion*(1-proportion)/n+z*z/(4*n*n))**0.5)/denominator
            bounds.append(dict(id=row['id'], wins=row['wins'], samples=n, lower=max(0, center-radius), upper=min(1, center+radius)))
        verdict = 'Pass' if max(b['upper'] for b in bounds) <= 0.5 and max(b['lower'] for b in bounds) >= 0.1 else (
            'Fail' if any(b['wins']/b['samples'] > 0.5 for b in bounds) or max(b['upper'] for b in bounds) < 0.1 else 'Inconclusive')
        verified['assessment'] = dict(verdict=verdict, familySize=len(cells), alpha=0.05,
            method='approximate Bonferroni-Wilson', bounds=bounds)
    return verified


def ability_module():
    spec = importlib.util.spec_from_file_location('tower_ability_candidate', Path(__file__).with_name('tower-ability-candidate.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def health_pressure_module():
    spec = importlib.util.spec_from_file_location('tower_health_pressure_candidate', Path(__file__).with_name('tower-health-pressure-candidate.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def recovery_pressure_module(version='tower-recovery-pressure-candidate-v1'):
    helpers = {'tower-recovery-pressure-candidate-v1': 'tower-recovery-pressure-candidate.py',
               'tower-recovery-pressure-refinement-v1': 'tower-recovery-pressure-refinement.py'}
    check(version in helpers, 'Unknown recovery candidate version')
    spec = importlib.util.spec_from_file_location('tower_recovery_pressure_candidate', Path(__file__).with_name(helpers[version]))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def one_healer_module():
    spec = importlib.util.spec_from_file_location('tower_one_healer', Path(__file__).with_name('tower-one-healer-family.py'))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def eight_item_module():
    spec = importlib.util.spec_from_file_location('tower_eight_item', Path(__file__).with_name('tower-eight-item-diagnostic.py'))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def ni_restoration_module():
    spec = importlib.util.spec_from_file_location('tower_ni_restoration', Path(__file__).with_name('tower-ni-limited-restoration.py'))
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def diagnostic_module(version=None):
    if version is None or version == 'floor8-two-healer-eight-item-diagnostic-v1':
        return eight_item_module()
    if version == 'floor9-limited-restoration-diagnostic-v1':
        return ni_restoration_module()
    modules = {'floor9-restoration-offense-diagnostic-v1': 'tower-ni-restoration-offense-diagnostic.py',
               'floor8-kodoku-offense-diagnostic-v1': 'tower-kodoku-offense-diagnostic.py',
               'floor8-kodoku-offense-refinement-diagnostic-v1': 'tower-kodoku-offense-refinement-diagnostic.py',
               'floor8-kodoku-midpoint-diagnostic-v1': 'tower-kodoku-midpoint-diagnostic.py'}
    check(version in modules, 'Unknown diagnostic version')
    spec = importlib.util.spec_from_file_location('tower_offense_diagnostic', Path(__file__).with_name(modules[version]))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def qualification_module():
    spec = importlib.util.spec_from_file_location('tower_catalog_qualification', Path(__file__).with_name('tower-catalog-qualification.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def validate_ability_mode(mode, source, candidate, health, offense, imports, current):
    if candidate is not None:
        check(mode in ('screen', 'confirm') and source is not None and health == offense == 1 and not imports and not current,
              'Ability candidates require a fixed source family, screen/confirm mode and no other modifications')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--mode', choices=['prepare', 'screen', 'search', 'confirm'], required=True)
    p.add_argument('--name', required=True, help='Unique phase identifier')
    p.add_argument('--artifacts', type=Path, required=True)
    p.add_argument('--source', type=Path)
    p.add_argument('--search-gear', help='Search at this measured gear profile; default selects the strongest measured cell')
    p.add_argument('--samples', type=int, default=32)
    p.add_argument('--floor', type=int, default=5)
    p.add_argument('--native-seconds', type=int, choices=[840, 1140], default=840,
                   help='1140 requires a separately declared floor-15 confirmation; process allowance adds 60 seconds')
    p.add_argument('--add-search', type=Path, action='append', default=[], help='Add generated finalists at every frozen gear profile; repeat for multiple completed searches')
    p.add_argument('--retain-search-references', action='store_true', help='Also retain exact projected reference recipes at their evaluated profile')
    p.add_argument('--add-references', type=Path, action='append', default=[],
                   help='Import only the three exact saved references; retain the existing family unchanged')
    p.add_argument('--current-content', action='store_true',
                   help='Seed-free preparation from saved cells and current content; target floor and catalogs must match')
    p.add_argument('--health-factor', type=float, default=1.0, help='Multiply boss health in an isolated content copy only')
    p.add_argument('--offense-factor', type=float, default=1.0, help='Multiply boss offense in the same isolated copy')
    p.add_argument('--penetration-factor', type=float, default=1.0,
                   help='Fixed-family trial: increase both penetration multipliers (1-128), paired with lower offense only')
    p.add_argument('--ability-candidate', type=Path,
                   help='Explicit damage-coefficient plan from an unchanged source; no scalar or family changes')
    p.add_argument('--recovery-pressure-candidate', type=Path, help='Fixed floor-7 health pressure plus recovery candidate')
    p.add_argument('--health-pressure-candidate', type=Path,
                   help='Explicit target-MaxHealth magical base with reduced offense and increased penetration; fixed family only')
    p.add_argument('--qualified-family', type=Path,
                   help='Seed-free preparation with pinned accepted-catalog replay qualification and an exact supported gear proposal')
    p.add_argument('--one-healer-family', type=Path, help='Seed-free exact six-variant floor-eight extension from the unchanged original source')
    p.add_argument('--eight-item-family', type=Path, help='Seed-free exact three-variant two-healer extension')
    p.add_argument('--ni-restoration-family', type=Path, help='Seed-free exact fifteen-variant floor-nine Restoration extension')
    p.add_argument('--diagnostic-contract', type=Path, help='Pinned versioned diagnostic; never acceptance')
    p.add_argument('--diagnostic-sha256')
    p.add_argument('--diagnostic-batch', type=int, choices=[0, 1, 2, 3, 4, 5])
    p.add_argument('--gear-reference', nargs=2, metavar=('SOURCE_ID', 'TEMPLATE_ID'),
                   help='Seed-free preparation: retain the family and copy saved template equipment onto one saved composition')
    p.add_argument('--fixed-support-reference', nargs=3, action='append', default=[],
                   metavar=('SOURCE_ID', 'DONOR_ID', 'PARTY_SLOT'),
                   help='Seed-free preparation: copy only a saved donor slot\'s ordered Essences; repeat for declared recipes')
    a = p.parse_args()
    if a.ni_restoration_family:
        ni_restoration_module().validate_mode(a.mode, a.floor, a.source, [a.current_content, a.qualified_family,
            a.add_search, a.add_references, a.gear_reference, a.fixed_support_reference, a.ability_candidate,
            a.health_pressure_candidate, a.recovery_pressure_candidate, a.health_factor != 1,
            a.offense_factor != 1, a.penetration_factor != 1, a.search_gear, a.retain_search_references,
            a.one_healer_family, a.eight_item_family, a.diagnostic_contract])
    if a.one_healer_family:
        one_healer_module().validate_mode(a.mode, a.floor, a.source, [a.current_content, a.qualified_family,
            a.add_search, a.add_references, a.gear_reference, a.fixed_support_reference, a.ability_candidate,
            a.health_pressure_candidate, a.recovery_pressure_candidate, a.health_factor != 1,
            a.offense_factor != 1, a.penetration_factor != 1, a.search_gear, a.retain_search_references,
            a.eight_item_family, a.diagnostic_contract])
    if a.eight_item_family:
        eight_item_module().validate_mode(a.mode, a.floor, a.source, [a.current_content, a.qualified_family,
            a.add_search, a.add_references, a.gear_reference, a.fixed_support_reference, a.ability_candidate,
            a.health_pressure_candidate, a.recovery_pressure_candidate, a.health_factor != 1,
            a.offense_factor != 1, a.penetration_factor != 1, a.search_gear, a.retain_search_references,
            a.one_healer_family, a.diagnostic_contract])
    check(bool(a.diagnostic_contract) == bool(a.diagnostic_sha256) == (a.diagnostic_batch is not None), 'Complete diagnostic binding required')
    check(a.name.replace('-', '').isalnum(), 'Unsafe phase name')
    if not a.diagnostic_contract:
        eight_item_module().validate_panel(a.mode, a.floor, a.samples, None)
    check(a.search_gear is None or a.mode == 'search', 'Search gear is only valid in search mode')
    check(not a.retain_search_references or a.add_search, 'Reference retention requires a completed search')
    check(not a.current_content or a.mode == 'prepare' and a.source is not None,
          'Current-content refresh requires seed-free preparation from a completed source')
    check(not a.fixed_support_reference or a.mode == 'prepare' and a.source is not None and not a.current_content and
          not a.gear_reference and not a.qualified_family and not a.add_search and not a.add_references and
          a.ability_candidate is None and a.health_factor == a.offense_factor == 1,
          'Fixed support requires seed-free preparation from a saved family with no other modifications')
    check(not a.gear_reference or a.mode == 'prepare' and a.source is not None and not a.current_content and
          not a.qualified_family and not a.add_search and not a.add_references and a.ability_candidate is None and
          a.health_factor == a.offense_factor == 1,
          'Gear reference requires seed-free preparation from a saved family with no other modifications')
    check(not a.qualified_family or a.mode == 'prepare' and a.source is not None and not a.current_content and
          not a.add_search and not a.add_references and a.ability_candidate is None and
          a.health_factor == a.offense_factor == 1 and a.floor in (2, 4, 6, 7, 8, 9, 10, 12),
          'Qualified family requires floor-2, floor-4, floor-6, floor-7, floor-8, floor-9, floor-10 or floor-12 seed-free preparation with no other modifications')
    check(not a.add_references or a.mode in ('prepare', 'screen', 'confirm') and a.source is not None,
          'Reference-only import requires a saved family and a fixed-family mode')
    check(a.native_seconds == 840 or a.mode == 'confirm' and a.floor == 15,
          'Larger predeclared envelope is only for floor-15 confirmation')
    scaled_guardian({'health': 1, 'offense': 1}, a.health_factor, a.offense_factor)
    scaled_guardian({'health': 1, 'offense': 1, 'penetration': 1}, 1, 1, a.penetration_factor)
    validate_penetration_mode(a.mode, a.source, a.penetration_factor, a.health_factor, a.offense_factor,
                              a.ability_candidate, a.add_search or a.add_references, a.current_content,
                              a.gear_reference or a.qualified_family or a.fixed_support_reference)
    validate_ability_mode(a.mode, a.source, a.ability_candidate, a.health_factor, a.offense_factor,
                          a.add_search or a.add_references, a.current_content)
    health_pressure_module().validate_mode(a.mode, a.source, a.health_pressure_candidate,
        a.health_factor, a.offense_factor, a.penetration_factor, a.ability_candidate,
        a.add_search or a.add_references, a.current_content,
        a.gear_reference or a.qualified_family or a.fixed_support_reference)
    recovery_pressure_module().validate_mode(a.mode, a.source, a.recovery_pressure_candidate,
        a.health_factor, a.offense_factor, a.penetration_factor, a.ability_candidate, a.health_pressure_candidate,
        a.add_search or a.add_references, a.current_content,
        a.gear_reference or a.qualified_family or a.fixed_support_reference)
    owner = ROOT / f'TestResults/tower-balance-pass-{a.name}-owner-20260929'
    output = ROOT / f'TestResults/tower-balance-pass-{a.name}-study-20260929'
    diagnostic = None
    if a.diagnostic_contract:
        diagnostic_version = read(a.diagnostic_contract)['version']
        ni_diagnostic = diagnostic_version == 'floor9-limited-restoration-diagnostic-v1'
        diagnostic = diagnostic_module(diagnostic_version).admit_batch(
            SimpleNamespace(authenticate=authenticate, ability_module=ability_module, audit=audit, history=history),
            a.diagnostic_contract.resolve(), a.diagnostic_sha256, a.diagnostic_batch, a.source, output,
            a.mode, a.floor, a.samples, a.ability_candidate,
            [a.current_content,a.qualified_family,a.one_healer_family,a.eight_item_family,a.ni_restoration_family,a.add_search,a.add_references,
             a.gear_reference,a.fixed_support_reference,a.health_pressure_candidate,a.recovery_pressure_candidate,
             a.health_factor != 1,not ni_diagnostic and a.offense_factor != 1,not ni_diagnostic and a.penetration_factor != 1,a.search_gear,a.retain_search_references],a.native_seconds,
            **({'scalar_factors': (a.health_factor,a.offense_factor,a.penetration_factor)} if ni_diagnostic else {}))
    artifacts = a.artifacts.resolve()
    check(not owner.exists() and not output.exists(), 'Fresh phase paths required; no retries or overwrite')
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    check((tests / 'EssenceSystem.Tests.dll').is_file(), 'Build through run-tests.ps1 first')
    excluded, pins = history()
    api = ROOT / 'LL/src/API/API.LL'
    fixtures = ROOT / 'LL/tools/BalanceHarness/Fixtures'
    source_cells = None
    benchmark = 1
    if a.mode != 'prepare' or a.source is not None:
        check(a.source is not None, 'A completed source is required')
        source = a.source.resolve()
        authenticate(source)
        check(read(source / 'completion.json')['status'] == 'Complete', 'Failed source')
        check(not any(Path(path).name in ('ability-candidate-provenance.json', 'penetration-candidate-provenance.json', 'health-pressure-candidate-provenance.json', 'recovery-pressure-candidate-provenance.json')
                      for path in read(source / 'request.json')['inputHashes']),
              'Candidate phases must start from the original unchanged source and redeclare the plan')
        pins[str(source / 'files.json')] = sha(source / 'files.json')
        api = source / 'content'
        source_cells = read(source / ('evaluation-cells.json' if (source / 'evaluation-cells.json').exists() else 'cells.json'))
        if a.one_healer_family:
            source_cells = one_healer_module().admit(a.one_healer_family.resolve(), source)
        if a.eight_item_family:
            source_cells = eight_item_module().admit(a.eight_item_family.resolve(), source)
        if a.ni_restoration_family:
            source_cells = ni_restoration_module().admit(a.ni_restoration_family.resolve(), source)
        if a.current_content:
            api = ROOT / 'LL/src/API/API.LL'
            validate_current_content(source, api, a.floor)
        if a.qualified_family:
            api = ROOT / 'LL/src/API/API.LL'
            source_cells = qualification_module().admit_family(a.qualified_family.resolve(), source, api, tests, input_pins=pins)
            check(all(c['scenario']['floorNumber'] == a.floor for c in source_cells), 'Qualified family floor differs')
            pins[str(a.qualified_family.resolve())] = sha(a.qualified_family)
            pins[str(Path(__file__).with_name('tower-catalog-qualification.py'))] = sha(Path(__file__).with_name('tower-catalog-qualification.py'))
        if a.mode == 'search':
            rows = read(source / 'result.json')['rows']
            source_cells, benchmark = select_search_references(source_cells, rows, a.search_gear)
    elif a.floor == 5:
        check(sha(EARNED / 'files.json') == EARNED_PIN, 'Changed earned archive')
        manifest = read(EARNED / 'files.json')
        for path in EARNED.glob('*--continuation.json.gz'):
            check(sha(path) == manifest[path.name], 'Changed earned composition')
            pins[str(path)] = manifest[path.name]
        pins[str(EARNED / 'files.json')] = EARNED_PIN
    ability_plan = None
    if a.ability_candidate:
        ability_plan = read(a.ability_candidate)
        if ability_plan.get('version') == 'tower-ni-restoration-offense-v1':
            check(diagnostic is not None and diagnostic['version'] == 'floor9-restoration-offense-diagnostic-v1', 'Ni offense calibration is diagnostic only')
        ability_module().validate(api, ability_plan, a.floor)
    health_plan = read(a.health_pressure_candidate) if a.health_pressure_candidate else None
    if health_plan is not None:
        health_pressure_module().expected(api, health_plan, a.floor)
    recovery_plan = read(a.recovery_pressure_candidate) if a.recovery_pressure_candidate else None
    if recovery_plan is not None:
        recovery_pressure_module(recovery_plan['version']).expected(api, recovery_plan, a.floor)
    gear_reference = append_gear_reference(source_cells, *a.gear_reference) if a.gear_reference else None
    support_references = []
    if a.fixed_support_reference:
        saved_ids = {c['id'] for c in source_cells}
        check(all(s in saved_ids and d in saved_ids for s, d, _ in a.fixed_support_reference),
              'Support projections must use original saved recipes, not chained projections')
        for source_id, donor_id, party_slot in a.fixed_support_reference:
            support_references.append(append_fixed_support_reference(source_cells, source_id, donor_id, int(party_slot)))
        helper = Path(__file__).with_name('tower-catalog-qualification.py')
        pins[str(helper)] = sha(helper)
    owner.mkdir()
    shutil.copy2(Path(__file__), owner / 'owner-source.py')
    if a.ni_restoration_family:
        proposal = a.ni_restoration_family.resolve(); provenance = owner/'ni-restoration-family-provenance.json'
        write(provenance, dict(source=str(source),sourceManifestSha256=sha(source/'files.json'),proposal=str(proposal),proposalSha256=sha(proposal)))
        for path in (proposal, provenance): pins[str(path)] = sha(path)
    if a.ni_restoration_family or diagnostic and diagnostic['version'] == 'floor9-limited-restoration-diagnostic-v1':
        for name in ('tower-ni-limited-restoration.py','tower-ni-penetration.py','tower-one-healer-family.py','tower-kodoku-shared-penetration.py'):
            helper = Path(__file__).with_name(name); shutil.copy2(helper, owner/name)
            for path in (helper,owner/name): pins[str(path)] = sha(path)
    if a.eight_item_family:
        proposal = a.eight_item_family.resolve(); provenance = owner/'eight-item-family-provenance.json'
        write(provenance, dict(source=str(source),sourceManifestSha256=sha(source/'files.json'),proposal=str(proposal),proposalSha256=sha(proposal)))
        for path in (proposal, provenance): pins[str(path)] = sha(path)
    if diagnostic:
        declaration = a.diagnostic_contract.resolve()
        provenance = owner/(diagnostic_module(diagnostic['version']).PROVENANCE if diagnostic['version'] != 'floor8-two-healer-eight-item-diagnostic-v1' else 'eight-item-diagnostic-provenance.json')
        write(provenance, dict(declaration=str(declaration),declarationSha256=a.diagnostic_sha256,batchIndex=a.diagnostic_batch))
        for path in (declaration, provenance): pins[str(path)] = sha(path)
    if a.eight_item_family or diagnostic:
        for name in ('tower-eight-item-diagnostic.py','tower-one-healer-family.py'):
            helper = Path(__file__).with_name(name); shutil.copy2(helper, owner/name)
            for path in (helper,owner/name): pins[str(path)] = sha(path)
        if diagnostic and diagnostic['version'] in ('floor9-restoration-offense-diagnostic-v1', 'floor8-kodoku-offense-diagnostic-v1', 'floor8-kodoku-offense-refinement-diagnostic-v1', 'floor8-kodoku-midpoint-diagnostic-v1'):
            helper = Path(diagnostic_module(diagnostic['version']).__file__); shutil.copy2(helper, owner/helper.name)
            for path in (helper,owner/helper.name): pins[str(path)] = sha(path)
    if a.one_healer_family:
        proposal = a.one_healer_family.resolve(); helper = Path(__file__).with_name('tower-one-healer-family.py')
        provenance = owner/'one-healer-family-provenance.json'
        write(provenance, dict(source=str(source),sourceManifestSha256=sha(source/'files.json'),
                              proposal=str(proposal),proposalSha256=sha(proposal)))
        shutil.copy2(helper, owner/helper.name)
        for path in (proposal, helper, owner/helper.name, provenance): pins[str(path)] = sha(path)
    if gear_reference is not None:
        write(owner / 'gear-reference-provenance.json', gear_reference)
        pins[str(owner / 'gear-reference-provenance.json')] = sha(owner / 'gear-reference-provenance.json')
    if support_references:
        write(owner / 'fixed-support-provenance.json', dict(source=str(source),
              sourceManifestSha256=sha(source / 'files.json'), projections=support_references))
        pins[str(owner / 'fixed-support-provenance.json')] = sha(owner / 'fixed-support-provenance.json')
    reference_coverage = []
    for added in a.add_references:
        search_root = added.resolve()
        authenticate(search_root)
        check(read(search_root / 'completion.json')['status'] == 'Complete' and
              read(search_root / 'request.json')['mode'] == 'search', 'Completed search required')
        pins[str(search_root / 'files.json')] = sha(search_root / 'files.json')
        reference_coverage.extend(append_references(source_cells, read(search_root / 'evaluation-cells.json'), search_root))
    if a.add_references:
        write(owner / 'reference-coverage.json', reference_coverage)
        pins[str(owner / 'reference-coverage.json')] = sha(owner / 'reference-coverage.json')
    finalist_coverage = []
    for added in a.add_search:
        check(a.mode in ('screen', 'confirm') and source_cells is not None, 'Only fixed-family evaluations may add finalists')
        search_root = added.resolve()
        authenticate(search_root)
        check(read(search_root / 'completion.json')['status'] == 'Complete' and
              read(search_root / 'request.json')['mode'] == 'search', 'Completed search required')
        pins[str(search_root / 'files.json')] = sha(search_root / 'files.json')
        evaluated = read(search_root / 'evaluation-cells.json')
        finalist_coverage.extend(append_search_finalists(source_cells, evaluated, search_root))
        if a.retain_search_references:
            reference_coverage.extend(append_references(source_cells, evaluated, search_root))
    if a.add_search:
        write(owner / 'search-coverage.json', dict(finalists=finalist_coverage, references=reference_coverage))
        pins[str(owner / 'search-coverage.json')] = sha(owner / 'search-coverage.json')
    if ability_plan is not None:
        isolated = owner / 'candidate-content'
        shutil.copytree(api, isolated)
        ability_module().materialize(api, isolated, ability_plan, a.floor)
        provenance = owner / 'ability-candidate-provenance.json'
        write(provenance, dict(source=str(source), sourceManifestSha256=sha(source / 'files.json'), plan=ability_plan))
        pins[str(provenance)] = sha(provenance)
        helper = Path(__file__).with_name('tower-ability-candidate.py')
        shutil.copy2(helper, owner / helper.name)
        pins[str(helper)] = sha(helper)
        pins[str(owner / helper.name)] = sha(owner / helper.name)
        if ability_plan['version'] in ('tower-kodoku-miasma-v1', 'tower-kodoku-shared-penetration-v1'):
            helper = Path(__file__).with_name('tower-kodoku-miasma.py' if ability_plan['version'] == 'tower-kodoku-miasma-v1' else 'tower-kodoku-shared-penetration.py')
            shutil.copy2(helper, owner / helper.name)
            pins[str(helper)] = sha(helper)
            pins[str(owner / helper.name)] = sha(owner / helper.name)
        if ability_plan['version'] in ('tower-kodoku-eight-item-pressure-v1', 'tower-kodoku-eight-item-pressure-refinement-v1', 'tower-kodoku-eight-item-pressure-midpoint-v1'):
            candidate_helper = {'tower-kodoku-eight-item-pressure-v1': 'tower-kodoku-offense.py',
                                'tower-kodoku-eight-item-pressure-refinement-v1': 'tower-kodoku-offense-refinement.py',
                                'tower-kodoku-eight-item-pressure-midpoint-v1': 'tower-kodoku-midpoint.py'}[ability_plan['version']]
            for name in (candidate_helper, 'tower-kodoku-shared-penetration.py'):
                helper = Path(__file__).with_name(name); shutil.copy2(helper, owner/name)
                for path in (helper,owner/name): pins[str(path)] = sha(path)
        if ability_plan['version'] == 'tower-ni-restoration-acceptance-v1':
            for name in ('tower-ni-restoration-acceptance.py', 'tower-kodoku-shared-penetration.py'):
                helper = Path(__file__).with_name(name); shutil.copy2(helper, owner/name)
                for path in (helper, owner/name): pins[str(path)] = sha(path)
        if ability_plan['version'] == 'tower-ni-restoration-offense-v1':
            for name in ('tower-ni-restoration-offense.py', 'tower-kodoku-shared-penetration.py'):
                helper = Path(__file__).with_name(name); shutil.copy2(helper, owner/name)
                for path in (helper, owner/name): pins[str(path)] = sha(path)
        if ability_plan['version'] == 'tower-ni-copy-health-v1':
            for name in ('tower-ni-copy-health.py', 'tower-kodoku-shared-penetration.py'):
                helper = Path(__file__).with_name(name); shutil.copy2(helper, owner/name)
                for path in (helper, owner/name): pins[str(path)] = sha(path)
        for path in (api / 'Data').rglob('*.json'):
            pins[str(path)] = sha(path)
        api = isolated
    if health_plan is not None:
        isolated = owner / 'candidate-content'
        shutil.copytree(api, isolated)
        health_pressure_module().materialize(api, isolated, health_plan, a.floor)
        provenance = owner / 'health-pressure-candidate-provenance.json'
        write(provenance, dict(source=str(source), sourceManifestSha256=sha(source / 'files.json'), plan=health_plan))
        pins[str(provenance)] = sha(provenance)
        helper = Path(__file__).with_name('tower-health-pressure-candidate.py')
        shutil.copy2(helper, owner / helper.name)
        pins[str(helper)] = sha(helper)
        pins[str(owner / helper.name)] = sha(owner / helper.name)
        for path in (api / 'Data').rglob('*.json'):
            pins[str(path)] = sha(path)
        api = isolated
    if recovery_plan is not None:
        isolated = owner / 'candidate-content'
        shutil.copytree(api, isolated)
        recovery_pressure_module(recovery_plan['version']).materialize(api, isolated, recovery_plan, a.floor)
        provenance = owner / 'recovery-pressure-candidate-provenance.json'
        write(provenance, dict(source=str(source), sourceManifestSha256=sha(source / 'files.json'), plan=recovery_plan))
        pins[str(provenance)] = sha(provenance)
        for name in ('tower-recovery-pressure-candidate.py', 'tower-recovery-pressure-refinement.py', 'tower-health-pressure-candidate.py'):
            helper = Path(__file__).with_name(name)
            shutil.copy2(helper, owner / helper.name)
            pins[str(helper)] = sha(helper)
            pins[str(owner / helper.name)] = sha(owner / helper.name)
        for path in (api / 'Data').rglob('*.json'):
            pins[str(path)] = sha(path)
        api = isolated
    if a.health_factor != 1 or a.offense_factor != 1 or a.penetration_factor != 1:
        check(a.mode in ('screen', 'confirm'), 'Boss variants are fixed-family studies only')
        isolated = owner / 'candidate-content'
        shutil.copytree(api, isolated)
        path = isolated / 'Data/world-tower/tower-floors.json'
        data = read(path)
        floor = next(f for f in data['floors'] if f['floorNumber'] == a.floor)
        before = floor['guardianScaling']
        floor['guardianScaling'] = scaled_guardian(before, a.health_factor, a.offense_factor, a.penetration_factor)
        path.write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')
        if a.penetration_factor != 1:
            plan = dict(source=str(source), sourceManifestSha256=sha(source / 'files.json'), floor=a.floor,
                        offenseFactor=a.offense_factor, penetrationFactor=a.penetration_factor)
            verify_penetration_candidate(api, isolated, plan)
            provenance = owner / 'penetration-candidate-provenance.json'
            write(provenance, plan); pins[str(provenance)] = sha(provenance)
            for original_path in (api / 'Data').rglob('*.json'):
                pins[str(original_path)] = sha(original_path)
        write(owner / 'candidate.json', dict(floor=a.floor, healthBefore=before['health'],
              healthAfter=floor['guardianScaling']['health'], healthFactor=a.health_factor,
              offenseBefore=before['offense'], offenseAfter=floor['guardianScaling']['offense'], offenseFactor=a.offense_factor,
              productionChanged=False))
        api = isolated
    if source_cells is not None:
        write(owner / 'cells.json', source_cells)
        pins[str(owner / 'cells.json')] = sha(owner / 'cells.json')
    history_values = sorted(excluded)
    write(owner / 'history.json', history_values)
    pins[str(owner / 'history.json')] = sha(owner / 'history.json')
    seeds, search = [], []
    if a.mode != 'prepare':
        reserved, cursor = [], 0
        total = a.samples + (109 if a.mode == 'search' else 0)
        while len(reserved) < total:
            value = int.from_bytes(hashlib.sha256(f'{VERSION}|{a.name}|{cursor}'.encode()).digest()[:4], 'little', signed=True)
            cursor += 1
            if value not in excluded:
                excluded.add(value); reserved.append(value)
        search = reserved[:109] if a.mode == 'search' else []
        seeds = reserved[109:] if a.mode == 'search' else reserved
        write(owner / 'seed-ledger.json', dict(version=VERSION, state='ReservedIncludingUnconsumed',
              predecessorPins=pins.copy(), reserved=reserved, exclusionUnionCount=len(excluded)))
        pins[str(owner / 'seed-ledger.json')] = sha(owner / 'seed-ledger.json')
    for folder, pattern in [(api / 'Data', '*.json'), (fixtures, '*.json'), (tests, '*.dll')]:
        for path in folder.rglob(pattern):
            pins[str(path)] = sha(path)
    for path in [api / 'appsettings.json', Path(__file__), ROOT / 'build/run-tests.ps1',
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalancePassTests.cs',
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs']:
        pins[str(path)] = sha(path)
    maximum = 0 if a.mode == 'prepare' else 528 + 5*a.samples if a.mode == 'search' else len(source_cells)*a.samples
    q = dict(mode=a.mode, apiRoot=str(api), fixtures=str(fixtures), output=str(output), floor=a.floor, seeds=seeds,
             searchSeeds=search, cells=str(owner/'cells.json') if source_cells else None,
             earned=str(EARNED) if a.mode == 'prepare' and a.floor == 5 and source_cells is None else None,
             history=str(owner/'history.json'), benchmark=benchmark, maximumFights=maximum, inputHashes=pins)
    if a.native_seconds != 840:
        q['nativeSeconds'] = a.native_seconds
    if diagnostic:
        q['diagnosticVersion'] = diagnostic['version']
    write(owner / 'request.json', q)
    spec = importlib.util.spec_from_file_location('balance_pass_owner', ROOT / 'build/bounded_windows_process.py')
    process = importlib.util.module_from_spec(spec); sys.modules[spec.name] = process; spec.loader.exec_module(process)
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT/'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests.Frozen_tower_balance_pass']
    os.environ['LL_TOWER_BALANCE_PASS'] = str(owner / 'request.json')
    receipt = process.run(command, ROOT, owner/'execution.log', time.monotonic()+a.native_seconds+60, log_byte_limit=1048576)
    write(owner / 'process.json', receipt)
    check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Bounded operation failed; preserve outputs')
    verified = audit(output)
    if a.current_content or a.qualified_family:
        check(read(source / 'scope.json')['settings'] == read(output / 'scope.json')['settings'],
              'Current native combat settings differ; preparation preserved, no fights permitted')
    write(owner / 'independent-audit.json', verified)
    write(owner / 'completion.json', dict(status='Complete', manifestPin=sha(output/'files.json'), result=read(output/'result.json')))
    summary = {k: v for k, v in verified.items() if k != 'assessment'}
    if 'assessment' in verified:
        summary['verdict'] = verified['assessment']['verdict']
    print(json.dumps(dict(output=str(output), **summary)), flush=True)


if __name__ == '__main__':
    main()
