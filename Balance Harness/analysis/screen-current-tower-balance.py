"""Small current-runtime reference screen; fixed recipes, budgets and historical seeds.

Uses the existing progression screen verifier and owned process runner. This does
not allocate fresh seeds, tune budgets, run a search, or certify a winning team.
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


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    sys.modules[name] = result
    spec.loader.exec_module(result)
    return result


screen = module('current_balance_screen', Path(__file__).with_name('screen-affinity-progression.py'))
io, require = screen.io, screen.require
owner = module('current_balance_owner', ROOT / 'build/bounded_windows_process.py')


def run(args):
    output = io.unlinked(args.output.resolve())
    if args.continue_frozen:
        require(not (output / 'result.json').exists(), 'Screen is already complete')
        require(io.read(output / 'failure.json')['message'] == 'Effective settings changed',
                'Only the verified settings-casing failure can continue')
        io.write(output / 'continuation.json', dict(reason='Normalize settings property casing in verifier; no combat retries.',
                                                  freezeSha256=io.sha(output / 'freeze.json')))
        return execute(output, io.read(output / 'freeze.json'))
    require(not output.exists(), 'Choose a new output directory')
    previous = args.previous.resolve()
    original = io.read(previous / 'freeze.json')
    handoff = io.read(previous / 'follow-up-inputs.json')
    require(handoff['sourceFreezeSha256'] == io.sha(previous / 'freeze.json') and
            handoff['sourceResultSha256'] == io.sha(previous / 'result.json'), 'Changed previous screen')
    settings = io.read(args.settings)
    balance = dict(attributeRulesVersion=settings['AttributeRedesign']['LiveVersion'],
                   equipmentBalanceVersion=settings['EquipmentBalance']['LiveVersion'],
                   abilityBalanceProfile=settings['Combat']['AbilityBalanceProfile'])
    require(balance == dict(attributeRulesVersion=18, equipmentBalanceVersion=4,
                            abilityBalanceProfile='healing-v1'), 'This screen is bound to 18/4/healing-v1')
    seeds = original['seeds']
    require(len(seeds) == len(set(seeds)) == 32, 'Expected the existing diagnostic seed panel')
    output.mkdir()
    runtime = args.runtime.resolve()
    shutil.copytree(runtime, output / 'executable', ignore=shutil.ignore_patterns('Fixtures', '*.pdb'))
    content = ROOT / 'LL/src/API/API.LL'
    files = set(original['scope']['contentHashes'])
    files.update('equipment/' + p.name for p in (content / 'Data/equipment').glob('*.legacy-v1.json'))
    for release in (3, 4):
        files.update(f'equipment/equipment-{kind}.v{release}.json' for kind in ('starters', 'styles', 'sets', 'named'))
    files.update(['equipment/equipment-releases.json', 'combat/ability-balance.healing-v1.json'])
    hashes = {}
    for name in sorted(files):
        src, dst = content / 'Data' / name, output / 'content/Data' / name
        dst.parent.mkdir(parents=True, exist_ok=True)
        expected = io.sha(src)
        shutil.copyfile(src, dst)
        require(io.sha(dst) == expected, 'Content changed during capture')
        hashes[name] = expected
    io.write(output / 'content/appsettings.json', settings)
    cases, prepared = [], []
    for floor in (3, 7, 15):
        selected = next(c for c in original['definition']['cases'] if c['floor'] == floor)
        if floor == 3:
            corrected = next(c for c in handoff['cases'] if c['case']['floor'] == floor)
            selected, recipes = corrected['case'], copy.deepcopy(corrected['scenarios'])
        else:
            recipes = []
            for n in range(1, 4):
                name = f'scenarios/floor-{floor:02d}-reference-{n}.json'
                require(io.sha(previous / name) == original['captured'][name], 'Changed historical recipe')
                recipes.append(io.read(previous / name))
        require(len({screen.essence_sets(s) for s in recipes}) == 3, 'Three distinct compositions required')
        require(all(screen.background(s) == screen.background(recipes[0]) for s in recipes), 'Gear differs between references')
        for n, recipe in enumerate(recipes, 1):
            recipe['seeds'] = seeds
            recipe['assumptions'].append('Current 18/4/healing-v1 diagnostic; fixed historical gear budget; no transferred quality claim.')
            name = f'floor-{floor:02d}-reference-{n}'
            (output / 'scenarios').mkdir(exist_ok=True)
            io.write(output / 'scenarios' / (name + '.json'), recipe)
            prepared.append((name, floor, n, selected['referenceIds'][n-1]))
        cases.append(selected)
    captured = {p.relative_to(output).as_posix(): io.sha(p) for p in output.rglob('*') if p.is_file()}
    expected_assemblies = {name: io.sha(output / 'executable' / (name + '.dll'))
                          for name in ('Application', 'BalanceHarness', 'Common', 'Domain', 'Services.LL')}
    freeze = dict(version='tower-current-balance-screen-v1', balance=balance, cases=cases, seeds=seeds,
                  plannedFights=288, captured=captured, expectedAssemblies=expected_assemblies,
                  previousFreeze=io.sha(previous / 'freeze.json'), previousHandoff=io.sha(previous / 'follow-up-inputs.json'),
                  screenRule='Preselected reference 1: 4 through 28 wins out of 32 flags an informative follow-up case.',
                  limitations=['Descriptive reused seeds; no fresh confirmation.',
                               'Existing progression budgets are provisional, not a player-population target.',
                               'No equipment specialization search; Essence order and character identities preserved.'])
    io.write(output / 'freeze.json', freeze)
    return execute(output, freeze)


def execute(output, freeze):
    settings = io.read(output / 'content/appsettings.json')
    captured, seeds, balance = freeze['captured'], freeze['seeds'], freeze['balance']
    hashes = {name[len('content/Data/'):]: sha for name, sha in captured.items() if name.startswith('content/Data/')}
    expected_assemblies = freeze['expectedAssemblies']
    prepared = [(f"floor-{case['floor']:02d}-reference-{n}", case['floor'], n, source)
                for case in freeze['cases'] for n, source in enumerate(case['referenceIds'], 1)]
    freeze_hash = io.sha(output / 'freeze.json')
    started = time.monotonic()
    rows, execution = [], None
    recovered = 0

    def check():
        require(time.monotonic() - started < 600, 'Ten-minute screen deadline')
        require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) < 1073741824, 'One-GiB screen limit')

    def verify_capture():
        require(io.sha(output / 'freeze.json') == freeze_hash, 'Freeze changed')
        for name, sha in captured.items():
            require(io.sha(output / name) == sha, 'Changed capture: ' + name)

    try:
        verify_capture()
        for name, floor, reference, source in prepared:
            check()
            recipe = output / 'scenarios' / (name + '.json')
            target = output / 'runs' / name
            receipt_path = output / (name + '-process.json')
            if receipt_path.exists():
                receipt = io.read(receipt_path)
                recovered += 1
            else:
                require(not target.exists(), 'Partial cells cannot be retried')
                receipt = owner.run(['dotnet', str(output / 'executable/BalanceHarness.dll'), 'tower',
                                     '--content-root', str(output / 'content'), '--scenario', str(recipe),
                                     '--output', str(target)], ROOT, output / (name + '.log'),
                                    min(started + 598, time.monotonic() + 90), check=check, log_byte_limit=1048576)
                io.write(receipt_path, receipt)
            require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                    'Failed owned screen cell: ' + name)
            manifest = io.read(target / 'tower-manifest.json')
            require(manifest['execution']['assemblyHashes'] == expected_assemblies, 'Changed runtime assemblies')
            execution = execution or manifest['execution']
            frozen = dict(seeds=seeds, scope=dict(execution=execution, contentHashes=hashes,
                          settings=dict(threat={k[0].lower()+k[1:]: v for k, v in settings['Combat']['ThreatAndTanking'].items()},
                                        checkpointIntervalTicks=settings['WorldTower']['CombatTicksPerFrame'])))
            metrics = screen.verify_cell(target, io.read(recipe), frozen)
            require(all(i['balance'] == balance for i in io.read(target / 'tower-input.json')), 'Wrong effective balance')
            row = dict(floor=floor, reference=reference, sourceId=source, **metrics)
            rows.append(row)
            row_path = output / (name + '-result.json')
            if row_path.exists():
                require(io.read(row_path) == row, 'Changed completed cell')
            else:
                io.write(row_path, row)
            print(json.dumps(row), flush=True)
        verify_capture()
        check()
        result = dict(status='ScreenComplete', freezeSha256=freeze_hash, fights=288, newSeeds=0,
                      searchRuns=0, retries=0, recoveredCompletedCells=recovered, execution=execution, elapsedSeconds=time.monotonic()-started,
                      followUpFloors=[r['floor'] for r in rows if r['reference'] == 1 and r['classification'] == 'FollowUpCandidate'], rows=rows)
        io.write(output / 'result.json', result)
        print(json.dumps({k: v for k, v in result.items() if k not in ('rows', 'execution')}), flush=True)
    except Exception as error:
        failure = output / ('continuation-failure.json' if (output / 'failure.json').exists() else 'failure.json')
        io.write(failure, dict(status='Failed', message=str(error), completedCells=len(rows), retries=0))
        raise


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for flag in ('previous', 'runtime', 'settings', 'output'):
        parser.add_argument('--' + flag, required=True, type=Path)
    parser.add_argument('--continue-frozen', action='store_true', help='Verify and reuse completed cells after the settings-casing verifier failure')
    run(parser.parse_args())
