"""Independent dungeon-equipment audit: native random draws, pending-loss/claim boundaries, owned progression and combat."""
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
spec = importlib.util.spec_from_file_location('activity_owner', Path(__file__).with_name('run-tower-prophecy-combat.py'))
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
    for k, v in dict(histories=64, scenarios=2, recipes=4, paths=4, policies=2, maximumAttempts=768, controls=64, maximumReplays=144,
                     maximumFights=64000, maximumEncountersPerHistory=86400, maximumSeconds=900,
                     maximumNativeSeconds=840, maximumBytes=256*1048576, newSeeds=6240, retries=0).items():
        check(declaration[k] == v, 'Changed envelope: '+k)
    for name, pin in q['inputHashes'].items(): check(io.sha(Path(name)) == pin, 'Input drift: '+name)
    for relative, pin in io.read(owner/'source-compatibility.json').items():
        check(io.sha(owner/'inputs'/relative)==pin, 'Changed compared content/reward source: '+relative)
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
    check(any(part['sha256']==owner_module.SOURCE_LEDGER_PIN for part in ledger['preceding']), 'Missing latest source-policy exclusions')
    check(any(part['sha256']==owner_module.READINESS_LEDGER_PIN for part in ledger['preceding']), 'Missing readiness exclusions')
    check(any(part['sha256']=='c48c5fafa3b176febbf862841425f6fcbffdfd016032ad1ad02f0d459d52f1c9' for part in ledger['preceding']), 'Missing latest dungeon-loot reservations')
    reserved = [s for p in q['panels'] for s in [p['run']['layoutSeed'], *p['run']['roomSeeds']]]
    check(len(reserved) == len(set(reserved)) == 6240 and reserved == ledger['reserved'] and not excluded.intersection(reserved)
          and ledger['exclusionUnionCount'] == len(excluded)+6240, 'Invalid exclusion accounting')
    rules = io.read(output/'rules.json'); cadence = rules['cadence']; result = io.read(output/'result.json')
    check(result['version']==owner_module.VERSION and result['status']=='NativeOfferComparisonComplete', 'Wrong result version/status')
    old_request=io.read(owner/'inputs/TestResults/tower-activity-owner-20260929/request.json')
    check(q['identities']==old_request['identities'], 'Changed paired identity seeds')
    plan = io.read(Path(q['fixtures'])/'tower-activity.json')
    check(rules['plan'] == plan and plan['checkpoints'] == [2160,8640,25920,86400] and cadence == 10, 'Changed activity plan/cadence')
    check(len(rules['armorPool']) == len(set(rules['armorPool'])) == 42, 'Changed armor pool')
    comparison = io.read(Path(q['fixtures'])/'tower-prophecy-combat.json')
    check(rules['comparison']==comparison and comparison['policies']==['no-prophecy-credit','offered-kills-no-reroll'], 'Changed source policies')
    prior_output=Path(q['sourceArchive']); prior_manifest=io.read(prior_output/'files.json')
    check(io.sha(prior_output/'files.json')==owner_module.ACTIVITY_MANIFEST_PIN, 'Wrong historical activity manifest')
    def prior_history(key):
        name=key+'--history.json'
        check(io.sha(prior_output/name)==prior_manifest[name], 'Historical personal history changed')
        return io.read(prior_output/name)
    def choose(policy,stock):
        if stock['sigil_goblin_mines']>0:return 'goblin_mines'
        return 'forgotten_catacombs' if policy in ['no-prophecy-credit','offered-kills-no-reroll'] and stock['sigil_forgotten_catacombs']>0 else None
    def missing_slots(equipped):
        slots={e['slot'] for e in equipped}
        missing=[s for s in ['Head','Chest','Legs','Necklace','Ring','Relic','MainHand'] if s not in slots]
        two=any(e['slot']=='MainHand' and e['data']['equipmentType']=='TwoHanded' for e in equipped)
        if not two and 'OffHand' not in slots: missing.append('OffHand')
        return missing
    def decision(policy,equipped,stock,encounter,quest_at,attempts,earned):
        missing=missing_slots(equipped); family=choose(policy,stock)
        reason='QuestGate' if not quest_at else 'Complete' if earned>=7 else 'AttemptCap' if attempts>=12 else 'EquipmentCoverage' if missing else 'NoSigil' if family is None else 'Enter'
        return dict(encounter=encounter,attemptOrdinal=attempts,missingSlots=missing,reason=reason,dungeon=family if reason=='Enter' else None)
    panels = {(p['run']['dungeon'],p['path'],p['attempt']):p['run'] for p in q['panels']}
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
    offer_auditor = owner_module.module('native_offer_audit', Path(__file__).with_name('audit-prophecy-offers.py')).OfferAudit(api,Path(q['fixtures']))
    offer_totals = dict(days=0,offers=0,claims=0,abstentions=0,extraSigils=0)
    used = set()
    fights = replays = attempts = controls = prepared = awards = completed = encounters = 0
    replay_windows = 0
    expected_keys = set()
    loot_auditor=owner_module.module('loot_audit', Path(__file__).with_name('audit-dungeon-equipment-loot.py')).LootAuditor(api)
    prefixes_checked=paired_matches=decisions_checked=coverage_waits=0
    loot_runs=dungeon_items=lost_items=blueprint_items=loot_retry_checks=0
    matched_inputs={}
    arm_histories={}
    for identity in q['identities']:
        for outcome in plan['idleOutcomes']:
            for policy in comparison['policies']:
                history_key = f"{identity['recipe']}--{identity['path']}--{outcome}"
                key = history_key+'--'+policy
                expected_keys.add(key)
                history = io.read(output/(key+'--history.json')); summary = history['summary']
                check(summary == next(s for s in result['histories'] if s['key']==key), 'Summary mismatch')
                owner_id = history['final']['id']
                old = prior_history(history_key)
                check(summary['policy']==policy and summary['historyKey']==history_key, 'Wrong arm identity')
                check(history['starting']==old['starting'] and history['targets']==old['targets'] and owner_id==old['final']['id'], 'Unmatched starting owner/gear/targets')
                n=min(len(old['windows']),len(history['windows']))
                check(old['windows'][:n]==history['windows'][:n], 'Historical loot prefix changed')
                prefixes_checked+=1
                if history_key in arm_histories:
                    first=arm_histories[history_key]; n=min(len(first['windows']),len(history['windows']))
                    check(first['windows'][:n]==history['windows'][:n], 'Policy altered the underlying reward history')
                else:arm_histories[history_key]=history
                check(history['starting'][1]['state']['definitionId'] == rules['armorPool'][int(first_random(identity['seed'])*42)], 'Quest armor outcome was conditioned')
                owned = list(history['starting']); stock = {'sigil_goblin_mines':0,'sigil_forgotten_catacombs':1}
                position = wins = first_drop = moonlit = twilight = quest_at = earned = 0
                window_index = step_index = decision_index = 0
                blueprint_state=[]; retained_dungeon_items=0
                offer_result=offer_auditor.audit(history['prophecy'],owner_id,outcome,summary['encounters'])
                for field,value in offer_result.items(): offer_totals[field]+=value
                if policy == comparison['policies'][0]:
                    for family in ['goblin_mines','forgotten_catacombs']:
                        control_key=history_key+'--'+family
                        control=battle(control_key+'--control.json.gz'); panel=panels[family,identity['path'],0]
                        audit_run(control,panel); fights+=len(control['battles']); controls+=1
                        if family=='goblin_mines':check(summary['controlSucceeded']==(control['status']=='Completed'),'Control result changed')
                        for b in control['battles']:
                            friendly=next(p for p in b['preparedParticipants'] if p['slot']['side']=='Friendly')
                            check({e['state']['id']:e for e in friendly['equipment']}=={e['state']['id']:e for e in history['targets']},'Rare control changed')
                        if identity['path']==0:
                            replay=battle(control_key+'--control-replay.json.gz'); check(replay==control,'Control replay mismatch')
                            fights+=len(replay['battles']); replays+=1
                        used.add(panel['layoutSeed']); used.update(b['seed'] for b in control['battles'])
                source_ordinals={'goblin_mines':0,'forgotten_catacombs':0}
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
                    source_cp=next(c for c in history['prophecy']['checkpoints'] if c['encounter']==position)
                    if policy=='offered-kills-no-reroll': stock['sigil_goblin_mines']+=source_cp['assembledNow']
                    while step_index < len(history['steps']) and history['steps'][step_index]['encounter']==position:
                        step=history['steps'][step_index]
                        expected_decision=decision(policy,select(owned),stock,position,quest_at,step_index,earned)
                        check(history['decisions'][decision_index]==expected_decision and expected_decision['reason']=='Enter', 'Unready or unlogged entry')
                        decision_index+=1; decisions_checked+=1
                        family=expected_decision['dungeon']; sigil='sigil_'+str(family)
                        check(step['ordinal']==step_index and step_index<12 and earned<7 and quest_at and family is not None, 'Unfunded/unbounded attempt')
                        check(step['dungeon']==family and step['sourceOrdinal']==source_ordinals[family], 'Wrong source/panel ordinal')
                        check(step['before']['equipment']==select(owned) and step['before']['id']==owner_id and step['before']['level']==30, 'Wrong inventory policy/owner')
                        check(len(step['before']['essences'])==4 and all(e['level']==1 and e['ascensionTier']==0 for e in step['before']['essences']), 'Unfunded Essence budget')
                        check(step['sigilsBefore']==stock[sigil], 'Invented sigil stock')
                        stock[sigil]-=1
                        check(step['sigilsAfter']==stock[sigil], 'Entry not paid on failure')
                        panel=panels[family,identity['path'],source_ordinals[family]]; run=battle(step['file']); audit_run(run,panel)
                        check(run['entryItems']=={sigil:1}, 'Spent the wrong entry resource')
                        check(step['before']['essences']==old['final']['essences'] and step['before']['baseAttributes']==old['final']['baseAttributes'], 'Unmatched Essence/base budget')
                        check(run['status']==step['status'] and run['combatSeconds']==step['combatSeconds'],'Wrong run summary')
                        used.add(panel['layoutSeed']); used.update(b['seed'] for b in run['battles'])
                        for b in run['battles']:
                            friendly=[p for p in b['preparedParticipants'] if p['slot']['side']=='Friendly']
                            check(len(friendly)==1 and friendly[0]['slot']['sourceEntityId']==owner_id,'Wrong prepared owner')
                            check({e['state']['id']:e for e in friendly[0]['equipment']}==equipment(step['before']),'Production preparation changed earned gear')
                            check(friendly[0]['essences']==[dict(essenceDefinitionId=e['definitionId'],level=1,ascensionTier=0,isEvolved=False) for e in step['before']['essences']], 'Prepared Essences changed')
                            prepared += 1
                        fights += len(run['battles']); attempts += 1
                        if source_ordinals[family]==0:
                            replay=battle(key+'--'+family+'--replay.json.gz'); check(run==replay,'Acquisition replay mismatch')
                            fights += len(replay['battles']); replays += 1
                        loot=step['dungeonLoot']; check(loot is not None, 'Missing projected dungeon loot')
                        loot_auditor.audit(loot,owner_id,run,blueprint_state)
                        blueprint_state=loot['after']; loot_runs+=1; dungeon_items+=len(loot['equipment']); lost_items+=len(loot['lost'])
                        blueprint_items+=sum(loot['blueprints'].values()); loot_retry_checks+=loot['retryChecks']
                        if policy in ['no-prophecy-credit','offered-kills-no-reroll']:
                            owned.extend(loot['equipment']); retained_dungeon_items+=len(loot['equipment'])
                        if run['status']=='Completed':
                            award=step['award']; check(award is not None,'Missing earned item')
                            expected=copy.deepcopy(history['targets'][earned])
                            expected['state']['id']=award['state']['id']; expected['state']['provenance']['awardId']=f'{history_key}/{earned+1}'
                            check(award==expected,'Supply award differs from production target')
                            owned.append(award); earned+=1; awards+=1
                        else: check(step['award'] is None,'Failed attempt earned gear')
                        match_key=(history_key,family,source_ordinals[family],json.dumps(step['before'],sort_keys=True))
                        if match_key in matched_inputs:
                            check(run==matched_inputs[match_key], 'Equal paired inputs produced different run output'); paired_matches+=1
                        else:matched_inputs[match_key]=run
                        source_ordinals[family]+=1
                        step_index+=1
                    check(cp['victories']==wins and cp['ordinaryItems']==len(owned)-2-earned and cp['supplyItems']==earned
                          and cp['sigils']==stock and cp['character']['equipment']==select(owned), 'Checkpoint ledger mismatch')
                    expected_decision=decision(policy,select(owned),stock,position,quest_at,step_index,earned)
                    check(history['decisions'][decision_index]==expected_decision and expected_decision['reason']!='Enter', 'Missing wait/stop or unused ready entry')
                    coverage_waits+=int(expected_decision['reason']=='EquipmentCoverage')
                    decision_index+=1; decisions_checked+=1
                check(window_index==len(history['windows']) and step_index==len(history['steps']) and decision_index==len(history['decisions']), 'Unconsumed ledger records')
                check(history['owned']==owned and len({i['state']['id'] for i in owned})==len(owned) and history['final']['equipment']==select(owned),'Lost/duplicated owned equipment')
                check(all(i['state']['ownership']['ownerId']==owner_id for i in owned),'Donated equipment')
                check(len(history['finalPrepared'])==1 and {e['state']['id']:e for e in history['finalPrepared'][0]['equipment']}==equipment(history['final']), 'Final preparation differs')
                check(summary['encounters']==position and summary['victories']==wins and summary['idleCadenceSeconds']==position*cadence
                      and summary['firstDrop']==first_drop and summary['questAt']==quest_at and summary['sigils']==stock
                      and summary['attempts']==step_index and summary['successes']==earned and summary['completed']==(earned==7)
                      and summary['ordinaryItems']==len(owned)-2-earned
                      and math.isclose(summary['combatSeconds'],sum(s['combatSeconds'] for s in history['steps'])), 'Wrong activity summary')
                check(summary['dungeonItems']==retained_dungeon_items, 'Wrong retained dungeon item total')
                stop='SevenSupplyItemsEarned' if earned==7 else 'AttemptCap' if step_index==12 else 'ActivityCap'
                check(summary['stop']==stop and (stop!='ActivityCap' or position==86400),'Wrong censoring')
                completed+=int(earned==7); encounters+=position
    check(len(expected_keys)==64 and {h['key'] for h in result['histories']}==expected_keys,'Missing history')
    check(result['fights']==fights<=64000 and result['replays']==replays<=144 and result['attempts']==attempts<=768
          and result['controls']==controls==64 and result['replayedRewardWindows']==replay_windows and result['measuredPlayerSamples']==0,'Work accounting mismatch')
    io.write(args.receipt,dict(status='VerifiedNativeOfferCombatNotPlayerPace',manifestPin=args.manifest_pin,prophecy=offer_totals,
        inputHashesChecked=len(q['inputHashes']),histories=64,dungeonLootRunsChecked=loot_runs,dungeonItemsProjected=dungeon_items,lostPendingItems=lost_items,blueprintItems=blueprint_items,lootRetryChecks=loot_retry_checks,entryDecisionsChecked=decisions_checked,coverageWaits=coverage_waits,historicalLootPrefixesChecked=prefixes_checked,pairedIdenticalRunsChecked=paired_matches,encounters=encounters,attempts=attempts,controls=controls,fights=fights,
        replays=replays,rewardWindowsReplayed=replay_windows,productionPreparedRoomsChecked=prepared,finalPreparationsChecked=64,
        completedHistories=completed,censoredHistories=64-completed,supplyItemsEarned=awards,
        reservedSeeds=len(reserved),usedSeeds=len(used),reservedUnconsumedSeeds=len(set(reserved)-used),exclusionUnionCount=ledger['exclusionUnionCount'],
        measuredPlayerSamples=0,newAuditFights=0))
    print(json.dumps(io.read(args.receipt),indent=2))


if __name__=='__main__': main()
