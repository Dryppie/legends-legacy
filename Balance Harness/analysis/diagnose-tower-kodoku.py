"""Bounded floor-eight explanation using 96 exact historical replays, never acceptance."""
import argparse
from collections import defaultdict
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('kodoku_gear', Path(__file__).with_name('diagnose-tower-gear.py'))
base = importlib.util.module_from_spec(spec); spec.loader.exec_module(base)
io = base.io
PLAN_PIN = '6dd48ba6049092e87ed1dc9cb6a01230290ae548ca3243336b6a2e822caa47f2'
SOURCE_PIN = 'fc3bf5eded69b653f2f567249e7b7bea719ff311a48379b7454c671427bfbe56'
DAMAGE = ('incomingRawDamage', 'physicalMitigationPrevented', 'magicalMitigationPrevented', 'finalHealthDamage')
PREFIX = 'effect.creature.kodoku.'


def validate_selection(plan, review, cells, seeds, trials):
    io.check(plan['version'] == 'floor8-gear-diagnostic-proposal-v1' and plan['status'] == 'ProposedNotExecuted'
             and plan['floor'] == 8 and plan['maximumHistoricalReplays'] == 96
             and plan['allocatedReplays'] == plan['newSeeds'] == 0 and not plan['usedForAcceptance'], 'Unallocated floor-eight diagnostic required')
    io.check((plan['maximumSeconds'], plan['perReplaySeconds'], plan['maximumLogBytes'], plan['maximumBytes'])
             == (840, 60, 16*1024**2, 2*1024**3), 'Frozen resource limits required')
    io.check(len(cells) == 177 and len({io.composition_key(c) for c in cells}) == 9
             and all(c['scenario']['floorNumber'] == 8 for c in cells), 'Complete floor-eight family required')
    io.check(len(seeds) == len(set(seeds)) == 32, 'Complete source seed panel required')
    lookup = {c['id']: c for c in cells}; io.check(len(lookup) == 177, 'Duplicate raw recipe')
    chosen = []; compositions = set()
    io.check(len(review['comparisons']) == 2, 'Two original comparisons required')
    for label, comp in zip(('A', 'B'), review['comparisons'], strict=True):
        plain, limited, full = [lookup[comp[k]] for k in ('baselineId', 'limitedId', 'fullId')]
        reference = plain['scenario']; compositions.add(io.composition_key(plain))
        io.check(plain['gear'] == 'baseline' and full['gear'] == 'armor-and-health'
                 and [m['partySlot'] for m in reference['party']] == list(range(1, 11)), 'Original ten-character endpoints required')
        for profile, cell, slots in (('baseline', plain, []), ('limited-armor', limited, comp['limitedPartySlots']),
                                     ('full-armor', full, list(range(1, 11)))):
            raw = copy.deepcopy(cell['scenario'])
            specialized = [m['partySlot'] for m in raw['party'] for item in m['build']['equipment'] if '.spec.' in item['definitionId']]
            io.check(cell['composition'] == comp['composition'] and not raw['seeds']
                     and len(specialized) == 4*len(slots) and sorted(set(specialized)) == slots, 'Exact equipment counts and slots required')
            for m, b, f in zip(raw['party'], reference['party'], full['scenario']['party'], strict=True):
                expected = f if m['partySlot'] in slots else b
                io.check(m['build']['equipment'] == expected['build']['equipment'], 'Undeclared equipment substitution')
                m['build']['equipment'] = copy.deepcopy(b['build']['equipment'])
            io.check(raw == reference, 'Raw identity, Essence order, positions or budget changed')
            chosen.append(dict(label=label+'/'+profile, cell=cell['id'], armorSlots=slots))
    io.check(len(compositions) == 2 and len({c['cell'] for c in chosen}) == 6, 'Six recipes and two actual compositions required')
    keyed = {(t['stage'], t['seed']): t for t in trials}
    io.check(len(keyed) == len(trials) == 177*32, 'Complete unique source trials required')
    selected = [dict(cell=c['cell'], seed=seed, trial=keyed[c['cell'], seed]['id']) for seed in seeds[:16] for c in chosen]
    io.check(selected == plan['selected'], 'Frozen paired sample changed')
    return chosen


