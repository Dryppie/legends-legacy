"""Prepare and audit one captured-gameplay three-reference native admission.

Only the preset and allocation-check commands are available. This helper never
launches a scientific search, derives values, changes defaults or resumes a run.
"""
import argparse
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
PREVIOUS = ROOT / 'TestResults/incumbent-tie-practical-admission-20260922'
PREVIOUS_PIN = '3f580b3216d1da086e4bdc080c6a883e3f1bceaec4028500ed78ccaa585cf034'
VERIFICATION = ROOT / 'TestResults/three-reference-implementation-verification-20260922'
VERIFICATION_PIN = 'b24d5ec45a92aa4ec55bcf9f322ddb019355092a1f36783bfe1469f21c3bb7c0'
BUILD = ROOT / 'TestResults/three-reference-final-build-20260922/bin/BalanceHarness/release'
REUSE = ROOT / 'TestResults/confirmed-team-reuse-checked-20260922'
REUSE_PIN = '84177584cafed74cdde9d6f4b51b986dda040affd410f47e799b2eabe882eb33'
HISTORY = ROOT / 'TestResults/fixed-family-execution-20260922/live-history-files.json'
HISTORY_PIN = 'b32e934d261a7da255500665ab89c7daf6e8f3f2a5b5cc7e1df0d60883cb12b2'
PACKAGE = ROOT / 'TestResults/three-reference-native-admission-20260922'
RUN = ROOT / 'TestResults/balance/tower-practical-three-reference-20260922'
SECONDS, BYTES = 600, 512 * 1048576
ALLOCATION = dict(master=20260922, domain='practical-three-reference-pilot-20260922',
                  discoverySamples=8, selectionSamples=32, confirmationSamples=1000)
VERSION = 'tower-practical-three-reference-allocated-search-v1'
POLICY = 'retained-composition-three-references-v1'

# Reuse the sealed independent history reader, inventory checks and Job Object owner.
# Imports have no execution side effects; their old preparation entry points are unused.
common_path = Path(__file__).with_name('comparison-preparation.py')
if not common_path.exists():
    common_path = PREVIOUS / 'comparison-preparation.py'
spec = importlib.util.spec_from_file_location('three_reference_common', common_path)
common = importlib.util.module_from_spec(spec)
spec.loader.exec_module(common)
require, read, sha, save = common.require, common.read, common.sha, common.save


def source_template(previous, values, context):
    require(context['settingsHash'] == previous['settingsHash'], 'Captured effective settings changed')
    result = copy.deepcopy(previous)
    result.update(id='practical-three-reference-pilot', executionHash=context['executionHash'],
                  excludedCombatSeeds=values, maximumBattles=4528)
    require(result['generation']['candidatesPerArm'] == 46
            and result['generation']['maximumAttemptsPerArm'] == 256, 'Changed candidate/proposal scope')
    return result


def source_request(history):
    previous = read(PACKAGE / 'capture/request.json')
    return dict(version='tower-practical-allocated-search-v1', contentRoot=str(PACKAGE / 'content'),
                definitionPath=str(PACKAGE / 'source-template.json'), definitionHash=sha(PACKAGE / 'source-template.json'),
                registryRoot=str(RUN.parent), outputRoot=str(RUN), requiredHistory=history,
                maximumSeconds=1800, maximumBytes=2 * 1073741824, priorSeconds=SECONDS, priorBytes=BYTES,
                pendingHistoryRecoveries=previous['pendingHistoryRecoveries'],
                recoveryReceiptHashes=previous['recoveryReceiptHashes'], allocation=ALLOCATION)


