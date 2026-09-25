"""Independent nomination contract and engineering monitor regression checks."""
import copy
import importlib.util
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path)
    value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


audit=module('nomination_audit',ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py')
cycle=module('nomination_cycle_tests',ROOT/'Balance Harness/analysis/run-affinity-nomination-cycle.py')


class NominationTests(unittest.TestCase):
    def test_exact_generator_and_validation_contract(self):
        policy=dict(version=audit.CREATION_POLICY,name='benchmark-affinity-creation-v3',firstWave=['affinity-create']*9,
            secondWave=['affinity-create']*8,parentTickets=['benchmark'],preserveParentInteractions=False,
            preservedDamageAffinityIds=[],createdDamageAffinityIds=['a'*64])
        design=dict(version=audit.NOMINATION_VERSION,control=copy.deepcopy(policy),candidate=copy.deepcopy(policy),
            selectionContrast=dict(control=audit.VALIDATION_POLICY,candidate=audit.VALIDATION_POLICY),
            validationProtocol=audit.preservation_protocol())
        audit.validate_policies(design)
        for edit in (lambda d:d['candidate'].update(parentTickets=['beam']),
            lambda d:d['candidate'].update(creationRemovalRule=audit.REMOVAL_RULE),
            lambda d:d['control'].update(createdDamageAffinityIds=['b'*64]),
            lambda d:d['selectionContrast'].update(candidate=audit.SELECTOR_POLICY),
            lambda d:d['validationProtocol'].update(validationValues=59)):
            bad=copy.deepcopy(design); edit(bad)
            with self.assertRaises(ValueError): audit.validate_policies(bad)

    def test_selection_without_reference_preserves_frozen_order_and_zero_win_health(self):
        def score(pid,wins,health): return dict(id=pid,wins=wins,fitness=dict(guardianHealth=health))
        self.assertEqual('second',audit.select([score('first',3,0),score('second',3,99)],['second','first'],'old-reference'))
        self.assertEqual('first',audit.select([score('first',0,20),score('second',0,30)],['second','first'],'old-reference'))
        self.assertEqual('old-reference',audit.select([score('first',3,0),score('old-reference',3,99)],['first','old-reference'],'old-reference'))

    def test_advancement_needs_both_gains_and_three_promising_novel_outputs(self):
        decide=lambda *args:audit.decision(*args,audit.NOMINATION_VERSION)
        self.assertEqual('LargerFreshEvaluationWarranted',decide(.02,.02,3,3))
        for values in ((.019,.02,3,3),(.02,.019,3,3),(.02,.02,2,3),(.02,.02,3,2)):
            self.assertEqual('Inconclusive',decide(*values))
        self.assertEqual('NoObservedOutputDifferentiation',decide(0,0,0,0))
        self.assertEqual('AbandonThisConfiguration',decide(.02,-.02,12,12))
        self.assertEqual('AbandonThisConfiguration',decide(-.02,.02,12,12))

    def test_all_exposed_values_stay_reserved(self):
        allocation=audit.classify(struct.pack('<16384i',*range(200000,216384)),[],audit.NOMINATION_VERSION)
        self.assertEqual((4380,16384),(len(allocation['selected']),len(allocation['reserved'])))

    def test_outer_monitor_uses_result_relative_native_lease_rules(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); result=root/'tower-proposal-owned-fixture-nomination/result'
            result.mkdir(parents=True); (root/'support.json').write_bytes(b'123')
            live=result/'search/root-01/control/racing.writer.lock'
            with patch.object(cycle.owner,'inventory',return_value=[root/'support.json',live]), \
                 patch.object(cycle.owner,'storage_bytes',return_value=7) as storage:
                self.assertEqual(10,cycle.fixture_storage(root))
                storage.assert_called_once_with(result)

    def test_outer_monitor_accepts_only_empty_known_owner_leases(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); registry=root/'tower-proposal-owned-fixture-nomination'
            registry.mkdir(); paths=[registry/(n+'.writer.lock') for n in ('result','complete-family-allocation')]
            with patch.object(cycle.owner,'inventory',return_value=paths):
                self.assertEqual(0,cycle.fixture_storage(root))
                paths[0].write_bytes(b'not empty')
                with self.assertRaisesRegex(ValueError,'lease must be empty'): cycle.fixture_storage(root)


if __name__=='__main__': unittest.main()
