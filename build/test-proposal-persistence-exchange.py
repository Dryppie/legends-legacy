"""Independent v3 native receipt validation and tiny owned worker processes."""
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_work_accounting as w
import bounded_windows_process as owned


def sha(path):
    import hashlib
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


class Exchange(unittest.TestCase):
    def setUp(self):
        for key in ('LL_PERSISTENCE_EXCHANGE', 'LL_PERSISTENCE_EXCHANGE_PIN', 'LL_PERSISTENCE_DLL'):
            if key not in os.environ: self.skipTest('Requires isolated v3 native verification')
        self.exchange = Path(os.environ['LL_PERSISTENCE_EXCHANGE'])
        self.dll = Path(os.environ['LL_PERSISTENCE_DLL'])
        self.assertEqual(sha(self.exchange/'files.json'), os.environ['LL_PERSISTENCE_EXCHANGE_PIN'])
        self.manifest(self.exchange)
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def manifest(self, root):
        values = w.strict((root/'files.json').read_bytes())
        self.assertEqual(set(values), {p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and p != root/'files.json'})
        for name, pin in values.items(): self.assertEqual(sha(root/name), pin)

    def case(self, name):
        root = self.exchange/name; self.manifest(root)
        binding = w.strict((root/'binding.json').read_bytes())
        meta = w.strict((root/'fixture.json').read_bytes())
        self.assertTrue(meta['fixtureOnly'])
        self.assertEqual(meta['actualCombat'], 0); self.assertEqual(meta['productionEntropyDraws'], 0)
        self.assertEqual(meta['producerSha256'], sha(self.dll))
        self.assertEqual(binding['producerSha256'], sha(self.dll))
        self.assertEqual(binding['requestSha256'], sha(root/'request.json'))
        self.assertEqual(binding['version'], w.WORKER_PERSISTENCE_BINDING_VERSION)
        value = w.worker_publication_persistence((root/'persistence.json').read_bytes(), sha(root/'binding.json'),
            binding['phase'], binding['requestSha256'], binding['producerSha256'])
        self.assertTrue(value['terminalObservationPersistenceExcluded'])
        self.assertFalse(value['wholeProcessCoverage']); self.assertFalse(value['usableForAdmission'])
        return root, binding, value

    def complete(self, name):
        root, binding, value = self.case(name)
        self.assertEqual(value['outcome'], 'Complete')
        self.assertEqual(value['serializedObservationSha256'], sha(root/'publication.json'))
        self.assertEqual(value['serializedObservationBytes'], (root/'publication.json').stat().st_size)
        self.assertEqual(value['counters']['acceptedWriteBytes'], (root/'publication.json').stat().st_size)
        observed = w.worker_publication((root/'publication.json').read_bytes(), sha(root/'binding.json'), binding['phase'],
            binding['requestSha256'], binding['producerSha256'])
        return root, binding, value, observed

    def test_native_success_is_byte_exact_for_all_native_phases(self):
        for phase in ('native', 'nativeAudit', 'publication'):
            with self.subTest(phase=phase):
                root, binding, value, observed = self.complete('v3-success-'+phase)
                self.assertEqual(observed['outcome'], 'Complete')
                receipt = w.verify_counter_receipt(root/'native-work.json', sha(root/'native-work.json'), phase,
                    binding['requestSha256'], binding['producerSha256'])
                self.assertEqual(receipt['outcome'], 'Complete')
                self.assertEqual(observed['serializedReceiptSha256'], sha(root/'native-work.json'))
                for op in ('open','serialize','write','flush','sync','close'):
                    self.assertEqual(value['counters'][op+'Completed'], 1)

    def test_failed_body_cancellation_and_command_keep_complete_persistence_separate(self):
        for name in ('v3-worker-failed', 'v3-worker-cancelled', 'v3-command-failed-native',
                     'v3-command-failed-nativeAudit', 'v3-command-failed-publication'):
            with self.subTest(name=name):
                root, binding, _, observed = self.complete(name)
                self.assertEqual(observed['outcome'], 'Complete')
                receipt = w.verify_counter_receipt(root/'native-work.json', sha(root/'native-work.json'), binding['phase'],
                    binding['requestSha256'], binding['producerSha256'])
                self.assertEqual(receipt['outcome'], 'Failed')

    def test_reservation_failures_retain_the_correct_boundary(self):
        root, _, value = self.case('v3-existing-observation')
        self.assertEqual(value['outcome'], 'Failed')
        self.assertEqual(value['counters'], dict(openAttempted=1, openFailed=1))
        self.assertEqual((root/'publication.json').read_bytes(), b'previous')
        self.assertFalse((root/'native-work.json').exists())
        root, _, _, observed = self.complete('v3-existing-receipt')
        self.assertEqual(observed['outcome'], 'Failed')
        self.assertEqual((root/'native-work.json').read_bytes(), b'previous')

    def test_native_injected_failures_obey_shared_progress_validation(self):
        for fault in ('serialize','write','flush','sync','close','write-close','write-worker','sync-worker'):
            with self.subTest(fault=fault):
                root, _, value = self.case('v3-'+fault+'-failed')
                self.assertEqual(value['outcome'], 'Failed')
                self.assertEqual(value['counters']['closeAttempted'], 1)
                if fault.startswith('write'):
                    self.assertEqual(value['counters']['failedWriteBytesUnknown'], 1)
                    self.assertNotIn('acceptedWriteBytes', value['counters'])
                elif fault == 'serialize':
                    self.assertIsNone(value['serializedObservationBytes'])
                else:
                    self.assertEqual(value['counters']['acceptedWriteBytes'], (root/'publication.json').stat().st_size)

    def test_real_owned_native_failures_retain_all_three_receipts(self):
        export = os.environ.get('LL_PERSISTENCE_NATIVE_OWNER_EXPORT')
        root = Path(export) if export else self.root
        if export: root.mkdir()
        for phase, command in (('native','tower-proposal-study-run'), ('nativeAudit','tower-proposal-study-audit'),
                               ('publication','tower-proposal-study-publication-check')):
            case = root/phase; case.mkdir(); study = case/'study'; study.mkdir()
            (study/'request.json').write_bytes(b'{"literal":true}')
            owner = w.RetainedOwner(case/'owner', sha(study/'request.json'), sha(self.dll), sha(w.__file__), work=w.OwnerFileCounters())
            with self.assertRaisesRegex(ValueError, 'Worker returned a failed receipt'):
                with owner:
                    # A single failed native worker belongs to a literal native
                    # phase for native, or preceding empty phases for the others.
                    for preceding in w.PHASES[:w.PHASES.index(phase)]:
                        with owner.phase(preceding, sha(self.dll)): pass
                    with owner.phase(phase, sha(self.dll)):
                        def invoke(binding, pin):
                            result = owned.run(['dotnet', str(self.dll), command, str(study), '--work-binding', str(binding), pin],
                                ROOT, case/'worker.log', time.monotonic()+8,
                                observe=lambda value:owner.observe_process(value, sha(owned.__file__)))
                            self.assertNotEqual(result['exitCode'], 0)
                            self.assertFalse(result['timedOut']); self.assertEqual(result['activeProcesses'], 0)
                            return result
                        owner.run_worker(study, self.dll, self.dll, case/'worker.json', invoke,
                            publication_path=case/'publication.json', publication_persistence_path=case/'persistence.json')
            self.manifest(case/'owner')
            self.assertEqual(len(list(study.iterdir())), 1)
            value = w.strict((case/'owner'/(phase+'-publication-persistence.json')).read_bytes())
            self.assertEqual(value['outcome'], 'Complete')
            self.assertEqual(value['serializedObservationSha256'], sha(case/'publication.json'))
            process = owner.receipt['ledger']['processObservations'][0]['observation']
            self.assertTrue(process['jobDrained'])
            self.assertEqual(process['jobIo']['coverage'], 'KernelJobLifetimeTotals')
            self.assertGreaterEqual(process['jobIo']['counters']['writeTransferBytes'],
                sum((case/name).stat().st_size for name in ('worker.json','publication.json','persistence.json')))
            w.seal_new(case/'final-owner-observation.json', owner.final_observation)
        if export:
            w.seal_new(root/'fixture.json', dict(fixtureOnly=True, realNativeCommandFailures=True, actualCombat=0,
                productionEntropyDraws=0, wholeProcessCoverage=False, usableForAdmission=False,
                producerSha256=sha(self.dll), sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p)
                    for p in (w.__file__, owned.__file__, __file__)}))
            w.seal_new(root/'files.json', {p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})

    def test_real_python_worker_kernel_scope_includes_terminal_file(self):
        export = os.environ.get('LL_PERSISTENCE_PYTHON_EXPORT')
        root = Path(export) if export else self.root
        if export: root.mkdir()
        study = root/'study'; study.mkdir(); (study/'request.json').write_bytes(b'{"literal":true}')
        worker = root/'literal-worker.py'
        worker.write_text('import sys\nsys.path.insert(0,'+repr(str(ROOT/'Balance Harness/analysis'))+')\n'
            'from proposal_work_accounting import worker_receipt\n'
            'with worker_receipt(sys.argv[1],"independentAudit",sys.argv[2],sys.argv[3],__file__) as work:\n'
            '    work.add("literalWork")\nprint("after-terminal-publication",flush=True)\n', encoding='utf-8')
        binding = root/'binding.json'
        w.seal_new(binding, dict(version=w.WORKER_PERSISTENCE_BINDING_VERSION, phase='independentAudit', studyRoot=str(study),
            requestSha256=sha(study/'request.json'), producerSha256=sha(worker), accountingModuleSha256=sha(w.__file__),
            receiptPath=str(root/'worker.json'), publicationPath=str(root/'publication.json'), publicationPersistencePath=str(root/'persistence.json')))
        values = []
        result = owned.run([sys.executable,'-B',str(worker),str(study),str(binding),sha(binding)], root, root/'worker.log',
            time.monotonic()+8, observe=values.append)
        self.assertEqual(result['exitCode'], 0, (root/'worker.log').read_text())
        value = w.worker_publication_persistence((root/'persistence.json').read_bytes(), sha(binding), 'independentAudit',
            sha(study/'request.json'), sha(worker))
        self.assertEqual(value['outcome'], 'Complete')
        self.assertEqual(value['serializedObservationSha256'], sha(root/'publication.json'))
        self.assertTrue(values[0]['jobDrained'])
        self.assertGreaterEqual(values[0]['jobIo']['counters']['writeTransferBytes'],
            sum((root/name).stat().st_size for name in ('worker.json','publication.json','persistence.json','worker.log')))
        self.assertIn('after-terminal-publication', (root/'worker.log').read_text())
        if export:
            w.seal_new(root/'process.json', dict(result=result, observation=values[0]))
            w.seal_new(root/'fixture.json', dict(fixtureOnly=True, realPythonWorker=True, actualCombat=0, productionEntropyDraws=0,
                wholeProcessCoverage=False, usableForAdmission=False, sources={Path(p).resolve().relative_to(ROOT).as_posix():sha(p)
                    for p in (w.__file__, owned.__file__, __file__)}))
            w.seal_new(root/'files.json', {p.relative_to(root).as_posix():sha(p) for p in sorted(root.rglob('*')) if p.is_file()})


if __name__ == '__main__': unittest.main()
