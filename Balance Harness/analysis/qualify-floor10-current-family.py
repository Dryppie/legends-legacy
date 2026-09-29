"""Qualify the exact accepted floor-10 family on current content and binaries.

No game edits or fresh seeds. Twenty-three frozen historical replays cover every
recipe and both observed outcomes of the two recipes with wins. Failures are
preserved; this owner cannot resume or overwrite a qualification.
"""
import argparse
import gzip
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT/'TestResults/balance/tower-floor10-family-confirmation-20260928'
SOURCE_PIN = '95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944'
RESULT_PIN = 'b2fd5bbbf849d15699f8a66907f68e1c16b53e37837e4cc4e8a71842dfaa7be5'
API = ROOT/'LL/src/API/API.LL'
FLOOR = 'world-tower/tower-floors.json'
ITEMS = 'items/items.json'
ADDED_ITEMS = {'item.tower_supply.v1.floor_'+n for n in ['01', '04', '07', '10', '11', '14']}


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor10_qualification_io', Path(__file__).with_name('run-tower-balance-pass.py'))


def validate_content():
    scope = io.read(SOURCE/'scope.json')
    for name, pin in scope['contentHashes'].items():
        old, current = SOURCE/'content/Data'/name, API/'Data'/name
        io.check(io.sha(old) == pin, 'Changed archived content: '+name)
        if name == FLOOR:
            a, b = io.read(old), io.read(current)
            io.check({k: v for k, v in a.items() if k != 'floors'} ==
                     {k: v for k, v in b.items() if k != 'floors'}, 'Shared Tower metadata changed')
            io.check(next(f for f in a['floors'] if f['floorNumber'] == 10) ==
                     next(f for f in b['floors'] if f['floorNumber'] == 10), 'Target floor changed')
        elif name == ITEMS:
            a = {x['id']: x for x in io.read(old)}
            b = {x['id']: x for x in io.read(current)}
            io.check(b.keys()-a.keys() == ADDED_ITEMS and all(b.get(k) == v for k, v in a.items()),
                     'Only the six known additions are permitted; existing items must match')
        else:
            io.check(io.sha(current) == pin, 'Unexpected current catalog change: '+name)


