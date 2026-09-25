"""Archived trajectory reconstruction and rejection of misleading diagnoses."""
import copy
import importlib.util
from pathlib import Path
import unittest

PATH = Path(__file__).with_name('proposal-affinity-stage-review.py')
SPEC = importlib.util.spec_from_file_location('proposal_stage_review', PATH)
review = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(review)


class ProposalStageReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scientific = review.Evidence(review.RUN, review.RUN_PIN)
        cls.admission = review.Evidence(review.ADMISSION, review.ADMISSION_PIN)
        cls.context = cls.scientific.read('source/context.json')
        cls.design = cls.scientific.read('source/plan.json')
        cls.pair = cls.scientific.read('study/pair-05.json')
        cls.plan = cls.scientific.read('search/root-05/candidate/racing/plan.json')
        cls.endpoint = cls.scientific.read('result.json')['roots'][4]
        cls.heldout = {cls.endpoint[r+'Party']: cls.endpoint[r+'Wins'] for r in ('control', 'candidate', 'benchmark')}

    def arm(self, report=None, plan=None):
        return review.arm_review(report or self.pair['candidate'], plan or self.plan, self.context,
            self.design['candidate'], self.heldout, self.endpoint['candidateParty'])

    def test_all_24_trajectories_and_nine_changes_recount(self):
        result = review.analyze(self.scientific, self.admission)
        s = result['summary']
        self.assertEqual((204, 9, 7, 1), tuple(s[k] for k in ('acceptedPositions', 'changedPositions', 'changedRoots', 'differingOutputs')))
        self.assertEqual((0, 0), (s['controlRejected'], s['candidateRejected']))
        self.assertEqual((45, 48), (s['controlChangedInitialWins'], s['candidateChangedInitialWins']))
        self.assertEqual((3, 0), (s['controlChangedNominations'], s['candidateChangedNominations']))
        self.assertTrue(all(x['alignedAttempt'] for x in result['changedPositions']))
        self.assertTrue(all(x[r]['heldoutWins'] is None and x[r]['heldoutSamples'] is None
                            for x in result['changedPositions'] for r in ('control', 'candidate')))
        coverage = result['coverage']
        self.assertEqual((3, 2, 3, [8]), (coverage['routeCount'], coverage['uniqueEssencePairs'],
            coverage['distinctProtectedEssences'], coverage['activeOwners']))
        trace = result['root5']
        self.assertEqual(trace['control']['selectedRecipePath']['edits'], trace['candidate']['selectedRecipePath']['edits'])
        self.assertEqual([dict(owner=2, removed=['essence.illusion_fox'], added=['essence.frost_imp'])],
            trace['candidate']['selectedRecipePath']['edits'])
        self.assertFalse(trace['control']['selectedRecipePath']['nominated'])
        self.assertTrue(trace['candidate']['selectedRecipePath']['nominated'])
        self.assertEqual(['UniqueMaximum']*2, [trace[r]['selection']['reason'] for r in ('control', 'candidate')])

    def test_unmeasured_is_unknown_even_for_a_nominated_proposal(self):
        rows = self.arm()['records']
        self.assertEqual(16, sum(r['heldoutWins'] is None for r in rows))
        self.assertTrue(any(r['nominated'] and r['heldoutWins'] is None for r in rows))

    def test_proposal_and_adaptive_versions_are_separate(self):
        self.assertEqual('tower-adaptive-beam-racing-v1', self.plan['racing']['version'])
        self.assertEqual(review.VERSION, self.pair['candidate']['version'])
        altered = copy.deepcopy(self.pair['candidate']); altered['evaluation']['version'] = 'unknown'
        with self.assertRaisesRegex(ValueError, 'contract'):
            self.arm(report=altered)

    def test_panel_scores_membership_and_nomination_tampering_fail(self):
        for kind in ('score', 'members', 'nominees', 'common', 'seed', 'recipe'):
            altered = copy.deepcopy(self.pair['candidate'])
            e = altered['evaluation']; p = e['panels'][0]
            if kind == 'score': p['scores'][0]['wins'] += 1
            if kind == 'members': p['observations'].pop()
            if kind == 'nominees': e['nominees'].reverse()
            if kind == 'common': e['decisions'][1]['commonScores'][0]['wins'] += 1
            if kind == 'seed': p['observations'][0]['outcome']['seed'] += 1
            if kind == 'recipe': p['freeze']['parties'][0]['builds']['1'][0] = 'invented'
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.arm(report=altered)

    def test_reference_preference_is_bound_to_saved_scope(self):
        altered = copy.deepcopy(self.plan)
        altered['racing']['scope']['stages']['selectionPrimaryReferenceId'] = self.context['benchmarkReferenceId']
        with self.assertRaisesRegex(ValueError, 'references'):
            self.arm(plan=altered)

    def test_shared_results_must_agree_despite_different_request_identity(self):
        self.assertGreater(review.shared_outcomes(self.pair['control'], self.pair['candidate']), 400)
        altered = copy.deepcopy(self.pair['candidate'])
        row = altered['evaluation']['panels'][0]['observations'][0]['outcome']
        row['guardianHealth'] += 1
        with self.assertRaisesRegex(ValueError, 'Shared recipe outcomes disagree'):
            review.shared_outcomes(self.pair['control'], altered)

    def test_rejected_attempt_does_not_silently_align_accepted_slots(self):
        a = copy.deepcopy(self.pair['control']['batches'][0]['proposals'])
        b = copy.deepcopy(a)
        rejected = copy.deepcopy(b[0]); rejected['rejection'] = 'duplicate'
        b.insert(0, rejected)
        for i, p in enumerate(b, 1): p['attempt'] = i
        positions = review.accepted_positions(a, b)
        self.assertEqual(9, len(positions))
        self.assertTrue(all(not p[3] for p in positions))
        with self.assertRaisesRegex(ValueError, 'Unpaired'):
            review.accepted_positions(a, b[2:])

    def test_route_deduplication_and_inactive_affinities(self):
        party = dict(id='parent', builds={'1': ['a', 'b'], '2': ['a', 'c']})
        affinities = [dict(id='one', producerEssenceId='a', modifierEssenceId='b'),
                      dict(id='two', producerEssenceId='a', modifierEssenceId='b'),
                      dict(id='inactive', producerEssenceId='c', modifierEssenceId='b')]
        a = review.active_affinities(party, affinities, ['one', 'two', 'inactive'])
        self.assertEqual((2, 1, 2, [1]), (a['routeCount'], a['uniqueEssencePairs'], a['distinctProtectedEssences'], a['activeOwners']))
        with self.assertRaisesRegex(ValueError, 'affinity'):
            review.active_affinities(party, affinities, ['missing'])


if __name__ == '__main__':
    unittest.main(verbosity=2)
