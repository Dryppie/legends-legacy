"""Bounded, seed-free admission for the frozen recognition plan. No launch, retries or combat entropy.

Copies captured gameplay dependencies and content, substitutes only the tested harness,
checks its producing symbols and native entry points, and refreshes the complete registry.
Admission consumes its full 600-second/512-MiB allowance even if it fails.
"""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import threading
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
CAPTURE = ROOT/'TestResults/adaptive-racing-pilot-02-admission-20260923'
CAPTURE_PIN = 'e931606aad5690e12774f5f33e2f50487d68ac802d6f21def93546973317d6a4'
FAILED_ADMISSION = ROOT/'TestResults/recognition-admission-20260923'
FAILED_PIN = '0b2955ec75ba1d0582e0a7862b1e45a5c27614050318d666d02ac4883e06a0b6'
CHARGE_PIN = '9d766ab1e3fdcdb32c372df293157956c62db3946b575c2b962ebe6ba69d9dc2'
REPAIRED_PACKAGE = ROOT/'TestResults/recognition-admission-repaired-20260923'
REPAIRED_OUTPUT = ROOT/'TestResults/balance/tower-frozen-pool-recognition-repaired-20260923'
PILOT = ROOT/'TestResults/balance/tower-adaptive-racing-pilot-02-20260923'
PILOT_PIN = 'f2327de7f9382f3a3ac3213a25cdb590e81e676ddd6e622fb91e3615ea8dd95a'
HISTORY_HELPER = ROOT/'TestResults/three-reference-admission-closeout-20260922/comparison-preparation.py'
HISTORY_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
VERSION = 'tower-frozen-pool-recognition-v1'
SECONDS, BYTES = 600, 536870912
HARNESS = ('BalanceHarness.dll','BalanceHarness.pdb','BalanceHarness.deps.json','BalanceHarness.runtimeconfig.json')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


audit_path = Path(__file__).with_name('audit-frozen-pool-recognition.py')
if not audit_path.exists(): audit_path = Path(__file__).with_name('auditor.py')
audit = module('recognition_admission_audit', audit_path)
require, read, sha = audit.require, audit.read, audit.sha
launcher_path = Path(__file__).with_name('run-frozen-pool-recognition.py')
if not launcher_path.exists(): launcher_path = ROOT/'build/run-frozen-pool-recognition.py'
launcher = module('recognition_admission_launcher', launcher_path)
save = launcher.write


def inventory(root):
    return {p.relative_to(root).as_posix():sha(p) for p in launcher.inventory(root)}


def authenticate(root, pin):
    require(sha(root/'files.json') == pin, 'Changed external manifest pin')
    audit.authenticate(root)
    return read(root/'files.json')


def history(registry, previous):
    require(sha(HISTORY_HELPER) == HISTORY_PIN, 'Changed historical recovery reader')
    return module('recognition_history',HISTORY_HELPER).history(registry,previous)


def definition(plan, values, context):
    return dict(version=VERSION,teams=[dict(role=t['stratum'],partyId=t['partyId'],referenceIds=[],scenario=t['scenario'])
        for r in plan['roots'] for t in r['teams']],contentHashes=plan['capturedRuntime']['contentHashes'],
        settingsHash=plan['capturedRuntime']['settingsHash'],executionHash=context['executionHash'],excludedCombatSeeds=values)


def request(package, output, files, previous):
    charge = package/'admission-charge.json'
    charges = [dict(scope='Frozen recognition admission allowance',seconds=SECONDS,bytes=BYTES,receiptPath=str(charge),receiptHash=sha(charge))]
    previous_charge = package/'preceding-admission-charge.json'
    if previous_charge.exists():
        charges.insert(0,dict(scope='Preserved failed capture-binding admission',seconds=SECONDS,bytes=BYTES,
            receiptPath=str(previous_charge),receiptHash=sha(previous_charge)))
    prior_seconds, prior_bytes = len(charges)*SECONDS,len(charges)*BYTES
    return dict(version=VERSION,contentRoot=str(package/'content'),definitionPath=str(package/'definition.json'),
        definitionHash=sha(package/'definition.json'),registryRoot=str(output.parent),outputRoot=str(output),requiredHistory=files,
        maximumSeconds=prior_seconds+7200,maximumBytes=prior_bytes+3758096384,phases={},priorSeconds=prior_seconds,priorBytes=prior_bytes,
        priorCharges=charges,
        pendingHistoryRecoveries=previous['pendingHistoryRecoveries'],recoveryReceiptHashes=previous['recoveryReceiptHashes'],
        recognition=dict(planPath=str(package/'plan.json'),auditorPath=str(package/'auditor.py'),auditorHash=sha(package/'auditor.py')))


