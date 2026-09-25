"""One separately charged admission of resource envelope v2; never starts combat or allocation.

The declaration binds the tested build and verification evidence before qualification.
The two rejected admissions and single cost probe remain immutable. No timing retry.
"""
import argparse
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


base = module('proposal_admission_shared', Path(__file__).with_name('prepare-proposal-affinity-admission.py'))
owner = base.launcher
require, read, sha, save, inventory = base.require, base.read, base.sha, base.save, base.inventory
VERSION = 'proposal-resource-admission-v2'
SECONDS, BYTES = 900, 1073741824
PACKAGE = ROOT/'TestResults/proposal-affinity-resource-admission-20260923'
OUTPUT = ROOT/'TestResults/balance/tower-proposal-affinity-pilot-01-20260923'
REVIEW = ROOT/'TestResults/proposal-affinity-admission-review-20260923'
REVIEW_PIN = 'b2cd5249373b58069af6000579aa2d73c971999adc74b5d57e19a0369b8c28d2'
PROBE = ROOT/'TestResults/proposal-native-audit-cost-probe-20260923'
PROBE_PIN = '8929c5b94a95e717e8f7ec2ffb4d94ee01d0cbc9083e4b31873f635a5cc85cf7'
CLOSEOUT = ROOT/'TestResults/proposal-audit-probe-closeout-20260923'
CLOSEOUT_PIN = '1b558bb858164a0e7942e915ad7e8d475206e72486c57ef19404efc5e5bc1ea1'
CHANGED_SOURCES = {'LL/tools/BalanceHarness/'+name for name in (
    'TowerProposalStudyResources.cs', 'TowerProposalStudyProtocol.cs', 'TowerProposalStudyRun.cs')}
PILOT02_VERSION = 'proposal-affinity-pilot-02-admission-v1'
PILOT02_PACKAGE = ROOT/'TestResults/proposal-affinity-pilot-02-admission-20260923'
PILOT02_OUTPUT = ROOT/'TestResults/balance/tower-proposal-affinity-pilot-02-20260923'
FAILED_STUDY = OUTPUT
FAILURE_REVIEW = ROOT/'TestResults/proposal-affinity-pilot-01-failure-closeout-20260923'
FAILURE_REVIEW_PIN = 'c095d252617a9ed1a85db096b586aa0d1a532ff4acb59c5d928def480d8e586f'
CONSUMED_ADMISSION = ROOT/'TestResults/proposal-affinity-resource-admission-repaired-20260923'
CONSUMED_ADMISSION_PIN = '4737c0bc455f3e7f7ce82149072564ec2f5431567ed45f8e5d10499c3235c711'
REPAIR_HANDOFF_PIN = 'a146fe56bd0c89c3a3f52d0dede95202de706e2d664d206bd47a1de5ea9a3590'
RETENTION_BYTES = 64*1048576
PILOT02_CONTRACT = dict(version=PILOT02_VERSION, scopeId='proposal-affinity-pilot-02',
    package=str(PILOT02_PACKAGE), output=str(PILOT02_OUTPUT), resourceEnvelope=owner.RESOURCE_V2,
    chargedSeconds=SECONDS, chargedBytes=BYTES, originalPlanFileSha256=base.PLAN_PIN,
    resourceProbeManifestSha256=PROBE_PIN, failedStudyCloseoutManifestSha256=FAILURE_REVIEW_PIN,
    precedingChargedSeconds=15360, precedingChargedBytes=11140071424,
    scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
    scientificLaunches=0, maximumScientificLaunches=1, retries=0,
    retentionMaximumBytes=RETENTION_BYTES, retentionCheckRequired=True,
    forecastFloorManifestSha256=CONSUMED_ADMISSION_PIN,
    forecastRule='Maximum of the consumed admission and current qualification forecasts; frozen native audit probe, upward-only inventory scaling, margin 2 and publication reserve 120. No timing resampling.')
