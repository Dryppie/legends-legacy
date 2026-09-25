"""One bounded, seed-free admission of the frozen reference-exploration comparison.

This helper can capture/check inputs and audit its saved package. It has no route
to search, entropy generation, battle execution, retry, resume or deployment.
"""
import argparse
import copy
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
import sys
import threading
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
CAPTURE = ROOT/'TestResults/three-reference-native-admission-20260922'
CLOSEOUT = ROOT/'TestResults/three-reference-admission-closeout-20260922'
IMPLEMENTATION = ROOT/'TestResults/reference-exploration-comparison-implementation-20260922'
BUILD = ROOT/'TestResults/reference-exploration-comparison-build-20260922/bin/BalanceHarness/release'
PREVIOUS_RUN = ROOT/'TestResults/balance/tower-practical-three-reference-20260922'
PREVIOUS_EXECUTION = ROOT/'TestResults/three-reference-practical-execution-20260922'
PACKAGE = ROOT/'TestResults/reference-exploration-comparison-admission-20260922'
RUN = ROOT/'TestResults/balance/tower-reference-exploration-comparison-20260922'
CAPTURE_PIN = '001eb3e1a8642f3168f15f4ebc81ec65f33235cbae8ae1e2bc5104c61646142d'
FAILURE_PIN = '8e277d2ad609fe1dd00063f6a9b27930ee15f6a82389e07d933c907613be762e'
CLOSEOUT_PIN = '65cde20aabb7deaffcd889b0e685a9bd26830d63be4678db75a2f4506956d213'
TEMPLATE_PIN = 'cd12abf230a8244170aac3b059daf016dae3f2fccdf3cd2dfa3de2add7741f83'
IMPLEMENTATION_PIN = '0c1049faec80de51483101d29d1970afe22db170876a9915630b9daadd6b5383'
PREVIOUS_RUN_PIN = '92c0245d89afbf1a8776705577fbe18dc86c4410e8c2a7ef6a76f570978fc389'
PREVIOUS_EXECUTION_PIN = 'e3481e635b790726e6f7d4c0b4af07597ab17e22b86074a3c30b1bb4aa49968d'
PLAN_PIN = '6a0e8fb68d7a20e66740795d17278e5bf7e95c8e77c6b10f9127e8c3e77468a4'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
VERSION = 'tower-reference-exploration-comparison-v1'
BASELINE = 'retained-composition-three-references-v1'
SETTINGS = 'f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74'
SECONDS, BYTES = 600, 536870912
HISTORY_VALUES, HISTORY_FILES = 551408, 232
PROOF_FILES = ('verification.json', 'source-files.json', 'regression-tests.trx', 'final-controller-tests.trx', 'python-envelope-tests.log')
OFFSET = False
SCREENING = False
OWNER_PIN = '0efe39bcabf3ac3665988a9c38185938b77f97585ecf4d7dc22b4a6deb691d30'
HISTORY_SOURCE = ROOT/'TestResults/reference-exploration-offset-implementation-20260922'
HISTORY_SOURCE_PIN = '20dd587671e64a1d20db715c5dd1a4629c1f4fe6161cdaf98a7e59ec65b23ad2'
HARNESS_FILES = ('BalanceHarness.dll', 'BalanceHarness.pdb', 'BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


common_path = Path(__file__).with_name('comparison-preparation.py')
if not common_path.exists():
    common_path = CLOSEOUT/'comparison-preparation.py'
import hashlib
assert hashlib.sha256(common_path.read_bytes()).hexdigest() == COMMON_PIN
common = module('reference_exploration_admission_common', common_path)
require, read, sha, save = common.require, common.read, common.sha, common.save


def configure_offset():
    global OFFSET, VERSION, PLAN_PIN, PACKAGE, RUN, IMPLEMENTATION, IMPLEMENTATION_PIN, BUILD, HISTORY_VALUES, HISTORY_FILES, PROOF_FILES
    OFFSET = True
    VERSION = 'tower-reference-exploration-offset-comparison-v1'
    PLAN_PIN = '7e6203052cd8a1984b80bbe34dbb1e4b21fe3f882c8ab314e21987d289a25c90'
    PACKAGE = ROOT/'TestResults/reference-exploration-offset-comparison-admission-20260922'
    RUN = ROOT/'TestResults/balance/tower-reference-exploration-offset-comparison-20260922'
    IMPLEMENTATION = ROOT/'TestResults/reference-exploration-offset-comparison-implementation-20260922'
    IMPLEMENTATION_PIN = '0a546dbd9e4969cd4ac69445a4572910fa2ad9b9f999685914d0c55b71b1abf2'
    BUILD = ROOT/'TestResults/reference-exploration-offset-comparison-build-20260922/bin/BalanceHarness/release'
    HISTORY_VALUES, HISTORY_FILES = 567789, 234
    PROOF_FILES = ('verification.json', 'source-files.json', 'regression-tests.trx', 'protocol-tests.trx', 'python-envelope-tests.log', 'arithmetic-tests.log')


def configure_screening():
    global SCREENING, VERSION, PLAN_PIN, PACKAGE, RUN, IMPLEMENTATION, IMPLEMENTATION_PIN, BUILD, HISTORY_VALUES, HISTORY_FILES, PROOF_FILES, HISTORY_SOURCE, HISTORY_SOURCE_PIN
    SCREENING = True
    VERSION = 'tower-practical-fresh-screening-comparison-v1'
    PLAN_PIN = '13fc98e45afb7a96403918ffe0233fd1dcb375a52fb9446b63053911cc94c263'
    PACKAGE = ROOT/'TestResults/practical-fresh-screening-admission-20260922'
    RUN = ROOT/'TestResults/balance/tower-practical-fresh-screening-comparison-20260922'
    IMPLEMENTATION = ROOT/'TestResults/practical-fresh-screening-implementation-20260922'
    IMPLEMENTATION_PIN = '7802e75c8eb7a0f19bb69a277dc747b76280af30af8b50a2d8ba1b036122b9eb'
    BUILD = ROOT/'TestResults/practical-fresh-screening-build-20260922/bin/BalanceHarness/release'
    HISTORY_VALUES, HISTORY_FILES = 584171, 236
    HISTORY_SOURCE, HISTORY_SOURCE_PIN = IMPLEMENTATION, IMPLEMENTATION_PIN
    PROOF_FILES = ('completion.json', 'publication-verification.json', 'source-files.json', 'before-source-files.json',
                   'tested-harness-files.json', 'backend-tests.trx', 'python-tests.log', 'integrity.json')


def implementation_record(directory):
    return read(directory/('completion.json' if SCREENING else 'verification.json'))


def tested_binaries(directory):
    if SCREENING:
        pins = read(directory/'tested-harness-files.json')
        require(len(pins) == 4 and {Path(p).name for p in pins} == set(HARNESS_FILES), 'Incomplete tested harness files')
        return {Path(p).name: value for p, value in pins.items()}
    return implementation_record(directory)['engineeringBinaries']


def tested_sources(directory):
    pins = read(directory/'source-files.json')
    if SCREENING:
        before = read(directory/'before-source-files.json')
        require(len(before) == 201, 'Incomplete predecessor source coverage')
        pins = dict(before, **pins, **{'build/bounded_windows_process.py': OWNER_PIN})
    return pins


def screening_fixture_paths():
    prefix = 'fixtures/tower-practical-fresh-screening-comparison-v1/'
    return {prefix+'template.json': 'template.json', prefix+'allocation.json': 'fixture-values.json',
            prefix+'study/pair-01-candidate.json': 'candidate.json', prefix+'study/generation-mechanics.json': 'mechanics.json'}


def validate_implementation(tested):
    if SCREENING:
        require(tested['status'] == 'EngineeringVerified' and tested['version'] == VERSION
                and tested['backendTests'] == 154 and tested['pythonTests'] == 41
                and tested['fixtureArchives'] == 3 and tested['literalReportsPerArchive'] == 60672
                and tested['historyValues'] == HISTORY_VALUES and tested['historyFiles'] == HISTORY_FILES
                and tested['newScientificValues'] == tested['newScientificFights'] == tested['runtimeAdmissions'] == tested['scientificLaunches'] == 0
                and tested['preservedPriorLedger'] == dict(seconds=18180, mebibytes=13584), 'Missing passing fresh-screening implementation checks')
    elif OFFSET:
        require(tested['status'] == 'OffsetComparisonImplementationVerifiedAdmissionPending' and tested['version'] == VERSION
                and tested['planSha256'] == PLAN_PIN and tested['backendRegression']['passed'] == 124
                and tested['backendRegression']['failed'] == tested['backendRegression']['skipped'] == 0
                and tested['finalProtocol']['passed'] == 2 and tested['finalProtocol']['failed'] == 0
                and tested['python']['passed'] == 27 and tested['python']['failed'] == 0
                and tested['arithmetic']['passed'] == 14 and tested['arithmetic']['failed'] == 0,
                'Missing passing offset implementation checks')
    else:
        require(tested['status'] == 'ImplementationVerifiedAdmissionPending' and tested['backendRegression']['passed'] == 98
                and tested['finalController']['passed'] == 24 and tested['python']['passed'] == 14, 'Missing passing implementation checks')


def verify_history_source(package, history):
    if SCREENING:
        require(sha(package/'history-source/files.json') == HISTORY_SOURCE_PIN, 'Changed screening history source pin')
        pins = read(package/'history-source/files.json')
        for name in ('history-files.json', 'integrity.json'):
            require(sha(package/'history-source'/name) == pins[name], 'Changed screening history receipt')
        receipt = read(package/'history-source/integrity.json')
        require(receipt['status'] == 'Verified' and receipt['historyPreserved']
                and (receipt['historyValues'], receipt['historyFiles']) == (HISTORY_VALUES, HISTORY_FILES)
                and history == read(package/'history-source/history-files.json'), 'Changed authoritative screening history')
    elif OFFSET:
        require(sha(package/'history-source/files.json') == HISTORY_SOURCE_PIN, 'Changed offset history source pin')
        pins = read(package/'history-source/files.json')
        for name in ('history-files.json', 'history-preservation.json'):
            require(sha(package/'history-source'/name) == pins[name], 'Changed offset history receipt')
        require(history == read(package/'history-source/history-files.json'), 'Changed authoritative history inventory')
        receipt = read(package/'history-source/history-preservation.json')
        require((receipt['historicalValues'], receipt['historyFiles']) == (HISTORY_VALUES, HISTORY_FILES), 'Changed full history count')
    else:
        require(sha(package/'previous-execution/live-history-files.json') == read(package/'previous-execution/files.json')['live-history-files.json']
                and history == read(package/'previous-execution/live-history-files.json'), 'Changed authoritative history inventory')


def capture_inventory(package):
    require(sha(package/'files.json') == CAPTURE_PIN and sha(package/'failure.json') == FAILURE_PIN, 'Changed reconciled capture pin')
    files = read(package/'files.json')
    require({p.relative_to(package).as_posix() for p in common.paths(package)} == set(files) | {'files.json', 'failure.json'}, 'Changed source capture membership')
    for name, value in files.items():
        path = (package/name).resolve()
        require(path.is_relative_to(package.resolve()) and sha(path) == value, 'Changed source capture file: '+name)
    require(files['preset/template.json'] == TEMPLATE_PIN, 'Changed captured template')
    return files


def validate_runtime(runtime, captured, tested):
    require(set(runtime) == {name[8:] for name in captured if name.startswith('runtime/')}, 'Changed runtime membership')
    for name, value in runtime.items():
        if name not in HARNESS_FILES:
            require(value == captured['runtime/'+name], 'Changed captured dependency: '+name)
    require(all(runtime[name] == tested[name] for name in (HARNESS_FILES if SCREENING else ('BalanceHarness.dll', 'BalanceHarness.pdb'))), 'Untested producing harness')


def template_from(capture, values, context):
    require(context['settingsHash'] == capture['settingsHash'] == SETTINGS, 'Changed captured settings')
    require(values == sorted(set(values)) and 0 < len(values) <= 983616, 'Invalid full history')
    d = copy.deepcopy(capture)
    d.update(id='reference-exploration-template', executionHash=context['executionHash'], excludedCombatSeeds=values, maximumBattles=4528)
    d.pop('primaryReferenceId', None)
    d['generation'].update(policyVersion=BASELINE, seeds=[])
    require(d['generation']['candidatesPerArm'] == 46 and d['generation']['maximumAttemptsPerArm'] == 256
            and len(d['starts']) == len(d['references']) == 3 and d['stages']['shortlist'] == 5
            and d['stages']['selectionPolicyVersion'] == 'tower-staged-incumbent-tie-v1'
            and d['stages']['selectionPrimaryReferenceId'] == 'confirmed-399bc7760fb0cf790a5d8ac4'
            and all(s[k] == [] for s in d['stages']['schedules'].values() for k in ('discovery', 'selection', 'confirmation', 'diagnostics')),
            'Changed unscheduled comparison scope')
    return d


def request_from(package, history, previous):
    return dict(version=VERSION, captureRoot=str(package/'capture'), captureCloseoutRoot=str(package/'capture/closeout'),
        contentRoot=str(package/'content'), templatePath=str(package/'template.json'), templateHash=sha(package/'template.json'),
        registryRoot=str(RUN.parent), outputRoot=str(RUN), requiredHistory=history,
        pendingHistoryRecoveries=previous['pendingHistoryRecoveries'], recoveryReceiptHashes=previous['recoveryReceiptHashes'],
        planPath=str(package/'plan.json'), planHash=PLAN_PIN, auditorPath=str(package/'auditor.py'), auditorHash=sha(package/'auditor.py'),
        maximumSeconds=10800, maximumBytes=6442450944, priorSeconds=600, priorBytes=536870912)


def forecast(previous):
    require(previous['status'] == 'ThreeReferencePracticalExecutionVerified' and previous['completedFights'] == 3528
            and previous['nativeAudit'] == previous['independentAudit'] == 'Passed' and previous['newAuditFights'] == 0,
            'Invalid historical cost source')
    factor = 72672/previous['completedFights']
    seconds = previous['secondsThroughAudits']*factor
    size = math.ceil(previous['observedPeakBytes']*factor)
    require(seconds > 0 and size > 0 and math.isfinite(seconds), 'Invalid cost arithmetic')
    return dict(status='HistoricalLinearEstimate', sourceFights=3528, targetMaximumFights=72672,
        sourceSeconds=previous['secondsThroughAudits'], sourceBytes=previous['observedPeakBytes'],
        estimatedExecutionSeconds=seconds, estimatedExecutionBytes=size,
        remainingSeconds=10200, remainingBytes=5905580032, fits=seconds < 10200 and size < 5905580032,
        guaranteedUpperBound=False, includesHistoricalAuditAndPublicationCosts=True,
        caveat='New adaptive searches and unseen battles can cost more. The owned launcher enforces absolute caps; no extension or retry.')


def validate_process(p):
    require(p['exitCode'] == 0 and not p['timedOut'] and p['activeProcesses'] == 0 and p['totalProcesses'] >= 1
            and p['mechanism'] == 'suspended-owned-job-v1', 'Incomplete owned process')


def verify_contents(package):
    require(not RUN.exists() and not (package/'failure.json').exists(), 'Scientific output or failed admission exists')
    q, d, context = read(package/'request.json'), read(package/'template.json'), read(package/'context.json')
    captured = read(package/'capture/files.json')
    require(sha(package/'capture/files.json') == CAPTURE_PIN and sha(package/'capture/failure.json') == FAILURE_PIN
            and sha(package/'capture/preset/template.json') == TEMPLATE_PIN
            and sha(package/'capture/closeout/files.json') == CLOSEOUT_PIN, 'Changed copied source provenance')
    closeout = read(package/'capture/closeout/files.json')
    require(sha(package/'capture/closeout/receipt.json') == closeout['receipt.json'], 'Changed reconciliation receipt')
    receipt = read(package/'capture/closeout/receipt.json')
    require(receipt['closeoutStatus'] == 'ReadOnlyReconciled' and receipt['manifestSha256'] == CAPTURE_PIN
            and receipt['reconciledCloseoutFailureSha256'] == FAILURE_PIN, 'Invalid source reconciliation')
    require(sha(package/'verification/files.json') == IMPLEMENTATION_PIN, 'Changed implementation receipt pin')
    proof = read(package/'verification/files.json')
    for name in PROOF_FILES:
        require(sha(package/'verification'/name) == proof[name], 'Changed tested implementation evidence')
    tested = implementation_record(package/'verification')
    validate_implementation(tested)
    runtime = common.inventory(package/'runtime')
    validate_runtime(runtime, captured, tested_binaries(package/'verification'))
    require(runtime == read(package/'runtime-files.json') and all(runtime.get(k+'.dll') == v for k, v in context['execution']['assemblyHashes'].items()),
            'Loaded runtime identity differs from retained files')
    require(common.inventory(package/'content') == {name[8:]: value for name, value in captured.items() if name.startswith('content/')}, 'Changed content or settings')
    compiled = read(package/'compiled-source-files.json')
    source_pins = tested_sources(package/'verification')
    require(len(compiled) == context['compiledSourceDocuments'] and all(sha(package/'source'/name) == value for name, value in compiled.items()), 'Changed producing sources')
    require(all(source_pins.get(name) == value for name, value in compiled.items() if name.startswith('LL/tools/BalanceHarness/')), 'Producing source differs from tested implementation')
    require(context['nativePreparations'] == context['fights'] == context['newValues'] == 0 and context['settingsHash'] == SETTINGS, 'Changed context scope')
    for method in ('Check', 'Run', 'VerifyStudy', 'Assess', 'Reserve'):
        require(any('TowerReferenceExplorationComparison.'+method+'#' in name for name in context['jitResolvedMethods']), 'Unresolved comparison method: '+method)
    if SCREENING:
        require(len(compiled) == 202, 'Incomplete screened producing source coverage')
        for method in ('Validate', 'Freeze', 'Nominate'):
            require(any('TowerPracticalScreening.'+method+'#' in name for name in context['jitResolvedMethods']), 'Unresolved screening method: '+method)
        for name, target in screening_fixture_paths().items():
            require(sha(package/'screening-fixture'/target) == proof[name], 'Changed stored screening fixture')
        require(context['screeningFixture'] == dict(status='StoredMeasurementsReconstructed', version=VERSION,
            candidateFileSha256=sha(package/'screening-fixture/candidate.json'), discoveryFights=184, screeningMembers=23,
            afterSearchFights=368, nominees=5, newFights=0, newValues=0), 'Incomplete captured screening reconstruction')
    require(sha(package/'plan.json') == PLAN_PIN and sha(package/'auditor.py') == source_pins['Balance Harness/analysis/audit-reference-exploration-comparison.py']
            and sha(package/'run-reference-exploration-comparison.py') == source_pins['build/run-reference-exploration-comparison.py']
            and sha(package/'bounded_windows_process.py') == source_pins['build/bounded_windows_process.py'], 'Changed plan, auditor or owner')
    history = read(package/'history-files.json')
    require(len(history) == HISTORY_FILES and len(d['excludedCombatSeeds']) == HISTORY_VALUES and read(package/'history-after.json') == history, 'Changed live history')
    verify_history_source(package, history)
    require(q == request_from(package, history, read(package/'previous-request.json'))
            and d == template_from(read(package/'capture/preset/template.json'), d['excludedCombatSeeds'], context), 'Changed concrete request or policy scope')
    require(read(package/'native-check.json') == dict(version=VERSION, status='ReadyNoReservation', historicalValues=HISTORY_VALUES,
        newValues=0, fights=0, restarts=12, searchFights=12672, maximumFights=72672), 'Native admission mismatch')
    for name in ('context-process.json', 'native-check-process.json'):
        validate_process(read(package/name))
    require(sha(package/'previous-execution/files.json') == PREVIOUS_EXECUTION_PIN and sha(package/'previous-execution/completion.json')
            == read(package/'previous-execution/files.json')['completion.json'], 'Changed historical cost evidence')
    estimate = forecast(read(package/'previous-execution/completion.json'))
    require(estimate == read(package/'resource-forecast.json') and estimate['fits'], 'Resource estimate does not fit the frozen envelope')
    forbidden = common.LEDGERS | {'entropy.bin', 'allocation.json', 'allocation-journal.jsonl'}
    require(not any(p.name in forbidden for p in common.paths(package)), 'Premature reservation artifact')
    return dict(status='FreshScreeningComparisonAdmittedNoReservation' if SCREENING else 'ReferenceExplorationOffsetComparisonAdmittedNoReservation' if OFFSET else 'ReferenceExplorationComparisonAdmittedNoReservation', requestSha256=sha(package/'request.json'),
        templateSha256=sha(package/'template.json'), executionHash=context['executionHash'], settingsHash=context['settingsHash'],
        compiledSourceDocuments=len(compiled), jitResolvedMethods=len(context['jitResolvedMethods']),
        historicalValues=HISTORY_VALUES, historyFiles=HISTORY_FILES, nativeReferenceInputsPrepared=6,
        nativePreparationCountBasis='Public Check validates both policies and prepares three retained references per policy.',
        maximumScientificFights=72672, admissionChargeSeconds=600, admissionChargeBytes=536870912,
        remainingSeconds=10200, remainingBytes=5905580032, resourceForecast=estimate,
        scientificLaunches=0, newValues=0, fights=0, executionAuthorizedByAdmission=False, outputExists=False)


def worker():
    require(PACKAGE.exists() and not RUN.exists() and not (PACKAGE/'runtime').exists(), 'Existing native admission work; no resume')
    launch = read(PACKAGE/'admission-launch.json')
    deadline = launch['monotonicDeadline']
    def check():
        require(time.monotonic() < deadline and not RUN.exists(), 'Admission deadline or unexpected scientific output')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES-1048576, 'Admission storage ceiling')
    def command(name, args):
        owner = module('admission_nested_owner', PACKAGE/'bounded_windows_process.py')
        result = owner.run(args, str(ROOT), str(PACKAGE/(name+'.log')), deadline, cleanup_seconds=1, check=check)
        save(PACKAGE/(name+'-process.json'), result)
        validate_process(result)
        print(name+' passed', flush=True)
    captured = capture_inventory(CAPTURE)
    common.authenticate(CLOSEOUT, CLOSEOUT_PIN)
    common.authenticate(IMPLEMENTATION, IMPLEMENTATION_PIN)
    previous_manifest = common.authenticate(PREVIOUS_RUN, PREVIOUS_RUN_PIN)
    common.authenticate(PREVIOUS_EXECUTION, PREVIOUS_EXECUTION_PIN)
    tested = implementation_record(IMPLEMENTATION)
    validate_implementation(tested)
    for name in ('BalanceHarness.dll', 'BalanceHarness.pdb'):
        require(sha(BUILD/name) == tested_binaries(IMPLEMENTATION)[name], 'Changed tested binary')
    pins = tested_sources(IMPLEMENTATION)
    for name, value in pins.items():
        if Path(name).suffix in ('.cs', '.csproj', '.py', '.ps1'):
            require(sha(ROOT/name) == value, 'Changed tested source: '+name)
    shutil.copytree(CAPTURE/'runtime', PACKAGE/'runtime')
    shutil.copytree(CAPTURE/'content', PACKAGE/'content')
    for name in HARNESS_FILES:
        shutil.copyfile(BUILD/name, PACKAGE/'runtime'/name)
    for source, target in [(CAPTURE/'files.json', 'capture/files.json'), (CAPTURE/'failure.json', 'capture/failure.json'),
                           (CAPTURE/'preset/template.json', 'capture/preset/template.json'), (CLOSEOUT/'files.json', 'capture/closeout/files.json'),
                           (CLOSEOUT/'receipt.json', 'capture/closeout/receipt.json'), (PREVIOUS_RUN/'request.json', 'previous-request.json'),
                           (PREVIOUS_EXECUTION/'files.json', 'previous-execution/files.json'), (PREVIOUS_EXECUTION/'completion.json', 'previous-execution/completion.json'),
                           (PREVIOUS_EXECUTION/'live-history-files.json', 'previous-execution/live-history-files.json')]:
        dest = PACKAGE/target
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, dest)
    require(sha(PACKAGE/'previous-request.json') == previous_manifest['request.json'], 'Changed previous history contract')
    (PACKAGE/'verification').mkdir()
    for name in ('files.json', *PROOF_FILES):
        shutil.copyfile(IMPLEMENTATION/name, PACKAGE/'verification'/name)
    plan_name = 'Tower-Practical-Fresh-Screening-Comparison-Plan.json' if SCREENING else 'Tower-Practical-Reference-Exploration-' + ('Offset-' if OFFSET else '') + 'Comparison-Plan.json'
    for source, target in [('Balance Harness/'+plan_name, 'plan.json'),
                           ('Balance Harness/analysis/audit-reference-exploration-comparison.py', 'auditor.py'),
                           ('build/run-reference-exploration-comparison.py', 'run-reference-exploration-comparison.py')]:
        shutil.copyfile(ROOT/source, PACKAGE/target)
    validate_runtime(common.inventory(PACKAGE/'runtime'), captured, tested_binaries(IMPLEMENTATION))
    if SCREENING:
        (PACKAGE/'screening-fixture').mkdir()
        for source, target in screening_fixture_paths().items():
            shutil.copyfile(IMPLEMENTATION/source, PACKAGE/'screening-fixture'/target)
    check()
    command('context', ['pwsh', '-NoProfile', '-File', str(PACKAGE/'context.ps1'), '-Package', str(PACKAGE), '-RepositoryRoot', str(ROOT)] + (['-Screening'] if SCREENING else []))
    save(PACKAGE/'runtime-files.json', common.inventory(PACKAGE/'runtime'))
    previous = read(PACKAGE/'previous-request.json')
    files, values = common.history(RUN.parent, previous)
    require(len(files) == HISTORY_FILES and len(values) == HISTORY_VALUES, 'History changed from declared source')
    if OFFSET or SCREENING:
        if HISTORY_SOURCE != IMPLEMENTATION:
            common.authenticate(HISTORY_SOURCE, HISTORY_SOURCE_PIN)
        (PACKAGE/'history-source').mkdir()
        for name in ('files.json', 'history-files.json', 'integrity.json' if SCREENING else 'history-preservation.json'):
            shutil.copyfile(HISTORY_SOURCE/name, PACKAGE/'history-source'/name)
    verify_history_source(PACKAGE, files)
    # The exact inventory is pinned by the closed practical run and independently
    # recomputed by both the native Check and the second complete scan below.
    save(PACKAGE/'history-files.json', files)
    save(PACKAGE/'template.json', template_from(read(PACKAGE/'capture/preset/template.json'), values, read(PACKAGE/'context.json')))
    save(PACKAGE/'request.json', request_from(PACKAGE, files, previous))
    command('native-check', ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'), 'tower-reference-exploration-comparison-check', str(PACKAGE/'request.json')])
    save(PACKAGE/'native-check.json', read(PACKAGE/'native-check.log'))
    current, current_values = common.history(RUN.parent, read(PACKAGE/'request.json'))
    require(current == files and current_values == values, 'History changed during admission')
    save(PACKAGE/'history-after.json', current)
    save(PACKAGE/'resource-forecast.json', forecast(read(PACKAGE/'previous-execution/completion.json')))
    check()
    save(PACKAGE/'worker-completion.json', dict(status='ReadyForIndependentAdmissionAudit', scientificLaunches=0, newValues=0, fights=0))


def prepare():
    require(not PACKAGE.exists() and not RUN.exists(), 'New package and absent scientific output required; no retry or resume')
    started = time.monotonic()
    PACKAGE.mkdir()
    watchdog = threading.Timer(SECONDS, lambda: os._exit(124))
    watchdog.daemon = True
    watchdog.start()
    def check():
        require(time.monotonic()-started < SECONDS and not RUN.exists(), 'Admission elapsed ceiling or scientific output')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES, 'Admission retained storage ceiling')
    try:
        for source, name in [(Path(__file__), 'prepare.py'), (ROOT/'Balance Harness/analysis/reference-exploration-context.ps1', 'context.ps1'),
                             (common_path, 'comparison-preparation.py'), (ROOT/'build/bounded_windows_process.py', 'bounded_windows_process.py')]:
            shutil.copyfile(source, PACKAGE/name)
        save(PACKAGE/'admission-launch.json', dict(version=VERSION, maximumSeconds=SECONDS, maximumBytes=BYTES,
            monotonicDeadline=started+SECONDS-5, admissionChargeSeconds=SECONDS, admissionChargeBytes=BYTES,
            scientificLaunches=0, retries=0, resume=False, parentProcessId=os.getpid(),
            historicalEngineeringAccounting='Separately disclosed; incomplete older totals and prior ledger unchanged.'))
        owner = module('reference_admission_owner', PACKAGE/'bounded_windows_process.py')
        process = owner.run([sys.executable, '-B', '-X', 'utf8', str(PACKAGE/'prepare.py'), '_worker'] + (['--screening'] if SCREENING else ['--offset'] if OFFSET else []), str(ROOT),
                            str(PACKAGE/'worker.log'), started+SECONDS-5, cleanup_seconds=1, check=check)
        save(PACKAGE/'worker-process.json', process)
        validate_process(process)
        result = verify_contents(PACKAGE)
        save(PACKAGE/'admission.json', result)
        check()
        save(PACKAGE/'completion.json', dict(status='AdmissionCompleteNoReservation', measuredSecondsBeforeSeal=time.monotonic()-started,
            bytesBeforeSeal=sum(p.stat().st_size for p in common.paths(PACKAGE)), maximumSeconds=SECONDS, maximumBytes=BYTES,
            admissionChargeSeconds=SECONDS, admissionChargeBytes=BYTES, retries=0, scientificLaunches=0, newValues=0, fights=0))
        save(PACKAGE/'files.json', common.inventory(PACKAGE))
        check()
        pin = dict(status=result['status'], manifestSha256=sha(PACKAGE/'files.json'), requestSha256=result['requestSha256'],
                   measuredSeconds=time.monotonic()-started, retainedBytes=sum(p.stat().st_size for p in common.paths(PACKAGE)),
                   maximumSeconds=SECONDS, maximumBytes=BYTES, scientificLaunches=0, newValues=0, fights=0)
        save(PACKAGE.with_name(PACKAGE.name+'-pin.json'), pin)
        print(json.dumps(pin, indent=2))
    except BaseException as error:
        if not (PACKAGE/'failure.json').exists():
            save(PACKAGE/'failure.json', dict(status='AdmissionFailedNoSearch', reason=str(error), measuredSeconds=time.monotonic()-started,
                scientificLaunches=0, newValues=0, fights=0, retries=0, resume=False))
        raise
    finally:
        watchdog.cancel()


