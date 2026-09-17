"""Describe the six sealed allocation-comparison trajectories. No combat, RNG or native preparation."""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / 'TestResults/allocation-comparison-20260917'
RUN = ROOT / 'TestResults/balance/tower-allocation-comparison-20260917'
CONFIRMED = '399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b'
CHALLENGER = '7a249fb59ac0b4c46e185637f8e534e36f4853c9bf6f9de7bb7c1de6f3148515'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def canonical(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':')).encode()).hexdigest()


def edits(parent, child):
    """Composition changes on the same character, independent of display/ability order."""
    return [dict(slot=int(slot), removed=sorted(set(parent[slot])-set(child[slot])),
                 added=sorted(set(child[slot])-set(parent[slot])))
            for slot in sorted(parent, key=int) if set(parent[slot]) != set(child[slot])]


def distance(parent, child):
    return sum(len(change['removed']) for change in edits(parent, child))


def legal(builds, families, slots, width):
    return (set(builds) == {str(i) for i in range(1, slots+1)}
            and all(len(values) == width and values == sorted(values)
                    and all(e in families for e in values)
                    and len({families[e].casefold() for e in values}) == width for values in builds.values()))


def neighborhood_sizes(builds, families):
    # Count replacements without creating full parties or selecting any new candidate.
    result = {}
    for slot, values in builds.items():
        counts = {}
        for removed in values:
            retained_families = {families[e].casefold() for e in values if e != removed}
            counts[removed] = sum(e != removed and families[e].casefold() not in retained_families for e in families)
        result[slot] = dict(total=sum(counts.values()), byRemovedEssence=counts)
    return result


def wins(row):
    return sum(sum(cell['clears']) for cell in row['cells'])


def samples(row):
    return sum(len(cell['clears']) for cell in row['cells'])


def rank(rows):
    return sorted(rows, key=lambda r: (-r['fitness']['worstContextWinRate'], r['fitness']['guardianHealth'],
                                      -r['fitness']['survival'], r['fitness']['victoryDuration'], r['id']))


def selection_rank(rows, shortlist):
    order = {pid: index for index, pid in enumerate(shortlist)}
    return sorted(rows, key=lambda r: (-wins(r), r['fitness']['guardianHealth'] if wins(r) == 0 else 0,
                                      order[r['id']], r['id']))


def score(row):
    return dict(wins=wins(row), samples=samples(row), guardianHealth=row['fitness']['guardianHealth'])


