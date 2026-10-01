"""Explain the rejected 25% Springtide trial with exactly 64 saved replays."""
import argparse
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT=Path(__file__).resolve().parents[2]
s=importlib.util.spec_from_file_location('health_casualty_residual',Path(__file__).with_name('diagnose-tower-residual-pressure.py'))
residual=importlib.util.module_from_spec(s);s.loader.exec_module(residual)
resistance=residual.resistance;mixed=residual.mixed;io=residual.io
PLAN_PIN='4c0eff586fc0cf218a25792403c1e08be2a0d53568934853395f6e3873c373e9'
SOURCE_PIN='e1c5f5f7939a45c4c6e78d84d1e8198141f4368be47f500bdcdfc6599cfb7c0a'
PROFILES=('restorer-specialization','health-and-regeneration')
CONDITIONS=('condition.weaken','condition.empower','condition.slow')
REMOVALS=('StatusEffectExpired','StatusEffectCleansed','StatusEffectDispelled','StatusEffectRemoved')


def validate_selection(plan,cells,seeds):
    io.check(plan['version']=='tower-floor7-health-springtide-diagnostic-v1' and plan['status']=='ProposedNotAllocated' and
        plan['maximumHistoricalReplays']==64 and plan['newStudyFights']==plan['newSeeds']==plan['allocatedReplays']==0 and
        not plan['gameplayCandidateSelected'],'Expected unallocated descriptive diagnostic')
    io.check(len(cells)==plan['familySize']==120 and all(c['scenario']['floorNumber']==7 for c in cells) and
        len({io.composition_key(c) for c in cells})==plan['actualCompositions']==8,'Complete original family required')
    io.check(len(seeds)==len(set(seeds))==128 and plan['seeds']==seeds[:16],'First sixteen declared seeds required')
    lookup={c['id']:c for c in cells};io.check(len(lookup)==120,'Duplicate saved recipe identity')
    io.check(len(plan['cases'])==4,'Exactly four recipe selections required')
    chosen=[];compositions=set()
    for label,group in zip(('A','B'),(plan['cases'][:2],plan['cases'][2:]),strict=True):
        io.check([c['label'] for c in group]==[label,label] and [c['gear'] for c in group]==list(PROFILES),'Exact ordered labels and profiles required')
        parents={c['composition'] for c in group};io.check(len(parents)==1,'Matching parent required')
        bases=[c for c in cells if c['composition'] in parents and c['gear']=='baseline'];io.check(len(bases)==1,'Unique baseline required')
        base=bases[0];compositions.add(io.composition_key(base))
        io.check([m['partySlot'] for m in base['scenario']['party']]==list(range(1,6)),'Original party positions required')
        for case,count,slots in zip(group,(6,15),([2],[1,2,3,4,5]),strict=True):
            cell=lookup[case['cellId']];raw=copy.deepcopy(cell['scenario'])
            specialized=[m['partySlot'] for m in raw['party'] for item in m['build']['equipment'] if '.spec.' in item['definitionId']]
            io.check(cell['composition']==case['composition'] and cell['gear']==case['gear'] and not raw['seeds'] and
                len(specialized)==case['specializedItems']==count and sorted(set(specialized))==case['specializedPartySlots']==slots,
                'Exact saved equipment counts and slots required')
            for member,plain in zip(raw['party'],base['scenario']['party'],strict=True):
                if member['partySlot'] not in slots:io.check(member['build']['equipment']==plain['build']['equipment'],'Unselected equipment changed')
                member['build']['equipment']=copy.deepcopy(plain['build']['equipment'])
            io.check(raw==base['scenario'],'Raw identity, Essence order, positions or budget changed')
            chosen.append(dict(label=label+'/'+case['gear'],composition=case['composition'],profile=case['gear'],cellId=case['cellId'],specializedPartySlots=slots))
    io.check(len(compositions)==2 and len({c['cellId'] for c in chosen})==4,'Two actual compositions and four exact recipes required')
    return chosen


