"""Guard the Ni sample, resource admission and interpretation of native events."""
import copy
import gzip
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

def module(name, filename):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(filename));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
d=module('ni_diagnostic','diagnose-tower-ni.py')
old=module('ni_event_fixtures','test-tower-kodoku-diagnostic.py')


class SelectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan=d.io.read(d.ROOT/'TestResults/tower-floor9-ni-pressure-diagnostic-proposal-20261001.json')
        source=Path(cls.plan['source']);cls.cells=d.io.read(source/'cells.json');cls.seeds=d.io.read(source/'request.json')['seeds']
        cls.trials=[json.loads(line) for line in (source/'evaluation/trials.jsonl').read_text().splitlines()]

    def validate(self, **changes):
        args=dict(plan=self.plan,cells=self.cells,seeds=self.seeds,trials=self.trials);args.update(changes);return d.validate_selection(**args)

    def test_exact_six_recipes(self):
        self.assertEqual([[],[3,4],list(range(1,11)),[],[4,8],list(range(1,11))],[r['resistanceSlots'] for r in self.validate()])

    def test_limits_and_acceptance_flags_cannot_change(self):
        for key,value in [('maximumHistoricalReplays',97),('maximumSeconds',841),('perReplaySeconds',61),('maximumLogBytes',32*1024**2),
            ('maximumBytes',3*1024**3),('allocatedReplays',1),('newSeeds',1),('newStudyFights',1),('usedForAcceptance',True),
            ('gameplayCandidateSelected',True),('requireCompleteSavedReportParityAfterRemovingEventLog',False),
            ('requiresMeasuredResourceAdmission',False),('maximumAdmittedSeconds',673),('maximumAdmittedBytes',2*1024**3)]:
            with self.subTest(key=key):
                p=copy.deepcopy(self.plan);p[key]=value
                with self.assertRaises(ValueError):self.validate(plan=p)

    def test_wrong_floor_or_version_rejected(self):
        for key,value in [('floor',8),('version','floor8-gear-diagnostic-proposal-v1'),('status','Executed')]:
            p=copy.deepcopy(self.plan);p[key]=value
            with self.assertRaises(ValueError):self.validate(plan=p)

    def test_pair_order_is_fixed(self):
        p=copy.deepcopy(self.plan);p['selected'][0],p['selected'][1]=p['selected'][1],p['selected'][0]
        with self.assertRaisesRegex(ValueError,'paired sample'):self.validate(plan=p)

    def test_no_outcome_selected_seeds(self):
        p=copy.deepcopy(self.plan);p['seeds']=self.seeds[16:]
        with self.assertRaisesRegex(ValueError,'First sixteen'):self.validate(plan=p)

    def test_complete_unique_seeds_required(self):
        for seeds in (self.seeds[:-1],[self.seeds[0]]*32):
            with self.assertRaises(ValueError):self.validate(seeds=seeds)

    def test_preserves_complete_family(self):
        with self.assertRaisesRegex(ValueError,'Complete floor-nine'):self.validate(cells=self.cells[:-1])

    def test_trial_identity_cannot_repeat(self):
        with self.assertRaisesRegex(ValueError,'unique trial'):self.validate(trials=self.trials[:-1]+self.trials[:1])

    def test_raw_essence_order_is_preserved(self):
        cells=copy.deepcopy(self.cells);c=next(c for c in cells if c['id']==self.plan['comparisons'][0]['limitedId'])
        c['scenario']['party'][0]['build']['essenceIds'].reverse()
        with self.assertRaisesRegex(ValueError,'Raw identity'):self.validate(cells=cells)

    def test_unselected_equipment_is_preserved(self):
        cells=copy.deepcopy(self.cells);c=next(c for c in cells if c['id']==self.plan['comparisons'][0]['limitedId'])
        c['scenario']['party'][0]['build']['equipment'][0]['definitionId']+='changed'
        with self.assertRaisesRegex(ValueError,'equipment substitution'):self.validate(cells=cells)

    def test_selected_slots_are_fixed(self):
        p=copy.deepcopy(self.plan);p['comparisons'][0]['limitedPartySlots']=[1,2]
        with self.assertRaisesRegex(ValueError,'limited slots'):self.validate(plan=p)


def event(**kwargs):return old.event(**kwargs)
def spawn():return event(target='copy',source=d.SPAWN,eventType='Summon',magnitude=1,timestamp=0)


