"""Bound trusted diagnostic process trees with an owned Windows Job Object.

Create suspended inside the job, prohibit breakaway, and verify the job is
empty before reporting completion. Never infer tree completion from the root PID.
https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects
"""
import ctypes as c
from ctypes import wintypes as w
from contextlib import contextmanager, nullcontext
import math
import hashlib
import mmap
import msvcrt
import os
import re
import shutil
import stat
import subprocess
import time


ERROR_TEXT_CHARS = 4096
ERROR_ARGUMENTS = 4
ERROR_ARGUMENT_CHARS = 256
ERROR_NOTES = 8


def _clip_error(text, maximum):
    return text if len(text) <= maximum else text[:maximum-14]+'...[truncated]'


def _error_text(error, *, representation=False):
    """Bound selected diagnostic text without invoking exception/argument hooks.

    Native BaseException descriptors bypass overridden attributes. Only exact
    scalar arguments are rendered; input objects and traceback memory are outside
    this bound. Even a diagnostic allocation failure must not interrupt cleanup.
    """
    try:
        kind = type(error)
        name = type.__getattribute__(kind, '__name__') if type(kind) is type else 'BaseException'
        name = _clip_error(name, 128) if type(name) is str else 'BaseException'
        args = BaseException.args.__get__(error)
        if type(args) is not tuple: return name+'(<unsupported arguments>)'
        if not representation and len(args) == 1 and type(args[0]) is str:
            return _clip_error(args[0], ERROR_TEXT_CHARS) or name
        if not args: return name+'()' if representation else name
        parts = []
        for value in args[:ERROR_ARGUMENTS]:
            value_type = type(value)
            if value_type is str: parts.append(repr(_clip_error(value, ERROR_ARGUMENT_CHARS)))
            elif value is None or value_type is bool or value_type is float: parts.append(repr(value))
            elif value_type is int:
                parts.append(repr(value) if value.bit_length() <= 256 else '<large integer>')
            else: parts.append('<unsupported argument>')
        if len(args) > ERROR_ARGUMENTS: parts.append('...<arguments omitted>')
        return _clip_error(name+'('+', '.join(parts)+')', ERROR_TEXT_CHARS)
    except BaseException:
        return 'BaseException(<diagnostic unavailable>)'


def _error_note(primary, prefix, secondary):
    """Best-effort bounded notes; no overridable add_note or attribute access.

    Existing notes belong to the caller. Do not iterate, replace or normalize an
    unsupported notes object, or grow a list already at the note-count ceiling.
    """
    try:
        attributes = BaseException.__dict__['__dict__'].__get__(primary)
        if type(attributes) is not dict: return
        missing = object()
        notes = dict.get(attributes, '__notes__', missing)
        if notes is not missing and (type(notes) is not list or len(notes) >= ERROR_NOTES): return
        prefix = _clip_error(prefix, 128) if type(prefix) is str else 'Owned process error: '
        note = _clip_error(prefix+_error_text(secondary, representation=True), ERROR_TEXT_CHARS)
        if notes is missing: dict.__setitem__(attributes, '__notes__', [note])
        else: list.append(notes, note)
    except BaseException:
        pass


class _BasicLimit(c.Structure):
    _fields_ = [('process_time', c.c_longlong), ('job_time', c.c_longlong),
                ('flags', w.DWORD), ('minimum_ws', c.c_size_t), ('maximum_ws', c.c_size_t),
                ('active_limit', w.DWORD), ('affinity', c.c_size_t), ('priority', w.DWORD), ('scheduling', w.DWORD)]


class _Io(c.Structure):
    _fields_ = [(n, c.c_ulonglong) for n in ('read_ops', 'write_ops', 'other_ops', 'read_bytes', 'write_bytes', 'other_bytes')]


class _Limits(c.Structure):
    _fields_ = [('basic', _BasicLimit), ('io', _Io), ('process_memory', c.c_size_t),
                ('job_memory', c.c_size_t), ('peak_process', c.c_size_t), ('peak_job', c.c_size_t)]


class _Accounting(c.Structure):
    _fields_ = [(n, c.c_longlong) for n in ('user', 'kernel', 'period_user', 'period_kernel')] + [
        (n, w.DWORD) for n in ('faults', 'total_processes', 'active_processes', 'terminated_processes')]


