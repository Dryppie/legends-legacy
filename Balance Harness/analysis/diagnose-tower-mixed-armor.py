"""Describe a pinned, closed mixed-armor screen with 64 exact historical replays.

Eight saved recipes, first eight declared seeds, no new seeds or acceptance test.
The complete 98-recipe source remains immutable. No game content is edited.
"""
import argparse
from collections import defaultdict
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
from statistics import mean, median
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('mixed_gear_diagnostic', Path(__file__).with_name('diagnose-tower-gear.py'))
base = importlib.util.module_from_spec(spec); spec.loader.exec_module(base)
io = base.io
SOURCES = {
    2: dict(source='0d0be643c70c46fae49e735c8d303571e3e59769892248e06d77e202e91bd3bc',
            proposal='e8f85582d948ec8ddd9930feae5ef124ab1d41963ef12eb3afeae66a5bb16f68', exclusions=913772),
    4: dict(source='66cc379b5f226c6925e7b1a240c0f65c8bef6d630bcafde7978ca036140d406d',
            proposal='4807d0908c90bf54bd5ebe2545c37c85b8869b319c103dcbcd577eb7dfd3f8a9', exclusions=916825),
}
ARCHIVED_CANDIDATE_PLANS = {
    4: 'd40e451702756f774bd8da5e3bd94a73c9774146f040191b771806413aa273e7',
}
MEASURES = ('incomingRawDamage', 'physicalMitigationPrevented', 'magicalMitigationPrevented',
            'barrierAbsorbed', 'finalHealthDamage')


def validate_source(floor, source_pin, proposal_pin, cells):
    io.check(floor in SOURCES, 'Unsupported diagnostic floor')
    expected = SOURCES[floor]
    io.check(source_pin == expected['source'] and proposal_pin == expected['proposal'], 'Pinned diagnostic source/proposal changed')
    io.check(len(cells) == 98 and all(c['scenario']['floorNumber'] == floor for c in cells), 'Diagnostic source family/floor differs')
    return expected


def load_candidate_plan(path, floor):
    io.check(floor in ARCHIVED_CANDIDATE_PLANS and io.sha(path) == ARCHIVED_CANDIDATE_PLANS[floor],
             'Unrecognized archived-candidate diagnostic plan')
    plan = io.read(path)
    io.check(plan['status'] == 'ProposedNotExecuted' and not plan['usedForAcceptance'] and
             plan['allocatedReplays'] == plan['newSeeds'] == 0, 'Diagnostic plan is not an unexecuted descriptive scope')
    return plan


def validate_candidate_pair(cells, original_cells, scope, original_scope):
    io.check(cells == original_cells, 'Archived candidate changed raw loadouts')
    io.check(scope['execution'] == original_scope['execution'] and scope['settings'] == original_scope['settings'],
             'Archived candidate changed runtime or combat settings')


def validate_live_catalog(live, content_hashes):
    actual = {p.relative_to(live).as_posix(): io.sha(p) for p in live.rglob('*.json')}
    io.check(actual == content_hashes, 'Current catalog differs from original live source')


