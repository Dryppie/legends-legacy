"""Single owned launch of the prospective twelve-root proposal-affinity study. Never retries or prepares inputs.

Admission is separately charged; this launcher includes both saved-row audits and publication.
Only this launcher starts the native comparison, inside a hidden Windows Job assigned
before its process is resumed. Failure preserves output and unresolved reservations.
"""
import argparse
from contextlib import ExitStack, contextmanager
from contextvars import ContextVar
import ctypes
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
import sys
import threading
import time

FILE_WORK = ContextVar('proposal_owner_file_work', default=None)


@contextmanager
def file_accounting(work):
    """Opt-in diagnostic file boundaries; no CLI activation or admission change."""
    token = FILE_WORK.set(work)
    try:
        yield work
    finally:
        FILE_WORK.reset(token)


def copy_file(source, destination):
    work = FILE_WORK.get()
    return shutil.copyfile(source, destination) if work is None else work.copyfile(source, destination)


def read_json(path, *, encoding=None):
    work = FILE_WORK.get()
    return json.loads(Path(path).read_text(encoding=encoding)) if work is None else work.read_json_text(path, encoding=encoding)


def file_operation(name, action):
    work = FILE_WORK.get()
    return action() if work is None else work.operation(name, action)

VERSION = 'tower-proposal-affinity-comparison-v1'
CREATION_VERSION = 'tower-affinity-creation-comparison-v1'
SELECTOR_VERSION = 'tower-benchmark-tie-comparison-v1'
VALIDATION_VERSION = 'tower-benchmark-validation-comparison-v1'
PRESERVATION_VERSION = 'tower-affinity-preservation-comparison-v1'
ALLIED_VERSION = 'tower-affinity-allied-action-comparison-v1'
PLACEMENT_VERSION = 'tower-loadout-placement-comparison-v1'
NOMINATION_VERSION = 'tower-affinity-nomination-comparison-v1'
RESOURCE_V1 = 'tower-proposal-resource-envelope-v1'
RESOURCE_V2 = 'tower-proposal-resource-envelope-v2'
SECONDS, BYTES = 10800, 6442450944
CUMULATIVE_SECONDS, CUMULATIVE_BYTES = 10800, 6442450944
PRIOR_SECONDS, PRIOR_BYTES = 0, 0
NATIVE_SECONDS, NATIVE_BYTES = 9600, 5905580032
AUDIT_SECONDS, AUDIT_BYTES = 1200, 536870912


def require(ok, message):
    if not ok:
        raise ValueError(message)


def resources(q):
    version = q.get('resourceEnvelope')
    require(q.get('version') not in (ALLIED_VERSION, PLACEMENT_VERSION, NOMINATION_VERSION) or version == RESOURCE_V2,
        'This comparison requires the frozen v2 resource envelope')
    require(version in (None, RESOURCE_V1, RESOURCE_V2), 'Unknown proposal resource envelope')
    native, audit = (9000, 1800) if version == RESOURCE_V2 else (9600, 1200)
    return dict(version=version or RESOURCE_V1, nativeSeconds=native, auditSeconds=audit)


def audit_deadline(started, audit_started, limits):
    # One absolute deadline for both audits, publication and sealing; native
    # underspend does not enlarge the audit partition.
    return min(started+SECONDS-5, audit_started+limits['auditSeconds']-5)


def digest(path):
    work = FILE_WORK.get()
    if work is not None: return work.sha(path)
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def write(path, value):
    work = FILE_WORK.get()
    if work is not None: return work.write_json(path, value)
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2)
        stream.flush()
        os.fsync(stream.fileno())


def inventory(root):
    def scan():
        files = []
        for parent, dirs, names in os.walk(root, followlinks=False):
            for name in dirs + names:
                path = Path(parent)/name
                require(not file_operation('metadataSymlinkQuery', path.is_symlink)
                        and not file_operation('metadataJunctionQuery', path.is_junction), 'Linked output is forbidden')
            files.extend(Path(parent)/name for name in names)
        return sorted(files)
    # os.walk's existing error suppression is unchanged. Completion means this
    # traversal returned, not that inaccessible subtrees were observed.
    return file_operation('directoryInventory', scan)


def publication_inventory(root, protected):
    """One fresh post-audit pass verifies inputs and builds the final manifest.

    No earlier hash is reused as evidence of current contents. Checking the
    protected subset here also covers the subsequent result-copy operation.
    All workers have drained; this retains the existing exclusive-owner model,
    not a guarantee against concurrent external mutation after hashing.
    """
    files = {p.relative_to(root).as_posix(): digest(p) for p in inventory(root)}
    require(all(files.get(name) == pin for name, pin in protected.items()),
            'Audit inputs changed before publication')
    return files


