"""Prepare and authenticate one practical preset admission; never allocate or fight.

Only the preset and allocation-check CLI routes are invoked. The captured gameplay
runtime is retained; the harness comes from the separately verified preset build.
"""
import argparse
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
PREVIOUS = ROOT/'TestResults/incumbent-tie-comparison-admission-20260922-02'
PREVIOUS_PIN = 'ac6486e38aec867c5df5c1d2c7931edde13736d288de7d6a00dd87a408719776'
VERIFICATION = ROOT/'TestResults/incumbent-tie-preset-verification-20260922'
VERIFICATION_PIN = '17fc52ced1c6ec33f1f436df4b74f21d1ff38c00b07f74dc14fae9e0c4e8c060'
BUILD = ROOT/'TestResults/incumbent-tie-preset-build-20260922/bin/BalanceHarness/release'
PACKAGE = ROOT/'TestResults/incumbent-tie-practical-admission-20260922'
RUN = ROOT/'TestResults/balance/tower-practical-incumbent-tie-20260922'
PRIMARY = 'confirmed-399bc7760fb0cf790a5d8ac4'
PARTY = '399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b'
SECONDS, BYTES = 600, 512*1048576  # Engineering preparation only.
ALLOCATION = dict(master=20260922, domain='practical-incumbent-tie-pilot-20260922',
                  discoverySamples=8, selectionSamples=32, confirmationSamples=1000)

# Reuse the authenticated history reader and Windows Job owner wrapper. Importing
# this helper has no side effects; its comparison preparation entry point is unused.
common_path = Path(__file__).with_name('comparison-preparation.py')
if not common_path.exists():
    common_path = PREVIOUS/'prepare.py'
spec = importlib.util.spec_from_file_location('preparation_common', common_path)
common = importlib.util.module_from_spec(spec)
spec.loader.exec_module(common)
require, read, sha, save = common.require, common.read, common.sha, common.save


def source_template(previous, values, context):
    require(context['settingsHash'] == previous['settingsHash'], 'Changed captured settings')
    d = copy.deepcopy(previous)
    d.update(id='practical-incumbent-tie-pilot', excludedCombatSeeds=values, executionHash=context['executionHash'])
    return d


def source_request(history):
    old = read(PACKAGE/'capture/request.json')
    return dict(version='tower-practical-allocated-search-v1', contentRoot=str(PACKAGE/'content'),
                definitionPath=str(PACKAGE/'source-template.json'), definitionHash=sha(PACKAGE/'source-template.json'),
                registryRoot=str(RUN.parent), outputRoot=str(RUN), requiredHistory=history,
                maximumSeconds=1200, maximumBytes=1073741824, priorSeconds=0, priorBytes=0,
                pendingHistoryRecoveries=old['pendingHistoryRecoveries'], recoveryReceiptHashes=old['recoveryReceiptHashes'],
                allocation=ALLOCATION)


