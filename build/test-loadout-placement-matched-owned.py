"""One prospective pair of matched literal cost fixtures; never scientific admission.

Both packages are prepared and compared before either owner starts. There is no
retry path. All original placement costs remain upward-only phase floors.
"""
import argparse
import importlib.util
import math
from pathlib import Path
import shutil
import time
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[1]
PRIOR = 'TestResults/loadout-placement-resource-precheck-handoff-20260924.json'
PRIOR_PIN = 'd4db708723b048a832336c1c5a4825b0ba0b273061d3321218fce7c1955a9a15'
PLAN = 'Balance Harness/Tower-Loadout-Placement-Matched-Fixture-Plan.json'
PLAN_PIN = '059e00c200bb1e8322693f75c04c3219d9b4c3438acffa7b9ed2445064687bcd'
PRECHECK = 'TestResults/loadout-placement-resource-precheck-20260924-v2'
FIRST_PAIR = 'TestResults/loadout-placement-matched-owned-20260924'
VERSION = 'tower-loadout-placement-matched-cost-fixtures-v1'
PROFILES = ('matched-placement-baseline-v1', 'matched-placement-candidate-v1')
FILES = ('context.json', 'settings.json', 'history.json', 'runtime.json', 'matched-contract.json',
         'run-proposal-affinity-study.py', 'bounded_windows_process.py', 'auditor.py')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


pre = module('placement_precheck', ROOT/'Balance Harness/analysis/loadout-placement-resource-precheck.py')
read, write, sha, require = pre.read, pre.write, pre.sha, pre.require


def phase_costs(completion, closeout):
    pre.validate_costs(completion, completion['version'])
    require(closeout['version'] == completion['version'] and closeout['requestHash'] == completion['requestFileHash']
        and closeout['auditSeconds'] >= completion['auditSeconds'] and closeout['auditBytes'] >= completion['auditBytes']
        and math.isclose(closeout['measuredSeconds'], completion['nativeSeconds']+closeout['auditSeconds'], abs_tol=1e-6)
        and closeout['retainedBytes'] == completion['nativeBytes']+closeout['auditBytes'], 'Invalid final phase accounting')
    return {k:pre.positive(closeout[k] if k.startswith('audit') else completion[k], k) for k in pre.PHASES}


def forecast(inputs, baseline, placement):
    pre.assess(inputs)  # Retain all original design, pilot and inherited-evidence validation.
    old = phase_costs(inputs['new-costs'], inputs['new-closeout'])
    pilot = phase_costs(inputs['pilot-costs'], inputs['pilot-closeout'])
    for costs in (baseline, placement):
        require(set(costs) == set(pre.PHASES), 'Missing matched phase')
        for key in pre.PHASES: pre.positive(costs[key], key)
    placement_floor = {k:max(old[k], placement[k]) for k in pre.PHASES}
    ratios = {k:max(1, placement_floor[k]/baseline[k]) for k in pre.PHASES}
    scaled = {k:2*pilot[k]*(21888/inputs['pilot-result']['fights'])*ratios[k] for k in pre.PHASES}
    inherited = inputs['design']['resources']['inheritedForecastFloors']
    floors = {k:max(inherited[k], scaled[k]) for k in pre.PHASES}
    probe = inputs['audit-probe']
    historical_bytes = inputs['probe-previous-forecast']['totalBytes']/probe['inventoryByteRatio']
    storage_ratio = max(1, (floors['nativeBytes']+floors['auditBytes'])/historical_bytes)
    extra = probe['extraInventorySeconds']*max(1, storage_ratio/probe['inventoryByteRatio'])
    floors['auditSeconds'] = max(floors['auditSeconds'], probe['auditPlanningSeconds'],
        2*(probe['wholeWorkerSeconds']+sum(probe['independentAuditSeconds'])+extra)+120)
    rounded = {k:math.ceil(v) for k,v in floors.items()}
    failed = [k for k in pre.PHASES if rounded[k] >= pre.LIMITS[k]]
    return dict(version=VERSION, status='MatchedResourceFloorsExceedEnvelope' if failed else 'ReadyForBoundedQualificationOnly',
        admitted=False, runtimeQualified=False, qualificationStarted=False, scientificLaunches=0,
        productionEntropyDraws=0, actualCombat=0, originalPlacementCosts=old, matchedBaselineCosts=baseline,
        matchedPlacementCosts=placement, upwardOnlyPlacementFloors=placement_floor, upwardOnlyRatios=ratios,
        completedPilotCosts=pilot, completedPilotFights=inputs['pilot-result']['fights'], scaledPilotCosts=scaled,
        inheritedFloors=inherited, roundedPhaseFloors=rounded, exceededPartitions=failed, limits=pre.LIMITS,
        storageAppliedBeforeAuditScaling=True, timingMargin=2, publicationReserveSeconds=120,
        qualifiedCurrentForecast=None, usableForScientificAdmission=False,
        next='Address the exceeded partition through a prospectively specified implementation change; preserve these floors and all attempts. Do not enlarge limits or repeat timings.'
            if failed else 'Perform the separate guarded current-runtime qualification before resource admission; no prospective combat or production entropy yet.')


