"""Register and verify a read-only necessary gate for corrected-auditor recovery.

No native executable, fixture, entropy source or campaign is invoked. An incomplete
receipt is a prospective denominator ceiling, never a fabricated completed sample.
"""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-loadout-placement-recovery-protocol-v1'
PLAN = 'Balance Harness/Tower-Loadout-Placement-Recovery-Protocol.json'
PLAN_PIN = 'fa11f580a8a3cbe551081c08096114fc91f748c425a7f9822bba7d2adea9fa1a'
PRIOR = 'TestResults/loadout-placement-matched-fixture-handoff-20260924.json'
PRIOR_PIN = 'ab2f67fd0f195dd5f8dd0c00f1be35828efc3fe0f5189ac90dcefc817e6ac0fb'
FAILED = 'TestResults/loadout-placement-matched-owned-20260924'
FAILED_PIN = '7dd1df4ab8ea9d24c53d50cac4137bdb59351a56110e9642cd791474e3f02605'
BASELINE = 'tower-proposal-owned-fixture-matched-baseline/result/'
REGISTRATION = 'TestResults/loadout-placement-recovery-protocol-20260924'
PREVIOUS_VERIFICATION = 'TestResults/loadout-placement-matched-fixture-implementation-20260924'
SECONDS, BYTES = 180, 64*1048576


def module(name, path):
    spec=importlib.util.spec_from_file_location(name,path)
    value=importlib.util.module_from_spec(spec); spec.loader.exec_module(value); return value


matched = module('recovery_matched_rules',ROOT/'build/test-loadout-placement-matched-owned.py')
pre = matched.pre
read, write, sha, require = matched.read, matched.write, matched.sha, matched.require


def native_ceilings(receipt):
    require(receipt['version'] == 'tower-affinity-allied-action-comparison-v1'
        and receipt['status'] == 'MeasuredPendingAudits' and receipt['fights'] == 17280
        and receipt['newAuditFights'] == 0, 'Not the retained completed-native, incomplete-study receipt')
    return dict(nativeSeconds=pre.positive(receipt['measuredSeconds'],'native seconds ceiling'),
        nativeBytes=pre.positive(receipt['observedBytes'],'native bytes ceiling'))


def rounded_limits(floors):
    rounded={k:math.ceil(pre.positive(floors[k],k)) for k in pre.PHASES}
    return rounded,[k for k in pre.PHASES if rounded[k] >= pre.LIMITS[k]]


