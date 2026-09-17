"""Retrospective selector audit of two closed comparisons and one earlier diagnostic.

Reads pinned saved evidence only. No combat, preparation, RNG, or native calls.
The hypothetical rule is diagnostic arithmetic, not a new validated selector.
"""
import argparse
import hashlib
import json
from pathlib import Path
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
CONFIRMED = '399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b'
ANCHOR = '8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50'
POLICY = 'tower-staged-zero-win-health-v1'
PACKAGE_PINS = {
    'allocation': '59a3c65442310b8ab62d95ace9e9cb930a436d138d3e726d39908b6878c4bb2e',
    'anchored': '1a2da312f1cb7af7867f0d173c9fcdf8f2213afc36873240ed2b0da505eec314',
}
DIAGNOSTIC_PIN = 'b73a6fc344214693dddfc14d74c4114ab4782d0ae80456ebd019c1103f149552'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


class Inputs:
    def __init__(self):
        self.consumed = {}

    def read(self, path, expected):
        data = path.read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        require(digest == expected, 'Changed sealed input: ' + str(path))
        self.consumed[str(path)] = digest
        return json.loads(data.decode('utf-8-sig'))


def wins(row):
    return sum(sum(c['clears']) for c in row['cells'])


def samples(row):
    return sum(len(c['clears']) for c in row['cells'])


def health(row):
    return sum(c['guardianHealth'] * len(c['clears']) for c in row['cells']) / samples(row)


def discovery_rank(rows):
    return sorted(rows, key=lambda r: (-r['fitness']['worstContextWinRate'], r['fitness']['guardianHealth'],
                                      -r['fitness']['survival'], r['fitness']['victoryDuration'], r['id']))


def select(rows, shortlist):
    ids = [r['id'] for r in rows]
    require(len(ids) == len(set(ids)) == len(shortlist) == len(set(shortlist))
            and set(ids) == set(shortlist) and ids, 'Incomplete or duplicate selection membership')
    require(len({samples(r) for r in rows}) == 1 and samples(rows[0]) > 0, 'Unequal or empty panels')
    order = {pid: i for i, pid in enumerate(shortlist)}
    return min(rows, key=lambda r: (-wins(r), health(r) if wins(r) == 0 else 0, order[r['id']], r['id']))['id']


def retain_tied_primary(rows, shortlist, supplied, primary):
    """Hypothesis only: positive maximum wins tied with a predesignated supplied team.

    Zero-win ordering remains health-first. An absent designation means legacy behavior;
    an invalid explicit designation fails instead of silently choosing a different team.
    """
    selected = select(rows, shortlist)
    if primary is None:
        return selected
    require(primary in supplied and primary in shortlist, 'Primary must be a nominated supplied team')
    top = max(wins(r) for r in rows)
    tied = [r['id'] for r in rows if wins(r) == top]
    return primary if top > 0 and len(tied) > 1 and primary in tied else selected


def panel(trials):
    result = {}
    for trial in trials:
        require(trial['seed'] not in result, 'Duplicate confirmation trial')
        require(trial['outcome'] in ('Victory', 'Defeat', 'Draw'), 'Unknown outcome')
        result[trial['seed']] = trial['outcome'] == 'Victory'
    require(result, 'Empty confirmation panel')
    return result


def contrast(evidence, old, new):
    require(old in evidence and new in evidence, 'Unconfirmed counterfactual output')
    left, right = evidence[old], evidence[new]
    require(set(left) == set(right), 'Unpaired confirmation evidence')
    gains = sum(right[s] and not left[s] for s in left)
    losses = sum(left[s] and not right[s] for s in left)
    return dict(trials=len(left), actualWins=sum(left.values()), hypotheticalWins=sum(right.values()),
                gains=gains, losses=losses, difference=(gains-losses)/len(left))


