"""Independent chronological reconstruction of growth, native offers, dungeon XP, mastery and serial time."""
import copy
from collections import Counter
from datetime import datetime, timedelta
from decimal import Decimal, ROUND_HALF_UP
import importlib.util
import math
from pathlib import Path

def module(name,path):
    s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
growth=module('journey_growth_reference',Path(__file__).with_name('verify-tower-growth.py'))
loot=module('journey_identity_reference',Path(__file__).with_name('audit-dungeon-equipment-loot.py'))
read,check=growth.read,growth.check
def dt(s):return datetime.fromisoformat(s)

class JourneyAudit:
    def __init__(self,api,fixtures,history,battle):
        self.h=history;self.g=history['progression'];self.battle=battle;self.owner=history['final']['id'];self.enabled=history['summary']['policy']=='earned-progression'
        self.o=growth.GrowingOffers(api,fixtures);self.epoch=self.o.epoch
        self.curve=read(api/'Data/progression/character-experience.json')['characterLevelCurve']
        settings=read(api/'Data/progression/area-experience.json')['areaExperience']
        areas={a['id']:a for r in read(api/'Data/world/regions.json')['regions'] for a in r['areas']}
        self.rates={}
        for key in {w['area'] for w in history['windows']}:
            a=areas[key];weights=[Decimal(str(v)) for v in a['spawnProbabilities']]
            count=sum(w*(i+1) for i,w in enumerate(weights))/sum(weights)
            rate=Decimal(str(settings['baseExperiencePerHour']))*Decimal(str(settings['difficultyTierMultiplier']))**a['difficultyTier']/360/count
            self.rates[key]=int(rate.quantize(Decimal(1),rounding=ROUND_HALF_UP))
        self.dungeon_rules=read(api/'Data/progression/dungeon-rewards.json')['dungeonRewards']
        self.level=30;self.xp=self.idle=self.dungeon=self.prophecy=self.withheld_idle=self.withheld_dungeon=0
        self.n=self.ticks=self.dayindex=self.checkpointindex=self.entryindex=0
        self.es=[dict(id=loot.identity(['balance-essence-v1',self.owner.replace('-',''),str(i)]),definition=e['definitionId'],level=1,currentXp=0,ascensionTier=0) for i,e in enumerate(history['final']['essences'])]
        self.attuned=3;self.window=0;self.observed=None;self.week_start=None;self.selected=self.weekly=None
        self.claims=[];self.events=[];self.items=Counter();self.favor=Counter();self.milestones=set();self.cinders=self.soulstones=self.fate=0;self.masteries={}
    @property
    def now(self):return self.epoch+timedelta(seconds=self.n*10+self.ticks/10)
    def required(self,level):
        c=self.curve;raw=c['baseExperience']+c['linearExperiencePerLevel']*level+c['quadraticExperiencePerLevelSquared']*level*level
        return (raw+c['roundingIncrement']-1)//c['roundingIncrement']*c['roundingIncrement']
    def award_character(self,amount):
        self.xp+=amount
        while self.xp>=self.required(self.level):self.xp-=self.required(self.level);self.level+=1
    def award_combat(self,amount):
        if not self.enabled:return 0
        total=0
        for e in self.es[:self.attuned]:
            remaining=amount
            while remaining and e['level']<10:
                need=math.ceil(132860*math.pow(1.02,e['level']-1))-e['currentXp'];grant=min(remaining,need)
                e['currentXp']+=grant;remaining-=grant;total+=grant
                if grant==need:e['level']+=1;e['currentXp']=0
        self.award_character(amount);return total
    def observe(self):
        today=self.now.replace(hour=0,minute=0,second=0,microsecond=0)
        if self.observed==today:return
        row=self.g['days'][self.dayindex];self.dayindex+=1
        check(dt(row['at'])==self.now and row['level']==self.level,'Journey visit time/current level')
        self.o.level=self.level;self.o.xp=self.required(self.level)
        start=today-timedelta(days=today.weekday())
        if self.week_start!=start:
            self.week_start=start;self.weekly=self.o.offer(self.o.pick(self.owner,start,'Weekly','Greater'),'Greater',self.now)
        daily=[];ids=set();categories=set()
        for slot in ['Steady','Focused','Ominous']:
            d=self.o.pick(self.owner,today,'Daily',slot,ids,categories);daily.append(self.o.offer(d,slot));ids.add(d['id']);categories.add(d['category'])
        def compare(actual,expected):check(actual|dict(acceptedAt=growth.offers.dt(actual['acceptedAt']))=={k:v for k,v in expected.items() if not k.startswith('_')},'Journey generated offer/progress')
        check(len(row['daily'])==3,'Missing offers')
        for a,e in zip(row['daily'],daily):compare(a,e)
        compare(row['weekly'],self.weekly)
        choices=[d for d in daily if d['objective']=='KillCreatures']
        self.selected=min(choices,key=lambda d:(-d['reward']['sigilFragments'],d['target'],d['definition'])) if choices else None
        check(row['selected']==(self.selected['definition'] if self.selected else None),'Journey choice')
        if self.selected:self.selected['status']='Accepted';self.selected['acceptedAt']=self.now
        self.observed=today
    def event(self,kind,amount,enemies=None,creature=None,dungeon=False):
        if amount<=0:return
        if dungeon:self.events.append(dict(at=self.now,kind=kind,amount=amount,enemyCount=enemies,creature=creature))
        mapping={'KillCreatures':'CreatureDefeated','WinEncounters':'EncounterWon','GainEssenceXp':'EssenceXpGained',
                 'ClearDungeonRooms':'DungeonRoomCleared','CompleteDungeons':'DungeonCompleted'}
        for p,period,length in [(self.selected,self.observed,1),(self.weekly,self.week_start,7)]:
            if not p or p['status']!='Accepted' or not period<=self.now<period+timedelta(days=length):continue
            objective=p['objective'];delta=amount if mapping.get(objective)==kind else 0
            if objective=='WinEncounters' and delta:
                parameter=self.o.by_id[p['definition']].get('objectiveParameterJson','{}')
                minimum=__import__('json').loads(parameter).get('minimumEnemyCount',0) or 0
                if enemies is not None and enemies<minimum:delta=0
            if objective=='KillDifferentCreatureTypes' and kind=='CreatureDefeated' and creature:
                unique=p.setdefault('_unique',set());delta=int(creature not in unique);unique.add(creature)
            if objective=='MeaningfulDefeatThenWins':
                if kind=='EncounterLost':p['_defeat']=True
                if kind=='EncounterWon' and p.get('_defeat'):delta=amount
            p['progress']=min(p['target'],p['progress']+delta)
            if p['progress']==p['target']:p['status']='Completed'
    def add_claim(self,source,objective,target,reward,period):
        self.claims.append(dict(source=source,at=self.now,periodStart=period,requiredKills=target if objective=='KillCreatures' else 0,
            reward=reward,objectiveType=objective,requiredProgress=target))
        self.prophecy+=reward['characterExperience']
        if self.enabled:self.award_character(reward['characterExperience'])
        if reward['sigilFragments']:self.items['sigil_fragment']+=reward['sigilFragments']
        if reward['cacheItemId']:self.items[reward['cacheItemId']]+=1
        for i in reward['items']:self.items[i['itemId']]+=i['quantity']
        self.cinders+=reward['cinders'];self.soulstones+=reward['soulstones'];self.fate+=reward['fateEcho']
        week=self.now.replace(hour=0,minute=0,second=0,microsecond=0)-timedelta(days=self.now.weekday())
        self.favor[week]=min(7,self.favor[week]+reward['propheticFavor'])
    def claim(self):
        for p,period,length in [(self.selected,self.observed,1),(self.weekly,self.week_start,7)]:
            if p and p['status']=='Completed' and period<=self.now<period+timedelta(days=length):
                self.add_claim(p['definition'],p['objective'],p['target'],p['reward'],period);p['status']='Claimed'
        week=self.now.replace(hour=0,minute=0,second=0,microsecond=0)-timedelta(days=self.now.weekday())
        for m in self.o.revelation['milestones']:
            key=(week,m['favorRequired'])
            if self.favor[week]>=m['favorRequired'] and key not in self.milestones:
                self.milestones.add(key);self.add_claim('revelation.'+str(m['favorRequired']),'Favor',m['favorRequired'],self.o.empty|m['reward'],week)
    def state(self):
        return dict(at=self.now,idleSeconds=self.n*10,combatTicks=self.ticks,
            growth=dict(level=self.level,experience=self.xp,idleExperience=self.idle,prophecyExperience=self.prophecy,essences=copy.deepcopy(self.es[:self.attuned])),
            dungeonExperience=self.dungeon,withheldIdleExperience=self.withheld_idle,withheldDungeonExperience=self.withheld_dungeon,
            source=dict(owner=self.owner,items=dict(self.items),unappliedCharacterExperience=0 if self.enabled else self.prophecy,
                cinders=self.cinders,soulstones=self.soulstones,fateEcho=self.fate))
    def check_state(self,actual):check(actual|dict(at=dt(actual['at']))==self.state(),'Journey XP/source/clock state mismatch')
    def character(self,actual):
        check(actual['level']==self.level and actual['essences']==[dict(definitionId=e['definition'],level=e['level'],ascensionTier=0,isEvolved=False) for e in self.es[:self.attuned]],'Unfunded entry/final level or Essence')
        if self.level>30:check(actual['baseAttributes']['Power']==10+.25*(self.level-1) and actual['baseAttributes']['MaxHealth']==140+20*(self.level-1),'Growth base attributes')
    def checkpoint(self,cp):
        while self.n<cp['encounter']:
            self.observe();ordinal=self.n+1
            while self.h['windows'][self.window]['until']<ordinal:self.window+=1
            if self.h['summary']['outcome']=='perfect' or ordinal%5:
                xp=self.rates[self.h['windows'][self.window]['area']];essence=self.award_combat(xp)
                if self.enabled:self.idle+=xp
                else:self.withheld_idle+=xp
                self.event('CreatureDefeated',1);self.event('EncounterWon',1,1)
                self.event('EssenceXpGained',essence);self.claim()
            else:self.event('EncounterLost',1,1)
            self.n+=1
            if self.n==self.h['summary']['questAt']:self.attuned=4
        quantity=self.items.get('sigil_fragment',0)//self.o.assembly['fragmentCost']
        if quantity:self.items['sigil_fragment']-=quantity*self.o.assembly['fragmentCost'];self.items['sigil_goblin_mines']+=quantity
        recorded=self.g['checkpoints'][self.checkpointindex];self.checkpointindex+=1
        check(recorded['encounter']==self.n and recorded['assembled']==quantity,'Journey funded assembly')
        self.check_state(recorded['state']);return quantity
    def entry(self,step):
        entry=self.g['entries'][self.entryindex];check(entry['ordinal']==self.entryindex,'Journey entry order');self.entryindex+=1
        self.check_state(entry['before']);self.character(step['before'])
        r=entry['receipt'];start=self.now;family=step['dungeon'];total,level,count=self.masteries.get(family,(0,0,0));mastery=level if self.enabled else 0
        check(r['masteryAtEntry']==mastery and dt(r['started'])==start,'Entry mastery/time')
        run=self.battle(step['file']);bi=0;pending=0;room_rows=[]
        for action in run['actions']:
            self.observe()
            if action['type']!='RestSite':
                b=run['battles'][bi];bi+=1;s=b['summary'];victory=s['contentOutcome']=='Victory';ticks=s['durationTicks'];self.ticks+=ticks
                enemies=[p for p in b['preparedParticipants'] if p['slot']['side']=='Hostile']
                defeated=[p for i,p in enumerate(enemies) if victory or i<len(s['hostile']) and s['hostile'][i]['health']<=0]
                multiplier=Decimal(str(self.dungeon_rules['roomMultipliers'][action['type']]))
                xp=int((self.dungeon_rules['baseExperiencePerEncounter']*multiplier).quantize(Decimal(1),rounding=ROUND_HALF_UP)) if victory else 0
                cinders=int((self.dungeon_rules['baseCindersPerEncounter']*multiplier).quantize(Decimal(1),rounding=ROUND_HALF_UP)) if victory else 0
                pending+=xp
                self.event('EncounterWon' if victory else 'EncounterLost',1,len(enemies),dungeon=True)
                for p in defeated:self.event('CreatureDefeated',1,creature=p['slot']['sourceEntityId'],dungeon=True)
                room_rows.append(dict(room=action['room'],at=self.now,experience=xp,cinders=cinders,soulstones=5,enemies=len(enemies),kills=len(defeated),victory=victory,durationTicks=ticks));self.claim()
            if action['status']!='Failed':self.event('DungeonRoomCleared',1,dungeon=True)
            if action['status']=='Completed':self.event('DungeonCompleted',1,dungeon=True)
            self.claim()
        check([p|dict(at=dt(p['at'])) for p in r['rooms']]==room_rows,'Native room XP/event ledger')
        completed=run['status']=='Completed'
        credited=run['actions'][:-1] if run['failure'] in ['Combat Readiness','Abandonment'] else run['actions']
        reasons=[dict(id='completion',description='Dungeon completed',experience=100+(len(run['actions'])+1)*5)] if completed else [dict(id='attempt',description='Dungeon attempt',experience=5)]
        if not completed and credited:reasons.append(dict(id='rooms_cleared',description='Rooms cleared',experience=len(credited)*5))
        if any(a['type']=='Boss' for a in credited):reasons.append(dict(id='boss_defeated',description='Boss defeated',experience=50))
        mini=sum(a['type']=='MiniBoss' for a in credited)
        if mini:reasons.append(dict(id='miniboss_defeated',description='Miniboss defeated',experience=mini*25))
        earned=sum(x['experience'] for x in reasons);total+=earned;new_level=sum(total>=t for t in [1000,2500,5000,9000,14000,21000,30000,42000,56000,75000]);count+=int(completed)
        check(r['mastery']==dict(dungeonDefinitionId=family,experienceAwarded=earned,totalExperience=total,previousLevel=level,level=new_level,completionCount=count,reasons=reasons,alreadyAwarded=False,
            levelsGained=new_level-level,maxLevelRewardPreviouslyClaimed=not completed,maxLevelRewardWasDeferred=False,unlocksMaxLevelReward=False),'Native family mastery receipt')
        self.masteries[family]=(total,new_level,count)
        essence=0
        if completed:
            essence=self.award_combat(pending)
            if self.enabled:self.dungeon+=pending
            else:self.withheld_dungeon+=pending
            self.event('EssenceXpGained',essence,dungeon=True);self.claim()
        check(r['pendingExperience']==(pending if completed else 0) and r['lostExperience']==(0 if completed else pending)
            and r['appliedExperience']==(pending if completed and self.enabled else 0) and r['withheldExperience']==(pending if completed and not self.enabled else 0)
            and r['essenceExperience']==essence and dt(r['ended'])==self.now,'Dungeon XP loss/claim/time')
        self.check_state(entry['after']);return mastery
    def finish(self):
        self.check_state(self.g['final']);self.character(self.h['final'])
        check(len(self.claims)==len(self.g['claims'])==self.g['duplicateClaimsRejected'],'Journey claims/retries')
        for e,a in zip(self.claims,self.g['claims']):check(a|dict(at=dt(a['at']),periodStart=dt(a['periodStart']))==e,'Native growing claim amount/timestamp')
        check(self.events==[e|dict(at=dt(e['at'])) for e in self.g['dungeonEvents']],'Native dungeon events missing/duplicated')
        check(self.dayindex==len(self.g['days']) and self.checkpointindex==len(self.g['checkpoints']) and self.entryindex==len(self.g['entries']),'Unconsumed journey records')
        prepared=self.h['finalPrepared'][0]
        check(prepared['level']==self.level and prepared['essences']==[dict(essenceDefinitionId=e['definition'],level=e['level'],ascensionTier=0) for e in self.es],'Final prepared growth')
        return dict(days=self.dayindex,offers=self.dayindex*3+len(self.favor),claims=len(self.claims),abstentions=sum(d['selected'] is None for d in self.g['days']),extraSigils=self.items.get('sigil_goblin_mines',0))