def lower_bound(inputs, receipt):
    pre.assess(inputs)  # Validate the frozen design, pilot, probe, closeouts and inherited floors.
    ceilings=native_ceilings(receipt)
    placement=matched.phase_costs(inputs['new-costs'],inputs['new-closeout'])
    pilot=matched.phase_costs(inputs['pilot-costs'],inputs['pilot-closeout'])
    ratios={k:max(1,placement[k]/ceilings[k]) if k in ceilings else 1 for k in pre.PHASES}
    scaled={k:2*pilot[k]*(21888/inputs['pilot-result']['fights'])*ratios[k] for k in pre.PHASES}
    inherited=inputs['design']['resources']['inheritedForecastFloors']
    floors={k:max(inherited[k],scaled[k]) for k in pre.PHASES}
    probe=inputs['audit-probe']
    historical_bytes=inputs['probe-previous-forecast']['totalBytes']/probe['inventoryByteRatio']
    storage_floor=floors['nativeBytes']+floors['auditBytes']
    extra=probe['extraInventorySeconds']*max(1,max(1,storage_floor/historical_bytes)/probe['inventoryByteRatio'])
    audit_bound=2*(probe['wholeWorkerSeconds']+sum(probe['independentAuditSeconds'])+extra)+120
    floors['auditSeconds']=max(floors['auditSeconds'],probe['auditPlanningSeconds'],audit_bound)
    rounded,failed=rounded_limits(floors)
    return dict(version=VERSION,status='RecoveryNecessaryGateFailed' if failed else 'RecoveryNecessaryGatePassedOnly',
        interpretation='Necessary resource bound for this prospective recovery rule, not a measured current-runtime forecast or an efficacy result',
        admitted=False,runtimeQualified=False,qualificationStarted=False,recoveryPairStarted=False,
        recoveryPairMayBeRegistered=not failed,qualificationAllowed=False,scientificLaunches=0,
        actualCombat=0,productionEntropyDraws=0,newScientificReservations=0,newTimingSamples=0,
        originalExperimentStillFailed=True,completedBaselineAvailable=False,qualifiedCurrentForecast=None,
        retainedNativeDenominatorCeilings=ceilings,originalPlacementPhaseFloors=placement,
        minimumRatios=ratios,auditRatiosAreBoundsNotObservations=True,completedPilotPhaseCosts=pilot,
        completedPilotFights=inputs['pilot-result']['fights'],scaledPilotLowerBounds=scaled,
        inheritedFloors=inherited,unroundedNecessaryFloors=floors,roundedNecessaryFloors=rounded,
        exceededPartitions=failed,limits=pre.LIMITS,minimumProjectedRetainedBytes=storage_floor,
        frozenProbeAuditSecondsLowerBound=audit_bound,storageAppliedBeforeAuditScaling=True,
        timingMargin=2,publicationReserveSeconds=120,
        next='Close this recovery route under unchanged rules: another timing pair cannot lower the necessary bound. Any future implementation or scope change needs a separate prospective design and justified resource model, with all historical evidence and charges retained.'
            if failed else 'Register at most one corrected-auditor matched pair under the prospective recovery rules. Completion and guarded runtime qualification remain required; this gate grants no admission.')


def recovery_forecast(inputs, receipt, completed_baseline, completed_placement):
    """Pure prospective arithmetic for validation; callers must supply complete phase costs.

    This function does not read or publish receipts and cannot execute a recovery.
    """
    ceilings=native_ceilings(receipt)
    baseline=dict(completed_baseline)
    for k in pre.PHASES:
        pre.positive(baseline[k],k); pre.positive(completed_placement[k],k)
    for k,ceiling in ceilings.items(): baseline[k]=min(ceiling,baseline[k])
    return matched.forecast(inputs,baseline,completed_placement)


def inputs_at(root):
    return {k:read(root/'precheck'/p) for k,p in read(root/'precheck/inputs.json').items()}


