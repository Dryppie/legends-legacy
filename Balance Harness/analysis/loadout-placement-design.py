"""Bounded structural design review. No battle runner, RNG, or policy registration.

Enumerate complete loadout permutations in one five-owner subgroup at a time.
The saved catalogue is development evidence, not a search result or runtime plan.
"""
import argparse
from collections import Counter
import copy
import hashlib
from itertools import combinations, permutations
import json
import os
from pathlib import Path
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-loadout-placement-design-v1'
SECONDS, BYTES = 180, 64 * 1048576
GROUPS = (('1', '2', '3', '4', '5'), ('6', '7', '8', '9', '10'))
PRIOR = 'TestResults/affinity-neighborhood-recognition-execution-handoff-20260924.json'
PRIOR_PIN = '3b62b9a468387f2b7c1d7f1888e63b1041722e8b8cfe6c6b68f221cab4b23110'
INPUTS = {
    PRIOR: PRIOR_PIN,
    'Balance Harness/Tower-Affinity-Neighborhood-Recognition-Plan.json':
        'a0b4a5dbc8fe7c65d4df80bfcb17b8f5623343cef7e0c1a1f2506648b3e2fc2b',
    'TestResults/affinity-allied-action-preview-20260924-v2/root-01/request.json':
        '58ad6bbc09f0a126c40c08bd07e9a0bfadf98168d7c9c180c2f2bf20bf5a9f65',
}
SOURCE_FILES = [
    'LL/tools/BalanceHarness/TowerPartyCoverage.cs',
    'LL/tools/BalanceHarness/TowerBossPartyGenerator.cs',
    'LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs',
    'LL/tools/BalanceHarness/TowerAdaptiveRacingGenerator.cs',
    'LL/tools/BalanceHarness/TowerProposalPolicies.cs',
    'LL/tools/BalanceHarness/TowerBenchmarkValidation.cs',
    'LL/tools/BalanceHarness/TowerBattleRunner.cs',
    'LL/src/Core/Domain/Models/WorldTower/WorldTowerPartyRules.cs',
]


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def digest(value):
    return sha(json.dumps(value, sort_keys=True, separators=(',', ':'),
                          ensure_ascii=True, allow_nan=False).encode())


def decode(raw):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            require(key not in result, 'Duplicate JSON property')
            result[key] = value
        return result

    def invalid(value):
        raise ValueError('Nonfinite JSON number: ' + value)
    return json.loads(raw, object_pairs_hook=unique, parse_constant=invalid)


def validate(builds, scope):
    require(scope['requiredPartySize'] == 10 and scope['budget']['essenceSlots'] == 5,
            'This design requires ten owners and five Essence slots')
    require(set(builds) == {str(i) for i in range(1, 11)}, 'Invalid owners')
    allowed = {e['id']: e['family'].casefold() for e in scope['allowedEssences']}
    require(len(allowed) == len(scope['allowedEssences']), 'Duplicate allowed Essence')
    for ids in builds.values():
        require(len(ids) == 5 and ids == sorted(set(ids)) and all(e in allowed for e in ids),
                'Invalid canonical loadout')
        require(len({allowed[e] for e in ids}) == 5, 'Duplicate Essence family')
    counts = Counter(e for ids in builds.values() for e in ids)
    require(scope['ownedCopies'] is None or all(n <= scope['ownedCopies'].get(e, 0)
            for e, n in counts.items()), 'Owned-copy limit exceeded')


def inventory(builds, group):
    return Counter(e for owner in group for e in builds[owner])


def apply_assignment(parent, group, sources):
    require(group in GROUPS and len(sources) == len(group) and set(sources) == set(group),
            'Assignment must be a bijection inside one production subgroup')
    builds = copy.deepcopy(parent)
    for destination, source in zip(group, sources):
        builds[destination] = list(parent[source])
    return builds


def scenario_for(anchor, builds):
    scenario = copy.deepcopy(anchor)
    scenario['seeds'] = []
    require({p['partySlot'] for p in scenario['party']} == set(range(1, 11))
            and len(scenario['party']) == 10, 'Changed physical actor layout')
    for actor in scenario['party']:
        actor['build']['essenceIds'] = list(builds[str(actor['partySlot'])])
    return scenario


