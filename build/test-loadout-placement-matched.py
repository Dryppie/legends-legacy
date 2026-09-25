"""Regression checks for prospective pairing, retained phase floors and evidence parity."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('matched',Path(__file__).with_name('test-loadout-placement-matched-owned.py'))
m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)


class MatchedCosts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = m.ROOT/m.PRECHECK
        cls.inputs = {k:m.read(root/p) for k,p in m.read(root/'inputs.json').items()}
        cls.original = m.phase_costs(cls.inputs['new-costs'],cls.inputs['new-closeout'])

    def test_final_closeout_includes_publication_costs(self):
        self.assertGreater(self.original['auditSeconds'],self.inputs['new-costs']['auditSeconds'])
        self.assertGreater(self.original['auditBytes'],self.inputs['new-costs']['auditBytes'])
        self.assertEqual(self.original['nativeSeconds'],self.inputs['new-costs']['nativeSeconds'])

    def test_different_output_cannot_replace_first_cost_attempt(self):
        with self.assertRaisesRegex(ValueError,'no replacement output'):
            m.run(m.ROOT/'TestResults/not-the-frozen-first-pair',None)

    def test_faster_repeat_cannot_reduce_any_original_placement_floor(self):
        faster = {k:v/2 for k,v in self.original.items()}
        result = m.forecast(self.inputs,self.original,faster)
        self.assertEqual(self.original,result['upwardOnlyPlacementFloors'])
        self.assertEqual(dict.fromkeys(m.pre.PHASES,1),result['upwardOnlyRatios'])
        self.assertFalse(result['admitted']); self.assertFalse(result['qualificationStarted'])

    def test_slower_new_phase_raises_its_floor(self):
        for phase in m.pre.PHASES:
            with self.subTest(phase=phase):
                slower = dict(self.original); slower[phase] *= 2
                result = m.forecast(self.inputs,self.original,slower)
                self.assertEqual(2,result['upwardOnlyRatios'][phase])
                self.assertEqual(slower[phase],result['upwardOnlyPlacementFloors'][phase])

    def test_storage_growth_is_applied_before_audit_scaling(self):
        larger = dict(self.original); larger['nativeBytes'] *= 2
        a = m.forecast(self.inputs,self.original,self.original)
        b = m.forecast(self.inputs,self.original,larger)
        self.assertGreater(b['roundedPhaseFloors']['auditSeconds'],a['roundedPhaseFloors']['auditSeconds'])
        self.assertIn('auditSeconds',b['exceededPartitions'])
        self.assertIn('nativeBytes',b['exceededPartitions'])
        self.assertEqual('MatchedResourceFloorsExceedEnvelope',b['status'])

    def test_inherited_floors_and_margin_remain_binding(self):
        result = m.forecast(self.inputs,self.original,self.original)
        for k,v in self.inputs['design']['resources']['inheritedForecastFloors'].items():
            self.assertGreaterEqual(result['roundedPhaseFloors'][k],v)
        self.assertEqual(2,result['timingMargin']); self.assertEqual(120,result['publicationReserveSeconds'])
        self.assertEqual(self.inputs['pilot-closeout']['auditSeconds'],result['completedPilotCosts']['auditSeconds'])

    def test_invalid_or_incomplete_costs_fail_closed(self):
        for value in (0,-1,float('nan'),float('inf'),True):
            with self.subTest(value=value):
                invalid = dict(self.original); invalid['nativeSeconds'] = value
                with self.assertRaises(ValueError): m.forecast(self.inputs,invalid,self.original)
        for field,value in [('status','Incomplete'),('retries',1)]:
            invalid = copy.deepcopy(self.inputs['new-costs']); invalid[field] = value
            with self.assertRaises(ValueError): m.phase_costs(invalid,self.inputs['new-closeout'])

    def test_closeout_cannot_lower_receipts_or_change_request(self):
        for field,value in [('auditSeconds',1),('auditBytes',1),('requestHash','changed'),('retainedBytes',1)]:
            invalid = copy.deepcopy(self.inputs['new-closeout']); invalid[field] = value
            with self.assertRaises(ValueError): m.phase_costs(self.inputs['new-costs'],invalid)


class PairedEvidence(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.a = Path(self.tmp.name)/'a'; self.b = Path(self.tmp.name)/'b'
        for root in (self.a,self.b):
            root.mkdir()
            for name in m.FILES: (root/name).write_text('{}',encoding='utf-8')
            context = dict(scope=dict(requiredPartySize=10,budget=dict(essenceSlots=5),contexts=[dict(characterTemplates=list(range(10)))]))
            (root/'context.json').unlink(); m.write(root/'context.json',context)
            contract = dict(version=m.VERSION,commonV5PlanHashes=['x']*12,selected=list(range(200000,204380)),reserved=list(range(200000,216384)))
            (root/'matched-contract.json').unlink(); m.write(root/'matched-contract.json',contract)
            m.write(root/'plan.json',dict(version='tower-affinity-allied-action-comparison-v1' if root==self.a else 'tower-loadout-placement-comparison-v1',
                candidate={'v':5} if root==self.a else {'v':6},control={'v':4} if root==self.a else {'v':5}))

    def test_pair_is_exact_before_dispatch(self):
        self.assertEqual('MatchedBeforeEitherOwnerStarted',m.check_pair(self.a,self.b)['status'])

    def test_any_prepared_input_drift_blocks_pair(self):
        for name in m.FILES:
            with self.subTest(name=name):
                path=self.b/name; before=path.read_bytes(); path.write_bytes(before+b' ')
                with self.assertRaises(ValueError): m.check_pair(self.a,self.b)
                path.write_bytes(before)

    def test_all_v5_archives_and_catalogues_required(self):
        for number in range(1,13):
            for root,arm in ((self.a,'candidate'),(self.b,'control')):
                folder = root/f'search/root-{number:02}/{arm}'; (folder/'racing').mkdir(parents=True)
                for name in ('racing/plan.json','racing/search.json','racing/batch-01.json','racing/panel-01.json'):
                    m.write(folder/name,dict(root=number))
                m.seal(folder)
            m.write(self.b/f'study/placement-catalogue-{number:02}.json',dict(root=number))
        self.assertEqual(12,len(m.verify_shared(self.a,self.b)['roots']))
        target = self.b/'search/root-12/control/racing/batch-01.json'; target.write_text('{}',encoding='utf-8')
        with self.assertRaises(ValueError): m.verify_shared(self.a,self.b)


if __name__ == '__main__': unittest.main()
