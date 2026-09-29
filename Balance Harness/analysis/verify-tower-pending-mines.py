"""Audit qualified access, paid Mines outcomes, native progression and retained pending-quest acquisition without combat."""
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
owner_module=module('pending_owner','run-tower-pending-mines.py');io=owner_module.io;check=io.check
fifth=module('pending_fifth','verify-tower-fifth.py');prior=fifth.prior;native=fifth.native;equipment=fifth.equipment;dt=fifth.dt
previous=module('pending_combat_reference','verify-tower-next-entry.py');loot_module=previous.loot_module

def sources(q):
    archive=Path(q['sourceArchive']);result=io.read(archive/'result.json')
    return {r['key']:io.read(archive/(r['key']+'--acquisition.json.gz')) for r in result['journeys']}

def preview(p,o,character=None):
    c=character or o['point']['character'];stock=o['runtime']['sources']['state']['items'];expected=dict(stock);expected['sigil_goblin_mines']-=1
    check(p['owner']==c['id'] and p['highestCleared']==4 and native.ticks_at(p['startsAt'])==prior.instant(o['runtime'])
          and p['reason']=='Enter' and p['selectedDungeon']=='goblin_mines' and not p['missingSlots'],'Unqualified current entry')
    check(p['policy']=='full-slot-ready-mines-first-next-entry-v1' and p['inventoryBefore']==stock and p['inventoryAfterHypotheticalEntry']==expected
          and stock['sigil_goblin_mines']>=1 and p['awardedEquipment']==p['actualEntries']==0,'Invented entry resources')
    for a in p['nativeAccess']:
        item='sigil_'+a['family'];check(a['costs']==[dict(itemId=item,amount=1)] and a['result']['canEnter']==(stock.get(item,0)>=1),'Native access/cost mismatch')
    check(p['prospectiveSupply']['itemBaseId']=='item.tower_supply.v1.floor_04' and p['prospectiveSupply']['band']==dict(rarity='Epic',quality='Fine',rank=3),'Wrong repeating supply band')
    prepared=p['prepared'];check(prepared['level']==c['level'] and equipment.items(prepared['equipment'])==equipment.items([e['data'] for e in c['equipment']]),'Wrong prepared actor/gear')
    es=o['runtime']['growth']['ownedEssences'];check(prepared['essences']==[dict(id=e['id'],essenceDefinitionId=e['definition'],level=e['level'],ascensionTier=e['ascensionTier']) for e in es],'Prepared owned Essence mismatch')

def qualify(q,output):
    old=sources(q);rows=io.read(output/'branches.json.gz');seen=set();actual={key+'--'+r['owner']['history']:(key,r) for key,p in old.items() for r in p['acquired'] if r['acquisition'] is None}
    for b in rows:
        check(b['key'] in actual and b['key'] not in seen,'Extra/duplicate pending state');seen.add(b['key']);key,r=actual[b['key']]
        check(b['server']==key and b['pending']==r and b['sourceFile']==key+'--acquisition.json.gz' and b['sourceHash']==io.sha(Path(q['sourceArchive'])/b['sourceFile']),'Source binding drift')
        o=r['owner'];check(o['point']['character']['level']==40 and len(o['runtime']['growth']['ownedEssences'])==4,'Unearned entry growth')
        check(b['mastery']==next((m['level'] for m in o['runtime']['mastery'] if m['dungeonDefinitionId']=='goblin_mines'),0),'Invented mastery')
        preview(b['native'],o);control=b['control'];check(control==dict(o['point']['character'],name=o['history']+'--pending-mines-epic-control',equipment=control['equipment']),'Changed supplied growth')
        check(all(e['data']['rarity']=='Epic' and e['data']['state']['rank']==3 and e['data']['state']['tier']==1 for e in control['equipment']),'Wrong control band')
        preview(b['controlNative'],o,control)
    check(seen==set(actual) and len(rows)==97,'Incomplete pending population')
    result=io.read(output/'result.json');check(result==dict(version=owner_module.VERSION,status='PendingMinesQualified',plan=io.read(Path(q['fixtures'])/'tower-pending-mines.json'),
        owners=97,preparations=194,newFights=0,newSeeds=0,heldCompleted=143,heldServers=17,measuredPlayerSamples=0),'Invalid qualification summary')
    check(set(io.read(output/'files.json'))=={'result.json','branches.json.gz'},'Qualification output drift')
    return dict(status='VerifiedPendingMinesPreparation',owners=97,preparations=194,newFights=0,newSeeds=0)

