"""Bounded monitor I/O finalizers; literal files/jobs, no scientific study."""
import hashlib
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_monitor_io as budget
import proposal_work_accounting as work


class Hostile(RuntimeError):
    calls = []
    def __str__(self): self.calls.append('str'); raise AssertionError('Unexpected str')
    def __repr__(self): self.calls.append('repr'); raise AssertionError('Unexpected repr')
    def __bool__(self): self.calls.append('bool'); raise AssertionError('Unexpected truthiness')
    def add_note(self, note): self.calls.append('add_note'); raise AssertionError('Unexpected annotation')
    def __getattribute__(self, name):
        if name in ('args', '__dict__', '__notes__'):
            type(self).calls.append(name); raise AssertionError('Unexpected attribute access')
        return super().__getattribute__(name)


def attributes(error): return BaseException.__dict__['__dict__'].__get__(error)


def limits():
    return dict(version=budget.VERSION, maxFileReadBytes=16*1024*1024, maxTotalReadBytes=32*1024*1024,
                maxConsoleBytes=4096, maxObservationBytes=65536, maxManifestBytes=1024, maxTotalWriteBytes=66560)


class ReadFault:
    def __init__(self, stream, error, closing):
        self.stream, self.error, self.closing = stream, error, closing
        self.close_calls = 0
    def fileno(self): return self.stream.fileno()
    def read(self, size):
        if self.error is not None: raise self.error
        return self.stream.read(size)
    def close(self):
        self.close_calls += 1; self.stream.close()
        if self.closing is not None: raise self.closing


class ScanFault:
    def __init__(self, entries, error=None, closing=None, entry=None):
        self.entries, self.error, self.closing, self.entry = entries, error, closing, entry
        self.close_calls = self.next_calls = 0
    def __iter__(self): return self
    def __next__(self):
        self.next_calls += 1
        if self.error is not None: raise self.error
        value = next(self.entries)
        return value if self.entry is None else self.entry
    def close(self):
        self.close_calls += 1; self.entries.close()
        if self.closing is not None: raise self.closing


