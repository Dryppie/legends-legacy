"""Request relocation keeps qualified bytes and all scientific bindings intact."""
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('qualified_admission',Path(__file__).with_name('admit-qualified-proposal-resource.py'))
r = importlib.util.module_from_spec(spec); spec.loader.exec_module(r)


class RebindingTests(unittest.TestCase):
    def test_relocation_changes_only_bound_source_paths_and_content_root(self):
        with tempfile.TemporaryDirectory(prefix='qualified-proposal-') as temp:
            package = Path(temp); q = dict(version=r.a.owner.VERSION,resourceEnvelope=r.a.owner.RESOURCE_V2,
                outputRoot='frozen-output',registryRoot='frozen-registry',requiredHistory={'pinned-history':'hash'},
                pendingHistoryRecoveries={},recoveryReceiptHashes={},contentRoot='old/content')
            for name in ('plan','context','settings','history','runtime','auditor'):
                path = package/('auditor.py' if name == 'auditor' else name+'.json'); path.write_bytes(name.encode())
                q[name] = dict(path='old/'+path.name,sha256=r.a.sha(path))
            result = r.rebind(q,package)
            for key in ('version','resourceEnvelope','outputRoot','registryRoot','requiredHistory','pendingHistoryRecoveries','recoveryReceiptHashes'):
                self.assertEqual(q[key],result[key])
            self.assertEqual('old/content',q['contentRoot'])
            self.assertEqual(str(package/'content'),result['contentRoot'])
            for name in ('plan','context','settings','history','runtime','auditor'):
                self.assertEqual(q[name]['sha256'],result[name]['sha256']); self.assertNotEqual(q[name]['path'],result[name]['path'])
            (package/'context.json').write_bytes(b'changed')
            with self.assertRaisesRegex(ValueError,'Changed qualified'): r.rebind(q,package)


if __name__ == '__main__': unittest.main(verbosity=2)
