"""Counted-read ownership/failure tests; literal files/jobs, no scientific work."""
import gc
import hashlib
import importlib.util
import io
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


class FaultRead:
    def __init__(self,stream,reading=None,closing=None):
        self.stream,self.reading,self.closing=stream,reading,closing
        self.close_calls=self.read_calls=self.consumed=0
    def readable(self):return True
    def seekable(self):return self.stream.seekable()
    def tell(self):return self.stream.tell()
    def seek(self,*args):return self.stream.seek(*args)
    def read(self,size=-1):
        self.read_calls+=1
        if self.reading is not None:
            self.consumed+=len(self.stream.read(1));raise self.reading
        return self.stream.read(size)
    def readinto(self,buffer):
        self.read_calls+=1
        if self.reading is not None:
            self.consumed+=len(self.stream.read(1));raise self.reading
        return self.stream.readinto(buffer)
    def close(self):
        self.close_calls+=1;self.stream.close()
        if self.closing is not None:raise self.closing


class ReadFinalizers(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name);self.path=self.root/'input.json';self.path.write_bytes(b'{"literal":true}')
        self.counter=work.Counters();Hostile.calls.clear();self.streams=[]

    def opening(self,reading=None,closing=None):
        original=Path.open
        def opening(path,*args,**kwargs):
            stream=original(path,*args,**kwargs)
            if path==self.path and args and args[0]=='rb':
                value=FaultRead(stream,reading,closing);self.streams.append(value);return value
            return stream
        return patch.object(Path,'open',opening)

    def counts(self,failures):
        original=self.counter.add
        def add(name,*args):
            if name in failures:raise failures[name]
            return original(name,*args)
        return patch.object(self.counter,'add',add)

    def primary(self,error,action):
        try:action()
        except BaseException as caught:self.assertIs(caught,error)
        else:self.fail('Expected original failure')
        self.assertEqual(Hostile.calls,[])

    def receipt(self,expected='Failed'):
        value=self.counter.receipt('publication','a'*64,'b'*64,True)
        self.assertEqual(value['outcome'],expected);self.assertFalse(value['wholeProcessCoverage'])
        raw=work.canonical(value)
        self.assertEqual(work.counter_receipt(raw,hashlib.sha256(raw).hexdigest(),'publication','a'*64,'b'*64),value)
        for stream in self.streams:
            self.assertEqual(stream.close_calls,1);self.assertTrue(stream.stream.closed)
        return value

    def test_read_error_survives_close_and_partial_progress_stays_unknown(self):
        error=Hostile('read')
        with self.opening(error,Hostile('close')):self.primary(error,lambda:self.counter.read_bytes(self.path))
        self.receipt();self.assertEqual(self.streams[0].consumed,1)
        self.assertNotIn('applicationReadBytes.json',self.counter.values)
        self.assertEqual(attributes(error)['__notes__'],['Read stream close failed; secondary details omitted.'])

    def test_hash_readinto_error_survives_close(self):
        error=Hostile('hash read')
        with self.opening(error,Hostile('close')):self.primary(error,lambda:self.counter.sha(self.path))
        self.receipt();self.assertEqual(self.streams[0].read_calls,1)

    def test_parser_error_survives_owned_stream_close(self):
        error=Hostile('parse')
        with self.opening(closing=Hostile('close')),patch.object(work,'strict_json',side_effect=error):
            self.primary(error,lambda:self.counter.read_json(self.path))
        self.receipt();self.assertNotIn('jsonParseCompleted',self.counter.values)

    def test_close_only_failure_marks_receipt_failed(self):
        error=Hostile('close')
        with self.opening(closing=error):self.primary(error,lambda:self.counter.read_bytes(self.path))
        self.receipt();self.assertEqual(self.counter.values['applicationReadBytes.json'],self.path.stat().st_size)

    def test_failed_close_is_not_retried_by_explicit_close_or_collection(self):
        error=Hostile('close');raw=FaultRead(io.BytesIO(b'{}'),closing=error)
        wrapped=work.CountedRead(raw,lambda _:None,work=self.counter)
        self.primary(error,wrapped.close);self.assertTrue(wrapped.closed)
        wrapped.close();del wrapped;gc.collect()
        self.assertEqual(raw.close_calls,1);self.assertTrue(raw.stream.closed);self.receipt()

    def test_base_wrapper_close_still_runs_when_underlying_close_fails(self):
        error=Hostile('native close');events=[]
        class Wrapper(work.CountedRead):
            def flush(self):events.append('base flush');raise Hostile('wrapper close')
        raw=FaultRead(io.BytesIO(b'{}'),closing=error);wrapped=Wrapper(raw,lambda _:None,work=self.counter)
        self.primary(error,wrapped.close)
        self.assertEqual(events,['base flush']);self.assertTrue(wrapped.closed)
        wrapped.close();self.assertEqual(raw.close_calls,1);self.receipt()
        self.assertEqual(attributes(error)['__notes__'],['Read wrapper close failed; secondary details omitted.'])

    def test_close_does_not_need_failure_tracking_context_allocation(self):
        error=Hostile('native close');raw=FaultRead(io.BytesIO(b'{}'),closing=error)
        wrapped=work.CountedRead(raw,lambda _:None,work=self.counter)
        with patch.object(wrapped,'_failure_scope',side_effect=MemoryError):self.primary(error,wrapped.close)
        self.assertEqual(raw.close_calls,1);self.assertTrue(raw.stream.closed);self.assertTrue(wrapped.closed);self.receipt()

    def test_base_close_only_error_preserves_borrowed_stream(self):
        error=Hostile('base close')
        class Wrapper(work.CountedRead):
            def flush(self):raise error
        raw=io.BytesIO(b'{}');self.addCleanup(raw.close)
        wrapped=Wrapper(raw,lambda _:None,True,work=self.counter)
        self.primary(error,wrapped.close);self.assertTrue(wrapped.closed);self.assertFalse(raw.closed);self.receipt()

    def test_borrowed_wrapper_close_never_closes_underlying_stream(self):
        raw=FaultRead(io.BytesIO(b'{}'));self.addCleanup(raw.stream.close)
        with work.CountedRead(raw,lambda _:None,True,work=self.counter) as wrapped:self.assertEqual(wrapped.read(),b'{}')
        self.assertTrue(wrapped.closed);self.assertEqual(raw.close_calls,0);self.assertFalse(raw.stream.closed)
        self.receipt('Complete')

    def test_closed_wrapper_rejects_reads_and_seeks_without_touching_borrowed_stream(self):
        raw=io.BytesIO(b'{}');self.addCleanup(raw.close)
        wrapped=work.CountedRead(raw,lambda _:None,True,work=self.counter);wrapped.close()
        for action in (wrapped.read,lambda:wrapped.readinto(bytearray(1)),lambda:wrapped.seek(0),wrapped.tell,wrapped.seekable):
            with self.assertRaisesRegex(ValueError,'wrapper is closed'):action()
        self.assertEqual(raw.tell(),0);self.assertFalse(raw.closed);self.receipt()

    def test_read_failure_caught_inside_scope_still_marks_receipt_failed(self):
        error=Hostile('read')
        with self.opening(error):
            with self.counter.open_read(self.path) as wrapped:self.primary(error,wrapped.read)
        self.receipt()

    def test_byte_accounting_error_survives_close_error(self):
        error=Hostile('count')
        with self.opening(closing=Hostile('close')),self.counts({'applicationReadBytes.json':error}):
            self.primary(error,lambda:self.counter.read_bytes(self.path))
        self.receipt()

    def test_acquisition_failure_marks_failed_without_close(self):
        error=Hostile('open')
        with patch.object(Path,'open',side_effect=error):self.primary(error,lambda:self.counter.open_read(self.path))
        self.receipt();self.assertEqual(self.streams,[])

    def test_wrapper_construction_failure_closes_acquired_file_and_keeps_first_error(self):
        error=Hostile('construct')
        with self.opening(closing=Hostile('close')),patch.object(work,'CountedRead',side_effect=error):
            self.primary(error,lambda:self.counter.open_read(self.path))
        self.receipt()

    def test_seek_tell_and_seekable_failures_mark_failed(self):
        for method in ('seek','tell','seekable'):
            error=Hostile(method);raw=io.BytesIO(b'{}')
            wrapped=work.CountedRead(raw,lambda _:None,work=self.counter)
            with patch.object(raw,method,side_effect=error):
                self.primary(error,lambda:getattr(wrapped,method)(0) if method=='seek' else getattr(wrapped,method)())
            wrapped.close()
        self.receipt()

    def test_parse_failure_closes_only_its_wrapper(self):
        error=Hostile('parse');raw=io.BytesIO(b'{}');self.addCleanup(raw.close);seen=[]
        def loader(stream):seen.append(stream);raise error
        self.primary(error,lambda:self.counter.parse(raw,loader))
        self.assertTrue(seen[0].closed);self.assertFalse(raw.closed);self.receipt()

    def test_parser_caught_read_error_cannot_complete_receipt(self):
        error=Hostile('read');raw=FaultRead(io.BytesIO(b'{}'),reading=error);self.addCleanup(raw.stream.close)
        def loader(stream):self.primary(error,stream.read);return {'returned':True}
        self.assertEqual(self.counter.parse(raw,loader),{'returned':True})
        self.assertEqual(self.counter.values['jsonParseCompleted'],1);self.assertEqual(raw.close_calls,0);self.receipt()

    def test_parse_attempt_and_completion_counter_failures_leave_borrowed_stream_open(self):
        for name in ('jsonParseAttempts','jsonParseCompleted'):
            raw=io.BytesIO(b'{}');error=Hostile(name)
            with self.counts({name:error}):self.primary(error,lambda:self.counter.parse(raw))
            self.assertFalse(raw.closed);raw.close()
        self.receipt()

    def test_decode_failure_and_completion_counter_failure_keep_failed_state(self):
        raw=io.BytesIO(b'{}');self.addCleanup(raw.close);error=Hostile('complete')
        stream=self.counter.decode_stream(raw)
        with self.counts({'decodePassesCompleted':error}):self.primary(error,stream.read)
        stream.close();self.assertFalse(raw.closed);self.receipt()

    def test_decode_start_failure_leaves_borrowed_stream_open(self):
        raw=io.BytesIO(b'{}');self.addCleanup(raw.close);error=Hostile('start')
        with self.counts({'decodePassesStarted':error}):self.primary(error,lambda:self.counter.decode_stream(raw))
        self.assertFalse(raw.closed);self.receipt()

    def test_decode_read_failure_cannot_be_cleared_by_later_success(self):
        error=Hostile('decode');raw=FaultRead(io.BytesIO(b'{}'),reading=error);self.addCleanup(raw.stream.close)
        stream=self.counter.decode_stream(raw);self.primary(error,stream.read);stream.close()
        self.assertEqual(self.counter.read_json(self.path),{'literal':True});self.receipt()

    def test_successful_read_hash_parse_and_decode_keep_counts_and_borrowed_ownership(self):
        raw=self.path.read_bytes()
        self.assertEqual(self.counter.read_bytes(self.path),raw)
        self.assertEqual(self.counter.sha(self.path),hashlib.sha256(raw).hexdigest())
        self.assertEqual(self.counter.read_json(self.path),json.loads(raw))
        source=io.BytesIO(raw)
        with self.counter.decode_stream(source) as decoded:self.assertEqual(decoded.read(),raw)
        self.assertFalse(source.closed);source.close();self.receipt('Complete')
        self.assertEqual(self.counter.values['applicationReadBytes.json'],len(raw)*3)
        self.assertEqual(self.counter.values['jsonInputBytes'],len(raw))
        self.assertEqual(self.counter.values['decodedBytesProcessed'],len(raw))

    def test_text_read_close_only_error_closes_raw_once_and_prevents_parse(self):
        self.counter=work.OwnerFileCounters();error=Hostile('close')
        with self.opening(closing=error):self.primary(error,lambda:self.counter.read_json_text(self.path,encoding='utf-8'))
        self.receipt();self.assertNotIn('jsonParseAttempts',self.counter.values)

    def test_text_read_error_survives_text_close_raw_close_and_failed_counters(self):
        self.counter=work.OwnerFileCounters();error=Hostile('text read');events=[]
        class Text:
            def __init__(self,*args,**kwargs):pass
            def read(self):raise error
            def close(self):events.append('text close');raise Hostile('text close')
        with self.opening(closing=Hostile('raw close')),patch.object(work.io,'TextIOWrapper',Text),self.counts({
                'textReadOperationsFailed':Hostile('failed counter'),'failedTextReadBytesUnknown':Hostile('unknown counter')}):
            self.primary(error,lambda:self.counter.read_json_text(self.path))
        self.receipt();self.assertEqual(events,['text close']);self.assertEqual(len(attributes(error)['__notes__']),4)

    def test_text_wrapper_constructor_error_survives_raw_close(self):
        self.counter=work.OwnerFileCounters();error=Hostile('text constructor')
        with self.opening(closing=Hostile('raw close')),patch.object(work.io,'TextIOWrapper',side_effect=error):
            self.primary(error,lambda:self.counter.read_json_text(self.path))
        self.receipt();self.assertEqual(self.counter.values['textReadOperationsFailed'],1)

    def test_text_decode_error_survives_close_without_starting_parse(self):
        self.counter=work.OwnerFileCounters();self.path.write_bytes(b'\xff')
        with self.opening(closing=Hostile('close')):
            with self.assertRaises(UnicodeDecodeError):self.counter.read_json_text(self.path,encoding='utf-8')
        self.receipt();self.assertNotIn('jsonParseAttempts',self.counter.values)

    def test_text_parse_error_survives_failed_parse_counter(self):
        self.counter=work.OwnerFileCounters();error=Hostile('parser')
        with self.opening(),patch.object(work.json,'loads',side_effect=error),self.counts({'jsonParseFailed':Hostile('counter')}):
            self.primary(error,lambda:self.counter.read_json_text(self.path))
        self.receipt();self.assertEqual(self.counter.values['textReadOperationsCompleted'],1)

    def test_text_failed_read_counters_are_independent(self):
        self.counter=work.OwnerFileCounters();error=Hostile('read')
        with self.opening(error),self.counts({'textReadOperationsFailed':Hostile('counter')}):
            self.primary(error,lambda:self.counter.read_json_text(self.path))
        self.receipt();self.assertEqual(self.counter.values['failedTextReadBytesUnknown'],1)

    def test_text_attempt_counter_failure_prevents_open(self):
        self.counter=work.OwnerFileCounters();error=Hostile('attempt')
        with self.counts({'textReadOperationsAttempted':error}),patch.object(Path,'open') as opened:
            self.primary(error,lambda:self.counter.read_json_text(self.path))
        opened.assert_not_called();self.receipt()

    def test_owner_receipt_retains_read_failure_after_later_operation_and_json_success(self):
        self.counter=work.OwnerFileCounters();error=Hostile('read')
        with self.opening(error):self.primary(error,lambda:self.counter.read_json_text(self.path))
        self.counter.operation('later',lambda:None);self.counter.write_json(self.root/'later.json',{})
        self.receipt()

    def test_cancellation_preserves_first_error_and_unrelated_caller_notes(self):
        error=KeyboardInterrupt('cancel');caller=RuntimeError('caller')
        try:raise caller
        except RuntimeError:
            with self.opening(error,Hostile('close')):self.primary(error,lambda:self.counter.read_bytes(self.path))
        self.receipt();self.assertNotIn('__notes__',attributes(caller))

    def test_notes_stay_bounded_and_nonstandard_collections_are_untouched(self):
        class Notes(list):
            def __len__(self):raise AssertionError('length')
            def append(self,value):raise AssertionError('append')
        for prior in (['caller']*7,['caller']*8,Notes(),None,object()):
            error=Hostile('read');attributes(error)['__notes__']=prior
            with self.opening(error,Hostile('x'*1000000)):self.primary(error,lambda:self.counter.read_bytes(self.path))
            self.assertIs(attributes(error)['__notes__'],prior)
            if type(prior) is list:self.assertEqual(len(prior),8)
        self.receipt()

    def test_annotation_allocation_failure_does_not_replace_read_error(self):
        error=Hostile('read')
        with self.opening(error,Hostile('close')),patch.object(work,'object',side_effect=MemoryError,create=True):
            self.primary(error,lambda:self.counter.read_bytes(self.path))
        self.receipt();self.assertNotIn('__notes__',attributes(error))

    def test_caught_worker_read_failure_is_rejected_before_successful_authentication(self):
        spec=importlib.util.spec_from_file_location('read_worker_fixture',ROOT/'build/test-proposal-worker-io.py')
        fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
        case=fixture.WorkerIO();case.setUp();self.addCleanup(case.doCleanups)
        with self.assertRaisesRegex(ValueError,'caught a failed read operation'):
            with case.scope() as counter:
                try:counter.read_bytes(case.root/'missing')
                except FileNotFoundError:pass
        self.assertEqual(case.retained()['outcome'],'Failed')

    def test_receipt_still_rejects_invalid_success_argument_after_read_failure(self):
        error=Hostile('open')
        with patch.object(Path,'open',side_effect=error):self.primary(error,lambda:self.counter.open_read(self.path))
        with self.assertRaises(ValueError):self.counter.receipt('publication','a'*64,'b'*64,1)

    def test_standalone_wrapper_preserves_error_without_a_counter_collector(self):
        error=Hostile('body');raw=FaultRead(io.BytesIO(b'{}'),closing=Hostile('close'))
        def action():
            with work.CountedRead(raw,lambda _:None):raise error
        self.primary(error,action);self.assertEqual(raw.close_calls,1)

    def test_literal_worker_exports_read_failure_and_one_close_attempt(self):
        import bounded_windows_process as owned
        export=os.environ.get('LL_READ_FINALIZERS_EXPORT');root=Path(export) if export else self.root/'fixture'
        root.mkdir(parents=True);observations=[]
        result=owned.run([sys.executable,'-B','-X','utf8',str(Path(__file__).resolve()),'--literal-read-fixture',str(root)],
                         ROOT,root/'console.log',time.monotonic()+10,observe=observations.append,log_byte_limit=4096)
        self.assertEqual(result['exitCode'],0,(root/'console.log').read_text())
        fixture=work.strict((root/'fixture.json').read_bytes());receipt=work.strict((root/'receipt.json').read_bytes())
        self.assertTrue(fixture['primaryErrorPreserved']);self.assertTrue(fixture['wrapperClosed']);self.assertTrue(fixture['underlyingClosed'])
        self.assertEqual(fixture['closeAttempts'],1);self.assertEqual(fixture['formattingHookCalls'],[])
        self.assertEqual(receipt['outcome'],'Failed');self.assertNotIn('applicationReadBytes.json',receipt['counters'])
        self.assertTrue(observations[0]['jobDrained']);self.assertTrue(observations[0]['cleanup']['allAcquiredHandlesClosed'])
        if export:
            work.seal_new(root/'process-observation.json',observations[0])
            work.seal_new(root/'files.json',{p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})


