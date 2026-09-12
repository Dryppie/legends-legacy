"""Verify completed calibration partitions concurrently, without running combat.

This can overlap a still-running confirmation batch. All membership, hashes and
statistical rules come from that experiment's already frozen protocol. The output
is separate from the sequential driver's report; no original evidence is edited.
"""
import argparse
import concurrent.futures
import importlib.util
import json
from pathlib import Path
import shutil
import time

spec = importlib.util.spec_from_file_location('calibration', Path(__file__).with_name('calibrate-tower-search-portfolio.py'))
cal = importlib.util.module_from_spec(spec); spec.loader.exec_module(cal)
spec2 = importlib.util.spec_from_file_location('expanded', Path(__file__).with_name('tower-calibration-assessment.py'))
expanded = importlib.util.module_from_spec(spec2); spec2.loader.exec_module(expanded)


def verify(out):
    if (out / 'parallel-verification-plan.json').exists() or (out / 'producing-parallel-verifier.py').exists():
        raise FileExistsError('A verification plan already exists; preserve its captured driver and outputs.')
    protocol = cal.check(out); selection = cal.read(out / 'selection.json')
    if selection['status'] != 'FrozenBeforeConfirmation': raise RuntimeError('No frozen confirmation.')
    entries = cal.read(out / 'portfolio.json')
    expected_ids = [e['id'] for e in entries]
    if [i for p in selection['definitions'] for i in p['ids']] != expected_ids:
        raise RuntimeError('Partition coverage differs from the frozen portfolio.')
    for p in selection['definitions']:
        if cal.sha(out / p['file']) != p['sha256']: raise RuntimeError('Frozen partition changed.')
    if protocol.get('campaignAssessmentPolicy'):
        if protocol['assessmentScriptSha256'] != cal.sha(Path(expanded.__file__)):
            raise RuntimeError('Versioned campaign assessor changed.')
    shutil.copy2(Path(__file__), out / 'producing-parallel-verifier.py')
    cal.save(out / 'parallel-verification-plan.json', {
        'version': 'parallel-calibration-verification-v2', 'status': 'FrozenBeforeVerification',
        'scriptSha256': cal.sha(Path(__file__)), 'assessorScriptSha256': cal.sha(Path(cal.__file__)),
        'protocolSha256': cal.sha(out / 'protocol.json'), 'selectionSha256': cal.sha(out / 'selection.json'),
        'parallelPartitions': 5, 'familySize': len(entries),
        'scope': 'Read-only normal-archive verification. Wait for every required archive in a partition, then invoke the original C# adapter and apply the unchanged global-family assessor. No combat, resampling, new members or changed acceptance rules.'})

    def ready(partition):
        for cid in partition['ids']:
            folder = out / 'runs' / ('confirmation-' + cid)
            results = folder / 'tower-results.json'
            if (folder / 'failure.json').exists(): raise RuntimeError('Required archive failed: ' + cid)
            if not results.exists(): return False
            try:
                # This manifest is written after the scorecard and all battles.
                if len(cal.read(results)) != protocol['confirmationSamples']:
                    raise RuntimeError('Required archive has an incomplete schedule: ' + cid)
            except (OSError, json.JSONDecodeError): return False
        return True

    def partition_work(item):
        i, p = item
        while not ready(p): time.sleep(5)
        sources = [{'cellId': cid, 'runDirectory': f'runs/confirmation-{cid}'} for cid in p['ids']]
        path = out / f'parallel-sources-{i}.json'; cal.save(path, sources)
        print(f'Partition {i}: all {len(sources)} archives complete; verifying without combat', flush=True)
        cal.command(out, ['tower-balance-evaluate', '--definition', out / p['file'], '--sources', path,
                         '--output', out / 'parallel-assessments' / str(i)], f'parallel-assessment-{i}', (0, 1, 3))
        report = cal.read(out / 'parallel-assessments' / str(i) / 'assessment.json')
        print(f'Partition {i}: verified {len(report["cells"])} cells', flush=True)
        return report

    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:
        verified = list(pool.map(partition_work, enumerate(selection['definitions'])))
    cal.check(out)
    if cal.sha(out / 'selection.json') != cal.read(out / 'parallel-verification-plan.json')['selectionSha256']:
        raise RuntimeError('Frozen selection changed during verification.')
    report = expanded.assess(expected_ids, verified, protocol) if protocol.get('campaignAssessmentPolicy') else cal.global_assessment(expected_ids, verified)
    report.update(protocolSha256=cal.sha(out / 'protocol.json'), selectionSha256=cal.sha(out / 'selection.json'),
        verificationPlanSha256=cal.sha(out / 'parallel-verification-plan.json'),
        partitionReportHashes={str(i): cal.sha(out / 'parallel-assessments' / str(i) / 'assessment.json') for i in range(len(verified))},
        actualDiscoveryCombats=sum(len(o['rows']) * o['rows'][0]['valid'] for file in ['coarse.json', 'fine.json'] for o in cal.read(out / file)),
        actualConfirmationCombats=len(entries)*protocol['confirmationSamples'])
    cal.save(out / 'assessment-parallel.json', report)
    best = max(report['cells'], key=lambda r: (r['wins'], r['id']))
    print(f'Global {report["assessment"]}: {report["familySize"]} parties; strongest {best["wins"]}/{best["valid"]}, adjusted [{best["lower"]:.2%}, {best["upper"]:.2%}]', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args(); verify(args.out.resolve())
