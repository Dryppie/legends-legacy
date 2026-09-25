"""Single nomination cycle: reuse qualified combat bytes and existing owned study.

Engineering fixtures use literal outcomes in an isolated registry. Scientific
execution is a separate, pinned action, with no retry or entropy refill path.
"""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'build'))
import bounded_windows_process


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


maximum = module('nomination_maximum_helpers', ROOT / 'build/test-loadout-placement-plain-maximum.py')
sha, read, write, require, inventory = maximum.sha, maximum.read, maximum.write, maximum.require, maximum.inventory
owner = module('nomination_owner', ROOT / 'build/run-proposal-affinity-study.py')
PRIOR = maximum.QUALIFIED
QUALIFIED = ROOT / 'TestResults/affinity-nomination-runtime-20260925'
BUILD = ROOT / '.artifacts/loadout-plain-maximum-20260925/bin/BalanceHarness/release'
FAILED_FIXTURE = ROOT / 'TestResults/affinity-nomination-maximum-20260925'
FIXTURE = ROOT / 'TestResults/affinity-nomination-maximum-storage-root-20260925'
RECOVERED = ROOT / 'TestResults/affinity-nomination-maximum-verification-20260925'
HOST = ROOT / '.artifacts/affinity-nomination-fixture-20260925/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll'
DESIGN = ROOT / 'Balance Harness/Tower-Affinity-Search-Consolidation.md'


def checked_process(root, name, command, deadline, check=lambda: None):
    result = bounded_windows_process.run(command, str(ROOT), str(root / (name + '.log')), deadline,
        check=check, cleanup_seconds=1, log_byte_limit=1048576)
    write(root / (name + '-process.json'), result)
    require(result['exitCode'] == 0 and not result['timedOut'] and result['activeProcesses'] == 0, 'Process failed: ' + name)
    return result


def powershell(root, helper, *args):
    host = Path(shutil.which('pwsh')).with_suffix('.dll')
    return ['dotnet', 'exec', '--runtimeconfig', str(root / 'runtime/BalanceHarness.runtimeconfig.json'),
        '--depsfile', str(host.with_suffix('.deps.json')), str(host), '-NoProfile', '-File', str(helper),
        '-Package', str(root), *args]


def qualify():
    require(not QUALIFIED.exists(), 'Qualification already attempted')
    require(sha(PRIOR / 'files.json') == maximum.QUALIFIED_PIN, 'Changed captured qualification')
    previous = read(PRIOR / 'files.json')
    started = time.monotonic(); QUALIFIED.mkdir()
    write(QUALIFIED / 'declaration.json', dict(designSha256=sha(DESIGN), capturedManifestSha256=maximum.QUALIFIED_PIN,
        harnessFiles={n: sha(BUILD / n) for n in ('BalanceHarness.dll', 'BalanceHarness.pdb')},
        attempts=1, chargedSeconds=900, chargedBytes=1073741824, actualCombat=0, productionEntropyDraws=0,
        scope='Same captured gameplay dependencies; compare all 2098 input/participant preparations and deterministic legacy proposal replays.'))
    def check():
        require(time.monotonic()-started < 895 and owner.storage_bytes(QUALIFIED) < 1073741824-4194304, 'Qualification budget exhausted')
    try:
        names = [n for n in previous if n.startswith(('runtime/', 'content/', 'allied-preview/', 'preview-', 'preserving-'))
            or n in ('settings.json', 'probe-scenarios.json', 'runtime-context.json')]
        for name in names:
            require(sha(PRIOR / name) == previous[name], 'Changed captured input: ' + name)
            target = QUALIFIED / ('old-context.json' if name == 'runtime-context.json' else name)
            target.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(PRIOR / name, target)
        for name in ('BalanceHarness.dll', 'BalanceHarness.pdb'): shutil.copyfile(BUILD / name, QUALIFIED / 'runtime' / name)
        runtime = inventory(QUALIFIED / 'runtime'); write(QUALIFIED / 'runtime.json', runtime)
        require({n:p for n,p in runtime.items() if n not in ('BalanceHarness.dll', 'BalanceHarness.pdb')} ==
            {n:p for n,p in read(PRIOR / 'runtime.json').items() if n not in ('BalanceHarness.dll', 'BalanceHarness.pdb')}, 'Changed combat dependencies')
        helper = Path(__file__).with_name('affinity-allied-action-admission-context.ps1')
        shutil.copyfile(helper, QUALIFIED / 'context.ps1')
        checked_process(QUALIFIED, 'candidate', powershell(QUALIFIED, QUALIFIED / 'context.ps1',
            '-RepositoryRoot', str(ROOT), '-Mode', 'candidate'), started+895, check)
        require(sha(PRIOR / 'candidate-projections.json') == previous['candidate-projections.json'], 'Changed saved preparations')
        require(read(QUALIFIED / 'candidate-projections.json') == read(PRIOR / 'candidate-projections.json'), 'Physical preparation changed')
        details = read(QUALIFIED / 'candidate-qualification.json')
        require(details['materializations'] == details['nativePreparations'] == 2098 and details['replayedArms'] == 4
            and details['preservingReplayedArms'] == 2 and details['alliedReplayedRoots'] == 12, 'Incomplete qualification')
        context = read(QUALIFIED / 'old-context.json'); context['scope']['executionHash'] = details['executionHash']
        write(QUALIFIED / 'runtime-context.json', context)
        request = read(QUALIFIED / 'preview-request.json'); request['context'] = context
        write(QUALIFIED / 'nomination-request.json', request)
        checked_process(QUALIFIED, 'plan', ['dotnet', str(QUALIFIED / 'runtime/BalanceHarness.dll'),
            'tower-affinity-nomination-comparison-plan', str(QUALIFIED / 'nomination-request.json'),
            str(QUALIFIED / 'runtime-plan.json')], started+895, check)
        checked_process(QUALIFIED, 'retention', powershell(QUALIFIED, QUALIFIED / 'context.ps1',
            '-RepositoryRoot', str(ROOT), '-Mode', 'retention'), started+895, check)
        require(inventory(QUALIFIED / 'retention-check/executable') == runtime, 'Runtime retention changed bytes')
        require(inventory(QUALIFIED / 'source') == read(QUALIFIED / 'compiled-source-files.json'), 'Producing source changed')
        check(); write(QUALIFIED / 'completion.json', dict(status='RuntimeEquivalentNoReservation', seconds=time.monotonic()-started,
            preparations=2098, fights=0, newValues=0, executionHash=details['executionHash']))
    except BaseException as error:
        write(QUALIFIED / 'failure.json', dict(reason=str(error), fights=0, newValues=0)); raise
    finally:
        write(QUALIFIED / 'files.json', inventory(QUALIFIED))
    print(json.dumps(dict(status='RuntimeEquivalentNoReservation', manifestSha256=sha(QUALIFIED / 'files.json'))))


