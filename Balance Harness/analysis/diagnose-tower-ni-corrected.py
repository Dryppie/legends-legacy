"""Explain the corrected-runtime Ni gear gap with 96 exact historical replays.

The pre-fix diagnostic remains closed. Reuse its event/report accounting while
requiring the separately frozen corrected source, scalar and raw recipes.
"""
import argparse
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
s = importlib.util.spec_from_file_location('ni_historical_accounting', Path(__file__).with_name('diagnose-tower-ni.py'))
ni = importlib.util.module_from_spec(s); s.loader.exec_module(ni)
common = ni.common
io = ni.io
PLAN_PIN = 'a7bec338180ac7474e81e34b1697315104e5ede876c7f00ef032fd66963bda61'
SOURCE_PIN = 'bfdb640a0a772a8cc88d3657709d345660a008030f0b209b560f2c86cb79fc83'
PREFIX, SEAL, POWER, SWAP, SPAWN = ni.PREFIX, ni.SEAL, ni.POWER, ni.SWAP, ni.SPAWN
analyze, parse, compress_verified, resource_admission = ni.analyze, ni.parse, ni.compress_verified, ni.resource_admission


def validate_selection(plan, cells, seeds, trials):
    io.check(plan['version'] == 'floor9-corrected-pressure-diagnostic-proposal-v1' and plan['status'] == 'ProposedNotExecuted'
        and plan['floor'] == 9 and plan['maximumHistoricalReplays'] == 96
        and plan['allocatedReplays'] == plan['newSeeds'] == plan['newStudyFights'] == 0
        and not plan['usedForAcceptance'] and not plan['gameplayCandidateSelected']
        and plan['requireCompleteSavedReportParityAfterRemovingEventLog'], 'Frozen corrected Ni plan required')
    io.check((plan['maximumSeconds'], plan['perReplaySeconds'], plan['maximumLogBytes'], plan['maximumBytes'])
        == (840, 60, 16*1024**2, 2*1024**3) and plan['requiresMeasuredResourceAdmission']
        and plan['maximumAdmittedSeconds'] == 672 and plan['maximumAdmittedBytes'] == .8*2*1024**3
        and plan['retries'] == plan['extensions'] == 0, 'Frozen resource limits required')
    io.check(plan['sourceManifestSha256'] == SOURCE_PIN and plan['sourceOffenseFactor'] == 1.25
        and plan['sourceOffense'] == 5.8795166015 and plan['initialExclusions'] == 926348, 'Wrong corrected source or offense')
    io.check(len(cells) == plan['familySize'] == 148 and len({io.composition_key(c) for c in cells}) == plan['actualCompositions'] == 5
        and all(c['scenario']['floorNumber'] == 9 for c in cells), 'Complete floor-nine family required')
    io.check(len(seeds) == len(set(seeds)) == 32 and plan['seeds'] == seeds[:16], 'First sixteen declared seeds required')
    lookup = {c['id']: c for c in cells}; io.check(len(lookup) == 148, 'Duplicate raw recipe')
    io.check(len(plan['comparisons']) == 2, 'Two comparisons required')
    chosen = []; identities = set()
    for label, slots, group in zip(('A', 'B'), ([2, 4], [3, 9]), plan['comparisons'], strict=True):
        io.check(group['label'] == label and group['limitedPartySlots'] == slots, 'Frozen limited slots differ')
        plain, limited, full = [lookup[group[k]] for k in ('baselineId', 'limitedId', 'fullId')]
        reference = plain['scenario']; identities.add(io.composition_key(plain))
        io.check(plain['gear'] == 'baseline' and full['gear'] == 'resistance-and-health'
            and [m['partySlot'] for m in reference['party']] == list(range(1, 11)), 'Original endpoints required')
        for profile, cell, selected in [('baseline', plain, []), ('limited-resistance', limited, slots), ('full-resistance', full, list(range(1, 11)))]:
            raw = copy.deepcopy(cell['scenario'])
            actual = [m['partySlot'] for m in raw['party'] for item in m['build']['equipment'] if '.spec.' in item['definitionId']]
            io.check(cell['composition'] == group['composition'] and not raw['seeds'] and len(actual) == 4*len(selected)
                and sorted(set(actual)) == selected, 'Exact saved equipment counts required')
            for member, baseline, resistant in zip(raw['party'], reference['party'], full['scenario']['party'], strict=True):
                expected = resistant if member['partySlot'] in selected else baseline
                io.check(member['build']['equipment'] == expected['build']['equipment'], 'Undeclared equipment substitution')
                member['build']['equipment'] = copy.deepcopy(baseline['build']['equipment'])
            io.check(raw == reference, 'Raw identity, Essence order, position or budget changed')
            chosen.append(dict(label=label+'/'+profile, cell=cell['id'], resistanceSlots=selected))
    io.check(len(identities) == 2 and len({c['cell'] for c in chosen}) == 6, 'Six exact recipes and two actual compositions required')
    keyed = {(t['stage'], t['seed']): t for t in trials}
    io.check(len(keyed) == len(trials) == 4736, 'Complete unique trial schedule required')
    selected = [dict(cell=c['cell'], seed=seed, trial=keyed[c['cell'], seed]['id']) for seed in seeds[:16] for c in chosen]
    io.check(selected == plan['selected'], 'Frozen paired sample changed')
    return chosen


