"""Verify the new offset comparison declaration without allocation or native execution."""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
PLAN = ROOT/'Balance Harness/Tower-Practical-Reference-Exploration-Offset-Comparison-Plan.json'
PLAN_PIN = '7e6203052cd8a1984b80bbe34dbb1e4b21fe3f882c8ab314e21987d289a25c90'
OLD_PLAN = ROOT/'Balance Harness/Tower-Practical-Reference-Exploration-Comparison-Plan.json'
OLD_PIN = '6a0e8fb68d7a20e66740795d17278e5bf7e95c8e77c6b10f9127e8c3e77468a4'
HELPER = ROOT/'Balance Harness/analysis/practical-reference-exploration-design.py'
HELPER_PIN = '56c602ed93ebf3b7750c45875b9a5d2861a8ad9a230f9bbdb579c7bd4bcf2128'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def require(ok, message):
    if not ok:
        raise ValueError(message)


require(sha(HELPER) == HELPER_PIN and sha(OLD_PLAN) == OLD_PIN, 'Changed inherited arithmetic or closed protocol')
spec = importlib.util.spec_from_file_location('original_exploration_design', HELPER)
old = importlib.util.module_from_spec(spec)
spec.loader.exec_module(old)


def validate(p):
    require(p['version'] == 'tower-reference-exploration-offset-comparison-v1'
            and p['candidateGenerator'] == 'retained-composition-three-reference-exploration-offset-v1'
            and p['illustrativeHistoricalCount'] == 567789, 'Changed offset declaration')
    require(p['supportDecision'] == 'SupportsReferenceExplorationOffsetForFrozenOutputs'
            and p['negativeDecision'] == 'DoNotPromoteReferenceExplorationOffset', 'Changed decision labels')
    previous = read(OLD_PLAN)
    normalized = copy.deepcopy(p)
    for key in ['predecessor', 'candidateChange', 'supportDecision', 'negativeDecision', 'abandonment']:
        require(bool(normalized.pop(key)), 'Missing offset boundary: '+key)
    for key in ['version', 'candidateGenerator', 'illustrativeHistoricalCount', 'generatorVerification']:
        normalized[key] = previous[key]
    require(normalized == previous, 'Scientific scope differs beyond the declared candidate and source bindings')
    old.validate(normalized)
    require(p['predecessor']['planSha256'] == OLD_PIN
            and p['predecessor']['manifestSha256'] == '482707e32c6e4e398793e45e61627b452a765db237c382a9c75a34c8f6163a7e', 'Changed closed predecessor')
    return p


def report(p):
    require(sha(PLAN) == PLAN_PIN, 'Unfrozen offset protocol')
    consumed = {str(HELPER.relative_to(ROOT)): HELPER_PIN, str(OLD_PLAN.relative_to(ROOT)): OLD_PIN}
    def pin(path, expected):
        require(sha(path) == expected, 'Changed retained source: '+str(path))
        consumed[path.relative_to(ROOT).as_posix()] = expected
        return read(path)
    generator = p['generatorVerification']; package = (ROOT/generator['receipt']).parent
    manifest = pin(package/'files.json', generator['manifestSha256'])
    require({x.relative_to(package).as_posix() for x in package.rglob('*') if x.is_file()} == set(manifest) | {'files.json'}, 'Changed generator package membership')
    for name, expected in manifest.items():
        target = package/name
        require(target.resolve().is_relative_to(package.resolve()) and not target.is_symlink() and not target.is_junction()
                and sha(target) == expected, 'Changed generator evidence: '+name)
    verification = pin(ROOT/generator['receipt'], generator['sha256'])
    require(verification['backendTestsPassed'] == generator['passedTests'] == 325
            and verification['policyVersion'] == p['candidateGenerator'], 'Wrong mechanical verification')
    # The comparison controller/test is being versioned separately. The generator itself stays fixed.
    for name in ['TowerReferenceExploration.cs', 'TowerSuppliedCompositionSearch.cs', 'TowerBossDiscoveryContract.cs', 'TowerCompositionSearch.cs']:
        relative = 'LL/tools/BalanceHarness/'+name
        require(sha(ROOT/relative) == verification['sourceHashes'][relative], 'Candidate implementation changed')
        consumed[relative] = verification['sourceHashes'][relative]
    capture = p['capture']; source = ROOT/capture['package']; closeout = ROOT/capture['reconciledCloseout']
    files = pin(source/'files.json', capture['packageManifestSha256'])
    closure = pin(closeout/'files.json', capture['closeoutManifestSha256'])
    receipt = pin(closeout/'receipt.json', closure['receipt.json'])
    pin(source/'failure.json', capture['failureSha256'])
    require(receipt['closeoutStatus'] == 'ReadOnlyReconciled' and receipt['reconciledCloseoutFailureSha256'] == capture['failureSha256'], 'Unreconciled capture')
    require(files[capture['template']] == capture['templateSha256'], 'Unbound template')
    template = pin(source/capture['template'], capture['templateSha256'])
    require([s['party']['id'] for s in template['starts']] == p['referencePartyIds'] and template['settingsHash'] == capture['settingsHash']
            and template['generation']['seeds'] == [] and template['ownedCopies'] is None, 'Changed captured scope')
    prior = p['predecessor']; pin(ROOT/prior['scientificArchive']/'files.json', prior['manifestSha256'])
    history = pin(package/'history-preservation.json', manifest['history-preservation.json'])
    require((history['historicalValues'], history['historyFiles']) == (567789, 234), 'Changed history receipt')
    return dict(status='OffsetProspectiveDesignVerified', version=p['version'], planSha256=PLAN_PIN,
        sourcePins=consumed, historicalCountIsIllustrative=True, illustrativeHistoricalCount=567789,
        precision=[old.precision(p, k, 567789) for k in [0, 1, 3, 6, 7, 9, 12]],
        searchFights=12672, minimumFights=48672, maximumFights=72672, assignedValues=12492, entropyWords=16384,
        newFights=0, newValues=0, nativePreparations=0, admissionReady=False,
        interpretation='Candidate-only algorithm change; frozen-output strength endpoint and all resource gates retained.')


class OffsetDesignTests(unittest.TestCase):
    def test_declared_protocol(self):
        validate(read(PLAN))

    def test_old_candidate_or_version_cannot_be_substituted(self):
        for key in ['version', 'candidateGenerator']:
            value = read(PLAN); value[key] = read(OLD_PLAN)[key]
            with self.assertRaises(ValueError):
                validate(value)

    def test_no_weaker_gate_or_extra_budget(self):
        for key, changed in [('minimumNetGainedWins', 599), ('minimumPositiveRestarts', 6), ('maximumSeconds', 10801),
                             ('nativeMaximumSeconds', 10200), ('resume', True), ('searchValues', 491)]:
            value = read(PLAN); value[key] = changed
            with self.assertRaises(ValueError):
                validate(value)

    def test_source_and_generator_authentication(self):
        self.assertEqual('OffsetProspectiveDesignVerified', report(validate(read(PLAN)))['status'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    suite = unittest.TestSuite([unittest.defaultTestLoader.loadTestsFromTestCase(old.DesignTests),
                              unittest.defaultTestLoader.loadTestsFromTestCase(OffsetDesignTests)])
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    if not result.wasSuccessful():
        raise SystemExit(1)
    if args.output:
        with args.output.open('x', encoding='utf-8') as stream:
            json.dump(report(validate(read(PLAN))), stream, indent=2)
