"""Independent audit of paid entries, native continuation, rewards, retained gear, preparation and bounded combat."""
import argparse
from collections import Counter
import copy
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode=True
def module(name,file):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
owner_module=module('funded_owner','run-tower-next-entry.py');io=owner_module.io;check=io.check
run_audit=module('funded_run','verify-dungeon-acquisition.py').audit_run
equipment=module('funded_equipment','verify-tower-earned-party.py')
loot_module=module('funded_loot','audit-dungeon-equipment-loot.py')
growth=module('funded_growth','audit-tower-continuation.py')

def audit(q,output,owner):
    qualification=Path(q['qualification']);entry=Path(q['entryArchive']);api=Path(q['apiRoot'])
    io.manifest(qualification,owner_module.RUNTIME_PIN);io.manifest(entry,owner_module.ENTRY_PIN)
    check(q['qualificationPin']==owner_module.RUNTIME_PIN,'Admission drift')
    costs=io.read(Path(q['scoringRules']))['scores'];select=lambda values:equipment.selected(values,costs)
    score=lambda values:sum(equipment.single(v)*costs[k] for x in select(values) for k,v in x['data']['stats'].items())
    branches={(b['server'],b['history']):b for b in io.read(qualification/'branches.json')}
    personal={r['history']:io.read(qualification/r['file']) for r in io.read(qualification/'result.json')['personal']}
    result=io.read(output/'result.json');check(result['version']==owner_module.VERSION and result['status']=='FundedNextEntriesComplete','Result status')
    check(result['plan']==io.read(Path(q['fixtures'])/'tower-funded-next-entry.json'),'Changed declared policy')
    check(result['attempts']==192 and result['replays']==12 and result['personalPanels']==12 and result['pairedServerAlternatives'] and result['reservedSeeds']==780 and result['measuredPlayerSamples']==0,'Wrong sample claims')
    latest=Path(io.read(owner/'seed-ledger.json')['preceding'][-1]['archive']);excluded,earlier=owner_module.exclusions(latest)
    ledger=io.read(owner/'seed-ledger.json');reserved=[s for p in q['panels'].values() for s in [p['layoutSeed'],*p['roomSeeds']]]
    check(len(reserved)==len(set(reserved))==780 and reserved==ledger['reserved'] and not excluded.intersection(reserved)
        and ledger['exclusionUnionCount']==878611,'Seed conservation')
    files={'result.json'};seen=set();first={};used=set();fights=prepared=attempts=ordinary_count=blueprints=opened=retained=0;counts=Counter();groups=Counter();growth_claims=0
    loot_auditor=loot_module.LootAuditor(api)
    def battle(file,panel,character,mastery):
        nonlocal fights,prepared
        files.add(file);run=io.read(output/file);run_audit(run,panel,mastery)
        fights+=len(run['battles']);used.add(panel['layoutSeed']);used.update(b['seed'] for b in run['battles'])
        for b in run['battles']:
            friendly=[p for p in b['preparedParticipants'] if p['slot']['side']=='Friendly'];check(len(friendly)==1,'Party injected into solo dungeon')
            f=friendly[0];check(f['slot']['sourceEntityId']==character['id'] and equipment.items(f['equipment'])==equipment.items([e['data'] for e in character['equipment']]),'Prepared equipment/owner drift')
            check(f['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in character['essences']],'Prepared Essence drift')
            check(f['level']==character['level'],'Prepared level drift');prepared+=1
        return run
    for row in result['results']:
        key=(row['server'],row['history']);check(key in branches and key not in seen,'Duplicate/foreign branch');seen.add(key)
        b=branches[key];native=b['native'];p=personal[row['history']];cp=io.read(entry/p['checkpointFile'])['checkpoint'];counts[row['reason']]+=1
        check(row['reason']==native['reason'] and row['participant']==b['participant'],'Decision/participation drift')
        if row['reason']!='Enter':
            check(row['actualEntries']==row['newEquipment']==0 and row['runtimeHash']==b['runtimeHash'],'Held branch changed');continue
        attempts+=1;check(row['actualEntries']==1,'Wrong paid entry count');files.add(row['progressFile']);proof=io.read(output/row['progressFile'])
        check(proof['server']==key[0] and proof['history']==key[1] and proof['native']==native and proof['entry']==cp['point']['character'],'Wrong entry witness')
        before=copy.deepcopy(p['canonicalRuntime']);before['waitingTicks']=b['waitingTicks'];check(proof['before']==before,'Restored runtime changed before spend')
        panel=q['panels'][key[1]];family=panel['dungeon'];check(family==native['selectedDungeon'],'Source switch')
        mastery=next((m['level'] for m in before['mastery'] if m['dungeonDefinitionId']==family),0)
        run=battle(proof['runFile'],panel,proof['entry'],mastery)
        check(run['status']==row['status'] and run['combatSeconds']==row['combatSeconds'] and run['entryItems']==proof['costs']=={'sigil_'+family:1},'Wrong run summary/cost')
        check(before['sources']['state']['items']['sigil_'+family]>=1,'Unfunded run')
        if key[1] in first:check(first[key[1]]==run,'Paired identical combat differs')
        else:
            first[key[1]]=run
            check(battle(key[1]+'--replay.json.gz',panel,proof['entry'],mastery)==run,'Full-run replay differs')
        check(growth.audit(api,Path(q['fixtures']),proof,run)==mastery,'Native growth reference mismatch')
        growth_claims+=len(proof['afterProgress']['sources']['claims'])-len(before['sources']['claims'])
        loot=proof['ordinary'];loot_auditor.audit(loot,proof['entry']['id'],run,p['retainedBlueprintProgress'],mastery,resolved_routes=True)
        owned=cp['point']['owned']+loot['equipment'];ordinary_count+=len(loot['equipment']);blueprints+=sum(loot['blueprints'].values())
        supply=proof['supply'];completed=run['status']=='Completed';chest=native['prospectiveSupply']['itemBaseId'];pending=supply['pending']
        check(supply['claimed']==({chest:1} if completed else {}) and len(pending)==int(completed) and supply['retryChecks']==2,'Native supply claim/guard mismatch')
        if completed:
            rid=loot_module.identity(['tower-equipment-supply',proof['entry']['id'].replace('-',''),loot['runId'].replace('-','')])
            check(pending[0]['id']==rid and pending[0]['itemId']==chest and pending[0]['quantity']==1 and pending[0]['source']=='tower-equipment-supply','Supply decision/identity')
        targets=proof['candidateTargets'];check(len(targets)==(7 if completed else 0),'Missing fixed-order choices')
        order=io.read(Path(q['fixtures'])/'tower-bootstrap.json')['purchaseOrder']
        target_slots=['MainHand' if t['equipmentType'] in ['OneHanded','TwoHanded'] else t['equipmentType'] for t in targets]
        check(not completed or target_slots==order,'Changed purchase order')
        for t in targets:
            check(t['rarity']==native['prospectiveSupply']['band']['rarity'] and t['state']['rank']==native['prospectiveSupply']['band']['rank']
                and t['quality']==native['prospectiveSupply']['band']['quality'] and t['state']['tier']==1 and t['state']['ownership']['ownerId']==proof['entry']['id'],'Wrong repeating supply band')
        chosen=next((t for t in targets if score(owned+[t])>score(owned)),None)
        check(supply['opened']==(chosen is not None) and row['supplyOpened']==supply['opened'],'Non-beneficial chest opening')
        if chosen is not None:
            award=supply['equipment'];expected=copy.deepcopy(chosen)
            expected['state']['id']=loot_module.identity(['tower-next-entry-award-v1',proof['entry']['id'],loot['runId'],chest]);expected['state']['provenance']['awardId']=loot['runId']
            check(award==expected and supply['choice']==chosen['state']['definitionId'],'Wrong selected native equipment');owned.append(award);opened+=1
        else:check(supply['equipment'] is None and supply['choice'] is None,'Unopened supply minted gear');retained+=int(completed)
        check(proof['owned']==owned and len(owned)==len(equipment.items(owned)) and all(i['state']['ownership']['ownerId']==proof['entry']['id'] for i in owned),'Owned inventory loss/duplication/transfer')
        check(proof['character']['equipment']==select(owned) and row['loadoutChanged']==(proof['character']['equipment']!=proof['entry']['equipment']),'Wrong retained-gear selection')
        check(row['newEquipment']==len(loot['equipment'])+int(supply['opened']) and row['suppliedChest']==(chest if completed else None),'Reward summary')
        expected=copy.deepcopy(proof['afterProgress']);stock=expected['sources']['state']['items']
        for item,quantity in (list(loot['blueprints'].items())+list(supply['claimed'].items())):stock[item]=stock.get(item,0)+quantity
        if supply['opened']:stock[chest]-=1
        check(proof['after']==expected and all(v>=0 for v in stock.values()),'Claimed resource conservation')
        check(proof['assemblyAfterEntry']==proof['additionalEntries']==proof['measuredPlayerSamples']==0,'Unmodeled extra activity')
        prepared_final=proof['next']['prepared'];check(equipment.items(prepared_final['equipment'])==equipment.items([e['data'] for e in proof['character']['equipment']]) and prepared_final['level']==proof['character']['level'],'Final production preparation')
        check(prepared_final['essences']==[dict(id=e['id'],essenceDefinitionId=e['definition'],level=e['level'],ascensionTier=e['ascensionTier']) for e in proof['after']['growth']['state']['essences']],'Final Essence identities')
        check(proof['next']['inventoryBefore']==stock,'Final funding preview drift')
        groups[(chest,row['participant'],run['status'],row['loadoutChanged'])]+=1
    for control in result['controls']:
        name=control['history'];check(name in first and not control['earned'],'Unqualified/earned supplied control')
        c=control['character'];check(all(e['data']['rarity']=='Epic' for e in c['equipment']),'Weak supplied control')
        run=battle(name+'--control.json.gz',q['panels'][name],c,control['mastery']);check(run['status']==control['status'] and run['combatSeconds']==control['combatSeconds'],'Control summary')
    check(set(branches)==seen and attempts==192 and len(first)==len(result['controls'])==12 and fights==result['fights']<=13824,'Population/count mismatch')
    check(set(result['usedSeeds'])==used<=set(reserved) and set(io.read(output/'files.json'))==files,'Used seeds/output manifest mismatch')
    return dict(status='VerifiedFundedNextEntries',combinations=512,attempts=attempts,personalPanels=12,replays=12,controls=12,fights=fights,
        preparedRooms=prepared,finalPreparations=192,entryReasons=dict(counts),ordinaryItems=ordinary_count,blueprintItems=blueprints,
        openedSupplies=opened,retainedSupplies=retained,newProphecyClaims=growth_claims,
        groups=[dict(chest=k[0],participant=k[1],status=k[2],loadoutChanged=k[3],combinations=v) for k,v in sorted(groups.items())],
        reservedSeeds=780,usedSeeds=len(used),unusedSeeds=780-len(used),exclusionUnionCount=878611,measuredPlayerSamples=0)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True)
    a=p.parse_args();owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');output=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'Fresh external audit receipt')
    check(d==dict(version=owner_module.VERSION,requestPin=io.sha(owner/'request.json'),maximumSeconds=900,maximumNativeSeconds=840,
        maximumFights=13824,maximumBytes=256*1048576,newSeeds=780,retries=0,combinations=512,attempts=192,controls=12,replays=12,personalPanels=12),'Owner envelope drift')
    for name,pin in q['inputHashes'].items():check(io.sha(Path(name))==pin,'Frozen input drift')
    io.manifest(output,a.manifest_pin);check(sum(p.stat().st_size for p in output.iterdir())<=d['maximumBytes'],'Output cap')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=901,'Incomplete owned process')
    result=audit(q,output,owner);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']))
    io.write(a.receipt,result);print(json.dumps(result,indent=2))

if __name__=='__main__':main()