def check_pair(a, b):
    for name in FILES:
        require(sha(a/name) == sha(b/name), 'Unmatched prepared input: '+name)
    contract = read(a/'matched-contract.json')
    require(contract['version'] == VERSION and len(contract['commonV5PlanHashes']) == 12
        and len(contract['selected']) == 4380 and contract['reserved'] == list(range(200000,216384)), 'Invalid shared fixture contract')
    require(pre.fixture_shape(read(a/'context.json')) == dict(partySize=10,essenceSlots=5,templateActors=10), 'Wrong physical workload')
    pa, pb = read(a/'plan.json'), read(b/'plan.json')
    require(pa['version'] == 'tower-affinity-allied-action-comparison-v1'
        and pb['version'] == 'tower-loadout-placement-comparison-v1' and pa['candidate'] == pb['control'], 'Wrong paired policies')
    for folder in (a,b):
        for name,pin in read(folder/'runtime.json').items(): require(sha(folder/'runtime'/name) == pin, 'Changed prepared runtime')
    return dict(status='MatchedBeforeEitherOwnerStarted', inputs={name:sha(a/name) for name in FILES},
        commonV5PlanHashes=contract['commonV5PlanHashes'], normalization='None; exact byte identity required')


def verify_shared(a, b):
    roots = []
    for root in range(1,13):
        old = a/f'search/root-{root:02}/candidate'; new = b/f'search/root-{root:02}/control'
        am, bm = read(old/'files.json'), read(new/'files.json')
        require(am == bm, f'Common v5 archive differs at root {root}')
        for name,pin in am.items():
            require(sha(old/name) == pin and sha(new/name) == pin, f'Changed common v5 evidence: {root}/{name}')
        roots.append(dict(root=root, files=len(am), manifestSha256=sha(old/'files.json'),
            racingPlanSha256=sha(old/'racing/plan.json'), searchSha256=sha(old/'racing/search.json')))
    catalogues = sorted(b.glob('study/placement-catalogue-*.json'))
    require(len(catalogues) == 12, 'Missing placement catalogue')
    return dict(status='AllTwelveCommonV5TrajectoriesByteIdentical', normalization='None', roots=roots,
        placementCatalogues={p.name:sha(p) for p in catalogues})


def seal(root):
    write(root/'files.json', {p.relative_to(root).as_posix():sha(p) for p in root.rglob('*') if p.is_file()})


