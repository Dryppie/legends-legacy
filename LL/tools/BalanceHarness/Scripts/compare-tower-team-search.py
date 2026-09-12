"""Frozen, equal-cost independent/retained search comparison and fresh quality audit.

Uses the existing Tower CLI and retained producing executable. Every phase writes
new evidence. Four search cells share predeclared paired seeds; a separate final
audit tests all shortlists and controls, without selecting from search confirmation.
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
import sys

ROOT = Path(__file__).resolve().parents[4]
LIVE = ROOT / 'LL/src/API/API.LL'
BIN = ROOT / 'LL/tools/BalanceHarness/bin/Release/net10.0'
CATALOGS = ROOT / 'LL/tools/BalanceHarness/Fixtures'
LIBRARY = ROOT / 'TestResults/balance'
NAMES = ['small-independent', 'small-retained', 'large-independent', 'large-retained']


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, ensure_ascii=False)
        stream.write('\n')


def command(out, arguments, label, allowed=(0,)):
    log = out / 'logs' / (label + '.log')
    log.parent.mkdir(parents=True, exist_ok=True)
    with log.open('x', encoding='utf-8') as stream:
        result = subprocess.run(['dotnet', str(out / 'executable/BalanceHarness.dll'), *map(str, arguments)],
                                cwd=ROOT, stdout=stream, stderr=subprocess.STDOUT)
    if result.returncode not in allowed:
        raise RuntimeError(f'{label}: exit {result.returncode}; see {log}')
    return result.returncode


def verify_protocol(out, check_current_script=True):
    protocol = read(out / 'protocol.json')
    if sha(out / 'producing-script.py') != protocol['scriptSha256']:
        raise RuntimeError('Captured producing script changed.')
    if check_current_script and sha(Path(__file__)) != protocol['scriptSha256']:
        raise RuntimeError('Producing campaign script changed; use its captured copy in a matching checkout.')
    for name, digest in protocol['frozenFiles'].items():
        if sha(out / name) != digest:
            raise RuntimeError(f'Frozen input changed: {name}')
    return protocol


def prepare(out, floor, slots, seed):
    if out.exists():
        raise FileExistsError('Choose a new campaign output directory.')
    out.mkdir(parents=True)
    shutil.copytree(BIN, out / 'executable')
    shutil.copytree(CATALOGS, out / 'catalogs')
    command(out, ['tower-team-plan', '--floor', floor, '--slots', slots, '--seed', seed,
                  '--output', out / 'base-plan', '--runs-root', LIBRARY, '--catalogs-root', out / 'catalogs',
                  '--content-root', LIVE], 'base-plan')
    base = read(out / 'base-plan/definition.json')
    if len(base['references']) > 32:
        raise ValueError('This bounded comparison requires at most 32 controls; freeze a separate larger protocol without dropping controls.')
    for name in base['contentHashes']:
        target = out / 'content/Data' / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(LIVE / 'Data' / name, target)
        if sha(target) != base['contentHashes'][name]:
            raise RuntimeError('Content changed during capture.')
    shutil.copy2(LIVE / 'appsettings.json', out / 'content/appsettings.json')
    used = set(base['excludedCombatSeeds']) | set(base['generation']['seeds'])

    def allocate(label, count):
        result = []
        for attempt in range(100000):
            value = struct.unpack('<i', hashlib.sha256(f'competitive-v1/{seed}/{label}/{attempt}'.encode()).digest()[:4])[0]
            if value not in used:
                used.add(value)
                result.append(value)
            if len(result) == count:
                return result
        raise RuntimeError('Fresh schedule allocation exhausted.')

    schedule = {key: allocate(key, count) for key, count in [('discovery', 16), ('selection', 128), ('confirmation', 1000)]}
    schedule['diagnostics'] = []
    audit_seeds = allocate('quality-audit', 1000)
    save(out / 'seed-ledger.json', {'historical': base['excludedCombatSeeds'], 'generation': base['generation']['seeds'],
                                  'pairedSearchSchedules': schedule, 'freshAudit': audit_seeds})
    costs = {}
    for name in NAMES:
        d = copy.deepcopy(base)
        # Scenario IDs also influence battle identity, so all comparisons retain one ID.
        d['id'] = f'competitive-floor-{floor}'
        d['excludedCombatSeeds'] = sorted(set(base['excludedCombatSeeds']) | set(audit_seeds))
        d['generation'].update(candidatesPerArm=128 if name.startswith('small') else 512, maximumAttemptsPerArm=4096)
        d['stages'].update(shortlist=16, diagnosticCandidates=0, replayReserve=200,
                           schedules={c['id']: schedule for c in d['contexts']})
        if len(d['contexts']) != 1:
            raise ValueError('Freeze a separate explicit protocol for multiple equipment contexts.')
        source = out / 'inputs' / (name + '.json')
        save(source, d)
        args = ['tower-boss-improvement-prepare' if name.endswith('retained') else 'tower-boss-discovery-prepare',
                '--definition', source, '--output', out / 'plans' / name, '--content-root', out / 'content']
        if name.endswith('retained'):
            args += ['--references', ','.join(r['id'] for r in d['references'])]
        command(out, args, 'prepare-' + name)
        costs[name] = read(out / 'plans' / name / 'cost.json')
    audit_maximum = (4 * 16 + len(base['references'])) * 1000
    if audit_maximum > 100000:
        raise ValueError('Audit family exceeds the unchanged confirmation combat cap.')
    shutil.copy2(Path(__file__), out / 'producing-script.py')
    frozen = {p.relative_to(out).as_posix(): sha(p) for folder in ('executable', 'catalogs', 'content', 'plans')
              for p in (out / folder).rglob('*') if p.is_file()}
    frozen['seed-ledger.json'] = sha(out / 'seed-ledger.json')
    save(out / 'protocol.json', {
        'status': 'FrozenBeforeCombat', 'version': 'competitive-search-comparison-v1', 'floor': floor, 'slots': slots,
        'budget': base['budget'], 'partySize': base['requiredPartySize'], 'controls': len(base['references']),
        'scope': 'Full allowed pool, hypothetical ownership, fixed neutral identities and declared equipment/training. Practical top-player acquisition unverified.',
        'scriptSha256': sha(Path(__file__)), 'frozenFiles': frozen, 'costs': costs,
        'searchReservation': sum(c['total'] for c in costs.values()), 'auditReservation': audit_maximum,
        'totalReservation': sum(c['total'] for c in costs.values()) + audit_maximum,
        'pairedDesign': 'All four cells share discovery, selection and search-confirmation seeds intentionally. Repeated recipes are not independent samples. Audit seeds are disjoint from all search and historical seeds.',
        'auditSelection': 'Exact union of all four discovery shortlists and all original controls. Selection-stage fitness freezes one primary per cell and one nominee per method/restart from that arm\'s shortlisted recipes. Search-confirmation outcomes never select audit members.',
        'qualityMarginPercentagePoints': 5,
        'qualityIntervals': 'Approximate simultaneous Bonferroni-Wilson differences: two discordant probabilities for every unordered pair in the frozen audit family, total alpha 0.05.',
        'qualityRules': [
            'Recovery: both large-cell selection-frozen primaries have upper performance gap <=5 pp against every original control.',
            'Reliability: at least two of three large-cell constructive-joint and retained-joint restart nominees separately have upper gap <=5 pp against every audited recipe.',
            'Plateau: for each mode, the upper gain of its large-cell frozen primary over its small-cell frozen primary is <=5 pp.',
            'All criteria must pass for a bounded-search-quality finding. Missing evidence is Invalid; unresolved width is Inconclusive; a supported gap >5 pp fails. No global-optimum claim or automatic floor acceptance.'
        ],
        'balance': 'Existing per-family 10–50% policy; earlier above-ceiling candidates remain unresolved until separately confirmed. New stronger teams trigger a separate calibration, never a weaker finalist.'
    })
    print(json.dumps({'status': 'FrozenBeforeCombat', 'costs': costs, 'auditReservation': audit_maximum}), flush=True)


def search(out):
    verify_protocol(out)

    def run(name):
        target = out / 'studies' / name
        code = command(out, ['tower-boss-study', '--definition', out / 'plans' / name / 'definition.json',
                            '--content-root', out / 'content', '--output', target,
                            '--runs-root', out / ('retention-' + name)], name, (0, 1, 3))
        report = read(target / 'study.json')
        if report['status'] != 'Complete':
            raise RuntimeError(f'{name}: incomplete search')
        command(out, ['tower-boss-study-verify', '--run', target], 'verify-' + name)
        print(f'{name}: Complete and reconstructed; {sum(report["accounting"]["completed"].values())} combats', flush=True)
        return {'name': name, 'exitCode': code, 'reportSha256': sha(target / 'study.json'), 'completed': report['accounting']['completed']}

    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        rows = list(pool.map(run, NAMES))
    save(out / 'search-completion.json', rows)


def recipe_key(scenario):
    party = copy.deepcopy(scenario['party'])
    for member in party:
        build = member['build']
        build['equipment'].sort(key=lambda e: e['slot'])
        build['identityEssenceIds'] = build.get('identityEssenceIds') or build['essenceIds']
    return hashlib.sha256(json.dumps(party, sort_keys=True, ensure_ascii=False).encode()).hexdigest()


def freeze_audit(out):
    verify_protocol(out)
    completed = read(out / 'search-completion.json')
    ledger = read(out / 'seed-ledger.json')
    entries = {}; nominees = {}; cohort = None; base = None

    def add(scenario, source):
        scenario = copy.deepcopy(scenario); scenario['seeds'] = ledger['freshAudit']
        key = recipe_key(scenario)
        if key not in entries:
            entries[key] = {'id': 'audit-' + key[:24], 'scenario': scenario, 'sources': []}
        entries[key]['sources'].append(source)
        return entries[key]['id']

    def ranking(row):
        f = row['fitness']
        return (-f['worstContextWinRate'], f['guardianHealth'], -f['survival'], f['victoryDuration'], row['id'])

    for result in completed:
        name = result['name']; path = out / 'studies' / name
        if sha(path / 'study.json') != result['reportSha256']:
            raise RuntimeError('Completed study changed before audit freeze.')
        d = read(path / 'definition.json'); base = d
        report = read(path / 'study.json'); cohort = report['confirmation']['definition']['cohorts'][0]
        shortlist = read(path / 'discovery-shortlist.json'); mapping = {}
        for party in shortlist:
            scenario = {'schemaVersion': 1, 'id': d['id'], 'floorNumber': d['budget']['priorityFloor'], 'startsAt': d['startsAt'],
                        'preparationState': 'uncleared-no-contributions', 'assumptions': ['Frozen competitive search audit'],
                        'seeds': [], 'party': copy.deepcopy(d['contexts'][0]['characterTemplates'])}
            for member in scenario['party']:
                member['build']['essenceIds'] = party['builds'][str(member['partySlot'])]
                member['build']['identityEssenceIds'] = [f'neutral-identity-slot-{i}' for i in range(1, d['budget']['essenceSlots'] + 1)]
            mapping[party['id']] = add(scenario, {'study': name, 'partyId': party['id'], 'role': 'shortlist'})
        rows = sorted(report['selection'], key=ranking)
        arms = []
        for arm in report['discovery']['arms']:
            ids = {row['id'] for row in arm['evaluations']}
            candidates = [row for row in rows if row['id'] in ids]
            if not candidates:
                raise RuntimeError('A restart has no audited shortlist representative.')
            arms.append({'method': arm['method'], 'seed': arm['seed'], 'cell': mapping[candidates[0]['id']]})
        nominees[name] = {'primary': mapping[rows[0]['id']], 'restarts': arms}
        for reference in d['references']:
            scenario = copy.deepcopy(reference['scenario']); scenario['id'] = d['id']
            add(scenario, {'study': name, 'role': 'control', 'referenceId': reference['id']})
    entries = sorted(entries.values(), key=lambda e: e['id'])
    exclusions = set(ledger['historical']) | set(ledger['generation'])
    for seeds in ledger['pairedSearchSchedules'].values():
        exclusions.update(seeds)
    cells = [{'id': e['id'], 'cohortId': cohort['id'], 'role': 'reference' if any(s['role'] == 'control' for s in e['sources']) else 'generated',
              'scenario': e['scenario'], 'minimumSamples': 1000} for e in entries]
    definition = {'schemaVersion': 1, 'id': 'competitive-quality-audit', 'intervalPolicy': 'bonferroni-wilson-95-v1',
                  'contentHashes': base['contentHashes'], 'settingsHash': base['settingsHash'], 'executionHash': base['executionHash'],
                  'cohorts': [cohort], 'cells': cells, 'excludedCombatSeeds': sorted(exclusions), 'maximumBattles': len(cells) * 1000}
    if definition['maximumBattles'] > read(out / 'protocol.json')['auditReservation']:
        raise RuntimeError('Frozen audit family exceeds the predeclared reservation.')
    save(out / 'audit-definition.json', definition)
    save(out / 'audit-portfolio.json', entries)
    save(out / 'audit-freeze.json', {'status': 'FrozenBeforeAuditCombat', 'definitionSha256': sha(out / 'audit-definition.json'),
                                   'portfolioSha256': sha(out / 'audit-portfolio.json'), 'nominees': nominees})
    print(f'Audit frozen: {len(entries)} exact parties, {len(cells) * 1000} fresh paired combats', flush=True)


def audit(out):
    verify_protocol(out)
    frozen = read(out / 'audit-freeze.json')
    if sha(out / 'audit-definition.json') != frozen['definitionSha256'] or sha(out / 'audit-portfolio.json') != frozen['portfolioSha256']:
        raise RuntimeError('Audit changed after freeze.')
    entries = read(out / 'audit-portfolio.json')
    # Validate the entire contract before spending any combat. Missing evidence
    # is expected here; a malformed definition must fail before the first fight.
    save(out / 'preflight-sources.json', [])
    command(out, ['tower-balance-evaluate', '--definition', out / 'audit-definition.json',
                  '--sources', out / 'preflight-sources.json', '--output', out / 'audit-preflight'],
            'audit-preflight', (2,))
    preflight = read(out / 'audit-preflight/assessment.json')
    if preflight['issues'] or any(c['issues'] != ['Required confirmation evidence is missing.'] for c in preflight['cells']):
        raise RuntimeError('Audit preflight failed for reasons other than deliberately missing evidence.')

    def run(entry):
        scenario = out / 'audit-scenarios' / (entry['id'] + '.json'); save(scenario, entry['scenario'])
        target = out / 'audit-runs' / entry['id']
        command(out, ['tower', '--scenario', scenario, '--content-root', out / 'content', '--output', target], entry['id'])
        score = read(target / 'scorecard.json')
        if score['status'] != 'Complete' or score['valid'] != 1000:
            raise RuntimeError('Incomplete audit party.')
        print(f'Audit {entry["id"]}: {score["wins"]}/1000', flush=True)
        return {'cellId': entry['id'], 'runDirectory': target.relative_to(out).as_posix()}

    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        sources = list(pool.map(run, entries))
    save(out / 'audit-sources.json', sources)
    command(out, ['tower-balance-evaluate', '--definition', out / 'audit-definition.json', '--sources', out / 'audit-sources.json',
                  '--output', out / 'audit-assessment'], 'audit-assessment', (0, 1, 3))


def reissue_audit(source, out, seed, extra_exclusions=None):
    """Preserve an invalid audit and reissue its exact membership on unused seeds."""
    original = verify_protocol(source, check_current_script=False)
    frozen = read(source / 'audit-freeze.json')
    for file, field in [('audit-definition.json', 'definitionSha256'), ('audit-portfolio.json', 'portfolioSha256')]:
        if sha(source / file) != frozen[field]:
            raise RuntimeError('Original audit input changed.')
    if out.exists():
        raise FileExistsError('Choose a new audit directory.')
    out.mkdir(parents=True)
    for folder in ('executable', 'content'):
        shutil.copytree(source / folder, out / folder)
    ledger = read(source / 'seed-ledger.json')
    used = set(ledger['historical']) | set(ledger['generation']) | set(ledger['freshAudit'])
    for schedule in ledger['pairedSearchSchedules'].values():
        used.update(schedule)
    extra_hash = None
    if extra_exclusions:
        extra_hash = sha(extra_exclusions)
        def integers(value):
            if isinstance(value, dict):
                for child in value.values(): yield from integers(child)
            elif isinstance(value, list):
                for child in value: yield from integers(child)
            elif type(value) is int: yield value
        used.update(integers(read(extra_exclusions)))
    historical = sorted(used); fresh = []
    for attempt in range(100000):
        value = struct.unpack('<i', hashlib.sha256(f'audit-reissue-v1/{seed}/{attempt}'.encode()).digest()[:4])[0]
        if value not in used:
            used.add(value); fresh.append(value)
        if len(fresh) == 1000: break
    if len(fresh) != 1000: raise RuntimeError('Fresh allocation exhausted.')
    entries = read(source / 'audit-portfolio.json')
    definition = read(source / 'audit-definition.json')
    for scenario in [e['scenario'] for e in entries] + [c['scenario'] for c in definition['cells']]:
        scenario['seeds'] = fresh
        if 'assumptions' not in scenario:
            scenario['assumptions'] = scenario.pop('notes')
    definition['id'] = 'competitive-quality-audit-reissued'
    definition['excludedCombatSeeds'] = historical
    save(out / 'audit-definition.json', definition)
    save(out / 'audit-portfolio.json', entries)
    save(out / 'seed-ledger.json', {'historical': historical, 'generation': [], 'pairedSearchSchedules': {}, 'freshAudit': fresh})
    save(out / 'audit-freeze.json', {**frozen, 'definitionSha256': sha(out / 'audit-definition.json'),
                                   'portfolioSha256': sha(out / 'audit-portfolio.json')})
    shutil.copy2(Path(__file__), out / 'producing-script.py')
    files = {p.relative_to(out).as_posix(): sha(p) for folder in ('executable', 'content')
             for p in (out / folder).rglob('*') if p.is_file()}
    files['seed-ledger.json'] = sha(out / 'seed-ledger.json')
    save(out / 'protocol.json', {**original, 'version': 'competitive-audit-reissue-v1',
        'scriptSha256': sha(Path(__file__)), 'frozenFiles': files, 'costs': {}, 'searchReservation': 0,
        'auditReservation': len(entries) * 1000, 'totalReservation': len(entries) * 1000,
        'source': str(source), 'sourceProtocolSha256': sha(source / 'protocol.json'),
        'sourceAuditFreezeSha256': sha(source / 'audit-freeze.json'), 'extraExclusionsSha256': extra_hash,
        'reason': 'Original generated audit scenarios used notes instead of required assumptions. Preserve original as invalid. Same recipes, nominees, quality rules and content; fix descriptive metadata only and use a completely new schedule. No membership selection from original audit outcomes.'})
    print(f'Reissued audit frozen: {len(entries)} unchanged parties, {len(entries) * 1000} new combats', flush=True)


def wilson(wins, samples, z):
    rate = wins / samples; divisor = 1 + z * z / samples
    center = (rate + z * z / (2 * samples)) / divisor
    margin = z * math.sqrt(rate * (1 - rate) / samples + z * z / (4 * samples * samples)) / divisor
    return max(0, center - margin), min(1, center + margin)


def validate_audit_evidence(entries, observations, assessment, seeds):
    ids = {e['id'] for e in entries}
    if (len(ids) != len(entries) or len(observations) != len(ids)
            or {o['cellId'] for o in observations} != ids
            or len(assessment['cells']) != len(ids) or {c['id'] for c in assessment['cells']} != ids
            or assessment['assessment'] == 'Invalid' or assessment['issues']
            or any(c['issues'] for c in assessment['cells'])):
        raise RuntimeError('Invalid or incompletely verified audit evidence family.')
    by_id = {c['id']: c for c in assessment['cells']}
    for o in observations:
        c = by_id[o['cellId']]
        if (o['status'] != 'Complete' or [t['seed'] for t in o['trials']] != seeds
                or any(t['outcome'] not in ('Victory', 'Defeat', 'Draw') for t in o['trials'])
                or c['valid'] != len(seeds) or c['wins'] != sum(t['outcome'] == 'Victory' for t in o['trials'])):
            raise RuntimeError('Audit schedule or outcomes differ from verified evidence.')


def report(out):
    protocol = verify_protocol(out); frozen = read(out / 'audit-freeze.json')
    entries = read(out / 'audit-portfolio.json'); observations = read(out / 'audit-assessment/evidence.json')
    seeds = read(out / 'seed-ledger.json')['freshAudit']
    assessment = read(out / 'audit-assessment/assessment.json')
    validate_audit_evidence(entries, observations, assessment, seeds)
    outcomes = {o['cellId']: [t['outcome'] == 'Victory' for t in o['trials']] for o in observations}
    m = len(entries); pairs = max(1, m * (m - 1) // 2)
    z = statistics.NormalDist().inv_cdf(1 - .05 / (4 * pairs))

    def difference(left, right):
        if left == right:
            return {'change': 0, 'lower': 0, 'upper': 0}
        paired = list(zip(outcomes[left], outcomes[right]))
        gains = sum(a and not b for a, b in paired); losses = sum(b and not a for a, b in paired)
        gl, gu = wilson(gains, len(paired), z); ll, lu = wilson(losses, len(paired), z)
        return {'change': 100 * (gains - losses) / len(paired), 'lower': 100 * (gl - lu), 'upper': 100 * (gu - ll)}

    controls = [e['id'] for e in entries if any(s['role'] == 'control' for s in e['sources'])]
    rows = []; margin = protocol['qualityMarginPercentagePoints']
    for name in NAMES:
        nominee = frozen['nominees'][name]; primary = nominee['primary']
        gaps = {control: difference(control, primary) for control in controls}
        restart_gaps = [{'method': arm['method'], 'seed': arm['seed'], 'cell': arm['cell'],
                         'maximumUpperGap': max(difference(other, arm['cell'])['upper'] for other in outcomes),
                         'maximumLowerGap': max(difference(other, arm['cell'])['lower'] for other in outcomes)} for arm in nominee['restarts']]
        rows.append({'name': name, 'primary': primary, 'wins': sum(outcomes[primary]), 'samples': len(seeds),
                     'controlGaps': gaps, 'recoverySupported': all(g['upper'] <= margin for g in gaps.values()),
                     'restartGaps': restart_gaps})
    plateau = {mode: difference(frozen['nominees']['large-' + mode]['primary'], frozen['nominees']['small-' + mode]['primary'])
               for mode in ('independent', 'retained')}
    large = [r for r in rows if r['name'].startswith('large')]
    reliable = {r['name']: sum(a['maximumUpperGap'] <= margin for a in r['restartGaps'] if a['method'] in ('constructive-joint', 'retained-joint')) >= 2 for r in large}
    passed = all(r['recoverySupported'] for r in large) and all(reliable.values()) and all(p['upper'] <= margin for p in plateau.values())
    failed = any(g['lower'] > margin for r in large for g in r['controlGaps'].values()) or any(p['lower'] > margin for p in plateau.values())
    failed |= any(sum(a['maximumLowerGap'] > margin for a in r['restartGaps'] if a['method'] in ('constructive-joint', 'retained-joint')) >= 2 for r in large)
    summary = {'status': 'Pass' if passed else 'Fail' if failed else 'Inconclusive', 'scope': protocol['scope'],
               'simultaneousPairs': pairs, 'z': z, 'marginPercentagePoints': margin, 'cells': rows, 'plateau': plateau,
               'restartReliability': reliable, 'bestObserved': max(outcomes, key=lambda k: (sum(outcomes[k]), k)),
               'auditWinCounts': {k: sum(v) for k, v in outcomes.items()}, 'auditFamilyBalance': assessment['assessment'],
               'note': 'Search quality and scoped balance are separate. This audit does not resolve earlier above-ceiling candidates outside its frozen family or establish global optimality.'}
    save(out / 'quality-report.json', summary)
    print(json.dumps(summary, indent=2), flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('phase', choices=['prepare', 'search', 'freeze-audit', 'audit', 'report', 'reissue-audit'])
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--floor', type=int, default=5); parser.add_argument('--slots', type=int, default=5)
    parser.add_argument('--seed', type=int, default=294711)
    parser.add_argument('--source', type=Path); parser.add_argument('--extra-exclusions', type=Path)
    args = parser.parse_args(); out = args.out.resolve()
    if args.phase == 'prepare':
        prepare(out, args.floor, args.slots, args.seed)
    elif args.phase == 'reissue-audit':
        if not args.source: parser.error('--source is required')
        reissue_audit(args.source.resolve(), out, args.seed, args.extra_exclusions)
    else:
        {'search': search, 'freeze-audit': freeze_audit, 'audit': audit, 'report': report}[args.phase](out)
