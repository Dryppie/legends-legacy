"""Once-only enclosing owner for the admitted 44,000-fight fixed-family run.

No rebuilding, request rebinding, extra verifier, retries, recovery or resume.
Both scientific audits execute inside the native controller. This owner validates
their receipts, authenticates publication and checks permanent history afterward.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import threading
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
ADMISSION = ROOT/'TestResults/fixed-family-final-admission-20260922'
RUNTIME = ROOT/'TestResults/fixed-family-runtime-admission-20260922'
RUNTIME_PIN = '49fa8a4eac7f40da417308f2aea270fb63f87d1f5c1ba0cbf2f9f1990be136bd'
OVERSIGHT = ROOT/'TestResults/fixed-family-execution-20260922'
RUN = ROOT/'TestResults/balance/tower-fixed-family-confirmation-20260922'
MIB = 1048576
SECONDS, BYTES = 6000, 3072*MIB
NATIVE_SECONDS, NATIVE_BYTES = 5760, 2944*MIB
OUTER_SECONDS, OUTER_BYTES = 240, 128*MIB


def require(ok, message):
    if not ok: raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def save(path, value):
    with path.open('xb') as stream:
        stream.write((json.dumps(value, indent=2, allow_nan=False)+'\n').encode())
        stream.flush(); os.fsync(stream.fileno())


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def size(root):
    if not root.exists(): return 0
    require(not root.is_symlink() and not root.is_junction(), 'Linked output root')
    total, pending = 0, [root]
    while pending:
        with os.scandir(pending.pop()) as entries:
            for entry in entries:
                try: info = entry.stat(follow_symlinks=False)
                except FileNotFoundError: continue  # Concurrent atomic native replacement.
                require(not entry.is_symlink() and not getattr(info, 'st_file_attributes', 0) & 0x400, 'Linked output')
                if entry.is_dir(follow_symlinks=False): pending.append(entry.path)
                else: total += info.st_size
    return total


def check_envelope(elapsed, native_elapsed, native_bytes, outer_bytes):
    require(0 <= native_elapsed <= elapsed and elapsed < SECONDS-2
            and elapsed-native_elapsed < OUTER_SECONDS-2, 'Enclosing time allowance exhausted')
    require(0 <= native_bytes <= NATIVE_BYTES and 0 <= outer_bytes < OUTER_BYTES-65536
            and native_bytes+outer_bytes < BYTES-65536, 'Enclosing storage allowance exhausted')


def validate_request(q):
    require(q['version'] == 'tower-practical-fixed-family-confirmation-v1'
        and q['outputRoot'] == str(RUN) and q['registryRoot'] == str(RUN.parent)
        and q['priorSeconds'] == 18180 and q['priorBytes'] == 13584*MIB
        and q['maximumSeconds'] == 23940 and q['maximumBytes'] == 16528*MIB,
        'Changed admitted request or scoped ledger')
    require(q['phases'] == dict(admission=dict(seconds=240, bytes=256*MIB),
        combat=dict(seconds=3600, bytes=2176*MIB), audit=dict(seconds=1560, bytes=256*MIB)), 'Changed fixed phase limits')


def expected_history(q, run, digest):
    # This fixed-family archive has two authoritative reservation ledgers. It does
    # not create the ordinary practical search's third study/seed-ledger.json.
    result = dict(q['requiredHistory'])
    for name in ('history-input.json', 'seed-ledger.json'):
        result[str(run/name)] = digest(run/name)
    return result


def validate_receipts(q, native_hash, result, completion, closeout, native_audit, independent, worker, phases):
    require(result['executionStatus'] == 'Complete' and result['integrityStatus'] == 'Verified'
        and result == independent == worker and result['balanceAssessment'] == 'NotAssessed', 'Incomplete or disagreeing results')
    require((result['strengthDecision'], result['adoption']) in
        [('StrongerFixedCandidatesConfirmed', 'RecommendFixedCandidates'), ('StrengthNotDemonstrated', 'Hold')], 'Unexpected endpoint')
    require(completion['version'] == q['version'] and completion['status'] == 'Complete' and completion['fights'] == 44000
        and completion['retries'] == 0 and completion['requestHash'] == native_hash
        and completion['chargedSeconds'] == q['maximumSeconds'] and completion['chargedBytes'] == q['maximumBytes'], 'Changed completion charges')
    require(closeout['version'] == q['version'] and closeout['requestHash'] == native_hash
        and 0 <= completion['measuredSecondsBeforeSeal'] <= closeout['measuredSeconds'] < NATIVE_SECONDS-60
        and 0 < closeout['retainedBytes'] <= NATIVE_BYTES-16*MIB, 'Invalid native closeout envelope')
    require(native_audit == dict(status='Passed', studyHash=result['studyHash'], archiveHash=result['archiveHash'], newFights=0), 'Missing native audit')
    for name, limit in q['phases'].items():
        p = phases[name]
        require(p['phase']['name'] == name and 0 <= p['measuredSeconds'] < limit['seconds']
            and 0 <= p['observedBytes'] <= limit['bytes'] and p['chargedSeconds'] == limit['seconds']
            and p['chargedBytes'] == limit['bytes'], 'Changed phase receipt')


def publication(common, q, native_hash, guard):
    require(not any((RUN/n).exists() for n in ('failure.json', 'worker-failure.json')), 'Native failure marker')
    require(read(RUN/'request.json') == q, 'Changed native request')
    result, completion, closeout = (read(RUN/(n+'.json')) for n in ('result', 'completion', 'closeout'))
    validate_receipts(q, native_hash, result, completion, closeout, read(RUN/'native-audit.json'),
        read(RUN/'independent-audit.json'), read(RUN/'worker-result.json'),
        {name: read(RUN/(name+'-phase.json')) for name in q['phases']})
    actual = {}
    for path in common.paths(RUN):
        actual[path.relative_to(RUN).as_posix()] = sha(path); guard()
    manifest = read(RUN/'files.json')
    require(set(actual) == set(manifest)|{'files.json', 'closeout.json'}
        and all(actual.get(n) == h for n, h in manifest.items())
        and closeout['filesHash'] == actual['files.json'] and closeout['retainedBytes'] == size(RUN), 'Changed exact native publication')
    with (RUN/'attempts.jsonl').open(encoding='utf-8-sig') as stream:
        for ordinal in range(1, 44001):
            for kind in ('Started', 'Completed'):
                line = stream.readline()
                require(line.endswith('\n') and json.loads(line) == dict(kind=kind, ordinal=ordinal), 'Incomplete attempt sequence')
        require(stream.read() == '', 'Excess attempts')
    history, values = common.history(RUN.parent, q)
    ledger, allocation = read(RUN/'seed-ledger.json'), read(RUN/'history-input.json')
    prior = read(Path(q['definitionPath']))['excludedCombatSeeds']
    reserved = ledger['reserved']
    require(ledger['reservationState'] == allocation['reservationState'] == 'Complete'
        and ledger['historical'] == prior and allocation['reserved'] == reserved
        and 5500 <= len(reserved) <= 11000 and len(reserved) == len(set(reserved))
        and not set(prior).intersection(reserved) and values == sorted(set(prior)|set(reserved))
        and history == expected_history(q, RUN, sha), 'Changed permanent union or history membership')
    guard(True)
    save(OVERSIGHT/'native-files.json', actual); save(OVERSIGHT/'live-history-files.json', history)
    return dict(result=result, fights=44000, reservations=len(reserved), totalExclusions=len(values),
        historyFiles=len(history), nativeManifestSha256=actual['files.json'], nativeCloseoutSha256=actual['closeout.json'])


def execute(pin):
    started = time.monotonic()
    require(not OVERSIGHT.exists() and not RUN.exists(), 'Existing scope; no retry or resume')
    require(sha(ADMISSION/'files.json') == pin, 'Supply the frozen final-admission manifest pin')
    manifest = read(ADMISSION/'files.json')
    for name in ('launcher.py', 'admission.py', 'bounded_windows_process.py'):
        require(sha(ADMISSION/name) == manifest[name], 'Changed admitted executable helper')
    require(sha(Path(__file__)) == manifest['launcher.py'], 'Use the admitted launcher')
    OVERSIGHT.mkdir()  # Atomic create-only claim, before revalidation or native entry.
    save(OVERSIGHT/'claim.json', dict(status='ClaimedOnceBeforePreflight', admissionManifestSha256=pin, attemptsAllowed=1, retriesAllowed=0))
    owner = load('fixed_family_owner', ADMISSION/'bounded_windows_process.py')
    native_started, native_elapsed, last_scan, last_progress = None, 0, -float('inf'), -float('inf')
    peak, native_bytes, outer_bytes, phase = 0, 0, 0, 'preflight'
    stopped = threading.Event()

    def hard_deadline():
        # Also bounds synchronous final inventory/history reads. Exiting closes
        # any live Job handle, terminating its entire owned process tree.
        while not stopped.wait(.1):
            now = time.monotonic()
            spent_native = now-native_started if native_started is not None else native_elapsed
            if now-started >= SECONDS or now-started-spent_native >= OUTER_SECONDS:
                os._exit(124)

    watchdog = threading.Thread(target=hard_deadline, daemon=True); watchdog.start()

    def guard(force=False):
        nonlocal last_scan, last_progress, peak, native_bytes, outer_bytes
        now = time.monotonic()
        if force or now-last_scan >= 1:
            native_bytes, outer_bytes = size(RUN), size(OVERSIGHT); last_scan = now
            peak = max(peak, native_bytes+outer_bytes)
        spent_native = now-native_started if native_started is not None else native_elapsed
        check_envelope(now-started, spent_native, native_bytes, outer_bytes)
        if now-last_progress >= 30:
            print(json.dumps(dict(phase=phase, elapsedSeconds=round(now-started, 3), sampledCombinedBytes=peak)), flush=True)
            last_progress = now

    try:
        guard(True)
        process = owner.run([sys.executable, '-B', str(ADMISSION/'admission.py'), 'verify', '--manifest-sha256', pin,
            '--execution-claim', str(OVERSIGHT)], ROOT, OVERSIGHT/'preflight.log', started+OUTER_SECONDS-15, cleanup_seconds=1, check=guard)
        save(OVERSIGHT/'preflight-process.json', process)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Preflight failed; scope closed')
        recheck = read(OVERSIGHT/'preflight.log'); require(recheck['status'] == 'ReadyForSingleBoundedExecution', 'Admission not ready')
        q = read(ADMISSION/'request.json'); validate_request(q)
        require(sha(ADMISSION/'request.json') == recheck['requestSha256'], 'Changed request before launch')
        shutil.copyfile(ADMISSION/'launcher.py', OVERSIGHT/'launcher.py')
        shutil.copyfile(ADMISSION/'accounting-decision.json', OVERSIGHT/'accounting-decision.json')
        save(OVERSIGHT/'launch-intent.json', dict(admissionManifestSha256=pin, requestSha256=recheck['requestSha256'],
            attempts=1, retries=0, additionalChargedSeconds=SECONDS, additionalChargedBytes=BYTES,
            cumulativeChargedSeconds=q['priorSeconds']+SECONDS, cumulativeChargedBytes=q['priorBytes']+BYTES))
        guard(True); phase = 'native-confirmation'; native_started = time.monotonic()
        process = owner.run(['dotnet', str(RUNTIME/'runtime/BalanceHarness.dll'), 'tower-fixed-family-confirmation-run', str(ADMISSION/'request.json')],
            ROOT, OVERSIGHT/'native-console.log', min(native_started+NATIVE_SECONDS, started+SECONDS-60), cleanup_seconds=1, check=guard)
        native_elapsed = time.monotonic()-native_started; native_started = None
        save(OVERSIGHT/'native-process.json', process); guard(True)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Native run failed; no retry')
        phase = 'publication-and-history'
        admission = load('final_admission_closeout', ADMISSION/'admission.py'); common = admission.runtime_helper().common_module()
        observation = publication(common, q, recheck['nativeRequestHash'], guard)
        common.authenticate(RUNTIME, RUNTIME_PIN); common.authenticate(ADMISSION, pin); guard(True)
        save(OVERSIGHT/'completion.json', dict(status='CompletedVerified', **observation,
            additionalChargedSeconds=SECONDS, additionalChargedBytes=BYTES,
            cumulativeChargedSeconds=q['priorSeconds']+SECONDS, cumulativeChargedBytes=q['priorBytes']+BYTES,
            nativeSeconds=native_elapsed, elapsedSecondsBeforeSeal=time.monotonic()-started,
            sampledHighWaterBytes=peak, extraPublicVerifiers=0, retries=0, resume=False))
        save(OVERSIGHT/'files.json', common.inventory(OVERSIGHT)); guard(True)
        save(OVERSIGHT/'closeout.json', dict(status='CompletedVerified', manifestSha256=sha(OVERSIGHT/'files.json'),
            secondsBeforeReceipt=time.monotonic()-started, nativeSeconds=native_elapsed, sampledCombinedBytes=peak,
            additionalChargedSeconds=SECONDS, additionalChargedBytes=BYTES))
        guard(True)
        print(json.dumps(dict(status='CompletedVerified', **observation), indent=2), flush=True)
    except BaseException as error:
        save(OVERSIGHT/'failure.json', dict(status='TerminalFailureNoRetry', phase=phase, reason=str(error),
            elapsedSeconds=time.monotonic()-started, additionalChargedSeconds=SECONDS, additionalChargedBytes=BYTES,
            cumulativeChargedSeconds=24180, cumulativeChargedBytes=16656*MIB,
            reservationsPreserved=True, retries=0, resume=False))
        raise
    finally:
        stopped.set(); watchdog.join(timeout=1)


class LauncherTests(unittest.TestCase):
    def test_total_outer_and_storage_limits_do_not_transfer(self):
        check_envelope(100, 20, MIB, MIB)
        for args in [(5999, 5760, MIB, MIB), (500, 250, MIB, MIB), (100, 0, NATIVE_BYTES+1, 0),
                     (100, 0, 1, OUTER_BYTES), (100, 101, 1, 1)]:
            with self.assertRaises(ValueError): check_envelope(*args)

    def test_history_membership_has_exactly_two_new_ledgers(self):
        q = dict(requiredHistory={'old': 'a'})
        result = expected_history(q, Path('new'), lambda p: p.name)
        self.assertEqual({'old', str(Path('new/history-input.json')), str(Path('new/seed-ledger.json'))}, set(result))
        self.assertEqual({'old': 'a'}, q['requiredHistory'])

    def test_request_cannot_expand_or_rebind(self):
        q = read(RUNTIME/'compatibility-request.json'); validate_request(q)
        for edit in ({'maximumSeconds': 24180}, {'priorSeconds': 0}, {'maximumBytes': q['maximumBytes']+1}, {'outputRoot': 'elsewhere'}):
            with self.assertRaises(ValueError): validate_request(dict(q, **edit))

    def fixture(self):
        q = read(RUNTIME/'compatibility-request.json')
        result = dict(executionStatus='Complete', integrityStatus='Verified', balanceAssessment='NotAssessed',
            strengthDecision='StrengthNotDemonstrated', adoption='Hold', studyHash='a'*64, archiveHash='b'*64)
        completion = dict(version=q['version'], status='Complete', fights=44000, retries=0, requestHash='c'*64,
            chargedSeconds=q['maximumSeconds'], chargedBytes=q['maximumBytes'], measuredSecondsBeforeSeal=99)
        closeout = dict(version=q['version'], requestHash='c'*64, measuredSeconds=100, retainedBytes=100)
        audit = dict(status='Passed', studyHash='a'*64, archiveHash='b'*64, newFights=0)
        phases = {n: dict(phase=dict(name=n), measuredSeconds=1, observedBytes=1, chargedSeconds=p['seconds'], chargedBytes=p['bytes']) for n, p in q['phases'].items()}
        return [q, 'c'*64, result, completion, closeout, audit, copy.deepcopy(result), copy.deepcopy(result), phases]

    def test_complete_positive_and_negative_receipts_accepted(self):
        args = self.fixture(); validate_receipts(*args)
        for index in (2, 6, 7):
            args[index].update(strengthDecision='StrongerFixedCandidatesConfirmed', adoption='RecommendFixedCandidates')
        validate_receipts(*args)

    def test_incomplete_disagreement_wrong_charge_and_phase_rejected(self):
        for index, key, value in [(2, 'integrityStatus', 'Failed'), (3, 'fights', 43999), (3, 'retries', 1),
                                  (3, 'chargedSeconds', 24180), (4, 'measuredSeconds', 5700), (5, 'newFights', 1)]:
            args = self.fixture(); args[index][key] = value
            with self.assertRaises(ValueError): validate_receipts(*args)
        args = self.fixture(); args[-1]['audit']['measuredSeconds'] = 1560
        with self.assertRaises(ValueError): validate_receipts(*args)

    def test_owned_timeout_is_terminal_with_no_active_children(self):
        owner = load('family_fixture_owner', RUNTIME/'bounded_windows_process.py')
        with tempfile.TemporaryDirectory(prefix='family-owner-') as directory:
            result = owner.run([sys.executable, '-B', '-c', 'import time; time.sleep(5)'], ROOT,
                Path(directory)/'timeout.log', time.monotonic()+.2, cleanup_seconds=1)
            self.assertTrue(result['timedOut']); self.assertEqual(0, result['activeProcesses'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['run', 'self-test'])
    parser.add_argument('--manifest-sha256')
    args = parser.parse_args()
    if args.command == 'self-test':
        sys.exit(not unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(LauncherTests)).wasSuccessful())
    else:
        require(args.manifest_sha256 is not None, 'Supply the independently retained final-admission pin')
        execute(args.manifest_sha256)
