"""Own one fixed floor-11 carry-forward screen: 7,296 parity + 7,296 historical fights."""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/balance/tower-floor11-higher-setting-20260928'
PIN = '41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378'
FLOOR10 = ROOT / 'TestResults/balance/tower-floor10-family-confirmation-20260928'
FLOOR10_PIN = '95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944'
APPLICATION = ROOT / 'TestResults/tower-floor10-application-20260928'
APPLICATION_PIN = 'b2bb16c69e25412488caed6a09916c336012ca2322db69f9d39a45f7addc90d7'
FLOOR_PIN = 'dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124'
VERSION = 'tower-floor11-carried-equipment-screen-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('carried_equipment_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


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
        io.require(expected is None or digest == expected, 'Changed source: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    source_files = pinned(SOURCE / 'files.json', PIN)
    scope = pinned(SOURCE / 'scope.json', source_files['scope.json'])
    cells = pinned(SOURCE / 'cells.json', source_files['cells.json'])
    panel = pinned(SOURCE / 'confirmation-seeds.json', source_files['confirmation-seeds.json'])
    result = pinned(SOURCE / 'result.json', source_files['result.json'])
    audit = pinned(ROOT / 'TestResults/tower-floor11-higher-setting-owner-20260928/independent-audit.json')
    io.require(result['assessment'] == audit['assessment'] == 'Pass' and result['fights'] == 58368
               and audit['status'] == 'Verified' and audit['archiveManifestSha256'] == PIN
               and audit['resultSha256'] == source_files['result.json'], 'Audited floor-11 family required')
    io.require(len(cells) == len({c['id'] for c in cells}) == 228 and len(panel) == len(set(panel)) == 256, 'Incomplete source family')
    for name in ('study/trials.jsonl',):
        path = SOURCE / name
        io.require(io.sha(path) == source_files[name], 'Changed source schedule')
        pins[str(path)] = source_files[name]
    floor10_files = pinned(FLOOR10 / 'files.json', FLOOR10_PIN)
    pinned(FLOOR10 / 'cells.json', floor10_files['cells.json'])
    # The latest local application authenticates current content, including both calibrated floors.
    applied_files = pinned(APPLICATION / 'files.json', APPLICATION_PIN)
    applied_scope = pinned(APPLICATION / 'scope.json', applied_files['scope.json'])
    applied_result = pinned(APPLICATION / 'result.json', applied_files['result.json'])
    applied_audit = pinned(ROOT / 'TestResults/tower-floor10-application-owner-20260928/independent-audit.json')
    io.require(applied_result['status'] == applied_audit['status'] == 'Verified'
               and applied_audit['archiveManifestSha256'] == APPLICATION_PIN
               and applied_audit['resultSha256'] == applied_files['result.json'], 'Audited current application required')
    api = ROOT / 'LL/src/API/API.LL'
    hashes = applied_scope['contentHashes']
    io.require(hashes.keys() == scope['contentHashes'].keys()
               and hashes['world-tower/tower-floors.json'] == FLOOR_PIN, 'Changed content inventory')
    for name, digest in hashes.items():
        io.require(io.sha(api / 'Data' / name) == digest, 'Current content changed: ' + name)
        pins[str(api / 'Data' / name)] = digest
        if name != 'world-tower/tower-floors.json':
            io.require(digest == scope['contentHashes'][name], 'Non-floor content changed')
    old_floors = pinned(SOURCE / 'content/Data/world-tower/tower-floors.json', scope['contentHashes']['world-tower/tower-floors.json'])
    new_floors = io.read(api / 'Data/world-tower/tower-floors.json')
    old_ten = next(f for f in old_floors['floors'] if f['floorNumber'] == 10)['guardianScaling']
    io.require(old_ten['health'] == 1.64 and old_ten['offense'] == .92, 'Unexpected historical baseline')
    old_ten.update(health=12.71, offense=7.13)
    io.require(old_floors == new_floors, 'Floor-11 input content changed')
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    assemblies = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in assemblies.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch')
        pins[str(runtime / (name + '.dll'))] = digest
        pins[str(tests / (name + '.dll'))] = digest
    sources = [Path(__file__), Path(__file__).with_name('verify-carried-equipment.py'),
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), Path(__file__).with_name('verify-tower-gear-coverage.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessCarryForwardEquipmentTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'LL/tools/BalanceHarness/TowerProgressionEquipment.cs', ROOT / 'LL/tools/BalanceHarness/TowerProgressionUpgrades.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    package.mkdir()
    request = dict(version=VERSION, source=str(SOURCE), sourcePin=PIN, floor10Source=str(FLOOR10), output=str(output),
                   apiRoot=str(api), runtime=str(runtime), inputHashes=pins, contentHashes=hashes, assemblyHashes=assemblies)
    io.write(package / 'request.json', request)
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), cells=228, samples=32,
                       maximumFights=14592, qualificationFights=7296, screenFights=7296,
                       maximumSeconds=1260, maximumBytes=2 * 1073741824, newSeeds=0, retries=0,
                       seeds=panel[:32], executedModes=['baseline', 'carried'],
                       rule='Retain every source cell. First reproduce all 7,296 baseline inputs and complete reports, then run all carried-gear cells. Intended >16/32 and lower-budget >=4/32 are descriptive concerns only. No fresh acceptance, automatic tuning, retries or extension.',
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
                       sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('carried_equipment_owner', ROOT / 'build/bounded_windows_process.py')
    previous = os.environ.get('LL_CARRIED_EQUIPMENT_SCREEN')
    os.environ['LL_CARRIED_EQUIPMENT_SCREEN'] = str(package / 'request.json')
    try:
        command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
                   '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessCarryForwardEquipmentTests']
        receipt = owner.run(command, ROOT, package / 'screen.log', time.monotonic() + 1260, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Screen failed; retain evidence, no combat retry')
        for name, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test executable changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'CarriedEquipmentScreenComplete' and result['fights'] == 14592, 'Incomplete comparison')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2 * 1073741824, 'Storage limit exceeded')
        io.write(package / 'completion.json', dict(status='Complete', fights=14592, newSeeds=0, retries=0,
                 resultSha256=io.sha(output / 'result.json'), archiveManifestSha256=io.sha(output / 'files.json'),
                 decision=result['summary']['decision']))
        print('Completed 14,592 historical fights: ' + result['summary']['decision'], flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_CARRIED_EQUIPMENT_SCREEN', None)
        else:
            os.environ['LL_CARRIED_EQUIPMENT_SCREEN'] = previous


if __name__ == '__main__':
    main()
