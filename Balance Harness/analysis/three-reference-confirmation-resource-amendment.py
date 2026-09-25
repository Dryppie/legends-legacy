"""Prospective resource amendment only; never creates an executable request or launches a study."""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json'
FAILED=ROOT/'TestResults/three-reference-confirmation-admission-20260923'
CLOSEOUT=ROOT/'TestResults/three-reference-confirmation-admission-tooling-20260923'
EVIDENCE=ROOT/'TestResults/three-reference-confirmation-admission-overhead-20260923'
OUTPUT=ROOT/'Balance Harness/Tower-Practical-Three-Reference-Confirmation-Resource-Amendment.json'
VERSION='tower-three-reference-confirmation-resource-amendment-v1'
NATIVE='tower-practical-three-reference-confirmation-v2'
BASE_PIN='c6544a63a569de5197c4e2a35749c01a14388478e6a9986509d991c64bd6a74f'
FAILED_PIN='b238a72eb240fef91cf18a6ed5876cee0ad70880f623142059724c979595c5be'
CLOSEOUT_PIN='4bf14189a9d728e5aa2db10944ea60757b295bd80153477d88368087e73cc292'

def require(ok,message):
    if not ok: raise ValueError(message)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def canonical(value): return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':'),ensure_ascii=True,allow_nan=False).encode()).hexdigest()
def save(path,value):
    with path.open('x',encoding='utf-8',newline='\n') as stream: json.dump(value,stream,indent=2,allow_nan=False); stream.write('\n')

def resources(original,failure,charge):
    require(failure['status']=='AdmissionFailedNoReservation' and failure['scientificLaunches']==failure['newValues']==failure['fights']==failure['retries']==0,'Unclosed or scientific prior attempt')
    require((failure['admissionChargeSeconds'],failure['admissionChargeBytes'])==(600,536870912)
        and (charge['chargedSeconds'],charge['chargedBytes'])==(600,536870912),'Changed failed charge; no refund')
    value=copy.deepcopy(original)
    require(value['admissionAllowance']==dict(seconds=600,bytes=536870912)
        and value['executionOwner']==dict(seconds=7200,bytes=3758096384)
        and value['proposedStudyCumulative']==dict(seconds=7800,bytes=4294967296),'Changed original allowance')
    value['originalStudyCumulative']=copy.deepcopy(value['proposedStudyCumulative'])
    value['proposedStudyCumulative']=dict(seconds=8400,bytes=4831838208)
    value['previousFailedAdmission']=dict(seconds=600,bytes=536870912,receiptPath=str(FAILED/'admission-charge.json'),
        receiptSha256=sha(FAILED/'admission-charge.json'),failurePath=str(FAILED/'failure.json'),failureSha256=sha(FAILED/'failure.json'),
        externalInventoryPath=str(CLOSEOUT/'failed-admission-files.json'),externalInventorySha256=FAILED_PIN)
    value['preExecutionCharges']=dict(seconds=1200,bytes=1073741824,requiredDistinctReceipts=2)
    value['additionalStudyAllowance']=dict(seconds=600,bytes=536870912)
    value['accounting']='Failed admission plus one proposed new admission plus unchanged execution owner. Full admission allowances remain charged, including failures; no refund, transfer or reset.'
    value['fullWorkflowFeasibilityEstablished']=False
    return value

def build():
    require(sha(BASE)==BASE_PIN and sha(CLOSEOUT/'files.json')==CLOSEOUT_PIN
        and sha(CLOSEOUT/'failed-admission-files.json')==FAILED_PIN,'Changed frozen source')
    old=read(BASE); inventory=read(CLOSEOUT/'failed-admission-files.json')
    for name in ('failure.json','admission-charge.json','worker-process.json','context-process.json'):
        require(sha(FAILED/name)==inventory[name],'Changed failed evidence')
    compare=read(EVIDENCE/'comparison.json'); process=read(EVIDENCE/'comparison-process.json')
    require(compare['status']=='CompleteVerificationParity' and compare['completeByteVerification']
        and compare['nativePreparations']==compare['scientificLaunches']==compare['newValues']==compare['fights']==0
        and compare['optimizedSeconds']<compare['originalSeconds'],'Missing complete verification improvement')
    require(process['exitCode']==0 and not process['timedOut'] and process['activeProcesses']==0
        and process['mechanism']=='suspended-owned-job-v1','Incomplete verification job')
    require(sha(ROOT/'Balance Harness/analysis/confirmation-evidence-verification.py')==compare['verifierSha256'],'Changed measured verifier')
    return dict(version=VERSION,status='ProspectiveRequiresVersionedImplementation',runnableRequest=False,implementationComplete=False,
        originalPlanPath=str(BASE),originalPlanSha256=BASE_PIN,originalNativeVersion=old['proposedNativeVersion'],proposedNativeVersion=NATIVE,
        unchangedProtocolFields={k:canonical(v) for k,v in old.items() if k not in ('resources','proposedNativeVersion')},
        resources=resources(old['resources'],read(FAILED/'failure.json'),read(FAILED/'admission-charge.json')),
        costEvidence=dict(comparisonPath=str(EVIDENCE/'comparison.json'),comparisonSha256=sha(EVIDENCE/'comparison.json'),
            processReceiptSha256=sha(EVIDENCE/'comparison-process.json'),oldVerifierSeconds=compare['originalSeconds'],
            newVerifierSeconds=compare['optimizedSeconds'],completeInputArchives=3,
            limitation=compare['limitation'],nativeAdmissionCostMeasured=False,fullWorkflowFeasibilityEstablished=False),
        implementationRequirements=[
            'Add an explicit v2 resource profile while preserving v1 commands, canonical request identity, thresholds and all closed evidence.',
            'Pin both the original scientific plan and this resource amendment; do not accept widened resources under v1.',
            'Require the preserved failed-admission receipt and the separately charged new-admission receipt: exactly 1200 seconds and 1 GiB before execution.',
            'Keep the execution owner at 7200 seconds/3.5 GiB, native work at 6000 seconds/3 GiB, and both audits/publication at 1200 seconds/512 MiB.',
            'Use complete-byte verification and the repaired framework host; retain source symbols, copied runtime/content, both independent Python history scans and the native history check.',
            'Use a new package and output identity after versioned implementation and tests; never mutate or resume the failed admission.',
            'A further admission failure is terminal within this amendment: preserve both charges; no automatic additional attempt or budget extension.',
            'Test all cumulative charge paths, missing/changed receipts, interrupted publication, unchanged family-38 statistics and legacy v1 parity before any new admission.'
        ],newAdmissions=0,nativePreparations=0,scientificLaunches=0,newValues=0,fights=0,
        oldEngineeringTreatment='Keep the accepted 18180-second /13584-MiB ledger separately disclosed; complete historical engineering totals remain unknown.')

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--write',action='store_true'); args=parser.parse_args()
    result=build()
    if args.write: save(OUTPUT,result)
    else: require(read(OUTPUT)==result,'Amendment does not reproduce')
    print(json.dumps(dict(status=result['status'],sha256=sha(OUTPUT),runnableRequest=False,newAdmissions=0,newValues=0,fights=0),indent=2))
