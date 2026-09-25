"""Literal confirmation, independent recount and owned-publication tests; never scientific combat."""
import argparse
from contextlib import contextmanager
import copy
import importlib.util
import json
import os
from pathlib import Path
import struct
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]


def module(name,path):
    spec = importlib.util.spec_from_file_location(name,path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


audit = module('confirmation_audit',ROOT/'Balance Harness/analysis/audit-frozen-pool-recognition.py')
launcher = module('confirmation_launcher',ROOT/'build/run-frozen-pool-recognition.py')


class ProtocolTests(unittest.TestCase):
    plan = audit.read(ROOT/'Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json')

    def rows(self):
        counts = [70,80,100,140,140,90,90,180,180]
        return [dict(partyId=audit.cell_id(r*9+i,t),trials=[dict(seed=r*256+n,outcome='Victory' if n < counts[i] else 'Draw')
                for n in range(256)]) for r,root in enumerate(self.plan['roots']) for i,t in enumerate(root['teams'])]

    def test_plan_and_weighted_estimands(self):
        self.assertEqual(audit.PLAN,audit.sha(ROOT/'Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json'))
        result = audit.endpoint(self.plan,self.rows(),list(range(3072)))
        self.assertEqual((108,216,36,132),tuple(len(result[k]) for k in ('rates','contrasts','strata','unmeasured')))
        self.assertTrue(all(t['independentOutcome'] is None for t in result['unmeasured']))
        self.assertTrue(all(abs(p['estimatedCandidateMeanGain']-(80-20+6.5*160)/(256*17)) < 1e-14 for p in result['populations']))
        self.assertFalse(result['policyDefaultsChanged']); self.assertEqual('CompleteDiagnosticOnly',result['decision'])
        self.assertNotIn('qualifyingPartyIds',result)
        from statistics import NormalDist
        self.assertAlmostEqual(NormalDist().inv_cdf(1-.025/540),audit.Z,7)

    def test_entropy_and_permanent_tail(self):
        values = [-987,-2**31,-2**31]+list(range(6141))
        words,panel,fresh = audit.classify(struct.pack('<6144i',*values),[-987])
        self.assertEqual((3072,6142,3070),(len(panel),len(fresh),sum(w['classification']=='ReservedUnused' for w in words)))
        self.assertEqual(-2**31,panel[0]); self.assertEqual('DuplicateBatch',words[2]['classification'])
        self.assertEqual(1,len(audit.classify(bytes(24576),[-987])[1]))
        for entropy,history in [(bytes(24575),[-987]),(bytes(24576),[-987,-987]),(bytes(24576),[True])]:
            with self.assertRaises(ValueError): audit.classify(entropy,history)

    def test_pairing_identity_faults_and_omissions_fail(self):
        original = self.rows()
        for change in ('rows','cross-root','missing','duplicate','fault','identity'):
            rows = copy.deepcopy(original)
            if change == 'rows': rows.reverse()
            if change == 'cross-root': rows[9]['trials'] = rows[0]['trials']
            if change == 'missing': rows[7]['trials'].pop()
            if change == 'duplicate': rows[7]['trials'][1] = rows[7]['trials'][0]
            if change == 'fault': rows[7]['trials'][0]['outcome'] = 'Faulted'
            if change == 'identity': rows[9]['partyId'] = rows[0]['partyId']
            with self.assertRaises(ValueError): audit.endpoint(self.plan,rows,list(range(3072)))
        with self.assertRaises(ValueError): audit.endpoint(self.plan,original,list(range(256))*12)


@unittest.skipUnless(os.name == 'nt','Windows ownership required')
class OwnerTests(unittest.TestCase):
    def test_owned_job_waits_for_and_terminates_a_surviving_descendant(self):
        from bounded_windows_process import run
        with tempfile.TemporaryDirectory() as folder:
            code = "import subprocess,sys; subprocess.Popen([sys.executable,'-c','import time; time.sleep(30)'],creationflags=0x08000000)"
            # Production children run in a retained runtime directory. Windows may
            # briefly retain an exiting descendant's cwd after job accounting is zero.
            result = run([sys.executable,'-B','-c',code],str(ROOT),str(Path(folder)/'process.log'),time.monotonic()+2,cleanup_seconds=1)
            self.assertTrue(result['timedOut'])
            self.assertEqual(0,result['activeProcesses'])
            self.assertGreaterEqual(result['totalProcesses'],2)

    def test_lease_is_exclusive_and_removed_on_close(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)/'registry'
            with launcher.writer_lease(root):
                with self.assertRaises(ValueError):
                    with launcher.writer_lease(root):
                        self.fail('Concurrent lease')
            self.assertFalse(Path(str(root)+'.writer.lock').exists())

    def exercise(self,failure=None):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); registry = root/'registry'; registry.mkdir(); output = registry/'run'
            harness = root/'BalanceHarness.dll'; harness.write_bytes(b'literal')
            auditor = ROOT/'Balance Harness/analysis/audit-frozen-pool-recognition.py'
            q = dict(version=launcher.VERSION,outputRoot=str(output),registryRoot=str(registry),maximumSeconds=7800,maximumBytes=4294967296,
                     priorSeconds=600,priorBytes=536870912,recognition=dict(auditorPath=str(auditor),auditorHash=launcher.digest(auditor)))
            request = root/'request.json'; launcher.write(request,q); calls=[]
            def run(command,cwd,log,deadline,**kwargs):
                calls.append((command,deadline)); Path(log).write_text('{}')
                kwargs['check']()
                # Verify both leases remain held through the final barrier.
                with self.assertRaises(ValueError):
                    with launcher.writer_lease(registry/'complete-family-allocation'): pass
                value = dict(mechanism='suspended-owned-job-v1',exitCode=0,activeProcesses=0,totalProcesses=1,timedOut=False,seconds=0)
                if len(calls) == 1:
                    launcher.write(output/'native-receipt.json',dict(version=launcher.VERSION,status='Verified',requestFileHash=launcher.digest(output/'request.json'),newAuditFights=0,fights=27648))
                    launcher.write(output/'provisional-result.json',{})
                    launcher.write(output/'proposed-teams.json',{})
                    (output/'proposed-recognition.md').write_text('literal')
                if failure == len(calls): return dict(value,exitCode=1)
                if len(calls) == 3:
                    launcher.write(output/'independent-audit.json',dict(status='Passed',requestFileHash=launcher.digest(output/'request.json'),newFights=0,newValues=0,result={}))
                if failure == 'mutation' and len(calls) == 4:
                    (output/'proposed-teams.json').write_text('{"changed":true}')
                return value
            original_write = launcher.write
            def write(path,value):
                if path.name == str(failure)+'.json': raise OSError('Injected terminal write failure')
                original_write(path,value)
            with patch('bounded_windows_process.run',side_effect=run), patch.object(launcher,'write',side_effect=write), patch.object(launcher,'validate_admission'):
                if failure:
                    with self.assertRaises((ValueError,OSError)): launcher.launch(request,harness,'dotnet')
                    self.assertTrue((output/'failure.json').exists())
                    self.assertFalse((output/'closeout.json').exists())
                    if failure not in ('completion','files','closeout'):
                        self.assertFalse((output/'completion.json').exists())
                        self.assertFalse((output/'teams.json').exists())
                else:
                    launcher.launch(request,harness,'dotnet')
                    self.assertEqual(4,len(calls)); self.assertEqual(calls[1][1],calls[2][1]); self.assertEqual(calls[2][1],calls[3][1])
                    result = audit.read(output/'completion.json')
                    self.assertEqual(600,result['chargedSeconds']-result['seconds'])
                    self.assertEqual(536870912,result['chargedBytes']-result['observedBytes'])
                    final = audit.read(output/'closeout.json')
                    self.assertEqual(final['filesHash'],audit.sha(output/'files.json'))
                    self.assertEqual(final['retainedBytes'],sum(p.stat().st_size for p in output.rglob('*') if p.is_file()))
                    self.assertGreaterEqual(final['measuredSeconds'],result['seconds'])
                    self.assertEqual(final['chargedBytes'],final['retainedBytes']+536870912)
                    files = audit.read(output/'files.json')
                    self.assertEqual(set(files)|{'files.json','closeout.json'},{p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()})
                    self.assertTrue(all(audit.sha(output/name) == pin for name,pin in files.items()))
                with self.assertRaises(ValueError): launcher.launch(request,harness,'dotnet')

    def test_complete_owner_uses_same_audit_deadline_and_includes_seal_bytes(self): self.exercise()
    def test_failed_native_has_no_publication(self): self.exercise(1)
    def test_failed_native_audit_has_no_publication(self): self.exercise(2)
    def test_failed_independent_audit_has_no_publication(self): self.exercise(3)
    def test_failed_barrier_has_no_publication(self): self.exercise(4)
    def test_mutated_proposals_cannot_publish(self): self.exercise('mutation')
    def test_native_storage_failure_has_no_publication(self):
        with patch.object(launcher,'NATIVE_BYTES',1): self.exercise('storage')
    def test_audit_storage_reserve_cannot_borrow_unused_native_allowance(self):
        with patch.object(launcher,'AUDIT_BYTES',1): self.exercise('storage')
    def test_interrupted_completion_is_failed(self): self.exercise('completion')
    def test_interrupted_seal_is_failed(self): self.exercise('files')
    def test_interrupted_terminal_receipt_is_failed(self): self.exercise('closeout')


