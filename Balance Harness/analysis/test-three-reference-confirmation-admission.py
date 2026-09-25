"""Admission contract tests; no native preparation, entropy, allocation or combat."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

path=Path(__file__).with_name('prepare-three-reference-confirmation-admission.py')
spec=importlib.util.spec_from_file_location('confirmation_admission',path)
a=importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class AdmissionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan=a.read(a.ROOT/'Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json')
        cls.impl=a.read(a.IMPLEMENTATION/'implementation.json')
        cls.context=dict(settingsHash=a.SETTINGS,executionHash='e'*64)

    def test_exact_family_matches_independent_stored_native_fixture(self):
        definition=a.definition_from(self.plan,[-2,0,3],self.context)
        stored=a.read(a.IMPLEMENTATION/'verified-literal-fixture/result/study/study.json')
        self.assertEqual(stored['freeze']['definition']['teams'],definition['teams'])
        self.assertEqual((a.VERSION,[-2,0,3],'e'*64),
                         (definition['version'],definition['excludedCombatSeeds'],definition['executionHash']))
        self.assertEqual(self.plan['contentHashes'],definition['contentHashes'])

    def test_order_role_reference_scenario_and_schedule_drift_fail(self):
        for change in ('order','omit','role','reference','scenario','schedule'):
            bad=copy.deepcopy(self.plan); teams=bad['recipesInFixedExecutionOrder']
            if change=='order': teams.reverse()
            if change=='omit': teams.pop()
            if change=='role': teams[0]['role']='PrimaryReference'
            if change=='reference': teams[-1]['referenceId']='changed'
            if change=='scenario': teams[0]['scenario']['party'][0]['build']['equipment'][0]['definitionId']='changed'
            if change=='schedule': teams[0]['scenario']['seeds']=[0]
            with self.subTest(change=change),self.assertRaises(ValueError): a.definition_from(bad,[1],self.context)

    def test_protocol_settings_and_invalid_history_fail(self):
        for values in ([],[2,1],[1,1],[True],[1.0],[-2**31-1],[2**31],list(range(987001))):
            with self.subTest(count=len(values)),self.assertRaises(ValueError): a.definition_from(self.plan,values,self.context)
        with self.assertRaises(ValueError): a.definition_from(dict(self.plan,proposedNativeVersion='legacy'),[1],self.context)
        with self.assertRaises(ValueError): a.definition_from(self.plan,[1],dict(self.context,settingsHash='changed'))

    def test_all_tested_harness_artifacts_and_retained_nested_dependencies_bound(self):
        tested=self.impl['testedHarness']; captured=a.read(a.CAPTURE/'files.json')
        runtime={n[8:]:p for n,p in captured.items() if n.startswith('runtime/')}; runtime.update(tested)
        a.validate_runtime(runtime,captured,tested)
        for name in runtime:
            with self.subTest(name=name),self.assertRaises(ValueError):
                a.validate_runtime(dict(runtime,**{name:'changed'}),captured,tested)
        with self.assertRaises(ValueError): a.validate_runtime(dict(runtime,extra='unexpected'),captured,tested)
        missing=dict(runtime); del missing['BalanceHarness.pdb']
        with self.assertRaises(ValueError): a.validate_runtime(missing,captured,tested)
        for name,pin in tested.items(): self.assertEqual(pin,a.sha(a.IMPLEMENTATION/'tested-harness'/name))

    def test_failed_verification_or_changed_engineering_disclosure_fails(self):
        a.validate_implementation(self.impl)
        for key,value in [('status','Incomplete'),('version','legacy'),('backendDistinctPassingTests',80),('pythonPassingTests',17),
            ('scientificFights',1),('authoritativeAllocations',1),('capturedRuntimeAdmitted',True),('runnableScientificRequest',True),
            ('oldEngineeringDisclosure',dict(seconds=0,MiB=0,completeHistoricalEngineeringTotalsKnown=True)),('testedHarness',{})]:
            with self.subTest(key=key),self.assertRaises(ValueError): a.validate_implementation(dict(self.impl,**{key:value}))

    def test_frozen_provenance_and_fixture_pins(self):
        for root,pin in [(a.CAPTURE,a.CAPTURE_PIN),(a.IMPLEMENTATION,a.IMPLEMENTATION_PIN),(a.PREVIOUS_RUN,a.PREVIOUS_RUN_PIN)]:
            self.assertEqual(pin,a.sha(root/'files.json'))
        self.assertEqual(a.PLAN_PIN,a.sha(a.ROOT/'Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json'))
        self.assertEqual(a.OWNER_PIN,a.sha(a.ROOT/'build/bounded_windows_process.py'))
        pins=a.read(a.IMPLEMENTATION/'source-hashes.json'); manifest=a.read(a.IMPLEMENTATION/'files.json')
        for source in a.INPUTS: self.assertEqual(pins[source],a.sha(a.ROOT/source))
        for source in a.PROOF_FILES: self.assertEqual(manifest[source],a.sha(a.IMPLEMENTATION/source))
        for source,target in a.FIXTURES.items():
            self.assertEqual(manifest['verified-literal-fixture/result/'+source],a.sha(a.IMPLEMENTATION/'verified-literal-fixture/result'/source))
            self.assertNotIn(target,a.common.LEDGERS|{'entropy.bin'})

    def test_only_empty_owned_success_counts(self):
        good=dict(exitCode=0,timedOut=False,activeProcesses=0,totalProcesses=1,mechanism='suspended-owned-job-v1')
        a.validate_process(good)
        for key,value in [('exitCode',1),('timedOut',True),('activeProcesses',1),('totalProcesses',0),('mechanism','unowned')]:
            with self.subTest(key=key),self.assertRaises(ValueError): a.validate_process(dict(good,**{key:value}))

    def test_forecast_keeps_hard_limits_and_does_not_claim_feasibility(self):
        result=a.forecast(self.plan,self.impl)
        self.assertTrue(result['fits']); self.assertFalse(result['guaranteedUpperBound'])
        self.assertFalse(result['fullWorkflowFeasibilityEstablished'])
        self.assertAlmostEqual(2*3506.516*52000/55672,result['doubledOwnerSeconds'])
        for key,value in [('ownerBytes',3758096384),('nativeSeconds',6000),('auditSeconds',1200)]:
            bad=copy.deepcopy(self.plan); bad['resources']['sizing'][key]=value
            self.assertFalse(a.forecast(bad,self.impl)['fits'])

    def test_request_binds_full_charge_history_recoveries_and_protocol(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            for name in ('definition.json','admission-charge.json','auditor.py'): (root/name).write_text('{}')
            previous=a.read(a.PREVIOUS_RUN/'request.json'); history={'retained-ledger':'a'*64}
            q=a.request_from(root,history,previous)
            self.assertEqual((a.VERSION,7800,4294967296,600,536870912,{}),
                (q['version'],q['maximumSeconds'],q['maximumBytes'],q['priorSeconds'],q['priorBytes'],q['phases']))
            self.assertEqual((600,536870912),(sum(c['seconds'] for c in q['priorCharges']),sum(c['bytes'] for c in q['priorCharges'])))
            self.assertEqual(a.sha(root/'admission-charge.json'),q['priorCharges'][0]['receiptHash'])
            self.assertEqual(a.sha(root/'definition.json'),q['definitionHash']); self.assertEqual(history,q['requiredHistory'])
            self.assertEqual(previous['pendingHistoryRecoveries'],q['pendingHistoryRecoveries'])
            self.assertEqual(previous['recoveryReceiptHashes'],q['recoveryReceiptHashes'])
            self.assertEqual(a.sha(root/'auditor.py'),q['threeReference']['auditorHash'])
            self.assertEqual(str(a.RUN),q['outputRoot']); self.assertEqual(str(root/'plan.json'),q['threeReference']['planPath'])

    def test_terminal_self_bytes_match_actual_pinned_writer(self):
        with tempfile.TemporaryDirectory() as folder:
            for index,other in enumerate((0,999,1000000000)):
                value=a.terminal_receipt(dict(status='Admitted',retainedBytes=other))
                target=Path(folder)/f'{index}.json'; a.save(target,value)
                self.assertEqual(target.stat().st_size,value['terminalBytes'])

    def test_context_uses_harness_frameworks_and_powershell_dependencies(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); host=root/'pwsh.exe'
            for path in (host,root/'pwsh.dll',root/'pwsh.deps.json'): path.write_text('literal')
            command=a.context_command(a.PACKAGE,str(host))
            self.assertEqual(['dotnet','exec','--runtimeconfig',str(a.PACKAGE/'runtime/BalanceHarness.runtimeconfig.json')],command[:4])
            self.assertEqual(['--depsfile',str(root/'pwsh.deps.json'),str(root/'pwsh.dll')],command[4:7])
            self.assertIn(str(a.PACKAGE/'context.ps1'),command)
            (root/'pwsh.dll').unlink()
            with self.assertRaises(ValueError): a.context_command(a.PACKAGE,str(host))
        with self.assertRaises(ValueError): a.context_command(a.PACKAGE,None)

    def test_existing_package_or_output_cannot_resume(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            with patch.object(a,'PACKAGE',root),self.assertRaisesRegex(ValueError,'no retry or resume'): a.prepare()
            self.assertEqual([],list(root.iterdir()))
            with patch.object(a,'PACKAGE',root/'new'),patch.object(a,'RUN',root),self.assertRaisesRegex(ValueError,'no retry or resume'): a.prepare()
            self.assertFalse((root/'new').exists())


if __name__=='__main__': unittest.main(verbosity=2)
