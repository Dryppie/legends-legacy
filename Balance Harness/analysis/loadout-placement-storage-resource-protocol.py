"""Authenticate the prospective storage accounting contract and its necessary gate.

This tool has no process, codec, fixture, entropy, qualification or admission path.
The retained recovery implementation is authenticated before executing its pure
verification. No historical archives are rescanned or rewritten.
"""
import argparse
import hashlib
import importlib.util
import json
import math
from pathlib import Path, PurePosixPath
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-loadout-placement-storage-resource-protocol-v1'
PLAN = 'Balance Harness/Tower-Loadout-Placement-Storage-Resource-Protocol.json'
PLAN_PIN = '9527ac21c60426cdfac769602860744f5f368c25b2ea6ea4f72103ccf68ded4e'
PRIOR = 'TestResults/loadout-placement-storage-integration-handoff-20260924.json'
PRIOR_PIN = '1a22b7af978898ee10243fba7dafe518a5b7736c0bffc96c271ff301772bc9fa'
PROOF = 'TestResults/loadout-placement-storage-integration-verification-20260924/logical-equivalence.json'
REGISTRATION = 'TestResults/loadout-placement-storage-resource-protocol-20260924'
RECOVERY_HELPER = 'implementation/Balance Harness/analysis/loadout-placement-recovery-protocol.py'
LIMITS = dict(nativeSeconds=9000, auditSeconds=1800, nativeBytes=5905580032, auditBytes=536870912)
SECONDS, BYTES = 180, 64 * 1048576


def require(ok, message):
    if not ok:
        raise ValueError(message)


def unlinked(path):
    for part in (path, *path.parents):
        if part.exists() or part.is_symlink():
            info = part.lstat()
            require(not part.is_symlink() and not getattr(info, 'st_file_attributes', 0) & 1024,
                    'Linked evidence path: ' + str(part))
    return path


def sha(path):
    with unlinked(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, 'Duplicate JSON key')
            result[key] = value
        return result
    return json.loads(unlinked(path).read_text(encoding='utf-8-sig'), object_pairs_hook=pairs,
                      parse_constant=lambda _: require(False, 'Nonfinite JSON constant'))


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    with unlinked(path).open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, allow_nan=False)
        stream.write('\n')


def member(root, name):
    require(isinstance(name, str) and name and '\\' not in name and ':' not in name,
            'Invalid manifest path')
    parts = name.split('/')
    require(all(p not in ('', '.', '..') and p == p.rstrip(' .') for p in parts)
            and not PurePosixPath(name).is_absolute(), 'Unsafe manifest path')
    return unlinked(root.joinpath(*parts))


def manifest(root, expected):
    require(sha(root/'files.json') == expected, 'Changed external manifest pin')
    files = read(root/'files.json')
    require('files.json' not in files and len({n.casefold() for n in files}) == len(files),
            'Colliding manifest membership')
    require({p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}
            == set(files) | {'files.json'}, 'Changed package membership')
    for name, pin in files.items():
        require(sha(member(root, name)) == pin, 'Changed retained member: ' + name)
    return files


