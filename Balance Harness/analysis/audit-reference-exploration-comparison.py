"""Independent saved-battle audit of the frozen reference-exploration comparison.

Never executes the game or draws entropy. Native reconstruction separately verifies
proposal trajectories and prepared inputs. This auditor recounts both searches,
the common selector, physical confirmation union and every reported endpoint.
"""
import argparse
import copy
import gzip
import hashlib
import json
import math
import os
from pathlib import Path
from statistics import NormalDist
import struct
import sys

VERSION = 'tower-reference-exploration-comparison-v1'
OFFSET_VERSION = 'tower-reference-exploration-offset-comparison-v1'
ADAPTIVE_VERSION = 'tower-adaptive-racing-comparison-v1'
ADAPTIVE_PLAN = '268159cb2f03a44066e76012e34591e560ce74f1d84acf79617d07503378d550'
ADAPTIVE_POLICY = 'tower-adaptive-beam-racing-v1'
SCREENING_VERSION = 'tower-practical-fresh-screening-comparison-v1'
SCREENING_PLAN = '13fc98e45afb7a96403918ffe0233fd1dcb375a52fb9446b63053911cc94c263'
SCREENING_PIPELINE = 'tower-practical-fresh-screening-v1'
DIRECT_PIPELINE = 'tower-practical-direct-nomination-v1'
OFFSET_PLAN = '7e6203052cd8a1984b80bbe34dbb1e4b21fe3f882c8ab314e21987d289a25c90'
BASELINE = 'retained-composition-three-references-v1'
CANDIDATE = 'retained-composition-three-reference-exploration-v1'
SELECTOR = 'tower-staged-incumbent-tie-v1'
PLAN = '6a0e8fb68d7a20e66740795d17278e5bf7e95c8e77c6b10f9127e8c3e77468a4'
CAPTURE = '001eb3e1a8642f3168f15f4ebc81ec65f33235cbae8ae1e2bc5104c61646142d'
CAPTURE_TEMPLATE = 'cd12abf230a8244170aac3b059daf016dae3f2fccdf3cd2dfa3de2add7741f83'
CAPTURE_FAILURE = '8e277d2ad609fe1dd00063f6a9b27930ee15f6a82389e07d933c907613be762e'
CLOSEOUT = '65cde20aabb7deaffcd889b0e685a9bd26830d63be4678db75a2f4506956d213'
PARTIES = ['399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b',
           '8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50',
           '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c']


def require(ok, message):
    if not ok:
        raise ValueError(message)


def protocol(version):
    if version == VERSION:
        return CANDIDATE, PLAN, 'SupportsReferenceExplorationForFrozenOutputs', 'DoNotPromoteReferenceExploration'
    if version == OFFSET_VERSION:
        return 'retained-composition-three-reference-exploration-offset-v1', OFFSET_PLAN, 'SupportsReferenceExplorationOffsetForFrozenOutputs', 'DoNotPromoteReferenceExplorationOffset'
    if version == SCREENING_VERSION:
        return BASELINE, SCREENING_PLAN, 'SupportsFreshScreeningForFrozenOutputs', 'DoNotPromoteFreshScreening'
    if version == ADAPTIVE_VERSION:
        return ADAPTIVE_POLICY, ADAPTIVE_PLAN, 'LargerFreshEvaluationWarranted', 'RetainBaselineAndBenchmark'
    raise ValueError('Unknown reference exploration comparison protocol')


def search_values(version):
    protocol(version)
    return 1356 if version == ADAPTIVE_VERSION else 588 if version == SCREENING_VERSION else 492


def assigned_values(version):
    return search_values(version) + 12 * sample_count(version)


def sample_count(version):
    return 256 if version == ADAPTIVE_VERSION else 1000


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def digest(value):
    # Canonical hashing for the fixed ASCII metadata and ordinary decimal values.
    # Search fitness can contain exponent-form .NET doubles; its hash is native-only.
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True).encode()).hexdigest()


def authenticate(root, pin):
    require(sha(root/'files.json') == pin, 'Changed manifest pin')
    files = read(root/'files.json')
    actual = set()
    for p in root.rglob('*'):
        require(not p.is_symlink() and not p.is_junction(), 'Linked archive path')
        if p.is_file():
            actual.add(p.relative_to(root).as_posix())
    require(actual == set(files) | {'files.json'}, 'Changed archive membership')
    for name, value in files.items():
        p = root/name
        require(p.resolve().is_relative_to(root.resolve()) and sha(p) == value, 'Changed file: '+name)
    return files


def classify(entropy, history, version=VERSION):
    require(len(entropy) == 65536 and history == sorted(set(history)) and len(history) <= 983616, 'Invalid reservation input')
    seen, prior, fresh, collisions, duplicates = set(), set(history), [], 0, 0
    for word in struct.unpack('<16384i', entropy):
        if word in prior:
            collisions += 1
        elif word in seen:
            duplicates += 1
        else:
            seen.add(word)
            fresh.append(word)
    require(len(fresh) >= assigned_values(version), 'Entropy exhausted; refill forbidden')
    return fresh[:assigned_values(version)], sorted(fresh), collisions, duplicates


