"""Fail-closed aggregate statistics, admission and application contract checks."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('aggregate', Path(__file__).with_name('tower-balance-aggregate.py'))
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)


class AggregateTests(unittest.TestCase):
    def setUp(self):
        self.cells = [dict(id=str(i), composition=str(i), gear='partial' if i < 2 else 'full',
                           scenario=dict(party=[dict(partySlot=1, build=dict(essenceIds=[str(i)]))])) for i in range(115)]
        self.batches = [[dict(id=c['id'], samples=128, wins=25 if int(c['id']) < 2 else 40)
                         for c in self.cells] for _ in range(4)]

    def assess(self):
        return a.assess(self.cells, self.batches, 4, 128, 'partial')

    def test_complete_panel_passes_although_each_batch_ceiling_can_fail(self):
        for batch in self.batches:
            batch[-1]['wins'] = 50
        result = self.assess()
        self.assertEqual('Pass', result['verdict'])
        self.assertGreater(a.interval(50, 128, 115)[1], .5)
        self.assertLess(result['maximumUpper'], .5)
        self.assertEqual(512, result['samples'])

    def test_exact_unchanged_512_gates(self):
        minimum = min(w for w in range(513) if a.interval(w, 512, 115)[0] >= .1)
        maximum = max(w for w in range(513) if a.interval(w, 512, 115)[1] <= .5)
        self.assertEqual((76, 216), (minimum, maximum))

    def test_one_over_ceiling_rejects_whole_family(self):
        for batch, wins in zip(self.batches, [54, 54, 54, 55]):
            batch[-1]['wins'] = wins
        self.assertEqual('NotAccepted', self.assess()['verdict'])

    def test_two_true_compositions_required(self):
        self.cells[1]['scenario'] = copy.deepcopy(self.cells[0]['scenario'])
        self.assertEqual('NotAccepted', self.assess()['verdict'])

    def test_gear_duplicates_and_essence_order_do_not_add_compositions(self):
        self.cells[0]['scenario']['party'][0]['build']['essenceIds'] = ['a', 'b']
        self.cells[1]['scenario']['party'][0]['build']['essenceIds'] = ['b', 'a']
        self.assertEqual(1, self.assess()['qualifyingPartialCompositions'])

    def test_one_partial_composition_below_lower_bound_rejects(self):
        for batch, wins in zip(self.batches, [18, 19, 19, 19]):
            batch[1]['wins'] = wins
        self.assertEqual('NotAccepted', self.assess()['verdict'])

    def test_full_gear_cannot_supply_partial_minimum(self):
        self.cells[1]['gear'] = 'full'
        self.assertEqual('NotAccepted', self.assess()['verdict'])

    def test_missing_extra_or_incomplete_batches(self):
        for batches in (self.batches[:3], self.batches + [self.batches[0]], [b[:-1] for b in self.batches]):
            with self.subTest(length=len(batches)), self.assertRaises(ValueError):
                a.assess(self.cells, batches, 4, 128, 'partial')

    def test_repeated_or_reordered_rows_rejected(self):
        for rows in ([self.batches[0][0]] * 115, self.batches[0][::-1]):
            with self.subTest(rows=rows[0]['id']), self.assertRaises(ValueError):
                a.assess(self.cells, [rows, *self.batches[1:]], 4, 128, 'partial')

    def test_invalid_counts_rejected(self):
        for wins in (-1, 129, True, 1.5):
            self.batches[0][0]['wins'] = wins
            with self.subTest(wins=wins), self.assertRaises(ValueError):
                self.assess()

    def test_wrong_sample_count_rejected(self):
        self.batches[0][0]['samples'] = 129
        with self.assertRaises(ValueError):
            self.assess()

    def test_resource_caps_and_headroom(self):
        self.assertEqual(14720, a.resource_admission(dict(seconds=250, completed=14720), 128, 115, 5000000)['fights'])
        for seconds, family, size in [(336, 115, 1), (1, 157, 1), (1, 115, 1024**3)]:
            with self.subTest(seconds=seconds, family=family, size=size), self.assertRaises(ValueError):
                a.resource_admission(dict(seconds=seconds, completed=14720), 128, family, size)

    def test_repeated_or_outside_study_paths_rejected(self):
        phases = {p: [str(a.io.ROOT/f'TestResults/tower-balance-pass-fixture-{p}-{i}-study-20260929') for i in range(4)]
                  for p in ('screen', 'confirm')}
        a.validate_paths(phases, a.io.ROOT)
        phases['confirm'][0] = phases['screen'][0]
        with self.assertRaises(ValueError):
            a.validate_paths(phases, a.io.ROOT)
        phases['confirm'][0] = str(a.io.ROOT/'outside-study-20260929')
        with self.assertRaises(ValueError):
            a.validate_paths(phases, a.io.ROOT)

    def test_application_rejects_screen_and_failed_confirmation_before_reconstruction(self):
        for phase, verdict in [('screen', 'Pass'), ('confirm', 'NotAccepted')]:
            with patch.object(a.io, 'sha', return_value='pin'), patch.object(a.io, 'read', return_value=dict(
                    phase=phase, status='Verified', assessment=dict(verdict=verdict))), patch.object(a, 'audit_phase') as audit:
                with self.assertRaises(ValueError):
                    a.application_batch('saved.json', 'pin', 'batch')
                audit.assert_not_called()

    def test_application_rejects_changed_evidence_or_unlisted_batch(self):
        saved = dict(phase='confirm', status='Verified', assessment=dict(verdict='Pass'), declaration='frozen',
                     declarationSha256='declaration-pin', batches=[dict(source='batch')])
        with patch.object(a.io, 'sha', return_value='pin'), patch.object(a.io, 'read', return_value=saved):
            with patch.object(a, 'audit_phase', return_value={**saved, 'phase': 'screen'}), self.assertRaises(ValueError):
                a.application_batch('saved.json', 'pin', 'batch')
            with patch.object(a, 'audit_phase', return_value=saved):
                with self.assertRaises(ValueError):
                    a.application_batch('saved.json', 'pin', 'other-batch')
                self.assertEqual(saved['batches'][0], a.application_batch('saved.json', 'pin', 'batch')[0])

    def test_batch_identity_and_seeds_are_fail_closed(self):
        # Real file reads for identity/provenance/history; the native outcome audit is
        # stubbed because these adversarial checks must not allocate or run combat.
        with tempfile.TemporaryDirectory() as temp:
            study = Path(temp)/'fixture-study-date'
            owned = a.owner(study)
            study.mkdir(); owned.mkdir()
            d = dict(runtime={'identity': 'current'}, settings={'version': 1}, candidateContentHashes={'catalog': 'pin'},
                     floor=2, candidatePlan={'version': 'tower-ability-coefficients-v1', 'coefficient': .35}, samplesPerBatch=128)
            q = dict(mode='screen', floor=2, searchSeeds=[], seeds=list(range(128)), inputHashes={})
            provenance = owned/'ability-candidate-provenance.json'
            a.io.write(provenance, dict(plan=d['candidatePlan']))
            q['inputHashes'][str(provenance)] = a.io.sha(provenance)
            scope = dict(execution=d['runtime'], settings=d['settings'], contentHashes=d['candidateContentHashes'])
            for path, data in [(study/'cells.json', self.cells), (study/'scope.json', scope), (study/'request.json', q),
                               (owned/'independent-audit.json', {'verified': True})]:
                a.io.write(path, data)
            for change in ('runtime', 'catalog', 'plan', 'family', 'historical-seeds', 'repeated-seeds'):
                broken = copy.deepcopy(d)
                excluded = set()
                if change == 'runtime': broken['runtime'] = {'identity': 'other'}
                if change == 'catalog': broken['candidateContentHashes'] = {'catalog': 'other'}
                if change == 'plan': broken['candidatePlan']['coefficient'] = .40
                if change == 'historical-seeds': excluded = {0}
                if change == 'repeated-seeds':
                    duplicate = copy.deepcopy(q)
                    duplicate['seeds'][1] = duplicate['seeds'][0]
                    (study/'request.json').write_text(__import__('json').dumps(duplicate), encoding='utf-8')
                cells = self.cells[:-1] if change == 'family' else self.cells
                with self.subTest(change=change), patch.object(a.io, 'audit', return_value={'verified': True}), self.assertRaises(ValueError):
                    a.verify_batch(broken, cells, study, 'screen', excluded)

    def test_confirmation_admission_rejects_failed_screen_before_reservations(self):
        with patch.object(a, 'declaration', return_value=({'liveCatalogPins': {}, 'batchCount': 4}, [], set())), \
             patch.object(a, 'audit_phase', return_value={'assessment': {'verdict': 'NotAccepted'}}), \
             patch.object(a.io, 'history') as history:
            with self.assertRaisesRegex(ValueError, 'Passing complete screening'):
                a.admit_batch('declaration', 'pin', 'confirm', 0)
            history.assert_not_called()

    def test_confirmation_audit_rejects_failed_screen_before_reading_confirmation(self):
        # Real audit recursion still runs; a missing first screen cannot be hidden
        # behind a purportedly accepted confirmation or an empty selected subset.
        d = {'phases': {'screen': ['missing-screen'], 'confirm': ['confirm']}}
        with patch.object(a, 'declaration', return_value=(d, [], set())), \
             patch.object(a, 'verify_batch', side_effect=ValueError('Missing screen')) as batch:
            with self.assertRaisesRegex(ValueError, 'Missing screen'):
                a.audit_phase('declaration', 'pin', 'confirm')
            self.assertEqual('screen', batch.call_args.args[3])


class LimitedEquipmentAggregateTests(unittest.TestCase):
    def setUp(self):
        self.cells = [dict(id=str(i), composition=f'label-{i}', gear='untrusted-label', scenario=dict(party=[
            dict(partySlot=slot, build=dict(essenceIds=[str(i), 'shared'], equipment=[
                dict(definitionId=f'item.spec.{j}') for j in range(4 if slot <= 2 or i > 1 else 0)]))
            for slot in range(1, 4)])) for i in range(132)]
        self.batches = [[dict(id=c['id'], samples=128, wins=25 if i < 2 else 40)
                         for i,c in enumerate(self.cells)] for _ in range(4)]

    def assess(self):return a.assess(self.cells, self.batches, 4, 128, None, a.LIMITED_EQUIPMENT)

    def test_actual_items_qualify_despite_labels(self):
        result=self.assess()
        self.assertEqual('Pass',result['verdict']);self.assertEqual(2,result['eligibleRecipes'])
        self.assertEqual(2,result['qualifyingPartialCompositions'])

    def test_exact_132_recipe_gates(self):
        self.assertEqual((76,215),(next(w for w in range(513) if a.interval(w,512,132)[0]>=.1),
                                  max(w for w in range(513) if a.interval(w,512,132)[1]<=.5)))

    def test_items_characters_and_essence_order_cannot_supply_second_composition(self):
        original=copy.deepcopy(self.cells)
        for change in ('items','characters','essence-order','lower'):
            self.cells=copy.deepcopy(original);party=self.cells[1]['scenario']['party']
            if change=='items':party[0]['build']['equipment'].append(dict(definitionId='item.spec.extra'))
            if change=='characters':party[2]['build']['equipment'].append(party[0]['build']['equipment'].pop())
            if change=='essence-order':
                for m in party:m['build']['essenceIds']=['shared','0']
            if change=='lower':
                for batch in self.batches:batch[1]['wins']=18
            with self.subTest(change=change):self.assertEqual('NotAccepted',self.assess()['verdict'])

    def test_ineligible_control_still_rejects_complete_panel(self):
        for batch in self.batches:batch[-1]['wins']=54
        self.assertEqual('NotAccepted',self.assess()['verdict'])

    def test_contract_cannot_be_weakened_or_combined_with_gear_label(self):
        for contract,gear in [({},None),({**a.LIMITED_EQUIPMENT,'maximumSpecializedItems':9},None),
                              ({**a.LIMITED_EQUIPMENT,'maximumSpecializedCharacters':3},None),(a.LIMITED_EQUIPMENT,'partial')]:
            with self.subTest(contract=contract,gear=gear),self.assertRaises(ValueError):
                a.assess(self.cells,self.batches,4,128,gear,contract)

    def test_health_candidate_requires_joint_validation_and_equipment_contract(self):
        d=dict(candidatePlan=dict(version='tower-health-pressure-candidate-v1'),floor=4,gear=None,
               equipmentEligibility=a.LIMITED_EQUIPMENT)
        with patch.object(a.io,'health_pressure_module') as health,patch.object(a.io,'ability_module') as ability:
            a.validate_candidate(d,Path('content'));health.return_value.expected.assert_called_once();ability.assert_not_called()
        for change in ({'equipmentEligibility':None},{'gear':'label'}, {'candidatePlan':{'version':'unknown'}}):
            with self.subTest(change=change),self.assertRaises(ValueError):a.validate_candidate({**d,**change},Path('content'))

    def test_health_batch_rejects_wrong_provenance_even_when_plan_matches(self):
        with tempfile.TemporaryDirectory() as temp:
            study=Path(temp)/'health-study-date';owned=a.owner(study);study.mkdir();owned.mkdir()
            d=dict(runtime={},settings={},candidateContentHashes={},floor=4,samplesPerBatch=128,
                   candidatePlan=dict(version='tower-health-pressure-candidate-v1'))
            provenance=owned/'ability-candidate-provenance.json';a.io.write(provenance,dict(plan=d['candidatePlan']))
            for path,data in [(study/'cells.json',self.cells),(study/'scope.json',dict(execution={},settings={},contentHashes={})),
                (study/'request.json',dict(mode='screen',floor=4,searchSeeds=[],seeds=list(range(128)),
                 inputHashes={str(provenance):a.io.sha(provenance)})),(owned/'independent-audit.json',{})]:a.io.write(path,data)
            with patch.object(a.io,'audit',return_value={}),self.assertRaisesRegex(ValueError,'candidate kind'):
                a.verify_batch(d,self.cells,study,'screen',set())


class AppliedParityTests(unittest.TestCase):
    def setUp(self):
        self.runtime = {'assemblyHashes': {'Combat': 'current'}}
        self.batches = [dict(source=f'batch-{i}', manifestSha256=f'manifest-{i}', audit=f'audit-{i}',
                             auditSha256=f'audit-pin-{i}', fights=14720, seeds=list(range(i*128, (i+1)*128))) for i in range(4)]
        self.receipts = []
        for i, batch in enumerate(self.batches):
            self.receipts.append(dict(
                request=dict(source=batch['source'], manifestPin=batch['manifestSha256'], audit=batch['audit'],
                             auditPin=batch['auditSha256'], aggregatePin='aggregate'),
                result=dict(status='AggregateInputsAndReplaysVerified', manifestPin=batch['manifestSha256'],
                            matchedInputs=14720, fullReplays=115, newSeeds=0, execution=self.runtime),
                completion=dict(status='Verified', resultSha256=f'result-{i}', matchedInputs=14720, fullReplays=115,
                                newSeeds=0, aggregateSha256='aggregate', floorFileSha256='tower'),
                process=dict(exitCode=0, timedOut=False, activeProcesses=0), resultSha256=f'result-{i}'))

    def validate(self):
        return a.validate_applied_parity(self.receipts, self.batches, 'aggregate', self.runtime)

    def test_every_batch_is_required_for_totals(self):
        self.assertEqual(dict(matchedInputs=58880, fullReplays=460, floorFileSha256='tower'), self.validate())

    def test_missing_repeated_or_reordered_receipts_rejected(self):
        original = self.receipts
        for receipts in (original[:3], [original[0]]*4, original[::-1]):
            self.receipts = receipts
            with self.assertRaises(ValueError): self.validate()

    def test_incomplete_runtime_or_parity_receipts_rejected(self):
        original = copy.deepcopy(self.receipts)
        for section, field, value in [('result', 'status', 'AppliedInputsAndReplaysVerified'),
                ('completion', 'matchedInputs', 14719), ('result', 'fullReplays', 114),
                ('completion', 'newSeeds', 1), ('result', 'execution', {'other': True}),
                ('completion', 'resultSha256', 'other'), ('process', 'exitCode', 1),
                ('process', 'timedOut', True), ('process', 'activeProcesses', 1),
                ('completion', 'floorFileSha256', 'other')]:
            self.receipts = copy.deepcopy(original)
            self.receipts[0][section][field] = value
            with self.subTest(field=field), self.assertRaises(ValueError): self.validate()

    def test_substituted_confirmation_or_aggregate_rejected(self):
        original = copy.deepcopy(self.receipts)
        for field in ('source', 'manifestPin', 'audit', 'auditPin', 'aggregatePin'):
            self.receipts = copy.deepcopy(original)
            self.receipts[0]['request'][field] = 'other'
            with self.subTest(field=field), self.assertRaises(ValueError): self.validate()

    def test_applied_catalog_requires_all_pinned_receipts_before_archive_reads(self):
        accepted = dict(version='applied-tower-aggregate-v1', declaration='decl', screen='screen', confirm='confirm',
                        completion='done', parityOwners=['one', 'two', 'three', 'four'])
        with patch.object(a.io, 'read') as read, self.assertRaisesRegex(ValueError, 'Incomplete accepted'):
            a.applied_catalog(dict(acceptedAggregate=accepted, receiptPins={}))
        read.assert_not_called()

    def test_unapplied_incomplete_or_different_catalog_completion_rejected(self):
        valid = dict(status='AppliedAndVerified', newSeeds=0, matchedInputs=58880, fullReplays=460,
                     afterAbilitiesSha256='applied', finalExclusions=916697)
        summary = dict(matchedInputs=58880, fullReplays=460)
        hashes = {'combat/abilities.json': 'applied'}
        a.validate_applied_completion(valid, summary, hashes, 916697)
        for field, value in [('status', 'ConfirmedIsolatedFeastPressure'), ('newSeeds', 1),
                ('matchedInputs', 14720), ('fullReplays', 115), ('afterAbilitiesSha256', 'unapplied'),
                ('finalExclusions', 916696)]:
            with self.subTest(field=field), self.assertRaisesRegex(ValueError, 'not fully applied'):
                a.validate_applied_completion({**valid, field: value}, summary, hashes, 916697)

    def test_joint_completion_requires_applied_tower_hash(self):
        valid=dict(status='AppliedAndVerified',newSeeds=0,matchedInputs=67584,fullReplays=528,
                   afterAbilitiesSha256='abilities',afterTowerSha256='tower',finalExclusions=919548)
        summary=dict(matchedInputs=67584,fullReplays=528)
        hashes={'combat/abilities.json':'abilities','world-tower/tower-floors.json':'tower'}
        a.validate_applied_completion(valid,summary,hashes,919548,tower_changed=True)
        for pin in (None,'other'):
            with self.assertRaises(ValueError):a.validate_applied_completion({**valid,'afterTowerSha256':pin},summary,hashes,919548,tower_changed=True)


if __name__ == '__main__':
    unittest.main()
