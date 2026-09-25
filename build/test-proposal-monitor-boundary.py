"""Literal monitor-call boundaries; no scientific worker or qualification run."""
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
import proposal_owner_process as m
import proposal_work_accounting as w
import bounded_windows_process as owned
spec = importlib.util.spec_from_file_location('monitor_boundary_fixture', ROOT/'build/test-proposal-owner-process.py')
f = importlib.util.module_from_spec(spec); spec.loader.exec_module(f)


class FaultFile:
    def __init__(self, stream, mode, error, close_error=None):
        self.stream, self.mode, self.error, self.close_error = stream, mode, error, close_error
    def write(self, raw):
        if self.mode == 'partial-fail':
            self.stream.write(raw[:3]); raise self.error
        if self.mode == 'zero': return 0
        if self.mode == 'short': return self.stream.write(raw[:max(1,len(raw)//2)])
        return self.stream.write(raw)
    def flush(self):
        if self.mode == 'flush': raise self.error
        return self.stream.flush()
    def fileno(self): return self.stream.fileno()
    def close(self):
        self.stream.close()
        if self.close_error is not None: raise self.close_error


class MonitorBoundary(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(); self.addCleanup(temporary.cleanup)
        self.base = Path(temporary.name)
        self.script = self.base/'literal.py'; self.script.write_text('print("literal owner",flush=True)',encoding='utf-8')
        self.monitor = m.OwnerProcessMonitor(self.base/'monitor', 'a'*64, monitor_accounting=True)

    def run_monitor(self, **options):
        return self.monitor.run(self.script, [], self.base, time.monotonic()+8, **options)

    def counters(self, phase): return self.monitor.monitor_terminal_observation['phaseCounters'][phase]

    def verify(self, monitor=None):
        monitor = monitor or self.monitor
        values = w.strict((monitor.root/'files.json').read_bytes())
        self.assertEqual(set(values), {p.name for p in monitor.root.iterdir()}-{'files.json'})
        for name,pin in values.items(): self.assertEqual(m.sha(monitor.root/name),pin)
        terminal = monitor.monitor_terminal_observation
        self.assertEqual(terminal['ownerObservationManifestSha256'], m.sha(monitor.root/'files.json'))
        self.assertFalse(terminal['wholeProcessCoverage']); self.assertFalse(terminal['usableForAdmission'])
        self.assertTrue(terminal['terminalObservationPersistenceExcluded'])
        self.assertIn('externalAndTransientScratch',terminal['missingCoverage'])
        self.assertIn('callerConsoleAndMonitorProcessExit',terminal['missingCoverage'])
        return terminal

    def fault_open(self, mode, error, close_error=None):
        original = Path.open
        def opening(path, *args, **kwargs):
            stream = original(path,*args,**kwargs)
            if path == self.monitor.root/'owner-process.json' and args and args[0] == 'xb':
                return FaultFile(stream,mode,error,close_error)
            return stream
        return patch.object(Path,'open',opening)

    def test_default_retains_v1_package_without_terminal_accounting(self):
        self.monitor = m.OwnerProcessMonitor(self.base/'monitor','a'*64)
        self.assertEqual(self.run_monitor()['exitCode'],0)
        self.assertIsNone(self.monitor.monitor_terminal_observation)
        self.assertEqual(set(p.name for p in self.monitor.root.iterdir()),{'owner-process.json','console.log','files.json'})
        self.assertEqual(self.monitor.observation['version'],'tower-proposal-owner-process-v1')
        self.assertTrue(self.monitor.observation['outerMonitorPublicationExcluded'])

    def test_real_call_counts_preparation_publication_and_verification_separately(self):
        self.assertEqual(self.run_monitor()['exitCode'],0)
        value = self.verify()
        self.assertEqual(value['outcome'],'Complete'); self.assertEqual(value['ownerObservationOutcome'],'Complete')
        self.assertEqual(list(value['phaseCounters']),['preparation','ownerProcess','observation','publication','verification'])
        self.assertGreaterEqual(value['enclosingSeconds'],self.monitor.observation['enclosingSeconds'])
        prep = self.counters('preparation')
        self.assertEqual(prep['hashFileCompleted'],5)
        paths = [self.script,Path(m.__file__),Path(owned.__file__),Path(w.__file__),Path(sys.executable)]
        self.assertEqual(sum(v for k,v in prep.items() if k.startswith('applicationReadBytes.')),sum(p.stat().st_size for p in paths))
        self.assertEqual(prep['directoryCreateCompleted'],1)
        publication = self.counters('publication')
        for name in ('Open','Flush','Sync','Close'): self.assertEqual(publication['publication'+name+'Completed'],2)
        for name,role in (('owner-process.json','json'),('files.json','manifest')):
            self.assertEqual(publication['publicationAcceptedWriteBytes.'+role],(self.monitor.root/name).stat().st_size)
        self.assertEqual(self.counters('verification')['hashFileCompleted'],3)
        self.assertEqual(self.counters('ownerProcess')['childLogFilesCreated'],1)
        self.assertEqual(self.counters('ownerProcess')['ownedProcessCallCompleted'],1)

    def test_real_nested_owner_tail_is_observed_without_claiming_scratch_peak(self):
        export = os.environ.get('LL_MONITOR_BOUNDARY_EXPORT')
        base = Path(export) if export else self.base/'nested'
        base.mkdir()
        fixture = f.fixture.Supervisor(); fixture.setUp(); self.addCleanup(fixture.doCleanups)
        supervisor = fixture.prepare(base/'owner')
        monitor = m.OwnerProcessMonitor(base/'monitor',m.sha(fixture.package/'request.json'),monitor_accounting=True)
        result = monitor.run(f.__file__,['--fixture-owner',str(base/'owner'),'success'],ROOT,time.monotonic()+15,
            protected_roots=[fixture.package,fixture.output.parent,supervisor.root])
        terminal = self.verify(monitor)
        self.assertEqual(result['exitCode'],0,(monitor.root/'console.log').read_text())
        self.assertGreaterEqual(result['totalProcesses'],5)
        self.assertEqual(len(list((base/'owner').glob('nested-*.json'))),4)
        self.assertFalse((base/'owner/late-owner.tmp').exists())
        self.assertGreaterEqual(monitor.observation['ownerJobIo']['counters']['writeTransferBytes'],131072)
        self.assertEqual(terminal['storageInterpretation'],'SelectedClosedFileLengthSamples;NoWholeProcessPeak')
        self.assertEqual(terminal['sourceBindings']['monitorProducerSha256'],m.sha(m.__file__))
        if export:
            # This persistence is deliberately outside the monitor-call snapshot.
            w.seal_new(base/'monitor-call.json',terminal)
            w.seal_new(base/'fixture.json',dict(fixtureOnly=True,scientificWorkersRouted=True,realEnclosingOwnerJob=True,
                realNestedLiteralJobs=4,literalDeletedTailFileBytes=131072,actualCombat=0,productionEntropyDraws=0,
                scientificLaunches=0,terminalObservationPersistedByCaller=True,usableForAdmission=False,
                sources={Path(p).resolve().relative_to(ROOT).as_posix():m.sha(p) for p in
                    (__file__,m.__file__,w.__file__,owned.__file__,f.__file__,f.fixture.__file__,f.fixture.launcher.__file__,f.fixture.s.__file__)}))
            w.seal_new(base/'files.json',{p.relative_to(base).as_posix():m.sha(p) for p in sorted(base.rglob('*')) if p.is_file()})

    def test_invalid_accounting_option_is_rejected(self):
        for value in (None,0,1,'yes'):
            with self.assertRaisesRegex(ValueError,'Invalid monitor accounting'):
                m.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=value)

    def test_invalid_preparation_has_no_files_and_no_authentication_claim(self):
        self.script.unlink()
        with self.assertRaisesRegex(ValueError,'trusted owner'): self.run_monitor()
        terminal = self.monitor.monitor_terminal_observation
        self.assertEqual(terminal['outcome'],'Failed'); self.assertEqual(terminal['failedPhase'],'preparation')
        self.assertIsNone(terminal['sourceBindings']); self.assertFalse(terminal['publicationVerified'])
        self.assertFalse(self.monitor.root.exists()); self.assertFalse(self.monitor.used)

    def test_invalid_deadline_is_observed_before_creation_or_launch(self):
        with patch.object(owned,'run') as launch:
            with self.assertRaisesRegex(ValueError,'Invalid owner monitor allowance'):
                self.monitor.run(self.script,[],self.base,float('nan'))
        launch.assert_not_called(); self.assertFalse(self.monitor.root.exists())
        self.assertEqual(list(self.monitor.monitor_terminal_observation['phaseCounters']),['preparation'])

    def test_hash_failure_preserves_error_and_unknown_read_progress(self):
        error = OSError('literal binding read failure'); original = Path.open
        def opening(path,*args,**kwargs):
            if path == self.script and args and args[0] == 'rb': raise error
            return original(path,*args,**kwargs)
        with patch.object(Path,'open',opening):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error)
        self.assertEqual(self.counters('preparation')['hashFileFailed'],1)
        self.assertEqual(self.counters('preparation')['failedHashReadBytesUnknown'],1)
        self.assertFalse(self.monitor.root.exists())

    def test_directory_creation_failure_retains_bound_preparation(self):
        error = PermissionError('literal mkdir failure'); original = Path.mkdir
        def mkdir(path,*args,**kwargs):
            if path == self.monitor.root: raise error
            return original(path,*args,**kwargs)
        with patch.object(Path,'mkdir',mkdir):
            with self.assertRaises(PermissionError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error)
        self.assertEqual(self.counters('preparation')['directoryCreateFailed'],1)
        self.assertIsNotNone(self.monitor.monitor_terminal_observation['sourceBindings'])

    def test_caller_check_failure_is_counted_and_preserved(self):
        error = RuntimeError('literal bound check')
        def check(): raise error
        with self.assertRaises(RuntimeError) as caught: self.run_monitor(check=check)
        self.assertIs(caught.exception,error)
        self.assertEqual(self.counters('preparation')['callerResourceCheckFailed'],1)
        self.assertFalse(self.monitor.root.exists())

    def test_process_creation_failure_still_counts_failed_observation_publication(self):
        error = OSError('literal job failure')
        with patch.object(owned,'_create_job',side_effect=error):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error); terminal = self.verify()
        self.assertEqual(terminal['outcome'],'Failed'); self.assertEqual(terminal['failedPhase'],'ownerProcess')
        self.assertEqual(terminal['ownerObservationOutcome'],'Incomplete')
        self.assertTrue(terminal['publicationVerified'])
        self.assertEqual(self.counters('ownerProcess')['ownedProcessCallFailed'],1)
        self.assertEqual(self.counters('publication')['publicationCloseCompleted'],2)

    def test_nonzero_child_is_complete_monitor_call_without_scientific_success(self):
        self.script.write_text('raise SystemExit(7)')
        self.assertEqual(self.run_monitor()['exitCode'],7)
        self.assertEqual(self.verify()['outcome'],'Complete')
        self.assertEqual(self.monitor.observation['processOutcome'],'ExitedNonzero')
        self.assertFalse(self.monitor.observation['scientificOutcomeVerified'])

    def test_missing_job_io_does_not_become_complete_owner_coverage(self):
        original = owned._query_job
        def query(*args):
            if args[1] == 8: raise OSError('literal I/O query failure')
            return original(*args)
        with patch.object(owned,'_query_job',side_effect=query): self.run_monitor()
        terminal = self.verify()
        self.assertEqual(terminal['outcome'],'Complete')
        self.assertEqual(terminal['ownerObservationOutcome'],'Incomplete')
        self.assertIsNone(self.monitor.observation['ownerJobIo']['counters'])

    def test_publication_open_failure_never_claims_a_close(self):
        error = OSError('literal open failure'); original = Path.open
        def opening(path,*args,**kwargs):
            if path == self.monitor.root/'owner-process.json' and args and args[0] == 'xb': raise error
            return original(path,*args,**kwargs)
        with patch.object(Path,'open',opening):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error)
        self.assertEqual(self.counters('publication')['publicationOpenFailed'],1)
        self.assertNotIn('publicationCloseAttempted',self.counters('publication'))
        self.assertIsNone(self.monitor.manifest_sha256)

    def test_hidden_partial_write_and_failed_close_keep_primary_and_unknown_bytes(self):
        error = OSError('literal partial write'); close_error = OSError('literal close')
        with self.fault_open('partial-fail',error,close_error):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error); self.assertIn('literal close',str(error.__notes__))
        counters = self.counters('publication')
        self.assertEqual(counters['publicationWriteFailed'],1)
        self.assertEqual(counters['failedPublicationWriteBytesUnknown'],1)
        self.assertNotIn('publicationAcceptedWriteBytes.json',counters)
        self.assertEqual(counters['publicationCloseFailed'],1)
        self.assertEqual(counters['sampledTrackedFileBytes'],3)
        self.assertEqual(self.monitor.monitor_terminal_observation['outcome'],'Failed')

    def test_short_writes_accumulate_actual_accepted_bytes(self):
        with self.fault_open('short',None): self.run_monitor()
        self.verify(); counters = self.counters('publication')
        self.assertGreater(counters['publicationWriteCompleted'],2)
        self.assertEqual(counters['publicationAcceptedWriteBytes.json'],(self.monitor.root/'owner-process.json').stat().st_size)

    def test_zero_write_is_not_completed_or_assigned_invented_bytes(self):
        with self.fault_open('zero',None):
            with self.assertRaisesRegex(ValueError,'Incomplete owner observation write'): self.run_monitor()
        self.assertEqual(self.counters('publication')['publicationWriteFailed'],1)
        self.assertNotIn('publicationAcceptedWriteBytes.json',self.counters('publication'))

    def test_flush_failure_still_closes_and_never_syncs(self):
        error = OSError('literal flush failure')
        with self.fault_open('flush',error):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error)
        self.assertEqual(self.counters('publication')['publicationFlushFailed'],1)
        self.assertEqual(self.counters('publication')['publicationCloseCompleted'],1)
        self.assertNotIn('publicationSyncAttempted',self.counters('publication'))

    def test_sync_failure_still_closes_and_fails_publication(self):
        error = OSError('literal sync failure')
        with patch.object(m.os,'fsync',side_effect=error):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error)
        self.assertEqual(self.counters('publication')['publicationSyncFailed'],1)
        self.assertEqual(self.counters('publication')['publicationCloseCompleted'],1)

    def test_close_failure_is_a_failed_publication(self):
        error = OSError('literal close failure')
        with self.fault_open('normal',None,error):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error)
        self.assertEqual(self.counters('publication')['publicationCloseFailed'],1)
        self.assertFalse(self.monitor.monitor_terminal_observation['publicationVerified'])

    def test_changed_manifest_fails_verification_after_successful_hash_call(self):
        original = self.monitor._sha
        def sha(path):
            if self.monitor._monitor_phase == 'verification' and Path(path).name == 'files.json':
                with Path(path).open('ab') as stream: stream.write(b' ')
            return original(path)
        with patch.object(self.monitor,'_sha',side_effect=sha):
            with self.assertRaisesRegex(ValueError,'Changed owner monitor publication'): self.run_monitor()
        self.assertEqual(self.counters('verification')['hashFileCompleted'],3)
        self.assertNotIn('hashFileFailed',self.counters('verification'))
        self.assertEqual(self.monitor.monitor_terminal_observation['failedPhase'],'verification')
        self.assertFalse(self.monitor.monitor_terminal_observation['publicationVerified'])

    def test_publication_failure_does_not_replace_process_failure(self):
        error = OSError('literal original job failure')
        with patch.object(owned,'_create_job',side_effect=error), patch.object(self.monitor,'_seal',side_effect=OSError('literal publication failure')):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error); self.assertIn('literal publication failure',str(error.__notes__))
        terminal = self.monitor.monitor_terminal_observation
        self.assertEqual(terminal['failedPhase'],'ownerProcess')
        self.assertIn('literal publication failure',terminal['publicationError'])

    def test_terminal_failure_preserves_original_error(self):
        error = OSError('literal original job failure')
        with patch.object(owned,'_create_job',side_effect=error), patch.object(self.monitor,'_monitor_snapshot',side_effect=OSError('literal terminal failure')):
            with self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error); self.assertIn('literal terminal failure',str(error.__notes__))
        self.assertIsNone(self.monitor.monitor_terminal_observation); self.assertFalse(self.monitor._monitor_active)

    def test_reuse_preserves_terminal_and_sealed_files(self):
        self.run_monitor(); terminal = w.canonical(self.verify())
        files = {p.name:p.read_bytes() for p in self.monitor.root.iterdir()}
        with self.assertRaisesRegex(ValueError,'cannot run twice'): self.run_monitor()
        self.assertEqual(w.canonical(self.monitor.monitor_terminal_observation),terminal)
        self.assertEqual({p.name:p.read_bytes() for p in self.monitor.root.iterdir()},files)

    def test_preparation_failure_also_cannot_be_overwritten_by_retry(self):
        self.script.unlink()
        with self.assertRaises(ValueError): self.run_monitor()
        terminal = w.canonical(self.monitor.monitor_terminal_observation)
        self.script.write_text('raise AssertionError("must not run")')
        with self.assertRaisesRegex(ValueError,'cannot run twice'): self.run_monitor()
        self.assertEqual(w.canonical(self.monitor.monitor_terminal_observation),terminal)
        self.assertFalse(self.monitor.root.exists())


if __name__ == '__main__': unittest.main()
