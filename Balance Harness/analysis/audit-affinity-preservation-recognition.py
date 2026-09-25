"""Independent direct-report recognition audit; no native calls, combat or entropy source."""
import argparse
import gzip
import hashlib
import json
import math
from pathlib import Path
import struct

VERSION = 'tower-affinity-preservation-recognition-v1'
PLAN = '8456bb84f4f061eba78d35371602ec8a49ac81e5e60a550552c02bda3d3fddf6'
N, CANDIDATES, FAMILY = 256, 109, 799
PANEL = 3072
ROOT_SIZES = (13,13,12,13,11,11,12,12,11,12,12,13)
LIMITATIONS = "Development diagnosis of twelve frozen paired pools only. Approximate family-799 Wilson intervals describe individual combat contrasts. Sampling variances condition on fixed full-panel outcomes; combat variances condition on the selected sample and retain shared-seed covariance. Report these components separately, never add them as a combined variance. Nominee summaries average all five frozen nominees. All-root summaries average twelve fixed roots, not future-root effects. Unknown outcomes remain null; no full-pool maximum, qualification, selector fitting, policy promotion or default change."
q = math.sqrt(-2 * math.log(.025 / FAMILY))
Z = -(((((-7.784894002430293e-3*q-.3223964580411365)*q-2.400758277161838)*q-2.549732539343734)*q+4.374664141464968)*q+2.938163982698783) / ((((7.784695709041462e-3*q+.3224671290700398)*q+2.445134137142996)*q+3.754408661907416)*q+1)


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def digest(value):
    text = json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True)
    for char in ['+', '<', '>', '&', "'"]:
        text = text.replace(char, '\\u%04X' % ord(char))
    return hashlib.sha256(text.encode()).hexdigest()


def authenticate(root):
    files = read(root/'files.json')
    actual = set()
    for p in root.rglob('*'):
        require(not p.is_symlink() and not p.is_junction(), 'Linked archive path')
        if p.is_file():
            actual.add(p.relative_to(root).as_posix())
    require(actual == set(files) | {'files.json'}, 'Changed archive membership')
    for name, pin in files.items():
        path = root/name
        require(path.resolve().is_relative_to(root.resolve()) and sha(path) == pin, 'Changed archive file: '+name)


def classify(entropy, history):
    require(len(entropy) == 24576 and history == sorted(set(history)) and 0 < len(history) <= 993856,
            'Invalid entropy/history input')
    require(all(type(s) is int and -2**31 <= s < 2**31 for s in history), 'Invalid historical value')
    prior, seen, words, fresh = set(history), set(), [], []
    for i, value in enumerate(struct.unpack('<6144i', entropy)):
        if value in prior:
            kind = 'AlreadyReserved'
        elif value in seen:
            kind = 'DuplicateBatch'
        else:
            kind = 'Confirmation' if len(fresh) < PANEL else 'ReservedUnused'
            fresh.append(value)
            seen.add(value)
        words.append(dict(ordinal=i, value=value, classification=kind))
    return words, fresh[:PANEL], fresh


def wilson(wins):
    require(type(wins) is int and 0 <= wins <= N, 'Invalid win count')
    p = wins/N
    denominator = 1+Z*Z/N
    center = (p+Z*Z/(2*N))/denominator
    width = Z*math.sqrt(p*(1-p)/N+Z*Z/(4*N*N))/denominator
    return dict(rate=p, lower=max(0, center-width), upper=min(1, center+width), confidence=1-.05/FAMILY)



def root_index(ordinal):
    require(type(ordinal) is int and 0 <= ordinal < 145, 'Invalid cell ordinal')
    offset = 0
    for r, size in enumerate(ROOT_SIZES):
        offset += size
        if ordinal < offset: return r
    raise ValueError('Unresolved root')


def cell_id(i, team):
    return f'r{root_index(i)+1:02}-'+team['partyId']


def covariance(a, b):
    require(len(a) == len(b) and len(a) > 1 and all(math.isfinite(v) for v in a+b), 'Invalid covariance sample')
    am, bm = sum(a)/len(a), sum(b)/len(b)
    return sum((x-am)*(y-bm) for x, y in zip(a, b))/ (len(a)-1)


def inclusion(team):
    p = team['inclusionProbability']
    require(type(p['numerator']) is type(p['denominator']) is int and 0 < p['numerator'] <= p['denominator'], 'Invalid inclusion probability')
    return p['numerator']/p['denominator']


