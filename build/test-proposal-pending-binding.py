"""Native v5 pending contracts: strict owner validation and literal interoperability."""
import copy
import hashlib
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as w
import bounded_windows_process as owned

def sha(path):
    with Path(path).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def limits():return dict(receipt=100000,publication=100000,persistence=100000,total=300000)
def budget(root):return dict(files=[dict(path=str(root/'literal.pending'),destination=str(root/'literal.json'),maxBytes=3,maxCreates=2)],maxLiveBytes=3,maxTotalWrittenBytes=6)
def snapshot(declaration):
    return dict(version=w.NATIVE_PENDING_VERSION,outcome='Complete',maxLiveBytes=declaration['maxLiveBytes'],
        maxTotalWrittenBytes=declaration['maxTotalWrittenBytes'],acceptedWriteBytes=6,trackedLiveBytes=0,peakTrackedLiveBytes=3,
        publishedBytes=6,deletedBytes=0,failedProgressMayBeUnknown=False,contracts=copy.deepcopy(declaration['files']),
        creates=[dict(path=declaration['files'][0]['path'],count=2)],scope='DeclaredInstrumentedPendingWritersOnly',
        filesystemConfinement=False,wholeProcessCoverage=False,usableForAdmission=False)
def seal(root):
    w.seal_new(root/'files.json',{p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})
def manifest(root):
    m=w.strict((root/'files.json').read_bytes())
    assert set(m)=={p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and p!=root/'files.json'}
    for n,pin in m.items():assert sha(root/n)==pin,n


