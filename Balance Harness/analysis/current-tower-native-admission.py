"""Single-use, bounded native admission launcher. 'verify-ready' never launches native work."""
import gzip
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import sys
import time

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
READY = ROOT / 'TestResults/current-tower-admission-readiness-20260917'
OUTPUT = ROOT / 'TestResults/current-tower-native-admission-20260917'
DESIGN = ROOT / 'TestResults/current-tower-calibration-design-20260917'
MIB = 1048576

def require(value, message):
    if not value:
        raise ValueError(message)

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def save(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2, allow_nan=False)
        stream.write('\n')
        stream.flush()
        os.fsync(stream.fileno())

def paths(root):
    import stat
    for directory, dirs, names in os.walk(root, followlinks=False):
        for name in dirs + names:
            p = Path(directory) / name
            require(not p.stat(follow_symlinks=False).st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT, 'Linked admission member')
        for name in names:
            yield Path(directory) / name

def verify_ready():
    receipt = read(READY / 'completion.json')
    require(receipt['status'] == 'RuntimeAndRequestReadyNativeAdmissionPending', 'Readiness is incomplete')
    require(sha(READY / 'files.json') == receipt['manifestSha256'], 'Changed readiness manifest')
    manifest = read(READY / 'files.json')
    actual = {p.relative_to(READY).as_posix() for p in paths(READY)} - {'files.json', 'completion.json'}
    require(actual == set(manifest), 'Changed readiness membership')
    for name, expected in manifest.items():
        require(sha(READY / name) == expected, 'Changed readiness artifact: ' + name)
    require(sha(DESIGN / 'files.json') == 'bf454b4228637406847c10186e75a277bb47d825cc77a10fa6011221adf656b6', 'Changed design seal')
    for name, expected in read(DESIGN / 'files.json').items():
        require(sha(DESIGN / name) == expected, 'Changed design member: ' + name)
    plan, request = read(READY / 'launch-plan.json'), read(READY / 'request.proposed.json')
    require(plan['outerSeconds'] == 1800 and plan['outerBytes'] == 2048 * MIB
            and request['maximumSeconds'] == 1500 and request['maximumBytes'] == 1920 * MIB, 'Changed launch envelope')
    require(request['executionHash'] == read(READY / 'captured-context.json')['executionHash'], 'Changed runtime binding')
    require(plan['command'] == ['dotnet', str(READY / 'runtime/BalanceHarness.dll'), 'tower-current-family-admit',
            str(READY / 'request.proposed.json'), str(OUTPUT / 'native')], 'Changed admission command')
    size = sum(p.stat().st_size for p in paths(READY))
    require(size < 64 * MIB, 'Readiness exceeds its enclosing allowance')
    return plan, size

def audit_rows(native, checkpoint):
    """Independent saved-row accounting, without native reconstruction or participant reserialization."""
    manifest = read(native / 'files.json')
    require(set(manifest) == {p.name for p in native.iterdir() if p.is_file()} - {'files.json'}, 'Changed native membership')
    for name, expected in manifest.items():
        checkpoint()
        require('/' not in name and '\\' not in name and sha(native / name) == expected, 'Changed native member')
    require(sha(native / 'design-files.json') == 'bf454b4228637406847c10186e75a277bb47d825cc77a10fa6011221adf656b6', 'Changed input design binding')
    for name in ['target-template.json', 'cells.jsonl.gz', 'entries.jsonl.gz', 'incompatibilities.json', 'inventory-summary.json']:
        require(sha(native / name) == read(native / 'design-files.json')[name], 'Changed copied input: ' + name)
    result = read(native / 'result.json')
    require(result == read(native / 'audit.json'), 'Built-in saved audit differs')
    identities, matched, forced = {}, set(), set()
    rows = invalid = 0
    with gzip.open(native / 'cells.jsonl.gz', 'rt', encoding='utf-8') as cells, gzip.open(native / 'rows.jsonl.gz', 'rt', encoding='utf-8') as saved:
        for line in cells:
            checkpoint()
            cell, row = json.loads(line), json.loads(next(saved))
            require(row['ordinal'] == rows and row['inputKey'] == cell['inputKey'], 'Missing or reordered admission row')
            for key in ['entryOrdinals', 'anchorReasons', 'requiredControlIds']:
                require(row[key] == cell[key], 'Changed origin/control mapping')
            rows += 1
            if row['status'] == 'Invalid':
                require(row['error'] and all(row[k] is None for k in ['inputHash', 'participantsHash', 'participants', 'aliasOf']), 'Invalid prepared evidence')
                invalid += 1
                continue
            require(row['status'] == 'Materialized' and row['error'] is None, 'Unexpected row status')
            key = row['identity']['cellHash']
            if key in identities:
                prior = identities[key]
                require(row['aliasOf'] == prior['inputKey'] and row['inputHash'] == prior['inputHash']
                        and row['participantsHash'] == prior['participantsHash'], 'Conflicting native alias')
            else:
                require(row['aliasOf'] is None, 'Unknown native alias')
                identities[key] = {k: row[k] for k in ['inputKey', 'inputHash', 'participantsHash']}
            matched.update(cell['requiredControlIds'])
            if cell['anchorReasons']:
                forced.add(key)
        require(next(saved, None) is None, 'Extra saved row')
    require(rows == 46077 and result['cells'] == rows and result['invalid'] == invalid
            and result['materialized'] == rows - invalid and result['distinctNativeCells'] == len(identities)
            and result['aliasCells'] == rows - invalid - len(identities) and result['forcedNativeCells'] == len(forced)
            and result['matchedControls'] == sorted(matched) and result['sourceOccurrences'] == 51624
            and result['forcedInputCells'] == 973 and result['incompatibleOccurrences'] == 162
            and result['fights'] == result['newSeeds'] == 0 and result['readyForFamilyFreeze'] is False, 'Saved row totals differ')
    return {'status': 'SavedCountsVerified', 'result': result, 'nativePreparationsByThisAudit': 0, 'fights': 0, 'newValues': 0}

