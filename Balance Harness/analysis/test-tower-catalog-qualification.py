"""Admission guards: applied provenance, unchanged target, exact recipes and runtime."""
import copy
import importlib.util
import itertools
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('qualification', Path(__file__).with_name('tower-catalog-qualification.py'))
q = importlib.util.module_from_spec(spec)
spec.loader.exec_module(q)
io = q.owner_module()


class QualificationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.source, self.api, self.tests, self.accepted, confirmed = [self.root/n for n in
            ('source', 'api', 'tests', 'accepted', 'confirmed')]
        def save(path, value):
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(json.dumps(value))
        self.save = save
        old = dict(version=1, floors=[dict(floorNumber=5, health=1), dict(floorNumber=6, health=2)])
        new = copy.deepcopy(old); new['floors'][0]['health'] = 3
        for base, tower, abilities in [(self.source/'content', old, 'old'), (confirmed/'content', new, 'accepted'), (self.api, new, 'accepted')]:
            save(base/'Data/world-tower/tower-floors.json', tower)
            save(base/'Data/combat/abilities.json', abilities)
            save(base/'Data/other.json', dict(unchanged=True))
        assemblies = {'Combat': 'runtime'}
        save(self.tests/'Combat.dll', assemblies)
        save(self.tests/'EssenceSystem.Tests.dll', 'tests')
        assembly_hashes = {'Combat': io.sha(self.tests/'Combat.dll')}
        for base in (self.source, confirmed):
            hashes = {p.relative_to(base/'content/Data').as_posix(): io.sha(p) for p in (base/'content/Data').rglob('*.json')}
            save(base/'scope.json', dict(settings={'fixed': 1}, contentHashes=hashes, execution={'assemblyHashes': assembly_hashes}))
            save(base/'cells.json', [{'scenario': {'floorNumber': 6}}])
            save(base/'result.json', dict(fights=256))
            save(base/'files.json', {p.relative_to(base).as_posix(): io.sha(p) for p in base.rglob('*.json')})
        save(self.accepted/'audit.json', dict(status='Verified', assessment={'verdict': 'Pass', 'familySize': 1},
             resultSha256=io.sha(confirmed/'result.json'), evaluationFights=256))
        save(self.accepted/'request.json', dict(source=str(confirmed), manifestPin=io.sha(confirmed/'files.json'),
             audit=str(self.accepted/'audit.json'), auditPin=io.sha(self.accepted/'audit.json')))
        save(self.accepted/'result.json', dict(status='AppliedInputsAndReplaysVerified', manifestPin=io.sha(confirmed/'files.json'),
             matchedInputs=256, fullReplays=1, newSeeds=0))
        save(self.accepted/'completion.json', dict(status='Verified', resultSha256=io.sha(self.accepted/'result.json'),
             matchedInputs=256, fullReplays=1, newSeeds=0))
        save(self.accepted/'process.json', dict(exitCode=0, timedOut=False, activeProcesses=0))
        self.plan = dict(version='tower-catalog-qualification-v1', source=str(self.source), sourceManifestSha256=io.sha(self.source/'files.json'),
             floor=6, acceptedApplication=str(self.accepted), receiptPins={str(self.accepted/n): io.sha(self.accepted/n) for n in
                ('request.json', 'result.json', 'process.json', 'completion.json')},
             sourceContentHashes=io.read(self.source/'scope.json')['contentHashes'],
             currentContentHashes=io.read(confirmed/'scope.json')['contentHashes'], assemblyHashes=assembly_hashes,
             testAssemblySha256=io.sha(self.tests/'EssenceSystem.Tests.dll'))

    def validate(self):
        q.validate_plan(self.plan, self.source, self.api, self.tests)

    def test_explicit_applied_catalog_and_other_floor_transition(self):
        self.validate()

    def test_receipt_tamper_is_rejected(self):
        self.save(self.accepted/'completion.json', {'status': 'Verified'})
        with self.assertRaisesRegex(ValueError, 'receipt changed'): self.validate()

    def test_incomplete_replays_rejected_even_when_receipt_rebound(self):
        path = self.accepted/'completion.json'; data = io.read(path); data['fullReplays'] = 0; self.save(path, data)
        self.plan['receiptPins'][str(path)] = io.sha(path)
        with self.assertRaisesRegex(ValueError, 'parity incomplete'): self.validate()

    def test_unapplied_candidate_cannot_replace_catalog(self):
        path = self.api/'Data/combat/abilities.json'; self.save(path, 'rejected')
        self.plan['currentContentHashes']['combat/abilities.json'] = io.sha(path)
        with self.assertRaisesRegex(ValueError, 'not the applied catalog'): self.validate()

    def test_undeclared_current_catalog_change(self):
        self.save(self.api/'Data/other.json', {'changed': True})
        with self.assertRaisesRegex(ValueError, 'Current catalog changed'): self.validate()

    def test_runtime_and_test_assembly_changes(self):
        for name in ('Combat', 'EssenceSystem.Tests'):
            with self.subTest(name=name):
                path = self.tests/(name+'.dll'); original = path.read_bytes(); path.write_bytes(b'changed')
                with self.assertRaisesRegex(ValueError, 'runtime changed|test assembly changed'): self.validate()
                path.write_bytes(original)

    def test_target_and_metadata_changes(self):
        old = {'version': 1, 'floors': [{'floorNumber': 6, 'health': 2}]}
        for new in ({'version': 2, 'floors': old['floors']}, {'version': 1, 'floors': [{'floorNumber': 6, 'health': 3}]}):
            with self.assertRaisesRegex(ValueError, 'metadata|Target floor'): q.validate_transition(old, new, 6)

    def reformat_tower(self, bind_receipt=True, change_other_floor=False):
        path = self.api/'Data/world-tower/tower-floors.json'
        value = io.read(path)
        if change_other_floor: value['floors'][0]['health'] += 1
        path.write_text(json.dumps(value, indent=4))
        self.plan['currentContentHashes']['world-tower/tower-floors.json'] = io.sha(path)
        if bind_receipt:
            receipt = self.accepted/'completion.json'; value = io.read(receipt)
            value['floorFileSha256'] = io.sha(path); self.save(receipt, value)
            self.plan['receiptPins'][str(receipt)] = io.sha(receipt)

    def test_receipt_bound_tower_formatting_is_admitted(self):
        self.reformat_tower()
        self.validate()

    def test_unreceipted_tower_formatting_is_rejected(self):
        self.reformat_tower(bind_receipt=False)
        with self.assertRaisesRegex(ValueError, 'not the applied catalog'): self.validate()

    def test_other_floor_gameplay_change_rejected_even_with_rebound_receipt(self):
        self.reformat_tower(change_other_floor=True)
        with self.assertRaisesRegex(ValueError, 'not the applied catalog'): self.validate()

    def test_formatting_exception_does_not_relax_current_byte_pin(self):
        self.reformat_tower()
        path = self.api/'Data/world-tower/tower-floors.json'
        path.write_text(json.dumps(io.read(path), indent=2))
        with self.assertRaisesRegex(ValueError, 'Current catalog changed'): self.validate()

    def test_other_catalog_formatting_still_requires_exact_accepted_bytes(self):
        path = self.api/'Data/other.json'; path.write_text(json.dumps(io.read(path), indent=4))
        self.plan['currentContentHashes']['other.json'] = io.sha(path)
        with self.assertRaisesRegex(ValueError, 'not the applied catalog'): self.validate()


