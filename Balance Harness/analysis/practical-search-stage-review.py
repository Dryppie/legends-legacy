"""Reconstruct both closed exploration comparisons from saved search stages only.

Selection sensitivity uses the fixed first/last 16 and each leave-one-out panel.
These are overlapping descriptive diagnostics, never fresh evidence or selectors.
"""
import argparse
from collections import Counter
import importlib.util
import json
from pathlib import Path
import time

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'TestResults/practical-search-stage-review-20260922'
OLD = ROOT/'TestResults/reference-exploration-diagnosis-20260922/analysis-final.py'
OLD_PIN = '3bc9eda0aa93f455aaff1b8d27d5256e260bb4c0994026f791ec50e9a08f214a'
COMMON = ROOT/'TestResults/reference-exploration-offset-comparison-admission-20260922/comparison-preparation.py'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
PACKAGES = {
    'original': ('reference-exploration-comparison',
        '482707e32c6e4e398793e45e61627b452a765db237c382a9c75a34c8f6163a7e',
        'e0842f0d96af3542d30d28f366d79485cf929b20eeeda8d406a5a38a8fa40e72',
        '55d47a7cf3b560baebf78435eb9089677fc880ce6f7d9e7f010dec22b4d03361'),
    'offset': ('reference-exploration-offset-comparison',
        '273ecad90d0f4532e889e77feae153dfe6f559a1ce69010a27af4d1778821431',
        '9c47e5ebb11c4a3731b31f47a24736e4cb3d6aa621fa3c7c17f9f8f93f69e3bc',
        '160db185b5c8ed49e045338d0590d2a8a51e57ab93432955d89e5db4b9fd5ff6')}


def module(name, path, pin):
    import hashlib
    assert hashlib.sha256(path.read_bytes()).hexdigest() == pin, 'Changed saved helper'
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


old = module('saved_stage_diagnosis', OLD, OLD_PIN)
read, save, sha, require = old.read, old.save, old.sha, old.require


def panel(rows, samples, trials=None, stage=None):
    require(len({r['id'] for r in rows}) == len(rows), 'Duplicate measurement')
    expected = None
    for row in rows:
        old.wins(row, samples)
        old.rank_key(row)
        ids = row['cells'][0]['trials']
        require(len(set(ids)) == samples, 'Duplicate or partial trial panel')
        if trials is not None:
            require(all(ident in trials and trials[ident]['stage'] == stage for ident in ids), 'Missing or wrong-stage trial')
            seeds = [trials[ident]['seed'] for ident in ids]
            require(len(set(seeds)) == samples, 'Repeated seed within panel')
            require(expected is None or seeds == expected, 'Misaligned seed panel')
            expected = seeds
    return {r['id']: r for r in rows}


def positive_winner(scores, order, primary):
    require(set(scores) == set(order) and len(order) == len(set(order)) and primary in scores, 'Invalid subset membership')
    maximum = max(scores.values())
    if maximum == 0:
        return None  # Per-trial guardian health is unavailable in a sliced panel.
    leaders = [pid for pid in order if scores[pid] == maximum]
    return primary if primary in leaders else leaders[0]


def sensitivity(rows, order, primary, selected):
    measured = panel(rows, 32)
    clears = {pid: measured[pid]['cells'][0]['clears'] for pid in order}
    def winner(indices):
        return positive_winner({pid: sum(values[i] for i in indices) for pid, values in clears.items()}, order, primary)
    halves = [winner(range(16)), winner(range(16, 32))]
    loo = [winner([i for i in range(32) if i != omit]) for omit in range(32)]
    return dict(firstHalf=halves[0], secondHalf=halves[1], halvesDisagree=None if None in halves else halves[0] != halves[1],
                halfDepartures=sum(pid != selected for pid in halves if pid is not None),
                leaveOneOutChanges=sum(pid != selected for pid in loo if pid is not None),
                leaveOneOutWinners=loo, unassessableSubsets=sum(pid is None for pid in halves+loo))


