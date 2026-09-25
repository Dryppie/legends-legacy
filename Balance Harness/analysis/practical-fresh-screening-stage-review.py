"""Read-only diagnosis of the closed screening comparison; no combat or policy fitting.

All 24 searches are reconstructed. Confirmation stays the closed experiment's
reported endpoint; missing later-stage measurements remain unknown.
"""
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
from pathlib import Path
import time

ROOT = Path(__file__).resolve().parents[2]
RUN = ROOT/'TestResults/balance/tower-practical-fresh-screening-comparison-20260922'
ADMISSION = ROOT/'TestResults/practical-fresh-screening-admission-20260922'
EXECUTION = ROOT/'TestResults/practical-fresh-screening-comparison-execution-20260923'
OUT = ROOT/'TestResults/practical-fresh-screening-stage-review-20260923'
PINS = {RUN: '031c7dfdc14f71ff911fdec94472bd161ef72d99b90882ec23ec93dd3fe38042',
        ADMISSION: 'e6eae89b36a30b4bf167c988ff3202c1148880765f559c0d314e67f7d4818caa',
        EXECUTION: '8223bd466f0ca915dabc89143d6789137ab38d05db33dcba9d1a28a5e0dfba9a'}
STAGE = ROOT/'TestResults/practical-search-stage-review-20260922/analysis.py'
STAGE_PIN = '2035037476ab995cd4140b9931fb223a88585b2dd0ed36b69f2e7cef8334e0e1'
COMMON = ADMISSION/'comparison-preparation.py'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
REFERENCES = {'399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b',
              '8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50',
              '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c'}


def module(name, path, pin):
    if hashlib.sha256(path.read_bytes()).hexdigest() != pin:
        raise ValueError('Changed saved helper: '+str(path))
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


stage = module('saved_screening_stage_tools', STAGE, STAGE_PIN)
old = stage.old
read, save, sha, require = old.read, old.save, old.sha, old.require


def protected(rows, count):
    ranked = [r['id'] for r in sorted(rows, key=old.rank_key)]
    require(REFERENCES.issubset(ranked) and len(ranked) == len(set(ranked)), 'Missing protected controls')
    kept = REFERENCES | set([pid for pid in ranked if pid not in REFERENCES][:count])
    require(len(kept) == count+3, 'Missing challenger')
    return [pid for pid in ranked if pid in kept]


def panel(rows, samples, trials, name, seeds):
    measured = stage.panel(rows, samples, trials, name)
    require(len(seeds) == samples and len(set(seeds)) == samples, 'Invalid expected panel')
    for row in rows:
        require([trials[t]['seed'] for t in row['cells'][0]['trials']] == seeds, 'Wrong reserved panel')
    return measured


