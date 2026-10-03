"""Qualified floor-thirteen preparation must retain its recipe and receipt guards."""
import importlib.util
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch


def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


q = module('floor13_integration_qualification', 'tower-catalog-qualification.py')
io = q.owner_module()
fixtures = module('floor13_integration_fixtures', 'test-tower-floor13-family-binding.py')
b = fixtures.b


class FamilyIntegrationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        fixtures.FamilyBindingTests.setUpClass()

    def setUp(self):
        self.fixture = fixtures.FamilyBindingTests()
        self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        self.root = self.fixture.root
        self.source = self.root/'source'
        self.owner = self.root/'qualification'
        self.source.mkdir()
        self.owner.mkdir()
        self.save(self.source/'cells.json', self.fixture.original)
        self.save(self.source/'files.json', {'cells.json': io.sha(self.source/'cells.json')})
        self.save(self.source/'result.json', dict(fights=18200))
        self.plan_path = self.root/'plan.json'
        self.plan = dict(floor=13, sourceManifestSha256=io.sha(self.source/'files.json'),
            currentContentHashes={'world-tower/tower-floors.json': 'a'*64, 'combat/abilities.json': 'b'*64},
            assemblyHashes={'Combat': 'runtime'})
        self.save(self.plan_path, self.plan)
        self.audit = self.root/'audit.json'
        self.save(self.audit, dict(status='Verified', assessment={'verdict': 'Pass', 'familySize': 130},
            evaluationFights=18200, resultSha256=io.sha(self.source/'result.json')))
        self.save(self.owner/'request.json', dict(qualificationPlan=str(self.plan_path),
            qualificationPlanPin=io.sha(self.plan_path), manifestPin=io.sha(self.source/'files.json'),
            audit=str(self.audit), auditPin=io.sha(self.audit)))
        self.save(self.owner/'result.json', dict(status='QualifiedInputsAndReplaysVerified',
            qualificationPlanPin=io.sha(self.plan_path), manifestPin=io.sha(self.source/'files.json'),
            matchedInputs=18200, fullReplays=130, newSeeds=0, execution={'assemblyHashes': {'Combat': 'runtime'}}))
        self.save(self.owner/'completion.json', dict(status='Qualified', resultSha256=io.sha(self.owner/'result.json'),
            matchedInputs=18200, fullReplays=130, newSeeds=0))
        self.save(self.owner/'process.json', dict(exitCode=0, timedOut=False, activeProcesses=0))
        self.binding = {**self.fixture.binding, 'sourceManifestSha256': io.sha(self.source/'files.json'),
                        'sourceCellsSha256': io.sha(self.source/'cells.json')}
        self.wrapper = self.root/'binding.json'
        self.save(self.wrapper, self.binding)
        self.admission = self.root/'admission.json'
        self.rebind()
        # Synthetic qualification receipts test the admission seam; they do not
        # establish native parity. Real execution retains validate_plan in full.
        for target, name, value in [(b, 'SOURCE_MANIFEST', self.binding['sourceManifestSha256']),
                                    (b, 'SOURCE_CELLS', self.binding['sourceCellsSha256']),
                                    (q, 'floor13_family_module', lambda: b)]:
            replacement = patch.object(target, name, value)
            replacement.start()
            self.addCleanup(replacement.stop)

    def save(self, path, value):
        path.write_text(json.dumps(value), encoding='utf-8')

    def rebind(self):
        self.save(self.admission, dict(qualificationOwner=str(self.owner),
            qualificationPins={name: io.sha(self.owner/name) for name in
                ('request.json', 'result.json', 'process.json', 'completion.json')},
            proposal=str(self.wrapper), proposalSha256=io.sha(self.wrapper)))

    def admit(self, pins=None):
        with patch.object(q, 'validate_plan') as validation:
            cells = q.admit_family(self.admission, self.source, self.root/'api', self.root/'runtime', input_pins=pins)
            validation.assert_called_once_with(self.plan, self.source, self.root/'api', self.root/'runtime')
            return cells

    def test_complete_binding_is_dispatched_without_changing_controls(self):
        cells = q.validate_family(self.binding, self.fixture.original)
        self.assertEqual(cells, self.fixture.proposal['cells'])
        self.assertEqual(cells[:130], self.fixture.original)
        self.assertEqual(len(cells), 240)

    def test_original_unqualified_proposal_cannot_bypass_binding(self):
        with self.assertRaisesRegex(ValueError, 'Unknown family'):
            q.validate_family(self.fixture.proposal, self.fixture.original)

    def test_native_receipts_are_required_before_recipe_validation(self):
        (self.owner/'result.json').unlink()
        with patch.object(q, 'validate_family') as family:
            with self.assertRaises(FileNotFoundError): self.admit()
            family.assert_not_called()

    def test_catalog_qualification_remains_mandatory(self):
        with patch.object(q, 'validate_plan', side_effect=ValueError('Catalog changed')):
            with patch.object(q, 'validate_family') as family:
                with self.assertRaisesRegex(ValueError, 'Catalog changed'):
                    q.admit_family(self.admission, self.source, self.root/'api', self.root/'runtime')
                family.assert_not_called()

    def test_changed_receipt_is_rejected(self):
        self.save(self.owner/'process.json', dict(exitCode=1))
        with self.assertRaisesRegex(ValueError, 'receipt changed'): self.admit()

    def test_missing_receipt_pin_is_rejected(self):
        admission = io.read(self.admission)
        del admission['qualificationPins']['process.json']
        self.save(self.admission, admission)
        with self.assertRaisesRegex(ValueError, 'Incomplete qualification'): self.admit()

    def test_incomplete_replays_fail_even_with_rebound_hashes(self):
        result = io.read(self.owner/'result.json')
        completion = io.read(self.owner/'completion.json')
        result['fullReplays'] = completion['fullReplays'] = 129
        self.save(self.owner/'result.json', result)
        completion['resultSha256'] = io.sha(self.owner/'result.json')
        self.save(self.owner/'completion.json', completion)
        self.rebind()
        with self.assertRaisesRegex(ValueError, 'parity incomplete'): self.admit()

    def test_runtime_mismatch_cannot_be_rebound(self):
        result = io.read(self.owner/'result.json')
        result['execution']['assemblyHashes']['Combat'] = 'different'
        self.save(self.owner/'result.json', result)
        completion = io.read(self.owner/'completion.json')
        completion['resultSha256'] = io.sha(self.owner/'result.json')
        self.save(self.owner/'completion.json', completion)
        self.rebind()
        with self.assertRaisesRegex(ValueError, 'parity incomplete'): self.admit()

    def test_failed_timed_out_or_active_qualification_cannot_admit(self):
        for field, value in [('exitCode', 1), ('timedOut', True), ('activeProcesses', 1)]:
            self.save(self.owner/'process.json', {**dict(exitCode=0, timedOut=False, activeProcesses=0), field: value})
            self.rebind()
            with self.subTest(field=field):
                with self.assertRaisesRegex(ValueError, 'parity incomplete'): self.admit()

    def test_binding_cannot_substitute_current_catalog(self):
        self.save(self.wrapper, {**self.binding, 'currentTowerSha256': 'c'*64})
        self.rebind()
        pins = {'existing': 'unchanged'}
        with self.assertRaisesRegex(ValueError, 'current catalog differs'): self.admit(pins)
        self.assertEqual(pins, {'existing': 'unchanged'})

    def test_admitted_native_pins_bind_saved_artifact_and_all_receipts(self):
        pins = {'existing': 'unchanged'}
        self.assertEqual(len(self.admit(pins)), 240)
        expected = {**b.input_pins(self.binding), **{str(self.owner/name): io.sha(self.owner/name) for name in
            ('request.json', 'result.json', 'completion.json', 'process.json')},
            **{str(p): io.sha(p) for p in (self.wrapper, self.plan_path, self.audit, self.admission)}, 'existing': 'unchanged'}
        self.assertEqual(pins, expected)

    def cli(self, extra=()):
        return ['owner', '--mode', 'prepare', '--floor', '13', '--name', 'floor13-guard-test',
                '--source', str(self.source), '--artifacts', str(self.root/'artifacts'),
                '--qualified-family', str(self.admission), *extra]

    def test_seed_free_floor_thirteen_reaches_existing_preparation_route(self):
        tests = self.root/'artifacts/bin/EssenceSystem.Tests/release'
        tests.mkdir(parents=True)
        (tests/'EssenceSystem.Tests.dll').write_bytes(b'synthetic fixture')
        with patch.object(io, 'ROOT', self.root), patch.object(sys, 'argv', self.cli()):
            with patch.object(io, 'history', side_effect=RuntimeError('preparation boundary')) as history:
                with self.assertRaisesRegex(RuntimeError, 'preparation boundary'): io.main()
                history.assert_called_once()
        self.assertFalse((self.root/'TestResults').exists())

    def test_combat_modes_and_other_modifications_rejected_before_allocation(self):
        changes = [ ['--mode', mode] for mode in ('screen', 'confirm', 'search') ] + [
            ['--floor', '15'], ['--current-content'], ['--health-factor', '.8'], ['--offense-factor', '.8'],
            ['--penetration-factor', '2'], ['--add-search', 'extra'], ['--add-references', 'extra'],
            ['--ability-candidate', 'candidate'], ['--health-pressure-candidate', 'candidate'],
            ['--recovery-pressure-candidate', 'candidate'], ['--gear-reference', 'a', 'b'],
            ['--fixed-support-reference', 'a', 'b', '1'], ['--one-healer-family', 'candidate'],
            ['--eight-item-family', 'candidate'], ['--ni-restoration-family', 'candidate'],
            ['--search-gear', 'baseline'], ['--native-seconds', '1140']]
        for changed in changes:
            with self.subTest(changed=changed), patch.object(io, 'ROOT', self.root), patch.object(sys, 'argv', self.cli(changed)):
                with patch.object(io, 'history') as history:
                    with self.assertRaises(ValueError): io.main()
                    history.assert_not_called()
        self.assertFalse((self.root/'TestResults').exists())


if __name__ == '__main__':
    unittest.main()
