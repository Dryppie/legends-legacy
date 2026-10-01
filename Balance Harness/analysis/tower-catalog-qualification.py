"""Explicit admission of a historical Tower family after accepted-catalog replay parity.

Qualification never changes a historical archive or relaxes ordinary source checks.
Only catalogs already bound to a passing, applied confirmation can be admitted.
"""
import copy
import hashlib
import importlib.util
import itertools
import json
from pathlib import Path


def owner_module():
    spec = importlib.util.spec_from_file_location('qualification_owner', Path(__file__).with_name('run-tower-balance-pass.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def validate_transition(before, after, floor):
    check = owner_module().check
    check({k: v for k, v in before.items() if k != 'floors'} ==
          {k: v for k, v in after.items() if k != 'floors'}, 'Tower metadata changed')
    def target(value):
        rows = [f for f in value['floors'] if f['floorNumber'] == floor]
        check(len(rows) == 1, 'Missing or ambiguous target floor')
        return rows[0]
    check(target(before) == target(after), 'Target floor changed')


def accepted_single_catalog(plan):
    io = owner_module()
    for path, pin in plan['receiptPins'].items():
        io.check(io.sha(path) == pin, 'Accepted application receipt changed')
    accepted = Path(plan['acceptedApplication'])
    required = {str(accepted/name) for name in ('request.json', 'result.json', 'process.json', 'completion.json')}
    io.check(required == set(plan['receiptPins']), 'Incomplete accepted application pins')
    request, result, completion, process = [io.read(accepted/name) for name in
        ('request.json', 'result.json', 'completion.json', 'process.json')]
    confirmed = Path(request['source'])
    io.check(io.sha(confirmed/'files.json') == request['manifestPin'] == result['manifestPin'] and
             io.sha(request['audit']) == request['auditPin'], 'Accepted confirmation changed')
    io.authenticate(confirmed)
    audit = io.read(request['audit'])
    io.check(audit['status'] == 'Verified' and audit['assessment']['verdict'] == 'Pass' and
             audit['resultSha256'] == io.sha(confirmed/'result.json'), 'Passing accepted confirmation required')
    io.check(completion['status'] == 'Verified' and result['status'] == 'AppliedInputsAndReplaysVerified' and
             completion['resultSha256'] == io.sha(accepted/'result.json') and
             result['matchedInputs'] == completion['matchedInputs'] == audit['evaluationFights'] and
             result['fullReplays'] == completion['fullReplays'] == audit['assessment']['familySize'] and
             completion['newSeeds'] == result['newSeeds'] == 0 and
             process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
             'Accepted application parity incomplete')
    return confirmed, completion


def validate_plan(plan, source, api, tests):
    io = owner_module()
    io.check(plan['version'] == 'tower-catalog-qualification-v1', 'Unknown qualification version')
    io.check(Path(plan['source']).resolve() == source.resolve() and
             io.sha(source/'files.json') == plan['sourceManifestSha256'], 'Qualification source changed')
    io.authenticate(source)
    # This extends only Python provenance. The native v1 input/replay contract is unchanged.
    if 'acceptedAggregate' in plan:
        io.check('acceptedApplication' not in plan, 'Ambiguous accepted catalog provenance')
        spec = importlib.util.spec_from_file_location('qualification_aggregate', Path(__file__).with_name('tower-balance-aggregate.py'))
        aggregate = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(aggregate)
        confirmed, completion = aggregate.applied_catalog(plan)
    else:
        confirmed, completion = accepted_single_catalog(plan)
    scope = io.read(source/'scope.json')
    accepted_scope = io.read(confirmed/'scope.json')
    io.check(scope['settings'] == accepted_scope['settings'], 'Accepted settings differ')
    io.check(scope['contentHashes'] == plan['sourceContentHashes'] and
             set(plan['currentContentHashes']) == set(scope['contentHashes']) == set(accepted_scope['contentHashes']),
             'Qualification catalog set changed')
    for name, pin in plan['currentContentHashes'].items():
        if pin != accepted_scope['contentHashes'][name]:
            # Application deliberately preserves Tower formatting. Its receipt must bind
            # the actual bytes, and every floor and metadata field must still agree.
            io.check(name == 'world-tower/tower-floors.json' and
                     completion.get('floorFileSha256') == pin and
                     io.read(api/'Data'/name) == io.read(confirmed/'content/Data'/name),
                     'Current content is not the applied catalog')
        io.check(io.sha(api/'Data'/name) == pin, 'Current catalog changed: ' + name)
        if name == 'combat/summons.json' and scope['contentHashes'][name] != pin:
            validate_summons_transition(plan, source, confirmed, api)
        elif name not in ('combat/abilities.json', 'world-tower/tower-floors.json'):
            io.check(scope['contentHashes'][name] == pin, 'Unrelated historical catalog changed: ' + name)
    validate_transition(io.read(source/'content/Data/world-tower/tower-floors.json'),
                        io.read(api/'Data/world-tower/tower-floors.json'), plan['floor'])
    io.check(all(c['scenario']['floorNumber'] == plan['floor'] for c in io.read(source/'cells.json')),
             'Source family floor differs')
    io.check(set(plan['assemblyHashes']) == set(scope['execution']['assemblyHashes']), 'Runtime assembly set changed')
    for name, pin in plan['assemblyHashes'].items():
        io.check(io.sha(tests/(name+'.dll')) == pin, 'Qualification runtime changed: ' + name)
    io.check(io.sha(tests/'EssenceSystem.Tests.dll') == plan['testAssemblySha256'], 'Qualification test assembly changed')


def validate_summons_transition(plan, source, confirmed, api):
    """Only the fully accepted exclusive floor-eight inheritance may cross catalogs."""
    io = owner_module()
    accepted = plan.get('acceptedAggregate', {})
    io.check(accepted.get('version') in ('applied-tower-shared-penetration-aggregate-v1', 'applied-tower-kodoku-fixed-aggregate-v1', 'applied-tower-kodoku-midpoint-aggregate-v1'), 'Unrelated historical summons catalog changed')
    d = io.read(accepted['declaration'])
    original = Path(d['source'])/'content'
    fixed = accepted['version'] == 'applied-tower-kodoku-fixed-aggregate-v1'
    midpoint = accepted['version'] == 'applied-tower-kodoku-midpoint-aggregate-v1'
    helper = io.ability_module().kodoku_midpoint_module() if midpoint else io.ability_module().kodoku_refinement_module() if fixed else io.ability_module().shared_penetration_module()
    helper.verify(original, confirmed/'content', d['candidatePlan'], 8)
    summons = helper.base.SUMMONS if fixed or midpoint else helper.SUMMONS
    io.check(io.sha(source/'content'/summons) == io.sha(original/summons) and
             io.sha(api/summons) == io.sha(confirmed/'content'/summons), 'Unconfirmed summons transition')


def signature(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':')).encode()).hexdigest()


