"""Freeze one fixed-team proposal; invoke only its public zero-combat check.

prepare is single-use. verify authenticates saved evidence without native execution.
Neither command approves or launches gameplay. Unmetered engineering is disclosed
separately and its proposed accounting treatment remains a run precondition.
"""
import copy
from collections import deque
from concurrent.futures import ThreadPoolExecutor, wait, FIRST_COMPLETED
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import stat
import sys
import threading
import time
import xml.etree.ElementTree as ET
from datetime import datetime

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT/'TestResults/fixed-team-confirmation-admission-20260917-03'
FAILED_PACKAGE = ROOT/'TestResults/fixed-team-confirmation-admission-20260917'
FAILED_PACKAGE_02 = ROOT/'TestResults/fixed-team-confirmation-admission-20260917-02'
RUN = ROOT/'TestResults/balance/tower-practical-fixed-team-confirmation-20260917'
REGISTRY = ROOT/'TestResults/balance'
CONTENT = ROOT/'LL/src/API/API.LL'
BUILD = ROOT/'TestResults/fixed-team-confirmation-build/bin/BalanceHarness/release'
OLD_PACKAGE = ROOT/'TestResults/selection-diagnostic-admission-20260917'
OLD_RUN = REGISTRY/'tower-practical-selection-diagnostic-20260917'
OLD_OVERSIGHT = ROOT/'TestResults/selection-diagnostic-execution-20260917'
OWNER = ROOT/'TestResults/practical-native-verification-20260917/bounded_output_process.py'
PLAN = ROOT/'Balance Harness/Tower-Practical-Fixed-Team-Confirmation-Plan.json'
IMPLEMENTATION = ROOT/'Balance Harness/Tower-Practical-Fixed-Team-Confirmation-Implementation.json'
VERSION = 'tower-practical-fixed-team-confirmation-v1'
MIB = 1048576
PREP_SECONDS, PREP_BYTES = 300, 64*MIB
PRIOR_SECONDS, PRIOR_BYTES = 1860+240+PREP_SECONDS, (1088+64+64+64)*MIB
RUN_SECONDS, RUN_BYTES = 2400, 1024*MIB
PHASES = {'admission': {'seconds':180, 'bytes':128*MIB},
          'combat': {'seconds':1440, 'bytes':768*MIB}, 'audit': {'seconds':600, 'bytes':64*MIB}}
PINS = {
    FAILED_PACKAGE_02/'files.json': '3768a409229febcfdea47575b520b00038c2ec36dc615070bf08a274aff8c476',
    FAILED_PACKAGE/'files.json': '47efe5cd3fdf0062d42748264c3e4646ea844024487f87dcf3a9897f262f1cf6',
    PLAN: '5f12203adf1f74d28a21d657df7fe7d74212700481bc8e316d7354f1b74c5149',
    IMPLEMENTATION: 'c9b12e7bd0d0a4865f5968e2ddca0326c8fc4d27f37dfc0a0da9dc514ccd50d0',
    OLD_PACKAGE/'files.json': '3dbb77e0097c1f8fa791fdbe8b37894a55c84b0b65643d93a18039206dbf4661',
    OLD_RUN/'files.json': 'b73a6fc344214693dddfc14d74c4114ab4782d0ae80456ebd019c1103f149552',
    OLD_OVERSIGHT/'files.json': 'a14c73e7d330d284d1072fd62424843be4c8bd7a70c43782bd6fc70d54d824df',
    OLD_OVERSIGHT/'execution-receipt.json': 'bd9748c6b5121dda39cc1d8e0d677cb4b5a8b521d91c6fb2874745cdc7982873',
    OWNER: '120c2447fa5414d1d0062dcd4fc4eeb2ca70f744b8bb6e447f7f2325fa155332',
}


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def canonical(value):
    # These pinned ASCII contracts have no floating-point/non-ASCII string edge cases.
    text = json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True, allow_nan=False)
    for char in "+<>&'":
        text = text.replace(char, '\\u%04X' % ord(char))
    return hashlib.sha256(text.encode('utf-8')).hexdigest()


def save(path, value):
    with path.open('xb') as stream:
        stream.write((json.dumps(value, indent=2, allow_nan=False)+'\n').encode('utf-8'))
        stream.flush()
        os.fsync(stream.fileno())


def unlinked(path):
    for part in (path, *path.parents):
        require(not part.stat().st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT, 'Linked path: '+str(part))