def coefficients(teams, arm, field):
    def value(team):
        p = team['provenance'][arm]
        if p is None: return 0.
        if field == 'generated': return 1/17 if p[field] else 0.
        if field == 'nomineeRank': return 1/5 if p[field] is not None else 0.
        require(field == 'validationChallenger', 'Unknown paired estimand')
        return float(p[field])
    return [value(t) for t in teams]


def sampling_covariance(root, gains, a, b):
    total = 0.
    for name, stratum in root['strata'].items():
        n, k = stratum['populationCount'], stratum['sampleCount']
        indices = [i for i, t in enumerate(root['teams']) if t['stratum'] == name]
        require(len(indices) == k and 0 <= k <= n and (k == n or k >= 2), 'Invalid sampled stratum')
        if k == n: continue
        x = [a[i]*sum(gains[i])/N for i in indices]
        y = [b[i]*sum(gains[i])/N for i in indices]
        total += n*n*(1-k/n)*covariance(x, y)/k
    return total


def estimate(root, gains, c):
    trials = [sum(c[i]/inclusion(t)*gains[i][s] for i, t in enumerate(root['teams'])) for s in range(N)]
    sampling, combat = sampling_covariance(root, gains, c, c), covariance(trials, trials)/N
    return dict(mean=sum(trials)/N, samplingVariance=sampling, samplingStandardError=math.sqrt(sampling),
        combatVariance=combat, combatStandardError=math.sqrt(combat)), trials


def root_estimates(root, gains):
    teams = root['teams']
    a, b = (coefficients(teams, arm, 'generated') for arm in ('control', 'candidate'))
    control, c_trials = estimate(root, gains, a)
    candidate, p_trials = estimate(root, gains, b)
    difference, _ = estimate(root, gains, [y-x for x, y in zip(a, b)])
    result = dict(control=control, candidate=candidate, difference=difference)
    for arm in ('control', 'candidate'):
        for label, field in (('Nominees', 'nomineeRank'), ('Challenger', 'validationChallenger')):
            result[arm+label] = estimate(root, gains, coefficients(teams, arm, field))[0]
    result.update(samplingCovariance=sampling_covariance(root, gains, a, b), combatCovariance=covariance(c_trials, p_trials)/N)
    return result


def average_roots(roots):
    estimates = [r['estimates'] for r in roots]
    result = {}
    for name in ('control', 'candidate', 'difference', 'controlNominees', 'candidateNominees', 'controlChallenger', 'candidateChallenger'):
        rows = [e[name] for e in estimates]
        sampling, combat = (sum(r[k] for r in rows)/144 for k in ('samplingVariance', 'combatVariance'))
        result[name] = dict(mean=sum(r['mean'] for r in rows)/12, samplingVariance=sampling, samplingStandardError=math.sqrt(sampling),
                            combatVariance=combat, combatStandardError=math.sqrt(combat))
    for name in ('samplingCovariance', 'combatCovariance'): result[name] = sum(e[name] for e in estimates)/144
    return result


