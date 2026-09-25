"""One bounded captured-runtime admission; never launch combat or allocate entropy.

The declaration pins tested code and source evidence before the full admission charge.
Failure preserves its package. Verification is read-only and cannot retry preparation.
"""
import argparse
import hashlib
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
VERSION = 'tower-affinity-creation-recognition-v1'
PACKAGE = ROOT/'TestResults/affinity-creation-recognition-admission-20260924'
OUTPUT = ROOT/'TestResults/balance/tower-affinity-creation-recognition-20260924'
BUILD = ROOT/'.artifacts/affinity-recognition-native-20260924/bin/BalanceHarness.ProcessFixture/release'
VERIFICATION = ROOT/'TestResults/affinity-creation-recognition-native-verification-20260924'
CAPTURE = ROOT/'TestResults/benchmark-validation-admission-20260924'
PILOT = ROOT/'TestResults/balance/tower-benchmark-validation-pilot-01-20260924'
RECOGNITION = ROOT/'TestResults/balance/tower-frozen-pool-recognition-repaired-20260923'
FIXTURE = ROOT/'TestResults/tower-affinity-recognition-owned-fixture-20260924'
PLAN = ROOT/'Balance Harness/Tower-Affinity-Creation-Recognition-Plan.json'
HISTORY_HELPER = ROOT/'TestResults/three-reference-admission-closeout-20260922/comparison-preparation.py'
HISTORY_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
SECONDS, BYTES = 600, 536870912
HARNESS = ('BalanceHarness.dll', 'BalanceHarness.pdb', 'BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json')
PINS = {
    'capture': (CAPTURE, 'e6fdb1369df6585fde905523e3190af221ef0dc9323e4b3ae3b1bf37659b5576'),
    'pilot': (PILOT, 'fd96c4d21a27a32ad17ab750d2972a6b7dab54d09950661042b9f3af56859504'),
    'recognition': (RECOGNITION, '5fd7e9eb38eb4319f41be7a0e874297a897c7a32a73d09030261579b2473b377'),
    'verification': (VERIFICATION, 'b88172e86d6565a3098bee992e8bcd9694dc88588951ac82feb22cc7290b6065'),
    'fixture': (FIXTURE/'result', '6c03bfe9eda04a2ce54cf6ee9712c1f107df1afd4068edb371bada5cbf3b150b'),
}
POPULATION = {
    'catalogue': ('affinity-creation-recognition-catalogue-20260924', 'ccd05f08fda2c86ab014c12ca056d061053683d7b47c69cafee6902a08831814'),
    'sampling': ('affinity-creation-recognition-sampling-20260924', '4062d335ff704b6d523dff8c2d950e1ba185a57246ec4cf6635db0decf936b63'),
    'plan': ('affinity-creation-recognition-plan-20260924', '4c7e048591ce426ea2301c2b1596ac8c61fcc2aced076b675be17027373cf7d8'),
}
CLOSEOUT_PINS = dict(pilot='15e192e436940e9bea9df5849b0305b02b71ffc33854cf025c5edb71272c46bb',
    recognition='5e0787ce6ed76435411d50532c56bd69c64797755d7ae612bb53b2b9a7d26e70')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def local(name, fallback):
    path = Path(__file__).with_name(name)
    return path if path.exists() else ROOT/fallback


