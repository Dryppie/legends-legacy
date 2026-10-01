"""Guard the full diagnostic grid, isolated candidates and descriptive selection."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from types import SimpleNamespace
from unittest.mock import patch

HERE=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('ni_grid',HERE/'tower-ni-restoration-offense-diagnostic.py')
m=importlib.util.module_from_spec(s);s.loader.exec_module(m);n=m.n
s=importlib.util.spec_from_file_location('ni_grid_owner',HERE/'run-tower-balance-pass.py')
io=importlib.util.module_from_spec(s);s.loader.exec_module(io)

class NiGridTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.source=Path(self.temp.name)/'source';self.candidate=Path(self.temp.name)/'candidate'
        api=io.ROOT/'LL/src/API/API.LL'
        shutil.copytree(api/'Data',self.source/'Data');shutil.copy2(api/'appsettings.json',self.source/'appsettings.json')
        # Model the frozen pre-candidate catalog explicitly, independent of later live tuning.
        tower=n.h.read(self.source/n.TOWER)
        n.h.unique(tower['floors'],'floorNumber',9)['guardianScaling'].update(health=2.970703125,offense=4.7036132812,penetration=1)
        (self.source/n.TOWER).write_text(json.dumps(tower,indent=2)+'\n',encoding='utf-8')
        shutil.copytree(self.source,self.candidate)
        self.plan={**n.VALUES,**{k:n.h.sha(self.source/v) for k,v in n.HASHES.items()}}

    def settings(self):
        return [dict(offenseFactor=f,rows=[dict(id=str(i),composition=str(i%5),eligible=i<130,wins=6 if i<2 else 0,samples=32) for i in range(163)]) for f in n.OFFENSE]

    def test_all_three_explicit_dispatches_match(self):
        for factor,offense in n.OFFENSE.items():
            plan={**self.plan,'offenseFactor':factor,'offense':offense}
            shutil.copy2(self.source/n.TOWER,self.candidate/n.TOWER)
            io.ability_module().validate(self.source,plan,9)
            result=io.ability_module().materialize(self.source,self.candidate,plan,9)
            self.assertEqual(result,io.ability_module().verify(self.source,self.candidate,plan,9))
            tower=n.h.read(self.candidate/n.TOWER);scaling=n.h.unique(tower['floors'],'floorNumber',9)['guardianScaling']
            self.assertEqual(offense,scaling['offense']);self.assertEqual(40,scaling['penetration'])
            scaling['offense']=4.7036132812;scaling['penetration']=1
            self.assertEqual(n.h.read(self.source/n.TOWER),tower)

    def test_changed_or_incomplete_plans_rejected(self):
        for key,value in [('floor',True),('offenseFactor',.95),('offenseFactor',True),('offense',4),('healthFactor',True),('penetrationFactor',39),('sourceSummonsSha256','bad'),('extra',1)]:
            with self.assertRaises(ValueError):n.validate_plan({**self.plan,key:value},9)
        for key in self.plan:
            plan=copy.deepcopy(self.plan);del plan[key]
            with self.assertRaises(ValueError):n.validate_plan(plan,9)

    def test_source_hash_and_isolation_required(self):
        with self.assertRaises(ValueError):n.materialize(self.source,self.source,self.plan,9)
        (self.source/n.SUMMONS).write_text('[]')
        with self.assertRaises(ValueError):n.expected(self.source,self.plan,9)

    def test_unrelated_tower_fields_guarded(self):
        n.materialize(self.source,self.candidate,self.plan,9);p=self.candidate/n.TOWER;raw=p.read_bytes()
        for floor,key,value in [(9,'health',1),(8,'offense',.2),(9,'penetration',39)]:
            data=n.h.read(p);n.h.unique(data['floors'],'floorNumber',floor)['guardianScaling'][key]=value;p.write_text(json.dumps(data))
            with self.assertRaises(ValueError):n.verify(self.source,self.candidate,self.plan,9)
            p.write_bytes(raw)

    def test_settings_and_other_catalogs_guarded(self):
        n.materialize(self.source,self.candidate,self.plan,9)
        for p in (self.candidate/'appsettings.json',self.candidate/n.SUMMONS,self.candidate/n.ABILITIES,self.candidate/'Data/unexpected.json'):
            raw=p.read_bytes() if p.exists() else None;p.write_text('{}')
            with self.assertRaises(ValueError):n.verify(self.source,self.candidate,self.plan,9)
            if raw is None:p.unlink()
            else:p.write_bytes(raw)

    def test_old_candidate_still_rejects_new_grid(self):
        s=importlib.util.spec_from_file_location('old_ni',HERE/'tower-ni-penetration.py');old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
        with self.assertRaises(ValueError):old.validate_plan(self.plan,9)

    def test_exact_non_acceptance_panel(self):
        m.validate_panel('screen',9,16,m.DIAGNOSTIC,163)
        for args in [('confirm',9,16,m.DIAGNOSTIC,163),('search',9,16,m.DIAGNOSTIC,163),('screen',8,16,m.DIAGNOSTIC,163),('screen',9,32,m.DIAGNOSTIC,163),('screen',9,16,None,163),('screen',9,16,m.DIAGNOSTIC,162)]:
            with self.assertRaises(ValueError):m.validate_panel(*args)

    def test_exact_batch_order(self):
        d={'variants':[1,2,3]}
        self.assertEqual([1,1,2,2,3,3],[m.batch_variant(d,i) for i in range(6)])
        for i in (-1,6,True,1.0):
            with self.assertRaises(ValueError):m.batch_variant(d,i)

    def test_mixed_admission_rejected_before_allocation(self):
        for candidate,modifications,seconds in [(None,[],840),('candidate',[True],840),('candidate',[],900)]:
            with self.assertRaises(ValueError):m.admit_batch(SimpleNamespace(),None,None,0,None,None,'screen',9,16,candidate,modifications,seconds)

    def test_missing_diagnostic_provenance_rejected(self):
        with self.assertRaises(ValueError):m.audit_request(SimpleNamespace(),Path('study'),dict(inputHashes={},diagnosticVersion=m.DIAGNOSTIC))

    def test_lowest_qualifying_offense_selected(self):
        self.assertEqual(1,m.select(self.settings()))

    def test_any_overpowered_control_rejects_setting(self):
        s=self.settings();s[0]['rows'][162]['wins']=13
        self.assertEqual(1.05,m.select(s))

    def test_two_actual_eligible_compositions_required(self):
        s=self.settings()
        for setting in s:setting['rows'][1]['wins']=5
        self.assertIsNone(m.select(s))

    def test_equipment_ineligible_route_does_not_qualify(self):
        s=self.settings()
        for setting in s:setting['rows'][1]['wins']=0;setting['rows'][131]['wins']=6
        self.assertIsNone(m.select(s))

    def test_multiple_recipes_same_composition_count_once(self):
        s=self.settings()
        for setting in s:setting['rows'][1]['wins']=0;setting['rows'][5]['wins']=6
        self.assertIsNone(m.select(s))

    def test_interim_selection_rejected(self):
        with self.assertRaises(ValueError):m.select(self.settings()[:2])

    def test_dropped_reordered_or_incomplete_rows_rejected(self):
        for change in ('drop','reorder','samples','eligibility'):
            s=self.settings()
            if change=='drop':s[2]['rows'].pop()
            if change=='reorder':s[2]['rows'].reverse()
            if change=='samples':s[2]['rows'][0]['samples']=16
            if change=='eligibility':s[2]['rows'][0]['eligible']=False
            with self.assertRaises(ValueError):m.select(s)

    def test_empty_qualifying_set_is_not_a_winner(self):
        s=self.settings()
        for setting in s:setting['rows'][162]['wins']=24
        self.assertIsNone(m.select(s))

if __name__=='__main__':unittest.main()
