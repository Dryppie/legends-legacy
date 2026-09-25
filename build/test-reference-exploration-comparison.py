"""Engineering-only entropy, decision, ownership and literal saved-row fixtures."""
import argparse
import copy
import importlib.util
import json
import os
from pathlib import Path
import shutil
import struct
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


audit = module('exploration_audit', ROOT/'Balance Harness/analysis/audit-reference-exploration-comparison.py')
launcher = module('exploration_launch', ROOT/'build/run-reference-exploration-comparison.py')


class ArithmeticTests(unittest.TestCase):
    version = audit.VERSION
    def pairs(self, nets, identical=False, recipes=4):
        return [dict(restart=i+1, baselineParty='a', candidateParty='a' if identical else 'b', identical=identical,
                     baselineWins=400, candidateWins=400+n, gains=max(0, n), losses=max(0, -n), difference=n/1000,
                     recipeCount=recipes) for i, n in enumerate(nets)]

    def test_decision_boundaries(self):
        for nets, decision in [([50]*12, 'SupportsReferenceExplorationForFrozenOutputs'),
                               ([50]*11+[49], 'DoNotPromoteReferenceExploration'),
                               ([100]*6+[0]*6, 'DoNotPromoteReferenceExploration'),
                               ([100]*5+[50]*2+[0]*5, 'SupportsReferenceExplorationForFrozenOutputs'),
                               ([-50]*12, 'DoNotPromoteReferenceExploration')]:
            result = audit.endpoint(self.pairs(nets), 551408, self.version)
            self.assertEqual(audit.protocol(self.version)[2 if decision.startswith('Supports') else 3], result['decision'])
            self.assertEqual(sum(nets)/12000, result['meanDifference'])
            self.assertEqual(60672, result['fights'])
        result = audit.endpoint(self.pairs([0]*12, identical=True, recipes=3), 551408, self.version)
        self.assertEqual('NoSelectedOutputDifferences', result['decision'])
        self.assertEqual(0, result['margin'])

    def test_physical_union_costs(self):
        for recipes, fights in [(3, 48672), (4, 60672), (5, 72672)]:
            self.assertEqual(fights, audit.endpoint(self.pairs([80]*12, recipes=recipes), 551408, self.version)['fights'])

    def test_entropy_collision_duplicate_tail_and_exhaustion(self):
        words = list(range(16384))
        words[1] = 0
        selected, reserved, collisions, duplicates = audit.classify(struct.pack('<16384i', *words), [3], self.version)
        self.assertEqual((audit.assigned_values(self.version), 16382, 1, 1), (len(selected), len(reserved), collisions, duplicates))
        first = audit.search_values(self.version)
        self.assertEqual(list(range(first+2, first+1002)), selected[first:first+1000])
        for raw, history in [(b'\0'*65536, []), (b'', []), (struct.pack('<16384i', *words), [2, 1])]:
            with self.assertRaises(ValueError):
                audit.classify(raw, history, self.version)

    def test_incomplete_or_impossible_endpoint(self):
        pairs = self.pairs([0]*12)
        for value, history in [(pairs[:-1], 0), (pairs, 983617), ([dict(pairs[0], gains=1001)]+pairs[1:], 0)]:
            with self.assertRaises(ValueError):
                audit.endpoint(value, history, self.version)

    def test_request_caps_prior_charge_and_no_reuse(self):
        with tempfile.TemporaryDirectory() as folder:
            q = dict(version=self.version, outputRoot=str(Path(folder)/'new'), registryRoot=folder)
            self.assertEqual(Path(folder)/'new', launcher.validate_request(q))
            for field, value in [('maximumSeconds', 10801), ('maximumBytes', 6442450945), ('priorSeconds', 0),
                                 ('priorBytes', 0), ('version', 'unknown')]:
                with self.assertRaises(ValueError):
                    launcher.validate_request(dict(q, **{field: value}))
            (Path(folder)/'new').mkdir()
            with self.assertRaises(ValueError):
                launcher.validate_request(q)

    def test_manifest_membership_and_digest(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root/'data').write_text('fixed')
            (root/'files.json').write_text(json.dumps({'data': audit.sha(root/'data')}))
            pin = audit.sha(root/'files.json')
            audit.authenticate(root, pin)
            (root/'extra').write_text('extra')
            with self.assertRaises(ValueError):
                audit.authenticate(root, pin)
            (root/'extra').unlink()
            (root/'data').write_text('changed')
            with self.assertRaises(ValueError):
                audit.authenticate(root, pin)


