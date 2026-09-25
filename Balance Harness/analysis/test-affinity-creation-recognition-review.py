"""Review integrity regressions using the retained literal (non-combat) fixture."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('review', Path(__file__).with_name('affinity-creation-recognition-review.py'))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


class ReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        fixture = ROOT/'TestResults/tower-affinity-recognition-owned-fixture-20260924/result'
        evidence = m.Evidence(fixture,'6c03bfe9eda04a2ce54cf6ee9712c1f107df1afd4068edb371bada5cbf3b150b')
        evidence.read('result.json')
        cls.result = json.loads((ROOT/'TestResults/tower-affinity-recognition-owned-fixture-20260924/result/result.json').read_text())
        cls.plan = json.loads((ROOT/'Balance Harness/Tower-Affinity-Creation-Recognition-Plan.json').read_text())
        cls.catalogue = json.loads((ROOT/'TestResults/affinity-creation-recognition-catalogue-20260924/catalogue.json').read_text())

    def test_literal_review_keeps_root_instances_and_population_weights(self):
        r = m.review(self.result,self.plan,self.catalogue)
        self.assertEqual(r['allMeasured']['measured'],72)
        self.assertEqual(r['discardedCandidates']['measured'],48)
        self.assertEqual(r['distinctGeneratedRecipes'],57)
        self.assertEqual(r['unmeasuredCount'],132)
        self.assertEqual(r['newFights'],0)
        self.assertEqual(len({(x['root'],x['candidateId']) for x in r['measured']}),72)
        self.assertAlmostEqual(r['roots'][0]['estimatedCandidateMeanGain'],0.25275735294117646)
        self.assertNotIn('qualifiers',r)

    def reject_result(self, mutate):
        result = copy.deepcopy(self.result)
        mutate(result)
        with self.assertRaises(ValueError):
            m.review(result,self.plan,self.catalogue)

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

    def test_wrong_catalogue_join_rejected(self):
        catalogue = copy.deepcopy(self.catalogue)
        catalogue['roots'][0]['teams'][3]['stratum'] = 'lower'
        with self.assertRaises(ValueError):
            m.review(self.result,self.plan,catalogue)

    def test_missing_catalogue_candidate_rejected(self):
        catalogue = copy.deepcopy(self.catalogue)
        catalogue['roots'][0]['teams'].pop()
        with self.assertRaises(ValueError):
            m.review(self.result,self.plan,catalogue)

    def test_invalid_discordance_counts_rejected(self):
        self.reject_result(lambda r:r['contrasts'][0].update(gains=326, losses=256))

    def test_old_study_version_rejected(self):
        self.reject_result(lambda r:r.update(version='tower-frozen-pool-recognition-v1'))

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
