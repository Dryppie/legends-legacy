"""New admission from an immutable qualified technical failure; never resamples timing.

The old directory stays failed. A new declared package, charge, live-history checks
and native input check bind the same qualified runtime to a fresh admitted request.
"""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('qualified_resource_admission', Path(__file__).with_name('prepare-proposal-affinity-resource-admission.py'))
a = importlib.util.module_from_spec(spec); spec.loader.exec_module(a)
PACKAGE = ROOT/'TestResults/proposal-affinity-resource-admission-repaired-20260923'
REVIEW = ROOT/'TestResults/proposal-resource-admission-closeout-20260923'
REVIEW_PIN = 'f04bef574dfc24c9e6b41e45f8f900621b8b9bd08dd42ba95b837fbe6b8f5cc0'


def qualified():
    a.base.authenticate(REVIEW, REVIEW_PIN)
    proof = a.read(REVIEW/'verification.json')
    a.require(proof['status'] == 'FailedResourceAdmissionQualifiedNoReservation' and not proof['admitted']
              and proof['forecastUsesSameFrozenTimings'], 'Unqualified preceding failure')
    files = a.read(REVIEW/'failed-attempt-files.json')
    a.require(a.inventory(a.PACKAGE) == files and a.sha(a.PACKAGE/'failure.json') == proof['failureSha256'], 'Changed qualified failed package')
    return proof, files


def rebind(q, package):
    result = dict(q)
    for name in ('plan','context','settings','history','runtime','auditor'):
        path = package/('auditor.py' if name == 'auditor' else name+'.json')
        a.require(a.sha(path) == q[name]['sha256'], 'Changed qualified source while rebinding')
        result[name] = dict(path=str(path), sha256=q[name]['sha256'])
    result['contentRoot'] = str(package/'content')
    return result


