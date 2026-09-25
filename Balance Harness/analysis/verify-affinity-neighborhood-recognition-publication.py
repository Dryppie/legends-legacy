"""One bounded postpublication verification; reads outcomes but never runs combat."""
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import struct
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
ADMISSION = ROOT/'TestResults/affinity-neighborhood-recognition-admission-20260924'
RUN = ROOT/'TestResults/balance/tower-affinity-neighborhood-recognition-20260924'
OUT = ROOT/'TestResults/affinity-neighborhood-recognition-publication-verification-20260924'
EXECUTION = ROOT/'TestResults/affinity-neighborhood-recognition-execution-20260924'
ADMISSION_PIN = '2e6cce9842430ac3383df46f449d09a9a8140c1c2c551ae2c5b8c88b02eb6287'
REQUEST_PIN = '0519d5efa039459bcf5a99f06e61b8f707d9a48f649510483a3ddb91536a4878'
HISTORY_PIN = 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
VERSION = 'tower-affinity-neighborhood-recognition-v1'
SECONDS, BYTES = 3600, 268435456


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def save(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, allow_nan=False)
        stream.write('\n')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def process_ok(value):
    require(value['mechanism'] == 'suspended-owned-job-v1' and value['exitCode'] == 0
        and not value['timedOut'] and value['activeProcesses'] == 0 and value['totalProcesses'] >= 1,
        'Incomplete owned process')


def reservations(entropy, ledger, binding):
    require(len(entropy) == 65536, 'Changed entropy length')
    historical = ledger['historical']
    require(historical == sorted(set(historical)) and historical
        and all(type(v) is int and -2**31 <= v < 2**31 for v in historical), 'Invalid historical values')
    prior, seen, words, fresh = set(historical), set(), [], []
    for i, value in enumerate(struct.unpack('<16384i', entropy)):
        if value in prior:
            kind = 'AlreadyReserved'
        elif value in seen:
            kind = 'DuplicateBatch'
        else:
            kind = 'Confirmation' if len(fresh) < 2048 else 'ReservedUnused'
            fresh.append(value)
            seen.add(value)
        words.append(dict(ordinal=i, value=value, classification=kind))
    require(len(fresh) >= 2048 and ledger['reservationState'] == 'Complete'
        and ledger['reserved'] == fresh, 'Changed permanent reservations')
    require(binding['entropyHash'] == hashlib.sha256(entropy).hexdigest() and binding['words'] == words
        and binding['panel'] == fresh[:2048] and binding['newReservations'] == fresh, 'Changed batch classification')
    return dict(historicalValues=len(historical), exposedWords=16384, newPermanentReservations=len(fresh),
        usedValues=2048, unusedPermanentlyReservedValues=len(fresh)-2048,
        classificationCounts=dict(sorted(Counter(w['classification'] for w in words).items())))


def validate_history(files, values, request, ledger, definition):
    require(ledger['historical'] == definition['excludedCombatSeeds'], 'Changed admitted history')
    require(all(files.get(n) == pin for n, pin in request['requiredHistory'].items()), 'Changed preceding history file')
    require(values == sorted(set(ledger['historical']) | set(ledger['reserved'])), 'Unexpected live exclusion union')