class _AccountingIo(c.Structure):
    _fields_ = [('basic', _Accounting), ('io', _Io)]


class _Startup(c.Structure):
    _fields_ = [('size', w.DWORD), ('reserved', w.LPWSTR), ('desktop', w.LPWSTR), ('title', w.LPWSTR)] + [
        (n, w.DWORD) for n in ('x', 'y', 'width', 'height', 'chars_x', 'chars_y', 'fill', 'flags')] + [
        ('show', w.WORD), ('reserved_bytes', w.WORD), ('reserved_pointer', c.c_void_p),
        ('stdin', w.HANDLE), ('stdout', w.HANDLE), ('stderr', w.HANDLE)]


class _StartupEx(c.Structure):
    _fields_ = [('startup', _Startup), ('attributes', c.c_void_p)]


class _Process(c.Structure):
    _fields_ = [('process', w.HANDLE), ('thread', w.HANDLE), ('pid', w.DWORD), ('tid', w.DWORD)]


class _Memory(c.Structure):
    _fields_ = [('size', w.DWORD), ('faults', w.DWORD)] + [(n, c.c_size_t) for n in
        ('peak_ws','ws','peak_paged','paged','peak_nonpaged','nonpaged','commit','peak_commit')]


_kernel = c.WinDLL('kernel32', use_last_error=True)


def _api(name, result, *args):
    f = getattr(_kernel, name)
    f.restype, f.argtypes = result, args
    return f


_create_job = _api('CreateJobObjectW', w.HANDLE, c.c_void_p, w.LPCWSTR)
_set_job = _api('SetInformationJobObject', w.BOOL, w.HANDLE, c.c_int, c.c_void_p, w.DWORD)
_query_job = _api('QueryInformationJobObject', w.BOOL, w.HANDLE, c.c_int, c.c_void_p, w.DWORD, c.c_void_p)
_is_in_job = _api('IsProcessInJob', w.BOOL, w.HANDLE, w.HANDLE, c.POINTER(w.BOOL))
_terminate_job = _api('TerminateJobObject', w.BOOL, w.HANDLE, w.UINT)
_terminate_process = _api('TerminateProcess', w.BOOL, w.HANDLE, w.UINT)
_close = _api('CloseHandle', w.BOOL, w.HANDLE)
_resume = _api('ResumeThread', w.DWORD, w.HANDLE)
_wait = _api('WaitForSingleObject', w.DWORD, w.HANDLE, w.DWORD)
_exit_code = _api('GetExitCodeProcess', w.BOOL, w.HANDLE, c.POINTER(w.DWORD))
_create_process = _api('CreateProcessW', w.BOOL, w.LPCWSTR, w.LPWSTR, c.c_void_p, c.c_void_p,
                       w.BOOL, w.DWORD, c.c_void_p, w.LPCWSTR, c.POINTER(_Startup), c.POINTER(_Process))
_current_process = _api('GetCurrentProcess', w.HANDLE)
_memory_info = _api('K32GetProcessMemoryInfo', w.BOOL, w.HANDLE, c.POINTER(_Memory), w.DWORD)
_peek_pipe = _api('PeekNamedPipe', w.BOOL, w.HANDLE, c.c_void_p, w.DWORD,
                  c.c_void_p, c.POINTER(w.DWORD), c.c_void_p)
_initialize_attributes = _api('InitializeProcThreadAttributeList', w.BOOL,
    c.c_void_p, w.DWORD, w.DWORD, c.POINTER(c.c_size_t))
_update_attribute = _api('UpdateProcThreadAttribute', w.BOOL,
    c.c_void_p, w.DWORD, c.c_size_t, c.c_void_p, c.c_size_t, c.c_void_p, c.c_void_p)
_delete_attributes = _api('DeleteProcThreadAttributeList', None, c.c_void_p)


