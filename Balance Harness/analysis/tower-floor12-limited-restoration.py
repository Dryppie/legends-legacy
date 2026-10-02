"""Exact six/eight-item Restoration subsets for the complete floor-twelve family.

This data-only proposal preserves all 241 controls. Native preparation and a
separately declared diagnostic are required before testing any new recipe.
"""
import argparse
import copy
import importlib.util
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
spec = importlib.util.spec_from_file_location('floor12_restoration_family', HERE/'tower-one-healer-family.py')
h = importlib.util.module_from_spec(spec)
spec.loader.exec_module(h)
VERSION = 'floor12-limited-restoration-proposal-v1'
FOUR_SLOTS = {'MainHand', 'Chest', 'Head', 'Necklace'}
SOURCE = ROOT/'TestResults/tower-balance-pass-floor12-limited-resistance-diagnostic-study-20260929'
CONTROL = ROOT/'TestResults/tower-floor12-pressure-midpoint-driver-20261002'
SOURCE_MANIFEST = '7a7406fd058b865983997c471bf17d30961c1c0d24b2f3a85a1bbff4735027cf'
SOURCE_CELLS = '196dd5b39f42708566a3cad63b2bc85d1e2fefb2cfba39a5719492f13db19681'


def eligible(cell):
    slots = h.specialized(cell)
    return len(slots) <= 8 and len(set(slots)) <= 2


def derive(original):
    h.check(len(original) == len({c['id'] for c in original}) ==
            len({h.signature(c['scenario']) for c in original}) == 241 and
            len({h.composition(c) for c in original}) == 9 and sum(map(eligible, original)) == 128,
            'Complete 241-recipe / nine-composition / 128-eligible original family required')
    for cell in original:
        s = cell['scenario']
        h.check(s['floorNumber'] == 12 and s['seeds'] == [] and
                [m['partySlot'] for m in s['party']] == list(range(1, 11)) and
                all(m['build']['characterLevel'] == 60 and m['build']['tier'] == 2 and
                    len(m['build']['essenceIds']) == 7 for m in s['party']),
                'Seed-free ordered floor-twelve party at level 60 / tier 2 / seven Essences required')
    donors = [c for c in original if c['gear'] == 'retained-restorer-specialization']
    h.check(len(donors) == 11, 'All eleven raw Restoration controls required')
    pairs, projected = [], []
    for full in donors:
        bases = [c for c in original if c['composition'] == full['composition'] and c['gear'] == 'retained-baseline']
        if not bases:
            h.check(full['id'].startswith('projected-reference/'), 'Missing baseline for original Restoration parent')
            projected.append(full)
            continue
        h.check(len(bases) == 1, 'Unambiguous original baseline required')
        pairs.append((bases[0], full))
    compositions = {h.composition(full) for _, full in pairs}
    h.check(len(pairs) == len(compositions) == 9 and len(projected) == 2 and
            all(h.composition(c) in compositions for c in projected),
            'Nine distinct original parents plus two retained projected controls required')
    variants = []
    for baseline, full in pairs:
        h.check(not h.specialized(baseline) and h.specialized(full) == [2]*6+[7]*6,
                'Exactly six Restoration items on each original healer required')
        normalized = copy.deepcopy(full['scenario'])
        for member, base in zip(normalized['party'], baseline['scenario']['party'], strict=True):
            items, plain = member['build']['equipment'], base['build']['equipment']
            h.check([i['slot'] for i in items] == [i['slot'] for i in plain] and
                    len({i['slot'] for i in items}) == len(items), 'Raw equipment slots/order changed')
            changes = [i for i, b in zip(items, plain, strict=True) if i != b]
            expected = h.SLOTS if member['partySlot'] in (2, 7) else set()
            h.check(len(changes) == len(expected) and {i['slot'] for i in changes} == expected and
                    all('.spec.restoration.' in i['definitionId'] for i in changes),
                    'Only the six saved Restoration equipment slots may differ')
            for item, ordinary in zip(items, plain, strict=True):
                h.check(dict(item, definitionId=ordinary['definitionId']) == ordinary,
                        'Donor equipment metadata changed')
            member['build']['equipment'] = copy.deepcopy(plain)
        h.check(normalized == baseline['scenario'], 'Parent differs beyond the declared equipment')
        for healer_slots, item_slots in [([2], None), ([7], None), ([2, 7], FOUR_SLOTS)]:
            cell = copy.deepcopy(baseline)
            for member, donor in zip(cell['scenario']['party'], full['scenario']['party'], strict=True):
                if member['partySlot'] not in healer_slots:
                    continue
                for index, item in enumerate(member['build']['equipment']):
                    supplied = donor['build']['equipment'][index]
                    if '.spec.' in supplied['definitionId'] and (item_slots is None or item['slot'] in item_slots):
                        member['build']['equipment'][index] = copy.deepcopy(supplied)
            digest = h.signature(cell['scenario'])
            cell.update(id='floor12-limited-restoration/'+digest,
                        gear='limited-restoration-slots-'+'-'.join(map(str, healer_slots))+
                             ('-four-each' if item_slots else '-six'), origin='proposed-floor12-limited-restoration')
            expected = [2]*4+[7]*4 if item_slots else healer_slots*6
            h.check(h.specialized(cell) == expected and eligible(cell), 'Exact six/eight-item subset required')
            variants.append(dict(sourceId=full['id'], baselineId=baseline['id'], healerPartySlots=healer_slots,
                                 equipmentSlots=sorted(item_slots) if item_slots else None,
                                 specializedItems=len(expected), scenarioSha256=digest, cell=cell))
    return variants


