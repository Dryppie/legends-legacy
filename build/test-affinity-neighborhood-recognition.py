"""Literal neighborhood protocol, independent math and owned failure-path tests. No combat."""
import argparse
import copy
import importlib.util
import io
import json
import math
import os
from pathlib import Path
import struct
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path)
    value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


audit=module('neighborhood_audit',ROOT/'Balance Harness/analysis/audit-affinity-neighborhood-recognition.py')
launcher=module('neighborhood_launcher',ROOT/'build/run-affinity-neighborhood-recognition.py')
oracle=module('frozen_neighborhood_oracle',ROOT/'Balance Harness/analysis/affinity-neighborhood-recognition-plan.py')


class ProtocolTests(unittest.TestCase):
    plan=audit.read(ROOT/'Balance Harness/Tower-Affinity-Neighborhood-Recognition-Plan.json')

    def rows(self,counts=None):
        counts=counts or [1000,1100,1200]+[1100+(i%7)*80 for i in range(3,44)]
        return [dict(partyId=t['partyId'],trials=[dict(seed=n,outcome='Victory' if n<counts[i] else 'Draw')
                for n in range(2048)]) for i,t in enumerate(self.plan['teams'])]

    def test_exact_frozen_family_and_equal_recipe_estimands(self):
        self.assertEqual(audit.PLAN,audit.sha(ROOT/'Balance Harness/Tower-Affinity-Neighborhood-Recognition-Plan.json'))
        rows=self.rows(); result=audit.endpoint(self.plan,rows,list(range(2048)))
        outcome=result['neighborhood']
        expected=oracle.outcome_summary(self.plan,{r['partyId']:[t['outcome']=='Victory' for t in r['trials']] for r in rows})
        self.assertTrue(audit.same(outcome,expected))
        self.assertEqual((44,46),(len(outcome['wins']),len(outcome['endpoints'])))
        self.assertEqual('FreshConfirmationWarranted',outcome['status'])
        self.assertFalse(outcome['promoted']);self.assertEqual(0,outcome['additionalSamples'])
        self.assertEqual(0,result['approximateWilsonFamily'])
        self.assertTrue(all(result[k]==[] for k in ('rates','contrasts','strata','populations','unmeasured')))
        self.assertNotIn('pairedPool',result)
        self.assertAlmostEqual(.08568155551614957,outcome['endpoints'][0]['upper']-outcome['endpoints'][0]['mean'])
        self.assertAlmostEqual(.03134691055468887,outcome['endpoints'][-1]['upper']-outcome['endpoints'][-1]['mean'])

    def test_all_stopping_decisions_and_exact_236_237_threshold(self):
        cases=[('RetireUnresolvedAtBudget',[0]*44,[]),
               ('RetireBelowPracticalThreshold',[0,0,2048]+[0]*41,[]),
               ('RetireUnresolvedAtBudget',[2048,0,0]+[0]*41,[]),
               ('RetireUnresolvedAtBudget',[0,0,0,236]+[0]*40,[]),
               ('FreshConfirmationWarranted',[0,0,0,237]+[0]*39+[237],sorted([self.plan['teams'][i]['partyId'] for i in (3,43)]))]
        for status,counts,eligible in cases:
            with self.subTest(status=status,counts=counts[:4]):
                n=audit.endpoint(self.plan,self.rows(counts),list(range(2048)))['neighborhood']
                self.assertEqual((status,eligible),(n['status'],n['eligiblePartyIds']))

    def test_shared_subset_coefficients_and_benchmark_cancellation(self):
        index=next(i for i,t in enumerate(self.plan['teams']) if t['membership']=='shared')
        counts=[0]*44; counts[index]=2048
        means=[]
        for benchmark in (0,1000,2048):
            counts[2]=benchmark
            n=audit.endpoint(self.plan,self.rows(counts),list(range(2048)))['neighborhood']
            e={x['id']:x for x in n['endpoints']}
            self.assertAlmostEqual(1/41-benchmark/2048,e['controlMean']['mean'])
            self.assertAlmostEqual(1/26-benchmark/2048,e['candidateMean']['mean'])
            means.append(e['candidateMinusControlMean']['mean'])
        self.assertEqual([means[0]]*3,means)
        self.assertAlmostEqual(1/26-1/41,means[0])

    def test_one_finite_family_has_no_root_reuse(self):
        self.assertEqual([0]*44,[audit.root_index(i) for i in range(44)])
        for bad in (-1,44,True,1.5):
            with self.assertRaises(ValueError):audit.root_index(bad)

    def test_entropy_all_exposed_fresh_values_are_permanently_reserved(self):
        values=[-987,-2**31,-2**31]+list(range(16381))
        words,panel,fresh=audit.classify(struct.pack('<16384i',*values),[-987])
        self.assertEqual((2048,16382,14334),(len(panel),len(fresh),sum(w['classification']=='ReservedUnused' for w in words)))
        self.assertEqual(-2**31,panel[0]);self.assertEqual('DuplicateBatch',words[2]['classification'])
        self.assertEqual(1,len(audit.classify(bytes(65536),[-987])[1]))
        for entropy,history in [(bytes(65535),[-987]),(bytes(24576),[-987]),(bytes(65536),[-987,-987]),(bytes(65536),[True])]:
            with self.assertRaises(ValueError):audit.classify(entropy,history)

    def test_incomplete_misordered_or_nonterminal_evidence_fails(self):
        original=self.rows()
        for mode in ('rows','missing','duplicate','fault','identity','boolean-seed','float-seed','truncated-family'):
            with self.subTest(mode=mode):
                rows=copy.deepcopy(original)
                if mode=='rows':rows.reverse()
                if mode=='missing':rows[7]['trials'].pop()
                if mode=='duplicate':rows[7]['trials'][1]=rows[7]['trials'][0]
                if mode=='fault':rows[7]['trials'][0]['outcome']='Faulted'
                if mode=='identity':rows[7]['partyId']=rows[0]['partyId']
                if mode=='boolean-seed':rows[7]['trials'][0]['seed']=False
                if mode=='float-seed':rows[7]['trials'][0]['seed']=0.0
                if mode=='truncated-family':rows.pop()
                with self.assertRaises(ValueError):audit.endpoint(self.plan,rows,list(range(2048)))
        with self.assertRaises(ValueError):audit.endpoint(self.plan,original,list(range(1024))*2)

    def test_ambiguous_json_and_nonfinite_numbers_fail(self):
        for raw in ('{"seed":1,"seed":2}','{"nested":{"value":true,"value":false}}','[NaN]','[Infinity]','[-Infinity]'):
            with self.assertRaises(ValueError):audit.decode(raw)
        self.assertFalse(audit.same(float('nan'),.03))

    def test_exact_nontransferable_phase_caps_reject_legacy_envelopes(self):
        self.assertEqual((18000,18253611008,14400,17179869184,3600,1073741824),
            (launcher.SECONDS,launcher.BYTES,launcher.NATIVE_SECONDS,launcher.NATIVE_BYTES,launcher.AUDIT_SECONDS,launcher.AUDIT_BYTES))
        with tempfile.TemporaryDirectory() as folder:
            registry=Path(folder);q=dict(version=launcher.VERSION,priorSeconds=1800,priorBytes=536870912,
                maximumSeconds=19800,maximumBytes=18790481920,registryRoot=str(registry),outputRoot=str(registry/'result'),
                pendingHistoryRecoveries=None,recoveryReceiptHashes=None)
            launcher.validate_request(q)
            for field in ('pendingHistoryRecoveries','recoveryReceiptHashes'):
                missing=dict(q);del missing[field]
                with self.assertRaisesRegex(ValueError,'nullable history fields'):launcher.validate_request(missing)
                self.assertFalse((registry/'result').exists())
            for key,value in [('priorSeconds',600),('priorSeconds',1200),('priorBytes',1073741824),('maximumSeconds',23400),
                              ('maximumBytes',19058917376),('version','tower-affinity-preservation-recognition-v1')]:
                with self.assertRaises(ValueError):launcher.validate_request(dict(q,**{key:value}))
            (registry/'plan.json').write_bytes((ROOT/'Balance Harness/Tower-Affinity-Preservation-Recognition-Plan.json').read_bytes())
            with self.assertRaisesRegex(ValueError,'frozen plan'):audit.audit(registry)

    def test_admission_requires_external_pin_exact_runtime_and_launcher(self):
        with tempfile.TemporaryDirectory() as folder:
            p=Path(folder);runtime=p/'runtime';runtime.mkdir()
            request=p/'request.json';launcher.write(request,dict(version=launcher.VERSION))
            harness=runtime/'BalanceHarness.dll';harness.write_bytes(b'literal')
            launcher.write(p/'admission.json',dict(status='RecognitionAdmittedNoReservation',version=launcher.VERSION,
                requestSha256=launcher.digest(request),fights=0,newValues=0))
            for name in ('run-affinity-neighborhood-recognition.py','bounded_windows_process.py'):
                (p/name).write_bytes((ROOT/'build'/name).read_bytes())
            launcher.write(p/'files.json',{f.relative_to(p).as_posix():launcher.digest(f) for f in launcher.inventory(p)})
            pin=launcher.digest(p/'files.json');launcher.validate_admission(request,harness,pin)
            for bad in (None,'0'*64):
                with self.assertRaises(ValueError):launcher.validate_admission(request,harness,bad)
            with self.assertRaises(ValueError):launcher.validate_admission(request,ROOT/'BalanceHarness.dll',pin)
            (p/'run-affinity-neighborhood-recognition.py').write_text('changed')
            with self.assertRaises(ValueError):launcher.validate_admission(request,harness,pin)

    def test_receipt_preserves_verified_native_number_tokens(self):
        raw='{"small": 1.2345678901234567E-30, "zero": 0, "negative": -0}'
        audited=dict(status='Passed',result=json.loads(raw),newFights=0,newValues=0)
        with tempfile.TemporaryDirectory() as folder:
            path=Path(folder)/'audit.json';audit.write_receipt(path,audited,raw)
            self.assertEqual(audited,audit.read(path));self.assertIn(raw,path.read_text())
            with self.assertRaises(FileExistsError):audit.write_receipt(path,audited,raw)
            with self.assertRaisesRegex(ValueError,'changed after'):
                audit.write_receipt(Path(folder)/'changed.json',audited,raw.replace('1.2345678901234567E-30','2.0'))


