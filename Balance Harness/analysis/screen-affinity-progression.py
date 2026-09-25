"""Descriptive reference screen on one captured combat runtime; never a search/confirmation.

Freeze all recipes and reused seeds before invoking the existing tower command.
Source recipes are inputs only: historical quality claims are not imported.
"""
import argparse
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
DEFINITION = ROOT / 'LL/tools/BalanceHarness/Fixtures/tower-affinity-progression-screen.json'
VERSION = 'tower-affinity-progression-screen-v1'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    sys.modules[name] = result
    spec.loader.exec_module(result)
    return result


io = module('progression_screen_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))
require = io.require


def background(scenario):
    """Keep identities, equipment and training; exclude only the searched Essences."""
    party = copy.deepcopy(scenario['party'])
    for member in party:
        member['build'].pop('essenceIds')
    return party


def essence_sets(scenario):
    """Composition-only comparison key; never used to rewrite equipped order."""
    return tuple((p['partySlot'], frozenset(p['build']['essenceIds'])) for p in scenario['party'])


def distinct_retained(study, count=3):
    selected, seen = [], set()
    for build in study['builds']:
        key = essence_sets(build['scenario'])
        if key not in seen:
            selected.append(build['id'])
            seen.add(key)
        if len(selected) == count:
            return selected
    raise ValueError('Insufficient distinct retained compositions')


def progression_recipe(case, choice, curve):
    """Match TowerPartyProgression.Scenarios then TowerPartySelection.Apply.

    Portable choices replace absolute slots 1-4, NOT every repeated cell.
    IdentityEssenceIds retain the authored profiles, as in the native path.
    """
    budget = case['budget']
    prefix = f"slots-{budget['essenceSlots']}-"
    profiles = {p['id']: p['build'] for p in curve['profiles']}
    roles = ['guardian', 'restorer', 'striker', 'striker', 'controller']
    party = []
    require(set(choice['builds']) == {'1', '2', '3', '4'}, 'Expected absolute first-cell controls')
    for slot in range(1, case['partySize'] + 1):
        profile = prefix + roles[(slot - 1) % 5]
        build = copy.deepcopy(profiles[profile])
        build.update({k: budget[k] for k in ('characterLevel', 'tier', 'rank', 'quality')})
        build['id'] = f"tower-party-slots-{budget['essenceSlots']}.balanced.slot-{slot}.{profile}"
        build['identityEssenceIds'] = list(build['essenceIds'])
        for equipment in build['equipment']:
            equipment.setdefault('activeStyleId', None)
        if str(slot) in choice['builds']:
            build['essenceIds'] = list(choice['builds'][str(slot)])
        party.append(dict(partySlot=slot, build=build))
    return dict(schemaVersion=1, id=f"floor-{case['floor']}.balanced", floorNumber=case['floor'],
                startsAt=curve['startsAt'], preparationState='uncleared-no-contributions',
                assumptions=['Existing progression cell and fixed absolute-slot controls; hypothetical ownership.',
                             'Provisional progression budget; level-1 unascended/unevolved Essences; no styles.'],
                seeds=[], party=party)


def scenarios(case, catalog, curve, seeds):
    if case['sourceKind'] == 'retained':
        study = next(s for s in catalog['studies'] if s['id'] == case['studyId'])
        choices = {b['id']: b['scenario'] for b in study['builds']}
    elif case['sourceKind'] == 'boss':
        entry = next(e for e in catalog['entries'] if e['floor'] == case['floor'])
        require(entry['anchorId'] == case['referenceIds'][0], 'Changed preselected anchor')
        choices = {e['id']: e['targetRecipe'] for e in entry['evidence']}
    else:
        require(case['sourceKind'] == 'progression', 'Unknown reference source')
        choices = {p['id']: progression_recipe(case, p, curve)
                   for p in catalog['parties'][str(case['budget']['essenceSlots'])]}
    result = []
    for reference in case['referenceIds']:
        s = copy.deepcopy(choices[reference])
        s['seeds'] = list(seeds)
        s['assumptions'] = s['assumptions'] + ['Descriptive screen only; historical seeds reused; no transferred confirmation.']
        require(s['floorNumber'] == case['floor'] and len(s['party']) == case['partySize'], 'Wrong encounter')
        require([p['partySlot'] for p in s['party']] == list(range(1, case['partySize'] + 1)), 'Wrong party slots')
        for p in s['party']:
            b = p['build']
            require(all(b[k] == case['budget'][k] for k in ('characterLevel', 'tier', 'rank', 'quality')),
                    'Reference progression mismatch')
            require(len(b['essenceIds']) == len(set(b['essenceIds'])) == case['budget']['essenceSlots'],
                    'Reference Essence budget mismatch')
        result.append(s)
    require(len(result) == 3 and len(set(case['referenceIds'])) == 3, 'Exactly three distinct references required')
    require(all(background(s) == background(result[0]) for s in result), 'References differ outside Essences')
    loadouts = [json.dumps([p['build']['essenceIds'] for p in s['party']]) for s in result]
    require(len(set(loadouts)) == 3, 'Duplicate complete reference loadouts')
    return result


def classify(wins):
    require(type(wins) is int and 0 <= wins <= 32, 'Invalid screen count')
    return 'LowWinReference' if wins < 4 else 'CeilingReference' if wins > 28 else 'FollowUpCandidate'


def prepare(output):
    definition = io.read(DEFINITION)
    require(definition['version'] == VERSION and [c['floor'] for c in definition['cases']] == [3, 7, 8, 10, 13, 15],
            'Use the fixed six-case screen')
    sources = {str(DEFINITION.relative_to(ROOT).as_posix()): io.sha(DEFINITION)}
    archive = io.member(ROOT, definition['archive'])
    manifest = io.read(archive / 'files.json')
    require(io.sha(archive / 'files.json') == definition['archiveManifestSha256'], 'Changed captured archive manifest')

    def pinned(relative, expected, root=ROOT):
        path = io.member(root, relative)
        require(io.sha(path) == expected, 'Changed input: ' + relative)
        value = io.read(path)
        require(io.sha(path) == expected, 'Input changed during read: ' + relative)
        sources[path.relative_to(ROOT).as_posix()] = expected
        return value

    scope = pinned('search/root-01/control/scope.json', manifest['search/root-01/control/scope.json'], archive)
    family = pinned('study/freeze.json', manifest['study/freeze.json'], archive)['families'][0]
    seeds = family['seeds'][:32]
    require(family['root'] == 1 and len(seeds) == len(set(seeds)) == 32, 'Invalid reused seed panel')
    curve = pinned(definition['curve']['path'], definition['curve']['sha256'])
    catalogs = {s['path']: pinned(s['path'], s['sha256']) for s in definition['sources']}
    floors = pinned('content/Data/world-tower/tower-floors.json', manifest['content/Data/world-tower/tower-floors.json'], archive)
    required = {f['floorNumber']: f['requiredSlots'] for f in floors['floors']}
    prepared = []
    for case in definition['cases']:
        require(required[case['floor']] == case['partySize'], 'Wrong released party size')
        for index, scenario in enumerate(scenarios(case, catalogs[case['source']], curve, seeds)):
            prepared.append((f"floor-{case['floor']:02d}-reference-{index + 1}", scenario))

    sources[(archive / 'files.json').relative_to(ROOT).as_posix()] = definition['archiveManifestSha256']
    # A new directory is also the no-resume/no-overwrite guard.
    output = io.unlinked(output)
    output.mkdir()
    captured = {}
    for name, expected in manifest.items():
        if not name.startswith(('executable/', 'content/')):
            continue
        src, dst = io.member(archive, name), io.member(output, name)
        require(io.sha(src) == expected, 'Changed captured runtime/content: ' + name)
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src, dst)
        require(io.sha(dst) == expected, 'Copy mismatch: ' + name)
        captured[name] = expected
    settings = {'Combat': {'ThreatAndTanking': scope['settings']['threat'],
                           'IdleProgression': {'EncounterCadenceSeconds': 1}},
                'WorldTower': {'CombatTicksPerFrame': scope['settings']['checkpointIntervalTicks']}}
    io.write(output / 'content/appsettings.json', settings)
    captured['content/appsettings.json'] = io.sha(output / 'content/appsettings.json')
    (output / 'scenarios').mkdir()
    for name, scenario in prepared:
        io.write(output / 'scenarios' / (name + '.json'), scenario)
        captured['scenarios/' + name + '.json'] = io.sha(output / 'scenarios' / (name + '.json'))
    io.write(output / 'freeze.json', dict(version=VERSION, definition=definition, sources=sources,
             captured=captured, scope=scope, seeds=seeds, plannedFights=576,
             interpretation='Descriptive reference suitability only; no search, fresh seeds or confirmation.',
             settingsNote='Idle cadence is a required parser field unused by Tower; effective Tower settings are checked per input.'))
    return output


