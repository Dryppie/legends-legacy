"""Independently audit paid completion prefixes, exact core probabilities, native probes and unchanged ownership."""
import argparse
from collections import Counter
from functools import lru_cache
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import sys
import uuid
sys.dont_write_bytecode=True

def module(name,file):
    spec=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));value=importlib.util.module_from_spec(spec);sys.modules[name]=value;spec.loader.exec_module(value);return value
owner_module=module('core_source_owner','run-tower-core-source.py');io=owner_module.io;check=io.check
prior=module('core_source_prior','verify-tower-return.py');equipment=prior.equipment;ticks=prior.native.ticks_at

@lru_cache(maxsize=512)
def read(path):return io.read(Path(path))

def history(q,key,o):
    rows=[];seen=set();owner=o['point']['character']['id'];now=prior.instant(o['runtime'])
    def add(archive,file,character,family,receipt,paid,run_id):
        path=Path(archive)/file;r=read(str(path));success=r['status']=='Completed'
        check(character['id']==owner and r['character']==character['name']==o['point']['character']['name'] and r['dungeon']==family,'Foreign source')
        check(r['status'] in ['Completed','Failed'] and paid==r['sigilsConsumed']==1 and r['entryItems']=={'sigil_'+family:1},'Unpaid source')
        check(r['completionCallbacks']==int(success) and receipt['appliedExperience']==(receipt['pendingExperience'] if success else 0),'Invalid historical claim')
        expected_id=str(uuid.UUID(bytes_le=hashlib.sha256('\x1f'.join(['dungeon-acquisition-run-v1',owner,family,str(r['layoutSeed'])]).encode()).digest()[:16]))
        check(run_id==expected_id,'Changed source run ID')
        first=success and family not in seen
        if success:seen.add(family)
        rows.append(dict(archive=Path(archive).name,file=file,hash=io.sha(path),runId=run_id,family=family,status=r['status'],started=receipt['started'],ended=receipt['ended'],combatSeconds=r['combatSeconds'],sigilsPaid=1,firstCompletion=first))
    cp=read(str(Path(q['entryArchive'])/(o['history']+'--checkpoint.json')))['checkpoint']
    check(cp['point']['history']==o['history'] and cp['point']['character']['id']==owner and cp['point']['encounter']==cp['point']['horizon']==25920,'Wrong checkpoint')
    # Check the archived prefix against its original full history, not just against our output.
    h=read(str(Path(q['historyArchive'])/(o['history']+'--history.json')))
    check(cp['steps']==[s for s in h['steps'] if s['encounter']<=25920],'Changed original prefix')
    for s in cp['steps']:
        e=next(e for e in h['progression']['entries'] if e['ordinal']==s['ordinal'])
        add(q['historyArchive'],s['file'],s['before'],s['dungeon'],e['receipt'],s['sigilsBefore']-s['sigilsAfter'],s['dungeonLoot']['runId'])
    for channel in ['waveArchive','minesArchive']:
        archive=Path(q[channel]);matching=[r for r in read(str(archive/'result.json'))['results'] if r['server']==key and r['history']==o['history']]
        check(len(matching)<=1,'Duplicated wave owner')
        for r in matching:
            if 'progressFile' not in r:continue
            p=read(str(archive/r['progressFile']));family=p['native']['selectedDungeon'];item='sigil_'+family
            check(p['costs']=={item:1} and p['before']['sources']['state']['items'][item]-p['afterProgress']['sources']['state']['items'][item]==1,'Wave sigil debit drift')
            add(archive,p['runFile'],p['entry'],family,p['receipt'],1,p['ordinary']['runId'])
    check(len({r['runId'] for r in rows})==len(rows),'Duplicated source run')
    previous=0
    for r in rows:
        check(previous<=ticks(r['started'])<=ticks(r['ended'])<=now and r['combatSeconds']>0,'Future or reordered source');previous=ticks(r['ended'])
    expected=Counter(r['family'] for r in rows if r['status']=='Completed')
    actual={m['dungeonDefinitionId']:m['completionCount'] for m in o['runtime']['mastery']}
    check(all(expected[f]==actual.get(f,0) for f in set(expected)|set(actual)),'Retained mastery disagrees')
    return rows

def probability(successes,first):
    weights={6*first:1}
    for _ in range(successes):
        after=Counter()
        for c,w in weights.items():
            for add,multiple in [(3,3),(4,4),(5,4),(6,1)]:after[c+add]+=w*multiple
        weights=after
    return {c:w/12**successes for c,w in sorted(weights.items())}

