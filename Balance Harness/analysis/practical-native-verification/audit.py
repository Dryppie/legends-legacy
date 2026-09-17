"""Independent saved-JSON/count/gate audit; never loads a gameplay assembly."""
from collections import Counter
import gzip
import hashlib
import json
import math
import os
from pathlib import Path
from statistics import NormalDist
import sys


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def sha(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def require(ok, message):
    if not ok:
        raise ValueError(message)


def close(actual, expected):
    require(math.isclose(actual, expected, rel_tol=0, abs_tol=1e-8), "Changed interval arithmetic")


def wilson(wins, n):
    z = NormalDist().inv_cdf(1-.025/7)
    p = wins/n
    denominator = 1+z*z/n
    center = (p+z*z/(2*n))/denominator
    width = z*math.sqrt(p*(1-p)/n+z*z/(4*n*n))/denominator
    return max(0,center-width), min(1,center+width)


def scan(registry, excluded):
    pending, found, directories = [registry], set(), 0
    while pending:
        folder = pending.pop()
        directories += 1
        require(directories <= 2_000_000, "Registry directory cap")
        if folder == excluded:
            continue
        with os.scandir(folder) as entries:
            for entry in entries:
                require(not entry.is_symlink() and not entry.is_junction(), "Linked registry input")
                if entry.is_dir(follow_symlinks=False):
                    pending.append(Path(entry.path))
                elif entry.name in ("history-input.json", "seed-ledger.json", "prior-seed-ledger.json"):
                    found.add(str(Path(entry.path)))
    return found


def main(package, run):
    q = read(run / "request.json")
    require(q == read(package / "request.json"), "Changed frozen request")
    require(q["priorSeconds"] == 120 and q["maximumSeconds"] == 480
            and q["priorBytes"] == 32*1024*1024 and q["maximumBytes"] == 240*1024*1024,
            "Changed cumulative phase charges")
    d = read(run / "definition.json")
    template = read(package / "template.json")
    require(sha(run / "source-definition.json") == q["definitionHash"] == sha(package / "template.json"), "Changed template")
    require(d == read(run / "study/definition.json"), "Changed native definition")
    require(d["excludedCombatSeeds"] == template["excludedCombatSeeds"] and len(d["excludedCombatSeeds"]) == 483988,
            "Lost historical exclusions")
    for field in ("contexts", "references", "starts", "budget", "allowedEssences", "ownedCopies", "startsAt", "settingsHash", "executionHash", "contentHashes"):
        require(d[field] == template[field], f"Changed fixed cohort: {field}")
    history = set(d["excludedCombatSeeds"])
    schedule = next(iter(d["stages"]["schedules"].values()))
    panels = dict(construction=d["generation"]["seeds"], discovery=schedule["discovery"],
                  selection=schedule["selection"], confirmation=schedule["confirmation"])
    require([len(v) for v in panels.values()] == [1,8,32,256], "Changed panel counts")
    reserved = [v for panel in panels.values() for v in panel]
    require(len(set(reserved)) == 297 and not history.intersection(reserved), "Overlapping allocation")
    allocation = [json.loads(line) for line in (run / "allocation-journal.jsonl").read_text().splitlines()]
    used, accepted, position, rejected = set(history), {k:[] for k in panels}, 0, 0
    for stage, panel in panels.items():
        ordinal = 0
        while len(accepted[stage]) < len(panel):
            start, result = allocation[position:position+2]
            require(start == dict(kind="Start",stage=stage,ordinal=ordinal,value=None,accepted=None), "Changed allocation start")
            require(result["kind"] == "Candidate" and result["stage"] == stage and result["ordinal"] == ordinal, "Changed allocation order")
            value = result["value"]
            keep = value not in used
            require(result["accepted"] == keep, "Changed rejection decision")
            if keep:
                used.add(value)
                accepted[stage].append(value)
            else:
                rejected += 1
            position += 2
            ordinal += 1
        require(accepted[stage] == panel, "Journal differs from bound panel")
    require(position == len(allocation) <= 200000 and used == history.union(reserved), "Extended allocation")
    require(read(run / "history-input.json") == dict(reservationState="Complete",reserved=reserved), "Incomplete reservation")
    require(read(run / "seed-ledger.json") == dict(reservationState="Complete",historical=sorted(history),reserved=reserved), "Changed permanent union")
    previous = read(run / "history-files.json")
    require(scan(Path(q["registryRoot"]), run) == set(previous), "Changed full registry membership")
    for path, expected in {**read(package / "retained-input-pins.json"), **previous}.items():
        require(sha(path) == expected, f"Changed historical/source pin: {path}")

    study_root = run / "study"
    study = read(study_root / "study.json")
    result = read(run / "result.json")
    require(result == read(package / "public-audit.log") and result["integrityStatus"] == "Verified", "Independent native audit mismatch")
    require(study["status"] == "Complete" and not study["replays"], "Incomplete study or replay")
    family = study["confirmation"]
    members = family["members"]
    require(len(members) in (2,3) and family["afterTrialCount"] == 640, "Changed confirmation freeze")
    require(read(study_root / "confirmation-freeze.json") == family, "Changed frozen family")
    shortlist = read(study_root / "discovery-shortlist.json")
    selection = read(study_root / "selection-results.json")
    finalists = read(study_root / "finalists.json")
    require(shortlist == study["discovery"]["discoveryShortlist"] and len(shortlist) == 4, "Changed shortlist")
    require(selection == study["selection"] and len(selection) == 4 and len(finalists) == 1, "Changed selection")
    require(len(study["discovery"]["arms"]) == 1, "Changed restart count")
    arm = study["discovery"]["arms"][0]
    require(len(arm["evaluations"]) == 64 and len(arm["proposals"]) <= 256, "Changed search allowance")
    counts = dict(discovery=512, selection=128, confirmation=len(members)*256, diagnostics=0, replay=0)
    require(study["accounting"]["attempted"] == study["accounting"]["completed"] == counts, "Changed stage charges")
    trials = [json.loads(line) for line in (study_root / "trials.jsonl").read_text().splitlines()]
    total = sum(counts.values())
    require(total <= 1408 and len(trials) == total, "Fight cap or ledger mismatch")
    require([t["stage"] for t in trials] == [stage for stage,count in counts.items() for _ in range(count)], "Changed stage order")
    attempts = [json.loads(line) for line in (run / "attempts.jsonl").read_text().splitlines()]
    require(attempts == [dict(kind=kind,ordinal=i) for i in range(1,total+1) for kind in ("Started","Completed")], "Incomplete durable attempts")
    for freeze, first in [("discovery-shortlist.json",512),("selection-results.json",640),
                          ("finalists.json",640),("confirmation-freeze.json",640)]:
        require((study_root / freeze).stat().st_mtime_ns <= (study_root / "battles" / (trials[first]["id"]+".json.gz")).stat().st_mtime_ns,
                "Freeze file postdates the first following-stage battle report")
    for stage, panel, parties in [("discovery",panels["discovery"],64),("selection",panels["selection"],4),("confirmation",panels["confirmation"],len(members))]:
        require(Counter(t["seed"] for t in trials if t["stage"] == stage) == Counter({v:parties for v in panel}), "Changed independent stage panels")

    by_recipe = {}
    for trial in trials[640:]:
        with gzip.open(study_root / "battles" / (trial["id"]+".json.gz"), "rt", encoding="utf-8") as stream:
            battle = json.load(stream)
        outcome = battle["battle"]["summary"]["contentOutcome"]
        require(battle["battle"]["seed"] == trial["seed"] and battle["succeeded"] == (outcome == "Victory"), "Changed saved battle outcome")
        by_recipe.setdefault(trial["recipe"], []).append(dict(seed=trial["seed"],outcome=outcome))
    evidence = {e["cellId"]:e for e in study["evidence"]}
    wins = {}
    for member in members:
        item = evidence[member["cellId"]]
        require(item["status"] == "Complete" and item["trials"] == by_recipe[item["scenarioHash"]], "Evidence differs from saved battles")
        require([t["seed"] for t in item["trials"]] == panels["confirmation"], "Reordered pairing")
        wins[member["cellId"]] = [t["outcome"] == "Victory" for t in item["trials"]]
    primary = next(m for m in members if m["primary"])
    selected = wins[primary["cellId"]]
    lower, upper = wilson(sum(selected), 256)
    close(result["selectedRate"]["rate"], sum(selected)/256)
    close(result["selectedRate"]["lower"], lower)
    close(result["selectedRate"]["upper"], upper)
    contrasts = []
    for reference in d["references"]:
        member = next(m for m in members if reference["id"] in m["referenceIds"])
        anchor = wins[member["cellId"]]
        gained = sum(a and not b for a,b in zip(selected,anchor))
        lost = sum(b and not a for a,b in zip(selected,anchor))
        gl,gu = wilson(gained,256)
        ll,lu = wilson(lost,256)
        contrast = next(c for c in result["contrasts"] if c["referenceId"] == reference["id"])
        require(contrast["gains"] == gained and contrast["losses"] == lost, "Changed paired counts")
        close(contrast["observedGain"],(gained-lost)/256)
        close(contrast["lower"],gl-lu)
        close(contrast["upper"],gu-ll)
        contrasts.append(dict(referenceId=reference["id"],wins=sum(anchor),gains=gained,losses=lost,
                              observedGain=(gained-lost)/256,lower=gl-lu))
    improved = not primary["referenceIds"] and lower >= .1 and all(c["observedGain"] >= .05 and c["lower"] > 0 for c in contrasts)
    decision = "IncumbentRetained" if primary["referenceIds"] else "DemonstratedImprovement" if improved else "ImprovementNotDemonstrated"
    require(result["strengthDecision"] == decision, "Changed strength gate")
    teams = read(run / "teams.json")
    require(teams["strengthDecision"] == decision and len(teams["teams"]) == len(members), "Changed exports")
    require(all(t["scenario"]["seeds"] == [] for t in teams["teams"]), "Execution schedules in player exports")
    require(result["balanceAssessment"] == study["balance"]["assessment"], "Changed separate balance assessment")
    audit = dict(status="Passed", completedFights=total, stageCounts=counts, newValues=297,
                 candidateDerivations=len(allocation)//2, rejectedCandidates=rejected, historicalExclusions=483988,
                 totalExclusions=len(used), historyFiles=len(previous), preservedSourcePins=True,
                 selectedWins=sum(selected), confirmationSamples=256, contrasts=contrasts, strengthDecision=decision,
                 balanceAssessment=result["balanceAssessment"], seedFreeExports=True,
                 freezeEvidence="Saved freeze dependencies, stage order and file times; native reconstruction audited separately",
                 newFightsDuringAudit=0, newValuesDuringAudit=0)
    with (package / "independent-audit.json").open("x", encoding="utf-8") as stream:
        json.dump(audit, stream, indent=2, allow_nan=False)
    print(json.dumps(audit, indent=2))


if __name__ == "__main__":
    require(len(sys.argv) == 3, "Supply the frozen package and completed run")
    main(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve())
