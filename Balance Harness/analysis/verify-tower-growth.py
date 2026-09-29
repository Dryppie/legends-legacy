"""Independent XP/offer/training and mastery-prefix audit. Executes no game code or combat."""
import argparse
from collections import Counter
import copy
from datetime import datetime, timedelta, timezone
from decimal import Decimal, ROUND_HALF_UP
import gzip
import hashlib
import importlib.util
import json
import math
from pathlib import Path

def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def check(ok,message):
    if not ok: raise AssertionError(message)
def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
offers=module('growth_offer_reference',Path(__file__).with_name('audit-prophecy-offers.py'))

class GrowingOffers(offers.OfferAudit):
    level=30
    def pick(self,owner,start,scope,slot,excluded=(),categories=()):
        eligible=[d for d in self.definitions if d.get('isEnabled',True) and d['scope']==scope and d.get('minPlayerLevel',1)<=self.level and (d.get('maxPlayerLevel') is None or d['maxPlayerLevel']>=self.level)]
        candidates=[d for d in eligible if slot in d['allowedSlots']] or [d for d in eligible if d['category']=='Combat']
        unique=[d for d in candidates if d['id'] not in excluded]
        pool=[d for d in unique if d['category'] not in categories] or unique or candidates
        seed=f"{owner.replace('-','')}:{start.strftime('%Y-%m-%dT%H:%M:%S')}.0000000+00:00:{slot}:{scope}:initial"
        roll=int.from_bytes(hashlib.sha256(seed.encode()).digest()[:4],'little')%sum(max(1,d.get('weight',100)) for d in pool)
        for d in pool:
            roll-=max(1,d.get('weight',100))
            if roll<0:return d
        raise AssertionError('No eligible generated offer')
    def reward(self,d):
        p=next(p for p in self.reward_rules['profiles'] if p['id']==d['rewardProfileId'])
        check(p['minimumCinders']==0,'Review Cinder scaling')
        r=self.empty|p['flatReward']|dict(characterExperience=(self.xp*p['characterExperience']['nextLevelBasisPoints']+5000)//10000,
            propheticFavor=next(f['amount'] for f in self.revelation['favorRewards'] if f['scope']==d['scope']))
        r['items']=[]
        for package in self.reward_rules['categoryPackages']:
            if package['scope']!=d['scope'] or package['category']!=d['category'] or package.get('difficulty',d['difficulty'])!=d['difficulty']:continue
            check(not package.get('reward'),'Review category flat reward')
            r['items'] += [dict(itemId=i['itemId'],quantity=i['quantity']) for i in package.get('levelScaledItems',[]) if i['minLevel']<=self.level<=i.get('maxLevel',self.level)]
        return r

