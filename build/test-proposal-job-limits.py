"""Kernel job limits exercised with literal processes; no scientific resource trial."""
import copy
import ctypes as c
import importlib.util
import json
import mmap
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
import proposal_runtime_environment as runtime
spec=importlib.util.spec_from_file_location('job_limit_fixture',ROOT/'build/test-proposal-monitor-terminal.py')
fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)


def limits(**values):
    return dict(version='tower-owned-job-limits-v1',maxJobCommitBytes=128*1024*1024,maxActiveProcesses=8)|values


def memory_refusal(maximum):
    return f'''import ctypes as c,json
k=c.WinDLL('kernel32',use_last_error=True)
a=k.VirtualAlloc;a.restype=c.c_void_p;a.argtypes=[c.c_void_p,c.c_size_t,c.c_ulong,c.c_ulong]
f=k.VirtualFree;f.restype=c.c_int;f.argtypes=[c.c_void_p,c.c_size_t,c.c_ulong]
small=a(None,4096,0x3000,4)
assert small,'small control allocation failed'
assert f(small,0,0x8000)
large=a(None,{maximum}+4096,0x3000,4)
error=c.get_last_error()
if large:f(large,0,0x8000)
assert not large,'job commitment ceiling was not enforced'
assert error!=0
print(json.dumps(dict(smallAllocationSucceeded=True,oversizedAllocationRefused=True,error=error)))
'''


