"""Safeguards for a fixed-family penetration/offense experiment."""
import copy
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('tower_pass', Path(__file__).with_name('run-tower-balance-pass.py'))
owner = importlib.util.module_from_spec(spec); spec.loader.exec_module(owner)


class PenetrationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.source = self.root/'source'; self.candidate = self.root/'candidate'
        (self.source/'Data/world-tower').mkdir(parents=True)
        self.tower = dict(version=1, floors=[dict(floorNumber=i, guardianScaling=dict(
            health=2, offense=5, defense=2.33, resistance=2.33, penetration=1, regeneration=1)) for i in (4, 5)])
        (self.source/'Data/world-tower/tower-floors.json').write_text(json.dumps(self.tower))
        (self.source/'Data/abilities.json').write_text('[{"coefficient":2,"target":"absolute"}]')
        (self.source/'appsettings.json').write_text('{"settings":1}')
        shutil.copytree(self.source, self.candidate)
        self.plan = dict(source='saved-source', sourceManifestSha256='a'*64, floor=4, offenseFactor=.68, penetrationFactor=100)
        value = copy.deepcopy(self.tower); value['floors'][0]['guardianScaling'].update(offense=3.4, penetration=100)
        self.save(value)

    def save(self, value):
        (self.candidate/'Data/world-tower/tower-floors.json').write_text(json.dumps(value))

    def test_valid_variant_preserves_source_and_other_fields(self):
        before = (self.source/'Data/world-tower/tower-floors.json').read_bytes()
        self.assertEqual(owner.verify_penetration_candidate(self.source, self.candidate, self.plan),
                         dict(floor=4, offenseFactor=.68, penetrationFactor=100))
        self.assertEqual((self.source/'Data/world-tower/tower-floors.json').read_bytes(), before)

    def test_existing_health_offense_scaling_stays_compatible(self):
        original = dict(health=2, offense=5, defense=2.33)
        self.assertEqual(owner.scaled_guardian(original, 1.1, .9), dict(health=2.2, offense=4.5, defense=2.33))
        self.assertNotIn('penetration', original)

    def test_factor_rejects_nonfinite_decreasing_or_excessive_values(self):
        for factor in (0, .99, -1, 128.01, float('nan'), float('inf')):
            with self.subTest(factor=factor), self.assertRaises(ValueError):
                owner.scaled_guardian(dict(health=2, offense=5, penetration=1), 1, .68, factor)

    def test_authored_penetration_must_be_present_positive_and_finite(self):
        for p in (None, 0, -1, float('inf'), float('nan')):
            source = dict(health=2, offense=5)
            if p is not None: source['penetration'] = p
            with self.subTest(value=p), self.assertRaises(ValueError): owner.scaled_guardian(source, 1, .68, 100)

    def test_overflow_and_rounded_noop_rejected(self):
        for p in (1e308, 1e-300):
            with self.subTest(value=p), self.assertRaises(ValueError):
                owner.scaled_guardian(dict(health=2, offense=5, penetration=p), 1, .68, 100)

    def test_only_fixed_source_modes_with_lower_offense_are_allowed(self):
        for mode in ('screen', 'confirm'):
            owner.validate_penetration_mode(mode, self.source, 100, 1, .68, None, [], False, [])
        for mode, source, health, offense in [('prepare',self.source,1,.68),('search',self.source,1,.68),
                ('screen',None,1,.68),('screen',self.source,.9,.68),('screen',self.source,1,1),
                ('screen',self.source,1,1.1),('screen',self.source,1,.24)]:
            with self.subTest(mode=mode,health=health,offense=offense), self.assertRaises(ValueError):
                owner.validate_penetration_mode(mode, source, 100, health, offense, None, [], False, [])

    def test_ability_import_refresh_and_projection_combinations_rejected(self):
        for ability, imports, current, projections in [('candidate',[],False,[]),(None,['source'],False,[]),
                                                      (None,[],True,[]),(None,[],False,['gear'])]:
            with self.subTest(ability=ability,imports=imports,current=current,projections=projections), self.assertRaises(ValueError):
                owner.validate_penetration_mode('screen', self.source, 100, 1, .68, ability, imports, current, projections)

    def test_changed_guardian_health_defenses_regeneration_or_other_floor_rejected(self):
        expected = owner.read(self.candidate/'Data/world-tower/tower-floors.json')
        for floor, field in [(0,'health'),(0,'defense'),(0,'resistance'),(0,'regeneration'),(1,'offense'),(1,'penetration')]:
            changed = copy.deepcopy(expected); changed['floors'][floor]['guardianScaling'][field] += 1; self.save(changed)
            with self.subTest(floor=floor,field=field), self.assertRaisesRegex(ValueError,'unrelated Tower'):
                owner.verify_penetration_candidate(self.source,self.candidate,self.plan)

    def test_wrong_scalars_or_tower_metadata_rejected(self):
        expected = owner.read(self.candidate/'Data/world-tower/tower-floors.json')
        for field in ('offense','penetration','version'):
            changed = copy.deepcopy(expected)
            if field == 'version': changed[field] = 2
            else: changed['floors'][0]['guardianScaling'][field] += .01
            self.save(changed)
            with self.subTest(field=field), self.assertRaises(ValueError):
                owner.verify_penetration_candidate(self.source,self.candidate,self.plan)

    def test_other_catalog_bytes_and_catalog_set_must_match(self):
        path = self.candidate/'Data/abilities.json'; original = path.read_text()
        path.write_text(original+'\n')
        with self.assertRaisesRegex(ValueError,'another catalog'): owner.verify_penetration_candidate(self.source,self.candidate,self.plan)
        path.write_text(original); extra = self.candidate/'Data/extra.json'; extra.write_text('{}')
        with self.assertRaisesRegex(ValueError,'catalog set'): owner.verify_penetration_candidate(self.source,self.candidate,self.plan)
        extra.unlink(); path.unlink()
        with self.assertRaisesRegex(ValueError,'catalog set'): owner.verify_penetration_candidate(self.source,self.candidate,self.plan)

    def test_settings_must_match(self):
        (self.candidate/'appsettings.json').write_text('{}')
        with self.assertRaisesRegex(ValueError,'settings'): owner.verify_penetration_candidate(self.source,self.candidate,self.plan)

    def test_plan_fields_floor_and_noop_are_rejected(self):
        variants = [{**self.plan,'healthFactor':.9},{**self.plan,'floor':7},{**self.plan,'penetrationFactor':1}]
        missing = self.plan.copy(); del missing['sourceManifestSha256']; variants.append(missing)
        for plan in variants:
            with self.subTest(plan=plan), self.assertRaises(ValueError): owner.verify_penetration_candidate(self.source,self.candidate,plan)
        duplicate = copy.deepcopy(self.tower); duplicate['floors'].append(copy.deepcopy(duplicate['floors'][0]))
        (self.source/'Data/world-tower/tower-floors.json').write_text(json.dumps(duplicate))
        with self.assertRaisesRegex(ValueError,'Exactly one'): owner.verify_penetration_candidate(self.source,self.candidate,self.plan)


if __name__ == '__main__': unittest.main()
