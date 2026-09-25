"""Opt-in, two-path Windows owner leases. No hard elapsed-time guarantee."""
from contextlib import contextmanager
import ctypes
import os
from pathlib import Path
import threading

import proposal_work_accounting as work

VERSION = 'tower-proposal-owner-leases-v1'


def declaration(value):
    work.require(type(value) is dict and set(value)=={'version','maxConcurrentLeases'}
                 and value['version']==VERSION and type(value['maxConcurrentLeases']) is int
                 and 0 <= value['maxConcurrentLeases'] <= 2, 'Invalid owner lease declaration')
    return dict(value)


def catalogue(output):
    root=Path(os.path.abspath(output))
    work.require(os.path.isabs(output) and Path(output)==root, 'Owner lease root must be canonical and absolute')
    paths=[str(root.parent/'complete-family-allocation.writer.lock'), str(root)+'.writer.lock']
    work.require(len(set(map(Path,paths)))==2, 'Aliased owner lease catalogue')
    for name in [str(root),*paths]:
        work.require(not name.startswith(('\\\\?\\','\\\\.\\')), 'Device owner lease path')
        for part in Path(name).parts[1:]:
            stem=part.split('.')[0].upper()
            work.require(part and part[-1] not in ' .' and ':' not in part and '\x00' not in part
                and not any(ord(c)<32 or c in '<>"|?*' for c in part)
                and stem not in {'CON','PRN','AUX','NUL'}
                and not (len(stem)==4 and stem[:3] in ('COM','LPT') and stem[3] in '123456789'),
                'Aliased or invalid owner lease path')
    return [dict(path=p,role=role,maxAttempts=1) for p,role in zip(paths,('RegistryAllocation','StudyOutput'))]


def observation(value):
    """Independent schema and lifetime consistency validation; no filesystem IO."""
    work.require(type(value) is dict and set(value)=={'version','studyRoot','declaration','entries','outcome',
        'closed','failedOrUnknown','currentOwnedHandles','peakOwnedHandles','maxLeaseBytes','acceptedWriteBytes',
        'scope','wallClockBounded','filesystemConfinement','wholeProcessCoverage','usableForAdmission'},
        'Unknown owner lease observation fields')
    work.require(value['version']==VERSION and value['scope']=='DeclaredEnclosingOwnerHandlesOnly'
        and all(value[k] is False for k in ('wallClockBounded','filesystemConfinement','wholeProcessCoverage','usableForAdmission')),
        'Unsupported owner lease coverage')
    root=value['studyRoot']
    work.require(type(root) is str and os.path.isabs(root) and str(Path(os.path.abspath(root)))==root, 'Invalid owner lease root')
    budget=declaration(value['declaration']);expected=catalogue(root)
    work.require(type(value['entries']) is list and len(value['entries'])==2, 'Changed owner lease catalogue')
    acquired=released=failures=forced=0
    for e,p in zip(value['entries'],expected):
        counts=('attempts','acquired','released','releaseFailures','forcedCleanup')
        work.require(type(e) is dict and set(e)==set(p)|set(counts) and all(e[k]==v for k,v in p.items())
            and type(e['maxAttempts']) is int and all(type(e[k]) is int and 0<=e[k]<=1 for k in counts), 'Invalid owner lease entry')
        work.require(e['released']+e['releaseFailures']<=e['acquired']<=e['attempts'] and e['forcedCleanup']<=e['acquired'],
                     'Inconsistent owner lease lifetime')
        acquired+=e['acquired'];released+=e['released'];failures+=e['releaseFailures'];forced+=e['forcedCleanup']
    live=value['currentOwnedHandles'];peak=value['peakOwnedHandles']
    work.require(type(live) is int and type(peak) is int and live==acquired-released
                 and 0<=live<=peak<=min(acquired,budget['maxConcurrentLeases']), 'Inconsistent owner lease handle bounds')
    work.require(all(type(value[k]) is int and value[k]==0 for k in ('maxLeaseBytes','acceptedWriteBytes')),
                 'Owner leases must remain empty')
    work.require(type(value['closed']) is bool and type(value['failedOrUnknown']) is bool
                 and value['outcome'] in ('Complete','FailedOrIncomplete'), 'Invalid owner lease outcome')
    work.require(value['outcome']!='Complete' or value['closed'] and not value['failedOrUnknown']
                 and acquired==released==peak==2 and not (live or failures or forced), 'Incomplete owner lease scope')
    return budget


