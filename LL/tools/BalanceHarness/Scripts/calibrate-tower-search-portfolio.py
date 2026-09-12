"""Linked Health/Power calibration with complete, partitioned portfolio confirmation.

Partitions only bound archive verification work. Acceptance uses one global
Bonferroni-Wilson family, never the individual partitions' viability decisions.
"""
import argparse
import concurrent.futures
import copy
import hashlib
import json
import math
from pathlib import Path
import shutil
import statistics
import struct
import subprocess

ROOT = Path(__file__).resolve().parents[4]
FLOORS = 'world-tower/tower-floors.json'
NAMES = ['small-independent', 'small-retained', 'large-independent', 'large-retained']
COARSE = [1, 1.1, 1.2, 1.4, 1.6, 2, 3, 4]
SAMPLES = 250
PARTITION = 350


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, ensure_ascii=False); stream.write('\n')


def command(out, args, label, allowed=(0,)):
    path = out / 'logs' / (label + '.log'); path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('x', encoding='utf-8') as stream:
        result = subprocess.run(['dotnet', str(out / 'executable/BalanceHarness.dll'), *map(str, args)],
                                cwd=ROOT, stdout=stream, stderr=subprocess.STDOUT)
    if result.returncode not in allowed: raise RuntimeError(f'{label}: exit {result.returncode}, see {path}')
    return result.returncode


def integers(value):
    if isinstance(value, dict):
        for child in value.values(): yield from integers(child)
    elif isinstance(value, list):
        for child in value: yield from integers(child)
    elif type(value) is int: yield value


def recipe_key(scenario):
    party = copy.deepcopy(scenario['party'])
    for member in party:
        b = member['build']; b['equipment'].sort(key=lambda e: e['slot'])
        b['identityEssenceIds'] = b.get('identityEssenceIds') or b['essenceIds']
    return hashlib.sha256(json.dumps(party, sort_keys=True, ensure_ascii=False).encode()).hexdigest()


def check(out):
    p = read(out / 'protocol.json')
    if sha(Path(__file__)) != p['scriptSha256']: raise RuntimeError('Use the frozen producing script.')
    for file, digest in p['frozenFiles'].items():
        if sha(out / file) != digest: raise RuntimeError('Frozen file changed: ' + file)
    return p


