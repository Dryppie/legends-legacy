"""Seed-free arithmetic and source authentication for the exploration comparison plan.

No generation, native preparation, allocation, combat, or experiment launch.
"""
import argparse
import copy
import hashlib
import itertools
import json
import math
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
PLAN = ROOT / 'Balance Harness/Tower-Practical-Reference-Exploration-Comparison-Plan.json'


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate(p):
    require(p['version'] == 'tower-reference-exploration-comparison-v1'
            and p['status'] == 'ProspectiveDesignOnly', 'Unknown planning contract')
    r, b = p['pairedRestarts'], p['confirmationTrialsPerRestart']
    require((r, b, p['armsPerRestart'], p['candidatesPerArm'], p['maximumProposalAttemptsPerArm'],
             p['discoveryTrials'], p['nomineesPerArm'], p['selectionTrials']) == (12, 1000, 2, 46, 256, 8, 5, 32),
            'Changed paired search design')
    require(p['baselineGenerator'] == 'retained-composition-three-references-v1'
            and p['candidateGenerator'] == 'retained-composition-three-reference-exploration-v1'
            and p['selector'] == 'tower-staged-incumbent-tie-v1'
            and p['scope']['suppliedReferences'] == 3 and p['logicalPracticalIntervalFamily'] == 10,
            'Changed policy or control family')
    require(p['independentSearchPerArm'] and p['sharedConstructionRootWithinPair']
            and p['sharedStagePanelsWithinPair'] and not p['shareSearchBattleResults']
            and p['allOutputsFrozenBeforeConfirmation'], 'Changed pairing or freeze boundary')
    search = r * 2 * (46 * 8 + 5 * 32)
    require(p['searchFights'] == search and p['minimumFights'] == search + r*b*3
            and p['maximumFights'] == search + r*b*5, 'Incorrect fight ceilings')
    require(p['minimumConfirmationRecipesPerRestart'] == 3 and p['maximumConfirmationRecipesPerRestart'] == 5,
            'Missing confirmation controls')
    require(p['searchValues'] == r*41 and p['assignedValues'] == r*(41+b)
            and p['endpointDenominator'] == r*b, 'Incorrect value mapping or endpoint denominator')
    require(p['entropyWordBits'] == 32 and p['entropyWords'] == 16384
            and p['assignedValues'] <= p['entropyWords']
            and p['maximumHistoricalValues'] + p['entropyWords'] == 1_000_000, 'Invalid entropy capacity')
    require(p['minimumMeanGain'] == .05 and p['minimumNetGainedWins'] == r*b//20
            and p['minimumPositiveRestarts'] == 7 and p['oneSidedAlpha'] == .05
            and p['boundVersion'] == 'conditional-range-hoeffding-depletion-v1', 'Changed scientific gates')
    require(p['maximumSeconds'] == p['admissionChargeSeconds'] + p['executionMaximumSeconds'] == 10800
            and p['maximumBytes'] == p['admissionChargeBytes'] + p['executionMaximumBytes'] == 6*1024**3
            and p['executionMaximumSeconds'] - p['nativeMaximumSeconds'] == 120
            and p['executionMaximumBytes'] - p['nativeMaximumBytes'] == 256*1024**2,
            'Missing cumulative charge or enclosing headroom')
    require(p['resourceStatus'] == 'ProposedNewEnvelopeNotLaunchAuthorization'
            and p['maximumScientificLaunches'] == 1 and p['retries'] == 0
            and not any(p[k] for k in ['resume', 'sampleExtension', 'replacementRestarts', 'automaticDefaultChange', 'automaticTeamAdoption']),
            'Closed scope or admission boundary changed')
    return p