def storage_bytes(root):
    """Sample live output, including temporary bytes during atomic publication.

    The native writer flushes <name>.pending and atomically renames it to <name>.
    Enumeration can see the old name immediately before that rename. Resolve only
    this known transition; missing ordinary files and other I/O failures still fail.
    Final manifest hashing remains strict and happens after all writers have exited.
    """
    sizes = {}
    for path in inventory(root):
        racing_lease = re.fullmatch(r'search/root-(?:0[1-9]|1[0-2])/(?:control|candidate)/racing\.writer\.lock',
                                   path.relative_to(root).as_posix()) is not None
        try:
            info = file_operation('metadataLstat', path.lstat)
        except FileNotFoundError:
            # Native arm evidence owns a zero-byte DeleteOnClose lease. Its
            # disappearance between enumeration and stat is a normal close.
            if racing_lease:
                continue
            if not path.name.endswith('.pending'):
                raise
            path = path.with_name(path.name[:-len('.pending')])
            info = file_operation('metadataLstat', path.lstat)
        require(stat.S_ISREG(info.st_mode) and not getattr(info, 'st_file_attributes', 0) & 0x400,
                'Linked or non-file output is forbidden')
        require(not racing_lease or info.st_size == 0, 'Native writer lease must be empty')
        # The same published path may also have been enumerated. Count it once,
        # retaining the larger observation if it grew between the two reads.
        sizes[path] = max(sizes.get(path, 0), info.st_size)
    return sum(sizes.values())


def validate_request(q):
    require(q.get('evidenceStorage') is None, 'Compressed evidence requires a separate resource protocol; the recovery gate is closed')
    require(q['version'] in (VERSION, CREATION_VERSION, SELECTOR_VERSION, VALIDATION_VERSION, PRESERVATION_VERSION, ALLIED_VERSION, PLACEMENT_VERSION, NOMINATION_VERSION), 'Changed proposal study version')
    resources(q)
    output, registry = Path(q['outputRoot']), Path(q['registryRoot'])
    require(output.is_absolute() and registry.is_absolute() and output.parent.resolve() == registry.resolve(),
            'Output must be directly beneath the complete registry')
    require(not output.exists(), 'No retry, resume or existing output')
    return output


@contextmanager
def writer_lease(root, *, scope=None):
    """Same sibling, exclusive share mode and DeleteOnClose as native AcquireWriter."""
    if scope is not None:
        with scope.acquire(root, FILE_WORK.get()): yield
        return
    work = FILE_WORK.get()
    def observe(name, action):
        return action() if work is None else work.operation(name, action)
    path = Path(str(root)+'.writer.lock')
    require(not observe('metadataSymlinkQuery', path.is_symlink)
            and not observe('metadataJunctionQuery', path.is_junction), 'Linked writer lease')
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    create = kernel.CreateFileW
    create.argtypes = [ctypes.c_wchar_p, ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p,
                       ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p]
    create.restype = ctypes.c_void_p
    close = kernel.CloseHandle
    close.argtypes = [ctypes.c_void_p]
    close.restype = ctypes.c_int
    def acquire():
        handle = create(str(path), 0xC0010000, 0, None, 4, 0x04000000, None)
        require(handle != ctypes.c_void_p(-1).value, 'Registry/output already leased or inaccessible')
        return handle
    handle = observe('writerLeaseAcquire', acquire)
    failure = None
    try:
        yield
    except BaseException as error:
        failure = error
        raise
    finally:
        def release():
            if not close(handle): raise ctypes.WinError(ctypes.get_last_error())
        try: observe('writerLeaseRelease', release)
        except BaseException as error:
            if work is not None: work.add('writerLeaseReleaseStateUnknown')
            if failure is None: raise
            failure.add_note('Writer lease release failed: '+repr(error))


