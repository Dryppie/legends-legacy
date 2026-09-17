"""Read-only resource qualification from sealed receipts and existing report bytes.

No benchmark, native reconstruction, candidate generation, seed derivation or combat.
The conditional envelope is a sizing proposal, not an authorization or runnable request.
Writes only JSON to stdout. Stage projections never become new battle evidence.
"""
import copy
import gzip
import hashlib
import json
from pathlib import Path
import statistics
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / 'TestResults/practical-native-verification-20260917'
RUN = ROOT / 'TestResults/balance/tower-practical-native-verification-20260917'
BUILD = ROOT / 'TestResults/selection-diagnostic-build/bin/BalanceHarness/release'
FIXTURE = ROOT / 'TestResults/selection-diagnostic-checks'
MIB = 1048576
PINS = {
    PACKAGE/'files.json': '2c8415a62bdf6d72451a6468be5ba0c7ea78a10744e0688516a796690b921e15',
    RUN/'files.json': '54a3a877b209207bc66edd027396e7116e79acbae379639c513990d1a37f3db3',
    FIXTURE/'literal-resource-observation.json': '77dae5b9ab3212d76ed9774c8b98282d61ef1f43432027bdfd51018a7248845a',
    FIXTURE/'final-durability.trx': '1499d8970c50c52f2e214b24e1c7795e04fddeb68f69da7101fe6bc80ee12318',
}
SOURCES = ['TowerSelectionDiagnostic.cs', 'TowerSelectionDiagnosticRun.cs',
    'TowerSelectionDiagnosticArchive.cs', 'TowerSelectionDiagnosticReservation.cs',
    'TowerBossDiscoveryContract.cs', 'TowerBossStudyArchive.cs', 'TowerLoadoutArchive.cs',
    'TowerBattleRunner.cs', 'TowerHistoryRegistry.cs', 'TowerRefinementComparisonLaunch.cs',
    'TowerCompleteReservation.cs', 'TowerSuppliedCompositionSearch.cs', 'HarnessJson.cs']


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def size(value):
    # The retained Windows .NET writer uses CRLF. Verify this sizing convention
    # against its whole saved definition before using it for related records.
    return len(json.dumps(value, indent=2, ensure_ascii=False).replace('\n', '\r\n').encode('utf-8'))


def summary(values):
    return {'minimum': min(values), 'median': statistics.median(values),
            'maximum': max(values), 'sum': sum(values), 'mean': statistics.mean(values)}


def executable_files():
    # Mirror the current RetainExecutable asset selection without copying or loading it.
    deps = read(BUILD/'BalanceHarness.deps.json')
    paths = {'BalanceHarness.dll', 'BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json'}
    for library in deps['targets'][deps['runtimeTarget']['name']].values():
        paths.update(Path(p).name for p in library.get('runtime', {}) if not p.endswith('/_._'))
        paths.update(f"{v['locale']}/{Path(p).name}" for p, v in library.get('resources', {}).items()
                     if (BUILD/v['locale']/Path(p).name).is_file())
    paths.update(p.relative_to(BUILD).as_posix() for p in (BUILD/'runtimes').rglob('*') if p.is_file())
    return {p: {'bytes': (BUILD/p).stat().st_size, 'sha256': sha(BUILD/p)} for p in sorted(paths)}


