"""Single registered matched pair after full plain maximum-workload coverage.

Original literal rules and physical fixture; qualified runtime and current owner.
No scientific entropy, combat, normalization, retries or raised phase limits.
"""
import argparse
import json
from pathlib import Path
import shutil
import sys
import time
from types import SimpleNamespace

import bounded_windows_process
import importlib.util

ROOT = Path(__file__).resolve().parents[1]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


maximum = module('qualified_pair_maximum', ROOT / 'build/test-loadout-placement-plain-maximum.py')
matched = module('qualified_pair_original', ROOT / 'build/test-loadout-placement-matched-owned.py')
costs = module('qualified_pair_costs', ROOT / 'Balance Harness/analysis/loadout-placement-qualified-costs.py')
sha, read, write, require = maximum.sha, maximum.read, maximum.write, maximum.require
OUTPUT = ROOT / 'TestResults/loadout-placement-qualified-pair-20260925'
DECLARATION = ROOT / 'Balance Harness/Tower-Loadout-Placement-Qualified-Pair-Declaration.json'
SECONDS, BYTES = 24420, 13019119616
AMENDMENT = ROOT / 'Balance Harness/Tower-Loadout-Placement-Publication-Inventory-Amendment.json'
FAILED = ROOT / 'TestResults/loadout-placement-matched-owned-20260924'
FAILED_PIN = '7dd1df4ab8ea9d24c53d50cac4137bdb59351a56110e9642cd791474e3f02605'
RECOVERY = ROOT / 'TestResults/loadout-placement-recovery-protocol-20260924'
FAILED_NATIVE = FAILED / 'tower-proposal-owned-fixture-matched-baseline/result/native-receipt.json'
SOURCES = maximum.SOURCES + [Path(__file__), ROOT / 'Balance Harness/analysis/loadout-placement-qualified-costs.py',
    ROOT / 'Balance Harness/analysis/test-loadout-placement-qualified-costs.py',
    ROOT / 'Balance Harness/analysis/loadout-placement-recovery-protocol.py', AMENDMENT]


