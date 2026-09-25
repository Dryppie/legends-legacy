"""Prospective resource arithmetic and literal owner call-count checks only."""
import copy
import importlib.util
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path);value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value
m=module('inventory_resource',ROOT/'Balance Harness/analysis/loadout-placement-publication-inventory.py')
f=module('inventory_calls',ROOT/'build/test-proposal-publication-inventory.py')

class Resource(unittest.TestCase):
    def setUp(self):
        self.recovery=json.loads((ROOT/'TestResults/loadout-placement-recovery-protocol-20260924/assessment.json').read_bytes())
        self.probe=json.loads((ROOT/'TestResults/proposal-native-audit-cost-probe-20260923/resource-assessment.json').read_bytes())
        self.proof=dict(legacyManifest={'input':'a','audit':'b'},currentManifest={'input':'a','audit':'b'},protected={'input':'a'},
            inputSizes={'input':100,'audit':10},legacyReadBytes=210,currentReadBytes=110,removedReadBytes=100,
            timingSample=False,resourceForecast=None,usableForAdmission=False)
    def assess(self):return m.assess(self.recovery,self.probe,self.proof,dict(m.PASSES))

    def test_historical_inputs_reconcile_to_1764_without_other_partition_discounts(self):
        value=self.assess();self.assertEqual(value['roundedNecessaryFloors'],dict(nativeSeconds=8020,auditSeconds=1764,nativeBytes=5347633246,auditBytes=34989702))
        self.assertEqual(value['historicalRoundedFloors']['auditSeconds'],1806)
        self.assertTrue(value['qualificationMayBeRegistered']);self.assertAlmostEqual(value['revisedProbePlanningSeconds'],1185.0402611728715)

    def test_passing_necessary_gate_never_authorizes_measurement_or_science(self):
        value=self.assess()
        for key in ('measurementAuthorized','nativePreparationAllowed','scientificAdmissionAllowed','compressedPreparationAllowed','compressedLaunchAllowed','wholeProcessCoverage','usableForAdmission'):
            self.assertIs(value[key],False)
        self.assertIsNone(value['qualifiedCurrentForecast'])

    def test_assessment_does_not_mutate_historical_inputs(self):
        before=copy.deepcopy((self.recovery,self.probe,self.proof));self.assess()
        self.assertEqual(before,(self.recovery,self.probe,self.proof))

    def test_higher_retained_floor_and_strict_rounded_cap_still_fail(self):
        for cost in (1799.01,1800,1900):
            self.recovery['inheritedFloors']['auditSeconds']=cost
            self.recovery['unroundedNecessaryFloors']['auditSeconds']=max(cost,self.recovery['frozenProbeAuditSecondsLowerBound'])
            value=self.assess();self.assertFalse(value['necessaryGatePassed']);self.assertIn('auditSeconds',value['exceededPartitions'])

    def test_no_arbitrary_extra_pass_discount(self):
        for key,value in (('extraAfter',0),('extraBefore',3),('ownerAfter',1),('ownerBefore',4),('includedInWholeWorker',True)):
            passes=dict(m.PASSES);passes[key]=value
            with self.assertRaises(ValueError):m.assess(self.recovery,self.probe,self.proof,passes)

    def test_nonfinite_zero_negative_and_boolean_costs_rejected(self):
        for value in (float('nan'),float('inf'),0,-1,True):
            self.probe['wholeWorkerSeconds']=value
            with self.assertRaises(ValueError):self.assess()

    def test_changed_historical_envelope_rejected(self):
        self.recovery['limits']['auditSeconds']=1806
        with self.assertRaisesRegex(ValueError,'historical limits'):self.assess()

    def test_unreconciled_historical_audit_floor_rejected(self):
        self.recovery['unroundedNecessaryFloors']['auditSeconds']=1800
        with self.assertRaisesRegex(ValueError,'Unreconciled'):self.assess()

    def test_invalid_work_proofs_cannot_receive_credit(self):
        for key,value in (('legacyReadBytes',209),('removedReadBytes',99),('currentReadBytes',True),('timingSample',True),('currentManifest',{'input':'changed','audit':'b'})):
            with self.subTest(key=key):
                proof=dict(self.proof);proof[key]=value
                with self.assertRaises(ValueError):m.assess(self.recovery,self.probe,proof,dict(m.PASSES))

    def test_both_audit_terms_must_be_retained(self):
        self.probe['independentAuditSeconds'].pop()
        with self.assertRaisesRegex(ValueError,'Both independent'):self.assess()


class OwnerPasses(unittest.TestCase):
    def test_default_and_accounted_owner_read_protected_native_log_twice(self):
        case=f.Publication();case.setUp();self.addCleanup(case.doCleanups)
        for counted in (False,True):
            with self.subTest(counted=counted):
                supervisor=case.f.prepare(case.f.base/str(counted));calls=[];digest=f.owner.digest
                def counted_digest(path):
                    if path==case.f.output/'native-console.log':calls.append(path)
                    return digest(path)
                with patch.object(f.owner,'digest',counted_digest):case.launch(supervisor if counted else None)
                self.assertEqual(len(calls),2)

if __name__=='__main__':unittest.main()