def verify(package, pin, live=False):
    common.authenticate(package, pin)
    result = verify_contents(package)
    require(result == read(package/'admission.json'), 'Changed admission conclusions')
    validate_process(read(package/'worker-process.json'))
    completion = read(package/'completion.json')
    require(completion['status'] == 'AdmissionCompleteNoReservation' and completion['maximumSeconds'] == completion['admissionChargeSeconds'] == 600
            and completion['maximumBytes'] == completion['admissionChargeBytes'] == BYTES
            and 0 <= completion['measuredSecondsBeforeSeal'] < SECONDS and sum(p.stat().st_size for p in common.paths(package)) < BYTES
            and completion['scientificLaunches'] == completion['newValues'] == completion['fights'] == completion['retries'] == 0, 'Changed admission accounting')
    if live:
        files, values = common.history(RUN.parent, read(package/'request.json'))
        require(files == read(package/'history-files.json') and values == read(package/'template.json')['excludedCombatSeeds'], 'Live history differs')
    return dict(result, manifestSha256=pin, readOnlyVerification=True, nativePreparationsDuringVerification=0)


class AdmissionTests(unittest.TestCase):
    def test_exact_template_conversion(self):
        source = read(CAPTURE/'preset/template.json')
        result = template_from(source, [1, 2], dict(settingsHash=SETTINGS, executionHash='e'*64))
        self.assertEqual(source['starts'], result['starts'])
        self.assertEqual(source['references'], result['references'])
        self.assertEqual(([], 4528, BASELINE), (result['generation']['seeds'], result['maximumBattles'], result['generation']['policyVersion']))

    def test_template_history_or_settings_mismatch(self):
        source = read(CAPTURE/'preset/template.json')
        for values, settings in [([2, 1], SETTINGS), ([1, 1], SETTINGS), ([], SETTINGS), ([1], 'changed')]:
            with self.assertRaises(ValueError):
                template_from(source, values, dict(settingsHash=settings, executionHash='e'*64))

    def test_selector_and_nominees_cannot_drift(self):
        source = read(CAPTURE/'preset/template.json')
        for key, value in [('shortlist', 4), ('selectionPolicyVersion', 'changed'), ('selectionPrimaryReferenceId', 'changed')]:
            bad = copy.deepcopy(source)
            bad['stages'][key] = value
            with self.assertRaises(ValueError):
                template_from(bad, [1], dict(settingsHash=SETTINGS, executionHash='e'*64))

    def test_nested_runtime_dependency_preserved(self):
        runtime = {'BalanceHarness.dll': 'new', 'BalanceHarness.pdb': 'symbols', 'runtimes/win/lib/net7.0/System.Management.dll': 'old'}
        if SCREENING:
            runtime.update({'BalanceHarness.deps.json': 'deps', 'BalanceHarness.runtimeconfig.json': 'config'})
        captured = {'runtime/'+k: 'old' for k in runtime}
        validate_runtime(runtime, captured, runtime)
        with self.assertRaises(ValueError):
            validate_runtime(dict(runtime, **{'runtimes/win/lib/net7.0/System.Management.dll': 'changed'}), captured, runtime)
        with self.assertRaises(ValueError):
            validate_runtime(dict(runtime, unexpected='extra'), captured, runtime)

    def test_process_tree_must_be_empty(self):
        good = dict(exitCode=0, timedOut=False, activeProcesses=0, totalProcesses=1, mechanism='suspended-owned-job-v1')
        validate_process(good)
        for key, value in [('exitCode', 1), ('timedOut', True), ('activeProcesses', 1), ('totalProcesses', 0), ('mechanism', 'unowned')]:
            with self.assertRaises(ValueError):
                validate_process(dict(good, **{key: value}))

    def test_cost_forecast_is_estimate_and_includes_audits(self):
        result = forecast(read(PREVIOUS_EXECUTION/'completion.json'))
        self.assertTrue(result['fits'])
        self.assertFalse(result['guaranteedUpperBound'])
        self.assertTrue(result['includesHistoricalAuditAndPublicationCosts'])
        self.assertLess(result['estimatedExecutionSeconds'], 10200)

    def test_failed_or_expensive_cost_source(self):
        source = read(PREVIOUS_EXECUTION/'completion.json')
        with self.assertRaises(ValueError):
            forecast(dict(source, nativeAudit='Failed'))
        self.assertFalse(forecast(dict(source, secondsThroughAudits=1000))['fits'])


