"""Seed-free cost, precision and decision arithmetic for the prospective selector plan.

No runtime invocation, random sampling, experiment allocation or battle execution.
This is a planning checker, not an experiment launcher or saved-battle verifier.
"""
import argparse
import hashlib
import itertools
import json
import math
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
PLAN = ROOT/'Balance Harness/Tower-Practical-Incumbent-Tie-Comparison-Plan.json'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_plan():
    p = json.loads(PLAN.read_text(encoding='utf-8'))
    r, b = p['restarts'], p['confirmationTrialsPerDifferingRestart']
    require(p['version'] == 'tower-incumbent-tie-comparison-v1' and r == 24 and b == 1000, 'Unexpected protocol')
    require(p['minimumNetGainedWins'] == p['minimumMeanGain'] * r * b, 'Inconsistent effect threshold')
    require(r * (p['candidatesPerRestart'] * p['discoveryTrials'] + p['nominees'] * p['selectionTrials'] + 2*b)
            == p['maximumFights'], 'Incorrect fight ceiling')
    require(p['maximumHistoricalValues'] + p['entropyWords'] == 1_000_000, 'Historical capacity exceeded')
    require(r * (1 + p['discoveryTrials'] + p['selectionTrials'] + b) <= p['entropyWords'], 'Insufficient entropy capacity')
    require(p['maximumSeconds'] - p['nativeMaximumSeconds'] == 60
            and p['maximumBytes'] - p['nativeMaximumBytes'] == 128 * 1024**2, 'Missing enclosing headroom')
    return p


def precision(p, differing, historical):
    r, b = p['restarts'], p['confirmationTrialsPerDifferingRestart']
    require(type(differing) is int and 0 <= differing <= r, 'Invalid differing-restart count')
    require(type(historical) is int and 0 <= historical <= p['maximumHistoricalValues'], 'Invalid history count')
    search_values = r * (1 + p['discoveryTrials'] + p['selectionTrials'])
    population = 2**32 - historical - search_values
    n, denominator = differing * b, r * b
    require(population > n, 'Confirmation population exhausted')
    drift = n * (n-1) / (population * denominator) if n else 0
    margin = math.sqrt(2*n*math.log(1/p['oneSidedAlpha'])) / denominator + drift if n else 0
    search_fights = r * (p['candidatesPerRestart'] * p['discoveryTrials'] + p['nominees'] * p['selectionTrials'])
    return dict(differingRestarts=differing, measuredPairedTrials=n, endpointDenominator=denominator,
                searchValues=search_values, assignedValues=search_values+denominator,
                confirmationPopulation=population, depletionAllowance=drift, lowerBoundMargin=margin,
                effectiveObservedThreshold=max(p['minimumMeanGain'], margin),
                totalFights=search_fights+2*n, searchFights=search_fights,
                replicationGatePossible=differing >= p['minimumPositiveRestarts'])


def assess(p, net_wins, differs, historical):
    require(len(net_wins) == len(differs) == p['restarts'], 'Every planned restart is required')
    b = p['confirmationTrialsPerDifferingRestart']
    require(all(type(d) is bool for d in differs) and all(type(w) is int and -b <= w <= b for w in net_wins), 'Invalid paired counts')
    require(all(d or w == 0 for d, w in zip(differs, net_wins)), 'Identical outputs must contribute exactly zero')
    k = sum(differs)
    m = precision(p, k, historical)
    net = sum(net_wins)
    mean = net / m['endpointDenominator']
    lower = max(-k/p['restarts'], mean-m['lowerBoundMargin'])
    positive = sum(w > 0 for w in net_wins)
    decision = ('NoSelectorDifferences' if k == 0 else 'SupportsIncumbentTieForFrozenOutputs'
                if net >= p['minimumNetGainedWins'] and lower > 0 and positive >= p['minimumPositiveRestarts']
                else 'DoNotPromoteIncumbentTie')
    return dict(decision=decision, netGainedWins=net, meanDifference=mean, lowerBound=lower, positiveRestarts=positive)


def aggregate_power_lower_bound(p, differing, historical, true_mean):
    """Aggregate gates only; does not guarantee the three-positive-restarts requirement."""
    m = precision(p, differing, historical)
    require(0 <= true_mean <= differing/p['restarts'], 'Impossible conditional mean')
    if not differing:
        return 0
    excess = true_mean - m['effectiveObservedThreshold'] - m['depletionAllowance']
    return max(0, 1-math.exp(-(m['endpointDenominator']*excess)**2/(2*m['measuredPairedTrials']))) if excess > 0 else 0


