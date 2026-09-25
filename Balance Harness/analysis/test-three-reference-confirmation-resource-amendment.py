"""Prospective accounting and unchanged-science checks; no admission or native calls."""
import copy
import importlib.util
from pathlib import Path
import unittest

path=Path(__file__).with_name('three-reference-confirmation-resource-amendment.py')
spec=importlib.util.spec_from_file_location('amendment',path)
m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m)

class AmendmentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.old=m.read(m.BASE); cls.failed=m.read(m.FAILED/'failure.json'); cls.charge=m.read(m.FAILED/'admission-charge.json')

    def test_failed_charge_and_one_new_allowance_are_both_counted(self):
        result=m.resources(self.old['resources'],self.failed,self.charge)
        self.assertEqual(dict(seconds=8400,bytes=4831838208),result['proposedStudyCumulative'])
        self.assertEqual(dict(seconds=1200,bytes=1073741824,requiredDistinctReceipts=2),result['preExecutionCharges'])
        for unit in ('seconds','bytes'):
            self.assertEqual(result['proposedStudyCumulative'][unit],result['executionOwner'][unit]+result['admissionAllowance'][unit]+result['previousFailedAdmission'][unit])
            self.assertEqual(result['proposedStudyCumulative'][unit],result['originalStudyCumulative'][unit]+result['additionalStudyAllowance'][unit])

    def test_measured_time_does_not_refund_the_full_failed_allowance(self):
        result=m.resources(self.old['resources'],dict(self.failed,measuredSeconds=1),self.charge)
        self.assertEqual(600,result['previousFailedAdmission']['seconds'])
        for bad in (dict(self.failed,admissionChargeSeconds=499),dict(self.failed,admissionChargeBytes=0)):
            with self.assertRaises(ValueError): m.resources(self.old['resources'],bad,self.charge)
        with self.assertRaises(ValueError): m.resources(self.old['resources'],self.failed,dict(self.charge,chargedSeconds=0))

    def test_unclosed_or_scientific_failed_attempt_is_rejected(self):
        for key,value in [('status','Open'),('scientificLaunches',1),('newValues',1),('fights',1),('retries',1)]:
            with self.subTest(key=key),self.assertRaises(ValueError):
                m.resources(self.old['resources'],dict(self.failed,**{key:value}),self.charge)

    def test_original_inputs_and_nontransferable_execution_limits_stay_unchanged(self):
        original=copy.deepcopy(self.old['resources']); result=m.resources(original,self.failed,self.charge)
        self.assertEqual(self.old['resources'],original)
        for name in ('executionOwner','nativeRunMaximum','auditAndPublicationReserve','priorEngineeringLedger','sizing'):
            self.assertEqual(original[name],result[name])
        bad=copy.deepcopy(original); bad['executionOwner']['seconds']+=600
        with self.assertRaises(ValueError): m.resources(bad,self.failed,self.charge)

    def test_every_nonresource_protocol_field_is_preserved_and_version_is_distinct(self):
        result=m.build()
        self.assertEqual(set(self.old)-{'resources','proposedNativeVersion'},set(result['unchangedProtocolFields']))
        for key,pin in result['unchangedProtocolFields'].items(): self.assertEqual(m.canonical(self.old[key]),pin)
        self.assertEqual('tower-practical-three-reference-confirmation-v1',result['originalNativeVersion'])
        self.assertEqual('tower-practical-three-reference-confirmation-v2',result['proposedNativeVersion'])
        self.assertEqual(m.BASE_PIN,m.sha(m.BASE))

    def test_proposal_is_not_an_executable_request_or_new_admission(self):
        result=m.build(); self.assertFalse(result['runnableRequest']); self.assertFalse(result['implementationComplete'])
        self.assertFalse(result['costEvidence']['nativeAdmissionCostMeasured'])
        for key in ('newAdmissions','nativePreparations','scientificLaunches','newValues','fights'): self.assertEqual(0,result[key])
        for key in ('outputRoot','requiredHistory','definitionPath','contentRoot'): self.assertNotIn(key,result)
        self.assertEqual(m.FAILED_PIN,result['resources']['previousFailedAdmission']['externalInventorySha256'])

if __name__=='__main__': unittest.main(verbosity=2)
