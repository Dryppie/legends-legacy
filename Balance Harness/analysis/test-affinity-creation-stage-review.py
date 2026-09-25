"""Creation-path recount and rejection tests. No combat or registry writes."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('creation_review',Path(__file__).with_name('affinity-creation-stage-review.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class CreationReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scientific=a.Evidence(a.RUN,a.RUN_PIN); cls.admission=a.Evidence(a.ADMISSION,a.ADMISSION_PIN)
        cls.context=cls.scientific.read('source/context.json'); cls.design=cls.scientific.read('source/plan.json')
        cls.affinities=cls.admission.read('preview-batches.json')['damageSourceAffinities']['affinities']
        cls.pair=cls.scientific.read('study/pair-01.json'); cls.plan=cls.scientific.read('search/root-01/candidate/racing/plan.json')
        cls.endpoint=cls.scientific.read('result.json')['roots'][0]
        cls.heldout={cls.endpoint[r+'Party']:cls.endpoint[r+'Wins'] for r in ('control','candidate','benchmark')}
        cls.benchmark=next(s['party'] for s in cls.context['scope']['starts'] if s['party']['id']==a.kernel.BENCHMARK)
        cls.selected=next(p for b in cls.pair['candidate']['batches'] for p in b['proposals'] if p['party'] and p['party']['id']==cls.endpoint['candidateParty'])
        cls.review=a.analyze(cls.scientific,cls.admission)

    def arm(self,report=None,plan=None):
        return a.creation_arm(report or self.pair['candidate'],plan or self.plan,self.context,self.design['candidate'],
            self.affinities,self.heldout,self.endpoint['candidateParty'])

    def test_all_roots_and_both_versions_are_recounted(self):
        self.assertEqual('Inconclusive',self.review['originalDecision']); self.assertEqual(19840,self.review['sourceFights'])
        self.assertEqual(list(range(1,13)),[p['root'] for p in self.review['pairs']])
        self.assertEqual(204,self.review['summary']['changedPositions'])
        for role in ('control','candidate'):
            self.assertEqual(204,self.review['summary'][role+'Accepted'])
            self.assertEqual(12*528,sum(p[role]['referenceFights']+p[role]['challengerFights'] for p in self.review['pairs']))

    def test_exact_root_one_creation_and_stage_path(self):
        trace=self.review['pairs'][0]['candidate']['selectionTrace']; record=trace['selectedProposal']
        self.assertEqual([dict(owner=6,removed=['essence.bark_golem'],added=['essence.viper'])],record['edits'])
        self.assertEqual((2,7,1),(record['wave'],record['position'],record['replacementDistance']))
        self.assertEqual((8,8,31),tuple(record['stages'][s]['wins'] for s in ('wave-2-screen','wave-2-continuation','selection')))
        self.assertEqual((5,9),(trace['marginOverBenchmark'],trace['heldoutNetWins']))
        self.assertEqual(3,len(record['affinityCreation']['newlyActivatedAffinityIds']))

    def test_unknown_same_root_outcomes_are_not_borrowed_from_another_root(self):
        measured=self.review['pairs'][7]['candidate']['selectionTrace']['selected']
        unknown=next(r for r in self.review['pairs'][5]['candidate']['records'] if r['id']==measured)
        self.assertTrue(unknown['nominated']); self.assertIsNone(unknown['heldoutWins']); self.assertIsNone(unknown['heldoutSamples'])
        self.assertEqual(195,self.review['pairs'][7]['candidate']['selectionTrace']['heldoutWins'])
        self.assertEqual(197,self.review['pairs'][10]['candidate']['selectionTrace']['heldoutWins'])

    def test_large_stable_training_lead_does_not_impute_a_heldout_win(self):
        trace=self.review['pairs'][2]['candidate']['selectionTrace']
        self.assertEqual((8,False,0,-9),(trace['marginOverBenchmark'],trace['halvesDisagree'],trace['leaveOneOutChanges'],trace['heldoutNetWins']))

    def test_primary_tie_explains_root_two_without_changing_the_policy(self):
        for role in ('control','candidate'):
            trace=self.review['pairs'][1][role]['selectionTrace']
            self.assertEqual('PrimaryPositiveTie',trace['reason']); self.assertEqual(a.kernel.PRIMARY,trace['selected'])
            self.assertEqual(29,trace['wins'][a.kernel.PRIMARY]); self.assertEqual(29,trace['wins'][a.kernel.BENCHMARK])
            self.assertEqual(-29,trace['heldoutNetWins'])

    def test_benchmark_loss_decomposition_retains_all_roots(self):
        rows=self.review['benchmarkContributions']['candidate']
        self.assertEqual([2,7,9,12],rows['other-reference']['roots'])
        self.assertEqual(-40,rows['other-reference']['netWins']); self.assertEqual(-1,rows['novel']['netWins'])
        self.assertEqual(-41,sum(r['netWins'] for r in rows.values()))
        self.assertEqual(list(range(1,13)),sorted(n for r in rows.values() for n in r['roots']))

    def test_mixed_policy_or_racing_versions_rejected(self):
        for target in ('report','evaluation','plan','policy'):
            report=copy.deepcopy(self.pair['candidate']); plan=copy.deepcopy(self.plan)
            if target=='report': report['version']='tower-proposal-racing-v2'
            elif target=='evaluation': report['evaluation']['version']='tower-proposal-racing-v2'
            elif target=='plan': plan['version']='tower-proposal-racing-v2'
            else: plan['policy']['name']='renamed'
            with self.subTest(target=target),self.assertRaises(ValueError): self.arm(report,plan)

    def test_authored_route_must_match_target_pair_and_all_new_activations(self):
        for kind in ('target','activation'):
            p=copy.deepcopy(self.selected)
            if kind=='target': p['affinityCreation']['targetAffinityIds']=p['affinityCreation']['newlyActivatedAffinityIds'][:1]
            else: p['affinityCreation']['newlyActivatedAffinityIds']=p['affinityCreation']['targetAffinityIds']
            with self.subTest(kind=kind),self.assertRaisesRegex(ValueError,'authored route'):
                a.route_step(p,self.benchmark,self.design['candidate'],self.affinities)

    def test_creation_locality_and_minimal_edits_cannot_be_relabelled(self):
        for kind in ('owner','distance','removed','fallback'):
            p=copy.deepcopy(self.selected)
            if kind=='owner': p['changedOwners']=[5]
            elif kind=='distance': p['replacementDistance']=2
            elif kind=='removed': p['affinityCreation']['removed']=[]
            else: p['fallback']='single'
            with self.subTest(kind=kind),self.assertRaises(ValueError): a.route_step(p,self.benchmark,self.design['candidate'],self.affinities)

    def test_unknown_authored_affinity_fails(self):
        policy=copy.deepcopy(self.design['candidate']); policy['createdDamageAffinityIds'].append('missing')
        with self.assertRaises(ValueError): a.route_step(self.selected,self.benchmark,policy,self.affinities)

    def test_changed_duplicate_rejection_or_parent_fails(self):
        for kind in ('rejection','parent','operator'):
            report=copy.deepcopy(self.pair['candidate']); p=next(p for p in report['batches'][0]['proposals'] if p['party'] and p['rejection'] is None)
            if kind=='rejection': p['rejection']='duplicate-recipe'
            elif kind=='parent': p['parents']=[a.kernel.PRIMARY]
            else: p['requestedOperator']='single'
            with self.subTest(kind=kind),self.assertRaises(ValueError): self.arm(report)

    def test_scores_observations_nomination_and_recipes_are_recounted(self):
        for kind in ('score','observation','nominees','common','seed','recipe'):
            report=copy.deepcopy(self.pair['candidate']); e=report['evaluation']; p=e['panels'][0]
            if kind=='score': p['scores'][0]['wins']+=1
            elif kind=='observation': p['observations'].pop()
            elif kind=='nominees': e['nominees'].reverse()
            elif kind=='common': e['decisions'][1]['commonScores'][0]['wins']+=1
            elif kind=='seed': p['observations'][0]['outcome']['seed']+=1
            else: p['freeze']['parties'][0]['builds']['1'][0]='invented'
            with self.subTest(kind=kind),self.assertRaises(ValueError): self.arm(report)

    def test_benchmark_cannot_silently_replace_the_saved_primary_reference(self):
        plan=copy.deepcopy(self.plan); plan['racing']['scope']['stages']['selectionPrimaryReferenceId']=self.context['benchmarkReferenceId']
        with self.assertRaisesRegex(ValueError,'references'): self.arm(plan=plan)

    def test_shared_training_observations_must_agree(self):
        report=copy.deepcopy(self.pair['candidate']); report['evaluation']['panels'][0]['observations'][0]['outcome']['guardianHealth']+=1
        with self.assertRaisesRegex(ValueError,'Shared recipe outcomes'): a.prior.shared_outcomes(self.pair['control'],report)

    def test_consumed_bytes_and_external_pin_are_required(self):
        with tempfile.TemporaryDirectory(prefix='creation-stage-test-') as folder:
            root=Path(folder); raw=b'{"complete":true}'
            (root/'row.json').write_bytes(raw); (root/'files.json').write_text(__import__('json').dumps({'row.json':a.sha(raw)}))
            pin=a.sha((root/'files.json').read_bytes()); evidence=a.Evidence(root,pin)
            self.assertTrue(evidence.read('row.json')['complete']); (root/'row.json').write_text('{}')
            with self.assertRaises(ValueError): evidence.recheck()
            with self.assertRaises(ValueError): a.Evidence(root,'0'*64)
            with self.assertRaises(ValueError): evidence.read('../row.json')


if __name__ == '__main__': unittest.main(verbosity=2)
