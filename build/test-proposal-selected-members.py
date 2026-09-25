"""Physical selected-team grouping regressions; no campaign or timing measurement."""
import argparse
import copy
import importlib.util
from pathlib import Path
import sys
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('selected_audit',ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py')
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class SelectedMembers(unittest.TestCase):
    def setUp(self):
        self.context = dict(scope=dict(budget=dict(essenceSlots=1),contexts=[dict(characterTemplates=[
            dict(partySlot=1,build=dict(equipment=[],essenceIds=['e0'],identityEssenceIds=['neutral-identity-slot-1']))])]))
        self.control = dict(id='same',source='v4',builds={'1':['e0']})
        self.candidate = dict(self.control,source='v5')
        self.benchmark = dict(id='benchmark',source='reference',builds={'1':['e1']})
        self.outputs = dict(control=self.control,candidate=self.candidate,benchmark=self.benchmark)
        self.members = [self.member(self.control,['candidate','control']),self.member(self.benchmark,['benchmark'])]

    def member(self,party,roles):
        return dict(party=copy.deepcopy(party),scenario=dict(party=a.physical_party(self.context,party)),roles=roles)

    def test_provenance_difference_shares_exact_physical_team(self):
        roles = a.frozen_members(self.context,self.outputs,self.members)
        self.assertIs(roles['candidate'],roles['control'])
        self.assertEqual('v4',roles['candidate']['party']['source'])
        self.assertEqual('v5',self.candidate['source'])

    def test_representative_follows_native_priority_not_sorted_role_names(self):
        self.members[0]['party'] = self.candidate
        with self.assertRaisesRegex(ValueError,'representative'): a.frozen_members(self.context,self.outputs,self.members)

    def test_representative_metadata_is_authenticated(self):
        for field,value in [('source','changed'),('id','changed'),('builds',{'1':['e2']})]:
            with self.subTest(field=field):
                members=copy.deepcopy(self.members); members[0]['party'][field]=value
                with self.assertRaises(ValueError): a.frozen_members(self.context,self.outputs,members)

    def test_roles_cannot_be_dropped_added_duplicated_or_reordered(self):
        for roles in (['control'],['candidate','control','unexpected'],['candidate','candidate','control'],['control','candidate']):
            with self.subTest(roles=roles):
                members=copy.deepcopy(self.members); members[0]['roles']=roles
                with self.assertRaises(ValueError): a.frozen_members(self.context,self.outputs,members)

    def test_changed_physical_output_cannot_share_the_old_group(self):
        outputs=copy.deepcopy(self.outputs); outputs['candidate']['builds']['1']=['e2']
        with self.assertRaises(ValueError): a.frozen_members(self.context,outputs,self.members)

    def test_physical_groups_cannot_be_split_or_duplicated(self):
        split=[self.member(self.control,['control']),self.member(self.candidate,['candidate']),self.members[1]]
        with self.assertRaises(ValueError): a.frozen_members(self.context,self.outputs,split)
        with self.assertRaises(ValueError): a.frozen_members(self.context,self.outputs,[self.members[0],self.members[0]])

    def test_all_three_roles_can_share_control_representative(self):
        outputs=dict(self.outputs,benchmark=dict(self.control,id='reference-alias',source='reference'))
        members=[self.member(self.control,['benchmark','candidate','control'])]
        roles=a.frozen_members(self.context,outputs,members)
        self.assertIs(roles['benchmark'],roles['control'])


class RetainedArchiveRegression(unittest.TestCase):
    failed_fixture = None
    completed_fixture = None
    closeout_pin = None

    def test_failed_rows_reconstruct_but_public_failure_guard_stays_closed(self):
        if self.failed_fixture is None: self.skipTest('No failed fixture supplied')
        root=self.failed_fixture; marker=root/'failure.json'; before=a.sha(marker)
        with self.assertRaisesRegex(ValueError,'Incomplete study'): a.audit(root,True)
        exists=Path.exists
        # Test-only fault isolation: suppress exactly the terminal guard while
        # reconstructing saved rows. No files are changed, no receipt is published,
        # and the public entry point remains forbidden for this failed attempt.
        with patch.object(Path,'exists',lambda path: False if path==marker else exists(path)):
            diagnostic=a.audit(root,True)
        native=a.read(root/'native-audit.log')
        self.assertEqual(native,diagnostic['result'])
        self.assertEqual(17280,diagnostic['result']['fights'])
        self.assertEqual(before,a.sha(marker))
        with self.assertRaisesRegex(ValueError,'Incomplete study'): a.audit(root,True)

    def test_completed_placement_archive_still_verifies(self):
        if self.completed_fixture is None: self.skipTest('No completed fixture supplied')
        result=a.audit(self.completed_fixture,False,self.closeout_pin)
        self.assertEqual('Passed',result['status']); self.assertEqual(18816,result['result']['fights'])


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--failed-fixture',type=Path); parser.add_argument('--completed-fixture',type=Path); parser.add_argument('--closeout-pin')
    args,remaining=parser.parse_known_args()
    RetainedArchiveRegression.failed_fixture=args.failed_fixture.resolve() if args.failed_fixture else None
    RetainedArchiveRegression.completed_fixture=args.completed_fixture.resolve() if args.completed_fixture else None
    RetainedArchiveRegression.closeout_pin=args.closeout_pin
    unittest.main(argv=[sys.argv[0],*remaining])
