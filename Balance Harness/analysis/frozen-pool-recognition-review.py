"""Read-only descriptive review of the completed frozen-pool recognition study.

Consumes pinned, already audited results and historical stage metadata. Does not
pool old and new combat panels, fit a selector, infer missing outcomes or promote
teams. The result's family-540 intervals apply to individual combat contrasts;
the sampled population means have no population confidence intervals.
"""
import argparse
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import time

BENCHMARK = '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c'
PLAN_PIN = 'f8e8b206cd6b0cf46f420ab4d6d4d4a9568e5e46186ae8a8ae18a552fa0957b8'
STAGE_PIN = 'f4087410b3647ef1294c732f0570459daeb901913617502216345828a2508835'
STRATA = ('nominee', 'near-miss', 'lower')


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


class Evidence:
    def __init__(self, root, pin):
        self.root = Path(root).resolve()
        raw = (self.root/'files.json').read_bytes()
        require(sha(raw) == pin, 'Changed external manifest')
        self.files = json.loads(raw)
        self.consumed = {'files.json': pin}

    def read(self, name):
        path = (self.root/name).resolve()
        require(name in self.files and path.is_relative_to(self.root), 'Unbound evidence path')
        raw = path.read_bytes()
        require(sha(raw) == self.files[name], 'Changed consumed evidence: '+name)
        self.consumed[name] = sha(raw)
        return json.loads(raw)

    def recheck(self):
        for name, pin in self.consumed.items():
            require(sha((self.root/name).read_bytes()) == pin, 'Evidence changed during review')


def close(a, b):
    return math.isfinite(a) and math.isclose(a, b, rel_tol=1e-12, abs_tol=1e-12)


def aggregate(rows):
    require(rows, 'Empty descriptive group')
    return dict(measured=len(rows), observedAboveBenchmark=sum(r['observedGain'] > 0 for r in rows),
        observedEqualBenchmark=sum(r['observedGain'] == 0 for r in rows),
        observedBelowBenchmark=sum(r['observedGain'] < 0 for r in rows),
        intervalAboveZero=sum(r['lower'] > 0 for r in rows),
        intervalBelowZero=sum(r['upper'] < 0 for r in rows),
        meanGain=sum(r['observedGain'] for r in rows)/len(rows))


