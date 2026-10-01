"""Eight-batch recovery safeguards; the legacy four-batch contract stays strict."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('recovery_aggregate',Path(__file__).with_name('tower-balance-aggregate.py'))
a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)

class RecoveryAggregateTests(unittest.TestCase):
    def setUp(self):
        self.d=dict(version=a.RECOVERY_VERSION,batchCount=8,samplesPerBatch=128,floor=7,familySize=120,
                    candidatePlan=dict(version='tower-recovery-pressure-refinement-v1'),gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT)
        self.cells=[dict(id=str(i),composition=str(i),gear='label',scenario=dict(party=[dict(partySlot=1,
            build=dict(essenceIds=[str(i)],equipment=[dict(definitionId='item.spec.'+str(j)) for j in range(6)]))])) for i in range(120)]
        self.rows=[[dict(id=c['id'],samples=128,wins=18 if int(c['id'])==1 else 40 if int(c['id'])==0 else 0) for c in self.cells] for _ in range(8)]

    def assess(self):return a.assess(self.cells,self.rows,8,128,None,a.LIMITED_EQUIPMENT)

    def test_distinct_strict_layouts(self):
        a.validate_layout(self.d)
        a.validate_layout(dict(version=a.VERSION,batchCount=4,samplesPerBatch=128,candidatePlan=dict(version='tower-health-pressure-candidate-v1')))
        for key,value in [('version',a.VERSION),('version','unknown'),('batchCount',4),('samplesPerBatch',160),('floor',4),('familySize',119),
                          ('candidatePlan',dict(version='tower-recovery-pressure-candidate-v1'))]:
            with self.subTest(key=key),self.assertRaises(ValueError):a.validate_layout({**self.d,key:value})

    def test_recovery_cannot_use_legacy_candidate_dispatch(self):
        self.assertEqual('recovery-pressure',a.candidate_kind(self.d))
        for version in (a.VERSION,None,'unknown'):
            with self.assertRaises(ValueError):a.candidate_kind({**self.d,'version':version})

    def test_exact_candidate_and_equipment_validation(self):
        with patch.object(a.io,'recovery_pressure_module') as recovery:
            a.validate_candidate(self.d,Path('content'))
            recovery.assert_called_once_with('tower-recovery-pressure-refinement-v1')
            recovery.return_value.expected.assert_called_once_with(Path('content'),self.d['candidatePlan'],7)
        for change in ({'equipmentEligibility':None},{'gear':'label'}, {'equipmentEligibility':{**a.LIMITED_EQUIPMENT,'maximumSpecializedItems':9}}):
            with self.assertRaises(ValueError):a.validate_candidate({**self.d,**change},Path('content'))

    def test_eight_batches_pass_without_borrowing_legacy_counts(self):
        result=self.assess();self.assertEqual('Pass',result['verdict']);self.assertEqual(1024,result['samples'])
        self.assertEqual(2,result['qualifyingPartialCompositions'])
        self.assertLess(a.interval(18,128,120)[0],.1)

    def test_exact_gates_and_boundaries(self):
        self.assertEqual((137,455),(min(w for w in range(1025) if a.interval(w,1024,120)[0]>=.1),
                                  max(w for w in range(1025) if a.interval(w,1024,120)[1]<=.5)))
        self.rows[0][1]['wins']=10
        self.assertEqual('NotAccepted',self.assess()['verdict']) # 136 wins, one below minimum
        self.rows[0][1]['wins']=11
        self.assertEqual('Pass',self.assess()['verdict'])
        for row in self.rows:row[2]['wins']=57
        self.assertEqual('NotAccepted',self.assess()['verdict']) # 456 wins, one above ceiling

    def test_missing_extra_repeated_rows_rejected(self):
        for rows in (self.rows[:-1],self.rows+[self.rows[0]],[self.rows[0][::-1],*self.rows[1:]]):
            with self.assertRaises(ValueError):a.assess(self.cells,rows,8,128,None,a.LIMITED_EQUIPMENT)

    def test_equipment_and_order_cannot_create_second_composition(self):
        self.cells[1]['scenario']['party'][0]['build']['equipment']*=2
        self.assertEqual('NotAccepted',self.assess()['verdict'])
        self.cells[1]['scenario']=copy.deepcopy(self.cells[0]['scenario'])
        self.assertEqual(1,self.assess()['qualifyingPartialCompositions'])

    def test_paths_cannot_mix_four_and_eight_batch_versions(self):
        phases={p:[str(a.io.ROOT/f'TestResults/tower-balance-pass-fixture-{p}-{i}-study-20260929') for i in range(8)] for p in ('screen','confirm')}
        a.validate_paths(phases,a.io.ROOT,a.RECOVERY_VERSION)
        with self.assertRaises(ValueError):a.validate_paths(phases,a.io.ROOT)
        phases['confirm'][7]=phases['screen'][0]
        with self.assertRaises(ValueError):a.validate_paths(phases,a.io.ROOT,a.RECOVERY_VERSION)

    def test_wrong_provenance_rejected_before_seed_or_result_reads(self):
        with tempfile.TemporaryDirectory() as temp:
            study=Path(temp)/'x-study-date';owned=a.owner(study);study.mkdir();owned.mkdir()
            d={**self.d,'runtime':{},'settings':{},'candidateContentHashes':{}}
            provenance=owned/'health-pressure-candidate-provenance.json';a.io.write(provenance,dict(plan=d['candidatePlan']))
            for path,data in [(study/'cells.json',self.cells),(study/'scope.json',dict(execution={},settings={},contentHashes={})),
                (study/'request.json',dict(mode='screen',floor=7,searchSeeds=[],inputHashes={str(provenance):a.io.sha(provenance)})),
                (owned/'independent-audit.json',{})]:a.io.write(path,data)
            with patch.object(a.io,'audit',return_value={}),self.assertRaisesRegex(ValueError,'candidate kind'):
                a.verify_batch(d,self.cells,study,'screen',set())

    def test_application_needs_eight_receipts(self):
        with self.assertRaisesRegex(ValueError,'Incomplete applied'):
            a.validate_applied_parity([],[],'pin',{},a.RECOVERY_VERSION)
        with self.assertRaises(ValueError):a.validate_applied_parity([],[],'pin',{},'unknown')

if __name__=='__main__':unittest.main()