@contextmanager
def _standard_handle_startup(output_handle, input_handle, job):
    """Allow only these two owned handles through this process-creation boundary.

    The standard-handle and job arrays live through attribute-list deletion.
    JOB_LIST assigns the child during creation, closing the orphan window before
    a separate AssignProcessToJobObject call. The job handle is not inherited.
    Temporary inheritance flags are restored independently on every exit. This
    does not confine paths or other launchers that still use broad inheritance.
    """
    size = c.c_size_t()
    c.set_last_error(0)
    result = _initialize_attributes(None, 2, 0, c.byref(size))
    if result or c.get_last_error() != 122:  # Expected ERROR_INSUFFICIENT_BUFFER.
        raise OSError('Unexpected process attribute-list sizing result')
    if not 0 < size.value <= 65536:
        raise ValueError('Process attribute-list storage exceeds 64 KiB')
    storage = c.create_string_buffer(size.value)
    handles = (w.HANDLE * 2)(output_handle, input_handle)
    jobs = (w.HANDLE * 1)(job)
    restore = []
    original = None
    _check(_initialize_attributes(storage, 2, 0, c.byref(size)))
    try:
        for handle in handles:
            restore.append((handle, os.get_handle_inheritable(handle)))
            os.set_handle_inheritable(handle, True)
        _check(_update_attribute(storage, 0, 0x00020002, handles, c.sizeof(handles), None, None))
        _check(_update_attribute(storage, 0, 0x0002000d, jobs, c.sizeof(jobs), None, None))
        startup = _StartupEx()
        startup.startup.size, startup.startup.flags = c.sizeof(startup), 0x100  # STARTF_USESTDHANDLES.
        startup.startup.stdin = input_handle
        startup.startup.stdout = startup.startup.stderr = output_handle
        startup.attributes = c.addressof(storage)
        yield startup
    except BaseException as error:
        original = error
        raise
    finally:
        failure = original
        for handle, inherited in restore:
            try: os.set_handle_inheritable(handle, inherited)
            except BaseException as error:
                if failure is None: failure = error
                else: _error_note(failure, 'Standard handle inheritance restoration failed: ', error)
        try: _delete_attributes(storage)
        except BaseException as error:
            if failure is None: failure = error
            else: _error_note(failure, 'Process attribute-list deletion failed: ', error)
        if original is None and failure is not None:
            raise failure


class _BoundedLog:
    """One parent writer for merged stdout/stderr; no inherited file handle.

    A single reader pumps at most 64 KiB per poll. No output queue or reader
    thread survives cleanup. This bounds retained log bytes, not kernel buffers,
    worker output attempts, external file mutations or the owner's whole memory.
    """
    def __init__(self, maximum):
        if type(maximum) is not int or not 0 <= maximum < 2**63:
            raise ValueError('Log byte limit must be a nonnegative signed-64-bit integer')
        self.maximum = maximum
        self.read_bytes = self.accepted = 0
        self.reader = self.writer = self.log = self.identity = None
        self.eof = self.exceeded = self.complete = False
        self.retained = self.error = None

    @contextmanager
    def redirect(self, log):
        self.log = log
        info = os.fstat(log.fileno())
        self.identity = (info.st_dev, info.st_ino)
        self.reader, self.writer = os.pipe()
        original = None
        try:
            yield msvcrt.get_osfhandle(self.writer)
        except BaseException as error:
            original = error
            raise
        finally:
            error = None
            for name in ('writer', 'reader'):
                descriptor = getattr(self, name)
                if descriptor is not None:
                    setattr(self, name, None)
                    try: os.close(descriptor)
                    except OSError as caught:
                        if error is None: error = caught
            if error is not None:
                if original is None: raise error
                _error_note(original, 'Log pipe cleanup failed: ', error)

    def child_started(self):
        descriptor, self.writer = self.writer, None
        os.close(descriptor)

    def pump(self):
        if self.error is not None: raise RuntimeError('Failed log capture cannot be reused')
        if self.eof: return False
        try:
            available = w.DWORD()
            if not _peek_pipe(msvcrt.get_osfhandle(self.reader), None, 0, None, c.byref(available), None):
                code = c.get_last_error()
                if code == 109:  # ERROR_BROKEN_PIPE: all writers closed.
                    self.eof = True
                    return False
                raise c.WinError(code)
            if not available.value: return False
            raw = os.read(self.reader, min(65536, available.value))
            if not raw: raise OSError('Pipe returned no available bytes')
            self.read_bytes += len(raw)
            keep = min(len(raw), self.maximum-self.accepted)
            offset = 0
            while offset < keep:
                count = self.log.write(raw[offset:keep])
                if type(count) is not int or not 0 < count <= keep-offset:
                    raise OSError('Invalid log write result')
                offset += count
                self.accepted += count
            if keep != len(raw):
                self.exceeded = True
                raise ValueError('Worker stdout/stderr exceeded its log byte limit')
            return True
        except BaseException as error:
            self.error = _error_text(error, representation=True)
            raise

    def finish(self, path, failure):
        """Check the closed log even on failure, without masking the first error."""
        if failure is not None and self.error is None: self.error = _error_text(failure, representation=True)
        if self.identity is None: return
        try:
            info = os.lstat(path)
            if (not stat.S_ISREG(info.st_mode) or info.st_nlink != 1
                    or getattr(info, 'st_file_attributes', 0) & 0x400
                    or (info.st_dev, info.st_ino) != self.identity
                    or info.st_size != self.accepted or info.st_size > self.maximum):
                raise ValueError('Changed or incomplete closed worker log')
            self.retained = info.st_size
            self.complete = self.eof and self.error is None
        except BaseException as error:
            if self.error is None: self.error = _error_text(error, representation=True)
            if failure is None: raise
            _error_note(failure, 'Worker log verification failed: ', error)

    def observation(self):
        return dict(version='tower-owned-log-capture-v1', maxBytes=self.maximum,
            acceptedBytes=self.accepted, readBytes=self.read_bytes, retainedBytes=self.retained,
            exceeded=self.exceeded, pipeEof=self.eof, captureComplete=self.complete, error=self.error,
            scope='MergedStdoutStderrThroughParentWriter', wholeProcessCoverage=False, usableForAdmission=False)


