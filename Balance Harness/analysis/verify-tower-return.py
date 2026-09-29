"""Independently reconcile post-wave ownership, native preparation and chronological Tower return rewards."""
import argparse
from collections import Counter
from copy import deepcopy
import importlib.util
import json
import math
from pathlib import Path
import sys

sys.dont_write_bytecode=True
def module(name,file):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
owner_module=module('return_owner','run-tower-return.py');io=owner_module.io;check=io.check
equipment=module('return_equipment','verify-tower-earned-party.py')
native=module('return_native','verify-tower-unlock.py')
ARMS=['actual','baseline','supplied']
EPOCH=native.ticks_at('2026-09-28T00:00:00+00:00')


def instant(r):return EPOCH+r['idleSeconds']*10000000+r['combatTicks']*1000000+r['waitingTicks']
def wait(r,t):
    value=deepcopy(r);delta=t-instant(r);check(delta>=0,'Runtime rewind');value['waitingTicks']+=delta;return value
def floor_check(p,api):
    catalog=io.read(api/'Data/world-tower/tower-floors.json');floor=next(f for f in catalog['floors'] if f['floorNumber']==p['floorNumber'])
    for k,v in floor.items():
        if k=='guardianScaling':check({a:native.single(b) for a,b in v.items()}=={a:native.single(b) for a,b in p[k].items()},'Guardian scaling drift')
        else:check(v==p[k],'Production floor drift: '+k)
    curve=catalog['rewardCurve'];tokens=math.floor(curve['baseReward']*(1+(curve['maximumMultiplier']-1)*((p['floorNumber']-1)/(curve['maximumFloor']-1))**curve['exponent'])+0.5)
    check(p['towerTokens']==tokens and p['firstClearTowerTokens']==tokens*4,'Tower reward drift')


