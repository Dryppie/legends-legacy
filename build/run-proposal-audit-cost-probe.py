"""One owned, separately charged native audit-cost measurement. Never admits or launches a study."""
import argparse
import ctypes
from ctypes import wintypes
import datetime as dt
import importlib.util
import json
import math
import os
from pathlib import Path
import shutil
import sys
import threading
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
VERSION = 'proposal-native-audit-cost-probe-v1'
SECONDS, WORKER_SECONDS, BYTES = 600, 480, 256*1048576
OUTPUT = ROOT/'TestResults/proposal-native-audit-cost-probe-20260923'
QUALIFIED = ROOT/'TestResults/proposal-affinity-admission-repaired-20260923'
REVIEW = ROOT/'TestResults/proposal-affinity-admission-review-20260923'
REVIEW_PIN = 'b2cd5249373b58069af6000579aa2d73c971999adc74b5d57e19a0369b8c28d2'
HISTORICAL = ROOT/'TestResults/balance/tower-frozen-pool-recognition-repaired-20260923'
HISTORICAL_PIN = '5fd7e9eb38eb4319f41be7a0e874297a897c7a32a73d09030261579b2473b377'
FIXTURE = ROOT/'TestResults/tower-proposal-owned-fixture-complete-final-20260923/result'
FIXTURE_PIN = '641e732b63f871e44fd74c0bb46602cd0a6e0b2fa810c31ff00d35a248e06e7e'
HOST_FILES = tuple('BalanceHarness.ProcessFixture'+s for s in ('.dll','.pdb','.deps.json','.runtimeconfig.json'))


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path); value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


shared=module('audit_probe_shared',ROOT/'build/run-proposal-affinity-study.py')
owner=module('audit_probe_job',ROOT/'build/bounded_windows_process.py')
require,sha,save=shared.require,shared.digest,shared.write


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def inventory(root): return {p.relative_to(root).as_posix():sha(p) for p in shared.inventory(root)}


def parent_start_ticks():
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.GetCurrentProcess.restype=wintypes.HANDLE
    kernel.GetProcessTimes.argtypes=[wintypes.HANDLE]+[ctypes.POINTER(wintypes.FILETIME)]*4
    times=[wintypes.FILETIME() for _ in range(4)]
    require(kernel.GetProcessTimes(kernel.GetCurrentProcess(),*[ctypes.byref(t) for t in times]),'Missing parent creation time')
    return (times[0].dwHighDateTime<<32)+times[0].dwLowDateTime+504911232000000000


def request(output,host,mode='native',started=None):
    utc=started or dt.datetime.now(dt.timezone.utc)
    return dict(version=VERSION,mode=mode,repository=str(ROOT),output=str(output),startedAt=utc.isoformat(),
                deadline=(utc+dt.timedelta(seconds=WORKER_SECONDS)).isoformat(),parentId=os.getpid(),
                parentStartTicks=parent_start_ticks(),hostHash=sha(host))


def process_ok(p):
    require(p['mechanism']=='suspended-owned-job-v1' and p['exitCode']==0 and not p['timedOut']
            and p['activeProcesses']==0 and p['totalProcesses']>=1,'Owned probe process failed')


def validate_observation(value):
    require(value['status']=='NativeAuditCostWorkloadComplete' and value['resourceOnly'] is True
            and value['inputReconstructions']==27648 and value['proposalArms']==24 and value['literalRows']==12672
            and value['historyValues']==672220 and value['fights']==value['newValues']==0
            and value['admission']=='NotPerformed','Incomplete or promoted resource workload')
    names=['qualification','historical-inventory','historical-native-reconstruction','full-history-workloads','full-history-proposal-replay']
    require([p['name'] for p in value['phases']]==names and all(math.isfinite(p['seconds']) and p['seconds']>=0 for p in value['phases']),
            'Missing, reordered or invalid phases')


