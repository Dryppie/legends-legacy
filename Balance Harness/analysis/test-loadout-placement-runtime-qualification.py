"""Runtime coverage and fail-closed equivalence checks; no preparation or combat."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import subprocess
import unittest
from unittest.mock import patch

path = Path(__file__).with_name('qualify-loadout-placement-runtime.py')
spec = importlib.util.spec_from_file_location('placement_runtime_test_subject', path)
q = importlib.util.module_from_spec(spec); spec.loader.exec_module(q)


class CoverageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.legacy = q.read(q.CAPTURE/'probe-scenarios.json')
        cls.requests = [q.read(q.PREVIEW/f'root-{n:02d}/request.json') for n in range(1,13)]
        cls.exports = [q.read(q.PREVIEW/f'root-{n:02d}/batches.json') for n in range(1,13)]

    def test_all_catalogue_recipes_preserve_actor_fields_and_legacy_rows(self):
        rows = q.probes(self.legacy,self.requests,self.exports)
        self.assertEqual(rows[:568],self.legacy)
        scope = self.requests[0]['context']['scope']
        benchmark = self.requests[0]['context']['benchmarkReferenceId']
        anchor = next(r['scenario'] for r in scope['references'] if r['id'] == benchmark)
        recipes = self.exports[0]['arms'][-1]['loadoutPlacementCatalogue']['recipes']
        self.assertEqual(len(rows),1049)
        for row,recipe in zip(rows[568:806],recipes):
            self.assertEqual(row['seeds'],self.legacy[0]['seeds'])
            self.assertEqual(len(row['scenario']['party']),10)
            for actual,original in zip(row['scenario']['party'],anchor['party']):
                expected = copy.deepcopy(original)
                expected['build']['essenceIds'] = recipe['party']['builds'][str(actual['partySlot'])]
                self.assertEqual(actual,expected)
        for row,ref in zip(rows[806:809],scope['references']):
            expected=copy.deepcopy(ref['scenario']); expected['seeds']=self.legacy[0]['seeds']
            self.assertEqual(row['scenario'],expected)
        self.assertEqual(len(rows[809:]),240)

    def test_missing_legacy_arm_or_preview_root_rejected(self):
        for legacy,requests,exports in ((self.legacy[:-1],self.requests,self.exports),
                                        (self.legacy,self.requests[:-1],self.exports),
                                        (self.legacy,self.requests,self.exports[:-1])):
            with self.subTest(lengths=(len(legacy),len(requests),len(exports))), self.assertRaises(Exception):
                q.probes(legacy,requests,exports)

    def test_unknown_probe_value_rejected(self):
        rows=copy.deepcopy(self.legacy)
        for row in rows: row['seeds']=[12345678,12345679]
        with self.assertRaisesRegex(Exception,'historical'): q.probes(rows,self.requests,self.exports)

    def test_duplicate_or_missing_placement_recipe_rejected(self):
        for missing in (True,False):
            exports=copy.deepcopy(self.exports)
            recipes=exports[0]['arms'][-1]['loadoutPlacementCatalogue']['recipes']
            if missing: recipes.pop()
            else: recipes[-1]=recipes[0]
            with self.subTest(missing=missing), self.assertRaisesRegex(Exception,'catalogue'):
                q.probes(self.legacy,self.requests,exports)

    def test_changed_family_on_later_root_rejected(self):
        exports=copy.deepcopy(self.exports)
        exports[11]['arms'][-1]['loadoutPlacementCatalogue']['recipes'][0]['party']['id']='changed'
        with self.assertRaisesRegex(Exception,'across roots'): q.probes(self.legacy,self.requests,exports)

    def test_underfilled_saved_wave_rejected(self):
        exports=copy.deepcopy(self.exports); exports[9]['arms'][-1]['teams'].pop()
        with self.assertRaisesRegex(Exception,'preview'): q.probes(self.legacy,self.requests,exports)


class EquivalenceTests(unittest.TestCase):
    def setUp(self):
        self.captured=q.read(q.CAPTURE/'candidate-qualification.json')
        self.context=q.read(q.PREVIEW/'root-01/request.json')['context']
        self.a=copy.deepcopy(self.captured); self.b=copy.deepcopy(self.captured)
        self.harness='c'*64; self.b['execution']['assemblyHashes']['BalanceHarness']=self.harness
        for item in (self.a,self.b): item.update(materializations=2098,nativePreparations=2098,
                materializationSeconds=[.01]*2098,preparationSeconds=[.01]*2098)
        self.before=[dict(ordinal=i+1,inputHash='a'*64,participantsHash='b'*64) for i in range(2098)]
        self.after=copy.deepcopy(self.before)

    def check(self):
        q.qualify(self.a,self.b,self.before,self.after,self.captured,self.context,self.harness)

    def test_identical_inputs_and_participants_with_only_harness_replaced_pass(self): self.check()

    def test_changed_input_rejected(self):
        self.after[800]['inputHash']='d'*64
        with self.assertRaisesRegex(Exception,'materializations'): self.check()

    def test_changed_prepared_participant_rejected(self):
        self.after[-1]['participantsHash']='d'*64
        with self.assertRaisesRegex(Exception,'preparations'): self.check()

    def test_missing_preparation_rejected_even_if_both_sides_match(self):
        self.before.pop(); self.after.pop()
        with self.assertRaisesRegex(Exception,'incomplete'): self.check()

    def test_duplicate_ordinal_rejected(self):
        self.before[-1]['ordinal']=1; self.after[-1]['ordinal']=1
        with self.assertRaisesRegex(Exception,'incomplete'): self.check()

    def test_gameplay_drift_rejected(self):
        self.b['execution']['assemblyHashes']['Services.LL']='d'*64
        with self.assertRaisesRegex(Exception,'gameplay'): self.check()

    def test_forged_baseline_rejected(self):
        self.a['executionHash']='d'*64
        with self.assertRaisesRegex(Exception,'baseline'): self.check()

    def test_combat_or_new_value_rejected(self):
        for field in ('fights','newValues'):
            self.b[field]=1
            with self.subTest(field=field), self.assertRaisesRegex(Exception,'preparation'): self.check()
            self.b[field]=0

    def test_incomplete_legacy_replay_rejected(self):
        self.b['alliedReplayedRoots']=11
        with self.assertRaisesRegex(Exception,'legacy replay'): self.check()

    def test_settings_drift_rejected(self):
        self.b['settingsHash']='d'*64
        with self.assertRaisesRegex(Exception,'settings'): self.check()

    def test_missing_or_invalid_measurement_rejected(self):
        for values in ([.01]*2097,[.01]*2097+[float('nan')]):
            self.b['preparationSeconds']=values
            with self.subTest(count=len(values)), self.assertRaisesRegex(Exception,'finite'): self.check()


class ExecutionGuards(unittest.TestCase):
    def test_powershell_failure_handler_executes_for_wrapped_provider_exception(self):
        text=(q.HERE/'loadout-placement-runtime-context.ps1').read_text()
        start=text.index('        $providerFailure = $_.Exception')
        end=text.index('        $rejected = $true',start)+len('        $rejected = $true')
        # Exercise the actual handler text. Parsing alone misses assignment to reserved variables.
        script="$ErrorActionPreference='Stop'; $rejected=$false; try { throw [System.Reflection.TargetInvocationException]::new([NotSupportedException]::new('Captured content providers do not support content-read accounting.')) } catch {\n"+text[start:end]+"\n}; if (-not $rejected) { exit 2 }"
        result=subprocess.run(['pwsh','-NoProfile','-Command',script],capture_output=True,text=True,timeout=15)
        self.assertEqual(result.returncode,0,result.stdout+result.stderr)

    def test_external_manifests_match_the_preceding_sealed_handoff(self):
        self.assertEqual(q.sha(q.PRIOR),q.PRIOR_PIN)
        prior=q.read(q.PRIOR)
        for root,pin in q.PINS.items():
            self.assertEqual(pin,prior['preservedHistoricalPins'][(root/'files.json').relative_to(q.ROOT).as_posix()])
            self.assertEqual(q.sha(root/'files.json'),pin)

    def test_unpinned_execution_rejected_before_attempt_directory(self):
        with patch.object(q,'sha',return_value='actual'), patch.object(q,'validate_declaration') as validate:
            with self.assertRaisesRegex(Exception,'pinned'): q.run('wrong')
            validate.assert_not_called()

    def test_existing_attempt_cannot_be_replaced(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); sentinel=root/'failed-first-attempt'; sentinel.write_text('retain')
            with patch.object(q,'PACKAGE',root), patch.object(q,'sha',return_value='pin'), patch.object(q,'read',return_value={}), patch.object(q,'validate_declaration'):
                with self.assertRaisesRegex(Exception,'retry'): q.run('pin')
            self.assertEqual(sentinel.read_text(),'retain')


if __name__ == '__main__': unittest.main()
