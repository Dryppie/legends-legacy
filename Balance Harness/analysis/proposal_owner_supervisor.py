"""Opt-in accounting adapter for the existing admitted study owner.

No admission, preparation or launch occurs here. The launcher validates admission
and the request before starting this adapter. Scientific defaults remain intact.
"""
import hashlib
import os
from pathlib import Path
import time

import proposal_work_accounting as work


class StudyWorkSupervisor:
    def __init__(self, root, *, worker_observation_persistence=False, storage_contracts=None, worker_sidecar_limits=None,
                 worker_log_limits=None, native_pending_budgets=None, native_pending_families=None, native_lease_budgets=None,
                 owner_lease_budget=None):
        work.require(type(worker_observation_persistence) is bool, 'Invalid worker persistence option')
        self.worker_observation_persistence = worker_observation_persistence
        self.owner_leases = self._owner_lease_module = self._owner_lease_budget = None
        if owner_lease_budget is not None:
            work.require(storage_contracts is not None, 'Owner leases require declared supervisor storage')
            import proposal_owner_leases as leases
            self._owner_lease_module = leases
            self._owner_lease_budget = leases.declaration(owner_lease_budget)
        self._worker_log_limits = None
        self._worker_log_paths = set()
        if worker_log_limits is not None:
            work.require(type(worker_log_limits) is dict and set(worker_log_limits)==set(work.PHASES)
                         and all(type(v) is int and 0 <= v < 2**63 for v in worker_log_limits.values()),
                         'Declare finite log byte limits for all four workers')
            self._worker_log_limits = dict(worker_log_limits)
        self._worker_sidecar_limits = None
        if worker_sidecar_limits is not None:
            work.require(worker_observation_persistence and type(worker_sidecar_limits) is dict
                         and set(worker_sidecar_limits)==set(work.PHASES), 'Bound all four persistent worker sidecars')
            self._worker_sidecar_limits = {phase:work.sidecar_limits(value) for phase,value in worker_sidecar_limits.items()}
        self._native_pending_budgets_raw = self._native_pending_plan_raw = None
        self._native_pending_phases = set()
        self._native_pending_families = native_pending_families is not None
        self._native_lease_budgets_raw = None
        if native_lease_budgets is not None:
            work.require(self._native_pending_families, 'Native leases require bounded pending family plans')
            work.require(type(native_lease_budgets) is dict and set(native_lease_budgets)==set(work.NATIVE_PHASES),
                         'Declare all three native lease phases')
            self._native_lease_budgets_raw=work.canonical({p:work.native_lease_limits(p,b) for p,b in native_lease_budgets.items()})
        work.require(not self._native_pending_families or native_pending_budgets is None, 'Pending family and explicit plans cannot be mixed')
        if self._native_pending_families: native_pending_budgets = native_pending_families
        if native_pending_budgets is not None:
            work.require(storage_contracts is not None and self._worker_sidecar_limits is not None,
                         'Native pending phase plans require declared storage and bounded persistent sidecars')
            work.require(type(native_pending_budgets) is dict and set(native_pending_budgets)==set(work.NATIVE_PHASES),
                         'Declare all three native pending phases')
            copied={phase:work.pending_family_limits(phase,v) if self._native_pending_families else work.native_pending_budget(v,allow_empty=phase!='native')
                    for phase,v in native_pending_budgets.items()}
            if not self._native_pending_families:
                work.require(all(not copied[p]['files'] for p in ('nativeAudit','publication')), 'Audit/publication phases prohibit pending writers')
            self._native_pending_budgets_raw = work.canonical(copied)
        self.root = Path(os.path.abspath(root))
        self.started = self.closed = False
        self.owner = self.phase_scope = self.file_scope = None
        self.final_observation = self.manifest_sha256 = None
        self.terminal_observation = self.observation_manifest_sha256 = None
        self.phase_name = None
        self.bound_check = None
        self.watchdog = None
        self.counters = work.OwnerFileCounters()
        self.publication_counters = work.OwnerFileCounters()
        self.observation_counters = work.OwnerFileCounters()
        self._storage_contracts = self._storage_module = None
        self.storage_binding_sha256 = None
        if storage_contracts is not None:
            import proposal_diagnostic_storage as diagnostic
            work.require(type(storage_contracts) is dict and set(storage_contracts) == {'owner','supervisor','terminal'},
                         'Declare all three supervisor storage scopes')
            self._storage_contracts = {name:diagnostic.declaration(value) for name,value in storage_contracts.items()}
            required = dict(owner={'binding.json','owner-work.json','files.json'},
                supervisor={'storage-binding.json','final-owner-observation.json','files.json'},
                terminal={'supervisor-observation.json','files.json'})
            for index,phase in enumerate(work.PHASES,1):
                required['owner'].update({phase+'-binding.json',phase+'-work.json',phase+'-receipt-publication.json',
                                          f'{phase}-process-{index:02d}.json'})
                if worker_observation_persistence: required['owner'].add(phase+'-publication-persistence.json')
            for scope,names in required.items():
                files = self._storage_contracts[scope]['files']
                work.require(all(name in files and files[name]['kind']=='retained' for name in names),
                             'Missing retained supervisor storage member: '+scope)
            self._storage_module = diagnostic

    def cancel_watchdog(self):
        if self.watchdog is not None: self.watchdog.cancel()

    def validate(self, request_path, output, harness):
        """Called only after the launcher's existing admission/request checks."""
        work.require(not self.started and not self.closed, 'Supervisor cannot be reused')
        package = Path(request_path).parent
        files = work.strict((package/'files.json').read_bytes())
        # Additional code must belong to the same authenticated package. The
        # independent auditor will run from there so its sibling module resolves
        # without inserting extra files into the native study inventory.
        modules = [Path(__file__), Path(work.__file__)]
        if self._storage_module is not None: modules.append(Path(self._storage_module.__file__))
        if self._owner_lease_module is not None: modules.append(Path(self._owner_lease_module.__file__))
        for path in modules:
            captured = package/path.name
            expected = files.get(path.name)
            work.require(expected is not None and hashlib.sha256(path.read_bytes()).hexdigest() == expected
                         and hashlib.sha256(captured.read_bytes()).hexdigest() == expected,
                         'Accounting module differs from admission: '+path.name)
        q = work.strict(Path(request_path).read_bytes())
        auditor = Path(q['auditor']['path']).resolve()
        work.require(auditor.parent == package and harness == package/'runtime/BalanceHarness.dll',
                     'Accounting requires the captured sibling auditor and retained runtime')
        work.unlinked(self.root)
        if self._storage_contracts is not None: self.root = self.root.resolve()
        work.require(not self.root.exists() and not self.root.is_relative_to(output.parent)
                     and not self.root.is_relative_to(package)
                     and not output.parent.is_relative_to(self.root) and not package.is_relative_to(self.root),
                     'Accounting output must be fresh and disjoint from registry/admission')
        if self._native_pending_budgets_raw is not None:
            work.require(Path(q['outputRoot'])==output, 'Changed pending plan output root')
            budgets=work.strict(self._native_pending_budgets_raw)
            plan={p:work.proposal_pending_families_budget(output,p,b) for p,b in budgets.items()} if self._native_pending_families else work.proposal_pending_plan(output,budgets)
            self._native_pending_plan_raw = work.canonical(plan)
        if self._native_lease_budgets_raw is not None:
            for phase,budget in work.strict(self._native_lease_budgets_raw).items():
                work.proposal_native_lease_budget(output,phase,budget,protected_paths=(self.root,package))
        if self._owner_lease_module is not None:
            self.owner_leases = self._owner_lease_module.OwnerLeases(output,self._owner_lease_budget,
                                                                   protected_paths=(self.root,package))
        self.request_path, self.output, self.harness, self.auditor = Path(request_path), output, harness, auditor

    def start(self, started, file_scope, run_process):
        work.require(hasattr(self, 'request_path') and not self.started, 'Unvalidated supervisor')
        work.require(self._native_pending_budgets_raw is None or self._native_pending_plan_raw is not None, 'Missing validated native pending plan')
        self.started_at, self.run_process = started, run_process
        self.root.mkdir()
        (self.root/'worker-receipts').mkdir()
        owner_options = {}
        if self._storage_contracts is None:
            self.publication = work.ManagedStorage(self.root/'supervisor', self.publication_counters)
            self.observation_publication = work.ManagedStorage(self.root/'terminal', self.observation_counters)
        else:
            protected = (self.request_path.parent, self.output.parent)
            self.publication = self._storage_module.DiagnosticStorage(self.root/'supervisor',
                self._storage_contracts['supervisor'], self.publication_counters, protected_roots=protected)
            self.observation_publication = self._storage_module.DiagnosticStorage(self.root/'terminal',
                self._storage_contracts['terminal'], self.observation_counters, protected_roots=protected)
            owner_options = dict(storage_contract=self._storage_contracts['owner'], protected_roots=protected)
        self.owner = work.RetainedOwner(self.root/'owner', hashlib.sha256(self.request_path.read_bytes()).hexdigest(),
            hashlib.sha256((self.request_path.parent/'run-proposal-affinity-study.py').read_bytes()).hexdigest(),
            hashlib.sha256(Path(work.__file__).read_bytes()).hexdigest(), work=self.counters, **owner_options)
        self.owner.__enter__()
        self.started = True
        self.file_scope = file_scope(self.counters)
        self.file_scope.__enter__()
        self.begin('native')

    def begin(self, phase):
        work.require(self.started and not self.closed, 'Supervisor is not active')
        if self.phase_scope is not None: self.phase_scope.__exit__(None, None, None)
        producer = self.auditor if phase == 'independentAudit' else self.harness
        self.phase_scope = self.owner.phase(phase, self.counters.sha(producer))
        self.phase_scope.__enter__()
        self.phase_name = phase

    def set_check(self, check):
        self.bound_check = check
        if self._storage_contracts is not None and self.storage_binding_sha256 is None:
            self.storage_binding_sha256 = self._seal('storage-binding.json', dict(
                version='tower-proposal-supervisor-storage-v1', requestSha256=self.owner.ledger.request,
                contracts=self._storage_contracts,
                contractsSha256=hashlib.sha256(work.canonical(self._storage_contracts)).hexdigest(),
                sourceBindings={p.name:self.publication_counters.sha(p) for p in
                    (Path(__file__),Path(work.__file__),Path(self._storage_module.__file__),
                     *([Path(self._owner_lease_module.__file__)] if self._owner_lease_module is not None else []))},
                scopeRoots={name:name for name in self._storage_contracts},
                coverage='ThreeDeclaredCooperativeDirectories', filesystemConfinement=False,
                externalWritesBounded=False, wholeProcessCoverage=False, usableForAdmission=False,
                **(dict(workerSidecarByteLimits=self._worker_sidecar_limits) if self._worker_sidecar_limits is not None else {}),
                **(dict(workerLogByteLimits=self._worker_log_limits) if self._worker_log_limits is not None else {}),
                **(dict(nativePendingPlan=self._pending_plan()) if self._native_pending_plan_raw is not None else {}),
                **(dict(ownerLeasePlan=self.owner_leases.plan()) if self.owner_leases is not None else {})))

    def _verify_owner_lease_binding(self):
        if self.owner_leases is None: return
        path=self.publication.path('storage-binding.json')
        work.require(self.storage_binding_sha256 is not None and self.counters.sha(path)==self.storage_binding_sha256,
                     'Missing or changed owner lease binding')
        bound=work.strict(self.counters.read_bytes(path))
        work.require(bound.get('ownerLeasePlan')==self.owner_leases.plan(), 'Changed owner lease declaration')

    def _pending_plan(self):
        work.require(self._native_pending_plan_raw is not None, 'Missing validated native pending plan')
        plan=dict(version=work.NATIVE_PENDING_FAMILIES_PLAN_VERSION if self._native_pending_families else work.NATIVE_PENDING_PLAN_VERSION,
            bindingVersion=work.WORKER_FAMILY_PENDING_BINDING_VERSION if self._native_pending_families else work.WORKER_PHASE_PENDING_BINDING_VERSION,
            budgets=work.strict(self._native_pending_plan_raw),budgetsSha256=hashlib.sha256(self._native_pending_plan_raw).hexdigest(),
            coverage='DeclaredProposalCommandPendingWritersOnly',pathInventoryComplete=self._native_pending_families,filesystemConfinement=False,usableForAdmission=False)
        if self._native_lease_budgets_raw is not None:
            plan.update(version=work.NATIVE_LEASE_PLAN_VERSION,bindingVersion=work.WORKER_NATIVE_LEASE_BINDING_VERSION,
                nativeLeaseBudgets=work.strict(self._native_lease_budgets_raw),
                nativeLeaseBudgetsSha256=hashlib.sha256(self._native_lease_budgets_raw).hexdigest())
        return plan

    def _pending_snapshot(self):
        self._verify_pending_plan_binding()
        plan=self._pending_plan();observations={}
        for phase,budget in plan['budgets'].items():
            receipts=[r for r in self.owner.ledger.workers if r['receipt']['phase']==phase]
            work.require(len(receipts)<=1, 'Repeated native pending receipt')
            if not receipts:
                observations[phase]=dict(status='Missing' if phase in self._native_pending_phases else 'NotInvoked',receiptSha256=None,storage=None)
                continue
            retained=receipts[0];receipt=retained['receipt']
            expected=work.NATIVE_LEASE_RECEIPT_VERSION if self._native_lease_budgets_raw is not None else work.NATIVE_FAMILY_PENDING_RECEIPT_VERSION if self._native_pending_families else work.NATIVE_PHASE_PENDING_RECEIPT_VERSION
            observed=work.pending_families_observation(receipt['pendingStorage']) if self._native_pending_families else work.native_pending_observation(receipt['pendingStorage'],allow_empty=True)
            work.require(receipt['version']==expected and observed==budget
                and (not self._native_pending_families or receipt['pendingStorage']['phase']==phase and receipt['pendingStorage']['studyRoot']==str(self.output)),
                'Changed retained native pending phase observation')
            observations[phase]=dict(status=receipt['outcome'],receiptSha256=retained['sha256'],storage=receipt['pendingStorage'])
            if self._native_lease_budgets_raw is not None:
                lease=receipt['nativeLeases']
                work.require(work.native_lease_observation(lease)==plan['nativeLeaseBudgets'][phase]
                    and lease['studyRoot']==str(self.output) and lease['phase']==phase, 'Changed retained native lease observation')
                observations[phase]['leases']=lease
        return work.strict(work.canonical(dict(**plan,observations=observations,
            allNativePhasesComplete=all(o['status']=='Complete' for o in observations.values()),
            counterMeaning='PerPhaseCumulativeSnapshots;DoNotAddRetainedCopies',wholeProcessCoverage=False)))

    def _verify_pending_plan_binding(self):
        path=self.publication.path('storage-binding.json')
        work.require(self.storage_binding_sha256 is not None and self.counters.sha(path)==self.storage_binding_sha256,
                     'Missing or changed native pending supervisor binding')
        bound=work.strict(self.counters.read_bytes(path))
        work.require(bound.get('nativePendingPlan')==self._pending_plan(), 'Changed native pending supervisor plan')

    def _storage_snapshot(self):
        """Separate scoped caps from unknown external domains and observed peaks."""
        self._verify_owner_lease_binding()
        work.require(self.storage_binding_sha256 is not None, 'Missing supervisor storage binding')
        work.unlinked(self.root)
        work.require({p.name for p in self.root.iterdir()} == {'owner','supervisor','terminal','worker-receipts'},
                     'Unclassified supervisor storage member')
        work.unlinked(self.root/'worker-receipts')
        work.require((self.root/'worker-receipts').is_dir(), 'Invalid worker receipt directory')
        work.require(self.publication_counters.sha(self.publication.path('storage-binding.json')) == self.storage_binding_sha256,
                     'Changed supervisor storage binding')
        scopes = {'owner':self.owner.storage.snapshot(), 'supervisor':self.publication.snapshot(),
                  'terminal':self.observation_publication.snapshot()}
        for name,value in scopes.items():
            work.require(value['contractSha256'] == hashlib.sha256(work.canonical(self._storage_contracts[name])).hexdigest(),
                         'Changed supervisor storage declaration')
        return dict(version='tower-proposal-supervisor-storage-v1', bindingSha256=self.storage_binding_sha256,
            scopes=scopes, currentRetainedBytes=sum(s['currentRetainedBytes'] for s in scopes.values()),
            currentScratchBytes=sum(s['currentScratchBytes'] for s in scopes.values()),
            declaredManagedLiveByteUpperBound=sum(c['maxLiveBytes'] for c in self._storage_contracts.values()),
            declaredManagedScratchByteUpperBound=sum(c['maxScratchBytes'] for c in self._storage_contracts.values()),
            declaredManagedTotalWriteByteUpperBound=sum(c['maxTotalWrittenBytes'] for c in self._storage_contracts.values()),
            simultaneousPeakBytes=None, upperBoundMeaning='SumOfDeclaredDisjointScopeCaps;NotObservedPeakOrScientificLimit',
            scopeCounters={name:dict(sorted(c.values.items())) for name,c in
                           (('owner',self.counters),('supervisor',self.publication_counters),('terminal',self.observation_counters))},
            counterMeaning='CumulativeSnapshotsOverlapEarlierOwnerAndPublicationObservations',
            missingCoverage=(['workerReceiptOriginals'] if self._worker_sidecar_limits is None else ['workerSidecarExternalMutations'])+
                            (['workerLogsAndStudyFiles'] if self._worker_log_limits is None else ['workerLogExternalMutationsAndStudyFiles'])+[
                             'nativeLeaseExternalMutationsAndHardElapsedLifetime' if self.owner_leases is not None and self._native_lease_budgets_raw is not None else
                             'nativeLeaseExternalMutationsAndEnclosingOwnerLifetime' if self._native_lease_budgets_raw is not None else
                             'nativePendingPublicationAndLeases' if self._native_pending_plan_raw is None else 'nativePendingExternalWritesAndLeases',
                             'runtimeAndExternalCaches','terminalObservationPersistenceAndRemainingObserverLifetime'],
            filesystemConfinement=False, externalWritesBounded=False, wholeProcessCoverage=False, usableForAdmission=False,
            **(dict(workerSidecarByteLimits=self._worker_sidecar_limits,
                    declaredWorkerSidecarByteUpperBound=sum(v['total'] for v in self._worker_sidecar_limits.values()))
               if self._worker_sidecar_limits is not None else {}),
            **(dict(workerLogByteLimits=self._worker_log_limits,
                    declaredWorkerLogByteUpperBound=sum(self._worker_log_limits.values()))
               if self._worker_log_limits is not None else {}),
            **(dict(nativePending=self._pending_snapshot()) if self._native_pending_plan_raw is not None else {}),
            **(dict(ownerLeases=self.owner_leases.snapshot()) if self.owner_leases is not None else {}))

    def run(self, phase, command, cwd, log_path, deadline, cleanup_seconds, check):
        work.require(phase == self.phase_name, 'Wrong active supervisor phase')
        self._verify_owner_lease_binding()
        if self.owner_leases is not None: self.owner_leases.require_held()
        pending_options = {}
        if self._native_pending_plan_raw is not None and phase in work.NATIVE_PHASES:
            work.require(phase not in self._native_pending_phases, 'Native pending phase cannot be reused')
            self._verify_pending_plan_binding()
            self._native_pending_phases.add(phase)
            budget=work.strict(self._native_pending_plan_raw)[phase]
            pending_options=dict(pending_families_budget=budget) if self._native_pending_families else dict(
                pending_storage_budget=budget,pending_binding_version=work.WORKER_PHASE_PENDING_BINDING_VERSION)
            if self._native_lease_budgets_raw is not None:
                pending_options['native_lease_budget']=work.strict(self._native_lease_budgets_raw)[phase]
        producer = self.auditor if phase == 'independentAudit' else self.harness
        module = Path(work.__file__) if phase == 'independentAudit' else self.harness
        helper = self.request_path.parent/'bounded_windows_process.py'
        helper_pin = self.counters.sha(helper)
        log_options = {}
        if self._worker_log_limits is not None:
            # One original log per phase: retries cannot silently multiply the bound.
            log_key = os.path.normcase(os.path.abspath(log_path))
            work.require(phase not in self._worker_log_paths and log_key not in self._worker_log_paths,
                         'Worker log phase or path cannot be reused')
            self._worker_log_paths.update((phase, log_key))
            log_options['log_byte_limit'] = self._worker_log_limits[phase]
        def observe(value):
            if self._worker_log_limits is not None:
                work.require(value.get('logCapture',{}).get('maxBytes')==self._worker_log_limits[phase],
                             'Missing or mismatched worker log limit observation')
            self.owner.observe_process(value, helper_pin)
        terminal = None
        def invoke(binding, pin):
            nonlocal terminal
            args = list(command)
            if phase == 'independentAudit':
                # Preserve Python flags and audit/output arguments; use the
                # authenticated script beside its authenticated accounting module.
                work.require(args[4] == str(self.output/'auditor.py'), 'Changed independent worker command')
                args[4] = str(self.auditor)
                args += ['--work-binding', str(binding), '--work-binding-pin', pin]
            else:
                expected = {'native':'tower-proposal-study-run', 'nativeAudit':'tower-proposal-study-audit',
                            'publication':'tower-proposal-study-publication-check'}[phase]
                work.require(len(args)==4 and args[1:]==[str(self.harness), expected, str(self.output)],
                             'Changed native worker command')
                args += ['--work-binding', str(binding), pin]
            terminal = self.run_process(args, cwd, log_path, deadline, cleanup_seconds=cleanup_seconds, check=check,
                file_work=self.counters, observe=observe, **log_options)
            return terminal
        try:
            return self.owner.run_worker(self.output, producer, module,
                self.root/'worker-receipts'/(phase+'-work.json'), invoke,
                publication_path=self.root/'worker-receipts'/(phase+'-receipt-publication.json'),
                publication_persistence_path=(self.root/'worker-receipts'/(phase+'-publication-persistence.json')
                    if self.worker_observation_persistence else None),
                sidecar_byte_limits=None if self._worker_sidecar_limits is None else self._worker_sidecar_limits[phase],**pending_options)
        except BaseException:
            # Preserve the launcher's existing failed-process receipt and error
            # path. Missing/failed worker accounting is already recorded by the
            # retained owner. A successful process cannot bypass that failure.
            if terminal is not None and (terminal['exitCode'] != 0 or terminal['timedOut'] or terminal['activeProcesses'] != 0):
                return terminal
            raise

    def _seal(self, name, value):
        self.bound_check()
        raw = work.canonical(value)
        with self.publication.create(name, 'retained') as stream:
            offset = 0
            while offset < len(raw):
                count = stream.write(raw[offset:])
                work.require(type(count) is int and 0 < count <= len(raw)-offset, 'Incomplete supervisor publication')
                offset += count
            stream.flush()
        self.bound_check()
        return hashlib.sha256(raw).hexdigest()

    def _seal_observation(self, name, value):
        self.bound_check()
        raw = work.canonical(value)
        stream = self.observation_publication.create(name, 'retained')
        original = None
        try:
            offset = 0
            while offset < len(raw):
                count = stream.write(raw[offset:])
                work.require(type(count) is int and 0 < count <= len(raw)-offset, 'Incomplete supervisor observation write')
                offset += count
            stream.flush()
        except BaseException as error:
            original = error
            raise
        finally:
            try: stream.close()
            except BaseException as error:
                if original is None: raise
                original.add_note('Supervisor observation close failed: '+repr(error))
        self.bound_check()
        return hashlib.sha256(raw).hexdigest()

    def _persist_observation(self, failure):
        """Persist the supervisor snapshot; return its enclosing accounting in memory.

        The snapshot keeps its original pre-publication boundary. Its enclosing
        observation includes both new files through close and verification, but
        still excludes external persistence, console output and process exit.
        """
        storage = observed_bytes = persistence_error = ownership = None
        try:
            work.require(self.final_observation is not None, 'Missing supervisor observation')
            observation_pin = self._seal_observation('supervisor-observation.json', self.final_observation)
            self.observation_manifest_sha256 = self._seal_observation('files.json', {'supervisor-observation.json':observation_pin})
            storage = self.observation_publication.snapshot()
            work.require(self.observation_counters.sha(self.observation_publication.path('files.json')) == self.observation_manifest_sha256
                and self.observation_counters.sha(self.observation_publication.path('supervisor-observation.json')) == observation_pin,
                'Changed supervisor observation publication')
            if self._storage_contracts is not None: ownership = self._storage_snapshot()
            observed_bytes = self.bound_check()
        except BaseException as error:
            persistence_error = error
            storage = observed_bytes = self.observation_manifest_sha256 = ownership = None
            raise
        finally:
            self.terminal_observation = dict(version='tower-proposal-supervisor-persistence-v1',
                requestSha256=self.owner.ledger.request, producerSha256=self.owner.ledger.producer,
                supervisorManifestSha256=self.manifest_sha256, ownerManifestSha256=self.owner.manifest_sha256,
                observationManifestSha256=self.observation_manifest_sha256,
                outcome='Complete' if failure is None and persistence_error is None and self.final_observation['outcome']=='Complete' else 'Failed',
                enclosingSeconds=time.monotonic()-self.started_at, observedCombinedBytes=observed_bytes,
                supervisorObservationPersistenceIncluded=persistence_error is None,
                publicationCounters=dict(sorted(self.observation_counters.values.items())),
                managedObservationStorage=storage,
                persistenceError=None if persistence_error is None else repr(persistence_error),
                coverage='SupervisorObservationPublicationAndCombinedStorageSamples',
                boundary='AfterObservationManifestCloseAndVerification' if persistence_error is None else 'FailedObservationPublicationAttempt',
                terminalObservationPersistenceExcluded=True,
                missingCoverage=['terminalObservationPersistence', 'finalConsoleAndProcessExit',
                                 'wholeOwnerMemoryLifetime', 'unobservedWorkerIO', 'wholeProcessScratch'],
                wholeProcessCoverage=False, usableForAdmission=False,
                **(dict(storageOwnership=ownership) if self._storage_contracts is not None else {}))

    def finish(self, error=None):
        if not self.started: return
        if self.closed and error is not None:
            error.add_note('Supervisor was already closed; existing evidence was preserved')
            return
        work.require(not self.closed, 'Supervisor cannot close twice')
        self.closed = True
        failure = error
        if self.owner_leases is not None:
            try:
                self._verify_owner_lease_binding()
            except BaseException as lease_error:
                if failure is None: failure=lease_error
                else: failure.add_note('Owner lease binding failed: '+repr(lease_error))
            try: self.owner_leases.finish(failure)
            except BaseException as lease_error:
                if failure is None: failure=lease_error
                else: failure.add_note('Owner lease cleanup failed: '+repr(lease_error))
        try:
            if self.phase_scope is not None:
                self.phase_scope.__exit__(type(failure) if failure is not None else None, failure,
                                          None if failure is None else failure.__traceback__)
            self.owner.__exit__(type(failure) if failure is not None else None, failure,
                                None if failure is None else failure.__traceback__)
        except BaseException as close_error:
            failure = failure or close_error
        finally:
            if self.file_scope is not None: self.file_scope.__exit__(None, None, None)
        publication_error = None
        try:
            work.require(self.bound_check is not None, 'Missing enclosing resource check')
            work.require(self.owner.final_observation is not None, 'Missing final owner observation')
            owner_pin = self._seal('final-owner-observation.json', self.owner.final_observation)
            members = {'final-owner-observation.json':owner_pin}
            if self._storage_contracts is not None:
                work.require(self.storage_binding_sha256 is not None, 'Missing supervisor storage binding')
                members['storage-binding.json'] = self.storage_binding_sha256
            self.manifest_sha256 = self._seal('files.json', members)
            storage = self.publication.snapshot()
            work.require(self.publication_counters.sha(self.publication.path('files.json')) == self.manifest_sha256
                         and self.publication_counters.sha(self.publication.path('final-owner-observation.json')) == owner_pin,
                         'Changed supervisor publication')
            observed_bytes = self.bound_check()
            self.final_observation = dict(version='tower-proposal-supervisor-observation-v1',
                requestSha256=self.owner.ledger.request, producerSha256=self.owner.ledger.producer,
                supervisorManifestSha256=self.manifest_sha256, ownerManifestSha256=self.owner.manifest_sha256,
                outcome='Complete' if failure is None and self.owner.final_observation['outcome']=='Complete' else 'Failed',
                enclosingSeconds=time.monotonic()-self.started_at, observedCombinedBytes=observed_bytes,
                ownerObservationPersistenceIncluded=True, publicationCounters=dict(self.publication_counters.values),
                managedSupervisorStorage=storage, supervisorObservationPersistenceExcluded=True,
                coverage='OwnerObservationPublicationAndCombinedStorageSamples',
                missingCoverage=['supervisorObservationPersistence', 'finalConsoleAndProcessExit',
                                 'wholeOwnerMemoryLifetime', 'unobservedWorkerIO', 'wholeProcessScratch'],
                wholeProcessCoverage=False, usableForAdmission=False,
                **(dict(ownerLeases=self.owner_leases.snapshot()) if self.owner_leases is not None else {}))
            self._persist_observation(failure)
        except BaseException as caught:
            publication_error = caught
            self.manifest_sha256 = None
            self.observation_manifest_sha256 = None
        if failure is not None:
            if publication_error is not None: failure.add_note('Supervisor publication failed: '+repr(publication_error))
            if error is None: raise failure
        elif publication_error is not None:
            raise publication_error
