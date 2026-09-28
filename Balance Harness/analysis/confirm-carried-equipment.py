"""Qualify the current runtime, then confirm the lowest eligible carried-equipment setting on 256 fresh seeds for all 344 cells."""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-carried-refined-20260928'
HISTORY = ROOT / 'TestResults/balance/tower-floor10-family-confirmation-20260928'
HISTORY_PIN = '95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944'
VERSION = 'tower-floor11-carried-confirmation-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor11_confirmation_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, type=Path, required=True)
    parser.add_argument('--source-pin', required=True)
    parser.add_argument('--master', type=int, default=2026092817)
    args = parser.parse_args()
    io.require(args.master == 2026092817, 'The declared confirmation master is fixed')
    package, output, artifacts = [io.unlinked(p.absolute()) for p in (args.package, args.output, args.artifacts)]
    registry = ROOT / 'TestResults/balance'
    io.require(not package.exists() and not output.exists() and output.parent == registry
               and package.parent == artifacts.parent == ROOT / 'TestResults', 'New package and direct registry child required; no resume')
    pins = {}

    def pinned(path, expected=None):
        digest = io.sha(path)
        io.require(expected is None or digest == expected, 'Changed input: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    files = pinned(SOURCE / 'files.json', args.source_pin)
    result = pinned(SOURCE / 'result.json', files['result.json'])
    audit = pinned(ROOT / 'TestResults/tower-carried-refined-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == args.source_pin
               and audit['resultSha256'] == files['result.json'], 'Audited calibration required')
    grid = [2, 2.25, 2.5, 2.75, 3, 3.5]
    io.require(result['status'] == 'RefinedCarriedCalibrationComplete' and result['fights'] == 66048
               and [s['multiplier'] for s in result['summaries']] == grid, 'Completed fixed carried calibration required')
    choices = [s for s in result['summaries'] if s['eligible']]
    io.require(choices and choices[0] == result['selected'], 'Lowest eligible candidate required; no alternate selection')
    selected = choices[0]
    io.require(selected['variant'] == grid.index(selected['multiplier'])
               and selected['health'] == round(6.525 * selected['multiplier'], 6)
               and selected['offense'] == round(8.37 * selected['multiplier'], 6), 'Changed selected setting')
    variant = f"variant-{selected['variant']:02}"
    scope = pinned(SOURCE / variant / 'scope.json', files[variant + '/scope.json'])
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    io.require(len(cells) == len({c['id'] for c in cells}) == 344, 'Full family required')
    ledger = SOURCE / variant / 'study/trials.jsonl'
    io.require(io.sha(ledger) == files[variant + '/study/trials.jsonl'], 'Changed qualification schedule')
    pins[str(ledger)] = files[variant + '/study/trials.jsonl']
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    assemblies = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in assemblies.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Current runtime/test mismatch')
        pins[str(runtime / (name + '.dll'))] = pins[str(tests / (name + '.dll'))] = digest
    source_request = pinned(SOURCE / 'request.json', files['request.json'])
    live_root = ROOT / 'LL/src/API/API.LL/Data'
    live_pins = {n: h for n, h in source_request['inputHashes'].items() if Path(n).is_relative_to(live_root)}
    io.require(len(live_pins) == len(scope['contentHashes']) == 29, 'Complete current content pins required')
    for name, digest in live_pins.items():
        io.require(io.sha(Path(name)) == digest, 'Current content changed since calibration: ' + name)
        pins[name] = digest
    for name, digest in scope['contentHashes'].items():
        path = SOURCE / variant / 'content/Data' / name
        io.require(io.sha(path) == digest, 'Changed selected content')
        pins[str(path)] = digest
    history_files = pinned(HISTORY / 'files.json', HISTORY_PIN)
    previous = pinned(HISTORY / 'request.json', history_files['request.json'])
    prior_result = pinned(HISTORY / 'result.json', history_files['result.json'])
    prior_selected = pinned(HISTORY / 'selected.json', history_files['selected.json'])
    prior_audit = pinned(ROOT / 'TestResults/tower-floor10-family-confirmation-owner-20260928/independent-audit.json')
    io.require(prior_audit['status'] == 'Verified' and prior_audit['assessment'] == prior_result['assessment'] == 'Pass'
               and prior_audit['archiveManifestSha256'] == HISTORY_PIN
               and prior_audit['resultSha256'] == history_files['result.json']
               and prior_result['status'] == 'Floor10FamilyConfirmationComplete'
               and prior_result['fights'] == 5376 and prior_result['exclusionUnion'] == 835063
               and prior_selected['multiplier'] == 7.75, 'Latest audited complete seed history required')
    required = dict(previous['requiredHistory'])
    for name in ('seed-ledger.json', 'history-input.json'):
        path = HISTORY / name
        pinned(path, history_files[name])
        required[str(path)] = history_files[name]
    package.mkdir()
    request = dict(version=VERSION, source=str(SOURCE), sourcePin=args.source_pin, output=str(output),
                   registry=str(registry), runtime=str(runtime), historySource=str(HISTORY), historyPin=HISTORY_PIN,
                   master=args.master, inputHashes=pins, assemblyHashes=assemblies, requiredHistory=required,
                   recoveries=previous['recoveries'], recoveryHashes=previous['recoveryHashes'])
    io.write(package / 'request.json', request)
    sources = [Path(__file__), Path(__file__).with_name('verify-carried-confirmation.py'),
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessCarriedConfirmationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityTeamConfirmationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), selected=selected,
                       historyAssessment='Pass', historyManifest=HISTORY_PIN,
                       cells=344, intendedCells=312, controlCells=32, samples=256, freshSeeds=256,
                       maximumFights=99072, qualificationFights=11008, confirmationFights=88064,
                       maximumSeconds=4260, maximumBytes=4 * 1073741824, retries=0,
                       intervalPolicy='bonferroni-wilson-95-v1', intervalFamilySize=344,
                       rule='All intended upper bounds <= .50, some intended lower bound >= .10, all control upper bounds < .10. Preserve observed intended > .50 and control >= .10 failures. Approximate simultaneous coverage for this fixed study, not across repeated studies; no pooled history or reselection.',
                       stopRule='Run the entire fixed family once; report Pass, Fail or Inconclusive. No extension, retry, retuning or production application.',
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'), sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('floor11_confirmation_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessCarriedConfirmationTests']
    prior = os.environ.get('LL_CARRIED_CONFIRMATION')
    os.environ['LL_CARRIED_CONFIRMATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'confirmation.log', time.monotonic() + 4260, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Confirmation failed; retain evidence and reservations; no retry')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'CarriedConfirmationComplete' and result['fights'] == 99072
                   and result['confirmationFights'] == 88064 and result['runtimeParityReports'] == 11008, 'Incomplete family')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 4 * 1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=99072, confirmationFights=88064, runtimeParityReports=11008, freshSeeds=256, retries=0,
                 assessment=result['assessment']))
        print('Completed 11,008 qualification and 88,064 fresh confirmation fights: ' + result['assessment'], flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_CARRIED_CONFIRMATION', None)
        else:
            os.environ['LL_CARRIED_CONFIRMATION'] = prior


if __name__ == '__main__':
    main()