def analyze_study(index, verified_read):
    prefix = f'study-{index}/'
    d = verified_read(prefix+'definition.json')
    discovery = verified_read(prefix+'discovery.json')
    selection = verified_read(prefix+'selection-results.json')
    finalists = verified_read(prefix+'finalists.json')
    report = verified_read(prefix+'study.json')
    require(discovery['status'] == report['status'] == 'Complete', 'Incomplete study')
    require(report['discovery'] == discovery and report['selection'] == selection, 'Study/phase mismatch')
    require(d['ownedCopies'] is None and d['requiredPartySize'] == 10 and d['budget']['essenceSlots'] == 5,
            'Analysis requires the captured unrestricted ten-by-five scope')
    families = {e['id']: e['family'] for e in d['allowedEssences']}
    confirmed = next(s['party']['builds'] for s in d['starts'] if s['party']['id'] == CONFIRMED)
    starts = {s['party']['id'] for s in d['starts']}
    arm = discovery['arms'][0]
    proposals = arm['proposals']
    first_rows = {r['id']: r for r in arm['evaluations']}
    accepted = {}; accepted_by_proposal = {}; previous_rows = []
    counts = defaultdict(Counter); children = Counter(); population_uses = Counter()
    rows = []; candidate_rows = {}; rounds = arm.get('evaluationRounds', [])
    round_for_candidate = {pid: r for r in rounds for pid in r['newCandidates']}
    observations = defaultdict(list)
    stage_summaries = []
    for ordinal, p in enumerate(proposals):
        provenance = p['provenance']; op = provenance['operator']; party = p['party']; result = p['result']
        require(int(provenance['id'].rsplit('-', 1)[1]) == ordinal, 'Proposal ordinal mismatch')
        counts[op]['attempts'] += 1; counts[op][result] += 1
        parent_parties = []
        if op != 'supplied':
            for parent in provenance['parentIds']:
                require(parent in accepted_by_proposal, 'Parent was not an earlier accepted proposal')
                parent_parties.append(accepted_by_proposal[parent]['party']); children[parent_parties[-1]['id']] += 1
        for pid in p['supplied']['populationIds']:
            require(pid in accepted or op in ('supplied', 'fresh-legal'), 'Unknown eligible parent')
            if parent_parties: population_uses[pid] += 1
        if not rounds and parent_parties:
            expected_population = [r['id'] for r in rank(previous_rows)[:4]]
            require(p['supplied']['populationIds'] == expected_population, 'Baseline parent population differs')
            require(all(parent['id'] in set(expected_population) | starts for parent in parent_parties), 'Ineligible baseline parent')
        if rounds and parent_parties:
            round_index = len(accepted)//8
            expected_population = rounds[round_index]['parentsBefore']
            require(p['supplied']['populationIds'] == expected_population, 'Racing parent population differs')
            require(all(parent['id'] in set(expected_population) | starts for parent in parent_parties), 'Ineligible racing parent')
        if party is not None:
            require(party['id'] == canonical(party['builds']), 'Wrong canonical recipe ID')
            is_legal = legal(party['builds'], families, 10, 5)
            require(is_legal == (result != 'duplicate-family'), 'Unexpected recorded legality')
            if result == 'duplicate': require(party['id'] in accepted, 'Duplicate was not previously evaluated')
            actual_slots = [e['slot'] for e in edits(parent_parties[0]['builds'], party['builds'])] if parent_parties else list(range(1, 11))
            require(actual_slots == p['supplied']['changedSlots'], 'Recorded changed slots differ')
        record = dict(proposal=ordinal, proposalId=provenance['id'], operator=op, result=result,
                      partyId=party['id'] if party else None, parentIds=[x['id'] for x in parent_parties],
                      distanceToConfirmed=distance(confirmed, party['builds']) if party else None,
                      changedFromPrimaryParent=edits(parent_parties[0]['builds'], party['builds']) if party and parent_parties else None,
                      changedFromConfirmed=edits(confirmed, party['builds']) if party else None)
        rows.append(record)
        if result != 'evaluated': continue
        require(party is not None and party['id'] not in accepted and is_legal, 'Repeated or illegal evaluated recipe')
        accepted[party['id']] = p; accepted_by_proposal[provenance['id']] = p
        candidate_rows[party['id']] = record
        record['evaluationOrdinal'] = len(accepted)  # Human-facing one-based index.
        row = first_rows[party['id']]; record['initialScreen'] = score(row)
        previous_rows.append(row)
        if not rounds:
            record['topFourImmediatelyAfterEvaluation'] = party['id'] in {r['id'] for r in rank(previous_rows)[:4]}
        else:
            record['introducedRound'] = round_for_candidate[party['id']]['index'] + 1
    require(set(accepted) == set(first_rows), 'Proposal/evaluation membership mismatch')
    require(len(accepted) == d['generation']['candidatesPerArm'], 'Incomplete candidate allowance')

    def observe(stage, panel_rows):
        ordered = rank(panel_rows)
        for position, row in enumerate(ordered, 1):
            require(row['id'] in accepted, 'Measurement has no accepted candidate')
            require(len(row['cells']) == 1 and row['fitness']['worstContextWinRate'] == wins(row)/samples(row), 'Wrong measurement rate')
            observations[row['id']].append(dict(stage=stage, rank=position, **score(row)))
        stage_summaries.append(dict(stage=stage, candidates=len(panel_rows), fights=sum(samples(r) for r in panel_rows),
                                    freshFights=sum(samples(r) for r in panel_rows if accepted[r['id']]['provenance']['operator'] == 'fresh-legal'),
                                    freshWins=sum(wins(r) for r in panel_rows if accepted[r['id']]['provenance']['operator'] == 'fresh-legal')))
    if rounds:
        previous_parents = None
        for r in rounds:
            require(len(r['newCandidates']) == 8 and len(r['screenSeeds']) == 8 and len(r['promotionSeeds']) == 16, 'Wrong round size')
            if previous_parents is not None: require(r['parentsBefore'] == previous_parents, 'Changed inter-round parent set')
            expected_screen = set(r['parentsBefore']) | starts | set(r['newCandidates'])
            require(set(x['id'] for x in r['screening']) == expected_screen, 'Wrong screening membership')
            require(all(samples(x) == 8 for x in r['screening']) and all(samples(x) == 16 for x in r['promotion']), 'Wrong panel sample size')
            promoted = [x['id'] for x in rank(r['screening'])[:4]]
            # Membership, plus the exact recorded promotion measurement order.
            require(set(r['promotedIds']) == set(promoted) | starts, 'Wrong promotion membership')
            require([x['id'] for x in r['promotion']] == r['promotedIds'], 'Wrong promotion order')
            require([x['id'] for x in rank(r['promotion'])[:4]] == r['parentsAfter'], 'Wrong retained parents')
            previous_parents = r['parentsAfter']
            for pid in r['newCandidates']:
                row = next(x for x in r['screening'] if x['id'] == pid)
                require(row == first_rows[pid], 'First screen differs from retained evaluation')
            observe(f"round-{r['index']+1}-screen", r['screening'])
            observe(f"round-{r['index']+1}-promotion", r['promotion'])
        nomination_rows = rank(rounds[-1]['promotion'])
    else:
        observe('discovery', arm['evaluations']); nomination_rows = rank(arm['evaluations'])
    nominees = starts | {r['id'] for r in [x for x in nomination_rows if x['id'] not in starts][:2]}
    expected_shortlist = [r['id'] for r in nomination_rows if r['id'] in nominees]
    shortlist = [p['id'] for p in discovery['discoveryShortlist']]
    require(expected_shortlist == shortlist, 'Nomination ranking differs')
    require(set(shortlist) == {r['id'] for r in selection}, 'Missing selection measurement')
    selected = selection_rank(selection, shortlist)[0]['id']
    require(len(finalists) == 1 and finalists[0]['party']['id'] == selected, 'Wrong selected output')
    primary = next(m for m in report['confirmation']['members'] if m['primary'])
    require(primary['generatedIds'] == [selected], 'Confirmation selected a different output')
    selection_by_id = {r['id']: r for r in selection}
    for pid, record in candidate_rows.items():
        record['observations'] = observations[pid]; record['actualParentUses'] = children[pid]
        record['eligiblePopulationAppearances'] = population_uses[pid]
        record['nominated'] = pid in shortlist; record['selected'] = pid == selected
        record['selection'] = score(selection_by_id[pid]) if pid in selection_by_id else None
        if not rounds: record['exit'] = 'selected' if pid == selected else 'selection-not-selected' if pid in shortlist else 'discovery-not-nominated'
        else:
            record['exit'] = ('selected' if pid == selected else 'selection-not-selected' if pid in shortlist
                              else 'last-promotion-not-nominated' if pid in rounds[-1]['promotedIds']
                              else 'last-screen-not-promoted' if pid in {r['id'] for r in rounds[-1]['screening']}
                              else 'earlier-round-not-retained')
    measured = list(candidate_rows.values())
    one = [r for r in measured if r['distanceToConfirmed'] == 1]
    mutation_rows = [r for r in measured if r['parentIds']]
    changed_slots = Counter(change['slot'] for r in mutation_rows for change in r['changedFromPrimaryParent'])
    nearby_slots = Counter(r['changedFromConfirmed'][0]['slot'] for r in one)
    fresh = [r for r in measured if r['operator'] == 'fresh-legal']
    direct_confirmed = [r for r in mutation_rows if r['parentIds'][0] == CONFIRMED]
    fresh_ids = {r['partyId'] for r in fresh}
    fresh_children = [r for r in measured if set(r['parentIds']) & fresh_ids]
    # These are descriptive counts of adaptive observations, not pooled strength estimates.
    for op, counter in counts.items():
        op_rows = [r for r in measured if r['operator'] == op]
        counter['nominees'] = sum(r['nominated'] for r in op_rows)
        counter['selected'] = sum(r['selected'] for r in op_rows)
        counter['initialScreenZeroWinCandidates'] = sum(r['initialScreen']['wins'] == 0 for r in op_rows)
    cost = report['accounting']['completed']
    require(sum(stage['fights'] for stage in stage_summaries) == cost['discovery'], 'Discovery charge mismatch')
    require(sum(samples(r) for r in selection) == cost['selection'], 'Selection charge mismatch')
    summary = dict(study=index, restart=index//2+1, method='racing' if rounds else 'baseline',
                   proposalAttempts=len(proposals), evaluated=len(accepted), legalUniqueRecipes=len(accepted),
                   proposalResults=dict(Counter(p['result'] for p in proposals)), operators={k: dict(v) for k, v in sorted(counts.items())},
                   discoveryFights=cost['discovery'], selectionFights=cost['selection'],
                   oneEditConfirmed=len(one), oneEditBySlot=dict(sorted(nearby_slots.items())),
                   mutationChangesBySlot=dict(sorted(changed_slots.items())),
                   mutatedDistinctCharacters=len(changed_slots), directConfirmedParentCandidates=len(direct_confirmed),
                   distanceHistogram=dict(sorted(Counter(r['distanceToConfirmed'] for r in measured).items())),
                   freshCandidates=len(fresh), freshInitialWins=sum(r['initialScreen']['wins'] for r in fresh),
                   freshDiscoveryFights=sum(s['freshFights'] for s in stage_summaries),
                   freshAllDiscoveryWins=sum(s['freshWins'] for s in stage_summaries),
                   evaluatedChildrenWithFreshParent=len(fresh_children),
                   freshParentChildrenInitialWins=sum(r['initialScreen']['wins'] for r in fresh_children),
                   freshDistanceMin=min(r['distanceToConfirmed'] for r in fresh), freshDistanceMax=max(r['distanceToConfirmed'] for r in fresh),
                   stages=stage_summaries, exits=dict(Counter(r['exit'] for r in measured)),
                   nominees=[dict(partyId=pid, operator=candidate_rows[pid]['operator'], distanceToConfirmed=candidate_rows[pid]['distanceToConfirmed'],
                                  discovery=score(next(r for r in nomination_rows if r['id'] == pid)), selection=score(selection_by_id[pid]), selected=pid == selected)
                             for pid in shortlist], selected=selected,
                   neighborhoodBySlot=neighborhood_sizes(confirmed, families))
    return dict(summary=summary, proposals=rows), accepted


