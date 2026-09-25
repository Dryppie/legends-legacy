"""Single owned launch of the frozen complete-neighborhood diagnostic. Never retries or prepares inputs.

Admission is separately charged; this launcher includes both saved-row audits and publication.
Only this launcher starts the native comparison, inside a hidden Windows Job assigned
before its process is resumed. Failure preserves output and unresolved reservations.
"""
import argparse
from contextlib import ExitStack, contextmanager
import ctypes
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import shutil
import stat
import sys
import threading
import time

VERSION = 'tower-affinity-neighborhood-recognition-v1'
SECONDS, BYTES = 18000, 18253611008
CUMULATIVE_SECONDS, CUMULATIVE_BYTES = 19800, 18790481920
PRIOR_SECONDS, PRIOR_BYTES = 1800, 536870912
NATIVE_SECONDS, NATIVE_BYTES = 14400, 17179869184
AUDIT_SECONDS, AUDIT_BYTES = 3600, 1073741824


def require(ok, message):
    if not ok:
        raise ValueError(message)


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def write(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2)
        stream.flush()
        os.fsync(stream.fileno())


def inventory(root):
    files = []
    for parent, dirs, names in os.walk(root, followlinks=False):
        for name in dirs + names:
            path = Path(parent)/name
            require(not path.is_symlink() and not path.is_junction(), 'Linked output is forbidden')
        files.extend(Path(parent)/name for name in names)
    return sorted(files)


def storage_bytes(root):
    """Sample live output, including temporary bytes during atomic publication.

    The native writer flushes <name>.pending and atomically renames it to <name>.
    Enumeration can see the old name immediately before that rename. Resolve only
    this known transition; missing ordinary files and other I/O failures still fail.
    Final manifest hashing remains strict and happens after all writers have exited.
    """
    sizes = {}
    for path in inventory(root):
        try:
            info = path.lstat()
        except FileNotFoundError:
            if not path.name.endswith('.pending'):
                raise
            path = path.with_name(path.name[:-len('.pending')])
            info = path.lstat()
        require(stat.S_ISREG(info.st_mode) and not getattr(info, 'st_file_attributes', 0) & 0x400,
                'Linked or non-file output is forbidden')
        # The same published path may also have been enumerated. Count it once,
        # retaining the larger observation if it grew between the two reads.
        sizes[path] = max(sizes.get(path, 0), info.st_size)
    return sum(sizes.values())


def validate_request(q):
    require(q['version'] == VERSION and q.get('priorSeconds') == PRIOR_SECONDS
            and q.get('priorBytes') == PRIOR_BYTES
            and q.get('maximumSeconds') == q['priorSeconds']+SECONDS and q.get('maximumBytes') == q['priorBytes']+BYTES,
            'Changed or missing recognition version or resource envelope')
    # The native record includes both nullable history fields in its canonical
    # hash. Refuse a differently shaped request before creating output or entropy.
    require('pendingHistoryRecoveries' in q and 'recoveryReceiptHashes' in q,
            'Retain both nullable history fields in the native request binding')
    output, registry = Path(q['outputRoot']), Path(q['registryRoot'])
    require(output.is_absolute() and registry.is_absolute() and output.parent.resolve() == registry.resolve(),
            'Output must be directly beneath the complete registry')
    require(not output.exists(), 'No retry, resume or existing output')
    return output


@contextmanager
def writer_lease(root):
    """Same sibling, exclusive share mode and DeleteOnClose as native AcquireWriter."""
    path = Path(str(root)+'.writer.lock')
    require(not path.is_symlink() and not path.is_junction(), 'Linked writer lease')
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    create = kernel.CreateFileW
    create.argtypes = [ctypes.c_wchar_p, ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p,
                       ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p]
    create.restype = ctypes.c_void_p
    close = kernel.CloseHandle
    close.argtypes = [ctypes.c_void_p]
    handle = create(str(path), 0xC0010000, 0, None, 4, 0x04000000, None)
    require(handle != ctypes.c_void_p(-1).value, 'Registry/output already leased or inaccessible')
    try:
        yield
    finally:
        close(handle)


