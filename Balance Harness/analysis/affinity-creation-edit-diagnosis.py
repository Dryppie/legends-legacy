"""Read-only, post-hoc join of frozen generation edits and audited recognition.

No simulator, entropy source, selector fitting or missing-outcome imputation.
Group means describe measured occurrences only; they are not causal effects or
population estimates. The sealed recognition review remains unchanged.
"""
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
BENCHMARK = '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c'
VERSION = 'tower-affinity-creation-edit-diagnosis-v1'
SECONDS, BYTES = 180, 64*1048576
PACKAGES = {
    'recognition': ('TestResults/affinity-creation-recognition-review-20260924', '54175d22ad3b2e622a4313d14c378e1d0036560ceb92e73d8ce9b709311111de'),
    'scientific': ('TestResults/balance/tower-affinity-creation-recognition-20260924', 'f1bb922ea31e59e8c7f270b3ae05f95fff111a35524d26e70ee22739add2e897'),
    'catalogue': ('TestResults/affinity-creation-recognition-catalogue-20260924', 'ccd05f08fda2c86ab014c12ca056d061053683d7b47c69cafee6902a08831814'),
    'generation': ('TestResults/balance/tower-benchmark-validation-pilot-01-20260924', 'fd96c4d21a27a32ad17ab750d2972a6b7dab54d09950661042b9f3af56859504'),
    'admission': ('TestResults/benchmark-validation-admission-20260924', 'e6fdb1369df6585fde905523e3190af221ef0dc9323e4b3ae3b1bf37659b5576')}
HANDOFF = 'TestResults/affinity-creation-recognition-execution-20260924'
HANDOFF_PIN = 'ba1e42dc80b9a852e77208eb22dcaf9871749db1e9b5b312f72c2418be26d276'
PLAN = 'Balance Harness/Tower-Affinity-Creation-Recognition-Plan.json'
PLAN_PIN = '170065b593a49609e72142d766443c3a47f7a271b937cc88d52436de2b4792a2'
LIMITATIONS = ('Post-hoc descriptive development diagnosis only. Measured-only group means are not finite-population means, '
    'causal Essence effects, independent-recipe estimates, fitted policy scores or qualification endpoints. '
    'Shared root panels, repeated recipes, training-based strata and incomplete lower-stratum sampling limit comparisons. '
    'Individual family-540 combat intervals are retained unchanged; no group confidence intervals are created. '
    'Historical combat outcomes are not extracted or pooled. All unmeasured outcomes remain null.')


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def digest(value):
    text = json.dumps(value,sort_keys=True,separators=(',',':'),ensure_ascii=True,allow_nan=False)
    for char in ['+', '<', '>', '&', "'"]:
        text = text.replace(char,'\\u%04X' % ord(char))
    return sha(text.encode())


class Evidence:
    def __init__(self, relative, pin):
        self.root = (ROOT/relative).resolve()
        raw = (self.root/'files.json').read_bytes()
        require(sha(raw)==pin,'Changed external manifest: '+relative)
        self.files, self.consumed = json.loads(raw), {'files.json':pin}

    def bytes(self, name):
        path = (self.root/name).resolve()
        require(name in self.files and path.is_relative_to(self.root),'Unbound evidence path')
        raw = path.read_bytes()
        require(sha(raw)==self.files[name],'Changed consumed evidence: '+name)
        self.consumed[name] = sha(raw)
        return raw

    def read(self, name):
        return json.loads(self.bytes(name))

    def recheck(self):
        for name,pin in self.consumed.items():
            require(sha((self.root/name).read_bytes())==pin,'Evidence changed during diagnosis')


def builds(team):
    return {str(a['partySlot']):a['build']['essenceIds'] for a in team['scenario']['party']}


def physical(scenario):
    value = json.loads(json.dumps(scenario))
    value['seeds'] = []
    for actor in value['party']:
        actor['build']['essenceIds'] = []
    return value


def selected_affinities(affinity_report, plan):
    require(affinity_report['version']=='tower-damage-source-affinity-v1'
        and affinity_report['inventoryHash']==digest(plan['damageAffinityInventory']), 'Changed affinity inventory')
    selected = plan['policy']['createdDamageAffinityIds']
    affinities = {a['id']:a for a in affinity_report['affinities'] if a['id'] in selected}
    require(len(affinities)==len(selected)==len(set(selected)),'Missing or duplicate selected affinity')
    nodes = {n['key']:n for n in plan['damageAffinityInventory']['nodes']}
    for aid,a in affinities.items():
        require(aid==digest(dict(version=affinity_report['version'],producer=a['producerEssenceId'],
            modifier=a['modifierEssenceId'],producerRoute=a['producerRoute'],modifierRoute=a['modifierRoute'],
            damageType=a['damageType'],sourceScope=a['sourceScope'])), 'Changed authored affinity identity')
        require(a['producerEssenceId']!=a['modifierEssenceId']
            and all(k in nodes and nodes[k]['unknowns']==[] for k in a['producerRoute']+a['modifierRoute']),
            'Unresolved affinity route')
    return affinities