PILOT02_REPAIRED_VERSION = 'proposal-affinity-pilot-02-qualified-admission-v1'
PILOT02_REPAIRED_PACKAGE = ROOT/'TestResults/proposal-affinity-pilot-02-admission-repaired-20260923'
QUALIFIED_REVIEW = ROOT/'TestResults/proposal-pilot-02-admission-failure-closeout-20260923'
QUALIFIED_REVIEW_PIN = '642f6333b073ec35c60742ea4714bf635bd9477692a8f24b925041ac604034c0'
PILOT02_REPAIRED_CONTRACT = dict(PILOT02_CONTRACT, version=PILOT02_REPAIRED_VERSION,
    package=str(PILOT02_REPAIRED_PACKAGE), precedingChargedSeconds=16380, precedingChargedBytes=12280922112,
    qualifiedAttemptReviewSha256=QUALIFIED_REVIEW_PIN, reuseSealedQualification=True, timingResamples=0)


def validate_pilot02_contract(d):
    contract = PILOT02_REPAIRED_CONTRACT if d.get('version') == PILOT02_REPAIRED_VERSION else PILOT02_CONTRACT
    require(all(d.get(k) == v for k,v in contract.items()),
            'Changed pilot 02 identity, prior charges, scientific design or qualification rules')
    require(d['repairHandoff']['sha256'] == REPAIR_HANDOFF_PIN and d['backendTestCount'] == 34
            and d['baselineBackendTestCount'] == 184, 'Missing verified retention repair or baseline tests')


def validate_retention(report, expected, actual):
    require(report['status'] == 'RuntimeRetentionVerifiedNoReservation'
            and report['fights'] == report['newValues'] == 0
            and report['maximumBytes'] == RETENTION_BYTES and 0 < report['retainedBytes'] <= RETENTION_BYTES
            and report['runtimeFiles'] == len(expected) and expected == actual
            and report['harnessSha256'] == expected['BalanceHarness.dll']
            and report['symbolsSha256'] == expected['BalanceHarness.pdb'],
            'Native retention did not preserve the complete admitted runtime and symbols')


def pilot02_preceding():
    base.authenticate(FAILURE_REVIEW, FAILURE_REVIEW_PIN)
    base.authenticate(CONSUMED_ADMISSION, CONSUMED_ADMISSION_PIN)
    proof = read(FAILURE_REVIEW/'verification.json')
    require(inventory(FAILED_STUDY) == read(FAILURE_REVIEW/'failed-study-files.json')
            and proof['status'] == 'ScientificLaunchFailedBeforeEntropyAndCombat'
            and proof['fights'] == proof['newValues'] == proof['attempts'] == proof['retries'] == 0
            and proof['scientificLaunches'] == 1 and proof['failureBeforeReservation']
            and proof['admissionManifestSha256'] == CONSUMED_ADMISSION_PIN
            and proof['totalRecordedChargedSeconds'] == PILOT02_CONTRACT['precedingChargedSeconds']
            and proof['totalRecordedChargedBytes'] == PILOT02_CONTRACT['precedingChargedBytes'],
            'Changed failed scientific study or its full recorded charges')
    return dict(chargedSeconds=proof['totalRecordedChargedSeconds'], chargedBytes=proof['totalRecordedChargedBytes'],
        failedStudyCloseoutManifestSha256=FAILURE_REVIEW_PIN, consumedAdmissionManifestSha256=CONSUMED_ADMISSION_PIN,
        scope='Recorded admission/probe/review charges and the full failed pilot 01 scientific allowance; no refund or reuse. Development builds and literal tests remain separate.')


def qualified_pilot02():
    base.authenticate(QUALIFIED_REVIEW, QUALIFIED_REVIEW_PIN)
    proof = read(QUALIFIED_REVIEW/'verification.json')
    require(proof['status'] == 'Pilot02AdmissionQualifiedRetentionWrapperFailedNoReservation'
            and proof['qualifiedRuntimeReusableWithoutResampling'] and proof['retentionCheckStillRequired']
            and proof['scientificLaunches'] == proof['fights'] == proof['newValues'] == proof['timingResamples'] == 0
            and inventory(PILOT02_PACKAGE) == read(QUALIFIED_REVIEW/'failed-admission-files.json'),
            'Changed sealed qualification or failed admission')
    return proof