def run(output, host):
    require(output == (ROOT/FIRST_PAIR).resolve(), 'This frozen registration permits only its first declared path; no replacement output')
    require(not output.exists(), 'No overwrite, timing retry or silent replacement')
    require(sha(ROOT/PRIOR) == PRIOR_PIN and sha(ROOT/PLAN) == PLAN_PIN, 'Changed frozen fixture plan or handoff')
    prior = read(ROOT/PRIOR)
    for path,pin in prior['preservedHistoricalPins'].items(): require(sha(ROOT/path) == pin, 'Changed inherited pin: '+path)
    for path,pin in prior['currentSourceHashes'].items():
        if path not in ('build/test-proposal-affinity-study-owned.py','LL/tests/BalanceHarness.ProcessFixture/ProposalStudyFixtureHost.cs'):
            require(sha(ROOT/path) == pin, 'Unrelated implementation drift: '+path)
    frozen = read(ROOT/PLAN)
    require(sha(host.parent/'BalanceHarness.dll') == frozen['currentHarnessSha256'], 'Production harness changed')
    pre.verify(ROOT/PRECHECK)
    output.mkdir(); started = time.monotonic(); owned_support_seconds = 0
    sources = [Path(__file__), ROOT/'build/test-proposal-affinity-study-owned.py',
        ROOT/'build/run-proposal-affinity-study.py', ROOT/'build/bounded_windows_process.py',
        ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py',
        ROOT/'Balance Harness/analysis/loadout-placement-resource-precheck.py',
        ROOT/'LL/tests/BalanceHarness.ProcessFixture/ProposalStudyFixtureHost.cs']
    pins = {str(p):sha(p) for p in sources+[host,host.with_suffix('.pdb')]}
    write(output/'declaration.json',dict(version=VERSION, priorHandoffSha256=PRIOR_PIN, frozenPlanSha256=PLAN_PIN,
        sourcePins=pins, executionOrder=list(PROFILES), attemptsPerProfile=1, retryPermitted=False,
        engineeringOnly=True, productionEntropyDraws=0, actualCombat=0, qualificationStarted=False,
        resourceEnvelope='tower-proposal-resource-envelope-v2', perOwnerLimits=pre.LIMITS,
        perOwnerMaximumSeconds=10800, perOwnerMaximumBytes=6442450944,
        supportingPreparationSecondsPerFixture=120, supportingVerificationSecondsPerFixture=1200,
        supportingAnalysisMaximumSeconds=180, supportingAnalysisMaximumBytes=134217728,
        fullPairAllowanceChargedAtStart=True, chargedMaximumSeconds=24420, chargedMaximumBytes=13019119616,
        costRule=frozen['costRule']))
    try:
        shutil.copyfile(ROOT/PRIOR,output/'prior-handoff.json'); shutil.copyfile(ROOT/PLAN,output/'frozen-plan.json')
        shutil.copytree(ROOT/PRECHECK,output/'retained-precheck')
        for p in sources:
            target = output/'implementation'/p.relative_to(ROOT); target.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(p,target)
        for p in (host,host.with_suffix('.pdb')):
            target = output/'fixture-host'/p.name; target.parent.mkdir(exist_ok=True); shutil.copyfile(p,target)
        source = output/'source-fixture'; source.mkdir()
        original = ROOT/Path(frozen['physicalContextSource']).parent.parent
        require(sha(original/'files.json') == frozen['sourceFixtureManifestSha256'], 'Changed physical source manifest')
        manifest = read(original/'files.json')
        for name in ('context','plan','settings','history','runtime'):
            path = 'source/'+name+'.json'
            require(sha(original/path) == manifest[path], 'Changed authenticated source: '+path)
            shutil.copyfile(original/path,source/('fixture-'+name+'.json'))
        fixture = module('matched_owned_fixture',ROOT/'build/test-proposal-affinity-study-owned.py')
        prepared = []
        for profile,label in zip(PROFILES,('baseline','placement')):
            args = SimpleNamespace(fixture_host=host,source_fixture=source,
                output=output/('tower-proposal-owned-fixture-matched-'+label), mode='complete',profile=profile,
                resource_envelope='tower-proposal-resource-envelope-v2')
            work_started = time.monotonic()
            prepared.append((args,fixture.prepare(args)))
            owned_support_seconds += time.monotonic()-work_started
        check = check_pair(prepared[0][0].output/'package',prepared[1][0].output/'package')
        require(read(prepared[0][0].output/'package/runtime.json') == read(source/'fixture-runtime.json'), 'Runtime/PDB differs from frozen source')
        write(output/'preflight.json',check)
        for path,pin in pins.items(): require(sha(Path(path)) == pin, 'Changed code before measurement')
        summaries = []
        for args,state in prepared:
            # Durable start receipt prevents treating a failed or incomplete first sample as absent.
            write(args.output/'measurement-start.json',dict(profile=args.profile,ordinal=len(summaries)+1,attempt=1))
            work_started = time.monotonic()
            summaries.append(fixture.execute(args,state))
            owned_support_seconds += time.monotonic()-work_started
        a,b = (args.output/'result' for args,_ in prepared)
        shared = verify_shared(a,b); write(output/'common-v5-verification.json',shared)
        index = read(output/'retained-precheck/inputs.json')
        inputs = {k:read(output/'retained-precheck'/p) for k,p in index.items()}
        costs = [phase_costs(read(p/'completion.json'),read(p/'closeout.json')) for p in (a,b)]
        assessment = forecast(inputs,*costs); write(output/'assessment.json',assessment)
        results = [read(p/'result.json') for p in (a,b)]
        supporting_bytes = sum(p.stat().st_size for p in output.rglob('*') if p.is_file() and not any(p.is_relative_to(r) for r in (a,b)))
        require(time.monotonic()-started-owned_support_seconds < 180 and supporting_bytes < 134217728, 'Supporting analysis allowance exceeded')
        require(time.monotonic()-started < 24420, 'Declared pair allowance exceeded')
        write(output/'completion.json',dict(status='MatchedOwnedFixturesVerified', seconds=time.monotonic()-started,
            supportingAnalysisSeconds=time.monotonic()-started-owned_support_seconds, supportingBytes=supporting_bytes,
            attempts=2, timingRetries=0, fixtures=summaries, commonV5Roots=12, catalogueCount=12,
            actualCombat=0, productionEntropyDraws=0, scientificLaunches=0, qualificationStarted=False,
            workload=[{k:r[k] for k in ('version','fights','searchFights','heldoutFights','differingRoots','decision')} for r in results],
            finalCloseoutPins=[sha(p/'closeout.json') for p in (a,b)], resultManifestPins=[sha(p/'files.json') for p in (a,b)]))
        for path,pin in pins.items(): require(sha(Path(path)) == pin, 'Code changed during measurement')
        return assessment
    except BaseException as error:
        write(output/'failure.json',dict(status='RetainedMatchedFixtureFailure',reason=str(error),seconds=time.monotonic()-started,
            retryPermitted=False,qualificationStarted=False,admitted=False))
        raise
    finally:
        seal(output)