def edit_metadata(proposal, parent, team, affinities):
    party = proposal['party']
    require(proposal['rejection'] is None and proposal['requestedOperator']==proposal['effectiveOperator']=='affinity-create'
        and proposal['parentSource']=='benchmark' and proposal['parents']==[BENCHMARK]
        and parent['id']==BENCHMARK and digest(parent['builds'])==BENCHMARK, 'Changed proposal lineage')
    require(party['id']==team['partyId']==digest(party['builds']) and party['builds']==builds(team),
        'Changed generated recipe identity')
    require(set(parent['builds'])==set(party['builds'])==set(map(str,range(1,11))), 'Changed owner membership')
    changed = [int(o) for o in parent['builds'] if parent['builds'][o]!=party['builds'][o]]
    require(len(changed)==1 and proposal['changedOwners']==proposal['scheduledOwners']==changed,
        'Changed edited owner')
    owner = changed[0]
    old,new = set(parent['builds'][str(owner)]),set(party['builds'][str(owner)])
    removed,added = sorted(old-new),sorted(new-old)
    step = proposal['affinityCreation']
    require(len(removed)==len(added)==proposal['replacementDistance'] and len(removed) in (1,2)
        and step['removed']==removed and step['added']==added, 'Changed replacement metadata')
    pairs = {}
    for aid,a in affinities.items():
        pair = sorted([a['producerEssenceId'],a['modifierEssenceId']])
        entry = pairs.setdefault(digest(pair),dict(essences=pair,ids=[]))
        entry['ids'].append(aid)
    require(step['pairId'] in pairs,'Unknown target pair')
    pair = pairs[step['pairId']]
    require(step['targetAffinityIds']==sorted(pair['ids']) and sorted(set(pair['essences'])-old)==added
        and set(pair['essences'])<=new, 'Changed minimal target-pair placement')
    def active(ids):
        return {aid for aid,a in affinities.items() if {a['producerEssenceId'],a['modifierEssenceId']}<=ids}
    newly,lost = sorted(active(new)-active(old)),sorted(active(old)-active(new))
    require(step['newlyActivatedAffinityIds']==newly and set(step['targetAffinityIds'])<=set(newly),
        'Changed authored activation')
    actor = next(a['build'] for a in team['scenario']['party'] if a['partySlot']==owner)
    return dict(parentId=BENCHMARK,owner=owner,subgroup=1+(owner-1)//5,removed=removed,added=added,
        replacementDistance=len(removed),pairId=step['pairId'],pairEssences=pair['essences'],
        targetAffinityIds=step['targetAffinityIds'],newlyActivatedAffinityIds=newly,
        deactivatedSelectedAffinityIds=lost,ownerContext={k:v for k,v in actor.items() if k!='essenceIds'},
        attempt=proposal['attempt'],requestedOperator=proposal['requestedOperator'],effectiveOperator=proposal['effectiveOperator'])


