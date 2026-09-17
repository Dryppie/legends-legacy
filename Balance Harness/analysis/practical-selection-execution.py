"""Own the single approved, frozen native diagnostic; read its receipts afterward.

No retry, rebuild, recovery, replay or standalone native verifier is provided.
The read-only mode authenticates inventories and receipts, not combat semantics;
native reconstruction and the independent audit belong to the public run itself.
"""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT/'TestResults/selection-diagnostic-admission-20260917'
OUTPUT = ROOT/'TestResults/selection-diagnostic-execution-20260917'
RUN = ROOT/'TestResults/balance/tower-practical-selection-diagnostic-20260917'
MIB = 1048576
SECONDS, BYTES = 1380, 512*MIB
REQUEST_HASH = 'd0baa036cd43c0e70edc8f33dd9b8db375ddaad1e9c77f33ddd43e039ee02136'
MANIFEST_HASH = '3dbb77e0097c1f8fa791fdbe8b37894a55c84b0b65643d93a18039206dbf4661'
OWNER_HASH = '120c2447fa5414d1d0062dcd4fc4eeb2ca70f744b8bb6e447f7f2325fa155332'


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def save(path, value):
    with path.open('xb') as f:
        f.write((json.dumps(value, indent=2, allow_nan=False)+'\n').encode('utf-8'))
        f.flush()
        os.fsync(f.fileno())


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def paths(root):
    return sorted(p for p in root.rglob('*') if p.is_file())


def size(root):
    return sum(p.stat().st_size for p in paths(root))


def inventory(root, guard=lambda: None):
    result = {}
    for p in paths(root):
        result[p.relative_to(root).as_posix()] = sha(p)
        guard()
    return result


def observe():
    result = {}
    for name in ('result', 'completion', 'failure', 'worker-failure', 'native-audit',
                 'independent-audit', 'history-input', 'confirmation-binding',
                 'admission-phase', 'search-phase', 'confirmation-phase', 'audit-phase'):
        p = RUN/(name+'.json')
        if p.exists():
            value = read(p)
            if name == 'history-input':
                value = {'reservationState': value['reservationState'], 'reservedCount': len(value.get('reserved', []))}
            if name == 'confirmation-binding':
                value = {k: len(v) if isinstance(v, list) else v for k, v in value.items()}
            result[name] = value
    counts = {'Started': 0, 'Completed': 0, 'unreadableLines': 0}
    p = RUN/'attempts.jsonl'
    if p.exists():
        for line in p.read_text(encoding='utf-8-sig').splitlines():
            try:
                kind = json.loads(line)['kind']
                counts[kind] = counts.get(kind, 0)+1
            except (ValueError, KeyError):
                counts['unreadableLines'] += 1
    result['attemptCounts'] = counts
    return result


