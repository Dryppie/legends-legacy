"""Native lease contract, supervisor routing and production read-only audit checks."""
import copy
import importlib.util
import os
from pathlib import Path
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('native_lease_fixtures',ROOT/'build/test-proposal-pending-families.py')
t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
f,w,s,p=t.f,t.w,t.s,t.p
def budget(cap=1):return dict(version=w.NATIVE_LEASE_VERSION,maxConcurrentLeases=cap)
def budgets():return dict(native=budget(),nativeAudit=budget(0),publication=budget(0))


class Leases(unittest.TestCase):
    setUp=t.Families.setUp
    def prepare(self,base=None,change=None,lease_change=None,**options):
        self.lease_values=budgets()
        if lease_change:lease_change(self.lease_values)
        return t.Families.prepare(self,base,change,**(dict(native_lease_budgets=self.lease_values)|options))
    def test_complete_routed_sequence_binds_and_retains_v8(self):
        export=os.environ.get('LL_SUPERVISOR_LEASE_EXPORT');base=Path(export).resolve() if export else self.f.base
        supervisor=self.prepare(base);self.f.launch(supervisor);self.f.verify(supervisor)
        observed=supervisor.terminal_observation['storageOwnership']['nativePending']
        self.assertTrue(observed['allNativePhasesComplete']);self.assertFalse(observed['usableForAdmission'])
        self.assertEqual(observed['nativeLeaseBudgets'],budgets());self.assertEqual(observed['version'],w.NATIVE_LEASE_PLAN_VERSION)
        for phase in w.PHASES:
            b=w.strict((supervisor.root/'owner'/(phase+'-binding.json')).read_bytes())
            self.assertEqual(b['version'],w.WORKER_SIDECAR_BINDING_VERSION if phase=='independentAudit' else w.WORKER_NATIVE_LEASE_BINDING_VERSION)
            if phase=='independentAudit':self.assertNotIn('nativeLeaseBudget',b)
            else:
                self.assertEqual(b['nativeLeaseBudget'],self.lease_values[phase])
                self.assertEqual(w.native_lease_observation(observed['observations'][phase]['leases']),self.lease_values[phase])
        if export:
            w.seal_new(base/'terminal.json',supervisor.terminal_observation)
            w.seal_new(base/'fixture.json',dict(syntheticProcessObservations=4,realProcessObservations=0,actualCombat=0,productionEntropyDraws=0,
                scientificReservations=0,nativeEncounterPreparations=0,wholeProcessCoverage=False,usableForAdmission=False));p.seal(base)
    test_requires_managed_retention=t.Families.test_requires_managed_retention
    test_requires_bounded_sidecars=t.Families.test_requires_bounded_sidecars
    test_changed_retained_binding_rejects_before_dispatch=t.Families.test_changed_retained_binding_rejects_before_dispatch
    test_failed_native_stops_following_phases_and_keeps_failed_snapshot=t.Families.test_failed_native_stops_following_phases_and_keeps_failed_snapshot
    test_missing_native_receipt_cannot_complete=t.Families.test_missing_native_receipt_cannot_complete
    test_phase_retry_is_rejected=t.Families.test_phase_retry_is_rejected
    def test_requires_bounded_pending_families(self):
        with self.assertRaisesRegex(ValueError,'pending family'):self.prepare(native_pending_families=None)
    def test_all_three_lease_phases_are_required(self):
        with self.assertRaisesRegex(ValueError,'three native lease'):self.prepare(lease_change=lambda b:b.pop('publication'))
    def test_independent_phase_cannot_receive_leases(self):
        with self.assertRaisesRegex(ValueError,'three native lease'):self.prepare(lease_change=lambda b:b.update(independentAudit=budget(0)))
    def test_strict_integer_version_and_phase_limits(self):
        for phase,value in [('native',budget(True)),('native',budget(-1)),('native',budget(27)),('publication',budget()),
                ('nativeAudit',budget()),('native',budget()|dict(extra=0)),('native',budget()|dict(version='unknown'))]:
            with self.subTest(phase=phase,value=value),self.assertRaises(ValueError):w.native_lease_limits(phase,value)
    def test_declaration_is_copied(self):
        supervisor=self.prepare();self.lease_values['native']['maxConcurrentLeases']=2;self.lease_values.clear();self.f.launch(supervisor)
        self.assertEqual(supervisor.terminal_observation['storageOwnership']['nativePending']['nativeLeaseBudgets'],budgets())
    def test_changed_in_memory_lease_plan_rejects_before_dispatch(self):
        supervisor=self.prepare();original=supervisor.set_check
        def change(check):
            original(check);v=w.strict(supervisor._native_lease_budgets_raw);v['native']['maxConcurrentLeases']=2
            supervisor._native_lease_budgets_raw=w.canonical(v)
        with patch.object(supervisor,'set_check',side_effect=change),self.assertRaisesRegex(ValueError,'Changed native pending supervisor plan'):self.f.launch(supervisor)
        self.assertEqual(self.f.calls,[])
    def test_failed_lease_validation_cannot_start(self):
        supervisor=self.prepare()
        with patch.object(w,'proposal_native_lease_budget',side_effect=ValueError('literal validation')):
            with self.assertRaisesRegex(ValueError,'literal validation'):supervisor.validate(self.f.package/'request.json',self.f.output,self.f.package/'runtime/BalanceHarness.dll')
        with self.assertRaisesRegex(ValueError,'Unvalidated'):supervisor.start(0,None,None)
        self.assertFalse(supervisor.root.exists())
    def test_protected_probe_and_owned_paths_reject(self):
        root=self.f.base/'output'
        for path in (root.parent/'complete-family-allocation.writer.lock',root/'search/root-01/control/racing.writer.lock'):
            with self.subTest(path=path),self.assertRaisesRegex(ValueError,'protected'):
                w.proposal_native_lease_budget(root,'native',budget(),protected_paths=[path])


