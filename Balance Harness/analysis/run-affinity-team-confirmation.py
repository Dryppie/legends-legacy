"""Confirm one frozen floor-15 candidate against both references on 512 fresh paired seeds.

Run backend verification first. This owner uses the tested build without rebuilding,
pins the completed source, and permits one 1,536-fight attempt with no extension.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/balance/tower-floor15-search-20260928'
SOURCE_PIN = 'cb732a486168afb88d7cb3bab123395edbcf20dc6d0b2488fbf3da8f9605361a'
VERSION = 'affinity-floor15-fixed-team-confirmation-v1'
CANDIDATE = '6d0e40ce3b07ede6836e24ab13ddfc54699537c5350d32256e398e50c09e8e78'
REFERENCES = [
    '6396cb05afa89d35c0c653b604e4000363fdf6cc12f63165a9cb5d7d36d3d4e6',
    '7b90d85b6dc8976d67064c2761617bdaa4b8bb4b59105c5f3bfc10c899ea1bfb',
]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('team_confirmation_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', required=True, type=Path, help='New request/process evidence directory')
    parser.add_argument('--output', required=True, type=Path, help='New direct child of TestResults/balance')
    parser.add_argument('--artifacts', required=True, type=Path, help='Previously tested isolated .NET build')
    parser.add_argument('--master', type=int, default=2026092851)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p) for p in (args.package, args.output, args.artifacts)]
    registry = ROOT / 'TestResults/balance'
    io.require(output.parent == registry and not output.exists(), 'Choose a new direct child of the complete registry')
    io.require(not package.exists(), 'No package overwrite or resume')
    io.require(io.sha(SOURCE / 'files.json') == SOURCE_PIN, 'Source manifest changed')
    manifest = io.read(SOURCE / 'files.json')

    def pinned(name):
        path = io.member(SOURCE, name)
        io.require(io.sha(path) == manifest[name], 'Changed source: ' + name)
        return path

    old = io.read(pinned('request.json'))
    result = io.read(pinned('result.json'))
    io.require(result['status'] == 'Complete' and result['fights'] == 1168, 'Source search is incomplete')
    scope = io.read(pinned('captured-scope.json'))
    plan = io.read(pinned('plan.json'))
    io.require([s['party']['id'] for s in plan['racing']['scope']['starts'][1:]] == REFERENCES, 'Changed references')
    freeze = io.read(pinned('heldout-freeze.json'))
    teams = [next(t for t in freeze if t['party']['id'] == key) for key in [CANDIDATE, *REFERENCES]]
    io.require([t['role'] for t in teams] == ['generated-finalist', 'existing-reference', 'existing-reference'], 'Changed team origins')
    source_ledger = pinned('seed-ledger.json')
    required = dict(old['requiredHistory'])
    required.update({str(p): io.sha(p) for p in [source_ledger, pinned('history-input.json')]})
    runtime = artifacts / 'bin/BalanceHarness/release'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    io.require((tests / 'EssenceSystem.Tests.dll').is_file(), 'Build and test through run-tests.ps1 first')
    for name, digest in scope['execution']['assemblyHashes'].items():
        for base in [runtime, tests]:
            io.require(io.sha(base / (name + '.dll')) == digest, 'Runtime differs from the exploratory evaluation: ' + name)
    package.mkdir()
    request = dict(version=VERSION, source=str(SOURCE), sourceManifestHash=SOURCE_PIN,
                   output=str(output), registry=str(registry), runtime=str(runtime), candidateId=CANDIDATE,
                   referenceIds=REFERENCES, master=args.master, requiredHistory=required,
                   recoveries=old['recoveries'], recoveryHashes=old['recoveryHashes'])
    io.write(package / 'request.json', request)
    io.write(package / 'declaration.json', dict(version=VERSION, requestSha256=io.sha(package / 'request.json'),
             sourceManifestSha256=SOURCE_PIN, candidateId=CANDIDATE, referenceIds=REFERENCES,
             pairedSeeds=512, teams=3, maximumFights=1536, freshValues=512, retries=0,
             maximumProcessSeconds=900, maximumStudyBytes=1073741824,
             comparisonRule='Both references separately: observed net gain >= 26/512 AND exact one-sided paired-binomial p <= 0.025 (Bonferroni for two comparisons).',
             allocation='Two fixed 256-value blocks before combat; no generation roots and no data-dependent refill or extension.',
             interpretation='Fixed-team confirmation under the captured rules and gear only; no pooled exploratory data, search rerun, nomination change, or algorithm-superiority claim.',
             sourcePins={str(p.relative_to(ROOT)): io.sha(p) for p in [Path(__file__),
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityTeamConfirmationTests.cs',
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs']},
             testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll')))
    owner = module('team_confirmation_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'),
               '-NoBuild', '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessAffinityTeamConfirmationTests']
    previous = os.environ.get('LL_AFFINITY_TEAM_CONFIRMATION')
    os.environ['LL_AFFINITY_TEAM_CONFIRMATION'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'confirmation.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Confirmation failed; preserve evidence and all reservations')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Complete' and result['fights'] == 1536, 'Incomplete confirmation')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=1536, freshValues=512, retries=0))
        print('Completed one fixed 1,536-fight confirmation: ' + str(output), flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_AFFINITY_TEAM_CONFIRMATION', None)
        else:
            os.environ['LL_AFFINITY_TEAM_CONFIRMATION'] = previous


if __name__ == '__main__':
    main()
