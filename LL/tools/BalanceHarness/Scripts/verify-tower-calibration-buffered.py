"""Resume full normal-archive verification using a separately proven buffered reader.

Combat archives, producing executables and frozen definitions stay untouched.
The handoff receipt must identify the stopped read-only verifier processes.
"""
import argparse
import concurrent.futures
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import time

spec = importlib.util.spec_from_file_location('calibration', Path(__file__).with_name('calibrate-tower-search-portfolio.py'))
cal = importlib.util.module_from_spec(spec); spec.loader.exec_module(cal)
spec2 = importlib.util.spec_from_file_location('expanded', Path(__file__).with_name('tower-calibration-assessment.py'))
expanded = importlib.util.module_from_spec(spec2); spec2.loader.exec_module(expanded)


def verify(out, reader):
    if (out / 'buffered-verification-plan.json').exists() or (out / 'producing-buffered-verifier.py').exists():
        raise FileExistsError('A buffered verification plan already exists; preserve its outputs.')
    protocol = cal.check(out); selection = cal.read(out / 'selection.json')
    if selection['status'] != 'FrozenBeforeConfirmation': raise ValueError('Confirmation is not frozen.')
    entries = cal.read(out / 'portfolio.json'); ids = [e['id'] for e in entries]
    if [i for p in selection['definitions'] for i in p['ids']] != ids: raise ValueError('Partition coverage changed.')
    for p in selection['definitions']:
        if cal.sha(out / p['file']) != p['sha256']: raise ValueError('Frozen definition changed.')
    if protocol['assessmentScriptSha256'] != cal.sha(Path(expanded.__file__)):
        raise ValueError('Campaign assessor changed.')
    handoff = cal.read(out / 'buffered-verification-handoff.json')
    if handoff['status'] != 'OriginalReadOnlyVerifiersStopped': raise ValueError('Original verifiers must stop before handoff.')
    proof = cal.read(reader / 'result.json'); identity = cal.read(reader / 'plan.json')
    if proof['status'] != 'ExactEvidenceAndAssessmentMatch' or proof['historicalCombatsVerified'] != 1250:
        raise ValueError('Buffered reader equivalence has not been established.')
    for row in proof['cases']:
        for field, name in [('evidenceSha256', 'evidence.json'), ('assessmentSha256', 'assessment.json')]:
            if cal.sha(reader / row['case'] / name) != row[field]: raise ValueError('Reader equivalence evidence changed.')
    for name, digest in identity['assemblies'].items():
        if cal.sha(reader / 'executable' / name) != digest: raise ValueError('Verified reader changed.')
    # Keep the prior plan and its producing script; this records the implementation handoff separately.
    shutil.copy2(Path(__file__), out / 'producing-buffered-verifier.py')
    plan = {'version': 'buffered-calibration-verification-v1', 'status': 'FrozenBeforeVerification',
        'scriptSha256': cal.sha(Path(__file__)), 'protocolSha256': cal.sha(out / 'protocol.json'),
        'selectionSha256': cal.sha(out / 'selection.json'), 'familySize': len(ids), 'parallelPartitions': 4,
        'handoffSha256': cal.sha(out / 'buffered-verification-handoff.json'),
        'readerProofSha256': cal.sha(reader / 'result.json'), 'readerIdentitySha256': cal.sha(reader / 'plan.json'),
        'sourceComparisonSha256': cal.sha(reader / 'source-checksum-comparison.json'),
        'readerDirectory': str(reader),
        'scope': 'Same complete normal C# validation and global-family arithmetic; only buffered file reads and assembly metadata differ in the verification path. No combat, resampling, new recipes, or reduced checks.'}
    cal.save(out / 'buffered-verification-plan.json', plan)

    def ready(p):
        for cid in p['ids']:
            folder = out / 'runs' / ('confirmation-' + cid)
            if (folder / 'failure.json').exists(): raise ValueError('Required combat archive failed: ' + cid)
            path = folder / 'tower-results.json'
            if not path.exists(): return False
            try:
                if len(cal.read(path)) != protocol['confirmationSamples']: raise ValueError('Incomplete required schedule.')
            except (OSError, json.JSONDecodeError): return False
        return True

    def work(item):
        i, p = item; target = out / 'parallel-assessments' / str(i)
        if target.exists(): raise FileExistsError('Preserve and explicitly review any earlier completed partition before restarting.')
        while not ready(p): time.sleep(5)
        sources = out / f'buffered-sources-{i}.json'
        cal.save(sources, [{'cellId': cid, 'runDirectory': f'runs/confirmation-{cid}'} for cid in p['ids']])
        print(f'Partition {i}: verifying all {len(p["ids"])} complete archives with buffered reads', flush=True)
        with (out / 'logs' / f'buffered-assessment-{i}.log').open('x', encoding='utf-8') as log:
            done = subprocess.run(['dotnet', str(reader / 'executable/BalanceHarness.dll'), 'tower-balance-evaluate',
                '--definition', str(out / p['file']), '--sources', str(sources), '--output', str(target)],
                cwd=cal.ROOT, stdout=log, stderr=subprocess.STDOUT)
        if done.returncode not in (0, 1, 3): raise RuntimeError('Normal archive verifier failed.')
        report = cal.read(target / 'assessment.json')
        print(f'Partition {i}: verified {len(report["cells"])} cells', flush=True)
        return report

    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        reports = list(pool.map(work, enumerate(selection['definitions'])))
    cal.check(out)
    if cal.sha(out / 'selection.json') != plan['selectionSha256']: raise ValueError('Selection changed during verification.')
    result = expanded.assess(ids, reports, protocol)
    result.update(protocolSha256=plan['protocolSha256'], selectionSha256=plan['selectionSha256'],
        verificationPlanSha256=cal.sha(out / 'buffered-verification-plan.json'),
        partitionReportHashes={str(i): cal.sha(out / 'parallel-assessments' / str(i) / 'assessment.json') for i in range(len(reports))},
        actualDiscoveryCombats=sum(len(o['rows']) * o['rows'][0]['valid'] for file in ['coarse.json', 'fine.json'] for o in cal.read(out / file)),
        actualConfirmationCombats=len(entries) * protocol['confirmationSamples'])
    cal.save(out / 'assessment-parallel.json', result)
    best = max(result['cells'], key=lambda r: (r['wins'], r['id']))
    print(f'Global {result["assessment"]}: strongest {best["wins"]}/{best["valid"]}, adjusted [{best["lower"]:.2%}, {best["upper"]:.2%}]', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=Path, required=True); parser.add_argument('--reader-proof', type=Path, required=True)
    args = parser.parse_args(); verify(args.out.resolve(), args.reader_proof.resolve())
