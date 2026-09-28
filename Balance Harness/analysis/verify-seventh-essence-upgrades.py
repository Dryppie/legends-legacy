"""Independently reconstruct the addition-only recipes, raw outcomes and level contrasts."""
import argparse
import copy
import gzip
import importlib.util
import json
import math
from pathlib import Path

spec = importlib.util.spec_from_file_location('upgrade_audit_io', Path(__file__).with_name('verify-tower-gear-coverage.py'))
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
require, sha, read, member = io.require, io.sha, io.read, io.member
VERSION = 'tower-floor11-seventh-essence-screen-v1'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('study', 'owner', 'receipt'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--manifest-pin', required=True)
    args = parser.parse_args()
    study, owner, receipt = args.study.resolve(), args.owner.resolve(), args.receipt.resolve()
    require(not receipt.exists() and not receipt.is_relative_to(study), 'Choose a new receipt outside the sealed study')
    require(sha(study / 'files.json') == args.manifest_pin, 'Changed manifest')
    files = read(study / 'files.json')
    require({p.relative_to(study).as_posix() for p in study.rglob('*') if p.is_file()} == set(files) | {'files.json'}, 'Archive inventory differs')
    for name, digest in files.items():
        require(sha(member(study, name)) == digest, 'Changed archive member: ' + name)
    q, declaration = read(owner / 'request.json'), read(owner / 'declaration.json')
    require(q == read(study / 'request.json') and sha(owner / 'request.json') == declaration['requestSha256'], 'Request binding differs')
    require(q['version'] == declaration['version'] == VERSION, 'Wrong version')
    require(all(declaration[k] == v for k, v in dict(qualificationCells=112, addedCells=116, samples=32, maximumFights=7296,
                qualificationFightsIncluded=3584, maximumSeconds=900, maximumBytes=2 * 1073741824, newSeeds=0, retries=0).items()), 'Changed envelope')
    for name, digest in {**q['inputHashes'], **declaration['sourcePins']}.items():
        require(sha(Path(name)) == digest, 'Changed frozen input: ' + name)
    process, completion = read(owner / 'process.json'), read(owner / 'completion.json')
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0, 'Owner did not complete and drain')
    require(completion['archiveManifestSha256'] == args.manifest_pin and completion['resultSha256'] == sha(study / 'result.json'), 'Completion binding differs')
    require(read(study / 'completion.json') == dict(status='Complete', attempts=7296, completed=7296, retries=0), 'Combat accounting differs')
    require([json.loads(line)['attempt'] for line in (study / 'attempts.jsonl').read_text().splitlines()] == list(range(1, 7297)), 'Attempt journal differs')
    require(read(study / 'preflight.json') == dict(status='PreparedNoFights', cells=228, knownInputMatches=3584), 'Preparation differs')
    source = Path(q['source'])
    require(q['sourcePin'] == sha(source / 'files.json') == 'feaae1ff33bfccd145c1cd733a91b8dc313f4b965f6d0b4394dffab3a4697c03', 'Changed calibration source')
    source_files = read(source / 'files.json')

    def source_json(name):
        require(sha(member(source, name)) == source_files[name], 'Changed source member: ' + name)
        return read(source / name)

    scope = read(study / 'scope.json'); old_scope = source_json('variant-03/scope.json')
    require({**scope, 'algorithm': old_scope['algorithm'], 'execution': old_scope['execution']} == old_scope, 'Changed content or settings')
    require(scope['algorithm'] == VERSION and scope['execution']['assemblyHashes'] == q['assemblyHashes'], 'Wrong execution')
    for name, digest in q['assemblyHashes'].items():
        require(sha(study / 'executable' / (name + '.dll')) == digest, 'Wrong captured executable')
    for name, digest in read(study / 'runtime-files.json').items():
        require(sha(member(study / 'executable', name)) == digest, 'Changed captured runtime file')
    for name, digest in scope['contentHashes'].items():
        require(sha(study / 'content/Data' / name) == digest == sha(study / 'study/content/Data' / name), 'Content copy differs')
    old_cells = source_json('cells.json'); require(len(old_cells) == 112, 'Missing prior controls')
    seeds = old_cells[0]['scenario']['seeds']; require(len(seeds) == len(set(seeds)) == 32, 'Wrong historical seed panel')
    expected_cells = [dict(id=c['case'] + '/' + c['profile'], sourceCase=c['case'], kind='retained-control', addition=None,
                           profile=c['profile'], scenario=c['scenario']) for c in old_cells]
    definitions = read(study / 'content/Data/essences/essences.json')['essences']
    families = {e['id']: e['sourceMonsterId'].lower() for e in definitions}
    eligible = {}
    for name in ('floor11-six-1', 'floor11-six-2'):
        original = next(c['scenario'] for c in old_cells if c['case'] == name and c['profile'] == 'resistance-and-health')
        used = {families[e] for p in original['party'] for e in p['build']['essenceIds']}
        eligible[name] = sorted(e['id'] for e in definitions if e['sourceMonsterId'].lower() not in used)
        require(len(eligible[name]) == 57, 'Wrong eligible family')
        for addition in [None, *eligible[name]]:
            scenario = copy.deepcopy(original)
            for p in scenario['party']:
                b = p['build']; require(len(b['essenceIds']) == 6 and b['characterLevel'] == 50, 'Wrong source progression')
                b['identityProgression'] = dict(characterLevel=b['characterLevel'], essenceIds=b.pop('identityEssenceIds', list(b['essenceIds'])))
                b['characterLevel'] = 60
                if addition is not None:
                    b['essenceIds'].append(addition)
            scenario['assumptions'].append(f'Controlled progression comparison: level 60; appended Essence {addition or "none"}; original six Essences, gear and instance identities retained. No transferred strength claim.')
            expected_cells.append(dict(id=name + ('/add/' + addition if addition else '/level60-six'), sourceCase=name,
                                       kind='seventh-addition' if addition else 'level-control', addition=addition,
                                       profile='resistance-and-health', scenario=scenario))
    require(eligible == declaration['eligibility'] == read(study / 'eligible-additions.json'), 'Eligibility declaration differs')
    cells = read(study / 'cells.json'); require(cells == expected_cells, 'Recipe, order, identity pin or schedule differs')
    require(sha(source / 'variant-03/study/trials.jsonl') == source_files['variant-03/study/trials.jsonl'], 'Changed baseline ledger')
    old_trials = [json.loads(line) for line in (source / 'variant-03/study/trials.jsonl').read_text().splitlines()]
    require(len(old_trials) == 3584, 'Incomplete parity family')
    trials = [json.loads(line) for line in (study / 'study/trials.jsonl').read_text().splitlines()]
    require(len(trials) == len({t['cacheKey'] for t in trials}) == 7296, 'Incomplete trials or duplicate cache identity')
    observations = {}; matched = 0
    for n, trial in enumerate(trials):
        cell, seed = cells[n // 32], seeds[n % 32]
        require(trial['id'] == f'trial-{n+1:06}' and trial['stage'] == cell['id'] and trial['seed'] == seed, 'Trial schedule differs')
        require(read(member(study / 'study/recipes', trial['recipe'] + '.json')) == cell['scenario'], 'Trial recipe differs')
        raw = json.loads(gzip.decompress((study / 'study/battles' / (trial['id'] + '.json.gz')).read_bytes()))
        require(raw['battle']['seed'] == seed and raw['battle']['scenarioId'] == cell['scenario']['id'], 'Report binding differs')
        require(raw['succeeded'] == (raw['battle']['summary']['contentOutcome'] == 'Victory'), 'Outcome differs')
        if n < 3584:
            old = old_trials[n]; name = 'variant-03/study/battles/' + old['id'] + '.json.gz'
            require(sha(source / name) == source_files[name], 'Changed parity report')
            require(trial['inputHash'] == old['inputHash'] and raw == json.loads(gzip.decompress((source / name).read_bytes())), 'Full-report parity differs')
            matched += 1
        observations.setdefault(cell['id'], []).append(raw)
    result = read(study / 'result.json')
    require(result['status'] == 'ProgressionUpgradeScreenComplete' and result['fights'] == 7296 and result['runtimeParityReports'] == matched == 3584, 'Incomplete result')
    require(result['balanceAcceptance'] == 'NotAssessedHistoricalSeeds' and all(result[k] == 0 for k in ('newSeeds', 'confirmedTeams', 'supportedSearchRuns', 'retries')), 'Unexpected acceptance or accounting')
    for row, cell in zip(result['rows'], cells, strict=True):
        values = observations[cell['id']]; wins = [v['succeeded'] for v in values]
        expected = {k: cell[k] for k in ('id', 'sourceCase', 'kind', 'addition', 'profile')}
        expected.update(level=cell['scenario']['party'][0]['build']['characterLevel'], essenceSlots=len(cell['scenario']['party'][0]['build']['essenceIds']),
                        wins=sum(wins), draws=sum(v['battle']['summary']['contentOutcome'] == 'Draw' for v in values), samples=32,
                        gainedAgainstLevel50=None, lostAgainstLevel50=None, gainedAgainstLevel60=None, lostAgainstLevel60=None)
        if cell['kind'] != 'retained-control':
            for level, suffix in ((50, '/resistance-and-health'), (60, '/level60-six')):
                paired = list(zip(wins, [v['succeeded'] for v in observations[cell['sourceCase'] + suffix]], strict=True))
                expected['gainedAgainstLevel' + str(level)] = sum(a and not b for a, b in paired)
                expected['lostAgainstLevel' + str(level)] = sum(not a and b for a, b in paired)
        require(all(row[k] == v for k, v in expected.items()), 'Reconstructed outcomes or level contrasts differ')
        require(math.isclose(row['meanGuardianHealth'], sum(v['guardianHealthRemainingPercent'] for v in values)/32, abs_tol=1e-6), 'Health mean differs')
        require(math.isclose(row['meanDurationSeconds'], sum(v['battle']['summary']['durationSeconds'] for v in values)/32, abs_tol=1e-6), 'Duration mean differs')
    leaders = [sorted((r for r in result['rows'] if r['sourceCase'] == name and r['kind'] == 'seventh-addition'),
                      key=lambda r: (-r['wins'], r['meanGuardianHealth'], r['addition']))[0] for name in eligible]
    require(result['leaders'] == leaders, 'Observed leaders differ')
    handoff = read(study / 'handoff.json')
    ids = {r['id'] for r in result['rows'] if r['kind'] == 'level-control' or r['kind'] == 'seventh-addition'
           and r['wins'] == next(x['wins'] for x in leaders if x['sourceCase'] == r['sourceCase'])}
    expected_handoff = [dict(c, scenario={**c['scenario'], 'seeds': []}) for c in cells if c['id'] in ids]
    require(handoff == dict(status='DiagnosticCandidatesNotConfirmed', leaders=leaders, teams=expected_handoff), 'Seed-free co-leader handoff differs')
    size = sum(p.stat().st_size for p in study.rglob('*') if p.is_file()); require(size <= 2 * 1073741824, 'Storage limit exceeded')
    audit = dict(status='Verified', authenticatedFiles=len(files), studyBytes=size, fights=7296, newFights=0, newSeeds=0,
                 allOriginalEssencesGearAndIdentityPinsPreserved=True, levelAndSlotContrastsVerified=True,
                 baselineInputsAndFullReportsMatched=3584, handoffTeams=len(expected_handoff),
                 archiveManifestSha256=args.manifest_pin, resultSha256=sha(study / 'result.json'))
    with receipt.open('x', encoding='utf-8') as stream:
        json.dump(audit, stream, indent=2)
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    main()