def enumerate_loadouts(parent, scope, excluded=()):
    validate(parent, scope)
    parent_id, recipes, accounting = digest(parent), {}, Counter()
    for group_index, group in enumerate(GROUPS, 1):
        for sources in permutations(group):
            accounting['assignmentsExamined'] += 1
            builds = apply_assignment(parent, group, sources)
            validate(builds, scope)
            require(all(inventory(builds, g) == inventory(parent, g) for g in GROUPS),
                    'Subgroup inventory changed')
            require(Counter(tuple(builds[o]) for o in group) == Counter(tuple(parent[o]) for o in group),
                    'Loadout bundle changed')
            pid = digest(builds)
            if pid == parent_id:
                accounting['identityAssignments'] += 1
                continue
            if pid in excluded:
                accounting['referenceAssignments'] += 1
                continue
            changed = [int(o) for o in group if builds[o] != parent[o]]
            row = recipes.setdefault(pid, dict(partyId=pid, builds=builds,
                changedOwners=changed, subgroup=group_index, assignments=[]))
            require(row['builds'] == builds, 'Recipe hash collision')
            row['assignments'].append(dict(zip(group, sources)))
    ordered = [recipes[k] for k in sorted(recipes)]
    accounting['distinctRecipes'] = len(ordered)
    accounting['duplicateAssignments'] = sum(len(r['assignments']) - 1 for r in ordered)
    require(accounting['assignmentsExamined'] == 240, 'Incomplete permutation census')
    return ordered, dict(sorted(accounting.items()))


def single_swap_ids(parent, scope):
    """Complete legal same-subgroup single swaps; not the legacy random sampler."""
    validate(parent, scope)
    found = set()
    for group in GROUPS:
        for a, b in combinations(group, 2):
            for left in range(5):
                for right in range(5):
                    if parent[a][left] == parent[b][right]:
                        continue
                    builds = copy.deepcopy(parent)
                    builds[a][left], builds[b][right] = builds[b][right], builds[a][left]
                    builds[a].sort()
                    builds[b].sort()
                    try:
                        validate(builds, scope)
                    except ValueError:
                        continue
                    found.add(digest(builds))
    return found