def estimate(worker_seconds,python_seconds,inventory_seconds,source_bytes,target_bytes):
    require(all(math.isfinite(x) and x>=0 for x in (worker_seconds,*python_seconds,inventory_seconds))
            and source_bytes>0 and target_bytes>0 and len(python_seconds)==2,'Invalid resource measurements')
    # Frozen before the single run: retain the whole worker (including synthetic
    # trajectory creation), both independent audits, two extra projected outer
    # inventory passes, a factor of two, and 120 seconds of publication reserve.
    ratio=max(1,target_bytes/source_bytes)
    extra=2*inventory_seconds*ratio
    seconds=2*(worker_seconds+sum(python_seconds)+extra)+120
    return dict(status='SupportsAnotherAdmissionReview' if seconds<1200 else 'AuditPartitionStillNotSupported',
                auditPlanningSeconds=seconds,auditMaximumSeconds=1200,wholeWorkerSeconds=worker_seconds,
                independentAuditSeconds=python_seconds,extraInventorySeconds=extra,inventoryByteRatio=ratio,
                timingMargin=2,publicationReserveSeconds=120,admitted=False,guaranteedUpperBound=False,
                interpretation='One historical native workload and synthetic proposal replay; not future output efficacy or a guarantee of future runtime.')


def ensure_new_output(output):
    require(output.resolve()==OUTPUT and not output.exists(),'Use the sole new probe output; no retry or resume')


def authenticate(root,pin):
    require(sha(root/'files.json')==pin,'Changed external evidence pin')
    require(inventory(root)==dict(read(root/'files.json'),**{'files.json':pin}),'Changed evidence membership or bytes')


