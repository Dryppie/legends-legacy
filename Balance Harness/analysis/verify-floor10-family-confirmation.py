"""Independently verify the complete fresh floor-10 family at selected factor 7.75."""
import argparse
import gzip
import hashlib
import importlib.util
import json
import math
from pathlib import Path
from statistics import NormalDist

spec = importlib.util.spec_from_file_location('floor10_family_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
require, sha, read, member = io.require, io.sha, io.read, io.member
VERSION = 'tower-floor10-fixed-family-confirmation-v1'


def interval(wins):
    n, family = 256, 21
    z = NormalDist().inv_cdf(1 - .025 / family)
    p = wins / n
    denominator = 1 + z * z / n
    center = (p + z * z / (2 * n)) / denominator
    width = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / denominator
    return dict(rate=p, lower=max(0, center-width), upper=min(1, center+width), confidence=1-.05/family)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('owner', 'receipt'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args = parser.parse_args()
    owner, receipt = args.owner.resolve(), args.receipt.resolve()
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    study, source, history_source = [Path(q[k]).resolve() for k in ('output', 'source', 'historySource')]
    require(not receipt.exists() and not receipt.is_relative_to(study), 'New receipt outside sealed study required')
    require(q['version'] == declaration['version'] == VERSION and read(study / 'request.json') == q
            and sha(owner / 'request.json') == declaration['requestSha256'], 'Changed request')
    require(sha(study / 'files.json') == args.manifest_pin, 'Changed manifest')
    files = read(study / 'files.json')
    require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Archive inventory differs')
    for name, digest in files.items():
        require(sha(member(study, name)) == digest, 'Changed archive member: ' + name)
    for name, digest in {**q['inputHashes'], **q['recoveryHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed frozen input: ' + name)
    require(all(declaration[k] == v for k, v in dict(cells=21, intendedCells=21, controlCells=0, samples=256,
                freshSeeds=256, maximumFights=5376, maximumSeconds=900, maximumBytes=2*1073741824,
                retries=0, intervalPolicy='bonferroni-wilson-95-v1', intervalFamilySize=21).items()), 'Changed envelope')
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Owner did not drain')
    require(completion['status'] == 'Complete' and completion['archiveManifestSha256'] == args.manifest_pin
            and completion['resultSha256'] == sha(study / 'result.json') and completion['fights'] == 5376
            and completion['freshSeeds'] == 256 and completion['retries'] == 0, 'Completion differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=5376, completed=5376, retries=0), 'Incomplete combat')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 5377)), 'Attempt journal differs')
    protocol = read(study / 'protocol.json')
    require(all(protocol[k] == v for k, v in dict(version=VERSION, cells=21, samples=256, maximumFights=5376,
                maximumSeconds=840, maximumBytes=2*1073741824, retries=0, freshSeeds=256).items()), 'Native protocol differs')
    require(sha(source / 'files.json') == q['sourcePin'] == 'b866bb74e5aa897dcd241423208fe53a47bdf5a583846434a07c460f6f6a015f'
            and sha(history_source / 'files.json') == q['historyPin']
            == '41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378', 'Changed source')
    source_files = read(source / 'files.json')

    def source_json(name):
        require(sha(member(source, name)) == source_files[name], 'Changed source member: ' + name)
        return read(source / name)

    source_result = source_json('result.json')
    choices = [s for s in source_result['summaries'] if s['multiplier'] == 7.75]
    require(source_result['status'] == 'RefinedFloor10CalibrationComplete' and len(choices) == 1, 'Wrong calibration')
    selected = choices[0]
    require(selected['variant'] == 7 and selected['eligible'] and selected['health'] == 12.71 and selected['offense'] == 7.13
            and selected == source_result['selected'] == declaration['selected'] == read(study / 'selected.json'), 'Changed preselected setting')
    variant = f"variant-{selected['variant']:02}"
    scope = read(study / 'scope.json')
    require(scope == {**source_json(variant + '/scope.json'), 'algorithm': VERSION}, 'Producing runtime/settings/content changed')
    for name, digest in scope['execution']['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong runtime')
    for name, digest in read(study / 'runtime-files.json').items():
        require(sha(member(study / 'executable', name)) == digest, 'Runtime inventory differs')
    for name, digest in scope['contentHashes'].items():
        require(sha(study / 'content/Data' / name) == sha(study / 'study/content/Data' / name) == digest, 'Changed selected content')
    require(read(study / 'study/scope.json') == scope, 'Nested archive scope differs')
    nested = read(study / 'study/files.json')
    require({p.relative_to(study / 'study').as_posix() for p in (study / 'study').rglob('*') if p.is_file()} == set(nested) | {'files.json'}, 'Nested inventory differs')
    require(all(files['study/' + name] == digest for name, digest in nested.items()), 'Nested manifest differs')
    prior_files = read(history_source / 'files.json')
    require(sha(history_source / 'seed-ledger.json') == prior_files['seed-ledger.json'], 'Changed historical ledger')
    prior_result = read(history_source / 'result.json')
    require(sha(history_source / 'result.json') == prior_files['result.json']
            and prior_result == read(study / 'history-source-result.json')
            and prior_result['status'] == 'Floor11FamilyConfirmationComplete'
            and prior_result['assessment'] == 'Pass'
            and prior_result['fights'] == 58368 and prior_result['exclusionUnion'] == 834807
            and declaration['historyManifest'] == q['historyPin'], 'Historical source lost or changed')
    require(sha(history_source / 'selected.json') == prior_files['selected.json']
            and read(history_source / 'selected.json')['multiplier'] == 2.25, 'Wrong historical source')
    prior = read(history_source / 'seed-ledger.json')
    history = set(prior['historical'] + prior['first'] + prior['second'])
    require(len(history) == 834807, 'Wrong exclusion union')
    history_files = read(study / 'history-files.json')
    require(all(history_files.get(n) == h for n, h in q['requiredHistory'].items()), 'Lost required history')
    for name, digest in history_files.items():
        require(sha(Path(name)) == digest, 'Changed historical reservation')
    seeds = read(study / 'seeds.json')
    require(seeds['historical'] == sorted(history), 'Wrong allocation exclusions')
    allocator = read(study / 'reservation-intent.json')['allocator']
    require(allocator == dict(algorithm='sha256-us-int32le-reject-v1', domain=VERSION + '/block-1', master=q['master'],
                             firstCount=1, secondCount=255, maximumCandidatesPerStage=100000), 'Wrong allocator')
    used, panel, candidates, rejections = set(history), [], 0, 0
    journal = iter(json.loads(line) for line in (study / 'allocation-journal.jsonl').read_text().splitlines())
    for stage, count in [('first', 1), ('second', 255)]:
        values, ordinal = [], 0
        while len(values) < count:
            require(ordinal < 100000, 'Allocation exceeded limit')
            require(next(journal) == dict(kind='Start', stage=stage, ordinal=ordinal, value=None, accepted=None), 'Unmatched allocation start')
            digest = hashlib.sha256('\x1f'.join(map(str, [allocator['domain'], q['master'], stage, ordinal])).encode()).digest()
            value = int.from_bytes(digest[:4], 'little', signed=True)
            accepted = value not in used
            require(next(journal) == dict(kind='Candidate', stage=stage, ordinal=ordinal, value=value, accepted=accepted), 'Derivation/rejection differs')
            candidates += 1
            if accepted:
                used.add(value); values.append(value)
            else:
                rejections += 1
            ordinal += 1
        require(values == seeds[stage], 'Accepted schedule differs')
        panel.extend(values)
    require(next(journal, None) is None, 'Extra allocation events')
    reservation = read(study / 'reservation.json')
    require(reservation['status'] == 'Complete' and (reservation['candidates'], reservation['rejections']) == (candidates, rejections), 'Reservation accounting differs')
    require(len(panel) == len(set(panel)) == 256 and not history.intersection(panel), 'Panel not fresh')
    require(read(study / 'confirmation-seeds.json') == panel
            and read(study / 'history-input.json') == dict(reservationState='Complete', reserved=panel)
            and read(study / 'seed-ledger.json') == dict(reservationState='Complete', historical=sorted(history), first=seeds['first'], second=seeds['second']), 'Incomplete durable reservation')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', cells=21, historyCount=len(history), identicalProducingRuntime=True), 'Preflight differs')
    cells = read(study / 'cells.json')
    expected_cells = [{**c, 'scenario': {**c['scenario'], 'seeds': []}} for c in source_json('cells.json')]
    require(cells == expected_cells and len(cells) == len({c['case'] + "/" + c['profile'] for c in cells}) == 21, 'Changed complete recipe family')
    cases = ['floor10-authored', 'floor10-retained-1', 'floor10-retained-2']
    profiles = ['baseline', 'precision', 'ability-haste', 'restorer-specialization', 'armor-and-health', 'resistance-and-health', 'health-and-regeneration']
    require([(c['case'], c['profile']) for c in cells] == [(c, p) for c in cases for p in profiles], 'Dropped source family')
    for cell in cells:
        require(cell['floor'] == cell['scenario']['floorNumber'] == 10 and cell['essenceSlots'] == 6
                and cell['purpose'] == 'intended-progression' and len(cell['scenario']['party']) == 15, 'Changed floor/budget')
        for actor in cell['scenario']['party']:
            b = actor['build']
            require((b['characterLevel'], b['tier'], b['rank'], b['quality'], len(b['essenceIds'])) == (50, 2, 5, 'Masterpiece', 6)
                    and all(e['definitionId'].endswith('.rarity.legendary') for e in b['equipment']), 'Changed equipment budget')
    def cell_id(c):
        return c['case'] + '/' + c['profile']
    require(set(source_json('cells.json')[0]['scenario']['seeds']).issubset(history), 'Historical screen seeds missing from exclusions')
    trials = [json.loads(line) for line in (study / 'study/trials.jsonl').read_text().splitlines()]
    require(len(trials) == len({t['cacheKey'] for t in trials}) == 5376, 'Incomplete trials or duplicate cache identity')
    counts = {cell_id(c): [0, 0] for c in cells}
    for n, trial in enumerate(trials):
        cell, seed = cells[n // 256], panel[n % 256]
        require(trial['id'] == f'trial-{n+1:06}' and trial['stage'] == cell_id(cell) and trial['seed'] == seed, 'Trial schedule differs')
        require(read(member(study / 'study/recipes', trial['recipe'] + '.json')) == {**cell['scenario'], 'seeds': panel}, 'Recipe changed')
        raw = json.loads(gzip.decompress((study / 'study/battles' / (trial['id'] + '.json.gz')).read_bytes()))
        require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
        require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome differs')
        counts[cell_id(cell)][0] += raw['succeeded']
        counts[cell_id(cell)][1] += raw['battle']['summary']['contentOutcome'] == 'Draw'
    result = read(study / 'result.json')
    require(result['status'] == 'Floor10FamilyConfirmationComplete' and result['fights'] == 5376 and result['freshSeeds'] == 256
            and result['historicalSeeds'] == len(history) and result['exclusionUnion'] == len(used) and result['retries'] == 0, 'Result accounting differs')
    require(result['selectedFactor'] == 7.75 and result['historyManifest'] == q['historyPin'], 'Changed confirmation context')
    expected_rows = []
    for row, cell in zip(result['rows'], cells, strict=True):
        wins, draws = counts[cell_id(cell)]
        expected = dict(id=cell_id(cell), level=cell['scenario']['party'][0]['build']['characterLevel'],
                        essenceSlots=len(cell['scenario']['party'][0]['build']['essenceIds']), wins=wins, draws=draws, samples=256)
        require(all(row[k] == v for k, v in expected.items()), 'Counts or budgets differ')
        bounds = interval(wins)
        require(all(math.isclose(row['adjusted'][k], v, abs_tol=1e-8) for k, v in bounds.items()), 'Independent simultaneous Wilson bounds differ')
        expected_rows.append({**expected, 'adjusted': bounds})
    require(all(r['essenceSlots'] == 6 and r['level'] == 50 for r in expected_rows), 'Changed progression budget')
    failed = any(r['wins'] > 128 for r in expected_rows) or all(r['adjusted']['upper'] < .1 for r in expected_rows)
    passed = all(r['adjusted']['upper'] <= .5 for r in expected_rows) and any(r['adjusted']['lower'] >= .1 for r in expected_rows)
    assessment = 'Fail' if failed else 'Pass' if passed else 'Inconclusive'
    require(result['assessment'] == completion['assessment'] == assessment, 'Assessment differs')
    size = sum(p.stat().st_size for p in study.rglob('*') if p.is_file())
    require(size <= 2*1073741824, 'Storage cap exceeded')
    audit = dict(status='Verified', authenticatedFiles=len(files), studyBytes=size, fights=5376, freshSeeds=256,
                 historicalExclusions=len(history), exclusionUnion=len(used), allocationRejections=rejections,
                 allRecipesPreserved=True, identicalProducingRuntime=True, simultaneousBoundsVerified=True,
                 assessment=assessment, selectedFactor=7.75, lowerEssenceControls=0, newFights=0, archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