def arm(search, trials):
    discovery = search['discovery']
    require(discovery['status'] == 'Complete' and len(discovery['arms']) == 1, 'Incomplete search')
    saved = discovery['arms'][0]
    require(saved['stopReason'] == 'CandidateBudgetReached' and len(saved['evaluations']) == 46
            and len(saved['proposals']) <= 256, 'Changed search budget')
    measured = panel(saved['evaluations'], 8, trials, 'discovery')
    accepted = [p for p in saved['proposals'] if p['result'] == 'evaluated']
    require(len(accepted) == 46 and {p['party']['id'] for p in accepted} == set(measured), 'Missing evaluated proposal')
    references = {p['party']['id'] for p in accepted if p['provenance']['operator'] == 'supplied'}
    require(len(references) == 3 and old.PRIMARY in references, 'Missing protected references')
    ranked = [r['id'] for r in sorted(measured.values(), key=old.rank_key)]
    challengers = [pid for pid in ranked if pid not in references]
    order = [p['id'] for p in discovery['discoveryShortlist']]
    require(order == [pid for pid in ranked if pid in references | set(challengers[:2])], 'Changed nomination membership/order')
    selected, reason, leaders = old.select(search['selection'], order)
    require(selected == search['output']['finalist']['party']['id'], 'Changed selected output')
    selection = panel(search['selection'], 32, trials, 'selection')
    require(not {trials[x]['seed'] for x in selection[order[0]]['cells'][0]['trials']}
            & {trials[x]['seed'] for x in measured[ranked[0]]['cells'][0]['trials']}, 'Reused stage panel')
    ancestry, proposals, records = {}, {}, []
    cutoff = old.wins(measured[challengers[1]])
    for index, p in enumerate(saved['proposals']):
        trace = p['provenance']; ident = trace['id']; op = trace['operator']; parents = trace['parentIds']
        require(ident not in proposals, 'Duplicate proposal identity')
        if op != 'supplied':
            require(all(x in proposals and proposals[x]['result'] == 'evaluated' for x in parents), 'Unresolved evaluated parent')
        roots = set().union(*(ancestry.get(x, set()) for x in parents))
        if op == 'reference-exploration':
            roots.add(ident)
        ancestry[ident], proposals[ident] = roots, p
        if p['result'] != 'evaluated':
            continue
        pid = p['party']['id']; count = old.wins(measured[pid]); nominated = pid in selection
        category = 'reference' if pid in references else 'direct' if op == 'reference-exploration' else 'descendant' if roots else 'other'
        exclusion = None if nominated else 'BelowWinCutoff' if count < cutoff else 'TiedWinCutoff'
        require(nominated or count <= cutoff, 'Impossible nomination exclusion')
        records.append(dict(id=pid, proposal=index, operator=op, category=category, discoveryWins=count,
            discoveryRank=ranked.index(pid)+1, challengerRank=challengers.index(pid)+1 if pid in challengers else None,
            nominated=nominated, selected=pid == selected, selectionWins=old.wins(selection[pid]) if nominated else None,
            exclusion=exclusion, explorationAncestors=sorted(roots)))
    best_reference = max(old.wins(selection[pid]) for pid in references)
    winner_values = selection[selected]['cells'][0]['clears']
    for record in records:
        if record['nominated']:
            values = selection[record['id']]['cells'][0]['clears']
            record.update(gapToWinner=old.wins(selection[selected])-sum(values),
                          gainsAgainstWinner=sum(a and not b for a,b in zip(values,winner_values)),
                          lossesAgainstWinner=sum(b and not a for a,b in zip(values,winner_values)),
                          versusBestReference=sum(values)-best_reference)
    scores = sorted([old.wins(r) for r in selection.values()], reverse=True)
    return dict(records=records, evaluated=46, proposals=len(saved['proposals']), shortlist=order, selected=selected,
        selectionReason=reason, leaders=leaders, winnerMargin=scores[0]-scores[1], cutoffWins=cutoff,
        challengerCutoffTieSize=sum(old.wins(measured[pid]) == cutoff for pid in challengers),
        challengerFirstSecondSelectionReversed=old.wins(selection[challengers[0]]) < old.wins(selection[challengers[1]]),
        sensitivity=sensitivity(search['selection'], order, old.PRIMARY, selected))


def summarize(arms):
    groups = {}
    for category in ['reference', 'direct', 'descendant', 'other']:
        records = [r for a in arms for r in a['records'] if r['category'] == category]
        nominees = [r for r in records if r['nominated']]
        groups[category] = dict(evaluated=len(records), nominated=len(nominees), selected=sum(r['selected'] for r in records),
            belowCutoff=sum(r['exclusion']=='BelowWinCutoff' for r in records), tiedExcluded=sum(r['exclusion']=='TiedWinCutoff' for r in records),
            discoveryWins=sum(r['discoveryWins'] for r in records), discoveryTrials=8*len(records),
            nomineeDiscoveryWins=sum(r['discoveryWins'] for r in nominees), nomineeDiscoveryTrials=8*len(nominees),
            selectionWins=sum(r['selectionWins'] for r in nominees), selectionTrials=32*len(nominees))
    nominees = [r for a in arms for r in a['records'] if r['nominated'] and r['category']!='reference']
    ops = {}
    for a in arms:
        for r in a['records']:
            row = ops.setdefault(r['operator'], Counter())
            row.update(evaluated=1, discoveryWins=r['discoveryWins'], nominated=int(r['nominated']), selected=int(r['selected']))
    return dict(searches=len(arms), groups=groups, operators=ops,
        cutoffHistogram=dict(Counter(a['cutoffWins'] for a in arms)), cutoffTieSizeHistogram=dict(Counter(a['challengerCutoffTieSize'] for a in arms)),
        selectionReasons=dict(Counter(a['selectionReason'] for a in arms)), marginHistogram=dict(Counter(a['winnerMargin'] for a in arms)),
        challengerFirstSecondReversals=sum(a['challengerFirstSecondSelectionReversed'] for a in arms),
        challengersAboveBestReference=sum(r['versusBestReference']>0 for r in nominees),
        challengersTiedBestReference=sum(r['versusBestReference']==0 for r in nominees),
        challengersBelowBestReference=sum(r['versusBestReference']<0 for r in nominees),
        halfPanelDisagreements=sum(a['sensitivity']['halvesDisagree'] is True for a in arms),
        anyLeaveOneOutChange=sum(a['sensitivity']['leaveOneOutChanges']>0 for a in arms),
        leaveOneOutChanges=sum(a['sensitivity']['leaveOneOutChanges'] for a in arms),
        unassessableSubsets=sum(a['sensitivity']['unassessableSubsets'] for a in arms))


