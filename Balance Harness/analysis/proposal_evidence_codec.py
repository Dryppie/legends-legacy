"""Independent bounded reader for the opt-in proposal JSON evidence codec.

Callers supply the authenticated descriptor, expected format and trusted limits.
This module does not select a format, authenticate a study manifest or admit a run.
"""
from collections import Counter
import hashlib
import io
import json
from pathlib import Path
import re
import struct
import zlib

VERSION = 'tower-proposal-json-evidence-gzip-v1'
CODEC = 'gzip-json-bytes-v1'
CHUNK = 64 * 1024
MAX_INT = (1 << 63) - 1
NAMES = re.compile(r'(?:search|pair-(?:0[1-9]|1[0-2])|heldout-(?:0[1-9]|1[0-2])-[0-9a-f]{64})\.json', re.ASCII)
FIELDS = {'logicalPath', 'physicalPath', 'codec', 'logicalBytes', 'logicalSha256', 'physicalBytes', 'physicalSha256'}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def positive(value):
    return type(value) is int and 0 < value <= MAX_INT


def contract(version, limits):
    require(version == VERSION, 'Explicit supported evidence format required')
    require(set(limits) == {'logicalBytes', 'physicalBytes'} and all(positive(v) for v in limits.values()),
            'Trusted positive evidence limits required')


def name(value):
    require(isinstance(value, str) and NAMES.fullmatch(value), 'Unexpected logical evidence name')


def validate_entry(entry, version, limits):
    contract(version, limits)
    require(set(entry) == FIELDS, 'Changed descriptor fields')
    name(entry['logicalPath'])
    require(entry['physicalPath'] == entry['logicalPath'] + '.gz' and entry['codec'] == CODEC,
            'Changed evidence mapping or codec')
    for key in ('logicalBytes', 'physicalBytes'):
        require(positive(entry[key]) and entry[key] <= limits[key], 'Evidence length exceeds trusted limits')
    require(entry['physicalBytes'] >= 20, 'Invalid physical length')
    for key in ('logicalSha256', 'physicalSha256'):
        require(isinstance(entry[key], str) and re.fullmatch('[0-9a-f]{64}', entry[key]), 'Invalid evidence digest')


def validate_entries(entries, expected, version, limits, maximum_members):
    contract(version, limits)
    require(positive(maximum_members) and len(entries) <= maximum_members and len(entries) == len(expected),
            'Evidence member limit or membership mismatch')
    for item in expected:
        name(item)
    require(len(set(expected)) == len(expected), 'Repeated expected member')
    for entry in entries:
        validate_entry(entry, version, limits)
    require([e['logicalPath'] for e in entries] == sorted(expected), 'Missing, repeated or unsorted evidence entry')
    for key in ('logicalBytes', 'physicalBytes'):
        require(sum(e[key] for e in entries) <= limits[key], 'Aggregate evidence limit exceeded')


def _member(root, leaf):
    root = Path(root).absolute()
    require(root.is_dir(), 'Evidence directory does not exist')
    for item in (root, *root.parents, root / leaf):
        require(not item.is_symlink() and not item.is_junction(), 'Linked evidence path')
    return root / leaf


def _unique(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'Duplicate JSON key')
        result[key] = value
    return result


def strict_json(stream):
    return json.load(stream, object_pairs_hook=_unique,
                     parse_constant=lambda _: require(False, 'Nonfinite JSON value'))


class _Input:
    def __init__(self, stream, cancel, work=None):
        self.stream, self.cancel, self.count = stream, cancel, 0
        self.work = work

    def read(self, size):
        self.cancel()
        result = self.stream.read(size)
        self.count += len(result)
        if self.work is not None: self.work.add('applicationReadBytes.payload', len(result))
        return result

    def authenticate(self, entry):
        self.stream.seek(0)
        digest = hashlib.sha256()
        total = 0
        while data := self.read(CHUNK):
            total += len(data)
            require(total <= entry['physicalBytes'], 'Changed physical length')
            digest.update(data)
        require(total == entry['physicalBytes'] and digest.hexdigest() == entry['physicalSha256'],
                'Changed physical length or digest')


