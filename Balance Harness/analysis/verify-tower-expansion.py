"""Independent audit of ten-owner admission, retained rewards, Essence dependencies and bounded floor-5 combat."""
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
owner_module=module('expansion_owner','run-tower-expansion.py');io=owner_module.io;check=io.check
prior_audit=module('expansion_return','verify-tower-return.py');native=prior_audit.native;equipment=prior_audit.equipment
wait=prior_audit.wait
ARMS=['actual','supplied']


def qualify(q,output):
    for key,(_,pin) in owner_module.ARCHIVES.items():io.manifest(Path(q[key]),pin)
    archive=Path(q['returnArchive']);previous=Path(q['preparationArchive']);api=Path(q['apiRoot']);fixtures=Path(q['fixtures'])
    result=io.read(output/'result.json');check(result['version']==owner_module.VERSION and result['status']=='EarnedExpansionPrepared','Wrong qualification status')
    check(result['plan']==io.read(fixtures/'tower-expansion.json') and result['newFights']==result['newSeeds']==result['measuredPlayerSamples']==result['newEssencesGranted']==0,'Unearned qualification claim')
    journeys={r['key']:io.read(archive/(r['key']+'--continuation.json.gz')) for r in io.read(archive/'result.json')['journeys']}
    eligible={key for key,j in journeys.items() if j['highestCleared']==4};held=set(journeys)-eligible
    check(len(eligible)==15 and len(held)==17,'Changed source population');seen=set();files={'result.json','essence-source-rules.json'}
    costs=io.read(Path(q['scoringRules']))['scores'];roster=io.read(fixtures/'tower-earned-party.json')['roster'][:10]
    xp=io.read(api/'Data/progression/character-experience.json')['characterLevelCurve'];xp_needed=[];prepared_count=0
    def required(level):return ((xp['baseExperience']+xp['linearExperiencePerLevel']*level+xp['quadraticExperiencePerLevelSquared']*level*level+xp['roundingIncrement']-1)//xp['roundingIncrement'])*xp['roundingIncrement']
    for row in result['prepared']:
        key=row['key'];check(key in eligible and key not in seen,'Duplicate/unearned expanded server');seen.add(key)
        filename=key+'--expansion.json.gz';files.add(filename);proof=io.read(output/filename);p=proof['continuation'];source=journeys[key];old=io.read(previous/(key+'--preparation.json.gz'))
        check(proof['sourceFile']==key+'--continuation.json.gz' and proof['sourceHash']==io.sha(archive/proof['sourceFile'])
            and proof['preparationFile']==key+'--preparation.json.gz' and proof['preparationHash']==io.sha(previous/proof['preparationFile']),'Source pin drift')
        check(p['key']==key and p['path']==old['path'] and p['previousState']==source['final'] and p['owners']==source['owners'],'Lost owners/gear/runtime/pity/server')
        owners={o['point']['character']['id']:o for o in p['owners']};check(len(owners)==16,'Duplicated owner')
        history=deepcopy(old['historicalAttempts'])
        for step in source['steps']:
            a=io.read(archive/step['file']);history.append(dict(floor=a['floor'],attempt=a['attempt'],battle=a['battles']['actual'],receipt=a['receipt']))
        check(proof['personalRefreshBefore']==len(old['historicalAttempts']) and p['historicalAttempts']==history,'Lost or counterfactual historical attempt')
        rotation=old['party']['rotation'];members=[next(o['point'] for o in owners.values() if o['point']['recipe']==r['recipe'] and o['point']['path']==(rotation+r['pathOffset'])%4) for r in roster]
        ids=[m['character']['id'] for m in members];check(len(set(ids))==10 and ids[:5]==[m['character']['id'] for m in old['party']['members']],'Cloned/moved prior owner')
        party=p['party'];prior_audit.floor_check(party['floor'],api);check(party['floor']['floorNumber']==5 and party['floor']['requiredSlots']==10,'Wrong expansion floor')
        check(party==dict(old['party'],id=key+'--floor-5',members=members,floor=party['floor'],startsAt=source['final']['at']) and p['baseline']==party,'Unmodeled party/growth change')
        check(all(prior_audit.instant(o['runtime'])==native.ticks_at(party['startsAt']) for o in owners.values()),'Clock drift')
        for arm in ARMS:
            arm_party=party if arm=='actual' else p['supplied'];prep=p['preparations'][arm]['prepared']
            if arm=='supplied':
                check(arm_party==dict(party,id=party['id']+'--supplied',members=arm_party['members']),'Changed control layout')
                for actual,m in zip(members,arm_party['members']):
                    extra=m['owned'][len(actual['owned']):];check(m['owned'][:len(actual['owned'])]==actual['owned'] and len(extra)==7,'Control replaced owned inventory')
                    check(all(i['rarity']=='Epic' and i['quality']=='Fine' and i['state']['rank']==3 and i['state']['tier']==1 and i['state']['ownership']['ownerId']==actual['character']['id'] for i in extra),'Wrong control supply band/owner')
                    check(m==dict(actual,owned=m['owned'],character=dict(actual['character'],equipment=equipment.selected(m['owned'],costs))),'Control granted growth/Essences')
            friendly=[c for c in prep if c['slot']['side']=='Friendly'];hostile=[c for c in prep if c['slot']['side']=='Hostile']
            check(len(friendly)==10 and len(hostile)==1 and hostile[0]['slot']['sourceEntityId']==party['floor']['guardianCreatureId'] and hostile[0]['sourceMonsterId']==party['floor']['guardianAbilityProfileId'],'Wrong native actors')
            for index,(m,c) in enumerate(zip(arm_party['members'],friendly)):
                actor=m['character'];check(actor['equipment']==equipment.selected(m['owned'],costs),'Lost stronger owned gear')
                check(c['slot']['sourceEntityId']==ids[index] and c['slot']['partyNumber']==index//5+1 and c['level']==actor['level'] and c['baseAttributes']==actor['baseAttributes'],'Prepared slots/growth mismatch')
                check(equipment.items(c['equipment'])==equipment.items([i['data'] for i in actor['equipment']]),'Prepared gear mismatch')
                check(c['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in actor['essences']],'Prepared Essence mismatch');prepared_count+=1
        state=deepcopy(source['final']);old_tokens={t['owner']:t['towerTokens'] for t in state['tokens']};probe=state['probes'][0]
        state['tokens']=[dict(owner=id,towerTokens=old_tokens.get(id,0),level=o['point']['character']['level'],experience=o['runtime']['growth']['state']['experience']) for id,o in owners.items()]
        state['probes']=[dict(owner=id,level=o['point']['character']['level'],participating=id in ids,hypotheticalNextCompletedDungeonChest=probe['hypotheticalNextCompletedDungeonChest'],oldCompletedDecisionChest=probe['oldCompletedDecisionChest'],towerEquipmentSupplyProcessed=True) for id,o in owners.items()]
        check(native.normalized(p['initialState'])==native.normalized(state),'Retroactive newcomer reward or lost server state')
        admission=proof['eligibility'];check(admission['accepted'] and admission['requiredSlots']==10 and admission['status']=='Ready'
            and admission['participants']==[dict(owner=id,account=id,partySlot=i+1,party=i//5+1) for i,id in enumerate(ids)],'Native expanded admission/layout failed')
        check(admission['modelRatingAvailabilityAssumed'] and admission['assumedRawRating']==1 and not admission['liveAccountsVerified'] and not admission['probeRetained'],'Undeclared/live admission assumption')
        expected_history=[dict(attemptId=a['receipt']['attemptId'],floorNumber=a['floor'],requiredSlots=5,owners=[r['owner'] for r in a['receipt']['before']['probes'] if r['participating']]) for a in history]
        check(admission['historicalRosters']==expected_history,'Newcomer inserted into historical native rally')
        check(len(proof['essences'])==16 and {g['owner'] for g in proof['essences']}==set(owners),'Missing personal Essence gates')
        for g in proof['essences']:
            o=owners[g['owner']];level=o['point']['character']['level'];current=o['runtime']['growth']['state']['experience'];steps=[dict(level=l,experience=required(l)) for l in range(level,40)]
            deficit=sum(s['experience'] for s in steps)-current;xp_needed.append(deficit)
            check(g['level']==level and g['unlockedSlots']==max(1,min(10,level//10+1))==4 and g['ownedEssences']==len(o['runtime']['growth']['ownedEssences'])==4 and g['actualAccepted'],'Invalid current Essence qualification')
            check(g['fifthAtCurrentLevel']==dict(succeeded=False,message='Loadout contains a locked Essence slot.') and g['fifthAtHypotheticalLevel40']==dict(succeeded=False,message='A loadout can only use absorbed Essences.'),'Fifth slot/ownership bypass')
            check(g['experienceSteps']==steps and g['currentExperience']==current and g['experienceToLevel40']==deficit>0,'Wrong XP dependency')
            check(g['trackedUnboundItems']=={k:v for k,v in o['runtime']['sources']['state']['items'].items() if k.startswith('item.essence.')} and not g['historicalCreatureResonanceRecorded'] and not g['historicalEssenceDropsRecorded']
                and not g['unearnedLevelProbeRetained'] and g['newEssencesGranted']==0,'Fabricated historical Essence acquisition')
        check(row==dict(key=key,participants=10,newParticipants=5,owners=16,startsAt=party['startsAt'],levelMin=min(m['character']['level'] for m in members),levelMax=max(m['character']['level'] for m in members)),'Wrong expansion summary')
    check(seen==eligible and {r['key'] for r in result['held']}==held,'Missing server path')
    for row in result['held']:check(row==dict(key=row['key'],file=row['key']+'--continuation.json.gz',hash=io.sha(archive/(row['key']+'--continuation.json.gz')),newAttempts=0),'Changed held server')
    rules=io.read(output/'essence-source-rules.json');tables=io.read(api/'Data/world/creature-essence-loot-tables.json')['creatures']
    check(rules['slots']==[dict(level=l,slots=max(1,min(10,l//10+1))) for l in range(30,61)],'Slot formula changed')
    check(rules['tables']==[dict(creatureId=t['id'],**t['essenceLootTable']) for t in tables],'Essence source catalog drift')
    check(rules['gainPerFailedEligibleKill']==1 and rules['failedEligibleKillsToMaximumBonus']==12000 and rules['maximumRelativeDropChanceBonus']==.01
        and rules['baseDropChanceMultiplier']==3 and rules['spawnChanceMultiplier']==1.2,'Resonance/focus assumptions changed')
    check(set(io.read(output/'files.json'))==files,'Unexpected qualification output')
    return dict(status='VerifiedEarnedExpansionPrepared',parties=15,heldServers=17,ownersPreserved=512,newPartyMembers=75,preparedMembers=prepared_count,
        nativeEssenceGates=240,experienceToLevel40Range=[min(xp_needed),max(xp_needed)],newFights=0,newCombatSeeds=0,newEssencesGranted=0,measuredPlayerSamples=0)


def combat(q,output,owner):
    qualification=Path(q['qualification']);io.manifest(qualification,q['qualificationPin']);result=io.read(output/'result.json')
    check(result['version']==owner_module.VERSION and result['status']=='EarnedExpansionCombatComplete' and result['qualificationPin']==q['qualificationPin'],'Wrong combat admission')
    check(result['plan']==io.read(Path(q['fixtures'])/'tower-expansion.json') and result['reservedSeeds']==60 and result['measuredPlayerSamples']==result['earnedNewEquipment']==0 and not result['searchPerformed'],'Combat claim drift')
    ledger=io.read(owner/'seed-ledger.json');old,_=owner_module.exclusions(Path(ledger['preceding'][-1]['archive']));reserved=[s for p in q['panels'].values() for s in p]
    check(len(reserved)==len(set(reserved))==60 and reserved==ledger['reserved'] and not old.intersection(reserved)
        and ledger['exclusionUnionCount']==878799 and ledger['state']=='ReservedIncludingUnconsumed','Reused/lost seed reservations')
    keys={r['key'] for r in io.read(qualification/'result.json')['prepared']};check(set(q['panels'])==keys and all(len(v)==4 for v in q['panels'].values()),'Wrong server seed panels')
    options=native.settings(Path(q['apiRoot'])/'appsettings.json')['WorldTower'];files={'result.json'};seen=set();used=set();counts=Counter();groups=Counter();attempts=replays=0;rows=[]
    for row in result['journeys']:
        key=row['key'];check(key in keys and key not in seen,'Repeated/foreign continuation');seen.add(key)
        p=io.read(qualification/(key+'--expansion.json.gz'))['continuation'];file=key+'--continuation.json.gz';files.add(file);j=io.read(output/file)
        check(j['key']==key and j['initial']==p['initialState'],'Reset initial server');state=j['initial'];floor=p['party']['floor']['floorNumber'];ids={m['character']['id'] for m in p['party']['members']}
        history=p['historicalAttempts'];offset=sum(a['floor']==floor for a in history);success=False
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
            history.append(dict(floor=floor,battle=battle,receipt=receipt));state=after
        check(success or len(j['steps'])==4,'Premature stop');check(j['final']==state and j['stop']==('FloorFiveCleared' if success else 'FourAttemptCap') and j['highestCleared']==(floor if success else floor-1),'Wrong terminal server')
        check(j['owners']==[dict(o,runtime=wait(o['runtime'],native.ticks_at(state['at']))) for o in p['owners']],'Tower phase lost runtime/items/pity or granted personal activity')
        check(row==dict(key=key,floor=floor,success=success,attempts=len(j['steps']),startedAt=p['party']['startsAt'],endedAt=state['at']),'Wrong journey summary')
        groups[(floor,success)]+=1;rows.append(row)
    check(seen==keys and set(io.read(output/'files.json'))==files,'Missing/unexpected combat outputs')
    check(result['attempts']==attempts and result['controls']==attempts and result['replays']==replays==30 and result['fights']==attempts*2+replays<=150,'Wrong fight accounting')
    check(result['held']==io.read(qualification/'result.json')['held'] and result['newEssencesGranted']==0,'Changed held paths or granted Essence')
    return dict(status='VerifiedEarnedExpansionCombat',servers=15,heldServers=17,attempts=attempts,controls=attempts,replays=replays,fights=result['fights'],newCombatSeeds=60,
        reservedUnconsumedSeeds=60-len(used),exclusionUnionCount=878799,measuredPlayerSamples=0,
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
        and d['maximumFights']==(0 if prepare else 150) and d['maximumSeconds']==(300 if prepare else 900) and d['maximumNativeSeconds']==(240 if prepare else 840),'Changed envelope')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=d['maximumSeconds']+process['cleanupAllowanceSeconds'],'Unfinished/overbudget process')
    result=qualify(q,output) if prepare else combat(q,output,owner);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']))
    io.write(a.receipt,result);print(json.dumps({k:v for k,v in result.items() if k!='rows'},indent=2))


if __name__=='__main__':main()