def precision(p, differing, historical):
    require(type(differing) is int and 0 <= differing <= p['pairedRestarts'], 'Invalid differing-root count')
    require(type(historical) is int and 0 <= historical <= p['maximumHistoricalValues'], 'Invalid historical count')
    n = differing * p['confirmationTrialsPerRestart']
    denominator = p['endpointDenominator']
    population = 2**32 - historical - p['searchValues']
    require(population > p['pairedRestarts'] * p['confirmationTrialsPerRestart'], 'Exhausted fresh population')
    drift = n * (n-1) / (population * denominator) if n else 0
    margin = math.sqrt(2*n*math.log(1/p['oneSidedAlpha'])) / denominator + drift if n else 0
    return dict(differingRestarts=differing, measuredPairedTrials=n, endpointDenominator=denominator,
                confirmationPopulation=population, depletionAllowance=drift, lowerBoundMargin=margin,
                requiredObservedMean=max(p['minimumMeanGain'], margin),
                replicationGatePossible=differing >= p['minimumPositiveRestarts'])


def family(reference_ids, baseline, candidate):
    """Inputs stand for already validated full physical identities, not display labels."""
    require(len(reference_ids) == len(set(reference_ids)) == 3 and all(reference_ids), 'Three distinct controls required')
    require(bool(baseline) and bool(candidate), 'Both selected identities are required')
    return sorted(set(reference_ids) | {baseline, candidate})


def fight_cost(p, family_sizes):
    require(len(family_sizes) == p['pairedRestarts']
            and all(type(n) is int and 3 <= n <= 5 for n in family_sizes), 'Missing or invalid confirmation families')
    return p['searchFights'] + sum(family_sizes)*p['confirmationTrialsPerRestart']


def assess(p, rows, historical):
    require(len(rows) == p['pairedRestarts'], 'Every planned root is required')
    net, positive, differs, sizes = 0, 0, 0, []
    for i, row in enumerate(rows):
        require(row['restart'] == i+1 and type(row['different']) is bool, 'Missing, reordered or relabeled restart')
        g, l = row['gains'], row['losses']
        require(type(g) is int and type(l) is int and min(g, l) >= 0
                and g+l <= p['confirmationTrialsPerRestart'], 'Invalid paired discordances')
        require(row['different'] or g == l == 0, 'Identical outputs must contribute exact zero')
        require(row['different'] or row['familySize'] <= 4, 'Identical outputs cannot require five distinct recipes')
        net += g-l; positive += g > l; differs += row['different']; sizes.append(row['familySize'])
    fights = fight_cost(p, sizes)
    m = precision(p, differs, historical)
    mean = net / p['endpointDenominator']
    lower = max(-differs/p['pairedRestarts'], mean-m['lowerBoundMargin'])
    decision = ('NoSelectedOutputDifferences' if differs == 0 else 'SupportsReferenceExplorationForFrozenOutputs'
                if net >= p['minimumNetGainedWins'] and lower > 0 and positive >= p['minimumPositiveRestarts']
                else 'DoNotPromoteReferenceExploration')
    return dict(decision=decision, netGainedWins=net, meanDifference=mean, lowerBound=lower,
                positiveRestarts=positive, differingRestarts=differs, totalFights=fights)


def aggregate_power_lower_bound(p, differing, historical, true_mean):
    require(0 <= true_mean <= differing/p['pairedRestarts'], 'Impossible conditional mean')
    m = precision(p, differing, historical)
    if not differing:
        return 0
    excess = true_mean - m['requiredObservedMean'] - m['depletionAllowance']
    return 1-math.exp(-(p['endpointDenominator']*excess)**2/(2*m['measuredPairedTrials'])) if excess > 0 else 0


