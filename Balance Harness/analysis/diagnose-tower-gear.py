"""Replay a frozen paired gear sample for explanation, never balance acceptance.

Uses the original BalanceHarness CLI/assemblies; does not build, allocate seeds,
modify source archives, or apply guardian values. Selection is the first N saved
seeds in declared order, independently of observed outcomes.
"""
import argparse
from collections import defaultdict
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
from statistics import mean
import sys
import time

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('gear_diagnostic_io', Path(__file__).with_name('run-tower-balance-pass.py'))
GEARS = ('resistance-and-health', 'health-and-regeneration')
HEALING = {'Heal', 'HealCrit', 'HealOverTime'}
FIELDS = ('incomingRawDamage', 'magicalMitigationPrevented', 'physicalMitigationPrevented',
          'finalHealthDamage', 'healingReceived', 'healthRegenerated', 'healthRegenerationPotential',
          'healthRegenerationOverhealed', 'damageDone', 'directHealthDamage')


def initial_stats(report, expected_count=10):
    b = report['battle']
    ids = {p['slot']['slotId'] for p in b['preparedParticipants'] if p['slot']['side'] == 'Friendly'}
    stats = [s for s in b['summary']['statistics'] if s['entityId'] in ids]
    io.check(len(stats) == len(ids) == expected_count, 'Expected all original friendly characters')
    return stats


def metrics(report, expected_count=10):
    b = report['battle']; stats = initial_stats(report, expected_count)
    deaths = [s['firstDeathTick'] for s in stats if s['firstDeathTick'] is not None]
    return dict(won=report['succeeded'], seconds=b['summary']['durationSeconds'],
                firstDeathSeconds=min(deaths)/b['ticksPerSecond'] if deaths else None,
                guardianHealth=report['guardianHealthRemainingPercent'],
                **{key: sum(s[key] for s in stats) for key in FIELDS})


def analyze(saved, replay, expected_count=10):
    stripped = copy.deepcopy(replay)
    stripped['battle']['eventLog'] = None
    io.check(stripped == saved, 'Detailed replay changed the complete saved report')
    events = replay['battle']['eventLog']
    io.check(bool(events), 'Missing detailed events')
    stats = initial_stats(replay, expected_count); ids = {s['entityId'] for s in stats}
    friendly = [e for e in events if e['targetId'] in ids]
    for s in stats:
        es = [e for e in friendly if e['targetId'] == s['entityId']]
        deaths = [e['timestamp'] for e in es if e['eventType'] == 'Death']
        io.check(sum(e['magnitude'] for e in es if e['eventType'] in HEALING) == s['healingReceived']
                 and sum(e['magnitude'] for e in es if e['eventType'] == 'HealthRegeneration') == s['healthRegenerated']
                 and sum(e['finalHealthDamage'] for e in es) == s['finalHealthDamage']
                 and (min(deaths) if deaths else None) == s['firstDeathTick'],
                 'Event totals differ from original recipient statistics')
    tps = replay['battle']['ticksPerSecond']
    def window(es):
        damage = defaultdict(int)
        for e in es:
            if e['finalHealthDamage']:
                damage[(e['source'], e['damageType'])] += e['finalHealthDamage']
        return dict(finalHealthDamage=sum(e['finalHealthDamage'] for e in es),
                    restoredHealth=sum(e['magnitude'] for e in es if e['eventType'] in HEALING),
                    regeneratedHealth=sum(e['magnitude'] for e in es if e['eventType'] == 'HealthRegeneration'),
                    deaths=sum(e['eventType'] == 'Death' for e in es),
                    damageBySource=[dict(source=k[0], damageType=k[1], finalHealthDamage=v)
                                    for k, v in sorted(damage.items(), key=lambda x: (-x[1], str(x[0])))])
    return dict(**metrics(replay, expected_count), eventCount=len(events),
                first40Seconds=window([e for e in friendly if e['timestamp'] < 40*tps]),
                allEvents=window(friendly),
                firstDeaths=[dict(name=s['entityName'], tick=s['firstDeathTick']) for s in stats])


