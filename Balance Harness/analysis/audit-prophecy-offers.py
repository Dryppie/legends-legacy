"""Independent SHA-256 weighted offers, visible choices, native-objective activity and personal ledger audit."""
from collections import Counter
from datetime import datetime, timedelta
import hashlib
import itertools
import json

def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def check(value,message):
    if not value: raise AssertionError(message)
def dt(value): return datetime.fromisoformat(value) if value is not None else None
def wins(outcome,n): return n if outcome=='perfect' else n-n//5
def threshold(outcome,target):
    low=target;high=target*2
    while low<high:
        mid=(low+high)//2
        if wins(outcome,mid)>=target:high=mid
        else:low=mid+1
    return low

class OfferAudit:
    def __init__(self,api,fixtures):
        self.plan=read(fixtures/'tower-prophecy-offers.json');self.epoch=dt(self.plan['epoch']);self.cadence=self.plan['cadenceSeconds']
        self.definitions=[d for file in ['daily.json','weekly.json'] for d in read(api/'Data/prophecies'/file)['definitions']]
        self.by_id={d['id']:d for d in self.definitions}
        self.targets=read(api/'Data/prophecies/targets.json')['targets']
        self.reward_rules=read(api/'Data/prophecies/rewards.json');self.revelation=read(api/'Data/prophecies/weekly-revelation.json')
        curve=read(api/'Data/progression/character-experience.json')['characterLevelCurve']
        raw=curve['baseExperience']+30*curve['linearExperiencePerLevel']+900*curve['quadraticExperiencePerLevelSquared']
        self.xp=(raw+curve['roundingIncrement']-1)//curve['roundingIncrement']*curve['roundingIncrement']
        self.assembly=read(api/'Data/dungeons/sigil-assembly.json');check(self.assembly['enabled'],'Disabled assembly')
        self.empty=dict(cinders=0,characterExperience=0,essenceExperience=0,soulstones=0,sigilFragments=0,propheticFavor=0,fateEcho=0,cacheItemId=None,items=[])

    def pick(self,owner,start,scope,slot,excluded=(),categories=()):
        eligible=[d for d in self.definitions if d.get('isEnabled',True) and d['scope']==scope and d.get('minPlayerLevel',1)<=30<=d.get('maxPlayerLevel',30)]
        candidates=[d for d in eligible if slot in d['allowedSlots']] or [d for d in eligible if d['category']=='Combat']
        unique=[d for d in candidates if d['id'] not in excluded]
        pool=[d for d in unique if d['category'] not in categories] or unique or candidates
        seed=f"{owner.replace('-','')}:{start.strftime('%Y-%m-%dT%H:%M:%S')}.0000000+00:00:{slot}:{scope}:initial"
        roll=int.from_bytes(hashlib.sha256(seed.encode()).digest()[:4],'little')%sum(max(1,d.get('weight',100)) for d in pool)
        for d in pool:
            roll-=max(1,d.get('weight',100))
            if roll<0:return d
        raise AssertionError('No offer')

    def reward(self,d):
        p=next(p for p in self.reward_rules['profiles'] if p['id']==d['rewardProfileId'])
        check(p['minimumCinders']==0,'Review Cinder scaling')
        r=self.empty|p['flatReward']|dict(characterExperience=(self.xp*p['characterExperience']['nextLevelBasisPoints']+5000)//10000,
            propheticFavor=next(f['amount'] for f in self.revelation['favorRewards'] if f['scope']==d['scope']))
        r['items']=[]
        for package in self.reward_rules['categoryPackages']:
            if package['scope']!=d['scope'] or package['category']!=d['category'] or package.get('difficulty',d['difficulty'])!=d['difficulty']:continue
            check(not package.get('reward'),'Review category flat reward')
            r['items'] += [dict(itemId=i['itemId'],quantity=i['quantity']) for i in package.get('levelScaledItems',[]) if i['minLevel']<=30<=i.get('maxLevel',30)]
        return r

    def offer(self,d,slot,at=None):
        target=next(t for t in self.targets if t['scope']==d['scope'] and t['objectiveType']==d['objectiveType'])['values'][d['difficulty'].lower()]
        return dict(definition=d['id'],slot=slot,objective=d['objectiveType'],target=target,progress=0,status='Accepted' if at else 'Offered',acceptedAt=at,reward=self.reward(d))

    def audit(self,schedule,owner,outcome,horizon):
        check(self.plan['policy']=='offered-kills-no-reroll' and horizon in self.plan['checkpoints'] and self.cadence==10,'Changed offer scope')
        days=schedule['offers'];daylen=86400//self.cadence;check(len(days)==(horizon+daylen-1)//daylen,'Wrong day count')
        claims=[];weekly=None;week=0;favor=Counter();milestones=set();offer_count=0;abstentions=0
        def assert_offer(observed,expected):
            normalized=observed|dict(acceptedAt=dt(observed['acceptedAt']))
            check(normalized==expected,'Changed native offer/progress: '+expected['definition'])
        for index,row in enumerate(days):
            start=index*daylen;end=min(horizon,start+daylen);at=self.epoch+timedelta(days=index)
            check(dt(row['day'])==at,'Changed visit day')
            if index%7==0:
                week=index//7;weekly=self.offer(self.pick(owner,at,'Weekly','Greater'),'Greater',at)
            excluded=set();categories=set();daily=[]
            for slot in ['Steady','Focused','Ominous']:
                d=self.pick(owner,at,'Daily',slot,excluded,categories);daily.append(self.offer(d,slot));excluded.add(d['id']);categories.add(d['category'])
            check(len(row['daily'])==len(row['finalDaily'])==3,'Missing daily slots')
            for observed,expected in zip(row['daily'],daily): assert_offer(observed,expected)
            assert_offer(row['weekly'],weekly);offer_count+=3+(index%7==0)
            options=[d for d in daily if d['objective']=='KillCreatures']
            chosen=min(options,key=lambda d:(-d['reward']['sigilFragments'],d['target'],d['definition'])) if options else None
            check(row['selected']==(chosen['definition'] if chosen else None),'Choice peeks at future outcomes')
            check(row['choiceReason']==('HighestFragmentsThenLowestTargetThenId' if chosen else 'NoOfferedKillObjective'),'Wrong choice rule')
            abstentions+=int(chosen is None)
            for daily_offer in daily:
                if chosen:
                    daily_offer['status']='Accepted' if daily_offer is chosen else 'Declined'
                    if daily_offer is chosen:daily_offer['acceptedAt']=at
            events=[]
            for offer in ([chosen] if chosen else [])+[weekly]:
                if offer['status']!='Accepted' or offer['objective'] not in ['KillCreatures','WinEncounters']:continue
                definition=self.by_id[offer['definition']]
                check(offer['objective']!='WinEncounters' or json.loads(definition.get('objectiveParameterJson','{}'))=={},'Unsupported win condition')
                remaining=offer['target']-offer['progress'];available=wins(outcome,end)-wins(outcome,start)
                offer['progress']=min(offer['target'],offer['progress']+available)
                if available>=remaining:
                    completion=threshold(outcome,wins(outcome,start)+remaining)
                    occurred=self.epoch+timedelta(seconds=(completion-1)*self.cadence)
                    period=self.epoch+timedelta(days=week*7) if offer is weekly else at
                    events.append(dict(source=offer['definition'],at=occurred,periodStart=period,requiredKills=offer['target'] if offer['objective']=='KillCreatures' else 0,
                        reward=offer['reward'],objectiveType=offer['objective'],requiredProgress=offer['target']))
                    offer['status']='Claimed'
            for occurred,group in itertools.groupby(sorted(events,key=lambda e:e['at']),key=lambda e:e['at']):
                for claim in group:
                    claims.append(claim);favor[week]=min(7,favor[week]+claim['reward']['propheticFavor'])
                for m in self.revelation['milestones']:
                    key=(week,m['favorRequired'])
                    if favor[week]>=m['favorRequired'] and key not in milestones:
                        milestones.add(key);claims.append(dict(source='revelation.'+str(m['favorRequired']),at=occurred,periodStart=self.epoch+timedelta(days=week*7),
                            requiredKills=0,reward=self.empty|m['reward'],objectiveType='Favor',requiredProgress=m['favorRequired']))
            for observed,expected in zip(row['finalDaily'],daily):assert_offer(observed,expected)
            assert_offer(row['finalWeekly'],weekly)
        check(len(claims)==len(schedule['claims'])==schedule['duplicateClaimsRejected'],'Wrong native claim/retry count')
        for expected,observed in zip(claims,schedule['claims']):
            check((observed|dict(at=dt(observed['at']),periodStart=dt(observed['periodStart'])))==expected,'Changed claim/calendar/reward')
        items=Counter();seen=xp=cinders=soulstones=fate=0
        check([c['encounter'] for c in schedule['checkpoints']]==[c for c in self.plan['checkpoints'] if c<=horizon],'Missing source checkpoint')
        for cp in schedule['checkpoints']:
            at=self.epoch+timedelta(seconds=cp['encounter']*self.cadence)
            while seen<len(claims) and claims[seen]['at']<at:
                reward=claims[seen]['reward'];items['sigil_fragment']+=reward['sigilFragments']
                if reward['cacheItemId']:items[reward['cacheItemId']]+=1
                for item in reward['items']:items[item['itemId']]+=item['quantity']
                xp+=reward['characterExperience'];cinders+=reward['cinders'];soulstones+=reward['soulstones'];fate+=reward['fateEcho'];seen+=1
            fragments=items.get('sigil_fragment',0);quantity=fragments//self.assembly['fragmentCost']
            if quantity:items['sigil_fragment']-=quantity*self.assembly['fragmentCost'];items['sigil_goblin_mines']+=quantity
            check(cp['fragmentsBeforeAssembly']==fragments and cp['assembledNow']==quantity and cp['claims']==seen and cp['victories']==wins(outcome,cp['encounter']),'Invalid assembly/activity')
            check(cp['state']==dict(owner=owner,items=dict(items),unappliedCharacterExperience=xp,cinders=cinders,soulstones=soulstones,fateEcho=fate),'Incorrect personal source stock')
        return dict(days=len(days),offers=offer_count,claims=len(claims),abstentions=abstentions,extraSigils=items['sigil_goblin_mines'])