def verify(output, expected_manifest):
    require(sha(output/'files.json') == expected_manifest, 'Changed externally pinned matched package')
    manifest = read(output/'files.json')
    require({p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()} == set(manifest)|{'files.json'}, 'Changed retained inventory')
    for name,pin in manifest.items(): require(sha(output/name) == pin, 'Changed retained file: '+name)
    a,b = (output/('tower-proposal-owned-fixture-matched-'+label) for label in ('baseline','placement'))
    if (output/'failure.json').exists():
        require(not (output/'completion.json').exists() and not (output/'assessment.json').exists(), 'Failed pair published a result')
        declaration=read(output/'declaration.json'); failure=read(output/'failure.json')
        require(declaration['retryPermitted'] is False and failure['retryPermitted'] is False, 'Failed pair permits timing replacement')
        return dict(status='RetainedMatchedFixtureFailureVerified',reason=failure['reason'],
            firstBaselineCompleted=(a/'result/closeout.json').exists(),candidateStarted=(b/'measurement-start.json').exists(),
            fixtureAttempts=sum((p/'measurement-start.json').exists() for p in (a,b)),timingRetries=0,
            admitted=False,runtimeQualified=False,qualificationStarted=False,qualifiedCurrentForecast=None,
            chargedMaximumSeconds=declaration['chargedMaximumSeconds'],chargedMaximumBytes=declaration['chargedMaximumBytes'])
    require(check_pair(a/'package',b/'package') == read(output/'preflight.json'), 'Changed preflight')
    require(verify_shared(a/'result',b/'result') == read(output/'common-v5-verification.json'), 'Changed common v5 proof')
    inputs = {k:read(output/'retained-precheck'/p) for k,p in read(output/'retained-precheck/inputs.json').items()}
    costs = [phase_costs(read(p/'result/completion.json'),read(p/'result/closeout.json')) for p in (a,b)]
    result = forecast(inputs,*costs)
    require(result == read(output/'assessment.json'), 'Changed assessment')
    return result


if __name__ == '__main__':
    import json
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action',choices=('run','verify')); parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--fixture-host',type=Path)
    parser.add_argument('--expected-manifest-sha256')
    args = parser.parse_args()
    if args.action == 'run': require(args.fixture_host is not None, 'Fixture host required')
    print(json.dumps(run(args.output.resolve(),args.fixture_host.resolve()) if args.action == 'run'
        else verify(args.output.resolve(),args.expected_manifest_sha256),indent=2))
