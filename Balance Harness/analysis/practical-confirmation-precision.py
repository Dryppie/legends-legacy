"""Deterministic planning arithmetic, not combat or a seed allocation.

Sum the paired-outcome multinomial distribution for ONE necessary contrast.
This is not joint power for both anchors, multiple restarts, or the viability gate.
Run from any directory; only the small checked-in outcome fixture is read.
"""

import hashlib
import json
import math
from pathlib import Path
from statistics import NormalDist


REPOSITORY = Path(__file__).resolve().parents[2]
FIXTURE = REPOSITORY / "LL/tests/EssenceSystem.Tests/Fixtures/tower-practical-pilot-outcomes.json"


def critical_value(family):
    # Same arithmetic as TowerBalanceEvaluator.CriticalValue; no fitted parameters.
    if family == 1:
        return 1.959963984540054
    q = math.sqrt(-2 * math.log(.025 / family))
    numerator = (((((-7.784894002430293e-3 * q - .3223964580411365) * q
                   - 2.400758277161838) * q - 2.549732539343734) * q
                 + 4.374664141464968) * q + 2.938163982698783)
    denominator = ((((7.784695709041462e-3 * q + .3224671290700398) * q
                     + 2.445134137142996) * q + 3.754408661907416) * q + 1)
    return -numerator / denominator


def wilson(wins, samples, family):
    z = critical_value(family)
    p = wins / samples
    denominator = 1 + z * z / samples
    center = (p + z * z / (2 * samples)) / denominator
    width = z * math.sqrt(p * (1 - p) / samples + z * z / (4 * samples * samples)) / denominator
    return max(0, center - width), min(1, center + width)


def binomial(samples, probability):
    """Stable centered recurrence; enumerate all masses without tail truncation."""
    assert samples >= 0 and 0 <= probability <= 1
    masses = [0.] * (samples + 1)
    if probability in (0, 1):
        masses[samples if probability else 0] = 1.
        return masses
    mode = min(samples, math.floor((samples + 1) * probability))
    masses[mode] = math.exp(math.lgamma(samples + 1) - math.lgamma(mode + 1)
                            - math.lgamma(samples - mode + 1) + mode * math.log(probability)
                            + (samples - mode) * math.log1p(-probability))
    for k in range(mode, 0, -1):
        masses[k - 1] = masses[k] * k / (samples - k + 1) * (1 - probability) / probability
    for k in range(mode, samples):
        masses[k + 1] = masses[k] * (samples - k) / (k + 1) * probability / (1 - probability)
    total = math.fsum(masses)
    assert abs(total - 1) < 1e-10, (samples, probability, total)
    return [value / total for value in masses]


def thresholds(samples, family):
    intervals = [wilson(k, samples, family) for k in range(samples + 1)]
    result = []
    # (gains - losses) / samples >= 0.05; integer arithmetic avoids rounding the boundary.
    minimum_difference = (samples + 19) // 20
    for discordant in range(samples + 1):
        low = (discordant + minimum_difference + 1) // 2
        high = discordant + 1
        if low > discordant:
            result.append(high)
            continue
        # At fixed discordance the paired lower bound increases monotonically in gains.
        while low < high:
            middle = (low + high) // 2
            if intervals[middle][0] > intervals[discordant - middle][1]:
                high = middle
            else:
                low = middle + 1
        result.append(low)
    return result


def contrast_probability(samples, family, discordance, true_gain, frozen_thresholds=None):
    assert 0 < discordance <= 1 and 0 <= true_gain <= discordance
    gain_given_discordance = (discordance + true_gain) / (2 * discordance)
    cuts = frozen_thresholds if frozen_thresholds is not None else thresholds(samples, family)
    terms = []
    for count, mass in enumerate(binomial(samples, discordance)):
        if mass == 0 or cuts[count] > count:
            continue
        terms.append(mass * math.fsum(binomial(count, gain_given_discordance)[cuts[count]:]))
    return math.fsum(terms)


