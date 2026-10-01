"""Guard exact diagnostic pairing, recipient accounting and event windows."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('mixed_diagnostic',Path(__file__).with_name('diagnose-tower-mixed-armor.py'))
d = importlib.util.module_from_spec(spec); spec.loader.exec_module(d)


class MixedDiagnosticTests(unittest.TestCase):
    def test_archived_candidate_requires_exact_registered_plan(self):
        with patch.object(d.io, 'sha', return_value='changed'), patch.object(d.io, 'read') as read:
            with self.assertRaisesRegex(ValueError, 'Unrecognized'): d.load_candidate_plan(Path('plan'), 4)
            read.assert_not_called()
        with patch.object(d.io, 'sha', return_value=d.ARCHIVED_CANDIDATE_PLANS[4]):
            with self.assertRaisesRegex(ValueError, 'Unrecognized'): d.load_candidate_plan(Path('plan'), 2)

    def test_archived_candidate_cannot_be_an_acceptance_or_started_plan(self):
        plan=dict(status='ProposedNotExecuted',usedForAcceptance=False,allocatedReplays=0,newSeeds=0)
        with patch.object(d.io, 'sha', return_value=d.ARCHIVED_CANDIDATE_PLANS[4]), patch.object(d.io, 'read', return_value=plan):
            self.assertEqual(plan,d.load_candidate_plan(Path('plan'),4))
            for key,value in [('status','Complete'),('usedForAcceptance',True),('allocatedReplays',1),('newSeeds',1)]:
                changed={**plan,key:value}
                with patch.object(d.io, 'read', return_value=changed):
                    with self.assertRaisesRegex(ValueError, 'unexecuted descriptive'): d.load_candidate_plan(Path('plan'),4)

    def test_archived_candidate_preserves_raw_identity_and_order(self):
        cells=[dict(id='raw',essences=['A','B'])];scope=dict(execution={'runtime':'a'},settings={'threat':True})
        d.validate_candidate_pair(cells,copy.deepcopy(cells),scope,copy.deepcopy(scope))
        for other in ([],[dict(id='changed',essences=['A','B'])],[dict(id='raw',essences=['B','A'])]):
            with self.assertRaisesRegex(ValueError,'raw loadouts'):d.validate_candidate_pair(cells,other,scope,scope)

    def test_archived_candidate_rejects_runtime_and_settings_changes(self):
        scope=dict(execution={'runtime':'a'},settings={'threat':True})
        for key in ('execution','settings'):
            changed=copy.deepcopy(scope);changed[key]={'changed':True}
            with self.assertRaisesRegex(ValueError,'runtime or combat settings'):d.validate_candidate_pair([],[],changed,scope)

    def test_live_reference_rejects_candidate_or_untracked_catalog_content(self):
        with tempfile.TemporaryDirectory() as folder:
            live=Path(folder);path=live/'abilities.json';path.write_text('original')
            hashes={'abilities.json':d.io.sha(path)}
            d.validate_live_catalog(live,hashes)
            path.write_text('candidate')
            with self.assertRaisesRegex(ValueError,'original live source'):d.validate_live_catalog(live,hashes)
            path.write_text('original');(live/'extra.json').write_text('{}')
            with self.assertRaisesRegex(ValueError,'original live source'):d.validate_live_catalog(live,hashes)

    def test_archived_candidate_preserves_frozen_seed_order_and_output(self):
        output=Path('diagnostic').resolve();selected=[dict(cellId='a',trial={'seed':2}),dict(cellId='b',trial={'seed':1})]
        plan=dict(selected=selected,plannedOutput=str(output))
        d.validate_planned_selection(plan,selected,output)
        for changed in (selected[::-1],selected[:-1],[{**selected[0],'cellId':'other'},selected[1]]):
            with self.assertRaisesRegex(ValueError,'recipe/seed selection'):d.validate_planned_selection(plan,changed,output)
        with self.assertRaisesRegex(ValueError,'output changed'):d.validate_planned_selection(plan,selected,output/'other')

    def test_source_contract_preserves_both_floor_pins(self):
        for floor, expected in d.SOURCES.items():
            cells=[dict(scenario=dict(floorNumber=floor)) for _ in range(98)]
            self.assertEqual(expected,d.validate_source(floor,expected['source'],expected['proposal'],cells))
            for source,proposal in [('changed',expected['proposal']),(expected['source'],'changed')]:
                with self.assertRaisesRegex(ValueError,'Pinned diagnostic'): d.validate_source(floor,source,proposal,cells)

    def test_source_cannot_change_floor_or_drop_controls(self):
        expected=d.SOURCES[4]
        for count,floor in [(97,4),(98,2)]:
            with self.assertRaisesRegex(ValueError,'family/floor'):
                d.validate_source(4,expected['source'],expected['proposal'],[dict(scenario=dict(floorNumber=floor))]*count)
        with self.assertRaisesRegex(ValueError,'Unsupported'): d.validate_source(6,'','',[])

    def reports(self):
        stats = [dict(entityId=str(i),entityName=str(i),firstDeathTick=None,**{k:0 for k in d.base.FIELDS}) for i in range(5)]
        report = dict(succeeded=False,guardianHealthRemainingPercent=50,battle=dict(seed=17,ticksPerSecond=10,
            preparedParticipants=[dict(name=str(i),slot=dict(side='Friendly',slotId=str(i)),combatAttributes={'Armor':30}) for i in range(5)] +
                [dict(name='guardian',slot=dict(side='Hostile',slotId='guardian'))],
            summary=dict(durationSeconds=30,statistics=stats),eventLog=None))
        scenario = dict(party=[dict(partySlot=i+1,build={'id':str(i)}) for i in range(5)])
        event = dict(source='hit',statsSource='ability',actorId='guardian',targetId='0',timestamp=149,eventType='Damage',
                     damageType='Physical',magnitude=3,details='',**{k:0 for k in d.MEASURES})
        event.update(incomingRawDamage=5,physicalMitigationPrevented=2,finalHealthDamage=3)
        stats[0].update(incomingRawDamage=5,physicalMitigationPrevented=2,finalHealthDamage=3)
        replay = copy.deepcopy(report); replay['battle']['eventLog']=[event]
        return report,replay,scenario

    def test_five_original_recipients_reconcile(self):
        saved,replay,scenario = self.reports(); result=d.analyze(saved,replay,scenario)
        self.assertEqual(3,result['windows']['15']['finalHealthDamage'])
        self.assertEqual(2,result['windows']['15']['physicalMitigationPrevented'])
        self.assertEqual(5,len(result['actors']))

    def test_default_ten_character_guard_is_preserved(self):
        saved,_,_ = self.reports()
        with self.assertRaisesRegex(ValueError,'original friendly'): d.base.initial_stats(saved)

    def test_changed_saved_outcome_rejected(self):
        saved,replay,scenario = self.reports(); replay['succeeded']=True
        with self.assertRaisesRegex(ValueError,'complete saved report'): d.analyze(saved,replay,scenario)

    def test_wrong_recipient_mitigation_rejected(self):
        saved,replay,scenario = self.reports(); replay['battle']['eventLog'][0]['physicalMitigationPrevented']=3
        with self.assertRaisesRegex(ValueError,'mitigation/raw'): d.analyze(saved,replay,scenario)

    def test_fixed_window_boundary_is_exclusive(self):
        saved,replay,scenario = self.reports(); replay['battle']['eventLog'][0]['timestamp']=150
        result=d.analyze(saved,replay,scenario)
        self.assertEqual(0,result['windows']['15']['finalHealthDamage'])
        self.assertEqual(3,result['windows']['20']['finalHealthDamage'])

    def test_party_identity_mismatch_rejected(self):
        saved,replay,scenario = self.reports(); scenario['party'][0]['build']['id']='changed'
        with self.assertRaisesRegex(ValueError,'identities differ'): d.analyze(saved,replay,scenario)

    def test_summon_damage_is_not_counted_as_original_party(self):
        saved,replay,scenario = self.reports()
        for value in (saved,replay):
            stats=copy.deepcopy(value['battle']['summary']['statistics'][0]); stats['entityId']='summon'
            value['battle']['summary']['statistics'].append(stats)
        event=copy.deepcopy(replay['battle']['eventLog'][0]); event['targetId']='summon'; replay['battle']['eventLog'].append(event)
        self.assertEqual(3,d.analyze(saved,replay,scenario)['windows']['all']['finalHealthDamage'])

    def test_death_context_respects_event_order_with_same_tick(self):
        saved,replay,scenario = self.reports()
        for value in (saved,replay): value['battle']['summary']['statistics'][0]['firstDeathTick']=149
        death=copy.deepcopy(replay['battle']['eventLog'][0]); death.update(eventType='Death',magnitude=0,**{k:0 for k in d.MEASURES})
        replay['battle']['eventLog'].append(death)
        later=copy.deepcopy(death); later.update(source='after-death',eventType='Damage',incomingRawDamage=2,finalHealthDamage=2)
        replay['battle']['eventLog'].append(later)
        for value in (saved,replay): value['battle']['summary']['statistics'][0].update(incomingRawDamage=7,finalHealthDamage=5)
        self.assertEqual('hit',d.analyze(saved,replay,scenario)['actors'][0]['lastDamageBeforeFirstDeath']['source'])

    def test_seed_order_preserved_without_outcome_selection(self):
        seeds=list(range(128)); seeds[0]=-19
        self.assertEqual(seeds[:8],d.seed_panel(seeds))
        with self.assertRaisesRegex(ValueError,'128-seed'): d.seed_panel(seeds[:-1])
        with self.assertRaisesRegex(ValueError,'128-seed'): d.seed_panel([1]*128)

    def family(self):
        cells=[]; variants=[]; rows=[]
        for parent in ('A','B'):
            base=dict(id=parent+'/baseline',composition=parent,scenario=dict(seeds=[],party=[dict(partySlot=i,build=dict(id=str(i),essenceIds=[parent,'raw'],equipment=[])) for i in range(1,6)]))
            cells.append(base); full=copy.deepcopy(base); full['id']=parent+'/armor'; cells.append(full)
            for count in (2,4):
                for choice in ('first','second'):
                    cell=copy.deepcopy(base); cell['id']=parent+'/'+str(count)+'/'+choice
                    for member in cell['scenario']['party'][:count]: member['build']['equipment']=[{'specialized':True}]
                    cells.append(cell); variants.append(dict(sourceId=full['id'],baselineId=base['id'],armorPartySlots=list(range(1,count+1)),cell=cell))
                    rows.append(dict(id=cell['id'],wins=1 if choice=='first' else 2,meanGuardianHealth=0))
        return cells,variants,rows

    def test_recipe_selection_keeps_exact_eight_cells(self):
        cells,variants,rows=self.family(); before=copy.deepcopy(cells); actual=d.select_cells(cells,variants,rows)
        self.assertEqual(before,cells); self.assertEqual(8,len(actual))
        self.assertEqual('A/2/second',actual[1]['cellId'])

    def test_selection_rejects_essence_order_change(self):
        cells,variants,rows=self.family()
        next(c for c in cells if c['id']=='A/2/second')['scenario']['party'][0]['build']['essenceIds'].reverse()
        with self.assertRaisesRegex(ValueError,'more than equipment'): d.select_cells(cells,variants,rows)

    def test_two_labels_for_same_actual_composition_rejected(self):
        cells,variants,rows=self.family()
        for cell in cells:
            if cell['composition']=='B':
                for member in cell['scenario']['party']: member['build']['essenceIds']=['raw','A']
        with self.assertRaisesRegex(ValueError,'Two actual parent'): d.select_cells(cells,variants,rows)


if __name__ == '__main__': unittest.main()
