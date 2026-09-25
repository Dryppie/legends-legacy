"""Finite monitor JSON encoding and hash close failures; no scientific trials."""
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Balance Harness/analysis'))
import proposal_monitor_io as budget
import proposal_owner_process as monitor
import proposal_work_accounting as work


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,ROOT/path)
    value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value)
    return value


fixture=module('serialization_fixture','build/test-proposal-monitor-io.py')


class Encoding(unittest.TestCase):
    def exact(self,value):
        expected=work.canonical(value)
        self.assertEqual(budget.bounded_canonical(value,len(expected)),expected)
        with self.assertRaisesRegex(ValueError,'byte allowance'):
            budget.bounded_canonical(value,len(expected)-1)
        return expected

    def test_canonical_scalar_and_container_compatibility(self):
        values=[None,True,False,0,-1,2**63-1,-2**63,0.0,-0.0,1e-20,1e30,1.2345678901234567,
                '',[],{},(),[{},[],None,False],{'z':[1,2.0],'a':{'text':'value'}}]
        for value in values:
            with self.subTest(value=value):self.exact(value)

    def test_all_control_escapes_and_unicode_are_identical(self):
        text=''.join(chr(i) for i in range(32))+'"\\/ æ東😀\u2028\u2029\x7f'
        self.exact({'text':text,text:None})

    def test_chunk_boundaries_preserve_escaping_and_utf8(self):
        for length in (1023,1024,1025,2048,2049):
            with self.subTest(length=length):self.exact('x'*(length-1)+'😀\n"\\')

    def test_string_encoder_never_receives_an_oversized_token(self):
        original=json.dumps;seen=[]
        def dumps(value,*args,**kwargs):
            self.assertIs(type(value),str);seen.append(len(value))
            self.assertLessEqual(len(value),budget.JSON_STRING_CHARS)
            return original(value,*args,**kwargs)
        text=('\x01😀"\\'*3000)
        expected=work.canonical(text)
        with patch.object(budget.json,'dumps',dumps):actual=budget.bounded_canonical(text,len(expected))
        self.assertEqual(actual,expected);self.assertGreater(len(seen),1)

    def test_large_string_is_rejected_before_token_encoding(self):
        with patch.object(budget.json,'dumps') as dumps,self.assertRaisesRegex(ValueError,'byte allowance'):
            budget.bounded_canonical('x'*1000000,32)
        dumps.assert_not_called()

    def test_control_expansion_stops_at_byte_cap(self):
        original=json.dumps;calls=[]
        def dumps(value,*args,**kwargs):calls.append(len(value));return original(value,*args,**kwargs)
        with patch.object(budget.json,'dumps',dumps),self.assertRaisesRegex(ValueError,'byte allowance'):
            budget.bounded_canonical('\x01'*4096,4098)
        self.assertEqual(calls,[1024])

    def test_large_key_rejected_before_sort_or_encoding(self):
        with patch.object(budget,'sorted',create=True) as sorting,patch.object(budget.json,'dumps') as dumps, \
                self.assertRaisesRegex(ValueError,'byte allowance'):
            budget.bounded_canonical({'x'*1000000:0},32)
        sorting.assert_not_called();dumps.assert_not_called()

    def test_wide_dictionary_rejected_before_sorting(self):
        value={str(i):0 for i in range(10000)}
        with patch.object(budget,'sorted',create=True) as sorting,self.assertRaisesRegex(ValueError,'byte allowance'):
            budget.bounded_canonical(value,32)
        sorting.assert_not_called()

    def test_wide_array_rejected_before_scalar_encoding(self):
        with patch.object(budget.json,'dumps') as dumps,self.assertRaisesRegex(ValueError,'byte allowance'):
            budget.bounded_canonical([1.0]*10000,32)
        dumps.assert_not_called()

    def test_shared_subtree_is_encoded_twice_without_cycle_error(self):
        shared={'value':[1,2,3]};self.exact([shared,shared])

    def test_cycles_fail_without_recursion_overflow(self):
        array=[];array.append(array)
        mapping={};mapping['self']=mapping
        mixed=[];member=(mixed,);mixed.append(member)
        for value in (array,mapping,mixed):
            with self.subTest(kind=type(value).__name__),self.assertRaisesRegex(ValueError,'Circular'):
                budget.bounded_canonical(value,10000)

    def test_depth_limit_accepts_boundary_and_rejects_next_container(self):
        value=None
        for _ in range(budget.JSON_CONTAINER_DEPTH):value=[value]
        self.exact(value)
        with self.assertRaisesRegex(ValueError,'container depth'):budget.bounded_canonical([value],10000)

    def test_tuple_and_string_key_sorting_match_reference(self):
        self.exact({'😀':(1,2),'æ':[], 'a':{'10':10,'2':2},'':None,'A':True})

    def test_nonstring_keys_are_rejected(self):
        for key in (1,1.5,False,None,('x',)):
            with self.subTest(key=key),self.assertRaisesRegex(ValueError,'string keys'):
                budget.bounded_canonical({key:0},100)

    def test_subclasses_and_custom_objects_cannot_run_encoding_hooks(self):
        class CustomList(list):
            def __iter__(self):raise AssertionError('unexpected iterator')
        class CustomDict(dict):
            def items(self):raise AssertionError('unexpected items')
        class CustomString(str):
            def __str__(self):raise AssertionError('unexpected string')
        for value in (CustomList([1]),CustomDict(a=1),CustomString('x'),object(),b'bytes',set()):
            with self.subTest(kind=type(value).__name__),self.assertRaisesRegex(ValueError,'exact built-in'):
                budget.bounded_canonical(value,100)

    def test_nonfinite_floats_are_rejected(self):
        for value in (float('nan'),float('inf'),float('-inf')):
            with self.subTest(value=value),self.assertRaisesRegex(ValueError,'Nonfinite'):
                budget.bounded_canonical(value,100)

    def test_custom_metaclass_cannot_impersonate_supported_type(self):
        class Meta(type):
            def __eq__(self,other):raise AssertionError('unexpected type comparison')
        class Custom(metaclass=Meta):pass
        with self.assertRaisesRegex(ValueError,'exact built-in'):budget.bounded_canonical(Custom(),100)

    def test_unpaired_surrogate_is_rejected_like_canonical_utf8(self):
        with self.assertRaises(UnicodeEncodeError):work.canonical('\ud800')
        with self.assertRaises(UnicodeEncodeError):budget.bounded_canonical('\ud800',100)

    def test_integer_conversion_is_guarded_by_remaining_allowance(self):
        with self.assertRaisesRegex(ValueError,'publication byte allowance'):
            budget.bounded_canonical(1<<100000,32)
        self.exact(10**1000);self.exact(-(10**1000))

    def test_invalid_and_zero_limits_are_rejected(self):
        for maximum in (True,-1,1.0,None,2**63):
            with self.subTest(maximum=maximum),self.assertRaisesRegex(ValueError,'Invalid JSON'):
                budget.bounded_canonical(None,maximum)
        with self.assertRaisesRegex(ValueError,'publication byte allowance'):budget.bounded_canonical(None,0)

    def test_failed_publication_has_no_open_or_write_reservation(self):
        instance=budget.MonitorIO(fixture.limits(maxObservationBytes=32))
        with self.assertRaisesRegex(ValueError,'byte allowance'):
            instance.publication('owner-process.json',{'large':'x'*10000})
        self.assertTrue(instance.failed);self.assertEqual(instance.write_reserved,0)
        self.assertEqual(instance.publications['owner-process.json'],dict(openAttempted=False,reservedBytes=0,acceptedBytes=0))
        with self.assertRaisesRegex(ValueError,'cannot be reused'):instance.publication('files.json',{})

    def test_remaining_total_write_budget_is_used(self):
        instance=budget.MonitorIO(fixture.limits(maxTotalWriteBytes=4))
        self.assertEqual(instance.publication('owner-process.json',{}),b'{}')
        self.assertEqual(instance.publication('files.json',{}),b'{}');self.assertEqual(instance.write_reserved,4)
        instance=budget.MonitorIO(fixture.limits(maxTotalWriteBytes=3));instance.publication('owner-process.json',{})
        with self.assertRaisesRegex(ValueError,'byte allowance'):instance.publication('files.json',{})

    def test_v2_terminal_uses_the_same_encoder_and_limits(self):
        with tempfile.TemporaryDirectory() as root:
            value=fixture.limits(version=budget.TERMINAL_VERSION,terminalRoot=str(Path(root)/'terminal'),maxTerminalBytes=2)
            instance=budget.MonitorIO(value);self.assertEqual(instance.publication('monitor-call.json',{}),b'{}')
            with self.assertRaisesRegex(ValueError,'repeated'):instance.publication('monitor-call.json',{})


