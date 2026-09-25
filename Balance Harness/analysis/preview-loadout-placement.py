"""Bounded native generation/reconstruction on twelve already exposed roots.

No campaign, combat preparation, allocator, live content, or new randomness.
Every native invocation is a generation-only export or its reconstruction.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
SECONDS, BYTES = 600, 512 * 1048576
OLD = 'TestResults/affinity-allied-action-preview-20260924-v2'
OLD_PIN = '8301070ad78ab8ea84875faf8092c7ae5da193f1d0106245e417b70f689231b1'
DESIGN = 'TestResults/loadout-placement-design-20260924'
DESIGN_PIN = '5444ed6a86ed099cc190c49df7b7304f5e976922c111f09e4a739b746ee2c229'
PRIOR = 'TestResults/loadout-placement-design-handoff-20260924.json'
PRIOR_PIN = 'dddb780bd9efeccbefd0abde59bd17dfa9d1831391ad2c2e473b3f9e6642c073'
FAILED_ATTEMPTS = {
    'TestResults/loadout-placement-native-preview-20260924':
        '87dcd5174d5820052b8919c6564990e87232ffe77879823fa5ad37796fbe971d',
    'TestResults/loadout-placement-native-preview-20260924-v2':
        '42b642f2f85209b21b3f507d3ea8904da6e68d947f28cd06daa852623391c31c',
    'TestResults/loadout-placement-native-preview-20260924-v3':
        '3c1ec27ea8772f79981f15dff912bd28e1bb8921c2f16ab9832c78b1faa98718'}
ENGINEERING = 'TestResults/loadout-placement-native-implementation-20260924'
POLICY = dict(version='tower-proposal-policy-v6', name='benchmark-subgroup-loadout-placement-v6',
    firstWave=['subgroup-loadout-placement'] * 9, secondWave=['subgroup-loadout-placement'] * 8,
    parentTickets=['benchmark'], preserveParentInteractions=False)


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True,
                                     allow_nan=False).encode()).hexdigest()


def native_digest(value):
    # Match HarnessJson for this captured ASCII fixture, as in the prior
    # neighborhood auditor. The historical Python design hash stays unchanged.
    text = json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False, allow_nan=False)
    require(text.isascii(), 'Native hash bridge only supports the captured ASCII fixture')
    for char in ['+', '<', '>', '&', "'"]:
        text = text.replace(char, '\\u%04X' % ord(char))
    return hashlib.sha256(text.encode()).hexdigest()


def scenario_for(anchor, builds):
    scenario = copy.deepcopy(anchor)
    for actor in scenario['party']:
        actor['build']['essenceIds'] = copy.deepcopy(builds[str(actor['partySlot'])])
    return scenario


def check_scope(catalogue, scope):
    require(catalogue['scopeHash'] == native_digest(scope), 'Changed native scope binding')


def check_preview_roots(rows):
    require(len(rows) == len({r['rootSeed'] for r in rows}) == 12
            and len({r['recipeHash'] for r in rows}) == 1, 'Changed roots or root-dependent recipes')


def read(path):
    return json.loads(path.read_bytes())


def check_legacy_arm_bytes(old_raw, new_raw):
    def arms(raw):
        text = raw.decode('utf-8')
        match = re.search(r'"arms"\s*:\s*\[', text)
        require(match is not None, 'Missing serialized arms')
        offset, result, decoder = match.end(), {}, json.JSONDecoder()
        while True:
            while text[offset].isspace() or text[offset] == ',':
                offset += 1
            if text[offset] == ']':
                return result
            value, end = decoder.raw_decode(text, offset)
            require(value['name'] not in result, 'Duplicate serialized arm')
            result[value['name']] = text[offset:end].encode('utf-8')
            offset = end
    old, new = arms(old_raw), arms(new_raw)
    require(all(new.get(name) == value for name, value in old.items()), 'Legacy serialized arm bytes changed')


def preview_request(previous, anchor):
    request = copy.deepcopy(previous)
    request.update(version='tower-proposal-export-v6', policies=request['policies'] + [copy.deepcopy(POLICY)])
    context = request['context']
    reference = next(r for r in context['scope']['references'] if r['id'] == context['benchmarkReferenceId'])
    require(dict(reference['scenario'], id=anchor['id']) == anchor and anchor['seeds'] == [],
            'Design anchor differs beyond the diagnostic scenario label')
    # The design used the later pilot envelope. This changes no actor or gameplay field.
    reference['scenario']['id'] = anchor['id']
    return request


def check_catalogue(catalogue, expected, anchor):
    require(catalogue['version'] == 'tower-subgroup-loadout-placement-v1'
            and catalogue['assignmentsExamined'] == 240 and catalogue['identityAssignments'] == 2
            and catalogue['referenceAssignments'] == catalogue['duplicateAssignments'] == 0,
            'Changed native catalogue accounting')
    rows = catalogue['recipes']
    require(len(rows) == len(expected) == 238 and [r['party']['id'] for r in rows] == sorted(expected),
            'Incomplete, duplicate or unordered native catalogue')
    bindings = []
    for row in rows:
        pid = row['party']['id']
        original = expected[pid]
        require(row['party']['builds'] == original['builds'] and digest(row['party']['builds']) == pid,
                'Changed recipe identity or owner builds')
        require(row['party']['source'] == POLICY['version'] and row['subgroup'] == original['subgroup']
                and row['changedOwners'] == original['changedOwners']
                and row['replacementDistance'] == original['replacedAssignments'],
                'Changed physical hash, subgroup or edit description')
        scenario = scenario_for(anchor, original['builds'])
        require(digest(scenario) == original['seedFreeScenarioHash']
                and native_digest(scenario) == row['seedFreeScenarioHash'], 'Changed physical scenario hash')
        bindings.append(dict(partyId=pid, pythonDesignSha256=original['seedFreeScenarioHash'],
                             nativeHarnessSha256=row['seedFreeScenarioHash']))
        assignments = [{'sourceByDestination': {k: int(v) for k, v in a.items()}} for a in original['assignments']]
        require(row['assignments'] == assignments, 'Changed complete assignment derivations')
    return bindings


def check_export(result, previous, expected, anchor):
    require(result['version'] == 'tower-proposal-export-v6' and result['status'] == 'Complete'
            and result['newFights'] == result['newReservedValues'] == 0, 'Invalid generation-only export')
    for arm in previous['arms']:
        require(next(a for a in result['arms'] if a['name'] == arm['name']) == arm,
                'An existing complete policy arm changed')
    arm = next(a for a in result['arms'] if a['name'] == POLICY['name'])
    catalogue = arm['loadoutPlacementCatalogue']
    check_catalogue(catalogue, expected, anchor)
    require(catalogue['parentId'] == digest({str(p['partySlot']): p['build']['essenceIds'] for p in anchor['party']}),
            'Changed benchmark parent')
    ids = []
    for number, batch in enumerate([arm['batch'], arm['secondWave']['batch']], 1):
        count = 9 if number == 1 else 8
        require(batch['wave'] == number and batch['afterEvaluations'] == 0
                and not batch['feedbackPanels'] and not batch['beamIds']
                and len(batch['candidates']) == len(batch['proposals']) == count, 'Changed wave contract')
        require([p['party']['id'] for p in batch['proposals']] == [p['id'] for p in batch['candidates']],
                'Proposals disagree with sampled candidates')
        for proposal in batch['proposals']:
            pid = proposal['party']['id']
            row = next(r for r in catalogue['recipes'] if r['party']['id'] == pid)
            step = proposal['loadoutPlacement']
            require(proposal['rejection'] is None and proposal['fallback'] is None
                    and proposal['parents'] == [catalogue['parentId']]
                    and proposal['party'] == row['party']
                    and step == dict(catalogueHash=native_digest(catalogue), drawOrdinal=len(ids) + 1,
                                     subgroup=row['subgroup'], assignment=row['assignments'][0]),
                    'Changed draw order or assignment provenance')
            ids.append(pid)
    require(len(set(ids)) == 17 and set(ids) <= expected.keys(), 'Repeated or unknown sampled recipe')
    require(len(arm['teams']) == 20 and sum(t['role'] == 'reference' for t in arm['teams']) == 3,
            'Changed exported reference/candidate membership')
    for team in arm['teams']:
        require(team['scenario']['seeds'] == [], 'Exported combat seeds')
        if team['role'] == 'candidate':
            require(team['partyId'] in ids, 'Unselected exported team')
            scenario = scenario_for(anchor, expected[team['partyId']]['builds'])
            require(team['scenario'] == scenario, 'Moved equipment, identity, styles or scenario fields')
    return ids, native_digest(catalogue)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--harness', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    out, harness = args.output.resolve(), args.harness.resolve()
    require(not out.exists() and out.is_relative_to(ROOT / 'TestResults') and harness.is_file(), 'Unsafe or existing output')
    # Engineering tests are a prerequisite, not another scientific run.
    test_pins, tests = {}, {}
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    for name in ('tests-final.trx',):
        path = ROOT / ENGINEERING / name
        counters = ET.parse(path).find('t:ResultSummary/t:Counters', ns).attrib
        require(int(counters['failed']) == 0 and int(counters['passed']) == int(counters['total']) > 0,
                'Required backend suite did not pass')
        test_pins[name], tests[name] = sha(path), int(counters['passed'])
    require(tests['tests-final.trx'] == 350, 'Missing final placement and scoped regression evidence')
    out.mkdir()
    started = time.monotonic()

    def save(name, value, compact=False):
        with (out / name).open('x', encoding='utf-8', newline='\n') as stream:
            json.dump(value, stream, indent=None if compact else 2, allow_nan=False)
            stream.write('\n')

    sources = sorted((ROOT / 'LL/tools/BalanceHarness').glob('*.cs')) + [
        ROOT / 'LL/tests/EssenceSystem.Tests/BalanceHarnessLoadoutPlacementTests.cs', Path(__file__).resolve()]
    save('declaration.json', dict(version='tower-loadout-placement-native-preview-v4',
        maximumSeconds=SECONDS, maximumBytes=BYTES, charge='FullAllowanceAtStartIncludingFailure',
        sourcePreviewManifestSha256=OLD_PIN, designManifestSha256=DESIGN_PIN, priorHandoffSha256=PRIOR_PIN,
        failedAttemptManifestPins=FAILED_ATTEMPTS, failedAttemptChargedSeconds=len(FAILED_ATTEMPTS) * SECONDS,
        failedAttemptChargedBytes=len(FAILED_ATTEMPTS) * BYTES, technicalAttempt=len(FAILED_ATTEMPTS) + 1,
        physicalHashBridge='SameExactScenario;SortedCompactASCIIJSON;NativeEscapesPlusAndHTMLCharacters',
        requestNormalization='OnlyBenchmarkReferenceScenarioIdAlignedToFrozenDesign;AllOtherFieldsMustAlreadyMatch',
        roots=12, rootSource='PreviouslyExposedDevelopmentRootsOnly', newFights=0, newValues=0, newEntropyDraws=0,
        tests=tests, testPins=test_pins, sourcePins={p.relative_to(ROOT).as_posix(): sha(p) for p in sources},
        inputHarnessSha256=sha(harness), sourceRuntimePins={p.relative_to(harness.parent).as_posix(): sha(p)
            for p in harness.parent.rglob('*') if p.is_file() and (p.suffix == '.dll' or p.name in
                ('BalanceHarness.deps.json', 'BalanceHarness.runtimeconfig.json'))}))
    consumed = {}

    def member(package, name, pin):
        path = ROOT / package / name
        require(sha(path) == pin, 'Changed source: ' + str(path))
        consumed[f'{package}/{name}'] = pin
        return read(path)

    def limits():
        require(time.monotonic() - started < SECONDS, 'Preview time limit exhausted')
        require(sum(p.stat().st_size for p in out.rglob('*') if p.is_file()) < BYTES, 'Preview storage limit exhausted')

    commands = []

    def native(*args):
        limits()
        index = len(commands) + 1
        command = ['dotnet', str(out / 'runtime/BalanceHarness.dll'), *map(str, args)]
        save(f'command-{index:02d}-intent.json', dict(command=command, attempt=1, retries=0))
        before = time.monotonic()
        with (out / f'command-{index:02d}.log').open('xb') as log:
            try:
                process = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT,
                    timeout=max(0.01, SECONDS - (before - started)), creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
                receipt = dict(index=index, command=command, seconds=time.monotonic() - before, exitCode=process.returncode, timedOut=False)
            except subprocess.TimeoutExpired:
                receipt = dict(index=index, command=command, seconds=time.monotonic() - before, exitCode=None, timedOut=True)
                save(f'command-{index:02d}-result.json', receipt)
                raise
        save(f'command-{index:02d}-result.json', receipt)
        commands.append(receipt)
        require(process.returncode == 0, 'Native generation/verification command failed')
        limits()

    try:
        require(sha(ROOT / PRIOR) == PRIOR_PIN, 'Changed prior handoff')
        prior = read(ROOT / PRIOR)
        for name, pin in prior['preservedHistoricalPins'].items():
            require(sha(ROOT / name) == pin, 'Changed inherited pin: ' + name)
        old = member(OLD, 'files.json', OLD_PIN)
        design = member(DESIGN, 'files.json', DESIGN_PIN)
        for package, manifest_pin in FAILED_ATTEMPTS.items():
            failed = member(package, 'files.json', manifest_pin)
            require({p.relative_to(ROOT / package).as_posix() for p in (ROOT / package).rglob('*') if p.is_file()}
                    == set(failed) | {'files.json'}, 'Changed retained failed preview inventory')
            for name, pin in failed.items():
                require(sha(ROOT / package / name) == pin, 'Changed retained failed preview member')
            failure = member(package, 'failure.json', failed['failure.json'])
            expected_commands = 25 if package.endswith('-v3') else 1
            require(failure['chargedSeconds'] == SECONDS and failure['chargedBytes'] == BYTES
                    and failure['completedCommands'] == expected_commands and failure['fullAllowanceCharged'],
                    'Changed failed preview charge')
        expected = {r['partyId']: r for r in member(DESIGN, 'catalogue.json', design['catalogue.json'])}
        anchor = member(DESIGN, 'fixture.json', design['fixture.json'])['anchor']
        declaration = read(out / 'declaration.json')
        for name, pin in declaration['sourceRuntimePins'].items():
            source, dest = harness.parent / name, out / 'runtime' / name
            require(sha(source) == pin, 'Build runtime changed')
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, dest)
        for source in sources:
            target = out / 'source' / source.relative_to(ROOT)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
        rows = []
        for root in range(1, 13):
            name = f'root-{root:02d}'
            previous_request = member(OLD, name + '/request.json', old[name + '/request.json'])
            previous = member(OLD, name + '/batches.json', old[name + '/batches.json'])
            request = preview_request(previous_request, anchor)
            input_name = f'input-{root:02d}.json'
            save(input_name, request, compact=True)
            native('tower-proposal-policy-export', out / input_name, out / name)
            check_legacy_arm_bytes((ROOT / OLD / name / 'batches.json').read_bytes(),
                                   (out / name / 'batches.json').read_bytes())
            result = read(out / name / 'batches.json')
            ids, catalogue_hash = check_export(result, previous, expected, anchor)
            catalogue = next(a for a in result['arms'] if a['name'] == POLICY['name'])['loadoutPlacementCatalogue']
            check_scope(catalogue, request['context']['scope'])
            if root == 1:
                save('physical-hash-bindings.json', dict(
                    interpretation='Both hashes bind the same exact scenario under their respective JSON encodings',
                    pythonEncoding='SortedCompactJSONEnsureASCII', nativeEncoding='SortedCompactASCIIJSONWithNativeHTMLEscapes',
                    bindings=check_catalogue(catalogue, expected, anchor)))
            pin = sha(out / name / 'files.json')
            native('tower-proposal-policy-verify', out / name, pin)
            rows.append(dict(root=root, rootSeed=request['context']['rootSeed'], accepted=17,
                             sampledPartyIds=ids, catalogueHash=catalogue_hash, scopeHash=catalogue['scopeHash'],
                             recipeHash=native_digest(catalogue['recipes']), manifestSha256=pin))
            print(json.dumps(dict(root=root, status='NativeAndIndependentPreviewPassed')), flush=True)
        native('tower-proposal-policy-verify', ROOT / OLD / 'root-01', old['root-01/files.json'])
        for name, pin in {**consumed, **declaration['sourcePins'], **prior['preservedHistoricalPins']}.items():
            require(sha(ROOT / name) == pin, 'Source changed during preview: ' + name)
        for name, pin in declaration['sourceRuntimePins'].items():
            require(sha(out / 'runtime' / name) == pin, 'Retained runtime changed')
        check_preview_roots(rows)
        save('summary.json', dict(status='VerifiedNativeLoadoutPlacementPreview', completeRoots=12, catalogueRecipes=238,
            oldCompleteArmsUnchanged=24, oldSerializedArmBytesUnchanged=24, oldFullExportReconstructed=True, accepted=204,
            distinctSampledRecipes=len({pid for row in rows for pid in row['sampledPartyIds']}), roots=rows,
            newFights=0, newValues=0, newEntropyDraws=0, chargedSeconds=SECONDS, chargedBytes=BYTES,
            failedAttemptManifestPins=FAILED_ATTEMPTS, failedAttemptChargedSeconds=len(FAILED_ATTEMPTS) * SECONDS,
            failedAttemptChargedBytes=len(FAILED_ATTEMPTS) * BYTES,
            technicalAttempts=len(FAILED_ATTEMPTS) + 1, retainedFailedAttempts=len(FAILED_ATTEMPTS),
            physicalScenariosMatched=238, physicalHashEncodingsExplicitlyBridged=True,
            cumulativeRecordedSeconds=prior['cumulativeRecordedSeconds'] + (len(FAILED_ATTEMPTS) + 1) * SECONDS,
            cumulativeRecordedBytes=prior['cumulativeRecordedBytes'] + (len(FAILED_ATTEMPTS) + 1) * BYTES,
            cumulativeDeclaredMaximumSeconds=prior['cumulativeDeclaredMaximumSeconds'] + (len(FAILED_ATTEMPTS) + 1) * SECONDS,
            cumulativeDeclaredMaximumBytes=prior['cumulativeDeclaredMaximumBytes'] + (len(FAILED_ATTEMPTS) + 1) * BYTES,
            history=prior['history'], liveHistoryRescanned=False, consumed=consumed,
            measuredSecondsBeforeSealing=time.monotonic() - started, tests=tests))
        save('commands.json', commands)
        limits()
        save('files.json', {p.relative_to(out).as_posix(): sha(p) for p in sorted(out.rglob('*')) if p.is_file()})
        limits()
        print(json.dumps(dict(status='VerifiedNativeLoadoutPlacementPreview', manifestSha256=sha(out / 'files.json'),
            measuredSeconds=time.monotonic() - started, retainedBytes=sum(p.stat().st_size for p in out.rglob('*') if p.is_file()))))
    except BaseException as error:
        save('failure.json', dict(reason=str(error), fullAllowanceCharged=True, chargedSeconds=SECONDS,
            chargedBytes=BYTES, measuredSeconds=time.monotonic() - started, completedCommands=len(commands)))
        save('files.json', {p.relative_to(out).as_posix(): sha(p) for p in sorted(out.rglob('*')) if p.is_file()})
        raise


if __name__ == '__main__':
    main()