def validate_admission(request_path, harness, pin):
    package = request_path.parent
    require(pin is not None and digest(package/'files.json') == pin, 'Supply the external admission manifest pin')
    files = json.loads((package/'files.json').read_text(encoding='utf-8-sig'))
    actual = {p.relative_to(package).as_posix() for p in inventory(package)}
    require(actual == set(files)|{'files.json'}, 'Changed admission inventory')
    for name, expected in files.items():
        path = package/name
        require(path.resolve().is_relative_to(package) and digest(path) == expected, 'Changed admitted input: '+name)
    receipt = json.loads((package/'admission.json').read_text(encoding='utf-8-sig'))
    require(receipt['status'] == 'RecognitionAdmittedNoReservation' and receipt['version'] == VERSION
            and receipt['requestSha256'] == digest(request_path) and receipt['fights'] == receipt['newValues'] == 0
            and harness == package/'runtime/BalanceHarness.dll' and 'failure.json' not in files,
            'Missing admitted request or substituted runtime')
    require(digest(Path(__file__)) == files['run-affinity-neighborhood-recognition.py']
            and digest(Path(__file__).with_name('bounded_windows_process.py')) == files['bounded_windows_process.py'],
            'Launcher/owner differs from admission')


def launch(request_path, harness, dotnet, admission_pin=None):
    require(os.name == 'nt', 'The frozen launch requires Windows Job ownership')
    from bounded_windows_process import run
    request_path, harness = Path(request_path).resolve(), Path(harness).resolve()
    q = json.loads(request_path.read_text(encoding='utf-8-sig'))
    validate_admission(request_path, harness, admission_pin)
    output = validate_request(q)
    version = q['version']
    prior_seconds, prior_bytes = q['priorSeconds'], q['priorBytes']
    require(harness.is_file() and harness.name == 'BalanceHarness.dll', 'Supply the prepared, compatible retained runtime')
    # No build, runtime substitution, preflight retry or entropy generation here.
    started, utc = time.monotonic(), dt.datetime.now(dt.timezone.utc)
    leases = ExitStack()
    try:
        leases.enter_context(writer_lease(output.parent/'complete-family-allocation'))
        leases.enter_context(writer_lease(output))
        require(not output.exists(), 'Output appeared after lease acquisition')
        output.mkdir()
    except BaseException:
        leases.close()
        raise
    watchdog = threading.Timer(SECONDS, lambda: os._exit(124))
    watchdog.daemon = True
    watchdog.start()
    high_water = 0

    def check(limit=NATIVE_BYTES):
        nonlocal high_water
        size = storage_bytes(output)
        high_water = max(high_water, size)
        require(size < limit, 'Combined retained storage allowance exhausted')
        require(time.monotonic() - started < SECONDS, 'Combined elapsed allowance exhausted')
        return size

    try:
        shutil.copyfile(request_path, output/'request.json')
        shutil.copyfile(__file__, output/'launcher.py')
        require(digest(Path(q['recognition']['auditorPath'])) == q['recognition']['auditorHash'], 'Unbound independent auditor')
        shutil.copyfile(q['recognition']['auditorPath'], output/'auditor.py')
        shutil.copyfile(Path(__file__).with_name('bounded_windows_process.py'), output/'bounded_windows_process.py')
        request_hash = digest(output/'request.json')
        write(output/'launch.json', dict(version=version, requestFileHash=request_hash,
            startedAt=utc.isoformat(), nativeDeadline=(utc+dt.timedelta(seconds=NATIVE_SECONDS)).isoformat(),
            deadline=(utc+dt.timedelta(seconds=SECONDS)).isoformat(), maximumSeconds=SECONDS, maximumBytes=BYTES,
            nativeMaximumSeconds=NATIVE_SECONDS, nativeMaximumBytes=NATIVE_BYTES,
            parentProcessId=os.getpid(), mechanism='suspended-owned-job-v1'))
        process = run([dotnet, str(harness), 'tower-affinity-neighborhood-recognition-run', str(output)],
                      str(harness.parent), str(output/'native-console.log'), started+NATIVE_SECONDS-1,
                      cleanup_seconds=1, check=check)
        write(output/'native-process.json', process)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
                'Native comparison failed; retain all evidence and reservations')
        require(not (output/'failure.json').exists(), 'Failed comparison cannot complete')
        receipt = json.loads((output/'native-receipt.json').read_text(encoding='utf-8'))
        require(receipt['version'] == version and receipt['status'] == 'Verified'
                and receipt['requestFileHash'] == request_hash and receipt['newAuditFights'] == 0,
                'Missing verified native completion')
        audit_started, audit_base = time.monotonic(), check(NATIVE_BYTES)
        require(audit_started-started < NATIVE_SECONDS, 'Native execution and cleanup exhausted their allowance')
        protected = {p.relative_to(output).as_posix(): digest(p) for p in inventory(output)}
        audit_deadline = min(started+SECONDS-5, audit_started+AUDIT_SECONDS-5)
        def audit_check():
            size = check(BYTES)
            require(size-audit_base < AUDIT_BYTES, 'Audit/publication storage reserve exhausted')
            require(time.monotonic() < audit_deadline, 'Audit/publication deadline exhausted')
            return size
        # Both audits use the SAME remaining execution deadline, never fresh allowances.
        audit_process = run([dotnet, str(harness), 'tower-affinity-neighborhood-recognition-audit', str(output)],
                            str(harness.parent), str(output/'native-audit.log'), audit_deadline,
                            cleanup_seconds=1, check=audit_check)
        write(output/'native-audit-process.json', audit_process)
        require(audit_process['exitCode'] == 0 and not audit_process['timedOut'] and audit_process['activeProcesses'] == 0, 'Native audit failed')
        independent_process = run([sys.executable, '-B', '-X', 'utf8', str(output/'auditor.py'), '--working', str(output),
                                   '--output', str(output/'independent-audit.json')], str(output),
                                  str(output/'independent-audit.log'), audit_deadline,
                                  cleanup_seconds=1, check=audit_check)
        write(output/'independent-audit-process.json', independent_process)
        require(independent_process['exitCode'] == 0 and not independent_process['timedOut'] and independent_process['activeProcesses'] == 0, 'Independent audit failed')
        audit = json.loads((output/'independent-audit.json').read_text(encoding='utf-8'))
        require(audit['status'] == 'Passed' and audit['requestFileHash'] == request_hash and audit['newFights'] == 0 and audit['newValues'] == 0,
                'Missing bound independent result')
        native_result = json.loads((output/'native-audit.log').read_text(encoding='utf-8-sig'))
        require(audit['result'] == native_result == json.loads((output/'provisional-result.json').read_text()), 'Audit result disagreement')
        publication = run([dotnet, str(harness), 'tower-affinity-neighborhood-recognition-publication-check', str(output)],
                          str(harness.parent), str(output/'publication.log'), audit_deadline,
                          cleanup_seconds=1, check=audit_check)
        write(output/'publication-process.json', publication)
        require(publication['exitCode'] == 0 and not publication['timedOut'] and publication['activeProcesses'] == 0, 'Final publication barrier failed')
        require(all(digest(output/name) == pin for name, pin in protected.items()), 'Audit inputs changed before publication')
        for source, target in [('provisional-result.json','result.json'), ('proposed-teams.json','teams.json'), ('proposed-recognition.md','recognition.md')]:
            shutil.copyfile(output/source, output/target)
        audit_check()
        # Hashing and finalization remain inside the same cumulative envelope.
        files = {p.relative_to(output).as_posix(): digest(p) for p in inventory(output)}
        elapsed = time.monotonic()-started
        completion = dict(version=version, status='Complete', requestFileHash=request_hash,
            seconds=elapsed, observedBytes=high_water, nativeSeconds=audit_started-started, nativeBytes=audit_base,
            auditSeconds=elapsed-(audit_started-started), auditBytes=high_water-audit_base,
            chargedSeconds=elapsed+prior_seconds, chargedBytes=high_water+prior_bytes, retries=0, process=process)
        payload_bytes = sum(p.stat().st_size for p in inventory(output))
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
        print(json.dumps(dict(status='Complete', output=str(output), fights=receipt['fights'], retries=0)))
    except BaseException as error:
        if not (output/'failure.json').exists():
            write(output/'failure.json', dict(version=version, status='IncompleteComparison', reason=str(error), retries=0))
        raise
    finally:
        watchdog.cancel()
        leases.close()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--request', required=True)
    parser.add_argument('--harness', required=True)
    parser.add_argument('--dotnet', default='dotnet')
    parser.add_argument('--admission-pin', required=True, help='External SHA-256 of the sealed admission files.json')
    args = parser.parse_args()
    launch(args.request, args.harness, args.dotnet, args.admission_pin)
