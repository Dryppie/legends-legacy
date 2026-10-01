"""Keep the diagnostic descriptive, paired, complete and faithful to native events."""
import copy
import importlib.util
from pathlib import Path
import unittest

s=importlib.util.spec_from_file_location('resistance_diagnostic',Path(__file__).with_name('diagnose-tower-resistance.py'))
d=importlib.util.module_from_spec(s);s.loader.exec_module(d)


class ResistanceDiagnosticTests(unittest.TestCase):
    def family(self):
        cells=[];groups=[]
        for label in ('A','B'):
            base=dict(id=label+'/baseline',composition=label,gear='baseline',scenario=dict(id=label,floorNumber=7,seeds=[],party=[dict(partySlot=i,build=dict(
                id=label+str(i),characterLevel=40,essenceIds=[label,'raw-order'],equipment=[dict(slot=slot,definitionId='plain') for slot in ('Chest','Head','Legs','Necklace')])) for i in range(1,6)]))
            cases=[]
            for count,gear in ((0,'baseline'),(2,'mixed-resistance'),(5,'resistance-and-health')):
                c=copy.deepcopy(base);c.update(id=label+'/'+gear,gear=gear)
                for member in c['scenario']['party'][:count]:
                    for item in member['build']['equipment']:item['definitionId']='plain.spec.resistance'
                cells.append(c);cases.append(dict(id=c['id'],gear=gear,specializedCharacters=count,resistancePartySlots=list(range(1,count+1))))
            groups.append(dict(composition=label,cases=cases))
        for i in range(114):
            c=copy.deepcopy(cells[0]);c['id']='other'+str(i);c['scenario']['id']=c['id']
            c['scenario']['party'][0]['build']['essenceIds']=['extra'+str(i%6)]
            cells.append(c)
        seeds=list(range(128));plan=dict(version='tower-floor7-pressure-diagnostic-proposal-v1',status='ProposedNotAllocated',maximumHistoricalReplays=96,
            newStudyFights=0,newSeeds=0,allocatedReplays=0,firstWindowSeconds=40,requireCompleteSavedReportParityAfterRemovingEventLog=True,
            gameplayCandidateSelected=False,familySize=120,actualCompositions=8,seeds=seeds[:16],cases=groups)
        return plan,cells,seeds

    def test_exact_selection_keeps_raw_inputs_and_frozen_seed_order(self):
        p,c,s=self.family();before=copy.deepcopy((p,c,s));chosen=d.validate_selection(p,c,s)
        self.assertEqual(6,len(chosen));self.assertEqual(before,(p,c,s))

    def test_selection_cannot_allocate_or_become_acceptance_candidate(self):
        for field,value in [('newSeeds',1),('newStudyFights',1),('allocatedReplays',1),('gameplayCandidateSelected',True),('maximumHistoricalReplays',97)]:
            with self.subTest(field=field):
                p,c,s=self.family();p[field]=value
                with self.assertRaisesRegex(ValueError,'unallocated descriptive'):d.validate_selection(p,c,s)

    def test_missing_family_member_or_wrong_floor_rejected(self):
        for change in ('missing','floor'):
            p,c,s=self.family()
            if change=='missing':c.pop()
            else:c[0]['scenario']['floorNumber']=4
            with self.assertRaisesRegex(ValueError,'Complete original family'):d.validate_selection(p,c,s)

    def test_repeated_reordered_or_outcome_selected_seeds_rejected(self):
        for change in ('repeated','reordered','later'):
            p,c,s=self.family()
            if change=='repeated':s[-1]=s[0]
            elif change=='reordered':p['seeds'].reverse()
            else:p['seeds']=s[1:17]
            with self.assertRaisesRegex(ValueError,'sixteen declared seeds'):d.validate_selection(p,c,s)

    def test_extra_or_missing_equipment_profile_rejected(self):
        p,c,s=self.family();p['cases'][0]['cases'].pop()
        with self.assertRaisesRegex(ValueError,'three equipment profiles'):d.validate_selection(p,c,s)

    def test_two_labels_do_not_establish_distinct_actual_compositions(self):
        p,c,s=self.family()
        for cell in c[3:6]:
            for member in cell['scenario']['party']:member['build']['essenceIds']=['raw-order','A']
        with self.assertRaisesRegex(ValueError,'original family|actual compositions'):d.validate_selection(p,c,s)

    def test_identity_essence_order_position_or_budget_change_rejected(self):
        for field,value in [('id','changed'),('essenceIds',['raw-order','A']),('characterLevel',41)]:
            p,c,s=self.family();c[1]['scenario']['party'][0]['build'][field]=value
            with self.assertRaisesRegex(ValueError,'Raw identity|original family'):d.validate_selection(p,c,s)

    def test_unselected_slot_equipment_cannot_change(self):
        p,c,s=self.family();c[1]['scenario']['party'][-1]['build']['equipment'][0]['definitionId']='other'
        with self.assertRaisesRegex(ValueError,'equipment substitution'):d.validate_selection(p,c,s)

    def test_declared_equipment_count_cannot_hide_actual_specialization(self):
        p,c,s=self.family();c[1]['scenario']['party'][-1]['build']['equipment'][0]['definitionId']='other.spec.resistance'
        with self.assertRaisesRegex(ValueError,'saved equipment profile'):d.validate_selection(p,c,s)

    def reports(self):
        def event(target,tick,source='hit',kind='Damage',magnitude=0,**fields):
            return dict(actorId='g' if target!='g' else '0',targetId=target,timestamp=tick,source=source,statsSource=source,eventType=kind,
                damageType='Magical',magnitude=magnitude,details='',**{k:fields.get(k,0) for k in d.mixed.MEASURES})
        stats=[dict(entityId=str(i),entityName=str(i),firstDeathTick=None,**{k:0 for k in d.mixed.base.FIELDS}) for i in range(5)]
        stats[0].update(incomingRawDamage=20,magicalMitigationPrevented=4,finalHealthDamage=16)
        guardian=dict(entityId='g',entityName='g',firstDeathTick=None,maxHealth=100,health=96,**{k:0 for k in d.mixed.base.FIELDS})
        guardian.update(incomingRawDamage=5,finalHealthDamage=5,healingReceived=1);stats.append(guardian)
        report=dict(succeeded=False,guardianHealthRemainingPercent=96,battle=dict(seed=4,ticksPerSecond=10,
            preparedParticipants=[dict(name=str(i),slot=dict(side='Friendly',slotId=str(i)),combatAttributes={'Armor':30}) for i in range(5)]+
                [dict(name='g',slot=dict(side='Hostile',slotId='g'),combatAttributes={'MaxHealth':100})],
            summary=dict(durationSeconds=55,statistics=stats),eventLog=None))
        events=[event('g',100,d.ABUNDANCE,'StatusEffect',1),event('0',120,d.SPRINGTIDE,incomingRawDamage=10,magicalMitigationPrevented=2,finalHealthDamage=8),
                event('g',399,incomingRawDamage=5,finalHealthDamage=5),event('0',400,d.SPRINGTIDE,incomingRawDamage=10,magicalMitigationPrevented=2,finalHealthDamage=8),
                event('g',401,'heal','Heal',1)]
        replay=copy.deepcopy(report);replay['battle']['eventLog']=events
        scenario=dict(party=[dict(partySlot=i+1,build=dict(id=str(i))) for i in range(5)])
        return report,replay,scenario,event

    def test_window_boundary_excludes_exact_40_second_hit(self):
        saved,replay,scenario,_=self.reports();r=d.analyze(saved,replay,scenario)['resistance']
        self.assertEqual(8,r['windows']['before40']['party']['finalHealthDamage'])
        self.assertEqual(8,r['windows']['after40']['party']['finalHealthDamage'])
        self.assertEqual(5,r['windows']['before40']['guardian']['finalHealthDamage'])

    def test_full_saved_result_equality_is_required(self):
        saved,replay,scenario,_=self.reports();replay['succeeded']=True
        with self.assertRaisesRegex(ValueError,'complete saved report'):d.analyze(saved,replay,scenario)

    def test_guardian_event_damage_and_healing_must_reconcile(self):
        for key in ('finalHealthDamage','magnitude'):
            saved,replay,scenario,_=self.reports()
            replay['battle']['eventLog'][2 if key=='finalHealthDamage' else 4][key]+=1
            with self.assertRaisesRegex(ValueError,'Guardian .* totals differ'):d.analyze(saved,replay,scenario)

    def test_health_trajectory_is_labeled_accounting_not_snapshot(self):
        saved,replay,scenario,_=self.reports();r=d.analyze(saved,replay,scenario)['resistance']
        self.assertEqual(96,r['guardianFinalAccountedHealth']);self.assertEqual(0,r['guardianHealthAccountingResidual'])
        self.assertIn('not a native health snapshot',r['healthTrajectoryMeaning'])

    def test_abundance_applications_are_not_claimed_as_exact_stacks(self):
        saved,replay,scenario,event=self.reports();replay['battle']['eventLog'].insert(2,event('g',120,d.ABUNDANCE,'StatusEffect',1))
        r=d.analyze(saved,replay,scenario)['resistance']['springtideHits']
        self.assertEqual([1,2],[h['loggedAbundanceApplicationsBeforeHit'] for h in r])
        self.assertTrue(all(h['exactAbundanceStacks'] is None for h in r))

    def test_pre_death_window_preserves_same_tick_event_order(self):
        saved,replay,scenario,event=self.reports()
        for value in (saved,replay):value['battle']['summary']['statistics'][0]['firstDeathTick']=120
        replay['battle']['eventLog'].insert(2,event('0',120,kind='Death'))
        r=d.analyze(saved,replay,scenario)['resistance']['recipients'][0]
        self.assertEqual(8,r['beforeFirstDeath']['finalHealthDamage']);self.assertEqual(120,r['lastDamageBeforeFirstDeath']['timestamp'])

    def test_condition_events_preserve_application_and_resistance(self):
        saved,replay,scenario,event=self.reports()
        replay['battle']['eventLog'].extend([event('1',450,'condition.slow','StatusEffect',1),event('2',450,'effect.creature.eydis.tranquil_waters.weaken','StatusEffectResisted',1)])
        r=d.analyze(saved,replay,scenario)['resistance']['conditionEvents']
        self.assertEqual(['StatusEffect','StatusEffectResisted'],[e['eventType'] for e in r])


if __name__=='__main__':unittest.main()
