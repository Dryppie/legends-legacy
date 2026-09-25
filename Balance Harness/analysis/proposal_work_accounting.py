"""Opt-in work counters, managed storage lifetime and bound owner receipts.

These collectors do not launch studies or change resource admission. Coverage is
explicitly limited to instrumented operations; missing process coverage is unknown.
"""
from collections import Counter
from contextlib import ExitStack, contextmanager, nullcontext
import hashlib
import io
import json
import math
import os
from pathlib import Path
import re
import shutil
import stat
import time

VERSION = 'tower-proposal-work-counters-v1'
OWNER_VERSION = 'tower-proposal-owner-work-v1'
PHASES = ('native', 'nativeAudit', 'independentAudit', 'publication')
WORKER_BINDING_VERSION = 'tower-proposal-worker-binding-v1'
WORKER_PUBLICATION_BINDING_VERSION = 'tower-proposal-worker-binding-v2'
WORKER_PUBLICATION_VERSION = 'tower-proposal-worker-publication-v1'
WORKER_PERSISTENCE_BINDING_VERSION = 'tower-proposal-worker-binding-v3'
WORKER_PERSISTENCE_VERSION = 'tower-proposal-worker-observation-persistence-v1'
WORKER_SIDECAR_BINDING_VERSION = 'tower-proposal-worker-binding-v4'
WORKER_PENDING_BINDING_VERSION = 'tower-proposal-worker-binding-v5'
NATIVE_PENDING_RECEIPT_VERSION = 'tower-proposal-work-counters-v2'
NATIVE_PENDING_VERSION = 'tower-native-pending-storage-v1'
WORKER_PHASE_PENDING_BINDING_VERSION = 'tower-proposal-worker-binding-v6'
NATIVE_PHASE_PENDING_RECEIPT_VERSION = 'tower-proposal-work-counters-v3'
NATIVE_PENDING_PLAN_VERSION = 'tower-proposal-native-pending-plan-v1'
WORKER_FAMILY_PENDING_BINDING_VERSION = 'tower-proposal-worker-binding-v7'
NATIVE_FAMILY_PENDING_RECEIPT_VERSION = 'tower-proposal-work-counters-v4'
NATIVE_PENDING_FAMILIES_VERSION = 'tower-proposal-pending-families-v1'
NATIVE_PENDING_FAMILIES_STORAGE_VERSION = 'tower-native-pending-storage-v2'
NATIVE_PENDING_FAMILIES_PLAN_VERSION = 'tower-proposal-native-pending-plan-v2'
WORKER_NATIVE_LEASE_BINDING_VERSION = 'tower-proposal-worker-binding-v8'
NATIVE_LEASE_RECEIPT_VERSION = 'tower-proposal-work-counters-v5'
NATIVE_LEASE_VERSION = 'tower-proposal-native-leases-v1'
NATIVE_LEASE_PLAN_VERSION = 'tower-proposal-native-pending-plan-v3'
NATIVE_PHASES = ('native','nativeAudit','publication')
PROPOSAL_PENDING_PROTECTED = ('content','executable','source','request.json','launch.json','launcher.py','auditor.py',
                              'bounded_windows_process.py','admission-files.json','admission-receipt.json','admission-binding.json')


def native_pending_budget(value, *, protected_paths=(), allow_empty=False):
    """Copy a native-only finite declaration. This is not filesystem confinement."""
    def number(n, maximum=2**63-1, minimum=0):
        require(type(n) is int and minimum <= n <= maximum, 'Invalid native pending limit')
        return n
    def path(raw):
        require(type(raw) is str and raw and os.path.isabs(raw), 'Pending paths must be absolute')
        full = os.path.abspath(raw)
        normalized = raw.replace(os.altsep, os.sep) if os.altsep else raw
        require(os.path.normcase(normalized) == os.path.normcase(full), 'Pending paths must be canonical')
        require(not full.startswith(('\\\\?\\','\\\\.\\')) and Path(full).name, 'Invalid pending file path')
        for part in Path(full).parts[1:]:
            stem = part.split('.')[0].upper()
            require(part and part[-1] not in ' .' and ':' not in part and '\x00' not in part
                and (os.name != 'nt' or not any(ord(c)<32 or c in '<>"|?*' for c in part))
                and stem not in {'CON','PRN','AUX','NUL'}
                and not (len(stem)==4 and stem[:3] in ('COM','LPT') and stem[3] in '123456789'),
                'Aliased or invalid pending path segment')
        return str(Path(full))
    require(type(value) is dict and set(value)=={'files','maxLiveBytes','maxTotalWrittenBytes'}
        and type(value['files']) is list and (value['files'] or allow_empty and value['maxLiveBytes']==0 and value['maxTotalWrittenBytes']==0),
        'Invalid native pending declaration')
    result = dict(files=[], maxLiveBytes=number(value['maxLiveBytes']), maxTotalWrittenBytes=number(value['maxTotalWrittenBytes']))
    paths=[]
    for file in value['files']:
        require(type(file) is dict and set(file)=={'path','destination','maxBytes','maxCreates'}, 'Invalid native pending file')
        copied=dict(path=path(file['path']), destination=path(file['destination']), maxBytes=number(file['maxBytes']),
                    maxCreates=number(file['maxCreates'],2**31-1,1))
        result['files'].append(copied);paths.extend((Path(copied['path']),Path(copied['destination'])))
    require(len(set(paths))==len(paths) and not any(p!=q and p.is_relative_to(q) for p in paths for q in paths),
            'Aliased or nested pending declarations')
    require(not any(p.is_relative_to(Path(q)) or Path(q).is_relative_to(p) for p in paths for q in protected_paths),
            'Pending declaration overlaps a protected worker path')
    return result


def proposal_pending_budget(root, phase, value):
    """Validate phase/path restrictions, not completeness of a dynamic native inventory."""
    require(phase in NATIVE_PHASES, 'Unknown native pending phase')
    root=Path(os.path.abspath(root))
    result=native_pending_budget(value,allow_empty=True,protected_paths=[root/name for name in PROPOSAL_PENDING_PROTECTED])
    if phase!='native':
        require(not result['files'], 'Audit/publication phases prohibit pending writers')
    else:
        require(result['files'] and all(Path(f['destination'])!=root and Path(f['destination']).is_relative_to(root)
            and Path(f['path'])==Path(f['destination']+'.pending') for f in result['files']),
            'Proposal pending paths must be target-adjacent members of the study output')
    return result


def proposal_pending_plan(root, budgets):
    require(type(budgets) is dict and set(budgets)==set(NATIVE_PHASES), 'Declare all three native pending phases')
    return {phase:proposal_pending_budget(root,phase,budgets[phase]) for phase in NATIVE_PHASES}


def pending_family_limits(phase, value):
    require(phase in NATIVE_PHASES and type(value) is dict
        and set(value)=={'version','maxFileBytes','maxLiveBytes','maxTotalWrittenBytes'}
        and value['version']==NATIVE_PENDING_FAMILIES_VERSION
        and all(type(value[k]) is int and 0 <= value[k] < 2**63 for k in ('maxFileBytes','maxLiveBytes','maxTotalWrittenBytes')),
        'Invalid proposal pending family declaration')
    require(phase=='native' or all(value[k]==0 for k in ('maxFileBytes','maxLiveBytes','maxTotalWrittenBytes')),
        'Audit/publication phases prohibit pending writers')
    return dict(value)


def pending_family_fixed(root, phase, value):
    budget=pending_family_limits(phase,value)
    names=['history-files.json','entropy-intent.json','history-input.json','entropy.bin','allocation.json','seed-ledger.json',
           'provisional-result.json','native-receipt.json','study/binding.json','study/freeze.json','study/summary.json','study/evidence-storage.json']
    for n in range(1,13): names.extend((f'study/pair-{n:02d}.json',f'study/placement-catalogue-{n:02d}.json'))
    return [dict(path=str(Path(root)/name)+'.pending',destination=str(Path(root)/name),maxBytes=budget['maxFileBytes'],
                 maxCreates=2 if name=='history-input.json' else 1) for name in names] if phase=='native' else []


def proposal_pending_families_budget(root, phase, value, *, protected_paths=()):
    budget=pending_family_limits(phase,value);root=Path(os.path.abspath(root))
    protected=tuple(protected_paths)+tuple(root/name for name in PROPOSAL_PENDING_PROTECTED)
    native_pending_budget(dict(files=pending_family_fixed(root,phase,budget),maxLiveBytes=budget['maxLiveBytes'],
        maxTotalWrittenBytes=budget['maxTotalWrittenBytes']),allow_empty=True,protected_paths=protected)
    if phase=='native':
        directory=root/'study'
        require(not any(directory.is_relative_to(Path(p)) or Path(p).is_relative_to(directory) for p in protected),
                'Pending family overlaps a protected worker path')
    return budget


def pending_families_observation(value):
    require(type(value) is dict and value.get('version')==NATIVE_PENDING_FAMILIES_STORAGE_VERSION
        and {'studyRoot','phase','familyBudget'} <= set(value), 'Invalid pending family observation')
    root=value['studyRoot'];phase=value['phase']
    require(type(root) is str and os.path.isabs(root) and str(Path(os.path.abspath(root)))==root, 'Invalid pending family root')
    budget=proposal_pending_families_budget(root,phase,value['familyBudget'])
    fixed=pending_family_fixed(root,phase,budget);contracts=value.get('contracts')
    require(type(contracts) is list and len(fixed) <= len(contracts) <= len(fixed)+(36 if phase=='native' else 0)
        and contracts[:len(fixed)]==fixed, 'Changed fixed pending family catalogue')
    members=Counter()
    for file in contracts[len(fixed):]:
        require(type(file) is dict and type(file.get('destination')) is str, 'Invalid dynamic pending member')
        target=Path(file['destination'])
        match=re.fullmatch(r'heldout-(0[1-9]|1[0-2])-([0-9a-f]{64})\.json',target.name)
        require(target.parent==Path(root)/'study' and match is not None, 'Undeclared dynamic pending member')
        members[match[1]]+=1
        require(members[match[1]]<=3 and file==dict(path=str(target)+'.pending',destination=str(target),
            maxBytes=budget['maxFileBytes'],maxCreates=1), 'Exhausted or changed dynamic pending family')
    legacy={k:v for k,v in value.items() if k not in ('studyRoot','phase','familyBudget')}
    legacy['version']=NATIVE_PENDING_VERSION
    observed=native_pending_observation(legacy,allow_empty=True)
    require(observed['maxLiveBytes']==budget['maxLiveBytes'] and observed['maxTotalWrittenBytes']==budget['maxTotalWrittenBytes'],
            'Changed pending family aggregate bounds')
    return budget


