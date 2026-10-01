"""Explain the frozen floor-nine equipment gap with 96 exact historical replays."""
import argparse
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('ni_common', Path(__file__).with_name('diagnose-tower-kodoku.py'))
common = importlib.util.module_from_spec(spec); spec.loader.exec_module(common)
io = common.io
PLAN_PIN = '31d196272058c1f03a1e7861c272171c615732e379d858e087cf4a854c10473e'
SOURCE_PIN = '62b8b1d408209997bb5beab90dc09b20412d02bf612caee0554754aa3bf6ae77'
PREFIX = 'effect.creature.ni.'
SEAL = PREFIX + 'ninth_seal.damage'
POWER = PREFIX + 'ninefold.power'
SWAP = PREFIX + 'one_among_nine.swap'
SPAWN = PREFIX + 'ninefold.summon'


def validate_selection(plan, cells, seeds, trials):
    io.check(plan['version'] == 'floor9-ni-pressure-diagnostic-proposal-v1' and plan['status'] == 'ProposedNotExecuted'
        and plan['floor'] == 9 and plan['maximumHistoricalReplays'] == 96
        and plan['allocatedReplays'] == plan['newSeeds'] == plan['newStudyFights'] == 0
        and not plan['usedForAcceptance'] and not plan['gameplayCandidateSelected']
        and plan['requireCompleteSavedReportParityAfterRemovingEventLog'], 'Frozen descriptive Ni plan required')
    io.check((plan['maximumSeconds'], plan['perReplaySeconds'], plan['maximumLogBytes'], plan['maximumBytes'])
        == (840, 60, 16*1024**2, 2*1024**3) and plan['requiresMeasuredResourceAdmission']
        and plan['maximumAdmittedSeconds'] == 672 and plan['maximumAdmittedBytes'] == .8*2*1024**3, 'Frozen resource limits required')
    io.check(len(cells) == plan['familySize'] == 148 and len({io.composition_key(c) for c in cells}) == plan['actualCompositions'] == 5
        and all(c['scenario']['floorNumber'] == 9 for c in cells), 'Complete floor-nine family required')
    io.check(len(seeds) == len(set(seeds)) == 32 and plan['seeds'] == seeds[:16], 'First sixteen declared seeds required')
    lookup = {c['id']: c for c in cells}; io.check(len(lookup) == 148, 'Duplicate raw recipe')
    io.check(len(plan['comparisons']) == 2, 'Two comparisons required')
    chosen = []; identities = set()
    for label, slots, group in zip(('A', 'B'), ([3, 4], [4, 8]), plan['comparisons'], strict=True):
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


def ni_events(events, guardian, original, copies):
    """Record native notifications without inferring unlogged current attributes."""
    alive = set(); created = set(); ended = set(); history = []; seals = []; power = []; swaps = []
    for index, event in enumerate(events):
        e = event; target = e['targetId']; source = e['source']
        record = dict(index=index, tick=e['timestamp'], actorId=e['actorId'], targetId=target, source=source,
                      eventType=e['eventType'], magnitude=e['magnitude'], details=e['details'])
        if source == SPAWN and e['eventType'] == 'Summon':
            io.check(e['actorId'] == guardian and target in copies and target not in created, 'Invalid Ni copy spawn')
            created.add(target); alive.add(target); history.append(dict(**record, observedCopiesAfter=len(alive)))
        if target in copies and e['eventType'] in ('Death', 'SummonExpired'):
            io.check(target in created, 'Copy ended before spawn')
            alive.discard(target); ended.add(target); history.append(dict(**record, observedCopiesAfter=len(alive)))
        if source == SEAL and target in original and e['incomingRawDamage'] > 0:
            io.check(e['actorId'] == guardian, 'Ninth Seal actor differs')
            seals.append(dict(**record, slot=original[target], observedCopiesBefore=len(alive),
                              **{k:e[k] for k in common.DAMAGE}))
        if source == POWER and e['eventType'] in ('Buff', 'Debuff'):
            io.check(e['actorId'] == guardian and target == guardian, 'Power notification recipient differs')
            power.append(dict(**record, observedCopiesBefore=len(alive)))
        if source == SWAP and e['eventType'] == 'Buff':
            io.check(e['actorId'] == guardian and target in alive and e['magnitude'] >= 0, 'Health swap target or direction differs')
            swaps.append(dict(**record, guardianHealthGain=e['magnitude'], observedCopiesBefore=len(alive)))
    io.check(created == set(copies), 'Missing Ni copy spawn events')
    return dict(copyEvents=history, ninthSealHits=seals, powerNotifications=power, healthSwaps=swaps,
                remainingObservedCopies=sorted(alive), endedCopies=sorted(ended),
                loggedPowerGain=sum(e['magnitude'] for e in power), loggedSwapHealthGain=sum(e['guardianHealthGain'] for e in swaps),
                meaning='Copy counts follow logged spawn/end event order; same-tick notification order is preserved. '
                        'Power magnitudes are native flat increments, not current Power after other modifiers. '
                        'Swap magnitude is native rounded Health gained by Ni; it is not healing to the copy target.')


