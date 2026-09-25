"""Bounded launcher failure text that cannot call custom formatting hooks."""
import ctypes as c
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
import bounded_windows_process as owned
import proposal_owner_process as monitor
import proposal_work_accounting as work


class Hostile(RuntimeError):
    calls=[]
    def __str__(self):self.calls.append('str');raise AssertionError('Must not format exception')
    def __repr__(self):self.calls.append('repr');raise AssertionError('Must not represent exception')
    def add_note(self,note):self.calls.append('add_note');raise AssertionError('Must not call note override')
    def __getattribute__(self,name):
        if name in ('args','__dict__','__notes__'):
            type(self).calls.append(name);raise AssertionError('Must not read overridden attributes')
        return super().__getattribute__(name)


def attributes(error):return BaseException.__dict__['__dict__'].__get__(error)


class Diagnostics(unittest.TestCase):
    def setUp(self):Hostile.calls.clear()

    def test_short_single_string_is_unchanged(self):
        self.assertEqual(owned._error_text(RuntimeError('literal failure')),'literal failure')
        self.assertEqual(owned._error_text(RuntimeError('literal failure'),representation=True),"RuntimeError('literal failure')")
        self.assertEqual(owned._error_text(RuntimeError('')),'RuntimeError')
        self.assertEqual(owned._error_text(KeyboardInterrupt()),'KeyboardInterrupt')

    def test_large_message_is_clipped_with_visible_marker(self):
        result=owned._error_text(RuntimeError('\U0001f600'*100000))
        self.assertEqual(len(result),4096);self.assertTrue(result.endswith('...[truncated]'))

    def test_string_is_clipped_before_repr_allocates_escaped_token(self):
        original=repr;seen=[]
        def represent(value):seen.append(value);return original(value)
        with patch.object(owned,'repr',represent,create=True):
            result=owned._error_text(RuntimeError('\0'*100000),representation=True)
        self.assertEqual(len(seen),1);self.assertLessEqual(len(seen[0]),256)
        self.assertIn('[truncated]',result);self.assertLessEqual(len(result),4096)

    def test_argument_count_and_combined_output_are_bounded(self):
        original=repr;seen=[]
        def represent(value):seen.append(value);return original(value)
        with patch.object(owned,'repr',represent,create=True):
            result=owned._error_text(RuntimeError(*(['\U000e0001'*10000]*1000)),representation=True)
        self.assertEqual(len(seen),4);self.assertTrue(all(len(s)<=256 for s in seen));self.assertLessEqual(len(result),4096)

    def test_huge_integer_is_not_converted_to_decimal(self):
        with patch.object(owned,'repr',side_effect=AssertionError('Decimal conversion forbidden'),create=True):
            result=owned._error_text(RuntimeError(1<<1000000))
        self.assertEqual(result,'RuntimeError(<large integer>)')

    def test_exact_small_scalars_remain_readable(self):
        self.assertEqual(owned._error_text(RuntimeError(17,None,False,1.5)),'RuntimeError(17, None, False, 1.5)')

    def test_unsupported_argument_hooks_are_never_called(self):
        class Argument:
            def __str__(self):raise AssertionError('str hook')
            def __repr__(self):raise AssertionError('repr hook')
        self.assertEqual(owned._error_text(RuntimeError(Argument())),'RuntimeError(<unsupported argument>)')

    def test_scalar_subclass_hooks_are_never_called(self):
        class Text(str):
            def __repr__(self):raise AssertionError('string subclass')
        class Integer(int):
            def bit_length(self):raise AssertionError('integer subclass')
        self.assertEqual(owned._error_text(RuntimeError(Text('x'),Integer(3))),
                         'RuntimeError(<unsupported argument>, <unsupported argument>)')

    def test_exception_attribute_and_formatting_overrides_are_bypassed(self):
        error=Hostile('literal hostile error')
        self.assertEqual(owned._error_text(error),'literal hostile error')
        self.assertEqual(owned._error_text(error,representation=True),"Hostile('literal hostile error')")
        owned._error_note(error,'secondary: ',Hostile('other'))
        self.assertEqual(attributes(error)['__notes__'],["secondary: Hostile('other')"])
        self.assertEqual(Hostile.calls,[])

    def test_metaclass_name_hook_is_not_called(self):
        calls=[]
        class Meta(type):
            def __getattribute__(cls,name):calls.append(name);raise AssertionError('metaclass hook')
        class Error(Exception,metaclass=Meta):pass
        error=Error('literal')
        self.assertEqual(owned._error_text(error,representation=True),"BaseException('literal')")
        self.assertEqual(calls,[])

    def test_exception_class_name_is_bounded(self):
        kind=type('X'*100000,(Exception,),{})
        self.assertLessEqual(len(owned._error_text(kind())),128)

    def test_note_count_and_text_are_bounded(self):
        primary=RuntimeError('primary')
        for _ in range(100):owned._error_note(primary,'P'*100000,RuntimeError('\U000e0001'*100000))
        notes=attributes(primary)['__notes__']
        self.assertEqual(len(notes),8);self.assertTrue(all(len(n)<=4096 for n in notes))

    def test_full_existing_notes_are_not_modified(self):
        primary=RuntimeError('primary');notes=['caller'*10000]*9
        attributes(primary)['__notes__']=notes
        owned._error_note(primary,'secondary: ',RuntimeError('ignored'))
        self.assertIs(attributes(primary)['__notes__'],notes);self.assertEqual(len(notes),9)

    def test_nonstandard_notes_are_not_accessed_or_replaced(self):
        class Notes(list):
            def __len__(self):raise AssertionError('len hook')
            def append(self,value):raise AssertionError('append hook')
        for notes in (Notes(),None,42):
            primary=RuntimeError('primary');attributes(primary)['__notes__']=notes
            owned._error_note(primary,'secondary: ',RuntimeError('ignored'))
            self.assertIs(attributes(primary)['__notes__'],notes)

    def test_note_prefix_cannot_invoke_conversion_hooks(self):
        class Prefix:
            def __str__(self):raise AssertionError('prefix hook')
        error=RuntimeError('primary');owned._error_note(error,Prefix(),RuntimeError('secondary'))
        self.assertEqual(attributes(error)['__notes__'],["Owned process error: RuntimeError('secondary')"])

    def test_diagnostic_allocation_failure_uses_literal_fallback(self):
        with patch.object(owned,'_clip_error',side_effect=MemoryError):
            self.assertEqual(owned._error_text(RuntimeError('failure')),'BaseException(<diagnostic unavailable>)')
            error=RuntimeError('primary');owned._error_note(error,'secondary: ',RuntimeError('other'))
        self.assertNotIn('__notes__',attributes(error))


