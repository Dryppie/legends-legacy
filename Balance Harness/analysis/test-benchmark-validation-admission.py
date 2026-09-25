"""Benchmark validation admission boundary tests; no live history, timing probe or combat."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('validation_admission', Path(__file__).with_name('prepare-benchmark-validation-admission.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class DeclarationTests(unittest.TestCase):
    def declaration(self):
        return dict(copy.deepcopy(a.CONTRACT), harnessFiles={n:a.CONTRACT['harnessSha256'] for n in a.base.HARNESS_FILES},
            implementationFiles=dict.fromkeys(a.IMPLEMENTATION,'implementation'),
            tests=dict(path=str(a.ROOT/'TestResults/benchmark-validation-admission-tests-20260924.log'),sha256='log',passed=23,failed=0))

    def validate(self, d):
        def digest(path):
            if path == a.PLAN: return a.CONTRACT['originalPlanFileSha256']
            if path.parent == a.BUILD: return a.CONTRACT['harnessSha256']
            if path.suffix == '.log': return 'log'
            return 'implementation'
        with patch.object(a,'sha',side_effect=digest), patch.object(Path,'read_text',return_value='Ran 23 tests in 0.123s\n\nOK\n'):
            a.validate_declaration(d)

    def test_exact_contract_accepts(self): self.validate(self.declaration())

    def test_scientific_identity_limits_paths_and_retry_policy_are_fixed(self):
        for key,value in [('studyVersion',a.owner.VERSION),('chargedSeconds',901),('scientificMaximumBytes',7000000000),
                ('scientificMaximumSeconds',10801),('retries',1),('timingResamples',1),('scientificLaunches',1),
                ('admissionOnly',False),('output','other'),('scopeId','other'),('precedingChargedSeconds',0)]:
            with self.subTest(key=key), self.assertRaises(ValueError): self.validate(dict(self.declaration(),**{key:value}))

    def test_unknown_or_missing_fields_rejected(self):
        unknown = self.declaration(); unknown['reuseTiming']=True
        missing = self.declaration(); del missing['forecastRule']
        for d in (unknown,missing):
            with self.assertRaises(ValueError): self.validate(d)

    def test_build_implementation_and_test_evidence_must_match(self):
        for field,key,value in [('harnessFiles','BalanceHarness.pdb','changed'),('implementationFiles',a.IMPLEMENTATION[0],'changed'),
                ('tests','failed',1),('tests','passed',19),('tests','sha256','changed'),('tests','path','other')]:
            d = self.declaration(); d[field][key]=value
            with self.subTest(field=field,key=key), self.assertRaises(ValueError): self.validate(d)


class QualificationTests(unittest.TestCase):
    def setUp(self):
        self.context = dict(scope=dict(executionHash='old',settingsHash='settings',stages=dict(schedules={
            'reference':dict(discovery=[12],selection=[34],confirmation=[],diagnostics=[],feedback=[])})))
        execution = dict(runtime='captured',architecture='X64',assemblyHashes={'BalanceHarness':'old','Domain':'same'})
        self.old = dict(execution=execution,executionHash='old',settingsHash='settings',materializations=96,nativePreparations=96,fights=0,newValues=0)
        self.new = copy.deepcopy(self.old); self.new['execution']['assemblyHashes']['BalanceHarness']=a.CONTRACT['harnessSha256']
        self.new.update(executionHash='new',replayedArms=4,compiledSourceDocuments=223,jitResolvedMethods=['BalanceHarness.'+name+'#1' for name in ('TowerAffinityCreation.Create','TowerProposalComparison.CreateValidationPlan','TowerProposalComparison.ValidateValidationTrajectories','TowerBatchRacing.Select','TowerBenchmarkValidation.Gate','TowerBenchmarkValidation.Decide')])
        self.captured = dict(execution=copy.deepcopy(execution),executionHash='old')
        self.rows = [dict(ordinal=i,seed=12,scenarioHash='recipe',inputHash='input',participantsHash='prepared') for i in range(96)]
        self.export = dict(arms=[dict(name=name,teams=[dict(scenario=dict(id=str(i),seeds=[],party=['fixed'])) for i in range(12)])
            for name in ('legacy-v1','benchmark-single-control-v2','benchmark-damage-preserving-single-v2','benchmark-affinity-creation-v3')])

    def qualify(self, new=None, after=None):
        a.qualified(self.old,new or self.new,self.rows,self.rows if after is None else after,self.captured,self.context)

    def test_all_recipes_use_only_two_declared_historical_values_without_mutation(self):
        before=copy.deepcopy(self.export); rows=a.probes(self.export,self.context)
        self.assertEqual(self.export,before); self.assertEqual(len(rows),48)
        self.assertTrue(all(r['seeds'] == r['scenario']['seeds'] == [12,34] for r in rows))

    def test_missing_duplicate_or_uneven_arm_coverage_rejected(self):
        missing=copy.deepcopy(self.export); missing['arms'].pop()
        duplicate=copy.deepcopy(self.export); duplicate['arms'][0]['name']=duplicate['arms'][1]['name']
        uneven=copy.deepcopy(self.export); uneven['arms'][0]['teams'].append(uneven['arms'][1]['teams'].pop())
        for export in (missing,duplicate,uneven):
            with self.assertRaises(ValueError): a.probes(export,self.context)

    def test_complete_native_qualification_accepts(self): self.qualify()

    def test_any_changed_native_input_or_participant_rejected(self):
        for key in ('inputHash','participantsHash','scenarioHash','seed'):
            changed=copy.deepcopy(self.rows); changed[-1][key]='changed'
            with self.subTest(key=key), self.assertRaises(ValueError): self.qualify(after=changed)
        with self.assertRaises(ValueError): self.qualify(after=self.rows[:-1])
        self.rows[-1]['ordinal']=0
        with self.assertRaises(ValueError): self.qualify()

    def test_gameplay_platform_replay_sources_or_combat_drift_rejected(self):
        for field,value in [('materializations',95),('nativePreparations',95),('fights',1),('newValues',1),
                ('replayedArms',3),('compiledSourceDocuments',0),('jitResolvedMethods',['Other.Create']),('settingsHash','other')]:
            with self.subTest(field=field), self.assertRaises(ValueError): self.qualify(new=dict(self.new,**{field:value}))
        for field,value in [('runtime','other'),('assemblyHashes',{'BalanceHarness':a.CONTRACT['harnessSha256'],'Domain':'changed'})]:
            changed=copy.deepcopy(self.new); changed['execution'][field]=value
            with self.assertRaises(ValueError): self.qualify(new=changed)

    def test_every_selector_entry_point_must_resolve(self):
        for missing in self.new['jitResolvedMethods']:
            changed=dict(self.new,jitResolvedMethods=[m for m in self.new['jitResolvedMethods'] if m != missing])
            with self.subTest(missing=missing), self.assertRaises(ValueError): self.qualify(new=changed)

    def test_runtime_binding_cannot_change_selector_or_generator(self):
        original=a.read(a.PLAN)
        derived=dict(original,scopeHash='different')
        a.base.validate_plan(original,derived)
        for field,value in [('version',a.owner.CREATION_VERSION),('selectionContrast',None),
                            ('control',dict(original['control'],name='different')),('roots',13),('requiredFreshValues',3948),('validationProtocol',None)]:
            with self.subTest(field=field), self.assertRaises(ValueError):
                a.base.validate_plan(original,dict(derived,**{field:value}))


class ForecastTests(unittest.TestCase):
    def setUp(self):
        self.previous=dict(nativeSeconds=4200,auditSeconds=1300,nativeBytes=3000,auditBytes=400,totalBytes=3400)
        self.probe=dict(status='AuditPartitionStillNotSupported',timingMargin=2,publicationReserveSeconds=120,
            wholeWorkerSeconds=170,independentAuditSeconds=[35,15],extraInventorySeconds=370,inventoryByteRatio=4,auditPlanningSeconds=1300)
        self.estimate=dict(self.previous,nativeSeconds=4100,auditSeconds=1200)
        self.completed=dict(status='Complete',nativeSeconds=1200,auditSeconds=245,nativeBytes=1900,auditBytes=25)
        self.creation=dict(status='Complete',nativeSeconds=260,auditSeconds=55,nativeBytes=632,auditBytes=2.6)
        self.preservation=dict(status='Complete',nativeSeconds=300,auditSeconds=63,nativeBytes=620,auditBytes=2.6)

    def result(self):
        return a.forecast(self.previous,self.probe,850,self.estimate,self.completed,17792,self.creation,self.preservation)

    def test_completed_storage_floor_scales_audit_before_admission(self):
        result=self.result(); floor=2*1900*(21888/17792)*(632/620)
        self.assertAlmostEqual(floor,result['nativeBytes'])
        expected=2*(170+35+15+370*((floor+400)/3400))+120
        self.assertAlmostEqual(expected,result['auditSeconds'])
        self.assertTrue(result['fits']); self.assertFalse(result['guaranteedUpperBound'])
        self.assertEqual(result['nativeMaximumSeconds'],9000); self.assertEqual(result['auditMaximumSeconds'],1800)

    def test_faster_fixture_cannot_reduce_prior_costs_or_margins(self):
        result=self.result()
        for key in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes'):
            self.assertGreaterEqual(result[key],self.previous[key]); self.assertGreaterEqual(result[key],self.estimate[key])
            self.assertGreaterEqual(result['upwardOnlyFixtureRatios'][key],1)
        self.assertEqual((result['timingMargin'],result['publicationReserveSeconds']),(2,120))

    def test_current_audit_cost_cannot_be_masked_by_old_probe(self):
        self.estimate['auditSeconds']=1801
        result=self.result(); self.assertEqual(result['auditSeconds'],1801); self.assertFalse(result['fits'])
        with self.assertRaises(ValueError): a.base.admit_forecast(result)

    def test_each_partition_rejects_without_borrowing(self):
        for key,value in [('nativeSeconds',9000),('auditSeconds',1800),('nativeBytes',a.owner.NATIVE_BYTES),('auditBytes',a.owner.AUDIT_BYTES)]:
            with self.subTest(key=key):
                saved=self.estimate; self.estimate=dict(saved,**{key:value})
                self.assertFalse(self.result()['fits']); self.estimate=saved

    def test_invalid_costs_or_unfinished_source_rejected(self):
        for value in (0,-1,float('nan'),float('inf'),True):
            saved=self.creation; self.creation=dict(saved,nativeSeconds=value)
            with self.subTest(value=value), self.assertRaises(ValueError): self.result()
            self.creation=saved
        self.completed['status']='Pending'
        with self.assertRaises(ValueError): self.result()

    def test_larger_archive_or_new_fixture_cost_never_lowers_forecast(self):
        before=self.result(); self.creation['nativeBytes']*=1.5; self.creation['auditSeconds']*=2
        after=self.result()
        for key in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes'):
            self.assertGreaterEqual(after[key],before[key])
        self.assertFalse(after['fits'])

    def test_other_study_cannot_replace_selector_cost_basis(self):
        with self.assertRaises(ValueError):
            a.forecast(self.previous,self.probe,850,self.estimate,self.completed,17536,self.creation,self.preservation)


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='selector-admission-evidence-'); self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name); a.save(self.root/'phase.json',{'status':'Complete'})
        a.save(self.root/'files.json',a.inventory(self.root)); self.pin=a.sha(self.root/'files.json')

    def test_external_manifest_pin_required(self):
        with self.assertRaises(ValueError): a.Evidence(self.root,'0'*64)

    def test_consumed_file_and_recheck_authenticate_original_bytes(self):
        e=a.Evidence(self.root,self.pin); self.assertEqual(e.get('phase.json'),self.root/'phase.json'); e.recheck()
        (self.root/'phase.json').write_text('{}')
        with self.assertRaises(ValueError): e.get('phase.json')
        with self.assertRaises(ValueError): e.recheck()

    def test_unlisted_or_escaping_file_rejected(self):
        e=a.Evidence(self.root,self.pin)
        for name in ('missing.json','../phase.json'):
            with self.subTest(name=name), self.assertRaises(ValueError): e.get(name)

    def test_manifest_change_after_consumption_rejected(self):
        e=a.Evidence(self.root,self.pin); e.get('phase.json'); (self.root/'files.json').write_text('{}')
        with self.assertRaises(ValueError): e.recheck()

    def test_repository_indexed_implementation_proof_is_authenticated(self):
        verification=self.root/'verification'; verification.mkdir()
        a.save(verification/'completion.json',dict(status='proof'))
        a.save(verification/'files.json',{'verification/completion.json':a.sha(verification/'completion.json')})
        with patch.object(a,'ROOT',self.root), patch.object(a,'VERIFICATION',verification):
            evidence=a.Evidence(verification,a.sha(verification/'files.json'))
            self.assertEqual(evidence.get('completion.json'),verification/'completion.json')
            evidence.recheck()
            (verification/'completion.json').write_text('{}')
            with self.assertRaises(ValueError): evidence.recheck()


if __name__ == '__main__': unittest.main(verbosity=2)