def _check(value):
    if not value:
        raise c.WinError(c.get_last_error())
    return value


def _job_io_observation(job, assigned, drained):
    """Kernel job totals after exit, separate from application/durable-byte counts.

    QueryInformationJobObject class 8 includes exited job members. Never turn a
    query failure, unassigned child or unconfirmed drain into a zero-I/O claim.
    https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_and_io_accounting_information
    """
    result = dict(version='tower-owned-job-io-v1', coverage='Unknown', counters=None,
                  activeProcessesAtQuery=None, totalProcessesAtQuery=None, boundary='Unavailable',
                  error=None, wholeProcessCoverage=False, usableForAdmission=False)
    if not assigned or not drained:
        result['error'] = 'Assigned job was not confirmed drained'
        return result
    try:
        value = _AccountingIo()
        _check(_query_job(job, 8, c.byref(value), c.sizeof(value), None))
        if value.basic.active_processes != 0 or value.basic.total_processes < 1:
            raise ValueError('Inconsistent terminal job I/O state')
        names = ('readOperations','writeOperations','otherOperations',
                 'readTransferBytes','writeTransferBytes','otherTransferBytes')
        result.update(coverage='KernelJobLifetimeTotals',
            counters={name:getattr(value.io, field) for name,(field,_) in zip(names,_Io._fields_)},
            activeProcessesAtQuery=0,totalProcessesAtQuery=value.basic.total_processes,
            boundary='AfterConfirmedDrainBeforeHandleCleanup')
    except (OSError, ValueError) as error:
        result['error'] = _error_text(error)
    return result


@contextmanager
def _log_file(path, opened):
    with open(path, 'xb', buffering=0) as log:
        if opened is not None: opened()
        yield log


def environment_block(values):
    """A complete replacement block; never serialize inherited values in evidence."""
    if type(values) is not dict or len(values)>128:
        raise ValueError('Declare at most 128 environment variables')
    copied={}
    for name,value in values.items():
        if type(name) is not str or re.fullmatch(r'[A-Za-z_][A-Za-z0-9_]*',name) is None \
                or type(value) is not str or '\0' in value or name.upper() in copied:
            raise ValueError('Invalid or duplicate environment variable')
        copied[name.upper()]=value
    raw=('\0'.join(k+'='+v for k,v in sorted(copied.items()))+'\0\0').encode('utf-16-le')
    if len(raw)>65534:raise ValueError('Environment exceeds declared UTF-16 block limit')
    return raw,dict(version='tower-owned-environment-v1',sha256=hashlib.sha256(raw).hexdigest(),
        variableCount=len(copied),utf16Bytes=len(raw),inherited=False,scope='ProcessCreationBlockOnly',
        filesystemConfinement=False,runtimeWritesBounded=False,wholeProcessCoverage=False,usableForAdmission=False)


