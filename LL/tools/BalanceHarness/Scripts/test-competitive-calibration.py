"""Numerical and completeness checks for the partitioned campaign assessor."""
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('calibration', Path(__file__).with_name('calibrate-tower-search-portfolio.py'))
calibration = importlib.util.module_from_spec(spec); spec.loader.exec_module(calibration)
spec2 = importlib.util.spec_from_file_location('expanded', Path(__file__).with_name('tower-calibration-assessment.py'))
expanded = importlib.util.module_from_spec(spec2); spec2.loader.exec_module(expanded)


def partition(ids, wins, status='Pass'):
    return {'issues': [], 'assessment': status, 'cells': [
        {'id': i, 'valid': 250, 'wins': wins, 'draws': 0, 'issues': [], 'artifactHash': 'a'*64} for i in ids]}


class GlobalFamilyTests(unittest.TestCase):
    def test_partition_viability_cannot_override_global_viability(self):
        ids = [str(i) for i in range(1500)]
        report = calibration.global_assessment(ids, [partition(ids[:1], 63), partition(ids[1:], 0, 'Fail')])
        self.assertEqual('Pass', report['assessment'])
        self.assertAlmostEqual(4.149410, report['z'], places=5)
        self.assertTrue(all(c['upper'] <= .5 for c in report['cells']))
        self.assertGreaterEqual(report['cells'][0]['lower'], .1)

    def test_family_size_is_global_and_increases_uncertainty(self):
        one = calibration.global_assessment(['a'], [partition(['a'], 90)])
        ids = [str(i) for i in range(1500)]
        many = calibration.global_assessment(ids, [partition(ids, 90)])
        self.assertGreater(many['cells'][0]['upper'], one['cells'][0]['upper'])
        self.assertEqual(1500, many['familySize'])

    def test_raw_ceiling_breach_always_fails(self):
        report = calibration.global_assessment(['a', 'b'], [partition(['a'], 126), partition(['b'], 63)])
        self.assertEqual('Fail', report['assessment'])

    def test_all_weak_parties_fail_even_if_upper_ceiling_passes(self):
        self.assertEqual('Fail', calibration.global_assessment(['a'], [partition(['a'], 0)])['assessment'])

    def test_duplicates_missing_extra_and_invalid_evidence_rejected(self):
        for ids, records in [(['a', 'b'], [partition(['a'], 63)]), (['a'], [partition(['a', 'a'], 63)]),
                             (['a'], [partition(['b'], 63)]), (['a', 'a'], [partition(['a', 'a'], 63)]),
                             (['a'], [partition(['a'], 63, 'Invalid')])]:
            with self.subTest(ids=ids, records=records), self.assertRaises(ValueError):
                calibration.global_assessment(ids, records)

    def test_partial_samples_rejected(self):
        p = partition(['a'], 63); p['cells'][0]['valid'] = 249
        with self.assertRaises(ValueError): calibration.global_assessment(['a'], [p])

    def test_expanded_policy_preserves_existing_arithmetic(self):
        ids = [str(i) for i in range(1500)]
        records = [partition(ids[:1], 63), partition(ids[1:], 0, 'Fail')]
        protocol = {'campaignAssessmentPolicy': expanded.VERSION, 'confirmationSamples': 250,
                    'familySize': len(ids), 'maximumActualBattles': 420000}
        original = calibration.global_assessment(ids, records)
        report = expanded.assess(ids, records, protocol)
        for key in ('assessment', 'familySize', 'z', 'cells', 'upperSupported', 'viabilitySupported'):
            self.assertEqual(original[key], report[key])

    def test_expanded_family_uses_all_2400_recipes(self):
        ids = [str(i) for i in range(2400)]
        protocol = {'campaignAssessmentPolicy': expanded.VERSION, 'confirmationSamples': 250,
                    'familySize': len(ids), 'maximumActualBattles': 700000}
        report = expanded.assess(ids, [partition(ids[:350], 63), partition(ids[350:], 0, 'Fail')], protocol)
        self.assertEqual('Pass', report['assessment'])
        self.assertEqual(2400, len(report['cells']))
        self.assertGreater(report['z'], 4.149410)
        with self.assertRaises(ValueError): expanded.assess(ids, [partition(ids[:350], 63)], protocol)

    def test_expanded_campaign_requires_explicit_valid_caps_and_policy(self):
        ids = [str(i) for i in range(2400)]
        protocol = {'campaignAssessmentPolicy': expanded.VERSION, 'confirmationSamples': 250,
                    'familySize': len(ids), 'maximumActualBattles': 700000}
        for change in ({'maximumActualBattles': 750001}, {'maximumActualBattles': 599999},
                       {'campaignAssessmentPolicy': 'unknown'}, {'familySize': 2399}, {'confirmationSamples': 249}):
            with self.subTest(change=change), self.assertRaises(ValueError):
                expanded.assess(ids, [partition(ids, 63)], {**protocol, **change})


if __name__ == '__main__': unittest.main()
