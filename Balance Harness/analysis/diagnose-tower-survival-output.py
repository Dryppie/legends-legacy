"""Explain the fixed 21.25% midpoint with 48 exact historical replays."""
import argparse
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT=Path(__file__).resolve().parents[2]
s=importlib.util.spec_from_file_location('survival_casualty',Path(__file__).with_name('diagnose-tower-health-casualties.py'))
casualty=importlib.util.module_from_spec(s);s.loader.exec_module(casualty)
residual=casualty.residual;resistance=casualty.resistance;mixed=casualty.mixed;io=casualty.io
PLAN_PIN='67f492b5fa83fa2d85de13f173ad30c5c3e8918d61be50b1a06edbcc23309e1c'
SOURCE_PIN='375e912a74bca3a32ebe1b3f60e6ea308916dd033b9af0c7f64d023d65750858'
ENDLESS='effect.creature.eydis.endless_spring.heal'
CASES=(('A','ability-haste',5,[1,2,3,4,5]),('A','restorer-specialization',6,[2]),('B','restorer-specialization',6,[2]))


def validate_selection(plan,cells,seeds):
    io.check(plan['version']=='tower-floor7-survival-output-diagnostic-v1' and plan['status']=='ProposedNotAllocated' and
        plan['maximumHistoricalReplays']==48 and plan['newStudyFights']==plan['newSeeds']==plan['allocatedReplays']==0 and
        not plan['gameplayCandidateSelected'],'Expected unallocated descriptive diagnostic')
    io.check(len(cells)==plan['familySize']==120 and all(c['scenario']['floorNumber']==7 for c in cells) and
        len({io.composition_key(c) for c in cells})==plan['actualCompositions']==8,'Complete original family required')
    io.check(len(seeds)==len(set(seeds))==128 and plan['seeds']==seeds[:16],'First sixteen declared seeds required')
    lookup={c['id']:c for c in cells};io.check(len(lookup)==120,'Duplicate saved recipe identity')
    io.check(len(plan['cases'])==3,'Exactly three recipe selections required')
    chosen=[];parents={};compositions={}
    for case,(label,profile,count,slots) in zip(plan['cases'],CASES,strict=True):
        io.check(case['label']==label and case['gear']==profile,'Exact ordered labels and profiles required')
        parent=case['composition'];io.check(parents.get(label,parent)==parent,'Matching parent required');parents[label]=parent
        bases=[c for c in cells if c['composition']==parent and c['gear']=='baseline'];io.check(len(bases)==1,'Unique baseline required')
        base=bases[0];compositions[label]=io.composition_key(base)
        io.check([m['partySlot'] for m in base['scenario']['party']]==list(range(1,6)),'Original party positions required')
        cell=lookup[case['cellId']];raw=copy.deepcopy(cell['scenario'])
        specialized=[m['partySlot'] for m in raw['party'] for item in m['build']['equipment'] if '.spec.' in item['definitionId']]
        io.check(cell['composition']==parent and cell['gear']==profile and not raw['seeds'] and
            len(specialized)==case['specializedItems']==count and sorted(set(specialized))==case['specializedPartySlots']==slots,
            'Exact saved equipment counts and slots required')
        for member,plain in zip(raw['party'],base['scenario']['party'],strict=True):
            if member['partySlot'] not in slots:io.check(member['build']['equipment']==plain['build']['equipment'],'Unselected equipment changed')
            member['build']['equipment']=copy.deepcopy(plain['build']['equipment'])
        io.check(raw==base['scenario'],'Raw identity, Essence order, positions or budget changed')
        chosen.append(dict(label=label+'/'+profile,composition=parent,profile=profile,cellId=cell['id'],specializedPartySlots=slots))
    io.check(len(set(compositions.values()))==2 and len({c['cellId'] for c in chosen})==3,'Two actual compositions and three exact recipes required')
    return chosen


