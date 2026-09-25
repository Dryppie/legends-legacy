"""Declared supervisor storage, routed literal workers only; no scientific run."""
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('storage_supervisor_fixture',ROOT/'build/test-proposal-owner-supervisor.py')
f=importlib.util.module_from_spec(spec);spec.loader.exec_module(f)
w,s,launcher,owned=f.w,f.s,f.launcher,f.owned
import proposal_diagnostic_storage as d


def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def contracts(persistence=False):
    names=dict(owner=['binding.json','owner-work.json','files.json'],
               supervisor=['storage-binding.json','final-owner-observation.json','files.json'],
               terminal=['supervisor-observation.json','files.json'])
    for index,phase in enumerate(w.PHASES,1):
        names['owner'] += [phase+'-binding.json',phase+'-work.json',phase+'-receipt-publication.json',
                           f'{phase}-process-{index:02d}.json']
        if persistence:names['owner'].append(phase+'-publication-persistence.json')
    values={name:dict(version=d.VERSION,files={leaf:dict(kind='retained',maxBytes=200000) for leaf in leaves},
                     maxLiveBytes=4000000,maxScratchBytes=8192,maxTotalWrittenBytes=4008192) for name,leaves in names.items()}
    for value in values.values():value['files']['cleanup.tmp']=dict(kind='scratch',maxBytes=8192)
    return values


