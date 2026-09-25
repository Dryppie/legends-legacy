"""Read-only history verification tests; never accesses a study or reservation registry."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('publication',Path(__file__).with_name('verify-proposal-affinity-publication.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)


class PublicationTests(unittest.TestCase):
    def setUp(self):
        self.study = Path('study').resolve()
        self.ledger = dict(reservationState='Complete',historical=[-2,-1],reserved=list(range(4000)))
        self.allocation = dict(selected=list(range(3948)),reserved=list(range(4000)))
        self.prior = {'prior/history-input.json':'old'}
        self.files = dict(self.prior, **{str(self.study/n):'new' for n in ('history-input.json','seed-ledger.json')})
        self.values = list(range(-2,4000))

    def result(self):
        return a.history_summary(self.ledger,self.allocation,self.files,self.values,self.prior,self.study)

    def test_unused_entropy_tail_remains_permanently_reserved(self):
        result = self.result()
        self.assertEqual((4000,3948,52,4002,3),tuple(result[k] for k in
            ('newPermanentReservations','assignedValues','unusedPermanentlyReservedValues','liveValues','historyFiles')))
        self.assertEqual(0,result['newFights']); self.assertEqual(0,result['newValues'])

    def test_lost_unused_value_or_unexpected_new_value_rejects_union(self):
        for values in (self.values[:-1], self.values+[5000], self.values+[3999]):
            with self.subTest(values=len(values)),self.assertRaises(ValueError):
                a.history_summary(self.ledger,self.allocation,self.files,values,self.prior,self.study)

    def test_only_the_two_owned_ledgers_may_extend_history(self):
        for files in ({k:v for k,v in self.files.items() if k != str(self.study/'seed-ledger.json')},
                      dict(self.files, unexpected='unapproved'), dict(self.files, **{'prior/history-input.json':'changed'})):
            with self.subTest(files=files), self.assertRaises(ValueError):
                a.history_summary(self.ledger,self.allocation,files,self.values,self.prior,self.study)

    def test_pending_duplicate_or_colliding_reservation_is_rejected(self):
        for changes in (dict(reservationState='Pending'),dict(reserved=list(range(-1,4000))),
                        dict(reserved=list(range(4000))+[3999]),dict(historical=[-1,-2])):
            with self.subTest(changes=next(iter(changes))),self.assertRaises(ValueError):
                a.history_summary(dict(self.ledger,**changes),self.allocation,self.files,self.values,self.prior,self.study)

    def test_assignment_and_reservation_must_match_the_allocation(self):
        for changes in (dict(selected=list(range(3947))),dict(selected=list(range(3947))+[0]),
                        dict(selected=list(range(3947))+[5000]),dict(reserved=list(range(3999)))):
            with self.subTest(changes=next(iter(changes))),self.assertRaises(ValueError):
                a.history_summary(self.ledger,dict(self.allocation,**changes),self.files,self.values,self.prior,self.study)


class AccountingTests(unittest.TestCase):
    def setUp(self):
        self.prior = dict(totalRecordedChargedSeconds=21000,totalRecordedChargedBytes=17000000000,
            cumulativeMaximumSeconds=31800,cumulativeMaximumBytes=23442450944)
        self.receipt = dict(chargedSeconds=1500,chargedBytes=2000000000)

    def test_explicit_declared_ceiling_is_not_replaced_with_smaller_recorded_use(self):
        self.prior.update(cumulativeDeclaredMaximumSeconds=30000,cumulativeDeclaredMaximumBytes=21000000000)
        result=a.verification_accounting(self.prior,self.receipt)
        self.assertEqual(result['totalRecordedChargedSeconds'],23100)
        self.assertEqual(result['cumulativeDeclaredMaximumSeconds'],41400)
        self.assertEqual(result['totalRecordedChargedBytes'],19067108864)
        self.assertEqual(result['cumulativeDeclaredMaximumBytes'],27509559808)
        self.assertEqual(result['declaredMaximumBasis'],'ExplicitPrecedingDeclaredAllowances')

    def test_legacy_admission_retains_its_existing_accounting_interpretation(self):
        result=a.verification_accounting(self.prior,self.receipt)
        self.assertEqual(result['cumulativeDeclaredMaximumSeconds'],32400)
        self.assertEqual(result['cumulativeDeclaredMaximumBytes'],23509559808)
        self.assertEqual(result['declaredMaximumBasis'],'LegacyRecordedChargesPlusFutureAllowances')

    def test_incomplete_or_inconsistent_allowances_are_rejected(self):
        for changes in (dict(cumulativeMaximumSeconds=32000),dict(cumulativeMaximumBytes=24000000000),
                dict(cumulativeDeclaredMaximumSeconds=30000),
                dict(cumulativeDeclaredMaximumSeconds=20000,cumulativeDeclaredMaximumBytes=21000000000)):
            with self.subTest(changes=changes),self.assertRaises(ValueError):
                a.verification_accounting(dict(self.prior,**changes),self.receipt)


if __name__ == '__main__': unittest.main(verbosity=2)