def freeze(out, source, previous, ledgers, seed):
    if out.exists(): raise FileExistsError('Choose a new calibration directory.')
    source_protocol = read(source / 'protocol.json')
    for file, digest in source_protocol['frozenFiles'].items():
        if sha(source / file) != digest: raise RuntimeError('Source frozen input changed: ' + file)
    completions = read(source / 'search-completion.json')
    if {r['name'] for r in completions} != set(NAMES): raise ValueError('Complete four-cell search required.')
    for row in completions:
        if sha(source / 'studies' / row['name'] / 'study.json') != row['reportSha256']:
            raise RuntimeError('Source completed study changed.')
    out.mkdir(parents=True)
    for src, dest in [('executable', 'executable'), ('content', 'baseline-root')]:
        shutil.copytree(source / src, out / dest)
    entries = {}; mandatory = set(); shortlist = set(); origins = {}

    def add(scenario, origin, finalist=False, coarse=False):
        s = copy.deepcopy(scenario); s['seeds'] = []; s['id'] = 'competitive-calibration-floor-5'
        if 'assumptions' not in s: s['assumptions'] = s.pop('notes', ['Fixed competitive calibration recipe'])
        key = recipe_key(s)
        if key not in entries: entries[key] = {'id': 'party-' + key[:24], 'scenario': s, 'sources': [], 'finalist': False}
        e = entries[key]; e['sources'].append(origin); e['finalist'] |= finalist
        if finalist: mandatory.add(e['id'])
        if coarse: shortlist.add(e['id'])
        return e['id']

    for name in NAMES:
        folder = source / 'studies' / name; report = read(folder / 'study.json'); d = read(folder / 'definition.json')
        if report['status'] != 'Complete': raise ValueError('Incomplete search.')
        cohort = report['confirmation']['definition']['cohorts'][0]
        ids = {b['partyId'] for b in report['conclusion']['earlierBreaches']}
        short_ids = {p['id'] for p in report['discovery']['discoveryShortlist']}; ids.update(short_ids)
        parties = {p['party']['id']: p['party'] for arm in report['discovery']['arms'] for p in arm['proposals']}
        if not ids <= parties.keys(): raise RuntimeError('A breach recipe cannot be resolved.')
        for pid in sorted(ids):
            scenario = {'schemaVersion': 1, 'id': d['id'], 'floorNumber': 5, 'startsAt': d['startsAt'],
                'preparationState': 'uncleared-no-contributions', 'assumptions': ['Exact recorded search candidate'],
                'seeds': [], 'party': copy.deepcopy(d['contexts'][0]['characterTemplates'])}
            for member in scenario['party']:
                member['build']['essenceIds'] = parties[pid]['builds'][str(member['partySlot'])]
                member['build']['identityEssenceIds'] = [f'neutral-identity-slot-{i}' for i in range(1, 6)]
            add(scenario, {'study': name, 'partyId': pid, 'role': 'shortlist' if pid in short_ids else 'earlier-breach'}, coarse=pid in short_ids)
        for cell in report['confirmation']['definition']['cells']:
            add(cell['scenario'], {'study': name, 'cell': cell['id'], 'role': cell['role']}, cell['role'] == 'generated', True)
        origins[name] = sha(folder / 'study.json')
    old = read(previous)
    if old['cohort'] != cohort: raise ValueError('Previous portfolio differs in budget/equipment.')
    for e in old['entries']: add(e['scenario'], {'previousPortfolio': str(previous), 'id': e['id']})
    entries = sorted(entries.values(), key=lambda e: e['id'])
    if len(entries) > 2000 or len(mandatory) > 32: raise ValueError('Freeze a larger explicit campaign without dropping recipes.')
    used = set()
    all_ledgers = [source / 'seed-ledger.json', *ledgers]
    for ledger in all_ledgers: used.update(integers(read(ledger)))
    historical = sorted(used); schedules = {}
    for label, count in [('coarse', 16), ('fine', 64), ('confirmation', SAMPLES)]:
        values = []
        for i in range(100000):
            value = struct.unpack('<i', hashlib.sha256(f'competitive-calibration-v1/{seed}/{label}/{i}'.encode()).digest()[:4])[0]
            if value not in used: used.add(value); values.append(value)
            if len(values) == count: break
        if len(values) != count: raise RuntimeError('Fresh schedule allocation exhausted.')
        schedules[label] = values
    save(out / 'seed-ledger.json', {'historical': historical, 'schedules': schedules})
    save(out / 'portfolio.json', entries)
    baseline = next(f['guardianScaling'] for f in read(out / 'baseline-root/Data' / FLOORS)['floors'] if f['floorNumber'] == 5)
    shutil.copy2(Path(__file__), out / 'producing-script.py')
    files = {p.relative_to(out).as_posix(): sha(p) for folder in ('executable', 'baseline-root')
             for p in (out / folder).rglob('*') if p.is_file()}
    for file in ['portfolio.json', 'seed-ledger.json']: files[file] = sha(out / file)
    maximum = len(shortlist) * len(COARSE) * 16 + 32 * 11 * 64 + len(entries) * SAMPLES + 1000
    if maximum > 600000: raise ValueError('Calibration exceeds explicit 600,000 campaign cap.')
    save(out / 'protocol.json', {'version': 'competitive-portfolio-calibration-v1', 'status': 'FrozenBeforeCombat',
        'floor': 5, 'budget': d['budget'], 'cohort': cohort, 'baseline': baseline,
        'contentHashes': d['contentHashes'], 'settingsHash': d['settingsHash'], 'executionHash': d['executionHash'],
        'source': str(source), 'sourceReports': origins, 'previousPortfolioSha256': sha(previous),
        'historicalLedgerHashes': {str(p): sha(p) for p in all_ledgers}, 'frozenFiles': files, 'scriptSha256': sha(Path(__file__)),
        'familySize': len(entries), 'coarseParties': sorted(shortlist), 'mandatoryFineParties': sorted(mandatory),
        'coarseMultipliers': COARSE, 'coarseSamples': 16, 'fineSamples': 64, 'confirmationSamples': SAMPLES,
        'partitionSize': PARTITION, 'maximumActualBattles': maximum,
        'discovery': 'All current shortlists and controls receive 16 paired seeds at every coarse multiplier. Choose the first adjacent crossing of 25% maximum rate, otherwise adjacent points around the nearest maximum. Fine scan: eleven equally spaced linked factors, 64 new paired seeds; all generated finalists plus highest total coarse-win nonfinalists to 32. Select maximum rate nearest 25%, restricted to 18–35%, tie lower multiplier. No eligible factor ends without application.',
        'confirmation': 'Freeze the selected factor and all exact parties before confirmation. Every earlier breach, shortlist, finalist, current control and previous portfolio recipe receives the same 250 new paired seeds. Do not drop, replace or extend cells after observing outcomes.',
        'acceptance': 'Verify bounded partitions with normal Tower archive reconstruction, then one global 95% approximate Bonferroni-Wilson correction across the entire frozen family. Every global upper <=50%; at least one global lower >=10%. A raw rate >50% fails. Draws are non-wins. Partition viability outcomes do not determine the overall result.',
        'caps': 'Each normal run <=1000 fights. Each verifier partition <=1000 cells and <=100000 fights. The separate campaign cap and global-family correction are explicit; the existing single-study caps are unchanged.',
        'application': 'Only a global Pass permits changing the local floor-5 Health and offense to the exact selected values. Record live-content hash before application, reproduce first 20 confirmation seeds for each original search finalist with live content, and compare five fixed identities on unaffected floors. Reserve 1000 combats for replay/parity. Search again afterward; this fixed portfolio cannot establish global optimality.'})
    print(f'Frozen: {len(entries)} complete parties, {len(shortlist)} coarse controls, maximum {maximum} combats', flush=True)


