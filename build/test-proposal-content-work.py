"""Independently check a fresh, captured native content-accounting fixture."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('content_work', ROOT/'Balance Harness/analysis/proposal_work_accounting.py')
work = importlib.util.module_from_spec(spec)
spec.loader.exec_module(work)


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def text(path):
    raw = path.read_bytes()
    if raw.startswith((b'\xff\xfe', b'\xfe\xff')): return raw.decode('utf-16')
    return raw.decode('utf-8-sig')


def equipment_fragments(path):
    # Preserve each object's original spelling/whitespace instead of reserializing
    # Python objects, which would manufacture different JSON input byte counts.
    source = text(path)
    decoder = json.JSONDecoder()
    position = len(source)-len(source.lstrip())
    if source[position] != '[': raise ValueError('Expected items array')
    position += 1
    result = []
    while True:
        while source[position].isspace(): position += 1
        if source[position] == ']': break
        start = position
        item, position = decoder.raw_decode(source, position)
        if item.get('itemType', '').lower() == 'equipment': result.append(source[start:position])
        while source[position].isspace(): position += 1
        if source[position] == ']': break
        if source[position] != ',': raise ValueError('Expected item separator')
        position += 1
    if source[position+1:].strip(): raise ValueError('Trailing item data')
    return result


class ContentWork(unittest.TestCase):
    fixture = manifest_pin = None

    @classmethod
    def setUpClass(cls):
        if sha(cls.fixture/'files.json') != cls.manifest_pin: raise ValueError('Changed fixture manifest')
        cls.files = work.strict((cls.fixture/'files.json').read_bytes())
        for name, pin in cls.files.items():
            path = cls.fixture/name
            if not path.resolve().is_relative_to(cls.fixture) or sha(path) != pin: raise ValueError('Changed fixture member')
        cls.expected = work.strict((cls.fixture/'expected.json').read_bytes())
        cls.receipt = work.verify_counter_receipt(cls.fixture/'native-work.json', cls.files['native-work.json'],
                                                  'nativeAudit', 'a'*64, 'b'*64)

    def test_captured_members_and_incomplete_coverage_are_explicit(self):
        actual = {p.relative_to(self.fixture).as_posix() for p in self.fixture.rglob('*') if p.is_file() and p.name != 'files.json'}
        self.assertEqual(actual, set(self.files))
        self.assertEqual(len([name for name in self.files if name.startswith('content/')]), 16)
        self.assertFalse(self.receipt['wholeProcessCoverage'])
        self.assertFalse(self.receipt['usableForAdmission'])

    def test_duplicate_catalog_loads_count_actual_file_bytes_twice(self):
        names = self.expected['filesRead']
        self.assertEqual(len(names), 13)
        self.assertEqual(names.count('combat/abilities.json'), 2)
        total = sum((self.fixture/'content'/name).stat().st_size for name in names)
        self.assertEqual(total, self.expected['applicationReadBytes'])
        self.assertEqual(total, self.receipt['counters']['applicationReadBytes.json'])

    def test_item_reparsing_adds_exact_parser_bytes_without_extra_file_reads(self):
        fragments = equipment_fragments(self.fixture/'content/items/items.json')
        self.assertEqual(fragments, self.expected['equipmentFragments'])
        count = len(self.expected['filesRead']) + len(fragments)
        total = sum(len(text(self.fixture/'content'/name).encode()) for name in self.expected['filesRead'])
        total += sum(len(fragment.encode()) for fragment in fragments)
        self.assertEqual(total, self.expected['jsonInputBytes'])
        self.assertEqual(total, self.receipt['counters']['jsonInputBytes'])
        self.assertEqual(count, self.receipt['counters']['jsonParseAttempts'])
        self.assertEqual(count, self.receipt['counters']['jsonParseCompleted'])
        self.assertEqual(count, self.expected['completedParses'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixture', type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args, remaining = parser.parse_known_args()
    ContentWork.fixture, ContentWork.manifest_pin = args.fixture.resolve(), work.pin(args.manifest_pin)
    unittest.main(argv=[sys.argv[0], *remaining])