class OwnerLeases:
    def __init__(self, output, budget, *, protected_paths=()):
        self._root=Path(os.path.abspath(output));self._budget=declaration(budget)
        self._entries=[dict(**e,attempts=0,acquired=0,released=0,releaseFailures=0,forcedCleanup=0) for e in catalogue(output)]
        for e in self._entries:
            p=Path(e['path']);work.unlinked(p)
            for protected in protected_paths:
                other=Path(os.path.abspath(protected))
                work.require(not (p==other or p.is_relative_to(other) or other.is_relative_to(p)),
                             'Owner lease overlaps a protected path')
        self._handles={};self._peak=0;self._failed=self._closed=False
        self._lock=threading.RLock()

    def plan(self):
        return dict(version=VERSION,studyRoot=str(self._root),declaration=dict(self._budget),
                    catalogue=[{k:e[k] for k in ('path','role','maxAttempts')} for e in self._entries])

    def require_held(self):
        with self._lock:
            try:
                work.require(not self._closed and not self._failed and len(self._handles)==2
                             and all(e['acquired']==1 and e['released']==0 for e in self._entries),
                             'Both enclosing owner leases must be held')
            except BaseException:
                self._failed=True
                raise

    @staticmethod
    def _observe(counters, name, action):
        return action() if counters is None else counters.operation(name,action)

    def _open(self, root, counters):
        with self._lock:
            try:
                work.require(not self._closed and not self._failed, 'Owner lease scope is closed or failed')
                path=Path(os.path.abspath(str(root)+'.writer.lock'))
                entry=next((e for e in self._entries if Path(e['path'])==path),None)
                work.require(entry is not None, 'Undeclared owner lease path')
                work.require(entry['attempts']==0, 'Owner lease attempt exhausted')
                live=sum(e['acquired']-e['released'] for e in self._entries)
                work.require(live < self._budget['maxConcurrentLeases'], 'Owner lease concurrency exhausted')
                entry['attempts']=1
                work.unlinked(path)
                work.require(not path.exists(), 'Owner lease already exists')
                kernel=ctypes.WinDLL('kernel32',use_last_error=True)
                create=kernel.CreateFileW
                create.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p]
                create.restype=ctypes.c_void_p
                close=kernel.CloseHandle;close.argtypes=[ctypes.c_void_p];close.restype=ctypes.c_int
                size=kernel.GetFileSizeEx;size.argtypes=[ctypes.c_void_p,ctypes.POINTER(ctypes.c_longlong)];size.restype=ctypes.c_int
                def acquire():
                    # GENERIC_READ | DELETE; CREATE_NEW; no sharing; DeleteOnClose.
                    # No raw handle is yielded to the caller.
                    handle=create(str(path),0x80010000,0,None,1,0x04000000,None)
                    work.require(handle != ctypes.c_void_p(-1).value, 'Owner lease already leased or inaccessible')
                    # Own the handle before the collector records completion.
                    entry['acquired']=1
                    self._handles[str(path)]=(handle,close,size,counters)
                    self._peak=max(self._peak,live+1)
                    return handle
                handle=self._observe(counters,'writerLeaseAcquire',acquire)
                self._empty(handle,size)
                return path
            except BaseException:
                self._failed=True
                raise

    @staticmethod
    def _empty(handle, size):
        length=ctypes.c_longlong()
        if not size(handle,ctypes.byref(length)):raise ctypes.WinError(ctypes.get_last_error())
        work.require(length.value==0, 'Owner lease is not empty')

    def _release(self, path, original=None):
        with self._lock:
            key=str(path)
            if key not in self._handles:return
            # Never retry an ambiguous close; keep unresolved counts on failure.
            handle,close,size,counters=self._handles.pop(key)
            entry=next(e for e in self._entries if e['path']==key)
            invoked=False
            def release():
                nonlocal invoked
                invoked=True
                failure=None
                try:self._empty(handle,size)
                except BaseException as error:failure=error
                try:
                    if not close(handle):raise ctypes.WinError(ctypes.get_last_error())
                except BaseException as error:
                    if failure is None:failure=error
                    else:failure.add_note('Owner lease close failed: '+repr(error))
                if failure is not None:raise failure
                work.unlinked(path)
                work.require(not path.exists(), 'Owner lease path remains after close')
            try:
                self._observe(counters,'writerLeaseRelease',release)
                entry['released']=1
            except BaseException as error:
                # A collector failure before its callback cannot skip cleanup.
                if not invoked:
                    try:release()
                    except BaseException as cleanup:error.add_note('Owner lease cleanup failed: '+repr(cleanup))
                self._failed=True;entry['releaseFailures']=1
                if counters is not None:
                    try:counters.add('writerLeaseReleaseStateUnknown')
                    except BaseException as accounting:error.add_note('Owner lease accounting failed: '+repr(accounting))
                if original is None:raise
                original.add_note('Owner lease release failed: '+repr(error))

    @contextmanager
    def acquire(self, root, counters=None):
        # An open that fails after acquiring is retained for finish() cleanup.
        path=self._open(root,counters);failure=None
        try:yield
        except BaseException as error:
            self._failed=True;failure=error
            raise
        finally:self._release(path,failure)

    def finish(self, error=None):
        with self._lock:
            work.require(not self._closed, 'Owner lease scope cannot close twice')
            self._closed=True
            if error is not None:self._failed=True
            failure=error
            for key in list(self._handles):
                self._failed=True
                next(e for e in self._entries if e['path']==key)['forcedCleanup']=1
                try:self._release(Path(key),failure)
                except BaseException as caught:failure=caught
            if self._failed or self._peak!=2 or any(e['released']!=1 for e in self._entries):
                self._failed=True
                if failure is None:failure=ValueError('Owner lease scope failed or incomplete')
            if error is None and failure is not None:raise failure

    def snapshot(self):
        with self._lock:
            result=dict(version=VERSION,studyRoot=str(self._root),declaration=dict(self._budget),
                entries=[dict(e) for e in self._entries],closed=self._closed,failedOrUnknown=self._failed,
                outcome='Complete' if self._closed and not self._failed else 'FailedOrIncomplete',
                currentOwnedHandles=sum(e['acquired']-e['released'] for e in self._entries),peakOwnedHandles=self._peak,
                maxLeaseBytes=0,acceptedWriteBytes=0,scope='DeclaredEnclosingOwnerHandlesOnly',wallClockBounded=False,
                filesystemConfinement=False,wholeProcessCoverage=False,usableForAdmission=False)
            observation(result)
            return result
