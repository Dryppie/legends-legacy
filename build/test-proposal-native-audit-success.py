"""Production native audit of literal evidence under two real Windows jobs.

Opt-in correctness fixture, never a launch/admission or performance qualification.
The backend fixture supplies captured content, actual materialized input hashes,
compressed native evidence and fabricated draws. No scientific worker is routed.
"""
import atexit
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_owner_process as monitor
import proposal_work_accounting as w
import bounded_windows_process as owned


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read(path):
    return w.strict(Path(path).read_bytes())


def manifest(root, pin):
    root = Path(root)
    w.require(sha(root/'files.json') == pin, 'Changed fixture manifest pin')
    values = read(root/'files.json')
    actual = {p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and p != root/'files.json'}
    w.require(set(values) == actual, 'Changed fixture membership')
    for name, digest in values.items():
        path = (root/name).resolve()
        w.require(path.is_relative_to(root.resolve()), 'Escaping fixture member')
        w.unlinked(path)
        w.require(sha(path) == digest, 'Changed fixture member: '+name)


def seal(root):
    return w.seal_new(root/'files.json', {p.relative_to(root).as_posix():sha(p)
        for p in sorted(root.rglob('*')) if p.is_file()})


def fixture(root, pin):
    root = Path(root)
    manifest(root, pin)
    meta = read(root/'fixture.json')
    w.require(meta['version'] == 'tower-native-audit-success-fixture-v1' and meta['fixtureOnly']
        and meta['literalOutcomes'] and not meta['scientificAdmitted'] and not meta['usableForAdmission'], 'Not a literal correctness fixture')
    for name in ('actualCombat', 'productionEntropyDraws', 'scientificReservations', 'nativeEncounterPreparations'):
        w.require(meta[name] == 0, 'Unexpected scientific activity')
    output = Path(meta['outputRoot']).resolve()
    dll = Path(meta['nativeDll']).resolve()
    w.require(output == root.resolve()/'literal-registry/result' and dll == output/'executable/BalanceHarness.dll', 'Changed fixture paths')
    w.require(sha(output/'request.json') == meta['requestSha256'], 'Changed fixture request')
    return meta, output, dll


def receipts(case, request_pin, producer_pin):
    binding_pin = sha(case/'binding.json')
    receipt = w.verify_counter_receipt(case/'worker.json', sha(case/'worker.json'), 'nativeAudit', request_pin, producer_pin)
    publication = w.worker_publication((case/'publication.json').read_bytes(), binding_pin, 'nativeAudit', request_pin, producer_pin)
    persistence = w.worker_publication_persistence((case/'persistence.json').read_bytes(), binding_pin, 'nativeAudit', request_pin, producer_pin)
    w.require(publication['outcome'] == persistence['outcome'] == 'Complete', 'Incomplete receipt publication')
    w.require(publication['serializedReceiptSha256'] == sha(case/'worker.json')
        and publication['serializedReceiptBytes'] == (case/'worker.json').stat().st_size, 'Changed counter receipt')
    w.require(persistence['serializedObservationSha256'] == sha(case/'publication.json')
        and persistence['serializedObservationBytes'] == (case/'publication.json').stat().st_size, 'Changed publication observation')
    return receipt


def verify_success(case, meta, output, dll):
    receipt = receipts(case, meta['requestSha256'], sha(dll))
    w.require(receipt['outcome'] == 'Complete', 'Native audit did not complete')
    counters = receipt['counters']
    expected = dict(reconstructedRoots=meta['expectedRoots'], reconstructedTrajectories=meta['expectedTrajectories'],
        reconstructedCatalogues=meta['expectedCatalogues'], reconstructedStudyEndpoints=meta['expectedEndpoints'],
        reconstructedHeldoutMembers=meta['expectedHeldoutMembers'], reconstructedTrialBindings=meta['fights'])
    w.require(all(counters.get(k) == v for k, v in expected.items()), 'Incomplete production audit reconstruction')
    w.require(read(case/'worker.log') == read(output/'provisional-result.json'), 'Different reconstructed result')
    process = read(case/'native-process.json')
    w.require(process['result']['exitCode'] == 0 and not process['result']['timedOut']
        and process['observation']['jobDrained'], 'Native job did not complete and drain')
    w.require(process['observation']['jobIo']['counters']['writeTransferBytes'] >= sum(
        (case/name).stat().st_size for name in ('worker.json', 'publication.json', 'persistence.json')), 'Receipt tail outside job observation')
    return expected


