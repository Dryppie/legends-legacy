"""One pinned, bounded allied-action comparison admission. No allocation or combat."""
import argparse
import copy
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
import threading
import time

ROOT = Path(__file__).resolve().parents[2]

def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value

resource = module('allied_action_resource_basis', Path(__file__).with_name('prepare-proposal-affinity-resource-admission.py'))
base, owner = resource.base, resource.owner
require, read, sha, save, inventory = base.require, base.read, base.sha, base.save, base.inventory
VERSION = 'tower-affinity-allied-action-admission-v1'
STUDY = 'tower-affinity-allied-action-comparison-v1'
PACKAGE = ROOT/'TestResults/affinity-allied-action-admission-20260924'
OUTPUT = ROOT/'TestResults/balance/tower-affinity-allied-action-pilot-01-20260924'
BUILD = ROOT/'TestResults/affinity-allied-action-comparison-build-20260924/bin/BalanceHarness/release'
PLAN = ROOT/'Balance Harness/Tower-Affinity-Allied-Action-Comparison-Plan.json'
SECONDS, BYTES = 900, 1073741824
CAPTURE = ROOT/'TestResults/benchmark-validation-admission-20260924'
PREVIEW = ROOT/'TestResults/affinity-creation-preview-20260923'
PRESERVING_PREVIEW = ROOT/'TestResults/affinity-creation-preservation-preview-20260924'
VERIFICATION = ROOT/'TestResults/affinity-allied-action-comparison-implementation-verification-20260924'
ALLIED_PREVIEW = ROOT/'TestResults/affinity-allied-action-preview-20260924-v2'
DESIGN = ROOT/'Balance Harness/Tower-Affinity-Allied-Action-Comparison-Design.json'
PRIOR = VERIFICATION
FLOOR = ROOT/'TestResults/affinity-preservation-admission-20260924'
COMPLETED = ROOT/'TestResults/balance/tower-affinity-preservation-pilot-01-20260924'
ALLIED_FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-allied-action-20260924/result'
PRESERVATION_FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-affinity-preservation-20260924/result'
PINS = {
    CAPTURE: 'e6fdb1369df6585fde905523e3190af221ef0dc9323e4b3ae3b1bf37659b5576',
    PREVIEW: 'db58d54f7a1c595e6b39995508643f16207941be9b772719ac039a9e1775ad39',
    PRESERVING_PREVIEW: '7b001519484ecac83fd6c3ce64085bd63cb56c595e56fa3eda5fe04427e85023',
    VERIFICATION: 'a6227929983b1cb67ead01e15ef60778b8e6285dd3a060bcae040a8d7113a517',
    ALLIED_PREVIEW: '8301070ad78ab8ea84875faf8092c7ae5da193f1d0106245e417b70f689231b1',
    FLOOR: '607b32435968ca9ea96e35234b5246aa0a359ef9b178521d7719b2b57d6b1109',
    COMPLETED: '1c4fb870912fca7b168596cc86645cb54e96361ba8099eac1da5b4b8748d49e4',
    ALLIED_FIXTURE: '317381ecc45142f9342d77d6d2bc9e227f9de77bbf7053a3faeefc4a47f1319b',
    PRESERVATION_FIXTURE: 'e8025ce3cbec2339f4c89d5b951c6fb2b2b73fb283a123fc6798e3eb1e4752f0',
    base.RECOGNITION: base.RECOGNITION_PIN,
    resource.PROBE: resource.PROBE_PIN,
}
CONTRACT = dict(version=VERSION, studyVersion=STUDY, scopeId='affinity-allied-action-pilot-01',
    package=PACKAGE.relative_to(ROOT).as_posix(), output=OUTPUT.relative_to(ROOT).as_posix(), harness=BUILD.relative_to(ROOT).as_posix(), resourceEnvelope=owner.RESOURCE_V2,
    chargedSeconds=SECONDS, chargedBytes=BYTES, scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
    precedingRecordedSeconds=39835.92595320009, precedingRecordedBytes=31216003784,
    precedingDeclaredMaximumSeconds=103920, precedingDeclaredMaximumBytes=64642613248,
    originalPlanFileSha256='907a31ea88910dad1f1d5a6d50c6a15e8077ccb5bc5e7c27a9b7099f75daf147',
    harnessSha256='deecc8b2fec1a1078ea417aed622d006038be056471df04a8c8ec0a84b2af007',
    designFileSha256='18e413460b6339fc1832a475eafa5cc7a986fbac4dd3a64ffc7ef3b64cfbe371',
    backendTestCount=125, probeRecipeInstances=568, preparationsPerRuntime=1136, alliedPreviewRoots=12,
    retries=0, timingResamples=0, scientificLaunches=0, admissionOnly=True,
    sourceManifests={p.relative_to(ROOT).as_posix(): pin for p,pin in PINS.items()},
    forecastRule='Upward-only maximum of prior admission, frozen native audit probe, current preparation and allied-action-fixture forecast, and twice the completed preservation study scaled from 15744 to 21888 fights with upward-only allied-action/preservation fixture phase ratios. Apply all storage floors before scaling the frozen audit probe. Margin 2 and publication reserve 120 remain. No timing retry.')
