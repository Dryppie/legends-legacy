"""Authenticate the completed resource-only probe; print a deterministic JSON review.

Reads saved bytes only. No benchmark, process launch, combat, allocation or replay.
The report qualifies the observed workload, not a complete diagnostic or future run.
"""
import collections
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PROBE = ROOT / 'TestResults/selection-diagnostic-resource-probe-20260917-02'
CHECKS = ROOT / 'TestResults/selection-diagnostic-probe-02-checks'
NATIVE = ROOT / 'TestResults/balance/tower-practical-native-verification-20260917'
MIB = 1048576
PINS = {
    CHECKS/'scope.json': '316d119f952e24db342eefb3fa9d22605621f56d3239689e451e9578aa4e7b80',
    PROBE/'resource-receipt.json': '84169a46a7b3bfebcfe4860f1e7c3c120e738b85f20a0fcdc89a77b3fc6eff27',
    CHECKS/'native-outcome.json': '45cc25c04d4bed29f49ecf59ba69c2e9fa71f139179616a64a5d17f09c707043',
    CHECKS/'native-launch-observation.json': '6c90b73e520f44a2e906809a5c3fc530d4a2237d9b2c0d3888561ac02fcb6731',
}


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def inventory(root, name, exceptions=()):
    entries = read(root/name)
    actual = {p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}
    require(actual == set(entries) | {name} | set(exceptions), 'Changed exact inventory: '+str(root))
    for relative, digest in entries.items():
        target = (root/relative).resolve()
        require(target.is_relative_to(root.resolve()) and sha(target) == digest, 'Changed artifact: '+relative)
    return entries


