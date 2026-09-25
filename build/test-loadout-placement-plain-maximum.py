"""One prospectively pinned, plain, full-maximum fixture on the qualified runtime.

Synthetic outcomes, real captured inputs, production owned launcher and audits.
This is an absolute workload check, not a matched ratio or combat forecast.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import time
from types import SimpleNamespace

import bounded_windows_process

ROOT = Path(__file__).resolve().parents[1]
QUALIFIED = ROOT / 'TestResults/loadout-placement-runtime-qualification-compatible-helper-20260925'
QUALIFIED_PIN = '42da1105a3a207a760f0a53bc244faa2fa6eeca539829c5a995a58ff17fd4ac7'
PRIOR = ROOT / 'TestResults/loadout-placement-runtime-qualification-handoff-20260925.json'
PRIOR_PIN = '7bd6edeb9ea692f15f3a135366932318a6e195e3e438dbc79f75db141bef2008'
FAILED = ROOT / 'TestResults/loadout-placement-plain-maximum-20260925'
FAILED_PIN = '78d4af9bfe4d4b151eb2d21f69f7eeef1733de7ad8250296f667e46ca1fcc7f3'
OUTPUT = ROOT / 'TestResults/loadout-placement-plain-maximum-history-20260925'
DECLARATION = ROOT / 'Balance Harness/Tower-Loadout-Placement-Plain-Maximum-History-Declaration.json'
SECONDS = 12300  # existing 10800 owner + 120 preparation + 1200 verification + 180 support
BYTES = 6442450944 + 134217728  # existing owner plus bounded supporting files
PROFILE = 'plain-maximum-placement-v1'
SOURCES = [Path(__file__), ROOT / 'build/test-proposal-affinity-study-owned.py',
    ROOT / 'build/run-proposal-affinity-study.py', ROOT / 'build/bounded_windows_process.py',
    ROOT / 'build/test-loadout-placement-matched-owned.py',
    ROOT / 'Balance Harness/analysis/audit-proposal-affinity-study.py',
    ROOT / 'Balance Harness/analysis/loadout-placement-resource-precheck.py',
    ROOT / 'LL/tests/BalanceHarness.ProcessFixture/PlainMaximumFixtureHost.cs',
    ROOT / 'LL/tests/BalanceHarness.ProcessFixture/ProposalStudyFixtureHost.cs',
    ROOT / 'LL/tests/BalanceHarness.ProcessFixture/BalanceHarness.ProcessFixture.csproj',
    ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessPlainMaximumFixtureTests.cs']


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def inventory(root):
    return {p.relative_to(root).as_posix(): sha(p) for p in sorted(root.rglob('*')) if p.is_file()}


def authenticate(root, pin):
    require(sha(root / 'files.json') == pin, 'Changed sealed manifest: ' + str(root))
    expected = read(root / 'files.json')
    actual = inventory(root)
    actual.pop('files.json')
    require(actual == expected, 'Changed sealed membership or file: ' + str(root))


def register(host):
    require(not OUTPUT.exists() and not DECLARATION.exists(), 'No replacement declaration or timing retry')
    require(sha(PRIOR) == PRIOR_PIN, 'Changed prior handoff')
    authenticate(QUALIFIED, QUALIFIED_PIN)
    authenticate(FAILED, FAILED_PIN)
    require(not (FAILED / 'tower-proposal-owned-fixture-plain-maximum/result/attempts.jsonl').exists(),
        'Original failure is no longer an input rejection before reports')
    require(host.name == 'BalanceHarness.ProcessFixture.dll', 'Require separate test host')
    runtime = read(QUALIFIED / 'runtime.json')
    input_pins = {name: pin for name, pin in read(QUALIFIED / 'files.json').items()
        if name in ('runtime-context.json', 'runtime-plan.json', 'settings.json', 'runtime.json') or name.startswith('content/')}
    for name, pin in runtime.items():
        require(sha(host.parent / name) == pin, 'Test host does not use qualified runtime: ' + name)
    host_names = ['BalanceHarness.ProcessFixture' + suffix for suffix in ('.dll', '.pdb', '.deps.json', '.runtimeconfig.json')]
    write(DECLARATION, dict(version='plain-maximum-placement-history-v2', profile=PROFILE, output=str(OUTPUT), qualifiedManifestSha256=QUALIFIED_PIN,
        preservedFailedManifestSha256=FAILED_PIN, preservedFailedCharge=dict(seconds=SECONDS, bytes=BYTES),
        repair='Complete the fixture history with all context-declared values, rebind only exclusions and plan scope hash, and perform production inspection before launch; no physical or outcome changes.',
        priorHandoffSha256=PRIOR_PIN, fixtureHost=str(host),
        sourcePins={str(p.relative_to(ROOT).as_posix()): sha(p) for p in SOURCES},
        hostPins={name: sha(host.parent / name) for name in host_names}, runtime=runtime, qualifiedInputPins=input_pins,
        attempts=1, retryPermitted=False, chargedMaximumSeconds=SECONDS, chargedMaximumBytes=BYTES,
        fullAllowanceChargedAtStart=True, nativeMaximumSeconds=9000, auditPublicationMaximumSeconds=1800,
        nativeMaximumBytes=5905580032, auditPublicationMaximumBytes=536870912,
        preparationMaximumSeconds=120, finalVerificationMaximumSeconds=1200, supportMaximumSeconds=180,
        requiredSearchReports=12672, requiredHeldoutReports=9216, requiredTotalReports=21888,
        requiredDistinctHeldoutMembers=36, requiredFrozenSearches=24, requiredPlacementCatalogues=12,
        literalEntropy='16384 little-endian words 1000000000+i; production historical exclusion/classification; no refill',
        literalOutcome='References draw; generated teams win training and nomination; first five validation observations win; heldout wins',
        plainEvidence=True, realCapturedContent=True, realInputHashes=True, productionNativeAudits=True,
        actualCombat=0, productionEntropyDraws=0, scientificReservations=0,
        costInterpretation='Absolute maximum-workload engineering costs only. Keep all inherited floors and failed native denominator ceilings. No substitution for the separately registered matched pair, no timing discount, and no combat forecast.',
        qualifiedCurrentForecast=None, scientificAdmitted=False))
    return sha(DECLARATION)


def worker():
    declaration = read(OUTPUT / 'declaration.json')
    fixture = module('plain_maximum_owned', ROOT / 'build/test-proposal-affinity-study-owned.py')
    args = SimpleNamespace(fixture_host=OUTPUT / 'host/BalanceHarness.ProcessFixture.dll', source_fixture=OUTPUT / 'qualified-inputs',
        output=OUTPUT / 'tower-proposal-owned-fixture-plain-maximum', mode='complete', profile=PROFILE,
        resource_envelope='tower-proposal-resource-envelope-v2')
    summary = fixture.execute(args, fixture.prepare(args))
    require(summary['literalReports'] == declaration['requiredTotalReports'], 'Incomplete maximum fixture')
    result = args.output / 'result'
    freeze = read(result / 'study/freeze.json')
    require(len(freeze['families']) == 12 and all(len(f['members']) == 3 for f in freeze['families']), 'Missing physical members')
    require(not list(result.rglob('evidence-storage.json')), 'Compressed study evidence is outside this declaration')
    phase = module('plain_maximum_costs', ROOT / 'build/test-loadout-placement-matched-owned.py')
    costs = phase.phase_costs(read(result / 'completion.json'), read(result / 'closeout.json'))
    write(OUTPUT / 'maximum-workload.json', dict(status='PlainMaximumWorkloadVerified', fixtureOnly=True,
        literalReports=21888, searchReports=12672, heldoutReports=9216, heldoutMembers=36,
        searches=24, catalogues=12, phaseCosts=costs, finalVerificationSeconds=summary['verificationProcess']['seconds'],
        nativeAuditSubstituted=False, productionAuditPassed=True, independentPythonAuditPassed=True,
        nativePublicationBarrierPassed=True, nativePostPublicationVerificationPassed=True,
        qualifiedRuntimeSha256=declaration['runtime']['BalanceHarness.dll'], plainEvidence=True,
        actualCombat=0, productionEntropyDraws=0, scientificReservations=0,
        qualifiedCurrentForecast=None, scientificAdmitted=False, usableForAdmission=False,
        next='Separately registered matched cost pair and upward-only reconciliation; synthetic outcomes establish no search effectiveness.'))


def run(pin):
    require(not OUTPUT.exists() and sha(DECLARATION) == pin, 'Changed declaration or attempted retry')
    declaration = read(DECLARATION)
    require(sha(PRIOR) == PRIOR_PIN and declaration['priorHandoffSha256'] == PRIOR_PIN, 'Changed prior handoff')
    for path, expected in declaration['sourcePins'].items():
        require(sha(ROOT / path) == expected, 'Changed registered implementation: ' + path)
    host = Path(declaration['fixtureHost'])
    for name, expected in (declaration['hostPins'] | declaration['runtime']).items():
        require(sha(host.parent / name) == expected, 'Changed registered runtime: ' + name)
    require(sha(QUALIFIED / 'files.json') == QUALIFIED_PIN, 'Changed qualification manifest')
    for name, expected in declaration['qualifiedInputPins'].items():
        require(sha(QUALIFIED / name) == expected, 'Changed qualified input: ' + name)
    OUTPUT.mkdir()
    started = time.monotonic()
    write(OUTPUT / 'charge.json', dict(chargedSeconds=SECONDS, chargedBytes=BYTES, declarationSha256=pin,
        fullAllowanceChargedAtStart=True, retryPermitted=False))
    failure = None
    try:
        shutil.copyfile(DECLARATION, OUTPUT / 'declaration.json')
        shutil.copyfile(PRIOR, OUTPUT / 'prior-handoff.json')
        shutil.copyfile(FAILED / 'files.json', OUTPUT / 'failed-files.json')
        for name, expected in declaration['qualifiedInputPins'].items():
            target = OUTPUT / 'qualified-inputs' / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(QUALIFIED / name, target)
            require(sha(target) == expected, 'Changed copied qualified input: ' + name)
        (OUTPUT / 'host').mkdir()
        for name in declaration['hostPins'] | declaration['runtime']:
            target = OUTPUT / 'host' / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(host.parent / name, target)
        for path in declaration['sourcePins']:
            target = OUTPUT / 'implementation' / path
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / path, target)
        last_check = 0
        def check():
            nonlocal last_check
            if time.monotonic() - last_check >= 1:
                last_check = time.monotonic()
                require(sum(p.stat().st_size for p in OUTPUT.rglob('*') if p.is_file()) < BYTES - 4 * 1048576,
                    'Maximum fixture storage allowance reached')
        receipt = bounded_windows_process.run([sys.executable, '-B', '-X', 'utf8', str(Path(__file__)), 'worker'],
            str(ROOT), str(OUTPUT / 'worker.log'), started + SECONDS - 10, check=check, cleanup_seconds=1)
        write(OUTPUT / 'worker-process.json', receipt)
        require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
            'Maximum fixture failed; see worker.log. No retry allowed.')
    except Exception as error:
        failure = str(error)
        write(OUTPUT / 'failure.json', dict(reason=failure, retryPermitted=False, scientificAdmitted=False))
    write(OUTPUT / 'completion.json', dict(status='Failed' if failure else 'MaximumWorkloadVerifiedCostPairPending',
        secondsBeforeSealing=time.monotonic()-started, bytesBeforeSealing=sum(p.stat().st_size for p in OUTPUT.rglob('*') if p.is_file()),
        recordedTimingIsLowerBound=True, fullCharge=dict(seconds=SECONDS, bytes=BYTES),
        actualCombat=0, productionEntropyDraws=0, scientificReservations=0, qualifiedCurrentForecast=None, scientificAdmitted=False))
    write(OUTPUT / 'files.json', inventory(OUTPUT))
    print(json.dumps(dict(output=str(OUTPUT), manifestSha256=sha(OUTPUT / 'files.json'), failed=failure), indent=2))
    if failure:
        raise ValueError(failure)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['register', 'run', 'worker'])
    parser.add_argument('--host', type=Path)
    parser.add_argument('--pin')
    args = parser.parse_args()
    if args.action == 'register':
        print(register(args.host.resolve()))
    elif args.action == 'run':
        run(args.pin)
    else:
        worker()