def files(root):
    unlinked(root)
    result = {}
    for directory, dirs, names in os.walk(root, followlinks=False):
        for name in dirs+names:
            require(not (Path(directory)/name).stat().st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT,
                    'Linked evidence member')
        for name in names:
            p = Path(directory)/name
            result[p.relative_to(root).as_posix()] = p
    return result


def size(root):
    return sum(p.stat().st_size for p in files(root).values())


def inventory(root, exclusions=()):
    return {name:sha(p) for name,p in sorted(files(root).items()) if name not in exclusions}


def authenticate(root, exclusions):
    expected = read(root/'files.json')
    require(inventory(root, exclusions) == expected, 'Changed sealed inventory: '+str(root))
    return len(expected)+len(exclusions)


def history_files(guard=lambda: None):
    # Match TowerHistoryRegistry's full recursive filename scan; do not skip
    # duplicate-content copies, old Pending inputs or nested historical sources.
    # Match the native scanner's four bounded directory workers. DirEntry uses
    # Windows enumeration metadata; Path.stat on every report is prohibitively
    # expensive in this registry. Reparse points remain rejected on all entries.
    unlinked(REGISTRY)
    pending, result, stopped = deque([REGISTRY]), {}, threading.Event()
    def scan(directory):
        require(not directory.stat().st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT, 'Linked registry directory')
        children, ledgers = [], []
        with os.scandir(directory) as entries:
            for ordinal, entry in enumerate(entries):
                if ordinal % 256 == 0:
                    require(not stopped.is_set(), 'Registry scan cancelled')
                require(not entry.stat(follow_symlinks=False).st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT,
                        'Linked registry entry')
                if entry.is_dir(follow_symlinks=False):
                    children.append(Path(entry.path))
                elif entry.name in ('seed-ledger.json', 'prior-seed-ledger.json', 'history-input.json'):
                    ledgers.append((entry.path, sha(Path(entry.path))))
        return children, ledgers
    with ThreadPoolExecutor(max_workers=4) as pool:
        active, directories = set(), 0
        try:
            while pending or active:
                guard()
                while pending and len(active) < 4:
                    directories += 1
                    require(directories <= 2000000, 'History directory bound exceeded')
                    active.add(pool.submit(scan, pending.popleft()))
                done, active = wait(active, timeout=.05, return_when=FIRST_COMPLETED)
                for future in done:
                    children, ledgers = future.result()
                    pending.extend(children)
                    result.update(ledgers)
        finally:
            stopped.set()
    return dict(sorted(result.items()))


def evidence():
    for path,digest in PINS.items():
        require(sha(path) == digest, 'Changed evidence pin: '+str(path))
    count = authenticate(OLD_RUN, ('files.json',))
    count += authenticate(OLD_OVERSIGHT, ('files.json', 'execution-receipt.json'))
    count += authenticate(OLD_PACKAGE, ('files.json', 'preparation-receipt.json'))
    authenticate(FAILED_PACKAGE, ('files.json',))
    failed = read(FAILED_PACKAGE/'failure.json')
    require(failed['nativeChecks'] == failed['newFights'] == failed['newReservations'] == 0
        and failed['chargedSeconds'] == 120 and failed['chargedBytes'] == 64*MIB, 'Changed failed preparation')
    oversight = failed['oversightReceipt']
    require(sha(ROOT/oversight['path']) == oversight['sha256'], 'Changed failed-preparation oversight')
    authenticate(FAILED_PACKAGE_02, ('files.json',))
    failed02 = read(FAILED_PACKAGE_02/'failure.json')
    check02 = read(FAILED_PACKAGE_02/'native-check.json')
    process02 = read(FAILED_PACKAGE_02/'native-check-process.json')
    require(failed02['chargedSeconds'] == 120 and failed02['chargedBytes'] == 64*MIB
        and check02['status'] == 'ContractValidNoReservation' and check02['newValues'] == check02['fights'] == 0
        and process02['exitCode'] == 0 and process02['activeProcesses'] == 0, 'Changed second failed preparation')
    implementation = read(IMPLEMENTATION)
    for key in ('testLog', 'testTrx'):
        pin = implementation[key]
        require(sha(ROOT/pin['path']) == pin['sha256'], 'Changed successful implementation verification')
    for name,digest in implementation['sourceHashes'].items():
        require(sha(ROOT/name) == digest, 'Changed tested implementation: '+name)
    require(implementation['testsPassed'] == 293 and implementation['testsFailed'] == 0, 'Unverified implementation')
    # Old producing pins remain immutable; only the two reviewed routers differ.
    sources = read(OLD_PACKAGE/'source-files.json')
    for name,digest in sources.items():
        expected = implementation['sourceHashes'].get(name, digest)
        require(sha(ROOT/name) == expected, 'Unreviewed producing source change: '+name)
    sources.update(implementation['sourceHashes'])
    for name in sources:
        sources[name] = sha(ROOT/name)
    return implementation, sources, count


