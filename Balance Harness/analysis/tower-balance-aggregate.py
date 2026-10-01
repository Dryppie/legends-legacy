"""Prospective fixed-setting Tower panels split only for native resource limits.

Every declared batch is required. Earlier experiments and individual batch
verdicts never contribute to aggregate acceptance. This module allocates no seeds.
"""
import importlib.util
import copy
import math
from pathlib import Path
from statistics import NormalDist

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('aggregate_balance_io', HERE / 'run-tower-balance-pass.py')
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
VERSION = 'tower-balance-aggregate-v1'
RECOVERY_VERSION = 'tower-balance-recovery-aggregate-v1'
LIMITED_ARMOR_VERSION = 'tower-balance-limited-armor-aggregate-v1'
LIMITED_RESISTANCE_VERSION = 'tower-balance-limited-resistance-aggregate-v1'
MIASMA_VERSION = 'tower-balance-miasma-aggregate-v1'
MIASMA_RESOURCE_VERSION = 'tower-balance-miasma-resource-aggregate-v1'
SHARED_PENETRATION_VERSION = 'tower-balance-shared-penetration-aggregate-v1'
KODOKU_FIXED_VERSION = 'tower-balance-kodoku-fixed-aggregate-v1'
KODOKU_MIDPOINT_VERSION = 'tower-balance-kodoku-midpoint-aggregate-v1'
NI_COPY_HEALTH_VERSION = 'tower-balance-ni-copy-health-aggregate-v1'
NI_RESTORATION_VERSION = 'tower-balance-ni-restoration-aggregate-v1'
NI_PENETRATION_VERSION = 'tower-balance-ni-penetration-aggregate-v1'
KODOKU_ACCEPTANCE_VERSIONS = (KODOKU_FIXED_VERSION, KODOKU_MIDPOINT_VERSION)


