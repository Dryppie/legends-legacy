"""Bounded post-hoc diagnosis of paired v3/v4 edits; no combat or policy fitting."""
import argparse
from collections import Counter
import importlib.util
from itertools import combinations
import json
import math
import os
from pathlib import Path
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-affinity-preservation-edit-diagnosis-v1'
SECONDS, BYTES = 180, 64*1048576
ARMS = ('control','candidate')
PACKAGES = {
    'legacy': ('TestResults/affinity-creation-edit-diagnosis-20260924','1a249db3ff2e9274e36773e4e01edd6e035cb2136a0adff6ad054e629376bf45'),
    'recognition': ('TestResults/affinity-preservation-recognition-review-20260924-v2','498669c35f09c67f8bb9ef1d7e202b966bc83663deee1df1b6875ae3170ed4b6'),
    'scientific': ('TestResults/balance/tower-affinity-preservation-recognition-20260924','dcea52f2ed251c04d3985679cfb31e71285814fa6e97199fc35379f4e82a6c39'),
    'catalogue': ('TestResults/affinity-preservation-recognition-catalogue-20260924','c9b448243b4cd2edb530678740c733e485257550de7810db6fb5296420330529'),
    'generation': ('TestResults/balance/tower-affinity-preservation-pilot-01-20260924','1c4fb870912fca7b168596cc86645cb54e96361ba8099eac1da5b4b8748d49e4'),
    'admission': ('TestResults/affinity-preservation-admission-20260924','607b32435968ca9ea96e35234b5246aa0a359ef9b178521d7719b2b57d6b1109'),
    'runtime': ('TestResults/affinity-preservation-recognition-admission-20260924','f96631cc46058b8730246304106beb067957f7480cc5bc6bb4270e339579f622'),
    'handoff': ('TestResults/affinity-preservation-recognition-execution-20260924','28af4f0229b4ee83540808e5fa0a147be15305c7054e67d721d7c3042629a5c2')}
PLAN = 'Balance Harness/Tower-Affinity-Preservation-Recognition-Plan.json'
PLAN_PIN = '8456bb84f4f061eba78d35371602ec8a49ac81e5e60a550552c02bda3d3fddf6'
LIMITATIONS = ('Post-hoc development diagnosis of twelve frozen paired pools. No causal Essence, equipment or owner effects, '
    'fitted policy scores, group confidence intervals or qualification claims. Group totals use known inclusion probabilities; '
    'group labels describe complete metadata and may overlap. Shared cells have one physical measurement and two arm occurrences. '
    'Unknown outcomes remain null, including repeated recipes measured at another root. Authored signals and provider counts '
    'describe structural presence, not realized uptime, damage, effective role, marginal utility or successful control. '
    'Historical combat scores are not extracted or pooled. No new fights, entropy or seed reservations.')


