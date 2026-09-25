"""One read-only final verification inside the original pilot audit allowance."""
import argparse
import datetime as dt
import importlib.util
import json
from pathlib import Path
import time

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('nomination_verify_helpers',Path(__file__).with_name('run-affinity-nomination-cycle.py'))
cycle=importlib.util.module_from_spec(spec); spec.loader.exec_module(cycle)
SOURCE=ROOT/'TestResults/balance/tower-affinity-nomination-pilot-01-20260925'
OUTPUT=ROOT/'TestResults/affinity-nomination-pilot-verification-20260925'


def run(pin):
    cycle.require(not OUTPUT.exists() and cycle.sha(SOURCE/'closeout.json')==pin,'Verification already attempted or changed closeout')
    launch=cycle.read(SOURCE/'launch.json'); complete=cycle.read(SOURCE/'completion.json'); closeout=cycle.read(SOURCE/'closeout.json')
    start=dt.datetime.fromisoformat(launch['startedAt'])
    deadline=min(dt.datetime.fromisoformat(launch['deadline']),start+dt.timedelta(seconds=complete['nativeSeconds']+1800))-dt.timedelta(seconds=5)
    remaining=(deadline-dt.datetime.now(dt.timezone.utc)).total_seconds()
    cycle.require(remaining>10 and closeout['auditBytes']<536870912-67108864,'Original audit allowance exhausted')
    OUTPUT.mkdir(); started=time.monotonic()
    cycle.write(OUTPUT/'declaration.json',dict(closeoutSha256=pin,originalAuditDeadline=deadline.isoformat(),
        maximumSeconds=min(450,remaining),maximumBytes=67108864,attempts=1,newFights=0,newValues=0))
    try:
        receipt=cycle.checked_process(OUTPUT,'verification',['dotnet',str(SOURCE/'executable/BalanceHarness.dll'),
            'tower-proposal-study-verify',str(SOURCE),pin],started+min(445,remaining))
        result=cycle.read(OUTPUT/'verification.log')
        cycle.require(result==cycle.read(SOURCE/'result.json') and result['status']=='Verified','Verified result changed')
        cycle.require(dt.datetime.now(dt.timezone.utc)<deadline,'Original audit deadline exceeded')
        cycle.write(OUTPUT/'completion.json',dict(status='PostPublicationVerified',seconds=time.monotonic()-started,
            closeoutSha256=pin,originalAuditDeadline=deadline.isoformat(),newFights=0,newValues=0,
            decision=result['decision'],processSeconds=receipt['seconds']))
    except BaseException as error:
        cycle.write(OUTPUT/'failure.json',dict(reason=str(error),retryPermitted=False)); raise
    finally: cycle.write(OUTPUT/'files.json',cycle.inventory(OUTPUT))
    print(json.dumps(dict(status='PostPublicationVerified',manifestSha256=cycle.sha(OUTPUT/'files.json'),decision=result['decision'])))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--pin',required=True)
    run(parser.parse_args().pin)
