"""Apply only a verified passing local calibration and check exact combat parity.

This does not deploy content. Retention is staged and validated separately before
the caller atomically merges it under the existing catalog's exclusive lock.
"""
import argparse
import concurrent.futures
import copy
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess

spec = importlib.util.spec_from_file_location('refinement', Path(__file__).with_name('refine-tower-search-calibration.py'))
refinement = importlib.util.module_from_spec(spec); spec.loader.exec_module(refinement)
cal = refinement.cal
LIVE = cal.ROOT / 'LL/src/API/API.LL'


def verified(out):
    p = refinement.check(out); selection = cal.read(out / 'selection.json'); result = cal.read(out / 'assessment-parallel.json')
    if result['protocolSha256'] != cal.sha(out / 'protocol.json') or result['selectionSha256'] != cal.sha(out / 'selection.json'):
        raise ValueError('Assessment differs from frozen campaign.')
    reports = []
    for i, digest in result['partitionReportHashes'].items():
        path = out / 'parallel-assessments' / i / 'assessment.json'
        if cal.sha(path) != digest: raise ValueError('Verified partition changed.')
        reports.append(cal.read(path))
    entries = cal.read(out / 'portfolio.json')
    expected = refinement.expanded.assess([e['id'] for e in entries], reports, p)
    if any(result.get(k) != v for k, v in expected.items()): raise ValueError('Global arithmetic changed.')
    return p, selection, result, entries


def acceptance(out, report, precision_directory=None):
    if precision_directory is None:
        if report['assessment'] != 'Pass': raise ValueError('Only a global Pass permits local application.')
        return None
    spec = importlib.util.spec_from_file_location('precision', Path(__file__).with_name('resolve-tower-calibration-precision.py'))
    precision = importlib.util.module_from_spec(spec); spec.loader.exec_module(precision)
    p, combined = precision.read_proof(precision_directory)
    if (Path(p['source']).resolve() != out.resolve() or combined['assessment'] != 'Pass'
            or p['sourceAssessmentSha256'] != cal.sha(out / 'assessment-parallel.json')):
        raise ValueError('A separate passing precision decision must cover this exact full calibration.')
    return {'directory': str(precision_directory.resolve()), 'protocolSha256': cal.sha(precision_directory / 'protocol.json'),
        'assessmentSha256': cal.sha(precision_directory / 'assessment.json'), 'assessmentPolicy': combined['assessmentPolicy'],
        'originalAssessment': report['assessment'], 'combinedAssessment': combined['assessment']}


def prepare(out, expected_live_hash, precision_directory=None):
    p, selected, report, entries = verified(out)
    precision_proof = acceptance(out, report, precision_directory)
    path = LIVE / 'Data' / cal.FLOORS
    if cal.sha(path) != expected_live_hash: raise ValueError('Live content changed since the declared baseline.')
    if cal.read(path) != cal.read(out / 'baseline-root/Data' / cal.FLOORS): raise ValueError('Live content differs from the experiment baseline.')
    for name, digest in p['contentHashes'].items():
        if name != cal.FLOORS and cal.sha(LIVE / 'Data' / name) != digest: raise ValueError('Other live input changed: ' + name)
    candidate = out / 'variants/selected/Data' / cal.FLOORS
    if cal.sha(candidate) != selected['selectedContentSha256']: raise ValueError('Selected content changed.')
    settings_hash = cal.sha(LIVE / 'appsettings.json')
    if settings_hash != cal.sha(out / 'variants/selected/appsettings.json'):
        raise ValueError('Live application settings differ from the experiment; review combat settings before application.')
    original = cal.read(path); expected = copy.deepcopy(original)
    values = next(f['guardianScaling'] for f in cal.read(candidate)['floors'] if f['floorNumber'] == p['floor'])
    values = {k: values[k] for k in ('health', 'offense')}
    next(f for f in expected['floors'] if f['floorNumber'] == p['floor'])['guardianScaling'].update(values)
    if expected != cal.read(candidate): raise ValueError('Selected content changes more than linked Health/offense.')
    raw = path.read_bytes()
    start = re.search(rb'"floorNumber"\s*:\s*' + str(p['floor']).encode() + rb'\s*,', raw).end()
    region = re.search(rb'"guardianScaling"\s*:\s*\{[^}]+\}', raw[start:]); left, right = start + region.start(), start + region.end()
    block = raw[left:right]
    for key, value in values.items():
        block, count = re.subn(rb'("' + key.encode() + rb'"\s*:\s*)[0-9.]+',
            lambda m: m[1] + format(value, '.6f').rstrip('0').rstrip('.').encode(), block)
        if count != 1: raise ValueError('Ambiguous scaling field.')
    raw = raw[:left] + block + raw[right:]
    if json.loads(raw.decode('utf-8-sig')) != expected: raise ValueError('Content patch differs from reviewed values.')
    folder = out / 'application'; folder.mkdir()
    shutil.copy2(path, folder / 'before.json')
    with (folder / 'proposed.json').open('xb') as stream: stream.write(raw)
    shutil.copy2(Path(__file__), folder / 'producing-finish-script.py')
    # Preserve parity for every published competitive search snapshot.
    published = cal.ROOT / 'Balance Harness/Competitive-Kharad-Recipes-20260912'
    by_recipe = {cal.recipe_key(e['scenario']): e['id'] for e in entries}
    parity = {by_recipe[cal.recipe_key(cal.read(published / (n + '.json')))] for n in
        ('small-retained', 'large-independent', 'large-retained', 'challenger-retained-1', 'challenger-retained-2')}
    top = sorted(report['cells'], key=lambda c: (-c['wins'], c['id']))[:10]
    parity.update(c['id'] for c in top)
    diagnostic = out / 'current-engine-diagnostic/proof.json'
    earlier_checks = cal.read(diagnostic)['combats'] if diagnostic.exists() else 0
    if diagnostic.exists() and cal.read(diagnostic)['status'] != 'AllReportsMatched':
        raise ValueError('Earlier current-engine diagnostic did not match.')
    cal.save(folder / 'plan.json', {'status': 'PreparedBeforeApplication', 'sourceAssessmentSha256': cal.sha(out / 'assessment-parallel.json'),
        'scriptSha256': cal.sha(Path(__file__)), 'liveBeforeSha256': cal.sha(path), 'proposedSha256': cal.sha(folder / 'proposed.json'),
        'liveSettingsSha256': settings_hash,
        'precisionProof': precision_proof,
        'floor': p['floor'], 'values': values, 'parityIds': sorted(parity), 'strongest': top[0]['id'],
        'paritySeeds': cal.read(out / 'seed-ledger.json')['schedules']['confirmation'][:20],
        'priorDiagnosticCombats': earlier_checks, 'maximumChecks': len(parity)*40 + 14*10 + 4 + earlier_checks, 'reserve': 1000})
    print(json.dumps({'prepared': values, 'parityTeams': len(parity)}), flush=True)


