"""The expanded binding must preserve both previously frozen recipe families."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch


def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


b = module('floor14_restoration_binding_tests', 'tower-floor14-restoration-binding.py')
fixtures = module('floor14_restoration_binding_fixtures', 'test-tower-floor14-limited-restoration.py')


class RestorationBindingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        fixtures.Floor14RestorationTests.setUpClass()
        source = fixtures.Floor14RestorationTests.original
        cls.original = copy.deepcopy(source[:10])
        cls.parents = []
        for i in range(2):
            full = copy.deepcopy(source[2*i])
            full.update(id='full-armor-'+str(i), gear='retained-armor-and-health')
            for member in full['scenario']['party']:
                for item in member['build']['equipment']:
                    if item['slot'] in b.base.family.SLOTS:
                        item['definitionId'] += '.spec.armor-health'
            cls.original.append(full)
            cls.parents.append(full['id'])
        cls.original.extend(copy.deepcopy(source[10:15]+source[125:181]))
        cls.armor = b.base.family.propose(14, cls.original, cls.parents)
        cls.proposal = b.family.propose(cls.armor['cells'])

    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.saved = self.root/'restoration.json'
        self.prior = self.root/'armor.json'
        self.saved.write_text(json.dumps(self.proposal), encoding='utf-8')
        self.prior.write_text(json.dumps(self.armor), encoding='utf-8')
        self.pin, self.prior_pin = b.family.h.sha(self.saved), b.family.h.sha(self.prior)
        for target, name, value in [(b, 'SAVED_PROPOSAL', self.pin), (b.base, 'SAVED_PROPOSAL', self.prior_pin),
                                    (b.base, 'PARENTS', self.parents)]:
            replacement = patch.object(target, name, value)
            replacement.start()
            self.addCleanup(replacement.stop)
        self.binding = dict(version=b.VERSION, status='ProposedNotAdmitted', floor=14,
            source=str(self.root/'source'), sourceManifestSha256=b.base.SOURCE_MANIFEST,
            sourceCellsSha256=b.base.SOURCE_CELLS, currentTowerSha256='a'*64, currentAbilitiesSha256='b'*64,
            savedProposal=str(self.saved), savedProposalSha256=self.pin,
            armorProposal=str(self.prior), armorProposalSha256=self.prior_pin,
            newSeeds=0, newFights=0, nativePreparations=0)

    def test_both_families_reconstruct_with_all_controls(self):
        cells = b.validate(self.binding, self.original)
        self.assertEqual(cells, self.proposal['cells'])
        self.assertEqual(cells[:183], self.armor['cells'])
        self.assertEqual(cells[:73], self.original)
        self.assertEqual(len(cells), 198)

    def test_existing_binding_remains_unchanged(self):
        self.assertEqual(b.base.validate(b.previous_binding(self.binding), self.original), self.armor['cells'])

    def test_missing_or_extra_field_rejected(self):
        for field in self.binding:
            changed = self.binding.copy()
            del changed[field]
            with self.assertRaises(ValueError): b.validate(changed, self.original)
        with self.assertRaises(ValueError): b.validate({**self.binding, 'candidatePlan': {}}, self.original)

    def test_status_floor_or_allocations_cannot_change(self):
        for field, value in [('status', 'Qualified'), ('floor', 15), ('newSeeds', 1), ('newFights', 1),
                             ('nativePreparations', 1), ('version', b.base.VERSION)]:
            with self.assertRaises(ValueError): b.validate({**self.binding, field: value}, self.original)

    def test_source_or_catalog_pins_cannot_be_removed(self):
        for field in ('sourceManifestSha256', 'sourceCellsSha256', 'currentTowerSha256', 'currentAbilitiesSha256'):
            with self.assertRaises(ValueError): b.validate({**self.binding, field: 'invalid'}, self.original)

    def test_relative_source_and_both_artifact_paths_rejected(self):
        for field in ('source', 'savedProposal', 'armorProposal'):
            with self.assertRaises(ValueError): b.validate({**self.binding, field: 'relative/file'}, self.original)

    def test_changed_restoration_artifact_cannot_rebind_hash(self):
        self.saved.write_text('{}', encoding='utf-8')
        for pin in (self.pin, b.family.h.sha(self.saved)):
            with self.assertRaises(ValueError): b.validate({**self.binding, 'savedProposalSha256': pin}, self.original)

    def test_changed_armor_artifact_cannot_rebind_hash(self):
        self.prior.write_text('{}', encoding='utf-8')
        for pin in (self.prior_pin, b.family.h.sha(self.prior)):
            with self.assertRaises(ValueError): b.validate({**self.binding, 'armorProposalSha256': pin}, self.original)

    def test_raw_restoration_recipe_reconstructed_even_after_repinning(self):
        changed = copy.deepcopy(self.proposal)
        changed['cells'][-1]['scenario']['party'][1]['build']['essenceIds'].reverse()
        self.saved.write_text(json.dumps(changed), encoding='utf-8')
        pin = b.family.h.sha(self.saved)
        with patch.object(b, 'SAVED_PROPOSAL', pin):
            with self.assertRaisesRegex(ValueError, 'raw recipes'):
                b.validate({**self.binding, 'savedProposalSha256': pin}, self.original)

    def test_raw_armor_recipe_reconstructed_even_after_repinning(self):
        changed = copy.deepcopy(self.armor)
        changed['cells'][-1]['scenario']['party'][1]['build']['equipment'].reverse()
        self.prior.write_text(json.dumps(changed), encoding='utf-8')
        pin = b.family.h.sha(self.prior)
        with patch.object(b.base, 'SAVED_PROPOSAL', pin):
            with self.assertRaisesRegex(ValueError, 'raw recipes'):
                b.validate({**self.binding, 'armorProposalSha256': pin}, self.original)

    def test_native_pins_include_both_artifacts_and_every_helper(self):
        pins = b.input_pins(self.binding)
        expected = b.base.input_pins(b.previous_binding(self.binding))
        expected.update({str(p.resolve()): b.family.h.sha(p) for p in
                         [self.saved, Path(b.__file__), Path(b.family.__file__), Path(b.family.h.__file__)]})
        self.assertEqual(pins, expected)
        self.assertEqual(pins[str(self.saved)], self.pin)
        self.assertEqual(pins[str(self.prior)], self.prior_pin)

    def test_native_pins_reject_changed_artifact(self):
        self.saved.write_text('{}', encoding='utf-8')
        with self.assertRaises(ValueError): b.input_pins(self.binding)


if __name__ == '__main__':
    unittest.main()
