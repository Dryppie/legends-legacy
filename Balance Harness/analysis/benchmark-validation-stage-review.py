"""Bounded saved-stage diagnosis of the closed v4/v5 pilot; no new combat.

Reuse the sealed generation/racing reviewer for the common trajectory, then
recount the actual nomination and validation decisions. Never fill missing
same-root held-out cells or evaluate a replacement policy on these outcomes.
"""
from collections import Counter
import copy
import importlib.util
import json
import math
import os
from pathlib import Path
import re
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('validation_tie_review', Path(__file__).with_name('benchmark-tie-stage-review.py'))
tie = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tie)
creation, prior, kernel, audit = tie.creation, tie.prior, tie.kernel, tie.audit
Evidence, require, sha, digest = tie.Evidence, tie.require, tie.sha, tie.digest
RUN = ROOT/'TestResults/balance/tower-benchmark-validation-pilot-01-20260924'
RUN_PIN = 'fd96c4d21a27a32ad17ab750d2972a6b7dab54d09950661042b9f3af56859504'
CLOSEOUT_PIN = '15e192e436940e9bea9df5849b0305b02b71ffc33854cf025c5edb71272c46bb'
ADMISSION = ROOT/'TestResults/benchmark-validation-admission-20260924'
ADMISSION_PIN = 'e6fdb1369df6585fde905523e3190af221ef0dc9323e4b3ae3b1bf37659b5576'
PUBLICATION = ROOT/'TestResults/benchmark-validation-pilot-01-publication-verification-20260924'
PUBLICATION_PIN = '58d10de4135efa1451b9085c859752129b7a236d139037abd07126f0a75e2911'
OUT = ROOT/'TestResults/benchmark-validation-stage-review-20260924'
TEST_LOG = ROOT/'TestResults/benchmark-validation-stage-review-tests-20260924.log'
STUDY = 'tower-benchmark-validation-comparison-v1'
VERSION, SELECTOR = 'tower-proposal-racing-v5', 'tower-racing-benchmark-validation-v1'
SECONDS, BYTES = 180, 64*1048576


def category(pid):
    return 'benchmark' if pid == kernel.BENCHMARK else 'other-reference' if pid in kernel.REFERENCES else 'novel'


def final_panel(panel, planned, ids, parties, references, index, before, report_hash, scope_hash):
    """Check complete, ordered final-panel evidence before any gate recount."""
    f, observations = panel['freeze'], panel['observations']
    seeds = planned['seeds']
    require(type(panel['complete']) is bool and panel['complete']
            and f['version'] == VERSION and f['planHash'] == report_hash and f['scopeHash'] == scope_hash
            and f['index'] == index and f['role'] == planned['role'] and f['context'] == 'fixed-equipment'
            and f['seeds'] == seeds and f['parties'] == [parties[pid] for pid in ids]
            and f['evaluationsBefore'] == before
            and f['plannedEvaluations'] == len(observations) == len(ids)*len(seeds), 'Changed final panel freeze')
    rows = {pid: [] for pid in ids}
    panel_hash = observations[0]['request']['panelHash']
    for offset, (pid, seed) in enumerate((p, s) for p in ids for s in seeds):
        request, outcome = observations[offset]['request'], observations[offset]['outcome']
        require(request['partyId'] == pid and request['seed'] == outcome['seed'] == seed
                and type(request['seed']) is int and type(outcome['seed']) is int
                and request['ordinal'] == before+offset+1 and type(request['ordinal']) is int
                and request['scopeHash'] == scope_hash and request['panelHash'] == panel_hash
                and request['role'] == f['role'] and request['scenario']['seeds'] == seeds
                and outcome['outcome'] in ('Victory', 'Defeat', 'Draw')
                and all(type(outcome[k]) in (int, float) and math.isfinite(outcome[k]) and outcome[k] >= 0
                        for k in ('guardianHealth', 'survival', 'durationSeconds')), 'Changed final observation')
        require({str(p['partySlot']): p['build']['essenceIds'] for p in request['scenario']['party']} == parties[pid]['builds'],
                'Changed final scenario recipe')
        rows[pid].append(outcome)
    require(kernel.same(panel['scores'], [kernel.score(pid, values) for pid, values in rows.items()]), 'Changed final score')
    contrasts = []
    for pid in ids:
        for ref in references:
            if ref in ids and pid != ref:
                pairs = [(a['outcome'] == 'Victory', b['outcome'] == 'Victory') for a,b in zip(rows[pid], rows[ref])]
                contrasts.append(dict(partyId=pid, referenceId=ref, samples=len(seeds),
                    gainedWins=sum(a and not b for a,b in pairs), lostWins=sum(b and not a for a,b in pairs)))
    require(kernel.same(panel['contrasts'], contrasts), 'Changed final paired contrasts')
    return rows


