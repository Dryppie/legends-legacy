"""One fixed 0.60 offense candidate, preserving the closed 0.525 contract."""
import importlib.util
from pathlib import Path

s=importlib.util.spec_from_file_location('kodoku_offense_base',Path(__file__).with_name('tower-kodoku-shared-penetration.py'))
base=importlib.util.module_from_spec(s);s.loader.exec_module(base)
VERSION='tower-kodoku-eight-item-pressure-v1'
VALUES={**base.VALUES,'version':VERSION,'offenseFactor':.6}


def legacy_plan(plan, floor):
    base.check(plan.get('version') == VERSION and type(plan.get('offenseFactor')) in (int,float) and plan['offenseFactor'] == .6,
               'Only the frozen 0.60 offense candidate is supported')
    legacy={**plan,'version':base.VERSION,'offenseFactor':.525}
    base.validate_plan(legacy,floor)
    return legacy


def expected(api, plan, floor):
    legacy=legacy_plan(plan,floor)
    intended=base.expected(api,legacy,floor)
    guardian=base.unique(intended[base.TOWER]['floors'],'floorNumber',8)
    guardian['guardianScaling']['offense']=round(plan['sourceOffense']*.6,10)
    return intended


def materialize(api, isolated, plan, floor):
    base.check(api.resolve()!=isolated.resolve(),'Candidate must be isolated')
    intended=expected(api,plan,floor)
    for key,relative in base.HASHES.items(): base.check(base.sha(isolated/relative)==plan[key],'Isolated source changed')
    base.write_objects(api,isolated,intended)
    return verify(api,isolated,plan,floor)


def verify(api,candidate,plan,floor):
    return base.verify_expected(api,candidate,expected(api,plan,floor),floor,VERSION)
