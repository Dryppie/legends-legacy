"""Read-only diagnosis of the closed second adaptive pilot; never executes combat.

Authenticates every consumed file against the published external pins. Recounts
saved training outcomes and stage decisions, not all compressed battle reports.
Missing held-out outcomes stay null. This is development evidence, not promotion.
"""
import argparse
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
RUN = ROOT/'TestResults/balance/tower-adaptive-racing-pilot-02-20260923'
RUN_PIN = 'f2327de7f9382f3a3ac3213a25cdb590e81e676ddd6e622fb91e3615ea8dd95a'
CLOSEOUT = ROOT/'TestResults/adaptive-racing-pilot-02-publication-verification-20260923'
CLOSEOUT_PIN = '3e11948b3892f4d813a6b992d12a242c8c10342df34c6ee91996604045d2d798'
OUT = ROOT/'TestResults/adaptive-racing-stage-review-20260923'
PRIMARY = '399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b'
BENCHMARK = '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c'
REFERENCES = {PRIMARY, BENCHMARK, '8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50'}
ROLES = ['wave-1-screen', 'wave-1-continuation', 'wave-2-screen', 'wave-2-continuation', 'selection']


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


class Evidence:
    def __init__(self, root, pin):
        self.root, self.consumed = root, {'files.json': pin}
        raw = (root/'files.json').read_bytes()
        require(sha(raw) == pin, 'Changed external manifest pin')
        self.manifest = json.loads(raw)

    def read(self, name):
        require(name in self.manifest and not Path(name).is_absolute()
                and '..' not in Path(name).parts, 'Unbound evidence path')
        raw = (self.root/name).read_bytes()
        require(sha(raw) == self.manifest[name], 'Changed consumed evidence: '+name)
        self.consumed[name] = self.manifest[name]
        return json.loads(raw)

    def recheck(self):
        for name, pin in self.consumed.items():
            require(sha((self.root/name).read_bytes()) == pin, 'Evidence changed during review: '+name)


def same(a, b):
    if isinstance(b, float):
        return type(a) in (int, float) and math.isfinite(a) and math.isclose(a, b, rel_tol=1e-12, abs_tol=1e-12)
    if isinstance(b, dict):
        return isinstance(a, dict) and a.keys() == b.keys() and all(same(a[k], v) for k, v in b.items())
    if isinstance(b, list):
        return isinstance(a, list) and len(a) == len(b) and all(same(x, y) for x, y in zip(a, b))
    return type(a) is type(b) and a == b


def rank(row):
    f = row['fitness']
    return (-f['worstContextWinRate'], f['guardianHealth'], -f['survival'], f['victoryDuration'], row['id'])


def select(rows, order, indices=None, *, benchmark_ties=False):
    require(len(order) == len(set(order)) == len(rows) and set(order) == set(rows)
            and REFERENCES.issubset(rows), 'Incomplete selection membership')
    n = len(rows[order[0]]['clears'])
    require(n > 0 and all(len(r['clears']) == n and all(type(v) is bool for v in r['clears']) for r in rows.values()),
            'Incomplete or nonboolean selection outcomes')
    indices = list(range(n)) if indices is None else list(indices)
    require(indices and len(indices) == len(set(indices)) and all(0 <= i < n for i in indices), 'Invalid selection subset')
    wins = {pid: sum(rows[pid]['clears'][i] for i in indices) for pid in order}
    best = max(wins.values())
    if best > 0:
        if benchmark_ties and wins[BENCHMARK] == best:
            return BENCHMARK
        return PRIMARY if wins[PRIMARY] == best else next(pid for pid in order if wins[pid] == best)
    # Baseline checkpoints retain only aggregate health. Do not invent subset health.
    if any(rows[pid].get('health') is None for pid in order):
        return None
    return min(order, key=lambda pid: (sum(rows[pid]['health'][i] for i in indices), order.index(pid), pid))


