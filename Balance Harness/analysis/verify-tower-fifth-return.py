"""Audit retained five-Essence admission, native Tower history and bounded paired combat without rerunning fights."""
import argparse
from collections import Counter
from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import sys
sys.dont_write_bytecode=True
def module(name,file):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
owner_module=module('fifth_return_owner','run-tower-fifth-return.py');io=owner_module.io;check=io.check
prior_audit=module('fifth_return_prior','verify-tower-return.py');native=prior_audit.native;equipment=prior_audit.equipment;wait=prior_audit.wait
ARMS=['actual','supplied']

def population_boundary(before,after):
    check(before['at']==after['at'],'Population expansion added activity')
    for field in ['floors','titles','unlocks','earnedNewEquipment']:check(before[field]==after[field],'Population awarded Tower rewards')
    b={t['owner']:t for t in before['tokens']};a={t['owner']:t for t in after['tokens']}
    check(len(b)==6 and len(a)==16 and b.keys()<=a.keys() and all(a[k]==v for k,v in b.items()),'Population lost old balances/growth')
    check(all(t['towerTokens']==0 for k,t in a.items() if k not in b),'Retroactive newcomer tokens')
    bp={p['owner']:p for p in before['probes']};ap={p['owner']:p for p in after['probes']}
    check(bp.keys()==b.keys() and ap.keys()==a.keys() and sum(p['participating'] for p in bp.values())==5 and sum(p['participating'] for p in ap.values())==10,'Population coverage drift')
    for k,p in bp.items():check(ap[k]==dict(p,participating=ap[k]['participating']) and (not p['participating'] or ap[k]['participating']),'Changed old population probe')
    for k,p in ap.items():check(p['level']==a[k]['level'],'Population level mismatch')

