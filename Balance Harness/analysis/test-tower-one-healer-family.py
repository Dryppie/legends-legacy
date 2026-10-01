"""Reject undeclared recipe changes, weakened equipment limits and invalid provenance."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

HERE=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('one_healer',HERE/'tower-one-healer-family.py');h=importlib.util.module_from_spec(s);s.loader.exec_module(h)


class OneHealerFamilyTests(unittest.TestCase):
    def setUp(self):
        self.original=[]
        for c in range(9):
            self.original.append(dict(id=f'base-{c}',composition=str(c),gear='baseline',origin='saved',scenario=dict(id=f'party-{c}',floorNumber=8,seeds=[],
                party=[dict(partySlot=i,build=dict(id=f'char-{c}-{i}',characterLevel=40,tier=1,rank=4,quality='Exceptional',attributeRollMultiplier=1,
                    essenceIds=[f'{c}-{j}' for j in range(5)],identityEssenceIds=['original'],equipment=[dict(slot=slot,definitionId='plain.'+slot+'.unique',useNativeStyle=False,activeStyleId=None)
                    for slot in ('MainHand','Chest','Head','Legs','Ring','Necklace','Relic')])) for i in range(1,11)])))
        self.parents=[]
        for c in range(3):
            cell=copy.deepcopy(self.original[c]);cell.update(id=f'full-{c}',gear='restorer-specialization')
            for member in cell['scenario']['party']:
                if member['partySlot'] in (2,7):
                    for item in member['build']['equipment']:
                        if item['slot'] in h.SLOTS:item['definitionId']='item.spec.restorer.'+item['slot']
            self.original.append(cell);self.parents.append(cell['id'])
        for i in range(165):
            cell=copy.deepcopy(self.original[i%9]);cell.update(id=f'control-{i}',gear=f'control-{i}');cell['scenario']['id']=f'control-scenario-{i}'
            if i>=110:
                for m in cell['scenario']['party']:m['build']['equipment'][0]['definitionId']='item.spec.control'
            self.original.append(cell)
        variants=h.derive(self.original,self.parents)
        self.p=dict(version=h.VERSION,status='ProposedNotPrepared',floor=8,newFights=0,newSeeds=0,nativePreparations=0,
            retainedCells=177,newVariants=6,totalCells=183,actualCompositions=9,eligibleRecipes=125,equipmentEligibility=copy.deepcopy(h.EQUIPMENT),
            variants=variants,cells=self.original+[v['cell'] for v in variants])

    def validate(self):return h.validate(self.p,self.original,self.parents)

    def test_complete_family_retains_raw_controls(self):
        before=copy.deepcopy(self.original);result=self.validate()
        self.assertEqual(before,self.original);self.assertEqual(result[:177],before);self.assertEqual(183,len(result))

    def test_each_variant_uses_one_saved_healer_and_exactly_six_items(self):
        for v in self.validate()[177:]:
            specialized=h.specialized(v);self.assertEqual(6,len(specialized));self.assertEqual(1,len(set(specialized)))
        self.assertEqual([2,7]*3,[v['restorationPartySlot'] for v in self.p['variants']])

    def test_dropped_reordered_extra_or_duplicate_controls_fail(self):
        for kind in ('drop','reorder','extra','duplicate'):
            p=copy.deepcopy(self.p)
            if kind=='drop':p['cells'].pop(0)
            if kind=='reorder':p['cells'][0],p['cells'][1]=p['cells'][1],p['cells'][0]
            if kind=='extra':p['cells'].append(copy.deepcopy(p['cells'][0]))
            if kind=='duplicate':p['cells'][1]=copy.deepcopy(p['cells'][0])
            with self.subTest(kind=kind),self.assertRaises(ValueError):h.validate(p,self.original,self.parents)

    def test_essence_order_native_identity_and_progression_cannot_change(self):
        for key,value in [('essenceIds',['changed']),('identityEssenceIds',['changed']),('id','changed'),('rank',5),('characterLevel',41),('tier',2),('quality','Masterpiece')]:
            p=copy.deepcopy(self.p);p['variants'][0]['cell']['scenario']['party'][1]['build'][key]=value
            with self.subTest(key=key),self.assertRaises(ValueError):h.validate(p,self.original,self.parents)

    def test_unchosen_character_or_party_position_change_rejected(self):
        for change in ('equipment','partySlot'):
            p=copy.deepcopy(self.p);member=p['variants'][0]['cell']['scenario']['party'][0]
            if change=='equipment':member['build']['equipment'][0]['definitionId']='new'
            else:member['partySlot']=2
            with self.assertRaises(ValueError):h.validate(p,self.original,self.parents)

    def test_missing_repeated_or_wrong_healer_variant_rejected(self):
        for change in ('missing','duplicate','slot'):
            p=copy.deepcopy(self.p)
            if change=='missing':p['variants'].pop()
            if change=='duplicate':p['variants'][1]=copy.deepcopy(p['variants'][0])
            if change=='slot':p['variants'][0]['restorationPartySlot']=1
            with self.assertRaises(ValueError):h.validate(p,self.original,self.parents)

    def test_cannot_weaken_equipment_contract_or_change_counts(self):
        for delta in ({'equipmentEligibility':{**h.EQUIPMENT,'maximumSpecializedItems':12}},{'eligibleRecipes':126},{'retainedCells':176},{'newVariants':4}):
            with self.assertRaises(ValueError):h.validate({**self.p,**delta},self.original,self.parents)

    def test_seeded_or_already_run_proposal_rejected(self):
        for key,value in [('newSeeds',1),('newFights',1),('nativePreparations',1),('status','Complete'),('floor',7),('version','other')]:
            with self.assertRaises(ValueError):h.validate({**self.p,key:value},self.original,self.parents)
        original=copy.deepcopy(self.original);original[0]['scenario']['seeds']=[1]
        with self.assertRaises(ValueError):h.validate(self.p,original,self.parents)

    def test_parent_changes_beyond_healer_equipment_rejected(self):
        for change in ('essences','extra-piece','non-healer'):
            original=copy.deepcopy(self.original);full=next(c for c in original if c['id']==self.parents[0])
            if change=='essences':full['scenario']['party'][1]['build']['essenceIds'].reverse()
            if change=='extra-piece':full['scenario']['party'][1]['build']['equipment'][3]['definitionId']='item.spec.legs'
            if change=='non-healer':full['scenario']['party'][0]['build']['equipment'][0]['definitionId']='other'
            with self.assertRaises(ValueError):h.derive(original,self.parents)

    def test_three_actual_original_compositions_required(self):
        for parents in (self.parents[:2],[self.parents[0]]*3,[*self.parents[:2],'missing']):
            with self.assertRaises(ValueError):h.derive(self.original,parents)
        original=copy.deepcopy(self.original);full=next(c for c in original if c['id']==self.parents[1]);full['scenario']=copy.deepcopy(next(c for c in original if c['id']==self.parents[0])['scenario'])
        with self.assertRaises(ValueError):h.derive(original,self.parents)

    def test_preparation_only_no_combined_candidate_or_other_modification(self):
        h.validate_mode('prepare',8,Path('source'),[False,None,[],False])
        for mode in ('screen','search','confirm'):
            with self.assertRaises(ValueError):h.validate_mode(mode,8,Path('source'),[])
        for index in range(14):
            modifications=[False]*14;modifications[index]=True
            with self.assertRaises(ValueError):h.validate_mode('prepare',8,Path('source'),modifications)
        with self.assertRaises(ValueError):h.validate_mode('prepare',7,Path('source'),[])
        with self.assertRaises(ValueError):h.validate_mode('prepare',8,None,[])

    def provenance(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);root=Path(temp.name);source=root/'source';source.mkdir()
        def write(p,v):p.write_text(json.dumps(v),encoding='utf-8')
        write(source/'cells.json',self.original);write(source/'files.json',{})
        evidence=root/'evidence.json';write(evidence,dict(status='Verified',trialStatus='SharedPenetrationScreenNotAccepted'))
        review=root/'review.json';write(review,dict(status='VerifiedSavedReportReview',evidenceSha256=h.sha(evidence),newFights=0,newSeeds=0,usedForAcceptance=False,
            comparisons=[dict(fullId=p,fullWins=w) for p,w in zip(self.parents,[48,33,27])]))
        self.p.update(source=str(source),sourceManifestSha256=h.sha(source/'files.json'),sourceCellsSha256=h.sha(source/'cells.json'),
            precedingEvidence=str(evidence),precedingEvidenceSha256=h.sha(evidence),review=str(review),reviewSha256=h.sha(review))
        path=root/'proposal.json';write(path,self.p);return source,path,write

    def test_authenticated_closed_provenance_admits_exact_family(self):
        source,path,_=self.provenance();self.assertEqual(self.p['cells'],h.admit(path,source))

    def test_changed_source_evidence_or_review_rejected(self):
        source,path,write=self.provenance()
        for field in ('sourceManifestSha256','sourceCellsSha256','precedingEvidenceSha256','reviewSha256'):
            write(path,{**self.p,field:'0'*64})
            with self.assertRaises(ValueError):h.admit(path,source)

    def test_parent_selection_cannot_be_silently_replaced(self):
        source,path,write=self.provenance();review=Path(self.p['review']);data=h.read(review);data['comparisons'][0]['fullWins']=47
        write(review,data);self.p['reviewSha256']=h.sha(review);write(path,self.p)
        with self.assertRaises(ValueError):h.admit(path,source)

if __name__=='__main__':unittest.main()
