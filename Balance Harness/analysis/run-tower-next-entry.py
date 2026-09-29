"""Own one bounded funded next-entry wave after audited native runtime restoration."""
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
spec=importlib.util.spec_from_file_location('next_entry_io',Path(__file__).with_name('run-tower-earned-party.py'))
io=importlib.util.module_from_spec(spec);sys.modules[spec.name]=io;spec.loader.exec_module(io)
VERSION='tower-funded-next-entry-v1'
RUNTIME_PIN='981c2adf6ad621f0060d03df409969fd69e91cabc6d196c8b94fa60b6892a76b'
ENTRY_PIN='4b0cbf561aa44dca4d9eaccc50dfac846399cf1c60f67560e9cf0bd30a37c315'
LEDGER_PIN='4094923d5ca85548df97c30d00046eb26935279bed7c3dd03ecc060be9862dff'

def exclusions(latest):
    io.check(io.sha(latest)==LEDGER_PIN,'Changed predecessor reservations');ledger=io.read(latest)
    def pinned(entry):
        io.check(io.sha(Path(entry['archive']))==entry['sha256'],'Changed reservation ancestor')
        return io.read(Path(entry['archive']))
    h,d=pinned(ledger['historical']),pinned(ledger['dungeon'])
    values=set(h['historical']+h['first']+h['second']+d['reserved'])
    for entry in ledger['preceding']:values.update(pinned(entry)['reserved'])
    values.update(ledger['reserved']);io.check(len(values)==877831,'Incomplete exclusion union')
    return values,ledger

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['owner','output','artifacts','qualification-owner']:p.add_argument('--'+name,type=Path,required=True)
    p.add_argument('--qualification-pin',required=True)
    a=p.parse_args();owner,output,artifacts=[v.absolute() for v in [a.owner,a.output,a.artifacts]]
    io.check(owner.parent==output.parent==artifacts.parent==ROOT/'TestResults','Direct TestResults children required')
    io.check(owner.name.startswith('tower-next-entry-') and not owner.exists() and not output.exists(),'Fresh owner/output required')
    io.check(not list((ROOT/'TestResults').glob('tower-next-entry-*/seed-ledger.json')),'Existing next-entry reservations require a new declared predecessor; no renamed rerun')
    qo=a.qualification_owner.absolute();qualification=Path(io.read(qo/'request.json')['output'])
    io.check(a.qualification_pin==RUNTIME_PIN,'Wrong runtime admission');io.manifest(qualification,RUNTIME_PIN)
    audit=io.read(qo/'independent-audit.json')
    io.check(audit['status']=='VerifiedContinuationRuntime' and audit['manifestPin']==RUNTIME_PIN and audit['branches']==512,'Audited native runtime required')
    amendment=io.read(qo/'audit-amendment.json')
    for name,pin in amendment['files'].items():io.check(io.sha(qo/'audit-amendment'/name)==pin,'Auditor amendment drift')
    entry=ROOT/'TestResults/tower-upgrade-entry-study-20260929';io.manifest(entry,ENTRY_PIN)
    prior=ROOT/'TestResults/tower-unlock-owner-20260929';latest=prior/'seed-ledger.json';excluded,earlier=exclusions(latest)
    compatibility=io.read(prior/'source-compatibility.json')
    for name,pin in compatibility.items():io.check(io.sha(ROOT/name)==pin,'Changed production input: '+name)
    owner.mkdir();inputs={};sources={}
    def freeze(path):
        if str(path) in sources:return
        dest=owner/'inputs'/path.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);pin=io.sha(path)
        with path.open('rb') as src,dest.open('xb') as dst:shutil.copyfileobj(src,dst)
        io.check(io.sha(path)==io.sha(dest)==pin,'Capture drift');inputs[str(dest)]=pin;sources[str(path)]=dict(archive=str(dest),sha256=pin)
    for folder in ['LL/src/API/API.LL/Data','LL/tools/BalanceHarness/Fixtures']:
        for path in sorted((ROOT/folder).rglob('*.json')):freeze(path)
    freeze(ROOT/'LL/src/API/API.LL/appsettings.json')
    for folder in ['LL/tools/BalanceHarness','LL/src/Core/Common','LL/src/Core/Domain','LL/src/Core/Application','LL/src/Infrastructure/Service/Services.LL']:
        for path in sorted((ROOT/folder).rglob('*.cs')):
            if not {'bin','obj'}.intersection(path.relative_to(ROOT).parts):freeze(path)
    for path in sorted(Path(__file__).parent.glob('*.py')):freeze(path)
    scoring=ROOT/'TestResults/tower-growing-activity-study-20260929/rules.json'
    scoring_manifest=scoring.parent/'files.json'
    io.check(io.sha(scoring_manifest)==io.ARCHIVE_PIN and io.sha(scoring)==io.read(scoring_manifest)[scoring.name],'Scoring witness drift')
    freeze(scoring);freeze(scoring_manifest)
    for path in [*sorted(qualification.iterdir()),*sorted(entry.iterdir()),qo/'independent-audit.json',qo/'audit-amendment.json',
        *sorted((qo/'audit-amendment').iterdir()),latest,Path(__file__),Path(__file__).with_name('verify-tower-next-entry.py'),
        Path(__file__).with_name('run-tower-earned-party.py'),ROOT/'build/run-tests.ps1',ROOT/'build/bounded_windows_process.py',
        ROOT/'LL/tests/EssenceSystem.Tests/BalanceHarnessNextEntryTests.cs']:freeze(path)
    for path in sorted((artifacts/'bin/EssenceSystem.Tests/release').iterdir()):
        if path.is_file() and (path.suffix=='.dll' or path.name.endswith(('.deps.json','.runtimeconfig.json'))):
            freeze(path);inputs[str(path)]=io.sha(path)
    ready={}
    for b in io.read(qualification/'branches.json'):
        if b['native']['reason']=='Enter':
            family=b['native']['selectedDungeon'];io.check(b['history'] not in ready or ready[b['history']]==family,'Inconsistent paired family');ready[b['history']]=family
    io.check(len(ready)==12,'Changed ready personal population');reserved=[];cursor=0
    def fresh():
        nonlocal cursor
        while True:
            value=int.from_bytes(hashlib.sha256(f'{VERSION}|{cursor}'.encode()).digest()[:4],'little',signed=True);cursor+=1
            if value not in excluded:excluded.add(value);reserved.append(value);return value
    panels={name:dict(dungeon=family,layoutSeed=fresh(),roomSeeds=[fresh() for _ in range(64)]) for name,family in sorted(ready.items())}
    io.write(owner/'seed-ledger.json',dict(version=VERSION,state='ReservedIncludingUnconsumed',historical=earlier['historical'],dungeon=earlier['dungeon'],
        preceding=earlier['preceding']+[dict(archive=str(latest),sha256=LEDGER_PIN)],reserved=reserved,exclusionUnionCount=len(excluded)))
    inputs[str(owner/'seed-ledger.json')]=io.sha(owner/'seed-ledger.json')
    q=dict(apiRoot=str(owner/'inputs/LL/src/API/API.LL'),fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),
        qualification=str(owner/'inputs'/qualification.relative_to(ROOT)),qualificationPin=RUNTIME_PIN,entryArchive=str(owner/'inputs'/entry.relative_to(ROOT)),
        output=str(output),inputHashes=inputs,panels=panels,scoringRules=str(owner/'inputs'/scoring.relative_to(ROOT)))
    io.write(owner/'source-map.json',sources);io.write(owner/'source-compatibility.json',compatibility);io.write(owner/'request.json',q)
    io.write(owner/'declaration.json',dict(version=VERSION,requestPin=io.sha(owner/'request.json'),maximumSeconds=900,maximumNativeSeconds=840,
        maximumFights=13824,maximumBytes=256*1048576,newSeeds=780,retries=0,combinations=512,attempts=192,controls=12,replays=12,personalPanels=12))
    process=io.module('next_entry_process',ROOT/'build/bounded_windows_process.py');env='LL_TOWER_NEXT_ENTRY';old=os.environ.get(env);os.environ[env]=str(owner/'request.json')
    try:
        command=[shutil.which('pwsh'),'-NoProfile','-File',str(ROOT/'build/run-tests.ps1'),'-NoBuild','-ArtifactsPath',str(artifacts),
            '-Filter','FullyQualifiedName~BalanceHarnessNextEntryTests.Frozen_funded_next_entries']
        receipt=process.run(command,ROOT,owner/'study.log',time.monotonic()+900,log_byte_limit=1048576);io.write(owner/'process.json',receipt)
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx',owner/'study-tests.trx')
        io.check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Incomplete study; preserve all results/reservations, no retry')
        for path,pin in inputs.items():io.check(io.sha(Path(path))==pin,'Input drift')
        io.check(sum(p.stat().st_size for p in output.iterdir())<=256*1048576,'Output cap')
        pin=io.sha(output/'files.json');io.manifest(output,pin);io.check(io.read(output/'result.json')['status']=='FundedNextEntriesComplete','Incomplete result')
        done=dict(status='Complete',manifestPin=pin,resultPin=io.sha(output/'result.json'),newSeeds=780)
        io.write(owner/'completion.json',done);print(json.dumps(done,indent=2))
    finally:
        if old is None:os.environ.pop(env,None)
        else:os.environ[env]=old

if __name__=='__main__':main()