def application_plan(out):
    plan = cal.read(out / 'application/plan.json')
    if plan['scriptSha256'] != cal.sha(Path(__file__)) or plan['maximumChecks'] > plan['reserve']:
        raise ValueError('Application plan changed or exceeds reserve.')
    if plan['sourceAssessmentSha256'] != cal.sha(out / 'assessment-parallel.json'):
        raise ValueError('Application assessment changed.')
    proof = plan.get('precisionProof')
    current = acceptance(out, verified(out)[2], Path(proof['directory']) if proof else None)
    if current != proof: raise ValueError('Application precision proof changed.')
    return plan


def apply(out):
    protocol, _, _, _ = verified(out); plan = application_plan(out); path = LIVE / 'Data' / cal.FLOORS
    engine = cal.read(out / 'application/current-engine-parity.json')
    if engine['status'] != 'AllReportsMatched': raise ValueError('Current engine parity is required before application.')
    current = cal.ROOT / 'LL/tools/BalanceHarness/bin/Release/net10.0'
    for name, digest in engine['assemblies'].items():
        if cal.sha(current / name) != digest: raise ValueError('Current engine changed after parity: ' + name)
    if cal.sha(path) != plan['liveBeforeSha256'] or cal.sha(out / 'application/proposed.json') != plan['proposedSha256']:
        raise ValueError('Application inputs changed.')
    if cal.sha(LIVE / 'appsettings.json') != plan['liveSettingsSha256']:
        raise ValueError('Live application settings changed after preparation.')
    for name, digest in protocol['contentHashes'].items():
        if name != cal.FLOORS and cal.sha(LIVE / 'Data' / name) != digest:
            raise ValueError('Other live content changed after preparation: ' + name)
    if (out / 'application/applied.json').exists(): raise FileExistsError('Application already recorded.')
    path.write_bytes((out / 'application/proposed.json').read_bytes())
    cal.save(out / 'application/applied.json', {'status': 'AppliedLocally', 'sha256': cal.sha(path), 'values': plan['values'],
        'precisionProof': plan.get('precisionProof'), 'deployment': 'None'})


