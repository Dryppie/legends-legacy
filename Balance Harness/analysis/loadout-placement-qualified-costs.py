"""Plain current-runtime reconciliation; no execution or scientific admission.

Keep the failed native denominator ceilings and every inherited cost floor.
Apply only the implementation-backed single-extra-inventory amendment. The
separate maximum fixture supplies absolute coverage floors, never a denominator.
"""
import importlib.util
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


matched = module('qualified_matched', ROOT / 'build/test-loadout-placement-matched-owned.py')
recovery = module('qualified_recovery', ROOT / 'Balance Harness/analysis/loadout-placement-recovery-protocol.py')
pre = matched.pre


def forecast(inputs, failed_receipt, baseline, placement, maximum):
    ceilings = recovery.native_ceilings(failed_receipt)
    bounded_baseline = dict(baseline)
    for phase, ceiling in ceilings.items():
        bounded_baseline[phase] = min(pre.positive(baseline[phase], phase), ceiling)
    original = matched.forecast(inputs, bounded_baseline, placement)
    pre.require(maximum['status'] == 'PlainMaximumWorkloadVerified' and maximum['literalReports'] == 21888
        and maximum['searchReports'] == 12672 and maximum['heldoutReports'] == 9216
        and maximum['heldoutMembers'] == 36 and maximum['searches'] == 24 and maximum['catalogues'] == 12
        and maximum['nativeAuditSubstituted'] is False
        and maximum['productionAuditPassed'] and maximum['independentPythonAuditPassed']
        and maximum['nativePublicationBarrierPassed'] and maximum['nativePostPublicationVerificationPassed']
        and maximum['plainEvidence'] and maximum['actualCombat'] == maximum['productionEntropyDraws'] == maximum['scientificReservations'] == 0,
        'Incomplete plain maximum-workload evidence')
    full = maximum['phaseCosts']
    pre.require(set(full) == set(pre.PHASES), 'Missing maximum-workload phase')
    for phase in pre.PHASES:
        pre.positive(full[phase], phase)
    closeout = pre.positive(maximum['finalVerificationSeconds'], 'finalVerificationSeconds')
    floors = {phase: max(original['inheritedFloors'][phase], original['scaledPilotCosts'][phase], 2 * full[phase])
        for phase in pre.PHASES}
    probe = inputs['audit-probe']
    historical_bytes = inputs['probe-previous-forecast']['totalBytes'] / probe['inventoryByteRatio']
    storage_ratio = max(1, (floors['nativeBytes'] + floors['auditBytes']) / historical_bytes)
    # The frozen whole worker includes one inventory. Current owner has two total.
    extra = probe['extraInventorySeconds'] * max(1, storage_ratio / probe['inventoryByteRatio']) / 2
    historical_audit = 2 * (probe['wholeWorkerSeconds'] + sum(probe['independentAuditSeconds']) + extra) + 120
    current_audit = 2 * (full['auditSeconds'] + closeout) + 120
    floors['auditSeconds'] = max(floors['auditSeconds'], probe['auditPlanningSeconds'], historical_audit, current_audit)
    rounded = {phase: math.ceil(cost) for phase, cost in floors.items()}
    exceeded = [phase for phase in pre.PHASES if rounded[phase] >= pre.LIMITS[phase]]
    return dict(version='tower-loadout-placement-qualified-plain-costs-v1',
        status='ResourceForecastRejected' if exceeded else 'ResourceForecastPassedFreshAdmissionRequired',
        failedNativeDenominatorCeilings=ceilings, measuredBaseline=baseline, boundedBaseline=bounded_baseline,
        measuredPlacement=placement, maximumWorkloadCosts=full, maximumVerificationSeconds=closeout,
        retainedUnamendedCalculation=original, storageAppliedBeforeAuditScaling=True,
        extraProtectedInventoryPasses=1, timingMargin=2, publicationReserveSeconds=120,
        amendedHistoricalAuditSeconds=historical_audit, currentMaximumAuditSeconds=current_audit,
        unroundedPhaseFloors=floors, roundedPhaseFloors=rounded, limits=pre.LIMITS, exceededPartitions=exceeded,
        resourceForecastPassed=not exceeded, actualCombat=0, productionEntropyDraws=0,
        scientificAdmitted=False, compressedPreparationAllowed=False, compressedLaunchAllowed=False,
        interpretation='Conservative planning forecast using the completed matched pair and independently checked maximum workload; synthetic outcomes do not establish search efficacy.')
