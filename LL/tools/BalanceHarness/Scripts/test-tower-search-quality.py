"""Audit quality must never bypass the strict normal-archive assessment."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('search', Path(__file__).with_name('compare-tower-team-search.py'))
search = importlib.util.module_from_spec(spec); spec.loader.exec_module(search)


class QualityEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.entries = [{'id': 'a'}]
        self.observations = [{'cellId': 'a', 'status': 'Complete', 'trials': [{'seed': 42, 'outcome': 'Victory'}]}]
        self.assessment = {'assessment': 'Fail', 'issues': [], 'cells': [{'id': 'a', 'valid': 1, 'wins': 1, 'issues': []}]}

    def test_verified_balance_failure_remains_valid_quality_evidence(self):
        search.validate_audit_evidence(self.entries, self.observations, self.assessment, [42])

    def test_complete_raw_evidence_cannot_override_invalid_contract(self):
        for update in ({'assessment': 'Invalid'}, {'issues': ['Unknown scenario field']}):
            with self.subTest(update=update), self.assertRaises(RuntimeError):
                search.validate_audit_evidence(self.entries, self.observations, {**self.assessment, **update}, [42])

    def test_duplicate_and_changed_outcomes_rejected(self):
        for observations in (self.observations * 2,
                             [{'cellId': 'a', 'status': 'Complete', 'trials': [{'seed': 42, 'outcome': 'Defeat'}]}]):
            with self.subTest(observations=observations), self.assertRaises(RuntimeError):
                search.validate_audit_evidence(self.entries, observations, self.assessment, [42])
        invalid = copy.deepcopy(self.assessment); invalid['cells'][0]['issues'] = ['Content changed']
        with self.assertRaises(RuntimeError):
            search.validate_audit_evidence(self.entries, self.observations, invalid, [42])


if __name__ == '__main__': unittest.main()
