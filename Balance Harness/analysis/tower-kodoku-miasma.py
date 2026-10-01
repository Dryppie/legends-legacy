"""Strict single-effect Miasma candidate; no shared engine or other catalog changes."""
import copy
import hashlib
import json
from pathlib import Path
import re

VERSION = 'tower-kodoku-miasma-v1'
ABILITY = 'ability.creature.kodoku.withering_miasma'
HEALING = dict(id='effect.creature.kodoku.withering_miasma.healing', operation='ModifyHealingReceived',
               target='AllEnemies', baseValue=-80, durationTicks=150, procCoefficient=1)
RATE = dict(id='effect.creature.kodoku.withering_miasma.regeneration', operation='ModifyRegenerationRate',
            target='AllEnemies', baseValue=-80, durationTicks=150, procCoefficient=1)
BEFORE = "Reduce all enemies' healing received and Health Regeneration by 80% for 15 seconds."
AFTER = "Reduce all enemies' healing received, including Health Regeneration amounts, by 80% for 15 seconds."
ABILITIES = Path('Data/combat/abilities.json')
TOWER = Path('Data/world-tower/tower-floors.json')


def check(ok, message):
    if not ok: raise ValueError(message)


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def sha(path):
    with Path(path).open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()


def unique(items, key, value):
    found = [x for x in items if x.get(key) == value]
    check(len(found) == 1, 'Missing or ambiguous '+str(value)); return found[0]


def validate_plan(plan, floor):
    check(type(floor) is int and floor == 8 and set(plan) == {'version', 'abilityId', 'removeEffect', 'preserveEffects',
        'descriptionFrom', 'descriptionTo', 'sourceAbilitiesSha256', 'sourceTowerSha256'}, 'Exact floor-eight Miasma plan required')
    check(plan['version'] == VERSION and plan['abilityId'] == ABILITY and plan['removeEffect'] == RATE and
          plan['preserveEffects'] == [HEALING] and plan['descriptionFrom'] == BEFORE and plan['descriptionTo'] == AFTER,
          'Only the extra regeneration-rate effect may be removed')
    for key in ('sourceAbilitiesSha256', 'sourceTowerSha256'):
        check(type(plan[key]) is str and re.fullmatch('[0-9a-f]{64}', plan[key]), 'Source SHA-256 required')


def expected(api, plan, floor):
    validate_plan(plan, floor)
    check(sha(api/ABILITIES) == plan['sourceAbilitiesSha256'] and sha(api/TOWER) == plan['sourceTowerSha256'], 'Candidate source changed')
    floors = read(api/TOWER)['floors']; profile = unique(floors, 'floorNumber', floor)['guardianAbilityProfileId']
    check(sum(f.get('guardianAbilityProfileId') == profile for f in floors) == 1, 'Guardian profile shared across floors')
    owners = read(api/'Data/combat/creature-abilities.json')['creatures']
    check([p['monsterId'] for p in owners if ABILITY in p['abilityIds']] == [profile], 'Ability is not exclusive to this guardian')
    abilities = read(api/ABILITIES); ability = unique(abilities, 'id', ABILITY)
    check(ability == dict(id=ABILITY, kind='Active', name='Withering Miasma', description=BEFORE, cooldownTicks=150,
        tags=['Area', 'Debuff', 'HealingReduction', 'RegenerationReduction'], effects=[HEALING, RATE]), 'Authored Miasma changed')
    ability['effects'] = copy.deepcopy(plan['preserveEffects']); ability['description'] = AFTER
    return abilities


def materialize(api, isolated, plan, floor):
    check(api.resolve() != isolated.resolve(), 'Candidate must be isolated')
    intended = expected(api, plan, floor)
    check(sha(isolated/ABILITIES) == plan['sourceAbilitiesSha256'], 'Isolated source changed')
    # Preserve unrelated formatting and bytes in this large authored catalog.
    raw = (api/ABILITIES).read_bytes().decode('utf-8')
    pattern = r',\s*\{\s*"id"\s*:\s*'+re.escape(json.dumps(RATE['id']))+r'\s*,[^{}]*\}'
    raw, count = re.subn(pattern, '', raw); check(count == 1, 'Ambiguous effect removal')
    description = r'("id"\s*:\s*'+re.escape(json.dumps(ABILITY))+r'\s*,.*?"description"\s*:\s*)"(?:[^"\\]|\\.)*"'
    raw, count = re.subn(description, lambda m: m[1]+json.dumps(AFTER), raw, flags=re.DOTALL)
    check(count == 1 and json.loads(raw.lstrip('\ufeff')) == intended, 'Candidate text differs from exact removal')
    (isolated/ABILITIES).write_bytes(raw.encode('utf-8'))
    return verify(api, isolated, plan, floor)


def verify(api, candidate, plan, floor):
    intended = expected(api, plan, floor)
    source_files = {p.relative_to(api) for p in (api/'Data').rglob('*.json')}
    check(source_files == {p.relative_to(candidate) for p in (candidate/'Data').rglob('*.json')}, 'Catalog set changed')
    for relative in source_files:
        if relative == ABILITIES: check(read(candidate/relative) == intended, 'Undeclared ability delta')
        else: check(sha(api/relative) == sha(candidate/relative), 'Unrelated catalog changed: '+str(relative))
    check(sha(api/'appsettings.json') == sha(candidate/'appsettings.json'), 'Settings changed')
    return dict(version=VERSION, floor=floor, changes=1, catalogs=len(source_files),
        sourceAbilitiesSha256=sha(api/ABILITIES), candidateAbilitiesSha256=sha(candidate/ABILITIES))