def qualify(q,output):
    result=io.read(output/'result.json');source=read(str(Path(q['sourceArchive'])/'result.json'))
    check(result['version']==owner_module.VERSION and result['status']=='CoreSourcesQualifiedNotCredited','Wrong result')
    check(result['plan']==io.read(Path(q['fixtures'])/'tower-core-source.json'),'Plan drift')
    check(not result['plan']['historicalDrawsRecorded'] and result['plan']['repeatCoreDistribution']=={'3':.25,'4':1/3,'5':1/3,'6':1/12},'Wrong source distribution')
    totals=Counter();summary=[];files={'result.json','native-completion-probes.json'};xp_values=[];funding=Counter()
    for sr in source['journeys']:
        key=sr['key'];file=key+'--sources.json.gz';files.add(file);p=io.read(output/file);old=read(str(Path(q['sourceArchive'])/(key+'--continuation.json.gz')))
        check(p['key']==key and p['sourceFile']==key+'--continuation.json.gz' and p['sourceHash']==io.sha(Path(q['sourceArchive'])/p['sourceFile']),'Unbound latest state')
        check(p['retained']==old,'Rewrote retained owners, gear, XP, claims, clocks, server state or attempts')
        check(len(p['qualified'])==len(old['owners'])==16,'Missing owners')
        for r,o in zip(p['qualified'],old['owners']):
            check(r['owner']==o['point']['character']['id'] and r['history']==o['history'],'Cross-owner projection')
            runs=history(q,key,o);check(r['runs']==runs,'Source ledger mismatch')
            n=sum(x['status']=='Completed' for x in runs);f=sum(x['firstCompletion'] for x in runs)
            check((r['successes'],r['failures'],r['firstFamilies'])==(n,len(runs)-n,f),'Wrong completion totals')
            distribution=probability(n,f);lo,hi=min(distribution),max(distribution);p_all=sum(p for c,p in distribution.items() if c>=30)
            project=r['projection'];check([(p['cores']) for p in project['distribution']]==list(distribution),'Wrong probability support')
            check(all(abs(p['probability']-distribution[p['cores']])<1e-12 for p in project['distribution']),'Wrong probability mass')
            expected=dict(minimum=lo,maximum=hi,mean=n*4.25+f*6,distribution=project['distribution'],allFiveCoreCost=30,probabilityAllFive=project['probabilityAllFive'],
                minimumAffordable=min(5,lo//6),maximumAffordable=min(5,hi//6),minimumAdditionalRepeatSuccesses=max(0,math.ceil((30-hi)/6)),guaranteedAdditionalRepeatSuccesses=max(0,math.ceil((30-lo)/3)))
            check(project==expected and abs(project['probabilityAllFive']-p_all)<1e-12,'Incorrect affordability')
            check(o['runtime']['sources']['state']['items'].get('item.monster_core.lesser',0)==0,'Projection became stock')
            native=r['native'];es=o['runtime']['growth']['ownedEssences'];check(len(native['essences'])==len(es)==5,'Missing training controls')
            for saved,proof in zip(es,native['essences']):
                check(saved['ascensionTier']==0 and saved['level'] in [1,8],'Changed source Essence')
                xp=sum(math.ceil(132860*1.02**(l-1)) for l in range(saved['level'],10))-saved['currentXp'];xp_values.append(xp)
                check(proof==dict(id=saved['id'],definition=saved['definition'],level=saved['level'],currentXp=saved['currentXp'],xpRequired=xp,currentRejected=True,oneXpShortRejected=True,fiveCoresRejected=True,sixCoresConsumed=6,ascensionTier=1,retryRejected=True),'Native training/ascension guard drift')
            check(native['foreignOwnerRejected'] and native['debits']==native['notifications']==5 and not native['probeRetained'],'Probe spending escaped or incomplete')
            check(len(native['retainedOwnerHash'])==64 and native['xpSource']=='hypothetical boundary probe only; no earned activity or player time credited','Unlabeled XP source')
            totals.update(owners=1,paidAttempts=len(runs),successfulCompletions=n,failedAttempts=len(runs)-n,firstFamilies=f,guaranteedCoreBudgets=int(lo>=30),possibleCoreBudgets=int(hi>=30),retainedEquipment=len(o['point']['owned']))
            funding[f'{lo}..{hi}']+=1
        party=old['party'];actors=[a for a in p['prepared'] if a['slot']['side']=='Friendly'];check(len(actors)==10 and len(p['prepared'])==11,'Preparation actor count')
        for i,(m,a) in enumerate(zip(party['members'],actors)):
            c=m['character'];check(a['slot']['sourceEntityId']==c['id'] and a['slot']['partyNumber']==i//5+1 and a['level']==c['level'] and a['baseAttributes']==c['baseAttributes'],'Prepared identity drift')
            check(equipment.items(a['equipment'])==equipment.items([e['data'] for e in c['equipment']]),'Prepared gear lost')
            check(a['essences']==[dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in c['essences']],'Probe became combat upgrade')
        totals['historicalAttempts']+=len(old['historicalAttempts']);summary.append(dict(key=key,owners=16))
    check(result['journeys']==summary and len(summary)==15 and result['owners']==240,'Incorrect population')
    for k in ['owners','paidAttempts','successfulCompletions','failedAttempts','guaranteedCoreBudgets','possibleCoreBudgets']:check(result[k]==totals[k],'Wrong aggregate: '+k)
    check(result['held']==source['held'] and len(result['held'])==17,'Lost held servers')
    for h in result['held']:check(io.sha(Path(q['returnArchive'])/h['file'])==h['hash'],'Held archive changed')
    for k in ['newResourcesCredited','newExperience','newAscensions','newFights','newSeeds','measuredPlayerSamples']:check(result[k]==0,'Unadmitted activity: '+k)
    check(not result['searchPerformed'],'Unadmitted search')
    native=io.read(output/'native-completion-probes.json');check(native['completionMarks']==4 and not native['probeRetained'] and len(native['results'])==4,'Incomplete completion probes')
    for p,(family,ordinal) in zip(native['results'],[(f,i) for f in ['forgotten_catacombs','goblin_mines'] for i in range(2)]):
        check(p['family']==family and p['ordinal']==ordinal and p['firstCompletion']==(ordinal==0) and p['duplicateClaimRejected'] and not p['probeRetained'],'Native first/repeat guard drift')
        grants={r['source']:r['quantity'] for r in p['pending']};check(all(r['itemId']=='item.monster_core.lesser' for r in p['pending']),'Wrong item')
        check(3<=grants['Grade I Monster Cores']<=6 and grants.get('Grade I First Completion',0)==(6 if ordinal==0 else 0),'Invalid native draw')
        check(p['total']==p['treasureProgress']==p['claimed']==sum(grants.values()),'Pending/claim/treasure conservation failed')
    check(set(io.read(output/'files.json'))==files,'Unexpected output')
    return dict(status='VerifiedCoreSourcesNotCredited',**totals,coreBudgetRanges=dict(sorted(funding.items())),minimumXpDeficit=min(xp_values),maximumXpDeficit=max(xp_values),
        nativeAscensionProbes=1200,nativeCompletionProbes=4,preparedMembers=150,heldServers=17,ownersPreserved=512,newFights=0,newSeeds=0,measuredPlayerSamples=0)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--owner',type=Path,required=True);p.add_argument('--manifest-pin',required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    owner=a.owner.absolute();q=io.read(owner/'request.json');d=io.read(owner/'declaration.json');output=Path(q['output'])
    check(not a.receipt.exists() and not a.receipt.absolute().is_relative_to(output),'Fresh external audit receipt required')
    check(d['version']==owner_module.VERSION and d['requestPin']==io.sha(owner/'request.json'),'Declaration drift')
    for file,pin in q['inputHashes'].items():check(io.sha(Path(file))==pin,'Frozen drift: '+file)
    for key,(_,pin) in owner_module.ARCHIVES.items():io.manifest(Path(q[key]),pin)
    owner_module.exclusions(Path(d['latestSeedLedger']));check(d['latestSeedLedgerPin']==owner_module.LEDGER_PIN and d['exclusionUnionCount']==885164,'Lost seeds')
    io.manifest(output,a.manifest_pin);check(sum(f.stat().st_size for f in output.iterdir())<=d['maximumBytes']==256*1048576,'Output cap')
    check(d['maximumSeconds']==900 and d['maximumNativeSeconds']==840 and d['maximumFights']==d['newSeeds']==d['retries']==0 and d['maximumOwners']==240,'Changed cap')
    process=io.read(owner/'process.json');check(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0 and process['seconds']<=900+process['cleanupAllowanceSeconds'],'Incomplete process')
    result=qualify(q,output);result.update(manifestPin=a.manifest_pin,inputHashesChecked=len(q['inputHashes']));io.write(a.receipt,result);print(json.dumps(result,indent=2))

if __name__=='__main__':main()