def repaired_pilot02_preceding():
    prior = pilot02_preceding(); proof = qualified_pilot02()
    require(proof['totalRecordedChargedSeconds'] == prior['chargedSeconds']+SECONDS+120
            == PILOT02_REPAIRED_CONTRACT['precedingChargedSeconds']
            and proof['totalRecordedChargedBytes'] == prior['chargedBytes']+BYTES+67108864
            == PILOT02_REPAIRED_CONTRACT['precedingChargedBytes'], 'Missing failed qualification/review charge')
    return dict(prior, chargedSeconds=proof['totalRecordedChargedSeconds'], chargedBytes=proof['totalRecordedChargedBytes'],
        qualifiedAttemptReviewSha256=QUALIFIED_REVIEW_PIN,
        scope=prior['scope']+' Also includes the failed pilot 02 admission and its separately charged closeout.')


def copy_qualified_pilot02(package, files, values, harness_pin):
    proof = qualified_pilot02()
    require(proof['harnessSha256'] == harness_pin and read(PILOT02_PACKAGE/'history-files.json') == files
            and read(PILOT02_PACKAGE/'history.json') == values, 'Changed runtime or history since sealed qualification')
    source_files = read(PILOT02_PACKAGE/'compiled-source-files.json')
    require(all(sha(ROOT/n) == pin for n,pin in source_files.items()), 'Producing source changed since qualification')
    shutil.copytree(PILOT02_PACKAGE/'source', package/'source')
    for name in ('compiled-source-files.json','baseline-qualification.json','candidate-qualification.json',
                 'baseline-projections.json','candidate-projections.json','baseline-process.json','candidate-process.json',
                 'baseline.log','candidate.log'):
        shutil.copyfile(PILOT02_PACKAGE/name, package/name)


def validate_declaration(d, package=None):
    pilot02 = d['version'] in (PILOT02_VERSION, PILOT02_REPAIRED_VERSION)
    if pilot02: validate_pilot02_contract(d)
    expected_package = (PILOT02_REPAIRED_PACKAGE if d['version'] == PILOT02_REPAIRED_VERSION else PILOT02_PACKAGE) if pilot02 else (package or PACKAGE)
    require(d['version'] in (VERSION, PILOT02_VERSION, PILOT02_REPAIRED_VERSION) and d['resourceEnvelope'] == owner.RESOURCE_V2
            and d['package'] == str(expected_package) and d['output'] == str(PILOT02_OUTPUT if pilot02 else OUTPUT)
            and d['chargedSeconds'] == SECONDS and d['chargedBytes'] == BYTES,
            'Changed declaration, output or admission allowance')
    require(set(d['harnessFiles']) == set(base.HARNESS_FILES) and len(d['harnessFiles']['BalanceHarness.dll']) == 64,
            'Missing tested harness inventory')
    base.validate_tests(Path(d['backendTests']['path']), d['backendTestCount'])
    require(d['backendTestCount'] >= (34 if pilot02 else 100) and d['verificationFiles'], 'Incomplete implementation verification')
    extra = [d['repairHandoff'], d['baselineBackendTests']] if pilot02 else []
    for bound in [d['backendTests'], *d['verificationFiles'], *d['ownedFixtures'].values(), *extra]:
        path = Path(bound['path'])
        require(path.is_absolute() and path.is_relative_to(ROOT/'TestResults') and sha(path) == bound['sha256'],
                'Changed declared verification evidence')
    require(all(sha(Path(d['harness'])/n) == pin for n,pin in d['harnessFiles'].items()), 'Changed tested build')
    for name, pin in d['implementationFiles'].items():
        path = (ROOT/name).resolve()
        require(path.is_relative_to(ROOT) and sha(path) == pin, 'Changed declared implementation: '+name)
    if pilot02:
        handoff = read(Path(d['repairHandoff']['path']))
        base.validate_tests(Path(d['baselineBackendTests']['path']), d['baselineBackendTestCount'])
        require(handoff['status'] == 'RuntimeRetentionRepairVerifiedNotQualifiedOrAdmitted'
                and handoff['harnessSha256'] == d['harnessFiles']['BalanceHarness.dll']
                and handoff['symbolsSha256'] == d['harnessFiles']['BalanceHarness.pdb']
                and handoff['backendTrxSha256'] == d['backendTests']['sha256']
                and handoff['ownedCompletionVerificationSha256'] == d['ownedFixtures']['complete']['sha256']
                and handoff['ownedFailureVerificationSha256'] == d['ownedFixtures']['failure']['sha256']
                and d['baselineBackendTests']['sha256'] == sha(CONSUMED_ADMISSION/'backend-tests.trx')
                and all(d['implementationFiles'].get(n) == p for n,p in handoff['sourceFiles'].items()),
                'Declaration does not bind the verified runtime-retention repair')
    for mode, expected in (('complete','OwnedProposalFixturePassed'), ('failure','OwnedProposalFailureFixturePassed')):
        path = Path(d['ownedFixtures'][mode]['path']); result = read(path)
        require(result['status'] == expected and result['actualCombat'] == result['productionEntropyDraws'] == 0
                and result['harnessSha256'] == d['harnessFiles']['BalanceHarness.dll']
                and read(path.parent/'result/request.json')['resourceEnvelope'] == owner.RESOURCE_V2,
                'Missing owned verification on this envelope/build')
        if mode == 'complete':
            require(sha(path.parent/'result/closeout.json') == result['closeoutSha256']
                    and sha(path.parent/'result/files.json') == result['archiveManifestSha256'], 'Changed owned completion')
            phases = [read(path.parent/'result'/name) for name in
                ('native-audit-process.json','independent-audit-process.json','publication-process.json')]
            for phase in phases: base.process_ok(phase)
            require(phases[0]['workAllowanceSeconds'] < 1800 and all(
                later['workAllowanceSeconds']+earlier['seconds'] <= earlier['workAllowanceSeconds']+.001
                for earlier,later in zip(phases,phases[1:])), 'Audits/publication did not share a cumulative deadline')
        else:
            require(result['chargedAttempts'] == 1 and result['retainedValues'] == 16384
                    and not (path.parent/'result/closeout.json').exists(), 'Lost failure reservation or unexpected publication')


