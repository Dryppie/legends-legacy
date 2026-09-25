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


audit = module('confirmation_audit',ROOT/'Balance Harness/analysis/audit-three-reference-confirmation.py')
launcher = module('confirmation_launcher',ROOT/'build/run-three-reference-confirmation.py')


class ProtocolTests(unittest.TestCase):
    def test_plan_and_fixed_envelope(self):
        self.assertEqual(audit.PLAN,audit.sha(ROOT/'Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json'))
        self.assertEqual((7800,4294967296,7200,3758096384,6000,3221225472,1200,536870912),
            (launcher.CUMULATIVE_SECONDS,launcher.CUMULATIVE_BYTES,launcher.SECONDS,launcher.BYTES,launcher.NATIVE_SECONDS,
             launcher.NATIVE_BYTES,launcher.AUDIT_SECONDS,launcher.AUDIT_BYTES))

    def test_entropy_collisions_signed_values_tail_and_shortfall(self):
        values = [-987,-2**31,-2**31]+list(range(12997))
        words,panel,fresh = audit.classify(struct.pack('<13000i',*values),[-987])
        self.assertEqual((6500,12998,6498),(len(panel),len(fresh),sum(w['classification']=='ReservedUnused' for w in words)))
        self.assertEqual(-2**31,panel[0])
        self.assertEqual('DuplicateBatch',words[2]['classification'])
        self.assertEqual(1,len(audit.classify(bytes(52000),[-987])[1]))
        for entropy,history in [(bytes(44000),[-987]),(bytes(52000),[-987,-987]),(bytes(52000),list(range(987001)))]:
            with self.assertRaises(ValueError):
                audit.classify(entropy,history)

    def rows(self,counts):
        return [dict(partyId=str(i),trials=[dict(seed=n,outcome='Victory' if n < c else 'Draw') for n in range(6500)]) for i,c in enumerate(counts)]

    def test_thresholds_all_references_and_recommendation_order(self):
        ids,panel = list(map(str,range(8))),list(range(6500))
        for counts,qualifiers in [([727]+[0]*7,[]),([728]+[0]*7,['0']),([1324]+[1000]*7,[]),
                                  ([1325]+[1000]*7,['0']),([1600]+[1000]*6+[1600],[]),([2500]*5+[2000]*3,ids[:5])]:
            with self.subTest(counts=counts):
                result = audit.endpoint(ids,self.rows(counts),panel)
                self.assertEqual(qualifiers,result['qualifyingPartyIds'])
                self.assertEqual(qualifiers+ids[5:],result['recommendedPartyIds'])
                self.assertEqual(15,len(result['contrasts']))

    def test_no_silent_pairing_or_fault_to_loss_conversion(self):
        ids,panel = list(map(str,range(8))),list(range(6500))
        original = self.rows([2000]*8)
        for change in ('rows','seeds','missing','duplicate','fault'):
            rows = copy.deepcopy(original)
            if change == 'rows': rows.reverse()
            if change == 'seeds': rows[7]['trials'].reverse()
            if change == 'missing': rows[7]['trials'].pop()
            if change == 'duplicate': rows[7]['trials'][1] = rows[7]['trials'][0]
            if change == 'fault': rows[7]['trials'][0]['outcome'] = 'Faulted'
            with self.assertRaises(ValueError):
                audit.endpoint(ids,rows,panel)


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
            auditor = ROOT/'Balance Harness/analysis/audit-three-reference-confirmation.py'
            q = dict(version=launcher.VERSION,outputRoot=str(output),registryRoot=str(registry),maximumSeconds=7800,maximumBytes=4294967296,
                     priorSeconds=600,priorBytes=536870912,threeReference=dict(auditorPath=str(auditor),auditorHash=launcher.digest(auditor)))
            request = root/'request.json'; launcher.write(request,q); calls=[]
            def run(command,cwd,log,deadline,**kwargs):
                calls.append((command,deadline)); Path(log).write_text('{}')
                kwargs['check']()
                # Verify both leases remain held through the final barrier.
                with self.assertRaises(ValueError):
                    with launcher.writer_lease(registry/'complete-family-allocation'): pass
                value = dict(mechanism='suspended-owned-job-v1',exitCode=0,activeProcesses=0,totalProcesses=1,timedOut=False,seconds=0)
                if len(calls) == 1:
                    launcher.write(output/'native-receipt.json',dict(version=launcher.VERSION,status='Verified',requestFileHash=launcher.digest(output/'request.json'),newAuditFights=0,fights=52000))
                    launcher.write(output/'provisional-result.json',{})
                    launcher.write(output/'proposed-teams.json',{})
                    (output/'proposed-confirmation.md').write_text('literal')
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
            with patch('bounded_windows_process.run',side_effect=run), patch.object(launcher,'write',side_effect=write):
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
        self.assertEqual('Passed',result['status']); self.assertEqual(2,len(result['result']['qualifyingPartyIds']))
        self.assertEqual(before,{p:audit.sha(p) for p in root.rglob('*') if p.is_file()})
        trials = root/'study/trials.jsonl'; original = trials.read_bytes()
        try:
            lines=original.splitlines(keepends=True); lines[0],lines[1]=lines[1],lines[0]; trials.write_bytes(b''.join(lines))
            # Direct rows must reject even if a caller were to re-sign a surrounding inventory.
            with self.assertRaisesRegex(ValueError,'ordinal'):
                audit.audit_rows(root/'study',audit.read(root/'source-definition.json')['teams'],audit.read(root/'confirmation-binding.json')['panel'])
        finally:
            trials.write_bytes(original)


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--fixture'); args=parser.parse_args()
    SavedRowsTests.fixture=args.fixture
    classes=[ProtocolTests,OwnerTests]+([SavedRowsTests] if args.fixture else [])
    suite=unittest.TestSuite(unittest.defaultTestLoader.loadTestsFromTestCase(c) for c in classes)
    sys.exit(not unittest.TextTestRunner(verbosity=2).run(suite).wasSuccessful())
