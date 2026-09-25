"""Read-only closeout tests: saved receipts and literal integers, no game or entropy draw."""
import copy
import importlib.util
import math
from pathlib import Path
import struct
import unittest

ROOT = Path(__file__).resolve().parents[2]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


close = module('three_tie_closeout', ROOT/'Balance Harness/analysis/closeout-reference-exploration-comparison.py')
audit = module('admitted_three_tie_auditor', ROOT/'TestResults/three-reference-tie-admission-20260923/auditor.py')


class CloseoutTests(unittest.TestCase):
    def setUp(self):
        close.configure_three_tie()
        self.result = close.read(ROOT/'TestResults/three-reference-tie-implementation-20260923/three-fixture/expected-result.json')

    def test_exact_admitted_binding(self):
        q = close.read(close.ADMISSION/'request.json')
        self.assertEqual(close.sha(close.ADMISSION/'files.json'), close.ADMISSION_PIN)
        self.assertEqual(close.sha(close.ADMISSION/'request.json'), close.REQUEST_PIN)
        self.assertEqual(q['version'], close.VERSION)
        self.assertEqual(Path(q['outputRoot']), close.RUN)

    def test_saved_literal_endpoint_and_conditional_denominator(self):
        close.verify_selector_endpoint(self.result)
        template = close.read(ROOT/'TestResults/three-reference-tie-implementation-20260923/three-fixture/template.json')
        saved = close.reproduce_primary(audit, self.result['pairs'], len(template['excludedCombatSeeds']))
        self.assertTrue(audit.same_numbers(saved, self.result))
        actual = close.reproduce_primary(audit, self.result['pairs'], 600552)
        depletion = 3000*2999/((2**32-600552-984)*24000)
        self.assertEqual(actual['depletion'], depletion)
        self.assertAlmostEqual(actual['lowerBound'], 0.01-math.sqrt(6000*math.log(20))/24000-depletion, places=14)
        self.assertEqual((actual['netWins'], actual['denominator'], actual['fights']), (240,24000,18672))

    def test_identical_outputs_require_null_absolute_counts(self):
        for change in [dict(baselineWins=0), dict(candidateWins=1000), dict(gains=1), dict(losses=1), dict(difference=0.01)]:
            with self.subTest(change=change), self.assertRaises(ValueError):
                result = copy.deepcopy(self.result)
                result['pairs'][3].update(change)
                close.verify_selector_endpoint(result)

    def test_identity_family_and_fight_mismatches_fail(self):
        for change in [dict(version=audit.VERSION), dict(denominator=3000), dict(fights=17904), dict(activeRestarts=4)]:
            with self.subTest(change=change), self.assertRaises(ValueError):
                close.verify_selector_endpoint(dict(self.result, **change))
        for change in [dict(restart=2), dict(identical=True), dict(candidateParty=self.result['pairs'][0]['baselineParty'])]:
            with self.subTest(change=change), self.assertRaises(ValueError):
                result = copy.deepcopy(self.result)
                result['pairs'][0].update(change)
                close.verify_selector_endpoint(result)
        with self.assertRaises(ValueError):
            close.verify_selector_endpoint(dict(self.result, pairs=self.result['pairs'][:-1]))

    def test_invalid_active_counts_fail(self):
        for change in [dict(baselineWins=None), dict(baselineWins=True), dict(candidateWins=1001), dict(gains=-1),
                       dict(losses=501), dict(difference=0.079), dict(gains=81), dict(gains=580,losses=500)]:
            with self.subTest(change=change), self.assertRaises(ValueError):
                result = copy.deepcopy(self.result)
                result['pairs'][0].update(change)
                close.verify_selector_endpoint(result)

    def test_entire_literal_batch_remains_reserved(self):
        words = list(range(32768))
        words[-1] = 100  # A literal within-batch duplicate, not a random draw.
        selected, reserved, collisions, duplicates = close.classify_reservation(audit, struct.pack('<32768i', *words), [0,1])
        self.assertEqual(selected, list(range(2,24986)))
        self.assertEqual(reserved, list(range(2,32767)))
        self.assertEqual((collisions,duplicates,len(reserved)-len(selected)), (2,1,7781))

    def test_undersized_fresh_prefix_fails_without_refill(self):
        entropy = struct.pack('<32768i', *range(32768))
        with self.assertRaisesRegex(ValueError, 'Entropy exhausted'):
            close.classify_reservation(audit, entropy, list(range(24983,32768)))


if __name__ == '__main__':
    unittest.main(verbosity=2)
