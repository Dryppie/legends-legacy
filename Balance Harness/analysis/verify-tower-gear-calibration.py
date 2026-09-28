"""Independently audit the fixed gear/offense matrix. Runs zero fights."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
from pathlib import Path


def helpers():
    spec = importlib.util.spec_from_file_location('gear_calibration_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


io = helpers()
require, sha, read, member = io.require, io.sha, io.read, io.member
OFFENSES = [3.36, 4.2, 5.04, 6.72, 10.08]
PROFILES = ['baseline', 'precision', 'ability-haste', 'restorer-specialization',
            'armor-and-health', 'resistance-and-health', 'health-and-regeneration']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('study', 'owner', 'receipt'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args = parser.parse_args()
    study, owner, receipt = args.study.resolve(), args.owner.resolve(), args.receipt.resolve()
    require(not receipt.exists() and not receipt.is_relative_to(study), 'Choose a new receipt outside the sealed study')
    require(sha(study / 'files.json') == args.manifest_pin, 'Changed archive manifest')
    files = read(study / 'files.json')
    require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Archive inventory differs')
    for name, digest in files.items():
        require(sha(member(study, name)) == digest, 'Changed archive member: ' + name)
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    require(q == read(study / 'request.json') and sha(owner / 'request.json') == declaration['requestSha256'], 'Request binding differs')
    require(declaration['proposalManifestSha256'] == '36781cd2034011bd3dd78f530c541e902bcdd8809e74f1bddc7c904296703abe', 'Wrong frozen proposal')
    require(declaration['maximumFights'] == 1120 and declaration['maximumSeconds'] == 900 and declaration['maximumBytes'] == 1073741824, 'Changed limits')
    process = read(owner / 'process.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Owner did not complete and drain')
    completion = read(owner / 'completion.json')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Completion binding differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=1120, completed=1120, retries=0), 'Wrong combat accounting')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 1121)), 'Attempt journal differs')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', cells=35, inputs=1120, historicalInputsMatched=224), 'Incomplete preflight')
    for name, digest in {**q['inputHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed pinned input: ' + name)
    source = Path(q['source']).resolve()
    require(q['sourcePin'] == 'f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746'
            and sha(source / 'files.json') == q['sourcePin'], 'Wrong source archive')
    source_files = read(source / 'files.json')

    def source_json(name):
        path = member(source, name)
        require(sha(path) == source_files[name], 'Changed source member: ' + name)
        return read(path)

    proposal = read(study / 'proposal.json')
    require(proposal == read(Path(q['proposal'])) and proposal['offenseValues'] == OFFENSES, 'Proposal changed')
    controls = read(study / 'controls.json')
    seeds = proposal['seeds']
    require(len(seeds) == len(set(seeds)) == 32 and seeds == source_json('request.json')['seeds'], 'Historical seeds changed')
    expected_controls = [dict(profile=c['profile'], scenario=c['scenario']) for c in source_json('cells.json') if c['floor'] == 13]
    require(controls == expected_controls and [c['profile'] for c in controls] == PROFILES, 'Source parties or seed panels changed')
    require([{**c, 'scenario': {**c['scenario'], 'seeds': []}} for c in controls] == read(Path(q['controls'])), 'Seed-free handoff differs')
    scope = read(study / 'source-scope.json')
    original_scope = source_json('scope.json')
    require(scope == {**original_scope, 'algorithm': 'tower-gear-offense-calibration-v1'}, 'Source execution/settings drift')
    require(scope['settings'] == proposal['settings'], 'Proposal settings differ')
    for name, digest in scope['execution']['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong captured combat executable')
    for name, digest in read(study / 'runtime-files.json').items():
        require(sha(member(study / 'executable', name)) == digest, 'Runtime file differs')
    source_trial_path = source / 'study/trials.jsonl'
    require(sha(source_trial_path) == source_files['study/trials.jsonl'], 'Source trial ledger changed')
    source_trials = {(t['stage'].removeprefix('13/'), t['seed']): t for t in
                     map(json.loads, source_trial_path.read_text().splitlines()) if t['stage'].startswith('13/')}
    require(len(source_trials) == 224, 'Incomplete baseline source trials')
    floor_file = 'world-tower/tower-floors.json'
    original_tower = read(source / 'content/Data' / floor_file)
    rows, summaries, cache_keys = [], [], set()
    baseline_matches = 0
    for i, offense in enumerate(OFFENSES):
        variant = study / f'variant-{i:02}'
        variant_scope = read(variant / 'scope.json')
        require({**variant_scope, 'contentHashes': scope['contentHashes']} == scope, 'Noncontent scope changed')
        require(set(variant_scope['contentHashes']) == set(scope['contentHashes']), 'Snapshot membership changed')
        require(read(variant / 'study/scope.json') == variant_scope, 'Battle archive scope differs')
        for name, digest in variant_scope['contentHashes'].items():
            require(sha(variant / 'content/Data' / name) == digest == sha(variant / 'study/content/Data' / name), 'Content copy differs')
            if i == 0 or name != floor_file:
                require(digest == scope['contentHashes'][name], 'Unselected content changed: ' + name)
        expected_tower = copy.deepcopy(original_tower)
        next(f for f in expected_tower['floors'] if f['floorNumber'] == 13)['guardianScaling']['offense'] = offense
        require(read(variant / 'content/Data' / floor_file) == expected_tower, 'Content changes more than floor-13 offense')
        trial_root = variant / 'study'
        trial_manifest = read(trial_root / 'files.json')
        require({p.relative_to(trial_root).as_posix() for p in trial_root.rglob('*') if p.is_file()} == set(trial_manifest) | {'files.json'}, 'Native archive inventory differs')
        for name, digest in trial_manifest.items():
            require(sha(member(trial_root, name)) == digest, 'Native archive member changed')
        trials = [json.loads(line) for line in (trial_root / 'trials.jsonl').read_text().splitlines()]
        require(len(trials) == 224, 'Incomplete variant')
        observations = {p: [] for p in PROFILES}
        for n, trial in enumerate(trials):
            control, seed = controls[n // 32], seeds[n % 32]
            profile = control['profile']
            require(trial['id'] == f'trial-{n + 1:06}' and trial['stage'] == profile and trial['seed'] == seed, 'Trial schedule differs')
            require(read(member(trial_root / 'recipes', trial['recipe'] + '.json')) == control['scenario'], 'Party/Essence order differs')
            require(trial['recipe'] == source_trials[(profile, seed)]['recipe'], 'Recipe hash differs from original')
            require(trial['cacheKey'] not in cache_keys, 'Duplicate cross-variant cache identity')
            cache_keys.add(trial['cacheKey'])
            if i == 0:
                require(trial['inputHash'] == source_trials[(profile, seed)]['inputHash'], 'Baseline input drift')
                baseline_matches += 1
            else:
                require(trial['inputHash'] != source_trials[(profile, seed)]['inputHash'], 'Offense change missing from input identity')
            raw = json.loads(gzip.decompress((trial_root / 'battles' / (trial['id'] + '.json.gz')).read_bytes()))
            require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == control['scenario']['id'], 'Report binding differs')
            require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Report outcome differs')
            if i == 0:
                old_name = 'study/battles/' + source_trials[(profile, seed)]['id'] + '.json.gz'
                old_path = member(source, old_name)
                require(sha(old_path) == source_files[old_name], 'Changed historical report')
                require(raw == json.loads(gzip.decompress(old_path.read_bytes())), 'Baseline full report differs')
            observations[profile].append((raw['succeeded'], raw['guardianHealthRemainingPercent']))
        for profile, values in observations.items():
            rows.append(dict(variant=i, offense=offense, profile=profile, wins=sum(w for w, _ in values), samples=32,
                             meanGuardianHealth=sum(h for _, h in values) / 32))
        best = max(r['wins'] for r in rows if r['variant'] == i)
        summaries.append(dict(variant=i, offense=offense, bestWins=best,
                              bestProfiles=[r['profile'] for r in rows if r['variant'] == i and r['wins'] == best], eligible=4 <= best <= 28))
    require(baseline_matches == 224 and len(cache_keys) == 1120, 'Incomplete input checks')
    result = read(study / 'result.json')
    require(result['status'] == 'CalibrationComplete' and result['fights'] == 1120 and all(result[k] == 0 for k in ('newSeeds', 'confirmedTeams', 'searchRuns', 'retries')), 'Wrong result accounting')
    require(len(result['rows']) == len(rows) == 35, 'Incomplete results')
    for actual, expected in zip(result['rows'], rows, strict=True):
        require(all(actual[k] == v for k, v in expected.items() if k != 'meanGuardianHealth'), 'Reconstructed counts differ')
        require(math.isclose(actual['meanGuardianHealth'], expected['meanGuardianHealth'], abs_tol=1e-6), 'Health mean differs')
    selected = next((s for s in summaries if s['eligible']), None)
    require(result['summaries'] == summaries and result['selected'] == selected, 'Best-profile selection differs')
    require(result['decision'] == ('DiagnosticBenchmarkCandidate' if selected else 'NoInformativeBenchmark'), 'Decision differs')
    size = sum(p.stat().st_size for p in study.rglob('*') if p.is_file())
    require(size <= 1073741824, 'Storage limit exceeded')
    audit = dict(status='Verified', authenticatedFiles=len(files), studyBytes=size, fights=1120, newFights=0, freshSeeds=0,
                 allRecipesPreserved=True, onlyFloor13OffenseChanged=True, baselineInputsAndFullReportsMatched=224,
                 decision=result['decision'], selected=selected, archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
