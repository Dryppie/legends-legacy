"""Guards for importing historical search recipes without losing identity or coverage."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

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


if __name__ == '__main__':
    unittest.main()
