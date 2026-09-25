"""Authenticate retained resource evidence before qualification; never admit or execute."""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import shutil
import time

ROOT = Path(__file__).resolve().parents[2]
VERSION = 'tower-loadout-placement-resource-precheck-v1'
PRIOR = 'TestResults/loadout-placement-comparison-implementation-handoff-20260924.json'
PRIOR_PIN = '5cdb69339875eb84fc7861d0c676476c1200b97147ab4df9075fe19193302b20'
DESIGN = 'Balance Harness/Tower-Loadout-Placement-Comparison-Design.json'
OLD = 'TestResults/tower-proposal-owned-fixture-allied-action-20260924/result'
NEW = 'TestResults/tower-proposal-owned-fixture-loadout-placement-20260924/result'
PILOT = 'TestResults/balance/tower-affinity-allied-action-pilot-01-20260924'
FLOOR = 'TestResults/affinity-allied-action-admission-20260924'
PROBE = 'TestResults/proposal-native-audit-cost-probe-20260923'
SECONDS, BYTES = 180, 64*1048576
PHASES = ('nativeSeconds','auditSeconds','nativeBytes','auditBytes')
LIMITS = dict(nativeSeconds=9000,auditSeconds=1800,nativeBytes=5905580032,auditBytes=536870912)


def require(ok, message):
    if not ok: raise ValueError(message)


def sha(path):
    with path.open('rb') as stream: return hashlib.file_digest(stream,'sha256').hexdigest()


def read(path):
    def unique(pairs):
        result = {}
        for key,value in pairs:
            require(key not in result,'Duplicate JSON key'); result[key] = value
        return result
    return json.loads(path.read_text(encoding='utf-8-sig'),object_pairs_hook=unique,
        parse_constant=lambda _: require(False,'Nonfinite JSON constant'))


