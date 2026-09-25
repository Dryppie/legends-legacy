"""One pinned, bounded creation admission. Never allocates values or launches combat."""
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

resource = module('creation_resource_basis', Path(__file__).with_name('prepare-proposal-affinity-resource-admission.py'))
base, owner = resource.base, resource.owner
require, read, sha, save, inventory = base.require, base.read, base.sha, base.save, base.inventory
VERSION = 'tower-affinity-creation-admission-v1'
STUDY = 'tower-affinity-creation-comparison-v1'
PACKAGE = ROOT/'TestResults/affinity-creation-admission-20260923'
OUTPUT = ROOT/'TestResults/balance/tower-affinity-creation-pilot-01-20260923'
BUILD = ROOT/'TestResults/affinity-creation-study-build-20260923/bin/BalanceHarness/release'
PLAN = ROOT/'Balance Harness/Tower-Affinity-Creation-Comparison-Plan.json'
SECONDS, BYTES = 900, 1073741824
CAPTURE = ROOT/'TestResults/proposal-affinity-pilot-02-admission-repaired-20260923'
PREVIEW = ROOT/'TestResults/affinity-creation-preview-20260923'
VERIFICATION = ROOT/'TestResults/affinity-creation-study-verification-20260923'
PRIOR = ROOT/'TestResults/affinity-creation-native-qualification-repaired-20260923'
COMPLETED = ROOT/'TestResults/balance/tower-proposal-affinity-pilot-02-20260923'
CREATION_FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-creation-versioned-20260923/result'
PRESERVATION_FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-preservation-versioned-20260923/result'
PINS = {
    CAPTURE: 'e5172f3b6b3db3ddcbe2e4a68cdd96eeb04e6822a3e65d12078676b1da7979fa',
    PREVIEW: 'db58d54f7a1c595e6b39995508643f16207941be9b772719ac039a9e1775ad39',
    VERIFICATION: '77101ab1174dc5cb96b464ce03e5639865c49d2882cd37f598631e27d5804aba',
    PRIOR: '171d8615636b5a1b531e2f5d81f36f4e29ad61bc0764686a8250103622d405c1',
    COMPLETED: 'f0d4d22531f4c46426c28224102d193a91c0d20318377b71b95e2fe19f4acc33',
    CREATION_FIXTURE: '9334cb659e5615490c1e957199264e1fee081851e9415c3f71d5e4c1f7076c06',
    PRESERVATION_FIXTURE: 'd70d324e4ddd820c6732679b0a077c866fc2ed89519a92e923146aff2b05b36b',
    base.RECOGNITION: base.RECOGNITION_PIN,
    resource.PROBE: resource.PROBE_PIN,
}
CONTRACT = dict(version=VERSION, studyVersion=STUDY, scopeId='affinity-creation-pilot-01',
    package=str(PACKAGE), output=str(OUTPUT), harness=str(BUILD), resourceEnvelope=owner.RESOURCE_V2,
    chargedSeconds=SECONDS, chargedBytes=BYTES, scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
    precedingChargedSeconds=20715.17200000002, precedingChargedBytes=15939176965,
    originalPlanFileSha256='efb477737fcbc104294d7a96532ae86de4ace3ee6f23ef8d0e6e6be1d0ed6b31',
    harnessSha256='bf2e5c1f283ea566184099fe1686ed8e9de1774f6ea0afdd5dd8c79a65a77cf4',
    backendTestCount=140, probeRecipeInstances=48, preparationsPerRuntime=96,
    retries=0, timingResamples=0, scientificLaunches=0, admissionOnly=True,
    sourceManifests={str(p): pin for p,pin in PINS.items()},
    forecastRule='Upward-only maximum of prior admission, frozen native audit probe, current preparation and creation-fixture forecast, and twice the completed preservation study scaled to 21888 fights with upward-only creation/preservation fixture phase ratios. Apply all storage floors before scaling the frozen audit probe. Margin 2 and publication reserve 120 remain. No timing retry.')
IMPLEMENTATION = (
    'Balance Harness/analysis/prepare-affinity-creation-admission.py',
    'Balance Harness/analysis/test-affinity-creation-admission.py',
    'Balance Harness/analysis/prepare-proposal-affinity-admission.py',
    'Balance Harness/analysis/prepare-proposal-affinity-resource-admission.py',
    'Balance Harness/analysis/proposal-affinity-admission-context.ps1',
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
    def get(self, name):
        path = self.root/name
        require(path.resolve().is_relative_to(self.root.resolve()) and name in self.files
                and sha(path) == self.files[name], 'Changed consumed source: '+name)
        self.used[name] = self.files[name]; return path
    def recheck(self):
        require(all(sha(self.root/n) == pin for n,pin in self.used.items()), 'Consumed evidence changed during admission')

def validate_declaration(d):
    require(set(d) == set(CONTRACT)|{'harnessFiles','implementationFiles','tests'}, 'Unknown or missing declaration field')
    require(all(d[k] == v for k,v in CONTRACT.items()), 'Changed creation admission contract')
    require(set(d['harnessFiles']) == set(base.HARNESS_FILES)
            and all(sha(BUILD/n) == pin for n,pin in d['harnessFiles'].items())
            and d['harnessFiles']['BalanceHarness.dll'] == CONTRACT['harnessSha256'], 'Changed tested build')
    require(set(d['implementationFiles']) == set(IMPLEMENTATION)
            and all(sha(ROOT/n) == pin for n,pin in d['implementationFiles'].items()), 'Changed admission implementation')
    tests = d['tests']; path = Path(tests['path'])
    require(path == ROOT/'TestResults/affinity-creation-admission-tests-20260923.log'
            and sha(path) == tests['sha256'] and tests['passed'] >= 10 and tests['failed'] == 0
            and f"Ran {tests['passed']} tests in " in path.read_text(encoding='utf-8-sig')
            and '\nOK' in path.read_text(encoding='utf-8-sig'), 'Missing admission test evidence')
    require(sha(PLAN) == CONTRACT['originalPlanFileSha256'], 'Changed original creation plan')

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
            and any('TowerAffinityCreation.Create' in m for m in candidate['jitResolvedMethods']), 'Missing v3 replay or producing sources')