def fixture_worker():
    fixture = module('nomination_full_fixture', ROOT / 'build/test-proposal-affinity-study-owned.py')
    declaration = read(FIXTURE / 'declaration.json')
    args = SimpleNamespace(fixture_host=HOST, source_fixture=QUALIFIED,
        output=FIXTURE / 'tower-proposal-owned-fixture-nomination', mode='complete', profile='plain-maximum-nomination-v1',
        resource_envelope=owner.RESOURCE_V2)
    summary = fixture.execute(args, fixture.prepare(args))
    require(summary['literalReports'] == 21888, 'Incomplete maximum workload')
    result = args.output / 'result'
    complete, closeout = read(result / 'completion.json'), read(result / 'closeout.json')
    require(closeout['requestHash'] == complete['requestFileHash'], 'Changed workload binding')
    costs = {key: (closeout if key.startswith('audit') else complete)[key]
        for key in ('nativeSeconds', 'auditSeconds', 'nativeBytes', 'auditBytes')}
    write(FIXTURE / 'maximum-workload.json', dict(status='NominationMaximumVerified', literalReports=21888,
        phaseCosts=costs, finalVerificationSeconds=summary['verificationProcess']['seconds'],
        actualCombat=0, productionEntropyDraws=0, scientificReservations=0))


def fixture():
    require(not FIXTURE.exists() and (QUALIFIED / 'completion.json').exists(), 'Missing qualification or repeated fixture')
    runtime = read(QUALIFIED / 'runtime.json')
    for name, pin in runtime.items(): require(sha(HOST.parent / name) == pin, 'Fixture runtime mismatch: ' + name)
    FIXTURE.mkdir(); started=time.monotonic()
    write(FIXTURE / 'declaration.json', dict(designSha256=sha(DESIGN), qualificationManifestSha256=sha(QUALIFIED / 'files.json'),
        precedingFailedFixtureManifestSha256=sha(FAILED_FIXTURE / 'files.json'),
        retainedFailedCharge=dict(seconds=12300, bytes=6576668672),
        correction='The outer fixture monitor now delegates result storage to the production counter with its required result-relative root. Outcomes and scientific design are unchanged. No scientific seeds were used.',
        hostSha256=sha(HOST), sourcePins={str(p.relative_to(ROOT)):sha(p) for p in (Path(__file__),
            ROOT / 'build/test-proposal-affinity-study-owned.py', ROOT / 'build/run-proposal-affinity-study.py',
            ROOT / 'Balance Harness/analysis/audit-proposal-affinity-study.py')},
        attempts=1, chargedSeconds=12300, chargedBytes=6576668672, actualCombat=0, productionEntropyDraws=0,
        outcome='Generated training wins; old references win nomination; nonbenchmark first five validation wins; all heldout wins.',
        forecast='Retain all earlier qualified placement phase floors; scale new absolute maximum costs upward for metadata growth, with factor two and publication reserve. No discount or new matched pair.'))
    def check(): require(fixture_storage(FIXTURE) < 6576668672-4194304, 'Fixture storage exhausted')
    try:
        checked_process(FIXTURE, 'worker', [sys.executable, '-B', '-X', 'utf8', str(Path(__file__)), 'fixture-worker'],
            started+12290, check)
        write(FIXTURE / 'completion.json', dict(status='NominationMaximumVerified', seconds=time.monotonic()-started))
    except BaseException as error:
        write(FIXTURE / 'failure.json', dict(reason=str(error))); raise
    finally: write(FIXTURE / 'files.json', inventory(FIXTURE))
    print(json.dumps(dict(status='NominationMaximumVerified', manifestSha256=sha(FIXTURE / 'files.json'))))