def execute():
    # Start the enclosing allowance before admission revalidation or receipt writes.
    started = time.monotonic()
    require(not OUTPUT.exists() and not RUN.exists(), 'Existing scope/output; no retry or resume.')
    OUTPUT.mkdir()
    peak, last_scan, last_report = 0, -float('inf'), -float('inf')

    def guard(force=False):
        nonlocal peak, last_scan, last_report
        require(time.monotonic()-started < SECONDS-2, 'Outer execution deadline reached.')
        now = time.monotonic()
        if force or now-last_scan >= 1:
            observed = size(RUN)+size(OUTPUT)
            peak = max(peak, observed)
            last_scan = now
            require(observed < BYTES-4*MIB, 'Combined run/oversight storage ceiling reached.')
        if now-last_report >= 30:
            phase = 'setup'
            try:
                if (RUN/'phase.json').exists():
                    phase = read(RUN/'phase.json')['name']
            except (OSError, ValueError):
                phase = 'transition'
            print(json.dumps({'elapsedSeconds': round(now-started, 3), 'phase': phase,
                              'sampledCombinedBytes': peak}), flush=True)
            last_report = now

    process, observation, error = None, {}, None
    try:
        require(sha(PACKAGE/'request.proposed.json') == REQUEST_HASH
                and sha(PACKAGE/'files.json') == MANIFEST_HASH
                and sha(PACKAGE/'bounded_output_process.py') == OWNER_HASH, 'Frozen identity changed.')
        save(OUTPUT/'approval.json', {
            'recordedAtUtc': datetime.now(timezone.utc).isoformat(),
            'basis': 'User replied "Please proceed" to the concrete single-run approval and capped-failure-risk question.',
            'gameplayAuthorized': True, 'riskAccepted': True, 'attemptsAllowed': 1, 'retriesAllowed': 0,
            'requestFileSha256': REQUEST_HASH, 'packageManifestSha256': MANIFEST_HASH,
            'additionalSeconds': SECONDS, 'additionalBytes': BYTES,
            'priorSeconds': 480, 'priorBytes': 576*MIB,
            'maximumCumulativeSeconds': 1860, 'maximumCumulativeBytes': 1088*MIB,
            'includes': 'Revalidation, enclosing setup/logs, native run, built-in audits/publication, inventory closeout.',
            'risk': 'Terminal capped failure may yield incomplete evidence and blocking Pending reservations.',
            'excludes': 'Retry, refill, replay, extension, phase transfer, extra public verifier, automatic promotion.'})
        shutil.copyfile(Path(__file__), OUTPUT/'execution-source.py')
        shutil.copyfile(PACKAGE/'bounded_output_process.py', OUTPUT/'bounded_output_process.py')
        admission = load('selection_execution_admission', ROOT/'Balance Harness/analysis/practical-selection-admission.py')
        check = admission.verify()
        require(check == read(ROOT/'Balance Harness/Tower-Practical-Selection-Diagnostic-Admission.json'), 'Admission review differs.')
        save(OUTPUT/'admission-revalidation.json', check)
        guard(True)
        command = read(PACKAGE/'command-after-approval.json')['executableArguments']
        require(command == ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'),
                            'tower-selection-diagnostic-run', str(PACKAGE/'request.proposed.json')], 'Command changed.')
        save(OUTPUT/'command.json', {'executableArguments': command, 'cwd': str(ROOT), 'attempt': 1})
        owner = load('selection_execution_owner', OUTPUT/'bounded_output_process.py')
        print('Launching the approved public diagnostic exactly once.', flush=True)
        process = owner.run(command, ROOT, OUTPUT/'native-console.json', started+SECONDS-2,
                            cleanup_seconds=1, guard=guard)
        save(OUTPUT/'process.json', process)
        guard(True)
        observation = observe()
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
                'Native diagnostic failed or exceeded its limit; no retry.')
        require(not (RUN/'failure.json').exists() and not (RUN/'worker-failure.json').exists(), 'Native failure marker.')
        require(observation['result']['executionStatus'] == 'Complete'
                and observation['result']['integrityStatus'] == 'Verified'
                and observation['completion']['status'] == 'Complete'
                and observation['completion']['fights'] == 4640
                and observation['native-audit']['status'] == 'Passed'
                and observation['native-audit']['newFights'] == 0
                and observation['result'] == observation['independent-audit'] == read(RUN/'worker-result.json')
                and observation['attemptCounts'] == {'Started': 4640, 'Completed': 4640, 'unreadableLines': 0},
                'Incomplete or disagreeing publication receipts.')
    except Exception as caught:
        error = str(caught)
        observation = observe()
    # Archive only existing evidence. This never invokes reconstruction or combat.
    try:
        native_files = inventory(RUN, guard)
        if error is None:
            require(set(native_files) == set(read(RUN/'files.json')) | {'files.json'}, 'Native inventory membership differs.')
            require(all(native_files[n] == h for n, h in read(RUN/'files.json').items()), 'Native inventory hash differs.')
        save(OUTPUT/'native-files.json', native_files)
        save(OUTPUT/'observation.json', observation)
        guard(True)
        save(OUTPUT/'files.json', inventory(OUTPUT, guard))
        guard(True)
    except Exception as caught:
        error = (error+'; ' if error else '')+str(caught)
    save(OUTPUT/'execution-receipt.json', {
        'status': 'CompletedVerified' if error is None else 'TerminalFailure', 'error': error,
        'attempts': 1 if process is not None or (OUTPUT/'native-console.json').exists() else 0, 'retries': 0,
        'additionalChargedSeconds': SECONDS, 'additionalChargedBytes': BYTES,
        'cumulativeChargedSeconds': 1860, 'cumulativeChargedBytes': 1088*MIB,
        'measuredSecondsAfterSealingBeforeReceipt': time.monotonic()-started,
        'sampledHighWaterBytesBeforeReceipt': peak,
        'oversightManifestSha256': sha(OUTPUT/'files.json') if (OUTPUT/'files.json').exists() else None,
        'nativeManifestSha256': sha(RUN/'files.json') if (RUN/'files.json').exists() else None,
        'extraPublicVerifierRuns': 0, 'automaticPromotion': False})
    elapsed, final_bytes = time.monotonic()-started, size(RUN)+size(OUTPUT)
    print(json.dumps({'status': 'CompletedVerified' if error is None else 'TerminalFailure', 'error': error,
                      'secondsIncludingReceipt': elapsed, 'finalCombinedBytes': final_bytes}), flush=True)
    require(elapsed < SECONDS and final_bytes < BYTES, 'Enclosing allowance exceeded at closeout.')


def verify():
    receipt = read(OUTPUT/'execution-receipt.json')
    require(sha(OUTPUT/'files.json') == receipt['oversightManifestSha256'], 'Changed oversight manifest.')
    current = inventory(OUTPUT)
    require(set(current) == set(read(OUTPUT/'files.json')) | {'files.json', 'execution-receipt.json'}, 'Changed oversight membership.')
    require(all(current[n] == h for n, h in read(OUTPUT/'files.json').items()), 'Changed oversight evidence.')
    require(inventory(RUN) == read(OUTPUT/'native-files.json'), 'Changed native evidence.')
    require(observe() == read(OUTPUT/'observation.json'), 'Changed observed receipts.')
    require(sha(PACKAGE/'files.json') == MANIFEST_HASH and sha(PACKAGE/'request.proposed.json') == REQUEST_HASH,
            'Changed admission identity.')
    return {'version': 'tower-selection-execution-review-v1', 'receipt': receipt,
            'process': read(OUTPUT/'process.json') if (OUTPUT/'process.json').exists() else None,
            'observation': observe(), 'nativeInventoryEntries': len(read(OUTPUT/'native-files.json')),
            'oversightInventoryEntries': len(read(OUTPUT/'files.json')),
            'nativeBytes': size(RUN), 'oversightBytes': size(OUTPUT), 'combinedBytes': size(RUN)+size(OUTPUT)}


if __name__ == '__main__':
    require(sys.argv[1:] in (['run'], ['verify']), 'Use run once, or read-only verify.')
    if sys.argv[1:] == ['run']:
        execute()
    else:
        print(json.dumps(verify(), indent=2))