def variant(out, label, multiplier):
    root = out / 'variants' / label; shutil.copytree(out / 'baseline-root', root)
    data = read(root / 'Data' / FLOORS); p = read(out / 'protocol.json')
    row = next(f for f in data['floors'] if f['floorNumber'] == p['floor'])
    row['guardianScaling'].update({key: round(p['baseline'][key] * multiplier, 6) for key in ['health', 'offense']})
    (root / 'Data' / FLOORS).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    return root


def batch(out, label, content, entries, seeds):
    if shutil.disk_usage(out).free < 30 * 1024**3: raise RuntimeError('Less than 30 GiB free; preserve completed evidence and stop.')
    def run(e):
        scenario = copy.deepcopy(e['scenario']); scenario['seeds'] = seeds
        path = out / 'scenarios' / (label + '-' + e['id'] + '.json'); save(path, scenario)
        target = out / 'runs' / (label + '-' + e['id'])
        command(out, ['tower', '--scenario', path, '--content-root', content, '--output', target], label + '-' + e['id'])
        score = read(target / 'scorecard.json')
        if score['status'] != 'Complete' or score['valid'] != len(seeds) or any(score[k] for k in ['invalid', 'cancelled', 'notRun']):
            raise RuntimeError('Incomplete calibration cell: ' + e['id'])
        return {'id': e['id'], 'run': target.relative_to(out).as_posix(), 'wins': score['wins'], 'valid': score['valid'], 'draws': score['draws']}
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        rows = []
        for i, row in enumerate(pool.map(run, entries), 1):
            rows.append(row)
            if i % 20 == 0: print(f'{label}: {i}/{len(entries)} complete', flush=True)
    result = {'label': label, 'rows': rows, 'maximumRate': max(r['wins'] / r['valid'] for r in rows)}
    save(out / 'observations' / (label + '.json'), result)
    print(f'{label}: strongest {result["maximumRate"]:.2%}', flush=True)
    return result


