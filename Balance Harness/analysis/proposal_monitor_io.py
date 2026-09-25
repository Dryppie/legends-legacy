"""Finite selected monitor I/O; not an OS quota or a whole-process envelope."""
from contextlib import contextmanager
import hashlib
import json
import math
import os
from pathlib import Path
import stat

import proposal_work_accounting as work

VERSION = 'tower-proposal-monitor-io-v1'
TERMINAL_VERSION = 'tower-proposal-monitor-io-v2'
LIMITS = ('maxFileReadBytes', 'maxTotalReadBytes', 'maxConsoleBytes',
          'maxObservationBytes', 'maxManifestBytes', 'maxTotalWriteBytes')
PUBLICATIONS = {'owner-process.json': 'maxObservationBytes', 'files.json': 'maxManifestBytes'}
MAX_HASHES = 11  # Six bindings, optional runtime binding, console twice, two receipts.
MAX_INVENTORIES = 2
JSON_STRING_CHARS = 1024
JSON_CONTAINER_DEPTH = 64
FINALIZER_NOTES = 8


def _close(resource, original, note):
    """Attempt one close, preserving a body failure without exception hooks.

    Callers supply fixed literal notes. Secondary exception details are omitted;
    this pure I/O module does not import the Windows launcher to format errors.
    Full or nonstandard caller-owned notes are left untouched.
    """
    try: resource.close()
    except BaseException:
        if original is None: raise
        try:
            attributes = BaseException.__dict__['__dict__'].__get__(original)
            if type(attributes) is not dict: return
            missing = object()
            notes = dict.get(attributes, '__notes__', missing)
            if notes is missing: dict.__setitem__(attributes, '__notes__', [note])
            elif type(notes) is list and len(notes) < FINALIZER_NOTES: list.append(notes, note)
        except BaseException:
            pass  # Diagnostic allocation must not replace the original failure.


