"""Exact floor-7 Springtide midpoint plus the declared Endless Spring recovery edit."""
import importlib.util
import json
from pathlib import Path

s = importlib.util.spec_from_file_location('recovery_health', Path(__file__).with_name('tower-health-pressure-candidate.py'))
h = importlib.util.module_from_spec(s)
s.loader.exec_module(h)
VERSION = 'tower-recovery-pressure-candidate-v1'
ENDLESS = 'ability.creature.eydis.endless_spring'
HEAL = 'effect.creature.eydis.endless_spring.heal'
ABUNDANCE = 'effect.creature.eydis.endless_spring.abundance'
DESCRIPTION = 'Every 10 seconds, gain 1 Abundance, then heal for {statusScaling} of Max Health per Abundance stack.'


def validate_mode(mode, source, candidate, health, offense, penetration, ability, health_candidate, imports, current, projections):
    if candidate is not None:
        h.validate_mode(mode, source, candidate, health, offense, penetration, ability, imports, current, projections)
        h.check(health_candidate is None, 'Recovery pressure cannot combine candidate flags')


def expected(api, plan, floor):
    h.check(set(plan) == {'version', 'floor', 'healthPressurePlan', 'recoveryChange'}, 'Unexpected recovery candidate fields')
    h.check(plan['version'] == VERSION and type(plan['floor']) is int and plan['floor'] == floor == 7,
            'Fixed floor-7 recovery contract required')
    health = plan['healthPressurePlan']
    data = h.expected(api, health, floor)
    change = health['change']
    h.check(health['offenseFactor'] == .6 and health['penetrationFactor'] == 50 and
            change['abilityId'] == 'ability.creature.eydis.springtide' and
            change['effectId'] == 'effect.creature.eydis.springtide.damage' and change['from'] == 1 and change['to'] == .2125 and
            change['descriptionFrom'] == 'Deal 100% Magical Damage plus {statusScaling} per stack of Abundance to all enemies.' and
            change['descriptionTo'] == "Deal 21.25% of each target's Max Health as Magical Damage plus 20% Power per stack of Abundance to all enemies.",
            'Exact declared Springtide midpoint required')
    damage = h.unique(h.unique(data[h.ABILITIES], 'id', change['abilityId'])['effects'], 'id', change['effectId'])
    h.check(damage['statusScalingCoefficient'] == .2 and damage['scalingStatusId'] == 'status.eydis.abundance',
            'Original Abundance damage bonus required')
    recovery = plan['recoveryChange']
    h.check(recovery == dict(abilityId=ENDLESS, effectId=HEAL, field='statusScalingCoefficient', fromValue=.01, toValue=.0075),
            'Only the declared 1% to 0.75% recovery edit is permitted')
    owner = h.unique(h.read(api / 'Data/combat/creature-abilities.json')['creatures'], 'monsterId',
                     h.unique(data[h.TOWER]['floors'], 'floorNumber', floor)['guardianAbilityProfileId'])
    profiles = h.read(api / 'Data/combat/creature-abilities.json')['creatures']
    h.check(ENDLESS in owner['abilityIds'] and sum(ENDLESS in p['abilityIds'] for p in profiles) == 1,
            'Unique guardian recovery ability required')
    ability = h.unique(data[h.ABILITIES], 'id', ENDLESS)
    h.check(ability['description'] == DESCRIPTION and ability['kind'] == 'Passive' and ability['cooldownTicks'] == 0 and
            ability['triggers'] == [dict(event='OnInterval', internalCooldownTicks=100, initialDelayTicks=100,
                                        effectIds=[ABUNDANCE, HEAL])], 'Original recovery description, interval and ordering required')
    h.check(ability['effects'] == [
        dict(id=ABUNDANCE, operation='ApplyStatus', target='Self', baseValue=1, statusId='status.eydis.abundance', procCoefficient=1),
        dict(id=HEAL, operation='Heal', target='Self', scalingStatusId='status.eydis.abundance',
             statusScalingAttribute='MaxHealth', statusScalingCoefficient=.01, critEligibility='Disallowed', procCoefficient=1)
    ], 'Original recovery, Abundance generation and noncritical self-heal required')
    ability['effects'][1]['statusScalingCoefficient'] = .0075
    return data


def materialize(api, isolated, plan, floor):
    h.check(api.resolve() != isolated.resolve(), 'Candidate must be isolated')
    data = expected(api, plan, floor)
    for relative, value in data.items():
        h.check(h.sha(api / relative) == h.sha(isolated / relative), 'Isolated source changed')
        (isolated / relative).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
    return verify(api, isolated, plan, floor)


def verify(api, candidate, plan, floor):
    data = expected(api, plan, floor)
    catalogs = {p.relative_to(api) for p in (api / 'Data').rglob('*.json')}
    h.check(catalogs == {p.relative_to(candidate) for p in (candidate / 'Data').rglob('*.json')}, 'Candidate catalog set changed')
    for relative in catalogs:
        if relative in data:
            h.check(h.read(candidate / relative) == data[relative], 'Undeclared candidate delta: ' + str(relative))
        else:
            h.check(h.sha(api / relative) == h.sha(candidate / relative), 'Unrelated catalog changed: ' + str(relative))
    h.check(h.sha(api / 'appsettings.json') == h.sha(candidate / 'appsettings.json'), 'Settings changed')
    return dict(version=VERSION, floor=floor, catalogs=len(catalogs),
                candidateAbilitiesSha256=h.sha(candidate / h.ABILITIES), candidateTowerSha256=h.sha(candidate / h.TOWER))
