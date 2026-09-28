"""Freeze and own the repeating-equipment baseline: 224 parity + 4,256 paired fights.

Uses all 19 previously prepared parties, seven gear choices and the existing
32 historical seeds. No retries, seed allocation, tuning or confirmation.
"""
import argparse
import copy
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-checkpoint-screen-20260928'
SOURCE_PIN = 'c488b7987ca1ac44dbc06ad43203aff01d03ebe32dedd4bc09d4d3c98a8e0dda'
TEAMS = ROOT / 'TestResults/tower-equipment-cycle-teams-20260928'
TEAMS_PIN = '7fd4df8ad67f6f49a1239049866f7758d2205e7efe78e48282a1d9b12964ddfc'
FIXTURES = ROOT / 'LL/tools/BalanceHarness/Fixtures'
VERSION = 'tower-equipment-cycle-screen-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('equipment_cycle_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def prepare(package, output, artifacts):
    io.require(not package.exists() and not output.exists(), 'Choose new directories; no resume')
    io.require(not output.is_relative_to(ROOT / 'TestResults/balance'), 'Historical diagnostics stay outside the fresh-seed registry')
    pins = {}

    def pinned(path, expected=None):
        digest = io.sha(path)
        io.require(expected is None or digest == expected, 'Changed source: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    source_files = pinned(SOURCE / 'files.json', SOURCE_PIN)
    previous = pinned(SOURCE / 'request.json', source_files['request.json'])
    scope = pinned(SOURCE / 'scope.json', source_files['scope.json'])
    team_files = pinned(TEAMS / 'files.json', TEAMS_PIN)
    team_declaration = pinned(TEAMS / 'declaration.json', team_files['declaration.json'])
    prepared = pinned(TEAMS / 'result.json', team_files['result.json'])
    io.require(prepared['status'] == 'PreparedNewBudgetNotBalanceEvidence', 'Wrong prepared team status')
    io.require(team_declaration['sourceManifestSha256'] == SOURCE_PIN and team_declaration['requestSha256'] == io.sha(SOURCE / 'request.json'), 'Changed preparation source')
    budget_path = FIXTURES / 'tower-progression-budget-cycle.json'
    draft = pinned(budget_path, team_declaration['budgetSha256'])
    io.require(draft['version'] == 'tower-progression-budget-preview-v2' and draft['equipmentCycle']['length'] == 10, 'Wrong curve')
    profiles_path = FIXTURES / 'tower-gear-specialization-screen.json'
    profiles = pinned(profiles_path)
    io.require(len(profiles['profiles']) == 6, 'All six gear alternatives required')
    seeds = previous['seeds']
    io.require(len(seeds) == len(set(seeds)) == 32, 'Expected previously consumed panel')
    io.require(len(previous['cases']) == len(prepared['parties']) == 19, 'All 19 retained teams required')
    cases = []
    for old, row in zip(previous['cases'], prepared['parties'], strict=True):
        io.require((old['id'], old['purpose'], old['sourceId']) == (row['case'], row['purpose'], row['sourceId']), 'Changed case order or identity')
        scenario = pinned(TEAMS / row['scenarioFile'], team_files[row['scenarioFile']])
        io.require(row['sha256'] == io.sha(TEAMS / row['scenarioFile']), 'Changed converted team')
        original = pinned(TEAMS / (old['id'] + '.source.json'), team_files[old['id'] + '.source.json'])
        io.require(original == old['scenario'] and not scenario['seeds'], 'Changed source or stale seeds')
        position = (scenario['floorNumber'] - 1) % 10 + 1
        band = next(b for b in draft['equipmentCycle']['bands'] if b['firstFloor'] <= position <= b['lastFloor'])
        budget = {**old['budget'], 'rank': band['rank'], 'quality': band['quality']}
        if old['purpose'] == 'intended-progression':
            io.require(budget == next(b for b in draft['budgets'] if b['priorityFloor'] == scenario['floorNumber']), 'Wrong intended budget')
        for before, after in zip(original['party'], scenario['party'], strict=True):
            io.require(before['partySlot'] == after['partySlot'], 'Changed positions')
            for key in ('characterLevel', 'tier', 'attributeRollMultiplier', 'essenceIds'):
                io.require(before['build'][key] == after['build'][key], 'Changed retained team: ' + key)
            build = after['build']
            io.require(build['rank'] == band['rank'] and build['quality'] == band['quality'] and
                       all(e['definitionId'].endswith('.rarity.' + band['rarity'].lower()) for e in build['equipment']), 'Wrong equipment band')
        case = copy.deepcopy(old)
        case.update(budget=budget, scenario=scenario)
        cases.append(case)
    for name, digest in scope['contentHashes'].items():
        path = ROOT / 'LL/src/API/API.LL/Data' / name
        io.require(io.sha(path) == digest, 'Gameplay content changed: ' + name)
        pins[str(path)] = digest
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    hashes = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in hashes.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch: ' + name)
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN, runtime=str(runtime),
                   profiles=str(profiles_path), budget=str(budget_path), cases=cases, seeds=seeds, inputHashes=pins, assemblyHashes=hashes)
    io.write(package / 'request.json', request)
    io.write(package / 'declaration.json', dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), cases=19, profiles=7,
             samples=32, maximumFights=4480, screenFights=4256, qualificationFights=224, maximumSeconds=900,
             maximumBytes=2 * 1073741824, newSeeds=0, retries=0, testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
             rule='Reproduce old-budget floor-10 inputs and full reports first. Then all 133 new-budget cells. Descriptive paired changes only; no balance acceptance or extension.',
             teamPreparation=str(TEAMS), teamPreparationManifestSha256=TEAMS_PIN,
             sourcePins={str(p): io.sha(p) for p in [Path(__file__), ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessEquipmentCycleScreenTests.cs']}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.resolve()) for p in (args.package, args.output, args.artifacts)]
    prepare(package, output, artifacts)
    owner = module('equipment_cycle_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessEquipmentCycleScreenTests']
    prior = os.environ.get('LL_EQUIPMENT_CYCLE_SCREEN')
    os.environ['LL_EQUIPMENT_CYCLE_SCREEN'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'screen.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Screen failed; preserve evidence, do not retry fights')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'EquipmentCycleScreenComplete' and result['fights'] == 4480 and result['screenFights'] == 4256, 'Incomplete screen')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=4480, newSeeds=0, retries=0))
        print('Completed 224 parity and 4,256 new-budget fights: ' + str(output), flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_EQUIPMENT_CYCLE_SCREEN', None)
        else:
            os.environ['LL_EQUIPMENT_CYCLE_SCREEN'] = prior


if __name__ == '__main__':
    main()
