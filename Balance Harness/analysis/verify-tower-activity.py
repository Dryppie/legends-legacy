"""File-only audit of joint activity, quest gates, retained loot, entry stock and production dungeon evidence."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
import struct
from pathlib import Path
import sys

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('activity_owner', Path(__file__).with_name('run-tower-activity.py'))
owner_module = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = owner_module
spec.loader.exec_module(owner_module)
io = owner_module.io
audit_run = owner_module.module('activity_run_audit', Path(__file__).with_name('verify-dungeon-acquisition.py')).audit_run


def first_random(seed):
    """Seeded System.Random compatibility algorithm, first sample only (quest box uniform draw)."""
    big = 2147483647
    sub = big if seed == -2147483648 else abs(seed)
    mj = 161803398 - sub
    values = [0] * 56
    values[55] = mj
    mk = 1
    for i in range(1, 55):
        index = 21 * i % 55
        values[index] = mk
        mk = mj - mk
        if mk < 0: mk += big
        mj = values[index]
    for _ in range(4):
        for i in range(1, 56):
            values[i] -= values[1 + (i + 30) % 55]
            if values[i] < 0: values[i] += big
    value = values[1] - values[22]
    if value == big: value -= 1
    if value < 0: value += big
    return value / big


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--owner', type=Path, required=True)
    p.add_argument('--manifest-pin', required=True)
    p.add_argument('--receipt', type=Path, required=True)
    args = p.parse_args()
    owner = args.owner.absolute()
    q, declaration = io.read(owner/'request.json'), io.read(owner/'declaration.json')
    output, api = Path(q['output']), Path(q['apiRoot'])
    check = io.require
    check(not args.receipt.exists() and not args.receipt.absolute().is_relative_to(output), 'New receipt outside output required')
    check(q['version'] == owner_module.VERSION and io.sha(owner/'request.json') == declaration['requestSha256'], 'Changed request')
    for k, v in dict(histories=32, scenarios=2, recipes=4, paths=4, maximumAttempts=384, controls=32, maximumReplays=40,
                     maximumFights=30000, maximumEncountersPerHistory=86400, maximumSeconds=900,
                     maximumNativeSeconds=840, maximumBytes=256*1048576, newSeeds=3136, retries=0).items():
        check(declaration[k] == v, 'Changed envelope: '+k)
    for name, pin in q['inputHashes'].items(): check(io.sha(Path(name)) == pin, 'Input drift: '+name)
    check(io.sha(output/'files.json') == args.manifest_pin, 'Wrong manifest pin')
    manifest = io.read(output/'files.json')
    check(set(manifest) == {f.name for f in output.iterdir() if f.name != 'files.json'}, 'Unmanifested output')
    for name, pin in manifest.items(): check(io.sha(io.member(output, name)) == pin, 'Output drift: '+name)
    check(sum(f.stat().st_size for f in output.iterdir()) <= declaration['maximumBytes'], 'Output cap exceeded')
    process = io.read(owner/'process.json')
    check(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Incomplete owned process')
    ledger = io.read(owner/'seed-ledger.json')
    def pinned(part):
        check(io.sha(Path(part['archive'])) == part['sha256'], 'Predecessor changed')
        return io.read(Path(part['archive']))
    h, d = pinned(ledger['historical']), pinned(ledger['dungeon'])
    check(ledger['historical']['sha256'] == owner_module.HISTORICAL_PIN and ledger['dungeon']['sha256'] == owner_module.DUNGEON_PIN, 'Wrong history')
    excluded = set(h['historical'] + h['first'] + h['second'] + d['reserved'])
    for prior in ledger['preceding']: excluded.update(pinned(prior)['reserved'])
    reserved = [i['seed'] for i in q['identities']] + [s for p in q['panels'] for s in [p['run']['layoutSeed'], *p['run']['roomSeeds']]]
    check(len(reserved) == len(set(reserved)) == 3136 and reserved == ledger['reserved'] and not excluded.intersection(reserved)
          and ledger['exclusionUnionCount'] == len(excluded)+3136, 'Invalid exclusion accounting')
    rules = io.read(output/'rules.json'); cadence = rules['cadence']; result = io.read(output/'result.json')
    plan = io.read(Path(q['fixtures'])/'tower-activity.json')
    check(rules['plan'] == plan and plan['checkpoints'] == [2160,8640,25920,86400] and cadence == 10, 'Changed activity plan/cadence')
    check(len(rules['armorPool']) == len(set(rules['armorPool'])) == 42, 'Changed armor pool')
    panels = {(p['path'],p['attempt']):p['run'] for p in q['panels']}
    slot_order = ['Head','Relic','Chest','Necklace','Legs','Ring','MainHand','OffHand']
    def score(item):
        # Production stats are Single values, serialized with their shortest round-trip decimal.
        return sum(struct.unpack('<f',struct.pack('<f',value))[0] * rules['scores'][key] for key,value in item['stats'].items())
    def select(items):
        def best(kind):
            candidates = [i for i in items if i['equipmentType'] == kind]
            return min(candidates, key=lambda i:(-score(i),i['state']['id'])) if candidates else None
        selected = {kind:best(kind) for kind in slot_order[:6]}
        one, off, two = best('OneHanded'), best('OffHand'), best('TwoHanded')
        if two is not None and (one is None or score(two) > score(one)+(score(off) if off else 0)):
            selected['MainHand'] = two
        elif one is not None:
            selected['MainHand'] = one
            selected['OffHand'] = off
        return [dict(slot=slot,data=selected[slot]) for slot in slot_order if selected.get(slot) is not None]
    def battle(name):
        with gzip.open(output/name,'rt',encoding='utf-8') as stream: return json.load(stream)
    def equipment(character): return {e['data']['state']['id']:e['data'] for e in character['equipment']}
    used = set(i['seed'] for i in q['identities'])
    fights = replays = attempts = controls = prepared = awards = completed = encounters = 0
    replay_windows = 0
    expected_keys = set()
    for identity in q['identities']:
        for outcome in plan['idleOutcomes']:
            key = f"{identity['recipe']}--{identity['path']}--{outcome}"
            expected_keys.add(key)
            history = io.read(output/(key+'--history.json')); summary = history['summary']
            check(summary == next(s for s in result['histories'] if s['key']==key), 'Summary mismatch')
            owner_id = history['final']['id']
            check(history['starting'][1]['state']['definitionId'] == rules['armorPool'][int(first_random(identity['seed'])*42)], 'Quest armor outcome was conditioned')
            owned = list(history['starting']); stock = {'sigil_goblin_mines':0,'sigil_forgotten_catacombs':1}
            position = wins = first_drop = moonlit = twilight = quest_at = earned = 0
            window_index = step_index = 0
            control = battle(key+'--control.json.gz'); panel = panels[identity['path'],0]
            audit_run(control,panel); fights += len(control['battles']); controls += 1
            check(summary['controlSucceeded'] == (control['status']=='Completed'), 'Control result changed')
            for b in control['battles']:
                friendly = next(p for p in b['preparedParticipants'] if p['slot']['side']=='Friendly')
                check({e['state']['id']:e for e in friendly['equipment']} == {e['state']['id']:e for e in history['targets']}, 'Rare control changed')
            if identity['path'] == 0:
                replay = battle(key+'--control-replay.json.gz'); check(replay == control,'Control replay mismatch')
                fights += len(replay['battles']); replays += 1
            used.add(panel['layoutSeed']); used.update(b['seed'] for b in control['battles'])
            for cp in history['checkpoints']:
                check(cp['encounter'] in plan['checkpoints'], 'Undeclared checkpoint')
                while position < cp['encounter']:
                    w = history['windows'][window_index]; window_index += 1
                    area = 'region_01_area_06' if first_drop and moonlit >= 5 else 'region_01_area_04'
                    check(w['from']==position and position < w['until'] <= cp['encounter'] and w['area']==area, 'Missing/reordered activity')
                    check(quest_at or w['until']==position+1, 'Quest gate skipped')
                    expected_wins = w['until']-position if outcome=='perfect' else w['until']-position-(w['until']//5-position//5)
                    check(w['victories']==expected_wins, 'Loss scenario changed')
                    for item in w['equipment']:
                        state=item['state']; ticks=int(state['provenance']['awardId'].split(':')[-1])
                        ordinal=(ticks-621355968000000000)//(cadence*10000000)+1
                        check(position < ordinal <= w['until'] and (outcome=='perfect' or ordinal%5 != 0), 'Equipment from an absent/failed encounter')
                        check(state['provenance']['sourceId']==area and state['ownership']['ownerId']==owner_id
                              and state['tier']==1 and state['rank']==0 and item['rarity'] in ['Common','Uncommon','Rare']
                              and .95 <= item['attributeRollMultiplier'] <= 1.05, 'Invalid ordinary award')
                    owned.extend(w['equipment']); wins += expected_wins
                    for name,count in w['sigils'].items(): check(name in stock and count>=0,'Unknown sigil source'); stock[name] += count
                    if not first_drop and w['equipment']:
                        first_drop=w['until']; check(history['gateEquippedItem']==w['equipment'][0]['state']['id'], 'Unfunded equipment quest gate')
                    elif first_drop and moonlit < 5: moonlit += expected_wins
                    elif not quest_at and moonlit >= 5:
                        twilight += expected_wins
                        if twilight == 5: quest_at=w['until']; stock['sigil_goblin_mines'] += 1
                    if w['until']-position>1: replay_windows += 1
                    position=w['until']
                while step_index < len(history['steps']) and history['steps'][step_index]['encounter']==position:
                    step=history['steps'][step_index]
                    check(step['ordinal']==step_index and step_index<12 and earned<7 and quest_at and stock['sigil_goblin_mines']>0, 'Unfunded/unbounded attempt')
                    check(step['before']['equipment']==select(owned) and step['before']['id']==owner_id and step['before']['level']==30, 'Wrong inventory policy/owner')
                    check(len(step['before']['essences'])==4 and all(e['level']==1 and e['ascensionTier']==0 for e in step['before']['essences']), 'Unfunded Essence budget')
                    check(step['sigilsBefore']==stock['sigil_goblin_mines'], 'Invented sigil stock')
                    stock['sigil_goblin_mines']-=1
                    check(step['sigilsAfter']==stock['sigil_goblin_mines'], 'Entry not paid on failure')
                    panel=panels[identity['path'],step_index]; run=battle(step['file']); audit_run(run,panel)
                    check(run['status']==step['status'] and run['combatSeconds']==step['combatSeconds'],'Wrong run summary')
                    used.add(panel['layoutSeed']); used.update(b['seed'] for b in run['battles'])
                    for b in run['battles']:
                        friendly=[p for p in b['preparedParticipants'] if p['slot']['side']=='Friendly']
                        check(len(friendly)==1 and friendly[0]['slot']['sourceEntityId']==owner_id,'Wrong prepared owner')
                        check({e['state']['id']:e for e in friendly[0]['equipment']}==equipment(step['before']),'Production preparation changed earned gear')
                        check(friendly[0]['essences']==[dict(essenceDefinitionId=e['definitionId'],level=1,ascensionTier=0,isEvolved=False) for e in step['before']['essences']], 'Prepared Essences changed')
                        prepared += 1
                    fights += len(run['battles']); attempts += 1
                    if step_index==0:
                        replay=battle(key+'--replay.json.gz'); check(run==replay,'Acquisition replay mismatch')
                        fights += len(replay['battles']); replays += 1
                    if run['status']=='Completed':
                        award=step['award']; check(award is not None,'Missing earned item')
                        expected=copy.deepcopy(history['targets'][earned])
                        expected['state']['id']=award['state']['id']; expected['state']['provenance']['awardId']=f'{key}/{earned+1}'
                        check(award==expected,'Supply award differs from production target')
                        owned.append(award); earned+=1; awards+=1
                    else: check(step['award'] is None,'Failed attempt earned gear')
                    step_index+=1
                check(cp['victories']==wins and cp['ordinaryItems']==len(owned)-2-earned and cp['supplyItems']==earned
                      and cp['sigils']==stock and cp['character']['equipment']==select(owned), 'Checkpoint ledger mismatch')
                check(earned==7 or step_index==12 or not quest_at or stock['sigil_goblin_mines']==0,'Policy left a funded attempt unused')
            check(window_index==len(history['windows']) and step_index==len(history['steps']), 'Unconsumed ledger records')
            check(history['owned']==owned and len({i['state']['id'] for i in owned})==len(owned) and history['final']['equipment']==select(owned),'Lost/duplicated owned equipment')
            check(all(i['state']['ownership']['ownerId']==owner_id for i in owned),'Donated equipment')
            check(len(history['finalPrepared'])==1 and {e['state']['id']:e for e in history['finalPrepared'][0]['equipment']}==equipment(history['final']), 'Final preparation differs')
            check(summary['encounters']==position and summary['victories']==wins and summary['idleCadenceSeconds']==position*cadence
                  and summary['firstDrop']==first_drop and summary['questAt']==quest_at and summary['sigils']==stock
                  and summary['attempts']==step_index and summary['successes']==earned and summary['completed']==(earned==7)
                  and summary['ordinaryItems']==len(owned)-2-earned
                  and math.isclose(summary['combatSeconds'],sum(s['combatSeconds'] for s in history['steps'])), 'Wrong activity summary')
            stop='SevenSupplyItemsEarned' if earned==7 else 'AttemptCap' if step_index==12 else 'ActivityCap'
            check(summary['stop']==stop and (stop!='ActivityCap' or position==86400),'Wrong censoring')
            completed+=int(earned==7); encounters+=position
    check(len(expected_keys)==32 and {h['key'] for h in result['histories']}==expected_keys,'Missing history')
    check(result['fights']==fights<=30000 and result['replays']==replays<=40 and result['attempts']==attempts<=384
          and result['controls']==controls==32 and result['replayedRewardWindows']==replay_windows and result['measuredPlayerSamples']==0,'Work accounting mismatch')
    io.write(args.receipt,dict(status='VerifiedConditionalJointActivityNotPlayerPace',manifestPin=args.manifest_pin,
        inputHashesChecked=len(q['inputHashes']),histories=32,encounters=encounters,attempts=attempts,controls=controls,fights=fights,
        replays=replays,rewardWindowsReplayed=replay_windows,productionPreparedRoomsChecked=prepared,finalPreparationsChecked=32,
        completedHistories=completed,censoredHistories=32-completed,supplyItemsEarned=awards,
        reservedSeeds=len(reserved),usedSeeds=len(used),reservedUnconsumedSeeds=len(set(reserved)-used),exclusionUnionCount=ledger['exclusionUnionCount'],
        measuredPlayerSamples=0,newAuditFights=0))
    print(json.dumps(io.read(args.receipt),indent=2))


if __name__=='__main__': main()