def definition(implementation):
    plan = read(PLAN)
    ledger = read(OLD_RUN/'seed-ledger.json')
    require(ledger['reservationState'] == 'Complete', 'Previous reservation incomplete')
    history = sorted(set(ledger['historical']) | set(ledger['reserved']))
    require(len(history) == 486374, 'Changed historical union')
    teams = [{k:copy.deepcopy(t[k]) for k in ('role','partyId','referenceIds','scenario')}
             for t in plan['recipesInFixedExecutionOrder']]
    require(canonical(teams) == '1162c50936eb5116d27e20642c65fdca6f9d5ad6a9b5a7058be5d282bcb32428', 'Changed exact family')
    execution = implementation['fixtureObservations']['producingExecution']
    return {'version':VERSION, 'teams':teams, 'contentHashes':plan['contentHashes'],
            'settingsHash':plan['settingsHash'], 'executionHash':canonical(execution), 'excludedCombatSeeds':history}


def request(definition_hash, history):
    old = read(OLD_PACKAGE/'request.proposed.json')['operation']
    return {'version':VERSION, 'contentRoot':str(CONTENT), 'definitionPath':str(PACKAGE/'definition.json'),
            'definitionHash':definition_hash, 'registryRoot':str(REGISTRY), 'outputRoot':str(RUN),
            'requiredHistory':history, 'maximumSeconds':PRIOR_SECONDS+RUN_SECONDS, 'maximumBytes':PRIOR_BYTES+RUN_BYTES,
            'phases':PHASES, 'priorSeconds':PRIOR_SECONDS, 'priorBytes':PRIOR_BYTES,
            'pendingHistoryRecoveries':old['pendingHistoryRecoveries'], 'recoveryReceiptHashes':old['recoveryReceiptHashes']}


def accounting():
    observations = []
    base = ROOT/'TestResults/fixed-team-confirmation-implementation-20260917'
    ns = {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    for name in ('initial-focused.trx', 'verification.trx'):
        p = base/name
        times = ET.parse(p).find('t:Times', ns).attrib
        duration = (datetime.fromisoformat(times['finish'])-datetime.fromisoformat(times['start'])).total_seconds()
        observations.append({'path':str(p.relative_to(ROOT)), 'sha256':sha(p), 'recordedSessionSeconds':duration})
    observed = {str((base/name).relative_to(ROOT)):sha(base/name) for name in files(base)}
    return {'status':'OperationalChargesReconciledEngineeringTreatmentPending',
            'closedDiagnosticChain':{'seconds':1860, 'bytes':1088*MIB, 'basis':'Full declared charges, no refund'},
            'failedPreparations':{'seconds':240, 'bytes':128*MIB,
                'basis':'Two 120-second /64-MiB scopes. First stopped before native check; second check passed but final scan exceeded its deadline. Both full caps carried, no refund.'},
            'thisPreparation':{'seconds':PREP_SECONDS, 'bytes':PREP_BYTES, 'basis':'Full prospective cap, no refund'},
            'carriedOperationalCharges':{'seconds':PRIOR_SECONDS, 'bytes':PRIOR_BYTES},
            'additionalRunProposal':{'seconds':RUN_SECONDS, 'bytes':RUN_BYTES},
            'proposedOperationalCumulativeCeiling':{'seconds':PRIOR_SECONDS+RUN_SECONDS, 'bytes':PRIOR_BYTES+RUN_BYTES},
            'engineering':{'status':'UnmeteredSeparateScope', 'chargedSeconds':None, 'chargedBytes':None,
                'proposedTreatment':'Keep implementation and admission-script/document engineering separately disclosed from the capped native-operation ledger; explicitly accept this treatment before any run.',
                'acceptanceRecorded':False, 'observationsAreCompleteCost':False,
                'reason':'No numerical engineering allowance was declared. Successful test session times and retained evidence bytes cannot reconstruct all builds, failed tests, temporary files, preparation or review time. Unknown is not zero.',
                'successfulTestSessions':observations, 'implementationEvidenceBytes':size(base), 'implementationEvidenceFiles':observed,
                'currentAdmissionEngineering':'Script authoring, read-only verification and documentation are unmetered engineering, outside the prospectively bounded native package preparation.'},
            'ifTreatmentRejected':'Establish an explicit accounting disposition and issue a new request; do not silently debit zero, invent a historical cap, or use run phase headroom.'}


def command():
    return {'executableArguments':['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'),
                'tower-fixed-team-confirmation-run', str(PACKAGE/'request.proposed.json')], 'executed':False,
            'preconditions':['Approve the exact run and capped failure risk',
                'Accept the explicitly separate unmetered engineering accounting treatment',
                'Revalidate this package, runtime, content, source, recoveries and complete history',
                'Use enclosing owned execution with combined output/log/oversight accounting inside 2400 seconds and 1 GiB',
                'Gameplay output must still be absent'],
            'audits':'Both audits are included in the run; no extra public native verification, probe or replay is authorized.'}


