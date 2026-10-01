"""One frozen floor-eight Power/penetration and Venomspawn inheritance trial."""
import copy
import hashlib
import json
from pathlib import Path
import re

VERSION = 'tower-kodoku-shared-penetration-v1'
TOWER = Path('Data/world-tower/tower-floors.json')
SUMMONS = Path('Data/combat/summons.json')
ABILITIES = Path('Data/combat/abilities.json')
ATTRIBUTE = dict(attribute='ArmorPenetration', baseValue=0, minimumValue=0,
                 scalingAttribute='ArmorPenetration', scalingCoefficient=1)
VALUES = dict(version=VERSION, floor=8, offenseFactor=.525, penetrationFactor=40,
              sourceOffense=8.8260253906, sourcePenetration=1, summonId='venomSpawn', appendAttribute=ATTRIBUTE)
HASHES = {'sourceTowerSha256': TOWER, 'sourceSummonsSha256': SUMMONS, 'sourceAbilitiesSha256': ABILITIES}


def check(ok, message):
    if not ok: raise ValueError(message)


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def unique(items, key, value):
    found = [x for x in items if x.get(key) == value]
    check(len(found) == 1, 'Missing or ambiguous '+str(value)); return found[0]


def validate_plan(plan, floor):
    check(type(floor) is int and floor == 8 and set(plan) == set(VALUES) | set(HASHES), 'Exact floor-eight shared-penetration plan required')
    check({k: plan[k] for k in VALUES} == VALUES, 'Only the frozen shared-penetration candidate is supported')
    for k, v in VALUES.items():
        if type(v) in (int, float): check(type(plan[k]) in (int, float) and type(plan[k]) is not bool, 'Numeric candidate field required')
    check(type(plan['floor']) is int and all(type(plan['appendAttribute'][k]) in (int, float) for k in ('baseValue', 'minimumValue', 'scalingCoefficient')), 'Invalid candidate numeric types')
    for key in HASHES:
        check(type(plan[key]) is str and re.fullmatch('[0-9a-f]{64}', plan[key]), 'Source SHA-256 required')


def expected(api, plan, floor):
    validate_plan(plan, floor)
    for key, relative in HASHES.items(): check(sha(api/relative) == plan[key], 'Candidate source changed')
    tower, summons, abilities = read(api/TOWER), read(api/SUMMONS), read(api/ABILITIES)
    guardian = unique(tower['floors'], 'floorNumber', floor); profile = guardian['guardianAbilityProfileId']
    check(sum(f.get('guardianAbilityProfileId') == profile for f in tower['floors']) == 1, 'Guardian profile shared across floors')
    owners = read(api/'Data/combat/creature-abilities.json')['creatures']
    owner = unique(owners, 'monsterId', profile)
    users = [a['id'] for a in abilities if any(e.get('summonId') == 'venomSpawn' for e in a.get('effects', []))]
    check(set(users) == {'ability.creature.kodoku.insect_jar', 'ability.creature.kodoku.survivors_struggle'} and
          all(a in owner['abilityIds'] and sum(a in p['abilityIds'] for p in owners) == 1 for a in users), 'Venomspawn is not exclusive to floor eight')
    summon = unique(summons, 'id', 'venomSpawn')
    check(summon['attributes'] == [dict(attribute='MaxHealth', baseValue=0, minimumValue=1, scalingAttribute='MaxHealth', scalingCoefficient=.08),
        dict(attribute='Power', baseValue=0, minimumValue=1, scalingAttribute='Power', scalingCoefficient=.15),
        dict(attribute='AttackSpeed', baseValue=0, minimumValue=0)] and summon['maxActive'] == 5 and summon['durationTicks'] == 0 and
        summon['canBasicAttack'] is True and summon['abilityIds'] == [], 'Authored Venomspawn changed')
    for name in ('offense', 'penetration'):
        check(guardian['guardianScaling'][name] == plan['source'+name.title()], 'Guardian scaling changed')
        guardian['guardianScaling'][name] = round(guardian['guardianScaling'][name] * plan[name+'Factor'], 10)
    summon['attributes'].append(copy.deepcopy(ATTRIBUTE))
    return {TOWER: tower, SUMMONS: summons}


def materialize(api, isolated, plan, floor):
    check(api.resolve() != isolated.resolve(), 'Candidate must be isolated')
    intended = expected(api, plan, floor)
    for key, relative in HASHES.items(): check(sha(isolated/relative) == plan[key], 'Isolated source changed')
    write_objects(api, isolated, intended)
    return verify(api, isolated, plan, floor)


def write_objects(api, isolated, intended):
    # Replace only the changed object; preserve every byte outside it.
    for relative, key, value, container in ((TOWER, 'floorNumber', 8, 'floors'), (SUMMONS, 'id', 'venomSpawn', None)):
        raw = (api/relative).read_bytes().decode('utf-8')
        matches = list(re.finditer(r'\{\s*"'+key+r'"\s*:\s*'+re.escape(json.dumps(value))+r'\s*,', raw))
        check(len(matches) == 1, 'Ambiguous authored object')
        start = matches[0].start(); _, length = json.JSONDecoder().raw_decode(raw[start:])
        rows = intended[relative][container] if container else intended[relative]
        replacement = json.dumps(unique(rows, key, value), indent=2, ensure_ascii=False)
        indent = ' ' * (start - raw.rfind('\n', 0, start) - 1)
        newline = '\r\n' if '\r\n' in raw else '\n'
        replacement = replacement.replace('\n', newline+indent)
        raw = raw[:start]+replacement+raw[start+length:]
        check(json.loads(raw.lstrip('\ufeff')) == intended[relative], 'Candidate text differs')
        (isolated/relative).write_bytes(raw.encode('utf-8'))


def verify(api, candidate, plan, floor):
    intended = expected(api, plan, floor)
    return verify_expected(api, candidate, intended, floor, VERSION)


def verify_expected(api, candidate, intended, floor, version):
    files = {p.relative_to(api) for p in (api/'Data').rglob('*.json')}
    check(files == {p.relative_to(candidate) for p in (candidate/'Data').rglob('*.json')}, 'Catalog set changed')
    for relative in files:
        if relative in intended: check(read(candidate/relative) == intended[relative], 'Undeclared catalog delta: '+str(relative))
        else: check(sha(api/relative) == sha(candidate/relative), 'Unrelated catalog changed: '+str(relative))
    check(sha(api/'appsettings.json') == sha(candidate/'appsettings.json'), 'Settings changed')
    return dict(version=version, floor=floor, changes=3, catalogs=len(files),
                candidateTowerSha256=sha(candidate/TOWER), candidateSummonsSha256=sha(candidate/SUMMONS))