IMPLEMENTATION = (
    'Balance Harness/analysis/prepare-affinity-allied-action-admission.py',
    'Balance Harness/analysis/test-affinity-allied-action-admission.py',
    'Balance Harness/analysis/prepare-affinity-creation-admission.py',
    'Balance Harness/analysis/prepare-proposal-affinity-admission.py',
    'Balance Harness/analysis/prepare-proposal-affinity-resource-admission.py',
    'Balance Harness/analysis/affinity-allied-action-admission-context.ps1',
    'Balance Harness/analysis/audit-proposal-affinity-study.py',
    'build/run-proposal-affinity-study.py', 'build/bounded_windows_process.py',
    str(base.HISTORY_HELPER.relative_to(ROOT)),
)

class Evidence:
    """Authenticate consumed evidence without rescanning historical battle payloads."""
    def __init__(self, root, pin):
        self.root, self.pin, self.used = root, pin, {'files.json': pin}
        require(sha(root/'files.json') == pin, 'Changed external source manifest')
        self.files = read(root/'files.json')
        self.index_root = root
    def get(self, name):
        path = self.root/name
        require(path.resolve().is_relative_to(self.root.resolve()), 'Escaping consumed source')
        key = path.relative_to(self.index_root).as_posix()
        require(key in self.files and sha(path) == self.files[key], 'Changed consumed source: '+name)
        self.used[name] = self.files[key]; return path
    def recheck(self):
        require(all(sha(self.root/n) == pin for n,pin in self.used.items()), 'Consumed evidence changed during admission')

def validate_declaration(d):
    require(set(d) == set(CONTRACT)|{'harnessFiles','implementationFiles','tests'}, 'Unknown or missing declaration field')
    require(all(d[k] == v for k,v in CONTRACT.items()), 'Changed allied-action comparison admission contract')
    require(set(d['harnessFiles']) == set(base.HARNESS_FILES)
            and all(sha(BUILD/n) == pin for n,pin in d['harnessFiles'].items())
            and d['harnessFiles']['BalanceHarness.dll'] == CONTRACT['harnessSha256'], 'Changed tested build')
    require(set(d['implementationFiles']) == set(IMPLEMENTATION)
            and all(sha(ROOT/n) == pin for n,pin in d['implementationFiles'].items()), 'Changed admission implementation')
    tests = d['tests']; path = ROOT/tests['path']
    require(path == ROOT/'TestResults/affinity-allied-action-admission-tests-20260924.log'
            and sha(path) == tests['sha256'] and tests['passed'] >= 28 and tests['failed'] == 0
            and f"Ran {tests['passed']} tests in " in path.read_text(encoding='utf-8-sig')
            and '\nOK' in path.read_text(encoding='utf-8-sig'), 'Missing admission test evidence')
    require(sha(PLAN) == CONTRACT['originalPlanFileSha256'] and sha(DESIGN) == CONTRACT['designFileSha256']
            and read(PLAN) == read(DESIGN)['plannedNativePlan'], 'Changed original allied-action plan or frozen design')

