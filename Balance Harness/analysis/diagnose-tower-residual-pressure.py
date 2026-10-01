"""Describe the frozen rejected floor-7 candidate using 96 saved replays, never acceptance."""
import argparse
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT=Path(__file__).resolve().parents[2]
s=importlib.util.spec_from_file_location('residual_resistance',Path(__file__).with_name('diagnose-tower-resistance.py'))
resistance=importlib.util.module_from_spec(s);s.loader.exec_module(resistance)
mixed=resistance.mixed;io=mixed.io
PLAN_PIN='ef6d116b3ba9984a0b55819cf2fd2d360ad111e2fa5c793970bfac787d0afadb'
SOURCE_PIN='c7fb1d8c5ed09ec7ea1f02694ddc6ca3629de842dfeb8d2bfa4d1e9f7f7f7db6'
PROFILES=('baseline','restorer-specialization','health-and-regeneration')


def validate_selection(plan,cells,seeds):
    io.check(plan['version']=='tower-floor7-residual-pressure-diagnostic-v1' and plan['status']=='ProposedNotAllocated' and
        plan['maximumHistoricalReplays']==96 and plan['newStudyFights']==plan['newSeeds']==plan['allocatedReplays']==0 and
        not plan['gameplayCandidateSelected'],'Expected unallocated descriptive diagnostic')
    io.check(len(cells)==plan['familySize']==120 and all(c['scenario']['floorNumber']==7 for c in cells) and
        len({io.composition_key(c) for c in cells})==plan['actualCompositions']==8,'Complete original family required')
    io.check(len(seeds)==len(set(seeds))==128 and plan['seeds']==seeds[:16],'First sixteen declared seeds required')
    lookup={c['id']:c for c in cells};io.check(len(lookup)==120,'Duplicate saved recipe identity')
    io.check(len(plan['cases'])==2,'Two parent compositions required')
    chosen=[];compositions=set()
    for label,group in zip(('A','B'),plan['cases'],strict=True):
        cases=group['cases'];io.check(group['label']==label and [c['gear'] for c in cases]==list(PROFILES),'Exact three equipment profiles required')
        base=lookup[cases[0]['id']];compositions.add(io.composition_key(base))
        io.check([m['partySlot'] for m in base['scenario']['party']]==list(range(1,6)),'Original party positions required')
        for profile,case,count,slots in zip(PROFILES,cases,(0,6,15),([], [2], [1,2,3,4,5]),strict=True):
            cell=lookup[case['id']];raw=copy.deepcopy(cell['scenario'])
            specialized=[m['partySlot'] for m in raw['party'] for item in m['build']['equipment'] if '.spec.' in item['definitionId']]
            io.check(cell['composition']==group['composition'] and cell['gear']==profile and not raw['seeds'] and
                len(specialized)==case['specializedItems']==count and sorted(set(specialized))==case['specializedPartySlots']==slots and
                case['specializedCharacters']==len(slots),'Exact saved equipment counts and slots required')
            for member,plain in zip(raw['party'],base['scenario']['party'],strict=True):
                if member['partySlot'] not in slots:
                    io.check(member['build']['equipment']==plain['build']['equipment'],'Unselected equipment changed')
                member['build']['equipment']=copy.deepcopy(plain['build']['equipment'])
            io.check(raw==base['scenario'],'Raw identity, Essence order, positions or budget changed')
            chosen.append(dict(label=label+'/'+profile,composition=group['composition'],profile=profile,cellId=cell['id'],specializedPartySlots=slots))
    io.check(len(compositions)==2 and len({c['cellId'] for c in chosen})==6,'Two actual compositions and six exact recipes required')
    return chosen


