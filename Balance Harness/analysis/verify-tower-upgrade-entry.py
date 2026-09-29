"""Independent audit of personal checkpoint conservation and conditional native entry preparation."""
import argparse
from collections import Counter
from datetime import datetime
import importlib.util
from pathlib import Path
import sys

sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('upgrade_entry_owner',Path(__file__).with_name('run-tower-upgrade-entry.py'))
owner_module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=owner_module;spec.loader.exec_module(owner_module)
io=owner_module.io
def at(value):return datetime.fromisoformat(value)
def items(values):return {e['state']['id']:e for e in values}
def check(value,message):io.check(value,message)


def audit(q,directory):
    growth=Path(q['growthArchive']);party=Path(q['partyArchive']);unlock=Path(q['unlockArchive'])
    for folder,pin in [(growth,owner_module.GROWTH_PIN),(party,owner_module.PARTY_PIN),(unlock,owner_module.UNLOCK_PIN)]:
        check(io.sha(folder/'files.json')==pin,'Changed historical manifest')
        manifest=io.read(folder/'files.json')
        for path in folder.iterdir():
            if path.name!='files.json':check(io.sha(path)==manifest[path.name],'Changed historical member')
    points={p['history']:p for p in io.read(party/'points.json') if p['policy']=='earned-progression' and p['horizon']==25920}
    check(len(points)==32,'Incomplete personal source population')
    result=io.read(directory/'result.json');plan=io.read(Path(q['fixtures'])/'tower-upgrade-entry.json')
    check(result['version']==owner_module.VERSION and result['status']=='FundedUpgradeEntriesQualifiedNotExecuted' and result['plan']==plan,'Changed result/plan')
    check(result['newFights']==result['newCombatSeeds']==result['actualEntries']==result['earnedEquipment']==result['measuredPlayerSamples']==0
        and result['preparations']==512,'Qualification misrepresented as acquisition/combat')
    personal={};files={'result.json'};mastery_runs=0;removed=0;inventory_owners=set()
    for row in result['personal']:
        key=row['history'];check(key in points and key not in personal,'Unexpected/duplicate personal state')
        p=points[key];owner=p['character']['id'];inventory_owners.add(owner);filename=key+'--checkpoint.json'
        check(row==dict(file=filename,history=key,owner=owner,outcome=p['outcome']),'Wrong personal summary');files.add(filename)
        proof=io.read(directory/filename);cp=proof['checkpoint'];h=io.read(growth/(key+'--history.json'))
        check(proof['historicalFile']==key+'--history.json' and proof['historicalHash']==io.sha(growth/proof['historicalFile']),'Unbound history')
        check(cp['point']==p and p['encounter']==p['horizon']==25920,'Changed personal point')
        last=next(x for x in h['checkpoints'] if x['encounter']==25920);steps=[s for s in h['steps'] if s['encounter']<=25920]
        ordinals={s['ordinal'] for s in steps};entries=[e for e in h['progression']['entries'] if e['ordinal'] in ordinals]
        state=next(c['state'] for c in h['progression']['checkpoints'] if c['encounter']==25920)
        if any(s['encounter']==25920 for s in steps):state=entries[-1]['after']
        check(cp['state']==state and cp['steps']==steps and cp['entries']==entries,'Reset/future growth or dungeon prefix')
        check(at(state['at'])==at(p['availableAt']) and state['source']['owner']==owner and state['growth']['level']==p['character']['level'],'Wrong owner/clock/level')
        check(last['character']==p['character'],'Changed historical equipped loadout')
        for field in ['claims','days','dungeonEvents']:
            check(cp[field]==[r for r in h['progression'][field] if at(r['at'])<=at(p['availableAt'])],'Dropped/future source event: '+field)
        windows=[w for w in h['windows'] if w['until']<=25920]
        inventory=dict(state['source']['items']);reconciliation=[]
        for family in ['goblin_mines','forgotten_catacombs']:
            item='sigil_'+family;quest=1 if family=='forgotten_catacombs' or h['summary']['questAt']<=25920 else 0
            idle=sum(w['sigils'].get(item,0) for w in windows)
            assembled=sum(c['assembled'] for c in h['progression']['checkpoints'] if c['encounter']<=25920) if family=='goblin_mines' else 0
            spent=sum(s['dungeon']==family for s in steps);remaining=quest+idle+assembled-spent
            check(remaining>=0 and remaining==last['sigils'][item] and inventory.get(item,0)==assembled,'Sigil grant/spend conservation failed')
            inventory[item]=remaining;removed+=assembled
            reconciliation.append(dict(item=item,quest=quest,idle=idle,assembled=assembled,spent=spent,remaining=remaining))
        check(cp['sigilReconciliation']==reconciliation,'Wrong reconciliation evidence')
        owned=list(h['starting'])+[i for w in windows for i in w['equipment']]
        for s,e in zip(steps,entries):
            check(s['ordinal']==e['ordinal'],'Crossed entry prefix')
            if s['award']:owned.append(s['award'])
            owned+=s['dungeonLoot']['equipment']
            for item,quantity in s['dungeonLoot']['blueprints'].items():inventory[item]=inventory.get(item,0)+quantity
            receipt=e['receipt']
            check(receipt['pendingExperience']+receipt['lostExperience']==sum(r['experience'] for r in receipt['rooms']),'XP conservation failed')
            check(receipt['appliedExperience']==(receipt['pendingExperience'] if s['status']=='Completed' else 0),'Unclaimed XP credited')
        check(cp['inventory']==inventory and all(v>=0 for v in inventory.values()),'Duplicated/lost inventory or blueprint items')
        check(len(owned)==len(items(owned))==len(p['owned']) and items(owned)==items(p['owned']) and all(i['state']['ownership']['ownerId']==owner for i in owned),'Invented/lost/foreign item')
        check(proof['pendingDungeonExperience']==0 and proof['prophecyRuntimeRehydrated'] is False,'Unproved resumable runtime claim')
        expected_mastery=[];states={}
        for s,e in zip(steps,entries):
            check(io.sha(growth/s['file'])==io.read(growth/'files.json')[s['file']],'Changed historical mastery run')
            award=e['receipt']['mastery'];expected_mastery.append(dict(file=s['file'],award=award));mastery_runs+=1
            states[s['dungeon']]=dict(dungeonDefinitionId=s['dungeon'],experience=award['totalExperience'],level=award['level'],completionCount=award['completionCount'])
        check(proof['masteryPrefix']==expected_mastery and proof['mastery']==[states[k] for k in sorted(states)],'Reset mastery prefix')
        check(proof['retainedBlueprintProgress']==(steps[-1]['dungeonLoot']['after'] if steps else []),'Reset blueprint pity')
        personal[key]=proof
    check(set(personal)==set(points) and len(inventory_owners)==16,'Incomplete personal alternatives')
    expected_servers={r['key'] for r in io.read(unlock/'result.json')['journeys']};servers=set();counts=Counter();grouped=Counter();prepared_by_owner={};prepared_count=0;per_server=[]
    for row in result['servers']:
        key=row['key'];check(key in expected_servers and key not in servers,'Wrong/duplicate server');servers.add(key)
        check(row['file']==key+'--entries.json','Wrong server file');files.add(row['file']);proof=io.read(directory/row['file'])
        j=io.read(unlock/(key+'--journey.json'));state=j['final'];members={r['owner'] for r in j['unchangedOwnedCharacterHashes']}
        check(proof['key']==key and proof['serverFile']==key+'--journey.json' and proof['serverHash']==io.sha(unlock/proof['serverFile']) and proof['serverState']==state,'Changed earned server state')
        selected_points={k:p for k,p in points.items() if p['outcome']==j['outcome']};seen=set();local=Counter()
        for row in proof['entries']:
            name=row['history'];check(name in selected_points and name not in seen,'Missing/foreign/repeated server owner');seen.add(name)
            cp=personal[name]['checkpoint'];p=cp['point'];c=p['character'];n=row['native'];stock=cp['inventory'];participant=c['id'] in members
            check(row['participant']==participant and n['owner']==c['id'] and n['highestCleared']==j['highestCleared'],'Server participation/ownership drift')
            check(at(n['startsAt'])==max(at(p['availableAt']),at(state['at'])),'Future owner or credited idle waiting')
            slots={e['slot']:e for e in c['equipment']};missing=[s for s in ['Head','Chest','Legs','Necklace','Ring','Relic','MainHand'] if s not in slots]
            two=slots.get('MainHand',{}).get('data',{}).get('equipmentType')=='TwoHanded'
            if not two and 'OffHand' not in slots:missing.append('OffHand')
            family=next((f for f in ['goblin_mines','forgotten_catacombs'] if stock.get('sigil_'+f,0)>0),None)
            reason='EquipmentCoverage' if missing else 'NoSigil' if family is None else 'Enter';selected=family if reason=='Enter' else None
            check(n['policy']=='full-slot-ready-mines-first-next-entry-v1' and n['reason']==reason and n['selectedDungeon']==selected and n['missingSlots']==missing,'Changed entry policy')
            after=dict(stock)
            if selected:after['sigil_'+selected]-=1
            check(n['inventoryBefore']==stock and n['inventoryAfterHypotheticalEntry']==after and n['affordableAttempts']==sum(stock.get('sigil_'+f,0) for f in ['goblin_mines','forgotten_catacombs']),'Invented funding/debit')
            check(n['awardedEquipment']==n['actualEntries']==0,'Unexecuted preview counted as earned')
            access=n['nativeAccess'];check(len(access)==2 and {a['family'] for a in access}=={'goblin_mines','forgotten_catacombs'},'Missing native access evaluation')
            for a in access:
                item='sigil_'+a['family'];r=a['result'];check(a['costs']==[dict(itemId=item,amount=1)] and r['canEnter']==(stock.get(item,0)>=1),'Native affordability mismatch')
                check(len(r['entryRequirements'])==1 and r['entryRequirements'][0]['itemId']==item and r['entryRequirements'][0]['requiredAmount']==1
                    and r['entryRequirements'][0]['ownedAmount']==stock.get(item,0),'Native entry quantity mismatch')
            supply=n['prospectiveSupply'];epic=j['highestCleared']>=3
            check(supply['itemBaseId']=='item.tower_supply.v1.floor_'+('04' if epic else '01') and supply['tier']==supply['sourceRegion']==1 and supply['minimumLevel']==30,'Wrong earned supply eligibility')
            check(supply['band']==dict(rarity='Epic' if epic else 'Rare',quality='Fine' if epic else 'Standard',rank=3 if epic else 2),'Wrong repeating band')
            prepared=n['prepared'];check(prepared['level']==c['level'] and items(prepared['equipment'])==items([e['data'] for e in c['equipment']]),'Changed native prepared inventory/level')
            check(prepared['essences']==[dict(id=e['id'],essenceDefinitionId=e['definition'],level=e['level'],ascensionTier=e['ascensionTier']) for e in cp['state']['growth']['essences']],'Changed earned Essence IDs/training')
            if name in prepared_by_owner:check(prepared_by_owner[name]==prepared,'Server unlock changed character stats')
            else:prepared_by_owner[name]=prepared
            prepared_count+=1;counts[reason]+=1;local[reason]+=1;grouped[(epic,participant,reason)]+=1
        check(seen==set(selected_points) and len(seen)==16,'Incomplete server population')
        per_server.append(dict(key=key,highestCleared=j['highestCleared'],**local))
    check(servers==expected_servers and prepared_count==512 and set(io.read(directory/'files.json'))==files,'Incomplete qualification output')
    return dict(status='VerifiedFundedUpgradeEntriesNotExecuted',personalStates=32,distinctOwners=16,serverStates=32,preparations=512,
        nativeAccessChecks=1024,historicalMasteryRuns=mastery_runs,assembledGrantReceiptsReconciled=removed,
        reasons=dict(counts),groups=[dict(epicEligible=k[0],participant=k[1],reason=k[2],owners=v) for k,v in sorted(grouped.items())],
        servers=per_server,newFights=0,newCombatSeeds=0,earnedEquipment=0,measuredPlayerSamples=0,prophecyRuntimeRehydrated=False)


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True)
    a=p.parse_args();owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');output=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'Fresh external audit receipt required')
    check(d==dict(version=owner_module.VERSION,requestPin=io.sha(owner/'request.json'),maximumSeconds=300,maximumNativeSeconds=240,
        maximumBytes=256*1048576,newSeeds=0,maximumFights=0,retries=0,personalStates=32,serverStates=32,preparations=512),'Changed owner envelope')
    for name,pin in q['inputHashes'].items():check(io.sha(Path(name))==pin,'Frozen input drift')
    io.manifest(output,a.manifest_pin);check(sum(p.stat().st_size for p in output.iterdir())<=d['maximumBytes'],'Output cap')
    receipt=io.read(owner/'process.json');check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0 and receipt['seconds']<=301,'Incomplete process')
    result=audit(q,output);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']))
    io.write(a.receipt,result);print(__import__('json').dumps({k:v for k,v in result.items() if k!='servers'},indent=2))


if __name__=='__main__':main()
