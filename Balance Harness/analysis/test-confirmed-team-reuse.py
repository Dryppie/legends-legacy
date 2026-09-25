"""Saved-input and publication tests. Literal receipts never claim native combat."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('reuse', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))
reuse = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reuse)


class ReuseTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='tower-reuse-fixture-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.archive = self.root / 'source'; self.archive.mkdir()
        self.runtime = self.root / 'runtime'; self.runtime.mkdir()
        self.content = self.root / 'content'; self.content.mkdir()
        (self.content / 'Data').mkdir()
        (self.runtime / 'BalanceHarness.dll').write_bytes(b'literal executable; never loaded')
        (self.content / 'Data/floors.json').write_bytes(b'literal content')
        (self.content / 'appsettings.json').write_text('{}')
        ids = [format(i, '064x') for i in range(1, 9)]
        self.ids = ids
        family = [dict(role='Candidate' if i < 6 else 'PrimaryReference' if i == 6 else 'OtherReference',
                       partyId=pid, referenceIds=[] if i < 6 else ['reference-' + str(i)],
                       scenario=dict(schemaVersion=1, id='literal-' + str(i), seeds=[], party=[{'exact': i}]))
                  for i, pid in enumerate(ids)]
        self.definition = dict(version=reuse.SOURCE_VERSION, teams=family,
                               contentHashes={'floors.json': reuse.sha(self.content / 'Data/floors.json')},
                               executionHash='a' * 64, settingsHash='b' * 64, excludedCombatSeeds=[17])
        result = dict(version=reuse.SOURCE_VERSION, executionStatus='Complete', integrityStatus='Verified',
                      strengthDecision='StrongerFixedCandidatesConfirmed', adoption='RecommendFixedCandidates',
                      candidateIds=ids[:6], qualifyingPartyIds=[ids[0]], recommendedPartyIds=[ids[0]],
                      controlPartyIds=ids[-2:], studyHash='c' * 64, archiveHash='d' * 64)
        frozen = dict(version=reuse.SOURCE_VERSION, requestHash='e' * 64, definition=self.definition)
        self.put('result.json', result); self.put('independent-audit.json', result)
        self.put('native-audit.json', dict(status='Passed', studyHash=result['studyHash'], archiveHash=result['archiveHash'], newFights=0))
        self.put('completion.json', dict(status='Complete', fights=44000, retries=0, requestHash=frozen['requestHash']))
        self.put('freeze.json', frozen); self.put('study/freeze.json', frozen); self.put('source-definition.json', self.definition)
        self.put('study/scope.json', dict(algorithm=reuse.SOURCE_VERSION, contentHashes=self.definition['contentHashes'],
                 execution=dict(assemblyHashes={'BalanceHarness': reuse.sha(self.runtime / 'BalanceHarness.dll')})))
        self.put('study/executable-files.json', {'BalanceHarness.dll': reuse.sha(self.runtime / 'BalanceHarness.dll')})
        self.put('teams.json', dict(version=reuse.SOURCE_VERSION, adoption=result['adoption'], candidateIds=ids[:6], qualifyingPartyIds=[ids[0]],
                 teams=[dict(**t, qualifies=i == 0, recommended=i == 0, control=i >= 6) for i, t in enumerate(family)]))
        for t in family:
            self.put('study/exports/' + t['partyId'] + '.json', t['scenario'])
        self.q = dict(version=reuse.VERSION, archiveRoot=str(self.archive), candidatePartyId=ids[0],
                      archiveManifestSha256='', runtimeRoot=str(self.runtime), contentRoot=str(self.content))
        self.repin()
        self.request = self.root / 'request.json'; self.save_request()
        self.output = self.root / 'prepared'

    def put(self, name, obj):
        path = self.archive / name; path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(obj), encoding='utf-8')

    def repin(self):
        files = {p.relative_to(self.archive).as_posix(): reuse.sha(p)
                 for p in self.archive.rglob('*') if p.is_file() and p.name != 'files.json'}
        files['study/files.json'] = 'd' * 64
        self.put('files.json', files)
        self.q['archiveManifestSha256'] = reuse.sha(self.archive / 'files.json')

    def save_request(self):
        self.request.write_text(json.dumps(self.q), encoding='utf-8')

    @staticmethod
    def literal_check(output, deadline):
        q = reuse.read(output / 'native-request.json')
        result = dict(status='ContextMatchedRecipesPrepared', executionHash=q['executionHash'], settingsHash=q['settingsHash'],
                      teams=[{k: t[k] for k in ('partyId', 'scenarioFileHash')} for t in q['teams']],
                      nativePreparations=3, fights=0, newValues=0)
        reuse.write(output / 'native-check.json', result)
        return result

    def test_exact_selection_preserves_both_controls_and_export_bytes(self):
        result = reuse.prepare(self.request, self.output, self.literal_check)
        self.assertEqual('ReadyForExplicitReuse', result['status'])
        self.assertEqual(self.ids[-2:], result['controlPartyIds'])
        for pid in (self.ids[0], *self.ids[-2:]):
            self.assertEqual((self.archive / 'study/exports' / (pid + '.json')).read_bytes(),
                             (self.output / 'scenarios' / (pid + '.json')).read_bytes())
        self.assertEqual('VerifiedReusableInputs', reuse.verify(self.output, result['manifestSha256'])['status'])
        self.assertFalse(reuse.read(self.output / 'reuse.json')['searchRequest'])
        self.assertFalse(any(p.name in ('history-input.json', 'seed-ledger.json') for p in self.output.rglob('*')))

    def test_no_implicit_candidate_control_or_nonqualifier_selection(self):
        for pid in ('', 'f' * 64, self.ids[1], self.ids[-1]):
            with self.subTest(pid=pid):
                q = self.q | {'candidatePartyId': pid}
                with self.assertRaises(ValueError): reuse.inspect(q)

    def test_changed_pinned_evidence_fails_before_output(self):
        with (self.archive / 'teams.json').open('a') as stream: stream.write(' ')
        with self.assertRaisesRegex(ValueError, 'Changed or unpinned'): reuse.prepare(self.request, self.output, self.literal_check)
        self.assertFalse(self.output.exists())

    def test_changed_runtime_or_content_cannot_transfer_strength(self):
        for path in (self.runtime / 'BalanceHarness.dll', self.content / 'Data/floors.json'):
            with self.subTest(path=path):
                original = path.read_bytes(); path.write_bytes(b'changed')
                with self.assertRaisesRegex(ValueError, 'Target .* differs'): reuse.inspect(self.q)
                path.write_bytes(original)

    def test_resealed_source_semantic_errors_fail(self):
        originals = {p: p.read_bytes() for p in self.archive.rglob('*') if p.is_file()}
        mutations = [('independent-audit.json', lambda d: d.update(adoption='Hold')),
                     ('native-audit.json', lambda d: d.update(status='Failed')),
                     ('completion.json', lambda d: d.update(fights=43999)),
                     ('result.json', lambda d: d.update(controlPartyIds=[self.ids[-1]])),
                     ('teams.json', lambda d: d['teams'][-1].update(control=False)),
                     ('teams.json', lambda d: d['teams'][0].update(recommended=False)),
                     ('study/exports/' + self.ids[0] + '.json', lambda d: d.update(seeds=[123]))]
        for name, mutate in mutations:
            with self.subTest(name=name):
                for p, data in originals.items(): p.write_bytes(data)
                obj = reuse.read(self.archive / name); mutate(obj); self.put(name, obj); self.repin()
                with self.assertRaises(ValueError): reuse.inspect(self.q)

    def test_output_and_source_are_never_overwritten(self):
        self.output.mkdir(); (self.output / 'keep').write_text('preserve')
        with self.assertRaises(ValueError): reuse.prepare(self.request, self.output, self.literal_check)
        self.assertEqual('preserve', (self.output / 'keep').read_text())
        for path in (self.archive / 'new', self.content / 'new', self.runtime / 'new'):
            with self.subTest(path=path):
                with self.assertRaises(ValueError): reuse.prepare(self.request, path, self.literal_check)
                self.assertFalse(path.exists())

    def test_failed_native_check_never_publishes_success(self):
        def fail(output, deadline): raise ValueError('literal native failure')
        with self.assertRaises(ValueError): reuse.prepare(self.request, self.output, fail)
        self.assertFalse((self.output / 'reuse.json').exists())
        self.assertFalse((self.output / 'files.json').exists())

    def test_mid_check_mutation_is_rejected(self):
        def changed(output, deadline):
            result = self.literal_check(output, deadline)
            (self.content / 'appsettings.json').write_text('{"changed":true}')
            return result
        with self.assertRaisesRegex(ValueError, 'changed during'): reuse.prepare(self.request, self.output, changed)
        self.assertFalse((self.output / 'reuse.json').exists())

    def test_checker_cannot_change_export_or_admit_combat(self):
        def changed(output, deadline):
            result = self.literal_check(output, deadline); result['fights'] = 1
            return result
        with self.assertRaisesRegex(ValueError, 'Changed native check'): reuse.prepare(self.request, self.output, changed)
        self.assertFalse((self.output / 'reuse.json').exists())

    def test_verification_rejects_extra_files_and_changed_target_settings(self):
        result = reuse.prepare(self.request, self.output, self.literal_check)
        extra = self.output / 'extra.txt'; extra.write_text('changed')
        with self.assertRaisesRegex(ValueError, 'inventory'): reuse.verify(self.output, result['manifestSha256'])
        extra.unlink()
        (self.content / 'appsettings.json').write_text('{"changed":true}')
        with self.assertRaisesRegex(ValueError, 'changed during'): reuse.verify(self.output, result['manifestSha256'])

    def test_unsafe_members_and_duplicate_properties_rejected(self):
        for name in ('../escape', '/absolute', 'C:/absolute', 'nested/../escape', 'a\\b'):
            with self.subTest(name=name):
                with self.assertRaises(ValueError): reuse.member(self.archive, name)
        path = self.root / 'duplicates.json'; path.write_text('{"x":1,"x":2}')
        with self.assertRaisesRegex(ValueError, 'Duplicate'): reuse.read(path)


if __name__ == '__main__':
    unittest.main(verbosity=2)