audit = module('affinity_admission_audit', local('auditor.py', 'Balance Harness/analysis/audit-affinity-creation-recognition.py'))
launcher = module('affinity_admission_launcher', local('run-affinity-creation-recognition.py', 'build/run-affinity-creation-recognition.py'))
require, read, sha, save = audit.require, audit.read, audit.sha, launcher.write
IMPLEMENTATION = (
    'Balance Harness/analysis/prepare-affinity-creation-recognition.py',
    'Balance Harness/analysis/affinity-creation-recognition-context.ps1',
    'Balance Harness/analysis/test-affinity-creation-recognition-admission.py',
    'Balance Harness/analysis/audit-affinity-creation-recognition.py',
    'build/run-affinity-creation-recognition.py', 'build/bounded_windows_process.py',
)
CONTRACT = dict(version=VERSION, admissionVersion='tower-affinity-creation-recognition-admission-v1',
    package=PACKAGE.relative_to(ROOT).as_posix(), output=OUTPUT.relative_to(ROOT).as_posix(),
    chargedSeconds=SECONDS, chargedBytes=BYTES, scientificSeconds=7200, scientificBytes=3758096384,
    priorRecordedSeconds=30405.312000000034, priorRecordedBytes=25525204135,
    priorDeclaredMaximumSeconds=67680, priorDeclaredMaximumBytes=43486543872,
    planSha256=audit.PLAN, harnessSha256='bcfd818a72244dbe3526a075627d2ba4c91d66dce9484fcc64405acdcb771811',
    sourceManifests={k: v[1] for k, v in PINS.items()}, populationManifests={k: v[1] for k, v in POPULATION.items()},
    closeoutPins=CLOSEOUT_PINS,
    retries=0, timingResamples=0, scientificLaunches=0,
    forecastRule='Margin 2. Native time: maximum of completed recognition and v5 time per fight scaled to 27648, plus current context/check elapsed time. Audit time: same maximum using full closeout audit times, plus current context/check elapsed time. Native storage: 27648 times the maximum authenticated gzip report size across both studies, plus the larger of recognition non-report native bytes and current literal-fixture native bytes, plus 64 MiB. Audit storage: larger of historical/fixture audit bytes scaled for fight count plus 16 MiB. Apply margin 2 to every estimate; no downward correction or timing retry.')


def inventory(root):
    return {p.relative_to(root).as_posix(): sha(p) for p in launcher.inventory(root)}


def authenticate(root, pin):
    require(sha(root/'files.json') == pin, 'Changed external manifest')
    audit.authenticate(root)
    return read(root/'files.json')


class Evidence:
    """Authenticate consumed records; report payload scans are explicit and bounded."""
    def __init__(self, label, package):
        self.label = label
        self.root, self.pin = PINS[label]
        require(sha(self.root/'files.json') == self.pin, 'Changed '+label+' manifest')
        self.files = read(self.root/'files.json')
        self.index = ROOT if label == 'verification' else self.root
        self.dest = package/'evidence'/label
        self.dest.mkdir(parents=True)
        shutil.copyfile(self.root/'files.json', self.dest/'files.json')
        self.used = {}

    def get(self, name):
        path = (self.root/name).resolve()
        require(path.is_relative_to(self.root.resolve()), 'Escaping evidence path')
        key = path.relative_to(self.index).as_posix()
        require(key in self.files and sha(path) == self.files[key], 'Changed consumed '+self.label+': '+name)
        target = self.dest/name
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists():
            shutil.copyfile(path, target)
        require(sha(target) == self.files[key], 'Changed retained evidence')
        self.used[name] = self.files[key]
        return target

    def recheck(self):
        require(sha(self.root/'files.json') == self.pin and all(sha(self.root/n) == h for n, h in self.used.items()), 'Source changed during admission')


def report_sizes(evidence, count, check):
    names = sorted(n for n in evidence.files if '/battles/' in n)
    require(len(names) == count and all(n.endswith('.json.gz') for n in names), 'Incomplete gzip report family')
    sizes = {}
    for i, name in enumerate(names):
        path = evidence.root/name
        require(not path.is_symlink() and not path.is_junction(), 'Linked sizing report')
        raw = path.read_bytes()
        require(hashlib.sha256(raw).hexdigest() == evidence.files[name], 'Changed sizing report')
        sizes[name] = len(raw)
        if i % 256 == 0:
            check()
    return dict(reports=count, bytes=sum(sizes.values()), maximumBytes=max(sizes.values()), sizes=sizes)


