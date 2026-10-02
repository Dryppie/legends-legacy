"""Floor-twelve proposal guards: complete controls, exact budgets and raw order."""
import copy
import importlib.util
import itertools
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('floor12_resistance', Path(__file__).with_name('tower-floor12-limited-resistance.py'))
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)


def cell(name, composition, specialized):
    party = []
    for slot in range(1, 11):
        equipment = [dict(slot=s, definitionId=('defense.spec.resistance-health' if specialized and s in p.SLOTS else 'plain')+'.'+s,
                          activeStyleId=None, useNativeStyle=False)
                     for s in ('MainHand', 'Chest', 'Head', 'Legs', 'Ring', 'Necklace', 'Relic')]
        essence_ids = [composition+'-'+str(i) for i in (6, 0, 5, 1, 4, 2, 3)]
        party.append(dict(partySlot=slot, build=dict(id=name+'-'+str(slot), characterLevel=60, tier=2, rank=5,
            quality='Masterpiece', attributeRollMultiplier=1, essenceIds=essence_ids,
            identityEssenceIds=list(reversed(essence_ids)), identityEquipment=[dict(id='original-identity')], equipment=equipment)))
    return dict(id=name, composition=name, gear='retained-resistance-and-health' if specialized else 'retained-baseline',
                origin='retained-control', scenario=dict(id=name, floorNumber=12, seeds=[], party=party, assumptions=['unchanged']))


class Floor12ProposalTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original = []
        cls.parents = []
        for name in ('a', 'b'):
            baseline = cell(name, name, False)
            full = copy.deepcopy(baseline)
            full.update(id=name+'-full', gear='retained-resistance-and-health')
            donor = cell(name, name, True)
            for member, supplied in zip(full['scenario']['party'], donor['scenario']['party'], strict=True):
                member['build']['equipment'] = supplied['build']['equipment']
            cls.original.extend([baseline, full])
            cls.parents.append(full['id'])
        for i in range(127):
            cls.original.append(cell('control-'+str(i), 'other-'+str(i % 7), i >= 16))
        cls.proposal = p.propose(cls.original, cls.parents)

    def test_every_one_and_two_character_subset_is_present(self):
        result = p.validate(self.proposal, self.original, self.parents)
        self.assertEqual(result[:131], self.original)
        self.assertEqual((len(result), self.proposal['eligibleRecipes']), (241, 128))
        expected = {s for n in (1, 2) for s in itertools.combinations(range(1, 11), n)}
        for parent in self.parents:
            variants = [v for v in self.proposal['variants'] if v['sourceId'] == parent]
            self.assertEqual(len(variants), 55)
            self.assertEqual({tuple(v['resistancePartySlots']) for v in variants}, expected)

    def test_derivation_does_not_mutate_or_alias_original(self):
        original = copy.deepcopy(self.original)
        result = p.propose(original, self.parents)
        result['cells'][0]['scenario']['party'][0]['build']['essenceIds'].reverse()
        result['variants'][0]['cell']['scenario']['party'][0]['build']['equipment'].clear()
        self.assertEqual(original, self.original)

    def test_raw_essence_identity_party_and_item_order_cannot_change(self):
        changes = [
            lambda s: s['party'][0]['build']['essenceIds'].reverse(),
            lambda s: s['party'][0]['build']['identityEssenceIds'].reverse(),
            lambda s: s['party'][0]['build']['identityEquipment'].append({'id': 'changed'}),
            lambda s: s['party'][0]['build']['equipment'].reverse(),
            lambda s: s['party'].reverse(),
            lambda s: s['assumptions'].append('changed'),
        ]
        for change in changes:
            with self.subTest(change=changes.index(change)):
                proposal = copy.deepcopy(self.proposal)
                change(proposal['variants'][0]['cell']['scenario'])
                with self.assertRaisesRegex(ValueError, 'raw recipes'):
                    p.validate(proposal, self.original, self.parents)

    def test_missing_control_subset_and_extra_variant_are_rejected(self):
        for field, index in [('cells', 0), ('variants', 0)]:
            proposal = copy.deepcopy(self.proposal)
            proposal[field].pop(index)
            with self.assertRaises(ValueError): p.validate(proposal, self.original, self.parents)
        proposal = copy.deepcopy(self.proposal)
        proposal['variants'].append(copy.deepcopy(proposal['variants'][0]))
        with self.assertRaises(ValueError): p.validate(proposal, self.original, self.parents)

    def test_subset_and_specialized_item_claims_cannot_be_relabelled(self):
        for field, value in [('resistancePartySlots', [1, 2, 3]), ('specializedItems', 3), ('scenarioSha256', 'changed')]:
            proposal = copy.deepcopy(self.proposal)
            proposal['variants'][0][field] = value
            with self.assertRaises(ValueError): p.validate(proposal, self.original, self.parents)

    def test_unallocated_status_cannot_be_promoted(self):
        for field, value in [('newSeeds', 1), ('newFights', 1), ('nativePreparations', 1),
                             ('currentRuntimeQualified', True), ('usedForAcceptance', True), ('status', 'Verified'),
                             ('candidatePlan', {'offense': .5})]:
            proposal = copy.deepcopy(self.proposal)
            proposal[field] = value
            with self.assertRaises(ValueError): p.validate(proposal, self.original, self.parents)

    def test_equipment_limit_cannot_be_relaxed(self):
        for field in ('maximumSpecializedItems', 'maximumSpecializedCharacters'):
            proposal = copy.deepcopy(self.proposal)
            proposal['equipmentEligibility'][field] += 1
            with self.assertRaises(ValueError): p.validate(proposal, self.original, self.parents)

    def test_donor_cannot_change_fields_outside_equipment(self):
        for field, value in [('essenceIds', ['changed']*7), ('identityEquipment', []), ('rank', 6)]:
            original = copy.deepcopy(self.original)
            original[1]['scenario']['party'][0]['build'][field] = value
            with self.assertRaises(ValueError): p.propose(original, self.parents)

    def test_donor_requires_exact_four_equipment_slots(self):
        for mode in ('additional', 'missing', 'reordered'):
            original = copy.deepcopy(self.original)
            items = original[1]['scenario']['party'][0]['build']['equipment']
            if mode == 'additional': items[0]['definitionId'] += '.spec.extra'
            if mode == 'missing': items[1] = copy.deepcopy(original[0]['scenario']['party'][0]['build']['equipment'][1])
            if mode == 'reordered': items.reverse()
            with self.assertRaises(ValueError): p.propose(original, self.parents)

    def test_progression_and_seed_changes_are_rejected(self):
        for field, value in [('characterLevel', 65), ('tier', 3), ('essenceIds', ['wrong']*8)]:
            original = copy.deepcopy(self.original)
            original[-1]['scenario']['party'][0]['build'][field] = value
            with self.assertRaises(ValueError): p.propose(original, self.parents)
        original = copy.deepcopy(self.original)
        original[-1]['scenario']['seeds'] = [123]
        with self.assertRaises(ValueError): p.propose(original, self.parents)

    def test_duplicate_parent_and_controls_are_rejected(self):
        with self.assertRaises(ValueError): p.propose(self.original, self.parents[:1]*2)
        original = copy.deepcopy(self.original)
        original[-1] = copy.deepcopy(original[-2])
        with self.assertRaises(ValueError): p.propose(original, self.parents)


if __name__ == '__main__':
    unittest.main()
