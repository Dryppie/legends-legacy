"""Independent zero-fight audit of one complete v5 native search archive.

Recounts all six panels, pruning, nomination, the frozen pair and exact gate.
Native reconstruction remains required for RNG, typed content, canonical hashes
containing .NET numbers and prepared-input identity. Neither audit is admission,
team confirmation or scientific evidence that this selection rule improves play.
"""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import re

_spec = importlib.util.spec_from_file_location('proposal_audit', Path(__file__).with_name('audit-proposal-affinity-study.py'))
base = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(base)
require, read, digest, score, rank, close = base.require, base.read, base.digest, base.score, base.rank, base.close
VERSION = 'tower-proposal-racing-v5'
POLICY = 'tower-racing-benchmark-validation-v1'
ROLES = base.ROLES[:4] + ['nomination', 'validation']
SAMPLES = [8, 8, 8, 8, 16, 60]
MEMBERS = {'plan.json', 'search.json', 'charges.jsonl', 'inputs.jsonl', 'batch-01.json', 'batch-02.json',
           'validation-freeze.json', 'validation-decision.json'} | {f'panel-{i:02d}.json' for i in range(1, 7)}


def gate(gains, losses):
    require(type(gains) is int and type(losses) is int and min(gains, losses) >= 0 and gains + losses <= 60,
            'Invalid discordant counts')
    numerator = sum(math.comb(gains + losses, k) for k in range(gains, gains + losses + 1))
    denominator = 1 << (gains + losses)
    return numerator, denominator, gains > losses and 20 * numerator <= denominator


