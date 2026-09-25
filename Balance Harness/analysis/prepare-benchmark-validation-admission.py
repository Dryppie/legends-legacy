"""One pinned, bounded benchmark validation admission. Never allocates values or launches combat."""
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

resource = module('validation_resource_basis', Path(__file__).with_name('prepare-proposal-affinity-resource-admission.py'))
base, owner = resource.base, resource.owner
require, read, sha, save, inventory = base.require, base.read, base.sha, base.save, base.inventory
VERSION = 'tower-benchmark-validation-admission-v1'
STUDY = 'tower-benchmark-validation-comparison-v1'
PACKAGE = ROOT/'TestResults/benchmark-validation-admission-20260924'
OUTPUT = ROOT/'TestResults/balance/tower-benchmark-validation-pilot-01-20260924'
BUILD = ROOT/'.artifacts/benchmark-validation-study-20260924/bin/BalanceHarness/release'
PLAN = ROOT/'Balance Harness/Tower-Benchmark-Validation-Comparison-Plan.json'
SECONDS, BYTES = 900, 1073741824
CAPTURE = ROOT/'TestResults/proposal-affinity-pilot-02-admission-repaired-20260923'
PREVIEW = ROOT/'TestResults/affinity-creation-preview-20260923'
VERIFICATION = ROOT/'TestResults/benchmark-validation-study-verification-20260924'
PRIOR = ROOT/'TestResults/benchmark-tie-stage-review-20260923'
FLOOR = ROOT/'TestResults/benchmark-tie-admission-20260923'
COMPLETED = ROOT/'TestResults/balance/tower-benchmark-tie-pilot-01-20260923'
VALIDATION_FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-benchmark-validation-20260924/result'
SELECTOR_FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-benchmark-tie-20260923/result'
PINS = {
    CAPTURE: 'e5172f3b6b3db3ddcbe2e4a68cdd96eeb04e6822a3e65d12078676b1da7979fa',
    PREVIEW: 'db58d54f7a1c595e6b39995508643f16207941be9b772719ac039a9e1775ad39',
    VERIFICATION: '4b370920f0d82d98ae2d86599d1fa61a74cbc4121d41b0ca14f8ac78ebb98aae',
    PRIOR: '13b860996ca4c31561aea0a3ac9f0252c6e0a98d904b3d85e48d214de58be023',
    FLOOR: '80c8b6023642b75c7802cf0cc8b8f56d2becb4e14d1e09f847a43d5d073c189c',
    COMPLETED: 'e7c1ddff081e3f895b6d96c0cf1067b10d5b08c1376d6fe2682619467858c643',
    VALIDATION_FIXTURE: '6912ed31819b3904058b8b4e7de3b75a5450fdb1f62119030529f417a2e1c962',
    SELECTOR_FIXTURE: 'e367bfbb85cd64cba547e33c39c609a87fe027d18e01acd48431ef186344101f',
    base.RECOGNITION: base.RECOGNITION_PIN,
    resource.PROBE: resource.PROBE_PIN,
}
CONTRACT = dict(version=VERSION, studyVersion=STUDY, scopeId='benchmark-validation-pilot-01',
    package=PACKAGE.relative_to(ROOT).as_posix(), output=OUTPUT.relative_to(ROOT).as_posix(), harness=BUILD.relative_to(ROOT).as_posix(), resourceEnvelope=owner.RESOURCE_V2,
    chargedSeconds=SECONDS, chargedBytes=BYTES, scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
    precedingChargedSeconds=27191.67100000009, precedingChargedBytes=22349413892,
    originalPlanFileSha256='f03380b48885af9a9051d3e712d378eb76affdab8d8ae2e6575fb115afb5c7ae',
    harnessSha256='24e20a6f46d17ed14f53f72d1ff9cccbc3db1782f38d8e12ca67b91ac1c76cb6',
    backendTestCount=132, probeRecipeInstances=48, preparationsPerRuntime=96,
    retries=0, timingResamples=0, scientificLaunches=0, admissionOnly=True,
    sourceManifests={p.relative_to(ROOT).as_posix(): pin for p,pin in PINS.items()},
    forecastRule='Upward-only maximum of prior admission, frozen native audit probe, current preparation and validation-fixture forecast, and twice the completed selector study scaled from 17792 to 21888 fights with upward-only validation/selector fixture phase ratios. Apply all storage floors before scaling the frozen audit probe. Margin 2 and publication reserve 120 remain. No timing retry.')
