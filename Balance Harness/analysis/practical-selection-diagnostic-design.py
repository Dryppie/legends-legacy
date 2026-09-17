"""Deterministic precision and resource assessment; no experiment or allocation.

Reuse the existing Wilson/multinomial arithmetic without its fixture-reading main.
Read only sealed resource receipts, manifests and file bytes for integrity/size.
No gameplay, outcomes, seeds, random draws, fitting or native reconstruction.
Stdout is the reproducible JSON artifact; this script writes no files.
"""

import hashlib
import json
import math
from pathlib import Path
import runpy
from statistics import NormalDist


ROOT = Path(__file__).resolve().parents[2]
PRECISION_SOURCE = Path(__file__).with_name("practical-confirmation-precision.py")
P = runpy.run_path(str(PRECISION_SOURCE))
N = 1000
FAMILY = 10
TRUE_GAIN = .16
OBSERVED_GAIN = .05
VIABILITY = .10
POWER_TARGET = .80
HISTORICAL_VALUES = 484285
PRECONFIRMATION_VALUES = 1 + 8 + 32
PACKAGE = "TestResults/practical-native-verification-20260917"
RUN = "TestResults/balance/tower-practical-native-verification-20260917"
PINS = {
    PACKAGE + "/files.json": "2c8415a62bdf6d72451a6468be5ba0c7ea78a10744e0688516a796690b921e15",
    RUN + "/files.json": "54a3a877b209207bc66edd027396e7116e79acbae379639c513990d1a37f3db3",
}


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def cutoffs(n):
    z = P["critical_value"](FAMILY)
    return max(OBSERVED_GAIN, z * math.sqrt(n + z*z) / n), VIABILITY + z * math.sqrt(VIABILITY * (1-VIABILITY) / n)


def bound(n, delta, minimum_win_rate=None):
    """At least one fixed other nominee is delta stronger than the primary.

    For this OR diagnostic, success of that nominee suffices. There is no
    independence assumption or extra failure factor for the other two contrasts.
    A true gain delta implies the other nominee's win probability is >= delta.
    """
    contrast, viability = cutoffs(n)
    minimum_rate = max(delta, minimum_win_rate or 0)
    contrast_failure = math.exp(-n * (delta-contrast)**2 / 2) if delta > contrast else 1.
    viability_failure = math.exp(-2*n * (minimum_rate-viability)**2) if minimum_rate > viability else 1.
    return {"samples": n, "trueGainAtLeast": delta, "impliedOrAssumedWinRateAtLeast": minimum_rate,
            "contrastFailureUpper": contrast_failure, "viabilityFailureUpper": viability_failure,
            "conditionalDiagnosticPassLower": max(0., 1-contrast_failure-viability_failure)}


def check_math():
    base = P["self_check"]()
    z = P["critical_value"](FAMILY)
    require(abs(z-NormalDist().inv_cdf(1-.025/FAMILY)) < 1e-8, "Changed critical value.")
    intervals = [P["wilson"](k, N, FAMILY) for k in range(N+1)]
    thresholds = P["thresholds"](N, FAMILY)
    contrast, viability = cutoffs(N)
    pairs = sufficient = 0
    for gained in range(N+1):
        for lost in range(N-gained+1):
            passed = 20*(gained-lost) >= N and intervals[gained][0] > intervals[lost][1]
            require(passed == (gained >= thresholds[gained+lost]), "Threshold disagrees with gate.")
            if (gained-lost)/N > contrast:
                require(passed, "Invalid sufficient event.")
                sufficient += 1
            pairs += 1
    for k in range(N+1):
        require((intervals[k][0] >= VIABILITY) == (k/N >= viability), "Viability cutoff disagreement.")
    minimum_wins = next(k for k in range(N+1) if intervals[k][0] >= VIABILITY)
    require(bound(N, TRUE_GAIN)["conditionalDiagnosticPassLower"] > POWER_TARGET, "Conditional target not reached.")
    # Independently enumerate a small multinomial with family 10. No simulation.
    n, q, delta = 24, .6, .2
    gain, loss = (q+delta)/2, (q-delta)/2
    direct = []
    for g in range(n+1):
        for l in range(n-g+1):
            if 20*(g-l) >= n and P["wilson"](g,n,FAMILY)[0] > P["wilson"](l,n,FAMILY)[1]:
                direct.append(math.comb(n,g)*math.comb(n-g,l)*gain**g*loss**l*(1-q)**(n-g-l))
    error = abs(math.fsum(direct)-P["contrast_probability"](n,FAMILY,q,delta))
    require(error < 1e-12, "Multinomial summation disagreement.")
    return {"priorArithmetic": base, "feasibleGainLossPairs": pairs, "sufficientGainLossPairs": sufficient,
            "viabilityCounts": N+1, "minimumViabilityWins": minimum_wins,
            "family10DirectMultinomialError": error}


