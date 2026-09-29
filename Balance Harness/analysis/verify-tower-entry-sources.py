"""Independent calendar, currency, whole-sigil and first-divergence audit; never executes combat."""
import argparse
from collections import Counter, defaultdict
from datetime import datetime, timedelta
from fractions import Fraction
import hashlib
import json
from pathlib import Path

ARCHIVE_PIN='3b4f4870dfc8e10f9b0056788eb006b394ad7618dca963cba1d48d204a453fe7'
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def check(value,message):
    if not value: raise AssertionError(message)
def dt(value): return datetime.fromisoformat(value)
def victories(outcome,n): return n if outcome=='perfect' else n-n//5
def first_victory(outcome,target):
    low=target; high=target*2
    while low<high:
        mid=(low+high)//2
        if victories(outcome,mid)>=target: high=mid
        else: low=mid+1
    return low

def expected_claims(api,plan,outcome,policy,horizon):
    definitions={d['id']:d for file in ['daily.json','weekly.json'] for d in read(api/'Data/prophecies'/file)['definitions']}
    targets=read(api/'Data/prophecies/targets.json')['targets']
    rewards=read(api/'Data/prophecies/rewards.json')
    profiles={p['id']:p for p in rewards['profiles']}
    revelation=read(api/'Data/prophecies/weekly-revelation.json')
    favors={f['scope']:f['amount'] for f in revelation['favorRewards']}
    curve=read(api/'Data/progression/character-experience.json')['characterLevelCurve']
    raw=curve['baseExperience']+curve['linearExperiencePerLevel']*30+curve['quadraticExperiencePerLevelSquared']*900
    xp=(raw+curve['roundingIncrement']-1)//curve['roundingIncrement']*curve['roundingIncrement']
    day=86400//plan['cadenceSeconds'];epoch=dt(plan['epoch']);events=[]
    empty=dict(cinders=0,characterExperience=0,essenceExperience=0,soulstones=0,sigilFragments=0,propheticFavor=0,fateEcho=0,cacheItemId=None,items=[])
    for scope,definition,length in [('Daily',plan['dailyDefinition'],day),('Weekly',plan['weeklyDefinition'],day*7)]:
        if scope=='Weekly' and policy=='daily-common': continue
        d=definitions[definition]
        check(d['objectiveType']=='KillCreatures' and d.get('isEnabled',True) and d.get('minPlayerLevel',1)<=30<=d.get('maxPlayerLevel',30),'Ineligible conditional offer')
        check(not any(d.get(k) for k in ['requiredTags','excludedTags','requiredFeatures']),'Unfunded offer eligibility')
        target=next(t for t in targets if t['scope']==scope and t['objectiveType']==d['objectiveType'])['values'][d['difficulty'].lower()]
        profile=profiles[d['rewardProfileId']]
        check(profile['minimumCinders']==0 and not any(p['scope']==scope and p['category']==d['category'] for p in rewards['categoryPackages']),'Review changed category reward')
        reward=empty|profile['flatReward']|dict(characterExperience=(xp*profile['characterExperience']['nextLevelBasisPoints']+5000)//10000,propheticFavor=favors[scope])
        for start in range(0,horizon,length):
            end=first_victory(outcome,victories(outcome,start)+target)
            if end>min(horizon,start+length): continue
            events.append(dict(source=definition,at=epoch+timedelta(seconds=(end-1)*plan['cadenceSeconds']),
                periodStart=epoch+timedelta(seconds=start*plan['cadenceSeconds']),requiredKills=target,reward=reward))
    result=[];favor=Counter();claimed=set()
    for event in sorted(events,key=lambda e:e['at']):
        result.append(event);week=(event['periodStart']-epoch).days//7
        favor[week]=min(max(m['favorRequired'] for m in revelation['milestones']),favor[week]+event['reward']['propheticFavor'])
        for m in revelation['milestones']:
            key=(week,m['favorRequired'])
            if favor[week]>=m['favorRequired'] and key not in claimed:
                claimed.add(key)
                result.append(dict(source='revelation.'+str(m['favorRequired']),at=event['at'],periodStart=epoch+timedelta(days=week*7),requiredKills=0,reward=empty|m['reward']))
    return result

def economy_catalog(api):
    assembly=read(api/'Data/dungeons/sigil-assembly.json');check(assembly['enabled'] and assembly['fragmentCost']>0,'Disabled assembly')
    quests=[]
    for path in sorted((api/'Data/quests').rglob('*.json')):
        q=read(path)
        for reward in q.get('rewards',[]):
            if reward.get('itemBaseId','').startswith('sigil_'):
                quests.append(dict(quest=q['id'],item=reward['itemBaseId'],quantity=reward['quantity'],availability=q['availability'],objectives=q['objectives'],additionalCredit=0))
    check({q['item'] for q in quests}=={'sigil_goblin_mines','sigil_forgotten_catacombs'} and len(quests)==2,'Review new quest source')
    guild=read(api/'Data/guilds/guild-content.json');market=read(api/'Data/market/champion-market.json')
    shops=[]
    for item in guild['shopItems']:
        amount=sum(r['amount'] for r in item['rewards'] if r['type']=='SigilFragments')
        if amount: shops.append(dict(id=item['key'],currency='GuildFavor',cost=item['guildFavorCost'],fragments=amount,
            weeklyLimit=item['weeklyLimit'],requiredMarketOfficeLevel=item['requiredMarketOfficeLevel'],ownedCurrency=0,affordablePurchases=0,
            reason='No earned Guild Favor, guild membership or funded Market Office in the activity ledger'))
    for item in market['items']:
        if item.get('sigilFragmentsGranted',0): shops.append(dict(id=item['id'],currency='Glory',cost=item['gloryCost'],fragments=item['sigilFragmentsGranted'],
            weeklyLimit=item['weeklyPurchaseLimit'],requiredRankTier=item['requiredRankTier'],ownedCurrency=0,affordablePurchases=0,
            reason='No funded arena/tournament activity or earned Glory in the activity ledger'))
    for shop in shops: check(shop['cost']>0 and shop['fragments']>0 and shop['affordablePurchases']==shop['ownedCurrency']//shop['cost'],'Unfunded purchase')
    caches=[]
    for cache in read(api/'Data/prophecies/caches.json')['caches']:
        dist={0:Fraction(1)};total=sum(r['weight'] for r in cache['rewards'])
        for _ in range(cache['rolls']):
            next_dist=defaultdict(Fraction)
            for held,p in dist.items():
                for entry in cache['rewards']: next_dist[held+entry['reward'].get('sigilFragments',0)]+=p*Fraction(entry['weight'],total)
            dist=dict(next_dist)
        caches.append(dict(item=cache['itemId'],minimum=min(dist),maximum=max(dist),expectedFragments=str(sum(n*p for n,p in dist.items())),
            zeroFragmentProbability=str(dist.get(0,0)),distribution={str(n):str(p) for n,p in sorted(dist.items())},creditedFragments=0))
    return dict(fragmentCost=assembly['fragmentCost'],questsAlreadyCounted=quests,purchases=shops,cachesUnopened=caches,
        excludedSources=['Unobserved prophecy offers/rerolls','Unfunded guild missions and Market Office construction','Unfunded arena/tournament placements','Unspecified events','Unclaimed caches','Prior-activity credit'],
        otherCurrencyConversion='Prophetic Favor, Fate Echo, Guild Favor and Glory are distinct balances; no conversion assumed')

def main():
    p=argparse.ArgumentParser();p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    request=read(a.owner/'request.json');output=Path(request['output']);api=Path(request['apiRoot']);archive=Path(request['archive'])
    check(sha(output/'files.json')==a.manifest_pin,'Wrong manifest pin')
    check(sha(a.owner/'request.json')==read(a.owner/'declaration.json')['requestPin'],'Changed request')
    for name,pin in request['inputHashes'].items(): check(sha(Path(name))==pin,'Changed frozen input: '+name)
    check(sha(archive/'files.json')==ARCHIVE_PIN,'Wrong archive pin')
    manifest=read(output/'files.json');historical_manifest=read(archive/'files.json')
    for name,pin in manifest.items(): check(sha(output/name)==pin,'Changed output: '+name)
    process=read(a.owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0,'Unfinished owner')
    plan=read(Path(request['fixtures'])/'tower-entry-sources.json');catalog=economy_catalog(api)
    totals=Counter();details=[]
    for name in sorted(n for n in manifest if n!='result.json'):
        row=read(output/name);history=read(archive/row['history']);summary=history['summary'];schedule=row['schedule']
        check(sha(archive/row['history'])==row['historicalHash']==historical_manifest[row['history']],'Changed historical ledger')
        check(row['owner']==history['final']['id'] and row['outcome']==summary['outcome'] and row['horizon']==summary['encounters'],'Owner/activity mismatch')
        claims=expected_claims(api,plan,row['outcome'],row['policy'],row['horizon'])
        check(len(claims)==len(schedule['claims'])==schedule['duplicateClaimsRejected'],'Claim/retry count mismatch')
        for expected,observed in zip(claims,schedule['claims']):
            for key in ['source','requiredKills','reward']: check(expected[key]==observed[key],'Changed claim '+key)
            check(expected['at']==dt(observed['at']) and expected['periodStart']==dt(observed['periodStart']),'Retroactive or misdated claim')
        stock=Counter();seen=0;xp=cinders=soulstones=fate=0
        for checkpoint in schedule['checkpoints']:
            end=checkpoint['encounter'];at=dt(plan['epoch'])+timedelta(seconds=end*plan['cadenceSeconds'])
            while seen<len(claims) and claims[seen]['at']<at:
                reward=claims[seen]['reward'];stock['sigil_fragment']+=reward['sigilFragments']
                if reward['cacheItemId']: stock[reward['cacheItemId']]+=1
                for item in reward['items']: stock[item['itemId']]+=item['quantity']
                xp+=reward['characterExperience'];cinders+=reward['cinders'];soulstones+=reward['soulstones'];fate+=reward['fateEcho'];seen+=1
            fragments=stock['sigil_fragment'];assembled=fragments//catalog['fragmentCost']
            stock['sigil_fragment']-=assembled*catalog['fragmentCost']
            if assembled: stock['sigil_goblin_mines']+=assembled
            check(checkpoint['victories']==victories(row['outcome'],end) and checkpoint['claims']==seen,'Changed shared activity')
            check(checkpoint['fragmentsBeforeAssembly']==fragments and checkpoint['assembledNow']==assembled,'Fractional/unfunded assembly')
            check(checkpoint['state']==dict(owner=row['owner'],items=dict(stock),unappliedCharacterExperience=xp,cinders=cinders,soulstones=soulstones,fateEcho=fate),'Personal balance mismatch')
        check([c['encounter'] for c in schedule['checkpoints']]==[e for e in plan['checkpoints'] if e<=row['horizon']],'Missing checkpoint')
        change=None
        for d in history['decisions']:
            step=next((s for s in history['steps'] if s['ordinal']==d['attemptOrdinal'] and s['encounter']==d['encounter']),None)
            character=step['before'] if step else next(c['character'] for c in history['checkpoints'] if c['encounter']==d['encounter'])
            base=Counter(sigil_goblin_mines=int(0<summary['questAt']<=d['encounter']),sigil_forgotten_catacombs=1)
            for w in history['windows']:
                if w['until']<=d['encounter']: base.update(w['sigils'])
            for s in history['steps'][:d['attemptOrdinal']]: base['sigil_'+s['dungeon']]-=1
            check(min(base.values())>=0,'Unfunded archived entry')
            extra=next(c['state']['items'].get('sigil_goblin_mines',0) for c in schedule['checkpoints'] if c['encounter']==d['encounter'])
            reason=d['reason'];family=d['dungeon']
            if reason in ['Enter','NoSigil'] and extra>0: reason='Enter';family='goblin_mines'
            if reason==d['reason'] and family==d['dungeon']: continue
            change=dict(baseline=d,candidate=d|dict(reason=reason,dungeon=family),extraSigils=extra,stock=dict(base+Counter(sigil_goblin_mines=extra)),character=character)
            # Preserve zero balances too, as the native ledger does.
            change['stock']={k:base[k]+(extra if k=='sigil_goblin_mines' else 0) for k in ['sigil_goblin_mines','sigil_forgotten_catacombs']}
            break
        observed=row['firstChange'];check((change is None)==(observed is None),'Wrong first changed decision')
        if change:
            for key,value in change.items(): check(observed[key]==value,'Changed divergence field: '+key)
            check(observed['stop']=='FirstChangedEntryDecisionNoOutcomeTransferred','Outcome extrapolation')
            expected=sorted([e['data'] for e in observed['character']['equipment']],key=lambda e:e['state']['id'])
            check(sorted(observed['preparedEquipment'],key=lambda e:e['state']['id'])==expected,'Prepared equipment descriptor mismatch')
            totals['prepared']+=1
        check(row['projectedCombatOutcomes']==0 and row['historicalSupplies']==summary['successes'],'Transferred combat result')
        totals['histories']+=1;totals['claims']+=len(claims)
        details.append(dict(history=summary['historyKey'],policy=row['policy'],horizon=row['horizon'],extraSigils=stock['sigil_goblin_mines'],remainingFragments=stock['sigil_fragment'],
            firstChange=None if change is None else dict(encounter=change['baseline']['encounter'],attempt=change['baseline']['attemptOrdinal'],oldDungeon=change['baseline']['dungeon'],newDungeon=change['candidate']['dungeon'],oldReason=change['baseline']['reason']),
            historicalSupplies=summary['successes']))
    result=read(output/'result.json')
    check(totals['histories']==len(result['histories'])==64 and totals['prepared']==result['preparations'],'Result totals mismatch')
    check(result['newFights']==result['newCombatSeeds']==result['measuredPlayerSamples']==0,'Unexpected samples')
    receipt=dict(status='VerifiedConditionalEntryAffordabilityNotPlayerPace',manifestPin=a.manifest_pin,inputHashesChecked=len(request['inputHashes']),
        histories=totals['histories'],claimsChecked=totals['claims'],productionPreparationsChecked=totals['prepared'],newFights=0,newCombatSeeds=0,measuredPlayerSamples=0,
        sourceCatalog=catalog,details=details)
    with a.receipt.open('x',encoding='utf-8') as f: json.dump(receipt,f,indent=2);f.write('\n')
    print(json.dumps({k:v for k,v in receipt.items() if k not in ['sourceCatalog','details']},indent=2))

if __name__=='__main__': main()
