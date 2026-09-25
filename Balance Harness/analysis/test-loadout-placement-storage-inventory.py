"""Inventory integrity, byte reconciliation and within-archive deduplication tests."""
import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec=importlib.util.spec_from_file_location('inventory',Path(__file__).with_name('loadout-placement-storage-inventory.py'))
i=importlib.util.module_from_spec(spec); spec.loader.exec_module(i)


def row(path,size,pin='a'*64): return dict(path=path,bytes=size,sha256=pin,category=i.category(path))


class InventoryTests(unittest.TestCase):
    def test_categories_distinguish_reports_recipes_catalogues_and_journals(self):
        expected={
            'heldout/battles/trial-000001.json.gz':'battle-reports',
            'search/root-01/control/recipes/a.json':'recipes',
            'study/heldout-01-a.json':'heldout-observations',
            'study/pair-01.json':'paired-search-results',
            'study/placement-catalogue-01.json':'placement-catalogues',
            'search/root-01/control/racing/search.json':'search-results',
            'search/root-01/control/racing/plan.json':'search-plans',
            'search/root-01/control/racing/batch-01.json':'proposal-batches',
            'search/root-01/control/racing/panel-01.json':'frozen-search-panels',
            'search/root-01/control/racing/validation-freeze.json':'validation',
            'attempts.jsonl':'attempt-journals','heldout/trials.jsonl':'trial-bindings',
            'study/files.json':'manifests','source/context.json':'source-bindings',
            'executable/BalanceHarness.dll':'runtime','heldout/content/Data/a.json':'captured-content'}
        for path,value in expected.items():
            with self.subTest(path=path): self.assertEqual(value,i.category(path))

    def test_category_totals_reconcile_exactly(self):
        value=i.summarize([row('a.json',3),row('study/pair-01.json',7,'b'*64)])
        self.assertEqual(10,value['retainedBytes'])
        self.assertEqual(value['retainedBytes'],sum(g['bytes'] for g in value['categories'].values()))
        self.assertEqual(2,sum(g['files'] for g in value['categories'].values()))

    def test_exact_duplicates_keep_one_copy_and_all_logical_paths(self):
        value=i.summarize([row('a.json',7),row('b.json',7),row('c.json',7)])
        self.assertEqual(14,value['exactDuplicateBytes']); self.assertEqual(7,value['uniqueWholeFileBytes'])
        self.assertEqual(['a.json','b.json','c.json'],value['duplicateGroups'][0]['paths'])

    def test_cross_archive_overlap_is_not_counted_as_within_archive_savings(self):
        value=i.comparison([row('a.json',7)],[row('b.json',7)])
        self.assertEqual(0,value['baseline']['exactDuplicateBytes'])
        self.assertEqual(0,value['placement']['exactDuplicateBytes'])
        self.assertEqual(7,value['crossArchiveUniqueSharedBytes'])
        self.assertFalse(value['crossArchiveSavingsUsableForAdmission'])

    def test_different_bytes_are_not_deduplicated_by_size(self):
        self.assertEqual(0,i.summarize([row('a',7),row('b',7,'b'*64)])['exactDuplicateBytes'])

    def test_zero_byte_duplicates_do_not_create_savings(self):
        self.assertEqual(0,i.summarize([row('a',0),row('b',0)])['exactDuplicateBytes'])

    def test_same_digest_with_different_length_is_rejected(self):
        with self.assertRaises(ValueError): i.summarize([row('a',7),row('b',8)])

    def test_duplicate_paths_are_rejected(self):
        with self.assertRaises(ValueError): i.summarize([row('a',7),row('a',7)])

    def test_changed_category_is_rejected(self):
        value=row('a',7); value['category']='runtime'
        with self.assertRaises(ValueError): i.summarize([value])

    def test_invalid_lengths_are_rejected(self):
        for size in (-1,True,1.5):
            with self.subTest(size=size), self.assertRaises(ValueError): i.summarize([row('a',size)])

    def test_unsafe_members_are_rejected(self):
        for name in ('','../escape','/absolute','a/../b','a//b','a/./b','C:/file','a\\b'):
            with self.subTest(name=name), self.assertRaises(ValueError): i.safe_name(name)

    def test_prefix_selection_excludes_sibling_packages(self):
        spec=dict(prefix='result/',extras={})
        self.assertEqual({'a':'a'*64},i.members({'result/a':'a'*64,'result-other/b':'b'*64,'package/c':'c'*64},spec))

    def test_external_members_are_unique_and_pinned(self):
        with self.assertRaises(ValueError): i.members({'files.json':'a'*64},dict(prefix='',extras={'files.json':'b'*64}))
        with self.assertRaises(ValueError): i.members({'a':'bad'},dict(prefix='',extras={}))

    def test_streamed_inventory_authenticates_bytes_and_counts(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); (root/'a.json').write_bytes(b'{"a":1}\n'); (root/'b.json').write_bytes(b'{"a":1}\n')
            pins={p.name:i.sha(p) for p in root.iterdir()}; value=i.inventory(root,pins,lambda:None)
            self.assertEqual(16,i.summarize(value)['retainedBytes'])
            self.assertEqual(8,i.summarize(value)['exactDuplicateBytes'])

    def test_changed_payload_fails_authentication(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); (root/'a').write_bytes(b'changed')
            with self.assertRaisesRegex(ValueError,'source bytes'): i.inventory(root,{'a':hashlib.sha256(b'original').hexdigest()},lambda:None)

    def test_unlisted_file_fails_before_traversal(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); (root/'extra').write_bytes(b'')
            with self.assertRaisesRegex(ValueError,'membership'): i.inventory(root,{},lambda:None)

    def test_missing_file_fails_before_traversal(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaisesRegex(ValueError,'membership'): i.inventory(Path(folder),{'missing':'a'*64},lambda:None)

    def test_gate_and_no_execution_flags_cannot_be_misreported_as_savings(self):
        value=i.comparison([row('a',1)],[row('b',1)])
        self.assertTrue(value['recoveryGateStillClosed']); self.assertIsNone(value['qualifiedCurrentForecast'])
        for name in ('admitted','runtimeQualified','proposedSavingsAppliedToForecast','nativePreparation'):
            self.assertFalse(value[name])
        for name in ('newTimingSamples','actualCombat','productionEntropyDraws','newScientificReservations','archiveRewrites','compressionTrials'):
            self.assertEqual(0,value[name])

    def test_alternate_output_cannot_replace_inventory(self):
        with tempfile.TemporaryDirectory() as folder:
            path=Path(folder)/'other'
            with self.assertRaisesRegex(ValueError,'single new inventory path'): i.create(path)
            self.assertFalse(path.exists())

    def test_duplicate_json_keys_are_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            path=Path(folder)/'bad.json'; path.write_text('{"a":1,"a":2}',encoding='utf-8')
            with self.assertRaisesRegex(ValueError,'Duplicate JSON key'): i.read(path)


if __name__=='__main__': unittest.main()
