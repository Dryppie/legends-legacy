"""Single owned launch of the frozen 12-pair comparison. Never retries or prepares inputs.

Admission is separately charged; this launcher includes both saved-row audits and publication.
Only this launcher starts the native comparison, inside a hidden Windows Job assigned
before its process is resumed. Failure preserves output and unresolved reservations.
"""
import argparse
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

VERSION = 'tower-reference-exploration-comparison-v1'
OFFSET_VERSION = 'tower-reference-exploration-offset-comparison-v1'
ADAPTIVE_VERSION = 'tower-adaptive-racing-comparison-v1'
SCREENING_VERSION = 'tower-practical-fresh-screening-comparison-v1'
SECONDS, BYTES = 10200, 5905580032
CUMULATIVE_SECONDS, CUMULATIVE_BYTES = 10800, 6442450944
PRIOR_SECONDS, PRIOR_BYTES = 600, 536870912
NATIVE_SECONDS, NATIVE_BYTES = 10080, 5637144576


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
    require(q['version'] in (VERSION, OFFSET_VERSION, SCREENING_VERSION, ADAPTIVE_VERSION) and q.get('maximumSeconds', CUMULATIVE_SECONDS) == CUMULATIVE_SECONDS
            and q.get('maximumBytes', CUMULATIVE_BYTES) == CUMULATIVE_BYTES
            and q.get('priorSeconds', PRIOR_SECONDS) == (0 if q['version'] == ADAPTIVE_VERSION else PRIOR_SECONDS) and q.get('priorBytes', PRIOR_BYTES) == (0 if q['version'] == ADAPTIVE_VERSION else PRIOR_BYTES), 'Changed comparison version or resource envelope')
    output, registry = Path(q['outputRoot']), Path(q['registryRoot'])
    require(output.is_absolute() and registry.is_absolute() and output.parent.resolve() == registry.resolve(),
            'Output must be directly beneath the complete registry')
    require(not output.exists(), 'No retry, resume or existing output')
    return output


