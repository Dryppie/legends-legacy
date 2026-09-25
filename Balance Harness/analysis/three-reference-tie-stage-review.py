"""Diagnose the closed selector comparison using authenticated saved observations only.

No native invocation, random draw, policy fitting or absolute confirmation estimate.
Half-panel and leave-one-out checks are overlapping descriptive diagnostics.
"""
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
from pathlib import Path
import time

ROOT = Path(__file__).resolve().parents[2]
RUN = ROOT/'TestResults/balance/tower-three-reference-tie-comparison-20260923'
ADMISSION = ROOT/'TestResults/three-reference-tie-admission-20260923'
EXECUTION = ROOT/'TestResults/three-reference-tie-comparison-execution-20260923'
OUT = ROOT/'TestResults/three-reference-tie-stage-review-20260923'
PINS = {RUN:'68f7468ba270f1d51a075772a6fed3d800ad627a1376a00c4829c10ef9d806d1',
        ADMISSION:'90b9a5a9815454ba1169d64f61cc8b7a950597cfa56bc1c9380c53155538f1c5',
        EXECUTION:'f05051cf90896bb9d9497a07b32ca207c996886608daa3579b828b1f53bc9507'}
STAGE = ROOT/'TestResults/practical-search-stage-review-20260922/analysis.py'
STAGE_PIN = '2035037476ab995cd4140b9931fb223a88585b2dd0ed36b69f2e7cef8334e0e1'
COMMON = ROOT/'TestResults/reference-exploration-comparison-admission-20260922/comparison-preparation.py'
COMMON_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
REFERENCES = ['399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b',
              '8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50',
              '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c']
VERSION = 'tower-three-reference-tie-comparison-v1'


