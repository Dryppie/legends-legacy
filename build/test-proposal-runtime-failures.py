"""Runtime inspection failure preservation; literal directories and jobs only."""
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import proposal_runtime_environment as runtime
import proposal_work_accounting as work


class Hostile(RuntimeError):
    calls=[]
    def __str__(self):self.calls.append('str');raise AssertionError('Unexpected exception formatting')
    def __repr__(self):self.calls.append('repr');raise AssertionError('Unexpected exception representation')
    def __bool__(self):self.calls.append('bool');raise AssertionError('Unexpected exception truthiness')
    def add_note(self,note):self.calls.append('add_note');raise AssertionError('Unexpected exception note')
    def __getattribute__(self,name):
        if name in ('args','__dict__','__notes__'):
            type(self).calls.append(name);raise AssertionError('Unexpected exception attribute')
        return super().__getattribute__(name)


def declaration(root,**changes):
    return dict(version=runtime.VERSION,root=str(root),maxEntries=64,maxDepth=6,maxRetainedBytes=65536)|changes


class ScanFault:
    def __init__(self,entries,error=None,closing=None,entry=None):
        self.entries,self.error,self.closing,self.entry=entries,error,closing,entry
        self.close_calls=self.next_calls=0
    def __iter__(self):return self
    def __next__(self):
        self.next_calls+=1
        if self.error is not None:raise self.error
        value=next(self.entries)
        return value if self.entry is None else self.entry
    def close(self):
        self.close_calls+=1;self.entries.close()
        if self.closing is not None:raise self.closing