def verify():
    require(PACKAGE.is_dir() and not (PACKAGE/'failure.json').exists(), 'No completed package')
    receipt = read(PACKAGE/'preparation-receipt.json')
    require(sha(PACKAGE/'files.json') == receipt['manifestSha256'], 'Changed package manifest')
    count = authenticate(PACKAGE, ('files.json', 'preparation-receipt.json'))
    require(sha(Path(__file__)) == sha(PACKAGE/'prepare-source.py'), 'Changed admission reader')
    implementation, sources, sealed = evidence()
    require(read(PACKAGE/'source-files.json') == sources, 'Changed source freeze')
    d = read(PACKAGE/'definition.json')
    require(d == definition(implementation), 'Changed definition')
    history = history_files()
    q = read(PACKAGE/'request.proposed.json')
    require(q == request(sha(PACKAGE/'definition.json'), history), 'Changed request or complete registry membership')
    require(read(PACKAGE/'accounting.json') == accounting(), 'Changed accounting disclosure/evidence')
    require(read(PACKAGE/'command-after-approval.json') == command(), 'Changed deferred command')
    for name,digest in q['recoveryReceiptHashes'].items():
        require(sha(Path(name)) == digest, 'Changed supported recovery receipt')
    for name,digest in d['contentHashes'].items():
        require(sha(CONTENT/'Data'/name) == digest, 'Changed live content')
    runtime = read(PACKAGE/'runtime-files.json')
    require(inventory(PACKAGE/'runtime') == runtime, 'Changed retained runtime')
    execution = implementation['fixtureObservations']['producingExecution']
    require(all(runtime[k+'.dll'] == h for k,h in execution['assemblyHashes'].items()), 'Untested runtime')
    settings = read(PACKAGE/'settings-and-execution.json')
    require(settings['settingsFileSha256'] == sha(CONTENT/'appsettings.json')
        and settings['settingsHash'] == d['settingsHash'] and settings['execution'] == execution
        and settings['executionHash'] == d['executionHash'], 'Changed settings or execution binding')
    check = read(PACKAGE/'native-check.json')
    process = read(PACKAGE/'native-check-process.json')
    require(check == {'status':'ContractValidNoReservation', 'version':VERSION,
        'requestHash':canonical(q), 'historicalValues':486374, 'maximumFights':16500,
        'maximumNewReservations':11000, 'transportBindings':18, 'resourceFeasibilityEstablished':False,
        'newValues':0, 'fights':0}, 'Changed or failed native check')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Native process incomplete')
    require(receipt['chargedSeconds'] == PREP_SECONDS and receipt['chargedBytes'] == PREP_BYTES
        and receipt['measuredSecondsAfterSealing'] < PREP_SECONDS-2 and size(PACKAGE) < PREP_BYTES, 'Preparation cap exceeded')
    require(sum(p['seconds'] for p in PHASES.values())+2 <= RUN_SECONDS
        and sum(p['bytes'] for p in PHASES.values())+4*MIB <= RUN_BYTES, 'Invalid phase accounting')
    require(not RUN.exists(), 'Gameplay output already exists')
    decision = read(PACKAGE/'decision.json')
    require(decision['status'] == 'AwaitingRunAndAccountingDecision' and not decision['gameplayAuthorized']
        and not decision['riskAccepted'] and not decision['engineeringTreatmentAccepted'], 'Changed decision boundary')
    return {'version':'tower-fixed-team-admission-review-v1', 'status':'NativeCheckPassedAwaitingRunAndAccountingDecision',
        'gameplayAuthorized':False, 'nativeCombatFeasibilityEstablished':False, 'engineeringTreatmentAccepted':False,
        'requestFileHash':sha(PACKAGE/'request.proposed.json'), 'nativeRequestHash':check['requestHash'],
        'definitionFileHash':q['definitionHash'], 'executionHash':d['executionHash'],
        'packageManifestSha256':sha(PACKAGE/'files.json'), 'packageReceiptSha256':sha(PACKAGE/'preparation-receipt.json'),
        'packageFiles':count, 'packageBytes':size(PACKAGE), 'preparationSeconds':receipt['measuredSecondsAfterSealing'],
        'nativeCheckSeconds':process['seconds'], 'runtimeFiles':len(runtime), 'sourcePins':len(sources),
        'sealedHistoricalFilesPreserved':sealed, 'historicalValues':486374, 'requiredHistoryFiles':len(history),
        'supportedRecoveryReceipts':len(q['recoveryReceiptHashes']), 'maximumFights':16500, 'maximumNewReservations':11000,
        'maximumHistoricalUnion':497374, 'phases':PHASES, 'closeoutReserve':{'seconds':2,'bytes':4*MIB},
        'enclosingHeadroom':{'seconds':178,'bytes':60*MIB},
        'carriedOperationalCharges':{'seconds':PRIOR_SECONDS,'bytes':PRIOR_BYTES},
        'additionalRunProposal':{'seconds':RUN_SECONDS,'bytes':RUN_BYTES},
        'proposedOperationalCumulativeCeiling':{'seconds':q['maximumSeconds'],'bytes':q['maximumBytes']},
        'newFights':0, 'newReservations':0, 'entropyCalls':0, 'adoption':'Hold', 'v19Status':'Unresolved',
        'decision':decision, 'commandAfterApproval':command()}


