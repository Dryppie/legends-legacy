"""Execute the frozen floor-13 offense calibration: 1,120 historical-seed fights.

All seven gear profiles and all five offense settings are mandatory. No fresh
allocation, adaptive extension, retry, production edits or automatic search.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
PACKET = ROOT / 'TestResults/tower-gear-benchmark-review-20260928'
PACKET_PIN = '36781cd2034011bd3dd78f530c541e902bcdd8809e74f1bddc7c904296703abe'
SOURCE = ROOT / 'TestResults/tower-gear-coverage-20260928'
SOURCE_PIN = 'f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746'
CONFIRMED = ROOT / 'TestResults/balance/tower-floor13-gear-confirmation-20260928'
CONFIRMED_PIN = '5b91d70abee1c5cdde891918ad7001479c43568da090b2995111066b7a9e869f'
VERSION = 'tower-gear-offense-calibration-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('gear_calibration_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.resolve()) for p in (args.package, args.output, args.artifacts)]
    io.require(not package.exists() and not output.exists(), 'Choose new package and output directories; no resume')
    io.require(not output.is_relative_to(ROOT / 'TestResults/balance'), 'Keep historical-seed diagnostics outside the fresh-seed registry')
    pins = {}

    def pinned(path, expected):
        io.require(io.sha(path) == expected, 'Changed frozen input: ' + str(path))
        pins[str(path)] = expected
        return io.read(path)

    manifest = pinned(PACKET / 'files.json', PACKET_PIN)
    proposal = pinned(PACKET / 'calibration-proposal.json', manifest['calibration-proposal.json'])
    controls = pinned(PACKET / 'floor13-controls.json', manifest['floor13-controls.json'])
    pinned(PACKET / 'review.json', manifest['review.json'])
    source_files = pinned(SOURCE / 'files.json', SOURCE_PIN)
    scope = pinned(SOURCE / 'scope.json', source_files['scope.json'])
    cells = pinned(SOURCE / 'cells.json', source_files['cells.json'])
    request = pinned(SOURCE / 'request.json', source_files['request.json'])
    # The trial ledger is JSONL: authenticate bytes without parsing it as JSON.
    trial_path = SOURCE / 'study/trials.jsonl'
    io.require(io.sha(trial_path) == source_files['study/trials.jsonl'], 'Changed baseline trial ledger')
    pins[str(trial_path)] = source_files['study/trials.jsonl']
    confirmed_files = pinned(CONFIRMED / 'files.json', CONFIRMED_PIN)
    teams = pinned(CONFIRMED / 'teams.json', confirmed_files['teams.json'])
    ledger = pinned(CONFIRMED / 'seed-ledger.json', confirmed_files['seed-ledger.json'])
    io.require(proposal['offenseValues'] == [3.36, 4.2, 5.04, 6.72, 10.08]
               and proposal['offenseMultipliers'] == [1, 1.25, 1.5, 2, 3]
               and proposal['maximumFights'] == 1120, 'Changed declared grid')
    io.require(proposal['settings'] == scope['settings'] and proposal['seeds'] == request['seeds'], 'Settings/seed drift')
    io.require(len(proposal['seeds']) == len(set(proposal['seeds'])) == 32, 'Incomplete seed panel')
    io.require(set(proposal['seeds']) <= set(ledger['historical']) | set(ledger.get('reserved', [])), 'Seeds were not previously consumed')
    expected = [dict(profile=c['profile'], scenario={**c['scenario'], 'seeds': []}) for c in cells if c['floor'] == 13]
    io.require(controls == expected and len(controls) == 7, 'Changed exact controls')
    for team in teams:
        io.require(team['scenario'] == next(c['scenario'] for c in controls if c['profile'] == team['profile']), 'Confirmed recipe drift')
    runtime = artifacts / 'bin/BalanceHarness/release'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    for name, digest in scope['execution']['assemblyHashes'].items():
        for folder in (runtime, tests):
            io.require(io.sha(folder / (name + '.dll')) == digest, 'Combat runtime drift: ' + str(folder / name))
    for name, digest in scope['contentHashes'].items():
        path = io.member(SOURCE / 'content/Data', name)
        io.require(io.sha(path) == digest, 'Captured content drift: ' + name)
        pins[str(path)] = digest
    package.mkdir()
    q = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN, runtime=str(runtime),
             proposal=str(PACKET / 'calibration-proposal.json'), controls=str(PACKET / 'floor13-controls.json'), inputHashes=pins)
    io.write(package / 'request.json', q)
    sources = [Path(__file__), Path(__file__).with_name('verify-tower-gear-calibration.py'),
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessGearCalibrationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
               ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), proposalManifestSha256=PACKET_PIN,
                       samples=32, variants=5, profiles=7, maximumFights=1120, maximumSeconds=900, maximumBytes=1073741824,
                       newSeeds=0, retries=0, confirmedTeams=0, searchRuns=0,
                       rule=proposal['selectionRule'], stopRule=proposal['stopRule'],
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'), sourcePins={str(p): io.sha(p) for p in sources})
    io.write(package / 'declaration.json', declaration)
    owner = module('gear_calibration_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessGearCalibrationTests']
    prior = os.environ.get('LL_GEAR_CALIBRATION')
    os.environ['LL_GEAR_CALIBRATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'calibration.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Calibration failed; preserve evidence, do not retry fights')
        for path, digest in {**pins, **declaration['sourcePins']}.items():
            io.require(io.sha(Path(path)) == digest, 'Frozen input changed during execution: ' + path)
        io.require(io.sha(tests / 'EssenceSystem.Tests.dll') == declaration['testedAssemblySha256'], 'Test assembly changed')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'CalibrationComplete' and result['fights'] == 1120, 'Incomplete calibration')
        io.require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) <= 1073741824, 'Archive exceeded storage cap')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=1120, newSeeds=0, retries=0))
        print('Completed 1,120 diagnostic fights: ' + str(output), flush=True)
        print(result['decision'], result['selected'], flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_GEAR_CALIBRATION', None)
        else:
            os.environ['LL_GEAR_CALIBRATION'] = prior


if __name__ == '__main__':
    main()