def sensitivities():
    results = []
    for n in (256,512,N):
        cuts = P["thresholds"](n,FAMILY)
        minimum_wins = next(k for k in range(n+1) if P["wilson"](k,n,FAMILY)[0] >= VIABILITY)
        # This additional condition is a sensitivity assumption, not a gate.
        viability_failure = math.fsum(P["binomial"](n,.25)[:minimum_wins])
        for q in (.25,.5,.6,1.):
            for delta in (.05,.10,.15,.16):
                probability = P["contrast_probability"](n,FAMILY,q,delta,cuts)
                require(probability+1e-12 >= 1-bound(n,delta)["contrastFailureUpper"], "Contrast lower-bound check failed.")
                results.append({"samples":n,"discordance":q,"trueGain":delta,
                                "singleContrastPassProbability":probability,
                                "diagnosticPassLowerIfThisNomineeWinRateAtLeast25Percent":max(0.,probability-viability_failure)})
    return results


def resources():
    pins = dict(PINS)
    manifests = {}
    byte_groups = {"battleRecords":[], "otherRetained":[]}
    for directory in (PACKAGE,RUN):
        path = ROOT/directory
        require(sha(path/"files.json") == PINS[directory+"/files.json"], "Changed sealed manifest.")
        manifest = json.loads((path/"files.json").read_text(encoding="utf-8-sig"))
        actual = {p.relative_to(path).as_posix() for p in path.rglob("*") if p.is_file()}
        require(actual == set(manifest)|{"files.json"}, "Changed retained inventory.")
        manifests[directory] = manifest
        for name, expected in manifest.items():
            require(sha(path/name) == expected, "Changed retained file: "+name)
            group = "battleRecords" if directory==RUN and name.startswith("study/battles/") else "otherRetained"
            byte_groups[group].append((path/name).stat().st_size)
        byte_groups["otherRetained"].append((path/"files.json").stat().st_size)
    name = PACKAGE+"/completion.json"
    pins[name] = manifests[PACKAGE]["completion.json"]
    completion = json.loads((ROOT/name).read_text(encoding="utf-8-sig"))
    require(completion["scope"]=="Closed" and completion["totalExclusions"]==HISTORICAL_VALUES,
            "Changed native closeout.")
    old_fights = completion["completedFights"]
    require(old_fights==1408 and len(byte_groups["battleRecords"])==old_fights, "Missing predecessor battle file.")
    battles = sum(byte_groups["battleRecords"])
    other = sum(byte_groups["otherRetained"])
    total = battles+other
    largest = max(byte_groups["battleRecords"])
    proposed_fights = 640+4*N
    seconds = completion["measuredSeconds"]
    # These are explicit stress assumptions, never measured throughput or bounds.
    linear_time = seconds*proposed_fights/old_fights
    observed_size_proxy = other+largest*proposed_fights
    reference_seconds, reference_bytes = completion["chargedSeconds"], completion["chargedBytes"]
    stress = [{"multiplier":m,"illustrativeSeconds":m*linear_time,
               "illustrativeBytes":math.ceil(m*observed_size_proxy),
               "withinHistoricalReferenceTime":m*linear_time<=reference_seconds,
               "withinHistoricalReferenceBytes":m*observed_size_proxy<=reference_bytes}
              for m in (1.,1.25,2.)]
    for directory in (PACKAGE,RUN):
        require(sha(ROOT/directory/"files.json")==PINS[directory+"/files.json"], "Manifest changed after reading.")
    return {"observed": {"fights":old_fights,"operationalSeconds":seconds,"finalRetainedBytes":total,
                         "battleRecordBytes":battles,"otherRetainedBytes":other,"largestBattleRecordBytes":largest,
                         "phaseMeasurements":completion["phaseMeasurements"]},
            "historicalReferenceOnly": {"seconds":reference_seconds,"bytes":reference_bytes,
                                        "remainingSpendableAllowance":0},
            "assumptionsNotBounds": {"linearTimeSeconds":linear_time,"fixedOtherPlusObservedMaxBytes":observed_size_proxy,
                                     "relativeSlowdownToReferenceTime":reference_seconds/linear_time-1,
                                     "stress":stress},
            "newWallStorageAllowanceGranted":False,"feasibilityEstablished":False,
            "verifiedManifestEntries":{p:len(m) for p,m in manifests.items()},"pins":pins}