def qualification(api,fixtures,row,history):
    g=row['growth'];o=GrowingOffers(api,fixtures);owner=g['owner'];horizon=g['horizon'];quest=g['questAt'];outcome=g['outcome']
    before=history['steps'][0]['before']
    check(g['before']==before and owner==before['id'] and horizon==history['steps'][0]['encounter'] and quest==history['summary']['questAt'],'Wrong first entry or personal identity')
    check(g['transferredDungeonOutcomes']==g['dungeonMasteryAtEntry']==0 and g['stop']=='BeforeFirstArchivedDungeonEntry','Transferred changed combat')
    curve=read(api/'Data/progression/character-experience.json')['characterLevelCurve']
    def required(level):
        raw=curve['baseExperience']+curve['linearExperiencePerLevel']*level+curve['quadraticExperiencePerLevelSquared']*level*level
        return (raw+curve['roundingIncrement']-1)//curve['roundingIncrement']*curve['roundingIncrement']
    area_settings=read(api/'Data/progression/area-experience.json')['areaExperience']
    regions=read(api/'Data/world/regions.json')['regions'];areas={a['id']:a for r in regions for a in r['areas']}
    def rate(area_id):
        a=areas[area_id];weights=[Decimal(str(v)) for v in a['spawnProbabilities']]
        count=sum(w*(i+1) for i,w in enumerate(weights))/sum(weights)
        target=Decimal(str(area_settings['baseExperiencePerHour']))*Decimal(str(area_settings['difficultyTierMultiplier']))**a['difficultyTier']
        return int((target/360/count).quantize(Decimal(1),rounding=ROUND_HALF_UP))
    rates={w['area']:rate(w['area']) for w in history['windows'] if w['from']<horizon}
    check(g['rates']==rates,'Idle rates do not match production content')
    check(before['level']==30 and len(before['essences'])==4 and all(e['level']==1 and e['ascensionTier']==0 and not e['isEvolved'] for e in before['essences']),'Changed initial training')
    identities=[e['id'] for e in g['baselinePrepared']['essences']]
    check(len(set(identities))==4,'Duplicate owned Essence identity')
    es=[dict(id=identities[i],definition=e['definitionId'],level=1,currentXp=0,ascensionTier=0) for i,e in enumerate(before['essences'])]
    level=30;xp=idle=prophecy=0;attuned=3;window=0;dayindex=0;cpindex=0;changes=[];claims=[];weekly=None;week=0;favor=Counter();milestones=set();items=Counter();cinders=soulstones=fate=0
    def state(): return dict(level=level,experience=xp,idleExperience=idle,prophecyExperience=prophecy,essences=copy.deepcopy(es[:attuned]))
    def add_character(amount):
        nonlocal level,xp
        xp+=amount
        while xp>=required(level):xp-=required(level);level+=1
    def add_claim(source,objective,target,reward,at,period):
        nonlocal prophecy,cinders,soulstones,fate
        claims.append(dict(source=source,at=at,periodStart=period,requiredKills=target if objective=='KillCreatures' else 0,reward=reward,objectiveType=objective,requiredProgress=target))
        prophecy+=reward['characterExperience'];add_character(reward['characterExperience'])
        if reward['sigilFragments']: items['sigil_fragment']+=reward['sigilFragments']
        if reward['cacheItemId']:items[reward['cacheItemId']]+=1
        for item in reward['items']:items[item['itemId']]+=item['quantity']
        cinders+=reward['cinders'];soulstones+=reward['soulstones'];fate+=reward['fateEcho'];favor[week]=min(7,favor[week]+reward['propheticFavor'])
    def compare_offer(actual,expected):check(actual|dict(acceptedAt=offers.dt(actual['acceptedAt']))==expected,'Offer generation/progress mismatch')
    for n in range(1,horizon+1):
        at=o.epoch+timedelta(seconds=(n-1)*10)
        if (n-1)%8640==0:
            day=g['days'][dayindex];o.level=level;o.xp=required(level)
            check(offers.dt(day['day'])==at and day['level']==level,'Offer did not use current level')
            if dayindex%7==0:
                week=dayindex//7;weekly=o.offer(o.pick(owner,at,'Weekly','Greater'),'Greater',at)
            ids=set();categories=set();daily=[]
            for slot in ['Steady','Focused','Ominous']:
                d=o.pick(owner,at,'Daily',slot,ids,categories);daily.append(o.offer(d,slot));ids.add(d['id']);categories.add(d['category'])
            check(len(day['daily'])==3,'Missing daily offers')
            for actual,expected in zip(day['daily'],daily):compare_offer(actual,expected)
            compare_offer(day['weekly'],weekly)
            options=[p for p in daily if p['objective']=='KillCreatures'];selected=min(options,key=lambda p:(-p['reward']['sigilFragments'],p['target'],p['definition'])) if options else None
            check(day['selected']==(selected['definition'] if selected else None),'Different visible choice')
            if selected:selected['status']='Accepted';selected['acceptedAt']=at
            dayindex+=1
        while history['windows'][window]['until']<n:window+=1
        oldlevel=level;oldess=[e['level'] for e in es[:attuned]]
        if outcome=='perfect' or n%5:
            amount=rates[history['windows'][window]['area']];idle+=amount;earned_essence=0
            for e in es[:attuned]:
                remaining=amount
                while remaining and e['level']<10:
                    needed=math.ceil(132860*math.pow(1.02,e['level']-1))-e['currentXp'];grant=min(needed,remaining)
                    e['currentXp']+=grant;remaining-=grant;earned_essence+=grant
                    if grant==needed:e['level']+=1;e['currentXp']=0
            add_character(amount)
            for p in ([selected] if selected else [])+[weekly]:
                if p['status']!='Accepted':continue
                objective=p['objective'];delta=1 if objective in ('KillCreatures','WinEncounters') else earned_essence if objective=='GainEssenceXp' else 0
                if objective=='WinEncounters':check(json.loads(o.by_id[p['definition']].get('objectiveParameterJson','{}'))=={},'Unsupported win filter')
                p['progress']=min(p['target'],p['progress']+delta)
                if p['progress']==p['target']:
                    p['status']='Claimed';period=o.epoch+timedelta(days=week*7) if p is weekly else o.epoch+timedelta(days=dayindex-1)
                    add_claim(p['definition'],objective,p['target'],p['reward'],at,period)
            for m in o.revelation['milestones']:
                key=(week,m['favorRequired'])
                if favor[week]>=m['favorRequired'] and key not in milestones:
                    milestones.add(key);add_claim('revelation.'+str(m['favorRequired']),'Favor',m['favorRequired'],o.empty|m['reward'],at,o.epoch+timedelta(days=week*7))
        if n==quest:attuned=4
        if level!=oldlevel or [e['level'] for e in es[:attuned]]!=oldess:changes.append(dict(encounter=n,state=state()))
        if n in [2160,8640,25920,86400]:
            quantity=items.get('sigil_fragment',0)//10
            if quantity:items['sigil_fragment']-=quantity*10;items['sigil_goblin_mines']+=quantity
            cp=g['checkpoints'][cpindex];cpindex+=1
            check(cp['encounter']==n and cp['state']==state(),'Growth checkpoint XP mismatch')
            check(cp['source']==dict(owner=owner,items=dict(items),unappliedCharacterExperience=0,cinders=cinders,soulstones=soulstones,fateEcho=fate),'Growth source stock mismatch')
    check(dayindex==len(g['days']) and cpindex==len(g['checkpoints']) and g['changes']==changes and g['state']==state(),'Missing growth state/change')
    check(len(claims)==len(g['claims'])==g['duplicateClaimsRejected'],'Claim count/retries mismatch')
    for expected,actual in zip(claims,g['claims']):check(actual|dict(at=offers.dt(actual['at']),periodStart=offers.dt(actual['periodStart']))==expected,'Claim amount or calendar mismatch')
    expected=copy.deepcopy(before);expected['level']=level
    if level>30:expected['baseAttributes']['Power']=10+0.25*(level-1);expected['baseAttributes']['MaxHealth']=140+20*(level-1)
    expected['essences']=[dict(definitionId=e['definition'],level=e['level'],ascensionTier=0,isEvolved=False) for e in es]
    check(g['candidate']==expected,'Grown character attributes, equipment or Essence state mismatch')
    for name,char in [('baselinePrepared',before),('candidatePrepared',expected)]:
        prepared=g[name];check(prepared['level']==char['level'],'Prepared level mismatch')
        check({e['state']['id']:e for e in prepared['equipment']}=={e['data']['state']['id']:e['data'] for e in char['equipment']},'Prepared inventory mismatch')
        check(prepared['essences']==[dict(id=identities[i],essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=0) for i,e in enumerate(char['essences'])],'Prepared Essence identity/level mismatch')
    return dict(owner=owner,horizon=horizon,level=level,essenceLevels=[e['level'] for e in es],idleXp=idle,prophecyXp=prophecy,claims=len(claims),sigils=items['sigil_goblin_mines'],firstGrowth=changes[0]['encounter'] if changes else None)

