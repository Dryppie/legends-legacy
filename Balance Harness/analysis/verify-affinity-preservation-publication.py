"""Bounded read-only verification of the preservation comparison and its complete live history.

The separate 600-second /64-MiB engineering allowance never extends the study's
native or audit budgets. The admitted native verifier cannot run new combat.
"""
import argparse
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-affinity-preservation-comparison-v1'
ASSIGNED_VALUES = 4380
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
    """Carry observed use separately; add the final verification use at closeout."""
    result = {}
    require(prior.get('status') == 'PreservationComparisonAdmittedNoReservation', 'Missing admitted accounting ledger')
    for unit, scientific, verification in (('Seconds',10800,SECONDS),('Bytes',6442450944,BYTES)):
        recorded = prior.get('cumulativeRecorded'+unit)
        ceiling = prior.get('cumulativeDeclaredMaximum'+unit)
        cost = receipt.get('charged'+unit)
        require(all(type(v) in (int,float) and math.isfinite(v) and v >= 0 for v in (recorded,ceiling,cost))
                and ceiling >= recorded and 0 < cost < scientific, 'Changed recorded or declared allowance ledger')
        result['recordedBeforeVerification'+unit] = recorded+cost
        result['cumulativeDeclaredMaximum'+unit] = ceiling+scientific+verification
    result['declaredMaximumBasis'] = 'ExplicitPrecedingDeclaredAllowances'
    return result


def validate_execution(d, prior, admission, study, output):
    require(d['version'] == 'tower-affinity-preservation-execution-v1' and d['studyVersion'] == VERSION
            and (d['scientificLaunches'],d['retries'],d['maximumFights'],d['requiredFreshValues']) == (1,0,21888,ASSIGNED_VALUES)
            and (d['scientificMaximumSeconds'],d['scientificMaximumBytes'],d['nativeMaximumSeconds'],d['auditMaximumSeconds'])
                == (10800,6442450944,9000,1800)
            and Path(d['admissionRoot']) == admission and Path(d['outputRoot']) == study,
            'Changed single execution contract')
    pub = d['publicationVerification']
    require(Path(pub['outputRoot']) == output and pub['chargedSeconds'] == SECONDS and pub['chargedBytes'] == BYTES
            and pub['fights'] == pub['newValues'] == 0, 'Changed independent verification allowance')
    require(prior['status'] == 'PreservationComparisonAdmittedNoReservation'
            and all(d['priorAccounting'][k] == prior[k] for k in (
                'cumulativeRecordedSeconds','cumulativeRecordedBytes','cumulativeDeclaredMaximumSeconds','cumulativeDeclaredMaximumBytes')),
            'Changed prior accounting')


def main(args):
    admission, study, output = (p.resolve() for p in (args.admission, args.study, args.output))
    require(os.name == 'nt' and all(p.is_relative_to(ROOT/'TestResults') for p in (admission, study, output)),
            'Use retained Windows artifacts within TestResults')
    require(not output.exists() and not output.is_relative_to(study) and not output.is_relative_to(admission),
            'No verification retry, overwrite or writes inside sealed evidence')
    require(not (study/'failure.json').exists() and sha(study/'closeout.json') == args.closeout_pin,
            'Supply the external successful scientific closeout pin')
    require(sha(admission/'files.json') == args.admission_pin, 'Changed external admission pin')
    require(sha(args.declaration) == args.declaration_pin, 'Changed external execution declaration')
    d = read(args.declaration)
    prior_root = Path(d['admissionVerificationRoot'])
    require(sha(prior_root/'files.json') == d['admissionVerificationManifestSha256'], 'Changed admission handoff pin')
    prior_manifest = read(prior_root/'files.json')
    prior_path = prior_root/'closeout.json'
    require(sha(prior_path) == prior_manifest[prior_path.relative_to(ROOT).as_posix()], 'Changed prior accounting receipt')
    prior = read(prior_path); validate_execution(d,prior,admission,study,output)
    require(d['admissionManifestSha256'] == args.admission_pin and d['requestSha256'] == sha(admission/'request.json'),
            'Changed declared admission')
    require(all(sha(ROOT/name) == pin for name,pin in d['publicationVerification']['sourceHashes'].items()), 'Changed declared verifier')
    require(d['publicationVerification']['sourceHashes'].get(Path(__file__).relative_to(ROOT).as_posix()) == sha(Path(__file__)),
            'Undeclared publication verifier')
    started = time.monotonic(); output.mkdir()
    def save(name, value):
        with (output/name).open('x', encoding='utf-8') as stream:
            json.dump(value, stream, indent=2, allow_nan=False); stream.write('\n')
    save('declaration.json',dict(kind='ReadOnlyPublishedAffinityPreservationVerification', version=VERSION,
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
    parser.add_argument('--declaration',required=True,type=Path); parser.add_argument('--declaration-pin',required=True)
    main(parser.parse_args())