def validate_partial_family(proposal, original):
    """Reconstruct every declared subset, retaining raw identities and original order."""
    io = owner_module()
    io.check(proposal['version'] == 'floor6-partial-restoration-proposal-v1' and proposal['floor'] == 6 and
             proposal['status'] == 'ProposedNotAdmitted' and proposal['newSeeds'] == 0,
             'Expected seed-free partial Restoration proposal')
    io.check(len(original) == proposal['retainedCells'] == 38 and proposal['newVariants'] == 60 and
             proposal['totalCells'] == 98 and proposal['cells'][:38] == original,
             'Original family must be retained exactly')
    lookup = {c['id']: c for c in original}
    variants = proposal['variants']
    parents = sorted({v['sourceId'] for v in variants})
    io.check(len(parents) == 2 and len({io.composition_key(lookup[p]) for p in parents}) == 2,
             'Two actual parent compositions required')
    slots = ['MainHand', 'Chest', 'Head', 'Ring', 'Necklace', 'Relic']
    expected_subsets = {tuple(s) for n in (2, 4) for s in itertools.combinations(slots, n)}
    for parent in parents:
        full = lookup[parent]
        io.check(full['gear'] == 'restorer-specialization', 'Full Restorer parent required')
        baseline = [c for c in original if c['composition'] == full['composition'] and c['gear'] == 'baseline']
        io.check(len(baseline) == 1, 'Unambiguous baseline required')
        def healer(cell):
            return next(m for m in cell['scenario']['party'] if m['partySlot'] == 2)['build']
        base_items = {i['slot']: i for i in healer(baseline[0])['equipment']}
        normalized = copy.deepcopy(full)
        healer(normalized)['equipment'] = copy.deepcopy(healer(baseline[0])['equipment'])
        io.check(normalized['scenario'] == baseline[0]['scenario'] and
                 [i['slot'] for i in healer(full)['equipment'] if i != base_items[i['slot']]] == slots,
                 'Only the six healer equipment slots may differ')
        group = [v for v in variants if v['sourceId'] == parent]
        io.check(len(group) == 30 and {tuple(v['restorationSlots']) for v in group} == expected_subsets,
                 'Every two-piece and four-piece subset is required exactly once')
        for variant in group:
            chosen = variant['restorationSlots']
            cell = copy.deepcopy(full)
            healer(cell)['equipment'] = [copy.deepcopy(i if i['slot'] in chosen else base_items[i['slot']])
                                          for i in healer(full)['equipment']]
            digest = signature(cell['scenario'])
            cell.update(id='partial-restoration/'+digest,
                        gear='partial-restoration-'+str(len(chosen))+'-'+'-'.join(chosen).lower(),
                        origin='proposed-partial-healer-gear')
            io.check(variant['cell'] == cell and variant['scenarioSha256'] == digest and not cell['scenario']['seeds'],
                     'Partial recipe changed identities, order, budget or undeclared equipment')
    cells = original + [v['cell'] for v in variants]
    io.check(proposal['cells'] == cells and len(cells) == len({c['id'] for c in cells}) ==
             len({signature(c['scenario']) for c in cells}) == 98, 'Duplicate or changed complete family')
    return cells


