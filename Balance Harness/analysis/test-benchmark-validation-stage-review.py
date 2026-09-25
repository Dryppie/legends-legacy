"""Saved v5 panel, challenger, gate, missing-evidence and tamper checks."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('validation_review', Path(__file__).with_name('benchmark-validation-stage-review.py'))
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)


class ValidationStageReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source, cls.admission = a.Evidence(a.RUN, a.RUN_PIN), a.Evidence(a.ADMISSION, a.ADMISSION_PIN)
        context, design = cls.source.read('source/context.json'), cls.source.read('source/plan.json')
        affinities = cls.admission.read('preview-batches.json')['damageSourceAffinities']['affinities']
        endpoints, families = cls.source.read('result.json')['roots'], cls.source.read('study/freeze.json')['families']
        cls.fixtures = {}
        for n in (1,4):
            pair = cls.source.read(f'study/pair-{n:02d}.json')
            cp, plan = [cls.source.read(f'search/root-{n:02d}/{role}/racing/plan.json') for role in ('control','candidate')]
            endpoint = endpoints[n-1]
            heldout = {endpoint[r+'Party']: endpoint[r+'Wins'] for r in ('control','candidate','benchmark')}
            reviewed = a.creation.creation_arm(pair['control'], cp, context, design['control'], affinities,
                heldout, endpoint['controlParty'], report_version='tower-proposal-racing-v4', benchmark_ties=True)
            cls.fixtures[n] = dict(control=pair['control'], report=pair['candidate'], control_plan=cp, plan=plan,
                reviewed_control=reviewed, heldout=heldout, selected=endpoint['candidateParty'], heldout_seeds=families[n-1]['seeds'])

    def review(self, n=1, **overrides):
        return a.candidate_review(**dict(self.fixtures[n], **overrides))

    def test_saved_rejected_challenger_has_no_invented_heldout(self):
        r = self.review()
        self.assertEqual(('novel', False, 3, 12), (r['challengerCategory'], r['validationDecision']['passed'],
            r['validation']['gainedWins'], r['validation']['lostWins']))
        self.assertEqual(a.kernel.BENCHMARK, r['selected'])
        self.assertIsNone(r['challengerHeldoutWins'])
        self.assertEqual(17, len(r['records']))
        self.assertEqual(2, sum(x['nominated'] for x in r['records']))
        self.assertEqual(1, sum(x['validationChallenger'] for x in r['records']))
        self.assertTrue(all(x['heldoutWins'] is None for x in r['records']))
        self.assertTrue(all('selection' not in x['stages'] for x in r['records']))

    def test_saved_gate_pass_is_existing_reference_with_observed_reversal(self):
        r = self.review(4)
        self.assertEqual((a.kernel.PRIMARY, 'other-reference', True, 17, 6, 185),
            (r['challenger'], r['selectedCategory'], r['validationDecision']['passed'],
             r['validation']['gainedWins'], r['validation']['lostWins'], r['challengerHeldoutWins']))
        self.assertEqual(198, self.fixtures[4]['heldout'][a.kernel.BENCHMARK])

    def test_v4_and_v5_contracts_are_not_interchangeable(self):
        for target, key, value in [('plan','version','tower-proposal-racing-v4'), ('report','selectionPolicyVersion',a.tie.SELECTOR),
                                   ('plan','selectionPolicyVersion',a.tie.SELECTOR)]:
            changed = copy.deepcopy(self.fixtures[1][target]); changed[key] = value
            with self.subTest(target=target,key=key), self.assertRaises(ValueError): self.review(**{target:changed})

    def test_validation_seeds_cannot_reuse_unused_control_selection_or_heldout(self):
        for seed in (self.fixtures[1]['control_plan']['racing']['panels'][4]['seeds'][20], self.fixtures[1]['heldout_seeds'][0]):
            plan = copy.deepcopy(self.fixtures[1]['plan']); plan['racing']['panels'][5]['seeds'][0] = seed
            with self.assertRaisesRegex(ValueError, 'Reused validation'): self.review(plan=plan)

    def test_nomination_is_exact_first_sixteen_control_values(self):
        plan = copy.deepcopy(self.fixtures[1]['plan']); plan['racing']['panels'][4]['seeds'].reverse()
        with self.assertRaisesRegex(ValueError, 'paired plans'): self.review(plan=plan)

    def test_shared_trajectory_rejects_changed_generation_and_training(self):
        for kind in ('generation','score','outcome','nominees'):
            report = copy.deepcopy(self.fixtures[1]['report'])
            if kind == 'generation': report['batches'][0]['proposals'][0]['scheduledOwners'] = [10]
            elif kind == 'score': report['evaluation']['panels'][0]['scores'][0]['wins'] += 1
            elif kind == 'outcome': report['evaluation']['panels'][0]['observations'][0]['outcome']['survival'] += 1
            else: report['evaluation']['nominees'].reverse()
            with self.subTest(kind=kind), self.assertRaises(ValueError): self.review(report=report)

    def test_saved_challenger_cannot_be_changed_after_nomination(self):
        report = copy.deepcopy(self.fixtures[1]['report']); report['evaluation']['validationFreeze']['challengerId'] = a.kernel.PRIMARY
        with self.assertRaisesRegex(ValueError, 'frozen challenger'): self.review(report=report)

    def test_incomplete_or_reordered_validation_is_rejected(self):
        for kind in ('complete','short','order','party','before'):
            report = copy.deepcopy(self.fixtures[1]['report']); panel = report['evaluation']['panels'][5]
            if kind == 'complete': panel['complete'] = False
            elif kind == 'short': panel['observations'].pop()
            elif kind == 'order': panel['observations'][0],panel['observations'][1] = panel['observations'][1],panel['observations'][0]
            elif kind == 'party': panel['freeze']['parties'].reverse()
            else: panel['freeze']['evaluationsBefore'] -= 1
            with self.subTest(kind=kind), self.assertRaises(ValueError): self.review(report=report)

    def test_final_scores_contrasts_and_literal_outcomes_are_recounted(self):
        for kind in ('score','contrast','enum','numeric','recipe','seed'):
            report = copy.deepcopy(self.fixtures[1]['report']); panel = report['evaluation']['panels'][5]
            if kind == 'score': panel['scores'][0]['wins'] += 1
            elif kind == 'contrast': panel['contrasts'][0]['gainedWins'] += 1
            elif kind == 'enum': panel['observations'][0]['outcome']['outcome'] = 'Unknown'
            elif kind == 'numeric': panel['observations'][0]['outcome']['survival'] = True
            elif kind == 'recipe': panel['observations'][0]['request']['scenario']['party'][0]['build']['essenceIds'] = []
            else: panel['observations'][0]['outcome']['seed'] += 1
            with self.subTest(kind=kind), self.assertRaises(ValueError): self.review(report=report)

    def test_gate_fields_and_endpoint_cannot_be_forged(self):
        for key,value in [('passed',1), ('gainedWins',True), ('lostWins',11), ('tailNumerator',1), ('selectedId',a.kernel.PRIMARY)]:
            report = copy.deepcopy(self.fixtures[1]['report']); report['evaluation']['validationDecision'][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.review(report=report)
        with self.assertRaises(ValueError): self.review(selected=a.kernel.PRIMARY)

    def test_full_sixty_pairs_use_exact_gate_boundary(self):
        frozen = dict(version=a.SELECTOR,planHash='test',nominationPanelHash='test',challengerId='challenger',benchmarkId='benchmark')
        for gains,losses,passed in [(0,0,False),(4,0,False),(5,0,True),(17,6,True),(6,17,False)]:
            outcomes = ([True]*gains+[False]*losses+[True]*(60-gains-losses),
                        [False]*gains+[True]*losses+[True]*(60-gains-losses))
            rows = [dict(request=dict(seed=i,panelHash='test'),outcome=dict(outcome='Victory' if won else 'Defeat'))
                    for values in outcomes for i,won in enumerate(values)]
            result = a.audit.validation_decision(frozen,dict(complete=True,freeze=dict(seeds=list(range(60))),observations=rows))
            with self.subTest(gains=gains,losses=losses):
                self.assertEqual((gains,losses,passed), (result['gainedWins'],result['lostWins'],result['passed']))
                self.assertEqual(60,result['samples'])

    def test_primary_positive_tie_and_zero_health_fallback(self):
        order = ['novel',a.kernel.PRIMARY,'other']
        scores = [dict(id=p,wins=1,fitness=dict(guardianHealth=10)) for p in order]
        self.assertEqual(a.kernel.PRIMARY,a.audit.select(scores,order,a.kernel.PRIMARY))
        for score in scores: score['wins'] = 0
        scores[-1]['fitness']['guardianHealth'] = 1
        self.assertEqual('other',a.audit.select(scores,order,a.kernel.PRIMARY))

    def test_missing_training_stage_stays_unknown(self):
        r = self.review()
        absent = next(x for x in r['records'] if not x['nominated'])
        self.assertNotIn('nomination', absent['stages'])
        self.assertNotIn('validation', absent['stages'])
        self.assertIsNone(a.tie.stage_contrast(self.fixtures[1]['report']['evaluation']['panels'][5],absent['id']))

    def test_consumed_input_and_external_manifest_pin_are_required(self):
        with tempfile.TemporaryDirectory(prefix='validation-stage-test-') as folder:
            root = Path(folder); raw = b'{"complete":true}'
            (root/'row.json').write_bytes(raw)
            (root/'files.json').write_text(json.dumps({'row.json':a.sha(raw)}))
            evidence = a.Evidence(root,a.sha((root/'files.json').read_bytes()))
            self.assertTrue(evidence.read('row.json')['complete'])
            (root/'row.json').write_text('{}')
            with self.assertRaises(ValueError): evidence.recheck()
            with self.assertRaises(ValueError): a.Evidence(root,'0'*64)
            with self.assertRaises(ValueError): evidence.read('../row.json')


if __name__ == '__main__':
    unittest.main(verbosity=2)