def launch(request_path, harness, dotnet):
    require(os.name == 'nt', 'The frozen launch requires Windows Job ownership')
    from bounded_windows_process import run
    request_path, harness = Path(request_path).resolve(), Path(harness).resolve()
    q = json.loads(request_path.read_text(encoding='utf-8-sig'))
    output = validate_request(q)
    version = q['version']
    seconds, byte_limit, prior_seconds, prior_bytes, native_seconds, native_bytes = ((10800, 6442450944, 0, 0, 10680, 6174015488)
        if version == ADAPTIVE_VERSION else (SECONDS, BYTES, PRIOR_SECONDS, PRIOR_BYTES, NATIVE_SECONDS, NATIVE_BYTES))
    require(harness.is_file() and harness.name == 'BalanceHarness.dll', 'Supply the prepared, compatible retained runtime')
    # No build, runtime substitution, preflight retry or entropy generation here.
    started, utc = time.monotonic(), dt.datetime.now(dt.timezone.utc)
    output.mkdir()
    watchdog = threading.Timer(seconds, lambda: os._exit(124))
    watchdog.daemon = True
    watchdog.start()
    high_water = 0

    def check(limit=native_bytes):
        nonlocal high_water
        size = storage_bytes(output)
        high_water = max(high_water, size)
        require(size < limit, 'Combined retained storage allowance exhausted')
        require(time.monotonic() - started < seconds, 'Combined elapsed allowance exhausted')
        return size

    try:
        shutil.copyfile(request_path, output/'request.json')
        shutil.copyfile(__file__, output/'launcher.py')
        require(digest(Path(q['auditorPath'])) == q['auditorHash'], 'Unbound independent auditor')
        shutil.copyfile(q['auditorPath'], output/'auditor.py')
        shutil.copyfile(Path(__file__).with_name('bounded_windows_process.py'), output/'bounded_windows_process.py')
        request_hash = digest(output/'request.json')
        write(output/'launch.json', dict(version=version, requestFileHash=request_hash,
            startedAt=utc.isoformat(), nativeDeadline=(utc+dt.timedelta(seconds=native_seconds)).isoformat(),
            deadline=(utc+dt.timedelta(seconds=seconds)).isoformat(), maximumSeconds=seconds, maximumBytes=byte_limit,
            nativeMaximumSeconds=native_seconds, nativeMaximumBytes=native_bytes,
            parentProcessId=os.getpid(), mechanism='suspended-owned-job-v1'))
        process = run([dotnet, str(harness), 'tower-reference-exploration-comparison-run', str(output)],
                      str(harness.parent), str(output/'native-console.log'), started+native_seconds,
                      cleanup_seconds=1, check=check)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
                'Native comparison failed; retain all evidence and reservations')
        require(not (output/'failure.json').exists(), 'Failed comparison cannot complete')
        receipt = json.loads((output/'native-receipt.json').read_text(encoding='utf-8'))
        require(receipt['version'] == version and receipt['status'] == 'Verified'
                and receipt['requestFileHash'] == request_hash and receipt['newAuditFights'] == 0,
                'Missing verified native completion')
        # Both audits use the SAME remaining execution deadline, never fresh allowances.
        audit_process = run([dotnet, str(harness), 'tower-reference-exploration-comparison-audit', str(output)],
                            str(harness.parent), str(output/'native-audit.log'), started+seconds-5,
                            cleanup_seconds=1, check=lambda: check(byte_limit))
        write(output/'native-audit-process.json', audit_process)
        require(audit_process['exitCode'] == 0 and not audit_process['timedOut'] and audit_process['activeProcesses'] == 0, 'Native audit failed')
        independent_process = run([sys.executable, '-B', '-X', 'utf8', str(output/'auditor.py'), '--working', str(output),
                                   '--output', str(output/'independent-audit.json')], str(output),
                                  str(output/'independent-audit.log'), started+seconds-5,
                                  cleanup_seconds=1, check=lambda: check(byte_limit))
        write(output/'independent-audit-process.json', independent_process)
        require(independent_process['exitCode'] == 0 and not independent_process['timedOut'] and independent_process['activeProcesses'] == 0, 'Independent audit failed')
        audit = json.loads((output/'independent-audit.json').read_text(encoding='utf-8'))
        require(audit['status'] == 'Passed' and audit['requestFileHash'] == request_hash and audit['newFights'] == 0,
                'Missing bound independent result')
        check(byte_limit)
        # Hashing and finalization remain inside the same cumulative envelope.
        files = {p.relative_to(output).as_posix(): digest(p) for p in inventory(output)}
        elapsed = time.monotonic()-started
        completion = dict(version=version, status='Complete', requestFileHash=request_hash,
            seconds=elapsed, observedBytes=high_water, chargedSeconds=elapsed+prior_seconds, chargedBytes=high_water+prior_bytes, retries=0, process=process)
        payload_bytes = sum(p.stat().st_size for p in inventory(output))
        # Include completion and its enclosing manifest in the retained-byte charge.
        for _ in range(16):
            encoded = json.dumps(completion, indent=2).encode('utf-8')
            files['completion.json'] = hashlib.sha256(encoded).hexdigest()
            retained = max(high_water, payload_bytes+len(encoded)+len(json.dumps(files, indent=2).encode('utf-8')))
            if completion['observedBytes'] == retained:
                break
            completion.update(observedBytes=retained, chargedBytes=retained+prior_bytes)
        else:
            raise ValueError('Publication byte accounting did not converge')
        require(retained < byte_limit, 'Publication exceeds retained storage')
        write(output/'completion.json', completion)
        require(files['completion.json'] == digest(output/'completion.json'), 'Publication serialization changed')
        write(output/'files.json', files)
        check(byte_limit)
        print(json.dumps(dict(status='Complete', output=str(output), fights=receipt['fights'], retries=0)))
    except BaseException as error:
        if not (output/'failure.json').exists():
            write(output/'failure.json', dict(version=version, status='IncompleteComparison', reason=str(error), retries=0))
        raise
    finally:
        watchdog.cancel()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--request', required=True)
    parser.add_argument('--harness', required=True)
    parser.add_argument('--dotnet', default='dotnet')
    args = parser.parse_args()
    launch(args.request, args.harness, args.dotnet)
