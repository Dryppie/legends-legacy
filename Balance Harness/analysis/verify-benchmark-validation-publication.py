"""Bounded read-only verification of the v5 comparison and its complete live history.

The separate 600-second /64-MiB engineering allowance never extends the study's
native or audit budgets. The admitted native verifier cannot run new combat.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-benchmark-validation-comparison-v1'
ASSIGNED_VALUES = 4668
HISTORY_HELPER = ROOT/'TestResults/three-reference-admission-closeout-20260922/comparison-preparation.py'
HISTORY_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
SECONDS, BYTES = 600, 64*1048576


def require(condition, message):
    if not condition: raise ValueError(message)


def sha(path):
    with path.open('rb') as stream: return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result); return result


def history_summary(ledger, allocation, history_files, values, required_history, study):
    historical, reserved, selected = ledger['historical'], ledger['reserved'], allocation['selected']
    require(allocation['version'] == VERSION and ledger['reservationState'] == 'Complete' and historical == sorted(set(historical))
            and reserved == sorted(set(reserved)) and not set(historical).intersection(reserved)
            and ASSIGNED_VALUES <= len(reserved) <= 16384 and allocation['reserved'] == reserved
            and len(selected) == len(set(selected)) == ASSIGNED_VALUES and set(selected) <= set(reserved),
            'Changed complete reservation or assigned values')
    require(values == sorted(set(historical)|set(reserved)), 'Unexpected live reservation union')
    own = {str(study/'history-input.json'), str(study/'seed-ledger.json')}
    require(set(history_files) == set(required_history)|own
            and all(history_files[n] == pin for n,pin in required_history.items()),
            'Unexpected historical files or changed preceding reservation')
    return dict(historicalValues=len(historical), newPermanentReservations=len(reserved), assignedValues=len(selected),
        unusedPermanentlyReservedValues=len(reserved)-len(selected), liveValues=len(values), historyFiles=len(history_files),
        newFights=0, newValues=0)


def verification_accounting(prior, receipt):
    """Keep recorded use distinct from explicit cumulative declared ceilings."""
    result = {}
    declared_keys = {'cumulativeDeclaredMaximumSeconds', 'cumulativeDeclaredMaximumBytes'}
    require(not declared_keys.intersection(prior) or declared_keys <= prior.keys(),
            'Incomplete declared allowance ledger')
    explicit = declared_keys <= prior.keys()
    for unit, scientific, verification in (('Seconds',10800,SECONDS),('Bytes',6442450944,BYTES)):
        recorded = prior['totalRecordedCharged'+unit]
        require(prior['cumulativeMaximum'+unit] == recorded+scientific, 'Changed declared cumulative allowance')
        # Older admissions retained only the recorded-use-plus-next-run ceiling.
        # New admissions also retain all preceding declared allowances; preserve
        # that stronger ledger instead of silently replacing it with actual use.
        ceiling = prior['cumulativeDeclaredMaximum'+unit] if explicit else recorded
        require(ceiling >= recorded, 'Declared ceiling omits recorded charges')
        result['totalRecordedCharged'+unit] = recorded+receipt['charged'+unit]+verification
        result['cumulativeDeclaredMaximum'+unit] = ceiling+scientific+verification
    result['declaredMaximumBasis'] = 'ExplicitPrecedingDeclaredAllowances' if explicit else 'LegacyRecordedChargesPlusFutureAllowances'
    return result


def main(args):
    admission, study, output = (p.resolve() for p in (args.admission, args.study, args.output))
    require(os.name == 'nt' and all(p.is_relative_to(ROOT/'TestResults') for p in (admission, study, output)),
            'Use retained Windows artifacts within TestResults')
    require(not output.exists() and not output.is_relative_to(study) and not output.is_relative_to(admission),
            'No verification retry, overwrite or writes inside sealed evidence')
    require(not (study/'failure.json').exists() and sha(study/'closeout.json') == args.closeout_pin,
            'Supply the external successful scientific closeout pin')
    require(sha(admission/'files.json') == args.admission_pin, 'Changed external admission pin')
    started = time.monotonic(); output.mkdir()
    def save(name, value):
        with (output/name).open('x', encoding='utf-8') as stream:
            json.dump(value, stream, indent=2, allow_nan=False); stream.write('\n')
    save('declaration.json',dict(kind='ReadOnlyPublishedBenchmarkValidationVerification', version=VERSION,
        assignedValues=ASSIGNED_VALUES, chargedSeconds=SECONDS, chargedBytes=BYTES,
        studyRoot=str(study), admissionRoot=str(admission), admissionManifestSha256=args.admission_pin,
        scientificCloseoutSha256=args.closeout_pin, fights=0, newValues=0,
        scope='Full separate engineering allowance charged, including failure; no allocation, combat or extension of scientific limits.'))
    shutil.copyfile(__file__, output/'verify.py')
    timer = threading.Timer(SECONDS, lambda:os._exit(124)); timer.daemon=True; timer.start()
    try:
        owner = module('published_proposal_owner', admission/'run-proposal-affinity-study.py')
        process_owner = module('published_proposal_job', admission/'bounded_windows_process.py')
        def check():
            require(time.monotonic()-started < SECONDS and owner.storage_bytes(output) < BYTES, 'Verification allowance exhausted')
        owner.validate_admission(admission/'request.json', admission/'runtime/BalanceHarness.dll', args.admission_pin)
        q = read(study/'request.json')
        require(q['version'] == VERSION and Path(q['outputRoot']) == study and Path(q['registryRoot']) == study.parent
                and sha(study/'request.json') == sha(admission/'request.json'), 'Changed scientific request')
        with owner.writer_lease(study.parent/'complete-family-allocation'), owner.writer_lease(study):
            receipt = read(study/'closeout.json'); scientific_pin = sha(study/'files.json')
            require(receipt['filesHash'] == scientific_pin, 'Changed scientific manifest')
            process = process_owner.run(['dotnet',str(admission/'runtime/BalanceHarness.dll'),
                'tower-proposal-study-verify',str(study),args.closeout_pin], str(admission/'runtime'),
                str(output/'native-verify.log'), started+SECONDS-5, cleanup_seconds=1, check=check)
            save('native-process.json',process)
            require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0
                    and process['mechanism'] == 'suspended-owned-job-v1', 'Published native verification failed')
            require(read(output/'native-verify.log') == read(study/'result.json'), 'Changed reconstructed result')
            require(sha(HISTORY_HELPER) == HISTORY_PIN, 'Changed full-history reader')
            files, values = module('published_proposal_history', HISTORY_HELPER).history(study.parent,q)
            history = history_summary(read(study/'seed-ledger.json'), read(study/'allocation.json'), files, values, q['requiredHistory'], study)
            save('history-files.json',files); save('history-summary.json',dict(history, helperSha256=HISTORY_PIN))
            require(sha(study/'closeout.json') == args.closeout_pin and sha(study/'files.json') == scientific_pin,
                    'Publication changed during verification')
            prior = read(admission/'admission.json')
            accounting = verification_accounting(prior, receipt)
            check()
            save('verification.json',dict(status='VerifiedPublishedArchiveAndCompleteLiveHistory',
                scientificManifestSha256=scientific_pin, scientificCloseoutSha256=args.closeout_pin,
                admissionManifestSha256=args.admission_pin, scientificFights=read(study/'result.json')['fights'],
                scientificChargedSeconds=receipt['chargedSeconds'], scientificChargedBytes=receipt['chargedBytes'],
                verificationChargedSeconds=SECONDS, verificationChargedBytes=BYTES,
                measuredVerificationSecondsBeforeSealing=time.monotonic()-started, ownedProcess=process, **accounting, **history))
            save('files.json',{p.relative_to(output).as_posix():sha(p) for p in owner.inventory(output)})
            check()
            print(json.dumps(dict(status='Verified', manifestSha256=sha(output/'files.json'),
                measuredSeconds=time.monotonic()-started,retainedBytes=owner.storage_bytes(output), **history),indent=2))
    except BaseException as error:
        save('failure.json',dict(reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES,seconds=time.monotonic()-started))
        raise
    finally: timer.cancel()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('admission','study','output'): parser.add_argument('--'+name,required=True,type=Path)
    parser.add_argument('--admission-pin',required=True); parser.add_argument('--closeout-pin',required=True)
    main(parser.parse_args())
