"""One 600-second /512-MiB captured-runtime admission; no scientific allocation or combat."""
import argparse
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-practical-three-reference-confirmation-v1'
PACKAGE = ROOT/'TestResults/three-reference-confirmation-admission-20260923'
RUN = ROOT/'TestResults/balance/tower-three-reference-confirmation-20260923'
CAPTURE = ROOT/'TestResults/three-reference-tie-admission-20260923'
IMPLEMENTATION = ROOT/'TestResults/three-reference-confirmation-implementation-20260923'
PREVIOUS_RUN = ROOT/'TestResults/balance/tower-three-reference-tie-comparison-20260923'
CAPTURE_PIN = '90b9a5a9815454ba1169d64f61cc8b7a950597cfa56bc1c9380c53155538f1c5'
IMPLEMENTATION_PIN = 'da5614e23848942698e390daa75ab51e13bbb554b03aee5af72f0f9585108bf3'
PREVIOUS_RUN_PIN = '68f7468ba270f1d51a075772a6fed3d800ad627a1376a00c4829c10ef9d806d1'
PLAN_PIN = 'c6544a63a569de5197c4e2a35749c01a14388478e6a9986509d991c64bd6a74f'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
OWNER_PIN = '0efe39bcabf3ac3665988a9c38185938b77f97585ecf4d7dc22b4a6deb691d30'
SETTINGS = 'f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74'
TEAMS = '3ec3b0e93a491ef4837babbf5d8bf6f98745369c53e9be4a6e21f430a590ef32'
SECONDS, BYTES = 600, 536870912
HISTORY_VALUES, HISTORY_FILES = 633313, 240
HARNESS_FILES = ('BalanceHarness.dll','BalanceHarness.pdb','BalanceHarness.deps.json','BalanceHarness.runtimeconfig.json')
PROOF_FILES = ('implementation.json','source-hashes.json','backend-closeout.trx','backend-latest-runtime.trx',
               'python-verified-runtime.log','history-verification.json')
INPUTS = {'Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json':'plan.json',
          'Balance Harness/analysis/audit-three-reference-confirmation.py':'auditor.py',
          'build/run-three-reference-confirmation.py':'run-three-reference-confirmation.py'}
FIXTURES = {'study/study.json':'stored-study.json','provisional-result.json':'stored-result.json',
            'confirmation-binding.json':'stored-binding.json','chunks.json':'stored-chunks.json','entropy.bin':'literal-entropy.bin'}

def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path); value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value

common_path = Path(__file__).with_name('comparison-preparation.py')
if not common_path.exists(): common_path=ROOT/'TestResults/reference-exploration-comparison-admission-20260922/comparison-preparation.py'
assert hashlib.sha256(common_path.read_bytes()).hexdigest() == COMMON_PIN
common=module('confirmation_admission_history',common_path)
require,read,sha,save=common.require,common.read,common.sha,common.save
evidence_path=Path(__file__).with_name('confirmation-evidence-verification.py')
if not evidence_path.exists(): evidence_path=ROOT/'Balance Harness/analysis/confirmation-evidence-verification.py'
evidence=module('confirmation_admission_evidence',evidence_path)

def canonical(value):
    text=json.dumps(value,sort_keys=True,separators=(',',':'),ensure_ascii=True)
    for c in ['+','<','>','&',"'"]: text=text.replace(c,'\\u%04X'%ord(c))
    return hashlib.sha256(text.encode()).hexdigest()

def validate_implementation(value):
    require(value['status']=='ImplementationVerifiedCapturedRuntimeAdmissionRequired' and value['version']==VERSION
        and value['backendDistinctPassingTests']==81 and value['pythonPassingTests']==18
        and value['scientificFights']==value['authoritativeAllocations']==0 and not value['capturedRuntimeAdmitted']
        and not value['runnableScientificRequest'] and value['oldEngineeringDisclosure']==dict(seconds=18180,MiB=13584,completeHistoricalEngineeringTotalsKnown=False),
        'Changed implementation proof or accepted engineering disclosure')
    require(set(value['testedHarness'])==set(HARNESS_FILES),'Incomplete tested binaries')

def validate_runtime(runtime,captured,tested):
    expected={name[8:]:pin for name,pin in captured.items() if name.startswith('runtime/')}
    require(runtime.keys()==expected.keys() and set(tested)==set(HARNESS_FILES),'Changed runtime membership')
    for name,pin in runtime.items(): require(pin==(tested[name] if name in tested else expected[name]),'Changed tested harness or retained dependency: '+name)