def run(output):
    started = time.monotonic()
    closeout = read(PACKAGE/'closeout.json')
    require(sha(PACKAGE/'files.json') == closeout['manifestSha256'], 'Changed package inventory')
    require(sha(PACKAGE/'native-files.json') == closeout['nativeInventorySha256'], 'Changed native inventory')
    native_pins = read(PACKAGE/'native-files.json'); consumed = {}
    def verified_read(name):
        path = RUN/name
        digest = sha(path); require(native_pins.get(name) == digest, 'Changed sealed input: '+name)
        consumed[str(path)] = digest
        return read(path)
    original = verified_read('result.json')
    studies, accepted = [], []
    for index in range(6):
        study, parties = analyze_study(index, verified_read)
        studies.append(study); accepted.append(parties)
    pairs = []
    for i in range(0, 6, 2):
        left, right = studies[i]['proposals'], studies[i+1]['proposals']
        signatures = lambda rows: [(r['operator'], r['result'], r['partyId'], r['parentIds']) for r in rows]
        a, b = signatures(left), signatures(right); first_difference = next((j for j in range(min(len(a), len(b))) if a[j] != b[j]), None)
        intersection = set(accepted[i]) & set(accepted[i+1])
        # Only the first panel is shared; later first-screens have different trial sets.
        for x, y in zip(left[:8], right[:8]):
            require(x['partyId'] == y['partyId'] and x['initialScreen'] == y['initialScreen'], 'Initial common batch differs')
        pairs.append(dict(restart=i//2+1, sharedEvaluatedRecipes=len(intersection),
                          sharedNonSuppliedRecipes=len(intersection)-2,
                          baselineOnly=len(accepted[i])-len(intersection), racingOnly=len(accepted[i+1])-len(intersection),
                          firstDifferentProposal=first_difference,
                          firstDifferenceBaseline=left[first_difference] if first_difference is not None else None,
                          firstDifferenceRacing=right[first_difference] if first_difference is not None else None))
    distinct_fresh = {r['partyId'] for s in studies for r in s['proposals'] if r['result'] == 'evaluated' and r['operator'] == 'fresh-legal'}
    distinct_one = {r['partyId'] for s in studies for r in s['proposals'] if r['result'] == 'evaluated' and r['distanceToConfirmed'] == 1}
    challenger = [dict(study=s['summary']['study'], rows=[r for r in s['proposals'] if r['partyId'] == CHALLENGER]) for s in studies]
    summary = dict(status='VerifiedSavedCoverage', studies=[s['summary'] for s in studies], pairs=pairs,
                   uniqueFreshRecipesAcrossBothMethods=len(distinct_fresh), uniqueOneEditConfirmedAcrossBothMethods=len(distinct_one),
                   oneEditNeighborhoodSize=sum(v['total'] for v in studies[0]['summary']['neighborhoodBySlot'].values()),
                   proposalAttempts=sum(s['summary']['proposalAttempts'] for s in studies),
                   evaluatedOccurrences=sum(s['summary']['evaluated'] for s in studies),
                   uniqueEvaluatedRecipes=len(set().union(*[set(a) for a in accepted])),
                   originalDecision=original['decision'], originalMeanDifference=original['meanDifference'],
                   newFights=0, newSeeds=0, newGeneratedParties=0, nativePreparations=0)
    summary['byMethod'] = {}
    for method in ('baseline', 'racing'):
        group = [s['summary'] for s in studies if s['summary']['method'] == method]
        totals = {key: sum(s[key] for s in group) for key in ('proposalAttempts', 'evaluated', 'discoveryFights',
                  'freshDiscoveryFights', 'freshAllDiscoveryWins', 'oneEditConfirmed', 'evaluatedChildrenWithFreshParent')}
        totals['operators'] = {}
        for s in group:
            for operator, counts in s['operators'].items():
                merged = Counter(totals['operators'].get(operator, {})); merged.update(counts)
                totals['operators'][operator] = dict(merged)
        summary['byMethod'][method] = totals
    require(not output.exists(), 'Use a new analysis output; sealed inputs are read-only')
    output.mkdir(parents=True)
    def save(name, value):
        with (output/name).open('x', encoding='utf-8') as stream: json.dump(value, stream, indent=2)
    save('summary.json', summary); save('trajectories.json', studies); save('challenger.json', challenger)
    (output/'analyzer.py').write_bytes(Path(__file__).read_bytes())
    for path, digest in consumed.items(): require(sha(Path(path)) == digest, 'Input changed during analysis')
    sources = [Path(__file__), ROOT/'LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs',
               ROOT/'LL/tools/BalanceHarness/TowerEvaluationAllocationSearch.cs', ROOT/'LL/tools/BalanceHarness/TowerBossGeneration.cs',
               ROOT/'LL/tools/BalanceHarness/TowerBossStudyPolicy.cs', ROOT/'LL/tools/BalanceHarness/TowerZeroWinSelection.cs',
               ROOT/'LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs']
    save('inputs.json', dict(archiveInputs=consumed, sourceFiles={str(p): sha(p) for p in sources},
                            nativeInventorySha256=sha(PACKAGE/'native-files.json'), packageCloseoutSha256=sha(PACKAGE/'closeout.json')))
    save('files.json', {p.name: sha(p) for p in sorted(output.iterdir()) if p.is_file()})
    save('completion.json', dict(status=summary['status'], elapsedSeconds=time.monotonic()-started,
                                checkedArchiveInputs=len(consumed), filesSha256=sha(output/'files.json'),
                                newFights=0, newSeeds=0, newGeneratedParties=0, nativePreparations=0))
    print(json.dumps({k: v for k, v in summary.items() if k not in ('studies', 'pairs', 'byMethod')}, indent=2))
    for s in summary['studies']:
        print(s['study'], s['proposalResults'], 'nearby', s['oneEditConfirmed'], 'fresh fights', s['freshDiscoveryFights'],
              'fresh all wins', s['freshAllDiscoveryWins'], 'exits', s['exits'])


