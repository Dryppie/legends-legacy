"""Freeze the accepted scoped accounting and once-only launcher; never run combat."""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT/'TestResults/fixed-family-runtime-admission-20260922'
RUNTIME_PIN = '49fa8a4eac7f40da417308f2aea270fb63f87d1f5c1ba0cbf2f9f1990be136bd'
PACKAGE = ROOT/'TestResults/fixed-family-final-admission-20260922'
OVERSIGHT = ROOT/'TestResults/fixed-family-execution-20260922'
SECONDS, BYTES = 300, 16*1048576
MIB = 1048576


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def runtime_helper():
    # Check the immutable producing helper before importing it.
    import hashlib
    digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
    if digest(RUNTIME/'files.json') != RUNTIME_PIN:
        raise ValueError('Changed runtime-admission manifest')
    manifest = json.loads((RUNTIME/'files.json').read_text())
    if digest(RUNTIME/'prepare.py') != manifest['prepare.py']:
        raise ValueError('Changed producing preparation helper')
    return load('captured_fixed_family_admission', RUNTIME/'prepare.py')


def accepted_accounting(a):
    return dict(status='ScopedAccountingAccepted', accepted=True,
        userReply='Keep engineering separately disclosed (Recommended)',
        decision='Use the reconciled 18180-second /13584-MiB prior-charge ledger. Retain missing engineering costs and the two later selector audits as separately disclosed engineering; unknown totals are not zero.',
        knownCharges=a['knownCharges'], priorSeconds=a['knownChargedSeconds'], priorBytes=a['knownChargedBytes'],
        separatelyDisclosedEngineering=a['unresolved'], allInEngineeringTotalsKnown=False,
        laterPreparationTreatment='Final request/launcher preparation, tests and documentation after this ledger snapshot remain separately disclosed engineering under the accepted boundary. They do not refund prior charges or consume/increase the experiment allowance.',
        overallAdditionalSeconds=6000, overallAdditionalBytes=3072*MIB,
        overallCumulativeSeconds=a['knownChargedSeconds']+6000,
        overallCumulativeBytes=a['knownChargedBytes']+3072*MIB)


def validate_accounting(accounting, source, request, require):
    require(accounting == accepted_accounting(source), 'Changed or missing explicit accounting decision')
    require(request['priorCharges'] == accounting['knownCharges']
        and request['priorSeconds'] == accounting['priorSeconds'] == 18180
        and request['priorBytes'] == accounting['priorBytes'] == 13584*MIB
        and request['maximumSeconds'] == accounting['overallCumulativeSeconds']-240
        and request['maximumBytes'] == accounting['overallCumulativeBytes']-128*MIB,
        'Changed scoped ledger or enclosing headroom')


def verify(pin, live=True, execution_claim=None):
    prior = runtime_helper(); common = prior.common_module()
    manifest = common.authenticate(PACKAGE, pin)
    runtime = prior.verify(common, RUNTIME_PIN, live=live)
    if execution_claim is None:
        common.require(not OVERSIGHT.exists(), 'Execution has already claimed this scope; no retry')
    else:
        common.require(Path(execution_claim) == OVERSIGHT and not (OVERSIGHT/'launch-intent.json').exists()
            and not (OVERSIGHT/'failure.json').exists()
            and common.read(OVERSIGHT/'claim.json') == dict(status='ClaimedOnceBeforePreflight', admissionManifestSha256=pin,
                attemptsAllowed=1, retriesAllowed=0), 'Invalid prelaunch execution claim')
    q = common.read(PACKAGE/'request.json')
    common.require(common.sha(PACKAGE/'request.json') == common.sha(RUNTIME/'compatibility-request.json'), 'Changed native request')
    validate_accounting(common.read(PACKAGE/'accounting-decision.json'), common.read(RUNTIME/'accounting.json'), q, common.require)
    common.require(common.read(PACKAGE/'runtime-binding.json') == dict(package=str(RUNTIME), manifestSha256=RUNTIME_PIN,
        requestSha256=common.sha(PACKAGE/'request.json'), executionHash=runtime['executionHash']), 'Changed captured binding')
    common.require(common.read(PACKAGE/'scope.json') == scope(), 'Changed enclosing scope')
    return dict(status='ReadyForSingleBoundedExecution', manifestSha256=pin, manifestFiles=len(manifest),
        runtimeManifestSha256=RUNTIME_PIN, requestSha256=common.sha(PACKAGE/'request.json'),
        nativeRequestHash=common.read(RUNTIME/'native-check.json')['requestHash'],
        priorSeconds=q['priorSeconds'], priorBytes=q['priorBytes'],
        overallMaximumSeconds=q['priorSeconds']+6000, overallMaximumBytes=q['priorBytes']+3072*MIB,
        additionalSeconds=6000, additionalBytes=3072*MIB, exclusions=runtime['exclusions'], historyFiles=runtime['historyFiles'],
        separatelyDisclosedEngineering=True, scientificLaunches=0, newValues=0, fights=0)


