"""Bounded merged worker logs, literal processes and routed fixtures only."""
import contextlib
import copy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as work
import bounded_windows_process as owned


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class WorkerLogs(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        export = os.environ.get('LL_WORKER_LOG_EXPORT')
        self.root = Path(export)/self._testMethodName if export else Path(self.temp.name)
        self.root.mkdir(parents=True, exist_ok=True)
        self.log = self.root/'console.log'; self.observations = []

    def validate(self, value):
        ledger = work.OwnerLedger('a'*64, 'b'*64); ledger.begin('native')
        ledger.observe_process(value, sha(owned.__file__))

    def run_child(self, body, limit=100000, seconds=8, expect_drained=True, **options):
        command = [sys.executable, '-B', '-c', body]
        try:
            result = owned.run(command, self.root, self.log, time.monotonic()+seconds,
                log_byte_limit=limit, observe=self.observations.append, **options)
            return result
        finally:
            if self.observations:
                value = self.observations[-1]; self.validate(value)
                self.assertEqual(value['jobDrained'],expect_drained)
                if os.environ.get('LL_WORKER_LOG_EXPORT'):
                    work.seal_new(self.root/'process.json', dict(command=command, observation=value,
                        literalOnly=True, actualCombat=0, productionEntropyDraws=0, usableForAdmission=False))

    def captured(self): return self.observations[-1]['logCapture']

    def test_exact_shared_stdout_stderr_limit(self):
        self.assertEqual(self.run_child('import os;os.write(1,b"abc");os.write(2,b"def")',6)['exitCode'],0)
        self.assertEqual(self.log.read_bytes(), b'abcdef')
        self.assertTrue(self.captured()['captureComplete']); self.assertEqual(self.captured()['retainedBytes'],6)

    def test_empty_output_with_zero_cap(self):
        self.run_child('pass',0); self.assertEqual(self.log.read_bytes(), b'')
        self.assertTrue(self.captured()['captureComplete'])

    def test_zero_cap_rejects_first_output(self):
        with self.assertRaisesRegex(ValueError,'log byte limit'): self.run_child('print("x",flush=True)',0)
        self.assertEqual(self.log.stat().st_size,0); self.assertTrue(self.captured()['exceeded'])

    def test_overflow_retains_exact_prefix_and_kills_tree(self):
        body='import os,subprocess,sys,time;subprocess.Popen([sys.executable,"-B","-c","import time;time.sleep(60)"]);os.write(1,b"x"*200000);time.sleep(60)'
        with self.assertRaisesRegex(ValueError,'log byte limit'): self.run_child(body,8193)
        self.assertEqual(self.log.read_bytes(),b'x'*8193)
        self.assertGreaterEqual(self.observations[0]['jobIo']['totalProcessesAtQuery'],2)
        self.assertFalse(self.captured()['captureComplete'])

    def test_large_binary_output_without_line_buffering(self):
        expected=bytes(range(256))*4096
        self.run_child('import os;os.write(1,bytes(range(256))*4096)',len(expected))
        self.assertEqual(self.log.read_bytes(),expected); self.assertTrue(self.captured()['captureComplete'])

    def test_descendant_output_after_root_exit_is_captured(self):
        child='import os,time;time.sleep(.12);os.write(2,b"late descendant")'
        self.run_child('import subprocess,sys;subprocess.Popen([sys.executable,"-B","-c",'+repr(child)+'])',15)
        self.assertEqual(self.log.read_bytes(),b'late descendant'); self.assertTrue(self.captured()['pipeEof'])

    def test_descendant_overflow_after_root_exit_fails(self):
        child='import os,time;time.sleep(.12);os.write(2,b"late descendant")'
        with self.assertRaisesRegex(ValueError,'log byte limit'):
            self.run_child('import subprocess,sys;subprocess.Popen([sys.executable,"-B","-c",'+repr(child)+'])',4)
        self.assertEqual(self.log.read_bytes(),b'late')

    def test_timeout_retains_partial_log_without_claiming_full_capture(self):
        result=self.run_child('import os,time;os.write(1,b"prefix");time.sleep(60)',100,seconds=.5)
        self.assertTrue(result['timedOut']); self.assertFalse(self.captured()['captureComplete'])
        self.assertEqual(self.log.read_bytes(),b'prefix')

    def test_nonzero_exit_can_have_complete_output(self):
        result=self.run_child('import os;os.write(2,b"failed");raise SystemExit(7)',6)
        self.assertEqual(result['exitCode'],7); self.assertTrue(self.captured()['captureComplete'])

    def test_continuous_output_still_checks_deadline(self):
        result=self.run_child('import os\nwhile True:os.write(1,b"x"*4096)',2**40,seconds=.2)
        self.assertTrue(result['timedOut']); self.assertLess(self.captured()['retainedBytes'],2**40)

    def test_owner_cancellation_preserves_original_error(self):
        error=RuntimeError('owner check failed')
        with self.assertRaises(RuntimeError) as caught:
            self.run_child('import time;time.sleep(60)',check=lambda:(_ for _ in ()).throw(error))
        self.assertIs(caught.exception,error); self.assertFalse(self.captured()['captureComplete'])

    def test_existing_log_is_preserved_and_no_child_starts(self):
        self.log.write_bytes(b'previous')
        with patch.object(owned,'_create_process') as create, self.assertRaises(FileExistsError):
            self.run_child('pass',expect_drained=False)
        create.assert_not_called(); self.assertEqual(self.log.read_bytes(),b'previous')
        self.assertIsNone(self.captured()['retainedBytes'])

    def test_invalid_caps_fail_before_creating_job_or_log(self):
        for limit in (-1,True,1.5,'2',2**63):
            with self.subTest(limit=limit),patch.object(owned,'_create_job') as create,self.assertRaises(ValueError):
                owned.run([sys.executable,'-c','pass'],self.root,self.log,time.monotonic()+5,log_byte_limit=limit)
            create.assert_not_called(); self.assertFalse(self.log.exists())

    def test_default_does_not_add_observation_fields(self):
        owned.run([sys.executable,'-B','-c','print("legacy")'],self.root,self.log,time.monotonic()+8,observe=self.observations.append)
        self.assertNotIn('logCapture',self.observations[0]); self.assertIn(b'legacy',self.log.read_bytes())

    def test_log_file_handle_is_not_inherited(self):
        original=owned._create_process; seen=[]
        def create(*args):
            # Only the pipe writer, never the retained log, is made inheritable.
            seen.append(args[4]); return original(*args)
        original_redirect=owned._BoundedLog.redirect
        @contextlib.contextmanager
        def redirect(capture, log):
            self.assertFalse(os.get_inheritable(log.fileno()))
            with original_redirect(capture,log) as handle: yield handle
        with patch.object(owned,'_create_process',side_effect=create),patch.object(owned._BoundedLog,'redirect',redirect):
            self.run_child('print("pipe")')
        self.assertEqual(seen,[True])

    def test_failed_resume_cleans_pipe_and_suspended_child(self):
        with patch.object(owned,'_resume',return_value=0xffffffff),self.assertRaises(OSError): self.run_child('pass')
        self.assertFalse(self.captured()['captureComplete'])

    def test_failed_membership_query_terminates_unverified_suspended_child(self):
        with patch.object(owned,'_is_in_job',return_value=0),self.assertRaises(OSError):
            self.run_child('pass',expect_drained=False)
        self.assertFalse(self.captured()['captureComplete'])

    def test_pipe_descriptors_close_after_overflow(self):
        original=owned.os.pipe;descriptors=[]
        def pipe():
            pair=original();descriptors.extend(pair);return pair
        with patch.object(owned.os,'pipe',side_effect=pipe),self.assertRaisesRegex(ValueError,'log byte limit'):
            self.run_child('print("overflow",flush=True)',0)
        for descriptor in descriptors:
            with self.assertRaises(OSError):os.fstat(descriptor)

    def test_peek_failure_terminates_child_and_preserves_error(self):
        error=OSError('pipe query failed')
        with patch.object(owned,'_peek_pipe',side_effect=error),self.assertRaises(OSError) as caught:
            self.run_child('import time;time.sleep(60)')
        self.assertIs(caught.exception,error);self.assertFalse(self.captured()['captureComplete'])

    def test_invalid_write_result_stops_capture(self):
        original=owned._log_file
        class Invalid:
            def __init__(self,stream):self.stream=stream
            def fileno(self):return self.stream.fileno()
            def write(self,raw):return None
        @contextlib.contextmanager
        def opening(*args):
            with original(*args) as stream:yield Invalid(stream)
        with patch.object(owned,'_log_file',opening),self.assertRaisesRegex(OSError,'Invalid log write'):
            self.run_child('print("x",flush=True)')
        self.assertEqual(self.log.stat().st_size,0);self.assertFalse(self.captured()['captureComplete'])

    def test_short_file_writes_are_retried_and_counted(self):
        original=owned._log_file
        class Short:
            def __init__(self,stream):self.stream=stream
            def fileno(self):return self.stream.fileno()
            def write(self,raw):return self.stream.write(raw[:1])
        @contextlib.contextmanager
        def opening(*args):
            with original(*args) as stream:yield Short(stream)
        with patch.object(owned,'_log_file',opening):self.run_child('import os;os.write(1,b"abcdef")',6)
        self.assertEqual(self.log.read_bytes(),b'abcdef'); self.assertEqual(self.captured()['acceptedBytes'],6)

    def test_failed_write_prefix_is_unknown_and_never_complete(self):
        original=owned._log_file
        class Fail:
            def __init__(self,stream):self.stream=stream
            def fileno(self):return self.stream.fileno()
            def write(self,raw):self.stream.write(raw[:1]);raise OSError('hidden prefix')
        @contextlib.contextmanager
        def opening(*args):
            with original(*args) as stream:yield Fail(stream)
        with patch.object(owned,'_log_file',opening),self.assertRaisesRegex(OSError,'hidden prefix'):
            self.run_child('import os;os.write(1,b"abcdef")',6)
        self.assertEqual(self.log.read_bytes(),b'a'); self.assertIsNone(self.captured()['retainedBytes'])
        self.assertFalse(self.captured()['captureComplete'])

    def test_changed_closed_length_rejects_success(self):
        original=owned._BoundedLog.finish;changed=False
        def finish(capture,path,failure):
            nonlocal changed
            if not changed:
                changed=True
                with open(path,'ab') as stream:stream.write(b'x')
            return original(capture,path,failure)
        with patch.object(owned._BoundedLog,'finish',finish),self.assertRaisesRegex(ValueError,'closed worker log'):
            self.run_child('pass')
        self.assertFalse(self.captured()['captureComplete'])

    def test_observation_rejects_false_success_and_impossible_counts(self):
        self.run_child('print("ok")'); good=self.observations[-1]
        for change in ({'maxBytes':True},{'acceptedBytes':-1},{'readBytes':0},{'retainedBytes':99},
                       {'pipeEof':False},{'captureComplete':False},{'error':'failed'},
                       {'exceeded':True},{'wholeProcessCoverage':True},{'unknown':0}):
            bad=copy.deepcopy(good);bad['logCapture'].update(change)
            with self.subTest(change=change),self.assertRaises(ValueError):self.validate(bad)


class SupervisorLogs(unittest.TestCase):
    def setUp(self):
        spec=importlib.util.spec_from_file_location('log_storage_fixture',ROOT/'build/test-proposal-supervisor-storage.py')
        self.fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(self.fixture)
        self.f=self.fixture.SupervisorStorage();self.f.setUp();self.addCleanup(self.f.doCleanups)

    def prepare(self,export=False):
        path=os.environ.get('LL_WORKER_LOG_EXPORT')
        base=Path(path)/'supervisor' if export and path else self.f.base
        base.parent.mkdir(parents=True,exist_ok=True)
        old=self.f.ready(persistence=True,base=base)
        caps={phase:1000+i for i,phase in enumerate(work.PHASES)}
        supervisor=self.fixture.s.StudyWorkSupervisor(old.root,worker_observation_persistence=True,
            storage_contracts=self.fixture.contracts(True),worker_log_limits=caps)
        return supervisor,caps,base

    def route(self,*args,**kwargs):
        maximum=kwargs.pop('log_byte_limit');observer=kwargs['observe'];path=Path(args[2])
        def observe(value):
            length=path.stat().st_size
            observer(value|dict(logCapture=dict(version='tower-owned-log-capture-v1',maxBytes=maximum,
                acceptedBytes=length,readBytes=length,retainedBytes=length,exceeded=False,pipeEof=True,
                captureComplete=True,error=None,scope='MergedStdoutStderrThroughParentWriter',
                wholeProcessCoverage=False,usableForAdmission=False)))
        kwargs['observe']=observe
        return self.fixture.f.Supervisor.routed(self.f,*args,**kwargs)

    def test_all_four_phases_bind_and_retain_copied_caps(self):
        supervisor,caps,base=self.prepare(True);declared=dict(caps);caps['native']=0
        with patch.object(self.f,'routed',side_effect=self.route):self.f.launch(supervisor)
        self.f.verify(supervisor)
        value=supervisor.terminal_observation['storageOwnership']
        self.assertEqual(value['workerLogByteLimits'],declared)
        self.assertEqual(value['declaredWorkerLogByteUpperBound'],4006)
        self.assertNotIn('workerLogsAndStudyFiles',value['missingCoverage'])
        for phase in work.PHASES:
            observation=next(p for p in supervisor.owner.ledger.processes if p['phase']==phase)['observation']
            self.assertEqual(observation['logCapture']['maxBytes'],declared[phase])
        if os.environ.get('LL_WORKER_LOG_EXPORT'):
            work.seal_new(base/'terminal.json',supervisor.terminal_observation)
            work.seal_new(base/'fixture.json',dict(syntheticProcessObservations=4,realProcessObservations=0,
                actualCombat=0,productionEntropyDraws=0,wholeProcessCoverage=False,usableForAdmission=False))

    def test_incomplete_or_invalid_phase_caps_reject_before_io(self):
        for value in ({},[],{'native':1},{phase:True for phase in work.PHASES},{phase:-1 for phase in work.PHASES}):
            with self.subTest(value=value),self.assertRaises(ValueError):
                self.fixture.s.StudyWorkSupervisor(self.f.base/'bad',worker_log_limits=value)
        self.assertFalse((self.f.base/'bad').exists())

    def test_missing_bound_observation_stops_at_first_worker(self):
        supervisor,_,_=self.prepare()
        def unbound(*args,**kwargs):
            kwargs.pop('log_byte_limit');return self.fixture.f.Supervisor.routed(self.f,*args,**kwargs)
        with patch.object(self.f,'routed',side_effect=unbound),self.assertRaisesRegex(ValueError,'log limit observation'):
            self.f.launch(supervisor)
        self.assertEqual(len(self.f.calls),1);self.assertEqual(supervisor.terminal_observation['outcome'],'Failed')

    def test_phase_retry_cannot_multiply_declared_bound(self):
        supervisor,_,_=self.prepare()
        def route(*args,**kwargs):
            with self.assertRaisesRegex(ValueError,'cannot be reused'):
                supervisor.run('native',args[0],args[1],args[2],args[3],kwargs['cleanup_seconds'],kwargs['check'])
            return self.route(*args,**kwargs)
        with patch.object(self.f,'routed',side_effect=route):
            # Assert retry at the native callback only.
            original=route
            def first(*args,**kwargs):
                return original(*args,**kwargs) if not self.f.calls else self.route(*args,**kwargs)
            with patch.object(self.f,'routed',side_effect=first):self.f.launch(supervisor)


class NativeLogs(unittest.TestCase):
    def test_real_native_v4_audit_with_capped_merged_log(self):
        spec=importlib.util.spec_from_file_location('capped_native_fixture',ROOT/'build/test-proposal-worker-sidecars.py')
        fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
        original=owned.run;observations=[]
        def run(*args,**kwargs):
            observer=kwargs['observe']
            def observe(value):observations.append(value);observer(value)
            kwargs.update(log_byte_limit=65536,observe=observe)
            return original(*args,**kwargs)
        case=fixture.Native('test_v4_actual_audit_command_closes_bounded_originals')
        with patch.object(owned,'run',side_effect=run):case.test_v4_actual_audit_command_closes_bounded_originals()
        self.assertEqual(len(observations),1)
        capture=observations[0]['logCapture']
        self.assertEqual(capture['maxBytes'],65536);self.assertTrue(capture['captureComplete'])
        self.assertEqual(capture['retainedBytes'],capture['readBytes']);self.assertFalse(capture['exceeded'])


if __name__=='__main__':unittest.main()
