"""Fresh synthetic collector checks; no historical studies, entropy or combat."""
import copy
import gzip
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
ANALYSIS = ROOT/'Balance Harness/analysis'


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


w = module('work_accounting', ANALYSIS/'proposal_work_accounting.py')
c = module('work_codec', ANALYSIS/'proposal_evidence_codec.py')
s = module('work_storage', ANALYSIS/'proposal_evidence_storage.py')
a = module('work_audit', ANALYSIS/'audit-proposal-affinity-study.py')
owned = module('work_owned', ROOT/'build/bounded_windows_process.py')
REQUEST, PRODUCER = 'a'*64, 'b'*64
LIMITS = dict(logicalBytes=1048576, physicalBytes=1048576)


def put(root, name, raw):
    physical = gzip.compress(raw, mtime=0)
    (root/(name+'.gz')).write_bytes(physical)
    return dict(logicalPath=name, physicalPath=name+'.gz', codec=c.CODEC, logicalBytes=len(raw),
                logicalSha256=hashlib.sha256(raw).hexdigest(), physicalBytes=len(physical),
                physicalSha256=hashlib.sha256(physical).hexdigest())


class Counters(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def test_reads_hashing_and_parsing_remain_distinct(self):
        path = self.root/'files.json'; raw = '{"dragon":"🐉"}'.encode()
        path.write_bytes(raw); work = w.Counters()
        self.assertEqual(work.read_json(path), dict(dragon='🐉'))
        work.sha(path); work.sha(path)
        self.assertEqual(work.values['applicationReadBytes.manifest'], 3*len(raw))
        self.assertEqual(work.values['jsonInputBytes'], len(raw))
        self.assertEqual(work.values['jsonParseCompleted'], 1)

    def test_codec_repeated_passes_match_returned_payload_counts(self):
        raw = '{"dragon":"🐉æ","n":123}'.encode(); entry = put(self.root,'search.json',raw); work = w.Counters()
        value, metrics = c.read_json(self.root, entry, c.VERSION, LIMITS, work=work)
        self.assertEqual(value, json.loads(raw))
        self.assertEqual(work.values['decodedBytesProcessed'], 2*len(raw))
        self.assertEqual(work.values['jsonInputBytes'], len(raw))
        self.assertEqual(work.values['decodePassesStarted'], 2)
        self.assertEqual(work.values['decodePassesCompleted'], 2)
        self.assertEqual(work.values['applicationReadBytes.payload'], metrics['physicalBytesRead'])

    def test_corrupt_logical_digest_preserves_partial_work(self):
        raw = b'{"a":1}'; entry = put(self.root,'search.json',raw); entry['logicalSha256'] = '0'*64
        work = w.Counters()
        with self.assertRaises(ValueError): c.read_json(self.root,entry,c.VERSION,LIMITS,work=work)
        self.assertEqual(work.values['decodedBytesProcessed'], len(raw))
        self.assertGreater(work.values['applicationReadBytes.payload'], 0)
        self.assertEqual(work.values['decodePassesStarted'], 1)
        self.assertEqual(work.values['decodePassesCompleted'], 0)

    def test_parser_failure_retains_attempt_and_bytes(self):
        entry = put(self.root,'search.json',b'{invalid json}'); work = w.Counters()
        with self.assertRaises(ValueError): c.read_json(self.root,entry,c.VERSION,LIMITS,work=work)
        self.assertEqual(work.values['jsonParseAttempts'], 1)
        self.assertEqual(work.values['jsonParseCompleted'], 0)
        self.assertGreater(work.values['jsonInputBytes'], 0)

    def test_cancellation_retains_reads_before_cancellation(self):
        entry = put(self.root,'search.json',b'{"a":1}'); work = w.Counters()
        def cancel():
            if work.values['applicationReadBytes.payload'] > 0: raise RuntimeError('cancelled')
        with self.assertRaisesRegex(RuntimeError,'cancelled'):
            c.read_json(self.root,entry,c.VERSION,LIMITS,cancel=cancel,work=work)
        self.assertGreater(work.values['applicationReadBytes.payload'], 0)
        self.assertEqual(work.values['jsonParseCompleted'], 0)

    def test_bound_receipt_rejects_mutation_rebinding_and_false_coverage(self):
        work = w.Counters(); work.add('decodedBytesProcessed',10)
        receipt = work.receipt('nativeAudit',REQUEST,PRODUCER,False)
        path = self.root/'work.json'; pin = w.seal_new(path,receipt)
        self.assertEqual(w.verify_counter_receipt(path,pin,'nativeAudit',REQUEST,PRODUCER),receipt)
        for phase,request,producer in (('native',REQUEST,PRODUCER),('nativeAudit','c'*64,PRODUCER),('nativeAudit',REQUEST,'d'*64)):
            with self.assertRaises(ValueError): w.verify_counter_receipt(path,pin,phase,request,producer)
        receipt['wholeProcessCoverage'] = True
        path.write_bytes(w.canonical(receipt))
        with self.assertRaises(ValueError): w.verify_counter_receipt(path,pin,'nativeAudit',REQUEST,PRODUCER)
        pin = hashlib.sha256(path.read_bytes()).hexdigest()
        with self.assertRaises(ValueError): w.verify_counter_receipt(path,pin,'nativeAudit',REQUEST,PRODUCER)

    def test_invalid_counts_and_duplicate_json_rejected(self):
        work = w.Counters()
        for value in (-1,True,1.0,float('nan')):
            with self.assertRaises(ValueError): work.add('x',value)
        with self.assertRaises(ValueError): w.strict('{"x":1,"x":2}')
        with self.assertRaises(ValueError): w.strict('{"x":NaN}')

    def test_auditor_context_is_restored_after_failure(self):
        work = w.Counters()
        with self.assertRaises(FileNotFoundError): a.audit(self.root,working=True,work=work)
        self.assertIsNone(a.WORK.get())

    def test_auditor_plain_json_semantics_and_failed_parse_are_preserved(self):
        path = self.root/'plain.json'; path.write_text('{"x":1,"x":2}',encoding='utf-8')
        expected = a.read(path); work = w.Counters(); token = a.WORK.set(work)
        try:
            self.assertEqual(a.read(path),expected)
            a.sha(path); self.assertEqual(a.raw_read(path),path.read_bytes())
            with self.assertRaises(ValueError): a.parse_line(b'{bad}')
        finally: a.WORK.reset(token)
        self.assertEqual(work.values['jsonParseAttempts'],2)
        self.assertEqual(work.values['jsonParseCompleted'],1)
        self.assertEqual(work.values['applicationReadBytes.json'],3*path.stat().st_size)

    def test_all_storage_indexes_metadata_and_owning_manifests_are_observed(self):
        selection = dict(version=c.VERSION, maximumLogicalBytes=1048576,maximumPhysicalBytes=1048576,
                         maximumMembers=72,codecSha256='a'*64,readerSha256='b'*64)
        (self.root/s.INDEX).write_text(json.dumps(dict(selection=selection,indexes=[d+'/'+s.INDEX for d in s.DIRECTORIES])))
        logical_bytes = 0
        for relative in s.DIRECTORIES:
            folder = self.root/relative; folder.mkdir(parents=True)
            names = [f'pair-{n:02d}.json' for n in range(1,13)] if relative=='study' else ['search.json']
            entries = [put(folder,name,b'{"n":1}') for name in names]; logical_bytes += sum(e['logicalBytes'] for e in entries)
            (folder/s.INDEX).write_text(json.dumps(dict(version=c.VERSION,entries=entries)))
            owner = folder if relative=='study' else folder.parent
            manifest = {p.relative_to(owner).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in owner.rglob('*') if p.is_file()}
            (owner/'files.json').write_text(json.dumps(manifest))
        work = w.Counters()
        def authenticate(owner):
            for name,pin in work.read_json(owner/'files.json').items():
                self.assertEqual(work.sha(owner/name),pin)
        reader = s.Reader(self.root,selection,dict(families=[]),c,authenticate,work)
        for relative,entries in reader.maps.items():
            for name in entries: self.assertEqual(reader.read(self.root/relative,name),dict(n=1))
        reader.finish()
        self.assertEqual(work.values['decodedBytesProcessed'],2*logical_bytes)
        self.assertGreater(work.values['applicationReadBytes.payload'],reader.codec_physical_bytes_read)
        self.assertGreater(work.values['applicationReadBytes.manifest'],0)
        self.assertGreater(work.values['applicationReadBytes.metadata'],0)
        self.assertGreater(work.values['jsonParseCompleted'],36)


class StorageAndOwner(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup); self.root = Path(self.temp.name)

    def test_deleted_scratch_and_duplicate_content_remain_charged(self):
        store = w.ManagedStorage(self.root/'scope')
        for name in ('a','b'):
            with store.create(name,'retained') as stream: stream.write(b'x'*50)
        with store.create('temp','scratch') as stream: stream.write(b'y'*150)
        store.delete('temp'); state = store.snapshot()
        self.assertEqual(state['currentRetainedBytes'],100)
        self.assertEqual(state['peakRetainedPlusScratchBytes'],250)
        self.assertEqual(state['peakScratchBytes'],150)
        self.assertEqual(state['deletedBytes'],150)
        self.assertEqual(state['totalWrittenBytes'],250)

    def test_failed_writer_retains_partial_file_and_peak(self):
        store = w.ManagedStorage(self.root/'scope')
        with self.assertRaises(RuntimeError):
            with store.create('partial','scratch') as stream:
                stream.write(b'x'*30); raise RuntimeError('synthetic failure')
        self.assertEqual(store.snapshot()['currentScratchBytes'],30)
        self.assertEqual(store.snapshot()['peakRetainedPlusScratchBytes'],30)

    def test_untracked_changed_or_open_storage_is_rejected(self):
        store = w.ManagedStorage(self.root/'scope'); stream = store.create('a','retained')
        with self.assertRaises(ValueError): store.snapshot()
        stream.write(b'a'); stream.close()
        (store.root/'a').write_bytes(b'changed')
        with self.assertRaises(ValueError): store.snapshot()
        (store.root/'a').write_bytes(b'a'); (store.root/'extra').write_bytes(b'')
        with self.assertRaises(ValueError): store.snapshot()

    def test_storage_path_escape_and_case_collision_are_rejected(self):
        store = w.ManagedStorage(self.root/'scope')
        for name in ('../escape','C:/escape','a\\b','..','x.','x '):
            with self.assertRaises(ValueError): store.create(name,'scratch')
        with store.create('A','retained'): pass
        with self.assertRaises(ValueError): store.create('a','retained')

    def test_enclosing_phases_include_cleanup_tail_without_overlap(self):
        ticks = iter([10,11,13,16,20]); ledger = w.OwnerLedger(REQUEST,PRODUCER,lambda:next(ticks))
        for phase in w.PHASES: ledger.begin(phase)
        receipt = ledger.finish(True)
        self.assertEqual(receipt['enclosingSeconds'],10)
        self.assertEqual(sum(p['end']-p['start'] for p in receipt['phases']),10)
        self.assertEqual(receipt['phases'][-1],dict(phase='publication',start=6,end=10))
        self.assertFalse(receipt['usableForAdmission'])
        self.assertFalse(receipt['wholeProcessCoverage'])

    def test_failed_lifecycle_preserves_partial_intervals(self):
        ticks = iter([1,2,4]); ledger = w.OwnerLedger(REQUEST,PRODUCER,lambda:next(ticks))
        ledger.begin('native'); ledger.begin('nativeAudit'); receipt = ledger.finish(False)
        self.assertEqual(receipt['outcome'],'Failed'); self.assertEqual(receipt['enclosingSeconds'],3)
        self.assertEqual(len(receipt['phases']),2)

    def test_receipt_attachment_rejects_failed_worker_completion_and_duplicates(self):
        ledger = w.OwnerLedger(REQUEST,PRODUCER,lambda:1)
        ledger.begin('native'); path = self.root/'receipt.json'
        pin = w.seal_new(path,w.Counters().receipt('native',REQUEST,PRODUCER,False))
        ledger.attach(path,pin,PRODUCER)
        with self.assertRaises(ValueError): ledger.attach(path,pin,PRODUCER)
        for phase in w.PHASES[1:]: ledger.begin(phase)
        with self.assertRaises(ValueError): ledger.finish(True)
        self.assertEqual(ledger.finish(False)['workers'][0]['sha256'],pin)

    def test_invalid_phase_order_incomplete_success_and_backwards_clock(self):
        ledger = w.OwnerLedger(REQUEST,PRODUCER,lambda:1)
        with self.assertRaises(ValueError): ledger.begin('publication')
        ledger.begin('native')
        with self.assertRaises(ValueError): ledger.finish(True)
        ledger.clock = lambda:0
        with self.assertRaises(ValueError): ledger.begin('nativeAudit')


class RetainedOwner(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup); self.root = Path(self.temp.name)
        self.tick = 10
        self.module_pin = hashlib.sha256(Path(w.__file__).read_bytes()).hexdigest()

    def owner(self):
        return w.RetainedOwner(self.root/'retained',REQUEST,PRODUCER,self.module_pin,lambda:self.tick)

    def phase(self, owner, name, succeeded=True, observation=True):
        with owner.phase(name,'c'*64):
            self.tick += 2
            receipt = w.Counters().receipt(name,REQUEST,'c'*64,succeeded)
            # Noncanonical worker bytes must retain their original external pin.
            raw = json.dumps(receipt,indent=2).encode(); path = self.root/(name+'.json'); path.write_bytes(raw)
            owner.attach(path,hashlib.sha256(raw).hexdigest())
            if observation:
                owner.observe_process(dict(version='tower-owned-process-observation-v1', includesHandleCleanup=True,
                    usableForAdmission=False, jobDrained=True, enclosingSeconds=1, memoryCoverage='Unknown',
                    peakJobCommitBytes=None, ownerLifetimePeakCommitBytes=None, combinedCommitUpperBoundBytes=None,
                    exitCode=0,timedOut=False,ownerError=None,memoryBoundary='BeforeHandleCleanup'),'d'*64)
            owner.callback(path.unlink)
            return raw

    def verify(self, owner):
        root = owner.storage.root
        manifest = (root/'files.json').read_bytes()
        self.assertEqual(hashlib.sha256(manifest).hexdigest(),owner.manifest_sha256)
        files = w.strict(manifest)
        self.assertEqual(set(files),{p.name for p in root.iterdir()}-{'files.json'})
        for name,pin in files.items(): self.assertEqual(hashlib.sha256((root/name).read_bytes()).hexdigest(),pin)
        return w.strict((root/'owner-work.json').read_bytes())

    def test_success_retains_external_pins_and_includes_cleanup(self):
        def cleanup():
            self.tick += 5
            with owner.storage.create('cleanup.tmp','scratch') as stream: stream.write(b'x'*3000)
            owner.storage.delete('cleanup.tmp')
        with self.owner() as owner:
            owner.callback(cleanup)
            for phase in w.PHASES:
                raw = self.phase(owner,phase)
                self.assertEqual((owner.storage.root/(phase+'-work.json')).read_bytes(),raw)
        receipt = self.verify(owner)
        self.assertEqual(receipt['ledger']['outcome'],'Complete')
        self.assertEqual(receipt['ledger']['enclosingSeconds'],13)
        self.assertEqual(receipt['ledger']['phases'][-1],dict(phase='publication',start=6,end=13))
        self.assertEqual(receipt['managedStorageBeforeTerminalPublication']['deletedBytes'],3000)
        self.assertEqual(len(receipt['ledger']['workers']),4)
        self.assertEqual(len(receipt['ledger']['processObservations']),4)
        self.assertTrue(receipt['terminalPublicationExcluded'])
        self.assertFalse(receipt['wholeProcessCoverage'])
        self.assertFalse(receipt['usableForAdmission'])
        self.assertFalse(any((self.root/(phase+'.json')).exists() for phase in w.PHASES))

    def test_failure_at_each_phase_retains_prior_work_and_cleanup(self):
        for position in range(4):
            with self.subTest(phase=w.PHASES[position]):
                owner = w.RetainedOwner(self.root/str(position),REQUEST,PRODUCER,self.module_pin,lambda:self.tick)
                marker = []
                with self.assertRaisesRegex(RuntimeError,'phase interrupted'):
                    with owner:
                        owner.callback(marker.append,'cleaned')
                        for name in w.PHASES[:position+1]: self.phase(owner,name)
                        raise RuntimeError('phase interrupted')
                receipt = self.verify(owner)
                self.assertEqual(marker,['cleaned'])
                self.assertEqual(receipt['ledger']['outcome'],'Failed')
                self.assertEqual(len(receipt['ledger']['workers']),position+1)
                self.assertIn('phase interrupted',str(receipt['errors']))

    def test_failed_worker_cannot_produce_complete_owner(self):
        owner = self.owner()
        with self.assertRaisesRegex(ValueError,'Failed worker'):
            with owner:
                for name in w.PHASES: self.phase(owner,name,succeeded=name!='nativeAudit')
        self.assertEqual(self.verify(owner)['ledger']['outcome'],'Failed')

    def test_missing_process_observation_cannot_complete(self):
        owner = self.owner()
        with self.assertRaisesRegex(ValueError,'Missing phase process'):
            with owner:
                for name in w.PHASES: self.phase(owner,name,observation=name!='publication')
        self.assertEqual(self.verify(owner)['ledger']['outcome'],'Failed')

    def test_empty_owner_cannot_complete(self):
        owner = self.owner()
        with self.assertRaisesRegex(ValueError,'Missing phase counter'):
            with owner: pass
        self.assertEqual(self.verify(owner)['ledger']['outcome'],'Failed')

    def test_cleanup_failure_keeps_original_error_and_earlier_receipts(self):
        owner = self.owner(); cleaned = []
        def fail(): self.tick += 7; raise OSError('cleanup broke')
        with self.assertRaisesRegex(RuntimeError,'worker broke'):
            with owner:
                owner.callback(cleaned.append,'last'); owner.callback(fail)
                self.phase(owner,'native')
                raise RuntimeError('worker broke')
        receipt = self.verify(owner)
        self.assertEqual(cleaned,['last'])
        self.assertEqual(receipt['ledger']['enclosingSeconds'],9)
        self.assertIn('cleanup broke',str(receipt['errors']))
        self.assertIn('worker broke',str(receipt['errors']))

    def test_cleanup_failure_after_all_phases_is_failed_and_raised(self):
        owner = self.owner()
        def fail(): raise OSError('cleanup broke')
        with self.assertRaisesRegex(OSError,'cleanup broke'):
            with owner:
                owner.callback(fail)
                for name in w.PHASES: self.phase(owner,name)
        self.assertEqual(self.verify(owner)['ledger']['outcome'],'Failed')

    def test_changed_retained_receipt_prevents_sealed_closeout(self):
        owner = self.owner()
        def change():
            path = owner.storage.root/'native-work.json'; raw = path.read_bytes()
            path.write_bytes(raw.replace(b'Complete',b'Failured'))
        with self.assertRaisesRegex(ValueError,'Changed retained receipt'):
            with owner:
                owner.callback(change)
                for name in w.PHASES: self.phase(owner,name)
        self.assertIsNone(owner.manifest_sha256)
        self.assertFalse((owner.storage.root/'files.json').exists())
        self.assertEqual(w.strict((owner.storage.root/'owner-work.json').read_bytes())['ledger']['outcome'],'Failed')

    def test_wrong_worker_binding_and_closed_owner_fail_closed(self):
        owner = self.owner()
        with self.assertRaisesRegex(ValueError,'Wrong producer'):
            with owner:
                with owner.phase('native','c'*64):
                    path = self.root/'wrong.json'; pin = w.seal_new(path,w.Counters().receipt('native',REQUEST,PRODUCER,True))
                    owner.attach(path,pin)
        self.assertEqual(self.verify(owner)['ledger']['workers'],[])
        with self.assertRaises(ValueError): owner.attach(path,pin)
        with self.assertRaises(ValueError): owner.__enter__()

    def test_duplicate_worker_receipt_fails_without_replacing_original(self):
        owner = self.owner()
        with self.assertRaisesRegex(ValueError,'Repeated phase receipt'):
            with owner:
                with owner.phase('native','c'*64):
                    path = self.root/'worker.json'; digest = w.seal_new(path,w.Counters().receipt('native',REQUEST,'c'*64,True))
                    owner.attach(path,digest); owner.attach(path,digest)
        receipt = self.verify(owner)
        self.assertEqual(receipt['ledger']['workers'][0]['sha256'],digest)
        self.assertEqual(len(receipt['ledger']['workers']),1)

    def test_terminal_write_failure_does_not_replace_original_error_or_publish_manifest(self):
        owner = self.owner(); original = owner._seal
        def fail(name,value):
            if name == 'owner-work.json': raise OSError('disk full')
            return original(name,value)
        with self.assertRaisesRegex(RuntimeError,'worker broke'):
            with owner:
                self.phase(owner,'native')
                owner._seal = fail
                raise RuntimeError('worker broke')
        self.assertIsNone(owner.manifest_sha256)
        self.assertTrue((owner.storage.root/'native-work.json').is_file())
        self.assertTrue((owner.storage.root/'native-process-01.json').is_file())
        self.assertFalse((owner.storage.root/'files.json').exists())

    def test_caught_flush_failure_cannot_be_relabelled_complete(self):
        owner = self.owner()
        with self.assertRaisesRegex(ValueError,'previously failed'):
            with owner:
                for phase in w.PHASES: self.phase(owner,phase)
                with patch('os.fsync',side_effect=OSError('flush failed')):
                    with self.assertRaisesRegex(OSError,'flush failed'): owner._seal('additional.json',dict(partial=True))
        receipt = self.verify(owner)
        self.assertEqual(receipt['ledger']['outcome'],'Failed')
        self.assertIn('flush failed',str(receipt['errors']))

    def test_wrong_module_pin_does_not_create_storage(self):
        with self.assertRaisesRegex(ValueError,'Unbound accounting module'):
            w.RetainedOwner(self.root/'retained',REQUEST,PRODUCER,'0'*64)
        self.assertFalse((self.root/'retained').exists())


class OwnedObservation(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup); self.root = Path(self.temp.name)

    def run_child(self, script, timeout=5, check=None):
        observations = []
        result = owned.run([sys.executable,'-B','-c',script],self.root,self.root/'child.log',time.monotonic()+timeout,
                           check=check,observe=observations.append)
        if 'LL_WORK_ACCOUNTING_OBSERVATIONS' in os.environ:
            destination = Path(os.environ['LL_WORK_ACCOUNTING_OBSERVATIONS'])
            destination.mkdir(exist_ok=True)
            w.seal_new(destination/(self._testMethodName+'.json'), observations[0])
        return result,observations

    def test_kernel_memory_and_cleanup_are_observed_after_child_exit(self):
        result, observations = self.run_child('blob=bytearray(2*1024*1024); print(len(blob))')
        self.assertEqual(result['exitCode'],0); self.assertEqual(len(observations),1)
        receipt = observations[0]
        self.assertTrue(receipt['jobDrained']); self.assertTrue(receipt['includesHandleCleanup'])
        self.assertGreater(receipt['peakJobCommitBytes'],0)
        self.assertGreater(receipt['ownerLifetimePeakCommitBytes'],0)
        self.assertEqual(receipt['combinedCommitUpperBoundBytes'],receipt['peakJobCommitBytes']+receipt['ownerLifetimePeakCommitBytes'])
        self.assertGreaterEqual(receipt['enclosingSeconds'],result['seconds'])
        ledger = w.OwnerLedger(REQUEST,PRODUCER,lambda:1)
        ledger.begin('native'); ledger.observe_process(receipt,PRODUCER)
        for phase in w.PHASES[1:]: ledger.begin(phase)
        self.assertEqual(ledger.finish(True)['processObservations'][0]['observation'],receipt)

    def test_timeout_retains_drain_and_memory_receipt(self):
        result, observations = self.run_child('import time; x=bytearray(1048576); time.sleep(20)',timeout=.2)
        self.assertTrue(result['timedOut']); self.assertTrue(observations[0]['jobDrained'])
        self.assertGreater(observations[0]['peakJobCommitBytes'],0)
        ledger = w.OwnerLedger(REQUEST,PRODUCER,lambda:1)
        ledger.begin('native'); ledger.observe_process(observations[0],PRODUCER)
        for phase in w.PHASES[1:]: ledger.begin(phase)
        with self.assertRaises(ValueError): ledger.finish(True)
        self.assertEqual(ledger.finish(False)['outcome'],'Failed')

    def test_failed_memory_query_is_unknown_not_zero(self):
        with patch.object(owned,'_memory_info',side_effect=OSError('synthetic query failure')):
            result,observations = self.run_child('print("done")')
        self.assertEqual(result['exitCode'],0)
        self.assertEqual(observations[0]['memoryCoverage'],'Unknown')
        self.assertIsNone(observations[0]['combinedCommitUpperBoundBytes'])
        self.assertIsNone(observations[0]['ownerLifetimePeakCommitBytes'])
        ledger = w.OwnerLedger(REQUEST,PRODUCER,lambda:1)
        ledger.begin('native'); ledger.observe_process(observations[0],PRODUCER)
        self.assertIn('wholeOwnerMemoryLifetime',ledger.finish(False)['missingCoverage'])

    def test_callback_failure_retains_observation_after_cleanup(self):
        observations = []
        def fail(): raise RuntimeError('synthetic controller failure')
        with self.assertRaisesRegex(RuntimeError,'synthetic controller failure'):
            owned.run([sys.executable,'-B','-c','import time; time.sleep(20)'],self.root,self.root/'child.log',
                      time.monotonic()+5,check=fail,observe=observations.append)
        self.assertTrue(observations[0]['jobDrained'])
        self.assertIn('synthetic controller failure',observations[0]['ownerError'])
        if 'LL_WORK_ACCOUNTING_OBSERVATIONS' in os.environ:
            destination = Path(os.environ['LL_WORK_ACCOUNTING_OBSERVATIONS'])
            destination.mkdir(exist_ok=True)
            w.seal_new(destination/(self._testMethodName+'.json'), observations[0])

    def test_owned_descendant_is_drained_before_receipt(self):
        child = "import time,pathlib; x=bytearray(1048576); time.sleep(.1); pathlib.Path('descendant-done').write_text('done')"
        script = 'import subprocess,sys; subprocess.Popen([sys.executable,"-B","-c",'+repr(child)+'])'
        result,observations = self.run_child(script)
        self.assertEqual(result['exitCode'],0,(self.root/'child.log').read_text())
        self.assertGreaterEqual(result['totalProcesses'],2); self.assertEqual(result['activeProcesses'],0)
        self.assertEqual((self.root/'descendant-done').read_text(),'done')
        self.assertTrue(observations[0]['jobDrained'])

    def test_retained_owner_keeps_real_controller_failure_and_partial_counter_receipt(self):
        module_pin = hashlib.sha256(Path(w.__file__).read_bytes()).hexdigest()
        process_pin = hashlib.sha256(Path(owned.__file__).read_bytes()).hexdigest()
        owner = w.RetainedOwner(self.root/'owner',REQUEST,PRODUCER,module_pin)
        def fail(): raise RuntimeError('controller interrupted')
        with self.assertRaisesRegex(RuntimeError,'controller interrupted'):
            with owner:
                with owner.phase('native',PRODUCER):
                    counters = w.Counters(); counters.add('applicationReadBytes.json',7)
                    try:
                        owned.run([sys.executable,'-B','-c','import time; time.sleep(20)'],self.root,self.root/'child.log',
                                  time.monotonic()+5,check=fail,
                                  observe=lambda value:owner.observe_process(value,process_pin))
                    finally:
                        path = self.root/'partial-work.json'
                        digest = w.seal_new(path,counters.receipt('native',REQUEST,PRODUCER,False))
                        owner.attach(path,digest)
        manifest = w.strict((owner.storage.root/'files.json').read_bytes())
        self.assertEqual(hashlib.sha256((owner.storage.root/'files.json').read_bytes()).hexdigest(),owner.manifest_sha256)
        for name,digest in manifest.items():
            self.assertEqual(hashlib.sha256((owner.storage.root/name).read_bytes()).hexdigest(),digest)
        receipt = w.strict((owner.storage.root/'owner-work.json').read_bytes())
        self.assertEqual(receipt['ledger']['outcome'],'Failed')
        self.assertEqual(receipt['ledger']['workers'][0]['receipt']['counters']['applicationReadBytes.json'],7)
        observed = receipt['ledger']['processObservations'][0]
        self.assertTrue(observed['observation']['jobDrained'])
        self.assertIn('controller interrupted',observed['observation']['ownerError'])
        if 'LL_WORK_ACCOUNTING_OBSERVATIONS' in os.environ:
            import shutil
            destination = Path(os.environ['LL_WORK_ACCOUNTING_OBSERVATIONS'])/'retained-owner-failure'
            shutil.copytree(owner.storage.root,destination)


class NativeExchange(unittest.TestCase):
    def test_native_bound_receipt_and_python_decode_agree(self):
        root = Path(os.environ['LL_WORK_ACCOUNTING_EXCHANGE'])
        for name,pin in json.loads((root/'files.json').read_text()).items():
            self.assertEqual(hashlib.sha256((root/name).read_bytes()).hexdigest(),pin)
        native_pin = hashlib.sha256((root/'native-work.json').read_bytes()).hexdigest()
        receipt = w.verify_counter_receipt(root/'native-work.json',native_pin,'nativeAudit',REQUEST,PRODUCER)
        entry = json.loads((root/'entry.json').read_text()); work = w.Counters()
        c.read_json(root,entry,c.VERSION,LIMITS,work=work)
        for key in ('decodedBytesProcessed','decodePassesStarted','decodePassesCompleted','jsonInputBytes','jsonParseCompleted'):
            self.assertEqual(receipt['counters'][key],work.values[key])


if __name__ == '__main__': unittest.main()
