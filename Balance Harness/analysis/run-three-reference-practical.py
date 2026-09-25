"""Once-only owned execution of the admitted three-reference practical request.

The remaining 1,200-second /1,536-MiB ceiling covers preflight, execution, audits and closeout.
No input rebinding, seed derivation, combat implementation, retries or resume.
"""
import datetime as dt
import importlib.util
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import time
import traceback

ROOT = Path(__file__).resolve().parents[2]
ADMISSION = ROOT/'TestResults/three-reference-native-admission-20260922'
PIN = '001eb3e1a8642f3168f15f4ebc81ec65f33235cbae8ae1e2bc5104c61646142d'
PACKAGE = ROOT/'TestResults/three-reference-practical-execution-20260922'
RUN = ROOT/'TestResults/balance/tower-practical-three-reference-20260922'
CLOSEOUT = ROOT/'TestResults/three-reference-admission-closeout-20260922'
CLOSEOUT_PIN = '65cde20aabb7deaffcd889b0e685a9bd26830d63be4678db75a2f4506956d213'
FAILURE_PIN = '8e277d2ad609fe1dd00063f6a9b27930ee15f6a82389e07d933c907613be762e'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
SECONDS, BYTES = 1200, 1536*1048576


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def size(root):
    if not root.exists():
        return 0
    total, pending = 0, [root]
    while pending:
        with os.scandir(pending.pop()) as entries:
            for entry in entries:
                try:
                    stat = entry.stat(follow_symlinks=False)
                except FileNotFoundError:
                    continue  # Concurrent atomic publication can remove a temporary file.
                if entry.is_symlink() or getattr(stat, 'st_file_attributes', 0) & 0x400:
                    raise ValueError('Linked output: '+entry.path)
                if entry.is_dir(follow_symlinks=False):
                    pending.append(entry.path)
                else:
                    total += stat.st_size
    return total


def expected_history_files(request, run, digest):
    # Ordinary practical studies add their own schedule ledger inside the study
    # archive as well as the outer reservation and complete-union ledgers.
    expected = dict(request['requiredHistory'])
    for relative in ('history-input.json', 'seed-ledger.json', 'study/seed-ledger.json'):
        path = run/relative
        expected[str(path)] = digest(path)
    return expected