def validate_archived_candidate(plan, source, family_path, floor, cells, live):
    """Bind both catalogs explicitly; candidate content never becomes the live reference."""
    original = Path(plan['originalLiveCatalogSource'])
    expected = validate_source(floor, plan['originalSourceManifestSha256'], plan['familyProposalSha256'], cells)
    io.check(source == Path(plan['source']).resolve() and io.sha(source/'files.json') == plan['sourceManifestSha256'] and
             io.sha(original/'files.json') == expected['source'] and io.sha(family_path) == expected['proposal'],
             'Archived candidate source/family binding changed')
    io.authenticate(original)
    original_scope = io.read(original/'scope.json')
    validate_candidate_pair(cells, io.read(original/'cells.json'), io.read(source/'scope.json'), original_scope)
    original_data = original/'content/Data'
    original_hashes = {p.relative_to(original_data).as_posix(): io.sha(p) for p in original_data.rglob('*.json')}
    io.check(all(original_hashes.get(name) == pin for name, pin in original_scope['contentHashes'].items()),
             'Original runtime catalog subset changed')
    io.check(all(io.sha(live/name) == pin for name, pin in original_hashes.items()),
             'Current catalog differs from archived live source')
    # Native archives contain the combat loader's catalog subset. The frozen
    # diagnostic plan separately captures every live JSON file, including extras.
    validate_live_catalog(live, {Path(path).relative_to(live).as_posix(): pin
                               for path, pin in plan['liveCatalogPins'].items()})
    candidate = Path(plan['candidate'])
    review = Path(plan['savedReview'])
    io.check(io.sha(candidate) == plan['candidateSha256'] and io.sha(review) == plan['savedReviewSha256'],
             'Archived candidate/review binding changed')
    io.ability_module().verify(original/'content', source/'content', io.read(candidate), floor)
    for group in ('liveCatalogPins', 'runtimePins', 'ledgerPins'):
        for path, pin in plan[group].items(): io.check(io.sha(path) == pin, 'Candidate diagnostic pin changed')
    return dict(source=plan['sourceManifestSha256'], proposal=expected['proposal'], exclusions=plan['initialExclusions'])


def validate_planned_selection(plan, selected, output):
    io.check(selected == plan['selected'], 'Frozen diagnostic recipe/seed selection changed')
    io.check(output == Path(plan['plannedOutput']).resolve(), 'Frozen diagnostic output changed')


def select_cells(cells, variants, rows):
    lookup = {c['id']: c for c in cells}; results = {r['id']: r for r in rows}
    parents = list(dict.fromkeys(v['sourceId'] for v in variants))
    io.check(len(parents) == 2 and len({io.composition_key(lookup[p]) for p in parents}) == 2,
             'Two actual parent compositions required')
    selected = []
    for label, parent in zip(('A', 'B'), parents):
        group = [v for v in variants if v['sourceId'] == parent]
        baseline_ids = {v['baselineId'] for v in group}
        io.check(len(baseline_ids) == 1, 'Ambiguous baseline')
        choices = [('baseline', next(iter(baseline_ids)), [])]
        for size in (2, 4):
            candidates = [v for v in group if len(v['armorPartySlots']) == size]
            best = min(candidates, key=lambda v: (-results[v['cell']['id']]['wins'],
                results[v['cell']['id']]['meanGuardianHealth'], v['cell']['id']))
            choices.append((str(size)+'-armored', best['cell']['id'], best['armorPartySlots']))
        choices.append(('full-armor', parent, list(range(1,6))))
        reference = lookup[choices[0][1]]['scenario']
        for profile, cell_id, slots in choices:
            cell = lookup[cell_id]; normalized = copy.deepcopy(cell['scenario'])
            for member, original in zip(normalized['party'], reference['party']):
                member['build']['equipment'] = copy.deepcopy(original['build']['equipment'])
            io.check(normalized == reference and not reference['seeds'], 'Matched recipes changed more than equipment')
            selected.append(dict(label=label+'/'+profile, composition=label, profile=profile, cellId=cell_id, armorPartySlots=slots))
    io.check(len({s['cellId'] for s in selected}) == 8, 'Eight distinct saved recipes required')
    return selected


def seed_panel(seeds):
    io.check(len(seeds) == len(set(seeds)) == 128, 'Complete 128-seed source panel required')
    return seeds[:8]


def party(report, scenario):
    prepared = [p for p in report['battle']['preparedParticipants'] if p['slot']['side'] == 'Friendly']
    stats = base.initial_stats(report, 5)
    by_name = {p['name']: p for p in prepared}
    io.check(len(by_name) == 5 and set(by_name) == {m['build']['id'] for m in scenario['party']}, 'Prepared party identities differ')
    by_id = {s['entityId']: s for s in stats}
    return [(m['partySlot'], by_name[m['build']['id']], by_id[by_name[m['build']['id']]['slot']['slotId']])
            for m in scenario['party']]