class PartialFamilyTests(unittest.TestCase):
    def setUp(self):
        self.original, variants = [], []
        slots = ['MainHand', 'Chest', 'Head', 'Ring', 'Necklace', 'Relic']
        for parent in ('A', 'B'):
            base = dict(id=parent+'/baseline', composition=parent, gear='baseline', origin='saved', scenario=dict(
                id=parent, floorNumber=6, seeds=[], party=[dict(partySlot=2, build=dict(id=parent, essenceIds=[parent, 'raw-order'],
                equipment=[dict(slot=s, definition='baseline') for s in slots+['Legs']]))]))
            full = copy.deepcopy(base); full.update(id=parent+'/restorer', gear='restorer-specialization')
            for item in full['scenario']['party'][0]['build']['equipment'][:6]: item['definition'] = 'restorer'
            self.original.extend([base, full])
            for count in (2, 4):
                for selected in itertools.combinations(slots, count):
                    cell = copy.deepcopy(full)
                    for item in cell['scenario']['party'][0]['build']['equipment']:
                        if item['slot'] not in selected: item['definition'] = 'baseline'
                    digest = q.signature(cell['scenario'])
                    cell.update(id='partial-restoration/'+digest, gear='partial-restoration-'+str(count)+'-'+'-'.join(selected).lower(), origin='proposed-partial-healer-gear')
                    variants.append(dict(cell=cell, sourceId=full['id'], restorationSlots=list(selected), scenarioSha256=digest))
        for index in range(34):
            cell = copy.deepcopy(self.original[0]); cell['id'] = str(index); cell['scenario']['id'] = str(index); cell['composition'] = str(index)
            self.original.append(cell)
        self.proposal = dict(version='floor6-partial-restoration-proposal-v1', floor=6, status='ProposedNotAdmitted',
            newSeeds=0, retainedCells=38, newVariants=60, totalCells=98, cells=self.original+[v['cell'] for v in variants], variants=variants)

    def test_exact_full_family_admitted(self):
        self.assertEqual(98, len(q.validate_partial_family(self.proposal, self.original)))

    def test_raw_essence_order_identity_or_budget_change_rejected(self):
        for field, value in [('essenceIds', ['raw-order', 'A']), ('id', 'changed'), ('characterLevel', 99)]:
            proposal = copy.deepcopy(self.proposal)
            proposal['variants'][0]['cell']['scenario']['party'][0]['build'][field] = value
            with self.assertRaisesRegex(ValueError, 'Partial recipe changed'): q.validate_partial_family(proposal, self.original)

    def test_missing_subset_rejected(self):
        self.proposal['variants'].pop()
        with self.assertRaisesRegex(ValueError, 'subset'): q.validate_partial_family(self.proposal, self.original)

    def test_original_family_cannot_be_dropped_or_changed(self):
        self.proposal['cells'] = copy.deepcopy(self.proposal['cells']); self.proposal['cells'][0]['id'] = 'changed'
        with self.assertRaisesRegex(ValueError, 'retained exactly'): q.validate_partial_family(self.proposal, self.original)


