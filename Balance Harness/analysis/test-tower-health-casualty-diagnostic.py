"""Protect the fixed replay scope and condition/death event ordering."""
import copy
import importlib.util
from pathlib import Path
import unittest

def module(name,file):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
d=module('casualty_tests','diagnose-tower-health-casualties.py')
fixtures=module('casualty_fixtures','test-tower-residual-pressure-diagnostic.py')

class HealthCasualtyTests(unittest.TestCase):
    def family(self):
        p,c,s=fixtures.ResidualPressureTests().family();cases=[]
        for i,control in enumerate(c[6:]):control['composition']='control-'+str(i%6)
        for g in p['cases']:
            for case in g['cases'][1:]:cases.append(dict(label=g['label'],composition=g['composition'],cellId=case['id'],gear=case['gear'],specializedItems=case['specializedItems'],specializedPartySlots=case['specializedPartySlots']))
        p.update(version='tower-floor7-health-springtide-diagnostic-v1',maximumHistoricalReplays=64,cases=cases)
        return p,c,s

    def test_fixed_selection_is_read_only(self):
        p,c,s=self.family();before=copy.deepcopy((p,c,s));self.assertEqual(4,len(d.validate_selection(p,c,s)));self.assertEqual(before,(p,c,s))

    def test_allocations_and_scope_extensions_rejected(self):
        for field,value in [('newSeeds',1),('newStudyFights',1),('allocatedReplays',1),('maximumHistoricalReplays',65),('gameplayCandidateSelected',True)]:
            with self.subTest(field=field):
                p,c,s=self.family();p[field]=value
                with self.assertRaisesRegex(ValueError,'unallocated descriptive'):d.validate_selection(p,c,s)

    def test_missing_duplicate_wrong_floor_and_wrong_compositions_rejected(self):
        for change in ('missing','duplicate','floor','composition'):
            with self.subTest(change=change):
                p,c,s=self.family()
                if change=='missing':c.pop()
                elif change=='duplicate':c[-1]['id']=c[-2]['id']
                elif change=='floor':c[-1]['scenario']['floorNumber']=4
                else:c[-1]['scenario']['party'][0]['build']['essenceIds']=['unexpected']
                with self.assertRaises(ValueError):d.validate_selection(p,c,s)

    def test_seed_substitution_and_reordering_rejected(self):
        for change in ('later','reverse','duplicate'):
            p,c,s=self.family()
            if change=='later':p['seeds']=s[1:17]
            elif change=='reverse':p['seeds'].reverse()
            else:s[-1]=s[0]
            with self.assertRaisesRegex(ValueError,'sixteen declared'):d.validate_selection(p,c,s)

    def test_profile_substitution_and_duplicate_labels_rejected(self):
        for change in ('profile','label','count'):
            p,c,s=self.family()
            if change=='profile':p['cases'][0]['gear']='baseline'
            elif change=='label':p['cases'][2]['label']='A'
            else:p['cases'].pop()
            with self.assertRaises(ValueError):d.validate_selection(p,c,s)

    def test_equipment_cannot_be_hidden_by_counts(self):
        p,c,s=self.family();c[1]['scenario']['party'][0]['build']['equipment'][0]['definitionId']='hidden.spec.test'
        with self.assertRaisesRegex(ValueError,'equipment counts'):d.validate_selection(p,c,s)

    def test_ambiguous_baseline_is_rejected(self):
        p,c,s=self.family();c[-1]['composition']='A'
        with self.assertRaisesRegex(ValueError,'Unique baseline'):d.validate_selection(p,c,s)

    def test_raw_identity_essence_order_and_budget_preserved(self):
        for field,value in [('id','changed'),('essenceIds',['raw-order','A']),('characterLevel',41)]:
            p,c,s=self.family();c[1]['scenario']['party'][0]['build'][field]=value
            with self.assertRaises(ValueError):d.validate_selection(p,c,s)

    def events(self):
        _,r,_,event=fixtures.ResidualPressureTests().reports()
        return r['battle']['eventLog'],event

    def evaluate(self,events):return d.event_metrics(events,{str(i):i+1 for i in range(5)},'g',55,10)

    def test_no_death_uses_all_output_and_no_death_cause(self):
        es,_=self.events();r=self.evaluate(es)
        self.assertIsNone(r['slot1Death']);self.assertEqual([],r['firstDeathSlots'])
        self.assertEqual(5,r['windows']['beforeSlot1DeathEvent']['guardian']['finalHealthDamage'])
        self.assertEqual(0,r['windows']['fromSlot1DeathEvent']['exposureSeconds'])

    def test_all_tied_first_death_slots_retained(self):
        es,event=self.events();es.extend([event('0',480,kind='Death'),event('3',480,kind='Death'),event('1',490,kind='Death')])
        r=self.evaluate(es);self.assertEqual([1,4],r['firstDeathSlots']);self.assertTrue(r['slot1Death']['isEarliest'])

    def test_same_tick_death_boundary_preserves_output_order(self):
        es,event=self.events();es=[event('0',480,finalHealthDamage=2),event('g',480,finalHealthDamage=3),event('0',480,kind='Death'),event('g',480,finalHealthDamage=7)]
        r=self.evaluate(es);self.assertEqual(3,r['windows']['beforeSlot1DeathEvent']['guardian']['finalHealthDamage']);self.assertEqual(7,r['windows']['fromSlot1DeathEvent']['guardian']['finalHealthDamage'])
        self.assertEqual(0,r['slot1Death']['precedingDamageIndex']);self.assertEqual(0,r['before48']['party']['finalHealthDamage'])

    def test_resisted_application_does_not_establish_condition(self):
        es,event=self.events();es.insert(0,event('g',0,'condition.weaken','StatusEffectResisted',1))
        r=self.evaluate(es);self.assertIsNone(r['springtideConditionHistory'][0]['guardianNotifications']['condition.weaken'])
        self.assertEqual(1,len(r['conditionEvents']))

    def test_same_tick_application_and_removal_order_is_preserved(self):
        es,event=self.events();hit=copy.deepcopy(es[1]);hit['timestamp']=120
        es=[event('g',120,'condition.weaken','StatusEffect',1),hit,event('g',120,'condition.weaken','StatusEffectExpired'),copy.deepcopy(hit)]
        rows=self.evaluate(es)['springtideConditionHistory']
        self.assertEqual(['StatusEffect','StatusEffectExpired'],[r['guardianNotifications']['condition.weaken']['eventType'] for r in rows])

    def test_every_explicit_removal_is_recorded_without_guessing_active_stacks(self):
        for kind in d.REMOVALS:
            es,event=self.events();es.insert(0,event('g',0,'condition.weaken',kind))
            r=self.evaluate(es);self.assertEqual(kind,r['springtideConditionHistory'][0]['guardianNotifications']['condition.weaken']['eventType'])
            self.assertIn('Not a native active-condition',r['conditionMeaning'])

    def test_party_condition_is_not_attributed_to_guardian(self):
        es,event=self.events();es.insert(0,event('0',0,'condition.weaken','StatusEffect',1))
        r=self.evaluate(es);self.assertEqual('1',r['conditionEvents'][0]['target']);self.assertIsNone(r['springtideConditionHistory'][0]['guardianNotifications']['condition.weaken'])

    def test_death_notification_cannot_add_a_springtide_hit(self):
        es,event=self.events();es.append(event('0',400,d.resistance.SPRINGTIDE,'Death'))
        self.assertEqual(2,len(self.evaluate(es)['springtideConditionHistory']))

if __name__=='__main__':unittest.main()
