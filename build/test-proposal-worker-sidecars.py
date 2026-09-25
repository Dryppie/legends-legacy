"""V4 original sidecar bounds; literal worker bodies and retained native fixtures."""
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
p=load('sidecar_publication_fixture','build/test-proposal-receipt-publication.py')
f=load('sidecar_supervisor_fixture','build/test-proposal-owner-supervisor.py')
t=load('sidecar_storage_fixture','build/test-proposal-supervisor-storage.py')
w,s,sha,owned=p.w,f.s,p.sha,p.owned
def limits(**changes):return dict(receipt=100000,publication=100000,persistence=100000,total=300000)|changes


class Sidecars(unittest.TestCase):
    def setUp(self):
        self.f=p.Publication();self.f.setUp();self.addCleanup(self.f.doCleanups)
        self.terminal=self.f.root/'persistence.json'
    def bind(self,**changes):
        return self.f.bind(**(dict(version=w.WORKER_SIDECAR_BINDING_VERSION,publicationPersistencePath=str(self.terminal),sidecarByteLimits=limits())|changes))
    def check(self):
        publication=self.f.observed()
        value=w.worker_publication_persistence(self.terminal.read_bytes(),sha(self.f.binding),'independentAudit',sha(self.f.request),sha(self.f.producer))
        self.assertEqual(publication['outcome'],'Complete');self.assertEqual(value['outcome'],'Complete')
        self.assertEqual(value['serializedObservationBytes'],self.f.publication.stat().st_size)
        self.assertTrue(value['terminalObservationPersistenceExcluded']);self.assertFalse(value['usableForAdmission'])
        return value
    def test_v4_success_preserves_receipt_schemas_and_exclusions(self):
        self.bind()
        with self.f.scope() as counters:counters.add('literalWork')
        self.check()
    def test_invalid_limits_reject_before_action_and_files(self):
        for value in (None,{},limits(extra=1),limits(receipt=-1),limits(total=True),limits(receipt=1.5),limits(total=2**63)):
            self.bind(sidecarByteLimits=value)
            with self.subTest(value=value),self.assertRaises(ValueError),self.f.scope():self.fail('body invoked')
            self.assertFalse(self.terminal.exists());self.assertFalse(self.f.receipt.exists())
    def test_v4_missing_limits_rejects_before_reservation(self):
        self.bind();value=w.strict(self.f.binding.read_bytes());value.pop('sidecarByteLimits');self.f.binding.write_bytes(w.canonical(value))
        with self.assertRaises(ValueError),self.f.scope():self.fail('body invoked')
        self.assertFalse(self.terminal.exists())
    def test_v3_cannot_smuggle_limits(self):
        self.bind(version=w.WORKER_PERSISTENCE_BINDING_VERSION)
        with self.assertRaises(ValueError),self.f.scope():self.fail('body invoked')
    def test_v4_requires_persistence(self):
        self.bind();value=w.strict(self.f.binding.read_bytes());value.pop('publicationPersistencePath');self.f.binding.write_bytes(w.canonical(value))
        with self.assertRaises(ValueError),self.f.scope():self.fail('body invoked')
    def cap(self,role):
        self.bind(sidecarByteLimits=limits(**{role:0}))
        with self.assertRaisesRegex(ValueError,'byte limit'),self.f.scope():pass
        paths={'receipt':self.f.receipt,'publication':self.f.publication,'persistence':self.terminal}
        for path in (paths.values() if role=='total' else [paths[role]]):self.assertEqual(path.stat().st_size,0)
    def test_receipt_cap(self):self.cap('receipt')
    def test_publication_cap(self):self.cap('publication')
    def test_terminal_cap(self):self.cap('persistence')
    def test_total_cap(self):self.cap('total')
    def test_existing_terminal_prevents_action_and_preserves_bytes(self):
        self.bind();self.terminal.write_bytes(b'previous')
        with self.assertRaises(FileExistsError),self.f.scope():self.fail('body invoked')
        self.assertEqual(self.terminal.read_bytes(),b'previous');self.assertFalse(self.f.receipt.exists())
    def test_worker_error_survives_cap_failure(self):
        self.bind(sidecarByteLimits=limits(receipt=0));error=RuntimeError('body')
        with self.assertRaises(RuntimeError) as caught:
            with self.f.scope():raise error
        self.assertIs(caught.exception,error);self.assertIn('publication failed',str(error.__notes__))
    def fault(self,mode):
        original=Path.open
        class Stream:
            def __init__(self,stream):self.stream=stream
            def write(self,raw):
                if mode=='short':return self.stream.write(raw[:max(1,len(raw)//2)])
                if mode=='partial':self.stream.write(raw[:2]);raise OSError('hidden prefix')
                if mode=='invalid':return None
                return self.stream.write(raw)
            def flush(self):return self.stream.flush()
            def fileno(self):return self.stream.fileno()
            def close(self):return self.stream.close()
        def opening(path,*args,**kwargs):
            stream=original(path,*args,**kwargs)
            return Stream(stream) if path==self.f.receipt and args and args[0]=='xb' else stream
        return patch.object(Path,'open',opening)
    def test_partial_returns_count_only_accepted_bytes(self):
        self.bind()
        with self.fault('short'),self.f.scope():pass
        self.check();self.assertGreater(self.f.observed()['counters']['shortWrites'],0)
    def test_hidden_prefix_stops_following_sidecar_writes(self):
        self.bind()
        with self.fault('partial'),self.assertRaisesRegex(OSError,'hidden prefix'),self.f.scope():pass
        self.assertEqual(self.f.receipt.read_bytes(),b'{"')
        self.assertEqual(self.f.publication.stat().st_size,0);self.assertEqual(self.terminal.stat().st_size,0)
    def test_invalid_write_result_poisons_scope(self):
        self.bind()
        with self.fault('invalid'),self.assertRaisesRegex(ValueError,'write result'),self.f.scope():pass
        self.assertEqual(self.f.publication.stat().st_size,0)
    def test_changed_closed_length_rejects_success(self):
        self.bind();original=w.SidecarWrites.__exit__
        def closing(owner,*args):
            with self.terminal.open('ab') as stream:stream.write(b'x')
            return original(owner,*args)
        with patch.object(w.SidecarWrites,'__exit__',closing),self.assertRaisesRegex(ValueError,'Changed worker sidecar length'),self.f.scope():pass
    def test_caps_are_copied_before_caller_mutation(self):
        value=limits(receipt=1);owner=w.SidecarWrites(value);value['receipt']=100
        stream=owner.open('receipt',self.f.receipt)
        with self.assertRaisesRegex(ValueError,'byte limit'):stream.write(b'xx')
        stream.close()
    def test_append_only_writer_exposes_no_seek_or_truncate(self):
        owner=w.SidecarWrites(limits());stream=owner.open('receipt',self.f.receipt)
        self.assertFalse(hasattr(stream,'seek'));self.assertFalse(hasattr(stream,'truncate'));stream.close()


class Owner(unittest.TestCase):
    def setUp(self):
        self.f=f.Supervisor();self.f.setUp();self.addCleanup(self.f.doCleanups)
    def prepare(self,base=None,**options):
        old=self.f.prepare(base)
        shutil.copyfile(ROOT/'Balance Harness/analysis/proposal_diagnostic_storage.py',self.f.package/'proposal_diagnostic_storage.py');self.f.reseal()
        return s.StudyWorkSupervisor(old.root,worker_observation_persistence=True,storage_contracts=t.contracts(True),
            worker_sidecar_limits=options.get('worker_sidecar_limits',{phase:limits() for phase in w.PHASES}))
    def test_full_supervisor_binds_and_checks_all_four_original_writers(self):
        export=os.environ.get('LL_WORKER_SIDECAR_OWNER_EXPORT');base=Path(export).resolve() if export else self.f.base
        supervisor=self.prepare(base);self.f.launch(supervisor);self.f.verify(supervisor)
        observed=supervisor.terminal_observation['storageOwnership']
        self.assertEqual(observed['declaredWorkerSidecarByteUpperBound'],1200000)
        self.assertNotIn('workerReceiptOriginals',observed['missingCoverage']);self.assertIn('workerSidecarExternalMutations',observed['missingCoverage'])
        for phase in w.PHASES:
            b=w.strict((supervisor.root/'owner'/(phase+'-binding.json')).read_bytes())
            self.assertEqual(b['version'],w.WORKER_SIDECAR_BINDING_VERSION)
            self.assertEqual(b['sidecarByteLimits'],limits())
            self.assertLessEqual(sum(Path(b[key]).stat().st_size for key in ('receiptPath','publicationPath','publicationPersistencePath')),300000)
        if export:
            w.seal_new(base/'terminal.json',supervisor.terminal_observation)
            w.seal_new(base/'fixture.json',dict(actualCombat=0,productionEntropyDraws=0,syntheticProcessObservations=4,realProcessObservations=0,
                wholeProcessCoverage=False,usableForAdmission=False,sources={Path(path).resolve().relative_to(ROOT).as_posix():sha(path) for path in (__file__,w.__file__,s.__file__,f.__file__)}))
            w.seal_new(base/'files.json',{p.relative_to(base).as_posix():sha(p) for p in base.rglob('*') if p.is_file()})
    def test_incomplete_phase_declarations_reject_before_io(self):
        with self.assertRaises(ValueError):s.StudyWorkSupervisor(self.f.base/'accounting',worker_observation_persistence=True,worker_sidecar_limits={})
    def test_limits_require_persistent_worker_binding(self):
        with self.assertRaises(ValueError):s.StudyWorkSupervisor(self.f.base/'accounting',worker_sidecar_limits={phase:limits() for phase in w.PHASES})
    def test_native_original_cap_failure_stops_following_workers(self):
        values={phase:limits() for phase in w.PHASES};values['native']['receipt']=0
        supervisor=self.prepare(worker_sidecar_limits=values)
        with self.assertRaisesRegex(ValueError,'byte limit'):self.f.launch(supervisor)
        self.assertEqual([phase for phase,_,_ in self.f.calls],['native']);self.assertEqual(supervisor.terminal_observation['outcome'],'Failed')
    def test_owner_rejects_worker_that_returns_oversized_original(self):
        supervisor=self.prepare();original=self.f.routed
        def enlarged(*args,**kwargs):
            value=original(*args,**kwargs)
            path=supervisor.root/'worker-receipts/native-publication-persistence.json'
            with path.open('ab') as stream:stream.write(b' '*100001)
            return value
        with patch.object(self.f,'routed',side_effect=enlarged),self.assertRaisesRegex(ValueError,'bound sidecar'):self.f.launch(supervisor)
        self.assertEqual(supervisor.terminal_observation['outcome'],'Failed')


class Native(unittest.TestCase):
    def test_v4_actual_audit_command_closes_bounded_originals(self):
        fixture=Path(os.environ['LL_WORKER_SIDECAR_AUDIT_FIXTURE']).resolve()
        meta=w.strict((fixture/'fixture.json').read_bytes());study=Path(meta['outputRoot']);dll=Path(meta['nativeDll'])
        base=Path(os.environ['LL_WORKER_SIDECAR_AUDIT_EXPORT']).resolve();base.mkdir()
        binding=dict(version=w.WORKER_SIDECAR_BINDING_VERSION,phase='nativeAudit',studyRoot=str(study),
            requestSha256=meta['requestSha256'],producerSha256=sha(dll),accountingModuleSha256=sha(dll),
            receiptPath=str(base/'receipt.json'),publicationPath=str(base/'publication.json'),
            publicationPersistencePath=str(base/'persistence.json'),sidecarByteLimits=limits())
        pin=w.seal_new(base/'binding.json',binding);observations=[]
        command=['dotnet',str(dll),'tower-proposal-study-audit',str(study),'--work-binding',str(base/'binding.json'),pin]
        result=owned.run(command,ROOT,base/'console.log',time.monotonic()+600,observe=observations.append)
        w.seal_new(base/'process.json',dict(command=command,result=result,observation=observations[0]))
        self.assertEqual(result['exitCode'],0,(base/'console.log').read_text())
        receipt=w.verify_counter_receipt(base/'receipt.json',sha(base/'receipt.json'),'nativeAudit',meta['requestSha256'],sha(dll))
        self.assertEqual(receipt['outcome'],'Complete')
        self.assertEqual(receipt['counters']['reconstructedTrialBindings'],meta['fights'])
        publication=w.worker_publication((base/'publication.json').read_bytes(),pin,'nativeAudit',meta['requestSha256'],sha(dll))
        persistence=w.worker_publication_persistence((base/'persistence.json').read_bytes(),pin,'nativeAudit',meta['requestSha256'],sha(dll))
        self.assertEqual(publication['outcome'],persistence['outcome']);self.assertEqual(persistence['outcome'],'Complete')
        self.assertLessEqual(sum((base/(role+'.json')).stat().st_size for role in ('receipt','publication','persistence')),300000)
        w.seal_new(base/'fixture.json',dict(realNativeAuditCommand=True,realProcessObservations=1,actualCombat=0,
            productionEntropyDraws=0,nativeInputMaterializationPerformed=True,nativeEncounterPreparationPerformed=False,
            wholeProcessCoverage=False,usableForAdmission=False,sources={Path(path).resolve().relative_to(ROOT).as_posix():sha(path) for path in (__file__,w.__file__,owned.__file__)}))
        w.seal_new(base/'files.json',{p.relative_to(base).as_posix():sha(p) for p in base.rglob('*') if p.is_file()})

    def test_native_exports_validate_with_python(self):
        root=Path(os.environ['LL_WORKER_SIDECAR_NATIVE_EXPORT'])
        for phase in ('native','nativeAudit','publication'):
            base=root/phase;manifest=w.strict((base/'files.json').read_bytes())
            for name,pin in manifest.items():self.assertEqual(sha(base/name),pin)
            b=w.strict((base/'binding.json').read_bytes());self.assertEqual(b['version'],w.WORKER_SIDECAR_BINDING_VERSION)
            w.verify_counter_receipt(base/'receipt.json',sha(base/'receipt.json'),phase,b['requestSha256'],b['producerSha256'])
            w.worker_publication((base/'publication.json').read_bytes(),sha(base/'binding.json'),phase,b['requestSha256'],b['producerSha256'])
            w.worker_publication_persistence((base/'persistence.json').read_bytes(),sha(base/'binding.json'),phase,b['requestSha256'],b['producerSha256'])
            self.assertLessEqual(sum((base/(role+'.json')).stat().st_size for role in ('receipt','publication','persistence')),b['sidecarByteLimits']['total'])


if __name__=='__main__':unittest.main()
