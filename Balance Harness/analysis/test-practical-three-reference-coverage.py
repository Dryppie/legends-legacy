"""Regression checks for saved-row trajectory and sensitivity analysis."""
import importlib.util
from pathlib import Path
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('three_coverage', HERE/'practical-three-reference-coverage.py')
review = importlib.util.module_from_spec(spec)
spec.loader.exec_module(review)
coverage, selector, _, _ = review.ingredients()


def row(pid, outcomes, health=10):
    return dict(id=pid, cells=[dict(clears=outcomes, guardianHealth=health)], fitness=dict(guardianHealth=health))


class ReviewTests(unittest.TestCase):
    def test_composition_distance_preserves_owners(self):
        a = {'1':['a','b'], '2':['c','d']}
        self.assertEqual(coverage.distance(a, {'1':['b','a'], '2':['d','c']}), 0)
        self.assertEqual(coverage.distance(a, {'1':['c','b'], '2':['a','d']}), 2)

    def test_equal_rates_can_hide_different_paired_outcomes(self):
        self.assertEqual(review.paired([True,True,False,False], [True,False,True,False]),
            dict(samples=4,leftWins=2,rightWins=2,bothWin=1,leftOnly=1,rightOnly=1,bothLose=1))

    def test_incomplete_or_nonboolean_pairing_is_rejected(self):
        for left,right in [([True],[True,False]), ([1],[True]), ([],[])]:
            with self.assertRaises(ValueError):
                review.paired(left,right)

    def test_frozen_shortlist_deletion_sensitivity(self):
        rows = [row('a',[True,True,True,False]), row('b',[True,True,False,True]), row('c',[True,False,False,True])]
        result = review.selection_sensitivity(rows,['a','b','c'],{'a','c'},'c',selector)
        self.assertEqual(result['actualSelected'],'a')
        # Omitting the second position brings designated c into the positive top tie.
        self.assertEqual(result['selectedCounts'],{'a':2,'c':1,'b':1})
        self.assertEqual(rows[0]['cells'][0]['clears'],[True,True,True,False])

    def test_designated_reference_is_respected(self):
        rows = [row('a',[True,True,True,False]), row('b',[True,True,False,True])]
        result = review.selection_sensitivity(rows,['a','b'],{'b'},'b',selector)
        self.assertEqual(result['actualSelected'],'b')
        self.assertEqual(result['selectedCounts'],{'b':3,'a':1})

    def test_zero_win_health_not_invented_after_deletion(self):
        with self.assertRaisesRegex(ValueError,'positive counts'):
            review.selection_sensitivity([row('a',[True,False])],['a'],{'a'},'a',selector)

    def test_population_runs_skip_fresh_events_but_count_rejections(self):
        populations = [['a','b'],[],['a','b'],['b','a'],[],['b','a']]
        result = review.population_runs([dict(supplied=dict(populationIds=p)) for p in populations])
        self.assertEqual(result,[dict(firstProposal=0,lastProposal=2,mutationDecisions=2,partyIds=['a','b']),
                                 dict(firstProposal=3,lastProposal=5,mutationDecisions=2,partyIds=['b','a'])])

    def test_changed_saved_input_rejected_before_or_after_read(self):
        with tempfile.TemporaryDirectory() as directory:
            p = Path(directory)/'data.json'; p.write_text('{"value":1}')
            inputs = review.Inputs(); digest = review.sha(p)
            self.assertEqual(inputs.read(p,digest),dict(value=1))
            p.write_text('{"value":2}')
            with self.assertRaisesRegex(ValueError,'Changed pinned input'):
                inputs.read(p,digest)
            with self.assertRaisesRegex(ValueError,'changed during analysis'):
                inputs.recheck()


if __name__ == '__main__':
    unittest.main()
