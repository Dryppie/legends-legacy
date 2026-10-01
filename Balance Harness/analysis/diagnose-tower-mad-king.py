"""Explain the fixed floor-ten armor comparison with 96 exact historical replays."""
import argparse
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('mad_king_common', Path(__file__).with_name('diagnose-tower-ni.py'))
helper = importlib.util.module_from_spec(spec); spec.loader.exec_module(helper)
common = helper.common
io = common.io
PLAN_PIN = '80eb74e9bf1b9aa634703c2b8b91961bdb6015e071453d14f5f443e91741c572'
SOURCE_PIN = '389266f5af8deee0627a1d0aae952ebd97e073cbac0a6ff08be1f66f320aea3c'
PREFIX = 'effect.creature.mad_king.'
MODIFIERS = (PREFIX+'unrestrained.damage_dealt', PREFIX+'unrestrained.damage_taken', PREFIX+'bloodlust.lifesteal')


def validate_selection(plan, cells, seeds, trials):
    io.check(plan['version'] == 'floor10-armor-pressure-diagnostic-proposal-v1' and plan['status'] == 'ProposedNotAllocated'
        and plan['floor'] == 10 and plan['diagnosticOnly'] and not plan['usedForAcceptance']
        and plan['maximumHistoricalReplays'] == 96 and plan['allocatedReplays'] == plan['newSeeds'] == plan['retries'] == 0
        and not plan['confirmation'] and not plan['application'], 'Frozen descriptive floor-ten plan required')
    io.check(plan['limits'] == dict(nativeSeconds=600, ownerSeconds=660, maximumBytes=512*1024**2), 'Frozen resource limits required')
    io.check(len(cells) == 278 and len({io.composition_key(c) for c in cells}) == 5
        and all(c['scenario']['floorNumber'] == 10 for c in cells), 'Complete 278-recipe source required')
    lookup = {c['id']: c for c in cells}; keyed = {(t['stage'], t['seed']): t for t in trials}
    io.check(len(lookup) == 278 and len(keyed) == len(trials) == 8896, 'Unique full source schedule required')
    io.check(len(seeds) == len(set(seeds)) == 32 and plan['seeds'] == seeds[:16], 'First sixteen declared seeds required')
    chosen = plan['cells']; io.check(len(chosen) == len({c['cellId'] for c in chosen}) == 6, 'Six unique recipes required')
    identities = set()
    for offset, slots in ((0, [12, 14]), (3, [2, 3])):
        group = chosen[offset:offset+3]; baseline = group[0]['scenario']; full = group[2]['scenario']
        identities.add(io.composition_key(lookup[group[0]['cellId']]))
        io.check([m['partySlot'] for m in baseline['party']] == list(range(1, 16)), 'Fifteen original slots required')
        for c, label, selected in zip(group, ('baseline', 'eight-item armor', 'full armor'), ([], slots, list(range(1,16))), strict=True):
            cell = lookup[c['cellId']]; raw = copy.deepcopy(c['scenario'])
            io.check(c['label'] == label and c['composition'] == cell['composition'] == group[0]['composition']
                and raw == cell['scenario'] and not raw['seeds'], 'Exact saved recipe required')
            specialized = [m['partySlot'] for m in raw['party'] for e in m['build']['equipment'] if '.spec.' in e['definitionId']]
            io.check(len(specialized) == 4*len(selected) and sorted(set(specialized)) == selected, 'Exact equipment budget required')
            for m, b, f in zip(raw['party'], baseline['party'], full['party'], strict=True):
                io.check(m['build']['equipment'] == (f if m['partySlot'] in selected else b)['build']['equipment'], 'Undeclared equipment substitution')
                m['build']['equipment'] = copy.deepcopy(b['build']['equipment'])
            io.check(raw == baseline, 'Raw identities, positions or Essence order changed')
            io.check(c['trials'] == [keyed[c['cellId'], seed] for seed in seeds[:16]], 'Frozen trial identities/order changed')
    io.check(len(identities) == 2, 'Two actual compositions required')
    return [dict(label=('A' if i < 3 else 'B')+'/'+c['label'], cellId=c['cellId'], trial=c['trials'][j])
            for j in range(16) for i, c in enumerate(chosen)]


def modifier_state(events, guardian):
    """Native additive notifications for these three effects; not total combat attributes."""
    values = dict.fromkeys(MODIFIERS, 0)
    for e in events:
        if e['targetId'] == guardian and e['source'] in values and e['eventType'] in ('Buff', 'Debuff', 'BuffExpired'):
            values[e['source']] += e['magnitude']
    return values