def validate_admission(request_path, harness, pin):
    package = request_path.parent
    require(pin is not None and digest(package/'files.json') == pin, 'Supply the external admission manifest pin')
    files = read_json(package/'files.json', encoding='utf-8-sig')
    actual = {p.relative_to(package).as_posix() for p in inventory(package)}
    require(actual == set(files)|{'files.json'}, 'Changed admission inventory')
    for name, expected in files.items():
        path = package/name
        require(path.resolve().is_relative_to(package) and digest(path) == expected, 'Changed admitted input: '+name)
    receipt = read_json(package/'admission.json', encoding='utf-8-sig')
    q = read_json(request_path, encoding='utf-8-sig')
    require(q['version'] in (VERSION, CREATION_VERSION, SELECTOR_VERSION, VALIDATION_VERSION, PRESERVATION_VERSION, ALLIED_VERSION, PLACEMENT_VERSION, NOMINATION_VERSION), 'Changed proposal study version')
    require(receipt['status'] == 'ProposalStudyAdmittedNoReservation' and receipt['version'] == q['version']
            and receipt['requestSha256'] == digest(request_path) and receipt['fights'] == receipt['newValues'] == 0
            and harness == package/'runtime/BalanceHarness.dll' and 'failure.json' not in files,
            'Missing admitted request or substituted runtime')
    require(digest(Path(__file__)) == files['run-proposal-affinity-study.py']
            and digest(Path(__file__).with_name('bounded_windows_process.py')) == files['bounded_windows_process.py'],
            'Launcher/owner differs from admission')
    resources(q)
    if q.get('resourceEnvelope') is not None:
        require(receipt.get('resourceEnvelope') == q['resourceEnvelope'], 'Changed admitted resource envelope')
    require(request_path.name == 'request.json', 'Use the admitted request.json')
    for key in ('plan', 'context', 'settings', 'history', 'runtime', 'auditor'):
        bound = q[key]
        path = Path(bound['path']).resolve()
        require(path.is_relative_to(package) and files.get(path.relative_to(package).as_posix()) == bound['sha256'],
                'Source outside the authenticated admission: '+key)
    require(Path(q['contentRoot']).resolve().is_relative_to(package), 'Uncaptured content root')
    require(read_json(q['plan']['path'], encoding='utf-8-sig')['version'] == q['version'],
            'Request and plan study versions differ')



def launch(request_path, harness, dotnet, admission_pin=None, accounting=None):
    """Optional supervisor API; CLI defaults and admission gates are unchanged."""
    try:
        error = None
        try:
            result = _launch(request_path, harness, dotnet, admission_pin, accounting)
        except BaseException as caught:
            error = caught
            raise
        finally:
            if accounting is not None: accounting.finish(error)
        if accounting is not None and result is not None:
            print(json.dumps(dict(result, accountingManifestSha256=accounting.manifest_sha256,
                                  accountingObservationManifestSha256=accounting.observation_manifest_sha256,
                                  wholeProcessCoverage=False)))
    finally:
        if accounting is not None: accounting.cancel_watchdog()