class Cleanup(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name);self.observed=[];self.closed=[];Hostile.calls.clear()

    def run_child(self,code='pass',**options):
        return owned.run([sys.executable,'-B','-c',code],self.root,self.root/'console.log',time.monotonic()+8,
                         observe=self.observed.append,**options)

    def failing_closes(self,error,positions):
        original=owned._close
        def close(handle):
            self.closed.append(handle);owned._check(original(handle))
            if len(self.closed) in positions:raise error
            return 1
        return patch.object(owned,'_close',close)

    def validate(self):
        value=self.observed[0];ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native')
        ledger.observe_process(value,monitor.sha(owned.__file__))
        self.assertEqual(Hostile.calls,[]);return value

    def test_hostile_close_error_does_not_skip_remaining_closes(self):
        error=Hostile('close failed')
        with self.failing_closes(error,{1}),self.assertRaises(Hostile) as caught:self.run_child()
        self.assertIs(caught.exception,error);self.assertEqual(len(self.closed),3)
        value=self.validate();self.assertEqual(value['ownerError'],'close failed')
        self.assertEqual(value['cleanup']['handleCloses'],dict(thread='CloseFailed',process='Closed',job='Closed'))

    def test_hostile_primary_and_secondary_errors_preserve_drain_and_all_closes(self):
        export=os.environ.get('LL_FAILURE_DIAGNOSTICS_EXPORT')
        if export:self.root=Path(export);self.root.mkdir(parents=True)
        primary=Hostile('literal primary failure');secondary=Hostile('literal close failure')
        def check():raise primary
        with self.failing_closes(secondary,{1,2}),self.assertRaises(Hostile) as caught:
            self.run_child('import time;time.sleep(20)',check=check,log_byte_limit=65536)
        self.assertIs(caught.exception,primary);self.assertEqual(len(self.closed),3)
        value=self.validate();self.assertTrue(value['jobDrained']);self.assertEqual(len(value['cleanup']['errors']),2)
        self.assertEqual(len(attributes(primary)['__notes__']),2)
        if export:
            work.seal_new(self.root/'observation.json',value)
            work.seal_new(self.root/'fixture.json',dict(fixtureOnly=True,primaryErrorPreserved=True,
                injectedCloseFailureAfterRealClose=True,formattingHookCalls=Hostile.calls,closeAttempts=len(self.closed),
                notes=attributes(primary)['__notes__'],processHelperSha256=monitor.sha(owned.__file__),
                actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
                wholeProcessCoverage=False,usableForAdmission=False))
            work.seal_new(self.root/'files.json',{p.name:monitor.sha(p) for p in sorted(self.root.iterdir()) if p.is_file()})

    def test_hostile_job_io_error_keeps_unknown_and_closes(self):
        error=Hostile('job I/O failed')
        with patch.object(owned,'_job_io_observation',side_effect=error),self.failing_closes(error,set()), \
             self.assertRaises(Hostile) as caught:self.run_child()
        self.assertIs(caught.exception,error);self.assertEqual(len(self.closed),3)
        self.assertEqual(self.validate()['jobIo']['coverage'],'Unknown')

    def test_hostile_memory_error_keeps_unknown_and_closes(self):
        class HostileOS(OSError):
            def __str__(self):raise AssertionError('memory str hook')
            def __repr__(self):raise AssertionError('memory repr hook')
        error=HostileOS('memory query failed')
        with patch.object(owned,'_memory_info',side_effect=error),self.failing_closes(error,set()):
            self.assertEqual(self.run_child()['exitCode'],0)
        self.assertEqual(len(self.closed),3);self.assertEqual(self.validate()['memoryCoverage'],'Unknown')

    def test_hostile_observer_error_is_raised_after_closes(self):
        error=Hostile('observer failed')
        def observe(value):self.observed.append(value);raise error
        with self.failing_closes(error,set()),self.assertRaises(Hostile) as caught:
            owned.run([sys.executable,'-B','-c','pass'],self.root,self.root/'console.log',time.monotonic()+8,observe=observe)
        self.assertIs(caught.exception,error);self.assertEqual(len(self.closed),3);self.validate()

    def test_large_primary_text_is_bounded_in_failure_observation(self):
        error=RuntimeError('x'*100000)
        def check():raise error
        with self.assertRaises(RuntimeError) as caught:self.run_child('import time;time.sleep(20)',check=check,log_byte_limit=65536)
        self.assertIs(caught.exception,error);value=self.validate();self.assertEqual(len(value['ownerError']),4096)
        self.assertLessEqual(len(value['logCapture']['error']),4096);self.assertTrue(value['jobDrained'])

    def test_diagnostic_memory_error_does_not_interrupt_handle_cleanup(self):
        error=RuntimeError('close failed')
        with patch.object(owned,'_clip_error',side_effect=MemoryError),self.failing_closes(error,{1,2}), \
             self.assertRaises(RuntimeError) as caught:self.run_child()
        self.assertIs(caught.exception,error);self.assertEqual(len(self.closed),3)
        self.assertEqual(self.validate()['ownerError'],'BaseException(<diagnostic unavailable>)')


if __name__=='__main__':unittest.main()