def endpoint(pairs, historical, version=VERSION):
    require(version != ADAPTIVE_VERSION, 'Adaptive pilots require the absolute benchmark endpoint')
    _, _, support, negative = protocol(version)
    require(len(pairs) == 12 and [p['restart'] for p in pairs] == list(range(1, 13))
            and 0 <= historical <= 983616, 'Incomplete endpoint')
    for p in pairs:
        a, b, g, l = (p[k] for k in ('baselineWins', 'candidateWins', 'gains', 'losses'))
        require(all(type(x) is int for x in (a, b, g, l, p['recipeCount']))
                and 0 <= a <= 1000 and 0 <= b <= 1000 and 0 <= g <= min(b, 1000-a)
                and 0 <= l <= min(a, 1000-b) and g+l <= 1000 and b-a == g-l
                and p['difference'] == (g-l)/1000 and 3 <= p['recipeCount'] <= 5
                and p['identical'] == (p['baselineParty'] == p['candidateParty'])
                and (not p['identical'] or g == l == 0 and p['recipeCount'] <= 4), 'Invalid paired counts')
    k = sum(not p['identical'] for p in pairs)
    net = sum(p['gains']-p['losses'] for p in pairs)
    positive = sum(p['gains'] > p['losses'] for p in pairs)
    n, denominator = k*1000, 12000
    depletion = n*(n-1)/((2**32-historical-search_values(version))*denominator)
    margin = math.sqrt(2*n*math.log(20))/denominator+depletion
    mean, lower = net/denominator, max(-k/12, net/denominator-margin)
    decision = ('NoSelectedOutputDifferences' if k == 0 else support
                if net >= 600 and lower > 0 and positive >= 7 else negative)
    return dict(version=version, status='Verified', decision=decision, boundVersion='conditional-range-hoeffding-depletion-v1',
                pairs=pairs, activeRestarts=k, netWins=net, positiveRestarts=positive, denominator=denominator,
                meanDifference=mean, depletion=depletion, margin=margin, lowerBound=lower,
                fights=12672+1000*sum(p['recipeCount'] for p in pairs), descriptiveViews=[])


def same_numbers(actual, expected, tolerance=1e-12):
    if isinstance(expected, float):
        return type(actual) in (float, int) and math.isfinite(actual) and math.isclose(actual, expected, rel_tol=tolerance, abs_tol=tolerance)
    if isinstance(expected, dict):
        return isinstance(actual, dict) and actual.keys() == expected.keys() and all(same_numbers(actual[k], v, tolerance) for k, v in expected.items())
    if isinstance(expected, list):
        return isinstance(actual, list) and len(actual) == len(expected) and all(same_numbers(a, b, tolerance) for a, b in zip(actual, expected))
    return type(actual) is type(expected) and actual == expected


def wilson(wins, n=1000):
    z, p = NormalDist().inv_cdf(1-.025/10), wins/n
    denom = 1+z*z/n
    center = (p+z*z/(2*n))/denom
    radius = z*math.sqrt(p*(1-p)/n+z*z/(4*n*n))/denom
    return dict(rate=p, lower=max(0., center-radius), upper=min(1., center+radius), confidence=.995)


def pilot_root(restart, baseline, candidate, benchmark, benchmark_party, novel):
    require(len(baseline) == len(candidate) == len(benchmark) == 256, 'Incomplete pilot panel')
    d = [int(n)-int(b) for n, b in zip(candidate, baseline)]
    g = [int(n)-int(r) for n, r in zip(candidate, benchmark)]
    dm, gm = sum(d)/256, sum(g)/256
    def cov(x, y, xm, ym):
        return sum((a-xm)*(b-ym) for a, b in zip(x, y))/(256*255)
    return dict(restart=restart, benchmarkParty=benchmark_party, benchmarkWins=sum(benchmark), novel=novel,
                benchmarkGains=sum(x > 0 for x in g), benchmarkLosses=sum(x < 0 for x in g), benchmarkDifference=gm,
                methodSeedStandardError=math.sqrt(cov(d, d, dm, dm)), benchmarkSeedStandardError=math.sqrt(cov(g, g, gm, gm)),
                seedCovariance=cov(d, g, dm, gm))


def pilot_decision(method, benchmark, promising):
    if method <= -.02 or benchmark <= -.02 and promising < 3:
        return 'AbandonThisConfiguration'
    if method >= .02 and benchmark >= 0 and promising >= 3:
        return 'LargerFreshEvaluationWarranted'
    return 'InconclusiveRetainBaselineAndBenchmark'


