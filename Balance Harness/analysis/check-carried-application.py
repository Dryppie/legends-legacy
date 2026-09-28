"""Apply the confirmed carried-equipment floor-11 scaling locally, then independently audit its bounded parity check.

The 470 replays reuse historical seeds and provide integration evidence only.
No seed allocation, balance inference, service restart or deployment occurs.
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
SOURCE = ROOT / 'TestResults/balance/tower-carried-confirmation-20260928'
PIN = '6a0d98f41362a3428816679efc9d0e8a90a88a37789030fbc38afa0e4652427d'
RESULT_PIN = 'd5334add4af7c7d334842e9ecef1779aa7447f02dbcfc312898556f7c459dbc9'
BEFORE_PIN = 'dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124'
FLOOR_FILE = 'world-tower/tower-floors.json'
API = ROOT / 'LL/src/API/API.LL'
TARGET = API / 'Data' / FLOOR_FILE
ENV = 'LL_CARRIED_APPLICATION'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('carried_application_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def changed_values(before, after, path=''):
    if isinstance(before, dict) and isinstance(after, dict) and before.keys() == after.keys():
        return [d for key in before for d in changed_values(before[key], after[key], path + '/' + key)]
    if isinstance(before, list) and isinstance(after, list) and len(before) == len(after):
        return [d for i, (a, b) in enumerate(zip(before, after)) for d in changed_values(a, b, path + '/' + str(i))]
    return [] if before == after else [(path, before, after)]


def require_delta(before, after):
    io.require(changed_values(before, after) == [
        ('/floors/10/guardianScaling/health', 6.525, 17.94375),
        ('/floors/10/guardianScaling/offense', 8.37, 23.0175)], 'Only the confirmed two carried-equipment floor-11 values may change')


def apply(args):
    package, output, artifacts = [io.unlinked(p.absolute()) for p in (args.package, args.output, args.artifacts)]
    io.require(not package.exists() and not output.exists(), 'New owner and output required; no retry or resume')
    io.require(package.parent == output.parent == artifacts.parent == ROOT / 'TestResults', 'Use direct TestResults children')
    io.require(io.sha(TARGET) == BEFORE_PIN, 'Local floor content changed; reassess before applying')
    pins = {}

    def pinned(path, expected=None):
        digest = io.sha(path)
        io.require(expected is None or digest == expected, 'Changed input: ' + str(path))
        pins[str(path)] = digest
        return io.read(path)

    files = pinned(SOURCE / 'files.json', PIN)
    result = pinned(SOURCE / 'result.json', RESULT_PIN)
    audit = pinned(ROOT / 'TestResults/tower-carried-confirmation-owner-20260928/independent-audit.json')
    io.require(audit['status'] == 'Verified' and audit['archiveManifestSha256'] == PIN
               and audit['resultSha256'] == RESULT_PIN and result['assessment'] == audit['assessment'] == 'Pass',
               'Audited passing confirmation required')
    scope = pinned(SOURCE / 'scope.json', files['scope.json'])
    for name, digest in scope['contentHashes'].items():
        pinned(SOURCE / 'content/Data' / name, digest)
        if name != FLOOR_FILE:
            io.require(io.sha(API / 'Data' / name) == digest, 'Unconfirmed local content: ' + name)
            pins[str(API / 'Data' / name)] = digest
    confirmed = io.read(SOURCE / 'content/Data' / FLOOR_FILE)
    before_bytes = TARGET.read_bytes()
    start = before_bytes.index(b'"floorNumber": 11,')
    end = before_bytes.index(b'"floorNumber": 12,', start)
    block = before_bytes[start:end]
    io.require(block.count(b'"health": 6.525,') == block.count(b'"offense": 8.37,') == 1, 'Ambiguous floor edit')
    block = block.replace(b'"health": 6.525,', b'"health": 17.94375,').replace(b'"offense": 8.37,', b'"offense": 23.0175,')
    after_bytes = before_bytes[:start] + block + before_bytes[end:]
    require_delta(json.loads(before_bytes), json.loads(after_bytes))
    io.require(json.loads(after_bytes) == confirmed, 'Local result must equal all confirmed floor content')
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    seeds = pinned(SOURCE / 'confirmation-seeds.json', files['confirmation-seeds.json'])
    trial_path = SOURCE / 'study/trials.jsonl'
    io.require(io.sha(trial_path) == files['study/trials.jsonl'], 'Changed trial schedule')
    pins[str(trial_path)] = files['study/trials.jsonl']
    trials = [json.loads(line) for line in trial_path.read_text().splitlines()]
    io.require(len(cells) == 344 and len(seeds) == 256 and len(trials) == 88064, 'Complete family required')
    for recipe in {t['recipe'] for t in trials}:
        name = 'study/recipes/' + recipe + '.json'
        pinned(SOURCE / name, files[name])
    io.require(result['status'] == 'CarriedConfirmationComplete' and result['confirmationFights'] == 88064
               and result['selectedFactor'] == 2.75 and result['exclusionUnion'] == 835319,
               'Complete carried-equipment confirmation required')
    rows = {r['id']: r for r in result['rows']}
    io.require(len(rows) == len({c['id'] for c in cells}) == 344 and set(rows) == {c['id'] for c in cells}, 'Complete unique recipe family required')
    io.require(all(r['samples'] == 256 and 0 <= r['wins'] < 256 and r['draws'] == 0 for r in rows.values())
               and sum(r['wins'] > 0 for r in rows.values()) == 126, 'Changed observed outcome coverage')
    # For each recipe, use its first archived win (if any), then its first archived loss.
    selections = [(c['id'], outcome) for c in cells for outcome in ((True, False) if rows[c['id']]['wins'] > 0 else (False,))]
    trial_groups = {c['id']: [] for c in cells}
    for trial in trials:
        trial_groups[trial['stage']].append(trial)
    chosen = []
    for stage, outcome in selections:
        for trial in trial_groups[stage]:
            name = 'study/battles/' + trial['id'] + '.json.gz'
            path = SOURCE / name
            io.require(io.sha(path) == files[name], 'Changed saved report')
            pins[str(path)] = files[name]
            saved = json.loads(gzip.decompress(path.read_bytes()))
            if outcome is None or saved['succeeded'] == outcome:
                chosen.append(dict(id=trial['id'], stage=stage, seed=trial['seed'], succeeded=saved['succeeded']))
                break
        else:
            raise ValueError('Missing predeclared replay: ' + stage)
    io.require(len({t['id'] for t in chosen}) == 470, '470 distinct representative replays required')
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    runtime_pins = {str(tests / (name + '.dll')): io.sha(tests / (name + '.dll'))
                    for name in [*scope['execution']['assemblyHashes'], 'EssenceSystem.Tests']}
    source_paths = [Path(__file__), Path(__file__).with_name('prepare-confirmed-team-reuse.py'),
                    ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessCarriedApplicationTests.cs',
                    ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs',
                    ROOT / 'build/run-tests.ps1', ROOT / 'build/bounded_windows_process.py']
    pins.update({str(p): io.sha(p) for p in source_paths})
    pins.update(runtime_pins)
    package.mkdir()
    (package / 'floor-before.json').write_bytes(before_bytes)
    (package / 'floor-after.json').write_bytes(after_bytes)
    after_pin = io.sha(package / 'floor-after.json')
    pins[str(TARGET)] = after_pin
    request = dict(source=str(SOURCE), sourcePin=PIN, apiRoot=str(API), output=str(output), inputHashes=pins,
                   replayIds=[t['id'] for t in chosen])
    io.write(package / 'request.json', request)
    io.write(package / 'protocol.json', dict(requestSha256=io.sha(package / 'request.json'), beforeSha256=BEFORE_PIN,
             afterSha256=after_pin, maximumFights=470, freshSeeds=0, maximumSeconds=960, maximumNativeSeconds=900,
             maximumBytes=512 * 1048576, retries=0, checkedInputs=88064, preparedCells=344, replays=chosen,
             runtimePins=runtime_pins, rule='Exact local input and complete report parity. No new statistical evidence. Stop on failure; retain evidence.'))
    io.require(io.sha(TARGET) == BEFORE_PIN, 'Local floor changed during preparation')
    TARGET.write_bytes(after_bytes)
    print('Applied exactly two carried-equipment floor-11 values; frozen 88,064 input checks and 470 historical replays.', flush=True)
    owner = module('carried_application_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessCarriedApplicationTests']
    previous = os.environ.get(ENV)
    os.environ[ENV] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'application.log', time.monotonic() + 960, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Application parity failed; retain evidence and inspect the local edit; no automatic retry')
        for name, digest in pins.items():
            io.require(io.sha(Path(name)) == digest, 'Changed frozen input: ' + name)
        io.write(package / 'completion.json', dict(status='Complete', archiveManifestSha256=io.sha(output / 'files.json'),
                 resultSha256=io.sha(output / 'result.json'), repeatedFights=470, freshSeeds=0))
        print('Current-build parity passed. Run verify with the emitted manifest pin: ' + io.sha(output / 'files.json'), flush=True)
    finally:
        if previous is None:
            os.environ.pop(ENV, None)
        else:
            os.environ[ENV] = previous


def verify(args):
    package = io.unlinked(args.package.absolute())
    q = io.read(package / 'request.json')
    protocol = io.read(package / 'protocol.json')
    output = io.unlinked(Path(q['output']))
    receipt_path = io.unlinked(args.receipt.absolute())
    io.require(not receipt_path.exists() and not receipt_path.is_relative_to(output), 'New receipt outside sealed output required')
    io.require(all(protocol.get(k) == v for k, v in dict(maximumFights=470, freshSeeds=0, maximumSeconds=960,
               maximumNativeSeconds=900, maximumBytes=512 * 1048576, retries=0, checkedInputs=88064,
               preparedCells=344).items()), 'Changed application envelope')
    io.require(io.sha(package / 'request.json') == protocol['requestSha256'], 'Changed request')
    io.require(q['sourcePin'] == PIN and Path(q['source']) == SOURCE and io.sha(SOURCE / 'files.json') == PIN, 'Changed source')
    for name, digest in q['inputHashes'].items():
        io.require(io.sha(Path(name)) == digest, 'Changed input: ' + name)
    io.require(io.sha(package / 'floor-before.json') == protocol['beforeSha256'] == BEFORE_PIN, 'Changed baseline')
    io.require(io.sha(package / 'floor-after.json') == protocol['afterSha256'] == io.sha(TARGET), 'Changed local application')
    require_delta(io.read(package / 'floor-before.json'), io.read(TARGET))
    io.require(io.read(TARGET) == io.read(SOURCE / 'content/Data' / FLOOR_FILE), 'Confirmed floors differ')
    io.require(io.sha(output / 'files.json') == args.manifest_pin, 'Changed output manifest')
    manifest = io.read(output / 'files.json')
    actual = {p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()}
    io.require(actual == set(manifest) | {'files.json'}, 'Unexpected output inventory')
    for name, digest in manifest.items():
        io.require(io.sha(io.member(output, name)) == digest, 'Changed output: ' + name)
    io.require(io.read(output / 'request.json') == q, 'Native request differs')
    scope = io.read(output / 'scope.json')
    prior = io.read(SOURCE / 'scope.json')
    io.require(scope['algorithm'] == 'tower-floor11-carried-application-v1', 'Changed application version')
    io.require(scope['settings'] == prior['settings'], 'Changed effective settings')
    io.require(scope['contentHashes'].keys() == prior['contentHashes'].keys(), 'Changed content inventory')
    for name, digest in scope['contentHashes'].items():
        io.require(io.sha(API / 'Data' / name) == io.sha(output / 'content/Data' / name) == digest, 'Local/saved content differs')
        io.require(digest == (protocol['afterSha256'] if name == FLOOR_FILE else prior['contentHashes'][name]), 'Unconfirmed content')
    for name, digest in scope['execution']['assemblyHashes'].items():
        io.require(any(Path(p).name == name + '.dll' and h == digest for p, h in protocol['runtimePins'].items()), 'Runtime differs')
    trials = [json.loads(line) for line in (SOURCE / 'study/trials.jsonl').read_text().splitlines()]
    matches = [json.loads(line) for line in (output / 'input-matches.jsonl').read_text().splitlines()]
    io.require(len(trials) == len(matches) == 88064, 'Incomplete input parity')
    io.require(matches == [dict(Id=t['id'], inputHash=t['inputHash']) for t in trials], 'Input journal mismatch')
    io.require(io.read(output / 'preflight.json') == dict(prepared=344, checkedInputs=88064, fights=0), 'Incomplete preparation')
    io.require(q['replayIds'] == [t['id'] for t in protocol['replays']], 'Changed replay selection')
    cells = io.read(SOURCE / 'cells.json')
    stages = [c['id'] for c in cells]
    source_files = io.read(SOURCE / 'files.json')
    source_result = io.read(SOURCE / 'result.json')
    rows = {r['id']: r for r in source_result['rows']}
    io.require(len(set(q['replayIds'])) == 470 and len(stages) == len(set(stages)) == 344
               and {r['stage'] for r in protocol['replays']} == set(stages) == set(rows), 'Incomplete replay coverage')
    expected_replays = []
    for stage in stages:
        expected_outcomes = [True, False] if rows[stage]['wins'] > 0 else [False]
        selected = [r for r in protocol['replays'] if r['stage'] == stage]
        io.require([r['succeeded'] for r in selected] == expected_outcomes, 'Missing representative outcome: ' + stage)
        seen = {}
        for trial in (t for t in trials if t['stage'] == stage):
            name = 'study/battles/' + trial['id'] + '.json.gz'
            io.require(io.sha(SOURCE / name) == source_files[name], 'Changed selection report')
            saved = json.loads(gzip.decompress((SOURCE / name).read_bytes()))
            seen.setdefault(saved['succeeded'], dict(id=trial['id'], stage=stage, seed=trial['seed'], succeeded=saved['succeeded']))
            if all(outcome in seen for outcome in expected_outcomes):
                break
        io.require(all(outcome in seen for outcome in expected_outcomes), 'Missing archived outcome')
        expected_replays.extend(seen[outcome] for outcome in expected_outcomes)
    io.require(protocol['replays'] == expected_replays, 'Replays are not the first archived occurrence of each outcome')
    io.require(io.read(output / 'replay-coverage.json') == dict(preparedCells=344, mixedOutcomeCells=126, replays=470, wins=126, losses=344),
               'Native replay coverage differs')
    attempts = [json.loads(line) for line in (output / 'attempts.jsonl').read_text().splitlines()]
    io.require(attempts == [dict(attempt=i + 1, id=t) for i, t in enumerate(q['replayIds'])], 'Replay accounting differs')
    for item in protocol['replays']:
        trial = next(t for t in trials if t['id'] == item['id'])
        io.require((trial['seed'], trial['stage']) == (item['seed'], item['stage']), 'Changed representative trial')
        saved = json.loads(gzip.decompress((SOURCE / 'study/battles' / (item['id'] + '.json.gz')).read_bytes()))
        actual = io.read(output / 'battles' / (item['id'] + '.json'))
        io.require(saved == actual and saved['succeeded'] == item['succeeded'], 'Complete battle report differs')
    result = io.read(output / 'result.json')
    io.require(result['status'] == 'Verified' and result['prepared'] == 344 and result['checkedInputs'] == 88064
               and result['repeatedFights'] == 470 and result['freshSeeds'] == result['retries'] == 0
               and result['seconds'] <= 900, 'Incomplete or over-budget native result')
    io.require(io.read(output / 'completion.json') == dict(status='Complete', attempts=470, completed=470, checkedInputs=88064, prepared=344),
               'Incomplete native process')
    process = io.read(package / 'process.json')
    io.require(process['exitCode'] == 0 and process['activeProcesses'] == 0 and not process['timedOut'] and process['seconds'] <= 960,
               'Owner failed to complete and drain')
    completion = io.read(package / 'completion.json')
    io.require(completion['status'] == 'Complete' and completion['archiveManifestSha256'] == args.manifest_pin
               and completion['resultSha256'] == io.sha(output / 'result.json')
               and completion['repeatedFights'] == 470 and completion['freshSeeds'] == 0, 'Invalid owner completion')
    size = sum(p.stat().st_size for p in output.rglob('*') if p.is_file())
    io.require(size <= 512 * 1048576, 'Output exceeded storage cap')
    receipt = dict(status='Verified', checkedInputs=88064, preparedCells=344, repeatedReports=470, freshSeeds=0,
                   changedFields=2, unchangedOtherContentFiles=28, outputBytes=size, auditFights=0,
                   archiveManifestSha256=args.manifest_pin, resultSha256=io.sha(output / 'result.json'))
    io.write(receipt_path, receipt)
    print(json.dumps(receipt, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='mode', required=True)
    launch = sub.add_parser('apply')
    for name in ('package', 'output', 'artifacts'):
        launch.add_argument('--' + name, type=Path, required=True)
    audit = sub.add_parser('verify')
    audit.add_argument('--package', type=Path, required=True)
    audit.add_argument('--manifest-pin', required=True)
    audit.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    (apply if args.mode == 'apply' else verify)(args)


if __name__ == '__main__':
    main()