def owner(config_path, pin):
    """Trusted diagnostic owner, authenticating all inputs before native execution."""
    w.require(sha(config_path) == pin, 'Changed owner configuration')
    config = read(config_path)
    meta, output, dll = fixture(config['fixture'], config['fixtureManifestSha256'])
    w.require(meta['requestSha256'] == config['requestSha256'], 'Owner request binding mismatch')
    case = Path(config['owner']); case.mkdir()
    atexit.register(lambda: print('NATIVE_AUDIT_OWNER_ATEXIT', flush=True))
    binding = dict(version=w.WORKER_PERSISTENCE_BINDING_VERSION, phase='nativeAudit', studyRoot=str(output),
        requestSha256=meta['requestSha256'], producerSha256=sha(dll), accountingModuleSha256=sha(dll),
        receiptPath=str(case/'worker.json'), publicationPath=str(case/'publication.json'), publicationPersistencePath=str(case/'persistence.json'))
    w.seal_new(case/'binding.json', binding)
    observations = []
    command = ['dotnet', str(dll), 'tower-proposal-study-audit', str(output), '--work-binding', str(case/'binding.json'), sha(case/'binding.json')]
    result = owned.run(command, ROOT, case/'worker.log', config['deadline']-3, observe=observations.append)
    w.seal_new(case/'native-process.json', dict(command=command, result=result, observation=observations[0]))
    receipt = receipts(case, meta['requestSha256'], sha(dll))
    try:
        w.require(result['exitCode'] == 0, 'Native audit rejected the fixture')
        expected = verify_success(case, meta, output, dll)
        manifest(Path(config['fixture']), config['fixtureManifestSha256'])
        w.seal_new(case/'audit-verified.json', dict(fixtureOnly=True, expectedCounters=expected,
            resultSha256=sha(case/'worker.log'), requestSha256=meta['requestSha256'], producerSha256=sha(dll),
            realNativeCommand=True, productionAuditPassed=True, scientificAdmitted=False, usableForAdmission=False))
    finally:
        w.seal_new(case/'owner-tail.json', dict(workerOutcome=receipt['outcome'], completeOwnerBoundary=False,
            usableForAdmission=False))
        seal(case)
        print('NATIVE_AUDIT_OWNER_AFTER_PERSISTENCE', flush=True)


