"""Join-integrity tests using literal recognition outcomes and frozen generation."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]


def module(name,path):
    spec = importlib.util.spec_from_file_location(name,path)
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m


m = module('edit_diagnosis',Path(__file__).with_name('affinity-creation-edit-diagnosis.py'))


class EditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalogue = m.Evidence(*m.PACKAGES['catalogue'])
        generation = m.Evidence(*m.PACKAGES['generation'])
        admission = m.Evidence(*m.PACKAGES['admission'])
        recognition = m.Evidence(*m.PACKAGES['recognition'])
        recognition.bytes('analysis.py')
        reviewer = module('sealed_recognition_test',recognition.root/'analysis.py')
        cls.catalogue = catalogue.read('catalogue.json')
        cls.sources = [(generation.read(c['sourcePlan']),generation.read(c['sourceReport'])) for c in cls.catalogue['roots']]
        cls.affinity = admission.read('preview-batches.json')['damageSourceAffinities']
        fixture = m.Evidence('TestResults/tower-affinity-recognition-owned-fixture-20260924/result',
            '6c03bfe9eda04a2ce54cf6ee9712c1f107df1afd4068edb371bada5cbf3b150b')
        plan = json.loads((ROOT/m.PLAN).read_text())
        cls.recognition = reviewer.review(fixture.read('result.json'),plan,cls.catalogue)
        cls.joined = m.diagnose(cls.catalogue,cls.recognition,cls.sources,cls.affinity)
        cls.plan,cls.pair = cls.sources[0]
        cls.parent = cls.plan['racing']['scope']['starts'][2]['party']
        cls.proposal = cls.pair['candidate']['batches'][0]['proposals'][0]
        cls.team = next(t for t in cls.catalogue['roots'][0]['teams'] if t['partyId']==cls.proposal['party']['id'])
        cls.selected = m.selected_affinities(cls.affinity,cls.plan)

    def test_all_instances_and_unknowns_retained(self):
        r = self.joined
        self.assertEqual((r['summary']['catalogueOccurrences'],r['summary']['measuredOccurrences'],r['summary']['unmeasuredOccurrences']),(204,72,132))
        self.assertEqual(r['summary']['distinctRecipes'],57)
        self.assertEqual(r['summary']['distinctMeasuredRecipes'],45)
        self.assertEqual(len({(x['root'],x['partyId']) for x in r['rows']}),204)
        self.assertEqual(sum(x['independentOutcome'] is None for x in r['rows']),132)
        self.assertEqual(sum(r['rejectedAttempts'].values()),48)
        self.assertFalse(r['policyDefaultsChanged'])

    def test_subgroup_boundary_and_distances(self):
        rows = self.joined['rows']
        self.assertTrue(all(r['subgroup']==1 for r in rows if r['owner']==5))
        self.assertTrue(all(r['subgroup']==2 for r in rows if r['owner']==6))
        self.assertEqual({r['replacementDistance'] for r in rows},{1,2})
        for r in rows:
            self.assertEqual(len(r['removed']),r['replacementDistance'])
            self.assertEqual(len(r['added']),r['replacementDistance'])

    def reject_edit(self,change):
        p = copy.deepcopy(self.proposal); change(p)
        with self.assertRaises(ValueError): m.edit_metadata(p,self.parent,self.team,self.selected)

    def test_wrong_parent_rejected(self):
        self.reject_edit(lambda p:p.update(parents=['0'*64]))

    def test_wrong_owner_rejected(self):
        self.reject_edit(lambda p:p.update(changedOwners=[1]))

    def test_wrong_removal_rejected(self):
        self.reject_edit(lambda p:p['affinityCreation'].update(removed=['essence.viper']))

    def test_wrong_addition_rejected(self):
        self.reject_edit(lambda p:p['affinityCreation'].update(added=['essence.pack_howler']))

    def test_wrong_distance_rejected(self):
        self.reject_edit(lambda p:p.update(replacementDistance=2))

    def test_wrong_party_id_rejected(self):
        self.reject_edit(lambda p:p['party'].update(id='0'*64))

    def test_false_activation_rejected(self):
        self.reject_edit(lambda p:p['affinityCreation'].update(newlyActivatedAffinityIds=[]))

    def test_false_target_rejected(self):
        self.reject_edit(lambda p:p['affinityCreation'].update(targetAffinityIds=[]))

    def test_wrong_inventory_rejected(self):
        a = copy.deepcopy(self.affinity); a['inventoryHash']='0'*64
        with self.assertRaises(ValueError): m.selected_affinities(a,self.plan)

    def test_changed_physical_context_rejected(self):
        c = copy.deepcopy(self.catalogue['roots'][0]); c['teams'][3]['scenario']['party'][0]['build']['characterLevel']=41
        with self.assertRaises(ValueError): m.source_root(c,self.plan,self.pair,self.affinity)

    def test_changed_plan_rejected(self):
        p = copy.deepcopy(self.plan); p['policy']['name']='changed'
        with self.assertRaises(ValueError): m.source_root(self.catalogue['roots'][0],p,self.pair,self.affinity)

    def test_missing_source_candidate_rejected(self):
        p = copy.deepcopy(self.pair); p['candidate']['batches'][0]['candidates'].pop()
        with self.assertRaises(ValueError): m.source_root(self.catalogue['roots'][0],self.plan,p,self.affinity)

    def test_changed_training_stratum_rejected(self):
        c = copy.deepcopy(self.catalogue['roots'][0]); c['teams'][3]['stratum']='near-miss'
        with self.assertRaises(ValueError): m.source_root(c,self.plan,self.pair,self.affinity)

    def test_cross_root_join_rejected(self):
        r = copy.deepcopy(self.recognition); r['measured'][0]['root']=13
        with self.assertRaises(ValueError): m.diagnose(self.catalogue,r,self.sources,self.affinity)

    def test_duplicate_measurement_rejected(self):
        r = copy.deepcopy(self.recognition); r['measured'][1]=r['measured'][0]
        with self.assertRaises(ValueError): m.diagnose(self.catalogue,r,self.sources,self.affinity)

    def test_nonfinite_contrast_rejected(self):
        r = copy.deepcopy(self.recognition); r['measured'][0]['observedGain']=float('nan')
        with self.assertRaises(ValueError): m.diagnose(self.catalogue,r,self.sources,self.affinity)

    def test_old_scores_and_final_selection_do_not_enter_join(self):
        pair = copy.deepcopy(self.pair)
        e = pair['candidate']['evaluation']
        e['rawSelectedId']='not-consumed'; e['validationDecision']={'fabricated':True}
        e['panels']=[]
        self.assertEqual(m.source_root(self.catalogue['roots'][0],self.plan,pair,self.affinity),
            m.source_root(self.catalogue['roots'][0],self.plan,self.pair,self.affinity))

    def test_unknown_only_group_has_no_fabricated_mean(self):
        row = next(r for r in self.joined['rows'] if r['independentOutcome'] is None)
        s = m.summary([row]); self.assertIsNone(s['measuredMeanGain']); self.assertEqual(s['measuredOccurrences'],0)

    def test_two_slot_groups_overlap_without_changing_overall_count(self):
        g = self.joined['groups']
        self.assertEqual(sum(s['catalogueOccurrences'] for s in g['removedEssence'].values()),232)
        self.assertEqual(sum(s['catalogueOccurrences'] for s in g['replacementDistance'].values()),204)

    def test_mechanics_cover_every_edit(self):
        mechanics = m.mechanics(self.joined,self.plan['damageAffinityInventory'])
        ids = {e for r in self.joined['rows'] for e in r['removed']+r['added']}
        self.assertEqual({e['id'] for e in mechanics['essences']},ids)
        self.assertTrue(mechanics['nodes'])

    def test_manifest_or_member_mutation_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            p = Path(temp); raw=b'{"value":1}'
            (p/'value.json').write_bytes(raw)
            manifest=json.dumps({'value.json':m.sha(raw)}).encode(); (p/'files.json').write_bytes(manifest)
            e=m.Evidence(str(p),m.sha(manifest)); self.assertEqual(e.read('value.json'),{'value':1})
            (p/'value.json').write_bytes(b'{}')
            with self.assertRaises(ValueError): e.read('value.json')
            with self.assertRaises(ValueError): e.recheck()
            with self.assertRaises(ValueError): m.Evidence(str(p),'0'*64)


if __name__=='__main__':
    unittest.main()
