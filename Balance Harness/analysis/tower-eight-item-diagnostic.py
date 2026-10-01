"""Exact eight-item family and a separately bounded, non-acceptance diagnostic."""
import copy
import importlib.util
from pathlib import Path

s = importlib.util.spec_from_file_location('eight_item_family_base', Path(__file__).with_name('tower-one-healer-family.py'))
h = importlib.util.module_from_spec(s); s.loader.exec_module(h)
VERSION = 'floor8-two-healer-eight-item-proposal-v1'
DIAGNOSTIC = 'floor8-two-healer-eight-item-diagnostic-v1'
SLOTS = ('MainHand', 'Chest', 'Head', 'Necklace')
LIMITS = dict(diagnosticOnly=True, usedForAcceptance=False, batches=2, seedsPerBatch=8,
              maximumFights=2976, maximumNewSeeds=16, nativeSecondsLimit=840,
              ownerSecondsLimit=900, byteLimit=2*1024**3, retries=0, extensions=0,
              confirmation=False, application=False)


def derive(original, parents):
    lookup = {c['id']: c for c in original}; variants = []
    h.check(len(lookup) == len(original) and len(parents) == len(set(parents)) == 3, 'Three distinct saved parents required')
    h.check(all(p in lookup for p in parents) and len({h.composition(lookup[p]) for p in parents}) == 3, 'Three actual original compositions required')
    for parent in parents:
        full = lookup[parent]
        baselines = [c for c in original if c['composition'] == full['composition'] and c['gear'] == 'baseline']
        h.check(len(baselines) == 1 and full['gear'] == 'restorer-specialization', 'Unambiguous Restoration parent required')
        baseline = baselines[0]; cell = copy.deepcopy(baseline)
        h.check(not h.specialized(baseline) and h.specialized(full) == [2]*6+[7]*6, 'Original six-piece healer parents required')
        normalized = copy.deepcopy(full['scenario'])
        for m,b in zip(normalized['party'],baseline['scenario']['party'],strict=True): m['build']['equipment'] = copy.deepcopy(b['build']['equipment'])
        h.check(normalized == baseline['scenario'], 'Parent differs beyond gear')
        cell.update(id='two-healer-eight-item/'+h.signature(dict(parent=parent,slots=SLOTS)),
                    gear='two-healer-restoration-mainhand-chest-head-necklace',origin='proposed-two-healer-eight-item')
        for member,donor in zip(cell['scenario']['party'],full['scenario']['party'],strict=True):
            if member['partySlot'] in (2,7):
                for index,item in enumerate(member['build']['equipment']):
                    if item['slot'] in SLOTS:
                        h.check(item['slot'] == donor['build']['equipment'][index]['slot'], 'Equipment order differs')
                        member['build']['equipment'][index] = copy.deepcopy(donor['build']['equipment'][index])
        h.check(h.specialized(cell) == [2]*4+[7]*4, 'Exactly four saved pieces per healer required')
        variants.append(dict(sourceId=parent,baselineId=baseline['id'],specializedPartySlots=[2,7],equipmentSlots=list(SLOTS),specializedItems=8,cell=cell))
    return variants


def validate(p, original, parents):
    h.check(p['version'] == VERSION and p['status'] == 'ProposedNotPrepared' and p['floor'] == 8 and
            p['newFights'] == p['newSeeds'] == p['nativePreparations'] == 0, 'Unallocated original proposal required')
    h.check((p['retainedCells'],p['newVariants'],p['totalCells'],p['actualCompositions'],p['eligibleRecipes']) == (183,3,186,9,128)
            and p['equipmentEligibility'] == h.EQUIPMENT and p['proposedDiagnostic'] == LIMITS, 'Family, gear or diagnostic limits changed')
    h.check(len(original) == 183 and len({h.composition(c) for c in original}) == 9 and
            all(c['scenario']['floorNumber'] == 8 and not c['scenario']['seeds'] for c in original), 'Complete seed-free original family required')
    variants = derive(original, parents)
    h.check(p['variants'] == variants and p['cells'] == original+[v['cell'] for v in variants], 'Recipe, identity, ordering or control changed')
    cells = p['cells']
    h.check(len({c['id'] for c in cells}) == len({h.signature(c['scenario']) for c in cells}) == 186, 'Duplicate recipe')
    h.check(sum(len(h.specialized(c)) <= 8 and len(set(h.specialized(c))) <= 2 for c in cells) == 128, 'Actual equipment eligibility changed')
    return cells


