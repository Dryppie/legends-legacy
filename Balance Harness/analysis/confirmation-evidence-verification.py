"""Authenticate every member of a sealed confirmation package without per-file path rebuilding.

No cached digest or subset substitutes for a fresh full inventory and byte check.
The caller owns the deadline and any writer leases. This module has no launch route.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import time

HASH=re.compile(r'[0-9a-f]{64}\Z')
REPARSE=0x400
DEVICES={'CON','PRN','AUX','NUL','CONIN$','CONOUT$'}|{prefix+str(i) for prefix in ('COM','LPT') for i in range(1,10)}

def require(ok,message):
    if not ok: raise ValueError(message)

def _unlinked(info,name):
    require(not stat.S_ISLNK(info.st_mode) and not getattr(info,'st_file_attributes',0)&REPARSE,'Linked artifact: '+name)

def _identity(info):
    # On Windows this Python runtime reports different ctime semantics for lstat/fstat.
    return info.st_dev,info.st_ino,info.st_size,info.st_mtime_ns

def _stamp(info):
    # Windows DirEntry.stat omits device/inode IDs; compare those on the opened handle instead.
    return info.st_size,info.st_mtime_ns,info.st_ctime_ns

def _root(path):
    root=os.path.abspath(os.fspath(path))
    for parent in (Path(root),*Path(root).parents): _unlinked(os.lstat(parent),str(parent))
    require(os.path.isdir(root),'Missing package root')
    return root

def _scan(root,check):
    files={}; pending=[(root,'')]; directories=0
    while pending:
        parent,prefix=pending.pop(); directories+=1
        require(directories<=2000000,'Directory scan limit exceeded')
        check()
        with os.scandir(parent) as entries:
            for entry in entries:
                info=entry.stat(follow_symlinks=False); name=prefix+entry.name
                _unlinked(info,name)
                if stat.S_ISDIR(info.st_mode): pending.append((entry.path,name+'/'))
                else:
                    require(stat.S_ISREG(info.st_mode),'Non-file package member: '+name)
                    require(name not in files,'Duplicate package member')
                    files[name]=_stamp(info)
                    require(len(files)<=2000000,'File scan limit exceeded')
    return files

def _read(root,name,expected,capture=False):
    path=os.path.join(root,*name.split('/'))
    info=os.lstat(path); _unlinked(info,name)
    require(_stamp(info)==expected,'File changed before read: '+name)
    identity=_identity(info)
    with open(path,'rb') as stream:
        require(_identity(os.fstat(stream.fileno()))==identity,'File changed while opening: '+name)
        if capture:
            data=stream.read(); digest=hashlib.sha256(data).hexdigest()
        else:
            data=None; digest=hashlib.file_digest(stream,'sha256').hexdigest()
        require(_identity(os.fstat(stream.fileno()))==identity,'File changed while reading: '+name)
    return digest,data

def _pairs(pairs):
    result={}
    for name,value in pairs:
        require(name not in result,'Duplicate manifest key: '+name); result[name]=value
    return result

def _manifest(data):
    result=json.loads(data.decode('utf-8-sig'),object_pairs_hook=_pairs)
    require(isinstance(result,dict) and 0<len(result)<=2000000,'Invalid manifest')
    folded=set()
    for name,pin in result.items():
        require(isinstance(name,str) and name!='files.json' and not name.startswith('/')
            and not any(c in name for c in '\\:<>"|?*') and not any(ord(c)<32 for c in name),'Unsafe manifest path')
        parts=name.split('/')
        require(all(p and p not in ('.','..') and p==p.rstrip(' .') for p in parts),'Noncanonical manifest path')
        require(all(p.split('.',1)[0].upper() not in DEVICES for p in parts),'Device manifest path')
        require(name.casefold() not in folded,'Case-aliased manifest path'); folded.add(name.casefold())
        require(isinstance(pin,str) and HASH.fullmatch(pin),'Invalid file digest')
    return result

def authenticate(path,pin,check=lambda:None,report=None):
    """Hash every file; check full membership and file metadata before and after reads."""
    require(isinstance(pin,str) and HASH.fullmatch(pin),'Invalid external manifest pin')
    started=time.monotonic(); root=_root(path); check(); before=_scan(root,check)
    require('files.json' in before,'Missing manifest')
    digest,data=_read(root,'files.json',before['files.json'],capture=True)
    require(digest==pin,'Changed manifest pin'); manifest=_manifest(data)
    require(before.keys()==manifest.keys()|{'files.json'},'Changed package membership')
    inventory_done=time.monotonic()
    for index,(name,expected) in enumerate(manifest.items()):
        if index%256==0: check()
        actual,_=_read(root,name,before[name]); require(actual==expected,'Changed package file: '+name)
    hashing_done=time.monotonic(); check()
    require(_root(path)==root and _scan(root,check)==before,'Package changed during authentication')
    actual,_=_read(root,'files.json',before['files.json']); require(actual==pin,'Manifest changed during authentication')
    check()
    if report is not None:
        report.update(status='CompletePackageAuthenticated',manifestSha256=pin,files=len(manifest),
            bytes=sum(value[0] for value in before.values()),inventorySeconds=inventory_done-started,
            hashingSeconds=hashing_done-inventory_done,closingSeconds=time.monotonic()-hashing_done,
            seconds=time.monotonic()-started,completeByteVerification=True,metadataUsedAsDigestCache=False)
    return manifest
