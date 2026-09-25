"""Seed-free admission contract and failure-boundary tests; never dispatch a harness."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('admission', Path(__file__).with_name('prepare-adaptive-racing-admission.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class AdmissionTests(unittest.TestCase):
    def setUp(self):
        self.capture = a.read(a.CAPTURE/'preset/template.json')
        self.context = dict(settingsHash=a.SETTINGS, executionHash='a'*64)

    def test_template_changes_only_declared_fields(self):
        original = copy.deepcopy(self.capture)
        result = a.template_from(self.capture, [-9, 0, 12], self.context)
        expected = copy.deepcopy(original)
        expected.update(id='reference-exploration-template', executionHash='a'*64,
                        excludedCombatSeeds=[-9, 0, 12], maximumBattles=1552)
        expected.pop('primaryReferenceId', None)
        expected['generation'].update(policyVersion='retained-composition-three-references-v1', seeds=[])
        self.assertEqual(expected, result); self.assertEqual(original, self.capture)

    def test_history_rejects_duplicates_order_boolean_float_and_range(self):
        for values in ([], [3, 2], [2, 2], [True], [1.0], [2**31], [-2**31-1]):
            with self.subTest(values=values), self.assertRaises(ValueError): a.template_from(self.capture, values, self.context)

    def test_changed_settings_rejected(self):
        with self.assertRaises(ValueError): a.template_from(self.capture, [1], dict(self.context, settingsHash='b'*64))

    def test_premature_panels_rejected(self):
        next(iter(self.capture['stages']['schedules'].values()))['confirmation'] = [7]
        with self.assertRaises(ValueError): a.template_from(self.capture, [1], self.context)

    def test_premature_reference_panel_rejected(self):
        self.capture['references'][0]['scenario']['seeds'] = [7]
        with self.assertRaises(ValueError): a.template_from(self.capture, [1], self.context)

    def test_primary_and_baseline_shape_preserved(self):
        for container, name, value in [('generation', 'candidatesPerArm', 45), ('stages', 'selectionPrimaryReferenceId', 'other')]:
            capture = copy.deepcopy(self.capture); capture[container][name] = value
            with self.subTest(name=name), self.assertRaises(ValueError): a.template_from(capture, [1], self.context)

    def test_only_tested_harness_can_replace_captured_runtime(self):
        captured = {'runtime/'+n: 'old' for n in a.TESTED}
        captured.update({'runtime/Common.dll': 'captured', 'runtime/runtimes/x/native.dll': 'native'})
        runtime = dict(a.TESTED, **{'Common.dll': 'captured', 'runtimes/x/native.dll': 'native'})
        a.validate_runtime(runtime, captured)
        for name in runtime:
            changed = dict(runtime); changed[name] = 'changed'
            with self.subTest(name=name), self.assertRaises(ValueError): a.validate_runtime(changed, captured)

    def test_runtime_membership_is_exact(self):
        captured = {'runtime/'+n: 'old' for n in a.TESTED}
        for changed in ({}, dict(a.TESTED, extra='foreign')):
            with self.assertRaises(ValueError): a.validate_runtime(changed, captured)

    def test_request_keeps_admission_charge_separate(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); (root/'template.json').write_text('{}'); (root/'auditor.py').write_text('# bound')
            old = dict(pendingHistoryRecoveries={'pending': 'receipt'}, recoveryReceiptHashes={'receipt': 'hash'})
            q = a.request_from(root, {'ledger': 'pin'}, old)
            self.assertEqual((q['version'], q['maximumSeconds'], q['maximumBytes'], q['priorSeconds'], q['priorBytes']),
                             (a.VERSION, 10800, 6442450944, 0, 0))
            self.assertEqual(q['requiredHistory'], {'ledger': 'pin'})
            self.assertEqual(q['pendingHistoryRecoveries'], old['pendingHistoryRecoveries'])
            self.assertEqual(q['recoveryReceiptHashes'], old['recoveryReceiptHashes'])
            self.assertEqual((a.charge()['chargedSeconds'], a.charge()['chargedBytes']), (600, 536870912))

    def test_context_host_uses_harness_frameworks(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); executable = root/'pwsh.exe'
            (root/'pwsh.dll').touch(); (root/'pwsh.deps.json').touch()
            command = a.context_command(root, str(executable))
            self.assertEqual(command[:4], ['dotnet', 'exec', '--runtimeconfig', str(root/'runtime/BalanceHarness.runtimeconfig.json')])
            self.assertEqual(command[4:7], ['--depsfile', str(root/'pwsh.deps.json'), str(root/'pwsh.dll')])
            self.assertEqual(command[-1], '-AdaptiveRacing')
            self.assertNotIn(str(executable), command)

    def test_missing_managed_host_rejected(self):
        with self.assertRaises(ValueError): a.context_command(Path.cwd(), None)
        with tempfile.TemporaryDirectory() as temp, self.assertRaises(ValueError): a.context_command(Path(temp), str(Path(temp)/'pwsh.exe'))

    def test_process_receipt_requires_success_and_empty_job(self):
        ok = dict(exitCode=0, timedOut=False, activeProcesses=0, totalProcesses=2, mechanism='suspended-owned-job-v1')
        a.validate_process(ok)
        for key, value in [('exitCode', 1), ('timedOut', True), ('activeProcesses', 1), ('totalProcesses', 0), ('mechanism', 'unowned')]:
            with self.subTest(key=key), self.assertRaises(ValueError): a.validate_process(dict(ok, **{key: value}))

    def test_failed_or_incomplete_backend_receipts_rejected(self):
        source = (a.ROOT/'TestResults/adaptive-comparison-final-tests-20260923.trx').read_text(encoding='utf-8-sig')
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp)/'tests.trx'; path.write_text(source, encoding='utf-8'); a.validate_tests(path)
            for changed in (source.replace('passed="180"', 'passed="179"'), source.replace('outcome="Passed"', 'outcome="Failed"', 1)):
                path.write_text(changed, encoding='utf-8')
                with self.assertRaises(ValueError): a.validate_tests(path)

    def test_terminal_receipt_counts_its_own_bytes(self):
        value = a.terminal_receipt(dict(status='Admitted', retainedBytes=123456, measuredSeconds=42.123))
        self.assertEqual(value['terminalBytes'], len((json.dumps(value, indent=2, allow_nan=False)+'\n').encode('utf-8')))

    def test_existing_package_fails_before_any_mutation(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(a, 'PACKAGE', Path(temp)):
            with patch.object(a.shutil, 'copyfile') as copyfile, self.assertRaises(ValueError): a.prepare()
            copyfile.assert_not_called()

    def test_existing_scientific_output_rejects_verification(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(a, 'RUN', Path(temp)), self.assertRaises(ValueError):
            a.verify_contents(Path(temp)/'package')


class SecondPilotTests(unittest.TestCase):
    def setUp(self):
        self.b = a.module('pilot_02_admission', Path(__file__).with_name('prepare-adaptive-racing-admission.py'))
        self.b.configure_pilot_02()
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.package = Path(self.temp.name)
        sources = {
            self.b.ROOT/'Balance Harness/Tower-Adaptive-Racing-Pilot-02-Declaration.json': 'study-declaration.json',
            self.b.ROOT/'build/run-reference-exploration-comparison.py': 'run-reference-exploration-comparison.py',
            self.b.PRIOR_EXECUTION/'files.json': 'prior-pilot/closeout-files.json',
            self.b.PRIOR_EXECUTION/'failure-closeout.json': 'prior-pilot/closeout.json',
            self.b.PRIOR_EXECUTION/'failed-files.json': 'prior-pilot/failed-inventory.json',
            self.b.HISTORY: 'history-source.json',
        }
        sources.update({self.b.ROOT/n: target for n, target in self.b.INPUTS.items() if target in self.b.REPAIR_PROOFS})
        for source, target in sources.items():
            dest = self.package/target; dest.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(source, dest)
        shutil.copyfile(self.b.HISTORY, self.package/'history-files.json')

    def test_separate_identity_keeps_protocol_and_complete_prior_history(self):
        self.assertEqual((649696, 242), (self.b.HISTORY_VALUES, self.b.HISTORY_FILES))
        self.assertNotEqual(a.PACKAGE, self.b.PACKAGE); self.assertNotEqual(a.RUN, self.b.RUN)
        self.assertEqual(a.VERSION, self.b.VERSION); self.assertEqual(a.PLAN_PIN, self.b.PLAN_PIN)
        self.assertEqual(a.TESTED, self.b.TESTED)
        self.assertEqual((633313, 240), (a.HISTORY_VALUES, a.HISTORY_FILES))

    def test_second_pilot_binds_repair_failure_and_prospective_declaration(self):
        result = self.b.verify_pilot_02(self.package)
        self.assertEqual(self.b.STUDY_ID, result['studyId'])
        self.assertFalse(result['reusePreviousObservations'])
        declaration = self.b.read(self.package/'study-declaration.json')
        self.assertFalse(declaration['previousStudy']['reuseRootsOrObservations'])
        self.assertFalse(declaration['previousStudy']['poolResults'])
        self.assertFalse(declaration['change']['searchAlgorithmChanged'])

    def test_old_launcher_cannot_be_admitted_as_repaired(self):
        old = a.ROOT/'TestResults/adaptive-racing-comparison-admission-20260923/run-reference-exploration-comparison.py'
        shutil.copyfile(old, self.package/'run-reference-exploration-comparison.py')
        with self.assertRaisesRegex(ValueError, 'launcher repair'): self.b.verify_pilot_02(self.package)

    def test_failed_run_reservations_cannot_be_omitted_or_changed(self):
        path = self.package/'history-files.json'; original = self.b.read(path)
        for name in ('history-input.json', 'seed-ledger.json'):
            changed = dict(original); changed.pop(str(self.b.PRIOR_RUN/name))
            path.write_text(json.dumps(changed), encoding='utf-8')
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, 'Failed pilot reservations'):
                self.b.verify_pilot_02(self.package)

    def test_prior_failure_receipt_cannot_be_rewritten(self):
        path = self.package/'prior-pilot/closeout.json'
        value = self.b.read(path); value['completedFights'] = 0
        path.write_text(json.dumps(value), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'predecessor receipt'): self.b.verify_pilot_02(self.package)

    def test_declaration_cannot_authorize_observation_reuse(self):
        path = self.package/'study-declaration.json'
        value = self.b.read(path); value['previousStudy']['poolResults'] = True
        path.write_text(json.dumps(value), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'study declaration'): self.b.verify_pilot_02(self.package)

    def test_profile_selection_cannot_be_reapplied(self):
        with self.assertRaisesRegex(ValueError, 'already selected'): self.b.configure_pilot_02()


if __name__ == '__main__': unittest.main(verbosity=2)
