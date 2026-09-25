"""Literal generation fixtures and synthetic outcome panels; no battle execution."""
import copy
from fractions import Fraction
import importlib.util
import json
import math
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('neighborhood_plan',Path(__file__).with_name('affinity-neighborhood-recognition-plan.py'))
plan=importlib.util.module_from_spec(spec);spec.loader.exec_module(plan)


def fixture():
    ids=['a','b','c','d','e','f','x']
    context=dict(scope=dict(requiredPartySize=2,budget=dict(essenceSlots=3),ownedCopies=None,
        allowedEssences=[dict(id=e,family='family-'+e) for e in ids]))
    builds={'1':['a','b','c'],'2':['d','e','f']}
    parent=dict(id=plan.digest(builds),builds=builds)
    policy=dict(version=plan.CONTROL,parentTickets=['benchmark'],createdDamageAffinityIds=['route-a','route-b'])
    affinities=[dict(id='route-a',producerEssenceId='a',modifierEssenceId='x'),
                dict(id='route-b',producerEssenceId='b',modifierEssenceId='x')]
    return context,parent,policy,affinities,{'c'}


def statistical_fixture():
    teams=[dict(partyId=f'reference-{i}',membership='reference') for i in range(3)]
    teams += [dict(partyId=f'recipe-{i:02d}',membership='shared' if i<26 else 'control-only') for i in range(41)]
    value=dict(version=plan.VERSION,samplesPerTeam=plan.SAMPLES,teams=teams,benchmarkPartyId='reference-0',
        decision=dict(version=plan.OUTCOME_VERSION,minimumPracticalGain=dict(numerator=3,denominator=100)))
    value['statisticalEndpoints']=plan.endpoint_contract(teams,'reference-0')
    return value


class NeighborhoodTests(unittest.TestCase):
    def test_complete_distinct_recipes_and_duplicate_derivations(self):
        old=plan.enumerate_neighborhood(*fixture())
        self.assertEqual(7,len(old['recipes']))
        self.assertEqual(8,sum(len(r['derivations']) for r in old['recipes'].values()))
        self.assertEqual(4,len(old['opportunities']))
        self.assertEqual(1,sum(len(r['derivations'])==2 for r in old['recipes'].values()))

    def test_provider_guard_is_a_subset_not_a_new_search_space(self):
        f=list(fixture());old=plan.enumerate_neighborhood(*f)
        f[2]['version']=plan.CANDIDATE;new=plan.enumerate_neighborhood(*f)
        self.assertEqual(6,len(new['recipes']))
        self.assertLess(set(new['recipes']),set(old['recipes']))
        self.assertTrue(all(r['builds']['1']==f[1]['builds']['1'] for r in new['recipes'].values()))

    def test_family_collisions_are_excluded_case_insensitively(self):
        f=list(fixture());f[0]['scope']['allowedEssences'][-1]['family']='FAMILY-D'
        rows=plan.enumerate_neighborhood(*f)
        for r in rows['recipes'].values():
            for ids in r['builds'].values():self.assertFalse('x' in ids and 'd' in ids)

    def test_owned_copy_bound_is_enforced(self):
        f=list(fixture());f[0]['scope']['ownedCopies']={e['id']:2 for e in f[0]['scope']['allowedEssences']}
        f[0]['scope']['ownedCopies']['x']=0
        self.assertEqual({},plan.enumerate_neighborhood(*f)['recipes'])

    def test_active_pair_retains_empty_opportunity(self):
        f=list(fixture());f[1]['builds']['1']=['a','b','x'];f[1]['id']=plan.digest(f[1]['builds'])
        first=plan.enumerate_neighborhood(*f)['opportunities'][:2]
        self.assertTrue(all(r['alreadyActive'] and not r['legalEdits'] for r in first))

    def test_missing_affinity_rejected(self):
        f=list(fixture());f[3].pop()
        with self.assertRaisesRegex(ValueError,'Unresolved'):plan.enumerate_neighborhood(*f)

    def test_duplicate_affinity_rejected(self):
        f=list(fixture());f[3].append(copy.deepcopy(f[3][0]))
        with self.assertRaisesRegex(ValueError,'Unresolved'):plan.enumerate_neighborhood(*f)

    def test_unknown_policy_and_parent_identity_rejected(self):
        for field in ('policy','parent'):
            f=list(fixture())
            if field=='policy':f[2]['version']='unknown'
            else:f[1]['id']='forged'
            with self.assertRaises(ValueError):plan.enumerate_neighborhood(*f)

    def test_missing_owner_rejected(self):
        f=list(fixture());del f[1]['builds']['2'];f[1]['id']=plan.digest(f[1]['builds'])
        with self.assertRaisesRegex(ValueError,'owner'):plan.enumerate_neighborhood(*f)

    def coverage(self):
        expected=plan.enumerate_neighborhood(*fixture())
        coverage=copy.deepcopy({k:expected[k] for k in ('pairs','opportunities')})
        coverage.update(activePairOwners=0,eligiblePairOwners=4)
        return expected,coverage

    def test_literal_coverage_passes(self):
        e,c=self.coverage();plan.verify_coverage(e,c,[],False)

    def test_missing_opportunity_rejected(self):
        e,c=self.coverage();c['opportunities'].pop()
        with self.assertRaisesRegex(ValueError,'Missing opportunity'):plan.verify_coverage(e,c,[],False)

    def test_missing_legal_edit_rejected(self):
        e,c=self.coverage();c['opportunities'][0]['legalEdits'].pop()
        with self.assertRaisesRegex(ValueError,'complete legal'):plan.verify_coverage(e,c,[],False)

    def test_wrong_protection_rejected(self):
        e,c=self.coverage();c['opportunities'][0]['protectedEssences'].append('c')
        with self.assertRaisesRegex(ValueError,'complete legal'):plan.verify_coverage(e,c,[],False)

    def test_duplicate_opportunity_rejected(self):
        e,c=self.coverage();c['opportunities'][1]=copy.deepcopy(c['opportunities'][0])
        with self.assertRaisesRegex(ValueError,'complete legal'):plan.verify_coverage(e,c,[],False)

    def test_pair_route_drift_rejected(self):
        e,c=self.coverage();c['pairs'][0]['affinityIds']=[]
        with self.assertRaisesRegex(ValueError,'authored pairs'):plan.verify_coverage(e,c,[],False)

    def test_provider_provenance_drift_rejected(self):
        e,c=self.coverage()
        for r in c['opportunities']:r['alliedActionProtections']=[{'essenceId':'forged'}]
        with self.assertRaisesRegex(ValueError,'provider protection'):plan.verify_coverage(e,c,[],True)

    def test_scenario_changes_only_composition_and_seed_panel(self):
        anchor=dict(seeds=[123],party=[dict(partySlot=1,build=dict(essenceIds=['old'],equipment=['fixed'],identityEssenceIds=['neutral']))],floorNumber=5)
        original=copy.deepcopy(anchor)
        result=plan.scenario_for(anchor,{'1':['new']})
        self.assertEqual(original,anchor)
        self.assertEqual([],result['seeds'])
        self.assertEqual(['fixed'],result['party'][0]['build']['equipment'])
        self.assertEqual(['neutral'],result['party'][0]['build']['identityEssenceIds'])