def output_window(events,slots,guardian,summons):
    incoming=[e for e in events if e['targetId'] in slots];outgoing=[e for e in events if e['targetId']==guardian]
    origins={name:{kind:0 for kind in ('condition','direct','other')} for name in ('originalParty','summon','otherActor')}
    for e in outgoing:
        origin='originalParty' if e['actorId'] in slots else 'summon' if e['actorId'] in summons else 'otherActor'
        kind='condition' if (e['source'] or '').startswith('condition.') else 'direct' if e['eventType'] in ('Damage','DamageCrit') else 'other'
        origins[origin][kind]+=e['finalHealthDamage']
    healing=[e for e in outgoing if e['eventType'] in mixed.base.HEALING]
    restoration=sum(e['magnitude'] for e in healing)
    regen=sum(e['magnitude'] for e in outgoing if e['eventType']=='HealthRegeneration')
    return dict(party=mixed.window(incoming),guardian=mixed.window(outgoing),damageByOriginAndKind=origins,
        outgoingByOriginalSlot={str(slot):sum(e['finalHealthDamage'] for e in outgoing if e['actorId']==actor) for actor,slot in slots.items()},
        endlessSpringHealing=sum(e['magnitude'] for e in healing if e['source']==ENDLESS),
        otherHealing=sum(e['magnitude'] for e in healing if e['source']!=ENDLESS),guardianRegeneration=regen,
        guardianNetLoggedHealthChange=restoration+regen-sum(e['finalHealthDamage'] for e in outgoing))


def event_metrics(events,slots,guardian,summons,seconds,tps):
    io.check(tps>0 and seconds>=0,'Positive tick rate and nonnegative duration required')
    io.check(all(a['timestamp']<=b['timestamp'] for a,b in zip(events,events[1:])),'Chronological native events required')
    windows={}
    for name,start,stop in [('before48',0,48),('48to60',48,60),('60to72',60,72),('after72',72,float('inf')),('all',0,float('inf'))]:
        selected=[e for e in events if start*tps<=e['timestamp']<stop*tps]
        exposure=max(0,min(seconds,stop)-min(seconds,start))
        windows[name]=dict(exposureSeconds=exposure,**output_window(selected,slots,guardian,summons))
    # Native snapshots retain event ordering. No missing snapshot or exact stack is inferred.
    heals=[];last=None;notifications=[];deaths=[]
    for i,e in enumerate(events):
        snapshot=e.get('combatEntity');target_snapshot=snapshot if snapshot and snapshot['id']==guardian else None
        if e['targetId']==guardian and e['eventType'] in mixed.base.HEALING:
            io.check(target_snapshot is not None,'Native guardian healing snapshot required')
            after=dict(index=i,tick=e['timestamp'],health=target_snapshot['health'],maxHealth=target_snapshot['maxHealth'])
            heals.append(dict(index=i,tick=e['timestamp'],source=e['source'],actualHealing=e['magnitude'],lastObservedBefore=copy.deepcopy(last),
                after=after,observedHealthChange=None if last is None else after['health']-last['health']))
        if target_snapshot is not None:
            last=dict(index=i,tick=e['timestamp'],health=target_snapshot['health'],maxHealth=target_snapshot['maxHealth'])
        if e['targetId'] in {*slots,guardian} and ((e['source'] or '') in ('condition.slow','condition.weaken',resistance.ABUNDANCE) or
            (e['source'] or '').startswith('effect.creature.eydis.ancient_heartwood.')):
            notifications.append(dict(index=i,tick=e['timestamp'],actorId=e['actorId'],targetId=e['targetId'],source=e['source'],
                eventType=e['eventType'],magnitude=e['magnitude'],details=e['details']))
        if e['targetId'] in slots and e['eventType']=='Death':
            prior=next((j for j in range(i-1,-1,-1) if events[j]['targetId']==e['targetId'] and events[j]['finalHealthDamage']>0),None)
            deaths.append(dict(index=i,tick=e['timestamp'],slot=slots[e['targetId']],precedingDamageIndex=prior,
                precedingDamage=None if prior is None else events[prior]))
    first=deaths[0]['index'] if deaths else None
    cuts={'beforeFirstDeathEvent':(0,len(events) if first is None else first),'fromFirstDeathEvent':(len(events) if first is None else first,len(events))}
    boundary=next((h for h in heals if h['source']==ENDLESS and h['tick']==60*tps),None)
    if boundary is not None:
        at=boundary['index'];cuts.update(before60HealEvent=(0,at),from60HealEvent=(at,len(events)))
    ordered={name:output_window(events[left:right],slots,guardian,summons) for name,(left,right) in cuts.items()}
    around=None if boundary is None else dict(heal=boundary,
        preceding3Seconds=output_window([e for i,e in enumerate(events) if i<boundary['index'] and e['timestamp']>=57*tps],slots,guardian,summons),
        following3Seconds=output_window([e for i,e in enumerate(events) if i>=boundary['index'] and e['timestamp']<63*tps],slots,guardian,summons),
        sameTickEvents=[dict(index=i,targetId=e['targetId'],actorId=e['actorId'],source=e['source'],eventType=e['eventType'],magnitude=e['magnitude'],
            finalHealthDamage=e['finalHealthDamage']) for i,e in enumerate(events) if e['timestamp']==60*tps])
    return dict(windows=windows,eventOrderWindows=ordered,guardianHeals=heals,sixtySecondHeal=around,originalPartyDeaths=deaths,
        firstDeathSlots=sorted({d['slot'] for d in deaths if d['tick']==deaths[0]['tick']}) if deaths else [],notifications=notifications,
        meaning='Half-open time windows; event-index partitions include killing damage before Death and the 60s heal in the following partition. '
            'Healing is actual logged magnitude, not nominal or potential healing. Last observed guardian snapshot is labeled separately from the heal snapshot. '
            'Condition damage uses condition.* source IDs; origin uses original prepared actors and native isSummonedEntity flags. '
            'Post-death output can include ongoing conditions. Notifications do not establish exact stacks or unlogged condition state.')


