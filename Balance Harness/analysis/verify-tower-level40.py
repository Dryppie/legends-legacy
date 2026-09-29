"""Independent forward XP, offers, training, reward conservation and prospective quest-source audit; no combat."""
import argparse
from collections import Counter
from copy import deepcopy
from datetime import datetime,timedelta
import importlib.util
import json
from pathlib import Path
import sys
sys.dont_write_bytecode=True
def module(name,file):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
owner_module=module('level40_owner','run-tower-level40.py');io=owner_module.io;check=io.check
prior=module('level40_prior','verify-tower-return.py');native=prior.native;equipment=prior.equipment
journey=module('level40_journey','audit-growing-activity.py');dt=journey.dt

class Forward(journey.JourneyAudit):
    def __init__(self,api,fixtures,before,after):
        # Initialize production formula/offer references, then rehydrate the complete active source state.
        fake=dict(progression=dict(days=after['days']),final=dict(id=before['growth']['owner'],essences=[]),summary=dict(policy='earned-progression'),windows=[dict(area='region_01_area_08')])
        super().__init__(api,fixtures,fake,None)
        g=before['growth'];s=before['sources'];state=g['state'];stock=s['state']
        self.level=state['level'];self.xp=state['experience'];self.idle=state['idleExperience'];self.dungeon=g['dungeonExperience'];self.prophecy=state['prophecyExperience']
        self.es=deepcopy(g['ownedEssences']);self.attuned=g['attuned'];self.n=before['idleSeconds']//10;self.start_n=self.n;self.start=dt_from_ticks(prior.instant(before))
        self.dayindex=len(before['days']);self.observed=dt(before['observedDay']);self.items=Counter(stock['items'])
        self.cinders=stock['cinders'];self.soulstones=stock['soulstones'];self.fate=stock['fateEcho'];self.last_essence=g['lastEssenceExperience']
        def offer(i):
            if i is None:return None
            check(i['progressJson']=='{}','Unmodeled restored objective state')
            return dict(definition=i['prophecyDefinitionId'],slot=i['slotType'],objective=i['prophecyDefinition']['objectiveType'],target=i['targetValue'],progress=i['currentValue'],
                status=i['status'],acceptedAt=dt(i['acceptedAt']) if i['acceptedAt'] else None,reward=json.loads(i['rewardSnapshotJson']))
        view=before['overview'];choice=before['days'][-1]['selected']
        # The overview was obtained before accepting the daily offer; its nullable active pointer can be stale.
        selected=next((i for i in s['instances'] if i['scope']=='Daily' and dt(i['periodStart'])==self.observed and i['prophecyDefinitionId']==choice),None)
        self.selected=offer(selected);self.weekly=offer(next(i for i in s['instances'] if i['id']==view['greaterProphecy']['id']));self.week_start=dt(view['greaterProphecy']['periodStart'])
        for week in s['weeks']:
            start=dt(week['periodStart']);self.favor[start]=week['propheticFavor']
            for n in [3,5,7]:
                if week['milestone'+str(n)+'Claimed']:self.milestones.add((start,n))
    @property
    def now(self):return self.start+timedelta(seconds=(self.n-self.start_n)*10)
    def growth(self):return dict(level=self.level,experience=self.xp,idleExperience=self.idle,prophecyExperience=self.prophecy,essences=deepcopy(self.es[:self.attuned]))
    def run(self,outcome,until):
        changes=[];rate=self.rates['region_01_area_08']
        while self.level<40 and self.n<86400:
            self.observe();before=self.level;old=[e['level'] for e in self.es]
            win=outcome=='perfect' or (self.n+1)%5!=0
            if win:
                self.idle+=rate;self.last_essence=self.award_combat(rate)
                self.event('CreatureDefeated',1);self.event('EncounterWon',1);self.event('EssenceXpGained',self.last_essence);self.claim()
            else:self.event('EncounterLost',1,enemies=1)
            self.n+=1
            if before!=self.level or old!=[e['level'] for e in self.es]:changes.append(dict(encounter=self.n,at=self.now,state=self.growth()))
        check(self.n==until,'Stopped before/after the declared target');return changes

