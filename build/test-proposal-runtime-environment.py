"""Explicit environment and runtime metadata inspection; literal child jobs only."""
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import bounded_windows_process as owned
import proposal_owner_process as monitor
import proposal_runtime_environment as runtime
import proposal_work_accounting as work


def declaration(root,**options):
    return dict(version=runtime.VERSION,root=str(root),maxEntries=64,maxDepth=6,maxRetainedBytes=65536)|options


class Environment(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);self.root=Path(temp.name);self.observed=[]
        self.env={'SystemRoot':os.environ['SystemRoot'],'TEMP':str(self.root),'TMP':str(self.root),'PYTHONDONTWRITEBYTECODE':'1'}

    def run_child(self,code,**kwargs):
        return owned.run([sys.executable,'-B','-c',code],self.root,self.root/'child.log',time.monotonic()+6,
                         observe=self.observed.append,log_byte_limit=32768,**kwargs)

    def test_replacement_excludes_inherited_sentinel_and_preserves_unicode(self):
        self.env['UNICODE_VALUE']='snow 雪 😀 = value'
        with patch.dict(os.environ,LL_RUNTIME_TEST_SENTINEL='do not inherit'):
            result=self.run_child('import os,json;print(json.dumps([os.getenv("LL_RUNTIME_TEST_SENTINEL"),os.environ["UNICODE_VALUE"]]))',environment=self.env)
        self.assertEqual(result['exitCode'],0)
        self.assertEqual(json.loads((self.root/'child.log').read_bytes()),[None,self.env['UNICODE_VALUE']])
        self.assertEqual(self.observed[0]['environment'],owned.environment_block(self.env)[1])
        ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native');ledger.observe_process(self.observed[0],'c'*64)

    def test_default_still_inherits_and_keeps_legacy_schema(self):
        with patch.dict(os.environ,LL_RUNTIME_TEST_SENTINEL='literal'):
            self.run_child('import os;print(os.environ["LL_RUNTIME_TEST_SENTINEL"])')
        self.assertEqual((self.root/'child.log').read_text().strip(),'literal');self.assertNotIn('environment',self.observed[0])

    def test_case_normalization_sorting_and_double_terminator(self):
        raw,observed=owned.environment_block({'z':'2','A':'1'})
        self.assertEqual(raw,'A=1\0Z=2\0\0'.encode('utf-16-le'))
        self.assertEqual(owned.environment_block({'Z':'2','a':'1'}),(raw,observed))
        self.assertEqual(observed['sha256'],hashlib.sha256(raw).hexdigest())
        self.assertEqual(owned.environment_block({})[0],b'\0'*4)

    def test_invalid_blocks_reject_before_job_or_log(self):
        for env in ([],{'A':'x','a':'y'},{'A=B':'x'},{'':'x'},{'A':'x\0y'},{'A':1},{'A':'x'*32768},
                    {str(i):'x' for i in range(129)},{'A':'\ud800'}):
            with self.subTest(env_type=type(env)),patch.object(owned,'_create_job') as create,self.assertRaises((ValueError,UnicodeEncodeError)):
                self.run_child('pass',environment=env)
            create.assert_not_called();self.assertFalse((self.root/'child.log').exists())

    def test_relative_executable_cannot_consult_parent_path(self):
        with patch.object(owned,'_create_job') as create,self.assertRaisesRegex(ValueError,'absolute executable'):
            owned.run(['python','-c','pass'],self.root,self.root/'child.log',time.monotonic()+5,environment=self.env)
        create.assert_not_called();self.assertFalse((self.root/'child.log').exists())

    def test_block_copied_before_launch_callback_mutation(self):
        self.env['VALUE']='before';expected=owned.environment_block(self.env)[1];original=owned._create_job
        def changed(*args):self.env['VALUE']='after';return original(*args)
        with patch.object(owned,'_create_job',side_effect=changed):self.run_child('import os;print(os.environ["VALUE"])',environment=self.env)
        self.assertEqual((self.root/'child.log').read_text().strip(),'before');self.assertEqual(self.observed[0]['environment'],expected)

    def test_timeout_and_descendant_cleanup_keep_environment_identity(self):
        command='import subprocess,sys,time;subprocess.Popen([sys.executable,"-B","-c","import time;time.sleep(30)"]);time.sleep(30)'
        result=owned.run([sys.executable,'-B','-c',command],self.root,self.root/'child.log',time.monotonic()+.4,
                         observe=self.observed.append,environment=self.env)
        self.assertTrue(result['timedOut']);self.assertTrue(self.observed[0]['jobDrained'])
        self.assertEqual(self.observed[0]['environment'],owned.environment_block(self.env)[1])

    def test_ledger_rejects_environment_overclaims(self):
        self.run_child('pass',environment=self.env);original=self.observed[0]
        for change in (dict(inherited=True),dict(runtimeWritesBounded=True),dict(wholeProcessCoverage=True),dict(utf16Bytes=3),dict(variableCount=True),dict(extra=0),dict(sha256='bad')):
            value=copy.deepcopy(original);value['environment'].update(change);ledger=work.OwnerLedger('a'*64,'b'*64);ledger.begin('native')
            with self.assertRaises(ValueError):ledger.observe_process(value,'c'*64)


