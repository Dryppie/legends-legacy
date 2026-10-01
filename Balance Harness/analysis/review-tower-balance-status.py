"""Authenticate and summarize saved Tower evidence without running combat.

Read-only inputs; the output must be a new file. No seed allocation, archive
repair, sample pooling, runtime substitution or gameplay mutation is supported.
"""
import argparse
from collections import Counter
import gzip
import hashlib
import json
import math
from pathlib import Path
from statistics import NormalDist, mean, median

ROOT = Path(__file__).resolve().parents[2]
TOWER = ROOT / 'LL/src/API/API.LL/Data/world-tower/tower-floors.json'
POISON_MARKERS = {'essence.poisonous_rat', 'essence.venomous_snake',
                  'essence.venomous_spiderling', 'essence.viper'}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def composition(cell):
    # Sorting is solely for counting; archived recipes are never rewritten.
    return tuple(sorted((m['partySlot'], tuple(sorted(m['build']['essenceIds'])))
                        for m in cell['scenario']['party']))


def composition_id(cell):
    return hashlib.sha256(json.dumps(composition(cell), separators=(',', ':')).encode()).hexdigest()


def interval(wins, samples, family):
    require(0 <= wins <= samples and samples > 0 and family > 0, 'Invalid counts')
    z = NormalDist().inv_cdf(1 - .025 / family)
    p = wins / samples
    denominator = 1 + z*z/samples
    center = (p + z*z/(2*samples)) / denominator
    width = z*math.sqrt(p*(1-p)/samples + z*z/(4*samples*samples))/denominator
    return max(0, center-width), min(1, center+width)


def describe(values):
    if not values:
        return None
    values = sorted(values)
    return dict(count=len(values), mean=mean(values), median=median(values),
                p90=values[math.ceil(.9*len(values))-1], minimum=values[0], maximum=values[-1])


def budget(cell):
    builds = [p['build'] for p in cell['scenario']['party']]
    return dict(partySize=len(builds), levels=sorted({b['characterLevel'] for b in builds}),
                essenceCounts=sorted({len(b['essenceIds']) for b in builds}),
                tiers=sorted({b['tier'] for b in builds}), ranks=sorted({b['rank'] for b in builds}),
                qualities=sorted({b['quality'] for b in builds}),
                rarities=sorted({e['definitionId'].rsplit('.rarity.', 1)[-1]
                                 for b in builds for e in b['equipment']}),
                rolls=sorted({b['attributeRollMultiplier'] for b in builds}),
                activeStyles=any(e.get('activeStyleId') or e.get('useNativeStyle')
                                 for b in builds for e in b['equipment']))


def distinct_budgets(cells):
    return list({json.dumps(budget(c), sort_keys=True): budget(c) for c in cells}.values())


