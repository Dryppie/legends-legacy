"""Real enclosing owner/nested jobs with literal workers; never a scientific run."""
import atexit
import contextlib
import importlib.util
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
import proposal_owner_process as monitor
import proposal_work_accounting as w
import bounded_windows_process as owned

spec = importlib.util.spec_from_file_location('literal_owner_fixture', ROOT/'build/test-proposal-owner-supervisor.py')
fixture = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fixture)


def fixture_owner(base, mode):
    """Run the existing admission/publication owner; route every scientific worker.

    Four separate literal jobs prove nesting. The routed worker receipts and their
    inner supervisor observations remain synthetic. Only the enclosing job is used
    to claim real owner/descendant lifetime coverage.
    """
    f = fixture.Supervisor()
    f.base = Path(base)
    f.package = f.base/'package'
    f.output = f.base/'registry/result'
    f.q = w.strict((f.package/'request.json').read_bytes())
    f.calls = []
    f.failed_phase = 'native' if mode == 'worker-fail' else None
    f.missing_receipt_phase = None
    supervisor = fixture.s.StudyWorkSupervisor(f.base/'accounting')
    real_run = owned.run

    def routed(*args, **kwargs):
        observations = []
        index = len(f.calls)
        result = real_run([sys.executable, '-B', '-c', 'print("literal nested worker",flush=True)'],
            f.base, f.base/('nested-'+str(index)+'.log'), time.monotonic()+4, observe=observations.append)
        if result['exitCode'] != 0:
            raise RuntimeError('Literal nested job failed')
        w.seal_new(f.base/('nested-'+str(index)+'.json'), dict(result=result, observation=observations[0]))
        return f.routed(*args, **kwargs)

    def console_failure(*args, **kwargs):
        raise BrokenPipeError('literal final console failure')

    atexit.register(lambda: print('OWNER_ATEXIT', flush=True))
    try:
        with patch.object(owned, 'run', side_effect=routed):
            output = patch('builtins.print', side_effect=console_failure) if mode == 'console-fail' else contextlib.nullcontext()
            with output:
                fixture.launcher.launch(f.package/'request.json', f.package/'runtime/BalanceHarness.dll', 'dotnet',
                    monitor.sha(f.package/'files.json'), accounting=supervisor)
    finally:
        if supervisor.terminal_observation is not None:
            w.seal_new(f.base/'terminal-observation.json', supervisor.terminal_observation)
        # Deliberate work after supervisor persistence: covered by the outer job.
        tail = bytearray(24*1024*1024)
        tail[0] = 1
        scratch = f.base/'late-owner.tmp'
        scratch.write_bytes(b'x'*131072)
        scratch.read_bytes()
        scratch.unlink()
        print('OWNER_TAIL', flush=True)


