"""One 900-second/1-GiB runtime qualification. Does not admit or launch a study.

Resource reconciliation (including the full plain workload and completed matched
cost pair) remains a separate dependency. This deliberately cannot create an
admission receipt, scientific request, reservation, or qualified cost forecast.
"""
import argparse
import copy
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).parent
PRIOR = ROOT/'TestResults/loadout-placement-publication-inventory-handoff-20260925.json'
PRIOR_PIN = 'ae9cdf87a4db3ba4fb41d4636c4527960f27259153e50005649c8353b3ff3ba3'
AMENDMENT = ROOT/'Balance Harness/Tower-Loadout-Placement-Publication-Inventory-Amendment.json'
PLAN = ROOT/'Balance Harness/Tower-Loadout-Placement-Comparison-Plan.json'
BUILD = ROOT/'.artifacts/loadout-runtime-compatible-20260925/bin/BalanceHarness/release'
PACKAGE = ROOT/'TestResults/loadout-placement-runtime-qualification-compatible-helper-20260925'
DECLARATION = ROOT/'Balance Harness/Tower-Loadout-Placement-Runtime-Qualification-Compatible-Helper-Declaration.json'
CHECKS = ROOT/'TestResults/loadout-placement-runtime-compatibility-checks-20260925'
FAILED = ROOT/'TestResults/loadout-placement-runtime-qualification-20260925'
FAILED_PIN = '523eb2463fe40685612f9f3324bc9d567b5bf1bdc394106a0c9cd8c592865b12'
HELPER_FAILED = ROOT/'TestResults/loadout-placement-runtime-qualification-compatible-20260925'
HELPER_FAILED_PIN = '1abca6c07c3d5964806140aec728830be048b157bd9c54c1a4194dd262799957'
COMPATIBILITY_FILES = tuple('LL/tools/BalanceHarness/'+n+'.cs' for n in (
    'TowerContentProviders','OfflineContent','TowerBattleRunner','TowerBenchmark','TowerBossDiscoveryContract',
    'TowerBossInventory','EssenceMechanicsInventory')) + ('LL/tests/EssenceSystem.Tests/BalanceHarnessContentAccountingTests.cs',)
CAPTURE = ROOT/'TestResults/affinity-allied-action-admission-20260924'
PREVIEW = ROOT/'TestResults/loadout-placement-native-preview-20260924-v4'
PINS = {CAPTURE: 'c8eb52d39b57fc385a5e88330a8ec621908a3345ef3d12c33360cbdcb361e68d',
        PREVIEW: 'd677fb08736797adcc88704793332a3c77e1c8f046902e7ae1eb353f78e713d0'}
SECONDS, BYTES = 900, 1073741824
VERSION = 'tower-loadout-placement-runtime-qualification-v3'
COUNTS = dict(legacyRecipeInstances=568, completePlacements=238, references=3,
              savedPlacementInstances=240, recipeInstances=1049, preparationsPerRuntime=2098)


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


old = module('placement_runtime_basis', HERE/'prepare-affinity-allied-action-admission.py')
base, require, read, sha, save, inventory = old.base, old.require, old.read, old.sha, old.save, old.inventory