def verify(pin):
    manifest = common.authenticate(PACKAGE, pin)
    require(not RUN.exists() and not (PACKAGE/'failure.json').exists(), 'Output exists or preparation failed')
    require(sha(PACKAGE/'capture/files.json') == PREVIOUS_PIN
            and sha(PACKAGE/'verification/files.json') == VERIFICATION_PIN, 'Changed provenance')
    evidence = read(PACKAGE/'verification/verification.json')
    runtime = common.inventory(PACKAGE/'runtime')
    captured = read(PACKAGE/'capture/files.json')
    require(runtime == read(PACKAGE/'runtime-files.json')
            and set(runtime) == {name[8:] for name in captured if name.startswith('runtime/')}, 'Changed runtime membership')
    for name, digest in runtime.items():
        if name not in common.HARNESS_FILES:
            require(captured['runtime/'+name] == digest, 'Changed captured dependency: '+name)
    require(runtime['BalanceHarness.dll'] == evidence['runtimeDllSha256']
            and runtime['BalanceHarness.pdb'] == evidence['runtimePdbSha256'], 'Changed tested harness')
    require(common.inventory(PACKAGE/'content') == {name[8:]: digest for name, digest in captured.items()
                                                 if name.startswith('content/')}, 'Changed content/settings')
    context = read(PACKAGE/'context.json')
    sources = read(PACKAGE/'compiled-source-files.json')
    require(len(sources) == context['compiledSourceDocuments']
            and all(sha(PACKAGE/'source'/name) == digest for name, digest in sources.items()), 'Changed producing source')
    require(any('TowerPracticalSearch.CreateIncumbentTiePreset#' in name for name in context['jitResolvedMethods'])
            and any('TowerPracticalSearch.AllocationCheck#' in name for name in context['jitResolvedMethods'])
            and context['nativePreparations'] == context['newValues'] == context['fights'] == 0, 'Missing compatibility paths')
    files, values = common.history(RUN.parent, read(PACKAGE/'capture/request.json'))
    require(len(files) == 224 and len(values) == 538326, 'Changed prospective history scope')
    require(files == read(PACKAGE/'history-files.json'), 'Live history changed after admission')
    source = read(PACKAGE/'source-template.json')
    require(source == source_template(read(PACKAGE/'capture/template.json'), values, context), 'Changed template scope')
    q = read(PACKAGE/'source-request.json')
    require(q == source_request(files), 'Changed source request')
    expected = copy.deepcopy(source)
    expected['stages'].update(selectionPolicyVersion='tower-staged-incumbent-tie-v1', selectionPrimaryReferenceId=PRIMARY)
    require(read(PACKAGE/'preset/template.json') == expected, 'Preset changed fields beyond explicit selector')
    expected_q = dict(q, definitionPath=str(PACKAGE/'preset/template.json'), definitionHash=sha(PACKAGE/'preset/template.json'))
    require(read(PACKAGE/'preset/request.json') == expected_q, 'Preset changed request choices')
    receipt = read(PACKAGE/'preset/preset.json')
    require(receipt['status'] == 'PreparedNeedsAdmission' and receipt['admissionRequired'] is True
            and receipt['selectionPrimaryReferenceId'] == PRIMARY and receipt['selectionPrimaryPartyId'] == PARTY
            and receipt['sourceRequestFileHash'] == sha(PACKAGE/'source-request.json')
            and receipt['sourceTemplateFileHash'] == sha(PACKAGE/'source-template.json')
            and receipt['requestFileHash'] == sha(PACKAGE/'preset/request.json')
            and receipt['templateFileHash'] == sha(PACKAGE/'preset/template.json')
            and receipt['newValues'] == receipt['fights'] == 0, 'Invalid preset receipt')
    native = read(PACKAGE/'native-check.json')
    require(native['status'] == 'ReadyNoReservation' and native['version'] == q['version']
            and native['policy'] == 'retained-composition-incumbents-v1'
            and native['historicalValues'] == len(values) and native['historyFiles'] == files
            and native['declaredValues'] == 1041 and native['newValues'] == native['fights'] == 0
            and native['cost'] == receipt['cost'] and native['cost']['total'] == 3496,
            'Native admission differs from prepared input')
    for name in ('context', 'preset', 'native-check'):
        process = read(PACKAGE/(name+'-process.json'))
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0
                and process['mechanism'] == 'suspended-owned-job-v1', 'Incomplete owned process: '+name)
    require(not any(Path(name).name in common.LEDGERS or Path(name).name in ('entropy.bin', 'allocation-journal.jsonl')
                    for name in manifest), 'Premature allocation artifact')
    return dict(status='PracticalPresetAdmittedNoReservation', manifestSha256=pin, manifestFiles=len(manifest),
                requestSha256=sha(PACKAGE/'preset/request.json'), historicalValues=len(values), historyFiles=len(files),
                sourceDocuments=len(sources), jitResolvedMethods=len(context['jitResolvedMethods']),
                executionHash=context['executionHash'], cost=native['cost'], declaredValueCount=1041,
                scientificLaunches=0, newValues=0, fights=0, outputExists=False)


