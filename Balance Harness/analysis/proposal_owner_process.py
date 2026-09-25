"""Observe a trusted diagnostic Python owner from an enclosing Windows job.

API only: this does not admit, prepare or select a study. The supplied driver must
use the existing launcher/admission checks. Parent-job kernel totals include nested
worker jobs; they overlap worker/application counters and must never be added to them.
https://learn.microsoft.com/en-us/windows/win32/procthread/nested-jobs
"""
import hashlib
import math
import os
from pathlib import Path
import stat
import sys
import time

import bounded_windows_process as owned
import proposal_work_accounting as work


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


class OwnerProcessMonitor:
    """One create-new observation package, with no scientific success claim.

    The work deadline reserves cleanup and publication inside the caller's envelope.
    The optional check must cover any additional storage/resource limits. Observations
    stop at job drain/handle cleanup and the closed console hash. The monitor's later
    persistence, verification, console and exit are explicitly outside that boundary.
    """
    def __init__(self, root, request_sha256, *, monitor_accounting=False, runtime_environment=None, monitor_io_budget=None, job_limits=None):
        work.require(type(monitor_accounting) is bool, 'Invalid monitor accounting option')
        self.root = Path(os.path.abspath(root))
        self.request_sha256 = work.pin(request_sha256)
        self.used = False
        self.result = self.process_observation = self.observation = None
        self.manifest_sha256 = self.publication_error = None
        self.monitor_accounting = monitor_accounting
        self.monitor_terminal_observation = None
        self.terminal_publication_observation = None
        self._terminal_root = None
        self._terminal_identity = None
        self._terminal_ready = self._terminal_attempted = False
        self._monitor_started = self._monitor_active = False
        self._monitor_counters = {}
        self._monitor_phase = self._monitor_failure_phase = self._monitor_binding = None
        self._runtime_module = self._runtime_declaration = self.runtime_environment = None
        self._job_limits = None
        if job_limits is not None:
            self._job_limits = owned.job_limit_declaration(job_limits)
        self._io_module = self.monitor_io = None
        if monitor_io_budget is not None:
            work.require(monitor_accounting, 'Monitor I/O budget requires terminal accounting')
            import proposal_monitor_io as monitor_io
            self._io_module = monitor_io
            self.monitor_io = monitor_io.MonitorIO(monitor_io_budget)
        if runtime_environment is not None:
            import proposal_runtime_environment as runtime
            self._runtime_module=runtime
            self._runtime_declaration=runtime.declaration(runtime_environment)

    def _phase(self, name):
        if self._monitor_active:
            self._monitor_phase = name
            self._monitor_counters.setdefault(name, work.OwnerFileCounters())

    def _work(self):
        return self._monitor_counters[self._monitor_phase] if self._monitor_active else None

    def _call(self, name, action):
        counters = self._work()
        if counters is None: return action()
        try:
            counters.add(name+'Attempted')
            try: result = action()
            except BaseException as error:
                try: counters.add(name+'Failed')
                except BaseException as accounting_error:
                    owned._error_note(error, 'Monitor operation failure accounting failed: ', accounting_error)
                raise
            counters.add(name+'Completed')
            return result
        except BaseException:
            if self._monitor_failure_phase is None: self._monitor_failure_phase = self._monitor_phase
            raise

    def _sha(self, path):
        counters = self._work()
        if self.monitor_io is not None:
            return self._call('hashFile', lambda:self.monitor_io.sha(path, counters))
        if counters is None: return sha(path)
        try: return self._call('hashFile', lambda:counters.sha(path))
        except BaseException as error:
            try: counters.add('failedHashReadBytesUnknown')
            except BaseException as accounting_error:
                owned._error_note(error, 'Monitor hash failure accounting failed: ', accounting_error)
            raise

    def _unlinked(self, path):
        # One path validation may query multiple ancestors. This is a logical
        # validation count, not a claim about individual filesystem calls.
        return self._call('pathValidation', lambda:work.unlinked(path))

    def _check(self):
        self._call('deadlineCheck', lambda:work.require(time.monotonic() < self.deadline, 'Owner monitor elapsed allowance exhausted'))
        if self.check is not None:
            self._call('callerResourceCheck', self.check)

    def _seal(self, name, value, *, root=None):
        if self.monitor_io is not None:
            with self.monitor_io.scope():
                return self._seal_impl(name, value, root=root)
        return self._seal_impl(name, value, root=root)

    def _seal_impl(self, name, value, *, root=None):
        root = self.root if root is None else root
        self._check()
        raw = self._call('serializeJson', lambda:work.canonical(value) if self.monitor_io is None
                         else self.monitor_io.publication(name, value))
        counters = self._work()
        original = stream = None
        try:
            if self.monitor_io is not None: self.monitor_io.opening(name)
            def opening():
                nonlocal stream
                stream = (root/name).open('xb', buffering=0)
                return stream
            self._call('publicationOpen', opening)
            offset = 0
            while offset < len(raw):
                def write():
                    if self.monitor_io is not None: self.monitor_io.write_calls += 1
                    count = stream.write(raw[offset:])
                    work.require(type(count) is int and 0 < count <= len(raw)-offset, 'Incomplete owner observation write')
                    return count
                try: count = self._call('publicationWrite', write)
                except BaseException as error:
                    if self.monitor_io is not None: self.monitor_io.write_progress_unknown = True
                    if counters is not None:
                        try: counters.add('failedPublicationWriteBytesUnknown')
                        except BaseException as accounting_error:
                            owned._error_note(error, 'Owner observation write failure accounting failed: ', accounting_error)
                    raise
                if self.monitor_io is not None: self.monitor_io.written(name, count)
                if counters is not None: counters.add('publicationAcceptedWriteBytes.'+work.role(name), count)
                offset += count
            self._call('publicationFlush', stream.flush)
            self._call('publicationSync', lambda:os.fsync(stream.fileno()))
        except BaseException as error:
            original = error
            raise
        finally:
            if stream is not None:
                # Keep the first failure and attempt both independent finalizers.
                # A close/accounting error must not skip the file-length sample.
                failure = original
                try:
                    self._close_publication(stream)
                except BaseException as error:
                    if failure is None: failure = error
                    else: owned._error_note(failure, 'Owner observation close failed: ', error)
                if counters is not None:
                    try: counters.observe_file(root/name, missing_ok=False)
                    except BaseException as error:
                        if failure is None: failure = error
                        else: owned._error_note(failure, 'Owner observation file accounting failed: ', error)
                if failure is not None:
                    if self._monitor_failure_phase is None: self._monitor_failure_phase = self._monitor_phase
                    if original is None: raise failure
        self._check()
        return hashlib.sha256(raw).hexdigest()

    def _close_publication(self, stream):
        # Accounting must not prevent the one mandatory close attempt.
        counters = self._work()
        if counters is None: return stream.close()
        failure = None
        try: counters.add('publicationCloseAttempted')
        except BaseException as error: failure = error
        try:
            stream.close()
        except BaseException as error:
            outcome = 'Failed'
            if failure is None: failure = error
            else: owned._error_note(failure, 'Owner observation close failed: ', error)
        else: outcome = 'Completed'
        try: counters.add('publicationClose'+outcome)
        except BaseException as error:
            if failure is None: failure = error
            else: owned._error_note(failure, 'Owner observation close accounting failed: ', error)
        if failure is not None:
            if self._monitor_failure_phase is None: self._monitor_failure_phase = self._monitor_phase
            raise failure

    def _inventory(self, expected, *, root=None):
        root = self.root if root is None else root
        self._unlinked(root)
        names = self._call('directoryInventory', lambda:{p.name for p in root.iterdir()} if self.monitor_io is None
                           else self.monitor_io.inventory(root, expected))
        work.require(names == set(expected), 'Untracked owner monitor member')
        for name in expected:
            path = root/name
            self._unlinked(path)
            work.require(self._call('metadataIsFile', path.is_file), 'Invalid owner monitor member')

    def _snapshot(self, binding, failure):
        raw = self.process_observation
        result = self.result
        drained = raw is not None and raw['jobDrained'] is True
        exited = (drained and result is not None and result['activeProcesses'] == 0
                  and raw['exitCode'] == result['exitCode'] and raw['timedOut'] == result['timedOut'])
        io = None if raw is None else raw['jobIo']
        known_io = drained and io is not None and io['coverage'] == 'KernelJobLifetimeTotals'
        # A memory query on an empty/unassigned job is not owner lifetime coverage.
        assigned = (result is not None and result['totalProcesses'] > 0) or known_io
        memory = drained and assigned and raw['memoryCoverage'] == 'KernelJobHighWaterPlusOwnerLifetimeHighWater'
        console = self.root/'console.log'
        console_info = None
        if self._call('metadataExists', console.exists):
            self._unlinked(console)
            console_info = dict(path='console.log', sha256=self._sha(console), retainedBytes=self._call('metadataStat', console.stat).st_size,
                                coverage='ClosedRedirectedStdoutAndStderr', durableBytesClaimed=False)
            if self.monitor_io is not None:
                work.require(console_info['retainedBytes'] <= self.monitor_io.limits['maxConsoleBytes'],
                             'Owner monitor console byte allowance exhausted')
        if failure is not None:
            outcome = 'MonitorFailed'
        elif result is None:
            outcome = 'Unknown'
        elif result['timedOut']:
            outcome = 'TimedOut'
        else:
            outcome = 'ExitedZero' if result['exitCode'] == 0 else 'ExitedNonzero'
        complete = exited and known_io and memory and console_info is not None and failure is None
        self.observation = dict(version='tower-proposal-owner-process-v1', **binding,
            processOutcome=outcome, observationOutcome='Complete' if complete else 'Incomplete',
            processResult=result, processObservation=raw,
            ownerConsoleAndExitObserved=bool(exited and console_info is not None),
            ownerJobLifetimeMemoryObserved=bool(memory),
            ownerJobPeakCommitBytes=raw['peakJobCommitBytes'] if memory else None,
            ownerJobIo=io, console=console_info,
            monitorLifetimePeakCommitBytes=None if raw is None else raw['ownerLifetimePeakCommitBytes'],
            monitorMemoryBoundary='BeforeOwnedHandleCleanup' if raw is not None else 'Unavailable',
            memoryInterpretation='OwnerAndDescendantJobHighWater;SeparateMonitorLifetimeHighWaterAtQuery',
            enclosingSeconds=time.monotonic()-self.started_at,
            boundary='AfterOwnedHandleCleanupAndConsoleHash' if raw is not None else 'FailedProcessStartAttempt',
            monitorError=None if failure is None else owned._error_text(failure, representation=True),
            outerMonitorPublicationExcluded=True, scientificOutcomeVerified=False,
            missingCoverage=['outerMonitorPreparationAndApplicationIO',
                             'outerMonitorPublicationVerificationConsoleAndExit', 'wholeProcessScratch',
                             'completeApplicationOperationAttribution', 'physicalOrDurableDiskBytes'],
            wholeProcessCoverage=False, usableForAdmission=False,
            **(dict(runtimeEnvironment=self.runtime_environment.snapshot()) if self.runtime_environment is not None else {}))

    def _publish(self):
        self._check()
        console = self.observation['console']
        files = {} if console is None else {'console.log':console['sha256']}
        self._inventory(files)
        files['owner-process.json'] = self._seal('owner-process.json', self.observation)
        manifest = self._seal('files.json', files)
        self._phase('verification')
        self._inventory([*files, 'files.json'])
        for name, pin in dict(files, **{'files.json':manifest}).items():
            work.require(self._sha(self.root/name) == pin, 'Changed owner monitor publication')
        self._check()
        self.manifest_sha256 = manifest

    def run(self, script, arguments, cwd, deadline, *, cleanup_seconds=1.0,
            publication_seconds=1.0, check=None, protected_roots=()):
        """Optionally observe this call's own work, retaining the v1 package.

        The terminal observation stays in memory. Its construction/persistence,
        caller console, process exit, constructor and earlier imports remain out
        of scope. A recorded failure cannot be overwritten by another attempt.
        """
        if not self.monitor_accounting:
            return self._run(script, arguments, cwd, deadline, cleanup_seconds=cleanup_seconds,
                publication_seconds=publication_seconds, check=check, protected_roots=protected_roots)
        work.require(not self._monitor_started, 'Monitor accounting cannot run twice')
        self._monitor_started = self._monitor_active = True
        self._monitor_started_at = time.monotonic()
        self._phase('preparation')
        failure = None
        try:
            return self._run(script, arguments, cwd, deadline, cleanup_seconds=cleanup_seconds,
                publication_seconds=publication_seconds, check=check, protected_roots=protected_roots)
        except BaseException as error:
            failure = error
            if self._monitor_failure_phase is None: self._monitor_failure_phase = self._monitor_phase
            raise
        finally:
            try: self._monitor_snapshot(failure)
            except BaseException as error:
                self.monitor_terminal_observation = None
                if failure is None: raise
                owned._error_note(failure, 'Monitor terminal observation failed: ', error)
            finally: self._monitor_active = False
            if self._terminal_ready and self.monitor_terminal_observation is not None:
                try: self._persist_terminal()
                except BaseException as error:
                    if failure is None: raise
                    owned._error_note(failure, 'Monitor terminal persistence failed: ', error)

    def _persist_terminal(self):
        """One bounded terminal file; its publication result stays in memory.

        The saved record deliberately retains the earlier snapshot boundary.
        No recursive observer or claim about subsequent caller/exit work is added.
        """
        work.require(not self._terminal_attempted, 'Monitor terminal persistence cannot run twice')
        self._terminal_attempted = True
        pin = failure = None
        verified = False
        try:
            work.require(self._terminal_directory() == self._terminal_identity, 'Replaced monitor terminal directory')
            self._inventory([], root=self._terminal_root)
            pin = self._seal('monitor-call.json', self.monitor_terminal_observation, root=self._terminal_root)
            work.require(self._terminal_directory() == self._terminal_identity, 'Replaced monitor terminal directory')
            self._inventory(['monitor-call.json'], root=self._terminal_root)
            work.require(self._sha(self._terminal_root/'monitor-call.json') == pin, 'Changed monitor terminal publication')
            work.require(self._terminal_directory() == self._terminal_identity, 'Replaced monitor terminal directory')
            self._check()
            verified = True
        except BaseException as error:
            failure = error
            raise
        finally:
            try:
                self.terminal_publication_observation = dict(version='tower-proposal-monitor-terminal-publication-v1',
                    path=str(self._terminal_root/'monitor-call.json'), outcome='Verified' if verified else 'Failed',
                    sha256=pin if verified else None, error=None if failure is None else owned._error_text(failure, representation=True),
                    persistedBoundary='RunEntryThroughPublicationVerificationBeforeTerminalSnapshotConstruction',
                    publicationBoundary=('TerminalFileClosedAndReadbackVerifiedBeforePublicationResultConstruction' if verified
                                         else 'FailedTerminalPublicationAttemptBeforeResultConstruction'),
                    monitorIO=self.monitor_io.snapshot(), publicationResultPersistenceExcluded=True,
                    callerConsoleAndExitExcluded=True, wallClockBounded=False, memoryBounded=False,
                    externalMutationBounded=False, wholeProcessCoverage=False, usableForAdmission=False)
            except BaseException as error:
                self.terminal_publication_observation = None
                if failure is None: raise
                owned._error_note(failure, 'Terminal publication result construction failed: ', error)

    def _terminal_directory(self):
        self._unlinked(self._terminal_root)
        info = self._terminal_root.lstat()
        work.require(stat.S_ISDIR(info.st_mode) and info.st_ino != 0, 'Invalid monitor terminal directory')
        return info.st_dev, info.st_ino

    def _monitor_snapshot(self, failure):
        # Read the clock before constructing the final observation. This record
        # cannot include its own serialization or a future caller's persistence.
        elapsed = time.monotonic()-self._monitor_started_at
        self.monitor_terminal_observation = dict(version='tower-proposal-monitor-call-v1',
            requestSha256=self.request_sha256, sourceBindings=self._monitor_binding,
            ownerObservationManifestSha256=self.manifest_sha256,
            outcome='Complete' if failure is None else 'Failed',
            outcomeMeaning=('MonitorRunBodyReturnedOrRaisedBeforeTerminalPersistence;NotScientificSuccess'
                            if self.monitor_io is not None and self.monitor_io.has_terminal
                            else 'MonitorCallReturnedOrRaised;NotScientificSuccess'),
            ownerObservationOutcome=None if self.observation is None else self.observation['observationOutcome'],
            enclosingSeconds=elapsed, boundary='RunEntryThroughPublicationVerificationBeforeTerminalSnapshotConstruction',
            phaseCounters={phase:dict(sorted(counters.values.items())) for phase,counters in self._monitor_counters.items()},
            failedPhase=None if failure is None else self._monitor_failure_phase,
            monitorError=None if failure is None else owned._error_text(failure, representation=True),
            publicationError=None if self.publication_error is None else owned._error_text(self.publication_error, representation=True),
            publicationVerified=self.manifest_sha256 is not None,
            storageInterpretation='SelectedClosedFileLengthSamples;NoWholeProcessPeak',
            ioInterpretation='AcceptedApplicationBytes;SeparateFromOverlappingOwnerJobTotals',
            terminalObservationPersistenceExcluded=True,
            missingCoverage=['constructorAndEarlierImports', 'terminalSnapshotConstructionSerializationAndPersistence',
                             'callerConsoleAndMonitorProcessExit', 'outerMonitorMemoryLifetime',
                             'externalAndTransientScratch', 'completeApplicationOperationAttribution', 'physicalOrDurableDiskBytes'],
            wholeProcessCoverage=False, usableForAdmission=False,
            **(dict(monitorIO=self.monitor_io.snapshot()) if self.monitor_io is not None else {}))

    def _run(self, script, arguments, cwd, deadline, *, cleanup_seconds=1.0,
             publication_seconds=1.0, check=None, protected_roots=()):
        """Return the original process result; retain observations even on failure.

        A zero exit is only a process result. Missing kernel observations stay
        incomplete. A start/check/publication error raises; secondary publication
        errors attach to the original error. Never retry in the same package.
        """
        work.require(not self.used, 'Owner process monitor cannot run twice')
        protected_roots = tuple(protected_roots)
        script = Path(os.path.abspath(script))
        cwd = Path(os.path.abspath(cwd))
        self._unlinked(script)
        self._unlinked(cwd)
        self._unlinked(self.root)
        work.require(self._call('metadataIsFile', script.is_file), 'A trusted owner script is required')
        work.require(self._call('metadataIsDirectory', cwd.is_dir), 'An existing owner working directory is required')
        work.require(not self._call('metadataExists', self.root.exists), 'Owner monitor destination already exists')
        for protected in protected_roots:
            protected = Path(os.path.abspath(protected))
            work.require(not self.root.is_relative_to(protected) and not protected.is_relative_to(self.root),
                         'Owner monitor must be disjoint from protected evidence')
        work.require(type(deadline) in (int, float) and math.isfinite(deadline)
                     and type(cleanup_seconds) in (int, float) and 0 < cleanup_seconds <= 2
                     and type(publication_seconds) in (int, float) and math.isfinite(publication_seconds)
                     and publication_seconds > 0, 'Invalid owner monitor allowance')
        work_deadline = deadline-cleanup_seconds-publication_seconds
        work.require(work_deadline > time.monotonic(), 'Insufficient owner monitor allowance')
        if self.monitor_io is not None and self.monitor_io.has_terminal:
            terminal = Path(self.monitor_io.limits['terminalRoot'])
            self._unlinked(terminal)
            terminal = terminal.resolve()
            work.require(not terminal.exists() and terminal.parent.is_dir(), 'Fresh terminal directory with existing parent required')
            for protected in (*protected_roots, self.root, script):
                protected = Path(os.path.abspath(protected))
                self._unlinked(protected)
                protected = protected.resolve()
                work.require(not terminal.is_relative_to(protected) and not protected.is_relative_to(terminal),
                             'Terminal directory must be disjoint from protected evidence')
            self._terminal_root = terminal
        command = [sys.executable, '-B', '-X', 'utf8', str(script), *map(os.fspath, arguments)]
        binding = dict(requestSha256=self.request_sha256, ownerDriverSha256=self._sha(script),
            monitorProducerSha256=self._sha(__file__), processHelperSha256=self._sha(owned.__file__),
            accountingModuleSha256=self._sha(work.__file__), interpreterSha256=self._sha(sys.executable),
            command=command, commandSha256=hashlib.sha256(work.canonical(command)).hexdigest(),
            workingDirectory=str(cwd), requestBinding='CallerDeclared;DriverAdmissionRequired',
            cleanupAllowanceSeconds=cleanup_seconds, publicationReserveSeconds=publication_seconds)
        if self.monitor_io is not None:
            binding.update(monitorIOBudget=dict(self.monitor_io.limits), monitorIOModuleSha256=self._sha(self._io_module.__file__))
        if self._job_limits is not None: binding['jobLimitPlan'] = dict(self._job_limits)
        environment = expected_environment = None
        if self._runtime_module is not None:
            self.runtime_environment=self._runtime_module.RuntimeEnvironment(self._runtime_declaration,
                protected_roots=(*protected_roots,self.root,script,*([self._terminal_root] if self._terminal_root is not None else [])))
            plan=self.runtime_environment.plan()
            environment=dict(plan['environment'])
            _,expected_environment=owned.environment_block(environment)
            binding.update(runtimeEnvironmentPlan=plan,processEnvironment=expected_environment,
                           runtimeEnvironmentModuleSha256=self._sha(self._runtime_module.__file__))
        if self._monitor_active: self._monitor_binding = binding
        self.deadline, self.check = deadline, check
        self._check()
        self._call('directoryCreate', self.root.mkdir)
        self.used = True
        self.started_at = time.monotonic()
        failure = None

        def observe(value):
            if self._job_limits is not None:
                work.require('jobLimits' in value and work.job_limit_observation(value['jobLimits']) == self._job_limits,
                             'Changed or missing owner job limits')
                work.require(value['jobLimits']['verified'] or value['exitCode'] is None
                             and value.get('jobIo',{}).get('coverage') != 'KernelJobLifetimeTotals',
                             'Executed owner has unverified job limits')
            if self.monitor_io is not None:
                capture = value.get('logCapture')
                work.require(type(capture) is dict and capture.get('version') == 'tower-owned-log-capture-v1'
                             and type(capture.get('maxBytes')) is int
                             and capture['maxBytes'] == self.monitor_io.limits['maxConsoleBytes'],
                             'Changed or missing monitor console budget observation')
            if expected_environment is not None:
                work.require(value.get('environment')==expected_environment, 'Changed or missing process environment observation')
            self.process_observation = work.strict(work.canonical(value))

        def process_check():
            self._check()
            if self.runtime_environment is not None:self.runtime_environment.sample()

        try:
            if self._terminal_root is not None:
                self._call('directoryCreate', self._terminal_root.mkdir)
                self._terminal_identity = self._terminal_directory()
                self._terminal_ready = True
            self._phase('ownerProcess')
            options = dict(cleanup_seconds=cleanup_seconds, check=process_check, observe=observe)
            if self._job_limits is not None: options['job_limits'] = dict(self._job_limits)
            if self.monitor_io is not None: options['log_byte_limit'] = self.monitor_io.limits['maxConsoleBytes']
            if self.runtime_environment is not None:
                self.runtime_environment.prepare()
                options['environment']=environment
            if self._monitor_active: options['file_work'] = self._work()
            self.result = self._call('ownedProcessCall', lambda:owned.run(command, cwd, self.root/'console.log', work_deadline, **options))
        except BaseException as error:
            failure = error
            raise
        finally:
            runtime_error = None
            if self.runtime_environment is not None and not self.runtime_environment.failed \
                    and self.process_observation is not None and self.process_observation['jobDrained']:
                try:self._call('runtimeFinalInspection', self.runtime_environment.sample)
                except BaseException as error:runtime_error=error
            try:
                self._phase('observation')
                self._snapshot(binding, failure if failure is not None else runtime_error)
                self._phase('publication')
                self._publish()
            except BaseException as error:
                self.manifest_sha256 = None
                self.publication_error = error
                if failure is None and runtime_error is None:
                    raise
                owned._error_note(failure if failure is not None else runtime_error,
                                  'Owner process observation publication failed: ', error)
            if runtime_error is not None:
                if failure is None:raise runtime_error
                owned._error_note(failure, 'Runtime inspection failed: ', runtime_error)
        return self.result