def validate_mixed_armor_family(proposal, original):
    return validate_mixed_defense_family(proposal, original, 'armor', 'armor-and-health', 38, (2, 4))


def validate_mixed_resistance_family(proposal, original):
    return validate_mixed_defense_family(proposal, original, 'resistance', 'resistance-and-health', 60, (7,))


def validate_limited_armor_family(proposal, original):
    """Floor eight retains all controls and every one/two-character armor subset."""
    io = owner_module()
    io.check(proposal.get('maximumSpecializedItems') == 8 and
             proposal.get('maximumSpecializedCharacters') == 2, 'Floor-eight equipment budget changed')
    cells = validate_mixed_defense_family(proposal, original, 'armor', 'armor-and-health', 67, (8,),
        party_size=10, subset_counts=(1, 2), version='floor8-limited-armor-proposal-v1')
    for variant in proposal['variants']:
        slots = [m['partySlot'] for m in variant['cell']['scenario']['party']
                 for item in m['build']['equipment'] if '.spec.' in item['definitionId']]
        io.check(len(slots) == 4*len(variant['armorPartySlots']) and
                 set(slots) == set(variant['armorPartySlots']), 'Floor-eight actual specialized items differ')
    return cells


def validate_limited_resistance_family(proposal, original):
    """Floor nine keeps every control and every one/two-character resistance subset."""
    io = owner_module()
    io.check(proposal.get('maximumSpecializedItems') == 8 and
             proposal.get('maximumSpecializedCharacters') == 2, 'Floor-nine equipment budget changed')
    cells = validate_mixed_defense_family(proposal, original, 'resistance', 'resistance-and-health', 38, (9,),
        party_size=10, subset_counts=(1, 2), version='floor9-limited-resistance-proposal-v1')
    for variant in proposal['variants']:
        slots = [m['partySlot'] for m in variant['cell']['scenario']['party']
                 for item in m['build']['equipment'] if '.spec.' in item['definitionId']]
        io.check(len(slots) == 4*len(variant['resistancePartySlots']) and
                 set(slots) == set(variant['resistancePartySlots']), 'Floor-nine actual specialized items differ')
    return cells


