"""Authenticate a native copy/text fixture without treating native copies as observed stream I/O."""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('file_work', ROOT/'Balance Harness/analysis/proposal_work_accounting.py')
work = importlib.util.module_from_spec(spec)
spec.loader.exec_module(work)


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


class FileWork(unittest.TestCase):
    fixture = manifest_pin = None

    @classmethod
    def setUpClass(cls):
        if sha(cls.fixture/'files.json') != cls.manifest_pin: raise ValueError('Changed fixture manifest')
        cls.files = work.strict((cls.fixture/'files.json').read_bytes())
        for name, pin in cls.files.items():
            path = cls.fixture/name
            if not path.resolve().is_relative_to(cls.fixture) or sha(path) != pin: raise ValueError('Changed fixture member')
        cls.receipt = work.verify_counter_receipt(cls.fixture/'native-work.json', cls.files['native-work.json'],
                                                  'native', 'a'*64, 'b'*64)
        cls.counts = cls.receipt['counters']

    def test_manifest_and_incomplete_coverage_are_explicit(self):
        actual = {p.name for p in self.fixture.iterdir() if p.is_file() and p.name != 'files.json'}
        self.assertEqual(actual, set(self.files))
        self.assertFalse(self.receipt['wholeProcessCoverage'])
        self.assertFalse(self.receipt['usableForAdmission'])

    def test_native_and_bounded_copies_preserve_bytes_but_have_distinct_counters(self):
        source = (self.fixture/'source.json').read_bytes()
        self.assertEqual(source, (self.fixture/'target.json').read_bytes())
        self.assertEqual(source, (self.fixture/'bounded.json').read_bytes())
        self.assertEqual(len(source), self.counts['fileCopyLogicalBytes.json'])
        self.assertEqual(len(source), self.counts['applicationReadBytes.json'])
        self.assertEqual(len(source), self.counts['applicationWriteBytes.json'])
        self.assertEqual(1, self.counts['fileCopyOperationsCompleted'])

    def test_text_bytes_and_retained_peak_match_actual_files(self):
        journal = (self.fixture/'trials.jsonl').read_bytes()
        self.assertEqual(journal, b'{"literal":true}\n')
        self.assertEqual(len(journal), self.counts['applicationWriteBytes.journal'])
        self.assertEqual(1, self.counts['textWriteOperationsCompleted'])
        total = sum((self.fixture/name).stat().st_size for name in ('target.json', 'bounded.json', 'trials.jsonl'))
        for field in ('trackedRetainedBytes', 'trackedCombinedBytes', 'peakTrackedCombinedBytes'):
            self.assertEqual(total, self.counts[field])
        self.assertEqual(0, self.counts['trackedScratchBytes'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixture', type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args, remaining = parser.parse_known_args()
    FileWork.fixture, FileWork.manifest_pin = args.fixture.resolve(), work.pin(args.manifest_pin)
    unittest.main(argv=[sys.argv[0], *remaining])
