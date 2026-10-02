"""Materialize exact limited-equipment proposals for floors 13–15, without combat.

Historical parents supply equipment only. Every original control and raw field
is retained. These proposals cannot qualify a runtime, admit a study or tune it.
"""
import argparse
import copy
import importlib.util
import itertools
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
spec = importlib.util.spec_from_file_location('late_floor_equipment_helpers', HERE/'tower-one-healer-family.py')
h = importlib.util.module_from_spec(spec)
spec.loader.exec_module(h)
VERSION = 'late-floor-limited-defense-proposal-v1'
SLOTS = {'Chest', 'Head', 'Legs', 'Necklace'}
FLOORS = {
    13: dict(controls=130, compositions=9, party=10, level=60, essences=7,
             defense='resistance', originalEligible=18, historicalWins=[42, 38]),
    14: dict(controls=73, compositions=5, party=10, level=60, essences=7,
             defense='armor', originalEligible=10, historicalWins=[91, 64]),
    15: dict(controls=73, compositions=5, party=15, level=70, essences=8,
             defense='armor', originalEligible=10, historicalWins=[75, 73]),
}
REVIEW = ROOT/'TestResults/tower-late-floors-historical-review-20261002'
REVIEW_SHA = '4be5f4e4c7fbef540c1b575041a76cf4f7c986e2fa8d1b4ece2404ad52584805'


def propose(floor, original, parents):
    h.check(type(floor) is int and floor in FLOORS, 'Only floors 13–15 are supported')
    budget = FLOORS[floor]
    lookup = {c['id']: c for c in original}
    h.check(len(lookup) == len(original) == budget['controls'] and
            len({h.composition(c) for c in original}) == budget['compositions'],
            'Complete original family required')
    h.check(len(parents) == len(set(parents)) == 2 and all(p in lookup for p in parents),
            'Two distinct original parents required')
    h.check(len({h.composition(lookup[p]) for p in parents}) == 2,
            'Two actual parent compositions required')
    ordered_slots = list(range(1, budget['party']+1))
    for cell in original:
        scenario = cell['scenario']
        h.check(scenario['floorNumber'] == floor and scenario['seeds'] == [] and
                [m['partySlot'] for m in scenario['party']] == ordered_slots,
                'Seed-free original floor and raw party order required')
        h.check(all(m['build']['characterLevel'] == budget['level'] and m['build']['tier'] == 2 and
                    len(m['build']['essenceIds']) == budget['essences'] for m in scenario['party']),
                'Approved minimum level, tier and Essence budget required')
    variants = []
    for parent in parents:
        full = lookup[parent]
        bases = [c for c in original if c['composition'] == full['composition'] and c['gear'] == 'retained-baseline']
        h.check(full['gear'] == 'retained-'+budget['defense']+'-and-health' and len(bases) == 1,
                'Unambiguous saved baseline/defense-and-health pair required')
        baseline = bases[0]
        h.check(not h.specialized(baseline) and
                h.specialized(full) == [slot for slot in ordered_slots for _ in range(4)],
                'Exactly four specialized donor items per character required')
        normalized = copy.deepcopy(full['scenario'])
        for member, base in zip(normalized['party'], baseline['scenario']['party'], strict=True):
            items, base_items = member['build']['equipment'], base['build']['equipment']
            h.check([i['slot'] for i in items] == [i['slot'] for i in base_items] and
                    len({i['slot'] for i in items}) == len(items), 'Raw equipment slots/order changed')
            changed = [i['slot'] for i, b in zip(items, base_items, strict=True) if i != b]
            h.check(len(changed) == 4 and set(changed) == SLOTS, 'Only four saved equipment slots may differ')
            member['build']['equipment'] = copy.deepcopy(base_items)
        h.check(normalized == baseline['scenario'], 'Parent identity, ordering or non-equipment field changed')
        for count in (1, 2):
            for subset in itertools.combinations(ordered_slots, count):
                cell = copy.deepcopy(baseline)
                for member, donor in zip(cell['scenario']['party'], full['scenario']['party'], strict=True):
                    if member['partySlot'] in subset:
                        member['build']['equipment'] = copy.deepcopy(donor['build']['equipment'])
                digest = h.signature(cell['scenario'])
                cell.update(id=f'floor{floor}-limited-'+budget['defense']+'/'+digest,
                            gear='limited-'+budget['defense']+'-slots-'+'-'.join(map(str, subset)),
                            origin=f'proposed-floor{floor}-limited-defense')
                h.check(h.specialized(cell) == [slot for slot in subset for _ in range(4)],
                        'Actual equipment differs from declared subset')
                variants.append(dict(sourceId=parent, baselineId=baseline['id'], defensePartySlots=list(subset),
                                     specializedItems=4*count, scenarioSha256=digest, cell=cell))
    cells = copy.deepcopy(original)+[v['cell'] for v in variants]
    added = budget['party']*(budget['party']+1)
    total = budget['controls']+added
    h.check(len(variants) == added and len(cells) == len({c['id'] for c in cells}) ==
            len({h.signature(c['scenario']) for c in cells}) == total, 'Missing or duplicate raw recipe')
    eligible = sum(len(h.specialized(c)) <= 8 and len(set(h.specialized(c))) <= 2 for c in cells)
    h.check(eligible == budget['originalEligible']+added, 'Original equipment eligibility changed')
    return dict(version=VERSION, status='ProposedNotPrepared', floor=floor, defense=budget['defense'],
                retainedCells=budget['controls'], newVariants=added, totalCells=total,
                actualCompositions=budget['compositions'], eligibleRecipes=eligible,
                equipmentEligibility=copy.deepcopy(h.EQUIPMENT), parents=list(parents), variants=variants, cells=cells,
                currentRuntimeQualified=False, studyAdmitted=False, usedForAcceptance=False,
                candidateSelected=False, newFights=0, historicalReplays=0, newSeeds=0, nativePreparations=0)


