"""Prospective plain-format inventory amendment; pure arithmetic, no launch path.

The historical probe counted one inventory inside its whole worker and two extra
owner passes. The new owner performs the initial inventory and one combined final
verification/manifest pass. Retain the whole worker and charge one extra pass.
This changes an operation multiplicity, not a measured per-byte time coefficient.
"""
import math

VERSION='tower-loadout-placement-publication-inventory-amendment-v1'
LIMITS=dict(nativeSeconds=9000,auditSeconds=1800,nativeBytes=5905580032,auditBytes=536870912)
PASSES=dict(ownerBefore=3,ownerAfter=2,includedInWholeWorker=1,extraBefore=2,extraAfter=1)

def require(ok,message):
    if not ok:raise ValueError(message)

def positive(value):
    require(type(value) in (int,float) and math.isfinite(value) and value>0,'Invalid positive cost')
    return value

def assess(recovery,probe,proof,passes):
    require(passes==PASSES and all(type(v) is int for v in passes.values()),'Unverified inventory multiplicity')
    require(recovery['limits']==LIMITS and recovery['timingMargin']==2 and recovery['publicationReserveSeconds']==120,
            'Changed historical limits, margin or reserve')
    require(probe['timingMargin']==2 and probe['publicationReserveSeconds']==120,'Changed probe rule')
    require(proof['legacyManifest']==proof['currentManifest'] and proof['protected']
            and all(proof['currentManifest'].get(k)==v for k,v in proof['protected'].items()),'Unverified manifest equivalence')
    sizes=proof['inputSizes'];old,new,saved=(proof[k] for k in ('legacyReadBytes','currentReadBytes','removedReadBytes'))
    require(all(type(n) is int and n>=0 for n in sizes.values()) and all(type(n) is int and n>0 for n in (old,new,saved)),
            'Invalid proof byte counts')
    require(set(sizes)==set(proof['currentManifest']) and new==sum(sizes.values())
            and saved==sum(sizes[k] for k in proof['protected']) and old-new==saved,
            'No exact single protected-pass reduction')
    require(proof['timingSample'] is False and proof['resourceForecast'] is None and proof['usableForAdmission'] is False,
            'Correctness evidence cannot be a timing qualification')
    for collection in ('unroundedNecessaryFloors','inheritedFloors','scaledPilotLowerBounds'):
        require(set(recovery[collection])==set(LIMITS),'Missing historical partition')
        for value in recovery[collection].values():positive(value)
    whole=2*(positive(probe['wholeWorkerSeconds'])+sum(positive(v) for v in probe['independentAuditSeconds']))
    require(len(probe['independentAuditSeconds'])==2,'Both independent audits must remain')
    old_probe=positive(recovery['frozenProbeAuditSecondsLowerBound'])
    inventories=old_probe-whole-120
    positive(inventories)
    retained_audit=max(recovery['inheritedFloors']['auditSeconds'],recovery['scaledPilotLowerBounds']['auditSeconds'],
                       positive(probe['auditPlanningSeconds']))
    require(math.isclose(recovery['unroundedNecessaryFloors']['auditSeconds'],max(retained_audit,old_probe),rel_tol=0,abs_tol=1e-8),
            'Unreconciled historical audit floor')
    revised_probe=whole+inventories*passes['extraAfter']/passes['extraBefore']+120
    revised=dict(recovery['unroundedNecessaryFloors']);revised['auditSeconds']=max(retained_audit,revised_probe)
    rounded={k:math.ceil(v) for k,v in revised.items()};failed=[k for k in LIMITS if rounded[k]>=LIMITS[k]]
    return dict(version=VERSION,status='NecessaryGateFailed' if failed else 'NecessaryGatePassedNotQualified',
        scope='PlainJsonComparisonOnly',operationCounts=dict(passes),limits=dict(LIMITS),timingMargin=2,publicationReserveSeconds=120,
        historicalUnroundedFloors=dict(recovery['unroundedNecessaryFloors']),historicalRoundedFloors=dict(recovery['roundedNecessaryFloors']),
        retainedWholeWorkerAndIndependentAuditsWithMarginSeconds=whole,
        historicalExtraInventoriesWithMarginSeconds=inventories,
        replacementExtraInventoryWithMarginSeconds=inventories/2,
        revisedProbePlanningSeconds=revised_probe,retainedAuditFloorSeconds=retained_audit,
        unroundedNecessaryFloors=revised,roundedNecessaryFloors=rounded,exceededPartitions=failed,
        necessaryGatePassed=not failed,qualificationMayBeRegistered=not failed,
        interpretation='Operation-count amendment of the historical planning model; no measured speedup or current-runtime forecast',
        oldRecoveryGateUnchanged=True,historicalChargesUnchanged=True,physicalByteCompressionDiscount=False,
        currentRuntimeQualified=False,qualifiedCurrentForecast=None,measurementAuthorized=False,
        nativePreparationAllowed=False,scientificAdmissionAllowed=False,compressedPreparationAllowed=False,compressedLaunchAllowed=False,
        actualCombat=0,productionEntropyDraws=0,newScientificReservations=0,newTimingSamples=0,
        wholeProcessCoverage=False,usableForAdmission=False)
