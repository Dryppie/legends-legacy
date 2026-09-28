"""One bounded floor-3, calibrated floor-13 or floor-15 supported-search evaluation.

Build and test first through build/run-tests.ps1 with an isolated ArtifactsPath.
This owner freezes a pinned request, then runs that build without rebuilding.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/balance/tower-affinity-nomination-pilot-01-20260925'
SOURCE_PIN = 'a085d004823246466ec860876fcfdb370769cc76a71527a09c2b4a32dec79ee1'
HANDOFF = ROOT / 'TestResults/affinity-progression-screen-20260925/follow-up-inputs.json'
HANDOFF_PIN = 'a0481a2a3ad789339f3d8f2ea27cf121cbdad31b42cd61857b2f9dc0b1ee7460'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor_evaluation_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', required=True, type=Path, help='New request/process evidence directory')
    parser.add_argument('--output', required=True, type=Path, help='New direct child of TestResults/balance')
    parser.add_argument('--artifacts', required=True, type=Path, help='Previously tested isolated .NET artifacts directory')
    parser.add_argument('--floor', type=int, choices=(3, 13, 15), default=3)
    parser.add_argument('--benchmark-reference', type=int, choices=(1, 2, 3), default=1)
    parser.add_argument('--screen', type=Path, help='Completed current-runtime reference screen result; pins the selected benchmark')
    parser.add_argument('--screen-manifest-pin', help='Required floor-13 geared-reference archive manifest SHA-256')
    parser.add_argument('--master', type=int, default=2026092503, help='Declared deterministic allocator master; history is always excluded')
    parser.add_argument('--history', type=Path, help='Latest complete ledger; the native registry scan must independently match it')
    args = parser.parse_args()
    version = f'affinity-floor{args.floor}-baseline-evaluation-v1'
    io.require(args.floor not in (13, 15) or (args.screen is not None and args.history is not None),
               'Floors 13 and 15 require the completed current-runtime screen and latest history ledger')
    io.require((args.floor == 13) == (args.screen_manifest_pin is not None),
               'Only calibrated floor 13 uses the required screen manifest pin')
    package, output, artifacts = [io.unlinked(p) for p in (args.package, args.output, args.artifacts)]
    registry = ROOT / 'TestResults/balance'
    io.require(output.parent == registry and not output.exists(), 'Choose a new direct child of the complete balance registry')
    io.require(not package.exists(), 'No package overwrite or resume')
    io.require(io.sha(SOURCE / 'files.json') == SOURCE_PIN and io.sha(HANDOFF) == HANDOFF_PIN, 'Changed source pins')
    manifest = io.read(SOURCE / 'files.json')

    def pinned(name):
        path = io.member(SOURCE, name)
        io.require(io.sha(path) == manifest[name], 'Changed source: ' + name)
        return path

    old = io.read(pinned('request.json'))
    plan = pinned('search/root-01/control/racing/plan.json')
    scope = pinned('search/root-01/control/scope.json')
    source_ledger = pinned('seed-ledger.json')
    ledger = source_ledger if args.history is None else io.unlinked(args.history)
    delta = pinned('history-input.json')
    required = dict(old['requiredHistory'])
    required.update({str(p): io.sha(p) for p in (source_ledger, ledger, delta)})
    runtime = artifacts / 'bin/BalanceHarness/release'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    io.require((tests / 'EssenceSystem.Tests.dll').is_file() and (runtime / 'BalanceHarness.dll').is_file(), 'Build through run-tests.ps1 first')
    for name in ('BalanceHarness', 'Domain', 'Common', 'Application', 'Services.LL'):
        io.require(io.sha(runtime / (name + '.dll')) == io.sha(tests / (name + '.dll')), 'Test/retained runtime mismatch: ' + name)
    handoff, handoff_pin, content_root = HANDOFF, HANDOFF_PIN, None
    if args.floor == 13:
        screen_root = io.unlinked(args.screen.resolve().parent)
        io.require(args.screen.name == 'result.json' and io.sha(screen_root / 'files.json') == args.screen_manifest_pin,
                   'Changed geared-reference screen manifest')
        screen_files = io.read(screen_root / 'files.json')
        for name in ('result.json', 'scope.json', 'follow-up-inputs.json', 'freeze.json'):
            io.require(io.sha(screen_root / name) == screen_files[name], 'Changed geared-reference screen member: ' + name)
        handoff, handoff_pin = screen_root / 'follow-up-inputs.json', screen_files['follow-up-inputs.json']
        scope, content_root = screen_root / 'scope.json', screen_root / 'content'
    case = next(c['case'] for c in io.read(handoff)['cases'] if c['case']['floor'] == args.floor)
    if args.screen is not None:
        screen = io.read(io.unlinked(args.screen))
        io.require(screen['status'] == 'ScreenComplete', 'Incomplete reference screen')
        for name, digest in screen['execution']['assemblyHashes'].items():
            io.require(io.sha(runtime / (name + '.dll')) == digest, 'Runtime changed since the screen: ' + name)
        if args.floor == 13:
            io.require(screen['version'] == 'tower-floor13-geared-reference-screen-v1' and screen['eligible']
                       and screen['benchmarkReference'] == args.benchmark_reference
                       and len(screen['rows']) == 6 and max(r['wins'] for r in screen['rows']) <= 28,
                       'Geared references disqualify this context')
        screened = next(r for r in screen['rows'] if
                        (r['form'] == 'projected' if args.floor == 13 else r['floor'] == args.floor)
                        and r['reference'] == args.benchmark_reference)
        io.require(screened['sourceId'] == case['referenceIds'][args.benchmark_reference - 1]
                   and 4 <= screened['wins'] <= 28, 'Selected benchmark lacks a usable current-runtime screen')
    package.mkdir()
    request = dict(version=version, floor=args.floor, benchmarkReference=args.benchmark_reference,
                   output=str(output), registry=str(registry), runtime=str(runtime),
                   plan=str(plan), planHash=io.sha(plan), handoff=str(handoff), handoffHash=handoff_pin,
                   sourceScope=str(scope), sourceScopeHash=io.sha(scope), historyLedger=str(ledger),
                   historyLedgerHash=io.sha(ledger), requiredHistory=required, recoveries=old['pendingHistoryRecoveries'],
                   recoveryHashes=old['recoveryReceiptHashes'], master=args.master)
    if args.screen is not None:
        request.update(screen=str(io.unlinked(args.screen)), screenHash=io.sha(args.screen))
    if content_root is not None:
        request.update(contentRoot=str(content_root))
    io.write(package / 'request.json', request)
    io.write(package / 'declaration.json', dict(version=version, floor=args.floor, benchmarkReference=args.benchmark_reference,
             benchmarkSourceId=case['referenceIds'][args.benchmark_reference - 1], budget=case['budget'], partySize=case['partySize'],
             requestSha256=io.sha(package / 'request.json'),
             sourceManifestSha256=SOURCE_PIN, handoffSha256=handoff_pin, screenManifestSha256=args.screen_manifest_pin,
             roots=1, searchFights=528,
             heldoutTeams=5, heldoutSamples=128, maximumFights=1168, freshValues=237, retries=0,
             maximumProcessSeconds=900, maximumStudyBytes=1073741824,
             sourcePins={str(p.relative_to(ROOT)): io.sha(p) for p in [Path(__file__),
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
                 ROOT / 'LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentStatBudgetCatalog.cs']},
             testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
             interpretation='One exploratory current-runtime root; fixed original affinity policy and gate. Compare generated finalists with the strongest measured existing reference; heldout never changes selection. No confirmation or promotion.'))
    owner = module('floor_evaluation_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'),
               '-NoBuild', '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests']
    previous = os.environ.get('LL_AFFINITY_FLOOR_EVALUATION')
    os.environ['LL_AFFINITY_FLOOR_EVALUATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'evaluation.log', time.monotonic() + 900,
                            log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Evaluation process failed; preserve its complete evidence and reservation')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Complete' and result['fights'] == 1168, 'Incomplete evaluation')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=1168, freshValues=237, retries=0))
        print('Completed one 528-fight search and 640 held-out fights: ' + str(output), flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_AFFINITY_FLOOR_EVALUATION', None)
        else:
            os.environ['LL_AFFINITY_FLOOR_EVALUATION'] = previous


if __name__ == '__main__':
    main()