def authenticate(p):
    """Authenticate consumed source files; this does not perform native admission or a live history scan."""
    pins = {}
    def pinned(path, expected):
        require(sha(path) == expected, 'Changed source: '+str(path))
        pins[str(path.relative_to(ROOT)).replace('\\', '/')] = expected
        return read(path)
    capture = p['capture']; package = ROOT / capture['package']; closeout = ROOT / capture['reconciledCloseout']
    manifest = pinned(package/'files.json', capture['packageManifestSha256'])
    reconciled = pinned(closeout/'files.json', capture['closeoutManifestSha256'])
    receipt = pinned(closeout/'receipt.json', reconciled['receipt.json'])
    pinned(package/'failure.json', capture['failureSha256'])
    require(receipt['manifestSha256'] == capture['packageManifestSha256']
            and receipt['reconciledCloseoutFailureSha256'] == capture['failureSha256']
            and receipt['closeoutStatus'] == 'ReadOnlyReconciled', 'Unreconciled source admission')
    require(manifest[capture['template']] == capture['templateSha256'], 'Unbound seed-free template')
    template = pinned(package/capture['template'], capture['templateSha256'])
    require(template['settingsHash'] == capture['settingsHash']
            and template['generation']['policyVersion'] == p['baselineGenerator']
            and template['stages']['selectionPolicyVersion'] == p['selector']
            and template['stages']['selectionPrimaryReferenceId'] == p['selectionPrimaryReferenceId']
            and [s['party']['id'] for s in template['starts']] == p['referencePartyIds']
            and template['ownedCopies'] is None, 'Changed captured recipes or selector')
    require(template['generation']['seeds'] == [] and all(s[k] == [] for s in template['stages']['schedules'].values()
            for k in ['discovery', 'selection', 'confirmation', 'diagnostics']), 'Template contains execution values')
    v = p['generatorVerification']; verification = pinned(ROOT/v['receipt'], v['sha256'])
    require(verification['testCounters']['passed'] == str(v['passedTests'])
            and verification['generationPolicy'] == p['candidateGenerator'], 'Wrong generator verification')
    for name, digest in verification['sourceFiles'].items():
        require(sha(ROOT/name) == digest, 'Implementation changed since verification: '+name)
        pins[name] = digest
    verification_root = (ROOT/v['receipt']).parent
    for name in ['regression.trx', 'history-preservation.json']:
        path = verification_root/name
        require(sha(path) == verification['evidence'][name], 'Changed engineering evidence')
        pins[str(path.relative_to(ROOT)).replace('\\', '/')] = sha(path)
    return pins


def report(p):
    pins = authenticate(p); h = p['illustrativeHistoricalCount']
    table = []
    for k in [0, 1, 3, 6, 7, 9, 12]:
        row = precision(p, k, h)
        row['aggregatePowerLowerBoundAtSevenPointMean'] = aggregate_power_lower_bound(p, k, h, .07) if k else 0
        table.append(row)
    return dict(status='VerifiedProspectiveDesignArithmetic', version=p['version'], planSha256=sha(PLAN),
                consumedSourceFiles=pins, historicalCountIsIllustrative=True, illustrativeHistoricalCount=h,
                precision=table, searchFights=p['searchFights'], minimumFights=p['minimumFights'], maximumFights=p['maximumFights'],
                assignedValues=p['assignedValues'], entropyWords=p['entropyWords'], newFights=0, newValues=0,
                nativePreparations=0, admissionReady=False, comparisonRunnerImplemented=False,
                interpretation='Conditional on the twelve frozen output pairs; not future-root reliability or team adoption.')


