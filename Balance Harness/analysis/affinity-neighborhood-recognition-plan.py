"""Freeze an exhaustive physical-recipe diagnostic; no runtime, entropy or combat.

The outcome functions are a pure reference specification for later native and
independent implementations. They never read historical outcomes or run battles.
"""
import argparse
from collections import Counter
import copy
from fractions import Fraction
import hashlib
import importlib.util
from itertools import combinations
import json
import math
import os
from pathlib import Path
import re
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-affinity-neighborhood-recognition-plan-v1'
OUTCOME_VERSION = 'tower-affinity-neighborhood-recognition-outcome-v1'
CONTROL, CANDIDATE = 'tower-proposal-policy-v4', 'tower-proposal-policy-v5'
SAMPLES, ALPHA, MINIMUM_GAIN = 2048, Fraction(1, 20), Fraction(3, 100)
SECONDS, BYTES = 180, 64*1048576
PACKAGES = {
    'preview': ('TestResults/affinity-allied-action-preview-20260924-v2', '8301070ad78ab8ea84875faf8092c7ae5da193f1d0106245e417b70f689231b1'),
    'scientific': ('TestResults/balance/tower-affinity-allied-action-pilot-01-20260924', 'c9d191a8f81439d6bfa947e8c62bd74a6e35352ee5d999b8dcd9ecdfdded54ef'),
    'review': ('TestResults/affinity-allied-action-stage-review-20260924', '84226a6dd0eb2087ac8df8a37fc0e90c5c4cfb990d28547ee48bdf0fa6900f1d')}
PRIOR = 'TestResults/affinity-allied-action-stage-review-handoff-20260924.json'
PRIOR_PIN = '1e1591ba18a846255ce4cd2128838d41c1fd4a7bdcb00456a8cc0fc230c59924'
PLAN = ROOT/'Balance Harness/Tower-Affinity-Neighborhood-Recognition-Plan.json'


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def digest(value):
    return sha(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True, allow_nan=False).encode())


def unique(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'Duplicate JSON property')
        result[key] = value
    return result


def decode(raw):
    def nonfinite(_):
        raise ValueError('Nonfinite JSON number')
    return json.loads(raw, object_pairs_hook=unique, parse_constant=nonfinite)


def save(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, ensure_ascii=False, allow_nan=False)
        stream.write('\n')


class Evidence:
    def __init__(self, root, pin):
        self.root = root.resolve()
        raw = (root/'files.json').read_bytes()
        require(sha(raw) == pin, 'Changed external manifest pin')
        self.manifest, self.consumed = decode(raw), {'files.json': pin}

    def bytes(self, name):
        path = self.root/name
        require(not Path(name).is_absolute() and '..' not in Path(name).parts and name in self.manifest
                and path.resolve().is_relative_to(self.root) and not path.is_symlink(), 'Unbound evidence member')
        raw = path.read_bytes()
        require(sha(raw) == self.manifest[name], 'Changed consumed evidence')
        self.consumed[name] = self.manifest[name]
        return raw

    def read(self, name):
        return decode(self.bytes(name))

    def recheck(self):
        require(all(sha((self.root/name).read_bytes()) == pin for name, pin in self.consumed.items()), 'Evidence changed during planning')


def validate_builds(builds, scope):
    allowed = {e['id']: e['family'].casefold() for e in scope['allowedEssences']}
    require(len(allowed) == len(scope['allowedEssences']) and set(builds) == {str(i) for i in range(1, scope['requiredPartySize']+1)}, 'Changed owner/family catalogue')
    slots = scope['budget']['essenceSlots']
    for ids in builds.values():
        require(ids == sorted(set(ids)) and len(ids) == slots and all(e in allowed for e in ids)
                and len({allowed[e] for e in ids}) == slots, 'Illegal or noncanonical recipe')
    copies = Counter(e for ids in builds.values() for e in ids)
    require(scope['ownedCopies'] is None or all(n <= scope['ownedCopies'].get(e, 0) for e, n in copies.items()), 'Owned-copy limit exceeded')


