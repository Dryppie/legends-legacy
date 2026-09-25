"""Full owned-process recognition fixture. Literal reports and entropy; a combat trace guard rejects any simulator call.

Only command routing and the admission prerequisite are replaced. Process ownership,
leases, native worker orchestration, reservation, archive, both audits, publication and
post-publication native verification all execute their production implementations.
"""
import argparse
import copy
import importlib.util
import json
from pathlib import Path
import sys
import time
from unittest.mock import patch

import bounded_windows_process

ROOT = Path(__file__).resolve().parents[1]


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path); value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


def main(args):
    launcher=module('recognition_owned_launcher',ROOT/'build/run-affinity-creation-recognition.py')
    audit=module('recognition_owned_auditor',ROOT/'Balance Harness/analysis/audit-affinity-creation-recognition.py')
    root=args.output.resolve(); source=args.source_fixture.resolve()/'result'; host=args.fixture_host.resolve(); harness=host.with_name('BalanceHarness.dll')
    launcher.require(root.name.startswith('tower-affinity-recognition-owned-fixture-') and not root.exists(),'Require a new explicitly synthetic output')
    root.mkdir(); (root/'content').mkdir(); started=time.monotonic(); run=bounded_windows_process.run
    def process(command,log):
        value=run(command,str(ROOT),str(log),started+1200,cleanup_seconds=1)
        launcher.require(value['exitCode']==0 and not value['timedOut'] and value['activeProcesses']==0,'Owned fixture process failed: '+str(log))
        return value
    context_process=process(['dotnet',str(harness),'tower-affinity-creation-recognition-context',str(ROOT/'LL/src/API/API.LL')],root/'context.log')
    context=audit.read(root/'context.log'); d=copy.deepcopy(audit.read(source/'source-definition.json')); d['executionHash']=context['executionHash']
    launcher.write(root/'definition.json',d); launcher.write(root/'prior-seed-ledger.json',dict(historical=d['excludedCombatSeeds']))
    prior=root/'prior-seed-ledger.json'; q=copy.deepcopy(audit.read(source/'request.json'))
    q.update(contentRoot=str(root/'content'),definitionPath=str(root/'definition.json'),definitionHash=audit.sha(root/'definition.json'),
        registryRoot=str(root),outputRoot=str(root/'result'),requiredHistory={str(prior):audit.sha(prior)},
        priorCharges=[dict(scope='Literal owned fixture admission',seconds=600,bytes=536870912,receiptPath=str(prior),receiptHash=audit.sha(prior))])
    q['recognition']=dict(planPath=str(ROOT/'Balance Harness/Tower-Affinity-Creation-Recognition-Plan.json'),
        auditorPath=str(ROOT/'Balance Harness/analysis/audit-affinity-creation-recognition.py'),auditorHash=audit.sha(ROOT/'Balance Harness/analysis/audit-affinity-creation-recognition.py'))
    preceding=root/'preceding-admission-charge.json'; launcher.write(preceding,dict(literal=True,seconds=600,bytes=536870912))
    q['priorCharges'].insert(0,dict(scope='Literal preserved failed admission',seconds=600,bytes=536870912,receiptPath=str(preceding),receiptHash=audit.sha(preceding)))
    q.update(priorSeconds=1200,priorBytes=1073741824,maximumSeconds=8400,maximumBytes=4831838208)
    launcher.write(root/'request.json',q)
    launcher.write(root/'fixture.json',dict(version='synthetic-fixed-family-confirmation-fixture-v1',mode='affinity-recognition-owned',request=q,
        settings=audit.read(source/'study/scope.json')['settings']))
    commands=[]
    def routed(command,*positional,**kwargs):
        if len(command)>2 and command[2].startswith('tower-affinity-creation-recognition-'):
            action=command[2][len('tower-affinity-creation-recognition-'):]
            command=['dotnet',str(host),'affinity-recognition-fixture-'+action,str(root/'fixture.json')]
        commands.append(command)
        return run(command,*positional,**kwargs)
    with patch('bounded_windows_process.run',side_effect=routed),patch.object(launcher,'validate_admission'):
        launcher.launch(root/'request.json',harness,'dotnet')
    receipt=process(['dotnet',str(host),'affinity-recognition-fixture-verify',str(root/'fixture.json')],root/'verification.log')
    result=audit.read(root/'result/result.json'); launcher.require(result==audit.read(root/'verification.log'),'Native post-publication verification differs')
    launcher.require(len(commands)==4 and len(result['contrasts'])==216 and result['decision']=='CompleteDiagnosticOnly','Incomplete end-to-end fixture')
    summary=dict(status='OwnedRecognitionFixturePassed',literalReports=27648,actualCombat=0,productionEntropyDraws=0,
        nativeWorker=True,nativeReconstruction=True,independentPythonAudit=True,nativePublicationBarrier=True,nativePostPublicationVerification=True,
        seconds=time.monotonic()-started,retainedBytes=launcher.storage_bytes(root),archiveManifestSha256=audit.sha(root/'result/files.json'),
        fixtureHostSha256=audit.sha(host),harnessSha256=audit.sha(harness),contextProcess=context_process,verificationProcess=receipt,
        limitation='Synthetic combat inputs/outcomes and literal entropy; engineering compatibility does not establish combat efficacy or a runtime upper bound.')
    launcher.write(root/'verification.json',summary); print(json.dumps(summary,indent=2))


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__); p.add_argument('--fixture-host',type=Path,required=True)
    p.add_argument('--source-fixture',type=Path,required=True); p.add_argument('--output',type=Path,required=True)
    main(p.parse_args())
