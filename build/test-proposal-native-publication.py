"""Independent native publication verification; literal receipts and failed commands."""
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as w
import bounded_windows_process as owned


def sha(path):
    with Path(path).open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()


class NativePublication(unittest.TestCase):
    def setUp(self):
        for key in ('LL_NATIVE_PUBLICATION_EXCHANGE','LL_NATIVE_PUBLICATION_EXCHANGE_PIN','LL_NATIVE_PUBLICATION_DLL'):
            if key not in os.environ:self.skipTest('Requires isolated native publication verification')
        self.exchange=Path(os.environ['LL_NATIVE_PUBLICATION_EXCHANGE'])
        self.dll=Path(os.environ['LL_NATIVE_PUBLICATION_DLL']).resolve()
        self.assertEqual(sha(self.exchange/'files.json'),os.environ['LL_NATIVE_PUBLICATION_EXCHANGE_PIN'])
        self.manifest(self.exchange)

    def manifest(self,root):
        manifest=w.strict((root/'files.json').read_bytes())
        self.assertEqual(set(manifest),{p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and p!=root/'files.json'})
        for name,pin in manifest.items():self.assertEqual(sha(root/name),pin)

    def case(self,name):
        root=self.exchange/name;self.manifest(root)
        binding=w.strict((root/'binding.json').read_bytes());meta=w.strict((root/'fixture.json').read_bytes())
        self.assertTrue(meta['fixtureOnly']);self.assertEqual(meta['actualCombat'],0);self.assertEqual(meta['productionEntropyDraws'],0)
        self.assertFalse(meta['wholeProcessCoverage']);self.assertFalse(meta['usableForAdmission'])
        self.assertEqual(sha(meta['producerPath']),meta['producerSha256']);self.assertEqual(sha(self.dll),meta['producerSha256'])
        self.assertEqual(binding['producerSha256'],meta['producerSha256'])
        self.assertEqual(binding['accountingModuleSha256'],meta['producerSha256'])
        self.assertEqual(binding['requestSha256'],sha(root/'request.json'))
        self.assertEqual(binding['version'],w.WORKER_PUBLICATION_BINDING_VERSION)
        observed=w.worker_publication((root/'publication.json').read_bytes(),sha(root/'binding.json'),binding['phase'],
                                     binding['requestSha256'],binding['producerSha256'])
        return root,binding,observed,meta

    def receipt(self,root,binding):
        return w.verify_counter_receipt(root/'native-work.json',sha(root/'native-work.json'),binding['phase'],
                                        binding['requestSha256'],binding['producerSha256'])

    def test_real_native_success_observations_match_receipts_in_all_native_phases(self):
        for phase in ('native','nativeAudit','publication'):
            with self.subTest(phase=phase):
                root,binding,value,meta=self.case('success-'+phase)
                self.assertTrue(meta['realWorkerBoundary']);self.assertFalse(meta['injectedFailure'])
                receipt=self.receipt(root,binding);self.assertEqual(receipt['outcome'],'Complete')
                self.assertEqual(value['outcome'],'Complete')
                self.assertEqual(value['serializedReceiptSha256'],sha(root/'native-work.json'))
                self.assertEqual(value['serializedReceiptBytes'],(root/'native-work.json').stat().st_size)
                self.assertEqual(value['counters']['acceptedWriteBytes'],value['serializedReceiptBytes'])
                self.assertEqual(value['counters']['syncCompleted'],1);self.assertEqual(value['counters']['closeCompleted'],1)
                self.assertNotIn('acceptedWriteBytes',receipt['counters']);self.assertTrue(value['observationPersistenceExcluded'])

    def test_failed_or_cancelled_body_is_separate_from_complete_publication(self):
        for name in ('worker-failed','worker-cancelled'):
            with self.subTest(name=name):
                root,binding,value,meta=self.case(name)
                self.assertTrue(meta['realWorkerBoundary']);self.assertEqual(value['outcome'],'Complete')
                self.assertEqual(self.receipt(root,binding)['outcome'],'Failed')
                self.assertEqual(value['serializedReceiptSha256'],sha(root/'native-work.json'))

    def test_real_reservation_failure_preserves_existing_bytes_and_unknown_serialization(self):
        root,_,value,meta=self.case('open-failed')
        self.assertTrue(meta['realWorkerBoundary']);self.assertEqual((root/'native-work.json').read_bytes(),b'previous')
        self.assertEqual(value['outcome'],'Failed');self.assertIsNone(value['serializedReceiptBytes'])
        self.assertIsNone(value['serializedReceiptSha256']);self.assertEqual(value['counters'],dict(openAttempted=1,openFailed=1))

    def test_injected_native_failures_match_cross_language_progress_rules(self):
        for fault in ('serialize','write','flush','sync','close','write-close'):
            with self.subTest(fault=fault):
                root,binding,value,meta=self.case(fault+'-failed');counts=value['counters']
                self.assertFalse(meta['realWorkerBoundary']);self.assertTrue(meta['injectedFailure'])
                self.assertEqual(value['outcome'],'Failed');self.assertEqual(counts['closeAttempted'],1)
                if fault=='serialize':
                    self.assertEqual((root/'native-work.json').read_bytes(),b'');self.assertIsNone(value['serializedReceiptBytes'])
                elif fault.startswith('write'):
                    self.assertEqual((root/'native-work.json').stat().st_size,1)
                    self.assertEqual(counts['failedWriteBytesUnknown'],1);self.assertNotIn('acceptedWriteBytes',counts)
                else:
                    self.assertEqual(self.receipt(root,binding)['outcome'],'Complete')
                    self.assertEqual(value['serializedReceiptSha256'],sha(root/'native-work.json'))
                    self.assertEqual(counts['acceptedWriteBytes'],(root/'native-work.json').stat().st_size)

    def test_real_commands_reject_literal_input_without_preparation(self):
        for phase in ('native','nativeAudit','publication'):
            with self.subTest(phase=phase):
                root,binding,value,meta=self.case('command-failed-'+phase)
                self.assertTrue(meta['realWorkerBoundary']);self.assertEqual(value['outcome'],'Complete')
                self.assertEqual(self.receipt(root,binding)['outcome'],'Failed')

    def test_native_observation_cannot_be_rebound_or_claim_its_own_persistence(self):
        root,binding,value,_=self.case('success-nativeAudit')
        for change in ({'bindingSha256':'a'*64},{'requestSha256':'a'*64},{'producerSha256':'a'*64},
                       {'observationPersistenceExcluded':False},{'wholeProcessCoverage':True},{'usableForAdmission':True}):
            with self.subTest(change=change),self.assertRaises(ValueError):
                w.worker_publication(w.canonical(dict(value,**change)),sha(root/'binding.json'),binding['phase'],
                                     binding['requestSha256'],binding['producerSha256'])

    def test_real_owned_native_failures_retain_v2_observations_and_kernel_totals(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup)
        root=Path(os.environ.get('LL_NATIVE_PUBLICATION_OWNER_EXPORT',temp.name));root.mkdir(exist_ok=True)
        processes=[]
        for phase,command in [('native','tower-proposal-study-run'),('nativeAudit','tower-proposal-study-audit'),
                              ('publication','tower-proposal-study-publication-check')]:
            case=root/phase;case.mkdir();study=case/'study';study.mkdir()
            request=study/'request.json';request.write_bytes(b'{"fixtureOnly":true}')
            owner=w.RetainedOwner(case/'owner',sha(request),sha(__file__),sha(w.__file__),work=w.OwnerFileCounters())
            with self.subTest(phase=phase),self.assertRaisesRegex(ValueError,'failed receipt'):
                with owner:
                    for earlier in w.PHASES[:w.PHASES.index(phase)]:
                        with owner.phase(earlier,sha(self.dll)):pass
                    with owner.phase(phase,sha(self.dll)):
                        def invoke(binding,pin):
                            result=owned.run(['dotnet',str(self.dll),command,str(study),'--work-binding',str(binding),pin],
                                self.dll.parent,case/'worker.log',time.monotonic()+10,
                                observe=lambda value:owner.observe_process(value,sha(owned.__file__)))
                            self.assertNotEqual(result['exitCode'],0);self.assertEqual(result['activeProcesses'],0)
                            self.assertFalse(result['timedOut']);processes.append(result)
                        owner.run_worker(study,self.dll,self.dll,case/'worker.json',invoke,publication_path=case/'publication.json')
            self.manifest(case/'owner');self.assertEqual(len(list(study.iterdir())),1)
            self.assertEqual(owner.receipt['ledger']['outcome'],'Failed');self.assertEqual(owner.final_observation['outcome'],'Failed')
            value=w.strict((case/'owner'/(phase+'-receipt-publication.json')).read_bytes())
            self.assertEqual(value['outcome'],'Complete');self.assertEqual(value['serializedReceiptSha256'],sha(case/'worker.json'))
            observed=owner.receipt['ledger']['processObservations'][0]['observation']
            self.assertTrue(observed['jobDrained']);self.assertEqual(observed['jobIo']['coverage'],'KernelJobLifetimeTotals')
            w.seal_new(case/'final-owner-observation.json',owner.final_observation)
        if 'LL_NATIVE_PUBLICATION_OWNER_EXPORT' in os.environ:
            w.seal_new(root/'fixture.json',dict(fixtureOnly=True,realNativeCommandFailures=True,actualCombat=0,productionEntropyDraws=0,
                wholeProcessCoverage=False,usableForAdmission=False,processes=processes,producerPath=str(self.dll),producerSha256=sha(self.dll),
                sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p) for p in (w.__file__,owned.__file__,__file__)}))
            w.seal_new(root/'files.json',{p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})


if __name__=='__main__':unittest.main()