def main():
    started, utc = time.monotonic(), dt.datetime.now(dt.timezone.utc)
    common_path = CLOSEOUT/'comparison-preparation.py'
    if hashlib.sha256(common_path.read_bytes()).hexdigest() != COMMON_PIN:
        raise ValueError('Changed pinned history/authentication helper')
    common = load('preparation_common', common_path)
    require, read, sha, save = common.require, common.read, common.sha, common.save
    require(not PACKAGE.exists() and not RUN.exists(), 'Existing execution claim; no retry or resume')
    require(sha(ADMISSION/'files.json') == PIN, 'Changed admission manifest')
    common.authenticate(CLOSEOUT, CLOSEOUT_PIN)
    owner_path = CLOSEOUT/'bounded_windows_process.py'
    require(sha(owner_path) == read(CLOSEOUT/'files.json')['bounded_windows_process.py'], 'Changed process owner')
    PACKAGE.mkdir()
    phase, peak, last_sample, last_progress, measured = 'preflight', 0, -float('inf'), -float('inf'), 0
    owner = load('owned_execution', owner_path)

    def guard(force=False):
        nonlocal peak, last_sample, last_progress, measured
        now = time.monotonic()
        require(now-started < SECONDS-2, 'Enclosing execution deadline reached')
        if force or now-last_sample >= 1:
            measured = size(PACKAGE)+size(RUN)
            peak = max(peak, measured)
            last_sample = now
        require(measured < BYTES-65536, 'Enclosing retained-storage ceiling reached')
        if now-last_progress >= 25:
            attempts = RUN/'attempts.jsonl'
            rows = attempts.read_text().splitlines() if attempts.exists() else []
            completed = sum('"Completed"' in row for row in rows)
            print(json.dumps(dict(phase=phase, completedFights=completed,
                                  elapsedSeconds=round(now-started, 3), retainedBytes=measured)), flush=True)
            last_progress = now

    def command(name, arguments):
        guard(True)
        receipt = owner.run(arguments, ROOT, PACKAGE/(name+'.log'), started+SECONDS-3,
                            cleanup_seconds=1, check=guard)
        save(PACKAGE/(name+'-process.json'), receipt)
        guard(True)
        require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                name+' failed; preserve this claim without retry')

    try:
        save(PACKAGE/'authorization.json', dict(status='AuthorizedOnce',
             basis='User said Please proceed after admission and the explicit one-search 4528-fight/1200-second/1536-MiB remaining-envelope handoff',
             admissionPackage=str(ADMISSION), admissionManifestSha256=PIN,
             admissionFailureSha256=FAILURE_PIN, admissionCloseout=str(CLOSEOUT), admissionCloseoutManifestSha256=CLOSEOUT_PIN,
             requestSha256='68de8e4a5c72d4144e7decd871cbdca663a853e324151eda528f57640cdf4ea8',
             maximumSeconds=SECONDS, maximumBytes=BYTES, maximumFights=4528, maximumAcceptedValues=1041,
             retries=0, resume=False, historicalBudgetTransfer=False, startedAt=utc.isoformat(),
             accounting='Shared enclosing ceiling from preflight through audit and closeout; prior admission is conservatively charged at 600 seconds /512 MiB; earlier engineering remains separately disclosed'))
        source = ROOT/'Balance Harness/analysis'
        for original, name in [(Path(__file__), 'run.py'), (source/'audit-three-reference-practical.py', 'audit.py'),
                               (source/'practical-selection-ties.py', 'selection-arithmetic.py'), (owner_path, 'bounded_windows_process.py')]:
            shutil.copyfile(original, PACKAGE/name)
        command('preflight', [sys.executable, '-B', '-X', 'utf8', str(CLOSEOUT/'verify.py'), 'reconcile',
                              '--manifest-sha256', PIN, '--failure-sha256', FAILURE_PIN])
        require(read(PACKAGE/'preflight.log')['status'] == 'ThreeReferenceNativeAdmittedNoReservation', 'Preflight did not pass')
        q = read(ADMISSION/'preset/request.json')
        require(q['maximumSeconds'] == 1800 and q['maximumBytes'] == 2*1073741824
                and q['outputRoot'] == str(RUN) and q['priorSeconds'] == 600 and q['priorBytes'] == 512*1048576
                and q['maximumSeconds']-q['priorSeconds'] == SECONDS and q['maximumBytes']-q['priorBytes'] == BYTES,
                'Changed admitted execution envelope')
        require(sha(ADMISSION/'preset/request.json') == read(PACKAGE/'authorization.json')['requestSha256'], 'Changed admitted request')
        save(PACKAGE/'launch-intent.json', dict(requestSha256=sha(ADMISSION/'preset/request.json'),
             enclosingElapsedSeconds=time.monotonic()-started, attempts=1, retries=0))
        phase = 'search'
        command('public-run', ['dotnet', str(ADMISSION/'runtime/BalanceHarness.dll'),
                               'tower-practical-search-allocate-run', str(ADMISSION/'preset/request.json')])
        result = read(RUN/'result.json')
        require(result['integrityStatus'] == 'Verified' and result['executionStatus'] == 'Complete', 'Incomplete native publication')
        run_pin = sha(RUN/'files.json')
        save(PACKAGE/'archive-pin.json', dict(outputRoot=str(RUN), manifestSha256=run_pin,
             studyManifestSha256=sha(RUN/'study/files.json'), requestSha256=sha(RUN/'request.json')))
        common.authenticate(RUN, run_pin)
        phase = 'native-audit'
        command('public-audit', ['dotnet', str(RUN/'study/executable/BalanceHarness.dll'), 'tower-practical-search-verify', str(RUN)])
        phase = 'independent-audit'
        command('independent-audit', [sys.executable, '-B', str(PACKAGE/'audit.py'), str(PACKAGE), str(RUN)])
        audit = read(PACKAGE/'independent-audit.json')
        require(audit['status'] == 'Passed' and audit['newValues'] == 1041 and audit['totalExclusions'] == 551408
                and audit['completedFights'] <= 4528, 'Independent audit did not establish the declared scope')
        phase = 'closeout'
        common.authenticate(RUN, run_pin)
        common.authenticate(CLOSEOUT, CLOSEOUT_PIN)
        admission_verifier = load('reconciled_admission', CLOSEOUT/'verify.py')
        admission_verifier.authenticate_admission(PIN, FAILURE_PIN)
        files, values = common.history(RUN.parent, q)
        reserved = read(RUN/'history-input.json')['reserved']
        expected = sorted(set(read(ADMISSION/'preset/template.json')['excludedCombatSeeds']) | set(reserved))
        require(values == expected and len(values) == 551408
                and files == expected_history_files(q, RUN, sha), 'Changed final permanent union or membership')
        save(PACKAGE/'live-history-files.json', files)
        guard(True)
        save(PACKAGE/'completion.json', dict(status='ThreeReferencePracticalExecutionVerified', scope='Closed',
             secondsThroughAudits=time.monotonic()-started, priorSeconds=600, priorBytes=512*1048576,
             cumulativeChargedSeconds=600+time.monotonic()-started, cumulativeChargedBytes=512*1048576+measured,
             enclosingMaximumSeconds=SECONDS, enclosingMaximumBytes=BYTES,
             retainedBytesBeforeCloseout=measured, observedPeakBytes=peak, archiveManifestSha256=run_pin,
             completedFights=audit['completedFights'], newValues=1041, historicalValues=550367,
             totalExclusions=551408, historyFiles=len(files), strengthDecision=result['strengthDecision'],
             selectedPartyId=result['selectedPartyId'], tieRuleChangedChoice=audit['tieRuleChangedChoice'],
             balanceAssessment=result['balanceAssessment'], nativeAudit='Passed', independentAudit='Passed',
             retries=0, resume=False, newAuditFights=0, newAuditValues=0))
        save(PACKAGE/'files.json', common.inventory(PACKAGE))
        guard(True)
        pin = sha(PACKAGE/'files.json')
        common.authenticate(PACKAGE, pin)
        guard(True)
        save(PACKAGE.with_name(PACKAGE.name+'-pin.json'), dict(status='ExecutionAndAuditsVerified',
             manifestSha256=pin, seconds=time.monotonic()-started, retainedBytes=measured,
             maximumSeconds=SECONDS, maximumBytes=BYTES, archiveManifestSha256=run_pin))
        print(json.dumps(read(PACKAGE/'completion.json'), indent=2), flush=True)
    except BaseException:
        failure = dict(status='StoppedNoRetry', scope='Closed', phase=phase,
                       elapsedSeconds=time.monotonic()-started, error=traceback.format_exc(),
                       reservationsPreserved=True, retries=0, resume=False)
        save(PACKAGE/'failure.json', failure)
        print(json.dumps(failure, indent=2), flush=True)
        raise


if __name__ == '__main__':
    main()
