"""Own one frozen floor-10 refined Health/Power screen: 6,048 historical-seed fights.

Nine settings, both boundary panels first, all three retained teams and seven profiles. No allocation,
adaptive extension, combat retries, confirmation or production mutation.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-floor10-linked-20260928'
SOURCE_PIN = 'c67912fac6fa2f72b82676859c9f2cb525585d0cd6200f604b86d808c432bcf1'
VERSION = 'tower-floor10-refined-calibration-v1'
MULTIPLIERS = [6, 6.25, 6.5, 6.75, 7, 7.25, 7.5, 7.75, 8]
EXECUTION_ORDER = [0, 8, 1, 2, 3, 4, 5, 6, 7]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor10_refined_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.resolve()) for p in (args.package, args.output, args.artifacts)]
    io.require(not package.exists() and not output.exists(), 'Choose new directories; no resume')
    io.require(not output.is_relative_to(ROOT / 'TestResults/balance'), 'Historical diagnostics stay outside the fresh-seed registry')
    pins = {}

    def pinned(path, expected=None):
        digest = io.sha(path)
        io.require(expected is None or expected == digest, 'Changed input: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    files = pinned(SOURCE / 'files.json', SOURCE_PIN)
    scope = pinned(SOURCE / 'source-scope.json', files['source-scope.json'])
    cells = [c for c in pinned(SOURCE / 'cells.json', files['cells.json']) if c['floor'] == 10]
    pinned(SOURCE / 'request.json', files['request.json'])
    result = pinned(SOURCE / 'result.json', files['result.json'])
    io.require(result['status'] == 'Floor10CalibrationComplete' and result['decision'] == 'NoSettingInGrid' and result['fights'] == 5376, 'Completed broad grid required')
    io.require(len(cells) == 21 and len({c['case'] for c in cells}) == 3, 'All three teams and seven profiles required')
    io.require(all(c['essenceSlots'] == 6 and c['purpose'] == 'intended-progression' for c in cells), 'Changed intended family')
    seeds = cells[0]['scenario']['seeds']
    io.require(len(seeds) == len(set(seeds)) == 32 and all(c['scenario']['seeds'] == seeds for c in cells), 'Changed historical seeds')
    audit = pinned(ROOT / 'TestResults/tower-floor10-linked-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == SOURCE_PIN
               and audit['resultSha256'] == files['result.json'], 'Audited broad grid required')
    for index, factor, best in ((5, 6, 32), (6, 8, 0)):
        summary = pinned(SOURCE / f'variant-{index:02}/summary.json', files[f'variant-{index:02}/summary.json'])
        io.require(summary['multiplier'] == factor and summary['bestIntendedWins'] == best and not summary['eligible'], 'Changed boundary result')
    api = ROOT / 'LL/src/API/API.LL'
    current = scope['contentHashes']
    for name, digest in current.items():
        captured = SOURCE / 'variant-00/content/Data' / name
        local = api / 'Data' / name
        io.require(io.sha(captured) == digest == io.sha(local), 'Current or captured content changed: ' + name)
        pins[str(captured)] = pins[str(local)] = digest
    floors = io.read(api / 'Data/world-tower/tower-floors.json')
    scaling = next(f for f in floors['floors'] if f['floorNumber'] == 10)['guardianScaling']
    io.require(scaling['health'] == 1.64 and scaling['offense'] == .92, 'Changed floor-10 baseline')
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    hashes = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in hashes.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch: ' + name)
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN, runtime=str(runtime),
                   apiRoot=str(api), currentContentHashes=current, inputHashes=pins, assemblyHashes=hashes)
    io.write(package / 'request.json', request)
    sources = [Path(__file__), Path(__file__).with_name('verify-floor10-refined.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessRefinedFloor10CalibrationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), multipliers=MULTIPLIERS, executionOrder=EXECUTION_ORDER,
                       health=[round(1.64 * m, 6) for m in MULTIPLIERS], offense=[round(.92 * m, 6) for m in MULTIPLIERS],
                       cases=3, profiles=7, cells=21, samples=32, variants=9, maximumFights=6048,
                       qualificationFightsIncluded=1344, maximumSeconds=900, maximumBytes=2 * 1073741824,
                       newSeeds=0, retries=0, testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
                       rule='Choose the lowest multiplier with strongest retained six-Essence team at 4–16/32. Every one of the 21 cells remains. Historical range screen; no lower-budget separation or balance acceptance.',
                       stopRule='Complete both 672-fight boundary panels first, then all seven intermediate settings. Stop after the fixed grid, even if none qualifies; no interpolation, extra seeds, retry, tuning or automatic confirmation.',
                       sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('floor10_refined_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessRefinedFloor10CalibrationTests']
    prior = os.environ.get('LL_REFINED_FLOOR10_CALIBRATION')
    os.environ['LL_REFINED_FLOOR10_CALIBRATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'calibration.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Calibration failed; preserve evidence, do not retry fights')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'RefinedFloor10CalibrationComplete' and result['fights'] == 6048 and result['runtimeParityReports'] == 1344, 'Incomplete calibration')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2 * 1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=6048, newSeeds=0, retries=0))
        print('Completed 6,048 historical-seed fights: ' + str(output), flush=True)
        print(result['decision'], result['selected'], flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_REFINED_FLOOR10_CALIBRATION', None)
        else:
            os.environ['LL_REFINED_FLOOR10_CALIBRATION'] = prior


if __name__ == '__main__':
    main()