def mastery_prefix(row,history,archive):
    m=row['masteryOnly'];states={};prefix=[];first=None
    for step in history['steps']:
        family=step['dungeon'];s=states.setdefault(family,dict(dungeonDefinitionId=family,experience=0,level=0,completionCount=0))
        if s['level']:
            first=dict(ordinal=step['ordinal'],family=family,start=dict(dungeonDefinitionId=family,experience=s['experience'],level=s['level'],experienceRequiredForNextLevel=2500,completionCount=s['completionCount']),
                benefits=dict(additionalVisibilityRows=1,restSiteVigorBonus=0,combatVigorCostReduction=0,completionCurrencyBonusPercent=0,equipmentDropChanceBonusPercentagePoints=5),stop='BeforeChangedMasteryEntryNoLaterOutcomeTransferred');break
        run=json.loads(gzip.decompress((archive/step['file']).read_bytes()));rooms={r['roomIndex']:r.copy() for r in run['layout']['rooms']}
        for action in run['actions']:
            check(action['action'] in ['choose_route','fight','rest'] and action['type'] in ['Combat','Boss','MiniBoss','RestSite'],'Unsupported route reconstruction')
            rooms[action['room']]['status']='Completed'
        completed=[r for r in rooms.values() if r['status']=='Completed'];credited=[r for r in completed if r['type']!='Entrance']
        done=run['status']=='Completed';reasons=[]
        if done:reasons.append(dict(id='completion',description='Dungeon completed',experience=100+max(1,len(completed))*5));s['completionCount']+=1
        else:
            reasons.append(dict(id='attempt',description='Dungeon attempt',experience=5))
            if run['failure'] in ['Combat Readiness','Abandonment']:credited=[r for r in credited if r['roomIndex']!=run['actions'][-1]['room']]
            if credited:reasons.append(dict(id='rooms_cleared',description='Rooms cleared',experience=len(credited)*5))
        if any(r['type']=='Boss' for r in credited):reasons.append(dict(id='boss_defeated',description='Boss defeated',experience=50))
        mini=sum(r['type']=='MiniBoss' for r in credited)
        if mini:reasons.append(dict(id='miniboss_defeated',description='Miniboss defeated',experience=mini*25))
        award=sum(r['experience'] for r in reasons);previous=s['level'];s['experience']+=award;s['level']=sum(s['experience']>=t for t in [1000,2500,5000,9000,14000,21000,30000,42000,56000,75000])
        prefix.append(dict(file=step['file'],ordinal=step['ordinal'],award=dict(dungeonDefinitionId=family,experienceAwarded=award,totalExperience=s['experience'],previousLevel=previous,level=s['level'],completionCount=s['completionCount'],reasons=reasons,alreadyAwarded=False,levelsGained=s['level']-previous,maxLevelRewardPreviouslyClaimed=not done,maxLevelRewardWasDeferred=False,unlocksMaxLevelReward=False)))
    check(m['prefix']==prefix and m['firstChange']==first,'Mastery prefix/outcome cutoff mismatch')
    check(m['states']==[states[k] for k in sorted(states)],'Mastery family stock mismatch')
    return dict(runs=len(prefix),changed=first is not None)