def derive_pairs(policy, affinities):
    by_id = {a['id']: a for a in affinities}
    selected = policy['createdDamageAffinityIds']
    require(len(by_id) == len(affinities) and selected == sorted(set(selected)) and selected and set(selected) <= by_id.keys(), 'Unresolved selected affinity')
    pairs = {}
    for aid in selected:
        a = by_id[aid]
        ids = sorted({a['producerEssenceId'], a['modifierEssenceId']})
        require(len(ids) == 2, 'Invalid affinity endpoint pair')
        pid = digest(ids)
        pairs.setdefault(pid, dict(id=pid, essenceIds=ids, affinityIds=[]))['affinityIds'].append(aid)
    return [pairs[pid] for pid in sorted(pairs)]


def enumerate_neighborhood(context, parent, policy, affinities, provider_ids):
    """Reconstruct every owner/pair opportunity, including active and empty ones."""
    scope = context['scope']
    require(policy['version'] in (CONTROL, CANDIDATE) and policy['parentTickets'] == ['benchmark'], 'Unknown neighborhood policy')
    validate_builds(parent['builds'], scope)
    require(parent['id'] == digest(parent['builds']), 'Changed benchmark identity')
    pairs = derive_pairs(policy, affinities)
    require(all(e in {a['id'] for a in scope['allowedEssences']} for p in pairs for e in p['essenceIds']), 'Unavailable affinity endpoint')
    rows, recipes = [], {}
    for owner in sorted(parent['builds'], key=int):
        old = set(parent['builds'][owner])
        for pair in pairs:
            target = set(pair['essenceIds'])
            missing = target-old
            possible = old | missing
            protected = old & {e for p in pairs if set(p['essenceIds']) <= possible for e in p['essenceIds']}
            if policy['version'] == CANDIDATE:
                protected |= old & set(provider_ids)
            edits = []
            if missing:
                for removed in combinations(sorted(old-protected-target), len(missing)):
                    builds = copy.deepcopy(parent['builds'])
                    builds[owner] = sorted((old-set(removed)) | missing)
                    try:
                        validate_builds(builds, scope)
                    except ValueError:
                        continue
                    edit = dict(removed=list(removed), added=sorted(missing))
                    edits.append(edit)
                    pid = digest(builds)
                    row = recipes.setdefault(pid, dict(partyId=pid, builds=builds, derivations=[]))
                    require(row['builds'] == builds, 'Recipe identity collision')
                    row['derivations'].append(dict(owner=int(owner), pairId=pair['id'], **edit))
            rows.append(dict(parentId=parent['id'], owner=int(owner), pairId=pair['id'], alreadyActive=not missing,
                missingEssences=sorted(missing), protectedEssences=sorted(protected), legalEdits=edits))
    return dict(pairs=pairs, opportunities=rows, recipes={pid:recipes[pid] for pid in sorted(recipes)})


def verify_coverage(expected, coverage, providers, candidate):
    require(expected['pairs'] == coverage['pairs'], 'Changed authored pairs')
    rows = coverage['opportunities']
    require(len(rows) == len(expected['opportunities']), 'Missing opportunity')
    seen = set()
    for a, b in zip(expected['opportunities'], rows):
        key = (b['owner'], b['pairId'])
        require(key not in seen and {k:b.get(k) for k in a} == a, 'Changed complete legal neighborhood')
        seen.add(key)
        if candidate:
            expected_reasons = [p for p in providers if p['essenceId'] in a['protectedEssences']]
            require(b['alliedActionProtections'] == expected_reasons, 'Changed provider protection provenance')
    require(coverage['activePairOwners'] == sum(r['alreadyActive'] for r in rows)
            and coverage['eligiblePairOwners'] == sum(bool(r['legalEdits']) for r in rows), 'Changed coverage totals')


