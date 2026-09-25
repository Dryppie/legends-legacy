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
VERSION = 'tower-affinity-neighborhood-recognition-v1'
PACKAGE = ROOT/'TestResults/affinity-neighborhood-recognition-admission-20260924'
OUTPUT = ROOT/'TestResults/balance/tower-affinity-neighborhood-recognition-20260924'
BUILD = ROOT/'TestResults/affinity-neighborhood-native-verified-build-20260924/bin/BalanceHarness.ProcessFixture/release'
VERIFICATION = ROOT/'TestResults/affinity-neighborhood-native-implementation-verification-20260924'
CAPTURE = ROOT/'TestResults/affinity-allied-action-admission-20260924'
PILOT = ROOT/'TestResults/balance/tower-affinity-allied-action-pilot-01-20260924'
RECOGNITION = ROOT/'TestResults/balance/tower-affinity-preservation-recognition-20260924'
FIXTURE = ROOT/'TestResults/tower-neighborhood-recognition-owned-fixture-verified-20260924'
PLAN = ROOT/'Balance Harness/Tower-Affinity-Neighborhood-Recognition-Plan.json'
PLAN_PACKAGE = ROOT/'TestResults/affinity-neighborhood-recognition-plan-20260924'
PLAN_PACKAGE_PIN = 'a86890051d8fa44f54855f9b2bde7b9435dc9f6005faad683d698d54811539be'
HISTORY_HELPER = ROOT/'TestResults/three-reference-admission-closeout-20260922/comparison-preparation.py'
HISTORY_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
SECONDS, BYTES = 1800, 536870912
HARNESS = ('BalanceHarness.dll', 'BalanceHarness.pdb', 'BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json')
PINS = {
    'capture': (CAPTURE, 'c8eb52d39b57fc385a5e88330a8ec621908a3345ef3d12c33360cbdcb361e68d'),
    'pilot': (PILOT, 'c9d191a8f81439d6bfa947e8c62bd74a6e35352ee5d999b8dcd9ecdfdded54ef'),
    'recognition': (RECOGNITION, 'dcea52f2ed251c04d3985679cfb31e71285814fa6e97199fc35379f4e82a6c39'),
    'verification': (VERIFICATION, '320d6d937f9761c8331c68082c18e3edf9569a63f63d1ee3a6f4a86162cbf93a'),
    'publication': (ROOT/'TestResults/affinity-allied-action-pilot-01-publication-verification-20260924', '5a1553cf3c0591086514c632ced8c0513ac31cce5ceb8d4dae559156ea8aa7dc'),
    'recognition-publication': (ROOT/'TestResults/affinity-preservation-recognition-publication-verification-20260924', '5fffafb823e3d17bb4bc301f51d9b691da393c8ab48a6a0d01caa1b2ddd91c2c'),
    'review': (ROOT/'TestResults/affinity-allied-action-stage-review-20260924', '84226a6dd0eb2087ac8df8a37fc0e90c5c4cfb990d28547ee48bdf0fa6900f1d'),
    'fixture': (FIXTURE/'result', 'f79c5ce7c8362f03be0322238467274564962093e9fe32e49660744a3ae863e7'),
    'fixture-proof': (FIXTURE, 'b81b63c8ac129dbee40c5846ea5560870f2baeca092d8bc5f348520226035b52'),
}
CLOSEOUT_PINS = dict(pilot='f58552cbd568e66f6e76090f01cd0be8d450e173b552513d445e97a73379dc82',
    recognition='c7e197010b7611afc1fcd814c731faefb285834f7d170225fc95691887a714ce')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def local(name, fallback):
    path = Path(__file__).with_name(name)
    return path if path.exists() else ROOT/fallback