def candidate_review(control, report, control_plan, plan, reviewed_control, heldout, selected, heldout_seeds):
    e, cp, p = report['evaluation'], control_plan['racing'], plan['racing']
    require(plan['version'] == report['version'] == e['version'] == VERSION
            and plan['selectionPolicyVersion'] == report['selectionPolicyVersion'] == SELECTOR
            and report['policyHash'] == digest(plan['policy']) == control['policyHash']
            and report['planHash'] == e['planHash'] and e['status'] == 'Complete' and e['error'] is None
            and e['plannedEvaluations'] == e['chargedEvaluations'] == p['maximumEvaluations'] == 528
            and len(e['panels']) == len(p['panels']) == 6, 'Changed validation contract')
    require({k:v for k,v in plan.items() if k not in ('version','selectionPolicyVersion','racing')} ==
            {k:v for k,v in control_plan.items() if k not in ('version','selectionPolicyVersion','racing')}
            and {k:v for k,v in p.items() if k != 'panels'} == {k:v for k,v in cp.items() if k != 'panels'}
            and p['panels'][:4] == cp['panels'][:4]
            and p['panels'][4] == dict(role='nomination', seeds=cp['panels'][4]['seeds'][:16])
            and p['panels'][5]['role'] == 'validation', 'Changed paired plans')
    training = [cp['rootSeed']]+[s for panel in cp['panels'] for s in panel['seeds']]
    validation = p['panels'][5]['seeds']
    require(len(validation) == len(set(validation)) == 60 and all(type(s) is int for s in validation)
            and not set(validation) & set(training) and len(set(training+validation)) == 133
            and len(heldout_seeds) == len(set(heldout_seeds)) == 256
            and not set(training+validation) & set(heldout_seeds), 'Reused validation or held-out seeds')
    audit.validation_trajectories(control, report)
    parties = {s['party']['id']: s['party'] for s in p['scope']['starts']}
    references = list(parties)
    for batch in report['batches']:
        parties.update((party['id'], party) for party in batch['candidates'])
        require(batch['feedbackPanels'] == [panel['observations'][0]['request']['panelHash']
                for panel in e['panels'][:2*(batch['wave']-1)]], 'Changed candidate feedback binding')
    for panel in e['panels']:
        require(panel['freeze']['version'] == VERSION and panel['freeze']['planHash'] == report['planHash'],
                'Changed candidate panel binding')
    scope_hash = control['evaluation']['panels'][0]['freeze']['scopeHash']
    final_panel(e['panels'][4], p['panels'][4], e['nominees'], parties, references, 4, 328, report['planHash'], scope_hash)
    scores = e['panels'][4]['scores']
    order = [pid for pid in e['nominees'] if pid != kernel.BENCHMARK]
    nonbenchmark = [s for s in scores if s['id'] != kernel.BENCHMARK]
    challenger = audit.select(nonbenchmark, order, kernel.PRIMARY)
    frozen = dict(version=SELECTOR, planHash=report['planHash'],
        nominationPanelHash=e['panels'][4]['observations'][0]['request']['panelHash'],
        challengerId=challenger, benchmarkId=kernel.BENCHMARK)
    require(e['validationFreeze'] == frozen, 'Changed frozen challenger')
    final_panel(e['panels'][5], p['panels'][5], [challenger, kernel.BENCHMARK], parties, references, 5, 408,
                report['planHash'], scope_hash)
    decision = audit.validation_decision(frozen, e['panels'][5])
    require(kernel.same(e['validationDecision'], decision) and selected == e['rawSelectedId'] == decision['selectedId'],
            'Changed exact validation gate or selected endpoint')
    records = copy.deepcopy(reviewed_control['records'])
    for record in records:
        record['stages'].pop('selection', None)
        record['selected'] = record['id'] == selected
        record['validationChallenger'] = record['id'] == challenger
        for panel in e['panels'][4:]:
            contrast = tie.stage_contrast(panel, record['id'])
            if contrast is not None:
                record['stages'][panel['freeze']['role']] = contrast
    challenger_score = next(s for s in scores if s['id'] == challenger)
    leaders = [s['id'] for s in nonbenchmark if s['wins'] == challenger_score['wins']]
    reason = ('ZeroWinHealthFallback' if challenger_score['wins'] == 0 else 'UniqueNonbenchmarkMaximum'
              if len(leaders) == 1 else 'PrimaryPositiveTie' if challenger == kernel.PRIMARY else 'FrozenOrderPositiveTie')
    return dict(records=records, finalBeam=reviewed_control['finalBeam'], nomineeOrder=e['nominees'],
        nominees=[dict(id=pid, category=category(pid), heldoutWins=heldout.get(pid),
                      stages={panel['freeze']['role']: tie.stage_contrast(panel, pid) for panel in e['panels']})
                  for pid in e['nominees']],
        challenger=challenger, challengerCategory=category(challenger), nominationReason=reason,
        nomination=tie.stage_contrast(e['panels'][4], challenger), validation=tie.stage_contrast(e['panels'][5], challenger),
        validationDecision=decision, challengerHeldoutWins=heldout.get(challenger),
        challengerStages={panel['freeze']['role']: tie.stage_contrast(panel, challenger) for panel in e['panels']},
        selected=selected, selectedCategory=category(selected))


