"""Lease/metadata correctness only; scientific workers are always routed."""
import contextlib
import ctypes
import importlib.util
import io
import json
import os
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as w
spec = importlib.util.spec_from_file_location('lease_owner_fixture', ROOT/'build/test-proposal-owner-supervisor.py')
f = importlib.util.module_from_spec(spec); spec.loader.exec_module(f)
owner = f.launcher


class LeaseAccounting(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory(); self.addCleanup(temp.cleanup)
        self.root = Path(temp.name); self.work = w.OwnerFileCounters()

    def fake_kernel(self, *, acquire=123, close=1):
        kernel = SimpleNamespace(CreateFileW=Mock(return_value=acquire), CloseHandle=Mock())
        def release(handle):
            ctypes.set_last_error(6)
            if isinstance(close, BaseException): raise close
            return close
        kernel.CloseHandle.side_effect = release
        return kernel

    def release_failure_proxy(self):
        # Release the real handle for test isolation, then simulate a failed API
        # result. The production observer cannot infer actual release from it.
        kernel = ctypes.WinDLL('kernel32', use_last_error=True)
        close = kernel.CloseHandle; close.argtypes = [ctypes.c_void_p]; close.restype = ctypes.c_int
        def release(handle):
            self.assertTrue(close(handle)); ctypes.set_last_error(6); return 0
        return SimpleNamespace(CreateFileW=kernel.CreateFileW, CloseHandle=Mock(side_effect=release))

    def test_real_exclusion_and_delete_on_close_with_and_without_counters(self):
        for active in (False, True):
            path = self.root/str(active); lock = Path(str(path)+'.writer.lock')
            with owner.file_accounting(self.work if active else None):
                with owner.writer_lease(path):
                    self.assertTrue(lock.exists()); self.assertEqual(lock.stat().st_size, 0)
                    with self.assertRaisesRegex(ValueError, 'already leased'):
                        with owner.writer_lease(path): self.fail('Contended lease acquired')
                self.assertFalse(lock.exists())
        self.assertEqual(self.work.values['writerLeaseAcquireAttempted'], 2)
        self.assertEqual(self.work.values['writerLeaseAcquireCompleted'], 1)
        self.assertEqual(self.work.values['writerLeaseAcquireFailed'], 1)
        self.assertEqual(self.work.values['writerLeaseReleaseCompleted'], 1)

    def test_missing_parent_does_not_attempt_release(self):
        with owner.file_accounting(self.work), self.assertRaisesRegex(ValueError, 'inaccessible'):
            with owner.writer_lease(self.root/'missing/child'): pass
        self.assertEqual(self.work.values['writerLeaseAcquireFailed'], 1)
        self.assertEqual(self.work.values['writerLeaseReleaseAttempted'], 0)

    def test_link_rejection_keeps_short_circuit_and_never_acquires(self):
        with patch.object(Path, 'is_symlink', return_value=True), patch.object(Path, 'is_junction') as junction:
            with owner.file_accounting(self.work), self.assertRaisesRegex(ValueError, 'Linked writer lease'):
                with owner.writer_lease(self.root/'output'): pass
        junction.assert_not_called()
        self.assertEqual(self.work.values['metadataSymlinkQueryCompleted'], 1)
        self.assertEqual(self.work.values['writerLeaseAcquireAttempted'], 0)

    def test_metadata_exception_is_preserved_before_acquisition(self):
        failure = PermissionError('literal metadata failure')
        with patch.object(Path, 'is_symlink', side_effect=failure), owner.file_accounting(self.work):
            with self.assertRaises(PermissionError) as caught:
                with owner.writer_lease(self.root/'output'): pass
        self.assertIs(caught.exception, failure)
        self.assertEqual(self.work.values['metadataSymlinkQueryFailed'], 1)
        self.assertEqual(self.work.values['writerLeaseAcquireAttempted'], 0)

    def test_false_close_is_error_and_unknown_state_even_without_accounting(self):
        for active in (False, True):
            kernel = self.fake_kernel(close=0)
            with patch.object(ctypes, 'WinDLL', return_value=kernel), owner.file_accounting(self.work if active else None):
                with self.assertRaises(OSError) as caught:
                    with owner.writer_lease(self.root/'output'): pass
            self.assertEqual(caught.exception.winerror, 6)
            kernel.CloseHandle.assert_called_once_with(123)
        self.assertEqual(self.work.values['writerLeaseReleaseFailed'], 1)
        self.assertEqual(self.work.values['writerLeaseReleaseStateUnknown'], 1)
        self.assertEqual(self.work.values['writerLeaseReleaseCompleted'], 0)

    def test_close_failure_preserves_body_exception_including_base_exceptions(self):
        for failure in (RuntimeError('body'), KeyboardInterrupt(), SystemExit(12)):
            kernel = self.fake_kernel(close=0)
            with patch.object(ctypes, 'WinDLL', return_value=kernel), owner.file_accounting(self.work):
                with self.assertRaises(type(failure)) as caught:
                    with owner.writer_lease(self.root/'output'): raise failure
            self.assertIs(caught.exception, failure)
            self.assertEqual(len(failure.__notes__), 1)
            self.assertIn('Writer lease release failed', failure.__notes__[0])
        self.assertEqual(self.work.values['writerLeaseReleaseFailed'], 3)

    def test_thrown_close_is_not_retried_or_replaced(self):
        failure = OSError('literal release failure'); kernel = self.fake_kernel(close=failure)
        with patch.object(ctypes, 'WinDLL', return_value=kernel), owner.file_accounting(self.work):
            with self.assertRaises(OSError) as caught:
                with owner.writer_lease(self.root/'output'): pass
        self.assertIs(caught.exception, failure); kernel.CloseHandle.assert_called_once()
        self.assertEqual(self.work.values['writerLeaseReleaseStateUnknown'], 1)

    def test_invalid_handle_is_never_closed(self):
        kernel = self.fake_kernel(acquire=ctypes.c_void_p(-1).value)
        with patch.object(ctypes, 'WinDLL', return_value=kernel), owner.file_accounting(self.work):
            with self.assertRaisesRegex(ValueError, 'already leased'):
                with owner.writer_lease(self.root/'output'): pass
        kernel.CloseHandle.assert_not_called()
        self.assertEqual(self.work.values['writerLeaseAcquireFailed'], 1)

    def test_release_uses_acquisition_collector_after_scope_changes(self):
        other = w.OwnerFileCounters()
        with owner.file_accounting(self.work):
            lease = owner.writer_lease(self.root/'output'); lease.__enter__()
        with owner.file_accounting(other): lease.__exit__(None, None, None)
        self.assertEqual(self.work.values['writerLeaseReleaseCompleted'], 1)
        self.assertEqual(dict(other.values), {})

    def test_exit_stack_preserves_original_and_records_both_release_failures(self):
        failure = RuntimeError('first failure'); kernel = self.fake_kernel(close=0)
        with patch.object(ctypes, 'WinDLL', return_value=kernel), owner.file_accounting(self.work):
            with self.assertRaises(RuntimeError) as caught:
                with contextlib.ExitStack() as stack:
                    stack.enter_context(owner.writer_lease(self.root/'one'))
                    stack.enter_context(owner.writer_lease(self.root/'two'))
                    raise failure
        self.assertIs(caught.exception, failure); self.assertEqual(len(failure.__notes__), 2)
        self.assertEqual(self.work.values['writerLeaseReleaseFailed'], 2)

    def test_inventory_metadata_and_storage_return_original_sizes(self):
        (self.root/'nested').mkdir(); (self.root/'nested/value').write_bytes(b'123')
        expected = owner.inventory(self.root)
        with owner.file_accounting(self.work):
            self.assertEqual(owner.inventory(self.root), expected)
            self.assertEqual(owner.storage_bytes(self.root), 3)
        self.assertEqual(self.work.values['directoryInventoryCompleted'], 2)
        self.assertEqual(self.work.values['metadataSymlinkQueryCompleted'], 4)
        self.assertEqual(self.work.values['metadataJunctionQueryCompleted'], 4)
        self.assertEqual(self.work.values['metadataLstatCompleted'], 1)
        self.assertFalse(any('Bytes' in k for k in self.work.values))

    def test_failed_inventory_is_not_completed(self):
        failure = PermissionError('walk')
        with patch.object(owner.os, 'walk', side_effect=failure), owner.file_accounting(self.work):
            with self.assertRaises(PermissionError) as caught: owner.inventory(self.root)
        self.assertIs(caught.exception, failure)
        self.assertEqual(self.work.values['directoryInventoryFailed'], 1)
        self.assertEqual(self.work.values['directoryInventoryCompleted'], 0)

    def test_disappearing_racing_lease_is_failed_stat_but_valid_storage_sample(self):
        path = self.root/'search/root-01/control/racing.writer.lock'
        with patch.object(owner, 'inventory', return_value=[path]), owner.file_accounting(self.work):
            self.assertEqual(owner.storage_bytes(self.root), 0)
        self.assertEqual(self.work.values['metadataLstatFailed'], 1)

    def test_pending_rename_keeps_failed_and_completed_stat_observations(self):
        path = self.root/'value.pending'; (self.root/'value').write_bytes(b'1234')
        with patch.object(owner, 'inventory', return_value=[path]), owner.file_accounting(self.work):
            self.assertEqual(owner.storage_bytes(self.root), 4)
        self.assertEqual(self.work.values['metadataLstatAttempted'], 2)
        self.assertEqual(self.work.values['metadataLstatFailed'], 1)
        self.assertEqual(self.work.values['metadataLstatCompleted'], 1)

    def fixture(self, base=None):
        fixture = f.Supervisor(); fixture.setUp(); self.addCleanup(fixture.doCleanups)
        supervisor = fixture.prepare(base)
        return fixture, supervisor

    def test_real_owner_retains_acquisition_and_release_in_its_receipt(self):
        export = os.environ.get('LL_LEASE_OWNER_EXPORT')
        base = Path(export) if export else self.root/'owner'
        fixture, supervisor = self.fixture(base)
        result = fixture.launch(supervisor); fixture.verify(supervisor); fixture.verify_archive_resources()
        self.assertEqual(result['status'], 'Complete')
        for operation in ('writerLeaseAcquire', 'writerLeaseRelease'):
            self.assertEqual(supervisor.counters.values[operation+'Completed'], 2)
        if export:
            w.seal_new(base/'lease-boundaries.json', dict(fixtureOnly=True, scientificWorkersRouted=True,
                realProcessObservations=False, actualCombat=0, productionEntropyDraws=0, usableForAdmission=False,
                counters=dict(supervisor.counters.values), sources={Path(p).resolve().relative_to(ROOT).as_posix():f.sha(p)
                    for p in (__file__, owner.__file__, w.__file__, f.__file__, f.s.__file__)}))
            w.seal_new(base/'files.json', {p.relative_to(base).as_posix():f.sha(p) for p in sorted(base.rglob('*')) if p.is_file()})

    def test_release_failure_prevents_default_success_console_and_marks_failure(self):
        fixture, _ = self.fixture()
        proxy = self.release_failure_proxy(); output = io.StringIO()
        with patch.object(ctypes, 'WinDLL', return_value=proxy), patch.object(f.owned, 'run', side_effect=fixture.routed), contextlib.redirect_stdout(output):
            with self.assertRaises(OSError):
                owner.launch(fixture.package/'request.json', fixture.package/'runtime/BalanceHarness.dll', 'dotnet', f.sha(fixture.package/'files.json'))
        self.assertEqual(output.getvalue(), '')
        self.assertEqual(json.loads((fixture.output/'failure.json').read_bytes())['status'], 'IncompleteComparison')
        self.assertEqual(proxy.CloseHandle.call_count, 2)

    def test_release_failure_makes_supervisor_failed_after_successful_routed_workers(self):
        fixture, supervisor = self.fixture(); proxy = self.release_failure_proxy()
        with patch.object(ctypes, 'WinDLL', return_value=proxy):
            with self.assertRaises(OSError): fixture.launch(supervisor)
        self.assertEqual(len(fixture.calls), 4)
        self.assertEqual(fixture.verify(supervisor)['outcome'], 'Failed')
        self.assertEqual(supervisor.counters.values['writerLeaseReleaseFailed'], 2)
        self.assertTrue((fixture.output/'failure.json').exists())

    def test_worker_failure_survives_two_release_failures(self):
        fixture, supervisor = self.fixture(); fixture.failed_phase = 'native'
        with patch.object(ctypes, 'WinDLL', return_value=self.release_failure_proxy()):
            with self.assertRaisesRegex(ValueError, 'Native comparison failed') as caught: fixture.launch(supervisor)
        self.assertEqual(len(caught.exception.__notes__), 2)
        self.assertEqual(fixture.verify(supervisor)['outcome'], 'Failed')
        self.assertIn('Native comparison failed', json.loads((fixture.output/'failure.json').read_bytes())['reason'])

    def test_second_acquisition_failure_survives_first_release_failure(self):
        fixture, supervisor = self.fixture(); proxy = self.release_failure_proxy()
        create = proxy.CreateFileW; calls = []
        def acquire(*args):
            calls.append(args)
            return create(*args) if len(calls) == 1 else ctypes.c_void_p(-1).value
        # Configure the actual function, since the proxy carries only Python attributes.
        create.argtypes = [ctypes.c_wchar_p, ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p, ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p]
        create.restype = ctypes.c_void_p; proxy.CreateFileW = Mock(side_effect=acquire)
        with patch.object(ctypes, 'WinDLL', return_value=proxy):
            with self.assertRaisesRegex(ValueError, 'already leased') as caught: fixture.launch(supervisor)
        self.assertEqual(len(caught.exception.__notes__), 1); self.assertEqual(fixture.calls, [])
        self.assertFalse(fixture.output.exists()); self.assertEqual(supervisor.final_observation['outcome'], 'Failed')
        self.assertEqual(supervisor.counters.values['writerLeaseReleaseFailed'], 1)

    def test_watchdog_start_failure_releases_both_acquired_handles(self):
        fixture, supervisor = self.fixture(); failure = RuntimeError('literal timer failure')
        with patch.object(owner.threading.Timer, 'start', side_effect=failure):
            with self.assertRaises(RuntimeError) as caught: fixture.launch(supervisor)
        self.assertIs(caught.exception, failure); self.assertEqual(fixture.calls, [])
        self.assertEqual(supervisor.counters.values['writerLeaseReleaseCompleted'], 2)
        self.assertFalse(Path(str(fixture.output)+'.writer.lock').exists())


if __name__ == '__main__': unittest.main()
