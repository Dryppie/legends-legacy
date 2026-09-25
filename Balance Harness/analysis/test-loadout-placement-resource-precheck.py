"""Pure cost-evidence and fail-closed prequalification tests. No runtime sampling."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec=importlib.util.spec_from_file_location('precheck',Path(__file__).with_name('loadout-placement-resource-precheck.py'))
p=importlib.util.module_from_spec(spec); spec.loader.exec_module(p)


def inputs():
    paths={'design':p.DESIGN,'inherited-forecast':p.FLOOR+'/resource-forecast.json',
        'audit-probe':p.PROBE+'/resource-assessment.json','probe-previous-forecast':p.PROBE+'/previous-resource-forecast.json',
        'pilot-result':p.PILOT+'/result.json'}
    for label,directory in [('old',p.OLD),('new',p.NEW),('pilot',p.PILOT)]:
        paths[label+'-costs']=directory+'/completion.json'; paths[label+'-closeout']=directory+'/closeout.json'
        if label != 'pilot':
            paths[label+'-context']=directory+'/source/context.json'; paths[label+'-settings']=directory+'/source/settings.json'
    return {key:p.read(p.ROOT/path) for key,path in paths.items()}


def matched():
    value=inputs(); value['old-context']=copy.deepcopy(value['new-context']); value['old-settings']=copy.deepcopy(value['new-settings'])
    # Literal tests only: equivalent phase costs, preserving receipt consistency.
    value['new-costs']=dict(value['old-costs'],version='tower-loadout-placement-comparison-v1')
    value['new-closeout']=dict(value['old-closeout'],version='tower-loadout-placement-comparison-v1')
    return value


class PrecheckTests(unittest.TestCase):
    def test_older_probe_pin_follows_authenticated_forecast_chain(self):
        data=inputs(); pin=data['inherited-forecast']['resourceProbeManifestSha256']
        self.assertEqual(pin,p.source_manifest_pin(p.PROBE,{},data))
        self.assertEqual(p.sha(p.ROOT/p.PROBE/'files.json'),pin)
        with self.assertRaisesRegex(ValueError,'Missing source'): p.source_manifest_pin('unknown',{},data)

    def test_retained_fixtures_are_incomparable_and_cannot_qualify(self):
        result=p.assess(inputs())
        self.assertEqual('ResourcePrequalificationNotReady',result['status'])
        self.assertEqual({'partySize','essenceSlots','templateActors','physicalContext'},set(result['fixtureMismatches']))
        self.assertFalse(result['comparableFixtures']); self.assertIsNone(result['qualifiedCurrentForecast'])
        self.assertFalse(result['admitted']); self.assertFalse(result['qualificationStarted'])

    def test_sensitivity_is_never_promoted_to_a_current_forecast(self):
        result=p.assess(inputs())
        self.assertFalse(result['sensitivity']['usableForAdmission'])
        self.assertIn('nativeBytes',result['sensitivity']['exceededPartitions'])
        self.assertIn('auditSeconds',result['sensitivity']['exceededPartitions'])
        self.assertIn('Unmatched-fixture',result['sensitivity']['interpretation'])

    def test_matching_affordable_evidence_still_requires_runtime_qualification(self):
        result=p.assess(matched())
        self.assertEqual('ReadyForBoundedQualificationOnly',result['status'])
        self.assertFalse(result['admitted']); self.assertFalse(result['runtimeQualified'])

    def test_bookkeeping_differences_do_not_change_physical_matching(self):
        value=matched(); c=value['old-context']; c['rootSeed']+=1
        c['scope'].update(id='another-label',startsAt='2020-01-01',executionHash='different',excludedCombatSeeds=[123])
        c['scope']['generation']['seeds']=[123]
        self.assertTrue(p.assess(value)['comparableFixtures'])

    def test_actor_fields_cannot_be_normalized_away(self):
        value=matched(); value['old-context']['scope']['contexts'][0]['characterTemplates'][0]['build']['essenceIds']=['altered']
        self.assertIn('physicalContext',p.assess(value)['fixtureMismatches'])

    def test_settings_cannot_be_normalized_away(self):
        value=matched(); value['old-settings']['changed']=True
        self.assertIn('settings',p.assess(value)['fixtureMismatches'])

    def test_incomplete_receipts_rejected(self):
        for key in ('old-costs','new-costs','pilot-costs'):
            value=inputs(); value[key]['status']='Failed'
            with self.subTest(key=key),self.assertRaisesRegex(ValueError,'Incomplete'): p.assess(value)

    def test_resampled_receipts_rejected(self):
        value=inputs(); value['new-costs']['retries']=1
        with self.assertRaises(ValueError): p.assess(value)

    def test_invalid_numeric_costs_rejected(self):
        for value in (True,0,-1,float('nan'),float('inf'),'1'):
            data=inputs(); data['new-costs']['nativeSeconds']=value
            with self.subTest(value=value),self.assertRaises(ValueError): p.assess(data)

    def test_phase_totals_must_reconcile(self):
        value=inputs(); value['new-costs']['observedBytes']+=1
        with self.assertRaisesRegex(ValueError,'totals'): p.assess(value)

    def test_final_publication_cost_cannot_disappear(self):
        value=inputs(); value['new-closeout']['auditSeconds']=0
        with self.assertRaisesRegex(ValueError,'publication accounting'): p.assess(value)

    def test_changed_margin_or_envelope_rejected(self):
        for key,val in [('margin',1),('publicationReserveSeconds',0),('nativeMaximumBytes',99999999999),('qualificationMaximumSeconds',1800)]:
            value=inputs(); value['design']['resources'][key]=val
            with self.subTest(key=key),self.assertRaisesRegex(ValueError,'envelope'): p.assess(value)

    def test_inherited_floors_cannot_be_reduced(self):
        value=inputs(); value['design']['resources']['inheritedForecastFloors']['nativeBytes']-=1
        with self.assertRaisesRegex(ValueError,'inherited'): p.assess(value)

    def test_faster_receipts_do_not_reduce_floors(self):
        value=matched(); result=p.assess(value)
        for key,floor in result['inheritedFloors'].items():
            self.assertGreaterEqual(result['sensitivity']['roundedPhaseFloors'][key],floor)
        self.assertTrue(all(r >= 1 for r in result['sensitivity']['upwardOnlyRatios'].values()))

    def test_completed_pilot_fight_count_is_checked(self):
        value=inputs(); value['pilot-result']['fights']=21888
        with self.assertRaisesRegex(ValueError,'pilot accounting'): p.assess(value)

    def test_small_placement_fixture_cannot_be_used_to_lower_cost(self):
        value=inputs(); value['new-context']['scope']['requiredPartySize']=5
        with self.assertRaisesRegex(ValueError,'ten actors'): p.assess(value)

    def test_storage_growth_is_applied_before_audit_scaling(self):
        baseline=p.assess(matched()); larger=p.assess(inputs())
        self.assertGreater(larger['sensitivity']['roundedPhaseFloors']['auditSeconds'],baseline['sensitivity']['roundedPhaseFloors']['auditSeconds'])

    def test_duplicate_json_keys_and_nan_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'bad.json'
            for raw in ('{"x":1,"x":2}','{"x":NaN}'):
                path.write_text(raw)
                with self.assertRaises(ValueError): p.read(path)

    def test_retained_only_verification_rejects_tampering(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); value=inputs(); index={}
            for key,row in value.items():
                index[key]=key+'.json'; p.write(root/index[key],row)
            p.write(root/'inputs.json',index); p.write(root/'assessment.json',p.assess(value))
            p.write(root/'files.json',{f.name:p.sha(f) for f in root.iterdir()})
            self.assertEqual('ResourcePrequalificationNotReady',p.verify(root)['status'])
            result=json.loads((root/'assessment.json').read_text()); result['admitted']=True
            (root/'assessment.json').write_text(json.dumps(result))
            with self.assertRaisesRegex(ValueError,'retained evidence'): p.verify(root)
            manifest=p.read(root/'files.json'); manifest['assessment.json']=p.sha(root/'assessment.json')
            (root/'files.json').write_text(json.dumps(manifest))
            with self.assertRaisesRegex(ValueError,'resource assessment'): p.verify(root)


if __name__=='__main__': unittest.main()
