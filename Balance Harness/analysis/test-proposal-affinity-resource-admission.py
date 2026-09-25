"""Fail-closed planning tests for the versioned admission; never touches the registry."""
import copy
import importlib.util
from pathlib import Path
import unittest
import tempfile
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('resource_admission', Path(__file__).with_name('prepare-proposal-affinity-resource-admission.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class AdmissionTests(unittest.TestCase):
    def setUp(self):
        self.previous = dict(nativeSeconds=4200, nativeBytes=3000, auditBytes=400, totalBytes=3400)
        self.probe = dict(status='AuditPartitionStillNotSupported', timingMargin=2, publicationReserveSeconds=120,
            wholeWorkerSeconds=170, independentAuditSeconds=[35,15], extraInventorySeconds=370,
            inventoryByteRatio=4, auditPlanningSeconds=1300)
        self.estimate = dict(self.previous, nativeSeconds=4100)

    def result(self): return a.forecast(self.previous, self.probe, 850, self.estimate)

    def test_success_uses_new_caps_without_lowering_native_or_probe_estimates(self):
        result = self.result()
        self.assertTrue(result['fits']); self.assertFalse(result['guaranteedUpperBound'])
        self.assertEqual((4200, 1300, 9000, 1800), tuple(result[k] for k in
            ('nativeSeconds','auditSeconds','nativeMaximumSeconds','auditMaximumSeconds')))
        self.estimate['totalBytes'] = 1700
        self.assertEqual(1300, self.result()['auditSeconds'])

    def test_larger_archive_increases_projection_and_cannot_borrow_native_time(self):
        self.estimate['totalBytes'] = 6800
        result = self.result()
        self.assertEqual(2040, result['auditSeconds']); self.assertFalse(result['fits'])
        self.assertLess(result['totalSeconds'], 10800)
        with self.assertRaises(ValueError): a.base.admit_forecast(result)

    def test_each_partition_is_independently_binding(self):
        for key, value in (('nativeSeconds',9000), ('nativeBytes',a.owner.NATIVE_BYTES), ('auditBytes',a.owner.AUDIT_BYTES)):
            with self.subTest(key=key):
                estimate = dict(self.estimate, **{key:value})
                self.assertFalse(a.forecast(self.previous,self.probe,850,estimate)['fits'])

    def test_probe_margin_and_reserve_cannot_be_reduced_after_measurement(self):
        for key, value in (('timingMargin',1), ('publicationReserveSeconds',60), ('status','Passed')):
            with self.subTest(key=key), self.assertRaises(ValueError):
                a.forecast(self.previous,dict(self.probe, **{key:value}),850,self.estimate)

    def test_any_change_to_scientific_sources_rejects_qualification(self):
        old = {'LL/tools/BalanceHarness/TowerProposalStudyRun.cs':'old',
               'LL/tools/BalanceHarness/TowerProposalStudyProtocol.cs':'old',
               'LL/tools/BalanceHarness/TowerProposalPolicies.cs':'unchanged'}
        new = dict(old, **{n:'new' for n in a.CHANGED_SOURCES})
        with patch.object(a, 'read', return_value=old):
            self.assertEqual(sorted(a.CHANGED_SOURCES), a.source_changes(new))
            changed = dict(new); changed['LL/tools/BalanceHarness/TowerProposalPolicies.cs']='drift'
            with self.assertRaises(ValueError): a.source_changes(changed)
            changed = dict(new); del changed['LL/tools/BalanceHarness/TowerProposalPolicies.cs']
            with self.assertRaises(ValueError): a.source_changes(changed)

    def test_prior_forecast_is_consumed_from_its_pinned_probe_copy(self):
        with tempfile.TemporaryDirectory(prefix='proposal-forecast-binding-') as temp:
            root = Path(temp)
            a.save(root/'previous-resource-forecast.json', self.previous)
            a.save(root/'resource-assessment.json', self.probe)
            a.save(root/'files.json', a.inventory(root))
            with patch.object(a, 'PROBE_PIN', a.sha(root/'files.json')):
                previous, probe, size = a.retained_forecast_inputs(root)
                self.assertEqual(self.previous, previous); self.assertEqual(self.probe, probe); self.assertEqual(850, size)
                (root/'previous-resource-forecast.json').write_text('{}')
                with self.assertRaisesRegex(ValueError, 'Changed retained'): a.retained_forecast_inputs(root)


if __name__ == '__main__': unittest.main(verbosity=2)
