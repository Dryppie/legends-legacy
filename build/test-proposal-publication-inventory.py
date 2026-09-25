"""Exact hash-work and publication-integrity tests; all workers are literal routes."""
from collections import Counter
import contextlib
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('publication_inventory_fixture',ROOT/'build/test-proposal-owner-supervisor.py')
fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
owner,work,owned=fixture.launcher,fixture.w,fixture.owned

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class Inventory(unittest.TestCase):
    def setUp(self):
        temporary=tempfile.TemporaryDirectory();self.addCleanup(temporary.cleanup)
        self.root=Path(temporary.name)
        (self.root/'input.bin').write_bytes(bytes(range(256))*4096)
        (self.root/'nested').mkdir();(self.root/'nested/evidence.json').write_bytes(b'{"literal":true}')
        self.protected={p.relative_to(self.root).as_posix():sha(p) for p in owner.inventory(self.root)}
        (self.root/'audit.json').write_bytes(b'{"status":"Passed"}')

    def test_fresh_hashes_match_legacy_manifest_and_each_file_is_read_once(self):
        calls=[];digest=owner.digest
        def counted(path):calls.append(path.relative_to(self.root).as_posix());return digest(path)
        with patch.object(owner,'digest',counted):result=owner.publication_inventory(self.root,self.protected)
        expected={p.relative_to(self.root).as_posix():sha(p) for p in owner.inventory(self.root)}
        self.assertEqual(result,expected);self.assertEqual(Counter(calls),Counter({name:1 for name in expected}))

    def test_accounted_read_bytes_remove_exactly_one_protected_pass(self):
        legacy=work.OwnerFileCounters();current=work.OwnerFileCounters()
        with owner.file_accounting(legacy):
            self.assertTrue(all(owner.digest(self.root/name)==pin for name,pin in self.protected.items()))
            expected={p.relative_to(self.root).as_posix():owner.digest(p) for p in owner.inventory(self.root)}
        with owner.file_accounting(current):result=owner.publication_inventory(self.root,self.protected)
        def reads(counters):return sum(v for k,v in counters.values.items() if k.startswith('applicationReadBytes.'))
        saved=sum((self.root/name).stat().st_size for name in self.protected)
        self.assertEqual(expected,result);self.assertEqual(reads(legacy)-reads(current),saved)
        self.assertEqual(reads(current),sum(p.stat().st_size for p in owner.inventory(self.root)))
        export=os.environ.get('LL_PUBLICATION_INVENTORY_EXPORT')
        if export:
            target=Path(export);target.mkdir()
            value=dict(fixtureOnly=True,actualCombat=0,productionEntropyDraws=0,scientificReservations=0,
                inputSizes={p.relative_to(self.root).as_posix():p.stat().st_size for p in owner.inventory(self.root)},
                protected=self.protected,legacyManifest=expected,currentManifest=result,
                legacyReadBytes=reads(legacy),currentReadBytes=reads(current),removedReadBytes=saved,
                legacyCounters=dict(legacy.values),currentCounters=dict(current.values),
                sources={str(Path(p).resolve().relative_to(ROOT).as_posix()):sha(p) for p in (__file__,owner.__file__,work.__file__,fixture.__file__)},
                timingSample=False,resourceForecast=None,wholeProcessCoverage=False,usableForAdmission=False)
            work.seal_new(target/'fixture.json',value)
            work.seal_new(target/'files.json',{'fixture.json':sha(target/'fixture.json')})

    def test_same_length_changed_protected_content_is_rejected(self):
        path=self.root/'nested/evidence.json';path.write_bytes(path.read_bytes().replace(b'true',b'null'))
        with self.assertRaisesRegex(ValueError,'Audit inputs changed'):owner.publication_inventory(self.root,self.protected)

    def test_missing_protected_file_is_rejected(self):
        (self.root/'input.bin').unlink()
        with self.assertRaisesRegex(ValueError,'Audit inputs changed'):owner.publication_inventory(self.root,self.protected)

    def test_renamed_protected_file_is_rejected_even_with_same_bytes(self):
        (self.root/'input.bin').rename(self.root/'other.bin')
        with self.assertRaisesRegex(ValueError,'Audit inputs changed'):owner.publication_inventory(self.root,self.protected)

    def test_new_audit_files_are_hashed_instead_of_excluded(self):
        path=self.root/'audit.json';path.write_bytes(b'{"additional":"evidence"}')
        result=owner.publication_inventory(self.root,self.protected)
        self.assertEqual(result['audit.json'],sha(path));self.assertNotIn('audit.json',self.protected)

    def test_scan_failure_propagates_without_a_partial_manifest(self):
        failure=OSError('literal scan failure')
        with patch.object(owner,'inventory',side_effect=failure),self.assertRaises(OSError) as caught:
            owner.publication_inventory(self.root,self.protected)
        self.assertIs(caught.exception,failure);self.assertFalse((self.root/'files.json').exists())

    def test_hash_failure_propagates_without_a_partial_manifest(self):
        failure=OSError('literal read failure')
        with patch.object(owner,'digest',side_effect=failure),self.assertRaises(OSError) as caught:
            owner.publication_inventory(self.root,self.protected)
        self.assertIs(caught.exception,failure);self.assertFalse((self.root/'files.json').exists())


