"""Literal Python worker I/O accounting; no scientific launch or combat."""
import contextlib
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
ANALYSIS = ROOT/'Balance Harness/analysis'
sys.path.insert(0, str(ANALYSIS))
import proposal_work_accounting as w
spec = importlib.util.spec_from_file_location('worker_io_auditor', ANALYSIS/'audit-proposal-affinity-study.py')
audit = importlib.util.module_from_spec(spec); spec.loader.exec_module(audit)


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class WorkerIO(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.prepare(Path(self.temp.name))

    def prepare(self, root):
        self.root = root; root.mkdir(exist_ok=True)
        self.study = root/'study'; self.study.mkdir()
        self.request = self.study/'request.json'; self.request.write_bytes(b'{"fixtureOnly":true}')
        self.binding, self.receipt, self.output = (root/name for name in ('binding.json','worker.json','result.json'))
        self.producer = root/'producer.py'; self.producer.write_bytes(b'# literal producer\n')

    def bind(self):
        value = dict(version=w.WORKER_BINDING_VERSION, phase='independentAudit', studyRoot=str(self.study),
            requestSha256=sha(self.request), producerSha256=sha(self.producer), accountingModuleSha256=sha(w.__file__),
            receiptPath=str(self.receipt))
        self.binding.write_bytes(w.canonical(value))
        return sha(self.binding)

    def scope(self):
        return w.worker_receipt(self.study,'independentAudit',self.binding,self.bind(),self.producer)

    def retained(self): return w.strict(self.receipt.read_bytes())

    def fake_output(self, stream):
        original = Path.open
        def opened(path, *args, **kwargs):
            return stream if path == self.output else original(path, *args, **kwargs)
        return patch.object(Path, 'open', new=opened)

    def test_binding_and_both_authentication_passes_count_actual_reads(self):
        with self.scope(): pass
        counts = self.retained()['counters']
        self.assertEqual(counts['applicationReadBytes.json'], self.binding.stat().st_size+2*self.request.stat().st_size)
        self.assertEqual(counts['applicationReadBytes.other'], 2*(self.producer.stat().st_size+Path(w.__file__).stat().st_size))
        self.assertEqual(counts['jsonInputBytes'], self.binding.stat().st_size)
        self.assertEqual(counts['jsonParseAttempts'], 1); self.assertEqual(counts['jsonParseCompleted'], 1)
        self.assertEqual(counts['workerAuthenticationsAttempted'], 2)
        self.assertEqual(counts['workerAuthenticationsCompleted'], 2)
        self.assertFalse(any('Write' in key or 'Close' in key for key in counts))

    def test_changed_request_counts_failed_authentication_without_unperformed_reads(self):
        original = self.request.stat().st_size
        with self.assertRaisesRegex(ValueError,'Changed worker request'):
            with self.scope(): self.request.write_bytes(b'changed')
        receipt = self.retained(); counts = receipt['counters']
        self.assertEqual(receipt['outcome'], 'Failed')
        self.assertEqual(counts['applicationReadBytes.json'], self.binding.stat().st_size+original+7)
        self.assertEqual(counts['applicationReadBytes.other'], self.producer.stat().st_size+Path(w.__file__).stat().st_size)
        self.assertEqual(counts['workerAuthenticationsCompleted'], 1)
        self.assertEqual(counts['workerAuthenticationsFailed'], 1)

    def test_changed_producer_does_not_charge_skipped_module_read(self):
        original = self.producer.stat().st_size
        with self.assertRaisesRegex(ValueError,'Changed worker request'):
            with self.scope(): self.producer.write_bytes(b'changed')
        counts = self.retained()['counters']
        self.assertEqual(counts['applicationReadBytes.other'], original+7+Path(w.__file__).stat().st_size)
        self.assertEqual(counts['workerAuthenticationsFailed'], 1)

    def test_body_error_preserves_original_and_only_completed_authentication(self):
        error = KeyboardInterrupt('literal cancellation')
        with self.assertRaises(KeyboardInterrupt) as caught:
            with self.scope(): raise error
        self.assertIs(caught.exception,error)
        self.assertEqual(self.retained()['counters']['workerAuthenticationsAttempted'],1)
        self.assertEqual(self.retained()['outcome'],'Failed')

    def test_cli_file_bytes_match_default_and_retains_output_counters(self):
        export = os.environ.get('LL_WORKER_IO_EXPORT')
        if export: self.prepare(Path(export))
        self.producer = Path(audit.__file__); binding_pin = self.bind()
        literal = dict(fixtureOnly=True, text='æ🐉', rows=[1,2], newFights=0, newValues=0)
        default = self.root/'default.json'
        with patch.object(audit,'audit',return_value=literal):
            audit.main(['--working',str(self.study),'--output',str(default)])
            audit.main(['--working',str(self.study),'--output',str(self.output),
                        '--work-binding',str(self.binding),'--work-binding-pin',binding_pin])
        self.assertEqual(default.read_bytes(),self.output.read_bytes())
        receipt = w.verify_counter_receipt(self.receipt,sha(self.receipt),'independentAudit',sha(self.request),sha(self.producer))
        counts = receipt['counters']
        self.assertEqual(counts['textOutputAcceptedUtf8Bytes.json'],len(json.dumps(literal,indent=2).encode('utf-8')))
        self.assertEqual(counts['jsonOutputOperationsCompleted'],1)
        self.assertEqual(counts['textOutputOpensCompleted'],1); self.assertEqual(counts['textOutputClosesCompleted'],1)
        self.assertFalse(any(k.startswith('applicationWriteBytes') for k in counts))
        self.assertFalse(receipt['wholeProcessCoverage']); self.assertFalse(receipt['usableForAdmission'])
        if export:
            w.seal_new(self.root/'fixture.json',dict(fixtureOnly=True,auditComputationStubbed=True,
                actualCombat=0,productionEntropyDraws=0,wholeProcessCoverage=False,usableForAdmission=False,
                textAcceptedBytesBeforeNewlineTranslation=counts['textOutputAcceptedUtf8Bytes.json'],
                finalOutputFileBytes=self.output.stat().st_size,
                sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p) for p in (w.__file__,audit.__file__,__file__)}))
            w.seal_new(self.root/'files.json',{p.relative_to(self.root).as_posix():sha(p) for p in sorted(self.root.rglob('*')) if p.is_file()})

    def test_existing_output_is_preserved_and_open_failure_retained(self):
        self.output.write_bytes(b'previous')
        with self.assertRaises(FileExistsError):
            with self.scope() as work: work.write_json_output(self.output,{})
        self.assertEqual(self.output.read_bytes(),b'previous')
        counts = self.retained()['counters']
        self.assertEqual(counts['textOutputOpensFailed'],1)
        self.assertNotIn('textOutputClosesAttempted',counts)
        self.assertEqual(self.retained()['outcome'],'Failed')

    def test_serialization_failure_keeps_default_partial_output_and_counters(self):
        literal = dict(first='ok',bad=object())
        default = self.root/'default.json'
        with self.assertRaises(TypeError):
            with default.open('x',encoding='utf-8') as stream: json.dump(literal,stream,indent=2)
        with self.assertRaises(TypeError):
            with self.scope() as work: work.write_json_output(self.output,literal)
        self.assertEqual(self.output.read_bytes(),default.read_bytes())
        self.assertEqual(self.retained()['counters']['textOutputAcceptedUtf8Bytes.json'],
                         len(self.output.read_text(encoding='utf-8').encode('utf-8')))
        self.assertEqual(self.retained()['counters']['textOutputClosesCompleted'],1)

    def test_close_failure_prevents_complete_receipt(self):
        class CloseFailure(io.StringIO):
            def close(self):
                super().close()
                raise OSError('close failed')
        stream = CloseFailure()
        with self.fake_output(stream), self.assertRaisesRegex(OSError,'close failed'):
            with self.scope() as work: work.write_json_output(self.output,{})
        self.assertTrue(stream.closed)
        self.assertEqual(self.retained()['outcome'],'Failed')
        self.assertEqual(self.retained()['counters']['textOutputClosesFailed'],1)
        self.assertNotIn('textOutputClosesCompleted',self.retained()['counters'])

    def test_thrown_write_marks_unknown_progress_and_still_closes(self):
        class WriteFailure(io.StringIO):
            def write(self,text):
                super().write(text[:1])
                raise OSError('write failed')
        stream = WriteFailure()
        with self.fake_output(stream), self.assertRaisesRegex(OSError,'write failed'):
            with self.scope() as work: work.write_json_output(self.output,{})
        counts = self.retained()['counters']
        self.assertTrue(stream.closed)
        self.assertEqual(counts['failedTextOutputWriteBytesUnknown'],1)
        self.assertNotIn('textOutputAcceptedUtf8Bytes.json',counts)

    def test_short_write_counts_accepted_prefix_and_prevents_success(self):
        class ShortWrite(io.StringIO):
            def write(self,text): return super().write(text[:1])
        stream = ShortWrite()
        with self.fake_output(stream), self.assertRaisesRegex(OSError,'Short JSON output write'):
            with self.scope() as work: work.write_json_output(self.output,{})
        counts = self.retained()['counters']
        self.assertTrue(stream.closed)
        self.assertEqual(counts['textOutputAcceptedUtf8Bytes.json'],1)
        self.assertEqual(counts['textOutputShortWrites'],1)
        self.assertNotIn('failedTextOutputWriteBytesUnknown',counts)

    def test_invalid_write_result_cannot_claim_accepted_bytes(self):
        for result in (None,True,-1,99):
            with self.subTest(result=result):
                class InvalidWrite(io.StringIO):
                    def write(self,text): return result
                work = w.Counters(); stream = InvalidWrite()
                with self.fake_output(stream), self.assertRaisesRegex(ValueError,'Invalid text output write count'):
                    work.write_json_output(self.output,{})
                self.assertTrue(stream.closed)
                self.assertNotIn('textOutputAcceptedUtf8Bytes.json',work.values)
                self.assertEqual(work.values['failedTextOutputWriteBytesUnknown'],1)

    def test_original_write_error_survives_secondary_close_failure(self):
        error = RuntimeError('primary')
        class BothFail(io.StringIO):
            def write(self,text): raise error
            def close(self):
                super().close()
                raise OSError('secondary')
        with self.fake_output(BothFail()), self.assertRaises(RuntimeError) as caught:
            with self.scope() as work: work.write_json_output(self.output,{})
        self.assertIs(caught.exception,error)
        self.assertIn('secondary',str(error.__notes__))
        self.assertEqual(self.retained()['outcome'],'Failed')

    def test_caught_output_failure_cannot_be_relabelled_complete(self):
        self.output.write_bytes(b'previous')
        with self.assertRaisesRegex(ValueError,'caught a failed JSON output'):
            with self.scope() as work:
                try: work.write_json_output(self.output,{})
                except FileExistsError: pass
        self.assertEqual(self.retained()['outcome'],'Failed')

    def test_output_preserves_default_no_fsync_semantics(self):
        work = w.Counters()
        with patch.object(w.os,'fsync') as sync: work.write_json_output(self.output,{})
        sync.assert_not_called()
        self.assertEqual(self.output.read_bytes(),b'{}')
        self.assertEqual(work.values['textOutputClosesCompleted'],1)

    def test_default_stdout_bytes_unchanged_and_not_claimed_as_file_io(self):
        self.producer = Path(audit.__file__); binding_pin = self.bind()
        default, counted = io.StringIO(), io.StringIO()
        with patch.object(audit,'audit',return_value=dict(text='æ',fixtureOnly=True)):
            with contextlib.redirect_stdout(default): audit.main(['--working',str(self.study)])
            with contextlib.redirect_stdout(counted):
                audit.main(['--working',str(self.study),'--work-binding',str(self.binding),'--work-binding-pin',binding_pin])
        self.assertEqual(default.getvalue(),counted.getvalue())
        self.assertFalse(any('Output' in name for name in self.retained()['counters']))

    def test_authentication_after_output_close_detects_changed_request(self):
        request = self.request
        class ChangingClose(io.StringIO):
            def close(self):
                super().close()
                request.write_bytes(b'changed during close')
        with self.fake_output(ChangingClose()), self.assertRaisesRegex(ValueError,'Changed worker request'):
            with self.scope() as work: work.write_json_output(self.output,{})
        counts = self.retained()['counters']
        self.assertEqual(counts['jsonOutputOperationsCompleted'],1)
        self.assertEqual(counts['workerAuthenticationsFailed'],1)
        self.assertEqual(self.retained()['outcome'],'Failed')


if __name__ == '__main__': unittest.main()