def validate(proposal, floor, original, parents):
    h.check(proposal == propose(floor, original, parents),
            'Complete proposal, raw recipes, equipment limits or unallocated status changed')
    return proposal['cells']


def select_parents(rows):
    selected = []
    for row in sorted(rows, key=lambda r: (-r['wins'], r['meanGuardianHealth'], r['id'])):
        if row['actualComposition'] not in {s['actualComposition'] for s in selected}:
            selected.append(row)
        if len(selected) == 2:
            break
    h.check(len(selected) == 2, 'Two historical actual compositions required')
    return selected


def write(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    output = parser.parse_args().output.resolve()
    h.check(output.parent == ROOT/'TestResults' and not output.exists(), 'Fresh direct TestResults child required')
    h.check(h.sha(REVIEW/'completion.json') == REVIEW_SHA, 'Closed historical review changed')
    review = h.read(REVIEW/'completion.json')
    h.check(review['status'] == 'HistoricalReviewsCompleted' and review['historicalReports'] == 55576 and
            not review['currentRuntimeQualified'] and review['newSeeds'] == review['newFights'] == 0,
            'Complete historical-only review required')
    pins = {**review['sourcePins'], str(REVIEW/'completion.json'): REVIEW_SHA,
            str(Path(__file__)): h.sha(__file__), str(Path(h.__file__)): h.sha(h.__file__)}
    for path, pin in pins.items():
        h.check(h.sha(path) == pin, 'Historical input changed: '+path)
    proposals, summaries = {}, []
    for floor, budget in FLOORS.items():
        source_paths = [Path(p) for p in review['sourcePins'] if f'floor{floor}-' in p and Path(p).name == 'cells.json']
        h.check(len(source_paths) == 1, 'Unambiguous original source required')
        source = source_paths[0].parent
        rows = h.read(REVIEW/f'floor-{floor}-equipment-review.json')['rows']
        selected = select_parents(rows)
        h.check([r['wins'] for r in selected] == budget['historicalWins'], 'Historical parent selection changed')
        original = h.read(source/'cells.json')
        parents = [r['id'] for r in selected]
        proposal = propose(floor, original, parents)
        validate(proposal, floor, original, parents)
        proposals[floor] = proposal
        summaries.append(dict(floor=floor, source=str(source), selectedHistoricalParents=selected,
                              **{k: proposal[k] for k in ('retainedCells', 'newVariants', 'totalCells',
                                                         'actualCompositions', 'eligibleRecipes')}))
    output.mkdir()
    for floor, proposal in proposals.items():
        write(output/f'floor-{floor}-proposal.json', proposal)
    for path, pin in pins.items():
        h.check(h.sha(path) == pin, 'Proposal input changed during derivation: '+path)
    write(output/'completion.json', dict(status='ProposedNotPrepared', floors=summaries, sourcePins=pins,
          proposalPins={str(output/f'floor-{f}-proposal.json'): h.sha(output/f'floor-{f}-proposal.json') for f in FLOORS},
          currentRuntimeQualified=False, studyAdmitted=False, usedForAcceptance=False, gameContentChanged=False,
          candidateSelected=False, newFights=0, historicalReplays=0, newSeeds=0, nativePreparations=0,
          next='Finish floor 12 first. Each later floor still requires current-runtime qualification, native '
               'preparation and a separately bounded diagnostic before any guardian candidate or acceptance study.'))
    for row in summaries:
        print(f"Floor {row['floor']}: {row['totalCells']} proposed recipes, {row['retainedCells']} original controls, "
              f"{row['eligibleRecipes']} equipment-eligible; zero fights/preparations/seeds.")


if __name__ == '__main__':
    main()