def native_pending_observation(value, *, allow_empty=False):
    require(type(value) is dict and set(value)=={'version','outcome','maxLiveBytes','maxTotalWrittenBytes',
        'acceptedWriteBytes','trackedLiveBytes','peakTrackedLiveBytes','publishedBytes','deletedBytes',
        'failedProgressMayBeUnknown','contracts','creates','scope','filesystemConfinement','wholeProcessCoverage','usableForAdmission'},
        'Unknown native pending observation fields')
    require(value['version']==NATIVE_PENDING_VERSION and value['outcome'] in ('Complete','FailedOrIncomplete')
        and value['scope']=='DeclaredInstrumentedPendingWritersOnly' and value['filesystemConfinement'] is False
        and value['wholeProcessCoverage'] is False and value['usableForAdmission'] is False
        and type(value['failedProgressMayBeUnknown']) is bool, 'Unsupported native pending coverage')
    budget=native_pending_budget(dict(files=value['contracts'],maxLiveBytes=value['maxLiveBytes'],maxTotalWrittenBytes=value['maxTotalWrittenBytes']),allow_empty=allow_empty)
    keys=('acceptedWriteBytes','trackedLiveBytes','peakTrackedLiveBytes','publishedBytes','deletedBytes')
    require(all(type(value[k]) is int and 0 <= value[k] <= 2**63-1 for k in keys), 'Invalid native pending counters')
    total,live,peak,published,deleted=(value[k] for k in keys)
    require(live <= peak <= total <= budget['maxTotalWrittenBytes'] and peak <= budget['maxLiveBytes']
        and total==live+published+deleted, 'Inconsistent native pending byte accounting')
    require(type(value['creates']) is list and len(value['creates'])==len(budget['files']), 'Invalid native pending creates')
    capacity=0
    for file,created in zip(budget['files'],value['creates']):
        require(type(created) is dict and set(created)=={'path','count'} and created['path']==file['path']
            and type(created['count']) is int and 0 <= created['count'] <= file['maxCreates'], 'Invalid native pending create count')
        capacity+=file['maxBytes']*created['count']
    require(total <= capacity, 'Native pending writes exceed create capacity')
    require(value['outcome']!='Complete' or live==0 and value['failedProgressMayBeUnknown'] is False,
            'Incomplete native pending storage')
    return budget


def native_lease_limits(phase, value):
    require(phase in NATIVE_PHASES and type(value) is dict and set(value)=={'version','maxConcurrentLeases'}
        and value['version']==NATIVE_LEASE_VERSION and type(value['maxConcurrentLeases']) is int
        and 0 <= value['maxConcurrentLeases'] <= 26 and (phase=='native' or value['maxConcurrentLeases']==0),
        'Invalid native lease contract')
    return dict(value)


def native_lease_paths(root, phase):
    root=Path(root)
    if phase!='native': return []
    names=[(root.parent/'complete-family-allocation','EnclosingOwnerProbe'),(root,'EnclosingOwnerProbe')]
    names += [(root/f'search/root-{n:02d}/{arm}/racing','NativeOwned') for n in range(1,13) for arm in ('control','candidate')]
    return [dict(path=str(p)+'.writer.lock',role=role,maxAttempts=1) for p,role in names]


def proposal_native_lease_budget(root, phase, value, *, protected_paths=()):
    budget=native_lease_limits(phase,value);root=Path(os.path.abspath(root))
    paths=[Path(e['path']) for e in native_lease_paths(root,phase)]
    require(len(set(paths))==len(paths), 'Aliased native lease catalogue')
    protected=tuple(protected_paths)+tuple(root/name for name in PROPOSAL_PENDING_PROTECTED)
    require(not any(p.is_relative_to(Path(q)) or Path(q).is_relative_to(p) for p in paths for q in protected),
            'Native lease overlaps a protected worker path')
    return budget


def native_lease_observation(value):
    require(type(value) is dict and set(value)=={'version','studyRoot','phase','budget','outcome','maxLeaseBytes','acceptedWriteBytes',
        'currentOwnedHandles','peakOwnedHandles','failedOrUnknown','entries','scope','enclosingOwnerLifetimeBounded','wallClockBounded',
        'filesystemConfinement','wholeProcessCoverage','usableForAdmission'}, 'Unknown native lease observation fields')
    require(value['version']==NATIVE_LEASE_VERSION and value['scope']=='DeclaredNativeLeaseHandlesOnly'
        and value['outcome'] in ('Complete','FailedOrIncomplete') and type(value['failedOrUnknown']) is bool
        and all(value[k] is False for k in ('enclosingOwnerLifetimeBounded','wallClockBounded','filesystemConfinement','wholeProcessCoverage','usableForAdmission')),
        'Unsupported native lease coverage')
    root=value['studyRoot'];phase=value['phase']
    require(type(root) is str and os.path.isabs(root) and str(Path(os.path.abspath(root)))==root, 'Invalid native lease root')
    budget=proposal_native_lease_budget(root,phase,value['budget']);expected=native_lease_paths(root,phase)
    require(all(type(value[k]) is int and value[k]==0 for k in ('maxLeaseBytes','acceptedWriteBytes')), 'Native leases must remain empty')
    require(type(value['entries']) is list and len(value['entries'])==len(expected), 'Changed native lease catalogue')
    acquired=released=errors=forced=probe_acquired=0
    for e,contract in zip(value['entries'],expected):
        keys=('attempts','acquired','released','heldConflicts','releaseFailures','forcedCleanup')
        require(type(e) is dict and set(e)==set(contract)|set(keys) and all(e[k]==v for k,v in contract.items())
            and type(e['maxAttempts']) is int and all(type(e[k]) is int and 0<=e[k]<=1 for k in keys), 'Invalid native lease entry')
        require(e['acquired']+e['heldConflicts']<=e['attempts'] and e['released']+e['releaseFailures']<=e['acquired']
            and e['forcedCleanup']<=e['acquired'] and (e['role']=='EnclosingOwnerProbe' or e['heldConflicts']==0), 'Inconsistent native lease lifetime')
        acquired+=e['acquired'];released+=e['released'];errors+=e['releaseFailures'];forced+=e['forcedCleanup']
        if e['role']=='EnclosingOwnerProbe':probe_acquired+=e['acquired']
    live,peak=value['currentOwnedHandles'],value['peakOwnedHandles']
    require(type(live) is int and type(peak) is int and live==acquired-released and 0<=live<=peak<=min(acquired,budget['maxConcurrentLeases']),
            'Inconsistent native lease handle bounds')
    require(value['outcome']!='Complete' or not value['failedOrUnknown'] and live==errors==forced==probe_acquired==0,
            'Incomplete native lease scope')
    return budget


def sidecar_limits(value):
    require(type(value) is dict and set(value) == {'receipt','publication','persistence','total'}
            and all(type(n) is int and 0 <= n <= 2**63-1 for n in value.values()), 'Invalid worker sidecar byte limits')
    return dict(value)


class SidecarWrites:
    """Caps for three exclusive, append-only cooperative sidecar writers.

    Failed writes have unknown progress and poison subsequent writes. This is not
    OS confinement or coverage of worker logs, external caches or other paths.
    """
    def __init__(self, limits):
        self.limits = sidecar_limits(limits)
        self.files, self.total, self.failed = {}, 0, False

    def call(self, action):
        try: return action()
        except BaseException:
            self.failed = True
            raise

    def open(self, role, path):
        path = Path(path)
        def opening():
            require(not self.failed and role in ('receipt','publication','persistence') and role not in self.files,
                    'Invalid worker sidecar reservation')
            unlinked(path)
            stream = path.open('xb', buffering=0)
            writer = None
            try:
                writer = _SidecarWriter(self, role, path, stream)
                self.files[role] = writer
            except BaseException as original:
                try:
                    if writer is None: stream.close()
                    else: writer.close()
                except BaseException as error:
                    _json_failure(original, error, 'Worker sidecar acquisition close failed; secondary details omitted.')
                raise
            return writer
        return self.call(opening)

    def __enter__(self): return self

    def __exit__(self, _, original, __):
        failure = original
        for writer in self.files.values():
            try: writer.close()
            except BaseException as error:
                failure = _json_failure(failure, error, 'Worker sidecar cleanup failed; secondary details omitted.')
        if original is not None: return False
        if failure is not None: raise failure
        def verifying():
            require(not self.failed and set(self.files)=={'receipt','publication','persistence'},
                    'Incomplete worker sidecar storage')
            for writer in self.files.values():
                unlinked(writer.path)
                info = writer.path.stat()
                require(stat.S_ISREG(info.st_mode) and info.st_size == writer.size, 'Changed worker sidecar length')
        self.call(verifying)


class _SidecarWriter:
    def __init__(self, owner, role, path, stream):
        self.owner, self.role, self.path, self.stream = owner, role, path, stream
        self.size, self.closed = 0, False

    def write(self, raw):
        def writing():
            require(not self.closed and not self.owner.failed and type(raw) is bytes, 'Unavailable worker sidecar writer')
            require(len(raw) <= self.owner.limits[self.role]-self.size
                    and len(raw) <= self.owner.limits['total']-self.owner.total, 'Worker sidecar byte limit exceeded')
            count = self.stream.write(raw)
            require(type(count) is int and 0 <= count <= len(raw) and (count > 0 or not raw), 'Invalid worker sidecar write result')
            self.size += count
            self.owner.total += count
            return count
        return self.owner.call(writing)

    def flush(self): return self.owner.call(self.stream.flush)
    def fileno(self): return self.stream.fileno()
    def close(self):
        if not self.closed:
            self.closed = True
            self.owner.call(self.stream.close)
    def __enter__(self): return self
    def __exit__(self, _, error, __):
        try: self.close()
        except BaseException as closing:
            if error is None: raise
            _json_failure(error, closing, 'Worker sidecar close failed; secondary details omitted.')


def require(ok, message):
    if not ok:
        raise ValueError(message)


def pin(value):
    require(isinstance(value, str) and re.fullmatch('[0-9a-f]{64}', value), 'Invalid receipt binding')
    return value


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False, allow_nan=False).encode('utf-8')


def process_cleanup_observation(value):
    """Validate close attempts separately from process drain and observer work."""
    require(type(value) is dict and set(value)=={'version','handleCloses','errors','allAcquiredHandlesClosed',
        'waitPolicy','cleanupAllowanceSeconds','boundary','observerInvocationExcluded','wallClockBounded',
        'wholeProcessCoverage','usableForAdmission'}, 'Invalid process cleanup fields')
    require(value['version']=='tower-owned-process-cleanup-v1'
        and value['waitPolicy']=='SharedMonotonicDeadline;NoRetry'
        and value['boundary']=='AfterHandleCloseAttemptsBeforeObserverInvocation'
        and value['observerInvocationExcluded'] is True
        and all(value[k] is False for k in ('wallClockBounded','wholeProcessCoverage','usableForAdmission')),
        'Invalid process cleanup coverage')
    require(type(value['cleanupAllowanceSeconds']) in (int,float) and math.isfinite(value['cleanupAllowanceSeconds'])
        and 0<value['cleanupAllowanceSeconds']<=2, 'Invalid cleanup allowance')
    handles=value['handleCloses']
    require(type(handles) is dict and set(handles)=={'thread','process','job'}
        and all(type(v) is str and v in ('NotAcquired','Closed','CloseFailed') for v in handles.values())
        and handles['job']!='NotAcquired', 'Invalid handle close states')
    require(type(value['allAcquiredHandlesClosed']) is bool
        and value['allAcquiredHandlesClosed']==('CloseFailed' not in handles.values()), 'Invalid handle close coverage')
    errors=value['errors'];operations=set()
    allowed={'terminationAndDrain','logFinalization','jobIoObservation','memoryObservation',
             'threadHandleClose','processHandleClose','jobHandleClose'}
    require(type(errors) is list and len(errors)<=len(allowed), 'Invalid cleanup errors')
    for error in errors:
        require(type(error) is dict and set(error)=={'operation','error'}
            and type(error['operation']) is str and error['operation'] in allowed
            and error['operation'] not in operations and type(error['error']) is str and bool(error['error']),
            'Invalid cleanup error')
        operations.add(error['operation'])
    require(all((state=='CloseFailed')==(name+'HandleClose' in operations) for name,state in handles.items()),
            'Inconsistent failed handle close')


