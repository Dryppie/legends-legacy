"""Read-only admission contract tests. No capture, preparation, entropy or combat."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

path = Path(__file__).with_name('prepare-three-reference-tie-admission.py')
spec = importlib.util.spec_from_file_location('three_tie_admission',path)
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class AdmissionTests(unittest.TestCase):
    def test_template_preserves_exact_three_references_and_direct_scope(self):
        source = a.read(a.CAPTURE/'preset/template.json')
        result = a.template_from(source,[1,2],dict(settingsHash=a.SETTINGS,executionHash='e'*64))
        self.assertEqual(source['starts'],result['starts'])
        self.assertEqual(source['references'],result['references'])
        self.assertEqual(('three-reference-tie-template',4528,[],5),
                         (result['id'],result['maximumBattles'],result['generation']['seeds'],result['stages']['shortlist']))
        self.assertEqual('tower-staged-incumbent-tie-v1',result['stages']['selectionPolicyVersion'])

    def test_history_and_settings_mismatch_fail(self):
        source = a.read(a.CAPTURE/'preset/template.json')
        for values,settings in [([2,1],a.SETTINGS),([1,1],a.SETTINGS),([],a.SETTINGS),([1],'changed')]:
            with self.subTest(values=values,settings=settings),self.assertRaises(ValueError):
                a.template_from(source,values,dict(settingsHash=settings,executionHash='e'*64))

    def test_selector_nominees_primary_and_schedules_cannot_drift(self):
        source = a.read(a.CAPTURE/'preset/template.json')
        for key,value in [('shortlist',4),('selectionPolicyVersion','tower-staged-three-reference-tie-v1'),('selectionPrimaryReferenceId','changed')]:
            bad = copy.deepcopy(source); bad['stages'][key] = value
            with self.assertRaises(ValueError): a.template_from(bad,[1],dict(settingsHash=a.SETTINGS,executionHash='e'*64))
        bad = copy.deepcopy(source); next(iter(bad['stages']['schedules'].values()))['selection'] = [1]
        with self.assertRaises(ValueError): a.template_from(bad,[2],dict(settingsHash=a.SETTINGS,executionHash='e'*64))

    def test_all_four_tested_harness_files_and_nested_dependencies_are_required(self):
        runtime = {name:'tested-'+name for name in a.HARNESS_FILES}
        runtime['runtimes/win/lib/net7.0/System.Management.dll'] = 'retained'
        captured = {'runtime/'+name:'retained' for name in runtime}
        a.validate_runtime(runtime,captured,runtime)
        for name in runtime:
            with self.subTest(name=name),self.assertRaises(ValueError): a.validate_runtime(dict(runtime,**{name:'changed'}),captured,runtime)
        with self.assertRaises(ValueError): a.validate_runtime(dict(runtime,extra='new'),captured,runtime)
        tested = a.tested_binaries(a.IMPLEMENTATION)
        self.assertEqual({name:a.sha(a.BUILD/name) for name in a.HARNESS_FILES},tested)

    def test_failed_implementation_or_changed_accounting_is_rejected(self):
        source = a.read(a.IMPLEMENTATION/'completion.json'); a.validate_implementation(source)
        for key,value in [('status','Incomplete'),('backendTests',240),('pythonTests',17),('historyValues',600551),
                          ('capturedRuntimeAdmitted',True),('newScientificValues',1),('previousEngineeringMiB',0),('fullHistoricalEngineeringTotals',0)]:
            with self.subTest(key=key),self.assertRaises(ValueError): a.validate_implementation(dict(source,**{key:value}))

    def test_frozen_plan_source_and_literal_fixture_pins_match(self):
        pins = a.tested_sources(a.IMPLEMENTATION)
        for source in a.INPUTS: self.assertEqual(a.sha(a.ROOT/source),pins[source])
        self.assertEqual(a.PLAN_PIN,a.sha(a.ROOT/'Balance Harness/Tower-Practical-Three-Reference-Tie-Comparison-Plan.json'))
        self.assertEqual(a.OWNER_PIN,a.sha(a.ROOT/'build/bounded_windows_process.py'))
        manifest = a.read(a.IMPLEMENTATION/'files.json')
        for source in a.FIXTURE_FILES: self.assertEqual(a.sha(a.IMPLEMENTATION/source),manifest[source])
        self.assertFalse(a.common.LEDGERS.intersection(a.FIXTURE_FILES.values()))
        self.assertEqual(203,len(a.read(a.IMPLEMENTATION/'source-files.json')))

    def test_owned_tree_must_finish_empty(self):
        good = dict(exitCode=0,timedOut=False,activeProcesses=0,totalProcesses=1,mechanism='suspended-owned-job-v1')
        a.validate_process(good)
        for key,value in [('exitCode',1),('timedOut',True),('activeProcesses',1),('totalProcesses',0),('mechanism','unowned')]:
            with self.assertRaises(ValueError): a.validate_process(dict(good,**{key:value}))

    def test_cost_forecast_uses_latest_owned_execution_and_audits(self):
        source = a.read(a.PREVIOUS_EXECUTION/'closeout.json'); estimate = a.forecast(source)
        self.assertTrue(estimate['fits']); self.assertFalse(estimate['guaranteedUpperBound'])
        self.assertEqual((55672,60672),(estimate['sourceFights'],estimate['targetMaximumFights']))
        self.assertAlmostEqual(source['executionSeconds']*60672/55672,estimate['estimatedExecutionSeconds'],places=9)
        self.assertTrue(estimate['includesHistoricalAuditAndPublicationCosts'])
        self.assertFalse(a.forecast(dict(source,executionSeconds=20000))['fits'])
        for key,value in [('nativeAudit','Failed'),('scope','Open'),('retries',1)]:
            with self.assertRaises(ValueError): a.forecast(dict(source,**{key:value}))

    def test_existing_package_cannot_resume(self):
        with tempfile.TemporaryDirectory() as folder:
            existing = Path(folder)
            with patch.object(a,'PACKAGE',existing),self.assertRaisesRegex(ValueError,'no retry or resume'): a.prepare()
            self.assertEqual([],list(existing.iterdir()))

    def test_concrete_request_binds_version_paths_history_and_prior_charge(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root/'template.json').write_text('{}'); (root/'auditor.py').write_text('# literal')
            previous = a.read(a.PREVIOUS_RUN/'request.json')
            q = a.request_from(root,{'prior':'pinned'},previous)
            self.assertEqual(a.VERSION,q['version']); self.assertEqual(str(a.RUN),q['outputRoot'])
            self.assertEqual((10800,6442450944,600,536870912),(q['maximumSeconds'],q['maximumBytes'],q['priorSeconds'],q['priorBytes']))
            self.assertEqual(previous['pendingHistoryRecoveries'],q['pendingHistoryRecoveries'])
            self.assertEqual(previous['recoveryReceiptHashes'],q['recoveryReceiptHashes'])
            self.assertEqual({'prior':'pinned'},q['requiredHistory'])
            self.assertEqual(a.sha(root/'template.json'),q['templateHash'])


if __name__ == '__main__': unittest.main(verbosity=2)
