"""Qualify retained five-Essence Tower history and own a bounded floor-five return."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time

sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('fifth_return_io',Path(__file__).with_name('run-tower-earned-party.py'))
io=importlib.util.module_from_spec(spec);sys.modules[spec.name]=io;spec.loader.exec_module(io)
VERSION='tower-earned-fifth-return-v1'
ARCHIVES={
    'sourceArchive':('tower-pending-mines-combat-study-20260929','963fd340840d7b1f7d9f3e16eef9f57a97fec4445b322ef7d157e6adbd5f37c7'),
    'returnArchive':('tower-return-combat-study-20260929','9b9cff072ac42afe75c17485015bc792f34bae5580f5cf789e40bf3fafc2e6be')}
LEDGER_PIN='1b65df2ca57ab132f4ab35a6915b3663696a184fa65f171b93a2cbbc77c28d3c'



def exclusions(latest):
    io.check(io.sha(latest)==LEDGER_PIN,'Changed latest reservations');ledger=io.read(latest)
    def pinned(entry):
        io.check(io.sha(Path(entry['archive']))==entry['sha256'],'Changed reservation ancestor')
        return io.read(Path(entry['archive']))
    h,d=pinned(ledger['historical']),pinned(ledger['dungeon'])
    values=set(h['historical']+h['first']+h['second']+d['reserved'])
    for entry in ledger['preceding']:values.update(pinned(entry)['reserved'])
    values.update(ledger['reserved']);io.check(len(values)==885104,'Incomplete predecessor union')
    return values,ledger


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--mode',choices=['prepare','combat'],required=True)
    for name in ['owner','output','artifacts']:p.add_argument('--'+name,type=Path,required=True)
    p.add_argument('--qualification-owner',type=Path);p.add_argument('--qualification-pin');a=p.parse_args()
    owner,output,artifacts=[v.absolute() for v in [a.owner,a.output,a.artifacts]]
    io.check(owner.parent==output.parent==artifacts.parent==ROOT/'TestResults','Direct TestResults children required')
    io.check(owner.name.startswith('tower-fifth-return-') and not owner.exists() and not output.exists(),'Fresh return owner/output required')
    if a.mode=='combat':io.check(not list((ROOT/'TestResults').glob('tower-fifth-return-*/seed-ledger.json')),'Do not rename or extend an admitted panel')
    if a.mode=='prepare':io.check(not list((ROOT/'TestResults').glob('tower-fifth-return-preparation-*/completion.json')),'Do not repeat completed qualification')
    prior=ROOT/'TestResults/tower-pending-mines-combat-owner-20260929';latest=prior/'seed-ledger.json';excluded,earlier=exclusions(latest)
    compatibility=io.read(prior/'source-compatibility.json')
    for name,pin in compatibility.items():io.check(io.sha(ROOT/name)==pin,'Changed production input: '+name)
    audit=io.read(prior/'independent-audit.json')
    io.check(audit['status']=='VerifiedPendingMinesCombat' and audit['manifestPin']==ARCHIVES['sourceArchive'][1],'Audited funded predecessor required')
    for _,(name,pin) in ARCHIVES.items():io.manifest(ROOT/'TestResults'/name,pin)
    qualification=None
    if a.mode=='combat':
        io.check(a.qualification_owner and a.qualification_pin,'Explicit independent preparation audit required')
        qo=a.qualification_owner.absolute();qualification=Path(io.read(qo/'request.json')['output']);io.manifest(qualification,a.qualification_pin)
        qa=io.read(qo/'independent-audit.json')
        io.check(qa['status']=='VerifiedEarnedFifthReturnPrepared' and qa['manifestPin']==a.qualification_pin and qa['parties']==15,'Wrong preparation admission')
    owner.mkdir();inputs={};sources={}
    def freeze(path):
        if str(path) in sources:return
        dest=owner/'inputs'/path.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);pin=io.sha(path)
        with path.open('rb') as src,dest.open('xb') as dst:shutil.copyfileobj(src,dst)
        io.check(io.sha(path)==io.sha(dest)==pin,'Capture drift');inputs[str(dest)]=pin;sources[str(path)]=dict(archive=str(dest),sha256=pin)
    for folder in ['LL/src/API/API.LL/Data','LL/tools/BalanceHarness/Fixtures']:
        for path in sorted((ROOT/folder).rglob('*.json')):freeze(path)
    freeze(ROOT/'LL/src/API/API.LL/appsettings.json')
    for folder in ['LL/tools/BalanceHarness','LL/src/Core/Common','LL/src/Core/Domain','LL/src/Core/Application',
                   'LL/src/Infrastructure/Service/Services.LL','LL/src/Infrastructure/Persistence/Persistence.LL']:
        for path in sorted((ROOT/folder).rglob('*.cs')):
            if not {'bin','obj'}.intersection(path.relative_to(ROOT).parts):freeze(path)
    for path in sorted(Path(__file__).parent.glob('*.py')):freeze(path)
    for name,_ in ARCHIVES.values():
        for path in sorted((ROOT/'TestResults'/name).iterdir()):freeze(path)
    scoring=ROOT/'TestResults/tower-growing-activity-study-20260929/rules.json'
    io.check(io.sha(scoring.parent/'files.json')==io.ARCHIVE_PIN and io.sha(scoring)==io.read(scoring.parent/'files.json')[scoring.name],'Scoring witness drift')
    for path in [scoring,scoring.parent/'files.json',latest,prior/'independent-audit.json',ROOT/'build/run-tests.ps1',ROOT/'build/bounded_windows_process.py',
        *[ROOT/'LL/tests/EssenceSystem.Tests'/name for name in ['WorldTowerServiceTests.cs','WorldTowerTitleTests.cs','TowerUnlockServer.cs','TowerReturnServer.cs','BalanceHarnessFifthReturnTests.cs','TowerExpansionServer.cs','BalanceHarnessFifthReturnTests.cs','BalanceHarnessFifthEssenceTests.cs']]]:freeze(path)
    if qualification:
        for path in [*sorted(qualification.iterdir()),qo/'independent-audit.json',qo/'request.json']:freeze(path)
    for path in sorted((artifacts/'bin/EssenceSystem.Tests/release').iterdir()):
        if path.is_file() and (path.suffix=='.dll' or path.name.endswith(('.deps.json','.runtimeconfig.json'))):freeze(path);inputs[str(path)]=io.sha(path)
    q=dict(mode=a.mode,apiRoot=str(owner/'inputs/LL/src/API/API.LL'),fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),output=str(output),inputHashes=inputs,
        scoringRules=str(owner/'inputs'/scoring.relative_to(ROOT)))
    q.update({key:str(owner/'inputs/TestResults'/name) for key,(name,_) in ARCHIVES.items()})
    if a.mode=='prepare':seconds,native,fights,seeds,env,test=900,840,0,0,'LL_TOWER_FIFTH_RETURN','Frozen_fifth_return'
    else:
        reserved=[];cursor=0
        def fresh():
            nonlocal cursor
            while True:
                value=int.from_bytes(hashlib.sha256(f'{VERSION}|{cursor}'.encode()).digest()[:4],'little',signed=True);cursor+=1
                if value not in excluded:excluded.add(value);reserved.append(value);return value
        panels={r['key']:[fresh() for _ in range(4)] for r in io.read(qualification/'result.json')['prepared']}
        io.write(owner/'seed-ledger.json',dict(version=VERSION,state='ReservedIncludingUnconsumed',historical=earlier['historical'],dungeon=earlier['dungeon'],
            preceding=earlier['preceding']+[dict(archive=str(latest),sha256=LEDGER_PIN)],reserved=reserved,exclusionUnionCount=len(excluded)))
        inputs[str(owner/'seed-ledger.json')]=io.sha(owner/'seed-ledger.json')
        q.update(qualification=str(owner/'inputs'/qualification.relative_to(ROOT)),qualificationPin=a.qualification_pin,panels=panels)
        seconds,native,fights,seeds,env,test=900,840,150,60,'LL_TOWER_FIFTH_RETURN','Frozen_fifth_return'
    io.write(owner/'source-map.json',sources);io.write(owner/'source-compatibility.json',compatibility);io.write(owner/'request.json',q)
    io.write(owner/'declaration.json',dict(version=VERSION,mode=a.mode,requestPin=io.sha(owner/'request.json'),maximumSeconds=seconds,
        maximumNativeSeconds=native,maximumFights=fights,newSeeds=seeds,maximumBytes=256*1048576,retries=0,
        rule='Fifteen retained level-40 five-Essence floor-5 parties; seventeen held servers preserved. Restore prior failures and native rewards, distinct personal/population boundaries and historical seat order. Four fresh attempts or first actual victory, paired supplied Epic retaining stronger gear, two exact replays per server. No extra training or grants. Rating availability is a model assumption; no live account claim.'))
    process=io.module('expansion_process',ROOT/'build/bounded_windows_process.py');old=os.environ.get(env);os.environ[env]=str(owner/'request.json')
    try:
        command=[shutil.which('pwsh'),'-NoProfile','-File',str(ROOT/'build/run-tests.ps1'),'-NoBuild','-ArtifactsPath',str(artifacts),
            '-Filter','FullyQualifiedName~BalanceHarnessFifthReturnTests.'+test]
        receipt=process.run(command,ROOT,owner/'study.log',time.monotonic()+seconds,log_byte_limit=1048576);io.write(owner/'process.json',receipt)
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx',owner/'study-tests.trx')
        io.check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Incomplete study; preserve evidence/reservations, no retry')
        for path,pin in inputs.items():io.check(io.sha(Path(path))==pin,'Frozen input drift')
        io.check(sum(p.stat().st_size for p in output.iterdir())<=256*1048576,'Output cap')
        pin=io.sha(output/'files.json');io.manifest(output,pin)
        io.check(io.read(output/'result.json')['status']==('EarnedFifthReturnPrepared' if a.mode=='prepare' else 'EarnedFifthReturnCombatComplete'),'Incomplete result')
        done=dict(status='Complete',mode=a.mode,manifestPin=pin,resultPin=io.sha(output/'result.json'),newSeeds=seeds)
        io.write(owner/'completion.json',done);print(json.dumps(done,indent=2))
    finally:
        if old is None:os.environ.pop(env,None)
        else:os.environ[env]=old


if __name__=='__main__':main()