def validate_mixed_defense_family(proposal, original, defense, gear, retained, floors, *,
                                 party_size=5, subset_counts=(1, 2, 3, 4), version=None):
    """Admit only the complete, explicitly declared equipment comparison."""
    io = owner_module()
    floor = proposal['floor']
    expected_version = version or f'floor{floor}-mixed-{defense}-proposal-v1'
    party_slots = list(range(1, party_size+1))
    expected_subsets = {tuple(s) for n in subset_counts for s in itertools.combinations(party_slots, n)}
    variant_count = 2*len(expected_subsets)
    io.check(floor in floors and proposal['version'] == expected_version and
             proposal['status'] == 'ProposedNotAdmitted' and proposal['newSeeds'] == 0,
             'Expected seed-free mixed '+defense+' proposal')
    io.check(len(original) == proposal['retainedCells'] == retained and proposal['newVariants'] == variant_count and
             proposal['totalCells'] == retained+variant_count and proposal['cells'][:retained] == original,
             'Original family must be retained exactly')
    lookup = {c['id']: c for c in original}
    variants = proposal['variants']
    parents = {v['sourceId'] for v in variants}
    io.check(len(parents) == 2 and parents <= lookup.keys() and
             len({io.composition_key(lookup[p]) for p in parents}) == 2,
             'Two actual parent compositions required')
    for parent in parents:
        full = lookup[parent]
        io.check(full['gear'] == gear, 'Full '+defense+' parent required')
        bases = [c for c in original if c['composition'] == full['composition'] and c['gear'] == 'baseline']
        io.check(len(bases) == 1, 'Unambiguous baseline required')
        base = bases[0]
        normalized = copy.deepcopy(full['scenario'])
        io.check(normalized['floorNumber'] == floor and
                 [m['partySlot'] for m in normalized['party']] ==
                 [m['partySlot'] for m in base['scenario']['party']] == party_slots,
                 'Exact declared party on the declared floor required')
        for member, baseline in zip(normalized['party'], base['scenario']['party']):
            items, base_items = member['build']['equipment'], baseline['build']['equipment']
            io.check([i['slot'] for i in items] == [i['slot'] for i in base_items] and
                     len({i['slot'] for i in items}) == len(items), 'Equipment slots changed')
            changed = [a['slot'] for a, b in zip(items, base_items) if a != b]
            io.check(len(changed) == 4 and set(changed) == {'Chest', 'Head', 'Legs', 'Necklace'},
                     'Only the four saved '+defense+'/health items may differ per character')
            member['build']['equipment'] = copy.deepcopy(base_items)
        io.check(normalized == base['scenario'], 'Parent identities, order or budget changed')
        group = [v for v in variants if v['sourceId'] == parent]
        io.check(len(group) == len(expected_subsets) and {tuple(v[defense+'PartySlots']) for v in group} == expected_subsets,
                 'Every declared character subset is required exactly once')
        for variant in group:
            chosen = variant[defense+'PartySlots']
            cell = copy.deepcopy(base)
            for member, armored in zip(cell['scenario']['party'], full['scenario']['party']):
                if member['partySlot'] in chosen:
                    member['build']['equipment'] = copy.deepcopy(armored['build']['equipment'])
            digest = signature(cell['scenario'])
            cell.update(id='mixed-'+defense+'/'+digest, gear='mixed-'+defense+'-baseline-slots-'+'-'.join(map(str, chosen)),
                        origin='proposed-mixed-'+defense+'-gear')
            io.check(variant['baselineId'] == base['id'] and variant['cell'] == cell and
                     variant['scenarioSha256'] == digest and not cell['scenario']['seeds'],
                     'Mixed recipe changed identities, order, budget or undeclared equipment')
    cells = original + [v['cell'] for v in variants]
    io.check(proposal['cells'] == cells and len(cells) == len({c['id'] for c in cells}) ==
             len({signature(c['scenario']) for c in cells}) == retained+variant_count, 'Duplicate or changed complete family')
    return cells


