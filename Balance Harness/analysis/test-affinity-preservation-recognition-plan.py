"""Paired finite-population design, recorded sampling and source-binding tests."""
from collections import Counter
import copy
from fractions import Fraction
import importlib.util
from itertools import combinations
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('paired_recognition_builder', Path(__file__).with_name('affinity-preservation-recognition-plan.py'))
b = importlib.util.module_from_spec(spec)
spec.loader.exec_module(b)


class PairedRecognitionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.catalogue = b.catalogue()
        cls.catalogue_hash = b.digest(cls.catalogue)
        cls.entropy = bytes(range(256))
        cls.selection = b.choices(cls.catalogue, cls.catalogue_hash, cls.entropy)

    def plan(self):
        return b.build_plan(self.catalogue, self.selection, self.catalogue_hash, 'a'*64)

    def test_complete_union_and_both_arm_provenance(self):
        c = self.catalogue
        self.assertEqual(list(range(1, 13)), [r['root'] for r in c['roots']])
        self.assertEqual(321, sum(len(r['teams']) for r in c['roots']))
        for root in c['roots']:
            self.assertEqual(len(root['teams']), len({t['partyId'] for t in root['teams']}))
            for arm in b.ARMS:
                members = [t for t in root['teams'] if t['provenance'][arm]]
                self.assertEqual(20, len(members))
                self.assertEqual(17, sum(t['provenance'][arm]['generated'] for t in members))
                self.assertEqual(5, sum(t['provenance'][arm]['nomineeRank'] is not None for t in members))
                self.assertEqual(1, sum(t['provenance'][arm]['validationChallenger'] for t in members))
            self.assertTrue(all(t['scenario']['seeds'] == [] for t in root['teams']))
            self.assertTrue(all(t['seedFreeScenarioHash'] == b.digest(t['scenario']) for t in root['teams']))

    def test_finalists_are_census_and_every_remaining_member_can_be_sampled(self):
        for root in self.plan()['roots']:
            for t in root['teams']:
                if any(p and (p['nomineeRank'] is not None or p['validationChallenger']) for p in t['provenance'].values()):
                    self.assertEqual({'numerator': 1, 'denominator': 1}, t['inclusionProbability'])
                    self.assertIn(t['stratum'], ('reference', 'mandatory'))
            for t in root['unmeasured']:
                self.assertIsNone(t['independentOutcome'])
                self.assertGreater(t['inclusionProbability']['numerator'], 0)
                self.assertFalse(any(p and (p['nomineeRank'] is not None or p['validationChallenger']) for p in t['provenance'].values()))

    def test_stratum_partition_and_small_stratum_census(self):
        for root in self.catalogue['roots']:
            for name in b.STRATA:
                ids = [t['partyId'] for t in root['teams'] if t['stratum'] == name]
                self.assertEqual(sorted(set(ids)), ids)
                self.assertEqual(b.stratum_design(ids), root['strata'][name])
                self.assertTrue(all(t['membership'] == name.removeprefix('remaining-') for t in root['teams'] if t['stratum'] == name))
        for n in (0, 1, 2):
            ids = [str(i) for i in range(n)]
            selected, cursor = b.choose_subset(ids, b'', 0)
            self.assertEqual((ids, 0, None), (selected['partyIds'], cursor, selected['acceptedWordByteOffset']))

    def test_uniform_subsets_inclusion_and_weighted_expectation(self):
        # Exhaust every possible subset for every supported population size.
        for n in range(3, 35):
            ids = [f'{i:02}' for i in range(n)]
            pairs = list(combinations(range(n), 2))
            counts = Counter(i for p in pairs for i in p)
            self.assertEqual({i: n-1 for i in range(n)}, dict(counts))
            self.assertEqual(Fraction(2, n), Fraction(counts[0], len(pairs)))
            values = [Fraction(i*i-7, 13) for i in range(n)]
            estimates = [Fraction(n, 2)*(values[i]+values[j]) for i, j in pairs]
            self.assertEqual(sum(values), sum(estimates)/len(pairs))
            limit = (65536//len(pairs))*len(pairs)
            self.assertEqual(0, limit % len(pairs))
            for index, pair in enumerate(pairs):
                chosen, cursor = b.choose_subset(ids, index.to_bytes(2, 'little'), 0)
                self.assertEqual(([ids[i] for i in pair], 2), (chosen['partyIds'], cursor))

    def test_sampling_variance_identity_and_shared_cancellation(self):
        values = [Fraction(i*i-7, 13) for i in range(7)]
        estimates, variances = [], []
        for i, j in combinations(range(7), 2):
            estimates.append(Fraction(7, 2)*(values[i]+values[j]))
            sample_variance = (values[i]-values[j])**2/2
            variances.append(7**2*Fraction(5, 7)*sample_variance/2)
        actual_variance = sum((e-sum(values))**2 for e in estimates)/len(estimates)
        self.assertEqual(actual_variance, sum(variances)/len(variances))
        plan = self.plan()
        for root in plan['roots']:
            estimates = {arm: Fraction(0) for arm in b.ARMS}
            for i, t in enumerate(root['teams']):
                if t['stratum'] == 'reference':
                    continue
                p = t['inclusionProbability']
                contribution = Fraction(i-8, 256)*Fraction(p['denominator'], p['numerator'])/17
                for arm in b.ARMS:
                    if t['provenance'][arm]: estimates[arm] += contribution
            direct = sum(Fraction(i-8, 256)*Fraction(t['inclusionProbability']['denominator'], t['inclusionProbability']['numerator'])
                         * (int(t['provenance']['candidate'] is not None)-int(t['provenance']['control'] is not None))/17
                         for i, t in enumerate(root['teams']) if t['stratum'] != 'reference')
            self.assertEqual(direct, estimates['candidate']-estimates['control'])

    def test_rejected_words_tail_and_exhaustion(self):
        chosen, cursor = b.choose_subset(['a', 'b', 'c'], bytes([255, 255, 2, 0, 7, 9]), 0)
        self.assertEqual(([0], 2, ['b', 'c'], 4), (chosen['rejectedWordByteOffsets'], chosen['acceptedWordByteOffset'], chosen['partyIds'], cursor))
        for raw in (bytes([255])*256, bytes(255), bytes(257)):
            with self.assertRaises(ValueError): b.choices(self.catalogue, self.catalogue_hash, raw)
        self.assertEqual(256, self.selection['consumedBytes']+self.selection['unusedBytes'])

    def test_exact_costs_family_and_unknown_cells(self):
        plan = self.plan()
        n = sum(len(r['teams']) for r in plan['roots'])
        self.assertEqual(321-n, sum(len(r['unmeasured']) for r in plan['roots']))
        self.assertEqual(3072, plan['requiredFreshCombatValues'])
        self.assertEqual(n*256, plan['maximumAttemptedFights'])
        self.assertLessEqual(n, 156)
        self.assertEqual(n, plan['reporting']['rates'])
        self.assertEqual(3*(n-36), plan['reporting']['candidateReferenceContrasts'])
        self.assertEqual(n+6*(n-36), plan['reporting']['approximateWilsonFamily'])
        self.assertFalse(plan['runnableRequest'])
        self.assertEqual('FrozenDesignNativeIntegrationRequired', plan['status'])
        self.assertEqual('CompleteDiagnosticOnly', plan['reporting']['decision'])
        self.assertEqual('tower-affinity-preservation-comparison-v1', plan['sourceContract']['studyVersion'])

    def test_missing_reordered_or_altered_choices_rejected(self):
        for kind in ('root', 'order', 'stratum', 'probability', 'identity', 'index', 'binding'):
            s = copy.deepcopy(self.selection)
            row = s['roots'][0]['strata'][0]
            if kind == 'root': s['roots'].pop()
            elif kind == 'order': s['roots'].reverse()
            elif kind == 'stratum': s['roots'][0]['strata'].reverse()
            elif kind == 'probability': row['individualInclusion'] = b.fraction(1, 1)
            elif kind == 'identity': row['partyIds'][0] = 'unknown'
            elif kind == 'index': row['subsetIndex'] = True
            else: s['catalogueSha256'] = 'x'*64
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                b.build_plan(self.catalogue, s, self.catalogue_hash, 'a'*64)

    def source_root(self):
        source = b.source_tools()
        evidence = source.Evidence(source.RUN, source.RUN_PIN)
        pair = evidence.read('study/pair-01.json')
        plans = {arm: evidence.read(f'search/root-01/{arm}/racing/plan.json') for arm in b.ARMS}
        return {arm: pair[arm] for arm in b.ARMS}, plans, [s['party']['id'] for s in plans['control']['racing']['scope']['starts']]

    def test_scores_gate_result_and_selected_output_do_not_choose_membership(self):
        reports, plans, refs = self.source_root()
        expected = b.root_catalogue(1, reports, plans, refs)
        for arm in b.ARMS:
            e = reports[arm]['evaluation']
            e['rawSelectedId'] = 'irrelevant'
            e['validationDecision']['passed'] = not e['validationDecision']['passed']
            for p in e['panels']:
                for score in p['scores']: score['wins'] = 0
                for row in p['observations']: row['outcome']['outcome'] = 'Draw'
        self.assertEqual(expected, b.root_catalogue(1, reports, plans, refs))

    def test_wrong_source_versions_owned_scope_and_shared_context_rejected(self):
        reports, plans, refs = self.source_root()
        for kind in ('legacy', 'policy', 'owned', 'context', 'shared-recipe', 'finalist'):
            r, p = copy.deepcopy(reports), copy.deepcopy(plans)
            if kind == 'legacy': p['candidate']['version'] = 'tower-proposal-racing-v5'
            elif kind == 'policy': r['control']['selectionPolicyVersion'] = 'old'
            elif kind == 'owned': p['candidate']['racing']['scope']['ownedCopies'] = {}
            elif kind == 'context': r['candidate']['evaluation']['panels'][0]['observations'][0]['request']['scenario']['floorNumber'] += 1
            elif kind == 'shared-recipe': p['candidate']['racing']['scope']['starts'][0]['party']['builds']['1'].reverse()
            else: r['candidate']['evaluation']['validationFreeze']['challengerId'] = 'not-a-nominee'
            with self.subTest(kind=kind), self.assertRaises(ValueError): b.root_catalogue(1, r, p, refs)

    def catalogue_fixture(self, path):
        path.mkdir()
        b.save(path/'catalogue.json', self.catalogue)
        (path/'builder.py').write_bytes(Path(b.__file__).read_bytes())
        b.save(path/'completion.json', dict(status='CatalogueFrozen', seconds=0, newFights=0, newCombatValues=0))
        return b.seal(path)

    def test_recorded_round_trip_and_resealed_choice_tampering(self):
        with tempfile.TemporaryDirectory() as tmp:
            c, s, p = [Path(tmp)/n for n in ('catalogue', 'sample', 'plan')]
            pin = self.catalogue_fixture(c)
            with patch.object(b, 'catalogue', return_value=self.catalogue), patch.object(b.secrets, 'token_bytes', return_value=self.entropy) as draw:
                sample = b.sample(c, pin, s)
                plan = b.publish_plan(c, pin, s, sample['manifestSha256'], p)
                self.assertEqual('VerifiedAffinityPreservationRecognitionPlan', b.verify_plan(p, plan['manifestSha256'])['status'])
                with self.assertRaises(ValueError): b.sample(c, pin, s)
                with self.assertRaises(ValueError): b.publish_plan(c, pin, s, sample['manifestSha256'], p)
                draw.assert_called_once_with(256)
                changed = b.read(s/'choices.json'); changed['roots'][0]['strata'][0]['partyIds'].reverse()
                (s/'choices.json').write_text(json.dumps(changed), encoding='utf-8')
                (s/'files.json').unlink(); changed_pin = b.seal(s)
                with self.assertRaisesRegex(ValueError, 'choice differs'):
                    b.publish_plan(c, pin, s, changed_pin, Path(tmp)/'bad-plan')

    def test_failed_draw_is_retained_and_cannot_be_redrawn(self):
        with tempfile.TemporaryDirectory() as tmp:
            c, s = Path(tmp)/'catalogue', Path(tmp)/'sample'
            pin = self.catalogue_fixture(c)
            with patch.object(b, 'catalogue', return_value=self.catalogue), patch.object(b.secrets, 'token_bytes', return_value=bytes([255])*256) as draw:
                with self.assertRaisesRegex(ValueError, 'exhausted'): b.sample(c, pin, s)
                self.assertTrue((s/'failure.json').exists())
                self.assertEqual(256, (s/'entropy.bin').stat().st_size)
                self.assertFalse((s/'choices.json').exists())
                with self.assertRaisesRegex(ValueError, 'no redraw'): b.sample(c, pin, s)
                draw.assert_called_once_with(256)

    def test_literal_entropy_cannot_claim_production_provenance(self):
        with tempfile.TemporaryDirectory() as tmp:
            c, s = Path(tmp)/'catalogue', Path(tmp)/'sample'
            pin = self.catalogue_fixture(c)
            with patch.object(b, 'catalogue', return_value=self.catalogue):
                sampled = b.sample(c, pin, s, lambda size: self.entropy)
                with self.assertRaisesRegex(ValueError, 'provenance'):
                    b.verify_sampling(self.catalogue, b.sha((c/'catalogue.json').read_bytes()), pin, s, sampled['manifestSha256'])

    def test_external_pin_extra_member_and_changed_producer_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            c = Path(tmp)/'catalogue'; pin = self.catalogue_fixture(c)
            with self.assertRaisesRegex(ValueError, 'manifest'): b.authenticated(c, '0'*64)
            (c/'unexpected').write_text('extra', encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'membership'): b.authenticated(c, pin)
            (c/'unexpected').unlink()
            (c/'builder.py').write_text('# changed', encoding='utf-8')
            (c/'files.json').unlink(); changed = b.seal(c)
            with patch.object(b, 'catalogue', return_value=self.catalogue), self.assertRaisesRegex(ValueError, 'producer'):
                b.load_catalogue(c, changed)

    def test_resealed_entropy_failure_marker_and_plan_changes_rejected(self):
        for kind in ('entropy', 'failure', 'plan'):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as tmp:
                c, s, p = [Path(tmp)/n for n in ('catalogue', 'sample', 'plan')]
                pin = self.catalogue_fixture(c)
                with patch.object(b, 'catalogue', return_value=self.catalogue), patch.object(b.secrets, 'token_bytes', return_value=self.entropy):
                    sampled = b.sample(c, pin, s)
                    if kind == 'plan':
                        b.publish_plan(c, pin, s, sampled['manifestSha256'], p)
                        plan = b.read(p/'plan.json'); plan['maximumAttemptedFights'] -= 256
                        (p/'plan.json').write_text(json.dumps(plan), encoding='utf-8')
                        (p/'files.json').unlink(); changed = b.seal(p)
                        with self.assertRaisesRegex(ValueError, 'reproduce'): b.verify_plan(p, changed)
                    else:
                        if kind == 'entropy':
                            raw = bytearray((s/'entropy.bin').read_bytes()); raw[0] += 1
                            (s/'entropy.bin').write_bytes(raw)
                        else: b.save(s/'failure.json', dict(status='Failed'))
                        (s/'files.json').unlink(); changed = b.seal(s)
                        with self.assertRaises(ValueError): b.verify_sampling(self.catalogue, b.sha((c/'catalogue.json').read_bytes()), pin, s, changed)

    def test_same_recipe_across_roots_retains_separate_cells(self):
        plan = self.plan()
        benchmark = plan['roots'][0]['benchmarkPartyId']
        self.assertEqual(12, sum(t['partyId'] == benchmark for r in plan['roots'] for t in r['teams']))
        self.assertEqual(12, len(plan['roots']))
        self.assertTrue(all(r['benchmarkPartyId'] == benchmark for r in plan['roots']))

    def test_legacy_plan_identity_rejected(self):
        value = copy.deepcopy(self.catalogue)
        value['version'] = 'tower-affinity-creation-recognition-plan-v1'
        with self.assertRaisesRegex(ValueError, 'catalogue'): b.build_plan(value, self.selection, self.catalogue_hash, 'a'*64)


if __name__ == '__main__':
    unittest.main(verbosity=2)
