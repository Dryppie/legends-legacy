"""Bounded saved-stage review of the closed original/preserving creation pilot.

Authenticates consumed evidence and recounts both distinct search trajectories.
Uses the published audits for native RNG replay, raw battles and live history.
Never runs combat, fits a gate or supplies missing same-root held-out outcomes.
"""
from collections import Counter
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import re
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('preservation_review_audit', Path(__file__).with_name('audit-proposal-affinity-study.py'))
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)
require, digest = audit.require, audit.digest
RUN = ROOT/'TestResults/balance/tower-affinity-preservation-pilot-01-20260924'
RUN_PIN = '1c4fb870912fca7b168596cc86645cb54e96361ba8099eac1da5b4b8748d49e4'
CLOSEOUT_PIN = 'f9740a37d49210308261c841115661f774cfbf9e893e84398c138c74fb689d26'
ADMISSION = ROOT/'TestResults/affinity-preservation-admission-20260924'
ADMISSION_PIN = '607b32435968ca9ea96e35234b5246aa0a359ef9b178521d7719b2b57d6b1109'
PUBLICATION = ROOT/'TestResults/affinity-preservation-pilot-01-publication-verification-20260924'
PUBLICATION_PIN = 'd22377f904f4c287cd9a47364ce3843173cc805d16a16cbf102401c1c5027883'
PRIOR = ROOT/'TestResults/affinity-preservation-pilot-01-execution-review-20260924'
PRIOR_PIN = '2a10fcfa6ff3808a120b5729b616540fbb1e91f68ed7ceac27dc02ae1cd3ad39'
OUT = ROOT/'TestResults/affinity-preservation-stage-review-20260924'
TEST_LOG = ROOT/'TestResults/affinity-preservation-stage-review-tests-20260924.log'
STUDY, SELECTOR = audit.PRESERVATION_VERSION, audit.VALIDATION_POLICY
ROLES = audit.ROLES[:4]+['nomination', 'validation']
SECONDS, BYTES = 180, 64*1048576


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


class Evidence:
    def __init__(self, root, pin):
        self.root, self.consumed = root.resolve(), {'files.json': pin}
        raw = (self.root/'files.json').read_bytes()
        require(sha(raw) == pin, 'Changed external manifest pin')
        self.manifest = json.loads(raw)

    def bytes(self, name):
        path = (self.root/name).resolve()
        require(name in self.manifest and not Path(name).is_absolute() and '..' not in Path(name).parts
                and path.is_relative_to(self.root), 'Unbound evidence path')
        raw = path.read_bytes()
        require(sha(raw) == self.manifest[name], 'Changed consumed evidence: '+name)
        self.consumed[name] = self.manifest[name]
        return raw

    def read(self, name):
        return json.loads(self.bytes(name))

    def recheck(self):
        for name, pin in self.consumed.items():
            require(sha((self.root/name).read_bytes()) == pin, 'Evidence changed during review: '+name)


def category(pid, benchmark, references):
    return 'benchmark' if pid == benchmark else 'other-reference' if pid in references else 'novel'


def contrast(rows, pid, benchmark):
    if pid not in rows:
        return None
    a, b = rows[pid], rows[benchmark]
    require([o['seed'] for o in a] == [o['seed'] for o in b], 'Unpaired stage contrast')
    x, y = [[o['outcome'] == 'Victory' for o in values] for values in (a, b)]
    gains, losses = sum(v and not w for v, w in zip(x, y)), sum(w and not v for v, w in zip(x, y))
    return dict(samples=len(x), wins=sum(x), benchmarkWins=sum(y), gainedWins=gains, lostWins=losses, netWins=gains-losses)