def _decode(source, entry, accounting):
    if source.work is not None: source.work.add('decodePassesStarted')
    source.stream.seek(0)
    header = source.read(10)
    require(len(header) == 10 and header[:4] == b'\x1f\x8b\x08\x00' and header[4:8] == bytes(4),
            'Unsupported gzip header')
    source.stream.seek(entry['physicalBytes'] - 8)
    trailer = source.read(8)
    require(len(trailer) == 8, 'Truncated gzip trailer')
    expected_crc, modulo_size = struct.unpack('<II', trailer)
    require(modulo_size == entry['logicalBytes'] % (1 << 32), 'Changed gzip decoded length')
    source.stream.seek(10)
    remaining = entry['physicalBytes'] - 18
    inflater = zlib.decompressobj(-zlib.MAX_WBITS)
    digest, crc, length = hashlib.sha256(), 0, 0
    while remaining:
        data = source.read(min(CHUNK, remaining))
        require(data, 'Truncated evidence payload')
        remaining -= len(data)
        while data:
            source.cancel()
            decoded = inflater.decompress(data, min(CHUNK, entry['logicalBytes'] - length + 1))
            if source.work is not None: source.work.add('decodedBytesProcessed', len(decoded))
            require(not inflater.unused_data, 'Trailing data or multiple gzip members')
            data = inflater.unconsumed_tail
            length += len(decoded)
            require(length <= entry['logicalBytes'], 'Decoded evidence exceeds authenticated bound')
            digest.update(decoded)
            crc = zlib.crc32(decoded, crc)
            accounting['decodedBytesProcessed'] += len(decoded)
            if decoded:
                yield decoded
        if inflater.eof:
            require(remaining == 0, 'Trailing data or multiple gzip members')
            break
    require(inflater.eof and length == entry['logicalBytes'], 'Incomplete decoded evidence')
    require(crc == expected_crc, 'Changed gzip checksum')
    require(digest.hexdigest() == entry['logicalSha256'], 'Changed logical digest')
    accounting['decodePasses'] += 1
    if source.work is not None: source.work.add('decodePassesCompleted')


class _DecodedStream(io.RawIOBase):
    def __init__(self, chunks):
        super().__init__()
        self.chunks, self.pending = iter(chunks), memoryview(b'')

    def readable(self):
        return True

    def readinto(self, target):
        if not target:
            return 0
        if not self.pending:
            self.pending = memoryview(next(self.chunks, b''))
        size = min(len(target), len(self.pending))
        target[:size] = self.pending[:size]
        self.pending = self.pending[size:]
        return size


def read_json(root, entry, version, limits, cancel=lambda: None, work=None):
    """Return a JSON value and actual I/O/decode counters only after complete verification.

    Decompression buffers are bounded. The standard JSON parser still materializes
    the trusted-size-bounded Unicode document and object graph; no decoded file is staged.
    """
    validate_entry(entry, version, limits)
    cancel()
    require(not _member(root, entry['logicalPath']).exists(), 'Plain and compressed evidence cannot coexist')
    physical = _member(root, entry['physicalPath'])
    accounting = Counter(physicalBytesRead=0, decodedBytesProcessed=0, decodePasses=0)
    with physical.open('rb') as stream:
        source = _Input(stream, cancel, work)
        source.authenticate(entry)
        # Authenticate the entire logical stream before giving it to the JSON parser.
        import codecs
        utf8 = codecs.getincrementaldecoder('utf-8')('strict')
        for data in _decode(source, entry, accounting):
            utf8.decode(data, final=False)
        utf8.decode(b'', final=True)
        def parse_chunks():
            for data in _decode(source, entry, accounting):
                if work is not None: work.add('jsonInputBytes', len(data))
                yield data
        if work is not None: work.add('jsonParseAttempts')
        with io.TextIOWrapper(io.BufferedReader(_DecodedStream(parse_chunks())),
                              encoding='utf-8', errors='strict', newline='') as decoded:
            value = strict_json(decoded)
        if work is not None: work.add('jsonParseCompleted')
        source.authenticate(entry)
        accounting['physicalBytesRead'] = source.count
    require(not _member(root, entry['logicalPath']).exists(), 'Plain evidence appeared during verification')
    _member(root, entry['physicalPath'])
    return value, dict(accounting)
