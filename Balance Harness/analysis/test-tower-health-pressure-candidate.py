"""Fail-closed coverage of joint target-health candidate boundaries."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('health_pressure', Path(__file__).with_name('tower-health-pressure-candidate.py'))
h = importlib.util.module_from_spec(spec); spec.loader.exec_module(h)


class HealthPressureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name)/'source'; self.candidate = Path(self.temp.name)/'candidate'
        (self.source/'Data/combat').mkdir(parents=True); (self.source/'Data/world-tower').mkdir()
        self.save(self.source/h.TOWER, dict(floors=[dict(floorNumber=i, guardianAbilityProfileId=str(i),
            guardianScaling=dict(health=2, offense=5, penetration=1, defense=2.33)) for i in (4,5)]))
        self.save(self.source/'Data/combat/creature-abilities.json', dict(creatures=[dict(monsterId='4', abilityIds=['hall'])]))
        self.save(self.source/h.ABILITIES, [dict(id='hall', description='Old', cooldownTicks=160, effects=[dict(
            id='magic', operation='Damage', target='AllEnemies', scalingAttribute='Power', scalingCoefficient=.5,
            scalingStatusId='charge', statusScalingCoefficient=.005, damageType='Magical'),
            dict(id='consume', operation='RemoveStatus', statusId='charge', target='Self')])])
        self.save(self.source/'appsettings.json', dict(untouched=True))
        self.plan = dict(version=h.VERSION, floor=4, sourceTowerSha256=h.sha(self.source/h.TOWER),
            sourceAbilitiesSha256=h.sha(self.source/h.ABILITIES), offenseFactor=.68, penetrationFactor=100,
            change=dict(abilityId='hall', effectId='magic', **{'from':.5,'to':.25}, descriptionFrom='Old', descriptionTo='New'))
        shutil.copytree(self.source, self.candidate)

    def save(self, path, data):
        path.write_text(json.dumps(data), encoding='utf-8')

    def materialize(self):
        return h.materialize(self.source, self.candidate, self.plan, 4)

    def test_declared_candidate_preserves_source_and_status_bonus(self):
        before = h.sha(self.source/h.ABILITIES); self.materialize()
        effect = h.read(self.candidate/h.ABILITIES)[0]['effects'][0]
        self.assertEqual(effect['scalingAttributeSubject'], 'Target')
        self.assertEqual(effect['scalingAttribute'], 'MaxHealth')
        self.assertEqual(effect['statusScalingCoefficient'], .005)
        self.assertNotIn('scalingStatusSubject', effect)
        self.assertEqual(h.sha(self.source/h.ABILITIES), before)

    def test_rejects_wrong_source_hash_floor_or_fields(self):
        for key,value in [('sourceTowerSha256','bad'),('sourceAbilitiesSha256','bad'),('floor',5),('floor',True),('version','bad'),('extra',1)]:
            plan=copy.deepcopy(self.plan);plan[key]=value
            with self.subTest(key=key), self.assertRaises(ValueError):h.expected(self.source,plan,4)

    def test_rejects_nonfinite_or_unbounded_scaling(self):
        for key,values in [('offenseFactor',[True,0,.24,1,2,float('nan'),float('inf')]),
                           ('penetrationFactor',[True,0,1,129,float('nan'),float('inf')])]:
            for value in values:
                plan=copy.deepcopy(self.plan);plan[key]=value
                with self.subTest(key=key,value=value), self.assertRaises(ValueError):h.expected(self.source,plan,4)

    def test_rejects_invalid_fraction_original_or_description(self):
        for key,value in [('to',True),('to',0),('to',.51),('to',float('nan')),('from',.4),('descriptionFrom','bad'),('descriptionTo',''),('descriptionTo','Old'),('statusScalingTo',.003)]:
            plan=copy.deepcopy(self.plan);plan['change'][key]=value
            with self.subTest(key=key,value=value), self.assertRaises(ValueError):h.expected(self.source,plan,4)

    def test_rejects_other_source_mechanics(self):
        original=h.read(self.source/h.ABILITIES)
        for key,value in [('operation','Heal'),('scalingAttribute','MaxHealth'),('scalingAttributeSubject','Target'),
            ('target','CurrentTarget'),('damageType','Physical'),('intervalTicks',1),('durationTicks',1),
            ('scalingStatusId',''),('statusScalingCoefficient',0),('statusScalingAttribute','MaxHealth'),('scalingStatusSubject','Target')]:
            data=copy.deepcopy(original);data[0]['effects'][0][key]=value;self.save(self.source/h.ABILITIES,data)
            plan=copy.deepcopy(self.plan);plan['sourceAbilitiesSha256']=h.sha(self.source/h.ABILITIES)
            with self.subTest(key=key), self.assertRaises(ValueError):h.expected(self.source,plan,4)

    def test_rejects_shared_profile_and_shared_ability(self):
        tower=h.read(self.source/h.TOWER);tower['floors'][1]['guardianAbilityProfileId']='4';self.save(self.source/h.TOWER,tower)
        self.plan['sourceTowerSha256']=h.sha(self.source/h.TOWER)
        with self.assertRaises(ValueError):h.expected(self.source,self.plan,4)
        tower['floors'][1]['guardianAbilityProfileId']='5';self.save(self.source/h.TOWER,tower);self.plan['sourceTowerSha256']=h.sha(self.source/h.TOWER)
        self.save(self.source/'Data/combat/creature-abilities.json',dict(creatures=[dict(monsterId=str(i),abilityIds=['hall']) for i in (4,5)]))
        with self.assertRaises(ValueError):h.expected(self.source,self.plan,4)

    def test_rejects_undeclared_damage_status_target_timing_and_consumption_changes(self):
        self.materialize();original=h.read(self.candidate/h.ABILITIES)
        for index,key,value in [(0,'scalingCoefficient',.26),(0,'scalingAttributeSubject','Source'),(0,'statusScalingCoefficient',.01),
            (0,'scalingStatusSubject','Target'),(0,'target','CurrentTarget'),(0,'durationTicks',1),(1,'statusId','other')]:
            data=copy.deepcopy(original);data[0]['effects'][index][key]=value;self.save(self.candidate/h.ABILITIES,data)
            with self.subTest(key=key), self.assertRaises(ValueError):h.verify(self.source,self.candidate,self.plan,4)

    def test_rejects_unrelated_floor_or_guardian_delta(self):
        self.materialize();original=h.read(self.candidate/h.TOWER)
        for index,key in [(0,'health'),(0,'defense'),(0,'offense'),(0,'penetration'),(1,'offense')]:
            data=copy.deepcopy(original);data['floors'][index]['guardianScaling'][key]+=1;self.save(self.candidate/h.TOWER,data)
            with self.subTest(index=index,key=key), self.assertRaises(ValueError):h.verify(self.source,self.candidate,self.plan,4)

    def test_rejects_changed_catalog_set_and_settings(self):
        self.materialize();extra=self.candidate/'Data/extra.json';self.save(extra,{})
        with self.assertRaises(ValueError):h.verify(self.source,self.candidate,self.plan,4)
        extra.unlink();self.save(self.candidate/'appsettings.json',{})
        with self.assertRaises(ValueError):h.verify(self.source,self.candidate,self.plan,4)

    def test_rejects_in_place_materialization(self):
        with self.assertRaises(ValueError):h.materialize(self.source,self.source,self.plan,4)

    def test_rejects_mixed_modes_or_changes(self):
        args=['screen',self.source,self.plan,1,1,1,None,[],False,[]]
        for mode in ('screen','confirm'):h.validate_mode(mode,*args[1:])
        for index,value in [(0,'search'),(0,'prepare'),(1,None),(3,.9),(4,.68),(5,100),(6,'ability'),(7,['import']),(8,True),(9,['projection'])]:
            bad=args.copy();bad[index]=value
            with self.subTest(index=index), self.assertRaises(ValueError):h.validate_mode(*bad)


if __name__ == '__main__':unittest.main()
