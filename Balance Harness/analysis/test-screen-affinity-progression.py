"""Regression checks for reference selection, preserved allies and result integrity."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('screen', Path(__file__).with_name('screen-affinity-progression.py'))
screen = importlib.util.module_from_spec(spec)
spec.loader.exec_module(screen)


class ProgressionScreenTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.definition = screen.io.read(screen.DEFINITION)
        cls.curve = screen.io.read(screen.ROOT / cls.definition['curve']['path'])
        cls.controls = screen.io.read(screen.ROOT / 'LL/tools/BalanceHarness/Fixtures/tower-whole-party-controls.json')

    def test_portable_controls_change_only_absolute_first_four_slots(self):
        case = next(c for c in self.definition['cases'] if c['floor'] == 10)
        control, guided = self.controls['parties']['6'][:2]
        before = screen.progression_recipe(case, control, self.curve)
        after = screen.progression_recipe(case, guided, self.curve)
        self.assertEqual(before['party'][4:], after['party'][4:])
        self.assertEqual(screen.background(before), screen.background(after))
        for i in range(4):
            self.assertEqual(after['party'][i]['build']['essenceIds'], guided['builds'][str(i + 1)])
        self.assertNotEqual(before['party'][:4], after['party'][:4])

    def test_floor_fifteen_uses_declared_late_budget_and_fifteen_characters(self):
        case = next(c for c in self.definition['cases'] if c['floor'] == 15)
        result = screen.scenarios(case, self.controls, self.curve, list(range(32)))
        self.assertEqual(len(result), 3)
        for recipe in result:
            self.assertEqual(len(recipe['party']), 15)
            self.assertTrue(all(p['build']['characterLevel'] == 90 and len(p['build']['essenceIds']) == 10 for p in recipe['party']))

    def test_changed_budget_fails_before_combat(self):
        case = copy.deepcopy(next(c for c in self.definition['cases'] if c['floor'] == 10))
        case['budget']['essenceSlots'] = 5
        with self.assertRaises(KeyError):
            screen.scenarios(case, self.controls, self.curve, list(range(32)))

    def test_duplicate_complete_references_are_rejected(self):
        case = next(c for c in self.definition['cases'] if c['floor'] == 10)
        catalog = copy.deepcopy(self.controls)
        catalog['parties']['6'][1]['builds'] = copy.deepcopy(catalog['parties']['6'][0]['builds'])
        with self.assertRaisesRegex(ValueError, 'Duplicate complete'):
            screen.scenarios(case, catalog, self.curve, list(range(32)))

    def test_screen_gate_has_explicit_inclusive_boundaries(self):
        for wins, expected in [(0, 'LowWinReference'), (3, 'LowWinReference'), (4, 'FollowUpCandidate'),
                               (28, 'FollowUpCandidate'), (29, 'CeilingReference'), (32, 'CeilingReference')]:
            with self.subTest(wins=wins):
                self.assertEqual(screen.classify(wins), expected)
        for invalid in (-1, 33, True, 4.0):
            with self.subTest(invalid=invalid), self.assertRaises(ValueError):
                screen.classify(invalid)

    def test_order_only_controls_are_skipped_without_reordering_recipes(self):
        def build(identity, essences):
            return {'id': identity, 'scenario': {'party': [{'partySlot': 1, 'build': {'essenceIds': essences}}]}}
        study = {'builds': [build('first', ['b', 'a']), build('permuted', ['a', 'b']),
                            build('second', ['d', 'c']), build('third', ['f', 'e'])]}
        before = copy.deepcopy(study)
        self.assertEqual(screen.distinct_retained(study), ['first', 'second', 'third'])
        self.assertEqual(study, before)

    def test_equivalent_composition_key_keeps_character_assignment(self):
        one = {'party': [{'partySlot': 1, 'build': {'essenceIds': ['a']}},
                         {'partySlot': 2, 'build': {'essenceIds': ['b']}}]}
        two = copy.deepcopy(one)
        two['party'][0]['build']['essenceIds'], two['party'][1]['build']['essenceIds'] = ['b'], ['a']
        self.assertNotEqual(screen.essence_sets(one), screen.essence_sets(two))

    def fixture(self, directory):
        path = Path(directory)
        (path / 'battles').mkdir()
        seeds = list(range(32))
        scenario = {'id': 'test-reference', 'seeds': seeds}
        settings = {'threat': {'enabled': True}, 'checkpointIntervalTicks': 10}
        frozen = {'seeds': seeds, 'scope': {'execution': {'runtime': 'test'}, 'contentHashes': {}, 'settings': settings}}
        screen.io.write(path / 'tower-manifest.json', {'execution': {'runtime': 'test'}, 'contentHashes': {}})
        screen.io.write(path / 'tower-input.json', [{'scenario': scenario, 'rules': {'randomSeed': seed},
                        'threatAndTanking': settings['threat'], 'checkpointIntervalTicks': 10} for seed in seeds])
        trials, hashes = [], {}
        for seed in seeds:
            identity = f'tower.{seed+1:04d}'
            report = {'succeeded': seed < 10, 'guardianHealthRemainingPercent': 0 if seed < 10 else 50,
                      'battle': {'seed': seed, 'scenarioId': scenario['id'], 'summary': {
                          'contentOutcome': 'Victory' if seed < 10 else 'Defeat', 'terminationReason': 'Elimination'}}}
            battle = path / 'battles' / (identity + '.json')
            screen.io.write(battle, report)
            hashes[identity] = screen.io.sha(battle)
            trials.append({'id': identity, 'seed': seed, 'report': report})
        screen.io.write(path / 'tower-results.json', hashes)
        screen.io.write(path / 'scorecard.json', {'status': 'Complete', 'planned': 32, 'valid': 32,
                        'invalid': 0, 'cancelled': 0, 'notRun': 0, 'wins': 10, 'defeats': 22,
                        'draws': 0, 'tickLimits': 0, 'trials': trials})
        return path, scenario, frozen

    def test_results_are_recounted_from_individual_reports(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = screen.verify_cell(*self.fixture(tmp))
            self.assertEqual(result['wins'], 10)
            self.assertEqual(result['meanGuardianHealthRemainingPercent'], 34.375)

    def test_tampered_battle_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            args = self.fixture(tmp)
            (args[0] / 'battles/tower.0001.json').write_text('{}', encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'Battle hash'):
                screen.verify_cell(*args)

    def test_changed_runtime_settings_or_schedule_are_rejected(self):
        for field in ('execution', 'settings', 'seeds'):
            with self.subTest(field=field), tempfile.TemporaryDirectory() as tmp:
                path, scenario, frozen = self.fixture(tmp)
                if field == 'seeds':
                    frozen['seeds'] = list(reversed(frozen['seeds']))
                elif field == 'settings':
                    frozen['scope']['settings']['checkpointIntervalTicks'] = 5
                else:
                    frozen['scope']['execution']['runtime'] = 'other'
                with self.assertRaises(ValueError):
                    screen.verify_cell(path, scenario, frozen)

    def test_forged_win_count_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            args = self.fixture(tmp)
            file = args[0] / 'scorecard.json'
            score = screen.io.read(file)
            score['wins'] = 11
            file.write_text(json.dumps(score), encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'counts disagree'):
                screen.verify_cell(*args)


if __name__ == '__main__':
    unittest.main()
