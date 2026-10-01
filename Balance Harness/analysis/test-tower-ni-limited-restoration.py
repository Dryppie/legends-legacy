"""Protect exact healer subsets, raw identities and diagnostic-only execution."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('ni_restoration',HERE/'tower-ni-limited-restoration.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)

class RestorationTests(unittest.TestCase):
    def setUp(self):
        self.original=[]
        for comp in range(5):
            base=dict(id=f'base-{comp}',composition=f'comp-{comp}',gear='baseline',origin='test',scenario=dict(floorNumber=9,seeds=[],party=[
                dict(partySlot=slot,build=dict(id=f'actor-{comp}-{slot}',essenceIds=[f'essence-{comp}',f'slot-{slot}'],identityEssenceIds=['identity'],characterLevel=40,
                    equipment=[dict(slot=s,definitionId='plain.'+s,rank=4,quality='Exceptional') for s in sorted(m.h.SLOTS)])) for slot in range(1,11)]))
            full=copy.deepcopy(base);full.update(id=f'full-{comp}',gear='restorer-specialization')
            for member in full['scenario']['party']:
                if member['partySlot'] in (2,7):
                    for item in member['build']['equipment']:item['definitionId']+='.spec.restoration'
            self.original.extend([base,full])
        for i in range(138):
            cell=copy.deepcopy(self.original[(i%5)*2]);cell.update(id=f'control-{i}',gear=f'control-{i}')
            cell['scenario']['party'][0]['build']['id']=f'control-{i}'
            if i>=110:
                for member in cell['scenario']['party']:
                    for item in member['build']['equipment']:item['definitionId']+='.spec.health'
            self.original.append(cell)
        variants=m.derive(self.original)
        self.p=dict(version=m.VERSION,status='ProposedNotPrepared',floor=9,newFights=0,newSeeds=0,nativePreparations=0,
            retainedCells=148,newVariants=15,totalCells=163,actualCompositions=5,eligibleRecipes=130,equipmentEligibility=copy.deepcopy(m.h.EQUIPMENT),
            proposedDiagnostic=copy.deepcopy(m.LIMITS),candidatePlan={**m.n.VALUES,**{k:'a'*64 for k in m.n.HASHES}},variants=variants,cells=self.original+[v['cell'] for v in variants])

    def validate(self,p=None):return m.validate(p or self.p,self.original)

    def test_exact_family_preserves_all_controls(self):
        before=copy.deepcopy(self.original);cells=self.validate();self.assertEqual(before,cells[:148]);self.assertEqual(before,self.original)
        self.assertEqual(163,len(cells));self.assertEqual(5,len({m.h.composition(c) for c in cells}))

    def test_six_and_eight_items_for_all_five_compositions(self):
        self.assertEqual(([2]*6,[7]*6,[2]*4+[7]*4)*5,tuple(m.h.specialized(v['cell']) for v in self.p['variants']))

    def test_unselected_items_and_participants_preserved(self):
        lookup={c['id']:c for c in self.original}
        for v in self.p['variants']:
            for b,a in zip(lookup[v['baselineId']]['scenario']['party'],v['cell']['scenario']['party']):
                for x,y in zip(b['build']['equipment'],a['build']['equipment']):
                    if b['partySlot'] not in v['specializedPartySlots'] or v['equipmentSlots'] and x['slot'] not in v['equipmentSlots']:self.assertEqual(x,y)

    def test_drop_reorder_duplicate_or_add_control_rejected(self):
        for operation in ('drop','reorder','duplicate','add'):
            p=copy.deepcopy(self.p)
            if operation=='drop':p['cells'].pop(0)
            if operation=='reorder':p['cells'][0],p['cells'][1]=p['cells'][1],p['cells'][0]
            if operation=='duplicate':p['cells'][1]=copy.deepcopy(p['cells'][0])
            if operation=='add':p['cells'].append(copy.deepcopy(p['cells'][0]))
            with self.assertRaises(ValueError):self.validate(p)

    def test_raw_identity_essence_order_and_progression_preserved(self):
        for key,value in [('essenceIds',['changed']),('identityEssenceIds',['changed']),('id','new'),('characterLevel',41),('tier',2)]:
            p=copy.deepcopy(self.p);p['variants'][0]['cell']['scenario']['party'][1]['build'][key]=value
            with self.assertRaises(ValueError):self.validate(p)
        p=copy.deepcopy(self.p);p['variants'][0]['cell']['scenario']['party'][1]['build']['essenceIds'].reverse()
        with self.assertRaises(ValueError):self.validate(p)

    def test_extra_equipment_quality_or_rank_rejected(self):
        for key,value in [('definitionId','other.spec.item'),('rank',5),('quality','Masterpiece')]:
            p=copy.deepcopy(self.p);p['variants'][0]['cell']['scenario']['party'][0]['build']['equipment'][0][key]=value
            with self.assertRaises(ValueError):self.validate(p)

    def test_parent_non_equipment_changes_rejected(self):
        for change in ('essences','identity','party-order','item-order'):
            cells=copy.deepcopy(self.original);party=cells[1]['scenario']['party']
            if change=='essences':party[1]['build']['essenceIds'].reverse()
            if change=='identity':party[1]['build']['id']='new'
            if change=='party-order':party.reverse()
            if change=='item-order':party[1]['build']['equipment'].reverse()
            with self.assertRaises(ValueError):m.derive(cells)

    def test_all_five_distinct_parents_required(self):
        cells=copy.deepcopy(self.original);cells[1]['gear']='other'
        with self.assertRaises(ValueError):m.derive(cells)

    def test_metadata_and_limits_cannot_be_weakened(self):
        for delta in ({'eligibleRecipes':131},{'retainedCells':147},{'newSeeds':1},{'nativePreparations':1},{'floor':8},{'status':'Complete'},
                      {'equipmentEligibility':{**m.h.EQUIPMENT,'maximumSpecializedItems':12}},{'proposedDiagnostic':{**m.LIMITS,'confirmation':True}}):
            with self.assertRaises(ValueError):self.validate({**self.p,**delta})

    def test_candidate_cannot_change(self):
        for field,value in [('offenseFactor',.94),('penetrationFactor',39),('healthFactor',.5),('floor',8)]:
            p=copy.deepcopy(self.p);p['candidatePlan'][field]=value
            with self.assertRaises(ValueError):self.validate(p)

    def test_preparation_requires_unmixed_zero_seed_mode(self):
        m.validate_mode('prepare',9,Path('source'),[])
        for mode in ('screen','search','confirm'):
            with self.assertRaises(ValueError):m.validate_mode(mode,9,Path('source'),[])
        for args in [('prepare',8,Path('source'),[]),('prepare',9,None,[]),('prepare',9,Path('source'),[True])]:
            with self.assertRaises(ValueError):m.validate_mode(*args)

    def test_exact_non_acceptance_panel_required(self):
        m.validate_panel('screen',9,16,m.DIAGNOSTIC,163)
        for args in [('confirm',9,16,m.DIAGNOSTIC,163),('search',9,16,m.DIAGNOSTIC,163),('prepare',9,16,m.DIAGNOSTIC,163),
                     ('screen',8,16,m.DIAGNOSTIC,163),('screen',9,32,m.DIAGNOSTIC,163),('screen',9,16,None,163),('screen',9,16,m.DIAGNOSTIC,162)]:
            with self.assertRaises(ValueError):m.validate_panel(*args)

    def test_admission_rejects_scalar_change_or_mixed_candidate_before_history(self):
        fake=SimpleNamespace()
        for factors in [(1,.94,40),(1,.95,39),(.5,.95,40)]:
            with self.assertRaises(ValueError):m.admit_batch(fake,None,None,0,None,None,'screen',9,16,None,[],840,scalar_factors=factors)
        for delta in [(2,None,[],840),(0,'ability',[],840),(0,None,[True],840),(0,None,[],900)]:
            with self.assertRaises(ValueError):m.admit_batch(fake,None,None,delta[0],None,None,'screen',9,16,delta[1],delta[2],delta[3],scalar_factors=(1,.95,40))

    def test_diagnostic_marker_cannot_be_missing(self):
        for q in [dict(inputHashes={},diagnosticVersion=m.DIAGNOSTIC),dict(inputHashes={},diagnosticVersion=None)]:
            with self.assertRaises(ValueError):m.audit_request(SimpleNamespace(),Path('study'),q)

    def test_candidate_not_routed_into_old_acceptance(self):
        spec=importlib.util.spec_from_file_location('aggregate_guard',HERE/'tower-balance-aggregate.py');a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
        d=dict(version=a.NI_PENETRATION_VERSION,floor=9,familySize=163,batchCount=4,samplesPerBatch=32,candidatePlan=self.p['candidatePlan'])
        with self.assertRaises(ValueError):a.validate_layout(d)

if __name__=='__main__':unittest.main()
