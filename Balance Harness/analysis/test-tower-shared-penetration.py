"""Exact catalog delta, whole-family decision and application provenance safeguards."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

HERE=Path(__file__).resolve().parent
def module(name, path):
    s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
a=module('shared_aggregate',HERE/'tower-balance-aggregate.py');h=a.io.ability_module().shared_penetration_module()
resource=module('shared_resources',HERE/'test-tower-miasma-resource.py')


class SharedPenetrationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.api=Path(self.temp.name)/'api';self.candidate=Path(self.temp.name)/'candidate'
        self.write(h.TOWER,dict(floors=[dict(floorNumber=8,guardianAbilityProfileId='kodoku',guardianScaling=dict(
            offense=8.8260253906,penetration=1,health=9.9064526367,defense=2.61,resistance=2.61,regeneration=1)),
            dict(floorNumber=9,guardianAbilityProfileId='other',guardianScaling=dict(offense=5))]))
        self.write(h.SUMMONS,[dict(id='venomSpawn',maxActive=5,durationTicks=0,canBasicAttack=True,abilityIds=[],attributes=[
            dict(attribute='MaxHealth',baseValue=0,minimumValue=1,scalingAttribute='MaxHealth',scalingCoefficient=.08),
            dict(attribute='Power',baseValue=0,minimumValue=1,scalingAttribute='Power',scalingCoefficient=.15),
            dict(attribute='AttackSpeed',baseValue=0,minimumValue=0)]),dict(id='other',untouched=True)])
        ids=['ability.creature.kodoku.'+s for s in ('insect_jar','survivors_struggle')]
        self.write(h.ABILITIES,[dict(id=k,effects=[dict(summonId='venomSpawn')]) for k in ids]+[dict(id='miasma',untouched=True)])
        self.write(Path('Data/combat/creature-abilities.json'),dict(creatures=[dict(monsterId='kodoku',abilityIds=ids)]))
        self.write(Path('appsettings.json'),dict(unchanged=True))
        self.plan={**copy.deepcopy(h.VALUES),**{k:h.sha(self.api/v) for k,v in h.HASHES.items()}}
        self.d=dict(version=a.SHARED_PENETRATION_VERSION,floor=8,familySize=177,batchCount=8,samplesPerBatch=16,
                    candidatePlan=self.plan,gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT)

    def write(self,p,value):
        target=self.api/p;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

    def materialize(self):
        shutil.copytree(self.api,self.candidate)
        return a.io.ability_module().materialize(self.api,self.candidate,self.plan,8)

    def test_exact_two_catalog_delta_and_unchanged_abilities(self):
        pins={p:h.sha(self.api/p) for p in h.HASHES.values()};self.materialize()
        self.assertEqual(h.ATTRIBUTE,h.read(self.candidate/h.SUMMONS)[0]['attributes'][-1])
        scaling=h.read(self.candidate/h.TOWER)['floors'][0]['guardianScaling']
        self.assertEqual(round(8.8260253906*.525,10),scaling['offense']);self.assertEqual(40,scaling['penetration'])
        self.assertEqual(pins[h.ABILITIES],h.sha(self.candidate/h.ABILITIES))
        self.assertEqual(pins,{p:h.sha(self.api/p) for p in pins})
        self.assertEqual(h.VERSION,a.io.ability_module().verify(self.api,self.candidate,self.plan,8)['version'])

    def test_live_materialization_rejected(self):
        with self.assertRaises(ValueError):h.materialize(self.api,self.api,self.plan,8)

    def test_no_scalar_search_or_hidden_plan_fields(self):
        for delta in ({'offenseFactor':.526},{'penetrationFactor':39},{'sourceOffense':8.8},{'summonId':'other'},
                      {'sourcePenetration':True},{'floor':8.0},{'extra':1}):
            with self.subTest(delta=delta),self.assertRaises(ValueError):h.validate_plan({**self.plan,**delta},8)

    def test_exact_inheritance_only(self):
        for key,value in [('baseValue',1),('minimumValue',1),('attribute','MagicPenetration'),('scalingCoefficient',True),('extra',0)]:
            p=copy.deepcopy(self.plan);p['appendAttribute'][key]=value
            with self.subTest(key=key),self.assertRaises(ValueError):h.validate_plan(p,8)

    def test_all_three_source_hashes_required(self):
        for key in h.HASHES:
            with self.assertRaises(ValueError):h.expected(self.api,{**self.plan,key:'0'*64},8)

    def test_summon_health_power_cap_and_duration_frozen(self):
        original=h.read(self.api/h.SUMMONS)
        for key,value in [('maxActive',6),('durationTicks',150),('abilityIds',['other'])]:
            changed=copy.deepcopy(original);changed[0][key]=value;self.write(h.SUMMONS,changed)
            self.plan['sourceSummonsSha256']=h.sha(self.api/h.SUMMONS)
            with self.assertRaises(ValueError):h.expected(self.api,self.plan,8)

    def test_shared_profile_rejected(self):
        tower=h.read(self.api/h.TOWER);tower['floors'][1]['guardianAbilityProfileId']='kodoku';self.write(h.TOWER,tower)
        self.plan['sourceTowerSha256']=h.sha(self.api/h.TOWER)
        with self.assertRaisesRegex(ValueError,'shared'):h.expected(self.api,self.plan,8)

    def test_shared_summon_or_ability_rejected(self):
        for definition in (dict(id='outsider',effects=[dict(summonId='venomSpawn')]),):
            self.write(h.ABILITIES,h.read(self.api/h.ABILITIES)+[definition]);self.plan['sourceAbilitiesSha256']=h.sha(self.api/h.ABILITIES)
            with self.assertRaisesRegex(ValueError,'exclusive'):h.expected(self.api,self.plan,8)

    def test_hidden_catalog_settings_or_ability_change_rejected(self):
        self.materialize()
        for relative in (h.ABILITIES,Path('appsettings.json'),Path('Data/combat/creature-abilities.json')):
            saved=(self.candidate/relative).read_bytes();(self.candidate/relative).write_text('{}')
            with self.assertRaises(ValueError):h.verify(self.api,self.candidate,self.plan,8)
            (self.candidate/relative).write_bytes(saved)
        (self.candidate/'Data/extra.json').write_text('{}')
        with self.assertRaises(ValueError):h.verify(self.api,self.candidate,self.plan,8)

    def test_other_floor_or_summon_change_rejected(self):
        self.materialize()
        for relative in (h.TOWER,h.SUMMONS):
            saved=(self.candidate/relative).read_bytes();data=h.read(self.candidate/relative)
            if relative==h.TOWER:data['floors'][1]['guardianScaling']['offense']=4
            else:data[1]['untouched']=False
            (self.candidate/relative).write_text(json.dumps(data))
            with self.assertRaises(ValueError):h.verify(self.api,self.candidate,self.plan,8)
            (self.candidate/relative).write_bytes(saved)

    def test_only_complete_eight_by_sixteen_panel(self):
        a.validate_layout(self.d);a.validate_candidate(self.d,self.api)
        for key,value in [('floor',7),('familySize',176),('samplesPerBatch',32),('batchCount',4),('version',a.MIASMA_RESOURCE_VERSION),('version',a.VERSION)]:
            with self.subTest(key=key),self.assertRaises(ValueError):a.validate_layout({**self.d,key:value})

    def test_equipment_contract_cannot_be_weakened(self):
        for delta in ({'gear':'full'},{'equipmentEligibility':None},{'equipmentEligibility':{**a.LIMITED_EQUIPMENT,'maximumSpecializedItems':9}}):
            with self.assertRaises(ValueError):a.validate_candidate({**self.d,**delta},self.api)

    def test_rejected_screen_cannot_allocate_confirmation(self):
        with patch.object(a,'declaration',return_value=({**self.d,'liveCatalogPins':{}},[],set())),patch.object(a,'audit_phase',return_value={'assessment':{'verdict':'NotAccepted'}}),patch.object(a.io,'history') as history:
            with self.assertRaises(ValueError):a.admit_batch('d','pin','confirm',0)
            history.assert_not_called()

    def test_application_requires_both_changed_catalog_hashes(self):
        summary=dict(matchedInputs=22656,fullReplays=1416)
        hashes={'combat/abilities.json':'a','combat/summons.json':'s','world-tower/tower-floors.json':'t'}
        done=dict(status='AppliedAndVerified',newSeeds=0,**summary,afterAbilitiesSha256='a',afterSummonsSha256='s',afterTowerSha256='t',finalExclusions=256)
        a.validate_applied_completion(done,summary,hashes,256,tower_changed=True,summons_changed=True)
        for key in ('afterTowerSha256','afterSummonsSha256'):
            with self.assertRaises(ValueError):a.validate_applied_completion({**done,key:'wrong'},summary,hashes,256,tower_changed=True,summons_changed=True)

    def test_all_eight_parity_receipts_required(self):
        batches,receipts=resource.MiasmaResourceTests().receipts()
        summary=a.validate_applied_parity(receipts,batches,'aggregate',{},a.SHARED_PENETRATION_VERSION)
        self.assertEqual(22656,summary['matchedInputs']);self.assertEqual(1416,summary['fullReplays'])
        for bad in (receipts[:-1],receipts+[receipts[0]],[*receipts[:-1],receipts[0]]):
            with self.assertRaises(ValueError):a.validate_applied_parity(bad,batches,'aggregate',{},a.SHARED_PENETRATION_VERSION)

    def test_unaccepted_summon_transition_not_whitelisted(self):
        q=a.io.qualification_module()
        for plan in ({},{'acceptedAggregate':{'version':'applied-tower-miasma-resource-aggregate-v1'}}):
            with self.assertRaises(ValueError):q.validate_summons_transition(plan,self.api,self.api,self.api)

    def test_confirmed_transition_still_checks_exclusive_exact_delta(self):
        self.materialize();q=a.io.qualification_module()
        root=Path(self.temp.name);(root/'historical').mkdir();shutil.copytree(self.api,root/'historical/content')
        (root/'confirmed').mkdir();shutil.copytree(self.candidate,root/'confirmed/content')
        d=root/'declaration.json';d.write_text(json.dumps(dict(source=str(root/'historical'),candidatePlan=self.plan)))
        plan={'acceptedAggregate':dict(version='applied-tower-shared-penetration-aggregate-v1',declaration=str(d))}
        q.validate_summons_transition(plan,root/'historical',root/'confirmed',self.candidate)
        data=h.read(self.candidate/h.SUMMONS);data[0]['attributes'][-1]['scalingCoefficient']=2;(self.candidate/h.SUMMONS).write_text(json.dumps(data))
        with self.assertRaises(ValueError):q.validate_summons_transition(plan,root/'historical',root/'confirmed',self.candidate)

if __name__=='__main__':unittest.main()
