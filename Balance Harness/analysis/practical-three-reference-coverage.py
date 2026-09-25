"""Describe one pinned, closed three-reference search using saved rows only.

No native command, combat, allocation, random sampling or candidate construction.
Shared coverage arithmetic is reused from the earlier saved-trajectory analyzer.
"""
import argparse
from collections import Counter
import copy
import hashlib
import importlib.util
from itertools import combinations
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
RUN = ROOT/'TestResults/balance/tower-practical-three-reference-20260922'
EXECUTION = ROOT/'TestResults/three-reference-practical-execution-20260922'
ADMISSION = ROOT/'TestResults/three-reference-native-admission-20260922'
OUTPUT = ROOT/'TestResults/three-reference-search-coverage-20260922'
RUN_PIN = '92c0245d89afbf1a8776705577fbe18dc86c4410e8c2a7ef6a76f570978fc389'
EXECUTION_PIN = 'e3481e635b790726e6f7d4c0b4af07597ab17e22b86074a3c30b1bb4aa49968d'
ADMISSION_PIN = '001eb3e1a8642f3168f15f4ebc81ec65f33235cbae8ae1e2bc5104c61646142d'
COVERAGE_PIN = 'e3da23fc41a2c0afc7720a4d9708e5f5c8fbaf49ddd90a600145f30918f0669e'
SELECTOR_PIN = '66c1716e481c36e6a2a7a8d6c6343730c8e305b980dc0e73be9e763cc459fade'
STRONG = '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c'


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load(name, path, digest):
    require(sha(path) == digest, 'Changed arithmetic helper: '+str(path))
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def ingredients():
    coverage_path = Path(__file__).with_name('practical-search-coverage.py')
    selector_path = EXECUTION/'selection-arithmetic.py'
    if not coverage_path.exists():
        coverage_path = Path(__file__).with_name('coverage-arithmetic.py')
        selector_path = Path(__file__).with_name('selection-arithmetic.py')
    return (load('saved_coverage', coverage_path, COVERAGE_PIN),
            load('saved_selector', selector_path, SELECTOR_PIN), coverage_path, selector_path)


class Inputs:
    def __init__(self):
        self.consumed = {}

    def read(self, path, digest):
        data = path.read_bytes()
        require(hashlib.sha256(data).hexdigest() == digest, 'Changed pinned input: '+str(path))
        self.consumed[str(path)] = digest
        return json.loads(data.decode('utf-8-sig'))

    def recheck(self):
        for name, digest in self.consumed.items():
            require(sha(Path(name)) == digest, 'Input changed during analysis: '+name)


def paired(left, right):
    require(len(left) == len(right) > 0 and all(type(x) is bool for x in left+right), 'Invalid paired panel')
    return dict(samples=len(left), leftWins=sum(left), rightWins=sum(right),
                bothWin=sum(a and b for a, b in zip(left, right)),
                leftOnly=sum(a and not b for a, b in zip(left, right)),
                rightOnly=sum(b and not a for a, b in zip(left, right)),
                bothLose=sum(not a and not b for a, b in zip(left, right)))


def selection_sensitivity(rows, shortlist, supplied, designated, selector):
    """Delete each paired position once with the frozen shortlist unchanged.

    Diagnostic finite perturbations, not independent experiments or probability
    estimates. Zero-win health cannot be reconstructed from aggregate cells.
    """
    require(all(len(r['cells']) == 1 and sum(r['cells'][0]['clears']) > 1 for r in rows),
            'Sensitivity needs positive counts after deletion')
    n = len(rows[0]['cells'][0]['clears'])
    require(n > 1 and all(len(r['cells'][0]['clears']) == n for r in rows), 'Unpaired sensitivity rows')
    selected = selector.retain_tied_primary(rows, shortlist, supplied, designated)
    decisions = []
    for index in range(n):
        reduced = copy.deepcopy(rows)
        for row in reduced:
            del row['cells'][0]['clears'][index]
        chosen = selector.retain_tied_primary(reduced, shortlist, supplied, designated)
        decisions.append(dict(omittedPosition=index+1, selectedPartyId=chosen))
    return dict(actualSelected=selected, retainedShortlist=True, perturbations=n,
                selectedCounts=dict(Counter(r['selectedPartyId'] for r in decisions)), decisions=decisions,
                interpretation='Descriptive sensitivity to deleting one shared selection trial; not a win probability or validated rule')


