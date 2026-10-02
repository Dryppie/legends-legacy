"""Propose every one/two-character Resistance + Health subset on floor twelve.

This only materializes exact recipes. It cannot qualify a runtime, admit a study,
allocate seeds, fight, or change a guardian. Historical controls remain intact.
"""
import argparse
import copy
import importlib.util
import itertools
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
spec = importlib.util.spec_from_file_location('floor12_family_helpers', HERE/'tower-one-healer-family.py')
h = importlib.util.module_from_spec(spec)
spec.loader.exec_module(h)
VERSION = 'floor12-limited-resistance-proposal-v1'
SLOTS = {'Chest', 'Head', 'Legs', 'Necklace'}
SOURCE = ROOT/'TestResults/tower-balance-pass-floor12-expanded-calibration-confirmation-study-20260929'
REVIEW = ROOT/'TestResults/tower-floor12-historical-review-20261002'
PINS = {
    SOURCE/'files.json': '7facc8455060b9c1c97bb0eb3ab36066bf665a9d288d4146b7ecc2156626e0c5',
    SOURCE/'cells.json': '4068f5181954657fc12c8cd0f23399251b94ee3530953eae94593062d53adc06',
    REVIEW/'completion.json': '97bfae565cc25398f9e1943d8e13755f19327503f14ffd250d7ba63234532cba',
    REVIEW/'equipment-review.json': 'b1e86ffd2f96a12baebc933af06d250dfc71154fe91a17a13bd31b5458286d73',
}


def derive(original, parents):
    lookup = {c['id']: c for c in original}
    h.check(len(lookup) == len(original) == 131 and len({h.composition(c) for c in original}) == 9,
            'Complete 131-recipe / nine-composition original family required')
    h.check(len(parents) == len(set(parents)) == 2 and all(p in lookup for p in parents),
            'Two original parents required')
    h.check(len({h.composition(lookup[p]) for p in parents}) == 2, 'Two actual parent compositions required')
    for cell in original:
        scenario = cell['scenario']
        h.check(scenario['floorNumber'] == 12 and scenario['seeds'] == [] and
                [m['partySlot'] for m in scenario['party']] == list(range(1, 11)),
                'Seed-free ordered ten-character floor-twelve recipes required')
        h.check(all(m['build']['characterLevel'] == 60 and m['build']['tier'] == 2 and
                    len(m['build']['essenceIds']) == 7 for m in scenario['party']),
                'Approved level, tier and seven-Essence progression required')
    variants = []
    for parent in parents:
        full = lookup[parent]
        bases = [c for c in original if c['composition'] == full['composition'] and c['gear'] == 'retained-baseline']
        h.check(full['gear'] == 'retained-resistance-and-health' and len(bases) == 1,
                'Unambiguous retained baseline/Resistance + Health pair required')
        baseline = bases[0]
        h.check(not h.specialized(baseline) and h.specialized(full) == [slot for slot in range(1, 11) for _ in range(4)],
                'Exactly four specialized items on each donor character required')
        normalized = copy.deepcopy(full['scenario'])
        for member, base in zip(normalized['party'], baseline['scenario']['party'], strict=True):
            items, base_items = member['build']['equipment'], base['build']['equipment']
            h.check([i['slot'] for i in items] == [i['slot'] for i in base_items] and
                    len({i['slot'] for i in items}) == len(items), 'Raw equipment slots/order changed')
            changed = [i['slot'] for i, b in zip(items, base_items, strict=True) if i != b]
            h.check(len(changed) == 4 and set(changed) == SLOTS, 'Only the four saved defense/health items may differ')
            member['build']['equipment'] = copy.deepcopy(base_items)
        h.check(normalized == baseline['scenario'], 'Parent identity, ordering or non-equipment budget changed')
        for count in (1, 2):
            for subset in itertools.combinations(range(1, 11), count):
                cell = copy.deepcopy(baseline)
                for member, donor in zip(cell['scenario']['party'], full['scenario']['party'], strict=True):
                    if member['partySlot'] in subset:
                        member['build']['equipment'] = copy.deepcopy(donor['build']['equipment'])
                digest = h.signature(cell['scenario'])
                cell.update(id='floor12-limited-resistance/'+digest,
                            gear='limited-resistance-slots-'+'-'.join(map(str, subset)),
                            origin='proposed-floor12-limited-resistance')
                h.check(h.specialized(cell) == [slot for slot in subset for _ in range(4)],
                        'Actual specialized items exceed the declared subset')
                variants.append(dict(sourceId=parent, baselineId=baseline['id'], resistancePartySlots=list(subset),
                                     specializedItems=4*count, scenarioSha256=digest, cell=cell))
    return variants