def bounded_canonical(value, maximum):
    """Canonical receipt bytes with bounded tokens and built-in containers only.

    Input objects already exist outside this encoder. Output payload is capped;
    string escaping uses fixed chunks, and numeric/key-list allocations depend
    on the remaining byte allowance. This is not a process-memory/RSS limit.
    """
    work.require(type(maximum) is int and 0 <= maximum < 2**63, 'Invalid JSON byte allowance')
    output = bytearray()
    active = set()

    def fits(size):
        work.require(size <= maximum-len(output), 'Monitor publication byte allowance exhausted')

    def emit(raw):
        fits(len(raw))
        output.extend(raw)

    def string(text):
        fits(len(text)+2)  # Every code point needs at least one output byte.
        emit(b'"')
        for start in range(0, len(text), JSON_STRING_CHARS):
            # The standard encoder still owns escaping. At most 1,024 code
            # points become at most 6,144 UTF-8 payload bytes per chunk.
            chunk = text[start:start+JSON_STRING_CHARS]
            emit(json.dumps(chunk, ensure_ascii=False)[1:-1].encode('utf-8'))
        emit(b'"')

    def encode(item, depth):
        kind = type(item)
        if kind is str:
            string(item)
        elif item is None:
            emit(b'null')
        elif kind is bool:
            emit(b'true' if item else b'false')
        elif kind is int:
            # Hex digit count is a conservative lower bound on decimal digits.
            # Reject clearly oversized integers before converting to text.
            fits(max(1, (item.bit_length()+3)//4) + int(item < 0))
            emit(str(item).encode('ascii'))
        elif kind is float:
            work.require(math.isfinite(item), 'Nonfinite monitor JSON number')
            emit(json.dumps(item, allow_nan=False).encode('ascii'))
        else:
            work.require(kind is dict or kind is list or kind is tuple, 'Monitor JSON requires exact built-in values')
            work.require(depth < JSON_CONTAINER_DEPTH, 'Monitor JSON container depth exhausted')
            work.require(id(item) not in active, 'Circular monitor JSON container')
            count = len(item)
            # Bound traversal/key sorting before allocating lists or visiting
            # members. Empty string keys and one-byte values give these minima.
            fits(max(2, (5*count+1) if kind is dict else (2*count+1)))
            active.add(id(item))
            try:
                if kind is dict:
                    minimum = max(2, 5*count+1)
                    for key in item:
                        work.require(type(key) is str, 'Monitor JSON requires string keys')
                        minimum += len(key)
                        fits(minimum)
                    emit(b'{')
                    for index, key in enumerate(sorted(item)):
                        if index: emit(b',')
                        string(key)
                        emit(b':')
                        encode(item[key], depth+1)
                    emit(b'}')
                else:
                    emit(b'[')
                    for index, member in enumerate(item):
                        if index: emit(b',')
                        encode(member, depth+1)
                    emit(b']')
            finally:
                active.remove(id(item))

    encode(value, 0)
    return bytes(output)


def declaration(value):
    work.require(type(value) is dict and value.get('version') in (VERSION, TERMINAL_VERSION),
                 'Invalid monitor I/O declaration')
    terminal = value['version'] == TERMINAL_VERSION
    fields = {'version', *LIMITS} | ({'terminalRoot', 'maxTerminalBytes'} if terminal else set())
    work.require(set(value) == fields, 'Invalid monitor I/O declaration')
    for name in (*LIMITS, *(['maxTerminalBytes'] if terminal else [])):
        work.require(type(value[name]) is int and 0 <= value[name] < 2**63,
                     'Invalid monitor I/O limit: '+name)
    if terminal:
        root = value['terminalRoot']
        work.require(type(root) is str and Path(root).is_absolute()
                     and str(Path(os.path.abspath(root))) == root, 'Canonical absolute terminal root required')
    return dict(value)


class MonitorIO:
    """One run's reads and two publications, plus an optional v2 terminal file.

    Failed reservations are not refunded. A failed operation poisons this scope,
    while the caller must still close owned streams and drain its owned job.
    Console writes use the process helper's separate bounded parent writer.
    """
    def __init__(self, value):
        self.limits = declaration(value)
        self.has_terminal = self.limits['version'] == TERMINAL_VERSION
        self.publication_limits = dict(PUBLICATIONS)
        if self.has_terminal: self.publication_limits['monitor-call.json'] = 'maxTerminalBytes'
        self.max_hashes = MAX_HASHES + int(self.has_terminal)
        self.max_inventories = MAX_INVENTORIES + 2*int(self.has_terminal)
        self.failed = False
        self.hash_attempts = self.hash_completed = self.read_reserved = 0
        self.read_requested = self.read_accepted = self.read_calls = 0
        self.read_progress_unknown = False
        self.publications = {}
        self.write_reserved = self.write_accepted = self.write_calls = 0
        self.write_progress_unknown = False
        self.inventory_attempts = self.inventory_entries = 0

    @contextmanager
    def scope(self):
        work.require(not self.failed, 'Failed monitor I/O scope cannot be reused')
        try:
            yield
        except BaseException:
            self.failed = True
            raise

    def sha(self, path, counters):
        with self.scope():
            work.require(self.hash_attempts < self.max_hashes, 'Monitor hash attempts exhausted')
            self.hash_attempts += 1
            path = Path(path)
            work.unlinked(path)
            info = path.stat(follow_symlinks=False)
            work.require(stat.S_ISREG(info.st_mode), 'Nonregular monitor hash input')
            # Reserve the complete known length plus one EOF probe before open.
            # A growing, shrinking or replaced file never yields a trusted hash.
            size = info.st_size
            reservation = size+1
            work.require(0 < reservation <= self.limits['maxFileReadBytes'], 'Monitor file read allowance exhausted')
            work.require(reservation <= self.limits['maxTotalReadBytes']-self.read_reserved,
                         'Monitor total read allowance exhausted')
            self.read_reserved += reservation
            stream = path.open('rb', buffering=0)
            original = None
            try:
                opened = os.fstat(stream.fileno())
                identity = (info.st_dev, info.st_ino, size)
                work.require((opened.st_dev, opened.st_ino, opened.st_size) == identity,
                             'Changed monitor hash input')
                digest, remaining = hashlib.sha256(), size
                while True:
                    requested = min(65536, remaining) if remaining else 1
                    self.read_requested += requested
                    self.read_calls += 1
                    try:
                        raw = stream.read(requested)
                        work.require(type(raw) is bytes and len(raw) <= requested, 'Invalid monitor hash read')
                    except BaseException:
                        self.read_progress_unknown = True
                        raise
                    self.read_accepted += len(raw)
                    if counters is not None: counters.add('applicationReadBytes.'+work.role(path), len(raw))
                    if not remaining:
                        work.require(not raw, 'Growing monitor hash input')
                        break
                    work.require(len(raw) == requested, 'Short or shrinking monitor hash input')
                    digest.update(raw)
                    remaining -= len(raw)
                closed = os.fstat(stream.fileno())
                work.require((closed.st_dev, closed.st_ino, closed.st_size) == identity,
                             'Changed monitor hash input')
            except BaseException as error:
                original = error
                raise
            finally:
                _close(stream, original, 'Monitor hash close failed; secondary details omitted.')
            self.hash_completed += 1
            return digest.hexdigest()

    def publication(self, name, value):
        with self.scope():
            work.require(name in self.publication_limits and name not in self.publications,
                         'Undeclared or repeated monitor publication')
            self.publications[name] = dict(openAttempted=False, reservedBytes=0, acceptedBytes=0)
            limit = min(self.limits[self.publication_limits[name]], self.limits['maxTotalWriteBytes']-self.write_reserved)
            raw = bounded_canonical(value, limit)
            self.write_reserved += len(raw)
            self.publications[name]['reservedBytes'] = len(raw)
            return raw

    def opening(self, name):
        with self.scope():
            item = self.publications[name]
            work.require(not item['openAttempted'], 'Repeated monitor publication open')
            item['openAttempted'] = True

    def written(self, name, count):
        item = self.publications[name]
        item['acceptedBytes'] += count
        self.write_accepted += count

    def inventory(self, root, expected):
        with self.scope():
            work.require(self.inventory_attempts < self.max_inventories, 'Monitor inventories exhausted')
            work.require(set(expected) <= {'console.log', *self.publication_limits}, 'Undeclared monitor inventory')
            work.require(len(expected) <= 3, 'Monitor inventory size exhausted')
            self.inventory_attempts += 1
            names, count = set(), 0
            entries = os.scandir(root)
            original = None
            try:
                for entry in entries:
                    count += 1
                    self.inventory_entries += 1
                    work.require(count <= len(expected), 'Untracked owner monitor member')
                    names.add(entry.name)
            except BaseException as error:
                original = error
                raise
            finally:
                _close(entries, original, 'Monitor inventory close failed; secondary details omitted.')
            work.require(names == set(expected), 'Untracked owner monitor member')
            return names

    def snapshot(self):
        return dict(version=self.limits['version'], declaration=dict(self.limits), failed=self.failed,
            hashAttempts=self.hash_attempts, hashCompleted=self.hash_completed, maxHashAttempts=self.max_hashes,
            reservedReadBytes=self.read_reserved, requestedReadBytes=self.read_requested,
            acceptedReadBytes=self.read_accepted, readCalls=self.read_calls, failedReadProgressUnknown=self.read_progress_unknown,
            publications={k:dict(v) for k,v in self.publications.items()},
            reservedWriteBytes=self.write_reserved, acceptedWriteBytes=self.write_accepted,
            writeCalls=self.write_calls, failedWriteProgressUnknown=self.write_progress_unknown,
            inventoryAttempts=self.inventory_attempts, inventoryEntries=self.inventory_entries,
            maxInventoryAttempts=self.max_inventories, maxEntriesPerInventory=4,
            scope=('SelectedHashReadsAndThreeMonitorPublications;ConsoleHasSeparateHelperBudget' if self.has_terminal
                   else 'SelectedHashReadsAndTwoMonitorPublications;ConsoleHasSeparateHelperBudget'),
            reservationMeaning='KnownFileLengthPlusEOFProbe;EncodedPublicationLength;NoRefundOnFailure',
            externalMutationBounded=False, wallClockBounded=False, memoryBounded=False,
            terminalObservationPersistenceExcluded='monitor-call.json' not in self.publications,
            **(dict(snapshotPersistenceExcluded=True) if self.has_terminal else {}),
            wholeProcessCoverage=False, usableForAdmission=False)
