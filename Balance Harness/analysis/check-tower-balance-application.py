"""Verify local or isolated Tower content against a passing confirmation.

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


def validate_single_candidate_confirmation(request, evidence):
    if (request.get('mode') != 'confirm' or request.get('searchSeeds') != [] or
            evidence.get('status') != 'Verified' or evidence.get('assessment', {}).get('verdict') != 'Pass'):
        raise ValueError('An isolated single candidate requires a verified passing confirmation')


def validate_single_candidate_equipment(cells, evidence):
    """Apply the limited-equipment contract in addition to the generic audit."""
    assessment = evidence['assessment']
    bounds = assessment['bounds']
    ids = [cell['id'] for cell in cells]
    if (not ids or len(set(ids)) != len(ids) or assessment['familySize'] != len(ids) or
            [row['id'] for row in bounds] != ids):
        raise ValueError('Every confirmed recipe and bound must be retained exactly once')
    qualifying = set()
    for cell, row in zip(cells, bounds, strict=True):
        if not 0 <= row['lower'] <= row['upper'] <= .5:
            raise ValueError('Every recipe must pass the adjusted upper ceiling')
        party = cell['scenario']['party']
        specialized = [member['partySlot'] for member in party for item in member['build']['equipment']
                       if '.spec.' in item['definitionId']]
        if len(specialized) <= 8 and len(set(specialized)) <= 2 and row['lower'] >= .1:
            qualifying.add(tuple(sorted((member['partySlot'], tuple(sorted(member['build']['essenceIds'])))
                                        for member in party)))
    if len(qualifying) < 2:
        raise ValueError('Two actual compositions must qualify with at most eight specialized items on two characters')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('source', 'audit', 'artifacts', 'owner'):
        p.add_argument('--'+name, type=Path, required=True)
    p.add_argument('--qualification-plan', type=Path, help='Explicit accepted-catalog transition; never edits the historical archive')
    p.add_argument('--aggregate', type=Path, help='Complete independently passing aggregate confirmation')
    p.add_argument('--aggregate-sha256', help='Frozen aggregate evidence hash')
    p.add_argument('--candidate-root', type=Path, help='Verify isolated confirmed content before editing the live catalog')
    a = p.parse_args()
    source, audit, artifacts, owner = [v.resolve() for v in (a.source, a.audit, a.artifacts, a.owner)]
    io = module('tower_balance_application_io', Path(__file__).with_name('run-tower-balance-pass.py'))
    io.check(owner.parent == ROOT/'TestResults' and not owner.exists(), 'Fresh direct TestResults child required')
    io.authenticate(source)
    evidence = io.read(audit)
    aggregate = None
    io.check(bool(a.aggregate) == bool(a.aggregate_sha256), 'Aggregate evidence and hash are required together')
    io.check(not a.candidate_root or not a.qualification_plan, 'Isolated candidate and catalog qualification are separate contracts')
    io.check(not a.aggregate or not a.qualification_plan, 'Aggregate and qualification are separate contracts')
    if a.aggregate:
        aggregate_io = module('tower_balance_aggregate', Path(__file__).with_name('tower-balance-aggregate.py'))
        member, aggregate = aggregate_io.application_batch(a.aggregate.resolve(), a.aggregate_sha256, source)
        io.check(member['manifestSha256'] == io.sha(source/'files.json') and
                 member['auditSha256'] == io.sha(audit), 'Wrong aggregate batch audit')
    else:
        io.check(evidence['status'] == 'Verified' and evidence['assessment']['verdict'] == 'Pass', 'Passing fresh confirmation required')
        if a.candidate_root:
            validate_single_candidate_confirmation(io.read(source/'request.json'), evidence)
            io.check(io.audit(source) == evidence, 'Isolated candidate confirmation audit differs')
            validate_single_candidate_equipment(io.read(source/'cells.json'), evidence)
    io.check(evidence['resultSha256'] == io.sha(source/'result.json'), 'Audit does not bind this result')
    qualification = None
    if a.qualification_plan:
        qualification = a.qualification_plan.resolve()
        module('tower_catalog_qualification', Path(__file__).with_name('tower-catalog-qualification.py')).validate_plan(
            io.read(qualification), source, ROOT/'LL/src/API/API.LL', artifacts/'bin/EssenceSystem.Tests/release')
    owner.mkdir()
    shutil.copy2(Path(__file__), owner/'owner-source.py')
    floor = ROOT/'LL/src/API/API.LL/Data/world-tower/tower-floors.json'
    floor_pin = io.sha(floor)
    q = dict(source=str(source), manifestPin=io.sha(source/'files.json'), audit=str(audit), auditPin=io.sha(audit),
             output=str(owner/'result.json'))
    if qualification:
        q.update(qualificationPlan=str(qualification), qualificationPlanPin=io.sha(qualification))
    if aggregate:
        q.update(aggregate=str(a.aggregate.resolve()), aggregatePin=a.aggregate_sha256)
    if a.candidate_root:
        candidate = a.candidate_root.resolve()
        io.check(candidate.is_relative_to(ROOT/'TestResults') and candidate != source,
                 'Isolated candidate must be a TestResults content directory')
        q['candidateRoot'] = str(candidate)
    io.write(owner/'request.json', q)
    os.environ['LL_TOWER_BALANCE_APPLICATION'] = str(owner/'request.json')
    process = module('tower_balance_application_process', ROOT/'build/bounded_windows_process.py')
    command = [shutil.which('pwsh'), '-NoProfile', '-File', str(ROOT/'build/run-tests.ps1'), '-NoBuild',
               '-ArtifactsPath', str(artifacts), '-Filter', 'FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests']
    receipt = process.run(command, ROOT, owner/'execution.log', time.monotonic()+660, log_byte_limit=1048576)
    io.write(owner/'process.json', receipt)
    io.check(receipt['exitCode'] == 0 and not receipt['timedOut'] and receipt['activeProcesses'] == 0, 'Application check failed')
    result = io.read(owner/'result.json')
    expected = 'AggregateInputsAndReplaysVerified' if aggregate else 'IsolatedInputsAndReplaysVerified' if a.candidate_root else 'QualifiedInputsAndReplaysVerified' if qualification else 'AppliedInputsAndReplaysVerified'
    family_size = aggregate['assessment']['familySize'] if aggregate else evidence['assessment']['familySize']
    io.check(result['status'] == expected and result['matchedInputs'] == evidence['evaluationFights']
             and result['fullReplays'] == family_size and io.sha(floor) == floor_pin, 'Incomplete application parity')
    io.write(owner/'completion.json', dict(status='Qualified' if qualification else 'Verified', floorFileSha256=floor_pin, resultSha256=io.sha(owner/'result.json'),
                                          matchedInputs=result['matchedInputs'], fullReplays=result['fullReplays'], newSeeds=0,
                                          aggregateSha256=a.aggregate_sha256, isolated=bool(a.candidate_root)))
    print(f"Verified {result['matchedInputs']} native inputs and {result['fullReplays']} complete replays.")


if __name__ == '__main__':
    main()