def admit(path, source):
    p = h.read(path)
    h.check(Path(p['source']).resolve() == source.resolve() and h.sha(source/'files.json') == p['sourceManifestSha256'] and
            h.sha(source/'cells.json') == p['sourceCellsSha256'], 'Proposal source changed')
    h.check(h.sha(p['precedingEvidence']) == p['precedingEvidenceSha256'], 'Preceding diagnostic changed')
    e = h.read(p['precedingEvidence'])
    h.check(e['status'] == 'Verified' and e['trialStatus'] == 'CompletedDiagnosticOnly' and e['diagnosticOnly'] is True and
            e['usedForAcceptance'] is False and e['familySize'] == 183, 'Closed non-acceptance evidence required')
    parents = [c['twoHealers']['id'] for c in e['comparisons'][::2]]
    h.check([c['oneHealer']['wins'] for c in e['comparisons']] == [1,0,0,1,0,0] and
            [c['twoHealers']['wins'] for c in e['comparisons'][::2]] == [5,6,6], 'Frozen parent selection differs')
    return validate(p,h.read(source/'cells.json'),parents)


def validate_mode(mode, floor, source, modifications):
    h.check(mode == 'prepare' and floor == 8 and source is not None and not any(modifications), 'Eight-item family requires unmixed seed-free preparation')


def validate_panel(mode, floor, samples, version, family=None):
    if version is None:
        h.check(16 <= samples <= 512, 'Ordinary screen/search/confirm requires 16–512 seeds')
    else:
        h.check(version == DIAGNOSTIC and mode == 'screen' and floor == 8 and samples == 8 and family == 186,
                'Only the declared floor-eight diagnostic admits eight seeds')


def validate_contract(d, cells):
    h.check(d['version'] == DIAGNOSTIC and d['limits'] == LIMITS and d['floor'] == 8 and d['familySize'] == 186,
            'Diagnostic limits changed')
    h.check(len(cells) == 186 and d['cellsSha256'] == h.sha(Path(d['source'])/'cells.json'), 'Declared family changed')
    h.check(len(d['studies']) == len(set(d['studies'])) == 2 and all(Path(p).is_absolute() for p in d['studies']), 'Two distinct absolute diagnostic paths required')
    h.check(d['initialExclusions'] == 924204 and d['eligibleRecipes'] == 128 and d['actualCompositions'] == 9, 'Diagnostic context changed')


def read_contract(io, path, pin):
    h.check(pin is not None and h.sha(path) == pin, 'Diagnostic declaration pin required')
    d = h.read(path); source = Path(d['source']); cells = h.read(source/'cells.json')
    validate_contract(d,cells)
    for group in ('sourcePins','liveCatalogPins','ledgerPins'):
        for member,expected in d[group].items(): h.check(h.sha(member) == expected, 'Diagnostic input changed: '+member)
    io.authenticate(source)
    h.check(h.read(source/'request.json')['mode'] == 'prepare' and h.read(source/'completion.json')['status'] == 'Complete', 'Original-catalog prepared source required')
    proposal = Path(d['proposal']); p = h.read(proposal)
    h.check(h.sha(proposal) == d['proposalSha256'] and cells == admit(proposal,Path(p['source'])), 'Declared exact proposal differs')
    h.check(d['candidatePlan'] == p['candidatePlan'], 'Diagnostic candidate differs')
    io.ability_module().validate(source/'content',d['candidatePlan'],8)
    return d,cells