class Finalizers(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(); self.addCleanup(temporary.cleanup)
        self.base = Path(temporary.name); self.path = self.base/'input.bin'; self.path.write_bytes(b'abc')
        self.empty = self.base/'empty'; self.empty.mkdir()
        self.io = budget.MonitorIO(limits()); self.reader = self.scanner = None; Hostile.calls.clear()

    def read_fault(self, error=None, closing=None, path=None, guard=lambda: True):
        original = Path.open; target = self.path if path is None else path
        def opening(path, *args, **kwargs):
            stream = original(path, *args, **kwargs)
            if path == target and args and args[0] == 'rb' and guard():
                self.reader = ReadFault(stream, error, closing); return self.reader
            return stream
        return patch.object(Path, 'open', opening)

    def scan_fault(self, error=None, closing=None, entry=None):
        original = os.scandir
        def scanning(path):
            self.scanner = ScanFault(original(path), error, closing, entry); return self.scanner
        return patch.object(budget.os, 'scandir', scanning)

    def primary(self, error, action):
        try: action()
        except BaseException as caught: self.assertIs(caught, error)
        else: self.fail('Original exception did not escape')
        self.assertEqual(Hostile.calls, [])

    def assert_hash_failed(self, *, read_unknown=False):
        value = self.io.snapshot()
        self.assertTrue(value['failed']); self.assertEqual(value['hashCompleted'], 0)
        self.assertEqual(value['reservedReadBytes'], 4)
        self.assertEqual(value['failedReadProgressUnknown'], read_unknown)
        self.assertEqual(self.reader.close_calls, 1); self.assertTrue(self.reader.stream.closed)

    def test_read_failure_survives_hostile_close(self):
        primary = Hostile('read failed')
        with self.read_fault(primary, Hostile('close failed')):
            self.primary(primary, lambda: self.io.sha(self.path, None))
        self.assert_hash_failed(read_unknown=True)
        self.assertEqual(attributes(primary)['__notes__'], ['Monitor hash close failed; secondary details omitted.'])

    def test_close_failure_alone_prevents_hash_completion(self):
        primary = Hostile('close failed')
        with self.read_fault(closing=primary): self.primary(primary, lambda: self.io.sha(self.path, None))
        self.assert_hash_failed(); self.assertEqual(self.io.read_accepted, 3)
        self.assertNotIn('__notes__', attributes(primary))

    def test_hash_success_still_closes_and_counts_eof_probe(self):
        with self.read_fault(): result = self.io.sha(self.path, None)
        self.assertEqual(result, hashlib.sha256(b'abc').hexdigest())
        self.assertEqual((self.io.hash_completed, self.io.read_calls, self.io.read_requested, self.io.read_accepted), (1, 2, 4, 3))
        self.assertEqual(self.reader.close_calls, 1); self.assertTrue(self.reader.stream.closed)

    def test_read_counter_failure_survives_close_without_unknown_read_progress(self):
        primary = Hostile('counter failed'); counters = work.OwnerFileCounters()
        with self.read_fault(closing=Hostile('close failed')), patch.object(counters, 'add', side_effect=primary):
            self.primary(primary, lambda: self.io.sha(self.path, counters))
        self.assert_hash_failed(); self.assertEqual(self.io.read_accepted, 3)

    def test_metadata_failure_after_open_still_closes(self):
        primary = Hostile('fstat failed')
        with self.read_fault(closing=Hostile('close failed')), patch.object(budget.os, 'fstat', side_effect=primary):
            self.primary(primary, lambda: self.io.sha(self.path, None))
        self.assert_hash_failed(); self.assertEqual(self.io.read_calls, 0)

    def test_failed_open_retains_reservation_without_closing_unacquired_resource(self):
        primary = Hostile('open failed')
        with patch.object(Path, 'open', side_effect=primary), patch.object(budget, '_close') as close:
            self.primary(primary, lambda: self.io.sha(self.path, None))
        close.assert_not_called(); self.assertTrue(self.io.failed); self.assertEqual(self.io.read_reserved, 4)

    def test_caller_exception_does_not_hide_new_hash_close_failure(self):
        primary = Hostile('close failed'); caller = RuntimeError('unrelated')
        try: raise caller
        except RuntimeError:
            with self.read_fault(closing=primary): self.primary(primary, lambda: self.io.sha(self.path, None))
        self.assert_hash_failed(); self.assertNotIn('__notes__', attributes(caller))

    def test_keyboard_interrupt_survives_close_failure(self):
        primary = KeyboardInterrupt('cancelled')
        with self.read_fault(primary, Hostile('close failed')):
            self.primary(primary, lambda: self.io.sha(self.path, None))
        self.assert_hash_failed(read_unknown=True)

    def test_note_count_never_grows_beyond_eight(self):
        for count in (7, 8, 20):
            with self.subTest(count=count):
                self.io = budget.MonitorIO(limits()); primary = Hostile('read failed'); prior = ['caller']*count
                attributes(primary)['__notes__'] = prior
                with self.read_fault(primary, Hostile('close failed')):
                    self.primary(primary, lambda: self.io.sha(self.path, None))
                self.assertIs(attributes(primary)['__notes__'], prior); self.assertEqual(len(prior), max(count, 8))

    def test_list_subclass_note_hooks_are_not_called(self):
        class Notes(list):
            def __len__(self): raise AssertionError('Unexpected length hook')
            def append(self, value): raise AssertionError('Unexpected append hook')
        primary = Hostile('read failed'); prior = Notes(); attributes(primary)['__notes__'] = prior
        with self.read_fault(primary, Hostile('close failed')):
            self.primary(primary, lambda: self.io.sha(self.path, None))
        self.assertIs(attributes(primary)['__notes__'], prior); self.assertEqual(list.__len__(prior), 0)

    def test_unsupported_caller_notes_remain_unchanged(self):
        for prior in (None, 'caller', object()):
            self.io = budget.MonitorIO(limits()); primary = Hostile('read failed'); attributes(primary)['__notes__'] = prior
            with self.read_fault(primary, Hostile('close failed')):
                self.primary(primary, lambda: self.io.sha(self.path, None))
            self.assertIs(attributes(primary)['__notes__'], prior)

    def test_note_allocation_failure_preserves_read_error(self):
        primary = Hostile('read failed')
        with self.read_fault(primary, Hostile('close failed')), patch.object(budget, 'object', side_effect=MemoryError, create=True):
            self.primary(primary, lambda: self.io.sha(self.path, None))
        self.assert_hash_failed(read_unknown=True); self.assertNotIn('__notes__', attributes(primary))

    def test_secondary_argument_graph_is_not_formatted(self):
        class Argument:
            def __repr__(self): raise AssertionError('Unexpected argument formatting')
        primary = Hostile('read failed'); closing = Hostile('x'*1000000, Argument())
        with self.read_fault(primary, closing): self.primary(primary, lambda: self.io.sha(self.path, None))
        self.assertLess(len(attributes(primary)['__notes__'][0]), 80)

    def test_inventory_iteration_failure_survives_close_failure(self):
        primary = Hostile('scan failed')
        with self.scan_fault(primary, Hostile('close failed')):
            self.primary(primary, lambda: self.io.inventory(self.empty, []))
        self.assertTrue(self.io.failed); self.assertEqual(self.scanner.close_calls, 1)
        self.assertEqual(attributes(primary)['__notes__'], ['Monitor inventory close failed; secondary details omitted.'])

    def test_inventory_limit_error_survives_close_and_stops_before_next_entry(self):
        (self.empty/'unexpected').write_bytes(b'')
        with self.scan_fault(closing=Hostile('close failed')), self.assertRaisesRegex(ValueError, 'Untracked') as caught:
            self.io.inventory(self.empty, [])
        self.assertEqual(self.scanner.close_calls, 1); self.assertEqual(self.scanner.next_calls, 1)
        self.assertEqual(self.io.inventory_entries, 1); self.assertEqual(Hostile.calls, [])
        self.assertIn('inventory close failed', caught.exception.__notes__[0])

    def test_entry_name_error_survives_close(self):
        primary = Hostile('name failed'); (self.empty/'console.log').write_bytes(b'')
        class Entry:
            @property
            def name(self): raise primary
        with self.scan_fault(closing=Hostile('close failed'), entry=Entry()):
            self.primary(primary, lambda: self.io.inventory(self.empty, ['console.log']))
        self.assertEqual(self.scanner.close_calls, 1); self.assertTrue(self.io.failed)

    def test_inventory_close_failure_alone_escapes(self):
        primary = Hostile('close failed')
        with self.scan_fault(closing=primary): self.primary(primary, lambda: self.io.inventory(self.empty, []))
        self.assertEqual(self.scanner.close_calls, 1); self.assertTrue(self.io.failed)

    def test_inventory_creation_failure_does_not_close_unacquired_iterator(self):
        primary = Hostile('scandir failed')
        with patch.object(budget.os, 'scandir', side_effect=primary), patch.object(budget, '_close') as close:
            self.primary(primary, lambda: self.io.inventory(self.empty, []))
        close.assert_not_called(); self.assertEqual(self.io.inventory_attempts, 1); self.assertTrue(self.io.failed)

    def test_successful_inventory_explicitly_closes_iterator(self):
        (self.empty/'console.log').write_bytes(b'')
        with self.scan_fault(): self.assertEqual(self.io.inventory(self.empty, ['console.log']), {'console.log'})
        self.assertEqual(self.scanner.close_calls, 1); self.assertFalse(self.io.failed)

    def test_caller_exception_does_not_hide_inventory_close_failure(self):
        primary = Hostile('close failed'); caller = RuntimeError('unrelated')
        try: raise caller
        except RuntimeError:
            with self.scan_fault(closing=primary): self.primary(primary, lambda: self.io.inventory(self.empty, []))
        self.assertNotIn('__notes__', attributes(caller)); self.assertTrue(self.io.failed)

    def test_failed_scope_prevents_subsequent_reads_inventories_and_publications(self):
        primary = Hostile('read failed')
        with self.read_fault(primary): self.primary(primary, lambda: self.io.sha(self.path, None))
        before = self.io.snapshot()
        with patch.object(Path, 'open') as opening, patch.object(budget.os, 'scandir') as scanning:
            for action in (lambda: self.io.sha(self.path, None), lambda: self.io.inventory(self.empty, []),
                           lambda: self.io.publication('owner-process.json', {})):
                with self.assertRaisesRegex(ValueError, 'cannot be reused'): action()
        opening.assert_not_called(); scanning.assert_not_called(); self.assertEqual(self.io.snapshot(), before)

    def test_pure_io_import_never_requires_windows_launcher(self):
        code = '''import builtins,sys
original=builtins.__import__
def importing(name,*args,**kwargs):
    if name in ('bounded_windows_process','proposal_owner_process'): raise AssertionError('Windows dependency')
    return original(name,*args,**kwargs)
builtins.__import__=importing
sys.path.insert(0,sys.argv[1])
import proposal_monitor_io
assert proposal_monitor_io.bounded_canonical({'literal':1},64)==b'{"literal":1}'
'''
        result = subprocess.run([sys.executable, '-B', '-X', 'utf8', '-c', code, str(ROOT/'Balance Harness/analysis')],
                                capture_output=True, text=True, timeout=10)
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)

    def test_real_console_hash_failure_preserves_read_error_after_job_drain(self):
        import proposal_owner_process as monitor
        import bounded_windows_process as owned
        export = os.environ.get('LL_IO_FINALIZERS_EXPORT')
        if export: self.base = Path(export); self.base.mkdir(parents=True)
        script = self.base/'literal.py'; script.write_text('print("literal owner",flush=True)', encoding='utf-8')
        observed = monitor.OwnerProcessMonitor(self.base/'monitor', 'a'*64, monitor_accounting=True, monitor_io_budget=limits())
        primary = Hostile('literal console read failure')
        with self.read_fault(primary, Hostile('literal console close failure'), path=observed.root/'console.log',
                             guard=lambda: observed._monitor_phase == 'observation'):
            self.primary(primary, lambda: observed.run(script, [], self.base, time.monotonic()+10))
        value = observed.process_observation; terminal = observed.monitor_terminal_observation
        self.assertTrue(value['jobDrained']); self.assertTrue(value['cleanup']['allAcquiredHandlesClosed'])
        self.assertIs(observed.publication_error, primary); self.assertIsNone(observed.observation)
        self.assertEqual(self.reader.close_calls, 1); self.assertTrue(self.reader.stream.closed)
        self.assertEqual(terminal['outcome'], 'Failed'); self.assertEqual(terminal['failedPhase'], 'observation')
        self.assertFalse(terminal['publicationVerified']); self.assertEqual(terminal['monitorIO']['publications'], {})
        self.assertTrue(terminal['monitorIO']['failed']); self.assertTrue(terminal['monitorIO']['failedReadProgressUnknown'])
        self.assertEqual(terminal['monitorError'], "Hostile('literal console read failure')")
        if export:
            work.seal_new(self.base/'process-observation.json', value)
            work.seal_new(self.base/'memory-snapshot.json', terminal)
            work.seal_new(self.base/'fixture.json', dict(fixtureOnly=True, primaryErrorPreserved=True,
                readCloseAttempts=self.reader.close_calls, streamClosed=self.reader.stream.closed, notes=attributes(primary)['__notes__'],
                formattingHookCalls=Hostile.calls, monitorIOModuleSha256=monitor.sha(budget.__file__),
                monitorProducerSha256=monitor.sha(monitor.__file__), processHelperSha256=monitor.sha(owned.__file__),
                actualCombat=0, productionEntropyDraws=0, scientificReservations=0, nativeEncounterPreparations=0,
                wholeProcessCoverage=False, usableForAdmission=False))
            work.seal_new(self.base/'files.json', {p.relative_to(self.base).as_posix(): monitor.sha(p)
                for p in sorted(self.base.rglob('*')) if p.is_file()})


if __name__ == '__main__': unittest.main()
