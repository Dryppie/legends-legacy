"""Failure-safe owned job cleanup; literal workers and injected API faults only."""
import copy
import ctypes as c
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from contextlib import ExitStack
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import bounded_windows_process as owned
import proposal_work_accounting as work
import proposal_owner_process as monitor


class Cleanup(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup)
        self.root=Path(temp.name);self.observed=[];self.closed=[]

    def run_child(self, code='pass', **options):
        return owned.run([sys.executable,'-B','-c',code],self.root,self.root/'console.log',time.monotonic()+8,
                         observe=self.observed.append,**options)

    def fail_check(self, error):
        def check():raise error
        return check

    def close_failure(self, positions):
        original=owned._close
        def close(handle):
            self.closed.append(handle)
            result=original(handle)
            self.assertTrue(result)
            if len(self.closed) in positions:
                c.set_last_error(6)
                return 0
            return result
        return patch.object(owned,'_close',close)

    def validate(self, observation=None):
        value=self.observed[0] if observation is None else observation
        ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native');ledger.observe_process(value,'c'*64)
        return value['cleanup']

    def test_success_reports_all_three_closes(self):
        self.assertEqual(self.run_child()['exitCode'],0)
        cleanup=self.validate()
        self.assertEqual(cleanup['handleCloses'],dict(thread='Closed',process='Closed',job='Closed'))
        self.assertTrue(cleanup['allAcquiredHandlesClosed']);self.assertEqual(cleanup['errors'],[])
        self.assertTrue(cleanup['observerInvocationExcluded']);self.assertFalse(cleanup['wallClockBounded'])

    def test_caller_handled_exception_does_not_mark_success_failed(self):
        try:raise RuntimeError('already handled by caller')
        except RuntimeError:
            self.assertEqual(self.run_child()['exitCode'],0)
        self.assertIsNone(self.observed[0]['ownerError']);self.assertEqual(self.validate()['errors'],[])

    def test_caller_handled_exception_cannot_suppress_close_failure(self):
        try:raise RuntimeError('already handled by caller')
        except RuntimeError:
            with self.close_failure({1}),self.assertRaises(OSError):self.run_child()
        self.assertEqual(len(self.closed),3);self.assertNotEqual(self.observed[0]['ownerError'],'already handled by caller')
        self.validate()

    def pipe_close_failure(self):
        original=os.close;calls=[]
        def close(descriptor):
            original(descriptor);calls.append(descriptor)
            if len(calls)==2:raise OSError('literal pipe close failure')
        return patch.object(owned.os,'close',close)

    def test_caller_handled_exception_cannot_suppress_pipe_close_failure(self):
        try:raise RuntimeError('already handled by caller')
        except RuntimeError:
            with self.pipe_close_failure(),self.assertRaisesRegex(OSError,'literal pipe close failure'):
                self.run_child(log_byte_limit=65536)
        self.assertTrue(self.validate()['allAcquiredHandlesClosed'])

    def test_pipe_close_failure_preserves_body_error(self):
        error=RuntimeError('owner failed')
        with self.pipe_close_failure(),self.assertRaises(RuntimeError) as caught:
            self.run_child('import time;time.sleep(20)',log_byte_limit=65536,check=self.fail_check(error))
        self.assertIs(caught.exception,error);self.assertIn('pipe cleanup failed',error.__notes__[0])
        self.assertTrue(self.observed[0]['jobDrained']);self.validate()

    def test_first_close_failure_still_closes_process_and_job(self):
        with self.close_failure({1}),self.assertRaises(OSError):self.run_child()
        self.assertEqual(len(self.closed),3);self.assertEqual(len(set(self.closed)),3)
        cleanup=self.validate();self.assertFalse(cleanup['allAcquiredHandlesClosed'])
        self.assertEqual(cleanup['handleCloses'],dict(thread='CloseFailed',process='Closed',job='Closed'))
        self.assertIsNotNone(self.observed[0]['ownerError'])

    def test_process_close_failure_still_closes_job(self):
        with self.close_failure({2}),self.assertRaises(OSError):self.run_child()
        self.assertEqual(len(self.closed),3);self.assertEqual(self.validate()['handleCloses']['job'],'Closed')

    def test_job_close_failure_is_reported_without_retry(self):
        with self.close_failure({3}),self.assertRaises(OSError):self.run_child()
        self.assertEqual(len(self.closed),3);self.assertEqual(self.validate()['handleCloses']['job'],'CloseFailed')

    def test_all_failed_closes_keep_first_exception_and_all_errors(self):
        with self.close_failure({1,2,3}),self.assertRaises(OSError) as caught:self.run_child()
        self.assertEqual(len(self.closed),3);self.assertEqual(len(self.validate()['errors']),3)
        self.assertEqual(len(caught.exception.__notes__),2)

    def test_original_error_survives_failed_closes_and_is_retained(self):
        export=os.environ.get('LL_CLEANUP_EXPORT')
        if export:self.root=Path(export)/'failure';self.root.mkdir(parents=True)
        error=RuntimeError('literal owner cancellation')
        with self.close_failure({1,2}),self.assertRaises(RuntimeError) as caught:
            self.run_child('import time;time.sleep(20)',check=self.fail_check(error),log_byte_limit=65536,
                job_limits=dict(version='tower-owned-job-limits-v1',maxJobCommitBytes=128*1024*1024,maxActiveProcesses=8))
        self.assertIs(caught.exception,error);self.assertEqual(len(self.closed),3)
        self.assertTrue(self.observed[0]['jobDrained']);self.assertEqual(len(self.validate()['errors']),2)
        if export:
            work.seal_new(self.root/'observation.json',self.observed[0])
            work.seal_new(self.root/'fixture.json',dict(fixtureOnly=True,actualCombat=0,productionEntropyDraws=0,
                scientificReservations=0,nativeEncounterPreparations=0,primaryErrorPreserved=True,
                injectedCloseFailureAfterRealClose=True,closeAttempts=len(self.closed),usableForAdmission=False))
            work.seal_new(self.root/'files.json',{p.name:monitor.sha(p) for p in sorted(self.root.iterdir()) if p.is_file()})

    def test_cancellation_base_exception_survives_cleanup_failure(self):
        error=KeyboardInterrupt('literal interrupt')
        with self.close_failure({1}),self.assertRaises(KeyboardInterrupt) as caught:
            self.run_child('import time;time.sleep(20)',check=self.fail_check(error))
        self.assertIs(caught.exception,error);self.assertEqual(len(self.closed),3);self.validate()

    def test_failed_creation_closes_only_acquired_job(self):
        original=owned._close
        def close(handle):self.closed.append(handle);return original(handle)
        with patch.object(owned,'_create_process',return_value=0),patch.object(owned,'_close',close),self.assertRaises(OSError):
            self.run_child()
        self.assertEqual(len(self.closed),1)
        self.assertEqual(self.validate()['handleCloses'],dict(thread='NotAcquired',process='NotAcquired',job='Closed'))

    def test_failed_membership_query_terminates_suspended_root(self):
        original=owned._terminate_process;calls=[];error=OSError('membership query refused')
        def terminate(*args):calls.append(1);return original(*args)
        with patch.object(owned,'_is_in_job',side_effect=error),patch.object(owned,'_terminate_process',terminate),self.assertRaises(OSError) as caught:
            self.run_child('raise RuntimeError("must not resume")')
        self.assertIs(caught.exception,error);self.assertEqual(calls,[1]);self.assertFalse(self.observed[0]['jobDrained']);self.validate()

    def test_log_finalization_failure_cannot_skip_job_drain(self):
        error=RuntimeError('owner failed')
        with patch.object(owned._BoundedLog,'finish',side_effect=OSError('log finalization failed')),self.assertRaises(RuntimeError) as caught:
            self.run_child('import time;time.sleep(20)',log_byte_limit=65536,check=self.fail_check(error))
        self.assertIs(caught.exception,error);self.assertTrue(self.observed[0]['jobDrained'])
        self.assertEqual(self.validate()['errors'][0]['operation'],'logFinalization')

    def test_unexpected_io_diagnostic_failure_still_closes_and_observes_unknown(self):
        with patch.object(owned,'_job_io_observation',side_effect=RuntimeError('diagnostic failed')),self.close_failure(set()),self.assertRaisesRegex(RuntimeError,'diagnostic failed'):
            self.run_child()
        self.assertEqual(len(self.closed),3);self.assertEqual(self.observed[0]['jobIo']['coverage'],'Unknown');self.validate()

    def test_unexpected_memory_failure_preserves_original_error_and_closes(self):
        error=RuntimeError('owner failed')
        with patch.object(owned,'_memory_info',side_effect=ValueError('memory diagnostic failed')),self.close_failure(set()),self.assertRaises(RuntimeError) as caught:
            self.run_child('import time;time.sleep(20)',check=self.fail_check(error))
        self.assertIs(caught.exception,error);self.assertEqual(len(self.closed),3)
        self.assertEqual(self.observed[0]['memoryCoverage'],'Unknown');self.validate()

    def test_observer_failure_after_success_propagates_after_closes(self):
        error=RuntimeError('observer failed')
        def observe(value):self.observed.append(value);raise error
        with self.close_failure(set()),self.assertRaises(RuntimeError) as caught:
            owned.run([sys.executable,'-B','-c','pass'],self.root,self.root/'console.log',time.monotonic()+8,observe=observe)
        self.assertIs(caught.exception,error);self.assertEqual(len(self.closed),3)
        self.assertEqual(self.validate()['errors'],[])  # Observer invocation is after the recorded boundary.

    def test_observer_failure_cannot_replace_owner_error(self):
        error=RuntimeError('owner failed')
        def observe(value):self.observed.append(value);raise OSError('observer failed')
        with self.assertRaises(RuntimeError) as caught:
            owned.run([sys.executable,'-B','-c','import time;time.sleep(20)'],self.root,self.root/'console.log',
                time.monotonic()+8,check=self.fail_check(error),observe=observe)
        self.assertIs(caught.exception,error);self.assertIn('observerInvocation',error.__notes__[-1]);self.validate()

    def test_job_close_terminates_tree_after_explicit_termination_failure(self):
        original=owned._create_process;extra=[];attempts=[];error=RuntimeError('owner failed')
        opening=owned._api('OpenProcess',owned.w.HANDLE,owned.w.DWORD,owned.w.BOOL,owned.w.DWORD)
        def create(*args):
            result=original(*args)
            if result:
                process=c.cast(args[-1],c.POINTER(owned._Process)).contents
                extra.append(owned._check(opening(0x100000,False,process.pid)))
            return result
        def terminate(*args):attempts.append(1);raise OSError('termination failed')
        marker=self.root/'child-pid.txt'
        code='import subprocess,sys,time;from pathlib import Path;p=subprocess.Popen([sys.executable,"-B","-c","import time;time.sleep(20)"]);'
        code+='Path('+repr(str(marker))+').write_text(str(p.pid));time.sleep(20)'
        def check():
            if marker.exists():
                child_pid=marker.read_text()
                if child_pid:
                    extra.append(owned._check(opening(0x100000,False,int(child_pid))))
                    raise error
        try:
            with patch.object(owned,'_create_process',create),patch.object(owned,'_terminate_job',terminate),self.assertRaises(RuntimeError) as caught:
                self.run_child(code,check=check)
            self.assertIs(caught.exception,error);self.assertEqual(attempts,[1]);self.assertEqual(len(extra),2)
            for handle in extra:self.assertEqual(owned._wait(handle,2000),0)
            self.assertFalse(self.observed[0]['jobDrained']);self.assertEqual(self.observed[0]['jobIo']['coverage'],'Unknown')
            self.assertTrue(self.validate()['allAcquiredHandlesClosed'])
        finally:
            for handle in extra:owned._close(handle)

    def test_legacy_observation_without_cleanup_remains_valid(self):
        self.run_child();value=copy.deepcopy(self.observed[0]);value.pop('cleanup');self._legacy(value)

    def _legacy(self,value):
        ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native');ledger.observe_process(value,'c'*64)

    def test_ledger_rejects_cleanup_overclaims_and_inconsistent_states(self):
        self.run_child();original=self.observed[0]
        changes=[dict(wallClockBounded=True),dict(wholeProcessCoverage=True),dict(usableForAdmission=True),dict(observerInvocationExcluded=False),
            dict(extra=1),dict(allAcquiredHandlesClosed=False),dict(cleanupAllowanceSeconds=True),dict(cleanupAllowanceSeconds=3),
            dict(waitPolicy='ResetPerWait'),dict(handleCloses=dict(thread='Closed',process='Closed',job='NotAcquired')),
            dict(handleCloses=dict(thread='CloseFailed',process='Closed',job='Closed'),allAcquiredHandlesClosed=False),
            dict(errors=[dict(operation='jobHandleClose',error='failed')]),dict(errors=[dict(operation='unknown',error='failed')])]
        for change in changes:
            value=copy.deepcopy(original);value['cleanup'].update(change)
            with self.subTest(change=change),self.assertRaises(ValueError):self.validate(value)

    def test_ledger_rejects_cleanup_error_with_missing_owner_failure(self):
        self.run_child();value=self.observed[0]
        value['cleanup']['errors']=[dict(operation='jobIoObservation',error='failed')]
        with self.assertRaisesRegex(ValueError,'requires owner error'):self.validate(value)

    def test_monitor_cannot_publish_complete_after_handle_close_failure(self):
        script=self.root/'owner.py';script.write_text('pass',encoding='utf-8')
        instance=monitor.OwnerProcessMonitor(self.root/'monitor','a'*64,monitor_accounting=True)
        with self.close_failure({1}),self.assertRaises(OSError):instance.run(script,[],self.root,time.monotonic()+8)
        self.assertEqual(instance.observation['processOutcome'],'MonitorFailed')
        self.assertEqual(instance.observation['observationOutcome'],'Incomplete')
        self.assertEqual(instance.monitor_terminal_observation['outcome'],'Failed')

    def test_routed_owner_retains_cleanup_for_enclosing_and_nested_jobs(self):
        spec=importlib.util.spec_from_file_location('cleanup_fixture',ROOT/'build/test-proposal-job-limits.py')
        fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
        case=fixture.Monitor();case.setUp();self.addCleanup(case.doCleanups)
        export=os.environ.get('LL_CLEANUP_EXPORT')
        root=Path(export)/'routed' if export else self.root/'routed'
        root.parent.mkdir(parents=True,exist_ok=True)
        with patch.dict(os.environ,LL_JOB_LIMITS_EXPORT=str(root)):
            case.test_routed_owner_composes_limits_environment_io_and_terminal()
        observations=[json.loads((root/'monitor/owner-process.json').read_bytes())['processObservation']]
        observations.extend(json.loads(p.read_bytes())['observation'] for p in (root/'owner').glob('nested-*.json'))
        self.assertEqual(len(observations),5)
        for value in observations:
            self.assertTrue(self.validate(value)['allAcquiredHandlesClosed']);self.assertEqual(value['cleanup']['errors'],[])