def audit_request(io, output, q, *, contract_reader=None, panel_validator=None, provenance_name='eight-item-diagnostic-provenance.json'):
    contract_reader = contract_reader or read_contract; panel_validator = panel_validator or validate_panel
    paths = [Path(p) for p in q['inputHashes'] if Path(p).name == provenance_name]
    version = q.get('diagnosticVersion')
    h.check(bool(paths) == bool(version) and len(paths) <= 1, 'Diagnostic marker/provenance mismatch')
    if not paths: return
    p = h.read(paths[0]); h.check(h.sha(paths[0]) == q['inputHashes'][str(paths[0])], 'Diagnostic provenance changed')
    d,cells = contract_reader(io,Path(p['declaration']),p['declarationSha256'])
    index = p['batchIndex']; h.check(type(index) is int and index in range(d['limits']['batches']) and Path(d['studies'][index]) == output, 'Undeclared diagnostic path')
    panel_validator(q['mode'],q['floor'],len(q['seeds']),version,len(cells))
    h.check(q['maximumFights'] == len(cells)*d['limits']['seedsPerBatch'] and not q['searchSeeds'] and q.get('nativeSeconds',840) == 840, 'Diagnostic request exceeded limits')
    h.check(h.read(output/'cells.json') == cells, 'Diagnostic dropped or changed controls')
    result = h.read(output/'result.json'); scope = h.read(output/'scope.json')
    h.check(result.get('diagnosticOnly') is True and result.get('usedForAcceptance') is False, 'Diagnostic outcome lacks non-acceptance marker')
    h.check(scope['execution'] == d['execution'] and scope['settings'] == d['settings'] and scope['contentHashes'] == d['candidateContentHashes'], 'Diagnostic scope differs')


def admit_batch(io, path, pin, index, source, output, mode, floor, samples, candidate, modifications, native_seconds,
                *, contract_reader=None, panel_validator=None, provenance_name='eight-item-diagnostic-provenance.json', batch_count=2):
    contract_reader = contract_reader or read_contract; panel_validator = panel_validator or validate_panel
    h.check(type(index) is int and index in range(batch_count) and not any(modifications) and native_seconds == 840, 'Invalid or mixed diagnostic batch')
    d,cells = contract_reader(io,path,pin)
    h.check(d['limits']['batches'] == batch_count, 'Diagnostic batch count differs from its version')
    panel_validator(mode,floor,samples,d['version'],len(cells))
    h.check(source.resolve() == Path(d['source']).resolve() and output == Path(d['studies'][index]) and
            candidate is not None and h.read(candidate) == d['candidatePlan'], 'Unfrozen source, path or candidate')
    h.check(h.sha(d['initialHistory']) == d['initialHistorySha256'], 'Initial history changed')
    expected = set(h.read(d['initialHistory'])); h.check(len(expected) == d['initialExclusions'], 'Incomplete initial exclusions')
    for previous in d['studies'][:index]:
        study = Path(previous); owner = study.with_name(study.name.replace('-study-','-owner-'))
        h.check(io.audit(study) == h.read(owner/'independent-audit.json'), 'Preceding diagnostic not independently complete')
        q = h.read(study/'request.json'); audit_request(io,study,q,contract_reader=contract_reader,panel_validator=panel_validator,provenance_name=provenance_name)
        h.check(not expected.intersection(q['seeds']), 'Repeated diagnostic seeds'); expected.update(q['seeds'])
    for pending in d['studies'][index:]:
        study = Path(pending); h.check(not study.exists() and not study.with_name(study.name.replace('-study-','-owner-')).exists(), 'No retry or replacement panel')
    h.check(io.history()[0] == expected, 'Unexpected allocation between diagnostic panels')
    return d
