"""Own one frozen historical floor-10 five-Essence comparison; no allocation or content edits."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/balance/tower-floor10-family-confirmation-20260928'
PIN = '95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944'
FLOOR_PIN = '5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05'
VERSION = 'tower-floor10-five-essence-controls-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor10_controls_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.absolute()) for p in (args.package, args.output, args.artifacts)]
    io.require(not package.exists() and not output.exists(), 'New owner and output required; no retry or resume')
    io.require(package.parent == output.parent == artifacts.parent == ROOT / 'TestResults', 'Use direct TestResults children')
    api = ROOT / 'LL/src/API/API.LL'
    pins = {}

    def pinned(path, expected=None):
        digest = io.sha(path)
        io.require(expected is None or digest == expected, 'Changed input: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    files = pinned(SOURCE / 'files.json', PIN)
    scope = pinned(SOURCE / 'scope.json', files['scope.json'])
    result = pinned(SOURCE / 'result.json', files['result.json'])
    audit = pinned(ROOT / 'TestResults/tower-floor10-family-confirmation-owner-20260928/independent-audit.json')
    io.require(result['assessment'] == audit['assessment'] == 'Pass' and audit['archiveManifestSha256'] == PIN
               and audit['resultSha256'] == files['result.json'], 'Audited passing floor-10 source required')
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    panel = pinned(SOURCE / 'confirmation-seeds.json', files['confirmation-seeds.json'])
    io.require(len(cells) == 21 and len(panel) == len(set(panel)) == 256, 'Complete references and historical panel required')
    trials_path = SOURCE / 'study/trials.jsonl'
    io.require(io.sha(trials_path) == files['study/trials.jsonl'], 'Changed original trials')
    pins[str(trials_path)] = files['study/trials.jsonl']
    trials = [json.loads(line) for line in trials_path.read_text().splitlines()]
    io.require(len(trials) == 5376, 'Incomplete historical trials')
    for recipe in {t['recipe'] for t in trials}:
        name = 'study/recipes/' + recipe + '.json'
        pinned(SOURCE / name, files[name])
    for i in range(21):
        for trial in trials[i * 256:i * 256 + 32]:
            name = 'study/battles/' + trial['id'] + '.json.gz'
            io.require(io.sha(SOURCE / name) == files[name], 'Changed qualification report')
            pins[str(SOURCE / name)] = files[name]
    hashes = {}
    for name, digest in scope['contentHashes'].items():
        pinned(SOURCE / 'content/Data' / name, digest)
        expected = FLOOR_PIN if name == 'world-tower/tower-floors.json' else digest
        pinned(api / 'Data' / name, expected)
        hashes[name] = expected
    floors = io.read(SOURCE / 'content/Data/world-tower/tower-floors.json')
    next(f for f in floors['floors'] if f['floorNumber'] == 11)['guardianScaling'].update(health=17.94375, offense=23.0175)
    io.require(floors == io.read(api / 'Data/world-tower/tower-floors.json'), 'Unconfirmed floor change')
    for name in ('equipment-ordinary.v1.json', 'equipment-upgrades.v1.json'):
        pinned(api / 'Data/equipment' / name)
    ledger = ROOT / 'TestResults/balance/tower-carried-confirmation-20260928/seed-ledger.json'
    pinned(ledger)
    runtime = artifacts / 'bin/BalanceHarness/release'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    assemblies = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in assemblies.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime assembly mismatch')
        pins[str(runtime / (name + '.dll'))] = digest
        pins[str(tests / (name + '.dll'))] = digest
    source_paths = [Path(__file__), Path(__file__).with_name('verify-floor10-controls.py'),
        Path(__file__).with_name('prepare-confirmed-team-reuse.py'),
        ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10ControlTests.cs',
        ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
        ROOT / 'LL/tools/BalanceHarness/TowerEssenceAblation.cs', ROOT / 'LL/tools/BalanceHarness/TowerEquipmentAcquisitionAudit.cs',
        ROOT / 'LL/tools/BalanceHarness/TowerProgressionPreview.cs',
        ROOT / 'LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs',
        ROOT / 'LL/src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs',
        ROOT / 'LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentProgressionOrdinaryAcquisition.cs',
        ROOT / 'LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs',
        ROOT / 'LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentTierBudgetCurve.cs',
        ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py', tests / 'EssenceSystem.Tests.dll']
    pins.update({str(p): io.sha(p) for p in source_paths})
    request = dict(version=VERSION, source=str(SOURCE), sourcePin=PIN, apiRoot=str(api), output=str(output),
                   runtime=str(runtime), inputHashes=pins, contentHashes=hashes, assemblyHashes=assemblies)
    package.mkdir()
    io.write(package / 'request.json', request)
    io.write(package / 'declaration.json', dict(version=VERSION, requestSha256=io.sha(package / 'request.json'),
        cells=273, references=21, controls=252, samples=32, levels=[40, 50], tiers=[1, 2],
        maximumFights=8736, qualificationFights=672, controlFights=8064, historicalInputs=5376,
        maximumSeconds=1260, maximumNativeSeconds=1200, maximumBytes=2*1073741824,
        newSeeds=0, retries=0, seeds=panel[:32], rule='Every ordered removal, both declared budgets, every reference and gear profile. Historical diagnostic only; >=4/32 controls are an observed concern. No balance acceptance, tuning, extension or retry.'))
    owner = module('floor10_controls_owner', ROOT / 'build/bounded_windows_process.py')
    previous = os.environ.get('LL_FLOOR10_CONTROLS')
    os.environ['LL_FLOOR10_CONTROLS'] = str(package / 'request.json')
    try:
        command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
            '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessFloor10ControlTests']
        receipt = owner.run(command, ROOT, package / 'screen.log', time.monotonic() + 1260, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Screen failed; retain partial evidence, no combat retry')
        for name, digest in pins.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Floor10ControlScreenComplete' and result['fights'] == 8736, 'Incomplete result')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 2*1073741824, 'Storage cap exceeded')
        io.write(package / 'completion.json', dict(status='Complete', fights=8736, newSeeds=0, retries=0,
            archiveManifestSha256=io.sha(output / 'files.json'), resultSha256=io.sha(output / 'result.json'), decision=result['summary']['decision']))
        print(json.dumps(io.read(package / 'completion.json'), indent=2), flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_FLOOR10_CONTROLS', None)
        else:
            os.environ['LL_FLOOR10_CONTROLS'] = previous


if __name__ == '__main__':
    main()
