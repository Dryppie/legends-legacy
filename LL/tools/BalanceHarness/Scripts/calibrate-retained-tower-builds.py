"""Bounded linked Health/Power calibration of complete retained Tower parties.

Uses the source pilot's retained executable and normal Tower archives. Each phase
writes new evidence; completed phases are never silently rerun or overwritten.
"""
import argparse
import concurrent.futures
import copy
import hashlib
import json
import re
from pathlib import Path
import shutil
import struct
import subprocess
import time

ROOT = Path(__file__).resolve().parents[4]
LIVE = ROOT / 'LL/src/API/API.LL'
FLOORS = 'world-tower/tower-floors.json'
COARSE = [1, 1.2, 1.4, 1.6, 1.8, 2.1, 2.5, 3]


def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def read_settings(p):
    # Preserve quoted strings while removing the JSON comments accepted by .NET configuration.
    raw = re.sub(r'"(?:\\.|[^"\\])*"|//[^\r\n]*|/\*[\s\S]*?\*/',
                 lambda m: m.group() if m.group().startswith('"') else '',p.read_text(encoding='utf-8-sig'))
    return json.loads(raw)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def identity(party):
    p = copy.deepcopy(sorted(party, key=lambda x: x['partySlot']))
    for member in p:
        b = member['build']
        b['identityEssenceIds'] = b.get('identityEssenceIds') or b['essenceIds']
        b['equipment'].sort(key=lambda e: e['slot'])
    return hashlib.sha256(json.dumps(p, sort_keys=True, ensure_ascii=False).encode()).hexdigest()


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('x', encoding='utf-8', newline='\n') as f:
        json.dump(value, f, indent=2, ensure_ascii=False); f.write('\n')


def command(out, args, log, allowed=(0,)):
    log.parent.mkdir(parents=True, exist_ok=True)
    with log.open('x', encoding='utf-8') as f:
        result = subprocess.run(['dotnet', str(out/'executable/BalanceHarness.dll'), *map(str,args)],
                                cwd=ROOT, stdout=f, stderr=subprocess.STDOUT)
    if result.returncode not in allowed:
        raise RuntimeError(f'Command failed ({result.returncode}); retained log: {log}')
    return result.returncode