class SavedRowsTests(unittest.TestCase):
    fixture = None
    def test_full_native_fixture_recounts_independently_and_rejects_report_reordering(self):
        root = Path(self.fixture)/'result'
        before = {p:audit.sha(p) for p in root.rglob('*') if p.is_file()}
        result = audit.audit(root)
        self.assertEqual('Passed',result['status']); self.assertEqual(216,len(result['result']['contrasts']))
        self.assertEqual(before,{p:audit.sha(p) for p in root.rglob('*') if p.is_file()})
        trials = root/'study/trials.jsonl'; original = trials.read_bytes()
        try:
            lines=original.splitlines(keepends=True); lines[0],lines[1]=lines[1],lines[0]; trials.write_bytes(b''.join(lines))
            # Direct rows must reject even if a caller were to re-sign a surrounding inventory.
            with self.assertRaisesRegex(ValueError,'ordinal'):
                audit.audit_rows(root/'study',audit.read(root/'source-definition.json')['teams'],audit.read(root/'confirmation-binding.json')['panel'])
        finally:
            trials.write_bytes(original)


class AdmissionTests(unittest.TestCase):
    prepare = module('recognition_preparation',ROOT/'Balance Harness/analysis/prepare-frozen-pool-recognition.py')

    def test_runtime_substitution_is_limited_to_four_harness_files(self):
        tested = {n:'new-'+n for n in self.prepare.HARNESS}
        captured = {'runtime/Services.LL.dll':'captured',**{'runtime/'+n:'old' for n in tested}}
        actual = {'Services.LL.dll':'captured',**tested}
        self.prepare.validate_runtime(actual,captured,tested)
        for bad in (dict(actual,**{'Services.LL.dll':'changed'}),dict(actual,extra='x'),{n:v for n,v in actual.items() if n != 'Services.LL.dll'}):
            with self.assertRaises(ValueError): self.prepare.validate_runtime(bad,captured,tested)

    def test_forecast_rejects_changed_source_or_cap_overflow(self):
        native = dict(fights=23680,status='Verified',measuredSeconds=988,observedBytes=1664474382)
        completion = dict(status='Complete',seconds=1130)
        result = self.prepare.forecast(native,completion)
        self.assertTrue(result['fits']); self.assertFalse(result['guaranteedUpperBound'])
        for bad in (dict(native,fights=44000),dict(native,observedBytes=4*1024**3),dict(native,measuredSeconds=6100)):
            with self.assertRaises(ValueError): self.prepare.forecast(bad,completion)

    def test_pending_recovery_and_full_history_are_preserved_in_request(self):
        with tempfile.TemporaryDirectory() as folder:
            p = Path(folder)
            for name in ('admission-charge.json','definition.json','auditor.py'): (p/name).write_text('{}')
            old = dict(pendingHistoryRecoveries={'pending':'receipt'},recoveryReceiptHashes={'receipt':'pin'})
            q = self.prepare.request(p,p/'registry/run',{'ledger':'hash'},old)
            self.assertEqual(old['pendingHistoryRecoveries'],q['pendingHistoryRecoveries'])
            self.assertEqual(old['recoveryReceiptHashes'],q['recoveryReceiptHashes'])
            self.assertEqual({'ledger':'hash'},q['requiredHistory'])
            self.assertEqual((7800,600,4294967296,536870912),(q['maximumSeconds'],q['priorSeconds'],q['maximumBytes'],q['priorBytes']))
            (p/'preceding-admission-charge.json').write_text('{"failed":true}')
            amended = self.prepare.request(p,p/'registry/run',{'ledger':'hash'},old)
            self.assertEqual((8400,1200,4831838208,1073741824),(amended['maximumSeconds'],amended['priorSeconds'],amended['maximumBytes'],amended['priorBytes']))
            self.assertEqual(2,len(amended['priorCharges'])); launcher.validate_request(amended)
            with self.assertRaises(ValueError): launcher.validate_request(dict(amended,priorSeconds=600))

    def test_successful_capture_is_the_exact_retained_pilot_runtime(self):
        captured = self.prepare.authenticate(self.prepare.CAPTURE,self.prepare.CAPTURE_PIN)
        self.assertEqual('adaptive-racing-pilot-02-admission-20260923',self.prepare.CAPTURE.name)
        runtime = {n[8:]:p for n,p in captured.items() if n.startswith('runtime/')}
        context = audit.read(self.prepare.CAPTURE/'context.json')
        self.assertTrue(all(runtime[n+'.dll'] == pin for n,pin in context['execution']['assemblyHashes'].items()))
        self.assertEqual(ProtocolTests.plan['capturedRuntime']['executionHash'],context['executionHash'])

    def test_launcher_requires_external_pin_exact_inventory_and_admitted_runtime(self):
        with tempfile.TemporaryDirectory() as folder:
            p = Path(folder); (p/'runtime').mkdir(); harness=p/'runtime/BalanceHarness.dll'; harness.write_bytes(b'literal')
            request = p/'request.json'; launcher.write(request,{})
            launcher.write(p/'admission.json',dict(status='RecognitionAdmittedNoReservation',version=launcher.VERSION,
                requestSha256=launcher.digest(request),fights=0,newValues=0))
            import shutil
            for name in ('run-frozen-pool-recognition.py','bounded_windows_process.py'): shutil.copyfile(ROOT/'build'/name,p/name)
            launcher.write(p/'files.json',{f.relative_to(p).as_posix():launcher.digest(f) for f in launcher.inventory(p)})
            pin=launcher.digest(p/'files.json'); launcher.validate_admission(request,harness,pin)
            with self.assertRaises(ValueError): launcher.validate_admission(request,harness,'0'*64)
            with self.assertRaises(ValueError): launcher.validate_admission(request,p/'BalanceHarness.dll',pin)
            (p/'extra').write_bytes(b'x')
            with self.assertRaises(ValueError): launcher.validate_admission(request,harness,pin)

    def test_resealed_false_weight_or_imputed_outcome_is_not_an_endpoint(self):
        plan = ProtocolTests.plan; rows = ProtocolTests().rows()
        result = audit.endpoint(plan,rows,list(range(3072)))
        changed = copy.deepcopy(result); changed['unmeasured'][0]['independentOutcome'] = 0
        self.assertFalse(audit.same(changed,result))
        changed = copy.deepcopy(result); changed['strata'][2]['inclusionWeight'] = 1
        self.assertFalse(audit.same(changed,result))


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--fixture'); args=parser.parse_args()
    SavedRowsTests.fixture=args.fixture
    classes=[ProtocolTests,OwnerTests,AdmissionTests]+([SavedRowsTests] if args.fixture else [])
    suite=unittest.TestSuite(unittest.defaultTestLoader.loadTestsFromTestCase(c) for c in classes)
    sys.exit(not unittest.TextTestRunner(verbosity=2).run(suite).wasSuccessful())