def verify(scientific_pin, closeout_pin):
    declaration = read(EXECUTION/'declaration.json')
    require(declaration['version'] == VERSION and declaration['postpublicationSeconds'] == SECONDS
        and declaration['postpublicationBytes'] == BYTES and declaration['admissionManifestSha256'] == ADMISSION_PIN
        and declaration['verifierSha256'] == sha(Path(__file__)), 'Changed prelaunch verification contract')
    require(not OUT.exists(), 'No verification retries or overwrite')
    OUT.mkdir()
    started = time.monotonic()
    save(OUT/'charge.json', dict(kind='ScientificPostpublicationVerification', seconds=SECONDS, bytes=BYTES,
        treatment='Full predeclared allowance charged at start, including failure; no borrowing or retry.',
        newFights=0, newValues=0))
    timer = threading.Timer(SECONDS, lambda: os._exit(124))
    timer.daemon = True
    timer.start()
    def check():
        require(time.monotonic()-started < SECONDS-2, 'Verification deadline exhausted')
        require(sum(p.stat().st_size for p in OUT.rglob('*') if p.is_file()) < BYTES-1048576,
            'Verification storage exhausted')
    try:
        shutil.copyfile(EXECUTION/'declaration.json', OUT/'declaration.json')
        shutil.copyfile(Path(__file__), OUT/'verify.py')
        require(sha(ADMISSION/'files.json') == ADMISSION_PIN and sha(ADMISSION/'request.json') == REQUEST_PIN,
            'Changed admitted manifest/request')
        admitted = read(ADMISSION/'files.json')
        # Authenticate imported helpers before allowing their code to execute.
        for name in ('auditor.py', 'run-affinity-neighborhood-recognition.py', 'bounded_windows_process.py', 'history-helper.py'):
            require(sha(ADMISSION/name) == admitted[name], 'Changed admitted helper')
        audit = module('published_auditor', ADMISSION/'auditor.py')
        audit.authenticate(ADMISSION)
        launcher = module('published_launcher', ADMISSION/'run-affinity-neighborhood-recognition.py')
        owner = module('published_owner', ADMISSION/'bounded_windows_process.py')
        require(sha(RUN/'files.json') == scientific_pin and sha(RUN/'closeout.json') == closeout_pin,
            'Changed externally pinned publication')
        save(OUT/'scientific-pin.json', dict(filesSha256=scientific_pin, closeoutSha256=closeout_pin))
        with launcher.writer_lease(RUN.parent/'complete-family-allocation'), launcher.writer_lease(RUN):
            process = owner.run(['dotnet', str(ADMISSION/'runtime/BalanceHarness.dll'),
                'tower-affinity-neighborhood-recognition-verify', str(RUN)], str(ADMISSION/'runtime'),
                str(OUT/'native-verify.log'), started+SECONDS-5, cleanup_seconds=1, check=check)
            save(OUT/'native-process.json', process)
            process_ok(process)
            require(read(OUT/'native-verify.log') == read(RUN/'result.json'), 'Verification result differs')
            require(sha(ADMISSION/'history-helper.py') == HISTORY_PIN, 'Changed complete-history helper')
            helper = module('published_history', ADMISSION/'history-helper.py')
            request = read(RUN/'request.json')
            files, values = helper.history(Path(request['registryRoot']), request)
            ledger = read(RUN/'seed-ledger.json')
            validate_history(files, values, request, ledger, read(ADMISSION/'definition.json'))
            summary = reservations((RUN/'entropy.bin').read_bytes(), ledger, read(RUN/'confirmation-binding.json'))
            save(OUT/'history-files.json', files)
            save(OUT/'history-summary.json', dict(summary, liveValues=len(values), historyFiles=len(files), helperSha256=HISTORY_PIN))
            require(sha(RUN/'files.json') == scientific_pin and sha(RUN/'closeout.json') == closeout_pin,
                'Publication changed during verification')
            check()
            result = read(RUN/'result.json')
            require(result['version'] == VERSION and result['neighborhood']['promoted'] is False
                and result['neighborhood']['additionalSamples'] == 0, 'Changed diagnostic scope')
            save(OUT/'verification.json', dict(status='VerifiedPublishedArchiveAndCompleteLiveHistory',
                seconds=time.monotonic()-started, chargedSeconds=SECONDS, chargedBytes=BYTES,
                scientificManifestSha256=scientific_pin, scientificCloseoutSha256=closeout_pin,
                admissionManifestSha256=ADMISSION_PIN, newFights=0, newValues=0, entropyDraws=0,
                ownedProcess=process, decision=result['neighborhood']['status'],
                liveValues=len(values), historyFiles=len(files)))
            save(OUT/'files.json', {p.relative_to(OUT).as_posix():sha(p) for p in sorted(OUT.rglob('*')) if p.is_file()})
            check()
            print(json.dumps(dict(status='Verified', manifestSha256=sha(OUT/'files.json'),
                seconds=time.monotonic()-started, retainedBytes=launcher.storage_bytes(OUT),
                liveValues=len(values), historyFiles=len(files)), indent=2))
    except BaseException as error:
        save(OUT/'failure.json', dict(status='VerificationFailedNoRetry', reason=str(error),
            seconds=time.monotonic()-started, chargedSeconds=SECONDS, chargedBytes=BYTES))
        raise
    finally:
        timer.cancel()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--scientific-pin', required=True)
    parser.add_argument('--closeout-pin', required=True)
    args = parser.parse_args()
    verify(args.scientific_pin, args.closeout_pin)
