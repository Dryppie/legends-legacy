"""Fresh linked-scaling follow-up after a verified competitive ceiling failure.

Keep the entire prior family, add all challenger shortlists/earlier breaches,
screen the prior strongest teams plus all original/challenger finalists, and
confirm the selected setting on a completely new schedule. Verification is a
separate read-only phase so completed partitions can be processed in parallel.
"""
import argparse
import copy
import importlib.util
from pathlib import Path
import shutil
import struct
import hashlib

spec = importlib.util.spec_from_file_location('calibration', Path(__file__).with_name('calibrate-tower-search-portfolio.py'))
cal = importlib.util.module_from_spec(spec); spec.loader.exec_module(cal)
spec2 = importlib.util.spec_from_file_location('expanded', Path(__file__).with_name('tower-calibration-assessment.py'))
expanded = importlib.util.module_from_spec(spec2); spec2.loader.exec_module(expanded)


def check(out):
    p = cal.check(out)
    if cal.sha(Path(__file__)) != p['refinementScriptSha256']: raise RuntimeError('Refinement script changed.')
    if cal.sha(Path(expanded.__file__)) != p['assessmentScriptSha256']: raise RuntimeError('Campaign assessor changed.')
    return p


def freeze(out, previous, challengers, seed):
    prior = cal.check(previous); old_selection = cal.read(previous / 'selection.json')
    assessment_file = previous / 'assessment-parallel.json'
    if not assessment_file.exists(): assessment_file = previous / 'assessment.json'
    assessment = cal.read(assessment_file)
    if assessment['assessment'] not in ('Fail', 'Inconclusive') or not any(c['upper'] > .5 for c in assessment['cells']):
        raise ValueError('This strengthening follow-up requires verified unresolved upper bounds or ceiling breaches.')
    if assessment['protocolSha256'] != cal.sha(previous / 'protocol.json') or assessment['selectionSha256'] != cal.sha(previous / 'selection.json'):
        raise ValueError('Previous assessment does not match its frozen experiment.')
    folder = 'parallel-assessments' if assessment_file.name == 'assessment-parallel.json' else 'assessments'
    verified = []
    for i, digest in assessment['partitionReportHashes'].items():
        if cal.sha(previous / folder / i / 'assessment.json') != digest: raise ValueError('Verified partition changed.')
        verified.append(cal.read(previous / folder / i / 'assessment.json'))
    prior_ids = [e['id'] for e in cal.read(previous / 'portfolio.json')]
    reconstructed = expanded.assess(prior_ids, verified, prior) if prior.get('campaignAssessmentPolicy') else cal.global_assessment(prior_ids, verified)
    if any(assessment.get(k) != v for k, v in reconstructed.items()):
        raise ValueError('Previous global arithmetic differs from its verified partitions.')
    completed = cal.read(challengers / 'completion.json')
    if {r['name'] for r in completed} != {'independent', 'retained'}: raise ValueError('Both challenger studies must complete.')
    if out.exists(): raise FileExistsError('Choose a new follow-up output.')
    entries = {}; mandatory = set(prior['mandatoryFineParties']); source_hashes = {}

    def add(scenario, source, finalist=False):
        s = copy.deepcopy(scenario); s['seeds'] = []; s['id'] = 'competitive-calibration-floor-' + str(prior['floor'])
        key = cal.recipe_key(s)
        if key not in entries:
            entries[key] = {'id': 'party-' + key[:24], 'scenario': s, 'sources': [], 'finalist': False}
        e = entries[key]; e['sources'].append(source); e['finalist'] |= finalist
        if finalist: mandatory.add(e['id'])
        return e

    for e in cal.read(previous / 'portfolio.json'):
        added = add(e['scenario'], {'priorCalibration': str(previous), 'id': e['id']}, e['finalist'])
        added['sources'] += e['sources']
    for done in completed:
        name = done['name']; run = challengers / 'studies' / name
        if cal.sha(run / 'study.json') != done['studySha256']: raise ValueError('Challenger report changed.')
        report = cal.read(run / 'study.json'); d = cal.read(run / 'definition.json')
        if report['status'] != 'Complete' or report['confirmation']['definition']['cohorts'] != [prior['cohort']]:
            raise ValueError('Challenger is incomplete or has a different cohort.')
        concerns = {b['partyId'] for b in report['conclusion']['earlierBreaches']}
        concerns.update(p['id'] for p in report['discovery']['discoveryShortlist'])
        choices = {p['party']['id']: p['party'] for a in report['discovery']['arms'] for p in a['proposals'] if p['party']}
        for pid in sorted(concerns):
            choice = choices[pid]
            scenario = {'schemaVersion': 1, 'id': d['id'], 'floorNumber': prior['floor'], 'startsAt': d['startsAt'],
                'preparationState': 'uncleared-no-contributions', 'assumptions': ['Exact completed challenger candidate'],
                'seeds': [], 'party': copy.deepcopy(d['contexts'][0]['characterTemplates'])}
            for m in scenario['party']:
                m['build']['essenceIds'] = choice['builds'][str(m['partySlot'])]
                m['build']['identityEssenceIds'] = [f'neutral-identity-slot-{i}' for i in range(1, d['budget']['essenceSlots'] + 1)]
            add(scenario, {'challenger': name, 'partyId': pid, 'role': 'shortlist-or-earlier-breach'})
        for cell in report['confirmation']['definition']['cells']:
            add(cell['scenario'], {'challenger': name, 'cell': cell['id'], 'role': cell['role']}, cell['role'] == 'generated')
        source_hashes[name] = done['studySha256']
    entries = sorted(entries.values(), key=lambda e: e['id']); ids = {e['id'] for e in entries}
    if len(entries) > expanded.MAXIMUM_FAMILY or len(mandatory) > 48 or not mandatory <= ids:
        raise ValueError('Freeze a separately bounded larger campaign; no party may be silently dropped.')
    fine = sorted(mandatory)
    for row in sorted(assessment['cells'], key=lambda c: (-c['wins'], c['id'])):
        if len(fine) >= 48: break
        if row['id'] not in fine: fine.append(row['id'])
    historical = set(cal.integers(cal.read(previous / 'seed-ledger.json'))) | set(cal.integers(cal.read(challengers / 'seed-ledger.json')))
    used = set(historical); schedules = {}
    for label, count in [('coarse', 16), ('fine', 96), ('confirmation', 250)]:
        values = []
        for i in range(100000):
            value = struct.unpack('<i', hashlib.sha256(f'competitive-calibration-refinement-v1/{seed}/{label}/{i}'.encode()).digest()[:4])[0]
            if value not in used: used.add(value); values.append(value)
            if len(values) == count: break
        if len(values) != count: raise RuntimeError('Fresh schedule allocation exhausted.')
        schedules[label] = values
    relative = [1, 1.05, 1.10, 1.15, 1.20, 1.30, 1.50]
    multipliers = [round(old_selection['multiplier']*m, 6) for m in relative]
    maximum = len(fine)*(len(multipliers)*16 + 11*96) + len(entries)*250 + 1000
    if maximum > expanded.MAXIMUM_COMBATS: raise ValueError('Follow-up exceeds explicit 750,000-combat campaign cap.')
    out.mkdir(parents=True)
    for name in ['executable', 'baseline-root']: shutil.copytree(previous / name, out / name)
    cal.save(out / 'portfolio.json', entries)
    cal.save(out / 'seed-ledger.json', {'historical': sorted(historical), 'schedules': schedules})
    cal.save(out / 'screening-plan.json', {'entries': fine, 'multipliers': multipliers, 'relativeMultipliers': relative})
    shutil.copy2(Path(__file__), out / 'producing-refinement-script.py')
    shutil.copy2(Path(cal.__file__), out / 'producing-script.py')
    shutil.copy2(Path(expanded.__file__), out / 'producing-assessment-script.py')
    files = {p.relative_to(out).as_posix(): cal.sha(p) for name in ['executable', 'baseline-root'] for p in (out / name).rglob('*') if p.is_file()}
    for name in ['portfolio.json', 'seed-ledger.json', 'screening-plan.json', 'producing-refinement-script.py', 'producing-script.py', 'producing-assessment-script.py']:
        files[name] = cal.sha(out / name)
    cal.save(out / 'protocol.json', {**prior, 'version': 'competitive-portfolio-refinement-v1', 'status': 'FrozenBeforeCombat',
        'scriptSha256': cal.sha(Path(cal.__file__)), 'refinementScriptSha256': cal.sha(Path(__file__)), 'frozenFiles': files,
        'assessmentScriptSha256': cal.sha(Path(expanded.__file__)), 'campaignAssessmentPolicy': expanded.VERSION,
        'source': str(previous), 'sourceAssessmentSha256': cal.sha(assessment_file), 'challengers': str(challengers),
        'challengerReportHashes': source_hashes, 'familySize': len(entries), 'mandatoryFineParties': sorted(mandatory),
        'coarseParties': fine, 'coarseMultipliers': multipliers, 'coarseSamples': 16, 'fineSamples': 96,
        'confirmationSamples': 250, 'partitionSize': 350, 'maximumActualBattles': maximum,
        'historicalLedgerHashes': {str(f): cal.sha(f) for f in [previous / 'seed-ledger.json', challengers / 'seed-ledger.json']},
        'previousPortfolioSha256': cal.sha(previous / 'portfolio.json'),
        'discovery': 'Before any new combat, freeze all original and challenger finalists plus the highest previous full-family confirmation results to 48 screening teams. Test seven linked factors from 1.00 through 1.50 relative to the failed setting on 16 paired seeds. Choose the first adjacent crossing of a 25% maximum; otherwise use neighbors around the nearest maximum, tie lower factor. Test eleven equally spaced points in that bracket on 96 fresh paired seeds, keeping the same screening teams. Choose maximum win rate nearest 25%, restricted to 18–32%, tie lower multiplier. No eligible factor ends without application.',
        'confirmation': 'Preserve the entire prior family and add every challenger shortlist, earlier breach and confirmation recipe. Every distinct recipe receives 250 completely new paired seeds at one factor frozen after screening. Do not pool, overwrite or reuse previous confirmation samples.',
        'caps': 'Each normal run and each C# verifier definition retain their existing limits. This new explicit campaign permits at most 3000 exact recipes and 750000 total combats; one global family correction uses every recipe. No previous campaign cap is altered retrospectively.',
        'application': 'Only a globally passing factor may change local Health and offense. Preserve live content hashes and verify twenty exact confirmation reports for the three published original primaries and up to ten strongest calibrated recipes, plus five before/after identities on each unaffected floor. Replay first outcome examples for the strongest calibrated team. These checks use the 1000-combat reserve; repeated seeds are parity, not fresh acceptance evidence.',
        'execution': 'Confirmation runs fights only; use verify-tower-calibration-partitions.py for overlapping read-only verification and the unchanged global-family decision.'})
    print(f'Follow-up frozen: {len(entries)} parties, {len(fine)} screening controls, maximum {maximum} combats', flush=True)


