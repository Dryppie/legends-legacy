"""Independent saved-row audit. Reads a pinned completed archive; never invokes native code.

The native verifier reconstructs candidate generation and prepared inputs. This auditor
independently authenticates files, reservations, both selection choices, direct outcomes,
the global barrier, pairing, accounting and conditional confidence arithmetic.
"""
import argparse
import copy
import gzip
import hashlib
import json
import math
from pathlib import Path
import struct

VERSION = 'tower-incumbent-tie-comparison-v1'
CAPTURE = '1a2da312f1cb7af7867f0d173c9fcdf8f2213afc36873240ed2b0da505eec314'
CAPTURE_TEMPLATE = 'cc0ae4e8ccacb09ad169386c3f1c25fc1de6c93f9485dd20fe59438f6122a31a'
PRIMARY = '399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b'
SECOND = '8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50'


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def authenticate(root, pin):
    require(sha(root/'files.json') == pin, 'Archive manifest differs from the supplied pin')
    files = read(root/'files.json')
    actual = set()
    for p in root.rglob('*'):
        require(not p.is_symlink() and not p.is_junction(), 'Linked archive path')
        if p.is_file():
            actual.add(p.relative_to(root).as_posix())
    require(actual == set(files) | {'files.json'}, 'Changed archive membership')
    for name, digest in files.items():
        path = root/name
        require(path.resolve().is_relative_to(root.resolve()) and sha(path) == digest, 'Changed file: '+name)
    return files


def classify(entropy, history):
    require(len(entropy) == 131072 and history == sorted(set(history)) and len(history) <= 967232, 'Invalid reservation input')
    seen, prior, fresh, collisions, duplicates = set(), set(history), [], 0, 0
    for word in struct.unpack('<32768i', entropy):
        if word in prior:
            collisions += 1
        elif word in seen:
            duplicates += 1
        else:
            seen.add(word)
            fresh.append(word)
    require(len(fresh) >= 24984, 'Entropy exhausted; no valid comparison')
    return fresh[:24984], sorted(fresh), collisions, duplicates


def endpoint(pairs, historical):
    require(len(pairs) == 24 and [p['restart'] for p in pairs] == list(range(1, 25)) and 0 <= historical <= 967232, 'Incomplete endpoint')
    k = sum(not p['identical'] for p in pairs)
    net = sum(p['gains'] - p['losses'] for p in pairs)
    positive = sum(p['gains'] > p['losses'] for p in pairs)
    n, denominator = k * 1000, 24000
    depletion = n * (n-1) / ((2**32-historical-984) * denominator)
    margin = math.sqrt(2*n*math.log(20))/denominator + depletion
    mean = net/denominator
    lower = max(-k/24, mean-margin)
    decision = ('NoSelectorDifferences' if k == 0 else 'SupportsIncumbentTieForFrozenOutputs'
                if net >= 240 and lower > 0 and positive >= 3 else 'DoNotPromoteIncumbentTie')
    return dict(version=VERSION, status='Verified', decision=decision, boundVersion='conditional-range-hoeffding-depletion-v1',
                pairs=pairs, activeRestarts=k, netWins=net, positiveRestarts=positive, denominator=denominator,
                meanDifference=mean, depletion=depletion, margin=margin, lowerBound=lower, fights=11904+2*n)


def same_numbers(actual, expected):
    if isinstance(expected, float):
        return type(actual) in (float, int) and math.isfinite(actual) and math.isclose(actual, expected, rel_tol=1e-12, abs_tol=1e-14)
    if isinstance(expected, dict):
        return isinstance(actual, dict) and actual.keys() == expected.keys() and all(same_numbers(actual[k], v) for k, v in expected.items())
    if isinstance(expected, list):
        return isinstance(actual, list) and len(actual) == len(expected) and all(same_numbers(a, b) for a, b in zip(actual, expected))
    return actual == expected


