"""Bounded descriptive review of the audited affinity-creation frozen-pool study.

Uses the prospectively frozen catalogue and plan, without historical outcomes,
selector fitting, missing-outcome imputation or policy promotion.
"""
import argparse
from collections import Counter
import hashlib
import json
import math
import os
from pathlib import Path
import threading
import time

BENCHMARK = '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c'
PLAN_PIN = '170065b593a49609e72142d766443c3a47f7a271b937cc88d52436de2b4792a2'
CATALOGUE_PIN = 'ccd05f08fda2c86ab014c12ca056d061053683d7b47c69cafee6902a08831814'
STRATA = ('nominee', 'near-miss', 'lower')
SECONDS, BYTES = 120, 16*1048576


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


def review(result, plan, catalogue):
    require(result['version'] == 'tower-affinity-creation-recognition-v1'
        and result['executionStatus'] == 'Complete' and result['integrityStatus'] == 'Verified'
        and result['decision'] == 'CompleteDiagnosticOnly'
        and result['interpretation'] == 'DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion'
        and result['policyDefaultsChanged'] is False and result['samplesPerTeam'] == 256
        and result['approximateWilsonFamily'] == 540, 'Uncompleted or changed diagnostic')
    require(plan['version'] == catalogue['version'] == 'tower-affinity-creation-recognition-plan-v1'
        and catalogue['status'] == 'PopulationFrozenBeforeSampling'
        and [p['root'] for p in plan['roots']] == [c['root'] for c in catalogue['roots']] == list(range(1,13)),
        'Changed frozen root family')
    expected_rates = [(p['root'], t['partyId'], t['stratum']) for p in plan['roots'] for t in p['teams']]
    require([(r['root'], r['partyId'], r['stratum']) for r in result['rates']] == expected_rates
        and len(expected_rates) == len(set(expected_rates)) == 108, 'Changed measured membership/order')
    expected_contrasts = [(p['root'], c['partyId'], r['partyId'], c['stratum'])
        for p in plan['roots'] for c in p['teams'][3:] for r in p['teams'][:3]]
    require([(r['root'], r['candidateId'], r['referenceId'], r['stratum']) for r in result['contrasts']]
        == expected_contrasts and len(expected_contrasts) == 216, 'Changed contrast membership/order')
    rates = {(r['root'], r['partyId']): r for r in result['rates']}
    require(len(rates) == 108, 'Duplicate root recipe')
    for r in result['rates']:
        require(type(r['wins']) is int and 0 <= r['wins'] <= 256
            and close(r['estimate']['rate'], r['wins']/256), 'Changed rate')
    for c in result['contrasts']:
        difference = rates[c['root'], c['candidateId']]['wins']-rates[c['root'], c['referenceId']]['wins']
        require(all(type(c[k]) is int and 0 <= c[k] <= 256 for k in ('gains','losses'))
            and c['gains']+c['losses'] <= 256 and c['gains']-c['losses'] == difference
            and close(c['observedGain'], difference/256)
            and -1 <= c['lower'] <= c['observedGain'] <= c['upper'] <= 1, 'Changed contrast')
    expected_nulls = [dict(root=p['root'], **u) for p in plan['roots'] for u in p['unmeasured']]
    require(result['unmeasured'] == expected_nulls and len(expected_nulls) == 132
        and all(u['independentOutcome'] is None for u in expected_nulls), 'Missing or invented unmeasured outcome')
    require([(s['root'], s['stratum']) for s in result['strata']] ==
        [(i, s) for i in range(1,13) for s in STRATA]
        and [p['root'] for p in result['populations']] == list(range(1,13)), 'Changed population membership')
    rows, roots, generated = [], [], set()
    for p, cat in zip(plan['roots'], catalogue['roots'], strict=True):
        root = p['root']
        require(p['benchmarkPartyId'] == cat['benchmarkPartyId'] == BENCHMARK, 'Changed benchmark')
        population = {t['partyId']: t['stratum'] for t in cat['teams']}
        measured = {t['partyId']: t['stratum'] for t in p['teams']}
        unmeasured = {t['partyId']: t['stratum'] for t in p['unmeasured']}
        require(len(population) == 20 and len(measured) == 9 and len(unmeasured) == 11
            and not set(measured).intersection(unmeasured) and population == measured | unmeasured
            and Counter(population.values()) == dict(reference=3, nominee=2, **{'near-miss':2, 'lower':13})
            and all(s == 'lower' for s in unmeasured.values()), 'Changed frozen catalogue membership')
        require([(t['partyId'],t['stratum']) for t in p['teams'][:3]] ==
            [(t['partyId'],t['stratum']) for t in cat['teams'][:3]]
            and all(t['stratum'] == 'reference' for t in p['teams'][:3])
            and p['teams'][2]['partyId'] == BENCHMARK, 'Changed reference order')
        generated.update(k for k,v in population.items() if v != 'reference')
        group = [dict(c, wins=rates[root,c['candidateId']]['wins']) for c in result['contrasts']
            if c['root'] == root and c['referenceId'] == BENCHMARK]
        require(len(group) == 6, 'Incomplete measured root')
        rows.extend(group)
        strata = {}
        for name in STRATA:
            pair = [r for r in group if r['stratum'] == name]
            require(len(pair) == 2, 'Changed stratum sample')
            strata[name] = aggregate(pair)
            population_size = 13 if name == 'lower' else 2
            saved = next(s for s in result['strata'] if s['root'] == root and s['stratum'] == name)
            require(saved['measured'] == 2 and saved['population'] == population_size
                and saved['inclusionWeight'] == population_size/2
                and close(saved['meanGain'], strata[name]['meanGain'])
                and close(saved['estimatedTotalGain'], population_size*strata[name]['meanGain']), 'Changed sampling weights')
        mean = (2*strata['nominee']['meanGain']+2*strata['near-miss']['meanGain']+13*strata['lower']['meanGain'])/17
        saved = result['populations'][root-1]
        require(saved['benchmarkPartyId'] == BENCHMARK and close(saved['estimatedCandidateMeanGain'], mean), 'Changed population estimate')
        roots.append(dict(root=root, benchmarkWins=rates[root,BENCHMARK]['wins'], strata=strata,
            estimatedCandidateMeanGain=mean))
    return dict(version='tower-affinity-creation-recognition-review-v1',
        interpretation='PostHocDescriptiveDevelopmentDiagnosisNoFittingOrPromotion', newFights=0, newValues=0,
        diagnosticDecision=result['decision'], benchmarkPartyId=BENCHMARK, samplesPerTeam=256,
        approximateWilsonFamily=540, generatedOccurrences=204, distinctGeneratedRecipes=len(generated),
        distinctMeasuredRecipes=len({r['candidateId'] for r in rows}), allMeasured=aggregate(rows),
        strata={s:aggregate([r for r in rows if r['stratum']==s]) for s in STRATA},
        discardedCandidates=aggregate([r for r in rows if r['stratum']!='nominee']),
        equalRootMeanOfPopulationEstimates=sum(r['estimatedCandidateMeanGain'] for r in roots)/12,
        unmeasuredCount=len(expected_nulls), roots=roots, measured=rows,
        limitations=result['limitations']+' Across-root averages are descriptive post-hoc summaries, not new qualification endpoints. Historical outcomes are not consumed or pooled. Repeated recipes remain separate root instances.')