def validate_source(plan, entry):
    source = Path(plan['source']); scope = io.read(source/'scope.json')
    io.check(io.sha(source/'files.json') == SOURCE_PIN and io.sha(source/'cells.json') == plan['sourceCellsSha256'], 'Corrected archive changed')
    io.check(scope['execution'] == plan['runtimeExecution'] and entry['runtime'] == plan['runtime'], 'Corrected runtime required')
    for name, pin in plan['runtimeExecution']['assemblyHashes'].items():
        for project in ('EssenceSystem.Tests', 'BalanceHarness'):
            io.check(io.sha(Path(entry['runtime'])/f'bin/{project}/release/{name}.dll') == pin, 'Corrected assembly changed')
    io.check(io.sha(plan['precedingEvidence']) == plan['precedingEvidenceSha256'], 'Closed calibration evidence changed')
    evidence = io.read(plan['precedingEvidence'])
    io.check(evidence['status'] == 'Verified' and evidence['selection']['selectedFactor'] is None
        and evidence['finalExclusions'] == 926348 and evidence['panels'][0]['source'] == str(source)
        and evidence['panels'][0]['manifestSha256'] == SOURCE_PIN, 'Closed unselected calibration required')
    # The source is an isolated numerical candidate, not a new live baseline.
    helper = common.base.module('ni_source_scalar', Path(__file__).with_name('tower-ni-offense-calibration.py'))
    original = ROOT/'TestResults/tower-balance-pass-floor9-summon-defense-preparation-study-20260929'
    hashes = helper.verify_candidate(original/'content', source/'content', 1.25)
    io.check(scope['contentHashes'] == hashes and scope['settings'] == io.read(original/'scope.json')['settings'], 'Native scalar scope changed')
    helper.validate_family(io.read(source/'cells.json'))
    return source


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('plan','entry','tests','protocol','output'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args(); plan=io.read(args.plan);entry=io.read(args.entry);output=args.output.resolve()
    io.check(io.sha(args.plan)==PLAN_PIN and plan['sourceManifestSha256']==SOURCE_PIN,'Frozen proposal changed')
    io.check(output.parent==ROOT/'TestResults' and not output.exists(),'Fresh direct TestResults output required')
    for group in ('authenticatedPins','runtimePins','liveCatalogPins','ledgerPins'):
        for path,pin in entry[group].items():io.check(io.sha(path)==pin,'Entry binding changed: '+path)
    validate_source(plan,entry)
    source=Path(plan['source']);io.authenticate(source)
    cells=io.read(source/'cells.json');seeds=io.read(source/'request.json')['seeds'];archive=source/'evaluation'
    trials=[json.loads(line) for line in (archive/'trials.jsonl').read_text().splitlines()]
    chosen=validate_selection(plan,cells,seeds,trials);lookup={c['id']:c for c in cells};saved={};results={r['id']:r for r in io.read(source/'result.json')['rows']}
    for c in chosen:
        panel={}
        for t in trials:
            if t['stage']!=c['cell']:continue
            io.check(io.read(archive/'recipes'/(t['recipe']+'.json'))==dict(lookup[c['cell']]['scenario'],seeds=seeds),'Raw recipe changed')
            raw=json.loads(gzip.decompress((archive/'battles'/(t['id']+'.json.gz')).read_bytes()))
            io.check(raw['battle']['seed']==t['seed'] and t['seed'] not in panel,'Saved seed differs')
            panel[t['seed']]=raw;saved[t['id']]=raw
        io.check(set(panel)==set(seeds) and sum(r['succeeded'] for r in panel.values())==results[c['cell']]['wins'],'Complete saved panel differs')
    history,ledgers=io.history();io.check(len(history)==entry['initialExclusions']==926348,'Exclusion union changed')
    tests=io.read(args.tests/'completion.json');io.check(tests['status']=='Passed','Fresh diagnostic safeguards required')
    for path,pin in tests['sourcePins'].items():io.check(io.sha(path)==pin,'Tested diagnostic source changed')
    resource=resource_admission(entry);io.check(shutil.disk_usage(ROOT).free>resource['projectedBytes']+2*1024**3,'Free disk admission failed')
    paths=[Path(__file__),Path(ni.__file__),Path(common.__file__),Path(common.base.__file__),Path(io.__file__),args.plan,args.entry,args.protocol,
        Path(__file__).with_name('tower-ni-offense-calibration.py'),ROOT/'TestResults/tower-floor9-corrected-ni-collect-20261001.py',
        Path(__file__).with_name('test-tower-ni-corrected-diagnostic.py'),ROOT/'build/bounded_windows_process.py',*args.tests.iterdir()]
    pins={str(p.resolve()):io.sha(p) for p in paths if p.is_file()}
    output.mkdir();shutil.copy2(__file__,output/'owner-source.py');shutil.copy2(args.protocol,output/'protocol.md')
    io.write(output/'declaration.json',dict(plan=str(args.plan),source=str(source),sourceManifestSha256=SOURCE_PIN,chosen=chosen,
        selected=plan['selected'],sourcePins=pins,runtimePins=entry['runtimePins'],liveCatalogPins=entry['liveCatalogPins'],ledgerPins=ledgers,
        initialExclusions=len(history),maximumReplays=96,maximumSeconds=840,maximumReplaySeconds=60,maximumLogBytes=16*1024**2,
        maximumBytes=2*1024**3,newSeeds=0,usedForAcceptance=False))
    io.write(output/'resource-admission.json',resource)
    process=common.base.module('ni_process',ROOT/'build/bounded_windows_process.py');deadline=time.monotonic()+840
    runtime=Path(entry['runtime'])/'bin/EssenceSystem.Tests/release';labels={c['cell']:c['label'] for c in chosen}
    attempts=0;details=[];status='Failed'
    print(f'Admitted 96 historical replays; doubled native estimate {resource["projectedSeconds"]:.2f}s. Recounted 192 saved outcomes.',flush=True)
    try:
        for item in plan['selected']:
            trial=item['trial'];log=output/(trial+'.log')
            command=[shutil.which('dotnet'),str(runtime/'BalanceHarness.dll'),'tower-loadout-replay','--run',str(archive),'--battle',trial,'--detailed']
            attempts+=1;io.write(output/(trial+'-attempt.json'),dict(attempt=attempts,command=command))
            receipt=process.run(command,ROOT,log,min(deadline,time.monotonic()+60),log_byte_limit=16*1024**2,
                observe=lambda observation:io.write(output/(trial+'-observation.json'),observation))
            io.write(output/(trial+'-process.json'),receipt)
            io.check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Replay failed; preserve without retry')
            replay=parse(log.read_bytes());details.append(dict(label=labels[item['cell']],**item,**analyze(saved[trial],replay,lookup[item['cell']]['scenario'])))
            io.write(output/(trial+'-compression.json'),compress_verified(log,output))
            if len(details)%6==0:print(f'Completed {len(details)}/96 complete-report matches.',flush=True)
            io.check(sum(p.stat().st_size for p in output.iterdir() if p.is_file())<2*1024**3,'Archive byte limit exceeded')
        for group in (pins,entry['runtimePins'],entry['liveCatalogPins'],ledgers):
            for path,pin in group.items():io.check(io.sha(path)==pin,'Frozen input changed')
        io.check(io.history()[0]==history and io.sha(source/'files.json')==SOURCE_PIN,'Source or seed union changed')
        io.write(output/'details.json',details)
        io.write(output/'result.json',dict(status='VerifiedDescriptiveDiagnostic',historicalReplays=96,recountedSavedOutcomes=192,
            eventCount=sum(r['eventCount'] for r in details),newSeeds=0,newStudyFights=0,usedForAcceptance=False,finalExclusions=len(history)))
        status='Complete'
    finally:
        io.write(output/'completion.json',dict(status=status,attemptedReplays=attempts,completedReplays=len(details),newSeeds=0,usedForAcceptance=False))
        io.write(output/'files.json',{p.name:io.sha(p) for p in sorted(output.iterdir()) if p.is_file()})


if __name__=='__main__':main()
