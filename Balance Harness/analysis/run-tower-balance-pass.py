"""Bounded Tower-only preparation, composition/gear screen, supported search and confirmation.

Build first with build/run-tests.ps1. Every phase uses new output paths, immutable
inputs, disjoint reserved panels and archived production combat reports.
"""
import argparse
import gzip
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
from statistics import NormalDist
import sys
import time

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-balance-pass-v1'
BASE_LEDGER = ROOT / 'TestResults/tower-fifth-return-combat-owner-20260929/seed-ledger.json'
BASE_PIN = '5feb38e45e17328e9cac768139c7b9133fe20b0803a82fbab43d99912c9fd1cc'
EARNED = ROOT / 'TestResults/tower-fifth-return-combat-study-20260929'
EARNED_PIN = 'dd754b30a0ce0a26032fb0149d64984c2ef41b55f458212a0a4e2529d9adfd50'


def check(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write(path, value):
    with Path(path).open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')


def scaled_guardian(original, health_factor, offense_factor):
    check(all(math.isfinite(x) and 0.25 <= x <= 16 for x in (health_factor, offense_factor)),
          'Health and offense factors must be finite and between 0.25 and 16')
    return {**original, 'health': round(original['health']*health_factor, 10),
            'offense': round(original['offense']*offense_factor, 10)}


def authenticate(root):
    files = read(root / 'files.json')
    for name, pin in files.items():
        check(sha(root / name) == pin, 'Changed source member: ' + name)
    return files


def append_references(cells, evaluated, source):
    """Keep exact recipes, including identity fields; merge only identical scenarios."""
    references = [c for c in evaluated if c['origin'] == 'retained-reference']
    check(len(references) == 3, 'Expected all three saved search references')
    check(all(c['origin'] in ('retained-reference', 'generated-finalist') for c in evaluated),
          'Unknown search nominee origin')
    def key(cell):
        return json.dumps(cell['scenario'], sort_keys=True, separators=(',', ':'))
    existing = {key(c): c['id'] for c in cells}
    coverage = []
    for cell in references:
        scenario = cell['scenario']
        check(scenario['floorNumber'] == cells[0]['scenario']['floorNumber'] and not scenario['seeds'],
              'Reference must be seed-free and from the same floor')
        signature = key(cell)
        added = signature not in existing
        if added:
            clone = json.loads(json.dumps(cell))
            clone.update(id='projected-reference/' + hashlib.sha256(signature.encode()).hexdigest(),
                         origin='projected-reference')
            check(all(c['id'] != clone['id'] for c in cells), 'Reference identifier collision')
            cells.append(clone)
            existing[signature] = clone['id']
        coverage.append(dict(source=str(source), sourceId=cell['id'], cellId=existing[signature], added=added))
    return coverage


def composition_key(cell):
    """Count actual slot builds, ignoring labels, identities and Essence ordering."""
    return tuple(sorted((m['partySlot'], tuple(sorted(m['build']['essenceIds'])))
                        for m in cell['scenario']['party']))


def select_search_references(cells, rows, gear=None):
    lookup = {c['id']: c for c in cells}
    check(len(lookup) == len(cells) and len({r['id'] for r in rows}) == len(rows)
          and set(lookup) == {r['id'] for r in rows}, 'Measured cells and rows must match uniquely')
    check(all(r['gear'] == lookup[r['id']]['gear'] for r in rows), 'Measured gear differs from recipe')
    ranked = sorted(rows, key=lambda r: (-r['wins'], r['meanGuardianHealth'], r['id']))
    check(bool(ranked), 'Measured references required')
    selected_gear = gear if gear is not None else ranked[0]['gear']
    chosen, seen = [], set()
    for row in ranked:
        if row['gear'] != selected_gear:
            continue
        cell = lookup[row['id']]
        key = composition_key(cell)
        if key not in seen:
            seen.add(key)
            chosen.append(cell)
        if len(chosen) == 3:
            break
    check(len(chosen) == 3, 'Three distinct measured reference compositions required at selected gear')
    return chosen, 1


def append_search_finalists(cells, evaluated, source):
    """Preserve exact nominees plus gear projections; deduplicate only exact recipes."""
    finalists = [c for c in evaluated if c['origin'] == 'generated-finalist']
    check(len(finalists) == 2, 'Expected both generated search finalists')
    templates = [c for c in cells if c['composition'] == cells[0]['composition']]
    profiles = {c['gear']: c for c in templates}
    check(len(profiles) == len(templates), 'Ambiguous frozen gear templates')
    def key(cell):
        return json.dumps(cell['scenario'], sort_keys=True, separators=(',', ':'))
    existing = {key(c): c['id'] for c in cells}
    coverage = []
    def append(cell, source_id, kind):
        scenario = cell['scenario']
        check(scenario['floorNumber'] == cells[0]['scenario']['floorNumber'] and not scenario['seeds'],
              'Finalist must be seed-free and from the same floor')
        signature = key(cell)
        added = signature not in existing
        if added:
            clone = json.loads(json.dumps(cell))
            clone['id'] = 'generated-finalist/' + hashlib.sha256(signature.encode()).hexdigest()
            check(all(c['id'] != clone['id'] for c in cells), 'Finalist identifier collision')
            cells.append(clone)
            existing[signature] = clone['id']
        coverage.append(dict(source=str(source), sourceId=source_id, cellId=existing[signature],
                             kind=kind, gear=cell['gear'], added=added))
    for cell in finalists:
        append(cell, cell['id'], 'exact-finalist')
        for gear, template in profiles.items():
            clone = json.loads(json.dumps(cell))
            clone['gear'] = gear
            for member, gear_member in zip(clone['scenario']['party'], template['scenario']['party'], strict=True):
                build, frozen = member['build'], gear_member['build']
                check(member['partySlot'] == gear_member['partySlot'] and
                      all(build[k] == frozen[k] for k in ('characterLevel', 'tier', 'attributeRollMultiplier')),
                      'Gear projection cannot change positions, level, tier or rolls')
                for field in ('equipment', 'rank', 'quality'):
                    build[field] = frozen[field]
            append(clone, cell['id'], 'gear-projection')
    return coverage


def validate_current_content(source, api, floor):
    """Refresh other floors without silently changing the studied floor or catalogs."""
    for path in (source / 'content/Data').rglob('*.json'):
        relative = path.relative_to(source / 'content/Data')
        current = api / 'Data' / relative
        if relative.as_posix() == 'world-tower/tower-floors.json':
            before, after = read(path), read(current)
            check({k: v for k, v in before.items() if k != 'floors'} ==
                  {k: v for k, v in after.items() if k != 'floors'}, 'Tower metadata changed')
            check(next(f for f in before['floors'] if f['floorNumber'] == floor) ==
                  next(f for f in after['floors'] if f['floorNumber'] == floor), 'Target floor changed')
        else:
            check(sha(path) == sha(current), 'Current catalog differs: ' + str(relative))


def history():
    check(sha(BASE_LEDGER) == BASE_PIN, 'Changed predecessor ledger')
    ledger = read(BASE_LEDGER)
    pins = {str(BASE_LEDGER): BASE_PIN}
    def linked(entry):
        path = Path(entry['archive'])
        check(sha(path) == entry['sha256'], 'Changed seed ancestor')
        pins[str(path)] = entry['sha256']
        return read(path)
    original, dungeon = linked(ledger['historical']), linked(ledger['dungeon'])
    values = set(original['historical'] + original['first'] + original['second'] + dungeon['reserved'] + ledger['reserved'])
    for entry in ledger['preceding']:
        values.update(linked(entry)['reserved'])
    check(len(values) == 885164, 'Incomplete inherited exclusions')
    for path in sorted((ROOT / 'TestResults').glob('tower-balance-pass-*-owner-*/seed-ledger.json')):
        pins[str(path)] = sha(path)
        new = read(path)['reserved']
        check(not values.intersection(new), 'Overlapping Tower balance panels')
        values.update(new)
    return values, pins


def audit(output):
    files = authenticate(output)
    q, result = read(output / 'request.json'), read(output / 'result.json')
    check(read(output / 'completion.json')['status'] == 'Complete', 'Incomplete native operation')
    if q['mode'] == 'prepare':
        check(result['status'] == 'PreparedNoFights' and result['fights'] == 0, 'Preparation ran fights')
        return dict(status='VerifiedPreparation', files=len(files), fights=0)
    cells = read(output / ('evaluation-cells.json' if q['mode'] == 'search' else 'cells.json'))
    trials = [json.loads(line) for line in (output / 'evaluation/trials.jsonl').read_text().splitlines()]
    check(len(trials) == len(cells) * len(q['seeds']), 'Wrong evaluation count')
    for i, (cell, row) in enumerate(zip(cells, result['rows'], strict=True)):
        wins, health, seconds = 0, 0, 0
        for j, seed in enumerate(q['seeds']):
            t = trials[i * len(q['seeds']) + j]
            check(t['stage'] == cell['id'] and t['seed'] == seed, 'Changed schedule')
            recipe = read(output / 'evaluation/recipes' / (t['recipe'] + '.json'))
            check(recipe == {**cell['scenario'], 'seeds': q['seeds']}, 'Changed composition or budget')
            raw = json.loads(gzip.decompress((output / 'evaluation/battles' / (t['id'] + '.json.gz')).read_bytes()))
            check(raw['battle']['seed'] == seed, 'Changed battle seed')
            check(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Inconsistent victory')
            wins += raw['succeeded']; health += raw['guardianHealthRemainingPercent']; seconds += raw['displayDurationSeconds']
        n = len(q['seeds'])
        check(row['id'] == cell['id'] and row['wins'] == wins and row['samples'] == n, 'Wrong reported wins')
        check(abs(row['meanGuardianHealth'] - health/n) < 1e-6 and abs(row['meanSeconds'] - seconds/n) < 1e-6, 'Wrong means')
    check(result['fights'] == q['maximumFights'] == read(output / 'completion.json')['completed'], 'Wrong fight accounting')
    verified = dict(status='Verified', files=len(files), fights=result['fights'], evaluationFights=len(trials),
                    nativeSearchReconstruction=q['mode'] == 'search', resultSha256=sha(output / 'result.json'))
    if q['mode'] == 'confirm':
        z = NormalDist().inv_cdf(1-0.05/(2*len(cells)))
        bounds = []
        for row in result['rows']:
            n, proportion = row['samples'], row['wins']/row['samples']
            denominator = 1+z*z/n
            center = (proportion+z*z/(2*n))/denominator
            radius = z*((proportion*(1-proportion)/n+z*z/(4*n*n))**0.5)/denominator
            bounds.append(dict(id=row['id'], wins=row['wins'], samples=n, lower=max(0, center-radius), upper=min(1, center+radius)))
        verdict = 'Pass' if max(b['upper'] for b in bounds) <= 0.5 and max(b['lower'] for b in bounds) >= 0.1 else (
            'Fail' if any(b['wins']/b['samples'] > 0.5 for b in bounds) or max(b['upper'] for b in bounds) < 0.1 else 'Inconclusive')
        verified['assessment'] = dict(verdict=verdict, familySize=len(cells), alpha=0.05,
            method='approximate Bonferroni-Wilson', bounds=bounds)
    return verified


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--mode', choices=['prepare', 'screen', 'search', 'confirm'], required=True)
    p.add_argument('--name', required=True, help='Unique phase identifier')
    p.add_argument('--artifacts', type=Path, required=True)
    p.add_argument('--source', type=Path)
    p.add_argument('--search-gear', help='Search at this measured gear profile; default selects the strongest measured cell')
    p.add_argument('--samples', type=int, default=32)
    p.add_argument('--floor', type=int, default=5)
    p.add_argument('--native-seconds', type=int, choices=[840, 1140], default=840,
                   help='1140 requires a separately declared floor-15 confirmation; process allowance adds 60 seconds')
    p.add_argument('--add-search', type=Path, action='append', default=[], help='Add generated finalists at every frozen gear profile; repeat for multiple completed searches')
    p.add_argument('--retain-search-references', action='store_true', help='Also retain exact projected reference recipes at their evaluated profile')
    p.add_argument('--add-references', type=Path, action='append', default=[],
                   help='Import only the three exact saved references; retain the existing family unchanged')
    p.add_argument('--current-content', action='store_true',
                   help='Seed-free preparation from saved cells and current content; target floor and catalogs must match')
    p.add_argument('--health-factor', type=float, default=1.0, help='Multiply boss health in an isolated content copy only')
    p.add_argument('--offense-factor', type=float, default=1.0, help='Multiply boss offense in the same isolated copy')
    a = p.parse_args()
    check(a.name.replace('-', '').isalnum(), 'Unsafe phase name')
    check(16 <= a.samples <= 512, 'Bounded panel required')
    check(a.search_gear is None or a.mode == 'search', 'Search gear is only valid in search mode')
    check(not a.retain_search_references or a.add_search, 'Reference retention requires a completed search')
    check(not a.current_content or a.mode == 'prepare' and a.source is not None,
          'Current-content refresh requires seed-free preparation from a completed source')
    check(not a.add_references or a.mode in ('prepare', 'screen', 'confirm') and a.source is not None,
          'Reference-only import requires a saved family and a fixed-family mode')
    check(a.native_seconds == 840 or a.mode == 'confirm' and a.floor == 15,
          'Larger predeclared envelope is only for floor-15 confirmation')
    scaled_guardian({'health': 1, 'offense': 1}, a.health_factor, a.offense_factor)
    owner = ROOT / f'TestResults/tower-balance-pass-{a.name}-owner-20260929'
    output = ROOT / f'TestResults/tower-balance-pass-{a.name}-study-20260929'
    artifacts = a.artifacts.resolve()
    check(not owner.exists() and not output.exists(), 'Fresh phase paths required; no retries or overwrite')
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    check((tests / 'EssenceSystem.Tests.dll').is_file(), 'Build through run-tests.ps1 first')
    excluded, pins = history()
    api = ROOT / 'LL/src/API/API.LL'
    fixtures = ROOT / 'LL/tools/BalanceHarness/Fixtures'
    source_cells = None
    benchmark = 1
    if a.mode != 'prepare' or a.source is not None:
        check(a.source is not None, 'A completed source is required')
        source = a.source.resolve()
        authenticate(source)
        check(read(source / 'completion.json')['status'] == 'Complete', 'Failed source')
        pins[str(source / 'files.json')] = sha(source / 'files.json')
        api = source / 'content'
        source_cells = read(source / ('evaluation-cells.json' if (source / 'evaluation-cells.json').exists() else 'cells.json'))
        if a.current_content:
            api = ROOT / 'LL/src/API/API.LL'
            validate_current_content(source, api, a.floor)
        if a.mode == 'search':
            rows = read(source / 'result.json')['rows']
            source_cells, benchmark = select_search_references(source_cells, rows, a.search_gear)
    elif a.floor == 5:
        check(sha(EARNED / 'files.json') == EARNED_PIN, 'Changed earned archive')
        manifest = read(EARNED / 'files.json')
        for path in EARNED.glob('*--continuation.json.gz'):
            check(sha(path) == manifest[path.name], 'Changed earned composition')
            pins[str(path)] = manifest[path.name]
        pins[str(EARNED / 'files.json')] = EARNED_PIN
    owner.mkdir()
    shutil.copy2(Path(__file__), owner / 'owner-source.py')
    reference_coverage = []
    for added in a.add_references:
        search_root = added.resolve()
        authenticate(search_root)
        check(read(search_root / 'completion.json')['status'] == 'Complete' and
              read(search_root / 'request.json')['mode'] == 'search', 'Completed search required')
        pins[str(search_root / 'files.json')] = sha(search_root / 'files.json')
        reference_coverage.extend(append_references(source_cells, read(search_root / 'evaluation-cells.json'), search_root))
    if a.add_references:
        write(owner / 'reference-coverage.json', reference_coverage)
        pins[str(owner / 'reference-coverage.json')] = sha(owner / 'reference-coverage.json')
    finalist_coverage = []
    for added in a.add_search:
        check(a.mode in ('screen', 'confirm') and source_cells is not None, 'Only fixed-family evaluations may add finalists')
        search_root = added.resolve()
        authenticate(search_root)
        check(read(search_root / 'completion.json')['status'] == 'Complete' and
              read(search_root / 'request.json')['mode'] == 'search', 'Completed search required')
        pins[str(search_root / 'files.json')] = sha(search_root / 'files.json')
        evaluated = read(search_root / 'evaluation-cells.json')
        finalist_coverage.extend(append_search_finalists(source_cells, evaluated, search_root))
        if a.retain_search_references:
            reference_coverage.extend(append_references(source_cells, evaluated, search_root))
    if a.add_search:
        write(owner / 'search-coverage.json', dict(finalists=finalist_coverage, references=reference_coverage))
        pins[str(owner / 'search-coverage.json')] = sha(owner / 'search-coverage.json')
    if a.health_factor != 1 or a.offense_factor != 1:
        check(a.mode in ('screen', 'confirm'), 'Boss variants are fixed-family studies only')
        isolated = owner / 'candidate-content'
        shutil.copytree(api, isolated)
        path = isolated / 'Data/world-tower/tower-floors.json'
        data = read(path)
        floor = next(f for f in data['floors'] if f['floorNumber'] == a.floor)
        before = floor['guardianScaling']
        floor['guardianScaling'] = scaled_guardian(before, a.health_factor, a.offense_factor)
        path.write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')
        write(owner / 'candidate.json', dict(floor=a.floor, healthBefore=before['health'],
              healthAfter=floor['guardianScaling']['health'], healthFactor=a.health_factor,
              offenseBefore=before['offense'], offenseAfter=floor['guardianScaling']['offense'], offenseFactor=a.offense_factor,
              productionChanged=False))
        api = isolated
    if source_cells is not None:
        write(owner / 'cells.json', source_cells)
        pins[str(owner / 'cells.json')] = sha(owner / 'cells.json')
    history_values = sorted(excluded)
    write(owner / 'history.json', history_values)
    pins[str(owner / 'history.json')] = sha(owner / 'history.json')
    seeds, search = [], []
    if a.mode != 'prepare':
        reserved, cursor = [], 0
        total = a.samples + (109 if a.mode == 'search' else 0)
        while len(reserved) < total:
            value = int.from_bytes(hashlib.sha256(f'{VERSION}|{a.name}|{cursor}'.encode()).digest()[:4], 'little', signed=True)
            cursor += 1
            if value not in excluded:
                excluded.add(value); reserved.append(value)
        search = reserved[:109] if a.mode == 'search' else []
        seeds = reserved[109:] if a.mode == 'search' else reserved
        write(owner / 'seed-ledger.json', dict(version=VERSION, state='ReservedIncludingUnconsumed',
              predecessorPins=pins.copy(), reserved=reserved, exclusionUnionCount=len(excluded)))
        pins[str(owner / 'seed-ledger.json')] = sha(owner / 'seed-ledger.json')
    for folder, pattern in [(api / 'Data', '*.json'), (fixtures, '*.json'), (tests, '*.dll')]:
        for path in folder.rglob(pattern):
            pins[str(path)] = sha(path)
    for path in [api / 'appsettings.json', Path(__file__), ROOT / 'build/run-tests.ps1',
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalancePassTests.cs',
                 ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs']:
        pins[str(path)] = sha(path)
    maximum = 0 if a.mode == 'prepare' else 528 + 5*a.samples if a.mode == 'search' else len(source_cells)*a.samples
    q = dict(mode=a.mode, apiRoot=str(api), fixtures=str(fixtures), output=str(output), floor=a.floor, seeds=seeds,
             searchSeeds=search, cells=str(owner/'cells.json') if source_cells else None,
             earned=str(EARNED) if a.mode == 'prepare' and a.floor == 5 and source_cells is None else None,
             history=str(owner/'history.json'), benchmark=benchmark, maximumFights=maximum, inputHashes=pins)
    if a.native_seconds != 840:
        q['nativeSeconds'] = a.native_seconds
    write(owner / 'request.json', q)
    spec = importlib.util.spec_from_file_location('balance_pass_owner', ROOT / 'build/bounded_windows_process.py')
    process = importlib.util.module_from_spec(spec); sys.modules[spec.name] = process; spec.loader.exec_module(process)
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT/'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests.Frozen_tower_balance_pass']
    os.environ['LL_TOWER_BALANCE_PASS'] = str(owner / 'request.json')
    receipt = process.run(command, ROOT, owner/'execution.log', time.monotonic()+a.native_seconds+60, log_byte_limit=1048576)
    write(owner / 'process.json', receipt)
    check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Bounded operation failed; preserve outputs')
    verified = audit(output)
    if a.current_content:
        check(read(source / 'scope.json')['settings'] == read(output / 'scope.json')['settings'],
              'Current native combat settings differ; preparation preserved, no fights permitted')
    write(owner / 'independent-audit.json', verified)
    write(owner / 'completion.json', dict(status='Complete', manifestPin=sha(output/'files.json'), result=read(output/'result.json')))
    summary = {k: v for k, v in verified.items() if k != 'assessment'}
    if 'assessment' in verified:
        summary['verdict'] = verified['assessment']['verdict']
    print(json.dumps(dict(output=str(output), **summary)), flush=True)


if __name__ == '__main__':
    main()
