"""Guard the Ni sample, resource admission and interpretation of native events."""
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

def module(name, filename):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(filename));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
d=module('ni_diagnostic','diagnose-tower-ni-corrected.py')
old=module('ni_event_fixtures','test-tower-kodoku-diagnostic.py')


class SelectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan=d.io.read(d.ROOT/'TestResults/tower-floor9-corrected-pressure-proposal-20261001.json')
        source=Path(cls.plan['source']);cls.cells=d.io.read(source/'cells.json');cls.seeds=d.io.read(source/'request.json')['seeds']
        cls.trials=[json.loads(line) for line in (source/'evaluation/trials.jsonl').read_text().splitlines()]

    def validate(self, **changes):
        args=dict(plan=self.plan,cells=self.cells,seeds=self.seeds,trials=self.trials);args.update(changes);return d.validate_selection(**args)

    def test_exact_six_recipes(self):
        self.assertEqual([[],[2,4],list(range(1,11)),[],[3,9],list(range(1,11))],[r['resistanceSlots'] for r in self.validate()])

    def test_limits_and_acceptance_flags_cannot_change(self):
        for key,value in [('maximumHistoricalReplays',97),('maximumSeconds',841),('perReplaySeconds',61),('maximumLogBytes',32*1024**2),
            ('maximumBytes',3*1024**3),('allocatedReplays',1),('newSeeds',1),('newStudyFights',1),('usedForAcceptance',True),
            ('gameplayCandidateSelected',True),('requireCompleteSavedReportParityAfterRemovingEventLog',False),
            ('requiresMeasuredResourceAdmission',False),('maximumAdmittedSeconds',673),('maximumAdmittedBytes',2*1024**3)]:
            with self.subTest(key=key):
                p=copy.deepcopy(self.plan);p[key]=value
                with self.assertRaises(ValueError):self.validate(plan=p)

    def test_wrong_floor_or_version_rejected(self):
        for key,value in [('floor',8),('version','floor8-gear-diagnostic-proposal-v1'),('status','Executed')]:
            p=copy.deepcopy(self.plan);p[key]=value
            with self.assertRaises(ValueError):self.validate(plan=p)

    def test_pair_order_is_fixed(self):
        p=copy.deepcopy(self.plan);p['selected'][0],p['selected'][1]=p['selected'][1],p['selected'][0]
        with self.assertRaisesRegex(ValueError,'paired sample'):self.validate(plan=p)

    def test_no_outcome_selected_seeds(self):
        p=copy.deepcopy(self.plan);p['seeds']=self.seeds[16:]
        with self.assertRaisesRegex(ValueError,'First sixteen'):self.validate(plan=p)

    def test_complete_unique_seeds_required(self):
        for seeds in (self.seeds[:-1],[self.seeds[0]]*32):
            with self.assertRaises(ValueError):self.validate(seeds=seeds)

    def test_preserves_complete_family(self):
        with self.assertRaisesRegex(ValueError,'Complete floor-nine'):self.validate(cells=self.cells[:-1])

    def test_trial_identity_cannot_repeat(self):
        with self.assertRaisesRegex(ValueError,'unique trial'):self.validate(trials=self.trials[:-1]+self.trials[:1])

    def test_raw_essence_order_is_preserved(self):
        cells=copy.deepcopy(self.cells);c=next(c for c in cells if c['id']==self.plan['comparisons'][0]['limitedId'])
        c['scenario']['party'][0]['build']['essenceIds'].reverse()
        with self.assertRaisesRegex(ValueError,'Raw identity'):self.validate(cells=cells)

    def test_unselected_equipment_is_preserved(self):
        cells=copy.deepcopy(self.cells);c=next(c for c in cells if c['id']==self.plan['comparisons'][0]['limitedId'])
        c['scenario']['party'][0]['build']['equipment'][0]['definitionId']+='changed'
        with self.assertRaisesRegex(ValueError,'equipment substitution'):self.validate(cells=cells)

    def test_selected_slots_are_fixed(self):
        p=copy.deepcopy(self.plan);p['comparisons'][0]['limitedPartySlots']=[1,2]
        with self.assertRaisesRegex(ValueError,'limited slots'):self.validate(plan=p)


    def test_cannot_change_corrected_scalar_source(self):
        for key,value in [('sourceOffenseFactor',1),('sourceOffense',4.7036132812),('sourceManifestSha256',d.ni.SOURCE_PIN),('initialExclusions',925996),('retries',1),('extensions',1)]:
            p=copy.deepcopy(self.plan);p[key]=value
            with self.subTest(key=key),self.assertRaises(ValueError):self.validate(plan=p)

    def test_corrected_source_and_runtime_are_authenticated(self):
        entry=d.io.read(d.ROOT/'TestResults/tower-floor9-corrected-ni-entry-20261001/entry.json')
        self.assertEqual(Path(self.plan['source']),d.validate_source(self.plan,entry))
        for change in ('assembly','runtime'):
            p=copy.deepcopy(self.plan)
            if change=='assembly':p['runtimeExecution']['assemblyHashes']['Services.LL']='0'*64
            else:p['runtime']='old-runtime'
            with self.subTest(change=change),self.assertRaises(ValueError):d.validate_source(p,entry)

    def test_raw_actor_identity_is_preserved(self):
        cells=copy.deepcopy(self.cells);c=next(c for c in cells if c['id']==self.plan['comparisons'][0]['limitedId'])
        c['scenario']['party'][0]['build']['id']='changed-actor'
        with self.assertRaisesRegex(ValueError,'Raw identity'):self.validate(cells=cells)

if __name__=='__main__':unittest.main()
