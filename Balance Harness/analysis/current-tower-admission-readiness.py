"""Capture an admission runtime and request. No party preparation, combat, allocation or launch."""
import collections
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'TestResults/current-tower-admission-readiness-20260917'
TEST = ROOT / 'TestResults/current-tower-admission-compatibility-20260917'
OLD = ROOT / 'TestResults/fixed-team-confirmation-admission-20260917-03'
DESIGN = ROOT / 'TestResults/current-tower-calibration-design-20260917'
IMPL = ROOT / 'TestResults/current-tower-admission-implementation-20260917'
BUILD = IMPL / 'test-build/bin/BalanceHarness/release'
CONTENT = ROOT / 'LL/src/API/API.LL'
HELPER = ROOT / 'Balance Harness/analysis/current-tower-admission-context.ps1'
OWNER = ROOT / 'TestResults/practical-native-verification-20260917/bounded_output_process.py'
DESIGN_HASH = 'bf454b4228637406847c10186e75a277bb47d825cc77a10fa6011221adf656b6'
IMPL_HASH = '23c465c2ee6a05bbeaf2277ee47d2e033276f4ee3a027bf6b4f705433a87f5ba'
OWNER_HASH = '120c2447fa5414d1d0062dcd4fc4eeb2ca70f744b8bb6e447f7f2325fa155332'
MIB = 1048576

def require(ok, message):
    if not ok:
        raise ValueError(message)

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def save(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2, ensure_ascii=False, allow_nan=False)
        stream.write('\n')
        stream.flush()
        os.fsync(stream.fileno())

def files(root):
    found = {}
    for directory, dirs, names in os.walk(root, followlinks=False):
        for name in dirs + names:
            p = Path(directory) / name
            require(not p.stat(follow_symlinks=False).st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT, 'Linked path: ' + str(p))
        for name in names:
            p = Path(directory) / name
            found[p.relative_to(root).as_posix()] = p
    return found

def inventory(root):
    return {n: sha(p) for n, p in sorted(files(root).items())}

def authenticate(root, manifest_hash):
    require(sha(root / 'files.json') == manifest_hash, 'Changed source manifest: ' + str(root))
    for name, digest in read(root / 'files.json').items():
        require(sha(root / name) == digest, 'Changed source member: ' + name)

