"""Freeze joint-activity inputs and own one bounded, non-adaptive sequential acquisition diagnostic."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-joint-activity-v1'
HISTORICAL_PIN = 'ca81e9a595d2d6c7cdd011714689a76052fecbcff232d14f5b640a37c50d3ae2'
DUNGEON_PIN = '8077fa093168a61efd75a5afac034af72c74b54671e534b2ff7887bffde97afe'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('bootstrap_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ('owner', 'output', 'artifacts'):
        parser.add_argument('--'+key, type=Path, required=True)
    args = parser.parse_args()
    owner, output, artifacts = [io.unlinked(p.absolute()) for p in (args.owner, args.output, args.artifacts)]
    io.require(owner.parent == output.parent == artifacts.parent == ROOT/'TestResults', 'Direct TestResults children required')
    io.require(owner.name.startswith('tower-activity-owner') and not owner.exists() and not output.exists(), 'Fresh named owner/output; no retry or resume')
    history_path = ROOT/'TestResults/balance/tower-carried-confirmation-20260928/seed-ledger.json'
    dungeon_path = ROOT/'TestResults/tower-dungeon-owner-20260928/seed-ledger.json'
    io.require(io.sha(history_path) == HISTORICAL_PIN and io.sha(dungeon_path) == DUNGEON_PIN, 'Changed historical reservation pin')
    history, dungeon = io.read(history_path), io.read(dungeon_path)
    excluded = set(history['historical'] + history['first'] + history['second'] + dungeon['reserved'])
    io.require(len(excluded) == 836359, 'Changed initial exclusion union')
    earlier = sorted((ROOT/'TestResults').glob('tower-bootstrap-owner*/seed-ledger.json')) + sorted((ROOT/'TestResults').glob('tower-activity-owner*/seed-ledger.json'))
    io.require(io.sha(ROOT/'TestResults/tower-bootstrap-owner-20260928/seed-ledger.json') == '6449f56ac6750e565f7d64cd81c8b0ebf2dd3ac72dd44ca3595e29a014f398a2', 'Changed bootstrap ledger')
    for path in earlier:
        old = io.read(path)
        io.require(old['version'] in ('tower-first-supply-v1', VERSION), 'Unknown preceding reservation')
        excluded.update(old['reserved'])
    owner.mkdir()
    inputs, source_map = {}, {}

    def freeze(path):
        relative = path.relative_to(ROOT)
        destination = owner/'inputs'/relative
        if str(path) in source_map:
            return destination
        destination.parent.mkdir(parents=True, exist_ok=True)
        digest = io.sha(path)
        with path.open('rb') as source, destination.open('xb') as target:
            shutil.copyfileobj(source, target)
        io.require(io.sha(path) == io.sha(destination) == digest, 'Input changed during capture: '+str(path))
        inputs[str(destination)] = digest
        source_map[str(path)] = dict(archive=str(destination), sha256=digest)
        return destination

    for path in [history_path, dungeon_path, *earlier]:
        freeze(path)
    for tree in ('LL/src/API/API.LL/Data', 'LL/tools/BalanceHarness/Fixtures'):
        for path in sorted((ROOT/tree).rglob('*.json')):
            freeze(path)
    freeze(ROOT/'LL/src/API/API.LL/appsettings.json')
    for tree in ('LL/tools/BalanceHarness', 'LL/src/Core/Common', 'LL/src/Core/Domain', 'LL/src/Core/Application', 'LL/src/Infrastructure/Service/Services.LL'):
        for path in sorted((ROOT/tree).rglob('*.cs')):
            if not {'bin','obj'}.intersection(path.relative_to(ROOT).parts):
                freeze(path)
    for path in [Path(__file__), Path(__file__).with_name('verify-tower-activity.py'),
                 Path(__file__).with_name('verify-dungeon-acquisition.py'), Path(__file__).with_name('qualify-dungeon-acquisition.py'),
                 Path(__file__).with_name('prepare-confirmed-team-reuse.py'), ROOT/'build/bounded_windows_process.py',
                 ROOT/'build/run-tests.ps1', ROOT/'LL/tests/EssenceSystem.Tests/BalanceHarnessActivityTests.cs']:
        freeze(path)
    # The test process executes the retained build, not a silently rebuilt host or API.
    for name in ('BalanceHarness', 'Services.LL', 'Application', 'Domain', 'Common'):
        runtime = artifacts/'bin/BalanceHarness/release'/f'{name}.dll'
        test = artifacts/'bin/EssenceSystem.Tests/release'/f'{name}.dll'
        io.require(io.sha(runtime) == io.sha(test), 'Test/runtime mismatch: '+name)
        freeze(runtime); freeze(test)
        inputs[str(runtime)] = io.sha(runtime)
        inputs[str(test)] = io.sha(test)
    test_dll = artifacts/'bin/EssenceSystem.Tests/release/EssenceSystem.Tests.dll'
    freeze(test_dll); inputs[str(test_dll)] = io.sha(test_dll)
    io.write(owner/'source-map.json', source_map)
    allocated, cursor = [], 0

    def fresh():
        nonlocal cursor
        while True:
            candidate = int.from_bytes(hashlib.sha256(f'{VERSION}|{cursor}'.encode()).digest()[:4], 'little', signed=True)
            cursor += 1
            if candidate not in excluded:
                excluded.add(candidate); allocated.append(candidate)
                return candidate

    identities = [dict(recipe=recipe, path=path, seed=fresh()) for recipe in ('guardian','restorer','striker','controller') for path in range(4)]
    panels = [dict(path=path, attempt=attempt, run=dict(dungeon='goblin_mines', layoutSeed=fresh(), roomSeeds=[fresh() for _ in range(64)]))
              for path in range(4) for attempt in range(12)]
    io.write(owner/'seed-ledger.json', dict(version=VERSION, state='ReservedIncludingUnconsumed', historical=source_map[str(history_path)],
        dungeon=source_map[str(dungeon_path)], preceding=[source_map[str(p)] for p in earlier], reserved=allocated, exclusionUnionCount=len(excluded)))
    inputs[str(owner/'seed-ledger.json')] = io.sha(owner/'seed-ledger.json')
    request = dict(version=VERSION, apiRoot=str(owner/'inputs/LL/src/API/API.LL'), fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),
        output=str(output), inputHashes=inputs, identities=identities, panels=panels)
    io.write(owner/'request.json', request)
    io.write(owner/'declaration.json', dict(version=VERSION, requestSha256=io.sha(owner/'request.json'),
        histories=32, scenarios=2, recipes=4, paths=4, maximumAttempts=384, controls=32,
        maximumReplays=40, maximumFights=30000, maximumEncountersPerHistory=86400,
        maximumSeconds=900, maximumNativeSeconds=840, maximumBytes=256*1048576, newSeeds=3136, retries=0,
        rule='Fixed checkpoint policy, shared idle reward histories, random quest armor, legal stat-budget selection. No search, tuning, retries, adaptive extension or measured player pace.'))
    process = module('bootstrap_process', ROOT/'build/bounded_windows_process.py')
    previous = os.environ.get('LL_TOWER_ACTIVITY')
    os.environ['LL_TOWER_ACTIVITY'] = str(owner/'request.json')
    try:
        command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT/'build/run-tests.ps1'), '-NoBuild',
                   '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessActivityTests.Frozen_joint_activity_histories']
        receipt = process.run(command, ROOT, owner/'study.log', time.monotonic()+900, log_byte_limit=1048576)
        io.write(owner/'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Incomplete study: preserve all evidence and reservations; no retry')
        for path, digest in inputs.items():
            io.require(io.sha(Path(path)) == digest, 'Frozen input changed: '+path)
        result = io.read(output/'result.json')
        io.require(result['status'] == 'ConditionalJointActivityCompleteNotPlayerPace' and result['replays'] <= 40 and result['controls'] == 32
                   and result['attempts'] <= 384 and result['fights'] <= 30000, 'Incomplete study accounting')
        io.require(sum(p.stat().st_size for p in output.iterdir()) <= 256*1048576, 'Output cap exceeded')
        io.write(owner/'completion.json', dict(status='Complete', manifestPin=io.sha(output/'files.json'), resultPin=io.sha(output/'result.json'),
            attempts=result['attempts'], fights=result['fights'], replays=result['replays'], newSeeds=3136, exclusionUnionCount=len(excluded)))
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx', owner/'study-tests.trx')
        print(json.dumps(io.read(owner/'completion.json'), indent=2), flush=True)
    finally:
        if previous is None: os.environ.pop('LL_TOWER_ACTIVITY', None)
        else: os.environ['LL_TOWER_ACTIVITY'] = previous


if __name__ == '__main__':
    main()