class JobLimits(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name);self.observed=[]

    def run_child(self,code,**kwargs):
        return owned.run([sys.executable,'-B','-X','utf8','-c',code],self.root,self.root/'child.log',
                         time.monotonic()+8,observe=self.observed.append,log_byte_limit=65536,**kwargs)

    def test_exact_strict_declaration(self):
        invalid=[{},[],limits(version='wrong'),dict(limits(),extra=0)]
        for name,values in (('maxJobCommitBytes',(True,0,-1,1,4097,1.0,None,2**63)),
                            ('maxActiveProcesses',(True,0,-1,1.0,None,2**32))):
            invalid.extend(limits(**{name:v}) for v in values)
        for value in invalid:
            with self.subTest(value=value),patch.object(owned,'_create_job') as create,self.assertRaises(ValueError):
                self.run_child('pass',job_limits=value)
            create.assert_not_called();self.assertFalse((self.root/'child.log').exists())

    def test_smallest_declaration_and_copies(self):
        declared=limits(maxJobCommitBytes=mmap.PAGESIZE,maxActiveProcesses=1)
        value=owned.job_limit_declaration(declared);declared['maxActiveProcesses']=99
        self.assertEqual(value['maxActiveProcesses'],1)
        instance=owned._JobLimits(value);instance.observation()['declaration']['maxActiveProcesses']=99
        self.assertEqual(instance.declaration['maxActiveProcesses'],1)

    def test_default_omits_limit_observation(self):
        self.assertEqual(self.run_child('pass')['exitCode'],0)
        self.assertNotIn('jobLimits',self.observed[0])

    def test_real_verified_limits_and_ledger(self):
        result=self.run_child('print("literal")',job_limits=limits())
        self.assertEqual(result['exitCode'],0);value=self.observed[0]['jobLimits']
        self.assertTrue(value['applied']);self.assertTrue(value['verified'])
        self.assertEqual(value['declaration'],limits());self.assertEqual(value['verificationBoundary'],'BeforeOwnedProcessCreation')
        self.assertEqual(work.job_limit_observation(value),limits())
        ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native');ledger.observe_process(self.observed[0],'c'*64)

    def test_install_and_readback_precede_create_membership_and_resume(self):
        events=[];original_set,original_query,original_create,original_member,original_resume=(owned._set_job,owned._query_job,owned._create_process,owned._is_in_job,owned._resume)
        def setting(*args):events.append('set');return original_set(*args)
        def query(*args):events.append('query');return original_query(*args)
        def create(*args):events.append('create');return original_create(*args)
        def member(*args):events.append('member');return original_member(*args)
        def resume(*args):events.append('resume');return original_resume(*args)
        with patch.object(owned,'_set_job',setting),patch.object(owned,'_query_job',query), \
             patch.object(owned,'_create_process',create),patch.object(owned,'_is_in_job',member),patch.object(owned,'_resume',resume):
            self.run_child('pass',job_limits=limits())
        self.assertEqual(events[:5],['set','query','create','member','resume'])

    def test_original_declaration_mutation_cannot_change_installed_limit(self):
        declared=limits();original=owned._create_job
        def create(*args):declared['maxJobCommitBytes']*=2;return original(*args)
        with patch.object(owned,'_create_job',create):self.run_child('pass',job_limits=declared)
        self.assertEqual(self.observed[0]['jobLimits']['declaration'],limits())

    def test_set_failure_prevents_process_and_log(self):
        error=OSError('set failed')
        with patch.object(owned,'_set_job',side_effect=error),patch.object(owned,'_create_process') as create, \
                self.assertRaises(OSError) as caught:self.run_child('pass',job_limits=limits())
        self.assertIs(caught.exception,error);create.assert_not_called();self.assertFalse((self.root/'child.log').exists())
        value=self.observed[0]['jobLimits'];self.assertFalse(value['applied']);self.assertFalse(value['verified'])
        self.assertEqual(value['error'],'set failed');self.assertEqual(work.job_limit_observation(value),limits())
        ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native');ledger.observe_process(self.observed[0],'c'*64)

    def test_query_failure_prevents_process_and_retains_unknown_verification(self):
        original=owned._query_job;calls=[];error=OSError('limit query failed')
        def query(*args):
            if args[1]==9 and not calls:calls.append(1);raise error
            return original(*args)
        with patch.object(owned,'_query_job',query),patch.object(owned,'_create_process') as create, \
                self.assertRaises(OSError) as caught:self.run_child('pass',job_limits=limits())
        self.assertIs(caught.exception,error);create.assert_not_called()
        self.assertTrue(self.observed[0]['jobLimits']['applied']);self.assertFalse(self.observed[0]['jobLimits']['verified'])

    def test_mismatched_memory_readback_cannot_resume(self):
        original=owned._query_job
        def query(*args):
            result=original(*args)
            if args[1]==9:c.cast(args[2],c.POINTER(owned._Limits)).contents.job_memory+=mmap.PAGESIZE
            return result
        with patch.object(owned,'_query_job',query),patch.object(owned,'_create_process') as create, \
                self.assertRaisesRegex(ValueError,'readback mismatch'):self.run_child('pass',job_limits=limits())
        create.assert_not_called();self.assertFalse(self.observed[0]['jobLimits']['verified'])

    def test_missing_flags_and_changed_process_count_are_rejected(self):
        for field in ('flags','active_limit'):
            with self.subTest(field=field):
                instance=owned._JobLimits(limits());job=owned._create_job(None,None);self.assertTrue(job)
                original=owned._query_job
                def query(*args):
                    result=original(*args);basic=c.cast(args[2],c.POINTER(owned._Limits)).contents.basic
                    setattr(basic,field,0);return result
                try:
                    with patch.object(owned,'_query_job',query),self.assertRaisesRegex(ValueError,'readback mismatch'):instance.install(job)
                finally:self.assertTrue(owned._close(job))

    def test_job_rejects_oversized_commit_while_small_allocation_succeeds(self):
        result=self.run_child(memory_refusal(limits()['maxJobCommitBytes']),job_limits=limits())
        self.assertEqual(result['exitCode'],0,(self.root/'child.log').read_text())
        value=json.loads((self.root/'child.log').read_bytes())
        self.assertTrue(value['smallAllocationSucceeded']);self.assertTrue(value['oversizedAllocationRefused'])
        self.assertFalse(self.observed[0]['jobLimits']['violationsObserved'])
        self.assertTrue(self.observed[0]['jobDrained'])

    def test_active_process_limit_prevents_external_child(self):
        script=self.root/'child.py';marker=self.root/'marker'
        script.write_text('from pathlib import Path;Path('+repr(str(marker))+').write_text("ran")',encoding='utf-8')
        command=[os.path.join(os.environ['SystemRoot'],'System32','cmd.exe'),'/d','/c',subprocess.list2cmdline([sys.executable,'-B',str(script)])]
        denied=owned.run(command,self.root,self.root/'denied.log',time.monotonic()+8,job_limits=limits(maxActiveProcesses=1),observe=self.observed.append)
        self.assertNotEqual(denied['exitCode'],0);self.assertFalse(marker.exists());self.assertTrue(self.observed[0]['jobDrained'])
        allowed=owned.run(command,self.root,self.root/'allowed.log',time.monotonic()+8,job_limits=limits())
        self.assertEqual(allowed['exitCode'],0);self.assertEqual(marker.read_text(),'ran')

    def test_descendant_cannot_escape_commit_limit(self):
        child=self.root/'allocate.py';child.write_text(memory_refusal(limits()['maxJobCommitBytes']),encoding='utf-8')
        code='import subprocess,sys;raise SystemExit(subprocess.call([sys.executable,"-B",'+repr(str(child))+']))'
        result=self.run_child(code,job_limits=limits())
        self.assertEqual(result['exitCode'],0,(self.root/'child.log').read_text())
        self.assertGreaterEqual(result['totalProcesses'],2)
        self.assertTrue(json.loads((self.root/'child.log').read_bytes())['oversizedAllocationRefused'])

    def test_nested_job_cannot_escape_parent_commit_limit(self):
        child=self.root/'allocate.py';child.write_text(memory_refusal(limits()['maxJobCommitBytes']),encoding='utf-8')
        code='import sys,time,json;from pathlib import Path;sys.path.insert(0,'+repr(str(ROOT/'build'))+');import bounded_windows_process as b;'
        code+='r=b.run([sys.executable,"-B",'+repr(str(child))+'],Path.cwd(),Path("nested.log"),time.monotonic()+4);print(json.dumps(r));raise SystemExit(r["exitCode"])'
        result=self.run_child(code,job_limits=limits())
        self.assertEqual(result['exitCode'],0,(self.root/'child.log').read_text())
        self.assertTrue(json.loads((self.root/'nested.log').read_bytes())['oversizedAllocationRefused'])

    def test_timeout_drains_limited_owner_and_descendant(self):
        result=owned.run([sys.executable,'-B','-c','import subprocess,sys,time;subprocess.Popen([sys.executable,"-B","-c","import time;time.sleep(30)"]);time.sleep(30)'],
            self.root,self.root/'child.log',time.monotonic()+.5,job_limits=limits(),observe=self.observed.append)
        self.assertTrue(result['timedOut']);self.assertTrue(self.observed[0]['jobDrained']);self.assertTrue(self.observed[0]['jobLimits']['verified'])

    def test_ledger_rejects_overclaims_and_malformed_states(self):
        self.run_child('pass',job_limits=limits());original=self.observed[0]
        changes=[dict(wholeProcessCoverage=True),dict(monitorMemoryBounded=True),dict(violationsObserved=True),dict(wallClockBounded=True),
                 dict(totalProcessCreationsBounded=True),dict(filesystemConfinement=True),dict(extra=0),dict(applied=False),dict(verified=1),
                 dict(error='unexpected'),dict(verificationBoundary='AfterExit')]
        for change in changes:
            value=copy.deepcopy(original);value['jobLimits'].update(change)
            ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native')
            with self.subTest(change=change),self.assertRaises(ValueError):ledger.observe_process(value,'c'*64)

    def test_ledger_rejects_executed_process_with_unverified_limits(self):
        self.run_child('pass',job_limits=limits());value=self.observed[0]
        value['jobLimits'].update(verified=False,error='query failed',verificationBoundary='Unavailable')
        ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native')
        with self.assertRaisesRegex(ValueError,'Executed process'):ledger.observe_process(value,'c'*64)

    def test_observed_declaration_rejects_wrong_types_ranges_and_fields(self):
        instance=owned._JobLimits(limits());instance.applied=instance.verified=True;original=instance.observation()
        for change in (dict(maxJobCommitBytes=True),dict(maxJobCommitBytes=0),dict(maxActiveProcesses=2**32),dict(maxActiveProcesses=0),dict(extra=1),dict(version='wrong')):
            value=copy.deepcopy(original);value['declaration'].update(change)
            with self.subTest(change=change),self.assertRaises(ValueError):work.job_limit_observation(value)