def guardian_restoration_stats(stats):
    # EntityStats omits these two integer properties when zero (WhenWritingDefault).
    return dict(guardianHealingPotential=stats.get('healingPotential',0),guardianOverhealing=stats.get('overhealing',0),
        guardianRegenerationPotential=stats['healthRegenerationPotential'],guardianRegenerationOverhealed=stats['healthRegenerationOverhealed'])


def analyze(saved,replay,scenario):
    b=replay['battle'];actors=mixed.party(replay,scenario);slots={p['slot']['slotId']:slot for slot,p,_ in actors}
    guardian,stats,_=resistance.guardian_totals(replay)
    summons={s['entityId'] for s in b['summary']['statistics'] if s.get('isSummonedEntity',False)}
    return dict(**casualty.analyze(saved,replay,scenario),survival=event_metrics(b['eventLog'],slots,guardian,summons,b['summary']['durationSeconds'],b['ticksPerSecond']),
        **guardian_restoration_stats(stats))


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('plan','entry','artifacts','protocol','output'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args();output=args.output.resolve();plan=io.read(args.plan);entry=io.read(args.entry)
    io.check(io.sha(args.plan)==PLAN_PIN and plan['sourceManifestSha256']==SOURCE_PIN,'Unrecognized frozen plan')
    io.check(output.parent==ROOT/'TestResults' and not output.exists(),'Fresh direct TestResults output required')
    for group in ('authenticatedPins','liveCatalogPins','runtimePins','ledgerPins'):
        for path,pin in entry[group].items():io.check(io.sha(path)==pin,'Entry binding changed')
    source=Path(plan['source']);io.check(io.sha(source/'files.json')==SOURCE_PIN,'Source changed');io.authenticate(source)
    io.check(io.sha(plan['sourceAudit'])==plan['sourceAuditSha256'] and io.read(plan['sourceAudit'])['status']=='Verified','Source audit changed')
    cells=io.read(source/'cells.json');seeds=io.read(source/'request.json')['seeds'];chosen=validate_selection(plan,cells,seeds)
    # Authenticate the isolated Tower delta from its original source. Replay never reads live candidate content.
    candidate=io.read(ROOT/'TestResults/tower-floor7-health-springtide-midpoint-driver-20260930/candidate-1.json')
    io.check(candidate['offenseFactor']==.60 and candidate['penetrationFactor']==50 and candidate['change']['to']==.2125,'Wrong archived candidate')
    original=Path(plan['originalLiveSource']);io.authenticate(original)
    io.health_pressure_module().verify(original/'content',source/'content',candidate,7)
    io.check(cells==io.read(original/'cells.json'),'Whole original family changed')
    for relative,pin in io.read(original/'scope.json')['contentHashes'].items():
        io.check(io.sha(ROOT/'LL/src/API/API.LL/Data'/relative)==pin,'Original live source changed')
    scope=io.read(source/'scope.json');runtime=args.artifacts.resolve()/'bin/EssenceSystem.Tests/release'
    for name,pin in scope['execution']['assemblyHashes'].items():io.check(io.sha(runtime/(name+'.dll'))==pin,'Historical runtime changed')
    history,ledgers=io.history();io.check(len(history)==entry['initialExclusions']==plan['initialExclusions']==921468,'Unexpected reservations')
    archive=source/'evaluation';lookup={c['id']:c for c in cells};saved={c['cellId']:{} for c in chosen};trials={}
    rows={r['id']:r for r in io.read(source/'result.json')['rows']}
    for line in (archive/'trials.jsonl').read_text(encoding='utf-8').splitlines():
        t=json.loads(line)
        if t['stage'] not in saved:continue
        io.check(io.read(archive/'recipes'/(t['recipe']+'.json'))==dict(lookup[t['stage']]['scenario'],seeds=seeds),'Raw recipe changed')
        report=json.loads(gzip.decompress((archive/'battles'/(t['id']+'.json.gz')).read_bytes()))
        io.check(t['seed'] not in saved[t['stage']] and report['battle']['seed']==t['seed'],'Repeated or wrong saved seed')
        saved[t['stage']][t['seed']]=report;trials[t['stage'],t['seed']]=t
    for key,reports in saved.items():io.check(set(reports)==set(seeds) and sum(r['succeeded'] for r in reports.values())==rows[key]['wins'],'Incomplete historical panel')
    selected=[dict(**c,trial=trials[c['cellId'],seed]) for seed in plan['seeds'] for c in chosen]
    resource_path=ROOT/'TestResults/tower-floor7-health-casualty-evidence-20260930.json';reference=io.read(resource_path)
    io.check(io.sha(resource_path)==plan['resourceReferenceSha256'],'Measured reference changed')
    io.check(reference['replays']==64 and reference['status']=='VerifiedHealthCasualtyDiagnostic','Invalid measured reference')
    resource=dict(source=str(resource_path),sha256=io.sha(resource_path),referenceReplays=64,plannedReplays=48,
        projectedSeconds=2*reference['summedProcessSeconds']*48/64,projectedBytes=2*reference['archiveBytes']*48/64,admittedSeconds=.8*1200,admittedBytes=.8*2*1024**3)
    io.check(resource['projectedSeconds']<resource['admittedSeconds'] and resource['projectedBytes']<resource['admittedBytes'],'Measured resource admission failed')
    paths=[Path(__file__),Path(casualty.__file__),Path(residual.__file__),Path(resistance.__file__),Path(mixed.__file__),Path(mixed.base.__file__),Path(io.__file__),args.plan,args.entry,args.protocol,
        ROOT/'build/bounded_windows_process.py',Path(__file__).with_name('test-tower-survival-output-diagnostic.py'),resource_path,
        ROOT/'LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs']
    paths.extend((ROOT/'TestResults/tower-floor7-survival-output-tests-20260930').iterdir())
    pins={str(p.resolve()):io.sha(p) for p in paths}
    output.mkdir();shutil.copy2(args.protocol,output/'protocol.md');shutil.copy2(__file__,output/'owner-source.py')
    io.write(output/'declaration.json',dict(source=str(source),sourceManifestSha256=SOURCE_PIN,selected=selected,chosen=chosen,sourcePins=pins,
        runtimePins=entry['runtimePins'],liveCatalogPins=entry['liveCatalogPins'],ledgerPins=ledgers,initialExclusions=len(history),
        maximumReplays=48,maximumSeconds=1200,maximumReplaySeconds=60,maximumLogBytes=64*1024**2,maximumBytes=2*1024**3,newSeeds=0,usedForAcceptance=False))
    io.write(output/'resource-admission.json',resource)
    process=mixed.base.module('residual_process',ROOT/'build/bounded_windows_process.py');deadline=time.monotonic()+1200
    attempts=0;details=[];status='Failed';print('Admitted 48 exact historical replays; 384 selected-recipe outcomes recounted.',flush=True)
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
            if len(details)%3==0:print(f'Completed {len(details)}/48 complete-report matches.',flush=True)
            io.check(sum(p.stat().st_size for p in output.iterdir() if p.is_file())<=2*1024**3,'Diagnostic byte limit exceeded')
        for group in (pins,entry['runtimePins'],entry['liveCatalogPins'],ledgers):
            for path,pin in group.items():io.check(io.sha(path)==pin,'Frozen input changed')
        io.check(io.history()[0]==history,'Diagnostic allocated seeds')
        io.write(output/'details.json',details)
        io.write(output/'result.json',dict(status='VerifiedDescriptiveDiagnostic',recountedSavedFights=384,repeatedFights=48,newSeeds=0,usedForAcceptance=False,finalExclusions=len(history)))
        status='Complete'
    finally:
        io.write(output/'completion.json',dict(status=status,attemptedReplays=attempts,completedReplays=len(details),newSeeds=0,usedForAcceptance=False))
        io.write(output/'files.json',{p.name:io.sha(p) for p in sorted(output.iterdir()) if p.is_file()})


if __name__=='__main__':main()
