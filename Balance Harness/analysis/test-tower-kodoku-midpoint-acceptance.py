"""Fixed 186-recipe acceptance, complete confirmation and immutable audit guards."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import unittest
from unittest.mock import patch

HERE=Path(__file__).resolve().parent
def load(name,path):
    s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
old=load('fixed_candidate_fixture',HERE/'test-tower-shared-penetration.py')
family=load('fixed_family_fixture',HERE/'test-tower-eight-item-diagnostic.py')
a=old.a;io=a.io


class KodokuMidpointAcceptanceTests(unittest.TestCase):
    def setUp(self):
        self.f=old.SharedPenetrationTests();self.f.setUp();self.addCleanup(self.f.doCleanups)
        self.api=self.f.api;self.root=self.api.parent;self.plan={**self.f.plan,'version':'tower-kodoku-eight-item-pressure-midpoint-v1','offenseFactor':.5875}
        self.d={**self.f.d,'version':a.KODOKU_MIDPOINT_VERSION,'familySize':186,'batchCount':32,'candidatePlan':self.plan}
        f=family.EightItemTests();f.setUp();self.cells=f.validate()
        a._fixed_audits.clear();self.addCleanup(a._fixed_audits.clear)

    def test_exact_fixed_layout_and_candidate(self):
        a.validate_layout(self.d);a.validate_candidate(self.d,self.api)
        self.assertEqual('ability',a.candidate_kind(self.d));self.assertEqual(32,a.batch_count(self.d['version']))

    def test_legacy_versions_cannot_admit_fixed_candidate(self):
        for version in (a.VERSION,a.SHARED_PENETRATION_VERSION,a.MIASMA_RESOURCE_VERSION):
            with self.assertRaises(ValueError):a.validate_layout({**self.d,'version':version})
            with self.assertRaises(ValueError):a.candidate_kind({**self.d,'version':version})

    def test_no_partial_alternate_or_extended_layout(self):
        for k,v in [('floor',7),('familySize',185),('samplesPerBatch',8),('samplesPerBatch',32),('batchCount',8),('batchCount',31),('batchCount',33)]:
            with self.assertRaises(ValueError):a.validate_layout({**self.d,k:v})

    def test_other_coefficients_rejected(self):
        for value in (.525,.575,.6,.58,True,float('nan')):
            with self.assertRaises(ValueError):a.validate_layout({**self.d,'candidatePlan':{**self.plan,'offenseFactor':value}})

    def test_equipment_contract_stays_strict(self):
        for delta in ({'gear':'restorer-specialization'},{'equipmentEligibility':None},{'equipmentEligibility':{**a.LIMITED_EQUIPMENT,'maximumSpecializedItems':9}}):
            with self.assertRaises(ValueError):a.validate_candidate({**self.d,**delta},self.api)

    def paths(self):
        return {phase:[str(io.ROOT/'TestResults'/f'tower-balance-pass-fixed-{phase}-{i}-study-20260929') for i in range(32)] for phase in ('screen','confirm')}

    def test_exact_64_unique_paths(self):a.validate_paths(self.paths(),io.ROOT,a.KODOKU_MIDPOINT_VERSION)

    def test_missing_or_reused_phase_paths_rejected(self):
        paths=self.paths();paths['confirm'][0]=paths['screen'][0]
        with self.assertRaises(ValueError):a.validate_paths(paths,io.ROOT,a.KODOKU_MIDPOINT_VERSION)
        paths=self.paths();paths['screen'].pop()
        with self.assertRaises(ValueError):a.validate_paths(paths,io.ROOT,a.KODOKU_MIDPOINT_VERSION)

    def test_512_seed_acceptance_boundaries(self):
        self.assertLess(a.interval(75,512,186)[0],.1);self.assertGreaterEqual(a.interval(76,512,186)[0],.1)
        self.assertLessEqual(a.interval(214,512,186)[1],.5);self.assertGreater(a.interval(215,512,186)[1],.5)

    def panels(self,wins=None):
        wins=wins or {self.cells[-3]['id']:76,self.cells[-2]['id']:76}
        return [[dict(id=c['id'],wins=wins.get(c['id'],0)//32+(i<wins.get(c['id'],0)%32),samples=16) for c in self.cells] for i in range(32)]

    def assess(self,panels):return a.assess(self.cells,panels,32,16,None,a.LIMITED_EQUIPMENT)

    def test_two_actual_eligible_compositions_pass(self):
        r=self.assess(self.panels());self.assertEqual('Pass',r['verdict']);self.assertEqual(2,r['qualifyingPartialCompositions']);self.assertEqual(128,r['eligibleRecipes'])

    def test_one_composition_in_multiple_gear_profiles_is_not_two(self):
        cell=self.cells[-3];same=[c for c in self.cells if io.composition_key(c)==io.composition_key(cell) and a.equipment_eligible(c,a.LIMITED_EQUIPMENT)]
        r=self.assess(self.panels({c['id']:76 for c in same}));self.assertEqual('NotAccepted',r['verdict']);self.assertEqual(1,r['qualifyingPartialCompositions'])

    def test_ineligible_gear_cannot_supply_minimum(self):
        r=self.assess(self.panels({c['id']:76 for c in self.cells if not a.equipment_eligible(c,a.LIMITED_EQUIPMENT)}))
        self.assertEqual(0,r['qualifyingPartialCompositions']);self.assertEqual('NotAccepted',r['verdict'])

    def test_ineligible_outlier_still_fails_ceiling(self):
        outlier=next(c for c in self.cells if not a.equipment_eligible(c,a.LIMITED_EQUIPMENT))
        r=self.assess(self.panels({self.cells[-3]['id']:76,self.cells[-2]['id']:76,outlier['id']:215}))
        self.assertEqual(2,r['qualifyingPartialCompositions']);self.assertEqual('NotAccepted',r['verdict'])

    def test_second_recipe_below_minimum_fails(self):
        self.assertEqual('NotAccepted',self.assess(self.panels({self.cells[-3]['id']:76,self.cells[-2]['id']:75}))['verdict'])

    def test_incomplete_phase_or_reordered_recipe_rejected(self):
        with self.assertRaises(ValueError):self.assess(self.panels()[:-1])
        panels=self.panels();panels[-1][0],panels[-1][1]=panels[-1][1],panels[-1][0]
        with self.assertRaises(ValueError):self.assess(panels)

    def test_failed_screen_never_allocates_confirmation(self):
        with patch.object(a,'declaration',return_value=({**self.d,'liveCatalogPins':{}},[],set())),patch.object(a,'audit_phase',return_value={'assessment':{'verdict':'NotAccepted'}}),patch.object(io,'history') as history:
            with self.assertRaises(ValueError):a.admit_batch('d','pin','confirm',0)
            history.assert_not_called()

    def proposal(self):
        write=lambda path,data:Path(path).write_text(json.dumps(data),encoding='utf-8')
        evidence=self.root/'evidence.json';write(evidence,dict(status='Verified',diagnosticOnly=True,usedForAcceptance=False,comparisons=[dict(eightItems=dict(wins=w)) for w in (22,11,12)]))
        phase=dict(batchCount=32,seedsPerBatch=16,samplesPerRecipe=512,fights=95232)
        p=dict(version='floor8-kodoku-midpoint-acceptance-proposal-v1',proposedAggregateVersion=a.KODOKU_MIDPOINT_VERSION,
            localApplication=dict(onlyAfterCompleteIndependentConfirmation=True,matchedInputs=95232,fullReplays=5952,newSeeds=0,requireEveryConfirmationBatch=True,requireBoundApplicationPlan=True,requireFreshBackendRegression=True,externalDeployment=False),
            status='ProposedNotDeclared',newFights=0,newSeeds=0,diagnosticOutcomesUsedForAcceptance=False,
            screening=phase,confirmation={**phase,'onlyAfterCompleteScreenPass':True,'independentSeeds':True},
            acceptance=dict(method='approximate Bonferroni-Wilson',alpha=.05,familySize=186,minimumDistinctEligibleCompositions=2,minimumLowerBound=.1,maximumUpperBound=.5,minimumWinsPerQualifyingRecipe=76,maximumWinsEveryRecipe=214),
            limits=dict(maximumFreshFights=190464,maximumNewSeeds=1024,maximumPhases=2,maximumBatchesPerPhase=32,maximumApplicationReplays=5952,nativeSecondsLimit=840,ownerSecondsLimit=900,byteLimit=2*1024**3,retries=0,extensions=0,additionalCandidates=0,interimSelection=False),
            candidatePlan=self.plan,candidateContentHashes={},source=str(self.api),sourceManifestSha256='manifest',sourceCellsSha256='cells',initialExclusions=924844,
            equipmentEligibility=a.LIMITED_EQUIPMENT,familySize=186,actualCompositions=9,eligibleRecipes=128,precedingEvidence=str(evidence),precedingEvidenceSha256=io.sha(evidence))
        path=self.root/'proposal.json';write(path,p)
        d={**self.d,'proposal':str(path),'proposalSha256':io.sha(path),'candidateContentHashes':{},'source':str(self.api),'sourceManifestSha256':'manifest','cellsSha256':'cells','initialExclusions':924844}
        return p,path,d,write

    def test_exact_frozen_proposal(self):
        _,_,d,_=self.proposal();a.validate_fixed_proposal(d,self.cells)

    def test_proposal_cannot_change_precision_or_stopping_rules(self):
        p,path,d,write=self.proposal()
        for delta in ({'screening':{**p['screening'],'samplesPerRecipe':256}},{'limits':{**p['limits'],'extensions':1}},{'acceptance':{**p['acceptance'],'maximumUpperBound':.6}}):
            write(path,{**p,**delta})
            with self.assertRaises(ValueError):a.validate_fixed_proposal({**d,'proposalSha256':io.sha(path)},self.cells)

    def test_proposal_requires_exact_family_source_and_candidate(self):
        _,_,d,_=self.proposal()
        for delta in ({'cellsSha256':'other'},{'candidatePlan':{**self.plan,'offenseFactor':.6}},{'initialExclusions':924236}):
            with self.assertRaises(ValueError):a.validate_fixed_proposal({**d,**delta},self.cells)
        with self.assertRaises(ValueError):a.validate_fixed_proposal(d,self.cells[:-1])

    def test_diagnostic_evidence_is_pinned_and_never_pooled(self):
        p,path,d,write=self.proposal()
        for delta in ({'precedingEvidenceSha256':'bad'},{'diagnosticOutcomesUsedForAcceptance':True},{'newSeeds':1}):
            write(path,{**p,**delta})
            with self.assertRaises(ValueError):a.validate_fixed_proposal({**d,'proposalSha256':io.sha(path)},self.cells)

    def test_diagnostic_panel_cannot_become_acceptance(self):
        values={'independent-audit.json':{},'cells.json':self.cells,'scope.json':dict(execution={},settings={},contentHashes={}),
                'request.json':dict(mode='screen',floor=8,searchSeeds=[],diagnosticVersion='floor8-kodoku-offense-refinement-diagnostic-v1')}
        with patch.object(a,'fixed_batch_audit',return_value={}),patch.object(io,'read',side_effect=lambda p:values[Path(p).name]):
            with self.assertRaisesRegex(ValueError,'Diagnostic panels'):a.verify_batch({**self.d,'runtime':{},'settings':{},'candidateContentHashes':{}},self.cells,self.root/'s','screen',set())

    def test_cached_raw_recount_reauthenticates_and_returns_copy(self):
        with patch.object(io,'authenticate') as auth,patch.object(io,'sha',return_value='manifest'),patch.object(io,'audit',return_value={'wins':2}) as audit:
            first=a.fixed_batch_audit(self.root);first['wins']=999
            self.assertEqual({'wins':2},a.fixed_batch_audit(self.root));self.assertEqual(2,auth.call_count);self.assertEqual(1,audit.call_count)

    def test_cached_recount_still_checks_changed_external_inputs(self):
        values={'independent-audit.json':{},'cells.json':self.cells,'scope.json':dict(execution={},settings={},contentHashes={}),
                'request.json':dict(mode='screen',floor=8,searchSeeds=[],inputHashes={'ability-candidate-provenance.json':'p','external':'expected'}),
                'result.json':dict(diagnosticOnly=False),'ability-candidate-provenance.json':dict(plan=self.plan)}
        with patch.object(a,'fixed_batch_audit',return_value={}),patch.object(io,'read',side_effect=lambda p:values[Path(p).name]),patch.object(io,'sha',side_effect=lambda p:'p' if Path(p).name=='ability-candidate-provenance.json' else 'changed'):
            with self.assertRaisesRegex(ValueError,'Changed native input'):a.verify_batch({**self.d,'runtime':{},'settings':{},'candidateContentHashes':{}},self.cells,self.root/'s','screen',set())

    def test_changed_manifest_requires_new_recount(self):
        with patch.object(io,'authenticate'),patch.object(io,'sha',side_effect=['first','second']),patch.object(io,'audit',return_value={}) as audit:
            a.fixed_batch_audit(self.root);a.fixed_batch_audit(self.root);self.assertEqual(2,audit.call_count)

    def test_tampered_member_never_uses_cached_recount(self):
        with patch.object(io,'authenticate',side_effect=[None,ValueError('changed member')]),patch.object(io,'sha',return_value='same'),patch.object(io,'audit',return_value={}) as audit:
            a.fixed_batch_audit(self.root)
            with self.assertRaises(ValueError):a.fixed_batch_audit(self.root)
            self.assertEqual(1,audit.call_count)

    def test_all_32_application_receipts_required(self):
        batches=[];receipts=[]
        for i in range(32):
            b=dict(source=str(i),manifestSha256='m',audit='a',auditSha256='a-pin',fights=2976,seeds=list(range(i*16,(i+1)*16)));batches.append(b)
            receipts.append(dict(request=dict(source=str(i),manifestPin='m',audit='a',auditPin='a-pin',aggregatePin='aggregate'),
                result=dict(status='AggregateInputsAndReplaysVerified',manifestPin='m',matchedInputs=2976,fullReplays=186,newSeeds=0,execution={}),
                completion=dict(status='Verified',aggregateSha256='aggregate',resultSha256='result',matchedInputs=2976,fullReplays=186,newSeeds=0,floorFileSha256='floor'),
                process=dict(exitCode=0,timedOut=False,activeProcesses=0),resultSha256='result'))
        r=a.validate_applied_parity(receipts,batches,'aggregate',{},a.KODOKU_MIDPOINT_VERSION)
        self.assertEqual((95232,5952),(r['matchedInputs'],r['fullReplays']))
        for bad in (receipts[:-1],[*receipts[:-1],receipts[0]]):
            with self.assertRaises(ValueError):a.validate_applied_parity(bad,batches,'aggregate',{},a.KODOKU_MIDPOINT_VERSION)

    def test_fixed_accepted_summon_transition_checks_exact_delta(self):
        shutil.copytree(self.api,self.root/'original/content');shutil.copytree(self.api,self.root/'confirmed/content')
        candidate=self.root/'confirmed/content';io.ability_module().materialize(self.api,candidate,self.plan,8)
        declaration=self.root/'d.json';declaration.write_text(json.dumps(dict(source=str(self.root/'original'),candidatePlan=self.plan)))
        plan=dict(acceptedAggregate=dict(version='applied-tower-kodoku-midpoint-aggregate-v1',declaration=str(declaration)))
        io.qualification_module().validate_summons_transition(plan,self.root/'original',self.root/'confirmed',candidate)
        summons=candidate/'Data/combat/summons.json';data=json.loads(summons.read_text());data[0]['attributes'][-1]['scalingCoefficient']=2;summons.write_text(json.dumps(data))
        with self.assertRaises(ValueError):io.qualification_module().validate_summons_transition(plan,self.root/'original',self.root/'confirmed',candidate)


    def test_closed_fixed_contract_rejects_midpoint_and_reverse(self):
        for d in ({**self.d,'version':a.KODOKU_FIXED_VERSION},
                  {**self.d,'candidatePlan':{**self.plan,'version':'tower-kodoku-eight-item-pressure-refinement-v1','offenseFactor':.575}}):
            with self.assertRaises(ValueError):a.validate_layout(d)
            with self.assertRaises(ValueError):a.candidate_kind(d)

    def test_midpoint_proposal_cannot_reuse_old_contract(self):
        p,path,d,write=self.proposal()
        for delta in ({'version':'floor8-kodoku-fixed-acceptance-proposal-v1'}, {'proposedAggregateVersion':a.KODOKU_FIXED_VERSION}):
            write(path,{**p,**delta})
            with self.assertRaises(ValueError):a.validate_fixed_proposal({**d,'proposalSha256':io.sha(path)},self.cells)

    def test_local_application_cannot_skip_confirmation_parity_or_regression(self):
        p,path,d,write=self.proposal()
        for key,value in p['localApplication'].items():
            changed=not value if type(value) is bool else value+1
            write(path,{**p,'localApplication':{**p['localApplication'],key:changed}})
            with self.assertRaises(ValueError):a.validate_fixed_proposal({**d,'proposalSha256':io.sha(path)},self.cells)


if __name__=='__main__':unittest.main()
