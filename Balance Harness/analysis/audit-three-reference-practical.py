"""Independent saved-row audit for the admitted three-reference practical search.

Adapted from practical-native-verification/audit.py; no combat or allocation.
"""
from collections import Counter
import gzip
import hashlib
import copy
import importlib.util
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
    z = NormalDist().inv_cdf(1-.025/10)
    p = wins/n
    denominator = 1+z*z/n
    center = (p+z*z/(2*n))/denominator
    width = z*math.sqrt(p*(1-p)/n+z*z/(4*n*n))/denominator
    return max(0,center-width), min(1,center+width)


def check_rate(estimate, wins, n=1000):
    lower, upper = wilson(wins, n)
    close(estimate['rate'], wins/n)
    close(estimate['lower'], lower)
    close(estimate['upper'], upper)
    close(estimate['confidence'], .995)


def check_rates(rates, expected):
    require(len(rates) == len(expected) and {r['partyId'] for r in rates} == set(expected),
            'Missing, duplicated or unexpected recipe rate')
    for rate in rates:
        require(rate['wins'] == expected[rate['partyId']], 'Rate wins differ from saved outcomes')
        check_rate(rate['estimate'], rate['wins'])


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
                require(not entry.is_symlink() and not (getattr(entry.stat(follow_symlinks=False), "st_file_attributes", 0) & 0x400), "Linked registry input")
                if entry.is_dir(follow_symlinks=False):
                    pending.append(Path(entry.path))
                elif entry.name in ("history-input.json", "seed-ledger.json", "prior-seed-ledger.json"):
                    found.add(str(Path(entry.path)))
    return found


