"""Bounded terminal persistence; literal jobs only, with no scientific admission."""
import importlib.util
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import proposal_monitor_io as budget
import proposal_owner_process as monitor
import proposal_runtime_environment as runtime
import proposal_work_accounting as work
import bounded_windows_process as owned
spec = importlib.util.spec_from_file_location('terminal_fixture',ROOT/'build/test-proposal-monitor-io.py')
fixture = importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)


def declaration(root, **changes):
    value=fixture.limits(version=budget.TERMINAL_VERSION,terminalRoot=str(root),maxTerminalBytes=65536,maxTotalWriteBytes=131072)
    value.update(changes)
    return value


class Terminal(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.base=Path(temporary.name);self.root=self.base/'terminal'
        self.script=self.base/'owner.py';self.script.write_text('print("literal")',encoding='utf-8')
        self.make()

    def make(self, **changes):
        self.monitor=monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=True,
                                                monitor_io_budget=declaration(self.root,**changes))

    def run_monitor(self, **options):
        return self.monitor.run(self.script,[],self.base,time.monotonic()+10,**options)

    def test_v1_rejects_terminal_extension(self):
        with self.assertRaisesRegex(ValueError,'Invalid monitor I/O declaration'):
            budget.MonitorIO(dict(fixture.limits(),terminalRoot=str(self.root),maxTerminalBytes=1))

    def test_v2_requires_exact_declaration_and_integer_terminal_limit(self):
        values=[]
        for missing in ('terminalRoot','maxTerminalBytes'):
            value=declaration(self.root);value.pop(missing);values.append(value)
        values.append(dict(declaration(self.root),extra=1))
        for number in (True,-1,1.5,None,2**63):values.append(declaration(self.root,maxTerminalBytes=number))
        for value in values:
            with self.subTest(value=value),self.assertRaises(ValueError):budget.MonitorIO(value)

    def test_root_requires_canonical_absolute_string(self):
        for root in (None,1,'relative','',str(self.base/'a')+'\\..\\terminal'):
            with self.subTest(root=root),self.assertRaises(ValueError):
                budget.MonitorIO(declaration(self.root,terminalRoot=root))

    def test_v2_declaration_is_copied_and_limits_are_explicit(self):
        declared=declaration(self.root);io=budget.MonitorIO(declared);declared['maxTerminalBytes']=0
        self.assertEqual(io.limits['maxTerminalBytes'],65536)
        self.assertEqual((io.max_hashes,io.max_inventories),(12,4))
        self.assertEqual(set(io.publication_limits),{'owner-process.json','files.json','monitor-call.json'})

    def test_real_terminal_file_keeps_earlier_boundary_and_separate_publication_result(self):
        result=self.run_monitor();self.assertEqual(result['exitCode'],0)
        saved=work.strict((self.root/'monitor-call.json').read_bytes())
        self.assertEqual(saved,self.monitor.monitor_terminal_observation)
        self.assertEqual({p.name for p in self.root.iterdir()},{'monitor-call.json'})
        self.assertEqual({p.name for p in self.monitor.root.iterdir()},{'owner-process.json','console.log','files.json'})
        manifest=work.strict((self.monitor.root/'files.json').read_bytes())
        for name,pin in manifest.items():self.assertEqual(monitor.sha(self.monitor.root/name),pin)
        value=self.monitor.terminal_publication_observation;io=value['monitorIO']
        self.assertEqual(value['outcome'],'Verified');self.assertEqual(value['sha256'],monitor.sha(self.root/'monitor-call.json'))
        self.assertEqual((saved['monitorIO']['hashCompleted'],io['hashCompleted']),(10,11))
        self.assertEqual((saved['monitorIO']['inventoryAttempts'],io['inventoryAttempts']),(2,4))
        self.assertEqual(io['inventoryEntries'],5)
        self.assertEqual(io['acceptedWriteBytes'],sum((self.root/name if name=='monitor-call.json' else self.monitor.root/name).stat().st_size for name in io['publications']))
        self.assertLessEqual(io['reservedWriteBytes'],declaration(self.root)['maxTotalWriteBytes'])
        self.assertLessEqual(io['reservedReadBytes'],declaration(self.root)['maxTotalReadBytes'])
        self.assertTrue(saved['terminalObservationPersistenceExcluded']);self.assertTrue(value['publicationResultPersistenceExcluded'])
        self.assertTrue(saved['monitorIO']['terminalObservationPersistenceExcluded'])
        self.assertFalse(io['terminalObservationPersistenceExcluded']);self.assertTrue(io['snapshotPersistenceExcluded'])
        self.assertTrue(value['callerConsoleAndExitExcluded'])
        for key in ('wholeProcessCoverage','usableForAdmission','wallClockBounded','memoryBounded','externalMutationBounded'):self.assertFalse(value[key])

    def test_existing_terminal_root_is_rejected_before_process(self):
        self.root.mkdir();(self.root/'keep').write_bytes(b'keep')
        with patch.object(owned,'run') as launch,self.assertRaisesRegex(ValueError,'Fresh terminal'):self.run_monitor()
        launch.assert_not_called();self.assertEqual((self.root/'keep').read_bytes(),b'keep')
        self.assertFalse(self.monitor.root.exists());self.assertIsNone(self.monitor.terminal_publication_observation)

    def test_missing_parent_is_rejected_before_process(self):
        self.make(terminalRoot=str(self.base/'missing/terminal'))
        with patch.object(owned,'run') as launch,self.assertRaisesRegex(ValueError,'Fresh terminal'):self.run_monitor()
        launch.assert_not_called();self.assertFalse((self.base/'missing').exists())

    def test_overlap_with_monitor_root_is_rejected_before_creation(self):
        self.make(terminalRoot=str(self.monitor.root))
        with patch.object(owned,'run') as launch,self.assertRaisesRegex(ValueError,'disjoint'):self.run_monitor()
        launch.assert_not_called();self.assertFalse(self.monitor.root.exists())

    def test_protected_root_generator_is_checked_for_terminal_too(self):
        protected=self.base/'protected';protected.mkdir();self.make(terminalRoot=str(protected/'terminal'))
        with patch.object(owned,'run') as launch,self.assertRaisesRegex(ValueError,'disjoint'):
            self.run_monitor(protected_roots=(p for p in [protected]))
        launch.assert_not_called();self.assertFalse((protected/'terminal').exists())

    def test_runtime_directory_must_be_disjoint_from_terminal(self):
        profile=dict(version=runtime.VERSION,root=str(self.root),maxEntries=32,maxDepth=4,maxRetainedBytes=100)
        self.monitor=monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=True,
            monitor_io_budget=declaration(self.root),runtime_environment=profile)
        with patch.object(owned,'run') as launch,self.assertRaises(ValueError):self.run_monitor()
        launch.assert_not_called();self.assertFalse(self.root.exists())

    def test_terminal_creation_failure_is_not_retried(self):
        original=Path.mkdir;error=OSError('terminal mkdir failed');attempts=[]
        def mkdir(path,*args,**kwargs):
            if path==self.root:attempts.append(path);raise error
            return original(path,*args,**kwargs)
        with patch.object(Path,'mkdir',mkdir),patch.object(owned,'run') as launch,self.assertRaises(OSError) as caught:self.run_monitor()
        self.assertIs(caught.exception,error);launch.assert_not_called();self.assertEqual(len(attempts),1)
        self.assertIsNone(self.monitor.terminal_publication_observation)

    def test_terminal_byte_cap_fails_before_file_open(self):
        self.make(maxTerminalBytes=1)
        with self.assertRaisesRegex(ValueError,'publication byte allowance'):self.run_monitor()
        self.assertEqual(list(self.root.iterdir()),[])
        self.assertEqual(self.monitor.terminal_publication_observation['outcome'],'Failed')
        self.assertTrue(self.monitor.monitor_terminal_observation['publicationVerified'])
        self.assertFalse(self.monitor.monitor_io.publications['monitor-call.json']['openAttempted'])

    def test_terminal_writes_share_original_total_allowance(self):
        original=self.monitor._persist_terminal
        def persist():
            self.monitor.monitor_io.write_reserved=self.monitor.monitor_io.limits['maxTotalWriteBytes']
            return original()
        with patch.object(self.monitor,'_persist_terminal',persist),self.assertRaisesRegex(ValueError,'publication byte allowance'):self.run_monitor()
        self.assertFalse((self.root/'monitor-call.json').exists())

    def test_terminal_verification_shares_original_read_allowance(self):
        original=self.monitor._sha
        def sha(path):
            if Path(path)==self.root/'monitor-call.json':self.monitor.monitor_io.read_reserved=self.monitor.monitor_io.limits['maxTotalReadBytes']
            return original(path)
        with patch.object(self.monitor,'_sha',sha),self.assertRaisesRegex(ValueError,'total read allowance'):self.run_monitor()
        self.assertTrue((self.root/'monitor-call.json').exists())
        self.assertIsNone(self.monitor.terminal_publication_observation['sha256'])

    def test_terminal_persistence_uses_remaining_deadline(self):
        original=self.monitor._persist_terminal
        def persist():self.monitor.deadline=0;return original()
        with patch.object(self.monitor,'_persist_terminal',persist),self.assertRaisesRegex(ValueError,'elapsed allowance'):self.run_monitor()
        self.assertFalse((self.root/'monitor-call.json').exists())

    def test_child_failure_record_can_be_persisted_without_success_claim(self):
        self.script.write_text('raise SystemExit(7)',encoding='utf-8')
        self.assertEqual(self.run_monitor()['exitCode'],7)
        saved=work.strict((self.root/'monitor-call.json').read_bytes())
        self.assertEqual(saved['ownerObservationOutcome'],'Complete')
        self.assertEqual(self.monitor.observation['processOutcome'],'ExitedNonzero')
        self.assertFalse(saved['usableForAdmission']);self.assertEqual(self.monitor.terminal_publication_observation['outcome'],'Verified')

    def test_original_process_error_survives_terminal_failure(self):
        self.make(maxTerminalBytes=0);error=OSError('original process failure')
        with patch.object(owned,'run',side_effect=error),self.assertRaises(OSError) as caught:self.run_monitor()
        self.assertIs(caught.exception,error);self.assertIn('terminal persistence failed',' '.join(error.__notes__))
        self.assertEqual(self.monitor.monitor_terminal_observation['outcome'],'Failed')

    def test_poisoned_main_budget_cannot_be_reset_for_terminal(self):
        self.make(maxObservationBytes=0)
        with self.assertRaisesRegex(ValueError,'publication byte allowance') as caught:self.run_monitor()
        self.assertIn('cannot be reused',' '.join(caught.exception.__notes__))
        self.assertFalse((self.root/'monitor-call.json').exists());self.assertTrue(self.monitor.monitor_io.failed)

    def test_snapshot_failure_does_not_publish_a_missing_record(self):
        with patch.object(self.monitor,'_monitor_snapshot',side_effect=OSError('snapshot failed')),self.assertRaisesRegex(OSError,'snapshot failed'):self.run_monitor()
        self.assertFalse((self.root/'monitor-call.json').exists());self.assertIsNone(self.monitor.terminal_publication_observation)

    def test_existing_terminal_member_is_not_overwritten(self):
        original=self.monitor._persist_terminal
        def persist():(self.root/'monitor-call.json').write_bytes(b'foreign');return original()
        with patch.object(self.monitor,'_persist_terminal',persist),self.assertRaisesRegex(ValueError,'Untracked'):self.run_monitor()
        self.assertEqual((self.root/'monitor-call.json').read_bytes(),b'foreign')

    def test_replaced_terminal_directory_is_rejected_before_write(self):
        original=self.monitor._persist_terminal
        def persist():
            self.root.rename(self.base/'retained-original-directory')
            self.root.mkdir()
            return original()
        with patch.object(self.monitor,'_persist_terminal',persist),self.assertRaisesRegex(ValueError,'Replaced monitor terminal'):
            self.run_monitor()
        self.assertFalse((self.root/'monitor-call.json').exists())
        self.assertEqual(self.monitor.terminal_publication_observation['outcome'],'Failed')

    def fault_open(self,mode,error=None,close_error=None):
        original=Path.open
        def opening(path,*args,**kwargs):
            stream=original(path,*args,**kwargs)
            if path==self.root/'monitor-call.json' and args and args[0]=='xb':
                return fixture.fixture.FaultFile(stream,mode,error,close_error)
            return stream
        return patch.object(Path,'open',opening)

    def test_short_terminal_writes_complete_within_one_reservation(self):
        with self.fault_open('short'):self.assertEqual(self.run_monitor()['exitCode'],0)
        record=self.monitor.monitor_io.publications['monitor-call.json']
        self.assertEqual(record['reservedBytes'],record['acceptedBytes'])
        self.assertEqual(self.monitor.terminal_publication_observation['outcome'],'Verified')

    def test_partial_write_and_close_failure_preserve_first_error(self):
        error=OSError('partial terminal');close=OSError('secondary close')
        with self.fault_open('partial-fail',error,close),self.assertRaises(OSError) as caught:self.run_monitor()
        self.assertIs(caught.exception,error);self.assertIn('secondary close',' '.join(error.__notes__))
        self.assertEqual((self.root/'monitor-call.json').stat().st_size,3)
        self.assertTrue(self.monitor.monitor_io.write_progress_unknown)

    def test_zero_terminal_write_fails_without_retry(self):
        with self.fault_open('zero'),self.assertRaisesRegex(ValueError,'Incomplete'):self.run_monitor()
        self.assertTrue(self.monitor.monitor_io.write_progress_unknown)
        self.assertIsNone(self.monitor.terminal_publication_observation['sha256'])

    def test_result_construction_failure_cannot_mask_terminal_write_error(self):
        original=self.monitor.monitor_io.snapshot;error=OSError('first terminal write error')
        def snapshot():
            if self.monitor._terminal_attempted:raise OSError('secondary result failure')
            return original()
        with self.fault_open('partial-fail',error),patch.object(self.monitor.monitor_io,'snapshot',snapshot), \
                self.assertRaises(OSError) as caught:self.run_monitor()
        self.assertIs(caught.exception,error);self.assertIn('secondary result failure',' '.join(error.__notes__))
        self.assertIsNone(self.monitor.terminal_publication_observation)
        self.assertEqual((self.root/'monitor-call.json').stat().st_size,3)

    def test_terminal_flush_failure_prevents_verification(self):
        error=OSError('terminal flush failed')
        with self.fault_open('flush',error),self.assertRaises(OSError) as caught:self.run_monitor()
        self.assertIs(caught.exception,error);self.assertIsNone(self.monitor.terminal_publication_observation['sha256'])

    def test_modified_terminal_content_fails_readback(self):
        original=self.monitor._seal
        def seal(name,value,**kwargs):
            pin=original(name,value,**kwargs)
            if name=='monitor-call.json':
                path=self.root/name;raw=bytearray(path.read_bytes());raw[0]=ord('[');path.write_bytes(raw)
            return pin
        with patch.object(self.monitor,'_seal',seal),self.assertRaisesRegex(ValueError,'Changed monitor terminal'):self.run_monitor()
        self.assertIsNone(self.monitor.terminal_publication_observation['sha256'])

    def test_terminal_cannot_be_published_twice(self):
        self.run_monitor();pin=monitor.sha(self.root/'monitor-call.json')
        with self.assertRaisesRegex(ValueError,'cannot run twice'):self.monitor._persist_terminal()
        self.assertEqual(monitor.sha(self.root/'monitor-call.json'),pin)

    def test_runtime_profile_and_four_phase_owner_use_all_twelve_hashes(self):
        export=os.environ.get('LL_MONITOR_TERMINAL_EXPORT');base=Path(export) if export else self.base/'routed';base.mkdir()
        source=fixture.fixture.f.fixture.Supervisor();source.setUp();self.addCleanup(source.doCleanups)
        supervisor=source.prepare(base/'owner')
        profile=dict(version=runtime.VERSION,root=str(self.base/'runtime'),maxEntries=128,maxDepth=8,maxRetainedBytes=1048576)
        instance=monitor.OwnerProcessMonitor(base/'monitor',monitor.sha(source.package/'request.json'),monitor_accounting=True,
            monitor_io_budget=declaration(base/'terminal'),runtime_environment=profile)
        result=instance.run(fixture.fixture.f.__file__,['--fixture-owner',str(base/'owner'),'success'],ROOT,time.monotonic()+20,
            protected_roots=[source.package,source.output.parent,supervisor.root])
        self.assertEqual(result['exitCode'],0,(instance.root/'console.log').read_text())
        self.assertEqual(instance.terminal_publication_observation['outcome'],'Verified')
        self.assertEqual(instance.terminal_publication_observation['monitorIO']['hashCompleted'],12)
        saved=work.strict((base/'terminal/monitor-call.json').read_bytes())
        self.assertEqual(saved,instance.monitor_terminal_observation)
        self.assertEqual(saved['monitorIO']['hashCompleted'],11)
        self.assertEqual(len(list((base/'owner').glob('nested-*.json'))),4)
        if export:
            work.seal_new(base/'publication-result.json',instance.terminal_publication_observation)
            work.seal_new(base/'fixture.json',dict(fixtureOnly=True,scientificWorkersRouted=True,realNestedLiteralJobs=4,
                actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
                publicationResultPersistedByTestOutsideBudget=True,usableForAdmission=False))
            work.seal_new(base/'files.json',{p.relative_to(base).as_posix():monitor.sha(p) for p in sorted(base.rglob('*')) if p.is_file()})


if __name__=='__main__':unittest.main()