def source_root(cat, plan, pair, affinity_report):
    report = pair['candidate']
    require(cat['sourceReportRole']=='candidate' and pair['status']=='Complete'
        and plan['version']==report['version']=='tower-proposal-racing-v5'
        and plan['selectionPolicyVersion']==report['selectionPolicyVersion']=='tower-racing-benchmark-validation-v1'
        and cat['sourcePlanHash']==report['planHash']==digest(plan) and report['policyHash']==digest(plan['policy']),
        'Changed source plan or report binding')
    scope,e = plan['racing']['scope'],report['evaluation']
    require(plan['policy']['parentTickets']==['benchmark'] and scope['ownedCopies'] is None
        and e['status']=='Complete' and e['planHash']==report['planHash'] and [b['wave'] for b in report['batches']]==[1,2],
        'Changed generation scope')
    parents = [s['party'] for s in scope['starts']]
    require([t['partyId'] for t in cat['teams'][:3]]==[p['id'] for p in parents]
        and cat['benchmarkPartyId']==parents[2]['id']==BENCHMARK,'Changed reference identities')
    anchor = cat['teams'][2]
    require(builds(anchor)==parents[2]['builds'], 'Changed parent scenario')
    catalogue = {t['partyId']:t for t in cat['teams'][3:]}
    nominees = [pid for pid in e['nominees'] if pid not in {p['id'] for p in parents}]
    beam = e['decisions'][-1]['beamIds']
    require([t['partyId'] for t in cat['teams'] if t['stratum']=='nominee']==nominees
        and [t['partyId'] for t in cat['teams'] if t['stratum']=='near-miss']==[pid for pid in beam if pid not in nominees],
        'Changed training strata')
    affinities = selected_affinities(affinity_report,plan)
    accepted,rejections = {},Counter()
    allowed = {x['id']:x['family'] for x in scope['allowedEssences']}
    for batch in report['batches']:
        valid = [p for p in batch['proposals'] if p['rejection'] is None]
        require([p['party'] for p in valid]==batch['candidates'] and len(valid)==(9 if batch['wave']==1 else 8),
            'Changed accepted batch')
        rejections.update(p['rejection'] for p in batch['proposals'] if p['rejection'] is not None)
        for position,p in enumerate(valid,1):
            pid = p['party']['id']
            require(pid in catalogue and pid not in accepted,'Missing or duplicate catalogue proposal')
            team = catalogue[pid]
            require(team['scenario']['seeds']==[] and physical(team['scenario'])==physical(anchor['scenario']),
                'Changed physical owner context')
            for ids in p['party']['builds'].values():
                require(ids==sorted(set(ids)) and len(ids)==5 and all(x in allowed for x in ids)
                    and len({allowed[x].casefold() for x in ids})==5, 'Illegal generated recipe')
            accepted[pid] = dict(edit_metadata(p,parents[2],team,affinities),wave=batch['wave'],acceptedPosition=position)
    require(len(accepted)==len(catalogue)==17 and set(accepted)==set(catalogue),'Incomplete frozen population')
    return accepted,rejections,affinities


def summary(rows):
    measured = [r for r in rows if r['independentOutcome'] is not None]
    contrasts = [r['independentOutcome'] for r in measured]
    return dict(catalogueOccurrences=len(rows),distinctRecipes=len({r['partyId'] for r in rows}),
        measuredOccurrences=len(measured),distinctMeasuredRecipes=len({r['partyId'] for r in measured}),
        unmeasuredOccurrences=len(rows)-len(measured),measuredRoots=sorted({r['root'] for r in measured}),
        measuredStrata=dict(sorted(Counter(r['stratum'] for r in measured).items())),
        observedAbove=sum(c['observedGain']>0 for c in contrasts),observedEqual=sum(c['observedGain']==0 for c in contrasts),
        observedBelow=sum(c['observedGain']<0 for c in contrasts),
        intervalAboveZero=sum(c['lower']>0 for c in contrasts),intervalBelowZero=sum(c['upper']<0 for c in contrasts),
        measuredMeanGain=sum(c['observedGain'] for c in contrasts)/len(contrasts) if contrasts else None)


