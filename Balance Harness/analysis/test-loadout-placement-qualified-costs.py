"""Boundary checks for the prospective qualified plain resource model."""
import importlib.util
from pathlib import Path
import unittest

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('qualified_costs', HERE / 'loadout-placement-qualified-costs.py')
model = importlib.util.module_from_spec(spec); spec.loader.exec_module(model)


class QualifiedCostsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.inputs = model.recovery.inputs_at(model.ROOT / 'TestResults/loadout-placement-recovery-protocol-20260924')
        cls.failed = model.matched.read(model.ROOT / 'TestResults/loadout-placement-matched-owned-20260924/tower-proposal-owned-fixture-matched-baseline/result/native-receipt.json')
        cls.costs = model.matched.phase_costs(cls.inputs['new-costs'], cls.inputs['new-closeout'])

    def maximum(self):
        return dict(status='PlainMaximumWorkloadVerified', literalReports=21888, heldoutMembers=36,
            searchReports=12672, heldoutReports=9216, searches=24, catalogues=12, scientificReservations=0,
            nativeAuditSubstituted=False, productionAuditPassed=True, independentPythonAuditPassed=True,
            nativePublicationBarrierPassed=True, nativePostPublicationVerificationPassed=True, plainEvidence=True,
            actualCombat=0, productionEntropyDraws=0, phaseCosts=dict(nativeSeconds=100, auditSeconds=50,
                nativeBytes=100000000, auditBytes=1000000), finalVerificationSeconds=25)

    def forecast(self, baseline=None, placement=None, maximum=None):
        return model.forecast(self.inputs, self.failed, baseline or self.costs, placement or self.costs, maximum or self.maximum())

    def test_failed_native_ceilings_cannot_be_laundered_with_slower_new_baseline(self):
        baseline = {k: v * 100 for k, v in self.costs.items()}
        result = self.forecast(baseline=baseline)
        self.assertEqual(result['failedNativeDenominatorCeilings']['nativeSeconds'], result['boundedBaseline']['nativeSeconds'])
        self.assertEqual(result['failedNativeDenominatorCeilings']['nativeBytes'], result['boundedBaseline']['nativeBytes'])
        for k in model.pre.PHASES:
            self.assertGreaterEqual(result['unroundedPhaseFloors'][k], result['retainedUnamendedCalculation']['inheritedFloors'][k])

    def test_larger_candidate_costs_cannot_reduce_any_floor(self):
        before = self.forecast()
        after = self.forecast(placement={k: v * 2 for k, v in self.costs.items()})
        for k in model.pre.PHASES:
            self.assertGreaterEqual(after['unroundedPhaseFloors'][k], before['unroundedPhaseFloors'][k])

    def test_actual_maximum_workload_is_absolute_floor_never_denominator(self):
        maximum = self.maximum(); maximum['phaseCosts']['nativeSeconds'] = 4500
        result = self.forecast(maximum=maximum)
        self.assertIn('nativeSeconds', result['exceededPartitions'])
        self.assertFalse(result['resourceForecastPassed'])
        self.assertFalse(result['scientificAdmitted'])

    def test_post_publication_verification_and_reserve_are_charged(self):
        maximum = self.maximum(); maximum['finalVerificationSeconds'] = 1000
        result = self.forecast(maximum=maximum)
        self.assertEqual(2220, result['currentMaximumAuditSeconds'])
        self.assertIn('auditSeconds', result['exceededPartitions'])

    def test_rejects_missing_audit_and_partial_workload(self):
        for field, value in [('literalReports', 18816), ('heldoutMembers', 24), ('nativeAuditSubstituted', True),
            ('productionAuditPassed', False), ('independentPythonAuditPassed', False), ('plainEvidence', False)]:
            with self.subTest(field=field):
                maximum = self.maximum(); maximum[field] = value
                with self.assertRaises(ValueError): self.forecast(maximum=maximum)

    def test_current_inventory_model_keeps_unamended_calculation_and_closed_science(self):
        result = self.forecast()
        self.assertEqual(1, result['extraProtectedInventoryPasses'])
        self.assertEqual(2, result['timingMargin']); self.assertEqual(120, result['publicationReserveSeconds'])
        self.assertEqual(model.pre.LIMITS, result['limits'])
        self.assertIn('roundedPhaseFloors', result['retainedUnamendedCalculation'])
        self.assertFalse(result['scientificAdmitted']); self.assertFalse(result['compressedLaunchAllowed'])


if __name__ == '__main__': unittest.main()
