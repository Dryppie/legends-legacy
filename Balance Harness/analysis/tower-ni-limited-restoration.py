"""Exact floor-nine Restoration subsets and a bounded non-acceptance diagnostic."""
import copy
import importlib.util
from pathlib import Path

def module(name, file):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(file))
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value

h = module('ni_restoration_family', 'tower-one-healer-family.py')
n = module('ni_restoration_candidate', 'tower-ni-penetration.py')
VERSION = 'floor9-limited-restoration-proposal-v1'
DIAGNOSTIC = 'floor9-limited-restoration-diagnostic-v1'
PROVENANCE = 'ni-restoration-diagnostic-provenance.json'
FAMILY_PROVENANCE = 'ni-restoration-family-provenance.json'
SLOTS = ['MainHand', 'Chest', 'Head', 'Necklace']
LIMITS = dict(diagnosticOnly=True, usedForAcceptance=False, batches=2, seedsPerBatch=16,
              maximumFights=5216, maximumNewSeeds=32, nativeSecondsLimit=840,
              ownerSecondsLimit=900, byteLimit=2*1024**3, retries=0, extensions=0,
              confirmation=False, application=False)


def derive(original):
    parents = [c for c in original if c['gear'] == 'restorer-specialization']
    h.check(len(original) == len({c['id'] for c in original}) == 148 and len(parents) == 5 and
            len({h.composition(c) for c in parents}) == 5, 'Complete original family and five distinct Restoration parents required')
    h.check(all(c['scenario']['floorNumber'] == 9 and not c['scenario']['seeds'] for c in original), 'Seed-free floor-nine family required')
    variants = []
    for full in parents:
        matches = [c for c in original if c['composition'] == full['composition'] and c['gear'] == 'baseline']
        h.check(len(matches) == 1, 'Unambiguous baseline required'); baseline = matches[0]
        h.check(not h.specialized(baseline) and h.specialized(full) == [2]*6+[7]*6, 'Original six-piece healer parents required')
        normalized = copy.deepcopy(full['scenario'])
        h.check([m['partySlot'] for m in normalized['party']] == list(range(1, 11)), 'Original party order required')
        for member, base in zip(normalized['party'], baseline['scenario']['party'], strict=True):
            changed = [i['slot'] for i,b in zip(member['build']['equipment'], base['build']['equipment'], strict=True) if i != b]
            h.check(set(changed) == (h.SLOTS if member['partySlot'] in (2,7) else set()) and len(changed) in (0,6), 'Undeclared donor equipment difference')
            member['build']['equipment'] = copy.deepcopy(base['build']['equipment'])
        h.check(normalized == baseline['scenario'], 'Parent differs beyond equipment')
        for healer_slots, equipment_slots in [([2], None), ([7], None), ([2,7], SLOTS)]:
            cell = copy.deepcopy(baseline)
            identity = dict(parent=full['id'], partySlots=healer_slots, equipmentSlots=equipment_slots)
            cell.update(id='ni-limited-restoration/'+h.signature(identity),
                        gear='ni-restoration-'+('-'.join(map(str,healer_slots)))+('-four-each' if equipment_slots else '-six'),
                        origin='proposed-ni-limited-restoration')
            for member, donor in zip(cell['scenario']['party'], full['scenario']['party'], strict=True):
                if member['partySlot'] in healer_slots:
                    for index, item in enumerate(member['build']['equipment']):
                        donated = donor['build']['equipment'][index]
                        h.check(item['slot'] == donated['slot'], 'Equipment order differs')
                        if '.spec.' in donated['definitionId'] and (equipment_slots is None or item['slot'] in equipment_slots):
                            member['build']['equipment'][index] = copy.deepcopy(donated)
            expected = [2]*4+[7]*4 if equipment_slots else healer_slots*6
            h.check(h.specialized(cell) == expected, 'Exact six/eight-item subset required')
            variants.append(dict(sourceId=full['id'], baselineId=baseline['id'], specializedPartySlots=healer_slots,
                                 equipmentSlots=equipment_slots, specializedItems=len(expected), cell=cell))
    return variants


def validate(proposal, original):
    h.check(proposal['version'] == VERSION and proposal['status'] == 'ProposedNotPrepared' and proposal['floor'] == 9 and
            proposal['newFights'] == proposal['newSeeds'] == proposal['nativePreparations'] == 0, 'Unallocated floor-nine proposal required')
    h.check((proposal['retainedCells'], proposal['newVariants'], proposal['totalCells'], proposal['actualCompositions'], proposal['eligibleRecipes']) == (148,15,163,5,130) and
            proposal['equipmentEligibility'] == h.EQUIPMENT and proposal['proposedDiagnostic'] == LIMITS, 'Family or diagnostic limits changed')
    n.validate_plan(proposal['candidatePlan'], 9)
    variants = derive(original); cells = original+[v['cell'] for v in variants]
    h.check(proposal['variants'] == variants and proposal['cells'] == cells, 'Raw recipe, control or derivation changed')
    h.check(len({c['id'] for c in cells}) == len({h.signature(c['scenario']) for c in cells}) == 163, 'Duplicate recipe')
    h.check(sum(len(h.specialized(c)) <= 8 and len(set(h.specialized(c))) <= 2 for c in cells) == 130, 'Equipment eligibility differs')
    return cells


def admit(path, source):
    p = h.read(path)
    h.check(Path(p['source']).resolve() == source.resolve() and h.sha(source/'files.json') == p['sourceManifestSha256'] and
            h.sha(source/'cells.json') == p['sourceCellsSha256'], 'Original source changed')
    h.check(h.sha(p['precedingEvidence']) == p['precedingEvidenceSha256'], 'Closed evidence changed')
    e = h.read(p['precedingEvidence'])
    h.check(e['status'] == 'Verified' and e['trialStatus'] == 'NiPenetrationScreenNotAccepted' and
            e['freshStudyFights'] == 18944 and e['finalExclusions'] == 926476, 'Complete rejected Ni screen required')
    n.expected(source/'content', p['candidatePlan'], 9)
    return validate(p, h.read(source/'cells.json'))


