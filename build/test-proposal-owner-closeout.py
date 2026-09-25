"""Owner accounting through final publication; literal fixtures only."""
import hashlib
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
ANALYSIS = ROOT/'Balance Harness/analysis'
sys.path.insert(0, str(ANALYSIS))
import proposal_work_accounting as w
import bounded_windows_process as owned
spec = importlib.util.spec_from_file_location('lease_owner', ROOT/'build/run-proposal-affinity-study.py')
launcher = importlib.util.module_from_spec(spec); spec.loader.exec_module(launcher)


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class Closeout(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.tick = 10
        self.work = w.OwnerFileCounters()

    def owner(self, **kwargs):
        return w.RetainedOwner(self.root/'owner', 'a'*64, 'b'*64, sha(w.__file__), lambda:self.tick,
                               work=self.work, **kwargs)

    def phase(self, owner, phase):
        with owner.phase(phase, 'c'*64):
            self.tick += 1
            path = self.root/(phase+'.json')
            digest = w.seal_new(path, w.Counters().receipt(phase, 'a'*64, 'c'*64, True))
            owner.attach(path, digest)
            owner.observe_process(dict(version='tower-owned-process-observation-v1', includesHandleCleanup=True,
                usableForAdmission=False, jobDrained=True, enclosingSeconds=.1, memoryCoverage='Unknown',
                peakJobCommitBytes=None, ownerLifetimePeakCommitBytes=None, combinedCommitUpperBoundBytes=None,
                exitCode=0, timedOut=False, ownerError=None, memoryBoundary='BeforeHandleCleanup'), 'd'*64)

    def all_phases(self, owner):
        for phase in w.PHASES: self.phase(owner, phase)

    def verify(self, owner):
        root = owner.storage.root; manifest = json.loads((root/'files.json').read_bytes())
        self.assertEqual(sha(root/'files.json'), owner.manifest_sha256)
        for name, pin in manifest.items(): self.assertEqual(sha(root/name), pin)
        observed = owner.final_observation
        self.assertEqual(observed['manifestSha256'], owner.manifest_sha256)
        self.assertFalse(observed['wholeProcessCoverage']); self.assertFalse(observed['usableForAdmission'])
        return observed

    def test_observation_includes_manifest_bytes_close_cleanup_and_final_hashes(self):
        with self.owner() as owner:
            self.all_phases(owner)
            def cleanup():
                self.tick += 3
                with owner.storage.create('cleanup.tmp', 'scratch') as stream: stream.write(b'x'*4096)
                owner.storage.delete('cleanup.tmp')
            owner.callback(cleanup)
            original = owner._seal
            def seal(name, value):
                result = original(name, value)
                if name in ('owner-work.json', 'files.json'): self.tick += 2
                return result
            owner._seal = seal
        observed = self.verify(owner)
        self.assertEqual(observed['enclosingSeconds'], 11)
        self.assertEqual(observed['terminalPublicationSeconds'], 4)
        self.assertEqual(owner.receipt['ledger']['enclosingSeconds'], 7)
        self.assertTrue(owner.receipt['terminalPublicationExcluded'])
        self.assertTrue(observed['receiptManifestPublicationObserved'])
        self.assertTrue(observed['observationPersistenceExcluded'])
        size = sum(p.stat().st_size for p in owner.storage.root.iterdir())
        storage = observed['managedStorageAfterPublication']; counts = observed['ownerCounters']
        self.assertEqual(storage['currentRetainedBytes'], size)
        self.assertEqual(storage['currentScratchBytes'], 0)
        self.assertEqual(storage['totalWrittenBytes'], size+4096)
        self.assertEqual(storage['deletedBytes'], 4096)
        self.assertEqual(sum(v for k,v in counts.items() if k.startswith('applicationWriteBytes.')), size+4096)
        self.assertEqual(counts['applicationWriteBytes.manifest'], (owner.storage.root/'files.json').stat().st_size)
        self.assertEqual(counts['applicationReadBytes.manifest'], (owner.storage.root/'files.json').stat().st_size)
        self.assertEqual(counts['jsonParseCompleted'], 8)  # external receipt, retained receipt, four phases
        self.assertEqual(counts['managedClosesCompleted'], len(owner.storage.files)+1)

    def test_default_mode_retains_original_receipt_and_has_no_final_observation(self):
        with w.RetainedOwner(self.root/'owner', 'a'*64, 'b'*64, sha(w.__file__), lambda:self.tick) as owner:
            self.all_phases(owner)
        self.assertIsNone(owner.final_observation)
        self.assertTrue(owner.receipt['terminalPublicationExcluded'])

    def test_nonfresh_or_wrong_collector_fails_before_storage_creation(self):
        for work in (w.Counters(), self.work):
            if work is self.work: work.add('alreadyUsed')
            with self.assertRaisesRegex(ValueError, 'fresh owner'):
                w.RetainedOwner(self.root/'owner', 'a'*64, 'b'*64, sha(w.__file__), work=work)
        self.assertFalse((self.root/'owner').exists())

    def test_reclosing_owner_does_not_replace_final_observation(self):
        with self.owner() as owner: self.all_phases(owner)
        original = owner.final_observation
        with self.assertRaisesRegex(ValueError, 'close twice'): owner.__exit__(None, None, None)
        self.assertIs(owner.final_observation, original)

    def test_original_failure_survives_terminal_write_failure_and_has_failed_observation(self):
        error = RuntimeError('worker failed')
        with self.assertRaises(RuntimeError) as caught:
            with self.owner() as owner:
                self.phase(owner, 'native')
                original = owner._seal
                def fail(name, value):
                    if name == 'owner-work.json': raise OSError('disk full')
                    return original(name, value)
                owner._seal = fail
                raise error
        self.assertIs(caught.exception, error)
        self.assertEqual(owner.final_observation['outcome'], 'Failed')
        self.assertFalse(owner.final_observation['receiptManifestPublicationObserved'])
        self.assertIsNone(owner.manifest_sha256)
        self.assertGreater(owner.final_observation['ownerCounters']['applicationWriteBytes.json'], 0)

    def test_cleanup_failure_is_inside_enclosing_duration(self):
        def fail(): self.tick += 6; raise OSError('cleanup failed')
        with self.assertRaisesRegex(OSError, 'cleanup failed'):
            with self.owner() as owner:
                self.all_phases(owner); owner.callback(fail)
        observed = self.verify(owner)
        self.assertEqual(observed['outcome'], 'Failed')
        self.assertEqual(observed['enclosingSeconds'], 10)

    def test_manifest_tamper_after_close_is_not_reported_sealed(self):
        with self.assertRaisesRegex(ValueError, 'Changed final owner member'):
            with self.owner() as owner:
                self.all_phases(owner); original = owner._seal
                def change(name, value):
                    result = original(name, value)
                    if name == 'files.json':
                        path = owner.storage.path(name)
                        raw = path.read_bytes(); path.write_bytes(raw.replace(b'a', b'b', 1))
                    return result
                owner._seal = change
        self.assertIsNone(owner.manifest_sha256)
        observed = owner.final_observation
        self.assertEqual(observed['outcome'], 'Failed')
        self.assertIsNone(observed['managedStorageAfterPublication'])
        self.assertFalse(observed['receiptManifestPublicationObserved'])

    def test_observation_error_never_masks_original_worker_error(self):
        error = RuntimeError('original worker error')
        with self.assertRaises(RuntimeError) as caught:
            with self.owner() as owner:
                self.phase(owner, 'native')
                owner.storage.snapshot = lambda: (_ for _ in ()).throw(OSError('stat failed'))
                raise error
        self.assertIs(caught.exception, error)
        self.assertIn('stat failed', str(error.__notes__))
        self.assertIsNone(owner.final_observation['managedStorageAfterPublication'])
        self.assertIsNone(owner.final_observation['enclosingSeconds'])

    def test_backwards_final_clock_does_not_claim_a_duration(self):
        with self.assertRaisesRegex(ValueError, 'Clock moved backwards'):
            with self.owner() as owner:
                self.all_phases(owner); original = owner._seal
                def seal(name, value):
                    result = original(name, value)
                    if name == 'files.json': self.tick = 0
                    return result
                owner._seal = seal
        self.assertIsNone(owner.final_observation['enclosingSeconds'])
        self.assertIsNone(owner.manifest_sha256)

    def test_caught_managed_sync_failure_prevents_complete_owner(self):
        with self.assertRaisesRegex(ValueError, 'Managed storage operation previously failed'):
            with self.owner() as owner:
                self.all_phases(owner)
                with owner.storage.create('scratch.tmp', 'scratch') as stream:
                    stream.write(b'partial')
                    with patch.object(w.os, 'fsync', side_effect=OSError('sync failed')):
                        with self.assertRaises(OSError): stream.flush()
                owner.storage.delete('scratch.tmp')
        self.assertEqual(owner.final_observation['outcome'], 'Failed')
        self.assertEqual(owner.final_observation['ownerCounters']['managedFileSyncsFailed'], 1)

    def test_managed_short_writes_count_only_accepted_bytes(self):
        store = w.ManagedStorage(self.root/'storage', self.work)
        raw = io_stream = __import__('io').BytesIO()
        class Short:
            closed = False
            def write(self, data): return raw.write(data[:2])
            def close(self): self.closed = True
        with patch.object(Path, 'open', return_value=Short()):
            with store.create('out.json', 'retained') as stream:
                self.assertEqual(stream.write(b'12345'), 2)
                self.assertEqual(stream.write(b'345'), 2)
        self.assertEqual(self.work.values['applicationWriteBytes.json'], 4)
        self.assertEqual(store.written, 4)
        self.assertEqual(io_stream.getvalue(), b'1234')

    def test_failed_managed_write_does_not_invent_partial_bytes(self):
        store = w.ManagedStorage(self.root/'storage', self.work)
        original = Path.open
        class Broken:
            def __init__(self): self.raw = original(store.root/'partial.json', 'xb', buffering=0)
            @property
            def closed(self): return self.raw.closed
            def write(self, data): self.raw.write(data[:2]); raise OSError('partial write failed')
            def close(self): self.raw.close()
        with patch.object(Path, 'open', side_effect=lambda *a, **k: Broken()):
            with store.create('partial.json', 'retained') as stream:
                with self.assertRaisesRegex(OSError, 'partial write'): stream.write(b'12345')
        self.assertNotIn('applicationWriteBytes.json', self.work.values)
        self.assertEqual(self.work.values['failedManagedWriteBytesUnknown'], 1)
        with self.assertRaisesRegex(ValueError, 'Changed managed file'): store.snapshot()


class EnclosingWorkers(unittest.TestCase):
    def test_literal_workers_logs_and_real_lease_cleanup_share_owner_observation(self):
        with tempfile.TemporaryDirectory() as temp:
            destination = os.environ.get('LL_OWNER_CLOSEOUT_EXPORT')
            root = Path(destination) if destination else Path(temp)/'fixture'; root.mkdir()
            study = root/'study'; study.mkdir(); (study/'request.json').write_bytes(b'{"literal":true}')
            script = root/'literal-worker.py'
            script.write_text('import sys\nfrom pathlib import Path\nsys.path.insert(0,'+repr(str(ANALYSIS))+')\n'
                'from proposal_work_accounting import worker_receipt\n'
                'with worker_receipt(sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4],__file__) as work:\n'
                '    assert work.read_json(Path(sys.argv[1])/"request.json")["literal"]\n'
                'print("literal log")\n', encoding='utf-8')
            work = w.OwnerFileCounters()
            with w.RetainedOwner(root/'owner', sha(study/'request.json'), sha(__file__), sha(w.__file__), work=work) as owner:
                lease = launcher.writer_lease(root/'diagnostic-owner'); lease.__enter__()
                owner.callback(lease.__exit__, None, None, None)
                for phase in w.PHASES:
                    with owner.phase(phase, sha(script)):
                        def invoke(binding, pin):
                            result = owned.run([sys.executable, '-B', str(script), str(study), phase, str(binding), pin], root,
                                root/(phase+'.log'), time.monotonic()+8, file_work=work,
                                observe=lambda value:owner.observe_process(value, sha(owned.__file__)))
                            self.assertEqual(result['exitCode'], 0)
                        owner.run_worker(study, script, w.__file__, root/(phase+'-work.json'), invoke)
                def cleanup():
                    with owner.storage.create('scratch.tmp', 'scratch') as stream: stream.write(b'x'*2048)
                    owner.storage.delete('scratch.tmp')
                owner.callback(cleanup)
            self.assertFalse(Path(str(root/'diagnostic-owner')+'.writer.lock').exists())
            with launcher.writer_lease(root/'diagnostic-owner'): pass  # The real exclusive handle is released.
            observed = owner.final_observation
            self.assertEqual(observed['outcome'], 'Complete')
            self.assertTrue(observed['receiptManifestPublicationObserved'])
            self.assertEqual(observed['ownerCounters']['childLogFilesCreated'], 4)
            self.assertEqual(observed['ownerCounters']['childLogFinalObservedBytes.other'],
                             sum((root/(phase+'.log')).stat().st_size for phase in w.PHASES))
            storage = observed['managedStorageAfterPublication']
            self.assertEqual(storage['currentRetainedBytes'], sum(p.stat().st_size for p in owner.storage.root.iterdir()))
            self.assertEqual(storage['deletedBytes'], 2048)
            self.assertEqual(sum(v for k,v in observed['ownerCounters'].items() if k.startswith('applicationWriteBytes.')),
                             storage['totalWrittenBytes'])
            self.assertEqual(len(owner.receipt['ledger']['workers']), 4)
            self.assertEqual(len(owner.receipt['ledger']['processObservations']), 4)
            self.assertGreaterEqual(observed['enclosingSeconds'], owner.receipt['ledger']['enclosingSeconds'])
            w.seal_new(root/'final-owner-observation.json', observed)
            w.seal_new(root/'sources.json', {str(Path(p).resolve().relative_to(ROOT).as_posix()):sha(p)
                                           for p in (w.__file__, owned.__file__, launcher.__file__, __file__)})
            w.seal_new(root/'files.json', {p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})


if __name__ == '__main__': unittest.main()
