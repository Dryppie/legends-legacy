"""The qualified-family adapter cannot replace or promote its saved proposal."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch


def module(name, file):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(file))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


b = module('floor14_binding', 'tower-floor14-family-binding.py')
fixtures = module('floor14_binding_fixtures', 'test-tower-late-floor-limited-defense.py')


class FamilyBindingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original, cls.parents = fixtures.fixture(14)
        cls.proposal = b.family.propose(14, cls.original, cls.parents)

    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        self.saved = self.root/'proposal.json'
        self.saved.write_text(json.dumps(self.proposal), encoding='utf-8')
        self.pin = hashlib.sha256(self.saved.read_bytes()).hexdigest()
        # Small synthetic recipes exercise the adapter without requiring ignored
        # historical archives. Production uses its literal frozen hash/parents.
        for name, value in [('SAVED_PROPOSAL', self.pin), ('PARENTS', self.parents)]:
            replacement = patch.object(b, name, value)
            replacement.start()
            self.addCleanup(replacement.stop)
        self.binding = dict(version=b.VERSION, status='ProposedNotAdmitted', floor=14,
            source=str(self.root/'source'), sourceManifestSha256=b.SOURCE_MANIFEST, sourceCellsSha256=b.SOURCE_CELLS,
            currentTowerSha256='a'*64, currentAbilitiesSha256='b'*64, savedProposal=str(self.saved),
            savedProposalSha256=self.pin, newSeeds=0, newFights=0, nativePreparations=0)

    def test_complete_original_family_and_all_variants_are_returned(self):
        cells = b.validate(self.binding, self.original)
        self.assertEqual(cells, self.proposal['cells'])
        self.assertEqual(cells[:73], self.original)
        self.assertEqual(len(cells), 183)

    def test_claims_cannot_promote_or_expand_the_binding(self):
        for field, value in [('status', 'Qualified'), ('newSeeds', 1), ('newFights', 1), ('nativePreparations', 1),
                             ('floor', 15), ('version', 'unapproved'), ('candidatePlan', {'offense': .5})]:
            with self.subTest(field=field):
                binding = {**self.binding, field: value}
                with self.assertRaisesRegex(ValueError, 'unallocated'): b.validate(binding, self.original)

    def test_no_provenance_field_may_be_omitted(self):
        for field in self.binding:
            with self.subTest(field=field):
                binding = self.binding.copy()
                del binding[field]
                with self.assertRaises(ValueError): b.validate(binding, self.original)

    def test_other_source_cannot_replace_original(self):
        for field in ('sourceManifestSha256', 'sourceCellsSha256'):
            with self.assertRaisesRegex(ValueError, 'Original'):
                b.validate({**self.binding, field: 'c'*64}, self.original)

    def test_qualified_catalog_hashes_are_required(self):
        for field in ('currentTowerSha256', 'currentAbilitiesSha256'):
            for value in ('', 'A'*64, 'x'*64, None):
                with self.subTest(field=field, value=value):
                    with self.assertRaisesRegex(ValueError, 'SHA-256'):
                        b.validate({**self.binding, field: value}, self.original)

    def test_relative_artifact_or_source_paths_are_rejected(self):
        for field in ('source', 'savedProposal'):
            with self.assertRaisesRegex(ValueError, 'Absolute'):
                b.validate({**self.binding, field: 'relative/path'}, self.original)

    def test_changed_file_and_rebound_hash_cannot_replace_frozen_proposal(self):
        self.saved.write_text('{}', encoding='utf-8')
        changed = hashlib.sha256(self.saved.read_bytes()).hexdigest()
        for pin in (self.pin, changed):
            with self.assertRaisesRegex(ValueError, 'previously saved'):
                b.validate({**self.binding, 'savedProposalSha256': pin}, self.original)

    def test_native_input_pins_cover_artifact_and_all_helpers(self):
        pins = b.input_pins(self.binding)
        self.assertEqual(set(pins), {str(p.resolve()) for p in
            [self.saved, Path(b.__file__), Path(b.family.__file__), Path(b.family.h.__file__)]})
        self.assertEqual(pins[str(self.saved)], self.pin)

    def test_native_input_pins_reject_changed_proposal(self):
        self.saved.write_text('{}', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'previously saved'): b.input_pins(self.binding)

    def test_recipe_reconstruction_is_independent_of_byte_binding(self):
        changed = copy.deepcopy(self.proposal)
        changed['cells'][0]['scenario']['party'][0]['build']['essenceIds'].reverse()
        self.saved.write_text(json.dumps(changed), encoding='utf-8')
        pin = hashlib.sha256(self.saved.read_bytes()).hexdigest()
        with patch.object(b, 'SAVED_PROPOSAL', pin):
            with self.assertRaisesRegex(ValueError, 'raw recipes'):
                b.validate({**self.binding, 'savedProposalSha256': pin}, self.original)


if __name__ == '__main__':
    unittest.main()
