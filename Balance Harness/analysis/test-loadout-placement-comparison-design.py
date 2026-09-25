"""Independent allocation arithmetic and prospective decision-boundary checks."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('design', Path(__file__).with_name('loadout-placement-comparison-design.py'))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


class DesignTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.evidence = m.load_evidence()
        cls.design = m.build_design(cls.evidence)

    def test_authenticated_preview_and_reference(self):
        self.assertEqual(self.design['plannedNativePlan']['control'], self.evidence['oldPlan']['candidate'])
        self.assertEqual(self.design['feasibility']['candidateRecipes'], 238)
        self.assertEqual(self.design['feasibility']['controlRecipes'], 26)
        self.assertEqual(len(self.design['physicalBinding']['referencePartyIds']), 3)
        self.assertEqual(self.design['sourceFiles'][m.PRIOR], m.PRIOR_PIN)

    def test_allocation_is_disjoint_complete_and_ordered(self):
        positions = []
        for i, row in enumerate(self.design['allocation']['slots']):
            ranges = [row['proposalRoot'], *row['racing'], row['nomination'], row['validation']]
            self.assertEqual([b-a for a,b in ranges], [1,8,8,8,8,16,60])
            search = [v for a,b in ranges for v in range(a,b)]
            self.assertEqual(search, list(range(109*i,109*(i+1))))
            heldout = list(range(*row['heldout']))
            self.assertEqual(len(heldout), 256)
            self.assertEqual(row['root'], i+1)
            positions += search + heldout
        self.assertEqual(sorted(positions), list(range(4380)))
        self.assertEqual(len(set(positions)), 4380)

    def test_matched_fights_and_resource_partitions(self):
        p, e, r = [self.design[k] for k in ['plannedNativePlan','evaluation','resources']]
        self.assertEqual(e['searchFightsTotal'], 12*2*528)
        self.assertEqual(e['heldoutFightsMinimum'], 12*256)
        self.assertEqual(p['maximumFights'], 12*(2*528+3*256))
        self.assertEqual(p['requiredFreshValues'], 12*(1+32+16+60+256))
        self.assertEqual(r['scientificMaximumSeconds'], r['nativeMaximumSeconds']+r['auditPublicationMaximumSeconds'])
        self.assertEqual(r['scientificMaximumBytes'], r['nativeMaximumBytes']+r['auditPublicationMaximumBytes'])
        self.assertEqual((r['margin'],r['publicationReserveSeconds']), (2,120))
        for k,v in r['inheritedForecastFloors'].items():
            self.assertGreaterEqual(v,self.evidence['forecast'][k])

    def test_selector_and_existing_budget_are_unchanged(self):
        p, old = self.design['plannedNativePlan'], self.evidence['oldPlan']
        changed = {k for k in p if p[k] != old[k]}
        self.assertEqual(changed, {'version','scopeHash','control','candidate','analysis'})
        changed_analysis = {k for k in p['analysis'] if p['analysis'][k] != old['analysis'][k]}
        self.assertEqual(changed_analysis, {'goBenchmarkAtLeast','goPromisingNovelRootsAtLeast','decisionOrder'})
        self.assertEqual(p['validationProtocol']['validationValues'],60)

    def test_distinct_inventory_contract_and_new_version(self):
        runtime=self.design['runtime']
        self.assertEqual((runtime['controlRacingVersion'],runtime['candidateRacingVersion']),
                         ('tower-proposal-racing-v7','tower-proposal-racing-v8'))
        self.assertIn('MUST carry null',runtime['inventoryBinding'])
        self.assertFalse(runtime['comparisonImplemented'])
        self.assertFalse(runtime['priorAdmissionReusable'])
        self.assertEqual(self.design['plannedNativePlan']['version'],m.COMPARISON)

    def test_native_scope_bridge_matches_old_golden_and_new_frozen_label(self):
        self.assertEqual(m.native_scope_hash(self.evidence['oldRequest']['context']['scope']),
                         self.evidence['oldPlan']['scopeHash'])
        self.assertEqual(self.design['physicalBinding']['nativeScopeHash'],
                         '8cd7fdb0049b1e05320ae7921e1895703272a9951e5f2d171006eb458bd68b86')

    def test_changed_physical_context_rejected(self):
        e=copy.deepcopy(self.evidence)
        e['preview']['input-01.json']['context']['scope']['requiredPartySize']=9
        with self.assertRaisesRegex(ValueError,'Physical context'):
            m.build_design(e)

    def test_changed_candidate_or_control_rejected(self):
        for version in ['tower-proposal-policy-v5','tower-proposal-policy-v6']:
            e=copy.deepcopy(self.evidence)
            next(p for p in e['preview']['input-01.json']['policies'] if p['version']==version)['firstWave'].pop()
            with self.assertRaisesRegex(ValueError,'Changed'):
                m.build_design(e)

    def test_changed_verified_preview_rejected(self):
        e=copy.deepcopy(self.evidence);e['preview']['summary.json']['completeRoots']=11
        with self.assertRaisesRegex(ValueError,'Incomplete preview'):
            m.build_design(e)

    def test_every_scientific_boundary_is_frozen(self):
        paths=[('plannedNativePlan','roots'),('plannedNativePlan','heldoutSamples'),
               ('plannedNativePlan','maximumFights'),('allocation','entropyBytes'),
               ('evaluation','barrier'),('evaluation','gate'),('reporting','novelty'),
               ('resources','margin'),('resources','nativeMaximumSeconds'),('resources','auditPublicationMaximumSeconds'),
               ('runtime','candidateRacingVersion'),('physicalBinding','nativeScopeHash')]
        for outer,inner in paths:
            with self.subTest(field=(outer,inner)):
                d=copy.deepcopy(self.design);d[outer][inner]='changed'
                with self.assertRaisesRegex(ValueError,'Changed frozen design'):
                    m.verify_design(d,self.evidence)

    def test_relative_only_success_does_not_pass(self):
        self.assertEqual(m.decide(100,0,12,0),'Inconclusive')
        self.assertEqual(m.decide(100,61,12,3),'Inconclusive')
        self.assertEqual(m.decide(61,100,12,3),'Inconclusive')
        self.assertEqual(m.decide(62,62,3,3),'LargerFreshEvaluationWarranted')

    def test_novelty_and_differing_root_boundaries(self):
        self.assertEqual(m.decide(62,62,3,2),'Inconclusive')
        self.assertEqual(m.decide(62,62,2,3),'Inconclusive')
        self.assertEqual(m.decide(0,0,0,0),'NoObservedOutputDifferentiation')
        self.assertLess(7/256,.03);self.assertGreaterEqual(8/256,.03)

    def test_either_harm_endpoint_abandons_without_novelty_override(self):
        for method,benchmark in [(-62,100),(100,-62),(-62,-62)]:
            self.assertEqual(m.decide(method,benchmark,12,12),'AbandonThisConfiguration')
        self.assertEqual(m.decide(-61,-61,12,0),'Inconclusive')

    def test_invalid_study_precedes_all_other_decisions(self):
        self.assertEqual(m.decide(100,100,12,12,False),'InvalidStudy')
        self.assertEqual(m.decide(-100,-100,12,12,False),'InvalidStudy')
        with self.assertRaisesRegex(ValueError,'Invalid decision'):
            m.decide(float('nan'),0,12,0)

    def test_no_fresh_values_or_admission_claim(self):
        d=self.design
        self.assertEqual(d['status'],'FrozenProspectiveComparisonNotImplementedNotAdmitted')
        self.assertTrue(all(d[k]==0 for k in ['newFights','newReservedValues','newEntropyDraws']))
        self.assertEqual(d['allocation']['actualValuesAllocated'],0)
        self.assertFalse(d['productionDefaultsChanged'])
        self.assertIn('including unused tail',d['allocation']['permanentExclusion'])

    def test_evidence_tampering_rejected_at_external_pin(self):
        original=m.sha
        with patch.object(m,'sha',side_effect=lambda p:'0'*64 if p.as_posix().endswith(m.PRIOR) else original(p)):
            with self.assertRaisesRegex(ValueError,'Changed source'):
                m.load_evidence()

    def test_strict_json_and_exclusive_publication(self):
        with tempfile.TemporaryDirectory() as folder:
            p=Path(folder)/'input.json'
            for text in ['{"a":1,"a":2}','{"a":NaN}','{"a":Infinity}']:
                p.write_text(text,encoding='utf-8')
                with self.assertRaises(ValueError):m.read(p)
            with self.assertRaises(FileExistsError):m.save(p,{})

    def test_build_does_not_mutate_evidence(self):
        before=m.digest(self.evidence)
        self.assertEqual(m.build_design(self.evidence),self.design)
        self.assertEqual(before,m.digest(self.evidence))


if __name__=='__main__':
    unittest.main(verbosity=2)