def ni_penetration_module():
    spec = importlib.util.spec_from_file_location('aggregate_ni_penetration', HERE/'tower-ni-penetration.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def batch_count(version):
    if version == NI_RESTORATION_VERSION: return 8
    if version in KODOKU_ACCEPTANCE_VERSIONS:
        return 32
    io.check(version in (VERSION, RECOVERY_VERSION, LIMITED_ARMOR_VERSION, LIMITED_RESISTANCE_VERSION, NI_COPY_HEALTH_VERSION, NI_PENETRATION_VERSION, MIASMA_VERSION, MIASMA_RESOURCE_VERSION, SHARED_PENETRATION_VERSION), 'Unknown aggregate version')
    return 8 if version in (RECOVERY_VERSION, MIASMA_RESOURCE_VERSION, SHARED_PENETRATION_VERSION) else 4


def validate_layout(d):
    count = batch_count(d['version'])
    samples = 64 if d['version'] == NI_RESTORATION_VERSION else 16 if d['version'] in (MIASMA_RESOURCE_VERSION, SHARED_PENETRATION_VERSION, *KODOKU_ACCEPTANCE_VERSIONS) else 32 if d['version'] in (LIMITED_ARMOR_VERSION, LIMITED_RESISTANCE_VERSION, NI_COPY_HEALTH_VERSION, NI_PENETRATION_VERSION, MIASMA_VERSION) else 128
    io.check(d['batchCount'] == count and d['samplesPerBatch'] == samples, 'Unsupported prospective panel')
    if d['version'] in KODOKU_ACCEPTANCE_VERSIONS:
        io.check(d['floor'] == 8 and d['familySize'] == 186, 'Wrong fixed Kodoku family/floor')
        helper = io.ability_module().kodoku_midpoint_module() if d['version'] == KODOKU_MIDPOINT_VERSION else io.ability_module().kodoku_refinement_module()
        helper.legacy_plan(d['candidatePlan'], d['floor'])
    elif d['version'] == NI_RESTORATION_VERSION:
        io.check(d['floor'] == 9 and d['familySize'] == 163, 'Wrong nominated Ni family/floor')
        io.ability_module().ni_restoration_acceptance_module().validate_plan(d['candidatePlan'], 9)
    elif d['version'] == NI_PENETRATION_VERSION:
        io.check(d['floor'] == 9 and d['familySize'] == 148, 'Wrong Ni penetration family/floor')
        ni_penetration_module().validate_plan(d['candidatePlan'], d['floor'])
    elif d['version'] == NI_COPY_HEALTH_VERSION:
        io.check(d['floor'] == 9 and d['familySize'] == 148, 'Wrong copy-Health family/floor')
        io.ability_module().ni_copy_health_module().validate_plan(d['candidatePlan'], d['floor'])
    elif d['version'] == SHARED_PENETRATION_VERSION:
        io.check(d['floor'] == 8 and d['familySize'] == 177, 'Wrong shared-penetration family/floor')
        io.ability_module().shared_penetration_module().validate_plan(d['candidatePlan'], d['floor'])
    elif d['version'] in (MIASMA_VERSION, MIASMA_RESOURCE_VERSION):
        io.check(d['floor'] == 8 and d['familySize'] == 177, 'Wrong Miasma family/floor')
        io.ability_module().miasma_module().validate_plan(d['candidatePlan'], d['floor'])
    elif d['version'] == LIMITED_ARMOR_VERSION:
        io.check(d['floor'] == 8 and d['familySize'] == 177 and
                 d['candidatePlan'] == {'version': 'tower-unchanged-catalog-v1'}, 'Wrong limited-armor family/candidate')
    elif d['version'] == LIMITED_RESISTANCE_VERSION:
        io.check(d['floor'] == 9 and d['familySize'] == 148 and
                 d['candidatePlan'] == {'version': 'tower-unchanged-catalog-v1'}, 'Wrong limited-resistance family/candidate')
    elif d['version'] == RECOVERY_VERSION:
        io.check(d['floor'] == 7 and d['familySize'] == 120 and
                 d['candidatePlan']['version'] == 'tower-recovery-pressure-refinement-v1', 'Wrong recovery aggregate family/candidate')
    elif d['candidatePlan']['version'] in ('tower-ni-restoration-acceptance-v1', 'tower-ni-restoration-offense-v1', 'tower-ni-penetration-v1', 'tower-ni-copy-health-v1', 'tower-recovery-pressure-refinement-v1', 'tower-unchanged-catalog-v1', 'tower-kodoku-miasma-v1', 'tower-kodoku-shared-penetration-v1', 'tower-kodoku-eight-item-pressure-refinement-v1', 'tower-kodoku-eight-item-pressure-midpoint-v1'):
        raise ValueError('Candidate requires its separate aggregate contract')
LIMITED_EQUIPMENT = dict(version='tower-limited-equipment-v1', maximumSpecializedItems=8,
                         maximumSpecializedCharacters=2)


def candidate_kind(d):
    version = d['candidatePlan'].get('version')
    if version == 'tower-ni-restoration-acceptance-v1':
        io.check(d.get('version') == NI_RESTORATION_VERSION, 'Nominated Ni requires its separate acceptance contract')
        return 'ability'
    if version == 'tower-ni-penetration-v1':
        io.check(d.get('version') == NI_PENETRATION_VERSION, 'Ni penetration requires its separate contract')
        return 'penetration'
    if version == 'tower-ni-copy-health-v1':
        io.check(d.get('version') == NI_COPY_HEALTH_VERSION, 'Copy Health requires its separate contract')
        return 'ability'
    if version == 'tower-kodoku-eight-item-pressure-midpoint-v1':
        io.check(d.get('version') == KODOKU_MIDPOINT_VERSION, 'Midpoint Kodoku requires its separate contract')
        return 'ability'
    if version == 'tower-kodoku-eight-item-pressure-refinement-v1':
        io.check(d.get('version') == KODOKU_FIXED_VERSION, 'Fixed Kodoku requires its separate contract')
        return 'ability'
    if version == 'tower-kodoku-shared-penetration-v1':
        io.check(d.get('version') == SHARED_PENETRATION_VERSION, 'Shared penetration requires its separate contract')
        return 'ability'
    if version == 'tower-kodoku-miasma-v1':
        io.check(d.get('version') in (MIASMA_VERSION, MIASMA_RESOURCE_VERSION), 'Miasma requires its separate versioned contract')
        return 'ability'
    if version == 'tower-unchanged-catalog-v1':
        io.check(d.get('version') in (LIMITED_ARMOR_VERSION, LIMITED_RESISTANCE_VERSION), 'Unchanged catalog requires a separate limited-equipment contract')
        return 'unchanged'
    if version == 'tower-recovery-pressure-refinement-v1':
        io.check(d.get('version') == RECOVERY_VERSION, 'Recovery requires its separate eight-batch contract')
        return 'recovery-pressure'
    io.check(version in ('tower-ability-coefficients-v1', 'tower-ability-coefficients-v2',
                        'tower-ability-coefficients-v3', 'tower-ability-health-target-v1', 'tower-health-pressure-candidate-v1'),
             'Unsupported aggregate candidate')
    return 'health-pressure' if version == 'tower-health-pressure-candidate-v1' else 'ability'


def validate_candidate(d, content):
    equipment = d.get('equipmentEligibility')
    if d['candidatePlan'].get('version') in ('tower-ni-restoration-acceptance-v1', 'tower-ni-restoration-offense-v1', 'tower-ni-penetration-v1', 'tower-ni-copy-health-v1', 'tower-kodoku-miasma-v1', 'tower-kodoku-shared-penetration-v1', 'tower-kodoku-eight-item-pressure-refinement-v1', 'tower-kodoku-eight-item-pressure-midpoint-v1'):
        validate_layout(d)
        io.check(equipment == LIMITED_EQUIPMENT, 'Miasma requires actual limited-equipment eligibility')
    if equipment is not None:
        io.check(equipment == LIMITED_EQUIPMENT and d.get('gear') is None, 'Unsupported or ambiguous equipment contract')
    if candidate_kind(d) == 'unchanged':
        validate_layout(d)
        io.check(equipment == LIMITED_EQUIPMENT, 'Limited armor requires actual limited-equipment eligibility')
        hashes = {p.relative_to(content/'Data').as_posix(): io.sha(p) for p in (content/'Data').rglob('*.json')}
        io.check(hashes == d['candidateContentHashes'], 'Unchanged catalog differs from the qualified source')
    elif candidate_kind(d) == 'penetration':
        ni_penetration_module().expected(content, d['candidatePlan'], d['floor'])
    elif candidate_kind(d) == 'recovery-pressure':
        io.check(equipment == LIMITED_EQUIPMENT, 'Recovery aggregate requires actual limited-equipment eligibility')
        io.recovery_pressure_module(d['candidatePlan']['version']).expected(content, d['candidatePlan'], d['floor'])
    elif candidate_kind(d) == 'health-pressure':
        io.check(equipment == LIMITED_EQUIPMENT, 'Health-pressure aggregate requires actual limited-equipment eligibility')
        io.health_pressure_module().expected(content, d['candidatePlan'], d['floor'])
    else:
        io.ability_module().validate(content, d['candidatePlan'], d['floor'])


def equipment_eligible(cell, contract):
    io.check(contract == LIMITED_EQUIPMENT, 'Unsupported equipment contract')
    slots = [m['partySlot'] for m in cell['scenario']['party'] for item in m['build']['equipment']
             if '.spec.' in item['definitionId']]
    return len(slots) <= contract['maximumSpecializedItems'] and len(set(slots)) <= contract['maximumSpecializedCharacters']


def interval(wins, samples, family):
    io.check(type(wins) is int and type(samples) is int and 0 <= wins <= samples and samples > 0,
             'Invalid binomial counts')
    z = NormalDist().inv_cdf(1 - .05 / (2 * family))
    p = wins / samples
    denominator = 1 + z*z / samples
    center = (p + z*z / (2*samples)) / denominator
    radius = z * (p*(1-p)/samples + z*z/(4*samples*samples))**.5 / denominator
    return max(0, center-radius), min(1, center+radius)


def assess(cells, batches, batch_count, samples, gear, equipment=None):
    """Called only with complete, independently reconstructed batch rows."""
    io.check(len(batches) == batch_count and batch_count > 1, 'Every predeclared batch is required')
    if equipment is not None:
        io.check(equipment == LIMITED_EQUIPMENT and gear is None, 'Unsupported or ambiguous equipment contract')
    ids = [c['id'] for c in cells]
    io.check(len(set(ids)) == len(ids) and bool(ids), 'Unique complete family required')
    totals = dict.fromkeys(ids, 0)
    for batch in batches:
        io.check([r['id'] for r in batch] == ids, 'Missing, reordered or repeated recipe')
        for row in batch:
            io.check(type(row['wins']) is int and 0 <= row['wins'] <= samples and row['samples'] == samples,
                     'Wrong batch sample count')
            totals[row['id']] += row['wins']
    n = samples * batch_count
    rows, partial = [], {}
    for cell in cells:
        wins = totals[cell['id']]
        low, high = interval(wins, n, len(cells))
        key = io.qualification_module().signature(io.composition_key(cell))
        row = dict(id=cell['id'], composition=cell['composition'], actualComposition=key,
                   gear=cell['gear'], wins=wins, samples=n, lower=low, upper=high)
        eligible = equipment_eligible(cell, equipment) if equipment is not None else cell['gear'] == gear
        if equipment is not None: row['eligibleGear'] = eligible
        rows.append(row)
        if eligible:
            partial[key] = max(partial.get(key, 0), low)
    qualifying = sum(low >= .1 for low in partial.values())
    ceiling = max(r['upper'] for r in rows)
    result = dict(verdict='Pass' if qualifying >= 2 and ceiling <= .5 else 'NotAccepted',
                familySize=len(cells), samples=n, alpha=.05, method='approximate Bonferroni-Wilson',
                minimumDistinctCompositions=2, partialGear=gear,
                qualifyingPartialCompositions=qualifying, maximumUpper=ceiling, rows=rows)
    if equipment is not None:
        result.update(equipmentEligibility=equipment, eligibleRecipes=sum(r['eligibleGear'] for r in rows))
    return result


def validate_paths(phases, root, version=VERSION):
    count = batch_count(version)
    io.check(set(phases) == {'screen', 'confirm'}, 'Both prospective phases required')
    paths = [Path(p).resolve() for phase in ('screen', 'confirm') for p in phases[phase]]
    io.check(all(len(phases[p]) == count for p in phases) and len(set(paths)) == 2*count,
             'Every versioned batch path must be unique')
    io.check(all(p.parent == root / 'TestResults' and p.name.startswith('tower-balance-pass-')
                 and p.name.endswith('-study-20260929') for p in paths), 'Invalid native study paths')


def declaration(path, pin):
    path = Path(path)
    io.check(io.sha(path) == pin, 'Declaration changed')
    d = io.read(path)
    validate_layout(d)
    validate_paths(d['phases'], io.ROOT, d['version'])
    for member, expected in d['sourcePins'].items():
        io.check(io.sha(member) == expected, 'Frozen implementation/runtime input changed: ' + member)
    source = Path(d['source'])
    io.check(io.sha(source/'files.json') == d['sourceManifestSha256'] and
             io.sha(source/'cells.json') == d['cellsSha256'], 'Original source changed')
    io.authenticate(source)
    cells = io.read(source/'cells.json')
    io.check(len(cells) == d['familySize'] and all(c['scenario']['floorNumber'] == d['floor'] for c in cells),
             'Wrong source family')
    validate_candidate(d, source/'content')
    if d['version'] == NI_RESTORATION_VERSION:
        validate_ni_restoration_proposal(d, cells)
    elif d['version'] in KODOKU_ACCEPTANCE_VERSIONS:
        validate_fixed_proposal(d, cells)
    io.check(io.sha(d['initialHistory']) == d['initialHistorySha256'], 'Initial history changed')
    history = io.read(d['initialHistory'])
    io.check(len(set(history)) == len(history) == d['initialExclusions'], 'Wrong initial exclusions')
    return d, cells, set(history)


def validate_fixed_proposal(d, cells):
    io.check(d['version'] in KODOKU_ACCEPTANCE_VERSIONS, 'Unknown fixed acceptance version')
    midpoint = d['version'] == KODOKU_MIDPOINT_VERSION
    proposal_version = 'floor8-kodoku-midpoint-acceptance-proposal-v1' if midpoint else 'floor8-kodoku-fixed-acceptance-proposal-v1'
    io.check(io.sha(d['proposal']) == d['proposalSha256'], 'Fixed acceptance proposal changed')
    p = io.read(d['proposal'])
    io.check(p['version'] == proposal_version and p['status'] == 'ProposedNotDeclared' and
             p['newFights'] == p['newSeeds'] == 0 and p['diagnosticOutcomesUsedForAcceptance'] is False, 'Unallocated acceptance proposal required')
    phase = dict(batchCount=32, seedsPerBatch=16, samplesPerRecipe=512, fights=95232)
    io.check(p['screening'] == phase and p['confirmation'] == {**phase, 'onlyAfterCompleteScreenPass': True, 'independentSeeds': True},
             'Complete independent fixed phases required')
    io.check(p['acceptance'] == dict(method='approximate Bonferroni-Wilson', alpha=.05, familySize=186,
             minimumDistinctEligibleCompositions=2, minimumLowerBound=.1, maximumUpperBound=.5,
             minimumWinsPerQualifyingRecipe=76, maximumWinsEveryRecipe=214), 'Acceptance thresholds changed')
    io.check(p['limits'] == dict(maximumFreshFights=190464, maximumNewSeeds=1024, maximumPhases=2,
             maximumBatchesPerPhase=32, maximumApplicationReplays=5952, nativeSecondsLimit=840,
             ownerSecondsLimit=900, byteLimit=2*1024**3, retries=0, extensions=0, additionalCandidates=0, interimSelection=False),
             'Acceptance resource or stopping limits changed')
    io.check(p['candidatePlan'] == d['candidatePlan'] and p['candidateContentHashes'] == d['candidateContentHashes'] and
             Path(p['source']).resolve() == Path(d['source']).resolve() and p['sourceManifestSha256'] == d['sourceManifestSha256'] and
             p['sourceCellsSha256'] == d['cellsSha256'] and p['initialExclusions'] == d['initialExclusions'] == (924844 if midpoint else 924268),
             'Fixed source, candidate or exclusions changed')
    io.check(p['equipmentEligibility'] == d['equipmentEligibility'] == LIMITED_EQUIPMENT and
             (p['familySize'], p['actualCompositions'], p['eligibleRecipes']) == (186, 9, 128) and
             len(cells) == 186 and len({io.composition_key(c) for c in cells}) == 9 and
             sum(equipment_eligible(c, LIMITED_EQUIPMENT) for c in cells) == 128, 'Complete fixed family required')
    io.check(io.sha(p['precedingEvidence']) == p['precedingEvidenceSha256'], 'Diagnostic evidence changed')
    e = io.read(p['precedingEvidence'])
    io.check(e['status'] == 'Verified' and e['diagnosticOnly'] is True and e['usedForAcceptance'] is False and
             [v['eightItems']['wins'] for v in e['comparisons']] == ([22, 11, 12] if midpoint else [12, 12, 13]), 'Closed descriptive source required')
    if midpoint:
        io.check(p['proposedAggregateVersion'] == KODOKU_MIDPOINT_VERSION and
                 p['localApplication'] == dict(onlyAfterCompleteIndependentConfirmation=True, matchedInputs=95232,
                    fullReplays=5952, newSeeds=0, requireEveryConfirmationBatch=True, requireBoundApplicationPlan=True,
                    requireFreshBackendRegression=True, externalDeployment=False), 'Complete bound local application required')


def validate_ni_restoration_proposal(d, cells):
    io.check(d['version'] == NI_RESTORATION_VERSION and io.sha(d['proposal']) == d['proposalSha256'] ==
             '31d2fd371ad65b4c101a5f13d3f0bc73115efdbcdfcb459e5e5ae04db1b80c29', 'Exact frozen Ni acceptance proposal required')
    p = io.read(d['proposal'])
    io.check(p['candidatePlan'] == d['candidatePlan'] and p['candidateContentHashes'] == d['candidateContentHashes'] and
             Path(p['source']).resolve() == Path(d['source']).resolve() and p['sourceManifestSha256'] == d['sourceManifestSha256'] and
             p['sourceCellsSha256'] == d['cellsSha256'] and p['initialExclusions'] == d['initialExclusions'] == 926604,
             'Fixed source, candidate or exclusions changed')
    io.check(p['equipmentEligibility'] == d['equipmentEligibility'] == LIMITED_EQUIPMENT and d.get('gear') is None and
             len(cells) == 163 and len({io.composition_key(c) for c in cells}) == 5 and
             sum(equipment_eligible(c, LIMITED_EQUIPMENT) for c in cells) == 130, 'Complete nominated family required')
    io.check(io.sha(p['precedingEvidence']) == p['precedingEvidenceSha256'], 'Diagnostic evidence changed')
    e = io.read(p['precedingEvidence'])
    io.check(e['status'] == 'Verified' and e['diagnosticOnly'] is True and e['usedForAcceptance'] is False and
             e['selectedOffenseFactor'] == 1.0 and e['freshStudyFights'] == 15648, 'Complete diagnostic nomination required')


def owner(study):
    return Path(study).with_name(Path(study).name.replace('-study-', '-owner-'))


_fixed_audits = {}


def fixed_batch_audit(study):
    """Reuse a raw recount only after rehashing every archived byte on each call.

    The 32-batch contract repeatedly verifies completed prefixes. In-process
    memoization avoids reparsing identical reports; it never skips authentication
    or the current external-input checks in verify_batch.
    """
    study = Path(study)
    io.authenticate(study)
    key = (str(study.resolve()), io.sha(study/'files.json'))
    if key not in _fixed_audits:
        _fixed_audits[key] = io.audit(study)
    return copy.deepcopy(_fixed_audits[key])


def has_declared_owner_budget(value):
    """Monotonic deadline subtraction can round a 900-second allowance.

    Allow only sub-microsecond representation error; the process exit, timeout,
    child cleanup and native 840-second limit remain independently mandatory.
    """
    return type(value) in (int, float) and math.isfinite(value) and math.isclose(value, 900, rel_tol=0, abs_tol=1e-6)


def verify_batch(d, cells, study, phase, excluded, *, check_current_inputs=True):
    study = Path(study)
    audited = fixed_batch_audit(study) if d.get('version') in (*KODOKU_ACCEPTANCE_VERSIONS, NI_RESTORATION_VERSION) else io.audit(study)
    saved_owner = owner(study)
    io.check(audited == io.read(saved_owner/'independent-audit.json'), 'Native audit changed')
    io.check(io.read(study/'cells.json') == cells, 'Raw family differs')
    scope, q = io.read(study/'scope.json'), io.read(study/'request.json')
    io.check(scope['execution'] == d['runtime'] and scope['settings'] == d['settings'] and
             scope['contentHashes'] == d['candidateContentHashes'], 'Mixed runtime, settings or catalog')
    io.check(q['mode'] == phase and q['floor'] == d['floor'] and not q['searchSeeds'], 'Wrong batch mode')
    if d.get('version') in (*KODOKU_ACCEPTANCE_VERSIONS, NI_RESTORATION_VERSION):
        io.check(q.get('diagnosticVersion') is None and io.read(study/'result.json').get('diagnosticOnly') is False,
                 'Diagnostic panels cannot enter acceptance')
    kind = candidate_kind(d)
    expected_provenance = kind + '-candidate-provenance.json'
    provenance = [p for p in q['inputHashes'] if Path(p).name in (
        'ability-candidate-provenance.json', 'health-pressure-candidate-provenance.json', 'penetration-candidate-provenance.json', 'recovery-pressure-candidate-provenance.json')]
    if kind == 'unchanged':
        io.check(not provenance, 'Unchanged catalog cannot carry a candidate modification')
    elif kind == 'penetration':
        helper = ni_penetration_module()
        io.check(len(provenance) == 1 and Path(provenance[0]).name == expected_provenance and
                 io.read(provenance[0]) == helper.owner_plan(d), 'Mixed Ni penetration provenance')
        helper.verify(Path(d['source'])/'content', study/'content', d['candidatePlan'], d['floor'])
    else:
        io.check(len(provenance) == 1 and Path(provenance[0]).name == expected_provenance and
                 io.read(provenance[0])['plan'] == d['candidatePlan'], 'Mixed coefficient plan or candidate kind')
    if check_current_inputs:
        for member, expected in q['inputHashes'].items():
            io.check(io.sha(member) == expected, 'Changed native input: ' + member)
    panel = q['seeds']
    io.check(len(panel) == len(set(panel)) == d['samplesPerBatch'] and not excluded.intersection(panel),
             'Repeated, historical or incomplete seed panel')
    ledger = io.read(saved_owner/'seed-ledger.json')
    io.check(ledger['reserved'] == panel and set(io.read(saved_owner/'history.json')) == excluded,
             'Unexpected reservations or missing exclusions')
    process, native = io.read(saved_owner/'process.json'), io.read(study/'completion.json')
    io.check(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0 and
             has_declared_owner_budget(process['workAllowanceSeconds']) and native['retries'] == 0, 'Incomplete bounded batch')
    fights = len(cells) * d['samplesPerBatch']
    size = sum((study/p).stat().st_size for p in io.read(study/'files.json')) + (study/'files.json').stat().st_size
    io.check(audited['evaluationFights'] == native['attempts'] == native['completed'] == fights <= 20000 and
             native['seconds'] < 840 and size < 2*1024**3, 'Native resource/count limit exceeded')
    return dict(source=str(study), manifestSha256=io.sha(study/'files.json'),
                audit=str(saved_owner/'independent-audit.json'), auditSha256=io.sha(saved_owner/'independent-audit.json'),
                seeds=panel, fights=fights, seconds=native['seconds'], bytes=size), io.read(study/'result.json')['rows']


def validate_applied_parity(receipts, batches, aggregate_pin, runtime, version=VERSION):
    """Require every confirmed batch, with no duplicate or isolated-batch shortcut."""
    count = batch_count(version)
    io.check(len(receipts) == len(batches) == count and
             len({r['request']['source'] for r in receipts}) == count, 'Incomplete applied aggregate parity')
    matched = replays = 0
    floor_pins = set()
    for receipt, batch in zip(receipts, batches):
        request, result, completion, process = [receipt[n] for n in ('request', 'result', 'completion', 'process')]
        io.check(request['source'] == batch['source'] and
                 request['manifestPin'] == result['manifestPin'] == batch['manifestSha256'] and
                 request['audit'] == batch['audit'] and request['auditPin'] == batch['auditSha256'] and
                 request['aggregatePin'] == completion['aggregateSha256'] == aggregate_pin,
                 'Applied parity does not bind the complete confirmation')
        io.check(completion['status'] == 'Verified' and result['status'] == 'AggregateInputsAndReplaysVerified' and
                 completion['resultSha256'] == receipt['resultSha256'] and
                 completion['matchedInputs'] == result['matchedInputs'] == batch['fights'] and
                 completion['fullReplays'] == result['fullReplays'] == batch['fights'] // len(batch['seeds']) * (4 if version == NI_RESTORATION_VERSION else 1) and
                 completion['newSeeds'] == result['newSeeds'] == 0 and
                 result['execution'] == runtime and process['exitCode'] == 0 and
                 not process['timedOut'] and process['activeProcesses'] == 0,
                 'Applied aggregate parity incomplete')
        matched += result['matchedInputs']
        replays += result['fullReplays']
        floor_pins.add(completion['floorFileSha256'])
    io.check(len(floor_pins) == 1, 'Mixed applied Tower catalogs')
    return dict(matchedInputs=matched, fullReplays=replays, floorFileSha256=floor_pins.pop())


def validate_applied_completion(completion, summary, content_hashes, exclusions, *, tower_changed=False, summons_changed=False):
    io.check(completion['status'] == 'AppliedAndVerified' and completion['newSeeds'] == 0 and
             completion['matchedInputs'] == summary['matchedInputs'] and completion['fullReplays'] == summary['fullReplays'] and
             completion['afterAbilitiesSha256'] == content_hashes['combat/abilities.json'] and
             completion['finalExclusions'] == exclusions, 'Aggregate catalog was not fully applied')
    if tower_changed:
        io.check(completion.get('afterTowerSha256') == content_hashes['world-tower/tower-floors.json'],
                 'Joint aggregate Tower catalog was not fully applied')
    if summons_changed:
        io.check(completion.get('afterSummonsSha256') == content_hashes['combat/summons.json'],
                 'Shared-penetration summons catalog was not fully applied')


def applied_catalog(plan):
    """Recount a previously applied aggregate using its immutable archives and receipts.

    Pre-application live paths and helper source files may have since changed. Their
    old hashes remain untouched in the authenticated requests. This path requires
    all eight original archives, both passing panels and all four application
    receipts; it cannot admit a new candidate or allocate another batch.
    """
    accepted = plan['acceptedAggregate']
    versions = {'applied-tower-aggregate-v1': VERSION, 'applied-tower-recovery-aggregate-v1': RECOVERY_VERSION,
                'applied-tower-miasma-resource-aggregate-v1': MIASMA_RESOURCE_VERSION,
                'applied-tower-shared-penetration-aggregate-v1': SHARED_PENETRATION_VERSION,
                'applied-tower-kodoku-fixed-aggregate-v1': KODOKU_FIXED_VERSION,
                'applied-tower-kodoku-midpoint-aggregate-v1': KODOKU_MIDPOINT_VERSION,
                'applied-tower-ni-penetration-aggregate-v1': NI_PENETRATION_VERSION,
                'applied-tower-ni-restoration-aggregate-v1': NI_RESTORATION_VERSION}
    io.check(accepted['version'] in versions, 'Unknown applied aggregate version')
    version = versions[accepted['version']]; count = batch_count(version)
    declaration_path = Path(accepted['declaration'])
    phase_paths = {phase: Path(accepted[phase]) for phase in ('screen', 'confirm')}
    owners = [Path(p) for p in accepted['parityOwners']]
    completion_path = Path(accepted['completion'])
    required = {str(p) for p in [declaration_path, *phase_paths.values(), completion_path]}
    required.update(str(owner/name) for owner in owners for name in
                    ('request.json', 'result.json', 'process.json', 'completion.json'))
    io.check(len(set(owners)) == count and set(plan['receiptPins']) == required,
             'Incomplete accepted aggregate pins')
    for path, pin in plan['receiptPins'].items():
        io.check(io.sha(path) == pin, 'Accepted aggregate receipt changed')
    d = io.read(declaration_path)
    io.check(d['version'] == version, 'Applied declaration version differs')
    validate_layout(d)
    validate_paths(d['phases'], io.ROOT, d['version'])
    source = Path(d['source'])
    io.check(io.sha(source/'files.json') == d['sourceManifestSha256'] and
             io.sha(source/'cells.json') == d['cellsSha256'], 'Applied source changed')
    io.authenticate(source)
    cells = io.read(source/'cells.json')
    io.check(len(cells) == d['familySize'] and all(c['scenario']['floorNumber'] == d['floor'] for c in cells),
             'Wrong applied family')
    validate_candidate(d, source/'content')
    io.check(io.sha(d['initialHistory']) == d['initialHistorySha256'], 'Applied initial history changed')
    history = io.read(d['initialHistory'])
    excluded = set(history)
    io.check(len(excluded) == len(history) == d['initialExclusions'], 'Wrong applied initial exclusions')
    phases = {}
    for phase in ('screen', 'confirm'):
        records, rows = [], []
        for study in d['phases'][phase]:
            record, batch_rows = verify_batch(d, cells, study, phase, excluded, check_current_inputs=False)
            records.append(record); rows.append(batch_rows); excluded.update(record['seeds'])
        assessment = assess(cells, rows, count, d['samplesPerBatch'], d['gear'], d.get('equipmentEligibility'))
        actual = dict(version=d['version'], status='Verified', phase=phase, declaration=str(declaration_path.resolve()),
                      declarationSha256=io.sha(declaration_path), assessment=assessment, batches=records,
                      evaluationFights=sum(b['fights'] for b in records),
                      screeningManifests=[b['manifestSha256'] for b in phases['screen']['batches']] if phase == 'confirm' else [])
        io.check(actual == io.read(phase_paths[phase]) and assessment['verdict'] == 'Pass',
                 'Complete passing applied panels required')
        phases[phase] = actual
    receipts = []
    for owner in owners:
        receipt = {n: io.read(owner/(n+'.json')) for n in ('request', 'result', 'completion', 'process')}
        io.check(Path(receipt['request']['aggregate']).resolve() == phase_paths['confirm'].resolve(),
                 'Applied parity aggregate path differs')
        receipt['resultSha256'] = io.sha(owner/'result.json')
        receipts.append(receipt)
    summary = validate_applied_parity(receipts, phases['confirm']['batches'], io.sha(phase_paths['confirm']), d['runtime'], d['version'])
    completion = io.read(completion_path)
    validate_applied_completion(completion, summary, d['candidateContentHashes'], len(excluded),
                                tower_changed=candidate_kind(d) in ('penetration', 'health-pressure', 'recovery-pressure') or version in (NI_RESTORATION_VERSION, SHARED_PENETRATION_VERSION, *KODOKU_ACCEPTANCE_VERSIONS),
                                summons_changed=version in (SHARED_PENETRATION_VERSION, *KODOKU_ACCEPTANCE_VERSIONS))
    return Path(phases['confirm']['batches'][0]['source']), summary


def audit_phase(path, pin, phase):
    d, cells, excluded = declaration(path, pin)
    io.check(phase in ('screen', 'confirm'), 'Unknown aggregate phase')
    screening = None
    if phase == 'confirm':
        screening = audit_phase(path, pin, 'screen')
        io.check(screening['assessment']['verdict'] == 'Pass', 'Passing complete screening required')
        excluded.update(seed for batch in screening['batches'] for seed in batch['seeds'])
    records, batches = [], []
    for study in d['phases'][phase]:
        record, rows = verify_batch(d, cells, study, phase, excluded)
        records.append(record)
        batches.append(rows)
        excluded.update(record['seeds'])
    assessment = assess(cells, batches, d['batchCount'], d['samplesPerBatch'], d['gear'], d.get('equipmentEligibility'))
    return dict(version=d['version'], status='Verified', phase=phase, declaration=str(Path(path).resolve()),
                declarationSha256=pin, assessment=assessment, batches=records,
                evaluationFights=sum(b['fights'] for b in records),
                screeningManifests=[b['manifestSha256'] for b in screening['batches']] if screening else [])


def resource_admission(completed, samples, family, size):
    fights = samples * family
    projected_seconds = 2 * completed['seconds'] * fights / completed['completed']
    projected_bytes = 2 * size * fights / completed['completed']
    io.check(fights <= 20000 and projected_seconds < 672 and projected_bytes < .8*2*1024**3,
             'Measured resource admission failed')
    return dict(fights=fights, projectedSeconds=projected_seconds, projectedBytes=projected_bytes,
                nativeSecondsLimit=840, ownerSecondsLimit=900, byteLimit=2*1024**3)


def admit_batch(path, pin, phase, index):
    """Check the entire completed prefix before allocating the next fresh batch."""
    d, cells, excluded = declaration(path, pin)
    io.check(phase in ('screen', 'confirm') and type(index) is int and 0 <= index < d['batchCount'], 'Unknown batch')
    for member, expected in d['liveCatalogPins'].items():
        io.check(io.sha(member) == expected, 'Live catalog changed before admission')
    if phase == 'confirm':
        screening = audit_phase(path, pin, 'screen')
        io.check(screening['assessment']['verdict'] == 'Pass', 'Passing complete screening required')
        excluded.update(seed for batch in screening['batches'] for seed in batch['seeds'])
    else:
        io.check(all(not Path(p).exists() and not owner(p).exists() for p in d['phases']['confirm']),
                 'Confirmation allocated before full screening')
    for study in d['phases'][phase][:index]:
        record, _ = verify_batch(d, cells, study, phase, excluded)
        excluded.update(record['seeds'])
    io.check(all(not Path(p).exists() and not owner(p).exists() for p in d['phases'][phase][index:]),
             'Fresh predeclared remaining paths required; no retries')
    current, _ = io.history()
    io.check(current == excluded, 'Seed history changed outside the declared batch prefix')
    measured = Path(d['phases'][phase][index-1] if index else d['resourceReference'])
    if index == 0:
        io.check(io.sha(measured/'files.json') == d['resourceReferenceManifestSha256'], 'Resource reference changed')
    io.authenticate(measured)
    size = sum((measured/p).stat().st_size for p in io.read(measured/'files.json')) + (measured/'files.json').stat().st_size
    return resource_admission(io.read(measured/'completion.json'), d['samplesPerBatch'], len(cells), size)


def application_batch(evidence, evidence_pin, study):
    """Authenticate whole independent confirmation; no synthetic passing batch audits."""
    evidence = Path(evidence)
    io.check(io.sha(evidence) == evidence_pin, 'Aggregate evidence changed')
    saved = io.read(evidence)
    io.check(saved['phase'] == 'confirm' and saved['status'] == 'Verified' and
             saved['assessment']['verdict'] == 'Pass', 'Passing aggregate confirmation required')
    actual = audit_phase(saved['declaration'], saved['declarationSha256'], 'confirm')
    io.check(actual == saved, 'Aggregate evidence differs from reconstructed outcomes')
    members = [b for b in saved['batches'] if Path(b['source']).resolve() == Path(study).resolve()]
    io.check(len(members) == 1, 'Application source is not a declared confirmation batch')
    return members[0], saved