class DesignTests(unittest.TestCase):
    def setUp(self):
        self.p = validate(read(PLAN))

    @staticmethod
    def rows(net, different=True, size=5):
        return [dict(restart=i+1, gains=max(w, 0), losses=max(-w, 0), different=different, familySize=size)
                for i, w in enumerate(net)]

    def test_exact_cost_and_value_envelopes(self):
        self.assertEqual(fight_cost(self.p, [3]*12), 48672)
        self.assertEqual(fight_cost(self.p, [5]*12), 72672)
        self.assertEqual(fight_cost(self.p, [3, 4, 5]*4), 60672)
        self.assertEqual(self.p['assignedValues'], 12492)

    def test_union_preserves_controls_and_converged_output(self):
        controls = ['a', 'b', 'c']
        self.assertEqual(family(controls, 'a', 'b'), controls)
        self.assertEqual(family(controls, 'x', 'x'), controls+['x'])
        self.assertEqual(family(controls, 'x', 'y'), controls+['x', 'y'])
        with self.assertRaises(ValueError): family(['a', 'a', 'c'], 'x', 'y')

    def test_integer_effect_boundary_and_replication(self):
        self.assertEqual(assess(self.p, self.rows([50]*12), 551408)['decision'], 'SupportsReferenceExplorationForFrozenOutputs')
        self.assertEqual(assess(self.p, self.rows([50]*11+[49]), 551408)['decision'], 'DoNotPromoteReferenceExploration')
        self.assertEqual(assess(self.p, self.rows([100]*6+[0]*6), 551408)['decision'], 'DoNotPromoteReferenceExploration')
        self.assertEqual(assess(self.p, self.rows([86]*6+[84]+[0]*5), 551408)['positiveRestarts'], 7)

    def test_convergence_has_zero_difference_but_measures_controls(self):
        result = assess(self.p, self.rows([0]*12, False, 3), 551408)
        self.assertEqual(result['decision'], 'NoSelectedOutputDifferences')
        self.assertEqual((result['meanDifference'], result['lowerBound'], result['totalFights']), (0, 0, 48672))

    def test_primary_denominator_includes_identical_roots(self):
        rows = self.rows([100]*6+[0]*6, size=4)
        for row in rows[6:]: row['different'] = False
        self.assertEqual(assess(self.p, rows, 551408)['meanDifference'], .05)

    def test_incomplete_reordered_or_impossible_evidence_rejected(self):
        changes = [lambda r: r.pop(), lambda r: r.reverse(), lambda r: r[0].update(gains=1000, losses=1),
                   lambda r: r[0].update(different=False), lambda r: r[0].update(familySize=2),
                   lambda r: r[0].update(gains=True)]
        for change in changes:
            rows = self.rows([50]*12); change(rows)
            with self.assertRaises(ValueError): assess(self.p, rows, 551408)

    def test_precision_worst_case_and_invalid_population(self):
        m = precision(self.p, 12, 551408)
        self.assertTrue(.0223 < m['lowerBoundMargin'] < .0224)
        self.assertLess(m['depletionAllowance'], .000003)
        self.assertEqual(precision(self.p, 0, 0)['lowerBoundMargin'], 0)
        for k, h in [(-1, 0), (13, 0), (1, -1), (1, 983617), (True, 0)]:
            with self.assertRaises(ValueError): precision(self.p, k, h)

    def test_aggregate_power_is_not_a_replication_or_completion_claim(self):
        value = aggregate_power_lower_bound(self.p, 12, 551408, .07)
        self.assertTrue(.90 < value < .91)
        self.assertEqual(aggregate_power_lower_bound(self.p, 12, 551408, .05), 0)
        self.assertFalse(precision(self.p, 6, 551408)['replicationGatePossible'])

    def test_depletion_bound_exhaustive_small_population(self):
        for m in range(2, 7):
            for values in itertools.product([-1, 1], repeat=m):
                mean = sum(values)/m
                for removed in range(m):
                    for indices in itertools.combinations(range(m), removed):
                        remaining = [v for i, v in enumerate(values) if i not in indices]
                        self.assertLessEqual(abs(sum(remaining)/len(remaining)-mean), 2*removed/m+1e-12)

    def test_plan_cannot_hide_control_costs_or_expand_closed_scope(self):
        for key, value in [('maximumFights', 36672), ('nomineesPerArm', 4), ('searchValues', 984),
                           ('minimumNetGainedWins', 599), ('nativeMaximumSeconds', 10200), ('resume', True)]:
            p = copy.deepcopy(self.p); p[key] = value
            with self.assertRaises(ValueError): validate(p)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--self-test', action='store_true')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    if args.self_test:
        result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(DesignTests))
        if not result.wasSuccessful(): raise SystemExit(1)
    if args.output:
        result = report(validate(read(PLAN)))
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open('x', encoding='utf-8') as stream: json.dump(result, stream, indent=2)
        print(json.dumps({k: v for k, v in result.items() if k not in ['precision', 'consumedSourceFiles']}))
    if not args.self_test and not args.output:
        parser.error('Specify --self-test or --output; there is no launch operation.')
