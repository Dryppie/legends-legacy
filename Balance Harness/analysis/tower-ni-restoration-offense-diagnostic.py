"""Frozen six-panel Ni calibration. These panels cannot satisfy acceptance."""
import importlib.util
from pathlib import Path

s = importlib.util.spec_from_file_location('ni_restoration_offense_candidate', Path(__file__).with_name('tower-ni-restoration-offense.py'))
n = importlib.util.module_from_spec(s); s.loader.exec_module(n); h = n.h
DIAGNOSTIC = 'floor9-restoration-offense-diagnostic-v1'
PROVENANCE = 'ni-restoration-offense-diagnostic-provenance.json'
PROPOSAL_SHA256 = '4ec5f4d30fee27a851ce7781d92b901cfc7108f70703f475152aa715c1b769d6'
LIMITS = dict(diagnosticOnly=True, usedForAcceptance=False, batches=6, seedsPerBatch=16,
              maximumFights=15648, maximumNewSeeds=96, nativeSecondsLimit=840,
              ownerSecondsLimit=900, byteLimit=2*1024**3, retries=0, extensions=0,
              confirmation=False, application=False)
SELECTION = dict(maximumWinsEveryRecipe=12, minimumWinsEligibleRecipe=6,
                 minimumDistinctEligibleCompositions=2, tieBreak='lowestOffenseFactor')


def validate_panel(mode, floor, samples, version, family):
    h.check(version == DIAGNOSTIC and mode == 'screen' and floor == 9 and samples == 16 and family == 163,
            'Only the complete non-acceptance Ni offense panel is supported')


def validate_contract(d, cells):
    h.check(d['version'] == DIAGNOSTIC and d['limits'] == LIMITS and d['floor'] == 9 and d['familySize'] == 163,
            'Diagnostic limits changed')
    h.check(len(cells) == len({c['id'] for c in cells}) == 163 and d['cellsSha256'] == h.sha(Path(d['source'])/'cells.json'), 'Declared family changed')
    h.check(d['initialExclusions'] == 926508 and d['eligibleRecipes'] == 130 and d['actualCompositions'] == 5, 'Diagnostic context changed')
    h.check(len(d['studies']) == len(set(d['studies'])) == 6 and all(Path(p).is_absolute() for p in d['studies']), 'Six distinct absolute studies required')
    h.check([v['offenseFactor'] for v in d['variants']] == list(n.OFFENSE), 'Ordered offense grid changed')
    h.check(d['selection'] == SELECTION, 'Selection margins changed')
    for v in d['variants']:
        n.validate_plan(v['candidatePlan'], 9)
        h.check(v['candidatePlan']['offenseFactor'] == v['offenseFactor'], 'Variant plan mismatch')


def read_contract(io, path, pin):
    h.check(pin is not None and h.sha(path) == pin, 'Diagnostic declaration pin required')
    d = h.read(path); source = Path(d['source']); cells = h.read(source/'cells.json')
    validate_contract(d, cells)
    for group in ('sourcePins', 'liveCatalogPins', 'ledgerPins'):
        for member, expected in d[group].items(): h.check(h.sha(member) == expected, 'Diagnostic input changed: '+member)
    io.authenticate(source)
    q = h.read(source/'request.json')
    h.check(q['mode'] == 'prepare' and not q['seeds'] and not q['searchSeeds'] and
            h.read(source/'completion.json')['status'] == 'Complete', 'Seed-free original-catalog prepared source required')
    h.check(not any(Path(p).name.endswith('candidate-provenance.json') for p in q['inputHashes']), 'Candidate chaining forbidden')
    proposal = Path(d['proposal']); p = h.read(proposal)
    h.check(h.sha(proposal) == d['proposalSha256'] == PROPOSAL_SHA256, 'Frozen proposal differs')
    h.check(source.resolve() == Path(p['source']).resolve() and h.sha(source/'files.json') == p['sourceManifestSha256'] and
            h.sha(source/'cells.json') == p['sourceCellsSha256'], 'Original source differs')
    h.check(h.sha(p['precedingEvidence']) == p['precedingEvidenceSha256'], 'Preceding evidence changed')
    for actual, frozen in zip(d['variants'], p['variants'], strict=True):
        h.check(actual['candidatePlan'] == frozen['candidatePlan'], 'Frozen candidate changed')
        n.expected(source/'content', actual['candidatePlan'], 9)
    return d, cells


