"""Own one frozen floor-10 linked Health/Power screen: 5,376 historical-seed fights.

Eight settings, all three retained teams and seven profiles. No allocation,
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
VERSION = 'tower-floor10-linked-calibration-v1'
MULTIPLIERS = [1, 1.5, 2, 3, 4, 6, 8, 12]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor10_linked_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


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
    cells = [c for c in pinned(SOURCE / 'cells.json', files['cells.json']) if c['floor'] == 10]
    previous = pinned(SOURCE / 'request.json', files['request.json'])
    result = pinned(SOURCE / 'result.json', files['result.json'])
    io.require(result['status'] == 'EquipmentCycleScreenComplete', 'Incomplete baseline')
    io.require(len(cells) == 21 and len({c['case'] for c in cells}) == 3, 'All three teams and seven profiles required')
    io.require(all(c['essenceSlots'] == 6 and c['purpose'] == 'intended-progression' for c in cells), 'Changed intended family')
    io.require(len(previous['seeds']) == len(set(previous['seeds'])) == 32 and all(c['scenario']['seeds'] == previous['seeds'] for c in cells), 'Changed historical seeds')
    audit = pinned(ROOT / 'TestResults/tower-equipment-cycle-screen-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == SOURCE_PIN
               and audit['resultSha256'] == files['result.json'], 'Audited source baseline required')
    api = ROOT / 'LL/src/API/API.LL'
    applied = ROOT / 'TestResults/tower-floor11-application-20260928'
    applied_pin = 'a499d45ac176b3a9e13118b6cc1cabd28943685641ce7b78af0e7e99aba5cb2d'
    applied_files = pinned(applied / 'files.json', applied_pin)
    applied_scope = pinned(applied / 'scope.json', applied_files['scope.json'])
    application_audit = pinned(ROOT / 'TestResults/tower-floor11-application-owner-20260928/independent-audit.json')
    io.require(application_audit['status'] == 'Verified' and application_audit['archiveManifestSha256'] == applied_pin,
               'Audited floor-11 application required')
    current = applied_scope['contentHashes']
    io.require(current.keys() == scope['contentHashes'].keys() and applied_scope['settings'] == scope['settings'], 'Changed content/settings inventory')
    for name, digest in scope['contentHashes'].items():
        path = SOURCE / 'content/Data' / name
        io.require(io.sha(path) == digest, 'Captured source changed: ' + name)
        pins[str(path)] = digest
        local = api / 'Data' / name
        io.require(io.sha(local) == current[name], 'Current content differs from checked application: ' + name)
        pins[str(local)] = current[name]
        if name != 'world-tower/tower-floors.json':
            io.require(digest == current[name], 'Unrelated content changed: ' + name)
    floors = io.read(api / 'Data/world-tower/tower-floors.json')
    expected = io.read(SOURCE / 'content/Data/world-tower/tower-floors.json')
    next(f for f in expected['floors'] if f['floorNumber'] == 11)['guardianScaling'].update(health=6.525, offense=8.37)
    io.require(floors == expected, 'Only the checked floor-11 application may differ from the historical source')
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
    sources = [Path(__file__), Path(__file__).with_name('verify-floor10-linked.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10CalibrationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), multipliers=MULTIPLIERS,
                       health=[round(1.64 * m, 6) for m in MULTIPLIERS], offense=[round(.92 * m, 6) for m in MULTIPLIERS],
                       cases=3, profiles=7, cells=21, samples=32, variants=8, maximumFights=5376,
                       qualificationFightsIncluded=672, maximumSeconds=900, maximumBytes=2 * 1073741824,
                       newSeeds=0, retries=0, testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
                       rule='Choose the lowest multiplier with strongest retained six-Essence team at 4–16/32. Every one of the 21 cells remains. Historical range screen; no lower-budget separation or balance acceptance.',
                       stopRule='Complete all eight settings. Stop after the fixed grid, even if none qualifies; no interpolation, extra seeds, retry, tuning or automatic confirmation.',
                       sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('floor10_linked_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessFloor10CalibrationTests']
    prior = os.environ.get('LL_FLOOR10_CALIBRATION')
    os.environ['LL_FLOOR10_CALIBRATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'calibration.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Calibration failed; preserve evidence, do not retry fights')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Floor10CalibrationComplete' and result['fights'] == 5376, 'Incomplete calibration')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2 * 1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=5376, newSeeds=0, retries=0))
        print('Completed 5,376 historical-seed fights: ' + str(output), flush=True)
        print(result['decision'], result['selected'], flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_FLOOR10_CALIBRATION', None)
        else:
            os.environ['LL_FLOOR10_CALIBRATION'] = prior


if __name__ == '__main__':
    main()
