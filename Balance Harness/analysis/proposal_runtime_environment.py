"""Explicit runtime destinations and bounded metadata samples, not disk quotas."""
import ctypes
import os
from pathlib import Path
import stat
import tempfile

import proposal_work_accounting as work

VERSION='tower-proposal-runtime-environment-v1'
DIRECTORIES=('temp','profile','roaming','local','dotnet','bundle','nuget')
PREPARATION_FAILED='Runtime preparation failed; exception details omitted.'
INSPECTION_FAILED='Runtime inspection failed; exception details omitted.'
INSPECTION_AND_CLOSE_FAILED='Runtime inspection and directory close failed; exception details omitted.'


def declaration(value):
    work.require(type(value) is dict and set(value)=={'version','root','maxEntries','maxDepth','maxRetainedBytes'}
                 and value['version']==VERSION, 'Invalid runtime environment declaration')
    root=value['root']
    work.require(type(root) is str and os.path.isabs(root) and str(Path(os.path.abspath(root)))==root
                 and not root.startswith(('\\\\?\\','\\\\.\\')), 'Runtime root must be canonical and absolute')
    for part in Path(root).parts[1:]:
        stem=part.split('.')[0].upper()
        work.require(part and part[-1] not in ' .' and not any(ord(c)<32 or c in '<>"|?*:' for c in part)
            and stem not in {'CON','PRN','AUX','NUL'} and not (len(stem)==4 and stem[:3] in ('COM','LPT') and stem[3] in '123456789'),
            'Aliased runtime root')
    work.require(type(value['maxEntries']) is int and 0<=value['maxEntries']<=4096
                 and type(value['maxDepth']) is int and 1<=value['maxDepth']<=32
                 and type(value['maxRetainedBytes']) is int and 0<=value['maxRetainedBytes']<2**63,
                 'Invalid runtime inspection limits')
    return dict(value)


def observation(value):
    work.require(type(value) is dict and set(value)=={'version','declaration','started','samples','outcome','lastSample','error',
        'scope','retainedForInspection','transientWritesBounded','runtimeWritesBounded','filesystemConfinement','wholeProcessCoverage','usableForAdmission'},
        'Unknown runtime inspection fields')
    limits=declaration(value['declaration'])
    work.require(value['version']==VERSION and value['scope']=='NamedRuntimeDirectoriesAtInspectionTimes'
        and value['retainedForInspection'] is True and all(value[k] is False for k in
        ('transientWritesBounded','runtimeWritesBounded','filesystemConfinement','wholeProcessCoverage','usableForAdmission')),
        'Unsupported runtime inspection coverage')
    work.require(type(value['started']) is bool and type(value['samples']) is int and value['samples']>=0
        and (value['started'] or value['samples']==0) and (value['error'] is None or type(value['error']) is str and bool(value['error'])),
        'Invalid runtime inspection state')
    if value['outcome']=='FailedOrUnknown':
        work.require(value['lastSample'] is None, 'Failed runtime inspection must remain unknown')
        return limits
    work.require(value['outcome']=='Sampled' and value['started'] and value['samples']>0 and value['error'] is None,
                 'Incomplete runtime inspection')
    sample=value['lastSample']
    work.require(type(sample) is dict and set(sample)=={'entries','sampledRetainedBytes'} and type(sample['entries']) is list
                 and len(sample['entries'])<=limits['maxEntries'], 'Invalid runtime sample')
    names=set();total=0;directories=set()
    for e in sample['entries']:
        work.require(type(e) is dict and set(e)=={'path','kind','bytes'} and type(e['path']) is str and e['path']
                     and e['kind'] in ('file','directory') and type(e['bytes']) is int and e['bytes']>=0,
                     'Invalid runtime sample entry')
        p=Path(e['path']);key=os.path.normcase(str(p))
        work.require(not p.is_absolute() and not p.drive and '..' not in p.parts and p.as_posix()==e['path']
                     and len(p.parts)<=limits['maxDepth'] and key not in names, 'Aliased runtime sample member')
        names.add(key);total+=e['bytes']
        if e['kind']=='directory':
            work.require(e['bytes']==0, 'Directory is not a file byte observation');directories.add(e['path'])
    work.require(set(DIRECTORIES)<=directories and all(len(Path(e['path']).parts)==1 or Path(e['path']).parent.as_posix() in directories for e in sample['entries']),
                 'Incomplete runtime directory sample')
    work.require(type(sample['sampledRetainedBytes']) is int and sample['sampledRetainedBytes']==total<=limits['maxRetainedBytes'],
                 'Inconsistent runtime sampled bytes')
    return limits


