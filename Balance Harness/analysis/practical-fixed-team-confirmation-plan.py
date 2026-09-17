"""Deterministic fixed-team planning; no engine, allocations, random draws or writes.

Authenticates prior evidence; computes prospective sufficient-event power bounds
and qualified resource illustrations. Stdout is a planning artifact, not a native
request. No historical result is passed through a new strength endpoint.
"""
import hashlib
import importlib.util
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUN = ROOT/'TestResults/balance/tower-practical-selection-diagnostic-20260917'
N, FAMILY, HISTORY = 5500, 7, 486374
MIB = 1048576
PINS = {
    'Balance Harness/analysis/practical-primary-adoption-readiness.py': '9cf04c19738619944a095c7e1b6ff42c8f24f87506a69dee1537cefd8b872c39',
    'Balance Harness/Tower-Practical-Primary-Adoption-Readiness.json': '15256670a24f1979b531604ed4bddca102120447cc0f4324015a14d6914c2aa4',
    'Balance Harness/analysis/practical-confirmation-precision.py': '3404dbbd3beca5d86329a631727e30c0278c44341eb9d495c7255dcfef46d1fa',
    'LL/tools/BalanceHarness/TowerBalanceRuns.cs': '4f11b29a3810e94492ce4b348fc600e0ce5d11af1da4ebbf9b063c9fbd4e73d0',
    'LL/tools/BalanceHarness/TowerCompactBalanceRun.cs': '7857726f7aa2848a914e234f9869eea09035d53470cc582a091955cb235d6a5a',
    'LL/tools/BalanceHarness/TowerLoadoutArchive.cs': '0fa3cecc9831140856dbb8414675b5fde57fda11f2421317bee7ac7c3378d656',
    'LL/tools/BalanceHarness/TowerBattleRunner.cs': '5ca9d52bd850163e00a201b3f13e75fa925e196e64294c42d9253a85bb3b1278',
    'LL/tools/BalanceHarness/TowerSelectionDiagnosticRun.cs': '4a8c9c0d7b29f42234f8ca2a7a95e57cb13863556ffbe92c40d46a4a65473231',
    'LL/tools/BalanceHarness/TowerSelectionDiagnosticReservation.cs': '4bd51c3e403bfc5a56c6ee7f68cfdef6fa8c66d1f21d690e4955b6f397153337',
}


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def power_bound(precision, n, gain, minimum_rate=.25):
    z = precision.critical_value(FAMILY)
    cutoff = max(.05, z*math.sqrt(n+z*z)/n)
    viable_cutoff = .10+z*math.sqrt(.10*.90/n)
    contrast_failure = math.exp(-n*(gain-cutoff)**2/2) if gain > cutoff else 1.
    viability_failure = math.exp(-2*n*(minimum_rate-viable_cutoff)**2) if minimum_rate > viable_cutoff else 1.
    iid = max(0., 1-2*contrast_failure-viability_failure)
    coupling = n*(n-1)/(2*(2**32-HISTORY))
    return {'samplesPerTeam': n, 'trueGainAtLeastAgainstEachAnchor': gain,
        'trueCandidateWinRateAtLeast': minimum_rate, 'sufficientObservedGainStrictlyAbove': cutoff,
        'sufficientObservedWinRateAtLeast': viable_cutoff,
        'eachContrastFailureUpper': contrast_failure, 'viabilityFailureUpper': viability_failure,
        'iidJointPassLower': iid, 'finitePopulationCouplingUpper': coupling,
        'uniformWithoutReplacementJointPassLower': max(0., iid-coupling)}


