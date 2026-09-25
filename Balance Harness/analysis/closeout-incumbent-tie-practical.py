"""Read-only closeout after the wrapper's incorrect two-ledger count assertion.

Never executes a harness command, allocates, fights, resumes or changes the run.
Preserves the original wrapper failure and writes a separate corrective receipt.
"""
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
ADMISSION = ROOT/'TestResults/incumbent-tie-practical-admission-20260922'
EXECUTION = ROOT/'TestResults/incumbent-tie-practical-execution-20260922'
RUN = ROOT/'TestResults/balance/tower-practical-incumbent-tie-20260922'
OUTPUT = ROOT/'TestResults/incumbent-tie-practical-closeout-20260922'
ADMISSION_PIN = '3f580b3216d1da086e4bdc080c6a883e3f1bceaec4028500ed78ccaa585cf034'
RUN_PIN = 'ce03b27bb0b9ba96b6229be85849b1885af4f75e322876bb7877e38d67cadcb6'


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    common = load('common', ADMISSION/'comparison-preparation.py')
    fixed = load('fixed_runner', ROOT/'Balance Harness/analysis/run-incumbent-tie-practical.py')
    require, read, sha, save = common.require, common.read, common.sha, common.save
    require(not OUTPUT.exists(), 'Use a new read-only closeout receipt')
    started = time.monotonic()
    OUTPUT.mkdir()
    admission_files = common.authenticate(ADMISSION, ADMISSION_PIN)
    run_files = common.authenticate(RUN, RUN_PIN)
    execution_files = common.inventory(EXECUTION)
    failure = read(EXECUTION/'failure.json')
    require(failure['status'] == 'StoppedNoRetry' and failure['phase'] == 'closeout'
            and 'Changed final permanent union' in failure['error'], 'Unexpected wrapper failure')
    q = read(RUN/'request.json')
    require(q == read(ADMISSION/'preset/request.json'), 'Changed admitted request')
    result = read(RUN/'result.json')
    audit = read(EXECUTION/'independent-audit.json')
    require(result == read(EXECUTION/'public-audit.log') and result['executionStatus'] == 'Complete'
            and result['integrityStatus'] == 'Verified' and audit['status'] == 'Passed'
            and result['strengthDecision'] == audit['strengthDecision'], 'Incomplete or disagreeing audits')
    processes = {name:read(EXECUTION/(name+'-process.json'))
                 for name in ('preflight', 'public-run', 'public-audit', 'independent-audit')}
    require(all(p['exitCode'] == 0 and not p['timedOut'] and p['activeProcesses'] == 0
                and p['mechanism'] == 'suspended-owned-job-v1' for p in processes.values()), 'Unfinished process tree')
    files, values = common.history(RUN.parent, q)
    expected_files = fixed.expected_history_files(q, RUN, sha)
    reserved = read(RUN/'history-input.json')['reserved']
    prior = read(ADMISSION/'preset/template.json')['excludedCombatSeeds']
    require(files == expected_files and len(files) == 227, 'Unexpected final history membership')
    require(len(reserved) == len(set(reserved)) == 1041 and not set(prior).intersection(reserved)
            and values == sorted(set(prior) | set(reserved)) and len(values) == 539367,
            'Changed final permanent union')
    nested = common.ledger(read(RUN/'study/seed-ledger.json'))
    require(nested == set(values), 'Nested study ledger adds an unexpected value')
    require(common.inventory(EXECUTION) == execution_files, 'Original execution evidence changed')
    common.authenticate(RUN, RUN_PIN)
    common.authenticate(ADMISSION, ADMISSION_PIN)
    save(OUTPUT/'execution-files.json', execution_files)
    save(OUTPUT/'live-history-files.json', files)
    shutil.copyfile(EXECUTION/'failure.json', OUTPUT/'original-wrapper-failure.json')
    for path, name in [(Path(__file__), 'closeout.py'), (ROOT/'Balance Harness/analysis/run-incumbent-tie-practical.py', 'corrected-runner.py')]:
        shutil.copyfile(path, OUTPUT/name)
    retained = fixed.size(EXECUTION)+fixed.size(RUN)
    require(failure['elapsedSeconds'] < 1200 and retained < 1073741824, 'Original workflow exceeded its ceiling')
    receipt = dict(status='ReadOnlyCloseoutVerified', scientificExecution='CompleteVerified', scope='Closed',
         originalWrapperStatus='StoppedNoRetry', originalWrapperExitCode=1,
         correction='The practical run adds three ledger files, including study/seed-ledger.json; it was incorrectly expected to add two',
         previousHistoryFiles=224, addedLedgerFiles=['history-input.json', 'seed-ledger.json', 'study/seed-ledger.json'],
         historyFiles=227, historicalValues=538326, newValuesInCompletedRun=1041, totalExclusions=539367,
         completedFights=3496, strengthDecision=result['strengthDecision'], nativeAudit='Passed', independentAudit='Passed',
         originalWrapperSeconds=failure['elapsedSeconds'], originalRetainedBytes=retained,
         originalMaximumSeconds=1200, originalMaximumBytes=1073741824,
         closeoutSecondsBeforeSeal=time.monotonic()-started, engineeringMaximumSeconds=180, engineeringMaximumBytes=16777216,
         archiveManifestSha256=RUN_PIN, admissionManifestSha256=ADMISSION_PIN,
         originalExecutionInventorySha256=sha(OUTPUT/'execution-files.json'),
         originalArtifactsChanged=False, scientificRetries=0, closeoutFights=0, closeoutNewValues=0,
         processes=processes, admissionFiles=len(admission_files), archiveFiles=len(run_files))
    save(OUTPUT/'closeout.json', receipt)
    save(OUTPUT/'files.json', common.inventory(OUTPUT))
    pin = sha(OUTPUT/'files.json')
    common.authenticate(OUTPUT, pin)
    require(time.monotonic()-started < 178 and fixed.size(OUTPUT) < 16777216, 'Read-only closeout ceiling exceeded')
    save(OUTPUT.with_name(OUTPUT.name+'-pin.json'), dict(status='ReadOnlyCloseoutVerified', manifestSha256=pin,
         archiveManifestSha256=RUN_PIN, seconds=time.monotonic()-started, bytes=fixed.size(OUTPUT), newValues=0, fights=0))
    print(json.dumps(receipt, indent=2))


if __name__ == '__main__':
    main()
