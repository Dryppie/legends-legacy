"""Zero-combat checks of the adaptive pilot, independent recount and owned launcher."""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import struct
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


audit = module('adaptive_audit', ROOT/'Balance Harness/analysis/audit-reference-exploration-comparison.py')
launcher = module('adaptive_launch', ROOT/'build/run-reference-exploration-comparison.py')


class ProtocolTests(unittest.TestCase):
    def test_plan_hash_and_fresh_allocation(self):
        self.assertEqual(audit.ADAPTIVE_PLAN, audit.sha(ROOT/'Balance Harness/Tower-Adaptive-Racing-Comparison-Plan.json'))
        selected, reserved, collisions, duplicates = audit.classify(struct.pack('<16384i', *range(16384)), [], audit.ADAPTIVE_VERSION)
        self.assertEqual((4428, 16384, 0, 0), (len(selected), len(reserved), collisions, duplicates))
        self.assertEqual(1356, audit.search_values(audit.ADAPTIVE_VERSION))
        with self.assertRaises(ValueError):
            audit.endpoint([], 0, audit.ADAPTIVE_VERSION)

    def test_go_abandon_and_inconclusive_boundaries(self):
        for method, benchmark, promising, expected in [(.02, 0, 3, 'LargerFreshEvaluationWarranted'),
                (.0199, .1, 12, 'InconclusiveRetainBaselineAndBenchmark'), (.1, -.001, 12, 'InconclusiveRetainBaselineAndBenchmark'),
                (.1, .1, 2, 'InconclusiveRetainBaselineAndBenchmark'), (-.02, .1, 12, 'AbandonThisConfiguration'),
                (.1, -.02, 2, 'AbandonThisConfiguration'), (.1, -.02, 3, 'InconclusiveRetainBaselineAndBenchmark')]:
            self.assertEqual(expected, audit.pilot_decision(method, benchmark, promising))

    def test_shared_rows_have_zero_method_difference_but_preserve_absolute_regression(self):
        output = [i < 100 for i in range(256)]
        reference = [i < 140 for i in range(256)]
        roots = [audit.pilot_root(i+1, output, output, reference, 'r', True) for i in range(12)]
        pairs = [dict(restart=i+1, baselineParty='same', candidateParty='same', identical=True, baselineWins=100,
                      candidateWins=100, gains=0, losses=0, difference=0., recipeCount=4) for i in range(12)]
        result = audit.pilot_endpoint(pairs, roots, 0)
        self.assertEqual('AbandonThisConfiguration', result['decision'])
        self.assertEqual((0, 0), (roots[0]['methodSeedStandardError'], roots[0]['seedCovariance']))
        self.assertEqual(-40/256, result['pilot']['benchmarkRoots']['mean'])
        self.assertEqual(24960, result['fights'])

    def test_launcher_has_its_own_envelope_and_one_shared_audit_deadline(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); output = root/'run'; harness = root/'BalanceHarness.dll'; harness.write_bytes(b'fixture')
            auditor = ROOT/'Balance Harness/analysis/audit-reference-exploration-comparison.py'
            q = dict(version=audit.ADAPTIVE_VERSION, outputRoot=str(output), registryRoot=str(root), priorSeconds=0, priorBytes=0,
                     auditorPath=str(auditor), auditorHash=launcher.digest(auditor))
            self.assertEqual(output, launcher.validate_request(q))
            for change in (dict(priorSeconds=600), dict(priorBytes=1), dict(maximumSeconds=10801)):
                with self.assertRaises(ValueError):
                    launcher.validate_request(dict(q, **change))
            launcher.write(root/'request.json', q)
            deadlines = []
            def run(command, cwd, log, deadline, **kwargs):
                deadlines.append(deadline); Path(log).write_text('literal fixture')
                result = dict(mechanism='suspended-owned-job-v1', exitCode=0, activeProcesses=0, totalProcesses=1, timedOut=False, seconds=0)
                if len(deadlines) == 1:
                    launcher.write(output/'native-receipt.json', dict(version=q['version'], status='Verified',
                        requestFileHash=launcher.digest(output/'request.json'), newAuditFights=0, fights=24960))
                elif len(deadlines) == 3:
                    launcher.write(output/'independent-audit.json', dict(status='Passed', requestFileHash=launcher.digest(output/'request.json'), newFights=0))
                return result
            with patch('bounded_windows_process.run', side_effect=run):
                launcher.launch(root/'request.json', harness, 'dotnet')
            self.assertEqual(len(deadlines), 3); self.assertEqual(deadlines[1], deadlines[2])
            self.assertAlmostEqual(115, deadlines[1]-deadlines[0])
            completion = audit.read(output/'completion.json')
            self.assertEqual(completion['seconds'], completion['chargedSeconds'])
            self.assertEqual(completion['observedBytes'], completion['chargedBytes'])
            audit.authenticate(output, audit.sha(output/'files.json'))