def qualify(q,output):
    for key,(_,pin) in owner_module.ARCHIVES.items():io.manifest(Path(q[key]),pin)
    archive=Path(q['sourceArchive']);source=io.read(archive/'result.json');result=io.read(output/'result.json');api=Path(q['apiRoot']);fixtures=Path(q['fixtures'])
    check(result['version']==owner_module.VERSION and result['status']=='EarnedFifthReturnPrepared' and result['plan']==io.read(fixtures/'tower-fifth-return.json'),'Changed qualification')
    check(result['newFights']==result['newSeeds']==result['newEssencesGranted']==result['measuredPlayerSamples']==0,'Unadmitted qualification reward/sample')
    keys={r['key'] for r in source['servers']};seen=set();files={'result.json'};prepared_count=history_count=0
    costs=io.read(Path(q['scoringRules']))['scores']
    for row in result['prepared']:
        key=row['key'];check(key in keys and key not in seen,'Duplicate or missing server');seen.add(key)
        filename=key+'--preparation.json.gz';files.add(filename);proof=io.read(output/filename);p=proof['continuation'];old=io.read(archive/(key+'--continuation.json.gz'))
        check(proof['sourceFile']==key+'--continuation.json.gz' and proof['sourceHash']==io.sha(archive/proof['sourceFile']),'Changed source pin')
        check(p['key']==key and p['path']==int(key.split('--')[-1]) and p['owners']==old['owners'] and p['previousState']==old['server'] and p['historicalAttempts']==old['historicalAttempts'],'Lost runtime/gear/rewards/history')
        history=p['historicalAttempts'];pop=next(i for i,a in enumerate(history) if a['floor']==5);personal=old['personalRefreshBefore']
        check(proof['personalRefreshBefore']==personal and proof['populationRefreshBefore']==pop and proof['nextPersonalRefreshBefore']==old['nextPersonalRefreshBefore']==len(history),'Unbound refresh boundary')
        check(0<personal<pop and len(history)-pop==4 and all(a['floor']==5 and not a['battle']['succeeded'] for a in history[pop:]),'Lost prior floor-5 failures')
        for i,a in enumerate(history):
            before=a['receipt']['before'];after=a['receipt']['after'];history_count+=1
            check(native.ticks_at(before['at'])<=native.ticks_at(after['at'])<=native.ticks_at(p['party']['startsAt']),'Future historical attempt')
            if not i:continue
            prior=history[i-1]['receipt']['after']
            if i==pop:population_boundary(prior,before)
            elif i==personal:
                check(native.ticks_at(prior['at'])<=native.ticks_at(before['at']),'Personal clock rewind')
                for field in ['floors','titles','unlocks']:check(prior[field]==before[field],'Personal refresh changed server rewards')
                check([(t['owner'],t['towerTokens']) for t in prior['tokens']]==[(t['owner'],t['towerTokens']) for t in before['tokens']],'Personal refresh changed balances')
            else:check(prior==before,'Unexplained historical gap')
        check(p['previousState']==history[-1]['receipt']['after'],'Lost terminal historical state')
        owners={o['point']['character']['id']:o for o in p['owners']};party=p['party'];members=party['members'];ids=[m['character']['id'] for m in members]
        check(len(owners)==16 and len(ids)==len(set(ids))==10 and party==old['party'] and p['baseline']==party,'Roster or latest input drift')
        prior_audit.floor_check(party['floor'],api);check(party['floor']['floorNumber']==5 and party['floor']['requiredSlots']==10,'Wrong native floor')
        for o in owners.values():
            check(prior_audit.instant(o['runtime'])==native.ticks_at(party['startsAt']),'Personal clock drift')
            c=o['point']['character'];es=o['runtime']['growth']['ownedEssences']
            check(c['level']==40 and len(es)==5 and [e['level'] for e in es]==[8,8,8,8,1] and es[4]['currentXp']==0,'Unearned fifth training/growth')
            check(c['equipment']==equipment.selected(o['point']['owned'],costs),'Lost stronger personal gear')
        check(members==[owners[id]['point'] for id in ids],'Party substituted another inventory')
        for arm in ARMS:
            arm_party=party if arm=='actual' else p['supplied'];prep=p['preparations'][arm]['prepared']
            if arm=='actual':check(prep==old['prepared'],'Lost paid source preparation parity')
            else:
                check(arm_party==dict(party,id=party['id']+'--supplied',members=arm_party['members']),'Control layout changed')
                for m,a in zip(arm_party['members'],members):
                    extra=m['owned'][len(a['owned']):];check(m['owned'][:len(a['owned'])]==a['owned'] and len(extra)==7,'Control discarded stronger inventory')
                    check(all(x['rarity']=='Epic' and x['quality']=='Fine' and x['state']['rank']==3 and x['state']['tier']==1 and x['state']['ownership']['ownerId']==a['character']['id'] for x in extra),'Control supply band drift')
                    check(m==dict(a,owned=m['owned'],character=dict(a['character'],equipment=equipment.selected(m['owned'],costs))),'Control changed earned growth')
            friendly=[c for c in prep if c['slot']['side']=='Friendly'];hostile=[c for c in prep if c['slot']['side']=='Hostile']
            check(len(friendly)==10 and len(hostile)==1 and hostile[0]['slot']['sourceEntityId']==party['floor']['guardianCreatureId'] and hostile[0]['sourceMonsterId']==party['floor']['guardianAbilityProfileId'],'Wrong actors')
            for i,(m,c) in enumerate(zip(arm_party['members'],friendly)):
                actor=m['character'];check(c['slot']['sourceEntityId']==ids[i] and c['slot']['partyNumber']==i//5+1 and c['level']==actor['level'] and c['baseAttributes']==actor['baseAttributes'],'Prepared slot/growth drift')
                check(equipment.items(c['equipment'])==equipment.items([e['data'] for e in actor['equipment']]),'Prepared gear mismatch')
                check(c['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in actor['essences']],'Prepared Essence mismatch');prepared_count+=1
        state=deepcopy(old['server']);state['at']=party['startsAt'];tokens={t['owner']:t for t in state['tokens']}
        for id,o in owners.items():tokens[id].update(level=o['point']['character']['level'],experience=o['runtime']['growth']['state']['experience'])
        for probe in state['probes']:probe['level']=owners[probe['owner']]['point']['character']['level']
        check(native.normalized(p['initialState'])==native.normalized(state),'Personal refresh replayed Tower rewards')
        admission=proof['eligibility'];check(admission['accepted'] and admission['requiredSlots']==10 and admission['status']=='Ready'
            and admission['participants']==[dict(owner=id,account=id,partySlot=i+1,party=i//5+1) for i,id in enumerate(ids)],'Native rally failed')
        check(admission['modelRatingAvailabilityAssumed'] and admission['assumedRawRating']==1 and not admission['liveAccountsVerified'] and not admission['probeRetained'],'Undeclared admission assumption')
        expected=[dict(attemptId=a['receipt']['attemptId'],floorNumber=a['floor'],requiredSlots=5 if i<pop else 10,
            owners=[v['owner'] for v in a['receipt']['before']['probes'] if v['participating']] if i<pop else ids) for i,a in enumerate(history)]
        check(admission['historicalRosters']==expected,'Historical roster/seat order lost')
        check(len(proof['essences'])==16 and {g['owner'] for g in proof['essences']}==owners.keys(),'Missing ownership gates')
        for g in proof['essences']:
            o=owners[g['owner']];check(g==dict(owner=g['owner'],level=40,unlockedSlots=5,ownedEssences=5,actualAccepted=True,
                savedOwnedIds=[e['id'] for e in o['runtime']['growth']['ownedEssences']],at39=dict(succeeded=False,message='Loadout contains a locked Essence slot.'),
                foreignAt40=dict(succeeded=False,message='A loadout can only use absorbed Essences.'),newEssencesGranted=0,probeRetained=False),'Ownership/slot gate drift')
        check(row==dict(key=key,participants=10,owners=16,startsAt=party['startsAt'],personalRefreshBefore=personal,populationRefreshBefore=pop,nextPersonalRefreshBefore=len(history)),'Qualification summary drift')
    check(seen==keys and len(keys)==15 and result['held']==source['held'],'Lost eligible or held paths')
    for h in result['held']:check(io.sha(Path(q['returnArchive'])/h['file'])==h['hash'],'Changed held server')
    check(set(io.read(output/'files.json'))==files,'Unexpected preparation outputs')
    return dict(status='VerifiedEarnedFifthReturnPrepared',parties=15,heldServers=17,ownersPreserved=512,nativeEssenceGates=240,preparedMembers=prepared_count,
        historicalAttempts=history_count,newFights=0,newCombatSeeds=0,newEssencesGranted=0,measuredPlayerSamples=0)


def combat(q,output,owner):
    qualification=Path(q['qualification']);io.manifest(qualification,q['qualificationPin']);qualify(q,qualification);result=io.read(output/'result.json')
    check(result['version']==owner_module.VERSION and result['status']=='EarnedFifthReturnCombatComplete' and result['qualificationPin']==q['qualificationPin'],'Wrong combat admission')
    check(result['plan']==io.read(Path(q['fixtures'])/'tower-fifth-return.json') and result['reservedSeeds']==60 and result['measuredPlayerSamples']==result['earnedNewEquipment']==0 and not result['searchPerformed'],'Combat claim drift')
    ledger=io.read(owner/'seed-ledger.json');old,_=owner_module.exclusions(Path(ledger['preceding'][-1]['archive']));reserved=[s for p in q['panels'].values() for s in p]
    check(len(reserved)==len(set(reserved))==60 and reserved==ledger['reserved'] and not old.intersection(reserved)
        and ledger['exclusionUnionCount']==885164 and ledger['state']=='ReservedIncludingUnconsumed','Reused/lost seed reservations')
    keys={r['key'] for r in io.read(qualification/'result.json')['prepared']};check(set(q['panels'])==keys and all(len(v)==4 for v in q['panels'].values()),'Wrong server seed panels')
    options=native.settings(Path(q['apiRoot'])/'appsettings.json')['WorldTower'];files={'result.json'};seen=set();used=set();counts=Counter();groups=Counter();attempts=replays=0;rows=[]
    for row in result['journeys']:
        key=row['key'];check(key in keys and key not in seen,'Repeated/foreign continuation');seen.add(key)
        p=io.read(qualification/(key+'--preparation.json.gz'))['continuation'];file=key+'--continuation.json.gz';files.add(file);j=io.read(output/file)
        check(j['key']==key and j['initial']==p['initialState'],'Reset initial server');state=j['initial'];floor=p['party']['floor']['floorNumber'];ids={m['character']['id'] for m in p['party']['members']}
        history=deepcopy(p['historicalAttempts']);offset=sum(a['floor']==floor for a in history);success=False
        for index,step in enumerate(j['steps']):
            check(index<4 and not success,'Retry/sample cap exceeded');seed=q['panels'][key][index];check(seed not in used,'Reused new attempt seed');used.add(seed)
            filename=key+f'--attempt-{offset+index}.json';files.add(filename);a=io.read(output/filename);check(step['file']==filename,'Wrong attempt filename')
            check((a['key'],a['floor'],a['index'],a['seed'])==(key,floor,index,seed) and set(a['battles'])==set(ARMS),'Wrong native ordinal/paired panel')
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
            history.append(dict(floor=floor,attempt=offset+index,battle=battle,receipt=receipt));state=after
        check(success or len(j['steps'])==4,'Premature stop');check(j['final']==state and j['stop']==('FloorFiveCleared' if success else 'FourAttemptCap') and j['highestCleared']==(floor if success else floor-1),'Wrong terminal server')
        check(j['owners']==[dict(o,runtime=wait(o['runtime'],native.ticks_at(state['at']))) for o in p['owners']],'Tower phase lost runtime/items/pity or granted personal activity')
        proof=io.read(qualification/(key+'--preparation.json.gz'))
        check(j['historicalAttempts']==history and j['personalRefreshBoundaries']==[proof['personalRefreshBefore'],proof['nextPersonalRefreshBefore']]
            and j['populationRefreshBefore']==proof['populationRefreshBefore'],'Lost continuation history/boundaries')
        check(j['party']==dict(p['party'],startsAt=state['at']),'Changed continuation roster/gear/Essences')
        check(row==dict(key=key,floor=floor,success=success,attempts=len(j['steps']),startedAt=p['party']['startsAt'],endedAt=state['at']),'Wrong journey summary')
        groups[(floor,success)]+=1;rows.append(row)
    check(seen==keys and set(io.read(output/'files.json'))==files,'Missing/unexpected combat outputs')
    check(result['attempts']==attempts and result['controls']==attempts and result['replays']==replays==30 and result['fights']==attempts*2+replays<=150,'Wrong fight accounting')
    check(result['held']==io.read(qualification/'result.json')['held'] and result['newEssencesGranted']==0,'Changed held paths or granted Essence')
    return dict(status='VerifiedEarnedFifthReturnCombat',servers=15,heldServers=17,attempts=attempts,controls=attempts,replays=replays,fights=result['fights'],newCombatSeeds=60,
        reservedUnconsumedSeeds=60-len(used),exclusionUnionCount=885164,measuredPlayerSamples=0,
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
    prepare=d['mode']=='prepare';check(d['mode'] in ['prepare','combat'] and d['retries']==0 and d['newSeeds']==(0 if prepare else 60)
        and d['maximumFights']==(0 if prepare else 150) and d['maximumSeconds']==900 and d['maximumNativeSeconds']==840,'Changed envelope')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=d['maximumSeconds']+process['cleanupAllowanceSeconds'],'Unfinished/overbudget process')
    result=qualify(q,output) if prepare else combat(q,output,owner);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']))
    io.write(a.receipt,result);print(json.dumps({k:v for k,v in result.items() if k!='rows'},indent=2))


if __name__=='__main__':main()
