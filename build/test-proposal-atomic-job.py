"""Atomic owned-job creation: real membership, abrupt owner exit and API faults."""
import ctypes as c
import json
import os
from pathlib import Path
import subprocess
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


class AtomicJob(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name);self.observed=[]

    def run_child(self,code='print("literal worker")',**options):
        return owned.run([sys.executable,'-B','-c',code],self.root,self.root/'console.log',time.monotonic()+8,
                         observe=self.observed.append,**options)

    def membership_probe(self,captured):
        export=os.environ.get('LL_ATOMIC_JOB_EXPORT')
        if export:self.root=Path(export)/('captured' if captured else 'direct');self.root.mkdir(parents=True)
        create,update,initialize=owned._create_process,owned._update_attribute,owned._initialize_attributes
        job=[];standard=[];events=[];counts=[];before=[]
        def init(storage,count,*rest):counts.append(count);return initialize(storage,count,*rest)
        def attribute(storage,flags,key,values,size,*rest):
            events.append(key)
            if key==0x2000d:
                self.assertEqual(size,c.sizeof(owned.w.HANDLE))
                job.append(c.cast(values,c.POINTER(owned.w.HANDLE)).contents.value)
                self.assertFalse(os.get_handle_inheritable(job[0]))
            elif key==0x20002:standard.extend(c.cast(values,c.POINTER(owned.w.HANDLE*2)).contents)
            return update(storage,flags,key,values,size,*rest)
        def created(*args):
            result=create(*args)
            if result:
                process=c.cast(args[-1],c.POINTER(owned._Process)).contents
                member=owned.w.BOOL();owned._check(owned._is_in_job(process.process,job[0],c.byref(member)))
                self.assertTrue(member.value);before.append(True)
                state=owned._Accounting();owned._check(owned._query_job(job[0],1,c.byref(state),c.sizeof(state),None))
                self.assertEqual((state.total_processes,state.active_processes),(1,1))
            return result
        with patch.object(owned,'_initialize_attributes',init),patch.object(owned,'_update_attribute',attribute), \
             patch.object(owned,'_create_process',created):
            result=self.run_child(**({'log_byte_limit':65536} if captured else {}))
        self.assertEqual(result['exitCode'],0);self.assertEqual(before,[True]);self.assertEqual(counts,[2,2])
        self.assertEqual(events,[0x20002,0x2000d]);self.assertNotIn(job[0],standard);self.assertEqual(len(standard),2)
        self.assertTrue(self.observed[0]['jobDrained']);self.assertTrue(self.observed[0]['cleanup']['allAcquiredHandlesClosed'])
        if export:
            work.seal_new(self.root/'observation.json',self.observed[0])
            work.seal_new(self.root/'fixture.json',dict(fixtureOnly=True,membershipBeforeCreateReturned=True,
                jobHandleInherited=False,attributeCount=2,standardHandleCount=2,
                processHelperSha256=monitor.sha(owned.__file__),actualCombat=0,productionEntropyDraws=0,
                scientificReservations=0,nativeEncounterPreparations=0,usableForAdmission=False))
            self.seal()

    def seal(self):
        work.seal_new(self.root/'files.json',{p.name:monitor.sha(p) for p in sorted(self.root.iterdir()) if p.is_file()})

    def test_membership_exists_at_create_return_with_direct_log(self):self.membership_probe(False)

    def test_membership_exists_at_create_return_with_bounded_capture(self):self.membership_probe(True)

    def test_job_attribute_failure_prevents_creation_and_deletes_list(self):
        original=owned._update_attribute;error=OSError('literal job-list rejection');delete=owned._delete_attributes
        def update(*args):
            if args[2]==0x2000d:raise error
            return original(*args)
        with patch.object(owned,'_update_attribute',update),patch.object(owned,'_create_process') as create, \
             patch.object(owned,'_delete_attributes',wraps=delete) as deleted,self.assertRaises(OSError) as caught:self.run_child()
        self.assertIs(caught.exception,error);create.assert_not_called();self.assertEqual(deleted.call_count,1)
        self.assertEqual(self.observed[0]['cleanup']['handleCloses'],dict(thread='NotAcquired',process='NotAcquired',job='Closed'))

    def test_unsupported_job_attribute_never_falls_back(self):
        original=owned._update_attribute
        def update(*args):
            if args[2]==0x2000d:c.set_last_error(87);return 0
            return original(*args)
        with patch.object(owned,'_update_attribute',update),patch.object(owned,'_create_process') as create, \
             self.assertRaises(OSError) as caught:self.run_child()
        self.assertEqual(caught.exception.winerror,87);create.assert_not_called()

    def test_false_membership_stops_before_resume_and_retains_unknown(self):
        def member(process,job,pointer):c.cast(pointer,c.POINTER(owned.w.BOOL)).contents.value=0;return 1
        terminate=owned._terminate_process
        with patch.object(owned,'_is_in_job',member),patch.object(owned,'_resume') as resume, \
             patch.object(owned,'_terminate_process',wraps=terminate) as terminated, \
             self.assertRaisesRegex(RuntimeError,'not in its owned job'):self.run_child()
        resume.assert_not_called();self.assertEqual(terminated.call_count,1)
        self.assertFalse(self.observed[0]['jobDrained']);self.assertEqual(self.observed[0]['jobIo']['coverage'],'Unknown')
        self.assertTrue(self.observed[0]['cleanup']['allAcquiredHandlesClosed'])

    def test_query_failure_preserves_original_without_resuming(self):
        error=OSError('membership query failed')
        with patch.object(owned,'_is_in_job',side_effect=error),patch.object(owned,'_resume') as resume, \
             self.assertRaises(OSError) as caught:self.run_child()
        self.assertIs(caught.exception,error);resume.assert_not_called()
        self.assertEqual(self.observed[0]['ownerError'],'membership query failed')

    def test_omitted_job_attribute_is_detected_and_child_is_terminated(self):
        original=owned._update_attribute
        def update(*args):return 1 if args[2]==0x2000d else original(*args)
        with patch.object(owned,'_update_attribute',update),patch.object(owned,'_resume') as resume, \
             self.assertRaisesRegex(RuntimeError,'not in its owned job'):self.run_child()
        resume.assert_not_called();self.assertEqual((self.root/'console.log').read_bytes(),b'')
        self.assertTrue(self.observed[0]['cleanup']['allAcquiredHandlesClosed'])

    def test_failed_creation_does_not_claim_membership_or_terminate(self):
        with patch.object(owned,'_create_process',return_value=0),patch.object(owned,'_is_in_job') as member, \
             patch.object(owned,'_terminate_process') as terminate,self.assertRaises(OSError):self.run_child()
        member.assert_not_called();terminate.assert_not_called();self.assertFalse(self.observed[0]['jobDrained'])

    def test_limit_refusal_at_create_has_no_surviving_new_member(self):
        # Occupy the sole allowed slot with one suspended literal child.
        original_create=owned._create_process;original_update=owned._update_attribute
        retained=[];job=[]
        def update(*args):
            if args[2]==0x2000d:job.append(c.cast(args[3],c.POINTER(owned.w.HANDLE)).contents.value)
            return original_update(*args)
        def create(*args):
            extra=owned._Process();first=list(args);first[-1]=c.byref(extra)
            owned._check(original_create(*first));retained.append(extra)
            return original_create(*args)
        with patch.object(owned,'_update_attribute',update),patch.object(owned,'_create_process',create), \
             self.assertRaises(OSError):
            try:self.run_child(job_limits=dict(version='tower-owned-job-limits-v1',maxJobCommitBytes=128*1024*1024,maxActiveProcesses=1))
            finally:
                for value in retained:
                    # Closing the last job handle kills the retained suspended child.
                    try:self.assertEqual(owned._wait(value.process,5000),0)
                    finally:
                        try:owned._check(owned._close(value.thread))
                        finally:owned._check(owned._close(value.process))
        self.assertEqual(len(retained),1);self.assertEqual(self.observed[0]['cleanup']['handleCloses']['job'],'Closed')

    def abrupt_exit(self,phase):
        export=os.environ.get('LL_ATOMIC_JOB_EXPORT')
        if export:self.root=Path(export)/('crash-'+phase);self.root.mkdir(parents=True)
        script=self.root/'owner.py';ready=self.root/'ready.json';marker=self.root/'must-not-run'
        source='''import ctypes as c,json,os,sys,time
from pathlib import Path
sys.path.insert(0,BUILD)
import bounded_windows_process as b
job=[];process=[]
update=b._update_attribute;create=b._create_process;delete=b._delete_attributes
def attribute(*args):
 if args[2]==0x2000d:job.append(c.cast(args[3],c.POINTER(b.w.HANDLE)).contents.value)
 return update(*args)
def crash():
 member=b.w.BOOL();b._check(b._is_in_job(process[0].process,job[0],c.byref(member)))
 assert member.value
 value=dict(pid=process[0].pid,inOwnedJob=True,phase=PHASE)
 Path('ready.pending').write_text(json.dumps(value),encoding='utf-8');os.replace('ready.pending','ready.json')
 assert sys.stdin.buffer.read(1)==b'x'
 os._exit(73)
def created(*args):
 result=create(*args)
 if result:
  value=c.cast(args[-1],c.POINTER(b._Process)).contents
  process.append(b._Process(value.process,value.thread,value.pid,value.tid))
  if PHASE=='create':crash()
 return result
def deleting(*args):
 if PHASE=='cleanup':crash()
 return delete(*args)
b._update_attribute=attribute;b._create_process=created;b._delete_attributes=deleting
b.run([sys.executable,'-B','-c',CHILD],Path.cwd(),Path('child.log'),time.monotonic()+20)
raise RuntimeError('owner must exit abruptly')
'''.replace('BUILD',repr(str(ROOT/'build'))).replace('PHASE',repr(phase)).replace('CHILD',repr('from pathlib import Path;Path('+repr(str(marker))+').write_text("ran")'))
        script.write_text(source,encoding='utf-8',newline='\n')
        opening=owned._api('OpenProcess',owned.w.HANDLE,owned.w.DWORD,owned.w.BOOL,owned.w.DWORD)
        handle=None
        with (self.root/'owner.log').open('xb') as output:
            owner=subprocess.Popen([sys.executable,'-B',str(script)],cwd=self.root,stdin=subprocess.PIPE,
                stdout=output,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW)
            try:
                deadline=time.monotonic()+10
                while not ready.exists():
                    if owner.poll() is not None or time.monotonic()>=deadline:
                        self.fail('Owner failed before ready: '+(self.root/'owner.log').read_text())
                    time.sleep(.01)
                value=json.loads(ready.read_bytes());self.assertTrue(value['inOwnedJob'])
                handle=owned._check(opening(0x100000,False,value['pid']))
                owner.communicate(b'x',timeout=8);self.assertEqual(owner.returncode,73)
                self.assertEqual(owned._wait(handle,5000),0,'Suspended child survived owner exit')
                self.assertFalse(marker.exists())
            finally:
                if owner.poll() is None:owner.kill();owner.wait(timeout=8)
                if owner.stdin is not None:owner.stdin.close()
                if handle is not None:owned._check(owned._close(handle))
        if export:
            work.seal_new(self.root/'fixture.json',dict(fixtureOnly=True,phase=phase,ownerExitCode=73,
                childSignaledAfterOwnerExit=True,childNeverResumed=True,processHelperSha256=monitor.sha(owned.__file__),
                actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
                wallClockBounded=False,wholeProcessCoverage=False,usableForAdmission=False))
            self.seal()

    def test_owner_exit_inside_create_return_kills_suspended_child(self):self.abrupt_exit('create')

    def test_owner_exit_during_attribute_cleanup_kills_suspended_child(self):self.abrupt_exit('cleanup')


if __name__=='__main__':unittest.main()
