"""Tamper, path, interruption and full-byte parity tests; no gameplay or admission."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import stat
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

path=Path(__file__).with_name('confirmation-evidence-verification.py')
spec=importlib.util.spec_from_file_location('confirmation_evidence',path)
v=importlib.util.module_from_spec(spec); spec.loader.exec_module(v)

def sha(data): return hashlib.sha256(data).hexdigest()

class VerificationTests(unittest.TestCase):
    def setUp(self):
        self.folder=tempfile.TemporaryDirectory(); self.addCleanup(self.folder.cleanup)
        self.root=Path(self.folder.name)/'package'; self.root.mkdir()
        (self.root/'nested').mkdir(); (self.root/'a.txt').write_bytes(b'first')
        (self.root/'nested/b.bin').write_bytes(bytes(range(256))*1024)
        self.manifest={p.relative_to(self.root).as_posix():sha(p.read_bytes()) for p in self.root.rglob('*') if p.is_file()}
        self.pin=self.seal(self.manifest)

    def seal(self,manifest):
        data=json.dumps(manifest).encode(); (self.root/'files.json').write_bytes(data); return sha(data)

    def test_every_byte_matches_independent_manifest(self):
        report={}; self.assertEqual(self.manifest,v.authenticate(self.root,self.pin,report=report))
        self.assertEqual(2,report['files']); self.assertTrue(report['completeByteVerification'])
        self.assertFalse(report['metadataUsedAsDigestCache'])

    def test_changed_content_with_same_size_and_restored_timestamp_fails(self):
        target=self.root/'a.txt'; old=target.stat(); target.write_bytes(b'wrong')
        os.utime(target,ns=(old.st_atime_ns,old.st_mtime_ns))
        with self.assertRaisesRegex(ValueError,'Changed package file'): v.authenticate(self.root,self.pin)

    def test_changed_pin_and_changed_manifest_fail(self):
        with self.assertRaisesRegex(ValueError,'Changed manifest pin'): v.authenticate(self.root,'0'*64)
        self.seal(dict(self.manifest,extra='a'*64))
        with self.assertRaisesRegex(ValueError,'Changed manifest pin'): v.authenticate(self.root,self.pin)

    def test_missing_and_unlisted_files_fail(self):
        extra=self.root/'unlisted'; extra.write_text('unexpected')
        with self.assertRaisesRegex(ValueError,'membership'): v.authenticate(self.root,self.pin)
        extra.unlink(); (self.root/'a.txt').unlink()
        with self.assertRaisesRegex(ValueError,'membership'): v.authenticate(self.root,self.pin)

    def test_unsafe_paths_fail_before_file_read(self):
        for name in ('../outside','/absolute','C:/absolute','a/../b','a//b','a/./b','a\\b','a:stream',
            'a.','a ','a/CON.txt','NUL','a/COM9','a/\x01bad','files.json'):
            with self.subTest(name=name),self.assertRaises(ValueError):
                v._manifest(json.dumps({name:'a'*64}).encode())

    def test_duplicate_keys_case_aliases_and_bad_hashes_fail(self):
        for data in (b'{"x":"a","x":"b"}',json.dumps({'a':'a'*64,'A':'b'*64}).encode(),
                     b'[]',b'{}',json.dumps({'a':'A'*64}).encode(),json.dumps({'a':None}).encode()):
            with self.subTest(data=data),self.assertRaises(ValueError): v._manifest(data)

    def test_reparse_points_and_symlinks_fail_including_root_ancestors(self):
        info=SimpleNamespace(st_mode=stat.S_IFDIR,st_file_attributes=0x400)
        with self.assertRaisesRegex(ValueError,'Linked artifact'): v._unlinked(info,'junction')
        with self.assertRaisesRegex(ValueError,'Linked artifact'):
            v._unlinked(SimpleNamespace(st_mode=stat.S_IFLNK,st_file_attributes=0),'symlink')
        original=v.os.lstat
        def lstat(path): return info if Path(path)==self.root.parent else original(path)
        with patch.object(v.os,'lstat',lstat),self.assertRaisesRegex(ValueError,'Linked artifact'): v.authenticate(self.root,self.pin)

    def test_real_directory_link_is_rejected_when_supported(self):
        target=Path(self.folder.name)/'outside'; target.mkdir()
        linked=self.root/'linked'
        if os.name=='nt':
            import _winapi
            _winapi.CreateJunction(str(target),str(linked))
        else: linked.symlink_to(target,target_is_directory=True)
        try:
            with self.assertRaisesRegex(ValueError,'Linked artifact'): v.authenticate(self.root,self.pin)
        finally:
            if os.name=='nt': linked.rmdir()
            else: linked.unlink()

    def test_file_changed_between_scan_and_open_fails(self):
        original=v._read
        def read(root,name,expected,capture=False):
            if name=='a.txt': (self.root/name).write_bytes(b'longer')
            return original(root,name,expected,capture)
        with patch.object(v,'_read',read),self.assertRaisesRegex(ValueError,'File changed before read'): v.authenticate(self.root,self.pin)

    def test_membership_change_after_hash_is_detected(self):
        original=v._scan; calls=0
        def scan(root,check):
            nonlocal calls
            calls+=1
            if calls==2: (self.root/'late').write_text('new')
            return original(root,check)
        with patch.object(v,'_scan',scan),self.assertRaisesRegex(ValueError,'Package changed'): v.authenticate(self.root,self.pin)

    def test_deadline_interruption_propagates_without_mutation(self):
        before={p.relative_to(self.root).as_posix():p.read_bytes() for p in self.root.rglob('*') if p.is_file()}
        def stop(): raise TimeoutError('fixed deadline')
        with self.assertRaisesRegex(TimeoutError,'fixed deadline'): v.authenticate(self.root,self.pin,check=stop)
        self.assertEqual(before,{p.relative_to(self.root).as_posix():p.read_bytes() for p in self.root.rglob('*') if p.is_file()})

    def test_metadata_is_never_used_to_skip_a_second_hash(self):
        original=v._read; names=[]
        def read(root,name,expected,capture=False):
            names.append(name); return original(root,name,expected,capture)
        with patch.object(v,'_read',read):
            v.authenticate(self.root,self.pin); v.authenticate(self.root,self.pin)
        self.assertEqual(2,names.count('a.txt')); self.assertEqual(2,names.count('nested/b.bin'))


if __name__=='__main__': unittest.main(verbosity=2)