def job_limit_observation(value):
    """Validate optional pre-creation job limits without promoting their scope."""
    excluded = {'violationsObserved','monitorMemoryBounded','totalProcessCreationsBounded',
                'wallClockBounded','filesystemConfinement','wholeProcessCoverage','usableForAdmission'}
    require(type(value) is dict and set(value) == {'version','declaration','applied','verified','error',
            'verificationBoundary','scope',*excluded}, 'Invalid job limit observation fields')
    require(value['version']=='tower-owned-job-limits-observation-v1'
            and value['scope']=='AssignedJobAndDescendants;CommittedVirtualMemoryAndConcurrentProcesses'
            and all(value[k] is False for k in excluded), 'Invalid job limit coverage')
    declared = value['declaration']
    require(type(declared) is dict and set(declared)=={'version','maxJobCommitBytes','maxActiveProcesses'}
            and declared['version']=='tower-owned-job-limits-v1', 'Invalid observed job limit declaration')
    require(type(declared['maxJobCommitBytes']) is int and 0<declared['maxJobCommitBytes']<2**63
            and type(declared['maxActiveProcesses']) is int and 0<declared['maxActiveProcesses']<2**32,
            'Invalid observed job limit values')
    require(type(value['applied']) is bool and type(value['verified']) is bool, 'Invalid job limit state')
    if value['verified']:
        require(value['applied'] and value['error'] is None and value['verificationBoundary']=='BeforeOwnedProcessCreation',
                'Invalid verified job limits')
    else:
        require(type(value['error']) is str and bool(value['error']) and value['verificationBoundary']=='Unavailable',
                'Unverified job limits require an error')
    return dict(declared)


def strict(raw):
    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, 'Duplicate receipt key')
            result[key] = value
        return result
    return json.loads(raw, object_pairs_hook=pairs, parse_constant=lambda _: require(False, 'Nonfinite receipt'))


def role(path):
    path = Path(path)
    if path.name == 'files.json': return 'manifest'
    if path.name == 'evidence-storage.json': return 'metadata'
    return {'.jsonl':'journal', '.gz':'payload', '.json':'json'}.get(path.suffix, 'other')


JSON_FAILURE_NOTES = 8


def _json_failure(first, error, note):
    """Keep the first error; add only a fixed literal secondary-failure note.

    Exception arguments and caller-owned notes must not run formatting hooks or
    grow diagnostic text. Annotation is optional and never replaces the error.
    """
    if first is None: return error
    try:
        attributes = BaseException.__dict__['__dict__'].__get__(first)
        if type(attributes) is not dict: return first
        missing = object()
        notes = dict.get(attributes, '__notes__', missing)
        if notes is missing: dict.__setitem__(attributes, '__notes__', [note])
        elif type(notes) is list and len(notes) < JSON_FAILURE_NOTES: list.append(notes, note)
    except BaseException:
        pass
    return first


class CountedRead(io.RawIOBase):
    def __init__(self, stream, add, leave_open=False, *, work=None):
        # An incomplete constructor does not own the stream yet.
        self._close_attempted = True
        self.stream, self.add, self.leave_open = stream, add, leave_open
        self.work = work
        self._close_attempted = False

    @contextmanager
    def _failure_scope(self):
        try: yield
        except BaseException:
            if self.work is not None: self.work._read_failed = True
            raise

    def _check_readable(self):
        require(not self._close_attempted, 'Read wrapper is closed')

    def readable(self): return True
    def seekable(self):
        with self._failure_scope():
            self._check_readable()
            return self.stream.seekable()
    def tell(self):
        with self._failure_scope():
            self._check_readable()
            return self.stream.tell()
    def seek(self, offset, whence=0):
        with self._failure_scope():
            self._check_readable()
            return self.stream.seek(offset, whence)
    def read(self, size=-1):
        with self._failure_scope():
            self._check_readable()
            data = self.stream.read(size)
            self.add(len(data))
            return data

    def readinto(self, buffer):
        with self._failure_scope():
            self._check_readable()
            count = self.stream.readinto(buffer)
            self.add(count)
            return count

    def close(self):
        if getattr(self, '_close_attempted', True): return
        self._close_attempted = True
        # Mandatory cleanup must not depend on allocating a tracking context.
        failure = None
        try:
            if not self.leave_open: self.stream.close()
        except BaseException as error:
            failure = error
        finally:
            try: super().close()
            except BaseException as error:
                failure = _json_failure(failure, error, 'Read wrapper close failed; secondary details omitted.')
        if failure is not None:
            if self.work is not None: self.work._read_failed = True
            raise failure

    def __exit__(self, kind, original, traceback):
        if original is not None and self.work is not None: self.work._read_failed = True
        try: self.close()
        except BaseException as error:
            if original is None: raise
            _json_failure(original, error, 'Read stream close failed; secondary details omitted.')
        return False


class Counters:
    def __init__(self):
        self.values = Counter()
        self._json_failed = False
        self._read_failed = False

    @contextmanager
    def _read_failure_scope(self):
        try: yield
        except BaseException:
            self._read_failed = True
            raise

    def add(self, name, count=1):
        require(type(count) is int and count >= 0, 'Invalid counter increment')
        self.values[name] += count

    def _json_failure_counts(self, original, *names):
        for name in names:
            try: self.add(name)
            except BaseException as error:
                _json_failure(original, error, 'JSON failure accounting failed; secondary details omitted.')

    def _close_json_output(self, stream, original, prefix=None):
        # Keep accounting independent of the mandatory resource close.
        failure = original
        if prefix is not None:
            try: self.add(prefix+'Attempted')
            except BaseException as error:
                failure = _json_failure(failure, error, 'JSON close accounting failed; secondary details omitted.')
        try: stream.close()
        except BaseException as error:
            outcome = 'Failed'
            failure = _json_failure(failure, error, 'JSON output close failed; secondary details omitted.')
        else: outcome = 'Completed'
        if prefix is not None:
            try: self.add(prefix+outcome)
            except BaseException as error:
                failure = _json_failure(failure, error, 'JSON close accounting failed; secondary details omitted.')
        return failure

    def open_read(self, path):
        with self._read_failure_scope():
            stream = Path(path).open('rb')
            try:
                return CountedRead(stream, lambda n:self.add('applicationReadBytes.'+role(path), n), work=self)
            except BaseException as original:
                try: stream.close()
                except BaseException as error:
                    _json_failure(original, error, 'Read stream close failed; secondary details omitted.')
                raise

    def read_bytes(self, path):
        with self.open_read(path) as stream: return stream.read()

    def sha(self, path):
        with self.open_read(path) as stream: return hashlib.file_digest(stream, 'sha256').hexdigest()

    def parse(self, stream, loader=json.load):
        with self._read_failure_scope():
            self.add('jsonParseAttempts')
            with CountedRead(stream, lambda n:self.add('jsonInputBytes', n), leave_open=True, work=self) as counted:
                value = loader(counted)
            self.add('jsonParseCompleted')
            return value

    def read_json(self, path):
        with self.open_read(path) as stream: return self.parse(stream, strict_json)

    def write_json_output(self, path, value):
        """Observe the auditor's exclusive UTF-8 text output, including close.

        Accepted UTF-8 text bytes precede TextIO newline translation/buffering;
        they are not raw-file or durable bytes. A thrown write has unknown
        progress. Preserve an original serialization/write error if close fails.
        """
        owner = self
        # Fail closed even if the failed-operation counter itself cannot update.
        # Only a fully returned writer restores the previous state.
        previous_failure = self._json_failed
        self._json_failed = True
        self.add('jsonOutputOperationsAttempted')
        stream, failure = None, None
        class Text:
            def write(self, text):
                owner.add('textOutputWriteCallsAttempted')
                try:
                    count = stream.write(text)
                    require(type(count) is int and 0 <= count <= len(text), 'Invalid text output write count')
                except BaseException as error:
                    owner._json_failure_counts(error, 'textOutputWriteCallsFailed', 'failedTextOutputWriteBytesUnknown')
                    raise
                owner.add('textOutputAcceptedUtf8Bytes.'+role(path), len(text[:count].encode('utf-8')))
                owner.add('textOutputWriteCallsCompleted')
                if count != len(text):
                    owner.add('textOutputShortWrites')
                    raise OSError('Short JSON output write')
                return count
        try:
            self.add('textOutputOpensAttempted')
            try: stream = Path(path).open('x', encoding='utf-8')
            except BaseException as error:
                self._json_failure_counts(error, 'textOutputOpensFailed')
                raise
            self.add('textOutputOpensCompleted')
            json.dump(value, Text(), indent=2)
        except BaseException as error:
            failure = error
        finally:
            if stream is not None:
                failure = self._close_json_output(stream, failure, 'textOutputCloses')
        if failure is None: self.add('jsonOutputOperationsCompleted')
        else: self._json_failure_counts(failure, 'jsonOutputOperationsFailed')
        if failure is not None: raise failure
        self._json_failed = previous_failure

    def decode_stream(self, stream):
        with self._read_failure_scope():
            self.add('decodePassesStarted')
            owner = self
            class Decoded(CountedRead):
                complete = False
                def finish(self):
                    with owner._read_failure_scope():
                        if not self.complete:
                            self.complete = True
                            owner.add('decodePassesCompleted')
                def read(self, size=-1):
                    data = super().read(size)
                    if size < 0 or (size != 0 and not data): self.finish()
                    return data
                def readinto(self, buffer):
                    count = super().readinto(buffer)
                    if len(buffer) and not count: self.finish()
                    return count
            return Decoded(stream, lambda n:self.add('decodedBytesProcessed', n), leave_open=True, work=self)

    def receipt(self, phase, request_sha256, producer_sha256, succeeded):
        require(phase in PHASES and type(succeeded) is bool, 'Invalid work receipt phase/outcome')
        if self._read_failed: succeeded = False
        return dict(version=VERSION, phase=phase, requestSha256=pin(request_sha256), producerSha256=pin(producer_sha256),
                    outcome='Complete' if succeeded and not self._json_failed else 'Failed', coverage='InstrumentedOperationsOnly',
                    counters=dict(sorted(self.values.items())), wholeProcessCoverage=False, usableForAdmission=False)


def strict_json(stream): return strict(stream.read())


