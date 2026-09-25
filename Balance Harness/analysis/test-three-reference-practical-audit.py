"""Saved-row audit regression checks; no allocation, native process or combat."""
import copy
import importlib.util
from pathlib import Path
import unittest

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('three_reference_audit', HERE/'audit-three-reference-practical.py')
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class RateCoverageTests(unittest.TestCase):
    def setUp(self):
        self.wins = {'primary-control': 700, 'second-control': 640, 'third-control': 760}
        self.rates = []
        for party, wins in self.wins.items():
            lower, upper = audit.wilson(wins, 1000)
            self.rates.append(dict(partyId=party, wins=wins,
                estimate=dict(rate=wins/1000, lower=lower, upper=upper, confidence=.995)))

    def test_retained_and_novel_families(self):
        audit.check_rates(self.rates, self.wins)
        lower, upper = audit.wilson(820, 1000)
        self.rates.append(dict(partyId='novel', wins=820,
            estimate=dict(rate=.82, lower=lower, upper=upper, confidence=.995)))
        audit.check_rates(self.rates, dict(self.wins, novel=820))

    def test_missing_or_duplicated_third_control_rejected(self):
        for rates in (self.rates[:2], self.rates[:2]+[self.rates[0]]):
            with self.assertRaisesRegex(ValueError, 'recipe rate'):
                audit.check_rates(rates, self.wins)

    def test_old_family_and_false_wins_rejected(self):
        for field, value in [('confidence', 1-.05/7), ('lower', self.rates[2]['estimate']['lower']+.001)]:
            rates = copy.deepcopy(self.rates)
            rates[2]['estimate'][field] = value
            with self.assertRaisesRegex(ValueError, 'interval arithmetic'):
                audit.check_rates(rates, self.wins)
        rates = copy.deepcopy(self.rates)
        rates[2]['wins'] += 1
        with self.assertRaisesRegex(ValueError, 'saved outcomes'):
            audit.check_rates(rates, self.wins)

    def test_interval_zero_and_perfect_outcomes(self):
        # Family-ten, two-sided alpha=.005: z=2.807033768343811.
        audit.close(audit.wilson(0, 1000)[0], 0)
        audit.close(audit.wilson(0, 1000)[1], 0.007817838399154358)
        audit.close(audit.wilson(1000, 1000)[0], 0.9921821616008457)
        audit.close(audit.wilson(1000, 1000)[1], 1)


if __name__ == '__main__':
    unittest.main()