def run(host_folder,tests):
    require(os.name=='nt','Probe requires owned Windows Jobs'); ensure_new_output(OUTPUT)
    host_folder=host_folder.resolve()
    # Validate the bounded backend test selection before the expensive probe.
    ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    results=ET.parse(tests).findall('.//t:UnitTestResult',ns)
    require(len(results)==5 and all(r.attrib['outcome']=='Passed' and 'BalanceHarnessProposalAuditCostProbeTests' in r.attrib['testName'] for r in results),
            'Missing passing probe backend verification')
    started=time.monotonic(); utc=dt.datetime.now(dt.timezone.utc)
    with shared.writer_lease(OUTPUT):
        ensure_new_output(OUTPUT); OUTPUT.mkdir()
        save(OUTPUT/'charge.json',dict(version=VERSION,chargedSeconds=SECONDS,chargedBytes=BYTES,
            scope='One native audit-cost probe and both independent read-only audits. Full charge including failure; no refund, transfer, retry, admission or scientific launch.',
            priorAdmissionAndReviewChargedSeconds=1860,priorAdmissionAndReviewChargedBytes=2164260864))
        watchdog=threading.Timer(SECONDS,lambda:os._exit(124));watchdog.daemon=True;watchdog.start()
        def check():
            require(time.monotonic()-started<SECONDS-3 and shared.storage_bytes(OUTPUT)<BYTES-2*1048576,'Probe time/storage allowance reached')
        def command(name,args,deadline):
            result=owner.run(args,str(ROOT),str(OUTPUT/(name+'.log')),deadline,cleanup_seconds=1,check=check)
            save(OUTPUT/(name+'-process.json'),result);process_ok(result);check();return result
        try:
            authenticate(REVIEW,REVIEW_PIN)
            frozen=read(REVIEW/'attempt-2-files.json'); require(inventory(QUALIFIED)==frozen,'Changed qualified failed admission')
            save(OUTPUT/'qualified-files.json',frozen); shutil.copyfile(REVIEW/'files.json',OUTPUT/'qualification-review-files.json')
            shutil.copyfile(REVIEW/'review.json',OUTPUT/'qualification-review.json')
            shutil.copyfile(REVIEW/'reconstructed-resource-forecast.json',OUTPUT/'previous-resource-forecast.json')
            require(sha(HISTORICAL/'files.json')==HISTORICAL_PIN,'Changed historical archive pin')
            historical_files=read(HISTORICAL/'files.json'); shutil.copyfile(HISTORICAL/'files.json',OUTPUT/'historical-files.json')
            shutil.copytree(QUALIFIED/'runtime',OUTPUT/'runtime')
            require(inventory(OUTPUT/'runtime')==read(QUALIFIED/'runtime.json'),'Changed retained producing runtime')
            for name in HOST_FILES: shutil.copyfile(host_folder/name,OUTPUT/'runtime'/name)
            save(OUTPUT/'probe-runtime.json',inventory(OUTPUT/'runtime'))
            host=OUTPUT/'runtime/BalanceHarness.ProcessFixture.dll'
            for source,name in [(Path(__file__),'owner.py'),(ROOT/'build/bounded_windows_process.py','bounded_windows_process.py'),
                (ROOT/'build/run-proposal-affinity-study.py','shared.py'),(tests,'backend-tests.trx')]: shutil.copyfile(source,OUTPUT/name)
            save(OUTPUT/'declaration.json',dict(version=VERSION,resourceOnly=True,qualificationReviewPin=REVIEW_PIN,historicalPin=HISTORICAL_PIN,
                proposalFixtureCloseoutPin=FIXTURE_PIN,workerSeconds=WORKER_SECONDS,maximumSeconds=SECONDS,maximumBytes=BYTES,
                inputReconstructions=27648,proposalArms=24,literalRows=12672,roots='first 876 sorted historical values outside legacy/reference schedules, 12 blocks of 73',
                literalOutcome='Defeat, health 100, survival 0, duration 1; no observations or team recommendations',
                planningFormula='2*(whole owned worker seconds + both independent audit seconds + 2*historical inventory seconds*max(1,previous projected bytes/historical archive bytes))+120',
                auditMaximumSeconds=1200,admitted=False,fights=0,newValues=0))
            save(OUTPUT/'request.json',request(OUTPUT,host,started=utc));check()
            worker=command('native',['dotnet',str(host),'proposal-audit-cost-probe',str(OUTPUT/'request.json')],started+WORKER_SECONDS-2)
            observation=read(OUTPUT/'worker-observation.json');validate_observation(observation)
            require(sha(HISTORICAL/'auditor.py')==historical_files['auditor.py'],'Changed independent historical auditor')
            shutil.copyfile(HISTORICAL/'auditor.py',OUTPUT/'historical-auditor.py')
            historical=command('independent-historical',[sys.executable,'-B','-X','utf8',str(OUTPUT/'historical-auditor.py'),
                '--working',str(HISTORICAL),'--output',str(OUTPUT/'independent-historical.json')],started+SECONDS-4)
            require(read(OUTPUT/'independent-historical.json')['status']=='Passed','Historical independent audit failed')
            require(sha(FIXTURE/'closeout.json')==FIXTURE_PIN,'Changed proposal fixture closeout')
            shutil.copyfile(QUALIFIED/'auditor.py',OUTPUT/'proposal-auditor.py')
            proposal=command('independent-proposal',[sys.executable,'-B','-X','utf8',str(OUTPUT/'proposal-auditor.py'),str(FIXTURE),
                '--pin',FIXTURE_PIN,'--output',str(OUTPUT/'independent-proposal.json')],started+SECONDS-4)
            require(read(OUTPUT/'independent-proposal.json')['status']=='Passed','Proposal independent audit failed')
            require(inventory(QUALIFIED)==frozen and inventory(OUTPUT/'runtime')==read(OUTPUT/'probe-runtime.json'),'Source/runtime changed during probe')
            phase=next(p for p in observation['phases'] if p['name']=='historical-inventory')
            source_bytes=shared.storage_bytes(HISTORICAL)
            forecast=estimate(worker['seconds'],[historical['seconds'],proposal['seconds']],phase['seconds'],source_bytes,
                              read(OUTPUT/'previous-resource-forecast.json')['totalBytes'])
            save(OUTPUT/'resource-assessment.json',forecast);check()
            save(OUTPUT/'completion.json',dict(version=VERSION,status='ResourceProbeCompleteNoAdmission',chargedSeconds=SECONDS,chargedBytes=BYTES,
                measuredSecondsBeforeSealing=time.monotonic()-started,retainedBytesBeforeSealing=shared.storage_bytes(OUTPUT),
                priorAndProbeChargedSeconds=2460,priorAndProbeChargedBytes=2432696320,fights=0,newValues=0,admitted=False))
            save(OUTPUT/'files.json',inventory(OUTPUT));check()
            print(json.dumps(dict(status=forecast['status'],admitted=False,auditPlanningSeconds=forecast['auditPlanningSeconds'],
                manifestSha256=sha(OUTPUT/'files.json'),seconds=time.monotonic()-started,retainedBytes=shared.storage_bytes(OUTPUT),fights=0,newValues=0),indent=2))
        except BaseException as error:
            save(OUTPUT/'failure.json',dict(version=VERSION,status='ResourceProbeFailedNoAdmission',reason=str(error),
                chargedSeconds=SECONDS,chargedBytes=BYTES,seconds=time.monotonic()-started,fights=0,newValues=0,admitted=False))
            raise
        finally: watchdog.cancel()


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--host',required=True,type=Path);parser.add_argument('--tests',required=True,type=Path)
    args=parser.parse_args();run(args.host,args.tests)