def validate_family(proposal, original):
    validators = {'floor6-partial-restoration-proposal-v1': validate_partial_family,
                  'floor2-mixed-armor-proposal-v1': validate_mixed_armor_family,
                  'floor4-mixed-armor-proposal-v1': validate_mixed_armor_family,
                  'floor7-mixed-resistance-proposal-v1': validate_mixed_resistance_family,
                  'floor8-limited-armor-proposal-v1': validate_limited_armor_family,
                  'floor9-limited-resistance-proposal-v1': validate_limited_resistance_family}
    owner_module().check(proposal['version'] in validators, 'Unknown family proposal version')
    return validators[proposal['version']](proposal, original)


def admit_family(path, source, api, tests):
    io = owner_module()
    admission = io.read(path)
    qualified = Path(admission['qualificationOwner'])
    for name, pin in admission['qualificationPins'].items():
        io.check(io.sha(qualified/name) == pin, 'Qualification receipt changed')
    io.check(set(admission['qualificationPins']) == {'request.json', 'result.json', 'process.json', 'completion.json'},
             'Incomplete qualification receipt pins')
    request, result, process, completion = [io.read(qualified/name) for name in
        ('request.json', 'result.json', 'process.json', 'completion.json')]
    plan_path = Path(request['qualificationPlan'])
    io.check(io.sha(plan_path) == request['qualificationPlanPin'] == result['qualificationPlanPin'], 'Qualification plan changed')
    plan = io.read(plan_path)
    validate_plan(plan, source, api, tests)
    io.check(io.sha(source/'files.json') == request['manifestPin'] == result['manifestPin'] and
             io.sha(request['audit']) == request['auditPin'], 'Qualification source audit changed')
    audit = io.read(request['audit'])
    io.check(audit['status'] == 'Verified' and audit['assessment']['verdict'] == 'Pass' and
             audit['resultSha256'] == io.sha(source/'result.json'), 'Qualification audit does not bind source')
    io.check(result['status'] == 'QualifiedInputsAndReplaysVerified' and completion['status'] == 'Qualified' and
             io.sha(qualified/'result.json') == completion['resultSha256'] and
             result['matchedInputs'] == completion['matchedInputs'] == audit['evaluationFights'] and
             result['fullReplays'] == completion['fullReplays'] == audit['assessment']['familySize'] and
             result['newSeeds'] == completion['newSeeds'] == 0 and
             result['execution']['assemblyHashes'] == plan['assemblyHashes'] and
             process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
             'Qualification parity incomplete')
    proposal_path = Path(admission['proposal'])
    io.check(io.sha(proposal_path) == admission['proposalSha256'], 'Proposal changed')
    proposal = io.read(proposal_path)
    io.check(proposal['floor'] == plan['floor'] and Path(proposal['source']).resolve() == source.resolve() and
             proposal['sourceManifestSha256'] == plan['sourceManifestSha256'] and
             proposal['sourceCellsSha256'] == io.sha(source/'cells.json') and
             proposal['currentTowerSha256'] == plan['currentContentHashes']['world-tower/tower-floors.json'] and
             proposal['currentAbilitiesSha256'] == plan['currentContentHashes']['combat/abilities.json'],
             'Proposal source or current catalog differs')
    return validate_family(proposal, io.read(source/'cells.json'))