def main():
    checks = check_math()
    rows = sensitivities()
    resource = resources()
    main_bound = bound(N,TRUE_GAIN)
    population = 2**32-HISTORICAL_VALUES-PRECONFIRMATION_VALUES
    coupling = N*(N-1)/(2*population)
    without_replacement = max(0.,main_bound["conditionalDiagnosticPassLower"]-coupling)
    require(without_replacement >= POWER_TARGET, "Finite-population bound below target.")
    print(json.dumps({"version":"practical-selection-diagnostic-design-v1","launchDecision":"NoGo",
                      "planningDecision":"ConditionalPrecisionSpecified_ResourceAndSamplingAdmissionUnresolved",
                      "confirmationTrialsPerRecipe":N,"frozenNominees":4,"frozenPrimary":1,
                      "intervalFamily":FAMILY,"nominalFamilyCoverage":.95,"intervalCoverageIsApproximate":True,
                      "usefulObservedGain":OBSERVED_GAIN,"viabilityLowerRequirement":VIABILITY,
                      "powerTarget":POWER_TARGET,"sufficientTrueGainAlternative":TRUE_GAIN,
                      "criticalValue":P["critical_value"](FAMILY),"sufficientCutoffs":dict(zip(["contrast","viability"],cutoffs(N))),
                      "iidConditionalBound":main_bound,
                      "withoutReplacementIllustration":{"eligibleInt32ValuesAtCurrentHistory":population,
                          "couplingFailureUpper":coupling,"conditionalPassLower":without_replacement,
                          "requiresActuallyUniformSampling":True,"currentHashDerivationProvesUniformity":False},
                      "minimumNForThisSufficientBoundAt16Points":next(n for n in range(256,N+1) if bound(n,TRUE_GAIN)["conditionalDiagnosticPassLower"]>=POWER_TARGET),
                      "bounds":[bound(N,d) for d in (.05,.10,.15,.16,.20)],
                      "additional15PointSensitivity":bound(N,.15,.25),"sensitivity":rows,
                      "fights":{"discovery":512,"selection":128,"confirmation":4*N,"total":640+4*N},
                      "resources":resource,"checks":checks,"precisionSourceSha256":sha(PRECISION_SOURCE),
                      "analysisSourceSha256":sha(Path(__file__)),
                      "newFights":0,"newValues":0,"randomDraws":0,"preservedExclusions":HISTORICAL_VALUES},indent=2,allow_nan=False))


if __name__=="__main__":
    main()
