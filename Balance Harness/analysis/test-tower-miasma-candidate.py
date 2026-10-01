"""Reject every delta outside the frozen single-effect floor-eight candidate."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

HERE=Path(__file__).resolve().parent
def module(name,path):
    s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
h=module('miasma',HERE/'tower-kodoku-miasma.py');a=module('miasma_aggregate',HERE/'tower-balance-aggregate.py')


class MiasmaCandidateTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name);self.api=self.root/'api'
        self.ability=dict(id=h.ABILITY,kind='Active',name='Withering Miasma',description=h.BEFORE,cooldownTicks=150,
            tags=['Area','Debuff','HealingReduction','RegenerationReduction'],effects=[copy.deepcopy(h.HEALING),copy.deepcopy(h.RATE)])
        self.write(h.ABILITIES,[self.ability,dict(id='unrelated',unchanged=True)])
        self.write(h.TOWER,dict(floors=[dict(floorNumber=8,guardianAbilityProfileId='kodoku',guardianScaling=dict(health=9.9,offense=8.8))]))
        self.write(Path('Data/combat/creature-abilities.json'),dict(creatures=[dict(monsterId='kodoku',abilityIds=[h.ABILITY])]))
        self.write(Path('Data/combat/summons.json'),[dict(id='venomSpawn',unchanged=True)]);self.write(Path('appsettings.json'),dict(unchanged=True))
        self.plan=dict(version=h.VERSION,abilityId=h.ABILITY,removeEffect=copy.deepcopy(h.RATE),preserveEffects=[copy.deepcopy(h.HEALING)],
            descriptionFrom=h.BEFORE,descriptionTo=h.AFTER,sourceAbilitiesSha256=h.sha(self.api/h.ABILITIES),sourceTowerSha256=h.sha(self.api/h.TOWER))
        self.candidate=self.root/'candidate'

    def write(self,path,value):
        target=self.api/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

    def materialize(self):
        shutil.copytree(self.api,self.candidate);return a.io.ability_module().materialize(self.api,self.candidate,self.plan,8)

    def test_only_rate_effect_and_description_change(self):
        before=h.sha(self.api/h.ABILITIES);result=self.materialize();data=h.read(self.candidate/h.ABILITIES)
        self.assertEqual([h.HEALING],data[0]['effects']);self.assertEqual(h.AFTER,data[0]['description'])
        self.assertEqual(before,h.sha(self.api/h.ABILITIES));self.assertEqual(h.VERSION,result['version'])
        self.assertEqual(h.sha(self.api/h.TOWER),h.sha(self.candidate/h.TOWER))
        self.assertEqual(result,a.io.ability_module().verify(self.api,self.candidate,self.plan,8))

    def test_live_materialization_is_rejected(self):
        with self.assertRaisesRegex(ValueError,'isolated'):h.materialize(self.api,self.api,self.plan,8)

    def test_wrong_floor_and_version_rejected(self):
        for floor in (7,9,True):
            with self.assertRaises(ValueError):h.expected(self.api,self.plan,floor)
        p=copy.deepcopy(self.plan);p['version']='tower-ability-coefficients-v1'
        with self.assertRaises(ValueError):h.expected(self.api,p,8)

    def test_cannot_change_preserved_healing_target_strength_or_duration(self):
        for key,value in [('baseValue',-60),('durationTicks',100),('target','Self'),('operation','ModifyRegenerationRate')]:
            p=copy.deepcopy(self.plan);p['preserveEffects'][0][key]=value
            with self.subTest(key=key),self.assertRaises(ValueError):h.expected(self.api,p,8)

    def test_no_other_removal_noop_or_extra_parameter(self):
        for change in ('id','baseValue','extra','description','order'):
            p=copy.deepcopy(self.plan)
            if change=='id':p['removeEffect']['id']=h.HEALING['id']
            if change=='baseValue':p['removeEffect']['baseValue']=0
            if change=='extra':p['offenseFactor']=.5
            if change=='description':p['descriptionTo']='changed'
            if change=='order':p['preserveEffects'].append(copy.deepcopy(h.RATE))
            with self.subTest(change=change),self.assertRaises(ValueError):h.expected(self.api,p,8)

    def test_source_hashes_must_match(self):
        for key in ('sourceAbilitiesSha256','sourceTowerSha256'):
            p=copy.deepcopy(self.plan);p[key]='0'*64
            with self.assertRaisesRegex(ValueError,'source changed'):h.expected(self.api,p,8)

    def test_authored_ability_timing_and_tags_are_frozen(self):
        self.ability['cooldownTicks']=151;self.write(h.ABILITIES,[self.ability]);self.plan['sourceAbilitiesSha256']=h.sha(self.api/h.ABILITIES)
        with self.assertRaisesRegex(ValueError,'Authored Miasma'):h.expected(self.api,self.plan,8)

    def test_shared_profile_or_ability_is_rejected(self):
        self.write(Path('Data/combat/creature-abilities.json'),dict(creatures=[dict(monsterId='kodoku',abilityIds=[h.ABILITY]),dict(monsterId='other',abilityIds=[h.ABILITY])]))
        with self.assertRaisesRegex(ValueError,'exclusive'):h.expected(self.api,self.plan,8)

    def test_extra_catalog_and_settings_changes_are_rejected(self):
        self.materialize()
        for path in (Path('Data/combat/summons.json'),Path('appsettings.json')):
            original=(self.candidate/path).read_bytes();(self.candidate/path).write_text('{}')
            with self.assertRaises(ValueError):h.verify(self.api,self.candidate,self.plan,8)
            (self.candidate/path).write_bytes(original)
        (self.candidate/'Data/extra.json').write_text('{}')
        with self.assertRaisesRegex(ValueError,'Catalog set'):h.verify(self.api,self.candidate,self.plan,8)

    def test_hidden_ability_or_tower_delta_rejected(self):
        self.materialize()
        for relative in (h.ABILITIES,h.TOWER):
            original=(self.candidate/relative).read_bytes();data=h.read(self.candidate/relative)
            if relative==h.ABILITIES:data[1]['unchanged']=False
            else:data['floors'][0]['guardianScaling']['offense']=1
            (self.candidate/relative).write_text(json.dumps(data))
            with self.assertRaises(ValueError):h.verify(self.api,self.candidate,self.plan,8)
            (self.candidate/relative).write_bytes(original)

    def declaration(self):
        return dict(version=a.MIASMA_VERSION,floor=8,familySize=177,batchCount=4,samplesPerBatch=32,candidatePlan=self.plan,
            gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT)

    def test_separate_layout_and_candidate_dispatch(self):
        d=self.declaration();a.validate_layout(d);self.assertEqual('ability',a.candidate_kind(d));a.validate_candidate(d,self.api)
        for key,value in [('floor',7),('familySize',176),('samplesPerBatch',128),('batchCount',8),('version',a.VERSION),
                          ('version',a.LIMITED_ARMOR_VERSION),('version',a.RECOVERY_VERSION)]:
            with self.subTest(key=key),self.assertRaises(ValueError):a.validate_layout({**d,key:value})

    def test_equipment_contract_cannot_be_weakened_or_replaced_by_label(self):
        d=self.declaration()
        for delta in ({'equipmentEligibility':None},{'gear':'armor-and-health'},
                      {'equipmentEligibility':{**a.LIMITED_EQUIPMENT,'maximumSpecializedCharacters':3}}):
            with self.assertRaises(ValueError):a.validate_candidate({**d,**delta},self.api)

    def test_failed_screen_blocks_confirmation_before_history_allocation(self):
        with patch.object(a,'declaration',return_value=({**self.declaration(),'liveCatalogPins':{}},[],set())),\
             patch.object(a,'audit_phase',return_value={'assessment':{'verdict':'NotAccepted'}}),patch.object(a.io,'history') as history:
            with self.assertRaisesRegex(ValueError,'Passing complete screening'):a.admit_batch('d','pin','confirm',0)
            history.assert_not_called()

    def test_miasma_cannot_use_old_contract(self):
        with self.assertRaisesRegex(ValueError,'separate'):a.candidate_kind({**self.declaration(),'version':a.VERSION})

if __name__=='__main__':unittest.main()
