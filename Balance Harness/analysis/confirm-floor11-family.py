"""Confirm the audited finer-grid candidate on 256 fresh seeds for all 228 cells."""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-floor11-refined-20260928'
HISTORY = ROOT / 'TestResults/balance/tower-floor13-geared-search-20260928'
HISTORY_PIN = '8d8c049a5e284474613964f24174b753ed89a9d06454b6efcfd04e2e611c230e'
VERSION = 'tower-floor11-fixed-family-confirmation-v1'


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
    parser.add_argument('--master', type=int, default=2026092814)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.resolve()) for p in (args.package, args.output, args.artifacts)]
    registry = ROOT / 'TestResults/balance'
    io.require(not package.exists() and not output.exists() and output.parent == registry, 'New package and direct registry child required; no resume')
    pins = {}

    def pinned(path, expected=None):
        digest = io.sha(path)
        io.require(expected is None or digest == expected, 'Changed input: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    files = pinned(SOURCE / 'files.json', args.source_pin)
    result = pinned(SOURCE / 'result.json', files['result.json'])
    audit = pinned(ROOT / 'TestResults/tower-floor11-refined-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == args.source_pin
               and audit['resultSha256'] == files['result.json'], 'Audited calibration required')
    selected = result['selected']
    io.require(result['status'] == 'RefinedFloor11CalibrationComplete' and selected is not None
               and selected == next(s for s in result['summaries'] if s['eligible']), 'No qualifying preselected setting')
    variant = f"variant-{selected['variant']:02}"
    scope = pinned(SOURCE / variant / 'scope.json', files[variant + '/scope.json'])
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    io.require(len(cells) == len({c['id'] for c in cells}) == 228, 'Full family required')
    runtime = SOURCE / 'executable'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    for name, digest in scope['execution']['assemblyHashes'].items():
        io.require(io.sha(runtime / (name + '.dll')) == io.sha(tests / (name + '.dll')) == digest,
                   'Use the qualified captured game assemblies in the confirmation test directory: ' + name)
    for name, digest in scope['contentHashes'].items():
        path = SOURCE / variant / 'content/Data' / name
        io.require(io.sha(path) == digest, 'Changed selected content')
        pins[str(path)] = digest
    history_files = pinned(HISTORY / 'files.json', HISTORY_PIN)
    previous = pinned(HISTORY / 'request.json', history_files['request.json'])
    required = dict(previous['requiredHistory'])
    for name in ('seed-ledger.json', 'history-input.json'):
        path = HISTORY / name
        pinned(path, history_files[name])
        required[str(path)] = history_files[name]
    package.mkdir()
    request = dict(version=VERSION, source=str(SOURCE), sourcePin=args.source_pin, output=str(output),
                   registry=str(registry), runtime=str(runtime), historySource=str(HISTORY), historyPin=HISTORY_PIN,
                   master=args.master, inputHashes=pins, requiredHistory=required,
                   recoveries=previous['recoveries'], recoveryHashes=previous['recoveryHashes'])
    io.write(package / 'request.json', request)
    sources = [Path(__file__), Path(__file__).with_name('verify-floor11-family-confirmation.py'),
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessFloor11FamilyConfirmationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityTeamConfirmationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), selected=selected,
                       cells=228, intendedCells=198, controlCells=30, samples=256, freshSeeds=256,
                       maximumFights=58368, maximumSeconds=2460, maximumBytes=2 * 1073741824, retries=0,
                       intervalPolicy='bonferroni-wilson-95-v1', intervalFamilySize=228,
                       rule='All intended upper bounds <= .50, some intended lower bound >= .10, all control upper bounds < .10. Preserve observed intended > .50 and control >= .10 failures. Approximate simultaneous coverage; no pooled history or reselection.',
                       stopRule='Run the entire fixed family once; report Pass, Fail or Inconclusive. No extension, retry, retuning or production application.',
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'), sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('floor11_confirmation_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessFloor11FamilyConfirmationTests']
    prior = os.environ.get('LL_FLOOR11_FAMILY_CONFIRMATION')
    os.environ['LL_FLOOR11_FAMILY_CONFIRMATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'confirmation.log', time.monotonic() + 2460, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Confirmation failed; retain evidence and reservations; no retry')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Floor11FamilyConfirmationComplete' and result['fights'] == 58368, 'Incomplete family')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2 * 1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=58368, freshSeeds=256, retries=0,
                 assessment=result['assessment']))
        print('Completed 58,368 fresh confirmation fights: ' + result['assessment'], flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_FLOOR11_FAMILY_CONFIRMATION', None)
        else:
            os.environ['LL_FLOOR11_FAMILY_CONFIRMATION'] = prior


if __name__ == '__main__':
    main()
