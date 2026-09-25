"""Prepare and authenticate one bounded, seed-free adaptive pilot admission.

Only the native comparison-check command is dispatched. Stored synthetic observations
exercise compatibility; no scientific search, entropy draw, retry or resume is exposed.
The separate 600-second / 512-MiB admission is charged in full, including on failure.
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
import threading
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
CAPTURE = ROOT/'TestResults/three-reference-native-admission-20260922'
CLOSEOUT = ROOT/'TestResults/three-reference-admission-closeout-20260922'
BUILD = ROOT/'TestResults/adaptive-comparison-final-build-20260923/bin'
FIXTURE = ROOT/'TestResults/adaptive-comparison-final-fixture-20260923/study'
PREVIOUS = ROOT/'TestResults/balance/tower-three-reference-tie-comparison-20260923'
HISTORY = ROOT/'TestResults/three-reference-confirmation-admission-tooling-20260923/live-history-files.json'
PACKAGE = ROOT/'TestResults/adaptive-racing-comparison-admission-20260923'
RUN = ROOT/'TestResults/balance/tower-adaptive-racing-comparison-20260923'
VERSION = 'tower-adaptive-racing-comparison-v1'
SECONDS, BYTES = 600, 536870912
HISTORY_VALUES, HISTORY_FILES = 633313, 240
CAPTURE_PIN = '001eb3e1a8642f3168f15f4ebc81ec65f33235cbae8ae1e2bc5104c61646142d'
FAILURE_PIN = '8e277d2ad609fe1dd00063f6a9b27930ee15f6a82389e07d933c907613be762e'
CLOSEOUT_PIN = '65cde20aabb7deaffcd889b0e685a9bd26830d63be4678db75a2f4506956d213'
TEMPLATE_PIN = 'cd12abf230a8244170aac3b059daf016dae3f2fccdf3cd2dfa3de2add7741f83'
PLAN_PIN = '268159cb2f03a44066e76012e34591e560ce74f1d84acf79617d07503378d550'
PREVIOUS_PIN = '68f7468ba270f1d51a075772a6fed3d800ad627a1376a00c4829c10ef9d806d1'
HISTORY_PIN = '37e6eb81d6e61c3f0733c93ea309e0fdae4d9d812913835fdfa7043e01a52c0a'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
OWNER_PIN = '0efe39bcabf3ac3665988a9c38185938b77f97585ecf4d7dc22b4a6deb691d30'
SETTINGS = 'f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74'
PILOT_02 = False
STUDY_ID = 'tower-adaptive-racing-pilot-02'
DECLARATION_PIN = '55487a065798d94c2d62054ce6563014cf8a4df0a33f711b6038ec94b2b0b830'
REPAIRED_LAUNCHER_PIN = '4972e277baeb60afcd91532d9863e114f73fd567d07aa3d1568d1212484a9d83'
PRIOR_EXECUTION = ROOT/'TestResults/adaptive-racing-comparison-execution-20260923'
PRIOR_EXECUTION_PIN = '79cae962fa8e684ffbff77fd7402008c00efd049959aa7c3aec7785c692aa7d4'
PRIOR_RUN = ROOT/'TestResults/balance/tower-adaptive-racing-comparison-20260923'
PRIOR_INVENTORY_PIN = '435e31a83910280530427b8564e8c938b65794c404aa5b13a6c8def01251f5fd'
REPAIR_PROOFS = {
    'verification/storage-tests.log': '86fe6ecf6d4408b1c25e788e8fe42065bd713c8da96f7ba300a7bd19adf076e3',
    'verification/repaired-protocol-tests.log': '28e105c9db507d4efc3c63731b05a1982e9abd87c2a60da363ee65b6276cbdfe',
    'verification/repaired-compatibility-tests.log': '4dec593e8d5d8536ef2060262a3018f58d6d9fd2364cb48251b6ebb2a37b58ca',
    'verification/test-reference-exploration-storage.py': '18485d922d8f7749a481386842ee34eadbf2c6597893f171611b5cb9f3544736',
}
TESTED = {
    'BalanceHarness.dll': 'c4d97c98ca4a75a653ba85135d9be727bb249cdf74c0b615a8e5f656c55b7587',
    'BalanceHarness.pdb': '0484bc4abbc00627c5f14462bd71cecd48fa213bddd26ae43f34a7373ed7fece',
    'BalanceHarness.deps.json': '76137682e9fa47ebcbdddd68545f54f2c38517286770322a94621cd5050e334c',
    'BalanceHarness.runtimeconfig.json': '8cfba0672dbfced94e75ab40bc1bcf26868dfce981aa96fc1739bdb81478e2ca',
}
INPUTS = {
    'Balance Harness/Tower-Adaptive-Racing-Comparison-Plan.json': 'plan.json',
    'Balance Harness/analysis/audit-reference-exploration-comparison.py': 'auditor.py',
    'build/run-reference-exploration-comparison.py': 'run-reference-exploration-comparison.py',
    'TestResults/adaptive-comparison-final-tests-20260923.trx': 'verification/backend-tests.trx',
    'TestResults/adaptive-comparison-final-tests-20260923.log': 'verification/backend-tests.log',
    'TestResults/adaptive-comparison-python-final-20260923.log': 'verification/python-pilot.log',
    'TestResults/adaptive-comparison-python-compatibility-20260923.log': 'verification/python-compatibility.log',
    'build/test-adaptive-racing-comparison.py': 'verification/test-adaptive-racing-comparison.py',
    'build/test-reference-exploration-comparison.py': 'verification/test-reference-exploration-comparison.py',
    'Balance Harness/analysis/test-adaptive-racing-admission.py': 'verification/test-adaptive-racing-admission.py',
    'TestResults/adaptive-racing-admission-helper-tests-20260923.log': 'verification/admission-helper-tests.log',
}


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


common_path = Path(__file__).with_name('comparison-preparation.py')
if not common_path.exists(): common_path = CLOSEOUT/'comparison-preparation.py'
if hashlib.sha256(common_path.read_bytes()).hexdigest() != COMMON_PIN: raise ValueError('Changed history helper')
common = module('adaptive_admission_history', common_path)
require, read, sha, save = common.require, common.read, common.sha, common.save
evidence_path = Path(__file__).with_name('confirmation-evidence-verification.py')
evidence = module('adaptive_admission_evidence', evidence_path)


def configure_pilot_02():
    """A separately declared full study; never mutate or resume the failed first pilot."""
    global PILOT_02, PACKAGE, RUN, HISTORY, HISTORY_PIN, HISTORY_VALUES, HISTORY_FILES, INPUTS
    require(not PILOT_02, 'Pilot profile already selected')
    PILOT_02 = True
    PACKAGE = ROOT/'TestResults/adaptive-racing-pilot-02-admission-20260923'
    RUN = ROOT/'TestResults/balance/tower-adaptive-racing-pilot-02-20260923'
    HISTORY = PRIOR_EXECUTION/'live-history-files.json'
    HISTORY_PIN = '5766bcfb3f5e36ba85833dd8404ad78ee8a48a2b9702cc9b6cb6243530c68734'
    HISTORY_VALUES, HISTORY_FILES = 649696, 242
    INPUTS = {n: target for n, target in INPUTS.items() if target != 'verification/admission-helper-tests.log'}
    INPUTS.update({
        'Balance Harness/Tower-Adaptive-Racing-Pilot-02-Declaration.json': 'study-declaration.json',
        'TestResults/adaptive-racing-pilot-02-admission-tests-20260923.log': 'verification/admission-helper-tests.log',
        'TestResults/adaptive-racing-storage-regression-tests-20260923.log': 'verification/storage-tests.log',
        'TestResults/adaptive-racing-post-failure-protocol-tests-20260923.log': 'verification/repaired-protocol-tests.log',
        'TestResults/adaptive-racing-post-failure-compatibility-tests-20260923.log': 'verification/repaired-compatibility-tests.log',
        'build/test-reference-exploration-storage.py': 'verification/test-reference-exploration-storage.py',
    })


def verify_pilot_02(package):
    require(sha(package/'study-declaration.json') == DECLARATION_PIN, 'Changed separate study declaration')
    declaration = read(package/'study-declaration.json')
    require(declaration['studyId'] == STUDY_ID and declaration['protocol'] == VERSION
        and declaration['protocolPlanSha256'] == PLAN_PIN and RUN != PRIOR_RUN, 'Changed new study identity/protocol')
    require(sha(package/'run-reference-exploration-comparison.py') == REPAIRED_LAUNCHER_PIN, 'Unbound launcher repair')
    for name, pin in REPAIR_PROOFS.items(): require(sha(package/name) == pin, 'Changed repair verification: '+name)
    require(sha(package/'prior-pilot/closeout-files.json') == PRIOR_EXECUTION_PIN
        and sha(package/'prior-pilot/failed-inventory.json') == PRIOR_INVENTORY_PIN, 'Changed predecessor evidence')
    prior = read(package/'prior-pilot/closeout-files.json')
    require(sha(package/'prior-pilot/closeout.json') == prior['failure-closeout.json']
        and sha(package/'history-source.json') == prior['live-history-files.json']
        and sha(package/'prior-pilot/failed-inventory.json') == prior['failed-files.json'], 'Changed predecessor receipt')
    receipt = read(package/'prior-pilot/closeout.json')
    require(receipt['status'] == 'FailedPilotPreservedNoEfficacyResult' and receipt['completedFights'] == 2640
        and receipt['heldoutFights'] == 0 and not receipt['efficacyResultAvailable'] and receipt['newlyReservedValues'] == 16383
        and receipt['historyFiles'] == HISTORY_FILES and receipt['historicalValues'] == HISTORY_VALUES
        and receipt['retries'] == 0, 'Predecessor failure/history cannot be discarded')
    history = read(package/'history-files.json')
    failed = read(package/'prior-pilot/failed-inventory.json')
    require(all(history.get(str(PRIOR_RUN/name)) == failed[name] for name in ('history-input.json', 'seed-ledger.json')),
        'Failed pilot reservations are not bound to the new history')
    return dict(studyId=STUDY_ID, studyDeclarationSha256=DECLARATION_PIN, repairedLauncherSha256=REPAIRED_LAUNCHER_PIN,
                previousPilot='ClosedTechnicalFailureNoEfficacyResult', reusePreviousObservations=False)


def validate_runtime(runtime, captured):
    require(set(runtime) == {n[8:] for n in captured if n.startswith('runtime/')}, 'Changed runtime membership')
    for name, digest in runtime.items():
        require(digest == (TESTED[name] if name in TESTED else captured['runtime/'+name]), 'Changed runtime file: '+name)


def validate_tests(path):
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    tree = ET.parse(path)
    counters = tree.find('.//t:Counters', ns)
    results = tree.findall('.//t:UnitTestResult', ns)
    require(counters is not None and all(int(counters.get(k, '-1')) == 180 for k in ('total', 'executed', 'passed'))
        and all(int(counters.get(k, '-1')) == 0 for k in ('failed', 'error', 'timeout', 'aborted', 'notExecuted'))
        and len(results) == 180 and all(r.get('outcome') == 'Passed' for r in results), 'Incomplete passing backend receipt')


def template_from(capture, values, context):
    require(context['settingsHash'] == capture['settingsHash'] == SETTINGS, 'Changed captured settings')
    require(values == sorted(set(values)) and 0 < len(values) <= 983616
        and all(type(v) is int and -(2**31) <= v < 2**31 for v in values), 'Invalid complete history')
    d = copy.deepcopy(capture)
    d.update(id='reference-exploration-template', executionHash=context['executionHash'],
             excludedCombatSeeds=values, maximumBattles=1552)
    d.pop('primaryReferenceId', None)
    d['generation'].update(policyVersion='retained-composition-three-references-v1', seeds=[])
    require(d['generation']['candidatesPerArm'] == 46 and d['generation']['maximumAttemptsPerArm'] == 256
        and len(d['starts']) == len(d['references']) == 3 and d['stages']['shortlist'] == 5
        and d['stages']['selectionPrimaryReferenceId'] == 'confirmed-399bc7760fb0cf790a5d8ac4'
        and d['stages']['selectionPolicyVersion'] == 'tower-staged-incumbent-tie-v1'
        and all(s[k] == [] for s in d['stages']['schedules'].values() for k in ('discovery', 'selection', 'confirmation', 'diagnostics'))
        and all(r['scenario']['seeds'] == [] for r in d['references']), 'Changed unscheduled captured scope')
    return d


def request_from(package, history, previous):
    return dict(version=VERSION, captureRoot=str(package/'capture'), captureCloseoutRoot=str(package/'capture/closeout'),
        contentRoot=str(package/'content'), templatePath=str(package/'template.json'), templateHash=sha(package/'template.json'),
        registryRoot=str(RUN.parent), outputRoot=str(RUN), requiredHistory=history,
        pendingHistoryRecoveries=previous['pendingHistoryRecoveries'], recoveryReceiptHashes=previous['recoveryReceiptHashes'],
        planPath=str(package/'plan.json'), planHash=PLAN_PIN, auditorPath=str(package/'auditor.py'), auditorHash=sha(package/'auditor.py'),
        maximumSeconds=10800, maximumBytes=6442450944, priorSeconds=0, priorBytes=0)


def context_command(package, executable):
    require(executable is not None, 'PowerShell host is unavailable')
    host = Path(executable).resolve().with_suffix('.dll')
    deps = host.with_suffix('.deps.json')
    require(host.is_file() and deps.is_file(), 'PowerShell managed host/dependencies are unavailable')
    return ['dotnet', 'exec', '--runtimeconfig', str(package/'runtime/BalanceHarness.runtimeconfig.json'),
        '--depsfile', str(deps), str(host), '-NoProfile', '-File', str(package/'context.ps1'),
        '-Package', str(package), '-RepositoryRoot', str(ROOT), '-AdaptiveRacing']


def validate_process(p):
    require(p['exitCode'] == 0 and not p['timedOut'] and p['activeProcesses'] == 0 and p['totalProcesses'] >= 1
        and p['mechanism'] == 'suspended-owned-job-v1', 'Incomplete owned process')


def charge():
    return dict(version=VERSION, scope='Separate seed-free adaptive pilot admission', chargedSeconds=SECONDS, chargedBytes=BYTES,
        treatment='Full allowance charged at start, including failure. No retry, refund or transfer to the launch allowance.',
        launchMaximumSeconds=10800, launchMaximumBytes=6442450944, launchPriorSeconds=0, launchPriorBytes=0,
        completeHistoricalEngineeringTotalsKnown=False)


def verify_contents(package):
    require(not RUN.exists() and not (package/'failure.json').exists(), 'Scientific output or failed admission exists')
    for name, pin in {'capture/files.json': CAPTURE_PIN, 'capture/failure.json': FAILURE_PIN,
                     'capture/preset/template.json': TEMPLATE_PIN, 'capture/closeout/files.json': CLOSEOUT_PIN,
                     'previous-files.json': PREVIOUS_PIN, 'history-source.json': HISTORY_PIN, 'plan.json': PLAN_PIN,
                     'comparison-preparation.py': COMMON_PIN, 'bounded_windows_process.py': OWNER_PIN}.items():
        require(sha(package/name) == pin, 'Changed pinned input: '+name)
    require(sha(package/'previous-request.json') == read(package/'previous-files.json')['request.json'], 'Changed historical request')
    closeout = read(package/'capture/closeout/files.json')
    require(sha(package/'capture/closeout/receipt.json') == closeout['receipt.json'], 'Changed reconciliation receipt')
    reconciliation = read(package/'capture/closeout/receipt.json')
    require(reconciliation['closeoutStatus'] == 'ReadOnlyReconciled' and reconciliation['manifestSha256'] == CAPTURE_PIN
        and reconciliation['reconciledCloseoutFailureSha256'] == FAILURE_PIN, 'Unreconciled capture')
    captured = read(package/'capture/files.json')
    runtime = common.inventory(package/'runtime')
    validate_runtime(runtime, captured)
    require(runtime == read(package/'runtime-files.json'), 'Changed retained runtime')
    require(common.inventory(package/'content') == {n[8:]: p for n, p in captured.items() if n.startswith('content/')}, 'Changed content/settings')
    require(read(package/'tested-harness-files.json') == TESTED, 'Changed tested harness proof')
    validate_tests(package/'verification/backend-tests.trx')
    inputs = read(package/'input-files.json')
    helpers = read(package/'admission-helpers.json')
    require(set(inputs) == set(INPUTS.values()) | {'adaptive-fixture/plan.json', 'adaptive-fixture/report.json'}, 'Incomplete input binding')
    require(set(helpers) == {'prepare.py', 'context.ps1', 'comparison-preparation.py', 'confirmation-evidence-verification.py',
        'bounded_windows_process.py', 'lease-provider.py'}, 'Incomplete helper binding')
    for name, pin in inputs.items(): require(sha(package/name) == pin, 'Changed captured input: '+name)
    for name, pin in helpers.items(): require(sha(package/name) == pin, 'Changed admission helper: '+name)
    compiled = read(package/'compiled-source-files.json')
    context = read(package/'context.json')
    require(len(compiled) == context['compiledSourceDocuments']
        and all('LL/tools/BalanceHarness/'+n+'.cs' in compiled for n in ('TowerAdaptiveRacing', 'TowerAdaptiveRacingComparison',
            'TowerAdaptiveRacingNative', 'TowerAdaptiveRacingGenerator', 'TowerBatchRacing', 'TowerBatchRacingContract'))
        and all(sha(package/'source'/n) == p for n, p in compiled.items()), 'Changed producing source snapshot')
    require(context['status'] == 'CapturedRuntimeEntryPathsCompatible' and context['settingsHash'] == SETTINGS
        and context['nativePreparations'] == context['fights'] == context['newValues'] == 0
        and all(runtime.get(k+'.dll') == v for k, v in context['execution']['assemblyHashes'].items()), 'Changed loaded runtime')
    for kind, names in {'TowerReferenceExplorationComparison': ('Check', 'Run', 'VerifyStudy', 'Assess', 'Reserve'),
                        'TowerAdaptiveRacingComparison': ('Bind', 'Search', 'Summarize'),
                        'TowerAdaptiveRacing': ('Validate', 'RunAsync', 'ReconstructAsync'),
                        'TowerAdaptiveRacingNative': ('Authenticate',), 'TowerBatchRacing': ('RunCoreAsync',)}.items():
        for name in names:
            require(any(kind+'.'+name+'#' in m for m in context['jitResolvedMethods']), 'Unresolved entry path: '+kind+'.'+name)
    require(context['adaptiveFixture'] == dict(status='StoredMeasurementsReconstructed', version='tower-adaptive-beam-racing-v1',
        planFileSha256=sha(package/'adaptive-fixture/plan.json'), reportFileSha256=sha(package/'adaptive-fixture/report.json'),
        storedEvaluations=528, batches=2, panels=5, newFights=0, newValues=0), 'Incomplete captured adaptive reconstruction')
    history = read(package/'history-files.json')
    q, d = read(package/'request.json'), read(package/'template.json')
    require(history == read(package/'history-after.json') == read(package/'history-source.json')
        and len(history) == HISTORY_FILES and len(d['excludedCombatSeeds']) == HISTORY_VALUES, 'Changed complete history')
    require(q == request_from(package, history, read(package/'previous-request.json'))
        and d == template_from(read(package/'capture/preset/template.json'), d['excludedCombatSeeds'], context), 'Changed bound request/template')
    require(read(package/'admission-charge.json') == charge(), 'Changed separate admission charge')
    require(read(package/'native-check.json') == dict(version=VERSION, status='ReadyNoReservation', historicalValues=HISTORY_VALUES,
        newValues=0, fights=0, restarts=12, searchFights=12672, maximumFights=28032), 'Changed native admission receipt')
    for name in ('context-process.json', 'native-check-process.json'): validate_process(read(package/name))
    require(not any(p.name in common.LEDGERS | {'entropy.bin', 'entropy-intent.json', 'entropy-start.json', 'attempts.jsonl'}
        for p in common.paths(package)), 'Premature scientific reservation/attempt')
    successor = verify_pilot_02(package) if PILOT_02 else {}
    return dict(status='AdaptivePilotAdmittedNoReservation', version=VERSION, requestSha256=sha(package/'request.json'),
        templateSha256=sha(package/'template.json'), executionHash=context['executionHash'], settingsHash=SETTINGS,
        compiledSourceDocuments=len(compiled), jitResolvedMethods=len(context['jitResolvedMethods']), storedAdaptiveEvaluations=528,
        nativeReferencePreparations=6, historyFiles=HISTORY_FILES, historicalValues=HISTORY_VALUES,
        maximumScientificFights=28032, assignedValues=4428, entropyWords=16384, resourceFeasibilityEstablished=False,
        admissionChargeSeconds=SECONDS, admissionChargeBytes=BYTES, launchMaximumSeconds=10800, launchMaximumBytes=6442450944,
        scientificLaunches=0, newValues=0, fights=0, outputExists=False, **successor)


def worker():
    require(PACKAGE.exists() and not RUN.exists() and not (PACKAGE/'runtime').exists(), 'No retry or resume')
    deadline = read(PACKAGE/'admission-launch.json')['monotonicDeadline']
    def check():
        require(time.monotonic() < deadline and not RUN.exists(), 'Admission deadline or unexpected scientific output')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES-1048576, 'Admission storage ceiling')
    def command(name, args):
        owner = module('adaptive_nested_owner', PACKAGE/'bounded_windows_process.py')
        p = owner.run(args, str(ROOT), str(PACKAGE/(name+'.log')), deadline-1, cleanup_seconds=1, check=check)
        save(PACKAGE/(name+'-process.json'), p); validate_process(p); print(name+' passed', flush=True)
    require(sha(CAPTURE/'files.json') == CAPTURE_PIN and sha(CAPTURE/'failure.json') == FAILURE_PIN, 'Changed reconciled capture')
    captured = read(CAPTURE/'files.json')
    require(set(common.inventory(CAPTURE)) == set(captured) | {'files.json', 'failure.json'}, 'Changed source capture membership')
    for name, pin in captured.items(): require(sha(CAPTURE/name) == pin, 'Changed source capture: '+name)
    evidence.authenticate(CLOSEOUT, CLOSEOUT_PIN, check=check)
    # Only historical request provenance is consumed here. Past battle rows are
    # not re-audited or used as efficacy evidence; every current ledger is scanned.
    require(sha(PREVIOUS/'files.json') == PREVIOUS_PIN and sha(PREVIOUS/'request.json') == read(PREVIOUS/'files.json')['request.json'],
        'Changed history request provenance')
    require(sha(HISTORY) == HISTORY_PIN, 'Changed authoritative history inventory')
    if PILOT_02:
        evidence.authenticate(PRIOR_EXECUTION, PRIOR_EXECUTION_PIN, check=check)
        require(common.inventory(PRIOR_RUN) == read(PRIOR_EXECUTION/'failed-files.json'), 'Failed pilot archive changed')
        require(sha(ROOT/'build/run-reference-exploration-comparison.py') == REPAIRED_LAUNCHER_PIN, 'Changed repaired launcher')
        check()
    for name, pin in TESTED.items():
        require(sha(BUILD/'BalanceHarness/release'/name) == sha(BUILD/'EssenceSystem.Tests/release'/name) == pin,
            'Harness differs from backend test output: '+name)
    shutil.copytree(CAPTURE/'runtime', PACKAGE/'runtime'); shutil.copytree(CAPTURE/'content', PACKAGE/'content')
    for name in TESTED: shutil.copyfile(BUILD/'BalanceHarness/release'/name, PACKAGE/'runtime'/name)
    copies = [(CAPTURE/'files.json', 'capture/files.json'), (CAPTURE/'failure.json', 'capture/failure.json'),
        (CAPTURE/'preset/template.json', 'capture/preset/template.json'), (CLOSEOUT/'files.json', 'capture/closeout/files.json'),
        (CLOSEOUT/'receipt.json', 'capture/closeout/receipt.json'), (PREVIOUS/'files.json', 'previous-files.json'),
        (PREVIOUS/'request.json', 'previous-request.json'), (HISTORY, 'history-source.json'),
        (FIXTURE/'pair-01-candidate-plan.json', 'adaptive-fixture/plan.json'),
        (FIXTURE/'pair-01-candidate-adaptive.json', 'adaptive-fixture/report.json')]
    copies += [(ROOT/n, target) for n, target in INPUTS.items()]
    if PILOT_02:
        copies += [(PRIOR_EXECUTION/'files.json', 'prior-pilot/closeout-files.json'),
                   (PRIOR_EXECUTION/'failure-closeout.json', 'prior-pilot/closeout.json'),
                   (PRIOR_EXECUTION/'failed-files.json', 'prior-pilot/failed-inventory.json')]
    for source, target in copies:
        dest = PACKAGE/target; dest.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(source, dest); check()
    for target, pin in read(PACKAGE/'input-files.json').items(): require(sha(PACKAGE/target) == pin, 'Input changed during copy')
    save(PACKAGE/'tested-harness-files.json', TESTED)
    validate_tests(PACKAGE/'verification/backend-tests.trx')
    validate_runtime(common.inventory(PACKAGE/'runtime'), captured)
    command('context', context_command(PACKAGE, shutil.which('pwsh')))
    save(PACKAGE/'runtime-files.json', common.inventory(PACKAGE/'runtime'))
    files, values = common.history(RUN.parent, read(PACKAGE/'previous-request.json'))
    require((len(values), len(files)) == (HISTORY_VALUES, HISTORY_FILES) and files == read(PACKAGE/'history-source.json'), 'Changed full history')
    save(PACKAGE/'history-files.json', files)
    save(PACKAGE/'template.json', template_from(read(PACKAGE/'capture/preset/template.json'), values, read(PACKAGE/'context.json')))
    save(PACKAGE/'request.json', request_from(PACKAGE, files, read(PACKAGE/'previous-request.json')))
    command('native-check', ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'), 'tower-reference-exploration-comparison-check', str(PACKAGE/'request.json')])
    save(PACKAGE/'native-check.json', read(PACKAGE/'native-check.log'))
    after, current = common.history(RUN.parent, read(PACKAGE/'request.json'))
    require(after == files and current == values, 'History changed during admission')
    save(PACKAGE/'history-after.json', after); check()
    if PILOT_02:
        require(common.inventory(PRIOR_RUN) == read(PACKAGE/'prior-pilot/failed-inventory.json'), 'Failed pilot changed during admission')
        evidence.authenticate(PRIOR_EXECUTION, PRIOR_EXECUTION_PIN, check=check)


def terminal_receipt(value):
    value = dict(value, terminalBytes=0)
    for _ in range(16):
        size = len((json.dumps(value, indent=2, allow_nan=False)+'\n').encode('utf-8'))
        if value['terminalBytes'] == size: return value
        value['terminalBytes'] = size
    raise ValueError('Terminal byte accounting did not converge')


def prepare():
    require(os.name == 'nt' and not PACKAGE.exists() and not PACKAGE.with_name(PACKAGE.name+'-pin.json').exists()
        and not RUN.exists(), 'New Windows admission package required; no retry or resume')
    require(sha(ROOT/'build/bounded_windows_process.py') == OWNER_PIN, 'Changed process owner')
    started = time.monotonic(); PACKAGE.mkdir()
    watchdog = threading.Timer(SECONDS, lambda: os._exit(124)); watchdog.daemon = True; watchdog.start()
    def check():
        require(time.monotonic()-started < SECONDS and not RUN.exists(), 'Admission elapsed ceiling or scientific output')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES, 'Admission storage ceiling')
    try:
        helpers = [(Path(__file__), 'prepare.py'), (ROOT/'Balance Harness/analysis/reference-exploration-context.ps1', 'context.ps1'),
            (common_path, 'comparison-preparation.py'), (evidence_path, 'confirmation-evidence-verification.py'),
            (ROOT/'build/bounded_windows_process.py', 'bounded_windows_process.py'),
            (ROOT/'build/run-three-reference-confirmation.py', 'lease-provider.py')]
        for source, target in helpers: shutil.copyfile(source, PACKAGE/target)
        save(PACKAGE/'admission-helpers.json', {n: sha(PACKAGE/n) for _, n in helpers})
        inputs = {target: sha(ROOT/n) for n, target in INPUTS.items()}
        inputs.update({'adaptive-fixture/plan.json': sha(FIXTURE/'pair-01-candidate-plan.json'),
                       'adaptive-fixture/report.json': sha(FIXTURE/'pair-01-candidate-adaptive.json')})
        save(PACKAGE/'input-files.json', inputs)
        save(PACKAGE/'admission-charge.json', charge())
        save(PACKAGE/'admission-launch.json', dict(version=VERSION, maximumSeconds=SECONDS, maximumBytes=BYTES,
            monotonicDeadline=started+SECONDS-5, parentProcessId=os.getpid(), scientificLaunches=0, retries=0, resume=False))
        lease = module('adaptive_admission_lease', PACKAGE/'lease-provider.py')
        with lease.writer_lease(RUN.parent/'complete-family-allocation'), lease.writer_lease(RUN):
            owner = module('adaptive_admission_owner', PACKAGE/'bounded_windows_process.py')
            p = owner.run([sys.executable, '-B', '-X', 'utf8', str(PACKAGE/'prepare.py'), '_worker'] + (['--pilot-02'] if PILOT_02 else []), str(ROOT), str(PACKAGE/'worker.log'),
                started+SECONDS-5, cleanup_seconds=1, check=check)
            save(PACKAGE/'worker-process.json', p); validate_process(p)
            result = verify_contents(PACKAGE); save(PACKAGE/'admission.json', result); check()
            save(PACKAGE/'completion.json', dict(status='AdmissionCompleteNoReservation', measuredSecondsBeforeSeal=time.monotonic()-started,
                maximumSeconds=SECONDS, maximumBytes=BYTES, scientificLaunches=0, newValues=0, fights=0, retries=0))
            save(PACKAGE/'files.json', common.inventory(PACKAGE)); check()
            final = terminal_receipt(dict(status=result['status'], manifestSha256=sha(PACKAGE/'files.json'), requestSha256=result['requestSha256'],
                measuredSeconds=time.monotonic()-started, retainedBytes=sum(p.stat().st_size for p in common.paths(PACKAGE)),
                maximumSeconds=SECONDS, maximumBytes=BYTES, admissionChargeSeconds=SECONDS, admissionChargeBytes=BYTES,
                scientificLaunches=0, newValues=0, fights=0))
            require(final['retainedBytes']+final['terminalBytes'] < BYTES, 'Terminal receipt storage ceiling')
            save(PACKAGE.with_name(PACKAGE.name+'-pin.json'), final); check(); print(json.dumps(final, indent=2))
    except BaseException as error:
        if not (PACKAGE/'failure.json').exists(): save(PACKAGE/'failure.json', dict(status='AdmissionFailedNoReservation', reason=str(error),
            measuredSeconds=time.monotonic()-started, admissionChargeSeconds=SECONDS, admissionChargeBytes=BYTES,
            scientificLaunches=0, newValues=0, fights=0, retries=0))
        raise
    finally: watchdog.cancel()


def verify(package, pin, live=False):
    evidence.authenticate(package, pin)
    result = verify_contents(package)
    require(result == read(package/'admission.json'), 'Changed admission conclusions')
    validate_process(read(package/'worker-process.json'))
    completion = read(package/'completion.json')
    final_path = package.with_name(package.name+'-pin.json'); final = read(final_path)
    require(completion['status'] == 'AdmissionCompleteNoReservation' and completion['maximumSeconds'] == SECONDS
        and completion['maximumBytes'] == BYTES and 0 <= completion['measuredSecondsBeforeSeal'] <= final['measuredSeconds'] < SECONDS
        and final['manifestSha256'] == pin and final['requestSha256'] == result['requestSha256'] and final['terminalBytes'] == final_path.stat().st_size
        and final['retainedBytes'] == sum(p.stat().st_size for p in common.paths(package)) and final['retainedBytes']+final['terminalBytes'] < BYTES
        and final['status'] == result['status'] and final['maximumSeconds'] == final['admissionChargeSeconds'] == SECONDS
        and final['maximumBytes'] == final['admissionChargeBytes'] == BYTES and final['scientificLaunches'] == final['newValues'] == final['fights'] == 0
        and completion['scientificLaunches'] == completion['newValues'] == completion['fights'] == completion['retries'] == 0, 'Changed resource receipt')
    if live:
        files, values = common.history(RUN.parent, read(package/'request.json'))
        require(files == read(package/'history-files.json') and values == read(package/'template.json')['excludedCombatSeeds'], 'Live history changed')
    evidence.authenticate(package, pin)
    return dict(result, manifestSha256=pin, readOnlyVerification=True, nativePreparationsDuringVerification=0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare', 'verify', '_worker'])
    parser.add_argument('--package', type=Path)
    parser.add_argument('--pilot-02', action='store_true', help='Use the separately declared full second pilot and preserved failed-run history')
    parser.add_argument('--manifest-sha256'); parser.add_argument('--live', action='store_true')
    args = parser.parse_args()
    if args.pilot_02: configure_pilot_02()
    if args.command == 'prepare': prepare()
    elif args.command == '_worker': worker()
    else:
        require(args.manifest_sha256 is not None, 'Supply the external manifest pin')
        print(json.dumps(verify((args.package or PACKAGE).resolve(), args.manifest_sha256, args.live), indent=2))
