"""Own a bounded earned-party qualification or a separately admitted floor-one diagnostic."""
import argparse
import hashlib
import gzip
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-earned-party-v1'
ARCHIVE_PIN = 'f8f05eb4cf6d1e9c28a87b72bfeb0f1a9ff9f1229c5ad687c0413b042d7fbdc5'
LEDGER_PIN = '3d16dfa5acedb1f3c17a01fd75aa0b480eb907c72e7231267b4c60d7da868aff'


def read(path):
    data = gzip.decompress(path.read_bytes()) if path.suffix == '.gz' else path.read_bytes()
    return json.loads(data.decode('utf-8-sig'))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def check(value, message):
    if not value: raise AssertionError(message)
def write(path, value):
    with path.open('x', encoding='utf-8') as f: json.dump(value, f, indent=2); f.write('\n')
def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value
def manifest(directory, pin):
    check(sha(directory/'files.json') == pin, 'Wrong manifest pin')
    entries = read(directory/'files.json')
    check(set(entries) == {p.name for p in directory.iterdir() if p.name != 'files.json'}, 'Unmanifested output')
    for name, digest in entries.items():
        check(Path(name).name == name and sha(directory/name) == digest, 'Changed/unsafe manifest member: '+name)
    return entries
def exclusions():
    latest = ROOT/'TestResults/tower-growing-activity-owner-20260929/seed-ledger.json'
    check(sha(latest) == LEDGER_PIN, 'Changed growing activity reservation pin')
    ledger = read(latest)
    def pinned(entry):
        check(sha(Path(entry['archive'])) == entry['sha256'], 'Changed reservation ancestor')
        return read(Path(entry['archive']))
    h, d = pinned(ledger['historical']), pinned(ledger['dungeon'])
    values = set(h['historical'] + h['first'] + h['second'] + d['reserved'])
    for entry in ledger['preceding']: values.update(pinned(entry)['reserved'])
    values.update(ledger['reserved'])
    check(len(values) == 876935, 'Missing consumed or unused historical reservations')
    return values, ledger, latest


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--mode', choices=['prepare', 'combat'], required=True)
    for name in ['owner', 'output', 'artifacts']: p.add_argument('--'+name, type=Path, required=True)
    p.add_argument('--qualification-owner', type=Path)
    p.add_argument('--qualification-pin')
    a = p.parse_args()
    owner, output, artifacts = [x.absolute() for x in [a.owner, a.output, a.artifacts]]
    check(owner.parent == output.parent == artifacts.parent == ROOT/'TestResults', 'Direct TestResults children required')
    check(owner.name.startswith('tower-earned-') and not owner.exists() and not output.exists(), 'Fresh owner/output required, no retry or resume')
    # This fixed diagnostic has a closed predecessor union. A new output name must not silently
    # replay its deterministic allocation after an earlier owner has reserved these seeds.
    if a.mode == 'combat':
        check(not list((ROOT/'TestResults').glob('tower-earned-*/seed-ledger.json')),
              'Earlier earned-party combat reservations exist; declare a follow-up including those reservations, not a renamed rerun')
    archive = ROOT/'TestResults/tower-growing-activity-study-20260929'
    previous = ROOT/'TestResults/tower-growing-activity-owner-20260929'
    manifest(archive, ARCHIVE_PIN)
    prior_audit = read(previous/'independent-audit.json')
    check(prior_audit['status'] == 'VerifiedGrowingActivityNotPlayerPace' and prior_audit['manifestPin'] == ARCHIVE_PIN, 'Audited growing histories required')
    compatibility = {}
    for path, entry in read(previous/'source-map.json').items():
        relative = Path(path).relative_to(ROOT).as_posix()
        if relative.startswith(('LL/src/API/API.LL/Data/', 'LL/src/Core/Common/', 'LL/src/Core/Domain/Models/Combat/',
            'LL/src/Core/Domain/Models/WorldTower/', 'LL/src/Core/Domain/Models/Items/', 'LL/src/Core/Domain/Models/Essences/',
            'LL/src/Infrastructure/Service/Services.LL/Combat/', 'LL/src/Infrastructure/Service/Services.LL/WorldTower/',
            'LL/src/Infrastructure/Service/Services.LL/PowerRatings/')):
            check(sha(Path(path)) == entry['sha256'], 'Review production drift before transferring earned receipts: '+relative)
            compatibility[relative] = entry['sha256']
    qualification = None
    if a.mode == 'combat':
        check(a.qualification_owner is not None and a.qualification_pin, 'Explicit audited preparation required')
        q_owner = a.qualification_owner.absolute()
        qualification = Path(read(q_owner/'request.json')['output'])
        manifest(qualification, a.qualification_pin)
        audit = read(q_owner/'independent-audit.json')
        check(audit['status'] == 'VerifiedEarnedPartyPreparation' and audit['manifestPin'] == a.qualification_pin
              and audit['partyPreparations'] == 224 and audit['personalCheckpoints'] == 256, 'Independent preparation qualification must pass first')
    owner.mkdir(); inputs = {}; sources = {}
    def freeze(path):
        if str(path) in sources: return Path(sources[str(path)]['archive'])
        dest = owner/'inputs'/path.relative_to(ROOT)
        dest.parent.mkdir(parents=True, exist_ok=True)
        digest = sha(path)
        with path.open('rb') as source, dest.open('xb') as target: shutil.copyfileobj(source, target)
        check(sha(path) == sha(dest) == digest, 'Input changed during capture')
        sources[str(path)] = dict(archive=str(dest), sha256=digest); inputs[str(dest)] = digest
        return dest
    for folder in ['LL/src/API/API.LL/Data', 'LL/tools/BalanceHarness/Fixtures']:
        for path in sorted((ROOT/folder).rglob('*.json')): freeze(path)
    freeze(ROOT/'LL/src/API/API.LL/appsettings.json')
    for folder in ['LL/tools/BalanceHarness', 'LL/src/Core/Common', 'LL/src/Core/Domain', 'LL/src/Core/Application', 'LL/src/Infrastructure/Service/Services.LL']:
        for path in sorted((ROOT/folder).rglob('*.cs')):
            if not {'bin', 'obj'}.intersection(path.relative_to(ROOT).parts): freeze(path)
    for path in [Path(__file__), Path(__file__).with_name('verify-tower-earned-party.py'), ROOT/'build/bounded_windows_process.py',
                 ROOT/'build/run-tests.ps1', ROOT/'LL/tests/EssenceSystem.Tests/BalanceHarnessEarnedPartyTests.cs',
                 archive/'files.json', archive/'rules.json', previous/'independent-audit.json', previous/'seed-ledger.json',
                 *sorted(archive.glob('*--history.json'))]: freeze(path)
    if qualification:
        for path in [*sorted(qualification.iterdir()), q_owner/'independent-audit.json', q_owner/'request.json']: freeze(path)
    for name in ['BalanceHarness', 'Services.LL', 'Application', 'Domain', 'Common', 'EssenceSystem.Tests']:
        runtime = artifacts/'bin/EssenceSystem.Tests/release'/f'{name}.dll'
        freeze(runtime); inputs[str(runtime)] = sha(runtime)
    write(owner/'source-compatibility.json', compatibility)
    request = dict(apiRoot=str(owner/'inputs/LL/src/API/API.LL'), output=str(output), inputHashes=inputs)
    if a.mode == 'prepare':
        request.update(fixtures=str(owner/'inputs/LL/tools/BalanceHarness/Fixtures'), archive=str(owner/'inputs'/archive.relative_to(ROOT)))
        maximum_seconds, native_seconds, fights, seeds = 300, 240, 0, 0
        env, test = 'LL_TOWER_EARNED_PARTY', 'Frozen_earned_party_qualification'
    else:
        excluded, ledger, latest = exclusions(); allocated = []; cursor = 0
        def fresh():
            nonlocal cursor
            while True:
                seed = int.from_bytes(hashlib.sha256(f'{VERSION}|{cursor}'.encode()).digest()[:4], 'little', signed=True)
                cursor += 1
                if seed not in excluded: excluded.add(seed); allocated.append(seed); return seed
        panels = {f'{outcome}--{rotation}--{horizon}': [fresh() for _ in range(16)]
            for outcome in ['perfect','four-of-five'] for rotation in range(4) for horizon in [2160,8640,25920,86400]}
        write(owner/'seed-ledger.json', dict(version=VERSION,state='ReservedIncludingUnconsumed',historical=ledger['historical'],
            dungeon=ledger['dungeon'],preceding=ledger['preceding']+[dict(archive=str(latest),sha256=LEDGER_PIN)],
            reserved=allocated,exclusionUnionCount=len(excluded)))
        inputs[str(owner/'seed-ledger.json')] = sha(owner/'seed-ledger.json')
        request.update(qualification=str(owner/'inputs'/qualification.relative_to(ROOT)), qualificationPin=a.qualification_pin, panels=panels)
        maximum_seconds, native_seconds, fights, seeds = 600, 540, 1088, 512
        env, test = 'LL_TOWER_EARNED_COMBAT', 'Frozen_earned_floor_one_diagnostic'
    write(owner/'source-map.json', sources)
    write(owner/'request.json', request)
    write(owner/'declaration.json', dict(version=VERSION, mode=a.mode, requestPin=sha(owner/'request.json'),
        maximumSeconds=maximum_seconds, maximumNativeSeconds=native_seconds, maximumBytes=256*1048576,
        maximumFights=fights, newSeeds=seeds, retries=0, measuredPlayerSamples=0,
        rule='Frozen acquisition checkpoint parties, overlapping alternatives with distinct personal owners per party. No inventory grants, tuning, search or inferred later-floor progress. Preparation must pass independent audit before fixed floor-one combat. No extension.'))
    process = module('earned_process', ROOT/'build/bounded_windows_process.py')
    old = os.environ.get(env); os.environ[env] = str(owner/'request.json')
    try:
        command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT/'build/run-tests.ps1'), '-NoBuild',
            '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessEarnedPartyTests.'+test]
        receipt = process.run(command, ROOT, owner/'study.log', time.monotonic()+maximum_seconds, log_byte_limit=1048576)
        write(owner/'process.json', receipt)
        check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Incomplete study; preserve evidence and reservations, no retry')
        for path, digest in inputs.items(): check(sha(Path(path)) == digest, 'Frozen input drift')
        check(sum(p.stat().st_size for p in output.iterdir()) <= 256*1048576, 'Output cap exceeded')
        manifest(output, sha(output/'files.json'))
        check(read(output/'result.json')['status'] == ('EarnedPartiesPreparedNotFloorProgression' if a.mode == 'prepare' else 'EarnedFloorOneDiagnosticComplete'), 'Incomplete result')
        shutil.copyfile(ROOT/'TestResults/tests/tests.trx', owner/'study-tests.trx')
        done = dict(status='Complete', mode=a.mode, manifestPin=sha(output/'files.json'), resultPin=sha(output/'result.json'), newSeeds=seeds)
        write(owner/'completion.json', done); print(json.dumps(done, indent=2))
    finally:
        if old is None: os.environ.pop(env, None)
        else: os.environ[env] = old


if __name__ == '__main__': main()