audit = module('affinity_admission_audit', local('auditor.py', 'Balance Harness/analysis/audit-affinity-neighborhood-recognition.py'))
launcher = module('affinity_admission_launcher', local('run-affinity-neighborhood-recognition.py', 'build/run-affinity-neighborhood-recognition.py'))
require, read, sha, save = audit.require, audit.read, audit.sha, launcher.write
IMPLEMENTATION = (
    'Balance Harness/analysis/prepare-affinity-neighborhood-recognition.py',
    'Balance Harness/analysis/affinity-neighborhood-recognition-context.ps1',
    'Balance Harness/analysis/test-affinity-neighborhood-recognition-admission.py',
    'Balance Harness/analysis/audit-affinity-neighborhood-recognition.py',
    'build/run-affinity-neighborhood-recognition.py', 'build/bounded_windows_process.py',
)
CONTRACT = dict(version=VERSION, admissionVersion='tower-affinity-neighborhood-recognition-admission-v1',
    package=PACKAGE.relative_to(ROOT).as_posix(), output=OUTPUT.relative_to(ROOT).as_posix(),
    chargedSeconds=SECONDS, chargedBytes=BYTES, scientificSeconds=18000, scientificBytes=18253611008,
    postpublicationSeconds=3600, postpublicationBytes=268435456,
    priorRecordedSeconds=41262.09795319999, priorRecordedBytes=33519903348,
    priorDeclaredMaximumSeconds=116580, priorDeclaredMaximumBytes=72360132608,
    planSha256=audit.PLAN, harnessSha256='99fb2ec24db2a6de93b8317471a13ac53a04d3a36294708e3b405f7c4792b70f',
    sourceManifests={k: v[1] for k, v in PINS.items()}, planManifestSha256=PLAN_PACKAGE_PIN,
    closeoutPins=CLOSEOUT_PINS, retries=0, timingResamples=0, scientificLaunches=0,
    phaseCaps=dict(nativeSeconds=14400, nativeBytes=17179869184, auditSeconds=3600, auditBytes=1073741824,
        postpublicationSeconds=3600, postpublicationBytes=268435456),
    forecastRule='Margin 2 applied independently to every estimate. Target 90112 fights. Native, audit and full postpublication time each use the maximum observed time per actual fight in the closed allied-action pilot (16256) and preservation recognition (37120), plus current preparation time. Native bytes use target times maximum authenticated gzip size plus maximum non-report native overhead from either real study or the current full literal fixture plus 64 MiB. Audit bytes use maximum scaled historical or full literal-fixture audit bytes plus 16 MiB. Postpublication bytes use maximum scaled authenticated publication package bytes plus current definition/request bytes and 16 MiB; its time also has the full literal-fixture verification floor. No downward correction, timing retry, sample reduction or transfer between caps.')


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
        self.index = self.root
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


def validate_tests(path, total=26):
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    results = ET.parse(path).findall('.//t:UnitTestResult', ns)
    require(len(results) == total and all(r.attrib['outcome'] == 'Passed' for r in results), 'Backend verification failed')
    names = [r.attrib['testName'] for r in results if 'BalanceHarnessAffinityNeighborhoodRecognitionTests.' in r.attrib['testName']]
    require(len(names) == 26 and all(any(part in n for n in names) for part in
        ('Exact_plan_family', 'Terminal_decisions', 'Exact_integer_threshold', 'Incomplete_or_misbound',
         'Nontransferable_envelope', 'One_literal_entropy_batch', 'Wrong_command_profile')), 'Missing new profile tests')


def validate_runtime(actual, captured, tested):
    expected = {n[8:]: h for n, h in captured.items() if n.startswith('runtime/')}
    require(set(tested) == set(HARNESS) and set(tested).issubset(expected), 'Missing harness substitution contract')
    expected.update(tested)
    require(actual == expected, 'Changed captured dependencies or tested harness')


def history(registry, previous, helper):
    require(sha(helper) == HISTORY_PIN, 'Changed history reader')
    return module('affinity_admission_history', helper).history(registry, previous)


def definition(plan, values, context):
    return dict(version=VERSION, teams=[dict(role=t['membership'], partyId=t['partyId'], referenceIds=[], scenario=t['scenario'])
        for t in plan['teams']], contentHashes=plan['sourceContract']['capturedRuntime']['contentHashes'],
        settingsHash=plan['sourceContract']['capturedRuntime']['settingsHash'], executionHash=context['executionHash'], excludedCombatSeeds=values)