def main(package, run):
    admission = Path(read(package / "authorization.json")["admissionPackage"])
    q = read(run / "request.json")
    require(q == read(admission / "preset/request.json"), "Changed frozen request")
    require(q['version'] == 'tower-practical-three-reference-allocated-search-v1'
            and sha(admission/'preset/request.json') == read(package/'authorization.json')['requestSha256'],
            'Changed admitted request identity')
    require(q["priorSeconds"] == 600 and q["maximumSeconds"] == 1800
            and q["priorBytes"] == 512*1048576 and q["maximumBytes"] == 2*1073741824,
            "Changed cumulative phase charges")
    d = read(run / "definition.json")
    template = read(admission / "preset/template.json")
    require(sha(run / "source-definition.json") == q["definitionHash"] == sha(admission / "preset/template.json"), "Changed template")
    require(d == read(run / "study/definition.json"), "Changed native definition")
    require(d["excludedCombatSeeds"] == template["excludedCombatSeeds"] and len(d["excludedCombatSeeds"]) == 550367,
            "Lost historical exclusions")
    for field in ("contexts", "references", "starts", "budget", "allowedEssences", "ownedCopies", "startsAt", "settingsHash", "executionHash", "contentHashes"):
        require(d[field] == template[field], f"Changed fixed cohort: {field}")
    history = set(d["excludedCombatSeeds"])
    schedule = next(iter(d["stages"]["schedules"].values()))
    panels = dict(construction=d["generation"]["seeds"], discovery=schedule["discovery"],
                  selection=schedule["selection"], confirmation=schedule["confirmation"])
    require([len(v) for v in panels.values()] == [1,8,32,1000], "Changed panel counts")
    bound = copy.deepcopy(template)
    bound['generation']['seeds'] = panels['construction']
    for value in bound['stages']['schedules'].values():
        value.update(discovery=panels['discovery'], selection=panels['selection'],
                     confirmation=panels['confirmation'], diagnostics=[])
    require(bound == d, 'Binding changed more than the prospective schedules')
    require(d['generation']['policyVersion'] == 'retained-composition-three-references-v1'
            and d['stages']['selectionPolicyVersion'] == 'tower-staged-incumbent-tie-v1'
            and d['stages']['selectionPrimaryReferenceId'] == 'confirmed-399bc7760fb0cf790a5d8ac4',
            'Changed generator or primary selector')
    reserved = [v for panel in panels.values() for v in panel]
    require(len(set(reserved)) == 1041 and not history.intersection(reserved), "Overlapping allocation")
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
    for path, expected in {**read(admission / "history-files.json"), **previous}.items():
        require(sha(path) == expected, f"Changed historical/source pin: {path}")

    study_root = run / "study"
    study = read(study_root / "study.json")
    result = read(run / "result.json")
    require(result == read(package / "public-audit.log") and result["integrityStatus"] == "Verified", "Independent native audit mismatch")
    require(study["status"] == "Complete" and not study["replays"], "Incomplete study or replay")
    family = study["confirmation"]
    members = family["members"]
    references = ['confirmed-399bc7760fb0cf790a5d8ac4', 'confirmed-8287f77974c8c94e8af2fbcd',
                  'confirmed-96b943571150684df3a5be53']
    require([r['id'] for r in d['references']] == [s['referenceId'] for s in d['starts']] == references
            and Counter(r for m in members for r in m['referenceIds']) == Counter(references)
            and len({m['cellId'] for m in members}) == len(members)
            and sum(m['primary'] for m in members) == 1, 'Changed protected confirmation membership')
    require(result['version'] == 'tower-practical-three-reference-search-v1'
            and result['executionStatus'] == 'Complete', 'Changed result version or completion')
    require(len(study['discovery']['arms']) == 1, 'Changed restart count')
    evaluated = len(study['discovery']['arms'][0]['evaluations'])
    discovery_fights, freeze_at = evaluated*8, evaluated*8+160
    require(5 <= evaluated <= 46 and len(members) in (3,4)
            and family['afterTrialCount'] == freeze_at, 'Changed confirmation freeze')
    require(read(study_root / "confirmation-freeze.json") == family, "Changed frozen family")
    shortlist = read(study_root / "discovery-shortlist.json")
    selection = read(study_root / "selection-results.json")
    finalists = read(study_root / "finalists.json")
    require(shortlist == study["discovery"]["discoveryShortlist"] and len(shortlist) == 5, "Changed shortlist")
    require(selection == study["selection"] and len(selection) == 5 and len(finalists) == 1, "Changed selection")
    spec = importlib.util.spec_from_file_location('selection_arithmetic', package/'selection-arithmetic.py')
    selector = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(selector)
    supplied = {s['party']['id'] for s in d['starts']}
    designated = next(s['party']['id'] for s in d['starts']
                      if s['referenceId'] == d['stages']['selectionPrimaryReferenceId'])
    shortlist_ids = [p['id'] for p in shortlist]
    require(supplied <= set(shortlist_ids), 'A protected supplied team was dropped')
    chosen = selector.retain_tied_primary(selection, shortlist_ids, supplied, designated)
    legacy = selector.select(selection, shortlist_ids)
    require(finalists[0]['primary'] and finalists[0]['party']['id'] == chosen
            and result['selectedPartyId'] == chosen
            and [m['generatedIds'] for m in members if m['primary']] == [[chosen]], 'Changed primary choice')
    require(len(study["discovery"]["arms"]) == 1, "Changed restart count")
    arm = study["discovery"]["arms"][0]
    require(len(arm["proposals"]) <= 256, "Changed search allowance")
    ranking = selector.discovery_rank(arm['evaluations'])
    nominated = supplied | {r['id'] for r in [r for r in ranking if r['id'] not in supplied][:2]}
    require(shortlist_ids == [r['id'] for r in ranking if r['id'] in nominated], 'Changed nomination order')
    counts = dict(discovery=discovery_fights, selection=160, confirmation=len(members)*1000, diagnostics=0, replay=0)
    require(study["accounting"]["attempted"] == study["accounting"]["completed"] == counts, "Changed stage charges")
    trials = [json.loads(line) for line in (study_root / "trials.jsonl").read_text().splitlines()]
    total = sum(counts.values())
    require(total <= 4528 and len(trials) == total, "Fight cap or ledger mismatch")
    require([t["stage"] for t in trials] == [stage for stage,count in counts.items() for _ in range(count)], "Changed stage order")
    attempts = [json.loads(line) for line in (run / "attempts.jsonl").read_text().splitlines()]
    require(attempts == [dict(kind=kind,ordinal=i) for i in range(1,total+1) for kind in ("Started","Completed")], "Incomplete durable attempts")
    for freeze, first in [("discovery-shortlist.json",discovery_fights),("selection-results.json",freeze_at),
                          ("finalists.json",freeze_at),("confirmation-freeze.json",freeze_at)]:
        require((study_root / freeze).stat().st_mtime_ns <= (study_root / "battles" / (trials[first]["id"]+".json.gz")).stat().st_mtime_ns,
                "Freeze file postdates the first following-stage battle report")
    for stage, panel, parties in [("discovery",panels["discovery"],evaluated),("selection",panels["selection"],5),("confirmation",panels["confirmation"],len(members))]:
        require(Counter(t["seed"] for t in trials if t["stage"] == stage) == Counter({v:parties for v in panel}), "Changed independent stage panels")

    by_recipe, direct = {}, {}
    for trial in trials:
        with gzip.open(study_root / "battles" / (trial["id"]+".json.gz"), "rt", encoding="utf-8") as stream:
            battle = json.load(stream)
        outcome = battle["battle"]["summary"]["contentOutcome"]
        require(battle["battle"]["seed"] == trial["seed"] and battle["succeeded"] == (outcome == "Victory"), "Changed saved battle outcome")
        require(outcome in ('Victory', 'Defeat', 'Draw'), 'Invalid direct battle outcome')
        direct[trial['id']] = dict(seed=trial['seed'], stage=trial['stage'], won=outcome == 'Victory')
        if trial['stage'] == 'confirmation':
            by_recipe.setdefault(trial["recipe"], []).append(dict(seed=trial["seed"],outcome=outcome))
    require(len(direct) == total, 'Repeated direct trial ID')
    for stage, rows in [('discovery', arm['evaluations']), ('selection', selection)]:
        for row in rows:
            require(len(row['cells']) == 1, 'Changed context count')
            cell = row['cells'][0]
            raw = [direct[t] for t in cell['trials']]
            require(all(r['stage'] == stage for r in raw) and [r['seed'] for r in raw] == panels[stage]
                    and [r['won'] for r in raw] == cell['clears'], 'Measurements differ from direct stage outcomes')
    evidence = {e["cellId"]:e for e in study["evidence"]}
    wins = {}
    for member in members:
        item = evidence[member["cellId"]]
        require(item["status"] == "Complete" and item["trials"] == by_recipe[item["scenarioHash"]], "Evidence differs from saved battles")
        require([t["seed"] for t in item["trials"]] == panels["confirmation"], "Reordered pairing")
        wins[member["cellId"]] = [t["outcome"] == "Victory" for t in item["trials"]]
    primary = next(m for m in members if m["primary"])
    selected = wins[primary["cellId"]]
    lower, upper = wilson(sum(selected), 1000)
    check_rate(result['selectedRate'], sum(selected))
    party_by_cell = {m['cellId']: m['generatedIds'][0] if m['generatedIds'] else
                     next(s['party']['id'] for s in d['starts'] if s['referenceId'] in m['referenceIds'])
                     for m in members}
    recipe_wins = {party_by_cell[cell]: sum(outcomes) for cell, outcomes in wins.items()}
    require(len(recipe_wins) == len(members), 'Duplicated confirmation recipe')
    check_rates(result['rates'], recipe_wins)
    require([c['referenceId'] for c in result['contrasts']] == references, 'Changed contrast coverage')
    contrasts = []
    for reference in d["references"]:
        member = next(m for m in members if reference["id"] in m["referenceIds"])
        anchor = wins[member["cellId"]]
        gained = sum(a and not b for a,b in zip(selected,anchor))
        lost = sum(b and not a for a,b in zip(selected,anchor))
        gl,gu = wilson(gained,1000)
        ll,lu = wilson(lost,1000)
        contrast = next(c for c in result["contrasts"] if c["referenceId"] == reference["id"])
        require(contrast["gains"] == gained and contrast["losses"] == lost, "Changed paired counts")
        close(contrast["observedGain"],(gained-lost)/1000)
        close(contrast["lower"],gl-lu)
        close(contrast["upper"],gu-ll)
        contrasts.append(dict(referenceId=reference["id"],wins=sum(anchor),gains=gained,losses=lost,
                              observedGain=(gained-lost)/1000,lower=gl-lu))
    improved = not primary["referenceIds"] and lower >= .1 and all(c["observedGain"] >= .05 and c["lower"] > 0 for c in contrasts)
    decision = "IncumbentRetained" if primary["referenceIds"] else "DemonstratedImprovement" if improved else "ImprovementNotDemonstrated"
    require(result["strengthDecision"] == decision, "Changed strength gate")
    teams = read(run / "teams.json")
    require(teams["strengthDecision"] == decision and len(teams["teams"]) == len(members), "Changed exports")
    recommended = [chosen] if improved else [s['party']['id'] for s in d['starts']]
    require(result['recommendedPartyIds'] == recommended, 'Changed practical recommendation')
    require(teams['version'] == result['version'] and teams['integrityStatus'] == 'Verified'
            and all(teams[k] == result[k] for k in ('studyHash', 'archiveHash'))
            and all(teams[k] == d[k] for k in ('contentHashes', 'settingsHash', 'executionHash')),
            'Changed export identity or provenance')
    require({t['partyId'] for t in teams['teams']} == set(recipe_wins), 'Dropped or substituted exported recipe')
    for member in members:
        team = next(t for t in teams['teams'] if t['partyId'] == party_by_cell[member['cellId']])
        scenario = copy.deepcopy(next(c['scenario'] for c in family['definition']['cells'] if c['id'] == member['cellId']))
        scenario['seeds'] = []
        expected_role = 'Reference' if member['referenceIds'] else 'ImprovedCandidate' if improved else 'MeasuredChallenger'
        require(team['scenario'] == scenario and team['referenceIds'] == member['referenceIds']
                and team['role'] == expected_role and team['recommended'] == (team['partyId'] in recommended),
                'Export differs from frozen recipe, retained references or recommendation')
        copies = Counter(e for p in scenario['party'] for e in p['build']['essenceIds'])
        require(team['requiredCopies'] == dict(copies)
                and team['subgroups'] == {str(p['partySlot']): (p['partySlot']-1)//5+1 for p in scenario['party']}
                and all(p['build']['essenceIds'] == sorted(set(p['build']['essenceIds'])) for p in scenario['party']),
                'Changed required copies, subgroup map or canonical Essence order')
    require(all(t["scenario"]["seeds"] == [] for t in teams["teams"]), "Execution schedules in player exports")
    require(result["balanceAssessment"] == study["balance"]["assessment"], "Changed separate balance assessment")
    audit = dict(status="Passed", completedFights=total, stageCounts=counts, evaluatedCandidates=evaluated, newValues=1041,
                 candidateDerivations=len(allocation)//2, rejectedCandidates=rejected, historicalExclusions=550367,
                 totalExclusions=len(used), historyFiles=len(previous), preservedSourcePins=True,
                 selectedWins=sum(selected), confirmationSamples=1000, contrasts=contrasts, strengthDecision=decision,
                 intervalFamily=10, confirmationRecipeWins=recipe_wins, protectedReferences=references,
                 recommendedPartyIds=recommended, referenceAndExportCoverage=True,
                 selectedPartyId=chosen, designatedPartyId=designated, legacySelectedPartyId=legacy,
                 tieRuleChangedChoice=chosen != legacy,
                 selectionWins={r['id']:selector.wins(r) for r in selection},
                 balanceAssessment=result["balanceAssessment"], seedFreeExports=True,
                 freezeEvidence="Saved freeze dependencies, stage order and file times; native reconstruction audited separately",
                 newFightsDuringAudit=0, newValuesDuringAudit=0)
    with (package / "independent-audit.json").open("x", encoding="utf-8") as stream:
        json.dump(audit, stream, indent=2, allow_nan=False)
    print(json.dumps(audit, indent=2))


if __name__ == "__main__":
    require(len(sys.argv) == 3, "Supply the frozen package and completed run")
    main(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve())
