"""Read-only qualification evidence from two separately sealed practical runs.

No candidate generation, sampling, model fitting, combat, native reconstruction,
seed derivation or archive writes. Saved schedules are read only to check pairing;
their values and allocation masters are not exported. Stdout is reproducible JSON.
"""

from collections import Counter
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PACKAGE = "TestResults/practical-native-verification-20260917"
NATIVE = "TestResults/balance/tower-practical-native-verification-20260917"
PILOT = "TestResults/balance/tower-incumbent-practical-pilot-20260916"
PACKAGE_HASH = "2c8415a62bdf6d72451a6468be5ba0c7ea78a10744e0688516a796690b921e15"
NATIVE_HASH = "54a3a877b209207bc66edd027396e7116e79acbae379639c513990d1a37f3db3"


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


class Evidence:
    def __init__(self):
        self.reads = {}

    def read(self, name, expected):
        path = ROOT / name
        before = sha(path)
        require(before == expected, "Changed pinned source: " + name)
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        require(sha(path) == before, "Source changed while reading: " + name)
        self.reads[name] = before
        return data

    def verify_unchanged(self):
        for name, expected in self.reads.items():
            require(sha(ROOT / name) == expected, "Source changed after reading: " + name)


def wins(row):
    require(len(row["cells"]) == 1, "This analysis requires one context.")
    clears = row["cells"][0]["clears"]
    require(all(type(x) is bool for x in clears), "Non-binary saved clear flags.")
    return sum(clears)


def discovery_key(row):
    fitness = row["fitness"]
    return (-fitness["worstContextWinRate"], fitness["guardianHealth"],
            -fitness["survival"], fitness["victoryDuration"], row["id"])


