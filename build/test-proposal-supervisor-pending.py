"""Per-phase pending supervisor integration; literal routing and production read/audit only."""
import copy
import importlib.util
import os
from pathlib import Path
import shutil
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
t=load('phase_pending_storage_fixture','build/test-proposal-supervisor-storage.py')
p=load('phase_pending_native_fixture','build/test-proposal-pending-binding.py')
f,w,s=t.f,t.w,t.s
def budgets(output):
    target=output/'study/literal.json'
    return dict(native=dict(files=[dict(path=str(target)+'.pending',destination=str(target),maxBytes=1024,maxCreates=1)],maxLiveBytes=1024,maxTotalWrittenBytes=1024),
                nativeAudit=dict(files=[],maxLiveBytes=0,maxTotalWrittenBytes=0),publication=dict(files=[],maxLiveBytes=0,maxTotalWrittenBytes=0))


class Phases(unittest.TestCase):
    def setUp(self):
        self.f=f.Supervisor();self.f.setUp();self.addCleanup(self.f.doCleanups)
    def prepare(self,base=None,change=None,**options):
        original=self.f.prepare(base);self.value=budgets(self.f.output)
        shutil.copyfile(t.d.__file__,self.f.package/'proposal_diagnostic_storage.py');self.f.reseal()
        if change:change(self.value)
        arguments=dict(worker_observation_persistence=True,storage_contracts=t.contracts(True),
            worker_sidecar_limits={phase:p.limits() for phase in w.PHASES},native_pending_budgets=self.value)|options
        return s.StudyWorkSupervisor(original.root,**arguments)
    def test_complete_routed_sequence_binds_native_v6_and_independent_v4(self):
        export=os.environ.get('LL_SUPERVISOR_PENDING_EXPORT');base=Path(export).resolve() if export else self.f.base
        supervisor=self.prepare(base);self.f.launch(supervisor);self.f.verify(supervisor)
        observed=supervisor.terminal_observation['storageOwnership']['nativePending']
        self.assertTrue(observed['allNativePhasesComplete']);self.assertFalse(observed['pathInventoryComplete'])
        self.assertFalse(observed['wholeProcessCoverage']);self.assertFalse(observed['usableForAdmission'])
        self.assertEqual(set(observed['budgets']),set(w.NATIVE_PHASES))
        for phase in w.PHASES:
            b=w.strict((supervisor.root/'owner'/(phase+'-binding.json')).read_bytes())
            self.assertEqual(b['version'],w.WORKER_SIDECAR_BINDING_VERSION if phase=='independentAudit' else w.WORKER_PHASE_PENDING_BINDING_VERSION)
            if phase=='independentAudit':self.assertNotIn('pendingStorageBudget',b)
            else:self.assertEqual(b['pendingStorageBudget'],self.value[phase])
        if export:
            w.seal_new(base/'terminal.json',supervisor.terminal_observation)
            w.seal_new(base/'fixture.json',dict(syntheticProcessObservations=4,realProcessObservations=0,actualCombat=0,productionEntropyDraws=0,
                scientificReservations=0,nativeEncounterPreparations=0,wholeProcessCoverage=False,usableForAdmission=False,
                sources={Path(n).resolve().relative_to(ROOT).as_posix():p.sha(n) for n in (__file__,s.__file__,w.__file__,f.__file__)}));p.seal(base)
    def test_missing_phase_rejects_before_directory_creation(self):
        with self.assertRaisesRegex(ValueError,'three native'):self.prepare(change=lambda v:v.pop('publication'))
        self.assertFalse((self.f.base/'accounting').exists())
    def test_independent_phase_cannot_receive_native_plan(self):
        with self.assertRaisesRegex(ValueError,'three native'):self.prepare(change=lambda v:v.update(independentAudit=v['nativeAudit']))
    def test_requires_managed_retention(self):
        with self.assertRaisesRegex(ValueError,'declared storage'):self.prepare(storage_contracts=None)
    def test_requires_bounded_sidecars(self):
        with self.assertRaisesRegex(ValueError,'bounded persistent'):self.prepare(worker_sidecar_limits=None)
    def test_native_empty_rejects(self):
        with self.assertRaises(ValueError):self.prepare(change=lambda v:v.update(native=copy.deepcopy(v['nativeAudit'])))
    def test_audit_nonempty_rejects(self):
        with self.assertRaisesRegex(ValueError,'prohibit'):self.prepare(change=lambda v:v.update(nativeAudit=copy.deepcopy(v['native'])))
    def test_empty_nonzero_cap_rejects(self):
        with self.assertRaises(ValueError):self.prepare(change=lambda v:v['publication'].update(maxLiveBytes=1))
    def test_empty_bool_cap_rejects(self):
        with self.assertRaises(ValueError):self.prepare(change=lambda v:v['publication'].update(maxLiveBytes=False))
    def test_native_outside_output_rejects_before_start(self):
        def change(v):v['native']['files'][0].update(path=str(self.f.base/'other.pending'),destination=str(self.f.base/'other'))
        supervisor=self.prepare(change=change)
        with self.assertRaisesRegex(ValueError,'study output'):self.f.launch(supervisor)
        self.assertFalse(supervisor.root.exists());self.assertEqual(self.f.calls,[])
    def test_native_protected_input_rejects_before_start(self):
        def change(v):v['native']['files'][0].update(path=str(self.f.output/'source/context.json.pending'),destination=str(self.f.output/'source/context.json'))
        supervisor=self.prepare(change=change)
        with self.assertRaisesRegex(ValueError,'protected'):self.f.launch(supervisor)
        self.assertFalse(supervisor.root.exists())
    def test_failed_plan_validation_cannot_be_followed_by_start(self):
        def change(v):v['native']['files'][0].update(path=str(self.f.base/'other.pending'),destination=str(self.f.base/'other'))
        supervisor=self.prepare(change=change)
        with self.assertRaises(ValueError):supervisor.validate(self.f.package/'request.json',self.f.output,self.f.package/'runtime/BalanceHarness.dll')
        with self.assertRaisesRegex(ValueError,'Unvalidated'):supervisor.start(0,None,None)
        self.assertFalse(supervisor.root.exists())
    def test_budget_is_copied_before_caller_mutation(self):
        supervisor=self.prepare();self.value['native']['files'][0]['maxBytes']=99;self.value.clear()
        self.f.launch(supervisor)
        actual=supervisor.terminal_observation['storageOwnership']['nativePending']['budgets']
        self.assertEqual(actual,budgets(self.f.output))
    def test_changed_in_memory_plan_rejects_before_dispatch(self):
        supervisor=self.prepare();original=supervisor.set_check
        def change(check):
            original(check);v=w.strict(supervisor._native_pending_plan_raw);v['native']['maxLiveBytes']=999
            supervisor._native_pending_plan_raw=w.canonical(v)
        with patch.object(supervisor,'set_check',side_effect=change),self.assertRaisesRegex(ValueError,'Changed native pending supervisor plan'):self.f.launch(supervisor)
        self.assertEqual(self.f.calls,[])
    def test_changed_retained_binding_rejects_before_dispatch(self):
        supervisor=self.prepare();original=supervisor.set_check
        def change(check):
            original(check)
            with (supervisor.root/'supervisor/storage-binding.json').open('ab') as stream:stream.write(b' ')
        with patch.object(supervisor,'set_check',side_effect=change),self.assertRaises(ValueError):self.f.launch(supervisor)
        self.assertEqual(self.f.calls,[])
    def test_failed_native_stops_following_phases_and_keeps_failed_snapshot(self):
        supervisor=self.prepare();self.f.failed_phase='native'
        with self.assertRaisesRegex(ValueError,'Native comparison failed'):self.f.launch(supervisor)
        self.assertEqual([phase for phase,_,_ in self.f.calls],['native'])
        value=supervisor.terminal_observation['storageOwnership']['nativePending']
        self.assertFalse(value['allNativePhasesComplete']);self.assertEqual(value['observations']['native']['status'],'Failed')
        self.assertEqual(value['observations']['nativeAudit']['status'],'NotInvoked')
    def test_missing_native_receipt_cannot_complete(self):
        supervisor=self.prepare();self.f.missing_receipt_phase='native'
        with self.assertRaises(FileNotFoundError):self.f.launch(supervisor)
        self.assertFalse(supervisor.terminal_observation['storageOwnership']['nativePending']['allNativePhasesComplete'])
    def test_phase_retry_is_rejected(self):
        supervisor=self.prepare();original=supervisor.run
        def repeat(phase,*args,**kwargs):
            result=original(phase,*args,**kwargs)
            if phase=='native':original(phase,*args,**kwargs)
            return result
        with patch.object(supervisor,'run',side_effect=repeat),self.assertRaisesRegex(ValueError,'cannot be reused'):self.f.launch(supervisor)
        self.assertEqual(len(self.f.calls),1)
    def test_v5_still_rejects_empty_scope(self):
        with self.assertRaises(ValueError):w.native_pending_budget(dict(files=[],maxLiveBytes=0,maxTotalWrittenBytes=0))


