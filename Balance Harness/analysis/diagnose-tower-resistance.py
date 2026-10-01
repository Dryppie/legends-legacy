"""Explain the fixed floor-7 resistance comparison using 96 exact saved replays.

No gameplay mutation, acceptance test, fresh seeds, reordered builds or retries.
"""
import argparse
from collections import defaultdict
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('resistance_mixed',Path(__file__).with_name('diagnose-tower-mixed-armor.py'))
mixed=importlib.util.module_from_spec(spec);spec.loader.exec_module(mixed);io=mixed.io
PLAN_PIN='12eb8d799f0de523d9e371ff2dbbbdbf148f3214473a1b7b190bb8f074bdc989'
SOURCE_PIN='1df97981c1cb0ee295286d63692a3ed1817cd928b644b5002e54666bb3469b31'
SPRINGTIDE='effect.creature.eydis.springtide.damage'
ABUNDANCE='status.eydis.abundance'
WINDOWS=(('before40',0,40),('after40',40,float('inf')),('all',0,float('inf')))


def validate_selection(plan,cells,seeds):
    io.check(plan['version']=='tower-floor7-pressure-diagnostic-proposal-v1' and plan['status']=='ProposedNotAllocated' and
        plan['maximumHistoricalReplays']==96 and plan['newStudyFights']==plan['newSeeds']==plan['allocatedReplays']==0 and
        plan['firstWindowSeconds']==40 and plan['requireCompleteSavedReportParityAfterRemovingEventLog'] and
        not plan['gameplayCandidateSelected'],'Expected unallocated descriptive floor-7 plan')
    io.check(len(cells)==plan['familySize']==120 and all(c['scenario']['floorNumber']==7 for c in cells) and
        len({io.composition_key(c) for c in cells})==plan['actualCompositions']==8,'Complete original family required')
    io.check(len(seeds)==len(set(seeds))==128 and plan['seeds']==seeds[:16],'First sixteen declared seeds required')
    lookup={c['id']:c for c in cells};io.check(len(lookup)==120,'Duplicate saved identity')
    chosen=[];compositions=set()
    io.check(len(plan['cases'])==2,'Two paired compositions required')
    for label,group in zip(('A','B'),plan['cases'],strict=True):
        cases=group['cases'];io.check([c['specializedCharacters'] for c in cases]==[0,2,5],'Exact three equipment profiles required')
        base=lookup[cases[0]['id']];full=lookup[cases[-1]['id']]
        io.check(base['gear']=='baseline' and full['gear']=='resistance-and-health' and
            [m['partySlot'] for m in base['scenario']['party']]==list(range(1,6)),'Original five-character endpoints required')
        compositions.add(io.composition_key(base))
        for profile,case in zip(('baseline','two-resistance','full-resistance'),cases,strict=True):
            cell=lookup[case['id']];raw=copy.deepcopy(cell['scenario']);selected=case['resistancePartySlots']
            specialized=[m['partySlot'] for m in raw['party'] for item in m['build']['equipment'] if '.spec.' in item['definitionId']]
            io.check(cell['composition']==group['composition'] and cell['gear']==case['gear'] and not raw['seeds'] and
                len(specialized)==4*case['specializedCharacters'] and sorted(set(specialized))==selected and
                len(selected)==case['specializedCharacters'],'Exact saved equipment profile required')
            for member,plain,resistant in zip(raw['party'],base['scenario']['party'],full['scenario']['party'],strict=True):
                expected=resistant if member['partySlot'] in selected else plain
                io.check(member['build']['equipment']==expected['build']['equipment'],'Undeclared equipment substitution')
                member['build']['equipment']=copy.deepcopy(plain['build']['equipment'])
            io.check(raw==base['scenario'],'Raw identity, Essence order, position or budget changed')
            chosen.append(dict(label=label+'/'+profile,composition=group['composition'],profile=profile,cellId=cell['id'],resistancePartySlots=selected))
    io.check(len(compositions)==2 and len({c['cellId'] for c in chosen})==6,'Two actual compositions and six exact recipes required')
    return chosen


