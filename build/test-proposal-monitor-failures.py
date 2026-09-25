"""Monitor failure preservation; literal jobs only, never scientific admission."""
import os
from pathlib import Path
import sys
import tempfile
import time
from types import SimpleNamespace
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import bounded_windows_process as owned
import proposal_monitor_io as budget
import proposal_owner_process as monitor
import proposal_work_accounting as work


class Hostile(RuntimeError):
    calls = []
    def __str__(self): self.calls.append('str'); raise AssertionError('Exception str hook')
    def __repr__(self): self.calls.append('repr'); raise AssertionError('Exception repr hook')
    def __bool__(self): self.calls.append('bool'); raise AssertionError('Exception bool hook')
    def add_note(self, note): self.calls.append('add_note'); raise AssertionError('Exception note hook')
    def __getattribute__(self, name):
        if name in ('args', '__dict__', '__notes__'):
            type(self).calls.append(name); raise AssertionError('Exception attribute hook')
        return super().__getattribute__(name)


def notes(error): return BaseException.__dict__['__dict__'].__get__(error).get('__notes__', [])


def declaration(root):
    return dict(version=budget.TERMINAL_VERSION, terminalRoot=str(root), maxTerminalBytes=65536,
                maxFileReadBytes=16*1024*1024, maxTotalReadBytes=32*1024*1024,
                maxConsoleBytes=4096, maxObservationBytes=65536, maxManifestBytes=1024, maxTotalWriteBytes=131072)


class FaultFile:
    def __init__(self, stream, events, body_error=None, close_error=None, operation='write'):
        self.stream, self.events = stream, events
        self.body_error, self.close_error, self.operation = body_error, close_error, operation
    def write(self, raw):
        if self.body_error is not None and self.operation == 'write':
            self.stream.write(raw[:3]); raise self.body_error
        return self.stream.write(raw)
    def flush(self):
        if self.body_error is not None and self.operation == 'flush': raise self.body_error
        return self.stream.flush()
    def fileno(self): return self.stream.fileno()
    def close(self):
        self.events.append('close'); self.stream.close()
        if self.close_error is not None: raise self.close_error


