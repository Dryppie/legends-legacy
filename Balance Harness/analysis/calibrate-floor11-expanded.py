"""Own the complete expanded floor-11 family across six linked settings (43,776 fights).

Historical seeds only; no adaptation, retries, confirmation or production edits.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-seventh-upgrade-20260928'
SOURCE_PIN = 'c69c0624b45912926bfd3e8bfc02aaa6d1a3f5cf2b8c1ca1ed8e3b4e193f67e8'
VERSION = 'tower-floor11-expanded-calibration-v1'
MULTIPLIERS = [1.5, 1.625, 1.75, 1.875, 2, 2.5]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('expanded_floor11_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


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
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    result = pinned(SOURCE / 'result.json', files['result.json'])
    previous = pinned(SOURCE / 'request.json', files['request.json'])
    audit = pinned(ROOT / 'TestResults/tower-seventh-upgrade-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == SOURCE_PIN
               and audit['resultSha256'] == files['result.json'], 'Unaudited upgrade source')
    io.require(result['status'] == 'ProgressionUpgradeScreenComplete' and result['fights'] == 7296, 'Incomplete source')
    io.require(len(cells) == len({c['id'] for c in cells}) == 228, 'Missing expanded family')
    io.require([sum(c['kind'] == k for c in cells) for k in ('retained-control', 'level-control', 'seventh-addition')]
               == [112, 2, 114], 'Missing original controls or additions')
    io.require([sum(len(c['scenario']['party'][0]['build']['essenceIds']) == s for c in cells) for s in (7, 6, 4)]
               == [198, 16, 14], 'Changed cohorts')
    seeds = cells[0]['scenario']['seeds']
    io.require(len(seeds) == len(set(seeds)) == 32 and all(c['scenario']['seeds'] == seeds for c in cells), 'Changed historical seeds')
    ancestor = Path(previous['source'])
    ancestor_files = pinned(ancestor / 'files.json', previous['sourcePin'])
    floor_file = 'world-tower/tower-floors.json'
    for name, digest in scope['contentHashes'].items():
        captured = SOURCE / 'content/Data' / name
        current = ROOT / 'LL/src/API/API.LL/Data' / name
        io.require(io.sha(captured) == digest, 'Changed captured content: ' + name)
        expected_current = ancestor_files['variant-00/content/Data/' + name] if name == floor_file else digest
        io.require(io.sha(current) == expected_current, 'Changed current production source: ' + name)
        pins[str(captured)], pins[str(current)] = digest, expected_current
    scaling = next(f for f in io.read(SOURCE / 'content/Data' / floor_file)['floors'] if f['floorNumber'] == 11)['guardianScaling']
    io.require(scaling['health'] == 4.35 and scaling['offense'] == 5.58, 'Wrong baseline diagnostic setting')
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    hashes = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in hashes.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch: ' + name)
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN,
                   runtime=str(runtime), inputHashes=pins, assemblyHashes=hashes)
    io.write(package / 'request.json', request)
    sources = [Path(__file__), Path(__file__).with_name('verify-floor11-expanded.py'),
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'),
               Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessExpandedFloor11CalibrationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), multipliers=MULTIPLIERS,
                       health=[round(2.90 * m, 6) for m in MULTIPLIERS], offense=[round(3.72 * m, 6) for m in MULTIPLIERS],
                       cells=228, retainedCells=112, levelControls=2, additions=114, samples=32, variants=6,
                       maximumFights=43776, qualificationFightsIncluded=7296, maximumSeconds=2160,
                       maximumBytes=2 * 1073741824, newSeeds=0, retries=0,
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
                       rule='Lowest multiplier with strongest seven-Essence team at 4–16/32 and every four-/six-Essence control below 4/32, including both level-60 six-Essence controls. Descriptive separation screen; not balance acceptance.',
                       stopRule='Complete all six settings, even if none qualifies. No interpolation, extra seeds, retry, tuning or automatic confirmation.',
                       sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('expanded_floor11_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessExpandedFloor11CalibrationTests']
    prior = os.environ.get('LL_EXPANDED_FLOOR11_CALIBRATION')
    os.environ['LL_EXPANDED_FLOOR11_CALIBRATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'calibration.log', time.monotonic() + 2160, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Calibration failed; retain evidence, do not retry fights')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'ExpandedFloor11CalibrationComplete' and result['fights'] == 43776, 'Incomplete calibration')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2 * 1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=43776, newSeeds=0, retries=0))
        print('Completed 43,776 historical-seed fights: ' + str(output), flush=True)
        print(result['decision'], result['selected'], flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_EXPANDED_FLOOR11_CALIBRATION', None)
        else:
            os.environ['LL_EXPANDED_FLOOR11_CALIBRATION'] = prior


if __name__ == '__main__':
    main()