def discover(out):
    p = check(out); entries = read(out / 'portfolio.json'); schedules = read(out / 'seed-ledger.json')['schedules']
    coarse_entries = [e for e in entries if e['id'] in p['coarseParties']]; coarse = []
    for i, factor in enumerate(p['coarseMultipliers']):
        label = f'coarse-{i:02}'
        coarse.append({'multiplier': factor, **batch(out, label, variant(out, label, factor), coarse_entries, schedules['coarse'])})
    save(out / 'coarse.json', coarse)
    crossing = [(a, b) for a, b in zip(coarse, coarse[1:]) if (a['maximumRate'] - .25) * (b['maximumRate'] - .25) <= 0]
    if crossing: left, right = crossing[0]
    else:
        i = min(range(len(coarse)), key=lambda i: (abs(coarse[i]['maximumRate'] - .25), i))
        left, right = coarse[max(0, i - 1)], coarse[min(len(coarse) - 1, i + 1)]
    totals = {e['id']: sum(next(r['wins'] for r in c['rows'] if r['id'] == e['id']) for c in coarse) for e in coarse_entries}
    selected = [e for e in coarse_entries if e['id'] in p['mandatoryFineParties']]
    others = sorted([e for e in coarse_entries if e['id'] not in p['mandatoryFineParties']], key=lambda e: (-totals[e['id']], e['id']))
    selected += others[:32 - len(selected)]
    factors = sorted({round(left['multiplier'] + (right['multiplier'] - left['multiplier']) * i / 10, 6) for i in range(11)})
    save(out / 'fine-plan.json', {'entries': [e['id'] for e in selected], 'multipliers': factors})
    fine = []
    for i, factor in enumerate(factors):
        label = f'fine-{i:02}'
        fine.append({'multiplier': factor, **batch(out, label, variant(out, label, factor), selected, schedules['fine'])})
    save(out / 'fine.json', fine)
    eligible = [r for r in fine if .18 <= r['maximumRate'] <= .35]
    if not eligible:
        save(out / 'selection.json', {'status': 'NoEligibleSetting'}); return
    chosen = min(eligible, key=lambda r: (abs(r['maximumRate'] - .25), r['multiplier']))
    content = variant(out, 'selected', chosen['multiplier'])
    definitions = []; excluded = read(out / 'seed-ledger.json')['historical'] + schedules['coarse'] + schedules['fine']
    for offset in range(0, len(entries), p['partitionSize']):
        cells = []
        for e in entries[offset:offset + p['partitionSize']]:
            scenario = copy.deepcopy(e['scenario']); scenario['seeds'] = schedules['confirmation']
            cells.append({'id': e['id'], 'cohortId': p['cohort']['id'], 'role': 'generated' if e['finalist'] else 'reference', 'scenario': scenario, 'minimumSamples': SAMPLES})
        d = {'schemaVersion': 1, 'id': f'competitive-calibration-partition-{len(definitions)}', 'intervalPolicy': 'bonferroni-wilson-95-v1',
             'contentHashes': {**p['contentHashes'], FLOORS: sha(content / 'Data' / FLOORS)}, 'settingsHash': p['settingsHash'],
             'executionHash': p['executionHash'], 'cohorts': [p['cohort']], 'cells': cells, 'excludedCombatSeeds': sorted(set(excluded)), 'maximumBattles': len(cells) * SAMPLES}
        path = out / 'partitions' / (d['id'] + '.json'); save(path, d)
        definitions.append({'file': path.relative_to(out).as_posix(), 'sha256': sha(path), 'ids': [c['id'] for c in cells]})
    save(out / 'selection.json', {'status': 'FrozenBeforeConfirmation', 'multiplier': chosen['multiplier'], 'discoveryMaximumRate': chosen['maximumRate'],
        'selectedContentSha256': sha(content / 'Data' / FLOORS), 'definitions': definitions})
    print(f'Frozen multiplier {chosen["multiplier"]}; full confirmation family {len(entries)}', flush=True)