def analyze(saved,replay,scenario):
    result=resistance.analyze(saved,replay,scenario)
    b=replay['battle'];events=b['eventLog'];tps=b['ticksPerSecond'];actors=mixed.party(replay,scenario)
    slots={p['slot']['slotId']:slot for slot,p,_ in actors}
    guardian,_,_=resistance.guardian_totals(replay)
    first=next((i for i,e in enumerate(events) if e['targetId'] in slots and e['eventType']=='Death'),None)
    first_tick=events[first]['timestamp'] if first is not None else None
    boundary=len(events) if first is None else first
    windows={}
    for name,start,end,seconds in (('beforeFirstDeathEvent',0,boundary,first_tick/tps if first is not None else b['summary']['durationSeconds']),
                                 ('fromFirstDeathEvent',boundary,len(events),b['summary']['durationSeconds']-first_tick/tps if first is not None else 0)):
        select=events[start:end]
        incoming=[e for e in select if e['targetId'] in slots];outgoing=[e for e in select if e['targetId']==guardian]
        windows[name]=dict(party=mixed.window(incoming),guardian=mixed.window(outgoing),exposureSeconds=seconds,
            guardianDamagePerSecond=sum(e['finalHealthDamage'] for e in outgoing)/seconds if seconds>0 else None,
            outgoingByOriginalSlot={str(slot):sum(e['finalHealthDamage'] for e in outgoing if e['actorId']==actor) for actor,slot in slots.items()})
    hits=[]
    for index,event in enumerate(events):
        if (event['source']!=resistance.SPRINGTIDE or event['targetId'] not in slots
                or event['eventType'] not in ('Damage','DamageCrit')):continue
        snapshot=event.get('combatEntity')
        io.check(snapshot is not None and snapshot['id']==event['targetId'] and snapshot['maxHealth']>0 and snapshot['health']>=0,
                 'Native target Health snapshot required for each Springtide hit')
        hits.append(dict(index=index,tick=event['timestamp'],slot=slots[event['targetId']],
            healthAfter=snapshot['health'],maxHealthAfter=snapshot['maxHealth'],healthFractionAfter=snapshot['health']/snapshot['maxHealth'],
            finalHealthDamage=event['finalHealthDamage'],healthDamageFraction=event['finalHealthDamage']/snapshot['maxHealth'],
            atOrAfterFirstDeathEvent=first is not None and index>=first))
    return dict(**result,residual=dict(firstDeathEventIndex=first,firstDeathTick=first_tick,windows=windows,springtideHealthSnapshots=hits,
        snapshotMeaning='Native integer target Health and MaxHealth at event emission after the hit; fractions use that event maximum. No reconstructed pre-hit Health.',
        boundaryMeaning='Partition by event index, including the killing damage before the first Death event. Later same-tick events remain after it; zero-duration rates are null.'))


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
    candidate=io.read(ROOT/'TestResults/tower-floor7-penetration-pressure-driver-20260930/candidate-1.json')
    io.check(candidate['offenseFactor']==.60 and candidate['penetrationFactor']==50 and candidate['floor']==7,'Wrong archived candidate')
    original=Path(candidate['source']);io.check(io.sha(original/'files.json')==candidate['sourceManifestSha256'],'Original source changed')
    io.authenticate(original);io.verify_penetration_candidate(original/'content',source/'content',candidate)
    io.check(cells==io.read(original/'cells.json'),'Whole original family changed')
    scope=io.read(source/'scope.json');runtime=args.artifacts.resolve()/'bin/EssenceSystem.Tests/release'
    for name,pin in scope['execution']['assemblyHashes'].items():io.check(io.sha(runtime/(name+'.dll'))==pin,'Historical runtime changed')
    history,ledgers=io.history();io.check(len(history)==entry['initialExclusions']==plan['initialExclusions']==920572,'Unexpected reservations')
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
    resource_path=ROOT/'TestResults/tower-floor7-pressure-diagnostic-evidence-20260930.json';reference=io.read(resource_path)
    io.check(reference['replays']==96 and reference['status']=='VerifiedPressureDiagnostic','Invalid measured reference')
    resource=dict(source=str(resource_path),sha256=io.sha(resource_path),referenceReplays=96,plannedReplays=96,
        projectedSeconds=2*reference['summedProcessSeconds'],projectedBytes=2*reference['archiveBytes'],admittedSeconds=.8*1200,admittedBytes=.8*2*1024**3)
    io.check(resource['projectedSeconds']<resource['admittedSeconds'] and resource['projectedBytes']<resource['admittedBytes'],'Measured resource admission failed')
    paths=[Path(__file__),Path(resistance.__file__),Path(mixed.__file__),Path(mixed.base.__file__),Path(io.__file__),args.plan,args.entry,args.protocol,
        ROOT/'build/bounded_windows_process.py',Path(__file__).with_name('test-tower-residual-pressure-diagnostic.py'),resource_path,
        ROOT/'LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs']
    paths.extend((ROOT/'TestResults/tower-floor7-residual-pressure-tests-20260930').iterdir())
    pins={str(p.resolve()):io.sha(p) for p in paths}
    output.mkdir();shutil.copy2(args.protocol,output/'protocol.md');shutil.copy2(__file__,output/'owner-source.py')
    io.write(output/'declaration.json',dict(source=str(source),sourceManifestSha256=SOURCE_PIN,selected=selected,chosen=chosen,sourcePins=pins,
        runtimePins=entry['runtimePins'],liveCatalogPins=entry['liveCatalogPins'],ledgerPins=ledgers,initialExclusions=len(history),
        maximumReplays=96,maximumSeconds=1200,maximumReplaySeconds=60,maximumLogBytes=64*1024**2,maximumBytes=2*1024**3,newSeeds=0,usedForAcceptance=False))
    io.write(output/'resource-admission.json',resource)
    process=mixed.base.module('residual_process',ROOT/'build/bounded_windows_process.py');deadline=time.monotonic()+1200
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