def arm(search, trials, values, screened):
    discovery = search['discovery']; raw = discovery['arms'][0]
    require(discovery['status'] == 'Complete' and len(discovery['arms']) == 1
            and discovery['version'] == 'retained-composition-three-references-v1'
            and raw['seed'] == values[0] and raw['stopReason'] == 'CandidateBudgetReached'
            and len(raw['evaluations']) == 46 and len(raw['proposals']) <= 256, 'Changed discovery contract')
    samples = 4 if screened else 8
    measured = panel(raw['evaluations'], samples, trials, 'discovery', values[1:1+samples])
    accepted = [p for p in raw['proposals'] if p['result'] == 'evaluated']
    parties = {p['party']['id']:p['party'] for p in accepted}
    require(len(accepted) == len(parties) == 46 and set(parties) == set(measured)
            and {p['party']['id'] for p in accepted if p['provenance']['operator'] == 'supplied'} == REFERENCES,
            'Incomplete evaluated population')
    discovery_order = protected(raw['evaluations'], 2)
    require(discovery['discoveryShortlist'] == [parties[pid] for pid in discovery_order], 'Changed discovery shortlist')
    screen, screen_order, nomination_order = {}, [], discovery_order
    if screened:
        screening = search['screening']; freeze = screening['freeze']
        require(screening['version'] == freeze['version'] == 'tower-practical-fresh-screening-v1'
                and freeze['afterDiscoveryFights'] == 184 and screening['afterSearchFights'] == 368
                and freeze['seeds'] == values[9:17], 'Changed screening boundary')
        screen_order = protected(raw['evaluations'], 20)
        require(freeze['candidates'] == [parties[pid] for pid in screen_order]
                and [r['id'] for r in screening['measurements']] == screen_order, 'Changed screening membership')
        screen = panel(screening['measurements'], 8, trials, 'screening', values[9:17])
        nomination_order = protected(screening['measurements'], 2)
        require(screening['nominees'] == [parties[pid] for pid in nomination_order], 'Changed screening nominees')
    else:
        require('screening' not in search, 'Screening in direct pipeline')
    selection = panel(search['selection'], 32, trials, 'selection', values[17:49])
    require(list(selection) == nomination_order, 'Changed selection order')
    selected, reason, leaders = old.select(search['selection'], nomination_order)
    expected_pipeline = 'tower-practical-fresh-screening-v1' if screened else 'tower-practical-direct-nomination-v1'
    require(search['output']['finalist']['party'] == parties[selected]
            and search['output']['pipeline'] == expected_pipeline
            and search['output']['selector'] == 'tower-staged-incumbent-tie-v1', 'Changed selected output')
    ranked = [r['id'] for r in sorted(measured.values(), key=old.rank_key)]
    challengers = [pid for pid in ranked if pid not in REFERENCES]
    screen_rank = [r['id'] for r in sorted(screen.values(), key=old.rank_key)]
    screen_challengers = [pid for pid in screen_rank if pid not in REFERENCES]
    records, ancestry = [], {}
    for index, proposal in enumerate(raw['proposals']):
        trace = proposal['provenance']
        # Supplied controls retain external source ancestry; generated parents must
        # resolve to earlier evaluated proposals in this search.
        require(trace['id'] not in ancestry and (trace['operator'] == 'supplied' or
                all(parent in ancestry and ancestry[parent]['result'] == 'evaluated' for parent in trace['parentIds'])),
                'Unresolved evaluated ancestry')
        ancestry[trace['id']] = proposal
        if proposal['result'] != 'evaluated':
            continue
        pid = proposal['party']['id']
        records.append(dict(id=pid, operator=trace['operator'], proposal=index+1, parents=trace['parentIds'],
            reference=pid in REFERENCES, discoveryWins=old.wins(measured[pid]), discoverySamples=samples,
            discoveryRank=ranked.index(pid)+1, challengerRank=challengers.index(pid)+1 if pid in challengers else None,
            discoveryNominated=pid in discovery_order, screened=pid in screen,
            screeningWins=old.wins(screen[pid]) if pid in screen else None,
            screeningRank=screen_rank.index(pid)+1 if pid in screen else None,
            screeningChallengerRank=screen_challengers.index(pid)+1 if pid in screen_challengers else None,
            nominated=pid in selection, selected=pid == selected, selectionWins=old.wins(selection[pid]) if pid in selection else None))
    scores = sorted([old.wins(r) for r in selection.values()], reverse=True)
    selection_counts = {pid:old.wins(row) for pid,row in selection.items()}
    best_reference = max(selection_counts[pid] for pid in REFERENCES)
    cutoff = old.wins(measured[challengers[1]])
    screen_cutoff = old.wins(screen[screen_challengers[1]]) if screened else None
    return dict(records=records, discoveryShortlist=discovery_order, screenMembers=screen_order, shortlist=nomination_order,
        selected=selected, selectedReference=selected in REFERENCES, selectionWins=selection_counts, selectionReason=reason,
        leaders=leaders, winnerMargin=scores[0]-scores[1], bestReferenceWins=best_reference,
        winnerReferenceMargin=selection_counts[selected]-best_reference,
        unprotectedReferenceTie=selected not in REFERENCES and bool(set(leaders)&REFERENCES),
        discoveryCutoffWins=cutoff, discoveryCutoffTieSize=sum(old.wins(measured[pid]) == cutoff for pid in challengers),
        screeningCutoffWins=screen_cutoff, screeningCutoffTieSize=sum(old.wins(screen[pid]) == screen_cutoff for pid in screen_challengers),
        retainedDiscoveryChallengers=len((set(discovery_order)&set(nomination_order))-REFERENCES),
        sensitivity=stage.sensitivity(search['selection'], nomination_order, old.PRIMARY, selected))


