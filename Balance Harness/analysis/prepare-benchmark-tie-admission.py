"""One pinned, bounded selector admission. Never allocates values or launches combat."""
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

resource = module('selector_resource_basis', Path(__file__).with_name('prepare-proposal-affinity-resource-admission.py'))
base, owner = resource.base, resource.owner
require, read, sha, save, inventory = base.require, base.read, base.sha, base.save, base.inventory
VERSION = 'tower-benchmark-tie-admission-v1'
STUDY = 'tower-benchmark-tie-comparison-v1'
PACKAGE = ROOT/'TestResults/benchmark-tie-admission-20260923'
OUTPUT = ROOT/'TestResults/balance/tower-benchmark-tie-pilot-01-20260923'
BUILD = ROOT/'TestResults/benchmark-tie-study-build-20260923/bin/BalanceHarness/release'
PLAN = ROOT/'Balance Harness/Tower-Benchmark-Tie-Comparison-Plan.json'
SECONDS, BYTES = 900, 1073741824
CAPTURE = ROOT/'TestResults/proposal-affinity-pilot-02-admission-repaired-20260923'
PREVIEW = ROOT/'TestResults/affinity-creation-preview-20260923'
VERIFICATION = ROOT/'TestResults/benchmark-tie-study-verification-20260923'
PRIOR = ROOT/'TestResults/affinity-creation-stage-review-20260923'
FLOOR = ROOT/'TestResults/affinity-creation-admission-20260923'
COMPLETED = ROOT/'TestResults/balance/tower-affinity-creation-pilot-01-20260923'
SELECTOR_FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-benchmark-tie-20260923/result'
CREATION_FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-creation-versioned-20260923/result'
PINS = {
    CAPTURE: 'e5172f3b6b3db3ddcbe2e4a68cdd96eeb04e6822a3e65d12078676b1da7979fa',
    PREVIEW: 'db58d54f7a1c595e6b39995508643f16207941be9b772719ac039a9e1775ad39',
    VERIFICATION: '37e5c2eaf0861fe77cd0321fbdd4e51a80041cc6c073c1daec8614419b86a44b',
    PRIOR: 'e5ca00fc0f43c10693908281a77213074a77b384ba8c8df8b02689617a1fc21d',
    FLOOR: '554071201e9c01f99dbe084e5561df22fa7c97b785d823802d39a808898fa884',
    COMPLETED: '67ba1f437ab09a06d13eb76647a3d5e10e0aca7eba56f33e01775216a0630139',
    SELECTOR_FIXTURE: 'e367bfbb85cd64cba547e33c39c609a87fe027d18e01acd48431ef186344101f',
    CREATION_FIXTURE: '9334cb659e5615490c1e957199264e1fee081851e9415c3f71d5e4c1f7076c06',
    base.RECOGNITION: base.RECOGNITION_PIN,
    resource.PROBE: resource.PROBE_PIN,
}
CONTRACT = dict(version=VERSION, studyVersion=STUDY, scopeId='benchmark-tie-pilot-01',
    package=PACKAGE.relative_to(ROOT).as_posix(), output=OUTPUT.relative_to(ROOT).as_posix(), harness=BUILD.relative_to(ROOT).as_posix(), resourceEnvelope=owner.RESOURCE_V2,
    chargedSeconds=SECONDS, chargedBytes=BYTES, scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
    precedingChargedSeconds=24094.359000000055, precedingChargedBytes=19195820950,
    originalPlanFileSha256='d0b76b5c9f24ce917887ec7a1e02eb61a001975e864a9e5ef9dde8d2a155afe1',
    harnessSha256='f5583644f02239313621b20757a0debea5b8102712a8f8153ca457db8acf8ab8',
    backendTestCount=68, focusedTestCount=10, probeRecipeInstances=48, preparationsPerRuntime=96,
    retries=0, timingResamples=0, scientificLaunches=0, admissionOnly=True,
    sourceManifests={p.relative_to(ROOT).as_posix(): pin for p,pin in PINS.items()},
    forecastRule='Upward-only maximum of prior admission, frozen native audit probe, current preparation and selector-fixture forecast, and twice the completed creation study scaled from 19840 to 21888 fights with upward-only selector/creation fixture phase ratios. Apply all storage floors before scaling the frozen audit probe. Margin 2 and publication reserve 120 remain. No timing retry.')