def dt_from_ticks(t):
    # Engine/native histories retain seven-digit timestamps; Python formula replay uses their common microsecond projection.
    from datetime import timezone
    return datetime(1970,1,1,tzinfo=timezone.utc)+timedelta(microseconds=t//10)

def qualify(q,output):
    api,fixtures=Path(q['apiRoot']),Path(q['fixtures']);archive=Path(q['expansionArchive']);preparation=Path(q['preparationArchive'])
    result=io.read(output/'result.json');plan=io.read(fixtures/'tower-level40.json');costs=io.read(Path(q['scoringRules']))['scores']
    check(result['version']==owner_module.VERSION and result['status']=='Level40GrowthQualified' and result['plan']==plan,'Wrong qualification')
    check(result['newFights']==result['newSeeds']==result['newEssencesGranted']==result['measuredPlayerSamples']==0 and not result['searchPerformed'] and not result['combatAdmission'],'Unadmitted result claim')
    check(plan['targetLevel']==40 and plan['area']=='region_01_area_08' and plan['maximumIdleEncounters']==86400 and plan['cadenceSeconds']==10 and plan['maximumOwners']==240,'Changed envelope')
    original_result=io.read(archive/'result.json');keys={r['key'] for r in original_result['journeys']};seen=set();files={'result.json','source-rules.json'}
    rules=io.read(output/'source-rules.json');quest=io.read(api/'Data/quests/region-01/roots-remember.v4.json')
    check(rules['quest']==quest and rules['initialQuestStateAssumed'] and rules['historicalDungeonCredit']==rules['historicalDropCredit']==rules['previouslyOmittedTokensCredited']==rules['newEssencesGranted']==0,'Invented quest/drop stock')
    check(quest['availability']==dict(minimumLevel=25,completedQuestIds=['quest.shenic.between_day_and_night']) and quest['objectiveMode']=='All','Quest prerequisite drift')
    options=rules['token']['options'];check([o['id'] for o in options]==['thornback_boar','hollow_stag','treant_sapling','glade_panther','forest_spirit'],'Changed source options')
    family=next(d for d in io.read(api/'Data/dungeons/dungeons.json')['families'] if d['id']=='goblin_mines')
    check(family['entryCosts']==[dict(itemId='sigil_goblin_mines',amount=1)],'Changed prospective cost')
    area=next(a for r in io.read(api/'Data/world/regions.json')['regions'] for a in r['areas'] if a['id']=='region_01_area_08')
    check(area['levelRequirement']==25 and area['requiredCompletedQuestId']=='quest.shenic.between_day_and_night' and not area.get('requiredTowerFloor'),'Unqualified area gate')
    totals=Counter();times=[];levels=[];essence_levels=[];summaries=[]
    for row in result['journeys']:
        key=row['key'];check(key in keys and key not in seen,'Duplicate/foreign server');seen.add(key);file=key+'--growth.json.gz';files.add(file);p=io.read(output/file)
        old=io.read(archive/(key+'--continuation.json.gz'));prep=io.read(preparation/(key+'--expansion.json.gz'))
        check(p['key']==key and p['sourceFile']==key+'--continuation.json.gz' and p['sourceHash']==io.sha(archive/p['sourceFile']) and p['preparationHash']==io.sha(preparation/(key+'--expansion.json.gz')),'Source drift')
        check(p['server']==old['final'] and p['newEssencesGranted']==0,'Server reward reset or invented Essence')
        history=deepcopy(prep['continuation']['historicalAttempts'])
        for step in old['steps']:
            a=io.read(archive/step['file']);history.append(dict(floor=5,attempt=a['index'],battle=a['battles']['actual'],receipt=a['receipt']))
        check(p['historicalAttempts']==history and p['personalRefreshBefore']==prep['personalRefreshBefore'] and p['nextPersonalRefreshBefore']==len(history),'Lost historical failures/rosters/refresh boundary')
        check(len(p['growth'])==len(old['owners'])==len(p['owners'])==16,'Missing personal state');after_by_id={};new_items=idle=assembled=0
        for g,before in zip(p['growth'],old['owners']):
            after=g['owner'];b=before['runtime'];a=after['runtime'];actor=after['point']['character'];id=actor['id'];outcome=before['point']['outcome']
            check(id==before['point']['character']['id'] and id not in after_by_id,'Changed/duplicated owner');after_by_id[id]=after
            check(after==dict(before,point=after['point'],runtime=a),'Origin/pity/source identity changed')
            check(g['from']==b['idleSeconds']//10 and b['idleSeconds']%10==0 and g['until']>g['from'],'Replayed idle interval')
            count=g['until']-g['from'];wins=count if outcome=='perfect' else count-(g['until']//5-g['from']//5)
            check(g['victories']==wins and a['idleSeconds']==g['until']*10 and a['combatTicks']==b['combatTicks'] and a['waitingTicks']==b['waitingTicks'],'Invented activity/clock')
            f=Forward(api,fixtures,b,a);changes=f.run(outcome,g['until']);check(g['experiencePerVictory']==f.rates['region_01_area_08'],'Nonproduction XP rate')
            check([dict(c,at=dt(c['at'])) for c in g['changes']]==changes,'Wrong growth milestone chronology')
            expected=dict(b['growth'],state=f.growth(),ownedEssences=f.es,lastEssenceExperience=f.last_essence,baseAttributes=a['growth']['baseAttributes'])
            check(a['growth']==expected and a['growth']['attuned']==4 and len(f.es)==4,'Unearned growth/training/identity')
            check(a['days'][:len(b['days'])]==b['days'] and f.dayindex==len(a['days']) and a['dungeonEvents']==b['dungeonEvents'] and a['mastery']==b['mastery'],'Source/mastery history reset')
            check(dt(a['observedDay'])==f.observed and all(prior.native.ticks_at(d['at'])<prior.instant(a) for d in a['days']),'Future day observation')
            bs,asrc=b['sources'],a['sources'];new_claims=asrc['claims'][len(bs['claims']):]
            check(asrc['claims'][:len(bs['claims'])]==bs['claims'],'Historical claim reset')
            check([dict(c,at=dt(c['at']),periodStart=dt(c['periodStart'])) for c in new_claims]==f.claims,'Wrong native claim amount/objective/calendar')
            check(asrc['resourcesReconciled'] and g['claimGuards']==asrc['duplicateClaimsRejected']==len(asrc['claims']),'Lost duplicate-claim guard')
            rewards=g['rewards'];check(rewards['from']==g['from'] and rewards['until']==g['until'] and rewards['area']=='region_01_area_08' and rewards['victories']==wins,'Reward window drift')
            owned=after['point']['owned'];check(owned==before['point']['owned']+rewards['equipment'] and len(equipment.items(owned))==len(owned),'Lost/duplicated equipment')
            for item in rewards['equipment']:
                state=item['state'];tick=int(state['provenance']['awardId'].split(':')[-1]);ordinal=(tick-621355968000000000)//100000000+1
                check(g['from']<ordinal<=g['until'] and (outcome=='perfect' or ordinal%5),'Equipment from absent/failed encounter')
                check(state['ownership']['ownerId']==id and state['tier']==1 and state['rank']==0 and item['rarity'] in ['Common','Uncommon','Rare']
                    and state['provenance']['sourceId']=='region_01_area_08' and .95<=item['attributeRollMultiplier']<=1.05,'Unfunded ordinary item')
            stock=Counter(f.items);stock.update(rewards['sigils']);check(set(rewards['sigils'])<={'sigil_goblin_mines','sigil_forgotten_catacombs'} and all(v>0 for v in rewards['sigils'].values()),'Invented sigil type')
            n=stock['sigil_fragment']//10;check(n==g['assembledSigils'],'Wrong fragment assembly');stock['sigil_fragment']-=n*10
            if n:stock['sigil_goblin_mines']+=n
            check(asrc['state']==dict(bs['state'],items=dict(stock),cinders=f.cinders,soulstones=f.soulstones,fateEcho=f.fate),'Spendable resource mismatch')
            expected_actor=deepcopy(before['point']['character']);expected_actor['level']=f.level;expected_actor['baseAttributes']['Power']=10+.25*(f.level-1);expected_actor['baseAttributes']['MaxHealth']=140+20*(f.level-1)
            expected_actor['equipment']=equipment.selected(owned,costs);expected_actor['essences']=[dict(definitionId=e['definition'],level=e['level'],ascensionTier=0,isEvolved=False) for e in f.es]
            check(actor==expected_actor,'Lost stronger gear or unearned character attributes')
            check(after['point']==dict(before['point'],character=actor,owned=owned,encounter=g['until'],availableAt=after['point']['availableAt']) and native.ticks_at(after['point']['availableAt'])==prior.instant(a),'Changed cohort provenance/availability')
            check({v['attributeType']:v['value'] for v in a['growth']['baseAttributes']}==actor['baseAttributes'],'Growth attribute runtime mismatch')
            src=g['source'];check(src['owner']==id and src['initialQuestStateAssumed'] and src['prematureTurnInRejected'] and src['wrongFamilyRejected'] and src['duplicateTurnInGrantedNothing'],'Quest probe failed')
            check(src['pending']==dict(questId=quest['id'],status='Active',objectives=[dict(objectiveKey='cross_old_forest',currentAmount=6,requiredAmount=6,complete=True),dict(objectiveKey='break_the_goblin_gate',currentAmount=0,requiredAmount=1,complete=False),dict(objectiveKey='reach_level_30',currentAmount=30,requiredAmount=30,complete=True)]),'Retrospective or unearned quest credit')
            check(src['hypotheticalMinesTurnInTokenQuantity']==1 and src['remainingMinesSigils']==stock['sigil_goblin_mines'] and src['entryCost']=={'sigil_goblin_mines':1}
                and src['needsFreshPaidMinesCompletion'] and not src['probeRetained'] and not src['historicalResonanceReset'] and src['newEssencesGranted']==src['historicalDungeonCredit']==0,'Probe became owned state')
            check(len(src['candidates'])==5,'Missing token choice')
            for c,opt in zip(src['candidates'],options):
                already='essence.'+opt['id'] in {e['definition'] for e in f.es}
                check(c['id']==opt['id'] and c['itemId']==opt['itemId'] and c['alreadyOwned']==already and c['eligibleFifth']!=already,'Duplicate fifth choice')
                if not already:check(c['native']==dict(tokenConsumed=True,duplicateOpenRejected=True,unboundConsumed=True,duplicateAbsorptionRejected=True,lockedAt39=True,acceptedAt40=True,newEssenceLevel=1,probeTrainingExperience=1,probeRetained=False),'Native source/absorption/slot/training probe failed');totals['candidateProbes']+=1
            totals['owners']+=1;totals['claims']+=len(new_claims);totals['idleXp']+=wins*g['experiencePerVictory'];totals['prophecyXp']+=sum(c['reward']['characterExperience'] for c in new_claims)
            totals['fundedMinesOwners']+=int(stock['sigil_goblin_mines']>=1);totals['retainedItems']+=len(before['point']['owned']);totals['changedLoadouts']+=int(actor['equipment']!=before['point']['character']['equipment'])
            new_items+=len(rewards['equipment']);idle+=count;assembled+=n;times.append(count/360);levels.append(f.level);essence_levels.extend(e['level'] for e in f.es)
        at=max(prior.instant(o['runtime']) for o in after_by_id.values())
        check(p['owners']==[dict(g['owner'],runtime=prior.wait(g['owner']['runtime'],at)) for g in p['growth']],'Lost owner waiting alignment')
        party=p['party'];oldparty=prep['continuation']['party'];members=[next(o['point'] for o in p['owners'] if o['point']['character']['id']==m['character']['id']) for m in oldparty['members']]
        check(party==dict(oldparty,members=members,startsAt=party['startsAt']) and native.ticks_at(party['startsAt'])==at,'Moved/cloned roster or clock');prior.floor_check(party['floor'],api)
        friendly=[a for a in p['prepared'] if a['slot']['side']=='Friendly'];check(len(friendly)==10 and len(p['prepared'])==11,'Wrong prepared party')
        for i,(m,c) in enumerate(zip(members,friendly)):
            a=m['character'];check(c['slot']['sourceEntityId']==a['id'] and c['slot']['partyNumber']==i//5+1 and c['level']==a['level'] and c['baseAttributes']==a['baseAttributes'],'Prepared actor mismatch')
            check(equipment.items(c['equipment'])==equipment.items([e['data'] for e in a['equipment']]),'Prepared gear mismatch')
            check(c['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=0,isEvolved=False) for e in a['essences']],'Prepared training mismatch')
        check(row==dict(key=key,owners=16,partySize=10,startedAt=old['final']['at'],endedAt=party['startsAt'],levelMin=min(o['point']['character']['level'] for o in p['owners']),levelMax=max(o['point']['character']['level'] for o in p['owners']),newEquipment=new_items,newIdleEncounters=idle,assembledSigils=assembled),'Summary mismatch')
        totals['newItems']+=new_items;totals['idleEncounters']+=idle;totals['assembledSigils']+=assembled;summaries.append(row)
    check(seen==keys and len(keys)==15 and totals['owners']==240 and set(io.read(output/'files.json'))==files,'Incomplete qualification')
    check(result['held']==original_result['held'] and len(result['held'])==17,'Changed held servers')
    for h in result['held']:check(io.sha(Path(q['returnArchive'])/h['file'])==h['hash'],'Lost held personal states')
    return dict(status='VerifiedLevel40Growth',**totals,heldServers=17,ownersPreserved=512,levelRange=[min(levels),max(levels)],essenceLevelRange=[min(essence_levels),max(essence_levels)],
        additionalCadenceHoursRange=[min(times),max(times)],preparedMembers=150,newFights=0,newCombatSeeds=0,newEssencesGranted=0,measuredPlayerSamples=0,combatAdmission=False)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');output=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'Fresh external audit receipt required')
    check(d['version']==owner_module.VERSION and d['requestPin']==io.sha(owner/'request.json'),'Declaration drift')
    for file,pin in q['inputHashes'].items():check(io.sha(Path(file))==pin,'Frozen drift: '+file)
    for key,(_,pin) in owner_module.ARCHIVES.items():io.manifest(Path(q[key]),pin)
    owner_module.exclusions(Path(d['latestSeedLedger']));check(d['latestSeedLedgerPin']==owner_module.LEDGER_PIN and d['exclusionUnionCount']==878799,'Lost seed exclusion')
    io.manifest(output,a.manifest_pin);check(sum(f.stat().st_size for f in output.iterdir())<=d['maximumBytes']==256*1048576,'Output cap')
    check(d['maximumSeconds']==900 and d['maximumNativeSeconds']==840 and d['maximumFights']==d['newSeeds']==d['retries']==0 and d['maximumOwners']==240,'Changed cap')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=900+process['cleanupAllowanceSeconds'],'Incomplete process')
    result=qualify(q,output);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']));io.write(a.receipt,result);print(json.dumps(result,indent=2))

if __name__=='__main__':main()