def current_batch(out, label, content):
    plan = application_plan(out); entries = cal.read(out / 'portfolio.json')
    chosen = [e for e in entries if e['id'] in plan['parityIds']]
    executable = out / 'application/current-executable/BalanceHarness.dll'
    def run(e):
        scenario = {**e['scenario'], 'seeds': plan['paritySeeds']}
        path = out / 'scenarios' / (label + '-' + e['id'] + '.json'); cal.save(path, scenario)
        target = out / 'runs' / (label + '-' + e['id'])
        with (out / 'logs' / (label + '-' + e['id'] + '.log')).open('x', encoding='utf-8') as log:
            subprocess.run(['dotnet', str(executable), 'tower', '--scenario', str(path), '--content-root', str(content), '--output', str(target)],
                cwd=cal.ROOT, stdout=log, stderr=subprocess.STDOUT, check=True)
        score = cal.read(target / 'scorecard.json')
        before = cal.read(out / 'runs' / ('confirmation-' + e['id']) / 'tower-results.json')
        after = cal.read(target / 'tower-results.json')
        if (score['status'] != 'Complete' or score['valid'] != len(plan['paritySeeds'])
                or len(after) != len(plan['paritySeeds']) or after != {k: before[k] for k in after}):
            raise ValueError('Current engine combat parity mismatch: ' + e['id'])
        return {'id': e['id'], 'matched': len(after)}
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool: return list(pool.map(run, chosen))


def check_engine(out):
    verified(out); application_plan(out)
    source = cal.ROOT / 'LL/tools/BalanceHarness/bin/Release/net10.0'
    target = out / 'application/current-executable'; shutil.copytree(source, target)
    assemblies = {p.name: cal.sha(p) for p in target.glob('*.dll')}
    matches = current_batch(out, 'current-engine-parity', out / 'variants/selected')
    cal.save(out / 'application/current-engine-parity.json', {'status': 'AllReportsMatched', 'assemblies': assemblies,
        'teams': matches, 'combats': sum(r['matched'] for r in matches)})
    print(f'Current engine: {sum(r["matched"] for r in matches)} exact reports matched.', flush=True)


def parity(out):
    p, _, _, entries = verified(out); plan = application_plan(out)
    if cal.sha(LIVE / 'Data' / cal.FLOORS) != plan['proposedSha256']: raise ValueError('Applied content changed.')
    matches = current_batch(out, 'live-parity', LIVE)
    regression = []
    for floor in range(1, 16):
        if floor == p['floor']: continue
        path = cal.ROOT / f'TestResults/balance/tower-floor-1-linked-tuning-20260911/regression-scenarios/floor-{floor}.json'
        if floor == 1: path = cal.ROOT / 'TestResults/balance/tower-post-calibration-search-20260911/published/floor-1-04.json'
        s = cal.read(path); seeds = plan['paritySeeds'][:5]
        entry = {'id': f'floor-{floor}', 'scenario': s}
        before = cal.batch(out, f'regression-before-f{floor}', out / 'baseline-root', [entry], seeds)
        after = cal.batch(out, f'regression-after-f{floor}', LIVE, [entry], seeds)
        left = cal.read(out / before['rows'][0]['run'] / 'tower-results.json')
        right = cal.read(out / after['rows'][0]['run'] / 'tower-results.json')
        if left != right: raise ValueError('An unaffected floor changed: ' + str(floor))
        regression.append({'floor': floor, 'matched': len(right)})
    cal.save(out / 'application/parity.json', {'local': matches, 'otherFloors': regression,
        'combats': sum(r['matched'] for r in matches) + 2*sum(r['matched'] for r in regression)})


def replay(out):
    plan = application_plan(out); folder = out / 'runs' / ('confirmation-' + plan['strongest']); first = {}
    for trial in cal.read(folder / 'scorecard.json')['trials']:
        first.setdefault(trial['report']['battle']['summary']['contentOutcome'], trial['id'])
    results = []; destination = out / 'application/replays'; destination.mkdir()
    for outcome, battle in first.items():
        with (destination / (battle + '.json')).open('x', encoding='utf-8') as stdout, (destination / (battle + '.log')).open('x', encoding='utf-8') as stderr:
            subprocess.run(['dotnet', str(out / 'executable/BalanceHarness.dll'), 'replay', '--run', str(folder), '--battle', battle, '--detailed'],
                cwd=cal.ROOT, stdout=stdout, stderr=stderr, check=True)
        if 'saved preparation, combat and outcome matched' not in (destination / (battle + '.log')).read_text(encoding='utf-8-sig'):
            raise ValueError('Detailed replay did not match.')
        results.append({'outcome': outcome, 'battle': battle, 'matched': True})
    cal.save(out / 'application/replays.json', results)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('phase', choices=['prepare', 'check-engine', 'apply', 'parity', 'replay'])
    parser.add_argument('--out', type=Path, required=True); parser.add_argument('--expected-live-hash')
    parser.add_argument('--precision', type=Path, help='Separate verified full-family precision decision, if the original calibration was inconclusive.')
    args = parser.parse_args()
    if args.phase == 'prepare':
        if not args.expected_live_hash: parser.error('--expected-live-hash is required')
        prepare(args.out.resolve(), args.expected_live_hash, args.precision.resolve() if args.precision else None)
    else: {'check-engine': check_engine, 'apply': apply, 'parity': parity, 'replay': replay}[args.phase](args.out.resolve())
