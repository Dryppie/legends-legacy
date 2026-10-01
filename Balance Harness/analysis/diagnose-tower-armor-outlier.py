"""Paired descriptive summaries for archived Tower recipes; no acceptance or allocation."""
from collections import defaultdict
import importlib.util
from pathlib import Path
from statistics import mean, median

spec = importlib.util.spec_from_file_location('outlier_mixed', Path(__file__).with_name('diagnose-tower-mixed-armor.py'))
mixed = importlib.util.module_from_spec(spec); spec.loader.exec_module(mixed)
io = mixed.io
WINDOWS = (('opening', 0, 21), ('middle', 21, 42), ('late', 42, float('inf')), ('all', 0, float('inf')))
STAT_FIELDS = (*mixed.base.FIELDS, 'periodicHealthDamage', 'healingDone', 'threatGenerated', 'targetedAttacks',
               'statusEffectsApplied', 'actionDeniedTicks')


def paired(left, right):
    """Keep seed pairing: swapping a recipe can both gain and lose individual fights."""
    io.check(set(left) == set(right) and len(left) == 512, 'Complete shared 512-seed panel required')
    both = sum(left[s]['won'] and right[s]['won'] for s in left)
    gained = sum(left[s]['won'] and not right[s]['won'] for s in left)
    lost = sum(not left[s]['won'] and right[s]['won'] for s in left)
    return dict(samples=512, leftWins=sum(r['won'] for r in left.values()), rightWins=sum(r['won'] for r in right.values()),
                bothWin=both, leftOnly=gained, rightOnly=lost, neither=512-both-gained-lost,
                netWinDifference=gained-lost, meanDurationDifference=mean(left[s]['seconds']-right[s]['seconds'] for s in left))


def saved_metrics(report, scenario):
    actors = mixed.party(report, scenario)
    hostile = [p for p in report['battle']['preparedParticipants'] if p['slot']['side'] == 'Hostile']
    io.check(len(hostile) == 1, 'Exactly one guardian required')
    guardian = next(s for s in report['battle']['summary']['statistics'] if s['entityId'] == hostile[0]['slot']['slotId'])
    result = mixed.base.metrics(report, 5)
    result['guardian'] = {k: guardian[k] for k in (*STAT_FIELDS, 'maxHealth', 'health')}
    result['actors'] = [dict(slot=slot, name=p['name'], attributes=p['combatAttributes'],
        firstDeathSeconds=s['firstDeathTick']/report['battle']['ticksPerSecond'] if s['firstDeathTick'] is not None else None,
        **{k:s[k] for k in STAT_FIELDS}, abilities=s['abilities']) for slot,p,s in actors]
    return result


def summarize_saved(rows):
    io.check(len(rows) == 512, 'Full saved panel required')
    baseline = [(a['slot'], a['name'], a['attributes']) for a in rows[0]['actors']]
    io.check(all([(a['slot'], a['name'], a['attributes']) for a in r['actors']] == baseline for r in rows),
             'Prepared actors changed across seeds')
    actors = []
    for i, (slot, name, attributes) in enumerate(baseline):
        stats = [r['actors'][i] for r in rows]
        deaths = [s['firstDeathSeconds'] for s in stats if s['firstDeathSeconds'] is not None]
        names = sorted({ability['name'] for s in stats for ability in s['abilities']})
        actors.append(dict(slot=slot, name=name, attributes=attributes, deaths=len(deaths),
            medianFirstDeathAmongDeaths=median(deaths) if deaths else None,
            deathsBefore21=sum(s is not None and s < 21 for s in (a['firstDeathSeconds'] for a in stats)),
            deathsBefore42=sum(s is not None and s < 42 for s in (a['firstDeathSeconds'] for a in stats)),
            **{k:mean(s[k] for s in stats) for k in STAT_FIELDS},
            abilities={n:{k:mean(sum(a[k] for a in s['abilities'] if a['name']==n) for s in stats)
                          for k in ('totalDamage','totalHealing','uses')} for n in names}))
    return dict(samples=512, **mixed.base.summarize(rows),
        noOriginalPartyDeath=sum(r['firstDeathSeconds'] is None for r in rows),
        actors=actors, guardian={k:mean(r['guardian'][k] for r in rows) for k in rows[0]['guardian']})