def scenario_for(anchor, builds):
    scenario = copy.deepcopy(anchor)
    scenario['seeds'] = []
    for actor in scenario['party']:
        actor['build']['essenceIds'] = copy.deepcopy(builds[str(actor['partySlot'])])
    return scenario


def catalogue(context, parent, policies, affinities, coverages, providers, anchor):
    scope = context['scope']
    require(scope['ownedCopies'] is None and len(scope['contexts']) == 1 and scope['requiredPartySize'] == 10, 'Changed fixed context')
    expected_candidate = dict(policies[0], version=CANDIDATE, name='benchmark-allied-action-affinity-creation-v5',
        creationRemovalRule='preserve-completable-affinity-endpoints-and-allied-basic-attack-providers-v1')
    require(policies[0]['version'] == CONTROL and policies[0]['creationRemovalRule'] == 'preserve-completable-affinity-endpoints-v1'
            and policies[1] == expected_candidate, 'Changed frozen proposer contract')
    provider_ids = {p['essenceId'] for p in providers}
    neighborhoods = {}
    for policy in policies:
        version = policy['version']
        neighborhood = enumerate_neighborhood(context, parent, policy, affinities, provider_ids)
        verify_coverage(neighborhood, coverages[version], providers, version == CANDIDATE)
        neighborhoods[version] = neighborhood
    old, new = (neighborhoods[v]['recipes'] for v in (CONTROL, CANDIDATE))
    require(len(old) == 41 and len(new) == 26 and set(new) < set(old), 'Changed exhaustive population or subset')
    require(sum(len(x['derivations']) for x in old.values()) == 62
            and sum(len(x['derivations']) for x in new.values()) == 40, 'Changed pair/edit exposure counts')
    references = scope['starts']
    require(len(references) == len({s['referenceId'] for s in references}) == 3, 'Changed references')
    ids = [s['party']['id'] for s in references]
    require(len(set(ids)) == 3 and not set(ids) & set(old) and parent['id'] in ids, 'Duplicate physical reference')
    rows = []
    for pid in ids+sorted(old):
        reference = next((s for s in references if s['party']['id'] == pid), None)
        builds = reference['party']['builds'] if reference else old[pid]['builds']
        validate_builds(builds, scope)
        require(digest(builds) == pid, 'Changed physical recipe identity')
        scenario = scenario_for(anchor, builds)
        rows.append(dict(partyId=pid, referenceId=reference['referenceId'] if reference else None,
            membership='reference' if reference else 'shared' if pid in new else 'control-only',
            scenario=scenario, seedFreeScenarioHash=digest(scenario),
            derivations={v:neighborhoods[v]['recipes'][pid]['derivations'] if pid in neighborhoods[v]['recipes'] else []
                         for v in (CONTROL, CANDIDATE)}, inclusionProbability=dict(numerator=1, denominator=1)))
    return rows, neighborhoods


def coefficient_sets(teams, benchmark):
    ids = [t['partyId'] for t in teams]
    old = [t['partyId'] for t in teams if t['membership'] != 'reference']
    new = [t['partyId'] for t in teams if t['membership'] == 'shared']
    require(len(ids) == len(set(ids)) == 44 and len(old) == 41 and len(new) == 26
            and benchmark in {t['partyId'] for t in teams if t['membership']=='reference'}
            and all(t['membership'] in ('reference','shared','control-only') for t in teams), 'Changed statistical family')
    result = {pid:{pid:Fraction(1), benchmark:Fraction(-1)} for pid in ids if pid != benchmark}
    for label, members in [('controlMean', old), ('candidateMean', new)]:
        result[label] = {pid:Fraction(1,len(members)) for pid in members}
        result[label][benchmark] = Fraction(-1)
    result['candidateMinusControlMean'] = {pid:Fraction(pid in new,len(new))-Fraction(1,len(old)) for pid in old}
    return result