class Archive:
    def __init__(self, entry):
        self.path = ROOT / entry['source']
        require(sha(self.path/'files.json') == entry['manifestSha256'], 'Changed manifest: '+str(self.path))
        self.files = read(self.path/'files.json')
        for name in self.files:
            require((self.path/name).resolve().is_relative_to(self.path.resolve()), 'Archive path escapes source')

    def member(self, name):
        require(name in self.files and sha(self.path/name) == self.files[name], 'Changed member: '+name)
        return read(self.path/name)

    def recount(self, trial_directory, cells, rows):
        prefix = trial_directory+'/' if trial_directory else ''
        journal = prefix+'trials.jsonl'
        require(sha(self.path/journal) == self.files[journal], 'Changed trial journal')
        trials = {}
        stats = {key: dict(seeds=set(), wins=0, draws=0, health=[], display=[], engine=[], victory=[]) for key in cells}
        for line in (self.path/journal).read_text(encoding='utf-8').splitlines():
            t = json.loads(line)
            name = prefix+'battles/'+t['id']+'.json.gz'
            require(name not in trials and t['stage'] in cells and name in self.files, 'Invalid trial identity')
            trials[name] = t
        checked_bytes = 0
        for name, pin in self.files.items():
            data = (self.path/name).read_bytes()
            require(hashlib.sha256(data).hexdigest() == pin, 'Changed archive member: '+name)
            checked_bytes += len(data)
            if name not in trials:
                continue
            t = trials[name]
            b = json.loads(gzip.decompress(data))
            s = stats[t['stage']]
            require(b['battle']['seed'] == t['seed'] and t['seed'] not in s['seeds'], 'Wrong or repeated seed within cell')
            s['seeds'].add(t['seed'])
            s['wins'] += int(b['succeeded'])
            s['draws'] += int(b['battle']['summary']['engineOutcome'] == 'Draw')
            s['health'].append(b['guardianHealthRemainingPercent'])
            s['display'].append(b['displayDurationSeconds'])
            s['engine'].append(b['battle']['summary']['durationSeconds'])
            if b['succeeded']:
                s['victory'].append(b['displayDurationSeconds'])
        for row in rows:
            s = stats[row['id']]
            require(len(s['seeds']) == row['samples'] and s['wins'] == row['wins'], 'Recount mismatch: '+row['id'])
            if 'draws' in row:
                require(s['draws'] == row['draws'], 'Draw mismatch')
            for field, values in [('meanSeconds', s['display']), ('meanDurationSeconds', s['engine']),
                                  ('meanGuardianHealth', s['health'])]:
                if field in row:
                    require(abs(mean(values)-row[field]) < 1e-7, 'Mean mismatch: '+row['id'])
        require(len({tuple(sorted(s['seeds'])) for s in stats.values()}) == 1, 'Unequal within-family panels')
        return stats, dict(authenticatedFiles=len(self.files), authenticatedBytes=checked_bytes, recountedReports=len(trials))


