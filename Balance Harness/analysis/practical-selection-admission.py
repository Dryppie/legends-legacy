"""Prepare/check a frozen diagnostic proposal, or verify the saved package.

The only native command this script can invoke is the zero-combat public check.
There is no allocation, gameplay launch, retry, recovery or acceptance path here.
Preparation copies existing producing binaries; it does not rebuild the harness.
"""
import contextlib
import copy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT/'TestResults/selection-diagnostic-admission-20260917'
RUN = ROOT/'TestResults/balance/tower-practical-selection-diagnostic-20260917'
PROBE = ROOT/'TestResults/selection-diagnostic-resource-probe-20260917-02'
PREVIOUS = ROOT/'TestResults/practical-native-verification-20260917'
NATIVE = ROOT/'TestResults/balance/tower-practical-native-verification-20260917'
CONTENT = ROOT/'LL/src/API/API.LL'
MIB = 1048576
PREP_SECONDS, PREP_BYTES = 120, 64*MIB
PRIOR_SECONDS, PRIOR_BYTES = 360+PREP_SECONDS, 512*MIB+PREP_BYTES
RUN_SECONDS, RUN_BYTES = 1380, 512*MIB
MAX_SECONDS, MAX_BYTES = PRIOR_SECONDS+RUN_SECONDS, PRIOR_BYTES+RUN_BYTES
DOMAIN = 'tower-practical-selection-diagnostic-20260917-v1'
MASTER = 2026091702
PHASES = {'admission': {'seconds':180,'bytes':96*MIB}, 'search': {'seconds':120,'bytes':64*MIB},
          'confirmation': {'seconds':660,'bytes':256*MIB}, 'audit': {'seconds':360,'bytes':16*MIB}}
OWNER_HASH = '120c2447fa5414d1d0062dcd4fc4eeb2ca70f744b8bb6e447f7f2325fa155332'
PINS = {
    ROOT/'Balance Harness/Tower-Practical-Selection-Diagnostic-Resource-Probe-02.json': 'c93860df4e4317293b8948a797cfceaf37993afbb2c37eb47dd211207f1f83e8',
    ROOT/'Balance Harness/Tower-Practical-Selection-Diagnostic-Resources.json': 'b08f13bcdb9b1698bc1eeed6edfb75237728b9e57bdb60fd4ad0328c12d9fabe',
    ROOT/'Balance Harness/Tower-Practical-Selection-Diagnostic-Contract.json': 'adb15e0fb652d1ebcae03b56bc4c79ef64ddbe1595b90a4b59523a0cf6bb5630',
    ROOT/'Balance Harness/Tower-Practical-Selection-Diagnostic-Design.json': 'bc47df34b8d0219d079623183dafc31b07b7187b81b569fc081cc22c8784d7ec',
    PREVIOUS/'files.json': '2c8415a62bdf6d72451a6468be5ba0c7ea78a10744e0688516a796690b921e15',
}


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f,'sha256').hexdigest()


def save(path, value):
    with path.open('xb') as f:
        f.write((json.dumps(value,indent=2,allow_nan=False)+'\n').encode('utf-8'))
        f.flush(); os.fsync(f.fileno())


def size(root):
    return sum(p.stat().st_size for p in root.rglob('*') if p.is_file())