def fixture_storage(root):
    # storage_bytes recognizes the native zero-byte lease relative to the study
    # result. Support files outside that live tree do not use native leases.
    result = root / 'tower-proposal-owned-fixture-nomination/result'
    leases = {result.parent / 'complete-family-allocation.writer.lock', result.with_name('result.writer.lock')}
    support = 0
    for p in owner.inventory(root):
        if result in p.parents: continue
        try: size = p.stat().st_size
        except FileNotFoundError:
            if p in leases: continue
            raise
        require(p not in leases or size == 0, 'Fixture owner lease must be empty')
        support += size
    return support + (owner.storage_bytes(result) if result.exists() else 0)


def verify_completed_fixture():
    require(not RECOVERED.exists(), 'Completed-fixture verification already attempted')
    result = FIXTURE / 'tower-proposal-owned-fixture-nomination/result'
    pin = sha(result / 'closeout.json')
    require(pin == (result.parent / 'closeout-pin.txt').read_text(encoding='utf-8').strip(), 'Changed published fixture')
    require(read(result / 'result.json')['status'] == 'Verified', 'Fixture did not publish')
    RECOVERED.mkdir(); started = time.monotonic()
    write(RECOVERED / 'declaration.json', dict(sourceManifestSha256=sha(FIXTURE / 'files.json'), closeoutSha256=pin,
        source=str(result), attempts=1, chargedSeconds=450, chargedBytes=67108864,
        retainedFixtureCharges=dict(seconds=24600,bytes=13153337344),
        sourcePins={str(p.relative_to(ROOT)):sha(p) for p in (Path(__file__), ROOT / 'build/test-affinity-nomination.py')},
        scope='Read-only final verification of already published synthetic reports; no new reports, entropy, or scientific reservation. Use the full 450-second verifier ceiling in the current workload forecast, not a faster measured sample.'))
    try:
        receipt = checked_process(RECOVERED, 'verification', ['dotnet', str(QUALIFIED / 'runtime/BalanceHarness.dll'),
            'tower-proposal-study-verify', str(result), pin], started+445)
        verified = read(RECOVERED / 'verification.log')
        require(verified == read(result / 'result.json') and verified['fights'] == 21888 and verified['heldoutFights'] == 9216
            and verified['differingRoots'] == 12 and verified['validation']['passedRoots'] == verified['controlValidation']['passedRoots'] == 12
            and all(r['changedPositionsPerWave'] == [0,0] for r in verified['roots']), 'Incomplete nomination maximum verification')
        complete, closeout = read(result / 'completion.json'), read(result / 'closeout.json')
        require(closeout['requestHash'] == complete['requestFileHash'], 'Changed cost binding')
        costs = {k:(closeout if k.startswith('audit') else complete)[k] for k in ('nativeSeconds','auditSeconds','nativeBytes','auditBytes')}
        write(RECOVERED / 'maximum-workload.json', dict(status='NominationMaximumVerified', phaseCosts=costs,
            finalVerificationSeconds=450, observedFinalVerificationSeconds=receipt['seconds'], literalReports=21888,
            sourceCloseoutSha256=pin, finalVerificationUsesDeclaredCeiling=True, actualCombat=0, productionEntropyDraws=0))
        write(RECOVERED / 'completion.json', dict(status='NominationMaximumVerified', seconds=time.monotonic()-started,
            closeoutSha256=pin, sourceManifestSha256=sha(FIXTURE / 'files.json')))
    except BaseException as error:
        write(RECOVERED / 'failure.json', dict(reason=str(error))); raise
    finally: write(RECOVERED / 'files.json', inventory(RECOVERED))
    print(json.dumps(dict(status='NominationMaximumVerified', manifestSha256=sha(RECOVERED / 'files.json'))))


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['qualify', 'fixture', 'fixture-worker', 'verify-completed-fixture'])
    args=parser.parse_args()
    {'qualify': qualify, 'fixture': fixture, 'fixture-worker': fixture_worker,
        'verify-completed-fixture': verify_completed_fixture}[args.action]()