class RuntimeProfile(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);self.root=Path(temp.name)
        self.value=declaration(self.root/'runtime');self.profile=runtime.RuntimeEnvironment(self.value)

    def test_fresh_seven_directories_and_copied_samples(self):
        self.profile.prepare();self.assertEqual({p.name for p in self.profile.root.iterdir()},set(runtime.DIRECTORIES))
        self.assertEqual(self.profile.sample(),0)
        v=self.profile.snapshot();v['lastSample']['entries'].clear();self.value.clear()
        self.assertEqual(len(self.profile.snapshot()['lastSample']['entries']),7)
        with self.assertRaisesRegex(ValueError,'reused'):self.profile.prepare()

    def test_known_destinations_and_settings_without_parent_hooks(self):
        with patch.dict(os.environ,PYTHONPATH='untrusted',DOTNET_STARTUP_HOOKS='untrusted',PATH='untrusted'):
            env=self.profile.environment()
        self.assertNotIn('PYTHONPATH',env);self.assertNotIn('DOTNET_STARTUP_HOOKS',env)
        self.assertEqual(env['TEMP'],env['TMP']);self.assertEqual(env['DOTNET_EnableDiagnostics'],'0')
        self.assertEqual(env['PYTHONNOUSERSITE'],'1');self.assertNotIn('untrusted',env.values())

    def test_sample_byte_limit_is_not_a_write_quota(self):
        profile=runtime.RuntimeEnvironment(declaration(self.root/'bytes',maxRetainedBytes=3));profile.prepare()
        path=profile.root/'temp/value';path.write_bytes(b'123');self.assertEqual(profile.sample(),3)
        path.write_bytes(b'1234')
        with self.assertRaisesRegex(ValueError,'sampled byte'):profile.sample()
        self.assertEqual(path.read_bytes(),b'1234');self.assertIsNone(profile.snapshot()['lastSample'])
        self.assertFalse(profile.snapshot()['runtimeWritesBounded'])

    def test_entry_budget_limits_inspection(self):
        profile=runtime.RuntimeEnvironment(declaration(self.root/'entries',maxEntries=7));profile.prepare()
        (profile.root/'temp/value').write_bytes(b'')
        with self.assertRaisesRegex(ValueError,'entry inspection'):profile.sample()
        self.assertEqual(profile.snapshot()['outcome'],'FailedOrUnknown')

    def test_depth_budget_limits_recursion(self):
        profile=runtime.RuntimeEnvironment(declaration(self.root/'depth',maxDepth=1));profile.prepare()
        (profile.root/'temp/child').mkdir()
        with self.assertRaisesRegex(ValueError,'depth inspection'):profile.sample()

    def test_existing_root_and_protected_overlap_reject(self):
        for value,protected in ((declaration(self.root),()),(self.value,(self.root,))):
            with self.assertRaises(ValueError):runtime.RuntimeEnvironment(value,protected_roots=protected)
        with self.assertRaisesRegex(ValueError,'system temp'):runtime.RuntimeEnvironment(declaration(ROOT/'new-runtime'))
        self.assertFalse((ROOT/'new-runtime').exists())

    def test_strict_declarations_and_aliases(self):
        for changes in (dict(maxEntries=True),dict(maxEntries=4097),dict(maxDepth=0),dict(maxDepth=33),dict(maxRetainedBytes=-1),
                        dict(root=str(self.root/'bad.')),dict(root=str(self.root/'NUL')),dict(extra=0),dict(version='bad')):
            with self.subTest(changes=changes),self.assertRaises(ValueError):runtime.declaration(self.value|changes)

    def test_link_and_metadata_failure_remain_unknown(self):
        self.profile.prepare()
        with patch.object(runtime.os,'scandir',side_effect=PermissionError('literal metadata')):
            with self.assertRaises(PermissionError):self.profile.sample()
        self.assertIsNone(self.profile.snapshot()['lastSample'])
        with self.assertRaisesRegex(ValueError,'failed'):self.profile.sample()


    def test_disappeared_destination_cannot_report_empty_success(self):
        self.profile.prepare();(self.profile.root/'temp').rmdir()
        with self.assertRaisesRegex(ValueError,'destination disappeared'):self.profile.sample()
        self.assertIsNone(self.profile.snapshot()['lastSample'])

    def test_reparse_parent_rejected_before_directory_enumeration(self):
        self.profile.prepare();original=work.unlinked
        def checked(path):
            if path==self.profile.root/'temp':raise ValueError('Linked worker path')
            return original(path)
        with patch.object(work,'unlinked',side_effect=checked),patch.object(runtime.os,'scandir') as scan:
            with self.assertRaisesRegex(ValueError,'Linked'):self.profile.sample()
        scan.assert_not_called()

    def test_mutated_sample_rejects_coverage_and_size_claims(self):
        self.profile.prepare();original=self.profile.snapshot()
        edits=[lambda v:v.update(transientWritesBounded=True),lambda v:v.update(runtimeWritesBounded=True),
            lambda v:v['lastSample'].update(sampledRetainedBytes=1),lambda v:v['lastSample']['entries'].pop(),
            lambda v:v['lastSample']['entries'][0].update(path='../outside'),lambda v:v['lastSample']['entries'][0].update(bytes=1),
            lambda v:v.update(outcome='FailedOrUnknown')]
        for edit in edits:
            v=copy.deepcopy(original);edit(v)
            with self.assertRaises(ValueError):runtime.observation(v)