def check_conversion(source, converted, source_q, converted_q, teams):
    expected = copy.deepcopy(source)
    expected['generation']['policyVersion'] = POLICY
    expected['stages']['shortlist'] = 5
    require(len(converted['references']) == len(converted['starts']) == 3, 'Missing retained reference')
    selected = teams['selectedPartyId']
    added_ref, added_start = converted['references'][2], converted['starts'][2]
    expected_id = 'confirmed-' + selected[:24]
    require(added_ref['id'] == expected_id and added_ref['evidenceHash'] == REUSE_PIN
            and added_ref['scenario'] == next(t['scenario'] for t in teams['teams'] if t['partyId'] == selected)
            and added_start['referenceId'] == expected_id and added_start['party']['id'] == selected,
            'Selected recipe or provenance changed')
    expected['references'].append(added_ref)
    expected['starts'].append(added_start)
    require(converted == expected, 'Preset changed fields outside its three-reference contract')
    expected_q = dict(source_q, version=VERSION, definitionPath=str(PACKAGE / 'preset/template.json'),
                      definitionHash=sha(PACKAGE / 'preset/template.json'))
    require(converted_q == expected_q, 'Preset changed caller limits, history or allocation choices')


def authenticate_admission(pin, failure_pin):
    if failure_pin is None:
        return common.authenticate(PACKAGE, pin)
    # This one known closeout defect occurred after all native commands succeeded.
    # Authenticate the original seal plus the appended failure separately; never
    # remove the failure, rewrite the seal or resume native preparation.
    require(sha(PACKAGE / 'files.json') == pin and sha(PACKAGE / 'failure.json') == failure_pin,
            'Changed original admission seal or closeout failure')
    failure = read(PACKAGE / 'failure.json')
    require(failure['status'] == 'AdmissionFailedNoSearch' and failure['scientificLaunches'] == 0
            and failure['reason'] == 'Dependency differs from confirmed reuse context: runtimes/win/lib/net7.0/System.Management.dll',
            'This reconciliation only handles the recorded relative-path comparison defect')
    manifest = read(PACKAGE / 'files.json')
    require({p.relative_to(PACKAGE).as_posix() for p in common.paths(PACKAGE)} == set(manifest) | {'files.json', 'failure.json'},
            'Changed failed-package membership')
    for name, digest in manifest.items():
        target = (PACKAGE / name).resolve()
        require(target.is_relative_to(PACKAGE.resolve()) and sha(target) == digest, 'Changed saved admission file: ' + name)
    return manifest