def literal_fixture(root):
    import bounded_windows_process as owned
    root=Path(root);target=root/'input.json';target.write_bytes(b'{"literal":true}')
    counter=work.Counters();primary=Hostile('literal read failure');original_open=Path.open;streams=[]
    def opening(path,*args,**kwargs):
        stream=original_open(path,*args,**kwargs)
        if path==target:
            value=FaultRead(stream,primary,Hostile('literal close failure'));streams.append(value);return value
        return stream
    with patch.object(Path,'open',opening):
        try:
            with counter.open_read(target) as wrapped:wrapped.read()
        except BaseException as error:assert error is primary
        else:raise AssertionError('Expected read failure')
    wrapped.close()
    assert wrapped.closed and len(streams)==1 and streams[0].stream.closed and streams[0].close_calls==1 and Hostile.calls==[]
    counter.write_json_output(root/'later.json',{'later':'success'})
    receipt=counter.receipt('publication','a'*64,'b'*64,True)
    assert receipt['outcome']=='Failed' and 'applicationReadBytes.json' not in receipt['counters']
    work.seal_new(root/'receipt.json',receipt)
    work.seal_new(root/'fixture.json',dict(fixtureOnly=True,primaryErrorPreserved=True,wrapperClosed=True,underlyingClosed=True,
        readCalls=streams[0].read_calls,bytesConsumedByFault=streams[0].consumed,closeAttempts=streams[0].close_calls,
        closeFailureInjectedAfterRealClose=True,formattingHookCalls=Hostile.calls,notes=attributes(primary)['__notes__'],
        accountingModuleSha256=sha(work.__file__),processHelperSha256=sha(owned.__file__),fixtureDriverSha256=sha(__file__),
        actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
        wholeProcessCoverage=False,usableForAdmission=False))
    print('literal read failure verified',flush=True)


if __name__=='__main__':
    if len(sys.argv)==3 and sys.argv[1]=='--literal-read-fixture':literal_fixture(sys.argv[2])
    else:unittest.main()