def module(name,path,pin):
    if hashlib.sha256(path.read_bytes()).hexdigest() != pin:
        raise ValueError('Changed saved helper: '+str(path))
    spec = importlib.util.spec_from_file_location(name,path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


stage = module('saved_tie_stage_tools',STAGE,STAGE_PIN)
old = stage.old
read,save,sha,require = old.read,old.save,old.sha,old.require


def classify(scores,order,primary,references,selected):
    require(set(scores) == set(order) and len(order) == len(set(order)) and set(references) <= scores.keys()
            and primary in references and selected in scores, 'Invalid selection family')
    top = max(scores.values())
    leaders = [p for p in order if scores[p] == top]
    require(selected in leaders,'Output below maximum')
    if top == 0:
        return 'ZeroWinHealth'
    if len(leaders) == 1:
        return 'UniqueReferenceLeader' if selected in references else 'UniqueChallengerLeader'
    if primary in leaders:
        require(selected == primary,'Primary tie not retained')
        return 'PrimaryPositiveTie'
    require(selected == leaders[0],'Changed positive-tie nominee order')
    if any(p in references for p in leaders):
        return 'OtherReferenceAlreadyFirst' if selected in references else 'UnprotectedReferenceTie'
    return 'ChallengerOnlyTie'


def arm(search,trials,values,audit):
    require(len(values) == len(set(values)) == 41, 'Invalid root panel')
    discovery = search['discovery']
    raw = discovery['arms'][0]
    require(discovery['version'] == 'retained-composition-three-references-v1'
            and raw['seed'] == values[0] and 'screening' not in search, 'Changed direct generator')
    base = stage.arm(dict(discovery=discovery,selection=search['selection'],output=search['baseline']),trials)
    accepted = {p['party']['id']:p['party'] for p in raw['proposals'] if p['result'] == 'evaluated'}
    supplied = {p['party']['id'] for p in raw['proposals'] if p['provenance']['operator'] == 'supplied'}
    require(supplied == set(REFERENCES) and discovery['discoveryShortlist'] == [accepted[p] for p in base['shortlist']],
            'Changed exact supplied references or nominee recipes')
    for rows,seeds,label in [(raw['evaluations'],values[1:9],'discovery'),(search['selection'],values[9:41],'selection')]:
        require(all([trials[t]['seed'] for t in row['cells'][0]['trials']] == seeds for row in rows), 'Wrong reserved '+label+' panel')
    counts = {r['id']:old.wins(r,32) for r in search['selection']}
    health = {r['id']:r['cells'][0]['guardianHealth'] for r in search['selection']}
    order = base['shortlist']
    a,b = audit.choices(counts,health,{pid:i for i,pid in enumerate(order)},REFERENCES[0],REFERENCES,VERSION)
    for side,pid,selector in [('baseline',a,'tower-staged-incumbent-tie-v1'),('candidate',b,'tower-staged-three-reference-tie-v1')]:
        require(search[side]['selector'] == selector and search[side]['finalist']['party'] == accepted[pid], 'Changed '+side+' output')
    require(a == b == base['selected'] and search['baseline']['recipeHash'] == search['candidate']['recipeHash'],
            'Unexpected selector difference')
    challengers = [p for p in order if p not in REFERENCES]
    require(len(challengers) == 2, 'Changed challenger family')
    best = max(counts[p] for p in REFERENCES)
    tied_refs = [p for p in order if p in REFERENCES and counts[p] == best]
    best_ref = REFERENCES[0] if REFERENCES[0] in tied_refs else tied_refs[0]
    clears = {r['id']:r['cells'][0]['clears'] for r in search['selection']}
    paired = {ref:dict(gains=sum(x and not y for x,y in zip(clears[a],clears[ref])),
                       losses=sum(y and not x for x,y in zip(clears[a],clears[ref]))) for ref in REFERENCES}
    reason = classify(counts,order,REFERENCES[0],REFERENCES,a)
    return dict(base,root=search['restart'],selectionWins=counts,challengers=challengers,
        selectedReference=a in REFERENCES,classification=reason,bestReference=best_ref,bestReferenceWins=best,
        winnerReferenceMargin=counts[a]-best,selectedVersusReferences=paired,
        nonprimaryReferenceChallengerTopTie=counts[a]>0 and REFERENCES[0] not in base['leaders']
            and bool(set(base['leaders'])&set(REFERENCES)) and bool(set(base['leaders'])-set(REFERENCES)),
        selectedRecipeHash=search['baseline']['recipeHash'],absoluteConfirmationWins=None,
        referenceHalfWins={ref:[sum(clears[ref][:16]),sum(clears[ref][16:])] for ref in REFERENCES},
        selectedHalfWins=[sum(clears[a][:16]),sum(clears[a][16:])])


def summarize(arms):
    s = stage.summarize(arms)
    chosen = [a for a in arms if not a['selectedReference']]
    s.update(classifications=dict(Counter(a['classification'] for a in arms)),
        outputCounts=dict(Counter(a['selected'] for a in arms)),referenceOutputs=sum(a['selectedReference'] for a in arms),
        challengerOutputs=len(chosen),nonprimaryReferenceChallengerTopTies=sum(a['nonprimaryReferenceChallengerTopTie'] for a in arms),
        differingOutputs=0,challengerRoots=[a['root'] for a in chosen],
        challengerReferenceMargins=dict(Counter(a['winnerReferenceMargin'] for a in chosen)),
        challengerHalfDisagreements=sum(a['sensitivity']['halvesDisagree'] is True for a in chosen),
        challengerAnyLeaveOneOutChange=sum(a['sensitivity']['leaveOneOutChanges']>0 for a in chosen),
        challengerLeaveOneOutChanges=sum(a['sensitivity']['leaveOneOutChanges'] for a in chosen))
    return s


def run(output):
    started = time.monotonic()
    require(output.resolve() == OUT.resolve() and output.is_dir() and not (output/'review.json').exists(), 'Use new external review evidence')
    common = module('tie_stage_history',COMMON,COMMON_PIN)
    inventories = {}
    for path,pin in PINS.items():
        inventories[path] = dict(common.authenticate(path,pin),**{'files.json':pin})
        print('Authenticated '+path.name,flush=True)
    closeout,result = read(EXECUTION/'closeout.json'),read(RUN/'result.json')
    require(closeout['scope'] == 'Closed' and closeout['nativeAudit'] == closeout['independentAudit'] == 'Passed'
            and closeout['result'] == result == read(RUN/'independent-audit.json')['result']
            and result['decision'] == 'NoSelectorDifferences' and result['fights'] == 12672, 'Unverified closed comparison')
    audit = module('saved_tie_auditor',RUN/'auditor.py',read(RUN/'request.json')['auditorHash'])
    frozen,study = read(RUN/'study/outputs-freeze.json'),read(RUN/'study/study.json')
    require(study['freeze'] == frozen and frozen['version'] == VERSION and frozen['completedAttempts'] == 12672
            and len(frozen['searches']) == len(result['pairs']) == 24 and frozen['activeRestarts'] == []
            and study['evidence'] == [], 'Changed freeze or unexpected confirmation')
    journal = (RUN/'study/trials.jsonl').read_bytes()
    require(journal.endswith(b'\n'),'Torn journal')
    rows = [json.loads(line) for line in journal.splitlines()]
    require([t['id'] for t in rows] == [f'trial-{i+1:06d}' for i in range(12672)],'Incomplete trial order')
    trials = {t['id']:t for t in rows}
    values = read(RUN/'allocation.json')['selected']
    require(len(values) == len(set(values)) == 24984,'Changed allocation')
    arms = []
    for i in range(1,25):
        s = read(RUN/f'study/search-{i:02d}.json')
        require(s == frozen['searches'][i-1] and s['restart'] == i
                and s['discovery'] == read(RUN/f'study/search-{i:02d}-discovery.json'),'Changed checkpoint')
        ids = [t for row in s['discovery']['arms'][0]['evaluations']+s['selection'] for t in row['cells'][0]['trials']]
        require(ids == [t['id'] for t in rows[(i-1)*528:i*528]],'Changed physical search partition')
        a = arm(s,trials,values[(i-1)*41:i*41],audit)
        p = result['pairs'][i-1]
        require(p['restart'] == i and p['identical'] and p['baselineParty'] == p['candidateParty'] == a['selected']
                and p['baselineWins'] is None and p['candidateWins'] is None and p['gains'] == p['losses'] == p['difference'] == 0,
                'Changed closed endpoint or invented absolute outcomes')
        arms.append(a)
    history,excluded = common.history(RUN.parent,read(RUN/'request.json'))
    require(history == read(EXECUTION/'live-history-files.json') and len(excluded) == 633313 and len(history) == 240,'Changed permanent history')
    for path,inventory in inventories.items():
        require(common.inventory(path) == inventory,'Source changed during review')
    review = dict(status='VerifiedThreeReferenceTieStageReview',roots=arms,summary=summarize(arms),searches=24,
        closedDecision=result['decision'],newFights=0,newValues=0,newParties=0,nativePreparations=0,changedPolicy=False,
        historyValues=len(excluded),historyFiles=len(history),seconds=time.monotonic()-started,
        scope='Descriptive saved-stage diagnosis. No fitted selector or strength estimate; all absolute confirmation outcomes remain unknown.',
        sensitivity='Existing fixed first/last 16 and all 32 leave-one-out panels; overlapping diagnostics, zero-win subsets unassessed.',
        accounting='Engineering separately disclosed; complete historical totals unknown; 18,180-second /13,584-MiB ledger preserved.')
    save(output/'review.json',review)
    save(output/'history-files.json',history)
    save(output/'input-manifests.json',{str(p):pin for p,pin in PINS.items()})
    save(output/'helper-pins.json',{str(STAGE):STAGE_PIN,str(COMMON):COMMON_PIN,str(stage.OLD):stage.OLD_PIN,str(RUN/'auditor.py'):sha(RUN/'auditor.py')})
    save(output/'selected-challengers.json',dict(status='UnconfirmedInClosedComparison',sourceManifestSha256=PINS[RUN],
        scope='All five selected challengers; no selection by independent outcomes and no launch request.',
        candidates=[dict(root=a['root'],partyId=a['selected'],recipeHash=a['selectedRecipeHash'],
            sourceCheckpoint=str(RUN/f"study/search-{a['root']:02d}.json"),absoluteConfirmationWins=None)
            for a in arms if not a['selectedReference']]))
    (output/'analysis.py').write_bytes(Path(__file__).read_bytes())
    print(json.dumps({k:v for k,v in review.items() if k != 'roots'},indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,required=True)
    run(parser.parse_args().output)
