"""Owner file-boundary correctness only; never launches a scientific study."""
import builtins
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

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as w
import bounded_windows_process as owned

spec = importlib.util.spec_from_file_location('file_owner', ROOT/'build/run-proposal-affinity-study.py')
owner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(owner)


class OwnerFiles(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.work = w.OwnerFileCounters()

    def test_json_bytes_exclusive_open_and_hash_match_default(self):
        value = dict(text='æ\n漢字\ud800', values=[None, True, 1.25], nested={'x': 2})
        plain, counted = self.root/'plain.json', self.root/'counted.json'
        owner.write(plain, value)
        with owner.file_accounting(self.work):
            owner.write(counted, value)
            self.assertEqual(owner.digest(counted), hashlib.sha256(plain.read_bytes()).hexdigest())
            with self.assertRaises(FileExistsError): owner.write(counted, {})
        self.assertEqual(counted.read_bytes(), plain.read_bytes())
        size = counted.stat().st_size
        self.assertEqual(self.work.values['textAcceptedUtf8Bytes.json'], size)
        self.assertEqual(self.work.values['applicationReadBytes.json'], size)
        self.assertEqual(self.work.values['sampledTrackedFileBytes'], size)
        self.assertEqual(self.work.values['jsonWriteOperationsCompleted'], 1)
        self.assertEqual(self.work.values['jsonWriteOperationsFailed'], 1)
        self.assertEqual(self.work.values['fileSyncsCompleted'], 1)
        self.assertNotIn('applicationWriteBytes.json', self.work.values)

    def test_partial_serialization_retains_identical_prefix_and_original_error(self):
        value = dict(prefix='saved', invalid=object())
        for name, work in [('plain.json', None), ('counted.json', self.work)]:
            with owner.file_accounting(work), self.assertRaises(TypeError): owner.write(self.root/name, value)
        self.assertEqual((self.root/'plain.json').read_bytes(), (self.root/'counted.json').read_bytes())
        self.assertEqual(self.work.values['jsonWriteOperationsFailed'], 1)
        self.assertEqual(self.work.values['fileSyncsAttempted'], 0)
        self.assertEqual(self.work.values['textAcceptedUtf8Bytes.json'], (self.root/'counted.json').stat().st_size)

    def test_nested_scope_resets_after_exception(self):
        other = w.OwnerFileCounters()
        self.assertIsNone(owner.FILE_WORK.get())
        with owner.file_accounting(self.work):
            with self.assertRaisesRegex(RuntimeError, 'inner'):
                with owner.file_accounting(other):
                    owner.write(self.root/'inner.json', {})
                    raise RuntimeError('inner')
            self.assertIs(owner.FILE_WORK.get(), self.work)
            owner.write(self.root/'outer.json', {})
        self.assertIsNone(owner.FILE_WORK.get())
        self.assertEqual(self.work.values['jsonWriteOperationsCompleted'], 1)
        self.assertEqual(other.values['jsonWriteOperationsCompleted'], 1)

    def test_copy_overwrite_return_value_and_bytes_match_original(self):
        source, dest = self.root/'source.json', self.root/'dest.json'
        source.write_bytes(b'new'); dest.write_bytes(b'previous longer bytes')
        with owner.file_accounting(self.work):
            self.assertEqual(owner.copy_file(str(source), str(dest)), str(dest))
        self.assertEqual(dest.read_bytes(), b'new')
        self.assertEqual(self.work.values['fileCopyLogicalBytes.json'], 3)
        self.assertEqual(self.work.values['sampledTrackedFileBytes'], 3)
        self.assertEqual(self.work.values['sampledPeakTrackedFileBytes'], 21)
        self.assertNotIn('applicationReadBytes.json', self.work.values)
        self.assertNotIn('applicationWriteBytes.json', self.work.values)

    def test_copy_failures_preserve_python_errors_and_existing_bytes(self):
        source = self.root/'source.json'; source.write_bytes(b'saved')
        for src, dest, kind in [(source, source, shutil.SameFileError),
                                (self.root/'missing', source, FileNotFoundError),
                                (source, self.root/'missing'/'out', FileNotFoundError)]:
            with self.subTest(kind=kind), owner.file_accounting(self.work), self.assertRaises(kind):
                owner.copy_file(src, dest)
        self.assertEqual(source.read_bytes(), b'saved')
        self.assertEqual(self.work.values['failedFileCopyBytesUnknown'], 3)
        self.assertEqual(self.work.values['fileCopyOperationsCompleted'], 0)

    def test_failed_native_copy_partial_size_is_observed_not_write_io(self):
        dest = self.root/'dest.json'
        def fail(*_):
            dest.write_bytes(b'partial')
            raise OSError('copy failed')
        with patch.object(w.shutil, 'copyfile', side_effect=fail), self.assertRaisesRegex(OSError, 'copy failed'):
            self.work.copyfile(self.root/'source', dest)
        self.assertEqual(self.work.values['sampledTrackedFileBytes'], 7)
        self.assertEqual(self.work.values['failedFileCopyBytesUnknown'], 1)
        self.assertNotIn('fileCopyLogicalBytes.json', self.work.values)

    def test_fsync_failure_does_not_erase_accepted_text_or_report_success(self):
        with patch.object(w.os, 'fsync', side_effect=OSError('sync failed')):
            with self.assertRaisesRegex(OSError, 'sync failed'): self.work.write_json(self.root/'out.json', {})
        self.assertEqual((self.root/'out.json').read_bytes(), b'{}')
        self.assertEqual(self.work.values['textAcceptedUtf8Bytes.json'], 2)
        self.assertEqual(self.work.values['jsonWriteOperationsCompleted'], 0)
        self.assertEqual(self.work.values['jsonWriteOperationsFailed'], 1)
        self.assertEqual(self.work.values['textFlushesCompleted'], 1)
        self.assertEqual(self.work.values['fileSyncsAttempted'], 1)
        self.assertEqual(self.work.values['fileSyncsCompleted'], 0)

    def test_text_write_flush_and_close_failures_keep_native_error(self):
        original_open = Path.open
        for boundary in ('write', 'flush', 'close'):
            with self.subTest(boundary=boundary):
                work = w.OwnerFileCounters(); target = self.root/(boundary+'.json')
                class Stream:
                    def __init__(self):
                        self.raw = original_open(target, 'x', encoding='utf-8', newline='\n')
                    def write(self, text):
                        if boundary == 'write':
                            self.raw.write(text[:1])
                            raise OSError('write failed')
                        return self.raw.write(text)
                    def flush(self):
                        if boundary == 'flush': raise OSError('flush failed')
                        return self.raw.flush()
                    def fileno(self): return self.raw.fileno()
                    def close(self):
                        self.raw.close()
                        if boundary == 'close': raise OSError('close failed')
                with patch.object(Path, 'open', return_value=Stream()), self.assertRaisesRegex(OSError, boundary+' failed'):
                    work.write_json(target, dict(a='prefix'))
                self.assertEqual(work.values['jsonWriteOperationsFailed'], 1)
                self.assertEqual(work.values['jsonWriteOperationsCompleted'], 0)
                self.assertEqual(work.values['sampledTrackedFileBytes'], target.stat().st_size)
                self.assertEqual(work.values['failedTextWriteBytesUnknown'], int(boundary == 'write'))

    def test_failed_stat_is_unknown_and_does_not_mask_copy_error(self):
        with patch.object(Path, 'stat', side_effect=PermissionError('stat denied')):
            with patch.object(w.shutil, 'copyfile', side_effect=OSError('original')):
                with self.assertRaisesRegex(OSError, 'original'): self.work.copyfile('source', 'dest')
        self.assertEqual(self.work.values['fileLengthObservationFailures'], 2)
        self.assertNotIn('sampledTrackedFileBytes', self.work.values)

    def test_link_or_directory_size_is_not_regular_file_storage(self):
        self.assertIsNone(self.work.observe_file(self.root))
        self.assertEqual(self.work.values['fileLengthObservationFailures'], 1)
        self.assertNotIn('sampledTrackedFileBytes', self.work.values)

    def test_missing_created_log_final_length_is_unknown(self):
        with self.work.child_log(self.root/'missing') as log: log.opened()
        self.assertEqual(self.work.values['fileLengthObservationFailures'], 1)
        self.assertNotIn('childLogFinalObservedBytes.other', self.work.values)

    def test_counter_receipt_stays_bound_and_explicitly_incomplete(self):
        self.work.write_json(self.root/'out.json', {})
        receipt = self.work.receipt('publication', 'a'*64, 'b'*64, True)
        raw = w.canonical(receipt)
        self.assertEqual(w.counter_receipt(raw, hashlib.sha256(raw).hexdigest(), 'publication', 'a'*64, 'b'*64), receipt)
        self.assertFalse(receipt['wholeProcessCoverage'])
        self.assertFalse(receipt['usableForAdmission'])

    def test_closed_compressed_guard_is_unchanged_under_collector(self):
        with owner.file_accounting(self.work), self.assertRaisesRegex(ValueError, 'recovery gate is closed'):
            owner.validate_request(dict(evidenceStorage={'version': 'irrelevant'}))
        self.assertFalse(self.work.values)


class ChildLogs(unittest.TestCase):
    setUp = OwnerFiles.setUp

    def child(self, script, timeout=5, **kwargs):
        return owned.run([sys.executable, '-B', '-c', script], self.root, self.root/'child.log',
                         time.monotonic()+timeout, file_work=self.work, **kwargs)

    def test_stdout_stderr_log_observed_after_handles_and_observer(self):
        observations = []
        def observe(value):
            self.assertNotIn('childLogFinalObservedBytes.other', self.work.values)
            observations.append(value)
        result = self.child("import os; os.write(1,b'out'); os.write(2,b'err')", observe=observe)
        self.assertEqual(result['exitCode'], 0)
        self.assertEqual((self.root/'child.log').read_bytes(), b'outerr')
        self.assertTrue(observations[0]['jobDrained'])
        self.assertEqual(self.work.values['childLogFinalObservedBytes.other'], 6)
        self.assertNotIn('applicationWriteBytes.other', self.work.values)

    def test_existing_log_is_not_counted_or_overwritten(self):
        (self.root/'child.log').write_bytes(b'old')
        with self.assertRaises(FileExistsError): self.child('pass')
        self.assertEqual((self.root/'child.log').read_bytes(), b'old')
        self.assertEqual(self.work.values['childLogFilesCreated'], 0)
        self.assertNotIn('childLogFinalObservedBytes.other', self.work.values)

    def test_devnull_open_failure_still_observes_created_log(self):
        original = builtins.open
        def fail(path, *args, **kwargs):
            if path == os.devnull: raise OSError('null failed')
            return original(path, *args, **kwargs)
        with patch('builtins.open', side_effect=fail), self.assertRaisesRegex(OSError, 'null failed'):
            self.child('pass')
        self.assertEqual(self.work.values['childLogFilesCreated'], 1)
        self.assertEqual(self.work.values['childLogFinalObservedBytes.other'], 0)
        self.assertEqual(self.work.values['childLogScopesFailed'], 1)

    def test_failed_process_creation_retains_zero_log(self):
        with patch.object(owned, '_create_process', return_value=False), self.assertRaises(OSError):
            self.child('pass')
        self.assertEqual(self.work.values['childLogScopesFailed'], 1)
        self.assertEqual(self.work.values['childLogFilesCreated'], 1)
        self.assertEqual(self.work.values['childLogFinalObservedBytes.other'], 0)

    def test_failed_observer_still_accounts_after_cleanup(self):
        def fail(_): raise RuntimeError('observer failed')
        with self.assertRaisesRegex(RuntimeError, 'observer failed'):
            self.child("import os; os.write(1,b'saved')", observe=fail)
        self.assertEqual(self.work.values['childLogFinalObservedBytes.other'], 5)
        self.assertEqual(self.work.values['childLogScopesFailed'], 1)

    def test_descendant_log_is_complete_before_final_observation(self):
        observations = []
        child = "import os,time; time.sleep(.1); os.write(1,b'descendant')"
        result = self.child('import subprocess,sys; subprocess.Popen([sys.executable,"-B","-c",'+repr(child)+'])',
                            observe=observations.append)
        self.assertEqual(result['exitCode'], 0)
        self.assertGreaterEqual(result['totalProcesses'], 2)
        self.assertTrue(observations[0]['jobDrained'])
        self.assertEqual((self.root/'child.log').read_bytes(), b'descendant')
        self.assertEqual(self.work.values['childLogFinalObservedBytes.other'], 10)

    def test_owner_check_failure_retains_log_after_job_is_drained(self):
        observations = []
        # Expire once output is visible, without depending on process startup speed.
        deadline = time.monotonic()+5
        def check():
            if (self.root/'child.log').stat().st_size:
                raise RuntimeError('stop after output')
        with self.assertRaisesRegex(RuntimeError, 'stop after output'):
            owned.run([sys.executable, '-B', '-c', "import os,time; os.write(1,b'prefix'); time.sleep(30)"],
                      self.root, self.root/'child.log', deadline, check=check, observe=observations.append,
                      file_work=self.work)
        self.assertTrue(observations[0]['jobDrained'])
        self.assertEqual(self.work.values['childLogFinalObservedBytes.other'], 6)
        self.assertEqual(self.work.values['childLogScopesFailed'], 1)

    def test_timeout_and_nonzero_result_do_not_claim_child_success(self):
        result = self.child('raise SystemExit(7)')
        self.assertEqual(result['exitCode'], 7)
        self.assertEqual(self.work.values['childLogScopesCompleted'], 1)
        (self.root/'child.log').unlink()
        observations = []
        result = self.child('import time; time.sleep(30)', timeout=1, observe=observations.append)
        self.assertTrue(result['timedOut'])
        self.assertTrue(observations[0]['jobDrained'])
        self.assertEqual(self.work.values['childLogScopesCompleted'], 2)

    def test_retained_bound_fixture(self):
        destination = os.environ.get('LL_OWNER_FILE_FIXTURE')
        if destination:
            self.root = Path(destination)
            self.root.mkdir()
        source = self.root/'input.json'; source.write_bytes(b'{"literal":true}')
        with owner.file_accounting(self.work):
            owner.copy_file(source, self.root/'copied.json')
            owner.write(self.root/'publication.json', dict(status='LiteralFixture', newFights=0))
            owner.digest(self.root/'publication.json')
        observations = []
        result = self.child("import os; os.write(1,b'fixture-log')", observe=observations.append)
        self.assertEqual(result['exitCode'], 0)
        self.assertTrue(observations[0]['jobDrained'])
        self.assertEqual(self.work.values['childLogFinalObservedBytes.other'], 11)
        self.assertEqual(self.work.values['sampledTrackedFileBytes'],
                         sum((self.root/name).stat().st_size for name in ['copied.json', 'publication.json', 'child.log']))
        bindings = {str(Path(module.__file__).resolve().relative_to(ROOT).as_posix()):
                    hashlib.sha256(Path(module.__file__).read_bytes()).hexdigest() for module in (w, owner, owned)}
        receipt = self.work.receipt('publication', hashlib.sha256(source.read_bytes()).hexdigest(),
                                   bindings['build/run-proposal-affinity-study.py'], True)
        w.seal_new(self.root/'owner-file-work.json', receipt)
        w.seal_new(self.root/'process-observation.json', observations[0])
        w.seal_new(self.root/'binding.json', dict(sources=bindings, fixtureOnly=True,
                   terminalPublicationExcluded=True, wholeProcessCoverage=False, usableForAdmission=False))
        w.seal_new(self.root/'files.json', {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
                   for p in sorted(self.root.iterdir()) if p.is_file()})


if __name__ == '__main__': unittest.main()
