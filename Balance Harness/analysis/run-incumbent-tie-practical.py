"""Once-only owned execution of the admitted practical incumbent-tie request.

The 1,200-second /1-GiB ceiling covers preflight, execution, audits and closeout.
No input rebinding, seed derivation, combat implementation, retries or resume.
"""
import datetime as dt
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time
import traceback

ROOT = Path(__file__).resolve().parents[2]
ADMISSION = ROOT/'TestResults/incumbent-tie-practical-admission-20260922'
PIN = '3f580b3216d1da086e4bdc080c6a883e3f1bceaec4028500ed78ccaa585cf034'
PACKAGE = ROOT/'TestResults/incumbent-tie-practical-execution-20260922'
RUN = ROOT/'TestResults/balance/tower-practical-incumbent-tie-20260922'
SECONDS, BYTES = 1200, 1073741824


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
    common = load('preparation_common', ADMISSION/'comparison-preparation.py')
    require, read, sha, save = common.require, common.read, common.sha, common.save
    require(not PACKAGE.exists() and not RUN.exists(), 'Existing execution claim; no retry or resume')
    require(sha(ADMISSION/'files.json') == PIN, 'Changed admission manifest')
    owner_path = ADMISSION/'bounded_windows_process.py'
    require(sha(owner_path) == read(ADMISSION/'files.json')['bounded_windows_process.py'], 'Changed process owner')
    started, utc = time.monotonic(), dt.datetime.now(dt.timezone.utc)
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
             basis='User said Please proceed after admission and the explicit one-search 3496-fight/1200-second/1-GiB handoff',
             admissionPackage=str(ADMISSION), admissionManifestSha256=PIN,
             requestSha256='9e74fc1d755be81f3b3e02d2fc7a9c5e78742de71a13d2f09edfae69f988ab81',
             maximumSeconds=SECONDS, maximumBytes=BYTES, maximumFights=3496, maximumAcceptedValues=1041,
             retries=0, resume=False, historicalBudgetTransfer=False, startedAt=utc.isoformat(),
             accounting='Shared enclosing ceiling from preflight through audit and closeout; prior admission was separate engineering work'))
        source = ROOT/'Balance Harness/analysis'
        for original, name in [(Path(__file__), 'run.py'), (source/'audit-incumbent-tie-practical.py', 'audit.py'),
                               (source/'practical-selection-ties.py', 'selection-arithmetic.py'), (owner_path, 'bounded_windows_process.py')]:
            shutil.copyfile(original, PACKAGE/name)
        command('preflight', [sys.executable, '-B', str(ADMISSION/'prepare.py'), 'verify', '--manifest-sha256', PIN])
        require(read(PACKAGE/'preflight.log')['status'] == 'PracticalPresetAdmittedNoReservation', 'Preflight did not pass')
        q = read(ADMISSION/'preset/request.json')
        require(q['maximumSeconds'] == SECONDS and q['maximumBytes'] == BYTES
                and q['outputRoot'] == str(RUN) and q['priorSeconds'] == q['priorBytes'] == 0,
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
        require(audit['status'] == 'Passed' and audit['newValues'] == 1041 and audit['totalExclusions'] == 539367
                and audit['completedFights'] <= 3496, 'Independent audit did not establish the declared scope')
        phase = 'closeout'
        common.authenticate(RUN, run_pin)
        common.authenticate(ADMISSION, PIN)
        files, values = common.history(RUN.parent, q)
        reserved = read(RUN/'history-input.json')['reserved']
        expected = sorted(set(read(ADMISSION/'preset/template.json')['excludedCombatSeeds']) | set(reserved))
        require(values == expected and len(values) == 539367
                and files == expected_history_files(q, RUN, sha), 'Changed final permanent union or membership')
        save(PACKAGE/'live-history-files.json', files)
        guard(True)
        save(PACKAGE/'completion.json', dict(status='PracticalPresetExecutionVerified', scope='Closed',
             secondsThroughAudits=time.monotonic()-started, enclosingMaximumSeconds=SECONDS, enclosingMaximumBytes=BYTES,
             retainedBytesBeforeCloseout=measured, observedPeakBytes=peak, archiveManifestSha256=run_pin,
             completedFights=audit['completedFights'], newValues=1041, historicalValues=538326,
             totalExclusions=539367, historyFiles=len(files), strengthDecision=result['strengthDecision'],
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
