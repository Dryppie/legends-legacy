"""Coverage, fixed-sample and failure checks for the separate precision decision."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('precision', Path(__file__).with_name('confirm-tower-calibration-precision.py'))
precision = importlib.util.module_from_spec(spec); spec.loader.exec_module(precision)


def original(size=2438):
    rows = [{'id': f'party-{i:04}', 'valid': 250, 'wins': 0, 'draws': 0, 'artifactHash': 'a'*64} for i in range(size)]
    rows[0]['wins'] = 119; rows[1]['wins'] = 94; rows[2]['wins'] = 90
    return rows


def blocks(ids, wins=470):
    return [{'issues': [], 'assessment': 'Inconclusive', 'cells': [
        {'id': cid, 'valid': 1000, 'wins': wins if j == 0 else 370, 'draws': 0, 'issues': [],
         'artifactHash': format((i+1)*100+j, '064x')} for j, cid in enumerate(ids)]} for i in range(10)]


class PrecisionTests(unittest.TestCase):
    def test_selects_every_unresolved_team_under_stricter_original_family(self):
        self.assertEqual(['party-0000', 'party-0001'], precision.select(original()))

    def test_combined_pass_keeps_all_recipes_and_does_not_pool_old_samples(self):
        old = original(); ids = precision.select(old)
        result = precision.assess(old, ids, blocks(ids))
        self.assertEqual('Pass', result['assessment'])
        self.assertEqual(2438, len(result['cells']))
        self.assertEqual(20000, result['actualFreshCombats'])
        self.assertEqual((4700, 10000, 'fresh'), tuple(result['cells'][0][k] for k in ('wins', 'valid', 'sampleSource')))
        self.assertEqual((90, 250, 'original'), tuple(result['cells'][2][k] for k in ('wins', 'valid', 'sampleSource')))
        old_intervals, _ = precision.intervals(old, .025)
        self.assertEqual(old_intervals[2]['upper'], result['cells'][2]['upper'])
        self.assertGreater(result['originalZ'], result['freshZ'])

    def test_observed_breach_fails_instead_of_averaging_teams(self):
        old = original(); ids = precision.select(old)
        self.assertEqual('Fail', precision.assess(old, ids, blocks(ids, 501))['assessment'])

    def test_rate_below_ceiling_without_interval_support_is_inconclusive(self):
        old = original(); ids = precision.select(old)
        self.assertEqual('Inconclusive', precision.assess(old, ids, blocks(ids, 495))['assessment'])

    def test_missing_extra_duplicate_invalid_partial_and_repeated_blocks_rejected(self):
        old = original(); ids = precision.select(old); valid = blocks(ids)
        cases = [valid[:-1], valid + [valid[0]]]
        for field, value in [('issues', ['bad']), ('assessment', 'Invalid')]:
            bad = copy.deepcopy(valid); bad[0][field] = value; cases.append(bad)
        for field, value in [('valid', 999), ('wins', 1001), ('draws', 1001), ('valid', 1000.0), ('issues', ['bad'])]:
            bad = copy.deepcopy(valid); bad[0]['cells'][0][field] = value; cases.append(bad)
        bad = copy.deepcopy(valid); bad[0]['cells'][1] = copy.deepcopy(bad[0]['cells'][0]); cases.append(bad)
        bad = copy.deepcopy(valid); bad[1] = copy.deepcopy(bad[0]); cases.append(bad)
        for bad in cases:
            with self.subTest(bad=bad[0]), self.assertRaises(ValueError): precision.assess(old, ids, bad)

    def test_selection_cannot_drop_an_unresolved_team(self):
        old = original()
        with self.assertRaises(ValueError): precision.assess(old, ['party-0000'], blocks(['party-0000']))

    def test_original_breach_cannot_be_erased_by_fresh_confirmation(self):
        old = original(); old[0]['wins'] = 126
        with self.assertRaises(ValueError): precision.select(old)

    def test_large_unresolved_family_requires_different_explicit_campaign(self):
        old = original()
        for row in old[:9]: row['wins'] = 119
        with self.assertRaises(ValueError): precision.select(old)

    def test_viability_also_requires_support(self):
        old = original(3)
        for row in old: row['wins'] = 0
        ids = precision.select(old)
        self.assertEqual(['party-0000'], ids)
        self.assertEqual('Fail', precision.assess(old, ids, blocks(ids, 0))['assessment'])

    def test_recorded_decision_reconstructs_and_rejects_changed_partition(self):
        old = original(); ids = precision.select(old); reports = blocks(ids)
        with tempfile.TemporaryDirectory() as folder:
            out = Path(folder); plan = {'sourceAssessmentSha256': 'b'*64, 'selectedIds': ids}
            precision.cal.save(out / 'protocol.json', plan)
            hashes = {}
            for i, row in enumerate(reports):
                path = out / 'assessments' / str(i) / 'assessment.json'
                precision.cal.save(path, row); hashes[str(i)] = precision.cal.sha(path)
            result = precision.assess(old, ids, reports)
            result.update(protocolSha256=precision.cal.sha(out / 'protocol.json'),
                sourceAssessmentSha256=plan['sourceAssessmentSha256'], partitionReportHashes=hashes)
            precision.cal.save(out / 'assessment.json', result)
            with patch.object(precision, 'check', return_value=(plan, {'cells': old})):
                self.assertEqual('Pass', precision.verified(out)[1]['assessment'])
                path = out / 'assessments/0/assessment.json'
                path.write_text(path.read_text() + ' ', encoding='utf-8')
                with self.assertRaisesRegex(ValueError, 'partition changed'): precision.verified(out)

    def test_recorded_decision_cannot_relabel_inconclusive_as_pass(self):
        old = original(); ids = precision.select(old); reports = blocks(ids, 495)
        with tempfile.TemporaryDirectory() as folder:
            out = Path(folder); plan = {'sourceAssessmentSha256': 'b'*64, 'selectedIds': ids}
            precision.cal.save(out / 'protocol.json', plan); hashes = {}
            for i, row in enumerate(reports):
                path = out / 'assessments' / str(i) / 'assessment.json'
                precision.cal.save(path, row); hashes[str(i)] = precision.cal.sha(path)
            result = precision.assess(old, ids, reports)
            self.assertEqual('Inconclusive', result['assessment'])
            result.update(assessment='Pass', protocolSha256=precision.cal.sha(out / 'protocol.json'),
                sourceAssessmentSha256=plan['sourceAssessmentSha256'], partitionReportHashes=hashes)
            precision.cal.save(out / 'assessment.json', result)
            with patch.object(precision, 'check', return_value=(plan, {'cells': old})):
                with self.assertRaisesRegex(ValueError, 'arithmetic changed'): precision.verified(out)


if __name__ == '__main__': unittest.main()
