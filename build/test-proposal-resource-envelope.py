"""Version binding and cumulative-resource rejection tests; no combat or entropy."""
import datetime as dt
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result); return result


owner = module('resource_owner', ROOT/'build/run-proposal-affinity-study.py')
audit = module('resource_audit', ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py')


class ResourceTests(unittest.TestCase):
    def test_legacy_and_amended_partitions(self):
        for version, expected in ((None, (9600, 1200)), (owner.RESOURCE_V1, (9600, 1200)), (owner.RESOURCE_V2, (9000, 1800))):
            limits = owner.resources(dict(resourceEnvelope=version))
            self.assertEqual(expected, (limits['nativeSeconds'], limits['auditSeconds']))
            self.assertEqual(10800, sum(expected))
        for version in ('', 'v3', 9000, {}, []):
            with self.assertRaises(ValueError): owner.resources(dict(resourceEnvelope=version))

    def test_audits_and_publication_never_receive_fresh_or_borrowed_time(self):
        for version, audit_seconds in ((None, 1200), (owner.RESOURCE_V2, 1800)):
            limits = owner.resources(dict(resourceEnvelope=version))
            self.assertEqual(100+500+audit_seconds-5, owner.audit_deadline(100, 600, limits))
            self.assertEqual(100+10800-5, owner.audit_deadline(100, 100+10700, limits))
            # A late second audit uses this same absolute end, not its start + cap.
            end = owner.audit_deadline(100, 600, limits)
            self.assertEqual(5, end-(end-5))

    def fixture(self, root, version):
        q = dict(version=owner.VERSION)
        if version is not None: q['resourceEnvelope'] = version
        self.put(root/'request.json', q)
        native, seconds = (9000, 1800) if version == owner.RESOURCE_V2 else (9600, 1200)
        start = dt.datetime(2026, 9, 23, tzinfo=dt.timezone.utc)
        launch = dict(version=owner.VERSION, requestFileHash=audit.sha(root/'request.json'), startedAt=start.isoformat(),
            nativeDeadline=(start+dt.timedelta(seconds=native)).isoformat(), deadline=(start+dt.timedelta(seconds=10800)).isoformat(),
            maximumSeconds=10800, nativeMaximumSeconds=native, maximumBytes=6442450944, nativeMaximumBytes=5905580032)
        self.put(root/'launch.json', launch)
        self.put(root/'admission-receipt.json', dict(version=q['version'], resourceEnvelope=version))
        completion = dict(version=q['version'], status='Complete', retries=0, seconds=9500, nativeSeconds=8000, auditSeconds=1500,
            nativeBytes=100, auditBytes=100, observedBytes=200, chargedBytes=200, chargedSeconds=9500)
        final = dict(version=q['version'], measuredSeconds=9600, auditSeconds=1600, retainedBytes=220, auditBytes=120, chargedBytes=220, chargedSeconds=9600)
        self.put(root/'completion.json', completion); self.put(root/'closeout.json', final)
        return q, launch, completion, final

    @staticmethod
    def put(path, value): path.write_text(json.dumps(value), encoding='utf-8')

    def test_resealed_partition_or_version_changes_fail_independent_audit(self):
        with tempfile.TemporaryDirectory(prefix='proposal-envelope-') as temp:
            root = Path(temp); q, launch, _, _ = self.fixture(root, owner.RESOURCE_V2)
            audit.audit_resources(root, q, False)
            for name, value in (('nativeMaximumSeconds', 9600), ('maximumSeconds', 11400), ('nativeMaximumBytes', 6000000000)):
                self.put(root/'launch.json', dict(launch, **{name:value}))
                with self.assertRaises(ValueError): audit.audit_resources(root, q, False)
            self.put(root/'launch.json', launch)
            for version in (None, owner.RESOURCE_V1, 'unknown'):
                with self.assertRaises(ValueError): audit.audit_resources(root, dict(q, resourceEnvelope=version), False)
            self.put(root/'admission-receipt.json', dict(version=q['version'], resourceEnvelope=owner.RESOURCE_V1))
            with self.assertRaises(ValueError): audit.audit_resources(root, q, True)

    def test_amended_final_cannot_exceed_partition_even_if_total_fits(self):
        with tempfile.TemporaryDirectory(prefix='proposal-envelope-') as temp:
            root = Path(temp); q, _, completion, final = self.fixture(root, owner.RESOURCE_V2)
            for changed in (dict(final, auditSeconds=1800, measuredSeconds=9800, chargedSeconds=9800),
                            dict(final, auditSeconds=float('nan')), dict(final, auditBytes=536870912)):
                self.put(root/'closeout.json', changed)
                with self.assertRaises(ValueError): audit.audit_resources(root, q, False)
            self.put(root/'closeout.json', final)
            self.put(root/'completion.json', dict(completion, nativeSeconds=9000, seconds=10500, chargedSeconds=10500))
            with self.assertRaises(ValueError): audit.audit_resources(root, q, False)

    def test_legacy_does_not_inherit_larger_audit_allowance(self):
        with tempfile.TemporaryDirectory(prefix='proposal-envelope-') as temp:
            root = Path(temp); q, _, _, _ = self.fixture(root, None)
            with self.assertRaises(ValueError): audit.audit_resources(root, q, False)
            audit.audit_resources(root, q, True)


if __name__ == '__main__': unittest.main(verbosity=2)