def analyze(read, diagnostic=False):
    d = read('definition.json')
    discovery = read('discovery.json')
    shortlist = read('discovery-shortlist.json')
    selection = read('selection-results.json')
    report = read('study.json')
    require(d['stages']['selectionPolicyVersion'] == POLICY, 'Unexpected selector')
    require(d['stages']['generatedFinalists'] == 1 and d['stages']['shortlist'] == 4, 'Unexpected selection scope')
    require(discovery['status'] == 'Complete' and len(discovery['arms']) == 1, 'Incomplete or multi-arm discovery')
    require(d['requiredPartySize'] == 10 and d['budget']['essenceSlots'] == 5 and d['ownedCopies'] is None,
            'Unexpected acquisition or party scope')
    require(report['discovery'] == discovery and report['selection'] == selection, 'Phase/report mismatch')
    require(shortlist == discovery['discoveryShortlist'], 'Frozen shortlist differs')
    supplied = {s['party']['id'] for s in d['starts']}
    require(len(supplied) == 2 and supplied <= {p['id'] for p in shortlist}, 'Missing supplied nominee')
    arm = discovery['arms'][0]
    rounds = arm.get('evaluationRounds', [])
    ranking_rows = discovery_rank(rounds[-1]['promotion'] if rounds else arm['evaluations'])
    nominated = supplied | {r['id'] for r in [r for r in ranking_rows if r['id'] not in supplied][:2]}
    ids = [p['id'] for p in shortlist]
    require(ids == [r['id'] for r in ranking_rows if r['id'] in nominated], 'Nomination order mismatch')
    schedule = d['stages']['schedules']
    require(len(schedule) == 1, 'Audit is restricted to one equipment context')
    context, seeds = next(iter(schedule.items()))
    require(len(seeds['selection']) == len(set(seeds['selection'])) == 32, 'Unexpected selection panel')
    for r in selection:
        require(len(r['cells']) == 1 and r['cells'][0]['context'] == context and samples(r) == 32,
                'Incomplete selection matrix')
        require(all(type(w) is bool for w in r['cells'][0]['clears']), 'Invalid saved selection wins')
        require(r['fitness']['worstContextWinRate'] == wins(r)/32
                and r['fitness']['guardianHealth'] == health(r), 'Selection fitness mismatch')
    selected = select(selection, ids)
    if diagnostic:
        freeze = read('nominees-freeze.json')
        require(report['freeze'] == freeze and freeze['primaryId'] == selected, 'Diagnostic selected output mismatch')
        require([n['partyId'] for n in freeze['nominees']] == ids, 'Diagnostic freeze membership mismatch')
        evidence = {r['partyId']: panel(r['trials']) for r in report['evidence']}
        require(set(evidence) == set(ids), 'Diagnostic needs all nominees confirmed')
        primary = None  # The later adopted team was a new challenger in this historical episode.
    else:
        require(report['status'] == 'Complete' and supplied == {CONFIRMED, ANCHOR}, 'Wrong comparison scope')
        finalists = read('finalists.json')
        freeze = read('confirmation-freeze.json')
        require(report['confirmation'] == freeze and freeze['policyVersion'] == POLICY, 'Confirmation freeze mismatch')
        require(len(finalists) == 1 and finalists[0]['primary'] and finalists[0]['party']['id'] == selected,
                'Finalist differs from selector reconstruction')
        require([m['generatedIds'] for m in freeze['members'] if m['primary']] == [[selected]], 'Primary differs')
        reference_ids = {s['referenceId']: s['party']['id'] for s in d['starts']}
        by_cell = {r['cellId']: r for r in report['evidence']}
        require(set(by_cell) == {m['cellId'] for m in freeze['members']}, 'Confirmation evidence membership mismatch')
        evidence = {}
        for member in freeze['members']:
            r = by_cell[member['cellId']]
            require(r['status'] == 'Complete', 'Incomplete confirmation evidence')
            values = panel(r['trials'])
            require(list(values) == seeds['confirmation'], 'Confirmation schedule mismatch')
            # One frozen cell can name the same recipe as both finalist and control.
            for pid in set(member['generatedIds']) | {reference_ids[x] for x in member['referenceIds']}:
                require(pid not in evidence, 'Repeated recipe evidence')
                evidence[pid] = values
        require(set(evidence) == supplied | {selected}, 'Unexpected confirmed recipe family')
        primary = CONFIRMED
    require(all(len(v) == 1000 for v in evidence.values()), 'Incomplete holdout')
    require(len({tuple(sorted(v)) for v in evidence.values()}) == 1, 'Holdouts use different panels')
    require(not (set(next(iter(evidence.values()))) & set(seeds['selection'] + seeds['discovery'])), 'Holdout reused search seeds')
    hypothetical = retain_tied_primary(selection, ids, supplied, primary)
    top = max(wins(r) for r in selection)
    top_ids = [pid for pid in ids if wins(next(r for r in selection if r['id'] == pid)) == top]
    ranked_ids = {r['id']: i for i, r in enumerate(ranking_rows, 1)}
    ranked_rows = {r['id']: r for r in ranking_rows}
    by_id = {r['id']: r for r in selection}
    result = dict(policy=d['generation']['policyVersion'], selectionPolicy=POLICY,
                  orderingSource='last-promotion-panel' if rounds else 'discovery-panel',
                  supplied=sorted(supplied), selected=selected, selectionTopWins=top, topTied=len(top_ids) > 1,
                  topIds=top_ids, selectedSupplied=selected in supplied,
                  hypotheticalPrimary=primary, hypotheticalSelected=hypothetical, changed=hypothetical != selected,
                  hypotheticalContrast=contrast(evidence, selected, hypothetical),
                  unknownNominees=[pid for pid in ids if pid not in evidence],
                  nominees=[dict(partyId=pid, supplied=pid in supplied, shortlistPosition=i,
                                 discoveryRank=ranked_ids[pid], discoveryWins=wins(ranked_rows[pid]),
                                 discoveryTrials=samples(ranked_rows[pid]), selectionWins=wins(by_id[pid]),
                                 selectionGuardianHealth=health(by_id[pid]),
                                 confirmationWins=sum(evidence[pid].values()) if pid in evidence else None)
                            for i, pid in enumerate(ids, 1)])
    return result, evidence


