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
n=a.io.ability_module().ni_restoration_acceptance_module()

class NiRestorationAcceptanceTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.source=Path(self.temp.name)/'source';self.candidate=Path(self.temp.name)/'candidate'
        api=a.io.ROOT/'LL/src/API/API.LL'
        shutil.copytree(api/'Data',self.source/'Data');shutil.copy2(api/'appsettings.json',self.source/'appsettings.json')
        shutil.copytree(self.source,self.candidate)
        self.plan={**n.VALUES,**{k:n.h.sha(self.source/v) for k,v in n.HASHES.items()}}
        self.d=dict(version=a.NI_RESTORATION_VERSION,floor=9,familySize=163,batchCount=8,samplesPerBatch=64,
            candidatePlan=self.plan,gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT,source=str(self.source),sourceManifestSha256='a'*64)

    def materialize(self):return n.materialize(self.source,self.candidate,self.plan,9)

    def test_only_guardian_offense_and_penetration_change(self):
        result=self.materialize();self.assertEqual(1,result['changes']);self.assertEqual(102,result['catalogs'])
        before=n.h.read(self.source/n.TOWER);after=n.h.read(self.candidate/n.TOWER)
        scaling=n.h.unique(after['floors'],'floorNumber',9)['guardianScaling']
        self.assertEqual(4.7036132812,scaling['offense']);self.assertEqual(40,scaling['penetration'])
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

    def test_diagnostic_candidate_cannot_enter_acceptance(self):
        for version in ('tower-ni-restoration-offense-v1','tower-ni-penetration-v1'):
            with self.assertRaises(ValueError):a.validate_candidate({**self.d,'candidatePlan':{**self.plan,'version':version}},self.source)

    def test_version_family_and_full_panel_required(self):
        a.validate_layout(self.d);self.assertEqual('ability',a.candidate_kind(self.d))
        for key,value in [('version',a.VERSION),('version',a.LIMITED_RESISTANCE_VERSION),('version',a.NI_COPY_HEALTH_VERSION),('floor',8),('familySize',147),('batchCount',4),('samplesPerBatch',16)]:
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
        self.assertEqual((76,215),(min(w for w in range(513) if a.interval(w,512,163)[0]>=.1),max(w for w in range(513) if a.interval(w,512,163)[1]<=.5)))
        with self.assertRaises(ValueError):a.validate_applied_parity([],[],'pin',{},a.NI_RESTORATION_VERSION)

    def test_dispatch_materializes_identical_declared_catalog(self):
        result=a.io.ability_module().materialize(self.source,self.candidate,self.plan,9)
        self.assertEqual(result,a.io.ability_module().verify(self.source,self.candidate,self.plan,9))

    def test_offense_grid_cannot_be_reused_as_acceptance(self):
        for factor,offense in [(1.05,4.9387939453),(1.10,5.1739746093),(.95,4.4684326171)]:
            with self.assertRaises(ValueError):n.validate_plan({**self.plan,'offenseFactor':factor,'offense':offense},9)

    def test_exact_proposal_pin_required_before_source_lookup(self):
        with patch.object(a.io,'sha',return_value='a'*64):
            with self.assertRaises(ValueError):a.validate_ni_restoration_proposal({**self.d,'proposal':'changed','proposalSha256':'a'*64},[])

    def test_eight_distinct_paths_per_phase_required(self):
        root=Path(self.temp.name)
        phases={phase:[str(root/'TestResults'/f'tower-balance-pass-ni-{phase}-{i}-study-20260929') for i in range(8)] for phase in ('screen','confirm')}
        a.validate_paths(phases,root,a.NI_RESTORATION_VERSION)
        phases['confirm'][7]=phases['screen'][7]
        with self.assertRaises(ValueError):a.validate_paths(phases,root,a.NI_RESTORATION_VERSION)

    def test_partial_batch_collection_cannot_qualify(self):
        cell=dict(id='c',composition='c',gear='x',scenario=dict(party=[dict(partySlot=1,build=dict(essenceIds=['a'],equipment=[]))]))
        with self.assertRaises(ValueError):a.assess([cell],[[dict(id='c',wins=64,samples=64)]]*7,8,64,None,a.LIMITED_EQUIPMENT)

if __name__=='__main__':unittest.main()
