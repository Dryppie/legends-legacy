"""Narrow, data-driven damage and health-target candidates for isolated Tower studies."""
import copy
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import re

VERSION = 'tower-ability-coefficients-v1'
CONSUMPTION_VERSION = 'tower-ability-coefficients-v2'
STATUS_SCALED_VERSION = 'tower-ability-coefficients-v3'
HEALTH_TARGET_VERSION = 'tower-ability-health-target-v1'
SPRINGTIDE_RAMP_VERSION = 'tower-springtide-ramp-v1'
SPRINGTIDE_REFINEMENT_VERSION = 'tower-springtide-refinement-v1'
MIASMA_VERSION = 'tower-kodoku-miasma-v1'
SHARED_PENETRATION_VERSION = 'tower-kodoku-shared-penetration-v1'
KODOKU_OFFENSE_VERSION = 'tower-kodoku-eight-item-pressure-v1'
KODOKU_REFINEMENT_VERSION = 'tower-kodoku-eight-item-pressure-refinement-v1'
KODOKU_MIDPOINT_VERSION = 'tower-kodoku-eight-item-pressure-midpoint-v1'
NI_COPY_HEALTH_VERSION = 'tower-ni-copy-health-v1'
ABILITIES = Path('Data/combat/abilities.json')
TOWER = Path('Data/world-tower/tower-floors.json')


