"""Guards for importing historical search recipes without losing identity or coverage."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('tower_pass', Path(__file__).with_name('run-tower-balance-pass.py'))
owner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(owner)


def cell(identity, origin='retained-reference', floor=1):
    return dict(id=identity, composition=identity, gear='baseline', origin=origin,
                scenario=dict(id='scenario', floorNumber=floor, seeds=[], party=[dict(partySlot=1,
                    build=dict(id=identity, essenceIds=['essence.a'], identityEssenceIds=['neutral']))]))


class CoverageTests(unittest.TestCase):
    def test_same_essences_with_different_native_identities_are_retained(self):
        cells = [cell('original')]
        before = copy.deepcopy(cells)
        references = [cell(str(i)) for i in range(3)]
        references.append(cell('finalist', 'generated-finalist'))
        provenance = owner.append_references(cells, references, 'search')
        self.assertEqual(cells[:1], before)
        self.assertEqual([c['scenario'] for c in cells[1:]], [c['scenario'] for c in references[:3]])
        self.assertEqual(len(cells), 4)
        self.assertTrue(all(p['added'] for p in provenance))

    def test_duplicate_search_import_keeps_one_recipe_and_all_provenance(self):
        cells = [cell('original')]
        references = [cell(str(i)) for i in range(3)]
        first = owner.append_references(cells, references, 'search-a')
        second = owner.append_references(cells, references, 'search-b')
        self.assertEqual(len(cells), 4)
        self.assertEqual([p['cellId'] for p in first], [p['cellId'] for p in second])
        self.assertTrue(all(not p['added'] and p['source'] == 'search-b' for p in second))

    def test_partial_reference_family_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'all three'):
            owner.append_references([cell('original')], [cell('reference')], 'search')

    def test_wrong_floor_or_seeded_recipe_is_rejected(self):
        for wrong in [cell('bad', floor=2), cell('bad')]:
            if wrong['scenario']['floorNumber'] == 1:
                wrong['scenario']['seeds'] = [123]
            with self.assertRaisesRegex(ValueError, 'seed-free'):
                owner.append_references([cell('original')], [wrong, cell('b'), cell('c')], 'search')

    def test_unknown_origin_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'Unknown'):
            owner.append_references([cell('original')], [cell(str(i)) for i in range(3)] + [cell('bad', 'unknown')], 'search')

    def test_refresh_allows_other_floors_but_rejects_target_or_catalog_changes(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source, api = root / 'source', root / 'api'
            old = dict(schema=1, floors=[dict(floorNumber=1, health=2), dict(floorNumber=2, health=3)])
            for base in [source / 'content', api]:
                (base / 'Data/world-tower').mkdir(parents=True)
                (base / 'Data/catalog.json').write_text('{}')
                (base / 'Data/world-tower/tower-floors.json').write_text(json.dumps(old))
            current = api / 'Data/world-tower/tower-floors.json'
            new = copy.deepcopy(old)
            new['floors'][1]['health'] = 4
            current.write_text(json.dumps(new))
            owner.validate_current_content(source, api, 1)
            new['floors'][0]['health'] = 5
            current.write_text(json.dumps(new))
            with self.assertRaisesRegex(ValueError, 'Target floor'):
                owner.validate_current_content(source, api, 1)
            current.write_text(json.dumps(old))
            (api / 'Data/catalog.json').write_text('{"changed":true}')
            with self.assertRaisesRegex(ValueError, 'catalog differs'):
                owner.validate_current_content(source, api, 1)


class SearchChallengeTests(unittest.TestCase):
    @staticmethod
    def recipe(identity, essence, gear='baseline', origin='retained-reference'):
        value = cell(identity, origin)
        value['gear'] = gear
        value['scenario']['party'][0]['build'].update(essenceIds=[essence], characterLevel=40, tier=1,
            attributeRollMultiplier=1, equipment=[{'definitionId': gear}], rank=3, quality='Fine')
        return value

    @staticmethod
    def scores(cells):
        return [dict(id=c['id'], gear=c['gear'], wins=100-i, meanGuardianHealth=i) for i,c in enumerate(cells)]

    def test_search_selects_three_actual_compositions_without_mutating_duplicates(self):
        cells = [self.recipe('a','x'), self.recipe('a-copy','x'), self.recipe('b','y'), self.recipe('c','z')]
        cells[1]['scenario']['party'][0]['build']['identityEssenceIds'] = ['different']
        before = copy.deepcopy(cells)
        selected, benchmark = owner.select_search_references(cells, self.scores(cells))
        self.assertEqual([c['id'] for c in selected], ['a','b','c'])
        self.assertEqual(benchmark, 1)
        self.assertEqual(cells, before)

    def test_explicit_gear_chooses_its_own_best_reference(self):
        cells = [self.recipe('leader','w')] + [self.recipe(str(i),str(i),'haste') for i in range(3)]
        selected, benchmark = owner.select_search_references(cells, self.scores(cells), 'haste')
        self.assertEqual([c['id'] for c in selected], ['0','1','2'])
        self.assertEqual(benchmark, 1)
        for gear in ('missing','baseline'):
            with self.assertRaisesRegex(ValueError, 'Three distinct'):
                owner.select_search_references(cells, self.scores(cells), gear)

    def test_search_rejects_mismatched_measurements(self):
        cells = [self.recipe(str(i),str(i)) for i in range(3)]
        rows = self.scores(cells)
        with self.assertRaisesRegex(ValueError, 'match uniquely'):
            owner.select_search_references(cells, rows[:-1])
        rows[0]['gear'] = 'incorrect'
        with self.assertRaisesRegex(ValueError, 'gear differs'):
            owner.select_search_references(cells, rows)

    def test_grouping_ignores_essence_order_but_keeps_slot_assignments(self):
        a = self.recipe('a','x')
        a['scenario']['party'][0]['build']['essenceIds'] = ['b','a']
        b = copy.deepcopy(a)
        b['scenario']['party'][0]['build']['essenceIds'].reverse()
        self.assertEqual(owner.composition_key(a), owner.composition_key(b))
        b['scenario']['party'][0]['partySlot'] = 2
        self.assertNotEqual(owner.composition_key(a), owner.composition_key(b))

    def family(self):
        baseline = self.recipe('original','base')
        haste = copy.deepcopy(baseline)
        haste.update(id='original/haste', gear='haste')
        haste['scenario']['party'][0]['build']['equipment'] = [{'definitionId':'haste'}]
        return [baseline, haste]

    def finalists(self):
        return [self.recipe(str(i),str(i),'search-only','generated-finalist') for i in range(2)]

    def test_finalists_keep_exact_recipes_and_all_frozen_gear_variants(self):
        cells, finalists = self.family(), self.finalists()
        before, saved = copy.deepcopy(cells), copy.deepcopy(finalists)
        coverage = owner.append_search_finalists(cells, finalists, 'first')
        self.assertEqual(cells[:2], before)
        self.assertEqual(finalists, saved)
        self.assertEqual(len(cells), 8)
        self.assertEqual(sum(p['kind']=='exact-finalist' for p in coverage), 2)
        for finalist in finalists:
            self.assertIn(finalist['scenario'], [c['scenario'] for c in cells])
        for variant in cells[2:]:
            original = next(c for c in finalists if owner.composition_key(c)==owner.composition_key(variant))
            self.assertEqual(variant['scenario']['party'][0]['build']['identityEssenceIds'],
                             original['scenario']['party'][0]['build']['identityEssenceIds'])

    def test_repeat_finalists_merge_only_exact_scenarios_and_keep_provenance(self):
        cells, finalists = self.family(), self.finalists()
        first = owner.append_search_finalists(cells, finalists, 'first')
        second = owner.append_search_finalists(cells, finalists, 'second')
        self.assertEqual(len(cells), 8)
        self.assertEqual([p['cellId'] for p in first], [p['cellId'] for p in second])
        self.assertTrue(all(not p['added'] and p['source']=='second' for p in second))
        different = copy.deepcopy(finalists)
        for c in different:
            c['scenario']['party'][0]['build']['identityEssenceIds'] = ['new-identity']
        owner.append_search_finalists(cells, different, 'third')
        self.assertEqual(len(cells), 14)
        self.assertEqual(len({c['id'] for c in cells}), 14)

    def test_projection_rejects_slot_and_budget_changes(self):
        for field, value in [('partySlot',2),('characterLevel',41),('tier',2),('attributeRollMultiplier',1.1)]:
            finalists = self.finalists()
            member = finalists[0]['scenario']['party'][0]
            (member if field=='partySlot' else member['build'])[field] = value
            with self.assertRaisesRegex(ValueError, 'projection cannot change'):
                owner.append_search_finalists(self.family(), finalists, 'bad')

    def test_import_rejects_incomplete_or_seeded_finalists(self):
        with self.assertRaisesRegex(ValueError, 'both generated'):
            owner.append_search_finalists(self.family(), self.finalists()[:1], 'bad')
        finalists = self.finalists()
        finalists[0]['scenario']['seeds'] = [1]
        with self.assertRaisesRegex(ValueError, 'seed-free'):
            owner.append_search_finalists(self.family(), finalists, 'bad')


class GearReferenceTests(unittest.TestCase):
    def family(self):
        return [SearchChallengeTests.recipe('parent', 'parent-essence'),
                SearchChallengeTests.recipe('template', 'different-essence', 'partial-gear')]

    def test_only_equipment_changes_and_originals_are_retained(self):
        cells = self.family()
        cells[0]['scenario']['party'][0]['build']['essenceIds'] = ['z', 'a']
        before = copy.deepcopy(cells)
        receipt = owner.append_gear_reference(cells, 'parent', 'template')
        self.assertEqual(cells[:2], before)
        expected = copy.deepcopy(before[0]['scenario'])
        expected['party'][0]['build']['equipment'] = before[1]['scenario']['party'][0]['build']['equipment']
        self.assertEqual(cells[2]['scenario'], expected)
        self.assertEqual(cells[2]['composition'], 'parent')
        self.assertEqual(cells[2]['gear'], 'partial-gear')
        self.assertTrue(receipt['added'])

    def test_exact_duplicates_are_not_added(self):
        cells = self.family()
        first = owner.append_gear_reference(cells, 'parent', 'template')
        second = owner.append_gear_reference(cells, 'parent', 'template')
        self.assertEqual(len(cells), 3)
        self.assertEqual(first['cellId'], second['cellId'])
        self.assertFalse(second['added'])

    def test_rank_quality_level_tier_rolls_and_positions_are_guarded(self):
        for field,value in [('rank',4),('quality','Exceptional'),('characterLevel',50),('tier',2),
                            ('attributeRollMultiplier',1.1),('partySlot',2)]:
            cells = self.family()
            member = cells[1]['scenario']['party'][0]
            (member if field=='partySlot' else member['build'])[field] = value
            before = copy.deepcopy(cells)
            with self.assertRaisesRegex(ValueError,'positions or budgets'):
                owner.append_gear_reference(cells, 'parent', 'template')
            self.assertEqual(cells,before)

    def test_wrong_floor_seeded_or_different_party_size_is_rejected(self):
        for field,value in [('floorNumber',2),('seeds',[12]),('party',[])]:
            cells = self.family(); cells[1]['scenario'][field] = value
            with self.assertRaisesRegex(ValueError,'seed-free|party size'):
                owner.append_gear_reference(cells, 'parent', 'template')

    def test_unknown_or_ambiguous_saved_identity_is_rejected(self):
        cells = self.family()
        with self.assertRaisesRegex(ValueError,'Unique saved'):
            owner.append_gear_reference(cells, 'missing', 'template')
        cells.append(copy.deepcopy(cells[0]))
        with self.assertRaisesRegex(ValueError,'Unique saved'):
            owner.append_gear_reference(cells, 'parent', 'template')


class FixedSupportReferenceTests(unittest.TestCase):
    def family(self):
        source = SearchChallengeTests.recipe('source', 'damage')
        donor = SearchChallengeTests.recipe('donor', 'heal', 'different-gear')
        for c, essences in [(source, ['damage.z', 'damage.a']), (donor, ['heal.z', 'heal.a'])]:
            first = c['scenario']['party'][0]
            first['build']['essenceIds'] = essences
            second = copy.deepcopy(first)
            second['partySlot'] = 2
            second['build']['id'] += '-second'
            second['build']['essenceIds'] = ['unchanged.z', 'unchanged.a']
            c['scenario']['party'].append(second)
        return [source, donor]

    def test_only_declared_slot_essences_change_with_donor_order_preserved(self):
        cells = self.family(); before = copy.deepcopy(cells)
        receipt = owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
        self.assertEqual(cells[:2], before)
        expected = copy.deepcopy(before[0]['scenario'])
        expected['party'][0]['build']['essenceIds'] = ['heal.z', 'heal.a']
        self.assertEqual(cells[2]['scenario'], expected)
        self.assertEqual(cells[2]['gear'], 'baseline')
        self.assertEqual(receipt['newEssenceIds'], ['heal.z', 'heal.a'])
        self.assertTrue(receipt['added'])
        self.assertNotEqual(owner.composition_key(cells[0]), owner.composition_key(cells[2]))

    def test_exact_duplicate_merges_but_identity_variant_remains(self):
        cells = self.family()
        first = owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
        again = owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
        self.assertEqual(first['cellId'], again['cellId'])
        self.assertFalse(again['added'])
        cells[0]['scenario']['party'][0]['build']['identityEssenceIds'] = ['new-identity']
        other = owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
        self.assertTrue(other['added'])
        self.assertEqual(len(cells), 4)
        self.assertEqual(owner.composition_key(cells[2]), owner.composition_key(cells[3]))

    def test_budget_changes_rejected_without_mutation(self):
        for field, value in [('rank', 4), ('quality', 'Exceptional'), ('characterLevel', 50),
                             ('tier', 2), ('attributeRollMultiplier', 1.1)]:
            cells = self.family(); cells[1]['scenario']['party'][0]['build'][field] = value
            before = copy.deepcopy(cells)
            with self.assertRaisesRegex(ValueError, 'slot budgets'):
                owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
            self.assertEqual(cells, before)

    def test_wrong_floor_or_seeded_source_or_donor_rejected(self):
        for index in (0, 1):
            for key, value in [('floorNumber', 2), ('seeds', [123])]:
                cells = self.family(); cells[index]['scenario'][key] = value
                before = copy.deepcopy(cells)
                with self.assertRaisesRegex(ValueError, 'seed-free'):
                    owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
                self.assertEqual(cells, before)

    def test_unique_matching_party_positions_and_saved_slot_required(self):
        for value in (True, 0, 3, '1'):
            with self.assertRaisesRegex(ValueError, 'party positions'):
                owner.append_fixed_support_reference(self.family(), 'source', 'donor', value)
        for index in (0, 1):
            for change in ('reorder', 'duplicate', 'missing', 'boolean'):
                cells = self.family(); party = cells[index]['scenario']['party']
                if change == 'reorder': party.reverse()
                elif change == 'duplicate': party[1]['partySlot'] = 1
                elif change == 'missing': party.pop()
                else: party[0]['partySlot'] = True
                with self.assertRaisesRegex(ValueError, 'party positions'):
                    owner.append_fixed_support_reference(cells, 'source', 'donor', 1)

    def test_equal_unique_nonempty_essence_counts_required(self):
        for index in (0, 1):
            for value in ([], ['a'], ['a', 'b', 'c'], ['a', 'a'], ['', 'b'], [1, 2], None):
                cells = self.family(); cells[index]['scenario']['party'][0]['build']['essenceIds'] = value
                before = copy.deepcopy(cells)
                with self.assertRaisesRegex(ValueError, 'unique Essence counts'):
                    owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
                self.assertEqual(cells, before)

    def test_essence_permutations_are_not_role_changes(self):
        cells = self.family()
        cells[1]['scenario']['party'][0]['build']['essenceIds'] = ['damage.a', 'damage.z']
        with self.assertRaisesRegex(ValueError, 'not their order'):
            owner.append_fixed_support_reference(cells, 'source', 'donor', 1)

    def test_missing_ambiguous_ids_and_hash_collision_rejected(self):
        cells = self.family()
        with self.assertRaisesRegex(ValueError, 'Unique saved'):
            owner.append_fixed_support_reference(cells, 'missing', 'donor', 1)
        cells.append(copy.deepcopy(cells[0]))
        with self.assertRaisesRegex(ValueError, 'Unique saved'):
            owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
        cells = self.family()
        receipt = owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
        cells[-1]['scenario']['id'] = 'different-scenario'
        before = copy.deepcopy(cells)
        with self.assertRaisesRegex(ValueError, 'identifier collision'):
            owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
        self.assertEqual(cells, before)

    def test_existing_exact_recipe_must_keep_gear_label(self):
        cells = self.family()
        owner.append_fixed_support_reference(cells, 'source', 'donor', 1)
        cells[-1]['gear'] = 'wrong'
        with self.assertRaisesRegex(ValueError, 'different gear label'):
            owner.append_fixed_support_reference(cells, 'source', 'donor', 1)

    def test_cli_rejects_fights_or_combined_changes_before_history_or_allocation(self):
        base = ['runner', '--mode', 'prepare', '--name', 'test-support', '--source', 'source',
                '--artifacts', 'unused', '--fixed-support-reference', 'a', 'b', '1']
        cases = [['--mode', mode] for mode in ('screen', 'confirm', 'search')]
        cases += [['--current-content'], ['--gear-reference', 'a', 'b'], ['--qualified-family', 'plan'],
                  ['--add-search', 'search'], ['--add-references', 'search'], ['--ability-candidate', 'plan'],
                  ['--health-factor', '1.1'], ['--offense-factor', '.9']]
        for extra in cases:
            with patch.object(owner.sys, 'argv', base + extra), patch.object(owner, 'history') as history:
                with self.assertRaisesRegex(ValueError, 'Fixed support requires'):
                    owner.main()
                history.assert_not_called()
        no_source = base[:]
        position = no_source.index('--source'); del no_source[position:position+2]
        with patch.object(owner.sys, 'argv', no_source), patch.object(owner, 'history') as history:
            with self.assertRaisesRegex(ValueError, 'Fixed support requires'):
                owner.main()
            history.assert_not_called()


if __name__ == '__main__':
    unittest.main()