def audit_rows(root, template, values, primary):
    """The direct-row core is also exercised with literal report archives in backend tests."""
    study_root = root/'study'
    study, frozen = read(study_root/'study.json'), read(study_root/'outputs-freeze.json')
    require(study['version'] == frozen['version'] == VERSION and study['freeze'] == frozen, 'Changed global freeze')
    searches = frozen['searches']
    require([s['restart'] for s in searches] == list(range(1, 25)) and frozen['completedAttempts'] == 11904, 'Incomplete global search')
    raw_trials = (study_root/'trials.jsonl').read_bytes()
    require(raw_trials.endswith(b'\n'), 'Torn battle ledger')
    trials = [json.loads(line) for line in raw_trials.splitlines()]
    require([t['id'] for t in trials] == [f'trial-{i+1:06d}' for i in range(len(trials))], 'Reordered battle ledger')
    cursor, direct = 0, {}

    def consume(stage, seeds, scenario=None, expected_ids=None, expected_party=None):
        nonlocal cursor
        wins, rows = [], []
        for i, seed in enumerate(seeds):
            require(cursor < len(trials), 'Missing direct battle')
            trial = trials[cursor]
            cursor += 1
            require(trial['stage'] == stage and trial['seed'] == seed, 'Reordered or reused stage panel')
            if expected_ids is not None:
                require(trial['id'] == expected_ids[i], 'Measurement does not refer to direct battle')
            recipe = read(study_root/'recipes'/f"{trial['recipe']}.json")
            require(recipe['seeds'] == seeds, 'Wrong physical recipe panel')
            builds = {str(p['partySlot']): p['build']['essenceIds'] for p in recipe['party']}
            party_id = hashlib.sha256(json.dumps(builds, sort_keys=True, separators=(',', ':'), ensure_ascii=True).encode()).hexdigest()
            if expected_party is not None:
                require(party_id == expected_party, 'Measured party differs from the declared recipe')
            physical = copy.deepcopy(template['contexts'][0]['characterTemplates'])
            for actor in physical:
                actor['build']['essenceIds'] = builds[str(actor['partySlot'])]
                actor['build']['identityEssenceIds'] = [f'neutral-identity-slot-{i+1}' for i in range(template['budget']['essenceSlots'])]
            require(recipe['party'] == physical and recipe['floorNumber'] == template['budget']['priorityFloor']
                    and recipe['startsAt'] == template['startsAt'], 'Changed character identities, equipment or scope')
            if scenario is not None:
                require(recipe == dict(scenario, seeds=seeds), 'Confirmed an unselected physical recipe')
            with gzip.open(study_root/'battles'/f"{trial['id']}.json.gz", 'rt', encoding='utf-8') as stream:
                report = json.load(stream)
            battle, outcome = report['battle'], report['battle']['summary']['contentOutcome']
            require(battle['seed'] == seed and battle['scenarioId'] == recipe['id']
                    and outcome in ('Victory', 'Defeat', 'Draw') and report['succeeded'] == (outcome == 'Victory'), 'Invalid direct outcome')
            wins.append(outcome == 'Victory')
            rows.append(dict(seed=seed, outcome=outcome))
            direct[trial['id']] = (outcome, report['guardianHealthRemainingPercent'])
        return wins, rows

    for j, search in enumerate(searches):
        require(search == read(study_root/f'search-{j+1:02d}.json')
                and search['discovery'] == read(study_root/f'search-{j+1:02d}-discovery.json'), 'Changed saved search')
        discovery = search['discovery']
        require(discovery['status'] == 'Complete' and len(discovery['arms']) == 1, 'Incomplete discovery')
        arm = discovery['arms'][0]
        require(arm['seed'] == values[j*41] and len(arm['evaluations']) == 46 and len(arm['proposals']) <= 256, 'Wrong search width, root or attempt ceiling')
        for stage, measurements, seeds in [('discovery', arm['evaluations'], values[j*41+1:j*41+9]),
                                          ('selection', search['selection'], values[j*41+9:j*41+41])]:
            require(len(measurements) == (46 if stage == 'discovery' else 4), 'Incomplete measurements')
            for row in measurements:
                require(len(row['cells']) == 1, 'Changed contexts')
                cell = row['cells'][0]
                won, _ = consume(stage, seeds, expected_ids=cell['trials'], expected_party=row['id'])
                require(won == cell['clears'], 'Measurement differs from direct wins')
                require(math.isclose(cell['guardianHealth'], sum(direct[t][1] for t in cell['trials'])/len(seeds), abs_tol=1e-10), 'Changed selection health')
        shortlist = discovery['discoveryShortlist']
        rank = {p['id']: i for i, p in enumerate(shortlist)}
        require(len(rank) == 4 and {s['party']['id'] for s in template['starts']} <= rank.keys(), 'Missing protected teams')
        counts = {r['id']: sum(r['cells'][0]['clears']) for r in search['selection']}
        health = {r['id']: r['cells'][0]['guardianHealth'] for r in search['selection']}
        require(counts.keys() == rank.keys(), 'Selection family differs')
        best = min(counts, key=lambda p: (-counts[p], health[p] if counts[p] == 0 else 0, rank[p], p))
        maximum = max(counts.values())
        chosen = primary if maximum > 0 and counts[primary] == maximum and list(counts.values()).count(maximum) > 1 else best
        for key, party, policy in [('baseline', best, 'tower-staged-zero-win-health-v1'), ('candidate', chosen, 'tower-staged-incumbent-tie-v1')]:
            output = search[key]
            require(output['selector'] == policy and output['finalist']['party']['id'] == party and output['scenario']['seeds'] == [], 'Wrong real selector choice')
        # Exact scenario equality is sufficient here: both selectors share the same fixed context and canonical recipe construction.
        require((search['baseline']['scenario']['party'] == search['candidate']['scenario']['party'])
                == (search['baseline']['recipeHash'] == search['candidate']['recipeHash']), 'Changed physical recipe identity')
    require(cursor == 11904, 'Shared search accounting mismatch')
    active = [s['restart'] for s in searches if s['baseline']['recipeHash'] != s['candidate']['recipeHash']]
    require(frozen['activeRestarts'] == active and [e['restart'] for e in study['evidence']] == active, 'Changed active confirmation family')
    pairs = []
    for search in searches:
        restart = search['restart']
        pair = dict(restart=restart, baselineParty=search['baseline']['finalist']['party']['id'], candidateParty=search['candidate']['finalist']['party']['id'],
                    identical=restart not in active, baselineWins=None, candidateWins=None, gains=0, losses=0, difference=0.)
        if restart in active:
            panel = values[984+(restart-1)*1000:984+restart*1000]
            a, ar = consume('confirmation', panel, search['baseline']['scenario'], expected_party=pair['baselineParty'])
            b, br = consume('confirmation', panel, search['candidate']['scenario'], expected_party=pair['candidateParty'])
            evidence = next(e for e in study['evidence'] if e['restart'] == restart)
            require(evidence == dict(restart=restart, baseline=ar, candidate=br), 'Saved evidence differs from direct outcomes')
            gains, losses = sum(y and not x for x, y in zip(a, b)), sum(x and not y for x, y in zip(a, b))
            pair.update(baselineWins=sum(a), candidateWins=sum(b), gains=gains, losses=losses, difference=(gains-losses)/1000)
        pairs.append(pair)
    require(cursor == len(trials) == 11904+2000*len(active), 'Extra controls, repeated search, unselected or missing fights')
    raw_attempts = (root/'attempts.jsonl').read_bytes()
    require(raw_attempts.endswith(b'\n'), 'Torn attempt ledger')
    lines = raw_attempts.splitlines(keepends=True)
    require(len(lines) == 2*len(trials) and hashlib.sha256(b''.join(lines[:23808])).hexdigest() == frozen['attemptsHash'], 'Invalid global barrier')
    for i, line in enumerate(lines):
        require(json.loads(line) == dict(kind='Started' if i % 2 == 0 else 'Completed', ordinal=i//2+1), 'Reordered attempt ledger')
    return endpoint(pairs, len(template['excludedCombatSeeds']))


def audit(root, pin):
    files = authenticate(root, pin)
    require(not (root/'failure.json').exists(), 'Failed comparison')
    q, template = read(root/'request.json'), read(root/'template.json')
    launch, completion, receipt = read(root/'launch.json'), read(root/'completion.json'), read(root/'native-receipt.json')
    require(q['version'] == VERSION and q.get('maximumSeconds', 4500) == 4500 and q.get('maximumBytes', 4294967296) == 4294967296, 'Changed envelope')
    require(sha(root/'template.json') == q['templateHash'] and sha(root/'capture/files.json') == CAPTURE
            and sha(root/'capture/template.json') == CAPTURE_TEMPLATE, 'Changed template or capture')
    capture = read(root/'capture/template.json')
    expected = copy.deepcopy(capture)
    expected.update(id='incumbent-tie-template', executionHash=template['executionHash'], excludedCombatSeeds=template['excludedCombatSeeds'], maximumBattles=3496)
    expected.pop('primaryReferenceId', None)
    expected['generation'].update(policyVersion='retained-composition-incumbents-v1', seeds=[])
    require(template == expected and [s['party']['id'] for s in template['starts']] == [PRIMARY, SECOND], 'Changed captured teams or scope')
    require(launch['requestFileHash'] == receipt['requestFileHash'] == completion['requestFileHash'] == sha(root/'request.json')
            and launch['version'] == receipt['version'] == completion['version'] == VERSION and receipt['status'] == 'Verified'
            and receipt['newAuditFights'] == 0 and 0 <= receipt['measuredSeconds'] < 4440 and receipt['observedBytes'] < 4160749568
            and completion['status'] == 'Complete' and completion['retries'] == 0 and 0 <= completion['seconds'] < 4500
            and completion['observedBytes'] <= 4294967296, 'Invalid native or enclosing receipt')
    process = completion['process']
    require(process['mechanism'] == 'suspended-owned-job-v1' and process['activeProcesses'] == 0
            and process['totalProcesses'] >= 1 and process['exitCode'] == 0 and not process['timedOut'], 'Incomplete owned process tree')
    scope = read(root/'study/scope.json')
    captured_files = read(root/'capture/files.json')
    for name, digest in captured_files.items():
        if name.startswith('runtime/') and not Path(name).name.startswith('BalanceHarness'):
            require(sha(root/'study/executable'/name[8:]) == digest, 'Changed captured gameplay/runtime dependency')
    require(scope['algorithm'] == VERSION and scope['contentHashes'] == template['contentHashes'], 'Changed archive scope')
    for name, digest in template['contentHashes'].items():
        require(sha(root/'study/content/Data'/name) == digest, 'Changed captured content')
    values, reserved, collisions, duplicates = classify((root/'entropy.bin').read_bytes(), template['excludedCombatSeeds'])
    allocation = read(root/'allocation.json')
    require(allocation['version'] == VERSION and allocation['selected'] == values and allocation['reserved'] == reserved
            and allocation['historicalCollisions'] == collisions and allocation['duplicates'] == duplicates
            and allocation['entropyHash'] == sha(root/'entropy.bin'), 'Changed allocation')
    require(read(root/'history-input.json') == dict(reservationState='Complete', reserved=reserved)
            and read(root/'seed-ledger.json') == dict(reservationState='Complete', historical=template['excludedCombatSeeds'], reserved=reserved), 'Lost reservations')
    authenticate(root/'study', files['study/files.json'])
    result = audit_rows(root, template, values, PRIMARY)
    require(same_numbers(read(root/'result.json'), result) and receipt['fights'] == result['fights'], 'Independent arithmetic disagreement')
    return dict(status='IndependentSavedRowsVerified', manifestSha256=pin, result=result, newFights=0, newValues=0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    parser.add_argument('--manifest-sha256', required=True)
    args = parser.parse_args()
    print(json.dumps(audit(args.archive, args.manifest_sha256), indent=2))