def reconcile(events, stats):
    """Recipient accounting includes self/allied damage and excludes unrelated targets."""
    for s in stats:
        es = [e for e in events if e['targetId'] == s['entityId']]
        for key in DAMAGE:
            io.check(sum(e[key] for e in es) == s[key], 'Recipient damage does not reconcile: '+key)
        deaths = [e['timestamp'] for e in es if e['eventType'] == 'Death']
        io.check(sum(e['magnitude'] for e in es if e['eventType'] in base.HEALING) == s['healingReceived']
                 and sum(e['magnitude'] for e in es if e['eventType'] == 'HealthRegeneration') == s['healthRegenerated']
                 and (min(deaths) if deaths else None) == s['firstDeathTick'], 'Recipient restoration/death does not reconcile')


def window(events, original, guardian, hostile_summons, friendly_summons):
    groups = {'party': set(original), 'guardian': {guardian}, 'hostileSummons': hostile_summons}
    def origin(actor):
        return ('originalParty' if actor in original else 'guardian' if actor == guardian else
                'hostileSummon' if actor in hostile_summons else 'friendlySummon' if actor in friendly_summons else 'other')
    result = {}
    for name, targets in groups.items():
        es = [e for e in events if e['targetId'] in targets]; damage = defaultdict(lambda: dict.fromkeys(DAMAGE, 0)); healing = defaultdict(int)
        for e in es:
            if e['incomingRawDamage'] or e['finalHealthDamage']:
                for k in DAMAGE: damage[(origin(e['actorId']), e['source'], e['damageType'])][k] += e[k]
            if e['eventType'] in base.HEALING: healing[e['source']] += e['magnitude']
        result[name] = dict(**{k: sum(e[k] for e in es) for k in DAMAGE},
            healing=sum(healing.values()), regeneration=sum(e['magnitude'] for e in es if e['eventType'] == 'HealthRegeneration'),
            deaths=sum(e['eventType'] == 'Death' for e in es), healingBySource=dict(healing),
            damageBySource=[dict(origin=k[0], source=k[1], damageType=k[2], **v) for k, v in sorted(damage.items(), key=lambda kv: str(kv[0]))])
    return result