class NativeAuditSuccess(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if 'LL_NATIVE_AUDIT_FIXTURE' not in os.environ or 'LL_NATIVE_AUDIT_FIXTURE_PIN' not in os.environ:
            raise unittest.SkipTest('Requires a fresh exported native audit correctness fixture')
        cls.source = Path(os.environ['LL_NATIVE_AUDIT_FIXTURE']).resolve()
        cls.source_pin = os.environ['LL_NATIVE_AUDIT_FIXTURE_PIN']
        cls.meta, cls.output, cls.dll = fixture(cls.source, cls.source_pin)
        cls.temp = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.temp.cleanup)
        export = os.environ.get('LL_NATIVE_AUDIT_PROCESS_EXPORT')
        cls.root = Path(export).resolve() if export else Path(cls.temp.name)/'processes'
        cls.root.mkdir()

    @classmethod
    def tearDownClass(cls):
        manifest(cls.source, cls.source_pin)
        w.seal_new(cls.root/'fixture.json', dict(fixtureOnly=True, sourceManifestSha256=cls.source_pin,
            wholeProcessCoverage=False, usableForAdmission=False, resourceQualification=False,
            sources={str(Path(p).resolve().relative_to(ROOT).as_posix()):sha(p) for p in (__file__, monitor.__file__, w.__file__, owned.__file__)}))
        seal(cls.root)

    def run_case(self, name, source, pin):
        case = self.root/name; case.mkdir()
        meta = read(source/'fixture.json')
        config = dict(fixture=str(source), fixtureManifestSha256=pin, owner=str(case/'owner'),
            requestSha256=meta['requestSha256'], deadline=time.monotonic()+3600)
        w.seal_new(case/'config.json', config)
        observer = monitor.OwnerProcessMonitor(case/'monitor', meta['requestSha256'])
        result = observer.run(__file__, ['--owner', str(case/'config.json'), sha(case/'config.json')], ROOT,
            config['deadline'], protected_roots=[source, case/'owner'])
        manifest(observer.root, observer.manifest_sha256)
        manifest(case/'owner', sha(case/'owner/files.json'))
        return case, result, observer.observation

    def test_complete_production_audit_is_inside_real_enclosing_owner_job(self):
        case, result, observation = self.run_case('success', self.source, self.source_pin)
        self.assertEqual(result['exitCode'], 0, (case/'monitor/console.log').read_text())
        self.assertEqual(observation['observationOutcome'], 'Complete')
        self.assertEqual(observation['processOutcome'], 'ExitedZero')
        self.assertTrue(observation['ownerConsoleAndExitObserved'])
        self.assertFalse(observation['wholeProcessCoverage'])
        self.assertFalse(observation['usableForAdmission'])
        self.assertFalse(observation['scientificOutcomeVerified'])
        self.assertTrue(observation['outerMonitorPublicationExcluded'])
        self.assertIn('NATIVE_AUDIT_OWNER_ATEXIT', (case/'monitor/console.log').read_text())
        verify_success(case/'owner', self.meta, self.output, self.dll)
        native = read(case/'owner/native-process.json')['observation']
        outer = observation['processObservation']
        self.assertGreater(outer['jobIo']['totalProcessesAtQuery'], native['jobIo']['totalProcessesAtQuery'])
        for key in ('readTransferBytes', 'writeTransferBytes', 'otherTransferBytes'):
            self.assertGreaterEqual(outer['jobIo']['counters'][key], native['jobIo']['counters'][key])

    def mutation(self, name, change):
        # Mutate a fresh private copy only; the authenticated export is immutable.
        clone = Path(self.temp.name)/name
        shutil.copytree(self.source, clone)
        (clone/'files.json').unlink()
        output = clone/'literal-registry/result'
        change(output)
        request = read(output/'request.json')
        request.update(outputRoot=str(output), registryRoot=str(output.parent))
        (output/'request.json').write_bytes(w.canonical(request))
        meta = read(clone/'fixture.json')
        meta.update(outputRoot=str(output), nativeDll=str(output/'executable/BalanceHarness.dll'),
            requestSha256=sha(output/'request.json'), productionAuditPassed=False, mutation=name, sourceManifestSha256=self.source_pin)
        (clone/'fixture.json').write_bytes(w.canonical(meta))
        pin = seal(clone)
        case, result, observation = self.run_case(name, clone, pin)
        self.assertNotEqual(result['exitCode'], 0)
        self.assertEqual(observation['observationOutcome'], 'Complete')
        self.assertNotEqual(observation['processOutcome'], 'ExitedZero')
        receipt = receipts(case/'owner', meta['requestSha256'], sha(output/'executable/BalanceHarness.dll'))
        self.assertEqual(receipt['outcome'], 'Failed')
        self.assertFalse((case/'owner/audit-verified.json').exists())
        original = read(self.source/'files.json')
        mutated = read(clone/'files.json')
        changed = {p: digest for p, digest in mutated.items() if original.get(p) != digest}
        for relative in changed:
            target = case/'mutation-inputs'/relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(clone/relative, target)
        shutil.copyfile(clone/'files.json', case/'mutation-inputs/files.json')
        w.seal_new(case/'mutation.json', dict(name=name, sourceManifestSha256=self.source_pin, mutatedManifestSha256=pin,
            changedMembers=changed))
        return (case/'owner/worker.log').read_text()

    def test_changed_content_is_rejected_by_production_audit(self):
        def change(output):
            target = output/'content/Data/world-tower/tower-floors.json'
            target.write_bytes(target.read_bytes()+b'\n')
        self.assertIn('Changed captured content', self.mutation('changed-content', change))

    def test_resealed_false_input_identity_is_rejected_by_production_audit(self):
        def change(output):
            archive = output/'search/root-01/control'
            target = archive/'trials.jsonl'
            lines = target.read_bytes().splitlines()
            first = w.strict(lines[0]); first['inputHash'] = '0'*64
            lines[0] = json.dumps(first, separators=(',', ':'), allow_nan=False).encode('utf-8')
            target.write_bytes(b'\n'.join(lines)+b'\n')
            values = read(archive/'files.json'); values['trials.jsonl'] = sha(target)
            (archive/'files.json').write_bytes(w.canonical(values))
        self.assertIn('Adaptive battle evidence differs', self.mutation('resealed-input', change))


if __name__ == '__main__':
    if len(sys.argv) == 4 and sys.argv[1] == '--owner':
        owner(sys.argv[2], sys.argv[3])
    else:
        unittest.main()
