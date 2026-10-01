"""Protect the fixed three-case scope and recovery/death event accounting."""
import copy
import importlib.util
from pathlib import Path
import unittest

def module(name,file):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
d=module('survival_tests','diagnose-tower-survival-output.py')
fixtures=module('survival_fixtures','test-tower-health-casualty-diagnostic.py')

class SurvivalOutputTests(unittest.TestCase):
    def family(self):
        p,c,s=fixtures.HealthCasualtyTests().family()
        haste=p['cases'][1];haste.update(gear='ability-haste',specializedItems=5)
        cell=next(x for x in c if x['id']==haste['cellId']);cell['gear']='ability-haste'
        for actor in cell['scenario']['party']:
            actor['build']['equipment']=[dict(slot=str(i),definitionId='plain'+('.spec.test' if i==0 else '')) for i in range(6)]
        p.update(version='tower-floor7-survival-output-diagnostic-v1',maximumHistoricalReplays=48,cases=[haste,p['cases'][0],p['cases'][2]])
        return p,c,s

    def test_exact_selection_does_not_mutate_inputs(self):
        p,c,s=self.family();before=copy.deepcopy((p,c,s));self.assertEqual(3,len(d.validate_selection(p,c,s)));self.assertEqual(before,(p,c,s))

    def test_allocations_extensions_and_gameplay_selection_rejected(self):
        for field,value in [('newSeeds',1),('newStudyFights',1),('allocatedReplays',1),('maximumHistoricalReplays',49),('gameplayCandidateSelected',True)]:
            with self.subTest(field=field):
                p,c,s=self.family();p[field]=value
                with self.assertRaisesRegex(ValueError,'unallocated descriptive'):d.validate_selection(p,c,s)

    def test_missing_duplicate_wrong_floor_or_composition_family_rejected(self):
        for change in ('missing','duplicate','floor','composition'):
            p,c,s=self.family()
            if change=='missing':c.pop()
            elif change=='duplicate':c[-1]['id']=c[-2]['id']
            elif change=='floor':c[-1]['scenario']['floorNumber']=4
            else:c[-1]['scenario']['party'][0]['build']['essenceIds']=['unexpected']
            with self.assertRaises(ValueError):d.validate_selection(p,c,s)

    def test_later_reordered_and_duplicate_seeds_rejected(self):
        for change in ('later','reverse','duplicate'):
            p,c,s=self.family()
            if change=='later':p['seeds']=s[1:17]
            elif change=='reverse':p['seeds'].reverse()
            else:s[-1]=s[0]
            with self.assertRaisesRegex(ValueError,'sixteen declared'):d.validate_selection(p,c,s)

    def test_profiles_labels_counts_and_case_order_rejected(self):
        for change in ('profile','label','count','order'):
            p,c,s=self.family()
            if change=='profile':p['cases'][0]['gear']='health-and-regeneration'
            elif change=='label':p['cases'][2]['label']='A'
            elif change=='count':p['cases'].pop()
            else:p['cases'].reverse()
            with self.assertRaises(ValueError):d.validate_selection(p,c,s)

    def test_mismatched_parent_and_ambiguous_baseline_rejected(self):
        for change in ('parent','baseline'):
            p,c,s=self.family()
            if change=='parent':p['cases'][1]['composition']='B'
            else:c[-1]['composition']='A'
            with self.assertRaisesRegex(ValueError,'Matching parent|Unique baseline'):d.validate_selection(p,c,s)

    def test_equipment_counts_and_unselected_equipment_cannot_be_hidden(self):
        for change in ('specialized','plain','slots'):
            p,c,s=self.family()
            if change=='slots':p['cases'][1]['specializedPartySlots']=[3]
            else:c[1]['scenario']['party'][0]['build']['equipment'][0]['definitionId']='hidden.spec.test' if change=='specialized' else 'different-plain'
            with self.assertRaisesRegex(ValueError,'equipment counts|Unselected equipment'):d.validate_selection(p,c,s)

    def test_raw_identity_essence_order_and_budget_preserved(self):
        for field,value in [('id','changed'),('essenceIds',['raw-order','A']),('characterLevel',41)]:
            p,c,s=self.family();c[1]['scenario']['party'][0]['build'][field]=value
            with self.assertRaisesRegex(ValueError,'Raw identity'):d.validate_selection(p,c,s)

    def event(self,tick=600,target='g',actor='p1',source='Basic Attack',kind='Damage',amount=1,health=None):
        e=dict(timestamp=tick,targetId=target,actorId=actor,source=source,eventType=kind,magnitude=amount,details='',damageType='Magical',
            **{k:0 for k in d.mixed.MEASURES})
        if kind in ('Damage','DamageCrit'):e.update(finalHealthDamage=amount,incomingRawDamage=amount)
        e['combatEntity']=None if health is None else dict(id=target,health=health,maxHealth=100)
        return e

    def evaluate(self,events,seconds=75):return d.event_metrics(events,{'p1':1,'p2':2},'g',{'summon'},seconds,10)

    def test_half_open_windows_do_not_duplicate_boundaries(self):
        es=[self.event(tick=t) for t in (479,480,599,600,719,720)]
        r=self.evaluate(es)['windows'];self.assertEqual([1,2,2,1],[r[n]['guardian']['finalHealthDamage'] for n in ('before48','48to60','60to72','after72')])
        self.assertEqual(6,r['all']['guardian']['finalHealthDamage'])

    def test_exposure_is_clipped_when_battle_ends_early(self):
        r=self.evaluate([self.event(tick=470)],seconds=47)['windows']
        self.assertEqual([47,0,0,0],[r[n]['exposureSeconds'] for n in ('before48','48to60','60to72','after72')])

    def test_same_tick_death_and_heal_partitions_preserve_order(self):
        es=[self.event(target='p1',actor='g',amount=5),self.event(amount=3,health=90),self.event(target='p1',kind='Death',amount=0),
            self.event(actor='g',source=d.ENDLESS,kind='Heal',amount=6,health=96),self.event(amount=2,health=94)]
        r=self.evaluate(es);w=r['eventOrderWindows']
        self.assertEqual(3,w['beforeFirstDeathEvent']['guardian']['finalHealthDamage']);self.assertEqual(2,w['fromFirstDeathEvent']['guardian']['finalHealthDamage'])
        self.assertEqual(0,w['before60HealEvent']['endlessSpringHealing']);self.assertEqual(6,w['from60HealEvent']['endlessSpringHealing'])
        self.assertEqual(1,r['sixtySecondHeal']['heal']['lastObservedBefore']['index']);self.assertEqual(list(range(5)),[e['index'] for e in r['sixtySecondHeal']['sameTickEvents']])

    def test_missing_sixty_second_heal_is_not_inferred_from_duration(self):
        r=self.evaluate([self.event(tick=599,kind='Heal',source=d.ENDLESS,health=99)])
        self.assertIsNone(r['sixtySecondHeal']);self.assertNotIn('before60HealEvent',r['eventOrderWindows'])

    def test_original_condition_summon_and_other_output_are_separate(self):
        es=[self.event(source='condition.poison',amount=3),self.event(amount=5),self.event(actor='summon',amount=7),self.event(actor='unknown',amount=11)]
        r=self.evaluate(es)['windows']['all'];a=r['damageByOriginAndKind']
        self.assertEqual(3,a['originalParty']['condition']);self.assertEqual(5,a['originalParty']['direct']);self.assertEqual(7,a['summon']['direct']);self.assertEqual(11,a['otherActor']['direct'])
        self.assertEqual(26,sum(sum(v.values()) for v in a.values()));self.assertEqual(8,r['outgoingByOriginalSlot']['1'])

    def test_condition_damage_after_death_is_retained(self):
        es=[self.event(target='p1',kind='Death',amount=0),self.event(source='condition.poison',amount=3)]
        self.assertEqual(3,self.evaluate(es)['eventOrderWindows']['fromFirstDeathEvent']['damageByOriginAndKind']['originalParty']['condition'])

    def test_actual_healing_regeneration_and_other_heals_are_separate(self):
        es=[self.event(amount=20,health=80),self.event(actor='g',kind='Heal',source=d.ENDLESS,amount=5,health=85),
            self.event(kind='HealOverTime',source='other',amount=2,health=87),self.event(kind='HealthRegeneration',amount=3,health=90)]
        r=self.evaluate(es)['windows']['all'];self.assertEqual((5,2,3,-10),(r['endlessSpringHealing'],r['otherHealing'],r['guardianRegeneration'],r['guardianNetLoggedHealthChange']))

    def test_heal_snapshots_record_observed_not_assumed_change(self):
        es=[self.event(health=90),self.event(kind='Heal',source=d.ENDLESS,amount=7,health=96)]
        h=self.evaluate(es)['guardianHeals'][0];self.assertEqual(7,h['actualHealing']);self.assertEqual(6,h['observedHealthChange'])

    def test_first_heal_without_prior_snapshot_is_explicitly_unknown(self):
        h=self.evaluate([self.event(kind='Heal',source=d.ENDLESS,amount=7,health=96)])['guardianHeals'][0]
        self.assertIsNone(h['lastObservedBefore']);self.assertIsNone(h['observedHealthChange'])

    def test_guardian_heal_requires_native_target_snapshot(self):
        for snapshot in (None,dict(id='other',health=99,maxHealth=100)):
            e=self.event(kind='Heal',source=d.ENDLESS);e['combatEntity']=snapshot
            with self.assertRaisesRegex(ValueError,'healing snapshot'):self.evaluate([e])

    def test_tied_first_deaths_and_all_causes_preserved(self):
        es=[self.event(target='p1'),self.event(target='p1',kind='Death'),self.event(target='p2'),self.event(target='p2',kind='Death'),self.event(tick=610,target='p1',kind='Death')]
        r=self.evaluate(es);self.assertEqual([1,2],r['firstDeathSlots']);self.assertEqual([0,2,0],[x['precedingDamageIndex'] for x in r['originalPartyDeaths']])

    def test_no_death_has_empty_after_partition(self):
        r=self.evaluate([self.event()]);self.assertEqual([],r['originalPartyDeaths']);self.assertEqual(0,r['eventOrderWindows']['fromFirstDeathEvent']['guardian']['finalHealthDamage'])

    def test_near_heal_windows_respect_event_index_and_three_seconds(self):
        es=[self.event(tick=569),self.event(tick=570),self.event(kind='Heal',source=d.ENDLESS,amount=3,health=98),self.event(tick=629),self.event(tick=630)]
        r=self.evaluate(es)['sixtySecondHeal'];self.assertEqual(1,r['preceding3Seconds']['guardian']['finalHealthDamage']);self.assertEqual(1,r['following3Seconds']['guardian']['finalHealthDamage'])

    def test_notifications_retain_resistance_and_removals_without_state_inference(self):
        es=[self.event(source='condition.weaken',kind='StatusEffectResisted'),self.event(source=d.resistance.ABUNDANCE,kind='StatusEffect'),
            self.event(source='effect.creature.eydis.ancient_heartwood.defense',kind='BuffExpired')]
        r=self.evaluate(es);self.assertEqual(3,len(r['notifications']));self.assertIn('do not establish exact stacks',r['meaning'])

    def test_out_of_order_events_rejected(self):
        with self.assertRaisesRegex(ValueError,'Chronological'):self.evaluate([self.event(601),self.event(600)])

    def test_native_omitted_zero_restoration_fields_equal_explicit_zero(self):
        stats=dict(healthRegenerationPotential=0,healthRegenerationOverhealed=0)
        self.assertEqual(d.guardian_restoration_stats(stats),d.guardian_restoration_stats(dict(stats,healingPotential=0,overhealing=0)))

    def test_native_nonzero_healing_potential_and_overhealing_preserved(self):
        stats=dict(healingPotential=30,overhealing=10,healthRegenerationPotential=9,healthRegenerationOverhealed=2)
        self.assertEqual(dict(guardianHealingPotential=30,guardianOverhealing=10,guardianRegenerationPotential=9,guardianRegenerationOverhealed=2),d.guardian_restoration_stats(stats))

if __name__=='__main__':unittest.main()