class Integration(unittest.TestCase):
    def setUp(self):
        self.case=fixture.Budget();self.case.setUp();self.addCleanup(self.case.doCleanups)

    def test_hash_close_failure_in_caller_except_is_not_suppressed(self):
        error=OSError('hash close failed');caller=RuntimeError('already handled')
        try:raise caller
        except RuntimeError:
            with self.case.fault('normal',close_error=error),self.assertRaises(OSError) as caught:
                self.case.io.sha(self.case.path,None)
        self.assertIs(caught.exception,error);self.assertFalse(hasattr(caller,'__notes__'))
        self.assertTrue(self.case.io.failed);self.assertEqual(self.case.io.hash_completed,0)
        self.assertTrue(self.case.reader.closed);self.assertEqual(self.case.io.read_reserved,4)

    def test_hash_read_error_stays_primary_inside_caller_except(self):
        error=OSError('read failed');secondary=OSError('close failed')
        try:raise RuntimeError('already handled')
        except RuntimeError:
            with self.case.fault('raise',error,secondary),self.assertRaises(OSError) as caught:
                self.case.io.sha(self.case.path,None)
        self.assertIs(caught.exception,error);self.assertIn('close failed',error.__notes__[0]);self.assertTrue(self.case.io.failed)

    def test_successful_hash_inside_caller_except_remains_successful(self):
        try:raise RuntimeError('already handled')
        except RuntimeError:self.assertEqual(self.case.io.sha(self.case.path,None),monitor.sha(self.case.path))
        self.assertFalse(self.case.io.failed);self.assertEqual(self.case.io.hash_completed,1)

    def test_large_receipt_fails_before_publication_open_and_retains_drained_job(self):
        case=fixture.Monitor();case.setUp();self.addCleanup(case.doCleanups)
        snapshot=case.monitor._snapshot
        def changed(*args):snapshot(*args);case.monitor.observation['large']='x'*1000000
        with patch.object(case.monitor,'_snapshot',changed),self.assertRaisesRegex(ValueError,'byte allowance'):
            case.run_monitor()
        self.assertFalse((case.monitor.root/'owner-process.json').exists())
        self.assertTrue(case.monitor.process_observation['jobDrained']);self.assertTrue(case.monitor.monitor_io.failed)

    def test_real_v1_receipts_are_byte_identical_to_reference_encoding(self):
        case=fixture.Monitor();case.setUp();self.addCleanup(case.doCleanups)
        case.run_monitor()
        for name in ('owner-process.json','files.json'):
            raw=(case.monitor.root/name).read_bytes();self.assertEqual(raw,work.canonical(work.strict(raw)))

    def test_routed_owner_preserves_canonical_v2_receipts_and_job_limits(self):
        source=module('serialization_routed_fixture','build/test-proposal-job-limits.py')
        case=source.Monitor();case.setUp();self.addCleanup(case.doCleanups)
        root=Path(os.environ['LL_SERIALIZATION_EXPORT']) if os.environ.get('LL_SERIALIZATION_EXPORT') else self.case.base/'routed'
        with patch.dict(os.environ,LL_JOB_LIMITS_EXPORT=str(root)):
            case.test_routed_owner_composes_limits_environment_io_and_terminal()
        for path in (root/'monitor/owner-process.json',root/'monitor/files.json',root/'terminal/monitor-call.json'):
            raw=path.read_bytes();self.assertEqual(raw,work.canonical(work.strict(raw)))
        value=work.strict((root/'monitor/owner-process.json').read_bytes())
        self.assertTrue(value['processObservation']['cleanup']['allAcquiredHandlesClosed'])
        self.assertEqual(value['monitorIOModuleSha256'],monitor.sha(budget.__file__))


if __name__=='__main__':unittest.main()
