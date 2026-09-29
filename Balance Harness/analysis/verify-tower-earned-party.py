"""Independent receipt, ownership, chronology, party-layout and bounded combat audit."""
import argparse
from collections import Counter, defaultdict
from datetime import datetime, timedelta, timezone
import importlib.util
import math
from pathlib import Path
import struct
import sys

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('earned_owner', Path(__file__).with_name('run-tower-earned-party.py'))
io = importlib.util.module_from_spec(spec); sys.modules[spec.name] = io; spec.loader.exec_module(io)
check = io.check


def items(values): return {e['state']['id']: e for e in values}
def at(value): return datetime.fromisoformat(value)
def single(value): return struct.unpack('<f',struct.pack('<f',value))[0]
def selected(values, costs):
    # EquipmentSlotType declaration order, not the traversal used to find candidate pieces.
    slots = ['Head','Relic','Chest','Necklace','Legs','Ring','MainHand','OffHand']
    def score(item): return sum(struct.unpack('<f',struct.pack('<f',value))[0]*costs[key] for key,value in item['stats'].items())
    def best(kind):
        candidates = [i for i in values if i['equipmentType'] == kind]
        return min(candidates,key=lambda i:(-score(i),i['state']['id'])) if candidates else None
    result = {kind:best(kind) for kind in slots[:6]}
    one, off, two = [best(k) for k in ['OneHanded','OffHand','TwoHanded']]
    if two is not None and (one is None or score(two)>score(one)+(score(off) if off else 0)): result['MainHand']=two
    elif one is not None: result.update(MainHand=one,OffHand=off)
    return [dict(slot=k,data=result[k]) for k in slots if result.get(k) is not None]


