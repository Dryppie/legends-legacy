"""Freeze production finalization dependencies, qualify early parties, then admit one bounded unlock study."""
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
spec=importlib.util.spec_from_file_location('earned_io',Path(__file__).with_name('run-tower-earned-party.py'))
io=importlib.util.module_from_spec(spec);sys.modules[spec.name]=io;spec.loader.exec_module(io)
VERSION='tower-early-unlock-v1'
PARTY_PIN='d96b80e062b07a14712ace1686857b8c6b377845af683dcf0b69146dd3d731f2'
COMBAT_PIN='83ec1b78d6b53ffde69f7cfe839644e6ce56d39c5cbfd50db4e7d2d4888b783a'
EARNED_LEDGER='a07cc558bc333dfe291de556e0679a7a3c060fc027498f111b08990b416ed723'


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--mode',choices=['prepare','combat'],required=True)
    for name in ['owner','output','artifacts']:p.add_argument('--'+name,type=Path,required=True)
    p.add_argument('--qualification-owner',type=Path);p.add_argument('--qualification-pin')
    a=p.parse_args();owner,output,artifacts=[v.absolute() for v in [a.owner,a.output,a.artifacts]]
    io.check(owner.parent==output.parent==artifacts.parent==ROOT/'TestResults','Direct TestResults children required')
    io.check(owner.name.startswith('tower-unlock-') and not owner.exists() and not output.exists(),'Fresh named owner/output; no retry')
    if a.mode=='combat':io.check(not list((ROOT/'TestResults').glob('tower-unlock-*/seed-ledger.json')),'Earlier unlock reservations require a new declared predecessor union')
    party=ROOT/'TestResults/tower-earned-party-study-20260929';growth=ROOT/'TestResults/tower-growing-activity-study-20260929'
    prior=ROOT/'TestResults/tower-earned-combat-owner-20260929'
    io.manifest(party,PARTY_PIN);io.manifest(growth,io.ARCHIVE_PIN)
    audit=io.read(prior/'independent-audit.json')
    io.check(audit['status']=='VerifiedEarnedFloorOneDiagnostic' and audit['manifestPin']==COMBAT_PIN,'Audited earned-party predecessor required')
    compatibility=io.read(prior/'source-compatibility.json')
    for name,pin in compatibility.items():io.check(io.sha(ROOT/name)==pin,'Review changed production input: '+name)
    qualification=None
    if a.mode=='combat':
        io.check(a.qualification_owner and a.qualification_pin,'Explicit qualification required')
        q_owner=a.qualification_owner.absolute();qualification=Path(io.read(q_owner/'request.json')['output'])
        io.manifest(qualification,a.qualification_pin);qa=io.read(q_owner/'independent-audit.json')
        io.check(qa['status']=='VerifiedEarlyUnlockPreparation' and qa['manifestPin']==a.qualification_pin and qa['parties']==24,'Independent preparation audit required')
    owner.mkdir();inputs={};sources={}
    def freeze(path):
        if str(path) in sources:return Path(sources[str(path)]['archive'])
        dest=owner/'inputs'/path.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);pin=io.sha(path)
        with path.open('rb') as src,dest.open('xb') as dst:shutil.copyfileobj(src,dst)
        io.check(io.sha(path)==io.sha(dest)==pin,'Input drift while capturing')
        sources[str(path)]=dict(archive=str(dest),sha256=pin);inputs[str(dest)]=pin;return dest
    for folder in ['LL/src/API/API.LL/Data','LL/tools/BalanceHarness/Fixtures']:
        for path in sorted((ROOT/folder).rglob('*.json')):freeze(path)
    freeze(ROOT/'LL/src/API/API.LL/appsettings.json')
    for folder in ['LL/tools/BalanceHarness','LL/src/Core/Common','LL/src/Core/Domain','LL/src/Core/Application',
                   'LL/src/Infrastructure/Service/Services.LL','LL/src/Infrastructure/Persistence/Persistence.LL']:
        for path in sorted((ROOT/folder).rglob('*.cs')):
            if not {'bin','obj'}.intersection(path.relative_to(ROOT).parts):freeze(path)
    for path in [*sorted(party.iterdir()),growth/'files.json',*sorted(growth.glob('*--earned-progression--history.json')),
                 prior/'independent-audit.json',prior/'seed-ledger.json',
                 Path(__file__),Path(__file__).with_name('run-tower-earned-party.py'),Path(__file__).with_name('verify-tower-unlock.py'),
                 ROOT/'build/bounded_windows_process.py',ROOT/'build/run-tests.ps1',
                 *[ROOT/'LL/tests/EssenceSystem.Tests'/name for name in ['WorldTowerServiceTests.cs','WorldTowerTitleTests.cs',
                    'BalanceHarnessTowerUnlockTests.cs','TowerUnlockServer.cs']]]:freeze(path)
    if qualification:
        for path in [*sorted(qualification.iterdir()),q_owner/'independent-audit.json',q_owner/'request.json']:freeze(path)
    runtime=artifacts/'bin/EssenceSystem.Tests/release'
    for path in sorted(runtime.iterdir()):
        if path.is_file() and (path.suffix=='.dll' or path.name.endswith(('.deps.json','.runtimeconfig.json'))):
            freeze(path);inputs[str(path)]=io.sha(path)
    request=dict(apiRoot=str(owner/'inputs/LL/src/API/API.LL'),fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),
        archive=str(owner/'inputs'/party.relative_to(ROOT)),growthArchive=str(owner/'inputs'/growth.relative_to(ROOT)),output=str(output),inputHashes=inputs)
    if a.mode=='prepare':
        seconds,native,fights,seeds=300,240,0,0;env='LL_TOWER_UNLOCK_PREPARATION';test='Frozen_unlock_preparation'
    else:
        excluded,_,_=io.exclusions();latest=prior/'seed-ledger.json';io.check(io.sha(latest)==EARNED_LEDGER,'Wrong earned combat reservations')
        earlier=io.read(latest);excluded.update(earlier['reserved']);io.check(len(excluded)==877447,'Incomplete predecessor union')
        reserved=[];cursor=0
        def fresh():
            nonlocal cursor
            while True:
                value=int.from_bytes(hashlib.sha256(f'{VERSION}|{cursor}'.encode()).digest()[:4],'little',signed=True);cursor+=1
                if value not in excluded:excluded.add(value);reserved.append(value);return value
        panels={f'{outcome}--{rotation}--{path}':[fresh() for _ in range(12)] for outcome in ['perfect','four-of-five'] for rotation in range(4) for path in range(4)}
        io.write(owner/'seed-ledger.json',dict(version=VERSION,state='ReservedIncludingUnconsumed',historical=earlier['historical'],dungeon=earlier['dungeon'],
            preceding=earlier['preceding']+[dict(archive=str(latest),sha256=EARNED_LEDGER)],reserved=reserved,exclusionUnionCount=len(excluded)))
        inputs[str(owner/'seed-ledger.json')]=io.sha(owner/'seed-ledger.json')
        request.update(qualification=str(owner/'inputs'/qualification.relative_to(ROOT)),qualificationPin=a.qualification_pin,panels=panels)
        seconds,native,fights,seeds=900,840,480,384;env='LL_TOWER_UNLOCK_STUDY';test='Frozen_native_unlock_journeys'
    io.write(owner/'source-map.json',sources);io.write(owner/'source-compatibility.json',compatibility);io.write(owner/'request.json',request)
    io.write(owner/'declaration.json',dict(version=VERSION,mode=a.mode,requestPin=io.sha(owner/'request.json'),maximumSeconds=seconds,
        maximumNativeSeconds=native,maximumFights=fights,newSeeds=seeds,maximumBytes=256*1048576,retries=0,
        rule='24 preparations before 32 chronological paths. Native finalization on isolated in-memory servers; only actual victories unlock next floors. Four attempts per floor through first Epic supply source, no gear grant or player-time forecast.'))
    process=io.module('unlock_process',ROOT/'build/bounded_windows_process.py');old=os.environ.get(env);os.environ[env]=str(owner/'request.json')
    try:
        command=[shutil.which('pwsh'),'-NoProfile','-File',str(ROOT/'build/run-tests.ps1'),'-NoBuild','-ArtifactsPath',str(artifacts),
                 '-Filter','FullyQualifiedName~BalanceHarnessTowerUnlockTests.'+test]
        receipt=process.run(command,ROOT,owner/'study.log',time.monotonic()+seconds,log_byte_limit=1048576);io.write(owner/'process.json',receipt)
        io.check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Incomplete study; retain all evidence/reservations, no retry')
        for name,pin in inputs.items():io.check(io.sha(Path(name))==pin,'Frozen input changed')
        io.check(sum(p.stat().st_size for p in output.iterdir())<=256*1048576,'Output bound exceeded')
        pin=io.sha(output/'files.json');io.manifest(output,pin)
        io.check(io.read(output/'result.json')['status']==('EarlyUnlockPreparationComplete' if a.mode=='prepare' else 'EarlyTowerUnlockDiagnosticComplete'),'Incomplete study status')
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx',owner/'study-tests.trx')
        done=dict(status='Complete',mode=a.mode,manifestPin=pin,resultPin=io.sha(output/'result.json'),newSeeds=seeds)
        io.write(owner/'completion.json',done);print(json.dumps(done,indent=2))
    finally:
        if old is None:os.environ.pop(env,None)
        else:os.environ[env]=old


if __name__=='__main__':main()
