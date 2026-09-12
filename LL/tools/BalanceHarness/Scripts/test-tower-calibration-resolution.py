"""The final fixed-sample decision retains all families and their error budgets."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('resolution', Path(__file__).with_name('resolve-tower-calibration-precision.py'))
resolution = importlib.util.module_from_spec(spec); spec.loader.exec_module(resolution)


def previous():
    old = [{'id': f'p{i}', 'valid': 250, 'wins': [119, 94, 90][i] if i < 3 else 0,
            'draws': 0, 'artifactHash': 'a'*64} for i in range(2438)]
    blocks = [{'issues': [], 'assessment': 'Inconclusive', 'cells': [
        {'id': cid, 'valid': 1000, 'wins': (488 if i < 9 else 484) if cid == 'p0' else (257 if i < 9 else 255),
         'draws': 0, 'issues': [], 'artifactHash': format((i+1)*100+j, '064x')} for j, cid in enumerate(['p0', 'p1'])]} for i in range(10)]
    return resolution.precision.assess(old, ['p0', 'p1'], blocks)


def blocks(wins=488):
    return [{'issues': [], 'assessment': 'Inconclusive', 'cells': [
        {'id': 'p0', 'valid': 1000, 'wins': wins, 'draws': 0, 'issues': [], 'artifactHash': format(i+1, '064x')}]} for i in range(50)]


class ResolutionTests(unittest.TestCase):
    def test_selects_the_one_remaining_unresolved_team(self):
        old = previous()
        self.assertEqual('Inconclusive', old['assessment'])
        self.assertEqual(['p0'], resolution.select(old))

    def test_pass_preserves_full_family_and_uses_only_new_50000_for_selected_team(self):
        old = previous(); r = resolution.assess(old, ['p0'], blocks())
        self.assertEqual('Pass', r['assessment'])
        self.assertEqual(2438, r['familySize'])
        self.assertEqual(50000, r['actualFreshCombats'])
        self.assertEqual((24400, 50000, 'resolution'), tuple(r['cells'][0][k] for k in ('wins', 'valid', 'sampleSource')))
        self.assertEqual(2568, r['cells'][1]['wins'])
        self.assertGreater(r['cells'][1]['upper'], old['cells'][1]['upper'])
        self.assertEqual(old['cells'][2], r['cells'][2])
        self.assertEqual(.05, r['originalAlpha'] + r['firstFreshAlpha'] + r['resolutionAlpha'])

    def test_breach_and_inconclusive_intervals_do_not_pass(self):
        for wins, expected in [(501, 'Fail'), (495, 'Inconclusive')]:
            with self.subTest(wins=wins):
                self.assertEqual(expected, resolution.assess(previous(), ['p0'], blocks(wins))['assessment'])

    def test_missing_reused_partial_or_invalid_blocks_rejected(self):
        valid = blocks(); cases = [valid[:-1], valid + [valid[0]]]
        bad = copy.deepcopy(valid); bad[1] = copy.deepcopy(bad[0]); cases.append(bad)
        for field, value in [('valid', 999), ('wins', 1001), ('issues', ['invalid']), ('draws', 1000)]:
            bad = copy.deepcopy(valid); bad[0]['cells'][0][field] = value; cases.append(bad)
        bad = copy.deepcopy(valid); bad[0]['assessment'] = 'Invalid'; cases.append(bad)
        for bad in cases:
            with self.assertRaises(ValueError): resolution.assess(previous(), ['p0'], bad)

    def test_selection_cannot_drop_or_replace_the_unresolved_team(self):
        for ids in ([], ['p1'], ['p0', 'p1']):
            with self.subTest(ids=ids), self.assertRaises(ValueError): resolution.assess(previous(), ids, blocks())

    def test_prior_family_or_observed_breach_cannot_be_erased(self):
        cases = []
        bad = previous(); bad['cells'][0]['rate'] = .51; cases.append(bad)
        bad = previous(); bad['selectedIds'] = ['p0']; cases.append(bad)
        bad = previous(); bad['cells'].pop(); cases.append(bad)
        bad = previous(); bad['assessment'] = 'Pass'; cases.append(bad)
        bad = previous(); bad['originalAlpha'] = .05; cases.append(bad)
        for bad in cases:
            with self.assertRaises(ValueError): resolution.select(bad)


if __name__ == '__main__': unittest.main()
