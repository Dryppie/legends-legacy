"""Engineering selector, admission-envelope and saved-row tests; no random draw or game execution."""
import argparse
import copy
import importlib.util
import json
import os
import shutil
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


audit = module('three_tie_audit', ROOT/'Balance Harness/analysis/audit-incumbent-tie-comparison.py')
launcher = module('three_tie_launcher', ROOT/'build/run-three-reference-tie-comparison.py')


class ProtocolTests(unittest.TestCase):
    def test_frozen_plan_matches_selectors_accounting_and_envelope(self):
        path = ROOT/'Balance Harness/Tower-Practical-Three-Reference-Tie-Comparison-Plan.json'
        plan = audit.read(path)
        self.assertEqual(audit.THREE_PLAN, audit.sha(path))
        self.assertEqual((5, 12672, plan['supportDecision'], plan['negativeDecision']), audit.protocol(plan['version']))
        self.assertEqual(plan['restarts']*(46*8+5*32), plan['searchFights'])
        self.assertEqual(plan['searchFights']+2*24000, plan['maximumFights'])
        self.assertEqual(plan['restarts']*(1+8+32+1000), plan['assignedValues'])
        self.assertEqual(plan['maximumSeconds'], launcher.CUMULATIVE_SECONDS)
        self.assertEqual(plan['maximumBytes'], launcher.CUMULATIVE_BYTES)
        self.assertEqual(plan['maximumSeconds']-plan['priorSeconds'], launcher.SECONDS)
        self.assertEqual(plan['maximumBytes']-plan['priorBytes'], launcher.BYTES)
        with self.assertRaises(ValueError):
            audit.protocol('unknown')

    def test_primary_secondary_and_frozen_order_ties_preserve_strict_and_zero_leaders(self):
        rank = {p: i for i, p in enumerate(['x', 'y', 'r3', 'r2', 'r1'])}
        health = dict(x=90, y=1, r3=90, r2=90, r1=90)
        for counts, expected in [([23,20,23,21,20], ('x','r3')), ([23,20,21,23,20], ('x','r2')),
                                 ([23,23,23,23,20], ('x','r3')), ([23]*5, ('r1','r1')),
                                 ([24,20,23,23,20], ('x','x')), ([20,20,24,23,20], ('r3','r3')),
                                 ([23,23,22,22,20], ('x','x')), ([0]*5, ('y','y'))]:
            with self.subTest(counts=counts):
                values = dict(zip(rank, counts))
                self.assertEqual(expected, audit.choices(values, health, rank, 'r1', ['r1','r2','r3'], audit.THREE_VERSION))
                self.assertEqual(expected, audit.choices(dict(reversed(list(values.items()))), health, rank, 'r1', ['r3','r1','r2'], audit.THREE_VERSION))

    def test_missing_reference_and_invalid_measurements_fail_closed(self):
        rank = {p: i for i, p in enumerate(['x','y','r3','r2','r1'])}
        counts, health = dict.fromkeys(rank, 20), dict.fromkeys(rank, 50)
        for refs, primary, values in [(['r1','r2'], 'r1', counts), (['r1','r2','unknown'], 'r1', counts),
                                     (['r1','r2','r3'], 'unknown', counts), (['r1','r2','r3'], 'r1', dict(counts,x=33))]:
            with self.subTest(refs=refs, primary=primary, values=values):
                with self.assertRaises(ValueError):
                    audit.choices(values, health, rank, primary, refs, audit.THREE_VERSION)

    def test_endpoint_keeps_all_roots_one_point_and_replication_gates(self):
        for k, nets, decision in [(0, [], 'NoSelectorDifferences'), (1, [400], 'DoNotPromoteThreeReferenceTie'),
                                 (3, [80,80,79], 'DoNotPromoteThreeReferenceTie'),
                                 (3, [80,80,80], 'SupportsThreeReferenceTieForFrozenOutputs'),
                                 (3, [200,200,-100], 'DoNotPromoteThreeReferenceTie'),
                                 (24, [80,80,80], 'DoNotPromoteThreeReferenceTie')]:
            pairs = [dict(restart=i+1, identical=i>=k, gains=max(0,nets[i] if i<len(nets) else 0),
                          losses=max(0,-nets[i] if i<len(nets) else 0)) for i in range(24)]
            result = audit.endpoint(pairs, 600552, audit.THREE_VERSION)
            self.assertEqual(decision, result['decision'])
            self.assertEqual((24000,12672+2000*k), (result['denominator'],result['fights']))
            self.assertEqual(sum(nets)/24000, result['meanDifference'])

    def test_new_launcher_rejects_old_envelope_and_existing_output(self):
        with tempfile.TemporaryDirectory() as folder:
            q = dict(version=launcher.VERSION, registryRoot=folder, outputRoot=str(Path(folder)/'output'),
                     maximumSeconds=10800, maximumBytes=6442450944, priorSeconds=600, priorBytes=536870912)
            launcher.validate_request(q)
            for key in ['maximumSeconds', 'maximumBytes', 'priorSeconds', 'priorBytes']:
                missing = dict(q); del missing[key]
                with self.assertRaises(ValueError):
                    launcher.validate_request(missing)
            for field, value in [('version',audit.VERSION), ('maximumSeconds',4500), ('maximumBytes',4294967296),
                                 ('priorSeconds',0), ('priorBytes',0)]:
                with self.assertRaises(ValueError):
                    launcher.validate_request(dict(q, **{field:value}))
            (Path(folder)/'output').mkdir()
            with self.assertRaises(ValueError):
                launcher.validate_request(q)


