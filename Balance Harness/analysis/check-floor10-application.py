"""Apply the confirmed floor-10 scaling locally, then independently audit its bounded parity check.

The 23 replays reuse historical seeds and provide integration evidence only.
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
SOURCE = ROOT / 'TestResults/balance/tower-floor10-family-confirmation-20260928'
PIN = '95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944'
RESULT_PIN = 'b2fd5bbbf849d15699f8a66907f68e1c16b53e37837e4cc4e8a71842dfaa7be5'
BEFORE_PIN = '32ece402099702d29e34977c507adbb53c5d2a0a5994f82fc9a4507f7b4d6e8e'
FLOOR_FILE = 'world-tower/tower-floors.json'
API = ROOT / 'LL/src/API/API.LL'
TARGET = API / 'Data' / FLOOR_FILE
ENV = 'LL_FLOOR10_APPLICATION'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


io = module('floor10_application_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))


def changed_values(before, after, path=''):
    if isinstance(before, dict) and isinstance(after, dict) and before.keys() == after.keys():
        return [d for key in before for d in changed_values(before[key], after[key], path + '/' + key)]
    if isinstance(before, list) and isinstance(after, list) and len(before) == len(after):
        return [d for i, (a, b) in enumerate(zip(before, after)) for d in changed_values(a, b, path + '/' + str(i))]
    return [] if before == after else [(path, before, after)]


def require_delta(before, after):
    io.require(changed_values(before, after) == [
        ('/floors/9/guardianScaling/health', 1.64, 12.71),
        ('/floors/9/guardianScaling/offense', 0.92, 7.13)], 'Only the confirmed two floor-10 values may change')


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
    audit = pinned(ROOT / 'TestResults/tower-floor10-family-confirmation-owner-20260928/independent-audit.json')
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
    start = before_bytes.index(b'"floorNumber": 10,')
    end = before_bytes.index(b'"floorNumber": 11,', start)
    block = before_bytes[start:end]
    io.require(block.count(b'"health": 1.64,') == block.count(b'"offense": 0.92,') == 1, 'Ambiguous floor edit')
    block = block.replace(b'"health": 1.64,', b'"health": 12.71,').replace(b'"offense": 0.92,', b'"offense": 7.13,')
    after_bytes = before_bytes[:start] + block + before_bytes[end:]
    require_delta(json.loads(before_bytes), json.loads(after_bytes))
    io.require(json.loads(after_bytes) == confirmed, 'Local result must equal all confirmed floor content')
    cells = pinned(SOURCE / 'cells.json', files['cells.json'])
    seeds = pinned(SOURCE / 'confirmation-seeds.json', files['confirmation-seeds.json'])
    trial_path = SOURCE / 'study/trials.jsonl'
    io.require(io.sha(trial_path) == files['study/trials.jsonl'], 'Changed trial schedule')
    pins[str(trial_path)] = files['study/trials.jsonl']
    trials = [json.loads(line) for line in trial_path.read_text().splitlines()]
    io.require(len(cells) == 21 and len(seeds) == 256 and len(trials) == 5376, 'Complete family required')
    for recipe in {t['recipe'] for t in trials}:
        name = 'study/recipes/' + recipe + '.json'
        pinned(SOURCE / name, files[name])
    # Cover every recipe; replay both outcomes for the two recipes with observed wins.
    selections = []
    for cell in cells:
        stage = cell['case'] + '/' + cell['profile']
        outcomes = (True, False) if stage in (
            'floor10-retained-1/armor-and-health', 'floor10-retained-2/armor-and-health') else (None,)
        selections.extend((stage, outcome) for outcome in outcomes)
    chosen = []
    for stage, outcome in selections:
        for trial in (t for t in trials if t['stage'] == stage):
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
    io.require(len({t['id'] for t in chosen}) == 23, '23 distinct representative replays required')
    tests = artifacts / 'bin/EssenceSystem.Tests/release'
    runtime_pins = {str(tests / (name + '.dll')): io.sha(tests / (name + '.dll'))
                    for name in [*scope['execution']['assemblyHashes'], 'EssenceSystem.Tests']}
    source_paths = [Path(__file__), Path(__file__).with_name('prepare-confirmed-team-reuse.py'),
                    ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10ApplicationTests.cs',
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
             afterSha256=after_pin, maximumFights=23, freshSeeds=0, maximumSeconds=360, maximumNativeSeconds=300,
             maximumBytes=128 * 1048576, retries=0, checkedInputs=5376, preparedCells=21, replays=chosen,
             runtimePins=runtime_pins, rule='Exact local input and complete report parity. No new statistical evidence. Stop on failure; retain evidence.'))
    io.require(io.sha(TARGET) == BEFORE_PIN, 'Local floor changed during preparation')
    TARGET.write_bytes(after_bytes)
    print('Applied exactly two floor-10 values; frozen 5,376 input checks and 23 historical replays.', flush=True)
    owner = module('floor10_application_owner', ROOT / 'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT / 'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessFloor10ApplicationTests']
    previous = os.environ.get(ENV)
    os.environ[ENV] = str(package / 'request.json')
    try:
        receipt = owner.run(command, ROOT, package / 'application.log', time.monotonic() + 360, log_byte_limit=1048576)
        io.write(package / 'process.json', receipt)
        io.require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
                   'Application parity failed; retain evidence and inspect the local edit; no automatic retry')
        for name, digest in pins.items():
            io.require(io.sha(Path(name)) == digest, 'Changed frozen input: ' + name)
        io.write(package / 'completion.json', dict(status='Complete', archiveManifestSha256=io.sha(output / 'files.json'),
                 resultSha256=io.sha(output / 'result.json'), repeatedFights=23, freshSeeds=0))
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
    io.require(all(protocol.get(k) == v for k, v in dict(maximumFights=23, freshSeeds=0, maximumSeconds=360,
               maximumNativeSeconds=300, maximumBytes=128 * 1048576, retries=0, checkedInputs=5376,
               preparedCells=21).items()), 'Changed application envelope')
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
    io.require(scope['algorithm'] == 'tower-floor10-checked-application-v1', 'Changed application version')
    io.require(scope['settings'] == prior['settings'], 'Changed effective settings')
    io.require(scope['contentHashes'].keys() == prior['contentHashes'].keys(), 'Changed content inventory')
    for name, digest in scope['contentHashes'].items():
        io.require(io.sha(API / 'Data' / name) == io.sha(output / 'content/Data' / name) == digest, 'Local/saved content differs')
        io.require(digest == (protocol['afterSha256'] if name == FLOOR_FILE else prior['contentHashes'][name]), 'Unconfirmed content')
    for name, digest in scope['execution']['assemblyHashes'].items():
        io.require(any(Path(p).name == name + '.dll' and h == digest for p, h in protocol['runtimePins'].items()), 'Runtime differs')
    trials = [json.loads(line) for line in (SOURCE / 'study/trials.jsonl').read_text().splitlines()]
    matches = [json.loads(line) for line in (output / 'input-matches.jsonl').read_text().splitlines()]
    io.require(len(trials) == len(matches) == 5376, 'Incomplete input parity')
    io.require(matches == [dict(Id=t['id'], inputHash=t['inputHash']) for t in trials], 'Input journal mismatch')
    io.require(io.read(output / 'preflight.json') == dict(prepared=21, checkedInputs=5376, fights=0), 'Incomplete preparation')
    io.require(q['replayIds'] == [t['id'] for t in protocol['replays']], 'Changed replay selection')
    cells = io.read(SOURCE / 'cells.json')
    stages = [c['case'] + '/' + c['profile'] for c in cells]
    io.require(len(set(q['replayIds'])) == 23 and len(stages) == len(set(stages)) == 21
               and {r['stage'] for r in protocol['replays']} == set(stages), 'Incomplete replay coverage')
    for stage in stages:
        outcomes = [r['succeeded'] for r in protocol['replays'] if r['stage'] == stage]
        expected = [True, False] if stage in (
            'floor10-retained-1/armor-and-health', 'floor10-retained-2/armor-and-health') else [False]
        io.require(outcomes == expected, 'Missing representative outcome: ' + stage)
    attempts = [json.loads(line) for line in (output / 'attempts.jsonl').read_text().splitlines()]
    io.require(attempts == [dict(attempt=i + 1, id=t) for i, t in enumerate(q['replayIds'])], 'Replay accounting differs')
    for item in protocol['replays']:
        trial = next(t for t in trials if t['id'] == item['id'])
        io.require((trial['seed'], trial['stage']) == (item['seed'], item['stage']), 'Changed representative trial')
        saved = json.loads(gzip.decompress((SOURCE / 'study/battles' / (item['id'] + '.json.gz')).read_bytes()))
        actual = io.read(output / 'battles' / (item['id'] + '.json'))
        io.require(saved == actual and saved['succeeded'] == item['succeeded'], 'Complete battle report differs')
    result = io.read(output / 'result.json')
    io.require(result['status'] == 'Verified' and result['prepared'] == 21 and result['checkedInputs'] == 5376
               and result['repeatedFights'] == 23 and result['freshSeeds'] == result['retries'] == 0
               and result['seconds'] <= 300, 'Incomplete or over-budget native result')
    io.require(io.read(output / 'completion.json') == dict(status='Complete', attempts=23, completed=23, checkedInputs=5376, prepared=21),
               'Incomplete native process')
    process = io.read(package / 'process.json')
    io.require(process['exitCode'] == 0 and process['activeProcesses'] == 0 and not process['timedOut'] and process['seconds'] <= 360,
               'Owner failed to complete and drain')
    completion = io.read(package / 'completion.json')
    io.require(completion['status'] == 'Complete' and completion['archiveManifestSha256'] == args.manifest_pin
               and completion['resultSha256'] == io.sha(output / 'result.json')
               and completion['repeatedFights'] == 23 and completion['freshSeeds'] == 0, 'Invalid owner completion')
    size = sum(p.stat().st_size for p in output.rglob('*') if p.is_file())
    io.require(size <= 128 * 1048576, 'Output exceeded storage cap')
    receipt = dict(status='Verified', checkedInputs=5376, preparedCells=21, repeatedReports=23, freshSeeds=0,
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