class MixedArmorFamilyTests(unittest.TestCase):
    floor = 2
    defense = 'armor'
    gear = 'armor-and-health'
    retained = 38
    party_size = 5
    subset_counts = (1, 2, 3, 4)
    specialized_id = 'specialized'
    def setUp(self):
        self.original, variants = [], []
        for parent in ('A', 'B'):
            base = dict(id=parent+'/baseline', composition=parent, gear='baseline', origin='saved', scenario=dict(
                id=parent, floorNumber=self.floor, seeds=[], party=[dict(partySlot=slot, build=dict(id=parent+str(slot),
                    characterLevel=30, essenceIds=[parent, 'raw-order'], identityEssenceIds=['identity'],
                    equipment=[dict(slot=s, definitionId='baseline') for s in
                               ['MainHand', 'Chest', 'Head', 'Legs', 'Ring', 'Necklace', 'Relic']])) for slot in range(1,self.party_size+1)]))
            full = copy.deepcopy(base); full.update(id=parent+'/'+self.gear, gear=self.gear)
            for member in full['scenario']['party']:
                for item in member['build']['equipment']:
                    if item['slot'] in ('Chest','Head','Legs','Necklace'): item['definitionId'] = self.specialized_id
            self.original.extend([base, full])
            for count in self.subset_counts:
                for chosen in itertools.combinations(range(1,self.party_size+1),count):
                    cell = copy.deepcopy(base)
                    for member, armored in zip(cell['scenario']['party'],full['scenario']['party']):
                        if member['partySlot'] in chosen: member['build']['equipment'] = copy.deepcopy(armored['build']['equipment'])
                    digest = q.signature(cell['scenario'])
                    cell.update(id='mixed-'+self.defense+'/'+digest, gear='mixed-'+self.defense+'-baseline-slots-'+'-'.join(map(str,chosen)),
                                origin='proposed-mixed-'+self.defense+'-gear')
                    variants.append(dict(cell=cell, sourceId=full['id'], baselineId=base['id'],
                                         **{self.defense+'PartySlots':list(chosen)}, scenarioSha256=digest))
        for index in range(self.retained-4):
            cell = copy.deepcopy(self.original[0]); cell['id'] = str(index); cell['scenario']['id'] = str(index)
            cell['composition'] = str(index)
            self.original.append(cell)
        self.proposal = dict(version=f'floor{self.floor}-mixed-{self.defense}-proposal-v1', floor=self.floor, status='ProposedNotAdmitted',
            newSeeds=0, retainedCells=self.retained, newVariants=len(variants), totalCells=self.retained+len(variants), cells=self.original+[v['cell'] for v in variants], variants=variants)

    def test_complete_mixed_family_dispatched(self):
        self.assertEqual(self.proposal['totalCells'],len(q.validate_family(self.proposal,self.original)))

    def test_unknown_version_rejected(self):
        self.proposal['version'] = 'unapproved'
        with self.assertRaisesRegex(ValueError,'Unknown family'): q.validate_family(self.proposal,self.original)

    def test_qualified_cli_rejects_unsupported_modes_before_allocation(self):
        for changed in (['--floor','3'], ['--mode','confirm'], ['--health-factor','1.1'],
                        ['--offense-factor','1.1'], ['--add-references','extra']):
            args = ['owner','--mode','prepare','--name','guard-test','--source','source',
                    '--artifacts','artifacts','--floor',str(self.floor),'--qualified-family','admission',*changed]
            with self.subTest(changed=changed), patch('sys.argv',args):
                with self.assertRaisesRegex(ValueError,'Qualified family requires'): io.main()

    def test_wrong_floor_or_preallocated_seeds_rejected(self):
        for field,value in [('floor',6),('newSeeds',1)]:
            proposal = copy.deepcopy(self.proposal); proposal[field] = value
            with self.assertRaisesRegex(ValueError,'seed-free mixed'): q.validate_family(proposal,self.original)

    def test_raw_identity_order_budget_or_unselected_equipment_change_rejected(self):
        changes = [('essenceIds',['raw-order','A']),('id','changed'),('characterLevel',31),
                   ('identityEssenceIds',['changed']),('equipment',[])]
        for field,value in changes:
            with self.subTest(field=field):
                proposal = copy.deepcopy(self.proposal)
                proposal['variants'][0]['cell']['scenario']['party'][1]['build'][field] = value
                with self.assertRaisesRegex(ValueError,'Mixed recipe changed'): q.validate_family(proposal,self.original)

    def test_missing_or_duplicated_subset_rejected(self):
        for duplicate in (False,True):
            proposal = copy.deepcopy(self.proposal)
            if duplicate: proposal['variants'][1] = copy.deepcopy(proposal['variants'][0])
            else: proposal['variants'].pop()
            with self.assertRaisesRegex(ValueError,'subset'): q.validate_family(proposal,self.original)

    def test_wrong_parent_reference_rejected(self):
        self.proposal['variants'][0]['baselineId'] = 'B/baseline'
        with self.assertRaisesRegex(ValueError,'Mixed recipe changed'): q.validate_family(self.proposal,self.original)

    def test_changed_original_or_final_family_rejected(self):
        for index in (0,self.retained+59):
            proposal = copy.deepcopy(self.proposal); proposal['cells'] = copy.deepcopy(proposal['cells'])
            proposal['cells'][index]['id'] = 'changed'
            with self.assertRaisesRegex(ValueError,'retained exactly|complete family'): q.validate_family(proposal,self.original)

    def test_same_composition_with_different_labels_is_not_two_parents(self):
        for cell in self.original[2:4]:
            for member in cell['scenario']['party']: member['build']['essenceIds'] = ['raw-order','A']
        with self.assertRaisesRegex(ValueError,'Two actual parent'): q.validate_family(self.proposal,self.original)

    def test_parent_changes_outside_declared_equipment_rejected(self):
        for change in ('identity','equipment'):
            original = copy.deepcopy(self.original)
            if change == 'identity': original[1]['scenario']['party'][0]['build']['id'] = 'changed'
            else: original[1]['scenario']['party'][0]['build']['equipment'][0]['definitionId'] = 'changed'
            proposal = copy.deepcopy(self.proposal); proposal['cells'][:self.retained] = original
            with self.assertRaisesRegex(ValueError,'Parent identities|four saved'): q.validate_family(proposal,original)