def prepare():
    require(not PACKAGE.exists() and not RUN.exists(), 'Existing scope; no overwrite, resume or automatic retry')
    started = time.monotonic()
    PACKAGE.mkdir()
    peak, last_scan = 0, -float('inf')
    def guard(force=False):
        nonlocal peak, last_scan
        require(time.monotonic()-started < PREP_SECONDS-2, 'Preparation deadline reached')
        if force or time.monotonic()-last_scan >= .2:
            peak = max(peak, size(PACKAGE))
            last_scan = time.monotonic()
            require(peak < PREP_BYTES-4*MIB, 'Preparation storage ceiling reached')
            require(not RUN.exists(), 'Unexpected gameplay output')
    try:
        save(PACKAGE/'preparation-scope.json', {'scope':'Copy tested runtime, freeze proposal, public zero-combat check only',
            'maximumSeconds':PREP_SECONDS, 'maximumBytes':PREP_BYTES, 'sourceHash':sha(Path(__file__)),
            'gameplayAuthorized':False, 'newReservationsAllowed':0})
        implementation, sources, _ = evidence()
        guard(True)
        shutil.copyfile(Path(__file__), PACKAGE/'prepare-source.py')
        shutil.copyfile(OWNER, PACKAGE/OWNER.name)
        spec = importlib.util.spec_from_file_location('fixed_admission_owner', PACKAGE/OWNER.name)
        owner = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(owner)
        # Derive assets from this build, checking against the previously complete
        # layout; never treat old assembly digests as the new producing identity.
        deps = read(BUILD/'BalanceHarness.deps.json')
        names = {'BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json'}
        for target in deps['targets'].values():
            for library in target.values():
                names.update(Path(n).name for n in library.get('runtime', {}))
                names.update(n for n in library.get('runtimeTargets', {}))
        require(names == set(read(OLD_PACKAGE/'runtime-files.json')), 'Review changed dependency layout before native check')
        runtime = {}
        for name in sorted(names):
            target = PACKAGE/'runtime'/name
            target.parent.mkdir(parents=True, exist_ok=True)
            digest = sha(BUILD/name)
            shutil.copyfile(BUILD/name, target)
            require(sha(target) == digest, 'Changed runtime copy')
            runtime[name] = digest
            guard()
        execution = implementation['fixtureObservations']['producingExecution']
        require(all(runtime[k+'.dll'] == h for k,h in execution['assemblyHashes'].items()), 'Runtime differs from passing tests')
        save(PACKAGE/'runtime-files.json', runtime)
        save(PACKAGE/'source-files.json', sources)
        save(PACKAGE/'evidence-files.json', {str(p.relative_to(ROOT)):h for p,h in PINS.items()})
        save(PACKAGE/'definition.json', definition(implementation))
        history = history_files(guard)
        q = request(sha(PACKAGE/'definition.json'), history)
        save(PACKAGE/'request.proposed.json', q)
        save(PACKAGE/'accounting.json', accounting())
        save(PACKAGE/'settings-and-execution.json', {'settings':read(OLD_RUN/'study/scope.json')['settings'],
            'settingsHash':read(PLAN)['settingsHash'], 'execution':execution, 'executionHash':canonical(execution),
            'settingsFileSha256':sha(CONTENT/'appsettings.json'),
            'rawAppSettingsCopied':False})
        save(PACKAGE/'command-after-approval.json', command())
        save(PACKAGE/'decision.json', {'status':'AwaitingRunAndAccountingDecision', 'gameplayAuthorized':False,
            'riskAccepted':False, 'engineeringTreatmentAccepted':False,
            'proposal':'One 16500-fight fixed-team run, 5500 paired trials per exact team, within 2400 seconds and 1 GiB additional allowance.',
            'accountingDecision':'Accept separately disclosed unmetered engineering; operational prior is 2400 seconds /1280 MiB and proposed cumulative is 4800 seconds /2304 MiB. These are not all-in engineering totals.',
            'risk':'Native full-run feasibility is unproven. Resource, integrity or entropy failure is terminal; no retry, replay, refill, extension or budget transfer.',
            'reservationConsequences':'Up to 11000 exposed values remain permanently excluded; entropy interruption can leave blocking Pending with no supported automatic recovery.',
            'outcomes':'Complete agreeing positive audits may AdoptFixedTeam; a complete negative keeps Hold and both anchors. Incomplete evidence cannot pass. Balance and method reliability remain unassessed.'})
        guard(True)
        print('Running the public fixed-team zero-combat admission check once.', flush=True)
        result = owner.run(['dotnet', PACKAGE/'runtime/BalanceHarness.dll', 'tower-fixed-team-confirmation-check',
                            PACKAGE/'request.proposed.json'], ROOT, PACKAGE/'native-check.json',
                           started+PREP_SECONDS-2, cleanup_seconds=1, guard=guard)
        save(PACKAGE/'native-check-process.json', result)
        require(result['exitCode'] == 0 and not result['timedOut'] and result['activeProcesses'] == 0, 'Native check failed')
        check = read(PACKAGE/'native-check.json')
        require(check['status'] == 'ContractValidNoReservation' and check['newValues'] == check['fights'] == 0, 'Unexpected check result')
        require(history_files(guard) == history, 'Registry changed during preparation')
        for name,digest in sources.items():
            require(sha(ROOT/name) == digest, 'Source changed during preparation')
        guard(True)
        save(PACKAGE/'files.json', inventory(PACKAGE))
        guard(True)
        save(PACKAGE/'preparation-receipt.json', {'status':'PreparedAwaitingRunAndAccountingDecision',
            'manifestSha256':sha(PACKAGE/'files.json'), 'chargedSeconds':PREP_SECONDS, 'chargedBytes':PREP_BYTES,
            'measuredSecondsAfterSealing':time.monotonic()-started, 'sampledHighWaterBytesBeforeReceipt':peak,
            'newFights':0, 'newReservations':0, 'entropyCalls':0, 'gameplayAuthorized':False})
        guard(True)
        print(json.dumps({'status':'PreparedAwaitingRunAndAccountingDecision',
                          'secondsIncludingReceipt':time.monotonic()-started, 'finalBytes':size(PACKAGE)}))
    except Exception as error:
        save(PACKAGE/'failure.json', {'status':'PreparationFailed', 'error':str(error),
            'chargedSeconds':PREP_SECONDS, 'chargedBytes':PREP_BYTES, 'gameplayAuthorized':False})
        raise


if __name__ == '__main__':
    if sys.argv[1:] == ['prepare']:
        prepare()
    else:
        require(sys.argv[1:] in ([], ['verify']), 'Use prepare or verify; no gameplay launch command exists')
        print(json.dumps(verify(), indent=2))