@unittest.skipUnless(os.name == 'nt', 'Windows Job Object tests require Windows')
class OwnershipTests(unittest.TestCase):
    def run_owned(self, code, seconds=5, check=None):
        from bounded_windows_process import run
        with tempfile.TemporaryDirectory() as folder:
            return run([sys.executable, '-c', code], folder, str(Path(folder)/'process.log'), time.monotonic()+seconds, check=check)

    def test_waits_for_child_after_root_exit(self):
        result = self.run_owned("import subprocess,sys; subprocess.Popen([sys.executable,'-c','import time; time.sleep(.4)'])")
        self.assertEqual(0, result['exitCode'])
        self.assertFalse(result['timedOut'])
        self.assertGreaterEqual(result['totalProcesses'], 2)
        self.assertEqual(0, result['activeProcesses'])

    def test_timeout_drains_tree(self):
        result = self.run_owned("import subprocess,sys,time; subprocess.Popen([sys.executable,'-c','import time; time.sleep(30)']); time.sleep(30)", seconds=.5)
        self.assertTrue(result['timedOut'])
        self.assertEqual(0, result['activeProcesses'])

    def test_storage_callback_failure_drains_tree(self):
        calls = []
        def check():
            calls.append(1)
            if len(calls) > 1:
                raise ValueError('Fixture storage cap')
        with self.assertRaisesRegex(ValueError, 'Fixture storage cap'):
            self.run_owned('import time; time.sleep(30)', check=check)
        self.assertEqual(2, len(calls))


