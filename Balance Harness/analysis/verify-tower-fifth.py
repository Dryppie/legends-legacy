"""Independent chronological quest-credit, ownership and production-preparation audit. No combat or native execution."""
import argparse
from collections import Counter
from copy import deepcopy
from datetime import timedelta
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
sys.dont_write_bytecode=True
def module(name,file):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
owner_module=module('fifth_owner','run-tower-fifth.py');io=owner_module.io;check=io.check
level40=module('fifth_growth','verify-tower-level40.py');prior=level40.prior;native=prior.native;equipment=prior.equipment;dt=level40.dt

def digest(value):
    text=json.dumps(value,sort_keys=True,separators=(',',':'),ensure_ascii=True)
    for char in ['+','<','>','&',"'"]:text=text.replace(char,'\\u%04X'%ord(char))
    return hashlib.sha256(text.encode()).hexdigest()

def credit(q,key,b,g,c):
    archive=Path(q['historyArchive']);file=archive/(b['history']+'--history.json');h=io.read(file)
    gate=h['summary']['questAt'];activated=prior.EPOCH+gate*100000000
    start=prior.instant(g['owner']['runtime'])-(g['until']-g['from'])*100000000
    check(gate>0 and gate<=g['from'] and activated<start and h['summary']['policy']=='earned-progression','Unqualified quest prerequisite')
    eligible=[]
    for s in h['steps']:
        if s['encounter']>g['from'] or s['dungeon']!='goblin_mines' or s['status']!='Completed':continue
        e=next(e for e in h['progression']['entries'] if e['ordinal']==s['ordinal']);r=e['receipt'];run=io.read(archive/s['file'])
        check(activated<=native.ticks_at(r['started'])<=native.ticks_at(r['ended'])<=start,'Future or pre-activation completion')
        check(s['before']['id']==b['point']['character']['id'] and s['sigilsBefore']-s['sigilsAfter']==1 and s['sigilsAfter']>=0,'Foreign/unpaid completion')
        check(run['status']=='Completed' and run['dungeon']=='goblin_mines' and run['completionCallbacks']==run['sigilsConsumed']==1
              and run['entryItems']=={'sigil_goblin_mines':1} and r['appliedExperience']>0,'Non-native success/cost')
        eligible.append((s['file'],io.sha(archive/s['file']),r['ended']))
    selected=eligible[0] if eligible else (None,None,None)
    wave=io.read(Path(q['waveArchive'])/'result.json');w=next(r for r in wave['results'] if r['server']==key and r['history']==b['history'])
    if not eligible and w['actualEntries'] and w['status']=='Completed':
        check(io.read(Path(q['waveArchive'])/w['progressFile'])['native']['selectedDungeon']!='goblin_mines','Omitted paid later Mines completion')
    wins=[]
    for ordinal in range(g['from']+1,g['until']+1):
        if b['point']['outcome']=='perfect' or ordinal%5:wins.append(start+(ordinal-g['from']-1)*100000000)
        if len(wins)==6:break
    check(c['owner']==b['point']['character']['id'] and c['history']==b['history'] and c['questEncounter']==gate
          and native.ticks_at(c['activatedAt'])==activated and c['historyHash']==io.sha(file),'Quest activation provenance')
    check((c['runFile'],c['runHash'],c['minesCompletedAt'])==selected,'Invented/missed Mines prefix credit')
    check([native.ticks_at(t) for t in c['forestWins']]==wins and native.ticks_at(c['claimAt'])==prior.instant(b['runtime']),'Unearned forest events/interaction clock')
    check(c['initialStateAssumption']=='Roots Remember activates at the retained Between Day and Night gate and is unclaimed; historical quest inventory/status was not recorded. Claim is delayed until this level-40 interaction; no earlier fifth training is credited.','Missing quest-state assumption')
    return bool(eligible)