class StatisticalTests(unittest.TestCase):
    def setUp(self):
        self.plan=statistical_fixture()
        self.rows={t['partyId']:[False]*plan.SAMPLES for t in self.plan['teams']}

    def test_family_counts_coefficients_and_cancellation(self):
        endpoints=self.plan['statisticalEndpoints']
        self.assertEqual(46,len(endpoints))
        delta=endpoints[-1]
        self.assertNotIn('reference-0',delta['coefficients'])
        coeffs=[Fraction(x['numerator'],x['denominator']) for x in delta['coefficients'].values()]
        self.assertEqual(0,sum(coeffs))
        self.assertEqual(Fraction(15,41),sum(c for c in coeffs if c>0))
        self.assertAlmostEqual(15/41,delta['maximum'])
        self.assertAlmostEqual(30/41,delta['maximum']-delta['minimum'])

    def test_family_bound_matches_closed_form_and_tail_budget(self):
        row=self.plan['statisticalEndpoints'][0]
        self.assertAlmostEqual(math.sqrt(2*math.log(1840)/2048),row['simultaneousHalfWidth'])
        tail=math.exp(-2*plan.SAMPLES*(row['simultaneousHalfWidth']/2)**2)
        self.assertAlmostEqual(.05,2*46*tail)

    def test_benchmark_alone_cannot_trigger_discovery(self):
        self.rows['reference-1']=[True]*plan.SAMPLES
        result=plan.outcome_summary(self.plan,self.rows)
        self.assertEqual('RetireUnresolvedAtBudget',result['status'])
        self.assertEqual([],result['eligiblePartyIds'])

    def test_all_reliably_worse_retires_below_threshold(self):
        self.rows['reference-0']=[True]*plan.SAMPLES
        self.assertEqual('RetireBelowPracticalThreshold',plan.outcome_summary(self.plan,self.rows)['status'])

    def test_all_equal_stops_unresolved_without_absence_claim(self):
        result=plan.outcome_summary(self.plan,self.rows)
        self.assertEqual('RetireUnresolvedAtBudget',result['status'])
        self.assertFalse(result['promoted']);self.assertEqual(0,result['additionalSamples'])

    def test_all_qualifiers_including_control_only_are_reported(self):
        for pid in ('recipe-00','recipe-40'):self.rows[pid]=[True]*plan.SAMPLES
        result=plan.outcome_summary(self.plan,self.rows)
        self.assertEqual('FreshConfirmationWarranted',result['status'])
        self.assertEqual(['recipe-00','recipe-40'],result['eligiblePartyIds'])

    def test_integer_win_boundary_around_practical_threshold(self):
        radius=self.plan['statisticalEndpoints'][0]['simultaneousHalfWidth']
        wins=math.ceil((.03+radius)*plan.SAMPLES)
        for count,expected in ((wins-1,False),(wins,True)):
            self.rows['recipe-00']=[True]*count+[False]*(plan.SAMPLES-count)
            actual=plan.outcome_summary(self.plan,self.rows)
            self.assertEqual(expected,'recipe-00' in actual['eligiblePartyIds'])

    def test_group_means_use_distinct_recipes_not_derivations(self):
        self.rows['recipe-00']=[True]*plan.SAMPLES
        result=plan.outcome_summary(self.plan,self.rows)
        endpoints={e['id']:e for e in result['endpoints']}
        self.assertAlmostEqual(1/41,endpoints['controlMean']['mean'])
        self.assertAlmostEqual(1/26,endpoints['candidateMean']['mean'])
        self.assertAlmostEqual(1/26-1/41,endpoints['candidateMinusControlMean']['mean'])

    def test_missing_or_extra_team_rejected(self):
        del self.rows['recipe-00']
        with self.assertRaisesRegex(ValueError,'Incomplete'):plan.outcome_summary(self.plan,self.rows)
        self.rows['unknown']=[False]*plan.SAMPLES
        with self.assertRaisesRegex(ValueError,'Incomplete'):plan.outcome_summary(self.plan,self.rows)

    def test_incomplete_and_nonbinary_rows_rejected(self):
        for row in ([False]*(plan.SAMPLES-1),[0]*plan.SAMPLES,[float('nan')]*plan.SAMPLES):
            self.rows['recipe-00']=row
            with self.assertRaisesRegex(ValueError,'Incomplete'):plan.outcome_summary(self.plan,self.rows)

    def test_changed_threshold_or_interval_family_rejected(self):
        p=copy.deepcopy(self.plan);p['decision']['minimumPracticalGain']['numerator']=2
        with self.assertRaisesRegex(ValueError,'contract'):plan.outcome_summary(p,self.rows)
        p=copy.deepcopy(self.plan);p['statisticalEndpoints'].pop()
        with self.assertRaisesRegex(ValueError,'contract'):plan.outcome_summary(p,self.rows)

    def test_generated_benchmark_or_unknown_membership_rejected(self):
        with self.assertRaisesRegex(ValueError,'statistical family'):plan.coefficient_sets(self.plan['teams'],'recipe-00')
        self.plan['teams'][-1]['membership']='other'
        with self.assertRaisesRegex(ValueError,'statistical family'):plan.coefficient_sets(self.plan['teams'],'reference-0')

    def test_pairing_dependence_needs_no_independent_team_assumption(self):
        alternating=[bool(i%2) for i in range(plan.SAMPLES)]
        self.rows={pid:alternating.copy() for pid in self.rows}
        result=plan.outcome_summary(self.plan,self.rows)
        self.assertTrue(all(e['mean']==0 for e in result['endpoints']))
        self.assertTrue(all(e['lower']<=0<=e['upper'] for e in result['endpoints']))


