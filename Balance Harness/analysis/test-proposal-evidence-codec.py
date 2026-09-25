"""Synthetic integrity, limits and cross-language tests; never opens a study archive."""
import gzip
import hashlib
import json
import os
from pathlib import Path
import struct
import tempfile
import unittest
import zlib

import proposal_evidence_codec as c

LIMITS = dict(logicalBytes=4*1024*1024, physicalBytes=4*1024*1024)


def sha(data):
    return hashlib.sha256(data).hexdigest()


class CodecTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='ll-evidence-codec-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)

    def fixture(self, raw=b'{}'):
        physical = gzip.compress(raw, compresslevel=6, mtime=0)
        entry = dict(logicalPath='search.json', physicalPath='search.json.gz', codec=c.CODEC,
                     logicalBytes=len(raw), logicalSha256=sha(raw), physicalBytes=len(physical), physicalSha256=sha(physical))
        (self.root / entry['physicalPath']).write_bytes(physical)
        return entry

    def read(self, entry, **kwargs):
        return c.read_json(self.root, entry, c.VERSION, kwargs.pop('limits', LIMITS), **kwargs)

    def repin(self, entry, physical):
        (self.root / entry['physicalPath']).write_bytes(physical)
        return entry | dict(physicalBytes=len(physical), physicalSha256=sha(physical))

    def test_unicode_numeric_forms_empty_containers_and_whitespace(self):
        for raw in (b'{}', b'[]', b'null', b' {"n":-0.00e+10,"s":"\\u00c6"} \n',
                    ' {"text":"Æ 🐉","number":1e+2,"nested":[null,true,{}]}\n'.encode()):
            with self.subTest(raw=raw):
                entry = self.fixture(raw)
                value, work = self.read(entry)
                self.assertEqual(json.loads(raw), value)
                self.assertEqual(2, work['decodePasses'])
                self.assertEqual(2*len(raw), work['decodedBytesProcessed'])
                self.assertGreaterEqual(work['physicalBytesRead'], 2*entry['physicalBytes'])

    def test_large_unicode_crosses_buffer_boundaries(self):
        raw = ('"' + 'a'*65532 + '🐉æ'*30000 + '"').encode()
        value, work = self.read(self.fixture(raw))
        self.assertEqual(json.loads(raw), value)
        self.assertEqual(2*len(raw), work['decodedBytesProcessed'])

    def test_descriptors_are_authenticated(self):
        entry = self.fixture()
        for key, value in (('physicalSha256', '0'*64), ('physicalBytes', entry['physicalBytes']+1),
                           ('logicalSha256', '0'*64), ('logicalBytes', 3), ('codec', 'plain'),
                           ('physicalPath', '../search.json.gz')):
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.read(entry | {key:value})

    def test_missing_and_unexpected_descriptor_fields_are_rejected(self):
        entry = self.fixture()
        with self.assertRaises(ValueError):
            self.read(entry | {'extra':True})
        with self.assertRaises(ValueError):
            self.read({k:v for k,v in entry.items() if k != 'codec'})

    def test_corrupt_payload_is_rejected_without_repinning(self):
        entry = self.fixture()
        (self.root / entry['physicalPath']).write_bytes(b'broken')
        with self.assertRaises(ValueError):
            self.read(entry)

    def test_malformed_gzip_with_correct_physical_pin_is_rejected(self):
        entry = self.fixture(b'{"value":123}')
        original = (self.root / entry['physicalPath']).read_bytes()
        variants = {
            'crc': original[:-8] + bytes([original[-8] ^ 1]) + original[-7:],
            'truncated': original[:-9] + original[-8:],
            'trailing': original + b'\0',
            'member': original + original,
            'flag': original[:3] + b'\x08' + original[4:],
            'timestamp': original[:4] + b'\x01' + original[5:],
            'deflate-tail': original[:-8] + b'\0' + original[-8:]}
        for fault, physical in variants.items():
            with self.subTest(fault=fault), self.assertRaises((ValueError, zlib.error)):
                self.read(self.repin(entry, physical))

    def test_duplicate_json_keys_including_escaped_names_are_rejected(self):
        for raw in (b'{"x":1,"x":2}', b'{"nested":{"x":1,"\\u0078":2}}'):
            with self.subTest(raw=raw), self.assertRaisesRegex(ValueError, 'Duplicate'):
                self.read(self.fixture(raw))

    def test_invalid_json_and_nonfinite_literals_are_rejected(self):
        for raw in (b'{} {}', b'{"x":NaN}', b'{"x":Infinity}', b' '):
            with self.subTest(raw=raw), self.assertRaises(ValueError):
                self.read(self.fixture(raw))

    def test_invalid_utf8_is_rejected(self):
        with self.assertRaises(UnicodeDecodeError):
            self.read(self.fixture(b'"\xc3"'))

    def test_trusted_limits_are_independent_of_compressed_size(self):
        entry = self.fixture(b'"' + b'x'*1000000 + b'"')
        with self.assertRaises(ValueError):
            self.read(entry, limits=LIMITS | {'logicalBytes':1024})

    def test_repinned_small_length_cannot_bypass_streaming_limit(self):
        entry = self.fixture(b'"' + b'x'*1000000 + b'"')
        physical = (self.root / entry['physicalPath']).read_bytes()
        entry = self.repin(entry | {'logicalBytes':20}, physical[:-4] + struct.pack('<I', 20))
        with self.assertRaisesRegex(ValueError, 'bound'):
            self.read(entry)

    def test_format_must_be_explicit_and_supported(self):
        for version in (None, '', 'plain', 'future'):
            with self.subTest(version=version), self.assertRaises(ValueError):
                c.read_json(self.root, self.fixture(), version, LIMITS)

    def test_unsafe_and_ineligible_paths_are_rejected(self):
        for name in ('../search.json', 'SEARCH.json', 'pair-13.json', 'plan.json', 'search.json:stream', 'a/search.json'):
            with self.subTest(name=name), self.assertRaises(ValueError):
                self.read(self.fixture() | {'logicalPath':name, 'physicalPath':name+'.gz'})

    def test_nonpositive_bool_noninteger_and_overflow_bounds_are_rejected(self):
        for value in (0, -1, True, 1.5, 1 << 63):
            with self.subTest(value=value), self.assertRaises(ValueError):
                self.read(self.fixture(), limits=LIMITS | {'logicalBytes':value})

    def test_plain_compressed_collision_is_rejected(self):
        entry = self.fixture()
        (self.root / 'search.json').write_bytes(b'{}')
        with self.assertRaises(ValueError):
            self.read(entry)

    def test_cancellation_interrupts_before_access_and_during_decode(self):
        entry = self.fixture()
        for at in (1, 5):
            calls = 0
            def cancel():
                nonlocal calls
                calls += 1
                if calls == at:
                    raise InterruptedError('cancelled')
            with self.subTest(at=at), self.assertRaises(InterruptedError):
                self.read(entry, cancel=cancel)

    def test_index_membership_and_duplicate_entries_are_rejected(self):
        entry = self.fixture()
        c.validate_entries([entry], ['search.json'], c.VERSION, LIMITS, 1)
        for entries, expected, maximum in (([entry,entry], ['search.json','search.json'], 2),
                                           ([entry], ['pair-01.json'], 1), ([entry], ['search.json'], 0)):
            with self.subTest(expected=expected, maximum=maximum), self.assertRaises(ValueError):
                c.validate_entries(entries, expected, c.VERSION, LIMITS, maximum)

    def test_index_order_and_aggregate_limit_are_enforced(self):
        entry = self.fixture()
        pair = entry | dict(logicalPath='pair-01.json', physicalPath='pair-01.json.gz')
        with self.assertRaises(ValueError):
            c.validate_entries([entry,pair], ['search.json','pair-01.json'], c.VERSION, LIMITS, 2)
        with self.assertRaises(ValueError):
            c.validate_entries([pair,entry], ['pair-01.json','search.json'], c.VERSION, LIMITS | {'logicalBytes':3}, 2)

    def test_decoder_never_creates_decoded_files(self):
        entry = self.fixture()
        before = {p.name for p in self.root.iterdir()}
        self.read(entry)
        self.assertEqual(before, {p.name for p in self.root.iterdir()})

    @unittest.skipUnless(os.environ.get('LL_EVIDENCE_CODEC_EXCHANGE'), 'Native exchange directory not requested')
    def test_native_writer_samples_match_exact_logical_digests(self):
        exchange = Path(os.environ['LL_EVIDENCE_CODEC_EXCHANGE'])
        self.assertEqual({'0','1','2'}, {p.name for p in exchange.iterdir()})
        for folder in sorted(exchange.iterdir()):
            entry = json.loads((folder / 'entry.json').read_text())
            expected = (folder / 'expected.json').read_bytes()
            self.assertEqual(sha(expected), entry['logicalSha256'])
            actual, work = c.read_json(folder, entry, c.VERSION, LIMITS)
            self.assertEqual(json.loads(expected), actual)
            self.assertEqual(2*len(expected), work['decodedBytesProcessed'])


if __name__ == '__main__':
    unittest.main()