def analyze(saved, replay, scenario):
    result = common.base.analyze(saved, replay, 15)
    b = replay['battle']; events = b['eventLog']; stats = b['summary']['statistics']; tps = b['ticksPerSecond']
    seconds = b['summary']['durationSeconds']
    io.check(tps > 0 and all(a['timestamp'] <= z['timestamp'] for a,z in zip(events, events[1:])), 'Chronological events required')
    prepared = b['preparedParticipants']; names = {p['name']:p for p in prepared if p['slot']['side'] == 'Friendly'}
    io.check(len(names) == 15 and set(names) == {m['build']['id'] for m in scenario['party']}, 'Original party identities differ')
    original = {names[m['build']['id']]['slot']['slotId']:m['partySlot'] for m in scenario['party']}
    hostiles = [p for p in prepared if p['slot']['side'] == 'Hostile']; io.check(len(hostiles) == 1, 'One guardian required')
    guardian = hostiles[0]['slot']['slotId']; common.reconcile(events, stats)
    friendly = {s['entityId'] for s in stats if s.get('isSummonedEntity') and s['team'] == 'Friendly'}
    hostile = {s['entityId'] for s in stats if s.get('isSummonedEntity') and s['team'] == 'Hostile'}
    io.check(not hostile, 'Mad King must not have hostile summons')
    summarize = lambda es: common.window(es, original, guardian, hostile, friendly)
    deaths = []; last_damage = {}
    for i,e in enumerate(events):
        target = e['targetId']
        if target not in original: continue
        if e['eventType'] == 'Death':
            deaths.append(dict(index=i, tick=e['timestamp'], slot=original[target], precedingDamage=last_damage.get(target)))
        if e['finalHealthDamage'] > 0:
            last_damage[target] = dict(index=i, tick=e['timestamp'], actorId=e['actorId'], source=e['source'], damageType=e['damageType'],
                                       **{k:e[k] for k in common.DAMAGE})
    first = deaths[0]['index'] if deaths else len(events); boundary = deaths[0]['tick']/tps if deaths else seconds
    windows = {name:dict(exposureSeconds=exposure, **summarize(es)) for name,exposure,es in
        [('beforeFirstDeath',boundary,events[:first]),('fromFirstDeath',max(0,seconds-boundary),events[first:]),('all',seconds,events)]}
    notifications = [dict(index=i, **{k:e[k] for k in ('timestamp','actorId','targetId','source','eventType','magnitude','details')})
        for i,e in enumerate(events) if e['targetId'] == guardian and e['eventType'] in
        ('Buff','Debuff','BuffExpired','StatusEffect','StatusEffectExpired','StatusEffectRemoved')]
    return dict(**result, guardianId=guardian, originalSlots=original, friendlySummons=sorted(friendly), windows=windows,
        originalPartyDeaths=deaths, firstDeathIndex=first if deaths else None,
        guardianModifiersBeforeFirstDeath=modifier_state(events[:first],guardian), guardianNotifications=notifications,
        preparedParty=[dict(slot=m['partySlot'],attributes=names[m['build']['id']]['combatAttributes']) for m in scenario['party']],
        preparedGuardian=hostiles[0]['combatAttributes'],
        meaning='Event-order split includes killing damage before its Death event. No-death fights have no post-death exposure. '
        'Modifier values are net logged contributions from three Mad King effects, not total attributes or inferred condition stacks. '
        'Before/after output is descriptive and includes ongoing effects and friendly summons separately.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('plan','entry','tests','protocol','output'): parser.add_argument('--'+name,type=Path,required=True)
    a = parser.parse_args(); plan = io.read(a.plan); entry = io.read(a.entry); output = a.output.resolve()
    io.check(io.sha(a.plan) == PLAN_PIN and plan['sourceManifestSha256'] == SOURCE_PIN, 'Frozen proposal changed')
    io.check(output.parent == ROOT/'TestResults' and not output.exists(), 'Fresh direct output required')
    for group in ('authenticatedPins','runtimePins','liveCatalogPins','ledgerPins'):
        for path,pin in entry[group].items(): io.check(io.sha(path) == pin, 'Entry binding changed: '+path)
    source = Path(plan['source']); io.authenticate(source); archive = source/'evaluation'
    cells = io.read(source/'cells.json'); seeds = io.read(source/'request.json')['seeds']
    trials = [json.loads(line) for line in (archive/'trials.jsonl').read_text().splitlines()]
    selected = validate_selection(plan,cells,seeds,trials); lookup = {c['id']:c for c in cells}
    history, ledgers = io.history(); io.check(len(history) == entry['initialExclusions'] == plan['initialExclusions'] == 927660, 'Seed union changed')
    tests = io.read(a.tests/'completion.json'); io.check(tests['status'] == 'Passed', 'Diagnostic tests must pass')
    for path,pin in tests['sourcePins'].items(): io.check(io.sha(path) == pin, 'Tested source changed')
    saved = {}; rows = {r['id']:r for r in io.read(source/'result.json')['rows']}
    for cell in plan['cells']:
        panel = {}
        for t in trials:
            if t['stage'] != cell['cellId']: continue
            io.check(io.read(archive/'recipes'/(t['recipe']+'.json')) == dict(cell['scenario'],seeds=seeds), 'Raw archived recipe changed')
            report = json.loads(gzip.decompress((archive/'battles'/(t['id']+'.json.gz')).read_bytes()))
            io.check(report['battle']['seed'] == t['seed'] and t['seed'] not in panel, 'Archived seed differs')
            panel[t['seed']] = report; saved[t['id']] = report
        io.check(set(panel) == set(seeds) and sum(r['succeeded'] for r in panel.values()) == rows[cell['cellId']]['wins'], 'Saved panel recount differs')
    paths = [Path(__file__),Path(helper.__file__),Path(common.__file__),Path(common.base.__file__),Path(io.__file__),a.plan,a.entry,a.protocol,
             Path(__file__).with_name('test-tower-mad-king-diagnostic.py'),ROOT/'build/bounded_windows_process.py',*a.tests.iterdir()]
    pins = {str(p.resolve()):io.sha(p) for p in paths if p.is_file()}
    output.mkdir(); shutil.copy2(__file__,output/'owner-source.py'); shutil.copy2(a.protocol,output/'protocol.md')
    io.write(output/'declaration.json',dict(source=str(source),sourceManifestSha256=SOURCE_PIN,selected=selected,sourcePins=pins,
        runtimePins=entry['runtimePins'],liveCatalogPins=entry['liveCatalogPins'],ledgerPins=ledgers,limits=plan['limits'],
        maximumReplays=96,maximumReplaySeconds=60,maximumLogBytes=16*1024**2,newSeeds=0,usedForAcceptance=False,resourceAdmission=entry['resource']))
    process = common.base.module('mad_king_process',ROOT/'build/bounded_windows_process.py')
    runtime = Path(entry['runtime'])/'bin/EssenceSystem.Tests/release'; deadline = time.monotonic()+600
    attempts = 0; details = []; status = 'Failed'; native_seconds = 0
    print('Admitted 96 historical replays; all 192 saved comparison outcomes recounted.',flush=True)
    try:
        for item in selected:
            t = item['trial']; log = output/(t['id']+'.log')
            command = [shutil.which('dotnet'),str(runtime/'BalanceHarness.dll'),'tower-loadout-replay','--run',str(archive),'--battle',t['id'],'--detailed']
            attempts += 1; io.write(output/(t['id']+'-attempt.json'),dict(attempt=attempts,command=command))
            receipt = process.run(command,ROOT,log,min(deadline,time.monotonic()+60),log_byte_limit=16*1024**2,
                observe=lambda observation:io.write(output/(t['id']+'-observation.json'),observation))
            io.write(output/(t['id']+'-process.json'),receipt); native_seconds += receipt['seconds']
            io.check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Replay failed; no retry')
            replay = helper.parse(log.read_bytes()); details.append(dict(label=item['label'],cellId=item['cellId'],seed=t['seed'],trial=t['id'],
                **analyze(saved[t['id']],replay,lookup[item['cellId']]['scenario'])))
            io.write(output/(t['id']+'-compression.json'),helper.compress_verified(log,output))
            if len(details)%6 == 0: print(f'Completed {len(details)}/96 exact full-report matches.',flush=True)
            io.check(sum(p.stat().st_size for p in output.iterdir() if p.is_file()) < plan['limits']['maximumBytes'], 'Archive byte cap exceeded')
        for group in (pins,entry['runtimePins'],entry['liveCatalogPins'],ledgers):
            for path,pin in group.items(): io.check(io.sha(path) == pin, 'Frozen input changed')
        io.check(io.history()[0] == history, 'Diagnostic allocated seeds')
        io.write(output/'details.json',details)
        io.check(sum(p.stat().st_size for p in output.iterdir() if p.is_file()) < plan['limits']['maximumBytes'], 'Final archive byte cap exceeded')
        io.write(output/'result.json',dict(status='VerifiedDescriptiveDiagnostic',historicalReplays=96,recountedSavedOutcomes=192,
            eventCount=sum(r['eventCount'] for r in details),nativeSeconds=native_seconds,newSeeds=0,newStudyFights=0,usedForAcceptance=False,finalExclusions=len(history)))
        status = 'Complete'
    finally:
        io.write(output/'completion.json',dict(status=status,attemptedReplays=attempts,completedReplays=len(details),newSeeds=0,usedForAcceptance=False))
        io.write(output/'files.json',{p.name:io.sha(p) for p in sorted(output.iterdir()) if p.is_file()})


if __name__ == '__main__': main()
