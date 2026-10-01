"""Protect exact historical selection and casualty/event accounting."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

s=importlib.util.spec_from_file_location('mad_king',Path(__file__).with_name('diagnose-tower-mad-king.py'))
d=importlib.util.module_from_spec(s);s.loader.exec_module(d)


class MadKingDiagnosticTests(unittest.TestCase):
    def reports(self):
        participants=[dict(name=str(i),slot=dict(side='Friendly',slotId=str(i)),combatAttributes={'Armor':30}) for i in range(15)]
        participants.append(dict(name='king',slot=dict(side='Hostile',slotId='king'),combatAttributes={'Power':70}))
        stats=[dict(entityId=p['slot']['slotId'],entityName=p['name'],firstDeathTick=None,
            **dict.fromkeys(d.common.base.FIELDS,0)) for p in participants]
        report=dict(succeeded=False,guardianHealthRemainingPercent=40,battle=dict(seed=1,ticksPerSecond=10,
            preparedParticipants=participants,summary=dict(durationSeconds=20,statistics=stats),eventLog=None))
        scenario=dict(party=[dict(partySlot=i+1,build=dict(id=str(i))) for i in range(15)])
        replay=copy.deepcopy(report);replay['battle']['eventLog']=[self.event()]
        for r in (report,replay):r['battle']['summary']['statistics'][0].update(incomingRawDamage=5,physicalMitigationPrevented=2,finalHealthDamage=3)
        return report,replay,scenario

    def event(self,**values):
        return dict(dict(source=d.PREFIX+'bloodbath.damage',statsSource='ability',actorId='king',targetId='0',timestamp=100,
            eventType='Damage',damageType='Physical',magnitude=3,details='',incomingRawDamage=5,
            physicalMitigationPrevented=2,magicalMitigationPrevented=0,finalHealthDamage=3),**{}) | values

    def with_death(self):
        saved,replay,scenario=self.reports()
        for r in (saved,replay):r['battle']['summary']['statistics'][0]['firstDeathTick']=100
        replay['battle']['eventLog'].append(self.event(eventType='Death',magnitude=0,**dict.fromkeys(d.common.DAMAGE,0)))
        return saved,replay,scenario

    def test_fifteen_original_recipients_and_full_parity(self):
        result=d.analyze(*self.reports());self.assertEqual(15,len(result['preparedParty']));self.assertEqual(3,result['windows']['all']['party']['finalHealthDamage'])

    def test_complete_outcome_change_is_rejected(self):
        saved,replay,scenario=self.reports();replay['succeeded']=True
        with self.assertRaisesRegex(ValueError,'complete saved report'):d.analyze(saved,replay,scenario)

    def test_raw_mitigation_mismatch_is_rejected(self):
        saved,replay,scenario=self.reports();replay['battle']['eventLog'][0]['physicalMitigationPrevented']=4
        with self.assertRaisesRegex(ValueError,'Recipient damage'):d.analyze(saved,replay,scenario)

    def test_wrong_original_identity_is_rejected(self):
        saved,replay,scenario=self.reports();scenario['party'][0]['build']['id']='wrong'
        with self.assertRaisesRegex(ValueError,'identities'):d.analyze(saved,replay,scenario)

    def test_killing_hit_is_before_death_even_on_same_tick(self):
        result=d.analyze(*self.with_death())
        self.assertEqual(3,result['windows']['beforeFirstDeath']['party']['finalHealthDamage'])
        self.assertEqual(1,result['windows']['fromFirstDeath']['party']['deaths'])
        self.assertEqual(0,result['originalPartyDeaths'][0]['precedingDamage']['index'])

    def test_later_same_tick_damage_stays_after_death(self):
        saved,replay,scenario=self.with_death();replay['battle']['eventLog'].append(self.event(source='later'))
        for r in (saved,replay):r['battle']['summary']['statistics'][0].update(incomingRawDamage=10,physicalMitigationPrevented=4,finalHealthDamage=6)
        result=d.analyze(saved,replay,scenario)
        self.assertEqual(3,result['windows']['fromFirstDeath']['party']['finalHealthDamage'])
        self.assertEqual(d.PREFIX+'bloodbath.damage',result['originalPartyDeaths'][0]['precedingDamage']['source'])

    def test_no_death_has_no_post_death_exposure(self):
        result=d.analyze(*self.reports());self.assertIsNone(result['firstDeathIndex']);self.assertEqual(0,result['windows']['fromFirstDeath']['exposureSeconds'])

    def test_wrong_death_stat_is_rejected(self):
        saved,replay,scenario=self.with_death();replay['battle']['eventLog'][-1]['timestamp']=101
        with self.assertRaisesRegex(ValueError,'recipient statistics'):d.analyze(saved,replay,scenario)

    def test_modifier_expiration_and_recipient_are_respected(self):
        source=d.MODIFIERS[0]
        events=[self.event(source=source,eventType='Buff',magnitude=40,targetId='king'),
            self.event(source=source,eventType='BuffExpired',magnitude=-40,targetId='king'),
            self.event(source=source,eventType='Buff',magnitude=90,targetId='other')]
        self.assertEqual(0,d.modifier_state(events,'king')[source])

    def test_modifier_after_death_is_excluded(self):
        saved,replay,scenario=self.with_death()
        replay['battle']['eventLog'].append(self.event(source=d.MODIFIERS[0],eventType='Buff',magnitude=40,targetId='king',**dict.fromkeys(d.common.DAMAGE,0)))
        self.assertEqual(0,d.analyze(saved,replay,scenario)['guardianModifiersBeforeFirstDeath'][d.MODIFIERS[0]])

    def test_friendly_summon_is_not_an_original_casualty(self):
        saved,replay,scenario=self.reports()
        for r in (saved,replay):r['battle']['summary']['statistics'].append(dict(entityId='pet',entityName='pet',team='Friendly',isSummonedEntity=True,
            firstDeathTick=100,**dict.fromkeys(d.common.base.FIELDS,0)))
        replay['battle']['eventLog'].append(self.event(eventType='Death',targetId='pet',magnitude=0,**dict.fromkeys(d.common.DAMAGE,0)))
        result=d.analyze(saved,replay,scenario);self.assertEqual([],result['originalPartyDeaths']);self.assertEqual(['pet'],result['friendlySummons'])

    def test_party_output_excludes_guardian_self_damage(self):
        es=[self.event(actorId='0',targetId='king'),self.event(actorId='king',targetId='king')]
        rows=d.common.window(es,{'0':1},'king',set(),set())['guardian']['damageBySource']
        self.assertEqual({'originalParty','guardian'},{r['origin'] for r in rows})

    def test_unordered_events_are_rejected(self):
        saved,replay,scenario=self.reports();replay['battle']['eventLog'].append(self.event(timestamp=99,**dict.fromkeys(d.common.DAMAGE,0)))
        with self.assertRaisesRegex(ValueError,'Chronological'):d.analyze(saved,replay,scenario)


class FrozenSelectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        path=d.ROOT/'TestResults/tower-floor10-pressure-diagnostic-proposal-20261001.json'
        if not path.exists():raise unittest.SkipTest('Local immutable historical archive is required for selection integration tests')
        assert d.io.sha(path)==d.PLAN_PIN
        cls.plan=d.io.read(path);source=Path(cls.plan['source'])
        cls.cells=d.io.read(source/'cells.json');cls.seeds=d.io.read(source/'request.json')['seeds']
        cls.trials=[json.loads(line) for line in (source/'evaluation/trials.jsonl').read_text().splitlines()]

    def test_exact_six_by_sixteen_selection(self):
        selected=d.validate_selection(self.plan,self.cells,self.seeds,self.trials)
        self.assertEqual(96,len(selected));self.assertEqual(self.seeds[:16],[s['trial']['seed'] for s in selected[::6]])

    def test_seed_order_change_is_rejected(self):
        with self.assertRaisesRegex(ValueError,'First sixteen'):d.validate_selection(self.plan,self.cells,self.seeds[::-1],self.trials)

    def test_missing_source_control_is_rejected(self):
        with self.assertRaisesRegex(ValueError,'278-recipe'):d.validate_selection(self.plan,self.cells[:-1],self.seeds,self.trials)

    def test_raw_essence_order_change_is_rejected(self):
        plan=copy.deepcopy(self.plan);cells=copy.deepcopy(self.cells)
        scenario=plan['cells'][1]['scenario'];scenario['party'][0]['build']['essenceIds'].reverse()
        next(c for c in cells if c['id']==plan['cells'][1]['cellId'])['scenario']=scenario
        with self.assertRaisesRegex(ValueError,'Essence order'):d.validate_selection(plan,cells,self.seeds,self.trials)

    def test_trial_identity_change_is_rejected(self):
        plan=copy.deepcopy(self.plan);plan['cells'][0]['trials'][0]['inputHash']='changed'
        with self.assertRaisesRegex(ValueError,'trial identities'):d.validate_selection(plan,self.cells,self.seeds,self.trials)

    def test_budget_and_acceptance_changes_are_rejected(self):
        for field,value in [('newSeeds',1),('retries',1),('usedForAcceptance',True),('maximumHistoricalReplays',97)]:
            with self.subTest(field=field),self.assertRaisesRegex(ValueError,'Frozen descriptive'):
                d.validate_selection(dict(self.plan,**{field:value}),self.cells,self.seeds,self.trials)


if __name__=='__main__':unittest.main()
