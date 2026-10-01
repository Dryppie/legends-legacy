import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('ni_aggregate',Path(__file__).with_name('tower-balance-aggregate.py'))
a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
m=a.io.ability_module();n=m.ni_copy_health_module()

class NiCopyHealthTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.source=Path(self.temp.name)/'source';self.candidate=Path(self.temp.name)/'candidate'
        api=a.io.ROOT/'LL/src/API/API.LL'
        shutil.copytree(api/'Data',self.source/'Data');shutil.copy2(api/'appsettings.json',self.source/'appsettings.json')
        shutil.copytree(self.source,self.candidate)
        self.plan={**n.VALUES,**{k:n.h.sha(self.source/v) for k,v in n.HASHES.items()}}
        self.d=dict(version=a.NI_COPY_HEALTH_VERSION,floor=9,familySize=148,batchCount=4,samplesPerBatch=32,
            candidatePlan=self.plan,gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT)

    def materialize(self):return m.materialize(self.source,self.candidate,self.plan,9)

    def test_only_two_catalog_values_change(self):
        result=self.materialize();self.assertEqual(2,result['changes']);self.assertEqual(102,result['catalogs'])
        self.assertEqual(n.h.sha(self.source/n.TOWER),n.h.sha(self.candidate/n.TOWER))
        for relative,identity in [(n.SUMMONS,'niCopy'),(n.ABILITIES,self.plan['abilityId'])]:
            original=n.h.read(self.source/relative);actual=n.h.read(self.candidate/relative)
            obj=n.h.unique(actual,'id',identity)
            if relative==n.SUMMONS:obj['attributes'][0]['scalingCoefficient']=.1
            else:obj['description']=self.plan['originalDescription']
            self.assertEqual(original,actual)

    def test_exact_plan_types_values_and_fields(self):
        for key,value in [('floor',True),('scalingCoefficient',.04),('scalingCoefficient',False),('attribute','Armor'),('description','other'),('sourceSummonsSha256','bad'),('extra',1)]:
            with self.subTest(key=key),self.assertRaises(ValueError):n.validate_plan({**self.plan,key:value},9)
        for key in self.plan:
            plan=copy.deepcopy(self.plan);del plan[key]
            with self.assertRaises(ValueError):n.validate_plan(plan,9)

    def test_source_hash_and_isolation_required(self):
        with self.assertRaises(ValueError):m.materialize(self.source,self.source,self.plan,9)
        (self.source/n.SUMMONS).write_text('[]')
        with self.assertRaises(ValueError):m.validate(self.source,self.plan,9)

    def test_unchanged_fields_in_changed_catalogs_are_guarded(self):
        self.materialize()
        for rel in (n.SUMMONS,n.ABILITIES):
            p=self.candidate/rel;raw=p.read_bytes();data=n.h.read(p);data[0]['name']='other';p.write_text(json.dumps(data),encoding='utf-8')
            with self.assertRaises(ValueError):m.verify(self.source,self.candidate,self.plan,9)
            p.write_bytes(raw)

    def test_settings_other_catalogs_and_catalog_set_guarded(self):
        self.materialize()
        for p in (self.candidate/'appsettings.json',self.candidate/n.TOWER,self.candidate/'Data/unexpected.json'):
            raw=p.read_bytes() if p.exists() else None;p.write_text('{}')
            with self.assertRaises(ValueError):m.verify(self.source,self.candidate,self.plan,9)
            if raw is None:p.unlink()
            else:p.write_bytes(raw)

    def test_unique_floor_and_exclusive_profile_required(self):
        p=self.source/'Data/combat/creature-abilities.json';data=n.h.read(p)
        data['creatures'].append(dict(monsterId='unrelated',abilityIds=[self.plan['abilityId']]))
        p.write_text(json.dumps(data),encoding='utf-8')
        with self.assertRaises(ValueError):m.validate(self.source,self.plan,9)

    def test_version_family_and_full_panel_required(self):
        a.validate_layout(self.d);self.assertEqual('ability',a.candidate_kind(self.d))
        for key,value in [('version',a.VERSION),('version',a.LIMITED_RESISTANCE_VERSION),('floor',8),('familySize',147),('batchCount',8),('samplesPerBatch',16)]:
            with self.subTest(key=key),self.assertRaises(ValueError):a.validate_layout({**self.d,key:value})
        with self.assertRaises(ValueError):a.candidate_kind({**self.d,'version':a.VERSION})

    def test_equipment_contract_cannot_be_weakened(self):
        a.validate_candidate(self.d,self.source)
        for change in ({'gear':'full'},{'equipmentEligibility':None},{'equipmentEligibility':{**a.LIMITED_EQUIPMENT,'maximumSpecializedItems':9}}):
            with self.assertRaises(ValueError):a.validate_candidate({**self.d,**change},self.source)

    def test_failed_screen_cannot_allocate_confirmation(self):
        with patch.object(a,'declaration',return_value=({**self.d,'liveCatalogPins':{}},[],set())),patch.object(a,'audit_phase',return_value={'assessment':{'verdict':'NotAccepted'}}),patch.object(a.io,'history') as history:
            with self.assertRaises(ValueError):a.admit_batch('d','pin','confirm',0)
            history.assert_not_called()

    def test_thresholds_and_full_application_panel(self):
        self.assertEqual((25,43),(min(w for w in range(129) if a.interval(w,128,148)[0]>=.1),max(w for w in range(129) if a.interval(w,128,148)[1]<=.5)))
        with self.assertRaises(ValueError):a.validate_applied_parity([],[],'pin',{},a.NI_COPY_HEALTH_VERSION)

if __name__=='__main__':unittest.main()