class Contract(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
        self.value=budget(self.root)
    def test_declaration_copies_every_mutable_member(self):
        result=w.native_pending_budget(self.value);self.value['files'][0]['maxBytes']=999;self.value['files'].clear()
        self.assertEqual(result,budget(self.root))
    def test_invalid_declarations_reject(self):
        changes=[lambda v:v.update(extra=1),lambda v:v.update(files=[]),lambda v:v.update(maxLiveBytes=True),
            lambda v:v.update(maxTotalWrittenBytes=2**63),lambda v:v['files'][0].update(maxCreates=0),
            lambda v:v['files'][0].update(maxCreates=2**31),lambda v:v['files'][0].update(maxBytes=-1),
            lambda v:v['files'][0].update(extra=0),lambda v:v['files'].append(copy.deepcopy(v['files'][0])),
            lambda v:v['files'][0].update(destination=v['files'][0]['path']),
            lambda v:v['files'][0].update(destination=v['files'][0]['path']+os.sep+'child'),
            lambda v:v['files'][0].update(path='relative.pending')]
        for change in changes:
            v=copy.deepcopy(self.value);change(v)
            with self.subTest(value=v),self.assertRaises(ValueError):w.native_pending_budget(v)
    def test_noncanonical_names_reject(self):
        for name in ('bad.','bad ','CON','LPT1.json','ADS:stream','a'+os.sep+'..'+os.sep+'b'):
            v=copy.deepcopy(self.value);v['files'][0]['path']=str(self.root)+os.sep+name
            with self.subTest(name=name),self.assertRaises(ValueError):w.native_pending_budget(v)
    def test_protected_files_and_ancestors_reject(self):
        for protected in (self.root,self.root/'literal.pending',self.root/'literal.json',self.root/'literal.json'/'child'):
            with self.subTest(path=protected),self.assertRaises(ValueError):w.native_pending_budget(self.value,protected_paths=[protected])
    def test_snapshot_accepts_exact_caps_and_releases(self):self.assertEqual(w.native_pending_observation(snapshot(self.value)),self.value)
    def test_bad_snapshot_counters_and_coverage_reject(self):
        changes=[dict(acceptedWriteBytes=True),dict(acceptedWriteBytes=7),dict(trackedLiveBytes=1),dict(peakTrackedLiveBytes=4),
            dict(publishedBytes=5),dict(deletedBytes=-1),dict(failedProgressMayBeUnknown=True),dict(wholeProcessCoverage=True),
            dict(filesystemConfinement=True),dict(usableForAdmission=True),dict(creates=[]),dict(extra=1),dict(scope='AllFiles'),
            dict(creates=[dict(path=self.value['files'][0]['path'],count=1)]),dict(creates=[dict(path=self.value['files'][0]['path'],count=True)])]
        for change in changes:
            with self.subTest(change=change),self.assertRaises(ValueError):w.native_pending_observation(snapshot(self.value)|change)
    def test_failed_observation_preserves_unresolved_bytes(self):
        v=snapshot(self.value)|dict(outcome='FailedOrIncomplete',failedProgressMayBeUnknown=True,trackedLiveBytes=3,publishedBytes=3)
        self.assertEqual(w.native_pending_observation(v),self.value)
    def test_python_worker_rejects_native_only_binding_before_files(self):
        study=self.root/'study';study.mkdir();(study/'request.json').write_bytes(b'{}')
        value=dict(version=w.WORKER_PENDING_BINDING_VERSION,phase='independentAudit',studyRoot=str(study),
            requestSha256=sha(study/'request.json'),producerSha256=sha(__file__),accountingModuleSha256=sha(w.__file__),
            receiptPath=str(self.root/'receipt.json'),publicationPath=str(self.root/'publication.json'),
            publicationPersistencePath=str(self.root/'persistence.json'),sidecarByteLimits=limits(),pendingStorageBudget=self.value)
        pin=w.seal_new(self.root/'binding.json',value)
        with self.assertRaises(ValueError),w.worker_receipt(study,'independentAudit',self.root/'binding.json',pin,__file__):self.fail('invoked')
        for name in ('receipt.json','publication.json','persistence.json'):self.assertFalse((self.root/name).exists())


class Owner(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
        self.study=self.root/'study';self.study.mkdir();(self.study/'request.json').write_bytes(b'{}')
        self.producer=self.root/'literal-producer';self.producer.write_bytes(b'literal native identity')
        self.owner=w.RetainedOwner(self.root/'owner',sha(self.study/'request.json'),sha(self.producer),sha(w.__file__))
        self.called=False
    def invoke(self,binding_path,pin,mutate=None,error=None):
        self.called=True;b=w.strict(binding_path.read_bytes())
        value=w.Counters().receipt('native',b['requestSha256'],b['producerSha256'],error is None)
        value.update(version=w.NATIVE_PENDING_RECEIPT_VERSION,bindingSha256=pin,pendingStorage=snapshot(b['pendingStorageBudget']))
        if error is not None:value['pendingStorage'].update(outcome='FailedOrIncomplete',failedProgressMayBeUnknown=True)
        if mutate:mutate(value)
        with w.SidecarWrites(limits()) as sidecars,w.receipt_publication_scope(Path(b['publicationPath']),pin,'native',b['requestSha256'],
                b['producerSha256'],persistence_path=Path(b['publicationPersistencePath']),sidecars=sidecars) as publication:
            publication.open(Path(b['receiptPath']));publication.publish(lambda:value)
        if error:raise error
        return 7
    def run_worker(self,mutate=None,error=None,**options):
        values=dict(publication_path=self.root/'publication.json',publication_persistence_path=self.root/'persistence.json',
                    sidecar_byte_limits=limits(),pending_storage_budget=budget(self.study))|options
        return self.owner.run_worker(self.study,self.producer,self.producer,self.root/'receipt.json',
            lambda p,h:self.invoke(p,h,mutate,error),**values)
    def scope(self):
        self.owner.__enter__();phase=self.owner.phase('native',sha(self.producer));phase.__enter__()
        def cleanup():
            phase.__exit__(None,None,None)
            self.owner.__exit__(RuntimeError,RuntimeError('Single-phase literal fixture closeout'),None)
        self.addCleanup(cleanup)
    def test_owner_binds_validates_and_retains_exact_native_declaration(self):
        self.scope();self.assertEqual(self.run_worker(),7)
        b=w.strict((self.root/'owner/native-binding.json').read_bytes())
        r=w.strict((self.root/'owner/native-work.json').read_bytes())
        self.assertEqual(b['version'],w.WORKER_PENDING_BINDING_VERSION);self.assertEqual(b['pendingStorageBudget'],budget(self.study))
        self.assertEqual(r['bindingSha256'],sha(self.root/'owner/native-binding.json'))
        self.assertEqual(len(self.owner.ledger.workers),1)
    def test_owner_rejects_changed_binding_pin(self):
        self.scope()
        with self.assertRaisesRegex(ValueError,'Changed native pending'):self.run_worker(lambda r:r.update(bindingSha256='a'*64))
        self.assertFalse((self.root/'owner/native-work.json').exists())
    def test_owner_rejects_changed_valid_declaration(self):
        self.scope()
        with self.assertRaisesRegex(ValueError,'Changed native pending'):self.run_worker(lambda r:r['pendingStorage'].update(maxLiveBytes=4))
    def test_owner_rejects_omitted_observation(self):
        self.scope()
        with self.assertRaisesRegex(ValueError,'Unknown receipt fields'):self.run_worker(lambda r:r.pop('pendingStorage'))
    def test_owner_rejects_downgraded_receipt(self):
        self.scope()
        def downgrade(r):r.update(version=w.VERSION);r.pop('bindingSha256');r.pop('pendingStorage')
        with self.assertRaisesRegex(ValueError,'Unexpected worker receipt version'):self.run_worker(downgrade)
    def test_owner_preserves_original_failure_and_valid_failed_receipt(self):
        self.scope();original=RuntimeError('literal body')
        with self.assertRaises(RuntimeError) as caught:self.run_worker(error=original)
        self.assertIs(caught.exception,original)
        self.assertEqual(w.strict((self.root/'owner/native-work.json').read_bytes())['outcome'],'Failed')
    def test_owner_preserves_original_when_retention_rejects(self):
        self.scope();original=RuntimeError('literal body')
        with self.assertRaises(RuntimeError) as caught:self.run_worker(lambda r:r.update(bindingSha256='a'*64),error=original)
        self.assertIs(caught.exception,original);self.assertIn('retention failed',str(original.__notes__))
    def test_missing_sidecar_limits_reject_before_invoke(self):
        self.scope()
        with self.assertRaisesRegex(ValueError,'bounded sidecars'):self.run_worker(sidecar_byte_limits=None)
        self.assertFalse(self.called);self.assertFalse((self.root/'owner/native-binding.json').exists())
    def test_owner_storage_overlap_rejects_before_binding(self):
        self.scope()
        with self.assertRaisesRegex(ValueError,'protected'):self.run_worker(pending_storage_budget=budget(self.root/'owner'))
        self.assertFalse(self.called);self.assertFalse((self.root/'owner/native-binding.json').exists())
    def test_oversized_binding_rejects_before_binding_file(self):
        self.scope();v=budget(self.study)
        v['files']=[dict(path=str(self.study/f'{n:04d}.pending'),destination=str(self.study/f'{n:04d}.json'),maxBytes=3,maxCreates=2) for n in range(200)]
        with self.assertRaisesRegex(ValueError,'size limit'):self.run_worker(pending_storage_budget=v)
        self.assertFalse(self.called);self.assertFalse((self.root/'owner/native-binding.json').exists())
    def test_independent_phase_rejects_native_budget(self):
        self.scope();self.owner.phase_name='independentAudit'
        with self.assertRaisesRegex(ValueError,'native phase'):self.run_worker()
        self.assertFalse(self.called)


class Native(unittest.TestCase):
    def test_current_native_exports_pass_independent_validation(self):
        root=Path(os.environ['LL_PENDING_BINDING_EXPORT'])
        for name in ('native','nativeAudit','publication','failed-cap','failed-unresolved','failed-undeclared','failed-swallowed','failed-body'):
            case=root/name;manifest(case);b=w.strict((case/'binding.json').read_bytes())
            meta=w.strict((case/'fixture.json').read_bytes());self.assertEqual(sha(meta['producerPath']),meta['producerSha256'])
            r=w.verify_counter_receipt(case/'receipt.json',sha(case/'receipt.json'),b['phase'],b['requestSha256'],b['producerSha256'])
            self.assertEqual(r['bindingSha256'],sha(case/'binding.json'))
            self.assertEqual(w.native_pending_observation(r['pendingStorage']),w.native_pending_budget(b['pendingStorageBudget']))
            self.assertEqual(r['outcome'],'Failed' if name.startswith('failed-') else 'Complete')
            p=w.worker_publication((case/'publication.json').read_bytes(),sha(case/'binding.json'),b['phase'],b['requestSha256'],b['producerSha256'])
            self.assertEqual(p['serializedReceiptSha256'],sha(case/'receipt.json'))
            q=w.worker_publication_persistence((case/'persistence.json').read_bytes(),sha(case/'binding.json'),b['phase'],b['requestSha256'],b['producerSha256'])
            self.assertEqual(q['serializedObservationSha256'],sha(case/'publication.json'))
    def test_real_native_audit_retains_v5_receipt_through_owner(self):
        fixture=Path(os.environ['LL_PENDING_BINDING_AUDIT_FIXTURE']).resolve();manifest(fixture)
        meta=w.strict((fixture/'fixture.json').read_bytes());study=Path(meta['outputRoot']);dll=Path(meta['nativeDll'])
        for key in ('actualCombat','productionEntropyDraws','scientificReservations','nativeEncounterPreparations'):self.assertEqual(meta[key],0)
        base=Path(os.environ['LL_PENDING_BINDING_AUDIT_EXPORT']).resolve();base.mkdir()
        owner=w.RetainedOwner(base/'owner',meta['requestSha256'],sha(dll),sha(w.__file__))
        owner.__enter__()
        try:
            with owner.phase('native',sha(dll)):pass
            with owner.phase('nativeAudit',sha(dll)):
                def invoke(binding,pin):
                    command=['dotnet',str(dll),'tower-proposal-study-audit',str(study),'--work-binding',str(binding),pin]
                    observations=[]
                    def observe(value):observations.append(value);owner.observe_process(value,sha(owned.__file__))
                    result=owned.run(command,ROOT,base/'console.log',time.monotonic()+600,observe=observe,log_byte_limit=65536)
                    w.seal_new(base/'process.json',dict(command=command,result=result,observation=observations[0]))
                    self.assertEqual(result['exitCode'],0,(base/'console.log').read_text())
                    return result
                declaration=budget(base);declaration.update(maxLiveBytes=0,maxTotalWrittenBytes=0)
                owner.run_worker(study,dll,dll,base/'receipt.json',invoke,publication_path=base/'publication.json',
                    publication_persistence_path=base/'persistence.json',sidecar_byte_limits=limits(),pending_storage_budget=declaration)
                r=w.strict((base/'owner/nativeAudit-work.json').read_bytes())
                self.assertEqual(r['outcome'],'Complete');self.assertEqual(r['pendingStorage']['acceptedWriteBytes'],0)
                self.assertEqual(r['counters']['reconstructedTrialBindings'],meta['fights'])
        finally:
            owner.__exit__(RuntimeError,RuntimeError('Single-phase diagnostic: no complete scientific owner sequence'),None)
        manifest(base/'owner');self.assertEqual(owner.receipt['ledger']['outcome'],'Failed')
        w.seal_new(base/'fixture.json',dict(realNativeAuditCommand=True,realProcessObservations=1,actualCombat=0,productionEntropyDraws=0,
            scientificReservations=0,nativeEncounterPreparations=0,wholeProcessCoverage=False,usableForAdmission=False,
            ownerSequenceComplete=False,workerOutcome='Complete',sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p) for p in (__file__,w.__file__,owned.__file__)}))
        seal(base)


if __name__=='__main__':unittest.main()
