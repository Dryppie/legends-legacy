"""Real empty Windows leases and routed owners; no scientific worker executes."""
import contextlib
import copy
import ctypes
import importlib.util
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import proposal_owner_leases as leases
import proposal_work_accounting as w
spec=importlib.util.spec_from_file_location('owner_lease_storage_fixture',ROOT/'build/test-proposal-supervisor-storage.py')
f=importlib.util.module_from_spec(spec);spec.loader.exec_module(f)
launcher=f.launcher

def budget(cap=2):return dict(version=leases.VERSION,maxConcurrentLeases=cap)


class OwnerLeases(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup)
        self.root=Path(temp.name);self.output=self.root/'output';self.registry=self.root/'complete-family-allocation'
        self.scope=leases.OwnerLeases(self.output,budget());self.work=w.OwnerFileCounters()

    def complete(self):
        with self.scope.acquire(self.registry,self.work),self.scope.acquire(self.output,self.work):self.scope.require_held()
        self.scope.finish();return self.scope.snapshot()

    def kernel(self, **overrides):
        k=ctypes.WinDLL('kernel32',use_last_error=True)
        return SimpleNamespace(**({n:getattr(k,n) for n in ('CreateFileW','CloseHandle','GetFileSizeEx')}|overrides))

    def test_real_handles_exclude_legacy_acquisition_and_disappear(self):
        with self.scope.acquire(self.registry,self.work),self.scope.acquire(self.output,self.work):
            for root in (self.registry,self.output):
                self.assertEqual(Path(str(root)+'.writer.lock').stat().st_size,0)
                with self.assertRaisesRegex(ValueError,'already leased'):
                    with launcher.writer_lease(root):self.fail('Contended acquisition')
        self.scope.finish();v=self.scope.snapshot()
        self.assertEqual(v['outcome'],'Complete');self.assertEqual(v['peakOwnedHandles'],2)
        self.assertEqual(v['currentOwnedHandles'],0)
        self.assertTrue(all(e['attempts']==e['acquired']==e['released']==1 for e in v['entries']))
        self.assertEqual(self.work.values['writerLeaseReleaseCompleted'],2)
        self.assertFalse(any(Path(e['path']).exists() for e in v['entries']))

    def test_original_handle_cannot_write_or_resize(self):
        kernel=ctypes.WinDLL('kernel32',use_last_error=True)
        write=kernel.WriteFile;write.argtypes=[ctypes.c_void_p,ctypes.c_void_p,ctypes.c_uint32,ctypes.POINTER(ctypes.c_uint32),ctypes.c_void_p];write.restype=ctypes.c_int
        resize=kernel.SetEndOfFile;resize.argtypes=[ctypes.c_void_p];resize.restype=ctypes.c_int
        with self.scope.acquire(self.registry),self.scope.acquire(self.output):
            handle=next(iter(self.scope._handles.values()))[0];written=ctypes.c_uint32()
            self.assertFalse(write(handle,b'x',1,ctypes.byref(written),None));self.assertEqual(ctypes.get_last_error(),5)
            self.assertFalse(resize(handle));self.assertEqual(ctypes.get_last_error(),5)
        self.scope.finish()

    def test_child_process_sees_both_owners_as_sharing_conflicts(self):
        script='''import ctypes,sys
k=ctypes.WinDLL('kernel32',use_last_error=True)
f=k.CreateFileW;f.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];f.restype=ctypes.c_void_p
for path in sys.argv[1:]:
 h=f(path,0x80010000,0,None,4,0x04000000,None)
 assert h==ctypes.c_void_p(-1).value and ctypes.get_last_error() in (32,33)
print('two held owner probes')
'''
        with self.scope.acquire(self.registry),self.scope.acquire(self.output):
            result=subprocess.run([sys.executable,'-B','-c',script,*[e['path'] for e in self.scope.snapshot()['entries']]],
                                  capture_output=True,text=True,timeout=10)
            self.assertEqual(result.returncode,0,result.stderr);self.assertEqual(result.stdout.strip(),'two held owner probes')
        self.scope.finish()

    def test_existing_empty_and_nonempty_files_are_preserved(self):
        path=Path(str(self.output)+'.writer.lock')
        for raw in (b'',b'prior'):
            path.write_bytes(raw);scope=leases.OwnerLeases(self.output,budget())
            with self.assertRaisesRegex(ValueError,'already exists'):
                with scope.acquire(self.output):pass
            with self.assertRaises(ValueError):scope.finish()
            self.assertEqual(path.read_bytes(),raw);path.unlink()

    def test_zero_and_one_caps_reject_without_extra_creation(self):
        for cap in (0,1):
            scope=leases.OwnerLeases(self.output,budget(cap))
            with self.assertRaisesRegex(ValueError,'concurrency'),contextlib.ExitStack() as stack:
                if cap:stack.enter_context(scope.acquire(self.registry))
                stack.enter_context(scope.acquire(self.output))
            with self.assertRaises(ValueError):scope.finish()
            self.assertFalse(Path(str(self.output)+'.writer.lock').exists())
            self.assertLessEqual(scope.snapshot()['peakOwnedHandles'],cap)

    def test_attempt_is_not_refunded_after_release(self):
        with self.scope.acquire(self.registry):pass
        with self.assertRaisesRegex(ValueError,'attempt exhausted'):
            with self.scope.acquire(self.registry):pass
        with self.assertRaises(ValueError):self.scope.finish()
        self.assertEqual(self.scope.snapshot()['entries'][0]['attempts'],1)

    def test_undeclared_path_rejects_before_creating_parent(self):
        with self.assertRaisesRegex(ValueError,'Undeclared'):
            with self.scope.acquire(self.root/'missing/other'):pass
        self.assertFalse((self.root/'missing').exists())

    def test_missing_parent_does_not_acquire_or_release(self):
        scope=leases.OwnerLeases(self.root/'missing/out',budget())
        with self.assertRaisesRegex(ValueError,'inaccessible'):
            with scope.acquire(self.root/'missing/out',self.work):pass
        with self.assertRaises(ValueError):scope.finish()
        self.assertEqual(self.work.values['writerLeaseAcquireFailed'],1)
        self.assertEqual(self.work.values['writerLeaseReleaseAttempted'],0)

    def test_escaped_handle_cleanup_cannot_complete(self):
        context=self.scope.acquire(self.output,self.work);context.__enter__()
        with self.assertRaisesRegex(ValueError,'incomplete'):self.scope.finish()
        context.__exit__(None,None,None)
        value=self.scope.snapshot();self.assertEqual(value['entries'][1]['forcedCleanup'],1)
        self.assertEqual(value['currentOwnedHandles'],0);self.assertEqual(self.work.values['writerLeaseReleaseCompleted'],1)
        value.update(outcome='Complete',failedOrUnknown=False)
        with self.assertRaises(ValueError):leases.observation(value)

    def test_unused_scope_is_incomplete_and_cannot_be_reused(self):
        with self.assertRaisesRegex(ValueError,'incomplete'):self.scope.finish()
        with self.assertRaisesRegex(ValueError,'twice'):self.scope.finish()
        with self.assertRaisesRegex(ValueError,'closed'):
            with self.scope.acquire(self.output):pass

    def test_sequential_leases_do_not_establish_enclosing_pair(self):
        with self.scope.acquire(self.registry):pass
        with self.scope.acquire(self.output):pass
        with self.assertRaisesRegex(ValueError,'incomplete'):self.scope.finish()

    def test_caught_missing_pair_check_poison_scope(self):
        with self.scope.acquire(self.registry):
            with self.assertRaisesRegex(ValueError,'Both enclosing'):self.scope.require_held()
            with self.assertRaisesRegex(ValueError,'failed'):
                with self.scope.acquire(self.output):pass
        with self.assertRaises(ValueError):self.scope.finish()

    def test_release_uses_original_collector(self):
        other=w.OwnerFileCounters()
        with launcher.file_accounting(self.work):
            context=launcher.writer_lease(self.output,scope=self.scope);context.__enter__()
        with launcher.file_accounting(other):context.__exit__(None,None,None)
        self.assertEqual(self.work.values['writerLeaseReleaseCompleted'],1);self.assertEqual(dict(other.values),{})

    def test_close_failure_is_not_retried_and_preserves_body_error(self):
        kernel=self.kernel();original_close=kernel.CloseHandle
        original_close.argtypes=[ctypes.c_void_p];original_close.restype=ctypes.c_int
        def close(handle):self.assertTrue(original_close(handle));ctypes.set_last_error(6);return 0
        kernel.CloseHandle=Mock(side_effect=close)
        failure=KeyboardInterrupt('body')
        with patch.object(ctypes,'WinDLL',return_value=kernel),self.assertRaises(KeyboardInterrupt) as caught:
            with self.scope.acquire(self.output,self.work):raise failure
        self.assertIs(caught.exception,failure);self.assertIn('release failed',failure.__notes__[0])
        self.scope.finish(failure);kernel.CloseHandle.assert_called_once()
        self.assertEqual(self.scope.snapshot()['currentOwnedHandles'],1)
        self.assertEqual(self.work.values['writerLeaseReleaseStateUnknown'],1)

    def test_acquired_handle_is_cleaned_if_initial_size_check_fails(self):
        kernel=self.kernel();size=kernel.GetFileSizeEx
        size.argtypes=[ctypes.c_void_p,ctypes.POINTER(ctypes.c_longlong)];size.restype=ctypes.c_int
        def fail_once(*args):
            kernel.GetFileSizeEx.side_effect=size;ctypes.set_last_error(6);return 0
        kernel.GetFileSizeEx=Mock(side_effect=fail_once)
        with patch.object(ctypes,'WinDLL',return_value=kernel),self.assertRaises(OSError) as caught:
            with self.scope.acquire(self.output):pass
        self.scope.finish(caught.exception)
        self.assertEqual(self.scope.snapshot()['entries'][1]['forcedCleanup'],1)
        self.assertFalse(Path(str(self.output)+'.writer.lock').exists())

    def test_size_failure_still_closes_and_retains_unknown_state(self):
        kernel=self.kernel();size=kernel.GetFileSizeEx
        size.argtypes=[ctypes.c_void_p,ctypes.POINTER(ctypes.c_longlong)];size.restype=ctypes.c_int
        calls=[]
        def queried(*args):
            calls.append(1)
            if len(calls)>1:ctypes.set_last_error(6);return 0
            return size(*args)
        kernel.GetFileSizeEx=Mock(side_effect=queried)
        with patch.object(ctypes,'WinDLL',return_value=kernel),self.assertRaises(OSError):
            with self.scope.acquire(self.output,self.work):pass
        with self.assertRaises(ValueError):self.scope.finish()
        self.assertFalse(Path(str(self.output)+'.writer.lock').exists())
        self.assertEqual(self.scope.snapshot()['currentOwnedHandles'],1)

    def test_failed_close_with_no_body_error_propagates(self):
        kernel=self.kernel();close=kernel.CloseHandle;close.argtypes=[ctypes.c_void_p];close.restype=ctypes.c_int
        def release(handle):self.assertTrue(close(handle));raise OSError('literal close')
        kernel.CloseHandle=Mock(side_effect=release)
        with patch.object(ctypes,'WinDLL',return_value=kernel),self.assertRaisesRegex(OSError,'literal close'):
            with self.scope.acquire(self.output):pass
        with self.assertRaises(ValueError):self.scope.finish()
        kernel.CloseHandle.assert_called_once()

    def test_metadata_failure_prevents_open_and_poison_scope(self):
        with patch.object(w,'unlinked',side_effect=ValueError('literal link')),patch.object(ctypes,'WinDLL') as dll:
            with self.assertRaisesRegex(ValueError,'literal link'):
                with self.scope.acquire(self.output):pass
        dll.assert_not_called()
        with self.assertRaises(ValueError):self.scope.finish()

    def test_acquire_completion_observer_failure_keeps_handle_for_cleanup(self):
        original=self.work.add
        def failed(name,*args):
            if name=='writerLeaseAcquireCompleted':raise RuntimeError('acquire observer')
            return original(name,*args)
        with patch.object(self.work,'add',side_effect=failed),self.assertRaisesRegex(RuntimeError,'acquire observer') as caught:
            with self.scope.acquire(self.output,self.work):pass
        self.scope.finish(caught.exception)
        value=self.scope.snapshot();self.assertEqual(value['entries'][1]['forcedCleanup'],1)
        self.assertEqual(value['currentOwnedHandles'],0);self.assertFalse(Path(str(self.output)+'.writer.lock').exists())

    def test_release_observer_failure_cannot_skip_or_repeat_close(self):
        for suffix in ('Attempted','Completed'):
            scope=leases.OwnerLeases(self.output,budget());work=w.OwnerFileCounters();original=work.add
            def failed(name,*args):
                if name=='writerLeaseRelease'+suffix:raise RuntimeError('release observer')
                return original(name,*args)
            with patch.object(work,'add',side_effect=failed),self.assertRaisesRegex(RuntimeError,'release observer'):
                with scope.acquire(self.output,work):pass
            with self.assertRaises(ValueError):scope.finish()
            self.assertFalse(Path(str(self.output)+'.writer.lock').exists())
            self.assertEqual(scope.snapshot()['currentOwnedHandles'],1)

    def test_alias_and_protected_paths_reject(self):
        for output in (self.registry,self.root/'con',self.root/'trailing.'):
            with self.subTest(output=output),self.assertRaises(ValueError):leases.OwnerLeases(output,budget())
        for p in (self.root,Path(str(self.output)+'.writer.lock')):
            with self.assertRaisesRegex(ValueError,'protected'):leases.OwnerLeases(self.output,budget(),protected_paths=[p])

    def test_declaration_and_snapshot_are_copied(self):
        value=budget();scope=leases.OwnerLeases(self.output,value);value.clear()
        snapshot=scope.snapshot();snapshot['entries'].clear();snapshot['declaration'].clear()
        self.assertEqual(scope.plan()['declaration'],budget());self.assertEqual(len(scope.snapshot()['entries']),2)

    def test_bad_declarations_reject(self):
        for v in (None,{},budget(True),budget(-1),budget(3),budget(2.0),budget()|dict(extra=0),budget()|dict(version='unknown')):
            with self.subTest(value=v),self.assertRaises(ValueError):leases.declaration(v)

    def test_observation_mutations_cannot_claim_success(self):
        original=self.complete()
        edits=[lambda v:v['entries'].pop(),lambda v:v['entries'][0].update(path='other'),lambda v:v['entries'][0].update(role='Other'),
            lambda v:v['entries'][0].update(attempts=2),lambda v:v['entries'][0].update(released=0),lambda v:v['entries'][0].update(forcedCleanup=1),
            lambda v:v['entries'][0].update(releaseFailures=1),lambda v:v.update(currentOwnedHandles=1),lambda v:v.update(peakOwnedHandles=3),
            lambda v:v.update(closed=False),lambda v:v.update(acceptedWriteBytes=1),lambda v:v.update(maxLeaseBytes=False),
            lambda v:v.update(wallClockBounded=True),lambda v:v.update(wholeProcessCoverage=True),lambda v:v.update(extra=0)]
        for edit in edits:
            value=copy.deepcopy(original);edit(value)
            with self.assertRaises(ValueError):leases.observation(value)