def forecast(previous, probe, historical_bytes, estimate, completed, fights, creation_fixture, preservation_fixture):
    require(fights == 17536 and completed['status'] == 'Complete'
            and all(x['status'] == 'Complete' for x in (creation_fixture,preservation_fixture)), 'Changed sizing evidence')
    floors, ratios, adjusted = {}, {}, dict(estimate)
    for key in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes'):
        values = (completed[key], creation_fixture[key], preservation_fixture[key], estimate[key])
        require(all(type(v) in (int,float) and math.isfinite(v) and v > 0 for v in values), 'Invalid measured phase cost')
        ratios[key] = max(1, creation_fixture[key]/preservation_fixture[key])
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
    process_owner = module('creation_admission_process', ROOT/'build/bounded_windows_process.py')
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
            require(proof['status'] == 'ImplementationAndOwnedFixturesVerifiedNotAdmitted' and proof['harnessSha256'] == d['harnessSha256'], 'Unverified implementation')
            for name,pin in proof['sources'].items():
                if Path(name).suffix in ('.cs','.py'): require(sha(ROOT/name) == pin, 'Implementation changed after verification')
            trx = ROOT/'TestResults/affinity-creation-study-tests-20260923.trx'
            require(sha(trx) == proof['backendTrxSha256'], 'Changed tested build evidence'); base.validate_tests(trx,140)
            shutil.copyfile(trx,PACKAGE/'backend-tests.trx')
            prior = retained(PRIOR,'qualification.json','preceding-qualification.json')
            require(prior['status'] == 'RuntimeCompatibleNotAdmitted'
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
            copies = [(declaration,'declaration.json'),(PLAN,'prospective-plan.json'),(Path(__file__),'prepare-creation.py'),
                (Path(base.__file__),'prepare-proposal-affinity-admission.py'),(Path(resource.__file__),'prepare-proposal-affinity-resource-admission.py'),
                (Path(__file__).with_name('audit-proposal-affinity-study.py'),'auditor.py'),
                (ROOT/'build/run-proposal-affinity-study.py','run-proposal-affinity-study.py'),
                (ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),
                (base.HISTORY_HELPER,'history-helper.py'),(Path(d['tests']['path']),'admission-tests.log'),
                (Path(__file__).with_name('test-affinity-creation-admission.py'),'test-creation-admission.py')]
            for source,name in copies: shutil.copyfile(source,PACKAGE/name)
            helper = Path(__file__).with_name('proposal-affinity-admission-context.ps1')
            helper_text = helper.read_text(encoding='utf-8-sig')
            require(helper_text.count("'TowerProposalStudy','TowerProposalPolicies'") == 1, 'Changed qualification helper')
            script = helper_text.replace("'TowerProposalStudy','TowerProposalPolicies'",
                "'TowerAffinityCreation','TowerAdaptiveRacingGenerator','TowerProposalStudy','TowerProposalPolicies'")
            (PACKAGE/'context.ps1').write_text(script,encoding='utf-8')
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
            require(read(PACKAGE/'plan.json')['version'] == STUDY and candidate['inventoryHash'] == read(PACKAGE/'plan.json')['inventoryHash'], 'Changed creation plan/inventory')
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
            creation = retained(CREATION_FIXTURE,'completion.json','creation-fixture-phases.json')
            preservation = retained(PRESERVATION_FIXTURE,'completion.json','preservation-fixture-phases.json')
            previous_estimate = retained(CAPTURE,'resource-forecast.json','preceding-forecast.json')
            probe = retained(resource.PROBE,'resource-assessment.json','frozen-audit-probe.json')
            probe_previous = retained(resource.PROBE,'previous-resource-forecast.json','probe-previous-forecast.json')
            estimate = base.forecast(recognition_native,recognition_complete,creation,candidate,(PACKAGE/'context.json').stat().st_size,
                owner.storage_bytes(PACKAGE/'runtime'),owner.storage_bytes(PACKAGE/'content'))
            save(PACKAGE/'current-formula-forecast.json',estimate)
            result_forecast = forecast(previous_estimate,probe,probe_previous['totalBytes']/probe['inventoryByteRatio'],
                estimate,real,real_native['fights'],creation,preservation)
            save(PACKAGE/'resource-forecast.json',result_forecast); base.admit_forecast(result_forecast)
            fixture_pin = proof['ownedFixtures'][str(CREATION_FIXTURE.parent.relative_to(ROOT)).replace('\\','/')]['receipt']['closeoutSha256']
            command('independent-creation-fixture',[os.sys.executable,'-B','-X','utf8',str(PACKAGE/'auditor.py'),str(CREATION_FIXTURE),
                '--pin',fixture_pin,'--output',str(PACKAGE/'independent-creation-fixture.json')])
            require(read(PACKAGE/'independent-creation-fixture.json')['status'] == 'Passed', 'Independent audit failed')
            require(base.history(OUTPUT.parent,q) == (files,values), 'History changed during admission')
            for e in evidence.values(): e.recheck()
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
