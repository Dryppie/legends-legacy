"""Rejection tests for the no-combat admission boundary; no registry writes."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('admission', Path(__file__).with_name('prepare-proposal-affinity-admission.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class AdmissionTests(unittest.TestCase):
    def setUp(self):
        self.context = dict(rootSeed=10, scope=dict(id='old',executionHash='old',generation=dict(seeds=[11]),
            stages=dict(schedules={'old':dict(discovery=[1],selection=[2],confirmation=[],diagnostics=[],feedback=[3])}),
            excludedCombatSeeds=[4],references=[dict(scenario=dict(seeds=[12]))],settingsHash='settings'),
            damageAffinityInventory=dict(typedNodes=['immutable']))
        self.values = [1,2,3,4,10,11,12,100]

    def test_excludes_history_but_retains_declared_legacy_schedules(self):
        before = copy.deepcopy(self.context)
        c = a.derive_context(self.context,self.values,'qualified')
        self.assertEqual(c['scope']['excludedCombatSeeds'],[4,10,11,12,100])
        self.assertEqual(c['scope']['stages'],self.context['scope']['stages'])
        self.assertEqual(self.context,before)
        self.assertEqual(c['damageAffinityInventory'],before['damageAffinityInventory'])

    def test_missing_historical_declarations_fail(self):
        for value in (1,3,10,11,12):
            with self.subTest(value=value),self.assertRaisesRegex(ValueError,'omits'):
                a.derive_context(self.context,[v for v in self.values if v != value],'qualified')

    def test_unordered_or_duplicate_history_fails(self):
        for values in (list(reversed(self.values)), self.values+[100], []):
            with self.subTest(values=values),self.assertRaises(ValueError): a.derive_context(self.context,values,'qualified')

    def test_seedless_export_uses_only_declared_historical_values(self):
        export = dict(arms=[dict(teams=[dict(scenario=dict(id=str(i),seeds=[],party=['fixed'])) for i in range(12)]) for _ in range(3)])
        original = copy.deepcopy(export)
        rows = a.probe_scenarios(export,self.context,self.values)
        self.assertEqual(len(rows),36); self.assertEqual(export,original)
        self.assertTrue(all(r['seeds'] == [1,3] and r['scenario']['seeds'] == [1,3] for r in rows))
        with self.assertRaises(ValueError): a.probe_scenarios(export,self.context,[1])
        with self.assertRaises(ValueError): a.probe_scenarios(dict(arms=export['arms'][:1]),self.context,self.values)

    def test_only_runtime_plan_scope_may_change(self):
        plan = dict(scopeHash='original',candidate={'ids':[1,2,3]},roots=12,maximumFights=21888,analysis={'go':.02})
        derived = dict(plan,scopeHash='qualified'); a.validate_plan(plan,derived)
        for key,value in [('roots',11),('maximumFights',30000),('analysis',{'go':.01}),('candidate',{'ids':[1,2]})]:
            with self.subTest(key=key),self.assertRaises(ValueError): a.validate_plan(plan,dict(derived,**{key:value}))
        with self.assertRaises(ValueError): a.validate_plan(plan,plan)

    def test_only_tested_harness_can_replace_captured_runtime(self):
        captured = {'runtime/'+n:'old' for n in a.HARNESS_FILES}; captured['runtime/Services.LL.dll']='captured'
        tested = dict.fromkeys(a.HARNESS_FILES,'tested'); tested['BalanceHarness.dll']=a.HARNESS_PIN
        runtime = dict(tested,**{'Services.LL.dll':'captured'}); a.validate_runtime(runtime,captured,tested)
        for changed in (dict(runtime,**{'Services.LL.dll':'new'}),dict(runtime,extra='new'),dict(tested)):
            with self.assertRaises(ValueError): a.validate_runtime(changed,captured,tested)

    def test_qualification_requires_exact_inputs_participants_and_gameplay(self):
        execution = dict(runtime='runtime',architecture='X64',operatingSystem='os',assemblyHashes={'BalanceHarness':'old','Domain':'same'})
        old = dict(execution=execution,executionHash='old',settingsHash='settings',materializations=1,nativePreparations=1,fights=0,newValues=0)
        new = copy.deepcopy(old); new['execution']['assemblyHashes']['BalanceHarness']=a.HARNESS_PIN
        new.update(executionHash='new',replayedArms=3,compiledSourceDocuments=10,jitResolvedMethods=['entry'])
        captured = dict(execution=execution,executionHash='old')
        rows = [dict(ordinal=1,seed=1,scenarioHash='recipe',inputHash='input',participantsHash='prepared')]
        a.validate_qualification(old,new,rows,rows,captured,self.context)
        for key in ('inputHash','participantsHash','seed'):
            changed = copy.deepcopy(rows); changed[0][key]='changed'
            with self.subTest(key=key),self.assertRaises(ValueError): a.validate_qualification(old,new,rows,changed,captured,self.context)
        changed = copy.deepcopy(new); changed['execution']['assemblyHashes']['Domain']='drift'
        with self.assertRaises(ValueError): a.validate_qualification(old,changed,rows,rows,captured,self.context)

    def test_process_must_exit_with_empty_owned_job(self):
        good = dict(mechanism='suspended-owned-job-v1',exitCode=0,timedOut=False,activeProcesses=0,totalProcesses=2)
        a.process_ok(good)
        for key,value in [('exitCode',1),('timedOut',True),('activeProcesses',1),('totalProcesses',0),('mechanism','unowned')]:
            with self.subTest(key=key),self.assertRaises(ValueError): a.process_ok(dict(good,**{key:value}))

    def test_forecast_has_separate_caps_and_rejects_nonfinite_timings(self):
        native = dict(status='Verified',fights=27648,measuredSeconds=1360,observedBytes=669000000)
        completion = dict(status='Complete',auditSeconds=208,auditBytes=6500000)
        fixture = dict(status='Complete',nativeSeconds=148,auditSeconds=48,auditBytes=2700000)
        candidate = dict(materializationSeconds=[.1,.001,.002],preparationSeconds=[.2,.01,.02])
        args = (native,completion,fixture,candidate,17000000,15000000,3000000)
        self.assertTrue(a.forecast(*args)['fits']); self.assertFalse(a.forecast(*args)['guaranteedUpperBound'])
        for timings in ([.1,float('nan'),.1],[.1,-.1,.1],[]):
            bad = dict(candidate,materializationSeconds=timings)
            with self.subTest(timings=timings),self.assertRaises(ValueError): a.forecast(native,completion,fixture,bad,*args[4:])
        estimate = a.forecast(native,completion,fixture,candidate,500000000,15000000,3000000)
        self.assertFalse(estimate['fits'])
        with self.assertRaises(ValueError): a.admit_forecast(estimate)

    def test_rejected_audit_estimate_is_retained_without_relaxing_cap(self):
        native = dict(status='Verified',fights=27648,measuredSeconds=1360,observedBytes=669000000)
        completion = dict(status='Complete',auditSeconds=208,auditBytes=6500000)
        fixture = dict(status='Complete',nativeSeconds=148,auditSeconds=48,auditBytes=2700000)
        candidate = dict(materializationSeconds=[.08,.0214072,.0214072],preparationSeconds=[.1,.0176584,.0176584])
        estimate = a.forecast(native,completion,fixture,candidate,15132082,20934705,585784)
        self.assertGreater(estimate['auditSeconds'],1200)
        self.assertLess(estimate['nativeSeconds'],9600)
        self.assertLess(estimate['totalSeconds'],10800)
        self.assertFalse(estimate['fits'])
        with tempfile.TemporaryDirectory(prefix='proposal-admission-estimate-') as directory:
            p = Path(directory)/'resource-forecast.json'; a.save(p,estimate)
            with self.assertRaises(ValueError): a.admit_forecast(estimate)
            self.assertEqual(a.read(p),estimate)

    def test_existing_resource_attempt_closes_admission_before_new_charge(self):
        with tempfile.TemporaryDirectory(prefix='proposal-admission-closed-') as directory:
            p = Path(directory)/'attempt'; a.require_open_resource_gate(p); p.mkdir()
            with self.assertRaisesRegex(ValueError,'closed'): a.require_open_resource_gate(p)
            self.assertEqual(list(p.iterdir()),[])

    def test_manifest_rejects_resealed_and_extra_files(self):
        with tempfile.TemporaryDirectory(prefix='proposal-admission-test-') as directory:
            root = Path(directory); a.save(root/'source.json',dict(value=1)); a.save(root/'files.json',a.inventory(root))
            pin = a.sha(root/'files.json'); a.authenticate(root,pin)
            a.save(root/'extra.json',dict(value=2))
            with self.assertRaises(ValueError): a.authenticate(root,pin)
            with self.assertRaises(ValueError): a.authenticate(root,'0'*64)


if __name__ == '__main__': unittest.main(verbosity=2)