def load(name,path):
    spec = importlib.util.spec_from_file_location(name,path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def request(template_hash, history, old):
    return {'version':'tower-practical-selection-diagnostic-v1', 'operation':{
        'version':'tower-practical-allocated-search-v1', 'contentRoot':str(CONTENT),
        'definitionPath':str(PACKAGE/'template.json'), 'definitionHash':template_hash,
        'registryRoot':str(ROOT/'TestResults/balance'), 'outputRoot':str(RUN),
        'requiredHistory':history, 'maximumSeconds':MAX_SECONDS, 'maximumBytes':MAX_BYTES,
        'priorSeconds':PRIOR_SECONDS, 'priorBytes':PRIOR_BYTES,
        'pendingHistoryRecoveries':old['pendingHistoryRecoveries'], 'recoveryReceiptHashes':old['recoveryReceiptHashes'],
        'allocation':{'master':MASTER,'domain':DOMAIN,'discoverySamples':8,'selectionSamples':32,'confirmationSamples':1000}},
        'phases':PHASES}


def verify():
    require(PACKAGE.is_dir() and not (PACKAGE/'failure.json').exists(), 'No completed admission package')
    receipt = read(PACKAGE/'preparation-receipt.json'); manifest = read(PACKAGE/'files.json')
    require(sha(PACKAGE/'files.json') == receipt['manifestSha256'], 'Changed package manifest')
    actual = {p.relative_to(PACKAGE).as_posix() for p in PACKAGE.rglob('*') if p.is_file()}
    require(actual == set(manifest) | {'files.json','preparation-receipt.json'}, 'Changed package membership')
    for name,digest in manifest.items():
        target = (PACKAGE/name).resolve()
        require(target.is_relative_to(PACKAGE) and sha(target) == digest, 'Changed package file: '+name)
    for path,digest in PINS.items():
        require(sha(path) == digest, 'Changed evidence pin: '+str(path))
    pending = read(PACKAGE/'decision.json'); require(pending['gameplayAuthorized'] is False
        and pending['riskAccepted'] is False and pending['status'] == 'AwaitingExplicitRunDecision', 'Changed scope decision')
    protocol = read(PROBE/'protocol.json'); old = read(NATIVE/'request.json')
    q = read(PACKAGE/'request.proposed.json'); d = read(PACKAGE/'template.json')
    expected = copy.deepcopy(read(PROBE/'resource-template-payload.json')['payload']); expected['id'] = DOMAIN
    require(d == expected and q == request(sha(PACKAGE/'template.json'),protocol['historyFiles'],old), 'Changed template/request semantics')
    require(q['operation']['maximumSeconds']-q['operation']['priorSeconds'] == RUN_SECONDS
            and q['operation']['maximumBytes']-q['operation']['priorBytes'] == RUN_BYTES
            and sum(p['seconds'] for p in PHASES.values())+2 <= RUN_SECONDS
            and sum(p['bytes'] for p in PHASES.values())+4*MIB <= RUN_BYTES, 'Invalid cumulative/phase accounting')
    require(d['generation']['seeds'] == [] and all(s[k] == [] for s in d['stages']['schedules'].values()
            for k in ('discovery','selection','confirmation','diagnostics')), 'Premature execution schedule')
    for name,digest in read(PACKAGE/'source-files.json').items():
        require(sha(ROOT/name) == digest, 'Changed producing source: '+name)
    for name,digest in read(PACKAGE/'runtime-files.json').items():
        require(sha(PACKAGE/'runtime'/name) == digest, 'Changed producing runtime: '+name)
    for name,digest in d['contentHashes'].items():
        require(sha(CONTENT/'Data'/name) == digest, 'Changed live content: '+name)
    for name,digest in q['operation']['requiredHistory'].items():
        require(sha(Path(name)) == digest, 'Changed pinned history: '+name)
    for name,digest in q['operation']['recoveryReceiptHashes'].items():
        require(sha(Path(name)) == digest, 'Changed recovery receipt')
    check = read(PACKAGE/'native-check.json'); process = read(PACKAGE/'native-check-process.json')
    require(check['status'] == 'ContractValidNoReservation' and check['historicalValues'] == 484285
            and check['maximumFights'] == 4640 and check['maximumNewReservations'] == 2089
            and check['newValues'] == check['fights'] == 0 and not check['resourceFeasibilityEstablished'], 'Native check did not pass')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Native check process incomplete')
    require(receipt['chargedSeconds'] == PREP_SECONDS and receipt['chargedBytes'] == PREP_BYTES
            and receipt['measuredSecondsAfterSealing'] < PREP_SECONDS-2 and size(PACKAGE) < PREP_BYTES, 'Preparation limit exceeded')
    require(not RUN.exists(), 'Proposed gameplay output already exists')
    require(not any(Path(n).name in ('seed-ledger.json','history-input.json','prior-seed-ledger.json') for n in actual), 'Premature reservation artifact')
    return {'version':'tower-selection-admission-review-v1','status':'NativeCheckPassedAwaitingRunDecision',
        'gameplayAuthorized':False,'combatRiskAccepted':False,'requestFileHash':sha(PACKAGE/'request.proposed.json'),
        'nativeRequestHash':check['requestHash'],'templateFileHash':q['operation']['definitionHash'],
        'packageManifestSha256':sha(PACKAGE/'files.json'),'packageManifestEntries':len(manifest),
        'packageReceiptSha256':sha(PACKAGE/'preparation-receipt.json'),'packageBytes':size(PACKAGE),
        'preparationSecondsAfterSealing':receipt['measuredSecondsAfterSealing'], 'nativeCheckSeconds':process['seconds'],
        'preparationCharge':{'seconds':PREP_SECONDS,'bytes':PREP_BYTES},
        'previousProbeCharges':{'seconds':360,'bytes':512*MIB}, 'carriedPriorCharges':{'seconds':PRIOR_SECONDS,'bytes':PRIOR_BYTES},
        'proposedAdditionalRunAllowance':{'seconds':RUN_SECONDS,'bytes':RUN_BYTES},
        'proposedCumulativeCeiling':{'seconds':MAX_SECONDS,'bytes':MAX_BYTES},'phases':PHASES,
        'closeoutReserve':{'seconds':2,'bytes':4*MIB},'remainingSetupHeadroom':{'seconds':58,'bytes':76*MIB},
        'historicalValues':484285,'requiredHistoryFiles':len(q['operation']['requiredHistory']),
        'maximumFights':4640,'maximumNewReservations':2089,'newFights':0,'newReservations':0,'entropyCalls':0,
        'nativeAdmission':check,'proposedOutput':str(RUN),'commandAfterApproval':read(PACKAGE/'command-after-approval.json'),
        'decision':pending,'pins':{str(p.relative_to(ROOT)):h for p,h in PINS.items()}}


def prepare():
    require(not PACKAGE.exists() and not RUN.exists(), 'Existing scope/output; preparation cannot overwrite or resume')
    started = time.monotonic(); PACKAGE.mkdir(); peak = 0; last_scan = -float('inf')
    def guard(force=False):
        nonlocal peak,last_scan
        require(time.monotonic()-started < PREP_SECONDS-2, 'Preparation deadline reached')
        if force or time.monotonic()-last_scan >= .2:
            observed = size(PACKAGE); peak = max(peak,observed); last_scan = time.monotonic()
            require(observed < PREP_BYTES-4*MIB, 'Preparation storage ceiling reached')
        require(not RUN.exists(), 'Unexpected gameplay output during zero-combat preparation')
    try:
        save(PACKAGE/'preparation-scope.json', {'version':'tower-selection-admission-preparation-v1',
            'scope':'Freeze proposal and run only the public no-reservation native check',
            'maximumSeconds':PREP_SECONDS,'maximumBytes':PREP_BYTES,'gameplayAuthorized':False,'newReservationsAllowed':0,
            'previousProbeCharges':{'seconds':360,'bytes':512*MIB},'preparationSourceHash':sha(Path(__file__))})
        for path,digest in PINS.items():
            require(sha(path) == digest, 'Changed admission evidence: '+str(path)); guard()
        # Read-only authentication of the earlier successful probe, including source,
        # runtime, all retained inventories, history hashes and its literal test result.
        verifier = load('selection_probe_evidence',ROOT/'Balance Harness/analysis/practical-selection-resource-probe-02.py')
        with contextlib.redirect_stdout(io.StringIO()) as captured:
            verifier.main()
        require(json.loads(captured.getvalue()) == read(ROOT/'Balance Harness/Tower-Practical-Selection-Diagnostic-Resource-Probe-02.json'), 'Changed probe review')
        guard(True)
        owner = PREVIOUS/'bounded_output_process.py'
        require(read(PREVIOUS/'files.json')[owner.name] == sha(owner) == OWNER_HASH, 'Changed process owner')
        shutil.copyfile(owner,PACKAGE/owner.name); shutil.copyfile(Path(__file__),PACKAGE/'prepare-source.py')
        owned = load('selection_admission_owner',PACKAGE/owner.name)
        runtime_files = read(PROBE/'runtime-assets.json')
        for name,digest in runtime_files.items():
            source = PROBE/'payload/executable'/name; require(sha(source) == digest, 'Changed measured runtime asset')
            target = PACKAGE/'runtime'/name; target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(source,target); require(sha(target) == digest, 'Changed runtime copy'); guard()
        save(PACKAGE/'runtime-files.json',runtime_files)
        save(PACKAGE/'source-files.json',read(PROBE/'probe-source-pins.json')['sourcePins'])
        save(PACKAGE/'evidence-files.json',{str(p.relative_to(ROOT)):h for p,h in PINS.items()})
        d = copy.deepcopy(read(PROBE/'resource-template-payload.json')['payload']); d['id'] = DOMAIN
        save(PACKAGE/'template.json',d)
        protocol = read(PROBE/'protocol.json'); old = read(NATIVE/'request.json')
        q = request(sha(PACKAGE/'template.json'),protocol['historyFiles'],old)
        save(PACKAGE/'request.proposed.json',q)
        save(PACKAGE/'settings-and-execution.json',{'settings':read(NATIVE/'study/scope.json')['settings'],
            'settingsHash':d['settingsHash'],'execution':protocol['execution'],'executionHash':d['executionHash'],
            'contentRoot':str(CONTENT),'contentHashes':d['contentHashes'],'rawAppSettingsCopied':False})
        save(PACKAGE/'decision.json',{'status':'AwaitingExplicitRunDecision','gameplayAuthorized':False,'riskAccepted':False,
            'question':'Approve one 4640-fight diagnostic with 1380 seconds and 512 MiB additional allowance, accepting capped operational failure?',
            'risk':'Current native combat, candidate-generation and full-verifier cost are unmeasured at this workload. A timeout, storage stop, incomplete entropy panel or integrity error yields no valid completed diagnostic.',
            'reservationConsequences':'Up to 2089 exposed values remain excluded. Pending entropy interruptions may block later allocation and have no supported automatic recovery/resume.',
            'controls':'One run; fixed phase ceilings; no retry, refill, replay, sample extension, seed shopping, budget transfer or automatic team promotion.',
            'primaryAndNominees':'Freeze all four nominees and one primary after 512 discovery and 128 selection fights; confirm all four on the same post-freeze 1000-value panel.',
            'outcomes':'SelectionMissDemonstrated, NoSelectionMissDemonstrated, or incomplete/invalid evidence. Both anchors and adoption Hold remain unchanged.'})
        run_command = ['dotnet',str(PACKAGE/'runtime/BalanceHarness.dll'),'tower-selection-diagnostic-run',str(PACKAGE/'request.proposed.json')]
        save(PACKAGE/'command-after-approval.json',{'executableArguments':run_command,'executed':False,
            'preconditions':['Explicit approval of this request and combat-failure risk','Revalidate package/source/runtime/content/recovery/history pins','Gameplay output absent'],
            'verificationIncluded':'Run performs native reconstruction and independent audit before publication; no extra standalone audit or replay is authorized.'})
        guard(True)
        print('Checking the frozen diagnostic request with the public zero-combat command.',flush=True)
        process = owned.run(['dotnet',PACKAGE/'runtime/BalanceHarness.dll','tower-selection-diagnostic-check',PACKAGE/'request.proposed.json'],
            ROOT,PACKAGE/'native-check.json',started+PREP_SECONDS-2,cleanup_seconds=1,guard=guard)
        save(PACKAGE/'native-check-process.json',process)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Native admission check failed')
        check = read(PACKAGE/'native-check.json')
        require(check['status'] == 'ContractValidNoReservation' and check['newValues'] == check['fights'] == 0, 'Unexpected check result')
        for name,digest in q['operation']['requiredHistory'].items():
            require(sha(Path(name)) == digest, 'History changed after check'); guard()
        for name,digest in read(PACKAGE/'source-files.json').items():
            require(sha(ROOT/name) == digest, 'Producing source changed'); guard()
        guard(True)
        save(PACKAGE/'files.json',{p.relative_to(PACKAGE).as_posix():sha(p) for p in sorted(PACKAGE.rglob('*')) if p.is_file()})
        guard(True)
        save(PACKAGE/'preparation-receipt.json',{'status':'PreparedAwaitingRunDecision','manifestSha256':sha(PACKAGE/'files.json'),
            'chargedSeconds':PREP_SECONDS,'chargedBytes':PREP_BYTES,'measuredSecondsAfterSealing':time.monotonic()-started,
            'sampledHighWaterBytesBeforeReceipt':peak,'newFights':0,'newReservations':0,'entropyCalls':0,'gameplayAuthorized':False})
        guard(True)
        print(json.dumps({'status':'PreparedAwaitingRunDecision','secondsIncludingReceipt':time.monotonic()-started,'finalBytes':size(PACKAGE),'newFights':0,'newReservations':0}))
    except Exception as error:
        save(PACKAGE/'failure.json',{'status':'PreparationFailed','error':str(error),'chargedSeconds':PREP_SECONDS,'chargedBytes':PREP_BYTES,
            'gameplayAuthorized':False,'newFights':0,'newReservations':0})
        raise


if __name__ == '__main__':
    if sys.argv[1:] == ['prepare']:
        prepare()
    else:
        require(not sys.argv[1:] or sys.argv[1:] == ['verify'], 'Use prepare or verify; no gameplay launch command exists')
        print(json.dumps(verify(),indent=2))
