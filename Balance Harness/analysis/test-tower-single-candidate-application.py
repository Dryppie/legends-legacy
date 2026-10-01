"""Isolated application must never substitute a screen for confirmation."""
import importlib.util
import copy
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('application', Path(__file__).with_name('check-tower-balance-application.py'))
application = importlib.util.module_from_spec(spec); spec.loader.exec_module(application)


class SingleCandidateApplicationTests(unittest.TestCase):
    def setUp(self):
        self.cells = [dict(id=f'cell-{i}', composition=f'label-{i}', gear='label-is-not-evidence', scenario=dict(party=[
            dict(partySlot=slot, build=dict(essenceIds=[f'essence-{i}', 'shared'],
                equipment=[dict(definitionId=f'item.spec.{j}') for j in range(4 if slot <= 2 or i == 2 else 0)]))
            for slot in range(1, 4)])) for i in range(3)]
        self.evidence = dict(status='Verified', assessment=dict(verdict='Pass', familySize=3,
            bounds=[dict(id=f'cell-{i}', lower=.1, upper=.5) for i in range(3)]))

    def test_verified_confirmation_is_admitted(self):
        application.validate_single_candidate_confirmation(
            dict(mode='confirm', searchSeeds=[]), dict(status='Verified', assessment=dict(verdict='Pass')))

    def test_screen_search_or_search_seeds_are_rejected(self):
        evidence = dict(status='Verified', assessment=dict(verdict='Pass'))
        for request in [dict(mode='screen'), dict(mode='search'), dict(mode='prepare'),
                        dict(mode='confirm', searchSeeds=[1]), dict(mode='confirm'), {}]:
            with self.subTest(request=request), self.assertRaises(ValueError):
                application.validate_single_candidate_confirmation(request, evidence)

    def test_incomplete_failed_inconclusive_or_missing_audits_are_rejected(self):
        for evidence in [{}, dict(status='Verified'), dict(status='Incomplete', assessment=dict(verdict='Pass')),
                         dict(status='Verified', assessment=dict(verdict='Fail')),
                         dict(status='Verified', assessment=dict(verdict='Inconclusive'))]:
            with self.subTest(evidence=evidence), self.assertRaises(ValueError):
                application.validate_single_candidate_confirmation(dict(mode='confirm', searchSeeds=[]), evidence)

    def test_exact_item_character_and_probability_boundaries_are_accepted(self):
        application.validate_single_candidate_equipment(self.cells, self.evidence)

    def test_labels_and_essence_order_do_not_create_compositions(self):
        for essences in [['essence-0', 'shared'], ['shared', 'essence-0']]:
            with self.subTest(essences=essences), self.assertRaises(ValueError):
                cells = copy.deepcopy(self.cells)
                for member in cells[1]['scenario']['party']: member['build']['essenceIds'] = essences
                application.validate_single_candidate_equipment(cells, self.evidence)

    def test_nine_items_or_three_characters_do_not_qualify(self):
        for third_character in [False, True]:
            with self.subTest(third_character=third_character), self.assertRaises(ValueError):
                cells = copy.deepcopy(self.cells); party = cells[1]['scenario']['party']
                if third_character: party[2]['build']['equipment'].append(party[0]['build']['equipment'].pop())
                else: party[0]['build']['equipment'].append(dict(definitionId='item.spec.extra'))
                application.validate_single_candidate_equipment(cells, self.evidence)

    def test_lower_miss_does_not_qualify(self):
        self.evidence['assessment']['bounds'][1]['lower'] = .099
        with self.assertRaises(ValueError): application.validate_single_candidate_equipment(self.cells, self.evidence)

    def test_ineligible_gear_still_must_pass_ceiling(self):
        self.evidence['assessment']['bounds'][2]['upper'] = .501
        with self.assertRaises(ValueError): application.validate_single_candidate_equipment(self.cells, self.evidence)

    def test_missing_repeated_or_reordered_bounds_are_rejected(self):
        rows = self.evidence['assessment']['bounds']
        for bounds in [rows[:-1], rows[:2] + [rows[1]], list(reversed(rows))]:
            with self.subTest(bounds=bounds), self.assertRaises(ValueError):
                evidence = copy.deepcopy(self.evidence); evidence['assessment']['bounds'] = bounds
                application.validate_single_candidate_equipment(self.cells, evidence)

    def test_wrong_family_size_is_rejected(self):
        self.evidence['assessment']['familySize'] = 2
        with self.assertRaises(ValueError): application.validate_single_candidate_equipment(self.cells, self.evidence)


if __name__ == '__main__': unittest.main()