class MonitorFailures(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(); self.addCleanup(temporary.cleanup)
        self.base = Path(temporary.name); self.events = []; self.streams = []; Hostile.calls.clear()
        self.m = monitor.OwnerProcessMonitor(self.base/'monitor', 'a'*64, monitor_accounting=True)
        self.m.root.mkdir(); self.m.deadline = time.monotonic()+10; self.m.check = None
        self.m._monitor_active = True; self.m._phase('publication')
        self.counters = self.m._work()

    def opening(self, body=None, close=None, operation='write', target=None):
        original = Path.open; target = self.m.root/'owner-process.json' if target is None else target
        def opening(path, *args, **kwargs):
            stream = original(path, *args, **kwargs)
            if path == target and args and args[0] == 'xb':
                self.streams.append(stream); return FaultFile(stream, self.events, body, close, operation)
            return stream
        return patch.object(Path, 'open', opening)

    def observation(self, error=None):
        original = work.OwnerFileCounters.observe_file
        def observe(counters, path, missing_ok=True):
            if Path(path) == self.m.root/'owner-process.json':
                self.events.append('observe')
                if error is not None: raise error
            return original(counters, path, missing_ok)
        return patch.object(work.OwnerFileCounters, 'observe_file', observe)

    def accounting(self, failures):
        original = work.OwnerFileCounters.add
        def add(counters, key, *args):
            if key in failures: raise failures[key]
            return original(counters, key, *args)
        return patch.object(work.OwnerFileCounters, 'add', add)

    def assert_primary(self, error, action):
        try: action()
        except BaseException as caught: self.assertIs(caught, error)
        else: self.fail('Expected original failure')
        self.assertEqual(Hostile.calls, [])

    def seal(self): return self.m._seal('owner-process.json', {'literal': 'observation'})

    def assert_finalized(self):
        self.assertEqual(self.events, ['close', 'observe'])
        self.assertTrue(all(s.closed for s in self.streams))
        self.assertEqual(self.m._monitor_failure_phase, 'publication')

    def test_write_failure_survives_close_and_file_observation_errors(self):
        primary = Hostile('write failed')
        with self.opening(primary, Hostile('close failed')), self.observation(Hostile('sample failed')):
            self.assert_primary(primary, self.seal)
        self.assert_finalized(); self.assertEqual(len(notes(primary)), 2)
        self.assertEqual((self.m.root/'owner-process.json').read_bytes(), b'{"l')

    def test_flush_failure_survives_both_finalizers(self):
        primary = Hostile('flush failed')
        with self.opening(primary, Hostile('close failed'), 'flush'), self.observation(Hostile('sample failed')):
            self.assert_primary(primary, self.seal)
        self.assert_finalized(); self.assertEqual(len(notes(primary)), 2)

    def test_sync_failure_survives_both_finalizers(self):
        primary = Hostile('sync failed')
        with self.opening(close=Hostile('close failed')), self.observation(Hostile('sample failed')), \
             patch.object(monitor.os, 'fsync', side_effect=primary):
            self.assert_primary(primary, self.seal)
        self.assert_finalized()

    def test_close_failure_precedes_file_observation_failure(self):
        primary = Hostile('close failed')
        with self.opening(close=primary), self.observation(Hostile('sample failed')):
            self.assert_primary(primary, self.seal)
        self.assert_finalized(); self.assertIn('file accounting', notes(primary)[0])

    def test_file_observation_failure_alone_escapes_and_marks_phase(self):
        primary = Hostile('sample failed')
        with self.opening(), self.observation(primary): self.assert_primary(primary, self.seal)
        self.assert_finalized()

    def test_partial_write_survives_failed_and_unknown_byte_accounting(self):
        primary = Hostile('write failed')
        with self.opening(primary), self.observation(), self.accounting({
                'publicationWriteFailed': Hostile('failed counter'),
                'failedPublicationWriteBytesUnknown': Hostile('unknown counter')}):
            self.assert_primary(primary, self.seal)
        self.assert_finalized(); self.assertEqual(len(notes(primary)), 2)

    def test_close_attempt_counter_failure_does_not_skip_close_or_sample(self):
        primary = Hostile('close attempt counter')
        with self.opening(close=Hostile('close failed')), self.observation(Hostile('sample failed')), \
             self.accounting({'publicationCloseAttempted': primary, 'publicationCloseFailed': Hostile('close outcome counter')}):
            self.assert_primary(primary, self.seal)
        self.assert_finalized(); self.assertEqual(len(notes(primary)), 3)

    def test_close_completion_counter_failure_still_samples(self):
        primary = Hostile('close completion counter')
        with self.opening(), self.observation(), self.accounting({'publicationCloseCompleted': primary}):
            self.assert_primary(primary, self.seal)
        self.assert_finalized()

    def test_open_completion_counter_failure_still_closes_and_samples(self):
        primary = Hostile('open completion counter')
        with self.opening(), self.observation(), self.accounting({'publicationOpenCompleted': primary}):
            self.assert_primary(primary, self.seal)
        self.assert_finalized()

    def test_failed_open_has_no_stream_to_finalize(self):
        primary = Hostile('open failed')
        with patch.object(Path, 'open', side_effect=primary), self.observation():
            self.assert_primary(primary, self.seal)
        self.assertEqual(self.events, []); self.assertFalse((self.m.root/'owner-process.json').exists())

    def test_full_prior_note_list_does_not_prevent_finalizers(self):
        primary = Hostile('write failed'); attributes = BaseException.__dict__['__dict__'].__get__(primary)
        prior = ['caller']*8; attributes['__notes__'] = prior
        with self.opening(primary, Hostile('close failed')), self.observation(Hostile('sample failed')):
            self.assert_primary(primary, self.seal)
        self.assert_finalized(); self.assertIs(notes(primary), prior); self.assertEqual(len(prior), 8)

    def test_annotation_allocation_failure_does_not_skip_sample(self):
        primary = Hostile('write failed')
        with self.opening(primary, Hostile('close failed')), self.observation(Hostile('sample failed')), \
             patch.object(owned, '_clip_error', side_effect=MemoryError):
            self.assert_primary(primary, self.seal)
        self.assert_finalized(); self.assertEqual(notes(primary), [])

    def test_caller_active_exception_does_not_swallow_new_close_failure(self):
        primary = Hostile('close failed')
        try: raise RuntimeError('unrelated caller')
        except RuntimeError:
            with self.opening(close=primary), self.observation(): self.assert_primary(primary, self.seal)
        self.assert_finalized()

    def test_legacy_hash_failure_survives_unknown_progress_counter_error(self):
        primary = Hostile('hash failed')
        with patch.object(self.counters, 'sha', side_effect=primary), \
             self.accounting({'failedHashReadBytesUnknown': Hostile('hash counter')}):
            self.assert_primary(primary, lambda: self.m._sha(self.base/'unused'))
        self.assertEqual(len(notes(primary)), 1)

    def test_budgeted_write_failure_is_sticky_and_progress_remains_unknown(self):
        self.m.monitor_io = budget.MonitorIO(declaration(self.base/'terminal'))
        primary = Hostile('write failed')
        with self.opening(primary, Hostile('close failed')), self.observation(Hostile('sample failed')):
            self.assert_primary(primary, self.seal)
        self.assert_finalized(); value = self.m.monitor_io.snapshot()
        self.assertTrue(value['failed']); self.assertTrue(value['failedWriteProgressUnknown'])
        self.assertEqual(value['acceptedWriteBytes'], 0); self.assertGreater(value['reservedWriteBytes'], 0)

    def test_unaccounted_publication_keeps_original_error_and_closes(self):
        self.m._monitor_active = False; primary = Hostile('write failed')
        with self.opening(primary, Hostile('close failed')): self.assert_primary(primary, self.seal)
        self.assertEqual(self.events, ['close']); self.assertTrue(self.streams[0].closed)
        self.assertEqual(len(notes(primary)), 1)

    def test_large_snapshot_errors_use_bounded_text(self):
        self.m._monitor_started_at = time.monotonic(); self.m.started_at = time.monotonic()
        primary = Hostile(*(['\0'*10000]*100)); self.m.publication_error = primary
        self.m._snapshot({}, primary); self.m._monitor_snapshot(primary)
        values = [self.m.observation['monitorError'], self.m.monitor_terminal_observation['monitorError'],
                  self.m.monitor_terminal_observation['publicationError']]
        self.assertTrue(all(0 < len(v) <= 4096 and '[truncated]' in v for v in values))
        self.assertEqual(Hostile.calls, [])

    def test_terminal_snapshot_failure_keeps_original_body_error(self):
        primary = Hostile('body failed')
        with patch.object(self.m, '_run', side_effect=primary), \
             patch.object(self.m, '_monitor_snapshot', side_effect=Hostile('snapshot failed')):
            self.assert_primary(primary, lambda: self.m.run(None, [], None, 0))
        self.assertFalse(self.m._monitor_active); self.assertIsNone(self.m.monitor_terminal_observation)
        self.assertIn('terminal observation', notes(primary)[0])

    def test_terminal_result_failure_keeps_original_publication_error(self):
        primary = Hostile('terminal write failed')
        self.m.monitor_io = budget.MonitorIO(declaration(self.base/'terminal'))
        self.m._terminal_root = self.base/'terminal'; self.m._terminal_root.mkdir()
        self.m._terminal_identity = self.m._terminal_directory()
        with patch.object(self.m, '_seal', side_effect=primary), \
             patch.object(self.m.monitor_io, 'snapshot', side_effect=Hostile('result failed')):
            self.assert_primary(primary, self.m._persist_terminal)
        self.assertIsNone(self.m.terminal_publication_observation)
        self.assertIn('result construction', notes(primary)[0])

    def real_monitor(self, *, export=False):
        root = os.environ.get('LL_MONITOR_FAILURES_EXPORT') if export else None
        if root: self.base = Path(root); self.base.mkdir(parents=True)
        self.script = self.base/'literal.py'; self.script.write_text('print("literal owner", flush=True)', encoding='utf-8')
        self.m = monitor.OwnerProcessMonitor(self.base/'observed', 'a'*64, monitor_accounting=True,
                                            monitor_io_budget=declaration(self.base/'terminal'))

    def run_monitor(self): return self.m.run(self.script, [], self.base, time.monotonic()+10)

    def test_real_publication_failure_retains_memory_snapshot_and_original_error(self):
        self.real_monitor(export=True); primary = Hostile('literal write failure')
        with self.opening(primary, Hostile('literal close failure')), self.observation(Hostile('literal sample failure')):
            self.assert_primary(primary, self.run_monitor)
        self.assert_finalized(); self.assertEqual(len(notes(primary)), 3)
        self.assertTrue(self.m.process_observation['jobDrained'])
        self.assertTrue(self.m.process_observation['cleanup']['allAcquiredHandlesClosed'])
        terminal = self.m.monitor_terminal_observation
        self.assertEqual(terminal['outcome'], 'Failed'); self.assertFalse(terminal['publicationVerified'])
        self.assertEqual(terminal['monitorError'], "Hostile('literal write failure')")
        self.assertEqual(terminal['publicationError'], terminal['monitorError'])
        self.assertEqual(terminal['failedPhase'], 'publication')
        self.assertEqual(self.m.terminal_publication_observation['outcome'], 'Failed')
        self.assertIn('Failed monitor I/O scope cannot be reused', self.m.terminal_publication_observation['error'])
        self.assertFalse((self.base/'terminal/monitor-call.json').exists())
        self.assertTrue(terminal['monitorIO']['failedWriteProgressUnknown'])
        self.assertFalse((self.m.root/'files.json').exists())
        if os.environ.get('LL_MONITOR_FAILURES_EXPORT'):
            work.seal_new(self.base/'memory-snapshot.json', terminal)
            work.seal_new(self.base/'process-observation.json', self.m.process_observation)
            work.seal_new(self.base/'publication-result.json', self.m.terminal_publication_observation)
            work.seal_new(self.base/'fixture.json', dict(fixtureOnly=True, primaryErrorPreserved=True,
                cleanupEvents=self.events, streamsClosed=all(s.closed for s in self.streams), notes=notes(primary),
                formattingHookCalls=Hostile.calls, fileLengthObservationFailureInjected=True,
                monitorProducerSha256=monitor.sha(monitor.__file__), processHelperSha256=monitor.sha(owned.__file__),
                actualCombat=0, productionEntropyDraws=0, scientificReservations=0, nativeEncounterPreparations=0,
                wholeProcessCoverage=False, usableForAdmission=False))
            work.seal_new(self.base/'files.json', {p.relative_to(self.base).as_posix(): monitor.sha(p)
                for p in sorted(self.base.rglob('*')) if p.is_file()})

    def test_hostile_owner_failure_and_publication_failure_do_not_test_truthiness(self):
        self.real_monitor(); primary = Hostile('owner failed')
        with patch.object(owned, 'run', side_effect=primary), patch.object(self.m, '_publish', side_effect=Hostile('publish failed')):
            self.assert_primary(primary, self.run_monitor)
        self.assertEqual(self.m.observation['monitorError'], "Hostile('owner failed')")
        self.assertEqual(self.m.monitor_terminal_observation['monitorError'], "Hostile('owner failed')")
        self.assertIn('observation publication', notes(primary)[0])
        self.assertEqual(self.m.terminal_publication_observation['outcome'], 'Verified')

    def test_real_terminal_write_and_close_errors_preserve_first_failure(self):
        self.real_monitor(); primary = Hostile('terminal write failed')
        with self.opening(primary, Hostile('terminal close failed'), target=self.base/'terminal/monitor-call.json'):
            self.assert_primary(primary, self.run_monitor)
        value = self.m.terminal_publication_observation
        self.assertEqual(value['outcome'], 'Failed'); self.assertEqual(value['error'], "Hostile('terminal write failed')")
        self.assertTrue(self.m.monitor_terminal_observation['publicationVerified'])
        self.assertEqual(self.events, ['close']); self.assertTrue(self.streams[0].closed)
        self.assertEqual(len(notes(primary)), 1)

    def test_owner_error_survives_terminal_persistence_failure(self):
        self.real_monitor(); primary = Hostile('owner failed')
        with patch.object(owned, 'run', side_effect=primary), \
             self.opening(Hostile('terminal failed'), target=self.base/'terminal/monitor-call.json'):
            self.assert_primary(primary, self.run_monitor)
        self.assertIn('terminal persistence', notes(primary)[0])
        self.assertEqual(self.m.terminal_publication_observation['error'], "Hostile('terminal failed')")

    def runtime_failure(self, owner_error=None):
        original = owned.run; runtime_error = Hostile('runtime sample failed')
        def sample(): raise runtime_error
        def run(*args, **kwargs):
            result = original(*args, **kwargs)
            self.m.runtime_environment = SimpleNamespace(failed=False, sample=sample, snapshot=lambda: {'fixtureOnly': True})
            if owner_error is not None: raise owner_error
            return result
        return runtime_error, patch.object(owned, 'run', run)

    def test_runtime_error_precedes_publication_error_without_truthiness(self):
        self.real_monitor(); primary, injected = self.runtime_failure()
        with injected, patch.object(self.m, '_publish', side_effect=Hostile('publish failed')):
            self.assert_primary(primary, self.run_monitor)
        self.assertEqual(self.m.observation['monitorError'], "Hostile('runtime sample failed')")
        self.assertIn('observation publication', notes(primary)[0])

    def test_owner_error_precedes_runtime_and_publication_errors(self):
        self.real_monitor(); primary = Hostile('owner failed'); _, injected = self.runtime_failure(primary)
        with injected, patch.object(self.m, '_publish', side_effect=Hostile('publish failed')):
            self.assert_primary(primary, self.run_monitor)
        self.assertEqual(len(notes(primary)), 2)
        self.assertIn('Runtime inspection failed', notes(primary)[1])


if __name__ == '__main__': unittest.main()
