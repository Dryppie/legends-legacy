"""Admission contract, accounting and provenance tests; no preparation or combat."""
import copy
from contextlib import nullcontext
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('affinity_admission', Path(__file__).with_name('prepare-affinity-creation-recognition.py'))
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)


class AdmissionTests(unittest.TestCase):
    def inputs(self):
        native, closed, sizes = {}, {}, {}
        for label, count, version, status in [('pilot', 16768, 'tower-benchmark-validation-comparison-v1', 'MeasuredPendingAudits'),
            ('recognition', 27648, 'tower-frozen-pool-recognition-v1', 'Verified')]:
            native[label] = dict(version=version, fights=count, status=status, measuredSeconds=1000,
                observedBytes=count*21000+100000000, newAuditFights=0)
            closed[label] = dict(version=version, filesHash=p.PINS[label][1], measuredSeconds=1200,
                retainedBytes=count*21000+102000000, auditSeconds=200, auditBytes=2000000)
            sizes[label] = dict(reports=count, bytes=count*21000, maximumBytes=21000,
                sizes={f'battles/{i}.json.gz': 21000 for i in range(count)})
        return native, closed, sizes, dict(nativeBytes=60000000, auditBytes=1000000), 10

    def test_forecast_uses_worst_time_maximum_report_and_margin(self):
        result = p.forecast(*self.inputs())
        self.assertAlmostEqual(2*(1000*27648/16768+10), result['nativeSeconds'])
        self.assertAlmostEqual(2*(200*27648/16768+10), result['auditSeconds'])
        self.assertEqual(2*(27648*21000+100000000+64*1048576), result['nativeBytes'])
        self.assertTrue(result['fits'])
        self.assertFalse(result['guaranteedUpperBound'])

    def test_forecast_native_cap_cannot_borrow_from_audit(self):
        args = self.inputs(); args[0]['pilot']['measuredSeconds'] = 4000; args[1]['pilot']['measuredSeconds'] = 4200
        with self.assertRaises(ValueError): p.forecast(*args)

    def test_forecast_audit_cap_cannot_borrow_from_native(self):
        args = self.inputs(); args[1]['pilot']['auditSeconds'] = 800
        with self.assertRaises(ValueError): p.forecast(*args)

    def test_forecast_native_storage_cap(self):
        args = self.inputs(); args[3]['nativeBytes'] = 3*1073741824
        with self.assertRaises(ValueError): p.forecast(*args)

    def test_forecast_audit_storage_cap(self):
        args = self.inputs(); args[3]['auditBytes'] = 300*1048576
        with self.assertRaises(ValueError): p.forecast(*args)

    def test_forecast_rejects_wrong_source_and_incomplete_counts(self):
        for change in ('version', 'fights', 'status', 'pin', 'missing'):
            args = self.inputs()
            if change == 'version': args[0]['pilot']['version'] = p.VERSION
            elif change == 'fights': args[0]['pilot']['fights'] -= 1
            elif change == 'status': args[0]['recognition']['status'] = 'Failed'
            elif change == 'pin': args[1]['pilot']['filesHash'] = '0'*64
            else: args[2]['pilot']['sizes'].popitem()
            with self.assertRaises(ValueError): p.forecast(*args)

    def test_forecast_rejects_nonfinite_negative_and_false_summaries(self):
        for change in ('nan', 'negative', 'total', 'maximum'):
            args = self.inputs()
            if change == 'nan': args[0]['pilot']['measuredSeconds'] = float('nan')
            elif change == 'negative': args[1]['pilot']['auditBytes'] = -1
            elif change == 'total': args[2]['pilot']['bytes'] += 1
            else: args[2]['pilot']['maximumBytes'] = 1
            with self.assertRaises(ValueError): p.forecast(*args)

    def test_runtime_substitution_only_replaces_four_harness_files(self):
        tested = {n: 'new-'+n for n in p.HARNESS}
        captured = {'runtime/Services.LL.dll': 'captured', **{'runtime/'+n: 'old' for n in tested}}
        actual = {'Services.LL.dll': 'captured', **tested}
        p.validate_runtime(actual, captured, tested)
        for bad in (dict(actual, **{'Services.LL.dll': 'changed'}), dict(actual, extra='x'), tested):
            with self.assertRaises(ValueError): p.validate_runtime(bad, captured, tested)
        with self.assertRaises(ValueError): p.validate_runtime(actual, captured, dict(tested, **{'Services.LL.dll': 'changed'}))

    def test_definition_is_exact_new_profile_without_recipe_redraw(self):
        plan = p.read(p.PLAN)
        d = p.definition(plan, [-987, 10], dict(executionHash='a'*64))
        self.assertEqual(p.VERSION, d['version'])
        self.assertEqual(108, len(d['teams']))
        self.assertEqual([-987, 10], d['excludedCombatSeeds'])
        self.assertEqual([t['scenario'] for r in plan['roots'] for t in r['teams']], [t['scenario'] for t in d['teams']])
        self.assertEqual(plan['capturedRuntime']['contentHashes'], d['contentHashes'])

    def test_request_preserves_recoveries_and_has_only_this_admission_charge(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for name in ('admission-charge.json', 'definition.json', 'auditor.py'): (root/name).write_text('{}')
            previous = dict(pendingHistoryRecoveries={'pending': 'receipt'}, recoveryReceiptHashes={'receipt': 'pin'})
            q = p.request(root, {'history': 'hash'}, previous)
            self.assertEqual(previous['pendingHistoryRecoveries'], q['pendingHistoryRecoveries'])
            self.assertEqual(previous['recoveryReceiptHashes'], q['recoveryReceiptHashes'])
            self.assertEqual({'history': 'hash'}, q['requiredHistory'])
            self.assertEqual((7800, 600, 4294967296, 536870912), (q['maximumSeconds'], q['priorSeconds'], q['maximumBytes'], q['priorBytes']))
            self.assertEqual(1, len(q['priorCharges']))
            self.assertEqual(str(p.OUTPUT), q['outputRoot'])

    def test_backend_proof_requires_this_profile_and_all_passes(self):
        path = p.VERIFICATION/'backend-tests.trx'
        p.validate_tests(path)
        with tempfile.TemporaryDirectory() as tmp:
            changed = Path(tmp)/'tests.trx'
            text = path.read_text(encoding='utf-8-sig')
            for replacement in (text.replace('outcome="Passed"', 'outcome="Failed"', 1),
                text.replace('BalanceHarnessAffinityCreationRecognitionTests', 'WrongProfile')):
                changed.write_text(replacement)
                with self.assertRaises(ValueError): p.validate_tests(changed)

    def test_declaration_unknown_fields_rejected(self):
        with self.assertRaises(ValueError): p.validate_declaration(dict(p.CONTRACT, unknown=True))

    def test_declaration_changed_budget_rejected_before_files(self):
        d = dict(p.CONTRACT, chargedSeconds=601, harnessFiles={}, implementationFiles={}, tests={})
        with self.assertRaises(ValueError): p.validate_declaration(d)

    def test_exact_new_plan_and_captured_source_bindings(self):
        plan = p.read(p.PLAN)
        self.assertEqual(p.audit.PLAN, p.sha(p.PLAN))
        self.assertEqual(plan['sources']['admissionManifestSha256'], p.PINS['capture'][1])
        self.assertEqual(plan['sources']['scientificManifestSha256'], p.PINS['pilot'][1])
        self.assertNotEqual(p.audit.PLAN, p.sha(ROOT/'Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json'))

    def population_copy(self, root):
        shutil.copyfile(p.PLAN, root/'plan.json')
        for label, (folder, _) in p.POPULATION.items():
            shutil.copytree(ROOT/'TestResults'/folder, root/'population'/label)

    def test_population_reproduces_without_entropy_source(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); self.population_copy(root)
            with patch('secrets.token_bytes', side_effect=AssertionError('Unexpected entropy')):
                p.population(root)

    def test_changed_sampling_bytes_or_plan_fail(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); self.population_copy(root)
            entropy = root/'population/sampling/entropy.bin'
            original = entropy.read_bytes(); entropy.write_bytes(b'\x00'+original[1:])
            with self.assertRaises(ValueError): p.population(root)
            entropy.write_bytes(original)
            (root/'plan.json').write_text('{}')
            with self.assertRaises(ValueError): p.population(root)

    def test_process_requires_owned_success_and_no_descendants(self):
        ok = dict(mechanism='suspended-owned-job-v1', exitCode=0, timedOut=False, activeProcesses=0, totalProcesses=1)
        p.process_ok(ok)
        for bad in (dict(ok, exitCode=1), dict(ok, timedOut=True), dict(ok, activeProcesses=1), dict(ok, mechanism='unowned')):
            with self.assertRaises(ValueError): p.process_ok(bad)

    def test_report_scan_rejects_changed_bytes_and_wrong_counts(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); (root/'battles').mkdir(); (root/'battles/a.json.gz').write_bytes(b'literal')
            class Source: pass
            e = Source(); e.root = root; e.files = {'study/battles/a.json.gz': 'bad'}
            with self.assertRaises(ValueError): p.report_sizes(e, 2, lambda: None)
            e.files = {'x/battles/a.json.gz': 'bad'}; (root/'x/battles').mkdir(parents=True); (root/'x/battles/a.json.gz').write_bytes(b'literal')
            with self.assertRaises(ValueError): p.report_sizes(e, 1, lambda: None)

    def test_admission_failure_preserves_charge_and_cannot_retry(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); package = root/'admission'; registry = root/'registry'; registry.mkdir(); output = registry/'run'
            declaration = root/'declaration.json'; declaration.write_text('{}')
            with patch.object(p, 'PACKAGE', package), patch.object(p, 'OUTPUT', output), patch.object(p, 'validate_declaration'), \
                patch.object(p.launcher, 'writer_lease', side_effect=lambda _: nullcontext()), \
                patch.object(p, 'Evidence', side_effect=ValueError('Injected provenance failure')):
                with self.assertRaisesRegex(ValueError, 'Injected'): p.prepare(declaration)
                charge = (package/'admission-charge.json').read_bytes()
                self.assertEqual(600, p.read(package/'failure.json')['chargedSeconds'])
                self.assertFalse(output.exists())
                self.assertFalse((package/'entropy.bin').exists())
                with self.assertRaisesRegex(ValueError, 'new Windows'): p.prepare(declaration)
                self.assertEqual(charge, (package/'admission-charge.json').read_bytes())


if __name__ == '__main__':
    unittest.main(verbosity=2)
