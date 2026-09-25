"""Reproduce the eight-recipe prospective plan; no combat, entropy or allocation.

Only authenticated saved JSON and pinned arithmetic/source are read. The default
prints a plan; --check compares an existing plan; --self-test checks the arithmetic.
This file is not an experiment controller or a runnable native request.
"""
import argparse
import hashlib
import importlib.util
import json
import math
from pathlib import Path
from statistics import NormalDist


ROOT = Path(__file__).resolve().parents[2]
N, FAMILY, HISTORY, HISTORY_LIMIT = 5500, 32, 539367, 1_000_000
MIB = 1048576
CATALOGUE_ROOT = 'TestResults/selection-margin-audit-final-20260922'
CLOSEOUT_ROOT = 'TestResults/incumbent-tie-practical-closeout-20260922'
MANIFESTS = {
    CATALOGUE_ROOT: '7884c456b59772676c49008249573fc9191623715869fcc647a3dacd43c7305b',
    CLOSEOUT_ROOT: '0bf81f630c839af5af22ee76cf22f5fafbdcb6838fb5a9cbf783f3816bdd4206',
}
PINS = {
    'Balance Harness/analysis/practical-confirmation-precision.py': '3404dbbd3beca5d86329a631727e30c0278c44341eb9d495c7255dcfef46d1fa',
    'Balance Harness/Tower-Practical-Fixed-Team-Confirmation-Execution.json': 'c7b1b387819810daac4a5149e54b9433643c896ef157a1fa9cc8e3975350f060',
    'LL/tools/BalanceHarness/TowerBalanceEvaluator.cs': '1614b92f07d944a1408a88496ce6d9262a4146965847c922a2fb8f165cc23690',
    'LL/tools/BalanceHarness/TowerFixedTeamConfirmation.cs': '68c6e75e3eebd2841280af3f3b5b89633173e9b0a7a059500041f3268a80fbb0',
    'LL/tools/BalanceHarness/TowerFixedTeamConfirmationArchive.cs': '9b8b26fd2972ecef5b132e95b7dcd7ad5891f2edb11ebc695833696d88072454',
    'LL/tools/BalanceHarness/TowerStudyLimits.cs': '54fb94c0779be8c514721f6a10d978bbd7b3e8079bc10bdeef34edfbeeaaeaca',
}


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def authenticate():
    consumed = {}
    for directory, expected in MANIFESTS.items():
        base = ROOT / directory
        manifest = base / 'files.json'
        require(sha(manifest) == expected, 'Changed evidence manifest: ' + directory)
        members = read(manifest)
        paths = list(base.rglob('*'))
        require(not any(p.is_symlink() or p.is_junction() for p in paths), 'Linked evidence member')
        require({p.relative_to(base).as_posix() for p in paths if p.is_file()}
                == set(members) | {'files.json'}, 'Changed evidence inventory: ' + directory)
        for name, digest in members.items():
            require(sha(base / name) == digest, 'Changed evidence member: ' + name)
            consumed[directory + '/' + name] = digest
        consumed[directory + '/files.json'] = expected
    for name, digest in PINS.items():
        require(sha(ROOT / name) == digest, 'Changed planning source: ' + name)
        consumed[name] = digest
    return consumed


