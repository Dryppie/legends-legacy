"""Restricted Windows inheritance; literal workers and injected API faults only."""
import ctypes as c
import json
import msvcrt
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


class Inheritance(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name);self.observed=[]
        self.output=open(self.root/'output','xb',buffering=0);self.addCleanup(self.output.close)
        self.input=open(os.devnull,'rb',buffering=0);self.addCleanup(self.input.close)
        self.handles=[msvcrt.get_osfhandle(f.fileno()) for f in (self.output,self.input)]
        self.job=owned._check(owned._create_job(None,None));self.addCleanup(owned._close,self.job)

    def startup(self):return owned._standard_handle_startup(*self.handles,self.job)

    def run_child(self,code='pass',**options):
        return owned.run([sys.executable,'-B','-c',code],self.root,self.root/'console.log',time.monotonic()+8,
                         observe=self.observed.append,**options)

    def test_structure_and_exact_two_handle_attribute(self):
        original=owned._update_attribute;seen=[]
        def update(storage,flags,key,values,size,previous,returned):
            if key==0x2000d:
                self.assertEqual(size,c.sizeof(owned.w.HANDLE))
                self.assertEqual(c.cast(values,c.POINTER(owned.w.HANDLE)).contents.value,self.job)
                return original(storage,flags,key,values,size,previous,returned)
            self.assertEqual((flags,key,size,previous,returned),(0,0x20002,2*c.sizeof(owned.w.HANDLE),None,None))
            actual=list(c.cast(values,c.POINTER(owned.w.HANDLE*2)).contents)
            self.assertEqual(actual,self.handles);self.assertTrue(all(os.get_handle_inheritable(h) for h in actual))
            seen.append(c.addressof(storage));return original(storage,flags,key,values,size,previous,returned)
        with patch.object(owned,'_update_attribute',update),self.startup() as value:
            self.assertEqual(value.startup.size,c.sizeof(owned._StartupEx))
            self.assertEqual(value.startup.flags,0x100)
            self.assertEqual((value.startup.stdin,value.startup.stdout,value.startup.stderr),
                             (self.handles[1],self.handles[0],self.handles[0]))
            self.assertEqual(value.attributes,seen[0])
        self.assertFalse(any(os.get_handle_inheritable(h) for h in self.handles))

    def test_preexisting_inheritance_flags_are_restored(self):
        os.set_handle_inheritable(self.handles[0],True)
        with self.startup():pass
        self.assertEqual([os.get_handle_inheritable(h) for h in self.handles],[True,False])

    def test_attribute_storage_lives_until_delete_and_deletes_once(self):
        original=owned._delete_attributes;deleted=[]
        def delete(storage):deleted.append(c.addressof(storage));original(storage)
        with patch.object(owned,'_delete_attributes',delete):
            with self.startup() as value:address=value.attributes;self.assertEqual(deleted,[])
        self.assertEqual(deleted,[address])

    def bad_sizing(self,size=48,result=0,error=122):
        def initialize(storage,count,flags,pointer):
            self.assertIsNone(storage);c.cast(pointer,c.POINTER(c.c_size_t)).contents.value=size
            c.set_last_error(error);return result
        return initialize

    def test_unexpected_success_does_not_allocate_or_launch(self):
        with patch.object(owned,'_initialize_attributes',self.bad_sizing(result=1)), \
             patch.object(owned.c,'create_string_buffer') as allocate,patch.object(owned,'_create_process') as create, \
             self.assertRaises(OSError):self.run_child()
        allocate.assert_not_called();create.assert_not_called()

    def test_sizing_error_has_no_broad_inheritance_fallback(self):
        with patch.object(owned,'_initialize_attributes',self.bad_sizing(error=5)), \
             patch.object(owned,'_create_process') as create,self.assertRaises(OSError):self.run_child()
        create.assert_not_called();self.assertEqual(self.observed[0]['cleanup']['handleCloses']['job'],'Closed')

    def test_zero_or_oversized_attribute_storage_is_rejected(self):
        for size in (0,65537,2**32):
            with self.subTest(size=size),patch.object(owned,'_initialize_attributes',self.bad_sizing(size=size)), \
                 patch.object(owned.c,'create_string_buffer') as allocate,self.assertRaises(ValueError):
                with self.startup():self.fail('Must not yield')
            allocate.assert_not_called()

    def test_initialization_failure_does_not_delete_uninitialized_list(self):
        original=owned._initialize_attributes
        def initialize(storage,*args):
            if storage is None:return original(storage,*args)
            c.set_last_error(8);return 0
        with patch.object(owned,'_initialize_attributes',initialize),patch.object(owned,'_delete_attributes') as delete, \
             patch.object(owned,'_create_process') as create,self.assertRaises(OSError):self.run_child()
        create.assert_not_called();delete.assert_not_called()

    def test_update_failure_restores_flags_and_deletes_list(self):
        error=OSError('attribute update refused');original=owned._delete_attributes
        with patch.object(owned,'_update_attribute',side_effect=error),patch.object(owned,'_delete_attributes',wraps=original) as delete, \
             self.assertRaises(OSError) as caught:
            with self.startup():self.fail('Must not yield')
        self.assertIs(caught.exception,error);self.assertEqual(delete.call_count,1)
        self.assertFalse(any(os.get_handle_inheritable(h) for h in self.handles))

    def test_failed_update_return_stops_before_creation(self):
        with patch.object(owned,'_update_attribute',return_value=0),patch.object(owned,'_create_process') as create, \
             self.assertRaises(OSError):self.run_child()
        create.assert_not_called()

    def test_second_flag_enable_failure_restores_both_handles(self):
        original=os.set_handle_inheritable;calls=[];error=OSError('enable failed')
        def set_flag(handle,value):
            calls.append((handle,value));original(handle,value)
            if len(calls)==2:raise error
        with patch.object(owned.os,'set_handle_inheritable',set_flag),self.assertRaises(OSError) as caught:
            with self.startup():self.fail('Must not yield')
        self.assertIs(caught.exception,error);self.assertEqual(calls,[(h,True) for h in self.handles]+[(h,False) for h in self.handles])
        self.assertFalse(any(os.get_handle_inheritable(h) for h in self.handles))

    def test_flag_query_failure_still_restores_first_handle(self):
        original=os.get_handle_inheritable;error=OSError('flag query failed')
        def get_flag(handle):
            if handle==self.handles[1]:raise error
            return original(handle)
        with patch.object(owned.os,'get_handle_inheritable',get_flag),self.assertRaises(OSError) as caught:
            with self.startup():self.fail('Must not yield')
        self.assertIs(caught.exception,error);self.assertFalse(original(self.handles[0]))

    def restoration_failure(self):
        original=os.set_handle_inheritable;calls=[];error=OSError('restore failed')
        def set_flag(handle,value):
            original(handle,value)
            if not value:
                calls.append(handle)
                if len(calls)==1:raise error
        return patch.object(owned.os,'set_handle_inheritable',set_flag),calls,error

    def test_failed_restore_does_not_skip_other_handle_or_delete(self):
        injected,calls,error=self.restoration_failure();original=owned._delete_attributes
        with injected,patch.object(owned,'_delete_attributes',wraps=original) as delete,self.assertRaises(OSError) as caught:
            with self.startup():pass
        self.assertIs(caught.exception,error);self.assertEqual(calls,self.handles);self.assertEqual(delete.call_count,1)

    def test_body_error_remains_primary_during_restore_failure(self):
        injected,calls,_=self.restoration_failure();error=KeyboardInterrupt('literal interrupt')
        with injected,self.assertRaises(KeyboardInterrupt) as caught:
            with self.startup():raise error
        self.assertIs(caught.exception,error);self.assertEqual(calls,self.handles)
        self.assertIn('inheritance restoration failed',error.__notes__[0])

    def test_caller_exception_cannot_suppress_restoration_failure(self):
        injected,calls,error=self.restoration_failure()
        try:raise RuntimeError('already handled')
        except RuntimeError:
            with injected,self.assertRaises(OSError) as caught:
                with self.startup():pass
        self.assertIs(caught.exception,error);self.assertEqual(calls,self.handles)

    def test_delete_failure_preserves_body_error_after_restoring_flags(self):
        original=owned._delete_attributes;error=RuntimeError('body failed')
        def delete(storage):original(storage);raise OSError('delete failed after real deletion')
        with patch.object(owned,'_delete_attributes',delete),self.assertRaises(RuntimeError) as caught:
            with self.startup():raise error
        self.assertIs(caught.exception,error);self.assertIn('attribute-list deletion failed',error.__notes__[0])
        self.assertFalse(any(os.get_handle_inheritable(h) for h in self.handles))

    def test_create_failure_is_preserved_and_attributes_are_deleted(self):
        error=OSError('literal creation failure');original=owned._delete_attributes
        with patch.object(owned,'_create_process',side_effect=error),patch.object(owned,'_delete_attributes',wraps=original) as delete, \
             self.assertRaises(OSError) as caught:self.run_child()
        self.assertIs(caught.exception,error);self.assertEqual(delete.call_count,1)
        self.assertEqual(self.observed[0]['cleanup']['handleCloses'],dict(thread='NotAcquired',process='NotAcquired',job='Closed'))

    def test_post_creation_restore_failure_terminates_without_resuming(self):
        injected,_,error=self.restoration_failure();terminate=owned._terminate_job
        with injected,patch.object(owned,'_resume') as resume,patch.object(owned,'_terminate_job',wraps=terminate) as terminated, \
             self.assertRaises(OSError) as caught:self.run_child('raise RuntimeError("must not run")')
        self.assertIs(caught.exception,error);resume.assert_not_called();self.assertEqual(terminated.call_count,1)
        self.assertEqual((self.root/'console.log').read_bytes(),b'')
        self.assertTrue(self.observed[0]['cleanup']['allAcquiredHandlesClosed'])

    def test_post_creation_delete_failure_terminates_without_resuming(self):
        original=owned._delete_attributes
        def delete(storage):original(storage);raise OSError('delete failed after real deletion')
        with patch.object(owned,'_delete_attributes',delete),patch.object(owned,'_resume') as resume, \
             self.assertRaisesRegex(OSError,'delete failed'):self.run_child()
        resume.assert_not_called();self.assertTrue(self.observed[0]['cleanup']['allAcquiredHandlesClosed'])

    def probe(self,captured):
        export=os.environ.get('LL_HANDLE_INHERITANCE_EXPORT')
        if export:self.root=Path(export)/('captured' if captured else 'direct');self.root.mkdir(parents=True)
        with open(self.root/'unrelated.bin','x+b',buffering=0) as unrelated:
            unrelated.write(b'literal parent-only writable handle')
            handle=msvcrt.get_osfhandle(unrelated.fileno());os.set_handle_inheritable(handle,True)
            code='''import ctypes as c,json,os,sys
from ctypes import wintypes as w
k=c.WinDLL('kernel32',use_last_error=True)
query=k.GetFinalPathNameByHandleW;query.argtypes=[w.HANDLE,w.LPWSTR,w.DWORD,w.DWORD];query.restype=w.DWORD
path=c.create_unicode_buffer(32768);count=query(PROBE_HANDLE,path,len(path),0)
actual=os.path.normcase(path.value.removeprefix(chr(92)*2+"?"+chr(92))) if count else None
expected=os.path.normcase(TARGET)
assert actual!=expected, 'unrelated writable file handle inherited'
assert sys.stdin.buffer.read()==b''
print(json.dumps(dict(unrelatedFileInherited=False,stdinEof=True)))
sys.stdout.flush();sys.stderr.write('literal stderr\\n')
'''.replace('PROBE_HANDLE',str(handle)).replace('TARGET',repr(str(self.root/'unrelated.bin')))
            original=owned._create_process;flags=[]
            def create(*args):
                self.assertTrue(args[4]);self.assertEqual(args[5]&0x8080004,0x8080004)
                startup=c.cast(args[8],c.POINTER(owned._StartupEx)).contents
                self.assertNotEqual(startup.startup.stdout,handle);self.assertTrue(startup.attributes)
                flags.append(args[5]);return original(*args)
            with patch.object(owned,'_create_process',create):
                result=self.run_child(code,**({'log_byte_limit':65536} if captured else {}))
            self.assertEqual(result['exitCode'],0,(self.root/'console.log').read_text())
            self.assertTrue(os.get_handle_inheritable(handle));unrelated.seek(0)
            self.assertEqual(unrelated.read(),b'literal parent-only writable handle')
        output=(self.root/'console.log').read_text().splitlines()
        self.assertEqual(json.loads(output[0]),dict(unrelatedFileInherited=False,stdinEof=True))
        self.assertEqual(output[1],'literal stderr');self.assertEqual(len(flags),1)
        self.assertTrue(self.observed[0]['jobDrained']);self.assertTrue(self.observed[0]['cleanup']['allAcquiredHandlesClosed'])
        if export:
            work.seal_new(self.root/'observation.json',self.observed[0])
            work.seal_new(self.root/'fixture.json',dict(fixtureOnly=True,unrelatedFileInherited=False,
                parentHandleStillInheritable=True,stdinEof=True,stdoutAndStderrVerified=True,creationFlags=flags[0],
                processHelperSha256=monitor.sha(owned.__file__),actualCombat=0,productionEntropyDraws=0,
                scientificReservations=0,nativeEncounterPreparations=0,wholeProcessCoverage=False,usableForAdmission=False))
            work.seal_new(self.root/'files.json',{p.name:monitor.sha(p) for p in sorted(self.root.iterdir()) if p.is_file()})

    def test_unrelated_inheritable_file_excluded_with_direct_log(self):self.probe(False)

    def test_unrelated_inheritable_file_excluded_with_bounded_capture(self):self.probe(True)

    def test_probe_detects_legacy_broad_inheritance_in_literal_control(self):
        original=owned._update_attribute
        def broad(*args):
            # Omit only HANDLE_LIST; keep atomic job ownership for this control.
            return 1 if args[2]==0x20002 else original(*args)
        with patch.dict(os.environ,LL_HANDLE_INHERITANCE_EXPORT=''),patch.object(owned,'_update_attribute',broad), \
             self.assertRaisesRegex(AssertionError,'unrelated writable file handle inherited'):
            self.probe(False)

    def test_replacement_unicode_environment_composes_with_extended_startup(self):
        result=self.run_child('import os;print(os.environ["LITERAL"])',environment={'LITERAL':'value'})
        self.assertEqual(result['exitCode'],0);self.assertEqual((self.root/'console.log').read_text().strip(),'value')
        self.assertFalse(self.observed[0]['environment']['inherited'])


if __name__=='__main__':unittest.main()