def main():
    for path, expected in PINS.items():
        require(sha(path) == expected, 'Changed pinned evidence: '+str(path))
    inventories = {}
    for folder in (PACKAGE, RUN):
        entries = read(folder/'files.json')
        actual = {p.relative_to(folder).as_posix() for p in folder.rglob('*') if p.is_file()}
        require(actual == set(entries) | {'files.json'}, 'Changed native inventory.')
        require(all(sha(folder/p) == h for p, h in entries.items()), 'Changed native file.')
        inventories[folder.relative_to(ROOT).as_posix()] = len(entries)
    source_pins = {('LL/tools/BalanceHarness/'+p): sha(ROOT/'LL/tools/BalanceHarness'/p) for p in SOURCES}
    require('MaxTicks: 6000' in (ROOT/'LL/tools/BalanceHarness/TowerBattleRunner.cs').read_text(), 'Changed tick cap.')
    closeout = read(PACKAGE/'completion.json')
    require(closeout['scope'] == 'Closed' and closeout['totalExclusions'] == 484285, 'Changed prior scope.')
    scope = read(RUN/'study/scope.json')
    request = read(RUN/'request.json')
    current_assets = executable_files()
    assembly_identity = {n: {'retained': h, 'current': current_assets[n+'.dll']['sha256'],
                            'matches': h == current_assets[n+'.dll']['sha256']}
                         for n, h in scope['execution']['assemblyHashes'].items()}
    live_content = Path(request['contentRoot'])/'Data'
    content_matches = {p: sha(live_content/p) == h for p, h in scope['contentHashes'].items()}
    require(all(content_matches.values()), 'Current gameplay data differs; reassess model.')
    old_sources = read(PACKAGE/'source-files.json')
    changed_sources = [p for p, h in old_sources.items() if sha(ROOT/p) != h]

    fixture = read(FIXTURE/'literal-resource-observation.json')
    trx = ET.parse(FIXTURE/'final-durability.trx').getroot()
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    case = next(t for t in trx.findall('.//t:UnitTestResult', ns) if 'Complete_literal_archive_' in t.attrib['testName'])
    require(case.attrib['outcome'] == 'Passed', 'Fixture did not pass.')
    output = case.find('t:Output/t:StdOut', ns).text
    require(json.loads(output[output.index('{'):]) == fixture, 'Fixture observation differs from TRX.')
    require(fixture['actualCombat'] == fixture['productionEntropyDraws'] == 0, 'Wrong fixture scope.')

    rows = [json.loads(s) for s in (RUN/'study/trials.jsonl').read_text().splitlines()]
    require(len(rows) == 1408, 'Wrong retained fight count.')
    stages = {}
    for stage, count in [('discovery', 512), ('selection', 128), ('confirmation', 768)]:
        selected = [t for t in rows if t['stage'] == stage]
        require(len(selected) == count, 'Changed native stage counts.')
        compressed, expanded, ticks = [], [], []
        for trial in selected:
            path = RUN/'study/battles'/(trial['id']+'.json.gz')
            raw = gzip.decompress(path.read_bytes()); report = json.loads(raw)
            require(report['battle']['seed'] == trial['seed'], 'Report/trial mismatch.')
            compressed.append(path.stat().st_size); expanded.append(len(raw))
            ticks.append(report['battle']['summary']['durationTicks'])
        stages[stage] = {'count': count, 'compressedBytes': summary(compressed),
                         'expandedBytes': summary(expanded), 'simulationTicks': summary(ticks)}

    # In-memory representative serialization only; no candidate or executable binding is emitted.
    ledger = read(RUN/'seed-ledger.json')
    history = sorted(set(ledger['historical']) | set(ledger['reserved']))
    require(len(history) == 484285, 'Changed retained exclusion union.')
    original = read(RUN/'definition.json')
    require(size(original) == (RUN/'definition.json').stat().st_size, 'Serialization sizing no longer matches retained .NET JSON.')
    search = copy.deepcopy(original)
    search['excludedCombatSeeds'] = history
    search['maximumBattles'] = 4640
    for schedule in search['stages']['schedules'].values():
        schedule['confirmation'] = []; schedule['diagnostics'] = []
    template = copy.deepcopy(search); template['generation']['seeds'] = []
    for schedule in template['stages']['schedules'].values():
        schedule['discovery'] = []; schedule['selection'] = []
    binding = {'version': 'tower-practical-selection-diagnostic-v1', 'requestHash': sha(RUN/'request.json'),
               'templateHash': sha(RUN/'source-definition.json'), 'definition': search}
    definition_bytes, template_bytes, binding_bytes = size(search), size(template), size(binding)
    # Reserve maximum signed-int decimal width plus indentation/comma/newline; no new integers are generated.
    ledger_base = size({'reservationState': 'Complete', 'historical': history, 'reserved': []})
    largest_ledger = ledger_base + 18*2089 + 8
    content_bytes = sum((live_content/p).stat().st_size for p in scope['contentHashes'])
    fixed = {
        'indentedSourceTemplate': template_bytes, 'rootAndStudySearchBindings': 2*binding_bytes,
        'studyDefinition': definition_bytes, 'completeHistoricalLedgerWith41Words': ledger_base+18*41+8,
        'contentCopy': content_bytes, 'producingExecutableCopy': sum(v['bytes'] for v in current_assets.values()),
        'bossProfiles': (RUN/'study/boss-profiles.json').stat().st_size,
        'generationInputsAndMechanics': sum((RUN/'study'/p).stat().st_size for p in ('generation-inputs.json','generation-mechanics.json')),
        'otherAdmissionMetadataAllowance': 2*MIB,
    }
    admission_model = sum(fixed.values())
    max_report = max(s['compressedBytes']['maximum'] for s in stages.values())
    observed_tick_total = sum(s['simulationTicks']['sum'] for s in stages.values())
    max_tick = max(s['simulationTicks']['maximum'] for s in stages.values())
    projection = {}
    for multiplier in (1, 2, 4):
        search_bytes = 640*max_report*multiplier + 4*MIB
        confirmation_bytes = 4000*max_report*multiplier + 8*MIB + 18*(2089-41)
        audit_bytes = 4*MIB
        retained = admission_model + search_bytes + confirmation_bytes + audit_bytes
        temporary = max(binding_bytes, largest_ledger)
        projection[str(multiplier)] = {'reportSizeMultiplier': multiplier,
            'searchGrowthBytes': search_bytes, 'confirmationGrowthBytes': confirmation_bytes,
            'auditGrowthBytes': audit_bytes, 'modeledRetainedBytes': retained,
            'withOneLargestAtomicTemporaryAndCloseoutBytes': retained+temporary+4*MIB}

    native = closeout['phaseMeasurements']
    # Deliberately charge the complete old run cost to each fight-equivalent, including its old overhead.
    # The factor two is a declared stress assumption, not a confidence or worst-case bound.
    proxies = {'admission': native['admission']['measuredSeconds']+native['run']['measuredSeconds'],
               'search': 2*native['run']['measuredSeconds']*640/1408,
               'confirmation': 2*native['run']['measuredSeconds']*4000/1408+native['admission']['measuredSeconds'],
               'audit': 2*native['audit']['measuredSeconds']*4640/1408}
    limits = {'admission': {'seconds': 180, 'bytes': 96*MIB}, 'search': {'seconds': 120, 'bytes': 64*MIB},
              'confirmation': {'seconds': 660, 'bytes': 256*MIB}, 'audit': {'seconds': 360, 'bytes': 16*MIB}}
    maximum_seconds, maximum_bytes = 1380, 512*MIB
    require(sum(p['seconds'] for p in limits.values())+2 <= maximum_seconds, 'Invalid time sum.')
    require(sum(p['bytes'] for p in limits.values())+4*MIB <= maximum_bytes, 'Invalid byte sum.')
    require(all(proxies[p] < limits[p]['seconds'] for p in limits), 'Insufficient declared time proxy margin.')
    require(admission_model < limits['admission']['bytes'], 'Admission sizing exceeds proposed cap.')
    require(projection['2']['searchGrowthBytes'] < limits['search']['bytes']
            and projection['2']['confirmationGrowthBytes'] < limits['confirmation']['bytes'], 'Stress byte sizing exceeds phase cap.')
    require(projection['1']['withOneLargestAtomicTemporaryAndCloseoutBytes'] < 256*MIB, 'Saved-report probe sizing exceeds proposed cap.')
    result = {
        'version': 'practical-selection-diagnostic-resource-qualification-v1',
        'status': 'ConditionalEnvelopeSpecified_NativeResourceQualificationMissing', 'launchDecision': 'NoGo',
        'runnableRequest': False, 'grantedAllowance': False, 'newFights': 0, 'newValues': 0, 'benchmarksRun': 0,
        'verifiedManifestEntries': inventories,
        'compatibility': {'currentContentMatches': content_matches, 'assemblyIdentity': assembly_identity,
            'retainedSourceCount': len(old_sources), 'changedRetainedSourcePaths': changed_sources,
            'unchangedRetainedSourceCount': len(old_sources)-len(changed_sources),
            'warning': 'Assembly inequality does not prove gameplay changed; exact producing-runtime parity is unestablished.'},
        'nativeObserved': {'phaseMeasurements': native, 'stages': stages, 'perFightWallSecondsAvailable': False},
        'literalObserved': fixture,
        'nativeReadWork': {'stateMachineReports': 4640, 'independentConfirmationReports': 4000,
            'totalReportDeserializations': 8640, 'fullStudyInventoryPasses': 2,
            'stageWeightedExpandedBytesRead': int(stages['discovery']['expandedBytes']['sum']+stages['selection']['expandedBytes']['sum']
                                                   +8000*stages['confirmation']['expandedBytes']['mean']),
            'isProjectionNotMeasurement': True},
        'admissionByteModel': {'components': fixed, 'totalBytes': admission_model,
            'largestBindingBytes': binding_bytes, 'maximumLedgerBytes': largest_ledger,
            'serializationMatchesRetainedDefinitionExactly': True,
            'oldAdmission32MiBInsufficientForThisModel': admission_model > 32*MIB,
            'assumptions': 'Same identifiers/cohort/gear and retained 484285-value history; live reconciliation pending; indented template; current dependency assets; 2 MiB metadata allowance.'},
        'currentExecutableAssets': current_assets,
        'conditionalStorageScenarios': projection,
        'conditionalTimeProxiesSeconds': proxies,
        'simulationDurationSensitivity': {'nativeTotalTicks': observed_tick_total, 'nativeMaximumTicks': max_tick,
            'engineMaximumTicksPerBattle': 6000, 'fullDiagnosticMaximumTicks': 4640*6000,
            'fullCapToNativeTickTotalRatio': 4640*6000/observed_tick_total,
            'allFutureCapToNativeMeanTickRatio': 6000/(observed_tick_total/1408),
            'wallTimeBound': False, 'reason': 'Tick work and event complexity are not constant; simulation seconds are not wall seconds.'},
        'candidateEnvelope': {'maximumSeconds': maximum_seconds, 'maximumBytes': maximum_bytes, 'phases': limits,
            'closeoutSeconds': 2, 'closeoutBytes': 4*MIB,
            'headroomForPriorSecondsAndSetup': maximum_seconds-2-sum(p['seconds'] for p in limits.values()),
            'headroomBeyondPhaseBytesAndCloseout': maximum_bytes-4*MIB-sum(p['bytes'] for p in limits.values()),
            'resourceFeasibilityEstablished': False, 'completionProbabilityEstablished': False,
            'basis': 'Rounded above explicit 2x aggregate-time proxies and 2x observed maximum report-size scenario; margins are assumptions.'},
        'nextObservation': {'kind': 'Bounded zero-combat resource probe on current producing runtime',
            'proposedMaximumSeconds': 180, 'proposedMaximumBytes': 256*MIB, 'attempts': 1, 'allowanceGranted': False,
            'requiredWork': ['Complete live-history admission without allocation', 'Content/executable copy and full-size metadata writes',
                '4640 retained report occurrences with native preparation/serialization and two audit read workloads',
                'Per-phase elapsed time, sampled storage high-water and final full-run elapsed time'],
            'forbiddenConclusions': ['Repeated reports are independent evidence', 'Probe is a valid completed diagnostic',
                'Probe establishes future combat cost or operational completion probability'],
            'stillNeededAfterProbe': 'Compatible combat-time evidence or explicit acceptance of capped operational-failure risk; freeze exact request/runtime/history and prior costs.'},
        'inputPins': {p.relative_to(ROOT).as_posix(): h for p,h in PINS.items()},
        'sourcePins': source_pins, 'analysisSourceSha256': sha(Path(__file__)),
    }
    require(all(sha(ROOT/p) == h for p,h in source_pins.items()), 'Source changed during analysis.')
    require(all(sha(p) == h for p,h in PINS.items()), 'Evidence changed during analysis.')
    require(all(sha(BUILD/p) == v['sha256'] for p,v in current_assets.items()), 'Executable changed during analysis.')
    require(all(sha(live_content/p) == h for p,h in scope['contentHashes'].items()), 'Content changed during analysis.')
    print(json.dumps(result, indent=2, allow_nan=False))


if __name__ == '__main__':
    main()