class EvidenceTests(unittest.TestCase):
    def test_json_duplicate_and_nonfinite_rejected(self):
        for raw in ('{"x":1,"x":2}','{"x":NaN}','{"x":Infinity}'):
            with self.assertRaises(ValueError):plan.decode(raw)

    def test_manifest_authentication_traversal_and_mutation(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);(root/'row.json').write_bytes(b'{}')
            plan.save(root/'files.json',{'row.json':plan.sha(b'{}')})
            pin=plan.sha((root/'files.json').read_bytes())
            with self.assertRaisesRegex(ValueError,'manifest'):plan.Evidence(root,'0'*64)
            evidence=plan.Evidence(root,pin);self.assertEqual({},evidence.read('row.json'))
            with self.assertRaisesRegex(ValueError,'Unbound'):evidence.read('../row.json')
            (root/'row.json').write_bytes(b'[]')
            with self.assertRaisesRegex(ValueError,'consumed'):evidence.read('row.json')
            with self.assertRaisesRegex(ValueError,'during planning'):evidence.recheck()

    def test_publish_refuses_existing_output_before_processing(self):
        with tempfile.TemporaryDirectory() as tmp,patch.object(plan,'load_plan',side_effect=AssertionError('should not run')):
            with self.assertRaisesRegex(ValueError,'no retry'):plan.publish(Path(tmp))

    def test_save_cannot_overwrite_a_frozen_plan(self):
        with tempfile.TemporaryDirectory() as tmp:
            path=Path(tmp)/'plan.json';plan.save(path,{'original':True})
            with self.assertRaises(FileExistsError):plan.save(path,{'original':False})
            self.assertTrue(json.loads(path.read_bytes())['original'])


if __name__=='__main__':
    unittest.main(verbosity=2)
