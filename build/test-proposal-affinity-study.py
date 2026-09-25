"""Literal archive and failure tests for the proposal study. Never launches a campaign."""
import argparse
import copy
import datetime as dt
import gzip
import importlib.util
import json
from pathlib import Path
import shutil
import struct
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path); value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


audit=module('proposal_audit',ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py')
owner=module('proposal_owner',ROOT/'build/run-proposal-affinity-study.py')


def write(path,value):
    path.write_text(json.dumps(value,indent=2),encoding='utf-8')


def seal(root):
    write(root/'files.json',{p.relative_to(root).as_posix():audit.sha(p) for p in audit.paths(root) if p!=root/'files.json'})


def prepare(root):
    if (root/'request.json').exists():
        return
    (root/'source').mkdir()
    for name in ('plan','context','settings','history'):
        shutil.copyfile(root/('fixture-'+name+'.json'),root/'source'/(name+'.json'))
    write(root/'source/runtime.json',{})
    shutil.copyfile(ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py',root/'auditor.py')
    q=dict(version=audit.read(root/'source/plan.json')['version'])
    for name in ('plan','context','settings','history','runtime'):
        q[name]=dict(path=str(root/'source'/(name+'.json')),sha256=audit.sha(root/'source'/(name+'.json')))
    q['auditor']=dict(path=str(root/'auditor.py'),sha256=audit.sha(root/'auditor.py'))
    if (root/'evidence-storage.json').exists():
        q['evidenceStorage'] = audit.read(root/'evidence-storage.json')['selection']
        for name in ('proposal_evidence_codec.py','proposal_evidence_storage.py'):
            shutil.copyfile(ROOT/'Balance Harness/analysis'/name,root/name)
    if q['version'] in (audit.ALLIED_VERSION, audit.PLACEMENT_VERSION):
        q['resourceEnvelope'] = owner.RESOURCE_V2
    write(root/'request.json',q)
    if q['version'] in (audit.ALLIED_VERSION, audit.PLACEMENT_VERSION):
        # Synthetic wrapper for saved-row tests only; the separate owned fixture
        # exercises an actual process tree and current-runtime admission boundary.
        start = dt.datetime(2026,9,24,tzinfo=dt.timezone.utc)
        write(root/'launch.json',dict(version=q['version'],requestFileHash=audit.sha(root/'request.json'),
            startedAt=start.isoformat(),nativeDeadline=(start+dt.timedelta(seconds=9000)).isoformat(),
            deadline=(start+dt.timedelta(seconds=10800)).isoformat(),maximumSeconds=10800,
            maximumBytes=6442450944,nativeMaximumSeconds=9000,nativeMaximumBytes=5905580032,fixtureOnly=True))
        write(root/'admission-receipt.json',dict(version=q['version'],resourceEnvelope=owner.RESOURCE_V2,fixtureOnly=True))


class ArithmeticTests(unittest.TestCase):
    def test_allied_action_design_keeps_paired_layout_and_exact_v4_v5_contrast(self):
        frozen=audit.read(ROOT/'Balance Harness/Tower-Affinity-Allied-Action-Comparison-Design.json')['plannedNativePlan']
        audit.validate_policies(frozen)
        allocation=audit.classify(struct.pack('<16384i',*range(200000,216384)),[],audit.ALLIED_VERSION)
        self.assertEqual((4380,16384),(len(allocation['selected']),len(allocation['reserved'])))
        for edit in (lambda d:d.update(control=d['candidate']),lambda d:d.update(candidate=d['control']),
                     lambda d:d['candidate'].update(creationRemovalRule=audit.REMOVAL_RULE),
                     lambda d:d['candidate'].update(createdDamageAffinityIds=['a'*64]),
                     lambda d:d['validationProtocol'].update(valuesPerPair=133)):
            bad=copy.deepcopy(frozen);edit(bad)
            with self.assertRaises(ValueError):audit.validate_policies(bad)
        for args in [(0,0,0,0),(.02,0,3,0),(.1,-.02,12,12)]:
            self.assertEqual(audit.decision(*args,audit.PRESERVATION_VERSION),audit.decision(*args,audit.ALLIED_VERSION))

    def test_allied_action_requires_v2_in_both_launcher_and_independent_auditor(self):
        with tempfile.TemporaryDirectory() as temp:
            for envelope in (None,owner.RESOURCE_V1,'unknown'):
                q=dict(version=audit.ALLIED_VERSION,resourceEnvelope=envelope)
                with self.assertRaisesRegex(ValueError,'frozen v2'):owner.resources(q)
                with self.assertRaisesRegex(ValueError,'frozen v2'):audit.audit_resources(Path(temp),q,True)
        self.assertEqual(dict(version=owner.RESOURCE_V2,nativeSeconds=9000,auditSeconds=1800),
            owner.resources(dict(version=audit.ALLIED_VERSION,resourceEnvelope=owner.RESOURCE_V2)))
        self.assertEqual(9600,owner.resources(dict(version=audit.PRESERVATION_VERSION))['nativeSeconds'])

    def test_allied_action_provider_derivation_uses_direct_equipped_effects(self):
        effect=dict(id='order',operation='PerformBasicAttack',target='NonSummonedAllies',chancePercent=35)
        ability=dict(id='shared',owningEssenceId='different-base',effects=[effect])
        inventory=dict(essences=[dict(id='provider',abilityIds=['shared','shared'])],nodes=[
            dict(key='Ability:shared',id='shared',kind='Ability',unknowns=[],definition=ability),
            dict(key='Effect:Ability:shared/order',id='qualified',kind='Effect',unknowns=[],definition=copy.deepcopy(effect))])
        expected=[dict(essenceId='provider',abilityNodeKey='Ability:shared',effectNodeKey='Effect:Ability:shared/order',
            operation='PerformBasicAttack',target='NonSummonedAllies')]
        self.assertEqual(expected,audit.allied_providers(inventory))
        for edit in (lambda d:d['nodes'].pop(), lambda d:d['nodes'].append(d['nodes'][0]),
                     lambda d:d['nodes'][1]['unknowns'].append('unknown'),
                     lambda d:d['nodes'][1]['definition'].update(chancePercent=100)):
            bad=copy.deepcopy(inventory);edit(bad)
            with self.assertRaises(ValueError):audit.allied_providers(bad)
        for node in inventory['nodes']:
            (node['definition']['effects'][0] if node['kind']=='Ability' else node['definition'])['target']='Self'
        self.assertEqual([],audit.allied_providers(inventory))

    def test_allied_action_metadata_requires_complete_equipped_reasons(self):
        parent=dict(builds={'1':['a','b','c']})
        policy=dict(version=audit.ALLIED_POLICY,createdDamageAffinityIds=['a'*64])
        provider=dict(essenceId='b',abilityNodeKey='Ability:order',effectNodeKey='Effect:Ability:order/effect',
            operation='PerformBasicAttack',target='NonSummonedAllies')
        p=dict(scheduledOwners=[1],changedOwners=[1],fallback=None,interaction=None,party=dict(builds={'1':['a','b','d']}),
            constructionChecks=1,replacementDistance=1,rejection=None,affinityCreation=dict(pairId=audit.digest(['a','d']),
                targetAffinityIds=['a'*64],newlyActivatedAffinityIds=['a'*64],removed=['c'],added=['d'],
                removalSelection=dict(rule=audit.ALLIED_RULE,protectedEssences=['a','b'],eligiblePairs=1,eligibleEdits=1,
                    alliedActionProtections=[dict(provider,effectDefinitionHash='f'*64)])))
        audit.creation_step(p,parent,policy,[provider])
        for edit in (lambda d:d.update(alliedActionProtections=[]),lambda d:d.update(protectedEssences=['a']),
                     lambda d:d['alliedActionProtections'][0].update(target='Self'),
                     lambda d:d['alliedActionProtections'][0].update(effectDefinitionHash='invalid'),
                     lambda d:d.update(rule=audit.REMOVAL_RULE)):
            bad=copy.deepcopy(p);edit(bad['affinityCreation']['removalSelection'])
            with self.assertRaises(ValueError):audit.creation_step(bad,parent,policy,[provider])
        with self.assertRaises(ValueError):audit.creation_step(p,parent,policy)

    def test_preservation_allocation_and_fixed_selector_design(self):
        values=audit.classify(struct.pack('<16384i',*range(200000,216384)),[],audit.PRESERVATION_VERSION)
        self.assertEqual((4380,16384),(len(values['selected']),len(values['reserved'])))
        self.assertEqual(list(range(200000,204380)),values['selected'])
        control=dict(version=audit.CREATION_POLICY,name='benchmark-affinity-creation-v3',firstWave=['affinity-create']*9,
            secondWave=['affinity-create']*8,parentTickets=['benchmark'],preserveParentInteractions=False,
            preservedDamageAffinityIds=[],createdDamageAffinityIds=['a'*64])
        candidate=dict(control,version=audit.PRESERVATION_POLICY,name='benchmark-preserving-affinity-creation-v4',creationRemovalRule=audit.REMOVAL_RULE)
        design=dict(version=audit.PRESERVATION_VERSION,control=control,candidate=candidate,
            selectionContrast=dict(control=audit.VALIDATION_POLICY,candidate=audit.VALIDATION_POLICY),validationProtocol=audit.preservation_protocol())
        audit.validate_policies(design)
        for edit in (lambda d:d['control'].update(version=audit.PRESERVATION_POLICY),
                     lambda d:d['candidate'].update(createdDamageAffinityIds=['b'*64]),
                     lambda d:d['candidate'].update(creationRemovalRule='relax'),
                     lambda d:d['selectionContrast'].update(control=audit.SELECTOR_POLICY),
                     lambda d:d['validationProtocol'].update(valuesPerPair=133)):
            bad=copy.deepcopy(design);edit(bad)
            with self.assertRaises(ValueError):audit.validate_policies(bad)
        for x in [(0,0,0,0),(.02,0,3,0),(.02,-.02,12,12)]:
            self.assertEqual(audit.decision(*x,audit.VALIDATION_VERSION),audit.decision(*x,audit.PRESERVATION_VERSION))

    def test_preservation_metadata_rejects_unprotecting_and_false_eligibility(self):
        parent=dict(builds={'1':['a','b','c']})
        policy=dict(version=audit.PRESERVATION_POLICY,createdDamageAffinityIds=['a'*64])
        p=dict(scheduledOwners=[1],changedOwners=[1],fallback=None,interaction=None,party=dict(builds={'1':['a','b','d']}),
            constructionChecks=1,replacementDistance=1,rejection=None,affinityCreation=dict(pairId=audit.digest(['a','d']),
                targetAffinityIds=['a'*64],newlyActivatedAffinityIds=['a'*64],removed=['c'],added=['d'],
                removalSelection=dict(rule=audit.REMOVAL_RULE,protectedEssences=['a','b'],eligiblePairs=1,eligibleEdits=1)))
        audit.creation_step(p,parent,policy)
        for edit in (lambda d:d.update(protectedEssences=['a','b','c']),lambda d:d.update(eligiblePairs=0),
                     lambda d:d.update(eligibleEdits=2),lambda d:d.update(eligiblePairs=True),lambda d:d.update(rule='unknown')):
            bad=copy.deepcopy(p);edit(bad['affinityCreation']['removalSelection'])
            with self.assertRaises(ValueError):audit.creation_step(bad,parent,policy)
        with self.assertRaises(ValueError):audit.creation_step(p,parent,dict(policy,version=audit.CREATION_POLICY))

    def test_validation_allocation_has_4668_selected_and_preserves_the_full_tail(self):
        entropy=struct.pack('<16384i',*range(200000,216384))
        allocation=audit.classify(entropy,[],audit.VALIDATION_VERSION)
        self.assertEqual((4668,16384),(len(allocation['selected']),len(allocation['reserved'])))
        self.assertEqual(list(range(200000,204668)),allocation['selected'])
        self.assertEqual(list(range(200000,216384)),allocation['reserved'])
        for method,benchmark,differing,expected in [(0,0,0,'NoObservedOutputDifferentiation'),(.02,0,3,'LargerFreshEvaluationWarranted'),
            (.02,-.02,12,'AbandonThisConfiguration'),(.019,0,3,'Inconclusive')]:
            self.assertEqual(expected,audit.decision(method,benchmark,differing,0,audit.VALIDATION_VERSION))

    def test_validation_design_rejects_changed_control_and_allocation_protocol(self):
        policy=dict(version=audit.CREATION_POLICY,name='benchmark-affinity-creation-v3',firstWave=['affinity-create']*9,
            secondWave=['affinity-create']*8,parentTickets=['benchmark'],preserveParentInteractions=False,
            preservedDamageAffinityIds=[],createdDamageAffinityIds=['a'*64])
        design=dict(version=audit.VALIDATION_VERSION,control=policy,candidate=copy.deepcopy(policy),
            selectionContrast=dict(control=audit.SELECTOR_POLICY,candidate=audit.VALIDATION_POLICY),validationProtocol=audit.validation_protocol())
        audit.validate_policies(design)
        for edit in (lambda d:d['selectionContrast'].update(control='tower-staged-incumbent-tie-v1'),
                     lambda d:d['validationProtocol'].update(valuesPerPair=109),lambda d:d.update(validationProtocol=None),
                     lambda d:d.update(version=audit.SELECTOR_VERSION)):
            bad=copy.deepcopy(design);edit(bad)
            with self.assertRaises(ValueError):audit.validate_policies(bad)

    def test_validation_exact_gate_all_count_pairs(self):
        frozen=dict(version=audit.VALIDATION_POLICY,planHash='a'*64,nominationPanelHash='b'*64,challengerId='c'*64,benchmarkId='d'*64)
        pascal=[1]
        for n in range(61):
            for gains in range(n+1):
                losses=n-gains
                def row(seed,won):return dict(request=dict(seed=seed,panelHash='e'*64),outcome=dict(outcome='Victory' if won else 'Draw'))
                panel=dict(complete=True,freeze=dict(seeds=list(range(60))),observations=
                    [row(i,i<gains) for i in range(60)]+[row(i,gains<=i<n) for i in range(60)])
                decision=audit.validation_decision(frozen,panel);tail=sum(pascal[gains:])
                self.assertEqual((tail,sum(pascal),gains>losses and 20*tail<=sum(pascal)),
                    (decision['tailNumerator'],decision['tailDenominator'],decision['passed']))
            pascal=[1]+[x+y for x,y in zip(pascal,pascal[1:])]+[1]

    def test_decision_boundaries(self):
        for args,expected in [((.02,0,3,3),'LargerFreshEvaluationWarranted'),((.019,0,3,3),'Inconclusive'),
            ((0,0,0,0),'NoObservedOutputDifferentiation'),((-.02,.1,3,3),'AbandonThisConfiguration'),
            ((.1,-.02,3,2),'AbandonThisConfiguration'),((.1,-.02,3,3),'Inconclusive')]:
            with self.subTest(args=args): self.assertEqual(expected,audit.decision(*args))

    def test_all_fresh_tail_is_reserved(self):
        values=list(range(200000,216384)); values[0]=17; values[1]=values[2]
        result=audit.classify(struct.pack('<16384i',*values),[17])
        self.assertEqual((3948,16382,1,1),(len(result['selected']),len(result['reserved']),result['historicalCollisions'],result['duplicates']))
        self.assertIn(216383,result['reserved']); self.assertNotIn(17,result['reserved'])

    def test_creation_allocation_has_its_own_version_with_identical_tail_rules(self):
        entropy=struct.pack('<16384i',*range(200000,216384))
        old=audit.classify(entropy,[]); new=audit.classify(entropy,[],audit.CREATION_VERSION)
        self.assertEqual(dict(old,version=audit.CREATION_VERSION),new)
        with self.assertRaises(ValueError): audit.classify(entropy,[],'unknown')

    def test_selector_allocation_preserves_all_exposed_values(self):
        entropy=struct.pack('<16384i',*range(200000,216384))
        self.assertEqual(dict(audit.classify(entropy,[]),version=audit.SELECTOR_VERSION), audit.classify(entropy,[],audit.SELECTOR_VERSION))

    def test_selector_decision_ignores_novelty_but_keeps_effect_and_benchmark_guards(self):
        for args,expected in [((.02,0,3),'LargerFreshEvaluationWarranted'),((.019,0,3),'Inconclusive'),
            ((.02,0,2),'Inconclusive'),((0,0,0),'NoObservedOutputDifferentiation'),
            ((-.02,.1,3),'AbandonThisConfiguration'),((.1,-.02,3),'AbandonThisConfiguration')]:
            for promising in (0,12): self.assertEqual(expected,audit.decision(*args,promising,audit.SELECTOR_VERSION))

    def test_selector_override_preserves_nonbenchmark_ties_and_zero_win_health(self):
        def scores(b,p,c): return [dict(id=i,wins=w,fitness=dict(guardianHealth=h)) for i,w,h in [('b',b,90),('p',p,10),('c',c,20)]]
        for wins,expected in [((29,29,20),'b'),((29,20,29),'b'),((28,30,30),'p'),((28,29,30),'c'),((0,0,0),'p')]:
            self.assertEqual(expected,audit.select(scores(*wins),['c','p','b'],'p','b'))
        self.assertEqual('p',audit.select(scores(29,29,20),['c','p','b'],'p'))

    def test_selector_design_requires_identical_creation_policies_and_exact_contrast(self):
        policy=dict(version=audit.CREATION_POLICY,name='benchmark-affinity-creation-v3',firstWave=['affinity-create']*9,
            secondWave=['affinity-create']*8,parentTickets=['benchmark'],preserveParentInteractions=False,
            preservedDamageAffinityIds=[],createdDamageAffinityIds=['a'*64])
        design=dict(version=audit.SELECTOR_VERSION,control=policy,candidate=copy.deepcopy(policy),
            selectionContrast=dict(control='tower-staged-incumbent-tie-v1',candidate=audit.SELECTOR_POLICY))
        audit.validate_policies(design)
        for key,value in [('control',dict(policy,name='other')),('selectionContrast',None),('version',audit.CREATION_VERSION)]:
            with self.assertRaises(ValueError): audit.validate_policies(dict(design,**{key:value}))

    def test_selector_trajectory_comparison_ignores_only_evidence_bindings(self):
        a=dict(policyHash='same',batches=[dict(feedbackPanels=['a'],proposals=['same'])],evaluation=dict(decisions=['same'],nominees=['same'],
            panels=[dict(freeze=dict(version='v3',planHash='a',parties=['same']),scores=['same'],contrasts=['same'],complete=True,
                observations=[dict(request=dict(panelHash='a',seed=1),outcome=dict(requestHash='a',outcome='Victory'))])]))
        b=copy.deepcopy(a); b['batches'][0]['feedbackPanels']=['b']; panel=b['evaluation']['panels'][0]
        panel['freeze'].update(version='v4',planHash='b'); panel['observations'][0]['request']['panelHash']='b'
        panel['observations'][0]['outcome']['requestHash']='b'
        audit.selector_trajectories(a,b)
        for target in ('proposals','outcome','nominees'):
            bad=copy.deepcopy(b)
            if target=='proposals':bad['batches'][0]['proposals']=['changed']
            elif target=='nominees':bad['evaluation']['nominees']=['changed']
            else:bad['evaluation']['panels'][0]['observations'][0]['outcome']['outcome']='Defeat'
            with self.assertRaisesRegex(ValueError,'trajectories'):audit.selector_trajectories(a,bad)

    def test_policy_design_cannot_relabel_creation_or_change_its_operator(self):
        control=dict(version=audit.POLICY,name='benchmark-single-control-v2',firstWave=['single']*9,
            secondWave=['single']*8,parentTickets=['benchmark'],preserveParentInteractions=False,preservedDamageAffinityIds=[])
        candidate=dict(control,version=audit.CREATION_POLICY,name='benchmark-affinity-creation-v3',
            firstWave=['affinity-create']*9,secondWave=['affinity-create']*8,createdDamageAffinityIds=['a'*64])
        design=dict(version=audit.CREATION_VERSION,control=control,candidate=candidate)
        audit.validate_policies(design)
        for changed in (dict(design,version=audit.VERSION),dict(design,version='unknown'),
                        dict(design,candidate=dict(candidate,firstWave=['single']*9)),
                        dict(design,candidate=dict(candidate,preservedDamageAffinityIds=['a'*64]))):
            with self.assertRaises(ValueError): audit.validate_policies(changed)

    def test_creation_metadata_requires_a_minimal_same_owner_activation(self):
        parent=dict(builds={'1':['a','b'],'2':['a','b']})
        policy=dict(version=audit.CREATION_POLICY,createdDamageAffinityIds=['c'*64])
        for added,removed,endpoints in ((['c'],['b'],['a','c']),(['c','d'],['a','b'],['c','d'])):
            party=dict(builds={'1':sorted(set(parent['builds']['1'])-set(removed)|set(added)),'2':['a','b']})
            proposal=dict(scheduledOwners=[1],changedOwners=[1],fallback=None,interaction=None,party=party,
                constructionChecks=1,replacementDistance=len(added),rejection=None,
                affinityCreation=dict(pairId=audit.digest(endpoints),targetAffinityIds=['c'*64],
                    newlyActivatedAffinityIds=['c'*64],removed=removed,added=added))
            audit.creation_step(proposal,parent,policy)
            for fault in ('pair','route','owner','removed'):
                bad=copy.deepcopy(proposal)
                if fault=='pair': bad['affinityCreation']['pairId']='f'*64
                elif fault=='route': bad['affinityCreation']['newlyActivatedAffinityIds']=[]
                elif fault=='owner': bad['party']['builds']['2']=['a','c']
                else: bad['affinityCreation']['removed']=[]
                with self.assertRaises(ValueError): audit.creation_step(bad,parent,policy)

    def test_atomic_rename_storage_is_counted_once(self):
        with tempfile.TemporaryDirectory(prefix='proposal-storage-') as temp:
            root=Path(temp); final=root/'allocation.json'; final.write_bytes(b'abcd'); pending=root/'allocation.json.pending'
            with patch.object(owner,'inventory',return_value=[pending,final]): self.assertEqual(4,owner.storage_bytes(root))
            with patch.object(owner,'inventory',return_value=[root/'missing.json']):
                with self.assertRaises(FileNotFoundError): owner.storage_bytes(root)

    def test_existing_output_cannot_retry(self):
        with tempfile.TemporaryDirectory(prefix='proposal-retry-') as temp:
            root=Path(temp); (root/'result').mkdir()
            with self.assertRaisesRegex(ValueError,'retry'): owner.validate_request(dict(version=audit.VERSION,registryRoot=str(root),outputRoot=str(root/'result')))

    def test_native_zero_byte_lease_can_close_during_storage_scan(self):
        with tempfile.TemporaryDirectory(prefix='proposal-lease-') as temp:
            root=Path(temp); lease=root/'search/root-12/candidate/racing.writer.lock'
            with patch.object(owner,'inventory',return_value=[lease]): self.assertEqual(0,owner.storage_bytes(root))
            with patch.object(owner,'inventory',return_value=[root/'search/root-13/candidate/racing.writer.lock']):
                with self.assertRaises(FileNotFoundError): owner.storage_bytes(root)
            lease.parent.mkdir(parents=True); lease.write_bytes(b'not a lease')
            with self.assertRaisesRegex(ValueError,'lease must be empty'): owner.storage_bytes(root)

    def test_admission_rejects_substituted_runtime_and_inputs_even_when_resealed(self):
        with tempfile.TemporaryDirectory(prefix='proposal-admission-') as temp:
            package=Path(temp); (package/'runtime').mkdir(); (package/'content').mkdir()
            harness=package/'runtime/BalanceHarness.dll'; harness.write_bytes(b'literal')
            q=dict(version=audit.VERSION,contentRoot=str(package/'content'))
            for name in ('plan','context','settings','history','runtime','auditor'):
                path=package/(name+'.json'); write(path,dict(version=audit.VERSION) if name=='plan' else {})
                q[name]=dict(path=str(path),sha256=audit.sha(path))
            for name in ('run-proposal-affinity-study.py','bounded_windows_process.py'):
                shutil.copyfile(ROOT/'build'/name,package/name)
            request=package/'request.json'; write(request,q)
            def admit(receipt_version=audit.VERSION):
                write(package/'admission.json',dict(status='ProposalStudyAdmittedNoReservation',version=receipt_version,
                    requestSha256=audit.sha(request),fights=0,newValues=0))
                seal(package); return audit.sha(package/'files.json')
            pin=admit(); owner.validate_admission(request,harness,pin)
            with self.assertRaisesRegex(ValueError,'manifest pin'): owner.validate_admission(request,harness,'0'*64)
            with self.assertRaisesRegex(ValueError,'substituted runtime'): owner.validate_admission(request,package/'BalanceHarness.dll',pin)
            q['version']=audit.CREATION_VERSION; write(request,q); pin=admit()
            with self.assertRaisesRegex(ValueError,'Missing admitted'): owner.validate_admission(request,harness,pin)
            pin=admit(audit.CREATION_VERSION)
            with self.assertRaisesRegex(ValueError,'versions differ'): owner.validate_admission(request,harness,pin)
            q['version']=audit.SELECTOR_VERSION; write(package/'plan.json',dict(version=audit.SELECTOR_VERSION))
            q['plan']['sha256']=audit.sha(package/'plan.json'); write(request,q); pin=admit(audit.SELECTOR_VERSION)
            owner.validate_admission(request,harness,pin)
            pin=admit(audit.CREATION_VERSION)
            with self.assertRaisesRegex(ValueError,'Missing admitted'): owner.validate_admission(request,harness,pin)
            q['version']=audit.VALIDATION_VERSION; write(package/'plan.json',dict(version=audit.VALIDATION_VERSION))
            q['plan']['sha256']=audit.sha(package/'plan.json'); write(request,q)
            pin=admit(audit.SELECTOR_VERSION)
            with self.assertRaisesRegex(ValueError,'Missing admitted'): owner.validate_admission(request,harness,pin)
            pin=admit(audit.VALIDATION_VERSION); owner.validate_admission(request,harness,pin)
            q['plan']['path']=str(package.parent/'outside-plan.json'); write(request,q)
            pin=admit(audit.VALIDATION_VERSION)
            with self.assertRaisesRegex(ValueError,'Source outside'): owner.validate_admission(request,harness,pin)

    @unittest.skipUnless(sys.platform=='win32','Windows Job ownership')
    def test_job_storage_failure_terminates_descendants(self):
        from bounded_windows_process import run
        with tempfile.TemporaryDirectory(prefix='proposal-overflow-') as temp:
            root=Path(temp)
            code="import subprocess,sys,time,pathlib; subprocess.Popen([sys.executable,'-c','import time; time.sleep(60)']); pathlib.Path('overflow').write_bytes(b'x'*4096); time.sleep(60)"
            def check():
                if (root/'overflow').exists(): raise ValueError('literal storage limit')
                return 0
            with self.assertRaisesRegex(ValueError,'literal storage limit'):
                run([sys.executable,'-B','-c',code],str(root),str(root/'log'),time.monotonic()+10,cleanup_seconds=1,check=check)

    @unittest.skipUnless(sys.platform=='win32','Windows Job ownership')
    def test_job_timeout_terminates_descendants(self):
        from bounded_windows_process import run
        with tempfile.TemporaryDirectory(prefix='proposal-job-') as temp:
            root=Path(temp)
            code="import subprocess,sys,time; subprocess.Popen([sys.executable,'-c','import time; time.sleep(60)']); time.sleep(60)"
            receipt=run([sys.executable,'-B','-c',code],str(root),str(root/'log'),time.monotonic()+2,cleanup_seconds=1)
            self.assertTrue(receipt['timedOut']); self.assertEqual(0,receipt['activeProcesses']); self.assertGreaterEqual(receipt['totalProcesses'],2)


class ArchiveTests(unittest.TestCase):
    root=None

    @classmethod
    def setUpClass(cls):
        if cls.root is None: raise unittest.SkipTest('Supply --fixture exported by the backend test')
        prepare(cls.root)

    def test_complete_literal_archive_agrees_with_native(self):
        result=audit.audit(self.root,True)
        self.assertEqual(result['result'],audit.read(self.root/'provisional-result.json'))
        validation=result['result']['version']==audit.VALIDATION_VERSION
        preservation=result['result']['version'] in (audit.PRESERVATION_VERSION,audit.ALLIED_VERSION)
        self.assertEqual(17280 if validation or preservation else 18816,result['result']['fights']); self.assertEqual(0,result['newFights'])
        if validation:
            self.assertEqual((6,6),(result['result']['validation']['passedRoots'],result['result']['validation']['fallbackRoots']))
        if preservation:
            self.assertEqual((6,6),(result['result']['controlValidation']['passedRoots'],result['result']['validation']['passedRoots']))

    def changed(self,path,transform,manifest=None,raw=False):
        original=path.read_bytes(); saved=(manifest/'files.json').read_bytes() if manifest else None
        try:
            if raw: path.write_bytes(transform(original))
            else:
                value=audit.read(path); transform(value); write(path,value)
            if manifest: seal(manifest)
            with self.assertRaises((ValueError,KeyError,IndexError)): audit.audit(self.root,True)
        finally:
            path.write_bytes(original)
            if manifest: (manifest/'files.json').write_bytes(saved)

    def test_resealed_pruning_change_rejected(self):
        arm=self.root/'search/root-01/control'
        self.changed(arm/'racing/search.json',lambda r:r['evaluation']['decisions'][0]['survivorIds'].reverse(),arm)

    def test_resealed_battle_change_rejected(self):
        arm=self.root/'search/root-01/control'
        def change(raw):
            value=json.loads(gzip.decompress(raw)); value['guardianHealthRemainingPercent']=99
            return gzip.compress(json.dumps(value).encode())
        self.changed(arm/'battles/trial-000001.json.gz',change,arm,True)

    def test_resealed_heldout_order_rejected(self):
        path=next((self.root/'study').glob('heldout-01-*.json'))
        self.changed(path,lambda v:v['observations'].reverse(),self.root/'study')

    def test_resealed_freeze_order_rejected(self):
        self.changed(self.root/'study/freeze.json',lambda v:v['families'].reverse(),self.root/'study')

    def test_resealed_endpoint_change_rejected(self):
        self.changed(self.root/'study/summary.json',lambda v:v['method'].update(mean=.5),self.root/'study')

    def test_unused_tail_cannot_be_released(self):
        self.changed(self.root/'seed-ledger.json',lambda v:v['reserved'].pop())

    def test_unmatched_charge_rejected(self):
        self.changed(self.root/'attempts.jsonl',lambda raw:raw.rsplit(b'\n',2)[0]+b'\n',raw=True)

    def test_final_requires_external_pin(self):
        with self.assertRaises(ValueError): audit.audit(self.root)

    def test_study_binding_cannot_cross_versions(self):
        self.changed(self.root/'study/binding.json',lambda v:v.update(version='unknown'),self.root/'study')

    def test_allocation_cannot_cross_versions(self):
        version=audit.read(self.root/'request.json')['version']
        other=audit.CREATION_VERSION if version==audit.VERSION else audit.VERSION
        self.changed(self.root/'allocation.json',lambda v:v.update(version=other))

    def test_creation_metadata_tampering_rejected_after_matching_batch_is_resealed(self):
        if audit.read(self.root/'request.json')['version'] not in (audit.CREATION_VERSION,audit.SELECTOR_VERSION,audit.VALIDATION_VERSION,audit.PRESERVATION_VERSION,audit.ALLIED_VERSION):
            self.skipTest('Creation metadata is absent in the preservation archive')
        arm=self.root/'search/root-01/candidate'
        paths=[arm/'racing/search.json',arm/'racing/batch-01.json',arm/'files.json']
        saved={p:p.read_bytes() for p in paths}
        try:
            report=audit.read(paths[0]); batch=report['batches'][0]
            proposal=next(p for p in batch['proposals'] if p['party'] is not None)
            proposal['affinityCreation']['newlyActivatedAffinityIds']=[]
            write(paths[0],report); write(paths[1],batch); seal(arm)
            with self.assertRaisesRegex(ValueError,'creation route metadata'): audit.audit(self.root,True)
        finally:
            for path,raw in saved.items(): path.write_bytes(raw)

    def test_selector_identity_and_selected_benchmark_cannot_be_relabelled(self):
        if audit.read(self.root/'request.json')['version'] not in (audit.SELECTOR_VERSION,audit.VALIDATION_VERSION,audit.PRESERVATION_VERSION,audit.ALLIED_VERSION):
            self.skipTest('Selector-specific archive')
        arm=self.root/'search/root-01/candidate'
        self.changed(arm/'racing/plan.json',lambda p:p.update(selectionPolicyVersion=None),arm)
        self.changed(arm/'racing/search.json',lambda p:p.update(selectionPolicyVersion=None),arm)
        chosen=audit.read(arm/'racing/search.json')['evaluation']['rawSelectedId']
        primary=next(s['party']['id'] for s in audit.read(self.root/'source/context.json')['scope']['starts'] if s['party']['id']!=chosen)
        self.changed(arm/'racing/search.json',lambda p:p['evaluation'].update(rawSelectedId=primary),arm)

    def test_validation_gate_and_nomination_freeze_cannot_be_resealed(self):
        if audit.read(self.root/'request.json')['version'] not in (audit.VALIDATION_VERSION,audit.PRESERVATION_VERSION,audit.ALLIED_VERSION):
            self.skipTest('Validation-specific archive')
        arm=self.root/'search/root-01/candidate'
        paths=[arm/'racing/search.json',arm/'racing/validation-decision.json',arm/'files.json']
        saved={p:p.read_bytes() for p in paths}
        try:
            report=audit.read(paths[0]);decision=report['evaluation']['validationDecision'];decision['tailNumerator']+=1
            write(paths[0],report);write(paths[1],decision);seal(arm)
            with self.assertRaisesRegex(ValueError,'exact validation gate'):audit.audit(self.root,True)
        finally:
            for path,raw in saved.items():path.write_bytes(raw)
        self.changed(arm/'racing/validation-freeze.json',lambda v:v.update(challengerId=v['benchmarkId']),arm)

    def test_validation_diagnostics_and_legacy_allocation_are_rejected(self):
        if audit.read(self.root/'request.json')['version'] not in (audit.VALIDATION_VERSION,audit.PRESERVATION_VERSION,audit.ALLIED_VERSION):
            self.skipTest('Validation-specific archive')
        self.changed(self.root/'study/summary.json',lambda v:v['validation'].update(fallbackRoots=12),self.root/'study')
        self.changed(self.root/'allocation.json',lambda v:v.update(selected=v['selected'][:3948]))

    def test_preservation_control_gate_and_metadata_cannot_be_resealed(self):
        if audit.read(self.root/'request.json')['version'] not in (audit.PRESERVATION_VERSION,audit.ALLIED_VERSION):
            self.skipTest('Preservation-specific archive')
        self.changed(self.root/'study/summary.json',lambda v:v['controlValidation'].update(passedRoots=0),self.root/'study')
        arm=self.root/'search/root-01/candidate'
        paths=[arm/'racing/search.json',arm/'racing/batch-01.json',arm/'files.json']
        saved={p:p.read_bytes() for p in paths}
        try:
            report=audit.read(paths[0]); batch=report['batches'][0]
            proposal=next(p for p in batch['proposals'] if p['party'] is not None)
            proposal['affinityCreation']['removalSelection']['eligibleEdits']=999
            write(paths[0],report);write(paths[1],batch);seal(arm)
            with self.assertRaisesRegex(ValueError,'preservation metadata'):audit.audit(self.root,True)
        finally:
            for path,raw in saved.items():path.write_bytes(raw)

    def test_allied_action_reasons_cannot_be_removed_from_resealed_batches(self):
        if audit.read(self.root/'request.json')['version'] != audit.ALLIED_VERSION:
            self.skipTest('Allied-action-specific archive')
        arm=self.root/'search/root-01/candidate'
        paths=[arm/'racing/search.json',arm/'racing/batch-01.json',arm/'files.json']
        saved={p:p.read_bytes() for p in paths}
        try:
            report=audit.read(paths[0]);batch=report['batches'][0]
            proposal=next(p for p in batch['proposals'] if p['party'] is not None and
                p['affinityCreation']['removalSelection']['alliedActionProtections'])
            proposal['affinityCreation']['removalSelection']['alliedActionProtections']=[]
            write(paths[0],report);write(paths[1],batch);seal(arm)
            with self.assertRaisesRegex(ValueError,'allied-action protection'):audit.audit(self.root,True)
        finally:
            for path,raw in saved.items():path.write_bytes(raw)


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--fixture',type=Path)
    args,remaining=parser.parse_known_args(); ArchiveTests.root=args.fixture.resolve() if args.fixture else None
    unittest.main(argv=[sys.argv[0],*remaining])
