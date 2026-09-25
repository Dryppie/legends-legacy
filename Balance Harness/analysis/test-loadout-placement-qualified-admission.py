import copy
import importlib.util
from pathlib import Path
import unittest

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('qualified_placement_admission', HERE / 'admit-loadout-placement-qualified.py')
admission = importlib.util.module_from_spec(spec); spec.loader.exec_module(admission)


class QualifiedAdmissionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.forecast = admission.read(admission.pair.OUTPUT / 'forecast.json')
        cls.maximum = admission.read(admission.pair.OUTPUT / 'maximum-workload.json')

    def test_unchanged_metadata_reproduces_every_qualified_floor(self):
        self.assertEqual(self.forecast['roundedPhaseFloors'], admission.check_forecast(self.forecast, self.maximum))

    def test_metadata_growth_never_reduces_any_floor(self):
        before = admission.check_forecast(self.forecast, self.maximum)
        after = admission.check_forecast(self.forecast, self.maximum, 1.2)
        for key in before: self.assertGreaterEqual(after[key], before[key])

    def test_metadata_growth_cannot_borrow_another_partition(self):
        with self.assertRaises(ValueError): admission.check_forecast(self.forecast, self.maximum, 2)

    def test_discount_nonfinite_and_nonpassing_forecast_are_rejected(self):
        for ratio in (0, .9, float('nan'), float('inf')):
            with self.subTest(ratio=ratio), self.assertRaises(ValueError):
                admission.check_forecast(self.forecast, self.maximum, ratio)
        altered = copy.deepcopy(self.forecast); altered['resourceForecastPassed'] = False
        with self.assertRaises(ValueError): admission.check_forecast(altered, self.maximum)

    def test_changed_limits_and_inventory_multiplicity_are_rejected(self):
        for field, value in [('extraProtectedInventoryPasses', 0), ('timingMargin', 1), ('publicationReserveSeconds', 0)]:
            altered = copy.deepcopy(self.forecast); altered[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError): admission.check_forecast(altered, self.maximum)
        altered = copy.deepcopy(self.forecast); altered['limits']['auditSeconds'] += 1
        with self.assertRaises(ValueError): admission.check_forecast(altered, self.maximum)


if __name__ == '__main__': unittest.main()
