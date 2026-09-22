"""Engineering-only arithmetic, saved-row and process-ownership fixtures; no combat or random draws."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import struct
import sys
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[1]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


audit = module('tie_audit', ROOT/'Balance Harness/analysis/audit-incumbent-tie-comparison.py')
launcher = module('tie_launch', ROOT/'build/run-incumbent-tie-comparison.py')


class ArithmeticTests(unittest.TestCase):
    def pairs(self, k, nets):
        return [dict(restart=i+1, identical=i >= k, gains=max(0, nets[i] if i < len(nets) else 0),
                     losses=max(0, -nets[i] if i < len(nets) else 0)) for i in range(24)]

    def test_boundaries(self):
        for k, nets, decision in [(0, [], 'NoSelectorDifferences'), (1, [400], 'DoNotPromoteIncumbentTie'),
                                 (3, [80, 80, 79], 'DoNotPromoteIncumbentTie'),
                                 (3, [80, 80, 80], 'SupportsIncumbentTieForFrozenOutputs'),
                                 (3, [200, 200, -100], 'DoNotPromoteIncumbentTie'),
                                 (24, [80, 80, 80], 'DoNotPromoteIncumbentTie')]:
            with self.subTest(k=k, nets=nets):
                result = audit.endpoint(self.pairs(k, nets), 505562)
                self.assertEqual(decision, result['decision'])
                self.assertEqual(sum(nets)/24000, result['meanDifference'])
                self.assertEqual(11904+2000*k, result['fights'])

    def test_full_entropy_and_tail(self):
        words = list(range(32768))
        words[1] = 0
        selected, reserved, collisions, duplicates = audit.classify(struct.pack('<32768i', *words), [3])
        self.assertEqual((24984, 32766, 1, 1), (len(selected), len(reserved), collisions, duplicates))
        self.assertEqual(selected[984:1984], list(range(986, 1986)))
        with self.assertRaises(ValueError):
            audit.classify(b'\0'*131072, [])

    def test_incomplete_and_capacity(self):
        with self.assertRaises(ValueError):
            audit.endpoint(self.pairs(0, [])[:-1], 0)
        with self.assertRaises(ValueError):
            audit.endpoint(self.pairs(0, []), 967233)

    def test_request_caps_and_no_reuse(self):
        with tempfile.TemporaryDirectory() as root:
            q = dict(version=launcher.VERSION, outputRoot=str(Path(root)/'new'), registryRoot=root)
            self.assertEqual(Path(root)/'new', launcher.validate_request(q))
            for field, value in [('maximumSeconds', 4501), ('maximumBytes', 4294967297), ('version', 'unknown')]:
                with self.assertRaises(ValueError):
                    launcher.validate_request(dict(q, **{field: value}))
            (Path(root)/'new').mkdir()
            with self.assertRaises(ValueError):
                launcher.validate_request(q)

    def test_manifest_authentication(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root/'data.txt').write_text('fixed')
            (root/'files.json').write_text(json.dumps({'data.txt': audit.sha(root/'data.txt')}))
            pin = audit.sha(root/'files.json')
            audit.authenticate(root, pin)
            (root/'data.txt').write_text('changed')
            with self.assertRaises(ValueError):
                audit.authenticate(root, pin)


@unittest.skipUnless(os.name == 'nt', 'Windows Job Object tests require Windows')
class OwnershipTests(unittest.TestCase):
    def run_owned(self, code, seconds=5, check=None):
        from bounded_windows_process import run
        with tempfile.TemporaryDirectory() as folder:
            return run([sys.executable, '-c', code], folder, str(Path(folder)/'process.log'),
                       time.monotonic()+seconds, check=check)

    def test_waits_for_child_after_root_exit(self):
        code = "import subprocess,sys; subprocess.Popen([sys.executable,'-c','import time; time.sleep(.4)'])"
        result = self.run_owned(code)
        self.assertEqual(0, result['exitCode'])
        self.assertFalse(result['timedOut'])
        self.assertGreaterEqual(result['totalProcesses'], 2)  # Bundled Python may have a launcher process.
        self.assertEqual(0, result['activeProcesses'])

    def test_timeout_kills_tree_and_proves_empty(self):
        code = "import subprocess,sys,time; subprocess.Popen([sys.executable,'-c','import time; time.sleep(30)']); time.sleep(30)"
        result = self.run_owned(code, seconds=.5)
        self.assertTrue(result['timedOut'])
        self.assertEqual(0, result['activeProcesses'])
        self.assertGreaterEqual(result['totalProcesses'], 2)

    def test_storage_callback_failure_drains_owned_tree(self):
        calls = []
        def check():
            calls.append(1)
            if len(calls) > 1:
                raise ValueError('Fixture storage cap')
        with self.assertRaisesRegex(ValueError, 'Fixture storage cap'):
            self.run_owned('import time; time.sleep(30)', check=check)
        self.assertEqual(2, len(calls))


class SavedRowsTests(unittest.TestCase):
    fixture = None

    def test_native_literal_archive_and_independent_recount_agree(self):
        self.assertIsNotNone(self.fixture, 'Supply --fixture from the backend archive test')
        root = Path(self.fixture)
        template, allocation = audit.read(root/'template.json'), audit.read(root/'allocation.json')
        actual = audit.audit_rows(root, template, allocation['selected'], template['starts'][0]['party']['id'])
        self.assertTrue(audit.same_numbers(audit.read(root/'expected-result.json'), actual))
        self.assertEqual('SupportsIncumbentTieForFrozenOutputs', actual['decision'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixture')
    args = parser.parse_args()
    SavedRowsTests.fixture = args.fixture
    suite = unittest.TestSuite(unittest.defaultTestLoader.loadTestsFromTestCase(c)
                               for c in [ArithmeticTests, OwnershipTests] + ([SavedRowsTests] if args.fixture else []))
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    sys.exit(not result.wasSuccessful())
