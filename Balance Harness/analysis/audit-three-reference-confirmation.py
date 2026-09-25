"""Independent direct-report confirmation audit; no native calls, combat or entropy source."""
import argparse
import gzip
import hashlib
import json
import math
from pathlib import Path
import struct

VERSION = 'tower-practical-three-reference-confirmation-v1'
PLAN = 'c6544a63a569de5197c4e2a35749c01a14388478e6a9986509d991c64bd6a74f'
N, CANDIDATES, FAMILY = 6500, 5, 38
Z = 3.2125135346835694


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
    require(len(entropy) == 52000 and history == sorted(set(history)) and 0 < len(history) <= 987000,
            'Invalid entropy/history input')
    require(all(type(s) is int and -2**31 <= s < 2**31 for s in history), 'Invalid historical value')
    prior, seen, words, fresh = set(history), set(), [], []
    for i, value in enumerate(struct.unpack('<13000i', entropy)):
        if value in prior:
            kind = 'AlreadyReserved'
        elif value in seen:
            kind = 'DuplicateBatch'
        else:
            kind = 'Confirmation' if len(fresh) < N else 'ReservedUnused'
            fresh.append(value)
            seen.add(value)
        words.append(dict(ordinal=i, value=value, classification=kind))
    return words, fresh[:N], fresh


def wilson(wins):
    require(type(wins) is int and 0 <= wins <= N, 'Invalid win count')
    p = wins/N
    denominator = 1+Z*Z/N
    center = (p+Z*Z/(2*N))/denominator
    width = Z*math.sqrt(p*(1-p)/N+Z*Z/(4*N*N))/denominator
    return dict(rate=p, lower=max(0, center-width), upper=min(1, center+width), confidence=1-.05/FAMILY)


def endpoint(ids, rows, panel):
    require(len(ids) == len(rows) == 8 and len(set(ids)) == 8 and len(panel) == N and len(set(panel)) == N, 'Incomplete fixed family')
    wins = []
    for party, row in zip(ids, rows, strict=True):
        require(row['partyId'] == party and len(row['trials']) == N, 'Changed row order or length')
        values = []
        for seed, trial in zip(panel, row['trials'], strict=True):
            require(trial.keys() == {'seed','outcome'} and type(trial['seed']) is int and trial['seed'] == seed
                    and trial['outcome'] in ('Victory','Defeat','Draw'), 'Changed ordinal, seed or terminal outcome')
            values.append(trial['outcome'] == 'Victory')
        wins.append(values)
    rates = [dict(partyId=party, wins=sum(row), estimate=wilson(sum(row))) for party, row in zip(ids, wins, strict=True)]
    contrasts, qualifiers = [], []
    for c in range(5):
        passed = True
        for r in range(5,8):
            gains = sum(wins[c][i] and not wins[r][i] for i in range(N))
            losses = sum(wins[r][i] and not wins[c][i] for i in range(N))
            g, l = wilson(gains), wilson(losses)
            qualifies = rates[c]['estimate']['lower'] >= .10 and gains-losses >= 325 and g['lower'] > l['upper']
            contrasts.append(dict(candidateId=ids[c], referenceId=ids[r], gains=gains, losses=losses,
                observedGain=(gains-losses)/N, lower=g['lower']-l['upper'], upper=g['upper']-l['lower'], qualifies=qualifies))
            passed &= qualifies
        if passed:
            qualifiers.append(ids[c])
    return dict(version=VERSION, executionStatus='Complete', integrityStatus='Verified',
        strengthDecision='StrongerFixedCandidatesConfirmed' if qualifiers else 'StrengthNotDemonstrated',
        adoption='RecommendFixedCandidates' if qualifiers else 'Hold', candidateIds=ids[:5], qualifyingPartyIds=qualifiers,
        rates=rates, contrasts=contrasts, recommendedPartyIds=qualifiers+ids[5:], controlPartyIds=ids[5:], balanceAssessment='NotAssessed',
        samplingAssumption='One post-freeze cryptographic batch modeled as independent uniform bits; approximate family-38 Wilson coverage; operational completion is not assumed.',
        stopReason='One complete fixed panel; report every qualifier in frozen order; no new primary, retry, extension, method-reliability or global-optimality claim.')


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
    require(len(trials) == 52000, 'Incomplete direct reports')
    rows, keys, recipes = [], set(), set()
    for team_no, team in enumerate(teams):
        row = []
        transport = []
        for start in range(0,N,1000):
            scenario = dict(team['scenario'],seeds=panel[start:min(N,start+1000)])
            recipe = digest(scenario)
            require(read(root/'recipes'/f'{recipe}.json') == scenario, 'Changed transport scenario')
            recipes.add(recipe)
            transport.append((scenario,recipe))
        for ordinal, seed in enumerate(panel):
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
        rows.append(dict(partyId=team['partyId'],trials=row))
    require({p.name for p in (root/'recipes').iterdir()} == {p+'.json' for p in recipes}, 'Changed recipe inventory')
    require({p.name for p in (root/'battles').iterdir()} == {f'trial-{i:06}.json.gz' for i in range(1,52001)}, 'Changed report inventory')
    return rows


