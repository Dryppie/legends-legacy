"""Necessary-bound, monotonicity and retained-registration integrity regressions."""
import copy
import importlib.util
from pathlib import Path
import random
import tempfile
import unittest

spec=importlib.util.spec_from_file_location('recovery',Path(__file__).with_name('loadout-placement-recovery-protocol.py'))
r=importlib.util.module_from_spec(spec); spec.loader.exec_module(r)


class RecoveryBoundTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root=r.ROOT/r.matched.PRECHECK
        cls.inputs={k:r.read(root/p) for k,p in r.read(root/'inputs.json').items()}
        cls.receipt=r.read(r.ROOT/r.FAILED/r.BASELINE/'native-receipt.json')
        cls.placement=r.matched.phase_costs(cls.inputs['new-costs'],cls.inputs['new-closeout'])
        cls.baseline=dict(cls.placement,**r.native_ceilings(cls.receipt))

    def test_frozen_protocol_pins_original_failure_and_corrected_auditor(self):
        self.assertEqual(r.PLAN_PIN,r.sha(r.ROOT/r.PLAN))
        p=r.read(r.ROOT/r.PLAN)
        self.assertEqual(r.FAILED_PIN,p['failedPairManifestSha256'])
        self.assertEqual(r.sha(r.ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py'),p['correctedAuditorSha256'])

    def test_retained_necessary_bound_rejects_recovery(self):
        value=r.lower_bound(self.inputs,self.receipt)
        self.assertEqual(dict(nativeSeconds=8020,auditSeconds=1806,nativeBytes=5347633246,auditBytes=34989702),value['roundedNecessaryFloors'])
        self.assertEqual(['auditSeconds'],value['exceededPartitions'])
        self.assertEqual('RecoveryNecessaryGateFailed',value['status'])
        self.assertFalse(value['recoveryPairMayBeRegistered'])

    def test_no_completion_admission_or_runtime_measurement_is_invented(self):
        value=r.lower_bound(self.inputs,self.receipt)
        for name in ('admitted','runtimeQualified','qualificationStarted','recoveryPairStarted','qualificationAllowed','completedBaselineAvailable'):
            self.assertIs(False,value[name],name)
        for name in ('scientificLaunches','actualCombat','productionEntropyDraws','newScientificReservations','newTimingSamples'):
            self.assertEqual(0,value[name],name)
        self.assertIsNone(value['qualifiedCurrentForecast'])
        self.assertTrue(value['originalExperimentStillFailed'])

    def test_unknown_audit_ratios_use_only_mathematical_minimum(self):
        value=r.lower_bound(self.inputs,self.receipt)
        self.assertEqual(1,value['minimumRatios']['auditSeconds'])
        self.assertEqual(1,value['minimumRatios']['auditBytes'])
        self.assertTrue(value['auditRatiosAreBoundsNotObservations'])

    def test_original_and_pilot_final_closeouts_remain_binding(self):
        value=r.lower_bound(self.inputs,self.receipt)
        self.assertEqual(self.inputs['new-closeout']['auditSeconds'],value['originalPlacementPhaseFloors']['auditSeconds'])
        self.assertEqual(self.inputs['pilot-closeout']['auditBytes'],value['completedPilotPhaseCosts']['auditBytes'])
        self.assertGreater(value['originalPlacementPhaseFloors']['auditSeconds'],self.inputs['new-costs']['auditSeconds'])

    def test_native_receipt_is_a_ceiling_not_a_completed_cost(self):
        before=copy.deepcopy(self.receipt)
        self.assertEqual(dict(nativeSeconds=146.6040994,nativeBytes=1078739935),r.native_ceilings(self.receipt))
        self.assertEqual(before,self.receipt)
        self.assertEqual('MeasuredPendingAudits',self.receipt['status'])

    def test_wrong_native_receipt_is_rejected(self):
        for key,value in [('version','other'),('status','Complete'),('fights',17279),('newAuditFights',1)]:
            with self.subTest(key=key):
                receipt=dict(self.receipt); receipt[key]=value
                with self.assertRaises(ValueError): r.native_ceilings(receipt)

    def test_nonpositive_nonfinite_or_boolean_native_cost_is_rejected(self):
        for key in ('measuredSeconds','observedBytes'):
            for value in (0,-1,float('nan'),float('inf'),True):
                with self.subTest(key=key,value=value):
                    receipt=dict(self.receipt); receipt[key]=value
                    with self.assertRaises(ValueError): r.native_ceilings(receipt)

    def test_frozen_margin_reserve_and_partitions_cannot_drift(self):
        for key,value in [('margin',1),('publicationReserveSeconds',119),('auditPublicationMaximumSeconds',1806),('nativeMaximumBytes',6000000000)]:
            with self.subTest(key=key):
                inputs=copy.deepcopy(self.inputs); inputs['design']['resources'][key]=value
                with self.assertRaises(ValueError): r.lower_bound(inputs,self.receipt)

    def test_inherited_floors_cannot_be_lowered(self):
        inputs=copy.deepcopy(self.inputs); inputs['design']['resources']['inheritedForecastFloors']['auditSeconds']-=1
        with self.assertRaises(ValueError): r.lower_bound(inputs,self.receipt)

    def test_storage_drives_audit_even_without_any_new_audit_overhead(self):
        value=r.lower_bound(self.inputs,self.receipt)
        self.assertGreater(value['minimumProjectedRetainedBytes'],sum(value['inheritedFloors'][k] for k in ('nativeBytes','auditBytes')))
        self.assertGreater(value['frozenProbeAuditSecondsLowerBound'],1800)
        self.assertEqual(value['unroundedNecessaryFloors']['auditSeconds'],value['frozenProbeAuditSecondsLowerBound'])

    def test_rounding_is_upward_and_partition_limit_is_strict(self):
        floors={k:v-1 for k,v in r.pre.LIMITS.items()}
        self.assertEqual([],r.rounded_limits(floors)[1])
        floors['auditSeconds']=1799.01
        self.assertIn('auditSeconds',r.rounded_limits(floors)[1])
        floors['auditSeconds']=1800
        self.assertIn('auditSeconds',r.rounded_limits(floors)[1])

    def test_slower_recovery_native_baseline_cannot_improve_ratios(self):
        a=r.recovery_forecast(self.inputs,self.receipt,self.baseline,self.placement)
        slower=dict(self.baseline,nativeSeconds=self.baseline['nativeSeconds']*3,nativeBytes=self.baseline['nativeBytes']*3)
        b=r.recovery_forecast(self.inputs,self.receipt,slower,self.placement)
        self.assertEqual(a['roundedPhaseFloors'],b['roundedPhaseFloors'])

    def test_faster_recovery_placement_cannot_discard_original_floor(self):
        faster={k:v/3 for k,v in self.placement.items()}
        value=r.recovery_forecast(self.inputs,self.receipt,self.baseline,faster)
        self.assertEqual(self.placement,value['upwardOnlyPlacementFloors'])

    def test_faster_native_baseline_increases_cost_floor(self):
        faster=dict(self.baseline,nativeSeconds=self.baseline['nativeSeconds']/2,nativeBytes=self.baseline['nativeBytes']/2)
        value=r.recovery_forecast(self.inputs,self.receipt,faster,self.placement)
        bound=r.lower_bound(self.inputs,self.receipt)
        for phase in ('nativeSeconds','nativeBytes'):
            self.assertGreater(value['roundedPhaseFloors'][phase],bound['roundedNecessaryFloors'][phase])

    def test_random_permitted_recovery_costs_never_undercut_necessary_bound(self):
        random_source=random.Random(17280)
        bound=r.lower_bound(self.inputs,self.receipt)['roundedNecessaryFloors']
        for _ in range(100):
            baseline={k:v*random_source.uniform(.25,4) for k,v in self.baseline.items()}
            placement={k:v*random_source.uniform(.25,4) for k,v in self.placement.items()}
            forecast=r.recovery_forecast(self.inputs,self.receipt,baseline,placement)
            self.assertTrue(all(forecast['roundedPhaseFloors'][k]>=bound[k] for k in r.pre.PHASES))
            self.assertIn('auditSeconds',forecast['exceededPartitions'])

    def test_registration_cannot_use_replacement_output(self):
        with tempfile.TemporaryDirectory() as folder:
            path=Path(folder)/'replacement'
            with self.assertRaisesRegex(ValueError,'single new registration path'): r.register(path)
            self.assertFalse(path.exists())


class RetainedIntegrityTests(unittest.TestCase):
    def test_external_manifest_pin_is_required(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); r.write(root/'files.json',{})
            with self.assertRaisesRegex(ValueError,'externally pinned'): r.verify(root,None)

    def test_added_file_is_rejected_before_assessment(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); r.write(root/'files.json',{}); pin=r.sha(root/'files.json')
            r.write(root/'extra.json',{})
            with self.assertRaisesRegex(ValueError,'membership'): r.verify(root,pin)

    def test_changed_retained_file_is_rejected_before_assessment(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); r.write(root/'input.json',{})
            r.write(root/'files.json',{'input.json':r.sha(root/'input.json')}); pin=r.sha(root/'files.json')
            (root/'input.json').write_text('{"changed":true}',encoding='utf-8')
            with self.assertRaisesRegex(ValueError,'retained registration file'): r.verify(root,pin)


if __name__ == '__main__': unittest.main()
