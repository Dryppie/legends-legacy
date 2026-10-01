"""Guard descriptive pairing, recipients and time-window attribution without combat."""
import copy
import importlib.util
from pathlib import Path
import unittest

HERE=Path(__file__).resolve().parent
def load(name):
    spec=importlib.util.spec_from_file_location(name.replace('-','_'),HERE/(name+'.py'))
    value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value

d=load('diagnose-tower-armor-outlier')
fixture=load('test-tower-mixed-armor-diagnostic')


class OutlierTests(unittest.TestCase):
    def test_paired_gains_and_losses_are_not_pooled(self):
        left={i:dict(won=i<300,seconds=60) for i in range(512)}
        right={i:dict(won=i>=212,seconds=50) for i in range(512)}
        result=d.paired(left,right)
        self.assertEqual((88,212,212,0),(result['bothWin'],result['leftOnly'],result['rightOnly'],result['neither']))
        self.assertEqual(0,result['netWinDifference']);self.assertEqual(10,result['meanDurationDifference'])

    def test_missing_or_different_seed_panels_rejected(self):
        rows={i:dict(won=False,seconds=1) for i in range(512)}
        for other in ({i:r for i,r in rows.items() if i}, {i+1:r for i,r in rows.items()}):
            with self.assertRaises(ValueError):d.paired(rows,other)

    def replay(self):
        _,replay,scenario=fixture.MixedDiagnosticTests().reports()
        event=copy.deepcopy(replay['battle']['eventLog'][0]);event.update(actorId='0',targetId='guardian',source='poison',timestamp=210,
            incomingRawDamage=4,physicalMitigationPrevented=0,finalHealthDamage=4)
        replay['battle']['eventLog'].append(event)
        replay['battle']['summary']['statistics'].append(dict(entityId='guardian',finalHealthDamage=4,healingReceived=0,healthRegenerated=0))
        return replay,scenario

    def test_incoming_and_outgoing_damage_are_separate(self):
        r,s=self.replay();result=d.timeline(r,s)
        self.assertEqual(3,result['windows']['all']['partyHealthDamage'])
        self.assertEqual(4,result['windows']['all']['guardianHealthDamage'])
        self.assertEqual({'poison':4},result['windows']['all']['outgoingBySource'])
        self.assertEqual(4,result['windows']['all']['outgoingByOriginalSlot']['1'])

    def test_fixed_boundary_and_battle_end_exposure(self):
        r,s=self.replay();result=d.timeline(r,s)
        self.assertEqual(0,result['windows']['opening']['guardianHealthDamage'])
        self.assertEqual(4,result['windows']['middle']['guardianHealthDamage'])
        self.assertEqual(9,result['windows']['middle']['exposureSeconds'])
        self.assertEqual(0,result['windows']['late']['exposureSeconds'])

    def test_guardian_recipient_must_reconcile(self):
        r,s=self.replay();r['battle']['summary']['statistics'][-1]['finalHealthDamage']=5
        with self.assertRaisesRegex(ValueError,'Guardian recipient'):d.timeline(r,s)

    def test_non_original_actor_damage_remains_explicit(self):
        r,s=self.replay();r['battle']['eventLog'][-1]['actorId']='summon'
        result=d.timeline(r,s)
        self.assertEqual(4,result['windows']['all']['otherActorDamage'])
        self.assertEqual(0,sum(result['windows']['all']['outgoingByOriginalSlot'].values()))

    def test_guardian_healing_is_not_player_damage(self):
        r,s=self.replay();event=copy.deepcopy(r['battle']['eventLog'][-1]);event.update(eventType='Heal',magnitude=7,
            incomingRawDamage=0,finalHealthDamage=0,source='heal',actorId='guardian')
        r['battle']['eventLog'].append(event);r['battle']['summary']['statistics'][-1]['healingReceived']=7
        result=d.timeline(r,s)['windows']['all'];self.assertEqual(7,result['guardianHealing']);self.assertEqual(4,result['guardianHealthDamage'])

    def test_exact_consumed_stacks_are_not_inferred(self):
        r,s=self.replay();self.assertIsNone(d.timeline(r,s)['exactFeastConsumedStacks'])

    def test_incomplete_detail_panel_cannot_be_summarized(self):
        with self.assertRaisesRegex(ValueError,'32-seed'):d.summarize_timelines([])

    def test_incomplete_saved_panel_cannot_be_summarized(self):
        with self.assertRaisesRegex(ValueError,'Full saved'):d.summarize_saved([])


if __name__=='__main__':unittest.main()
