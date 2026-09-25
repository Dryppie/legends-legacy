"""Closed file declarations for a trusted, single-owner diagnostic directory.

This is a cooperative API contract, not filesystem confinement. Unobserved writes
outside the directory (or between validations) remain unknown and disqualifying.
No scientific resource limit or admission decision is derived from this module.
"""
import hashlib
import os
from pathlib import Path
import re
import stat

from proposal_work_accounting import canonical, require, unlinked

VERSION = 'tower-proposal-diagnostic-storage-v1'


def declaration(value):
    require(type(value) is dict and set(value) == {
        'version', 'files', 'maxLiveBytes', 'maxScratchBytes', 'maxTotalWrittenBytes'},
        'Invalid diagnostic storage declaration')
    require(value['version'] == VERSION, 'Unknown diagnostic storage version')
    limits = {key:value[key] for key in ('maxLiveBytes', 'maxScratchBytes', 'maxTotalWrittenBytes')}
    require(all(type(n) is int and 0 <= n <= 2**63-1 for n in limits.values()), 'Invalid diagnostic byte limit')
    require(type(value['files']) is dict and value['files'], 'Declare diagnostic files before creation')
    files, names = {}, set()
    for name, info in value['files'].items():
        require(type(name) is str and re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_.-]{0,119}', name)
                and not name.endswith('.') and name.casefold() not in names, 'Unsafe or aliased diagnostic file name')
        require(not re.fullmatch(r'CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9]', name.split('.')[0], re.I),
                'Reserved diagnostic file name')
        require(type(info) is dict and set(info) == {'kind', 'maxBytes'}
                and info['kind'] in ('scratch', 'retained') and type(info['maxBytes']) is int
                and 0 <= info['maxBytes'] <= 2**63-1, 'Invalid diagnostic file declaration')
        names.add(name.casefold())
        files[name] = dict(info)
    return dict(version=VERSION, files=files, **limits)


class DiagnosticStorage:
    """Create each declared leaf once; account accepted appends until deletion.

    The root and existing ancestors must be ordinary, unlinked directories. File
    identity, link count, inventory, lengths and closed content are checked. The
    caller must ensure exclusive access; check/use races are not OS confinement.
    Failed operations poison the scope. Handles can still close for cleanup.
    """
    def __init__(self, root, contract, work=None, *, protected_roots=()):
        self._contract = declaration(contract)
        self.contract_sha256 = hashlib.sha256(canonical(self._contract)).hexdigest()
        requested = Path(os.path.abspath(root))
        unlinked(requested)
        # Resolve ordinary aliases (including Windows short paths) only after
        # rejecting links; resolution alone would hide linked ancestors.
        self.root = requested.resolve()
        for protected in protected_roots:
            other = Path(os.path.abspath(protected))
            unlinked(other)
            other = other.resolve()
            require(not (self.root == other or self.root in other.parents or other in self.root.parents),
                    'Overlapping diagnostic and protected roots')
        self._ancestors = {p:self._directory(p) for p in self.root.parents}
        # Exclusive mkdir also rejects an existing empty directory or dangling link.
        self.root.mkdir()
        self._ancestors[self.root] = self._directory(self.root)
        self.work = work
        self.files, self.live, self._used = {}, set(), set()
        self.failed_operations = 0
        self.write_progress_unknown = False
        self.peak = self.scratch_peak = self.written = self.deleted = 0

    @staticmethod
    def _identity(info):
        require(info.st_ino != 0, 'Unavailable diagnostic file identity')
        return info.st_dev, info.st_ino

    @classmethod
    def _directory(cls, path):
        info = path.lstat()
        require(stat.S_ISDIR(info.st_mode) and not getattr(info, 'st_file_attributes', 0) & 1024,
                'Linked or invalid diagnostic directory')
        return cls._identity(info)

    def _call(self, name, action):
        if self.work is not None: self.work.add(name+'Attempted')
        try:
            value = action()
        except BaseException:
            self.failed_operations += 1
            if self.work is not None: self.work.add(name+'Failed')
            raise
        if self.work is not None: self.work.add(name+'Completed')
        return value

    def _path(self, name):
        require(type(name) is str and name in self._contract['files'], 'Undeclared diagnostic file')
        return self.root/name

    def path(self, name):
        # Exposing a read path does not authorize external mutations.
        def validating():
            self._validate()
            return self._path(name)
        return self._call('diagnosticPathValidation', validating)

    def _validate(self, content=False):
        require(self.failed_operations == 0, 'Diagnostic storage previously failed')
        for path, identity in self._ancestors.items():
            require(self._directory(path) == identity, 'Replaced diagnostic directory')
        require({p.name for p in self.root.iterdir()} == set(self.files), 'Untracked diagnostic storage member')
        for name, file in self.files.items():
            path = self._path(name)
            info = path.lstat()
            require(stat.S_ISREG(info.st_mode) and not getattr(info, 'st_file_attributes', 0) & 1024
                    and info.st_nlink == 1 and self._identity(info) == file['identity']
                    and info.st_size == file['size'], 'Changed diagnostic storage member')
            if content:
                if self.work is None:
                    with path.open('rb') as stream: digest = hashlib.file_digest(stream, 'sha256').hexdigest()
                else: digest = self.work.sha(path)
                require(digest == file['hash'].hexdigest(), 'Changed diagnostic storage content')

    def _sizes(self):
        return (sum(f['size'] for f in self.files.values()),
                sum(f['size'] for f in self.files.values() if f['kind'] == 'scratch'))

    def create(self, name, kind):
        def opening():
            self._validate(content=True)
            path = self._path(name)
            require(name not in self._used and kind == self._contract['files'][name]['kind'],
                    'Repeated diagnostic file or changed role')
            # Remember attempts too: a failed create must never be retried in scope.
            self._used.add(name)
            stream = path.open('xb', buffering=0)
            try:
                info = os.fstat(stream.fileno())
                require(stat.S_ISREG(info.st_mode) and info.st_nlink == 1 and info.st_size == 0,
                        'Invalid created diagnostic file')
                self.files[name] = dict(kind=kind, size=0, identity=self._identity(info), hash=hashlib.sha256())
                self.live.add(name)
                self._validate()
                return _Writer(self, name, stream)
            except BaseException as error:
                try: stream.close()
                except BaseException as closing: error.add_note('Diagnostic create cleanup failed: '+repr(closing))
                raise
        return self._call('diagnosticCreate', opening)

    def delete(self, name):
        def deleting():
            self._validate(content=True)
            require(name in self.files and name not in self.live and self.files[name]['kind'] == 'scratch',
                    'Only closed diagnostic scratch may be deleted')
            self._path(name).unlink()
            self.deleted += self.files.pop(name)['size']
            self._validate()
        return self._call('diagnosticDelete', deleting)

    def snapshot(self):
        def observing():
            require(not self.live, 'Unclosed diagnostic file')
            self._validate(content=True)
            current, scratch = self._sizes()
            return dict(coverage='DeclaredCooperativeDirectory', contractVersion=VERSION,
                contractSha256=self.contract_sha256, currentRetainedBytes=current-scratch,
                currentScratchBytes=scratch, peakRetainedPlusScratchBytes=self.peak,
                peakScratchBytes=self.scratch_peak, totalWrittenBytes=self.written, deletedBytes=self.deleted,
                declaredLimits={k:v for k,v in self._contract.items() if k not in ('files', 'version')},
                lifetimeBoundary='SuccessfulApiOperationsThroughThisSnapshot',
                externalWritesBounded=False, filesystemConfinement=False,
                missingCoverage=['concurrentOrTransientExternalMutations', 'runtimeAndCachePathsOutsideScope',
                                 'snapshotPersistenceAndRemainingObserverLifetime', 'physicalOrDurableDiskBytes'],
                wholeProcessCoverage=False, usableForAdmission=False)
        return self._call('diagnosticSnapshot', observing)


