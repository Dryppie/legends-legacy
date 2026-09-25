"""Receipt publication failure tails; literal files/jobs, no scientific work."""
from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as work


class Hostile(RuntimeError):
    calls=[]
    def __str__(self):self.calls.append('str');raise AssertionError('Unexpected str')
    def __repr__(self):self.calls.append('repr');raise AssertionError('Unexpected repr')
    def __bool__(self):self.calls.append('bool');raise AssertionError('Unexpected truthiness')
    def add_note(self,note):self.calls.append('add_note');raise AssertionError('Unexpected note hook')
    def __getattribute__(self,name):
        if name in ('args','__dict__','__notes__'):
            type(self).calls.append(name);raise AssertionError('Unexpected attribute hook')
        return super().__getattribute__(name)


def attributes(error):return BaseException.__dict__['__dict__'].__get__(error)
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def fail(error):raise error
def limits():return dict(receipt=100000,publication=100000,persistence=100000,total=300000)


class FaultCounts(Counter):
    def __init__(self,values,failures):super().__init__(values);self.failures=failures
    def __setitem__(self,key,value):
        if key in getattr(self,'failures',{}):raise self.failures[key]
        super().__setitem__(key,value)


class FaultWrite:
    def __init__(self,stream,writing=None,closing=None):
        self.stream,self.writing,self.closing=stream,writing,closing;self.close_calls=0
    def write(self,raw):
        if self.writing is not None:self.stream.write(raw[:1]);raise self.writing
        return self.stream.write(raw)
    def flush(self):return self.stream.flush()
    def fileno(self):return self.stream.fileno()
    def close(self):
        self.close_calls+=1;self.stream.close()
        if self.closing is not None:raise self.closing


def binding(root,version):
    study=root/'study';study.mkdir();request=study/'request.json';request.write_bytes(b'{"literal":true}')
    value=dict(version=version,phase='independentAudit',studyRoot=str(study),requestSha256=sha(request),
        producerSha256=sha(__file__),accountingModuleSha256=sha(work.__file__),receiptPath=str(root/'worker.json'))
    if version!=work.WORKER_BINDING_VERSION:value['publicationPath']=str(root/'publication.json')
    if version in (work.WORKER_PERSISTENCE_BINDING_VERSION,work.WORKER_SIDECAR_BINDING_VERSION):value['publicationPersistencePath']=str(root/'persistence.json')
    if version==work.WORKER_SIDECAR_BINDING_VERSION:value['sidecarByteLimits']=limits()
    work.seal_new(root/'binding.json',value)
    return value


