"""Frozen Ni offense/penetration trial; health, kit and summons stay unchanged."""
import importlib.util
import json
from pathlib import Path
import re

spec = importlib.util.spec_from_file_location('ni_penetration_helpers', Path(__file__).with_name('tower-kodoku-shared-penetration.py'))
h = importlib.util.module_from_spec(spec); spec.loader.exec_module(h)
VERSION = 'tower-ni-penetration-v1'
TOWER, SUMMONS, ABILITIES, HASHES = h.TOWER, h.SUMMONS, h.ABILITIES, h.HASHES
VALUES = dict(version=VERSION, floor=9, healthFactor=1, offenseFactor=.95, penetrationFactor=40,
              originalOffense=4.7036132812, offense=4.4684326171, originalPenetration=1, penetration=40)


def validate_plan(plan, floor):
    h.check(type(floor) is int and floor == 9 and set(plan) == set(VALUES) | set(HASHES), 'Exact floor-nine penetration plan required')
    h.check({k: plan[k] for k in VALUES} == VALUES, 'Only the frozen Ni penetration candidate is supported')
    h.check(type(plan['floor']) is int and all(type(plan[k]) in (int, float) for k in VALUES if k not in ('version', 'floor')), 'Numeric candidate fields required')
    for key in HASHES:
        h.check(type(plan[key]) is str and re.fullmatch('[0-9a-f]{64}', plan[key]), 'Source SHA-256 required')


def expected(api, plan, floor):
    validate_plan(plan, floor)
    for key, relative in HASHES.items(): h.check(h.sha(api/relative) == plan[key], 'Candidate source changed')
    tower = h.read(api/TOWER)
    guardian = h.unique(tower['floors'], 'floorNumber', floor)
    scaling = guardian['guardianScaling']
    h.check(scaling['health'] == 2.970703125 and scaling['offense'] == plan['originalOffense'] and
            scaling['penetration'] == plan['originalPenetration'], 'Original Ni scaling changed')
    scaling['offense'] = plan['offense']; scaling['penetration'] = plan['penetration']
    return tower


def materialize(api, isolated, plan, floor):
    h.check(api.resolve() != isolated.resolve(), 'Candidate must be isolated')
    intended = expected(api, plan, floor)
    for key, relative in HASHES.items(): h.check(h.sha(isolated/relative) == plan[key], 'Isolated source changed')
    (isolated/TOWER).write_text(json.dumps(intended, indent=2)+'\n', encoding='utf-8')
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
    return dict(source=declaration['source'], sourceManifestSha256=declaration['sourceManifestSha256'],
                floor=9, offenseFactor=.95, penetrationFactor=40)
