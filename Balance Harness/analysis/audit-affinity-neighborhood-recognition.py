"""Independent direct-report recognition audit; no native calls, combat or entropy source."""
import argparse
from fractions import Fraction
import gzip
import hashlib
import json
import math
from pathlib import Path
import struct

VERSION = 'tower-affinity-neighborhood-recognition-v1'
PLAN = 'a0b4a5dbc8fe7c65d4df80bfcb17b8f5623343cef7e0c1a1f2506648b3e2fc2b'
N, CANDIDATES, FAMILY = 2048, 41, 46
PANEL = 2048
LIMITATIONS = "Complete captured finite neighborhood only. Equal distinct-recipe means are not proposer output performance. Two-sided Hoeffding bounds use one family of 46 fixed endpoints at alpha 0.05 over uniform fresh seeds without replacement. All 44 win rates are descriptive. The conservative 2048-value panel is not a high-power test of a true three-point gain. Report every generated recipe whose lower bound reaches three points; never promote, impute, extend, retry or generalize to future search roots."


def require(ok, message):
    if not ok:
        raise ValueError(message)


def unique(pairs):
    value = {}
    for key, item in pairs:
        require(key not in value, 'Duplicate JSON key: '+key)
        value[key] = item
    return value


def decode(text):
    return json.loads(text, object_pairs_hook=unique,
                      parse_constant=lambda value: require(False, 'Nonfinite JSON number: '+value))


def read(path):
    return decode(path.read_text(encoding='utf-8-sig'))


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
    require(len(entropy) == 65536 and history == sorted(set(history)) and 0 < len(history) <= 983616,
            'Invalid entropy/history input')
    require(all(type(s) is int and -2**31 <= s < 2**31 for s in history), 'Invalid historical value')
    prior, seen, words, fresh = set(history), set(), [], []
    for i, value in enumerate(struct.unpack('<16384i', entropy)):
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


def root_index(ordinal):
    require(type(ordinal) is int and 0 <= ordinal < 44, 'Invalid cell ordinal')
    return 0


def cell_id(i, team):
    root_index(i)
    return team['partyId']


def endpoint(plan, rows, panel):
    teams = plan['teams']
    require(plan['version'] == 'tower-affinity-neighborhood-recognition-plan-v1'
            and plan['samplesPerTeam'] == N and len(rows) == len(teams) == 44
            and len(panel) == len(set(panel)) == PANEL
            and all(type(s) is int and -2**31 <= s < 2**31 for s in panel), 'Incomplete neighborhood family')
    ids = [t['partyId'] for t in teams]
    old = [t['partyId'] for t in teams if t['membership'] != 'reference']
    subset = [t['partyId'] for t in teams if t['membership'] == 'shared']
    benchmark = plan['benchmarkPartyId']
    require(len(set(ids)) == 44 and len(old) == 41 and len(subset) == 26 and set(subset) < set(old)
            and benchmark in ids[:3] and not set(ids[:3]) & set(old), 'Changed neighborhood membership')
    wins = {}
    for team, row in zip(teams, rows, strict=True):
        require(row['partyId'] == team['partyId'] and len(row['trials']) == N, 'Changed physical cell identity')
        values = []
        for seed, trial in zip(panel, row['trials'], strict=True):
            require(trial.keys() == {'seed','outcome'} and type(trial['seed']) is int and trial['seed'] == seed
                    and trial['outcome'] in ('Victory','Defeat','Draw'), 'Changed common panel or outcome')
            values.append(trial['outcome'] == 'Victory')
        wins[team['partyId']] = sum(values)
    # Reconstruct coefficients independently; never use saved intervals or decisions.
    coefficients = {pid:{pid:Fraction(1),benchmark:Fraction(-1)} for pid in ids if pid != benchmark}
    for label, members in [('controlMean',old),('candidateMean',subset)]:
        coefficients[label] = {pid:Fraction(1,len(members)) for pid in members}
        coefficients[label][benchmark] = Fraction(-1)
    coefficients['candidateMinusControlMean'] = {pid:Fraction(pid in subset,len(subset))-Fraction(1,len(old)) for pid in old}
    endpoints = []
    require(len(coefficients) == 46, 'Changed simultaneous family')
    for key, c in coefficients.items():
        low, high = sum(v for v in c.values() if v < 0), sum(v for v in c.values() if v > 0)
        mean = float(sum(v*wins[pid] for pid,v in c.items())/N)
        radius = float(high-low)*math.sqrt(math.log(2*FAMILY/.05)/(2*N))
        endpoints.append(dict(id=key,mean=mean,lower=max(float(low),mean-radius),upper=min(float(high),mean+radius)))
    generated = [e for e in endpoints if e['id'] in old]
    eligible = sorted(e['id'] for e in generated if e['lower'] >= .03)
    status = ('FreshConfirmationWarranted' if eligible else 'RetireBelowPracticalThreshold'
              if all(e['upper'] < .03 for e in generated) else 'RetireUnresolvedAtBudget')
    outcome = dict(version='tower-affinity-neighborhood-recognition-outcome-v1',status=status,eligiblePartyIds=eligible,
        wins=wins,endpoints=endpoints,promoted=False,additionalSamples=0,interpretation='FiniteCapturedNeighborhoodOnly')
    return dict(version=VERSION,executionStatus='Complete',integrityStatus='Verified',decision=status,
        interpretation='FiniteCapturedNeighborhoodOnly',policyDefaultsChanged=False,samplesPerTeam=N,
        approximateWilsonFamily=0,rates=[],contrasts=[],strata=[],populations=[],unmeasured=[],limitations=LIMITATIONS,neighborhood=outcome)


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
    trials = [decode(line) for line in trials_path.read_text().splitlines()]
    require(len(trials) == 90112, 'Incomplete direct reports')
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
                report = decode(stream.read())
            outcome = report['battle']['summary']['contentOutcome']
            require(type(report['battle']['seed']) is int and report['battle']['seed'] == seed and report['battle']['scenarioId'] == scenario['id']
                    and outcome in ('Victory','Defeat','Draw') and report['succeeded'] is (outcome == 'Victory'), 'Invalid terminal report')
            row.append(dict(seed=seed,outcome=outcome))
        rows.append(dict(partyId=cell_id(team_no,team),trials=row))
    require({p.name for p in (root/'recipes').iterdir()} == {p+'.json' for p in recipes}, 'Changed recipe inventory')
    require({p.name for p in (root/'battles').iterdir()} == {f'trial-{i:06}.json.gz' for i in range(1,90113)}, 'Changed report inventory')
    return rows