class PublicationFinalizers(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name);self.target=self.root/'receipt.json';self.streams=[];Hostile.calls.clear()
        self.publisher=work.ReceiptPublication('a'*64,'publication','b'*64,'c'*64)

    def opening(self,writing=None,closing=None):
        original=Path.open
        def opened(path,*args,**kwargs):
            stream=original(path,*args,**kwargs)
            if path==self.target and args and args[0]=='xb':
                value=FaultWrite(stream,writing,closing);self.streams.append(value);return value
            return stream
        return patch.object(Path,'open',opened)

    def primary(self,error,action):
        try:action()
        except BaseException as caught:self.assertIs(caught,error)
        else:self.fail('Expected original failure')
        self.assertEqual(Hostile.calls,[])

    def publish(self):
        self.publisher.open(self.target);self.publisher.publish(lambda:{'literal':'value'})

    def checked(self,valid=True):
        value=self.publisher.observation();self.assertEqual(value['outcome'],'Failed')
        self.assertTrue(self.publisher.failed);self.assertFalse(value['wholeProcessCoverage']);self.assertFalse(value['usableForAdmission'])
        for stream in self.streams:self.assertEqual(stream.close_calls,1);self.assertTrue(stream.stream.closed)
        if valid:work.worker_publication(work.canonical(value),'a'*64,'publication','b'*64,'c'*64)
        else:
            with self.assertRaises(ValueError):work.worker_publication(work.canonical(value),'a'*64,'publication','b'*64,'c'*64)
        return value

    def counts(self,**failures):self.publisher.counts=FaultCounts(self.publisher.counts,failures)

    def test_hostile_write_and_close_errors_keep_first_error_and_fixed_records(self):
        error=Hostile('write')
        with self.opening(error,Hostile('close')):self.primary(error,self.publish)
        value=self.checked();self.assertEqual([e['operation'] for e in value['errors']],['write','close'])
        self.assertTrue(all(e['type']==work.PUBLICATION_ERROR_TYPE and e['message']==work.PUBLICATION_ERROR_MESSAGE for e in value['errors']))
        self.assertEqual(self.target.read_bytes(),b'{');self.assertEqual(attributes(error)['__notes__'],['Work receipt close failed; secondary details omitted.'])

    def test_close_attempt_counter_failure_cannot_skip_close(self):
        error=Hostile('close attempt');self.counts(closeAttempted=error)
        with self.opening():self.primary(error,self.publish)
        self.checked(False);self.assertEqual(self.publisher.counts['closeCompleted'],1)

    def test_write_error_survives_close_counters_and_close_error(self):
        error=Hostile('write');self.counts(closeAttempted=Hostile('attempt'),closeFailed=Hostile('failed'))
        with self.opening(error,Hostile('close')):self.primary(error,self.publish)
        self.checked(False);self.assertEqual(len(attributes(error)['__notes__']),3)

    def test_close_completion_counter_failure_keeps_failed_state_without_errors(self):
        error=Hostile('close completed');self.counts(closeCompleted=error)
        with self.opening():self.primary(error,self.publish)
        self.assertEqual(self.checked(False)['errors'],[])

    def test_open_completion_counter_failure_closes_acquired_stream(self):
        error=Hostile('open completed');self.counts(openCompleted=error)
        with self.opening(closing=Hostile('close')):self.primary(error,lambda:self.publisher.open(self.target))
        self.checked(False);self.assertEqual(self.target.read_bytes(),b'')

    def test_open_failure_survives_failed_counter_and_still_records_error(self):
        error=Hostile('open');self.counts(openFailed=Hostile('counter'))
        with patch.object(Path,'open',side_effect=error):self.primary(error,lambda:self.publisher.open(self.target))
        value=self.checked(False);self.assertEqual([e['operation'] for e in value['errors']],['open'])
        self.assertNotIn('closeAttempted',value['counters'])

    def test_open_attempt_counter_failure_never_acquires_file(self):
        error=Hostile('attempt');self.counts(openAttempted=error)
        with patch.object(Path,'open') as opened:self.primary(error,lambda:self.publisher.open(self.target))
        opened.assert_not_called();self.checked(False)

    def test_serialization_attempt_or_completion_counter_failure_still_closes(self):
        for name in ('serializeAttempted','serializeCompleted'):
            self.target=self.root/(name+'.json');self.publisher=work.ReceiptPublication('a'*64,'publication','b'*64,'c'*64)
            error=Hostile(name);self.counts(**{name:error})
            with self.opening():self.primary(error,self.publish)
            self.checked(False)

    def test_failed_error_record_append_preserves_write_error(self):
        class Errors(list):
            def append(self,value):raise Hostile('append')
        self.publisher.errors=Errors();error=Hostile('write')
        with self.opening(error):self.primary(error,self.publish)
        self.checked(False);self.assertEqual(self.publisher.counts['writeFailed'],1)

    def test_failed_write_and_unknown_progress_counters_are_independent(self):
        error=Hostile('write');self.counts(writeFailed=Hostile('failed'),failedWriteBytesUnknown=Hostile('unknown'))
        with self.opening(error):self.primary(error,self.publish)
        value=self.checked(False);self.assertEqual([e['operation'] for e in value['errors']],['write'])
        self.assertEqual(len(attributes(error)['__notes__']),2)

    def test_accepted_byte_counter_failure_cannot_complete_observation(self):
        error=Hostile('bytes');self.counts(acceptedWriteBytes=error)
        with self.opening():self.primary(error,self.publish)
        self.checked(False);self.assertEqual(work.strict(self.target.read_bytes()),{'literal':'value'})

    def test_write_completion_counter_failure_still_closes_and_marks_failed(self):
        error=Hostile('completed');self.counts(writeCompleted=error)
        with self.opening():self.primary(error,self.publish)
        self.checked(False)

    def test_flush_and_sync_errors_survive_close(self):
        for operation in ('flush','sync'):
            self.target=self.root/(operation+'.json');self.publisher=work.ReceiptPublication('a'*64,'publication','b'*64,'c'*64)
            error=Hostile(operation)
            with self.opening(closing=Hostile('close')):
                self.publisher.open(self.target)
                target=self.publisher.stream if operation=='flush' else work.os
                with patch.object(target,'flush' if operation=='flush' else 'fsync',side_effect=error):
                    self.primary(error,lambda:self.publisher.publish(lambda:{}))
            self.checked()

    def test_serialization_factory_error_survives_failed_accounting_and_close(self):
        error=Hostile('serialize');self.counts(serializeFailed=Hostile('count'))
        with self.opening(closing=Hostile('close')):
            self.publisher.open(self.target);self.primary(error,lambda:self.publisher.publish(lambda:fail(error)))
        self.checked(False)

    def test_failed_writer_cannot_retry_or_close_again(self):
        error=Hostile('write')
        with self.opening(error):self.primary(error,self.publish)
        before=self.target.read_bytes()
        with self.assertRaisesRegex(ValueError,'unavailable'):self.publisher.publish(lambda:{'retry':True})
        with self.assertRaisesRegex(ValueError,'cannot be reused'):self.publisher.open(self.root/'again.json')
        self.assertEqual(self.target.read_bytes(),before);self.assertFalse((self.root/'again.json').exists());self.checked()

    def test_completed_writer_reuse_is_rejected_and_not_relabelled_complete(self):
        with self.opening():self.publish()
        before=self.target.read_bytes()
        with self.assertRaisesRegex(ValueError,'unavailable'):self.publisher.publish(lambda:{})
        self.checked(False);self.assertEqual(self.target.read_bytes(),before)

    def test_duplicate_open_closes_existing_acquisition_without_creating_another_file(self):
        with self.opening():
            self.publisher.open(self.target)
            with self.assertRaisesRegex(ValueError,'cannot be reused'):self.publisher.open(self.root/'again.json')
        self.checked(False);self.assertFalse((self.root/'again.json').exists())

    def test_success_retains_canonical_bytes_and_valid_operation_counts(self):
        with self.opening():self.publish()
        value=self.publisher.observation();self.assertEqual(value['outcome'],'Complete')
        self.assertEqual(self.target.read_bytes(),work.canonical({'literal':'value'}));self.assertEqual(self.streams[0].close_calls,1)
        work.worker_publication(work.canonical(value),'a'*64,'publication','b'*64,'c'*64)

    def test_unobserved_writer_preserves_write_error_through_close(self):
        error=Hostile('write')
        with self.opening(error,Hostile('close')):
            stream=self.target.open('xb')
            self.primary(error,lambda:work._publish_unobserved(stream,lambda:{},'invalid write'))
        self.assertEqual(self.streams[0].close_calls,1);self.assertTrue(self.streams[0].stream.closed)
        self.assertEqual(attributes(error)['__notes__'],['Unobserved receipt close failed; secondary details omitted.'])

    def test_unobserved_writer_retries_short_returns_without_changing_bytes(self):
        with self.target.open('xb') as raw:
            stream=FaultWrite(raw);original=stream.write
            stream.write=lambda value:original(value[:2])
            work._publish_unobserved(stream,lambda:{'literal':'æ'},'invalid write')
        self.assertEqual(self.target.read_bytes(),work.canonical({'literal':'æ'}));self.assertEqual(stream.close_calls,1)

    def scope_failure(self,version,member):
        root=self.root/(version+'-'+member);root.mkdir();binding(root,version);self.target=root/member
        error=Hostile('worker')
        def action():
            with work.worker_receipt(root/'study','independentAudit',root/'binding.json',sha(root/'binding.json'),__file__):raise error
        with self.opening(Hostile('write'),Hostile('close')):self.primary(error,action)
        self.assertEqual(self.streams[-1].close_calls,1);self.assertTrue(self.streams[-1].stream.closed)
        self.assertIn('secondary details omitted.',attributes(error)['__notes__'][-1])

    def test_v1_worker_error_survives_legacy_write_and_close(self):self.scope_failure(work.WORKER_BINDING_VERSION,'worker.json')
    def test_v2_worker_error_survives_publication_observation_write_and_close(self):self.scope_failure(work.WORKER_PUBLICATION_BINDING_VERSION,'publication.json')
    def test_v3_worker_error_survives_publication_write_and_close(self):self.scope_failure(work.WORKER_PERSISTENCE_BINDING_VERSION,'publication.json')
    def test_v3_worker_error_survives_terminal_write_and_close(self):self.scope_failure(work.WORKER_PERSISTENCE_BINDING_VERSION,'persistence.json')
    def test_v4_worker_error_survives_sidecar_failure_without_repeated_close(self):self.scope_failure(work.WORKER_SIDECAR_BINDING_VERSION,'worker.json')

    def test_persistence_setup_failure_occurs_before_file_reservation(self):
        error=Hostile('constructor')
        def action():
            with work.publication_persistence_scope(self.target,'a'*64,'publication','b'*64,'c'*64):pass
        with patch.object(work,'PublicationObservationPersistence',side_effect=error):self.primary(error,action)
        self.assertFalse(self.target.exists())

    def test_publication_setup_failure_occurs_before_legacy_file_reservation(self):
        error=Hostile('constructor')
        def action():
            with work.receipt_publication_scope(self.target,'a'*64,'publication','b'*64,'c'*64):pass
        with patch.object(work,'ReceiptPublication',side_effect=error):self.primary(error,action)
        self.assertFalse(self.target.exists())

    def test_sidecar_constructor_failure_closes_acquired_stream(self):
        error=Hostile('constructor');sidecars=work.SidecarWrites(limits())
        with self.opening(closing=Hostile('close')),patch.object(work,'_SidecarWriter',side_effect=error):
            self.primary(error,lambda:sidecars.open('receipt',self.target))
        self.assertEqual(self.streams[0].close_calls,1);self.assertTrue(sidecars.failed)

    def test_sidecar_registration_failure_after_insert_does_not_repeat_close(self):
        error=Hostile('register');sidecars=work.SidecarWrites(limits())
        class Files(dict):
            def __setitem__(self,key,value):super().__setitem__(key,value);raise error
        sidecars.files=Files()
        def action():
            with sidecars:sidecars.open('receipt',self.target)
        with self.opening(closing=Hostile('close')):self.primary(error,action)
        self.assertEqual(self.streams[0].close_calls,1);self.assertTrue(sidecars.failed)

    def test_all_sidecar_closes_run_after_first_close_failure(self):
        sidecars=work.SidecarWrites(limits());error=Hostile('first');events=[]
        class Writer:
            def __init__(self,name):self.name=name
            def close(self):events.append(self.name);raise error if self.name=='receipt' else Hostile('later')
        sidecars.files={name:Writer(name) for name in ('receipt','publication','persistence')}
        self.primary(error,lambda:sidecars.__exit__(None,None,None))
        self.assertEqual(events,['receipt','publication','persistence']);self.assertEqual(len(attributes(error)['__notes__']),2)

    def test_sidecar_context_preserves_body_error_and_marks_failed(self):
        sidecars=work.SidecarWrites(limits());error=Hostile('body')
        def action():
            with sidecars.open('receipt',self.target):raise error
        with self.opening(closing=Hostile('close')):self.primary(error,action)
        self.assertTrue(sidecars.failed);self.assertTrue(sidecars.files['receipt'].closed);self.assertEqual(self.streams[0].close_calls,1)

    def test_bounded_notes_and_large_error_arguments_do_not_invoke_hooks(self):
        class Notes(list):
            def __len__(self):raise AssertionError('length')
            def append(self,value):raise AssertionError('append')
        for index,prior in enumerate((['caller']*7,['caller']*8,Notes(),None,object())):
            self.target=self.root/(str(index)+'.json');self.publisher=work.ReceiptPublication('a'*64,'publication','b'*64,'c'*64)
            error=Hostile('x'*1000000);attributes(error)['__notes__']=prior
            with self.opening(error,Hostile(object())):self.primary(error,self.publish)
            value=self.checked();self.assertIs(attributes(error)['__notes__'],prior)
            self.assertLess(len(work.canonical(value)),2000)
            if type(prior) is list:self.assertEqual(len(prior),8)

    def test_annotation_allocation_failure_does_not_replace_original_or_skip_close(self):
        error=Hostile('write')
        with self.opening(error,Hostile('close')),patch.object(work,'object',side_effect=MemoryError,create=True):self.primary(error,self.publish)
        self.checked();self.assertNotIn('__notes__',attributes(error))

    def test_error_record_does_not_inspect_exception_class_name(self):
        class Meta(type):
            def __getattribute__(cls,name):
                if name=='__name__':raise AssertionError('Unexpected class-name hook')
                return super().__getattribute__(name)
        class Error(Hostile,metaclass=Meta):pass
        error=Error('write')
        with self.opening(error):self.primary(error,self.publish)
        self.assertEqual(self.checked()['errors'][0]['type'],work.PUBLICATION_ERROR_TYPE)

    def test_cancellation_and_caller_exception_isolation(self):
        error=KeyboardInterrupt('cancel');caller=RuntimeError('caller')
        try:raise caller
        except RuntimeError:
            with self.opening(closing=Hostile('close')):
                self.publisher.open(self.target);self.primary(error,lambda:self.publisher.publish(lambda:fail(error)))
        self.checked();self.assertNotIn('__notes__',attributes(caller))

    def test_literal_worker_retains_failed_publication_and_complete_persistence(self):
        import bounded_windows_process as owned
        export=os.environ.get('LL_PUBLICATION_FINALIZERS_EXPORT');root=Path(export) if export else self.root/'fixture'
        root.mkdir(parents=True);observations=[]
        result=owned.run([sys.executable,'-B','-X','utf8',str(Path(__file__).resolve()),'--literal-publication-fixture',str(root)],
            ROOT,root/'console.log',time.monotonic()+10,observe=observations.append,log_byte_limit=4096)
        self.assertEqual(result['exitCode'],0,(root/'console.log').read_text())
        fixture=work.strict((root/'fixture.json').read_bytes());self.assertTrue(fixture['primaryErrorPreserved'])
        self.assertEqual(fixture['closeAttempts'],1);self.assertEqual(fixture['formattingHookCalls'],[])
        self.assertTrue(observations[0]['jobDrained']);self.assertTrue(observations[0]['cleanup']['allAcquiredHandlesClosed'])
        if export:
            work.seal_new(root/'process-observation.json',observations[0])
            work.seal_new(root/'files.json',{p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})