def check(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def unique(items, key, value):
    found = [item for item in items if item.get(key) == value]
    check(len(found) == 1, 'Missing or ambiguous identity: ' + str(value))
    return found[0]


def shared_penetration_module():
    spec = importlib.util.spec_from_file_location('tower_kodoku_shared_penetration', Path(__file__).with_name('tower-kodoku-shared-penetration.py'))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def kodoku_offense_module():
    spec = importlib.util.spec_from_file_location('tower_kodoku_offense', Path(__file__).with_name('tower-kodoku-offense.py'))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def kodoku_refinement_module():
    spec = importlib.util.spec_from_file_location('tower_kodoku_refinement', Path(__file__).with_name('tower-kodoku-offense-refinement.py'))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def miasma_module():
    spec = importlib.util.spec_from_file_location('tower_kodoku_miasma', Path(__file__).with_name('tower-kodoku-miasma.py'))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def kodoku_midpoint_module():
    spec = importlib.util.spec_from_file_location('tower_kodoku_midpoint', Path(__file__).with_name('tower-kodoku-midpoint.py'))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def ni_copy_health_module():
    spec = importlib.util.spec_from_file_location('tower_ni_copy_health', Path(__file__).with_name('tower-ni-copy-health.py'))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def validate(api, plan, floor):
    if plan.get('version') == NI_COPY_HEALTH_VERSION:
        return ni_copy_health_module().expected(api, plan, floor)
    if plan.get('version') == KODOKU_MIDPOINT_VERSION:
        return kodoku_midpoint_module().expected(api, plan, floor)
    if plan.get('version') == KODOKU_REFINEMENT_VERSION:
        return kodoku_refinement_module().expected(api, plan, floor)
    if plan.get('version') == KODOKU_OFFENSE_VERSION:
        return kodoku_offense_module().expected(api, plan, floor)
    if plan.get('version') == SHARED_PENETRATION_VERSION:
        return shared_penetration_module().expected(api, plan, floor)
    if plan.get('version') == MIASMA_VERSION:
        return miasma_module().expected(api, plan, floor)
    check(set(plan) == {'version', 'floor', 'sourceAbilitiesSha256', 'sourceTowerSha256', 'changes'},
          'Unexpected candidate fields')
    check(plan['version'] in (VERSION, CONSUMPTION_VERSION, STATUS_SCALED_VERSION, HEALTH_TARGET_VERSION, SPRINGTIDE_RAMP_VERSION, SPRINGTIDE_REFINEMENT_VERSION) and type(plan['floor']) is int and plan['floor'] == floor,
          'Wrong candidate version or floor')
    check(sha(api / ABILITIES) == plan['sourceAbilitiesSha256'] and
          sha(api / TOWER) == plan['sourceTowerSha256'], 'Candidate source changed')
    floors = read(api / TOWER)['floors']
    guardian = unique(floors, 'floorNumber', floor)
    profile = guardian['guardianAbilityProfileId']
    check(sum(f.get('guardianAbilityProfileId') == profile for f in floors) == 1,
          'Guardian profile is shared across floors')
    profiles = read(api / 'Data/combat/creature-abilities.json')['creatures']
    owner = unique(profiles, 'monsterId', profile)
    abilities = read(api / ABILITIES)
    changes = plan['changes']
    check(type(changes) is list and 1 <= len(changes) <= 4, 'One to four explicit changes required')
    health_target = plan['version'] == HEALTH_TARGET_VERSION
    springtide_ramp = plan['version'] in (SPRINGTIDE_RAMP_VERSION, SPRINGTIDE_REFINEMENT_VERSION)
    check(not health_target or len(changes) == 1, 'Health targeting requires exactly one effect change')
    check(not springtide_ramp or (floor == 7 and len(changes) == 1),
          'Springtide ramp requires exactly one floor-7 effect change')
    seen = set()
    descriptions = {}
    for change in changes:
        fields = {'abilityId', 'effectId', 'from', 'to', 'descriptionFrom', 'descriptionTo'}
        if plan['version'] in (STATUS_SCALED_VERSION, SPRINGTIDE_RAMP_VERSION, SPRINGTIDE_REFINEMENT_VERSION):
            fields |= {'statusScalingFrom', 'statusScalingTo'}
        if health_target:
            fields = {'abilityId', 'effectId', 'targetFrom', 'targetTo', 'descriptionFrom', 'descriptionTo'}
        check(set(change) == fields,
              'Unexpected effect change fields')
        ability_id = change['abilityId']
        identity = (ability_id, change['effectId']) if plan['version'] in (STATUS_SCALED_VERSION, HEALTH_TARGET_VERSION) else ability_id
        check(identity not in seen, 'Duplicate ability/effect change')
        seen.add(identity)
        description = (change['descriptionFrom'], change['descriptionTo'])
        check(ability_id not in descriptions or descriptions[ability_id] == description, 'Conflicting ability descriptions')
        descriptions[ability_id] = description
        check(ability_id in owner['abilityIds'] and
              sum(ability_id in p['abilityIds'] for p in profiles) == 1, 'Ability is not exclusive to this guardian')
        ability = unique(abilities, 'id', ability_id)
        effect = unique(ability['effects'], 'id', change['effectId'])
        operations = ('Damage', 'ConsumeConditionStacks') if plan['version'] == CONSUMPTION_VERSION else ('Damage',)
        check(effect.get('operation') in operations and effect.get('scalingAttribute') == 'Power',
              'Only declared-version Power damage coefficients may change')
        if effect['operation'] == 'ConsumeConditionStacks':
            check(type(effect.get('baseValue')) is int and effect['baseValue'] > 0 and
                  type(effect.get('condition')) is str and bool(effect['condition'].strip()),
                  'Condition consumption requires a positive integer stack limit and a condition')
        if health_target:
            check(change['targetFrom'] == effect.get('target') == 'LowestCurrentHealthEnemy' and
                  change['targetTo'] == 'LowestHealthEnemy',
                  'Only absolute-to-percentage lowest-health enemy targeting is supported')
            coefficient = effect.get('scalingCoefficient')
            check(type(coefficient) in (int, float) and math.isfinite(coefficient) and coefficient > 0 and
                  effect.get('intervalTicks', 0) == effect.get('durationTicks', 0) == 0,
                  'Health targeting requires positive direct Power damage without periodic timing')
        else:
            before, after = change['from'], change['to']
            check(all(type(value) in (int, float) and math.isfinite(value) and value > 0 for value in (before, after)),
                  'Coefficients must be finite positive numbers')
            check(before == effect['scalingCoefficient'] and before != after and 0.25 <= after / before <= 4,
                  'Wrong original, unchanged or out-of-range coefficient')
        if plan['version'] == STATUS_SCALED_VERSION:
            status_before, status_after = change['statusScalingFrom'], change['statusScalingTo']
            check(type(effect.get('scalingStatusId')) is str and bool(effect['scalingStatusId'].strip()) and
                  effect.get('statusScalingAttribute', 'Power') == 'Power' and
                  all(type(v) in (int, float) and math.isfinite(v) and v > 0 for v in (status_before, status_after)) and
                  status_before == effect.get('statusScalingCoefficient') and
                  math.isclose(status_after / status_before, after / before, rel_tol=1e-12, abs_tol=0),
                  'Status damage scaling must preserve the original per-stack ratio')
        if springtide_ramp:
            # A separately declared experiment; v3's proportional guard stays intact.
            status = change['statusScalingTo']
            allowed = (.35,) if plan['version'] == SPRINGTIDE_RAMP_VERSION else (.45, .55)
            check(type(status) in (int, float) and status in allowed,
                  'Only the declared Springtide coefficients are supported')
            check(ability_id == 'ability.creature.eydis.springtide' and
                  change['effectId'] == 'effect.creature.eydis.springtide.damage' and
                  all(type(change[k]) in (int, float) and math.isfinite(change[k]) and change[k] == value
                      for k, value in (('from', 1), ('to', .25), ('statusScalingFrom', .2))) and
                  effect.get('statusScalingCoefficient') == .2 and
                  effect.get('scalingStatusId') == 'status.eydis.abundance' and
                  effect.get('statusScalingAttribute', 'Power') == 'Power' and
                  effect.get('scalingAttributeSubject', 'Source') == 'Source' and
                  effect.get('scalingStatusSubject', 'Source') == 'Source' and
                  effect.get('target') == 'AllEnemies' and effect.get('damageType') == 'Magical' and
                  effect.get('intervalTicks', 0) == effect.get('durationTicks', 0) == 0 and
                  ability.get('cooldownTicks') == 120 and
                  change['descriptionTo'] == f'Deal 25% Power as Magical Damage plus {round(status*100)}% Power per stack of Abundance to all enemies.',
                  'Only the declared Springtide base/per-Abundance tradeoff is supported')
        check(change['descriptionFrom'] == ability['description'] and
              type(change['descriptionTo']) is str and 0 < len(change['descriptionTo']) <= 2000 and
              change['descriptionTo'] != change['descriptionFrom'], 'Matching updated description required')
    return abilities


def materialize(api, isolated, plan, floor):
    if plan.get('version') == NI_COPY_HEALTH_VERSION:
        return ni_copy_health_module().materialize(api, isolated, plan, floor)
    if plan.get('version') == KODOKU_MIDPOINT_VERSION:
        return kodoku_midpoint_module().materialize(api, isolated, plan, floor)
    if plan.get('version') == KODOKU_REFINEMENT_VERSION:
        return kodoku_refinement_module().materialize(api, isolated, plan, floor)
    if plan.get('version') == KODOKU_OFFENSE_VERSION:
        return kodoku_offense_module().materialize(api, isolated, plan, floor)
    """Caller first copies source content; only the declared ability fields are written."""
    if plan.get('version') == SHARED_PENETRATION_VERSION:
        return shared_penetration_module().materialize(api, isolated, plan, floor)
    if plan.get('version') == MIASMA_VERSION:
        return miasma_module().materialize(api, isolated, plan, floor)
    check(api.resolve() != isolated.resolve(), 'Candidate must be isolated')
    abilities = copy.deepcopy(validate(api, plan, floor))
    check(sha(isolated / ABILITIES) == plan['sourceAbilitiesSha256'], 'Isolated source changed')
    for change in plan['changes']:
        ability = unique(abilities, 'id', change['abilityId'])
        effect = unique(ability['effects'], 'id', change['effectId'])
        if plan['version'] == HEALTH_TARGET_VERSION:
            effect['target'] = change['targetTo']
        else:
            effect['scalingCoefficient'] = change['to']
        if plan['version'] in (STATUS_SCALED_VERSION, SPRINGTIDE_RAMP_VERSION, SPRINGTIDE_REFINEMENT_VERSION):
            effect['statusScalingCoefficient'] = change['statusScalingTo']
        ability['description'] = change['descriptionTo']
    raw = (api / ABILITIES).read_bytes().decode('utf-8')
    for change in plan['changes']:
        ability_id = re.escape(json.dumps(change['abilityId']))
        effect_id = re.escape(json.dumps(change['effectId']))
        coefficient = rf'("id"\s*:\s*{ability_id}\s*,.*?"id"\s*:\s*{effect_id}\s*,.*?"scalingCoefficient"\s*:\s*)[0-9.eE+-]+'
        description = rf'("id"\s*:\s*{ability_id}\s*,.*?"description"\s*:\s*)"(?:[^"\\]|\\.)*"'
        if plan['version'] == HEALTH_TARGET_VERSION:
            target = rf'("id"\s*:\s*{ability_id}\s*,.*?"id"\s*:\s*{effect_id}\s*,.*?"target"\s*:\s*)"(?:[^"\\]|\\.)*"'
            edits = [(target, change['targetTo']), (description, change['descriptionTo'])]
        else:
            edits = [(coefficient, change['to']), (description, change['descriptionTo'])]
        if plan['version'] in (STATUS_SCALED_VERSION, SPRINGTIDE_RAMP_VERSION, SPRINGTIDE_REFINEMENT_VERSION):
            status = rf'("id"\s*:\s*{ability_id}\s*,.*?"id"\s*:\s*{effect_id}\s*,.*?"statusScalingCoefficient"\s*:\s*)[0-9.eE+-]+'
            edits.append((status, change['statusScalingTo']))
        for pattern, value in edits:
            raw, count = re.subn(pattern, lambda match: match[1] + json.dumps(value), raw, flags=re.DOTALL)
            check(count == 1, 'Ambiguous textual candidate edit')
    check(json.loads(raw.lstrip('\ufeff')) == abilities, 'Text edits do not reproduce declared candidate')
    (isolated / ABILITIES).write_bytes(raw.encode('utf-8'))
    verify(api, isolated, plan, floor)


def verify(api, candidate, plan, floor):
    if plan.get('version') == NI_COPY_HEALTH_VERSION:
        return ni_copy_health_module().verify(api, candidate, plan, floor)
    if plan.get('version') == KODOKU_MIDPOINT_VERSION:
        return kodoku_midpoint_module().verify(api, candidate, plan, floor)
    if plan.get('version') == KODOKU_REFINEMENT_VERSION:
        return kodoku_refinement_module().verify(api, candidate, plan, floor)
    if plan.get('version') == KODOKU_OFFENSE_VERSION:
        return kodoku_offense_module().verify(api, candidate, plan, floor)
    if plan.get('version') == SHARED_PENETRATION_VERSION:
        return shared_penetration_module().verify(api, candidate, plan, floor)
    """Independently undo allowed deltas, comparing every other field and catalog byte."""
    if plan.get('version') == MIASMA_VERSION:
        return miasma_module().verify(api, candidate, plan, floor)
    original = validate(api, plan, floor)
    def catalogs(folder):
        return {p.relative_to(folder) for p in (folder / 'Data').rglob('*.json')}
    files = catalogs(api)
    if plan['version'] in (SPRINGTIDE_RAMP_VERSION, SPRINGTIDE_REFINEMENT_VERSION):
        check(sha(api / 'appsettings.json') == sha(candidate / 'appsettings.json'), 'Undeclared settings change')
    check(files == catalogs(candidate), 'Candidate catalog files added or removed')
    for relative in files - {ABILITIES}:
        check(sha(api / relative) == sha(candidate / relative), 'Undeclared catalog change: ' + str(relative))
    restored = read(candidate / ABILITIES)
    descriptions = {}
    for change in plan['changes']:
        ability = unique(restored, 'id', change['abilityId'])
        effect = unique(ability['effects'], 'id', change['effectId'])
        check(ability['description'] == change['descriptionTo'], 'Declared candidate description was not reproduced')
        if plan['version'] == HEALTH_TARGET_VERSION:
            check(effect['target'] == change['targetTo'], 'Declared target was not reproduced')
            effect['target'] = change['targetFrom']
        else:
            check(effect['scalingCoefficient'] == change['to'], 'Declared candidate was not reproduced')
            effect['scalingCoefficient'] = change['from']
        if plan['version'] in (STATUS_SCALED_VERSION, SPRINGTIDE_RAMP_VERSION, SPRINGTIDE_REFINEMENT_VERSION):
            check(effect['statusScalingCoefficient'] == change['statusScalingTo'], 'Declared status scaling was not reproduced')
            effect['statusScalingCoefficient'] = change['statusScalingFrom']
        descriptions[change['abilityId']] = change['descriptionFrom']
    for ability_id, description in descriptions.items():
        unique(restored, 'id', ability_id)['description'] = description
    check(restored == original, 'Undeclared ability, timing, targeting or mechanic change')
    return dict(version=plan['version'], floor=floor, changes=len(plan['changes']), catalogs=len(files),
                sourceAbilitiesSha256=sha(api / ABILITIES), candidateAbilitiesSha256=sha(candidate / ABILITIES))
