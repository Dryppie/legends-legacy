"""Run the controlled floor-11 seventh-Essence screen: 7,296 historical-seed fights."""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-floor11-linked-20260928'
SOURCE_PIN = 'feaae1ff33bfccd145c1cd733a91b8dc313f4b965f6d0b4394dffab3a4697c03'
VERSION = 'tower-floor11-seventh-essence-screen-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('seventh_essence_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.resolve()) for p in (args.package, args.output, args.artifacts)]
    io.require(not package.exists() and not output.exists(), 'Choose new directories; no resume')
    io.require(not output.is_relative_to(ROOT / 'TestResults/balance'), 'Historical-seed diagnostics stay outside the fresh-seed registry')
    pins = {}

    def pinned(path, expected=None):
        digest = io.sha(path)
        io.require(expected is None or digest == expected, 'Changed input: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    files = pinned(SOURCE / 'files.json', SOURCE_PIN)
    scope = pinned(SOURCE / 'variant-03/scope.json', files['variant-03/scope.json'])
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    result = pinned(SOURCE / 'result.json', files['result.json'])
    io.require(result['status'] == 'Floor11CalibrationComplete', 'Incomplete source')
    io.require(len(cells) == 112 and all(c['floor'] == 11 for c in cells), 'Retain all previous floor-11 controls')
    floor_file = 'world-tower/tower-floors.json'
    for name, digest in scope['contentHashes'].items():
        path = SOURCE / 'variant-03/content/Data' / name
        io.require(io.sha(path) == digest, 'Changed diagnostic content')
        pins[str(path)] = digest
        current = ROOT / 'LL/src/API/API.LL/Data' / name
        expected = files['variant-00/content/Data/' + name] if name == floor_file else digest
        io.require(io.sha(current) == expected, 'Current production source changed: ' + name)
        pins[str(current)] = expected
    tower = io.read(SOURCE / 'variant-03/content/Data' / floor_file)
    scaling = next(f for f in tower['floors'] if f['floorNumber'] == 11)['guardianScaling']
    io.require(scaling['health'] == 4.35 and scaling['offense'] == 5.58, 'Wrong diagnostic setting')
    definitions = io.read(SOURCE / 'variant-03/content/Data/essences/essences.json')['essences']
    families = {e['id']: e['sourceMonsterId'].lower() for e in definitions}
    eligibility = {}
    for name in ('floor11-six-1', 'floor11-six-2'):
        source = next(c['scenario'] for c in cells if c['case'] == name and c['profile'] == 'resistance-and-health')
        used = {families[e] for member in source['party'] for e in member['build']['essenceIds']}
        eligibility[name] = sorted(e['id'] for e in definitions if e['sourceMonsterId'].lower() not in used)
        io.require(len(eligibility[name]) == 57, 'Changed uniform-addition family')
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    hashes = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in hashes.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch: ' + name)
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN,
                   runtime=str(runtime), inputHashes=pins, assemblyHashes=hashes)
    io.write(package / 'request.json', request)
    sources = [Path(__file__), Path(__file__).with_name('verify-seventh-essence-upgrades.py'),
               ROOT / 'LL/tools/BalanceHarness/TowerProgressionUpgrades.cs',
               ROOT / 'LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessProgressionUpgradeTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'),
                       qualificationCells=112, addedCells=116, samples=32, maximumFights=7296,
                       qualificationFightsIncluded=3584, maximumSeconds=900, maximumBytes=2 * 1073741824,
                       newSeeds=0, retries=0, eligibility=eligibility,
                       rule='Keep both sources on resistance-and-health. Evaluate level-60 six-Essence controls and every globally legal uniform seventh definition. Preserve all six original Essences and instance identities. Rank observed additions by wins, lower guardian health, ordinal ID; export every co-leader.',
                       limitation='Historical seed screen of uniform additions only; not mixed additions, gear optimization, independent confirmation, supported-search execution or boss acceptance.',
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'), sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('seventh_essence_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessProgressionUpgradeTests']
    prior = os.environ.get('LL_PROGRESSION_UPGRADES')
    os.environ['LL_PROGRESSION_UPGRADES'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'screen.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Screen failed; preserve evidence, do not repeat fights')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'ProgressionUpgradeScreenComplete' and result['fights'] == 7296, 'Incomplete screen')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2 * 1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=7296, newSeeds=0, retries=0))
        print('Completed 7,296 historical-seed fights: ' + str(output), flush=True)
        for leader in result['leaders']:
            print(leader['sourceCase'], leader['addition'], leader['wins'], '/ 32', flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_PROGRESSION_UPGRADES', None)
        else:
            os.environ['LL_PROGRESSION_UPGRADES'] = prior


if __name__ == '__main__':
    main()