def literal_fixture(root):
    import bounded_windows_process as owned
    root=Path(root);value=binding(root,work.WORKER_PERSISTENCE_BINDING_VERSION);primary=Hostile('literal worker failure')
    original_open=Path.open;streams=[]
    def opened(path,*args,**kwargs):
        stream=original_open(path,*args,**kwargs)
        if path==root/'worker.json' and args and args[0]=='xb':
            stream=FaultWrite(stream,Hostile('literal receipt write failure'),Hostile('literal receipt close failure'));streams.append(stream)
        return stream
    with patch.object(Path,'open',opened):
        try:
            with work.worker_receipt(root/'study','independentAudit',root/'binding.json',sha(root/'binding.json'),__file__):raise primary
        except BaseException as error:assert error is primary
        else:raise AssertionError('Expected original worker failure')
    args=(sha(root/'binding.json'),'independentAudit',value['requestSha256'],value['producerSha256'])
    publication=work.worker_publication((root/'publication.json').read_bytes(),*args)
    persistence=work.worker_publication_persistence((root/'persistence.json').read_bytes(),*args)
    assert publication['outcome']=='Failed' and persistence['outcome']=='Complete' and (root/'worker.json').read_bytes()==b'{'
    assert [e['operation'] for e in publication['errors']]==['write','close']
    assert len(streams)==1 and streams[0].stream.closed and streams[0].close_calls==1 and Hostile.calls==[]
    work.seal_new(root/'fixture.json',dict(fixtureOnly=True,primaryErrorPreserved=True,streamClosed=True,closeAttempts=1,
        formattingHookCalls=Hostile.calls,notes=attributes(primary)['__notes__'],
        accountingModuleSha256=sha(work.__file__),processHelperSha256=sha(owned.__file__),fixtureDriverSha256=sha(__file__),
        actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
        wholeProcessCoverage=False,usableForAdmission=False))
    print('literal publication failure verified',flush=True)


if __name__=='__main__':
    if len(sys.argv)==3 and sys.argv[1]=='--literal-publication-fixture':literal_fixture(sys.argv[2])
    else:unittest.main()