def preceding():
    base.authenticate(REVIEW, REVIEW_PIN)
    for number, package in ((1, base.PRECEDING), (2, base.RESOURCE_ATTEMPT)):
        require(inventory(package) == read(REVIEW/f'attempt-{number}-files.json'), 'Changed retained failed admission')
        require(read(package/'failure.json')['status'] == 'AdmissionFailedNoReservation', 'Changed prior failure')
    base.authenticate(PROBE, PROBE_PIN); base.authenticate(CLOSEOUT, CLOSEOUT_PIN)
    prior = read(CLOSEOUT/'verification.json')
    require(prior['status'] == 'ProbeAndProposedAmendmentVerifiedNoAdmission' and not prior['admitted']
            and prior['probeManifestSha256'] == PROBE_PIN, 'Incomplete resource probe closeout')
    require(read(PROBE/'resource-assessment.json')['auditPlanningSeconds'] == 1301.0065557691128,
            'Changed frozen cost probe')
    amendment = 'Balance Harness/Tower-Proposal-Affinity-Resource-Amendment.json'
    require(sha(ROOT/amendment) == prior['sourceHashes'][amendment], 'Changed proposed amendment after probe closeout')
    return dict(chargedSeconds=prior['totalRecordedEngineeringChargedSeconds'],
        chargedBytes=prior['totalRecordedEngineeringChargedBytes'], reviewManifestSha256=REVIEW_PIN,
        probeManifestSha256=PROBE_PIN, probeCloseoutManifestSha256=CLOSEOUT_PIN,
        scope='Recorded admission/probe/review charges through the probe closeout; development test work is separate.')


def source_changes(sources, pilot02=False):
    old = read((CONSUMED_ADMISSION if pilot02 else base.RESOURCE_ATTEMPT)/'compiled-source-files.json')
    def production(rows): return {n:p for n,p in rows.items() if n.startswith('LL/tools/BalanceHarness/') and n.endswith('.cs')}
    old, new = production(old), production(sources)
    changed = {n for n in old.keys() | new.keys() if old.get(n) != new.get(n)}
    allowed = CHANGED_SOURCES - {'LL/tools/BalanceHarness/TowerProposalStudyResources.cs'} if pilot02 else CHANGED_SOURCES
    require(changed == allowed and old.keys() <= new.keys(), 'Changes exceed the declared infrastructure repair')
    return sorted(changed)


