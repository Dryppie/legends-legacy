"""Stage verified competitive finalists and calibration leaders for retention.

Validation uses the C# catalog reader and fresh preview. This command deliberately
does not replace the live catalog; merge its validated bytes under the existing
exclusive catalog lock with the recorded source-hash guard.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess

spec = importlib.util.spec_from_file_location('calibration', Path(__file__).with_name('calibrate-tower-search-portfolio.py'))
cal = importlib.util.module_from_spec(spec); spec.loader.exec_module(cal)
spec2 = importlib.util.spec_from_file_location('expanded', Path(__file__).with_name('tower-calibration-assessment.py'))
expanded = importlib.util.module_from_spec(spec2); spec2.loader.exec_module(expanded)


def canonical_hash(value):
    raw = json.dumps(value, ensure_ascii=True, sort_keys=True, separators=(',', ':'))
    raw = re.sub(r'\\u[0-9a-f]{4}', lambda m: m[0].upper().replace('\\U', '\\u'), raw)
    for char in ('+', '<', '>', '&', "'"): raw = raw.replace(char, '\\u' + format(ord(char), '04X'))
    return hashlib.sha256(raw.encode()).hexdigest()


def stage(out, source, challengers):
    if out.exists(): raise FileExistsError('Choose a new staging directory.')
    p = cal.check(source); report = cal.read(source / 'assessment-parallel.json')
    if report['protocolSha256'] != cal.sha(source / 'protocol.json') or report['selectionSha256'] != cal.sha(source / 'selection.json'):
        raise ValueError('Calibration assessment changed.')
    partitions = []
    for i, digest in report['partitionReportHashes'].items():
        path = source / 'parallel-assessments' / i / 'assessment.json'
        if cal.sha(path) != digest: raise ValueError('Verified partition changed.')
        partitions.append(cal.read(path))
    entries = cal.read(source / 'portfolio.json'); ids = [e['id'] for e in entries]
    expected = expanded.assess(ids, partitions, p) if p.get('campaignAssessmentPolicy') else cal.global_assessment(ids, partitions)
    if any(report.get(k) != v for k, v in expected.items()): raise ValueError('Global assessment differs from its verified evidence.')
    target = cal.ROOT / 'TestResults/balance/retained-tower-builds.json'
    before = cal.read(target); additions = []; sources = {str(source / 'assessment-parallel.json'): cal.sha(source / 'assessment-parallel.json')}
    if challengers:
        completed = cal.read(challengers / 'completion.json')
        if {r['name'] for r in completed} != {'independent', 'retained'}: raise ValueError('Both challengers must reconstruct successfully.')
        for done in completed:
            path = challengers / 'studies' / done['name'] / 'study.json'
            if cal.sha(path) != done['studySha256']: raise ValueError('Challenger study changed.')
            catalog = challengers / ('retention-' + done['name']) / 'retained-tower-builds.json'
            records = cal.read(catalog)['studies']
            if len(records) != 1 or records[0]['evidenceHash'] != done['studySha256']:
                raise ValueError('Unexpected challenger retention source.')
            additions += records; sources[str(path)] = cal.sha(path); sources[str(catalog)] = cal.sha(catalog)
    top = sorted(report['cells'], key=lambda c: (-c['wins'], c['id']))[:10]; builds = []
    for row in top:
        scenario = copy.deepcopy(next(e['scenario'] for e in entries if e['id'] == row['id']))
        if scenario['seeds']: raise ValueError('Retained recipes must not carry old schedules.')
        builds.append({'id': row['id'], 'recipeHash': canonical_hash(scenario), 'scenario': scenario})
    digest = cal.sha(source / 'assessment-parallel.json')
    additions.append({'id': 'competitive-calibration-' + digest[:32], 'evidenceHash': digest, 'budget': p['budget'],
        'requiredPartySize': p['cohort']['requiredPartySize'],
        'combatSeeds': sorted(set(cal.integers(cal.read(source / 'seed-ledger.json')['schedules']))), 'builds': builds})
    if {s['id'] for s in before['studies']}.intersection(s['id'] for s in additions): raise ValueError('These sources are already retained.')
    merged = {**before, 'studies': before['studies'] + additions}; out.mkdir(parents=True)
    cal.save(out / 'before.json', before); cal.save(out / 'retained-tower-builds.json', merged)
    cal.save(out / 'import-plan.json', {'status': 'Staged', 'sourceCatalogSha256': cal.sha(target), 'candidateSha256': cal.sha(out / 'retained-tower-builds.json'),
        'sourceHashes': sources, 'addedStudies': [s['id'] for s in additions], 'addedBuildRecords': sum(len(s['builds']) for s in additions),
        'scriptSha256': cal.sha(Path(__file__)), 'calibrationAssessment': report['assessment'],
        'scope': 'Retain exact completed discoveries even when balance fails; future studies must measure them afresh. Preserve all prior controls.'})
    with (out / 'validation.log').open('x', encoding='utf-8') as stream:
        subprocess.run(['dotnet', str(cal.ROOT / 'LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll'),
            'tower-team-plan', '--floor', str(p['floor']), '--slots', str(p['budget']['essenceSlots']), '--seed', '918120',
            '--output', str(out / 'validation'), '--runs-root', str(out), '--catalogs-root', str(cal.ROOT / 'LL/tools/BalanceHarness/Fixtures'),
            '--content-root', str(cal.ROOT / 'LL/src/API/API.LL')], cwd=cal.ROOT, stdout=stream, stderr=subprocess.STDOUT, check=True)
    preview = cal.read(out / 'validation/definition.json')
    cal.save(out / 'validated.json', {'status': 'ValidatedByCSharp', 'candidateSha256': cal.sha(out / 'retained-tower-builds.json'),
        'previewSha256': cal.sha(out / 'validation/definition.json'), 'compatibleControls': len(preview['references'])})
    print(json.dumps({'addedRecords': sum(len(s['builds']) for s in additions), 'compatibleControls': len(preview['references'])}), flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--calibration', type=Path, required=True); parser.add_argument('--challengers', type=Path)
    args = parser.parse_args(); stage(args.out.resolve(), args.calibration.resolve(), args.challengers.resolve() if args.challengers else None)
