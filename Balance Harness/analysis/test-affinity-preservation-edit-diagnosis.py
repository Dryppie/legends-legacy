"""Paired edit joins with published literal outcomes; no combat or entropy."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('paired_edits',Path(__file__).with_name('affinity-preservation-edit-diagnosis.py'))
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)


class PairedEditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        evidence={k:m.Evidence(*m.PACKAGES[k]) for k in ('catalogue','generation','admission','recognition','runtime')}
        cls.catalogue=evidence['catalogue'].read('catalogue.json')
        cls.plan=json.loads((ROOT/m.PLAN).read_text())
        cls.sources=[({a:evidence['generation'].read(c['sourcePlans'][a]) for a in m.ARMS},
            evidence['generation'].read(c['sourceReport'])) for c in cls.catalogue['roots']]
        cls.affinity=evidence['admission'].read('preview-batches.json')['damageSourceAffinities']
        evidence['recognition'].bytes('analysis.py')
        reviewer=m.module('paired_edits_fixture_reviewer',evidence['recognition'].root/'analysis.py')
        auditor=reviewer.load_auditor(evidence['runtime'].root/'auditor.py')
        # This is the sealed engineering fixture, never the real cohort's outcomes.
        fixture=m.Evidence('TestResults/tower-preservation-recognition-owned-fixture-20260924-02/result',
            '52ba8e85cf1b3a01f143cade4559f49b908ff7cae5f07546817c051f0e6e18c1')
        result,study=fixture.read('result.json'),fixture.read('study/study.json')
        fixture.bytes('study/files.json')
        endpoint=reviewer.published_endpoint(result,study,fixture.consumed['study/files.json'],auditor)
        cls.recognition=reviewer.review(endpoint,cls.plan,cls.catalogue,study['evidence'],fixture.read('confirmation-binding.json')['panel'],auditor)
        cls.joined=m.diagnose(cls.catalogue,cls.plan,cls.recognition,cls.sources,cls.affinity)

    def evaluate(self,catalogue=None,plan=None,recognition=None,sources=None):
        return m.diagnose(catalogue or self.catalogue,plan or self.plan,recognition or self.recognition,sources or self.sources,self.affinity)

    def test_complete_physical_and_arm_families(self):
        r=self.joined
        self.assertEqual((r['physicalCells'],r['physicalMeasured'],r['physicalUnknown'],r['armOccurrences']),(285,109,176,408))
        self.assertEqual([len(r['armRows'][a]) for a in m.ARMS],[204,204])
        self.assertEqual([r['summary'][a]['measuredOccurrences'] for a in m.ARMS],[78,79])
        self.assertEqual(len({(c['root'],c['partyId']) for c in r['cells']}),285)

    def test_shared_cells_retain_same_outcome_and_distinct_provenance(self):
        shared=[c for c in self.joined['cells'] if c['membership']=='shared']
        self.assertEqual(len(shared),123)
        self.assertTrue(any(c['provenance']['control']!=c['provenance']['candidate'] for c in shared))
        for c in shared:
            self.assertEqual(c['edits']['control']['removed'],c['edits']['candidate']['removed'])
            self.assertEqual(c['edits']['control']['mechanicsFit'],c['edits']['candidate']['mechanicsFit'])

    def test_weights_reconcile_each_arm_and_partition(self):
        for arm in m.ARMS:
            s=self.joined['summary'][arm]
            self.assertAlmostEqual(s['weightedContributionToArmMean'],self.recognition['allRoots'][arm]['mean'])
            parts=self.joined['groups'][arm]['membership'].values()
            self.assertAlmostEqual(sum(x['weightedContributionToArmMean'] or 0 for x in parts),s['weightedContributionToArmMean'])

    def test_preservation_rule_reconstructed(self):
        self.assertTrue(any(r['removedCompletableEndpoints'] for r in self.joined['armRows']['control']))
        self.assertFalse(any(r['removedCompletableEndpoints'] for r in self.joined['armRows']['candidate']))
        self.assertTrue(all(r['removalSelection'] for r in self.joined['armRows']['candidate']))

    def test_unknown_only_group_stays_unknown(self):
        row=next(r for r in self.joined['armRows']['candidate'] if r['independentOutcome'] is None)
        summary=m.summarize([row])
        self.assertIsNone(summary['estimatedGroupMean'])
        self.assertIsNone(summary['weightedContributionToArmMean'])

    def test_no_unweighted_pooled_mean(self):
        for arm in m.ARMS:
            self.assertNotIn('measuredMeanGain',self.joined['summary'][arm])

    def test_overlapping_removed_groups_explicit(self):
        for arm in m.ARMS:
            self.assertGreater(sum(g['populationOccurrences'] for g in self.joined['groups'][arm]['removedEssence'].values()),204)

    def reject_source(self,change,arm='candidate'):
        plans,pair=copy.deepcopy(self.sources[0]);change(plans,pair)
        with self.assertRaises(ValueError):m.source_arm(self.catalogue['roots'][0],plans[arm],pair,arm,self.affinity)

    def test_wrong_source_plan_rejected(self):
        self.reject_source(lambda plans,pair:plans['candidate']['policy'].update(name='changed'))

    def test_missing_accepted_candidate_rejected(self):
        self.reject_source(lambda plans,pair:pair['candidate']['batches'][0]['candidates'].pop())

    def test_wrong_removal_rejected(self):
        self.reject_source(lambda plans,pair:next(p for p in pair['candidate']['batches'][0]['proposals'] if p['rejection'] is None)['affinityCreation'].update(removed=['essence.viper']))

    def test_wrong_protected_endpoints_rejected(self):
        self.reject_source(lambda plans,pair:next(p for p in pair['candidate']['batches'][0]['proposals'] if p['rejection'] is None)['affinityCreation']['removalSelection'].update(protectedEssences=[]))

    def test_wrong_eligibility_count_rejected(self):
        self.reject_source(lambda plans,pair:next(p for p in pair['candidate']['batches'][0]['proposals'] if p['rejection'] is None)['affinityCreation']['removalSelection'].update(eligibleEdits=999))

    def test_old_scores_not_used(self):
        plans,pair=copy.deepcopy(self.sources[0])
        original=m.source_arm(self.catalogue['roots'][0],plans['candidate'],pair,'candidate',self.affinity)
        pair['candidate']['evaluation']['panels']=[]
        pair['candidate']['evaluation']['validationDecision']={'invented':True}
        pair['candidate']['evaluation']['decisions'][-1]['commonScores']=[]
        self.assertEqual(original,m.source_arm(self.catalogue['roots'][0],plans['candidate'],pair,'candidate',self.affinity))

    def test_changed_provenance_rejected(self):
        c=copy.deepcopy(self.catalogue)
        c['roots'][0]['teams'][3]['provenance']['candidate']['acceptedPosition']+=1
        with self.assertRaises(ValueError):self.evaluate(catalogue=c)

    def test_changed_owner_context_rejected(self):
        c=copy.deepcopy(self.catalogue)
        c['roots'][0]['teams'][3]['scenario']['party'][0]['build']['characterLevel']+=1
        with self.assertRaises(ValueError):self.evaluate(catalogue=c)

    def test_wrong_inclusion_probability_rejected(self):
        p=copy.deepcopy(self.plan)
        next(t for t in p['roots'][0]['teams'] if t['stratum']=='remaining-shared')['inclusionProbability']={'numerator':1,'denominator':1}
        with self.assertRaises(ValueError):self.evaluate(plan=p)

    def test_cross_root_join_rejected(self):
        r=copy.deepcopy(self.recognition);r['measured'][0]['root']=13
        with self.assertRaises(ValueError):self.evaluate(recognition=r)

    def test_duplicate_measurement_rejected(self):
        r=copy.deepcopy(self.recognition);r['measured'][1]=r['measured'][0]
        with self.assertRaises(ValueError):self.evaluate(recognition=r)

    def test_missing_measurement_rejected(self):
        r=copy.deepcopy(self.recognition);r['measured'].pop()
        with self.assertRaises(ValueError):self.evaluate(recognition=r)

    def test_nonfinite_measurement_rejected(self):
        r=copy.deepcopy(self.recognition);r['measured'][0]['observedGain']=float('nan')
        with self.assertRaises(ValueError):self.evaluate(recognition=r)

    def test_imputed_unknown_rejected(self):
        p=copy.deepcopy(self.plan);p['roots'][0]['unmeasured'][0]['independentOutcome']=0
        with self.assertRaises(ValueError):self.evaluate(plan=p)

    def test_structural_features_include_fixed_equipment_and_provider_counts(self):
        for row in self.joined['armRows']['candidate']:
            fit=row['mechanicsFit']
            self.assertTrue(fit['mainHand'])
            self.assertTrue(all(0<=c['before']<=5 and 0<=c['after']<=5 for c in fit['subgroupProviderChanges'].values()))
            self.assertIn('NoRoleCompatibilityScore',fit['interpretation'])
            self.assertTrue(set(fit['ownerSignalsLost'])<=set(fit['removedSignals']))

    def test_mechanics_require_every_owner_essence(self):
        row=self.joined['armRows']['candidate'][0]
        cat=self.catalogue['roots'][0]
        team=next(t for t in cat['teams'] if t['partyId']==row['partyId'])
        inventory=copy.deepcopy(self.sources[0][0]['candidate']['damageAffinityInventory'])
        inventory['essences']=[]
        with self.assertRaises(ValueError):m.mechanics_fit(row,self.sources[0][0]['candidate']['racing']['scope']['starts'][2]['party'],team,inventory)

    def test_mechanics_snapshot_covers_all_edits(self):
        rows=self.joined['armRows']['control']+self.joined['armRows']['candidate']
        snapshot=m.base.mechanics({'rows':rows},self.sources[0][0]['control']['damageAffinityInventory'])
        self.assertEqual({e['id'] for e in snapshot['essences']},{e for r in rows for e in r['added']+r['removed']})

    def test_evidence_mutation_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            p=Path(folder);(p/'item.json').write_bytes(b'{"x":1}')
            raw=json.dumps({'item.json':m.sha((p/'item.json').read_bytes())}).encode();(p/'files.json').write_bytes(raw)
            e=m.Evidence(str(p),m.sha(raw));e.read('item.json')
            (p/'item.json').write_bytes(b'{}')
            with self.assertRaises(ValueError):e.recheck()
            with self.assertRaises(ValueError):e.read('item.json')


if __name__=='__main__':
    unittest.main()
