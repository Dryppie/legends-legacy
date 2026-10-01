"""Exact seed-free extension of the retained floor-eight family with six gear variants."""
import copy
import hashlib
import json
from pathlib import Path

VERSION = 'floor8-one-healer-family-proposal-v1'
EQUIPMENT = dict(version='tower-limited-equipment-v1', maximumSpecializedItems=8, maximumSpecializedCharacters=2)
SLOTS = {'MainHand', 'Chest', 'Head', 'Ring', 'Necklace', 'Relic'}


def check(ok, message):
    if not ok: raise ValueError(message)


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def signature(value): return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
def composition(cell): return tuple(sorted((m['partySlot'], tuple(sorted(m['build']['essenceIds']))) for m in cell['scenario']['party']))
def specialized(cell): return [m['partySlot'] for m in cell['scenario']['party'] for item in m['build']['equipment'] if '.spec.' in item['definitionId']]


def derive(original, parents):
    """Preserve every raw field and substitute exactly one saved healer's equipment."""
    lookup = {c['id']: c for c in original}
    check(len(lookup) == len(original) and len(parents) == len(set(parents)) == 3, 'Three distinct original parents required')
    check(all(p in lookup for p in parents), 'Parent absent from original family')
    check(len({composition(lookup[p]) for p in parents}) == 3, 'Three actual compositions required')
    variants = []
    for parent in parents:
        full = lookup[parent]
        matches = [c for c in original if c['composition'] == full['composition'] and c['gear'] == 'baseline']
        check(len(matches) == 1 and full['gear'] == 'restorer-specialization', 'Unambiguous baseline/Restoration pair required')
        baseline = matches[0]
        check(not specialized(baseline) and specialized(full) == [2]*6+[7]*6, 'Only six specialized items on each healer are permitted')
        normalized = copy.deepcopy(full['scenario'])
        check([m['partySlot'] for m in normalized['party']] == list(range(1, 11)), 'Ordered ten-character party required')
        for member, base in zip(normalized['party'], baseline['scenario']['party'], strict=True):
            changed = [i['slot'] for i,b in zip(member['build']['equipment'],base['build']['equipment'],strict=True) if i != b]
            check(set(changed) == (SLOTS if member['partySlot'] in (2,7) else set()) and len(changed) in (0,6), 'Undeclared parent equipment difference')
            member['build']['equipment'] = copy.deepcopy(base['build']['equipment'])
        check(normalized == baseline['scenario'], 'Parent identity, Essence order, party position or budget changed')
        for slot in (2,7):
            cell = copy.deepcopy(baseline)
            cell.update(id='one-healer-restoration/'+signature(dict(parent=parent,slot=slot)),
                        gear='one-healer-restoration-slot-'+str(slot), origin='proposed-one-healer-restoration')
            member = next(m for m in cell['scenario']['party'] if m['partySlot'] == slot)
            member['build']['equipment'] = copy.deepcopy(next(m for m in full['scenario']['party'] if m['partySlot'] == slot)['build']['equipment'])
            check(specialized(cell) == [slot]*6 and not cell['scenario']['seeds'], 'Exact seed-free six-item variant required')
            variants.append(dict(sourceId=parent,baselineId=baseline['id'],restorationPartySlot=slot,specializedItems=6,cell=cell))
    return variants


def validate(proposal, original, parents):
    check(proposal['version'] == VERSION and proposal['status'] == 'ProposedNotPrepared' and proposal['floor'] == 8 and
          proposal['newFights'] == proposal['newSeeds'] == proposal['nativePreparations'] == 0, 'Unallocated floor-eight proposal required')
    check((proposal['retainedCells'],proposal['newVariants'],proposal['totalCells'],proposal['actualCompositions'],proposal['eligibleRecipes']) == (177,6,183,9,125)
          and proposal['equipmentEligibility'] == EQUIPMENT, 'Family or equipment limits changed')
    check(len(original) == 177 and len({composition(c) for c in original}) == 9 and
          all(c['scenario']['floorNumber'] == 8 and not c['scenario']['seeds'] for c in original), 'Complete original floor-eight family required')
    variants = derive(original, parents)
    check(proposal['variants'] == variants and proposal['cells'] == original+[v['cell'] for v in variants], 'Proposed recipes changed or original controls lost')
    cells = proposal['cells']
    check(len({c['id'] for c in cells}) == 183 and len({signature(c['scenario']) for c in cells}) == 183, 'Duplicate recipe or raw identity')
    check(sum(len(specialized(c)) <= 8 and len(set(specialized(c))) <= 2 for c in cells) == 125, 'Actual equipment eligibility differs')
    return cells


def admit(path, source):
    proposal = read(path)
    check(Path(proposal['source']).resolve() == source.resolve() and sha(source/'files.json') == proposal['sourceManifestSha256'] and
          sha(source/'cells.json') == proposal['sourceCellsSha256'], 'Proposal source changed')
    check(sha(proposal['precedingEvidence']) == proposal['precedingEvidenceSha256'] and sha(proposal['review']) == proposal['reviewSha256'], 'Supporting evidence changed')
    evidence, review = read(proposal['precedingEvidence']), read(proposal['review'])
    check(evidence['status'] == 'Verified' and evidence['trialStatus'] == 'SharedPenetrationScreenNotAccepted' and
          review['status'] == 'VerifiedSavedReportReview' and review['evidenceSha256'] == proposal['precedingEvidenceSha256'] and
          review['newFights'] == review['newSeeds'] == 0 and review['usedForAcceptance'] is False, 'Closed descriptive evidence required')
    parents = [c['fullId'] for c in review['comparisons']]
    check([c['fullWins'] for c in review['comparisons']] == [48,33,27], 'Frozen Restoration parent selection changed')
    return validate(proposal, read(source/'cells.json'), parents)


def validate_mode(mode, floor, source, modifications):
    check(mode == 'prepare' and floor == 8 and source is not None and not any(modifications),
          'One-healer family requires seed-free original-catalog preparation without other modifications')
