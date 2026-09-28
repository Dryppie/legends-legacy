"""Confirm the frozen floor-13 resistance profile against two controls, once.

Exactly 512 fresh paired seeds and 1,536 fights; no pooled diagnostic results,
search, replacement candidates, retries or extension. Build and test first.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-gear-coverage-20260928'
SOURCE_PIN = 'f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746'
HISTORY = ROOT / 'TestResults/balance/tower-gear-screen-20260928'
HISTORY_PIN = '20b196238a4a51576d0e07202002aebe6882aa68a9260bbc80f576b176e7aa8a'
VERSION = 'tower-floor13-gear-confirmation-v1'
PROFILES = ['resistance-and-health', 'baseline', 'armor-and-health']


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('gear_confirmation_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, required=True, type=Path)
    parser.add_argument('--master', type=int, default=2026092891)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p) for p in (args.package, args.output, args.artifacts)]
    registry = ROOT / 'TestResults/balance'
    io.require(output.parent == registry and not output.exists() and not package.exists(), 'New package and registry output required; no overwrite or resume')
    io.require(io.sha(SOURCE / 'files.json') == SOURCE_PIN, 'Changed coverage manifest')
    io.require(io.sha(HISTORY / 'files.json') == HISTORY_PIN, 'Changed history manifest')

    def pinned(root, name):
        manifest = io.read(root / 'files.json')
        path = io.member(root, name)
        io.require(io.sha(path) == manifest[name], 'Changed source file: ' + str(path))
        return path

    result = io.read(pinned(SOURCE, 'result.json'))
    io.require(result['status'] == 'CoverageComplete' and result['fights'] == 1344, 'Incomplete diagnostic source')
    scope = io.read(pinned(SOURCE, 'scope.json'))
    cells = io.read(pinned(SOURCE, 'cells.json'))
    teams = [next(c for c in cells if c['floor'] == 13 and c['profile'] == profile) for profile in PROFILES]
    io.require(all(c['scenario']['floorNumber'] == 13 and len(c['scenario']['party']) == 10 for c in teams), 'Wrong frozen encounter')
    prior = io.read(pinned(HISTORY, 'request.json'))
    required = dict(prior['requiredHistory'])
    required.update({str(p): io.sha(p) for p in [pinned(HISTORY, 'seed-ledger.json'), pinned(HISTORY, 'history-input.json')]})
    runtime = artifacts / 'bin/BalanceHarness/release'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    io.require((tests / 'EssenceSystem.Tests.dll').is_file(), 'Build and test through run-tests.ps1 first')
    for name, digest in scope['execution']['assemblyHashes'].items():
        for base in (runtime, tests):
            io.require(io.sha(base / (name + '.dll')) == digest, 'Runtime differs from coverage: ' + name)
    package.mkdir()
    request = dict(version=VERSION, source=str(SOURCE), sourceManifestHash=SOURCE_PIN,
                   historySource=str(HISTORY), historyManifestHash=HISTORY_PIN, output=str(output), registry=str(registry),
                   runtime=str(runtime), master=args.master, requiredHistory=required,
                   recoveries=prior['recoveries'], recoveryHashes=prior['recoveryHashes'])
    io.write(package / 'request.json', request)
    sources = [Path(__file__), ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessGearConfirmationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityTeamConfirmationTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs']
    io.write(package / 'declaration.json', dict(version=VERSION, requestSha256=io.sha(package / 'request.json'),
             sourceManifestSha256=SOURCE_PIN, historyManifestSha256=HISTORY_PIN, profiles=PROFILES,
             samples=512, maximumFights=1536, freshValues=512, retries=0, maximumSeconds=900, maximumBytes=1073741824,
             decision='Both controls: at least 26 net wins of 512 and exact one-sided paired-binomial p <= .025 (Bonferroni). No diagnostic pooling, reselection or extension.',
             allocation='Two declared 256-value blocks before combat. All accepted values are combat seeds.',
             sourcePins={str(p.relative_to(ROOT)): io.sha(p) for p in sources},
             testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll')))
    owner = module('gear_confirmation_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessGearConfirmationTests']
    previous = os.environ.get('LL_GEAR_CONFIRMATION')
    os.environ['LL_GEAR_CONFIRMATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'confirmation.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Confirmation failed; preserve evidence and all reservations')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Complete' and result['fights'] == 1536, 'Incomplete confirmation')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=1536, freshValues=512, retries=0))
        print('Completed floor-13 gear confirmation: ' + str(output), flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_GEAR_CONFIRMATION', None)
        else:
            os.environ['LL_GEAR_CONFIRMATION'] = previous


if __name__ == '__main__':
    main()