def probes(legacy, requests, exports):
    require(len(legacy) == 568 and len(requests) == len(exports) == 12, 'Incomplete saved coverage')
    rows = copy.deepcopy(legacy)
    seeds = rows[0]['seeds']
    require(len(seeds) == 2 and len(set(seeds)) == 2 and all(r['seeds'] == seeds for r in rows), 'Changed historical probe values')
    context = requests[0]['context']; scope = context['scope']
    require(set(seeds) <= base.legacy_values(context), 'Probe value is not historical')
    anchor = next(r['scenario'] for r in scope['references'] if r['id'] == context['benchmarkReferenceId'])
    def placement_arm(export):
        arms = [a for a in export['arms'] if a['name'] == 'benchmark-subgroup-loadout-placement-v6']
        require(len(arms) == 1 and arms[0]['status'] == 'Complete' and len(arms[0]['teams']) == 20, 'Incomplete placement preview')
        return arms[0]
    catalogue = placement_arm(exports[0])['loadoutPlacementCatalogue']
    require(len(catalogue['recipes']) == len({r['party']['id'] for r in catalogue['recipes']}) == 238, 'Incomplete or duplicated catalogue')
    def add(scenario):
        scenario = copy.deepcopy(scenario); scenario['seeds'] = seeds[:]
        rows.append(dict(scenario=scenario, seeds=seeds[:]))
    for recipe in catalogue['recipes']:
        scenario = copy.deepcopy(anchor)
        for actor in scenario['party']:
            actor['build']['essenceIds'] = recipe['party']['builds'][str(actor['partySlot'])]
        add(scenario)
    require(len(scope['references']) == 3, 'Changed fixed references')
    for reference in scope['references']: add(reference['scenario'])
    for export in exports:
        arm = placement_arm(export)
        require(arm['loadoutPlacementCatalogue']['recipes'] == catalogue['recipes'], 'Saved placement family changed across roots')
        for team in arm['teams']: add(team['scenario'])
    require(len(rows) == COUNTS['recipeInstances'], 'Incomplete qualification probes')
    return rows


def qualify(baseline, candidate, before, after, captured, context, harness):
    a, b = baseline['execution'], candidate['execution']
    require(a == captured['execution'] and baseline['executionHash'] == captured['executionHash'], 'Changed baseline runtime')
    require({k:v for k,v in a.items() if k != 'assemblyHashes'} == {k:v for k,v in b.items() if k != 'assemblyHashes'}, 'Changed platform')
    require({k:v for k,v in a['assemblyHashes'].items() if k != 'BalanceHarness'} ==
            {k:v for k,v in b['assemblyHashes'].items() if k != 'BalanceHarness'} and b['assemblyHashes']['BalanceHarness'] == harness,
            'Changed gameplay dependencies')
    require(before == after and len(before) == COUNTS['preparationsPerRuntime']
            and [r['ordinal'] for r in before] == list(range(1, len(before)+1)), 'Changed or incomplete materializations/preparations')
    for item in (baseline, candidate):
        require(item['materializations'] == item['nativePreparations'] == len(before) and item['fights'] == item['newValues'] == 0
                and item['settingsHash'] == context['scope']['settingsHash'], 'Changed settings or incomplete preparation')
        require(all(len(item[k]) == len(before) and all(type(v) in (int,float) and math.isfinite(v) and v >= 0 for v in item[k])
                    for k in ('materializationSeconds','preparationSeconds')), 'Missing finite preparation observations')
    require(candidate['compiledSourceDocuments'] > 0 and candidate['replayedArms'] == 4 and candidate['preservingReplayedArms'] == 2
            and candidate['alliedReplayedRoots'] == 12 and candidate['alliedReplayedArms'] == 24, 'Missing producing symbols or legacy replay')


def validate_declaration(d):
    require(d['version'] == VERSION and d['package'] == PACKAGE.relative_to(ROOT).as_posix()
            and d['chargedSeconds'] == SECONDS and d['chargedBytes'] == BYTES and d['coverage'] == COUNTS
            and d['attempts'] == 1 and d['retries'] == d['scientificLaunches'] == 0
            and d['resourceAdmission'] is False, 'Changed qualification contract')
    require(sha(PRIOR) == PRIOR_PIN, 'Changed preceding handoff')
    require(sha(FAILED/'files.json') == FAILED_PIN and d['failedQualificationManifestSha256'] == FAILED_PIN
            and sha(HELPER_FAILED/'files.json') == HELPER_FAILED_PIN and d['failedHelperManifestSha256'] == HELPER_FAILED_PIN
            and d['retainedFailedChargeSeconds'] == 2*SECONDS and d['retainedFailedChargeBytes'] == 2*BYTES, 'Lost failed qualification')
    prior = read(PRIOR)
    for name, pin in d['sourcePins'].items(): require(sha(ROOT/name) == pin, 'Changed registered input: '+name)
    require(d['harnessFiles'] == {n:sha(BUILD/n) for n in base.HARNESS_FILES}, 'Changed tested runtime')
    for name,pin in prior['isolatedNativeRuntimeHashes'].items(): require(sha(ROOT/name) == pin, 'Changed retained native build')
    require(d['precedingCharges'] == {k:prior[k] for k in d['precedingCharges']}, 'Lost preceding charges')
    require(read(AMENDMENT)['assessment']['qualificationMayBeRegistered'] is True, 'Necessary planning gate is closed')
    base.validate_tests(CHECKS/'backend.trx',d['backendTestsPassed'])
    require(d['backendTestsPassed'] > 0 and d['pythonTestsPassed'] == 21
            and 'Ran 21 tests in ' in (CHECKS/'python-helper-final.log').read_text(encoding='utf-8-sig')
            and '\nOK' in (CHECKS/'python-helper-final.log').read_text(encoding='utf-8-sig'), 'Missing passing qualification tests')