def request(package, files, previous):
    charge = package/'admission-charge.json'
    return dict(version=VERSION, contentRoot=str(package/'content'), definitionPath=str(package/'definition.json'),
        definitionHash=sha(package/'definition.json'), registryRoot=str(OUTPUT.parent), outputRoot=str(OUTPUT), requiredHistory=files,
        maximumSeconds=19800, maximumBytes=18790481920, phases={}, priorSeconds=SECONDS, priorBytes=BYTES,
        priorCharges=[dict(scope='Complete-neighborhood recognition admission allowance', seconds=SECONDS, bytes=BYTES,
            receiptPath=str(charge), receiptHash=sha(charge))],
        pendingHistoryRecoveries=previous['pendingHistoryRecoveries'], recoveryReceiptHashes=previous['recoveryReceiptHashes'],
        recognition=dict(planPath=str(package/'plan.json'), auditorPath=str(package/'auditor.py'), auditorHash=sha(package/'auditor.py')))


def process_ok(value):
    require(value['mechanism'] == 'suspended-owned-job-v1' and value['exitCode'] == 0 and not value['timedOut']
        and value['activeProcesses'] == 0 and value['totalProcesses'] >= 1, 'Incomplete owned admission process')


def retain_plan(package):
    authenticate(PLAN_PACKAGE, PLAN_PACKAGE_PIN)
    shutil.copytree(PLAN_PACKAGE, package/'population/plan')
    for name, pin in read(PLAN)['sourceFiles'].items():
        require(sha(ROOT/name) == pin, 'Changed plan source')
        dest = package/'population/source'/name
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT/name, dest)


def population(package):
    sealed = package/'population/plan'
    authenticate(sealed, PLAN_PACKAGE_PIN)
    require(sha(sealed/'plan.json') == sha(package/'plan.json') == audit.PLAN, 'Changed frozen plan')
    builder = module('affinity_frozen_neighborhood', sealed/'affinity-neighborhood-recognition-plan.py')
    value = builder.load_plan(package/'population/source')
    require(value == read(package/'plan.json'), 'Complete neighborhood does not reproduce')


def validate_source_records(plan, publication, review, pilot, captured):
    for label in ('pilot', 'review'):
        folder, pin = PINS[label]
        require(plan['sourceFiles'][folder.relative_to(ROOT).as_posix()+'/files.json'] == pin, 'Plan source bindings changed')
    require(publication['status'] == 'VerifiedPublishedArchiveAndCompleteLiveHistory'
        and publication['scientificManifestSha256'] == PINS['pilot'][1]
        and publication['scientificCloseoutSha256'] == CLOSEOUT_PINS['pilot']
        and publication['admissionManifestSha256'] == PINS['capture'][1]
        and publication['scientificFights'] == 16256, 'Missing closed allied-action publication')
    require(pilot['version'] == 'tower-affinity-allied-action-comparison-v1'
        and pilot['decision'] == review['originalDecision'] == 'Inconclusive'
        and pilot['fights'] == review['sourceFights'] == 16256, 'Changed source decision or stage review')
    require(captured['executionHash'] == plan['sourceContract']['capturedRuntime']['executionHash']
        == audit.digest(captured['execution']), 'Wrong producing capture')


def publications(package):
    result = {}
    for label, source, field in [('pilot', 'publication', 'measuredVerificationSecondsBeforeSealing'),
        ('recognition', 'recognition-publication', 'seconds')]:
        root = package/'evidence'/source
        value = read(root/'verification.json')
        require(value['status'] == 'VerifiedPublishedArchiveAndCompleteLiveHistory'
            and value['scientificManifestSha256'] == PINS[label][1], 'Unbound postpublication evidence')
        process_ok(value['ownedProcess'])
        # Every package member is retained, authenticated and counted, not a subset.
        authenticate(root, PINS[source][1])
        result[label] = dict(seconds=value[field], bytes=launcher.storage_bytes(root))
    return result