def diagnose(catalogue, recognition, sources, affinity_report):
    require(recognition['version']=='tower-affinity-creation-recognition-review-v1'
        and recognition['diagnosticDecision']=='CompleteDiagnosticOnly' and recognition['newFights']==recognition['newValues']==0,
        'Changed diagnostic input')
    measured = {(r['root'],r['candidateId']):r for r in recognition['measured']}
    require(len(measured)==len(recognition['measured'])==72 and recognition['unmeasuredCount']==132,
        'Incomplete or duplicate recognition cells')
    for r in measured.values():
        require(r['referenceId']==BENCHMARK and all(type(r[k]) in (int,float) and math.isfinite(r[k]) for k in ('observedGain','lower','upper'))
            and -1<=r['lower']<=r['observedGain']<=r['upper']<=1,'Invalid benchmark contrast')
    require([r['root'] for r in catalogue['roots']]==list(range(1,13)) and len(sources)==12,'Changed root family')
    rows,rejections,selected = [],Counter(),None
    for cat,(plan,pair) in zip(catalogue['roots'],sources,strict=True):
        accepted,rejected,affinities = source_root(cat,plan,pair,affinity_report)
        require(selected is None or selected==affinities,'Changed cross-root affinity meanings')
        selected = affinities
        rejections.update(rejected)
        for team in cat['teams'][3:]:
            key = cat['root'],team['partyId']
            outcome = measured.get(key)
            require(outcome is None and team['stratum']=='lower' or outcome is not None and outcome['stratum']==team['stratum'],
                'Changed measured stratum or missing upper observation')
            rows.append(dict(root=key[0],partyId=key[1],stratum=team['stratum'],
                inclusionProbability=dict(numerator=2,denominator=13) if team['stratum']=='lower' else dict(numerator=1,denominator=1),
                **accepted[key[1]],independentOutcome=outcome))
    require(len(rows)==204 and len({(r['root'],r['partyId']) for r in rows})==204
        and {key for key in measured}=={(r['root'],r['partyId']) for r in rows if r['independentOutcome'] is not None},
        'Missing or cross-root measurement join')
    dimensions = {
        'stratum':lambda r:[r['stratum']], 'owner':lambda r:[str(r['owner'])],
        'subgroup':lambda r:[str(r['subgroup'])], 'replacementDistance':lambda r:[str(r['replacementDistance'])],
        'addedSet':lambda r:[' + '.join(r['added'])], 'removedSet':lambda r:[' + '.join(r['removed'])],
        'removedEssence':lambda r:r['removed'], 'targetPair':lambda r:[r['pairId']],
        'activatedAffinitySet':lambda r:[' + '.join(r['newlyActivatedAffinityIds'])]}
    groups = {}
    for name,labels in dimensions.items():
        members = {}
        for row in rows:
            for label in labels(row):
                members.setdefault(label,[]).append(row)
        groups[name] = {label:summary(members[label]) for label in sorted(members)}
    return dict(version=VERSION,interpretation='PostHocDescriptiveEditsNoCausalAttributionOrPolicyFitting',newFights=0,newValues=0,
        sourceDiagnosticDecision=recognition['diagnosticDecision'],policyDefaultsChanged=False,benchmarkPartyId=BENCHMARK,
        summary=summary(rows),rejectedAttempts=dict(sorted(rejections.items())),selectedAffinities=list(selected.values()),
        groups=groups,rows=rows,limitations=LIMITATIONS)


def mechanics(diagnosis, inventory):
    ids = {e for row in diagnosis['rows'] for e in row['removed']+row['added']}
    essences = [e for e in inventory['essences'] if e['id'] in ids]
    require({e['id'] for e in essences}==ids,'Missing edited Essence mechanics')
    dependencies = {k for e in essences for k in e['dependencies']}
    nodes = [n for n in inventory['nodes'] if n['key'] in dependencies]
    require({n['key'] for n in nodes}==dependencies,'Missing mechanics dependency')
    return dict(inventoryHash=digest(inventory),sourceHashes=inventory['sourceHashes'],essences=essences,nodes=nodes,
        interpretation='CapturedAuthoredDefinitionsNotMeasuredUptimeOrCausalEffect')


