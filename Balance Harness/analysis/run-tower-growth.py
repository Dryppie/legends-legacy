"""Freeze and run one bounded, seed-free growth dependency projection through the test wrapper."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
ARCHIVE_PIN = '820ecd26646eb4d1b00b59c14db489054dee86d629b2d83427dc47064027e3c5'
LEDGER_PIN = '021a8ab1b4527222e738d4e61994e63e7ca21d439e6423e79292e2c223102cc3'
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def check(value, message):
    if not value: raise AssertionError(message)
def write(path, value):
    with path.open('x', encoding='utf-8') as f: json.dump(value, f, indent=2); f.write('\n')

def main():
    p=argparse.ArgumentParser()
    for name in ['owner','output','artifacts']: p.add_argument('--'+name,type=Path,required=True)
    a=p.parse_args(); owner,output,artifacts=[x.absolute() for x in [a.owner,a.output,a.artifacts]]
    check(owner.parent==output.parent==artifacts.parent==ROOT/'TestResults','Direct TestResults children required')
    check(owner.name.startswith('tower-growth-owner') and not owner.exists() and not output.exists(),'Fresh owner/output required')
    archive=ROOT/'TestResults/tower-prophecy-combat-study-20260929'
    ledger=ROOT/'TestResults/tower-prophecy-combat-owner-20260929/seed-ledger.json'
    check(sha(archive/'files.json')==ARCHIVE_PIN and sha(ledger)==LEDGER_PIN,'Changed historical pin')
    compatibility=read(ROOT/'TestResults/tower-prophecy-combat-owner-20260929/source-compatibility.json')
    for name,pin in compatibility.items(): check(sha(ROOT/name)==pin,'Review changed production source: '+name)
    archive_files=read(archive/'files.json')
    histories=sorted(archive.glob('*--offered-kills-no-reroll--history.json'))
    run_files=sorted({step['file'] for path in histories for step in read(path)['steps']})
    for name in [p.name for p in histories]+run_files: check(sha(archive/name)==archive_files[name],'Changed native history')
    owner.mkdir(); inputs={}; sources={}
    def freeze(path):
        if str(path) in sources: return
        dest=owner/'inputs'/path.relative_to(ROOT); dest.parent.mkdir(parents=True,exist_ok=True)
        pin=sha(path)
        with path.open('rb') as src, dest.open('xb') as dst: shutil.copyfileobj(src,dst)
        check(sha(path)==sha(dest)==pin,'Source changed during capture')
        sources[str(path)]=dict(archive=str(dest),sha256=pin); inputs[str(dest)]=pin
    for folder in ['LL/src/API/API.LL/Data','LL/tools/BalanceHarness/Fixtures']:
        for path in sorted((ROOT/folder).rglob('*.json')): freeze(path)
    freeze(ROOT/'LL/src/API/API.LL/appsettings.json')
    for folder in ['LL/tools/BalanceHarness','LL/src/Core/Common','LL/src/Core/Domain','LL/src/Core/Application','LL/src/Infrastructure/Service/Services.LL']:
        for path in sorted((ROOT/folder).rglob('*.cs')):
            if not {'bin','obj'}.intersection(path.relative_to(ROOT).parts): freeze(path)
    for path in [archive/'files.json',ledger,*histories,*[archive/n for n in run_files],
                 Path(__file__),Path(__file__).with_name('verify-tower-growth.py'),Path(__file__).with_name('audit-prophecy-offers.py'),ROOT/'build/bounded_windows_process.py',
                 ROOT/'build/run-tests.ps1',ROOT/'LL/tests/EssenceSystem.Tests/BalanceHarnessGrowthTests.cs']:
        freeze(path)
    for name in ['BalanceHarness','Services.LL','Application','Domain','Common','EssenceSystem.Tests']:
        path=artifacts/'bin/EssenceSystem.Tests/release'/f'{name}.dll'; freeze(path); inputs[str(path)]=sha(path)
    write(owner/'source-map.json',sources)
    write(owner/'source-compatibility.json',compatibility)
    request=dict(apiRoot=str(owner/'inputs/LL/src/API/API.LL'),fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),
        archive=str(owner/'inputs/TestResults/tower-prophecy-combat-study-20260929'),output=str(output),inputHashes=inputs)
    write(owner/'request.json',request)
    write(owner/'declaration.json',dict(version='tower-growth-qualification-v1',requestPin=sha(owner/'request.json'),
        histories=32,maximumEncountersPerHistory=86400,maximumSeconds=300,maximumNativeSeconds=240,maximumBytes=32*1048576,
        newFights=0,newCombatSeeds=0,measuredPlayerSamples=0,archivePin=ARCHIVE_PIN,historicalLedgerPin=LEDGER_PIN,
        rule='Native XP and Essence training through first archived entry, native level-dependent offers and supported events. Separate mastery-only recorded prefix stops before first changed entry benefit. Zero new combat outcomes, clocks or player-time claims.'))
    spec=importlib.util.spec_from_file_location('entry_process',ROOT/'build/bounded_windows_process.py')
    process=importlib.util.module_from_spec(spec); spec.loader.exec_module(process)
    previous=os.environ.get('LL_TOWER_GROWTH_PROJECTION');os.environ['LL_TOWER_GROWTH_PROJECTION']=str(owner/'request.json')
    try:
        command=[shutil.which('pwsh'),'-NoProfile','-File',str(ROOT/'build/run-tests.ps1'),'-NoBuild','-ArtifactsPath',str(artifacts),
                 '-Filter','FullyQualifiedName~BalanceHarnessGrowthTests.Qualify_growth_without_transferring_changed_combat']
        receipt=process.run(command,ROOT,owner/'export.log',time.monotonic()+300,log_byte_limit=1048576)
        write(owner/'process.json',receipt)
        check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Incomplete source projection')
        check(sum(p.stat().st_size for p in output.iterdir())<=32*1048576,'Output cap exceeded')
        check(sha(archive/'files.json')==ARCHIVE_PIN and sha(ledger)==LEDGER_PIN,'Historical evidence changed')
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx',owner/'export-tests.trx')
        check(read(output/'result.json')['status']=='GrowthDependenciesQualifiedNotCombat','Incomplete growth qualification')
        done=dict(status='CompleteGrowthQualification',manifestPin=sha(output/'files.json'),resultPin=sha(output/'result.json'),newFights=0,newCombatSeeds=0)
        write(owner/'completion.json',done);print(json.dumps(done,indent=2))
    finally:
        if previous is None: os.environ.pop('LL_TOWER_GROWTH_PROJECTION',None)
        else: os.environ['LL_TOWER_GROWTH_PROJECTION']=previous

if __name__=='__main__': main()
