"""Owner/resource rejection tests and real isolated Windows Job fixtures; no native probe run."""
import argparse
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import time
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('probe_owner',Path(__file__).with_name('run-proposal-audit-cost-probe.py'))
p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p)
HOST=None


class ProbeTests(unittest.TestCase):
    def test_fixed_estimate_preserves_margin_publication_reserve_and_separate_cap(self):
        value=p.estimate(400,[50,20],20,100,100)
        self.assertEqual(value['auditPlanningSeconds'],1140);self.assertEqual(value['status'],'SupportsAnotherAdmissionReview')
        self.assertFalse(value['admitted']);self.assertFalse(value['guaranteedUpperBound'])
        self.assertEqual(p.estimate(430,[50,20],20,100,100)['status'],'AuditPartitionStillNotSupported')
        self.assertGreater(p.estimate(400,[50,20],20,100,500)['auditPlanningSeconds'],1200)

    def test_nonfinite_or_missing_costs_fail(self):
        for worker,audits,inv,source,target in [(float('nan'),[1,2],1,1,1),(1,[1],1,1,1),(1,[1,float('inf')],1,1,1),
                                               (1,[1,2],-1,1,1),(1,[1,2],1,0,1)]:
            with self.assertRaises(ValueError):p.estimate(worker,audits,inv,source,target)

    def test_partial_or_promoted_workload_is_not_complete(self):
        value=dict(status='NativeAuditCostWorkloadComplete',resourceOnly=True,inputReconstructions=27648,proposalArms=24,literalRows=12672,
                   historyValues=672220,fights=0,newValues=0,admission='NotPerformed',phases=[dict(name=n,seconds=1) for n in
                   ['qualification','historical-inventory','historical-native-reconstruction','full-history-workloads','full-history-proposal-replay']])
        p.validate_observation(value)
        for key,change in [('inputReconstructions',21888),('proposalArms',23),('fights',1),('newValues',1),('admission','Admitted')]:
            with self.subTest(key=key),self.assertRaises(ValueError):p.validate_observation(dict(value,**{key:change}))
        bad=copy.deepcopy(value);bad['phases'].reverse()
        with self.assertRaises(ValueError):p.validate_observation(bad)

    def test_owner_refuses_existing_or_different_output_without_writes(self):
        with tempfile.TemporaryDirectory(prefix='proposal-audit-cost-test-') as d:
            output=Path(d)/'result'
            with patch.object(p,'OUTPUT',output):
                p.ensure_new_output(output)
                with self.assertRaises(ValueError):p.ensure_new_output(Path(d)/'other')
                output.mkdir()
                with self.assertRaises(ValueError):p.ensure_new_output(output)
                self.assertEqual(list(output.iterdir()),[])

    def exercise(self,mode,check=None):
        with tempfile.TemporaryDirectory(prefix='proposal-audit-cost-test-') as d:
            output=Path(d)/'result';output.mkdir();p.save(output/'request.json',p.request(output,HOST,mode))
            def limits():
                if check:check(output)
            result=p.owner.run(['dotnet',str(HOST),'proposal-audit-cost-probe',str(output/'request.json')],str(p.ROOT),str(output/'process.log'),
                               time.monotonic()+(2 if mode=='hang' else 10),cleanup_seconds=1,check=limits)
            self.assertEqual(result['activeProcesses'],0)
            self.assertEqual(result['mechanism'],'suspended-owned-job-v1')
            return result,dict((f.name,f.read_text()) for f in output.iterdir() if f.suffix=='.json'),(output/'process.log').read_text()

    def test_literal_owned_process_completes(self):
        result,files,log=self.exercise('literal');self.assertEqual(result['exitCode'],0,log)
        self.assertEqual(json.loads(files['worker-observation.json'])['status'],'LiteralProbeComplete')

    def test_hanging_worker_is_killed_before_any_completion(self):
        result,files,log=self.exercise('hang');self.assertTrue(result['timedOut'],log)
        self.assertIn('worker-start.json',files);self.assertNotIn('worker-observation.json',files)

    def test_combat_guard_fails_owned_worker(self):
        result,files,log=self.exercise('combat');self.assertNotEqual(result['exitCode'],0,log)
        self.assertIn('forbids combat',files['worker-failure.json']);self.assertNotIn('worker-observation.json',files)

    def test_storage_failure_kills_worker_and_retains_partial_evidence(self):
        checked=[]
        def check(output):
            if (output/'overflow.bin').exists() and (output/'overflow.bin').stat().st_size>p.BYTES:
                checked.append(True);raise ValueError('fixture storage allowance exceeded')
        with self.assertRaisesRegex(ValueError,'storage allowance'):self.exercise('storage',check)
        self.assertTrue(checked)


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--host',type=Path,required=True)
    args,remaining=parser.parse_known_args();HOST=args.host.resolve()
    unittest.main(argv=[__file__,*remaining],verbosity=2)