def markdown(report):
    s = report['summary']
    lines = ['# Affinity-creation edit diagnosis','',report['limitations'],'',
        f"Joined {s['catalogueOccurrences']} occurrences / {s['distinctRecipes']} distinct recipes. Measured: {s['measuredOccurrences']}; unmeasured: {s['unmeasuredOccurrences']}.", '']
    for name in ('replacementDistance','owner','subgroup','addedSet','removedEssence'):
        lines += [f'## {name}','', '| Group | Catalogue | Measured | Unknown | Above / equal / below | Measured mean gain |',
            '| --- | ---: | ---: | ---: | ---: | ---: |']
        for label,g in report['groups'][name].items():
            mean = 'Unknown' if g['measuredMeanGain'] is None else f"{100*g['measuredMeanGain']:+.3f} pp"
            lines.append(f"| {label} | {g['catalogueOccurrences']} | {g['measuredOccurrences']} | {g['unmeasuredOccurrences']} | {g['observedAbove']} / {g['observedEqual']} / {g['observedBelow']} | {mean} |")
        lines.append('')
    lines += ['Removed-Essence groups overlap for two-slot edits. Means use measured occurrences with equal weights; lower-stratum sampling weights are not applied to these post-hoc summaries.', '',
        '[All 204 joined rows and group counts](diagnosis.json). [Captured mechanics for every added or removed Essence](mechanics.json).','']
    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    require(not out.exists() and out.is_relative_to(ROOT/'TestResults')
        and all(not out.is_relative_to(ROOT/relative) for relative,_ in PACKAGES.values()),'Existing or unsafe output')
    out.mkdir()
    started = time.monotonic()
    timer = threading.Timer(SECONDS,lambda:os._exit(124)); timer.daemon=True; timer.start()
    def save(name,value):
        with (out/name).open('x',encoding='utf-8',newline='\n') as f:
            json.dump(value,f,indent=2,allow_nan=False); f.write('\n')
    try:
        save('declaration.json',dict(version=VERSION,maximumSeconds=SECONDS,maximumBytes=BYTES,
            charge='FullAllowanceAtStartIncludingFailure',newFights=0,newValues=0,sourceManifestPins=PACKAGES,
            previousHandoffManifestSha256=HANDOFF_PIN,scope=LIMITATIONS))
        (out/'analysis.py').write_bytes(Path(__file__).read_bytes())
        evidence = {key:Evidence(*value) for key,value in PACKAGES.items()}
        recog,scientific = evidence['recognition'],evidence['scientific']
        raw = (ROOT/PLAN).read_bytes(); require(sha(raw)==PLAN_PIN,'Changed recognition plan')
        catalogue = evidence['catalogue'].read('catalogue.json')
        retained = recog.read('review.json')
        producer = recog.bytes('analysis.py')
        spec = importlib.util.spec_from_file_location('sealed_recognition_review',recog.root/'analysis.py')
        reviewer = importlib.util.module_from_spec(spec); spec.loader.exec_module(reviewer)
        result = scientific.read('result.json')
        completion,native,independent = scientific.read('completion.json'),scientific.read('native-receipt.json'),scientific.read('independent-audit.json')
        require(completion['status']=='Complete' and native['status']=='Verified' and native['fights']==27648
            and independent['status']=='Passed' and independent['newFights']==independent['newValues']==0
            and independent['result']==result
            and completion['requestFileHash']==native['requestFileHash']==independent['requestFileHash'],
            'Missing completed audit agreement')
        reconstructed = reviewer.review(result,json.loads(raw),catalogue)
        require(all(retained[k]==v for k,v in reconstructed.items()),'Changed recognition review arithmetic')
        sources = [(evidence['generation'].read(cat['sourcePlan']),evidence['generation'].read(cat['sourceReport'])) for cat in catalogue['roots']]
        affinity_report = evidence['admission'].read('preview-batches.json')['damageSourceAffinities']
        report = diagnose(catalogue,reconstructed,sources,affinity_report)
        captured_mechanics = mechanics(report,sources[0][0]['damageAffinityInventory'])
        handoff = ROOT/HANDOFF
        old_manifest = (handoff/'files.json').read_bytes()
        require(sha(old_manifest)==HANDOFF_PIN,'Changed previous handoff')
        old_raw = (handoff/'closeout.json').read_bytes()
        require(sha(old_raw)==json.loads(old_manifest)[HANDOFF+'/closeout.json'],'Changed previous accounting')
        previous = json.loads(old_raw)
        report.update(sources={key:dict(root=str(e.root),consumed=e.consumed) for key,e in evidence.items()},
            planSha256=PLAN_PIN,previousHandoffManifestSha256=HANDOFF_PIN,
            reviewChargedSeconds=SECONDS,reviewChargedBytes=BYTES,
            cumulativeRecordedSeconds=previous['cumulativeRecordedSeconds']+SECONDS,
            cumulativeRecordedBytes=previous['cumulativeRecordedBytes']+BYTES,
            cumulativeDeclaredMaximumSeconds=previous['cumulativeDeclaredMaximumSeconds']+SECONDS,
            cumulativeDeclaredMaximumBytes=previous['cumulativeDeclaredMaximumBytes']+BYTES,
            lastVerifiedHistory=previous['historicalReservations'],liveHistoryRescanned=False,
            authentication='EveryConsumedFileAgainstExternalManifestPins;PriorFullBattleAuditsReused;NoNativeExecution')
        for e in evidence.values(): e.recheck()
        require(sha((ROOT/PLAN).read_bytes())==PLAN_PIN and sha((handoff/'closeout.json').read_bytes())==sha(old_raw),
            'Inputs changed during review')
        report['secondsBeforeSealing'] = time.monotonic()-started
        save('diagnosis.json',report); save('mechanics.json',captured_mechanics)
        (out/'diagnosis.md').write_text(markdown(report),encoding='utf-8',newline='\n')
        (out/'recognition-review.py').write_bytes(producer)
        save('files.json',{p.name:sha(p.read_bytes()) for p in sorted(out.iterdir())})
        require(time.monotonic()-started<SECONDS and sum(p.stat().st_size for p in out.iterdir())<BYTES,'Review allowance exceeded')
        print(json.dumps(dict(status='ReviewedEditsWithoutNewCombat',manifestSha256=sha((out/'files.json').read_bytes()),
            seconds=time.monotonic()-started,retainedBytes=sum(p.stat().st_size for p in out.iterdir()),summary=report['summary'])))
    except BaseException as e:
        save('failure.json',dict(reason=str(e),seconds=time.monotonic()-started)); raise
    finally:
        timer.cancel()


if __name__=='__main__':
    main()
