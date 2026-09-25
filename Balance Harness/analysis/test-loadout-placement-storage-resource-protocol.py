"""Read-only gate regressions; all adversarial files use fresh temporary directories."""
import copy
import importlib.util
import json
import math
from pathlib import Path
import random
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('storage_resources', Path(__file__).with_name('loadout-placement-storage-resource-protocol.py'))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


class StorageResourceProtocol(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if m.sha(m.ROOT/m.PRIOR) != m.PRIOR_PIN or m.sha(m.ROOT/m.PLAN) != m.PLAN_PIN:
            raise ValueError('Changed prospective bindings')
        cls.plan, cls.prior = m.read(m.ROOT/m.PLAN), m.read(m.ROOT/m.PRIOR)
        root = m.ROOT/cls.prior['recoveryRegistration']
        m.manifest(root, cls.prior['recoveryRegistrationManifestSha256'])
        code = m.module(root/m.RECOVERY_HELPER)
        cls.recovery = code.verify(root, cls.prior['recoveryRegistrationManifestSha256'])
        cls.probe = code.inputs_at(root)['audit-probe']
        proof_manifest = m.ROOT/Path(m.PROOF).parent/'files.json'
        if m.sha(proof_manifest) != cls.prior['verificationManifestSha256']:
            raise ValueError('Changed integration manifest')
        if m.sha(m.ROOT/m.PROOF) != m.read(proof_manifest)['logical-equivalence.json']:
            raise ValueError('Changed logical proof')
        cls.proof = m.read(m.ROOT/m.PROOF)

    def result(self, plan=None, prior=None, recovery=None, proof=None):
        return m.assess(plan or self.plan, prior or self.prior, recovery or self.recovery,
                        proof or self.proof, self.probe)

    def test_authenticated_recovery_floor_closes_new_gate(self):
        result = self.result()
        self.assertEqual(result['roundedNecessaryFloors'], dict(nativeSeconds=8020, auditSeconds=1806,
                         nativeBytes=5347633246, auditBytes=34989702))
        self.assertEqual(result['exceededPartitions'], ['auditSeconds'])
        self.assertFalse(result['measurementPairMayBeRegistered'])
        self.assertEqual(result['status'], 'StorageResourceNecessaryGateFailed')

    def test_probe_components_reconcile_without_discarding_worker(self):
        terms = self.result()['probeReconciliation']
        self.assertAlmostEqual(terms['retainedWorkerAndIndependentAuditsWithMarginSeconds'],
                               2*(self.probe['wholeWorkerSeconds']+sum(self.probe['independentAuditSeconds'])))
        self.assertGreater(terms['retainedScaledInventoriesWithMarginSeconds'], 0)
        self.assertEqual(terms['publicationReserveSeconds'], 120)
        self.assertAlmostEqual(terms['totalSeconds'], 1805.0805223456266)
        self.assertAlmostEqual(sum(terms[k] for k in ('retainedWorkerAndIndependentAuditsWithMarginSeconds',
                              'retainedScaledInventoriesWithMarginSeconds', 'publicationReserveSeconds')), terms['totalSeconds'])

    def test_both_audits_retain_repeated_decoded_work(self):
        work = self.result()['correctnessWork']
        self.assertEqual(work['logicalEncodedBytes'], 1030288532)
        self.assertEqual(work['nativeAuditDecodedBytes'], 2060577064)
        self.assertEqual(work['minimumTwoAuditDecodedBytesUnderCurrentReaders'], 4121154128)
        self.assertEqual(work['nativeAuditDecodePasses'], 120)

    def test_no_forecast_or_execution_is_inferred(self):
        result = self.result()
        for name in ('qualifiedCurrentForecast', 'measuredStorageSavings'):
            self.assertIsNone(result[name])
        for name in ('currentRuntimeQualified', 'measurementPairMayBeRegistered', 'nativePreparationAllowed',
                     'qualificationAllowed', 'scientificAdmissionAllowed', 'floorDiscountApplied'):
            self.assertIs(result[name], False)
        for name in ('productionEntropyDraws', 'newScientificReservations', 'actualCombat', 'newTimingSamples'):
            self.assertEqual(result[name], 0)

    def test_faster_or_smaller_future_costs_cannot_lower_any_floor(self):
        floors = self.recovery['unroundedNecessaryFloors']
        self.assertEqual(m.necessary_gate(floors), m.necessary_gate(floors, {k:1 for k in floors}))

    def test_larger_costs_raise_each_partition_independently(self):
        floors = self.recovery['unroundedNecessaryFloors']
        for key in floors:
            with self.subTest(key=key):
                proposed = dict(floors)
                proposed[key] = math.ceil(max(m.LIMITS[key], floors[key]))+1
                result = m.necessary_gate(floors, proposed)
                self.assertIn(key, result['exceededPartitions'])
                self.assertEqual(result['roundedNecessaryFloors'][key], proposed[key])

    def test_rounded_floor_equal_to_cap_fails(self):
        for key in m.LIMITS:
            for value in (m.LIMITS[key], math.nextafter(float(m.LIMITS[key]), -math.inf)):
                with self.subTest(key=key, value=value):
                    floors = {k:1 for k in m.LIMITS}
                    floors[key] = value
                    self.assertIn(key, m.necessary_gate(floors)['exceededPartitions'])

    def test_floor_arithmetic_is_monotone_for_100_deterministic_cases(self):
        rng = random.Random(9024)
        for _ in range(100):
            retained = {k:rng.uniform(1, cap*2) for k, cap in m.LIMITS.items()}
            larger = {k:v+rng.uniform(0, v) for k,v in retained.items()}
            result = m.necessary_gate(retained, larger)
            self.assertTrue(all(result['roundedNecessaryFloors'][k] >= math.ceil(v) for k,v in larger.items()))

    def test_even_a_passing_necessary_bound_grants_no_measurement(self):
        recovery = copy.deepcopy(self.recovery)
        recovery['unroundedNecessaryFloors'] = {k:1 for k in m.LIMITS}
        prior = copy.deepcopy(self.prior)
        prior['resourceAssessment'] = recovery
        result = self.result(prior=prior, recovery=recovery)
        self.assertTrue(result['necessaryGatePassed'])
        self.assertFalse(result['measurementPairMayBeRegistered'])

    def test_missing_or_invalid_floor_is_rejected(self):
        for value in (0, -1, True, None, '1', float('inf'), float('nan')):
            floors = dict(self.recovery['unroundedNecessaryFloors'], auditSeconds=value)
            with self.subTest(value=value), self.assertRaises(ValueError):
                m.necessary_gate(floors)
        with self.assertRaises(ValueError):
            m.necessary_gate({'auditSeconds':1806})
        with self.assertRaises(ValueError):
            m.necessary_gate(self.recovery['unroundedNecessaryFloors'], {'auditSeconds':1})

    def test_resource_limits_margins_reserve_and_charge_are_frozen(self):
        for key in self.plan['resources']:
            plan = copy.deepcopy(self.plan)
            plan['resources'][key] = None
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.result(plan=plan)

    def test_byte_discount_normalization_or_measurement_flags_rejected(self):
        for key,value in (('retainRecoveryFloors',False), ('physicalByteTimeDiscount',True),
                          ('postTimingNormalization',True), ('numericCurrentForecast',{}), ('measurementAuthorization',True)):
            plan = copy.deepcopy(self.plan)
            plan['necessaryGate'][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.result(plan=plan)

    def test_retries_warmups_and_changed_order_are_rejected(self):
        for key,value in (('attemptsPerFormat',2), ('warmups',1), ('retries',1), ('executionOrder',['compressed','plain'])):
            plan = copy.deepcopy(self.plan)
            plan['matchedWorkload'][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.result(plan=plan)

    def test_no_phase_or_accounting_category_can_be_dropped(self):
        for key in ('applicationReadBytes', 'logicalJsonBytes', 'decodedBytesProcessed', 'parseAndReconstruction',
                    'storage', 'scratch', 'memory', 'coverage', 'failureAndCharging', 'endToEnd'):
            plan = copy.deepcopy(self.plan)
            del plan['accounting'][key]
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.result(plan=plan)
        plan = copy.deepcopy(self.plan)
        plan['accounting']['phases'].pop()
        with self.assertRaises(ValueError):
            self.result(plan=plan)

    def test_changed_recovery_cannot_replace_authenticated_history(self):
        recovery = copy.deepcopy(self.recovery)
        recovery['unroundedNecessaryFloors']['auditSeconds'] = 1799
        with self.assertRaisesRegex(ValueError, 'reproduced recovery'):
            self.result(recovery=recovery)

    def test_incomplete_or_unique_only_decode_proof_rejected(self):
        for key in ('completeResultsEqual', 'fullNativeReconstructionPassed', 'independentWorkingAuditPassed'):
            proof = copy.deepcopy(self.proof)
            proof[key] = False
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.result(proof=proof)
        proof = copy.deepcopy(self.proof)
        proof['nativeCodecWork']['decodedBytesProcessed'] //= 2
        with self.assertRaises(ValueError):
            self.result(proof=proof)

    def test_duplicate_or_missing_logical_members_rejected(self):
        for duplicate in (True, False):
            proof = copy.deepcopy(self.proof)
            if duplicate:
                proof['encodedLogicalMembers'][-1] = proof['encodedLogicalMembers'][0]
            else:
                proof['encodedLogicalMembers'].pop()
            with self.subTest(duplicate=duplicate), self.assertRaises(ValueError):
                self.result(proof=proof)

    def test_missing_measurement_evidence_remains_explicit(self):
        evidence = self.result()['missingEvidence']
        self.assertEqual(set(evidence), set(self.plan['readiness'])-{'exactSyntheticEquivalence'})
        self.assertIn('publicationTailAccounting', evidence)
        self.assertIn('peakMemoryAccounting', evidence)
        self.assertIn('peakScratchAccounting', evidence)

    def test_manifest_detects_mutation_extra_members_and_wrong_pin(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            m.write(root/'input.json', {'retained':True})
            m.write(root/'files.json', {'input.json':m.sha(root/'input.json')})
            pin = m.sha(root/'files.json')
            m.manifest(root, pin)
            with self.assertRaises(ValueError):
                m.manifest(root, '0'*64)
            m.write(root/'unexpected.json', {})
            with self.assertRaises(ValueError):
                m.manifest(root, pin)
            (root/'unexpected.json').unlink()
            (root/'input.json').write_text('{}', encoding='utf-8')
            with self.assertRaises(ValueError):
                m.manifest(root, pin)

    def test_unsafe_manifest_paths_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            for name in ('../x', '/x', 'C:/x', 'a\\b', 'a//b', './a', 'a/../b', 'a /b', 'a./b', 'a/'):
                with self.subTest(name=name), self.assertRaises(ValueError):
                    m.member(Path(tmp), name)

    def test_strict_json_and_create_new_output(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp)/'bad.json'
            for raw in ('{"key":1,"key":2}', '{"value":NaN}', '{"value":Infinity}'):
                path.write_text(raw, encoding='utf-8')
                with self.subTest(raw=raw), self.assertRaises(ValueError):
                    m.read(path)
            with self.assertRaises(FileExistsError):
                m.write(path, {})

    def test_registration_rejects_alternate_output_before_creating_it(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = Path(tmp)/'replacement'
            with self.assertRaises(ValueError):
                m.register(output)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
