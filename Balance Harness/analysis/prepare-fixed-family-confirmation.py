"""Capture and check the fixed family without entropy, combat or a launch command.

The native compatibility request carries an authenticated *known-charge subtotal*.
It is not a final admission: unresolved engineering accounting is retained explicitly.
Never use that request to run an experiment. A future final request must reconcile
those entries and bind a separately verified enclosing launcher.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
PREVIOUS = ROOT/'TestResults/incumbent-tie-practical-admission-20260922'
PREVIOUS_PIN = '3f580b3216d1da086e4bdc080c6a883e3f1bceaec4028500ed78ccaa585cf034'
VERIFICATION = ROOT/'TestResults/fixed-family-implementation-verification-20260922'
VERIFICATION_PIN = '6d0064127e65ad33c90c902106a0fb234948e8cceed1d1728040532640dfc5bd'
PLAN = ROOT/'Balance Harness/Tower-Practical-Fixed-Family-Confirmation-Plan.json'
PLAN_PIN = 'cde1dfc0cad48ff366754938987c0bdb206c0d7116f0ec71d91d0f15f5a34275'
BUILD = ROOT/'TestResults/fixed-family-implementation-build-20260922/bin/BalanceHarness/release'
PACKAGE = ROOT/'TestResults/fixed-family-runtime-admission-20260922'
OUTPUT = ROOT/'TestResults/balance/tower-fixed-family-confirmation-20260922'
VERSION = 'tower-practical-fixed-family-confirmation-v1'
SECONDS, BYTES = 900, 512*1048576
MIB = 1048576
STATUS = 'RuntimeAdmissionPassedAccountingUnresolved'
HISTORY_PIN = '2cd75f68497d6960170dccadd883e2f7dce0842893d8975023d19fe333f657cf'
HISTORY_PATH = ROOT/'TestResults/incumbent-tie-practical-closeout-20260922/live-history-files.json'


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def common_module():
    local = Path(__file__).with_name('comparison-preparation.py')
    path = local if local.exists() else PREVIOUS/'comparison-preparation.py'
    require(sha(PREVIOUS/'files.json') == PREVIOUS_PIN, 'Changed previous manifest')
    require(sha(path) == read(PREVIOUS/'files.json')['comparison-preparation.py'], 'Changed history/ownership helper')
    spec = importlib.util.spec_from_file_location('family_preparation_common', path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def canonical_hash(value):
    text = json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True, allow_nan=False)
    for char in "+<>&'":
        text = text.replace(char, '\\u%04X' % ord(char))
    return hashlib.sha256(text.encode()).hexdigest()


def definition(plan, context, history):
    require(plan['proposedNativeVersion'] == VERSION and plan['settingsHash'] == context['settingsHash'], 'Changed contract/settings')
    teams = [dict(role=t['role'], partyId=t['partyId'], referenceIds=[t['referenceId']] if 'referenceId' in t else [],
                  scenario=copy.deepcopy(t['scenario'])) for t in plan['recipesInFixedExecutionOrder']]
    require(canonical_hash(teams) == '6cec8956d42a175044731021d9395ade5eca8275986892bfa94a248b54cc5d8d', 'Changed exact family')
    require(history and history == sorted(set(history)) and len(history) <= 989000, 'Invalid historical union')
    return dict(version=VERSION, teams=teams, contentHashes=plan['contentHashes'], settingsHash=plan['settingsHash'],
                executionHash=context['executionHash'], excludedCombatSeeds=history)


def totals(charges):
    require(charges and all(type(c['seconds']) is int and c['seconds'] > 0 and type(c['bytes']) is int and c['bytes'] > 0 for c in charges),
            'Only known positive exact charges belong in the subtotal')
    require(len({c['scope'] for c in charges}) == len(charges)
            and len({str(Path(c['receiptPath']).resolve()).casefold() for c in charges}) == len(charges), 'Duplicate charge')
    return sum(c['seconds'] for c in charges), sum(c['bytes'] for c in charges)


def final_caps(accounting):
    # Deliberately unavailable while the evidence cannot support a complete ledger.
    require(accounting['complete'] and not accounting['unresolved'], 'Incomplete accounting cannot yield final cumulative caps')
    seconds, size = totals(accounting['knownCharges'])
    return dict(seconds=seconds+6000, bytes=size+3072*MIB)


def reconcile(common):
    """Preserve receipts individually; use the old cumulative prefix exactly once."""
    receipt_root = PACKAGE/'charge-receipts'; receipt_root.mkdir()
    sources, charges = {}, []

    def retain(path, expected, label):
        require(sha(path) == expected, 'Changed charge evidence: '+str(path))
        target = receipt_root/label
        shutil.copyfile(path, target)
        sources[str(path)] = expected
        return target

    def member(directory, pin, name, label):
        root = ROOT/directory
        require(sha(root/'files.json') == pin, 'Changed accounting manifest: '+directory)
        sources[str(root/'files.json')] = pin
        return retain(root/name, read(root/'files.json')[name], label)

    def charge(scope, receipt, seconds, size, basis):
        value = dict(scope=scope, seconds=seconds, bytes=size, sourceReceipt=str(receipt), sourceHash=sha(receipt), basis=basis)
        target = receipt_root/(scope+'.json'); common.save(target, value)
        charges.append(dict(scope=scope, seconds=seconds, bytes=size, receiptPath=str(target), receiptHash=sha(target)))

    old = retain(ROOT/'Balance Harness/Tower-Practical-Fixed-Team-Confirmation-Execution.json',
                 'c7b1b387819810daac4a5149e54b9433643c896ef157a1fa9cc8e3975350f060', 'fixed-team-execution.json')
    d = read(old)['receipt']
    charge('fixed-team-cumulative-prefix', old, d['cumulativeChargedSeconds'], d['cumulativeChargedBytes'],
           'Cumulative operational prefix, including its 2400-second additional run. Do not add its preceding individual charges again. Separately disclosed unmetered engineering treatment was accepted in this historical receipt.')
    for directory, pin, scope in [
        ('TestResults/allocation-comparison-20260917', '59a3c65442310b8ab62d95ace9e9cb930a436d138d3e726d39908b6878c4bb2e', 'allocation-comparison'),
        ('TestResults/anchored-comparison-20260917', '1a2da312f1cb7af7867f0d173c9fcdf8f2213afc36873240ed2b0da505eec314', 'anchored-comparison')]:
        p = member(directory, pin, 'preparation.json', scope+'-source.json'); d = read(p)
        charge(scope, p, d['totalOwnedRunSeconds'], d['totalOwnedRunBytes'], 'Full enclosing operational allowance, not measured use; internal native allowance is nested, not additive.')
    p = member('TestResults/balance/tower-incumbent-tie-comparison-20260922',
               'c078df5d9f3f3074c763153e183c3544c462aefb50302867a5d854a3f9c4f909', 'request.json', 'selector-comparison-source.json')
    d = read(p); charge('selector-comparison', p, d['maximumSeconds'], d['maximumBytes'], 'Full enclosing allowance; 4440 seconds /3968 MiB native allowance is already included.')
    failed_script = retain(ROOT/'TestResults/incumbent-tie-comparison-admission-20260922/prepare.py',
                           'a3b2c52b3cb426d3fd5ca2ec6ea044ba2297d9ebacc1d755174afcdc1494d7d7', 'failed-preparation-source.py')
    failed = retain(ROOT/'TestResults/incumbent-tie-comparison-admission-20260922/failure.json',
                    '2e9aeeef4b09f8927bd56f0aedda59add0786c0592f1ec016a16f01b74b46156', 'failed-preparation-receipt.json')
    require('PREPARATION_SECONDS, PREPARATION_BYTES = 600, 512*1048576' in failed_script.read_text()
            and read(failed)['status'] == 'PreparationFailedNoLaunch', 'Changed failed preparation allowance')
    charge('selector-failed-preparation', failed, 600, 512*MIB, 'Full declared engineering allowance retained on failure; producing script is retained separately. No refund.')
    for directory, pin, scope in [
        ('TestResults/incumbent-tie-comparison-admission-20260922-02', 'ac6486e38aec867c5df5c1d2c7931edde13736d288de7d6a00dd87a408719776', 'selector-corrected-preparation'),
        ('TestResults/incumbent-tie-practical-admission-20260922', PREVIOUS_PIN, 'preset-preparation')]:
        p = member(directory, pin, 'preparation.json', scope+'-source.json'); d = read(p)
        charge(scope, p, d['engineeringMaximumSeconds'], d['engineeringMaximumBytes'], 'Full separate engineering preparation allowance; scientific run is a different entry.')
    p = retain(ROOT/'TestResults/incumbent-tie-practical-execution-20260922/authorization.json',
               'e7eeda08180b3c3a014312198ce4ec5a626091f846808f8e2081a4f20709282b', 'preset-execution-source.json')
    d = read(p); charge('preset-execution', p, d['maximumSeconds'], d['maximumBytes'], 'Full enclosing allowance retained despite post-combat wrapper failure; originalMaximum fields in the correction are not another charge.')
    p = member('TestResults/incumbent-tie-practical-closeout-20260922',
               '0bf81f630c839af5af22ee76cf22f5fafbdcb6838fb5a9cbf783f3816bdd4206', 'closeout.json', 'preset-correction-source.json')
    d = read(p); charge('preset-read-only-correction', p, d['engineeringMaximumSeconds'], d['engineeringMaximumBytes'], 'Separate read-only correction; no second scientific run.')
    charge('fixed-family-runtime-preparation', PACKAGE/'scope.json', SECONDS, BYTES, 'This create-only engineering preparation; charged in full before checking, even if preparation fails. Helper development/documentation is disclosed separately.')

    unresolved = []
    for name in ('native', 'independent'):
        p = member('TestResults/incumbent-tie-comparison-audit-20260922',
                   '1a7da49958115958c93fe67f8a999853a26ffff2a24bc6bbdd6ba12a015c28a9', name+'-process.json', 'selector-'+name+'-audit.json')
        d = read(p)
        unresolved.append(dict(scope='selector-'+name+'-audit', sourceReceipt=str(p), sourceHash=sha(p),
            workAllowanceSeconds=d['workAllowanceSeconds'], cleanupAllowanceSeconds=d['cleanupAllowanceSeconds'],
            measuredSeconds=d['seconds'], maximumBytes=None,
            reason='Process receipt declares 900 seconds work plus 1 second cleanup, but no storage ceiling. Cannot represent a complete two-dimensional charge by supplying zero or inventing a cap.'))
    unresolved.append(dict(scope='separately-disclosed-engineering', seconds=None, bytes=None,
        reason='Implementation/build/fixture/planning/documentation and some saved-data preparation/audit costs were separately disclosed, without a complete enclosing receipt. The older fixed-team execution accepted separate engineering treatment for that historical prefix; it does not establish numeric all-in totals for later work.',
        evidence=['Balance Harness/Tower-Practical-Search-Allocation-Comparison.md',
                  'Balance Harness/Tower-Practical-Anchored-Neighborhood-Comparison.md',
                  'Balance Harness/Tower-Practical-Incumbent-Tie-Comparison-Plan.md',
                  'TestResults/fixed-family-implementation-verification-20260922/summary.json']))
    seconds, size = totals(charges)
    result = dict(status='IncompleteNoFinalAdmission', complete=False, knownCharges=charges,
        knownChargedSeconds=seconds, knownChargedBytes=size, unresolved=unresolved,
        finalCumulativeSeconds=None, finalCumulativeBytes=None,
        next='Recover missing receipts if they exist, or explicitly settle the engineering accounting boundary and document it. Then create a new final request and enclosing launcher; this compatibility request cannot authorize execution.',
        sourcePins=sources)
    common.save(PACKAGE/'accounting.json', result)
    return result


def compatibility_request(accounting, history, old, definition_hash):
    seconds, size = totals(accounting['knownCharges'])
    # 240 seconds /128 MiB are held outside native work within the proposed overall
    # 6000 seconds /3072 MiB. These are conditional caps, not a final resource grant.
    return dict(version=VERSION, contentRoot=str(PACKAGE/'content'), definitionPath=str(PACKAGE/'definition.json'),
        definitionHash=definition_hash, registryRoot=str(OUTPUT.parent), outputRoot=str(OUTPUT), requiredHistory=history,
        maximumSeconds=seconds+5760, maximumBytes=size+2944*MIB,
        phases=dict(admission=dict(seconds=240, bytes=256*MIB), combat=dict(seconds=3600, bytes=2176*MIB),
                    audit=dict(seconds=1560, bytes=256*MIB)), priorSeconds=seconds, priorBytes=size,
        priorCharges=accounting['knownCharges'], pendingHistoryRecoveries=old['pendingHistoryRecoveries'],
        recoveryReceiptHashes=old['recoveryReceiptHashes'])


def verify(common, pin, live=True):
    manifest = common.authenticate(PACKAGE, pin)
    q = read(PACKAGE/'compatibility-request.json'); d = read(PACKAGE/'definition.json')
    context = read(PACKAGE/'context.json'); accounting = read(PACKAGE/'accounting.json')
    require(not OUTPUT.exists() and not (PACKAGE/'failure.json').exists(), 'Unexpected scientific output or failed preparation')
    require(read(PACKAGE/'preparation.json')['status'] == STATUS and read(PACKAGE/'scope.json')['launchReady'] is False,
            'Changed admission status')
    require(not accounting['complete'] and len(accounting['unresolved']) == 3
            and accounting['finalCumulativeSeconds'] is None and accounting['finalCumulativeBytes'] is None,
            'Do not erase unresolved accounting or claim final caps')
    require(totals(accounting['knownCharges']) == (18180, 13584*MIB), 'Changed reconciled subtotal')
    for c in accounting['knownCharges']:
        require(sha(Path(c['receiptPath'])) == c['receiptHash'], 'Changed retained charge')
        receipt = read(Path(c['receiptPath']))
        require(sha(Path(receipt['sourceReceipt'])) == receipt['sourceHash'], 'Changed charge source')
    for name, digest in accounting['sourcePins'].items():
        require(sha(Path(name)) == digest, 'Changed original accounting source')
    old = read(PACKAGE/'previous-request.json')
    require(q == compatibility_request(accounting, read(PACKAGE/'history-files.json'), old, sha(PACKAGE/'definition.json')), 'Changed compatibility request')
    require(sha(PACKAGE/'plan.json') == PLAN_PIN and d == definition(read(PACKAGE/'plan.json'), context, d['excludedCombatSeeds']), 'Changed recipe binding')
    runtime = common.inventory(PACKAGE/'runtime'); previous = read(PACKAGE/'previous-files.json')
    require(sha(PACKAGE/'previous-files.json') == PREVIOUS_PIN and sha(PACKAGE/'verification/files.json') == VERIFICATION_PIN, 'Changed provenance')
    require(runtime == read(PACKAGE/'runtime-files.json') and set(runtime) == {n[8:] for n in previous if n.startswith('runtime/')}, 'Changed runtime')
    tested = read(PACKAGE/'verification/summary.json')['buildRuntimeFiles']
    for name, digest in runtime.items():
        require(digest == (tested[name] if name in common.HARNESS_FILES else previous['runtime/'+name]), 'Changed producing runtime member')
    require(common.inventory(PACKAGE/'content') == {n[8:]: v for n, v in previous.items() if n.startswith('content/')}, 'Changed captured content')
    require(context['nativePreparations'] == context['fights'] == context['newValues'] == 0 and context['jitResolvedMethods'], 'Missing context evidence')
    sources = read(PACKAGE/'compiled-source-files.json')
    require(len(sources) == context['compiledSourceDocuments'] and common.inventory(PACKAGE/'source') == sources, 'Changed producing sources')
    native = read(PACKAGE/'native-check.json')
    require(native == dict(status='ContractValidNoReservation', version=VERSION, requestHash=canonical_hash(q), historicalValues=539367,
        maximumFights=44000, maximumNewReservations=11000, transportBindings=48, resourceFeasibilityEstablished=False, newValues=0, fights=0), 'Changed native check')
    for name in ('context', 'native-check'):
        process = read(PACKAGE/(name+'-process.json'))
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0
                and process['mechanism'] == 'suspended-owned-job-v1', 'Incomplete process cleanup')
    require(not any(Path(n).name in common.LEDGERS|{'entropy.bin'} for n in manifest), 'Unexpected reservation artifact')
    require(sha(HISTORY_PATH) == HISTORY_PIN and q['requiredHistory'] == read(HISTORY_PATH), 'Changed baseline history inventory')
    if live:
        files, values = common.history(OUTPUT.parent, old)
        require(files == q['requiredHistory'] and values == d['excludedCombatSeeds'], 'Live history changed')
    return dict(status=STATUS, launchReady=False, manifestSha256=pin, manifestFiles=len(manifest),
        requestSha256=sha(PACKAGE/'compatibility-request.json'), executionHash=context['executionHash'],
        sourceDocuments=len(sources), jitResolvedMethods=len(context['jitResolvedMethods']),
        historyFiles=len(q['requiredHistory']), exclusions=len(d['excludedCombatSeeds']),
        knownChargedSeconds=accounting['knownChargedSeconds'], knownChargedBytes=accounting['knownChargedBytes'],
        unresolvedAccountingEntries=len(accounting['unresolved']), scientificLaunches=0, newValues=0, fights=0)


def prepare():
    common = common_module()
    require(not PACKAGE.exists() and not OUTPUT.exists(), 'Use a new package and absent scientific output; never overwrite a failed attempt')
    started = time.monotonic(); PACKAGE.mkdir()
    def check():
        require(time.monotonic()-started < SECONDS-2, 'Preparation deadline reached')
        require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES, 'Preparation storage ceiling reached')
        require(not OUTPUT.exists(), 'Unexpected scientific output')
    try:
        common.save(PACKAGE/'scope.json', dict(purpose='Captured-runtime compatibility and explicit accounting gap review',
            engineeringMaximumSeconds=SECONDS, engineeringMaximumBytes=BYTES, launchReady=False,
            requestUse='CompatibilityCheckOnly', scientificLaunches=0, newValues=0, fights=0,
            proposedOverallAdditionalSeconds=6000, proposedOverallAdditionalBytes=3072*MIB,
            conditionalNativeAdditionalSeconds=5760, conditionalNativeAdditionalBytes=2944*MIB,
            outerHeadroomSeconds=240, outerHeadroomBytes=128*MIB))
        captured = common.authenticate(PREVIOUS, PREVIOUS_PIN); common.authenticate(VERIFICATION, VERIFICATION_PIN)
        evidence = read(VERIFICATION/'summary.json')
        for name, digest in evidence['sources'].items():
            if name.endswith('.cs'):
                require(sha(ROOT/name) == digest, 'Changed tested source: '+name)
        require(sha(PLAN) == PLAN_PIN, 'Changed frozen scientific plan')
        shutil.copytree(PREVIOUS/'runtime', PACKAGE/'runtime'); shutil.copytree(PREVIOUS/'content', PACKAGE/'content')
        for name in common.HARNESS_FILES:
            require(sha(BUILD/name) == evidence['buildRuntimeFiles'][name], 'Changed tested harness file')
            shutil.copyfile(BUILD/name, PACKAGE/'runtime'/name)
        (PACKAGE/'verification').mkdir()
        for name in ('files.json', 'summary.json'):
            shutil.copyfile(VERIFICATION/name, PACKAGE/'verification'/name)
        for source, target in [(Path(__file__), 'prepare.py'), (PREVIOUS/'comparison-preparation.py', 'comparison-preparation.py'),
            (PREVIOUS/'bounded_windows_process.py', 'bounded_windows_process.py'), (PREVIOUS/'files.json', 'previous-files.json'),
            (PREVIOUS/'source-request.json', 'previous-request.json'), (PLAN, 'plan.json')]:
            shutil.copyfile(source, PACKAGE/target)
        context_text = (PREVIOUS/'context.ps1').read_text(encoding='utf-8-sig')
        marker = "$typeNames = @('TowerPracticalSearch',"
        require(context_text.count(marker) == 1, 'Changed context helper entry points')
        context_text = context_text.replace(marker, "$typeNames = @('TowerFixedFamilyConfirmation',")
        (PACKAGE/'context.ps1').write_text(context_text, encoding='utf-8')
        check()
        process = common.owned(PACKAGE/'bounded_windows_process.py', ['pwsh', '-NoProfile', '-File', str(PACKAGE/'context.ps1'),
            '-Package', str(PACKAGE), '-RepositoryRoot', str(ROOT)], PACKAGE/'context.log', started+SECONDS-2, check)
        common.save(PACKAGE/'context-process.json', process)
        context = read(PACKAGE/'context.json')
        for name, digest in context['execution']['assemblyHashes'].items():
            require(digest == (evidence['buildRuntimeFiles'][name+'.dll'] if name == 'BalanceHarness' else captured['runtime/'+name+'.dll']), 'Changed gameplay assembly')
        common.save(PACKAGE/'runtime-files.json', common.inventory(PACKAGE/'runtime'))
        old = read(PACKAGE/'previous-request.json')
        files, values = common.history(OUTPUT.parent, old)
        require(sha(HISTORY_PATH) == HISTORY_PIN and files == read(HISTORY_PATH) and len(values) == 539367, 'Changed historical baseline')
        common.save(PACKAGE/'history-files.json', files)
        common.save(PACKAGE/'definition.json', definition(read(PLAN), context, values))
        accounting = reconcile(common)
        common.save(PACKAGE/'compatibility-request.json', compatibility_request(accounting, files, old, sha(PACKAGE/'definition.json')))
        check()
        process = common.owned(PACKAGE/'bounded_windows_process.py', ['dotnet', str(PACKAGE/'runtime/BalanceHarness.dll'),
            'tower-fixed-family-confirmation-check', str(PACKAGE/'compatibility-request.json')],
            PACKAGE/'native-check.log', started+SECONDS-2, check)
        common.save(PACKAGE/'native-check-process.json', process)
        common.save(PACKAGE/'native-check.json', read(PACKAGE/'native-check.log'))
        common.save(PACKAGE/'preparation.json', dict(status=STATUS, secondsBeforeSeal=time.monotonic()-started,
            nativePreparations=8, scientificLaunches=0, newValues=0, fights=0, launchReady=False,
            engineeringMaximumSeconds=SECONDS, engineeringMaximumBytes=BYTES, finalCumulativeCaps=None))
        check(); common.save(PACKAGE/'files.json', common.inventory(PACKAGE))
        result = verify(common, sha(PACKAGE/'files.json'))
        check(); result.update(secondsThroughVerification=time.monotonic()-started,
                               packageBytes=sum(p.stat().st_size for p in common.paths(PACKAGE)))
        common.save(PACKAGE.with_name(PACKAGE.name+'-pin.json'), result)
        print(json.dumps(result, indent=2))
    except BaseException as error:
        common.save(PACKAGE/'failure.json', dict(status='PreparationFailedNoLaunch', reason=str(error), scientificLaunches=0, newValues=0, fights=0))
        raise


class AdmissionTests(unittest.TestCase):
    def test_exact_recipe_projection(self):
        plan = read(PLAN); context = dict(settingsHash=plan['settingsHash'], executionHash='a'*64)
        d = definition(plan, context, [-1, 1]); self.assertEqual(8, len(d['teams']))
        self.assertEqual([[], [], [], [], [], []], [t['referenceIds'] for t in d['teams'][:6]])
        self.assertTrue(all(t['referenceIds'] for t in d['teams'][6:]))

    def test_modified_recipe_and_order_rejected(self):
        for edit in ('order', 'seed', 'essence'):
            plan = read(PLAN); teams = plan['recipesInFixedExecutionOrder']
            if edit == 'order': teams[0], teams[1] = teams[1], teams[0]
            elif edit == 'seed': teams[0]['scenario']['seeds'] = [1]
            else: teams[0]['scenario']['party'][0]['build']['essenceIds'][0] = 'changed'
            with self.assertRaises(ValueError): definition(plan, dict(settingsHash=plan['settingsHash'], executionHash='a'*64), [1])

    def test_history_must_be_complete_shape(self):
        plan = read(PLAN)
        for history in ([], [1, 1], [2, 1]):
            with self.assertRaises(ValueError): definition(plan, dict(settingsHash=plan['settingsHash'], executionHash='a'*64), history)

    def test_unknown_zero_and_duplicate_charges_rejected(self):
        charge = dict(scope='one', seconds=10, bytes=10, receiptPath=str(ROOT/'one'))
        for value in (None, 0, -1):
            with self.assertRaises(ValueError): totals([dict(charge, seconds=value)])
            with self.assertRaises(ValueError): totals([dict(charge, bytes=value)])
        with self.assertRaises(ValueError): totals([charge, charge])
        with self.assertRaises(ValueError): totals([charge, dict(charge, scope='other')])

    def test_unresolved_accounting_cannot_publish_caps(self):
        with self.assertRaises(ValueError): final_caps(dict(complete=False, unresolved=[{'bytes': None}], knownCharges=[]))
        with self.assertRaises(ValueError): final_caps(dict(complete=True, unresolved=[{'bytes': None}], knownCharges=[]))

    def test_enclosing_headroom_is_within_proposal(self):
        charge = dict(scope='one', seconds=10, bytes=10, receiptPath=str(ROOT/'one'))
        q = compatibility_request(dict(knownCharges=[charge]), {'ledger': 'a'*64}, dict(pendingHistoryRecoveries={}, recoveryReceiptHashes={}), 'b'*64)
        self.assertEqual(6000, q['maximumSeconds']-q['priorSeconds']+240)
        self.assertEqual(3072*MIB, q['maximumBytes']-q['priorBytes']+128*MIB)
        self.assertEqual(5760, sum(p['seconds'] for p in q['phases'].values())+300+60)
        self.assertEqual(2944*MIB, sum(p['bytes'] for p in q['phases'].values())+256*MIB)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare', 'verify', 'self-test'])
    parser.add_argument('--manifest-sha256')
    args = parser.parse_args()
    if args.command == 'prepare': prepare()
    elif args.command == 'verify':
        require(args.manifest_sha256 is not None, 'Supply the externally recorded manifest pin')
        print(json.dumps(verify(common_module(), args.manifest_sha256), indent=2))
    else:
        sys.exit(not unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(AdmissionTests)).wasSuccessful())
