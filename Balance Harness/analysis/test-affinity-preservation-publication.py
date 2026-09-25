"""Read-only history verification tests; never accesses a study or reservation registry."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('preservation_publication',Path(__file__).with_name('verify-affinity-preservation-publication.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class PublicationTests(unittest.TestCase):
    def setUp(self):
        self.study = Path('study').resolve()
        self.ledger = dict(reservationState='Complete',historical=[-2,-1],reserved=list(range(5000)))
        self.allocation = dict(version=a.VERSION,selected=list(range(4380)),reserved=list(range(5000)))
        self.prior = {'prior/history-input.json':'old'}
        self.files = dict(self.prior, **{str(self.study/n):'new' for n in ('history-input.json','seed-ledger.json')})
        self.values = list(range(-2,5000))

    def result(self):
        return a.history_summary(self.ledger,self.allocation,self.files,self.values,self.prior,self.study)

    def test_unused_entropy_tail_remains_permanently_reserved(self):
        result = self.result()
        self.assertEqual((5000,4380,620,5002,3),tuple(result[k] for k in
            ('newPermanentReservations','assignedValues','unusedPermanentlyReservedValues','liveValues','historyFiles')))
        self.assertEqual(0,result['newFights']); self.assertEqual(0,result['newValues'])

    def test_old_study_version_or_old_allocation_cannot_be_verified_as_preservation(self):
        for changes in (dict(version='tower-benchmark-tie-comparison-v1'),
                        dict(version='unknown'),dict(selected=list(range(3948)))):
            with self.subTest(changes=next(iter(changes))),self.assertRaises(ValueError):
                a.history_summary(self.ledger,dict(self.allocation,**changes),self.files,self.values,self.prior,self.study)

    def test_underfilled_or_larger_than_single_entropy_batch_reservation_is_rejected(self):
        for count in (4379,16385):
            ledger=dict(self.ledger,reserved=list(range(count)))
            allocation=dict(self.allocation,reserved=ledger['reserved'])
            with self.subTest(count=count),self.assertRaises(ValueError):
                a.history_summary(ledger,allocation,self.files,list(range(-2,count)),self.prior,self.study)

    def test_lost_unused_value_or_unexpected_new_value_rejects_union(self):
        for values in (self.values[:-1], self.values+[6000], self.values+[4999]):
            with self.subTest(values=len(values)),self.assertRaises(ValueError):
                a.history_summary(self.ledger,self.allocation,self.files,values,self.prior,self.study)

    def test_only_the_two_owned_ledgers_may_extend_history(self):
        for files in ({k:v for k,v in self.files.items() if k != str(self.study/'seed-ledger.json')},
                      dict(self.files, unexpected='unapproved'), dict(self.files, **{'prior/history-input.json':'changed'})):
            with self.subTest(files=files), self.assertRaises(ValueError):
                a.history_summary(self.ledger,self.allocation,files,self.values,self.prior,self.study)

    def test_pending_duplicate_or_colliding_reservation_is_rejected(self):
        for changes in (dict(reservationState='Pending'),dict(reserved=list(range(-1,5000))),
                        dict(reserved=list(range(5000))+[4999]),dict(historical=[-1,-2])):
            with self.subTest(changes=next(iter(changes))),self.assertRaises(ValueError):
                a.history_summary(dict(self.ledger,**changes),self.allocation,self.files,self.values,self.prior,self.study)

    def test_assignment_and_reservation_must_match_the_allocation(self):
        for changes in (dict(selected=list(range(4379))),dict(selected=list(range(4379))+[0]),
                        dict(selected=list(range(4379))+[6000]),dict(reserved=list(range(4999)))):
            with self.subTest(changes=next(iter(changes))),self.assertRaises(ValueError):
                a.history_summary(self.ledger,dict(self.allocation,**changes),self.files,self.values,self.prior,self.study)


class AccountingTests(unittest.TestCase):
    def setUp(self):
        self.prior = dict(status='PreservationComparisonAdmittedNoReservation',cumulativeRecordedSeconds=21000,
            cumulativeRecordedBytes=17000000000,cumulativeDeclaredMaximumSeconds=30000,cumulativeDeclaredMaximumBytes=21000000000)
        self.receipt = dict(chargedSeconds=1500,chargedBytes=2000000000)

    def test_actual_use_is_kept_separate_from_full_declared_allowances(self):
        result=a.verification_accounting(self.prior,self.receipt)
        self.assertEqual(result['recordedBeforeVerificationSeconds'],22500)
        self.assertEqual(result['recordedBeforeVerificationBytes'],19000000000)
        self.assertEqual(result['cumulativeDeclaredMaximumSeconds'],41400)
        self.assertEqual(result['cumulativeDeclaredMaximumBytes'],27509559808)

    def test_missing_or_smaller_declared_allowance_is_rejected(self):
        for key,value in [('status','pending'),('cumulativeDeclaredMaximumSeconds',20000),
                          ('cumulativeDeclaredMaximumBytes',16000000000),('cumulativeRecordedSeconds',None)]:
            with self.subTest(key=key),self.assertRaises(ValueError):
                a.verification_accounting(dict(self.prior,**{key:value}),self.receipt)

    def test_invalid_or_over_budget_scientific_charge_is_rejected(self):
        for key,value in [('chargedSeconds',0),('chargedSeconds',10800),('chargedBytes',6442450944),
                          ('chargedSeconds',float('nan')),('chargedBytes',True)]:
            with self.subTest(key=key),self.assertRaises(ValueError):
                a.verification_accounting(self.prior,dict(self.receipt,**{key:value}))


class ExecutionTests(unittest.TestCase):
    def setUp(self):
        self.admission,self.study,self.output=(Path(n).resolve() for n in ('admission','study','verification'))
        self.prior=dict(status='PreservationComparisonAdmittedNoReservation',cumulativeRecordedSeconds=21000,
            cumulativeRecordedBytes=17000000000,cumulativeDeclaredMaximumSeconds=30000,cumulativeDeclaredMaximumBytes=21000000000)
        self.d=dict(version='tower-affinity-preservation-execution-v1',studyVersion=a.VERSION,scientificLaunches=1,retries=0,
            maximumFights=21888,requiredFreshValues=4380,scientificMaximumSeconds=10800,scientificMaximumBytes=6442450944,
            nativeMaximumSeconds=9000,auditMaximumSeconds=1800,admissionRoot=str(self.admission),outputRoot=str(self.study),
            publicationVerification=dict(outputRoot=str(self.output),chargedSeconds=600,chargedBytes=64*1048576,fights=0,newValues=0),
            priorAccounting={k:v for k,v in self.prior.items() if k!='status'})

    def validate(self,d): a.validate_execution(d,self.prior,self.admission,self.study,self.output)

    def test_exact_execution_and_separate_verification_contract_accepts(self): self.validate(self.d)

    def test_retry_reallocation_and_extended_limits_are_rejected(self):
        for key,value in [('retries',1),('scientificLaunches',2),('requiredFreshValues',4668),('maximumFights',21889),
                          ('auditMaximumSeconds',1801),('outputRoot','other'),('studyVersion','old')]:
            with self.subTest(key=key),self.assertRaises(ValueError): self.validate(dict(self.d,**{key:value}))

    def test_changed_accounting_or_verification_allowance_is_rejected(self):
        for edit in (lambda d:d['priorAccounting'].update(cumulativeRecordedSeconds=0),
                     lambda d:d['publicationVerification'].update(chargedSeconds=601),
                     lambda d:d['publicationVerification'].update(fights=1)):
            d=copy.deepcopy(self.d);edit(d)
            with self.assertRaises(ValueError): self.validate(d)


if __name__ == '__main__': unittest.main(verbosity=2)
