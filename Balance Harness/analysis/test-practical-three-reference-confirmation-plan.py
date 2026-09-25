"""Deterministic arithmetic and exact-scenario planning checks; no game execution."""
import copy
import importlib.util
from pathlib import Path
from statistics import NormalDist
import unittest

path=Path(__file__).with_name('practical-three-reference-confirmation-plan.py')
spec=importlib.util.spec_from_file_location('confirmation_plan',path)
plan=importlib.util.module_from_spec(spec); spec.loader.exec_module(plan)


class PlanningTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.template=plan.read(plan.RUN/'template.json')
        cls.inventory=plan.read(plan.REVIEW/'selected-challengers.json')
        cls.review=plan.read(plan.REVIEW/'review.json')

    def test_family38_independent_boundaries_and_multinomials(self):
        checked=plan.arithmetic_checks()
        self.assertEqual((2983,6501,6501,728,6067),(checked['smallMultinomialCells'],checked['discordanceThresholds'],
            checked['viabilityCounts'],checked['minimumViableWins'],checked['minimumNForSufficientPowerBound']))
        self.assertAlmostEqual(plan.precision.critical_value(38),NormalDist().inv_cdf(1-.025/38),places=8)

    def test_three_comparisons_are_charged_in_joint_power(self):
        p=plan.power_bound(6500)
        self.assertAlmostEqual(1-3*p['eachContrastFailureUpper']-p['viabilityFailureUpper'],p['iidJointPassLower'],places=14)
        self.assertAlmostEqual(p['iidJointPassLower']-p['finitePopulationDeduction'],p['uniformWithoutReplacementJointPassLower'],places=14)
        self.assertGreater(p['uniformWithoutReplacementJointPassLower'],.8)
        self.assertLess(plan.power_bound(5500)['uniformWithoutReplacementJointPassLower'],.8)
        self.assertEqual(0,plan.power_bound(6500,.05)['uniformWithoutReplacementJointPassLower'])
        self.assertGreaterEqual(plan.power_bound(6500,history=633313)['uniformWithoutReplacementJointPassLower'],p['uniformWithoutReplacementJointPassLower'])

    def test_complete_eight_team_catalogue_and_order(self):
        teams=plan.catalogue(self.template,self.inventory,self.review)
        self.assertEqual([8,13,15,21,22],[t['sourceRoot'] for t in teams[:5]])
        self.assertEqual(plan.REFS,[t['partyId'] for t in teams[5:]])
        self.assertEqual(['PrimaryReference','OtherReference','OtherReference'],[t['role'] for t in teams[5:]])
        self.assertEqual(8,len({t['partyId'] for t in teams}))
        self.assertTrue(all(t['scenario']['seeds']==[] for t in teams))
        self.assertEqual(38,len(teams)+2*len(teams[:5])*len(teams[5:]))

    def test_candidate_omission_reordering_and_unbound_source_fail(self):
        for change in ['omit','reorder','path','recipe','outcome']:
            x=copy.deepcopy(self.inventory)
            if change=='omit': x['candidates'].pop()
            if change=='reorder': x['candidates'].reverse()
            if change=='path': x['candidates'][0]['sourceCheckpoint']=str(plan.RUN/'study/search-01.json')
            if change=='recipe': x['candidates'][0]['recipeHash']='changed'
            if change=='outcome': x['candidates'][0]['absoluteConfirmationWins']=0
            with self.subTest(change=change),self.assertRaises(ValueError):
                plan.catalogue(self.template,x,self.review)

    def test_reference_order_or_owned_copies_change_fails(self):
        for change in ['order','inventory']:
            x=copy.deepcopy(self.template)
            if change=='order': x['starts'].reverse()
            else: x['ownedCopies']={}
            with self.subTest(change=change),self.assertRaises(ValueError):
                plan.catalogue(x,self.inventory,self.review)

    def test_scheduled_altered_or_reordered_scenario_fails(self):
        original=plan.catalogue(self.template,self.inventory,self.review)[0]
        for change in ['seed','gear','identity','order','party','pool']:
            t=copy.deepcopy(original); s=t['scenario']; b=s['party'][0]['build']
            if change=='seed': s['seeds']=[1]
            if change=='gear': b['equipment'][0]['definitionId']='changed'
            if change=='identity': b['identityEssenceIds'].reverse()
            if change=='order': b['essenceIds'].reverse()
            if change=='party': s['party'].pop()
            if change=='pool':
                b['essenceIds'][0]='essence.outside_pool'
                b['essenceIds'].sort()
                t['partyId']=plan.digest({str(p['partySlot']):p['build']['essenceIds'] for p in s['party']})
            t['seedFreeScenarioHash']=plan.digest(s)
            with self.subTest(change=change),self.assertRaises(ValueError):
                plan.validate_team(t,self.template)

    def test_resource_envelope_reserves_both_audits(self):
        r=plan.resources(); total=r['proposedStudyCumulative']; a=r['admissionAllowance']; e=r['executionOwner']
        for field in ['seconds','bytes']:
            self.assertEqual(total[field],a[field]+e[field])
            self.assertEqual(e[field],r['nativeRunMaximum'][field]+r['auditAndPublicationReserve'][field])
        self.assertEqual((7800,4096*1048576),(total['seconds'],total['bytes']))
        self.assertEqual((18180,13584),(r['priorEngineeringLedger']['seconds'],r['priorEngineeringLedger']['MiB']))

    def test_transport_and_history_limits(self):
        chunks=[1000]*6+[500]
        self.assertEqual((6500,56,52000),(sum(chunks),8*len(chunks),8*sum(chunks)))
        self.assertEqual(1000000,987000+13000)
        self.assertEqual(646313,plan.HISTORY+13000)


if __name__=='__main__': unittest.main(verbosity=2)