def validate_declaration(value):
    require(set(value) == set(CONTRACT)|{'harnessFiles', 'implementationFiles', 'tests'}, 'Changed declaration fields')
    require(all(value[k] == v for k, v in CONTRACT.items()), 'Changed admission contract')
    require(set(value['harnessFiles']) == set(HARNESS)
        and all(sha(BUILD/n) == h for n, h in value['harnessFiles'].items())
        and value['harnessFiles']['BalanceHarness.dll'] == CONTRACT['harnessSha256'], 'Changed tested harness')
    require(set(value['implementationFiles']) == set(IMPLEMENTATION)
        and all(sha(ROOT/n) == h for n, h in value['implementationFiles'].items()), 'Changed admission implementation')
    tests = value['tests']
    text = (ROOT/tests['path']).read_text(encoding='utf-8-sig')
    require(sha(ROOT/tests['path']) == tests['sha256'] and tests['passed'] >= 15 and tests['failed'] == 0
        and f"Ran {tests['passed']} tests in " in text and '\nOK' in text, 'Missing passing admission tests')
    require(sha(PLAN) == audit.PLAN, 'Changed canonical plan')


def validate_tests(path):
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    tree = ET.parse(path)
    results = tree.findall('.//t:UnitTestResult', ns)
    require(len(results) == 122 and all(r.attrib['outcome'] == 'Passed' for r in results), 'Backend verification failed')
    names = [r.attrib['testName'] for r in results if 'BalanceHarnessAffinityCreationRecognitionTests.' in r.attrib['testName']]
    require(len(names) == 17 and all(any(part in n for n in names) for part in
        ('Full_27648_report_archive', 'Both_profiles_reject', 'Command_profiles_reject', 'Root_panels_and_recipes')), 'Missing new profile tests')


def validate_runtime(actual, captured, tested):
    expected = {n[8:]: h for n, h in captured.items() if n.startswith('runtime/')}
    require(set(tested) == set(HARNESS) and set(tested).issubset(expected), 'Missing harness substitution contract')
    expected.update(tested)
    require(actual == expected, 'Changed captured dependencies or tested harness')


def history(registry, previous, helper):
    require(sha(helper) == HISTORY_PIN, 'Changed history reader')
    return module('affinity_admission_history', helper).history(registry, previous)


def definition(plan, values, context):
    return dict(version=VERSION, teams=[dict(role=t['stratum'], partyId=t['partyId'], referenceIds=[], scenario=t['scenario'])
        for r in plan['roots'] for t in r['teams']], contentHashes=plan['capturedRuntime']['contentHashes'],
        settingsHash=plan['capturedRuntime']['settingsHash'], executionHash=context['executionHash'], excludedCombatSeeds=values)


def request(package, files, previous):
    charge = package/'admission-charge.json'
    return dict(version=VERSION, contentRoot=str(package/'content'), definitionPath=str(package/'definition.json'),
        definitionHash=sha(package/'definition.json'), registryRoot=str(OUTPUT.parent), outputRoot=str(OUTPUT), requiredHistory=files,
        maximumSeconds=7800, maximumBytes=4294967296, phases={}, priorSeconds=SECONDS, priorBytes=BYTES,
        priorCharges=[dict(scope='Affinity-creation recognition admission allowance', seconds=SECONDS, bytes=BYTES,
            receiptPath=str(charge), receiptHash=sha(charge))],
        pendingHistoryRecoveries=previous['pendingHistoryRecoveries'], recoveryReceiptHashes=previous['recoveryReceiptHashes'],
        recognition=dict(planPath=str(package/'plan.json'), auditorPath=str(package/'auditor.py'), auditorHash=sha(package/'auditor.py')))


def process_ok(value):
    require(value['mechanism'] == 'suspended-owned-job-v1' and value['exitCode'] == 0 and not value['timedOut']
        and value['activeProcesses'] == 0 and value['totalProcesses'] >= 1, 'Incomplete owned admission process')


def population(package):
    for label, (_, pin) in POPULATION.items():
        authenticate(package/'population'/label, pin)
    p = package/'population'
    builder = module('affinity_frozen_population', p/'plan/builder.py')
    require(sha(p/'plan/builder.py') == sha(p/'catalogue/builder.py'), 'Changed population builder')
    value = read(p/'catalogue/catalogue.json')
    catalogue_hash = sha(p/'catalogue/catalogue.json')
    selection = builder.verify_sampling(value, catalogue_hash, POPULATION['catalogue'][1], p/'sampling', POPULATION['sampling'][1])
    require(read(p/'sampling/intent.json')['catalogueSourceSha256'] == sha(p/'catalogue/builder.py'), 'Changed catalogue producer')
    require(builder.build_plan(value, selection, catalogue_hash, POPULATION['sampling'][1]) == read(package/'plan.json')
        and sha(p/'plan/plan.json') == sha(package/'plan.json') == audit.PLAN, 'Frozen plan does not reproduce')