class RuntimeFailures(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.base=Path(temporary.name);self.profile=runtime.RuntimeEnvironment(declaration(self.base/'runtime'))
        self.scanners=[];Hostile.calls.clear()

    def scan_fault(self,error=None,closing=None,entry=None,guard=lambda path:True):
        original=os.scandir
        def scanning(path):
            entries=original(path)
            if not guard(Path(path)):return entries
            scanner=ScanFault(entries,error,closing,entry);self.scanners.append(scanner);return scanner
        return patch.object(runtime.os,'scandir',scanning)

    def primary(self,error,action):
        try:action()
        except BaseException as caught:self.assertIs(caught,error)
        else:self.fail('Original failure did not escape')
        self.assertEqual(Hostile.calls,[])

    def failed(self,message,*,samples=2):
        value=self.profile.snapshot()
        self.assertTrue(self.profile.failed);self.assertIsNone(self.profile.last)
        self.assertEqual(value['outcome'],'FailedOrUnknown');self.assertIsNone(value['lastSample'])
        self.assertEqual(value['error'],message);self.assertEqual(value['samples'],samples)
        self.assertEqual(runtime.observation(value),self.profile._declaration)
        for name in ('transientWritesBounded','runtimeWritesBounded','filesystemConfinement','wholeProcessCoverage','usableForAdmission'):
            self.assertFalse(value[name])
        self.assertEqual(Hostile.calls,[])
        return value

    def assert_closed(self,count=1):
        self.assertEqual(len(self.scanners),count)
        self.assertTrue(all(s.close_calls==1 for s in self.scanners))

    def test_root_creation_failure_does_not_format_exception(self):
        error=Hostile('mkdir failed')
        with patch.object(Path,'mkdir',side_effect=error):self.primary(error,self.profile.prepare)
        self.failed(runtime.PREPARATION_FAILED,samples=0);self.assertFalse(self.profile.root.exists())

    def test_partial_preparation_is_retained_but_never_sampled_success(self):
        error=Hostile('child mkdir failed');original=Path.mkdir
        def mkdir(path,*args,**kwargs):
            if path==self.profile.root/'local':raise error
            return original(path,*args,**kwargs)
        with patch.object(Path,'mkdir',mkdir):self.primary(error,self.profile.prepare)
        self.failed(runtime.PREPARATION_FAILED,samples=0)
        self.assertEqual({p.name for p in self.profile.root.iterdir()},{'temp','profile','roaming'})

    def test_initial_inspection_error_is_not_overwritten_by_preparation_handler(self):
        error=Hostile('scandir failed')
        with patch.object(runtime.os,'scandir',side_effect=error):self.primary(error,self.profile.prepare)
        self.failed(runtime.INSPECTION_FAILED,samples=1)

    def test_initial_body_and_close_failures_survive_both_handlers(self):
        error=Hostile('iteration failed')
        with self.scan_fault(error,Hostile('close failed')):self.primary(error,self.profile.prepare)
        self.failed(runtime.INSPECTION_AND_CLOSE_FAILED,samples=1);self.assert_closed()

    def test_prepare_failure_after_sample_clears_earlier_success(self):
        original=self.profile.sample;error=Hostile('post-sample prepare failure')
        def sample():original();raise error
        with patch.object(self.profile,'sample',sample):self.primary(error,self.profile.prepare)
        self.failed(runtime.PREPARATION_FAILED,samples=1)

    def test_later_metadata_failure_clears_previous_sample(self):
        self.profile.prepare();error=Hostile('scandir failed')
        with patch.object(runtime.os,'scandir',side_effect=error):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_FAILED)

    def test_iteration_and_close_failures_preserve_first_error(self):
        self.profile.prepare();error=Hostile('iteration failed')
        with self.scan_fault(error,Hostile('close failed')):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_AND_CLOSE_FAILED);self.assert_closed()

    def test_close_failure_alone_invalidates_complete_iteration(self):
        self.profile.prepare();error=Hostile('close failed')
        with self.scan_fault(closing=error):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_FAILED);self.assert_closed()

    def test_entry_stat_failure_survives_close_failure(self):
        self.profile.prepare();error=Hostile('stat failed')
        class Entry:
            def stat(self,**kwargs):raise error
        with self.scan_fault(closing=Hostile('close failed'),entry=Entry()):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_AND_CLOSE_FAILED);self.assert_closed()

    def test_entry_path_failure_survives_close_failure(self):
        self.profile.prepare();error=Hostile('path failed');info=(self.profile.root/'temp').stat()
        class Entry:
            def stat(self,**kwargs):return info
            @property
            def path(self):raise error
        with self.scan_fault(closing=Hostile('close failed'),entry=Entry()):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_AND_CLOSE_FAILED);self.assert_closed()

    def test_entry_depth_and_byte_guards_remain_primary_when_close_fails(self):
        for name,options in [('entries',dict(maxEntries=7)),('depth',dict(maxDepth=1)),('bytes',dict(maxRetainedBytes=0))]:
            with self.subTest(name=name):
                self.scanners=[];self.profile=runtime.RuntimeEnvironment(declaration(self.base/name,**options));self.profile.prepare()
                (self.profile.root/'temp/value').write_bytes(b'x')
                with self.scan_fault(closing=Hostile('close failed'),guard=lambda path:path==self.profile.root/'temp'), \
                     self.assertRaisesRegex(ValueError,'allowance exhausted'):
                    self.profile.sample()
                self.failed(runtime.INSPECTION_AND_CLOSE_FAILED);self.assert_closed()
                self.assertEqual((self.profile.root/'temp/value').read_bytes(),b'x')

    def test_sort_allocation_failure_clears_previous_sample_after_all_closes(self):
        self.profile.prepare();error=MemoryError('sample sort allocation')
        with self.scan_fault(),patch.object(runtime,'sorted',side_effect=error,create=True):
            self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_FAILED);self.assert_closed(8)

    def test_failed_scandir_acquisition_has_no_iterator_to_close(self):
        self.profile.prepare();error=Hostile('open directory failed')
        with patch.object(runtime.os,'scandir',side_effect=error):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_FAILED);self.assertEqual(self.scanners,[])

    def test_caller_exception_does_not_hide_close_failure(self):
        self.profile.prepare();error=Hostile('close failed');caller=RuntimeError('unrelated')
        try:raise caller
        except RuntimeError:
            with self.scan_fault(closing=error):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_FAILED);self.assert_closed()
        self.assertNotIn('__notes__',BaseException.__dict__['__dict__'].__get__(caller))

    def test_cancellation_survives_directory_close_failure(self):
        self.profile.prepare();error=KeyboardInterrupt('cancelled')
        with self.scan_fault(error,Hostile('close failed')):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_AND_CLOSE_FAILED);self.assert_closed()

    def test_large_exception_arguments_and_existing_notes_are_not_inspected(self):
        self.profile.prepare();error=Hostile('x'*1000000,object())
        attrs=BaseException.__dict__['__dict__'].__get__(error);prior=object();attrs['__notes__']=prior
        with self.scan_fault(error,Hostile('y'*1000000)):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_AND_CLOSE_FAILED);self.assertIs(attrs['__notes__'],prior)

    def test_exception_metaclass_name_is_not_accessed(self):
        self.profile.prepare();calls=[]
        class Meta(type):
            def __getattribute__(cls,name):calls.append(name);raise AssertionError('Unexpected metaclass access')
        class Error(Exception,metaclass=Meta):pass
        error=Error('scan failed')
        with self.scan_fault(error):self.primary(error,self.profile.sample)
        self.failed(runtime.INSPECTION_FAILED);self.assertEqual(calls,[])

    def test_failed_profile_cannot_retry_or_overwrite_first_diagnostic(self):
        self.profile.prepare();error=Hostile('scan failed')
        with self.scan_fault(error,Hostile('close failed')):self.primary(error,self.profile.sample)
        before=self.failed(runtime.INSPECTION_AND_CLOSE_FAILED)
        with patch.object(runtime.os,'scandir') as scan,patch.object(Path,'mkdir') as mkdir:
            for action in (self.profile.sample,self.profile.prepare):
                with self.assertRaises(ValueError):action()
        scan.assert_not_called();mkdir.assert_not_called();self.assertEqual(self.profile.snapshot(),before)

    def test_successful_scans_close_each_iterator_without_changing_samples(self):
        with self.scan_fault():self.profile.prepare()
        self.assert_closed(8);value=self.profile.snapshot()
        self.assertEqual(value['outcome'],'Sampled');self.assertIsNone(value['error'])
        self.assertEqual(value['lastSample']['sampledRetainedBytes'],0);self.assertEqual(len(value['lastSample']['entries']),7)

    def test_runtime_import_and_sampling_require_no_monitor_or_launcher(self):
        code='''import builtins,sys
original=builtins.__import__
def importing(name,*args,**kwargs):
    if name in ('bounded_windows_process','proposal_monitor_io','proposal_owner_process'): raise AssertionError('Unexpected dependency')
    return original(name,*args,**kwargs)
builtins.__import__=importing
sys.path.insert(0,sys.argv[1])
import proposal_runtime_environment as runtime
value=dict(version=runtime.VERSION,root=sys.argv[2],maxEntries=7,maxDepth=1,maxRetainedBytes=0)
profile=runtime.RuntimeEnvironment(value);profile.prepare()
assert profile.snapshot()['outcome']=='Sampled'
'''
        result=subprocess.run([sys.executable,'-B','-X','utf8','-c',code,str(ROOT/'Balance Harness/analysis'),str(self.base/'independent')],
                              capture_output=True,text=True,timeout=10)
        self.assertEqual(result.returncode,0,result.stdout+result.stderr)

    def test_real_post_drain_failure_keeps_phase_and_persists_incomplete_evidence(self):
        import bounded_windows_process as owned
        import proposal_monitor_io as budget
        import proposal_owner_process as monitor
        export=os.environ.get('LL_RUNTIME_FAILURES_EXPORT');destination=self.base/'evidence' if not export else Path(export)
        destination.mkdir(parents=True);script=destination/'literal.py';script.write_text('print("literal owner",flush=True)',encoding='utf-8')
        observed=monitor.OwnerProcessMonitor(destination/'monitor','a'*64,monitor_accounting=True,
            runtime_environment=declaration(self.base/'real-runtime'),monitor_io_budget=dict(version=budget.TERMINAL_VERSION,
                terminalRoot=str(destination/'terminal'),maxTerminalBytes=65536,maxFileReadBytes=16*1024*1024,maxTotalReadBytes=32*1024*1024,
                maxConsoleBytes=4096,maxObservationBytes=65536,maxManifestBytes=1024,maxTotalWriteBytes=131072))
        original=owned.run;armed=False;error=Hostile('literal runtime iteration failure')
        def run(*args,**kwargs):
            nonlocal armed
            result=original(*args,**kwargs);armed=True;return result
        with patch.object(owned,'run',run),self.scan_fault(error,Hostile('literal runtime close failure'),
                guard=lambda path:armed and path==self.base/'real-runtime'):
            self.primary(error,lambda:observed.run(script,[],destination,time.monotonic()+10))
        self.assert_closed();value=observed.observation;terminal=observed.monitor_terminal_observation
        self.assertTrue(value['processObservation']['jobDrained']);self.assertTrue(value['processObservation']['cleanup']['allAcquiredHandlesClosed'])
        self.assertEqual(value['processOutcome'],'MonitorFailed');self.assertEqual(value['observationOutcome'],'Incomplete')
        self.assertEqual(value['runtimeEnvironment']['error'],runtime.INSPECTION_AND_CLOSE_FAILED)
        self.assertIsNone(value['runtimeEnvironment']['lastSample']);self.assertEqual(terminal['outcome'],'Failed')
        self.assertEqual(terminal['failedPhase'],'ownerProcess');self.assertTrue(terminal['publicationVerified'])
        counters=terminal['phaseCounters']['ownerProcess']
        self.assertEqual(counters['runtimeFinalInspectionAttempted'],1);self.assertEqual(counters['runtimeFinalInspectionFailed'],1)
        self.assertNotIn('runtimeFinalInspectionCompleted',counters)
        self.assertEqual(observed.terminal_publication_observation['outcome'],'Verified')
        self.assertEqual(work.strict((destination/'terminal/monitor-call.json').read_bytes()),terminal)
        if export:
            work.seal_new(destination/'publication-result.json',observed.terminal_publication_observation)
            work.seal_new(destination/'fixture.json',dict(fixtureOnly=True,primaryErrorPreserved=True,
                directoryCloseAttempts=self.scanners[0].close_calls,closeFailureInjectedAfterRealClose=True,
                formattingHookCalls=Hostile.calls,runtimeEnvironmentModuleSha256=monitor.sha(runtime.__file__),
                monitorProducerSha256=monitor.sha(monitor.__file__),processHelperSha256=monitor.sha(owned.__file__),
                actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
                wholeProcessCoverage=False,usableForAdmission=False))
            work.seal_new(destination/'files.json',{p.relative_to(destination).as_posix():monitor.sha(p)
                for p in sorted(destination.rglob('*')) if p.is_file()})


if __name__=='__main__':unittest.main()