def definition_from(plan,values,context):
    require(plan['proposedNativeVersion']==VERSION and context['settingsHash']==plan['settingsHash']==SETTINGS,'Changed protocol/settings')
    require(values==sorted(set(values)) and 0<len(values)<=987000 and all(type(v) is int and -2**31<=v<2**31 for v in values),'Invalid complete history')
    teams=[dict(role=t['role'],partyId=t['partyId'],referenceIds=[t['referenceId']] if 'referenceId' in t else [],scenario=copy.deepcopy(t['scenario']))
           for t in plan['recipesInFixedExecutionOrder']]
    require(canonical(teams)==TEAMS,'Changed exact ordered seed-free family')
    return dict(version=VERSION,teams=teams,contentHashes=plan['contentHashes'],settingsHash=SETTINGS,
                executionHash=context['executionHash'],excludedCombatSeeds=values)

def request_from(package,history,previous):
    charge=package/'admission-charge.json'
    return dict(version=VERSION,contentRoot=str(package/'content'),definitionPath=str(package/'definition.json'),
        definitionHash=sha(package/'definition.json'),registryRoot=str(RUN.parent),outputRoot=str(RUN),requiredHistory=history,
        maximumSeconds=7800,maximumBytes=4294967296,phases={},priorSeconds=SECONDS,priorBytes=BYTES,
        priorCharges=[dict(scope='Three-reference confirmation captured-runtime admission; full allowance charged',seconds=SECONDS,bytes=BYTES,
                          receiptPath=str(charge),receiptHash=sha(charge))],
        pendingHistoryRecoveries=previous['pendingHistoryRecoveries'],recoveryReceiptHashes=previous['recoveryReceiptHashes'],
        threeReference=dict(planPath=str(package/'plan.json'),auditorPath=str(package/'auditor.py'),auditorHash=sha(package/'auditor.py')))

def forecast(plan,implementation):
    resource=plan['resources']; size=resource['sizing']; fixture=implementation['literalFixtureObservation']
    require(size['sourceFights']==55672 and size['sourceOwnerSeconds']>0 and size['sourceOwnerBytes']>0,'Invalid historical cost source')
    factor=52000/55672
    require(abs(size['ownerSeconds']-size['sourceOwnerSeconds']*factor)<1e-8,'Changed frozen forecast')
    require(fixture['actualCombat']==fixture['productionEntropyDraws']==0 and fixture['literalReports']==52000,'Invalid engineering observation')
    fits=2*size['ownerSeconds']<7200 and 2*size['ownerBytes']<3758096384 and 2*size['nativeSeconds']<6000 and 2*size['auditSeconds']<1200
    return dict(status='FrozenHistoricalSizingWithLiteralFixtureObservation',targetFights=52000,fits=fits,guaranteedUpperBound=False,
        doubledOwnerSeconds=2*size['ownerSeconds'],doubledOwnerBytes=2*size['ownerBytes'],doubledNativeSeconds=2*size['nativeSeconds'],
        doubledAuditSeconds=2*size['auditSeconds'],literalFixtureSeconds=fixture['seconds'],literalFixtureBytes=fixture['bytes'],
        fullWorkflowFeasibilityEstablished=False,caveat='Literal overhead and historical scaling are not combat forecasts or completion guarantees; hard caps bind.')

def validate_process(value):
    require(value['exitCode']==0 and not value['timedOut'] and value['activeProcesses']==0 and value['totalProcesses']>=1
        and value['mechanism']=='suspended-owned-job-v1','Incomplete owned process')

def terminal_receipt(value):
    """Count the exact newline-terminated bytes emitted by the pinned save helper."""
    value=dict(value,terminalBytes=0)
    for _ in range(16):
        size=len((json.dumps(value,indent=2,allow_nan=False)+'\n').encode('utf-8'))
        if value['terminalBytes']==size: return value
        value['terminalBytes']=size
    raise ValueError('Terminal byte accounting did not converge')