def analyze(scientific, admission):
    result = scientific.read('result.json')
    require(result == scientific.read('independent-audit.json')['result'] and result['version'] == STUDY
            and result['status'] == 'Verified' and result['decision'] == 'Inconclusive', 'Changed audited validation result')
    context, design = scientific.read('source/context.json'), scientific.read('source/plan.json')
    require(design['version'] == STUDY and design['roots'] == len(result['roots']) == 12 and design['heldoutSamples'] == 256,
            'Changed frozen validation design')
    audit.validate_policies(design)
    freeze = scientific.read('study/freeze.json')
    require(freeze['version'] == STUDY and freeze['planHash'] == result['planHash'] and len(freeze['families']) == 12,
            'Changed held-out freeze')
    affinities = admission.read('preview-batches.json')['damageSourceAffinities']['affinities']
    for name in ('TowerAffinityCreation.cs', 'TowerAdaptiveRacingGenerator.cs', 'TowerProposalPolicies.cs',
                 'TowerBatchRacing.cs', 'TowerBenchmarkValidation.cs', 'TowerBenchmarkValidationComparison.cs'):
        admission.bytes('source/LL/tools/BalanceHarness/'+name)
    counts, pairs = Counter(), []
    for endpoint, family in zip(result['roots'], freeze['families']):
        number = endpoint['root']
        require(number == family['root'] == len(pairs)+1, 'Reordered roots')
        pair = scientific.read(f'study/pair-{number:02d}.json')
        require(pair['status'] == 'Complete' and pair['planHash'] == result['planHash'], 'Changed pair binding')
        heldout = {}
        for role in ('control', 'candidate', 'benchmark'):
            pid, wins = endpoint[role+'Party'], endpoint[role+'Wins']
            require(type(wins) is int and 0 <= wins <= 256 and (pid not in heldout or heldout[pid] == wins),
                    'Changed held-out outcome')
            heldout[pid] = wins
            require(any(role in member['roles'] and member['party']['id'] == pid for member in family['members']),
                    'Changed frozen held-out role')
        require(endpoint['benchmarkParty'] == kernel.BENCHMARK
                and endpoint['identical'] == (endpoint['controlParty'] == endpoint['candidateParty'])
                and endpoint['novel'] == (endpoint['candidateParty'] not in kernel.REFERENCES), 'Changed endpoint identity')
        plans = {role: scientific.read(f'search/root-{number:02d}/{role}/racing/plan.json') for role in ('control','candidate')}
        require(plans['control']['selectionPolicyVersion'] == pair['control']['selectionPolicyVersion'] == tie.SELECTOR,
                'Changed control selector identity')
        control = creation.creation_arm(pair['control'], plans['control'], context, design['control'], affinities,
                    heldout, endpoint['controlParty'], report_version='tower-proposal-racing-v4', benchmark_ties=True)
        control['selectionTrace'] = creation.selection_trace(control, endpoint, 'control')
        candidate = candidate_review(pair['control'], pair['candidate'], plans['control'], plans['candidate'],
                    control, heldout, endpoint['candidateParty'], family['seeds'])
        e = pair['candidate']['evaluation']
        for name, expected in [('validation-freeze.json', e['validationFreeze']), ('validation-decision.json', e['validationDecision'])]:
            require(scientific.read(f'search/root-{number:02d}/candidate/racing/{name}') == expected, 'Changed saved validation record')
        for i, panel in enumerate(e['panels'], 1):
            require(scientific.read(f'search/root-{number:02d}/candidate/racing/panel-{i:02d}.json') == panel['freeze'],
                    'Changed saved candidate freeze')
        counts['sharedRacingObservations'] += prior.shared_outcomes(pair['control'], pair['candidate'])
        counts['sharedNominationObservations'] += len(e['panels'][4]['observations'])
        for batch in pair['candidate']['batches']:
            counts['attempts'] += len(batch['proposals'])
            counts['rejectedAttempts'] += sum(p['rejection'] is not None for p in batch['proposals'])
        require(endpoint['changedPositionsPerWave'] == [0,0], 'Changed paired generation')
        for prefix, left, right in [('method','candidate','control'),('benchmark','candidate','benchmark')]:
            require(endpoint[prefix+'Gains']-endpoint[prefix+'Losses'] == endpoint[left+'Wins']-endpoint[right+'Wins'],
                    'Changed paired held-out contrast')
        candidate['selectionTrace'] = dict(category=candidate['selectedCategory'], heldoutNetWins=endpoint['candidateWins']-endpoint['benchmarkWins'])
        pairs.append(dict(root=number, control=control, candidate=candidate, heldout=endpoint))
    records = [r for p in pairs for r in p['candidate']['records']]
    counts.update(acceptedOccurrences=len(records), novelNomineeOccurrences=sum(r['nominated'] for r in records),
        novelValidationChallengers=sum(p['candidate']['challengerCategory'] == 'novel' for p in pairs),
        validationPasses=sum(p['candidate']['validationDecision']['passed'] for p in pairs),
        novelValidationPasses=sum(p['candidate']['validationDecision']['passed'] and p['candidate']['challengerCategory'] == 'novel' for p in pairs),
        novelOutputs=sum(p['candidate']['selectedCategory'] == 'novel' for p in pairs),
        generatedHeldoutKnown=sum(r['heldoutWins'] is not None for r in records),
        nominatedHeldoutKnown=sum(r['nominated'] and r['heldoutWins'] is not None for r in records),
        challengerHeldoutKnown=sum(p['candidate']['challengerHeldoutWins'] is not None for p in pairs),
        differingOutputs=sum(not p['heldout']['identical'] for p in pairs),
        methodNetWins=sum(p['heldout']['candidateWins']-p['heldout']['controlWins'] for p in pairs))
    arm_summaries = dict(control=kernel.summarize([p['control'] for p in pairs]),
        candidate=dict(challengerCategories=dict(Counter(p['candidate']['challengerCategory'] for p in pairs)),
            selectedCategories=dict(Counter(p['candidate']['selectedCategory'] for p in pairs)),
            nominationReasons=dict(Counter(p['candidate']['nominationReason'] for p in pairs))))
    coverage = dict(distinctRecipes=len({r['id'] for r in records}),
        acceptedByOwner=dict(Counter(str(r['changedOwners'][0]) for r in records)),
        acceptedByDistance=dict(Counter(str(r['replacementDistance']) for r in records)),
        nominatedByDistance=dict(Counter(str(r['replacementDistance']) for r in records if r['nominated'])),
        validationByDistance=dict(Counter(str(r['replacementDistance']) for r in records if r['validationChallenger'])))
    losses = {role: creation.loss_decomposition(pairs, role) for role in ('control','candidate')}
    require(kernel.same(sum(r['netWins'] for r in losses['candidate'].values())/(12*256), result['benchmark']['mean'])
            and kernel.same(counts['methodNetWins']/(12*256), result['method']['mean'])
            and counts['differingOutputs'] == result['differingRoots']
            and counts['validationPasses'] == result['validation']['passedRoots'], 'Changed all-root arithmetic')
    return dict(originalDecision=result['decision'], sourceFights=result['fights'], summary=dict(counts),
        armSummaries=arm_summaries, coverage=coverage, benchmarkContributions=losses, pairs=pairs,
        unknownOutcomes='Only exact recipes measured on the same root have held-out values. No cross-root borrowing, imputation, threshold sweep or retrospective replacement-policy score.')


