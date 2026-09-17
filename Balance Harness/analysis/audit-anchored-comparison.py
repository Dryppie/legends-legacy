"""Independent saved-evidence arithmetic and freeze audit; never launches combat or changes the study."""
import hashlib
import json
import math
from pathlib import Path
import struct
import time

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / 'TestResults/anchored-comparison-20260917'
RUN = ROOT / 'TestResults/balance/tower-anchored-comparison-20260917'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check(condition, message):
    if not condition:
        raise ValueError(message)


def check_anchored_batch(study, definition, report):
    frozen = read(study / 'anchored-candidate-batch.json')
    arm = frozen['arms'][0]
    batch = arm['anchoredBatch']
    check(arm['stopReason'] == 'BatchFrozen' and arm['evaluations'] == [], 'Batch was not frozen without outcomes')
    check(batch['primaryReferenceId'] == definition['primaryReferenceId'], 'Wrong primary reference')
    primary = next(s for s in definition['starts'] if s['referenceId'] == definition['primaryReferenceId'])
    order = batch['characterOrder']
    check(sorted(order) == list(range(1, 11)), 'Invalid character order')
    proposals = arm['proposals']
    check(len(proposals) == 46 and len({p['party']['id'] for p in proposals}) == 46, 'Invalid frozen width or duplicates')
    supplied = {s['party']['id'] for s in definition['starts']}
    check({p['party']['id'] for p in proposals[:2]} == supplied, 'Missing supplied recipe')
    families = {e['id']: e['family'].lower() for e in definition['allowedEssences']}
    check(definition['ownedCopies'] is None, 'This comparison declared unlimited acquisition')
    for slot, ids in primary['party']['builds'].items():
        options = {(removed, added) for removed in ids for added in families if added not in ids
                   and families[added] not in {families[e] for e in ids if e != removed}}
        for supplied_start in definition['starts']:
            other = supplied_start['party']['builds']
            changed = [s for s in other if other[s] != primary['party']['builds'][s]]
            removed, added = set(ids)-set(other[slot]), set(other[slot])-set(ids)
            if changed == [slot] and len(removed) == len(added) == 1:
                options.discard((next(iter(removed)), next(iter(added))))
        check(batch['legalOptionsPerSlot'][slot] == len(options), 'Wrong legal neighborhood count')
    for index, proposal in enumerate(proposals):
        check(proposal['result'] == 'proposed', 'Frozen batch contains evaluated outcomes')
        builds = proposal['party']['builds']
        digest = hashlib.sha256(json.dumps(builds, sort_keys=True, separators=(',', ':'), ensure_ascii=True).encode()).hexdigest()
        check(digest == proposal['party']['id'], 'Wrong recipe identity')
        check(all(ids == sorted(ids) and len(ids) == len({families[e] for e in ids}) == 5 for ids in builds.values()), 'Illegal canonical composition')
        if index < 2:
            continue
        edit = proposal['anchoredEdit']
        slot = str(edit['partySlot'])
        check(edit['partySlot'] == order[(index-2) % 10], 'Character quota/order differs')
        changed = [s for s in builds if builds[s] != primary['party']['builds'][s]]
        check(changed == [slot], 'Not one character edit')
        check(set(primary['party']['builds'][slot]) - set(builds[slot]) == {edit['removed']}
              and set(builds[slot]) - set(primary['party']['builds'][slot]) == {edit['added']}, 'Wrong edit descriptor')
        check(proposal['provenance']['referenceIds'] == [primary['referenceId']], 'Wrong primary ancestry')
        parent = next(p for p in proposals[:2] if p['party']['id'] == primary['party']['id'])
        check(proposal['provenance']['parentIds'] == [parent['provenance']['id']], 'Wrong fixed parent')
    final = report['discovery']['arms'][0]
    check(final['anchoredBatch'] == batch and final['proposals'] == [dict(p, result='evaluated') for p in proposals], 'Evaluated batch changed')
    check(len(final['evaluations']) == 46 and all(len(r['cells'][0]['clears']) == 8 for r in final['evaluations']), 'Wrong discovery measurements')