def review_floor(entry, reference, current):
    a = Archive(entry)
    audit_path = ROOT/entry['audit']
    require(sha(audit_path) == entry['auditSha256'], 'Changed independent audit')
    audit = read(audit_path)
    result = a.member('result.json')
    require(audit['status'] == 'Verified' and audit['resultSha256'] == sha(a.path/'result.json'), 'Unbound audit')
    raw = a.member('cells.json')
    cells = {c.get('id') or c['case']+'/'+c['profile']: c for c in raw}
    rows = result['rows']
    require(len(cells) == len(raw) and {r['id'] for r in rows} == set(cells), 'Incomplete family')
    floor = entry['floor']
    scope = a.member('scope.json')
    snapshot = a.member('content/Data/world-tower/tower-floors.json')
    target = next(f for f in snapshot['floors'] if f['floorNumber'] == floor)
    live = next(f for f in current['floors'] if f['floorNumber'] == floor)
    require(target == live, 'Target floor differs from current data: '+str(floor))
    changed_content = []
    for name, pin in a.files.items():
        if name.startswith('content/') and name != 'content/Data/world-tower/tower-floors.json':
            path = ROOT/'LL/src/API/API.LL'/name.removeprefix('content/')
            if not path.exists() or sha(path) != pin:
                changed_content.append(name)
    # Early confirmations use a different archive layout, independently of floor.
    legacy = 'confirmation-seeds.json' in a.files
    trial_directory = 'study' if legacy else 'evaluation'
    require(trial_directory+'/trials.jsonl' in a.files, 'Missing confirmation trial journal')
    captured_bounds = ({r['id']: r['adjusted'] for r in rows} if legacy
                       else {r['id']: r for r in audit['assessment']['bounds']})
    require((audit['assessment'] if isinstance(audit['assessment'], str) else audit['assessment']['verdict']) == 'Pass', 'Source not accepted')
    for row in rows:
        lo, hi = interval(row['wins'], row['samples'], len(cells))
        bound = captured_bounds[row['id']]
        require(abs(lo-bound['lower']) < 1e-7 and abs(hi-bound['upper']) < 1e-7, 'Adjusted bounds disagree')
        row.update(lower=bound['lower'], upper=bound['upper'], actualComposition=composition_id(cells[row['id']]),
                   gear=cells[row['id']].get('gear', cells[row['id']].get('profile')))
    stats, authenticated = a.recount(trial_directory, cells, rows)
    declared = a.member('confirmation-seeds.json') if legacy else a.member('request.json')['seeds']
    require(set(declared) == next(iter(stats.values()))['seeds'] and len(declared) == len(set(declared)), 'Declared panel mismatch')
    require(sum(r['samples'] for r in rows) == result.get('confirmationFights', result['fights']), 'Fight accounting mismatch')
    intended = [r for r in rows if floor != 11 or r['essenceSlots'] == 7]
    controls = [r for r in rows if r not in intended]
    qualified = [r for r in intended if r['lower'] >= .1 and r['upper'] <= .5]
    require(qualified and max(r['upper'] for r in intended) <= .5, 'No longer matches historical acceptance')
    require(not controls or max(r['upper'] for r in controls) < .1, 'Historical control criterion disagrees')
    leaders = {}
    for row in sorted(intended, key=lambda r: (-r['wins'], r['id'])):
        leaders.setdefault(row['actualComposition'], row)
    pacing = []
    poison = []
    for row in qualified:
        s = stats[row['id']]
        pacing.append(dict(id=row['id'], composition=row['actualComposition'], gear=row['gear'],
                           reportedSeconds=describe(s['display']), victoryReportedSeconds=describe(s['victory']),
                           engineSeconds=describe(s['engine'])))
        counts = Counter(e for m in cells[row['id']]['scenario']['party'] for e in m['build']['essenceIds'])
        poison.append(dict(id=row['id'], composition=row['actualComposition'],
                           markerCounts={k: counts[k] for k in sorted(POISON_MARKERS) if counts[k]},
                           membersWithMarkers=sum(bool(set(m['build']['essenceIds']) & POISON_MARKERS)
                                                  for m in cells[row['id']]['scenario']['party'])))
    all_keys = {composition(c) for c in raw}
    intended_keys = {composition(cells[r['id']]) for r in intended}
    control_keys = {composition(cells[r['id']]) for r in controls}
    cycle = [r for r in rows if r['gear'].startswith('cycle-')]
    return dict(floor=floor, source=entry['source'], manifestSha256=entry['manifestSha256'],
                auditSha256=entry['auditSha256'], resultSha256=sha(a.path/'result.json'), **authenticated,
                targetFloorMatchesCurrent=True, changedNonTowerContent=changed_content,
                settingsMatchLatestReference=scope['settings'] == reference['settings'],
                assembliesDifferentFromLatestReference=[k for k, v in scope['execution']['assemblyHashes'].items()
                                                         if reference['execution']['assemblyHashes'].get(k) != v],
                capturedExecution=scope['execution'], targetName=target.get('guardianName', target.get('name')),
                scaling=target['guardianScaling'], cells=len(cells), compositions=len(all_keys),
                intendedCells=len(intended), intendedCompositions=len(intended_keys),
                controlCells=len(controls), controlCompositions=len(control_keys),
                budgets=distinct_budgets(raw), intendedBudgets=distinct_budgets([cells[r['id']] for r in intended]),
                viableCells=len(qualified), viableCompositions=len({r['actualComposition'] for r in qualified}),
                viableProfiles=sorted({r['gear'] for r in qualified}), qualified=qualified,
                compositionLeaders=list(leaders.values()), maximumUpper=max(r['upper'] for r in intended),
                controlMaximumUpper=max((r['upper'] for r in controls), default=None),
                cycleCells=len(cycle), cycleMaximumWins=max((r['wins'] for r in cycle), default=None),
                samplesPerCell=len(declared), pacing=pacing, poisonMarkers=poison,
                controlBudgetNote='Composition counts ignore level and gear; do not sum across budgets.')


