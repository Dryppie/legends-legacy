"""Both explicit refinement slopes must preserve the original isolation safeguards."""
import copy
import importlib.util
from pathlib import Path
import unittest

s=importlib.util.spec_from_file_location('ramp_tests',Path(__file__).with_name('test-tower-springtide-ramp.py'))
base=importlib.util.module_from_spec(s);s.loader.exec_module(base);c=base.c


class Refinement45Tests(base.SpringtideRampTests):
    coefficient=.45

    def setUp(self):
        super().setUp()
        self.plan['version']=c.SPRINGTIDE_REFINEMENT_VERSION
        self.plan['changes'][0].update(statusScalingTo=self.coefficient,
            descriptionTo=f'Deal 25% Power as Magical Damage plus {round(self.coefficient*100)}% Power per stack of Abundance to all enemies.')

    def test_exact_tradeoff_and_source_preservation(self):
        before={str(p.relative_to(self.source)):p.read_bytes() for p in self.source.rglob('*.json')}
        self.apply();self.assertEqual(c.SPRINGTIDE_REFINEMENT_VERSION,self.verify()['version'])
        expected=c.read(self.source/c.ABILITIES)
        expected[0]['description']=self.plan['changes'][0]['descriptionTo']
        expected[0]['effects'][0].update(scalingCoefficient=.25,statusScalingCoefficient=self.coefficient)
        self.assertEqual(expected,c.read(self.target/c.ABILITIES))
        self.assertEqual(before,{str(p.relative_to(self.source)):p.read_bytes() for p in self.source.rglob('*.json')})

    def test_original_exact_contract_stays_closed(self):
        plan=copy.deepcopy(self.plan);plan['version']=c.SPRINGTIDE_RAMP_VERSION
        with self.assertRaisesRegex(ValueError,'declared Springtide'):self.apply(plan)

    def test_refinement_rejects_undeclared_slopes_and_mismatched_description(self):
        for value in (.35,.4,.5,.6,0,True,float('nan'),float('inf')):
            plan=copy.deepcopy(self.plan);plan['changes'][0]['statusScalingTo']=value
            with self.subTest(value=value),self.assertRaisesRegex(ValueError,'declared Springtide'):self.apply(plan)
        plan=copy.deepcopy(self.plan)
        plan['changes'][0]['statusScalingTo']=.55 if self.coefficient==.45 else .45
        with self.assertRaisesRegex(ValueError,'declared Springtide'):self.apply(plan)


class Refinement55Tests(Refinement45Tests):
    coefficient=.55


if __name__=='__main__':unittest.main()
