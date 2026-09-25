"""Synthetic invariant tests for the combat-free placement design specification."""
from collections import Counter
import copy
import importlib.util
from itertools import product
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('placement_design',
    Path(__file__).with_name('loadout-placement-design.py'))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


def fixture():
    parent = {str(i): [f'e{i:02d}.{s}' for s in 'abcde'] for i in range(1, 11)}
    scope = dict(requiredPartySize=10, budget=dict(essenceSlots=5), ownedCopies=None,
        allowedEssences=[dict(id=e, family=e[-1]) for ids in parent.values() for e in ids])
    return parent, scope


class PlacementDesignTests(unittest.TestCase):
    def test_complete_distinct_census(self):
        parent, scope = fixture()
        rows, counts = m.enumerate_loadouts(parent, scope)
        self.assertEqual(counts['assignmentsExamined'], 240)
        self.assertEqual(counts['identityAssignments'], 2)
        self.assertEqual(len(rows), 238)
        self.assertEqual(Counter(len(r['changedOwners']) for r in rows), {2: 20, 3: 40, 4: 90, 5: 88})

    def test_independent_cartesian_bijection_oracle(self):
        parent, scope = fixture()
        expected = set()
        for group in m.GROUPS:
            for assignment in product(range(5), repeat=5):
                if len(set(assignment)) != 5:
                    continue
                builds = copy.deepcopy(parent)
                for i, owner in enumerate(group):
                    builds[owner] = parent[group[assignment[i]]]
                if builds != parent:
                    expected.add(m.digest(builds))
        rows, _ = m.enumerate_loadouts(parent, scope)
        self.assertEqual(expected, {r['partyId'] for r in rows})

    def test_subgroup_multisets_and_complete_bundles(self):
        parent, scope = fixture()
        for row in m.enumerate_loadouts(parent, scope)[0]:
            for group in m.GROUPS:
                self.assertEqual(m.inventory(parent, group), m.inventory(row['builds'], group))
                self.assertEqual(Counter(tuple(parent[o]) for o in group),
                                 Counter(tuple(row['builds'][o]) for o in group))
            changed = [o for o in parent if parent[o] != row['builds'][o]]
            self.assertEqual(len({(int(o) - 1) // 5 for o in changed}), 1)

    def test_identical_loadouts_are_empty(self):
        parent, scope = fixture()
        parent = {o: list(parent['1']) for o in parent}
        rows, counts = m.enumerate_loadouts(parent, scope)
        self.assertEqual(rows, [])
        self.assertEqual(counts['identityAssignments'], 240)

    def test_duplicate_loadouts_do_not_weight_recipes(self):
        parent, scope = fixture()
        for group in m.GROUPS:
            for owner in group[1:4]:
                parent[owner] = list(parent[group[0]])
        rows, counts = m.enumerate_loadouts(parent, scope)
        self.assertEqual(len(rows), 8)
        self.assertEqual(counts['identityAssignments'], 48)
        self.assertEqual(counts['duplicateAssignments'], 184)
        self.assertTrue(all(len(r['assignments']) == 24 for r in rows))

    def test_explicit_reference_exclusion(self):
        parent, scope = fixture()
        excluded = {r['partyId'] for r in m.enumerate_loadouts(parent, scope)[0][:3]}
        rows, counts = m.enumerate_loadouts(parent, scope, excluded)
        self.assertEqual(len(rows), 235)
        self.assertEqual(counts['referenceAssignments'], 3)
        self.assertFalse(excluded & {r['partyId'] for r in rows})

    def test_parent_is_immutable_and_key_order_is_irrelevant(self):
        parent, scope = fixture()
        snapshot = copy.deepcopy(parent)
        rows, _ = m.enumerate_loadouts(parent, scope)
        again, _ = m.enumerate_loadouts(dict(reversed(list(parent.items()))), scope)
        self.assertEqual(parent, snapshot)
        self.assertEqual(rows, again)

    def test_cross_subgroup_and_nonbijective_assignment_rejected(self):
        parent, _ = fixture()
        for assignment in [('1', '2', '3', '4', '6'), ('1', '1', '3', '4', '5')]:
            with self.assertRaisesRegex(ValueError, 'bijection'):
                m.apply_assignment(parent, m.GROUPS[0], assignment)

    def test_exact_owned_limits_preserved(self):
        parent, scope = fixture()
        scope['ownedCopies'] = dict(Counter(e for ids in parent.values() for e in ids))
        self.assertEqual(len(m.enumerate_loadouts(parent, scope)[0]), 238)
        scope['ownedCopies'][parent['1'][0]] = 0
        with self.assertRaisesRegex(ValueError, 'Owned-copy'):
            m.enumerate_loadouts(parent, scope)

    def test_case_insensitive_family_collision_rejected(self):
        parent, scope = fixture()
        scope['allowedEssences'][0]['family'] = 'B'
        with self.assertRaisesRegex(ValueError, 'family'):
            m.enumerate_loadouts(parent, scope)

    def test_noncanonical_unknown_or_duplicate_id_rejected(self):
        for mutation in ('reversed', 'unknown', 'duplicate'):
            parent, scope = fixture()
            if mutation == 'reversed':
                parent['1'].reverse()
            elif mutation == 'unknown':
                parent['1'][0] = '!unknown'
            else:
                parent['1'][0] = parent['1'][1]
            with self.assertRaisesRegex(ValueError, 'canonical'):
                m.enumerate_loadouts(parent, scope)

    def test_changed_shape_rejected(self):
        parent, scope = fixture()
        scope['requiredPartySize'] = 15
        with self.assertRaisesRegex(ValueError, 'ten owners'):
            m.enumerate_loadouts(parent, scope)

    def test_scenario_changes_only_essences_and_clears_seeds(self):
        parent, scope = fixture()
        anchor = dict(seeds=[101], floorNumber=5, party=[dict(partySlot=int(o),
            build=dict(id='actor-' + o, equipment=[dict(id='weapon-' + o)],
                       identityEssenceIds=['neutral-' + o], essenceIds=ids)) for o, ids in parent.items()])
        before = copy.deepcopy(anchor)
        builds = m.enumerate_loadouts(parent, scope)[0][0]['builds']
        changed = m.scenario_for(anchor, builds)
        self.assertEqual(anchor, before)
        self.assertEqual(changed['seeds'], [])
        for old, new in zip(anchor['party'], changed['party']):
            self.assertEqual(old['partySlot'], new['partySlot'])
            self.assertEqual({k: v for k, v in old['build'].items() if k != 'essenceIds'},
                             {k: v for k, v in new['build'].items() if k != 'essenceIds'})

    def test_single_swap_comparison_preserves_counts(self):
        parent, scope = fixture()
        # Five legal same-family slot swaps for each of the twenty owner pairs.
        self.assertEqual(len(m.single_swap_ids(parent, scope)), 100)
        scope['ownedCopies'] = dict(Counter(e for ids in parent.values() for e in ids))
        self.assertEqual(len(m.single_swap_ids(parent, scope)), 100)

    def test_ambiguous_json_rejected(self):
        for raw in ('{"x":1,"x":2}', '{"x":NaN}', '{"x":Infinity}'):
            with self.assertRaises(ValueError):
                m.decode(raw)


if __name__ == '__main__':
    unittest.main(verbosity=2)
