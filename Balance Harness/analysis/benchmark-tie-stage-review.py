"""Bounded read-only diagnosis of the closed benchmark-tie pilot.

Recounts every saved training trajectory and selection. Missing same-root
held-out cells stay null; no combat, allocation, policy fitting or promotion.
Full battle reconstruction and registry scanning belong to pinned publication.
"""
from collections import Counter
import importlib.util
import json
import os
from pathlib import Path
import re
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('tie_creation_review', Path(__file__).with_name('affinity-creation-stage-review.py'))
creation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(creation)
prior, kernel, audit = creation.prior, creation.kernel, creation.audit
Evidence, require, sha, digest = creation.Evidence, creation.require, creation.sha, creation.digest
RUN = ROOT/'TestResults/balance/tower-benchmark-tie-pilot-01-20260923'
RUN_PIN = 'e7c1ddff081e3f895b6d96c0cf1067b10d5b08c1376d6fe2682619467858c643'
CLOSEOUT_PIN = '0bf83061458a2d316cc76727fe50eb72cab7dcd39e2a46e512b04ac840c7b04b'
ADMISSION = ROOT/'TestResults/benchmark-tie-admission-20260923'
ADMISSION_PIN = '80c8b6023642b75c7802cf0cc8b8f56d2becb4e14d1e09f847a43d5d073c189c'
PUBLICATION = ROOT/'TestResults/benchmark-tie-pilot-01-publication-verification-20260923'
PUBLICATION_PIN = '00779bd7ce53761857619076b91653df902e7b1937c1c31db99ce94852185ccd'
OUT = ROOT/'TestResults/benchmark-tie-stage-review-20260923'
TEST_LOG = ROOT/'TestResults/benchmark-tie-stage-review-tests-20260923.log'
STUDY = 'tower-benchmark-tie-comparison-v1'
SELECTOR = 'tower-racing-benchmark-positive-tie-v1'
SECONDS, BYTES = 180, 64*1048576


def stage_contrast(panel, party):
    """Pair only the observations that actually exist on this stage."""
    rows = {pid: [o for o in panel['observations'] if o['request']['partyId'] == pid]
            for pid in (party, kernel.BENCHMARK)}
    if not rows[party]:
        return None
    a, b = rows[party], rows[kernel.BENCHMARK]
    require([o['request']['seed'] for o in a] == [o['request']['seed'] for o in b], 'Unpaired diagnostic contrast')
    x, y = [[o['outcome']['outcome'] == 'Victory' for o in group] for group in (a, b)]
    gains, losses = sum(v and not w for v, w in zip(x, y)), sum(w and not v for v, w in zip(x, y))
    if party != kernel.BENCHMARK:
        expected = dict(partyId=party, referenceId=kernel.BENCHMARK, samples=len(x), gainedWins=gains, lostWins=losses)
        require(next(c for c in panel['contrasts'] if c['partyId'] == party and c['referenceId'] == kernel.BENCHMARK) == expected,
                'Changed paired panel contrast')
    return dict(samples=len(x), wins=sum(x), benchmarkWins=sum(y), gainedWins=gains, lostWins=losses,
                netWins=gains-losses)


def arm_review(report, plan, context, policy, affinities, heldout, selected, role):
    require(role in ('control', 'candidate'), 'Unknown selector role')
    candidate = role == 'candidate'
    version = 'tower-proposal-racing-v4' if candidate else 'tower-proposal-racing-v3'
    require(plan.get('selectionPolicyVersion') == report.get('selectionPolicyVersion') == (SELECTOR if candidate else None),
            'Changed explicit selector identity')
    reviewed = creation.creation_arm(report, plan, context, policy, affinities, heldout, selected,
                                    report_version=version, benchmark_ties=candidate)
    e = report['evaluation']
    primary = kernel.PRIMARY
    require(audit.select(e['panels'][-1]['scores'], e['nominees'], primary,
                         kernel.BENCHMARK if candidate else None) == selected, 'Independent selector disagreement')
    reviewed['outputStages'] = {p['freeze']['role']: stage_contrast(p, selected) for p in e['panels']}
    reviewed['nomineeOrder'] = e['nominees']
    reviewed['nomineeHeldout'] = {pid: heldout.get(pid) for pid in e['nominees']}
    return reviewed


