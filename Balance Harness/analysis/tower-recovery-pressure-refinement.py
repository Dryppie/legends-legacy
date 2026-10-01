"""Separate fixed 0.9% recovery contract; the 0.75% contract remains unchanged."""
import copy
import importlib.util
import json
from pathlib import Path

s=importlib.util.spec_from_file_location('refinement_recovery',Path(__file__).with_name('tower-recovery-pressure-candidate.py'))
r=importlib.util.module_from_spec(s);s.loader.exec_module(r)
h=r.h
VERSION='tower-recovery-pressure-refinement-v1'
ENDLESS,HEAL,ABUNDANCE,DESCRIPTION=r.ENDLESS,r.HEAL,r.ABUNDANCE,r.DESCRIPTION
validate_mode=r.validate_mode


def expected(api,plan,floor):
    h.check(set(plan)=={'version','floor','healthPressurePlan','recoveryChange'} and plan['version']==VERSION,
            'Separate fixed recovery refinement required')
    h.check(plan['recoveryChange']==dict(abilityId=ENDLESS,effectId=HEAL,field='statusScalingCoefficient',fromValue=.01,toValue=.009),
            'Only the declared 1% to 0.9% recovery edit is permitted')
    # Reuse the unchanged source, damage, ownership and timing checks from v1.
    original=copy.deepcopy(plan);original['version']=r.VERSION;original['recoveryChange']['toValue']=.0075
    data=r.expected(api,original,floor)
    h.unique(h.unique(data[h.ABILITIES],'id',ENDLESS)['effects'],'id',HEAL)['statusScalingCoefficient']=.009
    return data


def materialize(api,isolated,plan,floor):
    h.check(api.resolve()!=isolated.resolve(),'Candidate must be isolated')
    for relative,value in expected(api,plan,floor).items():
        h.check(h.sha(api/relative)==h.sha(isolated/relative),'Isolated source changed')
        (isolated/relative).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
    return verify(api,isolated,plan,floor)


def verify(api,candidate,plan,floor):
    data=expected(api,plan,floor)
    catalogs={p.relative_to(api) for p in (api/'Data').rglob('*.json')}
    h.check(catalogs=={p.relative_to(candidate) for p in (candidate/'Data').rglob('*.json')},'Candidate catalog set changed')
    for relative in catalogs:
        if relative in data:h.check(h.read(candidate/relative)==data[relative],'Undeclared candidate delta: '+str(relative))
        else:h.check(h.sha(api/relative)==h.sha(candidate/relative),'Unrelated catalog changed: '+str(relative))
    h.check(h.sha(api/'appsettings.json')==h.sha(candidate/'appsettings.json'),'Settings changed')
    return dict(version=VERSION,floor=floor,catalogs=len(catalogs),
                candidateAbilitiesSha256=h.sha(candidate/h.ABILITIES),candidateTowerSha256=h.sha(candidate/h.TOWER))
