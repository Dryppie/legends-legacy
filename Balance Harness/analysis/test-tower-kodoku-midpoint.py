"""Fixed offense candidate, unchanged family and diagnostic boundary safeguards."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
from types import SimpleNamespace
import unittest

HERE=Path(__file__).resolve().parent
def load(name,path):
    s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
old=load('offense_candidate_fixture',HERE/'test-tower-shared-penetration.py')
family=load('offense_family_fixture',HERE/'test-tower-eight-item-diagnostic.py')
io=old.a.io; h=io.ability_module().kodoku_midpoint_module(); b=h.base
d=io.diagnostic_module('floor8-kodoku-midpoint-diagnostic-v1')


class KodokuMidpointTests(unittest.TestCase):
    def setUp(self):
        self.f=old.SharedPenetrationTests();self.f.setUp();self.addCleanup(self.f.doCleanups)
        self.api=self.f.api;self.candidate=self.f.candidate
        self.plan={**copy.deepcopy(self.f.plan),'version':h.VERSION,'offenseFactor':.5875}

    def materialize(self):
        shutil.copytree(self.api,self.candidate)
        return io.ability_module().materialize(self.api,self.candidate,self.plan,8)

    def test_exact_point_five_eight_seven_five_candidate_only_changes_three_declared_values(self):
        self.materialize();tower=b.read(self.candidate/b.TOWER)
        self.assertEqual(5.185289917,tower['floors'][0]['guardianScaling']['offense'])
        self.assertEqual(40,tower['floors'][0]['guardianScaling']['penetration'])
        self.assertEqual(b.ATTRIBUTE,b.read(self.candidate/b.SUMMONS)[0]['attributes'][-1])
        self.assertEqual(h.VERSION,io.ability_module().verify(self.api,self.candidate,self.plan,8)['version'])

    def test_original_miasma_and_unrelated_catalog_bytes_preserved(self):
        self.materialize()
        for p in (b.ABILITIES,Path('Data/combat/creature-abilities.json'),Path('appsettings.json')):
            self.assertEqual((self.api/p).read_bytes(),(self.candidate/p).read_bytes())

    def test_old_contract_still_rejects_point_five_eight_seven_five(self):
        with self.assertRaises(ValueError):b.expected(self.api,{**self.f.plan,'offenseFactor':.5875},8)
        self.assertEqual(.525,b.VALUES['offenseFactor'])

    def test_new_contract_rejects_old_factor_and_scalar_search(self):
        for value in (.525,.575,.6,.55,.625,True,float('nan')):
            with self.assertRaises(ValueError):h.expected(self.api,{**self.plan,'offenseFactor':value},8)

    def test_cannot_compound_from_preceding_candidate(self):
        self.materialize()
        with self.assertRaises(ValueError):h.expected(self.candidate,self.plan,8)

    def test_source_hashes_cannot_change(self):
        for key in b.HASHES:
            with self.assertRaises(ValueError):h.expected(self.api,{**self.plan,key:'0'*64},8)

    def test_floor_penetration_inheritance_and_hidden_fields_frozen(self):
        for delta in ({'floor':9},{'floor':8.0},{'penetrationFactor':41},{'appendAttribute':{**b.ATTRIBUTE,'scalingCoefficient':.9}},{'extra':1}):
            with self.assertRaises(ValueError):h.expected(self.api,{**self.plan,**delta},8)

    def test_live_materialization_rejected(self):
        with self.assertRaises(ValueError):h.materialize(self.api,self.api,self.plan,8)

    def test_health_other_floor_and_summon_cap_changes_rejected(self):
        self.materialize()
        for target,mutate in [(b.TOWER,lambda x:x['floors'][0]['guardianScaling'].update(health=1)),
                              (b.TOWER,lambda x:x['floors'][1]['guardianScaling'].update(offense=1)),
                              (b.SUMMONS,lambda x:x[0].update(maxActive=6))]:
            raw=(self.candidate/target).read_bytes();value=b.read(self.candidate/target);mutate(value);(self.candidate/target).write_text(json.dumps(value))
            with self.assertRaises(ValueError):h.verify(self.api,self.candidate,self.plan,8)
            (self.candidate/target).write_bytes(raw)

    def test_extra_catalog_or_settings_change_rejected(self):
        self.materialize();(self.candidate/'Data/extra.json').write_text('{}')
        with self.assertRaises(ValueError):h.verify(self.api,self.candidate,self.plan,8)

    def proposal_fixture(self):
        f=family.EightItemTests();f.setUp();cells=f.validate();root=self.api.parent;source=root/'prepared';source.mkdir()
        def write(path,value):path.write_text(json.dumps(value),encoding='utf-8')
        shutil.copytree(self.api,source/'content');write(source/'cells.json',cells);write(source/'files.json',{})
        write(source/'request.json',dict(mode='prepare',maximumFights=0,seeds=[]));write(source/'completion.json',dict(status='Complete'))
        evidence=root/'evidence.json';write(evidence,dict(status='Verified',trialStatus='KodokuFixedScreenNotAccepted',familySize=186,
            freshStudyFights=95232,newReservations=512,finalExclusions=924780,gameContentChanged=False,
            phases=[dict(phase='screen',assessment=dict(verdict='NotAccepted',samples=512,qualifyingPartialCompositions=3),
                         limitedLeaders=[dict(id=str(i),wins=w) for i,w in enumerate((254,175,162))],ceilingFailures=[dict(id='0')])]))
        review=root/'review.json';write(review,dict(status='VerifiedSavedReportReview',evidenceSha256=b.sha(evidence),newFights=0,newSeeds=0,usedForAcceptance=False))
        p=dict(version='floor8-kodoku-midpoint-proposal-v1',status='ProposedNotPrepared',floor=8,newFights=0,newSeeds=0,nativePreparations=0,candidateMaterialized=False,
            proposedDiagnostic=copy.deepcopy(d.LIMITS),equipmentEligibility=copy.deepcopy(d.h.EQUIPMENT),familySize=186,actualCompositions=9,eligibleRecipes=128,
            source=str(source),sourceManifestSha256=b.sha(source/'files.json'),sourceCellsSha256=b.sha(source/'cells.json'),precedingEvidence=str(evidence),precedingEvidenceSha256=b.sha(evidence),
            savedReview=str(review),savedReviewSha256=b.sha(review),candidate=self.plan,initialExclusions=924780)
        path=root/'proposal.json';write(path,p);fake=SimpleNamespace(authenticate=lambda p:None,ability_module=io.ability_module)
        return p,path,source,write,fake,cells

    def test_exact_unchanged_proposal_admits_186_cells(self):
        p,path,_,_,fake,cells=self.proposal_fixture();self.assertEqual((p,cells),d.admit_proposal(fake,path))

    def test_dropped_control_rejected(self):
        p,path,source,write,fake,_=self.proposal_fixture();cells=b.read(source/'cells.json');cells.pop();write(source/'cells.json',cells)
        p['sourceCellsSha256']=b.sha(source/'cells.json');write(path,p)
        with self.assertRaises(ValueError):d.admit_proposal(fake,path)

    def test_seeded_or_already_materialized_proposal_rejected(self):
        p,path,_,write,fake,_=self.proposal_fixture()
        for delta in ({'newSeeds':1},{'candidateMaterialized':True},{'nativePreparations':1}):
            write(path,{**p,**delta})
            with self.assertRaises(ValueError):d.admit_proposal(fake,path)

    def test_proposal_cannot_weaken_equipment_or_acceptance_limits(self):
        p,path,_,write,fake,_=self.proposal_fixture()
        for delta in ({'equipmentEligibility':{**p['equipmentEligibility'],'maximumSpecializedItems':12}},{'proposedDiagnostic':{**d.LIMITS,'confirmation':True}}):
            write(path,{**p,**delta})
            with self.assertRaises(ValueError):d.admit_proposal(fake,path)

    def test_evidence_and_source_pins_required(self):
        p,path,_,write,fake,_=self.proposal_fixture()
        for key in ('precedingEvidenceSha256','sourceCellsSha256','sourceManifestSha256'):
            write(path,{**p,key:'0'*64})
            with self.assertRaises(ValueError):d.admit_proposal(fake,path)

    def test_sixteen_seeds_only_for_exact_refinement_diagnostic(self):
        d.validate_panel('screen',8,16,d.DIAGNOSTIC,186)
        for args in [('confirm',8,16,d.DIAGNOSTIC,186),('screen',7,16,d.DIAGNOSTIC,186),('screen',8,8,d.DIAGNOSTIC,186),('screen',8,16,d.base.DIAGNOSTIC,186),('screen',8,16,d.DIAGNOSTIC,185)]:
            with self.assertRaises(ValueError):d.validate_panel(*args)

    def test_old_diagnostic_does_not_accept_new_version(self):
        with self.assertRaises(ValueError):d.base.validate_panel('screen',8,8,d.DIAGNOSTIC,186)
        with self.assertRaises(ValueError):io.diagnostic_module('unknown')

    def test_distinct_provenance_required(self):
        with self.assertRaises(ValueError):d.audit_request(None,Path('out'),dict(diagnosticVersion=d.DIAGNOSTIC,inputHashes={}))
        with self.assertRaises(ValueError):d.audit_request(None,Path('out'),dict(diagnosticVersion=d.DIAGNOSTIC,inputHashes={'eight-item-diagnostic-provenance.json':'x'}))

    def test_contract_limits_and_initial_history_remain_strict(self):
        _,_,source,_,_,cells=self.proposal_fixture()
        contract=dict(version=d.DIAGNOSTIC,limits=copy.deepcopy(d.LIMITS),floor=8,familySize=186,source=str(source),cellsSha256=b.sha(source/'cells.json'),
            studies=[str(source.parent/f'study-{i}') for i in (1,2,3,4)],initialExclusions=924780,eligibleRecipes=128,actualCompositions=9)
        d.validate_contract(contract,cells)
        for delta in ({'initialExclusions':924204},{'limits':{**d.LIMITS,'retries':1}},{'limits':{**d.LIMITS,'usedForAcceptance':True}},{'studies':contract['studies'][:1]}):
            with self.assertRaises(ValueError):d.validate_contract({**contract,**delta},cells)

    def test_no_extra_batch_or_mixed_mode_before_allocation(self):
        for index,mods,seconds in [(4,[],840),(0,[True],840),(0,[],1140)]:
            with self.assertRaises(ValueError):d.admit_batch(None,Path('absent'),None,index,None,None,'screen',8,16,None,mods,seconds)



    def test_point_six_contract_rejects_refinement_factor(self):
        old_candidate=io.ability_module().kodoku_offense_module()
        with self.assertRaises(ValueError):old_candidate.expected(self.api,{**self.plan,'version':old_candidate.VERSION},8)

    def test_native_request_requires_complete_sixteen_seed_panel(self):
        from unittest.mock import patch
        _,_,source,write,_,cells=self.proposal_fixture()
        declaration=dict(studies=[str(source),*[str(source.parent/f'next-{i}') for i in range(3)]],limits=d.LIMITS,execution={},settings={},candidateContentHashes={})
        provenance=source/d.PROVENANCE
        write(provenance,dict(declaration=str(source/'unused.json'),declarationSha256='pinned',batchIndex=0))
        write(source/'result.json',dict(diagnosticOnly=True,usedForAcceptance=False))
        write(source/'scope.json',dict(execution={},settings={},contentHashes={}))
        q=dict(diagnosticVersion=d.DIAGNOSTIC,inputHashes={str(provenance):b.sha(provenance)},mode='screen',floor=8,seeds=list(range(16)),searchSeeds=[],maximumFights=2976)
        with patch.object(d,'read_contract',return_value=(declaration,cells)):
            d.audit_request(None,source,q)
            with self.assertRaises(ValueError):d.audit_request(None,source,{**q,'maximumFights':1488})
            write(source/'result.json',dict(diagnosticOnly=True,usedForAcceptance=True))
            with self.assertRaises(ValueError):d.audit_request(None,source,q)


    def test_closed_point_five_seven_five_candidate_rejects_midpoint(self):
        prior=io.ability_module().kodoku_refinement_module()
        with self.assertRaises(ValueError):prior.expected(self.api,{**self.plan,'version':prior.VERSION},8)

    def test_midpoint_has_no_aggregate_acceptance_or_application_contract(self):
        a=load('midpoint_aggregate_guard',HERE/'tower-balance-aggregate.py')
        with self.assertRaises(ValueError):a.candidate_kind(dict(candidatePlan=self.plan,version=a.KODOKU_FIXED_VERSION))
        with self.assertRaises(ValueError):a.validate_layout(dict(version=a.KODOKU_FIXED_VERSION,batchCount=32,samplesPerBatch=16,floor=8,familySize=186,candidatePlan=self.plan))

    def test_previous_diagnostic_still_rejects_third_batch_before_io(self):
        with self.assertRaises(ValueError):d.base.admit_batch(None,Path('absent'),None,2,None,None,'screen',8,8,None,[],840)

    def test_failed_screen_evidence_cannot_be_replaced_with_a_pass_or_diagnostic(self):
        p,path,_,write,fake,_=self.proposal_fixture()
        evidence=Path(p['precedingEvidence']);original=b.read(evidence)
        for status in ('CompletedDiagnosticOnly','ConfirmedKodokuFixed'):
            write(evidence,{**original,'trialStatus':status});p['precedingEvidenceSha256']=b.sha(evidence);write(path,p)
            with self.assertRaises(ValueError):d.admit_proposal(fake,path)

    def test_fourth_panel_provenance_and_fifth_panel_rejection(self):
        from unittest.mock import patch
        _,_,source,write,_,cells=self.proposal_fixture()
        contract=dict(studies=[str(source.parent/f'first-{i}') for i in range(3)]+[str(source)],limits=d.LIMITS,execution={},settings={},candidateContentHashes={})
        provenance=source/d.PROVENANCE
        write(source/'result.json',dict(diagnosticOnly=True,usedForAcceptance=False))
        write(source/'scope.json',dict(execution={},settings={},contentHashes={}))
        for index in (3,4):
            write(provenance,dict(declaration=str(source/'unused.json'),declarationSha256='pinned',batchIndex=index))
            q=dict(diagnosticVersion=d.DIAGNOSTIC,inputHashes={str(provenance):b.sha(provenance)},mode='screen',floor=8,seeds=list(range(16)),searchSeeds=[],maximumFights=2976)
            with patch.object(d,'read_contract',return_value=(contract,cells)):
                if index==3:d.audit_request(None,source,q)
                else:
                    with self.assertRaises(ValueError):d.audit_request(None,source,q)

    def test_cli_accepts_four_panel_indices_but_still_requires_bound_contract(self):
        import subprocess
        import sys
        for index in (2,3,4):
            result=subprocess.run([sys.executable,'-B',str(io.__file__),'--mode','screen','--name','midpoint-cli-boundary',
                                   '--floor','8','--artifacts',str(self.api.parent),'--diagnostic-batch',str(index)],capture_output=True,text=True)
            self.assertNotEqual(0,result.returncode)
            self.assertIn('invalid choice' if index==4 else 'Complete diagnostic binding required',result.stderr)

if __name__=='__main__':unittest.main()
