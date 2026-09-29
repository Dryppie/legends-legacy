"""Own a fixed full-dungeon diagnostic with fresh reservations, bounded process and immutable evidence."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-dungeon-acquisition-qualification-v1'
ACQUISITION_PIN = '6df2d6196bf51a024860d34e00a5cb4b347398de3378a8dbcd2222a2a3e63dc5'
LEDGER_PIN = 'ca81e9a595d2d6c7cdd011714689a76052fecbcff232d14f5b640a37c50d3ae2'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('dungeon_qualification_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('owner', 'output', 'artifacts'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    owner, output, artifacts = [io.unlinked(p.absolute()) for p in (args.owner, args.output, args.artifacts)]
    io.require(owner.parent == output.parent == artifacts.parent == ROOT / 'TestResults', 'Direct TestResults children required')
    io.require(not owner.exists() and not output.exists(), 'New owner and output required; no resume or retry')
    api = ROOT / 'LL/src/API/API.LL'
    fixtures = ROOT / 'LL/tools/BalanceHarness/Fixtures'
    acquisition = ROOT / 'TestResults/tower-acquisition-20260928/report-v2.json'
    historical = ROOT / 'TestResults/balance/tower-carried-confirmation-20260928/seed-ledger.json'
    io.require(io.sha(acquisition) == ACQUISITION_PIN and io.sha(historical) == LEDGER_PIN, 'Pinned source receipts changed')
    ledger = io.read(historical)
    excluded = set(ledger['historical'] + ledger['first'] + ledger['second'])
    io.require(len(excluded) == 835319, 'Changed historical exclusion union')
    # Retain reservations even from interrupted runs. Never recycle an unconsumed reservation.
    prior = list((ROOT / 'TestResults').glob('tower-dungeon-owner*/seed-ledger.json'))
    for path in prior:
        previous = io.read(path)
        io.require(previous['version'] == VERSION, 'Unknown earlier dungeon reservation')
        excluded.update(previous['reserved'])
    cursor = 0
    allocated = []

    def fresh():
        nonlocal cursor
        while True:
            value = int.from_bytes(hashlib.sha256(f'{VERSION}|{cursor}'.encode()).digest()[:4], 'little', signed=True)
            cursor += 1
            if value not in excluded:
                excluded.add(value)
                allocated.append(value)
                return value

    panels = [dict(dungeon=dungeon, layoutSeed=fresh(), roomSeeds=[fresh() for _ in range(64)])
              for dungeon in ('goblin_mines', 'forgotten_catacombs') for _ in range(8)]
    pins = {str(p): io.sha(p) for p in [acquisition, historical, *prior]}
    sources = [Path(__file__), Path(__file__).with_name('verify-dungeon-acquisition.py'),
               Path(__file__).with_name('prepare-confirmed-team-reuse.py'), ROOT / 'build/bounded_windows_process.py',
               ROOT / 'build/run-tests.ps1', api / 'appsettings.json',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessDungeonAcquisitionTests.cs']
    for tree in ('LL/tools/BalanceHarness', 'LL/src/Core/Domain', 'LL/src/Core/Application', 'LL/src/Infrastructure/Service/Services.LL'):
        sources += [p for p in (ROOT / tree).rglob('*.cs') if not {'bin', 'obj'}.intersection(p.relative_to(ROOT).parts)]
    sources += list((api / 'Data').rglob('*.json')) + list(fixtures.glob('*.json'))
    sources += list((ROOT / 'LL').rglob('Directory.Build.*'))
    for name in ('BalanceHarness', 'Services.LL', 'Application', 'Domain', 'Common'):
        runtime = artifacts / 'bin/BalanceHarness/release' / (name + '.dll')
        test_copy = artifacts / 'bin/EssenceSystem.Tests/release' / (name + '.dll')
        io.require(io.sha(runtime) == io.sha(test_copy), 'Runtime/test assembly mismatch: ' + name)
        sources += [runtime, test_copy]
    sources += [artifacts / 'bin/EssenceSystem.Tests/release/EssenceSystem.Tests.dll']
    pins.update({str(p): io.sha(p) for p in sources})
    # Historical inventory is usable only with unchanged definitions and effective settings.
    for name, digest in io.read(acquisition)['sourceHashes'].items():
        if name.startswith('LL/src/API/API.LL/') or name.endswith('tower-progression-budget-cycle.json') or name.endswith('tower-curve.json'):
            io.require(io.sha(ROOT / name) == digest, 'Acquisition content drift: ' + name)
    owner.mkdir()
    io.write(owner / 'seed-ledger.json', dict(version=VERSION, state='ReservedIncludingUnconsumed',
        historicalPath=str(historical), historicalPin=LEDGER_PIN, previousReservations=[str(p) for p in prior],
        reserved=allocated, exclusionUnionCount=len(excluded)))
    pins[str(owner / 'seed-ledger.json')] = io.sha(owner / 'seed-ledger.json')
    request = dict(version=VERSION, apiRoot=str(api), fixtures=str(fixtures), acquisitionReport=str(acquisition),
                   output=str(output), inputHashes=pins, panels=panels)
    io.write(owner / 'request.json', request)
    io.write(owner / 'declaration.json', dict(version=VERSION, requestSha256=io.sha(owner / 'request.json'),
        cells=52, samples=8, attempts=416, qualificationReplays=52, newSeeds=1040, retries=0,
        maximumFights=30000, maximumActionsPerRun=64, maximumSeconds=900, maximumNativeSeconds=840,
        maximumBytes=256*1048576, routePolicy='rest-first-then-lowest-visible-max-toll-min-toll-room-index-v1',
        decisionRule='Descriptive fixed-cohort diagnostic only. No acceptance, tuning, adaptive sample extension or retries. Zero measured player activity.'))
    process = module('dungeon_bounded_owner', ROOT / 'build/bounded_windows_process.py')
    previous = os.environ.get('LL_DUNGEON_ACQUISITION')
    os.environ['LL_DUNGEON_ACQUISITION'] = str(owner / 'request.json')
    try:
        command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
                   '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests']
        receipt = process.run(command, ROOT, owner / 'study.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(owner / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Study failed; preserve partial evidence and every reservation. No retry.')
        for name, digest in pins.items():
            io.require(io.sha(Path(name)) == digest, 'Frozen input changed: ' + name)
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'DiagnosticCompleteNotPaceAcceptance' and len(result['index']) == 416
                   and result['replays'] == 52 and result['fights'] <= 30000, 'Incomplete qualification')
        io.require(sum(p.stat().st_size for p in output.iterdir() if p.is_file()) <= 256*1048576, 'Output cap exceeded')
        io.write(owner / 'completion.json', dict(status='Complete', manifestPin=io.sha(output / 'files.json'),
            resultPin=io.sha(output / 'result.json'), fights=result['fights'], attempts=416, replays=52,
            newSeeds=1040, exclusionUnionCount=len(excluded)))
        print(json.dumps(io.read(owner / 'completion.json'), indent=2), flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_DUNGEON_ACQUISITION', None)
        else:
            os.environ['LL_DUNGEON_ACQUISITION'] = previous


if __name__ == '__main__':
    main()
