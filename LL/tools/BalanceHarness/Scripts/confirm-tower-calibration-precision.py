"""Separately frozen precision confirmation without changing a boss or its teams.

The old assessment remains unchanged. Reserve 2.5% error for simultaneous old
intervals and 2.5% for fresh intervals on every unresolved recipe. Selection
uses only the old data; fresh samples are neither pooled nor extended. Wilson
coverage is approximate, and applies to this portfolio, not to all possible teams.
"""
import argparse
import concurrent.futures
import copy
import hashlib
import importlib.util
import math
from pathlib import Path
import shutil
import statistics
import struct
import subprocess

spec = importlib.util.spec_from_file_location('refinement', Path(__file__).with_name('refine-tower-search-calibration.py'))
refinement = importlib.util.module_from_spec(spec); spec.loader.exec_module(refinement)
cal = refinement.cal
VERSION = 'selected-fresh-split-alpha-wilson-v1'
SAMPLES = 10000
BLOCK = 1000
MAX_SELECTED = 8
ALPHA = .025


def intervals(rows, alpha):
    if not rows or len({r['id'] for r in rows}) != len(rows):
        raise ValueError('A complete, distinct family is required.')
    z = statistics.NormalDist().inv_cdf(1 - alpha / (2 * len(rows)))
    result = []
    for r in rows:
        n, wins, draws = r['valid'], r['wins'], r['draws']
        if any(type(v) is not int for v in (n, wins, draws)) or n <= 0 or not 0 <= wins <= n or not 0 <= draws <= n - wins:
            raise ValueError('Invalid counts.')
        rate = wins / n; divisor = 1 + z*z/n
        center = (rate + z*z/(2*n)) / divisor
        width = z * math.sqrt(rate*(1-rate)/n + z*z/(4*n*n)) / divisor
        result.append({**r, 'rate': rate, 'lower': max(0, center-width), 'upper': min(1, center+width)})
    return result, z


def select(rows):
    old, _ = intervals(rows, ALPHA)
    if any(r['valid'] != 250 or r['rate'] > .5 for r in old):
        raise ValueError('Precision follow-up requires complete old samples with no observed ceiling breach.')
    ids = {r['id'] for r in old if r['upper'] > .5}
    if not any(r['lower'] >= .1 for r in old):
        ids.add(min(old, key=lambda r: (-r['rate'], r['id']))['id'])
    if not 1 <= len(ids) <= MAX_SELECTED:
        raise ValueError('No bounded precision follow-up is available; retain the original result.')
    return sorted(ids)


def assess(old_rows, selected_ids, verified_blocks):
    if selected_ids != select(old_rows): raise ValueError('Selection differs from the complete old family.')
    if len(verified_blocks) != SAMPLES // BLOCK: raise ValueError('All frozen blocks are required.')
    totals = {i: {'id': i, 'wins': 0, 'valid': 0, 'draws': 0, 'artifactHashes': []} for i in selected_ids}
    for report in verified_blocks:
        rows = report['cells']
        if (report['issues'] or report['assessment'] not in ('Pass', 'Fail', 'Inconclusive')
                or len(rows) != len(selected_ids) or {r['id'] for r in rows} != set(selected_ids)):
            raise ValueError('Missing, duplicate, extra or invalid fresh evidence.')
        for r in rows:
            if r['issues'] or r['valid'] != BLOCK: raise ValueError('Incomplete fresh block.')
            intervals([r], ALPHA)  # Validate integer counts, including draws.
            if len(r['artifactHash']) != 64: raise ValueError('Missing normal archive identity.')
            total = totals[r['id']]
            for k in ('wins', 'valid', 'draws'): total[k] += r[k]
            total['artifactHashes'].append(r['artifactHash'])
    for r in totals.values():
        if len(set(r['artifactHashes'])) != SAMPLES // BLOCK:
            raise ValueError('A historical block was reused.')
    old, old_z = intervals(old_rows, ALPHA)
    fresh, fresh_z = intervals(list(totals.values()), ALPHA)
    replacement = {r['id']: r for r in fresh}
    combined = [{**replacement.get(r['id'], r), 'sampleSource': 'fresh' if r['id'] in replacement else 'original'} for r in old]
    above = any(r['rate'] > .5 for r in combined)
    upper = all(r['upper'] <= .5 for r in combined)
    viable = any(r['lower'] >= .1 for r in combined)
    status = 'Fail' if above or all(r['upper'] < .1 for r in combined) else 'Pass' if upper and viable else 'Inconclusive'
    return {'assessmentPolicy': VERSION, 'assessment': status, 'familySize': len(combined),
        'familyConfidence': .95, 'originalAlpha': ALPHA, 'freshAlpha': ALPHA, 'originalZ': old_z, 'freshZ': fresh_z,
        'selectedIds': selected_ids, 'samplesPerSelectedTeam': SAMPLES, 'actualFreshCombats': len(selected_ids)*SAMPLES,
        'upperSupported': upper, 'viabilitySupported': viable, 'observedAboveCeiling': above, 'cells': combined,
        'scope': 'All original recipes remain represented. Simultaneous original 97.5% intervals cover unselected recipes; conditional fresh 97.5% intervals cover selected recipes. Their union gives approximate 95% family coverage for this separate fixed decision. No pooling, extension, overall search-optimality or lifetime error-rate claim.'}


