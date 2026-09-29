"""Verify an already applied local Tower setting against a passing confirmation.

This command never edits game content and never allocates new seeds.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import shutil
import sys
import time

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    sys.modules[name] = value
    spec.loader.exec_module(value)
    return value


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('source', 'audit', 'artifacts', 'owner'):
        p.add_argument('--'+name, type=Path, required=True)
    a = p.parse_args()
    source, audit, artifacts, owner = [v.resolve() for v in (a.source, a.audit, a.artifacts, a.owner)]
    io = module('tower_balance_application_io', Path(__file__).with_name('run-tower-balance-pass.py'))
    io.check(owner.parent == ROOT/'TestResults' and not owner.exists(), 'Fresh direct TestResults child required')
    io.authenticate(source)
    evidence = io.read(audit)
    io.check(evidence['status'] == 'Verified' and evidence['assessment']['verdict'] == 'Pass', 'Passing fresh confirmation required')
    io.check(evidence['resultSha256'] == io.sha(source/'result.json'), 'Audit does not bind this result')
    owner.mkdir()
    shutil.copy2(Path(__file__), owner/'owner-source.py')
    floor = ROOT/'LL/src/API/API.LL/Data/world-tower/tower-floors.json'
    floor_pin = io.sha(floor)
    q = dict(source=str(source), manifestPin=io.sha(source/'files.json'), audit=str(audit), auditPin=io.sha(audit),
             output=str(owner/'result.json'))
    io.write(owner/'request.json', q)
    os.environ['LL_TOWER_BALANCE_APPLICATION'] = str(owner/'request.json')
    process = module('tower_balance_application_process', ROOT/'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT/'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests']
    receipt = process.run(command, ROOT, owner/'execution.log', time.monotonic()+660, log_byte_limit=1048576)
    io.write(owner/'process.json', receipt)
    io.check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Application check failed')
    result = io.read(owner/'result.json')
    io.check(result['status'] == 'AppliedInputsAndReplaysVerified' and result['matchedInputs'] == evidence['evaluationFights']
             and result['fullReplays'] == evidence['assessment']['familySize'] and io.sha(floor) == floor_pin, 'Incomplete application parity')
    io.write(owner/'completion.json', dict(status='Verified', floorFileSha256=floor_pin, resultSha256=io.sha(owner/'result.json'),
                                          matchedInputs=result['matchedInputs'], fullReplays=result['fullReplays'], newSeeds=0))
    print(f"Verified {result['matchedInputs']} native inputs and {result['fullReplays']} complete replays.")


if __name__ == '__main__':
    main()
