"""Literal routed-worker tests of real owner admission, limits and publication.

Worker commands are intercepted; no native preparation, entropy or combat runs.
Process observations in this fixture are explicitly synthetic.
"""
import contextlib
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
ANALYSIS = ROOT/'Balance Harness/analysis'
sys.path.insert(0, str(ANALYSIS))
import proposal_work_accounting as w
import proposal_owner_supervisor as s
import bounded_windows_process as owned
spec = importlib.util.spec_from_file_location('supervised_launcher', ROOT/'build/run-proposal-affinity-study.py')
launcher = importlib.util.module_from_spec(spec); spec.loader.exec_module(launcher)
audit_spec = importlib.util.spec_from_file_location('supervised_resource_auditor', ANALYSIS/'audit-proposal-affinity-study.py')
auditor = importlib.util.module_from_spec(audit_spec); audit_spec.loader.exec_module(auditor)


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class Supervisor(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.calls = []
        self.failed_phase = self.missing_receipt_phase = None

    def prepare(self, base=None):
        base = base or self.base
        base.mkdir(exist_ok=True)
        package = base/'package'; package.mkdir()
        registry = base/'registry'; registry.mkdir()
        (package/'runtime').mkdir(); (package/'runtime/BalanceHarness.dll').write_bytes(b'literal fixture; never executable')
        (package/'content').mkdir()
        for source, name in [(launcher.__file__, 'run-proposal-affinity-study.py'), (owned.__file__, 'bounded_windows_process.py'),
                             (ANALYSIS/'audit-proposal-affinity-study.py', 'auditor.py'), (w.__file__, 'proposal_work_accounting.py'),
                             (s.__file__, 'proposal_owner_supervisor.py')]: shutil.copyfile(source, package/name)
        q = dict(version=launcher.VERSION, outputRoot=str(registry/'result'), registryRoot=str(registry), contentRoot=str(package/'content'))
        for name in ('plan','context','settings','history','runtime'):
            path = package/(name+'.json'); launcher.write(path, dict(version=launcher.VERSION, fixtureOnly=True))
            q[name] = dict(path=str(path), sha256=sha(path))
        q['auditor'] = dict(path=str(package/'auditor.py'), sha256=sha(package/'auditor.py'))
        launcher.write(package/'request.json', q)
        self.package, self.output, self.q = package, registry/'result', q
        self.reseal()
        return s.StudyWorkSupervisor(base/'accounting')

    def reseal(self):
        for name in ('admission.json', 'files.json'):
            path = self.package/name
            if path.exists(): path.unlink()
        launcher.write(self.package/'admission.json', dict(version=self.q['version'], status='ProposalStudyAdmittedNoReservation',
            requestSha256=sha(self.package/'request.json'), fights=0, newValues=0, fixtureOnly=True))
        launcher.write(self.package/'files.json', {p.relative_to(self.package).as_posix():sha(p) for p in launcher.inventory(self.package)})

    def routed(self, command, cwd, log_path, deadline, cleanup_seconds, check, file_work=None, observe=None):
        phase = ('independentAudit' if '--working' in command else {
            'tower-proposal-study-run':'native', 'tower-proposal-study-audit':'nativeAudit',
            'tower-proposal-study-publication-check':'publication'}[command[2]])
        self.calls.append((phase, command, deadline))
        check()
        result = dict(decision='LiteralFixture', fixtureOnly=True)
        bound = '--work-binding' in command
        if bound:
            pos = command.index('--work-binding'); binding_path = Path(command[pos+1])
            pin = command[command.index('--work-binding-pin')+1] if phase == 'independentAudit' else command[pos+2]
            self.assertEqual(sha(binding_path), pin)
            binding = w.strict(binding_path.read_bytes())
            self.assertEqual(binding['requestSha256'], sha(self.output/'request.json'))
            self.assertEqual(binding['phase'], phase)
            self.assertFalse(Path(binding['receiptPath']).is_relative_to(self.output))
            self.assertIn(binding['version'],(w.WORKER_PUBLICATION_BINDING_VERSION,w.WORKER_PERSISTENCE_BINDING_VERSION,w.WORKER_SIDECAR_BINDING_VERSION,w.WORKER_PHASE_PENDING_BINDING_VERSION,w.WORKER_FAMILY_PENDING_BINDING_VERSION,w.WORKER_NATIVE_LEASE_BINDING_VERSION))
            self.assertIn('publicationPath',binding)
        scope = contextlib.nullcontext(None) if file_work is None else file_work.child_log(log_path)
        with scope as observed_log:
            with Path(log_path).open('xb') as log:
                if observed_log is not None: observed_log.opened()
                log.write(json.dumps(result).encode() if phase == 'nativeAudit' else b'literal fixture log')
            if phase != self.failed_phase:
                if phase == 'native':
                    launcher.write(self.output/'provisional-result.json', result)
                    launcher.write(self.output/'native-receipt.json', dict(version=launcher.VERSION, status='MeasuredPendingAudits',
                        requestFileHash=sha(self.output/'request.json'), newAuditFights=0, fights=0, fixtureOnly=True))
                elif phase == 'independentAudit':
                    launcher.write(self.output/'independent-audit.json', dict(status='Passed', requestFileHash=sha(self.output/'request.json'),
                        newFights=0, newValues=0, result=result, fixtureOnly=True))
            if bound and phase != self.missing_receipt_phase:
                if phase == 'independentAudit':
                    try:
                        with w.worker_receipt(self.output, phase, binding_path, pin, self.package/'auditor.py') as counters:
                            counters.add('fixtureLiteralInvocations')
                            if phase == self.failed_phase: raise RuntimeError('Literal worker failure')
                    except RuntimeError:
                        if phase != self.failed_phase: raise
                else:
                    counters = w.Counters(); counters.add('fixtureLiteralInvocations')
                    # Routed native commands are still synthetic. Exercise the
                    # common publication schema without claiming native execution.
                    limits=binding.get('sidecarByteLimits')
                    with (w.SidecarWrites(limits) if limits is not None else contextlib.nullcontext()) as sidecars, w.receipt_publication_scope(binding['publicationPath'],pin,phase,
                            binding['requestSha256'],binding['producerSha256'],
                            persistence_path=binding.get('publicationPersistencePath'),sidecars=sidecars) as publication:
                        publication.open(binding['receiptPath'])
                        receipt=counters.receipt(phase,binding['requestSha256'],binding['producerSha256'],phase!=self.failed_phase)
                        if binding['version'] in (w.WORKER_PHASE_PENDING_BINDING_VERSION,w.WORKER_FAMILY_PENDING_BINDING_VERSION,w.WORKER_NATIVE_LEASE_BINDING_VERSION):
                            families=binding['version']!=w.WORKER_PHASE_PENDING_BINDING_VERSION
                            if families:
                                family=w.proposal_pending_families_budget(self.output,phase,binding['pendingFamiliesBudget'])
                                budget=dict(files=w.pending_family_fixed(self.output,phase,family),maxLiveBytes=family['maxLiveBytes'],maxTotalWrittenBytes=family['maxTotalWrittenBytes'])
                            else: budget=w.proposal_pending_budget(self.output,phase,binding['pendingStorageBudget'])
                            receipt.update(version=w.NATIVE_FAMILY_PENDING_RECEIPT_VERSION if families else w.NATIVE_PHASE_PENDING_RECEIPT_VERSION,bindingSha256=pin,
                                pendingStorage=dict(version=w.NATIVE_PENDING_FAMILIES_STORAGE_VERSION if families else w.NATIVE_PENDING_VERSION,outcome='Complete' if phase!=self.failed_phase else 'FailedOrIncomplete',
                                    maxLiveBytes=budget['maxLiveBytes'],maxTotalWrittenBytes=budget['maxTotalWrittenBytes'],
                                    acceptedWriteBytes=0,trackedLiveBytes=0,peakTrackedLiveBytes=0,publishedBytes=0,deletedBytes=0,
                                    failedProgressMayBeUnknown=phase==self.failed_phase,contracts=budget['files'],
                                    creates=[dict(path=f['path'],count=0) for f in budget['files']],scope='DeclaredInstrumentedPendingWritersOnly',
                                    filesystemConfinement=False,wholeProcessCoverage=False,usableForAdmission=False))
                            if families:receipt['pendingStorage'].update(studyRoot=str(self.output),phase=phase,familyBudget=family)
                        if binding['version']==w.WORKER_NATIVE_LEASE_BINDING_VERSION:
                            budget=w.proposal_native_lease_budget(self.output,phase,binding['nativeLeaseBudget'])
                            receipt.update(version=w.NATIVE_LEASE_RECEIPT_VERSION,nativeLeases=dict(
                                version=w.NATIVE_LEASE_VERSION,studyRoot=str(self.output),phase=phase,budget=budget,
                                outcome='Complete' if phase!=self.failed_phase else 'FailedOrIncomplete',maxLeaseBytes=0,acceptedWriteBytes=0,
                                currentOwnedHandles=0,peakOwnedHandles=0,failedOrUnknown=phase==self.failed_phase,
                                entries=[dict(**e,attempts=0,acquired=0,released=0,heldConflicts=0,releaseFailures=0,forcedCleanup=0)
                                    for e in w.native_lease_paths(self.output,phase)],scope='DeclaredNativeLeaseHandlesOnly',
                                enclosingOwnerLifetimeBounded=False,wallClockBounded=False,filesystemConfinement=False,
                                wholeProcessCoverage=False,usableForAdmission=False))
                        publication.publish(lambda:receipt)
            if observe is not None:
                observe(dict(version='tower-owned-process-observation-v1', includesHandleCleanup=True, usableForAdmission=False,
                    jobDrained=True, enclosingSeconds=.01, memoryCoverage='Unknown', peakJobCommitBytes=None,
                    ownerLifetimePeakCommitBytes=None, combinedCommitUpperBoundBytes=None,
                    exitCode=1 if phase == self.failed_phase else 0, timedOut=False,
                    ownerError=None, memoryBoundary='BeforeHandleCleanup', fixtureOnly=True))
        check()
        return dict(exitCode=1 if phase == self.failed_phase else 0, timedOut=False, activeProcesses=0,
                    seconds=.01, totalProcesses=1, rootPid=1, fixtureOnly=True)

    def launch(self, accounting):
        output = io.StringIO()
        with patch.object(owned, 'run', side_effect=self.routed), contextlib.redirect_stdout(output):
            launcher.launch(self.package/'request.json', self.package/'runtime/BalanceHarness.dll', 'dotnet',
                            sha(self.package/'files.json'), accounting=accounting)
        return json.loads(output.getvalue())

    def verify(self, supervisor):
        root = supervisor.root
        for folder in (root/'owner', root/'supervisor', root/'terminal'):
            manifest = w.strict((folder/'files.json').read_bytes())
            self.assertEqual(set(manifest), {p.name for p in folder.iterdir()}-{'files.json'})
            for name, pin in manifest.items(): self.assertEqual(sha(folder/name), pin)
        observation = supervisor.final_observation
        self.assertEqual(observation['supervisorManifestSha256'], sha(root/'supervisor/files.json'))
        self.assertEqual(observation['ownerManifestSha256'], sha(root/'owner/files.json'))
        self.assertEqual(supervisor.observation_manifest_sha256,sha(root/'terminal/files.json'))
        self.assertEqual(w.strict((root/'terminal/supervisor-observation.json').read_bytes()),observation)
        self.assertTrue(observation['ownerObservationPersistenceIncluded'])
        self.assertTrue(observation['supervisorObservationPersistenceExcluded'])
        self.assertFalse(observation['wholeProcessCoverage']); self.assertFalse(observation['usableForAdmission'])
        self.assertIsNone(launcher.FILE_WORK.get())
        self.assertTrue(supervisor.watchdog.finished.is_set())
        self.assertFalse(Path(str(self.output)+'.writer.lock').exists())
        self.assertFalse((self.output.parent/'complete-family-allocation.writer.lock').exists())
        if observation['outcome']=='Complete':
            for phase in w.PHASES:
                observed=w.strict((root/'owner'/(phase+'-receipt-publication.json')).read_bytes())
                receipt=root/'owner'/(phase+'-work.json')
                self.assertEqual(observed['outcome'],'Complete')
                self.assertEqual(observed['serializedReceiptSha256'],sha(receipt))
                self.assertEqual(observed['counters']['acceptedWriteBytes'],receipt.stat().st_size)
                self.assertEqual(observed['counters']['closeCompleted'],1)
        return observation

    def verify_archive_resources(self):
        # Match the native verifier's exact directory-length requirement as well
        # as the independent auditor's receipt arithmetic and deadline checks.
        closeout = json.loads((self.output/'closeout.json').read_bytes())
        self.assertEqual(closeout['retainedBytes'], launcher.storage_bytes(self.output))
        auditor.audit_resources(self.output, self.q, False)

    def test_complete_owner_routes_all_four_bound_workers_and_retains_observation(self):
        export = os.environ.get('LL_OWNER_SUPERVISOR_EXPORT')
        base = Path(export) if export else self.base
        supervisor = self.prepare(base)
        result = self.launch(supervisor)
        self.verify_archive_resources()
        observation = self.verify(supervisor)
        self.assertEqual([p for p,_,_ in self.calls], list(w.PHASES))
        self.assertEqual(len({deadline for phase,_,deadline in self.calls if phase!='native'}), 1)
        self.assertEqual(observation['outcome'], 'Complete')
        self.assertEqual(result['accountingManifestSha256'], supervisor.manifest_sha256)
        self.assertEqual(result['status'], 'Complete')
        audit_command = self.calls[2][1]
        self.assertEqual(audit_command[4], str(self.package/'auditor.py'))
        self.assertFalse((self.output/'proposal_work_accounting.py').exists())
        self.assertFalse(any(p.name.endswith('-work.json') for p in self.output.iterdir()))
        total = launcher.storage_bytes(self.output)+launcher.storage_bytes(supervisor.root)
        self.assertEqual(supervisor.terminal_observation['observedCombinedBytes'], total)
        self.assertLess(observation['observedCombinedBytes'],total)
        self.assertGreater(total, json.loads((self.output/'closeout.json').read_bytes())['retainedBytes'])
        metadata_bytes = sum(p.stat().st_size for p in (supervisor.root/'supervisor').iterdir())
        self.assertEqual(observation['managedSupervisorStorage']['totalWrittenBytes'], metadata_bytes)
        self.assertEqual(sum(v for k,v in observation['publicationCounters'].items() if k.startswith('applicationWriteBytes.')), metadata_bytes)
        if export:
            w.seal_new(base/'supervisor-observation.json', observation)
            w.seal_new(base/'fixture.json', dict(fixtureOnly=True, routedWorkers=True, actualCombat=0,
                productionEntropyDraws=0, realProcessObservations=False, requestSha256=sha(self.package/'request.json'),
                sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p) for p in
                         (s.__file__, w.__file__, launcher.__file__, owned.__file__, __file__)}))
            w.seal_new(base/'files.json', {p.relative_to(base).as_posix():sha(p) for p in sorted(base.rglob('*')) if p.is_file()})

    def test_default_launch_has_original_inventory_and_no_accounting_flags(self):
        self.prepare()
        result = self.launch(None)
        self.verify_archive_resources()
        self.assertEqual(set(result), {'status','output','fights','retries','closeoutSha256'})
        self.assertFalse((self.base/'accounting').exists())
        self.assertFalse(any('--work-binding' in args for _,args,_ in self.calls))
        self.assertEqual(self.calls[2][1][4], str(self.output/'auditor.py'))
        self.assertEqual(sha(self.output/'files.json'), json.loads((self.output/'closeout.json').read_bytes())['filesHash'])

    def test_invalid_admission_fails_before_supervisor_side_effects(self):
        supervisor = self.prepare()
        (self.package/'context.json').write_text('changed')
        with self.assertRaisesRegex(ValueError, 'Changed admitted input'): self.launch(supervisor)
        self.assertFalse(supervisor.started); self.assertFalse(supervisor.root.exists()); self.assertEqual(self.calls, [])

    def test_compressed_guard_fails_before_accounting_initialization(self):
        supervisor = self.prepare(); self.q['evidenceStorage'] = {'version':'literal'}
        (self.package/'request.json').write_bytes(w.canonical(self.q)); self.reseal()
        with self.assertRaisesRegex(ValueError, 'recovery gate is closed'): self.launch(supervisor)
        self.assertFalse(supervisor.started); self.assertFalse(supervisor.root.exists()); self.assertEqual(self.calls, [])

    def test_changed_accounting_module_cannot_use_a_new_admission_pin(self):
        supervisor = self.prepare()
        (self.package/'proposal_owner_supervisor.py').write_text('changed')
        self.reseal()
        with self.assertRaisesRegex(ValueError, 'Accounting module differs'): self.launch(supervisor)
        self.assertFalse(supervisor.root.exists()); self.assertEqual(self.calls, [])

    def test_sidecar_root_rejects_overlap_and_normalizes_parent_segments(self):
        self.prepare()
        for path in (self.output.parent/'sidecar', self.package/'sidecar', self.base,
                     self.base/'unused'/'..'/'package'/'sidecar'):
            with self.subTest(path=path):
                with self.assertRaisesRegex(ValueError, 'fresh and disjoint'): self.launch(s.StudyWorkSupervisor(path))
        self.assertFalse((self.package/'sidecar').exists()); self.assertEqual(self.calls, [])

    def test_missing_accounting_module_in_admission_fails_closed(self):
        supervisor = self.prepare(); (self.package/'proposal_work_accounting.py').unlink(); self.reseal()
        with self.assertRaisesRegex(ValueError, 'Accounting module differs'): self.launch(supervisor)
        self.assertFalse(supervisor.root.exists())

    def test_native_process_failure_retains_original_process_receipt_and_failed_owner(self):
        supervisor = self.prepare(); self.failed_phase = 'native'
        with self.assertRaisesRegex(ValueError, 'Native comparison failed'): self.launch(supervisor)
        observed = self.verify(supervisor)
        self.assertEqual(observed['outcome'], 'Failed')
        self.assertEqual(json.loads((self.output/'native-process.json').read_bytes())['exitCode'], 1)
        self.assertEqual(w.strict((supervisor.root/'owner/native-work.json').read_bytes())['outcome'], 'Failed')
        self.assertTrue((self.output/'failure.json').exists()); self.assertFalse((self.output/'closeout.json').exists())

    def test_successful_process_without_worker_receipt_cannot_advance(self):
        supervisor = self.prepare(); self.missing_receipt_phase = 'native'
        with self.assertRaises(FileNotFoundError): self.launch(supervisor)
        self.assertEqual(self.verify(supervisor)['outcome'], 'Failed')
        self.assertEqual(len(self.calls), 1); self.assertFalse((self.output/'result.json').exists())

    def test_sidecar_bytes_count_against_native_storage_cap(self):
        supervisor = self.prepare()
        # First process invokes check after the request and owner copies exist.
        def storage_limit(command, *args, **kwargs):
            with patch.object(launcher, 'storage_bytes', wraps=launcher.storage_bytes) as scan:
                with self.assertRaisesRegex(ValueError, 'storage allowance'):
                    # The check's original limit is large; add only sidecar bytes.
                    scan.side_effect = lambda root: launcher.NATIVE_BYTES if Path(root)==supervisor.root else 0
                    kwargs['check']()
            raise RuntimeError('literal quota stop')
        with patch.object(owned, 'run', side_effect=storage_limit), self.assertRaisesRegex(RuntimeError, 'literal quota stop'):
            launcher.launch(self.package/'request.json', self.package/'runtime/BalanceHarness.dll','dotnet',sha(self.package/'files.json'),supervisor)
        self.assertFalse((self.output/'closeout.json').exists())
        self.assertTrue(supervisor.watchdog.finished.is_set())

    def test_supervisor_publication_failure_does_not_print_success(self):
        supervisor = self.prepare(); output = io.StringIO()
        with patch.object(supervisor, '_seal', side_effect=OSError('publication failed')):
            with patch.object(owned, 'run', side_effect=self.routed), contextlib.redirect_stdout(output):
                with self.assertRaisesRegex(OSError, 'publication failed'):
                    launcher.launch(self.package/'request.json',self.package/'runtime/BalanceHarness.dll','dotnet',sha(self.package/'files.json'),supervisor)
        self.assertEqual(output.getvalue(), '')
        self.assertIsNone(supervisor.manifest_sha256); self.assertTrue(supervisor.watchdog.finished.is_set())

    def test_sidecar_growth_counts_against_shared_audit_storage_reserve(self):
        supervisor = self.prepare(); original_scan = launcher.storage_bytes
        def routed(command, *args, **kwargs):
            if command[2] == 'tower-proposal-study-audit':
                def inflated(root):
                    return original_scan(root) + (launcher.AUDIT_BYTES if Path(root)==supervisor.root else 0)
                with patch.object(launcher, 'storage_bytes', side_effect=inflated):
                    kwargs['check']()
            return self.routed(command, *args, **kwargs)
        with patch.object(owned, 'run', side_effect=routed):
            with self.assertRaisesRegex(ValueError, 'Audit/publication storage reserve exhausted'):
                launcher.launch(self.package/'request.json',self.package/'runtime/BalanceHarness.dll','dotnet',sha(self.package/'files.json'),supervisor)
        self.assertEqual(len(self.calls), 1)
        self.assertEqual(self.verify(supervisor)['outcome'], 'Failed')
        self.assertFalse((self.output/'closeout.json').exists())

    def test_watchdog_stays_active_until_success_output_returns(self):
        supervisor = self.prepare(); seen = []
        def output(*args, **kwargs):
            self.assertFalse(supervisor.watchdog.finished.is_set())
            self.assertIsNotNone(supervisor.manifest_sha256)
            seen.append(args)
        with patch.object(owned, 'run', side_effect=self.routed), patch('builtins.print', side_effect=output):
            launcher.launch(self.package/'request.json',self.package/'runtime/BalanceHarness.dll','dotnet',sha(self.package/'files.json'),supervisor)
        self.assertEqual(len(seen), 1)
        self.assertTrue(supervisor.watchdog.finished.is_set())

    def test_original_worker_error_survives_supervisor_publication_failure(self):
        supervisor = self.prepare(); self.failed_phase = 'native'
        with patch.object(supervisor, '_seal', side_effect=OSError('publication failed')):
            with self.assertRaisesRegex(ValueError, 'Native comparison failed') as caught: self.launch(supervisor)
        self.assertIn('publication failed', str(caught.exception.__notes__))
        self.assertIsNone(launcher.FILE_WORK.get()); self.assertTrue(supervisor.watchdog.finished.is_set())

    def test_final_publication_uses_existing_audit_deadline_and_byte_check(self):
        supervisor = self.prepare(); original = supervisor._seal
        def exhaust(name, value):
            # Delay only the supervisor boundary through a literal clock value;
            # no real cost/timing experiment is performed.
            with patch.object(launcher.time, 'monotonic', return_value=supervisor.started_at+launcher.SECONDS+1):
                return original(name, value)
        with patch.object(supervisor, '_seal', side_effect=exhaust):
            with self.assertRaisesRegex(ValueError, 'elapsed allowance'): self.launch(supervisor)
        self.assertIsNone(supervisor.manifest_sha256)
        self.assertTrue(supervisor.watchdog.finished.is_set())


if __name__ == '__main__': unittest.main()
