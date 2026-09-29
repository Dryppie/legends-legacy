"""Resume the independent chronological reference auditor from an audited personal runtime."""
from collections import Counter
import copy
from datetime import timedelta
import importlib.util
import json
from pathlib import Path

spec=importlib.util.spec_from_file_location('continuation_journey',Path(__file__).with_name('audit-growing-activity.py'))
reference=importlib.util.module_from_spec(spec);spec.loader.exec_module(reference)
dt,check=reference.dt,reference.check

def state(runtime,at):
    return dict(at=at,idleSeconds=runtime['idleSeconds'],combatTicks=runtime['combatTicks'],growth=runtime['growth']['state'],
        dungeonExperience=runtime['growth']['dungeonExperience'],withheldIdleExperience=0,withheldDungeonExperience=0,source=runtime['sources']['state'])

def audit(api,fixtures,proof,run):
    before=proof['before'];after=proof['afterProgress'];receipt=proof['receipt'];owner=proof['entry']['id']
    before_state=copy.deepcopy(state(before,receipt['started']))
    for item,quantity in proof['costs'].items():before_state['source']['items'][item]-=quantity
    after_state=state(after,receipt['ended'])
    synthetic=dict(summary=dict(policy='earned-progression'),final=proof['character'],windows=[],
        progression=dict(days=after['days'],entries=[dict(ordinal=0,before=before_state,receipt=receipt,after=after_state)]))
    j=reference.JourneyAudit(api,fixtures,synthetic,lambda _:run)
    g=before['growth'];s=before['sources'];v=g['state']
    j.level=v['level'];j.xp=v['experience'];j.idle=v['idleExperience'];j.prophecy=v['prophecyExperience'];j.dungeon=g['dungeonExperience']
    j.n=before['idleSeconds']//10;j.ticks=before['combatTicks'];j.es=copy.deepcopy(g['ownedEssences']);j.attuned=g['attuned']
    j.epoch=dt(receipt['started'])-timedelta(seconds=j.n*10+j.ticks/10)
    j.dayindex=len(before['days']);j.observed=dt(before['observedDay'])
    j.items=Counter(before_state['source']['items']);j.cinders=s['state']['cinders'];j.soulstones=s['state']['soulstones'];j.fate=s['state']['fateEcho']
    j.claims=[c|dict(at=dt(c['at']),periodStart=dt(c['periodStart'])) for c in s['claims']]
    j.events=[e|dict(at=dt(e['at'])) for e in before['dungeonEvents']]
    j.favor=Counter({dt(w['periodStart']):w['propheticFavor'] for w in s['weeks']})
    j.milestones={(dt(w['periodStart']),i) for w in s['weeks'] for i in [3,5,7] if w['milestone'+str(i)+'Claimed']}
    j.masteries={m['dungeonDefinitionId']:(m['experience'],m['level'],m['completionCount']) for m in before['mastery']}
    def lower(v):
        if isinstance(v,dict):return {k[0].lower()+k[1:]:lower(x) for k,x in v.items()}
        if isinstance(v,list):return [lower(x) for x in v]
        return v
    def offer(instance):
        return dict(definition=instance['prophecyDefinitionId'],slot=instance['slotType'],objective=instance['prophecyDefinition']['objectiveType'],
            target=instance['targetValue'],progress=instance['currentValue'],status=instance['status'],
            acceptedAt=dt(instance['acceptedAt']) if instance['acceptedAt'] else None,reward=lower(json.loads(instance['rewardSnapshotJson'])))
    overview=before['overview'];j.weekly=offer(overview['greaterProphecy']);j.week_start=dt(overview['greaterProphecy']['periodStart'])
    selected=before['days'][-1]['selected']
    j.selected=next((offer(p) for p in overview['dailyProphecies'] if p['prophecyDefinitionId']==selected),None)
    mastery=j.entry(dict(file=proof['runFile'],before=proof['entry'],dungeon=run['dungeon']))
    check(j.events==[e|dict(at=dt(e['at'])) for e in after['dungeonEvents']],'Resumed events differ')
    check(j.claims==[c|dict(at=dt(c['at']),periodStart=dt(c['periodStart'])) for c in after['sources']['claims']],'Resumed claims differ')
    check(j.dayindex==len(after['days']),'Missing new day observation')
    for current in [j.selected,j.weekly]:
        if current is None:continue
        match=next(i for i in after['sources']['instances'] if i['prophecyDefinitionId']==current['definition'] and i['acceptedAt'] and dt(i['acceptedAt'])==current['acceptedAt'])
        check(match['currentValue']==current['progress'] and match['status']==current['status'],'Active prophecy reset or changed')
    check(after['idleSeconds']==before['idleSeconds'] and after['waitingTicks']==before['waitingTicks'],'Unmodeled idle/waiting inside dungeon')
    return mastery
