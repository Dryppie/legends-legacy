"""Audit saved selection margins and catalogue unconfirmed fixed outputs.

Reads authenticated JSON only. Never constructs a party, derives a value, loads a
gameplay runtime or extends a closed experiment. Missing outcomes remain missing.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
SELECTOR_PATH = ROOT/'Balance Harness/analysis/practical-selection-ties.py'
spec = importlib.util.spec_from_file_location('selector', SELECTOR_PATH)
selector = importlib.util.module_from_spec(spec)
spec.loader.exec_module(selector)
require = selector.require
PRIMARY, SECOND = selector.CONFIRMED, selector.ANCHOR
COMPARISON = ROOT/'TestResults/balance/tower-incumbent-tie-comparison-20260922'
COMPARISON_PIN = 'c078df5d9f3f3074c763153e183c3544c462aefb50302867a5d854a3f9c4f909'
PRACTICAL = ROOT/'TestResults/balance/tower-practical-incumbent-tie-20260922'
PRACTICAL_PIN = 'ce03b27bb0b9ba96b6229be85849b1885af4f75e322876bb7877e38d67cadcb6'


def paired(left, right):
    require(left and left.keys() == right.keys(), 'Missing or unpaired observations')
    require(all(type(v) is bool for v in [*left.values(), *right.values()]), 'Non-boolean outcome')
    gains = sum(left[s] and not right[s] for s in left)
    losses = sum(right[s] and not left[s] for s in left)
    return dict(samples=len(left), selectedWins=sum(left.values()), incumbentWins=sum(right.values()),
                gains=gains, losses=losses, difference=(gains-losses)/len(left))


def confirmation(selected, evidence):
    if selected in evidence and PRIMARY in evidence:
        return dict(status='Measured', **paired(evidence[selected], evidence[PRIMARY]))
    return dict(status='SameRecipeUnmeasured' if selected == PRIMARY else 'Unmeasured',
                samples=None, selectedWins=None, incumbentWins=None, gains=None, losses=None,
                difference=0 if selected == PRIMARY else None)


def describe(cohort, restart, arm, shortlist, rows, selected, evidence):
    ids = [p['id'] for p in shortlist]
    require({PRIMARY, SECOND} <= set(ids) and len(ids) == len(set(ids)) == 4, 'Changed supplied nominees')
    target = selector.retain_tied_primary(rows, ids, {PRIMARY, SECOND}, PRIMARY)
    by_id = {r['id']:r for r in rows}
    require(selected in by_id and all(len(r['cells']) == 1 and selector.samples(r) == 32 for r in rows), 'Changed selection scope')
    vectors = {pid:dict(enumerate(r['cells'][0]['clears'])) for pid,r in by_id.items()}
    selection = paired(vectors[selected], vectors[PRIMARY])
    margin = selection['selectedWins']-selection['incumbentWins']
    require(selection['selectedWins'] == max(selector.wins(r) for r in rows), 'Selected output is not a maximum')
    return dict(cohort=cohort, restart=restart, arm=arm, selected=selected,
                incumbentTieSelected=target, selectedSupplied=selected in (PRIMARY, SECOND),
                selectionMargin=margin, selection=selection,
                strictDisplacement=selected != PRIMARY and margin > 0,
                uniqueLeader=sum(selector.wins(r) == selection['selectedWins'] for r in rows) == 1,
                confirmation=confirmation(selected, evidence),
                nominees=[dict(partyId=p, selectionWins=selector.wins(by_id[p]),
                               confirmationWins=sum(evidence[p].values()) if p in evidence else None) for p in ids])


def run(output):
    require(not output.exists(), 'Use a new analysis directory; archives are read-only')
    started = time.monotonic()
    inputs = selector.Inputs()
    rows, panels, recipes, all_holdouts = [], {}, {}, set()

    def record_panel(group, evidence):
        if not evidence:
            return
        seeds = set(next(iter(evidence.values())))
        require(len(seeds) == 1000 and all(set(v) == seeds for v in evidence.values()), 'Changed confirmation panel')
        if group in panels:
            require(panels[group] == seeds, 'Changed paired arm panel')
        else:
            require(not all_holdouts.intersection(seeds), 'Unexpected overlap between study panels')
            panels[group] = seeds
            all_holdouts.update(seeds)
        for pid, values in evidence.items():
            key = (group, pid)
            require(key not in recipes or recipes[key] == values, 'Inconsistent repeated recipe/panel')
            recipes[key] = values

    # Full census of the two previously audited three-pair comparisons.
    for cohort, package_pin in selector.PACKAGE_PINS.items():
        package = ROOT/f'TestResults/{cohort}-comparison-20260917'
        base = ROOT/f'TestResults/balance/tower-{cohort}-comparison-20260917'
        package_files = inputs.read(package/'files.json', package_pin)
        files = inputs.read(package/'native-files.json', package_files['native-files.json'])
        for index in range(6):
            prefix = f'study-{index}/'
            study_files = inputs.read(base/prefix/'files.json', files[prefix+'files.json'])

            def read(name):
                require(study_files[name] == files[prefix+name], 'Conflicting nested inventory')
                return inputs.read(base/prefix/name, study_files[name])

            old, evidence = selector.analyze(read)
            record_panel(f'{cohort}-{index//2+1}', evidence)
            rows.append(describe(cohort, index//2+1, 'baseline' if index % 2 == 0 else cohort,
                                 read('discovery-shortlist.json'), read('selection-results.json'), old['selected'], evidence))

    # Latest practical output: its actual selector already has the designation.
    files = inputs.read(PRACTICAL/'files.json', PRACTICAL_PIN)
    study_files = inputs.read(PRACTICAL/'study/files.json', files['study/files.json'])

    def practical(name):
        require(study_files[name] == files['study/'+name], 'Conflicting practical inventory')
        return inputs.read(PRACTICAL/'study'/name, study_files[name])

    report, definition = practical('study.json'), practical('definition.json')
    supplied = {s['referenceId']:s['party']['id'] for s in definition['starts']}
    evidence = {}
    by_cell = {e['cellId']:selector.panel(e['trials']) for e in report['evidence']}
    for member in report['confirmation']['members']:
        for pid in set(member['generatedIds']) | {supplied[r] for r in member['referenceIds']}:
            evidence[pid] = by_cell[member['cellId']]
    result = inputs.read(PRACTICAL/'result.json', files['result.json'])
    record_panel('practical-1', evidence)
    row = describe('practical', 1, 'incumbent-tie', practical('discovery-shortlist.json'),
                   practical('selection-results.json'), result['selectedPartyId'], evidence)
    require(row['selected'] == row['incumbentTieSelected'], 'Current practical selector mismatch')
    rows.append(row)

    # All 24 common searches, counting the incumbent-tie output once per root.
    files = inputs.read(COMPARISON/'files.json', COMPARISON_PIN)
    study_files = inputs.read(COMPARISON/'study/files.json', files['study/files.json'])

    def comparison(name):
        require(study_files[name] == files['study/'+name], 'Conflicting comparison inventory')
        return inputs.read(COMPARISON/'study'/name, study_files[name])

    frozen, report = comparison('outputs-freeze.json'), comparison('study.json')
    require(report['freeze'] == frozen and len(frozen['searches']) == 24, 'Changed common search freeze')
    results = inputs.read(COMPARISON/'result.json', files['result.json'])
    template = inputs.read(COMPARISON/'template.json', files['template.json'])
    measured = {e['restart']:e for e in report['evidence']}
    catalogue = []
    for search, result in zip(frozen['searches'], results['pairs'], strict=True):
        index = search['restart']
        require(index == result['restart'], 'Reordered common searches')
        shortlist, selection = search['discovery']['discoveryShortlist'], search['selection']
        ids = [p['id'] for p in shortlist]
        baseline, selected = search['baseline']['finalist']['party']['id'], search['candidate']['finalist']['party']['id']
        require(baseline == selector.select(selection, ids)
                and selected == selector.retain_tied_primary(selection, ids, {PRIMARY, SECOND}, PRIMARY)
                and baseline == result['baselineParty'] and selected == result['candidateParty'], 'Frozen selector mismatch')
        evidence = {}
        if baseline != selected:
            require(index in measured and not result['identical'], 'Missing differing-output confirmation')
            evidence = {baseline:selector.panel(measured[index]['baseline']), selected:selector.panel(measured[index]['candidate'])}
            require(sum(evidence[baseline].values()) == result['baselineWins']
                    and sum(evidence[selected].values()) == result['candidateWins'], 'Changed frozen pair counts')
        else:
            require(index not in measured and result['identical'] and result['baselineWins'] is None
                    and result['candidateWins'] is None, 'Identical-output branch invents measurements')
        record_panel(f'tie-{index}', evidence)
        row = describe('tie-comparison', index, 'incumbent-tie', shortlist, selection, selected, evidence)
        rows.append(row)
        if row['strictDisplacement'] and not row['selectedSupplied'] and row['confirmation']['status'] == 'Unmeasured':
            scenario = search['candidate']['scenario']
            builds = {str(p['partySlot']):p['build']['essenceIds'] for p in scenario['party']}
            require(not scenario['seeds'] and builds == search['candidate']['finalist']['party']['builds'], 'Changed seed-free recipe')
            catalogue.append(dict(partyId=selected, sourceRestart=index, selection=row['selection'],
                                  sourceRecipeHash=search['candidate']['recipeHash'], scenario=copy.deepcopy(scenario)))
    require([s['restart'] for s in frozen['searches']] == list(range(1,25)), 'Missing common root')
    require(len(catalogue) == len({c['partyId'] for c in catalogue}) == 6, 'Changed unmeasured fixed-output census')
    known = {pid for _, pid in recipes}
    require(not known.intersection(c['partyId'] for c in catalogue), 'Catalogue includes a measured recipe in this census')
    reference = next(r for r in template['references'] if r['id'] == 'confirmed-399bc7760fb0cf790a5d8ac4')
    require(not reference['scenario']['seeds'], 'Scheduled reference export')
    reference_parties = {s['referenceId']:s['party']['id'] for s in template['starts']}
    require(set(reference_parties.values()) == {PRIMARY, SECOND}, 'Changed fixed reference family')
    reference_scenarios = [dict(referenceId=r['id'], partyId=reference_parties[r['id']], scenario=r['scenario'])
                           for r in template['references']]
    require(all(not r['scenario']['seeds'] for r in reference_scenarios), 'Scheduled control export')
    primary_builds = {p['partySlot']:set(p['build']['essenceIds']) for p in reference['scenario']['party']}
    for candidate in catalogue:
        candidate['changesFromPrimary'] = [dict(partySlot=p['partySlot'],
             removed=sorted(primary_builds[p['partySlot']]-set(p['build']['essenceIds'])),
             added=sorted(set(p['build']['essenceIds'])-primary_builds[p['partySlot']]))
             for p in candidate['scenario']['party'] if set(p['build']['essenceIds']) != primary_builds[p['partySlot']]]
    strict = [r for r in rows if r['strictDisplacement']]
    summary = dict(status='VerifiedSavedSelectionMargins', searches=len(rows), comparisonArms=12,
                   commonSelectorSearches=24, latestPracticalSearches=1, distinctMeasuredPanels=len(panels),
                   distinctMeasuredRecipePanels=len(recipes), strictDisplacements=len(strict),
                   measuredStrictDisplacements=sum(r['confirmation']['status'] == 'Measured' for r in strict),
                   unmeasuredStrictDisplacements=sum(r['confirmation']['status'] == 'Unmeasured' for r in strict),
                   unmeasuredNovelOutputs=len(catalogue),
                   measuredNomineeOccurrences=sum(n['confirmationWins'] is not None for r in rows for n in r['nominees']),
                   unmeasuredNomineeOccurrences=sum(n['confirmationWins'] is None for r in rows for n in r['nominees']),
                   oneWinDisplacements=sum(r['selectionMargin'] == 1 for r in strict),
                   newFights=0, newValues=0, newParties=0, nativePreparations=0,
                   interpretation='Retrospective coverage and arithmetic only; no pooled quality estimate, retuned threshold, adoption or experiment extension')
    catalogue = dict(status='FrozenRecipesNeedNewProspectiveProtocol', sourceManifestSha256=COMPARISON_PIN,
                     eligibility='Every novel common output that strictly led the designated incumbent and lacked confirmation in the closed 24-root comparison',
                     contentHashes=template['contentHashes'], settingsHash=template['settingsHash'],
                     sourceExecutionHash=template['executionHash'], budget=template['budget'], ownedCopies=template['ownedCopies'],
                     primaryReferenceId=reference['id'], primaryPartyId=PRIMARY, primaryScenario=reference['scenario'],
                     references=reference_scenarios, candidates=catalogue,
                     seedsAllocated=0, runnableRequest=False, adoptionAuthorized=False)
    source_pins = {str(path):selector.sha(path) for path in [Path(__file__), SELECTOR_PATH,
                   ROOT/'LL/tools/BalanceHarness/TowerBossStudyPolicy.cs', ROOT/'LL/tools/BalanceHarness/TowerPracticalSearch.cs']}
    for path, digest in {**inputs.consumed, **source_pins}.items():
        require(selector.sha(Path(path)) == digest, 'Input changed during analysis')
    output.mkdir(parents=True)

    def save(name, value):
        with (output/name).open('x', encoding='utf-8') as stream:
            json.dump(value, stream, indent=2, allow_nan=False)

    save('summary.json', summary)
    save('searches.json', rows)
    save('candidate-catalogue.json', catalogue)
    save('inputs.json', inputs.consumed)
    save('source-files.json', source_pins)
    save('completion.json', dict(status=summary['status'], seconds=time.monotonic()-started, authenticatedInputs=len(inputs.consumed)))
    (output/'analyzer.py').write_bytes(Path(__file__).read_bytes())
    (output/'selector.py').write_bytes(SELECTOR_PATH.read_bytes())
    save('files.json', {p.name:selector.sha(p) for p in output.iterdir() if p.is_file()})
    print(json.dumps(dict(summary, manifestSha256=selector.sha(output/'files.json')), indent=2))


class ArithmeticTests(unittest.TestCase):
    def test_pairing_uses_shared_keys(self):
        self.assertEqual(paired({1:True,2:True,3:False}, {3:True,2:False,1:False})['gains'], 2)
        self.assertEqual(paired({1:True,2:True,3:False}, {3:True,2:False,1:False})['losses'], 1)

    def test_missing_strict_output_is_not_zero(self):
        row = confirmation('challenger', {PRIMARY:{1:True}})
        self.assertEqual(row['status'], 'Unmeasured')
        self.assertIsNone(row['difference'])
        self.assertIsNone(row['samples'])

    def test_identical_unmeasured_output_has_no_absolute_wins(self):
        row = confirmation(PRIMARY, {})
        self.assertEqual(row['difference'], 0)
        self.assertIsNone(row['selectedWins'])
        self.assertIsNone(row['samples'])

    def test_unequal_or_invalid_panels_fail(self):
        for a,b in [({1:True},{2:False}), ({},{}) , ({1:1},{1:True})]:
            with self.assertRaises(ValueError): paired(a,b)

    def test_measured_same_recipe_retains_absolute_evidence(self):
        row = confirmation(PRIMARY, {PRIMARY:{1:True,2:False}})
        self.assertEqual((row['status'],row['selectedWins'],row['difference']), ('Measured',1,0))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--self-test', action='store_true')
    group.add_argument('--output', type=Path)
    args = parser.parse_args()
    if args.self_test:
        outcome = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(ArithmeticTests))
        raise SystemExit(not outcome.wasSuccessful())
    run(args.output.resolve())