def module(path):
    spec = importlib.util.spec_from_file_location('retained_storage_recovery', path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def validate_contract(plan):
    require(plan['version'] == VERSION and plan['priorHandoffSha256'] == PRIOR_PIN
            and plan['storageFormat'] == 'tower-proposal-json-evidence-gzip-v1', 'Changed protocol binding')
    r = plan['resources']
    require(r == dict(envelope='tower-proposal-resource-envelope-v2', limits=LIMITS,
            timingMargin=2, publicationReserveSeconds=120, qualificationMaximumSeconds=900,
            qualificationMaximumBytes=1073741824, gateMaximumSeconds=SECONDS,
            gateMaximumBytes=BYTES, fullGateAllowanceChargedAtStart=True), 'Changed frozen resources')
    g = plan['necessaryGate']
    require(g['retainRecoveryFloors'] is True and g['physicalByteTimeDiscount'] is False
            and g['postTimingNormalization'] is False and g['numericCurrentForecast'] is None
            and g['measurementAuthorization'] is False, 'Unauthorized forecast or floor change')
    w = plan['matchedWorkload']
    require(w['executionOrder'] == ['plain-json', plan['storageFormat']] and w['attemptsPerFormat'] == 1
            and w['warmups'] == 0 and w['retries'] == 0, 'Changed prospective comparison order or attempts')
    require(plan['accounting']['phases'] == ['native', 'nativeAudit', 'independentAudit', 'publication'],
            'Missing full lifecycle phase')
    for key in ('endToEnd', 'applicationReadBytes', 'logicalJsonBytes', 'decodedBytesProcessed',
                'parseAndReconstruction', 'storage', 'scratch', 'memory', 'coverage', 'failureAndCharging'):
        require(isinstance(plan['accounting'].get(key), str) and plan['accounting'][key],
                'Missing accounting contract: ' + key)


def necessary_gate(retained, larger_floors=None):
    """An upward-only necessary bound; no measured compressed sizes enter this rule."""
    require(set(retained) == set(LIMITS), 'Missing retained partition')
    require(larger_floors is None or set(larger_floors) == set(LIMITS), 'Missing new partition')
    floors = {}
    for key in LIMITS:
        values = [retained[key]] + ([] if larger_floors is None else [larger_floors[key]])
        require(all(type(v) in (float, int) and math.isfinite(v) and v > 0 for v in values),
                'Invalid partition floor: ' + key)
        floors[key] = max(values)
    rounded = {k: math.ceil(v) for k, v in floors.items()}
    failed = [k for k in LIMITS if rounded[k] >= LIMITS[k]]
    return dict(unroundedNecessaryFloors=floors, roundedNecessaryFloors=rounded,
                exceededPartitions=failed, necessaryGatePassed=not failed)


def assess(plan, prior, recovery, proof, probe):
    validate_contract(plan)
    require(recovery == prior['resourceAssessment'], 'Changed reproduced recovery assessment')
    require(recovery['limits'] == LIMITS and recovery['timingMargin'] == 2
            and recovery['publicationReserveSeconds'] == 120, 'Changed recovery envelope')
    require(proof['completeResultsEqual'] is True and proof['fullNativeReconstructionPassed'] is True
            and proof['independentWorkingAuditPassed'] is True, 'Missing complete synthetic equivalence')
    entries = proof['encodedLogicalMembers']
    require(len(entries) == 60 and len({e['path'] for e in entries}) == 60
            and all(type(e['bytes']) is int and e['bytes'] > 0 for e in entries), 'Changed logical member proof')
    logical = sum(e['bytes'] for e in entries)
    require(proof['nativeCodecWork']['decodedBytesProcessed'] == 2*logical
            and proof['nativeCodecWork']['decodePasses'] == 120, 'Changed repeated decode work')
    gate = necessary_gate(recovery['unroundedNecessaryFloors'])
    whole = 2*(probe['wholeWorkerSeconds'] + sum(probe['independentAuditSeconds']))
    bound = recovery['frozenProbeAuditSecondsLowerBound']
    return dict(version=VERSION, status='StorageResourceNecessaryGateFailed' if not gate['necessaryGatePassed']
                else 'StorageResourceNecessaryGateOnlyNoMeasurementAuthorization', **gate, limits=LIMITS,
        probeReconciliation=dict(retainedWorkerAndIndependentAuditsWithMarginSeconds=whole,
            retainedScaledInventoriesWithMarginSeconds=bound-whole-120,
            publicationReserveSeconds=120, totalSeconds=bound,
            interpretation='Frozen planning components, not identified current compressed costs'),
        correctnessWork=dict(logicalEncodedBytes=logical, logicalMembers=60,
            nativeAuditDecodePasses=120, nativeAuditDecodedBytes=2*logical,
            minimumTwoAuditDecodedBytesUnderCurrentReaders=4*logical,
            interpretation='Exact synthetic work and reader contract only; no full owned cost observation'),
        missingEvidence={k:v for k,v in plan['readiness'].items() if k != 'exactSyntheticEquivalence'},
        capturedSyntheticExecution=proof['capturedExecution'],
        currentRuntimeQualified=False, qualifiedCurrentForecast=None, measuredStorageSavings=None,
        measurementPairMayBeRegistered=False, nativePreparationAllowed=False, qualificationAllowed=False,
        scientificAdmissionAllowed=False, productionEntropyDraws=0, newScientificReservations=0,
        actualCombat=0, newTimingSamples=0, oldRecoveryGateStillClosed=not recovery['recoveryPairMayBeRegistered'],
        originalExperimentStillFailed=True, floorDiscountApplied=False,
        historicalChargesUnchanged=True, next=plan['nextImplementation'])


def retained_assessment(root):
    require(sha(root/'protocol.json') == PLAN_PIN and sha(root/'prior-handoff.json') == PRIOR_PIN,
            'Changed protocol or prior handoff')
    prior = read(root/'prior-handoff.json')
    manifest(root/'recovery', prior['recoveryRegistrationManifestSha256'])
    # The entire retained implementation and all imported dependencies are pinned above.
    recovery_code = module(root/'recovery'/RECOVERY_HELPER)
    recovery = recovery_code.verify(root/'recovery', prior['recoveryRegistrationManifestSha256'])
    require(sha(root/'integration-files.json') == prior['verificationManifestSha256'], 'Changed integration manifest')
    require(sha(root/'logical-equivalence.json') == read(root/'integration-files.json')['logical-equivalence.json'],
            'Changed synthetic proof')
    for name, pin in prior['currentSourceHashes'].items():
        require(sha(member(root/'sources', name)) == pin, 'Changed retained current source')
    inputs = recovery_code.inputs_at(root/'recovery')
    return assess(read(root/'protocol.json'), prior, recovery, read(root/'logical-equivalence.json'), inputs['audit-probe'])


def verify(root, expected):
    manifest(root, expected)
    result = retained_assessment(root)
    require(read(root/'assessment.json') == result, 'Changed prospective resource assessment')
    declaration = read(root/'declaration.json')
    require(declaration['protocolSha256'] == PLAN_PIN and declaration['chargedSeconds'] == SECONDS
            and declaration['chargedBytes'] == BYTES and declaration['fullAllowanceChargedAtStart'] is True,
            'Changed read-only gate charge')
    require(declaration['helperSha256'] == sha(root/'helper.py'), 'Changed producing helper')
    require(read(root/'completion.json')['status'] == 'CompleteReadOnlyGateNoMeasurement', 'Incomplete gate')
    return result


def register(output):
    require(output == ROOT/REGISTRATION and not output.exists(), 'Use the single fresh registration; no overwrite or retry')
    unlinked(output)
    require(sha(ROOT/PLAN) == PLAN_PIN and sha(ROOT/PRIOR) == PRIOR_PIN, 'Changed prospective binding')
    output.mkdir()
    started = time.monotonic()
    write(output/'declaration.json', dict(version=VERSION, protocolSha256=PLAN_PIN, priorHandoffSha256=PRIOR_PIN,
        helperSha256=sha(Path(__file__)), chargedSeconds=SECONDS, chargedBytes=BYTES,
        fullAllowanceChargedAtStart=True, measurementPairStarted=False, newTimingSamples=0))
    def check():
        require(time.monotonic()-started < SECONDS and
                sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) < BYTES, 'Read-only gate allowance exceeded')
    try:
        prior = read(ROOT/PRIOR)
        for group in ('preservedHistoricalPins', 'currentSourceHashes'):
            for name, pin in prior[group].items():
                require(sha(member(ROOT, name)) == pin, 'Changed inherited evidence/source: ' + name)
                check()
        manifest(ROOT/prior['recoveryRegistration'], prior['recoveryRegistrationManifestSha256'])
        for source, target in ((ROOT/PLAN, 'protocol.json'), (ROOT/PRIOR, 'prior-handoff.json'),
                               (Path(__file__), 'helper.py'), (ROOT/PROOF, 'logical-equivalence.json'),
                               (ROOT/Path(PROOF).parent/'files.json', 'integration-files.json')):
            shutil.copyfile(source, output/target)
        shutil.copytree(ROOT/prior['recoveryRegistration'], output/'recovery')
        for name in prior['currentSourceHashes']:
            target = output/'sources'/name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT/name, target)
            check()
        result = retained_assessment(output)
        write(output/'assessment.json', result)
        check()
        for group in ('preservedHistoricalPins', 'currentSourceHashes'):
            for name, pin in prior[group].items():
                require(sha(member(ROOT, name)) == pin, 'Evidence/source changed during registration')
        write(output/'completion.json', dict(status='CompleteReadOnlyGateNoMeasurement',
            secondsBeforeSealing=time.monotonic()-started, chargedSeconds=SECONDS, chargedBytes=BYTES,
            inheritedPinsAuthenticated=len(prior['preservedHistoricalPins']), newTimingSamples=0))
        check()
    except BaseException as error:
        write(output/'failure.json', dict(reason=str(error), chargedSeconds=SECONDS, chargedBytes=BYTES,
                                        measurementPairStarted=False, newTimingSamples=0))
        raise
    finally:
        write(output/'files.json', {p.relative_to(output).as_posix():sha(p) for p in sorted(output.rglob('*')) if p.is_file()})
    return verify(output, sha(output/'files.json'))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('register', 'verify'))
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--expected-manifest-sha256')
    args = parser.parse_args()
    result = register(args.output.absolute()) if args.action == 'register' else verify(args.output.absolute(), args.expected_manifest_sha256)
    print(json.dumps(result, indent=2))