def register():
    require(not OUTPUT.exists() and not DECLARATION.exists(), 'No replacement registration or timing retry')
    maximum_pin = sha(maximum.OUTPUT / 'files.json')
    maximum.authenticate(maximum.OUTPUT, maximum_pin)
    require(read(maximum.OUTPUT / 'completion.json')['status'] == 'MaximumWorkloadVerifiedCostPairPending', 'Maximum fixture not verified')
    coverage = read(maximum.OUTPUT / 'maximum-workload.json')
    require(coverage['status'] == 'PlainMaximumWorkloadVerified' and coverage['literalReports'] == 21888
        and coverage['productionAuditPassed'] and not coverage['nativeAuditSubstituted'], 'Missing full production coverage')
    absolute = {key: 2 * value for key, value in coverage['phaseCosts'].items()}
    absolute['auditSeconds'] = 2 * (coverage['phaseCosts']['auditSeconds'] + coverage['finalVerificationSeconds']) + 120
    require(all(absolute[key] < costs.pre.LIMITS[key] for key in absolute),
        'Maximum-workload absolute floor already exceeds a frozen partition; do not measure another pair')
    maximum.authenticate(FAILED, FAILED_PIN)
    costs.recovery.validate_retained(RECOVERY)
    cost_inputs = {key: dict(path=str(RECOVERY / 'precheck' / path), sha256=sha(RECOVERY / 'precheck' / path))
        for key, path in read(RECOVERY / 'precheck/inputs.json').items()}
    require(sha(ROOT / matched.PLAN) == matched.PLAN_PIN, 'Changed original matched contract')
    amendment = read(AMENDMENT)
    require(amendment['assessment']['necessaryGatePassed'], 'Closed necessary gate')
    for path, pin in amendment['sourceBindings'].items():
        require(sha(ROOT / path) == pin, 'Changed inventory amendment proof: ' + path)
    source = ROOT / Path(read(ROOT / matched.PLAN)['physicalContextSource']).parent.parent
    require(sha(source / 'files.json') == read(ROOT / matched.PLAN)['sourceFixtureManifestSha256'], 'Changed physical source')
    source_pins = {name: read(source / 'files.json')['source/' + name + '.json'] for name in ('context', 'plan', 'settings', 'history', 'runtime')}
    for name, pin in source_pins.items():
        require(sha(source / 'source' / (name + '.json')) == pin, 'Changed physical source file')
    host_pins = maximum.inventory(maximum.OUTPUT / 'host')
    write(DECLARATION, dict(version='tower-loadout-placement-qualified-pair-v1', output=str(OUTPUT),
        maximumManifestSha256=maximum_pin, failedPairManifestSha256=FAILED_PIN,
        originalPlanSha256=matched.PLAN_PIN, amendmentSha256=sha(AMENDMENT),
        sourcePins={p.relative_to(ROOT).as_posix(): sha(p) for p in SOURCES}, hostPins=host_pins,
        costInputs=cost_inputs, failedNativeReceiptSha256=sha(FAILED_NATIVE),
        physicalSource=str(source), physicalPins=source_pins, profiles=list(matched.PROFILES),
        attemptsPerProfile=1, retryPermitted=False, executionOrder=['baseline', 'placement'],
        maximumSeconds=SECONDS, maximumBytes=BYTES, fullPairAllowanceChargedAtStart=True,
        perOwnerMaximumSeconds=10800, perOwnerMaximumBytes=6442450944,
        nativeMaximumSeconds=9000, auditMaximumSeconds=1800, nativeMaximumBytes=5905580032, auditMaximumBytes=536870912,
        preparationSecondsPerFixture=120, verificationSecondsPerFixture=1200, supportingSeconds=180,
        costRule='Pin the qualified-costs implementation before either matched owner starts. Keep failed native denominator ceilings; max(original placement, new candidate) numerators; every ratio >=1; factor two and 120-second reserve; all inherited floors; exactly one extra protected inventory pass; absolute maximum-workload floors including final native verification. No per-report normalization or favorable resampling.',
        actualCombat=0, productionEntropyDraws=0, scientificReservations=0, scientificAdmitted=False))
    return sha(DECLARATION)


def worker():
    declaration = read(OUTPUT / 'declaration.json')
    fixture = module('qualified_owned_fixture', ROOT / 'build/test-proposal-affinity-study-owned.py')
    prepared = []
    for profile, label in zip(matched.PROFILES, ('baseline', 'placement')):
        args = SimpleNamespace(fixture_host=OUTPUT / 'host/BalanceHarness.ProcessFixture.dll', source_fixture=OUTPUT / 'source-fixture',
            output=OUTPUT / ('tower-proposal-owned-fixture-qualified-' + label), mode='complete', profile=profile,
            resource_envelope='tower-proposal-resource-envelope-v2')
        prepared.append((args, fixture.prepare(args)))
    write(OUTPUT / 'matched-preflight.json', matched.check_pair(prepared[0][1][3], prepared[1][1][3]))
    phases = []
    for args, state in prepared:
        write(args.output / 'measurement-start.json', dict(profile=args.profile, attempts=1, retryPermitted=False))
        fixture.execute(args, state)
        phases.append(matched.phase_costs(read(args.output / 'result/completion.json'), read(args.output / 'result/closeout.json')))
    write(OUTPUT / 'shared-v5.json', matched.verify_shared(prepared[0][0].output / 'result', prepared[1][0].output / 'result'))
    inputs = {key: read(OUTPUT / 'inputs' / (key + '.json')) for key in declaration['costInputs']}
    result = costs.forecast(inputs, read(OUTPUT / 'failed-native-receipt.json'), phases[0], phases[1], read(OUTPUT / 'maximum-workload.json'))
    result['maximumManifestSha256'] = declaration['maximumManifestSha256']
    write(OUTPUT / 'forecast.json', result)