def validate_mode(mode, floor, source, modifications):
    h.check(mode == 'prepare' and floor == 9 and source is not None and not any(modifications), 'Restoration family requires unmixed seed-free preparation')


def validate_panel(mode, floor, samples, version, family):
    h.check(version == DIAGNOSTIC and mode == 'screen' and floor == 9 and samples == 16 and family == 163,
            'Only the exact non-acceptance Restoration panel is supported')


def validate_contract(d, cells):
    h.check(d['version'] == DIAGNOSTIC and d['limits'] == LIMITS and d['floor'] == 9 and d['familySize'] == 163,
            'Diagnostic limits changed')
    h.check(len(cells) == 163 and d['cellsSha256'] == h.sha(Path(d['source'])/'cells.json'), 'Declared family changed')
    h.check(d['initialExclusions'] == 926476 and d['eligibleRecipes'] == 130 and d['actualCompositions'] == 5, 'Diagnostic context changed')
    h.check(len(d['studies']) == len(set(d['studies'])) == 2 and all(Path(p).is_absolute() for p in d['studies']), 'Two distinct absolute studies required')
    n.validate_plan(d['candidatePlan'], 9)


def read_contract(io, path, pin):
    h.check(pin is not None and h.sha(path) == pin, 'Diagnostic declaration pin required')
    d = h.read(path); source = Path(d['source']); cells = h.read(source/'cells.json')
    validate_contract(d, cells)
    for group in ('sourcePins', 'liveCatalogPins', 'ledgerPins'):
        for member, expected in d[group].items(): h.check(h.sha(member) == expected, 'Diagnostic input changed: '+member)
    io.authenticate(source)
    h.check(h.read(source/'request.json')['mode'] == 'prepare' and h.read(source/'completion.json')['status'] == 'Complete', 'Original-catalog prepared source required')
    proposal = Path(d['proposal']); p = h.read(proposal)
    h.check(h.sha(proposal) == d['proposalSha256'] and cells == admit(proposal, Path(p['source'])), 'Exact proposal differs')
    h.check(d['candidatePlan'] == p['candidatePlan'], 'Candidate changed')
    n.expected(source/'content', d['candidatePlan'], 9)
    return d, cells


def audit_request(io, output, q):
    paths = [Path(p) for p in q['inputHashes'] if Path(p).name == PROVENANCE]
    h.check(q.get('diagnosticVersion') == DIAGNOSTIC and len(paths) == 1, 'Diagnostic marker/provenance mismatch')
    provenance = paths[0]; h.check(h.sha(provenance) == q['inputHashes'][str(provenance)], 'Provenance changed')
    p = h.read(provenance); d,cells = read_contract(io, Path(p['declaration']), p['declarationSha256'])
    index = p['batchIndex']; h.check(type(index) is int and index in (0,1) and Path(d['studies'][index]) == output, 'Undeclared batch')
    validate_panel(q['mode'], q['floor'], len(q['seeds']), q['diagnosticVersion'], len(cells))
    h.check(q['maximumFights'] == 2608 and not q['searchSeeds'] and q.get('nativeSeconds',840) == 840, 'Diagnostic request exceeded limits')
    h.check(h.read(output/'cells.json') == cells, 'Diagnostic changed controls')
    result,scope = h.read(output/'result.json'), h.read(output/'scope.json')
    h.check(result.get('diagnosticOnly') is True and result.get('usedForAcceptance') is False, 'Diagnostic outcome lacks non-acceptance marker')
    h.check(scope['execution'] == d['execution'] and scope['settings'] == d['settings'] and scope['contentHashes'] == d['candidateContentHashes'], 'Diagnostic scope differs')
    n.verify(Path(d['source'])/'content', output/'content', d['candidatePlan'], 9)


def admit_batch(io, path, pin, index, source, output, mode, floor, samples, candidate, modifications, native_seconds, *, scalar_factors):
    h.check(type(index) is int and index in (0,1) and not any(modifications) and native_seconds == 840 and
            candidate is None and scalar_factors == (1.0,.95,40.0), 'Mixed or changed diagnostic candidate')
    d,cells = read_contract(io, path, pin)
    validate_panel(mode, floor, samples, d['version'], len(cells))
    h.check(source.resolve() == Path(d['source']).resolve() and output == Path(d['studies'][index]), 'Unfrozen source or path')
    h.check(h.sha(d['initialHistory']) == d['initialHistorySha256'], 'History changed')
    expected = set(h.read(d['initialHistory'])); h.check(len(expected) == 926476, 'Initial exclusions differ')
    for previous in d['studies'][:index]:
        study = Path(previous); owner = study.with_name(study.name.replace('-study-','-owner-'))
        h.check(io.audit(study) == h.read(owner/'independent-audit.json'), 'Preceding diagnostic incomplete')
        q = h.read(study/'request.json'); audit_request(io, study, q)
        h.check(len(q['seeds']) == len(set(q['seeds'])) == 16 and not expected.intersection(q['seeds']), 'Repeated or incomplete seeds')
        expected.update(q['seeds'])
    for pending in d['studies'][index:]:
        study = Path(pending); h.check(not study.exists() and not study.with_name(study.name.replace('-study-','-owner-')).exists(), 'No retry or replacement panel')
    h.check(io.history()[0] == expected, 'Unexpected allocation between diagnostic panels')
    return d