def run(output):
    start = time.monotonic()
    require(output.resolve() == OUT.resolve() and not output.exists(), 'Use the new external review directory')
    common = module('stage_history', COMMON, COMMON_PIN)
    inventories, studies = {}, {}
    for label, (name, run_pin, execution_pin, admission_pin) in PACKAGES.items():
        run_root = ROOT/f'TestResults/balance/tower-{name}-20260922'
        execution = ROOT/f'TestResults/{name}-execution-20260922'
        admission = ROOT/f'TestResults/{name}-admission-20260922'
        for path,pin in [(run_root,run_pin),(execution,execution_pin),(admission,admission_pin)]:
            manifest = common.authenticate(path,pin)
            inventories[path] = dict(manifest, **{'files.json':pin})
        closeout = read(execution/'closeout.json')
        require(closeout['scope']=='Closed' and closeout['nativeAudit']==closeout['independentAudit']=='Passed', 'Unverified source')
        freeze = read(run_root/'study/outputs-freeze.json')
        require(len(freeze['searches'])==12 and freeze['completedAttempts']==12672, 'Invalid output freeze')
        trial_rows = [json.loads(line) for line in (run_root/'study/trials.jsonl').read_text(encoding='utf-8').splitlines()]
        trials = {row['id']:row for row in trial_rows}
        require(len(trials)==len(trial_rows), 'Duplicate journal trial identity')
        pairs = []
        for index in range(1,13):
            searches = [read(run_root/f'study/pair-{index:02d}-{side}.json') for side in ['baseline','candidate']]
            for side, search in zip(['baseline','candidate'], searches):
                require(search == freeze['searches'][index-1][side] and search['discovery'] == read(run_root/f'study/pair-{index:02d}-{side}-discovery.json'), 'Changed search checkpoint')
            a,b = [arm(search,trials) for search in searches]
            for stage in ['discovery','selection']:
                rows = [s['discovery']['arms'][0]['evaluations'] if stage=='discovery' else s['selection'] for s in searches]
                ma,mb = [{r['id']:r for r in values} for values in rows]
                require(all(old.observation(ma[pid]) == old.observation(mb[pid]) for pid in ma.keys() & mb.keys()), 'Shared observations differ')
            shared = len({r['id'] for r in a['records']} & {r['id'] for r in b['records']})
            pairs.append(dict(pair=index, baseline=a, candidate=b, sharedEvaluated=shared,
                sameNominees=a['shortlist']==b['shortlist'], sameOutput=a['selected']==b['selected']))
        studies[label] = dict(pairs=pairs, arms={side:summarize([p[side] for p in pairs]) for side in ['baseline','candidate']},
            sameNomineePairs=sum(p['sameNominees'] for p in pairs), sameOutputPairs=sum(p['sameOutput'] for p in pairs),
            closedDecision=closeout['result']['decision'])
        print(json.dumps(dict(study=label, **{k:v for k,v in studies[label].items() if k!='pairs'})), flush=True)
    latest = ROOT/'TestResults/reference-exploration-offset-comparison-execution-20260922'
    history, values = common.history(ROOT/'TestResults/balance', read(ROOT/'TestResults/balance/tower-reference-exploration-offset-comparison-20260922/request.json'))
    require(history == read(latest/'live-history-files.json') and len(values)==584171, 'Permanent history changed')
    for path,inventory in inventories.items():
        require(common.inventory(path)==inventory, 'Source changed during review')
    output.mkdir()
    result = dict(status='VerifiedSavedSearchStageReview', studies=studies, searches=48,
        scope='Descriptive search-stage evidence, reported separately by experiment; no confirmation reranking or strength claim.',
        sensitivityDefinition='Fixed first/last 16 trials and all 32 leave-one-out common panels; no random draws; zero-win subsets unassessed.',
        seconds=time.monotonic()-start, newFights=0,newValues=0,newParties=0,nativePreparations=0,
        changedPolicy=False,historyPreserved=True,historyValues=len(values),historyFiles=len(history),
        accounting='Engineering separately disclosed; older incomplete totals and 18,180-second /13,584-MiB ledger preserved.')
    save(output/'review.json', result)
    save(output/'history-files.json', history)
    save(output/'input-manifests.json', {str(path):inventory['files.json'] for path,inventory in inventories.items()})
    save(output/'helper-pins.json', {str(OLD):OLD_PIN,str(COMMON):COMMON_PIN})
    (output/'analysis.py').write_bytes(Path(__file__).read_bytes())
    print(json.dumps({k:v for k,v in result.items() if k!='studies'}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=OUT)
    run(parser.parse_args().output)
