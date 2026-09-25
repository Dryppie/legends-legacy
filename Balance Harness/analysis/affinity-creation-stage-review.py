"""Bounded read-only diagnosis of the closed creation pilot; never runs combat.

Recounts all saved training panels and selection paths. Authenticates consumed
records; relies on the pinned publication verification for raw battles/history.
Unknown same-root held-out outcomes stay null. No policy fitting or promotion.
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


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result); return result


prior = module('creation_stage_prior', Path(__file__).with_name('proposal-affinity-stage-review.py'))
audit = module('creation_stage_audit', Path(__file__).with_name('audit-proposal-affinity-study.py'))
kernel = prior.kernel
Evidence, require, sha, digest = prior.Evidence, prior.require, prior.sha, prior.digest
RUN = ROOT/'TestResults/balance/tower-affinity-creation-pilot-01-20260923'
RUN_PIN = '67ba1f437ab09a06d13eb76647a3d5e10e0aca7eba56f33e01775216a0630139'
CLOSEOUT_PIN = '23fe77e83ad692f9f715681a180d45876060820f6ae6f46fdd04ce0eb57ae7c8'
ADMISSION = ROOT/'TestResults/affinity-creation-admission-20260923'
ADMISSION_PIN = '554071201e9c01f99dbe084e5561df22fa7c97b785d823802d39a808898fa884'
PUBLICATION = ROOT/'TestResults/affinity-creation-pilot-01-publication-verification-20260923'
PUBLICATION_PIN = '40c6dddac80e78d1a6c3de2b205cbf3fbde148f18c7b7077ea0228685e7c5283'
OUT = ROOT/'TestResults/affinity-creation-stage-review-20260923'
TEST_LOG = ROOT/'TestResults/affinity-creation-stage-review-tests-20260923.log'
VERSION, STUDY = 'tower-proposal-racing-v3', 'tower-affinity-creation-comparison-v1'
SECONDS, BYTES = 180, 64*1048576


def route_step(proposal, parent, policy, affinities):
    audit.creation_step(proposal, parent, policy)
    by_id = {a['id']: a for a in affinities}
    selected = policy['createdDamageAffinityIds']
    require(len(by_id) == len(affinities) and set(selected) <= by_id.keys(), 'Unresolved authored affinity')
    if proposal['party'] is None: return
    pairs = {}
    for aid in selected:
        a = by_id[aid]; pair = sorted({a['producerEssenceId'], a['modifierEssenceId']})
        require(len(pair) == 2, 'Invalid affinity endpoints')
        pairs.setdefault(digest(pair), []).append(aid)
    step = proposal['affinityCreation']; owner = str(proposal['scheduledOwners'][0])
    old, new = set(parent['builds'][owner]), set(proposal['party']['builds'][owner])
    active = sorted(aid for aid in selected if
        {by_id[aid]['producerEssenceId'], by_id[aid]['modifierEssenceId']} <= new and not
        {by_id[aid]['producerEssenceId'], by_id[aid]['modifierEssenceId']} <= old)
    require(step['targetAffinityIds'] == sorted(pairs.get(step['pairId'], []))
            and step['newlyActivatedAffinityIds'] == active, 'Changed authored route activation')


def creation_arm(report, plan, context, policy, affinities, heldout, selected, *, report_version=VERSION, benchmark_ties=False):
    require(plan['version'] == report['version'] == report['evaluation']['version'] == report_version
            and plan['policy'] == policy and report['policyHash'] == digest(policy)
            and plan['damageAffinityInventory'] == context['damageAffinityInventory']
            and plan['racing']['mechanics'] == context['mechanics'], 'Changed creation contract')
    scope = plan['racing']['scope']; starts = scope['starts']
    parties = {s['party']['id']: s['party'] for s in starts}
    primary = next(s['party']['id'] for s in starts if s['referenceId'] == scope['stages']['selectionPrimaryReferenceId'])
    benchmark = next(s['party'] for s in starts if s['referenceId'] == plan['racing']['benchmarkReferenceId'])
    require(set(parties) == kernel.REFERENCES and primary == kernel.PRIMARY and benchmark['id'] == kernel.BENCHMARK,
            'Changed review references')
    allowed = {e['id']: e['family'] for e in scope['allowedEssences']}
    for batch in report['batches']:
        seen = set(parties)
        for p in batch['proposals']:
            require(p['requestedOperator'] == p['effectiveOperator'] == 'affinity-create'
                    and p['parents'] == [benchmark['id']] and p['parentSource'] == 'benchmark', 'Changed creation lineage')
            route_step(p, benchmark, policy, affinities)
            if p['party'] is not None:
                party = p['party']; pid = party['id']
                require(pid == digest(party['builds']) and p['rejection'] == ('duplicate-recipe' if pid in seen else None),
                        'Changed creation identity/duplicate rejection')
                for ids in party['builds'].values():
                    require(ids == sorted(set(ids)) and len(ids) == scope['budget']['essenceSlots']
                            and all(e in allowed for e in ids) and len({allowed[e].casefold() for e in ids}) == len(ids),
                            'Illegal creation recipe')
                seen.add(pid); parties[pid] = party
        require(batch['feedbackPanels'] == [p['observations'][0]['request']['panelHash']
            for p in report['evaluation']['panels'][:2*(batch['wave']-1)]], 'Changed feedback binding')
    for panel in report['evaluation']['panels']:
        f = panel['freeze']
        require(f['version'] == report_version and f['planHash'] == report['planHash']
                and all(p == parties.get(p['id']) for p in f['parties']), 'Changed panel recipe/binding')
    result = kernel.adaptive(report, plan['racing'], heldout, selected, report_version=report_version, benchmark_ties=benchmark_ties)
    records = {r['id']: r for r in result['records']}
    for batch in report['batches']:
        for position,p in enumerate((p for p in batch['proposals'] if p['rejection'] is None),1):
            records[p['party']['id']].update(position=position,attempt=p['attempt'],scheduledOwners=p['scheduledOwners'],
                edits=prior.edits(benchmark,p['party']),affinityCreation=p['affinityCreation'])
    result['finalBeam'] = [dict(rank=i, **next(s for s in report['evaluation']['decisions'][1]['commonScores'] if s['id'] == pid))
        for i,pid in enumerate(report['evaluation']['decisions'][1]['beamIds'],1)]
    return result


def selection_trace(review, endpoint, role):
    selection = review['selection']; pid = selection['selected']
    wins, benchmark = endpoint[role+'Wins'], endpoint['benchmarkWins']
    require(pid == endpoint[role+'Party'], 'Changed selected endpoint')
    category = 'benchmark' if pid == kernel.BENCHMARK else 'other-reference' if pid in kernel.REFERENCES else 'novel'
    return dict(category=category, **selection, heldoutWins=wins, heldoutBenchmarkWins=benchmark,
        heldoutNetWins=wins-benchmark, heldoutSamples=256,
        selectedProposal=next((r for r in review['records'] if r['id'] == pid),None))


def loss_decomposition(pairs, role):
    rows = [p[role]['selectionTrace'] for p in pairs]
    return {category:dict(roots=[p['root'] for p in pairs if p[role]['selectionTrace']['category'] == category],
        netWins=sum(r['heldoutNetWins'] for r in rows if r['category'] == category),
        equalRootContribution=sum(r['heldoutNetWins'] for r in rows if r['category'] == category)/(12*256))
        for category in ('benchmark','other-reference','novel')}


def analyze(scientific, admission):
    result = scientific.read('result.json')
    require(result == scientific.read('independent-audit.json')['result'] and result['version'] == STUDY
            and result['status'] == 'Verified' and result['decision'] == 'Inconclusive', 'Changed audited creation result')
    context, design = scientific.read('source/context.json'), scientific.read('source/plan.json')
    require(design['version'] == STUDY and design['roots'] == len(result['roots']) == 12 and design['heldoutSamples'] == 256,
            'Changed frozen creation design')
    audit.validate_policies(design)
    affinities = admission.read('preview-batches.json')['damageSourceAffinities']['affinities']
    for name in ('TowerAffinityCreation.cs','TowerAdaptiveRacingGenerator.cs','TowerProposalPolicies.cs','TowerBatchRacing.cs'):
        admission.bytes('source/LL/tools/BalanceHarness/'+name)
    counts, pairs, proposals = Counter(), [], {'control':[], 'candidate':[]}
    for endpoint in result['roots']:
        number = endpoint['root']; require(number == len(pairs)+1, 'Reordered roots')
        pair = scientific.read(f'study/pair-{number:02d}.json')
        require(pair['status'] == 'Complete' and pair['planHash'] == result['planHash'], 'Changed pair binding')
        heldout = {}
        for role in ('control','candidate','benchmark'):
            pid, wins = endpoint[role+'Party'], endpoint[role+'Wins']
            require(pid not in heldout or heldout[pid] == wins, 'Shared held-out outcome disagrees'); heldout[pid] = wins
        reviews, plans = {}, {}
        for role in ('control','candidate'):
            plan = plans[role] = scientific.read(f'search/root-{number:02d}/{role}/racing/plan.json')
            args = (pair[role],plan,context,design[role])
            reviewed = prior.arm_review(*args,heldout,endpoint[role+'Party']) if role == 'control' else creation_arm(
                *args,affinities,heldout,endpoint[role+'Party'])
            reviewed['selectionTrace'] = selection_trace(reviewed,endpoint,role); reviews[role] = reviewed
            for b in pair[role]['batches']:
                for p in b['proposals']:
                    counts[role+'Attempts'] += 1; counts[role+'Rejected'] += p['rejection'] is not None
                    counts[role+'ConstructionChecks'] += p['constructionChecks']
            proposals[role].extend(dict(root=number,**r) for r in reviewed['records'])
        require(plans['control']['racing'] == plans['candidate']['racing'], 'Changed paired racing plan')
        counts['sharedTrainingObservations'] += prior.shared_outcomes(pair['control'],pair['candidate'])
        changes = [sum(x['party']['id'] != y['party']['id'] for _,x,y,_ in prior.accepted_positions(a['proposals'],b['proposals']))
            for a,b in zip(pair['control']['batches'],pair['candidate']['batches'])]
        require(changes == endpoint['changedPositionsPerWave'], 'Changed divergence count')
        counts['changedPositions'] += sum(changes)
        pairs.append(dict(root=number,heldout=endpoint,**reviews))
    for role, records in proposals.items():
        counts.update({role+'Accepted':len(records),role+'DistinctRecipes':len({r['id'] for r in records}),
            role+'Nominated':sum(r['nominated'] for r in records),role+'SelectedNovel':sum(r['selected'] for r in records),
            role+'SameRootHeldoutKnown':sum(r['heldoutWins'] is not None for r in records),
            role+'SameRootHeldoutUnknown':sum(r['heldoutWins'] is None for r in records)})
    creation = proposals['candidate']
    coverage = dict(acceptedByOwner=dict(Counter(str(r['changedOwners'][0]) for r in creation)),
        acceptedByDistance=dict(Counter(str(r['replacementDistance']) for r in creation)),
        acceptedByTargetPair=dict(Counter(r['affinityCreation']['pairId'] for r in creation)),
        selectedByDistance=dict(Counter(str(r['replacementDistance']) for r in creation if r['selected'])),
        uniqueSelectedRecipes=len({r['id'] for r in creation if r['selected']}),
        selectedRecipes=[r for r in creation if r['selected']])
    return dict(originalDecision=result['decision'],sourceFights=result['fights'],summary=dict(counts),coverage=coverage,
        benchmarkContributions={role:loss_decomposition(pairs,role) for role in ('control','candidate')},pairs=pairs,
        unknownOutcomes='Held-out wins are populated only for an exact recipe measured in the same root. Training scores, other roots, half panels and leave-one-out stability do not supply missing outcomes.')


def run():
    require(not OUT.exists(), 'Review output exists; no retry or overwrite')
    code_paths = [Path(__file__),Path(prior.__file__),Path(kernel.__file__),Path(audit.__file__),
        Path(__file__).with_name('test-affinity-creation-stage-review.py')]
    code = {p:p.read_bytes() for p in code_paths}; log = TEST_LOG.read_bytes()
    match = re.search(r'Ran (\d+) tests in ',log.decode('utf-8-sig'))
    require(match and int(match[1]) >= 10 and '\nOK' in log.decode('utf-8-sig'), 'Missing passing creation-review tests')
    started = time.monotonic(); OUT.mkdir()
    def save(name,value):
        raw = (json.dumps(value,indent=2,allow_nan=False)+'\n').encode()
        require(sum(p.stat().st_size for p in OUT.iterdir())+len(raw) < BYTES, 'Review storage exhausted')
        with (OUT/name).open('xb') as f: f.write(raw)
    sources = [(RUN,RUN_PIN),(ADMISSION,ADMISSION_PIN),(PUBLICATION,PUBLICATION_PIN)]
    save('declaration.json',dict(kind='ReadOnlyAffinityCreationStageReview',chargedSeconds=SECONDS,chargedBytes=BYTES,
        sourceManifests={str(p):pin for p,pin in sources},closeoutSha256=CLOSEOUT_PIN,
        implementation={str(p.relative_to(ROOT)):sha(raw) for p,raw in code.items()},testsSha256=sha(log),testsPassed=int(match[1]),
        newFights=0,newValues=0,scope='Full separate engineering allowance charged on success or failure. No scientific extension, fitting or promotion.'))
    timer = threading.Timer(SECONDS,lambda:os._exit(124)); timer.daemon=True; timer.start()
    try:
        scientific,admission,publication = [Evidence(p,pin) for p,pin in sources]
        receipt = publication.read('verification.json')
        require(receipt['status'] == 'VerifiedPublishedArchiveAndCompleteLiveHistory'
                and receipt['scientificManifestSha256'] == RUN_PIN and receipt['scientificCloseoutSha256'] == CLOSEOUT_PIN
                and receipt['admissionManifestSha256'] == ADMISSION_PIN and sha((RUN/'closeout.json').read_bytes()) == CLOSEOUT_PIN,
                'Changed publication binding')
        review = analyze(scientific,admission)
        for evidence in (scientific,admission,publication): evidence.recheck()
        require(sha((RUN/'closeout.json').read_bytes()) == CLOSEOUT_PIN and all(p.read_bytes() == raw for p,raw in code.items())
                and TEST_LOG.read_bytes() == log,'Review inputs changed')
        review.update(version='tower-affinity-creation-stage-review-v1',newFights=0,newValues=0,
            interpretation='RetrospectiveDevelopmentDiagnosisNoPolicyFittingOrPromotion',
            authentication='AllConsumedFilesAndSavedRacingStages;NotFullBattleOrLiveHistoryReaudit',
            sources={str(e.root):e.consumed for e in (scientific,admission,publication)},scientificCloseoutSha256=CLOSEOUT_PIN,
            reviewChargedSeconds=SECONDS,reviewChargedBytes=BYTES,
            totalRecordedChargedSeconds=receipt['totalRecordedChargedSeconds']+SECONDS,
            totalRecordedChargedBytes=receipt['totalRecordedChargedBytes']+BYTES,
            cumulativeDeclaredMaximumSeconds=receipt['cumulativeDeclaredMaximumSeconds']+SECONDS,
            cumulativeDeclaredMaximumBytes=receipt['cumulativeDeclaredMaximumBytes']+BYTES,
            lastVerifiedHistory=dict(values=receipt['liveValues'],files=receipt['historyFiles'],rescannedByThisReview=False),
            secondsBeforeSealing=time.monotonic()-started)
        save('review.json',review)
        for p,raw in code.items(): (OUT/p.name).write_bytes(raw)
        (OUT/'tests.log').write_bytes(log)
        save('files.json',{p.name:sha(p.read_bytes()) for p in sorted(OUT.iterdir())})
        require(time.monotonic()-started < SECONDS and sum(p.stat().st_size for p in OUT.iterdir()) < BYTES,'Review allowance exhausted')
        print(json.dumps(dict(status='Complete',seconds=time.monotonic()-started,retainedBytes=sum(p.stat().st_size for p in OUT.iterdir()),
            manifestSha256=sha((OUT/'files.json').read_bytes()),summary=review['summary'],benchmarkContributions=review['benchmarkContributions']),indent=2))
    except BaseException as error:
        save('failure.json',dict(reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES)); raise
    finally: timer.cancel()


if __name__ == '__main__': run()