def audit_racing(root, trials, consume, fixture=False):
    """consume authenticates each row against its literal native battle and recipe.

    Only cross-language tests use fixture=True, with a separate literal-fixture.json;
    the public CLI always authenticates a pinned complete native archive.
    """
    require({p.name for p in root.iterdir()} == MEMBERS | ({'literal-fixture.json'} if fixture else set())
            and all(p.is_file() and not p.is_symlink() and not p.is_junction() for p in root.iterdir()),
            'Changed racing evidence membership')
    plan, report = read(root/'plan.json'), read(root/'search.json')
    racing, evaluation = plan['racing'], report['evaluation']
    scope = racing['scope']
    require(plan['version'] == report['version'] == evaluation['version'] == VERSION
            and plan['selectionPolicyVersion'] == report['selectionPolicyVersion'] == POLICY
            and evaluation['status'] == 'Complete' and evaluation['error'] is None
            and evaluation['chargedEvaluations'] == evaluation['plannedEvaluations'] == racing['maximumEvaluations'] == len(trials) == 528
            and len(report['batches']) == len(evaluation['decisions']) == 2 and len(evaluation['panels']) == 6
            and report['planHash'] == evaluation['planHash'] and re.fullmatch('[0-9a-f]{64}', report['planHash'])
            and report['policyHash'] == digest(plan['policy'])
            and plan['policy']['version'] in ('tower-proposal-policy-v1', 'tower-proposal-policy-v2', 'tower-proposal-policy-v3'),
            'Changed v5 contract')
    schedule = racing['panels']
    require(len(schedule) == 6 and [p['role'] for p in schedule] == ROLES
            and [len(p['seeds']) for p in schedule] == SAMPLES, 'Changed six-panel schedule')
    seeds = [s for p in schedule for s in p['seeds']]
    reserved = set(scope['excludedCombatSeeds']) | set(scope['generation']['seeds'])
    for panel in scope['stages']['schedules'].values():
        reserved.update(s for name in ('discovery', 'selection', 'confirmation', 'diagnostics', 'feedback') for s in (panel.get(name) or []))
    for reference in scope['references']:
        reserved.update(reference['scenario']['seeds'])
    require(all(type(s) is int and -(2**31) <= s < 2**31 for s in seeds + [racing['rootSeed']])
            and len(set(seeds + [racing['rootSeed']])) == 109 and not set(seeds) & reserved,
            'Reused or invalid panel values')
    parties = {s['party']['id']: s['party'] for s in scope['starts']}
    references, seen = list(parties), set(parties)
    require(len(references) == len(scope['starts']) == 3 and len(scope['contexts']) == 1, 'Changed reference scope')
    benchmark = next(s['party']['id'] for s in scope['starts'] if s['referenceId'] == racing['benchmarkReferenceId'])
    primary = next(s['party']['id'] for s in scope['starts'] if s['referenceId'] == scope['stages']['selectionPrimaryReferenceId'])
    allowed = {e['id']: e['family'] for e in scope['allowedEssences']}
    beam, previous, before, charges = [], [], 0, []
    for i, panel in enumerate(evaluation['panels']):
        freeze, rows, values = panel['freeze'], panel['observations'], schedule[i]['seeds']
        if i in (0, 2):
            batch = report['batches'][i//2]
            require(batch == read(root/f'batch-{i//2+1:02d}.json') and batch['wave'] == i//2+1
                    and batch['afterEvaluations'] == before and batch['beamIds'] == beam and set(batch['seenBefore']) == seen
                    and len(batch['seenBefore']) == len(seen)
                    and batch['feedbackPanels'] == [p['observations'][0]['request']['panelHash'] for p in evaluation['panels'][:i]]
                    and len(batch['candidates']) == (9 if i == 0 else 8) and len(batch['proposals']) <= 128,
                    'Changed adaptive batch')
            accepted = []
            for j, proposal in enumerate(batch['proposals']):
                require(proposal['attempt'] == j+1 and 0 <= proposal['constructionChecks'] <= 32
                        and all(pid in set(references) | set(beam) for pid in proposal['parents']), 'Changed proposal bounds/parentage')
                if proposal['rejection'] is None:
                    accepted.append(proposal['party'])
            require(accepted == batch['candidates'], 'Changed accepted candidates')
            for party in batch['candidates']:
                require(party['id'] == digest(party['builds']) and party['id'] not in seen
                        and set(party['builds']) == {str(n+1) for n in range(scope['requiredPartySize'])}, 'Duplicate or illegal candidate')
                for ids in party['builds'].values():
                    require(ids == sorted(set(ids)) and len(ids) == scope['budget']['essenceSlots']
                            and all(e in allowed for e in ids) and len({allowed[e] for e in ids}) == len(ids), 'Illegal essence placement')
                if scope['ownedCopies'] is not None:
                    flat = [e for ids in party['builds'].values() for e in ids]
                    require(all(flat.count(e) <= scope['ownedCopies'].get(e, 0) for e in set(flat)), 'Owned-copy bound exceeded')
                seen.add(party['id']); parties[party['id']] = party
            ids = references + beam + [p['id'] for p in batch['candidates']]
        elif i in (1, 3):
            ids = references + evaluation['decisions'][i//2]['survivorIds']
        elif i == 4:
            eligible = set(references) | set(beam[:2])
            ids = [r['id'] for r in rank(common) if r['id'] in eligible]
            require(evaluation['nominees'] == ids and len(ids) == 5, 'Changed nominees')
        else:
            nomination = evaluation['panels'][4]
            challengers = [r for r in nomination_scores if r['id'] != benchmark]
            chosen = base.select(challengers, [pid for pid in evaluation['nominees'] if pid != benchmark], primary)
            frozen = dict(version=POLICY, planHash=report['planHash'],
                          nominationPanelHash=nomination['observations'][0]['request']['panelHash'], challengerId=chosen, benchmarkId=benchmark)
            require(evaluation['validationFreeze'] == read(root/'validation-freeze.json') == frozen, 'Changed challenger freeze')
            ids = [chosen, benchmark]
        require(panel['complete'] is True and freeze == read(root/f'panel-{i+1:02d}.json')
                and freeze['parties'] == [parties[pid] for pid in ids] and freeze['index'] == i and freeze['role'] == ROLES[i]
                and freeze['version'] == VERSION and freeze['planHash'] == report['planHash'] and freeze['seeds'] == values
                and freeze['context'] == scope['contexts'][0]['id'] and freeze['evaluationsBefore'] == before
                and freeze['plannedEvaluations'] == len(rows) == len(ids)*len(values), 'Changed panel freeze')
        panel_hash = rows[0]['request']['panelHash']
        for index, row in enumerate(rows):
            request, outcome = row['request'], row['outcome']
            pid, seed = ids[index//len(values)], values[index % len(values)]
            ordinal = before+index+1
            require(request['ordinal'] == ordinal and request['partyId'] == pid and request['seed'] == outcome['seed'] == seed
                    and request['scopeHash'] == freeze['scopeHash'] and request['panelHash'] == panel_hash
                    and request['role'] == ROLES[i] and request['scenario']['seeds'] == values
                    and outcome['trialId'] == trials[ordinal-1]['id'] == f'trial-{ordinal:06d}'
                    and trials[ordinal-1]['stage'] == ROLES[i] and trials[ordinal-1]['seed'] == seed,
                    'Changed observation membership/order')
            require(outcome['outcome'] in ('Victory', 'Defeat', 'Draw')
                    and all(type(outcome[k]) in (int, float) and math.isfinite(outcome[k]) and 0 <= outcome[k] <= 100
                            for k in ('guardianHealth', 'survival'))
                    and type(outcome['durationSeconds']) in (int, float) and math.isfinite(outcome['durationSeconds'])
                    and outcome['durationSeconds'] >= 0, 'Invalid observation')
            consume(row, parties[pid])
            charges.append(dict(ordinal=ordinal, panelHash=panel_hash, partyId=pid, seed=seed))
        computed = score(ids, rows)
        require(close(panel['scores'], computed), 'Changed panel scores')
        require(all(type(saved[k]) is int and saved[k] == actual[k] for saved, actual in zip(panel['scores'], computed)
                    for k in ('samples', 'wins', 'draws')), 'Changed exact score counts')
        if i == 4:
            nomination_scores = computed
        contrasts = []
        wins = {pid: {o['request']['seed']: o['outcome']['outcome'] == 'Victory' for o in rows if o['request']['partyId'] == pid} for pid in ids}
        for pid in ids:
            for ref in references:
                if pid != ref and ref in ids:
                    contrasts.append(dict(partyId=pid, referenceId=ref, samples=len(values),
                        gainedWins=sum(wins[pid][s] and not wins[ref][s] for s in values),
                        lostWins=sum(not wins[pid][s] and wins[ref][s] for s in values)))
        require(panel['contrasts'] == contrasts, 'Changed reference contrasts')
        if i in (0, 2):
            d = evaluation['decisions'][i//2]; ranked = rank([r for r in computed if r['id'] not in references])
            elite = [r['id'] for r in ranked[:3]]; cutoff = ranked[2]['wins']-1
            def distance(pid):
                return min(sum(len(set(parties[pid]['builds'][o])-set(parties[e]['builds'][o])) for o in parties[pid]['builds']) for e in elite)
            eligible = [r for r in ranked[3:] if r['wins'] >= cutoff]
            diverse = sorted(eligible, key=lambda r: -distance(r['id']))[0] if eligible else ranked[3]
            survivors = elite + [diverse['id']]
            require(d['wave'] == i//2+1 and d['eliteIds'] == elite and d['survivorIds'] == survivors and d['diversityId'] == diverse['id']
                    and d['competitiveCutoffWins'] == cutoff and d['diversityFallback'] == (not eligible)
                    and d['diversityDistance'] == distance(diverse['id'])
                    and d['prunedIds'] == [r['id'] for r in ranked if r['id'] not in survivors], 'Changed pruning/diversity')
        elif i in (1, 3):
            common = score(ids, previous+rows); beam = [r['id'] for r in rank(common) if r['id'] not in references]
            require(close(evaluation['decisions'][i//2]['commonScores'], common)
                    and evaluation['decisions'][i//2]['beamIds'] == beam, 'Changed feedback beam')
        previous = rows; before += len(rows)
    gains = sum(wins[chosen][s] and not wins[benchmark][s] for s in values)
    losses = sum(not wins[chosen][s] and wins[benchmark][s] for s in values)
    numerator, denominator, passed = gate(gains, losses)
    selected = chosen if passed else benchmark
    expected = dict(version=POLICY, freezeHash=digest(frozen), panelHash=panel_hash, samples=60,
                    gainedWins=gains, lostWins=losses, tailNumerator=numerator, tailDenominator=denominator, passed=passed, selectedId=selected)
    decision = evaluation['validationDecision']
    require(all(type(decision[k]) is int for k in ('samples', 'gainedWins', 'lostWins', 'tailNumerator', 'tailDenominator'))
            and type(decision['passed']) is bool and decision == read(root/'validation-decision.json') == expected,
            'Changed exact validation decision')
    require(evaluation['rawSelectedId'] == selected and before == 528, 'Changed selected output')
    for name, expected_rows in [('charges.jsonl', charges), ('inputs.jsonl', trials)]:
        raw = (root/name).read_bytes()
        require(raw.endswith(b'\n') and [json.loads(line) for line in raw.splitlines()] == expected_rows, 'Changed durable journal')
    return dict(version=VERSION, status='IndependentlyRecounted', chargedEvaluations=before,
                validationDecision=decision, nativeReconstructionRequired=True, admissionRequired=True, newFights=0)


def audit(root, manifest_sha256):
    require(re.fullmatch('[0-9a-f]{64}', manifest_sha256) and base.sha(root/'files.json') == manifest_sha256, 'Changed external manifest pin')
    plan = read(root/'racing/plan.json')
    archive = base.Archive(root, dict(scope=plan['racing']['scope']))
    require(archive.scope['algorithm'] == VERSION+'/'+read(root/'racing/search.json')['planHash'], 'Changed archive algorithm')
    result = audit_racing(root/'racing', archive.trials, archive.consume)
    archive.finish()
    base.authenticate(root)
    require(base.sha(root/'files.json') == manifest_sha256, 'Archive changed during audit')
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    parser.add_argument('manifest_sha256')
    args = parser.parse_args()
    print(json.dumps(audit(args.archive, args.manifest_sha256), indent=2))