class Floor4MixedArmorFamilyTests(MixedArmorFamilyTests):
    floor = 4

    def test_version_cannot_admit_different_floor(self):
        self.proposal['floor'] = 2
        with self.assertRaisesRegex(ValueError, 'seed-free mixed'): q.validate_family(self.proposal, self.original)


class Floor7MixedResistanceFamilyTests(MixedArmorFamilyTests):
    floor = 7
    defense = 'resistance'
    gear = 'resistance-and-health'
    retained = 60

    def test_original_expanded_family_cannot_shrink_to_38(self):
        self.proposal['retainedCells'] = 38
        self.proposal['totalCells'] = 98
        with self.assertRaisesRegex(ValueError, 'retained exactly'): q.validate_family(self.proposal, self.original)

    def test_armor_parent_cannot_substitute_resistance_parent(self):
        self.original[1]['gear'] = 'armor-and-health'
        with self.assertRaisesRegex(ValueError, 'Full resistance parent'): q.validate_family(self.proposal, self.original)

    def test_version_cannot_admit_different_floor(self):
        self.proposal['floor'] = 4
        with self.assertRaisesRegex(ValueError, 'seed-free mixed'): q.validate_family(self.proposal, self.original)


class Floor8LimitedArmorFamilyTests(MixedArmorFamilyTests):
    floor = 8
    retained = 67
    party_size = 10
    subset_counts = (1, 2)
    specialized_id = 'gear.spec.armor'

    def setUp(self):
        super().setUp()
        self.proposal.update(version='floor8-limited-armor-proposal-v1', maximumSpecializedItems=8,
                             maximumSpecializedCharacters=2)

    def test_complete_177_family_and_late_party_slots(self):
        cells = q.validate_family(self.proposal, self.original)
        self.assertEqual(177, len(cells))
        self.assertEqual(2, sum(v['armorPartySlots'] == [9,10] for v in self.proposal['variants']))

    def test_three_character_subset_rejected(self):
        self.proposal['variants'][0]['armorPartySlots'] = [1,2,3]
        with self.assertRaisesRegex(ValueError, 'subset'): q.validate_family(self.proposal, self.original)

    def test_budget_cannot_expand(self):
        for field in ('maximumSpecializedItems', 'maximumSpecializedCharacters'):
            proposal = copy.deepcopy(self.proposal); proposal[field] += 1
            with self.assertRaisesRegex(ValueError, 'budget changed'): q.validate_family(proposal, self.original)

    def test_undeclared_specialization_in_baseline_rejected(self):
        # Even a consistently reconstructed family cannot hide specialization in the baseline.
        for cell in self.original + [v['cell'] for v in self.proposal['variants']]:
            cell['scenario']['party'][0]['build']['equipment'][0]['definitionId'] = 'gear.spec.hidden'
        for variant in self.proposal['variants']:
            digest=q.signature(variant['cell']['scenario'])
            variant['scenarioSha256']=digest; variant['cell']['id']='mixed-armor/'+digest
        with self.assertRaisesRegex(ValueError, 'actual specialized items'): q.validate_family(self.proposal, self.original)