def guardian_totals(replay):
    b=replay['battle'];hostile=[p for p in b['preparedParticipants'] if p['slot']['side']=='Hostile']
    io.check(len(hostile)==1,'Exactly one guardian required');guardian=hostile[0]['slot']['slotId']
    s=next(s for s in b['summary']['statistics'] if s['entityId']==guardian)
    events=[e for e in b['eventLog'] if e['targetId']==guardian]
    for field in ('incomingRawDamage','physicalMitigationPrevented','magicalMitigationPrevented','finalHealthDamage'):
        io.check(sum(e[field] for e in events)==s[field],'Guardian damage totals differ')
    io.check(sum(e['magnitude'] for e in events if e['eventType'] in mixed.base.HEALING)==s['healingReceived'] and
        sum(e['magnitude'] for e in events if e['eventType']=='HealthRegeneration')==s['healthRegenerated'],'Guardian healing totals differ')
    return guardian,s,events


def timeline(replay,scenario):
    b=replay['battle'];events=b['eventLog'];tps=b['ticksPerSecond'];actors=mixed.party(replay,scenario)
    slots={p['slot']['slotId']:slot for slot,p,_ in actors};incoming=[e for e in events if e['targetId'] in slots]
    guardian,stats,outgoing=guardian_totals(replay)
    windows={}
    for name,start,end in WINDOWS:
        select=lambda es:[e for e in es if start*tps<=e['timestamp']<end*tps]
        inc,out=select(incoming),select(outgoing)
        windows[name]=dict(party=mixed.window(inc),guardian=mixed.window(out),
            outgoingByOriginalSlot={str(slot):sum(e['finalHealthDamage'] for e in out if e['actorId']==actor) for actor,slot in slots.items()},
            otherActorDamage=sum(e['finalHealthDamage'] for e in out if e['actorId'] not in slots),
            exposureSeconds=max(0,min(b['summary']['durationSeconds'],end)-min(b['summary']['durationSeconds'],start)))
    # Applications are observable; stack modifications are not universally logged.
    # Do not present their cumulative magnitude as an exact native stack snapshot.
    applications=0;springtides=[];abundance=[];conditions=[];health=stats['maxHealth'];trajectory=[]
    for index,event in enumerate(events):
        e=event
        if e['targetId']==guardian and e['source']==ABUNDANCE:
            if e['eventType']=='StatusEffect':applications+=e['magnitude']
            abundance.append(dict(index=index,tick=e['timestamp'],eventType=e['eventType'],magnitude=e['magnitude'],details=e['details']))
        if e['source']==SPRINGTIDE and e['targetId'] in slots:
            springtides.append(dict(index=index,tick=e['timestamp'],slot=slots[e['targetId']],loggedAbundanceApplicationsBeforeHit=applications,
                exactAbundanceStacks=None,**{k:e[k] for k in mixed.MEASURES}))
        if e['targetId'] in slots and ('condition.slow' in (e['source'] or '').lower() or 'condition.weaken' in (e['source'] or '').lower() or
            'tranquil_waters' in (e['source'] or '').lower()):
            conditions.append(dict(index=index,tick=e['timestamp'],slot=slots[e['targetId']],source=e['source'],eventType=e['eventType'],details=e['details'],magnitude=e['magnitude']))
        if e['targetId']==guardian:
            delta=-e['finalHealthDamage']+(e['magnitude'] if e['eventType'] in mixed.base.HEALING or e['eventType']=='HealthRegeneration' else 0)
            if delta:
                health+=delta
                trajectory.append(dict(index=index,tick=e['timestamp'],source=e['source'],eventType=e['eventType'],delta=delta,accountedHealth=health))
    recipients=[]
    for slot,p,s in actors:
        es=[(i,e) for i,e in enumerate(events) if e['targetId']==s['entityId']]
        death=next(((i,e) for i,e in es if e['eventType']=='Death'),None)
        prior=[e for i,e in es if death is None or i<death[0]]
        last=next((e for e in reversed(prior) if e['finalHealthDamage']>0),None)
        recipients.append(dict(slot=slot,name=p['name'],firstDeathTick=s['firstDeathTick'],beforeFirstDeath=mixed.window(prior),
            lastDamageBeforeFirstDeath=last if death else None,
            before40=mixed.window([e for _,e in es if e['timestamp']<40*tps])))
    return dict(windows=windows,recipients=recipients,springtideHits=springtides,abundanceEvents=abundance,conditionEvents=conditions,
        guardianHealthTrajectory=trajectory,guardianFinalAccountedHealth=health,guardianReportedHealth=stats['health'],
        guardianHealthAccountingResidual=health-stats['health'],healthTrajectoryMeaning='Initial reported integer maximum plus logged actual healing/regeneration minus logged health damage; not a native health snapshot')


