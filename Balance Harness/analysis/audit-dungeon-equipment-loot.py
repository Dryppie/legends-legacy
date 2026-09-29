"""Independent native reward RNG, eligibility, blueprint state and loss/claim ledger reconstruction; no combat."""
import hashlib
import json
import math
import uuid
from collections import Counter


def check(value, message):
    if not value: raise AssertionError(message)


def digest(parts): return hashlib.sha256('\x1f'.join(parts).encode()).digest()
def identity(parts): return str(uuid.UUID(bytes_le=digest(parts)[:16]))
def seed(parts): return int.from_bytes(digest(parts)[:4], 'little', signed=True)
def int32(value): return (value + 2147483648) % 4294967296 - 2147483648


class Random:
    """Seeded .NET compatibility PRNG, independently transcribed for ledger verification."""
    def __init__(self, value):
        big=2147483647; mj=161803398-(big if value==-2147483648 else abs(value))
        self.values=[0]*56; self.values[55]=mj; mk=1
        for i in range(1,55):
            index=21*i%55; self.values[index]=mk; mk=int32(mj-mk)
            if mk<0: mk+=big
            mj=self.values[index]
        for _ in range(4):
            for i in range(1,56):
                self.values[i]=int32(self.values[i]-self.values[1+(i+30)%55])
                if self.values[i]<0: self.values[i]+=big
        self.a=0; self.b=21
    def next(self):
        self.a=self.a+1 if self.a<55 else 1; self.b=self.b+1 if self.b<55 else 1
        n=int32(self.values[self.a]-self.values[self.b])
        if n==2147483647: n-=1
        if n<0: n+=2147483647
        self.values[self.a]=n
        return n*(1.0/2147483647)
    def index(self, count): return int(self.next()*count)


def weighted(entries, random):
    available=[(key,value) for key,value in entries if value>0]
    roll=random.next()*sum(value for _,value in available); total=0
    for key,value in available:
        total+=value
        if roll<total: return key
    return available[-1][0]


