"""Own the single approved, frozen native fixed-team confirmation; read its receipts afterward.

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
import stat
import sys
import time
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT/'TestResults/fixed-team-confirmation-admission-20260917-03'
OUTPUT = ROOT/'TestResults/fixed-team-confirmation-execution-20260917'
RUN = ROOT/'TestResults/balance/tower-practical-fixed-team-confirmation-20260917'
MIB = 1048576
SECONDS, BYTES = 2400, 1024*MIB
REQUEST_HASH = '315063b481751d273ad17b568c8c785e8bff31738b9a960b7ee4d2db2a67dcf6'
MANIFEST_HASH = '0aa1b905a57b0e931f07139eb122f1b5b7e3eb7696ebb10118b1b20b8090a3fd'
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
    if not root.exists():
        return []
    result, pending = [], [root]
    while pending:
        with os.scandir(pending.pop()) as entries:
            for entry in entries:
                require(not entry.stat(follow_symlinks=False).st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT,
                        'Linked execution artifact.')
                if entry.is_dir(follow_symlinks=False):
                    pending.append(Path(entry.path))
                else:
                    result.append(Path(entry.path))
    return sorted(result)


def size(root):
    if not root.exists():
        return 0
    total, pending = 0, [root]
    while pending:
        with os.scandir(pending.pop()) as entries:
            for entry in entries:
                try:
                    info = entry.stat(follow_symlinks=False)
                except FileNotFoundError:
                    continue  # An active native atomic replacement may remove its temporary file.
                require(not info.st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT, 'Linked execution artifact.')
                if entry.is_dir(follow_symlinks=False):
                    pending.append(Path(entry.path))
                else:
                    total += info.st_size
    return total


def inventory(root, guard=lambda: None):
    result = {}
    for p in paths(root):
        result[p.relative_to(root).as_posix()] = sha(p)
        guard()
    return result


def observe():
    result = {}
    for name in ('result', 'completion', 'closeout', 'failure', 'worker-failure', 'native-audit',
                 'independent-audit', 'history-input', 'confirmation-binding',
                 'admission-phase', 'combat-phase', 'audit-phase'):
        p = RUN/(name+'.json')
        if p.exists():
            try:
                value = read(p)
            except (OSError, ValueError) as error:
                result[name] = {'unreadableArtifact':str(error)}
                continue
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
    ledger = RUN/'seed-ledger.json'
    if ledger.exists():
        try:
            value = read(ledger)
            historical, reserved = set(value['historical']), set(value['reserved'])
            result['reservation'] = {'state':value['reservationState'], 'historicalValues':len(historical),
                'newReservations':len(reserved), 'overlap':len(historical & reserved),
                'cumulativeValues':len(historical | reserved), 'ledgerSha256':sha(ledger)}
        except (OSError, ValueError, KeyError) as error:
            result['reservation'] = {'unreadableArtifact':str(error)}
    return result


def validate_publication(observation, process):
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
            'Native confirmation failed or exceeded its limit; no retry.')
    require(not (RUN/'failure.json').exists() and not (RUN/'worker-failure.json').exists(), 'Native failure marker.')
    result = observation['result']
    require(result['executionStatus'] == 'Complete' and result['integrityStatus'] == 'Verified'
            and observation['completion']['status'] == 'Complete' and observation['completion']['fights'] == 16500
            and observation['completion']['retries'] == 0 and observation['completion']['chargedSeconds'] == 4800
            and observation['completion']['chargedBytes'] == 2304*MIB
            and observation['native-audit']['status'] == 'Passed' and observation['native-audit']['newFights'] == 0
            and result == observation['independent-audit'] == read(RUN/'worker-result.json')
            and observation['attemptCounts'] == {'Started':16500, 'Completed':16500, 'unreadableLines':0},
            'Incomplete or disagreeing publication receipts.')
    require(result['balanceAssessment'] == 'NotAssessed'
            and (result['strengthDecision'], result['adoption']) in
                (('StrongerFixedTeamConfirmed','AdoptFixedTeam'), ('StrengthNotDemonstrated','Hold')),
            'Unexpected endpoint.')
    reservation = observation['reservation']
    require(reservation['state'] == observation['history-input']['reservationState'] == 'Complete'
            and reservation['historicalValues'] == 486374 and reservation['overlap'] == 0
            and 5500 <= reservation['newReservations'] <= 11000
            and reservation['newReservations'] == observation['history-input']['reservedCount']
            and observation['confirmation-binding']['panel'] == 5500, 'Incomplete reservation.')
    q = read(PACKAGE/'request.proposed.json')
    check = read(PACKAGE/'native-check.json')
    require(read(RUN/'request.json') == q and observation['completion']['requestHash'] == check['requestHash'],
            'Request changed during execution.')
    closeout = observation['closeout']
    require(closeout['filesHash'] == sha(RUN/'files.json') and closeout['requestHash'] == check['requestHash']
            and closeout['retainedBytes'] == size(RUN) and closeout['measuredSeconds'] < SECONDS-2,
            'Incomplete native closeout.')
    for phase, limit in q['phases'].items():
        receipt = observation[phase+'-phase']
        require(receipt['phase']['name'] == phase and 0 <= receipt['measuredSeconds'] < limit['seconds']
                and 0 <= receipt['observedBytes'] <= limit['bytes']
                and receipt['chargedSeconds'] == limit['seconds'] and receipt['chargedBytes'] == limit['bytes'],
                'Changed nontransferable phase receipt.')


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
            'basis': 'User replied "Please proceed" to the exact 16500-fight /40-minute /1-GiB run, capped-failure-risk and separate unmetered-engineering question.',
            'gameplayAuthorized': True, 'riskAccepted': True, 'attemptsAllowed': 1, 'retriesAllowed': 0,
            'engineeringTreatmentAccepted': True, 'engineeringChargeSeconds': None, 'engineeringChargeBytes': None,
            'engineeringTreatment': 'Separately disclosed unmetered implementation, orchestration and documentation; unknown cost is not zero and is not refunded from the operational ledger.',
            'requestFileSha256': REQUEST_HASH, 'packageManifestSha256': MANIFEST_HASH,
            'additionalSeconds': SECONDS, 'additionalBytes': BYTES,
            'priorSeconds': 2400, 'priorBytes': 1280*MIB,
            'maximumCumulativeSeconds': 4800, 'maximumCumulativeBytes': 2304*MIB,
            'includes': 'Revalidation, enclosing setup/logs, native run, built-in audits/publication, inventory closeout.',
            'risk': 'Terminal capped failure may yield incomplete evidence and blocking Pending reservations.',
            'excludes': 'Retry, refill, replay, extension, phase transfer, extra public verifier, gameplay configuration deployment.',
            'decision': 'The fixed gate may recommend AdoptFixedTeam only after complete agreeing audits; a negative keeps Hold and both anchors.'})
        shutil.copyfile(Path(__file__), OUTPUT/'execution-source.py')
        shutil.copyfile(PACKAGE/'bounded_output_process.py', OUTPUT/'bounded_output_process.py')
        admission = load('fixed_execution_admission', ROOT/'Balance Harness/analysis/practical-fixed-team-confirmation-admission.py')
        # Add the enclosing deadline/storage guard to the existing unchanged
        # scanner; its returned set and the sealed admission reader are unchanged.
        scan = admission.history_files
        admission.history_files = lambda: scan(guard)
        check = admission.verify()
        require(check == read(ROOT/'Balance Harness/Tower-Practical-Fixed-Team-Confirmation-Admission.json'), 'Admission review differs.')
        save(OUTPUT/'admission-revalidation.json', check)
        guard(True)
        command = read(PACKAGE/'command-after-approval.json')['executableArguments']
        require(command == ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'),
                            'tower-fixed-team-confirmation-run', str(PACKAGE/'request.proposed.json')], 'Command changed.')
        save(OUTPUT/'command.json', {'executableArguments': command, 'cwd': str(ROOT), 'attempt': 1})
        owner = load('fixed_execution_owner', OUTPUT/'bounded_output_process.py')
        print('Launching the approved public fixed-team confirmation exactly once.', flush=True)
        # Reserve 15 seconds inside the total for owner drain and enclosing seal.
        process = owner.run(command, ROOT, OUTPUT/'native-console.json', started+SECONDS-15,
                            cleanup_seconds=1, guard=guard)
        save(OUTPUT/'process.json', process)
        guard(True)
        observation = observe()
        validate_publication(observation, process)
    except Exception as caught:
        error = str(caught)
        observation = observe()
    # Archive only existing evidence. This never invokes reconstruction or combat.
    try:
        native_files = inventory(RUN, guard)
        if error is None:
            require(set(native_files) == set(read(RUN/'files.json')) | {'files.json', 'closeout.json'}, 'Native inventory membership differs.')
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
        'cumulativeChargedSeconds': 4800, 'cumulativeChargedBytes': 2304*MIB,
        'measuredSecondsAfterSealingBeforeReceipt': time.monotonic()-started,
        'sampledHighWaterBytesBeforeReceipt': peak,
        'oversightManifestSha256': sha(OUTPUT/'files.json') if (OUTPUT/'files.json').exists() else None,
        'nativeManifestSha256': sha(RUN/'files.json') if (RUN/'files.json').exists() else None,
        'extraPublicVerifierRuns': 0, 'engineeringTreatmentAccepted': True, 'gameplayConfigurationChanged': False})
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
    require(sha(Path(__file__)) == sha(OUTPUT/'execution-source.py'), 'Changed enclosing execution source.')
    approval = read(OUTPUT/'approval.json')
    require(approval['gameplayAuthorized'] and approval['riskAccepted'] and approval['engineeringTreatmentAccepted']
            and approval['attemptsAllowed'] == 1 and approval['retriesAllowed'] == 0, 'Changed approval.')
    if receipt['status'] == 'CompletedVerified':
        validate_publication(observe(), read(OUTPUT/'process.json'))
    return {'version': 'tower-fixed-team-execution-review-v1', 'receipt': receipt,
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