class SavedRowsTests(unittest.TestCase):
    version = audit.VERSION
    fixture = None

    def test_native_literal_archive_and_independent_recount_agree(self):
        root = Path(self.fixture)
        template, allocation = audit.read(root/'template.json'), audit.read(root/'allocation.json')
        actual = audit.audit_rows(root, template, allocation['selected'], template['starts'][0]['party']['id'], self.version)
        expected = audit.read(root/'expected-result.json')
        self.assertTrue(audit.same_numbers(dict(expected, descriptiveViews=[]), dict(actual, descriptiveViews=[])))
        self.assertTrue(audit.same_numbers(expected['descriptiveViews'], actual['descriptiveViews'], 2e-9))
        self.assertEqual(audit.protocol(self.version)[2], actual['decision'])
        self.assertEqual((60672, 24), (actual['fights'], len(actual['descriptiveViews'])))

    def test_changed_selector_rejected_without_mutating_archive(self):
        root = Path(self.fixture)
        template, allocation = audit.read(root/'template.json'), audit.read(root/'allocation.json')
        original = audit.read
        def changed(path):
            value = original(path)
            if path.name == 'pair-01-baseline.json':
                value['output']['selector'] = 'altered-selector'
            return value
        with patch.object(audit, 'read', side_effect=changed), self.assertRaisesRegex(ValueError, 'Changed saved search'):
            audit.audit_rows(root, template, allocation['selected'], template['starts'][0]['party']['id'], self.version)

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
            template['id'] = 'reference-exploration-template'
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
            put('capture/closeout/receipt.json', {'closeoutStatus': 'ReadOnlyReconciled'})
            put('capture/closeout/files.json', {'receipt.json': audit.sha(root/'capture/closeout/receipt.json')})
            plan_name = 'Tower-Practical-Reference-Exploration-' + ('Offset-' if self.version == audit.OFFSET_VERSION else '') + 'Comparison-Plan.json'
            if self.version == audit.SCREENING_VERSION:
                plan_name = 'Tower-Practical-Fresh-Screening-Comparison-Plan.json'
            shutil.copyfile(ROOT/'Balance Harness'/plan_name, root/'plan.json')
            shutil.copyfile(ROOT/'Balance Harness/analysis/audit-reference-exploration-comparison.py', root/'auditor.py')
            put('result.json', audit.read(root/'expected-result.json'))
            q = dict(version=self.version, planHash=audit.protocol(self.version)[1], auditorHash=audit.sha(root/'auditor.py'),
                     templateHash=audit.sha(root/'template.json'), requiredHistory=audit.read(root/'history-files.json'))
            put('request.json', q)
            request_hash = audit.sha(root/'request.json')
            put('native-receipt.json', dict(version=self.version, status='Verified', requestFileHash=request_hash,
                archiveHash=audit.sha(root/'study/files.json'), newAuditFights=0, measuredSeconds=1, observedBytes=1, fights=60672))
            put('launch.json', dict(version=self.version, requestFileHash=request_hash, maximumSeconds=10200, maximumBytes=5905580032,
                nativeMaximumSeconds=10080, nativeMaximumBytes=5637144576, mechanism='suspended-owned-job-v1'))
            put('native-audit-process.json', dict(mechanism='suspended-owned-job-v1', exitCode=0, timedOut=False, activeProcesses=0))
            pins = dict(CAPTURE=audit.sha(root/'capture/files.json'), CAPTURE_TEMPLATE=audit.sha(root/'capture/preset/template.json'),
                        CAPTURE_FAILURE=audit.sha(root/'capture/failure.json'), CLOSEOUT=audit.sha(root/'capture/closeout/files.json'),
                        PARTIES=[s['party']['id'] for s in template['starts']])
            with patch.multiple(audit, **pins):
                result = audit.audit(root)
                self.assertEqual(('Passed', 60672, 0), (result['status'], result['result']['fights'], result['newFights']))
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
            auditor = ROOT/'Balance Harness/analysis/audit-reference-exploration-comparison.py'
            q = dict(version=self.version, outputRoot=str(output), registryRoot=str(output.parent),
                     auditorPath=str(auditor), auditorHash=launcher.digest(auditor))
            request = root/'request.json'
            launcher.write(request, q)
            calls = []
            def run(command, cwd, log, deadline, **kwargs):
                calls.append((command, deadline))
                Path(log).write_text('fixture')
                result = dict(mechanism='suspended-owned-job-v1', exitCode=0, activeProcesses=0, totalProcesses=1, timedOut=False, seconds=0)
                if len(calls) == 1:
                    launcher.write(output/'native-receipt.json', dict(version=self.version, status='Verified',
                        requestFileHash=launcher.digest(output/'request.json'), newAuditFights=0, fights=60672))
                elif len(calls) == 2 and fail_audit:
                    return dict(result, exitCode=1)
                elif len(calls) == 3:
                    launcher.write(output/'independent-audit.json', dict(status='Passed', requestFileHash=launcher.digest(output/'request.json'), newFights=0))
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

    def test_one_launch_shared_audit_deadline_and_prior_charge(self):
        self.exercise()

    def test_failed_audit_never_publishes_or_retries(self):
        self.exercise(fail_audit=True)


class OffsetArithmeticTests(ArithmeticTests):
    version = audit.OFFSET_VERSION

    def test_unknown_protocol_and_distinct_plan_mapping(self):
        self.assertNotEqual(audit.protocol(self.version)[1], audit.protocol(audit.VERSION)[1])
        with self.assertRaises(ValueError):
            audit.protocol('unknown')


class OffsetSavedRowsTests(SavedRowsTests):
    version = audit.OFFSET_VERSION

    def test_original_protocol_cannot_relabel_offset_archive(self):
        root = Path(self.fixture)
        with self.assertRaisesRegex(ValueError, 'Changed global freeze'):
            audit.audit_rows(root, audit.read(root/'template.json'), audit.read(root/'allocation.json')['selected'],
                             audit.read(root/'template.json')['starts'][0]['party']['id'], audit.VERSION)


class OffsetLauncherTests(LauncherTests):
    version = launcher.OFFSET_VERSION