class SupervisorLeases(unittest.TestCase):
    def setUp(self):
        self.f=f.SupervisorStorage();self.f.setUp();self.addCleanup(self.f.doCleanups)

    def prepare(self,base=None,cap=2):
        self.f.ready(base=base)
        shutil.copyfile(leases.__file__,self.f.package/Path(leases.__file__).name);self.f.reseal()
        return f.s.StudyWorkSupervisor((base or self.f.base)/'accounting',storage_contracts=f.contracts(),owner_lease_budget=budget(cap))

    def test_complete_owner_binds_two_leases_and_retains_cleanup(self):
        export=os.environ.get('LL_OWNER_LEASE_SCOPE_EXPORT');base=Path(export).resolve()/'owner-v2' if export else None
        if base:base.parent.mkdir(exist_ok=True)
        s=self.prepare(base);original=self.f.routed
        def routed(*args,**kwargs):s.owner_leases.require_held();return original(*args,**kwargs)
        with patch.object(self.f,'routed',side_effect=routed):result=self.f.launch(s)
        v=self.f.verify(s);self.f.verify_archive_resources();self.assertEqual(result['status'],'Complete')
        self.assertEqual(v['ownerLeases']['outcome'],'Complete');self.assertEqual(leases.observation(v['ownerLeases']),budget())
        self.assertEqual(v['ownerLeases'],s.terminal_observation['storageOwnership']['ownerLeases'])
        binding=w.strict((s.root/'supervisor/storage-binding.json').read_bytes())
        self.assertEqual(binding['ownerLeasePlan'],s.owner_leases.plan())
        self.assertEqual(binding['sourceBindings'][Path(leases.__file__).name],f.sha(leases.__file__))
        self.assertEqual(len(self.f.calls),4)
        if export:
            w.seal_new(base/'terminal.json',s.terminal_observation)
            w.seal_new(base/'fixture.json',dict(syntheticProcessObservations=4,realProcessObservations=0,actualCombat=0,
                productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,wholeProcessCoverage=False,usableForAdmission=False))
            w.seal_new(base/'files.json',{p.relative_to(base).as_posix():f.sha(p) for p in sorted(base.rglob('*')) if p.is_file()})

    def test_requires_storage(self):
        with self.assertRaisesRegex(ValueError,'declared supervisor storage'):f.s.StudyWorkSupervisor(self.f.base/'out',owner_lease_budget=budget())

    def test_missing_or_changed_captured_module_rejects_before_start(self):
        s=self.prepare();(self.f.package/Path(leases.__file__).name).write_bytes(b'changed');self.f.reseal()
        with self.assertRaisesRegex(ValueError,'Accounting module differs'):self.f.launch(s)
        self.assertFalse(s.started);self.assertFalse(s.root.exists());self.assertFalse(self.f.output.exists())

    def test_failed_second_acquisition_releases_first_before_failure_receipt(self):
        s=self.prepare(cap=1)
        with self.assertRaisesRegex(ValueError,'concurrency'):self.f.launch(s)
        self.assertEqual(self.f.calls,[]);self.assertFalse(self.f.output.exists())
        v=s.final_observation['ownerLeases'];self.assertEqual(v['outcome'],'FailedOrIncomplete')
        self.assertEqual(v['currentOwnedHandles'],0);self.assertEqual(v['entries'][0]['released'],1)

    def test_worker_failure_preserves_error_and_cleanup(self):
        s=self.prepare();self.f.failed_phase='native'
        with self.assertRaisesRegex(ValueError,'Native comparison failed'):self.f.launch(s)
        v=self.f.verify(s);self.assertEqual(v['outcome'],'Failed');self.assertEqual(v['ownerLeases']['currentOwnedHandles'],0)
        self.assertEqual(v['ownerLeases']['outcome'],'FailedOrIncomplete')

    def test_watchdog_start_failure_releases_both(self):
        s=self.prepare();failure=RuntimeError('timer start')
        with patch.object(launcher.threading.Timer,'start',side_effect=failure),self.assertRaises(RuntimeError) as caught:self.f.launch(s)
        self.assertIs(caught.exception,failure);self.assertEqual(self.f.calls,[])
        self.assertTrue(all(e['released']==1 for e in s.final_observation['ownerLeases']['entries']))

    def test_mutated_binding_rejects_before_worker_and_still_closes(self):
        s=self.prepare();original=s.set_check
        def changed(check):
            original(check);(s.root/'supervisor/storage-binding.json').write_bytes(b'changed')
        with patch.object(s,'set_check',side_effect=changed),self.assertRaisesRegex(ValueError,'Changed diagnostic storage member'):self.f.launch(s)
        self.assertEqual(self.f.calls,[]);self.assertEqual(s.owner_leases.snapshot()['currentOwnedHandles'],0)

    def test_mutated_in_memory_plan_rejects_before_worker(self):
        s=self.prepare();original=s.set_check
        def changed(check):original(check);s.owner_leases._budget['maxConcurrentLeases']=1
        with patch.object(s,'set_check',side_effect=changed),self.assertRaisesRegex(ValueError,'concurrency'):self.f.launch(s)
        self.assertEqual(self.f.calls,[]);self.assertEqual(s.owner_leases.snapshot()['currentOwnedHandles'],0)

    def test_default_owner_schema_and_lease_path_remain_compatible(self):
        s=self.f.prepare();self.f.launch(s);v=self.f.verify(s)
        self.assertNotIn('ownerLeases',v);self.assertIsNone(s.owner_leases)

    def test_new_owner_scope_composes_with_all_v8_native_phases(self):
        spec=importlib.util.spec_from_file_location('combined_native_leases',ROOT/'build/test-proposal-native-leases.py')
        native=importlib.util.module_from_spec(spec);spec.loader.exec_module(native)
        fixture=native.Leases();fixture.setUp();self.addCleanup(fixture.doCleanups)
        export=os.environ.get('LL_OWNER_LEASE_SCOPE_EXPORT');base=Path(export).resolve()/'combined-v8' if export else None
        if base:base.parent.mkdir(exist_ok=True)
        s=fixture.prepare(base,owner_lease_budget=budget())
        shutil.copyfile(leases.__file__,fixture.f.package/Path(leases.__file__).name);fixture.f.reseal()
        fixture.f.launch(s);fixture.f.verify(s);fixture.f.verify_archive_resources()
        observed=s.terminal_observation['storageOwnership']
        self.assertTrue(observed['nativePending']['allNativePhasesComplete'])
        self.assertEqual(observed['ownerLeases']['outcome'],'Complete')
        self.assertEqual(len(fixture.f.calls),4)
        self.assertIn('nativeLeaseExternalMutationsAndHardElapsedLifetime',observed['missingCoverage'])
        if export:
            w.seal_new(base/'terminal.json',s.terminal_observation)
            w.seal_new(base/'fixture.json',dict(syntheticProcessObservations=4,realProcessObservations=0,actualCombat=0,
                productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,wholeProcessCoverage=False,usableForAdmission=False))
            w.seal_new(base/'files.json',{p.relative_to(base).as_posix():f.sha(p) for p in sorted(base.rglob('*')) if p.is_file()})

    def test_release_failure_prevents_success_after_four_workers(self):
        s=self.prepare();k=ctypes.WinDLL('kernel32',use_last_error=True)
        close=k.CloseHandle;close.argtypes=[ctypes.c_void_p];close.restype=ctypes.c_int
        def release(handle):self.assertTrue(close(handle));ctypes.set_last_error(6);return 0
        proxy=SimpleNamespace(CreateFileW=k.CreateFileW,GetFileSizeEx=k.GetFileSizeEx,CloseHandle=Mock(side_effect=release))
        with patch.object(ctypes,'WinDLL',return_value=proxy),self.assertRaises(OSError):self.f.launch(s)
        self.assertEqual(len(self.f.calls),4);v=self.f.verify(s)
        self.assertEqual(v['outcome'],'Failed');self.assertEqual(v['ownerLeases']['currentOwnedHandles'],2)
        self.assertTrue((self.f.output/'failure.json').exists());self.assertEqual(proxy.CloseHandle.call_count,2)


if __name__=='__main__':unittest.main()
