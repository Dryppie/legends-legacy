"""Bounded full owned fixture: literal entropy and reports, with the native combat trace guard active.

Only admission and command routing are substituted. Leases, process ownership,
reservation, archive, native reconstruction, independent audit and publication are real.
The declared engineering ceiling includes context and postpublication verification.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import time
from unittest.mock import patch

import bounded_windows_process

ROOT=Path(__file__).resolve().parents[1]
SECONDS,BYTES=1200,2147483648


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path)
    value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value


def main(args):
    launcher=module('neighborhood_owned_launcher',ROOT/'build/run-affinity-neighborhood-recognition.py')
    audit=module('neighborhood_owned_auditor',ROOT/'Balance Harness/analysis/audit-affinity-neighborhood-recognition.py')
    root=args.output.resolve();host=args.fixture_host.resolve();harness=host.with_name('BalanceHarness.dll')
    plan_path=ROOT/'Balance Harness/Tower-Affinity-Neighborhood-Recognition-Plan.json'
    auditor=ROOT/'Balance Harness/analysis/audit-affinity-neighborhood-recognition.py'
    settings_path=ROOT/'TestResults/balance/tower-affinity-allied-action-pilot-01-20260924/source/settings.json'
    launcher.require(root.parent==ROOT/'TestResults' and root.name.startswith('tower-neighborhood-recognition-owned-fixture-')
        and not root.exists(),'Require a new separately named synthetic output under TestResults')
    launcher.require(audit.sha(plan_path)==audit.PLAN,'Changed frozen plan')
    sources=[Path(__file__),ROOT/'build/run-affinity-neighborhood-recognition.py',ROOT/'build/bounded_windows_process.py',
        auditor,plan_path,host,harness,settings_path]
    source_pins={str(p):audit.sha(p) for p in sources}
    plan=audit.read(plan_path);settings=audit.read(settings_path)
    launcher.require(audit.digest(settings)==plan['sourceContract']['capturedRuntime']['settingsHash'],'Changed fixture settings')
    root.mkdir();started=time.monotonic();deadline=started+SECONDS
    launcher.write(root/'declaration.json',dict(version='synthetic-neighborhood-owned-fixture-v1',maximumSeconds=SECONDS,maximumBytes=BYTES,
        newCombat=0,productionEntropyDraws=0,sourcePins=source_pins,noRetry=True,
        scope='Context, complete literal owned run, both audits, publication, native postpublication verification and final fixture seal.'))
    run=bounded_windows_process.run
    def check():
        launcher.require(time.monotonic()<deadline,'Engineering fixture time ceiling exceeded')
        launcher.require(launcher.storage_bytes(root)<=BYTES,'Engineering fixture storage ceiling exceeded')
    def process(command,log):
        value=run(command,str(ROOT),str(log),deadline,cleanup_seconds=1,check=check)
        launcher.require(value['exitCode']==0 and not value['timedOut'] and value['activeProcesses']==0,'Owned fixture process failed: '+str(log))
        return value
    try:
        (root/'content').mkdir()
        context_process=process(['dotnet',str(harness),'tower-affinity-neighborhood-recognition-context',str(ROOT/'LL/src/API/API.LL')],root/'context.log')
        context=audit.read(root/'context.log');runtime=plan['sourceContract']['capturedRuntime']
        d=dict(version=launcher.VERSION,teams=[dict(role=t['membership'],partyId=t['partyId'],referenceIds=[],scenario=t['scenario']) for t in plan['teams']],
            contentHashes=runtime['contentHashes'],settingsHash=runtime['settingsHash'],executionHash=context['executionHash'],excludedCombatSeeds=[-987])
        launcher.write(root/'definition.json',d)
        prior=root/'prior-seed-ledger.json';launcher.write(prior,dict(historical=[-987]))
        q=dict(version=launcher.VERSION,contentRoot=str(root/'content'),definitionPath=str(root/'definition.json'),definitionHash=audit.sha(root/'definition.json'),
            registryRoot=str(root),outputRoot=str(root/'result'),requiredHistory={str(prior):audit.sha(prior)},
            maximumSeconds=19800,maximumBytes=18790481920,phases={},priorSeconds=1800,priorBytes=536870912,
            pendingHistoryRecoveries=None,recoveryReceiptHashes=None,
            priorCharges=[dict(scope='Literal fixture admission only',seconds=1800,bytes=536870912,receiptPath=str(prior),receiptHash=audit.sha(prior))],
            recognition=dict(planPath=str(plan_path),auditorPath=str(auditor),auditorHash=audit.sha(auditor)))
        launcher.write(root/'request.json',q)
        launcher.write(root/'fixture.json',dict(version='synthetic-fixed-family-confirmation-fixture-v1',mode='neighborhood-recognition-owned',request=q,settings=settings))
        commands=[]
        def routed(command,cwd,log,process_deadline,**kwargs):
            if len(command)>2 and command[2].startswith('tower-affinity-neighborhood-recognition-'):
                action=command[2][len('tower-affinity-neighborhood-recognition-'):]
                command=['dotnet',str(host),'neighborhood-recognition-fixture-'+action,str(root/'fixture.json')]
            commands.append(command);production_check=kwargs.get('check',lambda:None)
            def both_checks():
                check();production_check()
            kwargs['check']=both_checks
            return run(command,cwd,log,min(process_deadline,deadline),**kwargs)
        with patch('bounded_windows_process.run',side_effect=routed),patch.object(launcher,'validate_admission'):
            launcher.launch(root/'request.json',harness,'dotnet')
        receipt=process(['dotnet',str(host),'neighborhood-recognition-fixture-verify',str(root/'fixture.json')],root/'verification.log')
        result=audit.read(root/'result/result.json');launcher.require(result==audit.read(root/'verification.log'),'Native postpublication verification differs')
        launcher.require(len(commands)==4 and len(result['neighborhood']['endpoints'])==46
            and result['decision']=='FreshConfirmationWarranted','Incomplete end-to-end fixture')
        launcher.require(len(list((root/'result/study/battles').glob('*.json.gz')))==90112,'Incomplete literal report count')
        launcher.require(all(audit.sha(Path(p))==pin for p,pin in source_pins.items()),'Fixture source changed while executing')
        summary=dict(status='OwnedNeighborhoodFixturePassed',literalReports=90112,actualCombat=0,productionEntropyDraws=0,
            nativeWorker=True,nativeReconstruction=True,independentPythonAudit=True,nativePublicationBarrier=True,nativePostPublicationVerification=True,
            archiveManifestSha256=audit.sha(root/'result/files.json'),contextProcess=context_process,verificationProcess=receipt,
            limitation='Synthetic inputs/outcomes and literal entropy. Engineering compatibility does not establish efficacy, runtime admission or a combat runtime bound.')
        launcher.write(root/'verification.json',summary)
        launcher.write(root/'files.json',{p.relative_to(root).as_posix():audit.sha(p) for p in launcher.inventory(root)})
        retained=launcher.storage_bytes(root);elapsed=time.monotonic()-started
        closeout=dict(status='FixtureSealed',declaredSeconds=SECONDS,declaredBytes=BYTES,measuredSeconds=elapsed,retainedBytes=retained,
            filesHash=audit.sha(root/'files.json'),newFights=0,newValues=0)
        # Include the final receipt's own bytes without rewriting a sealed receipt.
        for _ in range(10):
            size=len(json.dumps(closeout,indent=2).encode('utf-8'))
            if closeout['retainedBytes']==retained+size:break
            closeout['retainedBytes']=retained+size
        launcher.write(root/'closeout.json',closeout);check()
        launcher.require(closeout['retainedBytes']==launcher.storage_bytes(root),'Incomplete retained-byte accounting')
        print(json.dumps(dict(summary,**closeout),indent=2))
    except BaseException as error:
        if not (root/'failure.json').exists():
            launcher.write(root/'failure.json',dict(status='FailedRetainedNoRetry',error=str(error),declaredSeconds=SECONDS,declaredBytes=BYTES,
                measuredSeconds=time.monotonic()-started,retainedBytesBeforeFailureReceipt=launcher.storage_bytes(root),newFights=0,newValues=0))
        raise


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--fixture-host',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    main(p.parse_args())