def analyze(request, plan):
    context, scope = request['context'], request['context']['scope']
    starts = scope['starts']
    benchmark = next(s['party'] for s in starts if s['referenceId'] == context['benchmarkReferenceId'])
    parent = benchmark['builds']
    require(benchmark['id'] == digest(parent) == plan['benchmarkPartyId'], 'Changed parent identity')
    anchor = next(t['scenario'] for t in plan['teams'] if t['partyId'] == benchmark['id'])
    require({str(p['partySlot']): p['build']['essenceIds'] for p in anchor['party']} == parent,
            'Physical benchmark differs from composition')
    require(anchor['seeds'] == [], 'Development fixture must be seed-free')
    references = {s['party']['id'] for s in starts}
    require(len(references) == 3 and references == {t['partyId'] for t in plan['teams']
            if t['membership'] == 'reference'}, 'Changed reference set')
    recipes, accounting = enumerate_loadouts(parent, scope, references)
    swaps = single_swap_ids(parent, scope)
    old = {t['partyId'] for t in plan['teams'] if t['membership'] != 'reference'}
    for row in recipes:
        row['seedFreeScenarioHash'] = digest(scenario_for(anchor, row['builds']))
        row['reachableBySingleWithinSubgroupSwap'] = row['partyId'] in swaps
        row['replacedAssignments'] = sum(len(set(parent[o]) - set(row['builds'][o])) for o in parent)
    fixture = dict(scope={k: scope[k] for k in ('requiredPartySize', 'budget', 'allowedEssences', 'ownedCopies')},
        parent=parent, benchmarkPartyId=benchmark['id'], referencePartyIds=sorted(references), anchor=anchor)
    summary = dict(version=VERSION, accounting=accounting, distinctSingleSwapRecipes=len(swaps),
        permutationOverlapWithSingleSwap=len({r['partyId'] for r in recipes} & swaps),
        permutationOverlapWithRetiredNeighborhood=sorted({r['partyId'] for r in recipes} & old),
        changedOwnerCounts=dict(sorted(Counter(len(r['changedOwners']) for r in recipes).items())),
        subgroupCounts=dict(sorted(Counter(r['subgroup'] for r in recipes).items())),
        minimumDistinctRequired=17, fillsNinePlusEight=len(recipes) >= 17,
        proposedSampling='UniformDistinctRecipeWithoutReplacementAcrossBothWaves',
        status='ReadyForNativeCombatFreePreview' if len(recipes) >= 17 else 'InsufficientDistinctRecipesStop',
        performanceClaim=False, newFights=0, newValues=0, newEntropyDraws=0)
    return fixture, recipes, summary


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    out = parser.parse_args().output.resolve()
    require(not out.exists() and out.is_relative_to(ROOT / 'TestResults'), 'Existing or unsafe output')
    out.mkdir()
    started = time.monotonic()

    def save(name, value):
        raw = (json.dumps(value, indent=2, allow_nan=False) + '\n').encode()
        require(sum(p.stat().st_size for p in out.rglob('*') if p.is_file()) + len(raw) < BYTES,
                'Review storage allowance exhausted')
        with (out / name).open('xb') as stream:
            stream.write(raw)

    # Persist the full charge and fixed mechanism before consuming structural inputs.
    save('declaration.json', dict(version=VERSION, maximumSeconds=SECONDS, maximumBytes=BYTES,
        charge='FullAllowanceAtStartIncludingFailure', inputPins=INPUTS,
        analysisSha256=sha(Path(__file__).read_bytes()),
        sourcePins={p: sha((ROOT / p).read_bytes()) for p in SOURCE_FILES},
        mechanism='AllWholeLoadoutPermutationsInExactlyOneFiveOwnerSubgroup',
        comparison='CompleteSingleEssenceSwapSetWithinSubgroupOnly',
        decision='RecommendCombatFreeNativePreviewIffAtLeast17DistinctNonreferenceRecipes',
        rankByOutcomes=False, sampleSeeds=False, newFights=0, newValues=0, newEntropyDraws=0))
    timer = threading.Timer(SECONDS, lambda: os._exit(124))
    timer.daemon = True
    timer.start()
    try:
        values = {}
        for path, pin in INPUTS.items():
            raw = (ROOT / path).read_bytes()
            require(sha(raw) == pin, 'Changed input: ' + path)
            values[path] = decode(raw)
        prior = values[PRIOR]
        for path, pin in prior['preservedHistoricalPins'].items():
            require(sha((ROOT / path).read_bytes()) == pin, 'Changed historical pin: ' + path)
        fixture, recipes, summary = analyze(values[next(k for k in INPUTS if k.endswith('/request.json'))],
                                            values[next(k for k in INPUTS if k.endswith('-Plan.json'))])
        save('fixture.json', fixture)
        save('catalogue.json', recipes)
        summary.update(chargedSeconds=SECONDS, chargedBytes=BYTES,
            cumulativeRecordedSeconds=prior['cumulativeRecordedSeconds'] + SECONDS,
            cumulativeRecordedBytes=prior['cumulativeRecordedBytes'] + BYTES,
            cumulativeDeclaredMaximumSeconds=prior['cumulativeDeclaredMaximumSeconds'] + SECONDS,
            cumulativeDeclaredMaximumBytes=prior['cumulativeDeclaredMaximumBytes'] + BYTES,
            preservedHistoricalPins=len(prior['preservedHistoricalPins']),
            lastVerifiedHistory=prior['history'], liveHistoryRescanned=False,
            secondsBeforeSealing=time.monotonic() - started)
        save('summary.json', summary)
        (out / 'analysis.py').write_bytes(Path(__file__).read_bytes())
        declaration = decode((out / 'declaration.json').read_bytes())
        for path, pin in {**INPUTS, **declaration['sourcePins'], **prior['preservedHistoricalPins']}.items():
            require(sha((ROOT / path).read_bytes()) == pin, 'Source changed during review: ' + path)
        require(time.monotonic() - started < SECONDS, 'Review time allowance exhausted')
        save('files.json', {p.name: sha(p.read_bytes()) for p in sorted(out.iterdir())})
        print(json.dumps(summary, indent=2))
    except BaseException as error:
        save('failure.json', dict(reason=str(error), fullAllowanceCharged=True,
                                  seconds=time.monotonic() - started))
        raise
    finally:
        timer.cancel()


if __name__ == '__main__':
    main()