def pilot_endpoint(pairs, roots, historical):
    require(len(pairs) == len(roots) == 12 and 0 <= historical <= 983616, 'Incomplete pilot')
    require([p['restart'] for p in pairs] == [r['restart'] for r in roots] == list(range(1, 13)), 'Reordered roots')
    def interval(values):
        mean = sum(values)/12
        sd = math.sqrt(sum((x-mean)**2 for x in values)/11)
        margin = 2.200985160082949*sd/math.sqrt(12)
        return dict(mean=mean, standardDeviation=sd, lower=max(-1., mean-margin), upper=min(1., mean+margin))
    method = interval([p['difference'] for p in pairs])
    ordered = sorted(r['benchmarkDifference'] for r in roots)
    benchmark = interval([r['benchmarkDifference'] for r in roots])
    promising = sum(r['novel'] and r['benchmarkDifference'] >= .03 for r in roots)
    depletion = 3071/(2**32-historical-1356)
    margin = math.sqrt(2*math.log(20)/3072)+depletion
    metrics = dict(interpretation='DevelopmentCriteriaOnlyNoPolicyPromotionOrTeamAdoption', roots=roots,
        methodRoots=method, benchmarkRoots=benchmark, medianBenchmarkDifference=(ordered[5]+ordered[6])/2,
        worstBenchmarkDifference=ordered[0], novelOutputs=sum(r['novel'] for r in roots), promisingNovelOutputs=promising,
        aboveThreePoints=sum(x >= .03 for x in ordered), aboveFivePoints=sum(x >= .05 for x in ordered),
        belowMinusThreePoints=sum(x <= -.03 for x in ordered), belowMinusFivePoints=sum(x <= -.05 for x in ordered),
        benchmarkConditionalLowerBound=max(-1., benchmark['mean']-margin),
        rootUncertainty='TwoSided95PercentPairedRootT11SmallSampleApproximation',
        conditionalUncertainty='SeparateOneSided95PercentConditionalHoeffdingWithDepletionNotFutureRootBounds')
    return dict(version=ADAPTIVE_VERSION, status='Verified', decision=pilot_decision(method['mean'], benchmark['mean'], promising),
        boundVersion='conditional-range-hoeffding-depletion-v1', pairs=pairs, activeRestarts=sum(not p['identical'] for p in pairs),
        netWins=sum(p['gains']-p['losses'] for p in pairs), positiveRestarts=sum(p['difference'] > 0 for p in pairs), denominator=3072,
        meanDifference=method['mean'], depletion=depletion, margin=margin, lowerBound=max(-1., method['mean']-margin),
        fights=12672+256*sum(p['recipeCount'] for p in pairs), descriptiveViews=[], pilot=metrics)


