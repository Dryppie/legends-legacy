"""Safeguards for distinctions that affect the consolidated Tower conclusions."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location('review', Path(__file__).with_name('review-tower-balance-status.py'))
review = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(review)


class ReviewTests(unittest.TestCase):
    def review_fixture(self, floor, legacy):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            target = dict(floorNumber=floor, guardianScaling=dict(health=2, offense=3))
            tower = dict(floors=[target])
            cell = dict(id='party/gear', gear='baseline', scenario=dict(party=[dict(partySlot=1,
                build=dict(characterLevel=30, tier=1, rank=2, quality='Standard', attributeRollMultiplier=1,
                    essenceIds=['rat']*7, equipment=[dict(definitionId='plain.staff.rarity.rare')]))]))
            lo, hi = review.interval(64,256,1)
            row = dict(id=cell['id'], wins=64, samples=256, essenceSlots=7)
            bound = dict(id=cell['id'], lower=lo, upper=hi)
            if legacy: row['adjusted'] = bound
            payloads = {'result.json':dict(rows=[row],fights=256), 'cells.json':[cell],
                'scope.json':dict(settings={},execution=dict(assemblyHashes={})),
                'content/Data/world-tower/tower-floors.json':tower}
            payloads['confirmation-seeds.json' if legacy else 'request.json'] = list(range(256)) if legacy else dict(seeds=list(range(256)))
            for name,value in payloads.items():
                path=root/name; path.parent.mkdir(parents=True,exist_ok=True); path.write_text(json.dumps(value),encoding='utf-8')
            journal=('study' if legacy else 'evaluation')+'/trials.jsonl'
            (root/journal).parent.mkdir(); (root/journal).write_text('',encoding='utf-8')
            manifest=root/'files.json'
            manifest.write_text(json.dumps({name:review.sha(root/name) for name in [*payloads,journal]}),encoding='utf-8')
            audit=root/'audit.json'; audit.write_text(json.dumps(dict(status='Verified',resultSha256=review.sha(root/'result.json'),
                assessment='Pass' if legacy else dict(verdict='Pass',bounds=[bound]))),encoding='utf-8')
            entry=dict(floor=floor,source=str(root),manifestSha256=review.sha(manifest),audit=str(audit),auditSha256=review.sha(audit))
            stats={cell['id']:dict(seeds=set(range(256)),display=[1]*256,engine=[1]*256,victory=[1]*64)}
            with patch.object(review.Archive,'recount',return_value=(stats,dict(recountedReports=256))) as recount:
                result=review.review_floor(entry,payloads['scope.json'],tower)
            self.assertEqual(result['viableCompositions'],1)
            self.assertEqual(recount.call_args.args[0],'study' if legacy else 'evaluation')

    def test_expanded_floor10_uses_modern_confirmation_archive(self):
        self.review_fixture(10,False)

    def test_historical_floor11_keeps_legacy_confirmation_archive(self):
        self.review_fixture(11,True)

    def test_gear_identity_level_and_order_do_not_inflate_compositions(self):
        original = {'scenario': {'party': [
            {'partySlot': 1, 'build': {'essenceIds': ['rat', 'snake'], 'tier': 1}},
            {'partySlot': 2, 'build': {'essenceIds': ['wisp', 'nymph'], 'tier': 1}}]}}
        variant = copy.deepcopy(original)
        variant['scenario']['party'].reverse()
        for member in variant['scenario']['party']:
            member['build'].update(tier=2, rank=5, id='different', characterLevel=60)
            member['build']['essenceIds'].reverse()
        self.assertEqual(review.composition(original), review.composition(variant))
        variant['scenario']['party'][0]['build']['essenceIds'][0] = 'imp'
        self.assertNotEqual(review.composition(original), review.composition(variant))
        swapped = copy.deepcopy(original)
        swapped['scenario']['party'][0]['partySlot'] = 2
        swapped['scenario']['party'][1]['partySlot'] = 1
        self.assertNotEqual(review.composition(original), review.composition(swapped))
        self.assertEqual(original['scenario']['party'][0]['build']['essenceIds'], ['rat', 'snake'])

    def test_original_family_adjustment_changes_viability(self):
        self.assertAlmostEqual(review.interval(42, 256, 344)[0], .0946, places=4)
        self.assertLess(review.interval(42, 256, 344)[0], .1)
        self.assertGreater(review.interval(42, 256, 21)[0], .1)
        self.assertAlmostEqual(review.interval(55, 256, 21)[0], .1475, places=4)

    def test_pacing_separates_median_from_nearest_rank_tail(self):
        actual = review.describe([99, 73, 84, 90])
        self.assertEqual(actual['median'], 87)
        self.assertEqual(actual['p90'], 99)
        self.assertIsNone(review.describe([]))

    def test_archive_rejects_changed_manifest_member_and_escape(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            payload = root/'result.json'
            payload.write_text('{}', encoding='utf-8')
            inventory = root/'files.json'
            inventory.write_text(json.dumps({'result.json': review.sha(payload)}), encoding='utf-8')
            entry = dict(source=str(root), manifestSha256=review.sha(inventory))
            archive = review.Archive(entry)
            self.assertEqual(archive.member('result.json'), {})
            payload.write_text('{"changed": true}', encoding='utf-8')
            with self.assertRaises(ValueError):
                archive.member('result.json')
            inventory.write_text(json.dumps({'../outside.json': '0'*64}), encoding='utf-8')
            with self.assertRaises(ValueError):
                review.Archive(entry)
            entry['manifestSha256'] = review.sha(inventory)
            with self.assertRaises(ValueError):
                review.Archive(entry)


if __name__ == '__main__':
    unittest.main()