def personal(q,key,b,g,r,choice):
    c=r['credit'];qualified=credit(q,key,b,g,c);a=r['owner'];receipt=r['acquisition'];quest=r['quest'];actor=b['point']['character'];id=actor['id']
    check(quest['characterId']==id and quest['questId']=='quest.shenic.roots_remember' and quest['definitionVersion']==4,'Foreign quest')
    objectives={o['objectiveKey']:o for o in quest['objectives']}
    for name,target,amount,at in [('cross_old_forest',6,6,c['forestWins'][-1]),('break_the_goblin_gate',1,int(qualified),c['minesCompletedAt']),('reach_level_30',30,30,c['activatedAt'])]:
        o=objectives[name];check(o['characterId']==id and o['questId']==quest['questId'] and o['requiredAmount']==target and o['currentAmount']==amount
            and (dt(o['completedAt']) if o['completedAt'] else None)==(dt(at) if at else None),'Incorrect native quest objective')
    if not qualified:
        check(receipt is None and a==b and quest['status']=='Active' and quest['rewardsGrantedAt'] is None
            and r['native']==dict(pendingPaidMines=True,prematureTurnInRejected=True),'Unfunded grant or mutation of held owner')
        return dict(grants=0,pending=1,claims=0,prophecyXp=0,newDays=0)
    check(quest['status']=='Completed' and dt(quest['rewardsGrantedAt'])==dt(c['claimAt']) and dt(quest['completedAt'])==dt(c['claimAt']),'Incorrect claim clock')
    check(receipt['owner']==id and receipt['quest']==quest['questId'] and receipt['token']=='item.essence_token.old_forest'
          and receipt['option']==choice and receipt['definition']=='essence.'+choice and dt(receipt['at'])==dt(c['claimAt']) and receipt['evidenceHash']==digest(c),'Grant identity/evidence mismatch')
    items=io.read(Path(q['apiRoot'])/'Data/items/items.json')
    item=next(i for i in (items if isinstance(items,list) else items['items']) if i['id']==receipt['item'])
    check(item['itemType']=='Essence' and item.get('essenceDefinitionId',item['id'].removeprefix('item.'))==receipt['definition'],'Wrong unbound Essence item')
    ids=[receipt[k] for k in ['tokenInstance','unboundInstance','essenceId']]
    check(len(set(ids))==3 and all(x!='00000000-0000-0000-0000-000000000000' for x in ids),'Lost native item/Essence identity')
    new=dict(id=receipt['essenceId'],definition=receipt['definition'],level=1,currentXp=0,ascensionTier=0)
    bg,ag=b['runtime']['growth'],a['runtime']['growth']
    check(new['id'] not in {e['id'] for e in bg['ownedEssences']} and new['definition'] not in {e['definition'] for e in bg['ownedEssences']},'Duplicate owned Essence')
    check(ag['ownedEssences']==bg['ownedEssences']+[new] and ag['attuned']==5 and ag['acquisitions']==[receipt],'Unearned training or replaced owned Essence')
    check(r['native']==dict(tokenConsumed=True,duplicateOpenRejected=True,unboundConsumed=True,duplicateAbsorptionRejected=True,lockedAt39=True,acceptedAt40=True,
                           absorptionNotifications=1,newEssenceLevel=1,newEssenceXp=0,savedOwnedIds=[e['id'] for e in ag['ownedEssences']]),'Native source/loadout guards')
    before,after=b['runtime'],a['runtime'];f=level40.Forward(Path(q['apiRoot']),Path(q['fixtures']),before,after)
    f.es.append(new);f.attuned=5;f.observe()
    # The original forward auditor predates the absorption event; apply the production one-count objective here.
    for p,period,length in [(f.selected,f.observed,1),(f.weekly,f.week_start,7)]:
        if p and p['status']=='Accepted' and period<=f.now<period+timedelta(days=length) and p['objective']=='AbsorbEssence':
            p['progress']=min(p['target'],p['progress']+1)
            if p['progress']==p['target']:p['status']='Completed'
    f.claim()
    check(ag==dict(bg,state=f.growth(),ownedEssences=f.es,attuned=5,acquisitions=[receipt]),'Wrong growth after absorption/claims')
    check(a['runtime']==dict(before,growth=ag,sources=after['sources'],days=after['days'],dungeonEvents=after['dungeonEvents'],observedDay=after['observedDay'],overview=after['overview']),'Changed clock/mastery')
    check(after['dungeonEvents']==before['dungeonEvents']+[dict(at=receipt['at'],kind='EssenceAbsorbed',amount=1,enemyCount=None,creature=None)],'Missing or replayed progression event')
    check(after['days'][:len(before['days'])]==before['days'] and len(after['days'])==f.dayindex and dt(after['observedDay'])==f.observed,'Lost/future offer history')
    bs,src=before['sources'],after['sources'];claims=src['claims'][len(bs['claims']):]
    check(src['claims'][:len(bs['claims'])]==bs['claims'] and [dict(c,at=dt(c['at']),periodStart=dt(c['periodStart'])) for c in claims]==f.claims,'Unfunded or repeated prophecy claims')
    check(src['state']==dict(bs['state'],items=dict(f.items),cinders=f.cinders,soulstones=f.soulstones,fateEcho=f.fate)
          and src['duplicateClaimsRejected']==len(src['claims']) and src['resourcesReconciled']==bs['resourcesReconciled'],'Source stock/retry corruption')
    # Observe may create the current day's zero-use bookkeeping after waiting alignment; it is not a reroll.
    days=after['days'][len(before['days']):]
    check(src['rerolls'][:len(bs['rerolls'])]==bs['rerolls'] and len(src['rerolls'])==len(bs['rerolls'])+len(days),'Historical reroll ledger changed')
    for row,day in zip(src['rerolls'][len(bs['rerolls']):],days):
        start=dt(day['at']).replace(hour=0,minute=0,second=0,microsecond=0)
        check(row['playerId']==row['characterId']==id and dt(row['periodStart'])==start and dt(row['periodEnd'])==start+timedelta(days=1)
              and row['rerollsUsed']==row['freeRerollsUsed']==row['paidRerollsUsed']==row['fateEchoSpent']==0
              and json.loads(row['shownDefinitionIdsJson'])==[p['definition'] for p in day['daily']]
              and dt(row['createdAt'])==dt(row['updatedAt'])==dt(day['at']),'Unfunded reroll or wrong new-day offers')
    old_instances={i['id']:i for i in bs['instances']};new_instances={i['id']:i for i in src['instances']}
    check(len(new_instances)==len(src['instances']) and all(new_instances.get(k)==v for k,v in old_instances.items()),'Lost/changed historical prophecy instance')
    expected_offers=[]
    for day in days:
        start=dt(day['at']).replace(hour=0,minute=0,second=0,microsecond=0);week=start-timedelta(days=start.weekday())
        expected_offers.extend((p,'Daily',start,start+timedelta(days=1),
            'Offered' if day['selected'] is None else 'Accepted' if p['definition']==day['selected'] else 'Declined') for p in day['daily'])
        if not any(i['scope']=='Weekly' and dt(i['periodStart'])==week for i in bs['instances']):
            expected_offers.append((day['weekly'],'Weekly',week,week+timedelta(days=7),'Accepted'))
    added=[i for i in src['instances'] if i['id'] not in old_instances]
    check(len(added)==len(expected_offers),'Extra/missing new prophecy instance')
    for p,scope,start,end,status in expected_offers:
        i=next(i for i in added if i['prophecyDefinitionId']==p['definition'] and i['scope']==scope and dt(i['periodStart'])==start)
        check(i['playerId']==i['characterId']==id and dt(i['periodEnd'])==end and i['targetValue']==p['target']
              and i['currentValue']==p['progress']==0 and i['status']==status and json.loads(i['rewardSnapshotJson'])==p['reward']
              and i['progressJson']=='{}' and i['claimedAt'] is None,'Unfunded prophecy progress/reward')
    weeks={w['periodStart']:w for w in src['weeks']}
    check(len(weeks)==len(src['weeks']) and all(weeks.get(w['periodStart'])==w for w in bs['weeks']),'Historical weekly ledger changed')
    for w in src['weeks']:
        check(w['characterId']==w['playerId']==id and w['propheticFavor']==f.favor[dt(w['periodStart'])]
              and all(w['milestone'+str(n)+'Claimed']==((dt(w['periodStart']),n) in f.milestones) for n in [3,5,7]),'Unfunded weekly favor/milestone')
    expected=dict(actor,level=f.level,essences=actor['essences']+[dict(definitionId=new['definition'],level=1,ascensionTier=0,isEvolved=False,ownedId=new['id'])])
    check(a==dict(b,point=dict(b['point'],character=expected),runtime=after),'Gear, identity, pity, origin or personal prefix changed')
    return dict(grants=1,pending=0,claims=len(claims),prophecyXp=sum(c['reward']['characterExperience'] for c in claims),newDays=len(after['days'])-len(before['days']))

