"""Complete fresh literal study reconstructions with opt-in work counters."""
import argparse
import importlib.util
import json
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name,path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


t = module('study_work_fixtures', ROOT/'build/test-proposal-affinity-study.py')
w = module('study_work_counters', ROOT/'Balance Harness/analysis/proposal_work_accounting.py')


class CompleteWork(unittest.TestCase):
    fixtures = output = None

    @classmethod
    def setUpClass(cls):
        cls.output.mkdir()
        for name in ('plain','compressed'):
            root = cls.fixtures/name
            if not (root/'fixture-native-work.json').is_file(): raise ValueError('Require fresh instrumented literal study')
            t.prepare(root)

    def test_native_reconstructions_preserve_complete_counts_and_partial_coverage(self):
        for name in ('plain','compressed'):
            root = self.fixtures/name
            receipt = w.verify_counter_receipt(root/'fixture-native-work.json',t.audit.sha(root/'fixture-native-work.json'),
                'nativeAudit','a'*64,'b'*64)
            counts = receipt['counters']
            self.assertEqual([counts[k] for k in ('reconstructedRoots','reconstructedTrajectories',
                'reconstructedTrialBindings','reconstructedCatalogues','reconstructedHeldoutMembers','reconstructedStudyEndpoints')],
                [12,24,12672,12,24,1])
            # Contract manifests and repeated raw journal reads now participate.
            for role in ('manifest','journal','json'):
                self.assertGreater(counts['applicationReadBytes.'+role],0)
            if name=='compressed':
                self.assertEqual(counts['decodePassesCompleted'],120)
                descriptor = t.audit.read(root/'evidence-storage.json')
                entries = [entry for index in descriptor['indexes'] for entry in t.audit.read(root/index)['entries']]
                self.assertEqual(len(entries),60)
                self.assertEqual(counts['decodedBytesProcessed'],2*sum(e['logicalBytes'] for e in entries))
                self.assertGreater(counts['applicationReadBytes.metadata'],0)
            self.assertFalse(receipt['wholeProcessCoverage'])

    def test_independent_complete_results_and_counters(self):
        results = {}
        for name in ('plain','compressed'):
            root = self.fixtures/name; work = w.Counters()
            results[name] = t.audit.audit(root,working=True,work=work)['result']
            counts = work.values
            self.assertEqual([counts[k] for k in ('reconstructedRoots','reconstructedTrajectories',
                'reconstructedTrialBindings','reconstructedCatalogues','reconstructedHeldoutMembers','reconstructedStudyEndpoints')],
                [12,24,18816,12,24,1])
            # Battle reports add one decode each; the explicit evidence adds two each.
            self.assertEqual(counts['decodePassesCompleted'],18816+(120 if name=='compressed' else 0))
            for role in ('manifest','journal','json','payload'):
                self.assertGreater(counts['applicationReadBytes.'+role],0)
            receipt = work.receipt('independentAudit',t.audit.sha(root/'request.json'),
                                  t.audit.sha(ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py'),True)
            w.seal_new(self.output/(name+'-work.json'),receipt)
        self.assertEqual(results['plain'],results['compressed'])
        self.assertEqual(results['plain']['fights'],18816)
        self.assertEqual(results['plain']['decision'],'Inconclusive')
        w.seal_new(self.output/'results.json',results)


if __name__=='__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixtures',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    args, remaining = parser.parse_known_args()
    CompleteWork.fixtures, CompleteWork.output = args.fixtures.resolve(), args.output.resolve()
    unittest.main(argv=[sys.argv[0],*remaining])