def discover(out):
    p = check(out); entries = cal.read(out / 'portfolio.json'); plan = cal.read(out / 'screening-plan.json')
    selected = [e for e in entries if e['id'] in plan['entries']]
    schedules = cal.read(out / 'seed-ledger.json')['schedules']; coarse = []
    for i, multiplier in enumerate(plan['multipliers']):
        label = f'coarse-{i:02}'
        coarse.append({'multiplier': multiplier, **cal.batch(out, label, cal.variant(out, label, multiplier), selected, schedules['coarse'])})
    cal.save(out / 'coarse.json', coarse)
    crossing = [(a,b) for a,b in zip(coarse,coarse[1:]) if (a['maximumRate']-.25)*(b['maximumRate']-.25) <= 0]
    if crossing: left,right = crossing[0]
    else:
        i = min(range(len(coarse)), key=lambda i:(abs(coarse[i]['maximumRate']-.25),i))
        left,right = coarse[max(0,i-1)],coarse[min(len(coarse)-1,i+1)]
    multipliers = sorted({round(left['multiplier']+(right['multiplier']-left['multiplier'])*i/10,6) for i in range(11)})
    cal.save(out / 'fine-plan.json', {'entries': plan['entries'], 'multipliers': multipliers, 'bracket':[left['multiplier'],right['multiplier']]})
    fine = []
    for i, multiplier in enumerate(multipliers):
        label = f'fine-{i:02}'
        fine.append({'multiplier': multiplier, **cal.batch(out, label, cal.variant(out, label, multiplier), selected, schedules['fine'])})
    cal.save(out / 'fine.json', fine)
    eligible = [r for r in fine if .18 <= r['maximumRate'] <= .32]
    if not eligible:
        cal.save(out / 'selection.json', {'status': 'NoEligibleSetting'}); return
    chosen = min(eligible, key=lambda r: (abs(r['maximumRate'] - .25), r['multiplier']))
    content = cal.variant(out, 'selected', chosen['multiplier']); definitions = []
    excluded = sorted(set(cal.read(out / 'seed-ledger.json')['historical']) | set(schedules['coarse']) | set(schedules['fine']))
    for offset in range(0, len(entries), 350):
        cells = []
        for e in entries[offset:offset + 350]:
            scenario = copy.deepcopy(e['scenario']); scenario['seeds'] = schedules['confirmation']
            cells.append({'id': e['id'], 'cohortId': p['cohort']['id'], 'role': 'generated' if e['finalist'] else 'reference', 'scenario': scenario, 'minimumSamples': 250})
        d = {'schemaVersion': 1, 'id': f'competitive-refined-partition-{len(definitions)}', 'intervalPolicy': 'bonferroni-wilson-95-v1',
            'contentHashes': {**p['contentHashes'], cal.FLOORS: cal.sha(content / 'Data' / cal.FLOORS)}, 'settingsHash': p['settingsHash'],
            'executionHash': p['executionHash'], 'cohorts': [p['cohort']], 'cells': cells, 'excludedCombatSeeds': excluded, 'maximumBattles': len(cells)*250}
        path = out / 'partitions' / (d['id'] + '.json'); cal.save(path, d)
        definitions.append({'file': path.relative_to(out).as_posix(), 'sha256': cal.sha(path), 'ids': [c['id'] for c in cells]})
    cal.save(out / 'selection.json', {'status': 'FrozenBeforeConfirmation', 'multiplier': chosen['multiplier'], 'discoveryMaximumRate': chosen['maximumRate'],
        'selectedContentSha256': cal.sha(content / 'Data' / cal.FLOORS), 'definitions': definitions})
    print(f'Frozen follow-up multiplier {chosen["multiplier"]}', flush=True)