def module(name,path):
    spec = importlib.util.spec_from_file_location(name,path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


# Reuse pure identity, edit and mechanics helpers from the sealed earlier diagnosis.
import hashlib
def sha(raw):
    return hashlib.sha256(raw).hexdigest()

legacy_path,legacy_pin = PACKAGES['legacy']
legacy_root = ROOT/legacy_path
manifest = (legacy_root/'files.json').read_bytes()
if sha(manifest) != legacy_pin or sha((legacy_root/'analysis.py').read_bytes()) != json.loads(manifest)['analysis.py']:
    raise ValueError('Changed sealed edit helpers')
base = module('sealed_creation_edit_helpers',legacy_root/'analysis.py')
require,digest,Evidence,BENCHMARK = base.require,base.digest,base.Evidence,base.BENCHMARK


def protection(proposal,parent,affinities,allowed,preserving):
    owner = str(proposal['changedOwners'][0])
    old = set(parent['builds'][owner])
    step = proposal['affinityCreation']
    pairs = {digest(sorted((a['producerEssenceId'],a['modifierEssenceId']))):
        {a['producerEssenceId'],a['modifierEssenceId']} for a in affinities.values()}
    eligible = {}
    for pid,pair in pairs.items():
        added = pair-old
        possible = old|added
        protect = {v for endpoints in pairs.values() if endpoints<=possible for v in endpoints if v in old}
        removable = old-pair-(protect if preserving else set())
        edits = [list(x) for x in combinations(sorted(removable),len(added)) if added
            and len({allowed[e].casefold() for e in (old-set(x))|added}) == 5]
        if edits:
            eligible[pid] = (protect,edits)
    require(step['pairId'] in eligible,'Target pair has no legal edit')
    protect,edits = eligible[step['pairId']]
    require(step['removed'] in edits,'Unlisted minimal removal')
    if preserving:
        require(step.get('removalSelection') == dict(rule='preserve-completable-affinity-endpoints-v1',
            protectedEssences=sorted(protect),eligiblePairs=len(eligible),eligibleEdits=len(edits)), 'Changed preservation metadata')
    else:
        require('removalSelection' not in step,'Unexpected original-policy removal rule')
    return dict(protectedIfPreserving=sorted(protect),removedCompletableEndpoints=sorted(protect&set(step['removed'])),
        removalSelection=step.get('removalSelection'),eligiblePairs=len(eligible),eligibleEdits=len(edits))


def source_arm(cat,plan,pair,arm,affinity_report):
    report = pair[arm]
    version = 'tower-proposal-racing-v5' if arm=='control' else 'tower-proposal-racing-v6'
    policy_version = 'tower-proposal-policy-v3' if arm=='control' else 'tower-proposal-policy-v4'
    require(pair['status']=='Complete' and plan['version']==report['version']==version
        and plan['selectionPolicyVersion']==report['selectionPolicyVersion']=='tower-racing-benchmark-validation-v1'
        and cat['sourcePlanHashes'][arm]==report['planHash']==digest(plan)
        and report['policyHash']==digest(plan['policy']) and plan['policy']['version']==policy_version,
        'Changed paired source contract')
    scope,e = plan['racing']['scope'],report['evaluation']
    require(plan['policy']['parentTickets']==['benchmark'] and not plan['policy']['preserveParentInteractions']
        and plan['policy']['preservedDamageAffinityIds']==[] and scope['ownedCopies'] is None
        and e['status']=='Complete' and e['planHash']==report['planHash']
        and [b['wave'] for b in report['batches']]==[1,2], 'Changed generation scope')
    parents = [s['party'] for s in scope['starts']]
    require([t['partyId'] for t in cat['teams'][:3]]==[p['id'] for p in parents]
        and cat['benchmarkPartyId']==parents[2]['id']==BENCHMARK, 'Changed reference identity')
    anchor = cat['teams'][2]
    require(base.builds(anchor)==parents[2]['builds'],'Changed anchor recipe')
    full = {t['partyId']:t for t in cat['teams']}
    population = {pid:t for pid,t in full.items() if t['provenance'][arm] and t['provenance'][arm]['generated']}
    beam,nominees,challenger = e['decisions'][-1]['beamIds'],e['nominees'],e['validationFreeze']['challengerId']
    require(len(beam)==len(set(beam))==4 and len(nominees)==len(set(nominees))==5 and challenger in nominees,
        'Changed frozen stage identities')
    affinities = base.selected_affinities(affinity_report,plan)
    allowed = {x['id']:x['family'] for x in scope['allowedEssences']}
    accepted,rejections = {},Counter()
    for batch in report['batches']:
        valid = [p for p in batch['proposals'] if p['rejection'] is None]
        require([p['party'] for p in valid]==batch['candidates'] and len(valid)==(9 if batch['wave']==1 else 8),
            'Changed accepted batch')
        rejections.update(p['rejection'] for p in batch['proposals'] if p['rejection'] is not None)
        for position,p in enumerate(valid,1):
            pid = p['party']['id']
            require(pid in population and pid not in accepted,'Missing or duplicate source proposal')
            team = population[pid]
            require(team['scenario']['seeds']==[] and base.physical(team['scenario'])==base.physical(anchor['scenario']),
                'Changed physical context')
            for ids in p['party']['builds'].values():
                require(ids==sorted(set(ids)) and len(ids)==5 and all(x in allowed for x in ids)
                    and len({allowed[x].casefold() for x in ids})==5,'Illegal loadout')
            provenance = dict(generated=True,wave=batch['wave'],acceptedPosition=position,
                finalBeamRank=beam.index(pid)+1 if pid in beam else None,
                nomineeRank=nominees.index(pid)+1 if pid in nominees else None,validationChallenger=pid==challenger)
            require(team['provenance'][arm]==provenance,'Changed both-arm provenance')
            accepted[pid] = dict(base.edit_metadata(p,parents[2],team,affinities),
                **protection(p,parents[2],affinities,allowed,arm=='candidate'),wave=batch['wave'],acceptedPosition=position)
    require(len(accepted)==len(population)==17 and set(accepted)==set(population),'Incomplete arm population')
    return accepted,rejections,affinities


def mechanics_fit(metadata,parent,team,inventory):
    by_id = {e['id']:e for e in inventory['essences']}
    def signals(ids):
        require(set(ids)<=by_id.keys(),'Missing owner mechanics')
        return {s for eid in ids for s in by_id[eid]['signals']}
    owner = str(metadata['owner'])
    old,new = parent['builds'],base.builds(team)
    before,after = signals(old[owner]),signals(new[owner])
    subgroup = [str(i) for i in range(1+(metadata['subgroup']-1)*5,1+metadata['subgroup']*5)]
    a = Counter(s for o in subgroup for s in signals(old[o]))
    b = Counter(s for o in subgroup for s in signals(new[o]))
    equipment = metadata['ownerContext']['equipment']
    return dict(mainHand=next(e['definitionId'] for e in equipment if e['slot']=='MainHand'),
        addedSignals=sorted(signals(metadata['added'])),removedSignals=sorted(signals(metadata['removed'])),
        ownerSignalsLost=sorted(before-after),ownerSignalsGained=sorted(after-before),
        subgroupSignalsLost=sorted(set(a)-set(b)),subgroupSignalsGained=sorted(set(b)-set(a)),
        subgroupProviderChanges={s:dict(before=a[s],after=b[s]) for s in sorted(set(a)|set(b)) if a[s]!=b[s]},
        interpretation='AuthoredPresenceOnlyNoRoleCompatibilityScoreOrMeasuredMarginalValue')


def summarize(rows):
    measured = [r for r in rows if r['independentOutcome'] is not None]
    total = sum(r['independentOutcome']['observedGain']*r['inclusionProbability']['denominator']
        /r['inclusionProbability']['numerator'] for r in measured)
    return dict(populationOccurrences=len(rows),distinctRecipes=len({r['partyId'] for r in rows}),
        measuredOccurrences=len(measured),unknownOccurrences=len(rows)-len(measured),
        observedAbove=sum(r['independentOutcome']['observedGain']>0 for r in measured),
        observedEqual=sum(r['independentOutcome']['observedGain']==0 for r in measured),
        observedBelow=sum(r['independentOutcome']['observedGain']<0 for r in measured),
        weightedContributionToArmMean=total/204 if measured else None,
        estimatedGroupMean=total/len(rows) if measured else None,
        interpretation='KnownPopulationDenominatorAndInclusionWeights;PostHocNoGroupConfidenceInterval')


def diagnose(catalogue,plan,recognition,sources,affinity_report):
    require(recognition['version']=='tower-affinity-preservation-recognition-review-v2'
        and recognition['diagnosticDecision']=='CompleteDiagnosticOnly' and recognition['newFights']==recognition['newValues']==0,
        'Changed recognition interpretation')
    measured = {(r['root'],r['candidateId']):r for r in recognition['measured']}
    require(len(measured)==len(recognition['measured'])==109 and recognition['unmeasuredCount']==176,'Changed measured family')
    for c in measured.values():
        require(c['referenceId']==BENCHMARK and all(type(c[k]) in (int,float) and math.isfinite(c[k]) for k in ('observedGain','lower','upper'))
            and -1<=c['lower']<=c['observedGain']<=c['upper']<=1,'Invalid benchmark contrast')
    require([c['root'] for c in catalogue['roots']]==[p['root'] for p in plan['roots']]==list(range(1,13))
        and len(sources)==12,'Changed root family')
    cells,rows,rejections,inventory = [],{a:[] for a in ARMS},{a:Counter() for a in ARMS},None
    for cat,p,(plans,pair) in zip(catalogue['roots'],plan['roots'],sources,strict=True):
        require(plans['control']['racing']==plans['candidate']['racing'],'Changed matched racing scope')
        require(inventory is None or inventory==plans['control']['damageAffinityInventory'], 'Changed captured inventory')
        inventory = plans['control']['damageAffinityInventory']
        require(inventory==plans['candidate']['damageAffinityInventory'],'Changed arm inventory')
        selected = {t['partyId']:t for t in p['teams']}
        unknown = {t['partyId']:t for t in p['unmeasured']}
        require(not set(selected)&set(unknown) and set(selected)|set(unknown)=={t['partyId'] for t in cat['teams']},
            'Changed physical partition')
        accepted = {}
        for arm in ARMS:
            accepted[arm],rejected,_ = source_arm(cat,plans[arm],pair,arm,affinity_report)
            rejections[arm].update(rejected)
        for team in cat['teams'][3:]:
            root,pid = cat['root'],team['partyId']
            source = (selected|unknown)[pid]
            outcome = measured.get((root,pid))
            for k in ('stratum','membership','provenance'):
                require(source[k]==team[k],'Changed catalogue/plan metadata')
            require((pid in selected)==(outcome is not None),'Missing or invented root measurement')
            if outcome is None:
                require(source['independentOutcome'] is None,'Imputed unknown outcome')
            else:
                require(all(outcome[k]==team[k] for k in ('stratum','membership','provenance')),'Changed measured provenance')
            probability = source['inclusionProbability']
            require(type(probability['numerator']) is type(probability['denominator']) is int
                and 0<probability['numerator']<=probability['denominator'],'Invalid inclusion probability')
            expected = 1 if team['stratum']=='mandatory' else p['strata'][team['stratum']]['sampleCount']/p['strata'][team['stratum']]['populationCount']
            require(math.isclose(probability['numerator']/probability['denominator'],expected,abs_tol=1e-15),'Changed sampling weight')
            edits = {}
            for arm in ARMS:
                edit = accepted[arm].get(pid)
                require((edit is not None)==(team['provenance'][arm] is not None),'Changed arm membership')
                if edit is not None:
                    parent = plans[arm]['racing']['scope']['starts'][2]['party']
                    edit = dict(edit,mechanicsFit=mechanics_fit(edit,parent,team,inventory))
                    rows[arm].append(dict(root=root,partyId=pid,membership=team['membership'],stratum=team['stratum'],
                        inclusionProbability=probability,provenance=team['provenance'][arm],**edit,independentOutcome=outcome))
                edits[arm] = edit
            if all(edits.values()):
                require(all(edits['control'][k]==edits['candidate'][k] for k in ('owner','added','removed','ownerContext','mechanicsFit')),
                    'Changed shared physical edit')
            cells.append(dict(root=root,partyId=pid,membership=team['membership'],stratum=team['stratum'],
                inclusionProbability=probability,provenance=team['provenance'],edits=edits,independentOutcome=outcome))
    require(len(cells)==len({(c['root'],c['partyId']) for c in cells})==285
        and sum(c['independentOutcome'] is None for c in cells)==176
        and set(measured)=={(c['root'],c['partyId']) for c in cells if c['independentOutcome'] is not None},'Incomplete or cross-root join')
    groups,summary = {},{}
    dimensions = dict(membership=lambda r:[r['membership']],owner=lambda r:[str(r['owner'])],
        subgroup=lambda r:[str(r['subgroup'])],replacementDistance=lambda r:[str(r['replacementDistance'])],
        mainHand=lambda r:[r['mechanicsFit']['mainHand']],removedEssence=lambda r:r['removed'],
        addedSet=lambda r:[' + '.join(r['added'])],removedCompletableEndpoint=lambda r:[str(bool(r['removedCompletableEndpoints']))],
        lostOwnerSignal=lambda r:r['mechanicsFit']['ownerSignalsLost'],lostSubgroupSignal=lambda r:r['mechanicsFit']['subgroupSignalsLost'])
    for arm in ARMS:
        require(len(rows[arm])==204,'Incomplete arm occurrences')
        summary[arm] = summarize(rows[arm])
        require(math.isclose(summary[arm]['weightedContributionToArmMean'],recognition['allRoots'][arm]['mean'],abs_tol=1e-12),
            'Weighted join fails pool reconciliation')
        groups[arm] = {}
        for name,labels in dimensions.items():
            members = {}
            for row in rows[arm]:
                for label in labels(row):members.setdefault(label,[]).append(row)
            groups[arm][name] = {label:summarize(items) for label,items in sorted(members.items())}
    return dict(version=VERSION,interpretation='PostHocPairedEditDiagnosisNoCausalAttributionOrPolicyFitting',newFights=0,newValues=0,
        sourceDiagnosticDecision=recognition['diagnosticDecision'],policyDefaultsChanged=False,benchmarkPartyId=BENCHMARK,
        physicalCells=285,physicalMeasured=109,physicalUnknown=176,armOccurrences=408,summary=summary,groups=groups,cells=cells,
        armRows=rows,rejectedAttempts={a:dict(sorted(c.items())) for a,c in rejections.items()},limitations=LIMITATIONS)


def markdown(report):
    lines = ['# Paired affinity-preservation edit diagnosis','',report['limitations'],'',
        '285 generated physical cells; 408 arm occurrences; 109 measured physical cells; 176 unknown physical outcomes.','']
    for dimension in ('membership','replacementDistance','owner','mainHand','removedEssence','removedCompletableEndpoint'):
        lines += [f'## {dimension}','', '| Arm / group | Population | Measured | Unknown | Above / equal / below | Weighted contribution to arm mean |',
            '| --- | ---: | ---: | ---: | ---: | ---: |']
        for arm in ARMS:
            for label,g in report['groups'][arm][dimension].items():
                value = g['weightedContributionToArmMean']
                text = 'Unknown' if value is None else f'{100*value:+.3f} pp'
                lines.append(f"| {arm} / {label} | {g['populationOccurrences']} | {g['measuredOccurrences']} | {g['unknownOccurrences']} | {g['observedAbove']} / {g['observedEqual']} / {g['observedBelow']} | {text} |")
        lines.append('')
    lines += ['Contributions divide inverse-inclusion-weighted gains by 204 arm occurrences. They are post-hoc descriptive decompositions of the fixed pool estimate; no group intervals or causal effects are inferred. Removal and signal groups overlap.','',
        '[Every joined cell, arm occurrence and structural signal change](diagnosis.json). [Captured mechanics](mechanics.json).','']
    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,required=True)
    out = parser.parse_args().output.resolve()
    require(not out.exists() and out.is_relative_to(ROOT/'TestResults')
        and all(not out.is_relative_to(ROOT/path) for path,_ in PACKAGES.values()),'Existing or unsafe output')
    out.mkdir()
    started = time.monotonic()
    timer = threading.Timer(SECONDS,lambda:os._exit(124));timer.daemon=True;timer.start()
    def save(name,value):
        with (out/name).open('x',encoding='utf-8',newline='\n') as f:json.dump(value,f,indent=2,allow_nan=False);f.write('\n')
    try:
        save('declaration.json',dict(version=VERSION,maximumSeconds=SECONDS,maximumBytes=BYTES,
            charge='FullAllowanceAtStartIncludingFailure',newFights=0,newValues=0,sourceManifestPins=PACKAGES,scope=LIMITATIONS))
        (out/'analysis.py').write_bytes(Path(__file__).read_bytes())
        evidence = {k:Evidence(*v) for k,v in PACKAGES.items()}
        recognition,scientific = evidence['recognition'],evidence['scientific']
        recognition.bytes('analysis.py');reviewer = module('paired_diagnosis_recognition',recognition.root/'analysis.py')
        evidence['runtime'].bytes('auditor.py');auditor = reviewer.load_auditor(evidence['runtime'].root/'auditor.py')
        raw = (ROOT/PLAN).read_bytes();require(sha(raw)==PLAN_PIN,'Changed frozen plan');plan = json.loads(raw)
        catalogue,retained = evidence['catalogue'].read('catalogue.json'),recognition.read('review.json')
        result,independent = scientific.read('result.json'),scientific.read('independent-audit.json')
        native,completion = scientific.read('native-receipt.json'),scientific.read('completion.json')
        require(native['status']=='Verified' and native['fights']==37120 and completion['status']=='Complete'
            and independent['status']=='Passed' and independent['result']==result
            and independent['newFights']==independent['newValues']==0
            and completion['requestFileHash']==native['requestFileHash']==independent['requestFileHash'],'Missing audit agreement')
        study = scientific.read('study/study.json');scientific.bytes('study/files.json')
        endpoint = reviewer.published_endpoint(result,study,scientific.consumed['study/files.json'],auditor)
        reconstructed = reviewer.review(endpoint,plan,catalogue,study['evidence'],scientific.read('confirmation-binding.json')['panel'],auditor)
        require(all(retained[k]==v for k,v in reconstructed.items()),'Changed recognition review')
        sources = [({a:evidence['generation'].read(c['sourcePlans'][a]) for a in ARMS},
            evidence['generation'].read(c['sourceReport'])) for c in catalogue['roots']]
        affinity = evidence['admission'].read('preview-batches.json')['damageSourceAffinities']
        report = diagnose(catalogue,plan,reconstructed,sources,affinity)
        mechanics = base.mechanics(dict(rows=report['armRows']['control']+report['armRows']['candidate']),sources[0][0]['control']['damageAffinityInventory'])
        previous = evidence['handoff'].read('closeout.json')
        report.update(sources={k:dict(root=str(e.root),consumed=e.consumed) for k,e in evidence.items()},planSha256=PLAN_PIN,
            chargedSeconds=SECONDS,chargedBytes=BYTES,cumulativeRecordedSeconds=previous['cumulativeRecordedSeconds']+SECONDS,
            cumulativeRecordedBytes=previous['cumulativeRecordedBytes']+BYTES,
            cumulativeDeclaredMaximumSeconds=previous['cumulativeDeclaredMaximumSeconds']+SECONDS,
            cumulativeDeclaredMaximumBytes=previous['cumulativeDeclaredMaximumBytes']+BYTES,
            lastVerifiedHistory=previous['historicalReservations'],liveHistoryRescanned=False,
            authentication='ConsumedFilesPinned;RecognitionReconstructed;PriorFullBattleAuditsReused;NoNativeExecution')
        for e in evidence.values():e.recheck()
        require(sha((ROOT/PLAN).read_bytes())==PLAN_PIN,'Plan changed during diagnosis')
        report['secondsBeforeSealing'] = time.monotonic()-started
        save('diagnosis.json',report);save('mechanics.json',mechanics)
        (out/'diagnosis.md').write_text(markdown(report),encoding='utf-8',newline='\n')
        save('files.json',{p.name:sha(p.read_bytes()) for p in sorted(out.iterdir())})
        require(time.monotonic()-started<SECONDS and sum(p.stat().st_size for p in out.iterdir())<BYTES,'Diagnosis allowance exhausted')
        print(json.dumps(dict(status='PairedEditsDiagnosedWithoutNewCombat',manifestSha256=sha((out/'files.json').read_bytes()),
            seconds=time.monotonic()-started,retainedBytes=sum(p.stat().st_size for p in out.iterdir()),summary=report['summary'])))
    except BaseException as e:
        save('failure.json',dict(reason=str(e),seconds=time.monotonic()-started));raise
    finally:
        timer.cancel()


if __name__=='__main__':
    main()