def run():
    started = time.monotonic()
    require(not OUTPUT.exists(), 'Existing output consumes this launch; no retry or resume')
    plan, ready_bytes = verify_ready()
    OUTPUT.mkdir()
    deadline = started + 1800
    def guard():
        require(time.monotonic() < deadline - 10, 'Enclosing admission deadline')
        files = list(paths(OUTPUT))
        require(ready_bytes + sum(p.stat().st_size for p in files) < 2048 * MIB - 65536, 'Combined admission byte limit')
        require(sum(p.stat().st_size for p in files if 'native' not in p.relative_to(OUTPUT).parts) < 64 * MIB - 65536,
                'Oversight/log allowance exceeded')
    try:
        save(OUTPUT / 'launch.json', {'status': 'SingleUseStarted', 'readinessManifest': sha(READY / 'files.json'),
             'maximumSeconds': 1800, 'maximumBytes': 2048 * MIB, 'nativeMaximumSeconds': 1500,
             'nativeMaximumBytes': 1920 * MIB, 'setupBytesCharged': ready_bytes, 'engineeringBudgetReused': False,
             'newFights': 0, 'newValues': 0, 'retries': 0, 'resume': False})
        spec = importlib.util.spec_from_file_location('native_admission_owner', READY / 'bounded_output_process.py')
        owner = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(owner)
        process = owner.run(plan['command'], ROOT, OUTPUT / 'native.log',
                            min(deadline - 60, time.monotonic() + 1501), cleanup_seconds=1, guard=guard)
        save(OUTPUT / 'process.json', process)
        require(process['activeProcesses'] == 0 and not process['timedOut'] and process['exitCode'] in (1, 3), 'Native admission stopped or returned an unexpected exit')
        guard()
        require(not (OUTPUT / 'native/failure.json').exists(), 'Native failure remains unresolved')
        audit = audit_rows(OUTPUT / 'native', guard)
        require((process['exitCode'] == 3 and audit['result']['status'] == 'AdmittedWithContextExceptions')
                or (process['exitCode'] == 1 and audit['result']['status'] == 'AdmissionIssues'), 'Result/exit mismatch')
        save(OUTPUT / 'independent-audit.json', audit)
        verify_ready()
        guard()
        save(OUTPUT / 'files.json', {p.relative_to(OUTPUT).as_posix(): sha(p) for p in paths(OUTPUT)})
        save(OUTPUT / 'completion.json', {'status': 'NativeAdmissionClosed', 'manifestSha256': sha(OUTPUT / 'files.json'),
             'measuredSecondsBeforeReceipt': time.monotonic() - started, 'chargedSeconds': 1800, 'chargedBytes': 2048 * MIB,
             'nativeStatus': audit['result']['status'], 'familyFreezeAllowed': False, 'fights': 0, 'newValues': 0})
        require(time.monotonic() < deadline and ready_bytes + sum(p.stat().st_size for p in paths(OUTPUT)) <= 2048 * MIB,
                'Closeout exceeded envelope')
        print(json.dumps(read(OUTPUT / 'completion.json')))
    except BaseException as error:
        save(OUTPUT / 'failure.json', {'status': 'ClosedWithoutAcceptedAdmission', 'error': str(error)[:2048],
             'elapsedSeconds': time.monotonic() - started, 'chargedSeconds': 1800, 'chargedBytes': 2048 * MIB,
             'resume': False, 'retries': 0, 'fights': 0, 'newValues': 0})
        raise

if __name__ == '__main__':
    if sys.argv[1:] == ['verify-ready']:
        plan, size = verify_ready()
        print(json.dumps({'status': 'ReadyPackageVerified', 'readinessBytes': size, 'nativeAdmissionExecuted': False}))
    elif sys.argv[1:] == ['run']:
        run()
    else:
        raise SystemExit('Use verify-ready (read-only) or run (one bounded native admission; never retry).')