def register():
    require(not DECLARATION.exists() and not PACKAGE.exists(), 'Qualification is already registered or started')
    require(sha(PRIOR) == PRIOR_PIN, 'Changed preceding handoff')
    prior = read(PRIOR)
    pins = dict(prior['currentSourceHashes'])
    sources = [Path(__file__), HERE/'loadout-placement-runtime-context.ps1', HERE/'affinity-allied-action-admission-context.ps1',
               HERE/'prepare-affinity-allied-action-admission.py', HERE/'prepare-proposal-affinity-resource-admission.py',
               HERE/'prepare-proposal-affinity-admission.py', HERE/'test-loadout-placement-runtime-qualification.py',
               AMENDMENT, PLAN, CHECKS/'backend.trx', CHECKS/'python-helper-final.log', FAILED/'files.json', FAILED/'failure.json',
               HELPER_FAILED/'files.json',HELPER_FAILED/'failure.json']
    sources += [ROOT/n for n in COMPATIBILITY_FILES]
    for source in sources: pins[source.relative_to(ROOT).as_posix()] = sha(source)
    for root,pin in PINS.items():
        require(sha(root/'files.json') == pin, 'Changed external source manifest')
        pins[(root/'files.json').relative_to(ROOT).as_posix()] = pin
    d = dict(version=VERSION, package=PACKAGE.relative_to(ROOT).as_posix(), chargedSeconds=SECONDS, chargedBytes=BYTES,
        coverage=COUNTS, attempts=1, retries=0, scientificLaunches=0, resourceAdmission=False,
        failedQualificationManifestSha256=FAILED_PIN,failedHelperManifestSha256=HELPER_FAILED_PIN,
        retainedFailedChargeSeconds=2*SECONDS,retainedFailedChargeBytes=2*BYTES,
        correction='One separately registered attempt with the fixed provider ABI and corrected PowerShell exception handler. V2 stopped before either preparation process because the helper assigned the reserved Error variable. Preserve v1 and v2 with their full charges; do not select a faster timing sample.',
        backendTestsPassed=len(base.ET.parse(CHECKS/'backend.trx').findall('.//{*}UnitTestResult')),pythonTestsPassed=21,
        sourcePins=pins, harnessFiles={n:sha(BUILD/n) for n in base.HARNESS_FILES},
        precedingCharges={k:prior[k] for k in ('cumulativeRecordedSeconds','cumulativeRecordedBytes',
                            'cumulativeDeclaredMaximumSeconds','cumulativeDeclaredMaximumBytes')},
        scope='Guarded runtime equivalence and deterministic replay only. Full allowance charged at start on every outcome.',
        pending=['Complete plain maximum-workload coverage', 'Completed matched cost pair under prospective registration',
                 'Both independent audits and publication costs', 'Qualified current forecast and fresh scientific admission'],
        measurements='One preparation sample per baseline/candidate; retain all values. No cost ratio or admission inferred from these timings.')
    validate_declaration(d); save(DECLARATION,d)
    print(json.dumps(dict(declaration=str(DECLARATION), sha256=sha(DECLARATION))))


