"""Bound worker receipts and retained diagnostic ownership; no scientific launch."""
import contextlib
import hashlib
import importlib.util
import io
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

spec = importlib.util.spec_from_file_location('receipt_audit', ANALYSIS/'audit-proposal-affinity-study.py')
audit = importlib.util.module_from_spec(spec); spec.loader.exec_module(audit)


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class WorkerReceipts(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.study = self.root/'study'; self.study.mkdir()
        (self.study/'request.json').write_bytes(b'{"literal":true}')
        self.receipt, self.binding = self.root/'worker.json', self.root/'binding.json'
        self.producer = Path(__file__).resolve()

    def bind(self, **changes):
        value = dict(version=w.WORKER_BINDING_VERSION, phase='independentAudit', studyRoot=str(self.study),
                     requestSha256=sha(self.study/'request.json'), producerSha256=sha(self.producer),
                     accountingModuleSha256=sha(w.__file__), receiptPath=str(self.receipt))
        value.update(changes)
        self.binding.write_bytes(w.canonical(value))
        return sha(self.binding)

    def scope(self, pin):
        return w.worker_receipt(self.study, 'independentAudit', self.binding, pin, self.producer)

    def test_success_is_bound_and_excludes_own_publication(self):
        pin = self.bind()
        with self.scope(pin) as work:
            self.assertTrue(work.read_json(self.study/'request.json')['literal'])
        receipt = w.verify_counter_receipt(self.receipt, sha(self.receipt), 'independentAudit',
                                            sha(self.study/'request.json'), sha(self.producer))
        self.assertEqual(receipt['outcome'], 'Complete')
        self.assertEqual(receipt['counters']['applicationReadBytes.json'], self.binding.stat().st_size+3*16)
        self.assertEqual(receipt['counters']['workerAuthenticationsCompleted'], 2)
        self.assertFalse(any(k.startswith('applicationWriteBytes') for k in receipt['counters']))
        self.assertFalse(receipt['wholeProcessCoverage']); self.assertFalse(receipt['usableForAdmission'])

    def test_invalid_bindings_fail_before_action(self):
        cases = [dict(version='unknown'), dict(phase='native'), dict(studyRoot=str(self.root)),
                 dict(requestSha256='a'*64), dict(producerSha256='a'*64), dict(accountingModuleSha256='a'*64),
                 dict(receiptPath='relative.json'), dict(receiptPath=str(self.study/'bad.json')), dict(extra=True)]
        for change in cases:
            with self.subTest(change=change):
                pin = self.bind(**change)
                with self.assertRaises(ValueError), self.scope(pin): self.fail('invalid binding invoked worker')
                self.assertFalse(self.receipt.exists()); self.assertFalse((self.study/'bad.json').exists())
        self.bind()
        with self.assertRaisesRegex(ValueError, 'Changed worker binding'), self.scope('a'*64): self.fail()

    def test_duplicate_keys_and_oversize_binding_are_rejected(self):
        for fault in ('duplicate', 'size'):
            self.bind()
            raw = self.binding.read_bytes().replace(b'{', b'{"phase":"independentAudit",', 1) if fault == 'duplicate' else b'x'*16385
            self.binding.write_bytes(raw)
            with self.assertRaises(ValueError), self.scope(sha(self.binding)): self.fail()
        self.assertFalse(self.receipt.exists())

    def test_existing_output_prevents_action_and_preserves_bytes(self):
        pin = self.bind(); self.receipt.write_bytes(b'previous')
        with self.assertRaises(FileExistsError), self.scope(pin): self.fail()
        self.assertEqual(self.receipt.read_bytes(), b'previous')

    def test_failed_action_retains_partial_counters_and_same_exception(self):
        pin = self.bind(); error = RuntimeError('worker failed')
        with self.assertRaises(RuntimeError) as caught:
            with self.scope(pin) as work:
                work.read_json(self.study/'request.json')
                raise error
        self.assertIs(caught.exception, error)
        receipt = w.strict(self.receipt.read_bytes())
        self.assertEqual(receipt['outcome'], 'Failed')
        self.assertEqual(receipt['counters']['jsonParseCompleted'], 2)

    def test_cancellation_is_failed_and_retained(self):
        pin = self.bind(); error = KeyboardInterrupt()
        with self.assertRaises(KeyboardInterrupt):
            with self.scope(pin): raise error
        self.assertEqual(w.strict(self.receipt.read_bytes())['outcome'], 'Failed')

    def test_post_action_request_change_cannot_complete(self):
        pin = self.bind()
        with self.assertRaisesRegex(ValueError, 'Changed worker request'):
            with self.scope(pin): (self.study/'request.json').write_bytes(b'{"changed":true}')
        self.assertEqual(w.strict(self.receipt.read_bytes())['outcome'], 'Failed')

    def test_publication_failure_preserves_original_and_is_never_silent_on_success(self):
        pin = self.bind(); error = RuntimeError('worker failed')
        with patch.object(w.os, 'fsync', side_effect=OSError('sync failed')):
            with self.assertRaises(RuntimeError) as caught:
                with self.scope(pin): raise error
        self.assertIs(caught.exception, error)
        self.assertEqual(error.__notes__, ['Work receipt publication failed; secondary details omitted.'])
        self.receipt.unlink()
        with patch.object(w.os, 'fsync', side_effect=OSError('sync failed')):
            with self.assertRaisesRegex(OSError, 'sync failed'):
                with self.scope(pin): pass

    def test_auditor_cli_uses_real_binding_and_preserves_default_result_bytes(self):
        self.producer = Path(audit.__file__)
        pin = self.bind(); original = self.root/'default.json'; counted = self.root/'counted.json'
        def literal(root, working, pin, work=None):
            if work is not None: work.read_json(root/'request.json')
            return dict(status='LiteralFixture', text='æ', newFights=0)
        with patch.object(audit, 'audit', side_effect=literal):
            audit.main(['--working', str(self.study), '--output', str(original)])
            audit.main(['--working', str(self.study), '--output', str(counted), '--work-binding', str(self.binding), '--work-binding-pin', pin])
        self.assertEqual(original.read_bytes(), counted.read_bytes())
        self.assertEqual(w.strict(self.receipt.read_bytes())['outcome'], 'Complete')

    def test_auditor_output_error_retains_failed_receipt(self):
        self.producer = Path(audit.__file__); pin = self.bind()
        output = self.root/'out.json'; output.write_text('previous')
        with patch.object(audit, 'audit', return_value={}), self.assertRaises(FileExistsError):
            audit.main(['--working', str(self.study), '--output', str(output), '--work-binding', str(self.binding), '--work-binding-pin', pin])
        self.assertEqual(output.read_text(), 'previous')
        self.assertEqual(w.strict(self.receipt.read_bytes())['outcome'], 'Failed')

    def test_incomplete_cli_binding_pair_fails_before_audit(self):
        for args in (['--work-binding', 'x'], ['--work-binding-pin', 'a'*64]):
            with patch.object(audit, 'audit') as action, contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
                audit.main(['--working', str(self.study), *args])
            action.assert_not_called()


class RetainedWorkers(unittest.TestCase):
    setUp = WorkerReceipts.setUp
    def owner(self):
        return w.RetainedOwner(self.root/'retained', sha(self.study/'request.json'), sha(__file__), sha(w.__file__))

    def verify_owner(self, owner):
        manifest = w.strict((owner.storage.root/'files.json').read_bytes())
        self.assertEqual(sha(owner.storage.root/'files.json'), owner.manifest_sha256)
        for name, pin in manifest.items(): self.assertEqual(sha(owner.storage.root/name), pin)
        return w.strict((owner.storage.root/'owner-work.json').read_bytes())

    def test_original_invocation_failure_survives_missing_receipt(self):
        error = RuntimeError('invoke failed')
        with self.assertRaises(RuntimeError) as caught:
            with self.owner() as owner:
                with owner.phase('native', sha(self.producer)):
                    def fail(*_): raise error
                    owner.run_worker(self.study, self.producer, w.__file__, self.receipt, fail)
        self.assertIs(caught.exception, error)
        self.assertIn('retention failed', str(error.__notes__))
        self.assertEqual(self.verify_owner(owner)['ledger']['outcome'], 'Failed')

    def test_bad_worker_identity_is_not_retained_as_valid_receipt(self):
        with self.assertRaisesRegex(ValueError, 'Wrong producer'):
            with self.owner() as owner:
                with owner.phase('native', sha(self.producer)):
                    def invalid(*_): w.seal_new(self.receipt, w.Counters().receipt('native', 'a'*64, 'b'*64, True))
                    owner.run_worker(self.study, self.producer, w.__file__, self.receipt, invalid)
        self.assertFalse((owner.storage.root/'native-work.json').exists())
        self.assertEqual(self.verify_owner(owner)['ledger']['workers'], [])

    def test_failed_receipt_is_retained_even_if_invocation_returned_normally(self):
        with self.assertRaisesRegex(ValueError, 'failed receipt'):
            with self.owner() as owner:
                with owner.phase('native', sha(self.producer)):
                    def invalid(*_):
                        w.seal_new(self.receipt, w.Counters().receipt('native', sha(self.study/'request.json'), sha(self.producer), False))
                    owner.run_worker(self.study, self.producer, w.__file__, self.receipt, invalid)
        self.assertEqual(self.verify_owner(owner)['ledger']['workers'][0]['receipt']['outcome'], 'Failed')

    def test_real_literal_workers_retained_in_all_four_phases(self):
        if os.environ.get('LL_WORKER_OWNER_EXPORT'):
            self.root = Path(os.environ['LL_WORKER_OWNER_EXPORT']); self.root.mkdir()
            self.study = self.root/'study'; self.study.mkdir(); (self.study/'request.json').write_bytes(b'{"literal":true}')
        script = self.root/'literal-worker.py'
        script.write_text('import sys\nfrom pathlib import Path\nsys.path.insert(0,'+repr(str(ANALYSIS))+')\n'
                          'from proposal_work_accounting import worker_receipt\n'
                          'with worker_receipt(sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4],__file__) as work:\n'
                          '    assert work.read_json(Path(sys.argv[1])/"request.json")["literal"]\n', encoding='utf-8')
        helper_pin = sha(owned.__file__)
        with self.owner() as owner:
            for phase in w.PHASES:
                with owner.phase(phase, sha(script)):
                    path = self.root/(phase+'-work.json')
                    def invoke(binding, pin):
                        result = owned.run([sys.executable, '-B', str(script), str(self.study), phase, str(binding), pin],
                                           self.root, self.root/(phase+'.log'), time.monotonic()+8,
                                           observe=lambda value: owner.observe_process(value, helper_pin))
                        self.assertEqual(result['exitCode'], 0)
                        return result
                    owner.run_worker(self.study, script, w.__file__, path, invoke)
                    owner.callback(path.unlink)
        receipt = self.verify_owner(owner)
        self.assertEqual(receipt['ledger']['outcome'], 'Complete')
        self.assertEqual([r['receipt']['phase'] for r in receipt['ledger']['workers']], list(w.PHASES))
        self.assertEqual(len(receipt['ledger']['processObservations']), 4)
        self.assertTrue(receipt['terminalPublicationExcluded'])
        self.assertFalse(receipt['wholeProcessCoverage'])
        for phase in w.PHASES: self.assertFalse((self.root/(phase+'-work.json')).exists())

    def test_real_independent_auditor_failure_retained_by_owner(self):
        script = Path(audit.__file__)
        with self.assertRaisesRegex(ValueError, 'failed receipt'):
            with self.owner() as owner:
                with owner.phase('native', sha(self.producer)):
                    # This test uses only the independent phase; set up a ledger
                    # prefix without claiming any earlier successful work.
                    pass
                with owner.phase('nativeAudit', sha(self.producer)): pass
                with owner.phase('independentAudit', sha(script)):
                    def invoke(binding, pin):
                        result = owned.run([sys.executable, '-B', '-X', 'utf8', str(script), '--working', str(self.study),
                                            '--work-binding', str(binding), '--work-binding-pin', pin], self.root,
                                           self.root/'independent.log', time.monotonic()+8,
                                           observe=lambda value: owner.observe_process(value, sha(owned.__file__)))
                        self.assertNotEqual(result['exitCode'], 0)
                    owner.run_worker(self.study, script, w.__file__, self.receipt, invoke)
        receipt = self.verify_owner(owner)
        self.assertEqual(receipt['ledger']['outcome'], 'Failed')
        worker = receipt['ledger']['workers'][0]['receipt']
        self.assertEqual(worker['phase'], 'independentAudit')
        self.assertEqual(worker['outcome'], 'Failed')
        self.assertGreater(worker['counters']['applicationReadBytes.json'], 0)

    @unittest.skipUnless(os.environ.get('LL_WORKER_RECEIPT_DLL'), 'requires isolated native test build')
    def test_real_native_command_failure_is_retained_by_owner(self):
        dll = Path(os.environ['LL_WORKER_RECEIPT_DLL']).resolve()
        with self.assertRaisesRegex(ValueError, 'failed receipt'):
            with self.owner() as owner:
                with owner.phase('native', sha(dll)): pass
                with owner.phase('nativeAudit', sha(dll)):
                    def invoke(binding, pin):
                        result = owned.run(['dotnet', str(dll), 'tower-proposal-study-audit', str(self.study),
                                            '--work-binding', str(binding), pin], dll.parent,
                                           self.root/'native.log', time.monotonic()+10,
                                           observe=lambda value: owner.observe_process(value, sha(owned.__file__)))
                        self.assertNotEqual(result['exitCode'], 0)
                    owner.run_worker(self.study, dll, dll, self.receipt, invoke)
        receipt = self.verify_owner(owner)
        self.assertEqual(receipt['ledger']['outcome'], 'Failed')
        worker = receipt['ledger']['workers'][0]['receipt']
        self.assertEqual(worker['producerSha256'], sha(dll))
        self.assertEqual(worker['outcome'], 'Failed')
        self.assertGreater(worker['counters']['applicationReadBytes.json'], 0)

    @unittest.skipUnless(os.environ.get('LL_WORKER_RECEIPT_EXCHANGE'), 'requires fresh native receipt exchange')
    def test_native_export_authenticates_cross_language_contract(self):
        exchange = Path(os.environ['LL_WORKER_RECEIPT_EXCHANGE'])
        self.assertEqual(sha(exchange/'files.json'), os.environ['LL_WORKER_RECEIPT_EXCHANGE_PIN'])
        manifest = w.strict((exchange/'files.json').read_bytes())
        self.assertEqual(set(manifest), {p.name for p in exchange.iterdir()}-{'files.json'})
        for name, pin in manifest.items(): self.assertEqual(sha(exchange/name), pin)
        producer = w.strict((exchange/'producer.json').read_bytes())
        binding = w.strict((exchange/'binding.json').read_bytes())
        self.assertEqual(sha(producer['path']), producer['sha256'])
        self.assertEqual(sha(exchange/'binding.json'), producer['bindingSha256'])
        self.assertEqual(binding['producerSha256'], producer['sha256'])
        self.assertEqual(binding['requestSha256'], sha(exchange/'request.json'))
        receipt = w.verify_counter_receipt(exchange/'native-work.json', manifest['native-work.json'],
                                            'nativeAudit', binding['requestSha256'], binding['producerSha256'])
        self.assertEqual(receipt['outcome'], 'Complete')
        if producer.get('nativeBindingAuthenticationCounted', False):
            counts = receipt['counters']
            self.assertEqual(counts['applicationReadBytes.json'], (exchange/'binding.json').stat().st_size+3*(exchange/'request.json').stat().st_size)
            self.assertEqual(counts['applicationReadBytes.other'], 2*Path(producer['path']).stat().st_size)
            self.assertEqual(counts['jsonInputBytes'], (exchange/'binding.json').stat().st_size+(exchange/'request.json').stat().st_size)
            self.assertEqual(counts['workerAuthenticationsCompleted'], 2)
            self.assertEqual(counts['jsonParseCompleted'], 2)
        else:
            # Historical exports retain their original narrower coverage.
            self.assertEqual(receipt['counters']['applicationReadBytes.json'], 16)
        self.assertFalse(receipt['wholeProcessCoverage']); self.assertFalse(receipt['usableForAdmission'])


if __name__ == '__main__': unittest.main()
