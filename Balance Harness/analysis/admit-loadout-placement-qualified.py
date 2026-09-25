"""Fresh plain admission using sealed runtime qualification and matched costs.

No preparation timing is repeated and no entropy, reservation or combat occurs.
"""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import shutil
import sys
import threading
import time
import os

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'build'))
import bounded_windows_process


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


pair = module('placement_admission_pair', ROOT / 'build/test-loadout-placement-qualified-pair.py')
base = module('placement_admission_history', Path(__file__).with_name('prepare-proposal-affinity-admission.py'))
owner = module('placement_admission_owner', ROOT / 'build/run-proposal-affinity-study.py')
sha, read, write, require = pair.sha, pair.read, pair.write, pair.require
PACKAGE = ROOT / 'TestResults/loadout-placement-admission-20260925'
OUTPUT = ROOT / 'TestResults/balance/tower-loadout-placement-pilot-01-20260925'
DECLARATION = ROOT / 'Balance Harness/Tower-Loadout-Placement-Admission-Declaration.json'
PREVIOUS = ROOT / 'TestResults/balance/tower-affinity-allied-action-pilot-01-20260924'
HELPER = Path(__file__).with_name('loadout-placement-admission-bind.ps1')
SECONDS, BYTES = 900, 1073741824


def check_forecast(forecast, maximum, context_ratio=1):
    require(forecast['resourceForecastPassed'] and forecast['status'] == 'ResourceForecastPassedFreshAdmissionRequired'
        and forecast['limits'] == pair.costs.pre.LIMITS and forecast['extraProtectedInventoryPasses'] == 1
        and forecast['timingMargin'] == 2 and forecast['publicationReserveSeconds'] == 120,
        'No passing qualified plain forecast')
    require(math.isfinite(context_ratio) and context_ratio >= 1, 'Invalid upward-only metadata factor')
    # Historical floors remain fixed minima, not per-context-byte coefficients.
    # Charge the entire measured current workload at the metadata growth factor,
    # including work that does not read history. Never discount a saved floor.
    bounds = {key: max(value, 2 * maximum['phaseCosts'][key] * context_ratio)
        for key, value in forecast['unroundedPhaseFloors'].items()}
    bounds['auditSeconds'] = max(bounds['auditSeconds'], 2 * context_ratio * (
        maximum['phaseCosts']['auditSeconds'] + maximum['finalVerificationSeconds']) + 120)
    inputs = {key: read(pair.OUTPUT / 'inputs' / (key + '.json'))
        for key in read(pair.OUTPUT / 'declaration.json')['costInputs']}
    probe = inputs['audit-probe']
    historical_bytes = inputs['probe-previous-forecast']['totalBytes'] / probe['inventoryByteRatio']
    storage_ratio = max(1, (bounds['nativeBytes'] + bounds['auditBytes']) / historical_bytes)
    extra = probe['extraInventorySeconds'] * max(1, storage_ratio / probe['inventoryByteRatio']) / 2
    bounds['auditSeconds'] = max(bounds['auditSeconds'], 2 * (probe['wholeWorkerSeconds'] + sum(probe['independentAuditSeconds']) + extra) + 120)
    bounds = {key: math.ceil(value) for key, value in bounds.items()}
    require(set(bounds) == set(forecast['limits']) and all(bounds[key] < forecast['limits'][key] for key in bounds),
        'Current metadata exceeds a frozen partition')
    return bounds


def register(pair_pin):
    require(not PACKAGE.exists() and not OUTPUT.exists() and not DECLARATION.exists(), 'No admission retry or existing pilot')
    pair.maximum.authenticate(pair.OUTPUT, pair_pin)
    require(read(pair.OUTPUT / 'completion.json')['status'] == 'CompletedMatchedPair', 'Incomplete matched pair')
    check_forecast(read(pair.OUTPUT / 'forecast.json'), read(pair.OUTPUT / 'maximum-workload.json'))
    previous_manifest = read(PREVIOUS / 'files.json')
    require(sha(PREVIOUS / 'request.json') == previous_manifest['request.json'], 'Changed previous history contract')
    sources = pair.SOURCES + [Path(__file__), HELPER, Path(base.__file__), base.HISTORY_HELPER, Path(owner.__file__),
        ROOT / 'build/bounded_windows_process.py', ROOT / 'Balance Harness/analysis/audit-proposal-affinity-study.py',
        ROOT / 'Balance Harness/analysis/test-loadout-placement-qualified-admission.py']
    write(DECLARATION, dict(version='tower-loadout-placement-qualified-admission-v1', pairManifestSha256=pair_pin,
        qualificationManifestSha256=pair.maximum.QUALIFIED_PIN, maximumManifestSha256=sha(pair.maximum.OUTPUT / 'files.json'),
        previousManifestSha256=sha(PREVIOUS / 'files.json'), previousRequestSha256=sha(PREVIOUS / 'request.json'),
        sourcePins={p.relative_to(ROOT).as_posix(): sha(p) for p in sources},
        package=str(PACKAGE), output=str(OUTPUT), chargedSeconds=SECONDS, chargedBytes=BYTES,
        attempts=1, retryPermitted=False, timingResamples=0,
        metadataRule='Change only history exclusions and the resulting plan scope binding. Retain every qualified historical floor; scale all measured maximum-workload phase costs and final verification upward by any context byte growth, with factor two and publication reserve. Apply resulting storage growth before the single-extra-inventory probe term. Reject any partition at its limit. No timing resampling or reduction of a prior floor.',
        maximumFights=21888, requiredFreshValues=4380, roots=12, frozenSearchesBeforeHeldout=24,
        actualCombat=0, productionEntropyDraws=0, newScientificReservations=0))
    return sha(DECLARATION)


