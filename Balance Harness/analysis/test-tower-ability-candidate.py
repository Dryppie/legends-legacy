"""Corruption and isolation checks for Tower ability coefficient candidates; zero combat."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

HERE = Path(__file__).resolve().parent


def module(name):
    spec = importlib.util.spec_from_file_location(name.replace('-', '_'), HERE / (name + '.py'))
    loaded = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(loaded)
    return loaded


candidate = module('tower-ability-candidate')
owner = module('run-tower-balance-pass')


class CandidateTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name) / 'source'
        self.target = Path(self.temp.name) / 'candidate'
        self.abilities = [dict(id='spell', description='50% damage', cooldownTicks=160,
            effects=[dict(id='pulse', operation='Damage', scalingAttribute='Power', scalingCoefficient=.5,
                          intervalTicks=20, target='AllEnemies', damageType='Magical'),
                     dict(id='barrier', operation='GrantBarrier', scalingAttribute='MaxHealth', scalingCoefficient=.05)])]
        self.save(self.source / candidate.ABILITIES, self.abilities)
        self.save(self.source / candidate.TOWER, dict(floors=[dict(floorNumber=5,
            guardianAbilityProfileId='guardian', guardianScaling=dict(health=3, offense=4)),
            dict(floorNumber=6, guardianAbilityProfileId='other', guardianScaling=dict(health=6, offense=5))]))
        self.save(self.source / 'Data/combat/creature-abilities.json', dict(creatures=[dict(monsterId='guardian', abilityIds=['spell'])]))
        self.save(self.source / 'Data/other.json', dict(untouched=True))
        self.plan = dict(version=candidate.VERSION, floor=5,
            sourceAbilitiesSha256=candidate.sha(self.source / candidate.ABILITIES),
            sourceTowerSha256=candidate.sha(self.source / candidate.TOWER), changes=[dict(
                abilityId='spell', effectId='pulse', **{'from': .5, 'to': .4},
                descriptionFrom='50% damage', descriptionTo='40% damage')])
        shutil.copytree(self.source, self.target)

    def save(self, path, data):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(data), encoding='utf-8')

    def apply(self):
        candidate.materialize(self.source, self.target, self.plan, 5)

    def verify(self):
        return candidate.verify(self.source, self.target, self.plan, 5)

    def test_exact_changes_preserve_source_and_all_other_fields(self):
        before = {p.relative_to(self.source): p.read_bytes() for p in self.source.rglob('*.json')}
        self.apply()
        self.assertEqual(1, self.verify()['changes'])
        self.assertEqual(before, {p.relative_to(self.source): p.read_bytes() for p in self.source.rglob('*.json')})
        result = candidate.read(self.target / candidate.ABILITIES)[0]
        self.assertEqual(.4, result['effects'][0]['scalingCoefficient'])
        self.assertEqual('40% damage', result['description'])
        self.assertEqual(160, result['cooldownTicks'])
        self.assertEqual(.05, result['effects'][1]['scalingCoefficient'])

    def test_no_live_write(self):
        with self.assertRaisesRegex(ValueError, 'isolated'):
            candidate.materialize(self.source, self.source, self.plan, 5)

    def test_edits_preserve_unrelated_formatting(self):
        raw = json.dumps(self.abilities, indent=4).replace('\n', '\r\n') + '\r\n'
        for folder in (self.source, self.target):
            (folder / candidate.ABILITIES).write_bytes(raw.encode())
        self.plan['sourceAbilitiesSha256'] = candidate.sha(self.source / candidate.ABILITIES)
        self.apply()
        expected = raw.replace('50% damage', '40% damage').replace('"scalingCoefficient": 0.5,', '"scalingCoefficient": 0.4,')
        self.assertEqual(expected.encode(), (self.target / candidate.ABILITIES).read_bytes())

    def test_wrong_pin_or_floor(self):
        for field, value in [('sourceAbilitiesSha256', '0'*64), ('sourceTowerSha256', '0'*64), ('floor', 6)]:
            with self.subTest(field=field), self.assertRaises(ValueError):
                candidate.validate(self.source, {**self.plan, field: value}, 5)

    def test_unknown_fields(self):
        for plan in [{**self.plan, 'cooldown': 10}, {**self.plan, 'changes': [{**self.plan['changes'][0], 'target': 'Self'}]}]:
            with self.assertRaises(ValueError):
                candidate.validate(self.source, plan, 5)

    def test_invalid_coefficients(self):
        for value in [True, 0, -1, float('nan'), float('inf'), .01, 10, .5, '0.4']:
            with self.subTest(value=value), self.assertRaises(ValueError):
                candidate.validate(self.source, {**self.plan, 'changes': [{**self.plan['changes'][0], 'to': value}]}, 5)

    def test_wrong_original_or_missing_identity(self):
        for change in [{'from': .6}, {'abilityId': 'unknown'}, {'effectId': 'unknown'}]:
            with self.subTest(change=change), self.assertRaises(ValueError):
                candidate.validate(self.source, {**self.plan, 'changes': [{**self.plan['changes'][0], **change}]}, 5)

    def test_non_damage_effect_is_rejected(self):
        plan = copy.deepcopy(self.plan)
        plan['changes'][0].update(effectId='barrier', **{'from': .05, 'to': .04})
        with self.assertRaisesRegex(ValueError, 'Power damage'):
            candidate.validate(self.source, plan, 5)

    def test_duplicate_changes_and_identity(self):
        with self.assertRaises(ValueError):
            candidate.validate(self.source, {**self.plan, 'changes': self.plan['changes'] * 2}, 5)
        self.save(self.source / candidate.ABILITIES, self.abilities * 2)
        self.plan['sourceAbilitiesSha256'] = candidate.sha(self.source / candidate.ABILITIES)
        with self.assertRaisesRegex(ValueError, 'ambiguous'):
            candidate.validate(self.source, self.plan, 5)

    def test_shared_ability_is_rejected(self):
        self.save(self.source / 'Data/combat/creature-abilities.json', dict(creatures=[
            dict(monsterId='guardian', abilityIds=['spell']), dict(monsterId='another', abilityIds=['spell'])]))
        with self.assertRaisesRegex(ValueError, 'exclusive'):
            candidate.validate(self.source, self.plan, 5)

    def test_shared_floor_profile_is_rejected(self):
        tower = candidate.read(self.source / candidate.TOWER)
        tower['floors'][1]['guardianAbilityProfileId'] = 'guardian'
        self.save(self.source / candidate.TOWER, tower)
        self.plan['sourceTowerSha256'] = candidate.sha(self.source / candidate.TOWER)
        with self.assertRaisesRegex(ValueError, 'shared across floors'):
            candidate.validate(self.source, self.plan, 5)

    def test_description_must_be_explicit(self):
        for fields in [dict(descriptionFrom='stale'), dict(descriptionTo='50% damage'), dict(descriptionTo='')]:
            with self.subTest(fields=fields), self.assertRaises(ValueError):
                candidate.validate(self.source, {**self.plan, 'changes': [{**self.plan['changes'][0], **fields}]}, 5)

    def test_verifier_rejects_unapplied_or_wrong_coefficient(self):
        with self.assertRaisesRegex(ValueError, 'reproduced'):
            self.verify()
        self.apply()
        changed = candidate.read(self.target / candidate.ABILITIES)
        changed[0]['effects'][0]['scalingCoefficient'] = .3
        self.save(self.target / candidate.ABILITIES, changed)
        with self.assertRaisesRegex(ValueError, 'reproduced'):
            self.verify()

    def test_verifier_rejects_hidden_mechanic_changes(self):
        self.apply()
        original = candidate.read(self.target / candidate.ABILITIES)
        for mutate in [lambda a: a[0].update(cooldownTicks=140),
                       lambda a: a[0]['effects'][0].update(target='Self'),
                       lambda a: a[0]['effects'][1].update(scalingCoefficient=.1)]:
            changed = copy.deepcopy(original)
            mutate(changed)
            self.save(self.target / candidate.ABILITIES, changed)
            with self.assertRaisesRegex(ValueError, 'Undeclared'):
                self.verify()

    def test_verifier_rejects_other_catalog_or_floor_changes(self):
        self.apply()
        self.save(self.target / 'Data/other.json', dict(untouched=False))
        with self.assertRaisesRegex(ValueError, 'Undeclared catalog'):
            self.verify()

    def test_verifier_rejects_added_and_removed_catalogs(self):
        self.apply()
        extra = self.target / 'Data/extra.json'
        self.save(extra, {})
        with self.assertRaisesRegex(ValueError, 'added or removed'):
            self.verify()
        extra.unlink()
        (self.target / 'Data/other.json').unlink()
        with self.assertRaisesRegex(ValueError, 'added or removed'):
            self.verify()

    def test_owner_rejects_implicit_combinations_before_allocation(self):
        owner.validate_ability_mode('screen', self.source, self.plan, 1, 1, [], False)
        owner.validate_ability_mode('confirm', self.source, self.plan, 1, 1, [], False)
        for args in [('prepare', self.source, 1, 1, [], False), ('search', self.source, 1, 1, [], False),
                     ('screen', None, 1, 1, [], False), ('screen', self.source, 1.05, 1, [], False),
                     ('screen', self.source, 1, .95, [], False), ('screen', self.source, 1, 1, ['import'], False),
                     ('screen', self.source, 1, 1, [], True)]:
            mode, source, health, offense, imports, current = args
            with self.subTest(args=args), self.assertRaises(ValueError):
                owner.validate_ability_mode(mode, source, self.plan, health, offense, imports, current)

    def configure_consumption(self):
        self.abilities[0]['effects'][0].update(operation='ConsumeConditionStacks', condition='Bleed',
            baseValue=3, healingScalingAttribute='MaxHealth', healingScalingCoefficient=.005,
            maximumHealingScalingCoefficient=.04)
        for folder in (self.source, self.target):
            self.save(folder / candidate.ABILITIES, self.abilities)
        self.plan['sourceAbilitiesSha256'] = candidate.sha(self.source / candidate.ABILITIES)
        self.plan['version'] = candidate.CONSUMPTION_VERSION

    def test_v1_still_rejects_condition_consumption(self):
        self.configure_consumption()
        self.plan['version'] = candidate.VERSION
        with self.assertRaisesRegex(ValueError, 'Power damage'):
            self.apply()

    def test_v2_consumption_preserves_stack_healing_and_other_fields(self):
        self.configure_consumption()
        before = copy.deepcopy(self.abilities)
        original = (self.source / candidate.ABILITIES).read_bytes()
        self.apply()
        self.assertEqual(candidate.CONSUMPTION_VERSION, self.verify()['version'])
        expected = copy.deepcopy(before)
        expected[0]['description'] = '40% damage'
        expected[0]['effects'][0]['scalingCoefficient'] = .4
        self.assertEqual(expected, candidate.read(self.target / candidate.ABILITIES))
        self.assertEqual(original, (self.source / candidate.ABILITIES).read_bytes())

    def test_v2_verifier_rejects_stack_heal_and_mechanic_deltas(self):
        self.configure_consumption()
        self.apply()
        original = candidate.read(self.target / candidate.ABILITIES)
        for field, value in [('baseValue', 4), ('condition', 'Poison'), ('healingScalingAttribute', 'Power'),
                             ('healingScalingCoefficient', .01), ('maximumHealingScalingCoefficient', .08),
                             ('operation', 'Damage'), ('target', 'Self'), ('damageType', 'Physical'),
                             ('intervalTicks', 10)]:
            changed = copy.deepcopy(original)
            changed[0]['effects'][0][field] = value
            self.save(self.target / candidate.ABILITIES, changed)
            with self.subTest(field=field), self.assertRaisesRegex(ValueError, 'Undeclared'):
                self.verify()

    def test_v2_rejects_invalid_stack_limit_and_condition(self):
        self.configure_consumption()
        original = copy.deepcopy(self.abilities)
        for field, value in [('baseValue', True), ('baseValue', 0), ('baseValue', -1), ('baseValue', 2.5),
                             ('condition', None), ('condition', ''), ('condition', ' ')]:
            changed = copy.deepcopy(original)
            changed[0]['effects'][0][field] = value
            self.save(self.source / candidate.ABILITIES, changed)
            self.plan['sourceAbilitiesSha256'] = candidate.sha(self.source / candidate.ABILITIES)
            with self.subTest(field=field, value=value), self.assertRaisesRegex(ValueError, 'stack limit'):
                candidate.validate(self.source, self.plan, 5)

    def test_v2_consumption_still_requires_power_damage(self):
        self.configure_consumption()
        self.abilities[0]['effects'][0]['scalingAttribute'] = 'MaxHealth'
        self.save(self.source / candidate.ABILITIES, self.abilities)
        self.plan['sourceAbilitiesSha256'] = candidate.sha(self.source / candidate.ABILITIES)
        with self.assertRaisesRegex(ValueError, 'Power damage'):
            candidate.validate(self.source, self.plan, 5)

    def test_v2_joint_direct_and_consumption_candidate(self):
        self.configure_consumption()
        self.abilities.append(dict(id='direct', description='65% damage', effects=[dict(id='hit',
            operation='Damage', scalingAttribute='Power', scalingCoefficient=.65, target='AllEnemies')]))
        for folder in (self.source, self.target):
            self.save(folder / candidate.ABILITIES, self.abilities)
            self.save(folder / 'Data/combat/creature-abilities.json', dict(creatures=[dict(monsterId='guardian', abilityIds=['spell','direct'])]))
        self.plan['sourceAbilitiesSha256'] = candidate.sha(self.source / candidate.ABILITIES)
        self.plan['changes'].append(dict(abilityId='direct', effectId='hit', **{'from': .65, 'to': .5},
            descriptionFrom='65% damage', descriptionTo='50% damage'))
        self.apply()
        self.assertEqual(2, self.verify()['changes'])
        self.assertEqual(.5, candidate.read(self.target / candidate.ABILITIES)[1]['effects'][0]['scalingCoefficient'])

    def test_unknown_candidate_version_is_rejected(self):
        self.plan['version'] = 'tower-ability-coefficients-v999'
        with self.assertRaisesRegex(ValueError, 'version'):
            candidate.validate(self.source, self.plan, 5)

    def configure_status_scaled(self):
        pulse=self.abilities[0]['effects'][0]
        pulse.update(scalingStatusId='magical-charge',statusScalingCoefficient=.005)
        physical=copy.deepcopy(pulse)
        physical.update(id='physical',damageType='Physical',scalingStatusId='physical-charge')
        self.abilities[0]['effects'].append(physical)
        for folder in (self.source,self.target): self.save(folder/candidate.ABILITIES,self.abilities)
        self.plan['sourceAbilitiesSha256']=candidate.sha(self.source/candidate.ABILITIES)
        self.plan['version']=candidate.STATUS_SCALED_VERSION
        self.plan['changes']=[dict(abilityId='spell',effectId=effect,**{'from':.5,'to':coefficient},
            statusScalingFrom=.005,statusScalingTo=status,descriptionFrom='50% damage',
            descriptionTo='65% magical and 25% physical damage') for effect,coefficient,status in
                [('pulse',.65,.0065),('physical',.25,.0025)]]

    def test_v3_two_effects_preserve_status_ratio_and_source_bytes(self):
        self.configure_status_scaled()
        before=(self.source/candidate.ABILITIES).read_bytes()
        self.apply();self.assertEqual(2,self.verify()['changes'])
        self.assertEqual(before,(self.source/candidate.ABILITIES).read_bytes())
        actual=candidate.read(self.target/candidate.ABILITIES)[0]
        for effect in (actual['effects'][0],actual['effects'][2]):
            self.assertAlmostEqual(.01,effect['statusScalingCoefficient']/effect['scalingCoefficient'])
        self.assertEqual(self.abilities[0]['effects'][1],actual['effects'][1])

    def test_v3_rejects_uncoupled_stale_or_invalid_status_scaling(self):
        self.configure_status_scaled();original=copy.deepcopy(self.plan)
        for field,value in [('statusScalingFrom',.004),('statusScalingTo',.005),('statusScalingTo',True),
                ('statusScalingTo',0),('statusScalingTo',float('nan')),('statusScalingTo',float('inf'))]:
            self.plan=copy.deepcopy(original);self.plan['changes'][0][field]=value
            with self.subTest(field=field,value=value),self.assertRaisesRegex(ValueError,'per-stack ratio'):self.apply()

    def test_v3_rejects_missing_status_and_non_power_status_attribute(self):
        self.configure_status_scaled();original=copy.deepcopy(self.abilities)
        for field,value in [('scalingStatusId',''),('statusScalingAttribute','MaxHealth')]:
            changed=copy.deepcopy(original);changed[0]['effects'][0][field]=value
            self.save(self.source/candidate.ABILITIES,changed)
            self.plan['sourceAbilitiesSha256']=candidate.sha(self.source/candidate.ABILITIES)
            with self.assertRaisesRegex(ValueError,'per-stack ratio'):candidate.validate(self.source,self.plan,5)

    def test_v3_rejects_duplicate_effect_or_conflicting_description(self):
        self.configure_status_scaled();original=copy.deepcopy(self.plan)
        for change in ('duplicate','description'):
            self.plan=copy.deepcopy(original)
            if change=='duplicate':self.plan['changes'][1]=copy.deepcopy(self.plan['changes'][0])
            else:self.plan['changes'][1]['descriptionTo']='different'
            with self.assertRaisesRegex(ValueError,'Duplicate|Conflicting'):self.apply()

    def test_old_versions_do_not_admit_v3_fields_or_multiple_effects(self):
        self.configure_status_scaled();original=copy.deepcopy(self.plan)
        for version in (candidate.VERSION,candidate.CONSUMPTION_VERSION):
            self.plan=copy.deepcopy(original);self.plan['version']=version
            with self.assertRaisesRegex(ValueError,'fields'):self.apply()
            for change in self.plan['changes']:
                del change['statusScalingFrom'];del change['statusScalingTo']
            with self.assertRaisesRegex(ValueError,'Duplicate'):self.apply()

    def test_v3_verifier_rejects_status_identity_timing_or_unapplied_scaling(self):
        self.configure_status_scaled();self.apply();original=candidate.read(self.target/candidate.ABILITIES)
        for field,value in [('statusScalingCoefficient',.005),('scalingStatusId','other'),
                ('target','Self'),('intervalTicks',10)]:
            changed=copy.deepcopy(original);changed[0]['effects'][0][field]=value
            self.save(self.target/candidate.ABILITIES,changed)
            with self.subTest(field=field),self.assertRaises(ValueError):self.verify()


    def configure_health_target(self):
        effect = self.abilities[0]['effects'][0]
        effect.update(target='LowestCurrentHealthEnemy', intervalTicks=0,
                      scalingStatusId='magical-charge', statusScalingCoefficient=.005)
        for folder in (self.source, self.target): self.save(folder / candidate.ABILITIES, self.abilities)
        self.plan.update(version=candidate.HEALTH_TARGET_VERSION,
            sourceAbilitiesSha256=candidate.sha(self.source / candidate.ABILITIES),
            changes=[dict(abilityId='spell', effectId='pulse', targetFrom='LowestCurrentHealthEnemy',
                targetTo='LowestHealthEnemy', descriptionFrom='50% damage',
                descriptionTo='50% damage to the enemy with the lowest health percentage')])

    def test_health_target_preserves_damage_status_timing_and_source_bytes(self):
        self.configure_health_target(); before=(self.source / candidate.ABILITIES).read_bytes()
        self.apply(); self.assertEqual(1,self.verify()['changes'])
        self.assertEqual(before,(self.source / candidate.ABILITIES).read_bytes())
        changed=candidate.read(self.target / candidate.ABILITIES)
        self.assertEqual('LowestHealthEnemy',changed[0]['effects'][0]['target'])
        changed[0]['effects'][0]['target']='LowestCurrentHealthEnemy'
        changed[0]['description']='50% damage'
        self.assertEqual(changed,self.abilities)

    def test_health_target_rejects_stale_noop_and_unsupported_selectors(self):
        self.configure_health_target(); original=copy.deepcopy(self.plan)
        for field,value in [('targetFrom','LowestHealthEnemy'),('targetTo','LowestCurrentHealthEnemy'),
                ('targetTo','HighestHealthEnemy'),('targetTo','Self'),('targetTo',None)]:
            self.plan=copy.deepcopy(original); self.plan['changes'][0][field]=value
            with self.subTest(field=field,value=value), self.assertRaisesRegex(ValueError,'absolute-to-percentage'):self.apply()

    def test_health_target_rejects_coefficient_fields_and_multiple_changes(self):
        self.configure_health_target(); original=copy.deepcopy(self.plan)
        for field in ('from','to','statusScalingFrom','statusScalingTo','cooldownTicks'):
            self.plan=copy.deepcopy(original);self.plan['changes'][0][field]=1
            with self.subTest(field=field),self.assertRaisesRegex(ValueError,'fields'):self.apply()
        self.plan=copy.deepcopy(original);self.plan['changes'].append(copy.deepcopy(self.plan['changes'][0]))
        with self.assertRaisesRegex(ValueError,'exactly one'):self.apply()

    def test_health_target_rejects_periodic_or_non_damage_effects(self):
        self.configure_health_target(); original=copy.deepcopy(self.abilities)
        for field,value in [('intervalTicks',10),('durationTicks',10),('operation','Heal'),
                ('operation','ConsumeConditionStacks'),('scalingAttribute','MaxHealth'),
                ('scalingCoefficient',0),('scalingCoefficient',True),('scalingCoefficient',float('nan'))]:
            changed=copy.deepcopy(original);changed[0]['effects'][0][field]=value
            self.save(self.source / candidate.ABILITIES,changed)
            self.plan['sourceAbilitiesSha256']=candidate.sha(self.source / candidate.ABILITIES)
            with self.subTest(field=field),self.assertRaisesRegex(ValueError,'Power damage|periodic timing'):
                candidate.validate(self.source,self.plan,5)

    def test_health_target_verify_rejects_damage_status_selector_and_flag_drift(self):
        self.configure_health_target();self.apply();original=candidate.read(self.target / candidate.ABILITIES)
        for field,value in [('scalingCoefficient',.4),('statusScalingCoefficient',.004),('scalingStatusId','other'),
                ('target','LowestCurrentHealthEnemy'),('durationTicks',5),('intervalTicks',10),
                ('ignoreTaunt',True),('useHealthPercentage',True),('randomizeTies',True),('excludeSummons',True)]:
            changed=copy.deepcopy(original);changed[0]['effects'][0][field]=value
            self.save(self.target / candidate.ABILITIES,changed)
            with self.subTest(field=field),self.assertRaises(ValueError):self.verify()
        changed=copy.deepcopy(original);changed[0]['cooldownTicks']=200
        self.save(self.target / candidate.ABILITIES,changed)
        with self.assertRaisesRegex(ValueError,'Undeclared'):self.verify()

    def test_coefficient_versions_cannot_admit_target_fields(self):
        self.configure_health_target(); original=copy.deepcopy(self.plan)
        for version in (candidate.VERSION,candidate.CONSUMPTION_VERSION,candidate.STATUS_SCALED_VERSION):
            self.plan=copy.deepcopy(original);self.plan['version']=version
            with self.subTest(version=version),self.assertRaisesRegex(ValueError,'fields'):self.apply()

    def test_health_target_requires_matching_live_description_and_exclusive_guardian(self):
        self.configure_health_target();self.plan['changes'][0]['descriptionFrom']='stale'
        with self.assertRaisesRegex(ValueError,'description'):self.apply()
        self.plan['changes'][0]['descriptionFrom']='50% damage'
        self.save(self.source / 'Data/combat/creature-abilities.json',dict(creatures=[
            dict(monsterId='guardian',abilityIds=['spell']),dict(monsterId='another',abilityIds=['spell'])]))
        with self.assertRaisesRegex(ValueError,'exclusive'):self.apply()


if __name__ == '__main__':
    unittest.main()