def validate_retained(root):
    require(sha(root/'protocol.json') == PLAN_PIN and sha(root/'prior-handoff.json') == PRIOR_PIN,
        'Changed prospective protocol or handoff')
    protocol=read(root/'protocol.json'); prior=read(root/'prior-handoff.json')
    require(sha(root/'failed-files.json') == FAILED_PIN,'Changed original failed manifest')
    require(sha(root/'original-plan.json') == protocol['originalPlanSha256'],'Changed original experiment')
    failed_files=read(root/'failed-files.json')
    for name,pin in read(root/'consumed-failed-files.json').items():
        require(failed_files[name] == pin and sha(root/'failed'/name) == pin,'Changed failed input: '+name)
    require(not any(BASELINE+p in failed_files for p in ('completion.json','closeout.json','result.json'))
        and 'tower-proposal-owned-fixture-matched-placement/measurement-start.json' not in failed_files,
        'First pair is no longer the retained incomplete baseline with unstarted candidate')
    declaration=read(root/'failed/declaration.json'); failure=read(root/'failed/failure.json')
    require(declaration['chargedMaximumSeconds'] == 24420 and declaration['chargedMaximumBytes'] == 13019119616
        and declaration['retryPermitted'] is False and failure['retryPermitted'] is False
        and failure['reason'] == 'Owned completion failed: Independent audit failed','Changed failed experiment accounting')
    receipt=read(root/'failed'/BASELINE/'native-receipt.json'); native_ceilings(receipt)
    require(receipt['requestFileHash'] == failed_files[BASELINE+'request.json']
        and receipt['studyHash'] == failed_files[BASELINE+'study/files.json'],'Changed native receipt binding')
    for name in ('native-process.json','native-audit-process.json'):
        process=read(root/'failed'/BASELINE/name)
        require(process['exitCode'] == 0 and not process['timedOut'] and process['activeProcesses'] == 0,'Native work or reconstruction did not complete')
    process=read(root/'failed'/BASELINE/'independent-audit-process.json')
    require(process['exitCode'] == 1 and not process['timedOut'] and process['activeProcesses'] == 0,'Changed independent audit failure')
    require(read(root/'failed'/BASELINE/'native-audit.log') == read(root/'failed'/BASELINE/'provisional-result.json'),
        'Native reconstruction differs from saved result')
    pre.verify(root/'precheck')
    require(sha(root/'precheck/files.json') == prior['preservedHistoricalPins'][matched.PRECHECK+'/files.json'],'Changed authenticated cost evidence')
    inputs=inputs_at(root)
    require(read(root/'failed'/BASELINE/'source/context.json') == inputs['new-context'], 'Changed matched physical context')
    require(sha(root/'corrected-auditor.py') == protocol['correctedAuditorSha256']
        == prior['currentSourceHashes']['Balance Harness/analysis/audit-proposal-affinity-study.py'],'Changed verified corrected auditor')
    require(read(root/'failed'/BASELINE/'source/runtime.json')['BalanceHarness.dll'] == protocol['harnessSha256'],'Changed production harness')
    require(sha(root/'previous-verification-files.json') == prior['verificationManifestSha256'],'Changed previous verification manifest')
    verified=read(root/'previous-verification-files.json')
    for name in ('verification.json','common-v5-retained-comparison.json'):
        require(sha(root/'previous-verification'/name) == verified[name],'Changed prior functional proof')
    proof=read(root/'previous-verification/common-v5-retained-comparison.json')
    require(proof['status'] == 'AllTwelveCommonV5TrajectoriesByteIdentical' and len(proof['roots']) == 12,'Missing common-v5 proof')
    original_files=read(root/'precheck/evidence'/pre.NEW/'files.json')
    for number in range(1,13):
        a=BASELINE+f'search/root-{number:02}/candidate/'; b=f'search/root-{number:02}/control/'
        left={p[len(a):]:pin for p,pin in failed_files.items() if p.startswith(a)}
        right={p[len(b):]:pin for p,pin in original_files.items() if p.startswith(b)}
        require(left and left == right,'Changed retained common-v5 trajectory manifest')
    return inputs,receipt


def verify(root, expected_manifest):
    require(sha(root/'files.json') == expected_manifest,'Changed externally pinned recovery registration')
    files=read(root/'files.json')
    require({p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()} == set(files)|{'files.json'},'Changed registration membership')
    for name,pin in files.items():
        path=(root/name).resolve()
        require(path.is_relative_to(root.resolve()) and sha(path) == pin,'Changed retained registration file')
    inputs,receipt=validate_retained(root)
    expected=lower_bound(inputs,receipt)
    require(read(root/'assessment.json') == expected,'Changed necessary-gate arithmetic')
    return expected