def window(events):
    groups = defaultdict(list)
    for event in events:
        if event['incomingRawDamage'] or event['finalHealthDamage']:
            groups[(event['source'], event['damageType'])].append(event)
    return dict(**{k: sum(e[k] for e in events) for k in MEASURES},
        healing=sum(e['magnitude'] for e in events if e['eventType'] in base.HEALING),
        regeneration=sum(e['magnitude'] for e in events if e['eventType'] == 'HealthRegeneration'),
        deaths=sum(e['eventType'] == 'Death' for e in events),
        damageBySource=[dict(source=k[0], damageType=k[1], events=len(es),
            **{field: sum(e[field] for e in es) for field in MEASURES}) for k,es in sorted(groups.items())])


def analyze(saved, replay, scenario):
    result = base.analyze(saved, replay, 5)  # Full report equality and recipient damage/healing/death reconciliation.
    actors = party(replay, scenario); events = replay['battle']['eventLog']; tps = replay['battle']['ticksPerSecond']
    ids = {s['entityId'] for _,_,s in actors}; friendly = [e for e in events if e['targetId'] in ids]
    for _,_,s in actors:
        for key in ('incomingRawDamage','physicalMitigationPrevented','magicalMitigationPrevented'):
            io.check(sum(e[key] for e in friendly if e['targetId'] == s['entityId']) == s[key], 'Recipient mitigation/raw totals differ')
    result['windows'] = {str(seconds): window([e for e in friendly if e['timestamp'] < seconds*tps]) for seconds in (15,20)}
    result['windows']['all'] = window(friendly)
    result['actors'] = []
    for slot,p,s in actors:
        es = [e for e in friendly if e['targetId'] == s['entityId']]
        first_death = next((index for index,e in enumerate(es) if e['eventType'] == 'Death'), None)
        prior = [] if first_death is None else es[:first_death]
        last_damage = next((e for e in reversed(prior) if e['finalHealthDamage'] > 0), None)
        result['actors'].append(dict(partySlot=slot, name=p['name'], attributes=p['combatAttributes'],
            firstDeathTick=s['firstDeathTick'], lastDamageBeforeFirstDeath=last_damage,
            precedingThreeSeconds=None if first_death is None else window([e for e in prior if e['timestamp'] >= s['firstDeathTick']-3*tps]),
            windows={str(seconds): window([e for e in es if e['timestamp'] < seconds*tps]) for seconds in (15,20)}))
    guardian_ids = {p['slot']['slotId'] for p in replay['battle']['preparedParticipants'] if p['slot']['side'] == 'Hostile'}
    io.check(len(guardian_ids) == 1, 'Expected one guardian')
    guardian_events = [e for e in events if e['actorId'] in guardian_ids or e['targetId'] in guardian_ids]
    result['guardianHealing'] = sum(e['magnitude'] for e in guardian_events if e['targetId'] in guardian_ids and e['eventType'] in base.HEALING)
    result['guardianAbilityUses'] = [dict(tick=e['timestamp'],source=e['source'],statsSource=e['statsSource'],target=e['targetId'])
        for e in guardian_events if e['actorId'] in guardian_ids and e['eventType'] == 'AbilityUse']
    result['guardianConditionEvents'] = [dict(tick=e['timestamp'],source=e['source'],statsSource=e['statsSource'],eventType=e['eventType'],details=e['details'])
        for e in guardian_events if e['targetId'] in guardian_ids and e['eventType'] in
            ('Buff','BuffExpired','StatusEffect','StatusEffectExpired','StatusEffectRemoved')]
    return result


