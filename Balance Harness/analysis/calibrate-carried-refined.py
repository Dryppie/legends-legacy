"""Own the fixed 344-party, six-setting finer carried-equipment calibration (66,048 fights)."""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-carried-calibration-20260928'
SOURCE_PIN = '4a9f8b8d2cca36888f3fb79e44a6225c54c30733f1e580510a6a7deffd0de6f3'
VERSION = 'tower-floor11-refined-carried-calibration-v1'
MULTIPLIERS = [2, 2.25, 2.5, 2.75, 3, 3.5]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('refined_carried_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


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
    scope = pinned(SOURCE / 'variant-01/scope.json', files['variant-01/scope.json'])
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    seeds = cells[0]['scenario']['seeds'][:32]
    result = pinned(SOURCE / 'result.json', files['result.json'])
    audit = pinned(ROOT / 'TestResults/tower-carried-calibration-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == SOURCE_PIN
               and audit['resultSha256'] == files['result.json'] and audit['all228ImportedRecipesPreserved']
               and audit['armorUpgradesReconstructed'] == 116, 'Audited complete carried calibration required')
    io.require(result['status'] == 'CarriedCalibrationComplete' and result['fights'] == 44032
               and result['selected'] is None, 'Completed coarse grid required')
    io.require(len(cells) == len({c['id'] for c in cells}) == 344
               and len(seeds) == len(set(seeds)) == 32
               and all(c['scenario']['seeds'][:32] == seeds for c in cells), 'Changed family or panel')
    ledger = SOURCE / 'variant-01/study/trials.jsonl'
    io.require(io.sha(ledger) == files['variant-01/study/trials.jsonl'], 'Changed source schedule')
    pins[str(ledger)] = files['variant-01/study/trials.jsonl']
    live_scope = pinned(SOURCE / 'variant-00/scope.json', files['variant-00/scope.json'])
    for name, digest in scope['contentHashes'].items():
        captured = SOURCE / 'variant-01/content/Data' / name
        current = ROOT / 'LL/src/API/API.LL/Data' / name
        io.require(io.sha(captured) == digest and io.sha(current) == live_scope['contentHashes'][name], 'Changed live/captured content: ' + name)
        pins[str(captured)] = digest
        pins[str(current)] = live_scope['contentHashes'][name]
        if name != 'world-tower/tower-floors.json':
            io.require(digest == live_scope['contentHashes'][name], 'Changed non-floor content')
    floors = io.read(SOURCE / 'variant-01/content/Data/world-tower/tower-floors.json')
    scaling = next(f for f in floors['floors'] if f['floorNumber'] == 11)['guardianScaling']
    io.require(scaling['health'] == 13.05 and scaling['offense'] == 16.74, 'Changed 2x baseline')
    scaling.update(health=6.525, offense=8.37)
    io.require(floors == io.read(ROOT / 'LL/src/API/API.LL/Data/world-tower/tower-floors.json'), 'Unexpected live floor changes')
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    assemblies = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in assemblies.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch')
        pins[str(runtime / (name + '.dll'))] = pins[str(tests / (name + '.dll'))] = digest
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN,
                   runtime=str(runtime), inputHashes=pins, assemblyHashes=assemblies)
    io.write(package / 'request.json', request)
    sources = [Path(__file__), Path(__file__).with_name('verify-carried-refined.py'),
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessRefinedCarriedCalibrationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'LL/tools/BalanceHarness/TowerProgressionUpgrades.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), multipliers=MULTIPLIERS,
                       health=[round(6.525*m, 6) for m in MULTIPLIERS], offense=[round(8.37*m, 6) for m in MULTIPLIERS],
                       cells=344, retainedCells=112, levelControls=4, additions=228, samples=32, variants=6,
                       importedCells=344, seeds=seeds,
                       maximumFights=66048, qualificationFightsIncluded=11008, maximumSeconds=2760,
                       maximumBytes=3*1073741824, newSeeds=0, retries=0,
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
                       rule='Lowest declared multiplier with strongest seven-Essence cell at 4-16/32 and every lower-Essence cell below 4/32. Diagnostic only. Retain all 344 imported recipes, including the 116 armor upgrades and all lower-Essence controls.',
                       stopRule='Complete all six settings without adaptation, extension, fresh seeds, retries, confirmation or application.',
                       sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('refined_carried_owner', ROOT / 'build/bounded_windows_process.py')
    previous = os.environ.get('LL_REFINED_CARRIED_CALIBRATION')
    os.environ['LL_REFINED_CARRIED_CALIBRATION'] = str(package / 'request.json')
    try:
        command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
                   '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessRefinedCarriedCalibrationTests']
        process = owner.run(command, ROOT, package / 'calibration.log', time.monotonic() + 2760, log_byte_limit=1048576)
        io.write(package / 'process.json', process)
        io.require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
                   'Calibration failed; preserve evidence, no retry')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test executable changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'RefinedCarriedCalibrationComplete' and result['fights'] == 66048, 'Incomplete comparison')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 3*1073741824, 'Storage limit exceeded')
        io.write(package / 'completion.json', dict(status='Complete', fights=66048, newSeeds=0, retries=0,
                 resultSha256=io.sha(output / 'result.json'), archiveManifestSha256=io.sha(output / 'files.json'),
                 decision=result['decision']))
        print('Completed 66,048 historical fights: ' + result['decision'], flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_REFINED_CARRIED_CALIBRATION', None)
        else:
            os.environ['LL_REFINED_CARRIED_CALIBRATION'] = previous


if __name__ == '__main__':
    main()