def prepare(args):
    a.require(os.name == 'nt' and a.sha(args.declaration) == args.pin, 'Changed external repair declaration')
    d = a.read(args.declaration); a.validate_declaration(d, PACKAGE)
    a.require(d['qualifiedAttemptReviewSha256'] == REVIEW_PIN and not PACKAGE.exists() and not a.OUTPUT.exists(), 'No repeat or existing output')
    started = time.monotonic(); PACKAGE.mkdir()
    a.save(PACKAGE/'admission-charge.json', dict(version=a.VERSION,chargedSeconds=a.SECONDS,chargedBytes=a.BYTES,
        scope='New admission using sealed qualification; entire allowance charged including failure, no resampling, combat or allocation.'))
    watchdog = threading.Timer(a.SECONDS,lambda:os._exit(124)); watchdog.daemon=True; watchdog.start()
    process_owner = a.module('qualified_admission_owner',ROOT/'build/bounded_windows_process.py')
    def check():
        a.require(time.monotonic()-started < a.SECONDS-5 and not a.OUTPUT.exists(), 'Admission deadline or scientific output')
        a.require(a.owner.storage_bytes(PACKAGE) < a.BYTES-4*1048576, 'Admission storage exhausted')
    try:
        with a.owner.writer_lease(a.OUTPUT.parent/'complete-family-allocation'), a.owner.writer_lease(a.OUTPUT):
            proof, files = qualified(); check()
            a.require(d['harnessFiles']['BalanceHarness.dll'] == proof['harnessSha256'], 'Changed qualified runtime')
            renames = {'request.json':'qualified-request.json','declaration.json':'qualification-declaration.json',
                'failure.json':'preceding-failure.json','admission-charge.json':'preceding-admission-charge.json',
                'preceding-charges.json':'qualification-preceding-charges.json'}
            for name in ('native-check.json','native-check.log','native-check-process.json'): renames[name]='qualified-'+name
            for name,pin in files.items():
                source = a.PACKAGE/name; target = PACKAGE/renames.get(name,name)
                a.require(target.resolve().is_relative_to(PACKAGE), 'Invalid copy path')
                target.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(source,target)
                a.require(a.sha(target) == pin, 'Changed qualification copy')
            source_copies = [(args.declaration,'declaration.json'),(Path(__file__),'admit-qualified.py'),
                (Path(a.__file__),'resource-admission-library.py'),(REVIEW/'files.json','qualified-review-files.json'),
                (REVIEW/'verification.json','qualified-review.json'),(REVIEW/'failed-attempt-files.json','qualified-attempt-files.json')]
            for path,name in source_copies: shutil.copyfile(path,PACKAGE/name)
            a.save(PACKAGE/'preceding-charges.json',dict(chargedSeconds=proof['totalRecordedEngineeringChargedSeconds'],
                chargedBytes=proof['totalRecordedEngineeringChargedBytes'],qualifiedReviewManifestSha256=REVIEW_PIN,
                scope='All recorded failed admissions and native probe/review allowances; development tests remain separate.'))
            q = rebind(a.read(PACKAGE/'qualified-request.json'),PACKAGE)
            a.require(q['outputRoot'] == str(a.OUTPUT) and q['resourceEnvelope'] == a.owner.RESOURCE_V2, 'Changed scientific target or envelope')
            history_files, values = a.base.history(a.OUTPUT.parent,q)
            a.require(history_files == q['requiredHistory'] and values == a.read(PACKAGE/'history.json'),
                      'History changed since qualification; no silent rebinding')
            a.save(PACKAGE/'request.json',q); check()
            process = process_owner.run(['dotnet',str(PACKAGE/'runtime/BalanceHarness.dll'),'tower-proposal-study-check',str(PACKAGE/'request.json')],
                str(ROOT),str(PACKAGE/'native-check.log'),started+a.SECONDS-5,cleanup_seconds=1,check=check)
            a.save(PACKAGE/'native-check-process.json',process); a.base.process_ok(process); check()
            native = a.read(PACKAGE/'native-check.log'); a.save(PACKAGE/'native-check.json',native)
            a.require(native == a.read(PACKAGE/'qualified-native-check.json'), 'Changed native input check')
            previous, probe, historical_bytes = a.retained_forecast_inputs()
            estimate = a.forecast(previous,probe,historical_bytes,a.read(PACKAGE/'original-formula-forecast.json'))
            a.require(estimate == proof['correctedLookupForecast'], 'Changed frozen planning evidence')
            a.save(PACKAGE/'resource-forecast.json',estimate); a.base.admit_forecast(estimate)
            a.require(a.base.history(a.OUTPUT.parent,q) == (history_files,values), 'History changed during repaired admission')
            a.base.validate_plan(a.read(PACKAGE/'prospective-plan.json'),a.read(PACKAGE/'plan.json'))
            a.require(a.sha(a.base.PLAN) == a.base.PLAN_PIN, 'Original scientific plan changed')
            candidate = a.read(PACKAGE/'candidate-qualification.json')
            a.base.validate_qualification(a.read(PACKAGE/'baseline-qualification.json'),candidate,a.read(PACKAGE/'baseline-projections.json'),
                a.read(PACKAGE/'candidate-projections.json'),a.read(PACKAGE/'capture-context.json'),a.read(PACKAGE/'preview-request.json')['context'],
                d['harnessFiles']['BalanceHarness.dll'])
            a.require(a.inventory(PACKAGE/'runtime') == a.read(PACKAGE/'runtime.json')
                and a.inventory(PACKAGE/'source') == a.read(PACKAGE/'compiled-source-files.json'), 'Changed copied runtime/source')
            a.validate_declaration(d,PACKAGE)
            for path,name in source_copies: a.require(a.sha(path) == a.sha(PACKAGE/name), 'Changed admission source')
            for name,pin in files.items():
                a.require(a.sha(PACKAGE/renames.get(name,name)) == pin, 'Qualified evidence changed during admission')
            a.require(qualified()[0] == proof, 'Preceding failure changed'); check()
            prior = a.read(PACKAGE/'preceding-charges.json')
            receipt = dict(version=a.owner.VERSION,status='ProposalStudyAdmittedNoReservation',resourceEnvelope=a.owner.RESOURCE_V2,
                requestSha256=a.sha(PACKAGE/'request.json'),originalPlanSha256=a.base.PLAN_PIN,qualifiedPlanSha256=a.sha(PACKAGE/'plan.json'),
                executionHash=candidate['executionHash'],historicalValues=len(values),historyFiles=len(history_files),
                qualificationNativePreparations=144,newAdmissionNativePreparations=0,qualifiedReviewManifestSha256=REVIEW_PIN,
                fights=0,newValues=0,scientificLaunches=0,chargedSeconds=a.SECONDS,chargedBytes=a.BYTES,
                precedingChargedSeconds=prior['chargedSeconds'],precedingChargedBytes=prior['chargedBytes'],
                scientificMaximumSeconds=10800,scientificMaximumBytes=6442450944,
                cumulativeMaximumSeconds=prior['chargedSeconds']+a.SECONDS+10800,
                cumulativeMaximumBytes=prior['chargedBytes']+a.BYTES+6442450944)
            a.save(PACKAGE/'admission.json',receipt)
            a.save(PACKAGE/'completion.json',dict(status='AdmissionCompleteNoReservation',measuredSecondsBeforeSealing=time.monotonic()-started,
                retainedBytesBeforeSealing=a.owner.storage_bytes(PACKAGE),chargedSeconds=a.SECONDS,chargedBytes=a.BYTES,fights=0,newValues=0,scientificLaunches=0))
            a.save(PACKAGE/'files.json',a.inventory(PACKAGE)); check()
            retained = a.module('qualified_retained_launcher',PACKAGE/'run-proposal-affinity-study.py')
            retained.validate_admission(PACKAGE/'request.json',PACKAGE/'runtime/BalanceHarness.dll',a.sha(PACKAGE/'files.json')); check()
            print(json.dumps(dict(status=receipt['status'],manifestSha256=a.sha(PACKAGE/'files.json'),requestSha256=a.sha(PACKAGE/'request.json'),
                measuredSecondsAfterSealing=time.monotonic()-started,retainedBytes=a.owner.storage_bytes(PACKAGE),resourceForecast=estimate,
                fights=0,newValues=0,scientificLaunches=0),indent=2))
    except BaseException as error:
        if not (PACKAGE/'failure.json').exists():
            a.save(PACKAGE/'failure.json',dict(status='AdmissionFailedNoReservation',reason=str(error),chargedSeconds=a.SECONDS,chargedBytes=a.BYTES,
                seconds=time.monotonic()-started,fights=0,newValues=0,scientificLaunches=0))
        raise
    finally: watchdog.cancel()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--declaration',required=True,type=Path); parser.add_argument('--pin',required=True)
    prepare(parser.parse_args())
