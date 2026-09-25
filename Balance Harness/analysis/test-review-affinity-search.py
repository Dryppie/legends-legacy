"""Literal published receipts: review tests never load a runtime or run combat."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('affinity_review', Path(__file__).with_name('review-affinity-search.py'))
review = importlib.util.module_from_spec(spec)
spec.loader.exec_module(review)


class ReviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='affinity-review-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'source'; self.source.mkdir()
        self.output = self.root / 'review'
        self.version = 'tower-affinity-nomination-comparison-v1'
        self.benchmark, self.challenger = 'b'*64, 'c'*64
        policy = dict(version='tower-proposal-policy-v3', name='benchmark-affinity-creation-v3',
            firstWave=['affinity-create']*9, secondWave=['affinity-create']*8, parentTickets=['benchmark'],
            preserveParentInteractions=False, preservedDamageAffinityIds=[], createdDamageAffinityIds=['a'*64])
        self.put('source/plan.json', dict(version=self.version, control=policy, candidate=policy,
            selectionContrast=dict(control='tower-racing-benchmark-validation-v1', candidate='tower-racing-benchmark-validation-v1'),
            roots=12, heldoutSamples=256, benchmarkPartyId=self.benchmark))
        self.put('source/context.json', dict(scope=dict(requiredPartySize=2), mechanics=dict(essences=[
            dict(id='essence.z', name='Z Essence'), dict(id='essence.a', name='A Essence')])))
        roots, families, gates = [], [], []
        for n in range(1, 13):
            chosen = self.challenger if n == 1 else self.benchmark
            seeds = list(range(n*256, (n+1)*256))
            parties = {self.benchmark: ['candidate', 'benchmark'], self.challenger: ['control']} if n == 1 else {self.benchmark: ['control','candidate','benchmark']}
            members = []
            for pid, roles in parties.items():
                essences = ['essence.z','essence.a'] if pid == self.benchmark else ['essence.z']
                actors = [dict(partySlot=i, build=dict(characterLevel=40, essenceIds=essences[:],
                    equipment=[dict(slot='MainHand',definitionId='fixed.weapon')], identityEssenceIds=['identity'])) for i in (1,2)]
                scenario = dict(id='literal', floorNumber=5, seeds=seeds, preparationState='uncleared', assumptions=['fixed context'], party=actors)
                members.append(dict(party=dict(id=pid, builds={str(i):essences[:] for i in (1,2)}), scenario=scenario, roles=roles))
            families.append(dict(root=n, seeds=seeds, members=members))
            roots.append(dict(root=n,controlParty=chosen,candidateParty=self.benchmark,benchmarkParty=self.benchmark,
                controlWins=190,candidateWins=180,benchmarkWins=180))
            gates.append(dict(selectedId=chosen,passed=n==1,samples=60,gainedWins=5 if n==1 else 0,lostWins=0))
        metric = dict(mean=0,lower=0,upper=0)
        result = dict(version=self.version,status='Verified',decision='Inconclusive',planHash='a'*64,
            fights=16000,searchFights=12672,heldoutFights=3328,method=metric,benchmark=metric,differingRoots=1,promisingNovelRoots=0,
            roots=roots,controlValidation=dict(passedRoots=1,fallbackRoots=11,decisions=gates))
        self.put('result.json',result); self.put('native-audit.log',result)
        self.put('study/freeze.json',dict(version=self.version,planHash='a'*64,families=families))
        for name in ('native-process.json','native-audit-process.json','independent-audit-process.json','publication-process.json'):
            self.put(name,dict(exitCode=0,timedOut=False,activeProcesses=0))
        self.repin()

    def put(self, name, obj):
        path=self.source/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(obj),encoding='utf-8')

    def read(self, name): return review.io.read(self.source/name)

    def repin(self):
        request=dict(version=self.version,plan=dict(sha256=review.io.sha(self.source/'source/plan.json')),
            context=dict(sha256=review.io.sha(self.source/'source/context.json')))
        self.put('request.json',request); request_pin=review.io.sha(self.source/'request.json')
        self.put('completion.json',dict(version=self.version,status='Complete',requestFileHash=request_pin,retries=0))
        self.put('independent-audit.json',dict(status='Passed',requestFileHash=request_pin,newFights=0,newValues=0,result=self.read('result.json')))
        self.seal(request_pin)

    def seal(self, request_pin=None):
        if request_pin is None: request_pin=review.io.sha(self.source/'request.json')
        self.put('files.json',{p.relative_to(self.source).as_posix():review.io.sha(p) for p in self.source.rglob('*')
            if p.is_file() and p.name not in ('files.json','closeout.json')})
        self.put('closeout.json',dict(version=self.version,requestHash=request_pin,filesHash=review.io.sha(self.source/'files.json')))
        self.pin=review.io.sha(self.source/'closeout.json')

    def inspect(self): return review.inspect(self.source,self.pin)

    def test_report_preserves_loadouts_context_and_source_and_never_promotes(self):
        before={str(p):review.io.sha(p) for p in self.source.rglob('*') if p.is_file()}
        receipt=review.publish(self.source,self.pin,self.output)
        data=review.io.read(self.output/'review.json')
        self.assertEqual((11,1,0),(data['benchmarkRetainedRoots'],data['provisionalChallengerRoots'],data['teamsConfirmedByThisStudy']))
        self.assertEqual(['essence.z','essence.a'],[e['id'] for e in data['teams'][0]['loadout'][0]['essences']])
        self.assertEqual([],data['teams'][0]['scenario']['seeds'])
        original=self.read('study/freeze.json')['families'][0]['members'][0]['scenario'];original['seeds']=[]
        self.assertEqual(original,data['teams'][0]['scenario'])
        self.assertEqual([2,2],[e['copies'] for e in data['teams'][0]['requiredCopies']])
        self.assertEqual(before,{str(p):review.io.sha(p) for p in self.source.rglob('*') if p.is_file()})
        self.assertEqual((0,0,False),(receipt['newFights'],receipt['newValues'],receipt['fullReconstruction']))
        text=(self.output/'report.md').read_text(encoding='utf-8')
        self.assertIn('Needs confirmation',text);self.assertIn('Z Essence; A Essence',text)
        self.assertIn('does not reconstruct the entire archive',text)

    def test_wrong_pin_and_changed_consumed_file_are_rejected(self):
        with self.assertRaisesRegex(ValueError,'Changed'):review.inspect(self.source,'0'*64)
        self.put('source/context.json',{})
        with self.assertRaisesRegex(ValueError,'Changed'):self.inspect()

    def test_supported_arm_is_selected_by_comparison_version(self):
        original_result=self.read('result.json');original_freeze=self.read('study/freeze.json')
        for version, arm in review.SUPPORTED_ARMS.items():
            with self.subTest(version=version):
                self.version=version
                plan=self.read('source/plan.json');plan['version']=version;self.put('source/plan.json',plan)
                result=copy.deepcopy(original_result);result['version']=version
                freeze=copy.deepcopy(original_freeze);freeze['version']=version
                if arm=='candidate':
                    result['validation']=result.pop('controlValidation')
                    for row in result['roots']:
                        row['controlParty'],row['candidateParty']=row['candidateParty'],row['controlParty']
                        row['controlWins'],row['candidateWins']=row['candidateWins'],row['controlWins']
                    for family in freeze['families']:
                        for member in family['members']:
                            member['roles']=[{'control':'candidate','candidate':'control'}.get(r,r) for r in member['roles']]
                self.put('result.json',result);self.put('native-audit.log',result)
                self.put('study/freeze.json',freeze);self.repin()
                data,_=self.inspect()
                self.assertEqual(arm,data['supportedArm'])
                self.assertEqual(self.challenger,data['roots'][0]['selectedId'])
                self.assertEqual(190,data['roots'][0]['selectedWins'])
                self.assertEqual((11,1),(data['benchmarkRetainedRoots'],data['provisionalChallengerRoots']))

    def test_resealed_manifest_cannot_replace_external_pin(self):
        pin=self.pin;self.put('native-audit.log',{});self.seal()
        with self.assertRaisesRegex(ValueError,'Changed'):review.inspect(self.source,pin)

    def test_audits_must_agree_even_with_a_new_trusted_pin(self):
        self.put('native-audit.log',{})
        self.seal()
        with self.assertRaisesRegex(ValueError,'native audit'):self.inspect()
        self.put('native-audit.log',self.read('result.json'))
        self.put('independent-audit.json',dict(status='Passed',result={}))
        self.seal()
        with self.assertRaisesRegex(ValueError,'independent audit'):self.inspect()

    def test_failure_incomplete_publication_and_running_process_are_rejected(self):
        for name, changed in [('completion.json',dict(status='Incomplete')),
                              ('native-audit-process.json',dict(activeProcesses=1)),
                              ('publication-process.json',dict(timedOut=True))]:
            saved=self.read(name);self.put(name,dict(saved,**changed));self.seal()
            with self.assertRaises(ValueError):self.inspect()
            self.put(name,saved)
        self.put('failure.json',{});self.seal()
        with self.assertRaisesRegex(ValueError,'Failed studies'):self.inspect()

    def test_unsupported_profile_and_incomplete_roots_are_rejected(self):
        plan=self.read('source/plan.json');bad=copy.deepcopy(plan);bad['control']['parentTickets']=['beam']
        self.put('source/plan.json',bad);self.repin()
        with self.assertRaisesRegex(ValueError,'supported search profile'):self.inspect()
        self.put('source/plan.json',plan);self.repin()
        freeze=self.read('study/freeze.json');freeze['families'].pop();self.put('study/freeze.json',freeze);self.seal()
        with self.assertRaisesRegex(ValueError,'root coverage'):self.inspect()

    def test_fallback_cannot_show_a_challenger_and_totals_cannot_change(self):
        original=self.read('result.json')
        for mutate in (lambda r:r['controlValidation']['decisions'][0].update(passed=False),
                       lambda r:r['controlValidation'].update(passedRoots=12)):
            result=copy.deepcopy(original);mutate(result)
            self.put('result.json',result);self.put('native-audit.log',result);self.repin()
            with self.assertRaisesRegex(ValueError,'validation'):self.inspect()

    def test_context_cannot_vary_under_the_same_party_id(self):
        freeze=self.read('study/freeze.json');freeze['families'][1]['members'][0]['scenario']['party'][0]['build']['equipment']=[]
        self.put('study/freeze.json',freeze);self.seal()
        with self.assertRaisesRegex(ValueError,'different equipment'):self.inspect()

    def test_unsafe_member_and_conflicting_request_binding_are_rejected(self):
        manifest=self.read('files.json');manifest['../outside']='a'*64;self.put('files.json',manifest)
        closeout=self.read('closeout.json');closeout['filesHash']=review.io.sha(self.source/'files.json');self.put('closeout.json',closeout)
        self.pin=review.io.sha(self.source/'closeout.json')
        with self.assertRaisesRegex(ValueError,'Unsafe relative member'):self.inspect()
        self.seal();manifest=self.read('files.json');manifest['request.json']='a'*64;self.put('files.json',manifest)
        closeout['filesHash']=review.io.sha(self.source/'files.json');self.put('closeout.json',closeout);self.pin=review.io.sha(self.source/'closeout.json')
        with self.assertRaisesRegex(ValueError,'Conflicting evidence'):self.inspect()

    def test_existing_and_overlapping_outputs_are_rejected_without_changes(self):
        for output in (self.source/'review',self.source,self.root):
            with self.assertRaises(ValueError):review.publish(self.source,self.pin,output)
        self.output.mkdir();(self.output/'keep').write_text('existing')
        with self.assertRaises(ValueError):review.publish(self.source,self.pin,self.output)
        self.assertEqual('existing',(self.output/'keep').read_text())

    def test_mid_publication_mutation_leaves_no_completion(self):
        render=review.markdown
        def change(data):
            text=render(data);self.put('source/context.json',{});return text
        with patch.object(review,'markdown',side_effect=change):
            with self.assertRaisesRegex(ValueError,'Source changed'):review.publish(self.source,self.pin,self.output)
        self.assertFalse((self.output/'completion.json').exists());self.assertFalse((self.output/'files.json').exists())

    def test_unconsumed_battles_are_not_read_or_claimed_reconstructed(self):
        self.put('battles/not-consumed.json',dict(not_a_battle=True));self.seal()
        data, consumed=self.inspect()
        self.assertNotIn('battles/not-consumed.json',consumed)
        self.assertEqual('Consumed published files only; full reconstruction not repeated.',data['verificationScope'])

    def test_markdown_escapes_names_and_preserves_unknown_ids(self):
        context=self.read('source/context.json');context['mechanics']['essences']=[dict(id='essence.z',name='<img>|[link](x)\n*name*')]
        self.put('source/context.json',context);self.repin()
        data,_=self.inspect();text=review.markdown(data)
        self.assertIn('&lt;img&gt;\\|\\[link\\]\\(x\\) \\*name\\*',text)
        self.assertIn('essence.a',text);self.assertNotIn('<img>',text)


if __name__=='__main__':unittest.main()