def selection_summary(rows, order, selected, *, benchmark_ties=False):
    require(select(rows, order, benchmark_ties=benchmark_ties) == selected, 'Changed selected output')
    n = len(rows[selected]['clears'])
    wins = {pid: sum(r['clears']) for pid, r in rows.items()}
    leaders = [pid for pid in order if wins[pid] == max(wins.values())]
    halves = [select(rows, order, indices, benchmark_ties=benchmark_ties)
              for indices in (range(0, n//2), range(n//2, n))]
    leave = [select(rows, order, [j for j in range(n) if j != i], benchmark_ties=benchmark_ties) for i in range(n)]
    return dict(selected=selected, reference=selected in REFERENCES, samples=n, wins=wins,
        leaders=leaders, reason='BenchmarkPositiveTie' if benchmark_ties and len(leaders) > 1 and selected == BENCHMARK and wins[selected] > 0
        else 'PrimaryPositiveTie' if len(leaders) > 1 and selected == PRIMARY and wins[selected] > 0
        else 'FrozenNomineeOrder' if len(leaders) > 1 and wins[selected] > 0 else 'UniqueMaximum' if wins[selected] > 0 else 'ZeroWinHealth',
        marginOverBestReference=wins[selected]-max(wins[pid] for pid in REFERENCES),
        marginOverBenchmark=wins[selected]-wins[BENCHMARK],
        nonprimaryReferenceTie=selected not in REFERENCES and bool(REFERENCES.intersection(leaders)),
        halfSelections=halves, halvesDisagree=None if None in halves else halves[0] != halves[1],
        leaveOneOutChanges=sum(x is not None and x != selected for x in leave),
        unassessableSubsets=sum(x is None for x in halves+leave))


def score(pid, outcomes):
    require(outcomes and all(o['outcome'] in ('Victory', 'Defeat', 'Draw') for o in outcomes), 'Invalid saved outcome')
    won = [o for o in outcomes if o['outcome'] == 'Victory']
    return dict(id=pid, samples=len(outcomes), wins=len(won), draws=sum(o['outcome'] == 'Draw' for o in outcomes),
        fitness=dict(worstContextWinRate=len(won)/len(outcomes),
            guardianHealth=sum(o['guardianHealth'] for o in outcomes)/len(outcomes),
            survival=sum(o['survival'] for o in outcomes)/len(outcomes),
            victoryDuration=sum(o['durationSeconds'] for o in won)/len(won) if won else sys.float_info.max))


def panel_rows(panel, seeds, index, before):
    f, observations = panel['freeze'], panel['observations']
    ids = [p['id'] for p in f['parties']]
    require(panel['complete'] and f['role'] == ROLES[index] and f['index'] == index
            and f['seeds'] == seeds and len(seeds) == len(set(seeds)) == (40 if index == 4 else 8)
            and len(ids) == len(set(ids)) and REFERENCES.issubset(ids)
            and len(observations) == f['plannedEvaluations'] == len(ids)*len(seeds)
            and f['evaluationsBefore'] == before, 'Incomplete or changed panel')
    rows = {pid: [] for pid in ids}
    for offset, (pid, seed) in enumerate((p, s) for p in ids for s in seeds):
        request, outcome = observations[offset]['request'], observations[offset]['outcome']
        require(request['partyId'] == pid and request['seed'] == outcome['seed'] == seed
                and request['ordinal'] == before+offset+1 and request['role'] == f['role'], 'Unpaired or reordered observation')
        rows[pid].append(outcome)
    require(same(panel['scores'], [score(pid, values) for pid, values in rows.items()]), 'Changed saved panel score')
    return rows


def distance(a, b):
    require(a['builds'].keys() == b['builds'].keys(), 'Mismatched party owners')
    return sum(len(set(a['builds'][owner])-set(b['builds'][owner])) for owner in a['builds'])


def adaptive(report, plan, heldout, selected, *, report_version='tower-adaptive-beam-racing-v1', benchmark_ties=False):
    e = report['evaluation']
    require(report['version'] == e['version'] == report_version
            and plan['version'] == 'tower-adaptive-beam-racing-v1'
            and report['planHash'] == e['planHash'] and e['status'] == 'Complete'
            and e['plannedEvaluations'] == e['chargedEvaluations'] == plan['maximumEvaluations'] == 528
            and len(e['panels']) == len(plan['panels']) == 5 and len(e['decisions']) == len(report['batches']) == 2,
            'Changed adaptive contract')
    seed_list = [s for p in plan['panels'] for s in p['seeds']]
    require(len(seed_list) == len(set(seed_list)) == 72 and plan['rootSeed'] not in seed_list, 'Reused training seeds')
    parties = {s['party']['id']: s['party'] for s in plan['scope']['starts']}
    require(set(parties) == REFERENCES, 'Changed references')
    beam, rows, records, diagnostics, proposals = [], [], [], [], []
    before = 0
    for i, panel in enumerate(e['panels']):
        require(plan['panels'][i]['role'] == ROLES[i], 'Changed panel role')
        if i in (0, 2):
            batch = report['batches'][i//2]
            require(batch['wave'] == i//2+1 and batch['afterEvaluations'] == before and batch['beamIds'] == beam
                    and len(batch['seenBefore']) == len(parties) and set(batch['seenBefore']) == set(parties)
                    and len(batch['candidates']) == (9 if i == 0 else 8), 'Changed batch or feedback')
            accepted = [p for p in batch['proposals'] if p['rejection'] is None]
            require([p['party'] for p in accepted] == batch['candidates'] and len(batch['proposals']) <= 128, 'Changed accepted proposals')
            for attempt, p in enumerate(batch['proposals'], 1):
                require(p['attempt'] == attempt and all(pid in parties for pid in p['parents']), 'Unresolved parent or attempt order')
                proposals.append(dict(wave=i//2+1, **{k: p[k] for k in ('requestedOperator', 'effectiveOperator', 'parentSource', 'fallback', 'rejection', 'constructionChecks')}))
            for p in accepted:
                pid = p['party']['id']
                require(pid not in parties, 'Duplicate accepted recipe')
                parties[pid] = p['party']
                records.append(dict(id=pid, wave=i//2+1, operator=p['effectiveOperator'], requestedOperator=p['requestedOperator'],
                    parentSource=p['parentSource'], parents=p['parents'], replacementDistance=p['replacementDistance'],
                    changedOwners=p['changedOwners'], initialWins=None, stages={}, nominated=False, selected=pid == selected,
                    heldoutWins=heldout.get(pid), heldoutSamples=256 if pid in heldout else None))
            expected = list(REFERENCES) + beam + [p['id'] for p in batch['candidates']]
        elif i in (1, 3):
            expected = list(REFERENCES) + e['decisions'][i//2]['survivorIds']
        else:
            nominated = REFERENCES | set(beam[:2])
            expected = [s['id'] for s in sorted(e['decisions'][1]['commonScores'], key=rank) if s['id'] in nominated]
            require(e['nominees'] == expected, 'Changed nomination')
        measured = panel_rows(panel, plan['panels'][i]['seeds'], i, before)
        require(set(measured) == set(expected) and (i < 4 or list(measured) == expected), 'Changed panel membership')
        rows.append(measured)
        for record in records:
            pid = record['id']
            if pid in measured:
                wins = sum(o['outcome'] == 'Victory' for o in measured[pid])
                record['stages'][ROLES[i]] = dict(wins=wins, samples=len(measured[pid]), benchmarkWins=sum(o['outcome'] == 'Victory' for o in measured[BENCHMARK]))
                if record['initialWins'] is None:
                    record['initialWins'] = wins
                record['nominated'] |= i == 4
        before += len(panel['observations'])
        if i in (0, 2):
            decision = e['decisions'][i//2]
            ranked = sorted([s for s in panel['scores'] if s['id'] not in REFERENCES], key=rank)
            elite = [s['id'] for s in ranked[:3]]
            cutoff = ranked[2]['wins']-1
            eligible = [s for s in ranked[3:] if s['wins'] >= cutoff]
            def nearest(pid):
                return min(distance(parties[pid], parties[x]) for x in elite)
            diverse = max(eligible, key=lambda s: nearest(s['id'])) if eligible else ranked[3]
            survivors = elite+[diverse['id']]
            require(decision['eliteIds'] == elite and decision['survivorIds'] == survivors and decision['diversityId'] == diverse['id']
                    and decision['competitiveCutoffWins'] == cutoff and decision['diversityFallback'] == (not eligible)
                    and decision['diversityDistance'] == nearest(diverse['id'])
                    and decision['prunedIds'] == [s['id'] for s in ranked if s['id'] not in survivors], 'Changed racing decision')
            diagnostics.append(dict(wave=i//2+1, screenChallengers=len(ranked), cutoffWins=ranked[2]['wins'],
                cutoffTieSize=sum(s['wins'] == ranked[2]['wins'] for s in ranked),
                diversityRank=next(j+1 for j, s in enumerate(ranked) if s['id'] == diverse['id']),
                diversityId=diverse['id'], pruned=len(ranked)-4,
                survivors=survivors, diversityNominated=diverse['id'] in e['nominees'], diversitySelected=diverse['id'] == selected))
        elif i in (1, 3):
            common = [score(pid, rows[i-1][pid]+measured[pid]) for pid in measured]
            require(same(e['decisions'][i//2]['commonScores'], common), 'Changed common-rung score')
            beam = [s['id'] for s in sorted(common, key=rank) if s['id'] not in REFERENCES]
            require(e['decisions'][i//2]['beamIds'] == beam, 'Changed common-rung beam')
    selection_rows = {pid: dict(clears=[o['outcome'] == 'Victory' for o in obs], health=[o['guardianHealth'] for o in obs]) for pid, obs in rows[-1].items()}
    require(before == 528 and selected == e['rawSelectedId'], 'Changed adaptive endpoint')
    return dict(records=records, proposals=proposals, waves=diagnostics,
        selection=selection_summary(selection_rows, e['nominees'], selected, benchmark_ties=benchmark_ties),
        referenceFights=sum(len(panel[pid]) for panel in rows for pid in REFERENCES), challengerFights=sum(len(obs) for panel in rows for pid, obs in panel.items() if pid not in REFERENCES))


def baseline(search, heldout, selected):
    discovery = search['discovery']
    require(discovery['status'] == 'Complete' and len(discovery['arms']) == 1, 'Incomplete baseline')
    arm = discovery['arms'][0]
    require(len(arm['evaluations']) == 46 and len(search['selection']) == 5, 'Changed baseline allocation')
    def rows(measurements, samples):
        require(len({r['id'] for r in measurements}) == len(measurements), 'Duplicate measurement')
        result = {}
        for row in measurements:
            require(len(row['cells']) == 1, 'Changed context')
            cell = row['cells'][0]
            require(len(cell['clears']) == samples and all(type(v) is bool for v in cell['clears'])
                    and math.isclose(row['fitness']['worstContextWinRate'], sum(cell['clears'])/samples), 'Invalid baseline outcomes')
            result[row['id']] = dict(clears=cell['clears'], health=None)
        require(REFERENCES.issubset(result), 'Missing reference')
        return result
    observed = rows(arm['evaluations'], 8)
    selection = rows(search['selection'], 32)
    ranked = sorted(arm['evaluations'], key=rank)
    kept = REFERENCES | set([r['id'] for r in ranked if r['id'] not in REFERENCES][:2])
    order = [r['id'] for r in ranked if r['id'] in kept]
    require([p['id'] for p in discovery['discoveryShortlist']] == order == list(selection)
            and search['output']['finalist']['party']['id'] == selected, 'Changed baseline shortlist or output')
    accepted = [p for p in arm['proposals'] if p['result'] == 'evaluated']
    require(len(accepted) == 46 and {p['party']['id'] for p in accepted} == set(observed), 'Incomplete baseline population')
    records = []
    for p in accepted:
        pid = p['party']['id']
        if pid in REFERENCES:
            continue
        records.append(dict(id=pid, operator=p['provenance']['operator'], initialWins=sum(observed[pid]['clears']),
            nominated=pid in kept, selected=pid == selected, selectionWins=sum(selection[pid]['clears']) if pid in selection else None,
            heldoutWins=heldout.get(pid), heldoutSamples=256 if pid in heldout else None))
    return dict(records=records, selection=selection_summary(selection, order, selected), referenceFights=120, challengerFights=408)


def summarize(arms):
    records = [r for a in arms for r in a['records']]
    return dict(searches=len(arms), generated=len(records), distinctRecipes=len({r['id'] for r in records}),
        nominees=sum(r['nominated'] for r in records), selected=sum(r['selected'] for r in records),
        heldoutMeasured=sum(r['heldoutWins'] is not None for r in records), heldoutUnknown=sum(r['heldoutWins'] is None for r in records),
        referenceFights=sum(a['referenceFights'] for a in arms), challengerFights=sum(a['challengerFights'] for a in arms),
        nonprimaryReferenceTies=sum(a['selection']['nonprimaryReferenceTie'] for a in arms),
        halfPanelDisagreements=sum(a['selection']['halvesDisagree'] is True for a in arms),
        rootsWithLeaveOneOutChange=sum(a['selection']['leaveOneOutChanges'] > 0 for a in arms),
        leaveOneOutChanges=sum(a['selection']['leaveOneOutChanges'] for a in arms),
        unassessableSubsets=sum(a['selection']['unassessableSubsets'] for a in arms),
        operators={op: dict(accepted=len(group), zeroInitialWins=sum(r['initialWins'] == 0 for r in group),
            initialWins=sum(r['initialWins'] for r in group), initialTrials=8*len(group),
            nominated=sum(r['nominated'] for r in group), selected=sum(r['selected'] for r in group),
            heldoutMeasured=sum(r['heldoutWins'] is not None for r in group))
            for op in sorted({r['operator'] for r in records})
            for group in [[r for r in records if r['operator'] == op]]})


def run(output):
    started = time.monotonic()
    require(not output.exists() and RUN.resolve() not in output.resolve().parents and CLOSEOUT.resolve() not in output.resolve().parents,
            'Use a new external review directory')
    scientific, close = Evidence(RUN, RUN_PIN), Evidence(CLOSEOUT, CLOSEOUT_PIN)
    result, receipt = scientific.read('result.json'), close.read('closeout.json')
    require(receipt['status'] == 'AdaptiveRacingPilotExecutionVerified' and receipt['archiveManifestSha256'] == RUN_PIN
            and receipt['result'] == result == scientific.read('independent-audit.json')['result']
            and receipt['nativeAudit'] == receipt['independentAudit'] == 'Passed' and result['decision'] == 'AbandonThisConfiguration',
            'Source is not the closed audited pilot')
    require(scientific.read('completion.json')['status'] == 'Complete' and scientific.read('native-receipt.json')['status'] == 'Verified', 'Incomplete source')
    pairs = []
    for p, q in zip(result['pairs'], result['pilot']['roots']):
        require(p['restart'] == q['restart'] == len(pairs)+1, 'Reordered endpoint roots')
        j = p['restart']
        heldout = {}
        for pid, wins in [(p['baselineParty'], p['baselineWins']), (p['candidateParty'], p['candidateWins']), (BENCHMARK, q['benchmarkWins'])]:
            require(pid not in heldout or heldout[pid] == wins, 'Shared held-out recipe disagrees')
            heldout[pid] = wins
        a = baseline(scientific.read(f'study/pair-{j:02d}-baseline.json'), heldout, p['baselineParty'])
        b = adaptive(scientific.read(f'study/pair-{j:02d}-candidate-adaptive.json'), scientific.read(f'study/pair-{j:02d}-candidate-plan.json'), heldout, p['candidateParty'])
        pairs.append(dict(root=j, baseline=a, adaptive=b, heldout=dict(baselineWins=p['baselineWins'], adaptiveWins=p['candidateWins'], benchmarkWins=q['benchmarkWins']),
            novelSelection=b['selection']['selected'] not in REFERENCES))
        require(time.monotonic()-started < 180, 'Read-only review exceeded 180 seconds')
    require(len(pairs) == 12, 'Incomplete roots')
    summaries = {side: summarize([p[side] for p in pairs]) for side in ('baseline', 'adaptive')}
    proposals = [v for p in pairs for v in p['adaptive']['proposals']]
    waves = [v for p in pairs for v in p['adaptive']['waves']]
    summary = dict(arms=summaries, proposals=len(proposals), rejectionCounts=dict(Counter(p['rejection'] or 'accepted' for p in proposals)),
        requestedOperators=dict(Counter(p['requestedOperator'] for p in proposals)),
        fallbackCounts=dict(Counter(p['fallback'] for p in proposals if p['fallback'])),
        acceptedParentSources=dict(Counter(p['parentSource'] for p in proposals if p['rejection'] is None)),
        racing=dict(waves=len(waves), cutoffTies=sum(w['cutoffTieSize'] > 1 for w in waves),
            diversityBeyondFourth=sum(w['diversityRank'] > 4 for w in waves), diversityNominations=sum(w['diversityNominated'] for w in waves),
            diversitySelections=sum(w['diversitySelected'] for w in waves)),
        selectedNovelRoots=[p['root'] for p in pairs if p['novelSelection']],
        measuredNovelNetWinsBySelectionReason={reason: sum(p['heldout']['adaptiveWins']-p['heldout']['benchmarkWins'] for p in pairs if p['novelSelection'] and p['adaptive']['selection']['reason'] == reason)
            for reason in sorted({p['adaptive']['selection']['reason'] for p in pairs if p['novelSelection']})})
    scientific.recheck(); close.recheck()
    review = dict(version='tower-adaptive-racing-stage-review-v1', interpretation='RetrospectiveDevelopmentDiagnosisNoPolicyFittingOrPromotion',
        authentication='EveryConsumedFileAgainstExternalManifestPinsNotFullBattleReaudit', sources={str(scientific.root): scientific.consumed, str(close.root): close.consumed},
        originalDecision=result['decision'], sourceFights=result['fights'], newFights=0, newValues=0, summary=summary, pairs=pairs,
        seconds=time.monotonic()-started, analysisSourceSha256=sha(Path(__file__).read_bytes()))
    raw = (json.dumps(review, indent=2, ensure_ascii=False, allow_nan=False)+'\n').encode('utf-8')
    require(len(raw) < 16*1048576 and review['seconds'] < 180, 'Read-only review resource limit')
    output.mkdir(parents=True)
    (output/'review.json').write_bytes(raw)
    (output/'analysis.py').write_bytes(Path(__file__).read_bytes())
    (output/'files.json').write_text(json.dumps({name: sha((output/name).read_bytes()) for name in ('review.json', 'analysis.py')}, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(status='Complete', seconds=review['seconds'], newFights=0, newValues=0,
        manifestSha256=sha((output/'files.json').read_bytes()), summary=summary), indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=OUT)
    run(parser.parse_args().output)