class Floor9LimitedResistanceFamilyTests(MixedArmorFamilyTests):
    floor = 9
    defense = 'resistance'
    gear = 'resistance-and-health'
    retained = 38
    party_size = 10
    subset_counts = (1, 2)
    specialized_id = 'gear.spec.resistance'

    def setUp(self):
        super().setUp()
        self.proposal.update(version='floor9-limited-resistance-proposal-v1', maximumSpecializedItems=8,
                             maximumSpecializedCharacters=2)

    def test_complete_148_family_and_late_party_slots(self):
        self.assertEqual(148, len(q.validate_family(self.proposal, self.original)))
        self.assertEqual(2, sum(v['resistancePartySlots'] == [9,10] for v in self.proposal['variants']))

    def test_three_character_subset_rejected(self):
        self.proposal['variants'][0]['resistancePartySlots'] = [1,2,3]
        with self.assertRaisesRegex(ValueError, 'subset'): q.validate_family(self.proposal, self.original)

    def test_budget_cannot_expand(self):
        for field in ('maximumSpecializedItems', 'maximumSpecializedCharacters'):
            proposal = copy.deepcopy(self.proposal); proposal[field] += 1
            with self.assertRaisesRegex(ValueError, 'budget changed'): q.validate_family(proposal, self.original)

    def test_undeclared_specialization_in_baseline_rejected(self):
        for cell in self.original + [v['cell'] for v in self.proposal['variants']]:
            cell['scenario']['party'][0]['build']['equipment'][0]['definitionId'] = 'gear.spec.hidden'
        for variant in self.proposal['variants']:
            digest=q.signature(variant['cell']['scenario'])
            variant['scenarioSha256']=digest; variant['cell']['id']='mixed-resistance/'+digest
        with self.assertRaisesRegex(ValueError, 'actual specialized items'): q.validate_family(self.proposal, self.original)

    def test_armor_parent_cannot_substitute_resistance_parent(self):
        self.original[1]['gear'] = 'armor-and-health'
        with self.assertRaisesRegex(ValueError, 'Full resistance parent'): q.validate_family(self.proposal, self.original)

    def test_floor_eight_contract_cannot_substitute_floor_nine(self):
        self.proposal['version'] = 'floor8-limited-armor-proposal-v1'
        with self.assertRaisesRegex(ValueError, 'seed-free mixed'): q.validate_family(self.proposal, self.original)


