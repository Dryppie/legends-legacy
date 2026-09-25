"""Saved-stage and corrupt-input checks; no battle execution or allocation."""
import importlib.util
import json
from pathlib import Path
import unittest

path = Path(__file__).with_name('three-reference-tie-stage-review.py')
spec = importlib.util.spec_from_file_location('tie_review',path)
review = importlib.util.module_from_spec(spec)
spec.loader.exec_module(review)


class ReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.trials = {t['id']:t for t in map(json.loads,(review.RUN/'study/trials.jsonl').read_text().splitlines())}
        cls.values = review.read(review.RUN/'allocation.json')['selected']
        cls.audit = review.module('test_saved_tie_auditor',review.RUN/'auditor.py',review.read(review.RUN/'request.json')['auditorHash'])

    def saved(self,root):
        return review.read(review.RUN/f'study/search-{root:02d}.json')

    def analyze(self,root,search=None,trials=None,values=None):
        return review.arm(search or self.saved(root),trials or self.trials,
                          values or self.values[(root-1)*41:root*41],self.audit)

    def test_classification_distinguishes_tie_opportunity_from_changed_choice(self):
        order = ['c','r3','r1','r2','d']
        cases = [([20,19,18,17,16],'c','UniqueChallengerLeader'),
                 ([19,20,18,17,16],'r3','UniqueReferenceLeader'),
                 ([20,20,20,17,16],'r1','PrimaryPositiveTie'),
                 ([20,20,18,17,16],'c','UnprotectedReferenceTie'),
                 ([20,20,18,17,16],'r3','OtherReferenceAlreadyFirst'),
                 ([20,19,18,17,20],'c','ChallengerOnlyTie'),
                 ([0,0,0,0,0],'d','ZeroWinHealth')]
        for counts,selected,expected in cases:
            with self.subTest(expected=expected):
                frozen = ['r3','c','r1','r2','d'] if expected == 'OtherReferenceAlreadyFirst' else order
                self.assertEqual(expected,review.classify(dict(zip(frozen,counts)),frozen,'r1',['r1','r2','r3'],selected))
        with self.assertRaisesRegex(ValueError,'nominee order'):
            review.classify(dict(zip(order,[20,20,18,17,16])),order,'r1',['r1','r2','r3'],'r3')

    def test_all_five_challengers_have_one_win_strict_leads(self):
        for root in [8,13,15,21,22]:
            with self.subTest(root=root):
                a = self.analyze(root)
                self.assertEqual(('UniqueChallengerLeader',1,1),(a['classification'],a['winnerMargin'],a['winnerReferenceMargin']))
                paired = a['selectedVersusReferences'][a['bestReference']]
                self.assertEqual(1,paired['gains']-paired['losses'])
                self.assertIsNone(a['absoluteConfirmationWins'])

    def test_other_reference_ties_already_choose_reference(self):
        for root in [2,24]:
            a = self.analyze(root)
            self.assertEqual('OtherReferenceAlreadyFirst',a['classification'])
            self.assertTrue(a['nonprimaryReferenceChallengerTopTie'])
            self.assertEqual(review.REFERENCES[2],a['selected'])

    def test_primary_ties_retain_original_designation(self):
        for root in [11,14,23]:
            a = self.analyze(root)
            self.assertEqual(('PrimaryPositiveTie',review.REFERENCES[0]),(a['classification'],a['selected']))

    def test_missing_later_measurements_and_sensitive_subsets_stay_distinct(self):
        a = self.analyze(8)
        self.assertEqual(46,len(a['records']))
        self.assertEqual(41,sum(r['selectionWins'] is None for r in a['records']))
        self.assertEqual(32,len(a['sensitivity']['leaveOneOutWinners']))
        self.assertIsNone(review.stage.positive_winner({'r':0,'c':0},['r','c'],'r'))

    def test_wrong_reserved_panel_or_stage_fails(self):
        s = self.saved(8)
        ident = s['selection'][0]['cells'][0]['trials'][0]
        for change in [dict(seed=0),dict(stage='confirmation')]:
            trials = dict(self.trials)
            trials[ident] = dict(trials[ident],**change)
            with self.subTest(change=change),self.assertRaises(ValueError):
                self.analyze(8,trials=trials)
        block = self.values[7*41:8*41]
        block[1] = block[0]
        with self.assertRaises(ValueError):
            self.analyze(8,values=block)

    def test_selector_output_recipe_and_nominee_tampering_fails(self):
        for change in ['selector','output','recipe','nominee']:
            s = self.saved(8)
            if change == 'selector': s['candidate']['selector']='changed'
            if change == 'output': s['candidate']['finalist']['party']['id']='changed'
            if change == 'recipe': s['candidate']['recipeHash']='changed'
            if change == 'nominee': s['discovery']['discoveryShortlist'].reverse()
            with self.subTest(change=change),self.assertRaises(ValueError):
                self.analyze(8,search=s)

    def test_missing_control_and_unresolved_parent_fails(self):
        for change in ['control','parent']:
            s = self.saved(8)
            if change == 'control':
                s['discovery']['arms'][0]['proposals'][0]['provenance']['operator']='changed'
            else:
                s['discovery']['arms'][0]['proposals'][12]['provenance']['parentIds']=['missing']
            with self.subTest(change=change),self.assertRaises(ValueError):
                self.analyze(8,search=s)

    def test_partial_nonboolean_and_changed_fitness_fails(self):
        for change in ['partial','boolean','fitness']:
            s = self.saved(8)
            row = s['selection'][0]
            if change == 'partial': row['cells'][0]['clears'].pop()
            if change == 'boolean': row['cells'][0]['clears'][0]=1
            if change == 'fitness': row['fitness']['worstContextWinRate']=0
            with self.subTest(change=change),self.assertRaises(ValueError):
                self.analyze(8,search=s)


if __name__ == '__main__':
    unittest.main(verbosity=2)