def analyze(saved,replay,scenario):
    return dict(**mixed.analyze(saved,replay,scenario),resistance=timeline(replay,scenario))


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('plan','entry','artifacts','protocol','output'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args();output=args.output.resolve();plan=io.read(args.plan);entry=io.read(args.entry)
    io.check(io.sha(args.plan)==PLAN_PIN and plan['sourceManifestSha256']==SOURCE_PIN,'Unrecognized frozen diagnostic plan')
    io.check(output.parent==ROOT/'TestResults' and not output.exists(),'Fresh direct TestResults output required')
    for group in ('authenticatedPins','liveCatalogPins','runtimePins','ledgerPins'):
        for path,pin in entry[group].items():io.check(io.sha(path)==pin,'Entry binding changed')
    source=Path(plan['source']);io.check(io.sha(source/'files.json')==SOURCE_PIN,'Source changed');io.authenticate(source)
    cells=io.read(source/'cells.json');family=io.read(ROOT/'TestResults/tower-floor7-mixed-resistance-proposed-family-20260930.json')
    io.check(cells==io.qualification_module().validate_family(family,cells[:60]),'Whole 120-recipe family changed')
    seeds=io.read(source/'request.json')['seeds'];chosen=validate_selection(plan,cells,seeds);scope=io.read(source/'scope.json')
    runtime=args.artifacts.resolve()/'bin/EssenceSystem.Tests/release';live=ROOT/'LL/src/API/API.LL/Data'
    for name,pin in scope['execution']['assemblyHashes'].items():io.check(io.sha(runtime/(name+'.dll'))==pin,'Historical runtime changed')
    for name,pin in scope['contentHashes'].items():io.check(io.sha(live/name)==pin,'Current catalog differs from saved source')
    history,ledgers=io.history();io.check(len(history)==entry['initialExclusions']==919804,'Unexpected reservations')
    archive=source/'evaluation';lookup={c['id']:c for c in cells};saved={c['cellId']:{} for c in chosen};trials={}
    rows={r['id']:r for r in io.read(source/'result.json')['rows']}
    for line in (archive/'trials.jsonl').read_text(encoding='utf-8').splitlines():
        t=json.loads(line)
        if t['stage'] not in saved:continue
        io.check(io.read(archive/'recipes'/(t['recipe']+'.json'))==dict(lookup[t['stage']]['scenario'],seeds=seeds),'Raw recipe changed')
        report=json.loads(gzip.decompress((archive/'battles'/(t['id']+'.json.gz')).read_bytes()))
        io.check(t['seed'] not in saved[t['stage']] and report['battle']['seed']==t['seed'],'Repeated or wrong seed')
        saved[t['stage']][t['seed']]=report;trials[t['stage'],t['seed']]=t
    for key,reports in saved.items():io.check(set(reports)==set(seeds) and sum(r['succeeded'] for r in reports.values())==rows[key]['wins'],'Incomplete historical panel')
    selected=[dict(**c,trial=trials[c['cellId'],seed]) for seed in plan['seeds'] for c in chosen]
    resource_path=ROOT/'TestResults/tower-floor4-recovery-diagnostic-evidence-20260930.json';reference=io.read(resource_path)
    resource=dict(source=str(resource_path),sha256=io.sha(resource_path),referenceReplays=80,plannedReplays=96,
        projectedSeconds=2*reference['summedProcessSeconds']*96/80,projectedBytes=2*reference['archiveBytes']*96/80,
        admittedSeconds=.8*1200,admittedBytes=.8*2*1024**3)
    io.check(resource['projectedSeconds']<resource['admittedSeconds'] and resource['projectedBytes']<resource['admittedBytes'],'Measured resource admission failed')
    paths=[Path(__file__),Path(mixed.__file__),Path(mixed.base.__file__),Path(io.__file__),args.plan,args.entry,args.protocol,
        ROOT/'build/bounded_windows_process.py',Path(__file__).with_name('test-tower-resistance-diagnostic.py'),resource_path]
    paths.extend((ROOT/'TestResults/tower-floor7-pressure-diagnostic-tests-20260930').iterdir())
    pins={str(p.resolve()):io.sha(p) for p in paths}
    output.mkdir();shutil.copy2(args.protocol,output/'protocol.md');shutil.copy2(__file__,output/'owner-source.py')
    io.write(output/'declaration.json',dict(source=str(source),sourceManifestSha256=SOURCE_PIN,selected=selected,chosen=chosen,sourcePins=pins,
        runtimePins=entry['runtimePins'],liveCatalogPins=entry['liveCatalogPins'],ledgerPins=ledgers,initialExclusions=len(history),
        maximumReplays=96,maximumSeconds=1200,maximumReplaySeconds=60,maximumLogBytes=64*1024**2,maximumBytes=2*1024**3,newSeeds=0,usedForAcceptance=False))
    io.write(output/'resource-admission.json',resource)
    io.write(output/'saved-summary.json',{c['label']:mixed.saved_summary(list(saved[c['cellId']].values()),lookup[c['cellId']]['scenario']) for c in chosen})
    process=mixed.base.module('resistance_process',ROOT/'build/bounded_windows_process.py');deadline=time.monotonic()+1200
    attempts=0;details=[];status='Failed';print('Admitted 96 exact historical replays; 768 selected-recipe outcomes recounted.',flush=True)
    try:
        for item in selected:
            t=item['trial'];log=output/(t['id']+'.log')
            command=[shutil.which('dotnet'),str(runtime/'BalanceHarness.dll'),'tower-loadout-replay','--run',str(archive),'--battle',t['id'],'--detailed']
            attempts+=1;io.write(output/(t['id']+'-attempt.json'),dict(attempt=attempts,command=command))
            receipt=process.run(command,ROOT,log,min(deadline,time.monotonic()+60),log_byte_limit=64*1024**2,
                observe=lambda observation:io.write(output/(t['id']+'-observation.json'),observation))
            io.write(output/(t['id']+'-process.json'),receipt)
            io.check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Replay failed; stop without retry')
            raw=log.read_text(encoding='utf-8-sig').lstrip();replay,end=json.JSONDecoder().raw_decode(raw)
            io.check(raw[end:].strip()=='Replay matched the saved input, combat result and Tower outcome.','Unexpected native output')
            details.append(dict(label=item['label'],cellId=item['cellId'],seed=t['seed'],trial=t['id'],**analyze(saved[item['cellId']][t['seed']],replay,lookup[item['cellId']]['scenario'])))
            if len(details)%6==0:print(f'Completed {len(details)}/96 complete-report matches.',flush=True)
            io.check(sum(p.stat().st_size for p in output.iterdir() if p.is_file())<=2*1024**3,'Diagnostic byte limit exceeded')
        for group in (pins,entry['runtimePins'],entry['liveCatalogPins'],ledgers):
            for path,pin in group.items():io.check(io.sha(path)==pin,'Frozen input changed')
        io.check(io.history()[0]==history,'Diagnostic allocated seeds')
        io.write(output/'details.json',details)
        io.write(output/'result.json',dict(status='VerifiedDescriptiveDiagnostic',recountedSavedFights=768,repeatedFights=96,newSeeds=0,usedForAcceptance=False,finalExclusions=len(history)))
        status='Complete'
    finally:
        io.write(output/'completion.json',dict(status=status,attemptedReplays=attempts,completedReplays=len(details),newSeeds=0,usedForAcceptance=False))
        io.write(output/'files.json',{p.name:io.sha(p) for p in sorted(output.iterdir()) if p.is_file()})


if __name__=='__main__':main()
