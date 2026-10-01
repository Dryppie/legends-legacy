"""Frozen floor-nine copy-Health trial; no guardian or other ability changes."""
import importlib.util
import json
from pathlib import Path
import re

spec = importlib.util.spec_from_file_location('ni_catalog_helpers', Path(__file__).with_name('tower-kodoku-shared-penetration.py'))
h = importlib.util.module_from_spec(spec); spec.loader.exec_module(h)
VERSION = 'tower-ni-copy-health-v1'
TOWER, SUMMONS, ABILITIES = h.TOWER, h.SUMMONS, h.ABILITIES
HASHES = h.HASHES
DESCRIPTION = "Innate: summon 9 inert copies with {}% of Ni's Max Health and inherited Armor and Resistance. Whenever a copy dies, Ni permanently gains 5% of initial Power."
VALUES = dict(version=VERSION, floor=9, summonId='niCopy', attribute='MaxHealth',
              originalScalingCoefficient=.1, scalingCoefficient=.05,
              abilityId='ability.creature.ni.ninefold', originalDescription=DESCRIPTION.format(10), description=DESCRIPTION.format(5))


def validate_plan(plan, floor):
    h.check(type(floor) is int and floor == 9 and set(plan) == set(VALUES) | set(HASHES), 'Exact floor-nine copy-Health plan required')
    h.check({k: plan[k] for k in VALUES} == VALUES, 'Only the frozen copy-Health candidate is supported')
    h.check(type(plan['floor']) is int and all(type(plan[k]) in (int, float) for k in ('originalScalingCoefficient', 'scalingCoefficient')), 'Numeric candidate fields required')
    for key in HASHES:
        h.check(type(plan[key]) is str and re.fullmatch('[0-9a-f]{64}', plan[key]), 'Source SHA-256 required')


def expected(api, plan, floor):
    validate_plan(plan, floor)
    for key, relative in HASHES.items(): h.check(h.sha(api/relative) == plan[key], 'Candidate source changed')
    tower, summons, abilities = h.read(api/TOWER), h.read(api/SUMMONS), h.read(api/ABILITIES)
    guardian = h.unique(tower['floors'], 'floorNumber', floor); profile = guardian['guardianAbilityProfileId']
    h.check(sum(f.get('guardianAbilityProfileId') == profile for f in tower['floors']) == 1, 'Guardian profile shared across floors')
    owners = h.read(api/'Data/combat/creature-abilities.json')['creatures']; owner = h.unique(owners, 'monsterId', profile)
    users = [a['id'] for a in abilities if 'niCopy' in json.dumps(a)]
    ids = {'ability.creature.ni.'+name for name in ('ninefold', 'ninefold_strike', 'ninth_seal', 'one_among_nine')}
    h.check(set(users) == ids and all(a in owner['abilityIds'] and sum(a in p['abilityIds'] for p in owners) == 1 for a in users), 'Copies must be exclusive to floor nine')
    summon = h.unique(summons, 'id', 'niCopy')
    h.check(summon['attributes'] == [dict(attribute='MaxHealth', baseValue=0, minimumValue=1, scalingAttribute='MaxHealth', scalingCoefficient=.1),
        dict(attribute='Armor', baseValue=0, minimumValue=0, scalingAttribute='Armor', scalingCoefficient=1),
        dict(attribute='Resistance', baseValue=0, minimumValue=0, scalingAttribute='Resistance', scalingCoefficient=1),
        dict(attribute='Power', baseValue=0, minimumValue=0), dict(attribute='AttackSpeed', baseValue=0, minimumValue=0)] and
        summon['maxActive'] == 9 and summon['durationTicks'] == 0 and summon['canBasicAttack'] is False and summon['abilityIds'] == [], 'Authored copies changed')
    ability = h.unique(abilities, 'id', plan['abilityId'])
    h.check(ability['description'] == plan['originalDescription'], 'Ninefold description changed')
    summon['attributes'][0]['scalingCoefficient'] = .05
    ability['description'] = plan['description']
    return {SUMMONS: summons, ABILITIES: abilities}


def materialize(api, isolated, plan, floor):
    h.check(api.resolve() != isolated.resolve(), 'Candidate must be isolated')
    intended = expected(api, plan, floor)
    for key, relative in HASHES.items(): h.check(h.sha(isolated/relative) == plan[key], 'Isolated source changed')
    for relative, identity in ((SUMMONS, 'niCopy'), (ABILITIES, plan['abilityId'])):
        raw = (api/relative).read_bytes().decode('utf-8')
        matches = list(re.finditer(r'\{\s*"id"\s*:\s*'+re.escape(json.dumps(identity))+r'\s*,', raw))
        h.check(len(matches) == 1, 'Ambiguous authored object')
        start = matches[0].start(); _, length = json.JSONDecoder().raw_decode(raw[start:])
        replacement = json.dumps(h.unique(intended[relative], 'id', identity), indent=2, ensure_ascii=False)
        indent = ' ' * (start - raw.rfind('\n', 0, start) - 1)
        replacement = replacement.replace('\n', ('\r\n' if '\r\n' in raw else '\n')+indent)
        raw = raw[:start]+replacement+raw[start+length:]
        h.check(json.loads(raw.lstrip('\ufeff')) == intended[relative], 'Candidate text differs')
        (isolated/relative).write_bytes(raw.encode('utf-8'))
    return verify(api, isolated, plan, floor)


def verify(api, candidate, plan, floor):
    intended = expected(api, plan, floor)
    files = {p.relative_to(api) for p in (api/'Data').rglob('*.json')}
    h.check(files == {p.relative_to(candidate) for p in (candidate/'Data').rglob('*.json')}, 'Catalog set changed')
    for relative in files:
        if relative in intended: h.check(h.read(candidate/relative) == intended[relative], 'Undeclared catalog delta: '+str(relative))
        else: h.check(h.sha(api/relative) == h.sha(candidate/relative), 'Unrelated catalog changed: '+str(relative))
    h.check(h.sha(api/'appsettings.json') == h.sha(candidate/'appsettings.json'), 'Settings changed')
    return dict(version=VERSION, floor=floor, changes=2, catalogs=len(files), candidateTowerSha256=h.sha(candidate/TOWER),
                candidateSummonsSha256=h.sha(candidate/SUMMONS), candidateAbilitiesSha256=h.sha(candidate/ABILITIES))
