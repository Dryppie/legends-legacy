"""One frozen gear-specialization screen and separate selected-profile confirmation.

Build and test through build/run-tests.ps1 first. This owner freezes six profiles,
one fixed Essence baseline, 384 fresh values and exactly 1,408 fights.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/balance/tower-floor15-confirmation-final-20260928'
SOURCE_PIN = 'af804279840211ec7a3895e365c98549eadbbe4ea0d84b8aa3d4e14780a9226b'
VERSION = 'tower-gear-specialization-screen-v1'
BASELINE = '6396cb05afa89d35c0c653b604e4000363fdf6cc12f63165a9cb5d7d36d3d4e6'
PROFILES = ROOT / 'LL/tools/BalanceHarness/Fixtures/tower-gear-specialization-screen.json'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('gear_screen_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--artifacts', required=True, type=Path)
    parser.add_argument('--master', type=int, default=2026092871)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p) for p in (args.package, args.output, args.artifacts)]
    registry = ROOT / 'TestResults/balance'
    io.require(output.parent == registry and not output.exists() and not package.exists(), 'New output and package required; no overwrite or resume')
    io.require(io.sha(SOURCE / 'files.json') == SOURCE_PIN, 'Changed source manifest')
    manifest = io.read(SOURCE / 'files.json')

    def pinned(name):
        path = io.member(SOURCE, name)
        io.require(io.sha(path) == manifest[name], 'Changed source: ' + name)
        return path

    old = io.read(pinned('request.json'))
    result = io.read(pinned('result.json'))
    io.require(result['status'] == 'Complete' and result['fights'] == 1536, 'Incomplete source confirmation')
    io.require(any(t['party']['id'] == BASELINE and t['role'] == 'existing-reference' for t in io.read(pinned('teams.json'))), 'Missing fixed baseline')
    scope = io.read(pinned('scope.json'))
    runtime = artifacts / 'bin/BalanceHarness/release'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    io.require((tests / 'EssenceSystem.Tests.dll').is_file(), 'Build and test first')
    hashes = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in hashes.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch: ' + name)
    required = dict(old['requiredHistory'])
    required.update({str(p): io.sha(p) for p in [pinned('seed-ledger.json'), pinned('history-input.json')]})
    profiles = io.read(PROFILES)
    io.require(profiles['version'] == VERSION and len(profiles['profiles']) == 6, 'Changed bounded profile design')
    package.mkdir()
    request = dict(version=VERSION, source=str(SOURCE), sourceManifestHash=SOURCE_PIN, output=str(output),
                   registry=str(registry), runtime=str(runtime), baselineId=BASELINE, profiles=str(PROFILES), profilesHash=io.sha(PROFILES),
                   master=args.master, assemblyHashes=hashes, requiredHistory=required,
                   recoveries=old['recoveries'], recoveryHashes=old['recoveryHashes'])
    io.write(package / 'request.json', request)
    io.write(package / 'declaration.json', dict(version=VERSION, requestSha256=io.sha(package / 'request.json'),
             sourceManifestSha256=SOURCE_PIN, baselineId=BASELINE, profiles=profiles,
             discoveryTeams=7, discoverySamples=128, confirmationTeams=2, confirmationSamples=256,
             maximumFights=1408, freshValues=384, retries=0, maximumProcessSeconds=900, maximumStudyBytes=1073741824,
             selection='Most discovery wins among six alternatives; then least mean boss health; then ordinal profile ID.',
             confirmation='Selected profile versus unchanged baseline: at least 13 net wins of 256 AND exact one-sided paired-binomial p <= .05. No discovery pooling, repeated panels or extensions.',
             identity='Optional equipment-identity pin preserves baseline character, item and Essence IDs. Before allocation the new build must reproduce all 512 source baseline input hashes.',
             sourceExecution=scope['execution'], currentAssemblyHashes=hashes,
             sourcePins={str(p.relative_to(ROOT)): io.sha(p) for p in [Path(__file__), PROFILES,
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessGearScreenTests.cs',
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
                 ROOT / 'LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs']},
             testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll')))
    owner = module('gear_screen_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessGearScreenTests']
    previous = os.environ.get('LL_GEAR_SCREEN')
    os.environ['LL_GEAR_SCREEN'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'screen.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Gear study failed; preserve all evidence and reservations')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'Complete' and result['fights'] == 1408, 'Incomplete gear study')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=1408, freshValues=384, retries=0))
        print('Completed 896 screening fights and 512 confirmation fights: ' + str(output), flush=True)
    finally:
        if previous is None:
            os.environ.pop('LL_GEAR_SCREEN', None)
        else:
            os.environ['LL_GEAR_SCREEN'] = previous


if __name__ == '__main__':
    main()