def global_assessment(frozen_ids, verified):
    if not frozen_ids or len(frozen_ids) != len(set(frozen_ids)) or len(frozen_ids) > 2000:
        raise ValueError('Invalid global family.')
    rows = [c for partition in verified for c in partition['cells']]
    if len(rows) != len(frozen_ids) or {r['id'] for r in rows} != set(frozen_ids): raise ValueError('Missing, duplicated or additional global evidence.')
    if any(p['issues'] or p['assessment'] == 'Invalid' for p in verified) or any(r['issues'] or r['valid'] != SAMPLES for r in rows):
        raise ValueError('Unverified or incomplete partition evidence.')
    z = statistics.NormalDist().inv_cdf(1 - .025 / len(rows)); checks = []
    for r in rows:
        n = r['valid']; rate = r['wins'] / n; divisor = 1 + z*z/n
        center = (rate + z*z/(2*n))/divisor
        width = z * math.sqrt(rate*(1-rate)/n + z*z/(4*n*n))/divisor
        checks.append({'id': r['id'], 'wins': r['wins'], 'valid': n, 'draws': r['draws'], 'rate': rate,
            'lower': max(0, center-width), 'upper': min(1, center+width), 'artifactHash': r['artifactHash']})
    above = any(r['rate'] > .5 for r in checks); upper = all(r['upper'] <= .5 for r in checks); viable = any(r['lower'] >= .1 for r in checks)
    status = 'Fail' if above or all(r['upper'] < .1 for r in checks) else 'Pass' if upper and viable else 'Inconclusive'
    return {'assessment': status, 'familySize': len(rows), 'familyConfidence': .95, 'z': z, 'upperSupported': upper, 'viabilitySupported': viable,
            'observedAboveCeiling': above, 'cells': checks,
            'scope': 'Complete frozen portfolio, one approximate Bonferroni-Wilson family. Partition acceptance labels are ignored. Unsearched teams remain unvalidated.'}


def confirm(out):
    p = check(out); selection = read(out / 'selection.json')
    if selection['status'] != 'FrozenBeforeConfirmation': raise RuntimeError('No selected setting.')
    if sha(out / 'variants/selected/Data' / FLOORS) != selection['selectedContentSha256']: raise RuntimeError('Selected content changed.')
    # A missing-source evaluation validates every definition before any confirmation fights.
    save(out / 'empty-sources.json', [])
    for i, partition in enumerate(selection['definitions']):
        path = out / partition['file']
        if sha(path) != partition['sha256']: raise RuntimeError('Confirmation definition changed.')
        command(out, ['tower-balance-evaluate', '--definition', path, '--sources', out / 'empty-sources.json', '--output', out / 'preflight' / str(i)], f'preflight-{i}', (2,))
        report = read(out / 'preflight' / str(i) / 'assessment.json')
        if report['issues'] or any(c['issues'] != ['Required confirmation evidence is missing.'] for c in report['cells']):
            raise RuntimeError('Invalid preflight contract.')
    entries = read(out / 'portfolio.json')
    result = batch(out, 'confirmation', out / 'variants/selected', entries, read(out / 'seed-ledger.json')['schedules']['confirmation'])
    verified = []
    for i, partition in enumerate(selection['definitions']):
        sources = [{'cellId': r['id'], 'runDirectory': r['run']} for r in result['rows'] if r['id'] in partition['ids']]
        path = out / f'sources-{i}.json'; save(path, sources)
        command(out, ['tower-balance-evaluate', '--definition', out / partition['file'], '--sources', path, '--output', out / 'assessments' / str(i)], f'assessment-{i}', (0, 1, 3))
        verified.append(read(out / 'assessments' / str(i) / 'assessment.json'))
    report = global_assessment([e['id'] for e in entries], verified)
    report.update(protocolSha256=sha(out / 'protocol.json'), selectionSha256=sha(out / 'selection.json'),
                  partitionReportHashes={str(i): sha(out / 'assessments' / str(i) / 'assessment.json') for i in range(len(verified))},
                  actualDiscoveryCombats=sum(len(o['rows']) * o['rows'][0]['valid'] for file in ['coarse.json', 'fine.json'] for o in read(out / file)),
                  actualConfirmationCombats=len(entries)*SAMPLES)
    save(out / 'assessment.json', report)
    best = max(report['cells'], key=lambda r: (r['wins'], r['id']))
    print(f'Global {report["assessment"]}: {report["familySize"]} parties; strongest {best["wins"]}/{best["valid"]}, adjusted [{best["lower"]:.2%}, {best["upper"]:.2%}]', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('phase', choices=['freeze', 'discover', 'confirm']); parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--source', type=Path); parser.add_argument('--previous', type=Path)
    parser.add_argument('--ledger', type=Path, action='append', default=[]); parser.add_argument('--seed', type=int, default=971205)
    args = parser.parse_args(); out = args.out.resolve()
    if args.phase == 'freeze':
        if not args.source or not args.previous: parser.error('--source and --previous required')
        freeze(out, args.source.resolve(), args.previous.resolve(), args.ledger, args.seed)
    elif args.phase == 'discover': discover(out)
    else: confirm(out)