def context_command(package,pwsh_executable):
    """Use the harness's shared frameworks; standalone PowerShell omits ASP.NET."""
    require(pwsh_executable is not None,'PowerShell host is unavailable')
    host=Path(pwsh_executable).resolve().with_suffix('.dll'); deps=host.with_suffix('.deps.json')
    require(host.is_file() and deps.is_file(),'PowerShell managed host/dependencies are unavailable')
    return ['dotnet','exec','--runtimeconfig',str(package/'runtime/BalanceHarness.runtimeconfig.json'),
        '--depsfile',str(deps),str(host),'-NoProfile','-File',str(package/'context.ps1'),
        '-Package',str(package),'-RepositoryRoot',str(ROOT),'-Implementation',str(IMPLEMENTATION)]

def verify_contents(package):
    require(not RUN.exists() and not (package/'failure.json').exists(),'Scientific output or failed admission exists')
    proof=read(package/'verification/files.json'); captured=read(package/'capture-files.json')
    require(sha(package/'verification/files.json')==IMPLEMENTATION_PIN and sha(package/'capture-files.json')==CAPTURE_PIN
        and sha(package/'previous-run-files.json')==PREVIOUS_RUN_PIN and sha(package/'plan.json')==PLAN_PIN,'Changed provenance')
    for name in PROOF_FILES: require(sha(package/'verification'/name)==proof[name],'Changed implementation receipt: '+name)
    impl=read(package/'verification/implementation.json'); validate_implementation(impl)
    runtime=common.inventory(package/'runtime'); validate_runtime(runtime,captured,impl['testedHarness'])
    require(runtime==read(package/'runtime-files.json'),'Changed copied runtime')
    require(common.inventory(package/'content')=={n[8:]:p for n,p in captured.items() if n.startswith('content/')},'Changed retained content')
    sources=read(package/'verification/source-hashes.json')
    compiled=read(package/'compiled-source-files.json'); context=read(package/'context.json')
    require(len(compiled)==context['compiledSourceDocuments']==205,'Incomplete producing symbols')
    prefix=IMPLEMENTATION.relative_to(ROOT).as_posix()+'/'
    for name,pin in compiled.items():
        require(sha(package/'source'/name)==pin,'Changed source snapshot')
        expected=sources.get(name) if name.startswith('LL/tools/BalanceHarness/') else proof.get(name[len(prefix):]) if name.startswith(prefix) else None
        require(expected==pin,'Source not bound to tested implementation')
    require(context['status']=='CapturedConfirmationRuntimeCompatible' and context['settingsHash']==SETTINGS
        and context['nativePreparations']==context['fights']==context['newValues']==0
        and all(runtime.get(k+'.dll')==v for k,v in context['execution']['assemblyHashes'].items()),'Changed native context')
    for method in ('CheckThreeReference','RunThreeReference','AuditThreeReference','VerifyThreeReference','PublicationCheck',
                   'VerifyStudy','Assess','Reserve','Classify','Chunks','Execute','RequireOwnerLeases'):
        require(any('TowerFixedFamilyConfirmation.'+method+'#' in m for m in context['jitResolvedMethods']),'Unresolved entry path: '+method)
    require(context['literalFixture']==dict(status='StoredEvidenceReconstructed',version=VERSION,teams=8,contrasts=15,qualifiers=2,recommendations=5,
        samples=6500,literalWords=13000,literalPanel=6500,transports=56,inputProjections=112),'Changed captured literal fixture')
    projections=read(package/'input-projections.json')
    require(len(projections)==112 and [(p['teamOrdinal'],p['sliceOrdinal']) for p in projections]==[(t,s) for t in range(8) for s in range(7) for _ in range(2)],'Incomplete transport projections')
    for source,target in INPUTS.items(): require(sha(package/target)==sources[source],'Untested protocol input: '+source)
    for source,target in FIXTURES.items(): require(sha(package/'literal-fixture'/target)==proof['verified-literal-fixture/result/'+source],'Changed literal fixture')
    require(sha(package/'comparison-preparation.py')==COMMON_PIN and sha(package/'bounded_windows_process.py')==OWNER_PIN,'Changed history/owner helper')
    helpers=read(package/'admission-helpers.json')
    require(set(helpers)=={'prepare.py','context.ps1','comparison-preparation.py','bounded_windows_process.py','confirmation-evidence-verification.py'}
        and all(sha(package/n)==p for n,p in helpers.items()),'Changed admission helpers')
    history=read(package/'history-files.json'); d=read(package/'definition.json'); q=read(package/'request.json')
    require(history==read(package/'history-after.json')==read(package/'verification/history-verification.json')['files']
        and len(history)==HISTORY_FILES and len(d['excludedCombatSeeds'])==HISTORY_VALUES,'Changed complete history')
    require(sha(package/'previous-request.json')==read(package/'previous-run-files.json')['request.json'],'Changed historical request')
    require(q==request_from(package,history,read(package/'previous-request.json'))
        and d==definition_from(read(package/'plan.json'),d['excludedCombatSeeds'],context),'Changed bound request/definition')
    charge=read(package/'admission-charge.json')
    require(charge==dict(version=VERSION,scope='Captured-runtime admission allowance',chargedSeconds=SECONDS,chargedBytes=BYTES,
        treatment='Full allowance charged at admission start, including failed admission; no refund or transfer.',
        priorEngineeringLedger=dict(seconds=18180,MiB=13584,completeHistoricalEngineeringTotalsKnown=False)),'Changed charge receipt')
    require(read(package/'native-check.json')==dict(status='ContractValidNoReservation',version=VERSION,requestHash=canonical(q),historicalValues=HISTORY_VALUES,
        maximumFights=52000,maximumNewReservations=13000,transportBindings=56,resourceFeasibilityEstablished=False,newValues=0,fights=0),'Changed native admission result')
    for name in ('context-process.json','native-check-process.json'): validate_process(read(package/name))
    estimate=forecast(read(package/'plan.json'),impl)
    require(estimate==read(package/'resource-forecast.json') and estimate['fits'],'Resource sizing does not fit')
    require(not any(p.name in common.LEDGERS|{'entropy.bin','entropy-start.json','entropy-intent.json','attempts.jsonl'} for p in common.paths(package)),
        'Premature scientific reservation or attempt')
    return dict(status='ThreeReferenceConfirmationAdmittedNoReservation',version=VERSION,requestSha256=sha(package/'request.json'),
        definitionSha256=sha(package/'definition.json'),executionHash=context['executionHash'],settingsHash=SETTINGS,
        compiledSourceDocuments=205,jitResolvedMethods=len(context['jitResolvedMethods']),nativeInputProjections=112,transportBindings=56,nativeTeamsPrepared=8,
        historicalValues=HISTORY_VALUES,historyFiles=HISTORY_FILES,maximumScientificFights=52000,maximumNewReservations=13000,
        admissionChargeSeconds=SECONDS,admissionChargeBytes=BYTES,remainingSeconds=7200,remainingBytes=3758096384,
        resourceForecast=estimate,scientificLaunches=0,newValues=0,fights=0,outputExists=False)

