"""Confirm selected floor-10 factor 7.75 on 256 fresh seeds for all 21 cells."""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-floor10-refined-20260928'
HISTORY = ROOT / 'TestResults/balance/tower-floor11-higher-setting-20260928'
HISTORY_PIN = '41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378'
VERSION = 'tower-floor10-fixed-family-confirmation-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor10_confirmation_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, type=Path, required=True)
    parser.add_argument('--source-pin', required=True)
    parser.add_argument('--master', type=int, default=2026092816)
    args = parser.parse_args()
    io.require(args.source_pin == 'b866bb74e5aa897dcd241423208fe53a47bdf5a583846434a07c460f6f6a015f',
               'The captured finer calibration is fixed for this follow-up')
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
    audit = pinned(ROOT / 'TestResults/tower-floor10-refined-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == args.source_pin
               and audit['resultSha256'] == files['result.json'], 'Audited calibration required')
    choices = [s for s in result['summaries'] if s['multiplier'] == 7.75]
    io.require(result['status'] == 'RefinedFloor10CalibrationComplete' and len(choices) == 1,
               'Completed fixed calibration and unique factor 7.75 required')
    selected = choices[0]
    io.require(selected['eligible'] and selected['variant'] == 7 and selected['health'] == 12.71
               and selected['offense'] == 7.13 and selected == result['selected'], 'Changed preselected setting')
    variant = f"variant-{selected['variant']:02}"
    scope = pinned(SOURCE / variant / 'scope.json', files[variant + '/scope.json'])
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    io.require(len(cells) == len({c['case'] + "/" + c['profile'] for c in cells}) == 21, 'Full family required')
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
    prior_result = pinned(HISTORY / 'result.json', history_files['result.json'])
    prior_selected = pinned(HISTORY / 'selected.json', history_files['selected.json'])
    prior_audit = pinned(ROOT / 'TestResults/tower-floor11-higher-setting-owner-20260928/independent-audit.json')
    io.require(prior_audit['status'] == 'Verified' and prior_audit['assessment'] == prior_result['assessment'] == 'Pass'
               and prior_audit['archiveManifestSha256'] == HISTORY_PIN
               and prior_audit['resultSha256'] == history_files['result.json']
               and prior_result['status'] == 'Floor11FamilyConfirmationComplete'
               and prior_result['fights'] == 58368 and prior_result['exclusionUnion'] == 834807
               and prior_selected['multiplier'] == 2.25, 'Latest audited historical exclusion source required')
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
    sources = [Path(__file__), Path(__file__).with_name('verify-floor10-family-confirmation.py'),
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10FamilyConfirmationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityTeamConfirmationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), selected=selected,
                       historyManifest=HISTORY_PIN,
                       cells=21, intendedCells=21, controlCells=0, samples=256, freshSeeds=256,
                       maximumFights=5376, maximumSeconds=900, maximumBytes=2 * 1073741824, retries=0,
                       intervalPolicy='bonferroni-wilson-95-v1', intervalFamilySize=21,
                       rule='All 21 upper bounds <= .50 and some lower bound >= .10. Any observed rate > .50 or all upper bounds < .10 fail; otherwise unresolved evidence is Inconclusive. No lower-Essence controls or necessity claim. Approximate simultaneous coverage for this fixed study, not across repeated studies; no pooled history or reselection.',
                       stopRule='Run the entire fixed family once; report Pass, Fail or Inconclusive. No extension, retry, retuning or production application.',
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'), sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('floor10_confirmation_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessFloor10FamilyConfirmationTests']
    prior = os.environ.get('LL_FLOOR10_FAMILY_CONFIRMATION')
    os.environ['LL_FLOOR10_FAMILY_CONFIRMATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'confirmation.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Confirmation failed; retain evidence and reservations; no retry')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Floor10FamilyConfirmationComplete' and result['fights'] == 5376, 'Incomplete family')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2 * 1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=5376, freshSeeds=256, retries=0,
                 assessment=result['assessment']))
        print('Completed 5,376 fresh confirmation fights: ' + result['assessment'], flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_FLOOR10_FAMILY_CONFIRMATION', None)
        else:
            os.environ['LL_FLOOR10_FAMILY_CONFIRMATION'] = prior


if __name__ == '__main__':
    main()