class OffsetAdmissionTests(unittest.TestCase):
    def test_failed_or_wrong_version_implementation_is_rejected(self):
        tested = read(IMPLEMENTATION/'verification.json')
        validate_implementation(tested)
        for mutate in [lambda v: v.update(version='tower-reference-exploration-comparison-v1'),
                       lambda v: v.update(planSha256='a'*64),
                       lambda v: v['backendRegression'].update(failed=1),
                       lambda v: v['finalProtocol'].update(passed=1),
                       lambda v: v['python'].update(failed=1)]:
            changed = copy.deepcopy(tested); mutate(changed)
            with self.assertRaises(ValueError):
                validate_implementation(changed)

    def test_new_scope_cannot_target_the_closed_original_output(self):
        require(OFFSET, 'Offset fixture mode required')
        self.assertNotEqual(ROOT/'TestResults/balance/tower-reference-exploration-comparison-20260922', RUN)
        self.assertFalse(RUN.exists())
        self.assertEqual((567789, 234, 600, 536870912), (HISTORY_VALUES, HISTORY_FILES, SECONDS, BYTES))
        self.assertEqual(PLAN_PIN, sha(ROOT/'Balance Harness/Tower-Practical-Reference-Exploration-Offset-Comparison-Plan.json'))


class ScreeningAdmissionTests(unittest.TestCase):
    def test_verified_screening_evidence_and_protocol_required(self):
        tested = implementation_record(IMPLEMENTATION)
        validate_implementation(tested)
        for key, value in [('version', 'tower-reference-exploration-offset-comparison-v1'), ('status', 'Incomplete'),
                           ('backendTests', 153), ('pythonTests', 40), ('newScientificFights', 1),
                           ('runtimeAdmissions', 1), ('historyValues', 567789)]:
            with self.subTest(field=key), self.assertRaises(ValueError):
                validate_implementation(dict(tested, **{key: value}))

    def test_new_scope_and_fixed_envelope(self):
        self.assertEqual('tower-practical-fresh-screening-comparison-v1', VERSION)
        self.assertFalse(RUN.exists())
        self.assertFalse(PACKAGE.exists())
        self.assertEqual((584171, 236, 600, 536870912), (HISTORY_VALUES, HISTORY_FILES, SECONDS, BYTES))
        self.assertEqual(PLAN_PIN, sha(ROOT/'Balance Harness/Tower-Practical-Fresh-Screening-Comparison-Plan.json'))

    def test_binary_receipt_covers_all_four_harness_files(self):
        pins = tested_binaries(IMPLEMENTATION)
        self.assertEqual(set(HARNESS_FILES), set(pins))
        self.assertEqual(pins, {p: sha(BUILD/p) for p in HARNESS_FILES})

    def test_prior_source_coverage_and_screening_overlay_are_bound(self):
        pins = tested_sources(IMPLEMENTATION)
        harness = {name: pin for name, pin in pins.items() if name.startswith('LL/tools/BalanceHarness/') and name.endswith('.cs')}
        self.assertEqual(199, len(harness))
        self.assertIn('LL/tools/BalanceHarness/TowerPracticalScreening.cs', harness)
        self.assertEqual(harness, {name: sha(ROOT/name) for name in harness})
        self.assertEqual(OWNER_PIN, sha(ROOT/'build/bounded_windows_process.py'))

    def test_only_hash_pinned_literal_screening_inputs_are_copied(self):
        proof = read(IMPLEMENTATION/'files.json')
        paths = screening_fixture_paths()
        self.assertEqual({'template.json', 'fixture-values.json', 'candidate.json', 'mechanics.json'}, set(paths.values()))
        self.assertTrue(all(sha(IMPLEMENTATION/path) == proof[path] for path in paths))
        self.assertFalse(common.LEDGERS.intersection(paths.values()))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare', 'verify', 'self-test', '_worker'])
    parser.add_argument('--package', type=Path, default=PACKAGE)
    parser.add_argument('--manifest-sha256')
    parser.add_argument('--live', action='store_true')
    versions = parser.add_mutually_exclusive_group()
    versions.add_argument('--offset', action='store_true', help='Use the separately frozen owner-offset comparison and its new admission package.')
    versions.add_argument('--screening', action='store_true', help='Use the frozen fresh-screening comparison and its new admission package.')
    args = parser.parse_args()
    if args.offset or args.screening:
        configure_screening() if args.screening else configure_offset()
        if args.package == ROOT/'TestResults/reference-exploration-comparison-admission-20260922':
            args.package = PACKAGE
    if args.command == 'self-test':
        classes = [AdmissionTests] + ([ScreeningAdmissionTests] if SCREENING else [OffsetAdmissionTests] if OFFSET else [])
        result = unittest.TextTestRunner(verbosity=2).run(unittest.TestSuite(unittest.defaultTestLoader.loadTestsFromTestCase(c) for c in classes))
        sys.exit(not result.wasSuccessful())
    elif args.command == 'prepare':
        prepare()
    elif args.command == '_worker':
        worker()
    else:
        require(args.manifest_sha256 is not None, 'Supply the externally retained manifest pin')
        print(json.dumps(verify(args.package.resolve(), args.manifest_sha256, args.live), indent=2))