def worker():
    require(PACKAGE.exists() and not RUN.exists() and not (PACKAGE/'runtime').exists(),'Existing admission work; no retry or resume')
    deadline=read(PACKAGE/'admission-launch.json')['monotonicDeadline']
    def check():
        require(time.monotonic()<deadline and not RUN.exists(),'Admission deadline or unexpected scientific output')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE))<BYTES-1048576,'Admission storage ceiling')
    def command(name,args):
        owner=module('confirmation_nested_owner',PACKAGE/'bounded_windows_process.py')
        process=owner.run(args,str(ROOT),str(PACKAGE/(name+'.log')),deadline-1,cleanup_seconds=1,check=check)
        save(PACKAGE/(name+'-process.json'),process); validate_process(process); print(name+' passed',flush=True)
    for path,pin in ((CAPTURE,CAPTURE_PIN),(IMPLEMENTATION,IMPLEMENTATION_PIN),(PREVIOUS_RUN,PREVIOUS_RUN_PIN)):
        evidence.authenticate(path,pin,check=check); check(); print(path.name+' authenticated',flush=True)
    impl=read(IMPLEMENTATION/'implementation.json'); validate_implementation(impl)
    sources=read(IMPLEMENTATION/'source-hashes.json')
    for source in INPUTS: require(sha(ROOT/source)==sources[source],'Changed tested input: '+source)
    captured=read(CAPTURE/'files.json')
    shutil.copytree(CAPTURE/'runtime',PACKAGE/'runtime'); shutil.copytree(CAPTURE/'content',PACKAGE/'content')
    for name in HARNESS_FILES: shutil.copyfile(IMPLEMENTATION/'tested-harness'/name,PACKAGE/'runtime'/name)
    copies=[(CAPTURE/'files.json','capture-files.json'),(IMPLEMENTATION/'files.json','verification/files.json'),
            (PREVIOUS_RUN/'files.json','previous-run-files.json'),(PREVIOUS_RUN/'request.json','previous-request.json')]
    copies += [(IMPLEMENTATION/name,'verification/'+name) for name in PROOF_FILES]
    copies += [(ROOT/name,target) for name,target in INPUTS.items()]
    copies += [(IMPLEMENTATION/'verified-literal-fixture/result'/name,'literal-fixture/'+target) for name,target in FIXTURES.items()]
    for source,target in copies:
        dest=PACKAGE/target; dest.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(source,dest); check()
    validate_runtime(common.inventory(PACKAGE/'runtime'),captured,impl['testedHarness'])
    command('context',context_command(PACKAGE,shutil.which('pwsh')))
    save(PACKAGE/'runtime-files.json',common.inventory(PACKAGE/'runtime'))
    previous=read(PACKAGE/'previous-request.json'); files,values=common.history(RUN.parent,previous)
    require((len(values),len(files))==(HISTORY_VALUES,HISTORY_FILES) and files==read(IMPLEMENTATION/'history-verification.json')['files'],'Changed authoritative history')
    save(PACKAGE/'history-files.json',files); print('Complete history frozen',flush=True)
    save(PACKAGE/'definition.json',definition_from(read(PACKAGE/'plan.json'),values,read(PACKAGE/'context.json')))
    save(PACKAGE/'request.json',request_from(PACKAGE,files,previous)); check()
    command('native-check',['dotnet',str(PACKAGE/'runtime/BalanceHarness.dll'),'tower-three-reference-confirmation-check',str(PACKAGE/'request.json')])
    save(PACKAGE/'native-check.json',read(PACKAGE/'native-check.log'))
    after,current=common.history(RUN.parent,read(PACKAGE/'request.json'))
    require(after==files and current==values,'History changed during admission')
    save(PACKAGE/'history-after.json',after); save(PACKAGE/'resource-forecast.json',forecast(read(PACKAGE/'plan.json'),impl))
    check(); save(PACKAGE/'worker-completion.json',dict(status='ReadyForAdmissionAudit',newValues=0,fights=0))

