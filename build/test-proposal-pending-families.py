"""Bounded native pending names: literal routing, native exports and a real read-only audit."""
import copy
import os
from pathlib import Path
import shutil
import unittest
from unittest.mock import patch

import importlib.util
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('pending_family_fixture',ROOT/'build/test-proposal-supervisor-pending.py')
t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
f,w,s,p=t.f,t.w,t.s,t.p
def limits(n=1024):return dict(version=w.NATIVE_PENDING_FAMILIES_VERSION,maxFileBytes=n,maxLiveBytes=n,maxTotalWrittenBytes=n*100)
def budgets():return dict(native=limits(),nativeAudit=limits(0),publication=limits(0))


class Families(unittest.TestCase):
    def setUp(self):
        self.f=f.Supervisor();self.f.setUp();self.addCleanup(self.f.doCleanups)
    def prepare(self,base=None,change=None,**options):
        original=self.f.prepare(base);self.value=budgets()
        shutil.copyfile(t.t.d.__file__,self.f.package/'proposal_diagnostic_storage.py');self.f.reseal()
        if change:change(self.value)
        return s.StudyWorkSupervisor(original.root,**(dict(worker_observation_persistence=True,storage_contracts=t.t.contracts(True),
            worker_sidecar_limits={phase:p.limits() for phase in w.PHASES},native_pending_families=self.value)|options))
    def test_complete_routed_sequence_seals_v7_family_plan(self):
        export=os.environ.get('LL_SUPERVISOR_FAMILIES_EXPORT');base=Path(export).resolve() if export else self.f.base
        supervisor=self.prepare(base);self.f.launch(supervisor);self.f.verify(supervisor)
        observed=supervisor.terminal_observation['storageOwnership']['nativePending']
        self.assertTrue(observed['allNativePhasesComplete']);self.assertTrue(observed['pathInventoryComplete'])
        self.assertFalse(observed['wholeProcessCoverage']);self.assertFalse(observed['usableForAdmission'])
        self.assertEqual(observed['version'],w.NATIVE_PENDING_FAMILIES_PLAN_VERSION)
        for phase in w.PHASES:
            b=w.strict((supervisor.root/'owner'/(phase+'-binding.json')).read_bytes())
            self.assertEqual(b['version'],w.WORKER_SIDECAR_BINDING_VERSION if phase=='independentAudit' else w.WORKER_FAMILY_PENDING_BINDING_VERSION)
            if phase=='independentAudit':self.assertNotIn('pendingFamiliesBudget',b)
            else:self.assertEqual(b['pendingFamiliesBudget'],self.value[phase])
        if export:
            w.seal_new(base/'terminal.json',supervisor.terminal_observation)
            w.seal_new(base/'fixture.json',dict(syntheticProcessObservations=4,realProcessObservations=0,actualCombat=0,productionEntropyDraws=0,
                scientificReservations=0,nativeEncounterPreparations=0,wholeProcessCoverage=False,usableForAdmission=False));p.seal(base)
    test_missing_phase_rejects_before_directory_creation=t.Phases.test_missing_phase_rejects_before_directory_creation
    test_independent_phase_cannot_receive_native_plan=t.Phases.test_independent_phase_cannot_receive_native_plan
    test_requires_managed_retention=t.Phases.test_requires_managed_retention
    test_requires_bounded_sidecars=t.Phases.test_requires_bounded_sidecars
    test_changed_in_memory_plan_rejects_before_dispatch=t.Phases.test_changed_in_memory_plan_rejects_before_dispatch
    test_changed_retained_binding_rejects_before_dispatch=t.Phases.test_changed_retained_binding_rejects_before_dispatch
    test_failed_native_stops_following_phases_and_keeps_failed_snapshot=t.Phases.test_failed_native_stops_following_phases_and_keeps_failed_snapshot
    test_missing_native_receipt_cannot_complete=t.Phases.test_missing_native_receipt_cannot_complete
    test_phase_retry_is_rejected=t.Phases.test_phase_retry_is_rejected
    def test_copies_caps_before_caller_mutation(self):
        supervisor=self.prepare();self.value['native']['maxFileBytes']=99;self.value.clear();self.f.launch(supervisor)
        self.assertEqual(supervisor.terminal_observation['storageOwnership']['nativePending']['budgets'],budgets())
    def test_cannot_mix_explicit_and_family_plans(self):
        with self.assertRaisesRegex(ValueError,'cannot be mixed'):self.prepare(native_pending_budgets={})
    def test_strict_caps_and_versions(self):
        for key,value in (('maxFileBytes',True),('maxLiveBytes',-1),('maxTotalWrittenBytes',2**63),('version','unknown'),('extra',1)):
            with self.subTest(key=key),self.assertRaises(ValueError):w.pending_family_limits('native',limits()|{key:value})
    def test_zero_writer_phases_require_all_zero_caps(self):
        for phase in ('nativeAudit','publication'):
            for key in ('maxFileBytes','maxLiveBytes','maxTotalWrittenBytes'):
                with self.subTest(phase=phase,key=key),self.assertRaises(ValueError):w.pending_family_limits(phase,limits(0)|{key:1})
    def test_dynamic_directory_rejects_protected_overlap(self):
        with self.assertRaisesRegex(ValueError,'protected'):
            w.proposal_pending_families_budget(self.f.base,'native',limits(),protected_paths=[self.f.base/'study/heldout-01-x.json'])


