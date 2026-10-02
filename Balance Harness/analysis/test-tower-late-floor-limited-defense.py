"""Reject missing controls, broadened gear budgets and reordered late-floor builds."""
import copy
import importlib.util
import itertools
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('late_floor_defense', Path(__file__).with_name('tower-late-floor-limited-defense.py'))
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)


def cell(floor, name, composition, specialized):
    budget = p.FLOORS[floor]
    party = []
    for slot in range(1, budget['party']+1):
        ids = [f'{composition}-{i}' for i in reversed(range(budget['essences']))]
        equipment = [dict(slot=s, definitionId=('saved.spec.'+budget['defense'] if specialized and s in p.SLOTS else 'baseline')+'.'+s,
                          activeStyleId=None, useNativeStyle=False)
                     for s in ('MainHand', 'Chest', 'Head', 'Legs', 'Ring', 'Necklace', 'Relic')]
        party.append(dict(partySlot=slot, build=dict(id=f'{name}-{slot}', characterLevel=budget['level'], tier=2,
            rank=5, quality='Masterpiece', attributeRollMultiplier=1, essenceIds=ids,
            identityEssenceIds=list(reversed(ids)), identityEquipment=[dict(id='saved-identity')], equipment=equipment)))
    return dict(id=name, composition=name, gear='retained-'+budget['defense']+'-and-health' if specialized else 'retained-baseline',
                origin='retained-control', scenario=dict(id=name, floorNumber=floor, seeds=[], party=party,
                                                        assumptions=['preserve-exactly']))


def fixture(floor):
    budget = p.FLOORS[floor]
    original, parents = [], []
    for name in ('a', 'b'):
        baseline = cell(floor, name, name, False)
        full = copy.deepcopy(baseline)
        full.update(id=name+'-full', gear='retained-'+budget['defense']+'-and-health')
        for member, donor in zip(full['scenario']['party'], cell(floor, name, name, True)['scenario']['party'], strict=True):
            member['build']['equipment'] = donor['build']['equipment']
        original.extend([baseline, full])
        parents.append(full['id'])
    for i in range(budget['controls']-4):
        original.append(cell(floor, f'control-{i}', f'other-{i % (budget["compositions"]-2)}',
                             i >= budget['originalEligible']-2))
    return original, parents


class LateFloorProposalTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.fixtures = {floor: fixture(floor) for floor in p.FLOORS}
        cls.proposals = {floor: p.propose(floor, *args) for floor, args in cls.fixtures.items()}

    def test_all_controls_and_one_two_character_subsets(self):
        for floor, (original, parents) in self.fixtures.items():
            with self.subTest(floor=floor):
                result = p.validate(self.proposals[floor], floor, original, parents)
                self.assertEqual(result[:len(original)], original)
                slots = range(1, p.FLOORS[floor]['party']+1)
                expected = {s for n in (1, 2) for s in itertools.combinations(slots, n)}
                for parent in parents:
                    variants = [v for v in self.proposals[floor]['variants'] if v['sourceId'] == parent]
                    self.assertEqual(len(variants), len(expected))
                    self.assertEqual({tuple(v['defensePartySlots']) for v in variants}, expected)
                self.assertEqual((len(result), self.proposals[floor]['eligibleRecipes']),
                                 {13: (240, 128), 14: (183, 120), 15: (313, 250)}[floor])

    def test_source_is_not_mutated_or_aliased(self):
        for floor, (original, parents) in self.fixtures.items():
            snapshot = copy.deepcopy(original)
            proposal = p.propose(floor, original, parents)
            proposal['cells'][0]['scenario']['party'].reverse()
            proposal['variants'][0]['cell']['scenario']['party'][0]['build']['equipment'].clear()
            self.assertEqual(original, snapshot)

    def test_raw_order_and_identity_cannot_change(self):
        changes = [lambda s: s['party'].reverse(),
                   lambda s: s['party'][0]['build']['essenceIds'].reverse(),
                   lambda s: s['party'][0]['build']['identityEssenceIds'].reverse(),
                   lambda s: s['party'][0]['build']['equipment'].reverse(),
                   lambda s: s['party'][0]['build']['identityEquipment'].clear(),
                   lambda s: s['assumptions'].append('changed')]
        for floor, args in self.fixtures.items():
            for index, change in enumerate(changes):
                with self.subTest(floor=floor, change=index):
                    proposal = copy.deepcopy(self.proposals[floor])
                    change(proposal['variants'][0]['cell']['scenario'])
                    with self.assertRaises(ValueError): p.validate(proposal, floor, *args)

    def test_missing_control_or_subset_and_duplicate_are_rejected(self):
        for floor, args in self.fixtures.items():
            for field in ('cells', 'variants'):
                for duplicate in (False, True):
                    proposal = copy.deepcopy(self.proposals[floor])
                    if duplicate: proposal[field].append(copy.deepcopy(proposal[field][0]))
                    else: proposal[field].pop(0)
                    with self.assertRaises(ValueError): p.validate(proposal, floor, *args)

    def test_specialization_claims_and_limits_cannot_change(self):
        for floor, args in self.fixtures.items():
            for field, value in [('defensePartySlots', [1, 2, 3]), ('specializedItems', 3), ('scenarioSha256', 'changed')]:
                proposal = copy.deepcopy(self.proposals[floor])
                proposal['variants'][0][field] = value
                with self.assertRaises(ValueError): p.validate(proposal, floor, *args)
            for field in ('maximumSpecializedItems', 'maximumSpecializedCharacters'):
                proposal = copy.deepcopy(self.proposals[floor])
                proposal['equipmentEligibility'][field] += 1
                with self.assertRaises(ValueError): p.validate(proposal, floor, *args)

    def test_unallocated_status_cannot_be_promoted(self):
        for floor, args in self.fixtures.items():
            for field, value in [('newFights', 1), ('newSeeds', 1), ('historicalReplays', 1), ('nativePreparations', 1),
                                 ('currentRuntimeQualified', True), ('studyAdmitted', True), ('usedForAcceptance', True),
                                 ('candidateSelected', True), ('status', 'Verified'), ('candidatePlan', {'offense': .5})]:
                proposal = copy.deepcopy(self.proposals[floor])
                proposal[field] = value
                with self.assertRaises(ValueError): p.validate(proposal, floor, *args)

    def test_donor_cannot_change_non_equipment_fields(self):
        for floor, (original, parents) in self.fixtures.items():
            for field, value in [('essenceIds', ['changed']*p.FLOORS[floor]['essences']), ('rank', 6),
                                 ('identityEquipment', []), ('attributeRollMultiplier', 2)]:
                changed = copy.deepcopy(original)
                changed[1]['scenario']['party'][0]['build'][field] = value
                with self.assertRaises(ValueError): p.propose(floor, changed, parents)

    def test_exact_donor_slots_and_raw_order_are_required(self):
        for floor, (original, parents) in self.fixtures.items():
            for mode in ('additional', 'missing', 'reordered', 'duplicate'):
                changed = copy.deepcopy(original)
                items = changed[1]['scenario']['party'][0]['build']['equipment']
                if mode == 'additional': items[0]['definitionId'] += '.spec.extra'
                elif mode == 'missing': items[1] = copy.deepcopy(changed[0]['scenario']['party'][0]['build']['equipment'][1])
                elif mode == 'reordered': items.reverse()
                else: items[-1]['slot'] = items[0]['slot']
                with self.assertRaises(ValueError): p.propose(floor, changed, parents)

    def test_progression_party_and_seed_budgets_are_fixed(self):
        for floor, (original, parents) in self.fixtures.items():
            for field, value in [('characterLevel', 99), ('tier', 3), ('essenceIds', ['wrong']*9)]:
                changed = copy.deepcopy(original)
                changed[-1]['scenario']['party'][0]['build'][field] = value
                with self.assertRaises(ValueError): p.propose(floor, changed, parents)
            for field, value in [('seeds', [123]), ('floorNumber', 12), ('party', [])]:
                changed = copy.deepcopy(original)
                changed[-1]['scenario'][field] = value
                with self.assertRaises(ValueError): p.propose(floor, changed, parents)

    def test_duplicate_parents_controls_and_wrong_defense_are_rejected(self):
        for floor, (original, parents) in self.fixtures.items():
            with self.assertRaises(ValueError): p.propose(floor, original, parents[:1]*2)
            changed = copy.deepcopy(original)
            changed[-1] = copy.deepcopy(changed[-2])
            with self.assertRaises(ValueError): p.propose(floor, changed, parents)
            changed = copy.deepcopy(original)
            changed[1]['gear'] = 'retained-health-regeneration'
            with self.assertRaises(ValueError): p.propose(floor, changed, parents)

    def test_parent_selection_uses_distinct_actual_compositions(self):
        rows = [dict(id=i, actualComposition=c, wins=w, meanGuardianHealth=hp)
                for i, c, w, hp in [('b', 'x', 5, 20), ('a', 'x', 5, 20), ('c', 'y', 3, 30), ('d', 'z', 3, 40)]]
        self.assertEqual([r['id'] for r in p.select_parents(rows)], ['a', 'c'])
        with self.assertRaises(ValueError): p.select_parents(rows[:2])

    def test_unrelated_floor_is_rejected(self):
        for floor in (12, 16, True, '13'):
            with self.assertRaises(ValueError): p.propose(floor, *self.fixtures[13])


if __name__ == '__main__':
    unittest.main()