IMPLEMENTATION = (
    'Balance Harness/analysis/prepare-benchmark-tie-admission.py',
    'Balance Harness/analysis/test-benchmark-tie-admission.py',
    'Balance Harness/analysis/prepare-affinity-creation-admission.py',
    'Balance Harness/analysis/prepare-proposal-affinity-admission.py',
    'Balance Harness/analysis/prepare-proposal-affinity-resource-admission.py',
    'Balance Harness/analysis/benchmark-tie-admission-context.ps1',
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
        self.index_root = ROOT if root == VERIFICATION else root
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
    require(all(d[k] == v for k,v in CONTRACT.items()), 'Changed selector admission contract')
    require(set(d['harnessFiles']) == set(base.HARNESS_FILES)
            and all(sha(BUILD/n) == pin for n,pin in d['harnessFiles'].items())
            and d['harnessFiles']['BalanceHarness.dll'] == CONTRACT['harnessSha256'], 'Changed tested build')
    require(set(d['implementationFiles']) == set(IMPLEMENTATION)
            and all(sha(ROOT/n) == pin for n,pin in d['implementationFiles'].items()), 'Changed admission implementation')
    tests = d['tests']; path = ROOT/tests['path']
    require(path == ROOT/'TestResults/benchmark-tie-admission-tests-20260923.log'
            and sha(path) == tests['sha256'] and tests['passed'] >= 10 and tests['failed'] == 0
            and f"Ran {tests['passed']} tests in " in path.read_text(encoding='utf-8-sig')
            and '\nOK' in path.read_text(encoding='utf-8-sig'), 'Missing admission test evidence')
    require(sha(PLAN) == CONTRACT['originalPlanFileSha256'], 'Changed original selector plan')

def probes(export, context):
    seeds = sorted(base.legacy_values(context)); require(len(seeds) >= 2, 'Missing declared probe values')
    require(len(export['arms']) == 4 and {arm['name'] for arm in export['arms']} == {
        'legacy-v1','benchmark-single-control-v2','benchmark-damage-preserving-single-v2','benchmark-affinity-creation-v3'}
        and all(len(arm['teams']) == 12 for arm in export['arms']), 'All four arms and reference instances must be qualified')
    rows = []
    for arm in export['arms']:
        for team in arm['teams']:
            scenario = copy.deepcopy(team['scenario']); scenario['seeds'] = [seeds[0], seeds[-1]]
            rows.append(dict(scenario=scenario, seeds=scenario['seeds']))
    return rows

def qualified(baseline, candidate, before, after, captured, context):
    old, new = baseline['execution'], candidate['execution']
    require(old == captured['execution'] and baseline['executionHash'] == captured['executionHash'] == context['scope']['executionHash'],
            'Changed captured baseline')
    require({k:v for k,v in old.items() if k != 'assemblyHashes'} == {k:v for k,v in new.items() if k != 'assemblyHashes'}
            and {k:v for k,v in old['assemblyHashes'].items() if k != 'BalanceHarness'} ==
                {k:v for k,v in new['assemblyHashes'].items() if k != 'BalanceHarness'}
            and new['assemblyHashes']['BalanceHarness'] == CONTRACT['harnessSha256'], 'Changed gameplay runtime or platform')
    require(before == after and len(before) == len({r['ordinal'] for r in before}) == 96, 'Changed native inputs/preparations')
    for item in (baseline, candidate):
        require(item['materializations'] == item['nativePreparations'] == 96 and item['fights'] == item['newValues'] == 0
                and item['settingsHash'] == context['scope']['settingsHash'], 'Incomplete native qualification')
    require(candidate['replayedArms'] == 4 and candidate['compiledSourceDocuments'] > 0
            and all(any(required in m for m in candidate['jitResolvedMethods']) for required in (
                'TowerAffinityCreation.Create', 'TowerProposalComparison.CreateSelectorPlan',
                'TowerProposalComparison.ValidateSelectorTrajectories', 'TowerBatchRacing.Select')), 'Missing selector entry points or producing sources')

def forecast(previous, probe, historical_bytes, estimate, completed, fights, selector_fixture, creation_fixture):
    require(fights == 19840 and completed['status'] == 'Complete'
            and all(x['status'] == 'Complete' for x in (selector_fixture,creation_fixture)), 'Changed sizing evidence')
    floors, ratios, adjusted = {}, {}, dict(estimate)
    for key in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes'):
        values = (completed[key], selector_fixture[key], creation_fixture[key], estimate[key])
        require(all(type(v) in (int,float) and math.isfinite(v) and v > 0 for v in values), 'Invalid measured phase cost')
        ratios[key] = max(1, selector_fixture[key]/creation_fixture[key])
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
    process_owner = module('selector_admission_process', ROOT/'build/bounded_windows_process.py')
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
            require(proof['status'] == 'VerifiedBenchmarkTieComparisonImplementation'
                    and proof['comparisonVersion'] == STUDY and proof['backendDistinctCases'] == 68
                    and proof['compiledSources']['BalanceHarness']['dllSha256'] == d['harnessSha256'], 'Unverified selector implementation')
            verified_sources = {}
            for name,pin in evidence[VERIFICATION].files.items():
                if Path(name).suffix in ('.cs','.py'):
                    require(sha(ROOT/name) == pin, 'Implementation changed after verification')
                    verified_sources[name] = pin
            for name,count,target in [('benchmark-tie-study-tests-20260923.trx',68,'backend-tests.trx'),
                                      ('benchmark-tie-study-nullability-tests-20260923.trx',10,'focused-tests.trx')]:
                relative = 'TestResults/'+name; trx = ROOT/relative
                require(sha(trx) == evidence[VERIFICATION].files[relative], 'Changed tested build evidence')
                base.validate_tests(trx,count); shutil.copyfile(trx,PACKAGE/target)
                verified_sources[relative] = sha(trx)
            save(PACKAGE/'verified-implementation-files.json',verified_sources)
            prior = retained(PRIOR,'review.json','preceding-review.json')
            require(prior['originalDecision'] == 'Inconclusive' and prior['sourceFights'] == 19840
                    and prior['totalRecordedChargedSeconds'] == d['precedingChargedSeconds']
                    and prior['totalRecordedChargedBytes'] == d['precedingChargedBytes'], 'Lost prior charges')
            for prefix,dest in [('runtime/','baseline-runtime'),('runtime/','runtime'),('content/','content')]:
                for name in evidence[CAPTURE].files:
                    if name.startswith(prefix): retained(CAPTURE,name,dest+'/'+name[len(prefix):])
            for name in base.HARNESS_FILES: shutil.copyfile(BUILD/name,PACKAGE/'runtime'/name)
            runtime = inventory(PACKAGE/'runtime')
            base.validate_runtime(runtime,evidence[CAPTURE].files,d['harnessFiles'],d['harnessSha256']); save(PACKAGE/'runtime.json',runtime)
            retained(CAPTURE,'settings.json','settings.json')
            captured = retained(CAPTURE,'candidate-qualification.json','captured-qualification.json')
            previous_request = retained(CAPTURE,'request.json','preceding-request.json')
            preview = retained(PREVIEW,'request.json','preview-request.json'); batches = retained(PREVIEW,'batches.json','preview-batches.json')
            original = preview['context']; save(PACKAGE/'probe-scenarios.json',probes(batches,original))
            copies = [(declaration,'declaration.json'),(PLAN,'prospective-plan.json'),(Path(__file__),'prepare-selector.py'),
                (Path(__file__).with_name('prepare-affinity-creation-admission.py'),'prepare-affinity-creation-admission.py'),
                (Path(base.__file__),'prepare-proposal-affinity-admission.py'),(Path(resource.__file__),'prepare-proposal-affinity-resource-admission.py'),
                (Path(__file__).with_name('audit-proposal-affinity-study.py'),'auditor.py'),
                (ROOT/'build/run-proposal-affinity-study.py','run-proposal-affinity-study.py'),
                (ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),
                (base.HISTORY_HELPER,'history-helper.py'),(ROOT/d['tests']['path'],'admission-tests.log'),
                (Path(__file__).with_name('test-benchmark-tie-admission.py'),'test-selector-admission.py')]
            for source,name in copies: shutil.copyfile(source,PACKAGE/name)
            helper = Path(__file__).with_name('benchmark-tie-admission-context.ps1')
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
            require(read(PACKAGE/'plan.json')['version'] == STUDY and candidate['inventoryHash'] == read(PACKAGE/'plan.json')['inventoryHash'], 'Changed selector plan/inventory')
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
            selector = retained(SELECTOR_FIXTURE,'completion.json','selector-fixture-phases.json')
            creation = retained(CREATION_FIXTURE,'completion.json','creation-fixture-phases.json')
            previous_estimate = retained(FLOOR,'resource-forecast.json','preceding-forecast.json')
            probe = retained(resource.PROBE,'resource-assessment.json','frozen-audit-probe.json')
            probe_previous = retained(resource.PROBE,'previous-resource-forecast.json','probe-previous-forecast.json')
            estimate = base.forecast(recognition_native,recognition_complete,selector,candidate,(PACKAGE/'context.json').stat().st_size,
                owner.storage_bytes(PACKAGE/'runtime'),owner.storage_bytes(PACKAGE/'content'))
            save(PACKAGE/'current-formula-forecast.json',estimate)
            result_forecast = forecast(previous_estimate,probe,probe_previous['totalBytes']/probe['inventoryByteRatio'],
                estimate,real,real_native['fights'],selector,creation)
            save(PACKAGE/'resource-forecast.json',result_forecast); base.admit_forecast(result_forecast)
            fixture_pin = proof['ownedCompletion']['closeoutSha256']
            command('independent-selector-fixture',[os.sys.executable,'-B','-X','utf8',str(PACKAGE/'auditor.py'),str(SELECTOR_FIXTURE),
                '--pin',fixture_pin,'--output',str(PACKAGE/'independent-selector-fixture.json')])
            require(read(PACKAGE/'independent-selector-fixture.json')['status'] == 'Passed', 'Independent audit failed')
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
                executionHash=candidate['executionHash'],historicalValues=len(values),historyFiles=len(files),nativePreparations=192,
                runtimeFiles=len(runtime),compiledSourceDocuments=candidate['compiledSourceDocuments'],fights=0,newValues=0,scientificLaunches=0,
                chargedSeconds=SECONDS,chargedBytes=BYTES,precedingChargedSeconds=d['precedingChargedSeconds'],precedingChargedBytes=d['precedingChargedBytes'],
                totalRecordedChargedSeconds=d['precedingChargedSeconds']+SECONDS,totalRecordedChargedBytes=d['precedingChargedBytes']+BYTES,
                cumulativeDeclaredMaximumSeconds=prior['cumulativeDeclaredMaximumSeconds']+SECONDS,
                cumulativeDeclaredMaximumBytes=prior['cumulativeDeclaredMaximumBytes']+BYTES,
                scientificMaximumSeconds=10800,scientificMaximumBytes=6442450944,
                cumulativeMaximumSeconds=d['precedingChargedSeconds']+SECONDS+10800,cumulativeMaximumBytes=d['precedingChargedBytes']+BYTES+6442450944)
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