class ArchiveTests(unittest.TestCase):
    fixture = None

    def recount(self):
        root = Path(self.fixture); template = audit.read(root/'template.json')
        return audit.audit_rows(root, template, audit.read(root/'allocation.json')['selected'],
                                template['starts'][0]['party']['id'], audit.ADAPTIVE_VERSION)

    def test_native_and_independent_full_recount_agree(self):
        actual = self.recount(); expected = audit.read(Path(self.fixture)/'expected-result.json')
        self.assertTrue(audit.same_numbers(dict(expected, descriptiveViews=[]), dict(actual, descriptiveViews=[])))
        self.assertTrue(audit.same_numbers(expected['descriptiveViews'], actual['descriptiveViews'], 2e-9))
        self.assertEqual(('LargerFreshEvaluationWarranted', 24960, 12),
                         (actual['decision'], actual['fights'], actual['pilot']['promisingNovelOutputs']))

    def test_resealed_semantic_tampering_is_rejected(self):
        original = audit.read
        def apply(change):
            def altered(path):
                value = original(path)
                def walk(node):
                    if isinstance(node, dict):
                        change(node)
                        for child in list(node.values()):
                            walk(child)
                    elif isinstance(node, list):
                        for child in node:
                            walk(child)
                if path.parent == Path(self.fixture)/'study':
                    walk(value)
                return value
            with patch.object(audit, 'read', side_effect=altered), self.assertRaises(ValueError):
                self.recount()
        def panel(node):
            if node.get('role') == 'wave-2-screen' and 'plannedEvaluations' in node:
                node['seeds'].reverse()
        def score(node):
            if node.get('samples') == 40 and 'wins' in node:
                node['wins'] += 1
        def survivor(node):
            if 'survivorIds' in node:
                node['survivorIds'].reverse()
        def output(node):
            if node.get('generator') == audit.ADAPTIVE_POLICY:
                node['selector'] = 'changed'
        for change in (panel, score, survivor, output):
            with self.subTest(change=change.__name__):
                apply(change)

    def test_working_audit_authenticates_capture_scope_reservation_and_both_results(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)/'fixture'; shutil.copytree(self.fixture, root)
            def put(name, value):
                path = root/name; path.parent.mkdir(parents=True, exist_ok=True); path.write_text(json.dumps(value), encoding='utf-8')
            template = audit.read(root/'template.json')
            template['id'] = 'reference-exploration-template'
            for name in template['contentHashes']:
                path = root/'study/content/Data'/name; path.parent.mkdir(parents=True, exist_ok=True); path.write_text('literal content')
                template['contentHashes'][name] = audit.sha(path)
            put('template.json', template)
            scope = audit.read(root/'study/scope.json'); scope['contentHashes'] = template['contentHashes']; put('study/scope.json', scope)
            put('study/files.json', {p.relative_to(root/'study').as_posix(): audit.sha(p)
                                    for p in (root/'study').rglob('*') if p.is_file() and p.name != 'files.json'})
            put('capture/preset/template.json', dict(template, id='literal-capture'))
            put('capture/files.json', {'preset/template.json': audit.sha(root/'capture/preset/template.json')})
            put('capture/failure.json', {'reason': 'literal reconciled fixture'})
            put('capture/closeout/receipt.json', {'closeoutStatus': 'ReadOnlyReconciled'})
            put('capture/closeout/files.json', {'receipt.json': audit.sha(root/'capture/closeout/receipt.json')})
            shutil.copyfile(ROOT/'Balance Harness/Tower-Adaptive-Racing-Comparison-Plan.json', root/'plan.json')
            shutil.copyfile(ROOT/'Balance Harness/analysis/audit-reference-exploration-comparison.py', root/'auditor.py')
            put('result.json', audit.read(root/'expected-result.json'))
            put('request.json', dict(version=audit.ADAPTIVE_VERSION, priorSeconds=0, priorBytes=0, planHash=audit.ADAPTIVE_PLAN,
                auditorHash=audit.sha(root/'auditor.py'), templateHash=audit.sha(root/'template.json'), requiredHistory=audit.read(root/'history-files.json')))
            request_hash = audit.sha(root/'request.json')
            put('native-receipt.json', dict(version=audit.ADAPTIVE_VERSION, status='Verified', requestFileHash=request_hash,
                archiveHash=audit.sha(root/'study/files.json'), newAuditFights=0, measuredSeconds=1, observedBytes=1, fights=24960))
            put('launch.json', dict(version=audit.ADAPTIVE_VERSION, requestFileHash=request_hash, maximumSeconds=10800, maximumBytes=6442450944,
                nativeMaximumSeconds=10680, nativeMaximumBytes=6174015488, mechanism='suspended-owned-job-v1'))
            put('native-audit-process.json', dict(mechanism='suspended-owned-job-v1', exitCode=0, timedOut=False, activeProcesses=0))
            pins = dict(CAPTURE=audit.sha(root/'capture/files.json'), CAPTURE_TEMPLATE=audit.sha(root/'capture/preset/template.json'),
                CAPTURE_FAILURE=audit.sha(root/'capture/failure.json'), CLOSEOUT=audit.sha(root/'capture/closeout/files.json'),
                PARTIES=[s['party']['id'] for s in template['starts']])
            with patch.multiple(audit, **pins):
                self.assertEqual('Passed', audit.audit(root)['status'])
                (root/'plan.json').write_text('changed')
                with self.assertRaisesRegex(ValueError, 'Unbound plan'):
                    audit.audit(root)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--fixture'); args = parser.parse_args()
    ArchiveTests.fixture = args.fixture
    classes = [ProtocolTests] + ([ArchiveTests] if args.fixture else [])
    suite = unittest.TestSuite(unittest.defaultTestLoader.loadTestsFromTestCase(c) for c in classes)
    raise SystemExit(not unittest.TextTestRunner(verbosity=2).run(suite).wasSuccessful())
