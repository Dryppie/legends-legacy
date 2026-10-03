"""The nominated floor-thirteen Power/penetration setting and complete acceptance scope."""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('floor13_penetration', Path(__file__).with_name('tower-ni-penetration.py'))
base = importlib.util.module_from_spec(spec); spec.loader.exec_module(base)
h = base.h
VERSION = 'tower-floor13-restoration-acceptance-v1'
TOWER, SUMMONS, ABILITIES, HASHES = base.TOWER, base.SUMMONS, base.ABILITIES, base.HASHES
VALUES = dict(version=VERSION, floor=13, healthFactor=1, offenseFactor=.675, penetrationFactor=40,
              originalOffense=12.83625, offense=8.66446875, originalPenetration=1, penetration=40)
PROPOSAL_PIN = 'd512f833244e04b2528d8cda391bc0aa7aac84b7e885c331267919f786fb308f'


def validate_plan(plan, floor):
    h.check(type(floor) is int and floor == 13 and set(plan) == set(VALUES) | set(HASHES), 'Exact floor-thirteen acceptance plan required')
    h.check({k: plan[k] for k in VALUES} == VALUES and type(plan['floor']) is int and
            all(type(plan[k]) in (int, float) for k in VALUES if k not in ('version', 'floor')), 'Only the nominated Nhalia setting is supported')
    for key in HASHES:
        h.check(type(plan[key]) is str and base.re.fullmatch('[0-9a-f]{64}', plan[key]), 'Source SHA-256 required')


def expected(api, plan, floor):
    validate_plan(plan, floor)
    for key, relative in HASHES.items(): h.check(h.sha(api/relative) == plan[key], 'Candidate source changed')
    tower = h.read(api/TOWER); guardian = h.unique(tower['floors'], 'floorNumber', 13); scaling = guardian['guardianScaling']
    h.check(guardian['guardianAbilityProfileId'] == 'monster.nhalia,_the_moondrowned' and scaling['health'] == 13.180078125
            and scaling['offense'] == 12.83625 and scaling['penetration'] == 1, 'Original Nhalia scaling changed')
    scaling['offense'] = 8.66446875; scaling['penetration'] = 40.0
    return tower


def materialize(api, isolated, plan, floor):
    h.check(api.resolve() != isolated.resolve(), 'Candidate must be isolated')
    intended = expected(api, plan, floor)
    for key, relative in HASHES.items(): h.check(h.sha(isolated/relative) == plan[key], 'Isolated source changed')
    (isolated/TOWER).write_text(base.json.dumps(intended, indent=2)+'\n', encoding='utf-8')
    return verify(api, isolated, plan, floor)


def verify(api, candidate, plan, floor):
    intended = expected(api, plan, floor)
    files = {p.relative_to(api) for p in (api/'Data').rglob('*.json')}
    h.check(files == {p.relative_to(candidate) for p in (candidate/'Data').rglob('*.json')}, 'Catalog set changed')
    for relative in files:
        if relative == TOWER: h.check(h.read(candidate/relative) == intended, 'Undeclared Tower delta')
        else: h.check(h.sha(api/relative) == h.sha(candidate/relative), 'Unrelated catalog changed: '+str(relative))
    h.check(h.sha(api/'appsettings.json') == h.sha(candidate/'appsettings.json'), 'Settings changed')
    return dict(version=VERSION, floor=floor, changes=2, catalogs=len(files), candidateTowerSha256=h.sha(candidate/TOWER))


def owner_plan(declaration):
    validate_plan(declaration['candidatePlan'], declaration['floor'])
    return dict(source=declaration['source'], sourceManifestSha256=declaration['sourceManifestSha256'], floor=13, offenseFactor=.675, penetrationFactor=40)


def validate_proposal(aggregate, declaration, cells):
    io = aggregate.io; d = declaration
    io.check(io.sha(d['proposal']) == d['proposalSha256'] == PROPOSAL_PIN, 'Exact frozen floor-thirteen acceptance proposal required')
    p = io.read(d['proposal'])
    io.check(p['version'] == 'floor13-restoration-fixed-acceptance-proposal-v1' and p['status'] == 'ProposedNotAllocated'
             and p['allocatedFights'] == p['allocatedSeeds'] == 0 and not p['usedForAcceptance'], 'Unallocated formal proposal required')
    io.check(p['candidatePlan'] == owner_plan(d) and p['source'] == d['source'] and p['sourceManifestSha256'] == d['sourceManifestSha256']
             and p['cellsSha256'] == d['cellsSha256'] and p['initialExclusions'] == d['initialExclusions'] == 930332, 'Frozen source/settings changed')
    io.check((p['batchCount'],p['samplesPerBatch'],p['samplesPerPhase'],p['maximumFreshFights'],p['maximumNewReservations'],p['retries'])
             == (16,32,512,273408,1024,0), 'Complete independent phases required')
    io.check(p['acceptance'] == dict(method='approximate Bonferroni-Wilson',alpha=.05,familySize=267,minimumDistinctCompositions=2,
             minimumLowerBound=.1,maximumUpperBound=.5,minimumWins=77,maximumWins=213,equipmentEligibility=aggregate.LIMITED_EQUIPMENT), 'Acceptance gates changed')
    io.check(d.get('gear') is None and d['equipmentEligibility'] == aggregate.LIMITED_EQUIPMENT and len(cells) == 267
             and len({io.composition_key(c) for c in cells}) == 9
             and sum(aggregate.equipment_eligible(c, aggregate.LIMITED_EQUIPMENT) for c in cells) == 155, 'Complete fixed family and equipment limits required')
    io.check(io.sha(Path(d['source'])/'cells.json') == 'eded9996018c9113484a923d63a6a73ed070e8a239a36edddd34ee1cf8e7d019'
             and cells == io.read(Path(d['source'])/'cells.json'), 'Every exact raw recipe and original order required')
    for path,pin in p['sourcePins'].items(): io.check(io.sha(path) == pin, 'Nomination evidence changed')
