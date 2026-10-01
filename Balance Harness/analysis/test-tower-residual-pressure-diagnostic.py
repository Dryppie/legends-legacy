"""Guard the archived candidate selection, event-order boundary and native Health snapshots."""
import copy
import importlib.util
from pathlib import Path
import unittest

def module(name,filename):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(filename));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
d=module('residual_diagnostic','diagnose-tower-residual-pressure.py')
fixtures=module('residual_fixtures','test-tower-resistance-diagnostic.py')


class ResidualPressureTests(unittest.TestCase):
    def family(self):
        plan,cells,seeds=fixtures.ResistanceDiagnosticTests().family()
        plan['version']='tower-floor7-residual-pressure-diagnostic-v1'
        for group,label in zip(plan['cases'],('A','B'),strict=True):
            group['label']=label
            for case,profile,count,slots in zip(group['cases'],d.PROFILES,(0,6,15),([], [2], [1,2,3,4,5]),strict=True):
                cell=next(c for c in cells if c['id']==case['id']);cell['gear']=profile
                for actor in cell['scenario']['party']:
                    actor['build']['equipment']=[dict(slot=str(i),definitionId='plain'+('.spec.test' if actor['partySlot'] in slots and i<(6 if count==6 else 3) else '')) for i in range(6)]
                case.update(gear=profile,specializedCharacters=len(slots),specializedItems=count,specializedPartySlots=slots)
        return plan,cells,seeds

    def test_frozen_selection_keeps_all_inputs(self):
        p,c,s=self.family();before=copy.deepcopy((p,c,s));chosen=d.validate_selection(p,c,s)
        self.assertEqual(6,len(chosen));self.assertEqual(before,(p,c,s))

    def test_diagnostic_cannot_allocate_or_become_acceptance(self):
        for field,value in [('newSeeds',1),('newStudyFights',1),('allocatedReplays',1),('maximumHistoricalReplays',97),('gameplayCandidateSelected',True)]:
            with self.subTest(field=field):
                p,c,s=self.family();p[field]=value
                with self.assertRaisesRegex(ValueError,'unallocated descriptive'):d.validate_selection(p,c,s)

    def test_family_and_seed_order_are_required(self):
        for mutation in ('drop','floor','duplicate','reverse','later'):
            with self.subTest(mutation=mutation):
                p,c,s=self.family()
                if mutation=='drop':c.pop()
                elif mutation=='floor':c[-1]['scenario']['floorNumber']=4
                elif mutation=='duplicate':s[-1]=s[0]
                elif mutation=='reverse':p['seeds'].reverse()
                else:p['seeds']=s[1:17]
                with self.assertRaisesRegex(ValueError,'original family|sixteen declared'):d.validate_selection(p,c,s)

    def test_resistance_cannot_replace_selected_restorer_profile(self):
        p,c,s=self.family();p['cases'][0]['cases'][1]['gear']='resistance-and-health'
        with self.assertRaisesRegex(ValueError,'three equipment profiles'):d.validate_selection(p,c,s)

    def test_actual_equipment_cannot_be_hidden_by_declared_count(self):
        p,c,s=self.family();c[1]['scenario']['party'][0]['build']['equipment'][0]['definitionId']='hidden.spec.test'
        with self.assertRaisesRegex(ValueError,'equipment counts'):d.validate_selection(p,c,s)

    def test_unselected_equipment_and_raw_build_changes_rejected(self):
        for change in ('equipment','order','identity','level'):
            with self.subTest(change=change):
                p,c,s=self.family();actor=c[1]['scenario']['party'][0]['build']
                if change=='equipment':actor['equipment'][0]['definitionId']='different-plain'
                elif change=='order':actor['essenceIds'].reverse()
                elif change=='identity':actor['id']='renamed'
                else:actor['characterLevel']+=1
                with self.assertRaisesRegex(ValueError,'Unselected equipment|Raw identity'):d.validate_selection(p,c,s)

    def reports(self):
        saved,replay,scenario,event=fixtures.ResistanceDiagnosticTests().reports()
        for e in replay['battle']['eventLog']:
            if e['source']==d.resistance.SPRINGTIDE:e['combatEntity']=dict(id=e['targetId'],health=84,maxHealth=100)
        return saved,replay,scenario,event

    def test_no_death_keeps_all_events_before_boundary_and_null_empty_rate(self):
        saved,replay,scenario,_=self.reports();r=d.analyze(saved,replay,scenario)['residual']
        self.assertIsNone(r['firstDeathEventIndex']);self.assertEqual(5,r['windows']['beforeFirstDeathEvent']['guardian']['finalHealthDamage'])
        self.assertEqual(0,r['windows']['fromFirstDeathEvent']['guardian']['finalHealthDamage'])
        self.assertIsNone(r['windows']['fromFirstDeathEvent']['guardianDamagePerSecond'])

    def test_death_partition_preserves_same_tick_order(self):
        saved,replay,scenario,event=self.reports();es=replay['battle']['eventLog']
        outgoing=es.pop(2);outgoing['timestamp']=120;es.insert(2,outgoing)
        es.insert(3,event('0',120,kind='Death'));es.insert(4,event('g',120,incomingRawDamage=2,finalHealthDamage=2))
        for r in (saved,replay):
            r['battle']['summary']['statistics'][0]['firstDeathTick']=120
            r['battle']['summary']['statistics'][-1].update(incomingRawDamage=7,finalHealthDamage=7,health=94)
        r=d.analyze(saved,replay,scenario)['residual']
        self.assertEqual(3,r['firstDeathEventIndex']);self.assertEqual(120,r['firstDeathTick'])
        self.assertEqual(5,r['windows']['beforeFirstDeathEvent']['guardian']['finalHealthDamage'])
        self.assertEqual(2,r['windows']['fromFirstDeathEvent']['guardian']['finalHealthDamage'])
        self.assertEqual([False,True],[h['atOrAfterFirstDeathEvent'] for h in r['springtideHealthSnapshots']])

    def test_native_health_snapshot_not_reconstructed_from_damage(self):
        saved,replay,scenario,_=self.reports();r=d.analyze(saved,replay,scenario)['residual']
        self.assertEqual([.84,.84],[h['healthFractionAfter'] for h in r['springtideHealthSnapshots']])
        self.assertEqual([.08,.08],[h['healthDamageFraction'] for h in r['springtideHealthSnapshots']])
        self.assertIn('No reconstructed pre-hit',r['snapshotMeaning'])

    def test_missing_wrong_target_or_invalid_native_snapshot_rejected(self):
        for snapshot in (None,dict(id='wrong',health=84,maxHealth=100),dict(id='0',health=84,maxHealth=0)):
            saved,replay,scenario,_=self.reports();replay['battle']['eventLog'][1]['combatEntity']=snapshot
            with self.assertRaisesRegex(ValueError,'Native target Health snapshot'):d.analyze(saved,replay,scenario)

    def test_springtide_death_notification_is_not_an_extra_hit(self):
        saved,replay,scenario,event=self.reports()
        replay['battle']['eventLog'].insert(4,event('0',400,d.resistance.SPRINGTIDE,kind='Death'))
        for report in (saved,replay):report['battle']['summary']['statistics'][0]['firstDeathTick']=400
        snapshots=d.analyze(saved,replay,scenario)['residual']['springtideHealthSnapshots']
        self.assertEqual(2,len(snapshots));self.assertEqual([120,400],[h['tick'] for h in snapshots])


if __name__=='__main__':unittest.main()
