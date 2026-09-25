"""Source binding, single-factor design and pre-execution boundary tests."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('allied_design', Path(__file__).with_name('affinity-allied-action-comparison-design.py'))
b = importlib.util.module_from_spec(spec)
spec.loader.exec_module(b)


class AlliedActionDesignTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.evidence = b.load_evidence()
        cls.design = b.build_design(cls.evidence)

    def test_authenticates_previous_native_design_and_preview(self):
        files = self.design['sourceFiles']
        self.assertEqual(b.PRIOR_PLAN_PIN, files[b.PRIOR_PLAN])
        self.assertEqual(b.PREVIEW_PIN, files[b.PREVIEW+'/files.json'])
        self.assertEqual(b.VERIFICATION_PIN, files[b.VERIFICATION+'/files.json'])
        self.assertEqual(b.PRIOR_PREVIEW_PIN, files[b.PRIOR_PREVIEW+'/files.json'])
        self.assertEqual(118, self.evidence['closeout']['testsPassed'])

    def test_only_the_authored_guard_changes_the_proposer(self):
        p = self.design['plannedNativePlan']; old = self.evidence['priorPlan']
        self.assertEqual(old['candidate'], p['control'])
        a, c = copy.deepcopy(p['control']), copy.deepcopy(p['candidate'])
        for key in ['version', 'name', 'creationRemovalRule']:
            del a[key]; del c[key]
        self.assertEqual(a, c)
        self.assertEqual(old['analysis'], p['analysis'])
        self.assertEqual(old['validationProtocol'], p['validationProtocol'])
        self.assertEqual(old['selectionContrast'], p['selectionContrast'])
        self.assertEqual(['benchmark'], p['control']['parentTickets'])
        self.assertEqual(['affinity-create']*9, p['candidate']['firstWave'])
        self.assertEqual(['affinity-create']*8, p['candidate']['secondWave'])

    def test_allocation_ranges_cover_every_position_once_without_overlap(self):
        positions = []; heldout = []; search = []
        for i, row in enumerate(self.design['allocation']['slots']):
            self.assertEqual(i+1, row['root'])
            ranges = [row['proposalRoot'], *row['racing'], row['nomination'], row['validation']]
            self.assertEqual([1, 8, 8, 8, 8, 16, 60], [end-start for start, end in ranges])
            block = [n for start, end in ranges for n in range(start, end)]
            self.assertEqual(list(range(109*i, 109*(i+1))), block)
            sample = list(range(*row['heldout']))
            self.assertEqual(256, len(sample)); self.assertFalse(set(sample) & set(block))
            positions.extend(block + sample); search.extend(block); heldout.extend(sample)
        self.assertEqual(list(range(4380)), sorted(positions))
        self.assertEqual(4380, len(set(positions))); self.assertFalse(set(search) & set(heldout))
        self.assertEqual(1308, len(search)); self.assertEqual(3072, len(heldout))

    def test_fight_and_value_budgets_are_matched_and_do_not_depend_on_preview_results(self):
        p = self.design['plannedNativePlan']; e = self.design['evaluation']
        self.assertEqual(12*(1+4*8+16+60+256), p['requiredFreshValues'])
        self.assertEqual(12*2*528, e['searchFightsTotal'])
        self.assertEqual(12*3*256, e['heldoutFightsMaximum'])
        self.assertEqual(e['searchFightsTotal']+e['heldoutFightsMaximum'], p['maximumFights'])
        self.assertEqual(21888, p['maximumFights'])
        self.assertEqual(65536, self.design['allocation']['entropyBytes'])
        self.assertEqual(16384, self.design['allocation']['exposedSignedInt32Words'])
        self.assertEqual(0, self.design['allocation']['actualValuesAllocated'])

    def test_resource_partitions_cannot_borrow_or_shrink_inherited_floors(self):
        r = self.design['resources']
        self.assertEqual(10800, r['nativeMaximumSeconds']+r['auditPublicationMaximumSeconds'])
        self.assertEqual(6442450944, r['nativeMaximumBytes']+r['auditPublicationMaximumBytes'])
        self.assertEqual((9000, 1800, 120, 2), (r['nativeMaximumSeconds'], r['auditPublicationMaximumSeconds'], r['auditPublicationReserveSeconds'], r['forecastMargin']))
        self.assertEqual((900, 1073741824), (r['qualificationMaximumSeconds'], r['qualificationMaximumBytes']))
        self.assertIn('no borrowing', r['accounting'])
        self.assertIn('faster new timings never lower inherited floors', r['fit'])

    def test_feasibility_uses_complete_neighborhood_not_only_observed_draws(self):
        f = self.design['feasibility']
        self.assertEqual((41, 26, 17), (f['controlLegalRecipes'], f['candidateLegalRecipes'], f['requiredUniqueRecipesPerArmRoot']))
        self.assertEqual(39, f['risks']['controlDistinctObservedRecipes'])
        self.assertEqual(26, f['risks']['candidateDistinctObservedRecipes'])
        self.assertEqual(80, f['risks']['candidateRemovals']['Enchanted Fairy Essence'])
        self.assertFalse(f['previewMayGuaranteeFreshRootFill'])

    def test_plan_stays_separate_from_admission_and_native_runtime(self):
        self.assertEqual('FrozenProspectiveDesignNotImplementedNotAdmitted', self.design['status'])
        self.assertEqual('tower-proposal-racing-v6', self.design['intendedRuntime']['controlRacingVersion'])
        self.assertEqual('tower-proposal-racing-v7', self.design['intendedRuntime']['candidateRacingVersion'])
        self.assertEqual('VerifiedProspectiveDesignNotAdmitted', b.verify_design(self.design, self.evidence)['status'])
        self.assertFalse(self.design['productionDefaultsChanged'])
        self.assertTrue(all(self.design[k] == 0 for k in ['newFights', 'newReservedValues', 'newEntropyDraws']))

    def test_incomplete_and_identical_output_paths_do_not_allow_a_replacement(self):
        e = self.design['evaluation']; a = self.design['plannedNativePlan']['analysis']
        self.assertIn('all 24 complete', e['barrier'])
        self.assertIn('never replace roots or refill', e['incomplete'])
        self.assertIn('exact zero method contrast', e['heldoutPairing'])
        self.assertEqual(0, a['goPromisingNovelRootsAtLeast'])
        self.assertEqual(0, a['abandonPromisingNovelRootsBelow'])
        self.assertEqual((.02, 0, 3, -.02, -.02), (a['goMethodAtLeast'], a['goBenchmarkAtLeast'], a['goDifferingRootsAtLeast'], a['abandonMethodAtMost'], a['abandonBenchmarkAtMost']))
        self.assertIn('never-adopt', a['decisionOrder'])
        self.assertIn('do not substitute', self.design['reporting']['noDifferentiation'])

    def test_build_is_deterministic_and_does_not_mutate_source_evidence(self):
        before = b.digest(self.evidence)
        self.assertEqual(b.canonical(self.design), b.canonical(b.build_design(self.evidence)))
        self.assertEqual(before, b.digest(self.evidence))
        with patch.object(b, 'save', side_effect=AssertionError('Verification wrote a file')):
            b.verify_design(self.design, self.evidence)

    def test_rejects_each_changed_design_boundary_even_when_rehashed(self):
        mutations = [
            (['status'], 'Admitted'), (['plannedNativePlan','version'], 'tower-affinity-preservation-comparison-v1'),
            (['plannedNativePlan','control','creationRemovalRule'], b.RULE),
            (['plannedNativePlan','candidate','parentTickets'], ['beam']),
            (['plannedNativePlan','candidate','createdDamageAffinityIds'], []),
            (['plannedNativePlan','candidate','firstWave'], ['single']*9),
            (['plannedNativePlan','candidate','creationRemovalRule'], 'relax-on-failure'),
            (['plannedNativePlan','selectionContrast','control'], 'new-selector'),
            (['plannedNativePlan','analysis','goMethodAtLeast'], 0),
            (['plannedNativePlan','roots'], 11), (['plannedNativePlan','heldoutSamples'], 255),
            (['plannedNativePlan','maximumFights'], 21889), (['plannedNativePlan','inventoryHash'], 'a'*64),
            (['allocation','requiredFreshValues'], 3948), (['allocation','entropyBytes'], 65540),
            (['allocation','actualValuesAllocated'], 1), (['allocation','slots',0,'heldout'], [33,289]),
            (['resources','nativeMaximumSeconds'], 9100), (['resources','auditPublicationMaximumSeconds'], 1700),
            (['resources','forecastMargin'], 1), (['resources','qualificationMaximumBytes'], 1),
            (['feasibility','previewMayGuaranteeFreshRootFill'], True),
            (['physicalBinding','savedContextHash'], 'b'*64), (['sourceFiles',b.PRIOR_PLAN], 'c'*64),
            (['intendedRuntime','candidateRacingVersion'], 'tower-proposal-racing-v6'),
            (['evaluation','barrier'], 'evaluate-after-each-root'), (['newFights'], False),
            (['admissionRequirements'], []), (['productionDefaultsChanged'], True)]
        for keys, value in mutations:
            with self.subTest(path=keys):
                altered = copy.deepcopy(self.design); node = altered
                for k in keys[:-1]:
                    node = node[k]
                node[keys[-1]] = value
                self.assertNotEqual(b.digest(altered), b.digest(self.design))
                with self.assertRaisesRegex(ValueError, 'Changed frozen design'):
                    b.verify_design(altered, self.evidence)

    def test_unknown_fields_and_boolean_number_aliases_fail(self):
        for field, value in [('unexpected', 1), ('newFights', False), ('productionDefaultsChanged', 0)]:
            with self.subTest(field=field):
                altered = copy.deepcopy(self.design); altered[field] = value
                with self.assertRaises(ValueError):
                    b.verify_design(altered, self.evidence)

    def test_rejects_candidate_drift_before_building(self):
        e = copy.deepcopy(self.evidence)
        candidate = next(p for p in e['request']['policies'] if p['version'] == b.CANDIDATE)
        candidate['preserveParentInteractions'] = True
        with self.assertRaisesRegex(ValueError, 'More than'):
            b.build_design(e)

    def test_rejects_context_drift_before_building(self):
        e = copy.deepcopy(self.evidence); e['request']['context']['rootSeed'] += 1
        with self.assertRaisesRegex(ValueError, 'physical development context'):
            b.build_design(e)

    def test_rejects_missing_root_or_incomplete_preview(self):
        for change in ['missing-root', 'incomplete']:
            with self.subTest(change=change):
                e = copy.deepcopy(self.evidence)
                if change == 'missing-root':
                    e['preview']['rows'].pop()
                else:
                    e['batches']['arms'][0]['secondWave']['batch']['candidates'].pop()
                with self.assertRaises(ValueError):
                    b.build_design(e)

    def test_rejects_an_insufficient_neighborhood(self):
        e = copy.deepcopy(self.evidence)
        arm = next(a for a in e['batches']['arms'] if a['name'] == 'benchmark-allied-action-affinity-creation-v5')
        for row in arm['affinityCreationCoverage']['opportunities']:
            row['legalEdits'] = []
        with self.assertRaisesRegex(ValueError, 'Insufficient'):
            b.build_design(e)

    def test_rejects_provider_evidence_target_or_inventory_drift(self):
        for change in ['target', 'inventory']:
            with self.subTest(change=change):
                e = copy.deepcopy(self.evidence)
                arm = next(a for a in e['batches']['arms'] if a['name'] == 'benchmark-allied-action-affinity-creation-v5')
                report = arm['affinityCreationCoverage']['alliedActionProtection']
                if change == 'target':
                    report['providers'][0]['target'] = 'Self'
                else:
                    report['inventoryHash'] = 'a'*64
                with self.assertRaisesRegex(ValueError, 'protection evidence'):
                    b.build_design(e)

    def test_manifest_and_member_tampering_fail_before_consumption(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); folder = root/'package'; folder.mkdir()
            b.save(folder/'a.json', {'data': 1}); b.save(folder/'files.json', {'a.json': b.sha(folder/'a.json')})
            pin = b.sha(folder/'files.json')
            self.assertEqual({'data': 1}, b.authenticated(root, 'package', pin, ['a.json'], {})['a.json'])
            (folder/'a.json').write_text('{"data":2}')
            with self.assertRaisesRegex(ValueError, 'Changed source member'):
                b.authenticated(root, 'package', pin, ['a.json'], {})
            (folder/'files.json').write_text(json.dumps({'a.json': b.sha(folder/'a.json')}))
            with self.assertRaisesRegex(ValueError, 'Changed source manifest'):
                b.authenticated(root, 'package', pin, ['a.json'], {})

    def test_source_members_cannot_escape_package(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); folder = root/'package'; folder.mkdir()
            b.save(root/'a.json', {'data': 1}); b.save(folder/'files.json', {'../a.json': b.sha(root/'a.json')})
            with self.assertRaisesRegex(ValueError, 'Escaping'):
                b.authenticated(root, 'package', b.sha(folder/'files.json'), ['../a.json'], {})

    def test_duplicate_keys_and_nonfinite_numbers_are_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp)/'plan.json'
            for text in ['{"newFights":0,"newFights":1}', '{"x":NaN}', '{"x":Infinity}']:
                with self.subTest(text=text):
                    path.write_text(text)
                    with self.assertRaises(ValueError):
                        b.read(path)

    def test_publication_never_overwrites_an_existing_plan(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp)/'plan.json'
            b.save(path, self.design); pin = b.sha(path)
            with self.assertRaises(FileExistsError):
                b.save(path, {})
            self.assertEqual(pin, b.sha(path))
            self.assertEqual(self.design, b.read(path))


if __name__ == '__main__':
    unittest.main(verbosity=2)
