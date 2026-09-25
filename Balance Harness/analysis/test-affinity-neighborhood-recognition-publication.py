"""Reservation, history and owned-verification regressions using literal inputs."""
import copy
import hashlib
import importlib.util
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('publication', Path(__file__).with_name('verify-affinity-neighborhood-recognition-publication.py'))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


class PublicationTests(unittest.TestCase):
    def fixture(self):
        fresh = list(range(1000,17382))
        values = [99]+fresh+[1000]
        words = [dict(ordinal=0,value=99,classification='AlreadyReserved')]
        words += [dict(ordinal=i+1,value=v,classification='Confirmation' if i<2048 else 'ReservedUnused') for i,v in enumerate(fresh)]
        words += [dict(ordinal=16383,value=1000,classification='DuplicateBatch')]
        entropy = struct.pack('<16384i',*values)
        ledger = dict(reservationState='Complete',historical=[99],reserved=fresh)
        binding = dict(entropyHash=hashlib.sha256(entropy).hexdigest(),words=words,panel=fresh[:2048],newReservations=fresh)
        return entropy,ledger,binding

    def test_all_fresh_tail_remains_reserved_after_collisions(self):
        summary = m.reservations(*self.fixture())
        self.assertEqual(16382,summary['newPermanentReservations'])
        self.assertEqual(14334,summary['unusedPermanentlyReservedValues'])
        self.assertEqual(dict(AlreadyReserved=1,DuplicateBatch=1,Confirmation=2048,ReservedUnused=14334),summary['classificationCounts'])

    def test_missing_unused_reservation_rejected(self):
        entropy,ledger,binding = self.fixture();ledger['reserved']=ledger['reserved'][:-1]
        with self.assertRaises(ValueError):m.reservations(entropy,ledger,binding)

    def test_panel_reordering_rejected(self):
        entropy,ledger,binding = self.fixture();binding['panel'].reverse()
        with self.assertRaises(ValueError):m.reservations(entropy,ledger,binding)

    def test_collision_reclassification_rejected(self):
        entropy,ledger,binding = self.fixture();binding['words'][0]['classification']='Confirmation'
        with self.assertRaises(ValueError):m.reservations(entropy,ledger,binding)

    def test_torn_and_extended_batches_rejected(self):
        entropy,ledger,binding = self.fixture()
        for bad in (entropy[:-4],entropy+b'\0'*4):
            with self.assertRaises(ValueError):m.reservations(bad,ledger,binding)

    def test_shortfall_is_not_treated_as_a_smaller_panel(self):
        entropy,ledger,binding = self.fixture()
        with self.assertRaises(ValueError):m.reservations(struct.pack('<16384i',*([99]*16384)),ledger,binding)

    def test_pending_ledger_rejected(self):
        entropy,ledger,binding = self.fixture();ledger['reservationState']='Pending'
        with self.assertRaises(ValueError):m.reservations(entropy,ledger,binding)

    def test_historical_values_must_be_unique_sorted_signed_integers(self):
        for history in ([99,99],[100,99],[True],[-2**31-1],[2**31],[]):
            entropy,ledger,binding = self.fixture();ledger['historical']=history
            with self.assertRaises(ValueError):m.reservations(entropy,ledger,binding)

    def test_changed_entropy_hash_and_binding_reservations_rejected(self):
        for key,value in [('entropyHash','0'*64),('newReservations',[])]:
            entropy,ledger,binding = self.fixture();binding[key]=value
            with self.assertRaises(ValueError):m.reservations(entropy,ledger,binding)

    def test_real_owned_literal_fixture_matches_same_profile(self):
        root=ROOT/'TestResults/tower-neighborhood-recognition-owned-fixture-verified-20260924/result'
        result=m.reservations((root/'entropy.bin').read_bytes(),m.read(root/'seed-ledger.json'),m.read(root/'confirmation-binding.json'))
        self.assertEqual(2048,result['usedValues']);self.assertEqual(16384,result['exposedWords'])

    def test_complete_union_preserves_preexisting_files_and_unused_values(self):
        args=({'old':'pin','new':'new-pin'},[1,2,3,4],{'requiredHistory':{'old':'pin'}},
            {'historical':[1,2],'reserved':[3,4]},{'excludedCombatSeeds':[1,2]})
        m.validate_history(*args)
        for kind in ('file','missing','extra','definition'):
            changed=list(copy.deepcopy(args))
            if kind=='file':changed[0]['old']='changed'
            elif kind=='missing':changed[1].pop()
            elif kind=='extra':changed[1].append(5)
            else:changed[4]['excludedCombatSeeds']=[1]
            with self.assertRaises(ValueError):m.validate_history(*changed)

    def test_owned_process_requires_success_no_timeout_no_descendants(self):
        ok=dict(mechanism='suspended-owned-job-v1',exitCode=0,timedOut=False,activeProcesses=0,totalProcesses=1)
        m.process_ok(ok)
        for key,value in [('exitCode',1),('timedOut',True),('activeProcesses',1),('totalProcesses',0),('mechanism','unowned')]:
            with self.assertRaises(ValueError):m.process_ok(dict(ok,**{key:value}))

    def test_verification_caps_and_source_are_checked_before_output(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);out=root/'out';execution=root/'execution';execution.mkdir()
            for field,value in [('postpublicationSeconds',3601),('postpublicationBytes',268435457),('verifierSha256','0'*64)]:
                declaration=dict(version=m.VERSION,postpublicationSeconds=3600,postpublicationBytes=268435456,
                    admissionManifestSha256=m.ADMISSION_PIN,verifierSha256=m.sha(Path(m.__file__)))
                declaration[field]=value
                with patch.object(m,'EXECUTION',execution),patch.object(m,'OUT',out),patch.object(m,'read',return_value=declaration):
                    with self.assertRaises(ValueError):m.verify('a'*64,'b'*64)
                self.assertFalse(out.exists())

    def test_verification_failure_retains_full_charge_and_forbids_retry(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);out=root/'out';execution=root/'execution';execution.mkdir()
            declaration=dict(version=m.VERSION,postpublicationSeconds=3600,postpublicationBytes=268435456,
                admissionManifestSha256=m.ADMISSION_PIN,verifierSha256=m.sha(Path(m.__file__)))
            m.save(execution/'declaration.json',declaration)
            with patch.object(m,'EXECUTION',execution),patch.object(m,'OUT',out),patch.object(m,'ADMISSION',root/'missing'):
                with self.assertRaises(FileNotFoundError):m.verify('a'*64,'b'*64)
                failure=m.read(out/'failure.json');charge=(out/'charge.json').read_bytes()
                self.assertEqual((3600,268435456),(failure['chargedSeconds'],failure['chargedBytes']))
                with self.assertRaisesRegex(ValueError,'No verification retries'):m.verify('a'*64,'b'*64)
                self.assertEqual(charge,(out/'charge.json').read_bytes())


if __name__=='__main__':unittest.main(verbosity=2)
