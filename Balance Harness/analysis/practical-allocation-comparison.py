"""Prepare or execute the one-shot paired allocation comparison. No retries or sample extension."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / 'TestResults/allocation-comparison-20260917'
RUN = ROOT / 'TestResults/balance/tower-allocation-comparison-20260917'
CAPTURE = ROOT / 'TestResults/current-tower-admission-readiness-20260917'
FIXED = ROOT / 'TestResults/balance/tower-practical-fixed-team-confirmation-20260917'
BUILD = ROOT / 'TestResults/allocation-comparison-build-20260917/bin/BalanceHarness/release'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def canonical(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True).encode()).hexdigest()


def save(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)
        stream.flush()
        os.fsync(stream.fileno())


def inventory(path):
    return {str(p.relative_to(path)).replace('\\', '/'): sha(p) for p in sorted(path.rglob('*')) if p.is_file()}


def prepare():
    started = time.monotonic()
    PACKAGE.mkdir()
    shutil.copytree(CAPTURE / 'runtime', PACKAGE / 'runtime')
    shutil.copytree(CAPTURE / 'content', PACKAGE / 'content')
    for name in ('BalanceHarness.dll', 'BalanceHarness.pdb', 'BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json'):
        shutil.copyfile(BUILD / name, PACKAGE / 'runtime' / name)
    shutil.copyfile(CAPTURE / 'bounded_output_process.py', PACKAGE / 'bounded_output_process.py')
    shutil.copyfile(__file__, PACKAGE / 'comparison-source.py')
    subprocess.run(['pwsh', '-NoProfile', '-File', str(ROOT / 'Balance Harness/analysis/practical-native-verification/native-context.ps1'),
                    '-Runtime', str(PACKAGE / 'runtime'), '-ContentRoot', str(PACKAGE / 'content'),
                    '-Output', str(PACKAGE / 'context.json')], check=True, timeout=30)
    context = read(PACKAGE / 'context.json')
    d = read(ROOT / 'TestResults/balance/tower-practical-native-verification-20260917/definition.json')
    original = read(FIXED / 'request.json')
    ledger = read(FIXED / 'seed-ledger.json')
    d['excludedCombatSeeds'] = sorted(set(ledger['historical'] + ledger['reserved']))
    assert len(d['excludedCombatSeeds']) == 497371
    d['executionHash'] = context['executionHash']
    assert d['settingsHash'] == context['settingsHash']
    for name, digest in d['contentHashes'].items():
        assert sha(PACKAGE / 'content/Data' / name) == digest
    d['id'] = 'allocation-comparison-template'
    d['generation'].update(seeds=[], candidatesPerArm=16, maximumAttemptsPerArm=256, policyVersion='retained-composition-racing-v1')
    d['stages']['schedules'] = {'fixed-equipment': dict(discovery=[], selection=[], confirmation=[], diagnostics=[])}
    d['maximumBattles'] = 3496
    d['references'], d['starts'] = [], []
    for team in read(FIXED / 'teams.json')['teams'][:2]:
        reference = 'confirmed-' + team['partyId'][:24]
        scenario = team['scenario']
        assert not scenario['seeds']
        builds = {str(p['partySlot']): p['build']['essenceIds'] for p in scenario['party']}
        assert canonical(builds) == team['partyId']
        d['references'].append(dict(id=reference, context='fixed-equipment', scenario=scenario,
                                   source='Fixed-team confirmation; exact candidate or Anchor040e; historical outcomes excluded from search.',
                                   evidenceHash=sha(FIXED / 'result.json')))
        d['starts'].append(dict(id='start-' + canonical(reference)[:24], referenceId=reference,
                                party=dict(id=team['partyId'], source='supplied', builds=builds)))
    d['starts'].sort(key=lambda s: s['referenceId'])
    save(PACKAGE / 'template.json', d)
    history = original['requiredHistory'].copy()
    for name in ('seed-ledger.json', 'history-input.json'):
        history[str(FIXED / name)] = sha(FIXED / name)
    q = dict(version='tower-allocation-comparison-v1', contentRoot=str(PACKAGE / 'content'),
             templatePath=str(PACKAGE / 'template.json'), templateHash=sha(PACKAGE / 'template.json'),
             registryRoot=str(RUN.parent), outputRoot=str(RUN), requiredHistory=history,
             pendingHistoryRecoveries=original['pendingHistoryRecoveries'], recoveryReceiptHashes=original['recoveryReceiptHashes'],
             maximumSeconds=2380, maximumBytes=2147483648 - 128 * 1048576)
    save(PACKAGE / 'request.json', q)
    save(PACKAGE / 'preparation.json', dict(seconds=time.monotonic()-started, newFights=0, newValues=0,
        context='Captured fixed-team gameplay/content, with the new harness. Current-family calibration is a separate scope.',
        totalOwnedRunSeconds=2400, totalOwnedRunBytes=2147483648, maximumFights=20976,
        sourceGameplayHashes=read(CAPTURE / 'captured-context.json')['execution']['assemblyHashes'],
        runtimeFiles=inventory(PACKAGE / 'runtime'), templateSha256=sha(PACKAGE / 'template.json')))
    save(PACKAGE / 'prepared-files.json', inventory(PACKAGE))
    print('Prepared seed-free comparison; zero allocation and zero combat.', flush=True)


def execute():
    started = time.monotonic()
    # Create-new receipt prevents every second launch, even if the first failed before reservation.
    save(PACKAGE / 'launch.json', dict(attempts=1, retries=0, requestSha256=sha(PACKAGE / 'request.json')))
    assert not RUN.exists()
    for name, digest in read(PACKAGE / 'prepared-files.json').items():
        assert sha(PACKAGE / name) == digest, name
    module = importlib.util.spec_from_file_location('comparison_owner', PACKAGE / 'bounded_output_process.py')
    owner = importlib.util.module_from_spec(module)
    module.loader.exec_module(owner)
    last, peak = 0, 0
    def guard():
        nonlocal last, peak
        now = time.monotonic()
        if now-last >= 5:
            peak = max(peak, sum(p.stat().st_size for root in (PACKAGE, RUN) if root.exists() for p in root.rglob('*') if p.is_file()))
            assert peak < 2147483648 - 1048576, 'Combined storage ceiling reached'
            last = now
    result = owner.run(['dotnet', str(PACKAGE / 'runtime/BalanceHarness.dll'), 'tower-allocation-comparison-run',
                        str(PACKAGE / 'request.json')], ROOT, PACKAGE / 'native-console.log', started+2395,
                       cleanup_seconds=1, guard=guard)
    save(PACKAGE / 'process.json', result)
    guard()
    save(PACKAGE / 'execution.json', dict(seconds=time.monotonic()-started, sampledHighWaterBytes=peak,
        process=result, status='Completed' if result['exitCode'] == 0 and (RUN / 'result.json').exists() else 'TerminalFailure'))
    print(json.dumps(read(PACKAGE / 'execution.json')), flush=True)
    if result['exitCode'] != 0:
        raise SystemExit(result['exitCode'])


if __name__ == '__main__':
    {'prepare': prepare, 'execute': execute}[sys.argv[1]]()