def _launch(request_path, harness, dotnet, admission_pin=None, accounting=None):
    require(os.name == 'nt', 'The frozen launch requires Windows Job ownership')
    from bounded_windows_process import run
    request_path, harness = Path(request_path).resolve(), Path(harness).resolve()
    q = read_json(request_path, encoding='utf-8-sig')
    validate_admission(request_path, harness, admission_pin)
    output = validate_request(q)
    if accounting is not None: accounting.validate(request_path, output, harness)
    version = q['version']
    limits = resources(q)
    prior_seconds, prior_bytes = 0, 0
    require(harness.is_file() and harness.name == 'BalanceHarness.dll', 'Supply the prepared, compatible retained runtime')
    # No build, runtime substitution, preflight retry or entropy generation here.
    started, utc = time.monotonic(), dt.datetime.now(dt.timezone.utc)
    if accounting is not None: accounting.start(started, file_accounting, run)
    high_water = combined_size = 0

    def check(limit=NATIVE_BYTES):
        nonlocal high_water, combined_size
        size = storage_bytes(output)
        combined_size = size + (storage_bytes(accounting.root) if accounting is not None else 0)
        high_water = max(high_water, size)
        require(combined_size < limit, 'Combined retained storage allowance exhausted')
        require(time.monotonic() - started < SECONDS, 'Combined elapsed allowance exhausted')
        return size

    def supervisor_check():
        check()
        return combined_size

    if accounting is not None: accounting.set_check(supervisor_check)
    leases = ExitStack()
    try:
        lease_scope = None if accounting is None else accounting.owner_leases
        leases.enter_context(writer_lease(output.parent/'complete-family-allocation', scope=lease_scope))
        leases.enter_context(writer_lease(output, scope=lease_scope))
        require(not output.exists(), 'Output appeared after lease acquisition')
        output.mkdir()
        watchdog = threading.Timer(SECONDS if accounting is None else max(.001, started+SECONDS-time.monotonic()), lambda: os._exit(124))
        watchdog.daemon = True
        watchdog.start()
        if accounting is not None: accounting.watchdog = watchdog
    except BaseException:
        leases.__exit__(*sys.exc_info())
        raise
    def worker(phase, command, cwd, log_path, deadline, cleanup_seconds, check, file_work):
        if accounting is not None:
            return accounting.run(phase, command, cwd, log_path, deadline, cleanup_seconds, check)
        return run(command, cwd, log_path, deadline, cleanup_seconds=cleanup_seconds, check=check, file_work=file_work)

    try:
        with leases:
            copy_file(request_path, output/'request.json')
            copy_file(request_path.parent/'files.json', output/'admission-files.json')
            copy_file(request_path.parent/'admission.json', output/'admission-receipt.json')
            write(output/'admission-binding.json', dict(manifestSha256=admission_pin))
            copy_file(__file__, output/'launcher.py')
            require(digest(Path(q['auditor']['path'])) == q['auditor']['sha256'], 'Unbound independent auditor')
            copy_file(q['auditor']['path'], output/'auditor.py')
            copy_file(Path(__file__).with_name('bounded_windows_process.py'), output/'bounded_windows_process.py')
            request_hash = digest(output/'request.json')
            write(output/'launch.json', dict(version=version, requestFileHash=request_hash,
                startedAt=utc.isoformat(), nativeDeadline=(utc+dt.timedelta(seconds=limits['nativeSeconds'])).isoformat(),
                deadline=(utc+dt.timedelta(seconds=SECONDS)).isoformat(), maximumSeconds=SECONDS, maximumBytes=BYTES,
                nativeMaximumSeconds=limits['nativeSeconds'], nativeMaximumBytes=NATIVE_BYTES,
                parentProcessId=os.getpid(), mechanism='suspended-owned-job-v1'))
            process = worker('native', [dotnet, str(harness), 'tower-proposal-study-run', str(output)],
                          str(harness.parent), str(output/'native-console.log'), started+limits['nativeSeconds']-1,
                          cleanup_seconds=1, check=check, file_work=FILE_WORK.get())
            write(output/'native-process.json', process)
            require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
                    'Native comparison failed; retain all evidence and reservations')
            require(not (output/'failure.json').exists(), 'Failed comparison cannot complete')
            receipt = read_json(output/'native-receipt.json', encoding='utf-8')
            require(receipt['version'] == version and receipt['status'] == 'MeasuredPendingAudits'
                    and receipt['requestFileHash'] == request_hash and receipt['newAuditFights'] == 0,
                    'Missing verified native completion')
            audit_started, audit_base = time.monotonic(), check(NATIVE_BYTES)
            combined_audit_base = combined_size
            if accounting is not None: accounting.begin('nativeAudit')
            require(audit_started-started < limits['nativeSeconds'], 'Native execution and cleanup exhausted their allowance')
            protected = {p.relative_to(output).as_posix(): digest(p) for p in inventory(output)}
            audit_end = audit_deadline(started, audit_started, limits)
            def audit_check():
                size = check(BYTES)
                require(combined_size-combined_audit_base < AUDIT_BYTES, 'Audit/publication storage reserve exhausted')
                require(time.monotonic() < audit_end, 'Audit/publication deadline exhausted')
                return size
            def supervisor_audit_check():
                audit_check()
                return combined_size
            if accounting is not None: accounting.set_check(supervisor_audit_check)
            # Both audits use the SAME remaining execution deadline, never fresh allowances.
            audit_process = worker('nativeAudit', [dotnet, str(harness), 'tower-proposal-study-audit', str(output)],
                                str(harness.parent), str(output/'native-audit.log'), audit_end,
                                cleanup_seconds=1, check=audit_check, file_work=FILE_WORK.get())
            write(output/'native-audit-process.json', audit_process)
            require(audit_process['exitCode'] == 0 and not audit_process['timedOut'] and audit_process['activeProcesses'] == 0, 'Native audit failed')
            if accounting is not None: accounting.begin('independentAudit')
            independent_process = worker('independentAudit', [sys.executable, '-B', '-X', 'utf8', str(output/'auditor.py'), '--working', str(output),
                                       '--output', str(output/'independent-audit.json')], str(output),
                                      str(output/'independent-audit.log'), audit_end,
                                      cleanup_seconds=1, check=audit_check, file_work=FILE_WORK.get())
            write(output/'independent-audit-process.json', independent_process)
            require(independent_process['exitCode'] == 0 and not independent_process['timedOut'] and independent_process['activeProcesses'] == 0, 'Independent audit failed')
            audit = read_json(output/'independent-audit.json', encoding='utf-8')
            require(audit['status'] == 'Passed' and audit['requestFileHash'] == request_hash and audit['newFights'] == 0 and audit['newValues'] == 0,
                    'Missing bound independent result')
            native_result = read_json(output/'native-audit.log', encoding='utf-8-sig')
            require(audit['result'] == native_result == read_json(output/'provisional-result.json'), 'Audit result disagreement')
            if accounting is not None: accounting.begin('publication')
            publication = worker('publication', [dotnet, str(harness), 'tower-proposal-study-publication-check', str(output)],
                              str(harness.parent), str(output/'publication.log'), audit_end,
                              cleanup_seconds=1, check=audit_check, file_work=FILE_WORK.get())
            write(output/'publication-process.json', publication)
            require(publication['exitCode'] == 0 and not publication['timedOut'] and publication['activeProcesses'] == 0, 'Final publication barrier failed')
            for source, target in [('provisional-result.json','result.json')]:
                copy_file(output/source, output/target)
            audit_check()
            # Hashing and finalization remain inside the same cumulative envelope.
            files = publication_inventory(output, protected)
            elapsed = time.monotonic()-started
            completion = dict(version=version, status='Complete', requestFileHash=request_hash,
                seconds=elapsed, observedBytes=high_water, nativeSeconds=audit_started-started, nativeBytes=audit_base,
                auditSeconds=elapsed-(audit_started-started), auditBytes=high_water-audit_base,
                chargedSeconds=elapsed+prior_seconds, chargedBytes=high_water+prior_bytes, retries=0, process=process)
            payload_bytes = sum(file_operation('metadataStat', p.stat).st_size for p in inventory(output))
            # Archive receipts preserve the native verifier's archive-only contract.
            # Every bound check separately enforces archive plus sidecar storage.
            # Include completion and its enclosing manifest in the retained-byte charge.
            for _ in range(16):
                encoded = json.dumps(completion, indent=2).encode('utf-8')
                files['completion.json'] = hashlib.sha256(encoded).hexdigest()
                retained = max(high_water, payload_bytes+len(encoded)+len(json.dumps(files, indent=2).encode('utf-8')))
                if completion['observedBytes'] == retained:
                    break
                completion.update(observedBytes=retained, chargedBytes=retained+prior_bytes, auditBytes=retained-audit_base)
            else:
                raise ValueError('Publication byte accounting did not converge')
            require(retained < BYTES and retained-audit_base < AUDIT_BYTES, 'Publication exceeds retained storage')
            audit_check()
            write(output/'completion.json', completion)
            require(files['completion.json'] == digest(output/'completion.json'), 'Publication serialization changed')
            write(output/'files.json', files)
            audit_check()
            # Terminal receipt binds the sealed manifest and includes its own storage.
            # The completion receipt above explicitly precedes sealing; this measures it.
            final_hash, base = digest(output/'files.json'), audit_check()
            final_elapsed = time.monotonic()-started
            final = dict(version=version, requestHash=request_hash, filesHash=final_hash,
                         measuredSeconds=final_elapsed, retainedBytes=0,
                         auditSeconds=final_elapsed-(audit_started-started), auditBytes=0,
                         chargedSeconds=0, chargedBytes=0)
            for _ in range(16):
                total = base+len(json.dumps(final,indent=2).encode('utf-8'))
                if final['retainedBytes'] == total:
                    break
                final.update(retainedBytes=total,auditBytes=total-audit_base,
                             chargedSeconds=final['measuredSeconds']+prior_seconds,chargedBytes=total+prior_bytes)
            else:
                raise ValueError('Terminal receipt accounting did not converge')
            require(final['retainedBytes'] < BYTES and final['auditBytes'] < AUDIT_BYTES, 'Terminal receipt exceeds storage')
            write(output/'closeout.json',final)
            audit_check()
            result = dict(status='Complete', output=str(output), fights=receipt['fights'], retries=0, closeoutSha256=digest(output/'closeout.json'))
    except BaseException as error:
        if not (output/'failure.json').exists():
            write(output/'failure.json', dict(version=version, status='IncompleteComparison', reason=str(error), retries=0))
        raise
    finally:
        if accounting is None: watchdog.cancel()
    # A release failure must reach the failure marker/supervisor before any
    # successful return or console completion is published.
    if accounting is not None: return result
    print(json.dumps(result))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--request', required=True)
    parser.add_argument('--harness', required=True)
    parser.add_argument('--dotnet', default='dotnet')
    parser.add_argument('--admission-pin', required=True, help='External SHA-256 of the sealed admission files.json')
    args = parser.parse_args()
    launch(args.request, args.harness, args.dotnet, args.admission_pin)
