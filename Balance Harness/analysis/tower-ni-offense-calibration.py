"""Fixed floor-nine scalar calibration. Descriptive evidence, never acceptance.

Use the existing native fixed-family owner; retain all recipes and allocate
three disjoint panels before making a decision. No combat runs on import.
"""
import copy
import importlib.util
from pathlib import Path

s = importlib.util.spec_from_file_location('ni_offense_io', Path(__file__).with_name('run-tower-balance-pass.py'))
io = importlib.util.module_from_spec(s)
s.loader.exec_module(io)
VERSION = 'tower-ni-offense-calibration-v1'
FACTORS = (1.25, 1.5, 2.0)
SAMPLES = 32
FAMILY = 148
INITIAL_EXCLUSIONS = 926252
TOWER = 'world-tower/tower-floors.json'


def catalog_hashes(content):
    return {p.relative_to(content/'Data').as_posix(): io.sha(p) for p in (content/'Data').rglob('*.json')}


def expected_tower(source, factor):
    io.check(type(factor) in (float, int) and factor in FACTORS, 'Only the three frozen offense factors are supported')
    original = io.read(source/'Data'/TOWER)
    expected = copy.deepcopy(original)
    floors = [f for f in expected['floors'] if f['floorNumber'] == 9]
    io.check(len(floors) == 1, 'Exactly one floor-nine guardian required')
    scaling = floors[0]['guardianScaling']
    io.check(scaling['offense'] == 4.7036132812 and scaling['health'] == 2.970703125, 'Original Ni scaling changed')
    scaling['offense'] = round(scaling['offense']*factor, 10)
    return expected


def verify_candidate(source, candidate, factor):
    io.check(source.resolve() != candidate.resolve(), 'An isolated candidate is required')
    expected = expected_tower(source, factor)
    io.check(io.read(candidate/'Data'/TOWER) == expected, 'Candidate changed more than Ni offense')
    before, after = catalog_hashes(source), catalog_hashes(candidate)
    io.check(before.keys() == after.keys() and all(after[p] == h for p, h in before.items() if p != TOWER),
             'Candidate changed another catalog')
    io.check(io.sha(source/'appsettings.json') == io.sha(candidate/'appsettings.json'), 'Candidate changed settings')
    return after


def eligible(cell):
    slots = [m['partySlot'] for m in cell['scenario']['party'] for e in m['build']['equipment'] if '.spec.' in e['definitionId']]
    return len(slots) <= 8 and len(set(slots)) <= 2


def validate_family(cells):
    io.check(len(cells) == len({c['id'] for c in cells}) == FAMILY, 'Complete unique 148-recipe family required')
    io.check(all(c['scenario']['floorNumber'] == 9 and not c['scenario']['seeds'] for c in cells), 'Wrong floor or seeded recipe')
    io.check(len({io.composition_key(c) for c in cells}) == 5 and sum(eligible(c) for c in cells) == 115,
             'Actual compositions or limited-equipment eligibility changed')


def choose(cells, panels):
    """Conservative selection for a later fresh study, not a passing verdict.

    Require <=12/32 everywhere and >=6/32 on two distinct eligible builds.
    These empirical margins intentionally do not replace simultaneous gates.
    """
    io.check(len(panels) == 3 and [p['factor'] for p in panels] == list(FACTORS), 'Complete ordered grid required')
    ids = [c['id'] for c in cells]
    result = []
    for panel in panels:
        rows = panel['rows']
        io.check([r['id'] for r in rows] == ids, 'Every exact recipe required in original order')
        io.check(all(type(r['wins']) is int and 0 <= r['wins'] <= SAMPLES and r['samples'] == SAMPLES for r in rows),
                 'Invalid diagnostic count')
        viable = {io.composition_key(c) for c, r in zip(cells, rows) if eligible(c) and r['wins'] >= 6}
        maximum = max(r['wins'] for r in rows)
        result.append(dict(factor=panel['factor'], maximumWins=maximum, promisingCompositions=len(viable),
                           eligibleForFreshAcceptance=maximum <= 12 and len(viable) >= 2))
    selected = next((r['factor'] for r in result if r['eligibleForFreshAcceptance']), None)
    return dict(status='CandidateForFreshAcceptance' if selected is not None else 'NoCandidateSelected',
                selectedFactor=selected, diagnosticOnly=True, usedForAcceptance=False, settings=result)


def validate_declaration(d, cells):
    validate_family(cells)
    io.check(d['version'] == VERSION and d['factors'] == list(FACTORS) and d['samples'] == SAMPLES and
             d['initialExclusions'] == INITIAL_EXCLUSIONS and d['maximumFights'] == 14208 and
             d['maximumNewSeeds'] == 96 and d['diagnosticOnly'] is True and d['usedForAcceptance'] is False,
             'Frozen diagnostic scope changed')
    io.check(d['selection'] == {'maximumWinsEveryRecipe': 12, 'minimumWinsEligibleRecipe': 6,
             'minimumDistinctEligibleCompositions': 2, 'tieBreak': 'lowestOffenseFactor'}, 'Selection changed')
    io.check(len(d['studies']) == len(set(d['studies'])) == 3 and all(Path(p).is_absolute() for p in d['studies']),
             'Three distinct absolute study paths required')
    io.check(d['limits'] == dict(nativeSeconds=840, ownerSeconds=900, bytes=2*1024**3, retries=0,
             extensions=0, additionalCandidates=0, interimSelection=False, confirmationFights=0, applicationFights=0),
             'Resource or stopping limits changed')


def admit(declaration, pin, index):
    io.check(io.sha(declaration) == pin, 'Declaration changed')
    d = io.read(declaration)
    source = Path(d['source']); cells = io.read(source/'cells.json')
    validate_declaration(d, cells)
    io.check(type(index) is int and 0 <= index < 3, 'Undeclared batch')
    for group in ('sourcePins', 'liveCatalogPins', 'ledgerPins'):
        for path, h in d[group].items(): io.check(io.sha(path) == h, 'Pinned input changed: '+path)
    io.check(io.sha(source/'files.json') == d['sourceManifestSha256'], 'Source changed')
    io.authenticate(source)
    excluded = set(io.read(d['initialHistory']))
    io.check(io.sha(d['initialHistory']) == d['initialHistorySha256'] and len(excluded) == INITIAL_EXCLUSIONS, 'Initial history changed')
    for i, path in enumerate(d['studies']):
        study = Path(path); owner = study.with_name(study.name.replace('-study-', '-owner-'))
        if i >= index:
            io.check(not study.exists() and not owner.exists(), 'Fresh remaining paths required; no retries')
            continue
        io.check(io.audit(study) == io.read(owner/'independent-audit.json'), 'Prior native audit changed')
        request = io.read(study/'request.json')
        panel = request['seeds']
        io.check(len(panel) == len(set(panel)) == SAMPLES and not excluded.intersection(panel), 'Panel overlaps history')
        io.check(io.read(study/'cells.json') == cells and request['mode'] == 'screen', 'Wrong family or mode')
        verify_candidate(source/'content', study/'content', FACTORS[i])
        excluded.update(panel)
    current, _ = io.history()
    io.check(current == excluded, 'Seed history changed outside the declared prefix')
    return d
