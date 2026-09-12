"""One separately bounded resolution of an inconclusive precision decision.

Keep alpha .025 for the original family, .0125 for the first fresh family and
.0125 for this selected fresh family. Every recipe stays covered; no old samples
are pooled into the new fixed 50000-seed measurement. Earlier reports are immutable.
"""
import argparse
import concurrent.futures
import copy
import hashlib
import importlib.util
from pathlib import Path
import shutil
import struct
import subprocess

spec = importlib.util.spec_from_file_location('precision', Path(__file__).with_name('confirm-tower-calibration-precision.py'))
precision = importlib.util.module_from_spec(spec); spec.loader.exec_module(precision)
cal = precision.cal
VERSION = 'selected-fresh-split-alpha-wilson-resolution-v1'
SAMPLES = 50000
BLOCK = 1000
ALPHA = .0125


def prior_intervals(previous):
    rows = previous['cells']; selected = previous['selectedIds']
    if (previous['assessmentPolicy'] != precision.VERSION or previous['assessment'] != 'Inconclusive'
            or previous['originalAlpha'] != .025 or previous['freshAlpha'] != .025
            or len(rows) != previous['familySize'] or len({r['id'] for r in rows}) != len(rows)
            or len(selected) != len(set(selected)) or any(r['rate'] > .5 for r in rows)):
        raise ValueError('Only the exact complete, inconclusive first precision decision is eligible.')
    fresh = [r for r in rows if r['sampleSource'] == 'fresh']
    if {r['id'] for r in fresh} != set(selected) or any(r['valid'] != 10000 for r in fresh):
        raise ValueError('The entire first fresh family must remain represented.')
    tightened, z = precision.intervals(fresh, ALPHA); replacements = {r['id']: r for r in tightened}
    return [replacements.get(r['id'], r) for r in rows], z


def select(previous):
    rows, _ = prior_intervals(previous)
    ids = {r['id'] for r in rows if r['upper'] > .5}
    if not any(r['lower'] >= .1 for r in rows): ids.add(min(rows, key=lambda r: (-r['rate'], r['id']))['id'])
    if not 1 <= len(ids) <= 2: raise ValueError('Resolution permits at most two unresolved teams.')
    return sorted(ids)


