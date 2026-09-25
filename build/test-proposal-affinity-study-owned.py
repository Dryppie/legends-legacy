"""Full Windows-owned proposal study fixture with literal entropy and reports.

Only the native content/combat boundary is replaced through the separate test host.
Admission pinning, registry/output leases, reservation, all searches, global barrier,
both audits, publication, terminal accounting and final verification stay active.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import time
from unittest.mock import patch

import bounded_windows_process

ROOT=Path(__file__).resolve().parents[1]


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path); value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


def prepare(args):
    owner=module('proposal_owned_launcher',ROOT/'build/run-proposal-affinity-study.py')
    root=args.output.resolve(); host=args.fixture_host.resolve(); source=args.source_fixture.resolve(); started=time.monotonic()
    owner.require(root.name.startswith('tower-proposal-owned-fixture-') and not root.exists(),'Require a new explicitly synthetic output')
    root.mkdir(); package=root/'package'; package.mkdir(); fixture=root/'fixture.json'
    owner.write(fixture,dict(source=str(source),registryRoot=str(root),packageRoot=str(package),mode=args.mode,
        **({'profile':args.profile} if getattr(args,'profile',None) is not None else {})))
    run=bounded_windows_process.run
    def process(action,log,seconds):
        receipt=run(['dotnet',str(host),'proposal-fixture-'+action,str(fixture)],str(ROOT),str(log),time.monotonic()+seconds,cleanup_seconds=1)
        owner.require(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Owned fixture failed: '+str(log))
        return receipt
    preparation=process('prepare',root/'prepare.log',120)
    for source_path,target in [(ROOT/'build/run-proposal-affinity-study.py','run-proposal-affinity-study.py'),
        (ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),
        (ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py','auditor.py')]:
        shutil.copyfile(source_path,package/target)
    version=json.loads((package/'plan.json').read_text(encoding='utf-8-sig'))['version']
    q=dict(version=version,contentRoot=str(package/'content'),registryRoot=str(root),outputRoot=str(root/'result'),
        requiredHistory={str(root/'prior-seed-ledger.json'):owner.digest(root/'prior-seed-ledger.json')},pendingHistoryRecoveries={},recoveryReceiptHashes={})
    if args.resource_envelope is not None: q['resourceEnvelope']=args.resource_envelope
    for name in ('plan','context','settings','history','runtime'):
        q[name]=dict(path=str(package/(name+'.json')),sha256=owner.digest(package/(name+'.json')))
    q['auditor']=dict(path=str(package/'auditor.py'),sha256=owner.digest(package/'auditor.py'))
    owner.write(package/'request.json',q)
    if getattr(args, 'profile', None) in ('plain-maximum-placement-v1', 'plain-maximum-nomination-v1'):
        process('inspect',root/'inspection.log',max(1,120-(time.monotonic()-started)))
    owner.write(package/'admission.json',dict(version=version,status='ProposalStudyAdmittedNoReservation',
        requestSha256=owner.digest(package/'request.json'),fights=0,newValues=0,fixtureOnly=True,
        **({'resourceEnvelope':args.resource_envelope} if args.resource_envelope is not None else {})))
    owner.write(package/'files.json',{p.relative_to(package).as_posix():owner.digest(p) for p in owner.inventory(package)})
    return owner,root,host,package,fixture,started,preparation


def execute(args,prepared):
    owner,root,host,package,fixture,started,preparation=prepared
    run=bounded_windows_process.run
    version=json.loads((package/'plan.json').read_text(encoding='utf-8-sig'))['version']
    def process(action,log,seconds):
        receipt=run(['dotnet',str(host),'proposal-fixture-'+action,str(fixture)],str(ROOT),str(log),time.monotonic()+seconds,cleanup_seconds=1)
        owner.require(receipt['exitCode']==0 and not receipt['timedOut'] and receipt['activeProcesses']==0,'Owned fixture failed: '+str(log))
        return receipt
    commands=[]
    def routed(command,*positional,**kwargs):
        if len(command)>2 and command[2].startswith('tower-proposal-study-'):
            action=command[2][len('tower-proposal-study-'):]
            command=['dotnet',str(host),'proposal-fixture-'+action,str(fixture)]
        commands.append(command)
        return run(command,*positional,**kwargs)
    failure=None
    with patch('bounded_windows_process.run',side_effect=routed):
        try: owner.launch(package/'request.json',package/'runtime/BalanceHarness.dll','dotnet',owner.digest(package/'files.json'))
        except ValueError as error: failure=str(error)
    output=root/'result'
    if args.mode=='attempt-failure':
        owner.require(failure is not None and (output/'failure.json').exists() and not (output/'result.json').exists()
            and not (output/'completion.json').exists() and not (output/'closeout.json').exists(), 'Failed attempt published')
        charges=[json.loads(line) for line in (output/'attempts.jsonl').read_text().splitlines()]
        owner.require(charges==[dict(kind='Started',ordinal=1)],'Lost failed-attempt charge')
        reservation=json.loads((output/'allocation.json').read_text())
        owner.require(len(reservation['reserved'])==16384 and len(reservation['selected'])==(4380 if version in (owner.PRESERVATION_VERSION, owner.ALLIED_VERSION, owner.PLACEMENT_VERSION, owner.NOMINATION_VERSION) else 4668 if version==owner.VALIDATION_VERSION else 3948),'Lost exposed tail')
        owner.require(len(commands)==1,'Failed worker advanced to audits')
        summary=dict(status='OwnedProposalFailureFixturePassed',failure=failure,chargedAttempts=1,retainedValues=16384)
    else:
        owner.require(failure is None,'Owned completion failed: '+str(failure))
        pin=owner.digest(output/'closeout.json'); (root/'closeout-pin.txt').write_text(pin,encoding='utf-8')
        verification=process('verify',root/'verification.log',1200)
        result=json.loads((output/'result.json').read_text()); verified=json.loads((root/'verification.log').read_text(encoding='utf-8-sig'))
        selector = version == owner.SELECTOR_VERSION
        validation = version == owner.VALIDATION_VERSION
        placement = version == owner.PLACEMENT_VERSION
        preservation = version in (owner.PRESERVATION_VERSION, owner.ALLIED_VERSION, owner.PLACEMENT_VERSION, owner.NOMINATION_VERSION)
        deduplicated = (validation or preservation) and not placement
        if getattr(args,'profile',None) is not None:
            owner.require(result==verified and len(commands)==4 and result['status']=='Verified'
                and result['searchFights']==12672 and result['fights']==result['searchFights']+result['heldoutFights']
                and 12672 < result['fights'] <= 21888 and len(result['roots'])==12,'Incomplete matched owned fixture')
        else:
            owner.require(result==verified and len(commands)==4 and result['fights']==(17280 if deduplicated else 18816) and result['heldoutFights']==(4608 if deduplicated else 6144)
                and result['decision']==('Inconclusive' if selector or validation or placement else 'NoObservedOutputDifferentiation')
                and result['differingRoots']==(6 if validation or placement else 12 if selector else 0),'Incomplete owned fixture')
        if validation:
            owner.require(result['validation']['passedRoots']==result['validation']['fallbackRoots']==6
                and len(result['validation']['decisions'])==12 and all(r['changedPositionsPerWave']==[0,0] for r in result['roots']), 'Changed validation diagnostics')
        if preservation:
            maximum = getattr(args, 'profile', None) in ('plain-maximum-placement-v1', 'plain-maximum-nomination-v1')
            expected_changes = (all(r['changedPositionsPerWave']==[0,0] for r in result['roots']) if version == owner.NOMINATION_VERSION
                else any(any(n>0 for n in r['changedPositionsPerWave']) for r in result['roots']))
            owner.require(result['validation']['passedRoots']==result['controlValidation']['passedRoots']==(12 if maximum else 6)
                and result['validation']['fallbackRoots']==result['controlValidation']['fallbackRoots']==(0 if maximum else 6)
                and expected_changes, 'Changed preservation diagnostics')
            if maximum:
                owner.require(result['fights']==21888 and result['heldoutFights']==9216 and result['differingRoots']==12,
                    'Maximum placement workload not covered')
        if selector:
            owner.require(all(r['changedPositionsPerWave']==[0,0] and r['candidateParty']==r['benchmarkParty']
                for r in result['roots']), 'Selector fixture changed generation or missed the benchmark tie')
        summary=dict(status='OwnedProposalFixturePassed',literalReports=result['fights'],nativeWorker=True,nativeReconstruction=True,
            independentPythonAudit=True,nativePublicationBarrier=True,nativePostPublicationVerification=True,closeoutSha256=pin,
            archiveManifestSha256=owner.digest(output/'files.json'),verificationProcess=verification)
    summary.update(version=version,actualCombat=0,productionEntropyDraws=0,seconds=time.monotonic()-started,retainedBytes=owner.storage_bytes(root),
        fixtureHostSha256=owner.digest(host),harnessSha256=owner.digest(package/'runtime/BalanceHarness.dll'),preparationProcess=preparation,
        limitation=('Captured qualified content/runtime with real input identities and production audits; literal combat outcomes are not efficacy evidence or resource admission.'
            if getattr(args, 'profile', None) in ('plain-maximum-placement-v1', 'plain-maximum-nomination-v1') else
            'Literal content and combat boundary; engineering verification is not production runtime qualification, admission or efficacy evidence.'))
    owner.write(root/'verification.json',summary)
    return summary


def main(args):
    print(json.dumps(execute(args,prepare(args)),indent=2))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--fixture-host',type=Path,required=True)
    parser.add_argument('--source-fixture',type=Path,required=True); parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--mode',choices=['complete','attempt-failure'],default='complete')
    parser.add_argument('--resource-envelope',choices=['tower-proposal-resource-envelope-v1','tower-proposal-resource-envelope-v2'])
    main(parser.parse_args())