def endpoint_contract(teams, benchmark):
    coefficients = coefficient_sets(teams, benchmark)
    rows = []
    for key, values in coefficients.items():
        lower, upper = sum(v for v in values.values() if v < 0), sum(v for v in values.values() if v > 0)
        radius = float(upper-lower)*math.sqrt(math.log(2*len(coefficients)/float(ALPHA))/(2*SAMPLES))
        rows.append(dict(id=key, coefficients={pid:dict(numerator=v.numerator,denominator=v.denominator) for pid,v in values.items()},
            minimum=float(lower), maximum=float(upper), simultaneousHalfWidth=radius))
    return rows


def outcome_summary(plan, outcomes):
    """Pure future-outcome oracle; strict complete rectangular paired panel."""
    require(plan['version'] == VERSION and plan['samplesPerTeam'] == SAMPLES
            and plan['decision']['version'] == OUTCOME_VERSION
            and plan['decision']['minimumPracticalGain'] == dict(numerator=3,denominator=100)
            and plan['statisticalEndpoints'] == endpoint_contract(plan['teams'],plan['benchmarkPartyId']), 'Changed outcome contract')
    expected = {t['partyId'] for t in plan['teams']}
    require(set(outcomes) == expected and all(len(row) == SAMPLES and all(type(x) is bool for x in row) for row in outcomes.values()), 'Incomplete or nonbinary outcome panel')
    wins = {pid:sum(row) for pid,row in outcomes.items()}
    endpoints = []
    for spec in plan['statisticalEndpoints']:
        estimate = float(sum(Fraction(v['numerator'],v['denominator'])*wins[pid] for pid,v in spec['coefficients'].items())/SAMPLES)
        radius = spec['simultaneousHalfWidth']
        endpoints.append(dict(id=spec['id'],mean=estimate,lower=max(spec['minimum'],estimate-radius),upper=min(spec['maximum'],estimate+radius)))
    generated = {t['partyId'] for t in plan['teams'] if t['membership'] != 'reference'}
    by_id = {e['id']:e for e in endpoints}
    eligible = sorted(pid for pid in generated if by_id[pid]['lower'] >= float(MINIMUM_GAIN))
    status = ('FreshConfirmationWarranted' if eligible else 'RetireBelowPracticalThreshold'
              if all(by_id[pid]['upper'] < float(MINIMUM_GAIN) for pid in generated) else 'RetireUnresolvedAtBudget')
    return dict(version=OUTCOME_VERSION,status=status,eligiblePartyIds=eligible,wins=wins,endpoints=endpoints,
        promoted=False,additionalSamples=0,interpretation='FiniteCapturedNeighborhoodOnly')