class Native(unittest.TestCase):
    def test_native_v6_exports_validate(self):
        root=Path(os.environ['LL_PHASE_PENDING_EXPORT'])
        for name in ('native','nativeAudit','publication','rejected-nativeAudit','rejected-publication','undeclared-native'):
            folder=root/name;p.manifest(folder);b=w.strict((folder/'binding.json').read_bytes())
            r=w.verify_counter_receipt(folder/'receipt.json',p.sha(folder/'receipt.json'),b['phase'],b['requestSha256'],b['producerSha256'])
            self.assertEqual(r['version'],w.NATIVE_PHASE_PENDING_RECEIPT_VERSION)
            self.assertEqual(r['bindingSha256'],p.sha(folder/'binding.json'))
            self.assertEqual(w.native_pending_observation(r['pendingStorage'],allow_empty=True),w.proposal_pending_budget(b['studyRoot'],b['phase'],b['pendingStorageBudget']))
    def test_real_v6_native_audit_enforces_no_pending_writers(self):
        # Reuse the real v5 audit test's process ownership and failure closeout,
        # replacing only the explicit protocol selection and declaration.
        original=w.RetainedOwner.run_worker
        def v6(owner,*args,**kwargs):
            kwargs.update(pending_storage_budget=dict(files=[],maxLiveBytes=0,maxTotalWrittenBytes=0),pending_binding_version=w.WORKER_PHASE_PENDING_BINDING_VERSION)
            return original(owner,*args,**kwargs)
        with patch.object(w.RetainedOwner,'run_worker',v6):p.Native().test_real_native_audit_retains_v5_receipt_through_owner()
        root=Path(os.environ['LL_PENDING_BINDING_AUDIT_EXPORT']);r=w.strict((root/'receipt.json').read_bytes())
        self.assertEqual(r['version'],w.NATIVE_PHASE_PENDING_RECEIPT_VERSION);self.assertEqual(r['pendingStorage']['contracts'],[])


if __name__=='__main__':unittest.main()