class _Writer:
    def __init__(self, owner, name, stream):
        self._owner, self._name, self._stream = owner, name, stream
        self._closed = False

    def write(self, data):
        owner, name, stream = self._owner, self._name, self._stream
        def writing():
            require(not self._closed and type(data) is bytes, 'Diagnostic writer requires open handle and bytes')
            owner._validate()
            file = owner.files[name]
            require(owner._identity(os.fstat(stream.fileno())) == file['identity'], 'Changed diagnostic handle')
            current, scratch = owner._sizes()
            count = len(data)
            require(file['size']+count <= owner._contract['files'][name]['maxBytes']
                    and current+count <= owner._contract['maxLiveBytes']
                    and scratch+(count if file['kind'] == 'scratch' else 0) <= owner._contract['maxScratchBytes']
                    and owner.written+count <= owner._contract['maxTotalWrittenBytes'], 'Diagnostic byte limit exceeded')
            try:
                accepted = stream.write(data)
                require(type(accepted) is int and 0 <= accepted <= count and (accepted > 0 or count == 0),
                        'Invalid diagnostic write result')
            except BaseException:
                owner.write_progress_unknown = True
                if owner.work is not None: owner.work.add('diagnosticFailedWriteBytesUnknown')
                raise
            file['size'] += accepted
            file['hash'].update(data[:accepted])
            owner.written += accepted
            owner.peak = max(owner.peak, current+accepted)
            owner.scratch_peak = max(owner.scratch_peak, scratch+(accepted if file['kind'] == 'scratch' else 0))
            if owner.work is not None: owner.work.add('diagnosticAcceptedWriteBytes', accepted)
            owner._validate()
            return accepted
        return owner._call('diagnosticWrite', writing)

    def flush(self):
        owner = self._owner
        def flushing():
            require(not self._closed, 'Closed diagnostic writer')
            owner._validate()
            self._stream.flush()
        owner._call('diagnosticFlush', flushing)
        owner._call('diagnosticSync', lambda:os.fsync(self._stream.fileno()))

    def close(self):
        if self._closed: return
        owner = self._owner
        def closing():
            self._stream.close()
            self._closed = True
            owner.live.remove(self._name)
            if not owner.failed_operations: owner._validate(content=True)
        owner._call('diagnosticClose', closing)

    def __enter__(self): return self

    def __exit__(self, _, error, __):
        try: self.close()
        except BaseException as closing:
            if error is None: raise
            error.add_note('Diagnostic close failed: '+repr(closing))