def write(path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    with path.open('x',encoding='utf-8',newline='\n') as stream:
        json.dump(value,stream,indent=2,allow_nan=False); stream.write('\n')


def positive(value, label):
    require(type(value) in (int,float) and math.isfinite(value) and value > 0,'Invalid positive cost: '+label)
    return value


def source_manifest_pin(directory, pins, authenticated_inputs):
    manifest=directory+'/files.json'
    if manifest in pins: return pins[manifest]
    # This older probe is transitively pinned by the authenticated admission forecast.
    require(directory==PROBE,'Missing source manifest pin: '+manifest)
    pin=authenticated_inputs['inherited-forecast']['resourceProbeManifestSha256']
    require(isinstance(pin,str) and len(pin)==64 and all(c in '0123456789abcdef' for c in pin),'Invalid inherited probe pin')
    return pin


def physical_context(context):
    value = copy.deepcopy(context)
    # Only bookkeeping and the intentionally contrasted harness identity may differ.
    value.pop('rootSeed',None)
    scope = value['scope']
    for key in ('id','startsAt','executionHash','excludedCombatSeeds'): scope.pop(key,None)
    scope['generation']['seeds'] = []
    return value


def fixture_shape(context):
    scope = context['scope']
    return dict(partySize=scope['requiredPartySize'],essenceSlots=scope['budget']['essenceSlots'],
        templateActors=len(scope['contexts'][0]['characterTemplates']))


def validate_costs(costs, version):
    require(costs['version'] == version and costs['status'] == 'Complete' and costs['retries'] == 0,'Incomplete or wrong cost receipt')
    for key in PHASES: positive(costs[key],key)
    require(math.isclose(costs['seconds'],costs['nativeSeconds']+costs['auditSeconds'],abs_tol=1e-6)
        and costs['observedBytes'] == costs['nativeBytes']+costs['auditBytes'],'Inconsistent phase totals')


def assess(inputs):
    design, old, new, pilot = (inputs[k] for k in ('design','old-costs','new-costs','pilot-costs'))
    resources = design['resources']
    require(resources['envelope']=='tower-proposal-resource-envelope-v2' and resources['margin']==2
        and resources['publicationReserveSeconds']==120 and resources['qualificationMaximumSeconds']==900
        and resources['qualificationMaximumBytes']==1073741824
        and resources['scientificMaximumSeconds']==10800 and resources['scientificMaximumBytes']==6442450944
        and {k:resources[v] for k,v in dict(nativeSeconds='nativeMaximumSeconds',auditSeconds='auditPublicationMaximumSeconds',
            nativeBytes='nativeMaximumBytes',auditBytes='auditPublicationMaximumBytes').items()} == LIMITS,'Changed frozen resource envelope')
    require(design['plannedNativePlan']['maximumFights']==21888,'Changed scientific ceiling')
    validate_costs(old,'tower-affinity-allied-action-comparison-v1')
    validate_costs(new,'tower-loadout-placement-comparison-v1')
    validate_costs(pilot,'tower-affinity-allied-action-comparison-v1')
    result = inputs['pilot-result']
    require(result['version']==pilot['version'] and result['status']=='Verified'
        and type(result['fights']) is int and result['fights'] == result['searchFights']+result['heldoutFights']
        and result['searchFights']==12672 and 12672 < result['fights'] <= 21888,'Changed completed pilot accounting')
    for prefix,costs in [('old',old),('new',new),('pilot',pilot)]:
        close = inputs[prefix+'-closeout']
        require(close['version']==costs['version'] and close['requestHash']==costs['requestFileHash']
            and close['auditSeconds'] >= costs['auditSeconds'] and close['auditBytes'] >= costs['auditBytes']
            and math.isclose(close['measuredSeconds'],costs['nativeSeconds']+close['auditSeconds'],abs_tol=1e-6)
            and close['retainedBytes']==costs['nativeBytes']+close['auditBytes'],'Changed final publication accounting')
    old_shape, new_shape = (fixture_shape(inputs[k+'-context']) for k in ('old','new'))
    require(new_shape == dict(partySize=10,essenceSlots=5,templateActors=10),'Placement cost fixture must cover ten actors and five slots')
    mismatches = [k for k in old_shape if old_shape[k] != new_shape[k]]
    if physical_context(inputs['old-context']) != physical_context(inputs['new-context']): mismatches.append('physicalContext')
    if inputs['old-settings'] != inputs['new-settings']: mismatches.append('settings')
    previous = inputs['inherited-forecast']; inherited = resources['inheritedForecastFloors']
    require(previous['resourceEnvelope']==resources['envelope'] and previous['fits'] is True
        and all(math.ceil(previous[k]) == inherited[k] for k in PHASES),'Changed inherited admission floors')
    ratios = {k:max(1,new[k]/old[k]) for k in PHASES}
    scaled = {k:2*pilot[k]*(21888/result['fights'])*ratios[k] for k in PHASES}
    floors = {k:max(inherited[k],scaled[k]) for k in PHASES}
    # Display the frozen formula as sensitivity only when fixtures do not match.
    # A shape mismatch cannot become an admitted forecast by rounding or normalization.
    probe, historical = inputs['audit-probe'], inputs['probe-previous-forecast']
    require(probe['status']=='AuditPartitionStillNotSupported' and probe['timingMargin']==2
        and probe['publicationReserveSeconds']==120 and probe['inventoryByteRatio'] > 0,'Changed audit probe')
    for key in ('wholeWorkerSeconds','extraInventorySeconds','auditPlanningSeconds'): positive(probe[key],key)
    require(probe['independentAuditSeconds'],'Missing independent audit cost')
    for value in probe['independentAuditSeconds']: positive(value,'independent audit')
    historical_bytes = positive(historical['totalBytes'],'historical bytes')/probe['inventoryByteRatio']
    storage_ratio = max(1,(floors['nativeBytes']+floors['auditBytes'])/historical_bytes)
    extra = probe['extraInventorySeconds']*max(1,storage_ratio/probe['inventoryByteRatio'])
    floors['auditSeconds'] = max(floors['auditSeconds'],probe['auditPlanningSeconds'],
        2*(probe['wholeWorkerSeconds']+sum(probe['independentAuditSeconds'])+extra)+120)
    rounded = {k:math.ceil(v) for k,v in floors.items()}
    failed = [k for k in PHASES if rounded[k] >= LIMITS[k]]
    matched = not mismatches
    return dict(version=VERSION,status='ResourcePrequalificationNotReady' if mismatches or failed else 'ReadyForBoundedQualificationOnly',
        admitted=False,runtimeQualified=False,qualificationStarted=False,scientificLaunches=0,newFights=0,newValues=0,entropyDraws=0,
        oldFixture=old_shape,newFixture=new_shape,comparableFixtures=matched,fixtureMismatches=mismatches,
        inheritedFloors=inherited,limits=LIMITS,qualifiedCurrentForecast=None,
        sensitivity=dict(usableForAdmission=False,interpretation='Unmatched-fixture sensitivity, not a current-runtime forecast or a causal overhead estimate' if not matched
            else 'Retained-evidence lower floors only; guarded current-runtime qualification is still required',
            upwardOnlyRatios=ratios,completedPilotFights=result['fights'],scaledPilotCosts=scaled,
            roundedPhaseFloors=rounded,exceededPartitions=failed,storageAppliedBeforeAuditScaling=True,
            timingMargin=2,publicationReserveSeconds=120),
        requiredNext='Freeze and build equivalent ten-actor/five-slot old/new cost fixtures, with identical content, actor/reference context, settings and literal evaluator rules; declare workload accounting and any normalization prospectively. Retain all prior samples and inherited floors. Then perform the separate guarded runtime qualification; do not enlarge the envelope from this sensitivity result.')


def verify(root):
    manifest=read(root/'files.json')
    require({p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}==set(manifest)|{'files.json'},'Changed precheck membership')
    for name,pin in manifest.items(): require(sha(root/name)==pin,'Changed retained evidence: '+name)
    index=read(root/'inputs.json')
    inputs={key:read(root/path) for key,path in index.items()}
    expected=assess(inputs)
    require(read(root/'assessment.json')==expected,'Changed resource assessment')
    return expected


def create(output):
    require(not output.exists(),'No precheck overwrite or retry')
    require(sha(ROOT/PRIOR)==PRIOR_PIN,'Changed implementation handoff')
    output.mkdir(parents=True); started=time.monotonic()
    write(output/'declaration.json',dict(version=VERSION,maximumSeconds=SECONDS,maximumBytes=BYTES,fullAllowanceChargedAtStart=True,
        priorHandoffSha256=PRIOR_PIN,helperSha256=sha(Path(__file__)),qualificationStarted=False,newFights=0,newValues=0,entropyDraws=0))
    def check():
        require(time.monotonic()-started < SECONDS and sum(p.stat().st_size for p in output.rglob('*') if p.is_file()) < BYTES,'Precheck resource limit')
    try:
        prior=read(ROOT/PRIOR); pins=prior['preservedHistoricalPins']
        for path,pin in pins.items(): require(sha(ROOT/path)==pin,'Inherited evidence changed: '+path); check()
        for path,pin in prior['currentSourceHashes'].items(): require(sha(ROOT/path)==pin,'Implementation changed since verification: '+path)
        shutil.copyfile(ROOT/PRIOR,output/'prior-handoff.json'); shutil.copyfile(Path(__file__),output/'helper.py')
        inputs={}; index={}; consumed={}
        def retain(key,source):
            target=output/'evidence'/source; target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(ROOT/source,target); inputs[key]=read(target); index[key]=target.relative_to(output).as_posix(); consumed[source]=sha(target); check()
        require(sha(ROOT/DESIGN)==pins[DESIGN],'Changed design'); retain('design',DESIGN)
        for directory,fields in [
            (OLD,{'old-costs':'completion.json','old-closeout':'closeout.json','old-context':'source/context.json','old-settings':'source/settings.json'}),
            (NEW,{'new-costs':'completion.json','new-closeout':'closeout.json','new-context':'source/context.json','new-settings':'source/settings.json'}),
            (PILOT,{'pilot-costs':'completion.json','pilot-closeout':'closeout.json','pilot-result':'result.json'}),
            (FLOOR,{'inherited-forecast':'resource-forecast.json'}),
            (PROBE,{'audit-probe':'resource-assessment.json','probe-previous-forecast':'previous-resource-forecast.json'})]:
            manifest=directory+'/files.json'; expected_manifest=source_manifest_pin(directory,pins,inputs)
            require(sha(ROOT/manifest)==expected_manifest,'Changed source manifest')
            files=read(ROOT/manifest); target=output/'evidence'/manifest; target.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(ROOT/manifest,target)
            consumed[manifest]=expected_manifest
            for key,name in fields.items():
                source=directory+'/'+name; expected=pins.get(source) if name=='closeout.json' else files[name]
                require(expected is not None and sha(ROOT/source)==expected,'Changed consumed source: '+source)
                retain(key,source)
            if 'closeout.json' in fields.values(): require(read(ROOT/directory/'closeout.json')['filesHash']==pins[manifest],'Changed closeout manifest binding')
        assessment=assess(inputs); write(output/'inputs.json',index); write(output/'consumed-evidence.json',consumed)
        write(output/'assessment.json',assessment)
        write(output/'completion.json',dict(status='ReadOnlyPrecheckCompleteNotAdmitted',inheritedPinsAuthenticated=len(pins),
            secondsBeforeSealing=time.monotonic()-started,bytesBeforeSealing=sum(p.stat().st_size for p in output.rglob('*') if p.is_file()),
            chargedSeconds=SECONDS,chargedBytes=BYTES,qualificationStarted=False,newFights=0,newValues=0,entropyDraws=0))
        for path,pin in consumed.items(): require(sha(ROOT/path)==pin,'Consumed evidence changed during precheck')
        check()
    except BaseException as error:
        write(output/'failure.json',dict(reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES,qualificationStarted=False,newFights=0,newValues=0))
        raise
    finally:
        write(output/'files.json',{p.relative_to(output).as_posix():sha(p) for p in output.rglob('*') if p.is_file()})
    return verify(output)


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('action',choices=['create','verify']); parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args(); result=create(args.output.resolve()) if args.action=='create' else verify(args.output.resolve())
    print(json.dumps(result,indent=2))
