"""Complete synthetic study storage equivalence and adversarial integration checks."""
import argparse
import copy
import importlib.util
import json
from pathlib import Path
import sys
import unittest

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('study_tests',ROOT/'build/test-proposal-affinity-study.py')
tests=importlib.util.module_from_spec(spec); spec.loader.exec_module(tests)
audit=tests.audit


class StorageTests(unittest.TestCase):
    fixtures=None

    @classmethod
    def setUpClass(cls):
        if cls.fixtures is None:
            raise ValueError('Supply newly generated plain/compressed synthetic fixtures')
        cls.plain,cls.compressed=cls.fixtures/'plain',cls.fixtures/'compressed'
        for root in (cls.plain,cls.compressed):
            if not (root/'fixture-plan.json').exists(): raise ValueError('Require a synthetic exported fixture')
            tests.prepare(root)
        cls.plain_result=audit.audit(cls.plain,True)
        cls.compressed_result=audit.audit(cls.compressed,True)

    def reader(self):
        return audit.storage_reader(self.compressed,audit.read(self.compressed/'request.json'),audit.read(self.compressed/'study/freeze.json'))

    def changed_index(self,change,extra=None):
        directory=self.compressed/'search/root-01/candidate/racing'
        path=directory/'evidence-storage.json'; manifest=directory.parent/'files.json'
        originals={p:p.read_bytes() for p in (path,manifest)}
        try:
            value=audit.read(path); change(value); tests.write(path,value)
            if extra: (directory/extra).write_text('{}',encoding='utf-8')
            tests.seal(directory.parent)
            with self.assertRaises((ValueError,FileNotFoundError)): self.reader()
        finally:
            if extra: (directory/extra).unlink()
            for p,data in originals.items(): p.write_bytes(data)

    def test_complete_independent_reconstruction_has_identical_results(self):
        self.assertEqual(self.plain_result['result'],self.compressed_result['result'])
        result=self.compressed_result['result']
        self.assertEqual((18816,6144,6,6,'Inconclusive'),(result['fights'],result['heldoutFights'],result['differingRoots'],result['novelRoots'],result['decision']))
        for field in ('validation','controlValidation'):
            self.assertEqual((6,6),(result[field]['passedRoots'],result[field]['fallbackRoots']))

    def test_every_encoded_logical_digest_equals_the_plain_writer_bytes(self):
        descriptor=audit.read(self.compressed/'evidence-storage.json'); count=0
        for index_path in descriptor['indexes']:
            relative=Path(index_path).parent
            index=audit.read(self.compressed/index_path)
            for entry in index['entries']:
                plain=self.plain/relative/entry['logicalPath']; count+=1
                self.assertEqual((plain.stat().st_size,audit.sha(plain)),(entry['logicalBytes'],entry['logicalSha256']))
                self.assertFalse((self.compressed/relative/entry['logicalPath']).exists())
        self.assertEqual(60,count)

    def test_catalogues_barrier_and_attempt_journals_remain_exact(self):
        names=['attempts.jsonl','study/freeze.json','study/summary.json']+[f'study/placement-catalogue-{n:02d}.json' for n in range(1,13)]
        for name in names:
            self.assertEqual(audit.sha(self.plain/name),audit.sha(self.compressed/name),name)
        work=audit.read(self.compressed/'fixture-storage-audit-work.json')
        self.assertEqual(120,work['decodePasses'])
        self.assertGreater(work['decodedBytesProcessed'],0)

    def test_missing_index_entry_rejected_after_resealing(self):
        self.changed_index(lambda index:index['entries'].clear())

    def test_repeated_index_entry_rejected_after_resealing(self):
        self.changed_index(lambda index:index['entries'].append(copy.deepcopy(index['entries'][0])))

    def test_changed_codec_and_mapping_rejected_after_resealing(self):
        for key,value in (('codec','plain'),('physicalPath','../search.json.gz'),('logicalPath','plan.json')):
            with self.subTest(key=key): self.changed_index(lambda index:index['entries'][0].update({key:value}))

    def test_physical_and_logical_bounds_rejected_after_resealing(self):
        for key in ('physicalBytes','logicalBytes'):
            with self.subTest(key=key): self.changed_index(lambda index:index['entries'][0].update({key:1<<63}))

    def test_plain_compressed_collision_rejected_after_resealing(self):
        self.changed_index(lambda _:None,'search.json')

    def test_changed_index_version_rejected_after_resealing(self):
        self.changed_index(lambda index:index.update(version='future'))

    def test_root_request_downgrade_and_budget_changes_rejected(self):
        q=audit.read(self.compressed/'request.json'); freeze=audit.read(self.compressed/'study/freeze.json')
        with self.assertRaises(ValueError): audit.storage_reader(self.compressed,{**q,'evidenceStorage':None},freeze)
        for key,value in (('maximumLogicalBytes',1),('maximumMembers',1),('version','future')):
            changed=copy.deepcopy(q); changed['evidenceStorage'][key]=value
            with self.subTest(key=key),self.assertRaises(ValueError): audit.storage_reader(self.compressed,changed,freeze)

    def test_changed_module_is_rejected_before_import(self):
        path=self.compressed/'proposal_evidence_codec.py'; original=path.read_bytes()
        try:
            path.write_text('raise RuntimeError("must not execute")',encoding='utf-8')
            with self.assertRaisesRegex(ValueError,'reader module'): self.reader()
        finally: path.write_bytes(original)

    def test_full_auditor_rejects_resealed_catalogue_tampering(self):
        path=self.compressed/'study/placement-catalogue-01.json'; manifest=path.parent/'files.json'
        originals={p:p.read_bytes() for p in (path,manifest)}
        try:
            value=audit.read(path); value['recipes'].pop(); tests.write(path,value); tests.seal(path.parent)
            with self.assertRaises(ValueError): audit.audit(self.compressed,True)
        finally:
            for p,data in originals.items(): p.write_bytes(data)

    def test_legacy_request_does_not_accept_encoded_search_files(self):
        context=audit.read(self.compressed/'source/context.json'); design=audit.read(self.compressed/'source/plan.json')
        values=audit.read(self.compressed/'allocation.json')['selected']
        with self.assertRaises(FileNotFoundError):
            audit.search(self.compressed/'search/root-01/control',context,design,values,1,'control')

    def test_owner_remains_closed_before_any_launch_or_reservation(self):
        with self.assertRaisesRegex(ValueError,'recovery gate is closed'):
            tests.owner.validate_request(audit.read(self.compressed/'request.json'))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--fixtures',type=Path,required=True)
    args,remaining=parser.parse_known_args(); StorageTests.fixtures=args.fixtures.resolve()
    unittest.main(argv=[sys.argv[0],*remaining])