IMPLEMENTATION = (
    'Balance Harness/analysis/prepare-benchmark-validation-admission.py',
    'Balance Harness/analysis/test-benchmark-validation-admission.py',
    'Balance Harness/analysis/prepare-affinity-creation-admission.py',
    'Balance Harness/analysis/prepare-proposal-affinity-admission.py',
    'Balance Harness/analysis/prepare-proposal-affinity-resource-admission.py',
    'Balance Harness/analysis/benchmark-validation-admission-context.ps1',
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
    require(all(d[k] == v for k,v in CONTRACT.items()), 'Changed benchmark validation admission contract')
    require(set(d['harnessFiles']) == set(base.HARNESS_FILES)
            and all(sha(BUILD/n) == pin for n,pin in d['harnessFiles'].items())
            and d['harnessFiles']['BalanceHarness.dll'] == CONTRACT['harnessSha256'], 'Changed tested build')
    require(set(d['implementationFiles']) == set(IMPLEMENTATION)
            and all(sha(ROOT/n) == pin for n,pin in d['implementationFiles'].items()), 'Changed admission implementation')
    tests = d['tests']; path = ROOT/tests['path']
    require(path == ROOT/'TestResults/benchmark-validation-admission-tests-20260924.log'
            and sha(path) == tests['sha256'] and tests['passed'] >= 23 and tests['failed'] == 0
            and f"Ran {tests['passed']} tests in " in path.read_text(encoding='utf-8-sig')
            and '\nOK' in path.read_text(encoding='utf-8-sig'), 'Missing admission test evidence')
    require(sha(PLAN) == CONTRACT['originalPlanFileSha256'], 'Changed original validation plan')

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
                'TowerAffinityCreation.Create', 'TowerProposalComparison.CreateValidationPlan',
                'TowerProposalComparison.ValidateValidationTrajectories', 'TowerBatchRacing.Select', 'TowerBenchmarkValidation.Gate', 'TowerBenchmarkValidation.Decide')), 'Missing validation entry points or producing sources')