def damage_sources(events):
    sums = defaultdict(int)
    for e in events:
        if e['finalHealthDamage']:
            sums[e['source']] += e['finalHealthDamage']
    return dict(sorted(sums.items()))


def timeline(replay, scenario):
    """Recipient health damage, original-actor output and fixed windows; no inferred stacks."""
    actors = mixed.party(replay, scenario)
    ids = {p['slot']['slotId']:slot for slot,p,_ in actors}
    guardian_ids = {p['slot']['slotId'] for p in replay['battle']['preparedParticipants'] if p['slot']['side']=='Hostile'}
    io.check(len(guardian_ids)==1, 'Exactly one guardian required')
    events = replay['battle']['eventLog']; io.check(bool(events), 'Detailed events required')
    tps = replay['battle']['ticksPerSecond']; seconds = replay['battle']['summary']['durationSeconds']
    incoming = [e for e in events if e['targetId'] in ids]
    outgoing = [e for e in events if e['targetId'] in guardian_ids]
    guardian = next(s for s in replay['battle']['summary']['statistics'] if s['entityId'] in guardian_ids)
    io.check(sum(e['finalHealthDamage'] for e in outgoing)==guardian['finalHealthDamage'] and
             sum(e['magnitude'] for e in outgoing if e['eventType'] in mixed.base.HEALING)==guardian['healingReceived'] and
             sum(e['magnitude'] for e in outgoing if e['eventType']=='HealthRegeneration')==guardian['healthRegenerated'],
             'Guardian recipient totals differ')
    windows = {}
    for name, start, end in WINDOWS:
        inside = lambda es: [e for e in es if start*tps <= e['timestamp'] < end*tps]
        inc, out = inside(incoming), inside(outgoing)
        windows[name] = dict(exposureSeconds=max(0,min(seconds,end)-min(seconds,start)),
            partyHealthDamage=sum(e['finalHealthDamage'] for e in inc),
            partyHealing=sum(e['magnitude'] for e in inc if e['eventType'] in mixed.base.HEALING),
            partyRegeneration=sum(e['magnitude'] for e in inc if e['eventType']=='HealthRegeneration'),
            originalPartyDeaths=sum(e['eventType']=='Death' for e in inc),
            incomingBySource=damage_sources(inc), guardianHealthDamage=sum(e['finalHealthDamage'] for e in out),
            guardianHealing=sum(e['magnitude'] for e in out if e['eventType'] in mixed.base.HEALING),
            guardianRegeneration=sum(e['magnitude'] for e in out if e['eventType']=='HealthRegeneration'),
            outgoingBySource=damage_sources(out),
            outgoingByOriginalSlot={str(slot):sum(e['finalHealthDamage'] for e in out if e['actorId']==actor) for actor,slot in ids.items()},
            otherActorDamage=sum(e['finalHealthDamage'] for e in out if e['actorId'] not in ids))
    first_dive = next((e for e in incoming if e['source']=='effect.creature.velka.rending_dive.damage' and e['incomingRawDamage']>0),None)
    return dict(windows=windows, firstDiveTargetSlot=ids[first_dive['targetId']] if first_dive else None,
                exactFeastConsumedStacks=None)


def summarize_timelines(rows):
    io.check(len(rows)==32, 'Fixed 32-seed detailed panel required')
    windows={}
    for name, _, _ in WINDOWS:
        values=[r['timeline']['windows'][name] for r in rows]
        windows[name]={k:mean(v[k] for v in values) for k in values[0] if not isinstance(values[0][k],dict)}
        for key in ('incomingBySource','outgoingBySource','outgoingByOriginalSlot'):
            sources=sorted({s for v in values for s in v[key]})
            windows[name][key]={s:mean(v[key].get(s,0) for v in values) for s in sources}
    return dict(samples=len(rows), wins=sum(r['won'] for r in rows), windows=windows,
        firstDiveTargets={str(slot):sum(r['timeline']['firstDiveTargetSlot']==slot for r in rows) for slot in range(1,6)},
        exactFeastConsumedStacks=None)
