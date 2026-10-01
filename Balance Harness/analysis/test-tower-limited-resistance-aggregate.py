"""Four-by-32 unchanged-catalog safeguards, independent of the older contracts."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('limited_resistance_aggregate',Path(__file__).with_name('tower-balance-aggregate.py'))
a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)

class LimitedResistanceAggregateTests(unittest.TestCase):
    def setUp(self):
        self.d=dict(version=a.LIMITED_RESISTANCE_VERSION,batchCount=4,samplesPerBatch=32,floor=9,familySize=148,
            candidatePlan=dict(version='tower-unchanged-catalog-v1'),gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT)
        self.cells=[dict(id=str(i),composition=str(i),gear='untrusted-label',scenario=dict(party=[dict(partySlot=s,
            build=dict(essenceIds=[str(i),'shared'],equipment=[dict(definitionId='item.spec.'+str(j))
            for j in range(4 if s<3 or i>1 else 0)])) for s in range(1,11)])) for i in range(148)]
        self.rows=[[dict(id=c['id'],samples=32,wins=7 if i<2 else 8) for i,c in enumerate(self.cells)] for _ in range(4)]

    def assess(self): return a.assess(self.cells,self.rows,4,32,None,a.LIMITED_EQUIPMENT)

    def test_strict_layouts_cannot_be_interchanged(self):
        a.validate_layout(self.d)
        for key,value in [('version',a.VERSION),('version',a.RECOVERY_VERSION),('version',a.LIMITED_ARMOR_VERSION),('batchCount',8),('samplesPerBatch',128),
            ('floor',7),('familySize',147),('candidatePlan',dict(version='tower-unchanged-catalog-v1',offenseFactor=.9))]:
            with self.subTest(key=key),self.assertRaises(ValueError):a.validate_layout({**self.d,key:value})

    def test_unchanged_candidate_cannot_use_another_version(self):
        self.assertEqual('unchanged',a.candidate_kind(self.d))
        for version in (a.VERSION,a.RECOVERY_VERSION,'unknown',None):
            with self.assertRaises(ValueError):a.candidate_kind({**self.d,'version':version})

    def test_content_and_equipment_must_match_exactly(self):
        with tempfile.TemporaryDirectory() as temp:
            content=Path(temp);(content/'Data').mkdir();p=content/'Data/catalog.json';a.io.write(p,{'unchanged':True})
            d={**self.d,'candidateContentHashes':{'catalog.json':a.io.sha(p)}}
            a.validate_candidate(d,content)
            for changes in ({'gear':'label'},{'equipmentEligibility':None},{'candidateContentHashes':{}},
                {'equipmentEligibility':{**a.LIMITED_EQUIPMENT,'maximumSpecializedItems':9}}):
                with self.assertRaises(ValueError):a.validate_candidate({**d,**changes},content)
            p.write_text('{"unchanged":false}',encoding='utf-8')
            with self.assertRaisesRegex(ValueError,'Unchanged catalog differs'):a.validate_candidate(d,content)

    def test_all_four_batches_and_whole_family_are_required(self):
        result=self.assess();self.assertEqual('Pass',result['verdict']);self.assertEqual(128,result['samples'])
        self.assertEqual(2,result['qualifyingPartialCompositions'])
        self.assertLess(a.interval(7,32,148)[0],.1)
        for rows in (self.rows[:-1],self.rows+[self.rows[0]],[self.rows[0][:-1],*self.rows[1:]],
                     [self.rows[0][::-1],*self.rows[1:]]):
            with self.assertRaises(ValueError):a.assess(self.cells,rows,4,32,None,a.LIMITED_EQUIPMENT)

    def test_exact_25_to_43_gates_and_both_boundaries(self):
        self.assertEqual((25,43),(min(w for w in range(129) if a.interval(w,128,148)[0]>=.1),
                                 max(w for w in range(129) if a.interval(w,128,148)[1]<=.5)))
        self.rows[0][1]['wins']=3 # 24 total
        self.assertEqual('NotAccepted',self.assess()['verdict'])
        self.rows[0][1]['wins']=4
        self.assertEqual('Pass',self.assess()['verdict'])
        for row in self.rows:row[-1]['wins']=11
        self.assertEqual('NotAccepted',self.assess()['verdict']) # Ineligible full armor still fails ceiling.
        self.rows[0][-1]['wins']=10
        self.assertEqual('Pass',self.assess()['verdict'])

    def test_actual_items_characters_and_order_control_eligibility(self):
        baseline=copy.deepcopy(self.cells)
        for change in ('items','characters','order'):
            self.cells=copy.deepcopy(baseline);party=self.cells[1]['scenario']['party']
            if change=='items':party[0]['build']['equipment'].append(dict(definitionId='item.spec.extra'))
            if change=='characters':party[9]['build']['equipment'].append(party[0]['build']['equipment'].pop())
            if change=='order':
                for m in party:m['build']['essenceIds']=['shared','0']
            with self.subTest(change=change):self.assertEqual('NotAccepted',self.assess()['verdict'])

    def test_wrong_counts_rejected(self):
        for value in (-1,33,True,1.5):
            self.rows[0][0]['wins']=value
            with self.assertRaises(ValueError):self.assess()
        self.rows[0][0]['wins']=7;self.rows[0][0]['samples']=128
        with self.assertRaises(ValueError):self.assess()

    def test_complete_unique_paths_required(self):
        phases={p:[str(a.io.ROOT/f'TestResults/tower-balance-pass-limited-{p}-{i}-study-20260929') for i in range(4)] for p in ('screen','confirm')}
        a.validate_paths(phases,a.io.ROOT,a.LIMITED_RESISTANCE_VERSION)
        phases['confirm'][3]=phases['screen'][0]
        with self.assertRaises(ValueError):a.validate_paths(phases,a.io.ROOT,a.LIMITED_RESISTANCE_VERSION)

    def test_resource_split_has_headroom_but_single_panel_is_rejected(self):
        completed=dict(seconds=194.9380479,completed=9728)
        result=a.resource_admission(completed,32,148,228097799)
        self.assertEqual(4736,result['fights']);self.assertLess(result['projectedSeconds'],672)
        with self.assertRaises(ValueError):a.resource_admission(completed,128,148,228097799)

    def test_modified_candidate_provenance_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            study=Path(temp)/'x-study-date';owner=a.owner(study);study.mkdir();owner.mkdir()
            d={**self.d,'runtime':{},'settings':{},'candidateContentHashes':{}}
            provenance=owner/'ability-candidate-provenance.json';a.io.write(provenance,dict(plan=d['candidatePlan']))
            for path,data in [(study/'cells.json',self.cells),(study/'scope.json',dict(execution={},settings={},contentHashes={})),
                (study/'request.json',dict(mode='screen',floor=9,searchSeeds=[],inputHashes={str(provenance):a.io.sha(provenance)})),
                (owner/'independent-audit.json',{})]:a.io.write(path,data)
            with patch.object(a.io,'audit',return_value={}),self.assertRaisesRegex(ValueError,'candidate modification'):
                a.verify_batch(d,self.cells,study,'screen',set())

    def test_failed_screen_cannot_allocate_confirmation(self):
        with patch.object(a,'declaration',return_value=({**self.d,'liveCatalogPins':{}},[],set())),\
             patch.object(a,'audit_phase',return_value={'assessment':{'verdict':'NotAccepted'}}),\
             patch.object(a.io,'history') as history:
            with self.assertRaisesRegex(ValueError,'Passing complete screening'):a.admit_batch('d','pin','confirm',0)
            history.assert_not_called()

    def test_application_requires_four_complete_parity_receipts(self):
        with self.assertRaisesRegex(ValueError,'Incomplete applied'):
            a.validate_applied_parity([],[],'pin',{},a.LIMITED_RESISTANCE_VERSION)

if __name__=='__main__':unittest.main()