def job_limit_declaration(value):
    """Copied opt-in limits for this job, not the calling monitor or whole host."""
    if type(value) is not dict or set(value) != {'version','maxJobCommitBytes','maxActiveProcesses'} \
            or value['version'] != 'tower-owned-job-limits-v1':
        raise ValueError('Invalid owned job limit declaration')
    memory, processes = value['maxJobCommitBytes'], value['maxActiveProcesses']
    if type(memory) is not int or not 0 < memory < min(2**63, 2**(8*c.sizeof(c.c_size_t))) \
            or memory % mmap.PAGESIZE or type(processes) is not int or not 0 < processes < 2**32:
        raise ValueError('Positive page-aligned commit bytes and positive DWORD active process limit required')
    return dict(value)


class _JobLimits:
    """Install and read back mandatory limits before creating the suspended owner.

    Memory commitment can fail at the ceiling without terminating the process.
    A child may handle that failure; exit status is not a limit-violation counter.
    https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_limit_information
    """
    def __init__(self, value):
        self.declaration = job_limit_declaration(value)
        self.applied = self.verified = False
        self.error = None

    def install(self, job):
        try:
            limits = _Limits()
            limits.basic.flags = 0x2208  # KILL_ON_JOB_CLOSE | JOB_MEMORY | ACTIVE_PROCESS; no breakaway.
            limits.basic.active_limit = self.declaration['maxActiveProcesses']
            limits.job_memory = self.declaration['maxJobCommitBytes']
            _check(_set_job(job, 9, c.byref(limits), c.sizeof(limits)))
            self.applied = True
            observed = _Limits()
            _check(_query_job(job, 9, c.byref(observed), c.sizeof(observed), None))
            if (observed.basic.flags != limits.basic.flags or observed.basic.active_limit != limits.basic.active_limit
                    or observed.job_memory != limits.job_memory):
                raise ValueError('Owned job limit readback mismatch')
            self.verified = True
        except BaseException as error:
            self.error = _error_text(error)
            raise

    def observation(self):
        return dict(version='tower-owned-job-limits-observation-v1', declaration=dict(self.declaration),
            applied=self.applied, verified=self.verified, error=self.error,
            verificationBoundary='BeforeOwnedProcessCreation' if self.verified else 'Unavailable',
            scope='AssignedJobAndDescendants;CommittedVirtualMemoryAndConcurrentProcesses',
            violationsObserved=False, monitorMemoryBounded=False, totalProcessCreationsBounded=False,
            wallClockBounded=False, filesystemConfinement=False, wholeProcessCoverage=False, usableForAdmission=False)


def run(command, cwd, log_path, deadline, cleanup_seconds=1.0, check=None, observe=None, file_work=None, *, log_byte_limit=None, environment=None, job_limits=None):
    """Optionally observe a newly created child log after process/handle cleanup.

    The file collector reports sampled lengths, not child write I/O or a peak.
    It also observes failure tails; default execution has no accounting dependency.
    """
    capture = None if log_byte_limit is None else _BoundedLog(log_byte_limit)
    declared_environment = None if environment is None else environment_block(environment)
    declared_limits = None if job_limits is None else _JobLimits(job_limits)
    if file_work is None:
        return _run(command, cwd, log_path, deadline, cleanup_seconds, check, observe, capture=capture, environment=declared_environment, job_limits=declared_limits)
    with file_work.child_log(log_path) as log:
        return _run(command, cwd, log_path, deadline, cleanup_seconds, check, observe, log.opened, capture, declared_environment, declared_limits)


