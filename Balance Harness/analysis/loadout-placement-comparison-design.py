"""Freeze a prospective placement-versus-allied-action pilot. Never allocate or run combat."""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-loadout-placement-comparison-design-v1'
COMPARISON = 'tower-loadout-placement-comparison-v1'
DECISION = 'incomplete-then-abandon-either-endpoint-then-no-differentiation-then-relative-and-absolute-gain-with-novelty-else-inconclusive-never-adopt'
PRIOR = 'TestResults/loadout-placement-native-implementation-handoff-20260924.json'
PRIOR_PIN = '8ecd2ba3875f0ec8cee3302b8abfe46cd4340381dd5e52d868f152074a52ab02'
PREVIEW = 'TestResults/loadout-placement-native-preview-20260924-v4'
OLD_PREVIEW = 'TestResults/affinity-allied-action-preview-20260924-v2'
PLAN = 'Balance Harness/Tower-Affinity-Allied-Action-Comparison-Plan.json'
FORECAST = 'TestResults/affinity-allied-action-admission-20260924/resource-forecast.json'
PILOT = 'TestResults/affinity-allied-action-pilot-01-execution-handoff-20260924.json'
DESIGN_PATH = 'Balance Harness/Tower-Loadout-Placement-Comparison-Design.json'
SECONDS, BYTES = 180, 64 * 1048576


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False, allow_nan=False).encode()


def digest(value):
    return hashlib.sha256(canonical(value)).hexdigest()


def native_scope_hash(scope):
    # The captured scope is ASCII; do not silently generalize this compatibility bridge.
    scope = copy.deepcopy(scope)
    scope.update(id='proposal-comparison-scope', startsAt='1970-01-01T00:00:00+00:00', excludedCombatSeeds=[])
    scope['generation']['seeds'] = []
    text = canonical(scope).decode()
    require(text.isascii(), 'Unsupported native scope encoding')
    for char in ['+', '<', '>', '&', "'"]:
        text = text.replace(char, '\\u%04X' % ord(char))
    return hashlib.sha256(text.encode()).hexdigest()


def unique(pairs):
    value = {}
    for key, item in pairs:
        require(key not in value, 'Duplicate JSON property')
        value[key] = item
    return value


def read(path):
    def nonfinite(value):
        raise ValueError('Nonfinite JSON number: ' + value)
    return json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=unique, parse_constant=nonfinite)


def save(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, ensure_ascii=False, allow_nan=False)
        stream.write('\n')


def load_evidence(root=ROOT):
    consumed = {}

    def item(name, pin):
        path = root / name
        require(path.resolve().is_relative_to(root.resolve()) and sha(path) == pin, 'Changed source: ' + name)
        consumed[name] = pin
        return read(path)

    prior = item(PRIOR, PRIOR_PIN)
    pins = prior['preservedHistoricalPins']

    def package(folder, names):
        manifest = item(folder + '/files.json', pins[folder + '/files.json'])
        return {name: item(folder + '/' + name, manifest[name]) for name in names}

    preview = package(PREVIEW, ['input-01.json', 'root-01/batches.json', 'summary.json',
                                'declaration.json', 'physical-hash-bindings.json'])
    old = package(OLD_PREVIEW, ['root-01/request.json'])
    return dict(prior=prior, preview=preview, oldRequest=old['root-01/request.json'],
                oldPlan=item(PLAN, pins[PLAN]), forecast=item(FORECAST, pins[FORECAST]),
                pilot=item(PILOT, pins[PILOT]), sourceFiles=consumed)


def slots():
    return [dict(root=i+1, proposalRoot=[109*i, 109*i+1],
                 racing=[[109*i+1+8*j, 109*i+9+8*j] for j in range(4)],
                 nomination=[109*i+33, 109*i+49], validation=[109*i+49, 109*i+109],
                 heldout=[1308+256*i, 1308+256*(i+1)]) for i in range(12)]