def audit(root):
    require(not (root/'failure.json').exists() and sha(root/'plan.json') == PLAN, 'Failed study or changed frozen plan')
    q, study, plan = read(root/'request.json'), read(root/'study/study.json'), read(root/'plan.json')
    require(q['version'] == VERSION and q['maximumSeconds'] == 7800 and q['maximumBytes'] == 4294967296
            and q['priorSeconds'] == 600 and q['priorBytes'] == 536870912 and q['phases'] == {}
            and sha(root/'auditor.py') == q['threeReference']['auditorHash'], 'Changed request envelope or auditor')
    definition = read(root/'source-definition.json')
    require(sha(root/'source-definition.json') == q['definitionHash'], 'Changed frozen definition')
    teams = [dict(role=t['role'], partyId=t['partyId'], referenceIds=[t['referenceId']] if 'referenceId' in t else [], scenario=t['scenario'])
             for t in plan['recipesInFixedExecutionOrder']]
    require(definition['version'] == VERSION and definition['teams'] == teams and definition['contentHashes'] == plan['contentHashes']
            and definition['settingsHash'] == plan['settingsHash'], 'Changed family or captured scope')
    freeze = dict(version=VERSION,requestHash=digest(q),definitionHash=q['definitionHash'],definition=definition)
    require(read(root/'freeze.json') == freeze == read(root/'study/freeze.json') == study['freeze'] and study['version'] == VERSION, 'Changed frozen inputs')
    entropy = (root/'entropy.bin').read_bytes()
    words, panel, fresh = classify(entropy, definition['excludedCombatSeeds'])
    require(len(panel) == N, 'Short entropy batch; cannot qualify')
    binding = dict(version=VERSION,freezeHash=digest(freeze),entropyHash=hashlib.sha256(entropy).hexdigest(),historicalHash=digest(definition['excludedCombatSeeds']),
                   words=words,panel=panel,newReservations=fresh)
    require(read(root/'confirmation-binding.json') == binding and read(root/'history-input.json') == dict(reservationState='Complete',reserved=fresh)
            and read(root/'seed-ledger.json') == dict(reservationState='Complete',historical=definition['excludedCombatSeeds'],reserved=fresh), 'Changed complete reservation ledger')
    intent = dict(version=VERSION,requestHash=digest(q),freezeHash=digest(freeze),bytes=52000,historicalHash=binding['historicalHash'])
    require(read(root/'entropy-intent.json') == intent == read(root/'entropy-start.json')
            and read(root/'entropy-complete.json') == dict(version=VERSION,entropyHash=binding['entropyHash'],bytes=52000), 'Changed entropy transaction')
    authenticate(root/'study')
    scope = read(root/'study/scope.json')
    require(scope['algorithm'] == VERSION and scope['reportStorage'] == 'gzip-json-v1' and scope['contentHashes'] == definition['contentHashes']
            and digest(scope['settings']) == definition['settingsHash'] and digest(scope['execution']) == definition['executionHash'], 'Changed runtime/settings scope')
    rows = audit_rows(root/'study', teams, panel)
    require(study['evidence'] == rows, 'Saved evidence differs from direct reports')
    result = endpoint([t['partyId'] for t in teams], rows, panel)
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