def precision_module():
    path = ROOT / 'Balance Harness/analysis/practical-confirmation-precision.py'
    require(sha(path) == PINS[path.relative_to(ROOT).as_posix()], 'Changed precision implementation')
    spec = importlib.util.spec_from_file_location('fixed_family_precision', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def power_bound(precision, n, gain=.08, minimum_rate=.25, history=HISTORY_LIMIT):
    z = precision.critical_value(FAMILY)
    contrast_cut = max(.05, z * math.sqrt(n + z*z) / n)
    viable_cut = .10 + z * math.sqrt(.09 / n)
    contrast_failure = math.exp(-n * (gain - contrast_cut)**2 / 2) if gain > contrast_cut else 1.
    viability_failure = math.exp(-2*n * (minimum_rate - viable_cut)**2) if minimum_rate > viable_cut else 1.
    iid = max(0., 1 - 2*contrast_failure - viability_failure)
    coupling = n*(n-1) / (2*(2**32-history))
    return {'samplesPerRecipe': n, 'trueGainAtLeastAgainstEachReference': gain,
            'trueCandidateWinRateAtLeast': minimum_rate, 'historicalExclusionBound': history,
            'sufficientObservedGainStrictlyAbove': contrast_cut,
            'sufficientObservedWinRateAtLeast': viable_cut,
            'eachContrastFailureUpper': contrast_failure, 'viabilityFailureUpper': viability_failure,
            'iidJointPassLower': iid, 'finitePopulationCouplingUpper': coupling,
            'uniformWithoutReplacementJointPassLower': max(0., iid-coupling)}


def arithmetic_checks(precision):
    prior = precision.self_check()
    z = precision.critical_value(FAMILY)
    require(abs(z-NormalDist().inv_cdf(1-.025/FAMILY)) < 1e-8, 'Family-32 critical value')
    # Independently enumerate small multinomials, including exact five-point
    # boundaries, and compare the existing recurrence and threshold search.
    cells, maximum_error = 0, 0.
    for n, discordance, delta in ((20, .4, .1), (40, .6, .2), (60, .2, .1)):
        cuts = precision.thresholds(n, FAMILY)
        terms = []
        pg, pl = (discordance+delta)/2, (discordance-delta)/2
        for g in range(n+1):
            for l in range(n-g+1):
                passed = 20*(g-l) >= n and precision.wilson(g,n,FAMILY)[0] > precision.wilson(l,n,FAMILY)[1]
                require(passed == (g >= cuts[g+l]), 'Small-case threshold mismatch')
                cells += 1
                if passed:
                    terms.append(math.comb(n,g)*math.comb(n-g,l)*pg**g*pl**l*(1-discordance)**(n-g-l))
        error = abs(math.fsum(terms)-precision.contrast_probability(n,FAMILY,discordance,delta,cuts))
        maximum_error = max(maximum_error,error)
        require(error < 1e-12, 'Family-32 multinomial mismatch')
    bound = power_bound(precision,N)
    require(bound['uniformWithoutReplacementJointPassLower'] >= .80, 'Conditional power target missed')
    cuts = precision.thresholds(N,FAMILY)
    rows, minimum_lower = 0, 1.
    for discordant in range(N+1):
        first_gain = (discordant + N//20 + 1)//2
        require(cuts[discordant] == (first_gain if first_gain <= discordant else discordant+1),
                'Five-point integer boundary differs')
        if first_gain <= discordant:
            rows += 1
            minimum_lower = min(minimum_lower, precision.wilson(first_gain,N,FAMILY)[0]
                                - precision.wilson(discordant-first_gain,N,FAMILY)[1])
    viable = []
    for wins in range(N+1):
        passed = precision.wilson(wins,N,FAMILY)[0] >= .10
        require(passed == (wins/N >= bound['sufficientObservedWinRateAtLeast']), 'Viability inversion differs')
        if passed:
            viable.append(wins)
    require(min(viable) == 621, 'Viability boundary changed')
    require(power_bound(precision,N,.05)['uniformWithoutReplacementJointPassLower'] == 0,
            'Boundary effect incorrectly claims useful power')
    require(power_bound(precision,N,.08,.10)['uniformWithoutReplacementJointPassLower'] == 0,
            'Unsupported viability alternative')
    require(power_bound(precision,N,history=HISTORY)['uniformWithoutReplacementJointPassLower']
            >= bound['uniformWithoutReplacementJointPassLower'], 'History sensitivity reversed')
    return {'status': 'Passed', 'legacyPrecisionChecks': prior,
            'independentFamily32MultinomialCells': cells, 'maximumMultinomialAbsoluteError': maximum_error,
            'discordanceThresholdRowsChecked': N+1, 'feasibleBoundaryRows': rows,
            'smallestPairedLowerAtFirstQualifyingNetCount': minimum_lower,
            'viabilityCountsChecked': N+1, 'minimumViableWins': min(viable)}


def main():
    consumed = authenticate()
    precision = precision_module()
    checks = arithmetic_checks(precision)
    catalogue = read(ROOT / CATALOGUE_ROOT / 'candidate-catalogue.json')
    closeout = read(ROOT / CLOSEOUT_ROOT / 'closeout.json')
    require(closeout['totalExclusions'] == HISTORY and closeout['historyFiles'] == 227
            and closeout['scope'] == 'Closed', 'Changed recorded history snapshot')
    require(catalogue['seedsAllocated'] == 0 and not catalogue['runnableRequest']
            and not catalogue['adoptionAuthorized'], 'Catalogue is no longer seed-free planning evidence')
    require([c['sourceRestart'] for c in catalogue['candidates']] == [1,11,13,17,22,23], 'Candidate set differs')
    recipes = [{'role': 'Candidate', 'partyId': c['partyId'], 'sourceRestart': c['sourceRestart'],
                'sourceRecipeHash': c['sourceRecipeHash'], 'scenario': c['scenario']}
               for c in catalogue['candidates']]
    recipes += [{'role': 'PrimaryReference' if r['partyId'] == catalogue['primaryPartyId'] else 'OtherReference',
                 'partyId': r['partyId'], 'referenceId': r['referenceId'], 'scenario': r['scenario']}
                for r in catalogue['references']]
    require(len(recipes) == len({r['partyId'] for r in recipes}) == 8, 'Expected eight unique recipes')
    require([r['role'] for r in recipes[6:]] == ['PrimaryReference','OtherReference'], 'Reference order differs')
    require(all(r['scenario']['seeds'] == [] and len(r['scenario']['party']) == 10 for r in recipes),
            'Scheduled or altered cohort')
    require(recipes[6]['scenario'] == catalogue['primaryScenario'], 'Primary reference differs')
    contrasts = [{'candidateId': c['partyId'], 'referenceId': r['partyId']}
                 for c in recipes[:6] for r in recipes[6:]]
    require(len(contrasts) == 12 and len(recipes)+2*len(contrasts) == FAMILY, 'Decision family differs')
    chunks = [1000]*5+[500]
    fights = N*len(recipes)
    require(sum(chunks) == N and max(chunks) <= 1000 and fights == 44000, 'Transport accounting differs')
    fixed = read(ROOT / 'Balance Harness/Tower-Practical-Fixed-Team-Confirmation-Execution.json')
    obs = fixed['observation']
    require(fixed['receipt']['status'] == 'CompletedVerified' and obs['completion']['fights'] == 16500,
            'Resource reference is incomplete')
    phases = {'admission': {'seconds':240, 'bytes':256*MIB},
              'combat': {'seconds':3600, 'bytes':2304*MIB},
              'audit': {'seconds':1800, 'bytes':256*MIB}}
    closeout_cap, overhead_cap = {'seconds':60,'bytes':16*MIB}, {'seconds':300,'bytes':240*MIB}
    for key, total in [('seconds',6000),('bytes',3072*MIB)]:
        require(sum(p[key] for p in phases.values())+closeout_cap[key]+overhead_cap[key] == total,
                'Resource allocation does not sum to total')
    old_other_seconds = fixed['receipt']['measuredSecondsAfterSealingBeforeReceipt'] - sum(
        obs[p+'-phase']['measuredSeconds'] for p in phases)
    proxies = {
        'admission': max(obs['admission-phase']['measuredSeconds'], closeout['processes']['preflight']['seconds']),
        'combat': obs['combat-phase']['measuredSeconds']*fights/16500,
        'audit': max(obs['audit-phase']['measuredSeconds']*fights/16500,
                     sum(closeout['processes'][p]['seconds'] for p in ('public-audit','independent-audit'))
                     * fights/closeout['completedFights']),
        'other': old_other_seconds,
    }
    combat_bytes = obs['combat-phase']['observedBytes']*fights/16500
    other_bytes = fixed['combinedBytes']-obs['combat-phase']['observedBytes']
    for phase in phases:
        require(2*proxies[phase] < phases[phase]['seconds'], 'Twofold time illustration exceeds phase')
    require(2*proxies['other'] < overhead_cap['seconds'] and 2*sum(proxies.values()) < 6000,
            'Twofold enclosing illustration exceeds proposal')
    require(2*combat_bytes < phases['combat']['bytes'] and 2*(combat_bytes+other_bytes) < 3072*MIB,
            'Twofold storage illustration exceeds proposal')
    bound = power_bound(precision,N)
    plan = {
        'version':'tower-practical-fixed-family-confirmation-plan-v1',
        'status':'ProtocolSpecifiedImplementationRequired', 'runnableRequest':False,
        'gameplayAuthorized':False, 'resourceGrant':False,
        'question':'Which, if any, of these six exact candidates meet the practical strength criterion against both fixed references?',
        'proposedNativeVersion':'tower-practical-fixed-family-confirmation-v1',
        'recipesInFixedExecutionOrder':recipes, 'contrastsInFixedReportingOrder':contrasts,
        'sourceCatalogueSha256':sha(ROOT/CATALOGUE_ROOT/'candidate-catalogue.json'),
        'contentHashes':catalogue['contentHashes'], 'settingsHash':catalogue['settingsHash'],
        'sourceExecutionHash':catalogue['sourceExecutionHash'], 'budget':catalogue['budget'],
        'ownedCopies':catalogue['ownedCopies'],
        'futureRuntimeBinding':'Required after adapter implementation; preserve captured gameplay/content/settings, not current live checkout gameplay.',
        'samplesPerRecipe':N, 'maximumAttemptedFights':fights, 'discoveryFights':0, 'selectionFights':0,
        'endpoint':{
            'win':'Victory only; defeat and draw are non-wins. Missing, faulted or invalid reports are terminal incompleteness, never non-wins.',
            'intervalFamily':FAMILY, 'familyComposition':'8 recipe win rates + 2 paired gain/loss rates for each of 12 candidate/reference contrasts',
            'nominalFamilyCoverage':.95, 'coverageIsApproximate':True, 'criticalValue':precision.critical_value(FAMILY),
            'minimumCandidateWilsonLower':.10, 'minimumCandidateWins':621,
            'minimumObservedGainAgainstEachReference':.05, 'minimumNetGainsAgainstEachReference':275,
            'pairedInterval':'[WilsonLower(G)-WilsonUpper(L), WilsonUpper(G)-WilsonLower(L)] with n=5500 and family=32 throughout',
            'pairedLowerAgainstEachReference':'strictly positive',
            'qualification':'Legal frozen candidate, complete verified family, viability, and both signed contrast gates must all pass.',
            'reporting':'Publish all 8 rates, 12 contrasts, every qualification flag and both unchanged control identities. One final look only.',
            'success':'Recommend every qualifying candidate in frozen catalogue order as a stronger fixed-cohort option; retain both references as controls.',
            'multipleQualifiers':'No estimated-best winner, new primary, candidate/candidate superiority or unique optimum. Candidate/candidate contrasts are outside this protocol.',
            'completeNegative':'Keep the designated incumbent and both supplied reference recommendations; strength not demonstrated is not equivalence.',
            'incomplete':'No new recommendation, even if an observed subset appears to pass. Preserve all exposed values and evidence; scope terminates.',
            'balanceAssessment':'NotAssessed', 'searchReliability':'Unresolved'},
        'power':{
            'target':.80,
            'claimScope':'For any one specified candidate satisfying the alternatives, lower bound for that candidate passing both references and viability; implies at least one qualifies if such a candidate exists. Not probability all six qualify.',
            'alternative':'At least eight true percentage points over each reference and true candidate win probability at least 25%; planning assumptions, not estimates or forecasts for these recipes.',
            'boundAtConservativeHistoryCeiling':bound,
            'boundAtRecordedHistory':power_bound(precision,N,history=HISTORY),
            'assumptions':['One post-freeze cryptographic batch modeled as independent uniform bits.',
                'Uniform ordered sample without replacement from eligible signed Int32 values, conditional on a full panel and operational completion.',
                'IID Hoeffding bound for each contrast in [-1,1] and viability in [0,1], then union bound and one finite-population collision-coupling penalty.',
                'No independence assumed between references or between candidates; common-seed dependence is preserved.',
                'No probability assigned to legal preparation, successful full batch, completion or the existence of a truly useful candidate.'],
            'smallestIntegerNForThisSufficientBound':next(n for n in range(256,N+1)
                if power_bound(precision,n)['uniformWithoutReplacementJointPassLower'] >= .80),
            'sampleSizeSensitivity':[power_bound(precision,n) for n in (1000,4000,5000,5152,5500,6000)],
            'effectSensitivity':[power_bound(precision,N,d) for d in (.05,.06,.07,.08,.10)],
            'maximumRecipeRateWilsonHalfWidth':precision.critical_value(FAMILY)/(2*math.sqrt(N+precision.critical_value(FAMILY)**2)),
            'maximumPairedWilsonIntervalHalfWidth':precision.critical_value(FAMILY)/math.sqrt(N+precision.critical_value(FAMILY)**2),
            'meaning':'Adjusted positive lower bounds support positive gain, not proof that true gain exceeds five points. Zero lower power bounds are uninformative.'},
        'sampling':{
            'recordedExclusions':HISTORY, 'recordedLedgerFiles':227, 'historyLimitIncludingReservations':HISTORY_LIMIT,
            'maximumAdmissionHistory':HISTORY_LIMIT-2*N, 'maximumResultingUnionAtRecordedHistory':HISTORY+2*N,
            'completeHistoryRefreshRequired':True, 'freezeBeforeEntropy':True,
            'cryptographicFillCalls':1, 'batchBytes':8*N, 'batchWords':2*N, 'acceptedPanelValues':N,
            'maximumNewReservations':2*N,
            'rule':'Read signed little-endian words in order; reject history and within-batch duplicates, choose first 5500 eligible distinct words; reserve all eligible unused tail values too.',
            'shortfall':'Terminal incomplete; no refill, fallback PRNG, replacement batch or reusing historical/unused values.',
            'durability':'Freeze owner/request/recipes/runtime/history and native admission before durable Pending and entropy intent/start; persist full batch and completion before cancellation; unresolved entropy start blocks future allocation.',
            'legacyRecovery':'Preserve the authenticated existing AbandonedPermanentlyReserved receipt. Old recovery protocols must reject this new version; no automatic new recovery or resume.'},
        'execution':{
            'implemented':False, 'nativeScenarioSeedLimit':1000, 'panelChunkSizes':chunks,
            'transportRecipeCount':len(chunks)*len(recipes),
            'order':'Six candidates in source-restart order 1,11,13,17,22,23; then designated incumbent, then other reference; each uses the same six consecutive panel slices.',
            'chunkSemantics':'Only Scenario.Seeds changes. Original scenario/build IDs, actors, canonical ability order, gear, subgroup, time and preparation stay exact. Chunks are transport partitions, not statistical looks.',
            'attempts':'Exactly 44000 distinct ordinal attempts and bound saved reports on success. No cached outcomes, replay, search, candidate edits, pruning, retry, resume, refill, extension or interim outcome-dependent decisions.',
            'integrity':'Owned registry/output leases and watched process; existing output is refusal. Stop on legal/runtime/content/history mismatch, invalid/missing/extra report, attempt mismatch, resource limit or ownership failure.',
            'audits':'Native reconstruction of all 48 transport inputs and 44000 reports without combat, plus an independent direct-outcome audit of 8 ordered rows and all 12 contrasts. Match seed by identity and ordinal, never silently zip reordered rows.',
            'publication':'Only agreeing complete audits and rechecked history permit recommendation, all-rate/all-contrast results and seed-free exports; seal exact inventories and enclosing receipts.',
            'compatibility':'Separate versioned adapter; do not reinterpret the existing three-team, family-seven command or its archives.'},
        'resources':{
            'additionalProposal':{'seconds':6000,'bytes':3072*MIB}, 'phases':phases,
            'closeoutReserve':closeout_cap, 'enclosingSetupHeadroom':overhead_cap,
            'grant':False, 'fullWorkflowFeasibilityEstablished':False,
            'scope':'All admission/setup, runtime/content copies, reservation, attempts/reports, both audits, transient writes, publication, logs, cleanup and closeout; limits apply to enclosing wall time and storage high-water.',
            'cumulativeRule':'Before admission reconcile unique applicable prior charge receipts. Required finite nonnegative PriorSeconds/PriorBytes plus 6000/3221225472 define the frozen cumulative ceilings. Fully charge closed allowances, not measured consumption; no refunds, double counting, transfer or zero-default prior costs.',
            'priorAccountingStatus':'Final numeric prior/cumulative totals require the later implementation/admission ledger; these planning inputs are not a complete global charge ledger.',
            'knownChargesToPreserve':[
                {'scope':'Earlier fixed-team chain cumulative through 17 September','seconds':fixed['receipt']['cumulativeChargedSeconds'],'bytes':fixed['receipt']['cumulativeChargedBytes']},
                {'scope':'Latest practical execution additional','seconds':closeout['originalMaximumSeconds'],'bytes':closeout['originalMaximumBytes']},
                {'scope':'Latest practical read-only correction additional','seconds':closeout['engineeringMaximumSeconds'],'bytes':closeout['engineeringMaximumBytes']}],
            'illustrationsNotBounds':{
                'sourceFixedTeamFights':16500,'sourceFixedTeamSeconds':fixed['receipt']['measuredSecondsAfterSealingBeforeReceipt'],
                'sourceFixedTeamBytes':fixed['combinedBytes'], 'phaseSeconds':proxies,
                'totalSeconds':sum(proxies.values()), 'twofoldSeconds':2*sum(proxies.values()),
                'scaledCombatBytes':combat_bytes, 'fixedOtherBytes':other_bytes,
                'totalBytes':combat_bytes+other_bytes, 'twofoldBytes':2*(combat_bytes+other_bytes),
                'method':'Scale actual 16500-fight fixed-team combat and combat-phase storage by 44000/16500; retain other bytes. For audit use the larger of scaled old fixed-team audit and both latest saved-report audits scaled by 44000/3496. Admission uses larger recorded admission/preflight; fixed-team enclosing overhead stays fixed.',
                'limits':'Linear/twofold sizing assumptions only. New controller, report sizes, native preparation, history/journal growth and cleanup can exceed them; capped failure remains possible.'},
            'admissionRequirements':'Implement and verify adapter, pin final runtime/request, reconcile cumulative receipts, assess resource risk and admit before entropy. This plan performs no resource probe or native preparation.'},
        'requiredImplementationChecks':[
            'Eight exact unique frozen recipes/roles/order, captured content/settings/gameplay and legal preparation; reject substitutions, altered ability order, arbitrary extra candidates and unknown schemas.',
            'Family 32 and all 12 signed contrasts; exact 621 viability and 275 net-gain boundaries, both-reference requirement, negative/equal results and complete zero/one/multiple-qualifier outputs without picking a new primary.',
            'Six shared consecutive chunks per recipe, 48 bindings, 1000 per-input cap, 44000 ordinal reports; reject duplicate/missing/reordered/extra trials and seed/input/cache/report tampering.',
            'One batch; exclusions, collisions, duplicate words, unused tail, shortfall and every Pending/intent/start/completion interruption; reserve exposed values even on failure.',
            'Native and separately implemented direct-row reconstruction agree; neither can invoke combat/entropy; incomplete subsets cannot qualify.',
            'Owned process termination, registry lease, immutable output, nontransferable phase and enclosing/cumulative limits, publication interruptions and legacy archive/command parity.',
            'Backend fixtures through build/run-tests.ps1; capture actual producing runtime and fixture resource observations before preparing a separately bounded native request.'],
        'arithmeticChecks':checks, 'authenticatedInputs':consumed,
        'plannerSha256':sha(Path(__file__)),
        'newFights':0,'newReservations':0,'randomDraws':0,'nativeReconstructions':0,
        'currentRecommendationUnchanged':True,'balancePolicyUnchanged':True,
        'v19RequiredRecipes':253,'v19UnusedValues':512,'v19Status':'Unresolved',
    }
    require(authenticate() == consumed, 'Evidence changed during planning')
    return plan


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group()
    group.add_argument('--self-test', action='store_true')
    group.add_argument('--check', type=Path)
    args = parser.parse_args()
    if args.self_test:
        output = arithmetic_checks(precision_module())
    else:
        output = main()
        if args.check:
            require(output == read(args.check), 'Saved plan differs from authenticated reproduction')
            output = {'status':'Passed','plan':args.check.as_posix(),'planSha256':sha(args.check),
                      'authenticatedFiles':len(output['authenticatedInputs']),
                      'arithmeticChecks':output['arithmeticChecks'],'newFights':0,'newReservations':0}
    print(json.dumps(output,indent=2,allow_nan=False))
