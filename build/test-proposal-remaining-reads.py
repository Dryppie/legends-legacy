"""Exact text-read/parse semantics and routed owner integration; no scientific work."""
import importlib.util
import io
import json
import math
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as w

spec = importlib.util.spec_from_file_location('remaining_read_fixture', ROOT/'build/test-proposal-owner-supervisor.py')
f = importlib.util.module_from_spec(spec); spec.loader.exec_module(f)
owner = f.launcher


class RemainingReads(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.work = w.OwnerFileCounters()

    def check_text(self, text, codec, encoding):
        path = self.root/'value.json'; raw = text.encode(codec); path.write_bytes(raw)
        expected = owner.read_json(path, encoding=encoding)
        with owner.file_accounting(self.work): actual = owner.read_json(path, encoding=encoding)
        self.assertEqual(actual, expected)
        self.assertEqual(self.work.values['applicationReadBytes.json'], len(raw))
        self.assertEqual(self.work.values['jsonInputBytes'], len(path.read_text(encoding=encoding).encode('utf-8')))
        self.assertEqual(self.work.values['textReadOperationsCompleted'], 1)
        self.assertEqual(self.work.values['jsonParseCompleted'], 1)
        path.unlink()  # A completed read does not retain the file handle.

    def test_utf8_and_newline_normalization(self):
        self.check_text('{\r\n"value":"æ漢字",\r"array":[1,2]}\n', 'utf-8', 'utf-8')

    def test_utf8_bom_is_removed_only_by_requested_codec(self):
        self.check_text('{"value":"æ"}', 'utf-8-sig', 'utf-8-sig')

    def test_utf16_uses_original_decoder_not_utf8_bytes(self):
        self.check_text('{\r\n"value":"漢字"}', 'utf-16', 'utf-16')

    def test_utf32_uses_original_decoder_not_utf8_bytes(self):
        self.check_text('{"value":2}', 'utf-32', 'utf-32')

    def test_default_locale_encoding_is_preserved(self):
        self.check_text('{"ascii":true}', 'ascii', None)

    def test_existing_json_duplicate_and_nonfinite_semantics_are_preserved(self):
        path = self.root/'value.json'; path.write_bytes(b'{"x":1,"x":2,"n":NaN,"i":Infinity}')
        with owner.file_accounting(self.work): value = owner.read_json(path)
        self.assertEqual(value['x'], 2); self.assertTrue(math.isnan(value['n'])); self.assertEqual(value['i'], float('inf'))
        self.assertEqual(self.work.values['jsonParseCompleted'], 1)

    def test_malformed_json_keeps_completed_read_and_failed_parse(self):
        path = self.root/'value.json'; path.write_bytes(b'{"bad":')
        with self.assertRaises(json.JSONDecodeError) as expected: owner.read_json(path)
        with owner.file_accounting(self.work), self.assertRaises(json.JSONDecodeError) as actual: owner.read_json(path)
        self.assertEqual(str(actual.exception), str(expected.exception))
        self.assertEqual(self.work.values['applicationReadBytes.json'], 7)
        self.assertEqual(self.work.values['textReadOperationsCompleted'], 1)
        self.assertEqual(self.work.values['jsonParseFailed'], 1)
        self.assertEqual(self.work.values['jsonParseCompleted'], 0)

    def test_utf8_bom_without_sig_remains_a_json_error(self):
        path = self.root/'value.json'; path.write_bytes(b'\xef\xbb\xbf{}')
        with self.assertRaises(json.JSONDecodeError) as expected: owner.read_json(path, encoding='utf-8')
        with owner.file_accounting(self.work), self.assertRaises(json.JSONDecodeError) as actual: owner.read_json(path, encoding='utf-8')
        self.assertEqual(str(actual.exception), str(expected.exception))
        self.assertEqual(self.work.values['jsonInputBytes'], 5)

    def test_decode_failure_never_starts_a_parse(self):
        path = self.root/'value.json'; path.write_bytes(b'{"bad":"\xff"}')
        with self.assertRaises(UnicodeDecodeError) as expected: owner.read_json(path, encoding='utf-8')
        with owner.file_accounting(self.work), self.assertRaises(UnicodeDecodeError) as actual: owner.read_json(path, encoding='utf-8')
        self.assertEqual(str(actual.exception), str(expected.exception))
        self.assertEqual(self.work.values['textReadOperationsFailed'], 1)
        self.assertEqual(self.work.values['jsonParseAttempts'], 0)
        self.assertEqual(self.work.values['applicationReadBytes.json'], path.stat().st_size)
        path.unlink()

    def test_missing_file_has_no_successful_read_or_parse(self):
        path = self.root/'missing.json'
        with self.assertRaises(FileNotFoundError) as expected: owner.read_json(path)
        with owner.file_accounting(self.work), self.assertRaises(FileNotFoundError) as actual: owner.read_json(path)
        self.assertEqual(actual.exception.errno, expected.exception.errno)
        self.assertEqual(actual.exception.filename, expected.exception.filename)
        self.assertEqual(self.work.values['textReadOperationsFailed'], 1)
        self.assertEqual(self.work.values['applicationReadBytes.json'], 0)
        self.assertEqual(self.work.values['jsonParseAttempts'], 0)

    def test_nested_scopes_keep_their_own_reads(self):
        path = self.root/'value.json'; path.write_bytes(b'{}'); other = w.OwnerFileCounters()
        with owner.file_accounting(self.work):
            with owner.file_accounting(other): owner.read_json(path)
            owner.read_json(path)
        self.assertIsNone(owner.FILE_WORK.get())
        self.assertEqual(self.work.values['jsonParseCompleted'], 1); self.assertEqual(other.values['jsonParseCompleted'], 1)

    def test_failed_read_does_not_invent_bytes_hidden_by_the_underlying_call(self):
        class Partial(io.BytesIO):
            def read(self, size=-1):
                super().read(3)
                raise OSError('partial read failure')
        with patch.object(Path, 'open', return_value=Partial(b'{} ')):
            with owner.file_accounting(self.work), self.assertRaisesRegex(OSError, 'partial read failure'):
                owner.read_json(self.root/'value.json')
        self.assertNotIn('applicationReadBytes.json', self.work.values)
        self.assertEqual(self.work.values['textReadOperationsFailed'], 1)
        self.assertEqual(self.work.values['failedTextReadBytesUnknown'], 1)
        self.assertEqual(self.work.values['jsonParseAttempts'], 0)

    def test_routed_owner_counts_all_four_result_reads_without_counting_initial_validation(self):
        fixture = f.Supervisor(); fixture.setUp(); self.addCleanup(fixture.doCleanups)
        export = os.environ.get('LL_REMAINING_READ_OWNER_EXPORT')
        base = Path(export) if export else self.root/'owner'
        supervisor = fixture.prepare(base)
        self.assertEqual(supervisor.counters.values['jsonParseAttempts'], 0)
        seen = []
        original = supervisor.counters.read_json_text
        def observed(path, **kwargs):
            seen.append((Path(path), kwargs.get('encoding')))
            return original(path, **kwargs)
        with patch.object(supervisor.counters, 'read_json_text', side_effect=observed):
            result = fixture.launch(supervisor)
        self.assertEqual(result['status'], 'Complete'); fixture.verify_archive_resources(); fixture.verify(supervisor)
        self.assertEqual([p.name for p, _ in seen], ['native-receipt.json', 'independent-audit.json', 'native-audit.log', 'provisional-result.json'])
        self.assertEqual([encoding for _, encoding in seen], ['utf-8', 'utf-8', 'utf-8-sig', None])
        self.assertEqual(supervisor.counters.values['textReadOperationsCompleted'], 4)
        if export:
            w.seal_new(base/'read-boundaries.json', dict(fixtureOnly=True, scientificWorkersRouted=True, realProcessObservations=False,
                actualCombat=0, productionEntropyDraws=0, readPaths=[str(p.relative_to(base)) for p, _ in seen],
                textReadsCompleted=4, usableForAdmission=False, sources={str(Path(p).resolve().relative_to(ROOT).as_posix()):f.sha(p)
                    for p in (__file__, owner.__file__, w.__file__, f.__file__, f.s.__file__)}))
            w.seal_new(base/'files.json', {str(p.relative_to(base).as_posix()):f.sha(p) for p in sorted(base.rglob('*')) if p.is_file()})


if __name__ == '__main__': unittest.main()
