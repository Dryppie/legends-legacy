"""Literal cooperative storage contracts; no combat, process or cost experiment."""
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Balance Harness/analysis'))
import proposal_diagnostic_storage as d
import proposal_work_accounting as w


def contract(files=None, **limits):
    return dict(version=d.VERSION, files=files or {
        'receipt.json':dict(kind='retained', maxBytes=10),
        'first.tmp':dict(kind='scratch', maxBytes=20),
        'second.tmp':dict(kind='scratch', maxBytes=20)},
        **(dict(maxLiveBytes=40, maxScratchBytes=30, maxTotalWrittenBytes=50) | limits))


class FaultFile:
    def __init__(self, stream, mode, error=None, close_error=None):
        self.stream, self.mode, self.error, self.close_error = stream, mode, error, close_error
    def fileno(self): return self.stream.fileno()
    def write(self, raw):
        if self.mode == 'partial': self.stream.write(raw[:2]); raise self.error
        if self.mode == 'short': return self.stream.write(raw[:max(1,len(raw)//2)])
        if self.mode == 'invalid': return None
        if self.mode == 'lie': self.stream.write(raw); return len(raw)-1
        return self.stream.write(raw)
    def flush(self):
        if self.mode == 'flush': raise self.error
        return self.stream.flush()
    def close(self):
        self.stream.close()
        if self.close_error is not None: raise self.close_error


class ScratchOwnership(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory(); self.addCleanup(temp.cleanup)
        self.base = Path(temp.name); self.work = w.OwnerFileCounters()

    def store(self, value=None):
        return d.DiagnosticStorage(self.base/'scope', value or contract(), self.work)

    def fault(self, mode, error=None, close_error=None):
        original = Path.open
        def opening(path,*args,**kwargs):
            stream = original(path,*args,**kwargs)
            return FaultFile(stream,mode,error,close_error) if args and args[0]=='xb' else stream
        return patch.object(Path,'open',opening)

    def owner_contract(self):
        files={name:dict(kind='retained',maxBytes=100000) for name in
               ['binding.json','owner-work.json','files.json',*[p+'-work.json' for p in w.PHASES],
                *[f'{p}-process-{i:02d}.json' for i,p in enumerate(w.PHASES,1)]]}
        files['transient.tmp']=dict(kind='scratch',maxBytes=131072)
        return contract(files,maxLiveBytes=1000000,maxScratchBytes=131072,maxTotalWrittenBytes=1000000)

    def literal_phases(self, owner, base):
        for phase in w.PHASES:
            with owner.phase(phase,'a'*64):
                raw=w.canonical(w.Counters().receipt(phase,'b'*64,'a'*64,True))
                source=base/(phase+'.json');source.write_bytes(raw)
                owner.attach(source,hashlib.sha256(raw).hexdigest())
                # Literal synthetic observation; no process is launched.
                owner.observe_process(dict(version='tower-owned-process-observation-v1',includesHandleCleanup=True,
                    usableForAdmission=False,jobDrained=True,enclosingSeconds=0,memoryCoverage='Unknown',
                    peakJobCommitBytes=None,ownerLifetimePeakCommitBytes=None,combinedCommitUpperBoundBytes=None,
                    exitCode=0,timedOut=False,ownerError=None,memoryBoundary='BeforeHandleCleanup'),'a'*64)

    def test_deleted_scratch_remains_in_peak_and_total(self):
        store = self.store()
        with store.create('receipt.json','retained') as stream: stream.write(b'r'*10); stream.flush()
        with store.create('first.tmp','scratch') as stream: stream.write(b'x'*20)
        store.delete('first.tmp')
        with store.create('second.tmp','scratch') as stream: stream.write(b'y'*20)
        store.delete('second.tmp')
        state = store.snapshot()
        self.assertEqual([state[k] for k in ('currentRetainedBytes','currentScratchBytes','peakRetainedPlusScratchBytes',
                                            'peakScratchBytes','totalWrittenBytes','deletedBytes')], [10,0,30,20,50,40])
        self.assertEqual(state['contractSha256'],hashlib.sha256(w.canonical(contract())).hexdigest())
        self.assertFalse(state['wholeProcessCoverage']); self.assertFalse(state['usableForAdmission'])
        self.assertFalse(state['filesystemConfinement']); self.assertFalse(state['externalWritesBounded'])

    def test_contract_is_copied_before_caller_mutation(self):
        value=contract();store=self.store(value);pin=store.contract_sha256
        value['files']['first.tmp']['maxBytes']=999
        self.assertEqual(store.contract_sha256,pin)
        with store.create('first.tmp','scratch') as stream:
            with self.assertRaisesRegex(ValueError,'limit'):stream.write(b'x'*21)

    def test_invalid_names_and_case_aliases_reject_before_mkdir(self):
        for name in ('../x','a/b','a\\b','x:y','C:foo','.', '..','x.', 'x ', 'NUL','con.json','COM1.tmp',
                     'LPT9','a\0b','a\nb','é','x~1', 'a'*121):
            with self.subTest(name=name),self.assertRaises(ValueError):
                self.store(contract({name:dict(kind='scratch',maxBytes=1)}))
            self.assertFalse((self.base/'scope').exists())
        with self.assertRaises(ValueError):self.store(contract({'A':dict(kind='scratch',maxBytes=1),'a':dict(kind='scratch',maxBytes=1)}))

    def test_invalid_schema_limits_and_roles_reject_before_mkdir(self):
        values=[contract() | {'extra':1},contract() | {'version':'bad'},contract() | {'files':{}},
                contract() | {'files':{'a':dict(kind='unknown',maxBytes=1)}},
                contract() | {'files':{'a':dict(kind='scratch',maxBytes=True)}}]
        for key in ('maxLiveBytes','maxScratchBytes','maxTotalWrittenBytes'):
            values += [contract(**{key:v}) for v in (-1,True,1.5,None,2**63)]
        for value in values:
            with self.subTest(value=value),self.assertRaises(ValueError):self.store(value)
            self.assertFalse((self.base/'scope').exists())

    def test_existing_directory_is_not_adopted(self):
        root=self.base/'scope';root.mkdir();(root/'old').write_bytes(b'preserve')
        with self.assertRaises(FileExistsError):self.store()
        self.assertEqual((root/'old').read_bytes(),b'preserve')

    def test_protected_roots_reject_overlap_in_both_directions(self):
        for protected in (self.base,self.base/'scope',self.base/'scope'/'child'):
            with self.subTest(protected=protected),self.assertRaisesRegex(ValueError,'Overlapping'):
                d.DiagnosticStorage(self.base/'scope',contract(),protected_roots=[protected])
        self.assertFalse((self.base/'scope').exists())

    def test_sibling_protected_root_is_allowed_and_unchanged(self):
        protected=self.base/'protected';protected.mkdir();(protected/'a').write_bytes(b'original')
        store=d.DiagnosticStorage(self.base/'scope',contract(),protected_roots=[protected])
        self.assertEqual(store.snapshot()['totalWrittenBytes'],0)
        self.assertEqual((protected/'a').read_bytes(),b'original')

    def test_undeclared_name_poison_prevents_later_success(self):
        store=self.store()
        with self.assertRaisesRegex(ValueError,'Undeclared'):store.create('unknown','scratch')
        self.assertFalse((store.root/'unknown').exists())
        with self.assertRaisesRegex(ValueError,'previously failed'):store.snapshot()

    def test_changed_role_is_rejected(self):
        store=self.store()
        with self.assertRaisesRegex(ValueError,'changed role'):store.create('first.tmp','retained')
        self.assertEqual(list(store.root.iterdir()),[])

    def test_deleted_name_cannot_be_reused(self):
        store=self.store()
        with store.create('first.tmp','scratch') as stream:stream.write(b'x')
        store.delete('first.tmp')
        with self.assertRaisesRegex(ValueError,'Repeated'):store.create('first.tmp','scratch')
        self.assertEqual(store.written,1)

    def test_per_file_cap_rejects_before_write(self):
        store=self.store()
        with store.create('first.tmp','scratch') as stream:
            with self.assertRaisesRegex(ValueError,'limit'):stream.write(b'x'*21)
        self.assertEqual((store.root/'first.tmp').stat().st_size,0)
        self.assertFalse(store.write_progress_unknown)
        with self.assertRaises(ValueError):store.snapshot()

    def test_live_cap_includes_retained_and_scratch(self):
        store=self.store(contract(maxLiveBytes=15))
        with store.create('receipt.json','retained') as stream:stream.write(b'r'*10)
        with store.create('first.tmp','scratch') as stream:
            with self.assertRaisesRegex(ValueError,'limit'):stream.write(b'x'*6)
        self.assertEqual(store.written,10)

    def test_scratch_cap_includes_overlapping_files(self):
        store=self.store()
        with store.create('first.tmp','scratch') as first:
            first.write(b'x'*20)
            with store.create('second.tmp','scratch') as second:
                with self.assertRaisesRegex(ValueError,'limit'):second.write(b'y'*11)
        self.assertEqual(store.scratch_peak,20)

    def test_total_cap_is_not_reset_by_delete(self):
        store=self.store(contract(maxTotalWrittenBytes=21))
        with store.create('first.tmp','scratch') as stream:stream.write(b'x'*20)
        store.delete('first.tmp')
        with store.create('second.tmp','scratch') as stream:
            with self.assertRaisesRegex(ValueError,'limit'):stream.write(b'y'*2)

    def test_zero_length_file_and_write_are_valid(self):
        store=self.store(contract({'empty':dict(kind='scratch',maxBytes=0)},maxLiveBytes=0,maxScratchBytes=0,maxTotalWrittenBytes=0))
        with store.create('empty','scratch') as stream:self.assertEqual(stream.write(b''),0)
        store.delete('empty');self.assertEqual(store.snapshot()['totalWrittenBytes'],0)

    def test_partial_return_counts_actual_accepted_bytes(self):
        store=self.store()
        with self.fault('short'),store.create('first.tmp','scratch') as stream:
            self.assertEqual(stream.write(b'123456'),3)
        self.assertEqual(store.snapshot()['totalWrittenBytes'],3)
        self.assertEqual(self.work.values['diagnosticAcceptedWriteBytes'],3)

    def test_hidden_prefix_error_is_unknown_and_unsealable(self):
        store=self.store();error=OSError('hidden prefix')
        with self.fault('partial',error),self.assertRaises(OSError) as caught:
            with store.create('first.tmp','scratch') as stream:stream.write(b'123456')
        self.assertIs(caught.exception,error);self.assertTrue(store.write_progress_unknown)
        self.assertEqual((store.root/'first.tmp').read_bytes(),b'12')
        self.assertEqual(self.work.values['diagnosticFailedWriteBytesUnknown'],1)
        with self.assertRaises(ValueError):store.snapshot()

    def test_invalid_write_result_is_unknown(self):
        store=self.store()
        with self.fault('invalid'),self.assertRaisesRegex(ValueError,'write result'):
            with store.create('first.tmp','scratch') as stream:stream.write(b'123')
        self.assertTrue(store.write_progress_unknown)
        with self.assertRaises(ValueError):store.snapshot()

    def test_hidden_extra_bytes_reject_even_with_valid_return_type(self):
        store=self.store()
        with self.fault('lie'),self.assertRaisesRegex(ValueError,'Changed'):
            with store.create('first.tmp','scratch') as stream:stream.write(b'123')
        with self.assertRaises(ValueError):store.snapshot()

    def test_write_error_survives_close_error(self):
        store=self.store();error=OSError('primary write');closing=OSError('secondary close')
        with self.fault('partial',error,closing),self.assertRaises(OSError) as caught:
            with store.create('first.tmp','scratch') as stream:stream.write(b'123')
        self.assertIs(caught.exception,error);self.assertIn('secondary close',str(error.__notes__))

    def test_flush_error_closes_handle_and_poisons_scope(self):
        store=self.store();error=OSError('flush')
        with self.fault('flush',error),self.assertRaises(OSError) as caught:
            with store.create('first.tmp','scratch') as stream:stream.write(b'123');stream.flush()
        self.assertIs(caught.exception,error);self.assertFalse(store.live)
        self.assertEqual(self.work.values['diagnosticSyncAttempted'],0)
        with self.assertRaises(ValueError):store.snapshot()

    def test_sync_error_poisons_scope(self):
        store=self.store()
        with patch.object(d.os,'fsync',side_effect=OSError('sync')),self.assertRaises(OSError):
            with store.create('first.tmp','scratch') as stream:stream.flush()
        self.assertFalse(store.live)
        with self.assertRaises(ValueError):store.snapshot()

    def test_close_error_poisons_scope(self):
        store=self.store()
        with self.fault('normal',close_error=OSError('close')),self.assertRaises(OSError):
            with store.create('first.tmp','scratch'):pass
        with self.assertRaises(ValueError):store.snapshot()

    def test_delete_failure_keeps_known_bytes_and_prevents_snapshot(self):
        store=self.store()
        with store.create('first.tmp','scratch') as stream:stream.write(b'123')
        with patch.object(Path,'unlink',side_effect=OSError('delete')),self.assertRaises(OSError):store.delete('first.tmp')
        self.assertEqual(store.deleted,0);self.assertEqual(store.files['first.tmp']['size'],3)
        with self.assertRaises(ValueError):store.snapshot()

    def test_retained_file_cannot_be_deleted(self):
        store=self.store()
        with store.create('receipt.json','retained'):pass
        with self.assertRaisesRegex(ValueError,'Only closed'):store.delete('receipt.json')
        self.assertTrue((store.root/'receipt.json').exists())

    def test_open_file_cannot_be_snapshotted_or_deleted(self):
        for operation in ('snapshot','delete'):
            store=d.DiagnosticStorage(self.base/operation,contract())
            with store.create('first.tmp','scratch'):
                with self.assertRaises(ValueError):getattr(store,operation)(*(['first.tmp'] if operation=='delete' else []))

    def test_same_length_content_change_is_rejected(self):
        store=self.store()
        with store.create('first.tmp','scratch') as stream:stream.write(b'123')
        (store.root/'first.tmp').write_bytes(b'456')
        with self.assertRaisesRegex(ValueError,'content'):store.snapshot()

    def test_replacement_with_identical_bytes_is_rejected(self):
        store=self.store()
        with store.create('first.tmp','scratch') as stream:stream.write(b'123')
        (store.root/'first.tmp').rename(self.base/'original')
        (store.root/'first.tmp').write_bytes(b'123')
        with self.assertRaisesRegex(ValueError,'Changed'):store.snapshot()

    def test_failed_create_preserves_error_and_cannot_retry(self):
        store=self.store();error=OSError('exclusive open')
        with patch.object(Path,'open',side_effect=error),self.assertRaises(OSError) as caught:
            store.create('first.tmp','scratch')
        self.assertIs(caught.exception,error)
        with self.assertRaisesRegex(ValueError,'previously failed'):store.create('first.tmp','scratch')

    def test_caller_exception_retains_known_partial_storage(self):
        store=self.store();error=RuntimeError('caller')
        with self.assertRaises(RuntimeError) as caught:
            with store.create('first.tmp','scratch') as stream:stream.write(b'123');raise error
        self.assertIs(caught.exception,error)
        self.assertEqual(store.snapshot()['currentScratchBytes'],3)
        self.assertEqual(store.failed_operations,0)

    def test_mutable_buffer_rejects_before_write(self):
        store=self.store()
        with store.create('first.tmp','scratch') as stream:
            with self.assertRaisesRegex(ValueError,'bytes'):stream.write(bytearray(b'123'))
        self.assertEqual(store.written,0)

    def test_hard_link_is_rejected(self):
        store=self.store()
        with store.create('first.tmp','scratch') as stream:stream.write(b'123')
        os.link(store.root/'first.tmp',self.base/'alias')
        with self.assertRaisesRegex(ValueError,'Changed'):store.snapshot()

    def test_root_replacement_is_rejected(self):
        store=self.store();store.root.rename(self.base/'old');store.root.mkdir()
        with self.assertRaisesRegex(ValueError,'Replaced'):store.snapshot()

    def test_untracked_file_rejects_before_next_write(self):
        store=self.store()
        with store.create('first.tmp','scratch') as stream:
            (store.root/'untracked').write_bytes(b'x')
            with self.assertRaisesRegex(ValueError,'Untracked'):stream.write(b'123')
        self.assertEqual((store.root/'first.tmp').stat().st_size,0)

    def test_reparse_attribute_rejects_before_creation(self):
        original=Path.lstat
        def metadata(path,*args,**kwargs):
            info=original(path,*args,**kwargs)
            if path==self.base:
                class Reparse:st_mode=info.st_mode;st_file_attributes=1024
                return Reparse()
            return info
        with patch.object(Path,'lstat',metadata),self.assertRaises(ValueError):self.store()
        self.assertFalse((self.base/'scope').exists())

    def test_external_transient_file_is_not_claimed_as_covered(self):
        store=self.store();external=self.base/'external-cache';external.write_bytes(b'x'*100);external.unlink()
        state=store.snapshot();self.assertEqual(state['peakRetainedPlusScratchBytes'],0)
        self.assertIn('runtimeAndCachePathsOutsideScope',state['missingCoverage'])
        self.assertFalse(state['externalWritesBounded'])

    def test_owner_opt_in_binds_contract_and_preserves_terminal_exclusion(self):
        export=os.environ.get('LL_SCRATCH_OWNERSHIP_EXPORT')
        base=Path(export) if export else self.base/'export';base.mkdir()
        value=self.owner_contract()
        producer='a'*64;module=hashlib.sha256(Path(w.__file__).read_bytes()).hexdigest()
        owner=w.RetainedOwner(base/'owner','b'*64,producer,module,work=self.work,storage_contract=value,
                              protected_roots=[ROOT/'LL/tools/BalanceHarness'])
        with owner:
            self.literal_phases(owner,base)
            with owner.storage.create('transient.tmp','scratch') as stream:stream.write(b'x'*131072)
            owner.storage.delete('transient.tmp')
        state=owner.final_observation['managedStorageAfterPublication']
        self.assertEqual(owner.final_observation['outcome'],'Complete')
        self.assertEqual(state['peakScratchBytes'],131072);self.assertEqual(state['currentScratchBytes'],0)
        self.assertGreater(state['totalWrittenBytes'],131072)
        binding=w.strict((owner.storage.root/'binding.json').read_bytes())
        self.assertEqual(binding['storageContract'],value)
        self.assertEqual(binding['storageModuleSha256'],self.work.sha(d.__file__))
        self.assertEqual(binding['storageContractSha256'],state['contractSha256'])
        self.assertTrue(owner.final_observation['observationPersistenceExcluded'])
        (base/'observation.json').write_bytes(w.canonical(owner.final_observation))
        (base/'contract.json').write_bytes(w.canonical(value))
        sources={str(Path(p).relative_to(ROOT).as_posix()):hashlib.sha256(Path(p).read_bytes()).hexdigest()
                 for p in (__file__,d.__file__,w.__file__)}
        (base/'fixture.json').write_bytes(w.canonical(dict(sources=sources,actualCombat=0,productionEntropyDraws=0,
            realProcessObservations=0,syntheticProcessObservations=4,wholeProcessCoverage=False,usableForAdmission=False)))
        manifest={p.relative_to(base).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in base.rglob('*') if p.is_file()}
        (base/'files.json').write_bytes(w.canonical(manifest))

    def test_terminal_limit_cannot_produce_successful_owner_package(self):
        value=self.owner_contract();value['files']['owner-work.json']['maxBytes']=0
        module=hashlib.sha256(Path(w.__file__).read_bytes()).hexdigest()
        owner=w.RetainedOwner(self.base/'owner','b'*64,'a'*64,module,work=self.work,storage_contract=value)
        with self.assertRaisesRegex(ValueError,'limit'):
            with owner:self.literal_phases(owner,self.base)
        self.assertIsNone(owner.manifest_sha256)
        self.assertEqual(owner.final_observation['outcome'],'Failed')
        self.assertIsNone(owner.final_observation['managedStorageAfterPublication'])
        self.assertFalse((owner.storage.root/'files.json').exists())


if __name__ == '__main__': unittest.main()