def qualify(q,output):
    result=io.read(output/'result.json');plan=io.read(Path(q['fixtures'])/'tower-fifth-essence.json');archive=Path(q['growthArchive'])
    oldresult=io.read(archive/'result.json');files={'result.json'};totals=Counter();rows=[];families=Counter();identities=set()
    check(result['version']==owner_module.VERSION and result['status']=='RetainedFifthEssencesQualified' and result['plan']==plan,'Wrong qualification')
    check(plan['choices']==dict(guardian='thornback_boar',restorer='forest_spirit',striker='glade_panther',controller='hollow_stag'),'Changed fixed choices')
    quest=io.read(Path(q['apiRoot'])/'Data/quests/region-01/roots-remember.v4.json')
    check(quest['availability']==dict(minimumLevel=25,completedQuestIds=['quest.shenic.between_day_and_night']) and quest['objectiveMode']=='All'
          and quest['rewards']==[dict(key='old_forest_essence_token',type='Item',itemBaseId='item.essence_token.old_forest',quantity=1)],'Changed native source')
    for oldrow in oldresult['journeys']:
        key=oldrow['key'];old=io.read(archive/(key+'--growth.json.gz'));file=key+'--acquisition.json.gz';files.add(file);p=io.read(output/file)
        check(p['key']==key and p['sourceFile']==key+'--growth.json.gz' and p['sourceHash']==io.sha(archive/p['sourceFile']),'Lost source binding')
        for field in ['server','historicalAttempts','personalRefreshBefore','nextPersonalRefreshBefore']:check(p[field]==old[field],'Rewritten server/failure history')
        check(len(p['acquired'])==len(p['owners'])==len(old['owners'])==16,'Incomplete ownership')
        granted=0
        for b,g,r,a in zip(old['owners'],old['growth'],p['acquired'],p['owners']):
            check(a==r['owner'],'Discarded retained ownership')
            values=personal(q,key,b,g,r,plan['choices'][b['point']['recipe']]);totals.update(values);granted+=values['grants'];totals['retainedEquipment']+=len(b['point']['owned'])
            if r['acquisition']:
                eid=r['acquisition']['essenceId'];check(eid not in identities,'Cloned absorption across alternative states');identities.add(eid);families[r['acquisition']['definition']]+=1
        by_id={o['point']['character']['id']:o['point'] for o in p['owners']};members=[by_id[m['character']['id']] for m in old['party']['members']]
        check(p['party']==dict(old['party'],members=members),'Changed party layout/clock/guardian')
        actors=[a for a in p['prepared'] if a['slot']['side']=='Friendly'];check(len(actors)==10 and len(p['prepared'])==11,'Wrong preparation count')
        for i,(m,a) in enumerate(zip(members,actors)):
            c=m['character'];check(a['slot']['sourceEntityId']==c['id'] and a['slot']['partyNumber']==i//5+1 and a['level']==c['level'] and a['baseAttributes']==c['baseAttributes'],'Prepared actor identity')
            check(equipment.items(a['equipment'])==equipment.items([e['data'] for e in c['equipment']]),'Prepared equipment drift')
            check(a['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in c['essences']],'Prepared Essence mismatch')
        rows.append(dict(key=key,granted=granted,pending=16-granted,fifthInParty=sum(len(m['character']['essences'])==5 for m in members)))
    check(result['journeys']==rows and result['grants']==totals['grants'] and result['pending']==totals['pending'] and totals['grants']+totals['pending']==240,'Incorrect summary')
    check(set(io.read(output/'files.json'))==files and len(rows)==15,'Extra/missing output')
    check(result['held']==oldresult['held'] and len(result['held'])==17,'Lost held servers')
    for h in result['held']:check(io.sha(Path(q['returnArchive'])/h['file'])==h['hash'],'Held owner drift')
    check(result['newFights']==result['newSeeds']==result['newIdleEncounters']==result['measuredPlayerSamples']==0 and not result['searchPerformed'] and not result['combatAdmission'],'Unadmitted activity/claim')
    return dict(status='VerifiedRetainedFifthEssences',**totals,choices=dict(families),preparedMembers=150,partyFifthCount=sum(r['fifthInParty'] for r in rows),
                heldServers=17,ownersPreserved=512,newFights=0,newSeeds=0,measuredPlayerSamples=0,combatAdmission=False)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');output=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'Fresh external audit receipt required')
    check(d['version']==owner_module.VERSION and d['requestPin']==io.sha(owner/'request.json'),'Declaration drift')
    for file,pin in q['inputHashes'].items():check(io.sha(Path(file))==pin,'Frozen drift: '+file)
    for key,(_,pin) in owner_module.ARCHIVES.items():io.manifest(Path(q[key]),pin)
    owner_module.exclusions(Path(d['latestSeedLedger']));check(d['latestSeedLedgerPin']==owner_module.LEDGER_PIN and d['exclusionUnionCount']==878799,'Lost seeds')
    io.manifest(output,a.manifest_pin);check(sum(f.stat().st_size for f in output.iterdir())<=d['maximumBytes']==256*1048576,'Output cap')
    check(d['maximumSeconds']==900 and d['maximumNativeSeconds']==840 and d['maximumFights']==d['newSeeds']==d['retries']==0 and d['maximumOwners']==240,'Changed cap')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=900+process['cleanupAllowanceSeconds'],'Incomplete process')
    result=qualify(q,output);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']));io.write(a.receipt,result);print(json.dumps(result,indent=2))

if __name__=='__main__':main()