def verify_cell(path, scenario, frozen):
    score = io.read(path / 'scorecard.json')
    manifest = io.read(path / 'tower-manifest.json')
    hashes = io.read(path / 'tower-results.json')
    inputs = io.read(path / 'tower-input.json')
    require(manifest['execution'] == frozen['scope']['execution'], 'Runtime identity differs from captured source')
    require(manifest['contentHashes'] == frozen['scope']['contentHashes'], 'Combat content changed')
    for name, expected in manifest['contentHashes'].items():
        require(io.sha(io.member(path / 'content/Data', name)) == expected, 'Cell content hash mismatch')
    seeds = frozen['seeds']
    require(len(inputs) == 32 and [i['rules']['randomSeed'] for i in inputs] == seeds, 'Input schedule mismatch')
    for i in inputs:
        # Native serialization may add optional defaults; compare every supplied scenario field.
        require(all(i['scenario'].get(k) == v for k, v in scenario.items()), 'Materialized recipe changed')
        require(i['threatAndTanking'] == frozen['scope']['settings']['threat'] and
                i['checkpointIntervalTicks'] == frozen['scope']['settings']['checkpointIntervalTicks'], 'Effective settings changed')
    require(score['status'] == 'Complete' and score['planned'] == score['valid'] == 32 and
            score['invalid'] == score['cancelled'] == score['notRun'] == 0, 'Incomplete screen cell')
    trials = score['trials']
    ids = [f'tower.{n:04d}' for n in range(1, 33)]
    require([t['id'] for t in trials] == ids and [t['seed'] for t in trials] == seeds and set(hashes) == set(ids),
            'Report schedule mismatch')
    wins, defeats, draws, limits, health = 0, 0, 0, 0, []
    for seed, trial in zip(seeds, trials):
        file = path / 'battles' / (trial['id'] + '.json')
        require(io.sha(file) == hashes[trial['id']], 'Battle hash mismatch')
        report = io.read(file)
        summary = report['battle']['summary']
        require(report == trial['report'] and report['battle']['seed'] == seed and
                report['battle']['scenarioId'] == scenario['id'], 'Battle identity mismatch')
        require(report['succeeded'] == (summary['contentOutcome'] == 'Victory'), 'Outcome mismatch')
        wins += report['succeeded']
        defeats += summary['contentOutcome'] == 'Defeat'
        draws += summary['contentOutcome'] == 'Draw'
        limits += summary['terminationReason'] == 'TickLimit'
        health.append(report['guardianHealthRemainingPercent'])
    require((score['wins'], score['defeats'], score['draws'], score['tickLimits']) == (wins, defeats, draws, limits),
            'Scorecard counts disagree with reports')
    return dict(wins=wins, defeats=defeats, draws=draws, tickLimits=limits,
                meanGuardianHealthRemainingPercent=sum(health) / 32, classification=classify(wins))


