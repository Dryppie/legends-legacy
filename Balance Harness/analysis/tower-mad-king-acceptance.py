"""The nominated floor-ten Power/penetration setting and complete acceptance scope."""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('mad_king_penetration', Path(__file__).with_name('tower-ni-penetration.py'))
base = importlib.util.module_from_spec(spec); spec.loader.exec_module(base)
h = base.h
VERSION = 'tower-mad-king-penetration-acceptance-v1'
TOWER, SUMMONS, ABILITIES, HASHES = base.TOWER, base.SUMMONS, base.ABILITIES, base.HASHES
VALUES = dict(version=VERSION, floor=10, healthFactor=1, offenseFactor=.5, penetrationFactor=40,
              originalOffense=7.13, offense=3.565, originalPenetration=1, penetration=40)
PROPOSAL_PIN = '0c588e56db75fb38416b66f5e021ad89d18096d2711d0889957abd99c7cc2bd1'


def validate_plan(plan, floor):
    h.check(type(floor) is int and floor == 10 and set(plan) == set(VALUES) | set(HASHES), 'Exact floor-ten acceptance plan required')
    h.check({k: plan[k] for k in VALUES} == VALUES and type(plan['floor']) is int and
            all(type(plan[k]) in (int, float) for k in VALUES if k not in ('version', 'floor')), 'Only the nominated Mad King setting is supported')
    for key in HASHES:
        h.check(type(plan[key]) is str and base.re.fullmatch('[0-9a-f]{64}', plan[key]), 'Source SHA-256 required')


def expected(api, plan, floor):
    validate_plan(plan, floor)
    for key, relative in HASHES.items(): h.check(h.sha(api/relative) == plan[key], 'Candidate source changed')
    tower = h.read(api/TOWER); guardian = h.unique(tower['floors'], 'floorNumber', 10); scaling = guardian['guardianScaling']
    h.check(guardian['guardianAbilityProfileId'] == 'monster.the_mad_king' and scaling['health'] == 12.8371
            and scaling['offense'] == 7.13 and scaling['penetration'] == 1, 'Original Mad King scaling changed')
    scaling['offense'] = 3.565; scaling['penetration'] = 40.0
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
    return dict(source=declaration['source'], sourceManifestSha256=declaration['sourceManifestSha256'], floor=10, offenseFactor=.5, penetrationFactor=40)


def validate_proposal(aggregate, declaration, cells):
    io = aggregate.io; d = declaration
    io.check(io.sha(d['proposal']) == d['proposalSha256'] == PROPOSAL_PIN, 'Exact frozen floor-ten acceptance proposal required')
    p = io.read(d['proposal'])
    io.check(p['version'] == 'floor10-fixed-penetration-acceptance-proposal-v1' and p['status'] == 'ProposedNotAllocated'
             and p['allocatedFights'] == p['allocatedSeeds'] == 0 and not p['usedForAcceptance'], 'Unallocated formal proposal required')
    io.check(p['candidatePlan'] == owner_plan(d) and p['source'] == d['source'] and p['sourceManifestSha256'] == d['sourceManifestSha256']
             and p['cellsSha256'] == d['cellsSha256'] and p['initialExclusions'] == d['initialExclusions'] == 927676, 'Frozen source/settings changed')
    io.check((p['batchCount'],p['samplesPerBatch'],p['samplesPerPhase'],p['maximumFreshFights'],p['maximumNewReservations'],p['retries'])
             == (32,16,512,284672,1024,0), 'Complete independent phases required')
    io.check(p['acceptance'] == dict(method='approximate Bonferroni-Wilson',alpha=.05,familySize=278,minimumDistinctCompositions=2,
             minimumLowerBound=.1,maximumUpperBound=.5,minimumWins=77,maximumWins=213,equipmentEligibility=aggregate.LIMITED_EQUIPMENT), 'Acceptance gates changed')
    io.check(d.get('gear') is None and d['equipmentEligibility'] == aggregate.LIMITED_EQUIPMENT and len(cells) == 278
             and len({io.composition_key(c) for c in cells}) == 5
             and sum(aggregate.equipment_eligible(c, aggregate.LIMITED_EQUIPMENT) for c in cells) == 245, 'Complete fixed family and equipment limits required')
    for path,pin in p['sourcePins'].items(): io.check(io.sha(path) == pin, 'Nomination evidence changed')