def forecast(native, completion):
    require(native['fights'] == 23680 and native['status'] == 'Verified' and completion['status'] == 'Complete', 'Invalid sizing source')
    scale = 27648/23680*1.5
    seconds, size = native['measuredSeconds']*scale, native['observedBytes']*scale
    audit_seconds = (completion['seconds']-native['measuredSeconds'])*scale
    require(0 < seconds < 6000 and 0 < size < 3221225472 and 0 < audit_seconds < 1200, 'Sizing does not fit fixed caps')
    return dict(sourceFights=23680,targetFights=27648,scalingMargin=1.5,nativeSeconds=seconds,nativeBytes=size,
        auditSeconds=audit_seconds,fits=True,guaranteedUpperBound=False,
        limitation='Historical scaling is a planning estimate; fixed caps and incomplete-evidence failure remain binding.')


def validate_tests(path):
    ns = {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    tree = ET.parse(path); results = tree.findall('.//t:UnitTestResult',ns)
    require(results and all(r.attrib['outcome'] == 'Passed' for r in results), 'Backend verification failed')
    names = [r.attrib['testName'] for r in results]
    require(any('Full_27648_report_archive' in n for n in names) and any('Root_panels_and_recipes' in n for n in names), 'Missing diagnostic verification')


def validate_runtime(actual, captured, tested):
    expected = {n[8:]:p for n,p in captured.items() if n.startswith('runtime/')}
    expected.update(tested)
    require(actual == expected and set(tested) == set(HARNESS), 'Changed captured dependencies or tested harness')


def process_ok(value):
    require(value['mechanism'] == 'suspended-owned-job-v1' and value['exitCode'] == 0 and not value['timedOut']
        and value['activeProcesses'] == 0 and value['totalProcesses'] >= 1, 'Incomplete owned admission process')


def prepare(args):
    package, output = args.package.resolve(), args.output.resolve()
    require(os.name == 'nt' and not package.exists() and not output.exists() and output.parent.is_dir(), 'Require new Windows admission/output paths')
    require(not package.is_relative_to(output.parent), 'Keep admission artifacts outside the scientific registry')
    require(args.repair_capture_binding and package == REPAIRED_PACKAGE and output == REPAIRED_OUTPUT,
        'Use the explicitly declared capture-binding repair and distinct admission/scientific identities')
    require(set(p.name for p in FAILED_ADMISSION.iterdir()) == {'failure.json','admission-charge.json'}
        and sha(FAILED_ADMISSION/'failure.json') == FAILED_PIN and sha(FAILED_ADMISSION/'admission-charge.json') == CHARGE_PIN,
        'Changed preceding failed admission or charge')
    started = time.monotonic(); package.mkdir()
    watchdog = threading.Timer(SECONDS,lambda:os._exit(124)); watchdog.daemon=True; watchdog.start()
    def check():
        require(time.monotonic()-started < SECONDS-3 and not output.exists(), 'Admission deadline or unexpected scientific output')
        require(launcher.storage_bytes(package) < BYTES-1048576, 'Admission storage exceeded')
    owner = module('recognition_admission_owner',ROOT/'build/bounded_windows_process.py')
    def command(name, command):
        receipt = owner.run(command,str(ROOT),str(package/(name+'.log')),started+SECONDS-5,cleanup_seconds=1,check=check)
        save(package/(name+'-process.json'),receipt); process_ok(receipt); check()
    try:
        save(package/'admission-charge.json',dict(version=VERSION,chargedSeconds=SECONDS,chargedBytes=BYTES,
            treatment='Full admission allowance charged at start, including failure. No refund or transfer. Historical engineering totals are separate and incomplete.'))
        shutil.copyfile(FAILED_ADMISSION/'failure.json',package/'preceding-admission-failure.json')
        shutil.copyfile(FAILED_ADMISSION/'admission-charge.json',package/'preceding-admission-charge.json')
        save(package/'repair-declaration.json',dict(version=VERSION,repair='Bind successful pilot admission as captured runtime source',
            precedingFailureSha256=FAILED_PIN,precedingChargeSha256=CHARGE_PIN,admissionCharges=2,
            cumulativeMaximumSeconds=8400,cumulativeMaximumBytes=4831838208,remainingScientificSeconds=7200,
            remainingScientificBytes=3758096384,scientificRetries=0,scientificLaunches=0))
        with launcher.writer_lease(output.parent/'complete-family-allocation'),launcher.writer_lease(output):
            captured = authenticate(CAPTURE,CAPTURE_PIN); check()
            # The full prior pilot was already independently closed. Authenticate the specific consumed source files against its external manifest.
            require(sha(PILOT/'files.json') == PILOT_PIN, 'Changed pilot manifest')
            pilot_files = read(PILOT/'files.json')
            for name in ('request.json','native-receipt.json','completion.json'):
                require(sha(PILOT/name) == pilot_files[name], 'Changed sizing/history source')
                shutil.copyfile(PILOT/name,package/('pilot-'+name))
            shutil.copyfile(PILOT/'files.json',package/'pilot-files.json'); shutil.copyfile(CAPTURE/'files.json',package/'capture-files.json')
            validate_tests(args.tests); shutil.copyfile(args.tests,package/'backend-tests.trx')
            require(sha(ROOT/'Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json') == audit.PLAN, 'Changed frozen plan')
            shutil.copytree(CAPTURE/'runtime',package/'runtime'); shutil.copytree(CAPTURE/'content',package/'content')
            tested = {name:sha(args.harness/name) for name in HARNESS}
            owned = read(args.owned_fixture/'verification.json')
            require(owned['status'] == 'OwnedRecognitionFixturePassed' and owned['literalReports'] == 27648
                and owned['actualCombat'] == owned['productionEntropyDraws'] == 0 and owned['harnessSha256'] == tested['BalanceHarness.dll'],
                'Missing full owned-workflow verification for this harness')
            shutil.copyfile(args.owned_fixture/'verification.json',package/'owned-fixture-verification.json')
            for name in HARNESS: shutil.copyfile(args.harness/name,package/'runtime'/name)
            save(package/'tested-harness.json',tested); validate_runtime(inventory(package/'runtime'),captured,tested)
            # A complete direct-report fixture is independently audited before retaining its compact compatibility projections.
            require(audit.audit(args.fixture/'result')['status'] == 'Passed','Literal fixture failed')
            fixture = package/'literal-fixture'; fixture.mkdir()
            for source,target in [('study/study.json','stored-study.json'),('provisional-result.json','stored-result.json'),
                                  ('confirmation-binding.json','stored-binding.json'),('chunks.json','stored-chunks.json'),('entropy.bin','literal-entropy.bin')]:
                shutil.copyfile(args.fixture/'result'/source,fixture/target)
            for source,target in [(ROOT/'Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json','plan.json'),
                (Path(__file__).with_name('frozen-pool-recognition-context.ps1'),'context.ps1'),
                (Path(__file__).with_name('audit-frozen-pool-recognition.py'),'auditor.py'),
                (ROOT/'build/run-frozen-pool-recognition.py','run-frozen-pool-recognition.py'),
                (ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),(Path(__file__),'prepare.py')]:
                shutil.copyfile(source,package/target)
            host = Path(shutil.which('pwsh')).with_suffix('.dll')
            command('context',['dotnet','exec','--runtimeconfig',str(package/'runtime/BalanceHarness.runtimeconfig.json'),
                '--depsfile',str(host.with_suffix('.deps.json')),str(host),'-NoProfile','-File',str(package/'context.ps1'),
                '-Package',str(package),'-RepositoryRoot',str(ROOT)])
            previous = read(package/'pilot-request.json'); files, values = history(output.parent,previous)
            context = read(package/'context.json'); plan = read(package/'plan.json')
            save(package/'history-files.json',files); save(package/'definition.json',definition(plan,values,context))
            save(package/'request.json',request(package,output,files,previous)); check()
            command('native-check',['dotnet',str(package/'runtime/BalanceHarness.dll'),'tower-frozen-pool-recognition-check',str(package/'request.json')])
            save(package/'native-check.json',read(package/'native-check.log'))
            require(history(output.parent,read(package/'request.json')) == (files,values), 'History changed during admission')
            save(package/'runtime-files.json',inventory(package/'runtime'))
            save(package/'resource-forecast.json',forecast(read(package/'pilot-native-receipt.json'),read(package/'pilot-completion.json')))
            result = verify_contents(package); save(package/'admission.json',result); check()
            save(package/'completion.json',dict(status='AdmissionCompleteNoReservation',seconds=time.monotonic()-started,
                chargedSeconds=SECONDS,chargedBytes=BYTES,scientificLaunches=0,newValues=0,fights=0,retries=0))
            save(package/'files.json',inventory(package)); check()
            print(json.dumps(dict(status=result['status'],manifestSha256=sha(package/'files.json'),requestSha256=result['requestSha256'],
                measuredSeconds=time.monotonic()-started,retainedBytes=launcher.storage_bytes(package),fights=0,newValues=0),indent=2))
    except BaseException as error:
        if not (package/'failure.json').exists(): save(package/'failure.json',dict(status='AdmissionFailedNoReservation',reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES))
        raise
    finally:
        watchdog.cancel()


def verify_contents(package):
    require(not (package/'failure.json').exists() and sha(package/'plan.json') == audit.PLAN, 'Failed admission or changed plan')
    require(sha(package/'capture-files.json') == CAPTURE_PIN and sha(package/'pilot-files.json') == PILOT_PIN, 'Changed sources')
    require(sha(package/'preceding-admission-failure.json') == FAILED_PIN and sha(package/'preceding-admission-charge.json') == CHARGE_PIN,
        'Discarded failed admission accounting')
    captured, pilot = read(package/'capture-files.json'),read(package/'pilot-files.json')
    for name in ('request.json','native-receipt.json','completion.json'): require(sha(package/('pilot-'+name)) == pilot[name], 'Changed pilot input')
    validate_tests(package/'backend-tests.trx')
    validate_runtime(inventory(package/'runtime'),captured,read(package/'tested-harness.json'))
    owned = read(package/'owned-fixture-verification.json')
    require(owned['status'] == 'OwnedRecognitionFixturePassed' and owned['literalReports'] == 27648
        and owned['actualCombat'] == owned['productionEntropyDraws'] == 0
        and owned['harnessSha256'] == read(package/'tested-harness.json')['BalanceHarness.dll']
        and all(owned[k] is True for k in ('nativeWorker','nativeReconstruction','independentPythonAudit','nativePublicationBarrier','nativePostPublicationVerification')),
        'Changed owned workflow proof')
    require(inventory(package/'runtime') == read(package/'runtime-files.json'),'Changed retained runtime')
    require(inventory(package/'content') == {n[8:]:p for n,p in captured.items() if n.startswith('content/')},'Changed captured content')
    context = read(package/'context.json'); d = read(package/'definition.json'); q = read(package/'request.json')
    require(context['status'] == 'CapturedRecognitionRuntimeCompatible' and context['fights'] == context['newValues'] == context['nativePreparations'] == 0,
        'Missing captured compatibility')
    require(context['literalFixture'] == dict(status='StoredEvidenceReconstructed',version=VERSION,teams=108,contrasts=216,
        unmeasured=132,strata=36,samples=256,literalWords=6144,literalPanel=3072,transports=108,inputProjections=216),'Changed literal compatibility')
    sources = read(package/'compiled-source-files.json')
    require(len(sources) == context['compiledSourceDocuments'] and sources and all(sha(package/'source'/n) == p for n,p in sources.items()),'Changed producing sources')
    for name in ('RunRecognition','AuditRecognition','RecognitionPublicationCheck','VerifyRecognition','AssessRecognition','Reserve','Execute','VerifyStudy'):
        require(any('TowerFixedFamilyConfirmation.'+name+'#' in m for m in context['jitResolvedMethods']),'Unresolved native entry path')
    require(q == request(package,Path(q['outputRoot']),read(package/'history-files.json'),read(package/'pilot-request.json'))
        and d == definition(read(package/'plan.json'),d['excludedCombatSeeds'],context),'Changed request or definition')
    require(Path(q['outputRoot']) == REPAIRED_OUTPUT and q['priorSeconds'] == 1200 and q['priorBytes'] == 1073741824,
        'Changed repaired study identity or lost failed admission charge')
    require(read(package/'repair-declaration.json') == dict(version=VERSION,repair='Bind successful pilot admission as captured runtime source',
        precedingFailureSha256=FAILED_PIN,precedingChargeSha256=CHARGE_PIN,admissionCharges=2,cumulativeMaximumSeconds=8400,
        cumulativeMaximumBytes=4831838208,remainingScientificSeconds=7200,remainingScientificBytes=3758096384,scientificRetries=0,scientificLaunches=0),
        'Changed prospective repair declaration')
    require(all(read(package/'runtime-files.json').get(k+'.dll') == v for k,v in context['execution']['assemblyHashes'].items()),'Changed execution identity')
    require(read(package/'native-check.json') == dict(status='ContractValidNoReservation',version=VERSION,requestHash=audit.digest(q),
        historicalValues=len(d['excludedCombatSeeds']),maximumFights=27648,maximumNewReservations=6144,transportBindings=108,
        resourceFeasibilityEstablished=False,newValues=0,fights=0),'Changed native admission')
    for name in ('context','native-check'): process_ok(read(package/(name+'-process.json')))
    require(read(package/'resource-forecast.json') == forecast(read(package/'pilot-native-receipt.json'),read(package/'pilot-completion.json')),'Changed sizing')
    return dict(status='RecognitionAdmittedNoReservation',version=VERSION,requestSha256=sha(package/'request.json'),
        definitionSha256=sha(package/'definition.json'),executionHash=context['executionHash'],historicalValues=len(d['excludedCombatSeeds']),
        historyFiles=len(q['requiredHistory']),maximumFights=27648,maximumNewReservations=6144,newValues=0,fights=0,
        admissionChargeSeconds=SECONDS,admissionChargeBytes=BYTES,cumulativeAdmissionSeconds=q['priorSeconds'],
        cumulativeAdmissionBytes=q['priorBytes'],maximumSeconds=q['maximumSeconds'],maximumBytes=q['maximumBytes'],remainingSeconds=7200,remainingBytes=3758096384)


def verify(package,pin,live=False):
    authenticate(package,pin); result = verify_contents(package)
    require(result == read(package/'admission.json'),'Changed admission conclusions')
    completion = read(package/'completion.json')
    require(completion['status'] == 'AdmissionCompleteNoReservation' and 0 <= completion['seconds'] < SECONDS
        and completion['chargedSeconds'] == SECONDS and completion['chargedBytes'] == BYTES
        and completion['scientificLaunches'] == completion['newValues'] == completion['fights'] == completion['retries'] == 0
        and launcher.storage_bytes(package) < BYTES,'Changed admission accounting')
    if live:
        q = read(package/'request.json')
        require(not Path(q['outputRoot']).exists() and history(Path(q['registryRoot']),q) ==
            (q['requiredHistory'],read(package/'definition.json')['excludedCombatSeeds']),'Changed live history or existing output')
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('command',choices=['prepare','verify'])
    parser.add_argument('--package',type=Path,required=True); parser.add_argument('--manifest-sha256'); parser.add_argument('--live',action='store_true')
    parser.add_argument('--output',type=Path); parser.add_argument('--harness',type=Path); parser.add_argument('--tests',type=Path); parser.add_argument('--fixture',type=Path)
    parser.add_argument('--owned-fixture',type=Path)
    parser.add_argument('--repair-capture-binding',action='store_true')
    args = parser.parse_args()
    if args.command == 'prepare':
        require(all((args.output,args.harness,args.tests,args.fixture,args.owned_fixture)),'Supply output, tested harness directory, tests TRX, literal fixture and owned workflow fixture')
        prepare(args)
    else:
        require(args.manifest_sha256 is not None,'Supply external admission manifest pin')
        print(json.dumps(verify(args.package.resolve(),args.manifest_sha256,args.live),indent=2))
