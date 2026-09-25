"""Python receipt publication boundaries; literal workers, no scientific runs."""
import copy
import hashlib
import importlib.util
import io
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
ANALYSIS = ROOT/'Balance Harness/analysis'
sys.path.insert(0, str(ANALYSIS))
import proposal_work_accounting as w
import bounded_windows_process as owned


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class Publication(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.prepare(Path(self.temp.name))

    def prepare(self, root):
        self.root = root; root.mkdir(exist_ok=True)
        self.study = root/'study'; self.study.mkdir()
        self.request = self.study/'request.json'; self.request.write_bytes(b'{"literal":true}')
        self.receipt, self.publication, self.binding = (root/n for n in ('worker.json','publication.json','binding.json'))
        self.producer = Path(__file__).resolve()

    def bind(self, **changes):
        value = dict(version=w.WORKER_PUBLICATION_BINDING_VERSION, phase='independentAudit', studyRoot=str(self.study),
            requestSha256=sha(self.request), producerSha256=sha(self.producer), accountingModuleSha256=sha(w.__file__),
            receiptPath=str(self.receipt), publicationPath=str(self.publication))
        value.update(changes); self.binding.write_bytes(w.canonical(value))
        return sha(self.binding)

    def scope(self):
        return w.worker_receipt(self.study,'independentAudit',self.binding,sha(self.binding),self.producer)

    def observed(self):
        return w.worker_publication(self.publication.read_bytes(),sha(self.binding),'independentAudit',sha(self.request),sha(self.producer))

    def fake_receipt(self, stream):
        original = Path.open
        def opened(path, *args, **kwargs):
            return stream if path==self.receipt else original(path,*args,**kwargs)
        return patch.object(Path,'open',new=opened)

    def test_success_counts_reservation_serialization_sync_and_close_exactly(self):
        self.bind()
        with self.scope() as work: work.add('literalWork',3)
        value = self.observed(); counts = value['counters']
        self.assertEqual(value['outcome'],'Complete')
        self.assertEqual(value['serializedReceiptSha256'],sha(self.receipt))
        self.assertEqual(value['serializedReceiptBytes'],self.receipt.stat().st_size)
        self.assertEqual(counts['acceptedWriteBytes'],self.receipt.stat().st_size)
        for op in ('open','serialize','write','flush','sync','close'):
            self.assertEqual(counts[op+'Attempted'],1); self.assertEqual(counts[op+'Completed'],1)
        self.assertTrue(value['observationPersistenceExcluded'])
        self.assertFalse(value['wholeProcessCoverage']); self.assertFalse(value['usableForAdmission'])
        self.assertNotIn('acceptedWriteBytes',w.strict(self.receipt.read_bytes())['counters'])

    def test_v1_uses_original_publication_without_sidecar(self):
        self.bind(); value=w.strict(self.binding.read_bytes()); value.pop('publicationPath'); value['version']=w.WORKER_BINDING_VERSION
        self.binding.write_bytes(w.canonical(value))
        with patch.object(w,'ReceiptPublication') as observation:
            with self.scope(): pass
        observation.assert_not_called(); self.assertFalse(self.publication.exists())
        self.assertEqual(w.strict(self.receipt.read_bytes())['outcome'],'Complete')

    def test_wrong_version_missing_or_extra_fields_reject_before_work(self):
        for change in ({'version':'unknown'},{'version':w.WORKER_BINDING_VERSION},{'publicationPath':'relative'},
                       {'publicationPath':str(self.study/'bad.json')},{'publicationPath':str(self.receipt)},{'extra':True}):
            self.bind(**change)
            with self.subTest(change=change),self.assertRaises(ValueError),self.scope(): self.fail('Action invoked')
            self.assertFalse(self.publication.exists()); self.assertFalse(self.receipt.exists())
        self.bind(); value=w.strict(self.binding.read_bytes()); value.pop('publicationPath'); self.binding.write_bytes(w.canonical(value))
        with self.assertRaises(ValueError),self.scope(): self.fail('Action invoked')

    def test_changed_binding_or_identity_creates_no_observation(self):
        self.bind(producerSha256='a'*64)
        with self.assertRaisesRegex(ValueError,'Changed worker'),self.scope(): self.fail('Action invoked')
        self.assertFalse(self.publication.exists()); self.assertFalse(self.receipt.exists())

    def test_existing_observation_is_preserved_before_receipt_reservation(self):
        self.bind(); self.publication.write_bytes(b'previous')
        with self.assertRaises(FileExistsError),self.scope(): self.fail('Action invoked')
        self.assertEqual(self.publication.read_bytes(),b'previous'); self.assertFalse(self.receipt.exists())

    def test_receipt_open_failure_is_observed_without_worker_action(self):
        self.bind(); self.receipt.write_bytes(b'previous')
        with self.assertRaises(FileExistsError),self.scope(): self.fail('Action invoked')
        value=self.observed(); self.assertEqual(value['outcome'],'Failed')
        self.assertEqual(value['counters'],dict(openAttempted=1,openFailed=1))
        self.assertIsNone(value['serializedReceiptBytes']); self.assertEqual(self.receipt.read_bytes(),b'previous')

    def test_failed_body_and_cancellation_keep_complete_publication_separate(self):
        self.bind(); error=KeyboardInterrupt('literal cancellation')
        with self.assertRaises(KeyboardInterrupt) as caught:
            with self.scope(): raise error
        self.assertIs(caught.exception,error)
        self.assertEqual(self.observed()['outcome'],'Complete')
        self.assertEqual(w.strict(self.receipt.read_bytes())['outcome'],'Failed')

    def test_short_binary_writes_are_retried_and_only_accepted_bytes_count(self):
        class Short(io.BytesIO):
            def write(self,data): return super().write(data[:17])
            def fileno(self): return 1234567
            def close(self): self.saved=self.getvalue(); super().close()
        stream=Short(); self.bind(); real_sync=w.os.fsync
        with self.fake_receipt(stream),patch.object(w.os,'fsync',side_effect=lambda fd:None if fd==1234567 else real_sync(fd)):
            with self.scope(): pass
        value=self.observed(); counts=value['counters']
        self.assertTrue(stream.closed); self.assertGreater(counts['shortWrites'],1)
        self.assertEqual(counts['writeCompleted'],counts['shortWrites']+1)
        self.assertEqual(counts['acceptedWriteBytes'],len(stream.saved))
        self.assertEqual(value['serializedReceiptSha256'],hashlib.sha256(stream.saved).hexdigest())

    def test_thrown_write_has_unknown_progress_and_still_closes(self):
        class Failed(io.BytesIO):
            def write(self,data): super().write(data[:1]); raise OSError('write failed')
        stream=Failed(); self.bind()
        with self.fake_receipt(stream),self.assertRaisesRegex(OSError,'write failed'):
            with self.scope(): pass
        value=self.observed(); self.assertEqual(value['outcome'],'Failed')
        self.assertEqual(value['counters']['failedWriteBytesUnknown'],1)
        self.assertNotIn('acceptedWriteBytes',value['counters'])
        self.assertEqual(value['counters']['closeCompleted'],1); self.assertTrue(stream.closed)

    def test_failure_after_short_write_retains_only_known_prefix(self):
        class Failed(io.BytesIO):
            calls=0
            def write(self,data):
                self.calls+=1
                if self.calls==1:return super().write(data[:11])
                super().write(data[:2]);raise OSError('later write failed')
        self.bind()
        with self.fake_receipt(Failed()),self.assertRaisesRegex(OSError,'later write'):
            with self.scope():pass
        counts=self.observed()['counters']
        self.assertEqual(counts['acceptedWriteBytes'],11);self.assertEqual(counts['shortWrites'],1)
        self.assertEqual(counts['writeCompleted'],1);self.assertEqual(counts['failedWriteBytesUnknown'],1)

    def test_invalid_write_results_cannot_claim_bytes(self):
        self.bind()
        for result in (None,True,0,-1,10**9):
            if self.publication.exists(): self.publication.unlink()
            class Invalid(io.BytesIO):
                def write(self,data): return result
            with self.subTest(result=result),self.fake_receipt(Invalid()),self.assertRaisesRegex(ValueError,'Incomplete worker'):
                with self.scope(): pass
            self.assertNotIn('acceptedWriteBytes',self.observed()['counters'])

    def test_serialization_failure_closes_reserved_receipt(self):
        self.bind()
        with patch.object(w.Counters,'receipt',side_effect=TypeError('serialization failed')),self.assertRaises(TypeError):
            with self.scope(): pass
        value=self.observed(); self.assertIsNone(value['serializedReceiptSha256'])
        self.assertEqual(value['counters']['serializeFailed'],1); self.assertEqual(value['counters']['closeCompleted'],1)
        self.assertEqual(self.receipt.read_bytes(),b'')

    def test_flush_failure_retains_accepted_bytes_and_skips_sync(self):
        class Failed(io.BytesIO):
            def flush(self): raise OSError('flush failed')
        self.bind(); stream=Failed()
        with self.fake_receipt(stream),self.assertRaisesRegex(OSError,'flush failed'):
            with self.scope(): pass
        value=self.observed(); counts=value['counters']
        self.assertEqual(counts['acceptedWriteBytes'],value['serializedReceiptBytes'])
        self.assertEqual(counts['flushFailed'],1); self.assertNotIn('syncAttempted',counts)
        self.assertTrue(stream.closed)

    def test_receipt_sync_failure_is_not_hidden_by_valid_receipt_bytes(self):
        self.bind(); real_sync=w.os.fsync; calls=[]
        def sync(fd):
            calls.append(fd)
            if len(calls)==1: raise OSError('sync failed')
            real_sync(fd)
        with patch.object(w.os,'fsync',side_effect=sync),self.assertRaisesRegex(OSError,'sync failed'):
            with self.scope(): pass
        self.assertEqual(w.strict(self.receipt.read_bytes())['outcome'],'Complete')
        self.assertEqual(self.observed()['outcome'],'Failed')
        self.assertEqual(self.observed()['counters']['syncFailed'],1)

    def test_close_failure_is_observed_and_original_body_error_survives(self):
        class Failed(io.BytesIO):
            def fileno(self): return 1234567
            def close(self): super().close(); raise OSError('close failed')
        self.bind(); real_sync=w.os.fsync; error=RuntimeError('original body failure')
        with self.fake_receipt(Failed()),patch.object(w.os,'fsync',side_effect=lambda fd:None if fd==1234567 else real_sync(fd)):
            with self.assertRaises(RuntimeError) as caught:
                with self.scope(): raise error
        self.assertIs(caught.exception,error); self.assertEqual(error.__notes__,['Work receipt publication failed; secondary details omitted.'])
        self.assertEqual(self.observed()['counters']['closeFailed'],1)

    def test_close_failure_after_success_prevents_normal_return(self):
        class Failed(io.BytesIO):
            def fileno(self):return 1234567
            def close(self):super().close();raise OSError('close failed')
        self.bind();real_sync=w.os.fsync
        with self.fake_receipt(Failed()),patch.object(w.os,'fsync',side_effect=lambda fd:None if fd==1234567 else real_sync(fd)):
            with self.assertRaisesRegex(OSError,'close failed'):
                with self.scope():pass
        self.assertEqual(self.observed()['outcome'],'Failed')

    def test_write_failure_is_not_replaced_by_secondary_close_failure(self):
        original=RuntimeError('original write')
        class Failed(io.BytesIO):
            def write(self,data): raise original
            def close(self): super().close(); raise OSError('secondary close')
        self.bind()
        with self.fake_receipt(Failed()),self.assertRaises(RuntimeError) as caught:
            with self.scope(): pass
        self.assertIs(caught.exception,original); self.assertEqual(original.__notes__,['Work receipt close failed; secondary details omitted.'])
        value=self.observed(); self.assertEqual([e['operation'] for e in value['errors']],['write','close'])

    def test_observation_persistence_failure_is_explicit_and_preserves_worker_error(self):
        self.bind(); error=RuntimeError('worker failed'); real_sync=w.os.fsync; calls=[]
        def sync(fd):
            calls.append(fd)
            if len(calls)==2: raise OSError('observation sync failed')
            real_sync(fd)
        with patch.object(w.os,'fsync',side_effect=sync),self.assertRaises(RuntimeError) as caught:
            with self.scope(): raise error
        self.assertIs(caught.exception,error); self.assertEqual(error.__notes__,['Publication observation persistence failed; secondary details omitted.'])
        self.assertEqual(w.strict(self.receipt.read_bytes())['outcome'],'Failed')

    def test_observation_persistence_failure_after_success_is_not_silent(self):
        self.bind(); real_sync=w.os.fsync; calls=[]
        def sync(fd):
            calls.append(fd)
            if len(calls)==2: raise OSError('observation sync failed')
            real_sync(fd)
        with patch.object(w.os,'fsync',side_effect=sync),self.assertRaisesRegex(OSError,'observation sync failed'):
            with self.scope(): pass

    def test_validation_rejects_changed_bindings_unknown_fields_and_false_coverage(self):
        self.bind()
        with self.scope(): pass
        baseline=self.observed()
        changes=[lambda v:v.update(version='unknown'),lambda v:v.update(bindingSha256='a'*64),
            lambda v:v.update(phase='native'),lambda v:v.update(requestSha256='a'*64),lambda v:v.update(producerSha256='a'*64),
            lambda v:v.update(extra=True),lambda v:v.update(wholeProcessCoverage=True),lambda v:v.update(usableForAdmission=True),
            lambda v:v.update(observationPersistenceExcluded=False),lambda v:v.update(outcome='Failed'),
            lambda v:v.update(serializedReceiptBytes=True),lambda v:v['counters'].update(acceptedWriteBytes=0),
            lambda v:v['counters'].update(writeCompleted=True),lambda v:v['counters'].update(closeCompleted=0),
            lambda v:v['counters'].update(unknown=1),lambda v:v['counters'].update(openFailed=1),
            lambda v:v.update(errors=[dict(operation='write',type='OSError',message='missing count')])]
        for index,change in enumerate(changes):
            value=copy.deepcopy(baseline); change(value)
            with self.subTest(case=index),self.assertRaises(ValueError):
                w.worker_publication(w.canonical(value),sha(self.binding),'independentAudit',sha(self.request),sha(self.producer))

    def test_auditor_cli_v2_preserves_default_result_bytes(self):
        spec=importlib.util.spec_from_file_location('publication_audit',ANALYSIS/'audit-proposal-affinity-study.py')
        audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
        self.producer=Path(audit.__file__);self.bind()
        default,counted=self.root/'default.json',self.root/'counted.json'
        with patch.object(audit,'audit',return_value=dict(fixtureOnly=True,text='æ🐉')):
            audit.main(['--working',str(self.study),'--output',str(default)])
            audit.main(['--working',str(self.study),'--output',str(counted),'--work-binding',str(self.binding),'--work-binding-pin',sha(self.binding)])
        self.assertEqual(default.read_bytes(),counted.read_bytes());self.assertEqual(self.observed()['outcome'],'Complete')


class Retention(unittest.TestCase):
    setUp=Publication.setUp
    prepare=Publication.prepare

    def owner(self):
        return w.RetainedOwner(self.root/'owner',sha(self.request),sha(__file__),sha(w.__file__),work=w.OwnerFileCounters())

    def invoke(self,binding,pin):
        with w.worker_receipt(self.study,'native',binding,pin,self.producer): pass

    def run_worker(self,owner,invoke):
        return owner.run_worker(self.study,self.producer,w.__file__,self.receipt,invoke,publication_path=self.publication)

    def test_missing_publication_cannot_complete_and_valid_receipt_is_retained(self):
        def invoke(binding,pin):
            self.invoke(binding,pin);self.publication.unlink()
        with self.assertRaises(FileNotFoundError):
            with self.owner() as owner:
                with owner.phase('native',sha(self.producer)): self.run_worker(owner,invoke)
        self.assertEqual(owner.receipt['ledger']['outcome'],'Failed')
        self.assertEqual(len(owner.receipt['ledger']['workers']),1)

    def test_failed_publication_retained_if_invoker_swallowed_sync_failure(self):
        def invoke(binding,pin):
            real_sync=w.os.fsync; calls=[]
            def sync(fd):
                calls.append(fd)
                if len(calls)==1: raise OSError('receipt sync')
                real_sync(fd)
            with patch.object(w.os,'fsync',side_effect=sync):
                try:self.invoke(binding,pin)
                except OSError:pass
        with self.assertRaisesRegex(ValueError,'publication failed'):
            with self.owner() as owner:
                with owner.phase('native',sha(self.producer)): self.run_worker(owner,invoke)
        self.assertEqual(len(owner.receipt['ledger']['workers']),1)
        observed=w.strict((owner.storage.root/'native-receipt-publication.json').read_bytes())
        self.assertEqual(observed['outcome'],'Failed')

    def test_changed_valid_receipt_cannot_match_publication(self):
        def invoke(binding,pin):
            self.invoke(binding,pin);value=w.strict(self.receipt.read_bytes());value['counters']['tampered']=1
            self.receipt.write_bytes(w.canonical(value))
        with self.assertRaisesRegex(ValueError,'Changed published receipt'):
            with self.owner() as owner:
                with owner.phase('native',sha(self.producer)): self.run_worker(owner,invoke)
        self.assertEqual(owner.receipt['ledger']['workers'],[])

    def test_failed_invocation_preserves_original_if_both_receipts_missing(self):
        error=KeyboardInterrupt('invocation failed')
        def invoke(*args):raise error
        with self.assertRaises(KeyboardInterrupt) as caught:
            with self.owner() as owner:
                with owner.phase('native',sha(self.producer)): self.run_worker(owner,invoke)
        self.assertIs(caught.exception,error);self.assertEqual(len(error.__notes__),2)
        self.assertEqual(owner.receipt['ledger']['outcome'],'Failed')

    def test_publication_path_must_be_fresh_disjoint_and_external(self):
        for choose in (lambda:self.receipt,lambda:self.study/'bad.json',lambda:self.root/'owner'/'bad.json'):
            path=choose()
            owner=self.owner()
            with self.subTest(path=path),self.assertRaises(ValueError):
                with owner:
                    with owner.phase('native',sha(self.producer)):
                        owner.run_worker(self.study,self.producer,w.__file__,self.receipt,
                            lambda *_:self.fail('Action invoked'),publication_path=path)
            # Each iteration must use a fresh managed owner directory.
            self.root=self.root/'next';self.prepare(self.root)

    def test_existing_observation_prevents_invocation(self):
        self.publication.write_bytes(b'previous')
        with self.assertRaisesRegex(ValueError,'Existing publication'):
            with self.owner() as owner:
                with owner.phase('native',sha(self.producer)):
                    self.run_worker(owner,lambda *_:self.fail('Action invoked'))
        self.assertEqual(self.publication.read_bytes(),b'previous');self.assertFalse(self.receipt.exists())

    def test_real_python_workers_retain_publication_after_close_in_all_phases(self):
        export=os.environ.get('LL_RECEIPT_PUBLICATION_EXPORT')
        if export:self.prepare(Path(export))
        script=self.root/'literal-worker.py'
        script.write_text('import sys\nfrom pathlib import Path\nsys.path.insert(0,'+repr(str(ANALYSIS))+')\n'
            'from proposal_work_accounting import worker_receipt\n'
            'with worker_receipt(sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4],__file__) as work:\n'
            '    assert work.read_json(Path(sys.argv[1])/"request.json")["literal"]\n',encoding='utf-8')
        processes=[]
        with self.owner() as owner:
            for phase in w.PHASES:
                with owner.phase(phase,sha(script)):
                    receipt=self.root/(phase+'-work.json');publication=self.root/(phase+'-publication.json')
                    def invoke(binding,pin):
                        result=owned.run([sys.executable,'-B',str(script),str(self.study),phase,str(binding),pin],self.root,
                            self.root/(phase+'.log'),time.monotonic()+8,
                            observe=lambda value:owner.observe_process(value,sha(owned.__file__)))
                        self.assertEqual(result['exitCode'],0,(self.root/(phase+'.log')).read_text());processes.append(result)
                    owner.run_worker(self.study,script,w.__file__,receipt,invoke,publication_path=publication)
                    observed=w.strict(publication.read_bytes())
                    self.assertEqual(observed['counters']['acceptedWriteBytes'],receipt.stat().st_size)
                    self.assertEqual(observed['serializedReceiptSha256'],sha(receipt))
                    self.assertEqual(observed['counters']['closeCompleted'],1)
                    owner.callback(receipt.unlink);owner.callback(publication.unlink)
        manifest=w.strict((owner.storage.root/'files.json').read_bytes())
        for name,pin in manifest.items():self.assertEqual(sha(owner.storage.root/name),pin)
        self.assertEqual(owner.receipt['ledger']['outcome'],'Complete')
        self.assertEqual(len([n for n in manifest if n.endswith('-receipt-publication.json')]),4)
        self.assertEqual(owner.final_observation['outcome'],'Complete')
        for entry in owner.receipt['ledger']['workers']:self.assertNotIn('acceptedWriteBytes',entry['receipt']['counters'])
        if export:
            w.seal_new(self.root/'final-owner-observation.json',owner.final_observation)
            w.seal_new(self.root/'fixture.json',dict(fixtureOnly=True,pythonWorkersOnly=True,actualCombat=0,
                productionEntropyDraws=0,wholeProcessCoverage=False,usableForAdmission=False,processes=processes,
                sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p) for p in (w.__file__,owned.__file__,__file__)}))
            w.seal_new(self.root/'files.json',{p.relative_to(self.root).as_posix():sha(p) for p in sorted(self.root.rglob('*')) if p.is_file()})


if __name__=='__main__':unittest.main()