def retained_forecast_inputs(probe_root=PROBE):
    # The previous admission did not publish this file at its root. The cost
    # probe retained and authenticated the consumed forecast under this name.
    require(sha(probe_root/'files.json') == PROBE_PIN, 'Changed forecast source manifest')
    files = read(probe_root/'files.json')
    for name in ('previous-resource-forecast.json','resource-assessment.json'):
        require(sha(probe_root/name) == files[name], 'Changed retained forecast source')
    previous, probe = read(probe_root/'previous-resource-forecast.json'), read(probe_root/'resource-assessment.json')
    return previous, probe, previous['totalBytes']/probe['inventoryByteRatio']


def forecast(previous, probe, historical_bytes, estimate):
    """Keep the probe formula/margins. Never reduce its frozen storage projection.

    Native timing comes from the newly qualified runtime. Audit timing comes from
    the retained native probe, with extra hash passes scaled upward if needed.
    """
    require(probe['status'] == 'AuditPartitionStillNotSupported' and probe['timingMargin'] == 2
            and probe['publicationReserveSeconds'] == 120 and historical_bytes > 0,
            'Changed resource planning basis')
    ratio = max(1, estimate['totalBytes']/historical_bytes)
    extra = probe['extraInventorySeconds'] * max(1, ratio/probe['inventoryByteRatio'])
    audit_seconds = 2*(probe['wholeWorkerSeconds']+sum(probe['independentAuditSeconds'])+extra)+120
    audit_seconds = max(probe['auditPlanningSeconds'], audit_seconds)
    native_seconds = max(previous['nativeSeconds'], estimate['nativeSeconds'])
    native_bytes, audit_bytes = max(previous['nativeBytes'], estimate['nativeBytes']), max(previous['auditBytes'], estimate['auditBytes'])
    require(all(math.isfinite(v) and v > 0 for v in (audit_seconds,native_seconds,native_bytes,audit_bytes)), 'Invalid forecast')
    return dict(resourceEnvelope=owner.RESOURCE_V2, nativeSeconds=native_seconds, auditSeconds=audit_seconds,
        nativeBytes=native_bytes, auditBytes=audit_bytes, totalSeconds=native_seconds+audit_seconds, totalBytes=native_bytes+audit_bytes,
        nativeMaximumSeconds=9000, auditMaximumSeconds=1800, nativeMaximumBytes=owner.NATIVE_BYTES, auditMaximumBytes=owner.AUDIT_BYTES,
        fits=native_seconds < 9000 and audit_seconds < 1800 and native_bytes < owner.NATIVE_BYTES and audit_bytes < owner.AUDIT_BYTES,
        resourceProbeManifestSha256=PROBE_PIN, timingMargin=2, publicationReserveSeconds=120,
        guaranteedUpperBound=False, actualFights=0, newValues=0,
        formula='Native: maximum of prior and current qualification forecast. Audit: frozen native probe formula, upward-only outer inventory scaling. No resampling or reduced margins.')


def pilot02_forecast(previous, probe, historical_bytes, estimate):
    # Use the latest consumed admission's floors, without altering historical v2
    # calculations or repeating the cost probe. Archive growth only adds cost.
    estimate = dict(estimate)
    for name in ('nativeSeconds','nativeBytes','auditBytes','totalBytes'):
        estimate[name] = max(estimate[name], previous[name])
    probe = dict(probe, auditPlanningSeconds=max(probe['auditPlanningSeconds'], previous['auditSeconds']))
    return forecast(previous, probe, historical_bytes, estimate)


