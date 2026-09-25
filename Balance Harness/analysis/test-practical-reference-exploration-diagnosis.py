"""Literal ranking/selection boundaries and fail-closed saved-trace checks."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

path = Path(__file__).with_name('practical-reference-exploration-diagnosis.py')
spec = importlib.util.spec_from_file_location('diagnosis', path)
diagnosis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(diagnosis)


def row(pid, count, health=10, survival=1, duration=2):
    return dict(id=pid, fitness=dict(worstContextWinRate=count/8, guardianHealth=health,
                survival=survival, victoryDuration=duration),
                cells=[dict(clears=[True]*count+[False]*(8-count), guardianHealth=health, trials=list(range(8)))])


class DiagnosticTests(unittest.TestCase):
    def test_discovery_lexicographic_boundaries(self):
        rows=[row('duration',5,9,2,1),row('survival',5,9,3,9),row('health',5,8),row('wins',6,99)]
        self.assertEqual(['wins','health','survival','duration'],[r['id'] for r in sorted(rows,key=diagnosis.rank_key)])

    def test_positive_tie_keeps_designated(self):
        self.assertEqual(('primary','DesignatedPositiveTie',['other','primary']),
                         diagnosis.select([row('other',6,1),row('primary',6,99)],['other','primary'],'primary'))

    def test_below_maximum_designation_cannot_override(self):
        self.assertEqual('other',diagnosis.select([row('other',6),row('primary',5)],['primary','other'],'primary')[0])

    def test_non_designated_tie_uses_frozen_order_not_health(self):
        self.assertEqual('b',diagnosis.select([row('a',6,1),row('b',6,99),row('p',5)],['b','a','p'],'p')[0])

    def test_zero_win_tie_uses_health(self):
        self.assertEqual('other',diagnosis.select([row('other',0,1),row('primary',0,99)],['primary','other'],'primary')[0])

    def test_missing_and_duplicate_measurements_rejected(self):
        for rows in [[row('a',2)],[row('a',2),row('a',2)]]:
            with self.assertRaisesRegex(ValueError,'shortlist'):
                diagnosis.select(rows,['a','b'],'a')

    def test_partial_and_nonboolean_panels_rejected(self):
        with self.assertRaisesRegex(ValueError,'Incomplete'):
            diagnosis.wins(row('a',2),32)
        invalid=row('a',2);invalid['cells'][0]['clears'][0]=1
        with self.assertRaisesRegex(ValueError,'outcomes'):
            diagnosis.wins(invalid)

    def test_changed_fitness_rejected(self):
        invalid=row('a',2);invalid['fitness']['worstContextWinRate']=1
        with self.assertRaisesRegex(ValueError,'fitness'):
            diagnosis.wins(invalid)

    def test_saved_trace_tampering_rejected(self):
        saved=json.loads((diagnosis.RUN/'study/pair-09-candidate.json').read_text())
        for kind in ['nomination','parent','schedule','output']:
            invalid=copy.deepcopy(saved)
            if kind=='nomination':
                invalid['discovery']['discoveryShortlist'].reverse()
            elif kind=='parent':
                invalid['discovery']['arms'][0]['proposals'][12]['provenance']['parentIds']=['unknown']
            elif kind=='schedule':
                invalid['discovery']['arms'][0]['proposals'][12]['supplied']['exploration']['radius']=3
            else:
                invalid['output']['finalist']['party']['id']='changed'
            with self.subTest(kind=kind), self.assertRaises(ValueError):
                diagnosis.arm_diagnosis(invalid,'candidate')

    def test_saved_funnel_partitions_all_direct_proposals(self):
        saved=diagnosis.read(diagnosis.OUTPUT/'diagnosis.json')
        summary=diagnosis.stage_summary(saved)
        self.assertEqual((114,1,7),(summary['directBelowWinCutoff'],summary['directTiedBelowCutoff'],len(summary['directNominees'])))
        self.assertEqual(122,summary['directBelowWinCutoff']+summary['directTiedBelowCutoff']+len(summary['directNominees']))

    def test_saved_owner_coverage_retains_unvisited_slots(self):
        saved=diagnosis.read(diagnosis.OUTPUT/'diagnosis.json')
        summary=diagnosis.stage_summary(saved)
        counts=summary['scheduledSlotCounts']['confirmed-96b943571150684df3a5be53']
        self.assertEqual({slot:12 if slot<=7 else 0 for slot in range(1,11)},counts)


if __name__=='__main__':
    unittest.main()