def propose(original, parents):
    variants = derive(original, parents)
    cells = copy.deepcopy(original)+[v['cell'] for v in variants]
    h.check(len(cells) == len({c['id'] for c in cells}) == len({h.signature(c['scenario']) for c in cells}) == 241,
            'Duplicate recipe or raw identity')
    eligible = sum(len(h.specialized(c)) <= 8 and len(set(h.specialized(c))) <= 2 for c in cells)
    h.check(eligible == 128, 'All 18 original eligible controls and 110 limited variants required')
    return dict(version=VERSION, status='ProposedNotPrepared', floor=12, retainedCells=131, newVariants=110,
                totalCells=241, actualCompositions=9, eligibleRecipes=128, equipmentEligibility=copy.deepcopy(h.EQUIPMENT),
                parents=list(parents), variants=variants, cells=cells, currentRuntimeQualified=False,
                usedForAcceptance=False, newFights=0, newSeeds=0, nativePreparations=0)


def validate(proposal, original, parents):
    # Reconstruct from immutable parents; never trust a submitted subset or its counts.
    expected = propose(original, parents)
    h.check(proposal == expected, 'Complete proposal, raw recipes, controls or unallocated status changed')
    return proposal['cells']


def write(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    h.check(output.parent == ROOT/'TestResults' and not output.exists(), 'Fresh direct TestResults child required')
    for path, pin in PINS.items():
        h.check(h.sha(path) == pin, 'Historical proposal input changed: '+str(path))
    review, equipment = h.read(REVIEW/'completion.json'), h.read(REVIEW/'equipment-review.json')
    h.check(review['status'] == 'HistoricalReviewCompleted' and review['historicalReports'] == 19912 and
            not review['currentRuntimeQualified'], 'Closed historical review required')
    h.check([p['fullResult']['wins'] for p in equipment['pairs']] == [53, 36] and
            [p['baselineResult']['wins'] for p in equipment['pairs']] == [0, 0], 'Historical parent selection changed')
    parents = [p['fullId'] for p in equipment['pairs']]
    original = h.read(SOURCE/'cells.json')
    proposal = propose(original, parents)
    validate(proposal, original, parents)
    pins = {str(p): h.sha(p) for p in [*PINS, Path(__file__), Path(h.__file__)]}
    output.mkdir()
    write(output/'proposal.json', proposal)
    for path, pin in pins.items():
        h.check(h.sha(path) == pin, 'Proposal input changed during derivation')
    write(output/'completion.json', dict(status='ProposedNotPrepared', source=str(SOURCE), sourcePins=pins,
          proposalSha256=h.sha(output/'proposal.json'), retainedCells=131, newVariants=110, totalCells=241,
          actualCompositions=9, eligibleRecipes=128, newFights=0, newSeeds=0, historicalReplays=0,
          nativePreparations=0, currentRuntimeQualified=False, studyAdmitted=False, gameContentChanged=False,
          next='After floor-ten closure, qualify all 131 original recipes on the accepted catalogs and current runtime. '
               'Then prepare and independently admit this complete family before selecting any finite study or guardian change.'))
    print('Proposed 241 exact floor-12 recipes: 131 controls + 110 limited-equipment variants; no combat or seed allocation.')


if __name__ == '__main__':
    main()
