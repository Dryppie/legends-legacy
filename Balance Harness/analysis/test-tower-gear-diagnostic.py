"""Guard diagnostic conclusions against changed outcomes and incorrect event totals."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('gear_diagnostic', Path(__file__).with_name('diagnose-tower-gear.py'))
d = importlib.util.module_from_spec(spec); spec.loader.exec_module(d)


class GearDiagnosticTests(unittest.TestCase):
    def reports(self):
        stats = [dict(entityId=str(i), entityName=str(i), firstDeathTick=None,
                      **{k: 0 for k in d.FIELDS}) for i in range(10)]
        stats[0]['healingReceived'] = 7
        report = dict(succeeded=True, guardianHealthRemainingPercent=0, displayDurationSeconds=10,
            battle=dict(seed=17, ticksPerSecond=10,
                preparedParticipants=[dict(slot=dict(side='Friendly', slotId=str(i))) for i in range(10)],
                summary=dict(durationSeconds=10, statistics=stats), eventLog=None))
        replay = copy.deepcopy(report)
        replay['battle']['eventLog'] = [dict(targetId='0', eventType='Heal', magnitude=7,
            finalHealthDamage=0, timestamp=15, source='heal', damageType='None')]
        return report, replay

    def test_reconciles_recipient_health_and_common_window(self):
        saved, replay = self.reports(); actual = d.analyze(saved, replay)
        self.assertEqual(7, actual['first40Seconds']['restoredHealth'])
        self.assertIsNone(actual['firstDeathSeconds'])

    def test_rejects_changed_saved_outcome(self):
        saved, replay = self.reports(); replay['succeeded'] = False
        with self.assertRaisesRegex(ValueError, 'complete saved report'): d.analyze(saved, replay)

    def test_rejects_nonreconciling_events(self):
        saved, replay = self.reports(); replay['battle']['eventLog'][0]['magnitude'] = 8
        with self.assertRaisesRegex(ValueError, 'recipient statistics'): d.analyze(saved, replay)

    def test_excludes_summoned_entity_from_original_party_totals(self):
        saved, replay = self.reports()
        for r in (saved, replay):
            summon = copy.deepcopy(r['battle']['summary']['statistics'][0])
            summon.update(entityId='summon', healingReceived=99)
            r['battle']['summary']['statistics'].append(summon)
        self.assertEqual(7, d.analyze(saved, replay)['healingReceived'])

    def test_rejects_missing_event_log(self):
        saved, replay = self.reports(); replay['battle']['eventLog'] = []
        with self.assertRaisesRegex(ValueError, 'Missing detailed'): d.analyze(saved, replay)


if __name__ == '__main__': unittest.main()