class ScreeningArithmeticTests(ArithmeticTests):
    version = audit.SCREENING_VERSION

    def test_screening_allocation_and_conditional_population(self):
        self.assertEqual((588, 12588), (audit.search_values(self.version), audit.assigned_values(self.version)))
        rows = self.pairs([50]*12)
        result = audit.endpoint(rows, 584171, self.version)
        self.assertEqual(12000*11999/((2**32-584171-588)*12000), result['depletion'])
        self.assertGreater(result['depletion'], audit.endpoint(rows, 584171, audit.VERSION)['depletion'])
        self.assertEqual(3, len({audit.protocol(v)[1] for v in (audit.VERSION, audit.OFFSET_VERSION, self.version)}))


class ScreeningSavedRowsTests(SavedRowsTests):
    version = audit.SCREENING_VERSION

    def test_resealed_screening_membership_nomination_and_panel_changes_fail(self):
        root = Path(self.fixture)
        template, allocation = audit.read(root/'template.json'), audit.read(root/'allocation.json')
        original = audit.read
        # Change every duplicate checkpoint consistently; rejection must be semantic.
        def audit_change(mutator):
            def changed(path):
                value = original(path)
                def walk(node):
                    if isinstance(node, dict):
                        for child in list(node.values()):
                            walk(child)
                        mutator(node)
                    elif isinstance(node, list):
                        for child in node:
                            walk(child)
                if path.parent == root/'study':
                    walk(value)
                return value
            with patch.object(audit, 'read', side_effect=changed), self.assertRaises(ValueError):
                audit.audit_rows(root, template, allocation['selected'], template['starts'][0]['party']['id'], self.version)
        def change_members(node):
            if 'afterDiscoveryFights' in node:
                node['candidates'].reverse()
        def change_nominees(node):
            if 'afterSearchFights' in node:
                node['nominees'].reverse()
        def change_panel(node):
            if 'afterDiscoveryFights' in node:
                node['seeds'] = allocation['selected'][1:9]
        def partial(node):
            if 'afterSearchFights' in node:
                node['measurements'].pop()
        def relabel(node):
            if node.get('pipeline') == audit.SCREENING_PIPELINE:
                node['pipeline'] = audit.DIRECT_PIPELINE
        def missing_control(node):
            if 'afterDiscoveryFights' in node:
                node['candidates'] = [p for p in node['candidates'] if p['id'] != template['starts'][0]['party']['id']]
        for mutator in (change_members, change_nominees, change_panel, partial, relabel, missing_control):
            with self.subTest(change=mutator.__name__):
                audit_change(mutator)

    def test_legacy_protocol_cannot_relabel_screened_allocation(self):
        root = Path(self.fixture)
        for version in (audit.VERSION, audit.OFFSET_VERSION):
            with self.assertRaisesRegex(ValueError, 'Changed panels'):
                audit.audit_rows(root, audit.read(root/'template.json'), audit.read(root/'allocation.json')['selected'],
                                 audit.read(root/'template.json')['starts'][0]['party']['id'], version)


class ScreeningLauncherTests(LauncherTests):
    version = launcher.SCREENING_VERSION


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixture')
    parser.add_argument('--offset-fixture')
    parser.add_argument('--screening-fixture')
    args = parser.parse_args()
    SavedRowsTests.fixture = args.fixture
    OffsetSavedRowsTests.fixture = args.offset_fixture
    ScreeningSavedRowsTests.fixture = args.screening_fixture
    classes = [ArithmeticTests, OffsetArithmeticTests, ScreeningArithmeticTests, OwnershipTests, LauncherTests, OffsetLauncherTests, ScreeningLauncherTests] + ([SavedRowsTests] if args.fixture else []) + ([OffsetSavedRowsTests] if args.offset_fixture else []) + ([ScreeningSavedRowsTests] if args.screening_fixture else [])
    suite = unittest.TestSuite(unittest.defaultTestLoader.loadTestsFromTestCase(c) for c in classes)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    sys.exit(not result.wasSuccessful())