def quest(b,p,run):
    old=b['pending'];r=p['acquisition'];after=p['beforeQuest'];c=r['credit'];receipt=r['acquisition'];q=r['quest'];completed=run['status']=='Completed'
    if not completed:
        check(r==dict(old,owner=after),'Failed entry granted quest/Essence or lost pending state');return 0
    check(c==dict(old['credit'],runFile=p['runFile'],runHash=p['_runHash'],minesCompletedAt=p['receipt']['ended'],claimAt=p['receipt']['ended']),'New run backdated or unlinked')
    check(q['characterId']==old['quest']['characterId'] and q['questId']==old['quest']['questId'] and q['status']=='Completed'
          and dt(q['completedAt'])==dt(q['rewardsGrantedAt'])==dt(c['claimAt']),'Unpaid or mistimed quest grant')
    for o in old['quest']['objectives']:
        a=next(a for a in q['objectives'] if a['objectiveKey']==o['objectiveKey'])
        if o['objectiveKey']=='break_the_goblin_gate':
            check(a['currentAmount']==a['requiredAmount']==1 and dt(a['completedAt'])==dt(c['claimAt']),'Wrong new Mines credit')
        else:check(a==o,'Replayed prior forest/level objective')
    id=after['point']['character']['id'];choice={'guardian':'thornback_boar','restorer':'forest_spirit','striker':'glade_panther','controller':'hollow_stag'}[after['point']['recipe']]
    check(receipt['owner']==id and receipt['quest']==q['questId'] and receipt['token']=='item.essence_token.old_forest' and receipt['option']==choice
          and receipt['item']=='item.essence.'+choice and receipt['definition']=='essence.'+choice and receipt['at']==c['claimAt'] and receipt['evidenceHash']==fifth.digest(c),'Wrong native grant receipt')
    ids=[receipt[k] for k in ['tokenInstance','unboundInstance','essenceId']];check(len(set(ids))==3 and all(v!='00000000-0000-0000-0000-000000000000' for v in ids),'Lost actual identities')
    new=dict(id=receipt['essenceId'],definition=receipt['definition'],level=1,currentXp=0,ascensionTier=0);g=after['runtime']['growth'];a=r['owner']
    check(new['id'] not in {e['id'] for e in g['ownedEssences']} and new['definition'] not in {e['definition'] for e in g['ownedEssences']},'Duplicate fifth')
    check(a['runtime']['growth']==dict(g,ownedEssences=g['ownedEssences']+[new],state=dict(g['state'],essences=g['state']['essences']+[new]),attuned=5,acquisitions=[receipt]),'Fifth received retrospective training/XP')
    check(r['native']==dict(tokenConsumed=True,duplicateOpenRejected=True,unboundConsumed=True,duplicateAbsorptionRejected=True,lockedAt39=True,acceptedAt40=True,
          absorptionNotifications=1,newEssenceLevel=1,newEssenceXp=0,savedOwnedIds=[e['id'] for e in g['ownedEssences']]+[new['id']]),'Native ownership/retry guard drift')
    expected=deepcopy(after['runtime']);expected['growth']=a['runtime']['growth'];expected['dungeonEvents'].append(dict(at=c['claimAt'],kind='EssenceAbsorbed',amount=1,enemyCount=None,creature=None))
    # Every run observes and claims before its terminal interaction. The selected kill daily and current weekly do not absorb.
    check(a['runtime']==expected,'Unmodeled absorption activity/source change')
    character=dict(after['point']['character'],essences=after['point']['character']['essences']+[dict(definitionId=new['definition'],level=1,ascensionTier=0,isEvolved=False,ownedId=new['id'])])
    check(a==dict(after,point=dict(after['point'],character=character),runtime=expected),'Lost retained gear/source/pity/clock')
    return 1

