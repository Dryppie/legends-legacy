"""Audit full native continuation snapshots against independently archived personal witnesses; no combat."""
import argparse
from collections import Counter
from datetime import datetime, timezone
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('runtime_owner',Path(__file__).with_name('run-tower-runtime.py'))
owner_module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=owner_module;spec.loader.exec_module(owner_module)
io=owner_module.io
check=io.check

def at(s):return datetime.fromisoformat(s).astimezone(timezone.utc)
def ticks(s):
    # Keep the seventh fractional digit: native waiting uses exact 100ns timestamps.
    date=s[:19];tail=s[19:];fraction=tail[1:].split('+')[0].split('Z')[0] if tail.startswith('.') else ''
    return int((at(date+'+00:00')-datetime(1970,1,1,tzinfo=timezone.utc)).total_seconds())*10000000+int((fraction+'0000000')[:7])
def reward(s):
    def lower(v):
        if isinstance(v,dict):return {k[0].lower()+k[1:]:lower(x) for k,x in v.items()}
        if isinstance(v,list):return [lower(x) for x in v]
        return v
    return lower(json.loads(s))

def audit(q,output):
    entry=Path(q['entryArchive']);growth=Path(q['growthArchive']);prior=io.read(entry/'result.json');result=io.read(output/'result.json')
    check(io.sha(entry/'files.json')==owner_module.ENTRY_PIN and io.sha(growth/'files.json')==owner_module.GROWTH_PIN,'Predecessor pins')
    for folder in [entry,growth]:
        manifest=io.read(folder/'files.json')
        for p in folder.iterdir():
            if p.name!='files.json':check(io.sha(p)==manifest[p.name],'Changed used archive member')
    check(result['version']==owner_module.VERSION and result['status']=='ContinuationRuntimeQualified','Runtime result')
    check(result['plan']==io.read(Path(q['fixtures'])/'tower-continuation-runtime.json'),'Frozen plan')
    check(result['newFights']==result['newSeeds']==result['actualEntries']==result['earnedEquipment']==result['measuredPlayerSamples']==0,'Qualification is not acquisition')
    check(result['branches']==result['preparations']==512 and result['replayedIdleEncounters']==829440,'Population/idle bound')
    personal={};instances_count=guards=runs=excluded_offers=0;statuses=Counter();weekly_objectives=Counter();files={'result.json','branches.json'}
    for row in result['personal']:
        name=row['history'];check(name not in personal,'Duplicate runtime');file=name+'--runtime.json';files.add(file)
        r=io.read(output/file);proof=io.read(entry/r['checkpointFile']);cp=proof['checkpoint'];p=cp['point'];state=cp['state']
        check(r['history']==name and r['checkpointHash']==io.sha(entry/r['checkpointFile']) and r['prophecyRuntimeRehydrated'] is True,'Unbound runtime')
        check(r['owned']==p['owned'] and r['character']==p['character'] and r['retainedBlueprintProgress']==proof['retainedBlueprintProgress'],'Inventory/pity drift')
        a=r['replayRuntime'];b=r['canonicalRuntime'];check(a['growth']==b['growth'] and a['mastery']==b['mastery'],'Reconciliation changed growth/mastery')
        excluded=[d for d in cp['days'] if at(d['at'])==at(p['availableAt'])]
        check(r['excludedBoundaryOffers']==excluded,'Incorrect same-instant boundary correction');excluded_offers+=len(excluded)
        cp=dict(cp,days=[d for d in cp['days'] if at(d['at'])<at(p['availableAt'])])
        for key in ['days','dungeonEvents']:
            check(a[key]==b[key]==cp[key],'Historical event/offer drift')
        check(a['idleSeconds']==b['idleSeconds']==state['idleSeconds']==259200 and a['combatTicks']==b['combatTicks']==state['combatTicks'],'Clock reset')
        check(a['waitingTicks']==b['waitingTicks']==0 and ticks(state['at'])==ticks('2026-09-28T00:00:00+00:00')+259200*10000000+a['combatTicks']*1000000,'Replayed clock')
        g=a['growth'];check(g['owner']==p['character']['id'] and g['state']==state['growth'] and g['dungeonExperience']==state['dungeonExperience'],'Earned growth drift')
        check(g['attuned']==len(g['state']['essences']) and g['ownedEssences'][:g['attuned']]==g['state']['essences'],'Essence training/order')
        check({x['attributeType']:x['value'] for x in g['baseAttributes']}==p['character']['baseAttributes'],'Native base attributes')
        for x in a['mastery']:
            m=next(m for m in proof['mastery'] if m['dungeonDefinitionId']==x['dungeonDefinitionId'])
            check(all(x[k]==v for k,v in m.items()) and x['characterId']==g['owner'],'Mastery witness')
            last=next(s for s in reversed(cp['steps']) if s['dungeon']==x['dungeonDefinitionId'])
            check(x['lastAwardedRunId']==last['dungeonLoot']['runId'],'Mastery duplicate guard lost')
        s=a['sources'];t=b['sources'];check(s['state']==state['source'] and not s['resourcesReconciled'] and t['resourcesReconciled'],'Source receipts')
        check(t['state']==dict(state['source'],items=cp['inventory']),'Canonical resources')
        check({k:v for k,v in s.items() if k not in ['state','resourcesReconciled']}=={k:v for k,v in t.items() if k not in ['state','resourcesReconciled']},'Reconciliation changed prophecy runtime')
        check(s['claims']==cp['claims'] and s['duplicateClaimsRejected']==len(cp['claims']),'Claim prefix')
        check(len(s['instances'])==len({i['id'] for i in s['instances']}),'Duplicate instances')
        expected_offers={};selections={}
        for day in cp['days']:
            period=at(day['at']).date().isoformat();selections[period]=day['selected']
            for offer in day['daily']:expected_offers[(period,offer['slot'])]=offer
            expected_offers[(at(day['weekly']['acceptedAt']).date().isoformat(),'Greater')]=day['weekly']
        actual_offers={}
        for i in s['instances']:
            check(i['characterId']==i['playerId']==g['owner'],'Foreign prophecy')
            key=(at(i['periodStart']).date().isoformat(),i['slotType']);check(key not in actual_offers and key in expected_offers,'Unexpected instance period/slot');actual_offers[key]=i
            offer=expected_offers[key];check(i['prophecyDefinitionId']==offer['definition'] and i['targetValue']==offer['target'] and reward(i['rewardSnapshotJson'])==offer['reward'],'Changed frozen offer/reward')
            matching=[c for c in cp['claims'] if c['source']==i['prophecyDefinitionId'] and at(c['periodStart'])==at(i['periodStart'])]
            check(len(matching)<=1 and (i['status']=='Claimed')==bool(matching),'Claim flag reset')
            if matching:
                check(at(i['claimedAt'])==at(matching[0]['at']) and i['currentValue']==i['targetValue'],'Claim state mismatch')
            check(0<=i['currentValue']<=i['targetValue'],'Progress bounds')
            if key[1]!='Greater':check((i['acceptedAt'] is not None)==(selections[key[0]]==i['prophecyDefinitionId']),'Daily choice reset')
            else:
                weekly_objectives[i['prophecyDefinition']['objectiveType']]+=1
                check(i['currentValue']>=offer['progress'] and i['acceptedAt']==offer['acceptedAt'],'Weekly progress reset')
                if i['prophecyDefinition']['objectiveType']=='ClearDungeonRooms':
                    check(i['currentValue']==min(i['targetValue'],sum(e['amount'] for e in cp['dungeonEvents'] if e['kind']=='DungeonRoomCleared')),'Weekly room event progress')
            statuses[i['status']]+=1
        check(set(actual_offers)==set(expected_offers),'Lost offers')
        for week in s['weeks']:
            check(week['characterId']==week['playerId']==g['owner'],'Foreign weekly progress')
            claims=[c for c in cp['claims'] if at(week['periodStart'])<=at(c['at'])<at(week['periodEnd'])]
            check(week['propheticFavor']==sum(c['reward']['propheticFavor'] for c in claims),'Weekly favor reset')
            for threshold in [3,5,7]:check(week['milestone'+str(threshold)+'Claimed']==any(c['source']=='revelation.'+str(threshold) for c in claims),'Milestone claim reset')
        for reroll in s['rerolls']:
            check(reroll['characterId']==reroll['playerId']==g['owner'] and all(reroll[k]==0 for k in ['rerollsUsed','freeRerollsUsed','paidRerollsUsed','fateEchoSpent']),'Unfunded reroll')
        check(a['observedDay']==b['observedDay'] and at(a['observedDay']).date()==at(cp['days'][-1]['at']).date(),'Observation day reset')
        check(a['overview']==b['overview'],'Active overview reset')
        for i in a['overview']['dailyProphecies']+[a['overview']['greaterProphecy']]:check(i==next(x for x in s['instances'] if x['id']==i['id']),'Detached stale overview')
        check(r['claimGuards']==row['claimGuards']==len(cp['claims']),'Missing claim guards')
        check(not r['discardedProbe']['retained'] and r['discardedProbe']['state']['idleSeconds']==259220,'Probe retained or wrong size')
        personal[name]=r;guards+=r['claimGuards'];runs+=len(cp['steps']);instances_count+=len(s['instances'])
    check(set(personal)=={r['history'] for r in prior['personal']} and len(personal)==32 and guards==result['claimGuards'],'Incomplete personal qualification')
    expected={}
    for server in prior['servers']:
        source=io.read(entry/server['file'])
        for e in source['entries']:expected[(source['key'],e['history'])]=e
    branches=io.read(output/'branches.json');seen=set();reasons=Counter()
    for b in branches:
        key=(b['server'],b['history']);check(key not in seen and key in expected,'Duplicate/foreign branch');seen.add(key);e=expected[key]
        check(b['native']==e['native'] and b['participant']==e['participant'] and b['at']==e['native']['startsAt'],'Prepared/access/clock parity')
        cp=io.read(entry/personal[b['history']]['checkpointFile'])['checkpoint']
        check(b['waitingTicks']==ticks(b['at'])-ticks(cp['point']['availableAt'])>=0,'Waiting minted time or lost precision');reasons[b['native']['reason']]+=1
    check(set(expected)==seen and len(branches)==512 and set(io.read(output/'files.json'))==files,'Incomplete branch/output population')
    return dict(status='VerifiedContinuationRuntime',personalStates=32,branches=512,preparations=512,replayedIdleEncounters=829440,
        historicalDungeonRuns=runs,restoredInstances=instances_count,excludedFutureDayObservations=excluded_offers,instanceStatuses=dict(statuses),weeklyObjectives=dict(weekly_objectives),
        claimGuards=guards,entryReasons=dict(reasons),newFights=0,newSeeds=0,actualEntries=0,earnedEquipment=0,measuredPlayerSamples=0)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True)
    a=p.parse_args();owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');output=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'Fresh external receipt')
    check(d==dict(version=owner_module.VERSION,requestPin=io.sha(owner/'request.json'),maximumSeconds=300,maximumNativeSeconds=240,
        maximumBytes=256*1048576,newSeeds=0,maximumFights=0,retries=0,personalStates=32,serverStates=32,preparations=512),'Owner envelope')
    for name,pin in q['inputHashes'].items():check(io.sha(Path(name))==pin,'Frozen input drift')
    io.manifest(output,a.manifest_pin);check(sum(p.stat().st_size for p in output.iterdir())<=d['maximumBytes'],'Output cap')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=301,'Incomplete process')
    result=audit(q,output);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']))
    io.write(a.receipt,result);print(json.dumps(result,indent=2))

if __name__=='__main__':main()