def batch_variant(d, index):
    h.check(type(index) is int and 0 <= index < 6, 'Undeclared batch')
    return d['variants'][index//2]


def audit_request(io, output, q):
    paths = [Path(p) for p in q['inputHashes'] if Path(p).name == PROVENANCE]
    h.check(q.get('diagnosticVersion') == DIAGNOSTIC and len(paths) == 1, 'Diagnostic marker/provenance mismatch')
    provenance = paths[0]; h.check(h.sha(provenance) == q['inputHashes'][str(provenance)], 'Provenance changed')
    p = h.read(provenance); d,cells = read_contract(io, Path(p['declaration']), p['declarationSha256'])
    index = p['batchIndex']; v = batch_variant(d, index)
    h.check(Path(d['studies'][index]) == output, 'Undeclared study path')
    validate_panel(q['mode'], q['floor'], len(q['seeds']), q['diagnosticVersion'], len(cells))
    h.check(q['maximumFights'] == 2608 and not q['searchSeeds'] and q.get('nativeSeconds',840) == 840, 'Diagnostic exceeded limits')
    h.check(h.read(output/'cells.json') == cells, 'Diagnostic changed controls')
    plans = [Path(p) for p in q['inputHashes'] if Path(p).name == 'ability-candidate-provenance.json']
    h.check(len(plans) == 1 and h.read(plans[0])['plan'] == v['candidatePlan'], 'Wrong candidate for batch')
    result,scope = h.read(output/'result.json'), h.read(output/'scope.json')
    h.check(result.get('diagnosticOnly') is True and result.get('usedForAcceptance') is False, 'Non-acceptance marker required')
    h.check(scope['execution'] == d['execution'] and scope['settings'] == d['settings'] and scope['contentHashes'] == v['candidateContentHashes'], 'Diagnostic scope differs')
    n.verify(Path(d['source'])/'content', output/'content', v['candidatePlan'], 9)


def admit_batch(io, path, pin, index, source, output, mode, floor, samples, candidate, modifications, native_seconds):
    h.check(candidate is not None and not any(modifications) and native_seconds == 840, 'Unmixed explicit diagnostic candidate required')
    d,cells = read_contract(io, path, pin); v = batch_variant(d, index)
    h.check(h.read(candidate) == v['candidatePlan'], 'Wrong candidate for frozen batch')
    validate_panel(mode, floor, samples, d['version'], len(cells))
    h.check(source.resolve() == Path(d['source']).resolve() and output == Path(d['studies'][index]), 'Unfrozen source or path')
    h.check(h.sha(d['initialHistory']) == d['initialHistorySha256'], 'History changed')
    expected = set(h.read(d['initialHistory'])); h.check(len(expected) == 926508, 'Initial exclusions differ')
    for previous in d['studies'][:index]:
        study = Path(previous); owner = study.with_name(study.name.replace('-study-','-owner-'))
        h.check(io.audit(study) == h.read(owner/'independent-audit.json'), 'Preceding diagnostic incomplete')
        q = h.read(study/'request.json'); audit_request(io, study, q)
        h.check(len(q['seeds']) == len(set(q['seeds'])) == 16 and not expected.intersection(q['seeds']), 'Repeated or incomplete seeds')
        expected.update(q['seeds'])
    for pending in d['studies'][index:]:
        study = Path(pending); h.check(not study.exists() and not study.with_name(study.name.replace('-study-','-owner-')).exists(), 'No retry or replacement panel')
    h.check(io.history()[0] == expected, 'Unexpected allocation between panels')
    return d


def select(settings):
    """Descriptive nomination only; requires the complete three-setting family."""
    h.check(len(settings) == 3 and [s['offenseFactor'] for s in settings] == list(n.OFFENSE), 'Complete ordered grid required')
    qualifying = []
    ids = None
    for setting in settings:
        rows = setting['rows']
        h.check(len(rows) == len({r['id'] for r in rows}) == 163 and all(type(r['wins']) is int and 0 <= r['wins'] <= 32 and r['samples'] == 32 for r in rows), 'Complete raw counts required')
        current = [(r['id'],r['composition'],r['eligible']) for r in rows]
        h.check(ids is None or current == ids, 'Recipes or eligibility changed between settings'); ids = current
        h.check(sum(r['eligible'] for r in rows) == 130 and len({r['composition'] for r in rows}) == 5, 'Complete family context required')
        if max(r['wins'] for r in rows) <= 12 and len({r['composition'] for r in rows if r['eligible'] and r['wins'] >= 6}) >= 2:
            qualifying.append(setting['offenseFactor'])
    return min(qualifying) if qualifying else None