def run(output):
    require(not output.exists(), 'Use a new output directory; sealed evidence is read-only')
    started = time.monotonic()
    inputs = Inputs()
    studies, unique_panels, all_seeds = [], {}, set()
    originals = {}
    for cohort, package_pin in PACKAGE_PINS.items():
        package = ROOT / f'TestResults/{cohort}-comparison-20260917'
        base = ROOT / f'TestResults/balance/tower-{cohort}-comparison-20260917'
        package_files = inputs.read(package/'files.json', package_pin)
        native_files = inputs.read(package/'native-files.json', package_files['native-files.json'])
        original = inputs.read(base/'result.json', native_files['result.json'])
        originals[cohort] = dict(decision=original['decision'], meanDifference=original['meanDifference'])
        prior_evidence = None
        for index in range(6):
            prefix = f'study-{index}/'
            study_files = inputs.read(base/prefix/'files.json', native_files[prefix+'files.json'])

            def read(name):
                require(study_files[name] == native_files[prefix+name], 'Native/study inventory mismatch')
                return inputs.read(base/prefix/name, study_files[name])

            result, evidence = analyze(read)
            pair = original['pairs'][index//2]
            arm = ('racing' if cohort == 'allocation' else 'anchored') if index % 2 else 'baseline'
            require(result['selected'] == pair[arm+'Party'] and sum(evidence[result['selected']].values()) == pair[arm+'Wins'],
                    'Closed comparison disagrees with reconstructed output')
            result.update(cohort=cohort, study=index, restart=index//2+1, arm=arm)
            studies.append(result)
            seeds = set(next(iter(evidence.values())))
            if index % 2:
                require(seeds == set(next(iter(prior_evidence.values()))), 'Restart arms use different holdouts')
                for pid in evidence.keys() & prior_evidence.keys():
                    require(evidence[pid] == prior_evidence[pid], 'Shared paired output differs')
            else:
                require(not seeds & all_seeds, 'Confirmation panels overlap across restarts')
                all_seeds.update(seeds)
            prior_evidence = evidence
            for pid, values in evidence.items():
                key = (cohort, index//2+1, pid)
                if key in unique_panels:
                    require(unique_panels[key] == values, 'Repeated paired evidence differs')
                unique_panels[key] = values
    diagnostic_base = ROOT/'TestResults/balance/tower-practical-selection-diagnostic-20260917'
    diagnostic_files = inputs.read(diagnostic_base/'files.json', DIAGNOSTIC_PIN)
    study_files = inputs.read(diagnostic_base/'study/files.json', diagnostic_files['study/files.json'])

    def diagnostic_read(name):
        require(study_files[name] == diagnostic_files['study/'+name], 'Diagnostic inventory mismatch')
        return inputs.read(diagnostic_base/'study'/name, study_files[name])

    diagnostic, evidence = analyze(diagnostic_read, diagnostic=True)
    saved_result = inputs.read(diagnostic_base/'result.json', diagnostic_files['result.json'])
    require(saved_result['primaryId'] == diagnostic['selected'], 'Diagnostic primary mismatch')
    require({r['partyId']: r['wins'] for r in saved_result['rates']} == {pid: sum(v.values()) for pid, v in evidence.items()},
            'Diagnostic saved rates differ')
    require(not set(next(iter(evidence.values()))) & all_seeds, 'Earlier diagnostic panel reused')
    diagnostic['hypotheticalSuppliedDesignations'] = {
        pid: retain_tied_primary(diagnostic_read('selection-results.json'),
                                [n['partyId'] for n in diagnostic['nominees']], diagnostic['supplied'], pid)
        for pid in diagnostic['supplied']}
    summary = dict(status='VerifiedRetrospectiveSelectionAudit', comparisonStudies=len(studies), distinctComparisonPanels=6,
                   topTieStudies=sum(s['topTied'] for s in studies), changedOutputs=sum(s['changed'] for s in studies),
                   strictLeads=sum(not s['topTied'] for s in studies),
                   selectedChallengers=sum(not s['selectedSupplied'] for s in studies),
                   confirmedNomineeOccurrences=sum(4-len(s['unknownNominees']) for s in studies),
                   unknownNomineeOccurrences=sum(len(s['unknownNominees']) for s in studies),
                   distinctConfirmedRecipePanels=len(unique_panels),
                   originalDecisions=originals, newFights=0, newSeeds=0, newGeneratedParties=0, nativePreparations=0,
                   caveat='Outcome-informed hypothesis; no pooled strength estimate, replacement decision, or prospective validation.')
    for path, digest in inputs.consumed.items():
        require(sha(Path(path)) == digest, 'Input changed during analysis')
    output.mkdir(parents=True)

    def save(name, value):
        with (output/name).open('x', encoding='utf-8') as stream:
            json.dump(value, stream, indent=2)

    save('summary.json', summary)
    save('studies.json', studies)
    save('earlier-diagnostic.json', diagnostic)
    save('inputs.json', inputs.consumed)
    (output/'analyzer.py').write_bytes(Path(__file__).read_bytes())
    save('files.json', {p.name: sha(p) for p in sorted(output.iterdir()) if p.is_file()})
    save('completion.json', dict(status=summary['status'], elapsedSeconds=time.monotonic()-started,
                                checkedInputs=len(inputs.consumed), filesSha256=sha(output/'files.json')))
    print(json.dumps(summary, indent=2))


class AuditTests(unittest.TestCase):
    @staticmethod
    def row(pid, clears, hp=50):
        return dict(id=pid, cells=[dict(clears=clears, guardianHealth=hp)])

    def test_positive_tie_uses_shortlist_not_measurement_order_or_health(self):
        rows = [self.row('a', [True], 1), self.row('z', [True], 99)]
        self.assertEqual(select(rows, ['z', 'a']), 'z')
        self.assertEqual(retain_tied_primary(rows, ['z', 'a'], {'a'}, 'a'), 'a')

    def test_zero_wins_preserve_weighted_health_order(self):
        rows = [self.row('a', [False], 99), self.row('z', [False], 1)]
        self.assertEqual(retain_tied_primary(rows, ['a', 'z'], {'a'}, 'a'), 'z')

    def test_strict_challenger_lead_is_preserved(self):
        rows = [self.row('a', [True, False]), self.row('z', [True, True])]
        self.assertEqual(retain_tied_primary(rows, ['a', 'z'], {'a'}, 'a'), 'z')

    def test_tie_above_designated_primary_keeps_legacy_winner(self):
        rows = [self.row('a', [True, False]), self.row('b', [True, True]), self.row('c', [True, True])]
        self.assertEqual(retain_tied_primary(rows, ['a', 'c', 'b'], {'a'}, 'a'), 'c')

    def test_absent_or_invalid_primary(self):
        rows = [self.row('a', [True]), self.row('z', [True])]
        self.assertEqual(retain_tied_primary(rows, ['a', 'z'], {'a'}, None), 'a')
        with self.assertRaises(ValueError):
            retain_tied_primary(rows, ['a', 'z'], {'a'}, 'z')

    def test_reject_duplicate_missing_and_unequal_measurements(self):
        a = self.row('a', [True])
        for rows, ids in [([a, a], ['a', 'b']), ([a], ['a', 'b']),
                          ([a, self.row('b', [True, False])], ['a', 'b'])]:
            with self.assertRaises(ValueError):
                select(rows, ids)

    def test_paired_contrast_aligns_seeds_and_counts_draws_as_nonwins(self):
        a = panel([dict(seed=1, outcome='Victory'), dict(seed=2, outcome='Draw'), dict(seed=3, outcome='Defeat')])
        b = panel([dict(seed=3, outcome='Victory'), dict(seed=2, outcome='Victory'), dict(seed=1, outcome='Defeat')])
        self.assertEqual(contrast(dict(a=a, b=b), 'a', 'b'),
                         dict(trials=3, actualWins=1, hypotheticalWins=2, gains=2, losses=1, difference=1/3))

    def test_no_invented_or_unpaired_holdout(self):
        with self.assertRaises(ValueError):
            contrast(dict(a={1: True}), 'a', 'b')
        with self.assertRaises(ValueError):
            contrast(dict(a={1: True}, b={2: True}), 'a', 'b')
        with self.assertRaises(ValueError):
            panel([dict(seed=1, outcome='Victory'), dict(seed=1, outcome='Defeat')])

    def test_changed_sealed_input_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)/'input.json'
            path.write_text('{}', encoding='utf-8')
            digest = sha(path)
            self.assertEqual(Inputs().read(path, digest), {})
            path.write_text('[]', encoding='utf-8')
            with self.assertRaises(ValueError):
                Inputs().read(path, digest)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--self-test', action='store_true')
    group.add_argument('--output', type=Path)
    args = parser.parse_args()
    if args.self_test:
        if not unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(AuditTests)).wasSuccessful():
            raise SystemExit(1)
    else:
        run(args.output.resolve())