class Native(unittest.TestCase):
    def folder(self,name='native'):return Path(os.environ['LL_NATIVE_LEASE_EXPORT'])/name
    def test_native_v8_receipts_pass_independent_validation(self):
        for name in ('native','nativeAudit','publication','rejected-nativeAudit','rejected-publication','missing-owner','escaped-handle'):
            folder=self.folder(name);p.manifest(folder);b=w.strict((folder/'binding.json').read_bytes())
            r=w.verify_counter_receipt(folder/'receipt.json',p.sha(folder/'receipt.json'),b['phase'],b['requestSha256'],b['producerSha256'])
            self.assertEqual(r['version'],w.NATIVE_LEASE_RECEIPT_VERSION);self.assertEqual(r['bindingSha256'],p.sha(folder/'binding.json'))
            self.assertEqual(w.native_lease_observation(r['nativeLeases']),b['nativeLeaseBudget'])
    def test_mutated_lease_counters_members_and_claims_reject(self):
        original=w.strict((self.folder()/'receipt.json').read_bytes())['nativeLeases']
        edits=[lambda v:v['entries'].pop(),lambda v:v['entries'][2].update(path='other'),
            lambda v:v['entries'][2].update(attempts=2),lambda v:v['entries'][2].update(heldConflicts=1),
            lambda v:v['entries'][2].update(released=0),lambda v:v['entries'][2].update(forcedCleanup=1),
            lambda v:v.update(peakOwnedHandles=2),lambda v:v.update(currentOwnedHandles=1),
            lambda v:v.update(acceptedWriteBytes=1),lambda v:v.update(maxLeaseBytes=False),
            lambda v:v.update(wallClockBounded=True),lambda v:v.update(enclosingOwnerLifetimeBounded=True),
            lambda v:v.update(wholeProcessCoverage=True),lambda v:v.update(phase='nativeAudit'),lambda v:v.update(extra=0)]
        for i,edit in enumerate(edits):
            with self.subTest(fault=i),self.assertRaises(ValueError):
                value=copy.deepcopy(original);edit(value);w.native_lease_observation(value)
    def test_failed_cleanup_cannot_be_relabelled_complete(self):
        value=w.strict((self.folder('escaped-handle')/'receipt.json').read_bytes())['nativeLeases']
        value.update(outcome='Complete',failedOrUnknown=False)
        with self.assertRaisesRegex(ValueError,'Incomplete'):w.native_lease_observation(value)
    def test_lease_observation_cannot_be_smuggled_into_v4_receipt(self):
        value=w.strict((self.folder()/'receipt.json').read_bytes());value['version']=w.NATIVE_FAMILY_PENDING_RECEIPT_VERSION
        raw=w.canonical(value)
        with self.assertRaises(ValueError):w.counter_receipt(raw,w.hashlib.sha256(raw).hexdigest(),value['phase'],value['requestSha256'],value['producerSha256'])
    def test_wrong_inner_root_and_phase_reject(self):
        original=w.strict((self.folder('nativeAudit')/'receipt.json').read_bytes())
        for key,value in (('studyRoot',str(self.folder())),('phase','publication')):
            v=copy.deepcopy(original);v['nativeLeases'][key]=value;raw=w.canonical(v)
            with self.subTest(key=key),self.assertRaises(ValueError):w.counter_receipt(raw,w.hashlib.sha256(raw).hexdigest(),v['phase'],v['requestSha256'],v['producerSha256'])
    def test_real_v8_audit_prohibits_native_leases(self):
        original=w.RetainedOwner.run_worker
        def v8(owner,*args,**kwargs):
            kwargs.pop('pending_storage_budget',None);kwargs.pop('pending_binding_version',None)
            return original(owner,*args,**kwargs,pending_families_budget=t.limits(0),native_lease_budget=budget(0))
        with patch.object(w.RetainedOwner,'run_worker',v8):p.Native().test_real_native_audit_retains_v5_receipt_through_owner()
        root=Path(os.environ['LL_PENDING_BINDING_AUDIT_EXPORT']);r=w.strict((root/'receipt.json').read_bytes())
        self.assertEqual(r['version'],w.NATIVE_LEASE_RECEIPT_VERSION);self.assertEqual(r['nativeLeases']['entries'],[])
        self.assertEqual(r['nativeLeases']['outcome'],'Complete')


if __name__=='__main__':unittest.main()
