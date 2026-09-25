"""Independent direct-report recognition audit; no native calls, combat or entropy source."""
import argparse
import gzip
import hashlib
import json
import math
from pathlib import Path
import struct

VERSION = 'tower-frozen-pool-recognition-v1'
PLAN = 'f8e8b206cd6b0cf46f420ab4d6d4d4a9568e5e46186ae8a8ae18a552fa0957b8'
N, CANDIDATES, FAMILY = 256, 6, 540
PANEL = 3072
LIMITATIONS = "Development diagnosis of twelve frozen roots only. Approximate family-540 Wilson intervals describe combat uncertainty. Lower-stratum sampling uncertainty is separate; weighted population summaries are point estimates without population confidence intervals. Unmeasured outcomes remain null. No qualification, adoption, future-root reliability or default-policy change."
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


def endpoint(plan, rows, panel):
    teams = [t for root in plan['roots'] for t in root['teams']]
    require(len(rows) == len(teams) == 108 and len(panel) == len(set(panel)) == PANEL, 'Incomplete root family')
    wins, rates, contrasts, strata, populations, unmeasured = [], [], [], [], [], []
    for i, (team, row) in enumerate(zip(teams, rows, strict=True)):
        require(row['partyId'] == cell_id(i, team) and len(row['trials']) == N, 'Changed root cell identity')
        values = []
        for seed, trial in zip(panel[i//9*N:(i//9+1)*N], row['trials'], strict=True):
            require(trial.keys() == {'seed','outcome'} and type(trial['seed']) is int and trial['seed'] == seed
                    and trial['outcome'] in ('Victory','Defeat','Draw'), 'Changed root panel or outcome')
            values.append(trial['outcome'] == 'Victory')
        wins.append(values)
        rates.append(dict(root=i//9+1,partyId=team['partyId'],stratum=team['stratum'],wins=sum(values),estimate=wilson(sum(values))))
    for r, root in enumerate(plan['roots']):
        for c in range(3,9):
            for reference in range(3):
                ci, ri = r*9+c, r*9+reference
                gains = sum(a and not b for a,b in zip(wins[ci],wins[ri],strict=True))
                losses = sum(b and not a for a,b in zip(wins[ci],wins[ri],strict=True))
                g, l = wilson(gains), wilson(losses)
                contrasts.append(dict(root=r+1,candidateId=teams[ci]['partyId'],referenceId=teams[ri]['partyId'],stratum=teams[ci]['stratum'],
                    gains=gains,losses=losses,observedGain=(gains-losses)/N,lower=g['lower']-l['upper'],upper=g['upper']-l['lower']))
        for name, population in [('nominee',2),('near-miss',2),('lower',13)]:
            total = sum(c['observedGain'] for c in contrasts if c['root'] == r+1 and c['stratum'] == name and c['referenceId'] == root['benchmarkPartyId'])
            strata.append(dict(root=r+1,stratum=name,measured=2,population=population,inclusionWeight=population/2,
                               meanGain=total/2,estimatedTotalGain=total*population/2))
        populations.append(dict(root=r+1,benchmarkPartyId=root['benchmarkPartyId'],
                                estimatedCandidateMeanGain=sum(s['estimatedTotalGain'] for s in strata if s['root'] == r+1)/17))
        unmeasured.extend(dict(root=r+1,partyId=t['partyId'],stratum='lower',independentOutcome=None) for t in root['unmeasured'])
    return dict(version=VERSION,executionStatus='Complete',integrityStatus='Verified',decision='CompleteDiagnosticOnly',
        interpretation='DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion',policyDefaultsChanged=False,samplesPerTeam=N,
        approximateWilsonFamily=FAMILY,rates=rates,contrasts=contrasts,strata=strata,populations=populations,unmeasured=unmeasured,limitations=LIMITATIONS)


def cell_id(i, team):
    return f"r{i//9+1:02}-"+team['partyId']


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
    require(len(trials) == 27648, 'Incomplete direct reports')
    rows, keys, recipes = [], set(), set()
    for team_no, team in enumerate(teams):
        paired = panel[team_no//9*N:(team_no//9+1)*N]
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
    require({p.name for p in (root/'battles').iterdir()} == {f'trial-{i:06}.json.gz' for i in range(1,27649)}, 'Changed report inventory')
    return rows


def verify_journals(root, study, binding, intent):
    path = root/'attempts.jsonl'
    require(path.read_bytes().endswith(b'\n'), 'Torn attempt journal')
    rows = [json.loads(line) for line in path.read_text().splitlines()]
    require(len(rows) == 55296, 'Incomplete attempts')
    for i, row in enumerate(rows):
        require(row == dict(kind='Started' if i%2 == 0 else 'Completed',ordinal=i//2+1), 'Changed attempt order')
    expected = [('RecipesFrozen',digest(study['freeze']),0),('EntropyStarted',digest(intent),0),
                ('EntropyCompleted',binding['entropyHash'],0),('ConfirmationReserved',digest(binding),0),
                ('ConfirmationStarted',digest(binding['panel']),0),('MeasurementCompleted',digest(study),27648)]
    path = root/'events.jsonl'
    require(path.read_bytes().endswith(b'\n'), 'Torn events')
    require([json.loads(line) for line in path.read_text().splitlines()] ==
            [dict(ordinal=i+1,kind=k,hash=h,completedAttempts=c) for i,(k,h,c) in enumerate(expected)], 'Changed event sequence')


def audit(root):
    require(not (root/'failure.json').exists() and sha(root/'plan.json') == PLAN, 'Failed study or changed frozen plan')
    q, study, plan = read(root/'request.json'), read(root/'study/study.json'), read(root/'plan.json')
    require(q['version'] == VERSION and q['priorSeconds'] in (600,1200) and q['priorBytes'] == q['priorSeconds']//600*536870912
            and q['maximumSeconds'] == q['priorSeconds']+7200 and q['maximumBytes'] == q['priorBytes']+3758096384 and q['phases'] == {}
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
                   scenario=dict(t['scenario'],seeds=panel[i//9*N:(i//9+1)*N])) for i,t in enumerate(teams)]
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
    # Preserve native JSON number encodings only after every result field has been independently checked.
    return dict(status='Passed',requestFileHash=sha(root/'request.json'),newFights=0,newValues=0,result=saved)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--working',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    args = parser.parse_args()
    result = audit(args.working)
    with args.output.open('x',encoding='utf-8',newline='\n') as stream:
        json.dump(result,stream,indent=2)