def register(output):
    require(output == (ROOT/REGISTRATION).resolve() and not output.exists(),'Use the single new registration path; no overwrite or replacement')
    require(sha(ROOT/PLAN) == PLAN_PIN and sha(ROOT/PRIOR) == PRIOR_PIN,'Changed protocol or handoff')
    output.mkdir(); started=time.monotonic()
    sources=[Path(__file__),ROOT/'build/test-loadout-placement-matched-owned.py',
        ROOT/'Balance Harness/analysis/loadout-placement-resource-precheck.py']
    write(output/'declaration.json',dict(version=VERSION,maximumSeconds=SECONDS,maximumBytes=BYTES,
        chargedSeconds=SECONDS,chargedBytes=BYTES,fullAllowanceChargedAtStart=True,
        protocolSha256=PLAN_PIN,priorHandoffSha256=PRIOR_PIN,failedPairManifestSha256=FAILED_PIN,
        sourceHashes={p.relative_to(ROOT).as_posix():sha(p) for p in sources},nativePreparation=False,
        newTimingSamples=0,actualCombat=0,productionEntropyDraws=0,qualificationStarted=False))
    def check():
        require(time.monotonic()-started < SECONDS and sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) < BYTES,
            'Read-only recovery gate allowance exceeded')
    try:
        prior=read(ROOT/PRIOR)
        for group in ('preservedHistoricalPins','currentSourceHashes'):
            for name,pin in prior[group].items(): require(sha(ROOT/name) == pin,'Changed inherited evidence or implementation: '+name); check()
        matched.verify(ROOT/FAILED,FAILED_PIN); check()
        for source,target in [(ROOT/PLAN,'protocol.json'),(ROOT/PRIOR,'prior-handoff.json'),
            (ROOT/matched.PLAN,'original-plan.json'),(ROOT/FAILED/'files.json','failed-files.json'),
            (ROOT/'Balance Harness/analysis/audit-proposal-affinity-study.py','corrected-auditor.py'),
            (ROOT/PREVIOUS_VERIFICATION/'files.json','previous-verification-files.json')]:
            shutil.copyfile(source,output/target)
        shutil.copytree(ROOT/matched.PRECHECK,output/'precheck')
        for name in ('verification.json','common-v5-retained-comparison.json'):
            path=output/'previous-verification'/name; path.parent.mkdir(exist_ok=True); shutil.copyfile(ROOT/PREVIOUS_VERIFICATION/name,path)
        failed_files=read(output/'failed-files.json'); consumed={}
        names=['declaration.json','failure.json','preflight.json','tower-proposal-owned-fixture-matched-baseline/measurement-start.json']
        names += [BASELINE+p for p in ('request.json','native-receipt.json','native-process.json','native-audit-process.json',
            'native-audit.log','independent-audit-process.json','independent-audit.log','provisional-result.json',
            'failure.json','source/context.json','source/plan.json','source/runtime.json','source/settings.json')]
        for name in names:
            require(sha(ROOT/FAILED/name) == failed_files[name],'Changed consumed failed evidence')
            path=output/'failed'/name; path.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(ROOT/FAILED/name,path)
            consumed[name]=failed_files[name]; check()
        write(output/'consumed-failed-files.json',consumed)
        for source in sources:
            path=output/'implementation'/source.relative_to(ROOT); path.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(source,path)
        inputs,receipt=validate_retained(output)
        assessment=lower_bound(inputs,receipt); write(output/'assessment.json',assessment); check()
        write(output/'completion.json',dict(status='RecoveryNecessaryGateCompleteNoExecution',secondsBeforeSealing=time.monotonic()-started,
            chargedSeconds=SECONDS,chargedBytes=BYTES,inheritedPinsAuthenticated=len(prior['preservedHistoricalPins']),
            nativePreparation=False,newTimingSamples=0,actualCombat=0,productionEntropyDraws=0,
            recoveryPairStarted=False,qualificationStarted=False,admitted=False))
        for group in ('preservedHistoricalPins','currentSourceHashes'):
            for name,pin in prior[group].items(): require(sha(ROOT/name) == pin,'Source changed during registration')
        check()
    except BaseException as error:
        write(output/'failure.json',dict(reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES,
            newTimingSamples=0,actualCombat=0,qualificationStarted=False,admitted=False))
        raise
    finally:
        matched.seal(output)
    return verify(output,sha(output/'files.json'))


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('action',choices=('register','verify'))
    parser.add_argument('--output',required=True,type=Path); parser.add_argument('--expected-manifest-sha256')
    args=parser.parse_args()
    result=register(args.output.resolve()) if args.action=='register' else verify(args.output.resolve(),args.expected_manifest_sha256)
    print(json.dumps(result,indent=2))
