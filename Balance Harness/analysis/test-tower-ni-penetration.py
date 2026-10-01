import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('ni_penetration_aggregate',Path(__file__).with_name('tower-balance-aggregate.py'))
a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
n=a.ni_penetration_module()

class NiPenetrationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.source=Path(self.temp.name)/'source';self.candidate=Path(self.temp.name)/'candidate'
        api=a.io.ROOT/'LL/src/API/API.LL'
        shutil.copytree(api/'Data',self.source/'Data');shutil.copy2(api/'appsettings.json',self.source/'appsettings.json')
        shutil.copytree(self.source,self.candidate)
        self.plan={**n.VALUES,**{k:n.h.sha(self.source/v) for k,v in n.HASHES.items()}}
        self.d=dict(version=a.NI_PENETRATION_VERSION,floor=9,familySize=148,batchCount=4,samplesPerBatch=32,
            candidatePlan=self.plan,gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT,source=str(self.source),sourceManifestSha256='a'*64)

    def materialize(self):return n.materialize(self.source,self.candidate,self.plan,9)

    def test_only_guardian_offense_and_penetration_change(self):
        result=self.materialize();self.assertEqual(2,result['changes']);self.assertEqual(102,result['catalogs'])
        before=n.h.read(self.source/n.TOWER);after=n.h.read(self.candidate/n.TOWER)
        scaling=n.h.unique(after['floors'],'floorNumber',9)['guardianScaling']
        self.assertEqual(4.4684326171,scaling['offense']);self.assertEqual(40,scaling['penetration'])
        scaling['offense']=self.plan['originalOffense'];scaling['penetration']=1
        self.assertEqual(before,after)

    def test_exact_plan_types_values_and_fields(self):
        for key,value in [('floor',True),('offenseFactor',.94),('offense',4),('healthFactor',True),('penetrationFactor',39),('penetration',39),('sourceSummonsSha256','bad'),('extra',1)]:
            with self.subTest(key=key),self.assertRaises(ValueError):n.validate_plan({**self.plan,key:value},9)
        for key in self.plan:
            plan=copy.deepcopy(self.plan);del plan[key]
            with self.assertRaises(ValueError):n.validate_plan(plan,9)

    def test_source_hash_and_isolation_required(self):
        with self.assertRaises(ValueError):n.materialize(self.source,self.source,self.plan,9)
        (self.source/n.SUMMONS).write_text('[]')
        with self.assertRaises(ValueError):n.expected(self.source,self.plan,9)

    def test_unchanged_tower_fields_and_floor_are_guarded(self):
        self.materialize();p=self.candidate/n.TOWER;raw=p.read_bytes()
        for floor,key,value in [(9,'health',1),(8,'offense',.2),(9,'penetration',39)]:
            data=n.h.read(p);n.h.unique(data['floors'],'floorNumber',floor)['guardianScaling'][key]=value;p.write_text(json.dumps(data))
            with self.assertRaises(ValueError):n.verify(self.source,self.candidate,self.plan,9)
            p.write_bytes(raw)

    def test_settings_other_catalogs_and_catalog_set_guarded(self):
        self.materialize()
        for p in (self.candidate/'appsettings.json',self.candidate/n.SUMMONS,self.candidate/n.ABILITIES,self.candidate/'Data/unexpected.json'):
            raw=p.read_bytes() if p.exists() else None;p.write_text('{}')
            with self.assertRaises(ValueError):n.verify(self.source,self.candidate,self.plan,9)
            if raw is None:p.unlink()
            else:p.write_bytes(raw)

    def test_generic_owner_and_fixed_plan_materialize_identical_content(self):
        self.materialize();flat=n.owner_plan(self.d)
        self.assertEqual(dict(source=str(self.source),sourceManifestSha256='a'*64,floor=9,offenseFactor=.95,penetrationFactor=40),flat)
        a.io.verify_penetration_candidate(self.source,self.candidate,flat)
        tower=n.h.read(self.source/n.TOWER);floor=n.h.unique(tower['floors'],'floorNumber',9)
        floor['guardianScaling']=a.io.scaled_guardian(floor['guardianScaling'],1,.95,40)
        self.assertEqual(tower,n.h.read(self.candidate/n.TOWER))

    def test_version_family_and_full_panel_required(self):
        a.validate_layout(self.d);self.assertEqual('penetration',a.candidate_kind(self.d))
        for key,value in [('version',a.VERSION),('version',a.LIMITED_RESISTANCE_VERSION),('version',a.NI_COPY_HEALTH_VERSION),('floor',8),('familySize',147),('batchCount',8),('samplesPerBatch',16)]:
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
        with self.assertRaises(ValueError):a.validate_applied_parity([],[],'pin',{},a.NI_PENETRATION_VERSION)

if __name__=='__main__':unittest.main()
