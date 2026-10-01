import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

s=importlib.util.spec_from_file_location('ni_calibration',Path(__file__).with_name('tower-ni-offense-calibration.py'))
m=importlib.util.module_from_spec(s);s.loader.exec_module(m)

class CalibrationTests(unittest.TestCase):
    def setUp(self):
        self.cells=[dict(id=str(i),scenario=dict(party=[dict(partySlot=1,build=dict(essenceIds=[str(i),'shared'],equipment=[]))])) for i in range(3)]
        self.panels=[dict(factor=f,rows=[dict(id=str(i),wins=w,samples=32) for i,w in enumerate(wins)])
                     for f,wins in zip(m.FACTORS,([28,20,32],[8,6,12],[7,6,10]))]

    def test_lowest_promising_setting_never_claims_acceptance(self):
        r=m.choose(self.cells,self.panels)
        self.assertEqual(1.5,r['selectedFactor']);self.assertTrue(r['diagnosticOnly']);self.assertFalse(r['usedForAcceptance'])

    def test_full_equipment_control_can_prevent_selection(self):
        for p in self.panels:p['rows'][2]['wins']=13
        self.assertIsNone(m.choose(self.cells,self.panels)['selectedFactor'])

    def test_recipe_aliases_do_not_count_as_distinct_compositions(self):
        self.cells[1]['scenario']=copy.deepcopy(self.cells[0]['scenario'])
        self.cells[2]['scenario']['party'][0]['build']['equipment']=[dict(definitionId='item.spec.'+str(i)) for i in range(9)]
        self.assertIsNone(m.choose(self.cells,self.panels)['selectedFactor'])

    def test_equipment_is_measured_not_inferred_from_labels(self):
        self.cells[1]['scenario']['party'][0]['build']['equipment']=[dict(definitionId='item.spec.'+str(i)) for i in range(9)]
        self.cells[2]['scenario']['party'][0]['build']['equipment']=[dict(definitionId='item.spec.'+str(i)) for i in range(9)]
        self.assertIsNone(m.choose(self.cells,self.panels)['selectedFactor'])

    def test_incomplete_grid_cannot_select(self):
        with self.assertRaises(ValueError):m.choose(self.cells,self.panels[:2])

    def test_missing_reordered_or_invalid_rows_cannot_select(self):
        for change in ('missing','order','wins','samples'):
            p=copy.deepcopy(self.panels)
            if change=='missing':p[1]['rows'].pop()
            if change=='order':p[1]['rows'].reverse()
            if change=='wins':p[1]['rows'][0]['wins']=33
            if change=='samples':p[1]['rows'][0]['samples']=16
            with self.subTest(change=change),self.assertRaises(ValueError):m.choose(self.cells,p)

    def test_only_frozen_factors_allowed(self):
        for value in (True,1,1.4,float('nan'),float('inf'),'1.5'):
            with self.assertRaises(ValueError):m.expected_tower(Path('unused'),value)

    def test_candidate_changes_only_offense_and_preserves_other_catalog_bytes(self):
        with tempfile.TemporaryDirectory() as temp:
            source=Path(temp)/'source';candidate=Path(temp)/'candidate'
            (source/'Data/world-tower').mkdir(parents=True)
            original=dict(floors=[dict(floorNumber=9,guardianScaling=dict(health=2.970703125,offense=4.7036132812,defense=2.55)),dict(floorNumber=10,guardianScaling=dict(health=1,offense=1))])
            (source/'Data'/m.TOWER).write_text(json.dumps(original));(source/'Data/other.json').write_text('{}');(source/'appsettings.json').write_text('{}')
            shutil.copytree(source,candidate)
            expected=m.expected_tower(source,1.5);(candidate/'Data'/m.TOWER).write_text(json.dumps(expected))
            m.verify_candidate(source,candidate,1.5)
            for key in ('health','defense'):
                changed=copy.deepcopy(expected);changed['floors'][0]['guardianScaling'][key]=999
                (candidate/'Data'/m.TOWER).write_text(json.dumps(changed))
                with self.assertRaises(ValueError):m.verify_candidate(source,candidate,1.5)
            (candidate/'Data'/m.TOWER).write_text(json.dumps(expected));(candidate/'Data/other.json').write_text('{ }')
            with self.assertRaises(ValueError):m.verify_candidate(source,candidate,1.5)
            with self.assertRaises(ValueError):m.verify_candidate(source,source,1.5)

if __name__=='__main__':unittest.main()
