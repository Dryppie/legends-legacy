"""Independent file-only audit: reservations, retained gear, route choices, Vigor, outcomes and simulated time."""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import sys

spec = importlib.util.spec_from_file_location('dungeon_launch', Path(__file__).with_name('qualify-dungeon-acquisition.py'))
launch = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = launch
spec.loader.exec_module(launch)
io = launch.io


def require(condition, message):
    io.require(condition, message)


def audit_run(run, panel, mastery_level=0):
    rooms = {r['roomIndex']: r for r in run['layout']['rooms']}
    nodes = {r['roomIndex']: r for r in run['layout']['mapNodes']}
    require(run['layoutSeed'] == panel['layoutSeed'] and run['dungeon'] == panel['dungeon'], 'Wrong run seed/source')
    require(run['sigilsConsumed'] == 1 and list(run['entryItems'].values()) == [1], 'Entry not charged once')
    require(len(run['actions']) <= 64 and len(run['battles']) <= 64, 'Run bound exceeded')
    current, vigor, combat_index = 0, 100, 0
    histories = []
    round_away = lambda n: math.floor(n + .5)
    reduction = int(mastery_level >= 4) + int(mastery_level >= 9)
    recovery = 15 + 2 * int(mastery_level >= 2) + 2 * int(mastery_level >= 7)
    scale = lambda n: max(0, round_away(max(0, n) * .85) - reduction)
    for index, action in enumerate(run['actions']):
        options = [n for n in nodes[current]['nextRoomIndexes'] if rooms[n]['type'] != 'Treasury']

        def route_key(room):
            kind, node = rooms[room]['type'], nodes[room]
            low, high = (0, 0) if kind == 'Boss' else (scale(node['vigorCostMin']), scale(node['vigorCostMax']))
            if kind != 'Boss' and vigor <= 40:
                low, high = max(0, low - 2), min(35, high + 2)
            return (0 if kind == 'RestSite' else 1, high, low, room)

        require(options and action['room'] == min(options, key=route_key), 'Route differs from frozen visible-information policy')
        current = action['room']
        node, room = nodes[current], rooms[current]
        require(action['vigorBefore'] == vigor and action['type'] == room['type'] and action['action'] == 'choose_route', 'Bad action sequence')
        require(action['route'] == f"route:{current}:{node['id']}", 'Wrong route identity')
        before = vigor
        if room['type'] == 'RestSite':
            vigor = min(100, vigor + recovery)
            histories.append(dict(roomIndex=current, amount=vigor-before, vigorAfter=vigor, reason='Rest Site recovery'))
        else:
            battle = run['battles'][combat_index]
            combat_index += 1
            summary = battle['summary']
            require(battle['seed'] == panel['roomSeeds'][current] and battle['scenarioId'] == f"{panel['dungeon']}/room-{current}", 'Unreserved fight')
            require(0 <= summary['durationTicks'] <= 6000 and math.isclose(summary['durationSeconds'], summary['durationTicks']/battle['ticksPerSecond']), 'Wrong combat seconds')
            if summary['contentOutcome'] != 'Victory':
                require(index == len(run['actions']) - 1 and run['status'] == 'Failed' and run['failure'] == 'Combat Readiness', 'Combat failure not terminal')
            elif room['type'] != 'Boss':
                team = summary['friendly']
                health = sum(min(max(p['health'], 0), max(p['maxHealth'], 0)) for p in team)
                maximum = max(1, sum(max(p['maxHealth'], 0) for p in team))
                minimum = node['vigorCostMin'] if node['vigorCostMin'] > 0 else 12
                maximum_toll = node['vigorCostMax'] if node['vigorCostMax'] >= minimum else max(minimum, 22)
                toll = min(35, scale(minimum + round_away((maximum_toll-minimum) * (1-health/maximum))))
                vigor = max(0, vigor-toll)
                histories.append(dict(roomIndex=current, amount=vigor-before, vigorAfter=vigor, reason='Combat toll'))
                if vigor == 0:
                    require(index == len(run['actions'])-1 and run['status'] == 'Failed' and run['failure'] == 'Attrition', 'Attrition not terminal')
        require(action['vigorAfter'] == vigor, 'Vigor arithmetic mismatch')
        require(action['status'] == (run['status'] if index == len(run['actions'])-1 else 'Active'), 'Premature terminal status')
    require(combat_index == len(run['battles']) and histories == run['vigorHistory'], 'Missing battle/Vigor event')
    completed = run['status'] == 'Completed'
    require(run['status'] in ('Completed', 'Failed') and run['completionCallbacks'] == int(completed), 'Supply completion boundary mismatch')
    if completed:
        require(rooms[current]['type'] == 'Boss' and not nodes[current]['nextRoomIndexes']
                and run['battles'][-1]['summary']['contentOutcome'] == 'Victory' and vigor > 0 and run['failure'] is None, 'Invalid completed run')
    ticks = sum(b['summary']['durationTicks'] for b in run['battles'])
    seconds = sum(b['summary']['durationSeconds'] for b in run['battles'])
    require(ticks == run['combatTicks'] and math.isclose(seconds, run['combatSeconds']), 'Combat duration total mismatch')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--owner', type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    owner = io.unlinked(args.owner.absolute())
    q, declaration = io.read(owner/'request.json'), io.read(owner/'declaration.json')
    output = io.unlinked(Path(q['output']))
    require(not args.receipt.exists() and not args.receipt.absolute().is_relative_to(output), 'New receipt outside archive required')
    require(io.sha(owner/'request.json') == declaration['requestSha256'] and q['version'] == launch.VERSION, 'Changed frozen declaration')
    for name, expected in dict(cells=52, samples=8, attempts=416, qualificationReplays=52, newSeeds=1040, retries=0,
                               maximumFights=30000, maximumActionsPerRun=64, maximumSeconds=900, maximumNativeSeconds=840,
                               maximumBytes=256*1048576).items():
        require(declaration[name] == expected, 'Changed envelope: ' + name)
    for name, digest in q['inputHashes'].items():
        require(io.sha(Path(name)) == digest, 'Changed frozen input: ' + name)
    require(io.sha(output/'files.json') == args.manifest_pin, 'Output manifest pin mismatch')
    manifest = io.read(output/'files.json')
    require(set(manifest) == {p.name for p in output.iterdir() if p.name != 'files.json'}, 'Unmanifested output')
    for name, digest in manifest.items():
        require(io.sha(io.member(output, name)) == digest, 'Changed output: ' + name)
    process = io.read(owner/'process.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Incomplete process')
    ledger = io.read(owner/'seed-ledger.json')
    historical = io.read(Path(ledger['historicalPath']))
    require(io.sha(Path(ledger['historicalPath'])) == launch.LEDGER_PIN, 'Historical ledger changed')
    excluded = set(historical['historical'] + historical['first'] + historical['second'])
    for name in ledger['previousReservations']:
        excluded.update(io.read(Path(name))['reserved'])
    reserved = [s for p in q['panels'] for s in [p['layoutSeed'], *p['roomSeeds']]]
    require(reserved == ledger['reserved'] and len(set(reserved)) == 1040 and not excluded.intersection(reserved)
            and ledger['exclusionUnionCount'] == len(excluded)+1040, 'Overlapping/unretained reservations')
    require(len(q['panels']) == 16 and all(len(p['roomSeeds']) == 64 for p in q['panels']), 'Changed panel')
    acquisition = io.read(Path(q['acquisitionReport']))
    require(io.sha(Path(q['acquisitionReport'])) == launch.ACQUISITION_PIN, 'Changed earned source')
    rows = {r['floor']: r for r in acquisition['floors']}
    cells = io.read(output/'cells.json')
    require(len(cells) == len({c['id'] for c in cells}) == 26, 'Changed cohort')
    starters = next(s['builds'] for s in io.read(Path(q['fixtures'])/'idle-first-hunt.json')['stages'] if s['id'] == 'first-hunt')
    expected_ids = {'starter-'+s['id'] for s in starters}
    for floor, prior in ((1, 1), (4, 3), (7, 6), (10, 9), (11, 10)):
        for slot in (1, 2, 3, 5):
            identifier = f"{'post' if floor == 1 else 'pre'}-floor-{floor:02}-slot-{slot}"
            expected_ids.add(identifier)
            character = next(c['character'] for c in cells if c['id'] == identifier)
            expected = next(m['character'] for m in rows[floor]['members'] if m['ownerKey'] == f'cohort-{slot:02}').copy()
            expected['equipment'] = next(m['character']['equipment'] for m in rows[prior]['members'] if m['ownerKey'] == f'cohort-{slot:02}')
            require(character == expected, 'Pre-upgrade gear, owner identity or ordered Essences changed: ' + identifier)
    require({c['id'] for c in cells} == expected_ids, 'Missing/extra loadout')
    for source in starters:
        character = next(c['character'] for c in cells if c['id'] == 'starter-'+source['id'])
        require(character['level'] == 30 and len(character['equipment']) == 1 and len(character['essences']) == 1, 'Starter changed')
        item = character['equipment'][0]['data']
        require(item['state']['definitionId'] == source['equipment'][0]['definitionId'] and item['state']['tier'] == 1
                and item['state']['rank'] == 0 and item['rarity'] == 'Common'
                and character['essences'][0]['definitionId'] == source['essenceIds'][0], 'Starter owns unearned supply')
    result = io.read(output/'result.json')
    require(result['status'] == 'DiagnosticCompleteNotPaceAcceptance' and len(result['index']) == 416 and len(result['scores']) == 52, 'Incomplete result')
    fights = replays = successes = 0
    used = set()
    for cell in cells:
        for dungeon in ('goblin_mines', 'forgotten_catacombs'):
            panels = [p for p in q['panels'] if p['dungeon'] == dungeon]
            runs = []
            for sample, panel in enumerate(panels):
                name = f"{cell['id']}--{dungeon}--{sample:02}.json"
                run = io.read(output/name)
                audit_run(run, panel)
                index = next(r for r in result['index'] if r['cell'] == cell['id'] and r['dungeon'] == dungeon and r['sample'] == sample)
                require(index['file'] == name and index['status'] == run['status'] and index['fights'] == len(run['battles']), 'Index mismatch')
                fights += len(run['battles'])
                used.add(panel['layoutSeed'])
                used.update(b['seed'] for b in run['battles'])
                if sample == 0:
                    replay = io.read(output/f"{cell['id']}--{dungeon}--replay.json")
                    require(replay == run, 'Full replay mismatch')
                    fights += len(replay['battles'])
                    replays += 1
                runs.append(run)
            score = next(s for s in result['scores'] if s['cell'] == cell['id'] and s['dungeon'] == dungeon)
            wins = [r for r in runs if r['status'] == 'Completed']
            failures = [r for r in runs if r['status'] == 'Failed']
            successes += len(wins)
            require(score['successes'] == len(wins) and score['failures'] == len(failures) and score['attempts'] == score['sigilsConsumed'] == 8
                    and score['observedProbability'] == len(wins)/8 and score['measuredPlayerSamples'] == 0 and score['sevenItemAttempts'] is None
                    and score['successCombatSeconds'] == [r['combatSeconds'] for r in wins]
                    and score['failureCombatSeconds'] == [r['combatSeconds'] for r in failures], 'Score mismatch')
    require(result['fights'] == fights <= 30000 and result['replays'] == replays == 52, 'Work accounting mismatch')
    io.write(args.receipt, dict(status='VerifiedDiagnosticNotPaceAcceptance', manifestPin=args.manifest_pin,
        inputHashesChecked=len(q['inputHashes']), attempts=416, successfulRuns=successes, failedRuns=416-successes,
        fights=fights, replays=replays, reservedSeeds=len(reserved), usedSeeds=len(used),
        reservedUnconsumedSeeds=len(set(reserved)-used), exclusionUnionCount=ledger['exclusionUnionCount'],
        measuredPlayerSamples=0, newAuditFights=0))
    print(json.dumps(io.read(args.receipt), indent=2))


if __name__ == '__main__':
    main()