def execute(output):
    frozen = io.read(output / 'freeze.json')
    freeze_hash = io.sha(output / 'freeze.json')
    owner = module('progression_screen_owner', ROOT / 'build/bounded_windows_process.py')
    started = time.monotonic()
    deadline = started + 600
    rows = []

    def check():
        require(time.monotonic() < deadline, 'Ten-minute screen deadline reached')
        require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) < 768 * 1048576, 'Screen output exceeded 768 MiB')

    def verify_captured():
        require(io.sha(output / 'freeze.json') == freeze_hash, 'Freeze changed')
        for name, expected in frozen['captured'].items():
            require(io.sha(io.member(output, name)) == expected, 'Frozen file changed: ' + name)

    verify_captured()
    (output / 'runs').mkdir()
    try:
        for case in frozen['definition']['cases']:
            for index, reference in enumerate(case['referenceIds']):
                check()
                name = f"floor-{case['floor']:02d}-reference-{index + 1}"
                scenario_path = output / 'scenarios' / (name + '.json')
                run = output / 'runs' / name
                receipt = owner.run(['dotnet', str(output / 'executable/BalanceHarness.dll'), 'tower',
                                     '--content-root', str(output / 'content'), '--scenario', str(scenario_path),
                                     '--output', str(run)], ROOT, output / (name + '.log'),
                                    min(deadline - 2, time.monotonic() + 90), check=check, log_byte_limit=1048576)
                io.write(output / (name + '-process.json'), receipt)
                require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                        'Failed owned cell: ' + name)
                metrics = verify_cell(run, io.read(scenario_path), frozen)
                row = dict(floor=case['floor'], reference=index + 1, sourceId=reference, **metrics)
                rows.append(row)
                io.write(output / (name + '-result.json'), row)
                print(json.dumps(row), flush=True)
        verify_captured()
        check()
        result = dict(version=VERSION, status='ScreenComplete', freezeSha256=freeze_hash, fights=576,
                      newSeeds=0, retries=0, searchRuns=0, elapsedSeconds=time.monotonic() - started,
                      followUpFloors=[r['floor'] for r in rows if r['reference'] == 1 and r['classification'] == 'FollowUpCandidate'],
                      rows=rows)
        io.write(output / 'result.json', result)
        text = ['# Affinity progression reference screen', '',
                'Descriptive only. All 18 recipes were frozen before combat; 32 historical seeds per recipe.',
                'The first reference was preselected. Its 4–28 wins flag is a screening rule, not a balance target or strength test.', '',
                '| Floor | Reference | Wins / 32 | Boss health remaining (mean %) | Screen classification |',
                '| --- | --- | --- | --- | --- |']
        text += [f"| {r['floor']} | {r['reference']} | {r['wins']} | {r['meanGuardianHealthRemainingPercent']:.2f} | {r['classification']} |" for r in rows]
        text += ['', 'Follow-up floors from preselected references: ' + str(result['followUpFloors']),
                 'This screen does not run the search algorithm or promote a team. Captured combat predates concurrent armor changes.']
        (output / 'report.md').write_text('\n'.join(text) + '\n', encoding='utf-8')
    except Exception as error:
        io.write(output / 'failure.json', dict(status='Failed', errorType=type(error).__name__, message=str(error),
                                             completedCells=len(rows), retries=0))
        raise


