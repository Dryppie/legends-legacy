"""V3 worker publication-observation persistence; literal work only."""
import copy
import importlib.util
import io
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as w
import proposal_owner_supervisor as supervisor
import bounded_windows_process as owned


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT/path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


pub = load('publication_fixture', 'build/test-proposal-receipt-publication.py')
owner = load('owner_fixture', 'build/test-proposal-owner-supervisor.py')
sha = pub.sha


class Persistence(unittest.TestCase):
    def setUp(self):
        self.f = pub.Publication()
        self.f.setUp()
        self.addCleanup(self.f.doCleanups)
        self.base = self.f.root
        self.persistence = self.base/'persistence.json'

    def bind(self, **changes):
        values = dict(version=w.WORKER_PERSISTENCE_BINDING_VERSION, publicationPersistencePath=str(self.persistence))
        values.update(changes)
        return self.f.bind(**values)

    def observed(self):
        return w.worker_publication_persistence(self.persistence.read_bytes(), sha(self.f.binding), 'independentAudit',
                                               sha(self.f.request), sha(self.f.producer))

    def test_success_counts_exact_observation_bytes_through_close(self):
        self.bind()
        with self.f.scope() as counters:
            counters.add('literalWork')
        value = self.observed()
        self.assertEqual(value['outcome'], 'Complete')
        self.assertEqual(value['serializedObservationSha256'], sha(self.f.publication))
        self.assertEqual(value['serializedObservationBytes'], self.f.publication.stat().st_size)
        self.assertEqual(value['counters']['acceptedWriteBytes'], self.f.publication.stat().st_size)
        for op in ('open','serialize','write','flush','sync','close'):
            self.assertEqual(value['counters'][op+'Attempted'], 1)
            self.assertEqual(value['counters'][op+'Completed'], 1)
        self.assertTrue(value['terminalObservationPersistenceExcluded'])
        self.assertTrue(self.f.observed()['observationPersistenceExcluded'])
        self.assertFalse(value['wholeProcessCoverage'])
        self.assertFalse(value['usableForAdmission'])
        self.assertNotIn('acceptedWriteBytes', w.strict(self.f.receipt.read_bytes())['counters'])

    def test_invalid_bindings_create_no_sidecars_or_action(self):
        edits = [dict(publicationPersistencePath=str(self.f.receipt)), dict(publicationPersistencePath=str(self.f.publication)),
            dict(publicationPersistencePath=str(self.f.study/'bad.json')), dict(publicationPersistencePath='relative'),
            dict(version=w.WORKER_PUBLICATION_BINDING_VERSION), dict(version=w.WORKER_BINDING_VERSION),
            dict(publicationPersistencePath=None), dict(extra=True)]
        for edit in edits:
            self.bind(**edit)
            with self.subTest(edit=edit), self.assertRaises((ValueError, TypeError)), self.f.scope():
                self.fail('Action invoked')
            self.assertFalse(self.persistence.exists())
            self.assertFalse(self.f.receipt.exists())
            self.assertFalse(self.f.publication.exists())
        for missing in ('publicationPath','publicationPersistencePath'):
            self.bind(); value = w.strict(self.f.binding.read_bytes()); value.pop(missing)
            self.f.binding.write_bytes(w.canonical(value))
            with self.assertRaises(ValueError), self.f.scope():
                self.fail('Action invoked')
            self.assertFalse(self.persistence.exists())

    def test_changed_binding_identity_rejects_before_terminal_reservation(self):
        self.bind(producerSha256='a'*64)
        with self.assertRaisesRegex(ValueError, 'Changed worker'), self.f.scope():
            self.fail('Action invoked')
        self.assertFalse(self.persistence.exists())

    def test_existing_terminal_preserves_bytes_before_other_reservations(self):
        self.bind(); self.persistence.write_bytes(b'previous')
        with self.assertRaises(FileExistsError), self.f.scope():
            self.fail('Action invoked')
        self.assertEqual(self.persistence.read_bytes(), b'previous')
        self.assertFalse(self.f.receipt.exists())
        self.assertFalse(self.f.publication.exists())

    def test_publication_open_failure_is_retained_without_worker_action(self):
        self.bind(); self.f.publication.write_bytes(b'previous')
        with self.assertRaises(FileExistsError), self.f.scope():
            self.fail('Action invoked')
        value = self.observed()
        self.assertEqual(value['outcome'], 'Failed')
        self.assertEqual(value['counters'], dict(openAttempted=1, openFailed=1))
        self.assertIsNone(value['serializedObservationSha256'])
        self.assertFalse(self.f.receipt.exists())
        self.assertEqual(self.f.publication.read_bytes(), b'previous')

    def test_receipt_open_failure_has_complete_failed_observation_persistence(self):
        self.bind(); self.f.receipt.write_bytes(b'previous')
        with self.assertRaises(FileExistsError), self.f.scope():
            self.fail('Action invoked')
        self.assertEqual(self.observed()['outcome'], 'Complete')
        self.assertEqual(self.f.observed()['outcome'], 'Failed')
        self.assertEqual(self.f.receipt.read_bytes(), b'previous')

    def test_worker_failure_is_separate_from_complete_persistence(self):
        self.bind(); original = KeyboardInterrupt('literal cancellation')
        with self.assertRaises(KeyboardInterrupt) as caught:
            with self.f.scope():
                raise original
        self.assertIs(caught.exception, original)
        self.assertEqual(self.observed()['outcome'], 'Complete')
        self.assertEqual(w.strict(self.f.receipt.read_bytes())['outcome'], 'Failed')

    def fake_publication(self, stream):
        original = Path.open
        def opened(path, *args, **kwargs):
            return stream if path == self.f.publication else original(path, *args, **kwargs)
        return patch.object(Path, 'open', opened)

    def test_short_writes_count_only_accepted_observation_bytes(self):
        class Short(io.BytesIO):
            def write(self, raw): return super().write(raw[:13])
            def fileno(self): return 1234567
            def close(self): self.saved = self.getvalue(); super().close()
        self.bind(); stream = Short(); sync = w.os.fsync
        with self.fake_publication(stream), patch.object(w.os, 'fsync', side_effect=lambda fd:None if fd == 1234567 else sync(fd)):
            with self.f.scope(): pass
        value = self.observed()
        self.assertEqual(value['outcome'], 'Complete')
        self.assertGreater(value['counters']['shortWrites'], 0)
        self.assertEqual(value['counters']['acceptedWriteBytes'], len(stream.saved))
        self.assertEqual(value['serializedObservationBytes'], len(stream.saved))
        self.assertTrue(stream.closed)

    def test_partial_failed_write_keeps_known_prefix_and_unknown_remainder(self):
        class Failed(io.BytesIO):
            calls = 0
            def write(self, raw):
                self.calls += 1
                if self.calls == 1: return super().write(raw[:11])
                super().write(raw[:2]); raise OSError('literal later write failure')
        self.bind(); stream = Failed()
        with self.fake_publication(stream), self.assertRaisesRegex(OSError, 'later write failure'):
            with self.f.scope(): pass
        value = self.observed()
        self.assertEqual(value['outcome'], 'Failed')
        self.assertEqual(value['counters']['acceptedWriteBytes'], 11)
        self.assertEqual(value['counters']['failedWriteBytesUnknown'], 1)
        self.assertEqual(value['counters']['closeCompleted'], 1)

    def test_zero_write_is_failed_unknown_progress(self):
        class Zero(io.BytesIO):
            def write(self, raw): return 0
        self.bind()
        with self.fake_publication(Zero()), self.assertRaisesRegex(ValueError, 'Incomplete worker receipt write'):
            with self.f.scope(): pass
        value = self.observed()
        self.assertEqual(value['counters']['failedWriteBytesUnknown'], 1)
        self.assertNotIn('acceptedWriteBytes', value['counters'])

    def test_serialization_failure_closes_observation_and_keeps_unknown_size(self):
        self.bind(); original = w.canonical
        def canonical(value):
            if value.get('version') == w.WORKER_PUBLICATION_VERSION:
                raise TypeError('literal serialization failure')
            return original(value)
        with patch.object(w, 'canonical', side_effect=canonical), self.assertRaisesRegex(TypeError, 'serialization failure'):
            with self.f.scope(): pass
        value = self.observed()
        self.assertEqual(value['counters']['serializeFailed'], 1)
        self.assertEqual(value['counters']['closeCompleted'], 1)
        self.assertIsNone(value['serializedObservationBytes'])

    def test_flush_sync_and_close_failures_retain_distinct_calls(self):
        for operation in ('flush','sync','close'):
            self.f.prepare(self.base/operation); self.persistence = self.f.root/'persistence.json'; self.bind()
            class Failed(io.BytesIO):
                def fileno(self): return 1234567
                def flush(self):
                    if operation == 'flush': raise OSError('literal flush failure')
                    return super().flush()
                def close(self):
                    super().close()
                    if operation == 'close': raise OSError('literal close failure')
            stream = Failed(); original = w.os.fsync
            def sync(fd):
                if fd != 1234567: return original(fd)
                if operation == 'sync': raise OSError('literal sync failure')
            with self.subTest(operation=operation), self.fake_publication(stream), patch.object(w.os, 'fsync', side_effect=sync):
                with self.assertRaisesRegex(OSError, operation+' failure'):
                    with self.f.scope(): pass
            value = self.observed()
            self.assertEqual(value['outcome'], 'Failed')
            self.assertEqual(value['counters'][operation+'Failed'], 1)
            self.assertEqual(value['counters']['closeAttempted'], 1)
            self.assertEqual(value['counters']['acceptedWriteBytes'], value['serializedObservationBytes'])

    def test_publication_write_and_close_failures_preserve_worker_error(self):
        class Failed(io.BytesIO):
            def write(self, raw): raise OSError('literal write failure')
            def close(self): super().close(); raise OSError('literal close failure')
        self.bind(); error = RuntimeError('literal worker error')
        with self.fake_publication(Failed()), self.assertRaises(RuntimeError) as caught:
            with self.f.scope(): raise error
        self.assertIs(caught.exception, error)
        self.assertEqual(error.__notes__, ['Publication observation persistence failed; secondary details omitted.'])
        value = self.observed()
        self.assertEqual(value['counters']['writeFailed'], 1)
        self.assertEqual(value['counters']['closeFailed'], 1)

    def test_terminal_persistence_failure_never_replaces_worker_error(self):
        self.bind(); original = Path.open; error = RuntimeError('literal worker failure')
        class Failed(io.BytesIO):
            def write(self, raw): raise OSError('terminal write failed')
        stream = Failed()
        def opened(path, *args, **kwargs):
            return stream if path == self.persistence else original(path, *args, **kwargs)
        with patch.object(Path, 'open', opened), self.assertRaises(RuntimeError) as caught:
            with self.f.scope(): raise error
        self.assertIs(caught.exception, error)
        self.assertEqual(error.__notes__, ['Terminal publication persistence failed; secondary details omitted.'])
        self.assertTrue(stream.closed)

    def test_validator_rejects_forged_scope_bindings_counters_and_outcome(self):
        self.bind()
        with self.f.scope(): pass
        value = self.observed()
        edits = [dict(version=w.WORKER_PUBLICATION_VERSION), dict(coverage='Complete'), dict(boundary='BeforeClose'),
            dict(terminalObservationPersistenceExcluded=False), dict(wholeProcessCoverage=True), dict(usableForAdmission=True),
            dict(requestSha256='a'*64), dict(serializedObservationSha256='bad'), dict(serializedObservationBytes=0),
            dict(extra=True), dict(serializedReceiptBytes=1), dict(outcome='Failed')]
        for edit in edits:
            with self.subTest(edit=edit), self.assertRaises(ValueError):
                w.worker_publication_persistence(w.canonical(dict(value, **edit)), sha(self.f.binding), 'independentAudit',
                    sha(self.f.request), sha(self.f.producer))
        for key, amount in (('closeCompleted',0), ('writeAttempted',0), ('acceptedWriteBytes',-1), ('syncCompleted',True)):
            changed = copy.deepcopy(value); changed['counters'][key] = amount
            with self.subTest(key=key), self.assertRaises(ValueError):
                w.worker_publication_persistence(w.canonical(changed), sha(self.f.binding), 'independentAudit',
                    sha(self.f.request), sha(self.f.producer))