def propose(original):
    variants = derive(original)
    cells = copy.deepcopy(original)+[v['cell'] for v in variants]
    h.check(len(variants) == 27 and len(cells) == len({c['id'] for c in cells}) ==
            len({h.signature(c['scenario']) for c in cells}) == 268 and sum(map(eligible, cells)) == 155,
            'Complete 268-recipe / 155-eligible expanded family required')
    return dict(version=VERSION, status='ProposedNotPrepared', floor=12, retainedCells=241, newVariants=27,
                totalCells=268, actualCompositions=9, eligibleRecipes=155,
                equipmentEligibility=copy.deepcopy(h.EQUIPMENT), variants=variants, cells=cells,
                currentRuntimeQualified=False, studyAdmitted=False, usedForAcceptance=False,
                newFights=0, newSeeds=0, nativePreparations=0)


def validate(proposal, original):
    h.check(proposal == propose(original), 'Complete proposal, raw recipes, controls or unallocated status changed')
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
    h.check(h.sha(SOURCE/'files.json') == SOURCE_MANIFEST and h.sha(SOURCE/'cells.json') == SOURCE_CELLS,
            'Exact original qualified family required')
    done = h.read(CONTROL/'completion.json')
    h.check(done['status'] == 'CompletedDiagnosticOnly' and not done['usedForAcceptance'] and
            (done['freshStudyFights'], done['newReservations'], done['finalExclusions']) == (7712, 32, 928892) and
            h.sha(CONTROL/'evidence.json') == done['evidenceSha256'], 'Closed complete midpoint required')
    evidence = h.read(CONTROL/'evidence.json')
    h.check(evidence['maximumWins'] == 16 and evidence['maximumLimitedWins'] == 10 and
            not evidence['diagnosticAssessment']['eligibleForDiagnosticNomination'], 'Closed midpoint decision changed')
    for group in ('sourcePins', 'liveCatalogPins', 'ledgerPins'):
        for path, pin in done[group].items(): h.check(h.sha(path) == pin, 'Diagnostic input changed: '+path)
    original = h.read(SOURCE/'cells.json')
    proposal = propose(original)
    validate(proposal, original)
    paths = [SOURCE/'files.json', SOURCE/'cells.json', CONTROL/'completion.json', CONTROL/'evidence.json',
             Path(__file__), Path(h.__file__)]
    pins = {str(p): h.sha(p) for p in paths}
    output.mkdir()
    write(output/'proposal.json', proposal)
    for path, pin in pins.items(): h.check(h.sha(path) == pin, 'Proposal input changed during derivation')
    write(output/'completion.json', dict(status='ProposedNotPrepared', source=str(SOURCE), sourcePins=pins,
          proposalSha256=h.sha(output/'proposal.json'), retainedCells=241, newVariants=27, totalCells=268,
          actualCompositions=9, eligibleRecipes=155, newFights=0, newSeeds=0, historicalReplays=0,
          nativePreparations=0, currentRuntimeQualified=False, studyAdmitted=False, gameContentChanged=False,
          next='Bind this expanded family to the current qualified unchanged source and prepare all 268 recipes '
               'natively before declaring a new finite diagnostic. Retain all prior controls; no setting is selected here.'))
    print('Proposed 268 floor-12 recipes: all 241 controls + 27 exact six/eight-item Restoration subsets; no fights or seeds.')


if __name__ == '__main__':
    main()
