"""Saved-evidence diagnosis, selection perturbations and corrupt-input rejection."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

PATH = Path(__file__).with_name('adaptive-racing-stage-review.py')
SPEC = importlib.util.spec_from_file_location('adaptive_stage_review', PATH)
review = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(review)


class AdaptiveStageReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.evidence = review.Evidence(review.RUN, review.RUN_PIN)
        cls.result = cls.evidence.read('result.json')
        cls.saved = {j: (cls.evidence.read(f'study/pair-{j:02d}-candidate-adaptive.json'),
                        cls.evidence.read(f'study/pair-{j:02d}-candidate-plan.json')) for j in (1, 2, 3, 11, 12)}

    def analyze(self, j, report=None, plan=None):
        p, q = self.result['pairs'][j-1], self.result['pilot']['roots'][j-1]
        return review.adaptive(report if report is not None else self.saved[j][0],
            plan if plan is not None else self.saved[j][1],
            {p['baselineParty']: p['baselineWins'], p['candidateParty']: p['candidateWins'], review.BENCHMARK: q['benchmarkWins']},
            p['candidateParty'])

    def test_saved_positive_ties_with_unprotected_benchmark_are_explicit(self):
        for j in (1, 11):
            a = self.analyze(j)
            self.assertTrue(a['selection']['nonprimaryReferenceTie'])
            self.assertEqual('FrozenNomineeOrder', a['selection']['reason'])
            self.assertEqual(0, a['selection']['marginOverBenchmark'])

    def test_strict_narrow_lead_and_unknown_heldout_remain_separate(self):
        a = self.analyze(2)
        self.assertEqual(1, a['selection']['marginOverBestReference'])
        self.assertFalse(a['selection']['nonprimaryReferenceTie'])
        self.assertEqual(17, len(a['records']))
        self.assertEqual(16, sum(r['heldoutWins'] is None for r in a['records']))
        self.assertEqual((216, 312), (a['referenceFights'], a['challengerFights']))
        self.assertEqual(2, sum(r['nominated'] for r in a['records']))

    def test_every_saved_root_recounts_and_preserves_endpoint(self):
        for j in range(1, 13):
            p, q = self.result['pairs'][j-1], self.result['pilot']['roots'][j-1]
            heldout = {p['baselineParty']: p['baselineWins'], p['candidateParty']: p['candidateWins'], review.BENCHMARK: q['benchmarkWins']}
            a = review.baseline(self.evidence.read(f'study/pair-{j:02d}-baseline.json'), heldout, p['baselineParty'])
            b = review.adaptive(self.evidence.read(f'study/pair-{j:02d}-candidate-adaptive.json'),
                self.evidence.read(f'study/pair-{j:02d}-candidate-plan.json'), heldout, p['candidateParty'])
            self.assertEqual((43, 17), (len(a['records']), len(b['records'])))
            self.assertEqual(528, a['referenceFights']+a['challengerFights'])
            self.assertEqual(528, b['referenceFights']+b['challengerFights'])

    def test_partial_reordered_wrong_seed_and_nonterminal_outcomes_rejected(self):
        for kind in ('partial', 'order', 'seed', 'outcome', 'score'):
            x = copy.deepcopy(self.saved[3][0]); panel = x['evaluation']['panels'][0]
            if kind == 'partial': panel['observations'].pop()
            if kind == 'order': panel['observations'].reverse()
            if kind == 'seed': panel['observations'][0]['outcome']['seed'] += 1
            if kind == 'outcome': panel['observations'][0]['outcome']['outcome'] = 'Failed'
            if kind == 'score': panel['scores'][0]['wins'] += 1
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.analyze(3, report=x)

    def test_membership_pruning_and_common_score_tampering_rejected(self):
        for kind in ('members', 'survivors', 'beam', 'common', 'diversity'):
            x = copy.deepcopy(self.saved[1][0]); decision = x['evaluation']['decisions'][0]
            if kind == 'members': x['evaluation']['panels'][0]['freeze']['parties'].pop()
            if kind == 'survivors': decision['survivorIds'].reverse()
            if kind == 'beam': decision['beamIds'].reverse()
            if kind == 'common': decision['commonScores'][0]['wins'] += 1
            if kind == 'diversity': decision['diversityDistance'] += 1
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.analyze(1, report=x)

    def test_incomplete_mismatched_budget_nomination_and_output_rejected(self):
        for kind in ('status', 'budget', 'nominees', 'selected'):
            x = copy.deepcopy(self.saved[2][0]); e = x['evaluation']
            if kind == 'status': e['status'] = 'Running'
            if kind == 'budget': e['chargedEvaluations'] -= 1
            if kind == 'nominees': e['nominees'].reverse()
            if kind == 'selected': e['rawSelectedId'] = review.PRIMARY
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.analyze(2, report=x)

    def test_reused_seed_unresolved_parent_and_duplicate_recipe_rejected(self):
        plan = copy.deepcopy(self.saved[12][1]); plan['panels'][1]['seeds'][0] = plan['panels'][0]['seeds'][0]
        with self.assertRaisesRegex(ValueError, 'Reused'):
            self.analyze(12, plan=plan)
        for kind in ('parent', 'duplicate'):
            x = copy.deepcopy(self.saved[12][0]); b = x['batches'][0]
            if kind == 'parent': b['proposals'][0]['parents'] = ['missing']
            if kind == 'duplicate':
                b['proposals'][1]['party'] = copy.deepcopy(b['proposals'][0]['party'])
                b['candidates'][1] = copy.deepcopy(b['candidates'][0])
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.analyze(12, report=x)

    def test_baseline_missing_data_is_not_counted_as_a_loss(self):
        p = self.result['pairs'][0]
        x = self.evidence.read('study/pair-01-baseline.json')
        a = review.baseline(x, {p['baselineParty']: p['baselineWins']}, p['baselineParty'])
        self.assertEqual(43, sum(r['heldoutWins'] is None for r in a['records']))
        self.assertEqual(41, sum(r['selectionWins'] is None for r in a['records']))
        x['selection'][0]['cells'][0]['clears'][0] = 1
        with self.assertRaises(ValueError):
            review.baseline(x, {}, p['baselineParty'])

    def test_consumed_evidence_tampering_and_wrong_pin_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); raw = b'{"value":1}'
            (root/'row.json').write_bytes(raw)
            manifest = json.dumps({'row.json': review.sha(raw)}).encode()
            (root/'files.json').write_bytes(manifest)
            with self.assertRaisesRegex(ValueError, 'pin'):
                review.Evidence(root, '0'*64)
            evidence = review.Evidence(root, review.sha(manifest))
            self.assertEqual({'value': 1}, evidence.read('row.json'))
            (root/'row.json').write_bytes(b'{"value":2}')
            with self.assertRaises(ValueError): evidence.recheck()
            with self.assertRaises(ValueError): evidence.read('row.json')
            with self.assertRaises(ValueError): evidence.read('../row.json')

    def test_primary_positive_tie_and_ordered_nonprimary_tie(self):
        order = ['challenger', review.BENCHMARK, review.PRIMARY, next(x for x in review.REFERENCES if x not in (review.PRIMARY, review.BENCHMARK))]
        rows = {pid: dict(clears=[True, False], health=None) for pid in order}
        self.assertEqual(review.PRIMARY, review.select(rows, order))
        rows[review.PRIMARY]['clears'] = [False, False]
        self.assertEqual('challenger', review.select(rows, order))

    def test_zero_win_subsets_require_actual_health_and_draws_are_nonwins(self):
        order = sorted(review.REFERENCES)
        rows = {pid: dict(clears=[False, False], health=None) for pid in order}
        self.assertIsNone(review.select(rows, order, [0]))
        for i, pid in enumerate(order): rows[pid]['health'] = [10-i, 20-i]
        self.assertEqual(order[-1], review.select(rows, order, [0]))
        outcome = dict(outcome='Draw', guardianHealth=10., survival=20., durationSeconds=30.)
        counted = review.score('x', [outcome])
        self.assertEqual((0, 1, sys_float_max()), (counted['wins'], counted['draws'], counted['fitness']['victoryDuration']))

    def test_summary_counts_root_recipe_instances_and_not_missing_outcomes(self):
        a = self.analyze(2)
        summary = review.summarize([a, copy.deepcopy(a)])
        self.assertEqual((34, 17, 2, 32), (summary['generated'], summary['distinctRecipes'], summary['heldoutMeasured'], summary['heldoutUnknown']))


def sys_float_max():
    return float.fromhex('0x1.fffffffffffffp+1023')


if __name__ == '__main__':
    unittest.main(verbosity=2)
