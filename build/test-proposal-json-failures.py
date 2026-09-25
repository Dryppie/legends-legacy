"""Accounted JSON failure cleanup; literal files/jobs, no scientific study."""
import hashlib
import importlib.util
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


class FaultFile:
    def __init__(self,stream,error=None,closing=None,operation='write'):
        self.stream,self.error,self.closing,self.operation=stream,error,closing,operation
        self.close_calls=0
    def write(self,text):
        if self.error is not None and self.operation=='write':
            self.stream.write(text[:1]);raise self.error
        return self.stream.write(text)
    def flush(self):
        if self.error is not None and self.operation=='flush':raise self.error
        return self.stream.flush()
    def fileno(self):return self.stream.fileno()
    def close(self):
        self.close_calls+=1;self.stream.close()
        if self.closing is not None:raise self.closing


class JSONFailures(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.base=Path(temporary.name);self.use('output');Hostile.calls.clear()

    def use(self,kind):
        self.kind=kind;self.path=self.base/(kind+'.json');self.stream=None
        self.counter=work.Counters() if kind=='output' else work.OwnerFileCounters()
        self.operation='jsonOutputOperations' if kind=='output' else 'jsonWriteOperations'

    def write(self,value=None):
        action=self.counter.write_json_output if self.kind=='output' else self.counter.write_json
        return action(self.path,{'literal':'value'} if value is None else value)

    def opening(self,error=None,closing=None,operation='write'):
        original=Path.open
        def opening(path,*args,**kwargs):
            stream=original(path,*args,**kwargs)
            if path==self.path and args and args[0]=='x':
                self.stream=FaultFile(stream,error,closing,operation);return self.stream
            return stream
        return patch.object(Path,'open',opening)

    def counts(self,failures):
        original=self.counter.add
        def add(name,*args):
            if name in failures:raise failures[name]
            return original(name,*args)
        return patch.object(self.counter,'add',add)

    def final_observation(self,error):
        original=self.counter.observe_file;self.observations=0
        def observe(*args,**kwargs):
            self.observations+=1
            if self.observations==2:raise error
            return original(*args,**kwargs)
        return patch.object(self.counter,'observe_file',observe)

    def primary(self,error,action):
        try:action()
        except BaseException as caught:self.assertIs(caught,error)
        else:self.fail('Expected original failure')
        self.assertEqual(Hostile.calls,[])

    def failed(self):
        receipt=self.counter.receipt('publication','a'*64,'b'*64,True)
        self.assertEqual(receipt['outcome'],'Failed');self.assertFalse(receipt['wholeProcessCoverage'])
        raw=work.canonical(receipt)
        self.assertEqual(work.counter_receipt(raw,hashlib.sha256(raw).hexdigest(),'publication','a'*64,'b'*64),receipt)
        if self.stream is not None:
            self.assertEqual(self.stream.close_calls,1);self.assertTrue(self.stream.stream.closed)
        return receipt

    def test_both_writers_preserve_write_failure_when_close_also_fails(self):
        for kind in ('output','owner'):
            with self.subTest(kind=kind):
                self.use(kind);error=Hostile('write failed')
                with self.opening(error,Hostile('close failed')):self.primary(error,self.write)
                self.failed();self.assertEqual(self.path.read_bytes(),b'{')
                self.assertEqual(attributes(error)['__notes__'],['JSON output close failed; secondary details omitted.'])

    def test_close_attempt_accounting_failure_cannot_skip_close(self):
        error=Hostile('close attempt counter')
        with self.opening(),self.counts({'textOutputClosesAttempted':error}):self.primary(error,self.write)
        self.failed();self.assertEqual(self.counter.values['textOutputClosesCompleted'],1)

    def test_body_failure_precedes_both_close_accounting_and_close_errors(self):
        error=Hostile('write failed')
        with self.opening(error,Hostile('close failed')),self.counts({
                'textOutputClosesAttempted':Hostile('attempt counter'),
                'textOutputClosesFailed':Hostile('outcome counter')}):self.primary(error,self.write)
        self.failed();self.assertEqual(len(attributes(error)['__notes__']),3)

    def test_close_failure_precedes_failed_close_counter_error(self):
        error=Hostile('close failed')
        with self.opening(closing=error),self.counts({'textOutputClosesFailed':Hostile('outcome counter')}):
            self.primary(error,self.write)
        self.failed();self.assertEqual(len(attributes(error)['__notes__']),1)

    def test_close_completion_counter_error_marks_operation_failed(self):
        error=Hostile('close completion counter')
        with self.opening(),self.counts({'textOutputClosesCompleted':error}):self.primary(error,self.write)
        self.failed();self.assertEqual(self.counter.values['jsonOutputOperationsFailed'],1)

    def test_write_failure_counters_are_independent_and_best_effort(self):
        for kind in ('output','owner'):
            with self.subTest(kind=kind):
                self.use(kind);error=Hostile('write failed')
                name='textOutputWriteCallsFailed' if kind=='output' else 'textWriteCallsFailed'
                unknown='failedTextOutputWriteBytesUnknown' if kind=='output' else 'failedTextWriteBytesUnknown'
                with self.opening(error),self.counts({name:Hostile('failure counter')}):self.primary(error,self.write)
                self.failed();self.assertEqual(self.counter.values[unknown],1)

    def test_unknown_progress_counter_failure_preserves_write_error(self):
        error=Hostile('write failed')
        with self.opening(error),self.counts({'failedTextOutputWriteBytesUnknown':Hostile('unknown counter')}):
            self.primary(error,self.write)
        self.failed();self.assertEqual(self.counter.values['textOutputWriteCallsFailed'],1)

    def test_open_error_survives_failed_open_counter_without_closing(self):
        error=Hostile('open failed')
        with patch.object(Path,'open',side_effect=error),self.counts({'textOutputOpensFailed':Hostile('open counter')}):
            self.primary(error,self.write)
        self.failed();self.assertNotIn('textOutputClosesAttempted',self.counter.values)

    def test_open_completion_counter_error_still_closes_acquired_stream(self):
        error=Hostile('open completion counter')
        with self.opening(),self.counts({'textOutputOpensCompleted':error}):self.primary(error,self.write)
        self.failed();self.assertEqual(self.path.read_bytes(),b'')

    def test_overall_failure_counter_error_cannot_allow_complete_receipt(self):
        for kind in ('output','owner'):
            with self.subTest(kind=kind):
                self.use(kind);error=Hostile('write failed')
                with self.opening(error),self.counts({self.operation+'Failed':Hostile('overall counter')}):
                    self.primary(error,self.write)
                self.failed();self.assertNotIn(self.operation+'Failed',self.counter.values)

    def test_overall_completion_counter_error_cannot_allow_complete_receipt(self):
        for kind in ('output','owner'):
            with self.subTest(kind=kind):
                self.use(kind);error=Hostile('completion counter')
                with self.opening(),self.counts({self.operation+'Completed':error}):self.primary(error,self.write)
                self.failed()

    def test_attempt_counter_error_marks_failed_without_opening(self):
        error=Hostile('attempt counter')
        with self.counts({'jsonOutputOperationsAttempted':error}),patch.object(Path,'open') as opening:
            self.primary(error,self.write)
        opening.assert_not_called();self.failed()

    def test_success_after_failed_write_cannot_clear_failure_state(self):
        error=Hostile('write failed')
        with self.opening(error),self.counts({'jsonOutputOperationsFailed':Hostile('counter failed')}):
            self.primary(error,self.write)
        self.path=self.base/'later.json';self.stream=None;self.write()
        self.failed();self.assertEqual(json.loads(self.path.read_bytes()),{'literal':'value'})

    def test_clean_success_restores_complete_receipt_and_preserves_bytes(self):
        for kind in ('output','owner'):
            with self.subTest(kind=kind):
                self.use(kind);value={'text':'æ\n漢字','values':[None,True,1.25]}
                with self.opening():self.write(value)
                self.assertEqual(json.loads(self.path.read_bytes()),value)
                self.assertEqual(self.counter.receipt('publication','a'*64,'b'*64,True)['outcome'],'Complete')
                self.assertEqual(self.stream.close_calls,1)

    def test_owner_body_failure_survives_close_and_final_observation_errors(self):
        self.use('owner');error=Hostile('write failed')
        with self.opening(error,Hostile('close failed')),self.final_observation(Hostile('observe failed')):
            self.primary(error,self.write)
        self.failed();self.assertEqual(self.observations,2);self.assertEqual(len(attributes(error)['__notes__']),2)

    def test_owner_close_failure_survives_final_observation_error(self):
        self.use('owner');error=Hostile('close failed')
        with self.opening(closing=error),self.final_observation(Hostile('observe failed')):self.primary(error,self.write)
        self.failed();self.assertEqual(self.observations,2)

    def test_owner_observation_failure_prevents_operation_completion(self):
        self.use('owner');error=Hostile('observe failed')
        with self.opening(),self.final_observation(error):self.primary(error,self.write)
        self.failed();self.assertEqual(self.counter.values['jsonWriteOperationsFailed'],1)
        self.assertNotIn('jsonWriteOperationsCompleted',self.counter.values)

    def test_owner_flush_failure_survives_close_and_still_observes_length(self):
        self.use('owner');error=Hostile('flush failed')
        with self.opening(error,Hostile('close failed'),'flush'),patch.object(work.os,'fsync') as sync:
            self.primary(error,self.write)
        sync.assert_not_called();self.failed()
        self.assertEqual(self.counter.values['sampledTrackedFileBytes'],self.path.stat().st_size)

    def test_owner_sync_failure_survives_close(self):
        self.use('owner');error=Hostile('sync failed')
        with self.opening(closing=Hostile('close failed')),patch.object(work.os,'fsync',side_effect=error):
            self.primary(error,self.write)
        self.failed();self.assertNotIn('fileSyncsCompleted',self.counter.values)

    def test_serialization_error_remains_primary_for_both_writers(self):
        for kind in ('output','owner'):
            with self.subTest(kind=kind):
                self.use(kind);error=Hostile('serialize failed')
                with self.opening(closing=Hostile('close failed')),patch.object(work.json,'dump',side_effect=error):
                    self.primary(error,self.write)
                self.failed()

    def test_cancellation_and_caller_exception_isolation(self):
        error=KeyboardInterrupt('cancelled');caller=RuntimeError('unrelated')
        try:raise caller
        except RuntimeError:
            with self.opening(error,Hostile('close failed')):self.primary(error,self.write)
        self.failed();self.assertNotIn('__notes__',attributes(caller))

    def test_full_and_nonstandard_note_collections_remain_unchanged(self):
        class Notes(list):
            def __len__(self):raise AssertionError('Unexpected length')
            def append(self,value):raise AssertionError('Unexpected append')
        for index,prior in enumerate((['caller']*8,Notes(),None,object())):
            self.use('output');self.path=self.base/(str(index)+'.json');error=Hostile('write failed')
            attributes(error)['__notes__']=prior
            with self.opening(error,Hostile('close failed')):self.primary(error,self.write)
            self.failed();self.assertIs(attributes(error)['__notes__'],prior)

    def test_note_count_ceiling_and_large_secondary_arguments(self):
        error=Hostile('write failed');attributes(error)['__notes__']=['caller']*7
        with self.opening(error,Hostile('x'*1000000)),self.counts({'textOutputClosesAttempted':Hostile(object())}):
            self.primary(error,self.write)
        self.failed();notes=attributes(error)['__notes__']
        self.assertEqual(len(notes),8);self.assertLess(len(notes[-1]),80)

    def test_annotation_allocation_failure_does_not_skip_close(self):
        error=Hostile('write failed')
        with self.opening(error,Hostile('close failed')),patch.object(work,'object',side_effect=MemoryError,create=True):
            self.primary(error,self.write)
        self.failed();self.assertNotIn('__notes__',attributes(error))

    def test_worker_scope_rejects_caught_failure_even_when_failed_counter_is_missing(self):
        spec=importlib.util.spec_from_file_location('json_failure_worker_fixture',ROOT/'build/test-proposal-worker-io.py')
        fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
        case=fixture.WorkerIO();case.setUp();self.addCleanup(case.doCleanups)
        with self.assertRaisesRegex(ValueError,'caught a failed JSON output'):
            with case.scope() as counter:
                original=counter.add
                def add(name,*args):
                    if name=='jsonOutputOperationsFailed':raise Hostile('failed counter')
                    return original(name,*args)
                with patch.object(counter,'add',add),patch.object(work.json,'dump',side_effect=Hostile('serialization')):
                    try:counter.write_json_output(case.output,{})
                    except Hostile:pass
        value=case.retained();self.assertEqual(value['outcome'],'Failed')
        self.assertNotIn('jsonOutputOperationsFailed',value['counters']);self.assertEqual(Hostile.calls,[])

    def test_literal_worker_retains_failure_receipt_after_all_cleanup_attempts(self):
        import bounded_windows_process as owned
        export=os.environ.get('LL_JSON_FAILURES_EXPORT');root=Path(export) if export else self.base/'fixture'
        root.mkdir(parents=True);observations=[]
        result=owned.run([sys.executable,'-B','-X','utf8',str(Path(__file__).resolve()),'--literal-json-fixture',str(root)],
                         ROOT,root/'console.log',time.monotonic()+10,observe=observations.append,log_byte_limit=4096)
        self.assertEqual(result['exitCode'],0,(root/'console.log').read_text())
        fixture=work.strict((root/'fixture.json').read_bytes());receipt=work.strict((root/'receipt.json').read_bytes())
        self.assertTrue(fixture['primaryErrorPreserved']);self.assertTrue(fixture['streamClosed'])
        self.assertEqual(fixture['closeAttempts'],1);self.assertEqual(fixture['formattingHookCalls'],[])
        self.assertEqual(receipt['outcome'],'Failed');self.assertNotIn('jsonOutputOperationsFailed',receipt['counters'])
        self.assertEqual((root/'partial.json').read_bytes(),b'{')
        self.assertTrue(observations[0]['jobDrained']);self.assertTrue(observations[0]['cleanup']['allAcquiredHandlesClosed'])
        if export:
            work.seal_new(root/'process-observation.json',observations[0])
            work.seal_new(root/'files.json',{p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})


def literal_fixture(root):
    import bounded_windows_process as owned
    root=Path(root);target=root/'partial.json';counter=work.Counters();primary=Hostile('literal write failure')
    original_open=Path.open;original_add=counter.add;streams=[]
    def opening(path,*args,**kwargs):
        stream=original_open(path,*args,**kwargs)
        if path==target:
            wrapped=FaultFile(stream,primary,Hostile('literal close failure'));streams.append(wrapped);return wrapped
        return stream
    def add(name,*args):
        if name in ('textOutputClosesAttempted','textOutputWriteCallsFailed','jsonOutputOperationsFailed'):
            raise Hostile('literal accounting failure')
        return original_add(name,*args)
    with patch.object(Path,'open',opening),patch.object(counter,'add',add):
        try:counter.write_json_output(target,{'literal':'value'})
        except BaseException as error:assert error is primary
        else:raise AssertionError('Expected write failure')
    assert len(streams)==1 and streams[0].stream.closed and streams[0].close_calls==1 and Hostile.calls==[]
    receipt=counter.receipt('publication','a'*64,'b'*64,True)
    assert receipt['outcome']=='Failed' and 'jsonOutputOperationsFailed' not in receipt['counters']
    work.seal_new(root/'receipt.json',receipt)
    work.seal_new(root/'fixture.json',dict(fixtureOnly=True,primaryErrorPreserved=True,streamClosed=True,closeAttempts=1,
        closeFailureInjectedAfterRealClose=True,formattingHookCalls=Hostile.calls,notes=attributes(primary)['__notes__'],
        accountingModuleSha256=sha(work.__file__),processHelperSha256=sha(owned.__file__),fixtureDriverSha256=sha(__file__),
        actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
        wholeProcessCoverage=False,usableForAdmission=False))
    print('literal JSON failure verified',flush=True)


if __name__=='__main__':
    if len(sys.argv)==3 and sys.argv[1]=='--literal-json-fixture':literal_fixture(sys.argv[2])
    else:unittest.main()