def confirm(out):
    p = check(out); selection = cal.read(out / 'selection.json')
    if selection['status'] != 'FrozenBeforeConfirmation': raise RuntimeError('No selected setting.')
    if cal.sha(out / 'variants/selected/Data' / cal.FLOORS) != selection['selectedContentSha256']: raise RuntimeError('Selected content changed.')
    cal.save(out / 'empty-sources.json', [])
    for i, partition in enumerate(selection['definitions']):
        path = out / partition['file']
        if cal.sha(path) != partition['sha256']: raise RuntimeError('Confirmation definition changed.')
        cal.command(out, ['tower-balance-evaluate', '--definition', path, '--sources', out / 'empty-sources.json', '--output', out / 'preflight' / str(i)], f'preflight-{i}', (2,))
        r = cal.read(out / 'preflight' / str(i) / 'assessment.json')
        if r['issues'] or any(c['issues'] != ['Required confirmation evidence is missing.'] for c in r['cells']): raise RuntimeError('Invalid confirmation contract.')
    result = cal.batch(out, 'confirmation', out / 'variants/selected', cal.read(out / 'portfolio.json'), cal.read(out / 'seed-ledger.json')['schedules']['confirmation'])
    cal.save(out / 'confirmation-fights-complete.json', {'status': 'AllCombatComplete', 'combats': sum(r['valid'] for r in result['rows']),
        'observationsSha256': cal.sha(out / 'observations/confirmation.json'), 'note': 'Run the separate partition verifier for a global assessment. Combat completion is not balance acceptance.'})


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('phase', choices=['freeze', 'discover', 'confirm'])
    parser.add_argument('--out', type=Path, required=True); parser.add_argument('--previous', type=Path); parser.add_argument('--challengers', type=Path)
    parser.add_argument('--seed', type=int, default=971212); args = parser.parse_args(); out = args.out.resolve()
    if args.phase == 'freeze':
        if not args.previous or not args.challengers: parser.error('--previous and --challengers required')
        freeze(out, args.previous.resolve(), args.challengers.resolve(), args.seed)
    elif args.phase == 'discover': discover(out)
    else: confirm(out)