def verify(pin, live=True, failure_pin=None):
    manifest = authenticate_admission(pin, failure_pin)
    require(not RUN.exists() and (failure_pin is not None or not (PACKAGE / 'failure.json').exists()), 'Search output exists or admission failed')
    require(sha(PACKAGE / 'capture/files.json') == PREVIOUS_PIN
            and sha(PACKAGE / 'verification/verification.json') == VERIFICATION_PIN
            and sha(PACKAGE / 'history-files.json') == HISTORY_PIN, 'Changed provenance or complete history pin')
    common.authenticate(PACKAGE / 'reuse', REUSE_PIN)
    inputs = read(PACKAGE / 'reuse/inputs.json')
    target_files = inputs['targetFiles']
    target_root = Path(read(PACKAGE / 'reuse/native-request.json')['runtimeRoot'])
    target_runtime = {Path(path).relative_to(target_root).as_posix(): digest for path, digest in target_files.items()
                      if Path(path).is_relative_to(target_root)}
    runtime = common.inventory(PACKAGE / 'runtime')
    captured = read(PACKAGE / 'capture/files.json')
    require(set(runtime) == {name[8:] for name in captured if name.startswith('runtime/')}, 'Runtime membership changed')
    for name, digest in runtime.items():
        if name not in common.HARNESS_FILES:
            require(digest == captured['runtime/' + name], 'Captured dependency changed: ' + name)
        if name.endswith('.dll') and name != 'BalanceHarness.dll':
            require(target_runtime.get(name) == digest, 'Dependency differs from confirmed reuse context: ' + name)
    require(runtime == read(PACKAGE / 'runtime-files.json'), 'Runtime inventory changed')
    evidence = read(PACKAGE / 'verification/verification.json')
    require(runtime['BalanceHarness.dll'] == evidence['finalHarnessSha256'], 'Harness differs from tested implementation')
    require(common.inventory(PACKAGE / 'content') == {name[8:]: digest for name, digest in captured.items()
                                                   if name.startswith('content/')}, 'Captured content/settings changed')
    context = read(PACKAGE / 'context.json')
    require(all(runtime.get(name + '.dll') == digest for name, digest in context['execution']['assemblyHashes'].items()),
            'Loaded execution assemblies differ from captured runtime files')
    sources = read(PACKAGE / 'compiled-source-files.json')
    require(len(sources) == context['compiledSourceDocuments']
            and all(sha(PACKAGE / 'source' / name) == digest for name, digest in sources.items()), 'Producing source changed')
    for method in ('CreateThreeReferencePreset', 'AllocationCheck'):
        require(any('TowerPracticalSearch.' + method + '#' in p for p in context['jitResolvedMethods']), 'Unresolved native entry path')
    history = read(PACKAGE / 'history-files.json')
    source, q = read(PACKAGE / 'source-template.json'), read(PACKAGE / 'source-request.json')
    require(len(history) == 229 and len(source['excludedCombatSeeds']) == 550367, 'Changed historical scope')
    require(source == source_template(read(PACKAGE / 'capture/template.json'), source['excludedCombatSeeds'], context)
            and q == source_request(history), 'Changed prospective source request')
    converted, converted_q = read(PACKAGE / 'preset/template.json'), read(PACKAGE / 'preset/request.json')
    check_conversion(source, converted, q, converted_q, read(PACKAGE / 'reuse/teams.json'))
    preset, native = read(PACKAGE / 'preset/preset.json'), read(PACKAGE / 'native-check.json')
    require(preset['status'] == 'PreparedNeedsAdmission' and preset['admissionRequired'] is True
            and preset['requestHash'] == sha(PACKAGE / 'preset/request.json')
            and preset['templateHash'] == sha(PACKAGE / 'preset/template.json')
            and preset['sourceRequestHash'] == sha(PACKAGE / 'source-request.json')
            and preset['reuseManifestHash'] == REUSE_PIN and preset['intervalFamily'] == 10
            and preset['newValues'] == preset['fights'] == 0, 'Invalid preset receipt')
    require(native['status'] == 'ReadyNoReservation' and native['version'] == VERSION and native['policy'] == POLICY
            and native['historicalValues'] == 550367 and native['historyFiles'] == history
            and native['declaredValues'] == 1041 and native['newValues'] == native['fights'] == 0
            and native['cost'] == preset['cost'] and native['cost']['total'] == 4528, 'Native admission differs from request')
    for name in ('context', 'preset', 'native-check'):
        process = read(PACKAGE / (name + '-process.json'))
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0
                and process['mechanism'] == 'suspended-owned-job-v1', 'Incomplete owned process: ' + name)
    require(not any(Path(name).name in common.LEDGERS or Path(name).name in ('entropy.bin', 'allocation-journal.jsonl')
                    for name in manifest), 'Unexpected allocation artifact')
    if live:
        actual, values = common.history(RUN.parent, converted_q)
        require(actual == history and values == source['excludedCombatSeeds'], 'Live history changed after admission')
    return dict(status='ThreeReferenceNativeAdmittedNoReservation', manifestSha256=pin, manifestFiles=len(manifest),
                reconciledCloseoutFailureSha256=failure_pin, nativePreparationsDuringVerification=0,
                requestSha256=sha(PACKAGE / 'preset/request.json'), historicalValues=550367, historyFiles=229,
                sourceDocuments=len(sources), jitResolvedMethods=len(context['jitResolvedMethods']),
                executionHash=context['executionHash'], settingsHash=context['settingsHash'], cost=native['cost'],
                declaredValueCount=1041, referenceCount=3, nativeReferenceInputsPrepared=3,
                preparationCountBasis='Successful native ValidateContent prepares one input for each reference',
                priorSeconds=SECONDS, priorBytes=BYTES, scientificLaunches=0, newValues=0, fights=0,
                executionAuthorizedByAdmission=False, outputExists=False)


