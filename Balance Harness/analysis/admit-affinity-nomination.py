"""Bind current history to the one frozen nomination comparison; never launch it."""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]


def module(name, path):
    spec=importlib.util.spec_from_file_location(name,path)
    value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


cycle=module('nomination_cycle',Path(__file__).with_name('run-affinity-nomination-cycle.py'))
prior=module('nomination_inherited_admission',Path(__file__).with_name('admit-loadout-placement-qualified.py'))
base, owner = prior.base, cycle.owner
read, write, sha, require = cycle.read, cycle.write, cycle.sha, cycle.require
PACKAGE=ROOT/'TestResults/affinity-nomination-admission-20260925'
OUTPUT=ROOT/'TestResults/balance/tower-affinity-nomination-pilot-01-20260925'
PREVIOUS=ROOT/'TestResults/balance/tower-loadout-placement-pilot-01-20260925'
DECLARATION=ROOT/'Balance Harness/Tower-Affinity-Nomination-Admission-Declaration.json'


def register():
    require(not DECLARATION.exists() and not PACKAGE.exists() and not OUTPUT.exists(), 'Cycle already registered or started')
    require(read(cycle.QUALIFIED/'completion.json')['status']=='RuntimeEquivalentNoReservation'
        and read(cycle.RECOVERED/'completion.json')['status']=='NominationMaximumVerified', 'Engineering checks incomplete')
    maximum=read(cycle.RECOVERED/'maximum-workload.json')
    forecast=read(prior.pair.OUTPUT/'forecast.json')
    prior.check_forecast(forecast,maximum)
    files=read(PREVIOUS/'files.json')
    require(sha(PREVIOUS/'request.json')==files['request.json'], 'Previous history contract changed')
    sources=[Path(__file__),Path(cycle.__file__),cycle.DESIGN,prior.HELPER,Path(base.__file__),base.HISTORY_HELPER,
        Path(owner.__file__),ROOT/'build/bounded_windows_process.py',ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py',
        Path(prior.__file__)]
    write(DECLARATION,dict(version='tower-affinity-nomination-admission-v1',
        qualificationManifestSha256=sha(cycle.QUALIFIED/'files.json'), maximumManifestSha256=sha(cycle.RECOVERED/'files.json'),
        inheritedForecastSha256=sha(prior.pair.OUTPUT/'forecast.json'), previousRequestSha256=sha(PREVIOUS/'request.json'),
        sourcePins={p.relative_to(ROOT).as_posix():sha(p) for p in sources},
        attempts=1,retryPermitted=False,chargedSeconds=900,chargedBytes=1073741824,
        scientificAttempts=1,maximumFights=21888,requiredFreshValues=4380,scientificSeconds=10800,scientificBytes=6442450944,
        design='Original affinity creation and identical training. Only generated finalists can be experimental challengers. Exact validation unchanged.',
        resourceRule='No discount: retain every qualified placement floor, scale the complete current synthetic workload and postverification upward for history growth, factor two plus publication reserve. Reject any partition at its limit.',
        actualCombat=0,productionEntropyDraws=0,newReservations=0))
    print(sha(DECLARATION))


