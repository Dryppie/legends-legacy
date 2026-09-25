"""Read-only publication/accounting/history closeout; never starts or resumes combat.

Both scientific audits are performed by the admitted owner. This consumes their
sealed results, checks publication and refreshes the complete permanent exclusion
union. It writes its receipt outside the scientific registry and archive.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import time

ROOT = Path(__file__).resolve().parents[2]
ADMISSION = ROOT/'TestResults/reference-exploration-comparison-admission-20260922'
ADMISSION_PIN = '55d47a7cf3b560baebf78435eb9089677fc880ce6f7d9e7f010dec22b4d03361'
REQUEST_PIN = '58f787b5a9347ca6a3a225c04ec822c97235d9a93bec0a8a8ca49294487e51db'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
RUN = ROOT/'TestResults/balance/tower-reference-exploration-comparison-20260922'
VERSION = 'tower-reference-exploration-comparison-v1'
ORIGINAL_VERSION = VERSION
OFFSET_VERSION = 'tower-reference-exploration-offset-comparison-v1'
SCREENING_VERSION = 'tower-practical-fresh-screening-comparison-v1'
THREE_TIE_VERSION = 'tower-three-reference-tie-comparison-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


assert hashlib.sha256((ADMISSION/'comparison-preparation.py').read_bytes()).hexdigest() == COMMON_PIN
common = module('exploration_closeout_common', ADMISSION/'comparison-preparation.py')
read, sha, require, save = common.read, common.sha, common.require, common.save


def configure_offset():
    global ADMISSION, ADMISSION_PIN, REQUEST_PIN, RUN, VERSION
    ADMISSION = ROOT/'TestResults/reference-exploration-offset-comparison-admission-20260922'
    ADMISSION_PIN = '160db185b5c8ed49e045338d0590d2a8a51e57ab93432955d89e5db4b9fd5ff6'
    REQUEST_PIN = 'dcca850da20a21aeeff850e7adb9951f8adf4b80292dab2a82d759e31194bed1'
    RUN = ROOT/'TestResults/balance/tower-reference-exploration-offset-comparison-20260922'
    VERSION = OFFSET_VERSION


def configure_screening():
    global ADMISSION, ADMISSION_PIN, REQUEST_PIN, RUN, VERSION
    ADMISSION = ROOT/'TestResults/practical-fresh-screening-admission-20260922'
    ADMISSION_PIN = 'e6eae89b36a30b4bf167c988ff3202c1148880765f559c0d314e67f7d4818caa'
    REQUEST_PIN = '9e7c7a8140cbc1d0006178bb99a264887d268a70448845da84c71fc9f24e1756'
    RUN = ROOT/'TestResults/balance/tower-practical-fresh-screening-comparison-20260922'
    VERSION = SCREENING_VERSION


def configure_three_tie():
    global ADMISSION, ADMISSION_PIN, REQUEST_PIN, RUN, VERSION
    ADMISSION = ROOT/'TestResults/three-reference-tie-admission-20260923'
    ADMISSION_PIN = '90b9a5a9815454ba1169d64f61cc8b7a950597cfa56bc1c9380c53155538f1c5'
    REQUEST_PIN = '24dd41a1ec3a3b42125e81210b3f27ffc277d996c9ade2dcd3e5efb6528ce6a2'
    RUN = ROOT/'TestResults/balance/tower-three-reference-tie-comparison-20260923'
    VERSION = THREE_TIE_VERSION


def reproduce_primary(audit, pairs, historical):
    # The original sealed auditor predates explicit protocol selection.
    return audit.endpoint(pairs, historical) if VERSION == ORIGINAL_VERSION else audit.endpoint(pairs, historical, VERSION)


def classify_reservation(audit, entropy, history):
    # Screening's shared auditor needs a version; the other saved auditors own
    # their reservation shape (24,984 values for the selector comparison).
    return audit.classify(entropy, history, VERSION) if VERSION == SCREENING_VERSION else audit.classify(entropy, history)


def verify_selector_endpoint(result):
    """The selector comparison leaves absolute wins unmeasured on identical outputs."""
    pairs = result['pairs']
    require(result['version'] == THREE_TIE_VERSION and len(pairs) == 24
            and [p['restart'] for p in pairs] == list(range(1,25)) and result['denominator'] == 24000,
            'Changed selector-comparison family or denominator')
    active = 0
    for p in pairs:
        require(p['identical'] == (p['baselineParty'] == p['candidateParty']), 'Changed selector identity equivalence')
        if p['identical']:
            require(p['baselineWins'] is None and p['candidateWins'] is None
                    and p['gains'] == p['losses'] == p['difference'] == 0, 'Identical outputs have unmeasured absolute wins and zero difference')
        else:
            active += 1
            a,b,g,l = p['baselineWins'],p['candidateWins'],p['gains'],p['losses']
            require(all(type(n) is int for n in (a,b,g,l)) and 0 <= a <= 1000 and 0 <= b <= 1000
                    and 0 <= g <= min(b,1000-a) and 0 <= l <= min(a,1000-b)
                    and g+l <= 1000 and b-a == g-l and p['difference'] == (g-l)/1000,
                    'Invalid differing-output paired counts')
    require(result['activeRestarts'] == active and result['fights'] == 12672+2000*active,
            'Changed selector search or confirmation accounting')


def process(value):
    require(value['exitCode'] == 0 and not value['timedOut'] and value['activeProcesses'] == 0
            and value['totalProcesses'] >= 1 and value['mechanism'] == 'suspended-owned-job-v1', 'Incomplete owned process tree')


def closeout(pin, output):
    started = time.monotonic()
    require(output.resolve().is_relative_to(ROOT.resolve()) and not output.resolve().is_relative_to(RUN.parent.resolve()),
            'Closeout evidence must be outside the scientific registry')
    require(output.is_dir() and not (output/'closeout.json').exists(), 'Use new closeout evidence')
    common.authenticate(ADMISSION, ADMISSION_PIN)
    files = common.authenticate(RUN, pin)
    require(not (RUN/'failure.json').exists() and sha(RUN/'request.json') == sha(ADMISSION/'request.json') == REQUEST_PIN,
            'Failed comparison or changed admitted request')
    q, launch, completion = read(RUN/'request.json'), read(RUN/'launch.json'), read(RUN/'completion.json')
    native, independent, result = read(RUN/'native-receipt.json'), read(RUN/'independent-audit.json'), read(RUN/'result.json')
    require(q['version'] == launch['version'] == completion['version'] == native['version'] == result['version'] == VERSION
            and launch['requestFileHash'] == completion['requestFileHash'] == native['requestFileHash'] == independent['requestFileHash'] == REQUEST_PIN,
            'Changed version or request binding')
    require(q['maximumSeconds'] == 10800 and q['maximumBytes'] == 6442450944 and q['priorSeconds'] == 600 and q['priorBytes'] == 536870912
            and launch['maximumSeconds'] == 10200 and launch['maximumBytes'] == 5905580032
            and launch['nativeMaximumSeconds'] == 10080 and launch['nativeMaximumBytes'] == 5637144576, 'Changed cumulative envelope')
    require(completion['status'] == 'Complete' and completion['retries'] == 0 and native['status'] == result['status'] == 'Verified'
            and independent['status'] == 'Passed' and native['newAuditFights'] == independent['newFights'] == independent['newValues'] == 0,
            'Incomplete scientific audits')
    process(completion['process'])
    process(read(RUN/'native-audit-process.json'))
    process(read(RUN/'independent-audit-process.json'))
    retained = sum(p.stat().st_size for p in common.paths(RUN))
    require(0 <= completion['seconds'] < 10200 and retained <= completion['observedBytes'] <= 5905580032
            and completion['chargedSeconds'] == completion['seconds']+600
            and completion['chargedBytes'] == completion['observedBytes']+536870912
            and native['measuredSeconds'] < 10080 and native['observedBytes'] < 5637144576, 'Invalid time or retained-byte accounting')
    require(result == independent['result'] and native['fights'] == result['fights']
            and native['archiveHash'] == sha(RUN/'study/files.json'), 'Audit or physical fight count disagreement')
    require(sha(RUN/'auditor.py') == q['auditorHash'], 'Changed independent auditor')
    audit = module('exploration_saved_auditor', RUN/'auditor.py')
    allocation, template = read(RUN/'allocation.json'), read(RUN/'template.json')
    primary = reproduce_primary(audit, result['pairs'], len(template['excludedCombatSeeds']))
    require(audit.same_numbers(primary, result if VERSION == THREE_TIE_VERSION else dict(result, descriptiveViews=[])), 'Changed primary decision or denominator')
    if VERSION == THREE_TIE_VERSION:
        verify_selector_endpoint(result)
    assigned, reserved, collisions, duplicates = classify_reservation(audit, (RUN/'entropy.bin').read_bytes(), template['excludedCombatSeeds'])
    require(allocation['selected'] == assigned and allocation['reserved'] == reserved and len(assigned) == (24984 if VERSION == THREE_TIE_VERSION else 12588 if VERSION == SCREENING_VERSION else 12492),
            'Changed full reservation or tail')
    old = read(ADMISSION/'history-files.json')
    history, values = common.history(RUN.parent, q)
    added = set(history)-set(old)
    require(all(history.get(name) == value for name, value in old.items()) and added == {str(RUN/'history-input.json'), str(RUN/'seed-ledger.json')}
            and values == sorted(set(template['excludedCombatSeeds']) | set(reserved)), 'Changed global history or incomplete reservation union')
    require(len(history) == len(old)+2 and len(values) == len(template['excludedCombatSeeds'])+len(reserved), 'Historical overlap or unexpected ledger count')
    # Recheck the immutable seal after the live scan, before publishing the receipt.
    require(sha(RUN/'files.json') == pin and all(sha(RUN/name) == value for name, value in files.items()), 'Archive changed during closeout')
    status = {ORIGINAL_VERSION: 'ReferenceExplorationComparisonExecutionVerified',
              OFFSET_VERSION: 'ReferenceExplorationOffsetComparisonExecutionVerified',
              SCREENING_VERSION: 'FreshScreeningComparisonExecutionVerified',
              THREE_TIE_VERSION: 'ThreeReferenceTieComparisonExecutionVerified'}[VERSION]
    receipt = dict(status=status, scope='Closed', archiveManifestSha256=pin,
        admissionManifestSha256=ADMISSION_PIN, requestSha256=REQUEST_PIN, result=result,
        completedFights=result['fights'], assignedValues=len(assigned), newlyReservedValues=len(reserved), unusedReservedTail=len(reserved)-len(assigned),
        historicalCollisions=collisions, batchDuplicates=duplicates, historicalValues=len(template['excludedCombatSeeds']),
        totalExclusions=len(values), historyFiles=len(history), priorHistoryPreserved=True,
        executionSeconds=completion['seconds'], retainedBytes=retained, chargedSeconds=completion['chargedSeconds'], chargedBytes=completion['chargedBytes'],
        nativeAudit='Passed', independentAudit='Passed', scientificLaunches=1, retries=0, resume=False,
        closeoutNewFights=0, closeoutNewValues=0, closeoutSeconds=time.monotonic()-started,
        defaultsChanged=False, teamAdoption=False, confidenceScope='Conditional on the '+('twenty-four' if VERSION == THREE_TIE_VERSION else 'twelve')+' frozen output pairs, not future search roots.',
        engineeringAccounting='Read-only closeout and documentation separately disclosed; older incomplete totals unchanged.')
    save(output/'live-history-files.json', history)
    save(output/'closeout.json', receipt)
    print(json.dumps({key: value for key, value in receipt.items() if key != 'result'}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    versions = parser.add_mutually_exclusive_group()
    versions.add_argument('--offset', action='store_true', help='Close the separately admitted owner-offset comparison.')
    versions.add_argument('--screening', action='store_true', help='Close the separately admitted fresh-screening comparison.')
    versions.add_argument('--three-tie', action='store_true', help='Close the separately admitted three-reference selector comparison.')
    args = parser.parse_args()
    if args.offset:
        configure_offset()
    elif args.screening:
        configure_screening()
    elif args.three_tie:
        configure_three_tie()
    closeout(args.manifest_sha256, args.output.resolve())
