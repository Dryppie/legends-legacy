"""Reject undeclared recovery, family-mode and source changes before seed allocation."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

s=importlib.util.spec_from_file_location('recovery_test',Path(__file__).with_name('tower-recovery-pressure-refinement.py'))
r=importlib.util.module_from_spec(s);s.loader.exec_module(r);h=r.h


class RecoveryPressureTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.source=Path(self.temp.name)/'source';self.candidate=Path(self.temp.name)/'candidate'
        (self.source/'Data/combat').mkdir(parents=True);(self.source/'Data/world-tower').mkdir()
        self.save(self.source/h.TOWER,dict(floors=[dict(floorNumber=i,guardianAbilityProfileId=str(i),
            guardianScaling=dict(health=4,offense=6,penetration=1,defense=2)) for i in (7,8)]))
        self.save(self.source/'Data/combat/creature-abilities.json',dict(creatures=[dict(monsterId='7',
            abilityIds=['ability.creature.eydis.springtide',r.ENDLESS])]))
        self.abilities=[dict(id='ability.creature.eydis.springtide',description='Deal 100% Magical Damage plus {statusScaling} per stack of Abundance to all enemies.',
            cooldownTicks=120,effects=[dict(id='effect.creature.eydis.springtide.damage',operation='Damage',target='AllEnemies',
                scalingAttribute='Power',scalingCoefficient=1,scalingStatusId='status.eydis.abundance',statusScalingAttribute='Power',
                statusScalingCoefficient=.2,damageType='Magical')]),
            dict(id=r.ENDLESS,description=r.DESCRIPTION,kind='Passive',cooldownTicks=0,
                triggers=[dict(event='OnInterval',internalCooldownTicks=100,initialDelayTicks=100,effectIds=[r.ABUNDANCE,r.HEAL])],
                effects=[dict(id=r.ABUNDANCE,operation='ApplyStatus',target='Self',baseValue=1,statusId='status.eydis.abundance',procCoefficient=1),
                    dict(id=r.HEAL,operation='Heal',target='Self',scalingStatusId='status.eydis.abundance',statusScalingAttribute='MaxHealth',
                         statusScalingCoefficient=.01,critEligibility='Disallowed',procCoefficient=1)])]
        self.save(self.source/h.ABILITIES,self.abilities);self.save(self.source/'appsettings.json',dict(untouched=True))
        self.plan=dict(version=r.VERSION,floor=7,healthPressurePlan=dict(version=h.VERSION,floor=7,
            sourceAbilitiesSha256=h.sha(self.source/h.ABILITIES),sourceTowerSha256=h.sha(self.source/h.TOWER),offenseFactor=.6,penetrationFactor=50,
            change=dict(abilityId=self.abilities[0]['id'],effectId=self.abilities[0]['effects'][0]['id'],**{'from':1,'to':.2125},
                descriptionFrom=self.abilities[0]['description'],descriptionTo="Deal 21.25% of each target's Max Health as Magical Damage plus 20% Power per stack of Abundance to all enemies.")),
            recoveryChange=dict(abilityId=r.ENDLESS,effectId=r.HEAL,field='statusScalingCoefficient',fromValue=.01,toValue=.009))
        shutil.copytree(self.source,self.candidate)

    @staticmethod
    def save(path,value):path.write_text(json.dumps(value),encoding='utf-8')

    def materialize(self):return r.materialize(self.source,self.candidate,self.plan,7)

    def test_exact_compound_change_keeps_dynamic_description_and_source(self):
        pin=h.sha(self.source/h.ABILITIES);v=self.materialize()
        self.assertEqual(v['version'],r.VERSION);self.assertEqual(h.sha(self.source/h.ABILITIES),pin)
        a=h.read(self.candidate/h.ABILITIES)
        self.assertEqual(a[0]['effects'][0]['scalingAttributeSubject'],'Target')
        self.assertEqual(a[0]['effects'][0]['statusScalingCoefficient'],.2)
        self.assertEqual(a[1]['effects'][1]['statusScalingCoefficient'],.009)
        self.assertEqual(a[1]['description'],r.DESCRIPTION)
        self.assertEqual(a[1]['triggers'],self.abilities[1]['triggers'])

    def test_old_contract_still_rejects_recovery_edit(self):
        self.materialize()
        with self.assertRaises(ValueError):h.verify(self.source,self.candidate,self.plan['healthPressurePlan'],7)

    def test_rejects_other_format_floor_and_extra_fields(self):
        for key,value in [('version',h.VERSION),('floor',8),('floor',True),('extra',{})]:
            plan=copy.deepcopy(self.plan);plan[key]=value
            with self.subTest(key=key),self.assertRaises(ValueError):r.expected(self.source,plan,7)

    def test_rejects_other_midpoints_and_factors(self):
        for key,value in [('offenseFactor',.65),('penetrationFactor',40),('sourceAbilitiesSha256','bad'),('sourceTowerSha256','bad')]:
            plan=copy.deepcopy(self.plan);plan['healthPressurePlan'][key]=value
            with self.subTest(key=key),self.assertRaises(ValueError):r.expected(self.source,plan,7)
        plan=copy.deepcopy(self.plan);plan['healthPressurePlan']['change']['to']=.20
        with self.assertRaises(ValueError):r.expected(self.source,plan,7)

    def test_rejects_other_recovery_fields_or_coefficients(self):
        for key,value in [('abilityId','other'),('effectId',r.ABUNDANCE),('field','scalingCoefficient'),
                          ('fromValue',.02),('toValue',.005),('toValue',True),('toValue',float('nan')),('extra',1)]:
            plan=copy.deepcopy(self.plan);plan['recoveryChange'][key]=value
            with self.subTest(key=key,value=value),self.assertRaises(ValueError):r.expected(self.source,plan,7)

    def test_rejects_changed_source_healing_even_when_repinned(self):
        for key,value in [('target','AllAllies'),('critEligibility','Allowed'),('statusScalingAttribute','Power'),
                          ('scalingStatusId','other'),('statusScalingCoefficient',.02),('scalingCoefficient',1),('durationTicks',1)]:
            a=copy.deepcopy(self.abilities);a[1]['effects'][1][key]=value;self.save(self.source/h.ABILITIES,a)
            plan=copy.deepcopy(self.plan);plan['healthPressurePlan']['sourceAbilitiesSha256']=h.sha(self.source/h.ABILITIES)
            with self.subTest(key=key),self.assertRaises(ValueError):r.expected(self.source,plan,7)

    def test_rejects_changed_source_interval_and_order(self):
        for key,value in [('internalCooldownTicks',90),('initialDelayTicks',0),('effectIds',[r.HEAL,r.ABUNDANCE]),('event','OnCombatStart')]:
            a=copy.deepcopy(self.abilities);a[1]['triggers'][0][key]=value;self.save(self.source/h.ABILITIES,a)
            plan=copy.deepcopy(self.plan);plan['healthPressurePlan']['sourceAbilitiesSha256']=h.sha(self.source/h.ABILITIES)
            with self.subTest(key=key),self.assertRaises(ValueError):r.expected(self.source,plan,7)

    def test_rejects_changed_source_generation_bonus_or_description(self):
        for field in ('generation','bonus','description'):
            a=copy.deepcopy(self.abilities)
            if field=='generation':a[1]['effects'][0]['baseValue']=2
            elif field=='bonus':a[0]['effects'][0]['statusScalingCoefficient']=.3
            else:a[1]['description']='Heal 1%.'
            self.save(self.source/h.ABILITIES,a);plan=copy.deepcopy(self.plan)
            plan['healthPressurePlan']['sourceAbilitiesSha256']=h.sha(self.source/h.ABILITIES)
            with self.subTest(field=field),self.assertRaises(ValueError):r.expected(self.source,plan,7)

    def test_rejects_shared_healing_ability(self):
        p=h.read(self.source/'Data/combat/creature-abilities.json');p['creatures'].append(dict(monsterId='8',abilityIds=[r.ENDLESS]))
        self.save(self.source/'Data/combat/creature-abilities.json',p)
        with self.assertRaises(ValueError):r.expected(self.source,self.plan,7)

    def test_rejects_undeclared_healing_and_damage_deltas(self):
        self.materialize();original=h.read(self.candidate/h.ABILITIES)
        for ai,ei,key,value in [(1,1,'statusScalingCoefficient',.005),(1,1,'target','AllAllies'),(1,1,'critEligibility','Allowed'),
                               (1,0,'baseValue',2),(0,0,'statusScalingCoefficient',.3),(0,0,'scalingCoefficient',.20)]:
            a=copy.deepcopy(original);a[ai]['effects'][ei][key]=value;self.save(self.candidate/h.ABILITIES,a)
            with self.subTest(key=key),self.assertRaises(ValueError):r.verify(self.source,self.candidate,self.plan,7)

    def test_rejects_undeclared_timing_or_description(self):
        self.materialize();original=h.read(self.candidate/h.ABILITIES)
        for field in ('description','timing'):
            a=copy.deepcopy(original)
            if field=='timing':a[1]['triggers'][0]['internalCooldownTicks']=90
            else:a[1]['description']='Changed'
            self.save(self.candidate/h.ABILITIES,a)
            with self.subTest(field=field),self.assertRaises(ValueError):r.verify(self.source,self.candidate,self.plan,7)

    def test_rejects_other_floor_or_guardian_edits(self):
        self.materialize();original=h.read(self.candidate/h.TOWER)
        for index,key in [(0,'health'),(0,'offense'),(0,'defense'),(0,'penetration'),(1,'health')]:
            a=copy.deepcopy(original);a['floors'][index]['guardianScaling'][key]+=1;self.save(self.candidate/h.TOWER,a)
            with self.subTest(key=key,index=index),self.assertRaises(ValueError):r.verify(self.source,self.candidate,self.plan,7)

    def test_rejects_catalog_set_and_settings_changes(self):
        self.materialize();extra=self.candidate/'Data/extra.json';self.save(extra,{})
        with self.assertRaises(ValueError):r.verify(self.source,self.candidate,self.plan,7)
        extra.unlink();self.save(self.candidate/'appsettings.json',{})
        with self.assertRaises(ValueError):r.verify(self.source,self.candidate,self.plan,7)

    def test_rejects_in_place_and_modified_isolated_source(self):
        with self.assertRaises(ValueError):r.materialize(self.source,self.source,self.plan,7)
        self.save(self.candidate/h.ABILITIES,[])
        with self.assertRaises(ValueError):self.materialize()

    def test_rejects_mutually_exclusive_flags_modes_and_family_changes(self):
        args=['screen',self.source,self.plan,1,1,1,None,None,[],False,[]]
        for mode in ('screen','confirm'):r.validate_mode(mode,*args[1:])
        for index,value in [(0,'prepare'),(0,'search'),(1,None),(3,.9),(4,.6),(5,50),(6,'ability'),(7,'health'),(8,['import']),(9,True),(10,['projection'])]:
            bad=args.copy();bad[index]=value
            with self.subTest(index=index),self.assertRaises(ValueError):r.validate_mode(*bad)


    def test_versions_cannot_substitute_for_each_other(self):
        with self.assertRaises(ValueError):r.r.expected(self.source,self.plan,7)
        old=copy.deepcopy(self.plan);old['version']=r.r.VERSION
        with self.assertRaises(ValueError):r.r.expected(self.source,old,7)
        old['recoveryChange']['toValue']=.0075
        r.r.expected(self.source,old,7)
        with self.assertRaises(ValueError):r.expected(self.source,old,7)
        wrong=copy.deepcopy(self.plan);wrong['recoveryChange']['toValue']=.0075
        with self.assertRaises(ValueError):r.expected(self.source,wrong,7)

    def test_owner_dispatch_rejects_unknown_version(self):
        spec=importlib.util.spec_from_file_location('refinement_owner',Path(__file__).with_name('run-tower-balance-pass.py'))
        owner=importlib.util.module_from_spec(spec);spec.loader.exec_module(owner)
        self.assertEqual(owner.recovery_pressure_module().VERSION,r.r.VERSION)
        self.assertEqual(owner.recovery_pressure_module(r.VERSION).VERSION,r.VERSION)
        with self.assertRaises(ValueError):owner.recovery_pressure_module('tower-recovery-pressure-refinement-v2')


if __name__=='__main__':unittest.main()