def prepare(args):
    require(os.name == 'nt' and sha(args.declaration) == args.pin, 'Use the externally pinned Windows declaration')
    d = read(args.declaration); validate_declaration(d)
    reuse_qualification = d['version'] == PILOT02_REPAIRED_VERSION
    pilot02 = d['version'] in (PILOT02_VERSION, PILOT02_REPAIRED_VERSION)
    package, output = (Path(d['package']), PILOT02_OUTPUT) if pilot02 else (PACKAGE, OUTPUT)
    prior_check = repaired_pilot02_preceding if reuse_qualification else (pilot02_preceding if pilot02 else preceding)
    require(not package.exists() and not output.exists(), 'No repeat, resume or existing output')
    started = time.monotonic(); package.mkdir()
    save(package/'admission-charge.json', dict(version=d['version'], chargedSeconds=SECONDS, chargedBytes=BYTES,
        scope='New v2 runtime qualification/admission; entire allowance charged on failure; no scientific launch or allocation.'))
    watchdog = threading.Timer(SECONDS, lambda:os._exit(124)); watchdog.daemon=True; watchdog.start()
    process_owner = module('resource_admission_owner', ROOT/'build/bounded_windows_process.py')
    def check():
        require(time.monotonic()-started < SECONDS-5 and not output.exists(), 'Admission deadline or unexpected scientific output')
        require(owner.storage_bytes(package) < BYTES-4*1048576, 'Admission storage exhausted')
    def command(name, cmd):
        receipt = process_owner.run(cmd, str(ROOT), str(package/(name+'.log')), started+SECONDS-5, cleanup_seconds=1, check=check)
        save(package/(name+'-process.json'), receipt); base.process_ok(receipt); check()
    try:
        with owner.writer_lease(output.parent/'complete-family-allocation'), owner.writer_lease(output):
            prior = prior_check(); save(package/'preceding-charges.json', prior); check()
            captured = base.authenticate(base.CAPTURE, base.CAPTURE_PIN); base.authenticate(base.PREVIEW, base.PREVIEW_PIN)
            require(sha(base.PLAN) == base.PLAN_PIN, 'Changed original scientific plan')
            base.consumed(base.RECOGNITION, base.RECOGNITION_PIN, ('request.json','native-receipt.json','completion.json'), package, 'recognition-')
            for root, names, prefix in ((base.CAPTURE, ('files.json','context.json'), 'capture-'),
                (base.PREVIEW, ('files.json','request.json','batches.json'), 'preview-'),
                (PROBE, ('files.json','resource-assessment.json'), 'probe-')):
                for name in names: shutil.copyfile(root/name, package/(prefix+name))
            shutil.copytree(base.CAPTURE/'runtime', package/'baseline-runtime'); shutil.copytree(base.CAPTURE/'runtime', package/'runtime')
            shutil.copytree(base.CAPTURE/'content', package/'content')
            for name in base.HARNESS_FILES: shutil.copyfile(Path(d['harness'])/name, package/'runtime'/name)
            base.validate_runtime(inventory(package/'runtime'), captured, d['harnessFiles'], d['harnessFiles']['BalanceHarness.dll'])
            save(package/'tested-harness.json', d['harnessFiles']); save(package/'settings.json', read(base.CAPTURE/'context.json')['settings'])
            sources = [(args.declaration,'declaration.json'), (base.PLAN,'prospective-plan.json'),
                (Path(__file__),'prepare-resource.py'), (Path(base.__file__),'prepare-proposal-affinity-admission.py'),
                (Path(__file__).with_name('proposal-affinity-admission-context.ps1'),'context.ps1'),
                (Path(__file__).with_name('audit-proposal-affinity-study.py'),'auditor.py'),
                (ROOT/'build/run-proposal-affinity-study.py','run-proposal-affinity-study.py'),
                (ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),
                (Path(d['backendTests']['path']),'backend-tests.trx')]
            if pilot02:
                sources += [(Path(d['repairHandoff']['path']), 'repair-handoff.json'),
                    (Path(d['baselineBackendTests']['path']), 'baseline-backend-tests.trx'),
                    (FAILURE_REVIEW/'files.json', 'failed-study-closeout-files.json'),
                    (FAILURE_REVIEW/'verification.json', 'failed-study-closeout.json'),
                    (FAILURE_REVIEW/'failed-study-files.json', 'failed-study-files.json'),
                    (CONSUMED_ADMISSION/'files.json', 'consumed-admission-files.json'),
                    (CONSUMED_ADMISSION/'resource-forecast.json', 'consumed-resource-forecast.json')]
            if reuse_qualification:
                sources += [(QUALIFIED_REVIEW/'files.json', 'qualified-review-files.json'),
                    (QUALIFIED_REVIEW/'verification.json', 'qualified-review.json'),
                    (QUALIFIED_REVIEW/'failed-admission-files.json', 'qualified-attempt-files.json')]
            for path, name in sources: shutil.copyfile(path, package/name)
            (package/'verification').mkdir()
            for index, bound in enumerate([*d['verificationFiles'], *d['ownedFixtures'].values()]):
                shutil.copyfile(bound['path'], package/'verification'/f'{index:02d}-{Path(bound["path"]).name}')
            original = read(package/'preview-request.json')['context']; previous = read(package/'recognition-request.json')
            files, values = base.history(output.parent, previous)
            save(package/'history-files.json', files); save(package/'history.json', values)
            save(package/'probe-scenarios.json', base.probe_scenarios(read(package/'preview-batches.json'), original, values))
            host = Path(shutil.which('pwsh')).with_suffix('.dll')
            def context(mode):
                runtime = 'baseline-runtime' if mode == 'baseline' else 'runtime'
                command(mode, ['dotnet','exec','--runtimeconfig',str(package/runtime/'BalanceHarness.runtimeconfig.json'),
                    '--depsfile',str(host.with_suffix('.deps.json')),str(host),'-NoProfile','-File',str(package/'context.ps1'),
                    '-Package',str(package),'-RepositoryRoot',str(ROOT),'-Mode',mode])
            if reuse_qualification:
                copy_qualified_pilot02(package, files, values, d['harnessFiles']['BalanceHarness.dll'])
            else:
                context('baseline'); context('candidate')
            candidate = read(package/'candidate-qualification.json')
            base.validate_qualification(read(package/'baseline-qualification.json'), candidate, read(package/'baseline-projections.json'),
                read(package/'candidate-projections.json'), read(package/'capture-context.json'), original, d['harnessFiles']['BalanceHarness.dll'])
            save(package/'source-change-proof.json', dict(changed=source_changes(read(package/'compiled-source-files.json'), pilot02),
                previousManifestSha256=CONSUMED_ADMISSION_PIN if pilot02 else REVIEW_PIN, scientificImplementationUnchanged=True))
            qualified_context = base.derive_context(original, values, candidate['executionHash'])
            if pilot02: qualified_context['scope']['id'] = d['scopeId']
            save(package/'context.json', qualified_context); context('bind')
            base.validate_plan(read(package/'prospective-plan.json'), read(package/'plan.json'))
            save(package/'runtime.json', inventory(package/'runtime'))
            if pilot02:
                context('retention')
                report = read(package/'retention-qualification.json')
                require(report['runtimeManifestSha256'] == sha(package/'runtime.json')
                        and read(package/'retention-files.json') == read(package/'runtime.json'), 'Changed native retained manifest')
                validate_retention(report, read(package/'runtime.json'), inventory(package/'retention-check/executable'))
            def bound(name): return dict(path=str(package/name), sha256=sha(package/name))
            q = dict(version=owner.VERSION, resourceEnvelope=owner.RESOURCE_V2,
                **{k:bound(k+'.json') for k in ('plan','context','settings','history','runtime')}, auditor=bound('auditor.py'),
                contentRoot=str(package/'content'), registryRoot=str(output.parent), outputRoot=str(output), requiredHistory=files,
                pendingHistoryRecoveries=previous['pendingHistoryRecoveries'], recoveryReceiptHashes=previous['recoveryReceiptHashes'])
            save(package/'request.json', q)
            command('native-check', ['dotnet',str(package/'runtime/BalanceHarness.dll'),'tower-proposal-study-check',str(package/'request.json')])
            native = read(package/'native-check.log'); save(package/'native-check.json',native)
            require(native['status'] == 'InputsVerifiedNoReservation' and native['fights'] == native['newValues'] == 0
                    and native['historicalValues'] == len(values), 'Native check incomplete')
            require(sha(base.FIXTURE/'result/closeout.json') == base.FIXTURE_CLOSEOUT, 'Changed historical literal fixture')
            base.consumed(base.FIXTURE/'result', read(base.FIXTURE/'result/closeout.json')['filesHash'], ('completion.json',), package, 'fixture-')
            command('independent-legacy-fixture', [os.sys.executable,'-B','-X','utf8',str(package/'auditor.py'),
                str(base.FIXTURE/'result'),'--pin',base.FIXTURE_CLOSEOUT,'--output',str(package/'independent-legacy-fixture.json')])
            require(read(package/'independent-legacy-fixture.json')['status'] == 'Passed', 'Legacy independent verification failed')
            native_estimate = base.forecast(read(package/'recognition-native-receipt.json'), read(package/'recognition-completion.json'),
                read(package/'fixture-completion.json'), candidate, (package/'context.json').stat().st_size,
                owner.storage_bytes(package/'runtime'), owner.storage_bytes(package/'content'))
            save(package/'original-formula-forecast.json', native_estimate)
            previous_estimate, probe, historical_bytes = retained_forecast_inputs()
            if pilot02:
                previous_estimate = read(package/'consumed-resource-forecast.json')
            estimate = (pilot02_forecast if pilot02 else forecast)(previous_estimate, probe, historical_bytes, native_estimate)
            save(package/'resource-forecast.json', estimate); base.admit_forecast(estimate)
            require(base.history(output.parent,q) == (files,values), 'History changed during admission')
            require(prior_check() == prior, 'Prior accounting changed')
            validate_declaration(d); base.validate_tests(package/'backend-tests.trx', d['backendTestCount'])
            for path, name in sources: require(sha(path) == sha(package/name), 'Admission source changed during qualification')
            require(inventory(package/'runtime') == read(package/'runtime.json'), 'Retained runtime changed')
            require(inventory(package/'source') == read(package/'compiled-source-files.json'), 'Retained source changed')
            if pilot02:
                validate_retention(read(package/'retention-qualification.json'), read(package/'runtime.json'),
                    inventory(package/'retention-check/executable'))
            require(inventory(package/'content') == {n[8:]:p for n,p in captured.items() if n.startswith('content/')}, 'Content changed')
            check()
            receipt = dict(version=owner.VERSION, status='ProposalStudyAdmittedNoReservation', resourceEnvelope=owner.RESOURCE_V2,
                requestSha256=sha(package/'request.json'), originalPlanSha256=base.PLAN_PIN, qualifiedPlanSha256=sha(package/'plan.json'),
                executionHash=candidate['executionHash'], historicalValues=len(values), historyFiles=len(files),
                nativePreparations=2*candidate['nativePreparations'], fights=0, newValues=0, scientificLaunches=0,
                chargedSeconds=SECONDS, chargedBytes=BYTES, precedingChargedSeconds=prior['chargedSeconds'], precedingChargedBytes=prior['chargedBytes'],
                scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
                cumulativeMaximumSeconds=prior['chargedSeconds']+SECONDS+10800,
                cumulativeMaximumBytes=prior['chargedBytes']+BYTES+6442450944)
            if pilot02:
                receipt.update(scopeId=d['scopeId'], failedStudyCloseoutManifestSha256=FAILURE_REVIEW_PIN,
                    retentionQualificationSha256=sha(package/'retention-qualification.json'),
                    totalRecordedChargedSeconds=prior['chargedSeconds']+SECONDS,
                    totalRecordedChargedBytes=prior['chargedBytes']+BYTES)
            if reuse_qualification:
                receipt.update(nativePreparations=0, qualificationNativePreparations=144,
                    qualifiedAttemptReviewSha256=QUALIFIED_REVIEW_PIN, timingResamples=0)
            save(package/'admission.json', receipt)
            save(package/'completion.json', dict(status='AdmissionCompleteNoReservation', measuredSecondsBeforeSealing=time.monotonic()-started,
                retainedBytesBeforeSealing=owner.storage_bytes(package), chargedSeconds=SECONDS, chargedBytes=BYTES, fights=0,newValues=0,scientificLaunches=0))
            save(package/'files.json', inventory(package)); check()
            retained = module('resource_admitted_launcher',package/'run-proposal-affinity-study.py')
            retained.validate_admission(package/'request.json', package/'runtime/BalanceHarness.dll', sha(package/'files.json')); check()
            print(json.dumps(dict(status=receipt['status'], manifestSha256=sha(package/'files.json'), requestSha256=sha(package/'request.json'),
                measuredSecondsAfterSealing=time.monotonic()-started, retainedBytes=owner.storage_bytes(package), resourceForecast=estimate,
                fights=0,newValues=0,scientificLaunches=0),indent=2))
    except BaseException as error:
        if not (package/'failure.json').exists():
            save(package/'failure.json', dict(status='AdmissionFailedNoReservation',reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES,
                seconds=time.monotonic()-started,fights=0,newValues=0,scientificLaunches=0))
        raise
    finally: watchdog.cancel()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--declaration', required=True, type=Path); parser.add_argument('--pin', required=True)
    prepare(parser.parse_args())