def main():
    started = time.monotonic()
    check(not (RUN / 'failure.json').exists(), 'Native comparison failed')
    process = read(PACKAGE / 'process.json')
    check(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Unsuccessful owned process')
    q, template = read(RUN / 'request.json'), read(RUN / 'template.json')
    check(q['version'] == 'tower-anchored-comparison-v1', 'Wrong comparison version')
    check(q == read(PACKAGE / 'request.json'), 'Request changed')
    check(sha(PACKAGE / 'template.json') == q['templateHash'] and template == read(PACKAGE / 'template.json'), 'Template changed')
    values, seen = [], set(template['excludedCombatSeeds'])
    for word in struct.unpack('<4096i', (RUN / 'entropy.bin').read_bytes()):
        if word not in seen:
            values.append(word)
            seen.add(word)
    allocation = read(RUN / 'allocation.json')
    check(values[:3123] == allocation['selected'] and sorted(values) == allocation['reserved'], 'Entropy classification changed')
    check(read(RUN / 'history-input.json') == dict(reservationState='Complete', reserved=sorted(values)), 'Incomplete reservation')
    ledger = read(RUN / 'seed-ledger.json')
    check(ledger['historical'] == template['excludedCombatSeeds'] and ledger['reserved'] == sorted(values), 'Ledger changed')
    definitions = read(RUN / 'definitions.json')
    reports = []
    for index, definition in enumerate(definitions):
        study = RUN / f'study-{index}'
        check(definition == read(study / 'definition.json'), 'Study definition differs')
        panel = values[index//2*1041:(index//2+1)*1041]
        stages = definition['stages']['schedules']['fixed-equipment']
        check(stages == dict(discovery=panel[1:9], selection=panel[9:41],
                              confirmation=panel[41:], diagnostics=[]), 'Wrong paired panel')
        check(definition['generation']['seeds'] == [panel[0]], 'Wrong construction root')
        check(definition['generation']['candidatesPerArm'] == 46, 'Wrong search width')
        check(definition['generation']['policyVersion'] == ('anchored-neighborhood-v1' if index % 2 else 'retained-composition-incumbents-v1'), 'Wrong method')
        check(definition.get('primaryReferenceId') == (template['primaryReferenceId'] if index % 2 else None), 'Wrong designation scope')
        check(definition['references'] == template['references'] and definition['starts'] == template['starts'], 'Different supplied knowledge')
        check(definition['contexts'] == template['contexts'] and definition['contentHashes'] == template['contentHashes']
              and definition['settingsHash'] == template['settingsHash'] and definition['executionHash'] == template['executionHash'], 'Different gameplay scope')
        check(read(study / 'cost.json')['total'] == 3496, 'Unequal ceiling')
        manifest = read(study / 'files.json')
        files = {str(p.relative_to(study)).replace('\\', '/') for p in study.rglob('*') if p.is_file()}
        check(files == set(manifest) | {'files.json'}, 'Unexpected archive membership')
        for name, digest in manifest.items():
            check(sha(study / name) == digest, 'Changed archive file: ' + name)
        report = read(study / 'study.json')
        check(report['accounting']['completed']['discovery'] == 368 and report['accounting']['completed']['selection'] == 128, 'Wrong fixed search cost')
        if index % 2:
            check_anchored_batch(study, definition, report)
        check(report['status'] == 'Complete' and report['accounting']['attempted'] == report['accounting']['completed'], 'Incomplete study')
        check(report['confirmation'] == read(study / 'confirmation-freeze.json'), 'Changed confirmation family')
        check(sum(report['accounting']['completed'].values()) == sum(1 for _ in (study / 'trials.jsonl').open()), 'Archive fight count differs')
        reports.append(report)
    freeze = read(RUN / 'all-outputs-frozen.json')
    check(freeze['freezes'] == [r['confirmation'] for r in reports], 'Global freeze differs')
    check(freeze['afterAttempts'] == sum(r['accounting']['completed']['discovery'] + r['accounting']['completed']['selection'] for r in reports), 'Confirmation began before all searches finished')
    events = [json.loads(line) for line in (RUN / 'attempts.jsonl').read_text().splitlines()]
    for i, event in enumerate(events):
        check(event == dict(kind='Started' if i % 2 == 0 else 'Completed', ordinal=i//2+1), 'Unmatched attempt or retry')
    total = sum(sum(r['accounting']['completed'].values()) for r in reports)
    check(len(events) == total*2 and total <= 20976, 'Total attempt cap mismatch')
    result = read(RUN / 'result.json')
    check(result['version'] == q['version'], 'Wrong result version')
    primary = []
    for r in reports:
        member = next(m for m in r['confirmation']['members'] if m['primary'])
        evidence = next(e for e in r['evidence'] if e['cellId'] == member['cellId'])
        check(evidence['status'] == 'Complete' and len(evidence['trials']) == 1000, 'Partial endpoint')
        primary.append((member['generatedIds'][0], evidence['trials']))
    differences = []
    for pair in range(3):
        a, b = primary[2*pair:2*pair+2]
        check([t['seed'] for t in a[1]] == [t['seed'] for t in b[1]], 'Unpaired endpoint')
        first = [t['outcome'] == 'Victory' for t in a[1]]
        second = [t['outcome'] == 'Victory' for t in b[1]]
        gains = sum(y and not x for x, y in zip(first, second))
        losses = sum(x and not y for x, y in zip(first, second))
        delta = (gains-losses)/1000
        expected = dict(restart=pair+1, baselineParty=a[0], anchoredParty=b[0], baselineWins=sum(first), anchoredWins=sum(second),
                        gains=gains, losses=losses, difference=delta,
                        baselineDiscoveryFights=reports[2*pair]['accounting']['completed']['discovery'],
                        anchoredDiscoveryFights=reports[2*pair+1]['accounting']['completed']['discovery'])
        check(result['pairs'][pair] == expected, 'Paired result differs')
        # Shared prepared recipes must have identical outcomes on the paired panel, controls included.
        by_cell = {e['cellId']: e['trials'] for e in reports[2*pair]['evidence']}
        for evidence in reports[2*pair+1]['evidence']:
            if evidence['cellId'] in by_cell:
                check(evidence['trials'] == by_cell[evidence['cellId']], 'Shared recipe is not reproducible')
        differences.append(delta)
    mean = sum(differences)/3
    collision = 3000*2999/(2*(2**32-len(template['excludedCombatSeeds'])-123))
    lower = max(-1, mean-math.sqrt(2*math.log(1/(.05-collision))/3000))
    check(abs(result['meanDifference']-mean) < 1e-12 and abs(result['lowerBound']-lower) < 1e-12, 'Bound differs')
    net_wins = sum(p['gains']-p['losses'] for p in result['pairs'])
    decision = 'SupportsAnchoredForFrozenOutputs' if net_wins >= 150 and lower > 0 and sum(d > 0 for d in differences) >= 2 else 'DoNotPromoteAnchored'
    check(result['decision'] == decision and result['completed'] == result['started'] == total, 'Decision/accounting differs')
    output = dict(status='Agrees', nativeResultSha256=sha(RUN / 'result.json'), fights=total, newAuditFights=0,
                  permanentExclusions=len(seen), reserved=len(values), selected=3123, unusedReserved=len(values)-3123,
                  meanDifference=mean, lowerBound=lower, decision=decision, seconds=time.monotonic()-started)
    with (PACKAGE / 'independent-audit.json').open('x', encoding='utf-8') as stream:
        json.dump(output, stream, indent=2)
    print(json.dumps(output))


if __name__ == '__main__':
    main()
