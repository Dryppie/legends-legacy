"""Pilot 02 rejection tests; no combat, entropy, registry writes or timing probes."""
import copy
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('pilot02_admission', Path(__file__).with_name('prepare-proposal-affinity-resource-admission.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class Pilot02Tests(unittest.TestCase):
    def test_repaired_admission_requires_closed_qualification_and_carries_failure_charge(self):
        declaration = dict(a.PILOT02_REPAIRED_CONTRACT, backendTestCount=34, baselineBackendTestCount=184,
            repairHandoff=dict(sha256=a.REPAIR_HANDOFF_PIN))
        a.validate_pilot02_contract(declaration)
        for changes in (dict(package=str(a.PILOT02_PACKAGE)), dict(precedingChargedSeconds=15360),
                        dict(precedingChargedBytes=11140071424), dict(reuseSealedQualification=False),
                        dict(timingResamples=1), dict(qualifiedAttemptReviewSha256='0'*64)):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                a.validate_pilot02_contract(dict(declaration, **changes))

    def test_reuse_rejects_history_or_runtime_drift_before_copying(self):
        proof = dict(harnessSha256='qualified')
        with patch.object(a,'qualified_pilot02',return_value=proof), patch.object(a,'read',return_value={'old':'history'}), \
             patch.object(a.shutil,'copytree') as copying:
            with self.assertRaises(ValueError): a.copy_qualified_pilot02(Path('unused'),{'changed':'history'},[],'qualified')
            with self.assertRaises(ValueError): a.copy_qualified_pilot02(Path('unused'),{'old':'history'},[],'changed')
            copying.assert_not_called()

    def test_identity_charges_limits_and_prior_failure_are_mandatory(self):
        declaration = dict(a.PILOT02_CONTRACT, backendTestCount=34, baselineBackendTestCount=184,
            repairHandoff=dict(sha256=a.REPAIR_HANDOFF_PIN))
        a.validate_pilot02_contract(declaration)
        for key in a.PILOT02_CONTRACT:
            with self.subTest(key=key), self.assertRaises(ValueError):
                a.validate_pilot02_contract({k:v for k,v in declaration.items() if k != key})
        for changes in (dict(output=str(a.OUTPUT)), dict(scopeId='proposal-affinity-pilot-01'),
                        dict(precedingChargedSeconds=4560), dict(precedingChargedBytes=0),
                        dict(retentionCheckRequired=False), dict(retries=1), dict(scientificLaunches=1),
                        dict(backendTestCount=28), dict(baselineBackendTestCount=34),
                        dict(repairHandoff=dict(sha256='0'*64))):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                a.validate_pilot02_contract(dict(declaration, **changes))

    def test_exact_retention_with_symbols_and_shared_cap_required(self):
        runtime = {'BalanceHarness.dll':'dll', 'BalanceHarness.pdb':'pdb', 'Domain.dll':'domain'}
        report = dict(status='RuntimeRetentionVerifiedNoReservation', fights=0, newValues=0,
            maximumBytes=a.RETENTION_BYTES, retainedBytes=100, runtimeFiles=3,
            harnessSha256='dll', symbolsSha256='pdb')
        a.validate_retention(report, runtime, runtime)
        for actual in ({k:v for k,v in runtime.items() if not k.endswith('.pdb')},
                       dict(runtime, **{'Domain.dll':'changed'}), dict(runtime, extra='unadmitted')):
            with self.subTest(actual=actual), self.assertRaises(ValueError): a.validate_retention(report,runtime,actual)
        for key,value in (('fights',1),('newValues',1),('symbolsSha256','wrong'),('runtimeFiles',2),
                          ('maximumBytes',a.RETENTION_BYTES+1),('retainedBytes',a.RETENTION_BYTES+1),('retainedBytes',0)):
            with self.subTest(key=key), self.assertRaises(ValueError):
                a.validate_retention(dict(report, **{key:value}), runtime, runtime)

    def test_only_two_retention_sources_can_differ_from_consumed_admission(self):
        old = {'LL/tools/BalanceHarness/'+name:'old' for name in
            ('TowerProposalStudyProtocol.cs','TowerProposalStudyRun.cs','TowerProposalStudyResources.cs','TowerProposalPolicies.cs')}
        new = dict(old)
        for name in ('TowerProposalStudyProtocol.cs','TowerProposalStudyRun.cs'): new['LL/tools/BalanceHarness/'+name]='repair'
        with patch.object(a, 'read', return_value=old):
            self.assertEqual(2, len(a.source_changes(new, True)))
            for name in ('TowerProposalStudyResources.cs','TowerProposalPolicies.cs'):
                with self.subTest(name=name), self.assertRaises(ValueError):
                    a.source_changes(dict(new, **{'LL/tools/BalanceHarness/'+name:'drift'}), True)

    def test_forecast_carries_all_consumed_floors_and_never_resamples(self):
        previous = dict(nativeSeconds=4239, auditSeconds=1301, nativeBytes=3000, auditBytes=400, totalBytes=3400)
        probe = dict(status='AuditPartitionStillNotSupported',timingMargin=2,publicationReserveSeconds=120,
            wholeWorkerSeconds=170,independentAuditSeconds=[35,15],extraInventorySeconds=370,
            inventoryByteRatio=4,auditPlanningSeconds=1300)
        estimate = dict(nativeSeconds=4100,nativeBytes=2800,auditBytes=300,totalBytes=3100)
        before = copy.deepcopy((previous,probe,estimate))
        result = a.pilot02_forecast(previous,probe,850,estimate)
        self.assertTrue(result['fits'])
        self.assertEqual((4239,1301,3000,400), tuple(result[k] for k in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes')))
        self.assertEqual(before,(previous,probe,estimate))
        result = a.pilot02_forecast(previous,probe,850,dict(estimate,totalBytes=6800))
        self.assertEqual(2040,result['auditSeconds']); self.assertFalse(result['fits'])

    def test_failed_launch_full_charge_and_archive_are_required(self):
        proof = dict(status='ScientificLaunchFailedBeforeEntropyAndCombat', fights=0,newValues=0,attempts=0,retries=0,
            scientificLaunches=1,failureBeforeReservation=True,admissionManifestSha256=a.CONSUMED_ADMISSION_PIN,
            totalRecordedChargedSeconds=15360,totalRecordedChargedBytes=11140071424)
        def read(path): return proof if path.name == 'verification.json' else {'failure.json':'pinned'}
        with patch.object(a.base,'authenticate'), patch.object(a,'read',side_effect=read), \
             patch.object(a,'inventory',return_value={'failure.json':'pinned'}) as files:
            self.assertEqual(15360, a.pilot02_preceding()['chargedSeconds'])
            proof['totalRecordedChargedSeconds']=4560
            with self.assertRaises(ValueError): a.pilot02_preceding()
            proof['totalRecordedChargedSeconds']=15360; files.return_value={'failure.json':'changed'}
            with self.assertRaises(ValueError): a.pilot02_preceding()


if __name__ == '__main__': unittest.main(verbosity=2)