def prepare(pin):
    require(sha(DECLARATION)==pin and not PACKAGE.exists() and not OUTPUT.exists(), 'Changed declaration or repeated admission')
    declaration=read(DECLARATION)
    for name, expected in declaration['sourcePins'].items(): require(sha(ROOT/name)==expected,'Changed registered source: '+name)
    require(sha(cycle.QUALIFIED/'files.json')==declaration['qualificationManifestSha256']
        and sha(cycle.RECOVERED/'files.json')==declaration['maximumManifestSha256']
        and sha(prior.pair.OUTPUT/'forecast.json')==declaration['inheritedForecastSha256'],'Changed evidence manifest')
    started=time.monotonic(); PACKAGE.mkdir()
    write(PACKAGE/'charge.json',dict(chargedSeconds=900,chargedBytes=1073741824,fullAllowanceChargedAtStart=True,declarationSha256=pin))
    def check():
        require(time.monotonic()-started<895 and owner.storage_bytes(PACKAGE)<1073741824-4194304 and not OUTPUT.exists(), 'Admission budget or output changed')
    try:
        with owner.writer_lease(OUTPUT.parent/'complete-family-allocation'),owner.writer_lease(OUTPUT):
            shutil.copyfile(DECLARATION,PACKAGE/'declaration.json')
            old=read(PREVIOUS/'request.json')
            require(sha(PREVIOUS/'request.json')==declaration['previousRequestSha256'],'Changed historical request')
            history_files,history=base.history(OUTPUT.parent,old); check()
            qualified=cycle.QUALIFIED; files=read(qualified/'files.json')
            for name,expected in files.items():
                if name in ('runtime-context.json','runtime-plan.json','settings.json','runtime.json') or name.startswith(('runtime/','content/')):
                    require(sha(qualified/name)==expected,'Changed qualified input: '+name)
            context=read(qualified/'runtime-context.json'); legacy=base.legacy_values(context)
            declared=legacy|set(context['scope']['generation']['seeds'])|{context['rootSeed']}
            declared.update(v for ref in context['scope']['references'] for v in ref['scenario']['seeds'])
            require(declared<=set(history),'Missing declared context history')
            context['scope']['excludedCombatSeeds']=sorted(set(history)-legacy)
            write(PACKAGE/'context-draft.json',context); write(PACKAGE/'history.json',history)
            for source,name in [(qualified/'runtime-plan.json','qualified-plan.json'),(qualified/'settings.json','settings.json'),
                (qualified/'runtime.json','runtime.json'),(prior.HELPER,'bind.ps1'),
                (ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py','auditor.py'),
                (Path(owner.__file__),'run-proposal-affinity-study.py'),(ROOT/'build/bounded_windows_process.py','bounded_windows_process.py')]:
                shutil.copyfile(source,PACKAGE/name)
            for directory in ('runtime','content'): shutil.copytree(qualified/directory,PACKAGE/directory)
            cycle.checked_process(PACKAGE,'binding',cycle.powershell(PACKAGE,PACKAGE/'bind.ps1'),started+895,check)
            plan=read(PACKAGE/'plan.json'); expected=read(qualified/'runtime-plan.json')
            require(plan==dict(expected,scopeHash=plan['scopeHash']) and read(PACKAGE/'context.json')==context,'Changed scientific design')
            fixture_context=cycle.FIXTURE/'tower-proposal-owned-fixture-nomination/package/context.json'
            maximum=read(cycle.RECOVERED/'maximum-workload.json')
            require(sha(cycle.RECOVERED/'maximum-workload.json')==read(cycle.RECOVERED/'files.json')['maximum-workload.json'],'Changed maximum costs')
            ratio=max(1,(PACKAGE/'context.json').stat().st_size/fixture_context.stat().st_size)
            forecast=read(prior.pair.OUTPUT/'forecast.json'); bounds=prior.check_forecast(forecast,maximum,ratio)
            write(PACKAGE/'resource-forecast.json',dict(inheritedForecastSha256=declaration['inheritedForecastSha256'],
                maximumManifestSha256=declaration['maximumManifestSha256'],metadataByteRatio=ratio,roundedPhaseFloors=bounds,
                limits=forecast['limits'],admitted=True))
            q=dict(version=plan['version'],contentRoot=str(PACKAGE/'content'),registryRoot=str(OUTPUT.parent),outputRoot=str(OUTPUT),
                requiredHistory=history_files,pendingHistoryRecoveries=old['pendingHistoryRecoveries'],recoveryReceiptHashes=old['recoveryReceiptHashes'],
                resourceEnvelope=owner.RESOURCE_V2)
            for name in ('plan','context','settings','history','runtime','auditor'):
                path=PACKAGE/('auditor.py' if name=='auditor' else name+'.json')
                q[name]=dict(path=str(path),sha256=sha(path))
            write(PACKAGE/'request.json',q)
            cycle.checked_process(PACKAGE,'native-check',['dotnet',str(PACKAGE/'runtime/BalanceHarness.dll'),
                'tower-proposal-study-check',str(PACKAGE/'request.json')],started+895,check)
            native=read(PACKAGE/'native-check.log')
            require(native['status']=='InputsVerifiedNoReservation' and native['fights']==native['newValues']==0,'Native inspection failed')
            require(base.history(OUTPUT.parent,q)==(history_files,history),'History changed during admission'); check()
            write(PACKAGE/'admission.json',dict(version=plan['version'],status='ProposalStudyAdmittedNoReservation',
                requestSha256=sha(PACKAGE/'request.json'),resourceEnvelope=owner.RESOURCE_V2,fights=0,newValues=0,
                qualificationManifestSha256=declaration['qualificationManifestSha256'],maximumManifestSha256=declaration['maximumManifestSha256'],
                resourceForecastSha256=sha(PACKAGE/'resource-forecast.json')))
            write(PACKAGE/'completion.json',dict(status='AdmittedNoReservation',seconds=time.monotonic()-started,
                historyValues=len(history),historyFiles=len(history_files),actualCombat=0,newValues=0))
    except BaseException as error:
        write(PACKAGE/'failure.json',dict(reason=str(error),retryPermitted=False)); raise
    finally: write(PACKAGE/'files.json',cycle.inventory(PACKAGE))
    owner.validate_admission(PACKAGE/'request.json',PACKAGE/'runtime/BalanceHarness.dll',sha(PACKAGE/'files.json'))
    print(json.dumps(dict(status='AdmittedNoReservation',manifestSha256=sha(PACKAGE/'files.json'))))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('action',choices=['register','prepare']); parser.add_argument('--pin')
    args=parser.parse_args()
    if args.action=='register': register()
    else: prepare(args.pin)
