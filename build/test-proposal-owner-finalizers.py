"""Owner operation/copy/log failure tails; literal files/jobs, no science."""
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
import bounded_windows_process as owned


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


class OwnerFinalizers(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name);self.counter=work.OwnerFileCounters();Hostile.calls.clear()
        self.source=self.root/'source.json';self.source.write_bytes(b'{"literal":true}')
        self.target=self.root/'target.json';self.observations=[]

    def counts(self,failures):
        original=self.counter.add
        def add(name,*args):
            if name in failures:raise failures[name]
            return original(name,*args)
        return patch.object(self.counter,'add',add)

    def observing(self,error=None,*,at=2):
        original=self.counter.observe_file
        def observe(path,*args,**kwargs):
            self.observations.append(dict(path=str(path),missing_ok=kwargs.get('missing_ok',True)))
            if error is not None and len(self.observations)==at:raise error
            return original(path,*args,**kwargs)
        return patch.object(self.counter,'observe_file',observe)

    def primary(self,error,action):
        try:action()
        except BaseException as caught:self.assertIs(caught,error)
        else:self.fail('Expected original failure')
        self.assertEqual(Hostile.calls,[])

    def receipt(self,expected='Failed'):
        value=self.counter.receipt('publication','a'*64,'b'*64,True)
        self.assertEqual(value['outcome'],expected)
        self.assertFalse(value['wholeProcessCoverage']);self.assertFalse(value['usableForAdmission'])
        raw=work.canonical(value)
        self.assertEqual(work.counter_receipt(raw,hashlib.sha256(raw).hexdigest(),'publication','a'*64,'b'*64),value)
        return value

    def copy(self):return self.counter.copyfile(self.source,self.target)

    def log(self,error=None,*,created=True):
        with self.counter.child_log(self.target) as log:
            if created:
                self.target.write_bytes(b'literal log');log.opened()
            if error is not None:raise error

    def test_action_error_survives_failed_counter(self):
        error=Hostile('action')
        with self.counts({'actionFailed':Hostile('counter')}):
            self.primary(error,lambda:self.counter.operation('action',lambda:fail(error)))
        self.receipt();self.assertNotIn('actionFailed',self.counter.values)
        self.assertEqual(attributes(error)['__notes__'],['Owner failure accounting failed; secondary details omitted.'])

    def test_operation_attempt_counter_failure_does_not_run_action(self):
        error=Hostile('attempt')
        with self.counts({'actionAttempted':error}),patch.object(work.shutil,'copyfile') as action:
            self.primary(error,lambda:self.counter.operation('action',action))
        action.assert_not_called();self.receipt()

    def test_operation_completion_counter_failure_marks_receipt_failed(self):
        error=Hostile('completion');events=[]
        with self.counts({'actionCompleted':error}):
            self.primary(error,lambda:self.counter.operation('action',lambda:events.append('ran')))
        self.assertEqual(events,['ran']);self.receipt()

    def test_false_and_none_action_results_remain_successful(self):
        for value in (False,None):self.assertIs(self.counter.operation('action',lambda:value),value)
        self.assertEqual(self.counter.values['actionCompleted'],2);self.receipt('Complete')

    def test_caught_nested_failure_cannot_be_cleared_by_outer_success(self):
        error=Hostile('nested')
        def outer():
            self.primary(error,lambda:self.counter.operation('inner',lambda:fail(error)))
            return 'returned'
        self.assertEqual(self.counter.operation('outer',outer),'returned');self.receipt()
        self.assertEqual(self.counter.values['outerCompleted'],1)

    def test_copy_error_survives_both_failed_counters_and_observation(self):
        error=Hostile('copy')
        def partial(*args):self.target.write_bytes(b'partial');raise error
        with patch.object(work.shutil,'copyfile',side_effect=partial),self.observing(Hostile('observation')),self.counts({
                'failedFileCopyBytesUnknown':Hostile('unknown'),'fileCopyOperationsFailed':Hostile('failed')}):
            self.primary(error,self.copy)
        self.receipt();self.assertEqual(self.target.read_bytes(),b'partial')
        self.assertEqual(len(self.observations),2);self.assertTrue(self.observations[-1]['missing_ok'])
        self.assertEqual(len(attributes(error)['__notes__']),3)
        self.assertNotIn('fileCopyOperationsCompleted',self.counter.values)

    def test_failed_copy_still_samples_partial_file_after_unknown_counter_error(self):
        error=Hostile('copy')
        def partial(*args):self.target.write_bytes(b'partial');raise error
        with patch.object(work.shutil,'copyfile',side_effect=partial),self.counts({'failedFileCopyBytesUnknown':Hostile('unknown')}):
            self.primary(error,self.copy)
        self.receipt();self.assertEqual(self.counter.values['sampledTrackedFileBytes'],7)
        self.assertEqual(self.counter.values['fileCopyOperationsFailed'],1)

    def test_copy_final_observation_error_prevents_completion(self):
        error=Hostile('observation')
        with self.observing(error):self.primary(error,self.copy)
        self.receipt();self.assertEqual(self.target.read_bytes(),self.source.read_bytes())
        self.assertFalse(self.observations[-1]['missing_ok'])
        self.assertNotIn('fileCopyOperationsCompleted',self.counter.values)
        self.assertNotIn('failedFileCopyBytesUnknown',self.counter.values)
        self.assertEqual(self.counter.values['fileCopyOperationsFailed'],1)

    def test_copy_logical_byte_counter_error_preserves_actual_copy_and_failed_receipt(self):
        error=Hostile('logical bytes')
        with self.observing(),self.counts({'fileCopyLogicalBytes.json':error}):self.primary(error,self.copy)
        self.receipt();self.assertEqual(len(self.observations),2)
        self.assertEqual(self.target.read_bytes(),self.source.read_bytes())
        self.assertNotIn('failedFileCopyBytesUnknown',self.counter.values)

    def test_copy_initial_observation_failure_prevents_copy(self):
        error=Hostile('initial')
        with self.observing(error,at=1),patch.object(work.shutil,'copyfile') as action:self.primary(error,self.copy)
        action.assert_not_called();self.assertFalse(self.target.exists());self.receipt()

    def test_copy_attempt_counter_failure_prevents_copy_and_observation(self):
        error=Hostile('attempt')
        with self.observing(),self.counts({'fileCopyOperationsAttempted':error}),patch.object(work.shutil,'copyfile') as action:
            self.primary(error,self.copy)
        action.assert_not_called();self.assertEqual(self.observations,[]);self.receipt()

    def test_copy_completion_counter_failure_follows_final_observation(self):
        error=Hostile('completion')
        with self.observing(),self.counts({'fileCopyOperationsCompleted':error}):self.primary(error,self.copy)
        self.assertEqual(len(self.observations),2);self.receipt()
        self.assertEqual(self.counter.values['fileCopyLogicalBytes.json'],self.target.stat().st_size)

    def test_copy_suppressed_metadata_failure_retains_unknown_sample_semantics(self):
        with patch.object(Path,'stat',side_effect=PermissionError('metadata')),patch.object(work.shutil,'copyfile',return_value='literal result'):
            self.assertEqual(self.copy(),'literal result')
        self.assertEqual(self.counter.values['fileLengthObservationFailures'],2);self.receipt('Complete')
        self.assertNotIn('fileCopyLogicalBytes.json',self.counter.values)

    def test_copy_success_preserves_overwrite_return_and_bytes(self):
        self.target.write_bytes(b'previous longer output')
        self.assertEqual(self.copy(),self.target)
        self.assertEqual(self.target.read_bytes(),self.source.read_bytes());self.receipt('Complete')

    def test_log_body_error_survives_observation_and_failed_counter(self):
        error=Hostile('body')
        with self.observing(Hostile('observation'),at=1),self.counts({'childLogScopesFailed':Hostile('counter')}):
            self.primary(error,lambda:self.log(error))
        self.receipt();self.assertEqual(len(self.observations),1)
        self.assertEqual(len(attributes(error)['__notes__']),2)
        self.assertFalse(self.observations[0]['missing_ok']);self.assertNotIn('childLogScopesFailed',self.counter.values)

    def test_log_created_counter_error_still_observes_created_file(self):
        error=Hostile('created')
        with self.observing(),self.counts({'childLogFilesCreated':error}):self.primary(error,self.log)
        self.receipt();self.assertEqual(len(self.observations),1)
        self.assertEqual(self.counter.values['childLogFinalObservedBytes.json'],11)

    def test_log_attempt_counter_failure_never_yields_or_observes(self):
        error=Hostile('attempt')
        with self.observing(),self.counts({'childLogScopesAttempted':error}):self.primary(error,self.log)
        self.assertEqual(self.observations,[]);self.assertFalse(self.target.exists());self.receipt()

    def test_uncreated_log_does_not_observe_preexisting_file_on_failure(self):
        self.target.write_bytes(b'previous');error=Hostile('setup')
        with self.observing():self.primary(error,lambda:self.log(error,created=False))
        self.receipt();self.assertEqual(self.observations,[]);self.assertEqual(self.target.read_bytes(),b'previous')

    def test_uncreated_log_success_keeps_previous_completion_semantics(self):
        with self.observing():self.log(created=False)
        self.assertEqual(self.observations,[]);self.receipt('Complete')

    def test_log_final_observation_error_prevents_completion(self):
        error=Hostile('observation')
        with self.observing(error,at=1):self.primary(error,self.log)
        self.receipt();self.assertNotIn('childLogScopesCompleted',self.counter.values)
        self.assertEqual(self.counter.values['childLogScopesFailed'],1)

    def test_log_body_error_survives_final_byte_counter(self):
        error=Hostile('body')
        with self.observing(),self.counts({'childLogFinalObservedBytes.json':Hostile('bytes')}):
            self.primary(error,lambda:self.log(error))
        self.receipt();self.assertEqual(len(self.observations),1)
        self.assertEqual(self.counter.values['sampledTrackedFileBytes'],11)

    def test_log_role_failure_does_not_skip_final_file_observation(self):
        error=Hostile('role')
        with self.observing(),patch.object(work,'role',side_effect=error):self.primary(error,self.log)
        self.receipt();self.assertEqual(len(self.observations),1)

    def test_log_completion_counter_failure_follows_final_observation(self):
        error=Hostile('completion')
        with self.observing(),self.counts({'childLogScopesCompleted':error}):self.primary(error,self.log)
        self.receipt();self.assertEqual(self.counter.values['childLogFinalObservedBytes.json'],11)

    def test_log_suppressed_metadata_failure_is_unknown_sample(self):
        with patch.object(Path,'stat',side_effect=PermissionError('metadata')):self.log()
        self.assertEqual(self.counter.values['fileLengthObservationFailures'],1);self.receipt('Complete')

    def test_later_copy_log_json_and_operation_success_cannot_clear_failed_state(self):
        error=Hostile('first')
        self.primary(error,lambda:self.counter.operation('first',lambda:fail(error)))
        self.copy();self.log();self.counter.write_json(self.root/'later.json',{})
        self.assertEqual(self.counter.operation('later',lambda:42),42);self.receipt()

    def test_cancellation_is_preserved_and_caller_exception_notes_are_untouched(self):
        error=KeyboardInterrupt('cancelled');caller=RuntimeError('caller')
        try:raise caller
        except RuntimeError:
            with self.observing(Hostile('observation'),at=1):self.primary(error,lambda:self.log(error))
        self.receipt();self.assertNotIn('__notes__',attributes(caller))

    def test_generator_exit_still_observes_created_log_and_marks_failed(self):
        scope=self.counter.child_log(self.target);log=scope.__enter__()
        self.target.write_bytes(b'log');log.opened()
        with self.observing():scope.gen.close()
        self.receipt();self.assertEqual(len(self.observations),1)
        self.assertEqual(self.counter.values['childLogScopesFailed'],1)

    def test_bounded_notes_and_nonstandard_collections_avoid_hooks(self):
        class Notes(list):
            def __len__(self):raise AssertionError('length')
            def append(self,value):raise AssertionError('append')
        for prior in (['caller']*7,['caller']*8,Notes(),None,object()):
            error=Hostile('body');attributes(error)['__notes__']=prior
            with self.observing(Hostile('x'*1000000),at=len(self.observations)+1),self.counts({'childLogScopesFailed':Hostile('count')}):
                self.primary(error,lambda:self.log(error))
            self.assertIs(attributes(error)['__notes__'],prior)
            if type(prior) is list:self.assertEqual(len(prior),8)
        self.receipt()

    def test_annotation_allocation_failure_preserves_body_error(self):
        error=Hostile('body')
        with self.observing(Hostile('observation'),at=1),patch.object(work,'object',side_effect=MemoryError,create=True):
            self.primary(error,lambda:self.log(error))
        self.receipt();self.assertNotIn('__notes__',attributes(error))

    def test_owner_receipt_keeps_base_argument_validation(self):
        for value in (1,None,'yes'):
            with self.assertRaises(ValueError):self.counter.receipt('publication','a'*64,'b'*64,value)
        self.receipt('Complete')

    def test_json_failure_still_prevents_owner_receipt_completion(self):
        error=Hostile('serialize')
        with patch.object(work.json,'dump',side_effect=error):
            self.primary(error,lambda:self.counter.write_json(self.target,{}))
        self.counter.operation('later',lambda:None);self.receipt()

    def test_real_observer_error_survives_log_finalization_after_job_cleanup(self):
        export=os.environ.get('LL_OWNER_FINALIZERS_EXPORT');root=Path(export) if export else self.root/'fixture'
        root.mkdir(parents=True);target=root/'console.log';observations=[];events=[];error=Hostile('literal observer failure')
        original_observe=self.counter.observe_file;original_add=self.counter.add
        def observer(value):
            observations.append(value);events.append('observer');raise error
        def observe_file(path,*args,**kwargs):
            events.append('fileObservation');original_observe(path,*args,**kwargs)
            raise Hostile('literal final observation failure')
        def add(name,*args):
            if name=='childLogScopesFailed':
                events.append('failureCounter');raise Hostile('literal failed counter')
            return original_add(name,*args)
        with patch.object(self.counter,'observe_file',observe_file),patch.object(self.counter,'add',add):
            self.primary(error,lambda:owned.run([sys.executable,'-B','-c',"print('literal owner finalizer')"],
                ROOT,target,time.monotonic()+10,observe=observer,file_work=self.counter,log_byte_limit=4096))
        receipt=self.receipt();self.assertEqual(events,['observer','fileObservation','failureCounter'])
        self.assertEqual(len(observations),1);process=observations[0]
        self.assertTrue(process['jobDrained']);self.assertTrue(process['cleanup']['allAcquiredHandlesClosed'])
        self.assertIsNone(process['ownerError']);self.assertEqual(process['exitCode'],0)
        self.assertNotIn('childLogScopesFailed',receipt['counters']);self.assertNotIn('childLogScopesCompleted',receipt['counters'])
        self.assertEqual(target.read_text().strip(),'literal owner finalizer')
        if export:
            work.seal_new(root/'process-observation.json',process);work.seal_new(root/'receipt.json',receipt)
            work.seal_new(root/'fixture.json',dict(fixtureOnly=True,primaryErrorPreserved=True,events=events,
                formattingHookCalls=Hostile.calls,notes=attributes(error)['__notes__'],
                accountingModuleSha256=sha(work.__file__),processHelperSha256=sha(owned.__file__),fixtureDriverSha256=sha(__file__),
                actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
                wholeProcessCoverage=False,usableForAdmission=False))
            work.seal_new(root/'files.json',{p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})


if __name__=='__main__':unittest.main()