class ArithmeticTests(unittest.TestCase):
    def test_distance_ignores_order_but_preserves_character_placement(self):
        self.assertEqual(distance({'1': ['a','b'], '2': ['c','d']}, {'1': ['b','a'], '2': ['c','d']}), 0)
        self.assertEqual(distance({'1': ['a','b'], '2': ['c','d']}, {'1': ['c','b'], '2': ['a','d']}), 2)

    def test_family_aliases_and_replacement_of_family_occupant(self):
        f = dict(a='A', b='B', c='C', d='a')
        self.assertFalse(legal({'1': ['a','d']}, f, 1, 2))
        self.assertTrue(legal({'1': ['b','d']}, f, 1, 2))
        # ab -> bc, bd, ac; replacing b with d would duplicate family A.
        self.assertEqual(neighborhood_sizes({'1': ['a','b']}, f)['1']['total'], 3)

    def test_positive_selection_ties_use_discovery_order(self):
        def row(pid, win, health):
            return dict(id=pid, cells=[dict(clears=[win])], fitness=dict(guardianHealth=health))
        a, b = row('a', True, 90), row('b', True, 1)
        self.assertEqual(selection_rank([a,b], ['a','b'])[0]['id'], 'a')
        a, b = row('a', False, 90), row('b', False, 1)
        self.assertEqual(selection_rank([a,b], ['a','b'])[0]['id'], 'b')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--self-test', action='store_true')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    if args.self_test:
        suite = unittest.defaultTestLoader.loadTestsFromTestCase(ArithmeticTests)
        if not unittest.TextTestRunner(verbosity=2).run(suite).wasSuccessful(): raise SystemExit(1)
    elif args.output:
        run(args.output.resolve())
    else:
        parser.error('Choose --self-test or --output')