def endpoint(plan, rows, panel):
    teams = [t for root in plan['roots'] for t in root['teams']]
    require(plan['version'] == 'tower-affinity-preservation-recognition-plan-v1'
            and [r['root'] for r in plan['roots']] == list(range(1, 13))
            and tuple(len(r['teams']) for r in plan['roots']) == ROOT_SIZES
            and len(rows) == len(teams) == 145 and len(panel) == len(set(panel)) == PANEL, 'Incomplete paired family')
    wins, rates, contrasts, strata, unmeasured, roots, cells = [], [], [], [], [], [], []
    for i, (team, row) in enumerate(zip(teams, rows, strict=True)):
        r = root_index(i)
        require(row['partyId'] == cell_id(i, team) and len(row['trials']) == N, 'Changed root cell identity')
        values = []
        for seed, trial in zip(panel[r*N:(r+1)*N], row['trials'], strict=True):
            require(trial.keys() == {'seed','outcome'} and type(trial['seed']) is int and trial['seed'] == seed
                    and trial['outcome'] in ('Victory','Defeat','Draw'), 'Changed root panel or outcome')
            values.append(trial['outcome'] == 'Victory')
        wins.append(values)
        rates.append(dict(root=r+1, partyId=team['partyId'], stratum=team['stratum'], wins=sum(values), estimate=wilson(sum(values))))
    start = 0
    for r, root in enumerate(plan['roots']):
        local = root['teams']
        benchmark = next(i for i, t in enumerate(local) if t['partyId'] == root['benchmarkPartyId'])
        require(benchmark < 3, 'Lost paired benchmark')
        gains = [[float(a)-float(b) for a,b in zip(wins[start+i],wins[start+benchmark])] for i in range(len(local))]
        for c in range(3,len(local)):
            for reference in range(3):
                pairs = list(zip(wins[start+c],wins[start+reference],strict=True))
                g, l = sum(a and not b for a,b in pairs), sum(b and not a for a,b in pairs)
                gi, li = wilson(g), wilson(l)
                contrasts.append(dict(root=r+1,candidateId=local[c]['partyId'],referenceId=local[reference]['partyId'],stratum=local[c]['stratum'],
                    gains=g,losses=l,observedGain=(g-l)/N,lower=gi['lower']-li['upper'],upper=gi['upper']-li['lower']))
        for name in ('mandatory', 'remaining-shared', 'remaining-control-only', 'remaining-candidate-only'):
            indices = [i for i,t in enumerate(local) if t['stratum'] == name]
            n = len(indices) if name == 'mandatory' else root['strata'][name]['populationCount']
            total = sum(sum(gains[i])/N for i in indices)
            weight = n/len(indices) if indices else 0.
            strata.append(dict(root=r+1,stratum=name,measured=len(indices),population=n,inclusionWeight=weight,
                meanGain=total/len(indices) if indices else 0.,estimatedTotalGain=total*weight))
        roots.append(dict(root=r+1,benchmarkPartyId=root['benchmarkPartyId'],estimates=root_estimates(root,gains)))
        for item in root['unmeasured']:
            require(item['independentOutcome'] is None, 'Imputed unknown outcome')
            unmeasured.append(dict(root=r+1,partyId=item['partyId'],stratum=item['stratum'],independentOutcome=None))
        for items, measured in ((local,True),(root['unmeasured'],False)):
            cells.extend(dict(root=r+1,partyId=t['partyId'],stratum=t['stratum'],membership=t['membership'],measured=measured,
                inclusionProbability=t['inclusionProbability'],provenance=t['provenance']) for t in items)
        start += len(local)
    return dict(version=VERSION,executionStatus='Complete',integrityStatus='Verified',decision='CompleteDiagnosticOnly',
        interpretation='DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion',policyDefaultsChanged=False,samplesPerTeam=N,
        approximateWilsonFamily=FAMILY,rates=rates,contrasts=contrasts,strata=strata,populations=[],unmeasured=unmeasured,limitations=LIMITATIONS,
        pairedPool=dict(roots=roots,allRoots=average_roots(roots),cells=cells))


def same(actual, expected):
    if isinstance(expected, float):
        return type(actual) in (int,float) and math.isfinite(actual) and math.isclose(actual, expected, abs_tol=1e-13, rel_tol=1e-12)
    if isinstance(expected, dict):
        return isinstance(actual, dict) and actual.keys() == expected.keys() and all(same(actual[k],v) for k,v in expected.items())
    if isinstance(expected, list):
        return isinstance(actual,list) and len(actual) == len(expected) and all(same(a,b) for a,b in zip(actual,expected,strict=True))
    return type(actual) is type(expected) and actual == expected