def summarize(rows):
    return {key: mean(r[key] for r in rows if r[key] is not None)
            for key in ('seconds', 'firstDeathSeconds', 'guardianHealth', *FIELDS)} | {'wins': sum(r['won'] for r in rows)}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('source', 'source-pin', 'artifacts', 'composition', 'output', 'protocol'):
        p.add_argument('--'+name, required=True)
    p.add_argument('--pairs', type=int, default=16)
    p.add_argument('--log-mib', type=int, default=8)
    p.add_argument('--total-mib', type=int, default=256)
    a = p.parse_args()
    io.check(1 <= a.pairs <= 16, 'Diagnostic maximum is sixteen pairs')
    io.check(8 <= a.log_mib <= 64 and 256 <= a.total_mib <= 2048, 'Invalid declared output allowance')
    source, output = Path(a.source).resolve(), Path(a.output).resolve()
    io.check(output.parent == ROOT/'TestResults' and not output.exists(), 'Fresh direct TestResults output required')
    io.check(io.sha(source/'files.json') == a.source_pin, 'Source manifest pin mismatch')
    io.authenticate(source)
    archive = source/'evaluation'
    scope = io.read(archive/'scope.json')
    runtime = Path(a.artifacts).resolve()/'bin/EssenceSystem.Tests/release'
    runtime_pins = {str(runtime/(name+'.dll')): pin for name, pin in scope['execution']['assemblyHashes'].items()}
    for path, pin in runtime_pins.items(): io.check(io.sha(path) == pin, 'Original assembly mismatch: '+path)
    cells = io.read(source/'cells.json')
    io.check(len(cells) == 103 and len({io.composition_key(c) for c in cells}) == 14, 'Changed complete source family')
    chosen = [c for c in cells if c['composition'] == a.composition and c['gear'] in GEARS]
    io.check(len(chosen) == 2 and {c['gear'] for c in chosen} == set(GEARS), 'Exactly two gear variants required')
    io.check(io.composition_key(chosen[0]) == io.composition_key(chosen[1]), 'Gear comparison changed composition')
    by_id = {c['id']: c for c in chosen}
    seeds = io.read(source/'request.json')['seeds']
    trials = [json.loads(line) for line in (archive/'trials.jsonl').read_text().splitlines()]
    saved = {g: {} for g in GEARS}; selected = []
    for t in trials:
        if t['stage'] not in by_id: continue
        c = by_id[t['stage']]; recipe = io.read(archive/'recipes'/(t['recipe']+'.json'))
        expected = copy.deepcopy(c['scenario']); expected['seeds'] = seeds
        io.check(recipe == expected, 'Changed raw recipe')
        b = json.loads(gzip.decompress((archive/'battles'/(t['id']+'.json.gz')).read_bytes()))
        io.check(t['seed'] not in saved[c['gear']] and b['battle']['seed'] == t['seed'], 'Invalid pairing')
        saved[c['gear']][t['seed']] = b
        if t['seed'] in seeds[:a.pairs]: selected.append(dict(trial=t, gear=c['gear']))
    io.check(all(set(rows) == set(seeds) for rows in saved.values()) and len(selected) == a.pairs*2, 'Incomplete paired family')
    selected.sort(key=lambda x: (seeds.index(x['trial']['seed']), GEARS.index(x['gear'])))
    output.mkdir(); (output/'protocol.md').write_bytes(Path(a.protocol).read_bytes())
    (output/'owner-source.py').write_bytes(Path(__file__).read_bytes())
    live = ROOT/'LL/src/API/API.LL/Data/world-tower/tower-floors.json'; live_pin = io.sha(live)
    io.write(output/'declaration.json', dict(source=str(source), sourceManifestSha256=a.source_pin,
        driverSha256=io.sha(Path(__file__)), protocolSha256=io.sha(Path(a.protocol)), selected=selected,
        runtimePins=runtime_pins, maximumRepeatedFights=32, newSeeds=0, maximumSeconds=660,
        maximumLogBytes=a.log_mib*1024**2, maximumBytes=a.total_mib*1024**2,
        liveTowerSha256=live_pin, usedForAcceptance=False))
    io.write(output/'saved-summary.json', {g: summarize([metrics(b) for b in rows.values()]) for g, rows in saved.items()})
    proc = module('gear_diagnostic_process', ROOT/'build/bounded_windows_process.py')
    deadline = time.monotonic()+660; details=[]; status='Failed'; attempts=0
    try:
        for item in selected:
            t=item['trial']; log=output/(t['id']+'.log')
            command=[shutil.which('dotnet'), str(runtime/'BalanceHarness.dll'), 'tower-loadout-replay',
                     '--run', str(archive), '--battle', t['id'], '--detailed']
            attempts += 1
            io.write(output/(t['id']+'-attempt.json'), dict(attempt=attempts, command=command))
            receipt=proc.run(command, ROOT, log, min(deadline, time.monotonic()+60),
                log_byte_limit=a.log_mib*1024**2,
                observe=lambda observation: io.write(output/(t['id']+'-observation.json'), observation))
            io.write(output/(t['id']+'-process.json'),receipt)
            io.check(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0, 'Replay failed; no retry')
            raw=log.read_text(encoding='utf-8-sig').lstrip()
            replay,end=json.JSONDecoder().raw_decode(raw)
            io.check(raw[end:].strip()=='Replay matched the saved input, combat result and Tower outcome.', 'Unexpected replay output')
            result=analyze(saved[item['gear']][t['seed']], replay)
            details.append(dict(trial=t['id'], seed=t['seed'], gear=item['gear'], **result))
            print(json.dumps(dict(completed=len(details),planned=len(selected),gear=item['gear'],seed=t['seed'],firstDeath=result['firstDeathSeconds'])),flush=True)
            io.check(sum(x.stat().st_size for x in output.iterdir() if x.is_file()) <= a.total_mib*1024**2, 'Diagnostic byte cap exceeded')
        for path,pin in runtime_pins.items(): io.check(io.sha(path)==pin, 'Runtime changed during diagnostic')
        io.check(io.sha(live)==live_pin and io.sha(source/'files.json')==a.source_pin,'Content/source changed')
        io.write(output/'details.json',details)
        io.write(output/'result.json',dict(status='VerifiedDescriptiveDiagnostic',savedPairedSeeds=len(seeds),
            repeatedFights=len(details),newSeeds=0,usedForAcceptance=False,
            profiles={g:summarize([d for d in details if d['gear']==g]) for g in GEARS}))
        status='Complete'
    finally:
        io.write(output/'completion.json',dict(status=status,attemptedReplays=attempts,completedReplays=len(details),newSeeds=0,usedForAcceptance=False))
        io.write(output/'files.json',{x.relative_to(output).as_posix():io.sha(x) for x in sorted(output.iterdir()) if x.is_file()})


if __name__ == '__main__': main()
