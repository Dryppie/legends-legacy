"""Saved selector boundaries, paired diagnosis, missing evidence and tamper tests."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('tie_review', Path(__file__).with_name('benchmark-tie-stage-review.py'))
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)


class BenchmarkTieReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scientific = a.Evidence(a.RUN, a.RUN_PIN)
        cls.admission = a.Evidence(a.ADMISSION, a.ADMISSION_PIN)
        cls.context = cls.scientific.read('source/context.json')
        cls.design = cls.scientific.read('source/plan.json')
        cls.affinities = cls.admission.read('preview-batches.json')['damageSourceAffinities']['affinities']
        cls.pair = cls.scientific.read('study/pair-01.json')
        cls.plan = cls.scientific.read('search/root-01/candidate/racing/plan.json')
        cls.endpoint = cls.scientific.read('result.json')['roots'][0]
        cls.heldout = {cls.endpoint[r+'Party']: cls.endpoint[r+'Wins'] for r in ('control', 'candidate', 'benchmark')}
        cls.review = a.analyze(cls.scientific, cls.admission)

    def arm(self, report=None, plan=None):
        return a.arm_review(report or self.pair['candidate'], plan or self.plan, self.context, self.design['candidate'],
                            self.affinities, self.heldout, self.endpoint['candidateParty'], 'candidate')

    def test_all_roots_both_selectors_and_training_equality_are_recounted(self):
        r = self.review
        self.assertEqual('AbandonThisConfiguration', r['originalDecision'])
        self.assertEqual(17792, r['sourceFights'])
        self.assertEqual(list(range(1, 13)), [p['root'] for p in r['pairs']])
        self.assertEqual((204, 6336, 2, 25), tuple(r['summary'][k] for k in
                         ('acceptedPositions', 'sharedTrainingObservations', 'differingOutputs', 'methodNetWins')))
        for role in ('control', 'candidate'):
            self.assertEqual(12*528, sum(p[role]['referenceFights']+p[role]['challengerFights'] for p in r['pairs']))

    def test_only_two_observed_positive_ties_change_the_output(self):
        for p in self.review['pairs'][:2]:
            self.assertEqual('PrimaryPositiveTie', p['control']['selection']['reason'])
            self.assertEqual('BenchmarkPositiveTie', p['candidate']['selection']['reason'])
        self.assertTrue(all(p['candidate']['selection']['reason'] == 'UniqueMaximum' for p in self.review['pairs'][2:]))

    def test_root_five_literal_edit_and_whole_selection_path(self):
        r = self.review['pairs'][4]['candidate']
        t, p = r['selectionTrace'], r['selectionTrace']['selectedProposal']
        self.assertEqual((1, 9, 10), (p['wave'], p['position'], p['attempt']))
        self.assertEqual([dict(owner=5, removed=['essence.enchanted_fairy'], added=['essence.viper'])], p['edits'])
        self.assertEqual(3, len(p['affinityCreation']['newlyActivatedAffinityIds']))
        self.assertEqual([(6, 7), (5, 5), (8, 8), (6, 4), (33, 32)],
                         [(s['wins'], s['benchmarkWins']) for s in r['outputStages'].values()])
        self.assertEqual((7, 6), tuple(r['outputStages']['selection'][k] for k in ('gainedWins', 'lostWins')))
        self.assertEqual((1, 7, -34), (t['marginOverBenchmark'], t['leaveOneOutChanges'], t['heldoutNetWins']))

    def test_root_eight_strict_reference_lead_is_not_a_tie_override(self):
        r = self.review['pairs'][7]['candidate']
        t = r['selectionTrace']
        self.assertEqual(a.kernel.PRIMARY, t['selected'])
        self.assertIsNone(t['selectedProposal'])
        self.assertEqual(('UniqueMaximum', 3, 0, -18), (t['reason'], t['marginOverBenchmark'], t['leaveOneOutChanges'], t['heldoutNetWins']))
        self.assertEqual((10, 7), tuple(r['outputStages']['selection'][k] for k in ('gainedWins', 'lostWins')))
        self.assertTrue(t['halvesDisagree'])

    def test_all_root_loss_arithmetic_and_negative_stable_counterexample(self):
        rows = self.review['benchmarkContributions']['candidate']
        self.assertEqual(-75, sum(r['netWins'] for r in rows.values()))
        self.assertEqual((-57, -18), (rows['novel']['netWins'], rows['other-reference']['netWins']))
        self.assertEqual(list(range(1, 13)), sorted(n for r in rows.values() for n in r['roots']))
        t = self.review['pairs'][11]['candidate']['selectionTrace']
        self.assertEqual((3, False, 0, -6), (t['marginOverBenchmark'], t['halvesDisagree'], t['leaveOneOutChanges'], t['heldoutNetWins']))

    def test_same_recipe_from_another_root_does_not_fill_missing_outcomes(self):
        known = self.review['pairs'][5]['candidate']['selectionTrace']
        unknown = next(r for r in self.review['pairs'][0]['candidate']['records'] if r['id'] == known['selected'])
        self.assertEqual(187, known['heldoutWins'])
        self.assertTrue(unknown['nominated'])
        self.assertIsNone(unknown['heldoutWins'])
        self.assertIsNone(self.review['pairs'][0]['candidate']['nomineeHeldout'][unknown['id']])
        self.assertEqual(199, self.review['armSummaries']['candidate']['heldoutUnknown'])

    def test_selector_identity_and_version_cannot_be_interchanged(self):
        for kind in ('plan-policy', 'report-policy', 'plan-version', 'report-version'):
            p, r = copy.deepcopy(self.plan), copy.deepcopy(self.pair['candidate'])
            target = p if kind.startswith('plan') else r
            if kind.endswith('policy'):
                target.pop('selectionPolicyVersion')
            else:
                target['version'] = 'tower-proposal-racing-v3'
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.arm(r, p)

    def test_candidate_summary_rejects_saved_legacy_output_on_benchmark_tie(self):
        r = copy.deepcopy(self.pair['candidate'])
        r['evaluation']['rawSelectedId'] = a.kernel.PRIMARY
        with self.assertRaises(ValueError):
            self.arm(r)

    def test_full_trajectory_equality_rejects_generation_and_training_changes(self):
        for kind in ('proposal', 'score', 'nominee', 'observation'):
            r = copy.deepcopy(self.pair['candidate'])
            if kind == 'proposal':
                r['batches'][0]['proposals'][0]['scheduledOwners'] = [10]
            elif kind == 'score':
                r['evaluation']['panels'][0]['scores'][0]['wins'] += 1
            elif kind == 'nominee':
                r['evaluation']['nominees'].reverse()
            else:
                r['evaluation']['panels'][0]['observations'][0]['outcome']['guardianHealth'] += 1
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                a.audit.selector_trajectories(self.pair['control'], r)

    def test_paired_contrasts_require_matching_seed_order_and_counts(self):
        p = self.scientific.read('study/pair-05.json')['candidate']['evaluation']['panels'][-1]
        pid = self.review['pairs'][4]['candidate']['selection']['selected']
        for kind in ('seed', 'contrast'):
            x = copy.deepcopy(p)
            if kind == 'seed':
                next(o for o in x['observations'] if o['request']['partyId'] == pid)['request']['seed'] += 1
            else:
                next(c for c in x['contrasts'] if c['partyId'] == pid and c['referenceId'] == a.kernel.BENCHMARK)['lostWins'] += 1
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                a.stage_contrast(x, pid)

    def test_positive_tie_opt_in_preserves_nonbenchmark_and_zero_win_fallbacks(self):
        order = ['challenger', a.kernel.BENCHMARK, a.kernel.PRIMARY, next(p for p in a.kernel.REFERENCES
                  if p not in (a.kernel.BENCHMARK, a.kernel.PRIMARY))]
        rows = {p: dict(clears=[True, False], health=[10, 10]) for p in order}
        self.assertEqual(a.kernel.BENCHMARK, a.kernel.select(rows, order, benchmark_ties=True))
        self.assertEqual(a.kernel.PRIMARY, a.kernel.select(rows, order))
        rows[a.kernel.BENCHMARK]['clears'] = [False, False]
        self.assertEqual(a.kernel.PRIMARY, a.kernel.select(rows, order, benchmark_ties=True))
        for r in rows.values():
            r['clears'] = [False, False]
        rows['challenger']['health'] = [1, 1]
        self.assertEqual('challenger', a.kernel.select(rows, order, benchmark_ties=True))

    def test_benchmark_ties_apply_to_subset_diagnostics_too(self):
        p = self.review['pairs'][4]
        self.assertEqual(0, p['control']['selection']['leaveOneOutChanges'])
        self.assertEqual(7, p['candidate']['selection']['leaveOneOutChanges'])
        self.assertEqual(p['control']['selection']['selected'], p['candidate']['selection']['selected'])

    def test_consumed_bytes_and_external_pin_are_required(self):
        with tempfile.TemporaryDirectory(prefix='tie-review-test-') as folder:
            root = Path(folder)
            raw = b'{"complete":true}'
            (root/'row.json').write_bytes(raw)
            (root/'files.json').write_text(a.json.dumps({'row.json': a.sha(raw)}))
            e = a.Evidence(root, a.sha((root/'files.json').read_bytes()))
            self.assertTrue(e.read('row.json')['complete'])
            (root/'row.json').write_text('{}')
            with self.assertRaises(ValueError):
                e.recheck()
            with self.assertRaises(ValueError):
                a.Evidence(root, '0'*64)
            with self.assertRaises(ValueError):
                e.read('../row.json')


if __name__ == '__main__':
    unittest.main(verbosity=2)