def forecast(previous, probe, historical_bytes, estimate, completed, fights, validation_fixture, selector_fixture):
    require(fights == 17792 and completed['status'] == 'Complete'
            and all(x['status'] == 'Complete' for x in (validation_fixture,selector_fixture)), 'Changed sizing evidence')
    floors, ratios, adjusted = {}, {}, dict(estimate)
    for key in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes'):
        values = (completed[key], validation_fixture[key], selector_fixture[key], estimate[key])
        require(all(type(v) in (int,float) and math.isfinite(v) and v > 0 for v in values), 'Invalid measured phase cost')
        ratios[key] = max(1, validation_fixture[key]/selector_fixture[key])
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
    process_owner = module('validation_admission_process', ROOT/'build/bounded_windows_process.py')
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
            require(proof['status'] == 'BenchmarkValidationStudyImplementedAndSyntheticOwnedPathQualified'
                    and proof['ownedSuccess']['version'] == STUDY and proof['backendTestsPassed'] == 132
                    and proof['harnessSha256'] == d['harnessSha256'], 'Unverified validation implementation')
            verified_sources = {}
            for name,pin in evidence[VERIFICATION].files.items():
                if Path(name).suffix in ('.cs','.py'):
                    require(sha(ROOT/name) == pin, 'Implementation changed after verification')
                    verified_sources[name] = pin
            for name,count,target in [('benchmark-validation-study-regression-20260924.trx',132,'backend-tests.trx')]:
                relative = 'TestResults/'+name; trx = ROOT/relative
                require(sha(trx) == evidence[VERIFICATION].files[relative], 'Changed tested build evidence')
                base.validate_tests(trx,count); shutil.copyfile(trx,PACKAGE/target)
                verified_sources[relative] = sha(trx)
            save(PACKAGE/'verified-implementation-files.json',verified_sources)
            prior = retained(PRIOR,'review.json','preceding-review.json')
            require(prior['originalDecision'] == 'AbandonThisConfiguration' and prior['sourceFights'] == 17792
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
            previous_request = retained(FLOOR,'request.json','preceding-request.json')
            preview = retained(PREVIEW,'request.json','preview-request.json'); batches = retained(PREVIEW,'batches.json','preview-batches.json')
            original = preview['context']; save(PACKAGE/'probe-scenarios.json',probes(batches,original))
            copies = [(declaration,'declaration.json'),(PLAN,'prospective-plan.json'),(Path(__file__),'prepare-validation.py'),
                (Path(__file__).with_name('prepare-affinity-creation-admission.py'),'prepare-affinity-creation-admission.py'),
                (Path(base.__file__),'prepare-proposal-affinity-admission.py'),(Path(resource.__file__),'prepare-proposal-affinity-resource-admission.py'),
                (Path(__file__).with_name('audit-proposal-affinity-study.py'),'auditor.py'),
                (ROOT/'build/run-proposal-affinity-study.py','run-proposal-affinity-study.py'),
                (ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),
                (base.HISTORY_HELPER,'history-helper.py'),(ROOT/d['tests']['path'],'admission-tests.log'),
                (Path(__file__).with_name('test-benchmark-validation-admission.py'),'test-validation-admission.py')]
            for source,name in copies: shutil.copyfile(source,PACKAGE/name)
            helper = Path(__file__).with_name('benchmark-validation-admission-context.ps1')
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
            require(read(PACKAGE/'plan.json')['version'] == STUDY and candidate['inventoryHash'] == read(PACKAGE/'plan.json')['inventoryHash'], 'Changed validation plan/inventory')
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
            validation = retained(VALIDATION_FIXTURE,'completion.json','validation-fixture-phases.json')
            selector = retained(SELECTOR_FIXTURE,'completion.json','selector-fixture-phases.json')
            previous_estimate = retained(FLOOR,'resource-forecast.json','preceding-forecast.json')
            probe = retained(resource.PROBE,'resource-assessment.json','frozen-audit-probe.json')
            probe_previous = retained(resource.PROBE,'previous-resource-forecast.json','probe-previous-forecast.json')
            estimate = base.forecast(recognition_native,recognition_complete,validation,candidate,(PACKAGE/'context.json').stat().st_size,
                owner.storage_bytes(PACKAGE/'runtime'),owner.storage_bytes(PACKAGE/'content'))
            save(PACKAGE/'current-formula-forecast.json',estimate)
            result_forecast = forecast(previous_estimate,probe,probe_previous['totalBytes']/probe['inventoryByteRatio'],
                estimate,real,real_native['fights'],validation,selector)
            save(PACKAGE/'resource-forecast.json',result_forecast); base.admit_forecast(result_forecast)
            fixture_pin = proof['ownedSuccess']['closeoutSha256']
            command('independent-validation-fixture',[os.sys.executable,'-B','-X','utf8',str(PACKAGE/'auditor.py'),str(VALIDATION_FIXTURE),
                '--pin',fixture_pin,'--output',str(PACKAGE/'independent-validation-fixture.json')])
            require(read(PACKAGE/'independent-validation-fixture.json')['status'] == 'Passed', 'Independent audit failed')
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