def load_plan(root=ROOT):
    evidence = {key:Evidence(root/folder,pin) for key,(folder,pin) in PACKAGES.items()}
    preview, scientific, review = (evidence[k] for k in ('preview','scientific','review'))
    # Only authenticated pure helpers; no old outcome analysis is invoked.
    helper = review.bytes('audit-proposal-affinity-study.py')
    path = review.root/'audit-proposal-affinity-study.py'
    spec = importlib.util.spec_from_file_location('sealed_neighborhood_helpers',path)
    audit = importlib.util.module_from_spec(spec); spec.loader.exec_module(audit)
    request, batches = preview.read('root-01/request.json'), preview.read('root-01/batches.json')
    context, native, freeze = (scientific.read(n) for n in ('source/context.json','source/plan.json','study/freeze.json'))
    require(request['version'] == batches['version'] == 'tower-proposal-export-v5' and batches['status'] == 'Complete', 'Changed generation export')
    development = copy.deepcopy(request['context'])
    for field in ('id','excludedCombatSeeds','executionHash'):
        development['scope'][field] = context['scope'][field]
    require(development == context, 'Changed captured physical context')
    audit.validate_policies(native)
    policies = [native['control'],native['candidate']]
    require(policies == request['policies'] and native['version'] == 'tower-affinity-allied-action-comparison-v1', 'Changed source policies')
    coverages = {p['version']:next(a['affinityCreationCoverage'] for a in batches['arms'] if a['name']==p['name']) for p in policies}
    providers = coverages[CANDIDATE]['alliedActionProtection']['providers']
    require([{k:v for k,v in p.items() if k!='effectDefinitionHash'} for p in providers] == audit.allied_providers(context['damageAffinityInventory'])
            and all(re.fullmatch('[0-9a-f]{64}',p['effectDefinitionHash']) for p in providers), 'Changed typed provider inventory')
    require(coverages[CANDIDATE]['alliedActionProtection']['inventoryHash'] == batches['damageSourceAffinities']['inventoryHash'] == native['inventoryHash'], 'Changed inventory binding')
    parent = next(s['party'] for s in context['scope']['starts'] if s['referenceId'] == context['benchmarkReferenceId'])
    require(parent['id'] == native['benchmarkPartyId'], 'Changed benchmark')
    member = next(m for m in freeze['families'][0]['members'] if 'benchmark' in m['roles'])
    anchor = copy.deepcopy(member['scenario']); anchor['seeds'] = []
    require(member['party'] == parent and anchor['party'] == audit.physical_party(context,parent)
            and anchor['floorNumber'] == context['mechanics']['floor'] and anchor['startsAt'] == context['scope']['startsAt'], 'Changed physical scenario anchor')
    teams, neighborhoods = catalogue(context,parent,policies,batches['damageSourceAffinities']['affinities'],coverages,providers,anchor)
    endpoints = endpoint_contract(teams,parent['id'])
    sources = {str(e.root.relative_to(root).as_posix())+'/'+n:pin for e in evidence.values() for n,pin in e.consumed.items()}
    plan = dict(version=VERSION,status='FrozenDesignNativeIntegrationRequired',runnableRequest=False,
        identity='SHA256 of sorted compact ASCII-escaped JSON; distinct from native HarnessJson identity',
        sourceFiles=dict(sorted(sources.items())),benchmarkPartyId=parent['id'],teams=teams,
        population=dict(controlRecipes=41,candidateRecipes=26,controlOnlyRecipes=15,references=3,physicalRecipes=44,
            controlPairEdits=62,candidatePairEdits=40,sampling='Complete inclusion; no population-sampling draw or weights',
            weighting='Equal distinct physical recipes within each neighborhood, not pair/edit multiplicity, search exposure or policy sampling probability'),
        sourceContract=dict(policies=policies,inventoryHash=native['inventoryHash'],capturedInventoryHash=digest(context['damageAffinityInventory']),
            capturedContextHash=digest(context),capturedRuntime={k:context['scope'][k] for k in ('contentHashes','settingsHash','executionHash')},
            physicalAnchorHash=digest(anchor),providerEvidence=providers),
        samplesPerTeam=SAMPLES,requiredFreshCombatValues=SAMPLES,maximumAttemptedFights=len(teams)*SAMPLES,
        allocation=dict(entropyDraws=1,entropyBytes=65536,int32Words=16384,assignment='First 2048 distinct signed Int32 words absent from the pre-draw complete history, preserving stream order',
            source='One operating-system CSPRNG batch after design freeze and admission; never draw during planning',
            exclusions='Permanently reserve every exposed fresh value, including unused tail; keep collisions and duplicate records; no refill or redraw',
            population='Signed Int32 seed space minus frozen pre-draw history; common uniform sample without replacement',
            pairing='Same ordered 2048-value panel for all 44 physical recipes; one evaluation per recipe/value, reused by both neighborhood memberships',
            order='Frozen team order, then common seed order',replication='No search roots or selector replay; seeds are the sampling units',
            failure='Missing cell, runtime drift or exhausted cap invalidates the entire experiment. Retain failures and charges; no partial conclusion, replacement or resume'),
        statisticalEndpoints=endpoints,
        uncertainty=dict(method='Two-sided Hoeffding bound with Bonferroni union bound over all 46 fixed linear endpoints',
            familySize=len(endpoints),alpha=dict(numerator=1,denominator=20),formula='(b-a)*sqrt(log(2*M/alpha)/(2*n)); interval clipped to [a,b]',
            derivation='For each common seed form its fixed linear win contrast. All wins are 0/1; negative coefficients bound a, positive coefficients bound b. Endpoint dependence is allowed by the union bound.',
            sampling='Uniform fresh seeds without replacement in the fixed eligible finite seed population; deterministic frozen engine assumed',
            ranges='Individual and neighborhood-minus-benchmark contrasts [-1,1]; candidate-minus-control neighborhood mean [-15/41,15/41], after shared coefficients cancel',
            references='Two nonbenchmark references are included in the simultaneous contrast family but never qualify as generated discoveries',
            rates='All 44 win counts/rates descriptive; the 46 contrasts alone have simultaneous intervals. No extra nominal intervals or data-dependent family.',
            source='https://arxiv.org/abs/1309.4029',sourceLocation='Bardenet and Maillard (2015), Proposition 1.2; apply both tails and a union bound',
            limitations='Conservative finite-sample bounds. Not calibrated to detect a 3-point true effect with high power. No generalization to other contexts or future-root search reliability.'),
        decision=dict(version=OUTCOME_VERSION,minimumPracticalGain=dict(numerator=3,denominator=100),
            first='Incomplete or invalid evidence: no scientific decision',
            advance='FreshConfirmationWarranted iff at least one generated recipe has simultaneous lower gain bound >= 0.03; report every such recipe in partyId order',
            retireBelow='RetireBelowPracticalThreshold iff no advance and every generated upper gain bound is strictly below 0.03',
            retireUnresolved='RetireUnresolvedAtBudget otherwise; stop this neighborhood without claiming absence of useful recipes',
            furtherSamples=0,promotion=False,referenceRule='The original benchmark stays fixed even if another reference scores higher',
            discovery='Fresh confirmation warranted is a development result, never team qualification or a proposer adoption rule'),
        proposedResourceCeilings=dict(status='DesignCeilingsNotAdmittedOrChargedYet',
            admission=dict(seconds=1800,bytes=512*1048576),nativeExecution=dict(seconds=14400,bytes=16*1073741824),
            auditsAndPublication=dict(seconds=3600,bytes=1073741824),postpublicationVerification=dict(seconds=3600,bytes=256*1048576),
            totalSeconds=23400,totalBytes=19058917376,borrowing=False,
            admissionRequirement='Freeze a compatible tested runtime, complete history, measured phase-specific cost forecast and cumulative allowance before entropy. Reject if the fixed 90112-fight design cannot fit. No sample reduction, borrowing, retries or silent cap extension.'),
        requiredBeforeExecution=['Implement a separately versioned native complete-family runner and independent audit for this exact contract; preserve legacy profile rejection',
            'Test complete membership, common-seed binding, all outcomes, linear endpoints, decisions, durable reservations, owned-process limits and failure retention',
            'Qualify runtime and perform fresh resource/history admission with explicit cumulative maxima; no prototype result supplies new confirmation'],
        newFights=0,newValues=0,newEntropyDraws=0,policyDefaultsChanged=False)
    require(len(endpoints)==46 and plan['maximumAttemptedFights']==90112, 'Changed endpoint/fight count')
    caps=plan['proposedResourceCeilings']
    require(sum(caps[k]['seconds'] for k in ('admission','nativeExecution','auditsAndPublication','postpublicationVerification'))==caps['totalSeconds']
            and sum(caps[k]['bytes'] for k in ('admission','nativeExecution','auditsAndPublication','postpublicationVerification'))==caps['totalBytes'], 'Changed resource arithmetic')
    for e in evidence.values(): e.recheck()
    require(path.read_bytes()==helper,'Helper changed during planning')
    return plan


