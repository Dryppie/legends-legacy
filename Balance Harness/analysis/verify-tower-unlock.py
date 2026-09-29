"""Independently audit fixed earned parties, chronological Tower receipts and supply eligibility."""
import argparse
from collections import Counter
from copy import deepcopy
from datetime import datetime
import importlib.util
import math
import json
from pathlib import Path
import re
import struct
import sys

sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('unlock_owner',Path(__file__).with_name('run-tower-unlock.py'))
owner_module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=owner_module;spec.loader.exec_module(owner_module)
io=owner_module.io
check=io.check
def at(s):return datetime.fromisoformat(s)
def single(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def settings(path):
    # Preserve quoted URLs and escaped quotes while removing JSON configuration comments.
    token=r'"(?:\\.|[^"\\])*"|//[^\r\n]*|/\*[\s\S]*?\*/'
    text=re.sub(token,lambda m:m[0] if m[0].startswith('"') else ' ',path.read_text(encoding='utf-8-sig'))
    return json.loads(text)
def ticks_at(value):
    match=re.fullmatch(r'(.*T\d\d:\d\d:\d\d)(?:\.(\d{1,7}))?([+-]\d\d:\d\d|Z)',value)
    check(match is not None,'Unsupported native timestamp')
    whole=int(datetime.fromisoformat(match[1]+match[3]).timestamp())
    return whole*10000000+int((match[2] or '').ljust(7,'0'))
def keyed(rows,key):return {r[key]:r for r in rows}
def normalized(state):
    value=deepcopy(state)
    for k in ['tokens','probes']:value[k].sort(key=lambda r:r['owner'])
    value['titles'].sort(key=lambda r:(r['owner'],r['key']))
    value['unlocks'].sort(key=lambda r:(r['sourceFloorNumber'],r['unlockKey']))
    return value


def qualify(q,directory):
    source=Path(q['archive']);io.manifest(source,owner_module.PARTY_PIN)
    catalog=io.read(Path(q['apiRoot'])/'Data/world-tower/tower-floors.json');floors=keyed(catalog['floors'],'floorNumber');curve=catalog['rewardCurve']
    result=io.read(directory/'result.json');plan=io.read(Path(q['fixtures'])/'tower-early-unlock.json')
    check(result['status']=='EarlyUnlockPreparationComplete' and result['sourcePin']==owner_module.PARTY_PIN,'Wrong source qualification')
    check(result['plan']==plan and {k:plan[k] for k in ['version','checkpoint','pathsPerParty','maximumFloor','attemptsPerFloor']}==
        dict(version=owner_module.VERSION,checkpoint=25920,pathsPerParty=4,maximumFloor=3,attemptsPerFloor=4),'Changed plan')
    check(result['newFights']==result['newCombatSeeds']==result['measuredPlayerSamples']==0,'Preparation made combat/time claim')
    expected={(o,r,f) for o in ['perfect','four-of-five'] for r in range(4) for f in range(1,4)};seen=set();files={'result.json'}
    for row in result['parties']:
        file=row['file'];proof=io.read(directory/file);p=proof['party'];f=row['floor'];key=(row['outcome'],row['rotation'],f)
        check(key in expected and key not in seen,'Unexpected or duplicate preparation');seen.add(key);files.add(file)
        src=f"earned-progression--{key[0]}--{key[1]}--25920--floor-1.json.gz";original=io.read(source/src)
        check(proof['sourceFile']==src and proof['sourceHash']==io.sha(source/src),'Unpinned source party')
        check(p['id']==f"{key[0]}--{key[1]}--floor-{f}" and file==p['id']+'.json','Changed party key')
        for field in ['members','policy','outcome','rotation','horizon','startsAt']:
            check(p[field]==original['party'][field],'Altered earned state: '+field)
        check(row['startsAt']==p['startsAt'] and proof['unlockAssumedForPreparationOnly']==(f>1),'Incorrect chronology/unlock claim')
        for field,value in floors[f].items():
            if field=='guardianScaling':check({k:single(v) for k,v in value.items()}=={k:single(v) for k,v in p['floor'][field].items()},'Guardian scaling drift')
            else:check(p['floor'][field]==value,'Floor definition drift: '+field)
        tokens=math.floor(curve['baseReward']*(1+(curve['maximumMultiplier']-1)*((f-1)/(curve['maximumFloor']-1))**curve['exponent'])+0.5)
        check(p['floor']['towerTokens']==tokens and p['floor']['firstClearTowerTokens']==4*tokens,'Reward curve drift')
        friendly=[v for v in proof['prepared'] if v['slot']['side']=='Friendly'];hostile=[v for v in proof['prepared'] if v['slot']['side']=='Hostile']
        check(friendly==[v for v in original['prepared'] if v['slot']['side']=='Friendly'],'Changed production actor preparation')
        check(len(friendly)==5 and len(hostile)==1 and hostile[0]['slot']['sourceEntityId']==floors[f]['guardianCreatureId']
            and hostile[0]['sourceMonsterId']==floors[f]['guardianAbilityProfileId'],'Wrong production guardian')
        if f==1:check(proof['prepared']==original['prepared'] and proof['preparedHash']==original['preparedHash'],'Floor-one prepared hash mismatch')
    check(seen==expected and set(io.read(directory/'files.json'))==files,'Incomplete/unexpected qualification output')
    return dict(status='VerifiedEarlyUnlockPreparation',parties=24,newFights=0,newCombatSeeds=0,measuredPlayerSamples=0)


def combat(q,directory,owner):
    source=Path(q['qualification']);io.manifest(source,q['qualificationPin'])
    result=io.read(directory/'result.json');ledger=io.read(owner/'seed-ledger.json')
    def pinned(entry):
        path=Path(entry['archive']);check(io.sha(path)==entry['sha256'],'Changed reservation ancestor');return io.read(path)
    historical=pinned(ledger['historical']);dungeon=pinned(ledger['dungeon'])
    old=set(historical['historical']+historical['first']+historical['second']+dungeon['reserved'])
    for entry in ledger['preceding']:old.update(pinned(entry)['reserved'])
    latest=Path(ledger['preceding'][-1]['archive']);check(io.sha(latest)==owner_module.EARNED_LEDGER,'Missing earned-combat predecessor')
    old.update(io.read(latest)['reserved']);values=[s for panel in q['panels'].values() for s in panel]
    check(len(old)==877447 and len(values)==len(set(values))==384 and not old.intersection(values),'Wrong/reused reservations')
    check(ledger['reserved']==values and ledger['state']=='ReservedIncludingUnconsumed' and ledger['exclusionUnionCount']==877831,'Reservation ledger mismatch')
    expected={f'{o}--{r}--{p}' for o in ['perfect','four-of-five'] for r in range(4) for p in range(4)}
    check(set(q['panels'])==expected and all(len(v)==12 for v in q['panels'].values()),'Changed fixed panels')
    check(result['status']=='EarlyTowerUnlockDiagnosticComplete' and result['qualificationPin']==q['qualificationPin']
        and result['newSeeds']==384 and result['maximumFights']==480 and result['measuredPlayerSamples']==result['earnedNewEquipment']==0
        and result['searchPerformed'] is False,'Invalid result claim')
    points=io.read(Path(q['archive'])/'points.json');options=settings(Path(q['apiRoot'])/'appsettings.json')['WorldTower']
    used=set();files={'result.json'};seen=set();attempts=replays=0;groups=Counter();rows=[];floor_counts={f:Counter() for f in range(1,4)}
    for row in result['journeys']:
        key=row['key'];check(key in expected and key not in seen,'Unexpected/repeated journey');seen.add(key)
        name=key+'--journey.json';files.add(name);j=io.read(directory/name);outcome,rotation,path=key.split('--');rotation=int(rotation);path=int(path)
        check((j['key'],j['outcome'],j['rotation'],j['path'])==(key,outcome,rotation,path),'Changed journey identity')
        first=io.read(source/f'{outcome}--{rotation}--floor-1.json')['party'];members=first['members'];ids={m['character']['id'] for m in members}
        outsider=next(p for p in points if p['policy']=='earned-progression' and p['outcome']==outcome and p['horizon']==25920 and p['character']['id'] not in ids)
        initial=j['initial'];check(at(initial['at'])==at(first['startsAt']) and row['startedAt']==first['startsAt'],'Wrong initial clock')
        check(not initial['titles'] and not initial['unlocks'] and initial['earnedNewEquipment']==0,'Unearned initial rewards')
        for f in initial['floors']:
            check(f==dict(floorNumber=f['floorNumber'],isCleared=False,unlockedAt=initial['at'] if f['floorNumber']==1 else None,
                clearedAt=None,firstClearAttemptId=None,scoutingProgress=0),'Unearned initial floor')
        check([f['floorNumber'] for f in initial['floors']]==list(range(1,16)),'Missing released floors')
        actors=members+[outsider];tokens=keyed(initial['tokens'],'owner');probes=keyed(initial['probes'],'owner')
        check(len(tokens)==len(probes)==6 and set(tokens)==set(probes)==ids|{outsider['character']['id']},'Wrong native owners')
        for p in actors:
            c=p['character'];h=io.read(Path(q['growthArchive'])/(p['history']+'--history.json'))
            state=next(cp['state'] for cp in h['progression']['checkpoints'] if cp['encounter']==p['encounter'])
            entries=[s for s in h['steps'] if s['encounter']==p['encounter']]
            if entries:state=next(e['after'] for e in h['progression']['entries'] if e['ordinal']==entries[-1]['ordinal'])
            check(tokens[c['id']]==dict(owner=c['id'],towerTokens=0,level=c['level'],experience=state['growth']['experience'])
                and state['growth']['level']==c['level'],'Invented/reset initial earned XP')
            check(probes[c['id']]==dict(owner=c['id'],level=c['level'],participating=c['id'] in ids,
                hypotheticalNextCompletedDungeonChest='item.tower_supply.v1.floor_01',oldCompletedDecisionChest='item.tower_supply.v1.floor_01',
                towerEquipmentSupplyProcessed=True),'Invalid initial supply decision')
        check({r['owner'] for r in j['unchangedOwnedCharacterHashes']}==ids and len(j['unchangedOwnedCharacterHashes'])==5,'Missing retained owner witnesses')
        state=initial;floor=1;ordinal=0;cleared=0;failures=Counter()
        for step in j['steps']:
            check(floor<=3 and ordinal<4 and (step['floor'],step['attempt'])==(floor,ordinal),'Skipped floor/retry cap')
            filename=f'{key}--floor-{floor}--attempt-{ordinal}.json';check(step['file']==filename,'Changed attempt filename');files.add(filename)
            a=io.read(directory/filename);proof_name=f'{outcome}--{rotation}--floor-{floor}.json';proof=io.read(source/proof_name)
            check((a['key'],a['floor'],a['attempt'])==(key,floor,ordinal) and a['sourceFile']==proof_name
                and a['sourceHash']==io.sha(source/proof_name) and a['preparedHash']==proof['preparedHash'],'Unqualified attempt')
            b=a['battle'];s=b['summary'];seed=q['panels'][key][(floor-1)*4+ordinal]
            check(step['seed']==b['seed']==seed and seed not in used,'Wrong/reused consumed seed');used.add(seed);attempts+=1
            check(step['success']==b['succeeded']==(s['contentOutcome']=='Victory') and s['contentOutcome'] in ['Victory','Defeat','Draw'],'Outcome drift')
            check({c['id'] for c in s['friendly']}==ids and len(s['hostile'])==1,'Changed battle owners')
            check(0<=s['durationTicks']<=6000 and abs(s['durationSeconds']-s['durationTicks']/10)<1e-9,'Wrong native duration')
            if b['succeeded']:check(b['guardianHealthRemainingPercent']==0,'Undefeated guardian on victory')
            if ordinal==0:
                replay=filename.removesuffix('.json')+'--replay.json';files.add(replay);check(io.read(directory/replay)==b,'Native replay mismatch');replays+=1
            receipt=a['receipt'];check(receipt['before']==state and receipt['earlyFinalizationRejected'] and receipt['duplicateFinalizationRejected'],'Nonsequential/duplicate finalization')
            check(receipt['status']==('Succeeded' if b['succeeded'] else 'Failed'),'Wrong persisted status')
            after=receipt['after'];check(at(step['startedAt'])==at(state['at']) and at(step['endedAt'])==at(after['at'])
                and 0<=s['durationTicks']*1000000-(ticks_at(after['at'])-ticks_at(state['at']))<=1,'Playback clock drift')
            check(at(after['at']).isocalendar()[:2]==at(initial['at']).isocalendar()[:2],'Undeclared weekly cap rollover')
            want=deepcopy(state);want['at']=after['at'];progress=want['floors'][floor-1];floor_counts[floor].update(attempts=1,wins=int(b['succeeded']))
            if b['succeeded']:
                progress.update(isCleared=True,scoutingProgress=100,firstClearAttemptId=receipt['attemptId'],clearedAt=after['at'])
                want['floors'][floor]['unlockedAt']=after['at']
                for t in want['tokens']:
                    if t['owner'] in ids:t['towerTokens']+=proof['party']['floor']['firstClearTowerTokens']
                want['titles'].extend(dict(owner=o,key=proof['party']['floor']['rewardTitleKey']) for o in ids)
                want['unlocks'].extend(dict(unlockKey=u['key'],sourceFloorNumber=floor,unlockedAt=after['at']) for u in proof['party']['floor']['unlocks'])
                if floor==3:
                    for p in want['probes']:p['hypotheticalNextCompletedDungeonChest']='item.tower_supply.v1.floor_04'
                cleared=floor;floor+=1;ordinal=0
            else:
                # The query counts persisted prior failures; current status has not yet been saved.
                failures[floor]+=1;progress['scoutingProgress']=min(failures[floor],options['FailedAttemptScoutingWeeklyCap'])*options['FailedAttemptScoutingGain'];ordinal+=1
            check(normalized(after)==normalized(want),'Native reward/unlock/XP/supply receipt mismatch: '+filename)
            state=after
        check(cleared==3 or ordinal==4,'Premature stop');check(j['final']==state and j['highestCleared']==cleared,'Wrong terminal state')
        check(j['stop']==('FirstSupplyUpgradeUnlocked' if cleared==3 else 'FloorAttemptCap'),'Wrong stop reason')
        expected_row=dict(key=key,cleared=cleared,attempts=len(j['steps']),startedAt=first['startsAt'],endedAt=state['at']);check(row==expected_row,'Wrong journey summary')
        groups[(outcome,cleared)]+=1;rows.append(dict(**expected_row,playbackSeconds=(at(state['at'])-at(initial['at'])).total_seconds()))
    check(seen==expected and set(io.read(directory/'files.json'))==files,'Missing or unexpected journey output')
    check(result['fights']==attempts+replays<=480 and result['replays']==replays and len(used)==attempts,'Wrong bounded combat accounting')
    return dict(status='VerifiedEarlyTowerUnlockDiagnostic',qualificationPin=q['qualificationPin'],journeys=32,attempts=attempts,replays=replays,
        fights=attempts+replays,newCombatSeeds=384,reservedUnconsumedSeeds=384-len(used),exclusionUnionCount=877831,
        measuredPlayerSamples=0,earnedNewEquipment=0,groups=[dict(outcome=k[0],highestCleared=k[1],paths=v) for k,v in sorted(groups.items())],
        floors=[dict(floor=k,**v) for k,v in floor_counts.items()],rows=rows)


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True)
    a=p.parse_args();owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');directory=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(directory),'Fresh audit receipt outside archive required')
    check(d['version']==owner_module.VERSION and io.sha(owner/'request.json')==d['requestPin'],'Request/declaration drift')
    for file,pin in q['inputHashes'].items():check(io.sha(Path(file))==pin,'Frozen input drift: '+file)
    io.manifest(directory,a.manifest_pin);check(sum(f.stat().st_size for f in directory.iterdir())<=d['maximumBytes']==256*1048576,'Output cap violated')
    prepare=d['mode']=='prepare';check(d['mode'] in ['prepare','combat'] and d['retries']==0 and d['newSeeds']==(0 if prepare else 384)
        and d['maximumFights']==(0 if prepare else 480) and d['maximumSeconds']==(300 if prepare else 900)
        and d['maximumNativeSeconds']==(240 if prepare else 840),'Changed bounded envelope')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0
        and process['seconds']<=d['maximumSeconds']+process['cleanupAllowanceSeconds'],'Incomplete/overbudget process')
    result=qualify(q,directory) if prepare else combat(q,directory,owner)
    result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']))
    io.write(a.receipt,result);print(__import__('json').dumps({k:v for k,v in result.items() if k!='rows'},indent=2))


if __name__=='__main__':main()
