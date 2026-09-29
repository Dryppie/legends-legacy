"""Own one seed-free Old Forest growth/source qualification, preserving the floor-5 failure archives."""
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
spec=importlib.util.spec_from_file_location('level40_io',Path(__file__).with_name('run-tower-earned-party.py'))
io=importlib.util.module_from_spec(spec);sys.modules[spec.name]=io;spec.loader.exec_module(io)
VERSION='tower-old-forest-growth-v2'
ARCHIVES={
    'expansionArchive':('tower-expansion-combat-study-20260929','81306b47b1aa870356dcdb43cc0fb7266449a031e2397a1debf781f19467a722'),
    'preparationArchive':('tower-expansion-preparation-study-20260929','b756d8027ae3b6e12258c6773c6eba0ca68b957f9bb976680f0e3e3352771e91'),
    'returnArchive':('tower-return-combat-study-20260929','9b9cff072ac42afe75c17485015bc792f34bae5580f5cf789e40bf3fafc2e6be')}
LEDGER_PIN='7473b0172d868e81d872d8caffb0bdf33fab41986c4ae4cc79720e4ec27a8fa2'

def exclusions(latest):
    io.check(io.sha(latest)==LEDGER_PIN,'Changed latest seed ledger');ledger=io.read(latest)
    def pinned(e):
        io.check(io.sha(Path(e['archive']))==e['sha256'],'Changed seed ancestor');return io.read(Path(e['archive']))
    h,d=pinned(ledger['historical']),pinned(ledger['dungeon'])
    values=set(h['historical']+h['first']+h['second']+d['reserved'])
    for e in ledger['preceding']:values.update(pinned(e)['reserved'])
    values.update(ledger['reserved']);io.check(len(values)==878799,'Lost reservations');return values

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['owner','output','artifacts']:p.add_argument('--'+name,type=Path,required=True)
    a=p.parse_args();owner,output,artifacts=[v.absolute() for v in [a.owner,a.output,a.artifacts]]
    io.check(owner.parent==output.parent==artifacts.parent==ROOT/'TestResults','Direct TestResults children required')
    io.check(owner.name.startswith('tower-level40-') and not owner.exists() and not output.exists(),'Fresh owner/output required')
    io.check(not list((ROOT/'TestResults').glob('tower-level40-*/completion.json')),'Do not extend a completed qualification')
    failed=ROOT/'TestResults/tower-level40-owner-20260929'
    failed_pin='207bda673a6178f7c6d203c52b8b8631f780b91acf705020ca5661e2796446a4'
    io.check(io.sha(failed/'request.json')==failed_pin and io.read(failed/'process.json')['exitCode']!=0,'Preserve the failed v1 qualification')
    for file,pin in io.read(failed/'request.json')['inputHashes'].items():io.check(io.sha(Path(file))==pin,'Changed failed qualification input')
    prior=ROOT/'TestResults/tower-expansion-combat-owner-20260929';latest=prior/'seed-ledger.json';exclusions(latest)
    compatibility=io.read(prior/'source-compatibility.json')
    for name,pin in compatibility.items():io.check(io.sha(ROOT/name)==pin,'Production drift: '+name)
    io.check(io.read(prior/'independent-audit.json')['status']=='VerifiedEarnedExpansionCombat','Unqualified predecessor')
    for name,pin in ARCHIVES.values():io.manifest(ROOT/'TestResults'/name,pin)
    owner.mkdir();inputs={};sources={}
    def freeze(path):
        if str(path) in sources:return
        dest=owner/'inputs'/path.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);pin=io.sha(path)
        with path.open('rb') as src,dest.open('xb') as dst:shutil.copyfileobj(src,dst)
        io.check(io.sha(path)==io.sha(dest)==pin,'Capture drift');inputs[str(dest)]=pin;sources[str(path)]=dict(archive=str(dest),sha256=pin)
    for folder in ['LL/src/API/API.LL/Data','LL/tools/BalanceHarness/Fixtures']:
        for path in sorted((ROOT/folder).rglob('*.json')):freeze(path)
    freeze(ROOT/'LL/src/API/API.LL/appsettings.json')
    for folder in ['LL/tools/BalanceHarness','LL/src/Core/Common','LL/src/Core/Domain','LL/src/Core/Application','LL/src/Infrastructure/Service/Services.LL','LL/src/Infrastructure/Persistence/Persistence.LL']:
        for path in sorted((ROOT/folder).rglob('*.cs')):
            if not {'bin','obj'}.intersection(path.relative_to(ROOT).parts):freeze(path)
    for path in sorted(Path(__file__).parent.glob('*.py')):freeze(path)
    for name,_ in ARCHIVES.values():
        for path in sorted((ROOT/'TestResults'/name).iterdir()):freeze(path)
    for pattern in ['BalanceHarness*Tests.cs','QuestSystemTests.cs','TowerQuestSourceProbe.cs']:
        for path in sorted((ROOT/'LL/tests/EssenceSystem.Tests').glob(pattern)):freeze(path)
    scoring=ROOT/'TestResults/tower-growing-activity-study-20260929/rules.json'
    io.check(io.sha(scoring.parent/'files.json')==io.ARCHIVE_PIN and io.sha(scoring)==io.read(scoring.parent/'files.json')[scoring.name],'Scoring witness drift')
    for path in [scoring,latest,prior/'independent-audit.json',ROOT/'build/run-tests.ps1',ROOT/'build/bounded_windows_process.py']:freeze(path)
    for name in ['request.json','process.json','study.log','study-tests.trx']:freeze(failed/name)
    for path in sorted((artifacts/'bin/EssenceSystem.Tests/release').iterdir()):
        if path.is_file() and (path.suffix=='.dll' or path.name.endswith(('.deps.json','.runtimeconfig.json'))):freeze(path);inputs[str(path)]=io.sha(path)
    q=dict(apiRoot=str(owner/'inputs/LL/src/API/API.LL'),fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'),output=str(output),inputHashes=inputs,
        scoringRules=str(owner/'inputs'/scoring.relative_to(ROOT)))
    q.update({key:str(owner/'inputs/TestResults'/name) for key,(name,_) in ARCHIVES.items()})
    io.write(owner/'source-map.json',sources);io.write(owner/'source-compatibility.json',compatibility);io.write(owner/'request.json',q)
    io.write(owner/'correction.json',dict(failedRequest=str(failed/'request.json'),failedRequestPin=failed_pin,
        reason='v1 changed each point Horizon to its new encounter count; native Tower party validation rejected mixed cohort horizons. v2 retains the original cohort Horizon and advances Encounter/AvailableAt only.',
        retainedFailedInputs=True,failedStudyOutputFiles=0,newCombatTransferred=0))
    io.write(owner/'declaration.json',dict(version=VERSION,requestPin=io.sha(owner/'request.json'),maximumSeconds=900,maximumNativeSeconds=840,
        maximumFights=0,newSeeds=0,maximumBytes=256*1048576,maximumLogBytes=1048576,retries=0,maximumOwners=240,
        latestSeedLedger=str(latest),latestSeedLedgerPin=LEDGER_PIN,exclusionUnionCount=878799,
        rule='One prospective Old Forest continuation to level 40 or the absolute 86400 encounter cap; no combat admission, fifth grant or historical quest/drop credit.'))
    process=io.module('level40_process',ROOT/'build/bounded_windows_process.py');env='LL_TOWER_LEVEL40';old=os.environ.get(env);os.environ[env]=str(owner/'request.json')
    try:
        command=[shutil.which('pwsh'),'-NoProfile','-File',str(ROOT/'build/run-tests.ps1'),'-NoBuild','-ArtifactsPath',str(artifacts),'-Filter','FullyQualifiedName~BalanceHarnessLevel40Tests.Frozen_level40_growth']
        receipt=process.run(command,ROOT,owner/'study.log',time.monotonic()+900,log_byte_limit=1048576);io.write(owner/'process.json',receipt)
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx',owner/'study-tests.trx')
        io.check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Incomplete qualification; preserve evidence, no retry')
        for path,pin in inputs.items():io.check(io.sha(Path(path))==pin,'Frozen input drift')
        io.check(sum(p.stat().st_size for p in output.iterdir())<=256*1048576,'Output cap')
        pin=io.sha(output/'files.json');io.manifest(output,pin)
        io.check(io.read(output/'result.json')['status']=='Level40GrowthQualified','Incomplete result')
        done=dict(status='Complete',manifestPin=pin,resultPin=io.sha(output/'result.json'),newSeeds=0,newFights=0)
        io.write(owner/'completion.json',done);print(json.dumps(done,indent=2))
    finally:
        if old is None:os.environ.pop(env,None)
        else:os.environ[env]=old

if __name__=='__main__':main()