class OwnerFileCounters(Counters):
    """Opt-in owner file boundaries; sizes are samples, never a storage/I/O peak.

    Native copies and inherited child log handles bypass Python stream writes.
    Keep their logical/observed bytes separate from text accepted by TextIO and
    from applicationReadBytes. Concurrent edits and failed write progress remain
    unknown. This collector neither owns the directory nor activates a study.
    """
    def __init__(self):
        super().__init__()
        self._sizes = {}
        self._owner_failed = False

    @contextmanager
    def _owner_failure_scope(self):
        # Never reset this flag: a nested failure may have been caught by an action.
        try: yield
        except BaseException:
            self._owner_failed = True
            raise

    def _owner_failure_counts(self, original, *names):
        for name in names:
            try: self.add(name)
            except BaseException as error:
                # Reuse the bounded, hook-free annotation used by JSON writers.
                _json_failure(original, error, 'Owner failure accounting failed; secondary details omitted.')

    def _final_file_observation(self, path, original, counter=None, *, missing_ok=True):
        try:
            size = self.observe_file(path, missing_ok=missing_ok)
            if size is not None and counter is not None: self.add(counter+role(path), size)
        except BaseException as error:
            return _json_failure(original, error, 'Owner final file observation failed; secondary details omitted.')
        return original

    def receipt(self, phase, request_sha256, producer_sha256, succeeded):
        result = super().receipt(phase, request_sha256, producer_sha256, succeeded)
        if self._owner_failed: result['outcome'] = 'Failed'
        return result

    def operation(self, name, action):
        """Count a selected API outcome; a returned false is not a failed call.

        These are operation counts, not bytes or evidence of storage coverage.
        APIs that suppress errors retain their original return semantics.
        """
        with self._owner_failure_scope():
            self.add(name+'Attempted')
            try: result = action()
            except BaseException as error:
                self._owner_failure_counts(error, name+'Failed')
                raise
            self.add(name+'Completed')
            return result

    def read_json_text(self, path, *, encoding=None):
        """Preserve Path.read_text + json.loads, including BOM/newline policy.

        Raw returned file bytes and UTF-8 size of decoded parser input are
        separate overlapping units. JSON semantics remain those of json.loads,
        not the stricter receipt reader. Decode/open failures never start a parse.
        """
        with self._read_failure_scope():
            self.add('textReadOperationsAttempted')
            try:
                with self.open_read(path) as raw:
                    stream, failure = None, None
                    try:
                        stream = io.TextIOWrapper(raw, encoding=encoding)
                        text = stream.read()
                    except BaseException as error:
                        failure = error
                        raise
                    finally:
                        if stream is not None:
                            try: stream.close()
                            except BaseException as error:
                                if failure is None: raise
                                _json_failure(failure, error, 'Text read close failed; secondary details omitted.')
            except BaseException as error:
                self._owner_failure_counts(error, 'textReadOperationsFailed', 'failedTextReadBytesUnknown')
                raise
            self.add('textReadOperationsCompleted')
            self.add('jsonParseAttempts')
            self.add('jsonInputBytes', len(text.encode('utf-8')))
            try:
                value = json.loads(text)
            except BaseException as error:
                self._owner_failure_counts(error, 'jsonParseFailed')
                raise
            self.add('jsonParseCompleted')
            return value

    def observe_file(self, path, missing_ok=True):
        try:
            key = os.path.normcase(os.path.abspath(path))
            info = Path(path).stat(follow_symlinks=False)
            if not stat.S_ISREG(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 1024:
                raise ValueError('Nonregular observed file')
            size = info.st_size
        except FileNotFoundError:
            if not missing_ok:
                self.add('fileLengthObservationFailures')
                return None
            size = 0
        except Exception:
            self.add('fileLengthObservationFailures')
            return None
        self._sizes[key] = size
        current = sum(self._sizes.values())
        self.values['sampledTrackedFileBytes'] = current
        self.values['sampledPeakTrackedFileBytes'] = max(self.values['sampledPeakTrackedFileBytes'], current)
        self.add('fileLengthObservations')
        return size

    def copyfile(self, source, destination):
        with self._owner_failure_scope():
            self.add('fileCopyOperationsAttempted')
            self.observe_file(destination)
            copied, failure = False, None
            try:
                result = shutil.copyfile(source, destination)
                copied = True
            except BaseException as error:
                failure = error
                self._owner_failure_counts(error, 'failedFileCopyBytesUnknown')
            finally:
                counter = 'fileCopyLogicalBytes.' if copied else None
                failure = self._final_file_observation(destination, failure, counter, missing_ok=not copied)
            if failure is not None:
                self._owner_failure_counts(failure, 'fileCopyOperationsFailed')
                raise failure
            self.add('fileCopyOperationsCompleted')
            return result

    def write_json(self, path, value):
        """Preserve the owner's exclusive text open, json.dump, flush and fsync.

        Successful TextIO.write returns accepted characters, not durable bytes.
        Count their UTF-8 size separately; any failed call has unknown progress.
        """
        owner = self
        previous_failure = self._json_failed
        self._json_failed = True
        self.add('jsonWriteOperationsAttempted')
        self.observe_file(path)
        class Text:
            def __init__(self, stream): self.stream = stream
            def write(self, text):
                owner.add('textWriteCallsAttempted')
                try:
                    count = self.stream.write(text)
                except BaseException as error:
                    owner._json_failure_counts(error, 'textWriteCallsFailed', 'failedTextWriteBytesUnknown')
                    raise
                owner.add('textAcceptedUtf8Bytes.'+role(path), len(text[:count].encode('utf-8')))
                owner.add('textWriteCallsCompleted')
                return count
        stream = failure = None
        try:
            stream = Path(path).open('x', encoding='utf-8', newline='\n')
            json.dump(value, Text(stream), indent=2)
            self.add('textFlushesAttempted')
            stream.flush()
            self.add('textFlushesCompleted')
            self.add('fileSyncsAttempted')
            os.fsync(stream.fileno())
            self.add('fileSyncsCompleted')
        except BaseException as error:
            failure = error
        finally:
            if stream is not None: failure = self._close_json_output(stream, failure)
            try: self.observe_file(path)
            except BaseException as error:
                failure = _json_failure(failure, error, 'JSON file observation failed; secondary details omitted.')
        if failure is None: self.add('jsonWriteOperationsCompleted')
        else: self._json_failure_counts(failure, 'jsonWriteOperationsFailed')
        if failure is not None: raise failure
        self._json_failed = previous_failure

    @contextmanager
    def child_log(self, path):
        """Observe only a log successfully created by this run, after cleanup.

        A returned timeout is still a completed wrapper scope. No drained-job or
        successful-child claim is made here; those belong to process receipts.
        Failed setup must never count a pre-existing log as this child's output.
        """
        with self._owner_failure_scope():
            owner = self
            class Log:
                created = False
                def opened(self):
                    self.created = True
                    owner.add('childLogFilesCreated')
            log = Log()
            self.add('childLogScopesAttempted')
            failure = None
            try:
                yield log
            except BaseException as error:
                failure = error
            finally:
                if log.created:
                    failure = self._final_file_observation(path, failure, 'childLogFinalObservedBytes.', missing_ok=False)
            if failure is not None:
                self._owner_failure_counts(failure, 'childLogScopesFailed')
                raise failure
            self.add('childLogScopesCompleted')


def seal_new(path, receipt):
    """Caller accounts for this write in its enclosing phase; no circular self-charge."""
    raw = canonical(receipt)
    with Path(path).open('xb') as stream:
        stream.write(raw)
        stream.flush()
        import os
        os.fsync(stream.fileno())
    return hashlib.sha256(raw).hexdigest()


def verify_counter_receipt(path, expected_pin, phase, request_pin, producer_pin, work=None):
    raw = Path(path).read_bytes() if work is None else work.read_bytes(path)
    return counter_receipt(raw, expected_pin, phase, request_pin, producer_pin, work)


def counter_receipt(raw, expected_pin, phase, request_pin, producer_pin, work=None):
    require(hashlib.sha256(raw).hexdigest() == pin(expected_pin), 'Changed work receipt')
    if work is None: receipt = strict(raw)
    else:
        with io.BytesIO(raw) as stream: receipt = work.parse(stream, strict_json)
    require(type(receipt) is dict, 'Invalid work receipt')
    phase_pending = receipt.get('version') == NATIVE_PHASE_PENDING_RECEIPT_VERSION
    leased = receipt.get('version') == NATIVE_LEASE_RECEIPT_VERSION
    family_pending = leased or receipt.get('version') == NATIVE_FAMILY_PENDING_RECEIPT_VERSION
    pending = family_pending or phase_pending or receipt.get('version') == NATIVE_PENDING_RECEIPT_VERSION
    fields = {'version','phase','requestSha256','producerSha256','outcome','coverage','counters','wholeProcessCoverage','usableForAdmission'}
    if pending: fields.update(('bindingSha256','pendingStorage'))
    if leased: fields.add('nativeLeases')
    require(set(receipt) == fields, 'Unknown receipt fields')
    require(receipt['version'] in (VERSION,NATIVE_PENDING_RECEIPT_VERSION,NATIVE_PHASE_PENDING_RECEIPT_VERSION,NATIVE_FAMILY_PENDING_RECEIPT_VERSION,NATIVE_LEASE_RECEIPT_VERSION) and receipt['phase'] == phase and receipt['requestSha256'] == pin(request_pin)
            and receipt['producerSha256'] == pin(producer_pin), 'Wrong producer/request/phase binding')
    require(receipt['outcome'] in ('Complete','Failed') and receipt['coverage'] == 'InstrumentedOperationsOnly'
            and receipt['wholeProcessCoverage'] is False and receipt['usableForAdmission'] is False, 'Unsupported coverage claim')
    require(isinstance(receipt['counters'], dict) and all(type(v) is int and v >= 0 for v in receipt['counters'].values()),
            'Invalid retained counters')
    if pending:
        require(phase in ('native','nativeAudit','publication'), 'Native pending receipt has a non-native phase')
        pin(receipt['bindingSha256'])
        budget=pending_families_observation(receipt['pendingStorage']) if family_pending else native_pending_observation(receipt['pendingStorage'],allow_empty=phase_pending)
        if family_pending: require(receipt['pendingStorage']['phase']==phase, 'Changed pending family phase')
        if phase_pending:
            require(phase!='native' or budget['files'], 'Native phase requires pending members')
            require(phase=='native' or not budget['files'], 'Audit/publication phases prohibit pending writers')
        require(receipt['outcome']!='Complete' or receipt['pendingStorage']['outcome']=='Complete', 'Failed pending scope cannot complete')
    if leased:
        native_lease_observation(receipt['nativeLeases'])
        require(receipt['nativeLeases']['phase']==phase and receipt['nativeLeases']['studyRoot']==receipt['pendingStorage']['studyRoot'],
                'Changed native lease phase/root')
        require(receipt['outcome']!='Complete' or receipt['nativeLeases']['outcome']=='Complete', 'Failed native leases cannot complete')
    return receipt


def unlinked(path):
    for parent in (path, *path.parents):
        try: info = parent.lstat()
        except FileNotFoundError: continue
        require(not stat.S_ISLNK(info.st_mode) and not getattr(info, 'st_file_attributes', 0) & 1024,
                'Linked worker path')


def outside_study(path, root):
    path = Path(path)
    require(path.is_absolute(), 'Work paths must be absolute')
    path = Path(os.path.abspath(path))
    require(not path.is_relative_to(root), 'Work sidecars must be outside the study archive')
    unlinked(path)
    return path


PUBLICATION_ERROR_TYPE = 'ExceptionDetailsOmitted'
PUBLICATION_ERROR_MESSAGE = 'Publication operation failed; exception details omitted.'


def _publish_unobserved(stream, receipt, write_error):
    """Persist a reserved legacy observation while preserving write/close errors."""
    failure = None
    try:
        raw = canonical(receipt())
        offset = 0
        while offset < len(raw):
            count = stream.write(raw[offset:])
            require(type(count) is int and 0 < count <= len(raw)-offset, write_error)
            offset += count
        stream.flush()
        os.fsync(stream.fileno())
    except BaseException as error:
        failure = error
        raise
    finally:
        try: stream.close()
        except BaseException as error:
            if failure is None: raise
            _json_failure(failure, error, 'Unobserved receipt close failed; secondary details omitted.')


class ReceiptPublication:
    """Observe one receipt's application calls, including reservation and close.

    Accepted binary writes are not durable or physical disk bytes. A failed
    write has unknown progress. The later observation's own I/O is excluded.
    """
    def __init__(self, binding_pin, phase, request_pin, producer_pin, *, sidecars=None, sidecar_role='receipt'):
        self.binding, self.phase = pin(binding_pin), phase
        self.request, self.producer = pin(request_pin), pin(producer_pin)
        self.counts, self.errors = Counter(), []
        self.stream = self.receipt_pin = self.serialized_bytes = None
        self.sidecars, self.sidecar_role = sidecars, sidecar_role
        self.failed = False
        self._open_attempted = self._publish_attempted = self._close_attempted = False

    def _record_failure(self, operation, original):
        self.failed = True
        try: self.counts[operation+'Failed'] += 1
        except BaseException as error:
            _json_failure(original, error, 'Publication failure accounting failed; secondary details omitted.')
        try:
            self.errors.append(dict(operation=operation, type=PUBLICATION_ERROR_TYPE, message=PUBLICATION_ERROR_MESSAGE))
        except BaseException as error:
            _json_failure(original, error, 'Publication error recording failed; secondary details omitted.')

    def call(self, operation, action):
        try:
            self.counts[operation+'Attempted'] += 1
            try: result = action()
            except BaseException as error:
                self._record_failure(operation, error)
                raise
            self.counts[operation+'Completed'] += 1
            return result
        except BaseException:
            self.failed = True
            raise

    def _close(self, original=None):
        if self.stream is None or self._close_attempted: return original
        self._close_attempted = True
        failure = original
        try: self.counts['closeAttempted'] += 1
        except BaseException as error:
            failure = _json_failure(failure, error, 'Publication close accounting failed; secondary details omitted.')
        try: self.stream.close()
        except BaseException as error:
            failure = _json_failure(failure, error, 'Work receipt close failed; secondary details omitted.')
            self._record_failure('close', failure)
        else:
            try: self.counts['closeCompleted'] += 1
            except BaseException as error:
                failure = _json_failure(failure, error, 'Publication close accounting failed; secondary details omitted.')
        if failure is not None: self.failed = True
        return failure

    def open(self, path):
        def acquire():
            self.stream = Path(path).open('xb') if self.sidecars is None else self.sidecars.open(self.sidecar_role,path)
            return self.stream
        try:
            require(not self.failed and not self._open_attempted, 'Publication reservation cannot be reused')
            self._open_attempted = True
            return self.call('open', acquire)
        except BaseException as original:
            self.failed = True
            self._close(original)
            raise

    def publish(self, receipt):
        failure = None
        try:
            require(not self.failed and self.stream is not None and not self._publish_attempted and not self._close_attempted,
                    'Publication writer is unavailable')
            self._publish_attempted = True
            raw = self.call('serialize', lambda:canonical(receipt()))
            self.serialized_bytes, self.receipt_pin = len(raw), hashlib.sha256(raw).hexdigest()
            offset = 0
            while offset < len(raw):
                def write():
                    count = self.stream.write(raw[offset:])
                    require(type(count) is int and 0 < count <= len(raw)-offset, 'Incomplete worker receipt write')
                    return count
                try: count = self.call('write', write)
                except BaseException as original:
                    try: self.counts['failedWriteBytesUnknown'] += 1
                    except BaseException as error:
                        _json_failure(original, error, 'Publication failure accounting failed; secondary details omitted.')
                    raise
                self.counts['acceptedWriteBytes'] += count
                if count < len(raw)-offset: self.counts['shortWrites'] += 1
                offset += count
            self.call('flush', self.stream.flush)
            self.call('sync', lambda:os.fsync(self.stream.fileno()))
        except BaseException as error:
            failure = error
            self.failed = True
        finally:
            failure = self._close(failure)
        if failure is not None: raise failure

    def observation(self):
        return dict(version=WORKER_PUBLICATION_VERSION, bindingSha256=self.binding,
            phase=self.phase, requestSha256=self.request, producerSha256=self.producer,
            outcome='Complete' if not self.failed and self._publish_attempted and self._close_attempted and not self.errors and self.counts['closeCompleted']==1 else 'Failed',
            serializedReceiptSha256=self.receipt_pin, serializedReceiptBytes=self.serialized_bytes,
            counters=dict(sorted(self.counts.items())), errors=list(self.errors),
            coverage='ReceiptPublicationApplicationCalls', boundary='AfterReceiptPublicationAttempt',
            observationPersistenceExcluded=True, wholeProcessCoverage=False, usableForAdmission=False)


class PublicationObservationPersistence(ReceiptPublication):
    """Publication-observation calls; this terminal receipt's own I/O is excluded."""
    def observation(self):
        value = super().observation()
        value.update(version=WORKER_PERSISTENCE_VERSION,
            coverage='PublicationObservationPersistenceApplicationCalls',
            boundary='AfterPublicationObservationAttempt', terminalObservationPersistenceExcluded=True)
        value['serializedObservationSha256'] = value.pop('serializedReceiptSha256')
        value['serializedObservationBytes'] = value.pop('serializedReceiptBytes')
        value.pop('observationPersistenceExcluded')
        return value


@contextmanager
def publication_persistence_scope(path, binding_pin, phase, request_pin, producer_pin, *, sidecars=None):
    if path is None:
        yield None
        return
    # Reserve the terminal receipt first. Its own calls are deliberately not
    # reported recursively; the enclosing job can observe their kernel totals.
    writer = ReceiptPublication(binding_pin, phase, request_pin, producer_pin, sidecars=sidecars, sidecar_role='persistence')
    persistence = PublicationObservationPersistence(binding_pin, phase, request_pin, producer_pin, sidecars=sidecars, sidecar_role='publication')
    writer.open(path)
    original = None
    try:
        yield persistence
    except BaseException as error:
        original = error
        raise
    finally:
        try:
            writer.publish(persistence.observation)
        except BaseException as error:
            if original is None: raise
            _json_failure(original, error, 'Terminal publication persistence failed; secondary details omitted.')


@contextmanager
def receipt_publication_scope(path, binding_pin, phase, request_pin, producer_pin, *, persistence_path=None, sidecars=None):
    if path is None:
        require(persistence_path is None, 'Persistence observation requires publication')
        yield None
        return
    require(sidecars is None or persistence_path is not None, 'Bounded sidecars require publication persistence')
    with publication_persistence_scope(persistence_path, binding_pin, phase, request_pin, producer_pin, sidecars=sidecars) as persistence:
        publication = ReceiptPublication(binding_pin, phase, request_pin, producer_pin, sidecars=sidecars)
        stream = Path(path).open('xb') if persistence is None else persistence.open(path)
        original = None
        try:
            yield publication
        except BaseException as error:
            original = error
            raise
        finally:
            try:
                if persistence is not None:
                    persistence.publish(publication.observation)
                else:
                    _publish_unobserved(stream, publication.observation, 'Incomplete publication observation write')
            except BaseException as error:
                if original is None: raise
                _json_failure(original, error, 'Publication observation persistence failed; secondary details omitted.')


def worker_publication(raw, binding_pin, phase, request_pin, producer_pin):
    """Validate the separately retained observation; never merge its counters."""
    value = strict(raw)
    require(set(value) == {'version','bindingSha256','phase','requestSha256','producerSha256','outcome',
        'serializedReceiptSha256','serializedReceiptBytes','counters','errors','coverage','boundary',
        'observationPersistenceExcluded','wholeProcessCoverage','usableForAdmission'}, 'Unknown publication fields')
    require(value['version']==WORKER_PUBLICATION_VERSION and value['bindingSha256']==pin(binding_pin)
        and value['phase']==phase and value['requestSha256']==pin(request_pin)
        and value['producerSha256']==pin(producer_pin), 'Wrong publication binding')
    require(value['coverage']=='ReceiptPublicationApplicationCalls' and value['boundary']=='AfterReceiptPublicationAttempt'
        and value['observationPersistenceExcluded'] is True and value['wholeProcessCoverage'] is False
        and value['usableForAdmission'] is False, 'Unsupported publication coverage')
    counts, errors = value['counters'], value['errors']
    operations = ('open','serialize','write','flush','sync','close')
    allowed = {op+suffix for op in operations for suffix in ('Attempted','Completed','Failed')}
    allowed.update(('acceptedWriteBytes','shortWrites','failedWriteBytesUnknown'))
    require(isinstance(counts,dict) and set(counts)<=allowed
        and all(type(v) is int and v>=0 for v in counts.values()), 'Invalid publication counters')
    require(isinstance(errors,list) and all(isinstance(e,dict) and set(e)=={'operation','type','message'}
        and e['operation'] in operations and isinstance(e['type'],str) and bool(e['type'])
        and isinstance(e['message'],str) for e in errors), 'Invalid publication errors')
    for op in operations:
        require(counts.get(op+'Attempted',0)==counts.get(op+'Completed',0)+counts.get(op+'Failed',0),
                'Incomplete publication operation')
        require(counts.get(op+'Failed',0)==sum(e['operation']==op for e in errors), 'Missing publication error')
        if op!='write': require(counts.get(op+'Attempted',0)<=1, 'Repeated publication operation')
    require(counts.get('openAttempted',0)==1, 'Missing receipt reservation')
    opened = counts.get('openCompleted',0)
    require(counts.get('closeAttempted',0)==opened and counts.get('serializeAttempted',0)==opened,
            'Missing receipt serialization/close')
    require(counts.get('failedWriteBytesUnknown',0)==counts.get('writeFailed',0)
        and counts.get('writeFailed',0)<=1 and counts.get('shortWrites',0)<=counts.get('writeCompleted',0)
        and counts.get('acceptedWriteBytes',0)>=counts.get('writeCompleted',0)
        and (counts.get('writeCompleted',0)>0 or counts.get('acceptedWriteBytes',0)==0), 'Invalid publication write progress')
    size, digest = value['serializedReceiptBytes'], value['serializedReceiptSha256']
    serialized = counts.get('serializeCompleted',0)
    if serialized:
        pin(digest)
        require(type(size) is int and size>0 and counts.get('acceptedWriteBytes',0)<=size
            and counts.get('writeAttempted',0)>=1, 'Invalid serialized receipt bytes')
    else:
        require(size is None and digest is None and all(counts.get(k,0)==0 for k in
            ('writeAttempted','acceptedWriteBytes','shortWrites','failedWriteBytesUnknown','flushAttempted','syncAttempted')),
            'Unserialized receipt has publication work')
    require(counts.get('flushAttempted',0)==int(bool(serialized and not counts.get('writeFailed',0)))
        and counts.get('syncAttempted',0)==counts.get('flushCompleted',0), 'Invalid publication ordering')
    if serialized and not counts.get('writeFailed',0):
        require(counts.get('acceptedWriteBytes',0)==size, 'Incomplete accepted receipt bytes')
    require(value['outcome']==('Failed' if errors else 'Complete'), 'Invalid publication outcome')
    if not errors:
        require(all(counts.get(op+'Completed',0)==1 for op in operations if op!='write')
            and counts.get('writeCompleted',0)>0, 'Incomplete successful publication')
    return value


def worker_publication_persistence(raw, binding_pin, phase, request_pin, producer_pin):
    """Reuse the checked call-order contract with explicitly different byte scope."""
    value = strict(raw)
    require(value.get('version') == WORKER_PERSISTENCE_VERSION
        and value.get('coverage') == 'PublicationObservationPersistenceApplicationCalls'
        and value.get('boundary') == 'AfterPublicationObservationAttempt'
        and value.get('terminalObservationPersistenceExcluded') is True,
        'Unsupported publication persistence coverage')
    require('serializedObservationSha256' in value and 'serializedObservationBytes' in value
        and not {'serializedReceiptSha256','serializedReceiptBytes','observationPersistenceExcluded'} & set(value),
        'Invalid publication persistence fields')
    checked = dict(value, version=WORKER_PUBLICATION_VERSION, coverage='ReceiptPublicationApplicationCalls',
                   boundary='AfterReceiptPublicationAttempt', observationPersistenceExcluded=True)
    checked['serializedReceiptSha256'] = checked.pop('serializedObservationSha256')
    checked['serializedReceiptBytes'] = checked.pop('serializedObservationBytes')
    checked.pop('terminalObservationPersistenceExcluded')
    worker_publication(canonical(checked), binding_pin, phase, request_pin, producer_pin)
    return value


@contextmanager
def worker_receipt(root, phase, binding_path, binding_pin, producer_path):
    """Authenticate an explicit opt-in binding; retain success or partial failure.

    Binding reads/parsing and both authentication passes are counted. Rejection
    before receipt reservation still creates no receipt. This receipt's own
    serialization/flush/close remain excluded from its own counters. A v2 binding
    retains these application calls separately after close, with its own
    persistence explicitly excluded. An opt-in v3 binding separately observes
    that persistence through close; its terminal receipt excludes its own I/O.
    Legacy v1/v2 publication contracts remain unchanged.
    No admission rule, process ownership rule or archive inventory is changed.
    """
    root = Path(os.path.abspath(root))
    work = Counters()
    binding_path = outside_study(binding_path, root)
    require(binding_path.stat().st_size <= 16384, 'Invalid worker binding size')
    raw = work.read_bytes(binding_path)
    require(hashlib.sha256(raw).hexdigest() == pin(binding_pin), 'Changed worker binding')
    with io.BytesIO(raw) as source: binding = work.parse(source, strict_json)
    bounded = binding.get('version') == WORKER_SIDECAR_BINDING_VERSION
    persistence_enabled = binding.get('version') in (WORKER_PERSISTENCE_BINDING_VERSION, WORKER_SIDECAR_BINDING_VERSION)
    publication_enabled = binding.get('version') in (WORKER_PUBLICATION_BINDING_VERSION, WORKER_PERSISTENCE_BINDING_VERSION, WORKER_SIDECAR_BINDING_VERSION)
    fields = {'version', 'phase', 'studyRoot', 'requestSha256', 'producerSha256', 'accountingModuleSha256', 'receiptPath'}
    if persistence_enabled: fields.add('publicationPersistencePath')
    if bounded: fields.add('sidecarByteLimits')
    require(set(binding) == fields | ({'publicationPath'} if publication_enabled else set()), 'Unknown worker binding fields')
    require(binding['version'] in (WORKER_BINDING_VERSION, WORKER_PUBLICATION_BINDING_VERSION, WORKER_PERSISTENCE_BINDING_VERSION, WORKER_SIDECAR_BINDING_VERSION)
            and phase in PHASES and binding['phase'] == phase
            and Path(binding['studyRoot']).is_absolute() and Path(os.path.abspath(binding['studyRoot'])) == root,
            'Wrong worker phase/root')
    request_pin, producer_pin, module_pin = (pin(binding[key]) for key in
                                           ('requestSha256', 'producerSha256', 'accountingModuleSha256'))
    receipt_path = outside_study(binding['receiptPath'], root)
    publication_path = outside_study(binding['publicationPath'], root) if publication_enabled else None
    require(publication_path != receipt_path, 'Receipt and publication paths overlap')
    persistence_path = outside_study(binding['publicationPersistencePath'], root) if persistence_enabled else None
    require(persistence_path is None or persistence_path not in (receipt_path, publication_path), 'Persistence paths overlap')
    def authenticate():
        work.add('workerAuthenticationsAttempted')
        try:
            unlinked(root/'request.json')
            require(work.sha(root/'request.json') == request_pin and work.sha(producer_path) == producer_pin
                    and work.sha(__file__) == module_pin, 'Changed worker request/producer/module')
        except BaseException:
            work.add('workerAuthenticationsFailed')
            raise
        work.add('workerAuthenticationsCompleted')
    limits = sidecar_limits(binding['sidecarByteLimits']) if bounded else None
    authenticate()
    with (SidecarWrites(limits) if bounded else nullcontext()) as sidecars, receipt_publication_scope(
            publication_path, binding_pin, phase, request_pin, producer_pin,
            persistence_path=persistence_path, sidecars=sidecars) as publication:
        stream = receipt_path.open('xb') if publication is None else publication.open(receipt_path)
        original, succeeded = None, False
        try:
            yield work
            require(not work._json_failed and not work.values.get('jsonOutputOperationsFailed', 0),
                    'Worker caught a failed JSON output operation')
            require(not work._read_failed, 'Worker caught a failed read operation')
            authenticate()
            succeeded = True
        except BaseException as error:
            original = error
            raise
        finally:
            try:
                if publication is not None:
                    publication.publish(lambda:work.receipt(phase, request_pin, producer_pin, succeeded))
                else:
                    _publish_unobserved(stream, lambda:work.receipt(phase, request_pin, producer_pin, succeeded), 'Incomplete worker receipt write')
            except BaseException as publication_error:
                if original is None: raise
                _json_failure(original, publication_error, 'Work receipt publication failed; secondary details omitted.')


class ManagedStorage:
    """Exact lifetime accounting for files exclusively created through this instance.

    The new private directory and checked inventory delimit coverage. External files,
    changes and live writes reject a final snapshot; this is not a machine-wide peak.
    """
    def __init__(self, root, work=None):
        self.work = work
        self.root = Path(root).absolute()
        for parent in (self.root, *self.root.parents):
            if parent.exists():
                require(not parent.is_symlink() and not getattr(parent.stat(), 'st_file_attributes', 0) & 1024,
                        'Linked storage scope')
        self.root.mkdir()
        self.files, self.live = {}, set()
        self.failed_operations = 0
        self.peak = self.scratch_peak = self.written = self.deleted = 0

    def path(self, name):
        require(isinstance(name,str) and name and '/' not in name and '\\' not in name and ':' not in name
                and name not in ('.','..') and name == name.rstrip(' .'), 'Unsafe managed file name')
        path = self.root/name
        require(not path.is_symlink(), 'Linked managed file')
        return path

    def create(self, name, kind):
        require(kind in ('retained','scratch') and name.casefold() not in {n.casefold() for n in self.files}, 'Repeated file or invalid kind')
        stream = self.path(name).open('xb', buffering=0)
        self.files[name] = dict(kind=kind, size=0)
        self.live.add(name)
        owner = self
        def call(boundary, action):
            if owner.work is not None: owner.work.add(boundary+'Attempted')
            try: result = action()
            except BaseException:
                owner.failed_operations += 1
                if owner.work is not None: owner.work.add(boundary+'Failed')
                raise
            if owner.work is not None: owner.work.add(boundary+'Completed')
            return result
        class Writer:
            def write(self, data):
                try: count = call('managedWriteCalls', lambda: stream.write(data))
                except BaseException:
                    if owner.work is not None: owner.work.add('failedManagedWriteBytesUnknown')
                    raise
                if owner.work is not None: owner.work.add('applicationWriteBytes.'+role(name), count)
                owner.files[name]['size'] += count
                owner.written += count
                owner.peak = max(owner.peak, sum(f['size'] for f in owner.files.values()))
                owner.scratch_peak = max(owner.scratch_peak, sum(f['size'] for f in owner.files.values() if f['kind']=='scratch'))
                return count
            def flush(self):
                call('managedFlushes', stream.flush)
                call('managedFileSyncs', lambda: os.fsync(stream.fileno()))
            def close(self):
                if not stream.closed:
                    call('managedCloses', stream.close)
                    owner.live.remove(name)
            def __enter__(self): return self
            def __exit__(self, *_): self.close()
        return Writer()

    def delete(self, name):
        require(name in self.files and name not in self.live, 'Unknown or open managed file')
        require(self.path(name).stat().st_size == self.files[name]['size'], 'Changed managed file')
        self.path(name).unlink()
        self.deleted += self.files.pop(name)['size']

    def snapshot(self):
        require(not self.live, 'Unclosed managed file')
        require({p.name for p in self.root.iterdir()} == set(self.files), 'Untracked storage member')
        for name, info in self.files.items():
            path = self.path(name)
            require(path.is_file() and not getattr(path.stat(), 'st_file_attributes', 0) & 1024
                    and path.stat().st_size == info['size'], 'Changed managed file')
        return dict(coverage='ExclusiveManagedDirectory', currentRetainedBytes=sum(f['size'] for f in self.files.values() if f['kind']=='retained'),
                    currentScratchBytes=sum(f['size'] for f in self.files.values() if f['kind']=='scratch'),
                    peakRetainedPlusScratchBytes=self.peak, peakScratchBytes=self.scratch_peak,
                    totalWrittenBytes=self.written, deletedBytes=self.deleted, wholeProcessCoverage=False)


class OwnerLedger:
    """Nonoverlapping enclosing intervals, including work after child closeout.

    Finish only after the last process/cleanup has returned. Its own receipt write
    belongs to an outer accounting boundary. Missing child/IO coverage stays unknown.
    """
    def __init__(self, request_sha256, producer_sha256, clock=time.monotonic, work=None):
        self.work = work
        self.request, self.producer, self.clock = pin(request_sha256), pin(producer_sha256), clock
        self.phases, self.workers, self.processes = [], [], []
        self.started = self.last = self.tick()
        self.finished = False

    def tick(self):
        value = self.clock()
        require(type(value) in (int,float) and math.isfinite(value), 'Invalid monotonic clock')
        return value

    def begin(self, phase):
        require(not self.finished and len(self.phases) < len(PHASES) and phase == PHASES[len(self.phases)], 'Invalid phase order')
        now = self.started if not self.phases else self.tick()
        require(now >= self.last, 'Clock moved backwards')
        if self.phases: self.phases[-1]['end'] = now-self.started
        self.phases.append(dict(phase=phase, start=now-self.started, end=None))
        self.last = now

    def attach(self, path, expected_pin, producer_pin):
        require(self.phases and not self.finished, 'No active phase')
        phase = self.phases[-1]['phase']
        receipt = verify_counter_receipt(path, expected_pin, phase, self.request, producer_pin, self.work)
        require(expected_pin not in {w['sha256'] for w in self.workers}, 'Repeated worker receipt')
        self.workers.append(dict(sha256=expected_pin, receipt=receipt))

    def observe_process(self, observation, producer_sha256):
        require(self.phases and not self.finished, 'No active phase')
        value = strict(canonical(observation))
        require(value['version']=='tower-owned-process-observation-v1' and value['includesHandleCleanup'] is True
                and value['usableForAdmission'] is False and type(value['jobDrained']) is bool, 'Invalid process observation')
        require(type(value['enclosingSeconds']) in (int,float) and math.isfinite(value['enclosingSeconds'])
                and value['enclosingSeconds'] >= 0, 'Invalid enclosing process duration')
        peaks = [value[k] for k in ('peakJobCommitBytes','ownerLifetimePeakCommitBytes','combinedCommitUpperBoundBytes')]
        if value['memoryCoverage']=='Unknown':
            require(all(v is None for v in peaks), 'Unknown memory must not be zero')
        else:
            require(value['memoryCoverage']=='KernelJobHighWaterPlusOwnerLifetimeHighWater'
                    and all(type(v) is int and v >= 0 for v in peaks) and sum(peaks[:2])==peaks[2], 'Invalid sum of memory peaks')
        # Legacy v1 process observations omit this independently versioned block.
        # Kernel transfer totals and application counters overlap; never add them.
        if 'jobIo' in value:
            io = value['jobIo']
            require(isinstance(io, dict) and set(io)=={'version','coverage','counters','activeProcessesAtQuery',
                'totalProcessesAtQuery','boundary','error','wholeProcessCoverage','usableForAdmission'}, 'Invalid job I/O fields')
            require(io['version']=='tower-owned-job-io-v1' and io['wholeProcessCoverage'] is False
                    and io['usableForAdmission'] is False, 'Invalid job I/O coverage claim')
            if io['coverage']=='Unknown':
                require(io['counters'] is None and io['activeProcessesAtQuery'] is None and io['totalProcessesAtQuery'] is None
                        and io['boundary']=='Unavailable' and isinstance(io['error'],str) and bool(io['error']),
                        'Unknown job I/O must not be zero')
            else:
                require(io['coverage']=='KernelJobLifetimeTotals' and value['jobDrained'] is True
                        and io['boundary']=='AfterConfirmedDrainBeforeHandleCleanup' and io['error'] is None
                        and type(io['activeProcessesAtQuery']) is int and io['activeProcessesAtQuery']==0
                        and type(io['totalProcessesAtQuery']) is int and 0 < io['totalProcessesAtQuery'] < 2**32,
                        'Invalid terminal job I/O state')
                names = {'readOperations','writeOperations','otherOperations','readTransferBytes','writeTransferBytes','otherTransferBytes'}
                require(isinstance(io['counters'],dict) and set(io['counters'])==names
                        and all(type(v) is int and 0 <= v < 2**64 for v in io['counters'].values()), 'Invalid kernel job I/O counters')
        if 'cleanup' in value:
            process_cleanup_observation(value['cleanup'])
            require(not value['cleanup']['errors'] or type(value['ownerError']) is str and bool(value['ownerError']),
                    'Cleanup failure requires owner error')
        if 'jobLimits' in value:
            job_limit_observation(value['jobLimits'])
            require(value['jobLimits']['verified'] or value['exitCode'] is None
                    and value.get('jobIo',{}).get('coverage') != 'KernelJobLifetimeTotals',
                    'Executed process has unverified job limits')
        if 'environment' in value:
            env=value['environment']
            require(type(env) is dict and set(env)=={'version','sha256','variableCount','utf16Bytes','inherited','scope',
                'filesystemConfinement','runtimeWritesBounded','wholeProcessCoverage','usableForAdmission'}, 'Invalid process environment fields')
            require(env['version']=='tower-owned-environment-v1' and env['scope']=='ProcessCreationBlockOnly'
                and all(env[k] is False for k in ('inherited','filesystemConfinement','runtimeWritesBounded','wholeProcessCoverage','usableForAdmission')),
                'Unsupported process environment coverage')
            pin(env['sha256'])
            require(type(env['variableCount']) is int and 0<=env['variableCount']<=128 and type(env['utf16Bytes']) is int
                and 4<=env['utf16Bytes']<=65534 and env['utf16Bytes']%2==0, 'Invalid process environment size')
        if 'logCapture' in value:
            log = value['logCapture']
            require(type(log) is dict and set(log)=={'version','maxBytes','acceptedBytes','readBytes','retainedBytes',
                'exceeded','pipeEof','captureComplete','error','scope','wholeProcessCoverage','usableForAdmission'},
                'Invalid log capture fields')
            require(log['version']=='tower-owned-log-capture-v1' and log['scope']=='MergedStdoutStderrThroughParentWriter'
                and log['wholeProcessCoverage'] is False and log['usableForAdmission'] is False, 'Invalid log capture scope')
            require(all(type(log[k]) is int and log[k]>=0 for k in ('maxBytes','acceptedBytes','readBytes'))
                and log['acceptedBytes'] <= log['maxBytes'] < 2**63 and log['acceptedBytes'] <= log['readBytes'],
                'Invalid log capture byte counts')
            require(log['retainedBytes'] is None or type(log['retainedBytes']) is int
                and log['retainedBytes']==log['acceptedBytes'], 'Invalid closed log length')
            require(all(type(log[k]) is bool for k in ('exceeded','pipeEof','captureComplete'))
                and (log['error'] is None or type(log['error']) is str and bool(log['error'])), 'Invalid log capture state')
            require(not log['exceeded'] or log['error'] is not None and log['acceptedBytes']==log['maxBytes']
                and log['readBytes']>log['acceptedBytes'], 'Invalid log overflow state')
            require(not log['captureComplete'] or log['pipeEof'] and not log['exceeded'] and log['error'] is None
                and log['retainedBytes']==log['acceptedBytes']==log['readBytes'], 'Invalid complete log capture')
            require(not (value['jobDrained'] and value['exitCode']==0 and value['timedOut'] is False
                         and value['ownerError'] is None) or log['captureComplete'], 'Successful process has incomplete log capture')
        self.processes.append(dict(phase=self.phases[-1]['phase'], producerSha256=pin(producer_sha256),
                                   observationSha256=hashlib.sha256(canonical(value)).hexdigest(), observation=value))

    def finish(self, succeeded):
        require(type(succeeded) is bool and not self.finished and self.phases, 'Invalid owner finish')
        require(not succeeded or len(self.phases)==4, 'Incomplete successful lifecycle')
        require(not succeeded or all(w['receipt']['outcome']=='Complete' for w in self.workers), 'Failed worker cannot complete')
        require(not succeeded or all(p['observation']['jobDrained'] and p['observation']['exitCode']==0
                and p['observation']['timedOut'] is False and p['observation']['ownerError'] is None for p in self.processes),
                'Failed or undrained process cannot complete')
        now = self.tick()
        require(now >= self.last, 'Clock moved backwards')
        self.phases[-1]['end'] = now-self.started
        self.finished = True
        return dict(version=OWNER_VERSION, requestSha256=self.request, producerSha256=self.producer,
                    outcome='Complete' if succeeded else 'Failed', phases=[dict(p) for p in self.phases],
                    enclosingSeconds=now-self.started, workers=list(self.workers),
                    processObservations=list(self.processes),
                    coverage='EnclosingIntervalsAndAttachedInstrumentedOperations',
                    missingCoverage=['wholeProcessIO','wholeProcessScratch','wholeProcessReconstruction','wholeOwnerMemoryLifetime'],
                    wholeProcessCoverage=False, usableForAdmission=False)


class RetainedOwner:
    """Explicit diagnostic owner with create-new receipts and cleanup before closeout.

    Each phase requires one bound counter receipt and at least one owned-process
    observation for a Complete outcome. Callbacks run inside the final interval,
    including on exceptions. This is opt-in and does not launch or admit studies.
    The terminal receipt and manifest writes remain excluded from that receipt.
    With a fresh OwnerFileCounters, final_observation captures owner operations
    after publication and cleanup; its own external persistence is excluded.
    """
    def __init__(self, root, request_sha256, producer_sha256, module_sha256, clock=time.monotonic, work=None,
                 *, storage_contract=None, protected_roots=()):
        require(work is None or isinstance(work, OwnerFileCounters) and not work.values, 'Use fresh owner file counters')
        self.work, self.final_observation = work, None
        self.ledger = OwnerLedger(request_sha256, producer_sha256, clock, work)
        require(self._sha(__file__) == pin(module_sha256), 'Unbound accounting module')
        storage_binding = {}
        if storage_contract is None:
            require(not protected_roots, 'Protected roots require a diagnostic storage contract')
            self.storage = ManagedStorage(root, work)
        else:
            import proposal_diagnostic_storage as diagnostic
            self.storage = diagnostic.DiagnosticStorage(root, storage_contract, work, protected_roots=protected_roots)
            storage_binding = dict(storageContractSha256=self.storage.contract_sha256,
                                   storageContract=diagnostic.declaration(storage_contract),
                                   storageModuleSha256=self._sha(diagnostic.__file__))
        self.cleanup, self.errors, self.members = ExitStack(), [], {}
        self.entered = self.closed = self.active = False
        self.phase_name = self.phase_producer = None
        self.manifest_sha256 = self.receipt = None
        self._seal('binding.json', dict(requestSha256=self.ledger.request, producerSha256=self.ledger.producer,
                                       moduleSha256=module_sha256, usableForAdmission=False, **storage_binding))

    def _read(self, path):
        return Path(path).read_bytes() if self.work is None else self.work.read_bytes(path)

    def _sha(self, path):
        return hashlib.sha256(Path(path).read_bytes()).hexdigest() if self.work is None else self.work.sha(path)

    def _seal(self, name, value):
        return self._seal_raw(name, canonical(value))

    def _seal_raw(self, name, raw):
        try:
            with self.storage.create(name, 'retained') as stream:
                offset = 0
                while offset < len(raw):
                    count = stream.write(raw[offset:])
                    require(type(count) is int and 0 < count <= len(raw)-offset, 'Incomplete receipt write')
                    offset += count
                stream.flush()
        except BaseException as error:
            self._error('retention/'+name, error)
            raise
        digest = hashlib.sha256(raw).hexdigest()
        self.members[name] = digest
        return digest

    def __enter__(self):
        require(not self.entered and not self.closed, 'Owner cannot be reused')
        self.entered = True
        return self

    def callback(self, function, *args, **kwargs):
        require(self.entered and not self.closed, 'Owner is not active')
        self.cleanup.callback(function, *args, **kwargs)

    def _error(self, boundary, error):
        self.errors.append(dict(boundary=boundary, type=type(error).__name__, message=str(error)))

    @contextmanager
    def phase(self, name, producer_sha256):
        require(self.entered and not self.closed and not self.active, 'No available owner phase')
        producer = pin(producer_sha256)
        self.ledger.begin(name)
        self.active, self.phase_name, self.phase_producer = True, name, producer
        try:
            yield self
        except BaseException as error:
            self._error(name, error)
            raise
        finally:
            self.active = False

    def attach(self, path, expected_pin):
        require(self.active and not self.closed, 'No active receipt phase')
        # Retain the verified bytes immediately; an external worker file can then
        # disappear during cleanup without losing its evidence.
        raw = self._read(path)
        counter_receipt(raw, expected_pin, self.phase_name, self.ledger.request, self.phase_producer, self.work)
        name = self.phase_name+'-work.json'
        require(name not in self.members, 'Repeated phase receipt')
        digest = self._seal_raw(name, raw)
        self.ledger.attach(self.storage.path(name), digest, self.phase_producer)

    def observe_process(self, observation, producer_sha256):
        require(self.active and not self.closed, 'No active process phase')
        self.ledger.observe_process(observation, producer_sha256)
        retained = self.ledger.processes[-1]
        self._seal(f'{self.phase_name}-process-{len(self.ledger.processes):02d}.json', retained)

    def run_worker(self, root, producer_path, accounting_module_path, receipt_path, invoke, *, publication_path=None,
                   publication_persistence_path=None, sidecar_byte_limits=None, pending_storage_budget=None, pending_binding_version=None,
                   pending_families_budget=None, native_lease_budget=None):
        """Bind and retain one explicitly invoked worker in the active phase.

        invoke(binding_path, binding_pin) must own/drain its child and supply a
        process observation through observe_process. The normal study launcher
        does not call this diagnostic integration. Failed invocations still
        retain any valid partial receipt and preserve the original exception.
        """
        require(self.active and not self.closed, 'No active worker phase')
        limits = sidecar_limits(sidecar_byte_limits) if sidecar_byte_limits is not None else None
        family_pending = pending_families_budget is not None
        require(native_lease_budget is None or family_pending, 'Native leases require the bounded pending family contract')
        require(not family_pending or pending_storage_budget is None and pending_binding_version is None,
                'Pending family and explicit declarations cannot be mixed')
        require(pending_binding_version is None or pending_storage_budget is not None and pending_binding_version in
            (WORKER_PENDING_BINDING_VERSION,WORKER_PHASE_PENDING_BINDING_VERSION), 'Invalid native pending binding version')
        phase_pending = pending_binding_version == WORKER_PHASE_PENDING_BINDING_VERSION
        require(pending_storage_budget is None or limits is not None and self.phase_name in ('native','nativeAudit','publication'),
                'Native pending storage requires a native phase and bounded sidecars')
        require(not family_pending or limits is not None and self.phase_name in NATIVE_PHASES,
                'Native pending families require a native phase and bounded sidecars')
        require(limits is None or publication_persistence_path is not None, 'Bounded sidecars require publication persistence')
        root = Path(os.path.abspath(root))
        receipt_path = outside_study(receipt_path, root)
        # ManagedStorage accepts only files it creates itself. Worker output is
        # external and copied into that scope only after authentication.
        require(not receipt_path.is_relative_to(self.storage.root), 'Worker output overlaps owner storage')
        require(not receipt_path.exists(), 'Existing worker receipt')
        if publication_path is not None:
            publication_path = outside_study(publication_path, root)
            require(publication_path != receipt_path and not publication_path.is_relative_to(self.storage.root),
                    'Publication output overlaps receipt/owner storage')
            require(not publication_path.exists(), 'Existing publication observation')
        if publication_persistence_path is not None:
            require(publication_path is not None, 'Persistence observation requires publication')
            publication_persistence_path = outside_study(publication_persistence_path, root)
            require(publication_persistence_path not in (receipt_path, publication_path)
                and not publication_persistence_path.is_relative_to(self.storage.root), 'Persistence output overlaps receipt/owner storage')
            require(not publication_persistence_path.exists(), 'Existing publication persistence observation')
        unlinked(root/'request.json')
        require(self._sha(root/'request.json') == self.ledger.request
                and self._sha(producer_path) == self.phase_producer,
                'Changed worker request/producer')
        name = self.phase_name+'-binding.json'
        binding_path = outside_study(self.storage.path(name), root)
        binding = dict(version=WORKER_BINDING_VERSION, phase=self.phase_name, studyRoot=str(root),
                       requestSha256=self.ledger.request, producerSha256=self.phase_producer,
                       accountingModuleSha256=self._sha(accounting_module_path),
                       receiptPath=str(receipt_path))
        if publication_path is not None:
            binding.update(version=WORKER_PUBLICATION_BINDING_VERSION, publicationPath=str(publication_path))
        if publication_persistence_path is not None:
            binding.update(version=WORKER_PERSISTENCE_BINDING_VERSION, publicationPersistencePath=str(publication_persistence_path))
        if limits is not None: binding.update(version=WORKER_SIDECAR_BINDING_VERSION, sidecarByteLimits=limits)
        pending = None
        if pending_storage_budget is not None:
            require(binding['accountingModuleSha256']==binding['producerSha256'], 'Invalid native accounting identity')
            protected=(self.storage.root,root/'request.json',Path(producer_path).absolute(),Path(accounting_module_path).absolute(),
                       receipt_path,publication_path,publication_persistence_path)
            pending=native_pending_budget(pending_storage_budget,protected_paths=protected,allow_empty=phase_pending)
            if phase_pending:pending=proposal_pending_budget(root,self.phase_name,pending)
            for file in pending['files']:
                unlinked(Path(file['path']));unlinked(Path(file['destination']))
            binding.update(version=pending_binding_version or WORKER_PENDING_BINDING_VERSION,pendingStorageBudget=pending)
            require(len(canonical(binding)) <= 16384, 'Native pending binding exceeds worker size limit')
        if family_pending:
            require(binding['accountingModuleSha256']==binding['producerSha256'], 'Invalid native accounting identity')
            protected=(self.storage.root,binding_path,Path(producer_path).absolute(),Path(accounting_module_path).absolute(),
                       receipt_path,publication_path,publication_persistence_path)
            pending=proposal_pending_families_budget(root,self.phase_name,pending_families_budget,protected_paths=protected)
            for file in pending_family_fixed(root,self.phase_name,pending):
                unlinked(Path(file['path']));unlinked(Path(file['destination']))
            binding.update(version=WORKER_FAMILY_PENDING_BINDING_VERSION,pendingFamiliesBudget=pending)
            require(len(canonical(binding)) <= 16384, 'Native pending binding exceeds worker size limit')
        leases = None
        if native_lease_budget is not None:
            leases=proposal_native_lease_budget(root,self.phase_name,native_lease_budget,protected_paths=protected)
            for entry in native_lease_paths(root,self.phase_name):unlinked(Path(entry['path']))
            binding.update(version=WORKER_NATIVE_LEASE_BINDING_VERSION,nativeLeaseBudget=leases)
            require(len(canonical(binding)) <= 16384, 'Native lease binding exceeds worker size limit')
        digest = self._seal(name, binding)
        original = None
        try:
            result = invoke(binding_path, digest)
            return result
        except BaseException as error:
            original = error
            self._error('worker/'+self.phase_name, error)
            raise
        finally:
            publication, persistence, retention_errors = None, None, []
            sidecar_bytes = 0
            def read_sidecar(path, role):
                nonlocal sidecar_bytes
                raw = self._read(path)
                if limits is not None:
                    sidecar_bytes += len(raw)
                    require(len(raw) <= limits[role] and sidecar_bytes <= limits['total'], 'Worker exceeded bound sidecar lengths')
                return raw
            if publication_persistence_path is not None:
                try:
                    observed = read_sidecar(publication_persistence_path, 'persistence')
                    persistence = worker_publication_persistence(observed, digest, self.phase_name, self.ledger.request, self.phase_producer)
                    self._seal_raw(self.phase_name+'-publication-persistence.json', observed)
                    require(original is not None or persistence['outcome']=='Complete', 'Publication observation persistence failed')
                except BaseException as error:
                    retention_errors.append(error)
            if publication_path is not None:
                try:
                    observed = read_sidecar(publication_path, 'publication')
                    if persistence is not None and persistence['outcome']=='Complete':
                        require(persistence['serializedObservationSha256']==hashlib.sha256(observed).hexdigest()
                            and persistence['serializedObservationBytes']==len(observed), 'Changed persisted publication observation')
                    publication = worker_publication(observed, digest, self.phase_name, self.ledger.request, self.phase_producer)
                    self._seal_raw(self.phase_name+'-receipt-publication.json', observed)
                    require(original is not None or publication['outcome']=='Complete', 'Worker receipt publication failed')
                except BaseException as error:
                    retention_errors.append(error)
            try:
                raw = read_sidecar(receipt_path, 'receipt')
                verified = counter_receipt(raw, hashlib.sha256(raw).hexdigest(), self.phase_name,
                                           self.ledger.request, self.phase_producer, self.work)
                require(verified['version']==(NATIVE_LEASE_RECEIPT_VERSION if leases is not None else VERSION if pending is None else NATIVE_FAMILY_PENDING_RECEIPT_VERSION if family_pending
                    else NATIVE_PHASE_PENDING_RECEIPT_VERSION if phase_pending else NATIVE_PENDING_RECEIPT_VERSION),
                        'Unexpected worker receipt version')
                if pending is not None:
                    observed=pending_families_observation(verified['pendingStorage']) if family_pending else native_pending_observation(verified['pendingStorage'],allow_empty=phase_pending)
                    require(verified['bindingSha256']==digest and observed==pending
                        and (not family_pending or verified['pendingStorage']['studyRoot']==str(root)),
                            'Changed native pending binding/declaration')
                if leases is not None:
                    require(native_lease_observation(verified['nativeLeases'])==leases, 'Changed native lease declaration')
                if publication is not None and publication['outcome']=='Complete':
                    require(publication['serializedReceiptSha256']==hashlib.sha256(raw).hexdigest()
                        and publication['serializedReceiptBytes']==len(raw), 'Changed published receipt')
                self.attach(receipt_path, hashlib.sha256(raw).hexdigest())
                require(original is not None or verified['outcome'] == 'Complete', 'Worker returned a failed receipt')
            except BaseException as error:
                retention_errors.append(error)
            for error in retention_errors:
                self._error('worker-retention/'+self.phase_name, error)
                if original is not None: original.add_note('Worker receipt retention failed: '+repr(error))
            if retention_errors and original is None:
                for error in retention_errors[1:]: retention_errors[0].add_note('Worker receipt retention failed: '+repr(error))
                raise retention_errors[0]

    def __exit__(self, error_type, error, traceback):
        require(self.entered and not self.closed, 'Owner cannot close twice')
        failure = error
        try:
            return self._close(error_type, error, traceback)
        except BaseException as close_error:
            failure = error or close_error
            raise
        finally:
            if self.work is not None:
                try: self._observe_final(failure)
                except BaseException as observation_error:
                    self.manifest_sha256 = None
                    if failure is None: raise
                    failure.add_note('Final owner observation failed: '+repr(observation_error))

    def _observe_final(self, failure):
        """Publish nothing here: the caller retains this observation externally.

        Revalidate every managed member after the terminal files have closed.
        Exact managed storage and sampled external file lengths stay separate.
        Unknown coverage must not turn into a successful zero-byte observation.
        """
        storage, observation_error = None, None
        try:
            storage = self.storage.snapshot()
            for name, digest in self.members.items():
                require(self._sha(self.storage.path(name)) == digest, 'Changed final owner member')
            now = self.ledger.tick()
            previous = self.receipt['ledger']['enclosingSeconds'] if self.receipt is not None else 0
            require(now >= self.ledger.started+previous, 'Clock moved backwards after publication')
        except BaseException as error:
            storage, now, previous, observation_error = None, None, None, error
            self.manifest_sha256 = None
        self.final_observation = dict(version='tower-proposal-owner-final-observation-v1',
            requestSha256=self.ledger.request, producerSha256=self.ledger.producer,
            manifestSha256=self.manifest_sha256,
            outcome='Complete' if failure is None and observation_error is None and self.manifest_sha256 is not None and not self.errors else 'Failed',
            enclosingSeconds=None if now is None else now-self.ledger.started,
            terminalPublicationSeconds=None if now is None else now-self.ledger.started-previous,
            ownerCounters=dict(sorted(self.work.values.items())), managedStorageAfterPublication=storage,
            receiptManifestPublicationObserved=self.manifest_sha256 is not None and storage is not None,
            observationError=None if observation_error is None else repr(observation_error),
            observationPersistenceExcluded=True, wholeProcessCoverage=False, usableForAdmission=False)
        if observation_error is not None: raise observation_error

    def _close(self, error_type, error, traceback):
        require(self.entered and not self.closed, 'Owner cannot close twice')
        self.closed = True
        if error is not None: self._error('owner', error)
        close_error = None
        try:
            self.cleanup.close()
        except BaseException as failure:
            self._error('cleanup', failure)
            close_error = failure
        if not self.ledger.phases: self.ledger.begin('native')
        storage = None
        try:
            require(not self.active, 'Unclosed owner phase')
            storage = self.storage.snapshot()
            # Check receipt identity as well as managed lengths before sealing.
            for name, digest in self.members.items():
                require(self._sha(self.storage.path(name)) == digest, 'Changed retained receipt')
            if error is None and close_error is None:
                require([w['receipt']['phase'] for w in self.ledger.workers] == list(PHASES), 'Missing phase counter receipts')
                require({p['phase'] for p in self.ledger.processes} == set(PHASES), 'Missing phase process observations')
                require(not self.errors, 'A phase previously failed')
                if self.work is not None:
                    require(self.storage.failed_operations == 0, 'Managed storage operation previously failed')
            receipt = self.ledger.finish(error is None and close_error is None)
        except BaseException as failure:
            self._error('closeout', failure)
            close_error = close_error or failure
            receipt = self.ledger.finish(False)
        self.receipt = dict(version='tower-proposal-retained-owner-v1', ledger=receipt, errors=self.errors,
                            managedStorageBeforeTerminalPublication=storage,
                            terminalPublicationExcluded=True, wholeProcessCoverage=False, usableForAdmission=False)
        try:
            self._seal('owner-work.json', self.receipt)
            # A changed or untracked member prevents a sealed package, even on a
            # failed run. Earlier durable receipts and the failure remain available.
            self.storage.snapshot()
            files = {name:self._sha(self.storage.path(name)) for name in self.storage.files}
            require(all(files[name] == digest for name, digest in self.members.items()), 'Changed retained receipt')
            self.manifest_sha256 = self._seal('files.json', files)
        except BaseException:
            if error is None: raise
            # Preserve the original failure; no manifest pin means unsealed.
        if error is None and close_error is not None: raise close_error
        return False