def review(result, plan, stage):
    require(result['version'] == 'tower-frozen-pool-recognition-v1'
        and result['executionStatus'] == 'Complete' and result['integrityStatus'] == 'Verified'
        and result['decision'] == 'CompleteDiagnosticOnly'
        and result['interpretation'] == 'DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion'
        and result['policyDefaultsChanged'] is False and result['samplesPerTeam'] == 256
        and result['approximateWilsonFamily'] == 540, 'Uncompleted or changed diagnostic')
    require(len(plan['roots']) == len(stage['pairs']) == 12
        and stage['originalDecision'] == 'AbandonThisConfiguration', 'Changed source study')
    expected_rates = [(p['root'], t['partyId'], t['stratum']) for p in plan['roots'] for t in p['teams']]
    require([(r['root'], r['partyId'], r['stratum']) for r in result['rates']] == expected_rates
        and len(expected_rates) == len(set(expected_rates)) == 108, 'Changed measured membership/order')
    expected_contrasts = [(p['root'], c['partyId'], r['partyId'], c['stratum'])
        for p in plan['roots'] for c in p['teams'][3:] for r in p['teams'][:3]]
    require([(r['root'], r['candidateId'], r['referenceId'], r['stratum']) for r in result['contrasts']]
        == expected_contrasts and len(expected_contrasts) == 216, 'Changed contrast membership/order')
    rates = {(r['root'], r['partyId']): r for r in result['rates']}
    for r in result['rates']:
        require(type(r['wins']) is int and 0 <= r['wins'] <= 256
            and close(r['estimate']['rate'], r['wins']/256), 'Changed rate')
    for c in result['contrasts']:
        difference = rates[c['root'], c['candidateId']]['wins']-rates[c['root'], c['referenceId']]['wins']
        require(c['gains']-c['losses'] == difference and close(c['observedGain'], difference/256)
            and -1 <= c['lower'] <= c['observedGain'] <= c['upper'] <= 1, 'Changed contrast')
    expected_nulls = [dict(root=p['root'], **u) for p in plan['roots'] for u in p['unmeasured']]
    require(result['unmeasured'] == expected_nulls and len(expected_nulls) == 132
        and all(u['independentOutcome'] is None for u in expected_nulls), 'Missing or invented unmeasured outcome')
    require([(s['root'], s['stratum']) for s in result['strata']] ==
        [(i, s) for i in range(1,13) for s in STRATA]
        and [p['root'] for p in result['populations']] == list(range(1,13)), 'Changed population membership')
    rows, roots = [], []
    for p, old in zip(plan['roots'], stage['pairs']):
        root = p['root']
        require(root == old['root'] and p['benchmarkPartyId'] == BENCHMARK, 'Mismatched root/benchmark')
        history = {r['id']: r for r in old['adaptive']['records']}
        require(len(history) == 17 and set(history) ==
            {t['partyId'] for t in p['teams'][3:]} | {u['partyId'] for u in p['unmeasured']}, 'Changed frozen pool')
        group = []
        for c in result['contrasts']:
            if c['root'] != root or c['referenceId'] != BENCHMARK:
                continue
            h = history[c['candidateId']]
            require(h['nominated'] == (c['stratum'] == 'nominee'), 'Changed historical nomination')
            row = dict(c, wins=rates[root,c['candidateId']]['wins'], operator=h['operator'],
                nominated=h['nominated'], historicallySelected=h['selected'],
                historicalTraining={k:h[k] for k in ('wave','parentSource','parents','replacementDistance','stages')})
            group.append(row)
            rows.append(row)
        require(len(group) == 6, 'Incomplete measured root')
        strata = {}
        for name in STRATA:
            pair = [r for r in group if r['stratum'] == name]
            require(len(pair) == 2, 'Changed stratum sample')
            strata[name] = aggregate(pair)
            population = 13 if name == 'lower' else 2
            saved = next(s for s in result['strata'] if s['root'] == root and s['stratum'] == name)
            require(saved['measured'] == 2 and saved['population'] == population
                and saved['inclusionWeight'] == population/2
                and close(saved['meanGain'], strata[name]['meanGain'])
                and close(saved['estimatedTotalGain'], population*strata[name]['meanGain']), 'Changed sampling weights')
        mean = (2*strata['nominee']['meanGain']+2*strata['near-miss']['meanGain']+13*strata['lower']['meanGain'])/17
        saved = result['populations'][root-1]
        require(saved['benchmarkPartyId'] == BENCHMARK and close(saved['estimatedCandidateMeanGain'], mean), 'Changed population estimate')
        selected = old['adaptive']['selection']['selected']
        selected_row = next((r for r in group if r['candidateId'] == selected), None)
        require((selected_row is not None) == (not old['adaptive']['selection']['reference']), 'Unmeasured historical selected challenger')
        require(all(r['historicallySelected'] == (r['candidateId'] == selected) for r in group), 'Changed historical selection')
        roots.append(dict(root=root, benchmarkWins=rates[root,BENCHMARK]['wins'], strata=strata,
            estimatedCandidateMeanGain=mean, historicallySelected=selected,
            selectedChallengerGain=None if selected_row is None else selected_row['observedGain']))
    return dict(version='tower-frozen-pool-recognition-review-v1',
        interpretation='PostHocDescriptiveDevelopmentDiagnosisNoFittingOrPromotion', newFights=0, newValues=0,
        originalPilotDecision=stage['originalDecision'], diagnosticDecision=result['decision'],
        benchmarkPartyId=BENCHMARK, samplesPerTeam=256, approximateWilsonFamily=540,
        allMeasured=aggregate(rows), strata={s:aggregate([r for r in rows if r['stratum']==s]) for s in STRATA},
        historicallySelectedChallengers=aggregate([r for r in rows if r['historicallySelected']]),
        discardedCandidates=aggregate([r for r in rows if not r['nominated']]),
        equalRootMeanOfPopulationEstimates=sum(r['estimatedCandidateMeanGain'] for r in roots)/12,
        unmeasuredCount=len(expected_nulls), roots=roots, measured=rows,
        measuredOperatorCounts=dict(sorted(Counter(r['operator'] for r in rows).items())),
        limitations=result['limitations']+' Across-root averages and historical-selection groups are descriptive post-hoc summaries, not new qualification endpoints. Old held-out results are not pooled.')