class Floor10LimitedArmorFamilyTests(Floor8LimitedArmorFamilyTests):
    floor = 10
    retained = 38
    party_size = 15

    def setUp(self):
        super().setUp()
        self.proposal['version'] = 'floor10-limited-armor-proposal-v1'

    def test_complete_177_family_and_late_party_slots(self):
        self.assertEqual(278, len(q.validate_family(self.proposal, self.original)))
        self.assertEqual(2, sum(v['armorPartySlots'] == [14,15] for v in self.proposal['variants']))

    def test_floor_nine_contract_cannot_admit_floor_ten(self):
        self.proposal['version'] = 'floor9-limited-resistance-proposal-v1'
        with self.assertRaisesRegex(ValueError, 'seed-free mixed'): q.validate_family(self.proposal, self.original)


class SummonAcceptanceChainTests(unittest.TestCase):
    def setUp(self):
        self.parent = dict(acceptedAggregate=dict(version='applied-tower-kodoku-midpoint-aggregate-v1'), receiptPins={})
        self.plan = dict(acceptedAggregate=dict(version='applied-tower-ni-restoration-aggregate-v1'), acceptedSummonsAggregate=self.parent)

    def test_explicit_parent_selected_and_old_direct_proof_unchanged(self):
        self.assertIs(self.parent, q.summons_acceptance(self.plan))
        self.assertIs(self.parent, q.summons_acceptance(self.parent))

    def test_wrong_parent_outer_and_unbound_parent_rejected(self):
        for mutation in ('outer', 'parent', 'pins', 'extra'):
            with self.subTest(mutation=mutation):
                plan = copy.deepcopy(self.plan)
                if mutation == 'outer': plan['acceptedAggregate']['version'] = 'unaccepted'
                if mutation == 'parent': plan['acceptedSummonsAggregate']['acceptedAggregate']['version'] = 'unaccepted'
                if mutation == 'pins': del plan['acceptedSummonsAggregate']['receiptPins']
                if mutation == 'extra': plan['acceptedSummonsAggregate']['extra'] = True
                with self.assertRaisesRegex(ValueError, 'acceptance chain'): q.summons_acceptance(plan)


class MadKingSummonAcceptanceChainTests(SummonAcceptanceChainTests):
    def setUp(self):
        super().setUp()
        self.plan['acceptedAggregate']['version'] = 'applied-tower-mad-king-acceptance-aggregate-v1'


if __name__ == '__main__':
    unittest.main()