def qualification(q, directory):
    archive = Path(q['archive']); check(io.sha(archive/'files.json') == io.ARCHIVE_PIN, 'Changed acquisition history pin')
    historical = io.read(archive/'files.json')
    costs = io.read(archive/'rules.json')['scores']
    histories = {}
    for name,pin in historical.items():
        if name.endswith('--history.json'):
            check(io.sha(archive/name)==pin,'Changed earned history')
            h=io.read(archive/name); histories[h['summary']['key']]=h
    check(len(histories)==64,'Missing source history')
    points = io.read(directory/'points.json'); index={}; personal_owners=set(); stopped=0
    for p in points:
        h=histories[p['history']]; summary=h['summary']
        check(p['horizon'] in [2160,8640,25920,86400], 'Undeclared horizon')
        cp=[c for c in h['checkpoints'] if c['encounter']<=p['horizon']][-1]; encounter=cp['encounter']
        check(encounter==p['horizon'] or (encounter==summary['encounters'] and summary['completed']), 'Unobserved forward activity')
        stopped += encounter<p['horizon']
        check(p['encounter']==encounter and p['character']==cp['character'], 'Changed earned character/Essences/attributes')
        for key in ['policy','outcome','recipe','path']: check(p[key]==summary[key],'Cross-history point')
        check(summary['questAt']<=encounter, 'Unearned fourth Essence')
        owned=list(h['starting'])
        for w in h['windows']:
            if w['until']<=encounter: owned.extend(w['equipment'])
        steps=[s for s in h['steps'] if s['encounter']<=encounter]
        for s in steps:
            if s['award']: owned.append(s['award'])
            owned.extend(s['dungeonLoot']['equipment'])
        check(len(items(owned))==len(owned)==len(p['owned']) and items(owned)==items(p['owned']), 'Invented, lost or duplicated owned item')
        owner=p['character']['id']; personal_owners.add(owner)
        check(all(i['state']['ownership']['ownerId']==owner for i in owned),'Cross-owner inventory')
        check(p['character']['equipment']==selected(owned,costs),'Changed deterministic retained-gear selection')
        check(p['supplyItems']==cp['supplyItems']==sum(s['award'] is not None for s in steps),'Wrong supply prefix')
        instant=datetime(2026,9,28,tzinfo=timezone.utc)+timedelta(seconds=encounter*10+sum(s['combatSeconds'] for s in steps))
        check(abs((at(p['availableAt'])-instant).total_seconds())<1e-6,'Activity overlap or invented acquisition time')
        if encounter==summary['encounters']:
            check(items(owned)==items(h['owned']) and p['character']==h['final'],'Changed terminal inventory')
            check(abs((instant-at(h['progression']['final']['at'])).total_seconds())<1e-6,'Serial journey clock mismatch')
        key=(p['policy'],p['outcome'],p['recipe'],p['path'],p['horizon'])
        check(key not in index,'Duplicate personal checkpoint'); index[key]=p
    check(len(points)==256 and len(personal_owners)==16,'Wrong checkpoint/owner count')
    fixtures=Path(q['fixtures']); plan=io.read(fixtures/'tower-earned-party.json')
    floors={f['floorNumber']:f for f in io.read(Path(q['apiRoot'])/'Data/world-tower/tower-floors.json')['floors']}
    budgets={b['priorityFloor']:b for b in io.read(fixtures/'tower-progression-budget-cycle.json')['budgets']}
    summary=io.read(directory/'result.json'); parties={}; prepared_members=0; ranges=defaultdict(list)
    check(summary['status']=='EarnedPartiesPreparedNotFloorProgression' and summary['newFights']==summary['newCombatSeeds']==summary['measuredPlayerSamples']==0,'Invalid qualification claim')
    expected={(policy,outcome,r,h,f) for policy in ['fixed-progression','earned-progression'] for outcome in ['perfect','four-of-five']
        for r in range(4) for h in [2160,8640,25920,86400] for f in (range(1,12) if h==86400 else [1])}
    for row in summary['parties']:
        proof=io.read(directory/row['file']); p=proof['party']; f=p['floor']['floorNumber']; floor=floors[f]
        key=(p['policy'],p['outcome'],p['rotation'],p['horizon'],f)
        check(key in expected and key not in parties,'Unexpected/repeated party'); parties[key]=p
        count=floor['requiredSlots']; roster=plan['roster'][:count]
        members=[index[(p['policy'],p['outcome'],s['recipe'],(p['rotation']+s['pathOffset'])%4,p['horizon'])] for s in roster]
        check(p['members']==members and len({m['character']['id'] for m in members})==count,'Changed roster or cloned owner')
        all_items=[e['state']['id'] for m in members for e in m['owned']]
        check(len(set(all_items))==len(all_items),'Shared/donated items')
        check(at(p['startsAt'])==max(at(m['availableAt']) for m in members),'Invented assembly activity')
        for field in ['floorNumber','requiredSlots','guardianCreatureId','stagger']:
            check(p['floor'][field]==floor[field],'Changed production guardian/layout: '+field)
        # Production TowerGuardianScalingDefinition stores Single values; JSON emits their shortest round-trip forms.
        check({k:single(v) for k,v in p['floor']['guardianScaling'].items()}=={k:single(v) for k,v in floor['guardianScaling'].items()},'Changed guardian scaling')
        check(proof['laterFloorUnlocksAssumed']==(f>1) and proof['liveAccountEligibilityVerified'] is False,'Unverified eligibility presented as measured')
        check(proof['budget']==budgets[f],'Changed independent budget')
        friendly=[x for x in proof['prepared'] if x['slot']['side']=='Friendly']
        hostile=[x for x in proof['prepared'] if x['slot']['side']=='Hostile']
        check(len(friendly)==count and len(hostile)==1,'Wrong production runtime size')
        check(hostile[0]['slot']['sourceEntityId']==floor['guardianCreatureId']
              and hostile[0]['sourceMonsterId']==floor['guardianAbilityProfileId'],'Wrong guardian or ability profile')
        for i,(m,prepared,gap) in enumerate(zip(members,friendly,proof['gaps'])):
            c=m['character']; slot=prepared['slot']
            check(slot['sourceEntityId']==c['id'] and slot['slotId']==c['id'] and slot['partyNumber']==i//5+1,'Wrong native slot/account owner')
            check(prepared['level']==c['level'] and prepared['baseAttributes']==c['baseAttributes'],'Changed prepared growth/attributes')
            eq={e['data']['state']['id']:e['data'] for e in c['equipment']}
            check(items(prepared['equipment'])==eq and len(prepared['equipment'])==len(eq),'Changed prepared equipment')
            check(prepared['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in c['essences']],'Changed prepared Essence order/training')
            check(gap==dict(slot=i+1,owner=c['id'],levelShortfall=max(0,budgets[f]['characterLevel']-c['level']),
                essenceShortfall=max(0,budgets[f]['essenceSlots']-len(c['essences'])),targetTier=budgets[f]['tier'],
                equippedTiers=[e['data']['state']['tier'] for e in c['equipment']]),'Incorrect budget dependency')
            prepared_members+=1
        check(row['preparedHash']==proof['preparedHash'] and row['supplies']==sum(m['supplyItems'] for m in members),'Summary mismatch')
        if f==1: ranges[(p['policy'],p['horizon'])].append(dict(supplies=row['supplies'],minimumLevel=row['minimumLevel'],maximumLevel=row['maximumLevel']))
    check(set(parties)==expected and len(parties)==224,'Incomplete party qualification')
    carried=0
    for policy in ['fixed-progression','earned-progression']:
        for outcome in ['perfect','four-of-five']:
            for r in range(4):
                ten=parties[(policy,outcome,r,86400,10)]['members']; eleven=parties[(policy,outcome,r,86400,11)]['members']
                check(ten[:10]==eleven and len(ten)==15,'Floor 10 to 11 lost or reset retained gear')
                carried+=sum(len(m['character']['equipment']) for m in eleven)
    return dict(status='VerifiedEarnedPartyPreparation',personalCheckpoints=256,distinctOwners=16,stoppedHistoryCheckpoints=stopped,
        partyPreparations=224,preparedMembers=prepared_members,retainedFloorTenToElevenEquippedReferences=carried,
        floorOneRanges=[dict(policy=k[0],horizon=k[1],minimumSupplies=min(r['supplies'] for r in v),maximumSupplies=max(r['supplies'] for r in v),
            minimumLevel=min(r['minimumLevel'] for r in v),maximumLevel=max(r['maximumLevel'] for r in v)) for k,v in sorted(ranges.items())],
        newFights=0,newCombatSeeds=0,measuredPlayerSamples=0)


def combat(q, directory, owner):
    source=Path(q['qualification']); io.manifest(source,q['qualificationPin'])
    source_result=io.read(source/'result.json'); expected={r['file'] for r in source_result['parties'] if r['floor']==1}
    values=[s for panel in q['panels'].values() for s in panel]; ledger=io.read(owner/'seed-ledger.json')
    old, _, _=io.exclusions()
    check(len(q['panels'])==32 and all(len(v)==16 for v in q['panels'].values()) and len(set(values))==len(values)==512,'Wrong fixed panel')
    check(ledger['reserved']==values and not set(values).intersection(old) and ledger['exclusionUnionCount']==877447,'Seed reuse or missing reservations')
    result=io.read(directory/'result.json'); files={}; rows=[]; groups=defaultdict(Counter); used=set(); replays=0
    for file in sorted(directory.glob('*--trials.json')):
        record=io.read(file); ref=record['qualificationFile']; check(ref in expected and ref not in files,'Repeated/unqualified party')
        files[ref]=file; proof=io.read(source/ref); p=proof['party']
        check(record['qualificationHash']==io.sha(source/ref) and record['preparedHash']==proof['preparedHash'],'Preparation not bound to trials')
        panel=q['panels'][f"{p['outcome']}--{p['rotation']}--{p['horizon']}"]
        check([t['seed'] for t in record['trials']]==panel,'Wrong trial panel')
        friendly={m['character']['id'] for m in p['members']}
        for t in record['trials']:
            s=t['summary']; used.add(t['seed'])
            check(s['contentOutcome'] in ['Victory','Defeat','Draw'] and t['succeeded']==(s['contentOutcome']=='Victory'),'Outcome mismatch')
            check(0<=s['durationTicks']<=6000 and abs(s['durationSeconds']-s['durationTicks']/10)<1e-9,'Wrong tick accounting')
            check({c['id'] for c in s['friendly']}==friendly and len(s['hostile'])==1,'Wrong combat participants')
            if t['succeeded']: check(t['guardianHealthRemainingPercent']==0,'Victory without defeated guardian')
        replay=io.read(directory/(p['id']+'--replay.json')); check(replay==record['trials'][0],'Nondeterministic playback'); replays+=1
        wins=sum(t['succeeded'] for t in record['trials']); rows.append(dict(party=p['id'],wins=wins,trials=16))
        group=groups[(p['policy'],p['horizon'])]; group.update(wins=wins,trials=16,parties=1)
    check(set(files)==expected and len(files)==64 and replays==64 and used==set(values),'Incomplete combat accounting')
    check(result['status']=='EarnedFloorOneDiagnosticComplete' and result['fights']==1088 and result['replays']==64
          and result['newSeeds']==512 and not result['searchPerformed'] and result['measuredPlayerSamples']==0,'Wrong diagnostic claims')
    check(sorted(result['parties'],key=lambda r:r['party'])==sorted(rows,key=lambda r:r['party']),'Wrong win summary')
    return dict(status='VerifiedEarnedFloorOneDiagnostic',qualificationPin=q['qualificationPin'],partyPanels=64,fights=1088,replays=64,
        newCombatSeeds=512,reservedUnconsumedSeeds=0,exclusionUnionCount=877447,measuredPlayerSamples=0,
        groups=[dict(policy=k[0],horizon=k[1],**v) for k,v in sorted(groups.items())])


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--owner',type=Path,required=True); p.add_argument('--manifest-pin',required=True); p.add_argument('--receipt',type=Path,required=True)
    a=p.parse_args(); owner=a.owner.absolute(); q=io.read(owner/'request.json'); d=io.read(owner/'declaration.json'); output=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'New audit receipt outside archive required')
    check(d['version']==io.VERSION and io.sha(owner/'request.json')==d['requestPin'],'Changed declaration/request')
    for file,pin in q['inputHashes'].items(): check(io.sha(Path(file))==pin,'Frozen input changed')
    io.manifest(output,a.manifest_pin)
    check(sum(f.stat().st_size for f in output.iterdir())<=d['maximumBytes']==256*1048576,'Output bound violated')
    process=io.read(owner/'process.json'); check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0,'Incomplete process')
    check(d['retries']==0 and d['measuredPlayerSamples']==0,'Unbounded or misrepresented study')
    prepare=d['mode']=='prepare'
    check(d['mode'] in ['prepare','combat'] and d['maximumSeconds']==(300 if prepare else 600)
        and d['maximumNativeSeconds']==(240 if prepare else 540) and d['maximumFights']==(0 if prepare else 1088)
        and d['newSeeds']==(0 if prepare else 512),'Changed bounded envelope')
    result=qualification(q,output) if d['mode']=='prepare' else combat(q,output,owner)
    result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']))
    io.write(a.receipt,result); print(__import__('json').dumps(result,indent=2))


if __name__=='__main__': main()