def audit_rows(root, template, values, primary, version=VERSION):
    candidate, _, _, _ = protocol(version)
    require(len(values) == len(set(values)) == assigned_values(version) and not set(values).intersection(template['excludedCombatSeeds']), 'Changed panels')
    samples = sample_count(version)
    pilot_roots = []
    screened_protocol = version == SCREENING_VERSION
    block = search_values(version)//12
    sr = root/'study'
    study, frozen = read(sr/'study.json'), read(sr/'outputs-freeze.json')
    require(study['version'] == frozen['version'] == version and study['freeze'] == frozen, 'Changed global freeze')
    require(frozen['bindingHash'] == digest(read(root/'allocation.json')), 'Changed allocation binding')
    searches, families = frozen['searches'], frozen['families']
    require([s['restart'] for s in searches] == [f['restart'] for f in families] == list(range(1, 13))
            and frozen['completedAttempts'] == 12672, 'Incomplete global search')
    raw = (sr/'trials.jsonl').read_bytes()
    require(raw.endswith(b'\n'), 'Torn trial ledger')
    trials = [json.loads(line) for line in raw.splitlines()]
    require([t['id'] for t in trials] == [f'trial-{i+1:06d}' for i in range(len(trials))], 'Reordered trial ledger')
    cursor, direct, scenarios, recipes, recipe_names = 0, {}, {}, {}, set()
    references = {s['party']['id']: s['referenceId'] for s in template['starts']}
    require(len(references) == 3 and primary in references, 'Changed controls')

    def consume(stage, seeds, scenario=None, expected_ids=None, expected_party=None):
        nonlocal cursor
        won, rows = [], []
        for i, seed in enumerate(seeds):
            require(cursor < len(trials), 'Missing direct battle')
            trial = trials[cursor]
            cursor += 1
            require(trial['stage'] == stage and trial['seed'] == seed, 'Reordered or reused stage panel')
            if expected_ids is not None:
                require(trial['id'] == expected_ids[i], 'Wrong measurement trial')
            name = trial['recipe']
            require(len(name) == 64 and all(c in '0123456789abcdef' for c in name), 'Invalid recipe path')
            if name not in recipes:
                recipe = read(sr/'recipes'/f'{name}.json')
                builds = {str(p['partySlot']): p['build']['essenceIds'] for p in recipe['party']}
                party = digest(builds)
                physical = copy.deepcopy(template['contexts'][0]['characterTemplates'])
                for actor in physical:
                    actor['build']['essenceIds'] = builds[str(actor['partySlot'])]
                    actor['build']['identityEssenceIds'] = [f'neutral-identity-slot-{j+1}' for j in range(template['budget']['essenceSlots'])]
                require(recipe['party'] == physical and recipe['floorNumber'] == template['budget']['priorityFloor']
                        and recipe['startsAt'] == template['startsAt'], 'Changed physical character or encounter')
                recipes[name] = recipe, party
                recipe_names.add(name+'.json')
                scenarios[(recipe['id'], party)] = dict(recipe, seeds=[])
            recipe, party = recipes[name]
            require(recipe['seeds'] == seeds and (expected_party is None or party == expected_party), 'Changed panel or measured party')
            if scenario is not None:
                require(recipe == dict(scenario, seeds=seeds), 'Confirmed the wrong physical recipe')
            with gzip.open(sr/'battles'/f"{trial['id']}.json.gz", 'rt', encoding='utf-8') as stream:
                report = json.load(stream)
            battle, outcome = report['battle'], report['battle']['summary']['contentOutcome']
            require(battle['seed'] == seed and battle['scenarioId'] == recipe['id'] and outcome in ('Victory', 'Defeat', 'Draw')
                    and report['succeeded'] == (outcome == 'Victory'), 'Invalid direct outcome')
            require(math.isfinite(report['guardianHealthRemainingPercent']), 'Invalid health')
            friendly = battle['summary']['friendly']
            require(len(friendly) > 0, 'Missing friendly outcome')
            direct[trial['id']] = (report['guardianHealthRemainingPercent'],
                                  100*sum(p['health'] > 0 for p in friendly)/len(friendly),
                                  battle['summary']['durationSeconds'], outcome)
            won.append(outcome == 'Victory')
            rows.append(dict(seed=seed, outcome=outcome))
        return won, rows

    def measurements(stage, rows, seeds, size):
        require(len(rows) == len({r['id'] for r in rows}) == size, 'Incomplete measurements')
        for row in rows:
            require(len(row['cells']) == 1 and len(row['cells'][0]['trials']) == len(seeds), 'Changed context or trial count')
            cell = row['cells'][0]
            won, _ = consume(stage, seeds, expected_ids=cell['trials'], expected_party=row['id'])
            health = sum(direct[t][0] for t in cell['trials'])/len(seeds)
            survival = sum(direct[t][1] for t in cell['trials'])/len(seeds)
            durations = [direct[t][2] for t in cell['trials'] if direct[t][3] == 'Victory']
            fitness = dict(worstContextWinRate=sum(won)/len(seeds), guardianHealth=health, survival=survival,
                           victoryDuration=sum(durations)/len(durations) if durations else sys.float_info.max)
            require(won == cell['clears'] and math.isclose(cell['guardianHealth'], health, abs_tol=1e-10)
                    and math.isclose(cell['survival'], survival, abs_tol=1e-10)
                    and cell['draws'] == sum(direct[t][3] == 'Draw' for t in cell['trials'])
                    and same_numbers(row['fitness'], fitness), 'Changed measurement or discovery ranking fitness')

    def rank(rows):
        return sorted(rows, key=lambda r: (-r['fitness']['worstContextWinRate'], r['fitness']['guardianHealth'],
                      -r['fitness']['survival'], r['fitness']['victoryDuration'], r['id']))

    def protected_ids(ranked, challengers):
        kept = set(references) | {r['id'] for r in ranked if r['id'] not in references}
        require(set(references).issubset({r['id'] for r in ranked}) and len(kept) >= challengers+3, 'Missing protected controls or challengers')
        kept = set(references) | set([r['id'] for r in ranked if r['id'] not in references][:challengers])
        return [r['id'] for r in ranked if r['id'] in kept]

    def adaptive_search(j, run, prefix):
        require(run == read(sr/(prefix+'.json')) and run['discovery'] is None and run['selection'] == []
                and 'screening' not in run, 'Changed adaptive arm')
        report = run['adaptive']
        evaluation = report['evaluation']
        plan = read(sr/(prefix+'-plan.json'))
        offset = j*113
        panel_seeds = [values[offset+41+i*8:offset+41+i*8+(40 if i == 4 else 8)] for i in range(5)]
        roles = ['wave-1-screen', 'wave-1-continuation', 'wave-2-screen', 'wave-2-continuation', 'selection']
        require(report == read(sr/(prefix+'-adaptive.json')) and report['version'] == plan['version'] == ADAPTIVE_POLICY
                and plan['rootSeed'] == values[offset] and plan['benchmarkReferenceId'] == template['starts'][2]['referenceId']
                and plan['maximumEvaluations'] == 528 and plan['panels'] == [dict(role=r, seeds=s) for r, s in zip(roles, panel_seeds)]
                and evaluation['version'] == ADAPTIVE_POLICY and evaluation['status'] == 'Complete'
                and evaluation['plannedEvaluations'] == evaluation['chargedEvaluations'] == 528
                and len(evaluation['panels']) == 5 and len(evaluation['decisions']) == len(report['batches']) == 2, 'Changed adaptive plan or accounting')
        parties = {s['party']['id']: s['party'] for s in template['starts']}
        seen = set(parties)
        beam, observations, before = [], [], 0

        def scores(ids, rows):
            result = []
            for pid in ids:
                outcomes = [o['outcome'] for o in rows if o['request']['partyId'] == pid]
                won = [o for o in outcomes if o['outcome'] == 'Victory']
                result.append(dict(id=pid, samples=len(outcomes), wins=len(won), draws=sum(o['outcome'] == 'Draw' for o in outcomes),
                    fitness=dict(worstContextWinRate=len(won)/len(outcomes), guardianHealth=sum(o['guardianHealth'] for o in outcomes)/len(outcomes),
                        survival=sum(o['survival'] for o in outcomes)/len(outcomes),
                        victoryDuration=sum(o['durationSeconds'] for o in won)/len(won) if won else sys.float_info.max)))
            return result

        for i, panel in enumerate(evaluation['panels']):
            freeze, rows = panel['freeze'], panel['observations']
            if i in (0, 2):
                batch = report['batches'][i//2]
                require(batch == read(sr/(prefix+f'-batch-{i//2+1:02d}.json')) and batch['wave'] == i//2+1
                        and batch['afterEvaluations'] == before and batch['beamIds'] == beam
                        and set(batch['seenBefore']) == seen and len(batch['proposals']) <= 128
                        and len(batch['candidates']) == (9 if i == 0 else 8), 'Changed adaptive batch')
                for party in batch['candidates']:
                    require(party['id'] == digest(party['builds']) and party['id'] not in seen, 'Duplicate or changed adaptive recipe')
                    seen.add(party['id']); parties[party['id']] = party
                expected_ids = list(references)+beam+[p['id'] for p in batch['candidates']]
            elif i in (1, 3):
                expected_ids = list(references)+evaluation['decisions'][i//2]['survivorIds']
            else:
                eligible = set(references) | set(beam[:2])
                expected_ids = [r['id'] for r in rank(evaluation['decisions'][1]['commonScores']) if r['id'] in eligible]
                require(evaluation['nominees'] == expected_ids, 'Changed adaptive nominees')
            require(panel['complete'] and freeze == read(sr/(prefix+f'-panel-{i+1:02d}.json'))
                    and freeze['version'] == ADAPTIVE_POLICY and freeze['index'] == i and freeze['role'] == roles[i]
                    and freeze['seeds'] == panel_seeds[i] and freeze['parties'] == [parties[p] for p in expected_ids]
                    and freeze['evaluationsBefore'] == before and freeze['plannedEvaluations'] == len(expected_ids)*len(panel_seeds[i])
                    and len(rows) == freeze['plannedEvaluations'], 'Changed adaptive panel freeze')
            for k, pid in enumerate(expected_ids):
                subset = rows[k*len(panel_seeds[i]):(k+1)*len(panel_seeds[i])]
                consume(roles[i], panel_seeds[i], expected_ids=[o['outcome']['trialId'] for o in subset], expected_party=pid)
                for t, (seed, observation) in enumerate(zip(panel_seeds[i], subset)):
                    request, outcome = observation['request'], observation['outcome']
                    health, survival, duration, content_outcome = direct[outcome['trialId']]
                    require(request['ordinal'] == before+k*len(panel_seeds[i])+t+1 and request['seed'] == outcome['seed'] == seed
                            and request['partyId'] == pid and request['role'] == roles[i]
                            and request['scenario'] == dict(scenarios[(request['scenario']['id'], pid)], seeds=panel_seeds[i])
                            and same_numbers([outcome['guardianHealth'], outcome['survival'], outcome['durationSeconds']], [health, survival, duration])
                            and outcome['outcome'] == content_outcome, 'Adaptive observation differs from battle')
            require(same_numbers(panel['scores'], scores(expected_ids, rows)), 'Changed adaptive score')
            observations.append(rows)
            before += len(rows)
            if i in (0, 2):
                decision = evaluation['decisions'][i//2]
                ranked = rank([r for r in panel['scores'] if r['id'] not in references])
                elite = [r['id'] for r in ranked[:3]]
                cutoff = ranked[2]['wins']-1
                def distance(pid):
                    return min(sum(len(set(parties[pid]['builds'][owner])-set(parties[e]['builds'][owner]))
                                   for owner in parties[pid]['builds']) for e in elite)
                eligible = [r for r in ranked[3:] if r['wins'] >= cutoff]
                diverse = sorted(eligible, key=lambda r: -distance(r['id']))[0] if eligible else ranked[3]
                survivors = elite+[diverse['id']]
                require(decision['wave'] == i//2+1 and decision['eliteIds'] == elite and decision['diversityId'] == diverse['id']
                        and decision['competitiveCutoffWins'] == cutoff and decision['diversityFallback'] == (not eligible)
                        and decision['diversityDistance'] == distance(diverse['id']) and decision['survivorIds'] == survivors
                        and decision['prunedIds'] == [r['id'] for r in ranked if r['id'] not in survivors], 'Changed adaptive pruning')
            elif i in (1, 3):
                decision = evaluation['decisions'][i//2]
                common = scores(expected_ids, observations[i-1]+rows)
                beam = [r['id'] for r in rank(common) if r['id'] not in references]
                require(same_numbers(decision['commonScores'], common) and decision['beamIds'] == beam, 'Changed common-rung beam')
        selection = evaluation['panels'][-1]['scores']
        positions = {pid: k for k, pid in enumerate(evaluation['nominees'])}
        best = min(selection, key=lambda r: (-r['wins'], r['fitness']['guardianHealth'] if r['wins'] == 0 else 0, positions[r['id']], r['id']))
        chosen = primary if best['wins'] > 0 and next(r for r in selection if r['id'] == primary)['wins'] == best['wins'] else best['id']
        output = run['output']
        require(before == 528 and evaluation['rawSelectedId'] == chosen and output['generator'] == ADAPTIVE_POLICY
                and output['selector'] == SELECTOR and output['finalist']['primary'] and output['finalist']['party'] == parties[chosen]
                and output['scenario'] == scenarios[(output['scenario']['id'], chosen)], 'Changed adaptive output')

    for j, search in enumerate(searches):
        for key, policy in [('baseline', BASELINE), ('candidate', candidate)]:
            run = search[key]
            prefix = f'pair-{j+1:02d}-{key}'
            if version == ADAPTIVE_VERSION and key == 'candidate':
                adaptive_search(j, run, prefix)
                continue
            discovery = run['discovery']
            require(run == read(sr/(prefix+'.json')) and discovery == read(sr/(prefix+'-discovery.json'))
                    and discovery['version'] == policy and discovery['status'] == 'Complete' and len(discovery['arms']) == 1, 'Changed saved search')
            arm = discovery['arms'][0]
            screened = screened_protocol and key == 'candidate'
            offset = j*block
            require(arm['seed'] == values[offset] and len(arm['evaluations']) == 46 and len(arm['proposals']) <= 256
                    and arm['stopReason'] == 'CandidateBudgetReached', 'Changed search width or root')
            measurements('discovery', arm['evaluations'], values[offset+1:offset+(5 if screened else 9)], 46)
            ranked = rank(arm['evaluations'])
            shortlist = discovery['discoveryShortlist']
            require([s['id'] for s in shortlist] == protected_ids(ranked, 2), 'Changed five nominees')
            if screened:
                screen = run['screening']
                freeze = screen['freeze']
                require(screen == read(sr/(prefix+'-screening.json')) and freeze == read(sr/(prefix+'-screening-freeze.json'))
                        and screen['version'] == freeze['version'] == SCREENING_PIPELINE
                        and freeze['afterDiscoveryFights'] == 184 and screen['afterSearchFights'] == 368
                        and freeze['seeds'] == values[offset+9:offset+17], 'Changed screening checkpoint')
                # Exact hashes over .NET exponent-form discovery fitness are checked by native reconstruction.
                require(all(len(freeze[k]) == 64 and all(c in '0123456789abcdef' for c in freeze[k])
                            for k in ('definitionHash', 'discoveryHash')), 'Invalid screening binding hash')
                parties = [p['party'] for p in arm['proposals'] if p['result'] == 'evaluated']
                require(len(parties) == len({p['id'] for p in parties}) == 46, 'Changed evaluated membership')
                by_id = {p['id']: p for p in parties}
                expected = [by_id[i] for i in protected_ids(ranked, 20)]
                require(freeze['candidates'] == expected
                        and [r['id'] for r in screen['measurements']] == [p['id'] for p in expected], 'Changed screening membership')
                measurements('screening', screen['measurements'], freeze['seeds'], 23)
                shortlist = [by_id[i] for i in protected_ids(rank(screen['measurements']), 2)]
                require(screen['nominees'] == shortlist, 'Changed screened nominees')
            else:
                require('screening' not in run, 'Screening attached to direct pipeline')
            measurements('selection', run['selection'], values[offset+(17 if screened_protocol else 9):offset+(49 if screened_protocol else 41)], 5)
            ranks = {p['id']: i for i, p in enumerate(shortlist)}
            counts = {r['id']: sum(r['cells'][0]['clears']) for r in run['selection']}
            health = {r['id']: r['cells'][0]['guardianHealth'] for r in run['selection']}
            require(list(counts) == list(ranks), 'Reordered selection family')
            best = min(counts, key=lambda p: (-counts[p], health[p] if counts[p] == 0 else 0, ranks[p], p))
            maximum = max(counts.values())
            chosen = primary if maximum > 0 and counts[primary] == maximum and list(counts.values()).count(maximum) > 1 else best
            output = run['output']
            require(output.get('pipeline') == (SCREENING_PIPELINE if screened else DIRECT_PIPELINE) if screened_protocol
                    else 'pipeline' not in output, 'Changed search pipeline')
            require(output['generator'] == policy and output['selector'] == SELECTOR and output['finalist']['party'] == next(s for s in shortlist if s['id'] == chosen)
                    and output['scenario'] == scenarios[(output['scenario']['id'], chosen)], 'Wrong generator, selector or output')
    require(cursor == 12672, 'Shared or missing search fights')
    pairs, views, evidence_cursor = [], [], 0
    for search, family in zip(searches, families):
        restart, members = search['restart'], family['members']
        a, b = search['baseline']['output'], search['candidate']['output']
        panel = values[search_values(version)+(restart-1)*samples:search_values(version)+restart*samples]
        expected_parties = set(references) | {a['finalist']['party']['id'], b['finalist']['party']['id']}
        require(family['seeds'] == panel and 3 <= len(members) <= 5
                and [m['recipeHash'] for m in members] == sorted({m['recipeHash'] for m in members})
                and len(members) == len(expected_parties) and {m['partyId'] for m in members} == expected_parties, 'Changed physical union')
        wins = {}
        for member in members:
            party = member['partyId']
            require(member['referenceIds'] == ([references[party]] if party in references else [])
                    and member['baseline'] == (party == a['finalist']['party']['id'])
                    and member['candidate'] == (party == b['finalist']['party']['id'])
                    and member['scenario'] == scenarios[(member['scenario']['id'], party)], 'Changed confirmation role or scope')
            for output, key in [(a, 'baseline'), (b, 'candidate')]:
                if member[key]:
                    require(member['recipeHash'] == output['recipeHash'] and member['scenario'] == output['scenario'], 'Changed selected identity')
            won, rows = consume('confirmation', panel, member['scenario'], expected_party=party)
            require(evidence_cursor < len(study['evidence']) and study['evidence'][evidence_cursor]
                    == dict(restart=restart, recipeHash=member['recipeHash'], trials=rows), 'Changed confirmation evidence')
            evidence_cursor += 1
            wins[party] = won
        ap, bp = a['finalist']['party']['id'], b['finalist']['party']['id']
        require((ap == bp) == (a['recipeHash'] == b['recipeHash']), 'Changed identity equivalence')
        g, l = sum(y and not x for x, y in zip(wins[ap], wins[bp])), sum(x and not y for x, y in zip(wins[ap], wins[bp]))
        pairs.append(dict(restart=restart, baselineParty=ap, candidateParty=bp, identical=ap == bp, baselineWins=sum(wins[ap]),
                          candidateWins=sum(wins[bp]), gains=g, losses=l, difference=(g-l)/samples, recipeCount=len(members)))
        if version == ADAPTIVE_VERSION:
            pilot_roots.append(pilot_root(restart, wins[ap], wins[bp], wins[template['starts'][2]['party']['id']],
                template['starts'][2]['party']['id'], bp not in references))
        for selected, policy, pipeline in [(ap, BASELINE, DIRECT_PIPELINE), (bp, candidate, SCREENING_PIPELINE)]:
            rates = [dict(partyId=m['partyId'], wins=sum(wins[m['partyId']]), estimate=wilson(sum(wins[m['partyId']]), samples))
                     for m in members if m['referenceIds'] or m['partyId'] == selected]
            contrasts = []
            for reference in template['references']:
                anchor = next(p for p, rid in references.items() if rid == reference['id'])
                cg = sum(x and not y for x, y in zip(wins[selected], wins[anchor]))
                cl = sum(y and not x for x, y in zip(wins[selected], wins[anchor]))
                gi, li = wilson(cg, samples), wilson(cl, samples)
                contrasts.append(dict(referenceId=reference['id'], gains=cg, losses=cl, observedGain=(cg-cl)/samples,
                                      lower=gi['lower']-li['upper'], upper=gi['upper']-li['lower']))
            views.append(dict(restart=restart, generator=policy, selectedPartyId=selected, interpretation='DescriptiveOnlyNoTeamAdoption',
                              intervalFamily=10, rates=rates, contrasts=contrasts))
            if screened_protocol:
                views[-1]['pipeline'] = pipeline
    require(evidence_cursor == len(study['evidence']) and cursor == len(trials) == 12672+samples*sum(p['recipeCount'] for p in pairs), 'Extra or missing fights')
    require({p.name for p in (sr/'recipes').iterdir()} == recipe_names
            and {p.name for p in (sr/'battles').iterdir()} == {t['id']+'.json.gz' for t in trials}, 'Extra or missing physical archive')
    raw = (root/'attempts.jsonl').read_bytes()
    lines = raw.splitlines(keepends=True)
    require(raw.endswith(b'\n') and len(lines) == 2*len(trials)
            and hashlib.sha256(b''.join(lines[:25344])).hexdigest() == frozen['attemptsHash'], 'Invalid global barrier')
    for i, line in enumerate(lines):
        require(json.loads(line) == dict(kind='Started' if i % 2 == 0 else 'Completed', ordinal=i//2+1), 'Reordered attempts')
    result = (pilot_endpoint(pairs, pilot_roots, len(template['excludedCombatSeeds'])) if version == ADAPTIVE_VERSION
              else endpoint(pairs, len(template['excludedCombatSeeds']), version))
    result['descriptiveViews'] = views
    return result


def audit(root, pin=None):
    if pin:
        authenticate(root, pin)
    require(not (root/'failure.json').exists(), 'Failed comparison')
    q, template = read(root/'request.json'), read(root/'template.json')
    version = q['version']
    _, plan, _, _ = protocol(version)
    adaptive = version == ADAPTIVE_VERSION
    seconds, byte_limit, native_seconds, native_bytes, prior_seconds, prior_bytes = ((10800, 6442450944, 10680, 6174015488, 0, 0)
        if adaptive else (10200, 5905580032, 10080, 5637144576, 600, 536870912))
    require( q.get('maximumSeconds', 10800) == 10800 and q.get('maximumBytes', 6442450944) == 6442450944
            and q.get('priorSeconds', 600) == prior_seconds and q.get('priorBytes', 536870912) == prior_bytes, 'Changed cumulative envelope')
    require(sha(root/'plan.json') == q['planHash'] == plan and sha(root/'auditor.py') == q['auditorHash']
            and sha(Path(__file__)) == q['auditorHash'] and sha(root/'template.json') == q['templateHash'], 'Unbound plan, template or auditor')
    pins = {'capture/files.json': CAPTURE, 'capture/preset/template.json': CAPTURE_TEMPLATE,
            'capture/failure.json': CAPTURE_FAILURE, 'capture/closeout/files.json': CLOSEOUT}
    for name, value in pins.items():
        require(sha(root/name) == value, 'Changed source capture')
    closeout = read(root/'capture/closeout/files.json')
    require(sha(root/'capture/closeout/receipt.json') == closeout['receipt.json'], 'Changed reconciliation receipt')
    expected = copy.deepcopy(read(root/'capture/preset/template.json'))
    expected.update(id='reference-exploration-template', executionHash=template['executionHash'], excludedCombatSeeds=template['excludedCombatSeeds'], maximumBattles=528+4*sample_count(version))
    expected.pop('primaryReferenceId', None)
    expected['generation'].update(policyVersion=BASELINE, seeds=[])
    require(template == expected and [s['party']['id'] for s in template['starts']] == PARTIES, 'Changed captured search or scope')
    receipt, launch = read(root/'native-receipt.json'), read(root/'launch.json')
    request_hash = sha(root/'request.json')
    require(receipt['version'] == launch['version'] == version and receipt['status'] == 'Verified'
            and receipt['requestFileHash'] == launch['requestFileHash'] == request_hash and receipt['newAuditFights'] == 0
            and 0 <= receipt['measuredSeconds'] < native_seconds and 0 <= receipt['observedBytes'] < native_bytes, 'Invalid native receipt')
    require(launch['maximumSeconds'] == seconds and launch['maximumBytes'] == byte_limit
            and launch['nativeMaximumSeconds'] == native_seconds and launch['nativeMaximumBytes'] == native_bytes
            and launch['mechanism'] == 'suspended-owned-job-v1', 'Changed launch allowance')
    if pin:
        completion = read(root/'completion.json')
        require(completion['version'] == version and completion['status'] == 'Complete' and completion['requestFileHash'] == request_hash
                and completion['retries'] == 0 and 0 <= completion['seconds'] < seconds and 0 <= completion['observedBytes'] <= byte_limit
                and completion['chargedSeconds'] == completion['seconds']+prior_seconds
                and completion['chargedBytes'] == completion['observedBytes']+prior_bytes, 'Invalid publication accounting')
        for p in [completion['process'], read(root/'independent-audit-process.json')]:
            require(p['exitCode'] == 0 and not p['timedOut'] and p['activeProcesses'] == 0
                    and p['totalProcesses'] >= 1 and p['mechanism'] == 'suspended-owned-job-v1', 'Incomplete process tree')
    process = read(root/'native-audit-process.json')
    require(process['mechanism'] == 'suspended-owned-job-v1' and process['exitCode'] == 0
            and not process['timedOut'] and process['activeProcesses'] == 0, 'Native audit did not finish')
    inputs = ['request.json', 'template.json', 'plan.json', 'auditor.py', 'native-receipt.json', 'launch.json', 'native-audit-process.json',
              'capture/closeout/receipt.json', 'entropy.bin', 'entropy-intent.json', 'allocation.json', 'history-files.json',
              'history-input.json', 'seed-ledger.json', 'attempts.jsonl', 'result.json'] + list(pins)
    input_pins = {name: sha(root/name) for name in inputs}
    authenticate(root/'study', receipt['archiveHash'])
    scope = read(root/'study/scope.json')
    require(scope['algorithm'] == version and scope['contentHashes'] == template['contentHashes']
            and scope['reportStorage'] == 'gzip-json-v1' and digest(scope['settings']) == template['settingsHash']
            and digest(scope['execution']) == template['executionHash'], 'Changed archive scope')
    for name, value in read(root/'capture/files.json').items():
        if name.startswith('runtime/') and not Path(name).name.startswith('BalanceHarness'):
            require(sha(root/'study/executable'/name[8:]) == value, 'Changed captured runtime dependency')
    for name, value in template['contentHashes'].items():
        require(sha(root/'study/content/Data'/name) == value, 'Changed captured content')
    values, reserved, collisions, duplicates = classify((root/'entropy.bin').read_bytes(), template['excludedCombatSeeds'], version)
    history = read(root/'history-files.json')
    require(all(history.get(name) == value for name, value in q['requiredHistory'].items()), 'Changed authoritative history pins')
    require(read(root/'entropy-intent.json') == dict(version=version, words=16384, assignedValues=assigned_values(version),
            historicalHash=digest(template['excludedCombatSeeds']), retries=0), 'Changed reservation intent')
    require(read(root/'allocation.json') == dict(version=version, entropyHash=sha(root/'entropy.bin'), historicalHash=digest(template['excludedCombatSeeds']),
            selected=values, reserved=reserved, historicalCollisions=collisions, duplicates=duplicates), 'Changed entropy allocation')
    require(read(root/'history-input.json') == dict(reservationState='Complete', reserved=reserved)
            and read(root/'seed-ledger.json') == dict(reservationState='Complete', historical=template['excludedCombatSeeds'], reserved=reserved), 'Lost reservations')
    computed, native = audit_rows(root, template, values, PARTIES[0], version), read(root/'result.json')
    native_primary, computed_primary = dict(native, descriptiveViews=[]), dict(computed, descriptiveViews=[])
    require(same_numbers(native_primary, computed_primary) and same_numbers(native['descriptiveViews'], computed['descriptiveViews'], 2e-9)
            and receipt['fights'] == computed['fights'], 'Independent arithmetic disagreement')
    # Native Wilson uses an Acklam inverse-normal approximation; retain its bytes
    # only after independently checking every number with an explicit tolerance.
    authenticate(root/'study', receipt['archiveHash'])
    require(all(sha(root/name) == value for name, value in input_pins.items()), 'Audit input changed while reading')
    if pin:
        authenticate(root, pin)
    return dict(status='Passed', requestFileHash=request_hash, manifestSha256=pin, result=native, inputHashes=input_pins,
                intervalTolerance=2e-9, primaryTolerance=1e-12, newFights=0, newValues=0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', nargs='?', type=Path)
    parser.add_argument('--manifest-sha256')
    parser.add_argument('--working', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    require(bool(args.working) != bool(args.archive), 'Supply one working or completed archive')
    require(bool(args.working) or bool(args.manifest_sha256), 'Completed archive needs a manifest pin')
    result = audit(args.working or args.archive, args.manifest_sha256)
    if args.output:
        with args.output.open('x', encoding='utf-8', newline='\n') as stream:
            json.dump(result, stream, indent=2)
            stream.flush()
            os.fsync(stream.fileno())
    print(json.dumps(result, indent=2))