class SavedRowsTests(unittest.TestCase):
    fixture = None

    def test_native_search_and_independent_battle_recount_agree(self):
        root = Path(self.fixture)
        template, allocation = audit.read(root/'template.json'), audit.read(root/'allocation.json')
        expected = audit.read(root/'expected-result.json')
        before = {p: audit.sha(p) for p in root.rglob('*') if p.is_file()}
        actual = audit.audit_rows(root, template, allocation['selected'], template['starts'][0]['party']['id'], audit.THREE_VERSION)
        self.assertTrue(audit.same_numbers(expected, actual))
        self.assertEqual('SupportsThreeReferenceTieForFrozenOutputs', actual['decision'])
        self.assertEqual((3,18672,240), (actual['activeRestarts'],actual['fights'],actual['netWins']))
        self.assertTrue(all(p['baselineWins'] is None and p['candidateWins'] is None for p in actual['pairs'] if p['identical']))
        self.assertEqual(before, {p: audit.sha(p) for p in root.rglob('*') if p.is_file()})
        with self.assertRaises(ValueError):
            audit.audit_rows(root, template, allocation['selected'], template['starts'][0]['party']['id'], audit.VERSION)


    def test_working_envelope_and_source_binding(self):
        # Synthetic capture pins are substituted only in this test. The public CLI
        # always requires the fixed reconciled production capture and exact teams.
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)/'archive'
            shutil.copytree(self.fixture, root)
            def put(name, value):
                path = root/name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(json.dumps(value, indent=2), encoding='utf-8')
            template = audit.read(root/'template.json')
            template['id'] = 'three-reference-tie-template'
            for name in template['contentHashes']:
                path = root/'study/content/Data'/name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text('literal content fixture', encoding='utf-8')
                template['contentHashes'][name] = audit.sha(path)
            put('template.json', template)
            scope = audit.read(root/'study/scope.json')
            scope['contentHashes'] = template['contentHashes']
            put('study/scope.json', scope)
            put('study/files.json', {p.relative_to(root/'study').as_posix(): audit.sha(p)
                                    for p in (root/'study').rglob('*') if p.is_file() and p != root/'study/files.json'})
            put('capture/preset/template.json', dict(template, id='literal-capture'))
            put('capture/files.json', {'preset/template.json': audit.sha(root/'capture/preset/template.json')})
            put('capture/failure.json', {'reason': 'literal reconciled fixture'})
            put('capture/closeout/receipt.json', dict(closeoutStatus='ReadOnlyReconciled', manifestSha256=audit.sha(root/'capture/files.json'), reconciledCloseoutFailureSha256=audit.sha(root/'capture/failure.json')))
            put('capture/closeout/files.json', {'receipt.json': audit.sha(root/'capture/closeout/receipt.json')})
            plan_name = 'Tower-Practical-Three-Reference-Tie-Comparison-Plan.json'
            shutil.copyfile(ROOT/'Balance Harness'/plan_name, root/'plan.json')
            shutil.copyfile(ROOT/'Balance Harness/analysis/audit-incumbent-tie-comparison.py', root/'auditor.py')
            put('result.json', audit.read(root/'expected-result.json'))
            q = dict(version=audit.THREE_VERSION, planHash=audit.THREE_PLAN, auditorHash=audit.sha(root/'auditor.py'),
                     maximumSeconds=10800, maximumBytes=6442450944, priorSeconds=600, priorBytes=536870912,
                     templateHash=audit.sha(root/'template.json'), requiredHistory=audit.read(root/'history-files.json'))
            put('request.json', q)
            request_hash = audit.sha(root/'request.json')
            put('native-receipt.json', dict(version=audit.THREE_VERSION, status='Verified', requestFileHash=request_hash,
                archiveHash=audit.sha(root/'study/files.json'), newAuditFights=0, measuredSeconds=1, observedBytes=1, fights=18672))
            put('launch.json', dict(version=audit.THREE_VERSION, requestFileHash=request_hash, maximumSeconds=10200, maximumBytes=5905580032,
                nativeMaximumSeconds=10080, nativeMaximumBytes=5637144576, mechanism='suspended-owned-job-v1'))
            put('native-audit-process.json', dict(mechanism='suspended-owned-job-v1', exitCode=0, timedOut=False, activeProcesses=0, totalProcesses=1))
            pins = dict(THREE_CAPTURE=audit.sha(root/'capture/files.json'), THREE_CAPTURE_TEMPLATE=audit.sha(root/'capture/preset/template.json'),
                        THREE_CAPTURE_FAILURE=audit.sha(root/'capture/failure.json'), THREE_CLOSEOUT=audit.sha(root/'capture/closeout/files.json'),
                        THREE_PARTIES=[s['party']['id'] for s in template['starts']])
            with patch.multiple(audit, **pins):
                result = audit.audit(root)
                self.assertEqual(('Passed', 18672, 0), (result['status'], result['result']['fights'], result['newFights']))
                (root/'plan.json').write_text('changed', encoding='utf-8')
                with self.assertRaisesRegex(ValueError, 'Unbound plan'):
                    audit.audit(root)