def prepare(pin):
    require(sha(DECLARATION) == pin and not PACKAGE.exists() and not OUTPUT.exists(), 'Changed declaration or admission retry')
    declaration = read(DECLARATION)
    for path, expected in declaration['sourcePins'].items():
        require(sha(ROOT / path) == expected, 'Changed registered admission source: ' + path)
    pair.maximum.authenticate(pair.OUTPUT, declaration['pairManifestSha256'])
    require(sha(pair.maximum.QUALIFIED / 'files.json') == declaration['qualificationManifestSha256'], 'Changed runtime qualification')
    started = time.monotonic(); PACKAGE.mkdir(); failure = None
    write(PACKAGE / 'charge.json', dict(chargedSeconds=SECONDS, chargedBytes=BYTES, declarationSha256=pin, fullAllowanceChargedAtStart=True))
    watchdog = threading.Timer(SECONDS, lambda: os._exit(124)); watchdog.daemon = True; watchdog.start()
    def check():
        require(time.monotonic()-started < SECONDS-5 and owner.storage_bytes(PACKAGE) < BYTES-4*1048576 and not OUTPUT.exists(), 'Admission allowance or output changed')
    def command(name, args):
        result = bounded_windows_process.run(args, str(ROOT), str(PACKAGE / (name + '.log')), started+SECONDS-5,
            check=check, cleanup_seconds=1, log_byte_limit=1048576)
        write(PACKAGE / (name + '-process.json'), result); base.process_ok(result); check()
    try:
        with owner.writer_lease(OUTPUT.parent / 'complete-family-allocation'), owner.writer_lease(OUTPUT):
            shutil.copyfile(DECLARATION, PACKAGE / 'declaration.json')
            for path in declaration['sourcePins']:
                target = PACKAGE / 'source' / path; target.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(ROOT / path, target)
            old = read(PREVIOUS / 'request.json')
            require(sha(PREVIOUS / 'request.json') == declaration['previousRequestSha256'], 'Changed historical request')
            history_files, history = base.history(OUTPUT.parent, old); check()
            qualified = pair.maximum.QUALIFIED
            qualified_files = read(qualified / 'files.json')
            for name, expected in qualified_files.items():
                if name in ('runtime-context.json', 'runtime-plan.json', 'settings.json', 'runtime.json') or name.startswith(('runtime/', 'content/')):
                    require(sha(qualified / name) == expected, 'Changed consumed qualification file: ' + name)
            context = read(qualified / 'runtime-context.json')
            legacy = base.legacy_values(context)
            declared = legacy | set(context['scope']['generation']['seeds']) | {context['rootSeed']}
            declared.update(v for ref in context['scope']['references'] for v in ref['scenario']['seeds'])
            require(declared <= set(history), 'Live historical ledger omits captured context declarations; no silent import')
            context['scope']['excludedCombatSeeds'] = sorted(set(history)-legacy)
            write(PACKAGE / 'context-draft.json', context); write(PACKAGE / 'history.json', history)
            for source, name in [(qualified / 'runtime-plan.json', 'qualified-plan.json'), (qualified / 'settings.json', 'settings.json'),
                (qualified / 'runtime.json', 'runtime.json'), (HELPER, 'bind.ps1'), (pair.OUTPUT / 'forecast.json', 'qualified-forecast.json'),
                (ROOT / 'Balance Harness/analysis/audit-proposal-affinity-study.py', 'auditor.py'), (Path(owner.__file__), 'run-proposal-affinity-study.py'),
                (ROOT / 'build/bounded_windows_process.py', 'bounded_windows_process.py')]:
                shutil.copyfile(source, PACKAGE / name)
            shutil.copytree(qualified / 'runtime', PACKAGE / 'runtime'); shutil.copytree(qualified / 'content', PACKAGE / 'content'); check()
            for name, expected in read(PACKAGE / 'runtime.json').items():
                require(sha(PACKAGE / 'runtime' / name) == expected, 'Changed qualified runtime file')
            pwsh = Path(shutil.which('pwsh')).with_suffix('.dll')
            command('binding', ['dotnet', 'exec', '--runtimeconfig', str(PACKAGE / 'runtime/BalanceHarness.runtimeconfig.json'),
                '--depsfile', str(pwsh.with_suffix('.deps.json')), str(pwsh), '-NoProfile', '-File', str(PACKAGE / 'bind.ps1'), '-Package', str(PACKAGE)])
            expected_plan = read(PACKAGE / 'qualified-plan.json'); plan = read(PACKAGE / 'plan.json')
            require(plan == dict(expected_plan, scopeHash=plan['scopeHash']), 'Changed frozen scientific plan')
            require(read(PACKAGE / 'context.json') == context, 'Native context differs from the declared metadata change')
            baseline_bytes = (pair.maximum.OUTPUT / 'tower-proposal-owned-fixture-plain-maximum/package/context.json').stat().st_size
            ratio = max(1, (PACKAGE / 'context.json').stat().st_size / baseline_bytes)
            forecast = read(PACKAGE / 'qualified-forecast.json')
            maximum = read(pair.OUTPUT / 'maximum-workload.json')
            bounds = check_forecast(forecast, maximum, ratio)
            write(PACKAGE / 'resource-forecast.json', dict(qualifiedForecastSha256=sha(PACKAGE / 'qualified-forecast.json'),
                metadataByteRatio=ratio, roundedPhaseFloors=bounds, limits=forecast['limits'], admitted=True))
            q = dict(version=plan['version'], contentRoot=str(PACKAGE / 'content'), registryRoot=str(OUTPUT.parent), outputRoot=str(OUTPUT),
                requiredHistory=history_files, pendingHistoryRecoveries=old['pendingHistoryRecoveries'], recoveryReceiptHashes=old['recoveryReceiptHashes'],
                resourceEnvelope=owner.RESOURCE_V2)
            for name in ('plan', 'context', 'settings', 'history', 'runtime', 'auditor'):
                path = PACKAGE / ('auditor.py' if name == 'auditor' else name + '.json')
                q[name] = dict(path=str(path), sha256=sha(path))
            write(PACKAGE / 'request.json', q)
            command('native-check', ['dotnet', str(PACKAGE / 'runtime/BalanceHarness.dll'), 'tower-proposal-study-check', str(PACKAGE / 'request.json')])
            native = read(PACKAGE / 'native-check.log')
            require(native['status'] == 'InputsVerifiedNoReservation' and native['fights'] == native['newValues'] == 0, 'Native check failed')
            require(base.history(OUTPUT.parent, q) == (history_files, history), 'History changed during admission'); check()
            write(PACKAGE / 'admission.json', dict(version=plan['version'], status='ProposalStudyAdmittedNoReservation',
                requestSha256=sha(PACKAGE / 'request.json'), resourceEnvelope=owner.RESOURCE_V2, fights=0, newValues=0,
                qualificationManifestSha256=declaration['qualificationManifestSha256'], pairManifestSha256=declaration['pairManifestSha256'],
                resourceForecastSha256=sha(PACKAGE / 'resource-forecast.json'), chargedSeconds=SECONDS, chargedBytes=BYTES))
    except Exception as error:
        failure = str(error); write(PACKAGE / 'failure.json', dict(reason=failure, retryPermitted=False, scientificAdmitted=False))
    finally:
        watchdog.cancel()
    write(PACKAGE / 'completion.json', dict(status='Failed' if failure else 'AdmittedNoReservation', secondsBeforeSealing=time.monotonic()-started,
        recordedTimingIsLowerBound=True, chargedSeconds=SECONDS, chargedBytes=BYTES, actualCombat=0, productionEntropyDraws=0, newScientificReservations=0))
    write(PACKAGE / 'files.json', pair.maximum.inventory(PACKAGE))
    manifest_pin = sha(PACKAGE / 'files.json')
    if not failure: owner.validate_admission(PACKAGE / 'request.json', PACKAGE / 'runtime/BalanceHarness.dll', manifest_pin)
    print(json.dumps(dict(package=str(PACKAGE), manifestSha256=manifest_pin, failure=failure), indent=2))
    if failure: raise ValueError(failure)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('action', choices=['register', 'prepare'])
    parser.add_argument('--pair-pin'); parser.add_argument('--pin'); args = parser.parse_args()
    if args.action == 'register': print(register(args.pair_pin))
    else: prepare(args.pin)