def prepare():
    require(os.name=='nt' and not PACKAGE.exists() and not PACKAGE.with_name(PACKAGE.name+'-pin.json').exists()
        and not RUN.exists(),'New Windows admission package and absent scientific output required; no retry or resume')
    owner_path=ROOT/'build/bounded_windows_process.py'; launcher_path=ROOT/'build/run-three-reference-confirmation.py'
    require(sha(owner_path)==OWNER_PIN and sha(launcher_path)==read(IMPLEMENTATION/'source-hashes.json')['build/run-three-reference-confirmation.py'],'Changed process owner')
    started=time.monotonic(); PACKAGE.mkdir()
    watchdog=threading.Timer(SECONDS,lambda:os._exit(124)); watchdog.daemon=True; watchdog.start()
    def check():
        require(time.monotonic()-started<SECONDS and not RUN.exists(),'Admission elapsed ceiling or scientific output')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE))<BYTES,'Admission storage ceiling')
    try:
        for source,target in ((Path(__file__),'prepare.py'),(ROOT/'Balance Harness/analysis/three-reference-confirmation-context.ps1','context.ps1'),
                              (common_path,'comparison-preparation.py'),(owner_path,'bounded_windows_process.py'),
                              (evidence_path,'confirmation-evidence-verification.py')): shutil.copyfile(source,PACKAGE/target)
        save(PACKAGE/'admission-helpers.json',{n:sha(PACKAGE/n) for n in ('prepare.py','context.ps1','comparison-preparation.py','bounded_windows_process.py','confirmation-evidence-verification.py')})
        save(PACKAGE/'admission-charge.json',dict(version=VERSION,scope='Captured-runtime admission allowance',chargedSeconds=SECONDS,chargedBytes=BYTES,
            treatment='Full allowance charged at admission start, including failed admission; no refund or transfer.',
            priorEngineeringLedger=dict(seconds=18180,MiB=13584,completeHistoricalEngineeringTotalsKnown=False)))
        save(PACKAGE/'admission-launch.json',dict(version=VERSION,maximumSeconds=SECONDS,maximumBytes=BYTES,monotonicDeadline=started+SECONDS-5,
            parentProcessId=os.getpid(),scientificLaunches=0,retries=0,resume=False))
        launcher=module('confirmation_lease_owner',launcher_path)
        with launcher.writer_lease(RUN.parent/'complete-family-allocation'),launcher.writer_lease(RUN):
            owner=module('confirmation_admission_owner',PACKAGE/'bounded_windows_process.py')
            process=owner.run([sys.executable,'-B','-X','utf8',str(PACKAGE/'prepare.py'),'_worker'],str(ROOT),str(PACKAGE/'worker.log'),
                started+SECONDS-5,cleanup_seconds=1,check=check)
            save(PACKAGE/'worker-process.json',process); validate_process(process)
            result=verify_contents(PACKAGE); save(PACKAGE/'admission.json',result); check()
            save(PACKAGE/'completion.json',dict(status='AdmissionCompleteNoReservation',measuredSecondsBeforeSeal=time.monotonic()-started,
                bytesBeforeSeal=sum(p.stat().st_size for p in common.paths(PACKAGE)),maximumSeconds=SECONDS,maximumBytes=BYTES,
                admissionChargeSeconds=SECONDS,admissionChargeBytes=BYTES,retries=0,scientificLaunches=0,newValues=0,fights=0))
            save(PACKAGE/'files.json',common.inventory(PACKAGE)); check()
            final=dict(status=result['status'],manifestSha256=sha(PACKAGE/'files.json'),requestSha256=result['requestSha256'],
                measuredSeconds=time.monotonic()-started,retainedBytes=sum(p.stat().st_size for p in common.paths(PACKAGE)),terminalBytes=0,
                maximumSeconds=SECONDS,maximumBytes=BYTES,admissionChargeSeconds=SECONDS,admissionChargeBytes=BYTES,scientificLaunches=0,newValues=0,fights=0)
            final=terminal_receipt(final)
            require(final['retainedBytes']+final['terminalBytes']<BYTES,'Terminal receipt storage exceeded')
            save(PACKAGE.with_name(PACKAGE.name+'-pin.json'),final); check(); print(json.dumps(final,indent=2))
    except BaseException as error:
        if not (PACKAGE/'failure.json').exists(): save(PACKAGE/'failure.json',dict(status='AdmissionFailedNoReservation',reason=str(error),
            measuredSeconds=time.monotonic()-started,admissionChargeSeconds=SECONDS,admissionChargeBytes=BYTES,scientificLaunches=0,newValues=0,fights=0,retries=0))
        raise
    finally: watchdog.cancel()

