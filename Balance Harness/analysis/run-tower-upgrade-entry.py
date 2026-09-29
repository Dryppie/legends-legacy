"""Freeze and own seed-free post-unlock personal funding/preparation qualification."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time

sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('entry_io',Path(__file__).with_name('run-tower-earned-party.py'))
io=importlib.util.module_from_spec(spec);sys.modules[spec.name]=io;spec.loader.exec_module(io)
VERSION='tower-upgrade-entry-v1'
PARTY_PIN='d96b80e062b07a14712ace1686857b8c6b377845af683dcf0b69146dd3d731f2'
GROWTH_PIN='f8f05eb4cf6d1e9c28a87b72bfeb0f1a9ff9f1229c5ad687c0413b042d7fbdc5'
UNLOCK_PIN='fce82fe32b7bfb224f9b6b4d6675a0fca72bc202c12aed54685d3735d698fc52'
LEDGER_PIN='4094923d5ca85548df97c30d00046eb26935279bed7c3dd03ecc060be9862dff'


def main():
    p=argparse.ArgumentParser(description=__doc__)
    for key in ['owner','output','artifacts']:p.add_argument('--'+key,type=Path,required=True)
    p.add_argument('--unlock-pin',required=True)
    a=p.parse_args();owner,output,artifacts=[x.absolute() for x in [a.owner,a.output,a.artifacts]]
    io.check(owner.parent==output.parent==artifacts.parent==ROOT/'TestResults','Direct TestResults children required')
    io.check(owner.name.startswith('tower-upgrade-entry-') and not owner.exists() and not output.exists(),'Fresh owner/output required')
    io.check(a.unlock_pin==UNLOCK_PIN,'Wrong earned-unlock pin')
    growth=ROOT/'TestResults/tower-growing-activity-study-20260929';party=ROOT/'TestResults/tower-earned-party-study-20260929'
    unlock=ROOT/'TestResults/tower-unlock-study-20260929';prior=ROOT/'TestResults/tower-unlock-owner-20260929'
    for folder,pin in [(growth,GROWTH_PIN),(party,PARTY_PIN),(unlock,UNLOCK_PIN)]:io.manifest(folder,pin)
    audit=io.read(prior/'independent-audit.json')
    io.check(audit['status']=='VerifiedEarlyTowerUnlockDiagnostic' and audit['manifestPin']==UNLOCK_PIN,'Audited unlocks required')
    io.check(io.sha(prior/'seed-ledger.json')==LEDGER_PIN,'Changed predecessor reservations')
    compatibility=io.read(prior/'source-compatibility.json')
    for name,pin in compatibility.items():io.check(io.sha(ROOT/name)==pin,'Review changed production input: '+name)
    owner.mkdir();inputs={};sources={}
    def freeze(path):
        if str(path) in sources:return
        target=owner/'inputs'/path.relative_to(ROOT);target.parent.mkdir(parents=True,exist_ok=True);pin=io.sha(path)
        with path.open('rb') as src,target.open('xb') as dst:shutil.copyfileobj(src,dst)
        io.check(io.sha(path)==io.sha(target)==pin,'Capture drift')
        inputs[str(target)]=pin;sources[str(path)]=dict(archive=str(target),sha256=pin)
    for folder in ['LL/src/API/API.LL/Data','LL/tools/BalanceHarness/Fixtures']:
        for path in sorted((ROOT/folder).rglob('*.json')):freeze(path)
    freeze(ROOT/'LL/src/API/API.LL/appsettings.json')
    for folder in ['LL/tools/BalanceHarness','LL/src/Core/Common','LL/src/Core/Domain','LL/src/Core/Application','LL/src/Infrastructure/Service/Services.LL']:
        for path in sorted((ROOT/folder).rglob('*.cs')):
            if not {'bin','obj'}.intersection(path.relative_to(ROOT).parts):freeze(path)
    for path in [party/'files.json',party/'points.json',growth/'files.json',unlock/'files.json',unlock/'result.json',
        prior/'independent-audit.json',prior/'seed-ledger.json',*sorted(unlock.glob('*--journey.json')),
        Path(__file__),Path(__file__).with_name('run-tower-earned-party.py'),Path(__file__).with_name('verify-tower-upgrade-entry.py'),
        ROOT/'build/run-tests.ps1',ROOT/'build/bounded_windows_process.py',ROOT/'LL/tests/EssenceSystem.Tests/BalanceHarnessUpgradeEntryTests.cs']:freeze(path)
    for point in io.read(party/'points.json'):
        if point['policy']!='earned-progression' or point['horizon']!=25920:continue
        path=growth/(point['history']+'--history.json');freeze(path)
        for step in io.read(path)['steps']:
            if step['encounter']<=point['encounter']:freeze(growth/step['file'])
    for path in sorted((artifacts/'bin/EssenceSystem.Tests/release').iterdir()):
        if path.is_file() and (path.suffix=='.dll' or path.name.endswith(('.deps.json','.runtimeconfig.json'))):
            freeze(path);inputs[str(path)]=io.sha(path)
    request=dict(apiRoot=str(owner/'inputs/LL/src/API/API.LL'),fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),
        growthArchive=str(owner/'inputs'/growth.relative_to(ROOT)),partyArchive=str(owner/'inputs'/party.relative_to(ROOT)),
        unlockArchive=str(owner/'inputs'/unlock.relative_to(ROOT)),output=str(output),inputHashes=inputs)
    io.write(owner/'source-map.json',sources);io.write(owner/'source-compatibility.json',compatibility);io.write(owner/'request.json',request)
    io.write(owner/'declaration.json',dict(version=VERSION,requestPin=io.sha(owner/'request.json'),maximumSeconds=300,maximumNativeSeconds=240,
        maximumBytes=256*1048576,newSeeds=0,maximumFights=0,retries=0,personalStates=32,serverStates=32,preparations=512))
    process=io.module('upgrade_entry_process',ROOT/'build/bounded_windows_process.py');env='LL_TOWER_UPGRADE_ENTRY';old=os.environ.get(env);os.environ[env]=str(owner/'request.json')
    try:
        command=[shutil.which('pwsh'),'-NoProfile','-File',str(ROOT/'build/run-tests.ps1'),'-NoBuild','-ArtifactsPath',str(artifacts),
            '-Filter','FullyQualifiedName~BalanceHarnessUpgradeEntryTests.Frozen_upgrade_entry_qualification']
        receipt=process.run(command,ROOT,owner/'study.log',time.monotonic()+300,log_byte_limit=1048576);io.write(owner/'process.json',receipt)
        io.check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Incomplete qualification; retain evidence, no overwrite')
        for path,pin in inputs.items():io.check(io.sha(Path(path))==pin,'Input drift')
        io.check(sum(p.stat().st_size for p in output.iterdir())<=256*1048576,'Output cap')
        pin=io.sha(output/'files.json');io.manifest(output,pin)
        io.check(io.read(output/'result.json')['status']=='FundedUpgradeEntriesQualifiedNotExecuted','Incomplete qualification')
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx',owner/'study-tests.trx')
        done=dict(status='Complete',manifestPin=pin,resultPin=io.sha(output/'result.json'),newSeeds=0)
        io.write(owner/'completion.json',done);print(json.dumps(done,indent=2))
    finally:
        if old is None:os.environ.pop(env,None)
        else:os.environ[env]=old


if __name__=='__main__':main()