def forecast(native, closeout, sizes, fixture, preparation_seconds, publication, request_bytes, fixture_verification_seconds):
    require(set(native) == set(closeout) == set(sizes) == {'pilot', 'recognition'}, 'Changed forecast sources')
    for label, count, version, status in [('pilot', 16256, 'tower-affinity-allied-action-comparison-v1', 'MeasuredPendingAudits'),
        ('recognition', 37120, 'tower-affinity-preservation-recognition-v1', 'Verified')]:
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
    target, margin = 90112, 2
    native_seconds = margin*(max(native[k]['measuredSeconds']*target/native[k]['fights'] for k in native)+preparation_seconds)
    audit_seconds = margin*(max(closeout[k]['auditSeconds']*target/native[k]['fights'] for k in native)+preparation_seconds)
    overhead = max([native[k]['observedBytes']-sizes[k]['bytes'] for k in native]+[fixture['nativeBytes']])
    native_bytes = margin*(target*max(s['maximumBytes'] for s in sizes.values())+overhead+64*1048576)
    audit_bytes = margin*(max([closeout[k]['auditBytes']*target/native[k]['fights'] for k in native]+[fixture['auditBytes']])+16*1048576)
    require(set(publication) == set(native) and type(request_bytes) is int and request_bytes > 0
        and type(fixture_verification_seconds) in (int,float) and math.isfinite(fixture_verification_seconds) and fixture_verification_seconds > 0
        and all(type(v) in (int,float) and math.isfinite(v) and v > 0 for p in publication.values() for v in (p['seconds'],p['bytes'])), 'Invalid publication measurements')
    post_seconds = margin*(max([publication[k]['seconds']*target/native[k]['fights'] for k in native]+[fixture_verification_seconds])+preparation_seconds)
    post_bytes = margin*(max(publication[k]['bytes']*target/native[k]['fights'] for k in native)+request_bytes+16*1048576)
    estimates = dict(nativeSeconds=native_seconds, nativeBytes=native_bytes, auditSeconds=audit_seconds,
        auditBytes=audit_bytes, postpublicationSeconds=post_seconds, postpublicationBytes=post_bytes)
    require(all(v < CONTRACT['phaseCaps'][k] for k,v in estimates.items()), 'Forecast does not fit separate fixed caps')
    return dict(rule=CONTRACT['forecastRule'], margin=margin, targetFights=target, nativeSeconds=native_seconds,
        nativeBytes=native_bytes, auditSeconds=audit_seconds, auditBytes=audit_bytes,
        postpublicationSeconds=post_seconds, postpublicationBytes=post_bytes, phaseCaps=CONTRACT['phaseCaps'],
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
            tests = proof.get('neighborhood-verified-tests.trx')
            validate_tests(proof.get('backend-regression-tests.trx'), 144)
            validate_tests(tests)
            sources = read(proof.get('tested-source-files.json'))
            for name, pin in sources.items():
                # Git line-ending metadata is not a compiled input. It is the
                # only preceding source-list entry changed by this admission.
                if name != '.gitattributes':
                    require(sha(ROOT/name) == pin, 'Tested source changed: '+name)
            tested_runtime = read(proof.get('tested-runtime-files.json'))
            require(d['harnessFiles'] == {n: tested_runtime[(BUILD/n).relative_to(ROOT).as_posix()] for n in HARNESS}, 'Harness files differ from verified runtime')
            proof.get('verification.json')
            proof.get('engineering-fixtures.json')
            shutil.copyfile(evidence['fixture-proof'].get('verification.json'), PACKAGE/'owned-fixture-verification.json')
            prior.get('result.json')
            for label in ('publication', 'recognition-publication'):
                for name in evidence[label].files:
                    evidence[label].get(name)
            evidence['review'].get('review.json')
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
            retain_plan(PACKAGE)
            for source, target in [(PLAN, 'plan.json'), (Path(__file__), 'prepare.py'),
                (ROOT/IMPLEMENTATION[1], 'context.ps1'), (ROOT/IMPLEMENTATION[3], 'auditor.py'),
                (ROOT/IMPLEMENTATION[4], 'run-affinity-neighborhood-recognition.py'), (ROOT/IMPLEMENTATION[5], 'bounded_windows_process.py'),
                (HISTORY_HELPER, 'history-helper.py'), (ROOT/d['tests']['path'], 'admission-tests.log'),
                (ROOT/IMPLEMENTATION[2], 'test-admission.py')]:
                shutil.copyfile(source, PACKAGE/target)
            population(PACKAGE)
            sizes = {label: report_sizes(evidence[label], count, check) for label, count in [('pilot', 16256), ('recognition', 37120)]}
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
            command('native-check', ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'), 'tower-affinity-neighborhood-recognition-check', str(PACKAGE/'request.json')])
            save(PACKAGE/'native-check.json', read(PACKAGE/'native-check.log'))
            require(history(OUTPUT.parent, read(PACKAGE/'request.json'), PACKAGE/'history-helper.py') == (files, values), 'History changed during admission')
            save(PACKAGE/'runtime-files.json', inventory(PACKAGE/'runtime'))
            native = {k: read(evidence[k].dest/'native-receipt.json') for k in sizes}
            closeout = {k: read(evidence[k].dest/'closeout.json') for k in sizes}
            overhead = sum(read(PACKAGE/(k+'-process.json'))['seconds'] for k in ('context', 'native-check'))
            save(PACKAGE/'resource-forecast.json', forecast(native, closeout, sizes, read(fixture.dest/'completion.json'), overhead, publications(PACKAGE),
                sum((PACKAGE/n).stat().st_size for n in ('request.json','definition.json')), read(PACKAGE/'owned-fixture-verification.json')['verificationProcess']['seconds']))
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
        'run-affinity-neighborhood-recognition.py', 'bounded_windows_process.py'), strict=True):
        require(declaration['implementationFiles'][source] == sha(package/target), 'Changed retained implementation')
    require(sha(package/'history-helper.py') == HISTORY_PIN and sha(package/'admission-tests.log') == declaration['tests']['sha256'], 'Changed retained verification helpers')
    consumed = read(package/'consumed-evidence.json')
    require(set(consumed) == set(PINS), 'Missing source evidence')
    for label, used in consumed.items():
        e = package/'evidence'/label
        require(sha(e/'files.json') == PINS[label][1], 'Changed source manifest')
        files = read(e/'files.json')
        for name, pin in used.items():
            key = name
            require(files[key] == pin == sha(e/name), 'Changed consumed evidence')
    e = package/'evidence'
    for label, pin in CLOSEOUT_PINS.items():
        require(sha(e/label/'closeout.json') == pin, 'Changed retained closeout')
    population(package)
    validate_tests(e/'verification/neighborhood-verified-tests.trx')
    validate_tests(e/'verification/backend-regression-tests.trx', 144)
    tested = read(package/'tested-harness.json')
    require(tested == declaration['harnessFiles'] and tested['BalanceHarness.dll'] == CONTRACT['harnessSha256'], 'Changed tested harness')
    validate_runtime(inventory(package/'runtime'), read(e/'capture/files.json'), tested)
    require(inventory(package/'runtime') == read(package/'runtime-files.json'), 'Changed retained runtime')
    require(inventory(package/'content') == {n[8:]: h for n, h in read(e/'capture/files.json').items() if n.startswith('content/')}, 'Changed captured content')
    context = read(package/'context.json')
    plan = read(package/'plan.json')
    captured = read(e/'capture/candidate-qualification.json')
    validate_source_records(plan, read(e/'publication/verification.json'), read(e/'review/review.json'),
        read(e/'pilot/result.json'), captured)
    for label in ('pilot','recognition'):
        n,c,done = (read(e/label/name) for name in ('native-receipt.json','closeout.json','completion.json'))
        require(n['requestFileHash'] == c['requestHash'] == done['requestFileHash'] == sha(e/label/'request.json')
            and done['status'] == 'Complete' and done['version'] == n['version'] == c['version'], 'Unbound completed resource source')
    old, new = captured['execution'], context['execution']
    require({k: v for k, v in old.items() if k != 'assemblyHashes'} == {k: v for k, v in new.items() if k != 'assemblyHashes'}
        and {k: v for k, v in old['assemblyHashes'].items() if k != 'BalanceHarness'} == {k: v for k, v in new['assemblyHashes'].items() if k != 'BalanceHarness'}, 'Changed captured execution dependencies')
    require(context['status'] == 'CapturedRecognitionRuntimeCompatible' and context['fights'] == context['newValues'] == 0 and context['nativePreparations'] == 264
        and context['executionHash'] == audit.digest(new) and context['settingsHash'] == plan['sourceContract']['capturedRuntime']['settingsHash'], 'Missing captured compatibility')
    require(context['literalFixture'] == dict(status='StoredEvidenceReconstructed', version=VERSION, teams=44, endpoints=46,
        samples=2048, literalWords=16384, literalPanel=2048, transports=132, inputProjections=264), 'Wrong fixture compatibility')
    projections = read(package/'input-projections.json')
    require(len(projections) == 264 and {(p['teamOrdinal'],p['sliceOrdinal']) for p in projections} ==
        {(team,slice_) for team in range(44) for slice_ in range(3)}
        and len({p['partyHash'] for p in projections}) == 44, 'Incomplete prepared transport coverage')
    sources = read(package/'compiled-source-files.json')
    require(len(sources) == context['compiledSourceDocuments'] and sources and all(sha(package/'source'/n) == h for n, h in sources.items()), 'Changed producing sources')
    for name in ('RunRecognition', 'AuditRecognition', 'RecognitionPublicationCheck', 'VerifyRecognition', 'AssessRecognition', 'NeighborhoodOutcome', 'AssessNeighborhoodRecognition', 'RecognitionPlanPin', 'Reserve', 'Execute', 'VerifyStudy'):
        require(any('TowerFixedFamilyConfirmation.'+name+'#' in m for m in context['jitResolvedMethods']), 'Unresolved native path')
    runtime = read(package/'runtime-files.json')
    require(all(runtime.get(k+'.dll') == h for k, h in new['assemblyHashes'].items()), 'Changed execution identity')
    owned = read(package/'owned-fixture-verification.json')
    require(owned == read(e/'fixture-proof/verification.json')
        and owned['status'] == 'OwnedNeighborhoodFixturePassed' and owned['literalReports'] == 90112 and owned['actualCombat'] == owned['productionEntropyDraws'] == 0
        and owned['archiveManifestSha256'] == PINS['fixture'][1], 'Changed owned workflow proof')
    completed = read(e/'verification/engineering-fixtures.json')['completed']
    require(len(completed) == 1 and completed[0]['filesHash'] == PINS['fixture-proof'][1]
        and completed[0]['verification'] == owned, 'Fixture is not the tested native workflow')
    for field in ('contextProcess', 'verificationProcess'):
        process_ok(owned[field])
    require(all(owned[k] is True for k in ('nativeWorker','nativeReconstruction','independentPythonAudit',
        'nativePublicationBarrier','nativePostPublicationVerification')), 'Incomplete owned workflow proof')
    runtime_proof = read(e/'verification/tested-runtime-files.json')
    require(tested == {n:runtime_proof[(BUILD/n).relative_to(ROOT).as_posix()] for n in HARNESS}, 'Changed tested runtime proof')
    q, d = read(package/'request.json'), read(package/'definition.json')
    require(q == request(package, read(package/'history-files.json'), read(e/'pilot/request.json'))
        and d == definition(plan, d['excludedCombatSeeds'], context), 'Changed request or definition')
    require(read(package/'native-check.json') == dict(status='ContractValidNoReservation', version=VERSION, requestHash=audit.digest(q),
        historicalValues=len(d['excludedCombatSeeds']), maximumFights=90112, maximumNewReservations=16384, transportBindings=132,
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
    require(read(package/'resource-forecast.json') == forecast(native, closeout, sizes, read(e/'fixture/completion.json'), overhead, publications(package),
        sum((package/n).stat().st_size for n in ('request.json','definition.json')), owned['verificationProcess']['seconds']), 'Changed resource forecast')
    return dict(status='RecognitionAdmittedNoReservation', version=VERSION, requestSha256=sha(package/'request.json'),
        definitionSha256=sha(package/'definition.json'), executionHash=context['executionHash'], historicalValues=len(d['excludedCombatSeeds']),
        historyFiles=len(q['requiredHistory']), maximumFights=90112, maximumNewReservations=16384, newValues=0, fights=0,
        nativePreparations=308, admissionChargeSeconds=SECONDS, admissionChargeBytes=BYTES,
        maximumSeconds=19800, maximumBytes=18790481920, remainingSeconds=18000, remainingBytes=18253611008,
        separatePostpublicationSeconds=3600, separatePostpublicationBytes=268435456,
        totalRecordedChargedSeconds=CONTRACT['priorRecordedSeconds']+SECONDS, totalRecordedChargedBytes=CONTRACT['priorRecordedBytes']+BYTES,
        cumulativeDeclaredMaximumSeconds=CONTRACT['priorDeclaredMaximumSeconds']+SECONDS+18000+3600,
        cumulativeDeclaredMaximumBytes=CONTRACT['priorDeclaredMaximumBytes']+BYTES+18253611008+268435456)


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
