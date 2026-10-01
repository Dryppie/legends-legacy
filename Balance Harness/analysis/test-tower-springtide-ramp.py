"""Reject unrelated or ambiguous Springtide experiments before allocating fights."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

s = importlib.util.spec_from_file_location('candidate', Path(__file__).with_name('tower-ability-candidate.py'))
c = importlib.util.module_from_spec(s); s.loader.exec_module(c)


class SpringtideRampTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory(); self.addCleanup(temp.cleanup)
        self.source = Path(temp.name) / 'source'; self.target = Path(temp.name) / 'candidate'
        self.ability = dict(id='ability.creature.eydis.springtide', cooldownTicks=120, description='Original description',
            effects=[dict(id='effect.creature.eydis.springtide.damage', operation='Damage', target='AllEnemies',
                scalingAttribute='Power', scalingCoefficient=1., scalingStatusId='status.eydis.abundance',
                statusScalingAttribute='Power', statusScalingCoefficient=.2, damageType='Magical')])
        self.save(self.source / c.ABILITIES, [self.ability, dict(id='unrelated', effects=[])])
        self.save(self.source / c.TOWER, dict(floors=[dict(floorNumber=7, guardianAbilityProfileId='eydis')]))
        self.save(self.source / 'Data/combat/creature-abilities.json', dict(creatures=[dict(monsterId='eydis', abilityIds=[self.ability['id']])]))
        self.save(self.source / 'Data/other.json', dict(preserve=True))
        self.save(self.source / 'appsettings.json', dict(preserve=True))
        self.plan = dict(version=c.SPRINGTIDE_RAMP_VERSION, floor=7,
            sourceAbilitiesSha256=c.sha(self.source / c.ABILITIES), sourceTowerSha256=c.sha(self.source / c.TOWER),
            changes=[dict(abilityId=self.ability['id'], effectId=self.ability['effects'][0]['id'], **{'from':1., 'to':.25},
                statusScalingFrom=.2, statusScalingTo=.35, descriptionFrom=self.ability['description'],
                descriptionTo='Deal 25% Power as Magical Damage plus 35% Power per stack of Abundance to all enemies.')])
        shutil.copytree(self.source, self.target)

    def save(self, path, data):
        path.parent.mkdir(parents=True, exist_ok=True); path.write_text(json.dumps(data), encoding='utf-8')

    def apply(self, plan=None):
        c.materialize(self.source, self.target, plan or self.plan, 7)

    def verify(self):
        return c.verify(self.source, self.target, self.plan, 7)

    def test_exact_tradeoff_and_source_preservation(self):
        before = {str(p.relative_to(self.source)):p.read_bytes() for p in self.source.rglob('*.json')}
        self.apply(); self.assertEqual(c.SPRINGTIDE_RAMP_VERSION, self.verify()['version'])
        result = c.read(self.target / c.ABILITIES)
        expected = c.read(self.source / c.ABILITIES)
        expected[0]['description'] = self.plan['changes'][0]['descriptionTo']
        expected[0]['effects'][0].update(scalingCoefficient=.25, statusScalingCoefficient=.35)
        self.assertEqual(expected, result)
        self.assertEqual(before, {str(p.relative_to(self.source)):p.read_bytes() for p in self.source.rglob('*.json')})

    def test_v3_still_rejects_nonproportional_tradeoff(self):
        plan=copy.deepcopy(self.plan); plan['version']=c.STATUS_SCALED_VERSION
        with self.assertRaisesRegex(ValueError, 'per-stack ratio'): self.apply(plan)

    def test_wrong_floor_and_multiple_changes(self):
        for floor, changes in [(6,self.plan['changes']), (7,self.plan['changes']*2)]:
            plan={**self.plan, 'floor':floor, 'changes':changes}
            with self.subTest(floor=floor), self.assertRaises(ValueError): c.validate(self.source, plan, floor)

    def test_exact_coefficients_identity_and_description_required(self):
        for field, value in [('from',True), ('to',.3), ('statusScalingFrom',.19), ('statusScalingTo',.3),
                ('statusScalingTo',True), ('statusScalingTo',float('nan')), ('statusScalingTo',float('inf')),
                ('abilityId','unrelated'), ('effectId','other'), ('descriptionFrom','stale'), ('descriptionTo','misleading')]:
            plan=copy.deepcopy(self.plan); plan['changes'][0][field]=value
            with self.subTest(field=field,value=value), self.assertRaises(ValueError): self.apply(plan)

    def test_undeclared_fields_rejected(self):
        for field in ('targetTo','healthFactor','cooldownTicks'):
            plan=copy.deepcopy(self.plan); plan['changes'][0][field]=1
            with self.subTest(field=field), self.assertRaisesRegex(ValueError,'fields'): self.apply(plan)

    def test_source_semantics_cannot_be_reinterpreted(self):
        original=c.read(self.source/c.ABILITIES)
        for field,value in [('operation','Heal'),('target','Self'),('damageType','Physical'),
                ('scalingAttributeSubject','Target'),('scalingStatusSubject','Target'),('statusScalingAttribute','MaxHealth'),
                ('scalingStatusId','other'),('statusScalingCoefficient',.3),('intervalTicks',1),('durationTicks',1)]:
            data=copy.deepcopy(original); data[0]['effects'][0][field]=value
            self.save(self.source/c.ABILITIES,data); plan=copy.deepcopy(self.plan)
            plan['sourceAbilitiesSha256']=c.sha(self.source/c.ABILITIES)
            with self.subTest(field=field), self.assertRaises(ValueError): c.validate(self.source,plan,7)

    def test_candidate_cannot_change_timing_targeting_or_status(self):
        self.apply(); original=c.read(self.target/c.ABILITIES)
        for field,value in [('scalingCoefficient',1),('statusScalingCoefficient',.2),('scalingStatusId','other'),
                ('target','Self'),('damageType','Physical'),('procCoefficient',.5),('durationTicks',1),('baseValue',10)]:
            data=copy.deepcopy(original);data[0]['effects'][0][field]=value;self.save(self.target/c.ABILITIES,data)
            with self.subTest(field=field),self.assertRaises(ValueError):self.verify()
        data=copy.deepcopy(original);data[0]['cooldownTicks']=60;self.save(self.target/c.ABILITIES,data)
        with self.assertRaisesRegex(ValueError,'Undeclared'):self.verify()

    def test_unrelated_catalog_and_settings_changes_rejected(self):
        self.apply()
        for relative in ('Data/other.json','appsettings.json','Data/world-tower/tower-floors.json'):
            path=self.target/relative;before=path.read_bytes();self.save(path,dict(changed=True))
            with self.subTest(relative=relative), self.assertRaises(ValueError):self.verify()
            path.write_bytes(before)

    def test_missing_or_extra_catalog_rejected(self):
        self.apply();self.save(self.target/'Data/extra.json',{})
        with self.assertRaisesRegex(ValueError,'added or removed'):self.verify()
        (self.target/'Data/extra.json').unlink();(self.target/'Data/other.json').unlink()
        with self.assertRaisesRegex(ValueError,'added or removed'):self.verify()

    def test_shared_guardian_and_wrong_hash_rejected(self):
        plan=copy.deepcopy(self.plan);plan['sourceAbilitiesSha256']='0'*64
        with self.assertRaisesRegex(ValueError,'source changed'):self.apply(plan)
        self.save(self.source/'Data/combat/creature-abilities.json',dict(creatures=[
            dict(monsterId='eydis',abilityIds=[self.ability['id']]),dict(monsterId='other',abilityIds=[self.ability['id']])]))
        with self.assertRaisesRegex(ValueError,'exclusive'):self.apply()


if __name__ == '__main__': unittest.main()