class NativeAgreementTests(unittest.TestCase):
    native=None
    def test_all_native_decisions_match_two_independent_python_implementations(self):
        paths=sorted(p for p in Path(self.native).glob('*.json') if p.stem!='request-binding')
        self.assertEqual({'unresolved','below','advance','reference-only'},{p.stem for p in paths})
        for path in paths:
            with self.subTest(case=path.stem):
                saved=audit.read(path); rows=saved['study']['evidence']
                r=audit.endpoint(ProtocolTests.plan,rows,[t['seed'] for t in rows[0]['trials']])
                native={k:v for k,v in saved['result'].items() if k not in ('studyHash','archiveHash')}
                self.assertTrue(audit.same(native,r))
                expected=oracle.outcome_summary(ProtocolTests.plan,{r['partyId']:[t['outcome']=='Victory' for t in r['trials']] for r in rows})
                self.assertTrue(audit.same(native['neighborhood'],expected))

    def test_native_request_binding_requires_explicit_nullable_history_fields(self):
        saved=audit.read(Path(self.native)/'request-binding.json');request=saved['request']
        self.assertEqual(saved['canonicalHash'],audit.digest(request))
        for name in ('pendingHistoryRecoveries','recoveryReceiptHashes'):
            self.assertIn(name,request);self.assertIsNone(request[name])
            changed=dict(request);del changed[name]
            self.assertNotEqual(saved['canonicalHash'],audit.digest(changed))