def self_check():
    maximum_error = 0.
    for family in (1, 7, 26):
        assert abs(critical_value(family) - NormalDist().inv_cdf(1 - .025 / family)) < 1e-8
    for n in range(9):
        for p in (0, .1, .5, .9, 1):
            expected = [math.comb(n, k) * p ** k * (1 - p) ** (n - k) for k in range(n + 1)]
            assert max(abs(a - b) for a, b in zip(expected, binomial(n, p))) < 1e-13
    for n, family, q, delta in ((32, 1, .4, .1), (32, 7, .5, .2), (32, 1, .1, .1)):
        gain, loss = (q + delta) / 2, (q - delta) / 2
        direct = []
        cuts = thresholds(n, family)
        for g in range(n + 1):
            for l in range(n - g + 1):
                passed = 20 * (g - l) >= n and wilson(g, n, family)[0] > wilson(l, n, family)[1]
                assert passed == (g >= cuts[g + l])
                if passed:
                    direct.append(math.comb(n, g) * math.comb(n - g, l)
                                  * gain ** g * loss ** l * (1 - q) ** (n - g - l))
        error = abs(math.fsum(direct) - contrast_probability(n, family, q, delta))
        maximum_error = max(maximum_error, error)
        assert error < 1e-12
    return {"status": "Passed", "directMultinomialMaximumAbsoluteError": maximum_error}


def main():
    checks = self_check()
    raw = FIXTURE.read_bytes()
    fixture = json.loads(raw)
    selected = fixture["selected"]
    assert len(selected) == 256 and selected.count("1") == 167 and set(selected) <= {"0", "1"}
    observed = []
    for anchor in fixture["anchors"]:
        assert len(anchor) == 256 and anchor.count("1") == 161 and set(anchor) <= {"0", "1"}
        gains = sum(a == "1" and b == "0" for a, b in zip(selected, anchor))
        losses = sum(a == "0" and b == "1" for a, b in zip(selected, anchor))
        lower = wilson(gains, 256, 7)[0] - wilson(losses, 256, 7)[1]
        observed.append({"gains": gains, "losses": losses, "discordance": (gains + losses) / 256,
                         "observedGain": (gains - losses) / 256, "pairedLower": lower})
    assert [(x["gains"], x["losses"]) for x in observed] == [(66, 60), (54, 48)]
    assert abs(observed[0]["pairedLower"] - (-.120654)) < 1e-6
    assert abs(observed[1]["pairedLower"] - (-.110599)) < 1e-6
    profiles = [("low-discordance sensitivity", .1), ("pilot second contrast", observed[1]["discordance"]),
                ("pilot first contrast", observed[0]["discordance"]), ("high-discordance sensitivity", .6)]
    rows = []
    for samples, family in ((256, 7), (1024, 7), (1024, 26)):
        cuts = thresholds(samples, family)
        for label, discordance in profiles:
            for true_gain in (.05, .10):
                rows.append({"samples": samples, "intervalFamily": family, "discordanceProfile": label,
                             "discordance": discordance, "assumedTrueGain": true_gain,
                             "singleContrastPassProbability": contrast_probability(samples, family, discordance, true_gain, cuts)})
    print(json.dumps({
        "kind": "Deterministic prospective sensitivity calculation; not an experiment",
        "model": "IID paired win/non-win outcomes: gains=(q+delta)/2, losses=(q-delta)/2, ties=1-q",
        "gate": "observed gain >= 0.05 and WilsonLower(gains)-WilsonUpper(losses) > 0",
        "scope": "One necessary contrast only; excludes viability, joint anchors, selection uncertainty and multi-restart success",
        "futureFamilyAssumption": "26 quantities: 8 recipe rates plus gain/loss rates for 9 predeclared contrasts; not adopted",
        "fixture": str(FIXTURE.relative_to(REPOSITORY)).replace("\\", "/"),
        "fixtureSha256": hashlib.sha256(raw).hexdigest(),
        "retainedSourceSha256RecordedInFixture": fixture["sourceSha256"],
        "retainedSourceReauthenticated": False,
        "checks": checks, "pilotArithmetic": observed, "probabilities": rows,
        "randomDraws": 0, "evaluationSeedValuesReadOrAllocated": 0, "combatExecutions": 0
    }, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
