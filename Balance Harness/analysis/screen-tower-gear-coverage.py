"""One bounded gear coverage screen: six existing encounter budgets, 1,344 fights.

Uses 32 already-consumed historical seeds. Descriptive only; no confirmation,
search-policy comparison, fresh allocation, retries or data-dependent extension.
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
SOURCE = ROOT / 'TestResults/balance/tower-gear-screen-20260928'
SOURCE_PIN = '20b196238a4a51576d0e07202002aebe6882aa68a9260bbc80f576b176e7aa8a'
PREVIOUS = ROOT / 'TestResults/affinity-progression-screen-20260925'
PREVIOUS_PIN = '71ea7fadf448b6eee3bddbc663b8cde9c502ba1a058a6c63a66c5c97dffe49d9'
PROFILES = ROOT / 'LL/tools/BalanceHarness/Fixtures/tower-gear-specialization-screen.json'
VERSION = 'tower-gear-encounter-coverage-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('gear_coverage_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.resolve()) for p in (args.package, args.output, args.artifacts)]
    io.require(not package.exists() and not output.exists(), 'Choose new package and output directories; no resume')
    io.require(io.sha(SOURCE / 'files.json') == SOURCE_PIN, 'Changed source manifest')
    io.require(io.sha(PREVIOUS / 'freeze.json') == PREVIOUS_PIN, 'Changed historical diagnostic freeze')
    pins = {str(SOURCE / 'files.json'): SOURCE_PIN, str(PREVIOUS / 'freeze.json'): PREVIOUS_PIN,
            str(PROFILES): io.sha(PROFILES)}

    def pinned(path, expected):
        io.require(io.sha(path) == expected, 'Changed source: ' + str(path))
        pins[str(path)] = expected
        return io.read(path)

    files = io.read(SOURCE / 'files.json')
    source_baseline = pinned(SOURCE / 'source-baseline.json', files['source-baseline.json'])
    scope = pinned(SOURCE / 'scope.json', files['scope.json'])
    pinned(SOURCE / 'variants.json', files['variants.json'])
    source_request = pinned(SOURCE / 'request.json', files['request.json'])
    io.require(io.sha(PROFILES) == source_request['profilesHash'], 'Use the six previously declared profiles unchanged')
    previous = io.read(PREVIOUS / 'freeze.json')
    seeds = previous['seeds']
    io.require(len(seeds) == len(set(seeds)) == 32, 'Expected the existing 32-seed diagnostic panel')
    ledger = pinned(SOURCE / 'seed-ledger.json', files['seed-ledger.json'])
    io.require(set(seeds) <= set(ledger['historical']), 'Every diagnostic seed must already be excluded')
    cases = []
    for case in previous['definition']['cases']:
        floor = case['floor']
        name = f'scenarios/floor-{floor:02d}-reference-1.json'
        scenario = (copy.deepcopy(source_baseline['scenario']) if floor == 15
                    else pinned(PREVIOUS / name, previous['captured'][name]))
        scenario['seeds'] = []
        cases.append(dict(floor=floor, sourceId=source_baseline['party']['id'] if floor == 15 else case['referenceIds'][0],
                          budget=case['budget'], scenario=scenario))
    io.require([c['floor'] for c in cases] == [3, 7, 8, 10, 13, 15], 'Use the fixed six encounter budgets')
    runtime = artifacts / 'bin/BalanceHarness/release'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    hashes = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in hashes.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch: ' + name)
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN,
                   runtime=str(runtime), profiles=str(PROFILES), cases=cases, seeds=seeds, inputHashes=pins, assemblyHashes=hashes)
    io.write(package / 'request.json', request)
    sources = [Path(__file__), ROOT / 'LL/tools/BalanceHarness/TowerGearProfiles.cs',
               ROOT / 'LL/tools/BalanceHarness/TowerAffinitySearch.cs', ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessGearCoverageTests.cs']
    io.write(package / 'declaration.json', dict(version=VERSION, requestSha256=io.sha(package / 'request.json'),
             samples=32, cases=6, profiles=7, maximumFights=1344, maximumSeconds=900, maximumBytes=1073741824,
             newSeeds=0, retries=0, confirmedTeams=0, rule='Armor-and-health only: 4 through 28 wins flags a follow-up case. Descriptive historical seeds; no confirmation.',
             testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'), sourcePins={str(p): io.sha(p) for p in sources}))
    owner = module('gear_coverage_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessGearCoverageTests']
    prior = os.environ.get('LL_GEAR_COVERAGE')
    os.environ['LL_GEAR_COVERAGE'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'coverage.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Coverage failed; preserve evidence, do not retry fights')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'CoverageComplete' and result['fights'] == 1344, 'Incomplete coverage')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=1344, newSeeds=0, retries=0))
        print('Completed 1,344 diagnostic fights: ' + str(output), flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_GEAR_COVERAGE', None)
        else:
            os.environ['LL_GEAR_COVERAGE'] = prior


if __name__ == '__main__':
    main()
