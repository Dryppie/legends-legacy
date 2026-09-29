"""Freeze a paired native-prophecy comparison after audited source accounting and preparation."""
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
VERSION = 'tower-prophecy-offers-v1'
READINESS_LEDGER_PIN = 'bc54eae43e081231eb28fdbfa9d2f676a2f2a7f66b6034cd31bc7b8f6c8c0e96'
SOURCE_LEDGER_PIN = 'f4a4b0006af3131302983b625b302f058029f8207758a91e14c72d2ded68eef5'
SOURCE_MANIFEST_PIN = '327d5288fdf2a898efbf73870bf8b2d882106ef589d2ba778caff473e703a62e'
ACTIVITY_LEDGER_PIN = '0e99464e7a7239ea51b22c204eb8eab6681b70a03ac4a98f012d7458a8d60f8f'
ACTIVITY_MANIFEST_PIN = 'aa84a148c2b8c0c008e84eae9d1d45ab8bcdd2999d5f428e23ab61549b84449c'
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
    for key in ('owner', 'output', 'artifacts', 'projection-owner'):
        parser.add_argument('--'+key, type=Path, required=True)
    parser.add_argument('--projection-pin', required=True)
    args = parser.parse_args()
    owner, output, artifacts = [io.unlinked(p.absolute()) for p in (args.owner, args.output, args.artifacts)]
    io.require(owner.parent == output.parent == artifacts.parent == ROOT/'TestResults', 'Direct TestResults children required')
    io.require(owner.name.startswith('tower-prophecy-combat-owner') and not owner.exists() and not output.exists(), 'Fresh named owner/output; no retry or resume')
    history_path = ROOT/'TestResults/balance/tower-carried-confirmation-20260928/seed-ledger.json'
    dungeon_path = ROOT/'TestResults/tower-dungeon-owner-20260928/seed-ledger.json'
    io.require(io.sha(history_path) == HISTORICAL_PIN and io.sha(dungeon_path) == DUNGEON_PIN, 'Changed historical reservation pin')
    history, dungeon = io.read(history_path), io.read(dungeon_path)
    excluded = set(history['historical'] + history['first'] + history['second'] + dungeon['reserved'])
    io.require(len(excluded) == 836359, 'Changed initial exclusion union')
    earlier = sorted((ROOT/'TestResults').glob('tower-bootstrap-owner*/seed-ledger.json')) + sorted((ROOT/'TestResults').glob('tower-activity-owner*/seed-ledger.json')) + sorted((ROOT/'TestResults').glob('tower-source-policy-owner*/seed-ledger.json')) + sorted((ROOT/'TestResults').glob('tower-entry-readiness-owner*/seed-ledger.json')) + sorted((ROOT/'TestResults').glob('tower-dungeon-loot-owner*/seed-ledger.json')) + sorted((ROOT/'TestResults').glob('tower-prophecy-combat-owner*/seed-ledger.json'))
    io.require(io.sha(ROOT/'TestResults/tower-bootstrap-owner-20260928/seed-ledger.json') == '6449f56ac6750e565f7d64cd81c8b0ebf2dd3ac72dd44ca3595e29a014f398a2', 'Changed bootstrap ledger')
    for path in earlier:
        old = io.read(path)
        io.require(old['version'] in ('tower-first-supply-v1', 'tower-joint-activity-v1', 'tower-two-source-v1', 'tower-entry-readiness-v1', 'tower-dungeon-loot-v1', VERSION), 'Unknown preceding reservation')
        excluded.update(old['reserved'])
    io.require(io.sha(ROOT/'TestResults/tower-source-policy-owner-20260929/seed-ledger.json') == SOURCE_LEDGER_PIN, 'Changed source-policy ledger')
    source_manifest = ROOT/'TestResults/tower-source-policy-study-20260929/files.json'
    io.require(io.sha(source_manifest) == SOURCE_MANIFEST_PIN, 'Changed source-policy manifest')
    io.require(io.sha(ROOT/'TestResults/tower-entry-readiness-owner-20260929/seed-ledger.json') == READINESS_LEDGER_PIN, 'Changed readiness ledger')
    io.require(len(excluded) >= 864455, 'Missing preceding reservations')
    projection_owner = args.projection_owner.absolute()
    projection_request = io.read(projection_owner/'request.json')
    projection_output = Path(projection_request['output'])
    projection_path = projection_output/'result.json'
    io.require(io.sha(projection_output/'files.json') == args.projection_pin, 'Wrong projection pin')
    for name, pin in io.read(projection_output/'files.json').items():
        io.require(io.sha(io.member(projection_output,name)) == pin, 'Changed projection output')
    projection_sources = io.read(projection_owner/'source-map.json')
    for relative in ['LL/tools/BalanceHarness/TowerProphecyOffers.cs', 'LL/tools/BalanceHarness/TowerEntrySources.cs',
                     'LL/tools/BalanceHarness/Fixtures/tower-prophecy-offers.json', 'Balance Harness/analysis/audit-prophecy-offers.py']:
        io.require(io.sha(ROOT/relative) == projection_sources[str(ROOT/relative)]['sha256'], 'Source differs from qualified projection')
    projection = io.read(projection_path)
    projection_audit = io.read(projection_owner/'independent-audit.json')
    io.require(projection['preparations'] > 0 and projection['newFights'] == projection['newCombatSeeds'] == 0
        and projection_audit['manifestPin'] == args.projection_pin and projection_audit['status'] == 'VerifiedNativeOffersNotPlayerPace', 'Native offer audit must precede combat')
    loot_ledger = ROOT/'TestResults/tower-dungeon-loot-owner-20260929/seed-ledger.json'
    io.require(io.sha(loot_ledger) == 'c48c5fafa3b176febbf862841425f6fcbffdfd016032ad1ad02f0d459d52f1c9', 'Changed latest reservations')
    activity_owner = ROOT/'TestResults/tower-activity-owner-20260929'
    activity_output = ROOT/'TestResults/tower-activity-study-20260929'
    io.require(io.sha(activity_owner/'seed-ledger.json') == ACTIVITY_LEDGER_PIN, 'Changed activity exclusions')
    io.require(io.sha(activity_output/'files.json') == ACTIVITY_MANIFEST_PIN, 'Changed source activity evidence')
    activity_manifest = io.read(activity_output/'files.json')
    for name, pin in activity_manifest.items():
        io.require(io.sha(io.member(activity_output, name)) == pin, 'Changed historical activity file: '+name)
    activity_request = io.read(activity_owner/'request.json')
    compatibility = {}
    for name, entry in io.read(activity_owner/'source-map.json').items():
        path = Path(name)
        relative = path.relative_to(ROOT).as_posix()
        relevant = relative.startswith(('LL/src/API/API.LL/Data/', 'LL/src/Core/Common/',
            'LL/src/Core/Domain/Models/Combat/', 'LL/src/Core/Domain/Models/Items/',
            'LL/src/Core/Domain/Models/Dungeons/', 'LL/src/Core/Domain/Models/Essences/',
            'LL/src/Infrastructure/Service/Services.LL/Combat/', 'LL/src/Infrastructure/Service/Services.LL/Dungeons/',
            'LL/src/Infrastructure/Service/Services.LL/Items/')) or relative in (
            'LL/tools/BalanceHarness/TowerActivityInventory.cs','LL/tools/BalanceHarness/TowerBootstrapCohorts.cs',
            'LL/tools/BalanceHarness/Fixtures/tower-activity.json','LL/tools/BalanceHarness/Fixtures/tower-bootstrap.json',
            'LL/src/API/API.LL/appsettings.json')
        if relevant:
            io.require(io.sha(path) == entry['sha256'], 'Comparison requires review of source drift: '+relative)
            compatibility[relative] = entry['sha256']
    owner.mkdir()
    io.write(owner/'source-compatibility.json', compatibility)
    inputs, source_map = {str(owner/'source-compatibility.json'): io.sha(owner/'source-compatibility.json')}, {}

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

    for path in [projection_path, projection_owner/'request.json', projection_owner/'independent-audit.json', *sorted(projection_output.glob('*.json')), source_manifest, activity_owner/'request.json', activity_output/'files.json', activity_output/'result.json', activity_output/'rules.json',
                 *sorted(activity_output.glob('*--history.json'))]:
        freeze(path)
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
    for path in [Path(__file__), Path(__file__).with_name('verify-tower-prophecy-combat.py'),
                 Path(__file__).with_name('verify-dungeon-acquisition.py'), Path(__file__).with_name('audit-dungeon-equipment-loot.py'), Path(__file__).with_name('audit-prophecy-offers.py'), Path(__file__).with_name('qualify-dungeon-acquisition.py'),
                 Path(__file__).with_name('prepare-confirmed-team-reuse.py'), ROOT/'build/bounded_windows_process.py',
                 ROOT/'build/run-tests.ps1', ROOT/'LL/tests/EssenceSystem.Tests/BalanceHarnessProphecyOfferTests.cs']:
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

    identities = activity_request['identities']
    panels = [dict(path=path, attempt=attempt, run=dict(dungeon=family, layoutSeed=fresh(), roomSeeds=[fresh() for _ in range(64)]))
              for family in ('goblin_mines','forgotten_catacombs') for path in range(4) for attempt in range(12)]
    io.write(owner/'seed-ledger.json', dict(version=VERSION, state='ReservedIncludingUnconsumed', historical=source_map[str(history_path)],
        dungeon=source_map[str(dungeon_path)], preceding=[source_map[str(p)] for p in earlier], reserved=allocated, exclusionUnionCount=len(excluded)))
    inputs[str(owner/'seed-ledger.json')] = io.sha(owner/'seed-ledger.json')
    request = dict(version=VERSION, apiRoot=str(owner/'inputs/LL/src/API/API.LL'), fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),
        output=str(output), inputHashes=inputs, identities=identities, panels=panels, sourceArchive=str(owner/'inputs/TestResults/tower-activity-study-20260929'))
    io.write(owner/'request.json', request)
    io.write(owner/'declaration.json', dict(version=VERSION, requestSha256=io.sha(owner/'request.json'),
        histories=64, scenarios=2, recipes=4, paths=4, policies=2, maximumAttempts=768, controls=64,
        maximumReplays=144, maximumFights=64000, maximumEncountersPerHistory=86400,
        maximumSeconds=900, maximumNativeSeconds=840, maximumBytes=256*1048576, newSeeds=6240, retries=0,
        rule='Native production offers versus no prophecy entry credit, with dungeon loot and full-slot readiness in both arms. Fixed visible kill choice, no rerolls. Same idle activity, funded whole Mines sigils at checkpoints, no unsupported objective credit or applied XP. Fresh family-ordinal panels; no tuning, extension or player pace claim.'))
    process = module('bootstrap_process', ROOT/'build/bounded_windows_process.py')
    previous = os.environ.get('LL_TOWER_PROPHECY_COMBAT')
    os.environ['LL_TOWER_PROPHECY_COMBAT'] = str(owner/'request.json')
    try:
        command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT/'build/run-tests.ps1'), '-NoBuild',
                   '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessProphecyOfferTests.Frozen_native_offer_comparison']
        receipt = process.run(command, ROOT, owner/'study.log', time.monotonic()+900, log_byte_limit=1048576)
        io.write(owner/'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Incomplete study: preserve all evidence and reservations; no retry')
        for path, digest in inputs.items():
            io.require(io.sha(Path(path)) == digest, 'Frozen input changed: '+path)
        result = io.read(output/'result.json')
        io.require(result['status'] == 'NativeOfferComparisonComplete' and result['replays'] <= 144 and result['controls'] == 64
                   and result['attempts'] <= 768 and result['fights'] <= 64000, 'Incomplete study accounting')
        io.require(sum(p.stat().st_size for p in output.iterdir()) <= 256*1048576, 'Output cap exceeded')
        io.write(owner/'completion.json', dict(status='Complete', manifestPin=io.sha(output/'files.json'), resultPin=io.sha(output/'result.json'),
            attempts=result['attempts'], fights=result['fights'], replays=result['replays'], newSeeds=6240, exclusionUnionCount=len(excluded)))
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx', owner/'study-tests.trx')
        print(json.dumps(io.read(owner/'completion.json'), indent=2), flush=True)
    finally:
        if previous is None: os.environ.pop('LL_TOWER_PROPHECY_COMBAT', None)
        else: os.environ['LL_TOWER_PROPHECY_COMBAT'] = previous


if __name__ == '__main__':
    main()