def population_runs(proposals):
    groups = []
    for ordinal, p in enumerate(proposals):
        population = p['supplied']['populationIds']
        if not population:
            continue
        if not groups or population != groups[-1]['partyIds']:
            groups.append(dict(firstProposal=ordinal, lastProposal=ordinal, mutationDecisions=0, partyIds=population))
        groups[-1]['lastProposal'] = ordinal
        groups[-1]['mutationDecisions'] += 1
    return groups


def analyze(d, study, base, coverage, selector):
    arm = study['discovery']['arms'][0]
    proposals = arm['proposals']
    accepted = {p['party']['id']:p for p in proposals if p['result'] == 'evaluated'}
    by_proposal = {p['provenance']['id']:p for p in proposals if p['result'] == 'evaluated'}
    starts = {s['party']['id']:s for s in d['starts']}
    require(len(starts) == 3 and STRONG in starts and len(accepted) == 46, 'Wrong retained scope')
    require(d['generation']['policyVersion'] == 'retained-composition-three-references-v1'
            and arm['method'] == 'retained-composition' and len(study['discovery']['arms']) == 1,
            'Unsupported generation trace')
    schedule = next(iter(d['stages']['schedules'].values()))
    require([len(schedule[k]) for k in ('discovery','selection','confirmation')] == [8,32,1000], 'Changed panels')
    evaluations = {r['id']:r for r in arm['evaluations']}
    selection = study['selection']
    shortlist = [p['id'] for p in study['discovery']['discoveryShortlist']]
    designated = next(s['party']['id'] for s in d['starts'] if s['referenceId'] == d['stages']['selectionPrimaryReferenceId'])
    selected = selector.retain_tied_primary(selection, shortlist, set(starts), designated)
    require(selected == STRONG == base['summary']['selected'], 'Changed saved selected output')
    trial_rows = {r['id']:r for r in study['_trials']}
    for stage, rows in [('discovery', arm['evaluations']), ('selection', selection)]:
        for row in rows:
            cell = row['cells'][0]
            trials = [trial_rows[tid] for tid in cell['trials']]
            require([t['seed'] for t in trials] == schedule[stage] and all(t['stage'] == stage for t in trials),
                    'Measurement rows do not share the declared panel')

    ancestry, parents, donors, operator_parents = Counter(), Counter(), Counter(), {}
    recombinations = []
    for ordinal, p in enumerate(proposals):
        op, parent_ids = p['provenance']['operator'], p['provenance']['parentIds']
        if op not in ('supplied', 'fresh-legal'):
            resolved = [by_proposal[pid]['party'] for pid in parent_ids]
            require(set(p['provenance']['referenceIds']) == set().union(
                *[set(by_proposal[pid]['provenance']['referenceIds']) for pid in parent_ids]), 'Changed ancestry propagation')
            parents[resolved[0]['id']] += 1
            operator_parents.setdefault(resolved[0]['id'], Counter())[op] += 1
            for parent in resolved[1:]:
                donors[parent['id']] += 1
            if op == 'recombine':
                different = len(coverage.edits(resolved[0]['builds'], resolved[1]['builds']))
                require(different > 0, 'Recombination parents must differ')
                recombinations.append(dict(proposal=ordinal, parents=[v['id'] for v in resolved],
                    differentCharacterLoadouts=different, possibleWholeCharacterMixtures=2**different,
                    possibleNonParentMixtures=2**different-2, parentCopyProbability=2/(2**different),
                    result=p['result'], matchesParent=p['party']['id'] in [v['id'] for v in resolved]))
        if p['result'] == 'evaluated':
            ancestry[','.join(p['provenance']['referenceIds']) or 'fresh'] += 1

    candidates = []
    for row in base['proposals']:
        if row['result'] != 'evaluated':
            continue
        pid = row['partyId']; builds = accepted[pid]['party']['builds']
        distances = {ref:coverage.distance(s['party']['builds'], builds) for ref, s in starts.items()}
        candidates.append(dict(partyId=pid, proposal=row['proposal'], operator=row['operator'],
            evaluationOrdinal=row['evaluationOrdinal'], discoveryWins=coverage.wins(evaluations[pid]),
            distancesToReferences=distances, nearestReferenceDistance=min(distances.values()),
            strongReferenceEdits=coverage.edits(starts[STRONG]['party']['builds'], builds),
            topFourImmediately=row['topFourImmediatelyAfterEvaluation'],
            populationAppearances=row['eligiblePopulationAppearances'], primaryParentUses=parents[pid], donorUses=donors[pid],
            nominated=pid in shortlist, selected=pid == selected,
            referenceAncestry=accepted[pid]['provenance']['referenceIds']))
    groups = {}
    for name, members in [('all', candidates), ('fresh', [c for c in candidates if c['operator']=='fresh-legal']),
                          ('mutated', [c for c in candidates if c['operator'] not in ('supplied','fresh-legal')])]:
        distances = [coverage.distance(accepted[a['partyId']]['party']['builds'], accepted[b['partyId']]['party']['builds'])
                     for a, b in combinations(members, 2)]
        groups[name] = dict(candidates=len(members), discoveryFights=len(members)*8,
            discoveryWins=sum(c['discoveryWins'] for c in members), zeroWinCandidates=sum(c['discoveryWins']==0 for c in members),
            nominated=sum(c['nominated'] for c in members), primaryParentUses=sum(c['primaryParentUses'] for c in members),
            nearestReferenceDistanceHistogram=dict(sorted(Counter(c['nearestReferenceDistance'] for c in members).items())),
            pairwiseDistance=dict(pairs=len(distances), minimum=min(distances), maximum=max(distances),
                                  mean=sum(distances)/len(distances)))
    nominees = []
    for pid in shortlist:
        row = next(r for r in selection if r['id'] == pid)
        recipe = next(c for c in candidates if c['partyId'] == pid)
        nominees.append(dict(recipe, selectionWins=coverage.wins(row),
                             selectionGuardianHealth=row['fitness']['guardianHealth'],
                             confirmationMeasured=pid in starts))
    panels = {r['id']:r['cells'][0]['clears'] for r in selection}
    selected_panel = panels[selected]
    pairing = {pid:paired(selected_panel, panels[pid]) for pid in shortlist if pid != selected}
    return dict(status='VerifiedSavedThreeReferenceCoverage', scope='One closed root; descriptive saved-evidence review',
        proposalAttempts=len(proposals), evaluatedCandidates=len(candidates), generatedCandidates=len(candidates)-len(starts),
        operators=base['summary']['operators'], groups=groups, referenceAncestryCounts=dict(ancestry),
        parentUseByOperator={pid:dict(counts) for pid, counts in operator_parents.items()},
        distinctPrimaryParents=len(parents), populationRuns=population_runs(proposals), recombinations=recombinations,
        candidates=candidates, nominees=nominees, selected=selected, designated=designated,
        pairedSelection=pairing, selectionSensitivity=selection_sensitivity(selection, shortlist, set(starts), designated, selector),
        nomination=dict(generatedNominated=2, generatedNotNominated=41, generatedConfirmed=0,
                        missedStrongerTeam='Not identifiable: unselected generated recipes lack confirmation'),
        newFights=0, newValues=0, newGeneratedParties=0, nativePreparations=0, changedPolicy=False)


