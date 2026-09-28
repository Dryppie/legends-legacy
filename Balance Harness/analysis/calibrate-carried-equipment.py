"""Own the fixed 344-party, four-setting carried-equipment calibration (44,032 fights)."""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-carried-equipment-20260928'
SOURCE_PIN = '76e028152ed3ccfb09e5e3bf60ff5ae3acabcb0a8f6db18a395806ed19f256f1'
VERSION = 'tower-floor11-carried-calibration-v1'
MULTIPLIERS = [1, 2, 4, 8]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('carried_calibration_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.absolute()) for p in (args.package, args.output, args.artifacts)]
    io.require(package.parent == output.parent == artifacts.parent == ROOT / 'TestResults'
               and not package.exists() and not output.exists(), 'New direct TestResults children required; no resume')
    pins = {}

    def pinned(path, expected=None):
        digest = io.sha(path)
        io.require(expected is None or expected == digest, 'Changed input: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    files = pinned(SOURCE / 'files.json', SOURCE_PIN)
    scope = pinned(SOURCE / 'scope.json', files['scope.json'])
    cells = pinned(SOURCE / 'carried-cells.json', files['carried-cells.json'])
    seeds = pinned(SOURCE / 'seeds.json', files['seeds.json'])
    result = pinned(SOURCE / 'result.json', files['result.json'])
    audit = pinned(ROOT / 'TestResults/tower-carried-equipment-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == SOURCE_PIN
               and audit['resultSha256'] == files['result.json'] and audit['all228CellsRetained']
               and audit['floor10PartiesMatched'] == 14 and audit['progressionPartiesMatched'] == 116,
               'Audited carry-forward source required')
    io.require(result['status'] == 'CarriedEquipmentScreenComplete' and result['fights'] == 14592
               and all(r['wins'] == 32 for r in result['rows']), 'Incomplete source')
    io.require(len(cells) == len({c['id'] for c in cells}) == 228
               and len(seeds) == len(set(seeds)) == 32
               and all(c['scenario']['seeds'][:32] == seeds for c in cells), 'Changed family or panel')
    ledger = SOURCE / 'carried/trials.jsonl'
    io.require(io.sha(ledger) == files['carried/trials.jsonl'], 'Changed source schedule')
    pins[str(ledger)] = files['carried/trials.jsonl']
    for name, digest in scope['contentHashes'].items():
        captured = SOURCE / 'content/Data' / name
        current = ROOT / 'LL/src/API/API.LL/Data' / name
        io.require(io.sha(captured) == io.sha(current) == digest, 'Changed live/captured content: ' + name)
        pins[str(captured)] = pins[str(current)] = digest
    floors = io.read(SOURCE / 'content/Data/world-tower/tower-floors.json')['floors']
    scaling = next(f for f in floors if f['floorNumber'] == 11)['guardianScaling']
    io.require(scaling['health'] == 6.525 and scaling['offense'] == 8.37, 'Changed baseline setting')
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    assemblies = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in assemblies.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch')
        pins[str(runtime / (name + '.dll'))] = pins[str(tests / (name + '.dll'))] = digest
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN,
                   runtime=str(runtime), inputHashes=pins, assemblyHashes=assemblies)
    io.write(package / 'request.json', request)
    sources = [Path(__file__), Path(__file__).with_name('verify-carried-calibration.py'),
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessCarriedCalibrationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'LL/tools/BalanceHarness/TowerProgressionUpgrades.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), multipliers=MULTIPLIERS,
                       health=[round(6.525*m, 6) for m in MULTIPLIERS], offense=[round(8.37*m, 6) for m in MULTIPLIERS],
                       cells=344, retainedCells=112, levelControls=4, additions=228, samples=32, variants=4,
                       importedCells=228, addedArmorCells=116, seeds=seeds,
                       maximumFights=44032, qualificationFightsIncluded=7296, maximumSeconds=1860,
                       maximumBytes=2*1073741824, newSeeds=0, retries=0,
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
                       rule='Lowest declared multiplier with strongest seven-Essence cell at 4-16/32 and every lower-Essence cell below 4/32. Diagnostic only. Retain all 228 carried cells plus both armor parents upgraded to level 60 with each legal uniform seventh Essence and a six-Essence level control.',
                       stopRule='Complete all four settings without adaptation, extension, fresh seeds, retries, confirmation or application.',
                       sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('carried_calibration_owner', ROOT / 'build/bounded_windows_process.py')
    previous = os.environ.get('LL_CARRIED_CALIBRATION')
    os.environ['LL_CARRIED_CALIBRATION'] = str(package / 'request.json')
    try:
        command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
                   '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessCarriedCalibrationTests']
        process = owner.run(command, ROOT, package / 'calibration.log', time.monotonic() + 1860, log_byte_limit=1048576)
        io.write(package / 'process.json', process)
        io.require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
                   'Calibration failed; preserve evidence, no retry')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test executable changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'CarriedCalibrationComplete' and result['fights'] == 44032, 'Incomplete comparison')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2*1073741824, 'Storage limit exceeded')
        io.write(package / 'completion.json', dict(status='Complete', fights=44032, newSeeds=0, retries=0,
                 resultSha256=io.sha(output / 'result.json'), archiveManifestSha256=io.sha(output / 'files.json'),
                 decision=result['decision']))
        print('Completed 44,032 historical fights: ' + result['decision'], flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_CARRIED_CALIBRATION', None)
        else:
            os.environ['LL_CARRIED_CALIBRATION'] = previous


if __name__ == '__main__':
    main()