def assess(previous, selected_ids, blocks):
    if selected_ids != select(previous): raise ValueError('Resolution selection changed.')
    if len(blocks) != SAMPLES // BLOCK: raise ValueError('All fifty frozen blocks are required.')
    totals = {cid: {'id': cid, 'wins': 0, 'valid': 0, 'draws': 0, 'artifactHashes': []} for cid in selected_ids}
    for report in blocks:
        rows = report['cells']
        if (report['issues'] or report['assessment'] not in ('Pass', 'Fail', 'Inconclusive')
                or len(rows) != len(selected_ids) or {r['id'] for r in rows} != set(selected_ids)):
            raise ValueError('Missing, additional, duplicated or invalid normal evidence.')
        for r in rows:
            if r['issues'] or r['valid'] != BLOCK or len(r['artifactHash']) != 64: raise ValueError('Incomplete normal evidence.')
            precision.intervals([r], ALPHA)
            for k in ('wins', 'valid', 'draws'): totals[r['id']][k] += r[k]
            totals[r['id']]['artifactHashes'].append(r['artifactHash'])
    if any(len(set(r['artifactHashes'])) != SAMPLES // BLOCK for r in totals.values()):
        raise ValueError('A block was reused.')
    prior, prior_z = prior_intervals(previous); fresh, z = precision.intervals(list(totals.values()), ALPHA)
    replacements = {r['id']: {**r, 'sampleSource': 'resolution'} for r in fresh}
    cells = [replacements.get(r['id'], r) for r in prior]
    above = any(r['rate'] > .5 for r in cells)
    upper = all(r['upper'] <= .5 for r in cells); viable = any(r['lower'] >= .1 for r in cells)
    status = 'Fail' if above or all(r['upper'] < .1 for r in cells) else 'Pass' if upper and viable else 'Inconclusive'
    return {'assessmentPolicy': VERSION, 'assessment': status, 'familySize': len(cells), 'familyConfidence': .95,
        'originalAlpha': .025, 'firstFreshAlpha': ALPHA, 'resolutionAlpha': ALPHA, 'firstFreshZ': prior_z, 'resolutionZ': z,
        'selectedIds': selected_ids, 'samplesPerSelectedTeam': SAMPLES, 'actualFreshCombats': len(selected_ids)*SAMPLES,
        'upperSupported': upper, 'viabilitySupported': viable, 'observedAboveCeiling': above, 'cells': cells,
        'scope': 'All original recipes remain represented. Original simultaneous alpha=.025 bounds are unchanged. Tightened first-fresh alpha=.0125 bounds cover teams not selected for resolution. Conditional new alpha=.0125 bounds cover selected teams on exactly 50000 entirely new seeds. Approximate 95% coverage for this separate decision; no pooling, optional extension, lifetime error-rate or global-optimality claim.'}


def freeze(out, previous, seed):
    prior_plan, prior = precision.verified(previous); ids = select(prior)
    if out.exists(): raise FileExistsError('Choose a new resolution directory; preserve earlier evidence.')
    historical = sorted(set(cal.integers(cal.read(previous / 'seed-ledger.json'))))
    used = set(historical); seeds = []
    for i in range(SAMPLES * 2):
        value = struct.unpack('<i', hashlib.sha256(f'{VERSION}/{seed}/{i}'.encode()).digest()[:4])[0]
        if value not in used: used.add(value); seeds.append(value)
        if len(seeds) == SAMPLES: break
    if len(seeds) != SAMPLES: raise ValueError('Fresh schedule allocation exhausted.')
    source = Path(prior_plan['source']); p, setting, _, entries = precision.source_evidence(source)
    out.mkdir(parents=True)
    for name in ('executable', 'reader', 'content'): shutil.copytree(previous / name, out / name)
    shutil.copy2(Path(__file__), out / 'producing-resolution-script.py')
    chosen = [e for e in entries if e['id'] in ids]
    cal.save(out / 'portfolio.json', chosen)
    cal.save(out / 'seed-ledger.json', {'historical': historical, 'confirmation': seeds,
        'retentionSeeds': sorted(set(seeds) | set(cal.read(previous / 'seed-ledger.json')['confirmation']))})
    definitions = []
    for i in range(SAMPLES // BLOCK):
        cells = [{'id': e['id'], 'cohortId': p['cohort']['id'], 'role': 'generated' if e['finalist'] else 'reference',
            'scenario': {**copy.deepcopy(e['scenario']), 'seeds': seeds[i*BLOCK:(i+1)*BLOCK]}, 'minimumSamples': BLOCK} for e in chosen]
        d = {'schemaVersion': 1, 'id': f'precision-resolution-{i:02}', 'intervalPolicy': 'bonferroni-wilson-95-v1',
            'contentHashes': {**p['contentHashes'], cal.FLOORS: setting['selectedContentSha256']}, 'settingsHash': p['settingsHash'],
            'executionHash': p['executionHash'], 'cohorts': [p['cohort']], 'cells': cells,
            'excludedCombatSeeds': historical, 'maximumBattles': len(ids)*BLOCK}
        name = f'definitions/{i}.json'; cal.save(out / name, d); definitions.append(name)
    files = {path.relative_to(out).as_posix(): cal.sha(path) for path in out.rglob('*') if path.is_file()}
    cal.save(out / 'protocol.json', {'version': VERSION, 'status': 'FrozenBeforeFreshCombat', 'source': str(source),
        'sourceAssessmentSha256': prior_plan['sourceAssessmentSha256'], 'previousPrecision': str(previous),
        'previousAssessmentSha256': cal.sha(previous / 'assessment.json'), 'previousProtocolSha256': cal.sha(previous / 'protocol.json'),
        'scriptSha256': cal.sha(Path(__file__)), 'selectedContentSha256': setting['selectedContentSha256'],
        'selectedIds': ids, 'familySize': len(entries), 'samplesPerSelectedTeam': SAMPLES,
        'maximumActualBattles': len(ids)*SAMPLES, 'parallelBlocks': 8 // len(ids), 'definitions': definitions, 'frozenFiles': files,
        'selection': 'Keep the original-family alpha=.025 intervals. Recompute all first-fresh intervals simultaneously at alpha=.0125. Select every unresolved upper >50%; include the strongest team if viability is unsupported. At most two teams.',
        'confirmation': 'Exactly 50000 unused paired seeds per selected team at unchanged boss, recipe, gear and untrained-Essence budget. Fifty ordinary 1000-seed blocks. No pooling, replacement, optional extension or early acceptance.',
        'acceptance': 'Original alpha=.025, first-fresh alpha=.0125 and conditionally selected resolution alpha=.0125 cover all original recipes with a separate approximate 95% union bound. All upper bounds <=50%, at least one lower >=10%, no observed complete-recipe rate >50%. Earlier reports stay unchanged. This protocol ends after its fixed sample even if still inconclusive.'})
    print(f'Frozen resolution: {len(ids)} team(s), {len(ids)*SAMPLES} fresh fights, same {setting["multiplier"]} setting.', flush=True)


def check(out):
    p = cal.read(out / 'protocol.json')
    if p['version'] != VERSION or p['scriptSha256'] != cal.sha(Path(__file__)): raise ValueError('Resolution policy changed.')
    for name, digest in p['frozenFiles'].items():
        if cal.sha(out / name) != digest: raise ValueError('Frozen resolution input changed: ' + name)
    previous = Path(p['previousPrecision']); old_plan, old = precision.verified(previous)
    if (p['previousAssessmentSha256'] != cal.sha(previous / 'assessment.json')
            or p['previousProtocolSha256'] != cal.sha(previous / 'protocol.json') or p['source'] != old_plan['source']
            or p['sourceAssessmentSha256'] != old_plan['sourceAssessmentSha256'] or p['selectedIds'] != select(old)
            or p['selectedContentSha256'] != old_plan['selectedContentSha256'] or p['samplesPerSelectedTeam'] != SAMPLES
            or p['maximumActualBattles'] != len(p['selectedIds'])*SAMPLES or p['parallelBlocks'] != 8 // len(p['selectedIds'])):
        raise ValueError('Resolution source, selection or bounds changed.')
    ledger = cal.read(out / 'seed-ledger.json'); seeds = ledger['confirmation']
    if len(seeds) != SAMPLES or len(set(seeds)) != SAMPLES or set(seeds).intersection(ledger['historical']):
        raise ValueError('Incomplete or reused resolution schedule.')
    if len(p['definitions']) != SAMPLES // BLOCK: raise ValueError('Missing resolution block.')
    for i, name in enumerate(p['definitions']):
        if any(c['scenario']['seeds'] != seeds[i*BLOCK:(i+1)*BLOCK] for c in cal.read(out / name)['cells']):
            raise ValueError('Block differs from the frozen schedule.')
    return p, old


def confirm(out):
    p, previous = check(out); cal.save(out / 'empty-sources.json', [])
    for i, name in enumerate(p['definitions']):
        cal.command(out, ['tower-balance-evaluate', '--definition', out / name, '--sources', out / 'empty-sources.json',
            '--output', out / 'preflight' / str(i)], f'preflight-{i}', (2,))
        r = cal.read(out / 'preflight' / str(i) / 'assessment.json')
        if r['issues'] or any(c['issues'] != ['Required confirmation evidence is missing.'] for c in r['cells']):
            raise ValueError('Invalid resolution contract before combat.')
    entries = cal.read(out / 'portfolio.json'); seeds = cal.read(out / 'seed-ledger.json')['confirmation']
    def work(item):
        i, name = item; label = f'resolution-{i:02}'
        result = cal.batch(out, label, out / 'content', entries, seeds[i*BLOCK:(i+1)*BLOCK])
        sources = out / f'sources-{i}.json'; target = out / 'assessments' / str(i)
        cal.save(sources, [{'cellId': r['id'], 'runDirectory': r['run']} for r in result['rows']])
        with (out / 'logs' / f'verify-{i}.log').open('x', encoding='utf-8') as log:
            done = subprocess.run(['dotnet', str(out / 'reader/BalanceHarness.dll'), 'tower-balance-evaluate',
                '--definition', str(out / name), '--sources', str(sources), '--output', str(target)],
                cwd=cal.ROOT, stdout=log, stderr=subprocess.STDOUT)
        if done.returncode not in (0, 1, 3): raise ValueError('Normal resolution verification failed.')
        print(f'Resolution block {i+1}/{SAMPLES // BLOCK}: normal archives verified.', flush=True)
        return cal.read(target / 'assessment.json'), cal.sha(target / 'assessment.json')
    with concurrent.futures.ThreadPoolExecutor(max_workers=p['parallelBlocks']) as pool:
        complete = list(pool.map(work, enumerate(p['definitions'])))
    check(out); report = assess(previous, p['selectedIds'], [r for r, _ in complete])
    report.update(protocolSha256=cal.sha(out / 'protocol.json'), previousAssessmentSha256=p['previousAssessmentSha256'],
        sourceAssessmentSha256=p['sourceAssessmentSha256'], partitionReportHashes={str(i): h for i, (_, h) in enumerate(complete)})
    cal.save(out / 'assessment.json', report)
    strongest = max(report['cells'], key=lambda r: r['rate'])
    print(f'Resolution {report["assessment"]}: strongest {strongest["wins"]}/{strongest["valid"]}, adjusted upper {strongest["upper"]:.4%}.', flush=True)


def verified(out):
    p, previous = check(out); r = cal.read(out / 'assessment.json')
    if (r['protocolSha256'] != cal.sha(out / 'protocol.json') or r['previousAssessmentSha256'] != p['previousAssessmentSha256']
            or r['sourceAssessmentSha256'] != p['sourceAssessmentSha256']
            or set(r['partitionReportHashes']) != {str(i) for i in range(SAMPLES // BLOCK)}):
        raise ValueError('Resolution assessment provenance changed.')
    parts = []
    for i in range(SAMPLES // BLOCK):
        path = out / 'assessments' / str(i) / 'assessment.json'
        if cal.sha(path) != r['partitionReportHashes'][str(i)]: raise ValueError('Resolution partition changed.')
        parts.append(cal.read(path))
    expected = assess(previous, p['selectedIds'], parts)
    if any(r.get(k) != v for k, v in expected.items()): raise ValueError('Resolution arithmetic changed.')
    return p, r


def read_proof(out):
    version = cal.read(out / 'protocol.json')['version']
    if version == precision.VERSION: return precision.verified(out)
    if version == VERSION: return verified(out)
    raise ValueError('Unknown separately frozen precision policy.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('phase', choices=['freeze', 'confirm', 'verify'])
    parser.add_argument('--out', type=Path, required=True); parser.add_argument('--previous', type=Path)
    parser.add_argument('--seed', type=int, default=971222); args = parser.parse_args()
    if args.phase == 'freeze':
        if not args.previous: parser.error('--previous is required')
        freeze(args.out.resolve(), args.previous.resolve(), args.seed)
    elif args.phase == 'confirm': confirm(args.out.resolve())
    else: print(verified(args.out.resolve())[1]['assessment'])
