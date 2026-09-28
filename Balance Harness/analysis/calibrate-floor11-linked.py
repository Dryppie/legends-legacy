"""Own one frozen floor-11 linked Health/Power screen: 21,504 historical-seed fights.

Six settings, all sixteen retained teams and seven profiles. No allocation,
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
SOURCE = ROOT / 'TestResults/tower-equipment-cycle-screen-20260928'
SOURCE_PIN = '79fd4c8f4db16d4b98dd3b78dd0a53224499a0a85ed6ba92cc0cceeacffc5abe'
VERSION = 'tower-floor11-linked-calibration-v1'
MULTIPLIERS = [1, 1.125, 1.25, 1.5, 2, 3]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor11_linked_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


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
    scope = pinned(SOURCE / 'scope.json', files['scope.json'])
    cells = [c for c in pinned(SOURCE / 'cells.json', files['cells.json']) if c['floor'] == 11]
    previous = pinned(SOURCE / 'request.json', files['request.json'])
    result = pinned(SOURCE / 'result.json', files['result.json'])
    io.require(result['status'] == 'EquipmentCycleScreenComplete', 'Incomplete baseline')
    io.require(len(cells) == 112 and len({c['case'] for c in cells}) == 16, 'All sixteen teams and seven profiles required')
    io.require([sum(c['essenceSlots'] == s for c in cells) for s in (7, 6, 4)] == [84, 14, 14], 'Changed cohorts')
    io.require(len(previous['seeds']) == len(set(previous['seeds'])) == 32 and all(c['scenario']['seeds'] == previous['seeds'] for c in cells), 'Changed historical seeds')
    for name, digest in scope['contentHashes'].items():
        for path in (ROOT / 'LL/src/API/API.LL/Data' / name, SOURCE / 'content/Data' / name):
            io.require(io.sha(path) == digest, 'Gameplay content changed: ' + str(path))
            pins[str(path)] = digest
    floors = io.read(SOURCE / 'content/Data/world-tower/tower-floors.json')
    scaling = next(f for f in floors['floors'] if f['floorNumber'] == 11)['guardianScaling']
    io.require(scaling['health'] == 2.90 and scaling['offense'] == 3.72, 'Changed floor-11 baseline')
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    hashes = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in hashes.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch: ' + name)
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN, runtime=str(runtime),
                   inputHashes=pins, assemblyHashes=hashes)
    io.write(package / 'request.json', request)
    sources = [Path(__file__), Path(__file__).with_name('verify-floor11-linked.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessFloor11CalibrationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), multipliers=MULTIPLIERS,
                       health=[round(2.90 * m, 6) for m in MULTIPLIERS], offense=[round(3.72 * m, 6) for m in MULTIPLIERS],
                       cases=16, profiles=7, cells=112, samples=32, variants=6, maximumFights=21504,
                       qualificationFightsIncluded=3584, maximumSeconds=900, maximumBytes=2 * 1073741824,
                       newSeeds=0, retries=0, testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
                       rule='Choose the lowest multiplier with strongest seven-Essence team at 4–16/32 and all four-/six-Essence controls below 4/32. Descriptive separation screen; not balance acceptance or a new production restriction.',
                       stopRule='Complete all six settings. Stop after the fixed grid, even if none qualifies; no interpolation, extra seeds, retry, tuning or automatic confirmation.',
                       sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('floor11_linked_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessFloor11CalibrationTests']
    prior = os.environ.get('LL_FLOOR11_CALIBRATION')
    os.environ['LL_FLOOR11_CALIBRATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'calibration.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Calibration failed; preserve evidence, do not retry fights')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Floor11CalibrationComplete' and result['fights'] == 21504, 'Incomplete calibration')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2 * 1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=21504, newSeeds=0, retries=0))
        print('Completed 21,504 historical-seed fights: ' + str(output), flush=True)
        print(result['decision'], result['selected'], flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_FLOOR11_CALIBRATION', None)
        else:
            os.environ['LL_FLOOR11_CALIBRATION'] = prior


if __name__ == '__main__':
    main()
