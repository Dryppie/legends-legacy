"""Guard exact gear derivation and prevent diagnostic panels from becoming acceptance."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

HERE=Path(__file__).resolve().parent
def load(name,path):
    s=importlib.util.spec_from_file_location(name,path); m=importlib.util.module_from_spec(s); s.loader.exec_module(m); return m
h=load('eight_item',HERE/'tower-eight-item-diagnostic.py')
old=load('one_healer_fixture',HERE/'test-tower-one-healer-family.py')


class EightItemTests(unittest.TestCase):
    def setUp(self):
        fixture=old.OneHealerFamilyTests(); fixture.setUp()
        self.original=fixture.validate(); self.parents=fixture.parents
        variants=h.derive(self.original,self.parents)
        self.p=dict(version=h.VERSION,status='ProposedNotPrepared',floor=8,newFights=0,newSeeds=0,nativePreparations=0,
            retainedCells=183,newVariants=3,totalCells=186,actualCompositions=9,eligibleRecipes=128,
            equipmentEligibility=copy.deepcopy(h.h.EQUIPMENT),proposedDiagnostic=copy.deepcopy(h.LIMITS),
            variants=variants,cells=self.original+[v['cell'] for v in variants])

    def validate(self,p=None): return h.validate(p or self.p,self.original,self.parents)

    def test_preserves_all_183_controls(self):
        before=copy.deepcopy(self.original); self.assertEqual(self.validate()[:183],before); self.assertEqual(self.original,before)

    def test_exact_four_items_per_healer(self):
        for c in self.validate()[183:]: self.assertEqual([2]*4+[7]*4,h.h.specialized(c))

    def test_retains_rings_relics_and_other_characters(self):
        lookup={c['id']:c for c in self.original}
        for v in self.p['variants']:
            for before,after in zip(lookup[v['baselineId']]['scenario']['party'],v['cell']['scenario']['party']):
                for b,n in zip(before['build']['equipment'],after['build']['equipment']):
                    if before['partySlot'] not in (2,7) or b['slot'] not in h.SLOTS: self.assertEqual(b,n)

    def test_drop_reorder_duplicate_or_add_control_rejected(self):
        for op in ('drop','reorder','duplicate','add'):
            p=copy.deepcopy(self.p)
            if op=='drop':p['cells'].pop()
            if op=='reorder':p['cells'][0],p['cells'][1]=p['cells'][1],p['cells'][0]
            if op=='duplicate':p['cells'][1]=copy.deepcopy(p['cells'][0])
            if op=='add':p['cells'].append(copy.deepcopy(p['cells'][0]))
            with self.assertRaises(ValueError):self.validate(p)

    def test_essence_order_identity_and_progression_change_rejected(self):
        for key,value in [('essenceIds',['changed']),('identityEssenceIds',['changed']),('id','new'),('characterLevel',41),('tier',2),('rank',5),('quality','Masterpiece')]:
            p=copy.deepcopy(self.p);p['variants'][0]['cell']['scenario']['party'][1]['build'][key]=value
            with self.assertRaises(ValueError):self.validate(p)

    def test_wrong_slot_or_extra_equipment_rejected(self):
        for slot in (0,1,6):
            p=copy.deepcopy(self.p);p['variants'][0]['cell']['scenario']['party'][slot]['build']['equipment'][-1]['definitionId']='item.spec.changed'
            with self.assertRaises(ValueError):self.validate(p)

    def test_three_actual_compositions_required(self):
        for parents in (self.parents[:2],[self.parents[0]]*3,[*self.parents[:2],'missing']):
            with self.assertRaises(ValueError):h.derive(self.original,parents)

    def test_parent_cannot_change_non_equipment_fields(self):
        changed=copy.deepcopy(self.original);next(c for c in changed if c['id']==self.parents[0])['scenario']['party'][1]['build']['essenceIds'].reverse()
        with self.assertRaises(ValueError):h.derive(changed,self.parents)

    def test_limits_and_metadata_cannot_be_weakened(self):
        for delta in ({'eligibleRecipes':129},{'retainedCells':182},{'equipmentEligibility':{**h.h.EQUIPMENT,'maximumSpecializedItems':12}},
                      {'proposedDiagnostic':{**h.LIMITS,'confirmation':True}},{'newSeeds':1},{'nativePreparations':1},{'status':'Complete'}):
            with self.assertRaises(ValueError):self.validate({**self.p,**delta})

    def test_preparation_only_without_mixed_modes(self):
        h.validate_mode('prepare',8,Path('source'),[])
        for mode in ('screen','search','confirm'):
            with self.assertRaises(ValueError):h.validate_mode(mode,8,Path('source'),[])
        for i in range(16):
            modifications=[False]*16;modifications[i]=True
            with self.assertRaises(ValueError):h.validate_mode('prepare',8,Path('source'),modifications)

    def test_ordinary_screen_search_confirm_still_require_sixteen(self):
        for mode in ('screen','search','confirm'):
            h.validate_panel(mode,8,16,None);h.validate_panel(mode,8,512,None)
            for n in (0,8,15,513):
                with self.assertRaises(ValueError):h.validate_panel(mode,8,n,None)

    def test_small_panel_only_exact_diagnostic_scope(self):
        h.validate_panel('screen',8,8,h.DIAGNOSTIC,186)
        for args in [('confirm',8,8,h.DIAGNOSTIC,186),('search',8,8,h.DIAGNOSTIC,186),('screen',7,8,h.DIAGNOSTIC,186),
                     ('screen',8,16,h.DIAGNOSTIC,186),('screen',8,8,'unknown',186),('screen',8,8,h.DIAGNOSTIC,185)]:
            with self.assertRaises(ValueError):h.validate_panel(*args)

    def fixture(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);root=Path(temp.name);source=root/'source';source.mkdir()
        def write(p,v):p.write_text(json.dumps(v),encoding='utf-8')
        write(source/'cells.json',self.original);write(source/'files.json',{})
        e=root/'evidence.json';write(e,dict(status='Verified',trialStatus='CompletedDiagnosticOnly',diagnosticOnly=True,usedForAcceptance=False,familySize=183,
            comparisons=[dict(oneHealer=dict(wins=w),twoHealers=dict(id=self.parents[i//2],wins=[5,6,6][i//2])) for i,w in enumerate([1,0,0,1,0,0])]))
        self.p.update(source=str(source),sourceManifestSha256=h.h.sha(source/'files.json'),sourceCellsSha256=h.h.sha(source/'cells.json'),precedingEvidence=str(e),precedingEvidenceSha256=h.h.sha(e))
        path=root/'proposal.json';write(path,self.p);return root,source,path,write

    def test_authenticated_proposal_admitted(self):
        _,source,path,_=self.fixture();self.assertEqual(self.p['cells'],h.admit(path,source))

    def test_changed_provenance_rejected(self):
        _,source,path,write=self.fixture()
        for key in ('sourceManifestSha256','sourceCellsSha256','precedingEvidenceSha256'):
            write(path,{**self.p,key:'0'*64})
            with self.assertRaises(ValueError):h.admit(path,source)

    def test_diagnostic_cannot_claim_acceptance(self):
        _,source,path,write=self.fixture();e=Path(self.p['precedingEvidence']);data=h.h.read(e);data['usedForAcceptance']=True
        write(e,data);write(path,{**self.p,'precedingEvidenceSha256':h.h.sha(e)})
        with self.assertRaises(ValueError):h.admit(path,source)

    def contract(self):
        root,source,path,write=self.fixture();write(source/'cells.json',self.p['cells'])
        d=dict(version=h.DIAGNOSTIC,limits=copy.deepcopy(h.LIMITS),floor=8,familySize=186,source=str(source),cellsSha256=h.h.sha(source/'cells.json'),
            studies=[str(root/f'batch-{i}-study-test') for i in (1,2)],initialExclusions=924204,eligibleRecipes=128,actualCompositions=9)
        return root,source,d,write

    def test_contract_rejects_extra_batch_retry_or_acceptance(self):
        _,_,d,_=self.contract();h.validate_contract(d,self.p['cells'])
        for key,value in [('batches',3),('seedsPerBatch',16),('maximumFights',3000),('retries',1),('extensions',1),('confirmation',True),('application',True),('diagnosticOnly',False),('usedForAcceptance',True)]:
            bad=copy.deepcopy(d);bad['limits'][key]=value
            with self.assertRaises(ValueError):h.validate_contract(bad,self.p['cells'])

    def test_contract_rejects_duplicate_or_relative_paths(self):
        _,_,d,_=self.contract()
        for studies in ([d['studies'][0]]*2,['one','two'],d['studies'][:1]):
            with self.assertRaises(ValueError):h.validate_contract({**d,'studies':studies},self.p['cells'])

    def test_marker_and_provenance_must_match(self):
        for q in [dict(inputHashes={},diagnosticVersion=h.DIAGNOSTIC),dict(inputHashes={'eight-item-diagnostic-provenance.json':'x'})]:
            with self.assertRaises(ValueError):h.audit_request(None,Path('out'),q)

    def test_unknown_batch_or_mixed_modification_rejected_before_allocation(self):
        for index,mods,seconds in [(2,[],840),(-1,[],840),(0,[True],840),(0,[],1140)]:
            with self.assertRaises(ValueError):h.admit_batch(None,Path('absent'),None,index,None,None,'screen',8,8,None,mods,seconds)

    def test_changed_declaration_pin_rejected(self):
        root,_,_,write=self.contract();path=root/'declaration.json';write(path,{})
        for pin in (None,'0'*64):
            with self.assertRaises(ValueError):h.read_contract(None,path,pin)


if __name__=='__main__':unittest.main()