def saved_summary(reports, scenario):
    metrics = [base.metrics(r,5) for r in reports]
    actors = [party(r,scenario) for r in reports]
    for rows in actors[1:]:
        io.check([(s,p['combatAttributes']) for s,p,_ in rows] == [(s,p['combatAttributes']) for s,p,_ in actors[0]], 'Prepared attributes vary by seed')
    return dict(base.summarize(metrics), samples=len(reports), eventTimelineReports=sum(bool(r['battle']['eventLog']) for r in reports),
        medianFirstDeathSeconds=median(m['firstDeathSeconds'] for m in metrics if m['firstDeathSeconds'] is not None),
        actors=[dict(partySlot=slot,name=p['name'],attributes=p['combatAttributes'],
            deaths=sum(rows[index][2]['firstDeathTick'] is not None for rows in actors),
            **{key:mean(rows[index][2][key] for rows in actors) for key in base.FIELDS}) for index,(slot,p,_) in enumerate(actors[0])])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('source','proposal','artifacts','output','protocol'): parser.add_argument('--'+name,type=Path,required=True)
    parser.add_argument('--floor', type=int, choices=tuple(SOURCES), default=2)
    parser.add_argument('--archived-candidate-plan', type=Path,
                        help='Explicit pinned descriptive replay plan for rejected isolated ability content')
    args = parser.parse_args()
    source,output = args.source.resolve(),args.output.resolve()
    io.check(output.parent == ROOT/'TestResults' and not output.exists(), 'Fresh direct TestResults output required')
    cells = io.read(source/'cells.json')
    io.authenticate(source)
    live = ROOT/'LL/src/API/API.LL/Data'
    candidate_plan = load_candidate_plan(args.archived_candidate_plan, args.floor) if args.archived_candidate_plan else None
    expected_source = (validate_archived_candidate(candidate_plan, source, args.proposal, args.floor, cells, live)
        if candidate_plan else validate_source(args.floor, io.sha(source/'files.json'), io.sha(args.proposal), cells))
    proposal = io.read(args.proposal); cells = io.read(source/'cells.json')
    io.check(cells == io.qualification_module().validate_mixed_armor_family(proposal, cells[:38]), 'Complete mixed family differs')
    rows = io.read(source/'result.json')['rows']; chosen = select_cells(cells,proposal['variants'],rows)
    seeds = io.read(source/'request.json')['seeds']; selected_seeds = seed_panel(seeds)
    archive = source/'evaluation'; scope = io.read(archive/'scope.json')
    runtime = args.artifacts.resolve()/'bin/EssenceSystem.Tests/release'
    runtime_pins = {str(runtime/(n+'.dll')):pin for n,pin in scope['execution']['assemblyHashes'].items()}
    for path,pin in runtime_pins.items(): io.check(io.sha(path) == pin, 'Original runtime changed')
    live_pins = {str(p):io.sha(p) for p in live.rglob('*.json')}
    if not candidate_plan:
        for name,pin in scope['contentHashes'].items(): io.check(io.sha(live/name) == pin, 'Current catalog differs from replay source')
    else:
        runtime_pins.update(candidate_plan['runtimePins'])
    exclusions,ledger_pins = io.history(); io.check(len(exclusions) == expected_source['exclusions'], 'Unexpected seed history')
    lookup = {c['id']: c for c in cells}; selected_ids = {s['cellId'] for s in chosen}; saved = {key:{} for key in selected_ids}; trials = {}
    for line in (archive/'trials.jsonl').read_text().splitlines():
        trial = json.loads(line)
        if trial['stage'] not in selected_ids: continue
        cell = lookup[trial['stage']]; recipe = io.read(archive/'recipes'/(trial['recipe']+'.json'))
        expected = copy.deepcopy(cell['scenario']); expected['seeds'] = seeds
        io.check(recipe == expected and trial['seed'] not in saved[cell['id']], 'Changed raw recipe or duplicate seed')
        report = json.loads(gzip.decompress((archive/'battles'/(trial['id']+'.json.gz')).read_bytes()))
        io.check(report['battle']['seed'] == trial['seed'], 'Saved trial seed differs')
        saved[cell['id']][trial['seed']] = report; trials[cell['id'],trial['seed']] = trial
    for key,reports in saved.items():
        io.check(set(reports) == set(seeds) and sum(r['succeeded'] for r in reports.values()) == next(r['wins'] for r in rows if r['id'] == key), 'Incomplete saved comparison')
    selected = [dict(**item,trial=trials[item['cellId'],seed]) for seed in selected_seeds for item in chosen]
    if candidate_plan: validate_planned_selection(candidate_plan, selected, output)
    source_pins = {str(p.resolve()):io.sha(p) for p in [Path(__file__),Path(base.__file__),args.protocol,args.proposal,
        Path(__file__).with_name('test-tower-mixed-armor-diagnostic.py'),ROOT/'build/bounded_windows_process.py']}
    if candidate_plan:
        for path in (args.archived_candidate_plan, Path(candidate_plan['candidate']), Path(candidate_plan['savedReview']),
                     Path(__file__).with_name('tower-ability-candidate.py')):
            source_pins[str(path.resolve())] = io.sha(path)
    output.mkdir(); shutil.copy2(args.protocol,output/'protocol.md'); shutil.copy2(__file__,output/'owner-source.py')
    io.write(output/'declaration.json',dict(source=str(source),sourceManifestSha256=expected_source['source'],floor=args.floor,selected=selected,
        sourcePins=source_pins,runtimePins=runtime_pins,liveCatalogPins=live_pins,ledgerPins=ledger_pins,
        maximumReplays=64,maximumSeconds=1200,maximumReplaySeconds=60,maximumLogBytes=64*1024**2,maximumBytes=2*1024**3,
        initialExclusions=len(exclusions),newSeeds=0,usedForAcceptance=False,
        archivedCandidatePlan=str(args.archived_candidate_plan.resolve()) if candidate_plan else None))
    io.write(output/'saved-summary.json',{item['label']:saved_summary(list(saved[item['cellId']].values()),lookup[item['cellId']]['scenario']) for item in chosen})
    proc = base.module('mixed_diagnostic_process', ROOT/'build/bounded_windows_process.py')
    deadline = time.monotonic()+1200; details=[]; attempts=0; status='Failed'
    try:
        for item in selected:
            trial = item['trial']; log = output/(trial['id']+'.log')
            command = [shutil.which('dotnet'),str(runtime/'BalanceHarness.dll'),'tower-loadout-replay','--run',str(archive),'--battle',trial['id'],'--detailed']
            attempts += 1; io.write(output/(trial['id']+'-attempt.json'),dict(attempt=attempts,command=command))
            receipt = proc.run(command,ROOT,log,min(deadline,time.monotonic()+60),log_byte_limit=64*1024**2,
                observe=lambda observation:io.write(output/(trial['id']+'-observation.json'),observation))
            io.write(output/(trial['id']+'-process.json'),receipt)
            io.check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Replay failed; no retry')
            raw = log.read_text(encoding='utf-8-sig').lstrip(); replay,end = json.JSONDecoder().raw_decode(raw)
            io.check(raw[end:].strip() == 'Replay matched the saved input, combat result and Tower outcome.', 'Unexpected replay result')
            detail = analyze(saved[item['cellId']][trial['seed']],replay,lookup[item['cellId']]['scenario'])
            details.append(dict(label=item['label'],cellId=item['cellId'],seed=trial['seed'],trial=trial['id'],**detail))
            print(json.dumps(dict(completed=len(details),planned=64,label=item['label'],seed=trial['seed'])),flush=True)
            io.check(sum(p.stat().st_size for p in output.iterdir() if p.is_file()) <= 2*1024**3,'Diagnostic byte cap exceeded')
        for pins in (source_pins,runtime_pins,live_pins,ledger_pins):
            for path,pin in pins.items(): io.check(io.sha(path) == pin,'Pinned content changed')
        final_exclusions,_ = io.history(); io.check(final_exclusions == exclusions,'Diagnostic allocated seeds')
        io.write(output/'details.json',details)
        io.write(output/'result.json',dict(status='VerifiedDescriptiveDiagnostic',recountedSavedFights=1024,repeatedFights=len(details),
            newSeeds=0,usedForAcceptance=False,finalExclusions=len(final_exclusions)))
        status='Complete'
    finally:
        io.write(output/'completion.json',dict(status=status,attemptedReplays=attempts,completedReplays=len(details),newSeeds=0,usedForAcceptance=False))
        io.write(output/'files.json',{p.name:io.sha(p) for p in sorted(output.iterdir()) if p.is_file()})


if __name__ == '__main__': main()
