"""Guard frozen paired selection, damage attribution and exact replay reconciliation."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('kodoku_diagnostic', Path(__file__).with_name('diagnose-tower-kodoku.py'))
d = importlib.util.module_from_spec(spec); spec.loader.exec_module(d)


class SelectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan = d.io.read(d.ROOT/'TestResults/tower-floor8-gear-diagnostic-proposal-20260930.json')
        source = Path(cls.plan['source']); cls.review = d.io.read(cls.plan['savedReview'])
        cls.cells = d.io.read(source/'cells.json'); cls.seeds = d.io.read(source/'request.json')['seeds']
        cls.trials = [json.loads(s) for s in (source/'evaluation/trials.jsonl').read_text().splitlines()]

    def validate(self, plan=None, review=None, cells=None, seeds=None, trials=None):
        return d.validate_selection(plan or self.plan, review or self.review, cells or self.cells, seeds or self.seeds, trials or self.trials)

    def test_exact_original_six_recipe_pairing(self):
        chosen = self.validate()
        self.assertEqual([[], [3, 7], list(range(1, 11)), [], [3, 8], list(range(1, 11))], [c['armorSlots'] for c in chosen])

    def test_rejects_changed_pair_order(self):
        p = copy.deepcopy(self.plan); p['selected'][0], p['selected'][1] = p['selected'][1], p['selected'][0]
        with self.assertRaisesRegex(ValueError, 'paired sample'): self.validate(plan=p)

    def test_rejects_incomplete_seed_panel(self):
        with self.assertRaisesRegex(ValueError, 'seed panel'): self.validate(seeds=self.seeds[:-1])

    def test_rejects_repeated_seed(self):
        with self.assertRaisesRegex(ValueError, 'seed panel'): self.validate(seeds=[self.seeds[0]]*32)

    def test_rejects_dropped_control(self):
        with self.assertRaisesRegex(ValueError, 'Complete floor-eight'): self.validate(cells=self.cells[:-1])

    def test_rejects_repeated_trial(self):
        with self.assertRaisesRegex(ValueError, 'unique source'): self.validate(trials=self.trials[:-1]+self.trials[:1])

    def test_rejects_changed_essence_order(self):
        cells = copy.deepcopy(self.cells); key = self.review['comparisons'][0]['limitedId']
        member = next(c for c in cells if c['id'] == key)['scenario']['party'][0]
        member['build']['essenceIds'].reverse()
        with self.assertRaisesRegex(ValueError, 'Raw identity'): self.validate(cells=cells)

    def test_rejects_unselected_slot_equipment_change(self):
        cells = copy.deepcopy(self.cells); key = self.review['comparisons'][0]['limitedId']
        member = next(c for c in cells if c['id'] == key)['scenario']['party'][0]
        member['build']['equipment'][0]['definitionId'] += '.changed'
        with self.assertRaisesRegex(ValueError, 'equipment substitution'): self.validate(cells=cells)

    def test_rejects_changed_limits(self):
        for field, value in [('maximumHistoricalReplays', 97), ('maximumSeconds', 841), ('perReplaySeconds', 61),
                             ('maximumLogBytes', 32*1024**2), ('maximumBytes', 3*1024**3), ('allocatedReplays', 1), ('usedForAcceptance', True)]:
            with self.subTest(field=field):
                plan = copy.deepcopy(self.plan); plan[field] = value
                with self.assertRaises(ValueError): self.validate(plan=plan)


def event(target='p0', actor='boss', **kwargs):
    e = dict(targetId=target, actorId=actor, timestamp=10, source='Basic Attack', damageType='Physical',
             eventType='Damage', magnitude=0, details='', **dict.fromkeys(d.DAMAGE, 0)); e.update(kwargs); return e


class EventTests(unittest.TestCase):
    def reports(self):
        stats = [dict(entityId='p'+str(i), entityName='p'+str(i), firstDeathTick=None, team='Friendly', isSummonedEntity=False,
                      **dict.fromkeys((*d.base.FIELDS, *d.DAMAGE), 0)) for i in range(10)]
        stats.append(dict(stats[0], entityId='boss', entityName='boss', team='Hostile'))
        saved = dict(succeeded=False, guardianHealthRemainingPercent=80, battle=dict(seed=42, ticksPerSecond=10,
            preparedParticipants=[dict(name=s['entityId'], slot=dict(slotId=s['entityId'], side=s['team'])) for s in stats],
            summary=dict(durationSeconds=50, statistics=stats), eventLog=None))
        replay = copy.deepcopy(saved); replay['battle']['eventLog'] = [event()]
        scenario = dict(party=[dict(partySlot=i+1, build=dict(id='p'+str(i))) for i in range(10)])
        return saved, replay, scenario

    def test_complete_report_must_match(self):
        saved, replay, scenario = self.reports(); replay['battle']['seed'] = 43
        with self.assertRaisesRegex(ValueError, 'complete saved report'): d.analyze(saved, replay, scenario)

    def test_rejects_recipient_raw_mitigation_mismatch(self):
        for field in d.DAMAGE:
            with self.subTest(field=field):
                saved, replay, scenario = self.reports(); replay['battle']['eventLog'][0][field] = 1
                with self.assertRaises(ValueError): d.analyze(saved, replay, scenario)

    def test_rejects_guardian_healing_mismatch(self):
        saved, replay, scenario = self.reports(); replay['battle']['eventLog'] = [event(target='boss', eventType='Heal', magnitude=8)]
        with self.assertRaisesRegex(ValueError, 'restoration/death'): d.analyze(saved, replay, scenario)

    def test_rejects_unordered_events(self):
        saved, replay, scenario = self.reports(); replay['battle']['eventLog'] = [event(timestamp=20), event(timestamp=10)]
        with self.assertRaisesRegex(ValueError, 'Chronological'): d.analyze(saved, replay, scenario)

    def test_excludes_friendly_summon_from_original_recipients(self):
        saved, replay, scenario = self.reports()
        for r in (saved, replay):
            s = dict(r['battle']['summary']['statistics'][0], entityId='summon', isSummonedEntity=True, healingReceived=99)
            r['battle']['summary']['statistics'].append(s)
        replay['battle']['eventLog'].append(event(target='summon', eventType='Heal', magnitude=99))
        result = d.analyze(saved, replay, scenario)
        self.assertEqual(0, result['windows']['all']['party']['healing']); self.assertEqual(['summon'], result['friendlySummons'])

    def test_half_open_windows_and_event_order_preserve_killing_damage(self):
        saved, replay, scenario = self.reports()
        for r in (saved, replay):
            s = r['battle']['summary']['statistics'][0]; s.update(finalHealthDamage=9, incomingRawDamage=12, physicalMitigationPrevented=3, firstDeathTick=150)
        replay['battle']['eventLog'] = [event(timestamp=150, finalHealthDamage=9, incomingRawDamage=12, physicalMitigationPrevented=3),
                                       event(timestamp=150, eventType='Death')]
        result = d.analyze(saved, replay, scenario)
        self.assertEqual(0, result['windows']['before15']['party']['finalHealthDamage'])
        self.assertEqual(9, result['windows']['15to40']['party']['finalHealthDamage'])
        self.assertEqual(9, result['eventOrderWindows']['beforeFirstDeath']['party']['finalHealthDamage'])
        self.assertEqual(1, result['eventOrderWindows']['fromFirstDeath']['party']['deaths'])
        self.assertEqual(0, result['originalPartyDeaths'][0]['precedingDamage']['index'])

    def test_origin_attribution_keeps_self_damage_and_summons_separate(self):
        es = [event(actor='p0', finalHealthDamage=3), event(actor='venom', finalHealthDamage=7),
              event(actor='boss', finalHealthDamage=11), event(target='boss', actor='pet', finalHealthDamage=13)]
        result = d.window(es, {'p0': 1}, 'boss', {'venom'}, {'pet'})
        origins = {row['origin']: row['finalHealthDamage'] for row in result['party']['damageBySource']}
        self.assertEqual({'originalParty': 3, 'hostileSummon': 7, 'guardian': 11}, origins)
        self.assertEqual('friendlySummon', result['guardian']['damageBySource'][0]['origin'])

    def test_zero_potential_fields_and_no_deaths_are_supported(self):
        result = d.analyze(*self.reports())
        self.assertEqual([], result['originalPartyDeaths']); self.assertEqual(0, result['recipientStats'][0]['overhealing'])
        self.assertEqual(0, result['eventOrderWindows']['fromFirstDeath']['exposureSeconds'])

    def test_notification_is_observation_not_reconstructed_status(self):
        saved, replay, scenario = self.reports(); replay['battle']['eventLog'] = [event(source=d.PREFIX+'withering_miasma.healing', eventType='Debuff', magnitude=-80)]
        result = d.analyze(saved, replay, scenario)
        self.assertEqual(-80, result['notifications'][0]['magnitude']); self.assertNotIn('exactStacks', result['notifications'][0])


if __name__ == '__main__': unittest.main()
