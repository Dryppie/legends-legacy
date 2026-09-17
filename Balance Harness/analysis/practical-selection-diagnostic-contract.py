"""Read-only contract accounting; no sampler, seed derivation or native execution.

Verify retained resource bytes, pin inspected sources and calculate finite-panel
accounting and conservative probability bounds. Stdout is the JSON appendix.
"""

import hashlib
import json
import math
from pathlib import Path
import runpy


ROOT = Path(__file__).resolve().parents[2]
PRIOR_SCRIPT = Path(__file__).with_name("practical-selection-diagnostic-design.py")
PRIOR_JSON = ROOT / "Balance Harness/Tower-Practical-Selection-Diagnostic-Design.json"
READER = runpy.run_path(str(PRIOR_SCRIPT))
SOURCE_PATHS = [
    "LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs",
    "LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs",
    "LL/tools/BalanceHarness/TowerBossStudy.cs",
    "LL/tools/BalanceHarness/TowerBossStudyPolicy.cs",
    "LL/tools/BalanceHarness/TowerBossStudyArchive.cs",
    "LL/tools/BalanceHarness/TowerPracticalSearch.cs",
    "LL/tools/BalanceHarness/TowerPracticalSearchRun.cs",
    "LL/tools/BalanceHarness/TowerPracticalAllocation.cs",
    "LL/tools/BalanceHarness/TowerPracticalAllocationRecovery.cs",
    "LL/tools/BalanceHarness/TowerCompleteReservation.cs",
    "LL/tools/BalanceHarness/TowerHistoryRegistry.cs",
    "LL/tools/BalanceHarness/TowerRefinementComparisonLaunch.cs",
    "LL/tools/BalanceHarness/TowerStudyLimits.cs",
    "LL/src/Core/Common/Randomness/StableRandom.cs",
]