def analyze(scientific, admission):
    result = scientific.read('result.json')
    require(result == scientific.read('independent-audit.json')['result'] and result['version'] == STUDY
            and result['status'] == 'Verified' and result['decision'] == 'AbandonThisConfiguration', 'Changed audited selector result')
    context, design = scientific.read('source/context.json'), scientific.read('source/plan.json')
    require(design['version'] == STUDY and design['roots'] == len(result['roots']) == 12 and design['heldoutSamples'] == 256,
            'Changed frozen selector design')
    audit.validate_policies(design)
    affinities = admission.read('preview-batches.json')['damageSourceAffinities']['affinities']
    for name in ('TowerAffinityCreation.cs', 'TowerAdaptiveRacingGenerator.cs', 'TowerProposalPolicies.cs', 'TowerBatchRacing.cs'):
        admission.bytes('source/LL/tools/BalanceHarness/'+name)
    counts, pairs = Counter(), []
    for endpoint in result['roots']:
        number = endpoint['root']
        require(number == len(pairs)+1, 'Reordered roots')
        pair = scientific.read(f'study/pair-{number:02d}.json')
        require(pair['status'] == 'Complete' and pair['planHash'] == result['planHash'], 'Changed pair binding')
        audit.selector_trajectories(pair['control'], pair['candidate'])
        heldout = {}
        for role in ('control', 'candidate', 'benchmark'):
            pid, wins = endpoint[role+'Party'], endpoint[role+'Wins']
            require(pid not in heldout or heldout[pid] == wins, 'Shared held-out outcome disagrees')
            heldout[pid] = wins
        reviewed, plans = {}, {}
        for role in ('control', 'candidate'):
            plans[role] = scientific.read(f'search/root-{number:02d}/{role}/racing/plan.json')
            r = reviewed[role] = arm_review(pair[role], plans[role], context, design[role], affinities,
                                          heldout, endpoint[role+'Party'], role)
            r['selectionTrace'] = creation.selection_trace(r, endpoint, role)
            for b in pair[role]['batches']:
                counts[role+'Attempts'] += len(b['proposals'])
                counts[role+'Rejected'] += sum(p['rejection'] is not None for p in b['proposals'])
                counts[role+'ConstructionChecks'] += sum(p['constructionChecks'] for p in b['proposals'])
        require(plans['control']['racing'] == plans['candidate']['racing'], 'Changed paired racing plan')
        counts['sharedTrainingObservations'] += prior.shared_outcomes(pair['control'], pair['candidate'])
        changes = []
        for a, b in zip(pair['control']['batches'], pair['candidate']['batches']):
            positions = prior.accepted_positions(a['proposals'], b['proposals'])
            require(all(aligned and x == y for _, x, y, aligned in positions), 'Changed accepted positions')
            counts['acceptedPositions'] += len(positions)
            changes.append(sum(x['party']['id'] != y['party']['id'] for _, x, y, _ in positions))
        require(changes == endpoint['changedPositionsPerWave'] == [0, 0], 'Changed proposal divergence')
        require(endpoint['benchmarkGains']-endpoint['benchmarkLosses'] == endpoint['candidateWins']-endpoint['benchmarkWins'],
                'Changed held-out paired contrast')
        pairs.append(dict(root=number, **reviewed, heldout=endpoint))
    counts['differingOutputs'] = sum(not p['heldout']['identical'] for p in pairs)
    counts['methodNetWins'] = sum(p['heldout']['candidateWins']-p['heldout']['controlWins'] for p in pairs)
    summaries = {role: kernel.summarize([p[role] for p in pairs]) for role in ('control', 'candidate')}
    records = [r for p in pairs for r in p['candidate']['records']]
    coverage = dict(acceptedByOwner=dict(Counter(str(r['changedOwners'][0]) for r in records)),
        acceptedByDistance=dict(Counter(str(r['replacementDistance']) for r in records)),
        distinctRecipes=len({r['id'] for r in records}),
        selectedByDistance=dict(Counter(str(r['replacementDistance']) for r in records if r['selected'])))
    losses = {role: creation.loss_decomposition(pairs, role) for role in ('control', 'candidate')}
    net = sum(r['netWins'] for r in losses['candidate'].values())
    require(kernel.same(net/(12*256), result['benchmark']['mean'])
            and kernel.same(counts['methodNetWins']/(12*256), result['method']['mean'])
            and counts['differingOutputs'] == result['differingRoots'], 'Changed all-root result arithmetic')
    return dict(originalDecision=result['decision'], sourceFights=result['fights'], summary=dict(counts),
        armSummaries=summaries, coverage=coverage, benchmarkContributions=losses, pairs=pairs,
        unknownOutcomes='Only exact recipes measured on the same root have held-out values. No borrowing across roots, imputation, threshold sweep or post-hoc policy score.')


def run():
    require(not OUT.exists(), 'Review output exists; no retry or overwrite')
    code_paths = [Path(__file__), Path(creation.__file__), Path(prior.__file__), Path(kernel.__file__), Path(audit.__file__),
                  Path(__file__).with_name('test-benchmark-tie-stage-review.py')]
    code = {p: p.read_bytes() for p in code_paths}
    log = TEST_LOG.read_bytes()
    match = re.search(r'Ran (\d+) tests in ', log.decode('utf-8-sig'))
    require(match and int(match[1]) >= 10 and '\nOK' in log.decode('utf-8-sig'), 'Missing passing selector-review tests')
    started = time.monotonic()
    OUT.mkdir()
    def save(name, value):
        raw = (json.dumps(value, indent=2, allow_nan=False)+'\n').encode()
        require(sum(p.stat().st_size for p in OUT.iterdir())+len(raw) < BYTES, 'Review storage exhausted')
        with (OUT/name).open('xb') as stream:
            stream.write(raw)
    sources = [(RUN, RUN_PIN), (ADMISSION, ADMISSION_PIN), (PUBLICATION, PUBLICATION_PIN)]
    save('declaration.json', dict(kind='ReadOnlyBenchmarkTieStageReview', chargedSeconds=SECONDS, chargedBytes=BYTES,
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
        review.update(version='tower-benchmark-tie-stage-review-v1', newFights=0, newValues=0,
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