def review_controls(entry):
    a = Archive(entry)
    raw = a.member('cells.json')
    cells = {c['id']: c for c in raw}
    result = a.member('result.json')
    files = a.files
    # The historical controls fixture stores all references and controls together.
    journals = [n.removesuffix('trials.jsonl').rstrip('/') for n in files if n.endswith('trials.jsonl')]
    require(len(journals) == 1, 'Unexpected control journals')
    _, authenticated = a.recount(journals[0], cells, result['rows'])
    controls = [c for c in raw if c['removedIndex'] is not None]
    references = [c for c in raw if c['removedIndex'] is None]
    require(result['newSeeds'] == 0 and result['balanceAcceptance'] == 'NotAssessedHistoricalSeeds', 'Diagnostic is not acceptance')
    return dict(source=entry['source'], manifestSha256=entry['manifestSha256'], **authenticated,
                referenceCells=len(references), referenceCompositions=len({composition(c) for c in references}),
                controlCells=len(controls), controlCompositions=len({composition(c) for c in controls}),
                controlBudgets=distinct_budgets(controls), summary=result['summary'],
                historicalSamplesPerCell=result['samples'], newAcceptance=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--sources', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), 'Output already exists; use a fresh receipt path')
    config = read(args.sources)
    require([e['floor'] for e in config['floors']] == list(range(1, 16)), 'Expected floors 1–15 exactly once')
    require(sha(TOWER) == config['towerSha256'], 'Current Tower changed since source selection')
    ledger = ROOT/config['latestLedger']['path']
    require(sha(ledger) == config['latestLedger']['sha256'], 'Latest ledger changed')
    reference_entry = next(e for e in config['floors'] if e['floor'] == config['runtimeReferenceFloor'])
    reference_archive = Archive(reference_entry)
    reference = reference_archive.member('scope.json')
    # Validate the latest reference's presently available implementation/runtime.
    q = reference_archive.member('request.json')
    for path, pin in q['inputHashes'].items():
        require(Path(path).exists() and sha(Path(path)) == pin, 'Latest reference input drift: '+path)
    current = read(TOWER)
    floors = []
    for entry in config['floors']:
        print('Reviewing floor '+str(entry['floor']), flush=True)
        value = review_floor(entry, reference, current)
        floors.append(value)
        print(json.dumps({k: value[k] for k in ['floor', 'cells', 'compositions', 'viableCells', 'viableCompositions', 'viableProfiles', 'recountedReports', 'changedNonTowerContent', 'assembliesDifferentFromLatestReference']}), flush=True)
    diagnostic = review_controls(config['lowerEssenceDiagnostic'])
    require(sha(TOWER) == config['towerSha256'] and sha(ledger) == config['latestLedger']['sha256'], 'Review changed game data or ledger')
    result = dict(status='VerifiedReadOnly', sourcesSha256=sha(args.sources), reviewerSha256=sha(Path(__file__)),
                  towerSha256=sha(TOWER), latestLedger=config['latestLedger'], newFights=0, newSeeds=0,
                  runtimeReferenceFloor=config['runtimeReferenceFloor'], latestReferenceInputPins=len(q['inputHashes']),
                  currentRuntimeReplays=0, intervalPolicy='Original per-floor approximate simultaneous 95% Bonferroni-Wilson; no joint all-floor claim',
                  compositionPolicy='Per-slot Essence sets; ignores gear, identity, ordering and level without editing recipes',
                  poisonMarkerPolicy='Recipe presence only: rat, snake, spiderling, viper; not measured damage attribution',
                  quantilePolicy='Nearest-rank p90; arithmetic midpoint median; reported and exact engine seconds separate',
                  floors=floors, floor10HistoricalControls=diagnostic)
    with args.output.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print('Verified review saved: '+str(args.output), flush=True)


if __name__ == '__main__':
    main()