def summarize(arms):
    nominees = [r for a in arms for r in a['records'] if r['nominated'] and not r['reference']]
    return dict(searches=len(arms), challengerOutputs=sum(not a['selectedReference'] for a in arms),
        selectedReferenceOutputs=sum(a['selectedReference'] for a in arms),
        unprotectedReferenceTies=sum(a['unprotectedReferenceTie'] for a in arms),
        selectionReasons=dict(Counter(a['selectionReason'] for a in arms)),
        winnerMargins=dict(Counter(a['winnerMargin'] for a in arms)),
        retainedDiscoveryChallengers=dict(Counter(a['retainedDiscoveryChallengers'] for a in arms)),
        nomineeDiscoveryRanks=sorted(r['challengerRank'] for r in nominees),
        challengerNomineesAboveReferences=sum(r['selectionWins'] > a['bestReferenceWins'] for a in arms for r in a['records'] if r['nominated'] and not r['reference']),
        halfPanelDisagreements=sum(a['sensitivity']['halvesDisagree'] is True for a in arms),
        anyLeaveOneOutChange=sum(a['sensitivity']['leaveOneOutChanges'] > 0 for a in arms),
        leaveOneOutChanges=sum(a['sensitivity']['leaveOneOutChanges'] for a in arms),
        unassessableSubsets=sum(a['sensitivity']['unassessableSubsets'] for a in arms),
        operators={op:dict(evaluated=len(rows), nominated=sum(r['nominated'] for r in rows), selected=sum(r['selected'] for r in rows))
                   for op in sorted({r['operator'] for a in arms for r in a['records']})
                   for rows in [[r for a in arms for r in a['records'] if r['operator'] == op]]})


def compare(a, b, searches):
    maps = [{r['id']:r for r in s['discovery']['arms'][0]['evaluations']} for s in searches]
    shared = maps[0].keys() & maps[1].keys()
    require(all(maps[0][pid]['cells'][0]['clears'][:4] == maps[1][pid]['cells'][0]['clears'] for pid in shared),
            'Shared discovery prefix differs')
    selections = [{r['id']:r for r in s['selection']} for s in searches]
    require(all(old.observation(selections[0][pid]) == old.observation(selections[1][pid])
                for pid in selections[0].keys() & selections[1].keys()), 'Shared selection observations differ')
    eval_order = [list(m) for m in maps]
    def path(arm, pid):
        record = next((r for r in arm['records'] if r['id'] == pid), None)
        return dict(id=pid, present=record is not None, record=record)
    return dict(sharedEvaluated=len(shared), firstDifferentEvaluatedRecipe=next((i+1 for i,(x,y) in enumerate(zip(*eval_order)) if x != y), None),
        sameNominees=set(a['shortlist']) == set(b['shortlist']), sameNomineeOrder=a['shortlist'] == b['shortlist'],
        sameOutput=a['selected'] == b['selected'], baselineOutputInCandidate=path(b,a['selected']),
        candidateOutputInBaseline=path(a,b['selected']))