def summarize(evidence, label, directory, manifest, audit):
    def read(name):
        key = "study/" + name
        return evidence.read(directory + "/" + key, manifest[key])

    discovery = read("discovery.json")
    selection = read("selection-results.json")
    shortlist = read("discovery-shortlist.json")
    finalists = read("finalists.json")
    report = read("study.json")
    definition = read("definition.json")
    require(discovery["status"] == report["status"] == "Complete", "Incomplete source study.")
    require(discovery["version"] == "retained-composition-incumbents-v1", "Changed incumbent contract.")
    require(report["discovery"] == discovery and report["selection"] == selection, "Inconsistent saved reports.")
    require(len(discovery["arms"]) == len(finalists) == len(definition["stages"]["schedules"]) == 1,
            "Unexpected run shape.")
    arm = discovery["arms"][0]
    require(arm["stopReason"] == "CandidateBudgetReached", "Incomplete discovery budget.")
    proposals = arm["proposals"]
    measured = {p["party"]["id"]: p for p in proposals if p["result"] == "evaluated"}
    rows = {r["id"]: r for r in arm["evaluations"]}
    require(len(rows) == len(measured) == 64 and rows.keys() == measured.keys(), "Incomplete candidate mapping.")
    schedule = next(iter(definition["stages"]["schedules"].values()))
    require([len(schedule[s]) for s in ["discovery", "selection", "confirmation"]] == [8, 32, 256],
            "Changed source stage sizes.")
    for row in rows.values():
        require(len(row["cells"][0]["clears"]) == 8 and row["fitness"]["worstContextWinRate"] == wins(row) / 8,
                "Inconsistent discovery wins.")
    for row in selection:
        require(len(row["cells"][0]["clears"]) == 32 and row["fitness"]["worstContextWinRate"] == wins(row) / 32,
                "Inconsistent selection wins.")
    ranked = sorted(rows.values(), key=discovery_key)
    ranks = {r["id"]: i + 1 for i, r in enumerate(ranked)}
    anchors = {s["party"]["id"]: s["referenceId"] for s in definition["starts"]}
    require(len(anchors) == 2 and anchors.keys() <= measured.keys(), "Missing incumbents.")
    challengers = [r["id"] for r in ranked if r["id"] not in anchors][:2]
    expected_shortlist = [r["id"] for r in ranked if r["id"] in set(anchors) | set(challengers)]
    require([s["id"] for s in shortlist] == expected_shortlist and discovery["discoveryShortlist"] == shortlist,
            "Nomination differs from current frozen rule.")
    selection_ids = {r["id"] for r in selection}
    require(len(selection) == 4 and selection_ids == set(expected_shortlist), "Incomplete selection.")
    frozen_ranks = {party: i for i, party in enumerate(expected_shortlist)}
    selected = sorted(selection, key=lambda r: (-wins(r), r["cells"][0]["guardianHealth"] if wins(r) == 0 else 0,
                                               frozen_ranks[r["id"]], r["id"]))[0]
    require(finalists[0]["primary"] and finalists[0]["party"]["id"] == selected["id"], "Changed finalist.")

    by_proposal = {}
    fresh_ancestry = {}
    for proposal in proposals:
        provenance = proposal["provenance"]
        if provenance["operator"] != "supplied":
            require(all(parent in by_proposal for parent in provenance["parentIds"]), "Forward or missing parent.")
        inherited = any(fresh_ancestry[parent] for parent in provenance["parentIds"] if parent in by_proposal)
        fresh_ancestry[provenance["id"]] = provenance["operator"] == "fresh-legal" or inherited
        by_proposal[provenance["id"]] = proposal
    fresh_ids = [k for k, p in measured.items() if p["provenance"]["operator"] == "fresh-legal"]
    fresh_descendants = [k for k, p in measured.items() if k not in fresh_ids and fresh_ancestry[p["provenance"]["id"]]]
    operators = {}
    for operator in sorted({p["provenance"]["operator"] for p in measured.values()}):
        ids = [k for k, p in measured.items() if p["provenance"]["operator"] == operator]
        operators[operator] = {"evaluatedParties": len(ids), "discoveryFights": 8 * len(ids),
                               "recordedWins": sum(wins(rows[k]) for k in ids),
                               "zeroWinParties": sum(wins(rows[k]) == 0 for k in ids)}

    confirmation = {}
    for member in report["confirmation"]["members"]:
        recorded = next(e for e in report["evidence"] if e["cellId"] == member["cellId"])
        require(recorded["status"] == "Complete" and [t["seed"] for t in recorded["trials"]] == schedule["confirmation"],
                "Incomplete or unpaired saved confirmation.")
        clear_count = sum(t["outcome"] == "Victory" for t in recorded["trials"])
        for party in member["generatedIds"]:
            confirmation[party] = clear_count
        for reference in member["referenceIds"]:
            party = next(k for k, value in anchors.items() if value == reference)
            confirmation[party] = clear_count
    require(set(confirmation) == set(anchors) | {selected["id"]}, "Unexpected confirmation family.")
    require(audit["status"] == "Passed", "Missing prior independent audit.")
    decision = audit.get("strengthDecision", audit.get("decision"))
    require(decision == "ImprovementNotDemonstrated", "Changed retained strength decision.")
    if label == "native":
        require(audit["selectedWins"] == confirmation[selected["id"]], "Changed native audit count.")
        for contrast in audit["contrasts"]:
            party = next(k for k, ref in anchors.items() if ref == contrast["referenceId"])
            require(contrast["wins"] == confirmation[party], "Changed native anchor count.")
    else:
        require(sorted(c["wins"] for c in audit["confirmation"]) == sorted(confirmation.values()), "Changed pilot audit counts.")
    primary_clears = selected["cells"][0]["clears"]
    nominees = []
    for party in expected_shortlist:
        row = next(r for r in selection if r["id"] == party)
        clears = row["cells"][0]["clears"]
        nominees.append({"partyId": party, "referenceId": anchors.get(party), "discoveryRank": ranks[party],
                         "discoveryWinsOf8": wins(rows[party]), "selectionWinsOf32": wins(row),
                         "selected": party == selected["id"], "confirmationWinsOf256": confirmation.get(party),
                         "primarySelectionGains": sum(a and not b for a, b in zip(primary_clears, clears)),
                         "primarySelectionLosses": sum(b and not a for a, b in zip(primary_clears, clears))})
    distances = {}
    selected_builds = measured[selected["id"]]["party"]["builds"]
    for party, reference in anchors.items():
        builds = measured[party]["party"]["builds"]
        distances[reference] = {
            "replacementsOf50": sum(len(set(v) - set(builds[k])) for k, v in selected_builds.items()),
            "changedOwnersOf10": sum(set(v) != set(builds[k]) for k, v in selected_builds.items())}
    return {"study": directory, "panelsPooled": False, "evaluatedParties": len(rows), "proposals": len(proposals),
            "proposalOutcomes": dict(sorted(Counter(p["result"] for p in proposals).items())),
            "discoveryWinHistogram": dict(sorted(Counter(wins(r) for r in rows.values()).items())),
            "operatorDescriptionsNotCausalEstimates": operators,
            "freshAncestryDescendants": {"evaluatedParties": len(fresh_descendants),
                                         "recordedDiscoveryWins": sum(wins(rows[k]) for k in fresh_descendants)},
            "nominees": nominees, "selectedDistanceFromAnchors": distances,
            "unconfirmedEvaluatedParties": len(rows) - len(confirmation),
            "strengthDecision": decision}


