"""One bounded, separately charged admission. No combat, entropy, reservation or launch.

Uses captured gameplay binaries, qualifies the tested harness in separate old/new
processes, and binds an unchanged scientific design to the qualified runtime.
"""
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
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-proposal-affinity-comparison-v1'
SECONDS, BYTES = 900, 1073741824
CAPTURE = ROOT/'TestResults/adaptive-racing-pilot-02-admission-20260923'
CAPTURE_PIN = 'e931606aad5690e12774f5f33e2f50487d68ac802d6f21def93546973317d6a4'
PREVIEW = ROOT/'TestResults/damage-affinity-preview-20260923'
PREVIEW_PIN = '3595d792881f9f78c3f3a44b75bd604699e367dd93d53afd63127e37d5469b18'
RECOGNITION = ROOT/'TestResults/balance/tower-frozen-pool-recognition-repaired-20260923'
RECOGNITION_PIN = '5fd7e9eb38eb4319f41be7a0e874297a897c7a32a73d09030261579b2473b377'
PLAN = ROOT/'Balance Harness/Tower-Proposal-Affinity-Comparison-Plan.json'
PLAN_PIN = 'f7840ded410c34a3283fd3424ec18af98cfc2057cee76181b02dfddf51666188'
HARNESS = ROOT/'TestResults/proposal-study-build-final-20260923/bin/BalanceHarness/release'
HARNESS_PIN = '015b8e06535543414d497ccdfb35f647b534f24fe655cc4732a845e082aa4a9b'
HISTORY_HELPER = ROOT/'TestResults/three-reference-admission-closeout-20260922/comparison-preparation.py'
HISTORY_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-complete-final-20260923'
FIXTURE_CLOSEOUT = '641e732b63f871e44fd74c0bb46602cd0a6e0b2fa810c31ff00d35a248e06e7e'
PRECEDING = ROOT/'TestResults/proposal-affinity-admission-20260923'
PRECEDING_FAILURE_PIN = '5924ebc3e5ced8162d1d1cd4b764d1eef04113b5c827d8325eda89c04d897d3a'
PRECEDING_CHARGE_PIN = '8a369b3bf1b40c6df3156c1707a2de96933b647906a0325d1cac42c24a1ecef7'
RESOURCE_ATTEMPT = ROOT/'TestResults/proposal-affinity-admission-repaired-20260923'
HARNESS_FILES = ('BalanceHarness.dll','BalanceHarness.pdb','BalanceHarness.deps.json','BalanceHarness.runtimeconfig.json')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


launcher = module('proposal_admission_launcher', ROOT/'build/run-proposal-affinity-study.py')
require, sha, save = launcher.require, launcher.digest, launcher.write


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def inventory(root):
    require(root.is_dir() and not root.is_symlink() and not root.is_junction(), 'Linked or missing root')
    return {p.relative_to(root).as_posix():sha(p) for p in launcher.inventory(root)}


def authenticate(root, pin):
    require(sha(root/'files.json') == pin, 'Changed external manifest pin')
    files = read(root/'files.json')
    require(inventory(root) == dict(files, **{'files.json':pin}), 'Changed captured package membership or bytes')
    return files


def consumed(root, pin, names, package, prefix):
    """Authenticate consumed files of an already closed scientific archive."""
    require(sha(root/'files.json') == pin, 'Changed scientific manifest')
    files = read(root/'files.json')
    shutil.copyfile(root/'files.json', package/(prefix+'files.json'))
    for name in names:
        require(sha(root/name) == files[name], 'Changed scientific source: '+name)
        shutil.copyfile(root/name, package/(prefix+name))


def history(registry, previous):
    require(sha(HISTORY_HELPER) == HISTORY_PIN, 'Changed independent history reader')
    return module('proposal_admission_history', HISTORY_HELPER).history(registry, previous)


def validate_runtime(actual, captured, tested, harness_pin=HARNESS_PIN):
    expected = {n[8:]:p for n,p in captured.items() if n.startswith('runtime/')}
    expected.update(tested)
    require(set(tested) == set(HARNESS_FILES) and actual == expected,
            'Only the four tested harness files may replace captured dependencies')
    require(tested['BalanceHarness.dll'] == harness_pin, 'Unverified harness')


def legacy_values(context):
    return {v for s in context['scope']['stages']['schedules'].values()
            for k in ('discovery','selection','confirmation','diagnostics','feedback') for v in (s.get(k) or [])}


