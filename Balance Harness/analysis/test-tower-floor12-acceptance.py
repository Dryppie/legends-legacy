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
n=a.floor12_module()

class Floor12AcceptanceTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.source=Path(self.temp.name)/'source';self.candidate=Path(self.temp.name)/'candidate'
        api=a.io.ROOT/'LL/src/API/API.LL'
        shutil.copytree(api/'Data',self.source/'Data');shutil.copy2(api/'appsettings.json',self.source/'appsettings.json')
        # Model the frozen pre-candidate catalog explicitly, independent of later live tuning.
        tower=n.h.read(self.source/n.TOWER)
        n.h.unique(tower['floors'],'floorNumber',12)['guardianScaling'].update(health=9.414125,offense=11.9,penetration=1)
        (self.source/n.TOWER).write_text(json.dumps(tower,indent=2)+'\n',encoding='utf-8')
        shutil.copytree(self.source,self.candidate)
        self.plan={**n.VALUES,**{k:n.h.sha(self.source/v) for k,v in n.HASHES.items()}}
        self.d=dict(version=a.FLOOR12_VERSION,floor=12,familySize=268,batchCount=16,samplesPerBatch=32,
            candidatePlan=self.plan,gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT,source=str(self.source),sourceManifestSha256='a'*64)

    def materialize(self):return n.materialize(self.source,self.candidate,self.plan,12)

    def test_only_guardian_offense_and_penetration_change(self):
        result=self.materialize();self.assertEqual(2,result['changes']);self.assertEqual(102,result['catalogs'])
        before=n.h.read(self.source/n.TOWER);after=n.h.read(self.candidate/n.TOWER)
        scaling=n.h.unique(after['floors'],'floorNumber',12)['guardianScaling']
        self.assertEqual(6.99125,scaling['offense']);self.assertEqual(40,scaling['penetration'])
        scaling['offense']=self.plan['originalOffense'];scaling['penetration']=1
        self.assertEqual(before,after)

    def test_exact_plan_types_values_and_fields(self):
        for key,value in [('floor',True),('offenseFactor',.94),('offense',4),('healthFactor',True),('penetrationFactor',39),('penetration',39),('sourceSummonsSha256','bad'),('extra',1)]:
            with self.subTest(key=key),self.assertRaises(ValueError):n.validate_plan({**self.plan,key:value},12)
        for key in self.plan:
            plan=copy.deepcopy(self.plan);del plan[key]
            with self.assertRaises(ValueError):n.validate_plan(plan,10)

    def test_source_hash_and_isolation_required(self):
        with self.assertRaises(ValueError):n.materialize(self.source,self.source,self.plan,12)
        (self.source/n.SUMMONS).write_text('[]')
        with self.assertRaises(ValueError):n.expected(self.source,self.plan,12)

    def test_unchanged_tower_fields_and_floor_are_guarded(self):
        self.materialize();p=self.candidate/n.TOWER;raw=p.read_bytes()
        for floor,key,value in [(12,'health',1),(8,'offense',.2),(12,'penetration',39)]:
            data=n.h.read(p);n.h.unique(data['floors'],'floorNumber',floor)['guardianScaling'][key]=value;p.write_text(json.dumps(data))
            with self.assertRaises(ValueError):n.verify(self.source,self.candidate,self.plan,12)
            p.write_bytes(raw)

    def test_settings_other_catalogs_and_catalog_set_guarded(self):
        self.materialize()
        for p in (self.candidate/'appsettings.json',self.candidate/n.SUMMONS,self.candidate/n.ABILITIES,self.candidate/'Data/unexpected.json'):
            raw=p.read_bytes() if p.exists() else None;p.write_text('{}')
            with self.assertRaises(ValueError):n.verify(self.source,self.candidate,self.plan,12)
            if raw is None:p.unlink()
            else:p.write_bytes(raw)

    def test_diagnostic_candidate_cannot_enter_acceptance(self):
        for version in ('tower-ni-restoration-offense-v1','tower-ni-penetration-v1'):
            with self.assertRaises(ValueError):a.validate_candidate({**self.d,'candidatePlan':{**self.plan,'version':version}},self.source)

    def test_version_family_and_full_panel_required(self):
        a.validate_layout(self.d);self.assertEqual('penetration',a.candidate_kind(self.d))
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
        self.assertEqual((77,213),(min(w for w in range(513) if a.interval(w,512,268)[0]>=.1),max(w for w in range(513) if a.interval(w,512,268)[1]<=.5)))
        with self.assertRaises(ValueError):a.validate_applied_parity([],[],'pin',{},a.FLOOR12_VERSION)

    def test_dispatch_materializes_identical_declared_catalog(self):
        result=n.materialize(self.source,self.candidate,self.plan,12)
        self.assertEqual(result,n.verify(self.source,self.candidate,self.plan,12))

    def test_offense_grid_cannot_be_reused_as_acceptance(self):
        for factor,offense in [(1.05,4.9387939453),(1.10,5.1739746093),(.95,4.4684326171)]:
            with self.assertRaises(ValueError):n.validate_plan({**self.plan,'offenseFactor':factor,'offense':offense},12)

    def test_exact_proposal_pin_required_before_source_lookup(self):
        with patch.object(a.io,'sha',return_value='a'*64):
            with self.assertRaises(ValueError):n.validate_proposal(a,{**self.d,'proposal':'changed','proposalSha256':'a'*64},[])

    def test_sixteen_distinct_paths_per_phase_required(self):
        root=Path(self.temp.name)
        phases={phase:[str(root/'TestResults'/f'tower-balance-pass-ni-{phase}-{i}-study-20260929') for i in range(16)] for phase in ('screen','confirm')}
        a.validate_paths(phases,root,a.FLOOR12_VERSION)
        phases['confirm'][15]=phases['screen'][15]
        with self.assertRaises(ValueError):a.validate_paths(phases,root,a.FLOOR12_VERSION)

    def test_partial_batch_collection_cannot_qualify(self):
        cell=dict(id='c',composition='c',gear='x',scenario=dict(party=[dict(partySlot=1,build=dict(essenceIds=['a'],equipment=[]))]))
        with self.assertRaises(ValueError):a.assess([cell],[[dict(id='c',wins=32,samples=32)]]*15,16,32,None,a.LIMITED_EQUIPMENT)

    def test_penetration_owner_preserves_the_original_source_and_exact_factors(self):
        self.assertEqual(dict(source=str(self.source),sourceManifestSha256='a'*64,floor=12,offenseFactor=.5875,penetrationFactor=40),n.owner_plan(self.d))

    def test_generic_scalar_and_strict_candidate_have_identical_tower_bytes(self):
        self.materialize()
        intended=n.h.read(self.source/n.TOWER)
        guardian=n.h.unique(intended['floors'],'floorNumber',12)
        guardian['guardianScaling']=a.io.scaled_guardian(guardian['guardianScaling'],1.0,.5875,40.0)
        expected=self.source/'generic.json';a.io.write(expected,intended)
        self.assertEqual(expected.read_bytes(),(self.candidate/n.TOWER).read_bytes())

    def test_positive_frozen_proposal_and_full_family(self):
        path=a.io.ROOT/'TestResults/tower-floor12-restoration-midpoint-next-20261002/acceptance-proposal.json'
        if not path.exists():self.skipTest('Local immutable nomination archive required')
        p=a.io.read(path);cells=a.io.read(Path(p['source'])/'cells.json')
        d={**self.d,'source':p['source'],'sourceManifestSha256':p['sourceManifestSha256'],'cellsSha256':p['cellsSha256'],
           'proposal':str(path),'proposalSha256':n.PROPOSAL_PIN,'initialExclusions':929020}
        n.validate_proposal(a,d,cells)
        for change in ({'initialExclusions':929021},{'cellsSha256':'b'*64},{'gear':'baseline'}):
            with self.assertRaises(ValueError):n.validate_proposal(a,{**d,**change},cells)
        with self.assertRaises(ValueError):n.validate_proposal(a,d,cells[:-1])

    def test_byte_changed_candidate_or_diagnostic_marker_is_rejected_by_batch_guard(self):
        d={**self.d,'runtime':{},'settings':{},'candidateContentHashes':{}}
        with patch.object(a,'fixed_batch_audit',return_value={}),patch.object(a.io,'read',side_effect=[{},[],
            {'execution':{},'settings':{},'contentHashes':{}},{'mode':'screen','floor':12,'searchSeeds':[],'diagnosticVersion':'diagnostic'}]):
            with self.assertRaisesRegex(ValueError,'Diagnostic panels'):a.verify_batch(d,[],Path('study'),'screen',set())

    def frozen_family(self):
        path=a.io.ROOT/'TestResults/tower-floor12-restoration-midpoint-next-20261002/acceptance-proposal.json'
        p=a.io.read(path)
        return {**self.d,'source':p['source'],'sourceManifestSha256':p['sourceManifestSha256'],
            'cellsSha256':p['cellsSha256'],'proposal':str(path),'proposalSha256':n.PROPOSAL_PIN,
            'initialExclusions':929020}, a.io.read(Path(p['source'])/'cells.json')

    def test_complete_raw_family_order_and_fields_are_required(self):
        declaration,cells=self.frozen_family()
        changes=[]
        changed=copy.deepcopy(cells);changed.reverse();changes.append(changed)
        changed=copy.deepcopy(cells);changed[0]['scenario']['party'][0]['build']['essenceIds'].reverse();changes.append(changed)
        changed=copy.deepcopy(cells);changed[0]['scenario']['party'][0]['build']['equipment'].reverse();changes.append(changed)
        changed=copy.deepcopy(cells);changed[0]['scenario']['party'].reverse();changes.append(changed)
        changed=copy.deepcopy(cells);changed[0]['scenario']['party'][0]['build']['characterLevel']+=1;changes.append(changed)
        changed=copy.deepcopy(cells);changed[-1]=copy.deepcopy(changed[0]);changes.append(changed)
        for changed in changes:
            with self.assertRaises(ValueError):n.validate_proposal(a,declaration,changed)

    def test_exact_simultaneous_count_boundaries_apply_to_the_whole_family(self):
        _,cells=self.frozen_family()
        representatives={}
        for cell in cells:
            if a.equipment_eligible(cell,a.LIMITED_EQUIPMENT):
                representatives.setdefault(a.io.composition_key(cell),cell['id'])
        first,second=list(representatives.values())[:2]
        control=next(c['id'] for c in cells if not a.equipment_eligible(c,a.LIMITED_EQUIPMENT))
        for minimum,maximum,verdict in [(77,213,'Pass'),(76,213,'NotAccepted'),(77,214,'NotAccepted')]:
            totals={first:77,second:minimum,control:maximum}
            batches=[[dict(id=c['id'],wins=max(0,min(32,totals.get(c['id'],0)-i*32)),samples=32)
                      for c in cells] for i in range(16)]
            result=a.assess(cells,batches,16,32,None,a.LIMITED_EQUIPMENT)
            self.assertEqual(verdict,result['verdict'])
            self.assertEqual(155,result['eligibleRecipes'])

    def test_new_setting_cannot_use_another_guardian_profile(self):
        p=self.source/n.TOWER;data=n.h.read(p)
        n.h.unique(data['floors'],'floorNumber',12)['guardianAbilityProfileId']='different'
        p.write_text(json.dumps(data),encoding='utf-8')
        self.plan['sourceTowerSha256']=n.h.sha(p)
        with self.assertRaises(ValueError):n.expected(self.source,self.plan,12)

if __name__=='__main__':unittest.main()
