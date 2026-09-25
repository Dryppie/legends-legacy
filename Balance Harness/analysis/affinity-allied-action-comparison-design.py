"""Freeze/verify the fresh v4-versus-v5 study design, without allocating or launching.

This is a prospective design, not a native admission or an executable study request.
All input evidence comes from externally pinned, completed development packages.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-affinity-allied-action-comparison-design-v1'
COMPARISON = 'tower-affinity-allied-action-comparison-v1'
PREVIEW = 'TestResults/affinity-allied-action-preview-20260924-v2'
PREVIEW_PIN = '8301070ad78ab8ea84875faf8092c7ae5da193f1d0106245e417b70f689231b1'
VERIFICATION = 'TestResults/affinity-allied-action-verification-20260924'
VERIFICATION_PIN = '8e2db033cba169decfa1eb4b856849ff89be02ed58f5d1e2efd5593e1d252236'
PRIOR_PLAN = 'Balance Harness/Tower-Affinity-Preservation-Comparison-Plan.json'
PRIOR_PLAN_PIN = '86399e4c6cc672443d6307aaa63ab6d5bf911416f1cb6396d0e8d5bda668407b'
PRIOR_PREVIEW = 'TestResults/affinity-creation-preservation-preview-20260924'
PRIOR_PREVIEW_PIN = '7b001519484ecac83fd6c3ce64085bd63cb56c595e56fa3eda5fe04427e85023'
PLAN = ROOT / 'Balance Harness/Tower-Affinity-Allied-Action-Comparison-Design.json'
CONTROL, CANDIDATE = 'tower-proposal-policy-v4', 'tower-proposal-policy-v5'
RULE = 'preserve-completable-affinity-endpoints-and-allied-basic-attack-providers-v1'


def require(ok, message):
    if not ok:
        raise ValueError(message)


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False, allow_nan=False).encode('utf-8')


def digest(value):
    return hashlib.sha256(canonical(value)).hexdigest()


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def unique_object(pairs):
    value = {}
    for key, item in pairs:
        require(key not in value, 'Duplicate JSON property: ' + key)
        value[key] = item
    return value


def read(path):
    def nonfinite(value):
        raise ValueError('Nonfinite JSON number: ' + value)
    return json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=unique_object, parse_constant=nonfinite)


def save(path, value):
    # Exclusive creation preserves an already reviewed design.
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, ensure_ascii=False, allow_nan=False)
        stream.write('\n')


def authenticated(root, folder, pin, names, consumed):
    package = root / folder
    require(sha(package / 'files.json') == pin, 'Changed source manifest: ' + folder)
    manifest = read(package / 'files.json')
    consumed[folder + '/files.json'] = pin
    result = {}
    for name in names:
        path = package / name
        require(path.resolve().is_relative_to(package.resolve()), 'Escaping source member')
        require(name in manifest and sha(path) == manifest[name], 'Changed source member: ' + name)
        consumed[folder + '/' + name] = manifest[name]
        result[name] = read(path)
    return result


def load_evidence(root=ROOT):
    consumed = {}
    preview = authenticated(root, PREVIEW, PREVIEW_PIN,
        ['root-01/request.json', 'root-01/batches.json', 'summary.json'], consumed)
    prior = authenticated(root, PRIOR_PREVIEW, PRIOR_PREVIEW_PIN, ['root-01/request.json'], consumed)
    verified = authenticated(root, VERIFICATION, VERIFICATION_PIN, ['closeout.json', 'implementation.json'], consumed)
    require(sha(root / PRIOR_PLAN) == PRIOR_PLAN_PIN, 'Changed preceding native design')
    consumed[PRIOR_PLAN] = PRIOR_PLAN_PIN
    return dict(request=preview['root-01/request.json'], batches=preview['root-01/batches.json'],
        preview=preview['summary.json'], oldRequest=prior['root-01/request.json'], priorPlan=read(root / PRIOR_PLAN),
        closeout=verified['closeout.json'], implementation=verified['implementation.json'], sourceFiles=consumed)


def slots():
    """Allocation positions, not actual entropy values; half-open zero-based ranges."""
    return [dict(root=i + 1, proposalRoot=[109*i, 109*i+1],
        racing=[[109*i+1+8*j, 109*i+1+8*(j+1)] for j in range(4)],
        nomination=[109*i+33, 109*i+49], validation=[109*i+49, 109*i+109],
        heldout=[1308+256*i, 1308+256*(i+1)]) for i in range(12)]


def neighborhood(coverage):
    return {(r['owner'], tuple(e['removed']), tuple(e['added']))
        for r in coverage['opportunities'] for e in r['legalEdits']}


def build_design(e):
    request, prior = e['request'], e['priorPlan']
    require(request['version'] == 'tower-proposal-export-v5' and len(request['policies']) == 2, 'Changed preview contract')
    policies = {p['version']: p for p in request['policies']}
    require(set(policies) == {CONTROL, CANDIDATE}, 'Changed arm versions')
    control, candidate = policies[CONTROL], policies[CANDIDATE]
    require(control == prior['candidate'] and prior['version'] == 'tower-affinity-preservation-comparison-v1', 'Changed baseline')
    expected = dict(control, version=CANDIDATE, name='benchmark-allied-action-affinity-creation-v5', creationRemovalRule=RULE)
    require(candidate == expected, 'More than the authored allied-action guard changed')
    require(control['parentTickets'] == ['benchmark'] and not control['preserveParentInteractions']
        and control['preservedDamageAffinityIds'] == [] and control['firstWave'] == ['affinity-create']*9
        and control['secondWave'] == ['affinity-create']*8, 'Changed proposal schedule')
    require(request['context'] == e['oldRequest']['context'], 'Changed physical development context')
    context = request['context']
    benchmark = next(s['party'] for s in context['scope']['starts'] if s['referenceId'] == context['benchmarkReferenceId'])
    require(benchmark['id'] == prior['benchmarkPartyId'], 'Changed benchmark')
    require(e['closeout']['previewManifestSha256'] == PREVIEW_PIN and not e['closeout']['newPolicyAdmittedToRacing']
        and e['closeout']['newFights'] == 0 and e['closeout']['testsPassed'] == 118, 'Changed implementation handoff')
    require(e['preview']['status'] == 'VerifiedGenerationOnly' and e['preview']['oldFullArmsReconstructed'] == 12
        and e['preview']['independentEnumerationAgrees'], 'Unverified preview')
    coverage = {}
    for policy in [control, candidate]:
        arm = next(a for a in e['batches']['arms'] if a['name'] == policy['name'])
        require(arm['status'] == 'Complete' and [len(arm['batch']['candidates']), len(arm['secondWave']['batch']['candidates'])] == [9, 8], 'Incomplete development preview')
        coverage[policy['version']] = arm['affinityCreationCoverage']
        rows = [r for r in e['preview']['rows'] if r['version'] == policy['version']]
        require(sorted(r['root'] for r in rows) == list(range(1, 13)) and all(r['acceptedByWave'] == [9, 8] for r in rows), 'Missing development roots')
    old, new = [neighborhood(coverage[v]) for v in [CONTROL, CANDIDATE]]
    require(new <= old and len(new) >= 17, 'Insufficient or expanded legal neighborhood')
    protection = coverage[CANDIDATE]['alliedActionProtection']
    require(protection['version'] == 'tower-allied-basic-attack-protection-v1'
        and protection['inventoryHash'] == prior['inventoryHash'] and protection['providers']
        and all(p['operation'] == 'PerformBasicAttack' and p['target'] == 'NonSummonedAllies' for p in protection['providers']), 'Changed protection evidence')
    native = dict(copy.deepcopy(prior), version=COMPARISON, control=copy.deepcopy(control), candidate=copy.deepcopy(candidate))
    totals = e['preview']['totals']
    return dict(version=VERSION, status='FrozenProspectiveDesignNotImplementedNotAdmitted',
        designIdentity='SHA256 of UTF-8 JSON with sorted keys, compact separators and literal Unicode; not a native HarnessJson plan hash',
        sourceFiles=dict(sorted(e['sourceFiles'].items())), plannedNativePlan=native,
        physicalBinding=dict(nativeScopeHash=prior['scopeHash'], nativeInventoryHash=prior['inventoryHash'],
            benchmarkPartyId=prior['benchmarkPartyId'], savedContextHash=digest(context),
            contextSource=PREVIEW+'/root-01/request.json',
            admissionChanges='Episode id/start time, accumulated history exclusions and qualified harness runtime identity only; rebind scope hash explicitly, retain original design',
            frozen='Captured content, gameplay dependencies, inventory, three references, floor, party, gear, styles, budgets, affinities and both policies'),
        intendedRuntime=dict(controlRacingVersion='tower-proposal-racing-v6', candidateRacingVersion='tower-proposal-racing-v7',
            selectorVersion='tower-racing-benchmark-validation-v1',
            currentCandidateStatus='Generation-export only; existing racing v1-v6 and study/audit/launcher versions reject this design'),
        feasibility=dict(source='Sealed development preview only; never a fresh efficacy sample',
            controlLegalRecipes=len(old), candidateLegalRecipes=len(new), requiredUniqueRecipesPerArmRoot=17,
            completedDevelopmentRoots=12, previewMayGuaranteeFreshRootFill=False,
            providerRule=protection['version'], providerDefinitionHash=digest(protection),
            risks=dict(controlAttempts=totals[CONTROL]['attempts'], candidateAttempts=totals[CANDIDATE]['attempts'],
                controlDistinctObservedRecipes=totals[CONTROL]['distinctObservedRecipes'], candidateDistinctObservedRecipes=totals[CANDIDATE]['distinctObservedRecipes'],
                controlRemovals=totals[CONTROL]['removedEssences'], candidateRemovals=totals[CANDIDATE]['removedEssences']),
            adaptation='No outcome-fitted removal score, owner preference, new protection class or fill relaxation in this study'),
        allocation=dict(requiredFreshValues=4380, actualValuesAllocated=0, entropyBytes=65536, exposedSignedInt32Words=16384,
            ordering='One post-admission draw; take first 4380 distinct nonhistorical values in exposure order, without screening',
            permanentExclusion='All distinct newly exposed values, including unused tail; retain historical collisions and duplicates; no refill',
            liveHistory='Complete authoritative scan and pending-recovery reconciliation at admission and launch; include all prior development roots, combat values and unused reservations',
            ranges='Half-open zero-based positions in the ordered 4380-value allocation; these are indices, not seeds',
            pairedRoles='Both arms share each root and the ordered 8/8/8/8/16/60 panels; every root and role is disjoint from heldout and other roots',
            slots=slots()),
        evaluation=dict(searchFightsPerArmRoot=528, searchFightsTotal=12672, heldoutSamplesPerRoot=256,
            heldoutFightsMaximum=9216, maximumFights=21888,
            preflight='Both arms preflight before either arm spends a search budget',
            barrier='Freeze and authenticate all 24 complete search outputs before the first heldout observation',
            incomplete='Any missing wave, root, search, validation, output, audit or publication invalidates the entire study; retain evidence, charge attempts, never replace roots or refill',
            searchPairing='Shared physical recipe/panel/seed must have equal recipe and outcome; search requests remain separately charged per arm',
            heldoutPairing='Within each root deduplicate physical recipes among control output, candidate output and fixed benchmark only; identical outputs have an exact zero method contrast',
            references='Retain all three references in both searches; independently measure fixed benchmark with selected outputs; generated-pool efficacy is not an endpoint',
            selection='Freeze one non-benchmark challenger from 16 nomination values; compare with benchmark on a complete fresh 60-value paired panel; pass iff gains exceed losses and exact one-sided discordant-pair binomial tail is at most 1/20; otherwise output benchmark',
            gateMeaning='Provisional search output gate only; no team confirmation or adoption'),
        reporting=dict(primary='All twelve equal-weight heldout candidate-minus-control root win-rate differences',
            guardrail='All twelve equal-weight heldout candidate-minus-fixed-benchmark root win-rate differences',
            uncertainty='All root contrasts; mean, median, worst and descriptive 95-percent t interval with 11 degrees of freedom. Report paired-seed standard errors and covariance separately, conditional on frozen outputs; no pooled independent-fight inference',
            decisions='Use the unchanged prior native Analysis object exactly: incomplete first; abandon at method or benchmark <= -0.02; then no differing outputs; then larger fresh evaluation warranted at method >= 0.02, benchmark >= 0 and >=3 differing roots; otherwise inconclusive; never adopt',
            diagnostics=['Both-arm all-root validation gains/losses/exact tails/pass/fallback and selected party',
                'Both-arm all-root novelty, proposal attempts, rejection reasons, unique recipes and protected-effect reasons',
                'Both-arm removals by Essence and owner, tagged descriptive only; no subgroup decisions or rescue selection',
                'Total and per-phase elapsed time, retained and peak bytes, charged fights, exposures, used and unused permanent values'],
            noDifferentiation='Retain absolute benchmark results and zero method contrasts; do not substitute best generated, ungated or retrospectively protected recipes',
            interpretation='Fresh development comparison for this frozen scope; not a powered efficacy test or deployment decision'),
        resources=dict(scientificMaximumSeconds=10800, scientificMaximumBytes=6442450944,
            nativeMaximumSeconds=9000, nativeMaximumBytes=5905580032,
            auditPublicationMaximumSeconds=1800, auditPublicationMaximumBytes=536870912,
            auditPublicationReserveSeconds=120, forecastMargin=2,
            qualificationMaximumSeconds=900, qualificationMaximumBytes=1073741824,
            accounting='Qualification and execution have separate declarations; charge full allowance on start including failure; no borrowing, timing resampling or retries',
            forecast='Upward-only maxima of prior admitted floors, frozen audit probe, completed preservation pilot actual phase costs scaled to the ceiling, current guarded preparation, and paired old/new owned fixtures; metadata and retention growth included before audit-byte scaling',
            fit='Both forecast partitions must fit their own hard limits with unchanged margins; faster new timings never lower inherited floors. Failure remains not admitted; any resource amendment is separately versioned and reviewed before entropy',
            currentAdmission='No current-runtime forecast or qualification performed; no allowance here has been spent as a campaign'),
        admissionRequirements=[
            'Implement the separately versioned v7 racing contract and allied-action comparison binding; leave v1-v6 behavior and hashes intact',
            'Extend native study execution, both-gate diagnostics, archive reconstruction and publication barriers for the exact new profile',
            'Extend independent Python audit and owned launcher with strict version/policy/panel/protection checks and allocation accounting',
            'Pass guarded literal-evaluator fixtures for all roots, resource failures, incomplete generation, shared outcome drift and rehashed tampering; no battle execution',
            'Authenticate the tested assembly, portable symbols, producing sources and all retained runtime dependencies against the captured gameplay runtime',
            'Rebuild and match captured inventory; compare materialized inputs and prepared participants under a combat guard for every required saved legacy and both-arm recipe instance',
            'Reconstruct saved legacy exports and this full two-wave preview; use saved development roots only for qualification, never screen prospective roots',
            'Scan authoritative history and recovery receipts, qualify upward-only resource forecasts, bind the qualified context, pin and seal admission with zero entropy and zero combat',
            'Before one owned execution, revalidate admission pins/history, acquire registry/output leases, preserve the entire single entropy exposure, and enforce process-tree/resource/publication barriers'],
        changesAllowedBeforeExecution='Only implementation and independently qualified runtime/history binding; changing any scientific rule requires a new prospective design',
        productionDefaultsChanged=False, newFights=0, newReservedValues=0, newEntropyDraws=0,
        implementationSourceHashes=copy.deepcopy(e['implementation']))


def verify_design(design, evidence):
    expected = build_design(evidence)
    require(canonical(design) == canonical(expected), 'Changed frozen design; regenerate only under a separately declared version')
    return dict(status='VerifiedProspectiveDesignNotAdmitted', designHash=digest(design),
        requiredFreshValues=4380, maximumFights=21888, newFights=0, newReservedValues=0, newEntropyDraws=0)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['create', 'verify'])
    parser.add_argument('--plan', type=Path, default=PLAN)
    parser.add_argument('--evidence-root', type=Path, default=ROOT,
        help='Repository root or retained evidence root; the same external source pins always apply')
    args = parser.parse_args()
    evidence = load_evidence(args.evidence_root)
    if args.command == 'create':
        save(args.plan, build_design(evidence))
    result = verify_design(read(args.plan), evidence)
    print(json.dumps({**result, 'planFileSha256': sha(args.plan)}, indent=2))


if __name__ == '__main__':
    main()
