"""Deterministic live-storage rename races, with no combat or seed allocation."""
import importlib.util
from pathlib import Path
import stat
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('launcher', Path(__file__).with_name('run-reference-exploration-comparison.py'))
launcher = importlib.util.module_from_spec(spec); spec.loader.exec_module(launcher)


class StorageTests(unittest.TestCase):
    def test_completed_atomic_rename_is_counted_even_if_final_name_was_not_enumerated(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); pending = root/'plan.json.pending'; final = root/'plan.json'
            pending.write_bytes(b'1234567')
            def inventory(_):
                paths = [pending]; pending.rename(final); return paths
            with patch.object(launcher, 'inventory', side_effect=inventory):
                self.assertEqual(7, launcher.storage_bytes(root))

    def test_pending_and_old_published_file_both_count_while_replacement_is_in_progress(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); (root/'plan.json').write_bytes(b'old'); (root/'plan.json.pending').write_bytes(b'new-value')
            self.assertEqual(12, launcher.storage_bytes(root))

    def test_replacement_race_deduplicates_the_published_path(self):
        for pending_first in (True, False):
            with self.subTest(pending_first=pending_first), tempfile.TemporaryDirectory() as temp:
                root = Path(temp); pending = root/'plan.json.pending'; final = root/'plan.json'
                final.write_bytes(b'old'); pending.write_bytes(b'new-value')
                def inventory(_):
                    pending.replace(final)
                    return [pending, final] if pending_first else [final, pending]
                with patch.object(launcher, 'inventory', side_effect=inventory):
                    self.assertEqual(9, launcher.storage_bytes(root))

    def test_missing_published_destination_is_not_silently_skipped(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            with patch.object(launcher, 'inventory', return_value=[root/'gone.pending']), self.assertRaises(FileNotFoundError):
                launcher.storage_bytes(root)

    def test_replacement_seen_twice_keeps_the_larger_size(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); final = root/'plan.json'; pending = root/'plan.json.pending'
            def regular(size): return SimpleNamespace(st_mode=stat.S_IFREG, st_file_attributes=0, st_size=size)
            with patch.object(launcher, 'inventory', return_value=[final, pending]), \
                 patch.object(Path, 'lstat', side_effect=[regular(3), FileNotFoundError(), regular(9)]):
                self.assertEqual(9, launcher.storage_bytes(root))

    def test_missing_ordinary_file_is_not_silently_skipped(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            with patch.object(launcher, 'inventory', return_value=[root/'gone.json']), self.assertRaises(FileNotFoundError):
                launcher.storage_bytes(root)

    def test_permission_failure_is_not_treated_as_a_rename(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            with patch.object(launcher, 'inventory', return_value=[root/'plan.json.pending']), \
                 patch.object(Path, 'lstat', side_effect=PermissionError('denied')), self.assertRaises(PermissionError):
                launcher.storage_bytes(root)

    def test_renamed_destination_cannot_be_a_link_reparse_point_or_directory(self):
        for mode, attributes in ((stat.S_IFLNK, 0), (stat.S_IFREG, 0x400), (stat.S_IFDIR, 0)):
            with self.subTest(mode=mode, attributes=attributes), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                with patch.object(launcher, 'inventory', return_value=[root/'plan.json.pending']), \
                     patch.object(Path, 'lstat', side_effect=[FileNotFoundError(), SimpleNamespace(st_mode=mode, st_file_attributes=attributes, st_size=9)]), \
                     self.assertRaises(ValueError):
                    launcher.storage_bytes(root)

    def test_nested_files_and_nonstandard_pending_names_are_counted(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); (root/'nested').mkdir()
            (root/'nested/file.json').write_bytes(b'123'); (root/'unpublished.pending').write_bytes(b'4567')
            self.assertEqual(7, launcher.storage_bytes(root))


if __name__ == '__main__': unittest.main(verbosity=2)