class Native(unittest.TestCase):
    def folder(self,name='native'):return Path(os.environ['LL_PENDING_FAMILIES_EXPORT'])/name
    def test_native_v7_exports_validate_independently(self):
        for name in ('native','nativeAudit','publication','rejected-nativeAudit','rejected-publication','exhausted-native'):
            folder=self.folder(name);p.manifest(folder);b=w.strict((folder/'binding.json').read_bytes())
            r=w.verify_counter_receipt(folder/'receipt.json',p.sha(folder/'receipt.json'),b['phase'],b['requestSha256'],b['producerSha256'])
            self.assertEqual(r['version'],w.NATIVE_FAMILY_PENDING_RECEIPT_VERSION)
            self.assertEqual(r['bindingSha256'],p.sha(folder/'binding.json'))
            self.assertEqual(w.pending_families_observation(r['pendingStorage']),b['pendingFamiliesBudget'])
    def test_receipt_rejects_mutated_catalogue_members_caps_and_claims(self):
        original=w.strict((self.folder()/'receipt.json').read_bytes())['pendingStorage']
        edits=[lambda v:v['contracts'].pop(0),lambda v:v['contracts'].append(copy.deepcopy(v['contracts'][-1])),
            lambda v:v['contracts'][-1].update(maxCreates=2),lambda v:v['contracts'][-1].update(maxBytes=2048),
            lambda v:v.update(maxLiveBytes=v['maxLiveBytes']+1),lambda v:v.update(phase='publication'),
            lambda v:v.update(wholeProcessCoverage=True),lambda v:v.update(unknown=1),
            lambda v:v['creates'][-1].update(count=2),lambda v:v.update(studyRoot=str(self.folder()))]
        for i,edit in enumerate(edits):
            with self.subTest(fault=i),self.assertRaises(ValueError):
                value=copy.deepcopy(original);edit(value);w.pending_families_observation(value)
    def test_extra_member_in_one_root_rejected_even_below_global_limit(self):
        v=w.strict((self.folder()/'receipt.json').read_bytes())['pendingStorage']
        f=v['contracts'][39];target=Path(v['studyRoot'])/'study'/('heldout-01-'+'d'*64+'.json')
        f.update(path=str(target)+'.pending',destination=str(target));v['creates'][39]['path']=f['path']
        with self.assertRaisesRegex(ValueError,'family'):w.pending_families_observation(v)
    def test_duplicate_dynamic_path_rejected(self):
        v=w.strict((self.folder()/'receipt.json').read_bytes())['pendingStorage']
        v['contracts'][-1]=copy.deepcopy(v['contracts'][-2]);v['creates'][-1]=copy.deepcopy(v['creates'][-2])
        with self.assertRaises(ValueError):w.pending_families_observation(v)
    def test_storage_v2_cannot_be_smuggled_into_v3_receipt(self):
        v=w.strict((self.folder()/'receipt.json').read_bytes());v['version']=w.NATIVE_PHASE_PENDING_RECEIPT_VERSION
        raw=w.canonical(v)
        with self.assertRaises(ValueError):w.counter_receipt(raw,w.hashlib.sha256(raw).hexdigest(),v['phase'],v['requestSha256'],v['producerSha256'])
    def test_real_v7_native_audit_uses_zero_writer_contract(self):
        original=w.RetainedOwner.run_worker
        def v7(owner,*args,**kwargs):
            kwargs.pop('pending_storage_budget',None);kwargs.pop('pending_binding_version',None)
            return original(owner,*args,**kwargs,pending_families_budget=limits(0))
        with patch.object(w.RetainedOwner,'run_worker',v7):p.Native().test_real_native_audit_retains_v5_receipt_through_owner()
        root=Path(os.environ['LL_PENDING_BINDING_AUDIT_EXPORT']);r=w.strict((root/'receipt.json').read_bytes())
        self.assertEqual(r['version'],w.NATIVE_FAMILY_PENDING_RECEIPT_VERSION);self.assertEqual(r['pendingStorage']['contracts'],[])


if __name__=='__main__':unittest.main()