def publish(output):
    require(not output.exists(), 'Plan package exists; no retry or overwrite')
    test=Path(__file__).with_name('test-affinity-neighborhood-recognition-plan.py')
    log=ROOT/'TestResults/affinity-neighborhood-recognition-plan-tests-20260924.log'
    code={p.name:p.read_bytes() for p in (Path(__file__),test)}
    rawlog=log.read_bytes(); match=re.search(r'Ran (\d+) tests in ',rawlog.decode('utf-8-sig'))
    require(match and int(match[1])>=20 and '\nOK' in rawlog.decode('utf-8-sig'),'Missing passing tests')
    prior=decode((ROOT/PRIOR).read_bytes()); require(sha((ROOT/PRIOR).read_bytes())==PRIOR_PIN,'Changed preceding handoff')
    output.mkdir(parents=True)
    started=time.monotonic()
    save(output/'declaration.json',dict(kind='CompleteNeighborhoodPlanConstruction',maximumSeconds=SECONDS,maximumBytes=BYTES,
        fullAllowanceChargedOnStartIncludingFailure=True,implementation={n:sha(raw) for n,raw in code.items()},testsSha256=sha(rawlog),testsPassed=int(match[1]),
        sourcePackages=PACKAGES,priorHandoffSha256=PRIOR_PIN,newFights=0,newValues=0,newEntropyDraws=0,
        recordedBeforeSeconds=prior['cumulativeRecordedSeconds'],recordedBeforeBytes=prior['cumulativeRecordedBytes'],
        cumulativeDeclaredMaximumSeconds=prior['cumulativeDeclaredMaximumSeconds']+SECONDS,
        cumulativeDeclaredMaximumBytes=prior['cumulativeDeclaredMaximumBytes']+BYTES))
    timer=threading.Timer(SECONDS,lambda:os._exit(124)); timer.daemon=True; timer.start()
    try:
        plan=load_plan()
        save(output/'plan.json',plan)
        for n,raw in code.items(): (output/n).write_bytes(raw)
        (output/'tests.log').write_bytes(rawlog)
        require(all((Path(__file__).parent/n).read_bytes()==raw for n,raw in code.items()) and log.read_bytes()==rawlog,'Planning implementation changed')
        require(time.monotonic()-started < SECONDS and sum(p.stat().st_size for p in output.iterdir()) < BYTES-100000,'Plan construction allowance exhausted')
        save(output/'files.json',{p.name:sha(p.read_bytes()) for p in sorted(output.iterdir())})
        return dict(status=plan['status'],manifestSha256=sha((output/'files.json').read_bytes()),planSha256=sha((output/'plan.json').read_bytes()),
            seconds=time.monotonic()-started,retainedBytes=sum(p.stat().st_size for p in output.iterdir()),physicalRecipes=44,
            futureFights=90112,futureValues=SAMPLES,newFights=0,newValues=0,newEntropyDraws=0)
    except BaseException as error:
        save(output/'failure.json',dict(error=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES))
        raise
    finally:
        timer.cancel()


def verify(package,pin):
    evidence=Evidence(package,pin)
    require(set(evidence.manifest)=={'declaration.json','plan.json',Path(__file__).name,'test-affinity-neighborhood-recognition-plan.py','tests.log'},'Incomplete plan package')
    require(evidence.bytes(Path(__file__).name)==Path(__file__).read_bytes(),'Changed reproducing builder')
    actual=evidence.read('plan.json')
    require(actual==load_plan(),'Plan does not reproduce')
    for n in evidence.manifest: evidence.bytes(n)
    evidence.recheck()
    return dict(status='VerifiedCompleteNeighborhoodPlan',physicalRecipes=44,endpoints=46,newFights=0,newValues=0)


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    sub=parser.add_subparsers(dest='command',required=True)
    p=sub.add_parser('build');p.add_argument('--output',type=Path,required=True)
    p=sub.add_parser('verify');p.add_argument('--package',type=Path,required=True);p.add_argument('--manifest-pin',required=True)
    args=parser.parse_args()
    print(json.dumps(publish(args.output) if args.command=='build' else verify(args.package,args.manifest_pin),indent=2))
