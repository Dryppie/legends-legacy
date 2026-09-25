"""Describe the sealed twelve-pair search trajectories; never run a search or battle.

Uses discovery/selection observations only. Confirmation results are retained as
the closed experiment's conclusion, not used to rank alternative policies.
"""
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import time

ROOT = Path(__file__).resolve().parents[2]
RUN = ROOT/'TestResults/balance/tower-reference-exploration-comparison-20260922'
EXECUTION = ROOT/'TestResults/reference-exploration-comparison-execution-20260922'
ADMISSION = ROOT/'TestResults/reference-exploration-comparison-admission-20260922'
OUTPUT = ROOT/'TestResults/reference-exploration-diagnosis-20260922'
RUN_PIN = '482707e32c6e4e398793e45e61627b452a765db237c382a9c75a34c8f6163a7e'
EXECUTION_PIN = 'e0842f0d96af3542d30d28f366d79485cf929b20eeeda8d406a5a38a8fa40e72'
ADMISSION_PIN = '55d47a7cf3b560baebf78435eb9089677fc880ce6f7d9e7f010dec22b4d03361'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
PRIMARY = '399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b'
EXPLORATION = 'reference-exploration'


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def save(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')


def rank_key(row):
    f = row['fitness']
    require(all(math.isfinite(f[k]) for k in ('worstContextWinRate', 'guardianHealth', 'survival', 'victoryDuration')),
            'Nonfinite discovery fitness')
    return (-f['worstContextWinRate'], f['guardianHealth'], -f['survival'], f['victoryDuration'], row['id'])


def wins(row, samples=None):
    require(len(row['cells']) == 1, 'Expected one fixed context')
    cell = row['cells'][0]
    require(all(type(x) is bool for x in cell['clears']) and len(cell['clears']) > 0, 'Invalid saved outcomes')
    if samples is not None:
        require(len(cell['clears']) == samples and len(cell['trials']) == samples, 'Incomplete saved measurement')
    count = sum(cell['clears'])
    require(abs(row['fitness']['worstContextWinRate']-count/len(cell['clears'])) < 1e-12, 'Wrong saved win fitness')
    return count


def select(rows, shortlist, primary=PRIMARY):
    require(len(rows) == len(shortlist) == len(set(shortlist)) and {r['id'] for r in rows} == set(shortlist),
            'Selection does not match the frozen shortlist')
    order = {pid: i for i, pid in enumerate(shortlist)}
    require(primary in order, 'Missing designated incumbent')
    scores = {r['id']: wins(r) for r in rows}
    maximum = max(scores.values())
    leaders = [r['id'] for r in rows if scores[r['id']] == maximum]
    ranked = sorted(rows, key=lambda r: (-scores[r['id']], r['cells'][0]['guardianHealth'] if maximum == 0 else 0,
                                         order[r['id']], r['id']))
    if maximum > 0 and len(leaders) > 1 and primary in leaders:
        return primary, 'DesignatedPositiveTie', leaders
    return ranked[0]['id'], ('ZeroWinHealth' if maximum == 0 else 'UniqueMaximum' if len(leaders) == 1 else 'FrozenDiscoveryOrder'), leaders


def observation(row):
    return dict(fitness=row['fitness'], cells=[{k:v for k,v in c.items() if k != 'trials'} for c in row['cells']],
                behavior=row['behavior'])


def arm_diagnosis(search, label):
    discovery = search['discovery']
    require(discovery['status'] == 'Complete' and len(discovery['arms']) == 1, 'Incomplete search')
    arm = discovery['arms'][0]
    proposals, evaluations = arm['proposals'], arm['evaluations']
    require(arm['stopReason'] == 'CandidateBudgetReached' and len(evaluations) == 46 and len(proposals) <= 256,
            'Changed search budget')
    measured = {r['id']:r for r in evaluations}
    accepted = {p['party']['id']:p for p in proposals if p['result'] == 'evaluated'}
    require(len(measured) == len(accepted) == 46 and set(measured) == set(accepted), 'Missing or duplicate measurement')
    references = {pid for pid,p in accepted.items() if p['provenance']['operator'] == 'supplied'}
    require(len(references) == 3 and PRIMARY in references, 'Missing protected references')
    ranked = [r['id'] for r in sorted(evaluations, key=rank_key)]
    challenger_rank = [pid for pid in ranked if pid not in references]
    expected_members = references | set(challenger_rank[:2])
    shortlist = [p['id'] for p in discovery['discoveryShortlist']]
    require(shortlist == [pid for pid in ranked if pid in expected_members], 'Wrong nomination membership or order')
    selected, reason, leaders = select(search['selection'], shortlist)
    require(selected == search['output']['finalist']['party']['id'], 'Wrong saved selector output')
    selection = {r['id']:r for r in search['selection']}
    for row in evaluations:
        wins(row, 8)
    for row in search['selection']:
        wins(row, 32)

    by_proposal, ancestry, records, seen, opportunities = {}, {}, [], [], []
    populations, parent_uses = Counter(), Counter()
    for p in proposals:
        populations.update(p['supplied']['populationIds'])
        parent_uses.update(p['provenance']['parentIds'])
    refs_by_id = {p['provenance']['referenceIds'][0]:p['party'] for p in proposals if p['provenance']['operator'] == 'supplied'}
    visits, cursors = Counter(), Counter()
    for index, p in enumerate(proposals):
        trace = p['provenance']; op = trace['operator']; pid = p['party']['id'] if p['party'] else None
        parents = trace['parentIds']
        if op != 'supplied':
            require(all(x in by_proposal and by_proposal[x]['result'] == 'evaluated' for x in parents), 'Unresolved evaluated parent')
        roots = set().union(*(ancestry.get(x, set()) for x in parents))
        if op == EXPLORATION:
            require(label == 'candidate' and index >= 12 and (index-12) % 4 == 0, 'Unexpected exploration opportunity')
            step = p['supplied']['exploration']; ref = sorted(refs_by_id)[len(opportunities) % 3]
            visit = visits[ref]; radius = 2+visit % 2
            owners = sorted(map(int, refs_by_id[ref]['builds']))
            slots = sorted(owners[(cursors[ref]+j) % len(owners)] for j in range(radius))
            require(step['referenceId'] == ref and step['visit'] == visit and step['radius'] == radius
                    and step['scheduledSlots'] == slots and 1 <= step['constructionChecks'] <= 32, 'Wrong exploration schedule')
            require(len(parents) == 1 and by_proposal[parents[0]]['party']['id'] == refs_by_id[ref]['id'], 'Indirect exploration parent')
            visits[ref] += 1; cursors[ref] += radius
            if p['result'] == 'evaluated':
                changed = [int(slot) for slot,ids in p['party']['builds'].items() if set(ids) != set(refs_by_id[ref]['builds'][slot])]
                distance = sum(len(set(ids)^set(refs_by_id[ref]['builds'][slot])) for slot,ids in p['party']['builds'].items())
                require(sorted(changed) == slots and distance == radius*2, 'Wrong realized exploration edits')
            roots.add(trace['id']); opportunities.append(index)
        ancestry[trace['id']] = roots; by_proposal[trace['id']] = p
        if p['result'] == 'evaluated':
            seen.append(measured[pid])
        category = 'direct' if op == EXPLORATION else 'descendant' if roots else 'other'
        record = dict(proposal=index, proposalId=trace['id'], partyId=pid, operator=op, result=p['result'], category=category,
                      parents=parents, explorationAncestors=sorted(roots), exploration=p['supplied'].get('exploration'),
                      populationAppearances=populations[pid], parentOrDonorUses=parent_uses[trace['id']])
        if p['result'] == 'evaluated':
            record.update(discoveryWins=wins(measured[pid]), discoveryRank=ranked.index(pid)+1,
                challengerRank=challenger_rank.index(pid)+1 if pid not in references else None,
                topFourImmediately=pid in [r['id'] for r in sorted(seen, key=rank_key)[:4]],
                nominated=pid in shortlist, selected=pid == selected,
                selectionWins=wins(selection[pid]) if pid in selection else None)
        records.append(record)
    require(opportunities == (list(range(12, len(proposals), 4)) if label == 'candidate' else []), 'Missing exploration slot')
    groups = {}
    for category in ['direct', 'descendant', 'other']:
        group = [r for r in records if r['category'] == category]; evaluated = [r for r in group if r['result']=='evaluated']
        groups[category] = dict(attempts=len(group), results=dict(Counter(r['result'] for r in group)), evaluated=len(evaluated),
            nominated=sum(r['nominated'] for r in evaluated), selected=sum(r['selected'] for r in evaluated),
            topFourImmediately=sum(r['topFourImmediately'] for r in evaluated), parentOrDonorUses=sum(r['parentOrDonorUses'] for r in group),
            discoveryWins=sum(r['discoveryWins'] for r in evaluated), discoveryTrials=8*len(evaluated),
            winHistogram=dict(sorted(Counter(r['discoveryWins'] for r in evaluated).items())))
    return dict(label=label, attempts=len(proposals), evaluated=46, references=sorted(references),
        operators=dict(Counter(r['operator'] for r in records)), results=dict(Counter(r['result'] for r in records)),
        groups=groups, shortlist=shortlist, selected=selected, selectionReason=reason, selectionLeaders=leaders,
        selectionMaximum=max(wins(r) for r in search['selection']),
        nominees=[dict(next(r for r in records if r['result']=='evaluated' and r['partyId']==pid),
                       gapToSelectionMaximum=max(wins(r) for r in search['selection'])-wins(selection[pid])) for pid in shortlist],
        records=records)


def compare(pair, baseline, candidate):
    a, b = arm_diagnosis(baseline, 'baseline'), arm_diagnosis(candidate, 'candidate')
    aset, bset = [{r['partyId'] for r in arm['records'] if r['result']=='evaluated'} for arm in (a,b)]
    for stage in ['discovery', 'selection']:
        ar, br = [search['discovery']['arms'][0]['evaluations'] if stage=='discovery' else search['selection']
                  for search in (baseline,candidate)]
        am, bm = ({r['id']:r for r in rows} for rows in (ar,br))
        require(all(observation(am[pid]) == observation(bm[pid]) for pid in am.keys() & bm.keys()), 'Shared recipe observations differ')
    same_nominees = set(a['shortlist']) == set(b['shortlist'])
    require(not same_nominees or a['shortlist'] == b['shortlist'], 'Unexpected rank change for identical nominees')
    source = next((r for r in b['records'] if r['result']=='evaluated' and r['partyId']==a['selected']), None)
    ordinary_divergence = []
    for x,y in zip(a['records'],b['records']):
        if x['operator'] not in ('supplied','fresh-legal') and y['operator'] != EXPLORATION:
            if any(x[k] != y[k] for k in ('partyId','parents','result')):
                ordinary_divergence.append(dict(proposal=x['proposal'], baseline=x, candidate=y))
    return dict(pair=pair, sharedEvaluated=len(aset & bset), baselineOnly=len(aset-bset), candidateOnly=len(bset-aset),
        sameNominees=same_nominees, sameOutput=a['selected']==b['selected'],
        baselineOutputInCandidate=None if source is None else dict(proposal=source['proposal'], challengerRank=source['challengerRank'], nominated=source['nominated']),
        firstOrdinaryDivergence=ordinary_divergence[0] if ordinary_divergence else None,
        ordinaryDivergences=len(ordinary_divergence), baseline=a, candidate=b)


def stage_summary(result):
    """Additional counts from the already verified search-only diagnostic rows."""
    slots, operator_totals, pair_rows, nominees = {}, {}, [], []
    below, tied, checks, radius = 0, 0, Counter(), Counter()
    for pair in result['pairs']:
        candidate = pair['candidate']
        direct = [r for r in candidate['records'] if r['category']=='direct']
        cutoff = next(r for r in candidate['records'] if r.get('challengerRank')==2)
        lower = sum(r['discoveryWins'] < cutoff['discoveryWins'] for r in direct if r['result']=='evaluated')
        equal = sum(r['discoveryWins'] == cutoff['discoveryWins'] and not r['nominated'] for r in direct if r['result']=='evaluated')
        below += lower; tied += equal
        for r in direct:
            step = r['exploration']; ref = step['referenceId']
            slots.setdefault(ref, Counter({slot:0 for slot in range(1,11)})).update(step['scheduledSlots'])
            checks[step['constructionChecks']] += 1; radius[step['radius']] += 1
        for r in candidate['nominees']:
            if r['category']=='direct':
                nominees.append(dict(pair=pair['pair'], partyId=r['partyId'], discoveryWins=r['discoveryWins'],
                    selectionWins=r['selectionWins'], selectionWinner=candidate['selected'],
                    winnerSelectionWins=candidate['selectionMaximum'], gap=r['gapToSelectionMaximum']))
        pair_rows.append(dict(pair=pair['pair'], sharedEvaluated=pair['sharedEvaluated'], direct=len(direct),
            descendants=candidate['groups']['descendant']['evaluated'], belowWinCutoff=lower, tiedBelowCutoff=equal,
            directNominated=candidate['groups']['direct']['nominated'], sameNominees=pair['sameNominees'],
            sameOutput=pair['sameOutput'], baselineOutputGenerated=pair['baselineOutputInCandidate'] is not None,
            ordinaryDivergences=pair['ordinaryDivergences']))
        for label in ['baseline','candidate']:
            totals=operator_totals.setdefault(label,{})
            for r in pair[label]['records']:
                group=totals.setdefault(r['operator'],dict(attempts=0,evaluated=0,discoveryWins=0,nominated=0,selected=0))
                group['attempts'] += 1
                if r['result']=='evaluated':
                    group['evaluated'] += 1; group['discoveryWins'] += r['discoveryWins']
                    group['nominated'] += r['nominated']; group['selected'] += r['selected']
    return dict(scope='Descriptive search-stage counts; no counterfactual strength or confirmation reranking.',
        pairs=pair_rows, directNominees=nominees, directBelowWinCutoff=below, directTiedBelowCutoff=tied,
        directNomineeDiscoveryWins=sum(r['discoveryWins'] for r in nominees), directNomineeDiscoveryTrials=8*len(nominees),
        directNomineeSelectionWins=sum(r['selectionWins'] for r in nominees), directNomineeSelectionTrials=32*len(nominees),
        scheduledSlotCounts={ref:dict(counts) for ref,counts in slots.items()}, constructionCheckHistogram=dict(checks),
        radiusHistogram=dict(radius), operators=operator_totals)


def run(output):
    started = time.monotonic()
    require(output.resolve() == OUTPUT.resolve() and not output.exists(), 'Use the new external diagnosis directory')
    common_path = ADMISSION/'comparison-preparation.py'
    require(sha(common_path) == COMMON_PIN, 'Changed history/authentication helper')
    spec = importlib.util.spec_from_file_location('diagnosis_common', common_path)
    common = importlib.util.module_from_spec(spec); spec.loader.exec_module(common)
    manifests = {root:common.authenticate(root,pin) for root,pin in
                 [(RUN,RUN_PIN),(EXECUTION,EXECUTION_PIN),(ADMISSION,ADMISSION_PIN)]}
    print('Authenticated scientific archive, execution closeout and admission.', flush=True)
    closeout = read(EXECUTION/'closeout.json')
    require(closeout['scope']=='Closed' and closeout['status']=='ReferenceExplorationComparisonExecutionVerified', 'Unclosed source')
    frozen = read(RUN/'study/outputs-freeze.json')
    require(frozen['completedAttempts']==12672 and len(frozen['searches'])==12, 'Incomplete global output freeze')
    pairs = []
    for i in range(1,13):
        arms = [read(RUN/f'study/pair-{i:02d}-{label}.json') for label in ['baseline','candidate']]
        for label,arm in zip(['baseline','candidate'],arms):
            require(arm == frozen['searches'][i-1][label], 'Search differs from frozen output')
            require(arm['discovery'] == read(RUN/f'study/pair-{i:02d}-{label}-discovery.json'), 'Checkpoint differs from final search')
        pairs.append(compare(i,*arms))
    direct = [r for p in pairs for r in p['candidate']['records'] if r['category']=='direct']
    descendants = [r for p in pairs for r in p['candidate']['records'] if r['category']=='descendant']
    summary = dict(pairs=12, searches=24, identicalNomineePairs=sum(p['sameNominees'] for p in pairs),
        identicalOutputPairs=sum(p['sameOutput'] for p in pairs), directAttempts=len(direct),
        directEvaluated=sum(r['result']=='evaluated' for r in direct), directNominated=sum(r.get('nominated',False) for r in direct),
        directSelected=sum(r.get('selected',False) for r in direct), directTopFour=sum(r.get('topFourImmediately',False) for r in direct),
        directParentOrDonorUses=sum(r['parentOrDonorUses'] for r in direct),
        descendantAttempts=len(descendants), descendantEvaluated=sum(r['result']=='evaluated' for r in descendants),
        descendantNominated=sum(r.get('nominated',False) for r in descendants), descendantSelected=sum(r.get('selected',False) for r in descendants),
        selectionReasons=dict(Counter(p[label]['selectionReason'] for p in pairs for label in ['baseline','candidate'])),
        minimumSharedEvaluated=min(p['sharedEvaluated'] for p in pairs), maximumSharedEvaluated=max(p['sharedEvaluated'] for p in pairs))
    print(json.dumps(summary), flush=True)
    # Refresh the full supported registry, including abandoned-reservation recovery.
    history, values = common.history(RUN.parent, read(RUN/'request.json'))
    require(history == read(EXECUTION/'live-history-files.json') and len(values)==567789, 'History changed since execution closeout')
    # Check membership and file hashes again after the analysis; no input writes.
    pins = {RUN:RUN_PIN, EXECUTION:EXECUTION_PIN, ADMISSION:ADMISSION_PIN}
    for root,manifest in manifests.items():
        require(common.inventory(root)==dict(manifest, **{'files.json':pins[root]}), 'Source package changed during diagnosis')
    output.mkdir()
    result = dict(status='VerifiedSavedReferenceExplorationDiagnosis', summary=summary, pairs=pairs,
        scope='Descriptive search-stage diagnosis of twelve frozen pairs; no new policy performance estimate.',
        newFights=0,newValues=0,newParties=0,nativePreparations=0,changedPolicy=False,
        historyValues=len(values),historyFiles=len(history),historyPreserved=True,seconds=time.monotonic()-started,
        scientificManifestSha256=RUN_PIN,executionManifestSha256=EXECUTION_PIN,admissionManifestSha256=ADMISSION_PIN,
        accounting='Engineering separately disclosed; incomplete older totals and the prior-charge ledger remain unchanged.')
    save(output/'diagnosis.json', result); save(output/'history-files.json', history)
    save(output/'stage-summary.json', stage_summary(result))
    save(output/'input-manifests.json', {str(root):sha(root/'files.json') for root in manifests})
    with (output/'analysis.py').open('xb') as stream: stream.write(Path(__file__).read_bytes())
    print(json.dumps({k:v for k,v in result.items() if k!='pairs'},indent=2))


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,default=OUTPUT)
    run(parser.parse_args().output)
