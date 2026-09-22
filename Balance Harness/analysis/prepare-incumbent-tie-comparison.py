"""Prepare/verify the captured incumbent-tie launch package; never allocate or fight.

The only harness command invoked is tower-incumbent-tie-comparison-check. A separate
context helper resolves methods and authenticates source symbols without executing
search, reservation or combat. The engineering allowance is outside the experiment.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
CAPTURE = ROOT/'TestResults/anchored-comparison-20260917'
VERIFICATION = ROOT/'TestResults/incumbent-tie-comparison-verification-20260922'
BUILD = ROOT/'TestResults/incumbent-tie-comparison-build-20260922/bin/BalanceHarness/release'
PACKAGE = ROOT/'TestResults/incumbent-tie-comparison-admission-20260922-02'
RUN = ROOT/'TestResults/balance/tower-incumbent-tie-comparison-20260922'
CAPTURE_HASH = '1a2da312f1cb7af7867f0d173c9fcdf8f2213afc36873240ed2b0da505eec314'
VERIFICATION_HASH = '4148aca044646514dc8cbcea04c2518e3f47f1a12881154700bb75b198df498e'
TEMPLATE_HASH = 'cc0ae4e8ccacb09ad169386c3f1c25fc1de6c93f9485dd20fe59438f6122a31a'
VERSION = 'tower-incumbent-tie-comparison-v1'
PREPARATION_SECONDS, PREPARATION_BYTES = 600, 512*1048576
HARNESS_FILES = ('BalanceHarness.dll', 'BalanceHarness.pdb', 'BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json')
LEDGERS = {'history-input.json', 'seed-ledger.json', 'prior-seed-ledger.json'}


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def save(path, value):
    with path.open('xb') as stream:
        stream.write((json.dumps(value, indent=2, allow_nan=False)+'\n').encode())
        stream.flush(); os.fsync(stream.fileno())


def paths(root, only_names=None):
    require(not root.is_symlink() and not root.is_junction(), 'Linked root')
    require(root.is_dir(), 'Missing input directory')
    result, pending, directories = [], [root], 0
    while pending:
        parent = pending.pop(); directories += 1
        require(directories <= 2000000, 'Directory scan limit exceeded')
        with os.scandir(parent) as entries:
            for entry in entries:
                require(not (getattr(entry.stat(follow_symlinks=False), 'st_file_attributes', 0) & 0x400)
                        and not entry.is_symlink(), 'Linked artifact: '+entry.path)
                if entry.is_dir(follow_symlinks=False):
                    pending.append(entry.path)
                elif only_names is None or entry.name in only_names:
                    result.append(Path(entry.path))
    return sorted(result)


def inventory(root):
    return {p.relative_to(root).as_posix(): sha(p) for p in paths(root)}


def authenticate(root, pin):
    require(sha(root/'files.json') == pin, 'Changed manifest pin: '+str(root))
    manifest = read(root/'files.json')
    require({p.relative_to(root).as_posix() for p in paths(root)} == set(manifest)|{'files.json'}, 'Changed package membership')
    for name, digest in manifest.items():
        target = (root/name).resolve()
        require(target.is_relative_to(root.resolve()) and sha(target) == digest, 'Changed package file: '+name)
    return manifest


def ledger(value):
    found = set()
    if isinstance(value, list):
        for child in value:
            if type(child) in (int, float):
                require(type(child) is int and -(2**31) <= child < 2**31, 'Invalid historical value')
                found.add(child)
            else:
                found.update(ledger(child))
    elif isinstance(value, dict):
        require(value.get('reservationState', 'Complete') == 'Complete', 'Unresolved historical reservation')
        for child in value.values():
            found.update(ledger(child))
    return found


def history(registry, old):
    # Independent full scan. The public native check subsequently reconstructs this
    # union and validates the recorded Pending recovery, including its source journal.
    files, values, seen, recovered = {}, set(), set(), set()
    recoveries = old['pendingHistoryRecoveries']
    for file in paths(registry, LEDGERS):
        digest = sha(file); key = str(file)
        files[key] = digest
        if key in recoveries:
            receipt_path = Path(recoveries[key]); receipt = read(receipt_path)
            require(sha(receipt_path) == old['recoveryReceiptHashes'][str(receipt_path)]
                    and receipt['version'] == 'tower-refinement-abandoned-reservation-v1'
                    and receipt['status'] == 'AbandonedPermanentlyReserved'
                    and Path(receipt['studyRoot']) == file.parent, 'Changed or unsupported Pending recovery')
            values.update(ledger(receipt['reserved'])); recovered.add(key)
        elif digest not in seen:
            values.update(ledger(read(file))); seen.add(digest)
        require(sha(file) == digest, 'History changed while reading')
    require(recovered == set(recoveries), 'Missing Pending recovery source')
    require(all(files.get(k) == v for k, v in old['requiredHistory'].items()), 'Lost historical source pin')
    require(0 < len(values) <= 967232, 'Historical capacity exceeded')
    return files, sorted(values)


def derive_template(capture, values, context):
    require(context['settingsHash'] == capture['settingsHash'], 'Changed effective settings')
    d = copy.deepcopy(capture)
    d.update(id='incumbent-tie-template', excludedCombatSeeds=values, executionHash=context['executionHash'], maximumBattles=3496)
    d.pop('primaryReferenceId', None)
    d['generation'].update(policyVersion='retained-composition-incumbents-v1', seeds=[])
    require(d['generation']['candidatesPerArm'] == 46 and d['generation']['maximumAttemptsPerArm'] == 256
            and d['stages']['selectionPolicyVersion'] == 'tower-staged-zero-win-health-v1'
            and not d['stages'].get('selectionPrimaryReferenceId')
            and all(s[k] == [] for s in d['stages']['schedules'].values() for k in ('discovery', 'selection', 'confirmation', 'diagnostics'))
            and all(r['scenario']['seeds'] == [] for r in d['references']), 'Captured template is not the frozen unscheduled scope')
    return d


def owned(module_path, command, log, deadline, check):
    spec = importlib.util.spec_from_file_location('tie_preparation_owner', module_path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    result = module.run(command, str(ROOT), str(log), deadline, cleanup_seconds=1, check=check)
    require(result['exitCode'] == 0 and not result['timedOut'] and result['activeProcesses'] == 0, 'Preparation process failed; inspect '+str(log))
    return result


def verify(package, manifest_hash, live=True):
    manifest = authenticate(package, manifest_hash)
    q, d, context = read(package/'request.json'), read(package/'template.json'), read(package/'context.json')
    preparation = read(package/'preparation.json')
    require(not RUN.exists() and not (package/'failure.json').exists(), 'Scientific output or failed preparation exists')
    require(preparation['status'] == 'ReadyNoReservation' and preparation['scientificLaunches'] == preparation['newValues'] == preparation['fights'] == 0, 'Changed preparation scope')
    require(sha(package/'capture/files.json') == CAPTURE_HASH and sha(package/'capture/template.json') == TEMPLATE_HASH, 'Changed capture')
    require(sha(package/'verification/files.json') == VERIFICATION_HASH, 'Changed preceding verification')
    prior = read(package/'verification/verification.json')
    require(sha(package/'runtime/BalanceHarness.dll') == prior['sourceRuntimeHash'], 'Harness differs from tested producing binary')
    runtime = inventory(package/'runtime'); captured = read(package/'capture/files.json')
    expected_names = {name[8:] for name in captured if name.startswith('runtime/')}
    require(set(runtime) == expected_names, 'Changed runtime membership')
    for name, digest in runtime.items():
        if name not in HARNESS_FILES:
            require(captured['runtime/'+name] == digest, 'Changed captured gameplay dependency')
    require(runtime == read(package/'runtime-files.json'), 'Changed runtime inventory')
    for name, digest in read(package/'compiled-source-files.json').items():
        require(sha(package/'source'/name) == digest, 'Changed producing source copy')
    require(len(read(package/'compiled-source-files.json')) == context['compiledSourceDocuments']
            and len(context['jitResolvedMethods']) > 0 and context['nativePreparations'] == context['fights'] == context['newValues'] == 0, 'Missing compatibility evidence')
    require(q['version'] == VERSION and q['captureRoot'] == str(package/'capture') and q['contentRoot'] == str(package/'content')
            and q['templatePath'] == str(package/'template.json') and q['templateHash'] == sha(package/'template.json')
            and q['outputRoot'] == str(RUN) and q['registryRoot'] == str(RUN.parent)
            and q['maximumSeconds'] == 4500 and q['maximumBytes'] == 4294967296, 'Changed concrete request')
    require(d == derive_template(read(package/'capture/template.json'), d['excludedCombatSeeds'], context), 'Changed template binding')
    require(inventory(package/'content') == {name[8:]: digest for name, digest in captured.items() if name.startswith('content/')}, 'Changed captured content/settings')
    native = read(package/'native-check.json')
    require(native == dict(version=VERSION, status='ReadyNoReservation', historicalValues=len(d['excludedCombatSeeds']),
                           newValues=0, fights=0, restarts=24, searchFights=11904, maximumFights=59904), 'Native admission failed')
    for name in ('context-process.json', 'native-check-process.json'):
        process = read(package/name)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0
                and process['mechanism'] == 'suspended-owned-job-v1', 'Incomplete preparation process tree')
    require(not any(Path(name).name in LEDGERS or Path(name).name == 'entropy.bin' for name in manifest), 'Premature reservation artifacts')
    if live:
        current, values = history(RUN.parent, read(package/'capture/request.json'))
        require(current == q['requiredHistory'] == read(package/'history-files.json') and values == d['excludedCombatSeeds'], 'Live history changed after preparation')
        require(q['pendingHistoryRecoveries'] == read(package/'capture/request.json')['pendingHistoryRecoveries']
                and q['recoveryReceiptHashes'] == read(package/'capture/request.json')['recoveryReceiptHashes'], 'Changed recovery mapping')
    return dict(status='PreparedPackageVerifiedNoReservation', manifestSha256=manifest_hash, manifestFiles=len(manifest),
                historicalValues=len(d['excludedCombatSeeds']), historyFiles=len(q['requiredHistory']),
                sourceDocuments=context['compiledSourceDocuments'], jitResolvedMethods=len(context['jitResolvedMethods']),
                requestSha256=sha(package/'request.json'), executionHash=context['executionHash'],
                scientificLaunches=0, newValues=0, fights=0, outputExists=False)


def prepare():
    require(not PACKAGE.exists() and not RUN.exists(), 'Preparation requires new package and absent scientific output')
    started = time.monotonic(); PACKAGE.mkdir()
    def check():
        require(time.monotonic()-started < PREPARATION_SECONDS-2, 'Preparation deadline reached')
        require(sum(p.stat().st_size for p in paths(PACKAGE)) < PREPARATION_BYTES, 'Preparation storage ceiling reached')
        require(not RUN.exists(), 'Unexpected scientific output during preparation')
    try:
        captured = authenticate(CAPTURE, CAPTURE_HASH); authenticate(VERIFICATION, VERIFICATION_HASH)
        evidence = read(VERIFICATION/'verification.json')
        require(sha(BUILD/'BalanceHarness.dll') == evidence['sourceRuntimeHash'], 'Tested harness changed')
        for name, digest in read(VERIFICATION/'source-files.json').items():
            require(sha(ROOT/name) == digest, 'Changed tested source: '+name)
        shutil.copytree(CAPTURE/'runtime', PACKAGE/'runtime')
        shutil.copytree(CAPTURE/'content', PACKAGE/'content')
        for name in HARNESS_FILES:
            shutil.copyfile(BUILD/name, PACKAGE/'runtime'/name)
        (PACKAGE/'capture').mkdir(); (PACKAGE/'verification').mkdir()
        for name in ('files.json', 'template.json', 'request.json'):
            shutil.copyfile(CAPTURE/name, PACKAGE/'capture'/name)
        for name in ('files.json', 'verification.json', 'source-files.json', 'tests-final.trx', 'tests-regressions.trx'):
            shutil.copyfile(VERIFICATION/name, PACKAGE/'verification'/name)
        for source, target in [(Path(__file__), 'prepare.py'), (Path(__file__).with_name('incumbent-tie-preparation-context.ps1'), 'context.ps1'),
            (ROOT/'build/bounded_windows_process.py', 'bounded_windows_process.py'), (ROOT/'build/run-incumbent-tie-comparison.py', 'launcher.py'),
            (ROOT/'Balance Harness/analysis/audit-incumbent-tie-comparison.py', 'independent-audit.py'),
            (ROOT/'Balance Harness/Tower-Practical-Incumbent-Tie-Comparison-Plan.json', 'plan.json'),
            (ROOT/'Balance Harness/Tower-Practical-Incumbent-Tie-Comparison-Plan.md', 'plan.md')]:
            shutil.copyfile(source, PACKAGE/target)
        check()
        process = owned(PACKAGE/'bounded_windows_process.py', ['pwsh', '-NoProfile', '-File', str(PACKAGE/'context.ps1'),
            '-Package', str(PACKAGE), '-RepositoryRoot', str(ROOT)], PACKAGE/'context.log', started+PREPARATION_SECONDS-2, check)
        save(PACKAGE/'context-process.json', process)
        context = read(PACKAGE/'context.json')
        for name, digest in context['execution']['assemblyHashes'].items():
            if name != 'BalanceHarness': require(captured['runtime/'+name+'.dll'] == digest, 'Changed gameplay assembly')
        save(PACKAGE/'runtime-files.json', inventory(PACKAGE/'runtime'))
        old = read(CAPTURE/'request.json'); files, values = history(RUN.parent, old)
        save(PACKAGE/'history-files.json', files)
        d = derive_template(read(CAPTURE/'template.json'), values, context); save(PACKAGE/'template.json', d)
        q = dict(version=VERSION, captureRoot=str(PACKAGE/'capture'), contentRoot=str(PACKAGE/'content'), templatePath=str(PACKAGE/'template.json'),
                 templateHash=sha(PACKAGE/'template.json'), registryRoot=str(RUN.parent), outputRoot=str(RUN), requiredHistory=files,
                 pendingHistoryRecoveries=old['pendingHistoryRecoveries'], recoveryReceiptHashes=old['recoveryReceiptHashes'],
                 maximumSeconds=4500, maximumBytes=4294967296)
        save(PACKAGE/'request.json', q)
        check()
        process = owned(PACKAGE/'bounded_windows_process.py', ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'),
            'tower-incumbent-tie-comparison-check', str(PACKAGE/'request.json')], PACKAGE/'native-check.log', started+PREPARATION_SECONDS-2, check)
        save(PACKAGE/'native-check-process.json', process)
        save(PACKAGE/'native-check.json', read(PACKAGE/'native-check.log'))
        current, current_values = history(RUN.parent, old)
        require(current == files and current_values == values, 'History changed during preparation')
        save(PACKAGE/'preparation.json', dict(version=VERSION, status='ReadyNoReservation', seconds=time.monotonic()-started,
             engineeringMaximumSeconds=PREPARATION_SECONDS, engineeringMaximumBytes=PREPARATION_BYTES,
             scientificEnvelopeSeconds=4500, scientificEnvelopeBytes=4294967296,
             nativePreparations='Public admission validates fixed recipes with local labels only; no reservations',
             sourceCaptureManifest=CAPTURE_HASH, sourceVerificationManifest=VERIFICATION_HASH,
             scientificLaunches=0, newValues=0, fights=0))
        check(); save(PACKAGE/'files.json', inventory(PACKAGE))
        result = verify(PACKAGE, sha(PACKAGE/'files.json'))
        check(); print(json.dumps(result, indent=2))
    except BaseException as error:
        save(PACKAGE/'failure.json', dict(status='PreparationFailedNoLaunch', reason=str(error), scientificLaunches=0))
        raise


class PreparationTests(unittest.TestCase):
    def test_ledger_union_excludes_scalar_metadata(self):
        self.assertEqual({-1, 2, 3}, ledger(dict(reservationState='Complete', reserved=[-1, 2], prior={'values':[2, 3]}, scalar=99)))
    def test_pending_and_noninteger_capacity_are_not_admitted(self):
        for value in ({'reservationState':'Pending','reserved':[1]}, [2**31], [-2**31-1], [1.5]):
            with self.assertRaises(ValueError): ledger(value)
    def test_template_changes_only_declared_fields(self):
        d = read(CAPTURE/'template.json'); original=copy.deepcopy(d)
        result = derive_template(d, [1, 2], dict(settingsHash=d['settingsHash'], executionHash='a'*64))
        self.assertEqual(original, d); self.assertEqual([1, 2], result['excludedCombatSeeds'])
        self.assertEqual(d['starts'], result['starts']); self.assertEqual(d['references'], result['references'])
        self.assertNotIn('primaryReferenceId', result)
        self.assertEqual('retained-composition-incumbents-v1', result['generation']['policyVersion'])
    def test_changed_settings_rejected(self):
        with self.assertRaises(ValueError): derive_template(read(CAPTURE/'template.json'), [], dict(settingsHash='changed', executionHash='a'*64))
    def test_premature_panel_rejected(self):
        d = read(CAPTURE/'template.json'); next(iter(d['stages']['schedules'].values()))['selection']=[17]
        with self.assertRaises(ValueError): derive_template(d, [], dict(settingsHash=d['settingsHash'], executionHash='a'*64))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare', 'verify', 'self-test'])
    parser.add_argument('--manifest-sha256')
    args=parser.parse_args()
    if args.command == 'prepare': prepare()
    elif args.command == 'verify':
        require(args.manifest_sha256 is not None, 'Supply the previously recorded package manifest pin')
        print(json.dumps(verify(PACKAGE, args.manifest_sha256), indent=2))
    else:
        result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(PreparationTests))
        sys.exit(not result.wasSuccessful())
