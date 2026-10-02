"""Reject altered progression, raw ordering, controls and healer equipment subsets."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('floor12_restoration', Path(__file__).with_name('tower-floor12-limited-restoration.py'))
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)


def baseline(name, composition):
    party = []
    for slot in range(1, 11):
        essences = [composition+'-'+str(i) for i in (6, 0, 5, 1, 4, 2, 3)]
        items = [dict(slot=s, definitionId='plain.'+s+'.rarity.legendary', activeStyleId=None, useNativeStyle=False)
                 for s in ('MainHand', 'Chest', 'Head', 'Legs', 'Ring', 'Necklace', 'Relic')]
        party.append(dict(partySlot=slot, build=dict(id=name+'-'+str(slot), characterLevel=60, tier=2, rank=5,
            quality='Masterpiece', attributeRollMultiplier=1, essenceIds=essences,
            identityEssenceIds=list(reversed(essences)), identityEquipment=[dict(id='keep-identity')], equipment=items)))
    return dict(id=name, composition=name, gear='retained-baseline', origin='retained-control',
                scenario=dict(id=name, floorNumber=12, seeds=[], party=party, assumptions=['unchanged']))


class Floor12RestorationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original = []
        for i in range(9):
            base = baseline('base-'+str(i), 'composition-'+str(i))
            full = copy.deepcopy(base)
            full.update(id='full-'+str(i), gear='retained-restorer-specialization')
            for member in full['scenario']['party']:
                if member['partySlot'] in (2, 7):
                    for item in member['build']['equipment']:
                        if item['slot'] in p.h.SLOTS:
                            item['definitionId'] = item['definitionId'].replace('.rarity.', '.spec.restoration.rarity.')
            cls.original.extend([base, full])
        for i in range(2):
            projected = copy.deepcopy(cls.original[2*i+1])
            projected.update(id='projected-reference/'+str(i), composition='projection-'+str(i))
            projected['scenario']['id'] = 'projection-'+str(i)
            cls.original.append(projected)
        for i in range(221):
            control = baseline('control-'+str(i), 'composition-'+str(i % 9))
            control['gear'] = 'retained-other-control'
            if i >= 119:
                for member in control['scenario']['party']:
                    member['build']['equipment'][1]['definitionId'] += '.spec.resistance'
            cls.original.append(control)
        cls.proposal = p.propose(cls.original)

    def test_complete_family_and_all_three_subsets_for_every_parent(self):
        result = p.validate(self.proposal, self.original)
        self.assertEqual(result[:241], self.original)
        self.assertEqual((len(result), self.proposal['eligibleRecipes']), (268, 155))
        for parent in ('full-'+str(i) for i in range(9)):
            variants = [v for v in self.proposal['variants'] if v['sourceId'] == parent]
            self.assertEqual([(v['healerPartySlots'], v['specializedItems']) for v in variants],
                             [([2], 6), ([7], 6), ([2, 7], 8)])

    def test_originals_not_mutated_or_aliased(self):
        original = copy.deepcopy(self.original)
        proposed = p.propose(original)
        proposed['cells'][0]['scenario']['party'].reverse()
        proposed['variants'][0]['cell']['scenario']['party'][0]['build']['equipment'].clear()
        self.assertEqual(original, self.original)

    def test_projected_controls_retained_without_counting_new_compositions(self):
        projected = [c for c in self.proposal['cells'] if c['id'].startswith('projected-reference/')]
        self.assertEqual(len(projected), 2)
        self.assertEqual(len({p.h.composition(c) for c in self.proposal['cells']}), 9)
        self.assertFalse(any(v['sourceId'].startswith('projected-reference/') for v in self.proposal['variants']))

    def test_only_exact_donor_items_change(self):
        lookup = {c['id']: c for c in self.original}
        for variant in self.proposal['variants']:
            restored = copy.deepcopy(variant['cell']['scenario'])
            base = lookup[variant['baselineId']]['scenario']
            donor = lookup[variant['sourceId']]['scenario']
            changes = []
            for member, ordinary, full in zip(restored['party'], base['party'], donor['party'], strict=True):
                for i, (item, plain) in enumerate(zip(member['build']['equipment'], ordinary['build']['equipment'], strict=True)):
                    if item != plain:
                        changes.append((member['partySlot'], item['slot']))
                        self.assertEqual(item, full['build']['equipment'][i])
                member['build']['equipment'] = copy.deepcopy(ordinary['build']['equipment'])
            expected = {(slot, item) for slot in variant['healerPartySlots']
                        for item in (p.FOUR_SLOTS if variant['equipmentSlots'] else p.h.SLOTS)}
            self.assertEqual(set(changes), expected)
            self.assertEqual(len(changes), variant['specializedItems'])
            self.assertEqual(restored, base)

    def test_raw_order_identity_and_metadata_cannot_change(self):
        for change in [lambda s: s['party'].reverse(),
                       lambda s: s['party'][1]['build']['essenceIds'].reverse(),
                       lambda s: s['party'][1]['build']['identityEssenceIds'].reverse(),
                       lambda s: s['party'][1]['build']['identityEquipment'].clear(),
                       lambda s: s['party'][1]['build']['equipment'].reverse(),
                       lambda s: s['assumptions'].append('changed')]:
            proposal = copy.deepcopy(self.proposal)
            change(proposal['variants'][0]['cell']['scenario'])
            with self.assertRaises(ValueError): p.validate(proposal, self.original)

    def test_unprepared_proposal_cannot_claim_admission_or_acceptance(self):
        for field, value in [('newSeeds', 1), ('newFights', 1), ('nativePreparations', 1),
                             ('currentRuntimeQualified', True), ('studyAdmitted', True), ('usedForAcceptance', True),
                             ('status', 'Verified'), ('candidatePlan', {'offense': .55})]:
            proposal = copy.deepcopy(self.proposal)
            proposal[field] = value
            with self.assertRaises(ValueError): p.validate(proposal, self.original)

    def test_subset_and_item_claims_cannot_be_relabelled(self):
        for field, value in [('healerPartySlots', [2, 3]), ('equipmentSlots', ['Ring']),
                             ('specializedItems', 5), ('scenarioSha256', 'changed')]:
            proposal = copy.deepcopy(self.proposal)
            proposal['variants'][0][field] = value
            with self.assertRaises(ValueError): p.validate(proposal, self.original)

    def test_equipment_limits_cannot_be_relaxed(self):
        for field in ('maximumSpecializedItems', 'maximumSpecializedCharacters'):
            proposal = copy.deepcopy(self.proposal)
            proposal['equipmentEligibility'][field] += 1
            with self.assertRaises(ValueError): p.validate(proposal, self.original)

    def test_progression_floor_and_seeds_cannot_change(self):
        for field, value in [('characterLevel', 70), ('tier', 3), ('essenceIds', ['wrong']*8)]:
            original = copy.deepcopy(self.original)
            original[-1]['scenario']['party'][0]['build'][field] = value
            with self.assertRaises(ValueError): p.propose(original)
        for field, value in [('seeds', [1]), ('floorNumber', 13)]:
            original = copy.deepcopy(self.original)
            original[-1]['scenario'][field] = value
            with self.assertRaises(ValueError): p.propose(original)

    def test_donor_cannot_change_non_equipment_fields(self):
        for field, value in [('rank', 6), ('essenceIds', ['changed']*7), ('identityEquipment', [])]:
            original = copy.deepcopy(self.original)
            original[1]['scenario']['party'][1]['build'][field] = value
            with self.assertRaises(ValueError): p.propose(original)

    def test_donor_equipment_must_match_original_slots_type_and_order(self):
        for change in [lambda items: items.reverse(),
                       lambda items: items[0].__setitem__('activeStyleId', 'changed'),
                       lambda items: items[0].__setitem__('definitionId', 'plain.staff.spec.power.rarity.legendary'),
                       lambda items: items[3].__setitem__('definitionId', 'plain.legs.spec.restoration.rarity.legendary')]:
            original = copy.deepcopy(self.original)
            change(original[1]['scenario']['party'][1]['build']['equipment'])
            with self.assertRaises(ValueError): p.propose(original)

    def test_missing_duplicate_controls_and_ambiguous_parent_are_rejected(self):
        with self.assertRaises(ValueError): p.propose(self.original[:-1])
        original = copy.deepcopy(self.original)
        original[-1] = copy.deepcopy(original[-2])
        with self.assertRaises(ValueError): p.propose(original)
        original = copy.deepcopy(self.original)
        original[-1].update(composition=original[0]['composition'], gear='retained-baseline')
        with self.assertRaises(ValueError): p.propose(original)
        proposal = copy.deepcopy(self.proposal)
        proposal['cells'].pop(0)
        with self.assertRaises(ValueError): p.validate(proposal, self.original)


if __name__ == '__main__':
    unittest.main()
