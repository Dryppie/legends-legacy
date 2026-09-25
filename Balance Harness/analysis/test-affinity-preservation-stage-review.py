"""Saved literal fixtures and corruption tests; never executes combat."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('preservation_stage_review', Path(__file__).with_name('affinity-preservation-stage-review.py'))
review = importlib.util.module_from_spec(spec)
spec.loader.exec_module(review)


class ReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = review.ROOT/'TestResults/tower-proposal-owned-fixture-affinity-preservation-20260924/result'
        cls.evidence = review.Evidence(root, 'e8025ce3cbec2339f4c89d5b951c6fb2b2b73fb283a123fc6798e3eb1e4752f0')
        cls.context = cls.evidence.read('source/context.json')
        cls.design = cls.evidence.read('source/plan.json')
        cls.result = cls.evidence.read('result.json')
        cls.freeze = cls.evidence.read('study/freeze.json')
        cls.pairs = [cls.evidence.read(f'study/pair-{n:02d}.json') for n in (1, 2)]
        cls.plans = [{role: cls.evidence.read(f'search/root-{n:02d}/{role}/racing/plan.json')
                      for role in ('control', 'candidate')} for n in (1, 2)]
        # Two authored routes in the literal fixture: shared added endpoint e10.
        cls.affinities = [dict(id='92514df878f5120dc5efdd36378f58d43d011bf32af21d0ebf97cb354f6f04ad', producerEssenceId='e01', modifierEssenceId='e10'),
                          dict(id='a17b93f57d6599f3fa0a4ee2df42d12f21558df2ce00b7cd93ca5f3650859859', producerEssenceId='e00', modifierEssenceId='e10')]

    @classmethod
    def tearDownClass(cls):
        cls.evidence.recheck()

    def checked(self, role='candidate', root=0, change=None, plan_change=None, heldout=None):
        report = copy.deepcopy(self.pairs[root][role])
        plan = copy.deepcopy(self.plans[root][role])
        if change:
            change(report)
        if plan_change:
            plan_change(plan)
        endpoint, family = self.result['roots'][root], self.freeze['families'][root]
        values = review.heldout_map(endpoint, family) if heldout is None else heldout
        return review.arm_review(report, plan, self.context, self.design[role], self.affinities,
            values, family['seeds'], endpoint[role+'Party'], role)

    def test_both_versions_and_saved_pass_fallback(self):
        review.audit.validate_policies(self.design)
        for role in ('control', 'candidate'):
            for root in (0, 1):
                actual = self.checked(role, root)
                self.assertEqual(17, len(actual['records']))
                self.assertEqual(self.result['controlValidation' if role == 'control' else 'validation']['decisions'][root], actual['validationDecision'])
        self.assertTrue(self.checked(root=0)['validationDecision']['passed'])
        self.assertFalse(self.checked(root=1)['validationDecision']['passed'])

    def test_protection_uses_all_completable_routes(self):
        actual = self.checked()
        self.assertEqual(['e00', 'e01'], actual['records'][0]['protectedIfPreserving'])
        self.assertTrue(all(not r['removedCompletableEndpoints'] for r in actual['records']))

    def test_wrong_version_rejected(self):
        with self.assertRaisesRegex(ValueError, 'arm contract'):
            self.checked(change=lambda r: r.update(version='tower-proposal-racing-v5'))

    def test_wrong_policy_rejected(self):
        with self.assertRaisesRegex(ValueError, 'arm contract'):
            self.checked(plan_change=lambda p: p['policy'].update(creationRemovalRule='changed'))

    def test_duplicate_and_heldout_seed_reuse_rejected(self):
        for value in (self.plans[0]['candidate']['racing']['panels'][0]['seeds'][0], self.freeze['families'][0]['seeds'][0]):
            with self.assertRaisesRegex(ValueError, 'Reused'):
                self.checked(plan_change=lambda p: p['racing']['panels'][5]['seeds'].__setitem__(0, value))

    def test_wrong_panel_order_rejected(self):
        def change(r):
            rows = r['evaluation']['panels'][4]['observations']
            rows[0], rows[1] = rows[1], rows[0]
        with self.assertRaisesRegex(ValueError, 'observation'):
            self.checked(change=change)

    def test_incomplete_panel_rejected(self):
        with self.assertRaisesRegex(ValueError, 'panel freeze'):
            self.checked(change=lambda r: r['evaluation']['panels'][5]['observations'].pop())

    def test_changed_score_rejected(self):
        with self.assertRaisesRegex(ValueError, 'panel score'):
            self.checked(change=lambda r: r['evaluation']['panels'][0]['scores'][0].update(wins=99))

    def test_changed_contrast_rejected(self):
        with self.assertRaisesRegex(ValueError, 'panel contrasts'):
            self.checked(change=lambda r: r['evaluation']['panels'][4]['contrasts'][0].update(gainedWins=99))

    def test_changed_pruning_rejected(self):
        with self.assertRaisesRegex(ValueError, 'pruning'):
            self.checked(change=lambda r: r['evaluation']['decisions'][0].update(diversityDistance=99))

    def test_changed_beam_rejected(self):
        with self.assertRaisesRegex(ValueError, 'continuation beam'):
            self.checked(change=lambda r: r['evaluation']['decisions'][0]['beamIds'].reverse())

    def test_changed_nominee_order_rejected(self):
        with self.assertRaisesRegex(ValueError, 'nominees'):
            self.checked(change=lambda r: r['evaluation']['nominees'].reverse())

    def test_changed_freeze_rejected(self):
        with self.assertRaisesRegex(ValueError, 'frozen challenger'):
            self.checked(change=lambda r: r['evaluation']['validationFreeze'].update(challengerId='other'))

    def test_changed_gate_and_boolean_integer_rejected(self):
        for fields in ({'passed': False}, {'gainedWins': True}):
            with self.assertRaisesRegex(ValueError, 'exact gate'):
                self.checked(change=lambda r: r['evaluation']['validationDecision'].update(fields))

    def test_exact_gate_boundary(self):
        panel = copy.deepcopy(self.pairs[0]['candidate']['evaluation']['panels'][5])
        freeze = self.pairs[0]['candidate']['evaluation']['validationFreeze']
        for gains, passed in ((0, False), (4, False), (5, True)):
            for i, row in enumerate(panel['observations']):
                row['outcome']['outcome'] = 'Victory' if i < gains else 'Defeat'
            decision = review.audit.validation_decision(freeze, panel)
            self.assertEqual(passed, decision['passed'])
            self.assertEqual(1, decision['tailNumerator'])
            self.assertEqual(2**gains, decision['tailDenominator'])

    def test_shared_physical_outcomes_must_agree(self):
        control, candidate = copy.deepcopy(self.pairs[0]['control']), copy.deepcopy(self.pairs[0]['candidate'])
        review.audit.preservation_trajectories(control, candidate)
        candidate['evaluation']['panels'][0]['observations'][0]['outcome']['guardianHealth'] += 1
        with self.assertRaisesRegex(ValueError, 'shared proposer'):
            review.audit.preservation_trajectories(control, candidate)

    def test_removed_protected_endpoint_rejected(self):
        with self.assertRaisesRegex(ValueError, 'preservation metadata'):
            self.checked(change=lambda r: r['batches'][0]['proposals'][0]['affinityCreation']['removalSelection'].update(protectedEssences=['e00', 'e01', 'e02']))

    def test_missing_heldout_stays_null(self):
        result = self.checked(heldout={})
        self.assertIsNone(result['challengerHeldoutWins'])
        self.assertTrue(all(r['heldoutWins'] is None for r in result['records']))
        self.assertEqual(0, review.summarize([result])['generatedHeldoutKnown'])
        self.assertIsNone(review.contrast({}, 'unmeasured', 'benchmark'))

    def test_inconsistent_shared_heldout_rejected(self):
        endpoint = copy.deepcopy(self.result['roots'][0])
        endpoint['candidateWins'] -= 1
        with self.assertRaisesRegex(ValueError, 'shared outcome'):
            review.heldout_map(endpoint, self.freeze['families'][0])

    def test_manifest_pin_path_drift_and_recheck(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root/'row.json').write_text('{}', encoding='utf-8')
            (root/'files.json').write_text(json.dumps({'row.json': review.sha(b'{}')}), encoding='utf-8')
            pin = review.audit.sha(root/'files.json')
            with self.assertRaisesRegex(ValueError, 'manifest pin'):
                review.Evidence(root, '0'*64)
            evidence = review.Evidence(root, pin)
            self.assertEqual({}, evidence.read('row.json'))
            with self.assertRaisesRegex(ValueError, 'Unbound'):
                evidence.read('../row.json')
            (root/'row.json').write_text('{"changed":true}', encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'consumed evidence'):
                evidence.read('row.json')
            with self.assertRaisesRegex(ValueError, 'during review'):
                evidence.recheck()


if __name__ == '__main__':
    unittest.main(verbosity=2)