def probes(export, preserving, allied, context):
    seeds = sorted(base.legacy_values(context)); require(len(seeds) >= 2, 'Missing declared probe values')
    require(len(export['arms']) == 4 and {arm['name'] for arm in export['arms']} == {
        'legacy-v1','benchmark-single-control-v2','benchmark-damage-preserving-single-v2','benchmark-affinity-creation-v3'}
        and all(len(arm['teams']) == 12 for arm in export['arms']), 'All four arms and reference instances must be qualified')
    require(preserving['version'] == 'tower-proposal-export-v4' and len(preserving['arms']) == 2
            and {arm['name'] for arm in preserving['arms']} == {'benchmark-affinity-creation-v3','benchmark-preserving-affinity-creation-v4'}
            and all(arm['status'] == 'Complete' and len(arm['teams']) == 20
                    and len(arm['batch']['candidates']) == 9 and len(arm['secondWave']['batch']['candidates']) == 8
                    for arm in preserving['arms']), 'Both complete preserving-preview waves must be qualified')
    require(len(allied) == CONTRACT['alliedPreviewRoots'], 'All twelve saved allied-action roots are required')
    for preview in allied:
        require(preview['version'] == 'tower-proposal-export-v5' and len(preview['arms']) == 2
            and {arm['name'] for arm in preview['arms']} == {'benchmark-preserving-affinity-creation-v4','benchmark-allied-action-affinity-creation-v5'}
            and all(arm['status'] == 'Complete' and len(arm['teams']) == 20
                and len(arm['batch']['candidates']) == 9 and len(arm['secondWave']['batch']['candidates']) == 8
                for arm in preview['arms']), 'Both complete allied-action waves must be qualified on every saved root')
    rows = []
    for arm in export['arms'] + preserving['arms'] + [arm for preview in allied for arm in preview['arms']]:
        for team in arm['teams']:
            scenario = copy.deepcopy(team['scenario']); scenario['seeds'] = [seeds[0], seeds[-1]]
            rows.append(dict(scenario=scenario, seeds=scenario['seeds']))
    require(len(rows) == CONTRACT['probeRecipeInstances'], 'Incomplete qualification coverage')
    return rows

def qualified(baseline, candidate, before, after, captured, context):
    old, new = baseline['execution'], candidate['execution']
    require(old == captured['execution'] and baseline['executionHash'] == captured['executionHash'] == context['scope']['executionHash'],
            'Changed captured baseline')
    require({k:v for k,v in old.items() if k != 'assemblyHashes'} == {k:v for k,v in new.items() if k != 'assemblyHashes'}
            and {k:v for k,v in old['assemblyHashes'].items() if k != 'BalanceHarness'} ==
                {k:v for k,v in new['assemblyHashes'].items() if k != 'BalanceHarness'}
            and new['assemblyHashes']['BalanceHarness'] == CONTRACT['harnessSha256'], 'Changed gameplay runtime or platform')
    require(before == after and len(before) == len({r['ordinal'] for r in before}) == CONTRACT['preparationsPerRuntime'], 'Changed native inputs/preparations')
    for item in (baseline, candidate):
        require(item['materializations'] == item['nativePreparations'] == CONTRACT['preparationsPerRuntime'] and item['fights'] == item['newValues'] == 0
                and item['settingsHash'] == context['scope']['settingsHash'], 'Incomplete native qualification')
    require(candidate['replayedArms'] == 4 and candidate['preservingReplayedArms'] == 2
            and candidate['alliedReplayedRoots'] == 12 and candidate['alliedReplayedArms'] == 24 and candidate['compiledSourceDocuments'] > 0
            and all(any(required in m for m in candidate['jitResolvedMethods']) for required in (
                'TowerAffinityCreation.Create', 'TowerProposalComparison.CreateValidationPlan',
                'TowerProposalComparison.ValidateValidationTrajectories', 'TowerProposalComparison.CreatePreservationPlan',
                'TowerProposalComparison.ValidatePreservationTrajectories', 'TowerProposalComparison.CreateAlliedActionPlan',
                'TowerAlliedActionProtection.Create', 'TowerBatchRacing.Select', 'TowerBenchmarkValidation.Gate', 'TowerBenchmarkValidation.Decide')), 'Missing allied-action entry points or producing sources')

