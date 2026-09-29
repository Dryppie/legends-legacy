"""Audit immutable bootstrap evidence without combat: ownership, production preparation, costs, censoring and seeds."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
from pathlib import Path
import sys

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('bootstrap_launch', Path(__file__).with_name('run-tower-bootstrap.py'))
launch = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = launch
spec.loader.exec_module(launch)
io = launch.io
run_audit = launch.module('bootstrap_run_audit', Path(__file__).with_name('verify-dungeon-acquisition.py')).audit_run


def dominates(a, b):
    rank = ['Common', 'Uncommon', 'Rare', 'Epic', 'Unique', 'Legendary', 'Legacy']
    quality = ['Crude', 'Standard', 'Fine', 'Exceptional', 'Masterpiece']
    return (a['state']['archetypeId'] == b['state']['archetypeId']
            and a['allocation']['specializationId'] == b['allocation']['specializationId']
            and a['state']['activeStyleId'] == b['state']['activeStyleId']
            and a['state']['tier'] >= b['state']['tier'] and a['state']['rank'] >= b['state']['rank']
            and rank.index(a['rarity']) >= rank.index(b['rarity']) and quality.index(a['quality']) >= quality.index(b['quality'])
            and a['attributeRollMultiplier'] >= b['attributeRollMultiplier']
            and all(a['stats'].get(key, 0) >= value for key, value in b['stats'].items()))


def read_battle(path):
    with gzip.open(path, 'rt', encoding='utf-8') as stream:
        return json.load(stream)


def audit_starting_economy(api, cells):
    release = io.read(api/'Data/equipment/equipment-releases.json')['4']
    items = io.read(api/'Data/equipment'/release['starters'])['items']
    styles = io.read(api/'Data/equipment'/release['styles'])
    blueprints = io.read(api/'Data/equipment/equipment-blueprints.v1.json')
    rules = io.read(api/'Data/equipment/equipment-ordinary.v1.json')[0]
    regional_styles = {s for source in blueprints['sources'] if source['region']==1 for s in source['styleIds']}
    checked = 0
    for cell in cells:
        for drop in cell['dropRequirements']:
            data = next(e['data'] for e in cell['character']['equipment'] if e['data']['state']['definitionId']==drop['definition'])
            item = next(i for i in items if i['id']==data['state']['archetypeId'])
            kind = item['equipmentType']; weights = rules['selectionWeights']
            if kind in {'Head','Chest','Legs'}: group, weight = {'Head','Chest','Legs'}, weights['armor']
            elif kind in {'Ring','Necklace','Relic'}: group, weight = {'Ring','Necklace','Relic'}, weights['jewelry']
            elif kind=='TwoHanded': group, weight = {'TwoHanded'}, weights['weapons']*weights['twoHanded']
            else: group, weight = {'OneHanded','OffHand'}, weights['weapons']*weights['oneHanded']
            compatible = any(s['id'] in regional_styles and item['id'] in s['compatibleArchetypeIds'] for s in styles)
            probability = weight/sum(i['equipmentType'] in group for i in items)/(1+len(item.get('specializationIds',[])))
            probability *= (rules['areaEquipment']['dropChance'] * rules['areaEquipment']['rarities'][data['rarity'].lower()]
                            * (1-rules['areaEquipment']['qualities']['crude']) * (1-blueprints['areaVariantChance'] if compatible else 1))
            io.require(math.isclose(probability,drop['perEligibleVictory'],rel_tol=1e-12)
                       and math.isclose(1/probability,drop['expectedVictoriesForThisItemAlone'],rel_tol=1e-12),'Wrong exact-item acquisition probability')
            checked += 1
        armor_probability = 1/sum(1+len(i.get('specializationIds',[])) for i in items if i['equipmentType'] in {'Head','Chest','Legs'})
        io.require(math.isclose(armor_probability,cell['armorBoxOutcomeProbability']), 'Armor Chest is random, not a free selected item')
    return checked


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--owner', type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    owner = io.unlinked(args.owner.absolute())
    request, declaration = io.read(owner/'request.json'), io.read(owner/'declaration.json')
    output, api = Path(request['output']), Path(request['apiRoot'])
    io.require(not args.receipt.exists() and not args.receipt.absolute().is_relative_to(output), 'New receipt outside output required')
    io.require(request['version'] == launch.VERSION and declaration['requestSha256'] == io.sha(owner/'request.json'), 'Changed frozen request')
    envelope = dict(acquisitionCells=32, alreadyOwnedControls=8, sources=2, pathsPerCellAndSource=4, maximumAttemptsPerPath=12,
        controlAttemptsPerPath=1, qualificationReplays=80, maximumAttempts=3136, maximumFights=205824,
        maximumSeconds=900, maximumNativeSeconds=840, maximumBytes=256*1048576, newSeeds=6240, retries=0)
    io.require(all(declaration[k] == v for k,v in envelope.items()), 'Changed study envelope')
    for path, pin in request['inputHashes'].items():
        io.require(io.sha(Path(path)) == pin, 'Changed frozen input: '+path)
    io.require(io.sha(output/'files.json') == args.manifest_pin, 'Wrong output manifest pin')
    manifest = io.read(output/'files.json')
    io.require(set(manifest) == {p.name for p in output.iterdir() if p.name != 'files.json'}, 'Unmanifested output')
    for name,pin in manifest.items():
        io.require(io.sha(io.member(output, name)) == pin, 'Changed output: '+name)
    io.require(sum(p.stat().st_size for p in output.iterdir()) <= envelope['maximumBytes'], 'Output cap exceeded')
    process = io.read(owner/'process.json')
    io.require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Incomplete owned process')
    ledger = io.read(owner/'seed-ledger.json')
    def pinned(part):
        io.require(io.sha(Path(part['archive'])) == part['sha256'], 'Changed predecessor ledger')
        return io.read(Path(part['archive']))
    h, d = pinned(ledger['historical']), pinned(ledger['dungeon'])
    io.require(ledger['historical']['sha256'] == launch.HISTORICAL_PIN and ledger['dungeon']['sha256'] == launch.DUNGEON_PIN, 'Untrusted history')
    excluded = set(h['historical'] + h['first'] + h['second'] + d['reserved'])
    for part in ledger['preceding']: excluded.update(pinned(part)['reserved'])
    reservations = [s for panel in request['panels'] for s in [panel['run']['layoutSeed'], *panel['run']['roomSeeds']]]
    io.require(reservations == ledger['reserved'] and len(set(reservations)) == 6240 and not excluded.intersection(reservations)
               and len(excluded)+6240 == ledger['exclusionUnionCount'], 'Seed overlap or omitted reservation')
    panels = {(p['run']['dungeon'],p['path'],p['attempt']):p['run'] for p in request['panels']}
    io.require(len(panels) == 96, 'Incomplete source/path/attempt panels')
    plan = io.read(Path(request['planPath']))
    cells = io.read(output/'cells.json')
    probability_rows = audit_starting_economy(api, cells)
    result = io.read(output/'result.json')
    io.require(len(cells) == 40 and len({c['id'] for c in cells}) == 40 and len(result['paths']) == 320, 'Incomplete matrix')
    # Independently resolve prerequisite closure; no dungeon-completion reward may fund the first clear.
    quests = [io.read(p) for p in (api/'Data/quests').rglob('*.json')]
    all_quests = {q['id']:q for q in quests if 'id' in q and q['id'].startswith(('quest.onboarding.', 'quest.shenic.', 'quest.region01.'))}
    eligible = {}
    while True:
        found = {key:q for key,q in all_quests.items() if key not in eligible
                 and q.get('availability',{}).get('minimumLevel',0) <= 30
                 and all(p in eligible for p in q.get('availability',{}).get('completedQuestIds',[]))
                 and not any(o['type'].startswith('Dungeon') or o['type']=='CharacterLevelReached' and o['requiredAmount']>30 for o in q['objectives'])}
        if not found: break
        eligible.update(found)
    io.require(eligible == io.read(output/'pre-dungeon-quests.json') and 'quest.shenic.roots_remember' not in eligible, 'Quest closure mismatch')
    rewards = [r for q in eligible.values() for r in q['rewards']]
    sigils = {family:sum(r['quantity'] for r in rewards if r.get('itemBaseId')=='sigil_'+family) for family in ('goblin_mines','forgotten_catacombs')}
    io.require(set(sigils.values()) == {1}, 'Quest sigils must be charged only once per alternative history')
    prices = io.read(api/'Data/equipment/equipment-upgrades.v1.json')['tiers'][0]
    rules = io.read(api/'Data/equipment/equipment-ordinary.v1.json')[0]
    fragment_cost = io.read(api/'Data/dungeons/sigil-assembly.json')['fragmentCost']
    # Captured local contract: the native calculator additionally loads the configured cadence.
    cadence = 10
    fights = replays = acquired_paths = censored_paths = awards = attempts = controls = prepared = 0
    used = set()
    for cell in cells:
        io.require(cell['character']['level'] == 30 and len(cell['character']['essences']) == 4, 'Wrong actor budget')
        xp = sum(math.ceil(132860*1.02**max(0,l-1)) for l in range(1,cell['essenceLevel']))
        io.require(cell['trainingXpPerEssence'] == xp and cell['sumOfEssenceXp'] == 4*xp, 'Training XP mismatch')
        slots = sum(2 if e['data']['equipmentType']=='TwoHanded' else 1 for e in cell['character']['equipment'])
        parts = slots*prices['rankPartCosts'][0] if cell['gear']=='common-rank1' else 0
        cinders = slots*prices['rankCinderCosts'][0] if parts else 0
        io.require(cell['reinforcementParts']==parts and cell['reinforcementCinders']==cinders
                   and cell['additionalEarnedCindersRequired']==max(0,cinders-cell['questCinders']), 'Upgrade cost mismatch')
        siblings = [c for c in cells if c['recipe']==cell['recipe'] and c['essenceLevel']==cell['essenceLevel']]
        io.require(all(c['character']['id']==cell['character']['id'] and c['character']['essences']==cell['character']['essences'] for c in siblings), 'Unmatched Essence/actor controls')
        for family in ('goblin_mines','forgotten_catacombs'):
            for path_index in range(4):
                path = io.read(output/f"{cell['id']}--{family}--{path_index}--path.json")
                owned = copy.deepcopy(cell['ownedItems'])
                equipped = {e['slot']:e for e in cell['character']['equipment']}
                control = cell['gear']=='rare-control'
                successes = failures = path_awards = 0
                combat_seconds = 0
                for index,step in enumerate(path['steps']):
                    io.require(index == step['attempt'] and index < (1 if control else 12), 'Unbounded/reordered attempt')
                    before_ids = {e['data']['state']['id'] for e in equipped.values()}
                    io.require(set(step['before']) == before_ids, 'Previous earnings lost at next attempt')
                    panel = panels[family,path_index,index]
                    run = read_battle(output/step['runFile'])
                    run_audit(run,panel)
                    io.require(run['status']==step['status'] and run['combatSeconds']==step['combatSeconds'], 'Bad step summary')
                    used.add(panel['layoutSeed']); used.update(b['seed'] for b in run['battles'])
                    for battle in run['battles']:
                        friendly = [p for p in battle['preparedParticipants'] if p['slot']['side']=='Friendly']
                        io.require(len(friendly)==1, 'Dungeon is individual, not a Tower party')
                        p=friendly[0]
                        io.require(p['slot']['sourceEntityId']==cell['character']['id'] and p['level']==30, 'Wrong prepared owner/level')
                        actual = {e['state']['id']:e for e in p['equipment']}
                        wanted = {e['data']['state']['id']:e['data'] for e in equipped.values()}
                        io.require(actual == wanted, 'Production preparation changed earned gear')
                        io.require(p['essences'] == [dict(essenceDefinitionId=e['definitionId'],level=e['level'],ascensionTier=e['ascensionTier'],isEvolved=e['isEvolved']) for e in cell['character']['essences']], 'Prepared Essence budget changed')
                        prepared+=1
                    fights+=len(run['battles']); attempts+=1; combat_seconds+=run['combatSeconds']
                    if index==0 and path_index==0:
                        replay=read_battle(output/f"{cell['id']}--{family}--replay.json.gz")
                        io.require(replay==run,'Replay mismatch'); fights+=len(run['battles']); replays+=1
                    success=run['status']=='Completed'
                    successes+=int(success); failures+=int(not success)
                    award=step['award']
                    if success and not control:
                        missing=[t for slot in plan['purchaseOrder'] for t in cell['target'] if t['slot']==slot
                                 and not any(o['slot']==slot and dominates(o['data'],t['data']) for o in owned)]
                        io.require(missing and award is not None and award['slot']==missing[0]['slot'], 'Wrong earned upgrade or circular completion')
                        expected=copy.deepcopy(missing[0]['data'])
                        expected['state']['id']=award['data']['state']['id']
                        expected['state']['provenance']['awardId']=f"{cell['id']}/{family}/{path_index}/{path_awards+1}"
                        io.require(expected==award['data'] and all(o['data']['state']['id']!=expected['state']['id'] for o in owned), 'Award differs from production supply or duplicates an item')
                        owned.append(award); path_awards+=1; awards+=1
                        old=equipped.get(award['slot'])
                        if old is None or not dominates(old['data'],award['data']):
                            io.require(old is None or dominates(award['data'],old['data']), 'Lost stronger gear')
                            equipped[award['slot']]=award
                    else: io.require(award is None,'Failed attempt/control received a supply item')
                    io.require(set(step['after'])=={e['data']['state']['id'] for e in equipped.values()},'Incorrect post-award equipment')
                covered=all(any(e['slot']==t['slot'] and dominates(e['data'],t['data']) for e in equipped.values()) for t in cell['target'])
                count=len(path['steps']); additional=max(0,count-1)
                io.require(path['owned']==owned and {e['slot']:e for e in path['finalCharacter']['equipment']}==equipped, 'Final inventory mismatch')
                io.require(path['attempts']==count==successes+failures and path['successfulRuns']==successes and path['failedRuns']==failures
                           and path['supplyItemsEarned']==path_awards and path['completedSet']==(covered and not control), 'Incorrect completion accounting')
                io.require(path['startingQuestSigils']==1 and path['additionalSigilsRequired']==additional
                           and path['assemblyOnlyAdditionalFragments']==additional*fragment_cost
                           and math.isclose(path['randomOnlyAdditionalEligibleIdleHours'], additional/(rules['sigilDropChance']/len(rules['sigils']))*cadence/3600)
                           and math.isclose(path['combatSeconds'],combat_seconds), 'Incorrect resource/time arithmetic')
                if control: io.require(count==1 and path_awards==0,'Control became acquisition evidence'); controls+=1
                elif covered: io.require(path_awards==7 and success,'Set completed without seven earned pieces'); acquired_paths+=1
                else: io.require(count==12,'Incomplete path was not retained as censored'); censored_paths+=1
                summary=next(p for p in result['paths'] if p['cell']==cell['id'] and p['dungeon']==family and p['path']==path_index)
                io.require(all(summary[k]==path[k] for k in summary),'Incorrect aggregate summary')
    io.require(result['fights']==fights<=205824 and result['replays']==replays==80 and result['attempts']==attempts<=3136
               and result['measuredPlayerSamples']==0,'Work accounting mismatch')
    io.write(args.receipt,dict(status='VerifiedConditionalBootstrapNotTimeAcceptance',manifestPin=args.manifest_pin,
        inputHashesChecked=len(request['inputHashes']),fights=fights,attempts=attempts,replays=replays,
        productionPreparedRoomsChecked=prepared,completedAcquisitionPaths=acquired_paths,censoredAcquisitionPaths=censored_paths,
        perItemProbabilityRowsChecked=probability_rows,armorBoxRowsChecked=len(cells),
        alreadyOwnedControlPaths=controls,supplyItemsEarned=awards,newAuditFights=0,measuredPlayerSamples=0,
        reservedSeeds=6240,usedSeeds=len(used),reservedUnconsumedSeeds=len(set(reservations)-used),exclusionUnionCount=ledger['exclusionUnionCount']))
    print(json.dumps(io.read(args.receipt),indent=2))


if __name__ == '__main__':
    main()
