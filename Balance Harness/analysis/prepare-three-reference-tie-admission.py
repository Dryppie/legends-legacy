"""One bounded admission of the frozen three-reference selector comparison.

Captures the tested harness with retained gameplay dependencies, checks compatibility
and prepares references once per selector. No entropy, search, combat or resume route.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
import sys
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-three-reference-tie-comparison-v1'
PACKAGE = ROOT/'TestResults/three-reference-tie-admission-20260923'
RUN = ROOT/'TestResults/balance/tower-three-reference-tie-comparison-20260923'
CAPTURE = ROOT/'TestResults/three-reference-native-admission-20260922'
CLOSEOUT = ROOT/'TestResults/three-reference-admission-closeout-20260922'
IMPLEMENTATION = ROOT/'TestResults/three-reference-tie-implementation-20260923'
BUILD = ROOT/'TestResults/three-reference-tie-build-20260923/bin/BalanceHarness/release'
PREVIOUS_RUN = ROOT/'TestResults/balance/tower-practical-fresh-screening-comparison-20260922'
PREVIOUS_EXECUTION = ROOT/'TestResults/practical-fresh-screening-comparison-execution-20260923'
CAPTURE_PIN = '001eb3e1a8642f3168f15f4ebc81ec65f33235cbae8ae1e2bc5104c61646142d'
FAILURE_PIN = '8e277d2ad609fe1dd00063f6a9b27930ee15f6a82389e07d933c907613be762e'
CLOSEOUT_PIN = '65cde20aabb7deaffcd889b0e685a9bd26830d63be4678db75a2f4506956d213'
TEMPLATE_PIN = 'cd12abf230a8244170aac3b059daf016dae3f2fccdf3cd2dfa3de2add7741f83'
IMPLEMENTATION_PIN = '0fee841e8ecf3e2adb639fbfdf952ef0609aa782819cea8ba34955853ed2d544'
PREVIOUS_RUN_PIN = '031c7dfdc14f71ff911fdec94472bd161ef72d99b90882ec23ec93dd3fe38042'
PREVIOUS_EXECUTION_PIN = '8223bd466f0ca915dabc89143d6789137ab38d05db33dcba9d1a28a5e0dfba9a'
PLAN_PIN = '2ae905286313000e04170f45acd849c552251954487c99186d4cea1b96fcdf24'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
OWNER_PIN = '0efe39bcabf3ac3665988a9c38185938b77f97585ecf4d7dc22b4a6deb691d30'
SETTINGS = 'f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74'
SECONDS, BYTES = 600, 536870912
HISTORY_VALUES, HISTORY_FILES = 600552, 238
HARNESS_FILES = ('BalanceHarness.dll', 'BalanceHarness.pdb', 'BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json')
PROOF_FILES = ('completion.json', 'verification.json', 'source-files.json', 'tested-harness-files.json',
               'backend-final.trx', 'python-three-envelope-final.log', 'python-legacy-saved.log', 'integrity.json', 'history-files.json')
INPUTS = {'Balance Harness/Tower-Practical-Three-Reference-Tie-Comparison-Plan.json': 'plan.json',
          'Balance Harness/analysis/audit-incumbent-tie-comparison.py': 'auditor.py',
          'build/run-three-reference-tie-comparison.py': 'run-three-reference-tie-comparison.py'}
FIXTURE_FILES = {'three-fixture/template.json': 'template.json', 'three-fixture/allocation.json': 'fixture-values.json',
                 'three-fixture/study/search-01.json': 'search.json', 'three-fixture/study/generation-mechanics.json': 'mechanics.json'}


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result)
    return result


common_path = Path(__file__).with_name('comparison-preparation.py')
if not common_path.exists(): common_path = CLOSEOUT/'comparison-preparation.py'
assert hashlib.sha256(common_path.read_bytes()).hexdigest() == COMMON_PIN
common = module('three_reference_tie_admission_common', common_path)
require, read, sha, save = common.require, common.read, common.sha, common.save


def validate_implementation(value):
    require(value['status'] == 'ThreeReferenceTieImplementationVerifiedNeedsAdmission'
            and value['backendTests'] == 241 and value['pythonTests'] == 18
            and (value['historyValues'], value['historyFiles']) == (HISTORY_VALUES, HISTORY_FILES)
            and value['newScientificFights'] == value['newScientificValues'] == value['scientificLaunches'] == 0
            and not value['capturedRuntimeAdmitted'] and value['fullHistoricalEngineeringTotals'] is None
            and (value['previousEngineeringSeconds'], value['previousEngineeringMiB']) == (18180, 13584),
            'Missing passing implementation evidence or changed accounting')


def tested_binaries(directory):
    pins = read(directory/'tested-harness-files.json')
    require(all(name in pins for name in HARNESS_FILES), 'Incomplete tested harness receipt')
    return {name:pins[name] for name in HARNESS_FILES}


def tested_sources(directory):
    producing = read(directory/'source-files.json')
    pins = read(directory/'verification.json')['sourceHashes']
    require(len(producing) == 203 and all(pins.get(name) == value for name,value in producing.items()), 'Incomplete tested source coverage')
    return pins


def capture_inventory(package):
    require(sha(package/'files.json') == CAPTURE_PIN and sha(package/'failure.json') == FAILURE_PIN, 'Changed reconciled capture')
    files = read(package/'files.json')
    require({p.relative_to(package).as_posix() for p in common.paths(package)} == set(files)|{'files.json','failure.json'}, 'Changed capture membership')
    for name,value in files.items():
        path = (package/name).resolve()
        require(path.is_relative_to(package.resolve()) and sha(path) == value, 'Changed capture file: '+name)
    require(files['preset/template.json'] == TEMPLATE_PIN, 'Changed captured template')
    return files


def validate_runtime(runtime, captured, tested):
    require(set(runtime) == {name[8:] for name in captured if name.startswith('runtime/')}, 'Changed runtime membership')
    require(all(runtime.get(name) == tested[name] for name in HARNESS_FILES), 'Untested producing harness')
    for name,value in runtime.items():
        if name not in HARNESS_FILES: require(value == captured['runtime/'+name], 'Changed captured dependency: '+name)


def template_from(capture, values, context):
    require(context['settingsHash'] == capture['settingsHash'] == SETTINGS, 'Changed settings')
    require(values == sorted(set(values)) and 0 < len(values) <= 967232, 'Invalid complete history')
    result = copy.deepcopy(capture)
    result.update(id='three-reference-tie-template', executionHash=context['executionHash'], excludedCombatSeeds=values, maximumBattles=4528)
    result.pop('primaryReferenceId', None)
    result['generation'].update(policyVersion='retained-composition-three-references-v1', seeds=[])
    require(result['generation']['candidatesPerArm'] == 46 and result['generation']['maximumAttemptsPerArm'] == 256
            and len(result['starts']) == len(result['references']) == 3 and result['stages']['shortlist'] == 5
            and result['stages']['selectionPolicyVersion'] == 'tower-staged-incumbent-tie-v1'
            and result['stages']['selectionPrimaryReferenceId'] == 'confirmed-399bc7760fb0cf790a5d8ac4'
            and all(s[k] == [] for s in result['stages']['schedules'].values() for k in ('discovery','selection','confirmation','diagnostics')),
            'Changed unscheduled direct comparison scope')
    return result


def request_from(package, history, previous):
    return dict(version=VERSION, captureRoot=str(package/'capture'), captureCloseoutRoot=str(package/'capture/closeout'),
        contentRoot=str(package/'content'), templatePath=str(package/'template.json'), templateHash=sha(package/'template.json'),
        registryRoot=str(RUN.parent), outputRoot=str(RUN), requiredHistory=history,
        pendingHistoryRecoveries=previous['pendingHistoryRecoveries'], recoveryReceiptHashes=previous['recoveryReceiptHashes'],
        planPath=str(package/'plan.json'), planHash=PLAN_PIN, auditorPath=str(package/'auditor.py'), auditorHash=sha(package/'auditor.py'),
        maximumSeconds=10800, maximumBytes=6442450944, priorSeconds=SECONDS, priorBytes=BYTES)


def forecast(previous):
    require(previous['status'] == 'FreshScreeningComparisonExecutionVerified' and previous['scope'] == 'Closed'
            and previous['archiveManifestSha256'] == PREVIOUS_RUN_PIN and previous['completedFights'] == 55672
            and previous['nativeAudit'] == previous['independentAudit'] == 'Passed'
            and previous['closeoutNewFights'] == previous['closeoutNewValues'] == previous['retries'] == 0,
            'Invalid historical cost source')
    factor = 60672/55672
    seconds, size = previous['executionSeconds']*factor, math.ceil(previous['retainedBytes']*factor)
    require(seconds > 0 and size > 0 and math.isfinite(seconds), 'Invalid forecast arithmetic')
    return dict(status='HistoricalLinearEstimate', sourceFights=55672, targetMaximumFights=60672,
        sourceSeconds=previous['executionSeconds'], sourceBytes=previous['retainedBytes'],
        estimatedExecutionSeconds=seconds, estimatedExecutionBytes=size, remainingSeconds=10200, remainingBytes=5905580032,
        fits=seconds < 10200 and size < 5905580032, guaranteedUpperBound=False, includesHistoricalAuditAndPublicationCosts=True,
        caveat='Search and unseen battles can cost more; absolute caps remain binding, with no extension or retry.')


def validate_process(value):
    require(value['exitCode'] == 0 and not value['timedOut'] and value['activeProcesses'] == 0 and value['totalProcesses'] >= 1
            and value['mechanism'] == 'suspended-owned-job-v1', 'Incomplete owned process')


def verify_contents(package):
    require(not RUN.exists() and not (package/'failure.json').exists(), 'Scientific output or failed admission exists')
    q, d, context = read(package/'request.json'), read(package/'template.json'), read(package/'context.json')
    for name,pin in [('capture/files.json',CAPTURE_PIN), ('capture/failure.json',FAILURE_PIN),
                     ('capture/preset/template.json',TEMPLATE_PIN), ('capture/closeout/files.json',CLOSEOUT_PIN),
                     ('verification/files.json',IMPLEMENTATION_PIN), ('previous-run-files.json',PREVIOUS_RUN_PIN),
                     ('previous-execution/files.json',PREVIOUS_EXECUTION_PIN)]:
        require(sha(package/name) == pin, 'Changed provenance: '+name)
    closeout = read(package/'capture/closeout/files.json')
    require(sha(package/'capture/closeout/receipt.json') == closeout['receipt.json'], 'Changed reconciliation receipt')
    receipt = read(package/'capture/closeout/receipt.json')
    require(receipt['closeoutStatus'] == 'ReadOnlyReconciled' and receipt['manifestSha256'] == CAPTURE_PIN
            and receipt['reconciledCloseoutFailureSha256'] == FAILURE_PIN, 'Unreconciled source failure')
    proof = read(package/'verification/files.json')
    for name in PROOF_FILES: require(sha(package/'verification'/name) == proof[name], 'Changed implementation evidence: '+name)
    validate_implementation(read(package/'verification/completion.json'))
    runtime, captured = common.inventory(package/'runtime'), read(package/'capture/files.json')
    validate_runtime(runtime, captured, tested_binaries(package/'verification'))
    require(runtime == read(package/'runtime-files.json') and all(runtime.get(k+'.dll') == v for k,v in context['execution']['assemblyHashes'].items()), 'Changed loaded runtime')
    require(common.inventory(package/'content') == {name[8:]:value for name,value in captured.items() if name.startswith('content/')}, 'Changed content')
    source_pins = tested_sources(package/'verification')
    compiled = read(package/'compiled-source-files.json')
    require(len(compiled) == context['compiledSourceDocuments'] == 203
            and all(sha(package/'source'/name) == value for name,value in compiled.items()), 'Changed producing sources')
    require(all(source_pins.get(name) == value for name,value in compiled.items() if name.startswith('LL/tools/BalanceHarness/')), 'Untested producing source')
    require(context['status'] == 'CapturedRuntimeEntryPathsCompatible' and context['settingsHash'] == SETTINGS
            and context['nativePreparations'] == context['fights'] == context['newValues'] == 0, 'Changed compatibility scope')
    for method in ('Check','Run','VerifyStudy','Assess','Reserve','Select','Bind'):
        require(any('TowerIncumbentTieComparison.'+method+'#' in name for name in context['jitResolvedMethods']), 'Unresolved comparison path: '+method)
    for name,target in FIXTURE_FILES.items():
        require(sha(package/'selector-fixture'/target) == proof[name], 'Changed saved selector fixture')
    search = read(package/'selector-fixture/search.json')
    require(context['selectorFixture'] == dict(status='StoredMeasurementsReconstructed',version=VERSION,
        searchFileSha256=sha(package/'selector-fixture/search.json'), nominees=5, selectionMeasurements=5,
        baselineSelector='tower-staged-incumbent-tie-v1', candidateSelector='tower-staged-three-reference-tie-v1',
        baselineParty=search['baseline']['finalist']['party']['id'], candidateParty=search['candidate']['finalist']['party']['id'],
        newFights=0,newValues=0), 'Incomplete captured selector reconstruction')
    require(search['baseline']['recipeHash'] != search['candidate']['recipeHash'], 'Fixture does not exercise changed selection')
    for source,target in INPUTS.items(): require(sha(package/target) == source_pins[source], 'Untested comparison input: '+source)
    require(sha(package/'plan.json') == PLAN_PIN and sha(package/'bounded_windows_process.py') == OWNER_PIN, 'Changed frozen plan or process owner')
    require(sha(package/'comparison-preparation.py') == COMMON_PIN, 'Changed history helper')
    helpers = read(package/'admission-helpers.json')
    require(all(sha(package/name) == value for name,value in helpers.items()) and set(helpers) == {'prepare.py','context.ps1','comparison-preparation.py','bounded_windows_process.py'}, 'Changed admission helpers')
    history = read(package/'history-files.json')
    require(history == read(package/'history-after.json') == read(package/'verification/history-files.json')
            and len(history) == HISTORY_FILES and len(d['excludedCombatSeeds']) == HISTORY_VALUES, 'Changed complete history')
    previous_manifest = read(package/'previous-run-files.json')
    require(sha(package/'previous-request.json') == previous_manifest['request.json'], 'Changed previous history contract')
    require(q == request_from(package, history, read(package/'previous-request.json'))
            and d == template_from(read(package/'capture/preset/template.json'), d['excludedCombatSeeds'], context), 'Changed bound request or template')
    require(read(package/'native-check.json') == dict(version=VERSION,status='ReadyNoReservation',historicalValues=HISTORY_VALUES,
        newValues=0,fights=0,restarts=24,searchFights=12672,maximumFights=60672), 'Native admission mismatch')
    for name in ('context-process.json','native-check-process.json'): validate_process(read(package/name))
    require(sha(package/'previous-execution/closeout.json') == read(package/'previous-execution/files.json')['closeout.json'], 'Changed cost source')
    estimate = forecast(read(package/'previous-execution/closeout.json'))
    require(estimate == read(package/'resource-forecast.json') and estimate['fits'], 'Resource forecast does not fit')
    forbidden = common.LEDGERS|{'entropy.bin','allocation.json','allocation-journal.jsonl'}
    require(not any(p.name in forbidden for p in common.paths(package)), 'Premature reservation artifact')
    return dict(status='ThreeReferenceTieComparisonAdmittedNoReservation',version=VERSION,requestSha256=sha(package/'request.json'),
        templateSha256=sha(package/'template.json'),executionHash=context['executionHash'],settingsHash=context['settingsHash'],
        compiledSourceDocuments=len(compiled),jitResolvedMethods=len(context['jitResolvedMethods']),
        historicalValues=HISTORY_VALUES,historyFiles=HISTORY_FILES,nativeReferenceInputsPrepared=6,
        nativePreparationCountBasis='One public Check validates both selectors and prepares three retained references per selector.',
        maximumScientificFights=60672,admissionChargeSeconds=SECONDS,admissionChargeBytes=BYTES,
        remainingSeconds=10200,remainingBytes=5905580032,resourceForecast=estimate,
        scientificLaunches=0,newValues=0,fights=0,executionAuthorizedByAdmission=False,outputExists=False)


def worker():
    require(PACKAGE.exists() and not RUN.exists() and not (PACKAGE/'runtime').exists(), 'Existing admission work; no resume')
    deadline = read(PACKAGE/'admission-launch.json')['monotonicDeadline']
    def check():
        require(time.monotonic() < deadline and not RUN.exists(), 'Admission deadline or unexpected scientific output')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES-1048576, 'Admission storage ceiling')
    def command(name,args):
        owner = module('nested_tie_admission_owner',PACKAGE/'bounded_windows_process.py')
        result = owner.run(args,str(ROOT),str(PACKAGE/(name+'.log')),deadline,cleanup_seconds=1,check=check)
        save(PACKAGE/(name+'-process.json'),result); validate_process(result)
        print(name+' passed',flush=True)
    captured = capture_inventory(CAPTURE)
    for path,pin in [(CLOSEOUT,CLOSEOUT_PIN),(IMPLEMENTATION,IMPLEMENTATION_PIN),(PREVIOUS_RUN,PREVIOUS_RUN_PIN),(PREVIOUS_EXECUTION,PREVIOUS_EXECUTION_PIN)]:
        common.authenticate(path,pin); check()
    validate_implementation(read(IMPLEMENTATION/'completion.json'))
    tested = tested_binaries(IMPLEMENTATION)
    require(all(sha(BUILD/name) == value for name,value in tested.items()), 'Changed tested harness')
    pins = tested_sources(IMPLEMENTATION)
    for name,value in pins.items():
        if Path(name).suffix in ('.cs','.csproj','.py'): require(sha(ROOT/name) == value, 'Changed tested source: '+name)
    require(sha(ROOT/'build/bounded_windows_process.py') == OWNER_PIN, 'Changed process owner')
    shutil.copytree(CAPTURE/'runtime',PACKAGE/'runtime'); shutil.copytree(CAPTURE/'content',PACKAGE/'content')
    for name in HARNESS_FILES: shutil.copyfile(BUILD/name,PACKAGE/'runtime'/name)
    copies = [(CAPTURE/'files.json','capture/files.json'),(CAPTURE/'failure.json','capture/failure.json'),
              (CAPTURE/'preset/template.json','capture/preset/template.json'),(CLOSEOUT/'files.json','capture/closeout/files.json'),
              (CLOSEOUT/'receipt.json','capture/closeout/receipt.json'),(PREVIOUS_RUN/'files.json','previous-run-files.json'),
              (PREVIOUS_RUN/'request.json','previous-request.json'),(PREVIOUS_EXECUTION/'files.json','previous-execution/files.json'),
              (PREVIOUS_EXECUTION/'closeout.json','previous-execution/closeout.json')]
    copies += [(IMPLEMENTATION/name,'verification/'+name) for name in ('files.json',*PROOF_FILES)]
    copies += [(ROOT/name,target) for name,target in INPUTS.items()]
    copies += [(IMPLEMENTATION/name,'selector-fixture/'+target) for name,target in FIXTURE_FILES.items()]
    for source,target in copies:
        dest = PACKAGE/target; dest.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(source,dest)
    validate_runtime(common.inventory(PACKAGE/'runtime'),captured,tested); check()
    command('context',['pwsh','-NoProfile','-File',str(PACKAGE/'context.ps1'),'-Package',str(PACKAGE),'-RepositoryRoot',str(ROOT),'-ThreeReferenceTie'])
    save(PACKAGE/'runtime-files.json',common.inventory(PACKAGE/'runtime'))
    previous = read(PACKAGE/'previous-request.json')
    files,values = common.history(RUN.parent,previous)
    require((len(values),len(files)) == (HISTORY_VALUES,HISTORY_FILES) and files == read(IMPLEMENTATION/'history-files.json'), 'Changed authoritative history')
    save(PACKAGE/'history-files.json',files)
    save(PACKAGE/'template.json',template_from(read(PACKAGE/'capture/preset/template.json'),values,read(PACKAGE/'context.json')))
    save(PACKAGE/'request.json',request_from(PACKAGE,files,previous))
    command('native-check',['dotnet',str(PACKAGE/'runtime/BalanceHarness.dll'),'tower-incumbent-tie-comparison-check',str(PACKAGE/'request.json')])
    save(PACKAGE/'native-check.json',read(PACKAGE/'native-check.log'))
    current,current_values = common.history(RUN.parent,read(PACKAGE/'request.json'))
    require(current == files and current_values == values, 'History changed during admission')
    save(PACKAGE/'history-after.json',current)
    save(PACKAGE/'resource-forecast.json',forecast(read(PACKAGE/'previous-execution/closeout.json')))
    check(); save(PACKAGE/'worker-completion.json',dict(status='ReadyForIndependentAdmissionAudit',scientificLaunches=0,newValues=0,fights=0))


def prepare():
    require(not PACKAGE.exists() and not RUN.exists(), 'New admission package and absent scientific output required; no retry or resume')
    started = time.monotonic(); PACKAGE.mkdir()
    watchdog = threading.Timer(SECONDS,lambda:os._exit(124)); watchdog.daemon=True; watchdog.start()
    def check():
        require(time.monotonic()-started < SECONDS and not RUN.exists(), 'Admission elapsed ceiling or scientific output')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES, 'Admission storage ceiling')
    try:
        for source,target in [(Path(__file__),'prepare.py'),(ROOT/'Balance Harness/analysis/reference-exploration-context.ps1','context.ps1'),
                              (common_path,'comparison-preparation.py'),(ROOT/'build/bounded_windows_process.py','bounded_windows_process.py')]:
            shutil.copyfile(source,PACKAGE/target)
        save(PACKAGE/'admission-helpers.json',{name:sha(PACKAGE/name) for name in ('prepare.py','context.ps1','comparison-preparation.py','bounded_windows_process.py')})
        save(PACKAGE/'admission-launch.json',dict(version=VERSION,maximumSeconds=SECONDS,maximumBytes=BYTES,
            monotonicDeadline=started+SECONDS-5,admissionChargeSeconds=SECONDS,admissionChargeBytes=BYTES,
            scientificLaunches=0,retries=0,resume=False,parentProcessId=os.getpid(),historicalEngineeringAccounting='Separately disclosed; incomplete older totals and prior ledger unchanged.'))
        owner = module('tie_admission_owner',PACKAGE/'bounded_windows_process.py')
        process = owner.run([sys.executable,'-B','-X','utf8',str(PACKAGE/'prepare.py'),'_worker'],str(ROOT),str(PACKAGE/'worker.log'),
                            started+SECONDS-5,cleanup_seconds=1,check=check)
        save(PACKAGE/'worker-process.json',process); validate_process(process)
        result = verify_contents(PACKAGE); save(PACKAGE/'admission.json',result); check()
        save(PACKAGE/'completion.json',dict(status='AdmissionCompleteNoReservation',measuredSecondsBeforeSeal=time.monotonic()-started,
            bytesBeforeSeal=sum(p.stat().st_size for p in common.paths(PACKAGE)),maximumSeconds=SECONDS,maximumBytes=BYTES,
            admissionChargeSeconds=SECONDS,admissionChargeBytes=BYTES,retries=0,scientificLaunches=0,newValues=0,fights=0))
        save(PACKAGE/'files.json',common.inventory(PACKAGE)); check()
        pin = dict(status=result['status'],manifestSha256=sha(PACKAGE/'files.json'),requestSha256=result['requestSha256'],
            measuredSeconds=time.monotonic()-started,retainedBytes=sum(p.stat().st_size for p in common.paths(PACKAGE)),
            maximumSeconds=SECONDS,maximumBytes=BYTES,scientificLaunches=0,newValues=0,fights=0)
        save(PACKAGE.with_name(PACKAGE.name+'-pin.json'),pin); print(json.dumps(pin,indent=2))
    except BaseException as error:
        if not (PACKAGE/'failure.json').exists():
            save(PACKAGE/'failure.json',dict(status='AdmissionFailedNoSearch',reason=str(error),measuredSeconds=time.monotonic()-started,
                scientificLaunches=0,newValues=0,fights=0,retries=0,resume=False))
        raise
    finally: watchdog.cancel()


def verify(package,pin,live=False):
    common.authenticate(package,pin)
    result = verify_contents(package)
    require(result == read(package/'admission.json'), 'Changed admission conclusions')
    validate_process(read(package/'worker-process.json'))
    completion = read(package/'completion.json')
    require(completion['status'] == 'AdmissionCompleteNoReservation' and completion['maximumSeconds'] == completion['admissionChargeSeconds'] == SECONDS
            and completion['maximumBytes'] == completion['admissionChargeBytes'] == BYTES and 0 <= completion['measuredSecondsBeforeSeal'] < SECONDS
            and sum(p.stat().st_size for p in common.paths(package)) < BYTES
            and completion['scientificLaunches'] == completion['newValues'] == completion['fights'] == completion['retries'] == 0, 'Changed admission accounting')
    if live:
        files,values = common.history(RUN.parent,read(package/'request.json'))
        require(files == read(package/'history-files.json') and values == read(package/'template.json')['excludedCombatSeeds'], 'Live history differs')
    common.authenticate(package,pin)
    return dict(result,manifestSha256=pin,readOnlyVerification=True,nativePreparationsDuringVerification=0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command',choices=['prepare','verify','_worker'])
    parser.add_argument('--package',type=Path,default=PACKAGE)
    parser.add_argument('--manifest-sha256')
    parser.add_argument('--live',action='store_true')
    args = parser.parse_args()
    if args.command == 'prepare': prepare()
    elif args.command == '_worker': worker()
    else:
        require(args.manifest_sha256 is not None, 'Supply the retained external manifest pin')
        print(json.dumps(verify(args.package.resolve(),args.manifest_sha256,args.live),indent=2))