def prepare():
    require(not PACKAGE.exists() and not RUN.exists(), 'Use a new package and absent scientific output')
    started = time.monotonic()
    PACKAGE.mkdir()

    def check():
        require(time.monotonic()-started < SECONDS-2, 'Engineering preparation deadline reached')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES, 'Engineering storage ceiling reached')
        require(not RUN.exists(), 'Unexpected scientific output')

    def command(name, arguments):
        process = common.owned(PACKAGE/'bounded_windows_process.py', arguments, PACKAGE/(name+'.log'),
                               started+SECONDS-2, check)
        save(PACKAGE/(name+'-process.json'), process)

    try:
        captured = common.authenticate(PREVIOUS, PREVIOUS_PIN)
        common.authenticate(VERIFICATION, VERIFICATION_PIN)
        evidence = read(VERIFICATION/'verification.json')
        require(sha(BUILD/'BalanceHarness.dll') == evidence['runtimeDllSha256']
                and sha(BUILD/'BalanceHarness.pdb') == evidence['runtimePdbSha256'], 'Tested binaries changed')
        for name, digest in read(VERIFICATION/'source-files.json').items():
            require(sha(ROOT/name) == digest, 'Tested source changed: '+name)
        shutil.copytree(PREVIOUS/'runtime', PACKAGE/'runtime')
        shutil.copytree(PREVIOUS/'content', PACKAGE/'content')
        for name in common.HARNESS_FILES:
            shutil.copyfile(BUILD/name, PACKAGE/'runtime'/name)
        (PACKAGE/'capture').mkdir()
        for name in ('files.json', 'template.json', 'request.json'):
            shutil.copyfile(PREVIOUS/name, PACKAGE/'capture'/name)
        shutil.copytree(VERIFICATION, PACKAGE/'verification')
        shutil.copyfile(Path(__file__), PACKAGE/'prepare.py')
        shutil.copyfile(PREVIOUS/'prepare.py', PACKAGE/'comparison-preparation.py')
        shutil.copyfile(PREVIOUS/'bounded_windows_process.py', PACKAGE/'bounded_windows_process.py')
        context_source = (PREVIOUS/'context.ps1').read_text(encoding='utf-8-sig')
        marker = "@('TowerIncumbentTieComparison', 'TowerBossImprovement'"
        require(context_source.count(marker) == 1, 'Changed context helper entry list')
        context_source = context_source.replace(marker, "@('TowerPracticalSearch', 'TowerIncumbentTieComparison', 'TowerBossImprovement'")
        with (PACKAGE/'context.ps1').open('x', encoding='utf-8', newline='\n') as stream:
            stream.write(context_source)
        save(PACKAGE/'scope.json', dict(purpose='One captured-floor practical search with the explicit incumbent selector',
             primaryReferenceId=PRIMARY, allocation=ALLOCATION, maximumFights=3496,
             proposedSearchMaximumSeconds=1200, proposedSearchMaximumBytes=1073741824,
             engineeringMaximumSeconds=SECONDS, engineeringMaximumBytes=BYTES,
             launchesInPreparation=0, retries=0, resume=False, defaultsChanged=False))
        check()
        command('context', ['pwsh', '-NoProfile', '-File', str(PACKAGE/'context.ps1'),
                           '-Package', str(PACKAGE), '-RepositoryRoot', str(ROOT)])
        context = read(PACKAGE/'context.json')
        for name, digest in context['execution']['assemblyHashes'].items():
            if name != 'BalanceHarness':
                require(captured['runtime/'+name+'.dll'] == digest, 'Changed gameplay assembly')
        save(PACKAGE/'runtime-files.json', common.inventory(PACKAGE/'runtime'))
        files, values = common.history(RUN.parent, read(PREVIOUS/'request.json'))
        require(len(files) == 224 and len(values) == 538326, 'Live history differs from closed comparison')
        save(PACKAGE/'history-files.json', files)
        save(PACKAGE/'source-template.json', source_template(read(PREVIOUS/'template.json'), values, context))
        save(PACKAGE/'source-request.json', source_request(files))
        command('preset', ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'),
                          'tower-practical-search-incumbent-tie-preset', str(PACKAGE/'source-request.json'), PRIMARY, str(PACKAGE/'preset')])
        command('native-check', ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'),
                                'tower-practical-search-allocation-check', str(PACKAGE/'preset/request.json')])
        save(PACKAGE/'native-check.json', read(PACKAGE/'native-check.log'))
        current, current_values = common.history(RUN.parent, read(PREVIOUS/'request.json'))
        require(current == files and current_values == values, 'History changed during preparation')
        save(PACKAGE/'preparation.json', dict(status='ReadyNoReservation', seconds=time.monotonic()-started,
             sourcePackageManifest=PREVIOUS_PIN, sourceVerificationManifest=VERIFICATION_PIN,
             engineeringMaximumSeconds=SECONDS, engineeringMaximumBytes=BYTES,
             scientificLaunches=0, newValues=0, fights=0))
        check()
        save(PACKAGE/'files.json', common.inventory(PACKAGE))
        result = verify(sha(PACKAGE/'files.json'))
        check()
        save(PACKAGE.with_name(PACKAGE.name+'-pin.json'), result)
        print(json.dumps(result, indent=2))
    except BaseException as error:
        save(PACKAGE/'failure.json', dict(status='PreparationFailedNoLaunch', reason=str(error), scientificLaunches=0))
        raise


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare', 'verify'])
    parser.add_argument('--manifest-sha256')
    args = parser.parse_args()
    if args.command == 'prepare':
        prepare()
    else:
        require(args.manifest_sha256 is not None, 'Supply the externally recorded manifest pin')
        print(json.dumps(verify(args.manifest_sha256), indent=2))