class EventTests(unittest.TestCase):
    def scan(self,events):return d.ni_events(events,'boss',{'p0':1},{'copy'})

    def test_copy_lifecycle_respects_same_tick_order(self):
        events=[spawn(),event(source=d.SEAL,incomingRawDamage=10),event(target='copy',eventType='Death'),event(source=d.SEAL,incomingRawDamage=10)]
        r=self.scan(events);self.assertEqual([1,0],[h['observedCopiesBefore'] for h in r['ninthSealHits']])

    def test_expiry_is_separate_from_killed_copy(self):
        r=self.scan([spawn(),event(target='copy',eventType='SummonExpired')])
        self.assertEqual(0,len(r['remainingObservedCopies']));self.assertEqual('SummonExpired',r['copyEvents'][-1]['eventType'])

    def test_copy_end_cannot_precede_spawn(self):
        with self.assertRaisesRegex(ValueError,'before spawn'):self.scan([event(target='copy',eventType='Death'),spawn()])

    def test_missing_or_duplicate_spawn_rejected(self):
        for events in ([],[spawn(),spawn()]):
            with self.assertRaises(ValueError):self.scan(events)

    def test_swap_gain_belongs_to_guardian(self):
        r=self.scan([spawn(),event(target='copy',source=d.SWAP,eventType='Buff',magnitude=123)])
        self.assertEqual(123,r['loggedSwapHealthGain']);self.assertEqual('copy',r['healthSwaps'][0]['targetId'])
        self.assertNotIn('healing',r['healthSwaps'][0])

    def test_invalid_swap_target_or_direction_rejected(self):
        for change in ({'target':'p0'},{'magnitude':-1},{'actor':'p0'}):
            args=dict(target='copy',source=d.SWAP,eventType='Buff',magnitude=3);args.update(change)
            with self.assertRaisesRegex(ValueError,'swap target'):self.scan([spawn(),event(**args)])

    def test_power_increments_are_not_current_power(self):
        r=self.scan([spawn(),event(target='boss',source=d.POWER,eventType='Buff',magnitude=83)])
        self.assertEqual(83,r['loggedPowerGain']);self.assertNotIn('currentPower',r)

    def test_power_cannot_be_attributed_to_copy(self):
        with self.assertRaisesRegex(ValueError,'Power notification'):self.scan([spawn(),event(target='copy',source=d.POWER,eventType='Buff')])

    def test_death_notification_is_not_another_seal_hit(self):
        r=self.scan([spawn(),event(source=d.SEAL,incomingRawDamage=10),event(source=d.SEAL,eventType='Death')])
        self.assertEqual(1,len(r['ninthSealHits']))

    def reports(self):
        saved,replay,scenario=old.EventTests().reports()
        for r in (saved,replay):
            for p in r['battle']['preparedParticipants']:p['combatAttributes']={}
            for i in range(1,10):
                r['battle']['summary']['statistics'].append(dict(r['battle']['summary']['statistics'][-1],
                    entityId='boss:summon:niCopy:'+str(i),team='Hostile',isSummonedEntity=True))
        replay['battle']['eventLog']=[event(target='boss:summon:niCopy:'+str(i),source=d.SPAWN,eventType='Summon',timestamp=0) for i in range(1,10)]
        return saved,replay,scenario

    def test_complete_ni_report_and_nine_copies(self):
        result=d.analyze(*self.reports());self.assertEqual(9,len(result['copyStats']));self.assertEqual(9,len(result['ni']['remainingObservedCopies']))

    def test_ni_complete_report_parity(self):
        saved,replay,scenario=self.reports();replay['battle']['seed']+=1
        with self.assertRaisesRegex(ValueError,'complete saved report'):d.analyze(saved,replay,scenario)

    def test_copy_damage_must_reconcile(self):
        saved,replay,scenario=self.reports();replay['battle']['eventLog'][-1]['incomingRawDamage']=1
        with self.assertRaisesRegex(ValueError,'damage does not reconcile'):d.analyze(saved,replay,scenario)

    def test_copies_cannot_cast(self):
        saved,replay,scenario=self.reports();replay['battle']['eventLog'].append(event(actor='boss:summon:niCopy:1',eventType='AbilityUse'))
        with self.assertRaisesRegex(ValueError,'remain inert'):d.analyze(saved,replay,scenario)


class ResourceAndLogTests(unittest.TestCase):
    def test_measured_admission(self):
        entry=dict(referenceReplays=96,referenceReplaySeconds=196.67,referenceArchiveBytes=176289803,resourceReference='ref')
        self.assertAlmostEqual(393.34,d.resource_admission(entry)['projectedSeconds'])

    def test_resource_limits_are_strict(self):
        for seconds,size in [(336,1),(1,2*1024**3),(0,1)]:
            with self.assertRaises(ValueError):d.resource_admission(dict(referenceReplays=96,referenceReplaySeconds=seconds,referenceArchiveBytes=size,resourceReference='ref'))

    def test_parse_requires_native_parity_footer(self):
        with self.assertRaises(ValueError):d.parse(b'{"battle":{}}')

    def test_compression_preserves_complete_native_bytes(self):
        with tempfile.TemporaryDirectory() as folder:
            out=Path(folder);log=out/'one.log';raw=b'{"battle":{}}\nReplay matched the saved input, combat result and Tower outcome.\n';log.write_bytes(raw)
            record=d.compress_verified(log,out)
            self.assertFalse(log.exists());self.assertEqual(raw,gzip.decompress((out/'one.log.gz').read_bytes()));self.assertEqual(len(raw),record['rawBytes'])

    def test_compression_cannot_remove_unowned_or_incomplete_log(self):
        with tempfile.TemporaryDirectory() as folder:
            out=Path(folder);log=out/'one.log';log.write_bytes(b'{}')
            with self.assertRaises(ValueError):d.compress_verified(log,out/'other')
            with self.assertRaises(ValueError):d.compress_verified(log,out)
            self.assertTrue(log.exists())


if __name__=='__main__':unittest.main()