# Windows process-ownership failure tests are below; synthetic operations never enter combat.
@unittest.skipUnless(os.name == 'nt','Windows ownership required')
class OwnerTests(unittest.TestCase):
    def test_owned_job_waits_for_and_terminates_a_surviving_descendant(self):
        from bounded_windows_process import run
        with tempfile.TemporaryDirectory() as folder:
            code = "import subprocess,sys; subprocess.Popen([sys.executable,'-c','import time; time.sleep(30)'],creationflags=0x08000000)"
            # Production children run in a retained runtime directory. Windows may
            # briefly retain an exiting descendant's cwd after job accounting is zero.
            result = run([sys.executable,'-B','-c',code],str(ROOT),str(Path(folder)/'process.log'),time.monotonic()+2,cleanup_seconds=1)
            self.assertTrue(result['timedOut'])
            self.assertEqual(0,result['activeProcesses'])
            self.assertGreaterEqual(result['totalProcesses'],2)

    def test_lease_is_exclusive_and_removed_on_close(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)/'registry'
            with launcher.writer_lease(root):
                with self.assertRaises(ValueError):
                    with launcher.writer_lease(root):
                        self.fail('Concurrent lease')
            self.assertFalse(Path(str(root)+'.writer.lock').exists())

    def exercise(self,failure=None):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); registry = root/'registry'; registry.mkdir(); output = registry/'run'
            harness = root/'BalanceHarness.dll'; harness.write_bytes(b'literal')
            auditor = ROOT/'Balance Harness/analysis/audit-affinity-neighborhood-recognition.py'
            q = dict(version=launcher.VERSION,outputRoot=str(output),registryRoot=str(registry),maximumSeconds=19800,maximumBytes=18790481920,
                     priorSeconds=1800,priorBytes=536870912,pendingHistoryRecoveries=None,recoveryReceiptHashes=None,
                     recognition=dict(auditorPath=str(auditor),auditorHash=launcher.digest(auditor)))
            request = root/'request.json'; launcher.write(request,q); calls=[]
            def run(command,cwd,log,deadline,**kwargs):
                calls.append((command,deadline)); Path(log).write_text('{}')
                kwargs['check']()
                # Verify both leases remain held through the final barrier.
                with self.assertRaises(ValueError):
                    with launcher.writer_lease(registry/'complete-family-allocation'): pass
                value = dict(mechanism='suspended-owned-job-v1',exitCode=0,activeProcesses=0,totalProcesses=1,timedOut=False,seconds=0)
                if len(calls) == 1:
                    launcher.write(output/'native-receipt.json',dict(version=launcher.VERSION,status='Verified',requestFileHash=launcher.digest(output/'request.json'),newAuditFights=0,fights=90112))
                    launcher.write(output/'provisional-result.json',{})
                    launcher.write(output/'proposed-teams.json',{})
                    (output/'proposed-recognition.md').write_text('literal')
                if failure == len(calls): return dict(value,exitCode=1)
                if len(calls) == 3:
                    launcher.write(output/'independent-audit.json',dict(status='Passed',requestFileHash=launcher.digest(output/'request.json'),newFights=0,newValues=0,result={}))
                if failure == 'mutation' and len(calls) == 4:
                    (output/'proposed-teams.json').write_text('{"changed":true}')
                return value
            original_write = launcher.write
            def write(path,value):
                if path.name == str(failure)+'.json': raise OSError('Injected terminal write failure')
                original_write(path,value)
            with patch('bounded_windows_process.run',side_effect=run), patch.object(launcher,'write',side_effect=write), patch.object(launcher,'validate_admission'):
                if failure:
                    with self.assertRaises((ValueError,OSError)): launcher.launch(request,harness,'dotnet')
                    self.assertTrue((output/'failure.json').exists())
                    self.assertFalse((output/'closeout.json').exists())
                    if failure not in ('completion','files','closeout'):
                        self.assertFalse((output/'completion.json').exists())
                        self.assertFalse((output/'teams.json').exists())
                else:
                    launcher.launch(request,harness,'dotnet')
                    self.assertEqual(4,len(calls)); self.assertEqual(calls[1][1],calls[2][1]); self.assertEqual(calls[2][1],calls[3][1])
                    result = audit.read(output/'completion.json')
                    self.assertEqual(1800,result['chargedSeconds']-result['seconds'])
                    self.assertEqual(536870912,result['chargedBytes']-result['observedBytes'])
                    final = audit.read(output/'closeout.json')
                    self.assertEqual(final['filesHash'],audit.sha(output/'files.json'))
                    self.assertEqual(final['retainedBytes'],sum(p.stat().st_size for p in output.rglob('*') if p.is_file()))
                    self.assertGreaterEqual(final['measuredSeconds'],result['seconds'])
                    self.assertEqual(final['chargedBytes'],final['retainedBytes']+536870912)
                    files = audit.read(output/'files.json')
                    self.assertEqual(set(files)|{'files.json','closeout.json'},{p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()})
                    self.assertTrue(all(audit.sha(output/name) == pin for name,pin in files.items()))
                with self.assertRaises(ValueError): launcher.launch(request,harness,'dotnet')

    def test_complete_owner_uses_same_audit_deadline_and_includes_seal_bytes(self): self.exercise()
    def test_failed_native_has_no_publication(self): self.exercise(1)
    def test_failed_native_audit_has_no_publication(self): self.exercise(2)
    def test_failed_independent_audit_has_no_publication(self): self.exercise(3)
    def test_failed_barrier_has_no_publication(self): self.exercise(4)
    def test_mutated_proposals_cannot_publish(self): self.exercise('mutation')
    def test_native_storage_failure_has_no_publication(self):
        with patch.object(launcher,'NATIVE_BYTES',1): self.exercise('storage')
    def test_audit_storage_reserve_cannot_borrow_unused_native_allowance(self):
        with patch.object(launcher,'AUDIT_BYTES',1): self.exercise('storage')
    def test_interrupted_completion_is_failed(self): self.exercise('completion')
    def test_interrupted_seal_is_failed(self): self.exercise('files')
    def test_interrupted_terminal_receipt_is_failed(self): self.exercise('closeout')