def derive_context(original, values, execution_hash):
    require(values == sorted(set(values)) and 0 < len(values) <= 983616, 'Invalid history set')
    legacy = legacy_values(original)
    declared = legacy | set(original['scope']['generation']['seeds']) | {original['rootSeed']}
    declared.update(v for r in original['scope']['references'] for v in r['scenario']['seeds'])
    require(declared <= set(values), 'History omits captured declarations')
    result = copy.deepcopy(original)
    result['scope'].update(id='proposal-affinity-pilot-01', executionHash=execution_hash,
                           excludedCombatSeeds=sorted(set(values)-legacy))
    return result


def probe_scenarios(export, context, values):
    # Exported recipes are deliberately seedless. Materialize every recipe with
    # the same first/last declared legacy values; never draw or screen new roots.
    seeds = sorted(legacy_values(context))
    require(len(seeds) >= 2 and set(seeds) <= set(values), 'Missing historical probe seeds')
    rows = []
    for arm in export['arms']:
        for team in arm['teams']:
            scenario = copy.deepcopy(team['scenario']); scenario['seeds'] = [seeds[0],seeds[-1]]
            rows.append(dict(scenario=scenario,seeds=scenario['seeds']))
    require(len(rows) == 36, 'Incomplete retained preview recipe coverage')
    return rows


def validate_plan(original, derived):
    expected = dict(original, scopeHash=derived['scopeHash'])
    require(derived == expected and derived['scopeHash'] != original['scopeHash'],
            'Runtime qualification may change only the plan scope binding')


def process_ok(value):
    require(value['mechanism'] == 'suspended-owned-job-v1' and value['exitCode'] == 0
            and not value['timedOut'] and value['activeProcesses'] == 0 and value['totalProcesses'] >= 1,
            'Admission process failed; retained logs explain the failure')


def validate_qualification(baseline, candidate, before, after, captured, context, harness_pin=HARNESS_PIN):
    old, new = baseline['execution'], candidate['execution']
    require(old == captured['execution'] and baseline['executionHash'] == context['scope']['executionHash']
            == captured['executionHash'], 'Baseline is not the captured producing runtime')
    require({k:v for k,v in old.items() if k != 'assemblyHashes'} ==
            {k:v for k,v in new.items() if k != 'assemblyHashes'}, 'Changed platform/runtime')
    require({k:v for k,v in old['assemblyHashes'].items() if k != 'BalanceHarness'} ==
            {k:v for k,v in new['assemblyHashes'].items() if k != 'BalanceHarness'}
            and new['assemblyHashes']['BalanceHarness'] == harness_pin, 'Changed gameplay dependency')
    require(before == after and len(before) > 0 and len({r['ordinal'] for r in before}) == len(before),
            'Native materialized inputs or prepared participants changed')
    for q in (baseline, candidate):
        require(q['materializations'] == q['nativePreparations'] == len(before)
                and q['fights'] == q['newValues'] == 0 and q['settingsHash'] == context['scope']['settingsHash'],
                'Incomplete qualification or changed settings')
    require(candidate['replayedArms'] == 3 and candidate['compiledSourceDocuments'] > 0
            and candidate['jitResolvedMethods'], 'Missing replay, symbols or entry-point resolution')


