"""Deterministic pool/sampling checks and saved-source tamper rejection; no combat."""
from collections import Counter
import copy
from fractions import Fraction
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location('recognition_builder', Path(__file__).with_name('affinity-creation-recognition-plan.py'))
builder = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(builder)


class AffinityRecognitionPlanTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.catalogue = builder.catalogue()
        cls.catalogue_hash = builder.digest(cls.catalogue)
        cls.entropy = bytes(range(128))
        cls.selection = builder.choices(cls.catalogue, cls.catalogue_hash, cls.entropy)

    def plan(self):
        return builder.build_plan(self.catalogue, self.selection, self.catalogue_hash, 'a'*64)

    def test_complete_catalogue_partition_and_real_seed_free_recipes(self):
        self.assertEqual(list(range(1, 13)), [r['root'] for r in self.catalogue['roots']])
        for root in self.catalogue['roots']:
            self.assertEqual(Counter(reference=3, nominee=2, **{'near-miss': 2, 'lower': 13}), Counter(t['stratum'] for t in root['teams']))
            self.assertEqual(20, len({t['partyId'] for t in root['teams']}))
            for team in root['teams']:
                self.assertEqual([], team['scenario']['seeds'])
                self.assertEqual(team['seedFreeScenarioHash'], builder.digest(team['scenario']))
        self.assertFalse(self.catalogue['runnableRequest'])

    def test_all_unordered_pairs_are_uniform_and_have_declared_inclusion(self):
        counts = Counter(byte % 78 for byte in range(234))
        self.assertEqual({i: 3 for i in range(78)}, dict(counts))
        inclusion = Counter(i for pair in builder.PAIRS for i in pair)
        self.assertEqual({i: 12 for i in range(13)}, dict(inclusion))
        self.assertEqual(Fraction(2, 13), Fraction(inclusion[0], len(builder.PAIRS)))
        self.assertEqual(78, len(set(builder.PAIRS)))
        # Exhaustive design-based expectation for arbitrary finite population values.
        values = [Fraction(i*i-9, 17) for i in range(13)]
        estimates = [Fraction(13, 2)*(values[a]+values[b]) for a, b in builder.PAIRS]
        self.assertEqual(sum(values), sum(estimates)/78)
        known = Fraction(-7, 3)
        self.assertEqual((known+sum(values))/17, sum((known+x)/17 for x in estimates)/78)

    def test_rejected_bytes_and_unused_tail_are_preserved(self):
        entropy = bytes([255, 234, 233, 0])+bytes(124)
        choices = builder.choices(self.catalogue, self.catalogue_hash, entropy)
        first = choices['roots'][0]
        self.assertEqual(([0, 1], 2, 77), (first['rejectedByteOffsets'], first['acceptedByteOffset'], first['pairIndex']))
        self.assertEqual((14, 114), (choices['consumedBytes'], choices['unusedBytes']))
        self.assertEqual(builder.sha(entropy), choices['entropySha256'])

    def test_exhausted_and_wrong_size_batches_do_not_make_partial_plan(self):
        for entropy in (bytes([255])*128, bytes(127), bytes(129)):
            with self.subTest(size=len(entropy)), self.assertRaises(ValueError):
                builder.choices(self.catalogue, self.catalogue_hash, entropy)

    def test_each_of_78_pairs_builds_exactly_nine_teams_per_root(self):
        for index in range(78):
            selected = builder.choices(self.catalogue, self.catalogue_hash, bytes([index])*128)
            plan = builder.build_plan(self.catalogue, selected, self.catalogue_hash, 'a'*64)
            self.assertEqual((108, 132), (sum(len(r['teams']) for r in plan['roots']), sum(len(r['unmeasured']) for r in plan['roots'])))
            for root, population in zip(plan['roots'], self.catalogue['roots']):
                self.assertEqual(Counter(reference=3, nominee=2, **{'near-miss': 2, 'lower': 2}), Counter(t['stratum'] for t in root['teams']))
                self.assertEqual([t['partyId'] for t in population['teams'] if t['stratum'] != 'lower'],
                                 [t['partyId'] for t in root['teams'] if t['stratum'] != 'lower'])

    def test_fight_arithmetic_nulls_and_stratified_reporting(self):
        plan = self.plan()
        self.assertEqual((256, 3072, 27648), (plan['samplesPerTeam'], plan['requiredFreshCombatValues'], plan['maximumAttemptedFights']))
        self.assertEqual((108, 216, 540), tuple(plan['reporting'][k] for k in ('rates', 'candidateReferenceContrasts', 'approximateWilsonFamily')))
        self.assertTrue(all(row['independentOutcome'] is None for root in plan['roots'] for row in root['unmeasured']))
        self.assertFalse(plan['runnableRequest'])
        self.assertEqual('DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion', plan['reporting']['interpretation'])
        self.assertEqual('FrozenDesignNativeIntegrationRequired', plan['status'])
        self.assertEqual(12, len(plan['roots']))  # References stay separate across roots.

    def test_missing_reordered_or_cherry_picked_sampling_choices_rejected(self):
        for kind in ('omit', 'order', 'identity', 'pair', 'binding'):
            selected = copy.deepcopy(self.selection)
            if kind == 'omit': selected['roots'].pop()
            if kind == 'order': selected['roots'].reverse()
            if kind == 'identity': selected['roots'][0]['partyIds'][0] = 'b'*64
            if kind == 'pair': selected['roots'][0]['pairIndex'] = True
            if kind == 'binding': selected['catalogueSha256'] = 'b'*64
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                builder.build_plan(self.catalogue, selected, self.catalogue_hash, 'a'*64)

    def test_outcome_values_do_not_drive_catalogue_membership(self):
        source = builder.source_tools()
        evidence = source.Evidence(source.RUN, source.RUN_PIN)
        report = evidence.read('study/pair-01.json')['candidate']
        plan = evidence.read('search/root-01/candidate/racing/plan.json')
        references = [s['party']['id'] for s in plan['racing']['scope']['starts']]
        expected = builder.root_catalogue(1, report, plan, references)
        # Membership consumes the already-frozen strata, not observed scores or winners.
        report['evaluation']['rawSelectedId'] = references[0]
        for panel in report['evaluation']['panels']:
            for row in panel['scores']: row['wins'] = 0
            for observation in panel['observations']: observation['outcome']['outcome'] = 'Defeat'
        self.assertEqual(expected, builder.root_catalogue(1, report, plan, references))

    def test_incomplete_duplicate_or_changed_strata_rejected(self):
        source = builder.source_tools(); evidence = source.Evidence(source.RUN, source.RUN_PIN)
        original = evidence.read('study/pair-01.json')['candidate']
        plan = evidence.read('search/root-01/candidate/racing/plan.json')
        references = [s['party']['id'] for s in plan['racing']['scope']['starts']]
        for kind in ('incomplete', 'duplicate', 'nominee', 'beam'):
            report = copy.deepcopy(original)
            if kind == 'incomplete': report['evaluation']['status'] = 'Running'
            if kind == 'duplicate': report['batches'][0]['candidates'][1] = report['batches'][0]['candidates'][0]
            if kind == 'nominee': report['evaluation']['nominees'].pop()
            if kind == 'beam': report['evaluation']['decisions'][-1]['beamIds'].pop()
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                builder.root_catalogue(1, report, plan, references)

    def test_altered_physical_context_composition_and_seed_rejected(self):
        team = self.catalogue['roots'][0]['teams'][3]
        context = builder.physical_context(team['scenario'])
        source = builder.source_tools(); evidence = source.Evidence(source.RUN, source.RUN_PIN)
        template = evidence.read('source/context.json')['scope']
        allowed = {e['id']: e['family'] for e in template['allowedEssences']}
        for kind in ('gear', 'identity', 'seed', 'order', 'family', 'party'):
            altered = copy.deepcopy(team); scenario = altered['scenario']; actor = scenario['party'][0]['build']
            if kind == 'gear': actor['equipment'][0]['definitionId'] = 'changed'
            if kind == 'identity': actor['identityEssenceIds'].reverse()
            if kind == 'seed': scenario['seeds'] = [1]
            if kind == 'order': actor['essenceIds'].reverse()
            if kind == 'family': actor['essenceIds'][0] = actor['essenceIds'][1]
            if kind == 'party': scenario['party'].pop()
            altered['seedFreeScenarioHash'] = builder.digest(scenario)
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                builder.validate_recipe(altered, context, allowed, 5)

    def test_owned_copy_scope_is_not_silently_generalized(self):
        source = builder.source_tools(); evidence = source.Evidence(source.RUN, source.RUN_PIN)
        report = evidence.read('study/pair-01.json')['candidate']
        plan = evidence.read('search/root-01/candidate/racing/plan.json')
        plan['racing']['scope']['ownedCopies'] = {}
        with self.assertRaises(ValueError):
            builder.root_catalogue(1, report, plan, [s['party']['id'] for s in plan['racing']['scope']['starts']])

    def test_same_family_essences_fail_even_with_recomputed_hashes(self):
        team = copy.deepcopy(self.catalogue['roots'][0]['teams'][3])
        context = builder.physical_context(team['scenario'])
        source = builder.source_tools(); evidence = source.Evidence(source.RUN, source.RUN_PIN)
        allowed = {e['id']: e['family'] for e in evidence.read('source/context.json')['scope']['allowedEssences']}
        family = next(f for f in allowed.values() if list(allowed.values()).count(f) > 1)
        pair = [pid for pid, f in allowed.items() if f == family][:2]
        other = []
        used = {family.casefold()}
        for pid, f in allowed.items():
            if f.casefold() not in used:
                other.append(pid); used.add(f.casefold())
            if len(other) == 3: break
        team['scenario']['party'][0]['build']['essenceIds'] = sorted(pair+other)
        team['partyId'] = builder.digest({str(a['partySlot']): a['build']['essenceIds'] for a in team['scenario']['party']})
        team['seedFreeScenarioHash'] = builder.digest(team['scenario'])
        with self.assertRaisesRegex(ValueError, 'source-monster family'):
            builder.validate_recipe(team, context, allowed, 5)

    def catalogue_fixture(self, path):
        path.mkdir()
        builder.save(path/'catalogue.json', self.catalogue)
        (path/'builder.py').write_bytes(Path(builder.__file__).read_bytes())
        builder.save(path/'completion.json',dict(status='CatalogueFrozen',seconds=0,newFights=0,newCombatValues=0))
        return builder.seal(path)

    def test_sampling_failure_is_retained_and_cannot_be_retried(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); package = root/'catalogue'; output = root/'sample'
            pin = self.catalogue_fixture(package); calls = []
            def exhausted(size):
                calls.append(size); return bytes([255])*size
            with patch.object(builder, 'catalogue', return_value=self.catalogue), patch.object(builder.secrets, 'token_bytes', return_value=self.entropy):
                with self.assertRaisesRegex(ValueError, 'exhausted'):
                    builder.sample(package, pin, output, exhausted)
                self.assertTrue((output/'failure.json').exists())
                self.assertFalse((output/'choices.json').exists())
                with self.assertRaisesRegex(ValueError, 'no redraw'):
                    builder.sample(package, pin, output, exhausted)
            self.assertEqual([128], calls)

    def test_literal_sampling_fixture_cannot_be_presented_as_real_draw(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); c = root/'catalogue'; s = root/'sample'
            pin = self.catalogue_fixture(c)
            with patch.object(builder, 'catalogue', return_value=self.catalogue), patch.object(builder.secrets, 'token_bytes', return_value=self.entropy):
                sampled = builder.sample(c, pin, s, lambda size: bytes(range(size)))
            self.assertEqual('LiteralEngineeringFixture', builder.read(s/'intent.json')['origin'])
            with self.assertRaisesRegex(ValueError, 'provenance'):
                builder.verify_sampling(self.catalogue, builder.sha((c/'catalogue.json').read_bytes()), pin, s, sampled['manifestSha256'])

    def test_real_sampler_package_round_trip_and_resealed_choice_tamper(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); c, s, p = [root/n for n in ('catalogue', 'sample', 'plan')]
            pin = self.catalogue_fixture(c)
            with patch.object(builder, 'catalogue', return_value=self.catalogue), patch.object(builder.secrets, 'token_bytes', return_value=self.entropy):
                sampled = builder.sample(c, pin, s)
                built = builder.publish_plan(c, pin, s, sampled['manifestSha256'], p)
                self.assertEqual('VerifiedAffinityCreationRecognitionPlan', builder.verify_plan(p, built['manifestSha256'])['status'])
                with self.assertRaisesRegex(ValueError, 'already exists'):
                    builder.publish_plan(c, pin, s, sampled['manifestSha256'], p)
                tampered = builder.read(s/'choices.json'); tampered['roots'][0]['partyIds'].reverse()
                (s/'choices.json').write_text(json.dumps(tampered), encoding='utf-8')
                (s/'files.json').unlink(); new_pin = builder.seal(s)
                with self.assertRaisesRegex(ValueError, 'choice differs'):
                    builder.publish_plan(c, pin, s, new_pin, root/'bad-plan')

    def test_changed_external_pin_and_package_membership_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)/'package'; pin = self.catalogue_fixture(root)
            with self.assertRaisesRegex(ValueError, 'manifest'):
                builder.authenticated(root, '0'*64)
            (root/'unexpected.json').write_text('{}', encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'membership'):
                builder.authenticated(root, pin)

    def test_resealed_failure_marker_cannot_be_ignored(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); c, s = root/'catalogue', root/'sample'
            pin = self.catalogue_fixture(c)
            with patch.object(builder, 'catalogue', return_value=self.catalogue), patch.object(builder.secrets, 'token_bytes', return_value=self.entropy):
                builder.sample(c, pin, s)
            builder.save(s/'failure.json', dict(status='Failed'))
            (s/'files.json').unlink(); changed_pin = builder.seal(s)
            with self.assertRaisesRegex(ValueError, 'failed sampling'):
                builder.verify_sampling(self.catalogue, builder.sha((c/'catalogue.json').read_bytes()), pin, s, changed_pin)

    def test_new_cohort_has_explicit_source_and_runtime_identity(self):
        c, plan = self.catalogue, self.plan()
        self.assertEqual('tower-affinity-creation-recognition-plan-v1', c['version'])
        self.assertEqual(c['sourceContract'], plan['sourceContract'])
        self.assertEqual('tower-benchmark-validation-comparison-v1', plan['sourceContract']['studyVersion'])
        self.assertEqual('tower-racing-benchmark-validation-v1', plan['sourceContract']['selectionPolicyVersion'])
        self.assertEqual('6aab62f71d865cdd96c4e28b50426de4a374c51cfd3be22d58d03ebe3c0c49cd', c['capturedRuntime']['executionHash'])
        self.assertEqual(57, len({t['partyId'] for r in c['roots'] for t in r['teams'] if t['stratum'] != 'reference'}))
        self.assertEqual('study/pair-01.json', c['roots'][0]['sourceReport'])
        self.assertEqual('candidate', c['roots'][0]['sourceReportRole'])

    def test_legacy_plan_and_racing_versions_are_rejected(self):
        value = copy.deepcopy(self.catalogue); value['version'] = 'tower-frozen-pool-recognition-plan-v1'
        with self.assertRaisesRegex(ValueError, 'catalogue'):
            builder.build_plan(value,self.selection,self.catalogue_hash,'a'*64)
        source = builder.source_tools(); evidence = source.Evidence(source.RUN,source.RUN_PIN)
        report = evidence.read('study/pair-01.json')['candidate']
        plan = evidence.read('search/root-01/candidate/racing/plan.json')
        references = [s['party']['id'] for s in plan['racing']['scope']['starts']]
        for kind in ('plan','report','selection'):
            p,r = copy.deepcopy(plan),copy.deepcopy(report)
            if kind == 'plan': p['version'] = 'tower-proposal-racing-v4'
            elif kind == 'report': r['version'] = 'tower-proposal-racing-v4'
            else: r['selectionPolicyVersion'] = 'tower-racing-benchmark-positive-tie-v1'
            with self.subTest(kind=kind), self.assertRaisesRegex(ValueError,'source contract'):
                builder.root_catalogue(1,r,p,references)

    def test_current_validation_output_does_not_choose_catalogue_membership(self):
        source = builder.source_tools(); evidence = source.Evidence(source.RUN,source.RUN_PIN)
        report = evidence.read('study/pair-01.json')['candidate']
        plan = evidence.read('search/root-01/candidate/racing/plan.json')
        references = [s['party']['id'] for s in plan['racing']['scope']['starts']]
        expected = builder.root_catalogue(1,report,plan,references)
        report['evaluation']['validationFreeze']['challengerId'] = references[0]
        report['evaluation']['validationDecision']['passed'] = True
        report['evaluation']['validationDecision']['selectedId'] = references[0]
        report['evaluation']['rawSelectedId'] = references[0]
        self.assertEqual(expected,builder.root_catalogue(1,report,plan,references))

    def test_sealed_catalogue_with_changed_builder_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            package = Path(directory)/'catalogue'; self.catalogue_fixture(package)
            (package/'builder.py').write_text('# changed source',encoding='utf-8')
            (package/'files.json').unlink(); pin = builder.seal(package)
            with patch.object(builder,'catalogue',return_value=self.catalogue), self.assertRaisesRegex(ValueError,'producer'):
                builder.load_catalogue(package,pin)

    def test_changed_draw_resealed_without_matching_choices_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            c,s = Path(directory)/'catalogue',Path(directory)/'sample'
            pin = self.catalogue_fixture(c)
            with patch.object(builder,'catalogue',return_value=self.catalogue), patch.object(builder.secrets,'token_bytes',return_value=self.entropy):
                builder.sample(c,pin,s)
            entropy = bytearray((s/'entropy.bin').read_bytes()); entropy[0] += 1
            (s/'entropy.bin').write_bytes(entropy)
            (s/'files.json').unlink(); pin_s = builder.seal(s)
            with self.assertRaisesRegex(ValueError,'choice differs'):
                builder.verify_sampling(self.catalogue,builder.sha((c/'catalogue.json').read_bytes()),pin,s,pin_s)

    def test_repeated_recipe_occurrences_keep_root_specific_scenarios(self):
        recipe = self.catalogue['roots'][0]['teams'][0]['partyId']
        cells = [r['teams'][0] for r in self.plan()['roots']]
        self.assertEqual([recipe]*12,[t['partyId'] for t in cells])
        self.assertEqual(12,len(cells))
        self.assertEqual(3072,self.plan()['requiredFreshCombatValues'])
        self.assertTrue(all(t['scenario']['seeds'] == [] for t in cells))


if __name__ == '__main__':
    unittest.main(verbosity=2)