def run(output):
    started = time.monotonic()
    require(not output.exists() and output.parent == ROOT/'TestResults', 'Use a new output directly in TestResults')
    coverage, selector, coverage_path, selector_path = ingredients()
    inputs = Inputs()
    manifest = inputs.read(RUN/'files.json', RUN_PIN)
    execution_manifest = inputs.read(EXECUTION/'files.json', EXECUTION_PIN)
    admission_manifest = inputs.read(ADMISSION/'files.json', ADMISSION_PIN)
    read_run = lambda name: inputs.read(RUN/name, manifest[name])
    result = read_run('result.json')
    native_audit = inputs.read(EXECUTION/'public-audit.log', execution_manifest['public-audit.log'])
    independent = inputs.read(EXECUTION/'independent-audit.json', execution_manifest['independent-audit.json'])
    completion = inputs.read(EXECUTION/'completion.json', execution_manifest['completion.json'])
    require(result == native_audit and result['integrityStatus'] == 'Verified'
            and independent['status'] == 'Passed' and completion['scope'] == 'Closed', 'Source execution is not verified and closed')
    source_pins = inputs.read(ADMISSION/'compiled-source-files.json', admission_manifest['compiled-source-files.json'])
    for name in ['TowerSuppliedCompositionSearch.cs', 'TowerBossPartyGenerator.cs', 'TowerBossStudyPolicy.cs', 'TowerBossGeneration.cs']:
        relative = 'LL/tools/BalanceHarness/'+name
        captured = ADMISSION/'source'/relative
        require(sha(captured) == source_pins[relative] == sha(ROOT/relative), 'Changed producing algorithm source')
        inputs.consumed[str(captured)] = source_pins[relative]
        inputs.consumed[str(ROOT/relative)] = source_pins[relative]
    base, _ = coverage.analyze_study(0, lambda name: read_run('study/'+name.removeprefix('study-0/')))
    d, study = read_run('study/definition.json'), read_run('study/study.json')
    trial_path = RUN/'study/trials.jsonl'
    require(sha(trial_path) == manifest['study/trials.jsonl'], 'Changed trial index')
    inputs.consumed[str(trial_path)] = manifest['study/trials.jsonl']
    study['_trials'] = [json.loads(line) for line in trial_path.read_text().splitlines()]
    summary = analyze(d, study, base, coverage, selector)
    history_path = EXECUTION/'live-history-files.json'
    history = inputs.read(history_path, execution_manifest['live-history-files.json'])
    # One full current scan: preservation only, with the previously audited recovery.
    common_path = ROOT/'TestResults/three-reference-admission-closeout-20260922/comparison-preparation.py'
    common = load('history_common', common_path, 'edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b')
    current_files, values = common.history(RUN.parent, read_run('request.json'))
    require(current_files == history and len(values) == 551408 and len(history) == 232, 'Changed full history')
    summary['preservedHistory'] = dict(exclusions=len(values), files=len(history), exactInventory=True)
    inputs.recheck()
    output.mkdir()
    for original, name in [(Path(__file__), 'analyzer.py'), (coverage_path, 'coverage-arithmetic.py'),
                            (selector_path, 'selection-arithmetic.py')]:
        shutil.copyfile(original, output/name)
    common.save(output/'summary.json', summary)
    common.save(output/'trajectory.json', base)
    common.save(output/'inputs.json', dict(consumed=inputs.consumed, scientificManifestSha256=RUN_PIN,
        executionManifestSha256=EXECUTION_PIN, coverageHelperSha256=COVERAGE_PIN, selectorHelperSha256=SELECTOR_PIN))
    common.save(output/'live-history-files.json', current_files)
    common.save(output/'completion.json', dict(status=summary['status'], elapsedSeconds=time.monotonic()-started,
        newFights=0, newValues=0, newGeneratedParties=0, nativePreparations=0,
        exclusions=551408, historyFiles=232, consumedInputs=len(inputs.consumed),
        engineeringAccounting='Separately disclosed read-only analysis; no transfer from the closed scientific allowance'))
    common.save(output/'files.json', common.inventory(output))
    common.save(output.with_name(output.name+'-pin.json'), dict(status=summary['status'],
        manifestSha256=sha(output/'files.json'), elapsedSeconds=time.monotonic()-started,
        retainedBytes=sum(p.stat().st_size for p in output.iterdir())))
    print(json.dumps({k:v for k,v in summary.items() if k not in ('candidates',)}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=OUTPUT)
    args = parser.parse_args()
    run(args.output.resolve())