def require(ok, reason):
    if not ok:
        raise ValueError(reason)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    prior = json.loads(PRIOR_JSON.read_text(encoding="utf-8-sig"))
    require(sha(PRIOR_SCRIPT) == prior["analysisSourceSha256"], "Changed precision reader.")
    require(sha(READER["PRECISION_SOURCE"]) == prior["precisionSourceSha256"], "Changed interval arithmetic.")
    resource = READER["resources"]()
    require(resource == prior["resources"], "Retained resource evidence differs from prior design.")
    source_hashes = {name: sha(ROOT/name) for name in SOURCE_PATHS}
    n, words, word_bytes, history, before = 1000, 2048, 4, 484285, 41
    require(prior["confirmationTrialsPerRecipe"] == n and prior["fights"]["total"] == 4640, "Changed design.")
    population = 2**32-history-before
    coupling = n*(n-1)/(2*population)
    # If fewer than n eligible distinct values appear, at least words-n+1
    # positions were rejected. For any fixed set of these positions, repeated
    # conditioning bounds its probability by r**k; union over at most 2**words
    # subsets. This loose bound avoids floating underflow and any RNG call.
    rejects_needed = words-n+1
    rejection_upper = (history+before+words-1)/2**32
    log10_shortfall_upper = words*math.log10(2)+rejects_needed*math.log10(rejection_upper)
    require(log10_shortfall_upper < -3000, "Entropy batch bound changed.")
    iid_lower = prior["iidConditionalBound"]["conditionalDiagnosticPassLower"]
    rounded_lower = .8109
    require(iid_lower-coupling > rounded_lower+1e-12, "Insufficient rounded power margin.")
    require(history+before+words <= 1_000_000, "Proposed reservation ceiling exceeds history limit.")
    observations = resource["observed"]
    ratio = prior["fights"]["total"]/observations["fights"]
    phase_rows = []
    for phase, measured in observations["phaseMeasurements"].items():
        # Hold admission fixed; scale worker and separate audit by fight count.
        # This is an assumption to expose phase constraints, never a bound.
        projected = measured["measuredSeconds"]*(1 if phase == "admission" else ratio)
        phase_rows.append({"phase":phase,"observedSeconds":measured["measuredSeconds"],
            "historicalCeilingSeconds":measured["chargedSeconds"],
            "illustrativeSeconds":projected,
            "illustrationExceedsHistoricalPhase":projected > measured["chargedSeconds"]})
    processes = {}
    for name in ("public-run", "public-audit", "independent-audit"):
        path = ROOT/READER["PACKAGE"]/(name+"-process.json")
        receipt = json.loads(path.read_text(encoding="utf-8-sig"))
        require(receipt["exitCode"] == 0 and not receipt["timedOut"] and receipt["activeProcesses"] == 0,
                "Changed process completion.")
        processes[name] = {"seconds":receipt["seconds"],"receiptSha256":sha(path)}
    result = {
        "version":"practical-selection-diagnostic-contract-analysis-v1",
        "proposedProtocol":"tower-practical-selection-diagnostic-v1",
        "status":"DesignSpecified_ImplementationAndResourceAdmissionPending",
        "runnableRequest":False,"launchDecision":"NoGo",
        "fights":prior["fights"],"confirmationNominees":4,"intervalFamily":10,
        "frozenPrimaryCount":1,"maximumProposals":256,
        "samplingProposal":{
            "source":"RandomNumberGenerator.Fill, single batch after nominee freeze",
            "sourceModelIsAssumption":True,"rawWordCount":words,"rawBytes":words*word_bytes,
            "mapping":"ReadInt32LittleEndian without modulo or sign masking",
            "selection":"First 1000 distinct values absent from history and pre-confirmation reservations",
            "reserveUnusedTail":True,"extraBatches":0,
            "preConfirmationNewValues":before,"maximumNewReservations":before+words,
            "minimumNewReservationsOnCompletePanel":before+n,
            "maximumNewUnusedTailReservations":words-n,
            "currentHistoricalExclusions":history,
            "maximumTotalExclusionsAtCurrentHistory":history+before+words,
            "targetEligiblePopulationBeforeBatch":population,
            "shortfallRejectionsRequired":rejects_needed,
            "perPositionRejectionUpper":rejection_upper,
            "batchShortfallProbabilityLog10Upper":log10_shortfall_upper,
            "couplingFailureUpper":coupling,
            "roundedConditionalDetectionLowerIncludingBatchCap":rounded_lower,
            "trueGainAlternative":.16,"operationalFailureProbabilityIncluded":False},
        "resourceAdmission":{
            "status":"InsufficientEvidence","newMaximumSeconds":None,"newMaximumBytes":None,
            "grantedAllowance":False,"measuredDiagnosticRuns":0,
            "historicalReferenceOnly":resource["historicalReferenceOnly"],
            "phaseIllustrationsAreNotBounds":True,"phaseIllustrations":phase_rows,
            "illustrativeTotalSeconds":sum(r["illustrativeSeconds"] for r in phase_rows),
            "observedProcesses":processes,
            "observedFinalRetainedBytes":observations["finalRetainedBytes"],
            "observedLargestBattleBytes":observations["largestBattleRecordBytes"],
            "missingEvidence":["Implemented diagnostic runtime identity and zero-combat parity",
                "Compatible phase time and storage model including separate audits and closeout",
                "Explicit prospective operational envelope and allowance"]},
        "requiredArtifacts":["diagnostic-request", "search-binding", "discovery-shortlist",
            "selection-results", "nominees-freeze", "entropy-intent", "entropy-start",
            "entropy-bytes", "entropy-complete", "confirmation-binding", "complete-reservation-ledger",
            "ordered-attempt-journal", "battle-archive", "native-audit", "independent-audit",
            "diagnostic-result", "seed-free-team-exports", "phase-resource-receipts"],
        "recoveryPolicy":"No resume; unresolved entropy remains Pending; legacy receipts reject new version",
        "sourcePins":source_hashes,"retainedEvidencePins":resource["pins"],
        "verifiedManifestEntries":resource["verifiedManifestEntries"],
        "priorDesignSha256":sha(PRIOR_JSON),"analysisSourceSha256":sha(Path(__file__)),
        "newFights":0,"newValues":0,"entropyCalls":0,
    }
    for name, digest in source_hashes.items():
        require(sha(ROOT/name) == digest, "Source changed during analysis: "+name)
    print(json.dumps(result, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