def freeze(out, source):
    assert not (out/'protocol.json').exists(), 'Choose a new campaign directory.'
    source = source.resolve()
    inventory = read(source/'package-checksums.json')
    assert sha(source/'package-checksums.json') == read(source/'package-seal.json')['sha256']
    proof = read(source/'verification-summary.json')
    used = set(read(source/'historical-exclusions.json'))
    portfolios = {}
    for floor in (1,5):
        run = source/f'floor-{floor}-run'
        for name in ('study.json','definition.json','files.json'):
            assert sha(run/name) == inventory[f'floor-{floor}-run/{name}']
        assert sha(run/'files.json') == proof['pilotResults'][str(floor)]['inventorySha256']
        d = read(run/'definition.json'); report = read(run/'study.json')
        assert report['status'] == 'Complete'
        used.update(d['generation']['seeds']); used.update(d['excludedCombatSeeds'])
        for schedule in d['stages']['schedules'].values():
            for seeds in schedule.values(): used.update(seeds)
        entries = {}
        members = {m['cellId']:m for m in report['confirmation']['members']}
        for cell in report['confirmation']['definition']['cells']:
            scenario = copy.deepcopy(cell['scenario']); scenario['seeds'] = []
            generated = bool(members[cell['id']]['generatedIds'])
            key = identity(scenario['party'])
            entries[key] = {'id': f'f{floor}-{key[:20]}', 'scenario': scenario,
                            'generatedFinalist': generated, 'sources':[cell['id']]}
        proposals = {p['party']['id']:p['party'] for arm in report['discovery']['arms'] for p in arm['proposals']}
        for selected in report['selection']:
            scenario = copy.deepcopy(report['confirmation']['definition']['cells'][0]['scenario'])
            scenario['seeds'] = []; scenario['party'] = copy.deepcopy(d['contexts'][0]['characterTemplates'])
            for m in scenario['party']:
                m['build']['essenceIds'] = proposals[selected['id']]['builds'][str(m['partySlot'])]
                m['build']['identityEssenceIds'] = [f'neutral-identity-slot-{i}' for i in range(1,d['budget']['essenceSlots']+1)]
            key = identity(scenario['party'])
            if key in entries: entries[key]['sources'].append(selected['id'])
            else: entries[key] = {'id':f'f{floor}-{key[:20]}','scenario':scenario,
                                  'generatedFinalist':False,'sources':[selected['id']]}
        portfolios[str(floor)] = {'budget':d['budget'],'cohort':report['confirmation']['definition']['cohorts'][0],
            'definition':d, 'entries':sorted(entries.values(),key=lambda e:e['id'])}
        save(out/f'floor-{floor}-portfolio.json',portfolios[str(floor)])
    # Take current allowlisted game content, including unrelated additions, without copying secrets.
    names = portfolios['1']['definition']['contentHashes']
    content_hashes = {}
    for name in names:
        src = LIVE/'Data'/name; dst = out/'baseline-root/Data'/name
        dst.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(src,dst); content_hashes[name]=sha(dst)
    settings = read(source/'frozen-root/appsettings.json'); current = read_settings(LIVE/'appsettings.json')
    assert settings['Combat']['ThreatAndTanking'] == current['Combat']['ThreatAndTanking']
    assert settings['WorldTower']['CombatTicksPerFrame'] == current['WorldTower']['CombatTicksPerFrame']
    assert settings['Combat']['IdleProgression']['EncounterCadenceSeconds'] == current['Combat']['IdleProgression']['EncounterCadenceSeconds']
    save(out/'baseline-root/appsettings.json',settings)
    shutil.copytree(source/'floor-1-run/executable',out/'executable')
    for p in (out/'executable').rglob('*'):
        if p.is_file(): assert sha(p)==inventory['floor-1-run/executable/'+p.relative_to(out/'executable').as_posix()]
    save(out/'excluded-seeds.json',sorted(used))
    ledger={}
    def seeds(label,count):
        result=[]; i=0
        while len(result)<count:
            v=struct.unpack('<i',hashlib.sha256(f'{out.name}:{label}:{i}'.encode()).digest()[:4])[0]; i+=1
            if v not in used: used.add(v); result.append(v)
        return result
    for floor in (1,5): ledger[str(floor)]={k:seeds(f'{floor}-{k}',n) for k,n in [('coarse',8),('fine',64),('confirmation',400)]}
    ledger['regression']={str(f):seeds(f'regression-{f}',5) for f in range(1,16) if f not in (1,5)}
    save(out/'seed-ledger.json',ledger)
    baselines={str(f['floorNumber']):f['guardianScaling'] for f in read(out/'baseline-root/Data'/FLOORS)['floors'] if f['floorNumber'] in (1,5)}
    maximum=sum(len(p['entries'])*(8*len(COARSE)+400)+12*11*64 for p in portfolios.values())+6*20+130+42
    assert maximum<=100000
    save(out/'protocol.json',{'status':'FrozenBeforeCombat','id':out.name,'source':str(source),
        'sourceManifestSha256':sha(source/'package-checksums.json'),'scope':'Floors 1 and 5, full 80-Essence pool including Rare; identical pilot gear, levels, identities and slot counts; hypothetical ownership.',
        'mode':'Linked: multiply the captured current Health and offense inputs by the same factor, rounded to six decimal places; all other fields fixed.',
        'baseline':baselines,'contentHashes':content_hashes,'settingsHash':portfolios['1']['definition']['settingsHash'],
        'executionHash':portfolios['1']['definition']['executionHash'],'coarseMultipliers':COARSE,
        'coarse':'Every retained confirmation party plus every previous shortlisted party, eight paired fresh seeds per multiplier.',
        'fineRule':'Narrowest adjacent coarse pair straddling a maximum party win rate of 30%, ties lower factor; otherwise neighbors of closest maximum rate. Eleven equally spaced points. Include all six original generated finalists, then strongest other parties by total coarse wins, ties stable ID, up to twelve per floor. Each receives 64 paired fresh seeds at every fine point. Omitted fine parties remain in confirmation.',
        'selectionRule':'Among fine settings with maximum party win rate in 15-40%, select closest maximum to 30%, ties lower factor. No eligible setting stops that floor without application.',
        'confirmation':'Freeze one linked setting per floor before confirmation. Every portfolio entry receives 400 new paired seeds. Use existing bonferroni-wilson-95-v1 evaluator separately per floor. Every adjusted upper bound <=50%, at least one adjusted lower bound >=10%. No reselection, extra samples or dropping parties after confirmation.',
        'application':'Only a passing frozen setting may change local Health/offense for its floor. Verify exact reports for first twenty confirmation seeds of each of six original finalists, and five before/after identities on each unaffected floor. Repeated seeds are parity, not fresh evidence.',
        'replays':'First occurrence of each outcome for each generated finalist confirmation and local parity, plus first strongest non-finalist per floor: maximum 42 detailed battles.',
        'limitations':'Fixed retained portfolio, not a new exhaustive search. Other discovery candidates and other floors remain unvalidated on tuned content; no claim of full Tower acceptance.',
        'maximumActualBattles':maximum,'portfolioCounts':{f:len(p['entries']) for f,p in portfolios.items()},
        'scriptSha256':sha(Path(__file__)),'scheduleSha256':sha(out/'seed-ledger.json')})
    print(json.dumps({'frozen':True,'parties':{f:len(p['entries']) for f,p in portfolios.items()},'maximumBattles':maximum}),flush=True)