class RuntimeMonitor(unittest.TestCase):
    def setUp(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);self.base=Path(temp.name)
        export=os.environ.get('LL_RUNTIME_ENVIRONMENT_EXPORT')
        if export:Path(export).parent.mkdir(parents=True,exist_ok=True)

    def run_monitor(self,code,*,max_bytes=65536,export=None,**kwargs):
        self.script=self.base/'owner.py';self.script.write_text(code,encoding='utf-8')
        destination=Path(export) if export else self.base/'monitor'
        self.monitor=monitor.OwnerProcessMonitor(destination,'a'*64,monitor_accounting=True,
            runtime_environment=declaration(self.base/'runtime',maxRetainedBytes=max_bytes))
        return self.monitor.run(self.script,[],self.base,time.monotonic()+8,**kwargs)

    def test_real_owner_and_descendant_use_frozen_environment(self):
        export=os.environ.get('LL_RUNTIME_ENVIRONMENT_EXPORT')
        code='''import os,sys,subprocess,tempfile,json
from pathlib import Path
assert os.getenv('LL_RUNTIME_TEST_SENTINEL') is None
assert sys.dont_write_bytecode and sys.flags.no_user_site
with tempfile.NamedTemporaryFile(delete=False) as f:f.write(b'literal')
child=subprocess.run([sys.executable,'-B','-c','import os;print(os.environ["TEMP"])'],capture_output=True,text=True,check=True)
assert child.stdout.strip()==os.environ['TEMP']
print(json.dumps(dict(temp=tempfile.gettempdir(),sentinel=os.getenv('LL_RUNTIME_TEST_SENTINEL'))))
'''
        with patch.dict(os.environ,LL_RUNTIME_TEST_SENTINEL='not inherited',PYTHONPATH='not inherited'):
            result=self.run_monitor(code,export=export)
        self.assertEqual(result['exitCode'],0);value=self.monitor.observation
        self.assertEqual(value['observationOutcome'],'Complete');self.assertGreaterEqual(value['processResult']['totalProcesses'],2)
        self.assertEqual(value['processEnvironment'],value['processObservation']['environment'])
        sampled=value['runtimeEnvironment'];self.assertEqual(sampled['outcome'],'Sampled')
        self.assertEqual(sampled['lastSample']['sampledRetainedBytes'],7)
        self.assertFalse(sampled['transientWritesBounded']);self.assertTrue(sampled['retainedForInspection'])
        if export:
            root=Path(export)
            # Monitor's package is already sealed; put the terminal record beside it.
            work.seal_new(root.parent/'runtime-terminal.json',self.monitor.monitor_terminal_observation)
            shutil.copyfile(self.script,root.parent/'literal-owner.py')

    def test_final_sample_catches_fast_writer_and_retains_failed_evidence(self):
        with self.assertRaisesRegex(ValueError,'sampled byte'):
            self.run_monitor('import os;from pathlib import Path;Path(os.environ["TEMP"],"value").write_bytes(b"1234")',max_bytes=3)
        self.assertEqual(self.monitor.observation['processOutcome'],'MonitorFailed')
        self.assertEqual(self.monitor.observation['runtimeEnvironment']['outcome'],'FailedOrUnknown')
        self.assertIsNotNone(self.monitor.manifest_sha256)
        self.assertEqual((self.base/'runtime/temp/value').read_bytes(),b'1234')

    def test_periodic_sample_stops_persistent_over_limit_writer(self):
        with self.assertRaisesRegex(ValueError,'sampled byte'):
            self.run_monitor('import os,time;from pathlib import Path;Path(os.environ["TEMP"],"value").write_bytes(b"1234");time.sleep(30)',max_bytes=3)
        self.assertTrue(self.monitor.process_observation['jobDrained'])
        self.assertEqual(self.monitor.observation['runtimeEnvironment']['outcome'],'FailedOrUnknown')

    def test_short_lived_writes_are_explicitly_unbounded(self):
        # No claim that sampled metadata is a lifetime peak.
        profile=runtime.RuntimeEnvironment(declaration(self.base/'runtime',maxRetainedBytes=0));profile.prepare()
        path=profile.root/'temp/transient';path.write_bytes(b'x'*100);path.unlink()
        self.assertEqual(profile.sample(),0);self.assertFalse(profile.snapshot()['transientWritesBounded'])

    def test_environment_receipt_mismatch_rejects(self):
        original=owned.run
        def run(*args,**kwargs):
            observe=kwargs['observe']
            def changed(value):value['environment']['sha256']='0'*64;observe(value)
            kwargs['observe']=changed;return original(*args,**kwargs)
        with patch.object(owned,'run',side_effect=run),self.assertRaisesRegex(ValueError,'environment observation'):self.run_monitor('pass')

    def test_dotnet_host_starts_with_same_replacement_profile(self):
        host=shutil.which('dotnet');self.assertIsNotNone(host)
        profile=runtime.RuntimeEnvironment(declaration(self.base/'runtime'));profile.prepare();observed=[]
        result=owned.run([host,'--list-runtimes'],self.base,self.base/'dotnet.log',time.monotonic()+8,
                         environment=profile.environment(),observe=observed.append,log_byte_limit=32768)
        self.assertEqual(result['exitCode'],0);self.assertIn('Microsoft.NETCore.App',(self.base/'dotnet.log').read_text())
        self.assertEqual(observed[0]['environment'],owned.environment_block(profile.environment())[1]);profile.sample()

    def test_four_phase_routed_owner_completes_under_profile(self):
        spec=importlib.util.spec_from_file_location('runtime_routed_owner_fixture',ROOT/'build/test-proposal-owner-process.py')
        fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
        case=fixture.OwnerProcess();case.setUp();self.addCleanup(case.doCleanups)
        export=os.environ.get('LL_RUNTIME_ENVIRONMENT_EXPORT')
        if export:
            case.base=Path(export).parent/'routed-owner';case.base.mkdir()
        original=monitor.OwnerProcessMonitor
        def create(root,request,**kwargs):
            return original(root,request,runtime_environment=declaration(self.base/'runtime'),**kwargs)
        with patch.object(monitor,'OwnerProcessMonitor',side_effect=create):observer,result=case.routed('success')
        value=case.verify(observer)
        self.assertEqual(result['exitCode'],0,(observer.root/'console.log').read_text())
        self.assertEqual(value['runtimeEnvironment']['outcome'],'Sampled')
        self.assertGreaterEqual(result['totalProcesses'],5)
        self.assertEqual(work.strict((case.base/'owner/terminal-observation.json').read_bytes())['outcome'],'Complete')
        if export:
            work.seal_new(case.base/'fixture.json',dict(scientificWorkersRouted=True,actualCombat=0,productionEntropyDraws=0,
                scientificReservations=0,nativeEncounterPreparations=0,wholeProcessCoverage=False,usableForAdmission=False))
            work.seal_new(case.base/'files.json',{p.relative_to(case.base).as_posix():monitor.sha(p) for p in sorted(case.base.rglob('*')) if p.is_file()})


if __name__=='__main__':unittest.main()