def main():
    for name, digest in PINS.items():
        require(sha(ROOT/name) == digest, 'Changed planning source: '+name)
    readiness = load('fixed_plan_readiness', ROOT/'Balance Harness/analysis/practical-primary-adoption-readiness.py').review()
    require(readiness == read(ROOT/'Balance Harness/Tower-Practical-Primary-Adoption-Readiness.json'), 'Readiness review differs.')
    precision = load('fixed_plan_precision', ROOT/'Balance Harness/analysis/practical-confirmation-precision.py')
    checks = precision.self_check()
    bound = power_bound(precision, N, .08)
    require(bound['uniformWithoutReplacementJointPassLower'] >= .80, 'Conditional target missed.')
    # At each possible discordance, check the first count satisfying the analytic
    # sufficient event against the exact integer gate. Monotonicity covers larger g.
    thresholds = precision.thresholds(N, FAMILY)
    checked = 0
    for discordant in range(N+1):
        gained = math.floor((discordant+N*bound['sufficientObservedGainStrictlyAbove'])/2)+1
        if gained <= discordant:
            require(gained >= thresholds[discordant], 'Sufficient-event threshold does not pass.')
            checked += 1
    minimum_wins = next(k for k in range(N+1) if precision.wilson(k, N, FAMILY)[0] >= .10)
    for k in range(N+1):
        require((precision.wilson(k, N, FAMILY)[0] >= .10)
                == (k/N >= bound['sufficientObservedWinRateAtLeast']), 'Viability inversion differs.')
    # Necessary single-contrast power for a permitted high-discordance population:
    # this counterexample shows that 1,000 is not a uniform 80% joint-power guarantee.
    one_thousand = precision.contrast_probability(1000, FAMILY, 1., .08)
    require(one_thousand < .80, 'Counterexample no longer rules out an 80% uniform claim.')
    teams = read(RUN/'teams.json')
    frozen = read(RUN/'study/nominees-freeze.json')
    selected = [next(t for t in teams['teams'] if t['primary'])]
    for reference in ('team-040e60d3dbc5c127321653c47ed3a9d3', 'team-49f6979895354870c89362d4abf214bb'):
        selected.append(next(t for t in teams['teams'] if t['referenceIds'] == [reference]))
    recipes = []
    for index, team in enumerate(selected):
        source = next(n for n in frozen['nominees'] if n['partyId'] == team['partyId'])
        require(team['scenario'] == source['scenario'] and team['scenario']['seeds'] == [], 'Seed-free recipe differs.')
        recipes.append({'role': ('Candidate', 'Anchor040e', 'Anchor49f6')[index], 'partyId': team['partyId'],
            'sourceFreezeRecipeHash': source['recipeHash'], 'referenceIds': team['referenceIds'],
            'scenario': team['scenario'], 'subgroups': team['subgroups'], 'requiredCopies': team['requiredCopies']})
    execution = read(ROOT/'Balance Harness/Tower-Practical-Selection-Diagnostic-Execution.json')
    obs = execution['observation']
    battles = [(RUN/n).stat().st_size for n in read(RUN/'files.json') if n.startswith('study/battles/')]
    require(len(battles) == 4640, 'Changed measured report count.')
    fights = 3*N
    chunks = [1000]*5+[500]
    require(sum(chunks) == N and max(chunks) <= 1000 and len(chunks)*3 == 18, 'Native scenario partition differs.')
    observed_overhead = execution['receipt']['measuredSecondsAfterSealingBeforeReceipt']-sum(
        obs[p+'-phase']['measuredSeconds'] for p in ('admission','search','confirmation','audit'))
    time_proxy = {'admission': obs['admission-phase']['measuredSeconds'],
        'combat': obs['confirmation-phase']['measuredSeconds']*fights/4000,
        'audit': obs['audit-phase']['measuredSeconds']*fights/4640,
        'other': observed_overhead}
    other_bytes = execution['combinedBytes']-sum(battles)
    byte_proxy = other_bytes+max(battles)*fights
    phases = {'admission': {'seconds':180,'bytes':128*MIB}, 'combat': {'seconds':1440,'bytes':768*MIB},
        'audit': {'seconds':600,'bytes':64*MIB}}
    require(sum(p['seconds'] for p in phases.values())+2 < 2400
            and sum(p['bytes'] for p in phases.values())+4*MIB < 1024*MIB, 'Phase accounting differs.')
    require(2*sum(time_proxy.values()) < 2400 and 2*byte_proxy < 1024*MIB, 'Twofold stress illustration exceeds proposed total.')
    for phase in ('admission','combat','audit'):
        require(2*time_proxy[phase] < phases[phase]['seconds'], 'Twofold phase-time illustration exceeds proposal.')
    require(2*max(battles)*fights < phases['combat']['bytes'], 'Twofold battle storage exceeds combat proposal.')
    return {
        'version': 'tower-practical-fixed-team-confirmation-plan-v1',
        'status': 'ProtocolSpecifiedImplementationRequired', 'runnableRequest': False, 'gameplayAuthorized': False,
        'question': 'Does this exact fixed candidate satisfy the existing practical strength criterion against both anchors on fresh evidence?',
        'recipesInFixedExecutionOrder': recipes, 'sourceTeamExportSha256': sha(RUN/'teams.json'),
        'contentHashes': read(RUN/'source-definition.json')['contentHashes'],
        'settingsHash': read(RUN/'source-definition.json')['settingsHash'],
        'referenceExecutionHash': read(RUN/'source-definition.json')['executionHash'],
        'futureProducingRuntimeBinding': 'Must be explicit after implementation; existing reference hash is evidence, not a runnable new binding.',
        'samplesPerTeam': N, 'maximumAttemptedFights': fights, 'discoveryFights':0, 'selectionFights':0,
        'intervalFamily': FAMILY, 'nominalFamilyCoverage': .95, 'coverageIsApproximate': True,
        'strengthGate': {'supportedCandidateWinRate': .10, 'minimumObservedGainEachAnchor': .05,
            'minimumNetGainsEachAnchor': N//20, 'pairedLowerEachAnchor': 'strictly greater than zero',
            'minimumCandidateWinsForViability': minimum_wins},
        'success': 'Recommend this candidate as a stronger fixed-cohort option; retain both original anchors as controls. No method-reliability or balance-pass claim.',
        'failure': 'Keep Hold and anchor recommendations; complete negative closes scope. Incomplete is never a scientific pass.',
        'stopping': 'One complete fixed panel; no outcome peeking decisions, interim success/futility, retries, refill, replacement candidates, replay, resume or extensions.',
        'power': {'target': .80, 'alternativeJustification': 'Eight points is a planning alternative near prior observed gains, not an estimate treated as known truth.',
            'bound': bound, 'assumptions': ['True candidate gain at least .08 against each anchor.',
                'True candidate win probability at least .25.', 'IID uniform eligible-value model, then conservative coupling to uniform sampling without replacement.',
                'No independence assumption between contrasts; union bound covers both and candidate viability.',
                'Operational completion is conditional and not assigned a probability.'],
            'smallestIntegerNForThisBoundAtCurrentHistory': next(n for n in range(256,N+1)
                if power_bound(precision,n,.08)['uniformWithoutReplacementJointPassLower'] >= .80),
            'sampleSizeSensitivity': [power_bound(precision,n,.08) for n in (256,1000,2000,4000,5000,N,6000)],
            'effectSensitivity': [power_bound(precision,N,d) for d in (.05,.06,.07,.08,.10,.12)],
            'permittedCounterexampleAt1000': {'discordance':1.,'trueGain':.08,'singleNecessaryContrastPassProbability':one_thousand,
                'model':'IID paired outcomes; the finite-population coupling penalty at N=1000 is 0.000116313 or less.',
                'meaning':'Under the IID model, joint pass probability cannot exceed this necessary contrast probability.'}},
        'sampling': {'historicalExclusionsAtPlanning': HISTORY, 'eligibleSignedInt32Population': 2**32-HISTORY,
            'freezeBeforeEntropy': True, 'cryptographicFillCalls':1, 'batchWords':2*N, 'batchBytes':8*N,
            'acceptedPanelValues':N, 'maximumNewReservations':2*N, 'maximumResultingUnionAtThisHistory':HISTORY+2*N,
            'rule':'Parse signed little-endian words once; exclude complete history and repeated words; panel is first 5500 eligible distinct values; reserve every eligible tail value.',
            'shortfall':'Terminal incomplete, no refill; exposed values remain excluded.',
            'pending':'Durable owned Pending and entropy intent/start before draw; completion and full batch before cancellation; unresolved draw blocks allocation; existing recovery formats must reject this new protocol.'},
        'executionDesign': {'proposedContract':'tower-practical-fixed-team-confirmation-v1',
            'implemented':False,'route':'Thin fixed-recipe controller over TowerLoadoutArchive gzip reports, TowerBattleRunner preparation and the diagnostic ownership/history/phase primitives.',
            'nativeScenarioSeedLimit':1000,'panelChunkSizes':[1000,1000,1000,1000,1000,500],
            'transportRecipeCount':18,
            'transportOrder':'Candidate then 040e then 49f6; each uses the same six consecutive slices of the one frozen 5500-value panel. Only Scenario.Seeds varies by slice; all other fields, including scenario/build IDs, stay exact.',
            'chunkSemantics':'Execution partitions only: no interim statistical looks, optional stages, extra samples or per-chunk family correction. Combine all six slices into the original one 5500-trial cell per team.',
            'compactBalanceAlternative':'Can execute fixed families, but reports the 10–50% balance endpoint and lacks this integrated strength/sampling contract; no invocation of it alone establishes adoption.',
            'verification':'Native reconstruction of all inputs/recipes/reports and separate direct-outcome count audit; both forbid combat/entropy and must agree before exact-inventory publication.'},
        'resources': {'additionalOperationalProposal': {'seconds':2400,'bytes':1024*MIB}, 'phases':phases,
            'closeoutReserve': {'seconds':2,'bytes':4*MIB}, 'otherSetupHeadroom': {'seconds':178,'bytes':60*MIB},
            'grant':False, 'completeWorkflowFeasibilityEstablished':False,
            'closedDiagnosticChainCharges': {'seconds':1860,'bytes':1088*MIB},
            'illustrativeCumulativeWithNoOtherInterveningCharges': {'seconds':4260,'bytes':2112*MIB},
            'priorAccounting':'Carry all applicable closed/intervening charges at admission; no refund, transfer or silent zero prior cost. Proposal is a fresh additional envelope.',
            'measuredSource': {'fights':4640,'enclosingSeconds':execution['receipt']['measuredSecondsAfterSealingBeforeReceipt'],
                'combinedBytes':execution['combinedBytes'],'battleBytes':sum(battles),'largestBattleBytes':max(battles),'otherBytes':other_bytes},
            'illustrationsNotBounds': {'phaseSeconds':time_proxy,'totalSeconds':sum(time_proxy.values()),
                'fixedOtherPlusObservedMaxReportBytes':byte_proxy,
                'twofoldSeconds':2*sum(time_proxy.values()),'twofoldBytes':2*byte_proxy},
            'limits':'No guaranteed speed/size bound. The full-panel ledger, 18 chunk recipe bindings, journals, changed controller/runtime, transient files and failure cleanup require implementation evidence or explicit capped-risk acceptance. Per-input seed lists never exceed the measured 1000-value size.'},
        'requiredImplementationChecks': ['Exact three frozen recipes and unchanged cohort; reject reselection, altered pool/gear/order and unknown schema.',
            'Family-seven gate parity, signed contrast orientation, 275-net-gain boundary, viability, equality/negative/incomplete results.',
            'One-batch history/collision/duplicate/tail/shortfall cases and every Pending/entropy/binding interruption.',
            'Exact 16500 attempts, no cache reuse, incomplete/reordered/extra reports and manifest tampering.',
            'Six fixed panel slices per team (five 1000, one 500); exact 18 transport bindings; invariant seed-free scenarios, IDs and preparation; reconstruct full paired order without intermediate statistical looks.',
            'Native reconstruction and independent counts cannot call combat or entropy; mismatches prevent recommendation.',
            'Owned process death, phase/cumulative time and storage, immutable output, no resume; preserve existing practical/diagnostic behavior.',
            'Use build/run-tests.ps1 with literal reports; record final producing runtime and resource observations before proposing gameplay admission.'],
        'arithmeticChecks': {**checks, 'sufficientDiscordanceRowsChecked':checked,'viabilityCountsChecked':N+1},
        'pins':PINS, 'newFights':0,'newReservations':0,'randomDraws':0,'nativeReconstructions':0,
        'balancePolicyUnchanged':True,'adoption':'Hold','v19RequiredRecipes':253,'v19UnusedValues':512,'searchReliability':'Unresolved',
    }


if __name__ == '__main__':
    print(json.dumps(main(), indent=2, allow_nan=False))
