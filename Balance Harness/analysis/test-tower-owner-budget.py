"""Owner allowance rounding must not discard successful fixed-panel evidence."""
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('owner_budget_aggregate', Path(__file__).with_name('tower-balance-aggregate.py'))
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)


class OwnerBudgetTests(unittest.TestCase):
    def test_exact_declared_allowance(self):
        self.assertTrue(a.has_declared_owner_budget(900))
        self.assertTrue(a.has_declared_owner_budget(900.0))

    def test_observed_monotonic_rounding(self):
        self.assertTrue(a.has_declared_owner_budget(899.9999999999709))
        self.assertTrue(a.has_declared_owner_budget(900.0000000000291))

    def test_different_limits_are_rejected(self):
        for value in (0, 840, 899, 901, 900.00001, 899.99999, -900):
            with self.subTest(value=value):
                self.assertFalse(a.has_declared_owner_budget(value))

    def test_nonfinite_limits_are_rejected(self):
        for value in (float('nan'), float('inf'), -float('inf')):
            self.assertFalse(a.has_declared_owner_budget(value))

    def test_nonnumeric_limits_are_rejected(self):
        for value in (True, False, '900', None, [900]):
            self.assertFalse(a.has_declared_owner_budget(value))

    def test_rounding_does_not_change_native_resource_gate(self):
        completed = dict(completed=2976, seconds=336)
        with self.assertRaises(ValueError):
            a.resource_admission(completed, 16, 186, 1000)


if __name__ == '__main__':
    unittest.main()
