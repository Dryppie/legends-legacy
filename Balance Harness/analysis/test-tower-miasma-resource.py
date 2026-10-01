"""Smaller Miasma batches preserve full-family gates and independent application."""
import copy
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

HERE=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('miasma_resource',HERE/'tower-balance-aggregate.py');a=importlib.util.module_from_spec(s);s.loader.exec_module(a)
h=a.io.ability_module().miasma_module()

class MiasmaResourceTests(unittest.TestCase):
    def setUp(self):
        plan=dict(version=h.VERSION,abilityId=h.ABILITY,removeEffect=copy.deepcopy(h.RATE),preserveEffects=[copy.deepcopy(h.HEALING)],
            descriptionFrom=h.BEFORE,descriptionTo=h.AFTER,sourceAbilitiesSha256='0'*64,sourceTowerSha256='1'*64)
        self.d=dict(version=a.MIASMA_RESOURCE_VERSION,floor=8,familySize=177,batchCount=8,samplesPerBatch=16,
            candidatePlan=plan,gear=None,equipmentEligibility=a.LIMITED_EQUIPMENT)
        self.cells=[dict(id=str(i),composition=str(i),gear='label',scenario=dict(party=[dict(partySlot=1,
            build=dict(essenceIds=[str(i)],equipment=[dict(definitionId='item.spec.'+str(j)) for j in range(4)]))])) for i in range(177)]
        self.rows=[[dict(id=c['id'],samples=16,wins=4 if int(c['id'])<2 else 0) for c in self.cells] for _ in range(8)]

    def assess(self):return a.assess(self.cells,self.rows,8,16,None,a.LIMITED_EQUIPMENT)

    def test_exact_eight_by_sixteen_layout(self):
        a.validate_layout(self.d);self.assertEqual('ability',a.candidate_kind(self.d))
        for key,value in [('floor',7),('familySize',176),('samplesPerBatch',32),('batchCount',4),('version',a.MIASMA_VERSION),('version',a.RECOVERY_VERSION)]:
            with self.subTest(key=key),self.assertRaises(ValueError):a.validate_layout({**self.d,key:value})

    def test_original_four_by_thirty_two_stays_strict(self):
        d={**self.d,'version':a.MIASMA_VERSION,'batchCount':4,'samplesPerBatch':32};a.validate_layout(d)
        for delta in ({'batchCount':8},{'samplesPerBatch':16},{'version':a.MIASMA_RESOURCE_VERSION}):
            with self.assertRaises(ValueError):a.validate_layout({**d,**delta})

    def test_exact_candidate_preserved(self):
        for key in ('removeEffect','preserveEffects','descriptionTo'):
            d=copy.deepcopy(self.d)
            if key=='removeEffect':d['candidatePlan'][key]['baseValue']=0
            elif key=='preserveEffects':d['candidatePlan'][key][0]['baseValue']=-60
            else:d['candidatePlan'][key]='changed'
            with self.assertRaises(ValueError):a.validate_layout(d)

    def test_equipment_cannot_be_weakened(self):
        for delta in ({'equipmentEligibility':None},{'gear':'armor-and-health'},
            {'equipmentEligibility':{**a.LIMITED_EQUIPMENT,'maximumSpecializedItems':9}}):
            with self.assertRaises(ValueError):a.validate_candidate({**self.d,**delta},Path('unused'))

    def test_full_panel_remains_128_samples(self):
        result=self.assess();self.assertEqual('Pass',result['verdict']);self.assertEqual(128,result['samples'])
        self.assertEqual(2,result['qualifyingPartialCompositions'])
        self.assertEqual((26,43),(min(w for w in range(129) if a.interval(w,128,177)[0]>=.1),max(w for w in range(129) if a.interval(w,128,177)[1]<=.5)))

    def test_minimum_requires_twenty_six_wins(self):
        self.rows[0][1]['wins']=0;self.rows[1][1]['wins']=1
        self.assertEqual('NotAccepted',self.assess()['verdict'])
        self.rows[1][1]['wins']=2;self.assertEqual('Pass',self.assess()['verdict'])

    def test_full_armor_still_enforces_ceiling(self):
        self.cells[2]['scenario']['party'][0]['build']['equipment']*=3
        for batch in self.rows:batch[2]['wins']=6
        self.rows[0][2]['wins']=1;self.assertEqual('Pass',self.assess()['verdict'])
        self.rows[0][2]['wins']=2;self.assertEqual('NotAccepted',self.assess()['verdict'])

    def test_partial_extra_and_reordered_panels_rejected(self):
        for rows in (self.rows[:-1],self.rows+[self.rows[0]],[self.rows[0][::-1],*self.rows[1:]]):
            with self.assertRaises(ValueError):a.assess(self.cells,rows,8,16,None,a.LIMITED_EQUIPMENT)

    def test_old_batch_counts_cannot_be_pooled(self):
        self.rows[7][0]['samples']=32
        with self.assertRaises(ValueError):self.assess()

    def test_gear_variants_do_not_create_compositions(self):
        self.cells[1]['scenario']=copy.deepcopy(self.cells[0]['scenario'])
        self.assertEqual(1,self.assess()['qualifyingPartialCompositions'])

    def test_all_sixteen_unique_paths_required(self):
        phases={p:[str(a.io.ROOT/f'TestResults/tower-balance-pass-fixture-{p}-{i}-study-20260929') for i in range(8)] for p in ('screen','confirm')}
        a.validate_paths(phases,a.io.ROOT,a.MIASMA_RESOURCE_VERSION)
        phases['confirm'][7]=phases['screen'][0]
        with self.assertRaises(ValueError):a.validate_paths(phases,a.io.ROOT,a.MIASMA_RESOURCE_VERSION)

    def test_rejected_screen_prevents_confirmation_allocation(self):
        with patch.object(a,'declaration',return_value=({**self.d,'liveCatalogPins':{}},[],set())),\
            patch.object(a,'audit_phase',return_value={'assessment':{'verdict':'NotAccepted'}}),patch.object(a.io,'history') as history:
            with self.assertRaisesRegex(ValueError,'Passing complete screening'):a.admit_batch('d','pin','confirm',0)
            history.assert_not_called()

    def test_half_batch_cost_admission_without_raising_limits(self):
        completed=dict(seconds=341.1608106,completed=5664)
        with self.assertRaises(ValueError):a.resource_admission(completed,32,177,204866086)
        admitted=a.resource_admission(completed,16,177,204866086)
        self.assertEqual(2832,admitted['fights']);self.assertEqual(341.1608106,admitted['projectedSeconds'])
        self.assertEqual(204866086,admitted['projectedBytes'])
        with self.assertRaises(ValueError):a.resource_admission(dict(seconds=336,completed=2832),16,177,1)

    def receipts(self):
        batches=[];receipts=[]
        for i in range(8):
            b=dict(source=str(i),manifestSha256='manifest',audit='audit',auditSha256='audit-pin',fights=2832,seeds=list(range(i*16,i*16+16)));batches.append(b)
            result=dict(status='AggregateInputsAndReplaysVerified',manifestPin='manifest',matchedInputs=2832,fullReplays=177,newSeeds=0,execution={})
            completion=dict(status='Verified',aggregateSha256='aggregate',resultSha256='result',matchedInputs=2832,fullReplays=177,newSeeds=0,floorFileSha256='tower')
            receipts.append(dict(request=dict(source=str(i),manifestPin='manifest',audit='audit',auditPin='audit-pin',aggregatePin='aggregate'),
                result=result,completion=completion,resultSha256='result',process=dict(exitCode=0,timedOut=False,activeProcesses=0)))
        return batches,receipts

    def test_complete_application_covers_all_inputs_and_replays(self):
        batches,receipts=self.receipts();result=a.validate_applied_parity(receipts,batches,'aggregate',{},a.MIASMA_RESOURCE_VERSION)
        self.assertEqual(22656,result['matchedInputs']);self.assertEqual(1416,result['fullReplays'])

    def test_partial_duplicate_or_mixed_parity_receipts_rejected(self):
        for change in ('missing','duplicate','wrong-fights','wrong-replays','mixed-catalog','isolated'):
            batches,receipts=self.receipts()
            if change=='missing':receipts.pop()
            if change=='duplicate':receipts[-1]=copy.deepcopy(receipts[0])
            if change=='wrong-fights':receipts[-1]['result']['matchedInputs']=5664
            if change=='wrong-replays':receipts[-1]['result']['fullReplays']=1
            if change=='mixed-catalog':receipts[-1]['completion']['floorFileSha256']='other'
            if change=='isolated':receipts[-1]['result']['status']='IsolatedInputsAndReplaysVerified'
            with self.subTest(change=change),self.assertRaises(ValueError):a.validate_applied_parity(receipts,batches,'aggregate',{},a.MIASMA_RESOURCE_VERSION)

if __name__=='__main__':unittest.main()