class WaitBudget(unittest.TestCase):
    """Fake clock/API results test exact wait arguments without slow processes."""
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);self.root=Path(temp.name)
        self.now=1.;self.waits=[];self.terminations=0;self.closes=[];self.drain_calls=0

    def monotonic(self):return self.now
    def sleep(self,seconds):self.now+=seconds

    def execute(self, mode):
        def create(*args):
            p=c.cast(args[-1],c.POINTER(owned._Process)).contents
            p.process,p.thread,p.pid=101,102,103
            return 1
        def terminate(*args):
            self.terminations+=1;self.now+=.3
            if mode=='terminate-fails':raise OSError('termination failed')
            return 1
        def query(job,kind,pointer,*rest):
            self.assertEqual(kind,1)
            value=c.cast(pointer,c.POINTER(owned._Accounting)).contents;value.total_processes=1
            if self.terminations:
                self.drain_calls+=1;self.now+=2 if mode in ('expired','drain-timeout') else .2
                value.active_processes=1 if mode=='drain-timeout' or self.drain_calls==1 and mode=='shared' else 0
            else:value.active_processes=1
            return 1
        def check():self.now=10.
        def wait(handle,milliseconds):
            self.waits.append(milliseconds)
            if mode=='wait-failed':c.set_last_error(6);return 0xffffffff
            return 0
        def close(handle):self.closes.append(handle);return 1
        def membership(process,job,pointer):c.cast(pointer,c.POINTER(owned.w.BOOL)).contents.value=1;return 1
        with ExitStack() as stack:
            for name,value in dict(time=self,_create_process=create,_is_in_job=membership,_resume=lambda *a:0,
                _create_job=lambda *a:100,_set_job=lambda *a:1,_terminate_job=terminate,_query_job=query,
                _wait=wait,_exit_code=lambda *a:1,_close=close).items():stack.enter_context(patch.object(owned,name,value))
            stack.enter_context(patch.object(owned.os,'set_handle_inheritable',lambda *a:None))
            return owned.run([sys.executable,'-B','-c','pass'],self.root,self.root/'console.log',10.,check=check)

    def test_drain_and_wait_share_remaining_allowance(self):
        self.assertTrue(self.execute('shared')['timedOut'])
        self.assertEqual(self.terminations,1);self.assertEqual(self.drain_calls,2)
        self.assertEqual(len(self.waits),1);self.assertTrue(280<=self.waits[0]<=290,self.waits)
        self.assertEqual(self.closes,[102,101,100])

    def test_exhausted_budget_only_polls_root_without_fresh_allowance(self):
        self.execute('expired');self.assertEqual(self.waits,[0]);self.assertEqual(self.terminations,1)

    def test_drain_timeout_does_not_retry_termination_or_wait(self):
        with self.assertRaisesRegex(TimeoutError,'did not empty'):self.execute('drain-timeout')
        self.assertEqual(self.terminations,1);self.assertEqual(self.waits,[]);self.assertEqual(self.closes,[102,101,100])

    def test_termination_failure_does_not_retry_and_still_closes(self):
        with self.assertRaisesRegex(OSError,'termination failed'):self.execute('terminate-fails')
        self.assertEqual(self.terminations,1);self.assertEqual(self.closes,[102,101,100])

    def test_wait_failed_reports_windows_error_and_still_closes(self):
        with self.assertRaises(OSError) as caught:self.execute('wait-failed')
        self.assertEqual(caught.exception.winerror,6);self.assertEqual(self.closes,[102,101,100])


if __name__=='__main__':unittest.main()
