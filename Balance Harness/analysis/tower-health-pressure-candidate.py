"""Explicit target-health damage plus reduced-offense/penetration Tower trial.

This format is separate from coefficient trials: it intentionally changes the
base's attribute subject while preserving the authored additive status bonus.
"""
import copy
import hashlib
import json
import math
from pathlib import Path

VERSION = 'tower-health-pressure-candidate-v1'
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
    check(len(found) == 1, 'Missing or ambiguous identity')
    return found[0]


def positive(value):
    return type(value) in (float, int) and math.isfinite(value) and value > 0


def validate_mode(mode, source, candidate, health, offense, penetration, ability, imports, current, projections):
    if candidate is not None:
        check(mode in ('screen', 'confirm') and source is not None and health == offense == penetration == 1
              and ability is None and not imports and not current and not projections,
              'Health pressure requires a fixed source screen/confirm and no other modifications')


def expected(api, plan, floor):
    check(set(plan) == {'version', 'floor', 'sourceAbilitiesSha256', 'sourceTowerSha256',
                        'offenseFactor', 'penetrationFactor', 'change'}, 'Unexpected candidate fields')
    check(plan['version'] == VERSION and type(plan['floor']) is int and plan['floor'] == floor,
          'Wrong candidate version or floor')
    check(sha(api / ABILITIES) == plan['sourceAbilitiesSha256'] and sha(api / TOWER) == plan['sourceTowerSha256'],
          'Candidate source changed')
    offense, penetration = plan['offenseFactor'], plan['penetrationFactor']
    check(positive(offense) and .25 <= offense < 1 and positive(penetration) and 1 < penetration <= 128,
          'Reduced offense and increased penetration required')
    tower, abilities = read(api / TOWER), read(api / ABILITIES)
    guardian = unique(tower['floors'], 'floorNumber', floor)
    profile = guardian['guardianAbilityProfileId']
    check(sum(f.get('guardianAbilityProfileId') == profile for f in tower['floors']) == 1, 'Shared guardian profile')
    profiles = read(api / 'Data/combat/creature-abilities.json')['creatures']
    owner = unique(profiles, 'monsterId', profile)
    change = plan['change']
    check(set(change) == {'abilityId', 'effectId', 'from', 'to', 'descriptionFrom', 'descriptionTo'},
          'Unexpected effect fields')
    aid = change['abilityId']
    check(aid in owner['abilityIds'] and sum(aid in p['abilityIds'] for p in profiles) == 1, 'Shared guardian ability')
    ability = unique(abilities, 'id', aid)
    effect = unique(ability['effects'], 'id', change['effectId'])
    check(effect.get('operation') == 'Damage' and effect.get('scalingAttribute') == 'Power'
          and effect.get('scalingAttributeSubject', 'Source') == 'Source'
          and effect.get('target') == 'AllEnemies' and effect.get('damageType') == 'Magical'
          and effect.get('intervalTicks', 0) == effect.get('durationTicks', 0) == 0,
          'Only direct source-Power magical all-enemy damage may become target-health damage')
    check(positive(change['from']) and change['from'] == effect.get('scalingCoefficient')
          and positive(change['to']) and change['to'] <= .5, 'Invalid original or target-health fraction')
    check(isinstance(effect.get('scalingStatusId'), str) and bool(effect['scalingStatusId'].strip())
          and positive(effect.get('statusScalingCoefficient'))
          and effect.get('statusScalingAttribute', 'Power') == 'Power'
          and effect.get('scalingStatusSubject', 'Source') == 'Source', 'Source-Power status bonus required')
    check(change['descriptionFrom'] == ability['description'] and isinstance(change['descriptionTo'], str)
          and 0 < len(change['descriptionTo']) <= 2000 and change['descriptionFrom'] != change['descriptionTo'],
          'Matching updated description required')
    scaling = guardian['guardianScaling']
    check(positive(scaling.get('offense')) and positive(scaling.get('penetration')), 'Authored guardian scaling required')
    scaling['offense'] = round(scaling['offense'] * offense, 10)
    scaling['penetration'] = round(scaling['penetration'] * penetration, 10)
    check(positive(scaling['offense']) and positive(scaling['penetration']), 'Nonfinite guardian result')
    effect.update(scalingAttribute='MaxHealth', scalingAttributeSubject='Target', scalingCoefficient=change['to'])
    ability['description'] = change['descriptionTo']
    return {ABILITIES: abilities, TOWER: tower}


def materialize(api, isolated, plan, floor):
    check(api.resolve() != isolated.resolve(), 'Candidate must be isolated')
    data = expected(api, plan, floor)
    for relative, value in data.items():
        check(sha(api / relative) == sha(isolated / relative), 'Isolated source changed')
        (isolated / relative).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
    return verify(api, isolated, plan, floor)


def verify(api, candidate, plan, floor):
    data = expected(api, plan, floor)
    def files(folder):
        return {p.relative_to(folder) for p in (folder / 'Data').rglob('*.json')}
    catalogs = files(api)
    check(catalogs == files(candidate), 'Candidate catalog set changed')
    for relative in catalogs:
        if relative in data:
            check(read(candidate / relative) == data[relative], 'Undeclared candidate delta: ' + str(relative))
        else:
            check(sha(api / relative) == sha(candidate / relative), 'Unrelated catalog changed: ' + str(relative))
    check(sha(api / 'appsettings.json') == sha(candidate / 'appsettings.json'), 'Settings changed')
    return dict(version=VERSION, floor=floor, catalogs=len(catalogs),
                candidateAbilitiesSha256=sha(candidate / ABILITIES), candidateTowerSha256=sha(candidate / TOWER))