def main():
    for path, digest in PINS.items():
        require(sha(path) == digest, 'Changed pinned observation: '+str(path))
    scope = read(CHECKS/'scope.json')
    require(scope['attemptsAllowed'] == 1 and scope['maximumSeconds'] == 180
            and scope['maximumBytes'] == 256*MIB and scope['retriesAllowed'] == 0, 'Changed scope')
    for name, digest in scope['sourceAndBuildPins'].items():
        require(sha(ROOT/name) == digest, 'Changed producing source/build: '+name)
    old = scope['previousClosedScope']
    require(sha(ROOT/old['record']) == old['sha256'], 'Changed first-scope record')
    first = read(ROOT/old['record'])
    require(sha(ROOT/first['verificationInventory']) == first['verificationInventorySha256'], 'Changed first-scope inventory')
    for name, digest in read(ROOT/first['verificationInventory']).items():
        require(sha(ROOT/name) == digest, 'Changed first-scope evidence: '+name)
    receipt = read(PROBE/'resource-receipt.json')
    require(receipt['status'] == 'ResourceWorkloadMeasured' and receipt['diagnosticDecision'] == 'NotApplicable', 'Not a resource receipt')
    require(sha(PROBE/'probe-files.json') == receipt['payloadManifestHash'], 'Changed probe manifest')
    files = inventory(PROBE, 'probe-files.json', ('resource-receipt.json',))
    payload = inventory(PROBE/'payload', 'resource-files.json')
    require(not any(Path(n).name in ('seed-ledger.json','prior-seed-ledger.json','history-input.json','probe-failure.json','worker-failure.json') for n in files),
            'Reservation or failure artifact in completed probe')
    worker = read(PROBE/'worker-observation.json')
    require(worker == receipt['worker'], 'Changed worker observation')
    for key in ('newFights','newReservations','entropyCalls'):
        require(worker[key] == receipt[key] == 0, 'Non-resource activity')
    protocol = read(PROBE/'protocol.json')
    source_pins = read(PROBE/'probe-source-pins.json')
    require(source_pins['execution'] == protocol['execution'], 'Changed producing identity')
    for name, digest in source_pins['sourcePins'].items():
        require(sha(ROOT/name) == digest, 'Changed probe source: '+name)
    verified_native = {}
    for name, digest in source_pins['pins'].items():
        path = Path(name)
        require(sha(path) == digest, 'Changed retained manifest')
        verified_native[path.parent.relative_to(ROOT).as_posix()] = len(inventory(path.parent,'files.json'))
    require(sum(verified_native.values()) == 1617, 'Changed retained inventory sizes')
    history_files = protocol['historyFiles']
    require(len(history_files) == 202 and protocol['historicalValues'] == 484285, 'Changed admitted history')
    for name, digest in history_files.items():
        require(sha(Path(name)) == digest, 'Changed admitted ledger: '+name)
    history = read(PROBE/'resource-history-payload.json')['payload']
    values = history['historical']
    require(len(values) == 484285 and values == sorted(set(values))
            and values == history['excluded'] == history['ledgerCopy'], 'Changed full-history serialization')
    labels = read(PROBE/'resource-existing-labels.json')['payload']['labels']
    require(len(set(labels)) == len(labels) == 1000 and set(labels) <= set(values), 'Labels are not existing exclusions')
    native_trials = [json.loads(line) for line in (NATIVE/'study/trials.jsonl').read_text().splitlines()]
    by_id = {t['id']: t for t in native_trials}
    confirms = [t for t in native_trials if t['stage'] == 'confirmation']
    plan = [t for t in native_trials if t['stage'] == 'discovery'] + [t for t in native_trials if t['stage'] == 'selection']
    plan += [confirms[i % len(confirms)] for i in range(4000)]
    rows = read(PROBE/'payload/resource-occurrences.json')['payload']
    counts = dict(collections.Counter(row['stage'] for row in rows))
    require(len(rows) == len(plan) == 4640 and counts == {'discovery':512,'selection':128,'confirmation':4000}, 'Changed workload counts')
    source_files = read(NATIVE/'study/files.json')
    for ordinal, (row, planned) in enumerate(zip(rows,plan),1):
        source = by_id[row['sourceId']]
        require(source == planned and row['id'] == f'trial-{ordinal:06d}' and row['stage'] == source['stage']
                and row['recipe'] == source['recipe'] and row['historicalLabel'] == source['seed'], 'Changed report repetition plan')
        require(payload['battles/'+row['id']+'.json.gz'] == source_files['battles/'+source['id']+'.json.gz'], 'Copied report bytes differ')
    details = worker['details']
    require(details['nativeAuditWorkloadReads'] == 4640 and details['independentAuditWorkloadReads'] == 4000
            and details['inputMaterializations'] == 9280 and not details['newEvidence'], 'Changed workload receipts')
    materialize = [t for t in worker['timings'] if t['path'] == 'input.materialize']
    require(len(materialize) == 1 and materialize[0]['calls'] == 9280, 'Native input trace count differs')
    outcome = read(CHECKS/'native-outcome.json'); external = read(CHECKS/'native-launch-observation.json')
    final_bytes = sum(p.stat().st_size for p in PROBE.rglob('*') if p.is_file())
    require(outcome['completed'] and external['exitCode'] == 0 and external['attempts'] == 1
            and outcome['secondsAfterPublication'] <= external['externalElapsedSeconds'] < 180
            and final_bytes == outcome['finalBytes'] <= outcome['sampledHighWaterBytes'] < 256*MIB, 'Probe exceeded scope or changed')
    ns = {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    tests = ET.parse(CHECKS/'pre-probe.trx').findall('.//t:UnitTestResult',ns)
    require(len(tests) == 14 and all(t.attrib['outcome'] == 'Passed' for t in tests), 'Probe tests did not pass')
    phases = [{**p, 'growthBytes':p['endBytes']-p['startBytes'], 'growthMiB':(p['endBytes']-p['startBytes'])/MIB} for p in worker['phases']]
    require([p['name'] for p in phases] == ['admission','search','confirmation','audit'], 'Changed phase order')
    step_seconds = details['steps']
    result = {
        'version':'practical-selection-resource-probe-02-review-v1', 'status':'ResourceWorkloadMeasured',
        'gameplayLaunchDecision':'NoGo', 'observedNonCombatWorkloadQualified':True,
        'completeDiagnosticAuditsExecuted':False, 'combatFeasibilityEstablished':False,
        'attemptsInThisScope':1, 'retries':0, 'chargedSeconds':180, 'chargedBytes':256*MIB,
        'combinedClosedProbeScopeCharges':scope['combinedResourceScopeCharges'],
        'externalElapsedSeconds':external['externalElapsedSeconds'], 'parentElapsedSeconds':outcome['secondsAfterPublication'],
        'finalBytes':final_bytes, 'finalMiB':final_bytes/MIB, 'sampledHighWaterBytes':outcome['sampledHighWaterBytes'],
        'phases':phases, 'otherParentOverheadSeconds':outcome['secondsAfterPublication']-sum(p['seconds'] for p in phases),
        'historyAdmissionAndRecheckSeconds':sum(v for k,v in step_seconds.items() if k.startswith('history-') and k != 'history-payload-serialization'),
        'stepSeconds':step_seconds, 'historicalValues':484285, 'historicalFiles':202, 'reportWrites':counts,
        'totalReportWrites':4640, 'byteIdenticalToRetainedReports':4640, 'inputMaterializations':9280,
        'auditWorkloadReads':{'native':4640,'independent':4000}, 'inventoryHashPassesInAuditWorkload':2,
        'runtimeAssets':len(read(PROBE/'runtime-assets.json')),
        'retainedRuntimeBytes':sum(p.stat().st_size for p in (PROBE/'payload/executable').rglob('*') if p.is_file()),
        'newFights':0, 'newReservations':0, 'entropyCalls':0, 'newCandidates':0,
        'verifiedNativeInventoryEntries':verified_native, 'verifiedProbeManifestEntries':len(files),
        'verifiedPayloadManifestEntries':len(payload), 'passedProbeTests':14,
        'evidencePins':{p.relative_to(ROOT).as_posix():h for p,h in PINS.items()},
        'probeManifestSha256':receipt['payloadManifestHash'], 'producingExecution':protocol['execution'],
        'limitations':protocol['limitations'] + ['One observation is not a worst-case timing/storage bound or a completion probability.',
            'The wrapped metadata payload differs from the diagnostic archive; phase comparisons are conditional.',
            'The earlier failed scope remains charged and preserved. Neither closed probe grants gameplay resources.'],
        'nextStep':'Freeze a prospective gameplay admission package, including prior accounting and an explicit decision on unmeasured combat completion risk.'
    }
    print(json.dumps(result,indent=2))


if __name__ == '__main__':
    main()
