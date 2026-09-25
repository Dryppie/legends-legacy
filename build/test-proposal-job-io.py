"""Terminal Windows job I/O diagnostics; tiny literal processes, no science."""
import copy
import ctypes
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as w
import bounded_windows_process as owned


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class JobIO(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def query(self, active=0, total=1, amounts=(1,2,3,4,5,6)):
        def fill(job, kind, pointer, size, returned):
            self.assertEqual(kind,8); self.assertEqual(size,ctypes.sizeof(owned._AccountingIo))
            value = ctypes.cast(pointer,ctypes.POINTER(owned._AccountingIo)).contents
            value.basic.active_processes = active; value.basic.total_processes = total
            for (name,_),amount in zip(owned._Io._fields_,amounts): setattr(value.io,name,amount)
            return 1
        return fill

    def known(self):
        with patch.object(owned,'_query_job',side_effect=self.query()):
            return owned._job_io_observation(1,True,True)

    def observation(self, io):
        return dict(version='tower-owned-process-observation-v1',includesHandleCleanup=True,usableForAdmission=False,
            jobDrained=True,enclosingSeconds=1,peakJobCommitBytes=None,ownerLifetimePeakCommitBytes=None,
            combinedCommitUpperBoundBytes=None,memoryCoverage='Unknown',exitCode=0,timedOut=False,ownerError=None,jobIo=io)

    def validate(self, observation):
        ledger = w.OwnerLedger('a'*64,'b'*64); ledger.begin('native')
        ledger.observe_process(observation,sha(owned.__file__))
        return ledger.processes[0]

    def run_child(self, script='print("done")', timeout=5, check=None):
        observations = []
        result = owned.run([sys.executable,'-B','-c',script],self.root,self.root/'child.log',time.monotonic()+timeout,
                           check=check,observe=observations.append)
        self.validate(observations[0])
        return result,observations[0]

    def test_windows_layout_and_full_width_fields(self):
        self.assertEqual(ctypes.sizeof(owned._Accounting),48)
        self.assertEqual(owned._AccountingIo.io.offset,48)
        self.assertEqual(ctypes.sizeof(owned._AccountingIo),96)
        amounts = tuple(2**40+n for n in range(6))
        with patch.object(owned,'_query_job',side_effect=self.query(amounts=amounts)):
            value = owned._job_io_observation(1,True,True)
        self.assertEqual(list(value['counters'].values()),list(amounts))
        self.validate(self.observation(value))

    def test_unassigned_or_undrained_job_is_unknown_without_query(self):
        for assigned,drained in ((False,False),(False,True),(True,False)):
            with self.subTest(assigned=assigned,drained=drained), patch.object(owned,'_query_job') as query:
                value = owned._job_io_observation(1,assigned,drained)
                query.assert_not_called(); self.assertEqual(value['coverage'],'Unknown')
                self.assertIsNone(value['counters']); self.validate(self.observation(value))

    def test_query_failure_is_unknown_not_zero(self):
        for error in (OSError('query unavailable'),OSError()):
            with self.subTest(error=error),patch.object(owned,'_query_job',side_effect=error):
                value = owned._job_io_observation(1,True,True)
            self.assertEqual(value['coverage'],'Unknown'); self.assertIsNone(value['counters'])
            self.assertTrue(value['error']); self.validate(self.observation(value))

    def test_inconsistent_query_state_cannot_claim_terminal_totals(self):
        for active,total in ((1,1),(0,0)):
            with self.subTest(active=active,total=total),patch.object(owned,'_query_job',side_effect=self.query(active,total)):
                value = owned._job_io_observation(1,True,True)
            self.assertEqual(value['coverage'],'Unknown'); self.assertIsNone(value['activeProcessesAtQuery'])
            self.assertIsNone(value['totalProcessesAtQuery']); self.validate(self.observation(value))

    def test_observed_zero_counters_are_distinct_from_unknown(self):
        with patch.object(owned,'_query_job',side_effect=self.query(amounts=(0,)*6)):
            value = owned._job_io_observation(1,True,True)
        self.assertEqual(value['coverage'],'KernelJobLifetimeTotals')
        self.assertEqual(sum(value['counters'].values()),0); self.assertIsNone(value['error'])
        self.validate(self.observation(value))

    def test_real_worker_receipt_and_late_descendant_are_observed_after_exit(self):
        export = os.environ.get('LL_JOB_IO_EXPORT')
        if export: self.root = Path(export); self.root.mkdir()
        study = self.root/'study'; study.mkdir(); (study/'request.json').write_bytes(b'{"fixtureOnly":true}')
        worker = self.root/'literal-worker.py'; binding = self.root/'binding.json'; receipt = self.root/'worker.json'
        child = "import pathlib,sys,time; time.sleep(.08); p=pathlib.Path('late.tmp'); p.write_bytes(b'z'*65536); p.unlink(); print('descendant-after-receipt',flush=True)"
        worker.write_text('import sys,subprocess,os\nfrom pathlib import Path\nsys.path.insert(0,'+repr(str(ROOT/'Balance Harness/analysis'))+')\n'
            'from proposal_work_accounting import worker_receipt\n'
            'with worker_receipt(sys.argv[1],"native",sys.argv[2],sys.argv[3],__file__) as work:\n'
            '    p=Path("scratch.tmp"); p.write_bytes(b"x"*32768); p.read_bytes(); p.unlink()\n'
            'subprocess.Popen([sys.executable,"-B","-c",'+repr(child)+'])\n'
            'print("root-after-receipt",flush=True)\n',encoding='utf-8')
        w.seal_new(binding,dict(version=w.WORKER_BINDING_VERSION,phase='native',studyRoot=str(study),
            requestSha256=sha(study/'request.json'),producerSha256=sha(worker),accountingModuleSha256=sha(w.__file__),receiptPath=str(receipt)))
        events = []; original_query,original_close = owned._query_job,owned._close
        def query(*args):
            if args[1]==8: events.append('io')
            return original_query(*args)
        def close(*args): events.append('close'); return original_close(*args)
        observations = []
        def observe(value): events.append('observe'); observations.append(value)
        with patch.object(owned,'_query_job',side_effect=query),patch.object(owned,'_close',side_effect=close):
            result = owned.run([sys.executable,'-B',str(worker),str(study),str(binding),sha(binding)],self.root,self.root/'child.log',
                               time.monotonic()+8,observe=observe)
        self.assertEqual(result['exitCode'],0,(self.root/'child.log').read_text())
        self.assertGreaterEqual(result['totalProcesses'],2); self.assertEqual(events,['io','close','close','close','observe'])
        observation = observations[0]; observed = observation['jobIo']; self.validate(observation)
        self.assertEqual(observed['totalProcessesAtQuery'],result['totalProcesses'])
        self.assertEqual(observed['coverage'],'KernelJobLifetimeTotals')
        self.assertGreaterEqual(observed['counters']['writeTransferBytes'],32768+65536+receipt.stat().st_size)
        self.assertGreaterEqual(observed['counters']['readTransferBytes'],32768)
        self.assertFalse((self.root/'scratch.tmp').exists()); self.assertFalse((self.root/'late.tmp').exists())
        self.assertIn('descendant-after-receipt',(self.root/'child.log').read_text())
        self.assertEqual(w.strict(receipt.read_bytes())['outcome'],'Complete')
        if export:
            w.seal_new(self.root/'observation.json',observation)
            w.seal_new(self.root/'process.json',result)
            w.seal_new(self.root/'fixture.json',dict(fixtureOnly=True,realJobObservation=True,actualCombat=0,productionEntropyDraws=0,
                deletedLiteralFileBytes=32768+65536,observationOrder=events,wholeProcessCoverage=False,usableForAdmission=False,
                sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p) for p in (owned.__file__,w.__file__,__file__)}))
            w.seal_new(self.root/'files.json',{p.relative_to(self.root).as_posix():sha(p) for p in sorted(self.root.rglob('*')) if p.is_file()})

    def test_real_query_failure_does_not_mask_success_or_memory(self):
        original = owned._query_job
        def query(*args):
            if args[1]==8: raise OSError('literal io query failure')
            return original(*args)
        with patch.object(owned,'_query_job',side_effect=query): result,observation = self.run_child()
        self.assertEqual(result['exitCode'],0); self.assertEqual(observation['jobIo']['coverage'],'Unknown')
        self.assertEqual(observation['memoryCoverage'],'KernelJobHighWaterPlusOwnerLifetimeHighWater')

    def test_memory_failure_does_not_erase_job_io(self):
        with patch.object(owned,'_memory_info',side_effect=OSError('literal memory failure')):
            result,observation = self.run_child()
        self.assertEqual(result['exitCode'],0); self.assertEqual(observation['memoryCoverage'],'Unknown')
        self.assertEqual(observation['jobIo']['coverage'],'KernelJobLifetimeTotals')

    def test_owner_error_survives_io_query_failure_and_cleanup(self):
        original = owned._query_job; error = RuntimeError('owner failed'); observations = []
        def query(*args):
            if args[1]==8: raise OSError('io query failed')
            return original(*args)
        def fail(): raise error
        with patch.object(owned,'_query_job',side_effect=query),self.assertRaises(RuntimeError) as caught:
            owned.run([sys.executable,'-B','-c','import time; time.sleep(20)'],self.root,self.root/'child.log',
                      time.monotonic()+5,check=fail,observe=observations.append)
        self.assertIs(caught.exception,error)
        self.assertTrue(observations[0]['jobDrained']); self.validate(observations[0])
        self.assertEqual(observations[0]['jobIo']['coverage'],'Unknown')

    def test_timeout_keeps_terminal_job_totals_without_claiming_worker_success(self):
        result,observation = self.run_child('import time; print("started",flush=True); time.sleep(20)',timeout=.3)
        self.assertTrue(result['timedOut']); self.assertTrue(observation['jobDrained'])
        self.assertEqual(observation['jobIo']['coverage'],'KernelJobLifetimeTotals')
        self.assertFalse(observation['jobIo']['usableForAdmission'])

    def test_default_call_does_not_query_or_change_return_contract(self):
        with patch.object(owned,'_job_io_observation') as query:
            result = owned.run([sys.executable,'-B','-c','pass'],self.root,self.root/'child.log',time.monotonic()+5)
        query.assert_not_called(); self.assertNotIn('jobIo',result)
        self.assertEqual(result['exitCode'],0)

    def test_unverified_suspended_child_is_cleaned_up_with_unknown_job_io(self):
        observations = []
        with patch.object(owned,'_is_in_job',side_effect=OSError('membership query failed')),self.assertRaisesRegex(OSError,'membership query failed'):
            owned.run([sys.executable,'-B','-c','raise Exception("must never run")'],self.root,self.root/'child.log',
                      time.monotonic()+5,observe=observations.append)
        self.assertFalse(observations[0]['jobDrained'])
        self.assertEqual(observations[0]['jobIo']['coverage'],'Unknown'); self.validate(observations[0])

    def test_historical_observation_without_io_block_is_still_supported(self):
        observation = self.observation(self.known()); del observation['jobIo']
        self.assertEqual(self.validate(observation)['observation'],observation)

    def test_invalid_io_claims_are_rejected_before_retention(self):
        baseline = self.observation(self.known())
        changes = [lambda v:v.update(jobIo=None),lambda v:v.update(jobDrained=False),
            lambda v:v['jobIo'].update(coverage='Complete'),lambda v:v['jobIo'].update(wholeProcessCoverage=True),
            lambda v:v['jobIo'].update(usableForAdmission=True),lambda v:v['jobIo'].update(extra=1),
            lambda v:v['jobIo'].update(activeProcessesAtQuery=True),lambda v:v['jobIo'].update(totalProcessesAtQuery=0),
            lambda v:v['jobIo'].update(boundary='AfterClose'),lambda v:v['jobIo'].update(error='failed'),
            lambda v:v['jobIo']['counters'].update(readTransferBytes=-1),lambda v:v['jobIo']['counters'].update(readTransferBytes=True),
            lambda v:v['jobIo']['counters'].update(readTransferBytes=2**64),lambda v:v['jobIo']['counters'].pop('writeOperations')]
        for i,change in enumerate(changes):
            value = copy.deepcopy(baseline); change(value)
            with self.subTest(case=i),self.assertRaises(ValueError): self.validate(value)

    def test_unknown_io_cannot_smuggle_zero_measurements(self):
        unknown = owned._job_io_observation(1,False,False)
        for change in ({'counters':{}},{'activeProcessesAtQuery':0},{'totalProcessesAtQuery':0},{'error':None},{'error':''}):
            value = copy.deepcopy(unknown); value.update(change)
            with self.subTest(change=change),self.assertRaises(ValueError): self.validate(self.observation(value))

    def test_retained_io_is_detached_from_caller_and_not_added_to_application_counters(self):
        observation = self.observation(self.known()); retained = self.validate(observation)
        pin = retained['observationSha256']; observation['jobIo']['counters']['readTransferBytes'] = 999
        self.assertEqual(retained['observation']['jobIo']['counters']['readTransferBytes'],4)
        self.assertEqual(hashlib.sha256(w.canonical(retained['observation'])).hexdigest(),pin)
        self.assertNotIn('counters',retained)


if __name__ == '__main__': unittest.main()