def combat(q,output,owner):
    qualification=Path(q['qualification']);io.manifest(qualification,q['qualificationPin']);qualify(q,qualification)
    branches={b['key']:b for b in io.read(qualification/'branches.json.gz')};result=io.read(output/'result.json');files={'result.json'};seen=set();used=set();fights=prepared=0;totals=Counter();updated={};times=[]
    check(result['version']==owner_module.VERSION and result['status']=='PendingMinesComplete' and result['plan']==io.read(Path(q['fixtures'])/'tower-pending-mines.json'),'Wrong paid wave')
    api=Path(q['apiRoot']);costs=io.read(Path(q['scoringRules']))['scores'];select=lambda v:equipment.selected(v,costs)
    score=lambda v:sum(equipment.single(value)*costs[k] for e in select(v) for k,value in e['data']['stats'].items())
    auditor=loot_module.LootAuditor(api)
    ledger=io.read(owner/'seed-ledger.json');latest=Path(ledger['preceding'][-1]['archive']);excluded=owner_module.exclusions(latest)
    reserved=[s for panel in q['panels'].values() for s in [panel['layoutSeed'],*panel['roomSeeds']]]
    check(len(reserved)==len(set(reserved))==6305 and reserved==ledger['reserved'] and not set(reserved)&excluded and ledger['exclusionUnionCount']==885104,'Reservation loss/reuse')
    def battle(file,panel,c,mastery):
        nonlocal fights,prepared
        files.add(file);run=io.read(output/file);previous.run_audit(run,panel,mastery);fights+=len(run['battles']);used.add(panel['layoutSeed']);used.update(b['seed'] for b in run['battles'])
        for b in run['battles']:
            players=[a for a in b['preparedParticipants'] if a['slot']['side']=='Friendly'];check(len(players)==1,'Non-solo entry');a=players[0]
            check(a['slot']['sourceEntityId']==c['id'] and a['level']==c['level'] and equipment.items(a['equipment'])==equipment.items([e['data'] for e in c['equipment']]),'Changed battle owner/gear')
            check(a['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in c['essences']],'Unearned room Essence');prepared+=1
        return run
    for row in result['results']:
        key=row['key'];check(key in branches and key not in seen,'Foreign/repeated paid branch');seen.add(key);b=branches[key];o=b['pending']['owner'];panel=q['panels'][key]
        files.add(row['progressFile']);p=io.read(output/row['progressFile']);check(p['before']==o['runtime'] and p['entry']==o['point']['character'] and p['native']==b['native'] and p['costs']=={'sigil_goblin_mines':1},'Entry parity/debit drift')
        run=battle(p['runFile'],panel,p['entry'],b['mastery']);check(battle(key+'--replay.json.gz',panel,p['entry'],b['mastery'])==run,'Non-identical full-run replay')
        control=battle(key+'--control.json.gz',panel,b['control'],b['mastery']);check(row['status']==run['status'] and row['combatSeconds']==run['combatSeconds'] and row['controlStatus']==control['status'],'Outcome summary mismatch')
        # Restore the actual selected persisted daily rather than a potentially stale overview pointer.
        gp=deepcopy(p);view=gp['before']['overview'];instances=gp['before']['sources']['instances'];view['dailyProphecies']=[next(i for i in instances if i['id']==x['id']) for x in view['dailyProphecies']];view['greaterProphecy']=next(i for i in instances if i['id']==view['greaterProphecy']['id'])
        check(previous.growth.audit(api,Path(q['fixtures']),gp,run)==b['mastery'],'Native progression mismatch')
        loot=p['ordinary'];auditor.audit(loot,p['entry']['id'],run,o['blueprintProgress'],b['mastery'],resolved_routes=True)
        owned=o['point']['owned']+loot['equipment'];supply=p['supply'];completed=run['status']=='Completed';chest='item.tower_supply.v1.floor_04';pending=supply['pending']
        check(supply['claimed']==({chest:1} if completed else {}) and len(pending)==int(completed) and supply['retryChecks']==2,'Wrong native supply claim')
        if completed:
            check(pending[0]['id']==loot_module.identity(['tower-equipment-supply',p['entry']['id'].replace('-',''),loot['runId'].replace('-','')])
                  and pending[0]['itemId']==chest and pending[0]['quantity']==1 and pending[0]['source']=='tower-equipment-supply','Repeated supply decision')
        targets=p['candidateTargets'];check(len(targets)==(7 if completed else 0),'Missing candidate supplies')
        order=io.read(Path(q['fixtures'])/'tower-bootstrap.json')['purchaseOrder'];check(not completed or ['MainHand' if t['equipmentType'] in ['OneHanded','TwoHanded'] else t['equipmentType'] for t in targets]==order,'Changed purchase order')
        for t in targets:check(t['rarity']=='Epic' and t['quality']=='Fine' and t['state']['rank']==3 and t['state']['tier']==1 and t['state']['ownership']['ownerId']==p['entry']['id'],'Changed repeating supply curve')
        chosen=next((t for t in targets if score(owned+[t])>score(owned)),None);check(supply['opened']==(chosen is not None),'Non-beneficial supply opening')
        if chosen:
            expected=deepcopy(chosen);expected['state']['id']=loot_module.identity(['tower-next-entry-award-v1',p['entry']['id'],loot['runId'],chest]);expected['state']['provenance']['awardId']=loot['runId']
            check(supply['equipment']==expected and supply['choice']==chosen['state']['definitionId'],'Wrong owned supply item');owned.append(expected)
        else:check(supply['equipment'] is None and supply['choice'] is None,'Unopened chest became gear')
        check(p['owned']==owned and len(equipment.items(owned))==len(owned) and p['character']['equipment']==select(owned),'Lost, lent or weakened owned gear')
        expected=deepcopy(p['afterProgress']);stock=expected['sources']['state']['items']
        for item,n in list(loot['blueprints'].items())+list(supply['claimed'].items()):stock[item]=stock.get(item,0)+n
        if supply['opened']:stock[chest]-=1
        check(p['after']==expected,'Resource/XP replay after claims')
        after=dict(o,point=dict(o['point'],character=p['character'],owned=owned,availableAt=p['receipt']['ended']),runtime=expected,blueprintProgress=loot['after']);check(p['beforeQuest']==after,'Unfunded pre-quest state')
        check(p['assemblyAfterEntry']==p['additionalEntries']==p['measuredPlayerSamples']==0,'Unmodeled extra activity')
        p['_runHash']=io.sha(output/p['runFile']);grant=quest(b,p,run);check(row['newFifth']==bool(grant) and row['newEquipment']==len(owned)-len(o['point']['owned']),'Wrong grant summary')
        acquired=p['acquisition'];updated[key]=acquired;preview(p['next'],acquired['owner'])
        totals['grants']+=grant;totals['successes']+=int(completed);totals['failures']+=int(not completed);totals['controlSuccesses']+=int(control['status']=='Completed')
        totals['ordinaryEquipment']+=len(loot['equipment']);totals['blueprints']+=sum(loot['blueprints'].values());totals['openedSupplies']+=int(supply['opened']);totals['retainedSupplies']+=int(completed and not supply['opened'])
        totals['dungeonXp']+=p['receipt']['appliedExperience'];totals['lostXp']+=p['receipt']['lostExperience'];totals['claims']+=len(p['afterProgress']['sources']['claims'])-len(p['before']['sources']['claims']);times.append(run['combatSeconds'])
    old=sources(q);rows=[];all_ids=set()
    for key,before in old.items():
        file=key+'--continuation.json.gz';files.add(file);a=io.read(output/file);check(a['sourceFile']==key+'--acquisition.json.gz' and a['sourceHash']==io.sha(Path(q['sourceArchive'])/a['sourceFile']),'Lost server source pin')
        for field in ['server','historicalAttempts','personalRefreshBefore','nextPersonalRefreshBefore']:check(a[field]==before[field],'Rewritten Tower failures/rewards')
        acquired=[updated.get(key+'--'+r['owner']['history'],r) for r in before['acquired']];check(a['acquired']==acquired,'Lost/repeated completed quest')
        at=max(prior.instant(r['owner']['runtime']) for r in acquired);owners=[dict(r['owner'],runtime=prior.wait(r['owner']['runtime'],at)) for r in acquired];check(a['owners']==owners,'Unearned waiting activity')
        by_id={o['point']['character']['id']:o['point'] for o in owners};members=[by_id[m['character']['id']] for m in before['party']['members']]
        check(a['party']==dict(before['party'],members=members,startsAt=a['party']['startsAt']) and native.ticks_at(a['party']['startsAt'])==at,'Changed Tower roster/clock')
        players=[x for x in a['prepared'] if x['slot']['side']=='Friendly'];check(len(players)==10 and len(a['prepared'])==11,'Incomplete production Tower preparation')
        for i,(m,p) in enumerate(zip(members,players)):
            c=m['character'];check(p['slot']['sourceEntityId']==c['id'] and p['slot']['partyNumber']==i//5+1 and p['level']==c['level'] and equipment.items(p['equipment'])==equipment.items([e['data'] for e in c['equipment']]),'Wrong prepared Tower gear/owner')
            check(p['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in c['essences']],'Prepared Tower Essence mismatch')
        for r in acquired:
            if r['acquisition']:
                eid=r['acquisition']['essenceId'];check(eid not in all_ids,'Cloned fifth across alternatives');all_ids.add(eid)
        rows.append(dict(key=key,newFifths=sum(r['acquisition'] is not None for r in acquired)-sum(r['acquisition'] is not None for r in before['acquired']),pending=sum(r['acquisition'] is None for r in acquired),endsAt=a['party']['startsAt']))
    check(result['servers']==rows and len(seen)==len(branches)==result['attempts']==result['controls']==result['replays']==97,'Incomplete wave')
    check(fights==result['fights']<=18624 and result['reservedSeeds']==6305 and set(result['usedSeeds'])==used<=set(reserved) and set(io.read(output/'files.json'))==files,'Counts/seeds/manifest drift')
    check(result['held']==io.read(Path(q['sourceArchive'])/'result.json')['held'],'Lost held servers')
    for h in result['held']:check(io.sha(Path(q['returnArchive'])/h['file'])==h['hash'],'Changed held state')
    check(result['measuredPlayerSamples']==result['newTowerFights']==0 and not result['searchPerformed'],'Unadmitted time/Tower/search claim')
    return dict(status='VerifiedPendingMinesCombat',**totals,owners=97,controls=97,replays=97,fights=fights,preparedRooms=prepared,preparedTowerMembers=150,heldCompletedQuests=143,heldServers=17,
        reservedSeeds=6305,usedSeeds=len(used),unusedSeeds=6305-len(used),exclusionUnionCount=885104,engineSecondsRange=[min(times),max(times)],measuredPlayerSamples=0)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');output=Path(q['output']);mode=q['mode']
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'Fresh external receipt required')
    check(d['version']==owner_module.VERSION and d['requestPin']==io.sha(owner/'request.json') and d['mode']==mode,'Declaration drift')
    for file,pin in q['inputHashes'].items():check(io.sha(Path(file))==pin,'Frozen drift: '+file)
    for key,(_,pin) in owner_module.ARCHIVES.items():io.manifest(Path(q[key]),pin)
    io.manifest(output,a.manifest_pin);check(sum(f.stat().st_size for f in output.iterdir())<=d['maximumBytes']==256*1048576,'Output cap')
    check(d['maximumSeconds']==900 and d['maximumNativeSeconds']==840 and d['maximumFights']==(18624 if mode=='combat' else 0) and d['newSeeds']==(6305 if mode=='combat' else 0) and d['retries']==0 and d['maximumOwners']==97,'Changed envelope')
    owner_module.exclusions(Path(d['latestSeedLedger']));check(d['exclusionUnionCount']==(885104 if mode=='combat' else 878799),'Lost exclusion values')
    proc=io.read(owner/'process.json');check(proc['exitCode']==0 and not proc['timedOut'] and proc['activeProcesses']==0 and proc['seconds']<=901,'Incomplete process')
    result=qualify(q,output) if mode=='prepare' else combat(q,output,owner);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']));io.write(a.receipt,result);print(json.dumps(result,indent=2))

if __name__=='__main__':main()