def forecast(native, completion, fixture, candidate, context_bytes, runtime_bytes, content_bytes):
    require(native['status'] == 'Verified' and native['fights'] == 27648
            and completion['status'] == 'Complete' and fixture['status'] == 'Complete', 'Invalid sizing evidence')
    # Use the historical whole native phase, not combat-only throughput. Add the
    # owned literal-study phase and full native preparation measured on this runtime.
    scale = 21888/27648
    native_seconds = 2 * (native['measuredSeconds']*scale + fixture['nativeSeconds'])
    def sampled_cost(name):
        samples = candidate[name]
        require(len(samples) >= 2 and all(math.isfinite(x) and x >= 0 for x in samples), 'Invalid preparation timing')
        steady = sorted(samples[1:])
        # Startup occurs once; predeclare a doubled empirical 95th percentile
        # for repeated work, rather than charging first-call JIT on every fight.
        return samples[0] + 2*21888*steady[math.ceil(.95*len(steady))-1]
    native_seconds += sampled_cost('materializationSeconds') + sampled_cost('preparationSeconds')
    audit_seconds = 2 * (completion['auditSeconds']*scale + fixture['auditSeconds'])
    audit_seconds += sampled_cost('materializationSeconds')
    # Each arm is independently capped at 64 MiB of racing evidence. Account
    # separately for duplicated context/pair freezes and all copied content.
    native_bytes = (24*64*1048576 + 30*context_bytes + 26*content_bytes + runtime_bytes
                    + 2*native['observedBytes']*scale)
    audit_bytes = 2*completion['auditBytes'] + 2*fixture['auditBytes'] + 16*1048576
    fits = (0 < native_seconds < 9600 and 0 < audit_seconds < 1200
            and 0 < native_bytes < launcher.NATIVE_BYTES and 0 < audit_bytes < launcher.AUDIT_BYTES)
    return dict(nativeSeconds=native_seconds, auditSeconds=audit_seconds, nativeBytes=native_bytes,
                auditBytes=audit_bytes, totalSeconds=native_seconds+audit_seconds, totalBytes=native_bytes+audit_bytes,
                fits=fits, guaranteedUpperBound=False, actualFights=0, newValues=0,
                nativeMaximumSeconds=9600,auditMaximumSeconds=1200,nativeMaximumBytes=launcher.NATIVE_BYTES,auditMaximumBytes=launcher.AUDIT_BYTES,
                formula='2*(historical whole phase * 21888/27648 + owned literal phase), plus first-call cost and 2*21888*empirical p95 of subsequent preparation; explicit archive duplication allowances',
                limitation='Historical timing and sampled preparation are estimates. No efficacy measurement or guaranteed upper bound; unchanged hard limits invalidate incomplete runs.')


def admit_forecast(estimate):
    require(estimate['fits'], 'Measured planning estimate does not fit the fixed scientific partitions')


def require_open_resource_gate(attempt):
    require(not attempt.exists(), 'Resource admission is closed after the retained attempt. Prepare a separately scoped native audit-cost probe before another admission; do not repeat or widen this attempt.')