def scope():
    return dict(version='tower-practical-fixed-family-confirmation-v1',
        runtimePackage=str(RUNTIME), runtimeManifestSha256=RUNTIME_PIN,
        outputRoot=str(ROOT/'TestResults/balance/tower-fixed-family-confirmation-20260922'), oversightRoot=str(OVERSIGHT),
        additionalSeconds=6000, additionalBytes=3072*MIB, nativeSeconds=5760, nativeBytes=2944*MIB,
        outerSeconds=240, outerBytes=128*MIB, maximumFights=44000, maximumNewReservations=11000,
        attempts=1, retries=0, resume=False, extraPublicVerifiers=0,
        audits='Native reconstruction and independent direct-outcome audit are mandatory parts of the native run. The enclosing launcher authenticates their receipts and final inventories without re-running combat or audits.',
        preparationMaximumSeconds=SECONDS, preparationMaximumBytes=BYTES, preparationAccounting='Separate engineering, as accepted by the user')


def prepare():
    prior = runtime_helper(); common = prior.common_module()
    common.require(not PACKAGE.exists() and not OVERSIGHT.exists() and not prior.OUTPUT.exists(), 'Create-only final admission')
    started = time.monotonic(); PACKAGE.mkdir()
    def guard():
        common.require(time.monotonic()-started < SECONDS-2, 'Final admission deadline')
        common.require(sum(p.stat().st_size for p in common.paths(PACKAGE)) < BYTES, 'Final admission storage limit')
    try:
        common.save(PACKAGE/'scope.json', scope())
        common.save(PACKAGE/'accounting-decision.json', accepted_accounting(common.read(RUNTIME/'accounting.json')))
        shutil.copyfile(RUNTIME/'compatibility-request.json', PACKAGE/'request.json')
        shutil.copyfile(Path(__file__), PACKAGE/'admission.py')
        shutil.copyfile(ROOT/'Balance Harness/analysis/run-fixed-family-confirmation.py', PACKAGE/'launcher.py')
        shutil.copyfile(RUNTIME/'bounded_windows_process.py', PACKAGE/'bounded_windows_process.py')
        for name in ('fixed-family-accounting-tests-20260922.log', 'fixed-family-launcher-tests-20260922.log'):
            shutil.copyfile(ROOT/'TestResults'/name, PACKAGE/name)
        common.save(PACKAGE/'runtime-binding.json', dict(package=str(RUNTIME), manifestSha256=RUNTIME_PIN,
            requestSha256=common.sha(PACKAGE/'request.json'), executionHash=common.read(RUNTIME/'context.json')['executionHash']))
        # Same request bytes and producing runtime as the native check. Revalidate
        # its full sealed evidence and live history; another native preparation is unnecessary.
        guard(); common.save(PACKAGE/'files.json', common.inventory(PACKAGE))
        result = verify(common.sha(PACKAGE/'files.json'))
        guard(); result.update(preparationSeconds=time.monotonic()-started,
            preparationBytes=sum(p.stat().st_size for p in common.paths(PACKAGE)),
            preparationMaximumSeconds=SECONDS, preparationMaximumBytes=BYTES,
            preparationAccounting='Separate engineering under the accepted boundary')
        common.save(PACKAGE.with_name(PACKAGE.name+'-pin.json'), result)
        print(json.dumps(result, indent=2))
    except BaseException as error:
        common.save(PACKAGE/'failure.json', dict(status='FinalAdmissionFailedNoLaunch', reason=str(error), scientificLaunches=0))
        raise


class AccountingTests(unittest.TestCase):
    def test_acceptance_preserves_unknowns_and_exact_charges(self):
        prior = runtime_helper(); a = prior.read(RUNTIME/'accounting.json')
        decision = accepted_accounting(a)
        self.assertEqual(a['unresolved'], decision['separatelyDisclosedEngineering'])
        self.assertFalse(decision['allInEngineeringTotalsKnown'])
        validate_accounting(decision, a, prior.read(RUNTIME/'compatibility-request.json'), prior.require)

    def test_missing_decision_dropped_engineering_and_inflated_caps_rejected(self):
        prior = runtime_helper(); a = prior.read(RUNTIME/'accounting.json'); q = prior.read(RUNTIME/'compatibility-request.json')
        for edit in ({'accepted': False}, {'separatelyDisclosedEngineering': []}, {'overallCumulativeSeconds': 24181}, {'allInEngineeringTotalsKnown': True}):
            with self.assertRaises(ValueError): validate_accounting(dict(accepted_accounting(a), **edit), a, q, prior.require)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare', 'verify', 'self-test'])
    parser.add_argument('--manifest-sha256')
    parser.add_argument('--execution-claim')
    args = parser.parse_args()
    if args.command == 'prepare': prepare()
    elif args.command == 'verify':
        if not args.manifest_sha256: raise ValueError('An external manifest pin is required')
        print(json.dumps(verify(args.manifest_sha256, execution_claim=args.execution_claim), indent=2))
    else:
        sys.exit(not unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(AccountingTests)).wasSuccessful())
