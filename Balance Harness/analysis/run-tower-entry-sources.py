"""Freeze and run one bounded, seed-free entry affordability projection through the test wrapper."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
ARCHIVE_PIN = '3b4f4870dfc8e10f9b0056788eb006b394ad7618dca963cba1d48d204a453fe7'
LEDGER_PIN = 'c48c5fafa3b176febbf862841425f6fcbffdfd016032ad1ad02f0d459d52f1c9'
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
    check(owner.name.startswith('tower-entry-sources-owner') and not owner.exists() and not output.exists(),'Fresh owner/output required')
    archive=ROOT/'TestResults/tower-dungeon-loot-study-20260929'
    ledger=ROOT/'TestResults/tower-dungeon-loot-owner-20260929/seed-ledger.json'
    check(sha(archive/'files.json')==ARCHIVE_PIN and sha(ledger)==LEDGER_PIN,'Changed historical pin')
    compatibility=read(ROOT/'TestResults/tower-dungeon-loot-owner-20260929/source-compatibility.json')
    for name,pin in compatibility.items(): check(sha(ROOT/name)==pin,'Review changed production source: '+name)
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
    for path in [archive/'files.json',ledger,*sorted(archive.glob('*--include-dungeon-loot--history.json')),
                 Path(__file__),Path(__file__).with_name('verify-tower-entry-sources.py'),ROOT/'build/bounded_windows_process.py',
                 ROOT/'build/run-tests.ps1',ROOT/'LL/tests/EssenceSystem.Tests/BalanceHarnessEntrySourceTests.cs']:
        freeze(path)
    for name in ['BalanceHarness','Services.LL','Application','Domain','Common','EssenceSystem.Tests']:
        path=artifacts/'bin/EssenceSystem.Tests/release'/f'{name}.dll'; freeze(path); inputs[str(path)]=sha(path)
    write(owner/'source-map.json',sources)
    request=dict(apiRoot=str(owner/'inputs/LL/src/API/API.LL'),fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),
        archive=str(owner/'inputs/TestResults/tower-dungeon-loot-study-20260929'),output=str(output),inputHashes=inputs)
    write(owner/'request.json',request)
    write(owner/'declaration.json',dict(version='tower-entry-sources-v1',requestPin=sha(owner/'request.json'),
        histories=64,maximumEncountersPerHistory=86400,maximumSeconds=180,maximumNativeSeconds=120,maximumBytes=32*1048576,
        newFights=0,newCombatSeeds=0,measuredPlayerSamples=0,archivePin=ARCHIVE_PIN,historicalLedgerPin=LEDGER_PIN,
        rule='Conditional offered kill prophecies share archived idle activity; native claims and whole-fragment assembly; stop outcome transfer at first changed entry decision. No source credit from caches, purchases or duplicate quests.'))
    spec=importlib.util.spec_from_file_location('entry_process',ROOT/'build/bounded_windows_process.py')
    process=importlib.util.module_from_spec(spec); spec.loader.exec_module(process)
    previous=os.environ.get('LL_TOWER_ENTRY_SOURCES');os.environ['LL_TOWER_ENTRY_SOURCES']=str(owner/'request.json')
    try:
        command=[shutil.which('pwsh'),'-NoProfile','-File',str(ROOT/'build/run-tests.ps1'),'-NoBuild','-ArtifactsPath',str(artifacts),
                 '-Filter','FullyQualifiedName~BalanceHarnessEntrySourceTests.Export_costed_sources_without_transferring_changed_combat_outcomes']
        receipt=process.run(command,ROOT,owner/'export.log',time.monotonic()+180,log_byte_limit=1048576)
        write(owner/'process.json',receipt)
        check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Incomplete source projection')
        check(sum(p.stat().st_size for p in output.iterdir())<=32*1048576,'Output cap exceeded')
        check(sha(archive/'files.json')==ARCHIVE_PIN and sha(ledger)==LEDGER_PIN,'Historical evidence changed')
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx',owner/'export-tests.trx')
        done=dict(status='CompleteSourceProjection',manifestPin=sha(output/'files.json'),resultPin=sha(output/'result.json'),newFights=0,newCombatSeeds=0)
        write(owner/'completion.json',done);print(json.dumps(done,indent=2))
    finally:
        if previous is None: os.environ.pop('LL_TOWER_ENTRY_SOURCES',None)
        else: os.environ['LL_TOWER_ENTRY_SOURCES']=previous

if __name__=='__main__': main()