def markdown(report):
    lines = ['# Frozen-pool recognition: descriptive review', '',
        'Decision: **CompleteDiagnosticOnly**. The original pilot remains **AbandonThisConfiguration**.', '',
        report['limitations'], '',
        'Differences below are percentage points relative to the fixed benchmark. Interval signs refer to individual family-540 combat contrasts.', '',
        '| Group | Measured | Observed above / equal / below | Interval above / below zero | Mean gain |',
        '| --- | ---: | ---: | ---: | ---: |']
    groups = list(report['strata'].items()) + [('Historically selected challengers',report['historicallySelectedChallengers'])]
    for name, g in groups:
        lines.append(f"| {name} | {g['measured']} | {g['observedAboveBenchmark']} / {g['observedEqualBenchmark']} / {g['observedBelowBenchmark']} | {g['intervalAboveZero']} / {g['intervalBelowZero']} | {100*g['meanGain']:+.3f} pp |")
    lines += ['', 'The lower-stratum mean uses two sampled members per root. The all-17 estimates weight these members by 6.5; the four nominees/near-misses each have weight one.', '',
        '| Root | R* wins /256 | Nominee mean | Near-miss mean | Lower mean estimate | All-17 mean estimate | Selected challenger gain |',
        '| --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for r in report['roots']:
        means = [f"{100*r['strata'][s]['meanGain']:+.3f}" for s in STRATA]
        selected = 'Reference selected' if r['selectedChallengerGain'] is None else f"{100*r['selectedChallengerGain']:+.3f}"
        lines.append(f"| {r['root']} | {r['benchmarkWins']} | {' | '.join(means)} | {100*r['estimatedCandidateMeanGain']:+.3f} | {selected} |")
    lines += ['', f"The equal-root average of the twelve population mean estimates is **{100*report['equalRootMeanOfPopulationEstimates']:+.3f} pp**. This is a descriptive point estimate, with no population confidence interval.", '',
        'All 132 unsampled candidates remain unmeasured. [Machine-readable results, historical training metadata and source pins](review.json).', '']
    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run', type=Path, required=True)
    parser.add_argument('--manifest-sha256', required=True)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--stage-review', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    started = time.monotonic()
    require(not args.output.exists(), 'Never overwrite a review')
    require(all(not args.output.resolve().is_relative_to(p.resolve()) for p in (args.run,args.stage_review)),
        'Review output must be outside source archives')
    args.output.mkdir(parents=True)
    try:
        run, stage = Evidence(args.run, args.manifest_sha256), Evidence(args.stage_review, STAGE_PIN)
        raw = args.plan.read_bytes()
        require(sha(raw) == PLAN_PIN, 'Changed frozen plan')
        result, independent = run.read('result.json'), run.read('independent-audit.json')
        completion, native = run.read('completion.json'), run.read('native-receipt.json')
        require(completion['status'] == 'Complete' and native['status'] == 'Verified'
            and native['fights'] == 27648 and independent['status'] == 'Passed'
            and independent['result'] == result and independent['newFights'] == independent['newValues'] == 0
            and completion['requestFileHash'] == native['requestFileHash'] == independent['requestFileHash'], 'Missing audit agreement')
        report = review(result, json.loads(raw), stage.read('review.json'))
        report['sources'] = {str(e.root): e.consumed for e in (run,stage)}
        report['planSha256'] = PLAN_PIN
        report['seconds'] = time.monotonic()-started
        report['authentication'] = 'EveryConsumedFileAgainstExternalManifestPinsNotFullBattleReaudit'
        for e in (run,stage):
            e.recheck()
        require(sha(args.plan.read_bytes()) == PLAN_PIN, 'Plan changed during review')
        (args.output/'review.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
        (args.output/'review.md').write_text(markdown(report),encoding='utf-8')
        (args.output/'analysis.py').write_bytes(Path(__file__).read_bytes())
        files = {p.name:sha(p.read_bytes()) for p in args.output.iterdir()}
        (args.output/'files.json').write_text(json.dumps(files,indent=2)+'\n',encoding='utf-8')
        print(json.dumps(dict(status='ReviewedDiagnosticOnly',manifestSha256=sha((args.output/'files.json').read_bytes()),seconds=report['seconds'])))
    except BaseException as e:
        (args.output/'failure.json').write_text(json.dumps(dict(reason=str(e))),encoding='utf-8')
        raise


if __name__ == '__main__':
    main()
