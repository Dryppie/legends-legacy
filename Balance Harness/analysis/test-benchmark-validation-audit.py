"""Cross-language qualification using fixtures emitted by the actual C# adapter.

Set TOWER_VALIDATION_FIXTURE_OUTPUT while running the native backend tests, then
pass that directory here. These literal fixtures contain no production fights.
"""
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('validation_audit', Path(__file__).with_name('audit-benchmark-validation.py'))
a = importlib.util.module_from_spec(spec)
spec.loader.exec_module(a)
FIXTURES = None


def recount(root):
    fixture = a.read(root/'literal-fixture.json')
    context = dict(scope=a.read(root/'plan.json')['racing']['scope'])
    consumed = []
    def consume(row, party):
        request, outcome = row['request'], row['outcome']
        report = fixture['battles'][outcome['trialId']]
        battle, summary = report['battle'], report['battle']['summary']
        a.require(request['scenario']['party'] == a.base.physical_party(context, party)
                  and battle['schemaVersion'] == 1 and battle['scenarioId'] == request['scenario']['id']
                  and battle['seed'] == request['seed'] and battle['ticksPerSecond'] == 10
                  and summary['contentOutcome'] in ('Victory', 'Defeat', 'Draw')
                  and summary['durationSeconds'] == summary['durationTicks']/10
                  and report['succeeded'] == (summary['contentOutcome'] == 'Victory'), 'Changed literal fixture battle')
        direct = dict(outcome=summary['contentOutcome'], guardianHealth=report['guardianHealthRemainingPercent'],
                      survival=100*sum(p['health'] > 0 for p in summary['friendly'])/len(summary['friendly']),
                      durationSeconds=summary['durationSeconds'])
        a.require(a.close({k: outcome[k] for k in direct}, direct), 'Changed literal fixture outcome')
        consumed.append(outcome['trialId'])
    result = a.audit_racing(root, fixture['trials'], consume, fixture=True)
    a.require(consumed == list(fixture['battles']), 'Changed literal fixture membership/order')
    return result


class ValidationAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        a.require(FIXTURES is not None and FIXTURES.is_dir(), 'Supply native fixture directory; no skipped qualification')
        cls.fixtures = sorted(FIXTURES.iterdir())
        a.require(len(cls.fixtures) == 6, 'Expected all three policies and both checkpoint modes')

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)/'racing'
        shutil.copytree(self.fixtures[-1], self.root)

    def change(self, file, edit):
        path = self.root/file
        value = a.read(path); edit(value)
        path.write_text(json.dumps(value, indent=2), encoding='utf-8')

    def test_native_fixtures_cover_all_policies_owned_and_ordinary_pass_and_fallback(self):
        passed = []
        for root in self.fixtures:
            with self.subTest(root=root.name):
                result = recount(root)
                self.assertEqual(528, result['chargedEvaluations'])
                self.assertEqual(0, result['newFights'])
                self.assertTrue(result['nativeReconstructionRequired'])
                passed.append(result['validationDecision']['passed'])
        self.assertEqual(3, sum(passed))

    def test_exact_gate_all_1891_count_pairs_and_integer_types(self):
        for gains in range(61):
            for losses in range(61-gains):
                n, d, passed = a.gate(gains, losses)
                # Independently build Pascal's row rather than the audit's comb sum.
                row = [1]
                for _ in range(gains+losses):
                    row = [1] + [x+y for x, y in zip(row, row[1:])] + [1]
                self.assertEqual((sum(row[gains:]), sum(row)), (n, d))
                self.assertEqual(gains > losses and 20*n <= d, passed)
        self.assertEqual((1, 1, False), a.gate(0, 0))
        self.assertEqual((1, 16, False), a.gate(4, 0))
        self.assertEqual((1, 32, True), a.gate(5, 0))
        for counts in ((True, 0), (1.0, 0), (-1, 0), (61, 0), (30, 31)):
            with self.assertRaises(ValueError): a.gate(*counts)

    def test_changed_exact_decision_in_both_copies_is_recomputed(self):
        originals = {name: (self.root/name).read_bytes() for name in ('search.json', 'validation-decision.json')}
        for field, change in [('tailNumerator', lambda x: x+1), ('tailDenominator', lambda x: x+1),
                              ('gainedWins', lambda x: x+1), ('passed', lambda x: not x),
                              ('selectedId', lambda x: 'f'*64), ('samples', lambda x: 59),
                              ('tailNumerator', float)]:
            with self.subTest(field=field):
                self.change('search.json', lambda v: v['evaluation']['validationDecision'].__setitem__(field, change(v['evaluation']['validationDecision'][field])))
                self.change('validation-decision.json', lambda v: v.__setitem__(field, change(v[field])))
                with self.assertRaises(ValueError): recount(self.root)
                for name, raw in originals.items(): (self.root/name).write_bytes(raw)

    def test_both_freeze_copies_cannot_renominate_from_validation(self):
        self.change('search.json', lambda v: v['evaluation']['validationFreeze'].__setitem__('challengerId', v['evaluation']['validationFreeze']['benchmarkId']))
        self.change('validation-freeze.json', lambda v: v.__setitem__('challengerId', v['benchmarkId']))
        with self.assertRaisesRegex(ValueError, 'challenger freeze'): recount(self.root)

    def test_one_unit_drift_above_float_precision_is_rejected(self):
        report = a.read(self.root/'search.json'); fixture = a.read(self.root/'literal-fixture.json')
        evaluation = report['evaluation']; panel = evaluation['panels'][5]
        frozen = evaluation['validationFreeze']; benchmark = frozen['benchmarkId']
        for row in panel['observations']:
            won = row['request']['partyId'] == benchmark
            outcome = 'Victory' if won else 'Defeat'
            row['outcome']['outcome'] = outcome
            battle = fixture['battles'][row['outcome']['trialId']]
            battle['succeeded'] = won
            battle['battle']['summary'].update(contentOutcome=outcome, engineOutcome=outcome)
        ids = [p['id'] for p in panel['freeze']['parties']]
        panel['scores'] = a.score(ids, panel['observations'])
        for contrast in panel['contrasts']:
            contrast.update(gainedWins=60 if contrast['partyId'] == benchmark else 0,
                            lostWins=0 if contrast['partyId'] == benchmark else 60)
        decision = evaluation['validationDecision']
        decision.update(gainedWins=0, lostWins=60, tailNumerator=2**60, tailDenominator=2**60, passed=False, selectedId=benchmark)
        evaluation['rawSelectedId'] = benchmark
        for name, value in [('search.json', report), ('literal-fixture.json', fixture), ('validation-decision.json', decision)]:
            (self.root/name).write_text(json.dumps(value), encoding='utf-8')
        self.assertFalse(recount(self.root)['validationDecision']['passed'])
        # float(2**60+1) == float(2**60); exact evidence must still distinguish them.
        decision['tailNumerator'] += 1
        for name, value in [('search.json', report), ('validation-decision.json', decision)]:
            (self.root/name).write_text(json.dumps(value), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'exact validation decision'): recount(self.root)

    def test_missing_and_extra_members(self):
        for name in ('validation-freeze.json', 'validation-decision.json', 'panel-06.json', 'search.json'):
            raw = (self.root/name).read_bytes(); (self.root/name).unlink()
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, 'membership'): recount(self.root)
            (self.root/name).write_bytes(raw)
        (self.root/'extra.json').write_text('{}')
        with self.assertRaisesRegex(ValueError, 'membership'): recount(self.root)

    def test_incomplete_never_becomes_benchmark_fallback(self):
        self.change('search.json', lambda v: v['evaluation'].__setitem__('status', 'Failed'))
        with self.assertRaisesRegex(ValueError, 'contract'): recount(self.root)

    def test_validation_pair_reordering_is_rejected(self):
        self.change('search.json', lambda v: v['evaluation']['panels'][5]['observations'].reverse())
        with self.assertRaisesRegex(ValueError, 'membership/order'): recount(self.root)

    def test_reused_validation_values_are_rejected(self):
        self.change('plan.json', lambda v: v['racing']['panels'][5]['seeds'].__setitem__(0, v['racing']['panels'][4]['seeds'][0]))
        with self.assertRaisesRegex(ValueError, 'panel values'): recount(self.root)

    def test_literal_battle_tampering_is_rejected(self):
        self.change('literal-fixture.json', lambda v: v['battles']['trial-000528'].__setitem__('guardianHealthRemainingPercent', 1))
        with self.assertRaisesRegex(ValueError, 'literal fixture outcome'): recount(self.root)

    def test_torn_charge_and_changed_input_journals(self):
        path = self.root/'charges.jsonl'; raw = path.read_bytes(); path.write_bytes(raw.rstrip())
        with self.assertRaisesRegex(ValueError, 'journal'): recount(self.root)
        path.write_bytes(raw)
        path = self.root/'inputs.jsonl'; path.write_bytes(path.read_bytes().replace(b'trial-000528', b'trial-000527'))
        with self.assertRaisesRegex(ValueError, 'journal'): recount(self.root)

    def test_pruning_and_nomination_scores_are_recounted(self):
        path = self.root/'search.json'; raw = path.read_bytes()
        for edit in (lambda v: v['evaluation']['decisions'][0].__setitem__('competitiveCutoffWins', -1),
                     lambda v: v['evaluation']['panels'][4]['scores'][0].__setitem__('wins', 0)):
            self.change('search.json', edit)
            with self.assertRaises(ValueError): recount(self.root)
            path.write_bytes(raw)

    def test_external_pin_is_checked_before_archive_read(self):
        (self.root/'files.json').write_text('{}')
        with self.assertRaisesRegex(ValueError, 'external manifest pin'): a.audit(self.root, '0'*64)


if __name__ == '__main__':
    FIXTURES = Path(sys.argv.pop(1)).resolve() if len(sys.argv) > 1 else None
    unittest.main()
