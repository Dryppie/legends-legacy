"""Saved-stage reconstruction and corrupt-evidence rejection; no game execution."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

path = Path(__file__).with_name('practical-fresh-screening-stage-review.py')
spec = importlib.util.spec_from_file_location('screening_review',path)
review = importlib.util.module_from_spec(spec)
spec.loader.exec_module(review)


class ScreeningReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.trials = {t['id']:t for t in map(json.loads,(review.RUN/'study/trials.jsonl').read_text().splitlines())}
        cls.values = review.read(review.RUN/'allocation.json')['selected']

    def saved(self,pair,side='candidate'):
        return review.read(review.RUN/f'study/pair-{pair:02d}-{side}.json')

    def analyze(self,pair,side='candidate',search=None,trials=None):
        return review.arm(search or self.saved(pair,side), trials or self.trials,
                          self.values[(pair-1)*49:pair*49],side == 'candidate')

    def test_baseline_pair_three_exposes_nonprimary_reference_tie(self):
        a = self.analyze(3,'baseline')
        self.assertEqual((23,0,True,'FrozenDiscoveryOrder'),
                         (a['selectionWins'][a['selected']],a['winnerReferenceMargin'],a['unprotectedReferenceTie'],a['selectionReason']))
        self.assertEqual(46,len(a['records']))

    def test_screening_pair_eleven_exposes_same_tie(self):
        a = self.analyze(11)
        self.assertEqual((25,0,True),(a['selectionWins'][a['selected']],a['winnerReferenceMargin'],a['unprotectedReferenceTie']))

    def test_pair_six_strict_lead_is_not_a_tie(self):
        a = self.analyze(6)
        self.assertEqual((26,2,False,'UniqueMaximum'),
                         (a['selectionWins'][a['selected']],a['winnerReferenceMargin'],a['unprotectedReferenceTie'],a['selectionReason']))

    def test_pair_five_reconstructs_screen_and_unknown_later_measurements(self):
        a = self.analyze(5)
        self.assertEqual((23,5,28),(len(a['screenMembers']),len(a['shortlist']),a['selectionWins'][a['selected']]))
        self.assertEqual(41,sum(r['selectionWins'] is None for r in a['records']))
        self.assertEqual(23,sum(r['screeningWins'] is None for r in a['records']))

    def test_screen_membership_and_nomination_tampering_rejected(self):
        for kind in ['members','nominees','measurement']:
            x = self.saved(5)
            if kind == 'members': x['screening']['freeze']['candidates'].reverse()
            if kind == 'nominees': x['screening']['nominees'].reverse()
            if kind == 'measurement': x['screening']['measurements'].pop()
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.analyze(5,search=x)

    def test_screen_boundaries_and_seed_reuse_rejected(self):
        for kind in ['boundary','seeds','version']:
            x = self.saved(5)
            if kind == 'boundary': x['screening']['afterSearchFights']=367
            if kind == 'seeds': x['screening']['freeze']['seeds'][0]=self.values[4*49+1]
            if kind == 'version': x['screening']['version']='changed'
            with self.subTest(kind=kind), self.assertRaisesRegex(ValueError,'boundary'):
                self.analyze(5,search=x)

    def test_partial_nonboolean_and_wrong_fitness_rejected(self):
        for kind in ['partial','boolean','fitness']:
            x = self.saved(6); row=x['selection'][0]
            if kind == 'partial': row['cells'][0]['clears'].pop()
            if kind == 'boolean': row['cells'][0]['clears'][0]=1
            if kind == 'fitness': row['fitness']['worstContextWinRate']=0
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.analyze(6,search=x)

    def test_wrong_stage_and_reserved_panel_rejected(self):
        x=self.saved(6); ident=x['selection'][0]['cells'][0]['trials'][0]
        for change in [dict(stage='screening'),dict(seed=0)]:
            trials=dict(self.trials); trials[ident]=dict(trials[ident],**change)
            with self.subTest(change=change), self.assertRaises(ValueError):
                self.analyze(6,search=x,trials=trials)

    def test_output_and_parent_tampering_rejected(self):
        for kind in ['output','parent']:
            x=self.saved(5)
            if kind == 'output': x['output']['finalist']['party']['id']='changed'
            else: x['discovery']['arms'][0]['proposals'][12]['provenance']['parentIds']=['missing']
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                self.analyze(5,search=x)

    def test_shared_discovery_prefix_and_selection_rows_reconstruct(self):
        searches=[self.saved(3,s) for s in ['baseline','candidate']]
        arms=[self.analyze(3,s) for s in ['baseline','candidate']]
        compared=review.compare(*arms,searches)
        self.assertGreaterEqual(compared['sharedEvaluated'],3)
        self.assertFalse(compared['sameOutput'])
        self.assertTrue(compared['candidateOutputInBaseline']['present'])
        tampered=copy.deepcopy(searches)
        row=next(r for r in tampered[1]['discovery']['arms'][0]['evaluations'] if r['id'] in review.REFERENCES)
        row['cells'][0]['clears'][0]=not row['cells'][0]['clears'][0]
        with self.assertRaisesRegex(ValueError,'prefix'):
            review.compare(*arms,tampered)

    def test_missing_reference_cannot_be_screened_out(self):
        x=self.saved(5)
        rows=[r for r in x['screening']['measurements'] if r['id'] != review.old.PRIMARY]
        with self.assertRaisesRegex(ValueError,'protected'):
            review.protected(rows,2)


if __name__ == '__main__':
    unittest.main(verbosity=2)