def markdown(report):
    lines = ['# Affinity-creation recognition: descriptive review', '',
        'Decision: **CompleteDiagnosticOnly**.', '', report['limitations'], '',
        'Differences are percentage points relative to the fixed benchmark. Interval signs refer to individual family-540 combat contrasts.', '',
        '| Group | Measured | Observed above / equal / below | Interval above / below zero | Mean gain |',
        '| --- | ---: | ---: | ---: | ---: |']
    for name, g in list(report['strata'].items())+[('All measured',report['allMeasured']),('Discarded',report['discardedCandidates'])]:
        lines.append(f"| {name} | {g['measured']} | {g['observedAboveBenchmark']} / {g['observedEqualBenchmark']} / {g['observedBelowBenchmark']} | {g['intervalAboveZero']} / {g['intervalBelowZero']} | {100*g['meanGain']:+.3f} pp |")
    lines += ['', 'The all-17 estimate weights each sampled lower member by 6.5; nominees and near misses have weight one.', '',
        '| Root | Benchmark wins /256 | Nominee mean | Near-miss mean | Lower mean estimate | All-17 mean estimate |',
        '| --- | ---: | ---: | ---: | ---: | ---: |']
    for r in report['roots']:
        means = [f"{100*r['strata'][s]['meanGain']:+.3f}" for s in STRATA]
        lines.append(f"| {r['root']} | {r['benchmarkWins']} | {' | '.join(means)} | {100*r['estimatedCandidateMeanGain']:+.3f} |")
    lines += ['', f"Equal-root average of the twelve population estimates: **{100*report['equalRootMeanOfPopulationEstimates']:+.3f} pp**, without a population confidence interval.", '',
        'All 132 unsampled candidates remain unmeasured. [Machine-readable results and source pins](review.json).', '']
    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run', type=Path, required=True)
    parser.add_argument('--manifest-sha256', required=True)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--catalogue', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    started = time.monotonic()
    require(not args.output.exists(), 'Never overwrite or retry a review')
    require(all(not args.output.resolve().is_relative_to(p.resolve()) for p in (args.run,args.catalogue,args.plan.parent)),
        'Review output must be outside source archives')
    args.output.mkdir(parents=True)
    timer = threading.Timer(SECONDS, lambda: os._exit(124))
    timer.daemon = True
    timer.start()
    def save(name, value):
        with (args.output/name).open('x',encoding='utf-8') as f:
            json.dump(value,f,indent=2,allow_nan=False)
            f.write('\n')
    try:
        save('declaration.json',dict(maximumSeconds=SECONDS,maximumBytes=BYTES,charge='FullAllowanceAtStartIncludingFailure',
            newFights=0,newValues=0,scientificManifestSha256=args.manifest_sha256,planSha256=PLAN_PIN,catalogueManifestSha256=CATALOGUE_PIN))
        (args.output/'analysis.py').write_bytes(Path(__file__).read_bytes())
        run, catalogue = Evidence(args.run,args.manifest_sha256), Evidence(args.catalogue,CATALOGUE_PIN)
        raw = args.plan.read_bytes()
        require(sha(raw) == PLAN_PIN, 'Changed frozen plan')
        result, independent = run.read('result.json'), run.read('independent-audit.json')
        completion, native = run.read('completion.json'), run.read('native-receipt.json')
        require(completion['status'] == 'Complete' and native['status'] == 'Verified'
            and native['fights'] == 27648 and independent['status'] == 'Passed'
            and independent['result'] == result and independent['newFights'] == independent['newValues'] == 0
            and completion['requestFileHash'] == native['requestFileHash'] == independent['requestFileHash'], 'Missing audit agreement')
        report = review(result,json.loads(raw),catalogue.read('catalogue.json'))
        report['sources'] = {str(e.root): e.consumed for e in (run,catalogue)}
        report['planSha256'] = PLAN_PIN
        report['seconds'] = time.monotonic()-started
        report['authentication'] = 'EveryConsumedFileAgainstExternalManifestPinsNotFullBattleReaudit'
        for e in (run,catalogue):
            e.recheck()
        require(sha(args.plan.read_bytes()) == PLAN_PIN, 'Plan changed during review')
        save('review.json',report)
        (args.output/'review.md').write_text(markdown(report),encoding='utf-8')
        save('files.json',{p.name:sha(p.read_bytes()) for p in args.output.iterdir()})
        require(time.monotonic()-started < SECONDS and sum(p.stat().st_size for p in args.output.iterdir()) < BYTES,
            'Review allowance exceeded')
        print(json.dumps(dict(status='ReviewedDiagnosticOnly',manifestSha256=sha((args.output/'files.json').read_bytes()),seconds=report['seconds'])))
    except BaseException as e:
        save('failure.json',dict(reason=str(e),seconds=time.monotonic()-started))
        raise
    finally:
        timer.cancel()


if __name__ == '__main__':
    main()