def run(pin):
    require(os.name == 'nt' and sha(DECLARATION) == pin, 'Use the externally pinned Windows declaration')
    d = read(DECLARATION); validate_declaration(d)
    require(not PACKAGE.exists(), 'No overwrite or timing retry')
    PACKAGE.mkdir(); started = time.monotonic()
    save(PACKAGE/'charge.json', dict(version=VERSION, chargedSeconds=SECONDS, chargedBytes=BYTES,
         precedingCharges=d['precedingCharges'], retainedFailedChargeSeconds=2*SECONDS,retainedFailedChargeBytes=2*BYTES,
         fullAllowanceChargedOnEveryOutcome=True, scientificLaunches=0))
    timer = threading.Timer(SECONDS, lambda: os._exit(124)); timer.daemon=True; timer.start()
    process = module('placement_runtime_process', ROOT/'build/bounded_windows_process.py')
    def check():
        require(time.monotonic()-started < SECONDS-5, 'Qualification deadline exhausted')
        require(old.owner.storage_bytes(PACKAGE) < BYTES-4*1048576, 'Qualification storage exhausted')
    evidence = {}
    try:
        evidence = {p:old.Evidence(p,v) for p,v in PINS.items()}
        def retain(root, name, dest=None):
            source=evidence[root].get(name); target=PACKAGE/(dest or name)
            target.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(source,target)
            return read(target) if target.suffix == '.json' else target
        for prefix in ('runtime/','content/'):
            for name in evidence[CAPTURE].files:
                if name.startswith(prefix):
                    retain(CAPTURE,name)
                    if prefix == 'runtime/': retain(CAPTURE,name,'baseline-'+name)
        for n in base.HARNESS_FILES: shutil.copyfile(BUILD/n,PACKAGE/'runtime'/n)
        runtime=inventory(PACKAGE/'runtime'); base.validate_runtime(runtime,evidence[CAPTURE].files,d['harnessFiles'],d['harnessFiles']['BalanceHarness.dll'])
        save(PACKAGE/'runtime.json',runtime)
        legacy=retain(CAPTURE,'probe-scenarios.json','legacy-probes.json')
        captured=retain(CAPTURE,'candidate-qualification.json','captured-qualification.json')
        for name in evidence[CAPTURE].files:
            if name.startswith(('allied-preview/','preview-','preserving-')) or name == 'settings.json': retain(CAPTURE,name)
        requests=[]; exports=[]
        for number in range(1,13):
            stem=f'root-{number:02d}/'
            requests.append(retain(PREVIEW,stem+'request.json','placement-preview/'+stem+'request.json'))
            exports.append(retain(PREVIEW,stem+'batches.json','placement-preview/'+stem+'batches.json'))
        save(PACKAGE/'probe-scenarios.json',probes(legacy,requests,exports))
        for source,target in [(DECLARATION,'declaration.json'),(PLAN,'prospective-plan.json'),
                (HERE/'affinity-allied-action-admission-context.ps1','legacy-context.ps1'),
                (HERE/'loadout-placement-runtime-context.ps1','placement-context.ps1'),
                (Path(__file__),'qualify-loadout-placement-runtime.py'),
                (HERE/'test-loadout-placement-runtime-qualification.py','test-loadout-placement-runtime-qualification.py'),
                (CHECKS/'backend.trx','backend.trx'),(CHECKS/'python-helper-final.log','python.log')]: shutil.copyfile(source,PACKAGE/target)
        host=Path(shutil.which('pwsh')).with_suffix('.dll')
        def command(name, mode=None):
            runtime_dir='baseline-runtime' if mode == 'baseline' else 'runtime'
            helper='legacy-context.ps1' if mode else 'placement-context.ps1'
            cmd=['dotnet','exec','--runtimeconfig',str(PACKAGE/runtime_dir/'BalanceHarness.runtimeconfig.json'),
                 '--depsfile',str(host.with_suffix('.deps.json')),str(host),'-NoProfile','-File',str(PACKAGE/helper),
                 '-Package',str(PACKAGE),'-RepositoryRoot',str(ROOT)]
            if mode: cmd += ['-Mode',mode]
            result=process.run(cmd,str(ROOT),str(PACKAGE/(name+'.log')),started+SECONDS-5,cleanup_seconds=1,check=check,log_byte_limit=1048576)
            save(PACKAGE/(name+'-process.json'),result); base.process_ok(result); check()
        command('placement')  # Validate every catalogue probe before preparing any participant.
        command('baseline','baseline'); command('candidate','candidate')
        baseline=read(PACKAGE/'baseline-qualification.json'); candidate=read(PACKAGE/'candidate-qualification.json')
        qualify(baseline,candidate,read(PACKAGE/'baseline-projections.json'),read(PACKAGE/'candidate-projections.json'),
                captured,requests[0]['context'],d['harnessFiles']['BalanceHarness.dll'])
        command('retention','retention')
        old.resource.validate_retention(read(PACKAGE/'retention-qualification.json'),runtime,inventory(PACKAGE/'retention-check/executable'))
        plan=read(PACKAGE/'runtime-plan.json'); original=read(PLAN)
        require({k:v for k,v in plan.items() if k != 'scopeHash'} == {k:v for k,v in original.items() if k != 'scopeHash'}, 'Scientific design changed')
        expected_context=copy.deepcopy(requests[0]['context']); expected_context['scope']['executionHash']=candidate['executionHash']
        require(read(PACKAGE/'runtime-context.json') == expected_context, 'Physical context changed during runtime binding')
        placement=read(PACKAGE/'placement-qualification.json')
        require(placement['placementRecipes'] == placement['nativeScenarioChecks'] == 238 and placement['roots'] == 12
                and placement['replayedArms'] == 36 and placement['capturedAccountingBlockedBeforeIO'] is True
                and all(any(n in m for m in placement['jitResolvedMethods']) for n in
                ('TowerLoadoutPlacement.Create','TowerLoadoutPlacement.Scenario','TowerProposalComparison.CreateLoadoutPlacementPlan','TowerProposalComparison.BindLoadoutPlacement')), 'Incomplete placement replay')
        for e in evidence.values(): e.recheck()
        require(inventory(PACKAGE/'runtime') == runtime and inventory(PACKAGE/'source') == read(PACKAGE/'compiled-source-files.json'), 'Retained runtime/source changed')
        for directory,prefix in [('baseline-runtime','runtime/'),('content','content/')]:
            require(inventory(PACKAGE/directory) == {n[len(prefix):]:p for n,p in evidence[CAPTURE].files.items() if n.startswith(prefix)}, 'Captured bytes changed')
        validate_declaration(d); check()
        save(PACKAGE/'consumed-evidence.json',{str(p.relative_to(ROOT)):e.used for p,e in evidence.items()})
        save(PACKAGE/'completion.json',dict(version=VERSION,status='RuntimeEquivalentResourceAdmissionPending',
             runtimeQualified=True, resourceAdmission=False, scientificAdmitted=False, qualifiedCurrentForecast=None,
             secondsBeforeSealing=time.monotonic()-started, bytesBeforeSealing=old.owner.storage_bytes(PACKAGE),
             chargedSeconds=SECONDS,chargedBytes=BYTES,coverage=COUNTS,nativePreparations=2*COUNTS['preparationsPerRuntime'],
             executionHash=candidate['executionHash'],compiledSourceDocuments=candidate['compiledSourceDocuments'],
             fights=0,newValues=0,scientificLaunches=0,pending=d['pending']))
    except BaseException as error:
        save(PACKAGE/'failure.json',dict(version=VERSION,status='RuntimeQualificationFailed',reason=str(error),
             seconds=time.monotonic()-started,chargedSeconds=SECONDS,chargedBytes=BYTES,resourceAdmission=False,scientificLaunches=0))
        raise
    finally:
        # Preserve a failed first attempt too. No existing archive is ever reopened.
        try:
            save(PACKAGE/'files.json',inventory(PACKAGE))
        finally: timer.cancel()
    print(json.dumps(dict(status='RuntimeEquivalentResourceAdmissionPending',manifestSha256=sha(PACKAGE/'files.json'),
                         seconds=time.monotonic()-started,retainedBytes=old.owner.storage_bytes(PACKAGE))))


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action',choices=['register','run']); parser.add_argument('--pin')
    args=parser.parse_args()
    if args.action == 'register': register()
    else: run(args.pin)