class Publication(unittest.TestCase):
    def setUp(self):
        self.f=fixture.Supervisor();self.f.setUp();self.addCleanup(self.f.doCleanups)

    def launch(self,accounting,route=None):
        output=io.StringIO()
        with patch.object(owned,'run',side_effect=route or self.f.routed),contextlib.redirect_stdout(output):
            owner.launch(self.f.package/'request.json',self.f.package/'runtime/BalanceHarness.dll',
                'dotnet',sha(self.f.package/'files.json'),accounting=accounting)
        return json.loads(output.getvalue())

    def unsealed(self):
        self.assertTrue((self.f.output/'failure.json').exists())
        self.assertFalse((self.f.output/'completion.json').exists())
        self.assertFalse((self.f.output/'files.json').exists())
        self.assertFalse((self.f.output/'closeout.json').exists())

    def test_default_and_accounted_modes_keep_all_workers_and_bound_manifest(self):
        for counted in (False,True):
            with self.subTest(counted=counted):
                base=self.f.base/str(counted);self.f.calls=[];supervisor=self.f.prepare(base)
                result=self.launch(supervisor if counted else None)
                self.assertEqual([p for p,_,_ in self.f.calls],list(work.PHASES))
                self.assertEqual(result['status'],'Complete')
                manifest=work.strict((self.f.output/'files.json').read_bytes())
                self.assertEqual(set(manifest),{p.relative_to(self.f.output).as_posix() for p in owner.inventory(self.f.output)}-{'files.json','closeout.json'})
                for name,pin in manifest.items():self.assertEqual(sha(self.f.output/name),pin)
                self.assertEqual(work.strict((self.f.output/'closeout.json').read_bytes())['filesHash'],sha(self.f.output/'files.json'))

    def test_changed_input_after_publication_worker_prevents_sealing(self):
        self.f.prepare()
        def route(*args,**kwargs):
            result=self.f.routed(*args,**kwargs)
            if self.f.calls[-1][0]=='publication':(self.f.output/'native-console.log').write_bytes(b'tampered')
            return result
        with self.assertRaisesRegex(ValueError,'Audit inputs changed'):self.launch(None,route)
        self.unsealed()

    def test_missing_input_after_publication_worker_prevents_sealing(self):
        self.f.prepare()
        def route(*args,**kwargs):
            result=self.f.routed(*args,**kwargs)
            if self.f.calls[-1][0]=='publication':(self.f.output/'native-console.log').unlink()
            return result
        with self.assertRaisesRegex(ValueError,'Audit inputs changed'):self.launch(None,route)
        self.unsealed()

    def test_mutation_during_result_copy_is_rejected(self):
        self.f.prepare();copy=owner.copy_file
        def copying(source,destination):
            result=copy(source,destination)
            if Path(destination).name=='result.json':(self.f.output/'native-console.log').write_bytes(b'changed during copy')
            return result
        with patch.object(owner,'copy_file',copying),self.assertRaisesRegex(ValueError,'Audit inputs changed'):self.launch(None)
        self.unsealed()

    def test_final_inventory_error_prevents_completion_and_closeout(self):
        self.f.prepare();failure=OSError('literal final inventory failure')
        with patch.object(owner,'publication_inventory',side_effect=failure),self.assertRaises(OSError) as caught:self.launch(None)
        self.assertIs(caught.exception,failure);self.unsealed()

    def test_audit_failure_never_reaches_final_inventory(self):
        self.f.prepare();self.f.failed_phase='independentAudit'
        with patch.object(owner,'publication_inventory') as scan,self.assertRaisesRegex(ValueError,'Independent audit failed'):self.launch(None)
        scan.assert_not_called();self.unsealed()

    def test_publication_failure_never_reaches_final_inventory(self):
        self.f.prepare();self.f.failed_phase='publication'
        with patch.object(owner,'publication_inventory') as scan,self.assertRaisesRegex(ValueError,'Final publication barrier failed'):self.launch(None)
        scan.assert_not_called();self.unsealed()


if __name__=='__main__':unittest.main()