class OwnerProcess(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.f = fixture.Supervisor()
        self.f.setUp()
        self.addCleanup(self.f.doCleanups)

    def literal(self, code='print("owner done",flush=True)', **kwargs):
        script = self.base/'owner.py'
        script.write_text(code, encoding='utf-8')
        observer = monitor.OwnerProcessMonitor(self.base/'monitor', 'a'*64)
        self.observer = observer
        result = observer.run(script, [], self.base, time.monotonic()+6, **kwargs)
        return observer, result

    def routed(self, mode='success', export=False):
        if export and os.environ.get('LL_OWNER_PROCESS_EXPORT'):
            self.base = Path(os.environ['LL_OWNER_PROCESS_EXPORT'])
            self.base.mkdir()
        supervisor = self.f.prepare(self.base/'owner')
        if mode in ('compressed', 'changed-admission'):
            if mode == 'compressed':
                self.f.q['evidenceStorage'] = {'version':'literal'}
                (self.f.package/'request.json').write_bytes(w.canonical(self.f.q))
                self.f.reseal()
            else:
                (self.f.package/'context.json').write_bytes(b'changed')
        observer = monitor.OwnerProcessMonitor(self.base/'monitor', monitor.sha(self.f.package/'request.json'))
        self.observer = observer
        result = observer.run(__file__, ['--fixture-owner', str(self.base/'owner'), mode], ROOT, time.monotonic()+12,
            protected_roots=[self.f.package, self.f.output.parent, supervisor.root])
        return observer, result

    def verify(self, observer):
        root = observer.root
        manifest = w.strict((root/'files.json').read_bytes())
        self.assertEqual(set(manifest), {p.name for p in root.iterdir()}-{'files.json'})
        self.assertEqual(monitor.sha(root/'files.json'), observer.manifest_sha256)
        for name, pin in manifest.items():
            self.assertEqual(monitor.sha(root/name), pin)
        value = w.strict((root/'owner-process.json').read_bytes())
        self.assertEqual(value, observer.observation)
        self.assertFalse(value['wholeProcessCoverage'])
        self.assertFalse(value['usableForAdmission'])
        self.assertFalse(value['scientificOutcomeVerified'])
        self.assertTrue(value['outerMonitorPublicationExcluded'])
        self.assertIn('outerMonitorPublicationVerificationConsoleAndExit', value['missingCoverage'])
        return value

    def test_real_routed_owner_nested_workers_console_exit_and_late_memory(self):
        observer, result = self.routed(export=True)
        value = self.verify(observer)
        console = (observer.root/'console.log').read_text(encoding='utf-8')
        self.assertEqual(result['exitCode'], 0, console)
        self.assertEqual(value['processOutcome'], 'ExitedZero')
        self.assertEqual(value['observationOutcome'], 'Complete')
        self.assertTrue(value['ownerConsoleAndExitObserved'])
        self.assertTrue(value['ownerJobLifetimeMemoryObserved'])
        self.assertGreaterEqual(value['ownerJobPeakCommitBytes'], 24*1024*1024)
        # The bundled interpreter may itself use a launcher process. Count every
        # associated process, without assuming one process per Python command.
        self.assertGreaterEqual(result['totalProcesses'], 5)
        self.assertEqual(value['ownerJobIo']['totalProcessesAtQuery'], result['totalProcesses'])
        self.assertEqual(len(list((self.base/'owner').glob('nested-*.json'))), 4)
        for path in (self.base/'owner').glob('nested-*.json'):
            nested = w.strict(path.read_bytes())
            self.assertEqual(nested['result']['exitCode'], 0)
            self.assertEqual(nested['result']['activeProcesses'], 0)
            self.assertTrue(nested['observation']['jobDrained'])
        self.assertEqual(value['ownerJobIo']['activeProcessesAtQuery'], 0)
        self.assertEqual(value['console']['retainedBytes'], (observer.root/'console.log').stat().st_size)
        self.assertEqual(value['console']['sha256'], monitor.sha(observer.root/'console.log'))
        self.assertIn('OWNER_TAIL', console)
        self.assertTrue(console.rstrip().endswith('OWNER_ATEXIT'))
        success = json.loads(console.splitlines()[0])
        terminal = w.strict((self.base/'owner/terminal-observation.json').read_bytes())
        self.assertEqual(terminal['outcome'], 'Complete')
        self.assertEqual(success['accountingObservationManifestSha256'], monitor.sha(self.base/'owner/accounting/terminal/files.json'))
        self.assertEqual(success['accountingManifestSha256'], monitor.sha(self.base/'owner/accounting/supervisor/files.json'))
        self.assertTrue(terminal['terminalObservationPersistenceExcluded'])
        self.assertIn('wholeOwnerMemoryLifetime', terminal['missingCoverage'])
        self.assertGreaterEqual(value['ownerJobIo']['counters']['writeTransferBytes'], 131072+value['console']['retainedBytes'])
        self.assertFalse((self.base/'owner/late-owner.tmp').exists())
        raw = value['processObservation']
        self.assertEqual(value['monitorLifetimePeakCommitBytes'], raw['ownerLifetimePeakCommitBytes'])
        self.assertEqual(value['ownerJobPeakCommitBytes'], raw['peakJobCommitBytes'])
        self.assertGreaterEqual(value['enclosingSeconds'], raw['enclosingSeconds'])
        self.assertEqual(value['ownerDriverSha256'], monitor.sha(__file__))
        self.assertEqual(value['processHelperSha256'], monitor.sha(owned.__file__))
        self.assertLessEqual(result['workAllowanceSeconds']+2, 12)
        if os.environ.get('LL_OWNER_PROCESS_EXPORT'):
            w.seal_new(self.base/'fixture.json', dict(fixtureOnly=True, routedWorkers=True,
                syntheticSupervisorWorkerObservations=True, realEnclosingOwnerJob=True, realNestedLiteralJobs=4,
                actualCombat=0, productionEntropyDraws=0, scientificLaunches=0,
                literalTailAllocationBytes=24*1024*1024, literalDeletedTailFileBytes=131072,
                sources={Path(p).resolve().relative_to(ROOT).as_posix():monitor.sha(p) for p in
                    (monitor.__file__, owned.__file__, w.__file__, fixture.s.__file__, fixture.launcher.__file__, fixture.__file__, __file__)}))
            w.seal_new(self.base/'files.json', {p.relative_to(self.base).as_posix():monitor.sha(p)
                for p in sorted(self.base.rglob('*')) if p.is_file()})

    def test_final_console_failure_cannot_report_zero_owner_exit(self):
        observer, result = self.routed('console-fail')
        value = self.verify(observer)
        self.assertNotEqual(result['exitCode'], 0)
        self.assertEqual(value['processOutcome'], 'ExitedNonzero')
        self.assertEqual(value['observationOutcome'], 'Complete')
        self.assertEqual(w.strict((self.base/'owner/terminal-observation.json').read_bytes())['outcome'], 'Complete')
        self.assertIn('literal final console failure', (observer.root/'console.log').read_text())

    def test_worker_failure_is_still_an_observed_owner_failure(self):
        observer, result = self.routed('worker-fail')
        value = self.verify(observer)
        self.assertNotEqual(result['exitCode'], 0)
        self.assertEqual(value['processOutcome'], 'ExitedNonzero')
        self.assertEqual(value['observationOutcome'], 'Complete')
        self.assertGreaterEqual(result['totalProcesses'], 2)
        self.assertEqual(len(list((self.base/'owner').glob('nested-*.json'))), 1)
        self.assertEqual(w.strict((self.base/'owner/terminal-observation.json').read_bytes())['outcome'], 'Failed')

    def test_existing_compressed_guard_rejects_before_supervisor_or_nested_jobs(self):
        observer, result = self.routed('compressed')
        self.verify(observer)
        self.assertNotEqual(result['exitCode'], 0)
        self.assertFalse(list((self.base/'owner').glob('nested-*.json')))
        self.assertFalse((self.base/'owner/accounting').exists())
        self.assertIn('recovery gate is closed', (observer.root/'console.log').read_text())

    def test_changed_admission_rejects_before_supervisor_or_nested_jobs(self):
        observer, result = self.routed('changed-admission')
        self.verify(observer)
        self.assertNotEqual(result['exitCode'], 0)
        self.assertFalse(list((self.base/'owner').glob('nested-*.json')))
        self.assertFalse((self.base/'owner/accounting').exists())
        self.assertIn('Changed admitted input', (observer.root/'console.log').read_text())

    def test_printed_success_does_not_override_later_abrupt_exit(self):
        observer, result = self.literal('import os\nprint("Complete",flush=True)\nos._exit(7)')
        value = self.verify(observer)
        self.assertEqual(result['exitCode'], 7)
        self.assertEqual(value['processOutcome'], 'ExitedNonzero')
        self.assertTrue(value['ownerConsoleAndExitObserved'])

    def test_parent_job_waits_for_late_descendant_after_root_exit(self):
        child = 'import time; time.sleep(.12); print("DESCENDANT_TAIL",flush=True)'
        observer, result = self.literal('import subprocess,sys\nsubprocess.Popen([sys.executable,"-B","-c",'+repr(child)+'])')
        value = self.verify(observer)
        self.assertEqual(result['exitCode'], 0)
        self.assertGreaterEqual(result['totalProcesses'], 2)
        self.assertIn('DESCENDANT_TAIL', (observer.root/'console.log').read_text())
        self.assertEqual(value['observationOutcome'], 'Complete')

    def test_timeout_drains_descendants_and_keeps_failure_separate_from_coverage(self):
        script = self.base/'owner.py'
        script.write_text('import subprocess,sys\nsubprocess.Popen([sys.executable,"-B","-c","import time;time.sleep(20)"])')
        observer = monitor.OwnerProcessMonitor(self.base/'monitor', 'a'*64)
        result = observer.run(script, [], self.base, time.monotonic()+3)
        value = self.verify(observer)
        self.assertTrue(result['timedOut'])
        self.assertGreaterEqual(result['totalProcesses'], 2)
        self.assertEqual(result['activeProcesses'], 0)
        self.assertEqual(value['processOutcome'], 'TimedOut')
        self.assertEqual(value['observationOutcome'], 'Complete')

    def test_unknown_io_is_not_zero_or_complete(self):
        original = owned._query_job
        def query(*args):
            if args[1] == 8:
                raise OSError('literal I/O query failure')
            return original(*args)
        with patch.object(owned, '_query_job', side_effect=query):
            observer, result = self.literal()
        value = self.verify(observer)
        self.assertEqual(result['exitCode'], 0)
        self.assertEqual(value['observationOutcome'], 'Incomplete')
        self.assertIsNone(value['ownerJobIo']['counters'])
        self.assertTrue(value['ownerJobLifetimeMemoryObserved'])

    def test_unknown_memory_is_not_zero_or_complete(self):
        with patch.object(owned, '_memory_info', side_effect=OSError('literal memory query failure')):
            observer, result = self.literal()
        value = self.verify(observer)
        self.assertEqual(result['exitCode'], 0)
        self.assertFalse(value['ownerJobLifetimeMemoryObserved'])
        self.assertIsNone(value['ownerJobPeakCommitBytes'])
        self.assertEqual(value['observationOutcome'], 'Incomplete')
        self.assertEqual(value['ownerJobIo']['coverage'], 'KernelJobLifetimeTotals')

    def test_unverified_job_cannot_claim_owner_memory(self):
        with patch.object(owned, '_is_in_job', side_effect=OSError('literal membership failure')):
            with self.assertRaisesRegex(OSError, 'membership failure'):
                self.literal()
        value = self.verify(self.observer)
        self.assertEqual(value['processOutcome'], 'MonitorFailed')
        self.assertFalse(value['ownerConsoleAndExitObserved'])
        self.assertFalse(value['ownerJobLifetimeMemoryObserved'])
        self.assertIsNone(value['ownerJobPeakCommitBytes'])

    def test_create_job_failure_retains_unknown_observation_without_console(self):
        with patch.object(owned, '_create_job', side_effect=OSError('literal job creation failure')):
            with self.assertRaisesRegex(OSError, 'job creation failure'):
                self.literal()
        value = self.verify(self.observer)
        self.assertIsNone(value['console'])
        self.assertIsNone(value['processObservation'])
        self.assertEqual(value['observationOutcome'], 'Incomplete')

    def failing_check(self):
        self.checks = 0
        error = RuntimeError('literal resource check failed')
        def check():
            self.checks += 1
            if self.checks == 2:
                raise error
        return error, check

    def test_check_failure_kills_job_retains_observation_and_preserves_error(self):
        error, check = self.failing_check()
        with self.assertRaises(RuntimeError) as caught:
            self.literal('import time;time.sleep(20)', check=check)
        self.assertIs(caught.exception, error)
        value = self.verify(self.observer)
        self.assertEqual(value['processOutcome'], 'MonitorFailed')
        self.assertTrue(value['processObservation']['jobDrained'])
        self.assertFalse(value['ownerConsoleAndExitObserved'])

    def test_publication_error_does_not_replace_original_check_error(self):
        error, check = self.failing_check()
        with patch.object(monitor.OwnerProcessMonitor, '_seal', side_effect=OSError('literal publication failure')):
            with self.assertRaises(RuntimeError) as caught:
                self.literal('import time;time.sleep(20)', check=check)
        self.assertIs(caught.exception, error)
        self.assertIn('literal publication failure', str(error.__notes__))
        self.assertIsNone(self.observer.manifest_sha256)
        self.assertTrue(self.observer.process_observation['jobDrained'])

    def test_existing_destination_and_repeat_call_preserve_evidence(self):
        observer, _ = self.literal()
        before = {p.name:p.read_bytes() for p in observer.root.iterdir()}
        for candidate, message in ((observer, 'cannot run twice'),
                (monitor.OwnerProcessMonitor(observer.root, 'a'*64), 'already exists')):
            with self.assertRaisesRegex(ValueError, message):
                candidate.run(self.base/'owner.py', [], self.base, time.monotonic()+6)
        self.assertEqual(before, {p.name:p.read_bytes() for p in observer.root.iterdir()})

    def test_protected_roots_reject_both_overlap_directions(self):
        script = self.base/'owner.py'
        script.write_text('raise AssertionError("must not run")')
        root = self.base/'monitor'
        for protected in (root, self.base, root/'child'):
            observer = monitor.OwnerProcessMonitor(root, 'a'*64)
            with self.assertRaisesRegex(ValueError, 'disjoint'):
                observer.run(script, [], self.base, time.monotonic()+6, protected_roots=[protected])
            self.assertFalse(root.exists())

    def test_invalid_allowances_have_no_side_effects(self):
        script = self.base/'owner.py'
        script.write_text('raise AssertionError("must not run")')
        for options in ({'deadline':float('nan')}, {'deadline':time.monotonic()},
                {'cleanup_seconds':0}, {'cleanup_seconds':3}, {'publication_seconds':0},
                {'publication_seconds':float('inf')}, {'publication_seconds':True}):
            with self.subTest(options=options):
                observer = monitor.OwnerProcessMonitor(self.base/'monitor', 'a'*64)
                options = dict(options)
                deadline = options.pop('deadline', time.monotonic()+6)
                with self.assertRaisesRegex(ValueError, 'allowance'):
                    observer.run(script, [], self.base, deadline, **options)
                self.assertFalse(observer.root.exists())

    @contextlib.contextmanager
    def stream_fault(self, operation, *, short=False, zero=False, secondary_close=False):
        original = Path.open
        def opened(path, *args, **kwargs):
            stream = original(path, *args, **kwargs)
            if path.name != 'owner-process.json' or not args or args[0] != 'xb':
                return stream
            class Stream:
                def write(self, data):
                    if operation == 'write':
                        raise OSError('literal write failure')
                    return 0 if zero else stream.write(data[:17] if short else data)
                def flush(self):
                    if operation == 'flush':
                        raise OSError('literal flush failure')
                    return stream.flush()
                def fileno(self):
                    return stream.fileno()
                def close(self):
                    stream.close()
                    if operation == 'close' or secondary_close:
                        raise OSError('literal close failure')
            return Stream()
        with patch.object(Path, 'open', opened):
            yield

    def test_short_publication_writes_are_completed(self):
        with self.stream_fault(None, short=True):
            observer, _ = self.literal()
        self.verify(observer)

    def test_zero_publication_write_has_no_success_pin(self):
        with self.stream_fault(None, zero=True), self.assertRaisesRegex(ValueError, 'Incomplete owner observation write'):
            self.literal()
        self.assertIsNone(self.observer.manifest_sha256)
        self.assertIsNotNone(self.observer.publication_error)

    def test_publication_open_failure_keeps_process_observation_without_pin(self):
        original = Path.open
        def opened(path, *args, **kwargs):
            if path.name == 'owner-process.json' and args and args[0] == 'xb':
                raise OSError('literal publication open failure')
            return original(path, *args, **kwargs)
        with patch.object(Path, 'open', opened), self.assertRaisesRegex(OSError, 'publication open failure'):
            self.literal()
        self.assertEqual(self.observer.result['exitCode'], 0)
        self.assertIsNone(self.observer.manifest_sha256)

    def test_console_hash_failure_cannot_publish_a_snapshot_or_pin(self):
        original = monitor.sha
        def sha(path):
            if Path(path).name == 'console.log':
                raise OSError('literal console hash failure')
            return original(path)
        with patch.object(monitor, 'sha', side_effect=sha), self.assertRaisesRegex(OSError, 'console hash failure'):
            self.literal()
        self.assertIsNone(self.observer.observation)
        self.assertIsNone(self.observer.manifest_sha256)
        self.assertTrue(self.observer.process_observation['jobDrained'])

    def test_external_storage_check_also_applies_after_publication(self):
        def check():
            if (self.base/'monitor/files.json').exists():
                raise ValueError('literal storage allowance exhausted')
        with self.assertRaisesRegex(ValueError, 'storage allowance'):
            self.literal(check=check)
        self.assertEqual(self.observer.result['exitCode'], 0)
        self.assertIsNone(self.observer.manifest_sha256)

    def test_write_error_survives_secondary_close_error(self):
        with self.stream_fault('write', secondary_close=True), self.assertRaisesRegex(OSError, 'write failure') as caught:
            self.literal()
        self.assertIn('close failure', str(caught.exception.__notes__))
        self.assertIsNone(self.observer.manifest_sha256)

    def test_flush_sync_close_and_manifest_failures_never_publish_success_pin(self):
        for operation in ('flush', 'sync', 'close', 'manifest'):
            with self.subTest(operation=operation):
                with tempfile.TemporaryDirectory() as directory:
                    self.base = Path(directory)
                    if operation == 'sync':
                        fault = patch.object(monitor.os, 'fsync', side_effect=OSError('literal sync failure'))
                    elif operation == 'manifest':
                        original = monitor.OwnerProcessMonitor._seal
                        def seal(instance, name, value):
                            if name == 'files.json':
                                raise OSError('literal manifest failure')
                            return original(instance, name, value)
                        fault = patch.object(monitor.OwnerProcessMonitor, '_seal', seal)
                    else:
                        fault = self.stream_fault(operation)
                    with fault, self.assertRaisesRegex(OSError, operation+' failure'):
                        self.literal()
                    self.assertIsNone(self.observer.manifest_sha256)

    def test_same_length_console_tampering_is_rejected_before_success_pin(self):
        original = monitor.OwnerProcessMonitor._seal
        def seal(instance, name, value):
            pin = original(instance, name, value)
            if name == 'files.json':
                path = instance.root/'console.log'
                path.write_bytes(b'x'*path.stat().st_size)
            return pin
        with patch.object(monitor.OwnerProcessMonitor, '_seal', seal):
            with self.assertRaisesRegex(ValueError, 'Changed owner monitor publication'):
                self.literal()
        self.assertIsNone(self.observer.manifest_sha256)

    def test_untracked_file_rejects_manifest_publication(self):
        original = monitor.OwnerProcessMonitor._publish
        def publish(instance):
            (instance.root/'unexpected').write_bytes(b'literal')
            original(instance)
        with patch.object(monitor.OwnerProcessMonitor, '_publish', publish):
            with self.assertRaisesRegex(ValueError, 'Untracked owner monitor member'):
                self.literal()
        self.assertFalse((self.observer.root/'files.json').exists())

    def test_publication_deadline_is_checked_after_owner_exit(self):
        original = monitor.OwnerProcessMonitor._publish
        def publish(instance):
            with patch.object(monitor.time, 'monotonic', return_value=instance.deadline+1):
                original(instance)
        with patch.object(monitor.OwnerProcessMonitor, '_publish', publish):
            with self.assertRaisesRegex(ValueError, 'elapsed allowance'):
                self.literal()
        self.assertEqual(self.observer.result['exitCode'], 0)
        self.assertIsNone(self.observer.manifest_sha256)


if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == '--fixture-owner':
        fixture_owner(*sys.argv[2:])
    else:
        unittest.main()