def run(pin):
    require(not OUTPUT.exists() and sha(DECLARATION) == pin, 'Changed declaration or forbidden retry')
    declaration = read(DECLARATION)
    maximum.authenticate(maximum.OUTPUT, declaration['maximumManifestSha256'])
    for path, expected in declaration['sourcePins'].items():
        require(sha(ROOT / path) == expected, 'Changed registered source: ' + path)
    for name, expected in declaration['hostPins'].items():
        require(sha(maximum.OUTPUT / 'host' / name) == expected, 'Changed host: ' + name)
    OUTPUT.mkdir(); started = time.monotonic(); failure = None
    write(OUTPUT / 'charge.json', dict(chargedSeconds=SECONDS, chargedBytes=BYTES, declarationSha256=pin,
        fullAllowanceChargedAtStart=True, retryPermitted=False))
    try:
        shutil.copyfile(DECLARATION, OUTPUT / 'declaration.json')
        shutil.copytree(maximum.OUTPUT / 'host', OUTPUT / 'host')
        shutil.copyfile(maximum.OUTPUT / 'maximum-workload.json', OUTPUT / 'maximum-workload.json')
        shutil.copyfile(FAILED / 'files.json', OUTPUT / 'failed-pair-files.json')
        require(sha(FAILED_NATIVE) == declaration['failedNativeReceiptSha256'], 'Changed failed denominator receipt')
        shutil.copyfile(FAILED_NATIVE, OUTPUT / 'failed-native-receipt.json')
        (OUTPUT / 'inputs').mkdir()
        for key, binding in declaration['costInputs'].items():
            source = Path(binding['path'])
            require(sha(source) == binding['sha256'], 'Changed historical cost input: ' + key)
            shutil.copyfile(source, OUTPUT / 'inputs' / (key + '.json'))
        for path in declaration['sourcePins']:
            target = OUTPUT / 'implementation' / path; target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / path, target)
        (OUTPUT / 'source-fixture').mkdir()
        for name, expected in declaration['physicalPins'].items():
            source = Path(declaration['physicalSource']) / 'source' / (name + '.json')
            require(sha(source) == expected, 'Changed frozen physical context')
            shutil.copyfile(source, OUTPUT / 'source-fixture' / ('fixture-' + name + '.json'))
        last = 0
        def check():
            nonlocal last
            if time.monotonic()-last >= 1:
                last = time.monotonic()
                require(sum(p.stat().st_size for p in OUTPUT.rglob('*') if p.is_file()) < BYTES - 4*1048576, 'Pair storage exceeded')
        receipt = bounded_windows_process.run([sys.executable, '-B', '-X', 'utf8', str(Path(__file__)), 'worker'],
            str(ROOT), str(OUTPUT / 'worker.log'), started + SECONDS - 10, check=check, cleanup_seconds=1)
        write(OUTPUT / 'worker-process.json', receipt)
        require(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0,
            'Qualified pair failed; preserve both arms without retry')
    except Exception as error:
        failure = str(error)
        write(OUTPUT / 'failure.json', dict(reason=failure, retryPermitted=False, scientificAdmitted=False))
    write(OUTPUT / 'completion.json', dict(status='Failed' if failure else 'CompletedMatchedPair',
        secondsBeforeSealing=time.monotonic()-started, recordedTimingIsLowerBound=True,
        bytesBeforeSealing=sum(p.stat().st_size for p in OUTPUT.rglob('*') if p.is_file()),
        chargedSeconds=SECONDS, chargedBytes=BYTES, actualCombat=0, productionEntropyDraws=0, scientificAdmitted=False))
    write(OUTPUT / 'files.json', maximum.inventory(OUTPUT))
    print(json.dumps(dict(output=str(OUTPUT), manifestSha256=sha(OUTPUT / 'files.json'), failure=failure), indent=2))
    if failure:
        raise ValueError(failure)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['register', 'run', 'worker']); parser.add_argument('--pin')
    args = parser.parse_args()
    if args.action == 'register': print(register())
    elif args.action == 'run': run(args.pin)
    else: worker()