def run(output):
    started = time.monotonic()
    require(output.resolve() == OUT.resolve() and output.is_dir() and not (output/'review.json').exists(), 'Use new external review evidence')
    common = module('screening_stage_history', COMMON, COMMON_PIN)
    inventories = {}
    for path,pin in PINS.items():
        inventories[path] = dict(common.authenticate(path,pin), **{'files.json':pin})
        print('Authenticated '+path.name, flush=True)
    closeout = read(EXECUTION/'closeout.json')
    result = read(RUN/'result.json')
    require(closeout['scope'] == 'Closed' and closeout['nativeAudit'] == closeout['independentAudit'] == 'Passed'
            and closeout['result'] == result == read(RUN/'independent-audit.json')['result']
            and result['decision'] == 'DoNotPromoteFreshScreening', 'Unverified closed source')
    frozen, study = read(RUN/'study/outputs-freeze.json'), read(RUN/'study/study.json')
    require(study['freeze'] == frozen and frozen['completedAttempts'] == 12672
            and len(frozen['searches']) == len(frozen['families']) == len(result['pairs']) == 12, 'Changed global freeze')
    raw_trials = (RUN/'study/trials.jsonl').read_bytes()
    require(raw_trials.endswith(b'\n'), 'Torn trial journal')
    trial_rows = [json.loads(line) for line in raw_trials.splitlines()]
    require([t['id'] for t in trial_rows] == [f'trial-{i+1:06d}' for i in range(55672)], 'Incomplete/reordered journal')
    trials = {t['id']:t for t in trial_rows}
    values = read(RUN/'allocation.json')['selected']
    require(len(values) == len(set(values)) == 12588, 'Changed assigned panels')
    pairs = []
    for index in range(1,13):
        searches = [read(RUN/f'study/pair-{index:02d}-{side}.json') for side in ['baseline','candidate']]
        for side, search in zip(['baseline','candidate'], searches):
            require(search == frozen['searches'][index-1][side]
                    and search['discovery'] == read(RUN/f'study/pair-{index:02d}-{side}-discovery.json'), 'Changed saved checkpoint')
        require(searches[1]['screening'] == read(RUN/f'study/pair-{index:02d}-candidate-screening.json')
                and searches[1]['screening']['freeze'] == read(RUN/f'study/pair-{index:02d}-candidate-screening-freeze.json'), 'Changed screening checkpoint')
        block = values[(index-1)*49:index*49]
        require(len(set(block)) == 49, 'Overlapping stage panels')
        a,b = [arm(s,trials,block,side == 'candidate') for s,side in zip(searches,['baseline','candidate'])]
        endpoint = result['pairs'][index-1]
        require(endpoint['restart'] == index and endpoint['baselineParty'] == a['selected'] and endpoint['candidateParty'] == b['selected'], 'Changed endpoint output')
        family = frozen['families'][index-1]
        evidence = {e['recipeHash']:e['trials'] for e in study['evidence'] if e['restart'] == index}
        require(len(evidence) == endpoint['recipeCount'] == len(family['members']), 'Changed confirmation membership')
        physical = {}
        for m in family['members']:
            rows = evidence[m['recipeHash']]
            require([t['seed'] for t in rows] == values[588+(index-1)*1000:588+index*1000]
                    and all(t['outcome'] in ('Victory','Defeat','Draw') for t in rows), 'Invalid saved confirmation panel')
            physical[m['partyId']] = [t['outcome'] == 'Victory' for t in rows]
        av,bv = physical[a['selected']],physical[b['selected']]
        require(endpoint['baselineWins'] == sum(av) and endpoint['candidateWins'] == sum(bv)
                and endpoint['gains'] == sum(y and not x for x,y in zip(av,bv))
                and endpoint['losses'] == sum(x and not y for x,y in zip(av,bv)), 'Changed closed confirmation counts')
        pairs.append(dict(pair=index, baseline=a, candidate=b, cross=compare(a,b,searches), closedConfirmation=endpoint))
    history, exclusions = common.history(RUN.parent, read(RUN/'request.json'))
    require(history == read(EXECUTION/'live-history-files.json') and len(exclusions) == 600552 and len(history) == 238, 'Permanent history changed')
    for path,inventory in inventories.items():
        require(common.inventory(path) == inventory, 'Source changed during diagnosis')
    review = dict(status='VerifiedFreshScreeningStageReview', pairs=pairs,
        summary={side:summarize([p[side] for p in pairs]) for side in ['baseline','candidate']},
        searches=24, closedDecision=result['decision'], changedPolicy=False, newFights=0, newValues=0,
        nativePreparations=0, newParties=0, historyValues=len(exclusions), historyFiles=len(history),
        seconds=time.monotonic()-started,
        scope='Descriptive saved-stage diagnosis; no candidate reranking on confirmation and no hypothetical policy strength estimate.',
        sensitivity='Fixed first/last 16 selection trials and all 32 leave-one-out panels; overlapping diagnostics; zero-win subsets unassessed.',
        missingData='Unmeasured later-stage scores remain null; no claim about recipes absent from either realized trajectory.',
        accounting='Engineering separately disclosed; older incomplete totals and 18,180-second /13,584-MiB ledger preserved.')
    save(output/'review.json',review)
    save(output/'history-files.json',history)
    save(output/'input-manifests.json',{str(p):pin for p,pin in PINS.items()})
    save(output/'helper-pins.json',{str(STAGE):STAGE_PIN,str(COMMON):COMMON_PIN,str(stage.OLD):stage.OLD_PIN})
    (output/'analysis.py').write_bytes(Path(__file__).read_bytes())
    print(json.dumps({k:v for k,v in review.items() if k != 'pairs'},indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,default=OUT)
    run(parser.parse_args().output)