@unittest.skipUnless(os.name == 'nt', 'Owned launcher requires Windows')
class LauncherTests(unittest.TestCase):
    version = launcher.VERSION
    def exercise(self, fail_audit=False):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            output = root/'registry'/'run'
            output.parent.mkdir()
            harness = root/'BalanceHarness.dll'
            harness.write_bytes(b'fixture')
            auditor = ROOT/'Balance Harness/analysis/audit-incumbent-tie-comparison.py'
            q = dict(version=audit.THREE_VERSION, outputRoot=str(output), registryRoot=str(output.parent),
                     maximumSeconds=10800, maximumBytes=6442450944, priorSeconds=600, priorBytes=536870912,
                     auditorPath=str(auditor), auditorHash=launcher.digest(auditor))
            request = root/'request.json'
            launcher.write(request, q)
            calls = []
            def run(command, cwd, log, deadline, **kwargs):
                calls.append((command, deadline))
                Path(log).write_text('fixture')
                result = dict(mechanism='suspended-owned-job-v1', exitCode=0, activeProcesses=0, totalProcesses=1, timedOut=False, seconds=0)
                if len(calls) == 1:
                    launcher.write(output/'native-receipt.json', dict(version=audit.THREE_VERSION, status='Verified',
                        requestFileHash=launcher.digest(output/'request.json'), newAuditFights=0, fights=18672))
                elif len(calls) == 2 and fail_audit:
                    return dict(result, exitCode=1)
                elif len(calls) == 3:
                    launcher.write(output/'independent-audit.json', dict(status='Passed', requestFileHash=launcher.digest(output/'request.json'), newFights=0, newValues=0))
                return result
            with patch('bounded_windows_process.run', side_effect=run):
                if fail_audit:
                    with self.assertRaisesRegex(ValueError, 'Native audit failed'):
                        launcher.launch(request, harness, 'dotnet')
                    self.assertTrue((output/'failure.json').is_file())
                    self.assertFalse((output/'completion.json').exists())
                    self.assertEqual(2, len(calls))
                else:
                    launcher.launch(request, harness, 'dotnet')
                    self.assertEqual(3, len(calls))
                    self.assertEqual(calls[1][1], calls[2][1])
                    self.assertAlmostEqual(115, calls[1][1]-calls[0][1])
                    result = audit.read(output/'completion.json')
                    self.assertEqual(600, result['chargedSeconds']-result['seconds'])
                    self.assertEqual(536870912, result['chargedBytes']-result['observedBytes'])
                    audit.authenticate(output, audit.sha(output/'files.json'))
                    self.assertEqual(result['observedBytes'], sum(p.stat().st_size for p in output.rglob('*') if p.is_file()))

    def test_one_launch_shared_audit_deadline_and_prior_charge(self):
        self.exercise()

    def test_failed_audit_never_publishes_or_retries(self):
        self.exercise(fail_audit=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixture')
    args = parser.parse_args()
    SavedRowsTests.fixture = args.fixture
    classes = [ProtocolTests, LauncherTests] + ([SavedRowsTests] if args.fixture else [])
    suite = unittest.TestSuite(unittest.defaultTestLoader.loadTestsFromTestCase(c) for c in classes)
    sys.exit(not unittest.TextTestRunner(verbosity=2).run(suite).wasSuccessful())
