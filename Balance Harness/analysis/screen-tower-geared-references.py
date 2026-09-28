"""Own and audit a fixed 192-fight floor-13 reference screen; no fresh seeds."""
import argparse
import gzip
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'TestResults/tower-gear-calibration-20260928'
SOURCE_PIN = 'a827de90aa8c9a68409ec65615f447d123629c3dcf62e1000ba7675718063e65'
VERSION = 'tower-floor13-geared-reference-screen-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


previous = module('geared_reference_previous', Path(__file__).with_name('run-affinity-floor-evaluation.py'))
io = previous.io


def audit(study, pin, q):
    io.require(io.sha(study / 'files.json') == pin, 'Changed manifest')
    files = io.read(study / 'files.json')
    io.require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Changed inventory')
    for name, digest in files.items():
        io.require(io.sha(io.member(study, name)) == digest, 'Changed file: ' + name)
    for name, digest in q['inputHashes'].items():
        io.require(io.sha(Path(name)) == digest, 'Changed source: ' + name)
    io.require(io.read(study / 'completion.json') == dict(status='Complete', attempts=192, completed=192, retries=0), 'Incomplete screen')
    io.require([json.loads(l)['attempt'] for l in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 193)), 'Wrong attempt journal')
    io.require(io.read(study / 'preflight.json') == dict(status='PreparedNoFights', inputs=192, historicalInputsMatched=32, projectionStable=True), 'Wrong preflight')
    scope = io.read(study / 'scope.json')
    source_scope = io.read(SOURCE / 'variant-01/scope.json')
    io.require(scope == {**source_scope, 'algorithm': VERSION}, 'Scope drift')
    for name, digest in scope['contentHashes'].items():
        io.require(io.sha(study / 'content/Data' / name) == digest == io.sha(study / 'study/content/Data' / name), 'Content drift')
    for name, digest in scope['execution']['assemblyHashes'].items():
        io.require(io.sha(study / 'executable' / (name + '.dll')) == digest, 'Runtime drift')
    cells = io.read(study / 'cells.json')
    io.require([(c['form'], c['reference']) for c in cells] == [(f, n) for f in ('retained', 'projected') for n in (1, 2, 3)], 'Changed cell schedule')
    seeds = io.read(SOURCE / 'proposal.json')['seeds']
    trials = [json.loads(l) for l in (study / 'study/trials.jsonl').read_text().splitlines()]
    io.require(len(trials) == len({t['cacheKey'] for t in trials}) == 192, 'Wrong trial count/cache identity')
    source_files = io.read(SOURCE / 'files.json')
    source_trials = [json.loads(l) for l in (SOURCE / 'variant-01/study/trials.jsonl').read_text().splitlines()]
    old = {t['seed']: t for t in source_trials if t['stage'] == 'resistance-and-health'}
    outcomes = [[] for _ in cells]
    for ordinal, trial in enumerate(trials):
        cell, seed = cells[ordinal // 32], seeds[ordinal % 32]
        io.require(trial['id'] == f'trial-{ordinal+1:06}' and trial['seed'] == seed and trial['stage'] == f'{cell["form"]}/{cell["reference"]}', 'Trial schedule changed')
        io.require(io.read(io.member(study / 'study/recipes', trial['recipe'] + '.json')) == cell['scenario'], 'Changed recipe')
        raw = json.loads(gzip.decompress((study / 'study/battles' / (trial['id'] + '.json.gz')).read_bytes()))
        io.require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding changed')
        io.require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome mismatch')
        if ordinal < 32:
            io.require(trial['inputHash'] == old[seed]['inputHash'], 'Known input changed')
            name = 'variant-01/study/battles/' + old[seed]['id'] + '.json.gz'
            io.require(io.sha(SOURCE / name) == source_files[name], 'Known report changed')
            io.require(raw == json.loads(gzip.decompress((SOURCE / name).read_bytes())), 'Known full report differs')
        outcomes[ordinal // 32].append(raw)
    result = io.read(study / 'result.json')
    io.require(result['status'] == 'ScreenComplete' and result['fights'] == 192 and all(result[k] == 0 for k in ('newSeeds', 'retries', 'searchRuns')), 'Wrong result accounting')
    for row, cell, reports in zip(result['rows'], cells, outcomes, strict=True):
        io.require((row['form'], row['reference'], row['sourceId'], row['wins'], row['samples']) ==
                   (cell['form'], cell['reference'], cell['sourceId'], sum(r['succeeded'] for r in reports), 32), 'Wrong row')
        io.require(math.isclose(row['meanGuardianHealth'], sum(r['guardianHealthRemainingPercent'] for r in reports)/32, abs_tol=1e-6), 'Wrong health mean')
    rows = result['rows']
    best = sorted(rows[3:], key=lambda r: (-r['wins'], r['reference']))[0]
    eligible = max(r['wins'] for r in rows) <= 28 and best['wins'] >= 4
    io.require(result['eligible'] == eligible and result['benchmarkReference'] == (best['reference'] if eligible else None), 'Wrong gate')
    return dict(status='Verified', authenticatedFiles=len(files), fights=192, newFights=0, newSeeds=0,
                baselineInputsAndFullReportsMatched=32, eligible=eligible, benchmarkReference=result['benchmarkReference'],
                resultSha256=io.sha(study / 'result.json'), archiveManifestSha256=pin)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('package', 'output', 'artifacts'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    package, output, artifacts = [io.unlinked(p.resolve()) for p in (args.package, args.output, args.artifacts)]
    io.require(not package.exists() and not output.exists(), 'New output paths required; no retry/resume')
    io.require(not output.is_relative_to(ROOT / 'TestResults/balance'), 'Historical screen belongs outside the fresh-seed registry')
    pins = {}

    def pinned(path, digest):
        io.require(io.sha(path) == digest, 'Changed source: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    source_files = pinned(SOURCE / 'files.json', SOURCE_PIN)
    scope = pinned(SOURCE / 'variant-01/scope.json', source_files['variant-01/scope.json'])
    for name in ('result.json', 'proposal.json', 'controls.json'):
        pinned(SOURCE / name, source_files[name])
    progression = ROOT / 'TestResults/affinity-progression-screen-20260925'
    freeze = pinned(progression / 'freeze.json', '71ea7fadf448b6eee3bddbc663b8cde9c502ba1a058a6c63a66c5c97dffe49d9')
    case = next(c for c in freeze['definition']['cases'] if c['floor'] == 13)
    recipes = []
    for reference in (1, 2, 3):
        name = f'scenarios/floor-13-reference-{reference}.json'
        recipes.append({**pinned(progression / name, freeze['captured'][name]), 'seeds': []})
    old_files = pinned(previous.SOURCE / 'files.json', previous.SOURCE_PIN)
    plan_path = previous.SOURCE / 'search/root-01/control/racing/plan.json'
    pinned(plan_path, old_files['search/root-01/control/racing/plan.json'])
    profiles = ROOT / 'LL/tools/BalanceHarness/Fixtures/tower-gear-specialization-screen.json'
    pinned(profiles, io.sha(profiles))
    for name, digest in scope['contentHashes'].items():
        path = SOURCE / 'variant-01/content/Data' / name
        io.require(io.sha(path) == digest, 'Changed calibrated content')
        pins[str(path)] = digest
    runtime = artifacts / 'bin/BalanceHarness/release'
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    for name, digest in scope['execution']['assemblyHashes'].items():
        io.require(io.sha(runtime / (name + '.dll')) == io.sha(tests / (name + '.dll')) == digest, 'Changed combat runtime')
    package.mkdir()
    handoff_path = package / 'handoff.json'
    io.write(handoff_path, dict(cases=[{'case': case, 'scenarios': recipes}]))
    pins[str(handoff_path)] = io.sha(handoff_path)
    q = dict(version=VERSION, output=str(output), source=str(SOURCE), sourcePin=SOURCE_PIN, runtime=str(runtime),
             handoff=str(handoff_path), plan=str(plan_path), profiles=str(profiles), inputHashes=pins)
    io.write(package / 'request.json', q)
    sources = [Path(__file__), ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessGearReferenceTests.cs',
               ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs']
    declaration = dict(version=VERSION, requestSha256=io.sha(package / 'request.json'), maximumFights=192, maximumSeconds=900,
                       maximumBytes=1073741824, newSeeds=0, retries=0, sourcePins={str(p): io.sha(p) for p in sources},
                       testedAssemblySha256=io.sha(tests / 'EssenceSystem.Tests.dll'),
                       rule='Stop if any retained/projected reference exceeds 28/32 or every projected reference is below 4/32. Otherwise highest projected wins, tie lowest reference number.')
    io.write(package / 'declaration.json', declaration)
    owner = module('geared_reference_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessGearReferenceTests']
    prior = os.environ.get('LL_GEAR_REFERENCES'); os.environ['LL_GEAR_REFERENCES'] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'screen.log', time.monotonic() + 900, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Screen failed; retain evidence, no combat retry')
        for path, digest in declaration['sourcePins'].items():
            io.require(io.sha(Path(path)) == digest, 'Source changed during execution')
        pin = io.sha(output / 'files.json')
        io.write(package / 'completion.json', dict(status='Complete', archiveManifestSha256=pin, resultSha256=io.sha(output / 'result.json'), fights=192, newSeeds=0))
        readback = audit(output, pin, q); io.write(package / 'independent-readback.json', readback)
        print(json.dumps(readback, indent=2)); print(json.dumps(io.read(output / 'result.json')['rows'], indent=2))
    finally:
        if prior is None: os.environ.pop('LL_GEAR_REFERENCES', None)
        else: os.environ['LL_GEAR_REFERENCES'] = prior


if __name__ == '__main__':
    main()
