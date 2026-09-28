"""Freeze and run 4,256 diagnostic fights on floors 10/11, including lower-budget controls.

Uses the existing 32 historical seeds. The first 224 fights qualify the new
runtime against complete saved reports before the other cells may execute.
No fresh allocation, retries, automatic tuning or confirmation.
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
SOURCE = ROOT / 'TestResults/tower-gear-coverage-20260928'
SOURCE_PIN = 'f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746'
WHOLE = ROOT / 'TestResults/balance/tower-whole-party-20260910'
WHOLE_PIN = '0a9fb683afc6f793f9bf3bb9b6e94944114e52a9496f5fb0dafb8a510b9c4813'
FIXTURES = ROOT / 'LL/tools/BalanceHarness/Fixtures'
VERSION = 'tower-progression-checkpoint-screen-v1'
SIX_IDS = ['097a5be249856416cfc90847855d26122626c5d31b15969eb72c040c6439ff98',
           'd340c925dc73cb07c747f8327f58dd4953d4d0657320991ab6ef553ec64c21f3']
FOUR = [('primary', 'cf74e56caa7128c75228a289212adc7fefd487a7b499dd3aeca1dc08da13f5ec'),
        ('anchor', 'aa6660f9178856bcf5529530be26c65065e5420d95fdcef2c09e175f2c3061e5')]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('checkpoint_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


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
    scope = pinned(SOURCE / 'scope.json', source_files['scope.json'])
    previous = pinned(SOURCE / 'request.json', source_files['request.json'])
    cells = pinned(SOURCE / 'cells.json', source_files['cells.json'])
    seeds = previous['seeds']
    io.require(len(seeds) == len(set(seeds)) == 32, 'Expected the previously consumed panel')
    whole_files = {x['path']: x['sha256'].lower() for x in pinned(WHOLE / 'checksums.json', WHOLE_PIN)}
    reports, contexts, budgets = {}, {}, {}
    for slots in (6, 7):
        def old(name):
            relative = f'studies/slots-{slots}/{name}.json'
            return pinned(WHOLE / relative, whole_files[relative])
        reports[slots], contexts[slots], budgets[slots] = old('party-search'), old('contexts')['0'], old('definition')['budget']
        io.require(reports[slots]['status'] == 'Complete', 'Incomplete historical source')
    draft = pinned(FIXTURES / 'tower-progression-budget-draft.json')
    profiles = pinned(FIXTURES / 'tower-gear-specialization-screen.json')
    io.require(len(profiles['profiles']) == 6, 'Expected all six alternatives')
    cases = []

    def add(identifier, source_id, context, budget, scenario, purpose):
        scenario = copy.deepcopy(scenario)
        scenario['seeds'] = []
        budget = {**budget, 'priorityFloor': scenario['floorNumber']}
        if purpose == 'intended-progression':
            io.require(budget == next(b for b in draft['budgets'] if b['priorityFloor'] == scenario['floorNumber']), 'Different declared checkpoint budget')
        io.require(len(scenario['party']) == (15 if scenario['floorNumber'] == 10 else 10), 'Wrong party extent')
        for member in scenario['party']:
            b = member['build']
            io.require(len(b['essenceIds']) == budget['essenceSlots'] and all(b[k] == budget[k] for k in ('characterLevel', 'tier', 'rank', 'quality')), 'Budget drift')
        cases.append(dict(id=identifier, purpose=purpose, sourceId=source_id, sourceContext=context, budget=budget, scenario=scenario))

    baseline = next(c['scenario'] for c in cells if c['floor'] == 10 and c['profile'] == 'baseline')
    prior_case = next(c for c in previous['cases'] if c['floor'] == 10)
    add('floor10-authored', prior_case['sourceId'], 'current-gear-coverage', prior_case['budget'], baseline, 'intended-progression')

    def retained(slots, choice, floor, context):
        scenario = copy.deepcopy(next(s for s in contexts[slots][context] if s['floorNumber'] == floor))
        for member in scenario['party']:
            key = str(member['partySlot'])
            if key in choice['builds']:
                member['build']['essenceIds'] = list(choice['builds'][key])
        return scenario

    six = [next(x for x in reports[6]['selection'] if x['id'] == identity) for identity in SIX_IDS]
    for i, choice in enumerate(six, 1):
        add(f'floor10-retained-{i}', choice['id'], 'slots-6--balanced', budgets[6], retained(6, choice, 10, 'slots-6--balanced'), 'intended-progression')
    io.require(len(reports[7]['selection']) == 12, 'Keep all twelve seven-slot finalists')
    for i, choice in enumerate(reports[7]['selection'], 1):
        evidence = next(c for c in reports[7]['confirmation'] if c['id'] == choice['id'])
        # Highest historical floor-11 count; ties use balanced before previous-05.
        context = min((c for c in evidence['cells'] if c['floor'] == 11), key=lambda c: (-sum(c['clears']), c['context']))['context']
        add(f'floor11-seven-{i:02}', choice['id'], context, budgets[7], retained(7, choice, 11, context), 'intended-progression')
    for i, choice in enumerate(six, 1):
        add(f'floor11-six-{i}', choice['id'], 'slots-6--balanced', budgets[6], retained(6, choice, 11, 'slots-6--balanced'), 'diagnostic')
    for role, digest in FOUR:
        path = ROOT / f'Balance Harness/Boss-Expansion-Recipes-20260911/serevin-4-slots-{role}.json'
        scenario = pinned(path, digest)
        budget = dict(essenceSlots=4, characterLevel=30, tier=1, rank=1, quality='Standard', priorityFloor=11)
        add(f'floor11-four-{role}', digest, 'published-expansion-' + role, budget, scenario, 'diagnostic')
    io.require(len(cases) == 19, 'Fixed 19-case family required')
    for name, digest in scope['contentHashes'].items():
        path = ROOT / 'LL/src/API/API.LL/Data' / name
        io.require(io.sha(path) == digest, 'Current gameplay content differs from captured coverage: ' + name)
        pins[str(path)] = digest
    runtime, tests = artifacts / 'bin/BalanceHarness/release', artifacts / 'bin/EssenceSystem.Tests/release'
    hashes = {name: io.sha(runtime / (name + '.dll')) for name in scope['execution']['assemblyHashes']}
    for name, digest in hashes.items():
        io.require(io.sha(tests / (name + '.dll')) == digest, 'Test/runtime mismatch: ' + name)
    package.mkdir()
    request = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN, runtime=str(runtime),
                   profiles=str(FIXTURES / 'tower-gear-specialization-screen.json'), cases=cases, seeds=seeds, inputHashes=pins, assemblyHashes=hashes)
    io.write(package / 'request.json', request)
    io.write(package / 'declaration.json', dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), cases=19, profiles=7,
             samples=32, maximumFights=4256, maximumSeconds=900, maximumBytes=2 * 1073741824, qualificationFightsIncluded=224,
             newSeeds=0, retries=0, testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
             rule='Complete all 133 cells; first 224 full reports must match saved floor-10 coverage. All observations descriptive; no balance acceptance or automatic extension.',
             selection='Floor10: authored plus both 20/20 six-slot full-party deployments. Floor11: all twelve seven-slot finalists, each strongest historical ally context (ties balanced); both six-slot deployments; exact published four-slot primary and anchor.',
             sourcePins={str(p): io.sha(p) for p in [Path(__file__), ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessProgressionCheckpointTests.cs']}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + flag, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.resolve()) for p in (args.package, args.output, args.artifacts)]
    prepare(package, output, artifacts)
    owner = module('checkpoint_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessProgressionCheckpointTests']
    prior = os.environ.get('LL_PROGRESSION_CHECKPOINTS')
    os.environ['LL_PROGRESSION_CHECKPOINTS'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'screen.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Screen failed; preserve evidence, do not retry fights')
        result = io.read(output / 'result.json')
        io.require(result['status'] == 'CheckpointScreenComplete' and result['fights'] == 4256, 'Incomplete screen')
        io.write(package / 'completion.json', dict(status='Complete', resultSha256=io.sha(output / 'result.json'),
                 archiveManifestSha256=io.sha(output / 'files.json'), fights=4256, newSeeds=0, retries=0))
        print('Completed 4,256 diagnostic fights: ' + str(output), flush=True)
    finally:
        if prior is None:
            os.environ.pop('LL_PROGRESSION_CHECKPOINTS', None)
        else:
            os.environ['LL_PROGRESSION_CHECKPOINTS'] = prior


if __name__ == '__main__':
    main()