def report(p):
    h = p['illustrativeHistoricalCount']
    rows = []
    for k in (0, 1, 2, 3, 4, 6, 8, 12, 24):
        row = precision(p, k, h)
        row['aggregatePowerLowerBoundAtTwoPointMean'] = aggregate_power_lower_bound(p, k, h, .02) if k else 0
        rows.append(row)
    package = ROOT/p['capture']['package']
    for filename, expected in [('files.json', p['capture']['packageManifestSha256']), ('template.json', p['capture']['templateSha256'])]:
        require(hashlib.sha256((package/filename).read_bytes()).hexdigest() == expected, 'Changed captured source: '+filename)
    return dict(status='VerifiedProspectiveDesignArithmetic', version=p['version'], historicalCountIsIllustrative=True,
                illustrativeHistoricalCount=h, rows=rows, maximumFights=p['maximumFights'],
                newFights=0, newSeeds=0, nativePreparations=0,
                scope='Conditional average over 24 frozen outputs; not future-root reliability or default adoption.')


class DesignTests(unittest.TestCase):
    def setUp(self):
        self.p = read_plan()

    def test_zero_differences_are_exact_and_cost_only_search(self):
        r = precision(self.p, 0, 505562)
        self.assertEqual((r['searchValues'], r['assignedValues'], r['totalFights']), (984, 24984, 11904))
        self.assertEqual(r['lowerBoundMargin'], 0)
        self.assertEqual(assess(self.p, [0]*24, [False]*24, 505562)['decision'], 'NoSelectorDifferences')

    def test_full_budget_and_precision_remain_defined(self):
        r = precision(self.p, 24, self.p['maximumHistoricalValues'])
        self.assertEqual(r['totalFights'], 59904)
        self.assertTrue(.0158 < r['lowerBoundMargin'] < .0159)
        self.assertGreater(r['effectiveObservedThreshold'], .01)

    def test_exact_one_point_gate_and_replication_requirement(self):
        for wins, expected in [([80, 80, 80], 'SupportsIncumbentTieForFrozenOutputs'),
                               ([79, 80, 80], 'DoNotPromoteIncumbentTie'),
                               ([120, 120, 0], 'DoNotPromoteIncumbentTie')]:
            self.assertEqual(assess(self.p, wins+[0]*21, [True]*3+[False]*21, 505562)['decision'], expected)

    def test_every_restart_remains_in_denominator_and_bound_can_block_point_gate(self):
        r = assess(self.p, [80]*3+[0]*21, [True]*3+[False]*21, 505562)
        self.assertEqual(r['meanDifference'], .01)
        self.assertEqual(assess(self.p, [10]*24, [True]*24, 505562)['decision'], 'DoNotPromoteIncumbentTie')

    def test_invalid_or_missing_restart_cannot_be_dropped(self):
        for wins, differs in [([0]*23, [False]*23), ([1]+[0]*23, [False]*24), ([1001]+[0]*23, [True]*24)]:
            with self.assertRaises(ValueError):
                assess(self.p, wins, differs, 505562)
        with self.assertRaises(ValueError):
            precision(self.p, 25, 505562)

    def test_depletion_drift_bound_for_every_removed_subset_of_small_population(self):
        # Verifies the finite-population step, independently of experiment arithmetic.
        values = (-1, -1, 0, 0, 1, 1)
        mean = sum(values)/len(values)
        for removed_count in range(len(values)):
            for removed in itertools.combinations(range(len(values)), removed_count):
                remaining = [x for i, x in enumerate(values) if i not in removed]
                self.assertLessEqual(abs(sum(remaining)/len(remaining)-mean), 2*removed_count/len(values))

    def test_power_is_limited_to_aggregate_gate_and_improves_with_larger_effect(self):
        self.assertEqual(aggregate_power_lower_bound(self.p, 4, 505562, .01), 0)
        a = aggregate_power_lower_bound(self.p, 4, 505562, .02)
        self.assertTrue(.999 < a < 1)
        self.assertGreater(aggregate_power_lower_bound(self.p, 4, 505562, .025), a)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    options = parser.add_mutually_exclusive_group(required=True)
    options.add_argument('--self-test', action='store_true')
    options.add_argument('--output', type=Path)
    args = parser.parse_args()
    if args.self_test:
        if not unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(DesignTests)).wasSuccessful():
            raise SystemExit(1)
    else:
        result = report(read_plan())
        with args.output.open('x', encoding='utf-8') as stream:
            json.dump(result, stream, indent=2)
        print(json.dumps(result, indent=2))
