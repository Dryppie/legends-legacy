"""Review integrity regressions using the retained literal (non-combat) fixture."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('review', Path(__file__).with_name('frozen-pool-recognition-review.py'))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


class ReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.result = json.loads((ROOT/'TestResults/tower-recognition-owned-fixture-repaired-20260923/result/result.json').read_text())
        cls.plan = json.loads((ROOT/'Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json').read_text())
        cls.stage = json.loads((ROOT/'TestResults/adaptive-racing-stage-review-20260923/review.json').read_text())

    def test_literal_review_keeps_root_instances_and_population_weights(self):
        r = m.review(self.result,self.plan,self.stage)
        self.assertEqual(r['allMeasured']['measured'],72)
        self.assertEqual(r['discardedCandidates']['measured'],48)
        self.assertEqual(r['historicallySelectedChallengers']['measured'],6)
        self.assertEqual(r['unmeasuredCount'],132)
        self.assertEqual(r['newFights'],0)
        self.assertEqual(len({(x['root'],x['candidateId']) for x in r['measured']}),72)
        self.assertAlmostEqual(r['roots'][0]['estimatedCandidateMeanGain'],0.25275735294117646)
        self.assertNotIn('qualifiers',r)

    def reject_result(self, mutate):
        result = copy.deepcopy(self.result)
        mutate(result)
        with self.assertRaises(ValueError):
            m.review(result,self.plan,self.stage)

    def test_incomplete_study_rejected(self):
        self.reject_result(lambda r:r.update(executionStatus='Incomplete'))

    def test_promotion_rejected(self):
        self.reject_result(lambda r:r.update(policyDefaultsChanged=True))

    def test_cross_root_duplicate_rejected(self):
        self.reject_result(lambda r:r['rates'][9].update(root=1))

    def test_missing_contrast_rejected(self):
        self.reject_result(lambda r:r['contrasts'].pop())

    def test_changed_gain_rejected(self):
        self.reject_result(lambda r:r['contrasts'][0].update(observedGain=0))

    def test_replaced_lower_weight_rejected(self):
        self.reject_result(lambda r:r['strata'][2].update(inclusionWeight=1))

    def test_nan_population_rejected(self):
        self.reject_result(lambda r:r['populations'][0].update(estimatedCandidateMeanGain=float('nan')))

    def test_invented_unsampled_outcome_rejected(self):
        self.reject_result(lambda r:r['unmeasured'][0].update(independentOutcome=0))

    def test_wrong_historical_join_rejected(self):
        stage = copy.deepcopy(self.stage)
        stage['pairs'][0]['adaptive']['records'][0]['selected'] = False
        with self.assertRaises(ValueError):
            m.review(self.result,self.plan,stage)

    def test_observed_sign_is_not_interval_sign(self):
        r = m.aggregate([dict(observedGain=.01,lower=-.1,upper=.1),dict(observedGain=-.2,lower=-.3,upper=-.1)])
        self.assertEqual(r['observedAboveBenchmark'],1)
        self.assertEqual(r['intervalAboveZero'],0)
        self.assertEqual(r['intervalBelowZero'],1)

    def test_consumed_evidence_cannot_change(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            raw = b'{"value": 1}'
            (root/'result.json').write_bytes(raw)
            manifest = json.dumps({'result.json':m.sha(raw)}).encode()
            (root/'files.json').write_bytes(manifest)
            e = m.Evidence(root,m.sha(manifest))
            self.assertEqual(e.read('result.json'),{'value':1})
            (root/'result.json').write_text('{}')
            with self.assertRaises(ValueError):
                e.recheck()
            with self.assertRaises(ValueError):
                e.read('result.json')
            with self.assertRaises(ValueError):
                m.Evidence(root,'0'*64)


if __name__ == '__main__':
    unittest.main()