def event_metrics(events,slots,guardian,seconds,tps):
    """Observed notifications and event-order windows, never inferred exact stacks."""
    slot1=next(actor for actor,slot in slots.items() if slot==1)
    death=next((i for i,e in enumerate(events) if e['targetId']==slot1 and e['eventType']=='Death'),None)
    cut=len(events) if death is None else death
    first_tick=min((e['timestamp'] for e in events if e['targetId'] in slots and e['eventType']=='Death'),default=None)
    first_slots=sorted({slots[e['targetId']] for e in events if e['targetId'] in slots and e['eventType']=='Death' and e['timestamp']==first_tick})
    preceding=next((i for i in range(cut-1,-1,-1) if events[i]['targetId']==slot1 and events[i]['finalHealthDamage']>0),None)
    condition_events=[];notifications={condition:None for condition in CONDITIONS};hits=[]
    for index,e in enumerate(events):
        if e['targetId'] in {*slots,guardian} and e['source'] in CONDITIONS:
            record=dict(index=index,tick=e['timestamp'],target='guardian' if e['targetId']==guardian else str(slots[e['targetId']]),
                condition=e['source'],eventType=e['eventType'],magnitude=e['magnitude'],actorId=e['actorId'],details=e['details'])
            condition_events.append(record)
            if e['targetId']==guardian and e['eventType'] in ('StatusEffect',*REMOVALS):notifications[e['source']]=record
        if e['source']==resistance.SPRINGTIDE and e['targetId'] in slots and e['eventType'] in ('Damage','DamageCrit'):
            hits.append(dict(index=index,tick=e['timestamp'],slot=slots[e['targetId']],guardianNotifications=copy.deepcopy(notifications)))
    windows={}
    for name,left,right,exposure in [('beforeSlot1DeathEvent',0,cut,events[death]['timestamp']/tps if death is not None else seconds),
        ('fromSlot1DeathEvent',cut,len(events),seconds-events[death]['timestamp']/tps if death is not None else 0)]:
        es=events[left:right];incoming=[e for e in es if e['targetId'] in slots];outgoing=[e for e in es if e['targetId']==guardian]
        windows[name]=dict(exposureSeconds=exposure,party=mixed.window(incoming),guardian=mixed.window(outgoing),
            outgoingByOriginalSlot={str(slot):sum(e['finalHealthDamage'] for e in outgoing if e['actorId']==actor) for actor,slot in slots.items()})
    before48=[e for e in events if e['timestamp']<48*tps]
    return dict(firstDeathSlots=first_slots,slot1Death=None if death is None else dict(index=death,tick=events[death]['timestamp'],
        isEarliest=events[death]['timestamp']==first_tick,precedingDamageIndex=preceding,precedingDamage=events[preceding] if preceding is not None else None),
        windows=windows,before48=dict(party=mixed.window([e for e in before48 if e['targetId'] in slots]),guardian=mixed.window([e for e in before48 if e['targetId']==guardian])),
        conditionEvents=condition_events,springtideConditionHistory=hits,
        conditionMeaning='Latest successful application or explicit removal notification preceding each damage event; resisted attempts do not replace it. Not a native active-condition or exact-stack snapshot.')


