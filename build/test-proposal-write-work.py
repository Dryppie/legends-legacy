"""Independently authenticate native write accounting and an atomic replacement fixture."""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('write_work', ROOT/'Balance Harness/analysis/proposal_work_accounting.py')
work = importlib.util.module_from_spec(spec)
spec.loader.exec_module(work)


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


class WriteWork(unittest.TestCase):
    fixture = manifest_pin = studies = None

    @classmethod
    def setUpClass(cls):
        if sha(cls.fixture/'files.json') != cls.manifest_pin: raise ValueError('Changed fixture manifest')
        cls.files = work.strict((cls.fixture/'files.json').read_bytes())
        for name, pin in cls.files.items():
            path = cls.fixture/name
            if not path.resolve().is_relative_to(cls.fixture) or sha(path) != pin: raise ValueError('Changed fixture member')
        cls.expected = work.strict((cls.fixture/'expected.json').read_bytes())
        cls.receipt = work.verify_counter_receipt(cls.fixture/'native-work.json', cls.files['native-work.json'],
                                                  'native', 'a'*64, 'b'*64)
        cls.counts = cls.receipt['counters']

    def test_manifest_and_explicit_incomplete_coverage(self):
        actual = {p.relative_to(self.fixture).as_posix() for p in self.fixture.rglob('*') if p.is_file() and p.name != 'files.json'}
        self.assertEqual(actual, set(self.files))
        self.assertFalse(self.receipt['wholeProcessCoverage'])
        self.assertFalse(self.receipt['usableForAdmission'])

    def test_replacement_and_deleted_scratch_count_actual_written_bytes_once(self):
        final = (self.fixture/'result.json').read_bytes()
        self.assertEqual(final, self.expected['newText'].encode())
        self.assertEqual(self.counts['applicationWriteBytes.other'], len(final) + self.expected['deletedScratchBytes'])
        self.assertEqual(self.counts['trackedReplacedBytes'], len(self.expected['oldText'].encode()))
        self.assertEqual(self.counts['trackedDeletedBytes'], self.expected['deletedScratchBytes'])
        self.assertEqual(self.counts['trackedFileMoves'], 1)
        self.assertEqual(self.counts['trackedFileDeletes'], 1)

    def test_simultaneous_peak_survives_rename_and_deletion(self):
        old = len(self.expected['oldText'].encode())
        final = (self.fixture/'result.json').stat().st_size
        scratch = self.expected['deletedScratchBytes']
        self.assertEqual(self.counts['peakTrackedCombinedBytes'], max(old + final, final + scratch))
        self.assertEqual(self.counts['peakTrackedScratchBytes'], max(final, scratch))
        self.assertEqual(self.counts['peakTrackedRetainedBytes'], max(old, final))
        self.assertEqual(self.counts['trackedRetainedBytes'], final)
        self.assertEqual(self.counts['trackedCombinedBytes'], final)
        self.assertEqual(self.counts['trackedScratchBytes'], 0)

    def test_full_generation_receipts_preserve_incomplete_coverage_and_durable_journal_bytes(self):
        for name in ('plain', 'compressed'):
            root = self.studies/name
            path = root/'fixture-native-write-work.json'
            receipt = work.verify_counter_receipt(path, sha(path), 'native', 'a'*64, 'b'*64)
            counts = receipt['counters']
            # Trial journals are added during the later fixture export, outside this receipt.
            journals = [root/'attempts.jsonl', *root.glob('search/*/*/racing/*.jsonl')]
            self.assertGreater(len(journals), 1)
            self.assertEqual(counts['applicationWriteBytes.journal'], sum(p.stat().st_size for p in journals))
            self.assertGreater(counts['trackedRetainedBytes'], 0)
            self.assertEqual(counts['trackedScratchBytes'], 0)
            self.assertGreaterEqual(counts['peakTrackedCombinedBytes'], counts['trackedRetainedBytes'])
            self.assertGreater(counts['peakTrackedScratchBytes'], 0)
            self.assertEqual(counts.get('storageLengthObservationFailures', 0), 0)
            self.assertFalse(receipt['wholeProcessCoverage'])
            self.assertFalse(receipt['usableForAdmission'])

    def test_full_codec_physical_bytes_include_each_payload_and_trailer_once(self):
        root = self.studies/'compressed'
        counts = work.strict((root/'fixture-native-write-work.json').read_bytes())['counters']
        metadata = work.strict((root/'evidence-storage.json').read_bytes())
        entries = [(Path(index).parent, entry) for index in metadata['indexes']
                   for entry in work.strict((root/index).read_bytes())['entries']]
        self.assertEqual(len(entries), 60)
        total = 0
        for parent, entry in entries:
            path = root/parent/entry['physicalPath']
            self.assertEqual(sha(path), entry['physicalSha256'])
            self.assertEqual(path.stat().st_size, entry['physicalBytes'])
            total += path.stat().st_size
        self.assertEqual(counts['applicationWriteBytes.payload'], total)
        plain = work.strict((self.studies/'plain/fixture-native-write-work.json').read_bytes())['counters']
        self.assertEqual(plain.get('applicationWriteBytes.payload', 0), 0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixture', type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    parser.add_argument('--studies', type=Path, required=True)
    args, remaining = parser.parse_known_args()
    WriteWork.fixture, WriteWork.manifest_pin = args.fixture.resolve(), work.pin(args.manifest_pin)
    WriteWork.studies = args.studies.resolve()
    unittest.main(argv=[sys.argv[0], *remaining])
