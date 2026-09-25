"""Final production verification inside the pilot's original remaining limits."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'build'))
import bounded_windows_process

PILOT = ROOT / 'TestResults/balance/tower-loadout-placement-pilot-01-20260925'
OUTPUT = ROOT / 'TestResults/loadout-placement-pilot-verification-20260925'
DECLARATION = ROOT / 'Balance Harness/Tower-Loadout-Placement-Pilot-01-Declaration.json'
DECLARATION_PIN = '8874dd2a558f1d0878984dcf93fd218e1fc257b1b30e159b262100eace0e810b'


def require(condition, message):
    if not condition: raise ValueError(message)


def sha(path):
    with path.open('rb') as stream: return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream: stream.write(json.dumps(value, indent=2) + '\n')


def run(closeout_pin):
    require(not OUTPUT.exists() and sha(DECLARATION) == DECLARATION_PIN, 'Changed execution declaration or verification retry')
    require(sha(PILOT / 'closeout.json') == closeout_pin and not (PILOT / 'failure.json').exists(), 'No complete pinned pilot')
    declaration = read(DECLARATION); launch = read(PILOT / 'launch.json')
    closeout = read(PILOT / 'closeout.json'); completion = read(PILOT / 'completion.json')
    start = datetime.fromisoformat(launch['startedAt'].replace('Z', '+00:00')).timestamp()
    end = min(start + declaration['maximumSeconds'], start + completion['nativeSeconds'] + declaration['auditPublicationMaximumSeconds'])
    remaining = end - datetime.now(timezone.utc).timestamp()
    require(remaining > 5 and closeout['retainedBytes'] < declaration['maximumBytes'], 'Original pilot envelope exhausted')
    dll = PILOT / 'executable/BalanceHarness.dll'
    require(sha(dll) == declaration['qualifiedHarnessSha256'], 'Changed retained qualified harness')
    OUTPUT.mkdir(); started = time.monotonic()
    write(OUTPUT / 'start.json', dict(closeoutSha256=closeout_pin, declarationSha256=DECLARATION_PIN,
        verifierSha256=sha(Path(__file__)), remainingOriginalSeconds=remaining, noAdditionalTimeAllowance=True,
        actualCombat=0, productionEntropyDraws=0, newScientificReservations=0))
    def check():
        extra = sum(p.stat().st_size for p in OUTPUT.rglob('*') if p.is_file())
        require(datetime.now(timezone.utc).timestamp() < end
            and closeout['retainedBytes'] + extra < declaration['maximumBytes']
            and closeout['auditBytes'] + extra < declaration['auditPublicationMaximumBytes'], 'Original verification envelope exhausted')
    failure = None
    try:
        process = bounded_windows_process.run(['dotnet', str(dll), 'tower-proposal-study-verify', str(PILOT), closeout_pin],
            str(ROOT), str(OUTPUT / 'native-verification.log'), started + min(1200, remaining) - 2,
            check=check, cleanup_seconds=1, log_byte_limit=1048576)
        write(OUTPUT / 'process.json', process)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Final native verification failed')
        require(read(OUTPUT / 'native-verification.log') == read(PILOT / 'result.json'), 'Published result does not reconstruct')
        check()
    except Exception as error:
        failure = str(error); write(OUTPUT / 'failure.json', dict(reason=failure, retryPermitted=False))
    write(OUTPUT / 'verification.json', dict(status='Failed' if failure else 'ScientificPilotReconstructed',
        closeoutSha256=closeout_pin, secondsBeforeSealing=time.monotonic()-started, recordedTimingIsLowerBound=True,
        withinOriginalAuditDeadline=datetime.now(timezone.utc).timestamp() < end, additionalAllowanceSeconds=0,
        actualCombat=0, productionEntropyDraws=0, newScientificReservations=0))
    write(OUTPUT / 'files.json', {p.relative_to(OUTPUT).as_posix(): sha(p) for p in sorted(OUTPUT.rglob('*')) if p.is_file()})
    print(json.dumps(dict(output=str(OUTPUT), manifestSha256=sha(OUTPUT / 'files.json'), failure=failure), indent=2))
    if failure: raise ValueError(failure)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--closeout-pin', required=True)
    run(parser.parse_args().closeout_pin)
