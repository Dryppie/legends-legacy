"""Import one explicitly selected confirmed recipe with both reference controls.

This is a seed-free input handoff, not an allocated-search request. A caller pins
the already audited source manifest. Only consumed evidence is authenticated;
neither archive reconstruction nor combat is repeated. Native preparation must
match the complete producing execution identity, effective settings and content.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import sys
import time

VERSION = 'tower-confirmed-team-reuse-v1'
SOURCE_VERSION = 'tower-practical-fixed-family-confirmation-v1'
ROOT = Path(__file__).resolve().parents[2]
MAX_BYTES = 32 * 1048576


def require(ok, message):
    if not ok:
        raise ValueError(message)


def unlinked(path):
    path = Path(path).absolute()
    for item in (path, *path.parents):
        require(not item.is_symlink() and not item.is_junction(), 'Linked input/output: ' + str(item))
    return path


def sha(path):
    with unlinked(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'Duplicate JSON property: ' + key)
        result[key] = value
    return result


def read(path):
    require(unlinked(path).stat().st_size <= MAX_BYTES, 'Oversized input: ' + str(path))
    return json.loads(Path(path).read_text(encoding='utf-8-sig'), object_pairs_hook=no_duplicates,
                      parse_constant=lambda _: require(False, 'Non-finite JSON number'))


def write(path, value):
    with Path(path).open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, allow_nan=False)
        stream.write('\n')


def digest(value):
    return isinstance(value, str) and re.fullmatch('[0-9a-f]{64}', value) is not None


def member(root, name):
    p = PurePosixPath(name)
    require(bool(name) and not p.is_absolute() and not any(x in ('..', '.') for x in p.parts)
            and '\\' not in name and ':' not in name and p.as_posix() == name, 'Unsafe relative member: ' + name)
    return unlinked(Path(root).joinpath(*p.parts))


def validate_request(q):
    require(set(q) == {'version', 'archiveRoot', 'archiveManifestSha256', 'candidatePartyId', 'runtimeRoot', 'contentRoot'}
            and q['version'] == VERSION and digest(q['archiveManifestSha256']) and digest(q['candidatePartyId']),
            'Use the explicit seed-free reuse request contract.')
    for key in ('archiveRoot', 'runtimeRoot', 'contentRoot'):
        require(isinstance(q[key], str) and Path(q[key]).is_absolute() and unlinked(q[key]).is_dir(),
                'Use an existing absolute ' + key)


def inspect(q):
    validate_request(q)
    archive = Path(q['archiveRoot'])
    inputs = {}

    def pinned(name, expected=None):
        path = member(archive, name)
        before = sha(path)
        require(before == (expected or manifest.get(name)), 'Changed or unpinned evidence: ' + name)
        value = read(path)
        require(sha(path) == before, 'Evidence changed while reading: ' + name)
        inputs[str(path)] = before
        return value

    manifest = pinned('files.json', q['archiveManifestSha256'])
    require(isinstance(manifest, dict) and all(digest(h) for h in manifest.values()), 'Invalid source manifest')
    result = pinned('result.json')
    require(result['version'] == SOURCE_VERSION and result['executionStatus'] == 'Complete'
            and result['integrityStatus'] == 'Verified' and result['strengthDecision'] == 'StrongerFixedCandidatesConfirmed'
            and result['adoption'] == 'RecommendFixedCandidates', 'Source did not confirm a fixed candidate.')
    require(pinned('independent-audit.json') == result, 'Independent result disagrees.')
    audit = pinned('native-audit.json')
    require(audit == dict(status='Passed', studyHash=result['studyHash'], archiveHash=result['archiveHash'], newFights=0),
            'Missing matching native audit.')
    require(manifest['study/files.json'] == result['archiveHash'], 'Changed study archive binding.')
    completion = pinned('completion.json')
    require(completion['status'] == 'Complete' and completion['fights'] == 44000 and completion['retries'] == 0,
            'Incomplete source execution.')
    frozen = pinned('study/freeze.json')
    require(frozen == pinned('freeze.json') and frozen['definition'] == pinned('source-definition.json')
            and frozen['requestHash'] == completion['requestHash'], 'Changed frozen source binding.')
    definition = frozen['definition']
    scope = pinned('study/scope.json')
    require(scope['algorithm'] == definition['version'] == SOURCE_VERSION
            and scope['contentHashes'] == definition['contentHashes'], 'Changed source scope.')
    family = definition['teams']
    require(len(family) == 8 and len({t['partyId'] for t in family}) == 8
            and [t['role'] for t in family] == ['Candidate'] * 6 + ['PrimaryReference', 'OtherReference'],
            'Changed fixed-family roles.')
    controls = [t['partyId'] for t in family[-2:]]
    require(result['controlPartyIds'] == controls and result['candidateIds'] == [t['partyId'] for t in family[:6]]
            and q['candidatePartyId'] in result['qualifyingPartyIds'], 'Explicit selection must be a qualifying candidate.')
    selected = [next(t for t in family if t['partyId'] == q['candidatePartyId']), *family[-2:]]
    exported = pinned('teams.json')
    require(exported['version'] == SOURCE_VERSION and exported['adoption'] == result['adoption']
            and exported['candidateIds'] == result['candidateIds'] and exported['qualifyingPartyIds'] == result['qualifyingPartyIds']
            and len(exported['teams']) == 8, 'Changed team export.')
    teams = []
    for team in selected:
        matches = [t for t in exported['teams'] if t['partyId'] == team['partyId']]
        require(len(matches) == 1, 'Missing or duplicate exported identity.')
        item = matches[0]
        require(all(item[k] == team[k] for k in ('role', 'partyId', 'referenceIds', 'scenario'))
                and item['qualifies'] == (team['partyId'] in result['qualifyingPartyIds'])
                and item['control'] == (team['partyId'] in controls)
                and item['recommended'] == (team['partyId'] in result['recommendedPartyIds']), 'Changed exported role or recipe.')
        scenario = pinned('study/exports/' + team['partyId'] + '.json')
        require(scenario == team['scenario'] and scenario['seeds'] == [], 'Changed or scheduled exported recipe.')
        teams.append(copy.deepcopy(item))
    runtime_files = pinned('study/executable-files.json')
    require(all(runtime_files.get(k + '.dll') == v for k, v in scope['execution']['assemblyHashes'].items()),
            'Producing executable inventory disagrees with scope.')
    target_files = {}
    for name, expected in runtime_files.items():
        path = member(q['runtimeRoot'], name)
        require(sha(path) == expected, 'Target runtime differs: ' + name)
        target_files[str(path)] = expected
    for name, expected in definition['contentHashes'].items():
        path = member(Path(q['contentRoot']) / 'Data', name)
        require(sha(path) == expected, 'Target gameplay content differs: ' + name)
        target_files[str(path)] = expected
    # Effective settings are compared by the producing loader in the native check.
    settings = unlinked(Path(q['contentRoot']) / 'appsettings.json')
    target_files[str(settings)] = sha(settings)
    return dict(teams=teams, scope=scope, executionHash=definition['executionHash'], settingsHash=definition['settingsHash'],
                sourceFiles=inputs, targetFiles=target_files, controlPartyIds=controls, candidatePartyId=q['candidatePartyId'])


def recheck(files):
    for path, expected in files.items():
        require(sha(path) == expected, 'Input changed during reuse preparation: ' + path)


def native_check(output, deadline):
    spec = importlib.util.spec_from_file_location('reuse_owned_process', output / 'bounded_windows_process.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    process = module.run(['pwsh', '-NoProfile', '-File', str(output / 'check-confirmed-team-reuse.ps1'),
                          '-Request', str(output / 'native-request.json'), '-Output', str(output / 'native-check.json')],
                         str(ROOT), output / 'native-check.log', deadline,
                         check=lambda: require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) < MAX_BYTES,
                                               'Reuse preparation storage limit exceeded.'))
    write(output / 'native-process.json', process)
    require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,
            'Native compatibility/preparation failed; inspect the retained log.')
    recheck(read(output / 'framework-files.json'))
    return read(output / 'native-check.json')


def prepare(request, output, checker=native_check):
    start = time.monotonic()
    request = unlinked(request); output = unlinked(output)
    request_hash = sha(request); q = read(request); evidence = inspect(q)
    require(not output.exists() and output.parent.is_dir(), 'Use a new output with an existing parent.')
    for input_root in (q['archiveRoot'], q['runtimeRoot'], q['contentRoot']):
        require(not output.is_relative_to(Path(input_root)) and not Path(input_root).is_relative_to(output),
                'Output overlaps source archive/runtime/content.')
    require(not request.is_relative_to(output), 'Output overlaps source request.')
    output.mkdir()  # Exclusive; a failed directory is retained without a success receipt.
    (output / 'scenarios').mkdir()
    write(output / 'request.json', q)
    write(output / 'scope.json', evidence['scope'])
    native = dict(runtimeRoot=q['runtimeRoot'], contentRoot=q['contentRoot'], executionHash=evidence['executionHash'],
                  settingsHash=evidence['settingsHash'], teams=[])
    for team in evidence['teams']:
        path = output / 'scenarios' / (team['partyId'] + '.json')
        # Preserve the exact native export bytes, including canonical ability order.
        path.write_bytes(member(q['archiveRoot'], 'study/exports/' + team['partyId'] + '.json').read_bytes())
        require(read(path) == team['scenario'], 'Export changed during copy.')
        native['teams'].append(dict(partyId=team['partyId'], scenarioPath=str(path), scenarioFileHash=sha(path)))
    write(output / 'teams.json', dict(version=VERSION, selectedPartyId=q['candidatePartyId'],
                                    controlPartyIds=evidence['controlPartyIds'], teams=evidence['teams']))
    write(output / 'native-request.json', native)
    write(output / 'inputs.json', dict(sourceFiles=evidence['sourceFiles'], targetFiles=evidence['targetFiles']))
    shutil.copyfile(Path(__file__).with_name('check-confirmed-team-reuse.ps1'), output / 'check-confirmed-team-reuse.ps1')
    shutil.copyfile(ROOT / 'build/bounded_windows_process.py', output / 'bounded_windows_process.py')
    shutil.copyfile(Path(__file__), output / 'prepare-confirmed-team-reuse.py')
    check_inputs = {str(p): sha(p) for p in output.rglob('*') if p.is_file()}
    checked = checker(output, start + 180)
    require(checked == dict(status='ContextMatchedRecipesPrepared', executionHash=native['executionHash'],
                           settingsHash=native['settingsHash'], teams=[{k: t[k] for k in ('partyId', 'scenarioFileHash')}
                           for t in native['teams']], nativePreparations=3, fights=0, newValues=0), 'Changed native check result.')
    recheck(check_inputs); recheck(evidence['sourceFiles']); recheck(evidence['targetFiles'])
    require(sha(request) == request_hash, 'Reuse request changed.')
    require(time.monotonic() - start < 180, 'Reuse preparation deadline exceeded.')
    receipt = dict(version=VERSION, status='ReadyForExplicitReuse', sourceRequestSha256=request_hash,
                   sourceArchiveManifestSha256=q['archiveManifestSha256'], selectedPartyId=q['candidatePartyId'],
                   controlPartyIds=evidence['controlPartyIds'], nativePreparations=3, newValues=0, fights=0,
                   searchRequest=False, searchDefaultsChanged=False, allocationAdmissionRequiredForAnyFutureRun=True,
                   context='ExactCapturedExecutionSettingsContent', measuredSeconds=time.monotonic() - start,
                   helperSha256=sha(Path(__file__)))
    write(output / 'reuse.json', receipt)
    files = {p.relative_to(output).as_posix(): sha(p) for p in sorted(output.rglob('*')) if p.is_file()}
    require(sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) < MAX_BYTES - 65536, 'Reuse package too large.')
    write(output / 'files.json', files)
    return dict(status=receipt['status'], output=str(output), manifestSha256=sha(output / 'files.json'),
                selectedPartyId=q['candidatePartyId'], controlPartyIds=evidence['controlPartyIds'], nativePreparations=3, fights=0, newValues=0)


def verify(output, expected):
    output = unlinked(output)
    require(digest(expected) and sha(output / 'files.json') == expected, 'Changed reuse manifest.')
    files = read(output / 'files.json')
    actual = {p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()}
    require(actual == set(files) | {'files.json'}, 'Changed reuse inventory.')
    for name, h in files.items():
        require(sha(member(output, name)) == h, 'Changed reuse artifact: ' + name)
    receipt = read(output / 'reuse.json'); q = read(output / 'request.json')
    require(receipt['version'] == VERSION and receipt['status'] == 'ReadyForExplicitReuse', 'Incomplete handoff.')
    evidence = inspect(q)
    require(read(output / 'teams.json') == dict(version=VERSION, selectedPartyId=q['candidatePartyId'],
            controlPartyIds=evidence['controlPartyIds'], teams=evidence['teams']), 'Changed handoff selection or controls.')
    for team in evidence['teams']:
        require(read(output / 'scenarios' / (team['partyId'] + '.json')) == team['scenario'], 'Changed reusable scenario.')
    inputs = read(output / 'inputs.json'); recheck(inputs['sourceFiles']); recheck(inputs['targetFiles'])
    if (output / 'framework-files.json').exists():
        recheck(read(output / 'framework-files.json'))
    return dict(status='VerifiedReusableInputs', nativePreparations=0, fights=0, newValues=0)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    p = commands.add_parser('prepare'); p.add_argument('request', type=Path); p.add_argument('output', type=Path)
    v = commands.add_parser('verify'); v.add_argument('output', type=Path); v.add_argument('--manifest-sha256', required=True)
    args = parser.parse_args()
    try:
        result = prepare(args.request, args.output) if args.command == 'prepare' else verify(args.output, args.manifest_sha256)
        print(json.dumps(result, indent=2))
    except (ValueError, KeyError, OSError, TypeError) as error:
        print('Reuse preparation rejected: ' + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
