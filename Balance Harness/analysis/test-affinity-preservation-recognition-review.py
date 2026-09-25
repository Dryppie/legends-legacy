"""Literal outcome/mutation tests; never generate entropy or run combat."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('reviewer',Path(__file__).with_name('affinity-preservation-recognition-review.py'))
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


class ReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.auditor = m.load_auditor(ROOT/'TestResults/affinity-preservation-recognition-admission-20260924/auditor.py')
        cls.plan = read(ROOT/'Balance Harness/Tower-Affinity-Preservation-Recognition-Plan.json')
        cls.catalogue = read(ROOT/'TestResults/affinity-preservation-recognition-catalogue-20260924/catalogue.json')
        # Literal deterministic vectors exercise unequal inclusion weights and
        # common-seed covariance. These integers are test inputs, not entropy.
        cls.panel = list(range(-10000,-10000-3072,-1))
        cls.rows = []
        for root in cls.plan['roots']:
            r = root['root']-1
            for i,t in enumerate(root['teams']):
                cls.rows.append(dict(partyId=f'r{r+1:02}-'+t['partyId'],trials=[
                    dict(seed=seed,outcome='Victory' if (s*7+i*3+r)%23 < 5+i%13 else 'Defeat')
                    for s,seed in enumerate(cls.panel[r*256:(r+1)*256])]))
        cls.result = cls.auditor.endpoint(cls.plan,cls.rows,cls.panel)

    def evaluate(self,result=None,plan=None,catalogue=None,rows=None,panel=None):
        return m.review(result or self.result,plan or self.plan,catalogue or self.catalogue,
            rows or self.rows,panel or self.panel,self.auditor)

    def test_literal_full_family(self):
        r = self.evaluate()
        self.assertEqual((r['measuredCells'],r['unmeasuredCount'],r['catalogueCells']),(145,176,321))
        self.assertEqual(r['measuredNonreferences']['measured'],109)
        self.assertEqual(sum(v['measured'] for v in r['membership'].values()),109)
        self.assertNotIn('meanGain',r['measuredNonreferences'])
        self.assertEqual(len(r['roots']),12)

    def test_published_fixture_includes_authenticated_provenance(self):
        root=ROOT/'TestResults/tower-preservation-recognition-owned-fixture-20260924-02/result'
        result=read(root/'result.json')
        study=read(root/'study/study.json')
        archive_hash=hashlib.sha256((root/'study/files.json').read_bytes()).hexdigest()
        endpoint=m.published_endpoint(result,study,archive_hash,self.auditor)
        report=self.evaluate(result=endpoint,rows=study['evidence'],panel=read(root/'confirmation-binding.json')['panel'])
        self.assertEqual(report['diagnosticDecision'],'CompleteDiagnosticOnly')
        self.assertEqual(report['allRoots'],result['pairedPool']['allRoots'])

    def test_wrong_study_provenance_rejected(self):
        study={'evidence':self.rows}
        result=dict(self.result,studyHash='0'*64,archiveHash='1'*64)
        with self.assertRaises(ValueError):m.published_endpoint(result,study,'1'*64,self.auditor)

    def test_wrong_archive_provenance_rejected(self):
        study={'evidence':self.rows}
        result=dict(self.result,studyHash=self.auditor.digest(study),archiveHash='0'*64)
        with self.assertRaises(ValueError):m.published_endpoint(result,study,'1'*64,self.auditor)

    def test_weighted_mean_and_difference(self):
        r = self.evaluate()['roots'][0]['estimates']
        p = self.plan['roots'][0]
        wins = {x['partyId']:x['wins']/256 for x in self.result['rates'] if x['root']==1}
        means = {}
        for arm in m.ARMS:
            means[arm] = sum((wins[t['partyId']]-wins[p['benchmarkPartyId']])/self.auditor.inclusion(t)/17
                for t in p['teams'] if t['provenance'][arm] and t['provenance'][arm]['generated'])
            self.assertAlmostEqual(r[arm]['mean'],means[arm])
        self.assertAlmostEqual(r['difference']['mean'],means['candidate']-means['control'])
        self.assertGreater(r['difference']['samplingVariance'],0)
        self.assertGreater(r['difference']['combatVariance'],0)

    def test_all_root_variance_divides_by_144(self):
        r = self.evaluate()
        for kind in ('samplingVariance','combatVariance'):
            self.assertAlmostEqual(r['allRoots']['difference'][kind],
                sum(x['estimates']['difference'][kind] for x in r['roots'])/144)

    def test_five_nominees_include_references(self):
        r = self.evaluate()['roots'][0]['estimates']
        p = self.plan['roots'][0]
        wins = {x['partyId']:x['wins']/256 for x in self.result['rates'] if x['root']==1}
        for arm in m.ARMS:
            selected = [t for t in p['teams'] if t['provenance'][arm] and t['provenance'][arm]['nomineeRank'] is not None]
            self.assertEqual(len(selected),5)
            self.assertTrue(any(t['stratum']=='reference' for t in selected))
            self.assertAlmostEqual(r[arm+'Nominees']['mean'],
                sum(wins[t['partyId']]-wins[p['benchmarkPartyId']] for t in selected)/5)

    def test_shared_cells_cancel_from_difference(self):
        gains = [[float((i+s)%7<3) for s in range(256)] for i in range(len(self.plan['roots'][0]['teams']))]
        p = self.plan['roots'][0]
        original = self.auditor.root_estimates(p,gains)['difference']
        for i,t in enumerate(p['teams']):
            if t['membership']=='shared': gains[i] = [100.0]*256
        self.assertEqual(original,self.auditor.root_estimates(p,gains)['difference'])

    def test_changed_weight_rejected(self):
        p=copy.deepcopy(self.plan)
        next(t for t in p['roots'][0]['teams'] if t['stratum']=='remaining-shared')['inclusionProbability'] = dict(numerator=1,denominator=1)
        with self.assertRaises(ValueError): self.evaluate(plan=p)

    def test_changed_provenance_rejected(self):
        c=copy.deepcopy(self.catalogue)
        c['roots'][0]['teams'][3]['provenance']['candidate']['acceptedPosition'] += 1
        with self.assertRaises(ValueError): self.evaluate(catalogue=c)

    def test_imputed_unknown_rejected(self):
        p=copy.deepcopy(self.plan)
        p['roots'][0]['unmeasured'][0]['independentOutcome'] = 0
        with self.assertRaises(ValueError): self.evaluate(plan=p)

    def test_missing_catalogue_cell_rejected(self):
        c=copy.deepcopy(self.catalogue)
        c['roots'][0]['teams'].pop()
        with self.assertRaises(ValueError): self.evaluate(catalogue=c)

    def test_changed_mean_rejected(self):
        r=copy.deepcopy(self.result)
        r['pairedPool']['allRoots']['difference']['mean'] += .001
        with self.assertRaises(ValueError): self.evaluate(result=r)

    def test_changed_covariance_rejected(self):
        r=copy.deepcopy(self.result)
        r['pairedPool']['roots'][0]['estimates']['combatCovariance'] = 0
        with self.assertRaises(ValueError): self.evaluate(result=r)

    def test_changed_paired_trial_rejected(self):
        rows=copy.deepcopy(self.rows)
        rows[3]['trials'][0]['outcome'] = 'Victory' if rows[3]['trials'][0]['outcome'] != 'Victory' else 'Defeat'
        with self.assertRaises(ValueError): self.evaluate(rows=rows)

    def test_changed_seed_order_rejected(self):
        panel=self.panel[:]
        panel[0],panel[1]=panel[1],panel[0]
        with self.assertRaises(ValueError): self.evaluate(panel=panel)

    def test_policy_promotion_rejected(self):
        r=copy.deepcopy(self.result)
        r['policyDefaultsChanged']=True
        with self.assertRaises(ValueError): self.evaluate(result=r)

    def test_markdown_keeps_separate_uncertainties_and_unknowns(self):
        text=m.markdown(self.evaluate())
        for token in ('Sampling SE','Combat SE','176','all five','do not combine'):
            self.assertIn(token,text)
        self.assertNotIn('Mean gain |',text.split('Measured nonreference group')[1])


class EvidenceTests(unittest.TestCase):
    def test_external_pin_and_changed_consumed_bytes_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            (root/'item.json').write_bytes(b'{"x":1}')
            raw=json.dumps({'item.json':hashlib.sha256((root/'item.json').read_bytes()).hexdigest()}).encode()
            (root/'files.json').write_bytes(raw)
            pin=hashlib.sha256(raw).hexdigest()
            with self.assertRaises(ValueError): m.Evidence(root,'0'*64)
            e=m.Evidence(root,pin)
            self.assertEqual(e.read('item.json'),{'x':1})
            (root/'item.json').write_bytes(b'{"x":2}')
            with self.assertRaises(ValueError): e.read('item.json')
            with self.assertRaises(ValueError): e.recheck()


if __name__=='__main__':
    unittest.main()