def panel_rows(panel, planned, ids, parties, references, index, before, report_hash, scope_hash, version, context_id):
    f, observations, seeds = panel['freeze'], panel['observations'], planned['seeds']
    require(len(ids) == len(set(ids)) and type(panel['complete']) is bool and panel['complete']
            and f['version'] == version and f['planHash'] == report_hash and f['scopeHash'] == scope_hash
            and f['index'] == index and f['role'] == planned['role'] == ROLES[index]
            and f['context'] == context_id and f['seeds'] == seeds
            and f['parties'] == [parties[pid] for pid in ids] and f['evaluationsBefore'] == before
            and f['plannedEvaluations'] == len(observations) == len(ids)*len(seeds), 'Changed panel freeze')
    rows = {pid: [] for pid in ids}
    panel_hash = observations[0]['request']['panelHash']
    for offset, (pid, seed) in enumerate((p, s) for p in ids for s in seeds):
        request, outcome = observations[offset]['request'], observations[offset]['outcome']
        require(request['partyId'] == pid and request['seed'] == outcome['seed'] == seed
                and type(request['seed']) is int and type(outcome['seed']) is int
                and type(request['ordinal']) is int and request['ordinal'] == before+offset+1
                and request['scopeHash'] == scope_hash and request['panelHash'] == panel_hash
                and request['role'] == f['role'] and request['scenario']['seeds'] == seeds
                and outcome['outcome'] in ('Victory', 'Defeat', 'Draw')
                and all(type(outcome[k]) in (int, float) and math.isfinite(outcome[k]) and outcome[k] >= 0
                        for k in ('guardianHealth', 'survival', 'durationSeconds')), 'Changed observation')
        require({str(p['partySlot']): p['build']['essenceIds'] for p in request['scenario']['party']} == parties[pid]['builds'],
                'Changed scenario recipe')
        rows[pid].append(outcome)
    require(audit.close(panel['scores'], audit.score(ids, observations)), 'Changed panel score')
    contrasts = []
    for pid in ids:
        for ref in references:
            if ref in ids and ref != pid:
                c = contrast(rows, pid, ref)
                contrasts.append(dict(partyId=pid, referenceId=ref, **{k: c[k] for k in ('samples', 'gainedWins', 'lostWins')}))
    require(panel['contrasts'] == contrasts, 'Changed panel contrasts')
    return rows


def route_metadata(proposal, parent, policy, affinities):
    audit.creation_step(proposal, parent, policy)
    if proposal['party'] is None:
        return None
    selected = policy['createdDamageAffinityIds']
    by_id = {a['id']: a for a in affinities}
    require(len(by_id) == len(affinities) and set(selected) <= by_id.keys(), 'Unresolved selected affinity')
    pairs = {aid: {by_id[aid]['producerEssenceId'], by_id[aid]['modifierEssenceId']} for aid in selected}
    require(all(len(pair) == 2 for pair in pairs.values()), 'Invalid affinity endpoints')
    step, owner = proposal['affinityCreation'], str(proposal['scheduledOwners'][0])
    old, new = set(parent['builds'][owner]), set(proposal['party']['builds'][owner])
    target = sorted(aid for aid, pair in pairs.items() if digest(sorted(pair)) == step['pairId'])
    active = sorted(aid for aid, pair in pairs.items() if pair <= new and not pair <= old)
    possible = old | set(step['added'])
    protect = sorted({e for pair in pairs.values() if pair <= possible for e in pair if e in old})
    require(step['targetAffinityIds'] == target and step['newlyActivatedAffinityIds'] == active, 'Changed authored route metadata')
    if policy['version'] == audit.PRESERVATION_POLICY:
        require(step['removalSelection']['protectedEssences'] == protect, 'Changed derived endpoint protection')
    return dict(protectedIfPreserving=protect, removedCompletableEndpoints=sorted(set(protect) & set(step['removed'])),
                targetAffinityIds=target, newlyActivatedAffinityIds=active)


def check_seed_partition(plan, heldout):
    require([p['role'] for p in plan['panels']] == ROLES and [len(p['seeds']) for p in plan['panels']] == [8, 8, 8, 8, 16, 60],
            'Changed panel allocation')
    training = [plan['rootSeed']]+[s for p in plan['panels'] for s in p['seeds']]
    require(len(training) == len(set(training)) == 109 and all(type(s) is int for s in training)
            and len(heldout) == len(set(heldout)) == 256 and all(type(s) is int for s in heldout)
            and not set(training) & set(heldout), 'Reused training or held-out values')
    return training