def prepare():
    require(not PACKAGE.exists() and not RUN.exists(), 'Use a new package and absent scientific output')
    started = time.monotonic()
    PACKAGE.mkdir()

    def check():
        require(time.monotonic() - started < SECONDS - 2, 'Admission preparation deadline reached')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES, 'Admission storage ceiling reached')
        require(not RUN.exists(), 'Unexpected scientific output')

    def command(name, arguments):
        owner_spec = importlib.util.spec_from_file_location('three_reference_owner', PACKAGE / 'bounded_windows_process.py')
        owner = importlib.util.module_from_spec(owner_spec)
        owner_spec.loader.exec_module(owner)
        result = owner.run(arguments, str(ROOT), str(PACKAGE / (name + '.log')), started + SECONDS - 2,
                           cleanup_seconds=1, check=check)
        save(PACKAGE / (name + '-process.json'), result)
        require(result['exitCode'] == 0 and not result['timedOut'] and result['activeProcesses'] == 0,
                'Native preparation process failed: ' + name)
        print(name + ' passed', flush=True)

    try:
        common.authenticate(PREVIOUS, PREVIOUS_PIN)
        common.authenticate(REUSE, REUSE_PIN)
        require(sha(VERIFICATION / 'verification.json') == VERIFICATION_PIN and sha(HISTORY) == HISTORY_PIN, 'Changed evidence pin')
        evidence = read(VERIFICATION / 'verification.json')
        require(sha(BUILD / 'BalanceHarness.dll') == evidence['finalHarnessSha256'] and not evidence['unresolvedTestFailures'], 'Untested harness')
        for name, digest in evidence['changedSourceFiles'].items():
            require(sha(ROOT / name) == digest, 'Tested source changed: ' + name)
        for run in evidence['testRuns']:
            require(sha(VERIFICATION / (run['name'] + '.trx')) == run['sha256'], 'Test evidence changed')
        reuse_inputs = read(REUSE / 'inputs.json')
        for group in reuse_inputs.values():
            for path, digest in group.items():
                require(sha(Path(path)) == digest, 'Reuse source or captured target changed: ' + path)
        shutil.copytree(PREVIOUS / 'runtime', PACKAGE / 'runtime')
        shutil.copytree(PREVIOUS / 'content', PACKAGE / 'content')
        for name in common.HARNESS_FILES:
            shutil.copyfile(BUILD / name, PACKAGE / 'runtime' / name)
        (PACKAGE / 'capture').mkdir()
        for source, target in [('files.json', 'files.json'), ('preset/template.json', 'template.json'), ('preset/request.json', 'request.json')]:
            shutil.copyfile(PREVIOUS / source, PACKAGE / 'capture' / target)
        (PACKAGE / 'verification').mkdir()
        shutil.copyfile(VERIFICATION / 'verification.json', PACKAGE / 'verification/verification.json')
        for run in evidence['testRuns']:
            shutil.copyfile(VERIFICATION / (run['name'] + '.trx'), PACKAGE / 'verification' / (run['name'] + '.trx'))
        shutil.copytree(REUSE, PACKAGE / 'reuse')
        shutil.copyfile(HISTORY, PACKAGE / 'history-files.json')
        for source, target in [(Path(__file__), 'prepare.py'), (PREVIOUS / 'comparison-preparation.py', 'comparison-preparation.py'),
                               (PREVIOUS / 'bounded_windows_process.py', 'bounded_windows_process.py'), (PREVIOUS / 'context.ps1', 'context.ps1')]:
            shutil.copyfile(source, PACKAGE / target)
        save(PACKAGE / 'scope.json', dict(purpose='Native admission for the confirmed recipe and both captured controls',
             engineeringMaximumSeconds=SECONDS, engineeringMaximumBytes=BYTES, conservativeChargeSeconds=SECONDS,
             conservativeChargeBytes=BYTES, historicalEngineeringAccounting='Separately disclosed; total remains unknown',
             proposedSearchMaximumSeconds=1800, proposedSearchMaximumBytes=2 * 1073741824,
             allocation=ALLOCATION, maximumFights=4528, retries=0, resume=False, defaultsChanged=False,
             scientificLaunches=0, executionAuthorizedByAdmission=False))
        check()
        command('context', ['pwsh', '-NoProfile', '-File', str(PACKAGE / 'context.ps1'), '-Package', str(PACKAGE), '-RepositoryRoot', str(ROOT)])
        context = read(PACKAGE / 'context.json')
        save(PACKAGE / 'runtime-files.json', common.inventory(PACKAGE / 'runtime'))
        files, values = common.history(RUN.parent, read(PACKAGE / 'capture/request.json'))
        require(files == read(PACKAGE / 'history-files.json') and len(values) == 550367, 'History differs from the closed study')
        save(PACKAGE / 'source-template.json', source_template(read(PACKAGE / 'capture/template.json'), values, context))
        save(PACKAGE / 'source-request.json', source_request(files))
        command('preset', ['dotnet', str(PACKAGE / 'runtime/BalanceHarness.dll'), 'tower-practical-search-three-reference-preset',
                          str(PACKAGE / 'source-request.json'), str(PACKAGE / 'reuse'), REUSE_PIN, str(PACKAGE / 'preset')])
        check_conversion(read(PACKAGE / 'source-template.json'), read(PACKAGE / 'preset/template.json'),
                         read(PACKAGE / 'source-request.json'), read(PACKAGE / 'preset/request.json'), read(PACKAGE / 'reuse/teams.json'))
        command('native-check', ['dotnet', str(PACKAGE / 'runtime/BalanceHarness.dll'), 'tower-practical-search-allocation-check',
                                str(PACKAGE / 'preset/request.json')])
        save(PACKAGE / 'native-check.json', read(PACKAGE / 'native-check.log'))
        current, current_values = common.history(RUN.parent, read(PACKAGE / 'preset/request.json'))
        require(current == files and current_values == values, 'History changed during native admission')
        save(PACKAGE / 'preparation.json', dict(status='ReadyNoReservation', secondsBeforeSeal=time.monotonic() - started,
             engineeringMaximumSeconds=SECONDS, engineeringMaximumBytes=BYTES,
             nativeReferenceInputsPrepared=3, scientificLaunches=0, newValues=0, fights=0))
        check()
        save(PACKAGE / 'files.json', common.inventory(PACKAGE))
        result = verify(sha(PACKAGE / 'files.json'), live=False)
        check()
        result.update(measuredPreparationSeconds=time.monotonic() - started,
                      retainedPackageBytes=sum(p.stat().st_size for p in common.paths(PACKAGE)))
        save(PACKAGE.with_name(PACKAGE.name + '-pin.json'), result)
        print(json.dumps(result, indent=2))
    except BaseException as error:
        save(PACKAGE / 'failure.json', dict(status='AdmissionFailedNoSearch', reason=str(error),
             measuredSeconds=time.monotonic() - started, scientificLaunches=0, noResume=True))
        raise


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare', 'verify', 'reconcile'])
    parser.add_argument('--manifest-sha256')
    parser.add_argument('--failure-sha256')
    args = parser.parse_args()
    if args.command == 'prepare':
        prepare()
    else:
        require(args.manifest_sha256 is not None, 'Supply the independently retained manifest SHA-256')
        require((args.command == 'reconcile') == (args.failure_sha256 is not None), 'Reconciliation requires its recorded failure SHA-256')
        print(json.dumps(verify(args.manifest_sha256, failure_pin=args.failure_sha256), indent=2))