def validate_tests(path, count):
    ns = {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    results = ET.parse(path).findall('.//t:UnitTestResult', ns)
    require(len(results) == count and all(r.attrib['outcome'] == 'Passed' for r in results), 'Missing backend verification')


def verify_contents(package):
    require(not (package/'failure.json').exists(), 'Failed admission cannot be admitted')
    q = read(package/'request.json'); context = read(package/'context.json'); preview = read(package/'preview-request.json')['context']
    require(sha(package/'prospective-plan.json') == PLAN_PIN and sha(package/'capture-files.json') == CAPTURE_PIN
            and sha(package/'preview-files.json') == PREVIEW_PIN and sha(package/'recognition-files.json') == RECOGNITION_PIN,
            'Changed external source pins')
    capture = read(package/'capture-files.json')
    for name in ('context.json',):
        require(sha(package/('capture-'+name)) == capture[name], 'Changed captured identity')
    for name in ('request.json','batches.json'):
        require(sha(package/('preview-'+name)) == read(package/'preview-files.json')[name], 'Changed preview')
    for name in ('request.json','native-receipt.json','completion.json'):
        require(sha(package/('recognition-'+name)) == read(package/'recognition-files.json')[name], 'Changed recognition evidence')
    require(sha(package/'fixture-closeout.json') == FIXTURE_CLOSEOUT
            and sha(package/'fixture-files.json') == read(package/'fixture-closeout.json')['filesHash']
            and sha(package/'fixture-completion.json') == read(package/'fixture-files.json')['completion.json']
            and read(package/'fixture-files.json')['executable/BalanceHarness.dll'] == HARNESS_PIN, 'Changed owned sizing evidence')
    validate_runtime(inventory(package/'runtime'), capture, read(package/'tested-harness.json'))
    require(inventory(package/'baseline-runtime') == {n[8:]:p for n,p in capture.items() if n.startswith('runtime/')}, 'Changed baseline dependencies')
    require(inventory(package/'content') == {n[8:]:p for n,p in capture.items() if n.startswith('content/')}, 'Changed captured content')
    candidate = read(package/'candidate-qualification.json')
    validate_qualification(read(package/'baseline-qualification.json'), candidate, read(package/'baseline-projections.json'),
                           read(package/'candidate-projections.json'), read(package/'capture-context.json'), preview)
    require(read(package/'probe-scenarios.json') == probe_scenarios(read(package/'preview-batches.json'),preview,read(package/'history.json'))
            and candidate['nativePreparations'] == 72, 'Incomplete historical recipe coverage')
    require(context == derive_context(preview, read(package/'history.json'), candidate['executionHash']), 'Changed admission context')
    validate_plan(read(package/'prospective-plan.json'), read(package/'plan.json'))
    require(read(package/'settings.json') == read(package/'capture-context.json')['settings'], 'Changed captured settings')
    require(candidate['inventoryHash'] == read(package/'plan.json')['inventoryHash'], 'Changed full native inventory')
    require(inventory(package/'runtime') == read(package/'runtime.json'), 'Changed runtime inventory')
    sources = read(package/'compiled-source-files.json')
    require(len(sources) == candidate['compiledSourceDocuments'] and inventory(package/'source') == sources, 'Changed producing sources')
    validate_tests(package/'backend-tests.trx', 184); validate_tests(package/'focused-tests.trx', 20)
    for name in ('baseline','candidate','bind','native-check','independent-fixture'):
        process_ok(read(package/(name+'-process.json')))
    native = read(package/'native-check.json')
    require(native['status'] == 'InputsVerifiedNoReservation' and native['fights'] == native['newValues'] == 0
            and native['historicalValues'] == len(read(package/'history.json')), 'Native admission check failed')
    require(read(package/'independent-fixture.json')['status'] == 'Passed', 'Independent fixture audit failed')
    for key in ('plan','context','settings','history','runtime','auditor'):
        require(Path(q[key]['path']) == package/(key+'.json' if key != 'auditor' else 'auditor.py')
                and sha(Path(q[key]['path'])) == q[key]['sha256'], 'Unbound request input')
    require(q['requiredHistory'] == read(package/'history-files.json') and q['version'] == VERSION
            and q['contentRoot'] == str(package/'content'), 'Changed request')
    previous = read(package/'recognition-request.json')
    require(all(q[k] == previous[k] for k in ('pendingHistoryRecoveries','recoveryReceiptHashes')), 'Changed historical recovery')
    charge = read(package/'admission-charge.json')
    require(charge['chargedSeconds'] == SECONDS and charge['chargedBytes'] == BYTES, 'Lost admission charge')
    prior = read(package/'preceding-admission.json')
    require(prior['package'] == str(PRECEDING) and prior['chargedSeconds'] == SECONDS and prior['chargedBytes'] == BYTES
            and prior['failureSha256'] == sha(package/'preceding-failure.json') == sha(PRECEDING/'failure.json') == PRECEDING_FAILURE_PIN
            and prior['chargeSha256'] == sha(package/'preceding-charge.json') == sha(PRECEDING/'admission-charge.json') == PRECEDING_CHARGE_PIN
            and read(package/'preceding-failure.json')['status'] == 'AdmissionFailedNoReservation', 'Lost preceding failed admission charge')
    estimate = forecast(read(package/'recognition-native-receipt.json'), read(package/'recognition-completion.json'),
                        read(package/'fixture-completion.json'), candidate, (package/'context.json').stat().st_size,
                        launcher.storage_bytes(package/'runtime'), launcher.storage_bytes(package/'content'))
    require(read(package/'resource-forecast.json') == estimate, 'Changed resource forecast')
    admit_forecast(estimate)
    return dict(version=VERSION, status='ProposalStudyAdmittedNoReservation', requestSha256=sha(package/'request.json'),
                originalPlanSha256=PLAN_PIN, qualifiedPlanSha256=sha(package/'plan.json'), executionHash=candidate['executionHash'],
                historicalValues=len(read(package/'history.json')), historyFiles=len(q['requiredHistory']),
                nativePreparations=2*candidate['nativePreparations'], fights=0, newValues=0, scientificLaunches=0,
                chargedSeconds=SECONDS, chargedBytes=BYTES, chargeReceiptSha256=sha(package/'admission-charge.json'),
                scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
                precedingChargedSeconds=SECONDS, precedingChargedBytes=BYTES,
                cumulativeMaximumSeconds=2*SECONDS+10800, cumulativeMaximumBytes=2*BYTES+6442450944)


def prepare(args):
    require_open_resource_gate(RESOURCE_ATTEMPT)
    package, output = args.package.resolve(), args.output.resolve()
    require(os.name == 'nt' and not package.exists() and not output.exists() and output.parent == ROOT/'TestResults/balance',
            'Use a new package and new scientific output beneath the complete registry')
    require(package.is_relative_to(ROOT/'TestResults') and not package.is_relative_to(output.parent), 'Admission must be outside the registry')
    require(args.preceding_failure.resolve() == PRECEDING and sha(PRECEDING/'failure.json') == PRECEDING_FAILURE_PIN
            and sha(PRECEDING/'admission-charge.json') == PRECEDING_CHARGE_PIN, 'Explicitly preserve the preceding failed admission')
    started = time.monotonic(); package.mkdir()
    save(package/'admission-charge.json', dict(version=VERSION, chargedSeconds=SECONDS, chargedBytes=BYTES,
         scope='Runtime qualification and admission only; full allowance charged including failure; no refund, transfer, retry or scientific launch.'))
    watchdog = threading.Timer(SECONDS, lambda:os._exit(124)); watchdog.daemon=True; watchdog.start()
    owner = module('proposal_admission_owner', ROOT/'build/bounded_windows_process.py')
    def check():
        require(time.monotonic()-started < SECONDS-5 and not output.exists(), 'Admission deadline or unexpected scientific output')
        require(launcher.storage_bytes(package) < BYTES-4*1048576, 'Admission storage allowance exhausted')
    def command(name, command):
        value = owner.run(command, str(ROOT), str(package/(name+'.log')), started+SECONDS-5, cleanup_seconds=1, check=check)
        save(package/(name+'-process.json'), value); process_ok(value); check()
    try:
        with launcher.writer_lease(output.parent/'complete-family-allocation'), launcher.writer_lease(output):
            save(package/'preceding-admission.json',dict(package=str(PRECEDING),failureSha256=sha(PRECEDING/'failure.json'),
                chargeSha256=sha(PRECEDING/'admission-charge.json'),chargedSeconds=SECONDS,chargedBytes=BYTES,
                repair='Materialize seedless preview recipes using two already reserved legacy schedule values. New separate admission attempt, no resumption.'))
            shutil.copyfile(PRECEDING/'failure.json',package/'preceding-failure.json')
            shutil.copyfile(PRECEDING/'admission-charge.json',package/'preceding-charge.json')
            captured = authenticate(CAPTURE, CAPTURE_PIN); authenticate(PREVIEW, PREVIEW_PIN); check()
            require(sha(PLAN) == PLAN_PIN and sha(HARNESS/'BalanceHarness.dll') == HARNESS_PIN, 'Changed plan or tested harness')
            consumed(RECOGNITION, RECOGNITION_PIN, ('request.json','native-receipt.json','completion.json'), package, 'recognition-')
            shutil.copyfile(CAPTURE/'files.json', package/'capture-files.json')
            shutil.copyfile(CAPTURE/'context.json', package/'capture-context.json')
            shutil.copyfile(PREVIEW/'files.json', package/'preview-files.json')
            for name in ('request.json','batches.json'): shutil.copyfile(PREVIEW/name, package/('preview-'+name))
            shutil.copytree(CAPTURE/'runtime', package/'baseline-runtime'); shutil.copytree(CAPTURE/'runtime', package/'runtime')
            shutil.copytree(CAPTURE/'content', package/'content')
            tested = {name:sha(HARNESS/name) for name in HARNESS_FILES}
            for name in HARNESS_FILES: shutil.copyfile(HARNESS/name, package/'runtime'/name)
            save(package/'tested-harness.json', tested); validate_runtime(inventory(package/'runtime'), captured, tested)
            save(package/'settings.json', read(CAPTURE/'context.json')['settings'])
            for source, name in ((PLAN,'prospective-plan.json'), (Path(__file__),'prepare.py'),
                (Path(__file__).with_name('proposal-affinity-admission-context.ps1'),'context.ps1'),
                (Path(__file__).with_name('audit-proposal-affinity-study.py'),'auditor.py'),
                (ROOT/'build/run-proposal-affinity-study.py','run-proposal-affinity-study.py'),
                (ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),
                (ROOT/'TestResults/proposal-study-tests-final-20260923.trx','backend-tests.trx'),
                (ROOT/'TestResults/proposal-study-postreview-20260923.trx','focused-tests.trx')):
                shutil.copyfile(source, package/name)
            validate_tests(package/'backend-tests.trx',184); validate_tests(package/'focused-tests.trx',20)
            original = read(package/'preview-request.json')['context']
            previous = read(package/'recognition-request.json')
            files, values = history(output.parent, previous); save(package/'history-files.json',files); save(package/'history.json',values)
            probes = probe_scenarios(read(package/'preview-batches.json'),original,values)
            save(package/'probe-scenarios.json', probes)
            host = Path(shutil.which('pwsh')).with_suffix('.dll')
            def context(mode):
                runtime = 'baseline-runtime' if mode == 'baseline' else 'runtime'
                command(mode, ['dotnet','exec','--runtimeconfig',str(package/runtime/'BalanceHarness.runtimeconfig.json'),
                    '--depsfile',str(host.with_suffix('.deps.json')),str(host),'-NoProfile','-File',str(package/'context.ps1'),
                    '-Package',str(package),'-RepositoryRoot',str(ROOT),'-Mode',mode])
            context('baseline'); context('candidate')
            candidate = read(package/'candidate-qualification.json')
            validate_qualification(read(package/'baseline-qualification.json'), candidate, read(package/'baseline-projections.json'),
                                   read(package/'candidate-projections.json'), read(package/'capture-context.json'), original)
            save(package/'context.json', derive_context(original, values, candidate['executionHash'])); context('bind')
            validate_plan(read(package/'prospective-plan.json'),read(package/'plan.json'))
            save(package/'runtime.json', inventory(package/'runtime'))
            def bound(name):
                path = package/name; return dict(path=str(path),sha256=sha(path))
            q = dict(version=VERSION, **{k:bound(k+'.json') for k in ('plan','context','settings','history','runtime')},
                     auditor=bound('auditor.py'), contentRoot=str(package/'content'), registryRoot=str(output.parent), outputRoot=str(output),
                     requiredHistory=files, pendingHistoryRecoveries=previous['pendingHistoryRecoveries'], recoveryReceiptHashes=previous['recoveryReceiptHashes'])
            save(package/'request.json',q)
            command('native-check',['dotnet',str(package/'runtime/BalanceHarness.dll'),'tower-proposal-study-check',str(package/'request.json')])
            save(package/'native-check.json',read(package/'native-check.log'))
            require(sha(FIXTURE/'result/closeout.json') == FIXTURE_CLOSEOUT, 'Changed owned fixture closeout')
            closeout = read(FIXTURE/'result/closeout.json')
            require(sha(FIXTURE/'result/files.json') == closeout['filesHash'], 'Changed owned fixture manifest')
            consumed(FIXTURE/'result', closeout['filesHash'], ('completion.json',), package, 'fixture-')
            shutil.copyfile(FIXTURE/'result/closeout.json',package/'fixture-closeout.json')
            command('independent-fixture',[str(Path(os.sys.executable)),'-B','-X','utf8',str(package/'auditor.py'),
                    str(FIXTURE/'result'),'--pin',FIXTURE_CLOSEOUT,'--output',str(package/'independent-fixture.json')])
            estimate = forecast(read(package/'recognition-native-receipt.json'),read(package/'recognition-completion.json'),
                read(package/'fixture-completion.json'),candidate,(package/'context.json').stat().st_size,
                launcher.storage_bytes(package/'runtime'),launcher.storage_bytes(package/'content'))
            # Retain the failed estimate as well as all raw measurements before
            # enforcing admission; a rejected attempt must remain diagnosable.
            save(package/'resource-forecast.json',estimate); admit_forecast(estimate)
            require(history(output.parent,q) == (files,values), 'Live history changed during admission'); check()
            result = verify_contents(package); save(package/'admission.json',result)
            save(package/'completion.json',dict(status='AdmissionCompleteNoReservation',measuredSecondsBeforeSealing=time.monotonic()-started,
                retainedBytesBeforeSealing=launcher.storage_bytes(package),chargedSeconds=SECONDS,chargedBytes=BYTES,fights=0,newValues=0,scientificLaunches=0))
            save(package/'files.json',inventory(package)); check()
            retained_launcher = module('proposal_retained_launcher',package/'run-proposal-affinity-study.py')
            retained_launcher.validate_admission(package/'request.json',package/'runtime/BalanceHarness.dll',sha(package/'files.json'))
            require(sha(PLAN) == PLAN_PIN,'Original plan changed'); check()
            print(json.dumps(dict(status=result['status'],manifestSha256=sha(package/'files.json'),requestSha256=sha(package/'request.json'),
                measuredSecondsAfterSealing=time.monotonic()-started,retainedBytes=launcher.storage_bytes(package),chargedSeconds=SECONDS,
                chargedBytes=BYTES,fights=0,newValues=0,scientificLaunches=0),indent=2))
    except BaseException as error:
        if not (package/'failure.json').exists():
            save(package/'failure.json',dict(status='AdmissionFailedNoReservation',reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES,
                seconds=time.monotonic()-started,fights=0,newValues=0,scientificLaunches=0))
        raise
    finally:
        watchdog.cancel()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package',required=True,type=Path); parser.add_argument('--output',required=True,type=Path)
    parser.add_argument('--preceding-failure',required=True,type=Path)
    prepare(parser.parse_args())