def forecast(previous, probe, historical_bytes, estimate, completed, fights, allied_fixture, preservation_fixture):
    require(fights == 15744 and completed['status'] == 'Complete'
            and completed['version'] == preservation_fixture['version'] == 'tower-affinity-preservation-comparison-v1'
            and allied_fixture['version'] == STUDY
            and all(x['status'] == 'Complete' for x in (allied_fixture,preservation_fixture)), 'Changed sizing evidence')
    floors, ratios, adjusted = {}, {}, dict(estimate)
    for key in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes'):
        values = (completed[key], allied_fixture[key], preservation_fixture[key], estimate[key])
        require(all(type(v) in (int,float) and math.isfinite(v) and v > 0 for v in values), 'Invalid measured phase cost')
        ratios[key] = max(1, allied_fixture[key]/preservation_fixture[key])
        floors[key] = 2*completed[key]*(21888/fights)*ratios[key]
        # The current fixture/preparation audit estimate is a floor too; the old
        # native probe cannot mask a regression in the new controller/auditor.
        adjusted[key] = max(previous[key], estimate[key], floors[key])
    adjusted['totalBytes'] = max(previous['totalBytes'], estimate['totalBytes'], adjusted['nativeBytes']+adjusted['auditBytes'])
    result = resource.pilot02_forecast(previous, probe, historical_bytes, adjusted)
    result['auditSeconds'] = max(result['auditSeconds'], adjusted['auditSeconds'])
    result.update(totalSeconds=result['nativeSeconds']+result['auditSeconds'], totalBytes=result['nativeBytes']+result['auditBytes'],
        completedStudyFloors=floors, upwardOnlyFixtureRatios=ratios, formula=CONTRACT['forecastRule'])
    result['fits'] = all(result[k] < result[k.replace('Seconds','MaximumSeconds').replace('Bytes','MaximumBytes')]
                         for k in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes'))
    return result

def prepare(declaration, pin):
    require(os.name == 'nt' and sha(declaration) == pin, 'Use the externally pinned Windows declaration')
    d = read(declaration); validate_declaration(d)
    require(not PACKAGE.exists() and not OUTPUT.exists(), 'No retry, existing admission or scientific output')
    PACKAGE.mkdir(); started = time.monotonic()
    save(PACKAGE/'admission-charge.json', dict(version=VERSION, chargedSeconds=SECONDS, chargedBytes=BYTES,
        fights=0,newValues=0,scientificLaunches=0,scope='Full allowance charged on success or failure; qualification and admission only.'))
    watchdog = threading.Timer(SECONDS, lambda: os._exit(124)); watchdog.daemon = True; watchdog.start()
    process_owner = module('allied_action_admission_process', ROOT/'build/bounded_windows_process.py')
    def check():
        require(time.monotonic()-started < SECONDS-5 and not OUTPUT.exists(), 'Admission deadline or unexpected scientific output')
        require(owner.storage_bytes(PACKAGE) < BYTES-4*1048576, 'Admission storage exhausted')
    def command(name, cmd):
        receipt = process_owner.run(cmd,str(ROOT),str(PACKAGE/(name+'.log')),started+SECONDS-5,cleanup_seconds=1,check=check)
        save(PACKAGE/(name+'-process.json'),receipt); base.process_ok(receipt); check()
    evidence = {}
    try:
        with owner.writer_lease(OUTPUT.parent/'complete-family-allocation'), owner.writer_lease(OUTPUT):
            evidence = {p:Evidence(p,pin) for p,pin in PINS.items()}
            def retained(root, name, target):
                source = evidence[root].get(name); path = PACKAGE/target
                path.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(source,path); return read(path) if path.suffix == '.json' else path
            for index,root in enumerate(evidence):
                shutil.copyfile(root/'files.json',PACKAGE/(f'{index:02d}-'+root.name+'-source-files.json'))
            proof = retained(VERIFICATION,'completion.json','implementation-verification.json')
            require(proof['status'] == 'VerifiedAlliedActionComparisonImplementationNotAdmitted'
                    and proof['ownedComplete']['version'] == STUDY and proof['backendTestsPassed'] == d['backendTestCount']
                    and proof['ownedComplete']['harnessSha256'] == d['harnessSha256'], 'Unverified allied-action implementation')
            verified_sources = {}
            for name,pin in read(evidence[VERIFICATION].get('implementation.json')).items():
                if Path(name).suffix in ('.cs','.py'):
                    require(sha(ROOT/name) == pin, 'Implementation changed after verification')
                    verified_sources[name] = pin
            trx = evidence[VERIFICATION].get('backend.trx')
            base.validate_tests(trx,d['backendTestCount']); shutil.copyfile(trx,PACKAGE/'backend-tests.trx')
            verified_sources[trx.relative_to(ROOT).as_posix()] = sha(trx)
            save(PACKAGE/'verified-implementation-files.json',verified_sources)
            prior = retained(PRIOR,'completion.json','preceding-accounting.json')
            require(prior['cumulativeRecordedSeconds'] == d['precedingRecordedSeconds']
                    and prior['cumulativeRecordedBytes'] == d['precedingRecordedBytes']
                    and prior['cumulativeDeclaredMaximumSeconds'] == d['precedingDeclaredMaximumSeconds']
                    and prior['cumulativeDeclaredMaximumBytes'] == d['precedingDeclaredMaximumBytes'], 'Lost prior charges')
            for prefix,dest in [('runtime/','baseline-runtime'),('runtime/','runtime'),('content/','content')]:
                for name in evidence[CAPTURE].files:
                    if name.startswith(prefix): retained(CAPTURE,name,dest+'/'+name[len(prefix):])
            for name in base.HARNESS_FILES: shutil.copyfile(BUILD/name,PACKAGE/'runtime'/name)
            runtime = inventory(PACKAGE/'runtime')
            base.validate_runtime(runtime,evidence[CAPTURE].files,d['harnessFiles'],d['harnessSha256']); save(PACKAGE/'runtime.json',runtime)
            retained(CAPTURE,'settings.json','settings.json')
            captured = retained(CAPTURE,'candidate-qualification.json','captured-qualification.json')
            previous_request = retained(FLOOR,'request.json','preceding-request.json')
            preview = retained(PREVIEW,'request.json','preview-request.json'); batches = retained(PREVIEW,'batches.json','preview-batches.json')
            preserving = retained(PRESERVING_PREVIEW,'root-01/request.json','preserving-request.json')
            preserving_batches = retained(PRESERVING_PREVIEW,'root-01/batches.json','preserving-batches.json')
            allied_requests, allied_batches = [], []
            for number in range(1,13):
                stem = f'root-{number:02d}/'
                allied_requests.append(retained(ALLIED_PREVIEW,stem+'request.json','allied-preview/'+stem+'request.json'))
                allied_batches.append(retained(ALLIED_PREVIEW,stem+'batches.json','allied-preview/'+stem+'batches.json'))
            require(len({r['context']['rootSeed'] for r in allied_requests}) == 12, 'Repeated saved preview roots')
            original = allied_requests[0]['context']
            save(PACKAGE/'probe-scenarios.json',probes(batches,preserving_batches,allied_batches,original))
            copies = [(declaration,'declaration.json'),(PLAN,'prospective-plan.json'),(DESIGN,'frozen-design.json'),(Path(__file__),'prepare-allied-action.py'),
                (Path(__file__).with_name('prepare-affinity-creation-admission.py'),'prepare-affinity-creation-admission.py'),
                (Path(base.__file__),'prepare-proposal-affinity-admission.py'),(Path(resource.__file__),'prepare-proposal-affinity-resource-admission.py'),
                (Path(__file__).with_name('audit-proposal-affinity-study.py'),'auditor.py'),
                (ROOT/'build/run-proposal-affinity-study.py','run-proposal-affinity-study.py'),
                (ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),
                (base.HISTORY_HELPER,'history-helper.py'),(ROOT/d['tests']['path'],'admission-tests.log'),
                (Path(__file__).with_name('test-affinity-allied-action-admission.py'),'test-allied-action-admission.py')]
            for source,name in copies: shutil.copyfile(source,PACKAGE/name)
            helper = Path(__file__).with_name('affinity-allied-action-admission-context.ps1')
            shutil.copyfile(helper,PACKAGE/'context.ps1')
            host = Path(shutil.which('pwsh')).with_suffix('.dll')
            def context(mode):
                runtime_dir = 'baseline-runtime' if mode == 'baseline' else 'runtime'
                command(mode,['dotnet','exec','--runtimeconfig',str(PACKAGE/runtime_dir/'BalanceHarness.runtimeconfig.json'),
                    '--depsfile',str(host.with_suffix('.deps.json')),str(host),'-NoProfile','-File',str(PACKAGE/'context.ps1'),
                    '-Package',str(PACKAGE),'-RepositoryRoot',str(ROOT),'-Mode',mode])
            context('baseline'); context('candidate')
            candidate = read(PACKAGE/'candidate-qualification.json')
            qualified(read(PACKAGE/'baseline-qualification.json'),candidate,read(PACKAGE/'baseline-projections.json'),
                read(PACKAGE/'candidate-projections.json'),captured,original)
            context('retention')
            resource.validate_retention(read(PACKAGE/'retention-qualification.json'),runtime,inventory(PACKAGE/'retention-check/executable'))
            files,values = base.history(OUTPUT.parent,previous_request); check()
            save(PACKAGE/'history-files.json',files); save(PACKAGE/'history.json',values)
            derived = base.derive_context(original,values,candidate['executionHash']); derived['scope']['id'] = d['scopeId']
            save(PACKAGE/'context.json',derived); context('bind')
            base.validate_plan(read(PACKAGE/'prospective-plan.json'),read(PACKAGE/'plan.json'))
            require(read(PACKAGE/'plan.json')['version'] == STUDY and candidate['inventoryHash'] == read(PACKAGE/'plan.json')['inventoryHash'], 'Changed allied-action plan/inventory')
            def bound(name):
                path = PACKAGE/name; return dict(path=str(path),sha256=sha(path))
            q = dict(version=STUDY,resourceEnvelope=owner.RESOURCE_V2,
                **{n:bound(n+'.json') for n in ('plan','context','settings','history','runtime')},auditor=bound('auditor.py'),
                contentRoot=str(PACKAGE/'content'),registryRoot=str(OUTPUT.parent),outputRoot=str(OUTPUT),requiredHistory=files,
                **{n:previous_request[n] for n in ('pendingHistoryRecoveries','recoveryReceiptHashes')})
            save(PACKAGE/'request.json',q)
            command('native-check',['dotnet',str(PACKAGE/'runtime/BalanceHarness.dll'),'tower-proposal-study-check',str(PACKAGE/'request.json')])
            native = read(PACKAGE/'native-check.log')
            require(native['version'] == STUDY and native['status'] == 'InputsVerifiedNoReservation'
                    and native['fights'] == native['newValues'] == 0 and native['historicalValues'] == len(values), 'Native check did not agree')
            # One current qualification sample; never resample to improve the forecast.
            recognition_native = retained(base.RECOGNITION,'native-receipt.json','recognition-native.json')
            recognition_complete = retained(base.RECOGNITION,'completion.json','recognition-completion.json')
            real = retained(COMPLETED,'completion.json','completed-study-phases.json')
            real_native = retained(COMPLETED,'native-receipt.json','completed-study-native.json')
            allied = retained(ALLIED_FIXTURE,'completion.json','allied-fixture-phases.json')
            preservation = retained(PRESERVATION_FIXTURE,'completion.json','preservation-fixture-phases.json')
            previous_estimate = retained(FLOOR,'resource-forecast.json','preceding-forecast.json')
            probe = retained(resource.PROBE,'resource-assessment.json','frozen-audit-probe.json')
            probe_previous = retained(resource.PROBE,'previous-resource-forecast.json','probe-previous-forecast.json')
            estimate = base.forecast(recognition_native,recognition_complete,allied,candidate,(PACKAGE/'context.json').stat().st_size,
                owner.storage_bytes(PACKAGE/'runtime'),owner.storage_bytes(PACKAGE/'content'))
            save(PACKAGE/'current-formula-forecast.json',estimate)
            result_forecast = forecast(previous_estimate,probe,probe_previous['totalBytes']/probe['inventoryByteRatio'],
                estimate,real,real_native['fights'],allied,preservation)
            save(PACKAGE/'resource-forecast.json',result_forecast); base.admit_forecast(result_forecast)
            fixture_pin = proof['ownedComplete']['closeoutSha256']
            command('independent-allied-fixture',[os.sys.executable,'-B','-X','utf8',str(PACKAGE/'auditor.py'),str(ALLIED_FIXTURE),
                '--pin',fixture_pin,'--output',str(PACKAGE/'independent-allied-fixture.json')])
            require(read(PACKAGE/'independent-allied-fixture.json')['status'] == 'Passed', 'Independent audit failed')
            require(base.history(OUTPUT.parent,q) == (files,values), 'History changed during admission')
            for e in evidence.values(): e.recheck()
            require(all(sha(ROOT/n) == pin for n,pin in verified_sources.items()), 'Verified implementation changed')
            require(sha(helper) == sha(PACKAGE/'context.ps1'), 'Qualification helper changed')
            require(inventory(PACKAGE/'runtime') == runtime and inventory(PACKAGE/'source') == read(PACKAGE/'compiled-source-files.json'), 'Copied runtime/source changed')
            require(inventory(PACKAGE/'baseline-runtime') == {n[8:]:p for n,p in evidence[CAPTURE].files.items() if n.startswith('runtime/')}, 'Copied baseline changed')
            require(inventory(PACKAGE/'content') == {n[8:]:p for n,p in evidence[CAPTURE].files.items() if n.startswith('content/')}, 'Copied content changed')
            for source,name in copies: require(sha(source) == sha(PACKAGE/name), 'Admission source changed')
            validate_declaration(d); check()
            save(PACKAGE/'consumed-evidence.json',{str(p):e.used for p,e in evidence.items()})
            receipt = dict(version=STUDY,status='ProposalStudyAdmittedNoReservation',resourceEnvelope=owner.RESOURCE_V2,
                requestSha256=sha(PACKAGE/'request.json'),originalPlanSha256=d['originalPlanFileSha256'],qualifiedPlanSha256=sha(PACKAGE/'plan.json'),
                executionHash=candidate['executionHash'],historicalValues=len(values),historyFiles=len(files),nativePreparations=2*d['preparationsPerRuntime'],
                runtimeFiles=len(runtime),compiledSourceDocuments=candidate['compiledSourceDocuments'],fights=0,newValues=0,scientificLaunches=0,
                chargedSeconds=SECONDS,chargedBytes=BYTES,precedingRecordedSeconds=d['precedingRecordedSeconds'],precedingRecordedBytes=d['precedingRecordedBytes'],
                cumulativeDeclaredMaximumSeconds=d['precedingDeclaredMaximumSeconds']+SECONDS,
                cumulativeDeclaredMaximumBytes=d['precedingDeclaredMaximumBytes']+BYTES,
                scientificMaximumSeconds=10800,scientificMaximumBytes=6442450944,
                cumulativeMaximumSeconds=d['precedingDeclaredMaximumSeconds']+SECONDS+10800,
                cumulativeMaximumBytes=d['precedingDeclaredMaximumBytes']+BYTES+6442450944)
            save(PACKAGE/'admission.json',receipt)
            save(PACKAGE/'completion.json',dict(status='AdmissionCompleteNoReservation',secondsBeforeSealing=time.monotonic()-started,
                bytesBeforeSealing=owner.storage_bytes(PACKAGE),chargedSeconds=SECONDS,chargedBytes=BYTES,fights=0,newValues=0,scientificLaunches=0))
            save(PACKAGE/'files.json',inventory(PACKAGE)); check()
            owner.validate_admission(PACKAGE/'request.json',PACKAGE/'runtime/BalanceHarness.dll',sha(PACKAGE/'files.json'))
            check()
            print(json.dumps(dict(status=receipt['status'],manifestSha256=sha(PACKAGE/'files.json'),requestSha256=sha(PACKAGE/'request.json'),
                seconds=time.monotonic()-started,retainedBytes=owner.storage_bytes(PACKAGE),resourceForecast=result_forecast,
                fights=0,newValues=0,scientificLaunches=0),indent=2))
    except BaseException as error:
        save(PACKAGE/'failure.json',dict(status='AdmissionFailedNoReservation',reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES,
            seconds=time.monotonic()-started,fights=0,newValues=0,scientificLaunches=0))
        if not (PACKAGE/'files.json').exists(): save(PACKAGE/'files.json',inventory(PACKAGE))
        raise
    finally: watchdog.cancel()

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--declaration',type=Path,required=True); parser.add_argument('--pin',required=True)
    args = parser.parse_args(); prepare(args.declaration,args.pin)