def main():
    p=argparse.ArgumentParser();p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    owner=a.owner.absolute();request=read(owner/'request.json');output=Path(request['output']);archive=Path(request['archive'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'New external receipt required')
    check(sha(output/'files.json')==a.manifest_pin,'Wrong manifest pin')
    for name,pin in request['inputHashes'].items():check(sha(Path(name))==pin,'Frozen input drift: '+name)
    manifest=read(output/'files.json');check(set(manifest)=={p.name for p in output.iterdir() if p.name!='files.json'},'Unmanifested output')
    for name,pin in manifest.items():check(sha(output/name)==pin,'Output drift')
    check(sha(archive/'files.json')=='820ecd26646eb4d1b00b59c14db489054dee86d629b2d83427dc47064027e3c5','Historical manifest mismatch')
    oldmanifest=read(archive/'files.json');result=read(output/'result.json');rows=[];masteries=[]
    check(result['status']=='GrowthDependenciesQualifiedNotCombat' and result['newFights']==result['newCombatSeeds']==result['measuredPlayerSamples']==0 and result['preparations']==64,'Invalid qualification scope')
    for name in sorted(n for n in manifest if n!='result.json'):
        row=read(output/name);old=archive/row['historicalFile'];check(sha(old)==row['historicalHash']==oldmanifest[old.name],'Changed historical row')
        history=read(old)
        for step in history['steps']:check(sha(archive/step['file'])==oldmanifest[step['file']],'Changed historical run')
        rows.append(qualification(Path(request['apiRoot']),Path(request['fixtures']),row,history));masteries.append(mastery_prefix(row,history,archive))
    check(len(rows)==len(result['histories'])==32 and len({(r['owner'],r['horizon'],read(output/result['histories'][i]['file'])['growth']['outcome']) for i,r in enumerate(rows)})==32,'Missing histories')
    receipt=dict(status='VerifiedGrowthDependenciesNotCombatOrPlayerPace',manifestPin=a.manifest_pin,inputHashesChecked=len(request['inputHashes']),histories=32,productionPreparationsChecked=64,claimsChecked=sum(r['claims'] for r in rows),masteryPrefixRunsChecked=sum(m['runs'] for m in masteries),changedMasteryEntries=sum(m['changed'] for m in masteries),rows=rows,newFights=0,newCombatSeeds=0,measuredPlayerSamples=0)
    with a.receipt.open('x',encoding='utf-8') as f:json.dump(receipt,f,indent=2);f.write('\n')
    print(json.dumps({k:v for k,v in receipt.items() if k!='rows'},indent=2))

if __name__=='__main__':main()