def load_owner():
    require(sha(OWNER) == OWNER_HASH, 'Changed retained process owner')
    spec = importlib.util.spec_from_file_location('admission_process_owner', OWNER)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def prepare():
    require(not OUT.exists() and not TEST.exists(), 'Use new preparation directories; existing evidence is never overwritten')
    authenticate(DESIGN, DESIGN_HASH)
    authenticate(IMPL, IMPL_HASH)
    old_manifest = read(OLD / 'files.json')
    for name in ['runtime-files.json', 'settings-and-execution.json', 'source-files.json']:
        require(sha(OLD / name) == old_manifest[name], 'Changed retained source: ' + name)
    for name, digest in read(IMPL / 'source-pins.json').items():
        if name.endswith('.cs'):
            require(sha(ROOT / name) == digest, 'Changed verified adapter source: ' + name)
    prior_runtime = read(OLD / 'runtime-files.json')
    differences = []
    for name, digest in prior_runtime.items():
        require(sha(OLD / 'runtime' / name) == digest, 'Changed retained runtime: ' + name)
        if sha(BUILD / name) != digest:
            differences.append(name)
    require(set(differences) == {'BalanceHarness.dll', 'Application.dll', 'Common.dll', 'Domain.dll', 'Services.LL.dll'},
            'Unexpected runtime/dependency change')
    names = subprocess.check_output(['rg', '--files', '-g', '*.cs', '-g', '*.csproj', '-g', '*.md', '-g', '*.json',
                                    'LL/tools/BalanceHarness', 'LL/tests/EssenceSystem.Tests', 'Balance Harness'], cwd=ROOT, text=True).splitlines()
    baseline = {name.replace('\\', '/'): sha(ROOT / name) for name in names}
    OUT.mkdir()
    TEST.mkdir()
    save(TEST / 'working-baseline.json', baseline)
    # Preserve all historical evidence covered by the prior pre-edit baseline, including seed ledgers.
    evidence = {p: h for p, h in read(IMPL / 'baseline.json').items() if p.startswith('TestResults/')}
    for p, digest in evidence.items():
        require(sha(ROOT / p) == digest, 'Changed historical evidence: ' + p)
    save(TEST / 'historical-baseline.json', evidence)
    save(OUT / 'preparation-scope.json', {'status': 'ReadinessPreparationOnly', 'fights': 0, 'newValues': 0,
         'realFamilyPreparations': 0, 'engineering': 'Separate unmetered capture, fixture verification and documentation; no old allowance reused',
         'futureEnvelopeProposed': {'seconds': 1800, 'bytes': 2048 * MIB}, 'futureLaunchAuthorizedByThisPreparation': False})
    shutil.copyfile(Path(__file__), OUT / 'prepare-source.py')
    shutil.copyfile(HELPER, OUT / HELPER.name)
    shutil.copyfile(OWNER, OUT / OWNER.name)
    provenance = {}
    for name, digest in prior_runtime.items():
        source = BUILD / name if name == 'BalanceHarness.dll' else OLD / 'runtime' / name
        target = OUT / 'runtime' / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
        require(sha(target) == sha(source), 'Runtime copy mismatch')
        provenance[name] = {'sha256': sha(target), 'source': source.relative_to(ROOT).as_posix(),
                            'origin': 'verified-adapter-build' if name == 'BalanceHarness.dll' else 'retained-gameplay-runtime'}
    save(OUT / 'runtime-provenance.json', provenance)
    save(OUT / 'runtime-files.json', inventory(OUT / 'runtime'))
    save(OUT / 'source-files.json', {p: h for p, h in baseline.items() if p.endswith(('.cs', '.csproj'))})
    owner = load_owner()
    def bounded(label, command, seconds=120):
        def guard():
            require(sum(p.stat().st_size for p in files(OUT).values()) < 64 * MIB, 'Readiness package exceeds future setup allowance')
        result = owner.run(command, ROOT, OUT / (label + '.log'), time.monotonic() + seconds - 1, guard=guard)
        save(OUT / (label + '-process.json'), result)
        require(result['exitCode'] == 0 and not result['timedOut'] and result['activeProcesses'] == 0, 'Failed readiness check: ' + label)
    bounded('live-context', ['pwsh', '-NoProfile', '-File', OUT / HELPER.name, '-Runtime', OUT / 'runtime',
                            '-ContentRoot', CONTENT, '-Output', OUT / 'live-context.json'])
    live = read(OUT / 'live-context.json')
    summary = read(DESIGN / 'inventory-summary.json')
    require(live['settingsHash'] == summary['settingsHash'], 'Changed effective Tower settings')
    for name in ['Application', 'Common', 'Domain', 'Services.LL']:
        require(live['execution']['assemblyHashes'][name] == summary['execution']['assemblyHashes'][name], 'Changed gameplay assembly')
    for name, digest in summary['contentHashes'].items():
        source = CONTENT / 'Data' / name
        require(sha(source) == digest, 'Changed content: ' + name)
        target = OUT / 'content/Data' / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
    save(OUT / 'content/appsettings.json', {'Combat': {'ThreatAndTanking': live['settings']['threat'],
         'IdleProgression': {'EncounterCadenceSeconds': live['idleCadenceSeconds']}},
         'WorldTower': {'CombatTicksPerFrame': live['settings']['checkpointIntervalTicks']}})
    save(OUT / 'content-files.json', inventory(OUT / 'content'))
    bounded('captured-context', ['pwsh', '-NoProfile', '-File', OUT / HELPER.name, '-Runtime', OUT / 'runtime',
                               '-ContentRoot', OUT / 'content', '-DesignRoot', DESIGN, '-Output', OUT / 'captured-context.json'])
    context = read(OUT / 'captured-context.json')
    require(context['executionHash'] == live['executionHash'] and context['settingsHash'] == live['settingsHash'], 'Captured context differs')
    require(context['inventory']['cells'] == 46077 and context['inventory']['occurrences'] == 51624
            and context['inventory']['forcedInputCells'] == 973 and context['inventory']['incompatibleOccurrences'] == 162,
            'Changed native-loaded static coverage')
    bounded('cli-help', ['dotnet', OUT / 'runtime/BalanceHarness.dll', '--help'], 30)
    # Isolate the existing fixture assembly, then bind every captured runtime member before rerunning fixtures.
    fixture_source = IMPL / 'test-build/bin/EssenceSystem.Tests/release'
    fixture_target = TEST / 'test-build/bin/EssenceSystem.Tests/release'
    shutil.copytree(fixture_source, fixture_target)
    for name in prior_runtime:
        shutil.copyfile(OUT / 'runtime' / name, fixture_target / name)
    # The SDK imports its test-project marker from the generated NuGet props even with -NoBuild.
    obj_target = TEST / 'test-build/obj/EssenceSystem.Tests'
    obj_target.mkdir(parents=True)
    for source in (IMPL / 'test-build/obj/EssenceSystem.Tests').iterdir():
        if source.is_file():
            shutil.copyfile(source, obj_target / source.name)
    save(TEST / 'test-input-files.json', inventory(fixture_target))
    if (ROOT / 'TestResults/tests/tests.trx').exists():
        shutil.copyfile(ROOT / 'TestResults/tests/tests.trx', TEST / 'previous-tests.trx')
    save(OUT / 'request.proposed.json', {'version': 'tower-current-family-admission-v1', 'designRoot': str(DESIGN),
         'contentRoot': str(OUT / 'content'), 'executionHash': context['executionHash'], 'maximumSeconds': 1500,
         'maximumBytes': 1920 * MIB})
    exceptions = read(DESIGN / 'incompatibilities.json')
    groups = collections.defaultdict(list)
    for row in exceptions:
        key = ' | '.join(sorted(row['reasons']))
        groups[key].append(row)
    save(OUT / 'context-disposition-plan.json', {'status': 'Unresolved', 'excludedByThisStep': 0,
         'familyFreezeAllowed': False, 'groups': [
             {'reasons': key.split(' | '), 'occurrences': len(rows), 'ordinals': [r['ordinal'] for r in rows],
              'anchorOccurrences': sum(r['anchorReason'] is not None for r in rows),
              'requiredDisposition': 'Resolve scope and native legality using authenticated original contexts; never silently discard or treat as weak.'}
             for key, rows in sorted(groups.items())],
         'decisionRule': 'Any required unresolved context blocks full-family freeze and broader balance acceptance.'})
    save(OUT / 'launch-plan.json', {'status': 'ProposedNotExecuted', 'outerSeconds': 1800, 'outerBytes': 2048 * MIB,
         'nativeSeconds': 1500, 'nativeBytes': 1920 * MIB, 'setupPackageBytesCeiling': 64 * MIB,
         'outerLogsAndCloseoutBytesCeiling': 64 * MIB, 'nonNativeSecondsReserve': 300,
         'output': str(ROOT / 'TestResults/current-tower-native-admission-20260917'),
         'command': ['dotnet', str(OUT / 'runtime/BalanceHarness.dll'), 'tower-current-family-admit',
                     str(OUT / 'request.proposed.json'), str(ROOT / 'TestResults/current-tower-native-admission-20260917/native')],
         'expectedCompletedExitCode': 3, 'expectedStatusIfAllPrepare': 'AdmittedWithContextExceptions',
         'retries': 0, 'resume': False, 'combat': 0, 'freshValues': 0,
         'enforcement': 'Retained owned Windows Job Object plus combined package/output/log byte guard and reserved cleanup; bind a single-use launch receipt before execution.',
         'onNativeInvalidOrResourceStop': 'Preserve evidence and stop; no thinning, cap increase or retry.'})
    print(json.dumps({'status': 'CapturedStaticCompatibilityPassedFixtureVerificationPending',
                      'executionHash': context['executionHash'], 'runtimeFiles': len(prior_runtime),
                      'nativePreparations': 0, 'fights': 0, 'newValues': 0}))

if __name__ == '__main__':
    require(sys.argv[1:] == ['prepare'], 'Only prepare is supported; this script cannot launch admission')
    prepare()
