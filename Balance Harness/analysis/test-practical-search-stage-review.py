"""Literal panel sensitivity and corrupt saved-evidence rejection; no combat."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

path = Path(__file__).with_name('practical-search-stage-review.py')
spec = importlib.util.spec_from_file_location('stage_review', path)
review = importlib.util.module_from_spec(spec)
spec.loader.exec_module(review)


def row(pid, clears, prefix=None):
    return dict(id=pid, fitness=dict(worstContextWinRate=sum(clears)/len(clears), guardianHealth=1, survival=1, victoryDuration=1),
        cells=[dict(clears=clears, guardianHealth=1, trials=[f'{prefix or pid}-{i}' for i in range(len(clears))])])


class StageReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        run = review.ROOT/'TestResults/balance/tower-reference-exploration-offset-comparison-20260922/study'
        cls.saved = review.read(run/'pair-06-candidate.json')
        cls.trials = {r['id']:r for r in map(json.loads, (run/'trials.jsonl').read_text().splitlines())}

    def test_positive_tie_uses_designated_and_other_ties_use_order(self):
        self.assertEqual('p', review.positive_winner(dict(p=2,a=2,b=1), ['a','p','b'], 'p'))
        self.assertEqual('b', review.positive_winner(dict(p=1,a=2,b=2), ['b','a','p'], 'p'))

    def test_strict_leader_cannot_be_overridden(self):
        self.assertEqual('a', review.positive_winner(dict(p=1,a=2), ['p','a'], 'p'))

    def test_zero_subsets_are_unassessed(self):
        self.assertIsNone(review.positive_winner(dict(p=0,a=0), ['p','a'], 'p'))

    def test_missing_and_duplicate_subset_members_rejected(self):
        for order in [['p'],['p','p']]:
            with self.assertRaises(ValueError):
                review.positive_winner(dict(p=1,a=1), order, 'p')

    def test_halves_and_leave_one_out_have_literal_answers(self):
        rows = [row('p',[True]*16+[False]*16),row('a',[False]*16+[True]*16)]
        result = review.sensitivity(rows,['a','p'],'p','p')
        self.assertEqual(('p','a',True,16,0), (result['firstHalf'],result['secondHalf'],result['halvesDisagree'],
            result['leaveOneOutChanges'],result['unassessableSubsets']))

    def test_unanimous_selection_is_stable(self):
        result = review.sensitivity([row('p',[True]*32),row('a',[False]*32)], ['p','a'],'p','p')
        self.assertEqual((False,0,0),(result['halvesDisagree'],result['halfDepartures'],result['leaveOneOutChanges']))

    def test_trial_ids_can_differ_but_seed_order_must_agree(self):
        rows = [row('p',[True]*8),row('a',[False]*8)]
        trials = {f'{pid}-{i}':dict(stage='discovery',seed=i) for pid in ['p','a'] for i in range(8)}
        self.assertEqual(2,len(review.panel(rows,8,trials,'discovery')))
        trials['a-0']['seed'] = 7
        trials['a-7']['seed'] = 0
        with self.assertRaisesRegex(ValueError,'Misaligned'):
            review.panel(rows,8,trials,'discovery')

    def test_partial_nonboolean_and_repeated_trial_panels_rejected(self):
        for kind in ['partial','nonboolean','repeated']:
            rows = [row('p',[True]*8)]
            if kind=='partial': rows[0]['cells'][0]['clears'].pop()
            if kind=='nonboolean': rows[0]['cells'][0]['clears'][0]=1
            if kind=='repeated': rows[0]['cells'][0]['trials'][1]=rows[0]['cells'][0]['trials'][0]
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                review.panel(rows,8)

    def test_wrong_stage_and_missing_trial_rejected(self):
        for kind in ['stage','missing']:
            rows = [row('p',[True]*8)]
            trials={f'p-{i}':dict(stage='discovery',seed=i) for i in range(8)}
            if kind=='stage': trials['p-0']['stage']='selection'
            else: del trials['p-0']
            with self.subTest(kind=kind), self.assertRaisesRegex(ValueError,'trial'):
                review.panel(rows,8,trials,'discovery')

    def test_saved_offset_reconstructs_known_output_and_partition(self):
        result = review.arm(self.saved,self.trials)
        self.assertEqual('90bded7e732aad2074d72d76a6148eca9a41c602a611dcef42c7b5dbf3680b4a',result['selected'])
        self.assertEqual((46,5,41),(len(result['records']),sum(r['nominated'] for r in result['records']),
            sum(r['exclusion'] is not None for r in result['records'])))

    def test_saved_nomination_parent_output_and_measurement_tampering_rejected(self):
        for kind in ['nomination','parent','output','measurement','duplicate']:
            saved=copy.deepcopy(self.saved)
            if kind=='nomination': saved['discovery']['discoveryShortlist'].reverse()
            if kind=='parent': saved['discovery']['arms'][0]['proposals'][12]['provenance']['parentIds']=['missing']
            if kind=='output': saved['output']['finalist']['party']['id']='changed'
            if kind=='measurement': saved['selection'][0]['fitness']['worstContextWinRate']=0
            if kind=='duplicate': saved['selection'][1]=copy.deepcopy(saved['selection'][0])
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                review.arm(saved,self.trials)


if __name__=='__main__':
    unittest.main()
