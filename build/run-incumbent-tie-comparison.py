"""Single owned launch of the frozen 24-restart comparison. Never retries or prepares inputs.

Preparation/checks and the independent saved-row audit are separate engineering work.
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
import sys
import threading
import time

VERSION = 'tower-incumbent-tie-comparison-v1'
SECONDS, BYTES = 4500, 4294967296
NATIVE_SECONDS, NATIVE_BYTES = 4440, 4160749568


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


def validate_request(q):
    require(q['version'] == VERSION and q.get('maximumSeconds', SECONDS) == SECONDS
            and q.get('maximumBytes', BYTES) == BYTES, 'Changed comparison version or resource envelope')
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
    require(harness.is_file() and harness.name == 'BalanceHarness.dll', 'Supply the prepared, compatible retained runtime')
    # No build, runtime substitution, preflight retry or entropy generation here.
    started, utc = time.monotonic(), dt.datetime.now(dt.timezone.utc)
    output.mkdir()
    watchdog = threading.Timer(SECONDS, lambda: os._exit(124))
    watchdog.daemon = True
    watchdog.start()
    high_water = 0

    def check(limit=NATIVE_BYTES):
        nonlocal high_water
        size = sum(p.stat().st_size for p in inventory(output))
        high_water = max(high_water, size)
        require(size < limit, 'Combined retained storage allowance exhausted')
        require(time.monotonic() - started < SECONDS, 'Combined elapsed allowance exhausted')
        return size

    try:
        shutil.copyfile(request_path, output/'request.json')
        shutil.copyfile(__file__, output/'launcher.py')
        shutil.copyfile(Path(__file__).with_name('bounded_windows_process.py'), output/'bounded_windows_process.py')
        request_hash = digest(output/'request.json')
        write(output/'launch.json', dict(version=VERSION, requestFileHash=request_hash,
            startedAt=utc.isoformat(), nativeDeadline=(utc+dt.timedelta(seconds=NATIVE_SECONDS)).isoformat(),
            deadline=(utc+dt.timedelta(seconds=SECONDS)).isoformat(), maximumSeconds=SECONDS, maximumBytes=BYTES,
            nativeMaximumSeconds=NATIVE_SECONDS, nativeMaximumBytes=NATIVE_BYTES,
            parentProcessId=os.getpid(), mechanism='suspended-owned-job-v1'))
        process = run([dotnet, str(harness), 'tower-incumbent-tie-comparison-run', str(output)],
                      str(harness.parent), str(output/'native-console.log'), started+NATIVE_SECONDS,
                      cleanup_seconds=1, check=check)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
                'Native comparison failed; retain all evidence and reservations')
        require(not (output/'failure.json').exists(), 'Failed comparison cannot complete')
        receipt = json.loads((output/'native-receipt.json').read_text(encoding='utf-8'))
        require(receipt['version'] == VERSION and receipt['status'] == 'Verified'
                and receipt['requestFileHash'] == request_hash and receipt['newAuditFights'] == 0,
                'Missing verified native completion')
        check(BYTES)
        # Hashing and finalization consume the enclosing 60-second / 128-MiB reserve.
        files = {p.relative_to(output).as_posix(): digest(p) for p in inventory(output)}
        write(output/'completion.json', dict(version=VERSION, status='Complete', requestFileHash=request_hash,
            seconds=time.monotonic()-started, observedBytes=high_water, retries=0, process=process))
        files['completion.json'] = digest(output/'completion.json')
        write(output/'files.json', files)
        check(BYTES)
        print(json.dumps(dict(status='Complete', output=str(output), fights=receipt['fights'], retries=0)))
    except BaseException as error:
        if not (output/'failure.json').exists():
            write(output/'failure.json', dict(version=VERSION, status='IncompleteComparison', reason=str(error), retries=0))
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