def variant(out,floor,label,multiplier):
    target=out/'variants'/f'f{floor}-{label}'
    shutil.copytree(out/'baseline-root',target)
    d=read(target/'Data'/FLOORS); base=read(out/'protocol.json')['baseline'][str(floor)]
    row=next(f for f in d['floors'] if f['floorNumber']==floor)
    row['guardianScaling'].update({k:round(base[k]*multiplier,6) for k in ('health','offense')})
    (target/'Data'/FLOORS).write_text(json.dumps(d,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    return target


def run_party(out,label,content,entry,seeds):
    scenario=copy.deepcopy(entry['scenario']); scenario['seeds']=seeds
    path=out/'scenarios'/f'{label}-{entry["id"]}.json'; save(path,scenario)
    target=out/'runs'/f'{label}-{entry["id"]}'
    command(out,['tower','--scenario',path,'--content-root',content,'--output',target],out/'logs'/f'{label}-{entry["id"]}.log')
    s=read(target/'scorecard.json')
    assert s['status']=='Complete' and s['valid']==len(seeds) and s['invalid']==s['cancelled']==s['notRun']==0
    return {'id':entry['id'],'run':target.relative_to(out).as_posix(),**{k:s[k] for k in ('wins','defeats','draws','valid','clearRate','winDurationSeconds','nonWinDurationSeconds')}}


def batch(out,label,content,entries,seeds):
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        jobs=[pool.submit(run_party,out,label,content,e,seeds) for e in entries]
        rows=[]
        for i,job in enumerate(jobs,1):
            rows.append(job.result())
            if i%20==0: print(f'{label}: {i}/{len(entries)} parties',flush=True)
    result={'label':label,'rows':rows,'maximumRate':max(r['wins']/r['valid'] for r in rows)}
    save(out/'observations'/f'{label}.json',result)
    print(f'{label}: strongest {100*result["maximumRate"]:.2f}%',flush=True)
    return result


def discover(out,floor):
    p=read(out/f'floor-{floor}-portfolio.json'); seeds=read(out/'seed-ledger.json')[str(floor)]
    coarse=[]
    for i,m in enumerate(COARSE):
        label=f'f{floor}-coarse-{i:02}'
        coarse.append({'multiplier':m,**batch(out,label,variant(out,floor,f'coarse-{i:02}',m),p['entries'],seeds['coarse'])})
    save(out/f'floor-{floor}-coarse.json',coarse)
    pairs=[(a,b) for a,b in zip(coarse,coarse[1:]) if (a['maximumRate']-.3)*(b['maximumRate']-.3)<=0]
    if pairs: left,right=min(pairs,key=lambda ab:(round(ab[1]['multiplier']-ab[0]['multiplier'],6),ab[0]['multiplier']))
    else:
        i=min(range(len(coarse)),key=lambda i:(abs(coarse[i]['maximumRate']-.3),coarse[i]['multiplier']))
        left,right=coarse[max(0,i-1)],coarse[min(len(coarse)-1,i+1)]
    scores={e['id']:sum(next(r['wins'] for r in c['rows'] if r['id']==e['id']) for c in coarse) for e in p['entries']}
    finalists=[e for e in p['entries'] if e['generatedFinalist']]
    others=sorted((e for e in p['entries'] if not e['generatedFinalist']),key=lambda e:(-scores[e['id']],e['id']))
    selected=finalists+others[:12-len(finalists)]
    grid=sorted({round(left['multiplier']+(right['multiplier']-left['multiplier'])*i/10,6) for i in range(11)})
    save(out/f'floor-{floor}-fine-plan.json',{'bracket':[left['multiplier'],right['multiplier']],'multipliers':grid,'entries':[e['id'] for e in selected]})
    fine=[]
    for i,m in enumerate(grid):
        label=f'f{floor}-fine-{i:02}'
        fine.append({'multiplier':m,**batch(out,label,variant(out,floor,f'fine-{i:02}',m),selected,seeds['fine'])})
    save(out/f'floor-{floor}-fine.json',fine)
    eligible=[r for r in fine if .15<=r['maximumRate']<=.4]
    if not eligible:
        save(out/f'floor-{floor}-selection.json',{'status':'NoEligibleSetting'}); return
    selected=min(eligible,key=lambda r:(abs(r['maximumRate']-.3),r['multiplier']))
    content=variant(out,floor,'selected',selected['multiplier'])
    definition=copy.deepcopy(p['definition']); definition['contentHashes'][FLOORS]=sha(content/'Data'/FLOORS)
    cohort=copy.deepcopy(p['cohort'])
    cells=[]
    for e in p['entries']:
        scenario=copy.deepcopy(e['scenario']); scenario['seeds']=seeds['confirmation']
        cells.append({'id':e['id'],'cohortId':cohort['id'],'role':'generated' if e['generatedFinalist'] else 'reference','scenario':scenario,'minimumSamples':400})
    exclusions=set(read(out/'excluded-seeds.json'))
    for s in read(out/'seed-ledger.json').values():
        if isinstance(s,dict):
            for k,v in s.items():
                if k!='confirmation': exclusions.update(v)
    balance={'schemaVersion':1,'id':f'f{floor}-retained-calibration','intervalPolicy':'bonferroni-wilson-95-v1',
        'contentHashes':read(out/'protocol.json')['contentHashes'],'settingsHash':definition['settingsHash'],
        'executionHash':definition['executionHash'],'cohorts':[cohort],'cells':cells,
        'excludedCombatSeeds':sorted(exclusions),'maximumBattles':len(cells)*400}
    balance['contentHashes'][FLOORS]=sha(content/'Data'/FLOORS)
    save(out/f'floor-{floor}-balance-definition.json',balance)
    save(out/f'floor-{floor}-selection.json',{'status':'FrozenBeforeConfirmation','selected':selected,
        'definitionSha256':sha(out/f'floor-{floor}-balance-definition.json')})
    print(f'Floor {floor} frozen multiplier {selected["multiplier"]}',flush=True)


def confirm(out,floor):
    selected=read(out/f'floor-{floor}-selection.json')
    if selected['status']!='FrozenBeforeConfirmation': return
    p=read(out/f'floor-{floor}-portfolio.json')
    result=batch(out,f'f{floor}-confirmation',out/'variants'/f'f{floor}-selected',p['entries'],read(out/'seed-ledger.json')[str(floor)]['confirmation'])
    sources=[{'cellId':r['id'],'runDirectory':r['run']} for r in result['rows']]
    save(out/f'floor-{floor}-balance-sources.json',sources)
    code=command(out,['tower-balance-evaluate','--definition',out/f'floor-{floor}-balance-definition.json',
        '--sources',out/f'floor-{floor}-balance-sources.json','--output',out/f'floor-{floor}-assessment'],out/'logs'/f'f{floor}-assessment.log',allowed=(0,1,2,3))
    report=read(out/f'floor-{floor}-assessment/assessment.json')
    save(out/f'floor-{floor}-confirmation.json',{'assessment':report['assessment'],'exitCode':code,'results':result})
    print(f'Floor {floor}: confirmation {report["assessment"]}',flush=True)


if __name__=='__main__':
    a=argparse.ArgumentParser(); a.add_argument('phase',choices=['freeze','discover','confirm']);a.add_argument('--output',type=Path,required=True)
    a.add_argument('--source-pilots',type=Path);a.add_argument('--floor',type=int,choices=[1,5]);args=a.parse_args()
    out=args.output.resolve()
    if args.phase=='freeze': freeze(out,args.source_pilots)
    elif args.phase=='discover': discover(out,args.floor)
    elif args.phase=='confirm': confirm(out,args.floor)
