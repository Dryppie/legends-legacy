"""Bounded descriptive review of the twelve frozen paired preservation pools.

Reconstructs prespecified endpoints from authenticated published trials using the
admitted independent auditor. No combat, entropy, fitting or policy promotion.
"""
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import threading
import time

PLAN_PIN = '8456bb84f4f061eba78d35371602ec8a49ac81e5e60a550552c02bda3d3fddf6'
CATALOGUE_PIN = 'c9b448243b4cd2edb530678740c733e485257550de7810db6fb5296420330529'
AUDITOR_PIN = '6a49797f5c5e2c707140fc156ff07f4bbef35010fc57725054b9e9e06fa3fc5d'
BENCHMARK = '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c'
SECONDS, BYTES = 180, 32*1048576
ARMS = ('control', 'candidate')
MEMBERSHIPS = ('shared', 'control-only', 'candidate-only')
STRATA = ('mandatory', 'remaining-shared', 'remaining-control-only', 'remaining-candidate-only')


def require(ok, message):
    if not ok:
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


def load_auditor(path):
    require(sha(path.read_bytes()) == AUDITOR_PIN, 'Changed admitted auditor')
    spec = importlib.util.spec_from_file_location('paired_review_auditor', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    require(sha(path.read_bytes()) == AUDITOR_PIN, 'Auditor changed during import')
    return module


def counts(rows):
    # These are counts of measured cells, never an unweighted population mean.
    return dict(measured=len(rows), observedAboveBenchmark=sum(r['observedGain'] > 0 for r in rows),
        observedEqualBenchmark=sum(r['observedGain'] == 0 for r in rows),
        observedBelowBenchmark=sum(r['observedGain'] < 0 for r in rows),
        intervalAboveZero=sum(r['lower'] > 0 for r in rows),
        intervalBelowZero=sum(r['upper'] < 0 for r in rows))


def published_endpoint(result, study, archive_hash, auditor):
    require(result.get('studyHash') == auditor.digest(study)
        and result.get('archiveHash') == archive_hash, 'Changed published provenance hashes')
    return {k:v for k,v in result.items() if k not in ('studyHash','archiveHash')}


def review(result, plan, catalogue, evidence, panel, auditor):
    require(result['version'] == 'tower-affinity-preservation-recognition-v1'
        and result['executionStatus'] == 'Complete' and result['integrityStatus'] == 'Verified'
        and result['decision'] == 'CompleteDiagnosticOnly'
        and result['interpretation'] == 'DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion'
        and result['policyDefaultsChanged'] is False and result['samplesPerTeam'] == 256
        and result['approximateWilsonFamily'] == 799 and result['populations'] == [], 'Changed diagnostic contract')
    require(plan['version'] == catalogue['version'] == 'tower-affinity-preservation-recognition-plan-v1'
        and catalogue['status'] == 'PopulationFrozenBeforeSampling'
        and [p['root'] for p in plan['roots']] == [p['root'] for p in catalogue['roots']] == list(range(1,13)),
        'Changed frozen root family')
    # Reconstruct all rates, paired contrasts, HT means, finite-population sampling
    # variances and per-seed combat covariances, including all-root /144 variances.
    require(auditor.same(result, auditor.endpoint(plan, evidence, panel)), 'Changed prespecified endpoint')
    require(len(result['rates']) == 145 and len(result['contrasts']) == 327
        and len(result['unmeasured']) == 176 and len(result['pairedPool']['cells']) == 321,
        'Changed measured/unknown family')
    rates = {(r['root'], r['partyId']): r for r in result['rates']}
    rows, roots, cell_counts = [], [], Counter()
    for p, cat, saved in zip(plan['roots'], catalogue['roots'], result['pairedPool']['roots'], strict=True):
        root = p['root']
        require(p['benchmarkPartyId'] == cat['benchmarkPartyId'] == saved['benchmarkPartyId'] == BENCHMARK,
            'Changed fixed benchmark')
        full = {t['partyId']: t for t in cat['teams']}
        measured = {t['partyId']: t for t in p['teams']}
        unknown = {t['partyId']: t for t in p['unmeasured']}
        require(len(full) == len(cat['teams']) and len(measured) == len(p['teams'])
            and len(unknown) == len(p['unmeasured']) and not set(measured).intersection(unknown)
            and set(full) == set(measured) | set(unknown), 'Changed catalogue partition')
        require([t['partyId'] for t in p['teams'][:3]] == [t['partyId'] for t in cat['teams'][:3]],
            'Changed reference order')
        for arm in ARMS:
            provenance = [t['provenance'][arm] for t in full.values() if t['provenance'][arm] is not None]
            require(sum(t['generated'] for t in provenance) == 17
                and sorted(t['nomineeRank'] for t in provenance if t['nomineeRank'] is not None) == [1,2,3,4,5]
                and sum(t['validationChallenger'] for t in provenance) == 1, 'Changed arm population')
        for party, t in (measured | unknown).items():
            for key in ('stratum', 'membership', 'provenance'):
                require(t[key] == full[party][key], 'Changed frozen provenance')
            require(t['membership'] in MEMBERSHIPS and t['stratum'] in STRATA+('reference',), 'Unknown membership or stratum')
            pi = auditor.inclusion(t)
            if t['stratum'] in ('mandatory', 'reference'):
                require(party in measured and pi == 1, 'Lost mandatory measurement')
            else:
                stratum = p['strata'][t['stratum']]
                require(math.isclose(pi, stratum['sampleCount']/stratum['populationCount'], abs_tol=1e-15),
                    'Changed inclusion probability')
            if party in unknown:
                require(t['independentOutcome'] is None, 'Imputed unknown outcome')
            cell_counts[t['membership']] += 1
        estimates = saved['estimates']
        for kind in ('sampling', 'combat'):
            difference = estimates['control'][kind+'Variance']+estimates['candidate'][kind+'Variance']-2*estimates[kind+'Covariance']
            require(math.isclose(difference, estimates['difference'][kind+'Variance'], rel_tol=1e-10, abs_tol=1e-13),
                'Lost shared-cell covariance')
        group = [dict(c, membership=measured[c['candidateId']]['membership'],
            provenance=measured[c['candidateId']]['provenance']) for c in result['contrasts']
            if c['root'] == root and c['referenceId'] == BENCHMARK]
        require(len(group) == len(measured)-3, 'Incomplete nonreference measurements')
        rows.extend(group)
        roots.append(dict(root=root, benchmarkWins=rates[root,BENCHMARK]['wins'], measured=len(measured),
            unmeasured=len(unknown), estimates=estimates, measuredNonreferences=counts(group)))
    require(len(rows) == 109, 'Changed physical nonreference family')
    return dict(version='tower-affinity-preservation-recognition-review-v2',
        interpretation='DescriptiveDevelopmentDiagnosisNoFittingOrPromotion', newFights=0, newValues=0,
        diagnosticDecision=result['decision'], benchmarkPartyId=BENCHMARK, samplesPerTeam=256,
        approximateWilsonFamily=799, measuredCells=145, catalogueCells=321, unmeasuredCount=176,
        catalogueMembership=dict(cell_counts), measuredNonreferences=counts(rows),
        membership={s: counts([r for r in rows if r['membership'] == s]) for s in MEMBERSHIPS},
        strata={s: counts([r for r in rows if r['stratum'] == s]) for s in STRATA},
        allRoots=result['pairedPool']['allRoots'], roots=roots, measured=rows,
        limitations=result['limitations']+' Measured-cell sign counts are descriptive counts, not pool proportions. No historical outcomes are consumed or pooled; repeated recipes remain separate root instances.')


def markdown(report):
    lines = ['# Affinity-preservation recognition: descriptive review', '',
        'Decision: **CompleteDiagnosticOnly**.', '', report['limitations'], '',
        'All means and standard errors below are percentage points of victory probability. Pool means are Horvitz-Thompson estimates over 17 generated recipes per arm. Difference is preservation minus control.', '',
        '| Root | Benchmark wins /256 | Control pool | Preservation pool | Difference | Sampling SE | Combat SE |',
        '| --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for r in report['roots']+[dict(root='Equal-root mean',benchmarkWins='—',estimates=report['allRoots'])]:
        e = r['estimates']
        values = [e['control']['mean'],e['candidate']['mean'],e['difference']['mean'],
            e['difference']['samplingStandardError'],e['difference']['combatStandardError']]
        lines.append(f"| {r['root']} | {r['benchmarkWins']} | "+' | '.join(f'{100*v:+.3f}' for v in values)+' |')
    lines += ['', 'The two SE columns are separate conditional uncertainty components; do not combine them or interpret them as a confidence interval. All-root variances sum the twelve root variances and divide by 144.', '',
        '| Frozen endpoint | Mean gain | Sampling SE | Combat SE |', '| --- | ---: | ---: | ---: |']
    for name, e in report['allRoots'].items():
        if isinstance(e, dict):
            lines.append(f"| {name} | {100*e['mean']:+.3f} | {100*e['samplingStandardError']:.3f} | {100*e['combatStandardError']:.3f} |")
    lines += ['', 'Nominee means include all five frozen nominees, including reference recipes. Challenger means use the single frozen challenger per root.', '',
        '| Measured nonreference group | Cells | Observed above / equal / below benchmark | Individual interval above / below zero |',
        '| --- | ---: | ---: | ---: |']
    for name, g in [('All',report['measuredNonreferences'])]+list(report['membership'].items())+list(report['strata'].items()):
        lines.append(f"| {name} | {g['measured']} | {g['observedAboveBenchmark']} / {g['observedEqualBenchmark']} / {g['observedBelowBenchmark']} | {g['intervalAboveZero']} / {g['intervalBelowZero']} |")
    lines += ['', 'Intervals are individual approximate family-799 Wilson contrast intervals. Counts do not estimate population proportions. All **176** unsampled cell outcomes remain unknown.', '',
        '[Complete estimates, covariance terms, provenance, measured contrasts and source pins](review.json).', '']
    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('run','plan','catalogue','auditor','output'):
        parser.add_argument('--'+name, type=Path, required=True)
    parser.add_argument('--manifest-sha256', required=True)
    args = parser.parse_args()
    started = time.monotonic()
    require(not args.output.exists(), 'Never overwrite or retry a review')
    require(all(not args.output.resolve().is_relative_to(p.resolve()) for p in (args.run,args.catalogue,args.plan.parent,args.auditor.parent)),
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
            newFights=0,newValues=0,scientificManifestSha256=args.manifest_sha256,planSha256=PLAN_PIN,
            catalogueManifestSha256=CATALOGUE_PIN,auditorSha256=AUDITOR_PIN))
        (args.output/'analysis.py').write_bytes(Path(__file__).read_bytes())
        auditor = load_auditor(args.auditor)
        run, catalogue = Evidence(args.run,args.manifest_sha256), Evidence(args.catalogue,CATALOGUE_PIN)
        raw = args.plan.read_bytes()
        require(sha(raw) == PLAN_PIN, 'Changed frozen plan')
        result, independent = run.read('result.json'), run.read('independent-audit.json')
        completion, native = run.read('completion.json'), run.read('native-receipt.json')
        require(completion['status'] == 'Complete' and native['status'] == 'Verified'
            and native['fights'] == 37120 and native['newAuditFights'] == 0 and independent['status'] == 'Passed'
            and independent['result'] == result and independent['newFights'] == independent['newValues'] == 0
            and completion['requestFileHash'] == native['requestFileHash'] == independent['requestFileHash'], 'Missing audit agreement')
        study = run.read('study/study.json')
        run.read('study/files.json')
        endpoint = published_endpoint(result,study,run.consumed['study/files.json'],auditor)
        report = review(endpoint,json.loads(raw),catalogue.read('catalogue.json'),
            study['evidence'],run.read('confirmation-binding.json')['panel'],auditor)
        report['sources'] = {str(e.root): e.consumed for e in (run,catalogue)}
        report['planSha256'], report['auditorSha256'] = PLAN_PIN, AUDITOR_PIN
        report['seconds'] = time.monotonic()-started
        report['authentication'] = 'AuthenticatedPublishedTrialsAndReconstructedEndpointsNotFullBattleReaudit'
        for e in (run,catalogue):
            e.recheck()
        require(sha(args.plan.read_bytes()) == PLAN_PIN and sha(args.auditor.read_bytes()) == AUDITOR_PIN,
            'Plan or auditor changed during review')
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