def _run(command, cwd, log_path, deadline, cleanup_seconds, check, observe, log_opened=None, capture=None, environment=None, job_limits=None):
    """Return measured completion/timeout data; on API failure raise and kill owned work.

    deadline is an absolute monotonic work deadline. The caller must also reserve
    cleanup_seconds inside its phase envelope. Log files are create-new, never reused.
    An optional observer receives a terminal diagnostic after handles close, even
    after failure. Job peak plus owner lifetime peak is a conservative sum of peaks,
    not a sampled simultaneous peak or an incremental owner allocation.
    """
    started = time.monotonic()
    last_check = float('-inf')
    if not math.isfinite(deadline) or deadline <= started or type(cleanup_seconds) not in (int, float) or not 0 < cleanup_seconds <= 2:
        raise ValueError('A future work deadline and bounded cleanup allowance are required.')
    command = [os.fspath(arg) for arg in command]
    if not command or not command[0]:
        raise ValueError('An executable is required.')
    if environment is not None and not os.path.isabs(command[0]):
        raise ValueError('Explicit environment requires an absolute executable')
    executable = (os.path.abspath(command[0]) if os.path.isfile(command[0]) else None) if environment is not None else shutil.which(command[0])
    if executable is None:
        raise FileNotFoundError(command[0])
    environment_buffer = None if environment is None else c.create_string_buffer(environment[0])
    job = _check(_create_job(None, None))
    process = _Process()
    assigned = False
    empty = False
    cleanup_started = False
    cleanup_deadline = None
    terminal = None
    body_failure = None

    def accounting():
        value = _Accounting()
        _check(_query_job(job, 1, c.byref(value), c.sizeof(value), None))
        return value

    def begin_cleanup():
        nonlocal cleanup_deadline
        if cleanup_deadline is None:
            cleanup_deadline = time.monotonic() + cleanup_seconds
        return cleanup_deadline

    def wait_root(message):
        # Drain, termination and root wait consume one allowance. Never round up
        # or restart it; an exhausted allowance permits only a zero-time poll.
        end = begin_cleanup()
        result = _wait(process.process, max(0, math.floor((end-time.monotonic())*1000)))
        if result == 0xffffffff:
            raise c.WinError(c.get_last_error())
        if result != 0:
            raise TimeoutError(message)

    def drain():
        end = begin_cleanup()
        while True:
            value = accounting()
            if value.active_processes == 0:
                return value
            if time.monotonic() >= end:
                raise TimeoutError('Owned job did not empty within its cleanup allowance.')
            time.sleep(min(.01, max(0, end - time.monotonic())))

    try:
        if job_limits is None:
            limits = _Limits()
            limits.basic.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE; no breakaway flags.
            _check(_set_job(job, 9, c.byref(limits), c.sizeof(limits)))
        else:
            job_limits.install(job)
        with _log_file(log_path, log_opened) as log, open(os.devnull, 'rb', buffering=0) as null, \
                (capture.redirect(log) if capture is not None else nullcontext(msvcrt.get_osfhandle(log.fileno()))) as output_handle:
            with _standard_handle_startup(output_handle, msvcrt.get_osfhandle(null.fileno()), job) as startup:
                _check(_create_process(executable, c.create_unicode_buffer(subprocess.list2cmdline(command)),
                    None, None, True, 0x08080004 | (0x400 if environment is not None else 0), environment_buffer,
                    os.fspath(cwd), c.cast(c.byref(startup), c.POINTER(_Startup)), c.byref(process)))
                member = w.BOOL()
                _check(_is_in_job(process.process, job, c.byref(member)))
                if not member.value:
                    raise RuntimeError('Created process is not in its owned job')
                assigned = True
            if capture is not None: capture.child_started()
            if time.monotonic() >= deadline:
                raise TimeoutError('Deadline reached before resuming the assigned process.')
            if _resume(process.thread) == 0xffffffff:
                raise c.WinError(c.get_last_error())
            timed_out = False
            while True:
                pumped = capture.pump() if capture is not None else False
                state = accounting()
                if state.active_processes == 0 and (capture is None or capture.eof):
                    break
                if check is not None and time.monotonic() - last_check >= .25:
                    check()
                    last_check = time.monotonic()
                if time.monotonic() >= deadline:
                    timed_out = True
                    cleanup_started = True
                    begin_cleanup()
                    _check(_terminate_job(job, 124))
                    state = drain()
                    break
                if not pumped: time.sleep(min(.01, max(0, deadline - time.monotonic())))
            empty = True
            wait_root('Empty job has a non-signaled root process.')
            code = w.DWORD()
            _check(_exit_code(process.process, c.byref(code)))
            terminal = dict(exitCode=code.value, timedOut=timed_out, rootPid=process.pid,
                        totalProcesses=state.total_processes, activeProcesses=state.active_processes,
                        seconds=time.monotonic() - started, workAllowanceSeconds=deadline - started,
                        cleanupAllowanceSeconds=cleanup_seconds, mechanism='suspended-owned-job-v1')
        if capture is not None: capture.finish(log_path, None)
        return terminal
    except BaseException as error:
        body_failure = error
        raise
    finally:
        # sys.exc_info() can refer to an already-handled caller exception.
        failure = body_failure
        original_failure = failure
        cleanup_errors = []
        handle_closes = dict(thread='NotAcquired', process='NotAcquired', job='NotAcquired')

        def failed(operation, error):
            nonlocal failure
            cleanup_errors.append(dict(operation=operation, error=_error_text(error)))
            if failure is None:
                failure = error
            elif failure is not error:
                _error_note(failure, 'Owned process '+operation+' failed: ', error)

        peaks = dict(peakJobCommitBytes=None, ownerLifetimePeakCommitBytes=None,
                     combinedCommitUpperBoundBytes=None, memoryCoverage='Unknown')
        job_io = None
        # Diagnostic failures must not skip termination or any handle close.
        if process.process and not empty and not cleanup_started:
            cleanup_started = True
            begin_cleanup()
            try:
                if assigned:
                    _check(_terminate_job(job, 124))
                    drain()
                    empty = True
                    wait_root('Terminated owned root did not signal within cleanup allowance.')
                else:
                    _check(_terminate_process(process.process, 124))
                    wait_root('Suspended unassigned child did not terminate.')
            except BaseException as error:
                failed('terminationAndDrain', error)
        if capture is not None and failure is not None:
            try: capture.finish(log_path, failure)
            except BaseException as error: failed('logFinalization', error)
        if observe is not None:
            try:
                job_io = _job_io_observation(job, assigned, empty)
            except BaseException as error:
                job_io = dict(version='tower-owned-job-io-v1', coverage='Unknown', counters=None,
                    activeProcessesAtQuery=None, totalProcessesAtQuery=None, boundary='Unavailable',
                    error=_error_text(error), wholeProcessCoverage=False, usableForAdmission=False)
                failed('jobIoObservation', error)
            try:
                peak = _Limits()
                _check(_query_job(job, 9, c.byref(peak), c.sizeof(peak), None))
                owner = _Memory()
                owner.size = c.sizeof(owner)
                _check(_memory_info(_current_process(), c.byref(owner), c.sizeof(owner)))
                peaks.update(peakJobCommitBytes=peak.peak_job, ownerLifetimePeakCommitBytes=owner.peak_commit,
                    combinedCommitUpperBoundBytes=peak.peak_job+owner.peak_commit,
                    memoryCoverage='KernelJobHighWaterPlusOwnerLifetimeHighWater')
            except OSError as error:
                peaks['memoryError'] = _error_text(error)
            except BaseException as error: failed('memoryObservation', error)
        # Close each acquired handle exactly once, even if an earlier close fails.
        # Closing a process handle alone does not terminate the process.
        for name, handle in (('thread', process.thread), ('process', process.process), ('job', job)):
            if handle:
                try:
                    _check(_close(handle))
                    handle_closes[name] = 'Closed'
                except BaseException as error:
                    handle_closes[name] = 'CloseFailed'
                    failed(name+'HandleClose', error)
        if observe is not None:
            try:
                observe(dict(version='tower-owned-process-observation-v1', **peaks,
                    jobIo=job_io,
                    cleanup=dict(version='tower-owned-process-cleanup-v1', handleCloses=handle_closes,
                        errors=list(cleanup_errors), allAcquiredHandlesClosed='CloseFailed' not in handle_closes.values(),
                        waitPolicy='SharedMonotonicDeadline;NoRetry', cleanupAllowanceSeconds=cleanup_seconds,
                        boundary='AfterHandleCloseAttemptsBeforeObserverInvocation', observerInvocationExcluded=True,
                        wallClockBounded=False, wholeProcessCoverage=False, usableForAdmission=False),
                    **(dict(logCapture=capture.observation()) if capture is not None else {}),
                    **(dict(environment=dict(environment[1])) if environment is not None else {}),
                    **(dict(jobLimits=job_limits.observation()) if job_limits is not None else {}),
                    enclosingSeconds=time.monotonic()-started, jobDrained=empty,
                    exitCode=None if terminal is None else terminal['exitCode'],
                    timedOut=None if terminal is None else terminal['timedOut'],
                    ownerError=None if failure is None else _error_text(failure),
                    memoryBoundary='BeforeHandleCleanup', includesHandleCleanup=True, usableForAdmission=False))
            except BaseException as error: failed('observerInvocation', error)
        if original_failure is None and failure is not None:
            raise failure
