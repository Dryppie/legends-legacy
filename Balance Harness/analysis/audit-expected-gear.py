"""Audit the user-defined expected gear curve against ordinary acquisition channels. Zero combat or grants."""
import argparse
import importlib.util
import json
from pathlib import Path

spec = importlib.util.spec_from_file_location('expected_gear_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
ROOT = Path(__file__).resolve().parents[2]
API = ROOT / 'LL/src/API/API.LL'
FIXTURES = ROOT / 'LL/tools/BalanceHarness/Fixtures'
RARITIES = ['common', 'uncommon', 'rare', 'epic', 'unique', 'legendary']
QUALITIES = ['crude', 'standard', 'fine', 'exceptional', 'masterpiece']


def probability(rarity_weights, quality_weights, rarity, quality, at_least):
    r = RARITIES[RARITIES.index(rarity):] if at_least else [rarity]
    q = QUALITIES[QUALITIES.index(quality):] if at_least else [quality]
    value = sum(rarity_weights[k] for k in r) * sum(quality_weights[k] for k in q)
    io.require(0 <= value <= 1, 'Invalid probability')
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--preview', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    preview_path, output = io.unlinked(args.preview.absolute()), io.unlinked(args.output.absolute())
    io.require(not output.exists(), 'New output required')
    preview = io.read(preview_path)
    io.require(preview['version'] == 'tower-progression-budget-preview-v2'
               and preview['status'] == 'PreparedBudgetNotBalanceEvidence'
               and preview['fights'] == preview['reservedSeeds'] == 0, 'Prepared repeating budget required')
    aliases = dict(draft=FIXTURES/'tower-progression-budget-cycle.json', gearProfiles=FIXTURES/'tower-gear-specialization-screen.json',
                   authoredCurve=FIXTURES/'tower-curve.json', settings=API/'appsettings.json')
    pins = {str(preview_path): io.sha(preview_path), str(Path(__file__).resolve()): io.sha(Path(__file__))}
    for name, digest in preview['sourceHashes'].items():
        path = aliases.get(name, API/'Data'/name)
        io.require(io.sha(path) == digest, 'Changed preview source: ' + name)
        pins[str(path)] = digest
    pool_path = API/'Data/equipment/equipment-ordinary.v1.json'
    pins[str(pool_path)] = io.sha(pool_path)
    pools = {p['equipmentTier']: p for p in io.read(pool_path)}
    prices = {p['tier']: p for p in io.read(API/'Data/equipment/equipment-upgrades.v1.json')['tiers']}
    rows = []
    io.require([f['budget']['priorityFloor'] for f in preview['floors']] == list(range(1,12)), 'Complete declared floors 1–11 required')
    for f in preview['floors']:
        budget = f['budget']; tier = budget['tier']; pool = pools[tier]; price = prices[tier]
        rarity, quality = f['rarity'].lower(), budget['quality'].lower()
        io.require(f['occupiedEquipmentSlots'] == f['characters']*8 and len(f['parties']) == 7, 'Incomplete prepared party coverage')
        for start, field in ((0,'fromRankZero'), (1,'fromRankOne')):
            expected = dict(startingRank=start, parts=sum(price['rankPartCosts'][start:budget['rank']])*f['occupiedEquipmentSlots'],
                cinders=sum(price['rankCinderCosts'][start:budget['rank']])*f['occupiedEquipmentSlots'])
            io.require(f[field] == expected, 'Native reinforcement calculation differs')
        channels = []
        for grade, weights in pool['dungeonEquipment']['rarities'].items():
            exact = probability(weights, pool['dungeonEquipment']['qualities'], rarity, quality, False)
            at_least = probability(weights, pool['dungeonEquipment']['qualities'], rarity, quality, True)
            channels.append(dict(grade=grade, exactPerEquipmentDrop=exact, atLeastPerEquipmentDrop=at_least,
                exactPerCompletionAtMasteryZero=exact*pool['dungeonEquipment']['dropChance'],
                exactPerCompletionAtMasteryTen=exact*min(1,pool['dungeonEquipment']['dropChance']+.5),
                expectedAnyExactTargetDrops=None if exact==0 else f['items']/exact,
                expectedAnyAtLeastTargetDrops=None if at_least==0 else f['items']/at_least))
        rows.append(dict(floor=budget['priorityFloor'], level=budget['characterLevel'], essenceSlots=budget['essenceSlots'],
            tier=tier, rarity=f['rarity'], quality=budget['quality'], rank=budget['rank'], characters=f['characters'],
            baselineItems=f['items'], occupiedSlots=f['occupiedEquipmentSlots'], reinforcementFromRankZero=f['fromRankZero'],
            reinforcementFromRankOne=f['fromRankOne'], channels=channels,
            areaAtLeastTargetPerEquipmentDrop=probability(pool['areaEquipment']['rarities'],pool['areaEquipment']['qualities'],rarity,quality,True)))
    for path,digest in pins.items():
        io.require(io.sha(Path(path))==digest,'Source changed during audit')
    result = dict(version='tower-expected-gear-acquisition-v1',status='AcquisitionRequirementsIdentified',
        gearInterpretation='ExpectedProgressionGear',floors=11,preparedParties=77,fights=0,reservedSeeds=0,
        sourceHashes=pins,rows=rows,
        limitations=[
            'Ordinary equipment channels only; dungeon access and successful clears are not established by nonzero drop probability.',
            'Qualifying rarity/quality counts ignore fit, specialization, style, rolls, duplicates and ownership. They are not complete-loadout forecasts.',
            'Costs describe standalone authored parties, not incremental spend or totals to sum across floors. Retain usable owned gear; never downgrade at a cycle boundary.',
            'No reward/trading network, resource income, elapsed time or acquisition schedule is modeled. No production rates changed.',
            'Numeric balance budgets are legal, but expected ownership at each floor remains unvalidated.'])
    io.write(output,result)
    print(json.dumps(dict(status=result['status'],gearInterpretation=result['gearInterpretation'],floors=11,preparedParties=77,fights=0,reservedSeeds=0,sha256=io.sha(output)),indent=2))


if __name__ == '__main__':
    main()
