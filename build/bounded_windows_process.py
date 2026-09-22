"""Bound trusted diagnostic process trees with an owned Windows Job Object.

Create suspended, assign before resuming, prohibit breakaway, and verify the job is
empty before reporting completion. Never infer tree completion from the root PID.
https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects
"""
import ctypes as c
from ctypes import wintypes as w
import math
import msvcrt
import os
import shutil
import subprocess
import time


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


class _Startup(c.Structure):
    _fields_ = [('size', w.DWORD), ('reserved', w.LPWSTR), ('desktop', w.LPWSTR), ('title', w.LPWSTR)] + [
        (n, w.DWORD) for n in ('x', 'y', 'width', 'height', 'chars_x', 'chars_y', 'fill', 'flags')] + [
        ('show', w.WORD), ('reserved_bytes', w.WORD), ('reserved_pointer', c.c_void_p),
        ('stdin', w.HANDLE), ('stdout', w.HANDLE), ('stderr', w.HANDLE)]


class _Process(c.Structure):
    _fields_ = [('process', w.HANDLE), ('thread', w.HANDLE), ('pid', w.DWORD), ('tid', w.DWORD)]


_kernel = c.WinDLL('kernel32', use_last_error=True)


def _api(name, result, *args):
    f = getattr(_kernel, name)
    f.restype, f.argtypes = result, args
    return f


_create_job = _api('CreateJobObjectW', w.HANDLE, c.c_void_p, w.LPCWSTR)
_set_job = _api('SetInformationJobObject', w.BOOL, w.HANDLE, c.c_int, c.c_void_p, w.DWORD)
_query_job = _api('QueryInformationJobObject', w.BOOL, w.HANDLE, c.c_int, c.c_void_p, w.DWORD, c.c_void_p)
_assign = _api('AssignProcessToJobObject', w.BOOL, w.HANDLE, w.HANDLE)
_terminate_job = _api('TerminateJobObject', w.BOOL, w.HANDLE, w.UINT)
_terminate_process = _api('TerminateProcess', w.BOOL, w.HANDLE, w.UINT)
_close = _api('CloseHandle', w.BOOL, w.HANDLE)
_resume = _api('ResumeThread', w.DWORD, w.HANDLE)
_wait = _api('WaitForSingleObject', w.DWORD, w.HANDLE, w.DWORD)
_exit_code = _api('GetExitCodeProcess', w.BOOL, w.HANDLE, c.POINTER(w.DWORD))
_create_process = _api('CreateProcessW', w.BOOL, w.LPCWSTR, w.LPWSTR, c.c_void_p, c.c_void_p,
                       w.BOOL, w.DWORD, c.c_void_p, w.LPCWSTR, c.POINTER(_Startup), c.POINTER(_Process))


def _check(value):
    if not value:
        raise c.WinError(c.get_last_error())
    return value


def run(command, cwd, log_path, deadline, cleanup_seconds=1.0, check=None):
    """Return measured completion/timeout data; on API failure raise and kill owned work.

    deadline is an absolute monotonic work deadline. The caller must also reserve
    cleanup_seconds inside its phase envelope. Log files are create-new, never reused.
    """
    started = time.monotonic()
    last_check = float('-inf')
    if not math.isfinite(deadline) or deadline <= started or not 0 < cleanup_seconds <= 2:
        raise ValueError('A future work deadline and bounded cleanup allowance are required.')
    command = [os.fspath(arg) for arg in command]
    if not command or not command[0]:
        raise ValueError('An executable is required.')
    executable = shutil.which(command[0])
    if executable is None:
        raise FileNotFoundError(command[0])
    job = _check(_create_job(None, None))
    process = _Process()
    assigned = False
    empty = False
    cleanup_started = False

    def accounting():
        value = _Accounting()
        _check(_query_job(job, 1, c.byref(value), c.sizeof(value), None))
        return value

    def drain():
        end = time.monotonic() + cleanup_seconds
        while True:
            value = accounting()
            if value.active_processes == 0:
                return value
            if time.monotonic() >= end:
                raise TimeoutError('Owned job did not empty within its cleanup allowance.')
            time.sleep(min(.01, max(0, end - time.monotonic())))

    try:
        limits = _Limits()
        limits.basic.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE; no breakaway flags.
        _check(_set_job(job, 9, c.byref(limits), c.sizeof(limits)))
        with open(log_path, 'xb', buffering=0) as log, open(os.devnull, 'rb', buffering=0) as null:
            handles = [msvcrt.get_osfhandle(log.fileno()), msvcrt.get_osfhandle(null.fileno())]
            startup = _Startup()
            startup.size, startup.flags = c.sizeof(startup), 0x100  # STARTF_USESTDHANDLES
            startup.stdin, startup.stdout, startup.stderr = handles[1], handles[0], handles[0]
            try:
                for handle in handles:
                    os.set_handle_inheritable(handle, True)
                _check(_create_process(executable, c.create_unicode_buffer(subprocess.list2cmdline(command)),
                    None, None, True, 0x08000004, None, os.fspath(cwd), c.byref(startup), c.byref(process)))
            finally:
                for handle in handles:
                    os.set_handle_inheritable(handle, False)
            _check(_assign(job, process.process))
            assigned = True
            if time.monotonic() >= deadline:
                raise TimeoutError('Deadline reached before resuming the assigned process.')
            if _resume(process.thread) == 0xffffffff:
                raise c.WinError(c.get_last_error())
            timed_out = False
            while True:
                state = accounting()
                if state.active_processes == 0:
                    break
                if check is not None and time.monotonic() - last_check >= .25:
                    check()
                    last_check = time.monotonic()
                if time.monotonic() >= deadline:
                    timed_out = True
                    cleanup_started = True
                    _check(_terminate_job(job, 124))
                    state = drain()
                    break
                time.sleep(min(.01, max(0, deadline - time.monotonic())))
            empty = True
            if _wait(process.process, math.ceil(cleanup_seconds * 1000)) != 0:
                raise RuntimeError('Empty job has a non-signaled root process.')
            code = w.DWORD()
            _check(_exit_code(process.process, c.byref(code)))
            return dict(exitCode=code.value, timedOut=timed_out, rootPid=process.pid,
                        totalProcesses=state.total_processes, activeProcesses=state.active_processes,
                        seconds=time.monotonic() - started, workAllowanceSeconds=deadline - started,
                        cleanupAllowanceSeconds=cleanup_seconds, mechanism='suspended-owned-job-v1')
    finally:
        try:
            if process.process and not empty and not cleanup_started:
                if assigned:
                    _check(_terminate_job(job, 124))
                    drain()
                    if _wait(process.process, math.ceil(cleanup_seconds * 1000)) != 0:
                        raise TimeoutError('Terminated owned root did not signal within cleanup allowance.')
                else:
                    _check(_terminate_process(process.process, 124))
                    if _wait(process.process, math.ceil(cleanup_seconds * 1000)) != 0:
                        raise TimeoutError('Suspended unassigned child did not terminate.')
        finally:
            for handle in (process.thread, process.process, job):
                if handle:
                    _check(_close(handle))
