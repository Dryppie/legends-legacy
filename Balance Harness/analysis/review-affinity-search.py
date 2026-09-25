"""Readable review of an already audited study, authenticated by its closeout pin.

Only consumed published evidence is checked. No combat, allocation, preparation,
full archive reconstruction, team promotion or executable loading takes place.
"""
import argparse
import copy
import html
import importlib.util
import json
from pathlib import Path
import re
import time

spec = importlib.util.spec_from_file_location('affinity_review_io', Path(__file__).with_name('prepare-confirmed-team-reuse.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
VERSION = 'tower-affinity-search-review-v1'
PROFILE = 'affinity-creation-with-benchmark-validation-v1'
SUPPORTED_ARMS = {
    'tower-benchmark-validation-comparison-v1': 'candidate',
    'tower-affinity-preservation-comparison-v1': 'control',
    'tower-affinity-nomination-comparison-v1': 'control',
}


def inspect(archive, closeout_pin):
    archive = io.unlinked(archive).resolve()
    io.require(archive.is_dir() and io.digest(closeout_pin), 'Supply an existing study and its externally retained closeout SHA-256.')
    io.require(not (archive / 'failure.json').exists(), 'Failed studies cannot produce a completed review.')
    consumed = {}
    manifest = {}

    def pinned(name, expected=None):
        path = io.member(archive, name)
        pin = expected or manifest.get(name)
        io.require(name not in manifest or manifest[name] == pin, 'Conflicting evidence binding: ' + name)
        io.require(io.digest(pin) and io.sha(path) == pin, 'Changed or unpinned evidence: ' + name)
        value = io.read(path)
        io.require(io.sha(path) == pin, 'Evidence changed while reading: ' + name)
        consumed[name] = pin
        return value

    closeout = pinned('closeout.json', closeout_pin)
    manifest = pinned('files.json', closeout['filesHash'])
    io.require(isinstance(manifest, dict) and all(io.digest(v) for v in manifest.values()), 'Invalid published manifest.')
    for name in manifest: io.member(archive, name)
    request = pinned('request.json', closeout['requestHash'])
    result = pinned('result.json')
    version = result['version']
    io.require(version in SUPPORTED_ARMS and version == closeout['version'] == request['version'],
               'This study does not contain the supported affinity search profile.')
    io.require(result['status'] == 'Verified' and pinned('native-audit.log') == result,
               'Missing matching published native audit.')
    independent = pinned('independent-audit.json')
    io.require(independent == dict(status='Passed', requestFileHash=closeout['requestHash'], newFights=0, newValues=0, result=result),
               'Missing matching independent audit.')
    complete = pinned('completion.json')
    io.require(complete['status'] == 'Complete' and complete['version'] == version and complete['retries'] == 0
               and complete['requestFileHash'] == closeout['requestHash'], 'Incomplete publication.')
    for name in ('native-process.json', 'native-audit-process.json', 'independent-audit-process.json', 'publication-process.json'):
        process = pinned(name)
        io.require(process['exitCode'] == 0 and process['timedOut'] is False and process['activeProcesses'] == 0,
                   'Unsuccessful process receipt: ' + name)
    plan = pinned('source/plan.json', request['plan']['sha256'])
    context = pinned('source/context.json', request['context']['sha256'])
    freeze = pinned('study/freeze.json')
    io.require(plan['version'] == freeze['version'] == version and freeze['planHash'] == result['planHash'], 'Changed study binding.')
    arm = SUPPORTED_ARMS[version]
    policy = plan[arm]
    ids = policy.get('createdDamageAffinityIds', [])
    expected = dict(version='tower-proposal-policy-v3', name='benchmark-affinity-creation-v3', firstWave=['affinity-create']*9,
                    secondWave=['affinity-create']*8, parentTickets=['benchmark'], preserveParentInteractions=False,
                    preservedDamageAffinityIds=[], createdDamageAffinityIds=ids)
    io.require(ids and policy == expected and plan['selectionContrast'][arm] == 'tower-racing-benchmark-validation-v1',
               'Changed supported search profile.')
    diagnostic = result['validation' if arm == 'candidate' else 'controlValidation']
    roots = result['roots']
    count = plan['roots']
    io.require(count == 12 and plan['heldoutSamples'] == 256 and [r['root'] for r in roots] == list(range(1, count+1))
               and [f['root'] for f in freeze['families']] == list(range(1, count+1))
               and len(diagnostic['decisions']) == count, 'Incomplete root coverage.')
    names = {e['id']: e['name'] for e in context['mechanics']['essences']}
    benchmark = plan['benchmarkPartyId']
    teams, root_rows = {}, []
    for row, family, gate in zip(roots, freeze['families'], diagnostic['decisions']):
        io.require(len(family['seeds']) == plan['heldoutSamples'] and row['benchmarkParty'] == benchmark,
                   'Changed benchmark or held-out panel.')
        roles = {}
        for member in family['members']:
            pid, scenario = member['party']['id'], copy.deepcopy(member['scenario'])
            io.require(io.digest(pid) and scenario['seeds'] == family['seeds']
                       and member['party']['builds'] == {str(a['partySlot']): a['build']['essenceIds'] for a in scenario['party']},
                       'Changed frozen team recipe.')
            scenario['seeds'] = []
            for role in member['roles']:
                io.require(role in ('control', 'candidate', 'benchmark') and role not in roles, 'Duplicate or unknown frozen role.')
                roles[role] = pid
            if pid in teams:
                io.require(teams[pid]['scenario'] == scenario, 'One party ID has different equipment or encounter context.')
            else:
                actors = sorted(scenario['party'], key=lambda a: a['partySlot'])
                slots = [a['partySlot'] for a in actors]
                io.require(slots == list(range(1, context['scope']['requiredPartySize']+1)), 'Incomplete party slots.')
                copies = {}
                for actor in actors:
                    for essence in actor['build']['essenceIds']: copies[essence] = copies.get(essence, 0)+1
                teams[pid] = dict(partyId=pid, benchmark=pid == benchmark, selectedBySupportedRoots=[],
                    requiredCopies=[dict(id=e, name=names.get(e, e), copies=n) for e, n in sorted(copies.items())],
                    loadout=[dict(slot=a['partySlot'], level=a['build']['characterLevel'],
                        essences=[dict(id=e, name=names.get(e, e)) for e in a['build']['essenceIds']]) for a in actors], scenario=scenario)
        io.require(roles == {r: row[r+'Party'] for r in ('control', 'candidate', 'benchmark')}, 'Changed selected team membership.')
        selected = row[arm+'Party']
        io.require(gate['selectedId'] == selected and type(gate['passed']) is bool and gate['samples'] == 60
                   and (selected != benchmark if gate['passed'] else selected == benchmark), 'Changed validation result.')
        teams[selected]['selectedBySupportedRoots'].append(row['root'])
        root_rows.append(dict(root=row['root'], selectedId=selected,
            status='ChallengerNeedsConfirmation' if gate['passed'] else 'BenchmarkRetained',
            gainedWins=gate['gainedWins'], lostWins=gate['lostWins'], validationSamples=60,
            selectedWins=row[arm+'Wins'], benchmarkWins=row['benchmarkWins'], heldoutSamples=plan['heldoutSamples']))
    passed = sum(r['status'] == 'ChallengerNeedsConfirmation' for r in root_rows)
    io.require(diagnostic['passedRoots'] == passed and diagnostic['fallbackRoots'] == count-passed, 'Changed validation totals.')
    review = dict(version=VERSION, profile=PROFILE, sourceVersion=version, supportedArm=arm,
        evidence='AuthenticatedPublishedResult', verificationScope='Consumed published files only; full reconstruction not repeated.',
        sourceArchive=str(archive), closeoutSha256=closeout_pin, archiveManifestSha256=closeout['filesHash'],
        decision=result['decision'], fights=result['fights'], searchFights=result['searchFights'], heldoutFights=result['heldoutFights'],
        method=result['method'], benchmark=result['benchmark'], differingRoots=result['differingRoots'],
        promisingNovelRoots=result['promisingNovelRoots'], benchmarkRetainedRoots=count-passed, provisionalChallengerRoots=passed,
        teamsConfirmedByThisStudy=0, roots=root_rows, teams=sorted(teams.values(), key=lambda t: (not t['benchmark'], t['partyId'])))
    return review, consumed


def cell(value):
    value = html.escape(str(value), quote=False).replace('\r', ' ').replace('\n', ' ')
    return re.sub(r'([\\`*_{}\[\]()!#|])', r'\\\1', value)


def markdown(review):
    labels = {t['partyId']: 'Benchmark' if t['benchmark'] else 'Team '+str(i) for i, t in enumerate(review['teams'], 1)}
    lines = ['# Affinity search review', '',
        f"**{review['benchmarkRetainedRoots']} of {len(review['roots'])} supported searches retained the benchmark.** "
        f"{review['provisionalChallengerRoots']} returned a provisional challenger requiring independent confirmation.", '',
        f"Published comparison decision: **{cell(review['decision'])}**. This study confirms no replacement team.", '',
        f"Supported profile: `{PROFILE}` ({review['supportedArm']} arm).",
        f"Completed work: {review['fights']:,} fights, including {review['searchFights']:,} search and {review['heldoutFights']:,} held-out fights.", '',
        '## Comparison results', '',
        'These compare the experimental candidate with its control and benchmark; they are not a promotion decision.', '',
        '| Endpoint | Mean gain | Descriptive 95% interval |', '| --- | ---: | ---: |']
    for key, label in [('method', 'Candidate minus control'), ('benchmark', 'Candidate minus benchmark')]:
        metric = review[key]
        lines.append(f"| {label} | {100*metric['mean']:.2f} pp | {100*metric['lower']:.2f} to {100*metric['upper']:.2f} pp |")
    lines += ['', f"Different outputs: {review['differingRoots']} roots. Promising novel outputs: {review['promisingNovelRoots']} roots.",
        'Zero contrasts from identical selected teams apply to those roots; they do not establish future equivalence.', '',
        '## Supported search by root', '',
        '| Root | Selected team | Result | Validation gained / lost wins | Held-out wins | Benchmark wins |',
        '| ---: | --- | --- | ---: | ---: | ---: |']
    for r in review['roots']:
        status = 'Needs confirmation' if r['status'] == 'ChallengerNeedsConfirmation' else 'Benchmark retained'
        lines.append(f"| {r['root']} | {labels[r['selectedId']]} | {status} | {r['gainedWins']} / {r['lostWins']} "
                     f"| {r['selectedWins']} / {r['heldoutSamples']} | {r['benchmarkWins']} / {r['heldoutSamples']} |")
    lines += ['', 'Validation counts compare the nominated challenger with the benchmark on 60 paired seeds.',
        'No root is discarded and no team is chosen by pooling the held-out results.', '', '## Frozen loadouts', '',
        'Essences are shown in their saved order. Exact equipment, progression and seed-free scenarios are in [review.json](review.json).',
        'These are review copies; running them requires a separately admitted request. Required copies do not certify account ownership.']
    for t in review['teams']:
        s = t['scenario']
        selected = ', '.join(map(str, t['selectedBySupportedRoots'])) or 'None'
        lines += ['', f"### {labels[t['partyId']]}", '', f"Party ID: `{t['partyId']}`.",
            f"Floor: {s['floorNumber']}. Preparation: {cell(s['preparationState'])}. Supported roots: {selected}."]
        lines += [f"Assumption: {cell(a)}" for a in s.get('assumptions', [])]
        lines += ['', '| Slot | Level | Essences |', '| ---: | ---: | --- |']
        for actor in t['loadout']:
            lines.append(f"| {actor['slot']} | {actor['level']} | {'; '.join(cell(e['name']) for e in actor['essences'])} |")
        lines += ['', '| Essence | Required copies |', '| --- | ---: |']
        lines += [f"| {cell(e['name'])} | {e['copies']} |" for e in t['requiredCopies']]
    lines += ['', '## Evidence', '',
        'This review authenticates the consumed published files through the supplied closeout pin and checks agreement of the saved native and independent audits.',
        'It does not reconstruct the entire archive or recheck unconsumed battle files. Use the existing study verifier for full reconstruction.',
        f"Source: {cell(review['sourceArchive'])}.", f"Closeout SHA-256: `{review['closeoutSha256']}`.",
        'The consumed file hashes are retained in [inputs.json](inputs.json). No new fights or seeds were used.', '']
    return '\n'.join(lines)


def publish(archive, closeout_pin, output):
    started = time.monotonic()
    archive, output = io.unlinked(archive).resolve(), io.unlinked(output).resolve()
    io.require(not output.exists() and output.parent.is_dir(), 'Use a new review directory with an existing parent.')
    io.require(not output.is_relative_to(archive) and not archive.is_relative_to(output), 'Review output overlaps the source archive.')
    review, consumed = inspect(archive, closeout_pin)
    output.mkdir()
    io.write(output / 'review.json', review)
    with (output / 'report.md').open('x', encoding='utf-8', newline='\n') as stream: stream.write(markdown(review))
    io.write(output / 'inputs.json', consumed)
    for name, pin in consumed.items(): io.require(io.sha(io.member(archive, name)) == pin, 'Source changed before publication: ' + name)
    io.require(not (archive / 'failure.json').exists(), 'Source failure appeared during review.')
    receipt = dict(version=VERSION, status='PublishedResultReviewed', seconds=time.monotonic()-started,
        closeoutSha256=closeout_pin, consumedFiles=len(consumed), newFights=0, newValues=0, fullReconstruction=False)
    io.require(sum(p.stat().st_size for p in output.iterdir()) < io.MAX_BYTES-65536, 'Review output exceeds its size limit.')
    io.write(output / 'completion.json', receipt)
    io.write(output / 'files.json', {p.name: io.sha(p) for p in sorted(output.iterdir())})
    return dict(**receipt, output=str(output), manifestSha256=io.sha(output / 'files.json'))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    parser.add_argument('--closeout-pin', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    try: print(json.dumps(publish(args.archive, args.closeout_pin, args.output), indent=2))
    except (OSError, ValueError, KeyError, TypeError) as error: parser.exit(1, 'Review failed: '+str(error)+'\n')