def analyze(saved, replay, scenario):
    result = common.analyze(saved, replay, scenario)
    b = replay['battle']; events = b['eventLog']; stats = b['summary']['statistics']
    guardian = result['guardianId']; original = result['originalSlots']; copies = set(result['hostileSummons'])
    io.check(len(copies) == 9 and all(s.startswith(guardian+':summon:niCopy:') for s in copies), 'Nine original Ni copies required')
    common.reconcile(events, [s for s in stats if s['entityId'] in copies])
    io.check(not any(e['actorId'] in copies and (e['eventType'] == 'AbilityUse' or e['incomingRawDamage'] > 0) for e in events), 'Ni copies must remain inert')
    result['notifications'] = [dict(index=i, **{k:e[k] for k in ('timestamp','actorId','targetId','source','eventType','magnitude','details')})
        for i,e in enumerate(events) if (e['source'] or '').startswith(PREFIX)]
    tps = b['ticksPerSecond']; duration = b['summary']['durationSeconds']
    result['niWindows'] = {name:dict(exposureSeconds=max(0,min(duration,stop)-min(duration,start)),
        **common.window([e for e in events if start*tps <= e['timestamp'] < stop*tps], original, guardian, copies, set(result['friendlySummons'])))
        for name,start,stop in [('before16',0,16),('16to32',16,32),('32to48',32,48),('after48',48,float('inf'))]}
    result['ni'] = ni_events(events,guardian,original,copies)
    result['copyStats'] = [{k:s.get(k) for k in ('entityId','health','maxHealth','firstDeathTick','summonedAtTick','summonEndedAtTick',*common.DAMAGE)}
                          for s in stats if s['entityId'] in copies]
    result['preparedParty'] = [dict(slot=m['partySlot'], attributes=next(p['combatAttributes'] for p in b['preparedParticipants'] if p['name']==m['build']['id']))
                               for m in scenario['party']]
    return result


def parse(raw):
    text = raw.decode('utf-8-sig').lstrip(); value,end = json.JSONDecoder().raw_decode(text)
    io.check(text[end:].strip() == 'Replay matched the saved input, combat result and Tower outcome.', 'Incomplete native replay output')
    return value


def compress_verified(log, output):
    io.check(log.resolve().parent == output.resolve() and log.suffix == '.log', 'Only owned temporary logs may be compressed')
    raw = log.read_bytes(); parse(raw); target = log.with_suffix('.log.gz'); io.check(not target.exists(), 'Fresh compressed path required')
    with target.open('xb') as stream: stream.write(gzip.compress(raw, compresslevel=1, mtime=0))
    io.check(gzip.decompress(target.read_bytes()) == raw, 'Compressed replay differs')
    record = dict(rawBytes=len(raw),rawSha256=io.sha(log),compressedBytes=target.stat().st_size,compressedSha256=io.sha(target))
    log.unlink(); return record


def resource_admission(entry):
    io.check(entry['referenceReplays'] == 96 and entry['referenceReplaySeconds'] > 0 and entry['referenceArchiveBytes'] > 0, 'Measured resource reference required')
    r = dict(reference=entry['resourceReference'],referenceReplays=96,plannedReplays=96,safetyFactor=2,
        projectedSeconds=2*entry['referenceReplaySeconds'],projectedBytes=2*entry['referenceArchiveBytes']+80*1024**2,
        admittedSeconds=672,admittedBytes=.8*2*1024**3,maximumRawLogBytes=16*1024**2)
    io.check(r['projectedSeconds'] < r['admittedSeconds'] and r['projectedBytes'] < r['admittedBytes'], 'Measured resource admission failed')
    return r


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('plan','entry','tests','protocol','output'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args(); plan=io.read(args.plan);entry=io.read(args.entry);output=args.output.resolve()
    io.check(io.sha(args.plan)==PLAN_PIN and plan['sourceManifestSha256']==SOURCE_PIN,'Frozen proposal changed')
    io.check(output.parent==ROOT/'TestResults' and not output.exists(),'Fresh direct TestResults output required')
    for group in ('authenticatedPins','runtimePins','liveCatalogPins','ledgerPins'):
        for path,pin in entry[group].items():io.check(io.sha(path)==pin,'Entry binding changed: '+path)
    for path,pin in plan['sourcePins'].items():io.check(io.sha(path)==pin,'Frozen selection input changed')
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
    history,ledgers=io.history();io.check(len(history)==entry['initialExclusions']==925996,'Exclusion union changed')
    tests=io.read(args.tests/'completion.json');io.check(tests['status']=='Passed','Fresh diagnostic safeguards required')
    for path,pin in tests['sourcePins'].items():io.check(io.sha(path)==pin,'Tested diagnostic source changed')
    resource=resource_admission(entry);io.check(shutil.disk_usage(ROOT).free>resource['projectedBytes']+2*1024**3,'Free disk admission failed')
    paths=[Path(__file__),Path(common.__file__),Path(common.base.__file__),Path(io.__file__),args.plan,args.entry,args.protocol,
        Path(__file__).with_name('test-tower-ni-diagnostic.py'),ROOT/'build/bounded_windows_process.py',*args.tests.iterdir()]
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
