"""Independent placement contract and adversarial archive tests; no combat or entropy."""
import argparse
import copy
import importlib.util
from pathlib import Path
import struct
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('study_tests', ROOT/'build/test-proposal-affinity-study.py')
tests = importlib.util.module_from_spec(spec); spec.loader.exec_module(tests)
audit, owner = tests.audit, tests.owner


class PlacementTests(unittest.TestCase):
    def test_frozen_design_exact_policy_and_analysis(self):
        plan = audit.read(ROOT/'Balance Harness/Tower-Loadout-Placement-Comparison-Design.json')['plannedNativePlan']
        audit.validate_policies(plan)
        for arm, field, value in [('candidate','createdDamageAffinityIds',[]), ('candidate','preservedDamageAffinityIds',[]),
                                  ('control','creationRemovalRule',audit.REMOVAL_RULE), ('candidate','firstWave',['single']*9)]:
            with self.subTest(arm=arm, field=field):
                changed = copy.deepcopy(plan); changed[arm][field] = value
                with self.assertRaises(ValueError): audit.validate_policies(changed)
        self.assertEqual(audit.PLACEMENT_DECISION, plan['analysis']['decisionOrder'])

    def test_exact_integer_decision_boundaries(self):
        cases = [(62,62,3,3,'LargerFreshEvaluationWarranted'), (61,100,3,3,'Inconclusive'),
            (100,61,3,3,'Inconclusive'), (100,0,12,0,'Inconclusive'), (62,62,2,3,'Inconclusive'),
            (62,62,3,2,'Inconclusive'), (-62,100,12,12,'AbandonThisConfiguration'),
            (100,-62,12,12,'AbandonThisConfiguration'), (-61,-61,12,0,'Inconclusive'),
            (0,0,0,0,'NoObservedOutputDifferentiation')]
        for m,b,d,n,expected in cases:
            with self.subTest(case=(m,b,d,n)):
                self.assertEqual(expected, audit.decision(m/3072,b/3072,d,n,audit.PLACEMENT_VERSION))

    def test_synthetic_exposure_keeps_unused_tail_and_collisions(self):
        words = [199999,200000,200000]+list(range(200001,216382))
        allocation = audit.classify(struct.pack('<16384i',*words),[199999],audit.PLACEMENT_VERSION)
        self.assertEqual((4380,16382,1,1),(len(allocation['selected']),len(allocation['reserved']),allocation['historicalCollisions'],allocation['duplicates']))
        self.assertEqual(216381,allocation['reserved'][-1])

    def test_resource_envelope_cannot_revert_to_legacy(self):
        for envelope in (None,owner.RESOURCE_V1,'unknown'):
            with self.subTest(envelope=envelope), self.assertRaisesRegex(ValueError,'frozen v2'):
                owner.resources(dict(version=audit.PLACEMENT_VERSION,resourceEnvelope=envelope))
        self.assertEqual(owner.resources(dict(version=audit.ALLIED_VERSION,resourceEnvelope=owner.RESOURCE_V2)),
                         owner.resources(dict(version=audit.PLACEMENT_VERSION,resourceEnvelope=owner.RESOURCE_V2)))

    def test_catalogue_reconstructs_all_frozen_physical_recipes(self):
        request = audit.read(ROOT/'TestResults/loadout-placement-native-preview-20260924-v4/input-01.json')
        context = request['context']; catalogue = audit.placement_catalogue(context['scope'],context['benchmarkReferenceId'])
        self.assertEqual((240,2,0,0,238),(catalogue['assignmentsExamined'],catalogue['identityAssignments'],
            catalogue['referenceAssignments'],catalogue['duplicateAssignments'],len(catalogue['recipes'])))
        self.assertEqual(238,len({r['party']['id'] for r in catalogue['recipes']}))
        for row in catalogue['recipes']:
            self.assertEqual(5,len(row['assignments'][0]['sourceByDestination']))
            self.assertTrue(row['changedOwners']); self.assertGreater(row['replacementDistance'],0)


class PlacementArchiveTests(unittest.TestCase):
    root = None

    @classmethod
    def setUpClass(cls):
        if cls.root is None: raise unittest.SkipTest('Supply the placement literal fixture')
        tests.prepare(cls.root)
        if audit.read(cls.root/'request.json')['version'] != audit.PLACEMENT_VERSION:
            raise ValueError('Require a placement fixture')

    def changed(self, path, transform, manifest=None):
        return tests.ArchiveTests.changed(self,path,transform,manifest)

    def test_all_roots_both_gates_and_novel_outputs(self):
        result = audit.audit(self.root,True)['result']
        self.assertEqual(result,audit.read(self.root/'provisional-result.json'))
        self.assertEqual((18816,6144,6,6,'Inconclusive'),(result['fights'],result['heldoutFights'],result['differingRoots'],result['novelRoots'],result['decision']))
        for key in ('validation','controlValidation'):
            self.assertEqual((6,6),(result[key]['passedRoots'],result[key]['fallbackRoots']))

    def test_resealed_catalogue_corruption_rejected(self):
        path = self.root/'study/placement-catalogue-01.json'
        for change in (lambda c:c['recipes'].pop(), lambda c:c.update(scopeHash='0'*64),
                       lambda c:c['recipes'][0].update(seedFreeScenarioHash='0'*64),
                       lambda c:c['recipes'][0]['assignments'][0]['sourceByDestination'].update({'1':10})):
            with self.subTest(change=change): self.changed(path,change,self.root/'study')

    def test_missing_catalogue_rejected_even_when_manifest_resealed(self):
        path = self.root/'study/placement-catalogue-01.json'; manifest = self.root/'study/files.json'
        saved, pin = path.read_bytes(),manifest.read_bytes()
        try:
            path.unlink(); tests.seal(self.root/'study')
            with self.assertRaises(FileNotFoundError): audit.audit(self.root,True)
        finally: path.write_bytes(saved); manifest.write_bytes(pin)

    def test_mixed_inventory_cannot_be_changed(self):
        inventory = audit.read(self.root/'source/context.json')['damageAffinityInventory']
        for arm, value in (('candidate',inventory),('control',None)):
            directory = self.root/f'search/root-01/{arm}'
            with self.subTest(arm=arm):
                self.changed(directory/'racing/plan.json',lambda p:p.update(damageAffinityInventory=value),directory)

    def test_resealed_proposal_provenance_rejected(self):
        directory = self.root/'search/root-01/candidate'
        paths = [directory/'racing/search.json',directory/'racing/batch-01.json',directory/'files.json']
        saved = {p:p.read_bytes() for p in paths}
        changes = [lambda p:p['loadoutPlacement'].update(drawOrdinal=18),
            lambda p:p['loadoutPlacement'].update(catalogueHash='0'*64),
            lambda p:p['loadoutPlacement']['assignment']['sourceByDestination'].update({'1':10}),
            lambda p:p.update(replacementDistance=0), lambda p:p.update(affinityCreation={}),
            lambda p:p.update(scheduledOwners=[1]), lambda p:p.update(fallback='single')]
        for change in changes:
            try:
                report = audit.read(paths[0]); batch = report['batches'][0]; change(batch['proposals'][0])
                tests.write(paths[0],report); tests.write(paths[1],batch); tests.seal(directory)
                with self.assertRaisesRegex(ValueError,'placement provenance'): audit.audit(self.root,True)
            finally:
                for path,raw in saved.items(): path.write_bytes(raw)


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--fixture',type=Path)
    args,remaining=parser.parse_known_args(); PlacementArchiveTests.root=args.fixture.resolve() if args.fixture else None
    unittest.main(argv=[sys.argv[0],*remaining])