class Monitor(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup);self.base=Path(temporary.name)
        self.script=self.base/'owner.py';self.script.write_text('print("literal")',encoding='utf-8')
        self.monitor=monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=True,job_limits=limits())

    def run_monitor(self):return self.monitor.run(self.script,[],self.base,time.monotonic()+10)

    def test_monitor_binds_plan_and_observed_configuration(self):
        self.assertEqual(self.run_monitor()['exitCode'],0)
        value=self.monitor.observation
        self.assertEqual(value['jobLimitPlan'],value['processObservation']['jobLimits']['declaration'])
        self.assertEqual(value['jobLimitPlan'],limits());self.assertFalse(value['wholeProcessCoverage'])

    def test_monitor_rejects_invalid_limits_before_any_directory(self):
        with self.assertRaises(ValueError):monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,job_limits=limits(maxActiveProcesses=0))
        self.assertFalse(self.monitor.root.exists())

    def test_monitor_plan_is_copied_before_run(self):
        declared=limits();self.monitor=monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,job_limits=declared)
        declared['maxActiveProcesses']=1;self.assertEqual(self.run_monitor()['exitCode'],0)
        self.assertEqual(self.monitor.observation['jobLimitPlan'],limits())

    def test_setup_error_preserves_original_and_failed_limit_evidence(self):
        error=OSError('limit installation failed')
        with patch.object(owned,'_set_job',side_effect=error),self.assertRaises(OSError) as caught:self.run_monitor()
        self.assertIs(caught.exception,error);self.assertFalse(self.monitor.observation['processObservation']['jobLimits']['verified'])
        self.assertEqual(self.monitor.observation['processOutcome'],'MonitorFailed')

    def test_missing_limit_observation_is_rejected(self):
        original=owned.run
        def run(*args,**kwargs):
            observer=kwargs['observe']
            def observe(value):value.pop('jobLimits');observer(value)
            kwargs['observe']=observe;return original(*args,**kwargs)
        with patch.object(owned,'run',run),self.assertRaisesRegex(ValueError,'missing owner job limits'):self.run_monitor()

    def test_changed_limit_observation_is_rejected(self):
        original=owned.run
        def run(*args,**kwargs):
            observer=kwargs['observe']
            def observe(value):value['jobLimits']['declaration']['maxActiveProcesses']+=1;observer(value)
            kwargs['observe']=observe;return original(*args,**kwargs)
        with patch.object(owned,'run',run),self.assertRaisesRegex(ValueError,'Changed or missing'):self.run_monitor()

    def test_routed_owner_composes_limits_environment_io_and_terminal(self):
        export=os.environ.get('LL_JOB_LIMITS_EXPORT');base=Path(export) if export else self.base/'routed';base.mkdir()
        source=fixture.fixture.fixture.f.fixture.Supervisor();source.setUp();self.addCleanup(source.doCleanups)
        supervisor=source.prepare(base/'owner')
        profile=dict(version=runtime.VERSION,root=str(self.base/'runtime'),maxEntries=128,maxDepth=8,maxRetainedBytes=1048576)
        declared=limits(maxJobCommitBytes=512*1024*1024,maxActiveProcesses=16)
        instance=monitor.OwnerProcessMonitor(base/'monitor',monitor.sha(source.package/'request.json'),monitor_accounting=True,
            monitor_io_budget=fixture.declaration(base/'terminal'),runtime_environment=profile,job_limits=declared)
        result=instance.run(fixture.fixture.fixture.f.__file__,['--fixture-owner',str(base/'owner'),'success'],ROOT,time.monotonic()+20,
            protected_roots=[source.package,source.output.parent,supervisor.root])
        self.assertEqual(result['exitCode'],0,(instance.root/'console.log').read_text())
        observed=instance.observation['processObservation']['jobLimits']
        self.assertTrue(observed['verified']);self.assertEqual(observed['declaration'],declared)
        self.assertEqual(instance.terminal_publication_observation['outcome'],'Verified')
        self.assertEqual(instance.terminal_publication_observation['monitorIO']['hashCompleted'],12)
        self.assertEqual(len(list((base/'owner').glob('nested-*.json'))),4)
        if export:
            work.seal_new(base/'publication-result.json',instance.terminal_publication_observation)
            work.seal_new(base/'fixture.json',dict(fixtureOnly=True,scientificWorkersRouted=True,realNestedLiteralJobs=4,
                actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
                publicationResultPersistedByTestOutsideBudget=True,usableForAdmission=False))
            work.seal_new(base/'files.json',{p.relative_to(base).as_posix():monitor.sha(p) for p in sorted(base.rglob('*')) if p.is_file()})


if __name__=='__main__':unittest.main()