def export_follow_up(output):
    """Seed-free candidate inputs, not an admitted search or a quality claim.

    Keep the completed screen immutable. For a subsequent floor-3 evaluation,
    skip order-only duplicate controls using saved catalog order, never scores.
    """
    output = io.unlinked(output)
    result = io.read(output / 'result.json')
    frozen = io.read(output / 'freeze.json')
    require(result['status'] == 'ScreenComplete' and result['freezeSha256'] == io.sha(output / 'freeze.json'),
            'Missing matching completed screen')
    diversity, cases = [], []
    for case in frozen['definition']['cases']:
        recipes = []
        for n in range(1, 4):
            name = f"scenarios/floor-{case['floor']:02d}-reference-{n}.json"
            require(io.sha(output / name) == frozen['captured'][name], 'Changed frozen recipe')
            recipes.append(io.read(output / name))
        keys = [essence_sets(s) for s in recipes]
        diversity.append(dict(floor=case['floor'], distinctCompositions=len(set(keys)),
                              sameEssenceSets=[[i+1, j+1] for i in range(3) for j in range(i+1, 3) if keys[i] == keys[j]]))
        if case['floor'] not in result['followUpFloors']:
            continue
        selected = copy.deepcopy(case)
        if case['sourceKind'] == 'retained':
            source = io.member(ROOT, case['source'])
            require(io.sha(source) == frozen['sources'][case['source']], 'Changed retained inputs')
            catalog = io.read(source)
            study = next(s for s in catalog['studies'] if s['id'] == case['studyId'])
            selected['referenceIds'] = distinct_retained(study)
            require(selected['referenceIds'][0] == case['referenceIds'][0], 'Follow-up changed the anchor')
            selected['selectionRule'] = 'First three distinct per-character Essence sets in saved study order; equipped order preserved.'
            recipes = scenarios(selected, catalog, None, [])
        for recipe in recipes:
            recipe['seeds'] = []
        require(len({essence_sets(s) for s in recipes}) == 3, 'Follow-up needs three distinct compositions')
        cases.append(dict(case=selected, scenarios=recipes,
                          note='Seed-free candidate inputs only. Rebind and verify the eventual search runtime and identities before allocating fresh evaluation seeds.'))
    handoff = dict(version=VERSION, purpose='Candidate inputs for a subsequent unchanged-baseline evaluation; zero new fights.',
                   sourceResultSha256=io.sha(output / 'result.json'), sourceFreezeSha256=result['freezeSha256'],
                   screenDiversity=diversity, cases=cases,
                   limitations=['The corrected floor-3 third composition has not been screened in this run.',
                                'Floor 15 uses a provisional late budget and an authored anchor weaker than its saved controls in this screen.',
                                'Picking an existing control is not evidence of better generated candidates.',
                                'Captured runtime predates concurrent armor changes; no current-combat quality claim.'])
    io.write(output / 'follow-up-inputs.json', handoff)
    return handoff


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True, help='New output directory with existing parent')
    parser.add_argument('--execute', action='store_true', help='Execute the fixed 576-fight descriptive screen after freezing')
    args = parser.parse_args()
    output = prepare(args.output)
    print('Frozen screen: ' + str(output), flush=True)
    if args.execute:
        execute(output)
        export_follow_up(output)


if __name__ == '__main__':
    main()