def main():
    evidence = Evidence()
    package = evidence.read(PACKAGE + "/files.json", PACKAGE_HASH)
    native = evidence.read(NATIVE + "/files.json", NATIVE_HASH)
    retained = evidence.read(PACKAGE + "/retained-input-pins.json", package["retained-input-pins.json"])

    def historical_pin(name):
        matches = [value for path, value in retained.items() if path.replace("\\", "/").endswith("/" + name)]
        require(len(matches) == 1, "Ambiguous historical pin: " + name)
        return matches[0]

    pilot = evidence.read(PILOT + "/files.json", historical_pin(PILOT + "/files.json"))
    source_pins = evidence.read(PACKAGE + "/source-files.json", package["source-files.json"])
    for filename in ["TowerSuppliedCompositionSearch.cs", "TowerBossGeneration.cs", "TowerBossStudyPolicy.cs", "TowerZeroWinSelection.cs"]:
        name = "LL/tools/BalanceHarness/" + filename
        require(sha(ROOT / name) == source_pins[name], "Current search rule differs from producing source: " + name)
        evidence.reads[name] = source_pins[name]
    pilot_audit = evidence.read(PILOT + "/independent-audit.json", pilot["independent-audit.json"])
    native_audit = evidence.read(PACKAGE + "/independent-audit.json", package["independent-audit.json"])
    completion = evidence.read(PACKAGE + "/completion.json", package["completion.json"])
    require(completion["scope"] == "Closed" and completion["totalExclusions"] == 484285, "Changed native closeout.")
    runs = [summarize(evidence, "pilot", PILOT, pilot, pilot_audit), summarize(evidence, "native", NATIVE, native, native_audit)]
    evidence.verify_unchanged()
    print(json.dumps({"version": "practical-hypothesis-qualification-v1", "decision": "NoSearchChangeQualified",
                      "scope": "Saved JSON arithmetic and source review; no new native authentication",
                      "runs": runs, "nativeOperationalObservation": {
                          "fights": completion["completedFights"], "measuredSeconds": completion["measuredSeconds"],
                          "forfeitedSeconds": completion["chargedSeconds"], "forfeitedBytes": completion["chargedBytes"],
                          "largerStudyFeasibilityEstablished": False},
                      "futureDiagnosticAccountingOnly": {"pairedRecipes": 4, "searchAndSelectionFights": 640,
                          "totalAt256PerParty": 1664, "totalAt1024PerParty": 4736, "launchReady": False},
                      "preservedExclusions": 484285, "seedValuesExported": 0, "newValues": 0, "newFights": 0,
                      "verifiedInputHashes": dict(sorted(evidence.reads.items())),
                      "analysisSourceSha256": sha(Path(__file__))}, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
