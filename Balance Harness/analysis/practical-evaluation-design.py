"""Deterministic conditional-power bounds for a proposed evaluation; no gameplay.

Only analysis source is read. No outcomes, historical archives, seeds or random
draws are used. Run with Python 3; stdout is the reproducible JSON artifact.
"""

import hashlib
import json
import math
from pathlib import Path
import runpy


SOURCE = Path(__file__).with_name("practical-confirmation-precision.py")
PRECISION = runpy.run_path(str(SOURCE))
critical_value = PRECISION["critical_value"]
wilson = PRECISION["wilson"]
N = 1024
FAMILY = 26
OBSERVED_GAIN = .05
VIABILITY = .10
RESTARTS = 3
REQUIRED = 2
CONTRASTS = 3


def cutoffs():
    z = critical_value(FAMILY)
    # Each Wilson half-width <= z / (2 * sqrt(n + z*z)).
    positive_lower = z * math.sqrt(N + z * z) / N
    # Strictly exceeding this value is sufficient, including the observed gate.
    contrast = max(OBSERVED_GAIN, positive_lower)
    # Score-test inversion: WilsonLower(p_hat) >= v iff p_hat >= this value.
    viability = VIABILITY + z * math.sqrt(VIABILITY * (1 - VIABILITY) / N)
    return z, contrast, viability


def lower_bound(delta, strong_restarts):
    """Conditional on eligible fixed outputs with all required gains >= delta.

    Independence is required across confirmation trials, not across recipes,
    contrasts or restarts sharing a trial. A true gain >= delta implies the
    candidate's true win probability >= delta, since reference rates are >= 0.
    """
    assert 0 <= delta <= 1 and REQUIRED <= strong_restarts <= RESTARTS
    _, contrast, viability = cutoffs()
    contrast_failure = math.exp(-N * (delta - contrast) ** 2 / 2) if delta > contrast else 1.
    viability_failure = math.exp(-2 * N * (delta - viability) ** 2) if delta > viability else 1.
    restart_failure = min(1., CONTRASTS * contrast_failure + viability_failure)
    # If fewer than REQUIRED pass, at least k - REQUIRED + 1 strong ones fail.
    joint_failure = min(1., strong_restarts * restart_failure / (strong_restarts - REQUIRED + 1))
    return {"trueGainAtLeast": delta, "eligibleStrongRestartsAtLeast": strong_restarts,
            "oneContrastFailureUpper": contrast_failure,
            "oneViabilityFailureUpper": viability_failure,
            "oneStrongRestartFailureUpper": restart_failure,
            "jointConfirmationPassLower": max(0., 1 - joint_failure)}


def self_check():
    base_checks = PRECISION["self_check"]()
    z, contrast, viability = cutoffs()
    intervals = [wilson(k, N, FAMILY) for k in range(N + 1)]
    maximum_width = z / (2 * math.sqrt(N + z * z))
    for k, (low, high) in enumerate(intervals):
        assert (high - low) / 2 <= maximum_width + 1e-14
        assert (low >= VIABILITY) == (k / N >= viability)
    minimum_wins = next(k for k, interval in enumerate(intervals) if interval[0] >= VIABILITY)
    assert minimum_wins == math.ceil(N * viability) == 133
    checked_pairs = 0
    sufficient_pairs = 0
    for gains in range(N + 1):
        for losses in range(N - gains + 1):
            checked_pairs += 1
            if (gains - losses) / N > contrast:
                sufficient_pairs += 1
                assert 20 * (gains - losses) >= N
                assert intervals[gains][0] > intervals[losses][1]
    # Check the event-count inequality for every possible pattern, without
    # assuming any distribution or independence between restart outcomes.
    patterns = 0
    for k in (2, 3):
        for mask in range(1 << k):
            successes = sum((mask >> j) & 1 for j in range(k))
            failures = k - successes
            assert int(successes < REQUIRED) <= failures / (k - REQUIRED + 1)
            patterns += 1
    # Independent numerical multinomial summation checks the contrast bound
    # at diverse discordances. These checks are sensitivity arithmetic, not
    # an exhaustive search over distributions and not the proof of the bound.
    cuts = PRECISION["thresholds"](N, FAMILY)
    sensitivity = []
    for delta in (.18, .20):
        bound = lower_bound(delta, 2)
        exact_viability = math.fsum(PRECISION["binomial"](N, delta)[minimum_wins:])
        assert exact_viability + 1e-12 >= 1 - bound["oneViabilityFailureUpper"]
        for discordance in (delta, .4, .6, 1.):
            probability = PRECISION["contrast_probability"](N, FAMILY, discordance, delta, cuts)
            assert probability + 1e-12 >= 1 - bound["oneContrastFailureUpper"]
            sensitivity.append({"trueGain": delta, "discordance": discordance,
                                "singleContrastPassProbability": probability})
    assert lower_bound(.18, 2)["jointConfirmationPassLower"] >= .80
    return {"status": "Passed", "priorArithmeticChecks": base_checks,
            "feasibleGainLossPairsChecked": checked_pairs,
            "sufficientGainLossPairsChecked": sufficient_pairs,
            "viabilityCountsChecked": N + 1, "restartPatternsChecked": patterns,
            "minimumViabilityWins": minimum_wins,
            "singleContrastSensitivity": sensitivity}


def main():
    checks = self_check()
    discovery = RESTARTS * 2 * 512
    selection = RESTARTS * 2 * 128
    confirmation = (RESTARTS * 2 + 2) * N
    assert discovery + selection + confirmation == 12032
    z, contrast, viability = cutoffs()
    print(json.dumps({
        "kind": "Prospective conditional confirmation-power bound; not an experiment",
        "launchDecision": "NoGo",
        "scope": "Statistical gate only, conditional on eligible fixed strong outputs; assumes a complete prescribed panel without outcome-dependent truncation; excludes search success and operational failure",
        "model": "Independent confirmation trials conditional on frozen recipes; arbitrary dependence between recipes, contrasts and restarts within a trial",
        "method": "Wilson maximum-width sufficient event; Hoeffding tails; union and failed-restart count bounds",
        "nominalIntervalFamily": FAMILY,
        "nominalFamilyCoverage": .95,
        "intervalCoverageIsApproximate": True,
        "confirmationTrialsPerRecipe": N,
        "observedUsefulGainThreshold": OBSERVED_GAIN,
        "supportedViabilityThreshold": VIABILITY,
        "conditionalPowerTarget": .80,
        "sufficientPlanningAlternative": .18,
        "criticalValue": z,
        "strictSufficientContrastCutoff": contrast,
        "viabilityObservedCutoff": viability,
        "maximumFights": {"discovery": discovery, "selection": selection,
                          "confirmation": confirmation, "total": discovery + selection + confirmation},
        "bounds": [lower_bound(delta, k) for delta in (.05, .10, .15, .18, .20) for k in (2, 3)],
        "checks": checks,
        "arithmeticSourceSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        "historicalEvidenceReadOrAuthenticated": False,
        "randomDraws": 0, "evaluationSeedValuesReadOrAllocated": 0, "combatExecutions": 0
    }, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