class SupervisorStorage(unittest.TestCase):
    setUp=f.Supervisor.setUp
    prepare=f.Supervisor.prepare
    reseal=f.Supervisor.reseal
    routed=f.Supervisor.routed
    launch=f.Supervisor.launch
    verify=f.Supervisor.verify
    verify_archive_resources=f.Supervisor.verify_archive_resources

    def ready(self, *, persistence=False, value=None, base=None):
        self.prepare(base)
        shutil.copyfile(d.__file__,self.package/'proposal_diagnostic_storage.py')
        self.reseal()
        return s.StudyWorkSupervisor((base or self.base)/'accounting',worker_observation_persistence=persistence,
                                    storage_contracts=contracts(persistence) if value is None else value)

    def failed(self,supervisor,pattern,kind=ValueError):
        with self.assertRaisesRegex(kind,pattern) as caught:self.launch(supervisor)
        self.assertIsNone(supervisor.manifest_sha256)
        self.assertIsNone(supervisor.observation_manifest_sha256)
        self.assertIsNone(launcher.FILE_WORK.get())
        if supervisor.watchdog is not None:self.assertTrue(supervisor.watchdog.finished.is_set())
        return caught.exception

    def verify_storage(self,supervisor):
        self.verify(supervisor);self.verify_archive_resources()
        value=supervisor.terminal_observation['storageOwnership']
        self.assertEqual(supervisor.terminal_observation['outcome'],'Complete')
        self.assertEqual(set(value['scopes']),{'owner','supervisor','terminal'})
        self.assertEqual(value['bindingSha256'],sha(supervisor.root/'supervisor/storage-binding.json'))
        binding=w.strict((supervisor.root/'supervisor/storage-binding.json').read_bytes())
        self.assertEqual(binding['contractsSha256'],hashlib.sha256(w.canonical(binding['contracts'])).hexdigest())
        for path in (s.__file__,w.__file__,d.__file__):self.assertEqual(binding['sourceBindings'][Path(path).name],sha(path))
        for name,scope in value['scopes'].items():
            self.assertEqual(scope['contractSha256'],hashlib.sha256(w.canonical(binding['contracts'][name])).hexdigest())
            self.assertEqual(scope['currentRetainedBytes'],sum(p.stat().st_size for p in (supervisor.root/name).iterdir()))
            self.assertFalse(scope['wholeProcessCoverage']);self.assertFalse(scope['usableForAdmission'])
        self.assertEqual(value['currentRetainedBytes'],sum(v['currentRetainedBytes'] for v in value['scopes'].values()))
        self.assertEqual(value['declaredManagedLiveByteUpperBound'],12000000)
        self.assertEqual(value['declaredManagedScratchByteUpperBound'],24576)
        self.assertEqual(value['declaredManagedTotalWriteByteUpperBound'],12024576)
        self.assertIsNone(value['simultaneousPeakBytes'])
        self.assertFalse(value['filesystemConfinement']);self.assertFalse(value['externalWritesBounded'])
        self.assertFalse(value['wholeProcessCoverage']);self.assertFalse(value['usableForAdmission'])
        self.assertTrue(supervisor.terminal_observation['terminalObservationPersistenceExcluded'])
        self.assertIn('workerReceiptOriginals',value['missingCoverage'])
        self.assertEqual(set(value['scopeCounters']),set(value['scopes']))
        return value

    def success_fixture(self,persistence):
        export=os.environ.get('LL_SUPERVISOR_STORAGE_EXPORT')
        base=Path(export).resolve()/('v3' if persistence else 'v2') if export else self.base
        if export:base.parent.mkdir(exist_ok=True)
        supervisor=self.ready(persistence=persistence,base=base)
        original=supervisor.set_check
        def checked(check):
            original(check)
            if not hasattr(supervisor,'literal_cleanup_added'):
                supervisor.literal_cleanup_added=True
                def cleanup():
                    for storage in (supervisor.owner.storage,supervisor.publication,supervisor.observation_publication):
                        with storage.create('cleanup.tmp','scratch') as stream:stream.write(b'x'*8192)
                        storage.delete('cleanup.tmp')
                supervisor.owner.callback(cleanup)
        with patch.object(supervisor,'set_check',side_effect=checked):self.launch(supervisor)
        value=self.verify_storage(supervisor)
        for scope in value['scopes'].values():
            self.assertEqual(scope['peakScratchBytes'],8192);self.assertEqual(scope['deletedBytes'],8192)
            self.assertEqual(scope['currentScratchBytes'],0)
        self.assertEqual(len(self.calls),4)
        (base/'terminal.json').write_bytes(w.canonical(supervisor.terminal_observation))
        sources={Path(path).relative_to(ROOT).as_posix():sha(path) for path in (__file__,s.__file__,w.__file__,d.__file__)}
        (base/'fixture.json').write_bytes(w.canonical(dict(sources=sources,workerObservationPersistence=persistence,
            realProcessObservations=0,syntheticProcessObservations=4,actualCombat=0,productionEntropyDraws=0,
            wholeProcessCoverage=False,usableForAdmission=False)))
        values={p.relative_to(base).as_posix():sha(p) for p in base.rglob('*') if p.is_file()}
        (base/'files.json').write_bytes(w.canonical(values))

    def test_v2_worker_receipts_and_all_publication_scopes(self):self.success_fixture(False)

    def test_v3_worker_receipts_and_all_publication_scopes(self):self.success_fixture(True)

    def test_default_does_not_require_storage_module_or_change_schema(self):
        supervisor=self.prepare();self.launch(supervisor);self.verify(supervisor)
        self.assertNotIn('storageOwnership',supervisor.terminal_observation)
        self.assertFalse((supervisor.root/'supervisor/storage-binding.json').exists())
        self.assertIsInstance(supervisor.owner.storage,w.ManagedStorage)

    def test_incomplete_or_unknown_scope_rejects_before_io(self):
        for value in ({},{'owner':contracts()['owner']},contracts()|{'unknown':contracts()['owner']},[]):
            with self.subTest(value=value),self.assertRaisesRegex(ValueError,'all three'):
                s.StudyWorkSupervisor(self.base/'scope',storage_contracts=value)
        self.assertFalse((self.base/'scope').exists())

    def test_required_retained_member_is_validated_before_io(self):
        for scope,name in (('owner','binding.json'),('supervisor','storage-binding.json'),('terminal','files.json')):
            value=contracts();value[scope]['files'].pop(name)
            with self.subTest(scope=scope),self.assertRaisesRegex(ValueError,'Missing retained'):
                s.StudyWorkSupervisor(self.base/'scope',storage_contracts=value)
        self.assertFalse((self.base/'scope').exists())

    def test_required_members_cannot_be_scratch(self):
        value=contracts();value['terminal']['files']['files.json']['kind']='scratch'
        with self.assertRaisesRegex(ValueError,'Missing retained'):s.StudyWorkSupervisor(self.base/'scope',storage_contracts=value)

    def test_v3_requires_persistence_receipt_declarations(self):
        with self.assertRaisesRegex(ValueError,'Missing retained'):
            s.StudyWorkSupervisor(self.base/'scope',worker_observation_persistence=True,storage_contracts=contracts())

    def test_caller_mutation_cannot_replace_declared_limits(self):
        value=contracts();supervisor=self.ready(value=value);before=copy.deepcopy(value)
        value['terminal']['maxLiveBytes']=0;value['owner']['files'].clear()
        self.launch(supervisor)
        binding=w.strict((supervisor.root/'supervisor/storage-binding.json').read_bytes())
        self.assertEqual(binding['contracts'],before)

    def test_missing_captured_storage_module_prevents_worker_and_directory(self):
        self.prepare();supervisor=s.StudyWorkSupervisor(self.base/'accounting',storage_contracts=contracts())
        self.failed(supervisor,'Accounting module differs')
        self.assertEqual(self.calls,[]);self.assertFalse(supervisor.root.exists())

    def test_changed_captured_storage_module_prevents_worker_and_directory(self):
        supervisor=self.ready();(self.package/'proposal_diagnostic_storage.py').write_bytes(b'changed')
        self.reseal();self.failed(supervisor,'Accounting module differs')
        self.assertEqual(self.calls,[]);self.assertFalse(supervisor.root.exists())

    def test_admission_overlap_rejects_before_creation(self):
        supervisor=self.ready();supervisor.root=self.package/'nested'
        self.failed(supervisor,'fresh and disjoint');self.assertFalse(supervisor.root.exists())

    def test_registry_overlap_rejects_before_creation(self):
        supervisor=self.ready();supervisor.root=self.output.parent/'nested'
        self.failed(supervisor,'fresh and disjoint');self.assertFalse(supervisor.root.exists())

    def test_owner_initial_binding_limit_prevents_any_worker(self):
        value=contracts();value['owner']['files']['binding.json']['maxBytes']=0
        supervisor=self.ready(value=value);self.failed(supervisor,'limit')
        self.assertEqual(self.calls,[]);self.assertFalse(supervisor.started)

    def test_storage_binding_limit_prevents_leases_and_worker(self):
        value=contracts();value['supervisor']['files']['storage-binding.json']['maxBytes']=0
        supervisor=self.ready(value=value);self.failed(supervisor,'limit')
        self.assertEqual(self.calls,[]);self.assertFalse(self.output.exists())
        self.assertFalse(Path(str(self.output)+'.writer.lock').exists())

    def test_retained_sidecar_limit_stops_following_workers(self):
        value=contracts();value['owner']['files']['native-receipt-publication.json']['maxBytes']=0
        supervisor=self.ready(value=value);self.failed(supervisor,'limit')
        self.assertEqual([p for p,_,_ in self.calls],['native'])
        self.assertTrue((supervisor.root/'worker-receipts/native-receipt-publication.json').is_file())
        self.assertIsNone(supervisor.owner.manifest_sha256)

    def test_supervisor_publication_limit_preserves_prior_owner_evidence(self):
        value=contracts();value['supervisor']['files']['final-owner-observation.json']['maxBytes']=0
        supervisor=self.ready(value=value);self.failed(supervisor,'limit')
        self.assertTrue((supervisor.root/'owner/files.json').is_file())
        self.assertEqual(len(self.calls),4)

    def test_terminal_publication_limit_prevents_success(self):
        value=contracts();value['terminal']['files']['supervisor-observation.json']['maxBytes']=0
        supervisor=self.ready(value=value);self.failed(supervisor,'limit')
        terminal=supervisor.terminal_observation
        self.assertEqual(terminal['outcome'],'Failed');self.assertIsNone(terminal['storageOwnership'])
        self.assertTrue((supervisor.root/'supervisor/files.json').is_file())

    def at_final_snapshot(self,supervisor,mutation):
        original=supervisor._storage_snapshot
        def changed():mutation();return original()
        return patch.object(supervisor,'_storage_snapshot',side_effect=changed)

    def test_changed_retained_copy_rejects_final_success(self):
        supervisor=self.ready()
        def mutate():
            path=supervisor.root/'owner/native-work.json';raw=path.read_bytes();path.write_bytes(b'!'+raw[1:])
        with self.at_final_snapshot(supervisor,mutate):self.failed(supervisor,'content')
        self.assertIsNone(supervisor.terminal_observation['storageOwnership'])

    def test_changed_storage_binding_rejects_final_success(self):
        supervisor=self.ready()
        def mutate():
            path=supervisor.root/'supervisor/storage-binding.json';raw=path.read_bytes();path.write_bytes(b'!'+raw[1:])
        with self.at_final_snapshot(supervisor,mutate):self.failed(supervisor,'Changed supervisor storage binding')

    def test_unclassified_top_level_member_rejects_final_success(self):
        supervisor=self.ready()
        with self.at_final_snapshot(supervisor,lambda:(supervisor.root/'extra.tmp').write_bytes(b'x')):
            self.failed(supervisor,'Unclassified')

    def test_untracked_terminal_member_rejects_final_success(self):
        supervisor=self.ready()
        with self.at_final_snapshot(supervisor,lambda:(supervisor.root/'terminal/extra.tmp').write_bytes(b'x')):
            self.failed(supervisor,'Untracked')

    def test_original_worker_sidecars_remain_outside_owned_bounds(self):
        supervisor=self.ready()
        def external():
            path=supervisor.root/'worker-receipts/undeclared-cache.tmp';path.write_bytes(b'x'*5000);path.unlink()
        with self.at_final_snapshot(supervisor,external):self.launch(supervisor)
        value=self.verify_storage(supervisor)
        self.assertIn('workerReceiptOriginals',value['missingCoverage']);self.assertFalse(value['externalWritesBounded'])
        self.assertEqual(value['currentScratchBytes'],0)

    def test_worker_error_survives_later_terminal_limit_failure(self):
        value=contracts();value['terminal']['files']['supervisor-observation.json']['maxBytes']=0
        supervisor=self.ready(value=value);self.failed_phase='native'
        error=self.failed(supervisor,'Native comparison failed')
        self.assertIn('Supervisor publication failed',str(error.__notes__))
        self.assertEqual(supervisor.terminal_observation['outcome'],'Failed')

    def test_contract_binding_is_written_once_across_resource_check_change(self):
        supervisor=self.ready();names=[];original=supervisor._seal
        def sealing(name,value):names.append(name);return original(name,value)
        with patch.object(supervisor,'_seal',side_effect=sealing):self.launch(supervisor)
        self.assertEqual(names.count('storage-binding.json'),1)
        self.assertEqual(supervisor.publication_counters.values['diagnosticCreateCompleted'],3)

    def test_reuse_preserves_first_contract_and_evidence(self):
        supervisor=self.ready();self.launch(supervisor)
        before={p.relative_to(supervisor.root):p.read_bytes() for p in supervisor.root.rglob('*') if p.is_file()}
        terminal=w.canonical(supervisor.terminal_observation)
        with self.assertRaisesRegex(ValueError,'No retry, resume or existing output'):self.launch(supervisor)
        self.assertEqual(w.canonical(supervisor.terminal_observation),terminal)
        self.assertEqual(before,{p.relative_to(supervisor.root):p.read_bytes() for p in supervisor.root.rglob('*') if p.is_file()})


if __name__=='__main__':unittest.main()