def run():
    require(not OUT.exists(), 'Review output exists; no retry or overwrite')
    code_paths = [Path(__file__), Path(tie.__file__), Path(creation.__file__), Path(prior.__file__), Path(kernel.__file__), Path(audit.__file__),
                  Path(__file__).with_name('test-benchmark-validation-stage-review.py')]
    code = {p: p.read_bytes() for p in code_paths}
    log = TEST_LOG.read_bytes()
    match = re.search(r'Ran (\d+) tests in ', log.decode('utf-8-sig'))
    require(match and int(match[1]) >= 10 and '\nOK' in log.decode('utf-8-sig'), 'Missing passing validation-review tests')
    started = time.monotonic()
    OUT.mkdir()
    def save(name, value):
        raw = (json.dumps(value, indent=2, allow_nan=False)+'\n').encode()
        require(sum(p.stat().st_size for p in OUT.iterdir())+len(raw) < BYTES, 'Review storage exhausted')
        with (OUT/name).open('xb') as stream:
            stream.write(raw)
    sources = [(RUN, RUN_PIN), (ADMISSION, ADMISSION_PIN), (PUBLICATION, PUBLICATION_PIN)]
    save('declaration.json', dict(kind='ReadOnlyBenchmarkValidationStageReview', chargedSeconds=SECONDS, chargedBytes=BYTES,
        sourceManifests={str(p): pin for p, pin in sources}, closeoutSha256=CLOSEOUT_PIN,
        implementation={str(p.relative_to(ROOT)): sha(raw) for p, raw in code.items()}, testsSha256=sha(log), testsPassed=int(match[1]),
        newFights=0, newValues=0, scope='Full separate engineering allowance charged on success or failure. No scientific extension, fitting or promotion.'))
    timer = threading.Timer(SECONDS, lambda: os._exit(124))
    timer.daemon = True
    timer.start()
    try:
        scientific, admission, publication = [Evidence(p, pin) for p, pin in sources]
        receipt = publication.read('verification.json')
        require(receipt['status'] == 'VerifiedPublishedArchiveAndCompleteLiveHistory'
                and receipt['scientificManifestSha256'] == RUN_PIN and receipt['scientificCloseoutSha256'] == CLOSEOUT_PIN
                and receipt['admissionManifestSha256'] == ADMISSION_PIN and sha((RUN/'closeout.json').read_bytes()) == CLOSEOUT_PIN,
                'Changed publication binding')
        review = analyze(scientific, admission)
        for evidence in (scientific, admission, publication):
            evidence.recheck()
        require(sha((RUN/'closeout.json').read_bytes()) == CLOSEOUT_PIN and all(p.read_bytes() == raw for p, raw in code.items())
                and TEST_LOG.read_bytes() == log, 'Review inputs changed')
        review.update(version='tower-benchmark-validation-stage-review-v1', newFights=0, newValues=0,
            interpretation='RetrospectiveDevelopmentDiagnosisNoPolicyFittingOrPromotion',
            authentication='AllConsumedFilesAndSavedRacingStages;NotFullBattleOrLiveHistoryReaudit',
            sources={str(e.root): e.consumed for e in (scientific, admission, publication)}, scientificCloseoutSha256=CLOSEOUT_PIN,
            reviewChargedSeconds=SECONDS, reviewChargedBytes=BYTES,
            totalRecordedChargedSeconds=receipt['totalRecordedChargedSeconds']+SECONDS,
            totalRecordedChargedBytes=receipt['totalRecordedChargedBytes']+BYTES,
            cumulativeDeclaredMaximumSeconds=receipt['cumulativeDeclaredMaximumSeconds']+SECONDS,
            cumulativeDeclaredMaximumBytes=receipt['cumulativeDeclaredMaximumBytes']+BYTES,
            lastVerifiedHistory=dict(values=receipt['liveValues'], files=receipt['historyFiles'], rescannedByThisReview=False),
            secondsBeforeSealing=time.monotonic()-started)
        save('review.json', review)
        for p, raw in code.items():
            (OUT/p.name).write_bytes(raw)
        (OUT/'tests.log').write_bytes(log)
        save('files.json', {p.name: sha(p.read_bytes()) for p in sorted(OUT.iterdir())})
        require(time.monotonic()-started < SECONDS and sum(p.stat().st_size for p in OUT.iterdir()) < BYTES, 'Review allowance exhausted')
        print(json.dumps(dict(status='Complete', seconds=time.monotonic()-started, retainedBytes=sum(p.stat().st_size for p in OUT.iterdir()),
            manifestSha256=sha((OUT/'files.json').read_bytes()), summary=review['summary'], armSummaries=review['armSummaries'],
            coverage=review['coverage'], benchmarkContributions=review['benchmarkContributions']), indent=2))
    except BaseException as error:
        save('failure.json', dict(reason=str(error), chargedSeconds=SECONDS, chargedBytes=BYTES))
        raise
    finally:
        timer.cancel()


if __name__ == '__main__':
    run()