def forecast(native, closeout, sizes, fixture, preparation_seconds):
    require(set(native) == set(closeout) == set(sizes) == {'pilot', 'recognition'}, 'Changed forecast sources')
    for label, count, version, status in [('pilot', 16768, 'tower-benchmark-validation-comparison-v1', 'MeasuredPendingAudits'),
        ('recognition', 27648, 'tower-frozen-pool-recognition-v1', 'Verified')]:
        n, c, s = native[label], closeout[label], sizes[label]
        require(n['version'] == c['version'] == version and n['status'] == status and n['fights'] == count
            and n['newAuditFights'] == 0 and c['filesHash'] == PINS[label][1], 'Invalid completed sizing source')
        require(s['reports'] == len(s['sizes']) == count and s['bytes'] == sum(s['sizes'].values())
            and s['maximumBytes'] == max(s['sizes'].values()) and all(type(v) is int and v > 0 for v in s['sizes'].values()), 'Invalid report sizes')
        require(all(type(v) in (int, float) and math.isfinite(v) and v > 0 for v in
            (n['measuredSeconds'], n['observedBytes'], c['auditSeconds'], c['auditBytes'], c['measuredSeconds'], c['retainedBytes']))
            and n['observedBytes'] >= s['bytes'] and c['retainedBytes'] >= n['observedBytes']
            and c['measuredSeconds'] >= n['measuredSeconds'], 'Invalid measured resource values')
    require(type(preparation_seconds) in (int, float) and math.isfinite(preparation_seconds) and preparation_seconds >= 0
        and all(type(fixture[k]) in (int, float) and math.isfinite(fixture[k]) and fixture[k] > 0 for k in ('nativeBytes', 'auditBytes')), 'Invalid current overhead')
    target, margin = 27648, 2
    native_seconds = margin*(max(native[k]['measuredSeconds']*target/native[k]['fights'] for k in native)+preparation_seconds)
    audit_seconds = margin*(max(closeout[k]['auditSeconds']*target/native[k]['fights'] for k in native)+preparation_seconds)
    overhead = max(native['recognition']['observedBytes']-sizes['recognition']['bytes'], fixture['nativeBytes'])
    native_bytes = margin*(target*max(s['maximumBytes'] for s in sizes.values())+overhead+64*1048576)
    audit_bytes = margin*(max([closeout[k]['auditBytes']*target/native[k]['fights'] for k in native]+[fixture['auditBytes']])+16*1048576)
    require(native_seconds < 6000 and audit_seconds < 1200 and native_bytes < 3*1073741824-4*1048576
        and audit_bytes < 512*1048576, 'Forecast does not fit separate fixed caps')
    return dict(rule=CONTRACT['forecastRule'], margin=margin, targetFights=target, nativeSeconds=native_seconds,
        nativeBytes=native_bytes, auditSeconds=audit_seconds, auditBytes=audit_bytes,
        currentPreparationSeconds=preparation_seconds, maximumReportBytes=max(s['maximumBytes'] for s in sizes.values()),
        nonReportNativeBytes=overhead, fits=True, guaranteedUpperBound=False,
        limitation='Historical planning estimate with a fixed margin; unobserved recipes and current machine load can exceed it. Operational caps remain binding.')


