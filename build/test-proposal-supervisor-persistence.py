"""Bounded supervisor persistence fixtures; worker commands remain intercepted."""
import contextlib
import hashlib
import importlib.util
import io
import os
from pathlib import Path
import sys
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('persistence_fixture',ROOT/'build/test-proposal-owner-supervisor.py')
fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
w,s,launcher,owned=fixture.w,fixture.s,fixture.launcher,fixture.owned


def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class Persistence(unittest.TestCase):
    setUp=fixture.Supervisor.setUp
    prepare=fixture.Supervisor.prepare
    reseal=fixture.Supervisor.reseal
    routed=fixture.Supervisor.routed
    launch=fixture.Supervisor.launch
    verify=fixture.Supervisor.verify
    verify_archive_resources=fixture.Supervisor.verify_archive_resources

    def failed_launch(self,supervisor,kind=OSError,pattern='literal'):
        output=io.StringIO()
        with patch.object(owned,'run',side_effect=self.routed),contextlib.redirect_stdout(output):
            with self.assertRaisesRegex(kind,pattern) as caught:
                launcher.launch(self.package/'request.json',self.package/'runtime/BalanceHarness.dll','dotnet',
                                sha(self.package/'files.json'),supervisor)
        self.assertEqual(output.getvalue(),'')
        self.assertIsNone(supervisor.observation_manifest_sha256)
        self.assertIsNone(supervisor.manifest_sha256)
        self.assertTrue(supervisor.watchdog.finished.is_set())
        self.assertIsNone(launcher.FILE_WORK.get())
        value=supervisor.terminal_observation
        self.assertEqual(value['outcome'],'Failed');self.assertFalse(value['supervisorObservationPersistenceIncluded'])
        self.assertEqual(value['boundary'],'FailedObservationPublicationAttempt')
        self.assertIsNone(value['managedObservationStorage']);self.assertIsNone(value['observedCombinedBytes'])
        self.assertIsNotNone(value['persistenceError'])
        self.assertFalse(value['wholeProcessCoverage']);self.assertFalse(value['usableForAdmission'])
        return caught.exception

    def stream_fault(self,supervisor,fault,member='supervisor-observation.json'):
        original=Path.open
        class Stream:
            def __init__(self,inner):self.inner=inner
            @property
            def closed(self):return self.inner.closed
            def fileno(self):return self.inner.fileno()
            def write(self,data):
                if fault in ('write','write-close'):
                    self.inner.write(data[:3]);raise OSError('literal write failure')
                if fault=='zero':return 0
                if fault=='short':return self.inner.write(data[:max(1,len(data)//2)])
                return self.inner.write(data)
            def flush(self):
                if fault=='flush':raise OSError('literal flush failure')
                self.inner.flush()
            def close(self):
                self.inner.close()
                if fault in ('close','write-close'):raise OSError('literal close failure')
        def opened(path,mode='r',*args,**kwargs):
            target=path==supervisor.root/'terminal'/member and mode=='xb'
            if target and fault=='open':raise OSError('literal open failure')
            stream=original(path,mode,*args,**kwargs)
            return Stream(stream) if target else stream
        return patch.object(Path,'open',new=opened)

    def test_snapshot_is_persisted_and_exact_publication_bytes_are_separate(self):
        export=os.environ.get('LL_SUPERVISOR_PERSISTENCE_EXPORT')
        base=Path(export) if export else self.base
        supervisor=self.prepare(base);result=self.launch(supervisor)
        self.verify(supervisor);self.verify_archive_resources()
        terminal=supervisor.terminal_observation;folder=supervisor.root/'terminal'
        manifest=w.strict((folder/'files.json').read_bytes())
        self.assertEqual(set(manifest),{'supervisor-observation.json'})
        self.assertEqual(result['accountingObservationManifestSha256'],sha(folder/'files.json'))
        self.assertEqual(result['accountingManifestSha256'],sha(supervisor.root/'supervisor/files.json'))
        self.assertEqual(w.strict((folder/'supervisor-observation.json').read_bytes()),supervisor.final_observation)
        self.assertEqual(manifest['supervisor-observation.json'],sha(folder/'supervisor-observation.json'))
        self.assertEqual(terminal['outcome'],'Complete');self.assertTrue(terminal['supervisorObservationPersistenceIncluded'])
        self.assertTrue(supervisor.final_observation['supervisorObservationPersistenceExcluded'])
        self.assertTrue(terminal['terminalObservationPersistenceExcluded'])
        self.assertEqual(terminal['observationManifestSha256'],result['accountingObservationManifestSha256'])
        self.assertGreaterEqual(terminal['enclosingSeconds'],supervisor.final_observation['enclosingSeconds'])
        size=sum(p.stat().st_size for p in folder.iterdir());counts=terminal['publicationCounters']
        self.assertEqual(terminal['observedCombinedBytes'],supervisor.final_observation['observedCombinedBytes']+size)
        self.assertEqual(terminal['managedObservationStorage']['totalWrittenBytes'],size)
        self.assertEqual(sum(v for k,v in counts.items() if k.startswith('applicationWriteBytes.')),size)
        for role,name in [('json','supervisor-observation.json'),('manifest','files.json')]:
            self.assertEqual(counts['applicationReadBytes.'+role],(folder/name).stat().st_size)
            self.assertEqual(counts['applicationWriteBytes.'+role],(folder/name).stat().st_size)
        for op in ('managedFlushesCompleted','managedFileSyncsCompleted','managedClosesCompleted'):self.assertEqual(counts[op],2)
        self.assertEqual(terminal['managedObservationStorage']['currentScratchBytes'],0)
        self.assertNotIn('publicationCounters',manifest)
        self.assertFalse(terminal['wholeProcessCoverage']);self.assertFalse(terminal['usableForAdmission'])
        if export:
            w.seal_new(base/'terminal-observation.json',terminal)
            w.seal_new(base/'success-output.json',result)
            w.seal_new(base/'fixture.json',dict(fixtureOnly=True,routedWorkers=True,realProcessObservations=False,
                actualCombat=0,productionEntropyDraws=0,wholeProcessCoverage=False,usableForAdmission=False,
                sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p) for p in
                         (s.__file__,w.__file__,launcher.__file__,owned.__file__,__file__,fixture.__file__)}))
            w.seal_new(base/'files.json',{p.relative_to(base).as_posix():sha(p) for p in sorted(base.rglob('*')) if p.is_file()})

    def test_failed_worker_still_persists_failed_supervisor_observation(self):
        supervisor=self.prepare();self.failed_phase='native'
        with self.assertRaisesRegex(ValueError,'Native comparison failed'):self.launch(supervisor)
        self.verify(supervisor)
        value=supervisor.terminal_observation
        self.assertEqual(value['outcome'],'Failed');self.assertTrue(value['supervisorObservationPersistenceIncluded'])
        self.assertIsNone(value['persistenceError']);self.assertIsNotNone(value['managedObservationStorage'])
        persisted=w.strict((supervisor.root/'terminal/supervisor-observation.json').read_bytes())
        self.assertEqual(persisted['outcome'],'Failed')

    def test_serialization_failure_does_not_open_observation_or_print_success(self):
        supervisor=self.prepare();canonical=w.canonical
        def encode(value):
            if isinstance(value,dict) and value.get('version')=='tower-proposal-supervisor-observation-v1':raise ValueError('literal serialize failure')
            return canonical(value)
        with patch.object(w,'canonical',side_effect=encode):self.failed_launch(supervisor,ValueError)
        self.assertFalse((supervisor.root/'terminal/supervisor-observation.json').exists())

    def test_open_failure_preserves_previous_stage_manifests(self):
        supervisor=self.prepare()
        with self.stream_fault(supervisor,'open'):self.failed_launch(supervisor)
        self.assertTrue((supervisor.root/'owner/files.json').exists())
        self.assertTrue((supervisor.root/'supervisor/files.json').exists())
        self.assertEqual(supervisor.terminal_observation['publicationCounters'],{})

    def test_short_writes_are_retried_and_accepted_bytes_equal_files(self):
        supervisor=self.prepare()
        with self.stream_fault(supervisor,'short'):self.launch(supervisor)
        self.verify(supervisor)
        counts=supervisor.terminal_observation['publicationCounters']
        self.assertGreater(counts['managedWriteCallsCompleted'],2)
        self.assertEqual(counts['applicationWriteBytes.json'],(supervisor.root/'terminal/supervisor-observation.json').stat().st_size)

    def test_zero_write_rejects_false_completion_and_closes(self):
        supervisor=self.prepare()
        with self.stream_fault(supervisor,'zero'):self.failed_launch(supervisor,ValueError,'Incomplete supervisor observation')
        counts=supervisor.terminal_observation['publicationCounters']
        self.assertEqual(counts['applicationWriteBytes.json'],0);self.assertEqual(counts['managedClosesCompleted'],1)

    def test_failed_write_progress_stays_unknown(self):
        supervisor=self.prepare()
        with self.stream_fault(supervisor,'write'):self.failed_launch(supervisor)
        counts=supervisor.terminal_observation['publicationCounters']
        self.assertEqual(counts['failedManagedWriteBytesUnknown'],1)
        self.assertNotIn('applicationWriteBytes.json',counts)
        self.assertEqual(counts['managedClosesCompleted'],1)
        self.assertEqual((supervisor.root/'terminal/supervisor-observation.json').stat().st_size,3)

    def test_flush_failure_closes_without_claiming_sync(self):
        supervisor=self.prepare()
        with self.stream_fault(supervisor,'flush'):self.failed_launch(supervisor)
        counts=supervisor.terminal_observation['publicationCounters']
        self.assertEqual(counts['managedFlushesFailed'],1);self.assertNotIn('managedFileSyncsAttempted',counts)
        self.assertEqual(counts['managedClosesCompleted'],1)

    def test_sync_failure_prevents_success_and_closes(self):
        supervisor=self.prepare();real_sync=w.os.fsync
        def sync(fd):
            if hasattr(supervisor,'observation_publication') and supervisor.observation_publication.live:raise OSError('literal sync failure')
            return real_sync(fd)
        with patch.object(w.os,'fsync',side_effect=sync):self.failed_launch(supervisor)
        counts=supervisor.terminal_observation['publicationCounters']
        self.assertEqual(counts['managedFileSyncsFailed'],1);self.assertEqual(counts['managedClosesCompleted'],1)

    def test_close_failure_never_claims_completed_persistence(self):
        supervisor=self.prepare()
        with self.stream_fault(supervisor,'close'):self.failed_launch(supervisor)
        counts=supervisor.terminal_observation['publicationCounters']
        self.assertEqual(counts['managedClosesFailed'],1);self.assertNotIn('managedClosesCompleted',counts)

    def test_original_write_error_survives_secondary_close_failure(self):
        supervisor=self.prepare()
        with self.stream_fault(supervisor,'write-close'):error=self.failed_launch(supervisor,pattern='literal write')
        self.assertIn('literal close',str(error.__notes__))

    def test_original_worker_failure_survives_observation_failure(self):
        supervisor=self.prepare();self.failed_phase='native'
        with self.stream_fault(supervisor,'write-close'):error=self.failed_launch(supervisor,ValueError,'Native comparison failed')
        self.assertIn('literal write',str(error.__notes__))

    def test_manifest_failure_keeps_closed_snapshot_without_success_pin(self):
        supervisor=self.prepare()
        with self.stream_fault(supervisor,'write',member='files.json'):self.failed_launch(supervisor)
        snapshot=w.strict((supervisor.root/'terminal/supervisor-observation.json').read_bytes())
        self.assertEqual(snapshot['version'],'tower-proposal-supervisor-observation-v1')
        counts=supervisor.terminal_observation['publicationCounters']
        self.assertEqual(counts['managedClosesCompleted'],2);self.assertEqual(counts['managedFileSyncsCompleted'],1)

    def test_same_length_snapshot_tampering_is_detected_after_manifest_close(self):
        supervisor=self.prepare();original=supervisor._seal_observation
        def seal(name,value):
            result=original(name,value)
            if name=='files.json':
                path=supervisor.root/'terminal/supervisor-observation.json'
                data=path.read_bytes();self.assertIn(b'"Complete"',data)
                path.write_bytes(data.replace(b'"Complete"',b'"Tampered"',1))
            return result
        with patch.object(supervisor,'_seal_observation',side_effect=seal):
            self.failed_launch(supervisor,ValueError,'Changed supervisor observation')

    def test_untracked_member_prevents_sealed_observation(self):
        supervisor=self.prepare();original=supervisor._seal_observation
        def seal(name,value):
            result=original(name,value)
            if name=='files.json':(supervisor.root/'terminal/extra').write_bytes(b'x')
            return result
        with patch.object(supervisor,'_seal_observation',side_effect=seal):self.failed_launch(supervisor,ValueError,'Untracked storage')

    def test_final_hash_read_failure_preserves_writes_as_partial_evidence(self):
        supervisor=self.prepare()
        with patch.object(supervisor.observation_counters,'sha',side_effect=OSError('literal read failure')):
            self.failed_launch(supervisor)
        counts=supervisor.terminal_observation['publicationCounters']
        self.assertEqual(counts['managedClosesCompleted'],2)

    def test_existing_snapshot_is_not_overwritten(self):
        supervisor=self.prepare();original=supervisor._seal_observation
        def seal(name,value):
            if name=='supervisor-observation.json':(supervisor.root/'terminal'/name).write_bytes(b'previous')
            return original(name,value)
        with patch.object(supervisor,'_seal_observation',side_effect=seal):self.failed_launch(supervisor,FileExistsError,'.*')
        self.assertEqual((supervisor.root/'terminal/supervisor-observation.json').read_bytes(),b'previous')

    def test_existing_shared_deadline_applies_to_new_persistence(self):
        supervisor=self.prepare();original=supervisor._seal_observation
        def seal(name,value):
            with patch.object(launcher.time,'monotonic',return_value=supervisor.started_at+launcher.SECONDS+1):return original(name,value)
        with patch.object(supervisor,'_seal_observation',side_effect=seal):self.failed_launch(supervisor,ValueError,'elapsed allowance')

    def test_new_persistence_bytes_cannot_bypass_shared_audit_reserve(self):
        supervisor=self.prepare();original=supervisor._seal_observation;scan=launcher.storage_bytes
        def seal(name,value):
            def inflated(path):return scan(path)+(launcher.AUDIT_BYTES if Path(path)==supervisor.root else 0)
            with patch.object(launcher,'storage_bytes',side_effect=inflated):return original(name,value)
        with patch.object(supervisor,'_seal_observation',side_effect=seal):self.failed_launch(supervisor,ValueError,'Audit/publication storage reserve')

    def test_checks_run_before_and_after_each_new_file_and_after_verification(self):
        supervisor=self.prepare();original=supervisor._persist_observation;seen=[]
        def persist(failure):
            check=supervisor.bound_check
            def checked():
                seen.append((tuple(supervisor.observation_publication.files),bool(supervisor.observation_publication.live)))
                return check()
            with patch.object(supervisor,'bound_check',side_effect=checked):return original(failure)
        with patch.object(supervisor,'_persist_observation',side_effect=persist):self.launch(supervisor)
        self.assertEqual(len(seen),5);self.assertFalse(any(live for _,live in seen))
        self.assertEqual([len(files) for files,_ in seen],[0,1,1,2,2])

    def test_watchdog_and_manifest_are_valid_when_success_output_starts(self):
        supervisor=self.prepare();seen=[]
        def output(text):
            self.assertFalse(supervisor.watchdog.finished.is_set())
            value=w.strict(text);self.assertEqual(value['accountingObservationManifestSha256'],sha(supervisor.root/'terminal/files.json'))
            self.assertEqual(supervisor.terminal_observation['publicationCounters']['managedClosesCompleted'],2)
            seen.append(value)
        with patch.object(owned,'run',side_effect=self.routed),patch('builtins.print',side_effect=output):
            launcher.launch(self.package/'request.json',self.package/'runtime/BalanceHarness.dll','dotnet',sha(self.package/'files.json'),supervisor)
        self.assertEqual(len(seen),1);self.assertTrue(supervisor.watchdog.finished.is_set())

    def test_console_failure_is_still_an_explicit_uncovered_tail(self):
        supervisor=self.prepare();error=BrokenPipeError('literal console failure')
        with patch.object(owned,'run',side_effect=self.routed),patch('builtins.print',side_effect=error):
            with self.assertRaises(BrokenPipeError) as caught:
                launcher.launch(self.package/'request.json',self.package/'runtime/BalanceHarness.dll','dotnet',sha(self.package/'files.json'),supervisor)
        self.assertIs(caught.exception,error);self.assertTrue(supervisor.watchdog.finished.is_set())
        self.assertEqual(supervisor.terminal_observation['outcome'],'Complete')
        self.assertIn('finalConsoleAndProcessExit',supervisor.terminal_observation['missingCoverage'])


if __name__=='__main__':unittest.main()
