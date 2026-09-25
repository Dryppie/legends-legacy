"""Literal selected monitor I/O contracts; no scientific launch or timing sample."""
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_monitor_io as budget
import proposal_owner_process as monitor
import proposal_work_accounting as work
import proposal_runtime_environment as runtime
import bounded_windows_process as owned
spec = importlib.util.spec_from_file_location('monitor_io_fixture', ROOT/'build/test-proposal-monitor-boundary.py')
fixture = importlib.util.module_from_spec(spec); spec.loader.exec_module(fixture)


def limits(**changes):
    value = dict(version=budget.VERSION, maxFileReadBytes=16*1024*1024, maxTotalReadBytes=32*1024*1024,
                 maxConsoleBytes=4096, maxObservationBytes=65536, maxManifestBytes=1024, maxTotalWriteBytes=66560)
    value.update(changes)
    return value


class ReadFault:
    def __init__(self, stream, mode, error, close_error=None):
        self.stream, self.mode, self.error, self.close_error = stream, mode, error, close_error
        self.closed = False
    def fileno(self): return self.stream.fileno()
    def read(self, size):
        if self.mode == 'raise': raise self.error
        if self.mode == 'short': return self.stream.read(max(1, size//2))
        if self.mode == 'grow' and self.stream.tell() == 3: return b'x'
        if self.mode == 'invalid': return None
        return self.stream.read(size)
    def close(self):
        self.closed = True
        self.stream.close()
        if self.close_error is not None: raise self.close_error


class Budget(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(); self.addCleanup(temporary.cleanup)
        self.base = Path(temporary.name)
        self.path = self.base/'data'; self.path.write_bytes(b'abc')
        self.io = budget.MonitorIO(limits())

    def fault(self, mode, error=None, close_error=None):
        original = Path.open
        def opening(path, *args, **kwargs):
            stream = original(path, *args, **kwargs)
            if path == self.path and args and args[0] == 'rb':
                self.reader = ReadFault(stream, mode, error or OSError('read failure'), close_error)
                return self.reader
            return stream
        return patch.object(Path, 'open', opening)

    def test_exact_declaration_and_strict_integer_limits(self):
        invalid = [None, {}, limits(version='wrong'), dict(limits(), extra=0)]
        for name in budget.LIMITS:
            for value in (True, -1, 1.0, None, 2**63): invalid.append(limits(**{name:value}))
        for value in invalid:
            with self.subTest(value=value), self.assertRaises(ValueError): budget.MonitorIO(value)

    def test_declaration_and_snapshot_are_copied(self):
        declared = limits(); io = budget.MonitorIO(declared); declared['maxConsoleBytes'] = 0
        io.publication('files.json', {}); value = io.snapshot()
        value['declaration']['maxConsoleBytes'] = 0; value['publications']['files.json']['reservedBytes'] = 0
        self.assertEqual(io.limits['maxConsoleBytes'],4096)
        self.assertEqual(io.snapshot()['publications']['files.json']['reservedBytes'],2)
        for name in ('wallClockBounded','memoryBounded','externalMutationBounded','wholeProcessCoverage','usableForAdmission'):
            self.assertFalse(value[name])

    def test_exact_hash_reserves_eof_and_preserves_accounting(self):
        self.io = budget.MonitorIO(limits(maxFileReadBytes=4,maxTotalReadBytes=4))
        counters = work.OwnerFileCounters()
        self.assertEqual(self.io.sha(self.path,counters),monitor.sha(self.path))
        value = self.io.snapshot()
        self.assertEqual((value['reservedReadBytes'],value['requestedReadBytes'],value['acceptedReadBytes']),(4,4,3))
        self.assertEqual(counters.values['applicationReadBytes.other'],3)

    def test_per_file_exhaustion_before_open(self):
        self.io = budget.MonitorIO(limits(maxFileReadBytes=3))
        with patch.object(Path,'open') as opening, self.assertRaisesRegex(ValueError,'file read allowance'):
            self.io.sha(self.path,None)
        opening.assert_not_called(); self.assertTrue(self.io.failed)

    def test_total_reads_do_not_refund_eof_or_previous_hash(self):
        self.io = budget.MonitorIO(limits(maxTotalReadBytes=7))
        self.io.sha(self.path,None)
        with self.assertRaisesRegex(ValueError,'total read allowance'): self.io.sha(self.path,None)
        self.assertEqual(self.io.read_reserved,4)
        with self.assertRaisesRegex(ValueError,'cannot be reused'): self.io.sha(self.path,None)

    def test_empty_hash_still_needs_one_byte_reservation(self):
        self.path.write_bytes(b''); self.io = budget.MonitorIO(limits(maxTotalReadBytes=0))
        with self.assertRaisesRegex(ValueError,'total read allowance'): self.io.sha(self.path,None)

    def test_hash_count_is_finite(self):
        for _ in range(budget.MAX_HASHES): self.io.sha(self.path,None)
        with self.assertRaisesRegex(ValueError,'hash attempts'): self.io.sha(self.path,None)
        self.assertEqual(self.io.hash_attempts,11)

    def test_read_error_retains_reservation_and_closes(self):
        error = OSError('first read'); close = OSError('secondary close')
        with self.fault('raise',error,close), self.assertRaises(OSError) as caught: self.io.sha(self.path,None)
        self.assertIs(caught.exception,error); self.assertTrue(self.reader.closed)
        self.assertTrue(self.io.read_progress_unknown); self.assertEqual(self.io.read_reserved,4)
        self.assertEqual(error.__notes__, ['Monitor hash close failed; secondary details omitted.'])

    def test_short_read_is_not_retried_beyond_reservation(self):
        with self.fault('short'), self.assertRaisesRegex(ValueError,'Short or shrinking'): self.io.sha(self.path,None)
        self.assertTrue(self.reader.closed); self.assertEqual(self.io.read_calls,1)
        self.assertLessEqual(self.io.read_requested,self.io.read_reserved)

    def test_growth_is_detected_by_reserved_probe(self):
        with self.fault('grow'), self.assertRaisesRegex(ValueError,'Growing'): self.io.sha(self.path,None)
        self.assertTrue(self.reader.closed); self.assertEqual(self.io.read_requested,4)

    def test_invalid_read_progress_is_unknown(self):
        with self.fault('invalid'), self.assertRaisesRegex(ValueError,'Invalid monitor hash read'): self.io.sha(self.path,None)
        self.assertTrue(self.io.read_progress_unknown); self.assertTrue(self.reader.closed)

    def test_hash_close_failure_prevents_completion(self):
        with self.fault('normal',close_error=OSError('close failure')), self.assertRaisesRegex(OSError,'close failure'):
            self.io.sha(self.path,None)
        self.assertEqual(self.io.hash_completed,0); self.assertTrue(self.io.failed)

    def test_nonregular_hash_rejected_before_open(self):
        with patch.object(Path,'open') as opening, self.assertRaisesRegex(ValueError,'Nonregular'):
            self.io.sha(self.base,None)
        opening.assert_not_called()

    def test_canonical_unicode_output_at_exact_limit(self):
        value = {'z':'æ😀','a':[1,None]}; raw = work.canonical(value)
        self.io = budget.MonitorIO(limits(maxObservationBytes=len(raw),maxTotalWriteBytes=len(raw)))
        self.assertEqual(self.io.publication('owner-process.json',value),raw)
        self.assertEqual(self.io.write_reserved,len(raw))

    def test_oversized_output_and_total_exhaustion_poison_scope(self):
        for declaration in (limits(maxObservationBytes=1), limits(maxTotalWriteBytes=1)):
            io = budget.MonitorIO(declaration)
            with self.assertRaisesRegex(ValueError,'publication byte allowance'): io.publication('owner-process.json',{})
            self.assertFalse(io.publications['owner-process.json']['openAttempted'])
            with self.assertRaisesRegex(ValueError,'cannot be reused'): io.publication('files.json',{})

    def test_publication_names_attempts_and_cumulative_budget(self):
        with self.assertRaisesRegex(ValueError,'Undeclared'): self.io.publication('other',{})
        io = budget.MonitorIO(limits(maxTotalWriteBytes=3)); io.publication('owner-process.json',{})
        with self.assertRaisesRegex(ValueError,'byte allowance'): io.publication('files.json',{})
        io = budget.MonitorIO(limits()); io.publication('files.json',{}); io.opening('files.json')
        with self.assertRaisesRegex(ValueError,'Repeated'): io.opening('files.json')
        io = budget.MonitorIO(limits()); io.publication('files.json',{})
        with self.assertRaisesRegex(ValueError,'repeated'): io.publication('files.json',{})

    def test_nonfinite_serialization_fails_before_open(self):
        with self.assertRaises(ValueError): self.io.publication('files.json',{'bad':float('nan')})
        self.assertFalse(self.io.publications['files.json']['openAttempted'])

    def test_inventory_stops_at_one_unexpected_entry(self):
        with self.assertRaisesRegex(ValueError,'Untracked'): self.io.inventory(self.base,[])
        self.assertEqual(self.io.inventory_entries,1)

    def test_inventory_has_only_two_attempts(self):
        self.path.unlink()
        for _ in range(2): self.assertEqual(self.io.inventory(self.base,[]),set())
        with self.assertRaisesRegex(ValueError,'inventories exhausted'): self.io.inventory(self.base,[])


class Monitor(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(); self.addCleanup(temporary.cleanup)
        self.base = Path(temporary.name)
        self.script = self.base/'owner.py'; self.script.write_text('print("literal")',encoding='utf-8')
        self.monitor = monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=True,monitor_io_budget=limits())

    def run_monitor(self, **options):
        return self.monitor.run(self.script,[],self.base,time.monotonic()+10,**options)

    def test_budget_requires_terminal_accounting(self):
        with self.assertRaisesRegex(ValueError,'requires terminal accounting'):
            monitor.OwnerProcessMonitor(self.base/'other','a'*64,monitor_io_budget=limits())

    def test_real_owner_selected_io_is_bounded_and_manifest_verified(self):
        self.assertEqual(self.run_monitor()['exitCode'],0)
        value = self.monitor.monitor_terminal_observation; io = value['monitorIO']
        self.assertTrue(value['publicationVerified']); self.assertFalse(io['failed'])
        self.assertEqual(io['hashCompleted'],10); self.assertEqual(io['inventoryAttempts'],2)
        self.assertLessEqual(io['requestedReadBytes'],io['reservedReadBytes'])
        self.assertLessEqual(io['reservedReadBytes'],limits()['maxTotalReadBytes'])
        self.assertEqual(io['acceptedWriteBytes'],io['reservedWriteBytes'])
        files = work.strict((self.monitor.root/'files.json').read_bytes())
        for name,pin in files.items(): self.assertEqual(monitor.sha(self.monitor.root/name),pin)
        self.assertEqual(self.monitor.observation['monitorIOModuleSha256'],monitor.sha(budget.__file__))
        self.assertEqual(self.monitor.observation['processObservation']['logCapture']['maxBytes'],4096)
        with self.assertRaisesRegex(ValueError,'cannot run twice'): self.run_monitor()

    def test_preparation_budget_failure_creates_no_package_or_process(self):
        self.monitor = monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=True,
                                                  monitor_io_budget=limits(maxTotalReadBytes=0))
        with patch.object(owned,'run') as launch, self.assertRaisesRegex(ValueError,'read allowance'): self.run_monitor()
        launch.assert_not_called(); self.assertFalse(self.monitor.root.exists())
        self.assertEqual(self.monitor.monitor_terminal_observation['failedPhase'],'preparation')

    def test_console_overflow_retains_capped_file_and_drains_owner(self):
        self.script.write_text('print("x"*10000,flush=True)',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'log byte limit'): self.run_monitor()
        raw = self.monitor.process_observation
        self.assertTrue(raw['jobDrained']); self.assertTrue(raw['logCapture']['exceeded'])
        self.assertEqual((self.monitor.root/'console.log').stat().st_size,4096)
        self.assertTrue(self.monitor.monitor_terminal_observation['publicationVerified'])
        self.assertEqual(self.monitor.monitor_terminal_observation['outcome'],'Failed')

    def test_observation_cap_fails_before_exclusive_open(self):
        self.monitor = monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=True,
                                                  monitor_io_budget=limits(maxObservationBytes=1))
        with self.assertRaisesRegex(ValueError,'publication byte allowance'): self.run_monitor()
        self.assertFalse((self.monitor.root/'owner-process.json').exists())
        self.assertTrue(self.monitor.process_observation['jobDrained'])
        self.assertTrue(self.monitor.monitor_terminal_observation['monitorIO']['failed'])

    def test_manifest_cap_preserves_first_receipt(self):
        self.monitor = monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=True,
                                                  monitor_io_budget=limits(maxManifestBytes=1))
        with self.assertRaisesRegex(ValueError,'publication byte allowance'): self.run_monitor()
        self.assertTrue((self.monitor.root/'owner-process.json').is_file())
        self.assertFalse((self.monitor.root/'files.json').exists()); self.assertIsNone(self.monitor.manifest_sha256)

    def fault_open(self, mode, error, close_error=None):
        original = Path.open
        def opening(path,*args,**kwargs):
            stream = original(path,*args,**kwargs)
            if path == self.monitor.root/'owner-process.json' and args and args[0] == 'xb':
                return fixture.FaultFile(stream,mode,error,close_error)
            return stream
        return patch.object(Path,'open',opening)

    def test_short_writes_consume_only_the_original_reservation(self):
        with self.fault_open('short',None): self.assertEqual(self.run_monitor()['exitCode'],0)
        io = self.monitor.monitor_terminal_observation['monitorIO']
        self.assertEqual(io['reservedWriteBytes'],io['acceptedWriteBytes'])
        self.assertGreater(io['writeCalls'],2)

    def test_partial_write_and_close_errors_preserve_original_and_unknown_progress(self):
        error = OSError('partial first'); close = OSError('close second')
        with self.fault_open('partial-fail',error,close), self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error); self.assertIn('close second',' '.join(error.__notes__))
        io = self.monitor.monitor_terminal_observation['monitorIO']
        self.assertTrue(io['failed']); self.assertTrue(io['failedWriteProgressUnknown'])
        self.assertEqual(io['acceptedWriteBytes'],0); self.assertGreater(io['reservedWriteBytes'],0)
        self.assertEqual((self.monitor.root/'owner-process.json').stat().st_size,3)

    def test_zero_write_fails_without_retry(self):
        with self.fault_open('zero',None), self.assertRaisesRegex(ValueError,'Incomplete'): self.run_monitor()
        self.assertEqual(self.monitor.monitor_io.write_calls,1)
        self.assertTrue(self.monitor.monitor_io.write_progress_unknown)

    def test_flush_failure_poisons_scope_after_closing(self):
        error = OSError('flush failed')
        with self.fault_open('flush',error), self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error); self.assertTrue(self.monitor.monitor_io.failed)
        with (self.monitor.root/'owner-process.json').open('ab') as stream: stream.write(b'')

    def test_sync_failure_poisons_scope_after_closing(self):
        original = os.fsync
        def sync(descriptor):
            if self.monitor._monitor_phase == 'publication': raise OSError('sync failed')
            return original(descriptor)
        with patch.object(os,'fsync',sync), self.assertRaisesRegex(OSError,'sync failed'): self.run_monitor()
        self.assertTrue(self.monitor.monitor_io.failed)
        self.assertFalse((self.monitor.root/'files.json').exists())

    def test_accounting_failure_cannot_skip_mandatory_close(self):
        original_open, original_add = Path.open, work.OwnerFileCounters.add
        streams = []
        def opening(path,*args,**kwargs):
            stream = original_open(path,*args,**kwargs)
            if path == self.monitor.root/'owner-process.json' and args and args[0] == 'xb': streams.append(stream)
            return stream
        def add(instance,name,*args):
            if name == 'publicationCloseAttempted': raise OSError('close accounting failed')
            return original_add(instance,name,*args)
        with patch.object(Path,'open',opening), patch.object(work.OwnerFileCounters,'add',add), \
                self.assertRaisesRegex(OSError,'close accounting failed'): self.run_monitor()
        self.assertEqual(len(streams),1); self.assertTrue(streams[0].closed)
        self.assertTrue(self.monitor.monitor_io.failed)

    def test_verification_read_exhaustion_leaves_publication_unverified(self):
        original = self.monitor._phase
        def phase(name):
            original(name)
            if name == 'verification': self.monitor.monitor_io.read_reserved = self.monitor.monitor_io.limits['maxTotalReadBytes']
        with patch.object(self.monitor,'_phase',phase), self.assertRaisesRegex(ValueError,'total read allowance'):
            self.run_monitor()
        self.assertTrue((self.monitor.root/'files.json').is_file()); self.assertIsNone(self.monitor.manifest_sha256)
        self.assertEqual(self.monitor.monitor_terminal_observation['failedPhase'],'verification')

    def test_open_completion_accounting_failure_still_closes_new_stream(self):
        original_open, original_add = Path.open, work.OwnerFileCounters.add
        streams = []
        def opening(path,*args,**kwargs):
            stream = original_open(path,*args,**kwargs)
            if path == self.monitor.root/'owner-process.json' and args and args[0] == 'xb': streams.append(stream)
            return stream
        def add(instance,name,*args):
            if name == 'publicationOpenCompleted': raise OSError('open accounting failed')
            return original_add(instance,name,*args)
        with patch.object(Path,'open',opening), patch.object(work.OwnerFileCounters,'add',add), \
                self.assertRaisesRegex(OSError,'open accounting failed'): self.run_monitor()
        self.assertEqual(len(streams),1); self.assertTrue(streams[0].closed)
        self.assertEqual(self.monitor.monitor_io.write_calls,0); self.assertTrue(self.monitor.monitor_io.failed)

    def test_existing_publication_is_not_overwritten_or_retried(self):
        original = self.monitor._seal
        def seal(name,value):
            if name == 'owner-process.json': (self.monitor.root/name).write_bytes(b'foreign')
            return original(name,value)
        with patch.object(self.monitor,'_seal',seal), self.assertRaises(FileExistsError): self.run_monitor()
        self.assertEqual((self.monitor.root/'owner-process.json').read_bytes(),b'foreign')
        self.assertTrue(self.monitor.monitor_io.publications['owner-process.json']['openAttempted'])
        self.assertTrue(self.monitor.monitor_io.failed)

    def test_original_process_error_survives_publication_exhaustion(self):
        self.monitor = monitor.OwnerProcessMonitor(self.base/'monitor','a'*64,monitor_accounting=True,
                                                  monitor_io_budget=limits(maxObservationBytes=1))
        error = OSError('original process failure')
        with patch.object(owned,'run',side_effect=error), self.assertRaises(OSError) as caught: self.run_monitor()
        self.assertIs(caught.exception,error); self.assertIn('publication byte allowance',' '.join(error.__notes__))

    def test_missing_console_budget_observation_is_not_accepted(self):
        original = owned.run
        def changed(*args,**kwargs):
            observer = kwargs['observe']
            def observe(value):
                value.pop('logCapture'); observer(value)
            kwargs['observe'] = observe
            return original(*args,**kwargs)
        with patch.object(owned,'run',changed), self.assertRaisesRegex(ValueError,'console budget observation'): self.run_monitor()
        self.assertIsNone(self.monitor.process_observation)

    def test_routed_owner_with_runtime_profile_uses_all_eleven_hashes(self):
        export = os.environ.get('LL_MONITOR_IO_EXPORT')
        base = Path(export) if export else self.base/'routed'
        base.mkdir()
        source = fixture.f.fixture.Supervisor(); source.setUp(); self.addCleanup(source.doCleanups)
        supervisor = source.prepare(base/'owner')
        declared = dict(version=runtime.VERSION,root=str(self.base/'runtime'),maxEntries=128,maxDepth=8,maxRetainedBytes=1048576)
        instance = monitor.OwnerProcessMonitor(base/'monitor',monitor.sha(source.package/'request.json'),
            monitor_accounting=True,monitor_io_budget=limits(),runtime_environment=declared)
        result = instance.run(fixture.f.__file__,['--fixture-owner',str(base/'owner'),'success'],ROOT,time.monotonic()+20,
            protected_roots=[source.package,source.output.parent,supervisor.root])
        self.assertEqual(result['exitCode'],0,(instance.root/'console.log').read_text())
        terminal = instance.monitor_terminal_observation
        self.assertEqual(terminal['monitorIO']['hashCompleted'],11); self.assertTrue(terminal['publicationVerified'])
        self.assertEqual(len(list((base/'owner').glob('nested-*.json'))),4)
        self.assertFalse(instance.observation['runtimeEnvironment']['runtimeWritesBounded'])
        if export:
            work.seal_new(base/'monitor-call.json',terminal)
            work.seal_new(base/'fixture.json',dict(fixtureOnly=True,scientificWorkersRouted=True,realNestedLiteralJobs=4,
                actualCombat=0,productionEntropyDraws=0,scientificReservations=0,nativeEncounterPreparations=0,
                terminalPersistenceOutsideBudget=True,usableForAdmission=False))
            work.seal_new(base/'files.json',{p.relative_to(base).as_posix():monitor.sha(p) for p in sorted(base.rglob('*')) if p.is_file()})


if __name__ == '__main__': unittest.main()
