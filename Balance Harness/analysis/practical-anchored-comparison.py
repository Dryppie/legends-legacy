"""One prospective anchored-neighborhood comparison; create-new preparation and launch, never retry."""
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
PACKAGE = ROOT / 'TestResults/anchored-comparison-20260917'
RUN = ROOT / 'TestResults/balance/tower-anchored-comparison-20260917'
PREVIOUS = ROOT / 'TestResults/balance/tower-allocation-comparison-20260917'
PREVIOUS_PACKAGE = ROOT / 'TestResults/allocation-comparison-20260917'
CAPTURE = ROOT / 'TestResults/current-tower-admission-readiness-20260917'
BUILD = ROOT / 'TestResults/anchored-comparison-build-20260917/bin/BalanceHarness/release'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


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
    shutil.copyfile(ROOT / 'Balance Harness/analysis/audit-anchored-comparison.py', PACKAGE / 'audit-source.py')
    shutil.copyfile(ROOT / 'Balance Harness/Tower-Practical-Anchored-Neighborhood-Comparison.md', PACKAGE / 'prospective-design.md')
    sources = ['TowerAnchoredComparison.cs', 'TowerAllocationComparison.cs', 'TowerAnchoredNeighborhoodSearch.cs',
               'TowerSuppliedCompositionSearch.cs', 'TowerBossStudy.cs', 'TowerBossStudyArchive.cs']
    (PACKAGE / 'source').mkdir()
    for name in sources:
        shutil.copyfile(ROOT / 'LL/tools/BalanceHarness' / name, PACKAGE / 'source' / name)
    subprocess.run(['pwsh', '-NoProfile', '-File', str(ROOT / 'Balance Harness/analysis/practical-native-verification/native-context.ps1'),
                    '-Runtime', str(PACKAGE / 'runtime'), '-ContentRoot', str(PACKAGE / 'content'),
                    '-Output', str(PACKAGE / 'context.json')], check=True, timeout=30)
    context = read(PACKAGE / 'context.json')
    d, original = read(PREVIOUS_PACKAGE / 'template.json'), read(PREVIOUS_PACKAGE / 'request.json')
    ledger = read(PREVIOUS / 'seed-ledger.json')
    d['excludedCombatSeeds'] = sorted(set(ledger['historical'] + ledger['reserved']))
    assert len(d['excludedCombatSeeds']) == 501467
    d['executionHash'] = context['executionHash']
    assert d['settingsHash'] == context['settingsHash']
    for name, digest in d['contentHashes'].items():
        assert sha(PACKAGE / 'content/Data' / name) == digest
    d['id'] = 'anchored-comparison-template'
    d['generation'].update(seeds=[], candidatesPerArm=46, maximumAttemptsPerArm=256, policyVersion='anchored-neighborhood-v1')
    d['stages']['schedules'] = {'fixed-equipment': dict(discovery=[], selection=[], confirmation=[], diagnostics=[])}
    d['primaryReferenceId'] = next(s['referenceId'] for s in d['starts'] if s['party']['id'].startswith('399bc776'))
    assert len(d['starts']) == 2 and d['starts'] == read(PREVIOUS_PACKAGE / 'template.json')['starts']
    d['maximumBattles'] = 3496
    save(PACKAGE / 'template.json', d)
    history = original['requiredHistory'].copy()
    for name in ('seed-ledger.json', 'history-input.json'):
        history[str(PREVIOUS / name)] = sha(PREVIOUS / name)
    q = dict(version='tower-anchored-comparison-v1', contentRoot=str(PACKAGE / 'content'),
             templatePath=str(PACKAGE / 'template.json'), templateHash=sha(PACKAGE / 'template.json'),
             registryRoot=str(RUN.parent), outputRoot=str(RUN), requiredHistory=history,
             pendingHistoryRecoveries=original['pendingHistoryRecoveries'], recoveryReceiptHashes=original['recoveryReceiptHashes'],
             maximumSeconds=2380, maximumBytes=2147483648 - 128 * 1048576)
    save(PACKAGE / 'request.json', q)
    shape = dict(q)
    shape.update(version='tower-practical-allocated-search-v1', definitionPath=shape.pop('templatePath'),
                 definitionHash=shape.pop('templateHash'), allocation=dict(master=0, domain='anchored-shape-only',
                 discoverySamples=8, selectionSamples=32, confirmationSamples=1000))
    save(PACKAGE / 'shape-check-request.json', shape)
    save(PACKAGE / 'preparation.json', dict(seconds=time.monotonic()-started, newFights=0, newValues=0,
        context='Captured fixed-team gameplay/content, new harness; current-family calibration remains separate.',
        totalOwnedRunSeconds=2400, totalOwnedRunBytes=2147483648, maximumFights=20976,
        sourceGameplayHashes=read(CAPTURE / 'captured-context.json')['execution']['assemblyHashes'],
        runtimeFiles=inventory(PACKAGE / 'runtime'), templateSha256=sha(PACKAGE / 'template.json')))
    save(PACKAGE / 'prepared-files.json', inventory(PACKAGE))
    print('Prepared seed-free anchored comparison; zero allocation and zero combat.', flush=True)


def execute():
    started = time.monotonic()
    save(PACKAGE / 'launch.json', dict(attempts=1, retries=0, requestSha256=sha(PACKAGE / 'request.json')))
    assert not RUN.exists()
    for name, digest in read(PACKAGE / 'prepared-files.json').items():
        assert sha(PACKAGE / name) == digest, name
    module = importlib.util.spec_from_file_location('anchored_owner', PACKAGE / 'bounded_output_process.py')
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
    result = owner.run(['dotnet', str(PACKAGE / 'runtime/BalanceHarness.dll'), 'tower-anchored-comparison-run',
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