def source_evidence(source):
    p = refinement.check(source); selected = cal.read(source / 'selection.json')
    result = cal.read(source / 'assessment-parallel.json')
    if result['protocolSha256'] != cal.sha(source / 'protocol.json') or result['selectionSha256'] != cal.sha(source / 'selection.json'):
        raise ValueError('Source assessment differs from its frozen campaign.')
    parts = []
    for i, digest in result['partitionReportHashes'].items():
        path = source / 'parallel-assessments' / i / 'assessment.json'
        if cal.sha(path) != digest: raise ValueError('Source verified partition changed.')
        parts.append(cal.read(path))
    entries = cal.read(source / 'portfolio.json')
    expected = refinement.expanded.assess([e['id'] for e in entries], parts, p)
    if any(result.get(k) != v for k, v in expected.items()): raise ValueError('Source arithmetic changed.')
    return p, selected, result, entries


def freeze(out, source, reader, seed):
    p, setting, old, entries = source_evidence(source)
    if old['assessment'] != 'Inconclusive': raise ValueError('Use this follow-up only for an inconclusive verified calibration.')
    ids = select(old['cells'])
    if out.exists(): raise FileExistsError('Preserve any previous follow-up; choose a new directory.')
    proof = cal.read(reader / 'result.json'); identity = cal.read(reader / 'plan.json')
    if proof['status'] != 'ExactEvidenceAndAssessmentMatch': raise ValueError('Reader equivalence is required.')
    for name, digest in identity['assemblies'].items():
        if cal.sha(reader / 'executable' / name) != digest: raise ValueError('Proven reader changed.')
    history = sorted(set(cal.integers(cal.read(source / 'seed-ledger.json'))))
    used = set(history); seeds = []
    for i in range(SAMPLES * 2):
        value = struct.unpack('<i', hashlib.sha256(f'{VERSION}/{seed}/{i}'.encode()).digest()[:4])[0]
        if value not in used: used.add(value); seeds.append(value)
        if len(seeds) == SAMPLES: break
    if len(seeds) != SAMPLES: raise ValueError('Fresh seed allocation exhausted.')
    out.mkdir(parents=True)
    shutil.copytree(source / 'executable', out / 'executable')
    shutil.copytree(source / 'variants/selected', out / 'content')
    shutil.copytree(reader / 'executable', out / 'reader')
    shutil.copy2(Path(__file__), out / 'producing-precision-script.py')
    cal.save(out / 'portfolio.json', [e for e in entries if e['id'] in ids])
    cal.save(out / 'seed-ledger.json', {'historical': history, 'confirmation': seeds})
    definitions = []
    for i in range(SAMPLES // BLOCK):
        cells = []
        for e in entries:
            if e['id'] not in ids: continue
            scenario = {**copy.deepcopy(e['scenario']), 'seeds': seeds[i*BLOCK:(i+1)*BLOCK]}
            cells.append({'id': e['id'], 'cohortId': p['cohort']['id'], 'role': 'generated' if e['finalist'] else 'reference',
                'scenario': scenario, 'minimumSamples': BLOCK})
        definition = {'schemaVersion': 1, 'id': f'precision-block-{i:02}', 'intervalPolicy': 'bonferroni-wilson-95-v1',
            'contentHashes': {**p['contentHashes'], cal.FLOORS: setting['selectedContentSha256']}, 'settingsHash': p['settingsHash'],
            'executionHash': p['executionHash'], 'cohorts': [p['cohort']], 'cells': cells,
            'excludedCombatSeeds': history, 'maximumBattles': len(ids)*BLOCK}
        filename = f'definitions/{i}.json'; cal.save(out / filename, definition); definitions.append(filename)
    files = {path.relative_to(out).as_posix(): cal.sha(path) for path in out.rglob('*') if path.is_file()}
    cal.save(out / 'protocol.json', {'version': VERSION, 'status': 'FrozenBeforeFreshCombat', 'source': str(source),
        'sourceAssessmentSha256': cal.sha(source / 'assessment-parallel.json'), 'scriptSha256': cal.sha(Path(__file__)),
        'sourceProtocolSha256': cal.sha(source / 'protocol.json'), 'selectedContentSha256': setting['selectedContentSha256'],
        'selectedIds': ids, 'familySize': len(entries), 'samplesPerSelectedTeam': SAMPLES, 'definitions': definitions,
        'maximumActualBattles': len(ids)*SAMPLES, 'parallelBlocks': min(4, max(1, 8 // len(ids))), 'frozenFiles': files,
        'selection': 'Recompute 97.5% simultaneous bounds across every original recipe. Select every upper above 50%; if no lower reaches 10%, also select the strongest observed recipe, tie ID. At most eight selected teams, otherwise no precision experiment.',
        'confirmation': 'Exactly 10000 entirely fresh paired seeds per selected recipe, split into ten normal 1000-seed archives. Keep boss, characters, fixed gear and untrained Essences unchanged. No pooling with original 250 samples, early acceptance, replacement or extension.',
        'acceptance': 'Original and fresh families each reserve alpha=0.025. Replace selected intervals only with their entire new sample; every original recipe remains in the combined family. Every upper <=50%, at least one lower >=10%, and no observed rate >50%. Preserve the original Inconclusive report. This is a separate approximate 95% decision, not repeated testing at an unchanged error budget.'})
    print(f'Frozen precision: {len(ids)} teams, {len(ids)*SAMPLES} fresh fights; unchanged {setting["multiplier"]} setting.', flush=True)


def check(out):
    p = cal.read(out / 'protocol.json')
    if p['version'] != VERSION or p['scriptSha256'] != cal.sha(Path(__file__)):
        raise ValueError('Precision policy changed.')
    for name, digest in p['frozenFiles'].items():
        if cal.sha(out / name) != digest: raise ValueError('Frozen precision input changed: ' + name)
    source = Path(p['source']); old_p, setting, old, entries = source_evidence(source)
    if (p['sourceAssessmentSha256'] != cal.sha(source / 'assessment-parallel.json')
            or p['sourceProtocolSha256'] != cal.sha(source / 'protocol.json')
            or p['selectedContentSha256'] != setting['selectedContentSha256'] or p['selectedIds'] != select(old['cells'])
            or p['maximumActualBattles'] != len(p['selectedIds'])*SAMPLES or p['samplesPerSelectedTeam'] != SAMPLES
            or p['parallelBlocks'] != min(4, max(1, 8 // len(p['selectedIds'])))):
        raise ValueError('Precision source, schedule or bounds changed.')
    ledger = cal.read(out / 'seed-ledger.json'); seeds = ledger['confirmation']
    if len(seeds) != SAMPLES or len(set(seeds)) != SAMPLES or set(seeds).intersection(ledger['historical']):
        raise ValueError('Fresh seed schedule is incomplete or overlaps history.')
    if len(p['definitions']) != SAMPLES // BLOCK: raise ValueError('Missing frozen block.')
    for i, name in enumerate(p['definitions']):
        d = cal.read(out / name)
        if any(c['scenario']['seeds'] != seeds[i*BLOCK:(i+1)*BLOCK] for c in d['cells']):
            raise ValueError('Block schedule differs from the frozen paired schedule.')
    return p, old


def confirm(out):
    p, old = check(out); cal.save(out / 'empty-sources.json', [])
    for i, name in enumerate(p['definitions']):
        cal.command(out, ['tower-balance-evaluate', '--definition', out / name, '--sources', out / 'empty-sources.json',
            '--output', out / 'preflight' / str(i)], f'preflight-{i}', (2,))
        r = cal.read(out / 'preflight' / str(i) / 'assessment.json')
        if r['issues'] or any(c['issues'] != ['Required confirmation evidence is missing.'] for c in r['cells']):
            raise ValueError('Invalid precision contract before combat.')
    entries = cal.read(out / 'portfolio.json'); seeds = cal.read(out / 'seed-ledger.json')['confirmation']
    def work(item):
        i, name = item
        label = f'precision-{i:02}'
        result = cal.batch(out, label, out / 'content', entries, seeds[i*BLOCK:(i+1)*BLOCK])
        sources = out / f'sources-{i}.json'; target = out / 'assessments' / str(i)
        cal.save(sources, [{'cellId': r['id'], 'runDirectory': r['run']} for r in result['rows']])
        with (out / 'logs' / f'verify-{i}.log').open('x', encoding='utf-8') as log:
            done = subprocess.run(['dotnet', str(out / 'reader/BalanceHarness.dll'), 'tower-balance-evaluate',
                '--definition', str(out / name), '--sources', str(sources), '--output', str(target)],
                cwd=cal.ROOT, stdout=log, stderr=subprocess.STDOUT)
        if done.returncode not in (0, 1, 3): raise ValueError('Normal precision archive validation failed.')
        print(f'Precision block {i+1}/{SAMPLES // BLOCK}: normal archives verified.', flush=True)
        return cal.read(target / 'assessment.json'), cal.sha(target / 'assessment.json')
    with concurrent.futures.ThreadPoolExecutor(max_workers=p['parallelBlocks']) as pool:
        completed = list(pool.map(work, enumerate(p['definitions'])))
    reports = [r for r, _ in completed]; hashes = {str(i): digest for i, (_, digest) in enumerate(completed)}
    check(out)
    result = assess(old['cells'], p['selectedIds'], reports)
    result.update(protocolSha256=cal.sha(out / 'protocol.json'), sourceAssessmentSha256=p['sourceAssessmentSha256'],
        partitionReportHashes=hashes)
    cal.save(out / 'assessment.json', result)
    strongest = max(result['cells'], key=lambda r: r['rate'])
    print(f'Combined {result["assessment"]}: strongest {strongest["wins"]}/{strongest["valid"]}, adjusted upper {strongest["upper"]:.2%}.', flush=True)


def verified(out):
    p, old = check(out); report = cal.read(out / 'assessment.json'); parts = []
    if report['protocolSha256'] != cal.sha(out / 'protocol.json') or report['sourceAssessmentSha256'] != p['sourceAssessmentSha256']:
        raise ValueError('Precision assessment source changed.')
    if set(report['partitionReportHashes']) != {str(i) for i in range(SAMPLES // BLOCK)}:
        raise ValueError('Incomplete precision verification.')
    for i in range(SAMPLES // BLOCK):
        path = out / 'assessments' / str(i) / 'assessment.json'
        if cal.sha(path) != report['partitionReportHashes'][str(i)]: raise ValueError('Verified precision partition changed.')
        parts.append(cal.read(path))
    expected = assess(old['cells'], p['selectedIds'], parts)
    if any(report.get(k) != v for k, v in expected.items()): raise ValueError('Precision arithmetic changed.')
    return p, report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('phase', choices=['freeze', 'confirm', 'verify'])
    parser.add_argument('--out', type=Path, required=True); parser.add_argument('--source', type=Path); parser.add_argument('--reader', type=Path)
    parser.add_argument('--seed', type=int, default=971220); args = parser.parse_args()
    if args.phase == 'freeze':
        if not args.source or not args.reader: parser.error('--source and --reader are required')
        freeze(args.out.resolve(), args.source.resolve(), args.reader.resolve(), args.seed)
    elif args.phase == 'confirm': confirm(args.out.resolve())
    else: print(verified(args.out.resolve())[1]['assessment'])