class SavedRowsTests(unittest.TestCase):
    fixture=None

    def test_direct_report_reader_rejects_reordering_and_terminal_metadata_tampering(self):
        root=Path(self.fixture)/'result'; study=root/'study'
        teams=audit.read(root/'source-definition.json')['teams'];panel=audit.read(root/'confirmation-binding.json')['panel']
        trial_path=study/'trials.jsonl'; original=trial_path.read_text(); lines=original.splitlines(keepends=True)
        before=audit.sha(trial_path)
        native_read_text=Path.read_text
        for fault in ('ordinal','recipe','seed','cache'):
            first=json.loads(lines[0])
            if fault=='ordinal':first['id']='trial-000002'
            if fault=='recipe':first['recipe']='0'*64
            if fault=='seed':first['seed']=panel[1]
            if fault=='cache':first['cacheKey']='bad'
            changed=json.dumps(first)+'\n'+''.join(lines[1:])
            def altered(path,*args,**kwargs):
                return changed if path==trial_path else native_read_text(path,*args,**kwargs)
            with self.subTest(fault=fault),patch.object(Path,'read_text',altered):
                with self.assertRaises(ValueError):audit.audit_rows(study,teams,panel)
        report_path=study/'battles/trial-000001.json.gz'; report_pin=audit.sha(report_path)
        original_open=audit.gzip.open
        with original_open(report_path,'rt',encoding='utf-8-sig') as stream:report=json.load(stream)
        for fault in ('seed','scenario','outcome','success'):
            changed=copy.deepcopy(report)
            if fault=='seed':changed['battle']['seed']=panel[1]
            if fault=='scenario':changed['battle']['scenarioId']='different'
            if fault=='outcome':changed['battle']['summary']['contentOutcome']='Faulted'
            if fault=='success':changed['succeeded']=not changed['succeeded']
            def altered_open(path,*args,**kwargs):
                return io.StringIO(json.dumps(changed)) if path==report_path else original_open(path,*args,**kwargs)
            with self.subTest(fault=fault),patch.object(audit.gzip,'open',altered_open):
                with self.assertRaises(ValueError):audit.audit_rows(study,teams,panel)
        self.assertEqual(before,audit.sha(trial_path));self.assertEqual(report_pin,audit.sha(report_path))


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--native-outcomes');p.add_argument('--fixture');args=p.parse_args()
    NativeAgreementTests.native=args.native_outcomes;SavedRowsTests.fixture=args.fixture
    classes=[ProtocolTests,OwnerTests]+([NativeAgreementTests] if args.native_outcomes else [])+([SavedRowsTests] if args.fixture else [])
    suite=unittest.TestSuite(unittest.defaultTestLoader.loadTestsFromTestCase(c) for c in classes)
    sys.exit(not unittest.TextTestRunner(verbosity=2).run(suite).wasSuccessful())
