"""Fault injection for the independent native-catalogue comparison."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('placement_preview', Path(__file__).with_name('preview-loadout-placement.py'))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


class NativeCatalogueTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = m.ROOT / m.DESIGN
        m.require(m.sha(root / 'files.json') == m.DESIGN_PIN, 'Changed design manifest')
        files = m.read(root / 'files.json')
        m.require(m.sha(root / 'catalogue.json') == files['catalogue.json'], 'Changed design catalogue')
        m.require(m.sha(root / 'fixture.json') == files['fixture.json'], 'Changed design fixture')
        cls.anchor = m.read(root / 'fixture.json')['anchor']
        cls.expected = {r['partyId']: r for r in m.read(root / 'catalogue.json')}
        cls.literal = dict(version='tower-subgroup-loadout-placement-v1', assignmentsExamined=240,
            identityAssignments=2, referenceAssignments=0, duplicateAssignments=0, recipes=[dict(
                party=dict(id=pid, source=m.POLICY['version'], builds=r['builds']), subgroup=r['subgroup'],
                changedOwners=r['changedOwners'], replacementDistance=r['replacedAssignments'],
                seedFreeScenarioHash=m.native_digest(m.scenario_for(cls.anchor, r['builds'])), assignments=[dict(sourceByDestination={k: int(v)
                    for k, v in a.items()}) for a in r['assignments']]) for pid, r in sorted(cls.expected.items())])

    def test_complete_literal_catalogue_agrees(self):
        m.check_catalogue(self.literal, self.expected, self.anchor)

    def test_native_physical_hash_matches_captured_golden(self):
        first = self.literal['recipes'][0]
        self.assertEqual(first['party']['id'], '0001631c487bceaa2a4cbc37f1c296a0968422acd5be60e4a1228e7d035eef20')
        self.assertEqual(first['seedFreeScenarioHash'], '0c5cc7cc2c247013a8e7fb333b84ce4e47ee8efc1ce91f72d97e9f5a270a5fa8')
        self.assertNotEqual(first['seedFreeScenarioHash'], self.expected[first['party']['id']]['seedFreeScenarioHash'])
        bindings = m.check_catalogue(self.literal, self.expected, self.anchor)
        self.assertEqual(len(bindings), 238)

    def test_changed_anchor_rejected(self):
        changed = copy.deepcopy(self.anchor)
        changed['id'] += '-changed'
        with self.assertRaisesRegex(ValueError, 'physical scenario hash'):
            m.check_catalogue(self.literal, self.expected, changed)

    def test_unsupported_hash_encoding_rejected(self):
        with self.assertRaisesRegex(ValueError, 'ASCII fixture'):
            m.native_digest({'value': '\u00e9'})

    def test_scope_hash_binds_exact_root_metadata(self):
        scope = {'generation': {'seeds': [123]}}
        catalogue = {'scopeHash': m.native_digest(scope)}
        m.check_scope(catalogue, scope)
        with self.assertRaisesRegex(ValueError, 'scope binding'):
            m.check_scope(catalogue, {'generation': {'seeds': [124]}})

    def test_roots_share_recipes_but_may_have_distinct_scope_bound_catalogues(self):
        rows = [dict(rootSeed=i, recipeHash='same', catalogueHash=str(i)) for i in range(12)]
        m.check_preview_roots(rows)
        rows[0]['recipeHash'] = 'different'
        with self.assertRaisesRegex(ValueError, 'root-dependent recipes'):
            m.check_preview_roots(rows)
        rows[0]['recipeHash'] = 'same'
        rows[0]['rootSeed'] = 1
        with self.assertRaisesRegex(ValueError, 'Changed roots'):
            m.check_preview_roots(rows)

    def test_missing_recipe_rejected(self):
        changed = copy.deepcopy(self.literal)
        changed['recipes'].pop()
        with self.assertRaisesRegex(ValueError, 'catalogue'):
            m.check_catalogue(changed, self.expected, self.anchor)

    def test_duplicate_recipe_rejected(self):
        changed = copy.deepcopy(self.literal)
        changed['recipes'][1] = changed['recipes'][0]
        with self.assertRaisesRegex(ValueError, 'catalogue'):
            m.check_catalogue(changed, self.expected, self.anchor)

    def test_changed_build_rejected(self):
        changed = copy.deepcopy(self.literal)
        changed['recipes'][0]['party']['builds']['1'][0] = 'different'
        with self.assertRaisesRegex(ValueError, 'identity'):
            m.check_catalogue(changed, self.expected, self.anchor)

    def test_changed_assignment_rejected(self):
        changed = copy.deepcopy(self.literal)
        changed['recipes'][0]['assignments'][0]['sourceByDestination']['1'] = 10
        with self.assertRaisesRegex(ValueError, 'derivations'):
            m.check_catalogue(changed, self.expected, self.anchor)

    def test_changed_physical_hash_rejected(self):
        changed = copy.deepcopy(self.literal)
        changed['recipes'][0]['seedFreeScenarioHash'] = '0' * 64
        with self.assertRaisesRegex(ValueError, 'physical'):
            m.check_catalogue(changed, self.expected, self.anchor)

    def test_changed_movement_metadata_rejected(self):
        for field, value in [('subgroup', 9), ('changedOwners', [1]), ('replacementDistance', 0)]:
            changed = copy.deepcopy(self.literal)
            changed['recipes'][0][field] = value
            with self.assertRaisesRegex(ValueError, 'physical'):
                m.check_catalogue(changed, self.expected, self.anchor)

    def test_changed_accounting_rejected(self):
        changed = copy.deepcopy(self.literal)
        changed['identityAssignments'] = 3
        with self.assertRaisesRegex(ValueError, 'accounting'):
            m.check_catalogue(changed, self.expected, self.anchor)

    def test_legacy_arm_bytes_include_serialization_and_ignore_new_arms(self):
        original = b'{"arms": [{"name": "old", "value": 1}]}'
        extended = b'{"arms": [{"name": "old", "value": 1}, {"name": "new"}]}'
        m.check_legacy_arm_bytes(original, extended)
        with self.assertRaisesRegex(ValueError, 'serialized arm bytes'):
            m.check_legacy_arm_bytes(original, extended.replace(b'"value": 1', b'"value":1'))

    def test_preview_label_alignment_cannot_change_any_gameplay_field(self):
        previous = dict(policies=[], context=dict(benchmarkReferenceId='benchmark', scope=dict(
            references=[dict(id='benchmark', scenario=dict(id='older-preview', seeds=[], party=[{'actor': 1}]))])))
        anchor = dict(id='later-pilot', seeds=[], party=[{'actor': 1}])
        normalized = m.preview_request(previous, anchor)
        self.assertEqual(normalized['context']['scope']['references'][0]['scenario'], anchor)
        self.assertEqual(previous['context']['scope']['references'][0]['scenario']['id'], 'older-preview')
        with self.assertRaisesRegex(ValueError, 'beyond the diagnostic scenario label'):
            m.preview_request(previous, dict(anchor, party=[{'actor': 2}]))


if __name__ == '__main__':
    unittest.main(verbosity=2)