class OwnerPersistence(unittest.TestCase):
    def setUp(self):
        self.f = owner.Supervisor(); self.f.setUp(); self.addCleanup(self.f.doCleanups)

    def prepare(self, base=None):
        old = self.f.prepare(base)
        return supervisor.StudyWorkSupervisor(old.root, worker_observation_persistence=True)

    def test_all_four_opt_in_workers_are_bound_verified_and_retained(self):
        export = os.environ.get('LL_OBSERVATION_PERSISTENCE_EXPORT')
        base = Path(export) if export else self.f.base
        s = self.prepare(base)
        self.f.launch(s); self.f.verify(s); self.f.verify_archive_resources()
        self.assertEqual(s.terminal_observation['outcome'], 'Complete')
        for phase in w.PHASES:
            binding = s.root/'owner'/(phase+'-binding.json')
            b = w.strict(binding.read_bytes())
            self.assertEqual(b['version'], w.WORKER_PERSISTENCE_BINDING_VERSION)
            observation = s.root/'owner'/(phase+'-receipt-publication.json')
            retained = s.root/'owner'/(phase+'-publication-persistence.json')
            value = w.worker_publication_persistence(retained.read_bytes(), sha(binding), phase, b['requestSha256'], b['producerSha256'])
            self.assertEqual(value['outcome'], 'Complete')
            self.assertEqual(value['serializedObservationSha256'], sha(observation))
            self.assertEqual(value['counters']['acceptedWriteBytes'], observation.stat().st_size)
            self.assertEqual(retained.read_bytes(), Path(b['publicationPersistencePath']).read_bytes())
        if export:
            w.seal_new(base/'fixture.json', dict(fixtureOnly=True, routedWorkers=True, realProcessObservations=False,
                bindingVersion=w.WORKER_PERSISTENCE_BINDING_VERSION, actualCombat=0, productionEntropyDraws=0,
                sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p) for p in
                    (w.__file__, supervisor.__file__, owner.__file__, owner.launcher.__file__, __file__)}))
            w.seal_new(base/'terminal-observation.json', s.terminal_observation)
            w.seal_new(base/'files.json', {p.relative_to(base).as_posix():sha(p) for p in sorted(base.rglob('*')) if p.is_file()})

    def test_missing_persistence_receipt_fails_owner_even_when_worker_succeeds(self):
        s = self.prepare(); routed = self.f.routed
        def missing(*args, **kwargs):
            result = routed(*args, **kwargs)
            for path in (s.root/'worker-receipts').glob('*-publication-persistence.json'): path.unlink()
            return result
        with patch.object(self.f, 'routed', side_effect=missing), self.assertRaises(FileNotFoundError):
            self.f.launch(s)
        self.assertEqual(s.terminal_observation['outcome'], 'Failed')

    def test_changed_observation_rejected_against_persistence_hash(self):
        s = self.prepare(); routed = self.f.routed
        def changed(*args, **kwargs):
            result = routed(*args, **kwargs)
            path = s.root/'worker-receipts/native-receipt-publication.json'
            raw = path.read_bytes(); path.write_bytes(raw+b' ')
            return result
        with patch.object(self.f, 'routed', side_effect=changed), self.assertRaisesRegex(ValueError, 'Changed persisted publication'):
            self.f.launch(s)
        self.assertEqual(s.terminal_observation['outcome'], 'Failed')

    def test_worker_error_survives_terminal_retention_failure(self):
        s = self.prepare(); self.f.failed_phase = 'native'; original = w.RetainedOwner._seal_raw
        def seal(instance, name, raw):
            if name.endswith('-publication-persistence.json'): raise OSError('literal retention failure')
            return original(instance, name, raw)
        with patch.object(w.RetainedOwner, '_seal_raw', seal), self.assertRaisesRegex(ValueError, 'Native comparison failed'):
            self.f.launch(s)
        self.assertEqual(s.terminal_observation['outcome'], 'Failed')

    def test_retained_owner_rejects_overlapping_or_existing_persistence_before_invoke(self):
        study = self.f.base/'study'; study.mkdir(); (study/'request.json').write_bytes(b'{}')
        producer = Path(w.__file__)
        for index, target in enumerate(('missing-publication','receipt','publication','owner','existing','study')):
            root = self.f.base/str(index); root.mkdir()
            receipt, publication, persistence = (root/n for n in ('worker.json','publication.json','persistence.json'))
            if target == 'missing-publication': publication = None
            elif target == 'receipt': persistence = receipt
            elif target == 'publication': persistence = publication
            elif target == 'owner': persistence = root/'owner/persistence.json'
            elif target == 'existing': persistence.write_bytes(b'previous')
            elif target == 'study': persistence = study/'persistence.json'
            retained = w.RetainedOwner(root/'owner', sha(study/'request.json'), sha(producer), sha(producer))
            with self.subTest(target=target), self.assertRaises(ValueError):
                with retained:
                    with retained.phase('native', sha(producer)):
                        retained.run_worker(study, producer, producer, receipt, lambda *_:self.fail('Invoked'),
                            publication_path=publication, publication_persistence_path=persistence)
            self.assertFalse(receipt.exists())


if __name__ == '__main__': unittest.main()