class RuntimeEnvironment:
    def __init__(self, value, *, protected_roots=()):
        self._declaration=declaration(value);self.root=Path(self._declaration['root'])
        temp=Path(tempfile.gettempdir()).resolve()
        work.unlinked(self.root)
        work.require(self.root!=temp and self.root.is_relative_to(temp), 'Runtime cache root must be beneath system temp')
        for protected in protected_roots:
            other=Path(os.path.abspath(protected))
            work.require(not (self.root.is_relative_to(other) or other.is_relative_to(self.root)), 'Runtime cache overlaps protected evidence')
        work.require(not self.root.exists() and self.root.parent.is_dir(), 'Runtime cache root must be fresh with an existing parent')
        self.started=False;self.samples=0;self.failed=False;self.last=None;self.error=None

    def environment(self):
        # Read the actual Windows directory, not inherited PATH or SystemRoot.
        kernel=ctypes.WinDLL('kernel32',use_last_error=True)
        function=kernel.GetWindowsDirectoryW;function.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32];function.restype=ctypes.c_uint32
        buffer=ctypes.create_unicode_buffer(32768);length=function(buffer,len(buffer))
        work.require(0<length<len(buffer), 'Cannot identify Windows runtime directory')
        windows=buffer.value
        values={k:str(self.root/leaf) for k,leaf in dict(TEMP='temp',TMP='temp',USERPROFILE='profile',HOME='profile',
            APPDATA='roaming',LOCALAPPDATA='local',DOTNET_CLI_HOME='dotnet',DOTNET_BUNDLE_EXTRACT_BASE_DIR='bundle',NUGET_PACKAGES='nuget').items()}
        values.update(SYSTEMROOT=windows,WINDIR=windows,PATH=str(Path(windows)/'System32')+';'+windows,
            PYTHONDONTWRITEBYTECODE='1',PYTHONNOUSERSITE='1',PYTHONUTF8='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',
            DOTNET_NOLOGO='1',DOTNET_EnableDiagnostics='0',DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='true')
        return values

    def plan(self):
        return dict(**self._declaration,directories=list(DIRECTORIES),environment=self.environment(),
                    scope='ExplicitOwnerEnvironmentAndNamedRuntimeDestinations',filesystemConfinement=False,
                    runtimeWritesBounded=False,wholeProcessCoverage=False,usableForAdmission=False)

    def prepare(self):
        work.require(not self.started, 'Runtime environment cannot be reused')
        self.started=True
        try:
            work.unlinked(self.root);self.root.mkdir()
            for name in DIRECTORIES:(self.root/name).mkdir()
            self.sample()
        except BaseException:
            self.failed=True;self.last=None
            if self.error is None:self.error=PREPARATION_FAILED
            raise

    def sample(self):
        work.require(self.started and not self.failed, 'Runtime inspection is inactive or failed')
        try:
            self.samples+=1;entries=[];total=0
            work.unlinked(self.root);work.require(self.root.is_dir(), 'Runtime root disappeared')
            for name in DIRECTORIES:
                path=self.root/name;work.unlinked(path);work.require(path.is_dir(), 'Runtime destination disappeared')
            pending=[(self.root,0)]
            while pending:
                parent,depth=pending.pop()
                work.unlinked(parent)
                children=os.scandir(parent)
                original=None
                try:
                    for child in children:
                        work.require(len(entries)<self._declaration['maxEntries'], 'Runtime entry inspection allowance exhausted')
                        work.require(depth+1<=self._declaration['maxDepth'], 'Runtime depth inspection allowance exhausted')
                        info=child.stat(follow_symlinks=False)
                        work.require(not stat.S_ISLNK(info.st_mode) and not getattr(info,'st_file_attributes',0)&1024,
                                     'Linked runtime member')
                        is_directory=stat.S_ISDIR(info.st_mode)
                        work.require(is_directory or stat.S_ISREG(info.st_mode), 'Unclassified runtime member')
                        size=0 if is_directory else info.st_size
                        total+=size
                        work.require(total<=self._declaration['maxRetainedBytes'], 'Runtime sampled byte allowance exhausted')
                        entries.append(dict(path=Path(child.path).relative_to(self.root).as_posix(),kind='directory' if is_directory else 'file',bytes=size))
                        if is_directory:pending.append((Path(child.path),depth+1))
                except BaseException as error:
                    original=error
                    raise
                finally:
                    try:children.close()
                    except BaseException:
                        if original is None:raise
                        # Record both failures without inspecting either exception.
                        # The body exception continues through the pending raise.
                        self.error=INSPECTION_AND_CLOSE_FAILED
            self.last=dict(entries=sorted(entries,key=lambda e:e['path']),sampledRetainedBytes=total)
            return total
        except BaseException:
            self.failed=True;self.last=None
            if self.error is None:self.error=INSPECTION_FAILED
            raise

    def snapshot(self):
        value=dict(version=VERSION,declaration=dict(self._declaration),started=self.started,samples=self.samples,
            outcome='Sampled' if self.last is not None and not self.failed else 'FailedOrUnknown',
            lastSample=None if self.last is None else work.strict(work.canonical(self.last)),
            error=self.error,scope='NamedRuntimeDirectoriesAtInspectionTimes',retainedForInspection=True,
            transientWritesBounded=False,runtimeWritesBounded=False,filesystemConfinement=False,
            wholeProcessCoverage=False,usableForAdmission=False)
        observation(value)
        return value