def arm_review(report, plan, context, policy, affinities, heldout, heldout_seeds, selected, role):
    require(role in ('control', 'candidate'), 'Unknown arm')
    version = audit.VALIDATION_RACING if role == 'control' else audit.PRESERVATION_RACING
    e, p = report['evaluation'], plan['racing']
    require(plan['version'] == report['version'] == e['version'] == version and plan['policy'] == policy
            and plan['selectionPolicyVersion'] == report['selectionPolicyVersion'] == SELECTOR
            and report['policyHash'] == digest(policy) and plan['damageAffinityInventory'] == context['damageAffinityInventory']
            and p['mechanics'] == context['mechanics'] and p['scope']['starts'] == context['scope']['starts']
            and report['planHash'] == e['planHash'] and e['status'] == 'Complete' and e['error'] is None
            and e['plannedEvaluations'] == e['chargedEvaluations'] == p['maximumEvaluations'] == 528
            and len(e['panels']) == 6 and len(e['decisions']) == len(report['batches']) == 2, 'Changed arm contract')
    check_seed_partition(p, heldout_seeds)
    scope = p['scope']
    require(len(scope['contexts']) == 1, 'Changed context count')
    parties = {s['party']['id']: s['party'] for s in scope['starts']}
    references = list(parties)
    require(len(references) == 3, 'Changed reference count')
    benchmark = next(s['party']['id'] for s in scope['starts'] if s['referenceId'] == p['benchmarkReferenceId'])
    primary = next(s['party']['id'] for s in scope['starts'] if s['referenceId'] == scope['stages']['selectionPrimaryReferenceId'])
    allowed = {item['id']: item['family'].casefold() for item in scope['allowedEssences']}
    beam, records, stages, before, attempts, rejections = [], [], [], 0, 0, Counter()
    scope_hash = e['panels'][0]['freeze']['scopeHash']
    for i, panel in enumerate(e['panels']):
        if i in (0, 2):
            batch = report['batches'][i//2]
            require(batch['wave'] == i//2+1 and batch['afterEvaluations'] == before and batch['beamIds'] == beam
                    and len(batch['seenBefore']) == len(parties) and set(batch['seenBefore']) == set(parties)
                    and batch['feedbackPanels'] == [x['observations'][0]['request']['panelHash'] for x in e['panels'][:i]]
                    and len(batch['candidates']) == (9 if i == 0 else 8) and len(batch['proposals']) <= 128, 'Changed batch feedback')
            accepted, seen = [], set(parties)
            for attempt, proposal in enumerate(batch['proposals'], 1):
                attempts += 1
                require(proposal['attempt'] == attempt and proposal['requestedOperator'] == proposal['effectiveOperator'] == 'affinity-create'
                        and proposal['parents'] == [benchmark] and proposal['parentSource'] == 'benchmark', 'Changed proposal lineage')
                metadata = route_metadata(proposal, parties[benchmark], policy, affinities)
                party = proposal['party']
                if party is not None:
                    pid = party['id']
                    require(pid == digest(party['builds']) and party['source'] == policy['version']
                            and proposal['rejection'] == ('duplicate-recipe' if pid in seen else None), 'Changed duplicate handling')
                    seen.add(pid)
                    for ids in party['builds'].values():
                        require(ids == sorted(set(ids)) and len(ids) == scope['budget']['essenceSlots']
                                and all(v in allowed for v in ids) and len({allowed[v] for v in ids}) == len(ids), 'Illegal proposal')
                    if scope['ownedCopies'] is not None:
                        copies = Counter(v for ids in party['builds'].values() for v in ids)
                        require(all(n <= scope['ownedCopies'].get(v, 0) for v, n in copies.items()), 'Owned-copy bound exceeded')
                if proposal['rejection'] is not None:
                    rejections[proposal['rejection']] += 1
                else:
                    require(party is not None, 'Accepted empty proposal')
                    accepted.append(party)
                    records.append(dict(id=pid, wave=i//2+1, position=len(accepted), attempt=attempt,
                        changedOwners=proposal['changedOwners'], replacementDistance=proposal['replacementDistance'],
                        edit=proposal['affinityCreation'], **metadata, heldoutWins=heldout.get(pid)))
            require(accepted == batch['candidates'], 'Changed accepted batch')
            parties.update((party['id'], party) for party in accepted)
            ids = references+beam+[party['id'] for party in accepted]
        elif i in (1, 3):
            ids = references+e['decisions'][i//2]['survivorIds']
        elif i == 4:
            eligible = set(references) | set(beam[:2])
            ids = [s['id'] for s in audit.rank(e['decisions'][1]['commonScores']) if s['id'] in eligible]
            require(e['nominees'] == ids, 'Changed nominees')
        else:
            nonbenchmark = [s for s in e['panels'][4]['scores'] if s['id'] != benchmark]
            challenger = audit.select(nonbenchmark, [pid for pid in e['nominees'] if pid != benchmark], primary)
            frozen = dict(version=SELECTOR, planHash=report['planHash'],
                nominationPanelHash=e['panels'][4]['observations'][0]['request']['panelHash'], challengerId=challenger, benchmarkId=benchmark)
            require(e['validationFreeze'] == frozen, 'Changed frozen challenger')
            ids = [challenger, benchmark]
        rows = panel_rows(panel, p['panels'][i], ids, parties, references, i, before, report['planHash'], scope_hash, version, scope['contexts'][0]['id'])
        stages.append(rows)
        if i in (0, 2):
            d = e['decisions'][i//2]
            ranked = audit.rank([s for s in panel['scores'] if s['id'] not in references])
            elite, cutoff = [s['id'] for s in ranked[:3]], ranked[2]['wins']-1
            def distance(pid):
                return min(sum(len(set(parties[pid]['builds'][o])-set(parties[x]['builds'][o])) for o in parties[pid]['builds']) for x in elite)
            eligible = [s for s in ranked[3:] if s['wins'] >= cutoff]
            diverse = max(eligible, key=lambda s: distance(s['id'])) if eligible else ranked[3]
            survivors = elite+[diverse['id']]
            require(d['wave'] == i//2+1 and d['eliteIds'] == elite and d['survivorIds'] == survivors
                    and d['competitiveCutoffWins'] == cutoff and d['diversityId'] == diverse['id']
                    and d['diversityFallback'] == (not eligible) and d['diversityDistance'] == distance(diverse['id'])
                    and d['prunedIds'] == [s['id'] for s in ranked if s['id'] not in survivors], 'Changed pruning decision')
        elif i in (1, 3):
            common = audit.score(ids, e['panels'][i-1]['observations']+panel['observations'])
            beam = [s['id'] for s in audit.rank(common) if s['id'] not in references]
            require(audit.close(e['decisions'][i//2]['commonScores'], common) and e['decisions'][i//2]['beamIds'] == beam,
                    'Changed continuation beam')
        before += len(panel['observations'])
    gate = audit.validation_decision(frozen, e['panels'][5])
    require(all(type(e['validationDecision'][k]) is int for k in ('samples', 'gainedWins', 'lostWins', 'tailNumerator', 'tailDenominator'))
            and type(e['validationDecision']['passed']) is bool and e['validationDecision'] == gate
            and selected == gate['selectedId'] == e['rawSelectedId'] and before == 528, 'Changed exact gate or output')
    for record in records:
        pid = record['id']
        record.update(nominated=pid in e['nominees'], validationChallenger=pid == challenger, selected=pid == selected,
                      stages={role: contrast(rows, pid, benchmark) for role, rows in zip(ROLES, stages)})
    nomination = contrast(stages[4], challenger, benchmark)
    leaders = [s['id'] for s in nonbenchmark if s['wins'] == nomination['wins']]
    reason = ('ZeroWinHealthFallback' if nomination['wins'] == 0 else 'UniqueNonbenchmarkMaximum' if len(leaders) == 1
              else 'PrimaryPositiveTie' if challenger == primary else 'FrozenOrderPositiveTie')
    return dict(records=records, attempts=attempts, rejections=dict(rejections), beamIds=beam,
        survivors=[d['survivorIds'] for d in e['decisions']], pruned=[d['prunedIds'] for d in e['decisions']],
        nominees=[dict(id=pid, category=category(pid, benchmark, references), heldoutWins=heldout.get(pid),
            stages={role: contrast(rows, pid, benchmark) for role, rows in zip(ROLES, stages)}) for pid in e['nominees']],
        challenger=challenger, challengerCategory=category(challenger, benchmark, references), nominationReason=reason,
        nomination=nomination, validation=contrast(stages[5], challenger, benchmark), validationDecision=gate,
        challengerHeldoutWins=heldout.get(challenger), selected=selected, selectedCategory=category(selected, benchmark, references))


def heldout_map(endpoint, family):
    values = {}
    require(len(family['seeds']) == len(set(family['seeds'])) == 256, 'Changed held-out sample count')
    for role in ('control', 'candidate', 'benchmark'):
        pid, wins = endpoint[role+'Party'], endpoint[role+'Wins']
        members = [m for m in family['members'] if role in m['roles']]
        require(len(members) == 1 and members[0]['party']['id'] == pid and type(wins) is int and 0 <= wins <= 256
                and (pid not in values or values[pid] == wins), 'Changed frozen held-out role or shared outcome')
        values[pid] = wins
    require(len(family['members']) == len(values), 'Extra held-out member')
    return values


def summarize(arms):
    records = [r for a in arms for r in a['records']]
    signs = Counter('positive' if a['validation']['netWins'] > 0 else 'negative' if a['validation']['netWins'] < 0 else 'zero' for a in arms)
    return dict(accepted=len(records), distinctRecipes=len({r['id'] for r in records}), attempts=sum(a['attempts'] for a in arms),
        rejections=dict(sum((Counter(a['rejections']) for a in arms), Counter())),
        removedCompletableEndpointOccurrences=sum(bool(r['removedCompletableEndpoints']) for r in records),
        newlyActiveRouteOccurrences=sum(len(r['newlyActivatedAffinityIds']) for r in records),
        novelNominees=sum(r['nominated'] for r in records), novelChallengers=sum(a['challengerCategory'] == 'novel' for a in arms),
        gatePasses=sum(a['validationDecision']['passed'] for a in arms), validationSigns=dict(signs),
        validationGains=sum(a['validation']['gainedWins'] for a in arms), validationLosses=sum(a['validation']['lostWins'] for a in arms),
        challengerCategories=dict(Counter(a['challengerCategory'] for a in arms)), nominationReasons=dict(Counter(a['nominationReason'] for a in arms)),
        selectedCategories=dict(Counter(a['selectedCategory'] for a in arms)),
        generatedHeldoutKnown=sum(r['heldoutWins'] is not None for r in records),
        challengerHeldoutKnown=sum(a['challengerHeldoutWins'] is not None for a in arms))


def analyze(scientific, admission):
    result, design, context = [scientific.read(name) for name in ('result.json', 'source/plan.json', 'source/context.json')]
    require(result == scientific.read('independent-audit.json')['result'] and result['version'] == design['version'] == STUDY
            and result['status'] == 'Verified' and result['decision'] == 'NoObservedOutputDifferentiation'
            and design['roots'] == len(result['roots']) == 12 and design['heldoutSamples'] == 256, 'Changed completed study')
    audit.validate_policies(design)
    freeze = scientific.read('study/freeze.json')
    require(freeze['version'] == STUDY and freeze['planHash'] == result['planHash'] and len(freeze['families']) == 12, 'Changed global freeze')
    affinities = admission.read('preserving-batches.json')['damageSourceAffinities']['affinities']
    for name in ('TowerAffinityCreation.cs', 'TowerAdaptiveRacingGenerator.cs', 'TowerProposalPolicies.cs', 'TowerBatchRacing.cs',
                 'TowerBenchmarkValidation.cs', 'TowerAffinityPreservationComparison.cs'):
        admission.bytes('source/LL/tools/BalanceHarness/'+name)
    pairs, used_seeds = [], set()
    for endpoint, family in zip(result['roots'], freeze['families']):
        number = endpoint['root']
        require(number == family['root'] == len(pairs)+1, 'Reordered roots')
        pair = scientific.read(f'study/pair-{number:02d}.json')
        require(pair['status'] == 'Complete' and pair['planHash'] == result['planHash'], 'Changed pair binding')
        plans = {role: scientific.read(f'search/root-{number:02d}/{role}/racing/plan.json') for role in ('control', 'candidate')}
        require(plans['control']['racing'] == plans['candidate']['racing'], 'Changed paired plan')
        seeds = check_seed_partition(plans['control']['racing'], family['seeds'])+family['seeds']
        require(not used_seeds.intersection(seeds), 'Cross-root seed reuse')
        used_seeds.update(seeds)
        heldout = heldout_map(endpoint, family)
        audit.preservation_trajectories(pair['control'], pair['candidate'])
        reviewed = {}
        for role in ('control', 'candidate'):
            reviewed[role] = arm_review(pair[role], plans[role], context, design[role], affinities, heldout,
                                       family['seeds'], endpoint[role+'Party'], role)
            e = pair[role]['evaluation']
            saved = [(f'panel-{i+1:02d}.json', p['freeze']) for i, p in enumerate(e['panels'])]
            saved += [(f'batch-{i+1:02d}.json', b) for i, b in enumerate(pair[role]['batches'])]
            saved += [('validation-freeze.json', e['validationFreeze']), ('validation-decision.json', e['validationDecision'])]
            for name, expected in saved:
                require(scientific.read(f'search/root-{number:02d}/{role}/racing/{name}') == expected, 'Changed durable stage record')
            decisions = result['controlValidation' if role == 'control' else 'validation']['decisions']
            require(decisions[number-1] == reviewed[role]['validationDecision'], 'Changed published gate')
        changes = [sum(x['id'] != y['id'] for x, y in zip(a['candidates'], b['candidates']))
                   for a, b in zip(pair['control']['batches'], pair['candidate']['batches'])]
        require(changes == endpoint['changedPositionsPerWave'] and endpoint['identical'] == (endpoint['controlParty'] == endpoint['candidateParty']),
                'Changed endpoint divergence')
        overlap = {}
        for name, get in [('generated', lambda a: [r['id'] for r in a['records']]), ('finalBeam', lambda a: a['beamIds']),
                          ('nominees', lambda a: [r['id'] for r in a['nominees']])]:
            a, b = (set(get(reviewed[role])) for role in ('control', 'candidate'))
            overlap[name] = dict(shared=sorted(a & b), controlOnly=sorted(a-b), candidateOnly=sorted(b-a))
        pairs.append(dict(root=number, changedPositions=changes, overlap=overlap,
            sameChallenger=reviewed['control']['challenger'] == reviewed['candidate']['challenger'], heldout=endpoint, **reviewed))
    arm_summaries = {role: summarize([p[role] for p in pairs]) for role in ('control', 'candidate')}
    require(len(used_seeds) == 4380 and sum(a['gatePasses'] for a in arm_summaries.values()) == 0
            and result['differingRoots'] == result['novelRoots'] == 0
            and all(a['selectedCategories'] == {'benchmark': 12} for a in arm_summaries.values())
            and result['method'] == result['benchmark'] == dict(mean=0, standardDeviation=0, lower=0, upper=0), 'Changed all-root endpoint')
    summary = dict(changedPositions=sum(sum(p['changedPositions']) for p in pairs),
        differentNomineeSets=sum(bool(p['overlap']['nominees']['controlOnly']) for p in pairs),
        differentFinalBeams=sum(bool(p['overlap']['finalBeam']['controlOnly']) for p in pairs),
        differentChallengers=sum(not p['sameChallenger'] for p in pairs), differentOutputs=result['differingRoots'],
        sharedGeneratedOccurrences=sum(len(p['overlap']['generated']['shared']) for p in pairs),
        armExclusiveGeneratedOccurrencesEach=sum(len(p['overlap']['generated']['controlOnly']) for p in pairs))
    return dict(originalDecision=result['decision'], sourceFights=result['fights'], summary=summary, armSummaries=arm_summaries, pairs=pairs,
        unknownOutcomes='Only exact physical recipes measured within the same root receive held-out wins. No cross-root borrowing, imputation, threshold sweep or replacement-policy scoring.',
        interpretation='RetrospectiveDevelopmentDiagnosisNoPolicyFittingOrPromotion')


def run():
    require(not OUT.exists(), 'Review output exists; no retry or overwrite')
    code = {p: p.read_bytes() for p in (Path(__file__), Path(audit.__file__), Path(__file__).with_name('test-affinity-preservation-stage-review.py'))}
    log = TEST_LOG.read_bytes()
    match = re.search(r'Ran (\d+) tests in ', log.decode('utf-8-sig'))
    require(match and int(match[1]) >= 15 and '\nOK' in log.decode('utf-8-sig'), 'Missing passing stage-review tests')
    sources = [(RUN, RUN_PIN), (ADMISSION, ADMISSION_PIN), (PUBLICATION, PUBLICATION_PIN)]
    started = time.monotonic()
    OUT.mkdir()
    def save(name, value):
        raw = (json.dumps(value, indent=2, allow_nan=False)+'\n').encode()
        require(sum(p.stat().st_size for p in OUT.iterdir())+len(raw) < BYTES, 'Review storage exhausted')
        with (OUT/name).open('xb') as stream:
            stream.write(raw)
    save('declaration.json', dict(kind='ReadOnlyAffinityPreservationStageReview', chargedSeconds=SECONDS, chargedBytes=BYTES,
        sourceManifests={str(p): pin for p, pin in sources}, closeoutSha256=CLOSEOUT_PIN, priorHandoffManifestSha256=PRIOR_PIN,
        implementation={str(p.relative_to(ROOT)): sha(raw) for p, raw in code.items()}, testsSha256=sha(log), testsPassed=int(match[1]),
        newFights=0, newValues=0, scope='Full separate engineering allowance charged on success or failure; no scientific extension, policy fitting or promotion.'))
    timer = threading.Timer(SECONDS, lambda: os._exit(124))
    timer.daemon = True
    timer.start()
    try:
        require(audit.sha(PRIOR/'files.json') == PRIOR_PIN, 'Changed prior handoff manifest')
        prior_files = audit.read(PRIOR/'files.json')
        for name, pin in prior_files.items():
            require((ROOT/name).resolve().is_relative_to(ROOT) and audit.sha(ROOT/name) == pin, 'Changed prior handoff member')
        prior = audit.read(PRIOR/'summary.json')
        scientific, admission, publication = [Evidence(p, pin) for p, pin in sources]
        receipt = publication.read('verification.json')
        require(receipt['status'] == 'VerifiedPublishedArchiveAndCompleteLiveHistory'
                and receipt['scientificManifestSha256'] == RUN_PIN and receipt['scientificCloseoutSha256'] == CLOSEOUT_PIN
                and receipt['admissionManifestSha256'] == ADMISSION_PIN and audit.sha(RUN/'closeout.json') == CLOSEOUT_PIN,
                'Changed publication binding')
        review = analyze(scientific, admission)
        for evidence in (scientific, admission, publication):
            evidence.recheck()
        require(audit.sha(RUN/'closeout.json') == CLOSEOUT_PIN and all(p.read_bytes() == raw for p, raw in code.items())
                and TEST_LOG.read_bytes() == log and all(audit.sha(ROOT/name) == pin for name, pin in prior_files.items()), 'Review inputs changed')
        review.update(version='tower-affinity-preservation-stage-review-v1', newFights=0, newValues=0,
            authentication='ConsumedFilesAndSavedRacingStages;NotFullBattleOrLiveHistoryReaudit',
            sources={str(e.root): e.consumed for e in (scientific, admission, publication)}, scientificCloseoutSha256=CLOSEOUT_PIN,
            priorHandoffManifestSha256=PRIOR_PIN, priorHandoffMembers=len(prior_files),
            reviewChargedSeconds=SECONDS, reviewChargedBytes=BYTES,
            cumulativeRecordedSeconds=prior['cumulativeRecordedSeconds']+SECONDS,
            cumulativeRecordedBytes=prior['cumulativeRecordedBytes']+BYTES,
            cumulativeDeclaredMaximumSeconds=prior['cumulativeDeclaredMaximumSeconds']+SECONDS,
            cumulativeDeclaredMaximumBytes=prior['cumulativeDeclaredMaximumBytes']+BYTES,
            lastVerifiedHistory=dict(values=receipt['liveValues'], files=receipt['historyFiles'], rescannedByThisReview=False),
            secondsBeforeSealing=time.monotonic()-started)
        save('review.json', review)
        for p, raw in code.items():
            (OUT/p.name).write_bytes(raw)
        (OUT/'tests.log').write_bytes(log)
        save('files.json', {p.name: sha(p.read_bytes()) for p in sorted(OUT.iterdir())})
        elapsed, retained = time.monotonic()-started, sum(p.stat().st_size for p in OUT.iterdir())
        require(elapsed < SECONDS and retained < BYTES, 'Review allowance exhausted')
        print(json.dumps(dict(status='Complete', seconds=elapsed, retainedBytes=retained,
            manifestSha256=sha((OUT/'files.json').read_bytes()), summary=review['summary'], armSummaries=review['armSummaries']), indent=2))
    except BaseException as error:
        save('failure.json', dict(reason=str(error), chargedSeconds=SECONDS, chargedBytes=BYTES))
        raise
    finally:
        timer.cancel()


if __name__ == '__main__':
    run()