def analyze(saved, replay, scenario):
    result = base.analyze(saved, replay, 10)
    b = replay['battle']; events = b['eventLog']; tps = b['ticksPerSecond']; seconds = b['summary']['durationSeconds']
    io.check(tps > 0 and seconds >= 0 and all(a['timestamp'] <= z['timestamp'] for a, z in zip(events, events[1:])), 'Chronological events required')
    prepared = b['preparedParticipants']; names = {p['name']: p for p in prepared if p['slot']['side'] == 'Friendly'}
    io.check(len(names) == 10 and set(names) == {m['build']['id'] for m in scenario['party']}, 'Original party identity differs')
    original = {names[m['build']['id']]['slot']['slotId']: m['partySlot'] for m in scenario['party']}
    hostiles = [p for p in prepared if p['slot']['side'] == 'Hostile']; io.check(len(hostiles) == 1, 'Exactly one prepared guardian required')
    guardian = hostiles[0]['slot']['slotId']; stats = b['summary']['statistics']
    recipients = [s for s in stats if s['entityId'] in {*original, guardian}]
    io.check(len(recipients) == 11, 'Complete original recipient statistics required'); reconcile(events, recipients)
    hostile_summons = {s['entityId'] for s in stats if s.get('isSummonedEntity') and s['team'] == 'Hostile'}
    friendly_summons = {s['entityId'] for s in stats if s.get('isSummonedEntity') and s['team'] == 'Friendly'}
    summarize = lambda es: window(es, original, guardian, hostile_summons, friendly_summons)
    windows = {}
    for name, start, stop in [('before15', 0, 15), ('15to40', 15, 40), ('after40', 40, float('inf')), ('all', 0, float('inf'))]:
        windows[name] = dict(exposureSeconds=max(0, min(seconds, stop)-min(seconds, start)),
                             **summarize([e for e in events if start*tps <= e['timestamp'] < stop*tps]))
    deaths = []; last_damage = {}
    for index, e in enumerate(events):
        if e['targetId'] in original:
            if e['eventType'] == 'Death':
                deaths.append(dict(index=index, tick=e['timestamp'], slot=original[e['targetId']], precedingDamage=last_damage.get(e['targetId'])))
            if e['finalHealthDamage'] > 0:
                last_damage[e['targetId']] = dict(index=index, tick=e['timestamp'], source=e['source'], actorId=e['actorId'],
                    damageType=e['damageType'], **{k: e[k] for k in DAMAGE})
    first = deaths[0]['index'] if deaths else len(events)
    boundary = deaths[0]['tick']/tps if deaths else seconds
    ordered = {'beforeFirstDeath': dict(exposureSeconds=boundary, **summarize(events[:first])),
               'fromFirstDeath': dict(exposureSeconds=max(0, seconds-boundary), **summarize(events[first:]))}
    notifications = [dict(index=i, tick=e['timestamp'], actorId=e['actorId'], targetId=e['targetId'], source=e['source'],
        eventType=e['eventType'], magnitude=e['magnitude'], details=e['details']) for i, e in enumerate(events) if (e['source'] or '').startswith(PREFIX)]
    return dict(**result, windows=windows, eventOrderWindows=ordered, originalPartyDeaths=deaths, notifications=notifications,
        guardianId=guardian, originalSlots=original, hostileSummons=sorted(hostile_summons), friendlySummons=sorted(friendly_summons),
        recipientStats=[{k: s.get(k, 0) for k in ('entityId', 'entityName', 'health', 'maxHealth', 'firstDeathTick', 'healingPotential',
            'overhealing', 'healthRegenerationPotential', 'healthRegenerationOverhealed', 'healthRegenerationPulses')} for s in recipients],
        meaning='Actual event amounts, not counterfactual healing or exact status stacks. Half-open time windows; '
                'event-order split places killing damage before its Death event. Post-death damage may include ongoing conditions. '
                'Summon attribution uses native team/isSummonedEntity fields. Timing correlation does not prove a causal effect.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for k in ('plan', 'entry', 'artifacts', 'protocol', 'tests', 'output'): parser.add_argument('--'+k, type=Path, required=True)
    a = parser.parse_args(); plan = io.read(a.plan); entry = io.read(a.entry); output = a.output.resolve()
    io.check(io.sha(a.plan) == PLAN_PIN and plan['sourceManifestSha256'] == SOURCE_PIN, 'Unrecognized frozen proposal')
    io.check(output.parent == ROOT/'TestResults' and not output.exists(), 'Fresh direct TestResults output required')
    for group in ('authenticatedPins', 'liveCatalogPins', 'runtimePins', 'ledgerPins'):
        for path, pin in entry[group].items(): io.check(io.sha(path) == pin, 'Entry binding changed: '+path)
    source = Path(plan['source']); io.check(io.sha(source/'files.json') == SOURCE_PIN, 'Source changed'); io.authenticate(source)
    for k in ('aggregateEvidence', 'savedReview'): io.check(io.sha(plan[k]) == plan[k+'Sha256'], 'Evidence changed')
    archive = source/'evaluation'; scope = io.read(source/'scope.json'); seeds = io.read(source/'request.json')['seeds']; cells = io.read(source/'cells.json')
    trials = [json.loads(line) for line in (archive/'trials.jsonl').read_text().splitlines()]
    chosen = validate_selection(plan, io.read(plan['savedReview']), cells, seeds, trials); lookup = {c['id']: c for c in cells}
    runtime = a.artifacts.resolve()/'bin/EssenceSystem.Tests/release'
    for name, pin in scope['execution']['assemblyHashes'].items(): io.check(io.sha(runtime/(name+'.dll')) == pin, 'Historical runtime changed')
    for relative, pin in scope['contentHashes'].items(): io.check(io.sha(ROOT/'LL/src/API/API.LL/Data'/relative) == pin, 'Current content changed')
    history, ledgers = io.history(); io.check(len(history) == entry['initialExclusions'] == plan['initialExclusions'] == 923900, 'Unexpected reservations')
    saved = {}; results = {r['id']: r for r in io.read(source/'result.json')['rows']}
    for c in chosen:
        panel = {}
        for t in trials:
            if t['stage'] != c['cell']: continue
            io.check(io.read(archive/'recipes'/(t['recipe']+'.json')) == dict(lookup[c['cell']]['scenario'], seeds=seeds), 'Raw recipe changed')
            report = json.loads(gzip.decompress((archive/'battles'/(t['id']+'.json.gz')).read_bytes()))
            io.check(report['battle']['seed'] == t['seed'] and t['seed'] not in panel, 'Invalid saved seed')
            panel[t['seed']] = report; saved[t['id']] = report
        io.check(set(panel) == set(seeds) and sum(r['succeeded'] for r in panel.values()) == results[c['cell']]['wins'], 'Saved panel recount differs')
    reference_path = ROOT/'TestResults/tower-floor7-survival-output-evidence-20260930.json'; reference = io.read(reference_path)
    io.check(str(reference_path) in entry['authenticatedPins'] and reference['replays'] == 48, 'Authenticated resource reference required')
    resource = dict(source=str(reference_path), sha256=io.sha(reference_path), referenceReplays=48, plannedReplays=96,
        partySizeFactor=2, safetyFactor=2, projectedSeconds=4*reference['summedProcessSeconds']*96/48,
        projectedBytes=4*reference['archiveBytes']*96/48, admittedSeconds=.8*840, admittedBytes=.8*2*1024**3)
    io.check(resource['projectedSeconds'] < resource['admittedSeconds'] and resource['projectedBytes'] < resource['admittedBytes']
             and shutil.disk_usage(ROOT).free > resource['projectedBytes']+2*1024**3, 'Measured resource admission failed')
    test_result = io.read(a.tests/'completion.json'); io.check(test_result['status'] == 'Passed', 'Diagnostic guards must pass before execution')
    paths = [Path(__file__), Path(base.__file__), Path(io.__file__), a.plan, a.entry, a.protocol, reference_path,
             Path(__file__).with_name('test-tower-kodoku-diagnostic.py'), ROOT/'build/bounded_windows_process.py']
    paths += [p for p in a.tests.iterdir() if p.is_file()]
    pins = {str(p.resolve()): io.sha(p) for p in paths}
    output.mkdir(); shutil.copy2(a.protocol, output/'protocol.md'); shutil.copy2(__file__, output/'owner-source.py')
    io.write(output/'declaration.json', dict(plan=str(a.plan.resolve()), source=str(source), sourceManifestSha256=SOURCE_PIN,
        selected=plan['selected'], chosen=chosen, sourcePins=pins, runtimePins=entry['runtimePins'], liveCatalogPins=entry['liveCatalogPins'],
        ledgerPins=ledgers, initialExclusions=len(history), maximumReplays=96, maximumSeconds=840, maximumReplaySeconds=60,
        maximumLogBytes=16*1024**2, maximumBytes=2*1024**3, newSeeds=0, usedForAcceptance=False))
    io.write(output/'resource-admission.json', resource)
    process = base.module('kodoku_process', ROOT/'build/bounded_windows_process.py'); deadline = time.monotonic()+840
    attempts = 0; details = []; status = 'Failed'; labels = {c['cell']: c['label'] for c in chosen}
    print('Admitted 96 historical replays; 192 selected-recipe outcomes independently recounted.', flush=True)
    try:
        for item in plan['selected']:
            trial = item['trial']; log = output/(trial+'.log')
            command = [shutil.which('dotnet'), str(runtime/'BalanceHarness.dll'), 'tower-loadout-replay', '--run', str(archive), '--battle', trial, '--detailed']
            attempts += 1; io.write(output/(trial+'-attempt.json'), dict(attempt=attempts, command=command))
            receipt = process.run(command, ROOT, log, min(deadline, time.monotonic()+60), log_byte_limit=16*1024**2,
                observe=lambda observation: io.write(output/(trial+'-observation.json'), observation))
            io.write(output/(trial+'-process.json'), receipt)
            io.check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Replay failed; no retry')
            raw = log.read_text(encoding='utf-8-sig').lstrip(); replay, end = json.JSONDecoder().raw_decode(raw)
            io.check(raw[end:].strip() == 'Replay matched the saved input, combat result and Tower outcome.', 'Unexpected native replay output')
            details.append(dict(label=labels[item['cell']], **item, **analyze(saved[trial], replay, lookup[item['cell']]['scenario'])))
            if len(details)%6 == 0: print(f'Completed {len(details)}/96 complete-report matches.', flush=True)
            io.check(sum(p.stat().st_size for p in output.iterdir() if p.is_file()) <= 2*1024**3, 'Diagnostic byte limit exceeded')
        for group in (pins, entry['runtimePins'], entry['liveCatalogPins'], ledgers):
            for path, pin in group.items(): io.check(io.sha(path) == pin, 'Frozen input changed')
        io.check(io.history()[0] == history and io.sha(source/'files.json') == SOURCE_PIN, 'Source or seed union changed')
        io.write(output/'details.json', details)
        io.write(output/'result.json', dict(status='VerifiedDescriptiveDiagnostic', recountedSavedFights=192, repeatedFights=96,
            eventCount=sum(d['eventCount'] for d in details), newSeeds=0, usedForAcceptance=False, finalExclusions=len(history)))
        status = 'Complete'
    finally:
        io.write(output/'completion.json', dict(status=status, attemptedReplays=attempts, completedReplays=len(details), newSeeds=0, usedForAcceptance=False))
        io.write(output/'files.json', {p.name: io.sha(p) for p in sorted(output.iterdir()) if p.is_file()})


if __name__ == '__main__': main()