def verify_journals(root, study, binding, intent):
    path = root/'attempts.jsonl'
    require(path.read_bytes().endswith(b'\n'), 'Torn attempt journal')
    rows = [decode(line) for line in path.read_text().splitlines()]
    require(len(rows) == 180224, 'Incomplete attempts')
    for i, row in enumerate(rows):
        require(row == dict(kind='Started' if i%2 == 0 else 'Completed',ordinal=i//2+1), 'Changed attempt order')
    expected = [('RecipesFrozen',digest(study['freeze']),0),('EntropyStarted',digest(intent),0),
                ('EntropyCompleted',binding['entropyHash'],0),('ConfirmationReserved',digest(binding),0),
                ('ConfirmationStarted',digest(binding['panel']),0),('MeasurementCompleted',digest(study),90112)]
    path = root/'events.jsonl'
    require(path.read_bytes().endswith(b'\n'), 'Torn events')
    require([decode(line) for line in path.read_text().splitlines()] ==
            [dict(ordinal=i+1,kind=k,hash=h,completedAttempts=c) for i,(k,h,c) in enumerate(expected)], 'Changed event sequence')


def audit(root):
    require(not (root/'failure.json').exists() and sha(root/'plan.json') == PLAN, 'Failed study or changed frozen plan')
    q, study, plan = read(root/'request.json'), read(root/'study/study.json'), read(root/'plan.json')
    require(q['version'] == VERSION and q['priorSeconds'] == 1800 and q['priorBytes'] == 536870912
            and q['maximumSeconds'] == q['priorSeconds']+18000 and q['maximumBytes'] == q['priorBytes']+18253611008 and q['phases'] == {}
            and sha(root/'auditor.py') == q['recognition']['auditorHash'], 'Changed request envelope or auditor')
    definition = read(root/'source-definition.json')
    require(sha(root/'source-definition.json') == q['definitionHash'], 'Changed frozen definition')
    plan_teams = plan['teams']
    teams = [dict(role=t['membership'],partyId=t['partyId'],referenceIds=[],scenario=t['scenario']) for t in plan_teams]
    require(definition['version'] == VERSION and definition['teams'] == teams and definition['contentHashes'] == plan['sourceContract']['capturedRuntime']['contentHashes']
            and definition['settingsHash'] == plan['sourceContract']['capturedRuntime']['settingsHash'], 'Changed family or captured scope')
    freeze = dict(version=VERSION,requestHash=digest(q),definitionHash=q['definitionHash'],definition=definition)
    require(read(root/'freeze.json') == freeze == read(root/'study/freeze.json') == study['freeze'] and study['version'] == VERSION, 'Changed frozen inputs')
    entropy = (root/'entropy.bin').read_bytes()
    words, panel, fresh = classify(entropy, definition['excludedCombatSeeds'])
    require(len(panel) == PANEL, 'Short entropy batch; cannot qualify')
    binding = dict(version=VERSION,freezeHash=digest(freeze),entropyHash=hashlib.sha256(entropy).hexdigest(),historicalHash=digest(definition['excludedCombatSeeds']),
                   words=words,panel=panel,newReservations=fresh)
    require(read(root/'confirmation-binding.json') == binding and read(root/'history-input.json') == dict(reservationState='Complete',reserved=fresh)
            and read(root/'seed-ledger.json') == dict(reservationState='Complete',historical=definition['excludedCombatSeeds'],reserved=fresh), 'Changed complete reservation ledger')
    intent = dict(version=VERSION,requestHash=digest(q),freezeHash=digest(freeze),bytes=65536,historicalHash=binding['historicalHash'])
    require(read(root/'entropy-intent.json') == intent == read(root/'entropy-start.json')
            and read(root/'entropy-complete.json') == dict(version=VERSION,entropyHash=binding['entropyHash'],bytes=65536), 'Changed entropy transaction')
    authenticate(root/'study')
    scope = read(root/'study/scope.json')
    require(scope['algorithm'] == VERSION and scope['reportStorage'] == 'gzip-json-v1' and scope['contentHashes'] == definition['contentHashes']
            and digest(scope['settings']) == definition['settingsHash'] and digest(scope['execution']) == definition['executionHash'], 'Changed runtime/settings scope')
    rows = audit_rows(root/'study', teams, panel)
    require(study['evidence'] == rows, 'Saved evidence differs from direct reports')
    verify_journals(root, study, binding, intent)
    chunks = [dict(teamOrdinal=i,sliceOrdinal=s,partyId=cell_id(i,t),seedFreeHash=digest(t['scenario']),
                   scenario=dict(t['scenario'],seeds=panel[start:min(N,start+1000)]))
              for i,t in enumerate(teams) for s,start in enumerate(range(0,N,1000))]
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