def audit_rows(root, teams, panel):
    """Reconstruct ordered rows from each direct terminal report, never from saved winners."""
    trials_path = root/'trials.jsonl'
    require(trials_path.read_bytes().endswith(b'\n'), 'Torn trial journal')
    trials = [json.loads(line) for line in trials_path.read_text().splitlines()]
    require(len(trials) == 37120, 'Incomplete direct reports')
    rows, keys, recipes = [], set(), set()
    for team_no, team in enumerate(teams):
        paired = panel[root_index(team_no)*N:(root_index(team_no)+1)*N]
        row = []
        transport = []
        for start in range(0,N,1000):
            scenario = dict(team['scenario'],seeds=paired[start:min(N,start+1000)])
            recipe = digest(scenario)
            require(read(root/'recipes'/f'{recipe}.json') == scenario, 'Changed transport scenario')
            recipes.add(recipe)
            transport.append((scenario,recipe))
        for ordinal, seed in enumerate(paired):
            scenario, recipe = transport[ordinal//1000]
            number = team_no*N+ordinal+1
            trial = trials[number-1]
            require(trial.keys() == {'id','stage','recipe','seed','inputHash','cacheKey'} and trial['id'] == f'trial-{number:06}'
                    and trial['stage'] == 'confirmation' and trial['recipe'] == recipe and type(trial['seed']) is int and trial['seed'] == seed,
                    'Changed trial ordinal, seed, stage or recipe')
            require(all(isinstance(trial[k],str) and len(trial[k]) == 64 and all(c in '0123456789abcdef' for c in trial[k]) for k in ('inputHash','cacheKey'))
                    and trial['cacheKey'] not in keys, 'Invalid or reused input/cache binding')
            keys.add(trial['cacheKey'])
            with gzip.open(root/'battles'/f"{trial['id']}.json.gz", 'rt', encoding='utf-8-sig') as stream:
                report = json.load(stream)
            outcome = report['battle']['summary']['contentOutcome']
            require(type(report['battle']['seed']) is int and report['battle']['seed'] == seed and report['battle']['scenarioId'] == scenario['id']
                    and outcome in ('Victory','Defeat','Draw') and report['succeeded'] is (outcome == 'Victory'), 'Invalid terminal report')
            row.append(dict(seed=seed,outcome=outcome))
        rows.append(dict(partyId=cell_id(team_no,team),trials=row))
    require({p.name for p in (root/'recipes').iterdir()} == {p+'.json' for p in recipes}, 'Changed recipe inventory')
    require({p.name for p in (root/'battles').iterdir()} == {f'trial-{i:06}.json.gz' for i in range(1,37121)}, 'Changed report inventory')
    return rows


def verify_journals(root, study, binding, intent):
    path = root/'attempts.jsonl'
    require(path.read_bytes().endswith(b'\n'), 'Torn attempt journal')
    rows = [json.loads(line) for line in path.read_text().splitlines()]
    require(len(rows) == 74240, 'Incomplete attempts')
    for i, row in enumerate(rows):
        require(row == dict(kind='Started' if i%2 == 0 else 'Completed',ordinal=i//2+1), 'Changed attempt order')
    expected = [('RecipesFrozen',digest(study['freeze']),0),('EntropyStarted',digest(intent),0),
                ('EntropyCompleted',binding['entropyHash'],0),('ConfirmationReserved',digest(binding),0),
                ('ConfirmationStarted',digest(binding['panel']),0),('MeasurementCompleted',digest(study),37120)]
    path = root/'events.jsonl'
    require(path.read_bytes().endswith(b'\n'), 'Torn events')
    require([json.loads(line) for line in path.read_text().splitlines()] ==
            [dict(ordinal=i+1,kind=k,hash=h,completedAttempts=c) for i,(k,h,c) in enumerate(expected)], 'Changed event sequence')


def audit(root):
    require(not (root/'failure.json').exists() and sha(root/'plan.json') == PLAN, 'Failed study or changed frozen plan')
    q, study, plan = read(root/'request.json'), read(root/'study/study.json'), read(root/'plan.json')
    require(q['version'] == VERSION and q['priorSeconds'] in (600,1200) and q['priorBytes'] == q['priorSeconds']//600*536870912
            and q['maximumSeconds'] == q['priorSeconds']+10800 and q['maximumBytes'] == q['priorBytes']+6442450944 and q['phases'] == {}
            and sha(root/'auditor.py') == q['recognition']['auditorHash'], 'Changed request envelope or auditor')
    definition = read(root/'source-definition.json')
    require(sha(root/'source-definition.json') == q['definitionHash'], 'Changed frozen definition')
    plan_teams = [t for r in plan['roots'] for t in r['teams']]
    teams = [dict(role=t['stratum'],partyId=t['partyId'],referenceIds=[],scenario=t['scenario']) for t in plan_teams]
    require(definition['version'] == VERSION and definition['teams'] == teams and definition['contentHashes'] == plan['capturedRuntime']['contentHashes']
            and definition['settingsHash'] == plan['capturedRuntime']['settingsHash'], 'Changed family or captured scope')
    freeze = dict(version=VERSION,requestHash=digest(q),definitionHash=q['definitionHash'],definition=definition)
    require(read(root/'freeze.json') == freeze == read(root/'study/freeze.json') == study['freeze'] and study['version'] == VERSION, 'Changed frozen inputs')
    entropy = (root/'entropy.bin').read_bytes()
    words, panel, fresh = classify(entropy, definition['excludedCombatSeeds'])
    require(len(panel) == PANEL, 'Short entropy batch; cannot qualify')
    binding = dict(version=VERSION,freezeHash=digest(freeze),entropyHash=hashlib.sha256(entropy).hexdigest(),historicalHash=digest(definition['excludedCombatSeeds']),
                   words=words,panel=panel,newReservations=fresh)
    require(read(root/'confirmation-binding.json') == binding and read(root/'history-input.json') == dict(reservationState='Complete',reserved=fresh)
            and read(root/'seed-ledger.json') == dict(reservationState='Complete',historical=definition['excludedCombatSeeds'],reserved=fresh), 'Changed complete reservation ledger')
    intent = dict(version=VERSION,requestHash=digest(q),freezeHash=digest(freeze),bytes=24576,historicalHash=binding['historicalHash'])
    require(read(root/'entropy-intent.json') == intent == read(root/'entropy-start.json')
            and read(root/'entropy-complete.json') == dict(version=VERSION,entropyHash=binding['entropyHash'],bytes=24576), 'Changed entropy transaction')
    authenticate(root/'study')
    scope = read(root/'study/scope.json')
    require(scope['algorithm'] == VERSION and scope['reportStorage'] == 'gzip-json-v1' and scope['contentHashes'] == definition['contentHashes']
            and digest(scope['settings']) == definition['settingsHash'] and digest(scope['execution']) == definition['executionHash'], 'Changed runtime/settings scope')
    rows = audit_rows(root/'study', teams, panel)
    require(study['evidence'] == rows, 'Saved evidence differs from direct reports')
    verify_journals(root, study, binding, intent)
    chunks = [dict(teamOrdinal=i,sliceOrdinal=0,partyId=cell_id(i,t),seedFreeHash=digest(t['scenario']),
                   scenario=dict(t['scenario'],seeds=panel[root_index(i)*N:(root_index(i)+1)*N])) for i,t in enumerate(teams)]
    require(read(root/'chunks.json') == chunks, 'Changed root transports')
    exports = root/'study/exports'
    require({p.name for p in exports.iterdir()} == {cell_id(i,t)+'.json' for i,t in enumerate(teams)}, 'Changed export membership')
    for i,t in enumerate(teams): require(read(exports/(cell_id(i,t)+'.json')) == t['scenario'], 'Changed exported scenario')
    require(read(root/'prior-charges.json') == q['priorCharges'] and sum(c['seconds'] for c in q['priorCharges']) == q['priorSeconds']
            and sum(c['bytes'] for c in q['priorCharges']) == q['priorBytes'], 'Changed admission charges')
    for i,c in enumerate(q['priorCharges']): require(sha(root/'prior-charges'/f'{i:03}.json') == c['receiptHash'], 'Changed charge receipt')
    history = read(root/'history-files.json')
    require(all(history.get(k) == v for k,v in q['requiredHistory'].items()), 'Lost history pin')
    result = endpoint(plan, rows, panel)
    result.update(studyHash=digest(study),archiveHash=sha(root/'study/files.json'))
    saved = read(root/'provisional-result.json')
    require(same(saved,result), 'Independent endpoint differs')
    # Return native values only after every result field has been independently checked.
    return dict(status='Passed',requestFileHash=sha(root/'request.json'),newFights=0,newValues=0,result=saved)


def write_receipt(output, audited, native_result_text):
    # The native canonical hash preserves number tokens (including exponent case).
    # Python float re-encoding would change tiny covariance/variance values even
    # when they round-trip to identical doubles. Embed the checked original JSON.
    require(json.loads(native_result_text) == audited['result'], 'Result changed after independent audit')
    metadata = {k:v for k,v in audited.items() if k != 'result'}
    with output.open('x',encoding='utf-8',newline='\n') as stream:
        stream.write(json.dumps(metadata,indent=2)[:-1]+',\n  "result": '+native_result_text+'\n}\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--working',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    args = parser.parse_args()
    result = audit(args.working)
    write_receipt(args.output,result,(args.working/'provisional-result.json').read_text(encoding='utf-8-sig'))