def decide(method_net, benchmark_net, differing, promising, complete=True):
    """Design oracle: integer win differences over twelve equal 256-value panels."""
    require(all(type(x) is int for x in [method_net, benchmark_net, differing, promising])
            and abs(method_net) <= 3072 and abs(benchmark_net) <= 3072
            and 0 <= differing <= 12 and 0 <= promising <= 12, 'Invalid decision inputs')
    if not complete:
        return 'InvalidStudy'
    if 100 * method_net <= -2 * 3072 or 100 * benchmark_net <= -2 * 3072:
        return 'AbandonThisConfiguration'
    if differing == 0:
        return 'NoObservedOutputDifferentiation'
    if 100 * method_net >= 2 * 3072 and 100 * benchmark_net >= 2 * 3072 and differing >= 3 and promising >= 3:
        return 'LargerFreshEvaluationWarranted'
    return 'Inconclusive'


def build_design(e):
    prior, preview, old = e['prior'], e['preview'], e['oldPlan']
    request, batches, summary = (preview[k] for k in ['input-01.json', 'root-01/batches.json', 'summary.json'])
    require(prior['backendTestsPassed'] == 350 and prior['pythonTestsPassed'] == 15
            and prior['nativePolicyImplemented'] and not prior['comparisonStudyImplemented'], 'Unverified native implementation')
    require(summary['status'] == 'VerifiedNativeLoadoutPlacementPreview' and summary['completeRoots'] == 12
            and summary['catalogueRecipes'] == 238 and summary['oldSerializedArmBytesUnchanged'] == 24
            and summary['newFights'] == summary['newValues'] == summary['newEntropyDraws'] == 0, 'Incomplete preview')
    require(request['version'] == 'tower-proposal-export-v6', 'Changed export version')
    policies = {p['version']: p for p in request['policies']}
    control, candidate = policies['tower-proposal-policy-v5'], policies['tower-proposal-policy-v6']
    require(control == old['candidate'], 'Changed v5 control')
    require(candidate == dict(version='tower-proposal-policy-v6', name='benchmark-subgroup-loadout-placement-v6',
        firstWave=['subgroup-loadout-placement']*9, secondWave=['subgroup-loadout-placement']*8,
        parentTickets=['benchmark'], preserveParentInteractions=False), 'Changed placement policy')
    context, old_context = request['context'], e['oldRequest']['context']
    normalized = copy.deepcopy(old_context)
    ref = next(r for r in normalized['scope']['references'] if r['id'] == normalized['benchmarkReferenceId'])
    current_ref = next(r for r in context['scope']['references'] if r['id'] == context['benchmarkReferenceId'])
    ref['scenario']['id'] = current_ref['scenario']['id']
    require(normalized == context, 'Physical context changed beyond frozen scenario label')
    require(native_scope_hash(old_context['scope']) == old['scopeHash'], 'Native scope hash bridge disagrees with historical golden')
    benchmark = next(s['party'] for s in context['scope']['starts'] if s['referenceId'] == context['benchmarkReferenceId'])
    require(benchmark['id'] == old['benchmarkPartyId'], 'Changed benchmark')
    arms = {a['name']: a for a in batches['arms']}
    catalogue = arms[candidate['name']]['loadoutPlacementCatalogue']
    require(len(catalogue['recipes']) == 238 and catalogue['parentId'] == benchmark['id']
            and len(preview['physical-hash-bindings.json']['bindings']) == 238, 'Incomplete placement catalogue')
    coverage = arms[control['name']]['affinityCreationCoverage']
    legal = {(r['owner'], tuple(edit['removed']), tuple(edit['added']))
             for r in coverage['opportunities'] for edit in r['legalEdits']}
    require(len(legal) == 26, 'Changed control neighborhood')
    for policy in [control, candidate]:
        arm = arms[policy['name']]
        require(arm['status'] == 'Complete' and len(arm['batch']['candidates']) == 9
                and len(arm['secondWave']['batch']['candidates']) == 8, 'Incomplete two-wave arm')
    require(e['pilot']['decision'] == 'Inconclusive' and e['pilot']['novelCandidateRoots'] == 0
            and e['pilot']['benchmark']['mean'] == 0, 'Changed motivating pilot evidence')
    plan = copy.deepcopy(old)
    plan.update(version=COMPARISON, scopeHash=native_scope_hash(context['scope']),
                control=copy.deepcopy(control), candidate=copy.deepcopy(candidate))
    plan['analysis'].update(goBenchmarkAtLeast=0.02, goPromisingNovelRootsAtLeast=3,
                            abandonPromisingNovelRootsBelow=0, decisionOrder=DECISION)
    require((plan['roots'], plan['heldoutSamples'], plan['searchFightsPerArm'], plan['requiredFreshValues'],
             plan['maximumFights'], plan['maximumSeconds'], plan['maximumBytes'])
            == (12, 256, 528, 4380, 21888, 10800, 6442450944), 'Changed inherited allocation')
    forecast = e['forecast']
    require(forecast['resourceEnvelope'] == 'tower-proposal-resource-envelope-v2' and forecast['fits']
            and forecast['timingMargin'] == 2 and forecast['publicationReserveSeconds'] == 120, 'Changed resource baseline')
    return dict(version=VERSION, status='FrozenProspectiveComparisonNotImplementedNotAdmitted',
        designIdentity='SHA256 of sorted compact UTF-8 JSON with literal Unicode; not a native plan hash',
        sourceFiles=dict(sorted(e['sourceFiles'].items())), plannedNativePlan=plan,
        physicalBinding=dict(contextSource=PREVIEW+'/input-01.json', contextDesignHash=digest(context),
            nativeScopeHash=plan['scopeHash'], nativeInventoryHash=old['inventoryHash'],
            benchmarkPartyId=benchmark['id'], referencePartyIds=sorted(s['party']['id'] for s in context['scope']['starts']),
            frozen='Captured content, gameplay dependencies, inventory, actor identities, gear, styles, floor, budgets, references, selected control affinities and both exact policies',
            allowedQualificationChanges='Episode id/start time, accumulated exclusions and qualified harness execution identity only; preserve original design and rebind native scope explicitly',
            scopeHashMeaning='Normalized comparison scope hash differs from full per-root placement catalogue scope hash; full catalogue hash remains in the unchanged root-derived shuffle'),
        runtime=dict(controlRacingVersion='tower-proposal-racing-v7', candidateRacingVersion='tower-proposal-racing-v8',
            selectorVersion='tower-racing-benchmark-validation-v1',
            inventoryBinding='Shared captured context retains inventory; control racing binds it, placement racing MUST carry null inventory and no affinity metadata',
            comparisonImplemented=False, qualificationPerformed=False, priorAdmissionReusable=False),
        hypothesis=dict(target='Selected-output improvement at matched search cost on this captured floor-5 scenario',
            contrast='Entire proposer package: whole-loadout placement and distinct-recipe shuffle versus allied-action affinity insertion and its existing sampling law',
            causalLimit='Not an isolated causal estimate of bundle conservation, edit radius, neighborhood size or uniform sampling',
            controlStatus='Existing development comparator, not a promoted or proven optimal baseline'),
        feasibility=dict(controlRecipes=26, candidateRecipes=238, requiredUniquePerRoot=17, savedRoots=12,
            candidateDeterministicCapacity=True, controlFreshRootFillGuaranteed=False,
            noRootScreening=True, noOutcomeFittedWeights=True, noUnderfillFallback=True),
        allocation=dict(requiredFreshValues=4380, actualValuesAllocated=0, entropyBytes=65536, exposedSignedInt32Words=16384,
            slots=slots(), ranges='Zero-based half-open allocation positions, not seed values',
            rule='One post-admission draw; first 4380 distinct nonhistorical values in exposure order, no refill or replacement',
            permanentExclusion='Every distinct newly exposed value including unused tail; retain collisions and duplicates',
            history='Reconcile full authoritative registry and pending recoveries at admission and launch, including previews and all unused reservations',
            isolation='Both arms share root and ordered 8/8/8/8/16/60 panels; all roles and roots disjoint, including heldout'),
        evaluation=dict(searchFightsPerArmRoot=528, searchFightsTotal=12672, heldoutPerRoot=256,
            heldoutFightsMinimum=3072, heldoutFightsMaximum=9216, maximumFights=21888,
            preflight='Both policies before either search, no prospective root preview; any incomplete root invalidates entire study',
            barrier='Freeze all 24 complete outputs before any heldout observation',
            gate='Existing selector unchanged: one challenger from 16 nomination values; 60 paired validation values; gains > losses and exact one-sided discordant-binomial tail <= 1/20, otherwise benchmark',
            pairedSearch='Equal physical recipe and seed on same panel must have equal outcomes; separately charge both arms',
            heldout='Per root evaluate selected control, selected candidate and benchmark on same 256 values; deduplicate only identical physical recipes within that root',
            failure='Retain all evidence, full charges and reservations; no sample-size extension, root replacement, retry or outcome-driven rescue'),
        reporting=dict(replicationUnit='Paired search root; all twelve retained equally',
            uncertainty='All root contrasts, mean, median, worst, SD and descriptive t interval df11; paired-seed SE and covariance conditional on frozen outputs; do not pool battles as independent search replications',
            rootIntervalFormula='mean +/- 2.200985160082949 * sampleSD / sqrt(12); existing descriptive convention, no power or coverage guarantee for this small mixture',
            decisionRule=DECISION, practicalEffects='Both mean gains >= 2 percentage points, >=3 differing outputs, >=3 reference-relative novel candidate outputs each >=3 points over benchmark; larger fresh evaluation only',
            harmfulRule='Either mean <= -2 points abandons regardless of promising-root count',
            novelty='Not equal to any of the three fixed physical reference recipes; not global or previously-unseen novelty',
            exactThresholds=dict(meanGainNetWinsAtLeast=62, meanLossNetWinsAtMost=-62, novelRootBenchmarkNetWinsAtLeast=8),
            diagnostics=['Both-arm gates, exact tails, fallbacks, selected identities and all 12 root contrasts',
                'Generation fill, attempts/rejections, duplicate rates, catalogue/assignment provenance and unique physical recipes',
                'Distinct candidate outputs, reference-relative novelty, regressions, complete-loadout/Essence/subgroup invariants',
                'Elapsed and retained/peak bytes by phase, charged/physical fights, used/exposed/unused permanent values'],
            prohibited='No best-pool substitution, ungated nominee rescue, favorable-root subset, repeated looks, automatic adoption or individual-team confirmation'),
        resources=dict(envelope='tower-proposal-resource-envelope-v2', qualificationMaximumSeconds=900,
            qualificationMaximumBytes=1073741824, scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
            nativeMaximumSeconds=9000, nativeMaximumBytes=5905580032,
            auditPublicationMaximumSeconds=1800, auditPublicationMaximumBytes=536870912,
            margin=2, publicationReserveSeconds=120,
            inheritedForecastFloors={k: math.ceil(forecast[k]) for k in ['nativeSeconds', 'auditSeconds', 'nativeBytes', 'auditBytes']},
            forecast='Upward-only maximum of inherited admitted floors, frozen audit probe, completed pilot phase costs scaled to 21888 fights, current guarded preparation, and paired old/new owned fixture ratios; apply metadata/storage growth before audit scaling',
            additionalCosts='Full placement catalogue, derivations, physical-hash bindings, source/runtime retention, repeated export reconstruction, both audits and publication',
            limits='Each partition must fit separately with margin and reserve; no borrowing, timing retries or reduction of inherited floors',
            failure='Not admitted if any forecast fails; separately version a prospective resource amendment before entropy',
            accounting='Separate qualification/execution declarations, full allowances charged at start including failure; neither started here'),
        implementationRequirements=[
            'Add an exact separately versioned comparison profile; preserve all existing policy, racing, comparison and default behavior',
            'Bind v7 control with inventory and v8 candidate with null inventory to identical racing scope/panels; do not weaken old equal-inventory pair validation',
            'Register fresh-value count, version dispatch, paired reconstruction, study runner, both-arm gate reports, archive and publication for the new version',
            'Implement the new decision order explicitly in native and independent Python audit; legacy selector branch ignores novelty and legacy other branch conditionally abandons benchmark harm',
            'Audit all 238 recipes, source maps, physical scenarios and both waves; verify scope-bound hash separately from root-invariant recipes and preserve both historical/native hash encodings',
            'Extend owned launcher and versioned resource admission; current preview is not scientific qualification',
            'Test 12-root literal evaluator fixtures, integer decision boundaries, zero-contrast/fallback cases, mixed inventory rules, tampering, disjointness, early-heldout rejection and failure retention',
            'Qualify producing PDB/source/runtime and captured gameplay; rebuild inventory and compare materialized/prepared participants under combat guard for all 238 placements, three references, saved control waves and required legacy coverage',
            'Use saved development values only for qualification; reconcile history, freeze resource forecast and seal new admission before one separately declared owned launch'],
        currentImplementationSourcePins=copy.deepcopy(preview['declaration.json']['sourcePins']),
        productionDefaultsChanged=False, newFights=0, newReservedValues=0, newEntropyDraws=0)