def analyze(saved,replay,scenario):
    b=replay['battle'];actors=mixed.party(replay,scenario);slots={p['slot']['slotId']:slot for slot,p,_ in actors}
    guardian,_,_=resistance.guardian_totals(replay)
    return dict(**residual.analyze(saved,replay,scenario),casualty=event_metrics(b['eventLog'],slots,guardian,b['summary']['durationSeconds'],b['ticksPerSecond']))


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
    candidate=io.read(ROOT/'TestResults/tower-floor7-health-springtide-driver-20260930/candidate-1.json')
    io.check(candidate['offenseFactor']==.60 and candidate['penetrationFactor']==50 and candidate['change']['to']==.25,'Wrong archived candidate')
    original=Path(plan['originalLiveSource']);io.authenticate(original)
    io.health_pressure_module().verify(original/'content',source/'content',candidate,7)
    io.check(cells==io.read(original/'cells.json'),'Whole original family changed')
    for relative,pin in io.read(original/'scope.json')['contentHashes'].items():
        io.check(io.sha(ROOT/'LL/src/API/API.LL/Data'/relative)==pin,'Original live source changed')
    scope=io.read(source/'scope.json');runtime=args.artifacts.resolve()/'bin/EssenceSystem.Tests/release'
    for name,pin in scope['execution']['assemblyHashes'].items():io.check(io.sha(runtime/(name+'.dll'))==pin,'Historical runtime changed')
    history,ledgers=io.history();io.check(len(history)==entry['initialExclusions']==plan['initialExclusions']==920956,'Unexpected reservations')
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
    resource_path=ROOT/'TestResults/tower-floor7-residual-pressure-evidence-20260930.json';reference=io.read(resource_path)
    io.check(reference['replays']==96 and reference['status']=='VerifiedResidualPressureDiagnostic','Invalid measured reference')
    resource=dict(source=str(resource_path),sha256=io.sha(resource_path),referenceReplays=96,plannedReplays=64,
        projectedSeconds=2*reference['summedProcessSeconds']*64/96,projectedBytes=2*reference['archiveBytes']*64/96,admittedSeconds=.8*1200,admittedBytes=.8*2*1024**3)
    io.check(resource['projectedSeconds']<resource['admittedSeconds'] and resource['projectedBytes']<resource['admittedBytes'],'Measured resource admission failed')
    paths=[Path(__file__),Path(residual.__file__),Path(resistance.__file__),Path(mixed.__file__),Path(mixed.base.__file__),Path(io.__file__),args.plan,args.entry,args.protocol,
        ROOT/'build/bounded_windows_process.py',Path(__file__).with_name('test-tower-health-casualty-diagnostic.py'),resource_path,
        ROOT/'LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs']
    paths.extend((ROOT/'TestResults/tower-floor7-health-casualty-tests-20260930').iterdir())
    pins={str(p.resolve()):io.sha(p) for p in paths}
    output.mkdir();shutil.copy2(args.protocol,output/'protocol.md');shutil.copy2(__file__,output/'owner-source.py')
    io.write(output/'declaration.json',dict(source=str(source),sourceManifestSha256=SOURCE_PIN,selected=selected,chosen=chosen,sourcePins=pins,
        runtimePins=entry['runtimePins'],liveCatalogPins=entry['liveCatalogPins'],ledgerPins=ledgers,initialExclusions=len(history),
        maximumReplays=64,maximumSeconds=1200,maximumReplaySeconds=60,maximumLogBytes=64*1024**2,maximumBytes=2*1024**3,newSeeds=0,usedForAcceptance=False))
    io.write(output/'resource-admission.json',resource)
    process=mixed.base.module('residual_process',ROOT/'build/bounded_windows_process.py');deadline=time.monotonic()+1200
    attempts=0;details=[];status='Failed';print('Admitted 64 exact historical replays; 512 selected-recipe outcomes recounted.',flush=True)
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
            if len(details)%4==0:print(f'Completed {len(details)}/64 complete-report matches.',flush=True)
            io.check(sum(p.stat().st_size for p in output.iterdir() if p.is_file())<=2*1024**3,'Diagnostic byte limit exceeded')
        for group in (pins,entry['runtimePins'],entry['liveCatalogPins'],ledgers):
            for path,pin in group.items():io.check(io.sha(path)==pin,'Frozen input changed')
        io.check(io.history()[0]==history,'Diagnostic allocated seeds')
        io.write(output/'details.json',details)
        io.write(output/'result.json',dict(status='VerifiedDescriptiveDiagnostic',recountedSavedFights=512,repeatedFights=64,newSeeds=0,usedForAcceptance=False,finalExclusions=len(history)))
        status='Complete'
    finally:
        io.write(output/'completion.json',dict(status=status,attemptedReplays=attempts,completedReplays=len(details),newSeeds=0,usedForAcceptance=False))
        io.write(output/'files.json',{p.name:io.sha(p) for p in sorted(output.iterdir()) if p.is_file()})


if __name__=='__main__':main()