def verify(package,pin,live=False):
    evidence.authenticate(package,pin); result=verify_contents(package)
    require(result==read(package/'admission.json'),'Changed admission conclusions'); validate_process(read(package/'worker-process.json'))
    completion=read(package/'completion.json'); final_path=package.with_name(package.name+'-pin.json'); final=read(final_path)
    require(completion['status']=='AdmissionCompleteNoReservation' and completion['maximumSeconds']==completion['admissionChargeSeconds']==SECONDS
        and completion['maximumBytes']==completion['admissionChargeBytes']==BYTES and 0<=completion['measuredSecondsBeforeSeal']<=final['measuredSeconds']<SECONDS
        and final['manifestSha256']==pin and final['requestSha256']==result['requestSha256'] and final['terminalBytes']==final_path.stat().st_size
        and final['retainedBytes']==sum(p.stat().st_size for p in common.paths(package)) and final['retainedBytes']+final['terminalBytes']<BYTES
        and final['status']==result['status'] and final['maximumSeconds']==final['admissionChargeSeconds']==SECONDS
        and final['maximumBytes']==final['admissionChargeBytes']==BYTES and final['scientificLaunches']==final['newValues']==final['fights']==0
        and completion['scientificLaunches']==completion['newValues']==completion['fights']==completion['retries']==0,'Changed admission resource receipt')
    if live:
        files,values=common.history(RUN.parent,read(package/'request.json'))
        require(files==read(package/'history-files.json') and values==read(package/'definition.json')['excludedCombatSeeds'],'Live history changed')
    evidence.authenticate(package,pin)
    return dict(result,manifestSha256=pin,readOnlyVerification=True,nativePreparationsDuringVerification=0)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('command',choices=['prepare','verify','_worker'])
    parser.add_argument('--package',type=Path,default=PACKAGE); parser.add_argument('--manifest-sha256'); parser.add_argument('--live',action='store_true')
    args=parser.parse_args()
    if args.command=='prepare': prepare()
    elif args.command=='_worker': worker()
    else:
        require(args.manifest_sha256 is not None,'Supply the external manifest pin')
        print(json.dumps(verify(args.package.resolve(),args.manifest_sha256,args.live),indent=2))