def qualify(q,output):
    for key,(_,pin) in owner_module.ARCHIVES.items():io.manifest(Path(q[key]),pin)
    runtime=Path(q['runtimeArchive']);wave=Path(q['waveArchive']);entry=Path(q['entryArchive']);unlock=Path(q['unlockArchive']);party=Path(q['partyArchive']);api=Path(q['apiRoot'])
    costs=io.read(Path(q['scoringRules']))['scores'];select=lambda owned:equipment.selected(owned,costs)
    personal={p['history']:io.read(runtime/p['file']) for p in io.read(runtime/'result.json')['personal']}
    branches={(b['server'],b['history']):b for b in io.read(runtime/'branches.json')}
    earned={(b['server'],b['history']):b for b in io.read(wave/'result.json')['results']}
    result=io.read(output/'result.json');check(result['version']==owner_module.VERSION and result['status']=='EarnedReturnPreparationComplete','Wrong qualification status')
    check(result['plan']==io.read(Path(q['fixtures'])/'tower-return.json') and result['newFights']==result['newCombatSeeds']==result['measuredPlayerSamples']==0,'Qualification made combat/time claim')
    keys={b[0] for b in branches};seen=set();files={'result.json'};counts=Counter();changed=0;participants=0
    for row in result['parties']:
        key=row['key'];check(key in keys and key not in seen,'Repeated/foreign server');seen.add(key)
        file=key+'--preparation.json.gz';files.add(file);p=io.read(output/file);old=io.read(unlock/(key+'--journey.json'))
        outcome,rotation,path=key.split('--');original=io.read(party/f'earned-progression--{outcome}--{rotation}--25920--floor-1.json.gz')['party']
        check(p['key']==key and p['path']==int(path) and p['previousState']==old['final'],'Reset/wrong server')
        history=[io.read(unlock/s['file']) for s in old['steps']];check(p['historicalAttempts']==history,'Lost historical failures/scouting cap')
        expected={};baseline={};times=[native.ticks_at(old['final']['at'])]
        for owner in p['owners']:
            name=owner['history'];b=branches[(key,name)];source=personal[name];cp=io.read(entry/source['checkpointFile'])['checkpoint']['point']
            identity=cp['character']['id'];check(identity not in expected,'Duplicate personal owner');baseline[identity]=deepcopy(cp)
            r=wait(source['canonicalRuntime'],native.ticks_at(b['at']));point=deepcopy(cp);pity=source['retainedBlueprintProgress'];sourcefile=runtime/(name+'--runtime.json')
            if earned[(key,name)]['actualEntries']==1:
                sourcefile=wave/earned[(key,name)]['progressFile'];progress=io.read(sourcefile);r=progress['after'];pity=progress['ordinary']['after']
                point.update(character=progress['character'],owned=progress['owned'],supplyItems=cp['supplyItems']+int(progress['supply']['opened']))
            now=instant(r);times.append(now)
            check(native.ticks_at(owner['point']['availableAt'])==now,'Wrong earned availability');point['availableAt']=owner['point']['availableAt']
            check(owner['point']==point and owner['origin']==source['origin'] and owner['blueprintProgress']==pity,'Lost/changed personal progress')
            check(owner['sourceHash']==io.sha(sourcefile) and Path(owner['sourceFile'])==sourcefile,'Unpinned personal source')
            owned=point['owned'];check(len(equipment.items(owned))==len(owned) and all(i['state']['ownership']['ownerId']==identity for i in owned),'Gear shared/duplicated')
            check(point['character']['equipment']==select(owned),'Discarded stronger gear');expected[identity]=(owner,r)
        start=max(times);check(len(expected)==16 and native.ticks_at(p['party']['startsAt'])==start,'Owner loss or invented assembly activity')
        for owner,r in expected.values():check(owner['runtime']==wait(r,start),'Changed runtime, XP, stock, prophecy, mastery or waiting')
        ids=[m['character']['id'] for m in original['members']];members=[expected[id][0]['point'] for id in ids]
        check(len(ids)==len(set(ids))==5 and p['party']['members']==members,'Changed fixed roster or borrowed gear')
        actual=p['party'];nextfloor=old['highestCleared']+1;floor_check(actual['floor'],api)
        check(actual==dict(original,id=key,startsAt=actual['startsAt'],floor=actual['floor'],members=members) and actual['floor']['floorNumber']==nextfloor,'Changed party policy/floor')
        gate=old['final']['floors'][nextfloor-1];check(not gate['isCleared'] and gate['unlockedAt'] and native.ticks_at(gate['unlockedAt'])<=start,'Locked/already-cleared next floor')
        check(p['baseline']==dict(actual,id=key+'--baseline',members=[baseline[id] for id in ids]),'Unmatched pre-wave control')
        for i,m in enumerate(p['supplied']['members']):
            original_member=members[i];owned=m['owned'];extra=owned[len(original_member['owned']):]
            check(owned[:len(original_member['owned'])]==original_member['owned'] and len(extra)==7,'Control lost stronger gear or wrong supply count')
            check(all(e['rarity']=='Epic' and e['state']['tier']==1 and e['state']['ownership']['ownerId']==ids[i] for e in extra),'Changed supplied control band/owner')
            check(m==dict(original_member,owned=owned,character=dict(original_member['character'],equipment=select(owned))),'Control granted levels/Essences')
        check(p['supplied']==dict(actual,id=key+'--supplied',members=p['supplied']['members']),'Wrong supplied control party')
        for arm in ARMS:
            party_value=p[{'actual':'party','baseline':'baseline','supplied':'supplied'}[arm]];prep=p['preparations'][arm]['prepared']
            friendly=[c for c in prep if c['slot']['side']=='Friendly'];hostile=[c for c in prep if c['slot']['side']=='Hostile']
            check(len(friendly)==5 and len(hostile)==1 and hostile[0]['slot']['sourceEntityId']==actual['floor']['guardianCreatureId']
                and hostile[0]['sourceMonsterId']==actual['floor']['guardianAbilityProfileId'],'Wrong native party/guardian')
            for index,(m,c) in enumerate(zip(party_value['members'],friendly)):
                actor=m['character'];check(c['slot']['sourceEntityId']==ids[index] and c['slot']['partyNumber']==1 and c['level']==actor['level'] and c['baseAttributes']==actor['baseAttributes'],'Prepared identity/growth mismatch')
                check(equipment.items(c['equipment'])==equipment.items([e['data'] for e in actor['equipment']]),'Prepared equipment mismatch')
                check(c['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in actor['essences']],'Prepared Essence mismatch')
                participants+=1
        state=deepcopy(old['final']);state['at']=actual['startsAt']
        for t in state['tokens']:
            owner=expected[t['owner']][0];t.update(level=owner['point']['character']['level'],experience=owner['runtime']['growth']['state']['experience'])
        for probe in state['probes']:probe['level']=expected[probe['owner']][0]['point']['character']['level']
        check(native.normalized(p['initialState'])==native.normalized(state),'Reset server tokens/titles/unlocks/old rewards')
        changed_here=sum(m['character']['equipment']!=baseline[m['character']['id']]['character']['equipment'] for m in members);changed+=changed_here;counts[nextfloor]+=1
        check(row==dict(key=key,floor=nextfloor,startsAt=actual['startsAt'],owners=16,participants=5,changedLoadouts=changed_here,historicalAttempts=len(history)),'Wrong preparation summary')
    check(seen==keys and set(io.read(output/'files.json'))==files,'Incomplete qualification')
    return dict(status='VerifiedEarnedReturnPreparation',parties=32,owners=512,preparedMembers=participants,changedParticipantLoadouts=changed,
        floors=dict(sorted(counts.items())),newFights=0,newCombatSeeds=0,measuredPlayerSamples=0)


def combat(q,output,owner):
    qualification=Path(q['qualification']);io.manifest(qualification,q['qualificationPin']);result=io.read(output/'result.json')
    check(result['version']==owner_module.VERSION and result['status']=='EarnedReturnCombatComplete' and result['qualificationPin']==q['qualificationPin'],'Wrong combat admission')
    check(result['plan']==io.read(Path(q['fixtures'])/'tower-return.json') and result['reservedSeeds']==128 and result['measuredPlayerSamples']==result['earnedNewEquipment']==0 and not result['searchPerformed'],'Combat claim drift')
    ledger=io.read(owner/'seed-ledger.json');old,_=owner_module.exclusions(Path(ledger['preceding'][-1]['archive']));reserved=[s for p in q['panels'].values() for s in p]
    check(len(reserved)==len(set(reserved))==128 and reserved==ledger['reserved'] and not old.intersection(reserved)
        and ledger['exclusionUnionCount']==878739 and ledger['state']=='ReservedIncludingUnconsumed','Reused/lost seed reservations')
    keys={r['key'] for r in io.read(qualification/'result.json')['parties']};check(set(q['panels'])==keys and all(len(v)==4 for v in q['panels'].values()),'Wrong server seed panels')
    options=native.settings(Path(q['apiRoot'])/'appsettings.json')['WorldTower'];files={'result.json'};seen=set();used=set();counts=Counter();groups=Counter();attempts=replays=0;rows=[]
    for row in result['journeys']:
        key=row['key'];check(key in keys and key not in seen,'Repeated/foreign continuation');seen.add(key)
        p=io.read(qualification/(key+'--preparation.json.gz'));file=key+'--continuation.json.gz';files.add(file);j=io.read(output/file)
        check(j['key']==key and j['initial']==p['initialState'],'Reset initial server');state=j['initial'];floor=p['party']['floor']['floorNumber'];ids={m['character']['id'] for m in p['party']['members']}
        history=p['historicalAttempts'];offset=sum(a['floor']==floor for a in history);success=False
        for index,step in enumerate(j['steps']):
            check(index<4 and not success,'Retry/sample cap exceeded');seed=q['panels'][key][index];check(seed not in used,'Reused new attempt seed');used.add(seed)
            filename=key+f'--attempt-{offset+index}.json';files.add(filename);a=io.read(output/filename);check(step['file']==filename,'Wrong attempt filename')
            check((a['key'],a['floor'],a['index'],a['attempt'],a['seed'])==(key,floor,index,offset+index,seed) and set(a['battles'])==set(ARMS),'Wrong native ordinal/paired panel')
            for arm,b in a['battles'].items():
                s=b['summary'];check(b['seed']==seed and b['succeeded']==(s['contentOutcome']=='Victory') and s['contentOutcome'] in ['Victory','Defeat','Draw'],'Outcome mismatch')
                check({c['id'] for c in s['friendly']}==ids and len(s['hostile'])==1 and 0<=s['durationTicks']<=6000 and abs(s['durationSeconds']-s['durationTicks']/10)<1e-9,'Combat participants/duration drift')
                if b['succeeded']:check(b['guardianHealthRemainingPercent']==0,'Guardian alive on victory')
                counts[(floor,arm,'attempts')]+=1;counts[(floor,arm,'wins')]+=int(b['succeeded'])
                if index==0:
                    replay=key+'--'+arm+'--replay.json';files.add(replay);check(io.read(output/replay)==b,'Exact replay mismatch');replays+=1
            battle=a['battles']['actual'];success=battle['succeeded'];attempts+=1
            check(step==dict(file=filename,seed=seed,success=success),'Step summary drift')
            receipt=a['receipt'];check(receipt['before']==state and receipt['earlyFinalizationRejected'] and receipt['duplicateFinalizationRejected'] and receipt['status']==('Succeeded' if success else 'Failed'),'Nonsequential/duplicate finalization')
            after=receipt['after'];check(0<=battle['summary']['durationTicks']*1000000-(native.ticks_at(after['at'])-native.ticks_at(state['at']))<=1,'Playback clock mismatch')
            want=deepcopy(state);want['at']=after['at'];progress=want['floors'][floor-1]
            if success:
                progress.update(isCleared=True,scoutingProgress=100,firstClearAttemptId=receipt['attemptId'],clearedAt=after['at']);want['floors'][floor]['unlockedAt']=after['at']
                for t in want['tokens']:
                    if t['owner'] in ids:t['towerTokens']+=p['party']['floor']['firstClearTowerTokens']
                want['titles'].extend(dict(owner=id,key=p['party']['floor']['rewardTitleKey']) for id in ids)
                want['unlocks'].extend(dict(unlockKey=u['key'],sourceFloorNumber=floor,unlockedAt=after['at']) for u in p['party']['floor']['unlocks'])
                if floor==3:
                    for probe in want['probes']:probe['hypotheticalNextCompletedDungeonChest']='item.tower_supply.v1.floor_04'
            else:
                week=native.at(after['at']).isocalendar()[:2]
                failures=sum(h['floor']==floor and not h['battle']['succeeded'] and native.at(h['receipt']['after']['at']).isocalendar()[:2]==week for h in history)
                if failures<options['FailedAttemptScoutingWeeklyCap']:progress['scoutingProgress']=min(100,progress['scoutingProgress']+options['FailedAttemptScoutingGain'])
            check(native.normalized(after)==native.normalized(want),'Native reward/scouting/title/unlock mismatch')
            history.append(dict(floor=floor,battle=battle,receipt=receipt));state=after
        check(success or len(j['steps'])==4,'Premature stop');check(j['final']==state and j['stop']==('NextFloorCleared' if success else 'FourAttemptCap') and j['highestCleared']==(floor if success else floor-1),'Wrong terminal server')
        check(j['owners']==[dict(o,runtime=wait(o['runtime'],native.ticks_at(state['at']))) for o in p['owners']],'Tower phase lost runtime/items/pity or granted personal activity')
        check(row==dict(key=key,floor=floor,success=success,attempts=len(j['steps']),startedAt=p['party']['startsAt'],endedAt=state['at']),'Wrong journey summary')
        groups[(floor,success)]+=1;rows.append(row)
    check(seen==keys and set(io.read(output/'files.json'))==files,'Missing/unexpected combat outputs')
    check(result['attempts']==attempts and result['controls']==attempts*2 and result['replays']==replays==96 and result['fights']==attempts*3+replays<=480,'Wrong fight accounting')
    return dict(status='VerifiedEarnedReturnCombat',servers=32,attempts=attempts,controls=attempts*2,replays=replays,fights=result['fights'],newCombatSeeds=128,
        reservedUnconsumedSeeds=128-len(used),exclusionUnionCount=878739,measuredPlayerSamples=0,
        groups=[dict(floor=f,success=s,servers=v) for (f,s),v in sorted(groups.items())],
        arms=[dict(floor=f,arm=arm,attempts=counts[(f,arm,'attempts')],wins=counts[(f,arm,'wins')]) for f in sorted({r['floor'] for r in rows}) for arm in ARMS],rows=rows)


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');output=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'Fresh audit receipt outside output required')
    check(d['version']==owner_module.VERSION and d['requestPin']==io.sha(owner/'request.json'),'Declaration/request drift')
    for file,pin in q['inputHashes'].items():check(io.sha(Path(file))==pin,'Frozen input drift: '+file)
    io.manifest(output,a.manifest_pin);check(sum(f.stat().st_size for f in output.iterdir())<=d['maximumBytes']==256*1048576,'Output cap')
    prepare=d['mode']=='prepare';check(d['mode'] in ['prepare','combat'] and d['retries']==0 and d['newSeeds']==(0 if prepare else 128)
        and d['maximumFights']==(0 if prepare else 480) and d['maximumSeconds']==(300 if prepare else 900) and d['maximumNativeSeconds']==(240 if prepare else 840),'Changed envelope')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=d['maximumSeconds']+process['cleanupAllowanceSeconds'],'Unfinished/overbudget process')
    result=qualify(q,output) if prepare else combat(q,output,owner);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']))
    io.write(a.receipt,result);print(json.dumps({k:v for k,v in result.items() if k!='rows'},indent=2))


if __name__=='__main__':main()