def select_replays(cells, trials):
    selected = []
    for cell in cells:
        stage = cell['case']+'/'+cell['profile']
        outcomes = [True, False] if stage in ['floor10-retained-1/armor-and-health',
                                             'floor10-retained-2/armor-and-health'] else [None]
        for wanted in outcomes:
            for trial in (t for t in trials if t['stage'] == stage):
                path = SOURCE/'study/battles'/(trial['id']+'.json.gz')
                battle = json.loads(gzip.decompress(path.read_bytes()))
                if wanted is None or battle['succeeded'] == wanted:
                    selected.append(dict(id=trial['id'], stage=stage, seed=trial['seed'], succeeded=battle['succeeded']))
                    break
            else:
                raise ValueError('Missing declared replay: '+stage)
    io.check(len(selected) == len({x['id'] for x in selected}) == 23, 'Incomplete replay selection')
    return selected


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['artifacts', 'owner', 'output', 'protocol']:
        parser.add_argument('--'+name, type=Path, required=True)
    parser.add_argument('--initial-exclusions', type=int, required=True)
    args = parser.parse_args()
    artifacts, owner, output, protocol = [p.resolve() for p in [args.artifacts, args.owner, args.output, args.protocol]]
    io.check(owner.parent == output.parent == artifacts.parent == ROOT/'TestResults', 'Direct TestResults children required')
    io.check(not owner.exists() and not output.exists(), 'Fresh qualification paths required')
    io.check(io.sha(SOURCE/'files.json') == SOURCE_PIN and io.sha(SOURCE/'result.json') == RESULT_PIN, 'Changed accepted source')
    io.authenticate(SOURCE)
    audit_path = ROOT/'TestResults/tower-floor10-family-confirmation-owner-20260928/independent-audit.json'
    audit = io.read(audit_path)
    io.check(audit['status'] == 'Verified' and audit['assessment'] == 'Pass' and
             audit['archiveManifestSha256'] == SOURCE_PIN and audit['resultSha256'] == RESULT_PIN, 'Accepted source audit required')
    validate_content()
    tower = API/'Data'/FLOOR
    tower_pin = io.sha(tower)
    io.check(tower_pin == '0b52aedce19700d542dbcb9a0b2441d272f1181457642e03ba8c7a14e20f01fd', 'Changed entry Tower')
    history, _ = io.history()
    io.check(len(history) == args.initial_exclusions, 'Changed initial exclusion union')
    cells = io.read(SOURCE/'cells.json')
    trials = [json.loads(line) for line in (SOURCE/'study/trials.jsonl').read_text().splitlines()]
    io.check(len(cells) == 21 and len(trials) == 5376, 'Incomplete source family')
    replays = select_replays(cells, trials)
    tests = artifacts/'bin/EssenceSystem.Tests/release'
    pin_paths = [*tests.glob('*.dll'), *API.joinpath('Data').rglob('*.json'), API/'appsettings.json',
                 SOURCE/'files.json', SOURCE/'result.json', audit_path, protocol, Path(__file__),
                 Path(io.__file__), ROOT/'build/run-tests.ps1', ROOT/'build/bounded_windows_process.py',
                 ROOT/'LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10QualificationTests.cs']
    pins = {str(p): io.sha(p) for p in pin_paths}
    io.check(str(tests/'EssenceSystem.Tests.dll') in pins, 'Build current tests first')
    owner.mkdir()
    shutil.copy2(Path(__file__), owner/'owner-source.py')
    shutil.copy2(protocol, owner/'protocol.md')
    q = dict(source=str(SOURCE), sourcePin=SOURCE_PIN, apiRoot=str(API), output=str(output),
             inputHashes=pins, replayIds=[x['id'] for x in replays])
    io.write(owner/'request.json', q)
    declaration = dict(sourceManifestSha256=SOURCE_PIN, initialTowerSha256=tower_pin, initialExclusions=len(history),
                       protocolSha256=io.sha(protocol), requestSha256=io.sha(owner/'request.json'), replays=replays,
                       maximumNativeSeconds=300, maximumOwnerSeconds=360, maximumBytes=128*1048576,
                       maximumFights=23, freshSeeds=0, checkedInputs=5376, preparedCells=21, retries=0)
    io.write(owner/'declaration.json', declaration)
    process = module('floor10_qualification_process', ROOT/'build/bounded_windows_process.py')
    env_name = 'LL_FLOOR10_QUALIFICATION'
    prior = os.environ.get(env_name)
    os.environ[env_name] = str(owner/'request.json')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT/'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter',
               'FullyQualifiedName~BalanceHarnessFloor10QualificationTests.Local_content_preserves_confirmed_inputs_and_representative_full_reports']
    io.write(owner/'command.json', command)
    try:
        receipt = process.run(command, ROOT, owner/'execution.log', time.monotonic()+360, log_byte_limit=1048576)
    finally:
        if prior is None:
            os.environ.pop(env_name, None)
        else:
            os.environ[env_name] = prior
    io.write(owner/'process.json', receipt)
    io.check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
             'Qualification failed; preserve without retry or fresh seed allocation')
    for path, pin in pins.items():
        io.check(io.sha(path) == pin, 'Changed qualification input: '+path)
    files = io.authenticate(output)
    io.check({p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()} == set(files)|{'files.json'}, 'Unexpected output inventory')
    result = io.read(output/'result.json')
    io.check(result['status'] == 'Verified' and result['prepared'] == 21 and result['checkedInputs'] == 5376 and
             result['repeatedFights'] == 23 and result['freshSeeds'] == result['retries'] == 0 and result['seconds'] <= 300,
             'Incomplete native qualification')
    io.check(io.read(output/'completion.json') == dict(status='Complete', attempts=23, completed=23, checkedInputs=5376, prepared=21), 'Incomplete native accounting')
    expected_cells = [dict(id=c['case']+'/'+c['profile'], composition=c['case'], gear=c['profile'],
                           origin='retained-reference', scenario=c['scenario']) for c in cells]
    io.check(io.read(output/'cells.json') == expected_cells, 'Legacy import changed an exact recipe')
    matches = [json.loads(line) for line in (output/'input-matches.jsonl').read_text().splitlines()]
    io.check(matches == [dict(Id=t['id'], inputHash=t['inputHash']) for t in trials], 'Incomplete input journal')
    io.check(io.read(output/'preflight.json') == dict(prepared=21, checkedInputs=5376, fights=0), 'Preflight fought')
    attempts = [json.loads(line) for line in (output/'attempts.jsonl').read_text().splitlines()]
    io.check(attempts == [dict(attempt=i+1, id=r['id']) for i, r in enumerate(replays)], 'Changed replay order')
    io.check(select_replays(cells, trials) == replays, 'Changed first-outcome selection')
    for replay in replays:
        saved = json.loads(gzip.decompress((SOURCE/'study/battles'/(replay['id']+'.json.gz')).read_bytes()))
        io.check(saved == io.read(output/'battles'/(replay['id']+'.json')), 'Complete replay differs')
    scope = io.read(output/'scope.json')
    io.check(scope['settings'] == io.read(SOURCE/'scope.json')['settings'], 'Settings differ')
    for name, pin in scope['contentHashes'].items():
        io.check(io.sha(output/'content/Data'/name) == io.sha(API/'Data'/name) == pin, 'Current content changed')
    for name, pin in scope['execution']['assemblyHashes'].items():
        io.check(pins[str(tests/(name+'.dll'))] == pin, 'Runtime differs')
    size = sum(p.stat().st_size for p in output.rglob('*') if p.is_file())
    io.check(size < 128*1048576 and io.sha(tower) == tower_pin, 'Content changed or byte limit exceeded')
    history_after, _ = io.history()
    io.check(history_after == history, 'Qualification allocated seeds')
    verified = dict(status='Verified', sourceManifestSha256=SOURCE_PIN, manifestSha256=io.sha(output/'files.json'),
                    resultSha256=io.sha(output/'result.json'), matchedInputs=5376, fullReplays=23, cells=21,
                    exactRecipesPreserved=True, currentRuntimeQualified=True, newSeeds=0, exclusions=len(history),
                    initialTowerSha256=tower_pin, outputBytes=size, checkedInputPins=len(pins))
    io.write(owner/'independent-audit.json', verified)
    io.write(owner/'completion.json', verified)
    print(json.dumps(verified, indent=2), flush=True)


if __name__ == '__main__':
    main()