class LootAuditor:
    def __init__(self, api):
        def read(name): return json.loads((api/'Data/equipment'/name).read_text(encoding='utf-8-sig'))
        self.rules=next(p for p in read('equipment-ordinary.v1.json') if p['region']==1)
        self.bp=read('equipment-blueprints.v1.json')
        self.items=read('equipment-starters.v4.json')['items']
        self.styles=read('equipment-styles.v4.json')

    def equipment(self, award, parts, family, owner):
        random=Random(seed(parts)); random.next()  # eligibility roll already verified by caller
        rarity=weighted(list(self.rules['dungeonEquipment']['rarities']['novice'].items()),random)
        weights=self.rules['selectionWeights']
        category=weighted([(k,weights[k]) for k in ['weapons','armor','jewelry']],random)
        types={'armor':['Head','Chest','Legs'],'jewelry':['Ring','Necklace','Relic']}
        if category=='weapons':
            hands=weighted([(k,weights[k]) for k in ['oneHanded','twoHanded']],random)
            allowed=['OneHanded','OffHand'] if hands=='oneHanded' else ['TwoHanded']
        else: allowed=types[category]
        candidates=sorted([i for i in self.items if i['equipmentType'] in allowed],key=lambda i:i['id'])
        item=candidates[random.index(len(candidates))]
        options=sorted([(item['id']+f'.rarity.{rarity}','default')]+[(item['id']+f'.spec.{s}.rarity.{rarity}',s) for s in item.get('specializationIds',[])])
        definition,specialization=options[random.index(len(options))]
        quality=weighted(list(self.rules['dungeonEquipment']['qualities'].items()),random)
        attribute=.95+random.next()*.10
        source=next(s for s in self.bp['sources'] if s['familyId']==family)
        variant=Random(seed(parts+['variant'])); style=None
        if variant.next()<self.bp['dungeonVariantChance']:
            compatible=sorted(s['id'] for s in self.styles if s['id'] in source['styleIds'] and item['id'] in s['compatibleArchetypeIds'])
            if compatible: style=compatible[variant.index(len(compatible))]
        data=award['progressionData']; state=data['state']; expected_id=identity(parts)
        check(award['id']==state['id']==expected_id and award['quantity']==1 and award['itemType']=='Equipment','Wrong ordinary item identity/type')
        check(award['source']==parts[0] and state['definitionId']==definition and state['archetypeId']==item['id'],'Changed ordinary equipment draw')
        check(data['itemBaseId']==award['itemId']==item['itemBaseId'] and data['equipmentType']==item['equipmentType'],'Changed ordinary item type')
        check(state['nativeStyleId']==state['activeStyleId']==style and data['allocation']['specializationId']==specialization,'Changed variant/specialization draw')
        check(state['rank']==self.rules['dungeonEquipment']['rank']==1 and state['tier']==self.rules['equipmentTier']==1 and state['balanceVersion']==4,'Changed reward rank/tier/balance')
        check(state['quality'].lower()==data['quality'].lower()==quality and data['rarity'].lower()==rarity,'Changed rarity/quality roll')
        check(math.isclose(state['attributeRollMultiplier'],attribute,rel_tol=0,abs_tol=1e-14) and data['attributeRollMultiplier']==state['attributeRollMultiplier'],'Changed attribute draw')
        check(state['ownership']['ownerId']==owner and state['ownership']['kind']=='UnboundPersonal','Wrong loot owner')
        check(state['provenance']==dict(kind='RandomDiscovery',sourceId=family,awardId=parts[2]),'Wrong loot provenance')
        check(all(math.isfinite(x) and x>=0 for x in data['stats'].values()),'Invalid materialized stats')

    def audit(self, loot, owner, run, previous, mastery_level=0, resolved_routes=False):
        family=run['dungeon']; layout=str(run['layoutSeed'])
        run_id=identity(['dungeon-acquisition-run-v1',owner,family,layout])
        check(loot['runId']==run_id and loot['before']==previous,'Wrong reward run/pity history')
        expected_hooks=[('dungeon-miniboss',a['room']) for a in run['actions'] if (a['action']=='fight' or resolved_routes and a['action']=='choose_route') and a['type']=='MiniBoss' and a['status']=='Active']
        completed=run['status']=='Completed'
        if completed: expected_hooks.append(('model-e:dungeon-completion',None))
        check([(r['source'],r['room']) for r in loot['rolls']]==expected_hooks,'Reward from absent/failed room')
        after={s['family']:dict(s) for s in previous}; pending=[]
        for roll in loot['rolls']:
            parts=[roll['source'],owner.replace('-',''),run_id.replace('-',''),layout]
            if roll['room'] is not None: parts.append(str(roll['room']))
            equipment=[a for a in roll['awards'] if a['progressionData'] is not None]
            chance=self.rules['dungeonEquipment']['dropChance' if roll['room'] is None else 'miniBossDropChance']
            if roll['room'] is None: chance=min(1,chance+mastery_level*.05)
            check(len(equipment)==int(Random(seed(parts)).next()<chance),'Wrong ordinary reward chance/no-drop')
            if equipment: self.equipment(equipment[0],parts,family,owner)
            blueprints=[a for a in roll['awards'] if a['progressionData'] is None]
            if roll['room'] is None:
                state=after.get(family,dict(family=family,misses=0,lastRunId=None))
                chance=1 if state['misses']>=self.bp['guaranteeCompletions']-1 else self.bp['dropChance']
                awarded=Random(seed(parts+['blueprint'])).next()<chance
                check(len(blueprints)==int(awarded),'Wrong blueprint pity/drop')
                if awarded:
                    source=next(s for s in self.bp['sources'] if s['familyId']==family)
                    ids=source['blueprintStyleIds']; chosen=ids[Random(seed(parts+['blueprint-type'])).index(len(ids))]
                    definition=next(b for b in self.bp['blueprints'] if b['styleId']==chosen)
                    check(blueprints[0]==dict(id=identity(parts+['blueprint']),itemId=definition['itemId'],name='Blueprint: '+definition['name'],itemType='Resource',quantity=1,source='equipment-blueprint',progressionData=None),'Changed blueprint reward')
                after[family]=dict(family=family,misses=0 if awarded else state['misses']+1,lastRunId=run_id)
            else: check(not blueprints,'Miniboss granted a blueprint')
            pending.extend(roll['awards'])
        check(loot['after']==[after[k] for k in sorted(after)],'Wrong persisted blueprint progress')
        check(loot['lost']==([] if completed else pending) and loot['pending']==(pending if completed else []),'Pending gear survived a failed run or was lost on success')
        check(loot['equipment']==[r['progressionData'] for r in pending if r['progressionData'] is not None and completed],'Claim changed frozen equipment')
        resources=Counter()
        for r in pending:
            if completed and r['progressionData'] is None: resources[r['itemId']]+=r['quantity']
        check(loot['blueprints']==dict(resources),'Blueprint claim mismatch')
        check(loot['retryChecks']==len(expected_hooks)+int(completed),'Missing callback/claim idempotence checks')