def verify_design(design, evidence):
    require(canonical(design) == canonical(build_design(evidence)), 'Changed frozen design')
    return dict(status='VerifiedProspectiveComparisonNotAdmitted', designHash=digest(design),
                maximumFights=21888, requiredFreshValues=4380, newFights=0, newReservedValues=0, newEntropyDraws=0)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['create', 'verify'])
    parser.add_argument('--output', type=Path)
    parser.add_argument('--design', type=Path, default=ROOT / DESIGN_PATH)
    parser.add_argument('--evidence-root', type=Path, default=ROOT)
    args = parser.parse_args()
    if args.command == 'verify':
        print(json.dumps(verify_design(read(args.design), load_evidence(args.evidence_root)), indent=2))
        return
    require(args.output is not None, 'Missing publication output')
    out = args.output.resolve()
    require(out.is_relative_to(ROOT / 'TestResults') and not out.exists() and not args.design.exists(), 'Existing or unsafe publication path')
    out.mkdir()
    started = time.monotonic()
    save(out / 'declaration.json', dict(version=VERSION, maximumSeconds=SECONDS, maximumBytes=BYTES,
        fullAllowanceChargedAtStart=True, sourceHandoffSha256=PRIOR_PIN, sourceSha256=sha(Path(__file__)),
        newFights=0, newValues=0, entropyDraws=0))

    def limits():
        require(time.monotonic() - started < SECONDS, 'Design time allowance exhausted')
        require(sum(p.stat().st_size for p in out.rglob('*') if p.is_file()) < BYTES, 'Design storage allowance exhausted')

    try:
        e = load_evidence(args.evidence_root)
        for name, pin in e['prior']['preservedHistoricalPins'].items():
            require(sha(ROOT / name) == pin, 'Changed inherited pin: ' + name)
        design = build_design(e)
        for name, pin in e['sourceFiles'].items():
            source, dest = args.evidence_root / name, out / 'evidence' / name
            require(sha(source) == pin, 'Changed design source')
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, dest)
            limits()
        save(out / 'design.json', design)
        result = verify_design(read(out / 'design.json'), load_evidence(out / 'evidence'))
        require(sha(Path(__file__)) == read(out / 'declaration.json')['sourceSha256'], 'Changed producing source')
        shutil.copyfile(Path(__file__), out / 'design-builder.py')
        save(out / 'summary.json', dict(**result, inheritedPinsPreserved=len(e['prior']['preservedHistoricalPins']),
            retainedEvidenceReconstruction=True, history=e['prior']['history'], liveHistoryRescanned=False,
            chargedSeconds=SECONDS, chargedBytes=BYTES,
            **{k: v + (SECONDS if k.endswith('Seconds') else BYTES)
               for k, v in e['prior'].items() if k.startswith('cumulative')},
            measuredSecondsBeforeSealing=time.monotonic()-started))
        limits()
        save(out / 'files.json', {p.relative_to(out).as_posix(): sha(p) for p in sorted(out.rglob('*')) if p.is_file()})
        limits()
        save(args.design, design)
        print(json.dumps(dict(**result, publicationManifestSha256=sha(out / 'files.json'),
                              retainedBytes=sum(p.stat().st_size for p in out.rglob('*') if p.is_file())), indent=2))
    except BaseException as error:
        save(out / 'failure.json', dict(reason=str(error), chargedSeconds=SECONDS, chargedBytes=BYTES))
        if not (out / 'files.json').exists():
            save(out / 'files.json', {p.relative_to(out).as_posix(): sha(p) for p in sorted(out.rglob('*')) if p.is_file()})
        raise


if __name__ == '__main__':
    main()