def prepare(declaration):
    d = read(declaration)
    validate_declaration(d)
    require(os.name == 'nt' and not PACKAGE.exists() and not OUTPUT.exists() and OUTPUT.parent.is_dir(), 'Require new Windows admission/output identities')
    started = time.monotonic()
    PACKAGE.mkdir()
    save(PACKAGE/'admission-charge.json', dict(version=VERSION, chargedSeconds=SECONDS, chargedBytes=BYTES,
        treatment='Full admission allowance charged at start, including failure. No refund, retry or transfer.'))
    watchdog = threading.Timer(SECONDS, lambda: os._exit(124))
    watchdog.daemon = True
    watchdog.start()
    def check():
        require(time.monotonic()-started < SECONDS-3 and not OUTPUT.exists(), 'Admission deadline or unexpected scientific output')
        require(launcher.storage_bytes(PACKAGE) < BYTES-1048576, 'Admission storage exceeded')
    owner = module('affinity_admission_owner', ROOT/'build/bounded_windows_process.py')
    def command(name, args):
        receipt = owner.run(args, str(ROOT), str(PACKAGE/(name+'.log')), started+SECONDS-5, cleanup_seconds=1, check=check)
        save(PACKAGE/(name+'-process.json'), receipt)
        process_ok(receipt)
        check()
    try:
        shutil.copyfile(declaration, PACKAGE/'declaration.json')
        with launcher.writer_lease(OUTPUT.parent/'complete-family-allocation'), launcher.writer_lease(OUTPUT):
            evidence = {k: Evidence(k, PACKAGE) for k in PINS}
            cap, prior, proof, fixture = (evidence[k] for k in ('capture', 'pilot', 'verification', 'fixture'))
            # Complete capture authentication, without re-running its preparation or combat.
            captured = authenticate(CAPTURE, PINS['capture'][1])
            for name in ('candidate-qualification.json', 'admission.json'):
                cap.get(name)
            for label in ('pilot', 'recognition'):
                e = evidence[label]
                for name in ('request.json', 'native-receipt.json', 'completion.json'):
                    e.get(name)
                # closeout is external to the scientific manifest; authenticate its known published pin.
                require(sha(e.root/'closeout.json') == CLOSEOUT_PINS[label], 'Changed external closeout')
                shutil.copyfile(e.root/'closeout.json', e.dest/'closeout.json')
            tests = proof.get('backend-tests.trx')
            validate_tests(tests)
            sources = read(proof.get('tested-source-files.json'))
            for name, pin in sources.items():
                require(sha(ROOT/name) == pin, 'Tested source changed')
            tested_runtime = read(proof.get('tested-runtime-files.json'))
            require(d['harnessFiles'] == {n: tested_runtime[(BUILD/n).relative_to(ROOT).as_posix()] for n in HARNESS}, 'Harness files differ from verified runtime')
            # The verification manifest also pins the owned receipt outside its own directory.
            key = (FIXTURE/'verification.json').relative_to(ROOT).as_posix()
            require(sha(FIXTURE/'verification.json') == proof.files[key], 'Changed owned fixture receipt')
            shutil.copyfile(FIXTURE/'verification.json', PACKAGE/'owned-fixture-verification.json')
            for name in ('completion.json', 'provisional-result.json'):
                fixture.get(name)
            require({p.relative_to(FIXTURE/'result').as_posix() for p in launcher.inventory(FIXTURE/'result')} ==
                set(fixture.files)|{'files.json', 'closeout.json'}, 'Changed owned fixture inventory')
            for name, pin in fixture.files.items():
                require(sha(fixture.root/name) == pin, 'Changed owned fixture member')
            require(audit.audit(FIXTURE/'result')['status'] == 'Passed', 'Literal fixture independent audit failed')
            shutil.copytree(CAPTURE/'runtime', PACKAGE/'runtime')
            shutil.copytree(CAPTURE/'content', PACKAGE/'content')
            for name in HARNESS:
                shutil.copyfile(BUILD/name, PACKAGE/'runtime'/name)
            save(PACKAGE/'tested-harness.json', d['harnessFiles'])
            validate_runtime(inventory(PACKAGE/'runtime'), captured, d['harnessFiles'])
            literal = PACKAGE/'literal-fixture'
            literal.mkdir()
            for source, target in [('study/study.json', 'stored-study.json'), ('provisional-result.json', 'stored-result.json'),
                ('confirmation-binding.json', 'stored-binding.json'), ('chunks.json', 'stored-chunks.json'), ('entropy.bin', 'literal-entropy.bin')]:
                shutil.copyfile(fixture.get(source), literal/target)
            for label, (folder, pin) in POPULATION.items():
                authenticate(ROOT/'TestResults'/folder, pin)
                shutil.copytree(ROOT/'TestResults'/folder, PACKAGE/'population'/label)
            for source, target in [(PLAN, 'plan.json'), (Path(__file__), 'prepare.py'),
                (ROOT/IMPLEMENTATION[1], 'context.ps1'), (ROOT/IMPLEMENTATION[3], 'auditor.py'),
                (ROOT/IMPLEMENTATION[4], 'run-affinity-creation-recognition.py'), (ROOT/IMPLEMENTATION[5], 'bounded_windows_process.py'),
                (HISTORY_HELPER, 'history-helper.py'), (ROOT/d['tests']['path'], 'admission-tests.log'),
                (ROOT/IMPLEMENTATION[2], 'test-admission.py')]:
                shutil.copyfile(source, PACKAGE/target)
            population(PACKAGE)
            sizes = {label: report_sizes(evidence[label], count, check) for label, count in [('pilot', 16768), ('recognition', 27648)]}
            save(PACKAGE/'report-sizes.json', sizes)
            host = Path(shutil.which('pwsh')).with_suffix('.dll')
            command('context', ['dotnet', 'exec', '--runtimeconfig', str(PACKAGE/'runtime/BalanceHarness.runtimeconfig.json'),
                '--depsfile', str(host.with_suffix('.deps.json')), str(host), '-NoProfile', '-File', str(PACKAGE/'context.ps1'),
                '-Package', str(PACKAGE), '-RepositoryRoot', str(ROOT)])
            previous = read(prior.dest/'request.json')
            files, values = history(OUTPUT.parent, previous, PACKAGE/'history-helper.py')
            context = read(PACKAGE/'context.json')
            save(PACKAGE/'history-files.json', files)
            save(PACKAGE/'definition.json', definition(read(PACKAGE/'plan.json'), values, context))
            save(PACKAGE/'request.json', request(PACKAGE, files, previous))
            command('native-check', ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'), 'tower-affinity-creation-recognition-check', str(PACKAGE/'request.json')])
            save(PACKAGE/'native-check.json', read(PACKAGE/'native-check.log'))
            require(history(OUTPUT.parent, read(PACKAGE/'request.json'), PACKAGE/'history-helper.py') == (files, values), 'History changed during admission')
            save(PACKAGE/'runtime-files.json', inventory(PACKAGE/'runtime'))
            native = {k: read(evidence[k].dest/'native-receipt.json') for k in sizes}
            closeout = {k: read(evidence[k].dest/'closeout.json') for k in sizes}
            overhead = sum(read(PACKAGE/(k+'-process.json'))['seconds'] for k in ('context', 'native-check'))
            save(PACKAGE/'resource-forecast.json', forecast(native, closeout, sizes, read(fixture.dest/'completion.json'), overhead))
            for e in evidence.values():
                e.recheck()
            save(PACKAGE/'consumed-evidence.json', {k: e.used for k, e in evidence.items()})
            result = verify_contents(PACKAGE)
            save(PACKAGE/'admission.json', result)
            check()
            save(PACKAGE/'completion.json', dict(status='AdmissionCompleteNoReservation', seconds=time.monotonic()-started,
                chargedSeconds=SECONDS, chargedBytes=BYTES, scientificLaunches=0, newValues=0, fights=0, retries=0))
            save(PACKAGE/'files.json', inventory(PACKAGE))
            check()
            print(json.dumps(dict(status=result['status'], manifestSha256=sha(PACKAGE/'files.json'),
                requestSha256=result['requestSha256'], measuredSeconds=time.monotonic()-started,
                retainedBytes=launcher.storage_bytes(PACKAGE), fights=0, newValues=0), indent=2))
    except BaseException as error:
        if not (PACKAGE/'failure.json').exists():
            save(PACKAGE/'failure.json', dict(status='AdmissionFailedNoReservation', reason=str(error), chargedSeconds=SECONDS, chargedBytes=BYTES))
        raise
    finally:
        watchdog.cancel()


def verify_contents(package):
    require(not (package/'failure.json').exists() and sha(package/'plan.json') == audit.PLAN, 'Failed admission or changed plan')
    declaration = read(package/'declaration.json')
    require(set(declaration) == set(CONTRACT)|{'harnessFiles', 'implementationFiles', 'tests'}
        and all(declaration[k] == v for k, v in CONTRACT.items()), 'Changed frozen declaration')
    for source, target in zip(IMPLEMENTATION, ('prepare.py', 'context.ps1', 'test-admission.py', 'auditor.py',
        'run-affinity-creation-recognition.py', 'bounded_windows_process.py'), strict=True):
        require(declaration['implementationFiles'][source] == sha(package/target), 'Changed retained implementation')
    require(sha(package/'history-helper.py') == HISTORY_PIN and sha(package/'admission-tests.log') == declaration['tests']['sha256'], 'Changed retained verification helpers')
    consumed = read(package/'consumed-evidence.json')
    require(set(consumed) == set(PINS), 'Missing source evidence')
    for label, used in consumed.items():
        e = package/'evidence'/label
        require(sha(e/'files.json') == PINS[label][1], 'Changed source manifest')
        files = read(e/'files.json')
        for name, pin in used.items():
            key = (PINS[label][0]/name).relative_to(ROOT).as_posix() if label == 'verification' else name
            require(files[key] == pin == sha(e/name), 'Changed consumed evidence')
    e = package/'evidence'
    for label, pin in CLOSEOUT_PINS.items():
        require(sha(e/label/'closeout.json') == pin, 'Changed retained closeout')
    population(package)
    validate_tests(e/'verification/backend-tests.trx')
    tested = read(package/'tested-harness.json')
    require(tested == declaration['harnessFiles'] and tested['BalanceHarness.dll'] == CONTRACT['harnessSha256'], 'Changed tested harness')
    validate_runtime(inventory(package/'runtime'), read(e/'capture/files.json'), tested)
    require(inventory(package/'runtime') == read(package/'runtime-files.json'), 'Changed retained runtime')
    require(inventory(package/'content') == {n[8:]: h for n, h in read(e/'capture/files.json').items() if n.startswith('content/')}, 'Changed captured content')
    context = read(package/'context.json')
    plan = read(package/'plan.json')
    captured = read(e/'capture/candidate-qualification.json')
    require(captured['executionHash'] == plan['capturedRuntime']['executionHash'] == audit.digest(captured['execution']), 'Wrong producing capture')
    old, new = captured['execution'], context['execution']
    require({k: v for k, v in old.items() if k != 'assemblyHashes'} == {k: v for k, v in new.items() if k != 'assemblyHashes'}
        and {k: v for k, v in old['assemblyHashes'].items() if k != 'BalanceHarness'} == {k: v for k, v in new['assemblyHashes'].items() if k != 'BalanceHarness'}, 'Changed captured execution dependencies')
    require(context['status'] == 'CapturedRecognitionRuntimeCompatible' and context['fights'] == context['newValues'] == context['nativePreparations'] == 0
        and context['executionHash'] == audit.digest(new) and context['settingsHash'] == plan['capturedRuntime']['settingsHash'], 'Missing captured compatibility')
    require(context['literalFixture'] == dict(status='StoredEvidenceReconstructed', version=VERSION, teams=108, contrasts=216,
        unmeasured=132, strata=36, samples=256, literalWords=6144, literalPanel=3072, transports=108, inputProjections=216), 'Wrong fixture compatibility')
    sources = read(package/'compiled-source-files.json')
    require(len(sources) == context['compiledSourceDocuments'] and sources and all(sha(package/'source'/n) == h for n, h in sources.items()), 'Changed producing sources')
    for name in ('RunRecognition', 'AuditRecognition', 'RecognitionPublicationCheck', 'VerifyRecognition', 'AssessRecognition', 'RecognitionPlanPin', 'Reserve', 'Execute', 'VerifyStudy'):
        require(any('TowerFixedFamilyConfirmation.'+name+'#' in m for m in context['jitResolvedMethods']), 'Unresolved native path')
    runtime = read(package/'runtime-files.json')
    require(all(runtime.get(k+'.dll') == h for k, h in new['assemblyHashes'].items()), 'Changed execution identity')
    owned = read(package/'owned-fixture-verification.json')
    key = (FIXTURE/'verification.json').relative_to(ROOT).as_posix()
    require(sha(package/'owned-fixture-verification.json') == read(e/'verification/files.json')[key]
        and owned['harnessSha256'] == tested['BalanceHarness.dll'] and owned['actualCombat'] == owned['productionEntropyDraws'] == 0
        and owned['archiveManifestSha256'] == PINS['fixture'][1], 'Changed owned workflow proof')
    q, d = read(package/'request.json'), read(package/'definition.json')
    require(q == request(package, read(package/'history-files.json'), read(e/'pilot/request.json'))
        and d == definition(plan, d['excludedCombatSeeds'], context), 'Changed request or definition')
    require(read(package/'native-check.json') == dict(status='ContractValidNoReservation', version=VERSION, requestHash=audit.digest(q),
        historicalValues=len(d['excludedCombatSeeds']), maximumFights=27648, maximumNewReservations=6144, transportBindings=108,
        resourceFeasibilityEstablished=False, newValues=0, fights=0), 'Changed native check')
    for name in ('context', 'native-check'):
        process_ok(read(package/(name+'-process.json')))
    sizes = read(package/'report-sizes.json')
    for label, value in sizes.items():
        names = {n for n in read(e/label/'files.json') if '/battles/' in n}
        require(names == set(value['sizes']), 'Changed sizing membership')
    native = {k: read(e/k/'native-receipt.json') for k in sizes}
    closeout = {k: read(e/k/'closeout.json') for k in sizes}
    overhead = sum(read(package/(k+'-process.json'))['seconds'] for k in ('context', 'native-check'))
    require(read(package/'resource-forecast.json') == forecast(native, closeout, sizes, read(e/'fixture/completion.json'), overhead), 'Changed resource forecast')
    return dict(status='RecognitionAdmittedNoReservation', version=VERSION, requestSha256=sha(package/'request.json'),
        definitionSha256=sha(package/'definition.json'), executionHash=context['executionHash'], historicalValues=len(d['excludedCombatSeeds']),
        historyFiles=len(q['requiredHistory']), maximumFights=27648, maximumNewReservations=6144, newValues=0, fights=0,
        nativePreparations=108, admissionChargeSeconds=SECONDS, admissionChargeBytes=BYTES,
        maximumSeconds=7800, maximumBytes=4294967296, remainingSeconds=7200, remainingBytes=3758096384,
        totalRecordedChargedSeconds=CONTRACT['priorRecordedSeconds']+SECONDS, totalRecordedChargedBytes=CONTRACT['priorRecordedBytes']+BYTES,
        cumulativeDeclaredMaximumSeconds=CONTRACT['priorDeclaredMaximumSeconds']+SECONDS+7200,
        cumulativeDeclaredMaximumBytes=CONTRACT['priorDeclaredMaximumBytes']+BYTES+3758096384)


def verify(package, pin, live=False):
    authenticate(package, pin)
    result = verify_contents(package)
    require(result == read(package/'admission.json'), 'Changed admission conclusions')
    completion = read(package/'completion.json')
    require(completion['status'] == 'AdmissionCompleteNoReservation' and 0 <= completion['seconds'] < SECONDS
        and completion['chargedSeconds'] == SECONDS and completion['chargedBytes'] == BYTES
        and completion['scientificLaunches'] == completion['newValues'] == completion['fights'] == completion['retries'] == 0
        and launcher.storage_bytes(package) < BYTES, 'Changed admission accounting')
    if live:
        q = read(package/'request.json')
        with launcher.writer_lease(Path(q['registryRoot'])/'complete-family-allocation'), launcher.writer_lease(Path(q['outputRoot'])):
            require(not Path(q['outputRoot']).exists() and history(Path(q['registryRoot']), q, package/'history-helper.py') ==
                (q['requiredHistory'], read(package/'definition.json')['excludedCombatSeeds']), 'Changed live history or existing output')
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=('prepare', 'verify'))
    parser.add_argument('--declaration', type=Path)
    parser.add_argument('--package', type=Path)
    parser.add_argument('--manifest-sha256')
    parser.add_argument('--live', action='store_true')
    args = parser.parse_args()
    if args.command == 'prepare':
        require(args.declaration is not None, 'Supply frozen declaration')
        prepare(args.declaration)
    else:
        require(args.package is not None and args.manifest_sha256 is not None, 'Supply admission and external manifest pin')
        print(json.dumps(verify(args.package.resolve(), args.manifest_sha256, args.live), indent=2))
