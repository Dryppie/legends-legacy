"""Offline Region 1 design analysis. Standard library only; never loads player data.

Run from any directory: python build/analyze-equipment.py [--check]
The companion JSON is a design input, not production configuration. This model
checks catalog coverage and computes expectations; it does not simulate combat.
"""

import argparse
import hashlib
import json
import math
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "LL/src/API/API.LL/Data"
INPUT = ROOT / "docs/design/equipment-region-one-inputs.json"
REPORT = ROOT / "docs/design/equipment-region-one-balance-report.md"
SOURCES = {}


def read(path):
    raw = path.read_bytes()
    SOURCES[path.relative_to(ROOT).as_posix()] = hashlib.sha256(raw).hexdigest()
    return json.loads(raw.decode("utf-8-sig"))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def index(rows, key="id"):
    result = {row[key]: row for row in rows}
    require(len(result) == len(rows), f"Duplicate {key} in catalog")
    return result


def compatible(recipe, style):
    tags = {t.lower() for t in recipe["tags"] + recipe["affinityTags"]}
    allowed = {r.lower() for r in style["compatibleRecipeIds"]}
    return (
        (not allowed or recipe["id"].lower() in allowed)
        and all(t.lower() in tags for t in style["requiredRecipeTags"])
        and (not style["anyRecipeTags"] or any(t.lower() in tags for t in style["anyRecipeTags"]))
        and not any(t.lower() in tags for t in style["excludedRecipeTags"])
    )


def table(headers, rows):
    return "\n".join([
        "| " + " | ".join(headers) + " |",
        "| " + " | ".join("---" for _ in headers) + " |",
        *("| " + " | ".join(str(c) for c in row) + " |" for row in rows),
    ])


def generate():
    plan = read(INPUT)
    recipes = index([r for r in read(DATA / "crafting/base-recipes.json") if r["enabled"]], "outputItemId")
    styles = index([s for s in read(DATA / "crafting/blueprints.json") if s["enabled"]])
    sets = index(read(DATA / "crafting/equipment-sets.json"))
    items = index(read(DATA / "items/items.json"))
    regions = read(DATA / "world/regions.json")["regions"]
    areas = index([a for region in regions for a in region["areas"]])
    families = index(read(DATA / "dungeons/dungeons.json")["families"])
    rates = read(DATA / "progression/area-experience.json")["areaExperience"]
    xp = read(DATA / "progression/character-experience.json")["characterLevelCurve"]
    quests = {}
    for path in sorted((DATA / "quests").rglob("*.json")):
        quest = read(path)
        if quest["version"] > quests.get(quest["id"], {}).get("version", 0):
            quests[quest["id"]] = quest
    economy, assumptions = plan["economy"], plan["modelAssumptions"]
    require(plan["region"] == 1 and plan["equipmentTier"] == 1, "This analysis covers Region 1 / Tier 1 only")
    require(plan["merchantEquipmentEnabled"] is False, "Merchant gear is excluded")
    require(len(economy["rankScrapCosts"]) == len(economy["rankCinderCosts"]) == 5, "Ranks must be 0-5")
    require(all(c > 0 for c in economy["rankScrapCosts"] + economy["rankCinderCosts"]), "Rank costs must be positive")
    require(0 < economy["paidScrapRecovery"] < 1, "Recovery must not mint Scrap")
    require(0 < economy["dungeonMatchingChance"] < 1 and economy["dungeonGuaranteeClear"] >= 1, "Invalid guarantee")
    require(0 < economy["archetypeBudgetShare"] < 1, "Invalid style budget allocation")
    require(0 <= economy["idleEquipmentChance"] <= 1, "Invalid equipment probability")
    require(economy["targetedSigilEligibleVictories"] > 0, "Invalid sigil access floor")
    require(0 <= economy["dungeonAwardedRank"] <= 5, "Invalid awarded rank")
    baseline = plan["baseline"]
    require(baseline["rank"] == baseline["baseSalvage"] == baseline["paidScrapBasis"] == 0, "Baseline must have no free salvage value")
    for key in ("handQuest", "armorQuest", "accessoryQuest"):
        require(baseline[key] in quests, f"Unknown baseline quest {baseline[key]}")
    groups = index(plan["archetypeGroups"], "slot")
    require(set(groups) == {"Head", "Chest", "Legs", "Ring", "Necklace", "Relic", "OneHanded", "TwoHanded", "OffHand"}, "Missing combat slot or hand family")
    authored_items = [item for group in groups.values() for item in group["items"]]
    require(len(authored_items) == len(set(authored_items)), "Duplicate archetype assignment")
    require(set(authored_items) == set(recipes), "Archetype coverage differs from enabled recipe catalog")
    for slot, group in groups.items():
        require(group["items"], f"Empty archetype group {slot}")
        for item_id in group["items"]:
            require(item_id in items and recipes[item_id]["slot"] == slot, f"Invalid archetype {item_id}")
            require(recipes[item_id]["tierRange"]["min"] <= plan["equipmentTier"] <= recipes[item_id]["tierRange"]["max"], f"Unsupported tier for {item_id}")
            require(math.isclose(sum(recipes[item_id]["initialStatProfile"].values()), 1), f"Invalid archetype weights {item_id}")
    area_plan = index(plan["areas"])
    region_one = {a["id"] for a in regions[0]["areas"] if a["id"].startswith("region_01_")}
    require(set(area_plan) == region_one, "Missing Region 1 area")
    require(all(a["scrapPerPerfectDay"] > 0 for a in area_plan.values()), "Invalid area Scrap yield")
    ordered = sorted((areas[a] for a in area_plan), key=lambda a: a["levelRequirement"])
    require(all(area_plan[b["id"]]["scrapPerPerfectDay"] >= area_plan[a["id"]]["scrapPerPerfectDay"] for a, b in zip(ordered, ordered[1:])), "Later areas must not have lower full-win yields")
    for area in ordered:
        require(area["requiredCompletedQuestId"] in quests, f"Unknown area gate {area['id']}")
    pools = index(plan["dungeonPools"], "familyId")
    require(set(pools) == {f["id"] for f in families.values() if f["region"] == 1}, "Missing Region 1 dungeon")
    for family_id, pool in pools.items():
        family = families[family_id]
        require(pool["accessQuest"] in quests and pool["minimumLevel"] >= 1, f"Invalid access gate {family_id}")
        require(not any(o.get("filters", {}).get("dungeonDefinitionId") == family_id for o in quests[pool["accessQuest"]]["objectives"]), "Circular first-entry quest")
        first_clear = {r["itemId"] for r in min(family["difficulties"], key=lambda d: d["difficulty"])["rewardTable"]["firstClearRewards"]}
        require(pool["targets"] and pool["styles"], f"Empty pool {family_id}")
        require(len({(t["itemId"], t["styleId"]) for t in pool["targets"]}) == len(pool["targets"]), "Duplicate target")
        for style_id in pool["styles"]:
            style = styles[style_id]
            require(style["sourceId"] == family_id and style["itemId"] in first_clear, f"Unbounded/misassigned style {style_id}")
            require(style["equipmentSetId"] in sets, f"Unknown set {style_id}")
            require(math.isclose(sum(style["bonusStatProfile"].values()), 1), f"Invalid style weights {style_id}")
        for target in pool["targets"]:
            require(target["styleId"] in pool["styles"] and target["itemId"] in recipes, "Target not in source pool")
            require(compatible(recipes[target["itemId"]], styles[target["styleId"]]), f"Incompatible target {target}")

    lines = ["# Equipment: Region 1 balance and coverage report", "",
             "Generated by `build/analyze-equipment.py` from the companion design inputs and checked-in content. All new rewards, prices and access rules are proposals. No runtime configuration is changed.", "",
             "This is exact expectation/coverage analysis, not a production combat simulation or measured player economy. A passing validator does not certify encounter difficulty, ownership transactions or migration safety.", "",
             "## Catalog coverage", "",
             f"Validated {len(recipes)} archetypes, {len(ordered)} areas, {len(pools)} dungeon families, {sum(len(p['styles']) for p in pools.values())} core styles and {sum(len(p['targets']) for p in pools.values())} named targets. No merchant gear.", "",
             table(["Slot/type", "Existing output IDs", "Initial deterministic route"], [
                 (slot, ", ".join(f"`{i}`" for i in group["items"]),
                  "First Weapon: choose legal hands" if slot in ("OneHanded", "TwoHanded", "OffHand") else
                  "First Weapon: choose armor" if slot in ("Head", "Chest", "Legs") else "Rewritten Tools of the Trade: fixed accessory")
                 for slot, group in groups.items()]), "",
             "One two-handed weapon occupies both hand slots but is one item: 7 paid items per loadout. One-handed + offhand or dual wield uses 8. No Tool slot is included. Every listed archetype also has an elective plain-target route; initial rewards grant one selected loadout, not every variant.", "",
             "## Area progression", "",
             "All rows award Tier 1, equip level 1. Area difficulty is not equipment tier. Cinders are the configured hourly targets before bonuses and encounter rounding; Scrap is proposed at 8,640 eligible victories/day.", ""]
    lines += [table(["Area", "Level", "Completed quest gate", "Cinders/hour target", "Proposed Scrap/day"], [
        (a["name"], a["levelRequirement"], f"`{a['requiredCompletedQuestId']}`", f"{rates['baseCindersPerHour'] * rates['difficultyTierMultiplier'] ** a['difficultyTier']:.0f}", area_plan[a["id"]]["scrapPerPerfectDay"])
        for a in ordered]), "", "## Styles and named targets", "",
        "Weights below are normalized inputs to the proposed 15% style allocation; the old additive Blueprint multiplier is not carried forward. Set effects remain candidates pending full-loadout combat review.", ""]
    style_rows, target_rows = [], []
    for pool in pools.values():
        for style_id in pool["styles"]:
            style = styles[style_id]
            eligible = [r for r in recipes.values() if compatible(r, style)]
            slots = {r["slot"] for r in eligible}
            nonhands = len(slots - {"OneHanded", "TwoHanded", "OffHand"})
            dual = nonhands + (2 if "OneHanded" in slots else 0)
            two = nonhands + (1 if "TwoHanded" in slots else 0)
            shield = nonhands + int("OneHanded" in slots) + int("OffHand" in slots)
            thresholds = [b["requiredEquippedItems"] for b in sets[style["equipmentSetId"]]["bonuses"] if b.get("enabled", True)]
            require(max(thresholds) <= max(dual, two, shield), f"Unreachable set threshold {style_id}")
            style_rows.append((style_id.removeprefix("blueprint_"), pool["familyId"], ", ".join(sorted(slots)), ", ".join(f"{k} {v:.0%}" for k, v in style["bonusStatProfile"].items()), "/".join(map(str, thresholds))))
        for target in pool["targets"]:
            recipe, style = recipes[target["itemId"]], styles[target["styleId"]]
            name = style["nameFormat"].replace("{BaseName}", recipe["name"]).replace("{BlueprintName}", style["name"].replace("Blueprint:", "").strip())
            target_rows.append((pool["familyId"], name, f"`{target['itemId']}`", target["styleId"].removeprefix("blueprint_")))
    lines += [table(["Style", "First-clear source", "Compatible types", "Within-style weights", "Set thresholds"], style_rows), "",
              table(["Source", "Named identity", "Archetype", "Native style"], target_rows), "",
              "Every named target is Rare, Tier 1, rank 1. Its mechanical fields are identical for first-clear, random and protected awards; only binding and salvage differ. Source/difficulty/Tier protection records remain separate. Two-handed equipment counts once for set thresholds.", "",
              "## Rank costs and retention", ""]
    scrap = [0] + [sum(economy["rankScrapCosts"][:r]) for r in range(1, 6)]
    cinders = [0] + [sum(economy["rankCinderCosts"][:r]) for r in range(1, 6)]
    lines += [table(["Rank", "Incremental Scrap", "Total Scrap from 0", "Incremental Cinders", "Total Cinders from 0", "Baseline budget multiplier"], [
        (r, economy["rankScrapCosts"][r-1], scrap[r], economy["rankCinderCosts"][r-1], cinders[r], f"{1+r*economy['rankBaselineBudgetIncrement']:.2f}") for r in range(1, 6)]), ""]
    cost_rows = []
    for n in assumptions["fullLoadoutItems"]:
        ordinary, favorite = assumptions["ordinaryRank"], assumptions["favoriteRank"]
        mixed = (n-assumptions["favoriteCount"])*scrap[ordinary] + assumptions["favoriteCount"]*scrap[favorite]
        for name, cost in ((f"All {n} at rank {ordinary}", n*scrap[ordinary]), (f"{n-assumptions['favoriteCount']} at {ordinary}, {assumptions['favoriteCount']} at {favorite}", mixed), (f"All {n} at rank {favorite}", n*scrap[favorite])):
            cost_rows.append((name, cost, *(f"{cost/rate:.2f}" for rate in [*assumptions["originalScrapPerDay"], assumptions["referenceScrapPerDay"]])))
    lines += [table(["Loadout goal, from rank 0", "Scrap", *[f"Days at {x}/day" for x in [*assumptions["originalScrapPerDay"], assumptions["referenceScrapPerDay"]]]], cost_rows), "",
              "These totals exclude free awarded ranks, one-time quest grants, random salvage and refunds. An initial rank-1 reward saves only the actual first-rank price; it does not create paid investment.", "",
              "## Calendar-time and inventory sensitivity", "",
              "Assumptions: continuous idle selection between claims, capped offline credit per claim, independent win fraction, and a separate assumed dungeon-clear rate. Dungeon time displacing idle reduces these estimates; access funding and player availability must also be checked. Other Cinder spending is a sensitivity assumption, not an audited sink total.", ""]
    daily_encounters = 86400 / assumptions["encounterSeconds"]
    p, ceiling = economy["dungeonMatchingChance"], economy["dungeonGuaranteeClear"]
    expected = sum((1-p)**k for k in range(ceiling))
    rows = []
    for scenario in plan["scenarios"]:
        require(scenario["hoursBetweenClaims"] > 0 and 0 < scenario["winRate"] <= 1, "Invalid cadence scenario")
        require(scenario["dungeonClearsPerDay"] > 0 and 0 <= scenario["otherCinderSpendFraction"] < 1, "Invalid economy scenario")
        eligible = min(scenario["hoursBetweenClaims"], assumptions["offlineCapHours"]) / scenario["hoursBetweenClaims"] * scenario["winRate"]
        drops = daily_encounters * eligible * economy["idleEquipmentChance"]
        clears = scenario["dungeonClearsPerDay"]
        income = assumptions["referenceScrapPerDay"] * eligible + economy["dungeonScrapPerClear"] * clears
        mixed = 6*scrap[3] + 2*scrap[5]
        random_sigils = assumptions["randomSigilsPerPerfectDay"] * eligible / len(pools)
        guaranteed_sigils = daily_encounters * eligible / economy["targetedSigilEligibleVictories"]
        target_income = random_sigils + guaranteed_sigils
        available_cinders = rates["baseCindersPerHour"] * rates["difficultyTierMultiplier"] * 24 * eligible * (1-scenario["otherCinderSpendFraction"])
        rows.append((scenario["id"], f"{eligible:.0%}", f"{income:.2f}", f"{mixed/income:.2f}", f"{drops+clears/expected:.2f}", f"{target_income:.2f}", f"{available_cinders:.0f}"))
    lines += [table(["Scenario", "Credited successful idle fraction", "Scrap/day at reference area", "Days: 6 rank-3 + 2 rank-5", "Gear/day, idle + repeat targets", "Sigils/day for selected family, expected", "Lumo Cinders/day after assumed sinks"], rows), "",
              "Gear arrivals omit finite starter/first-clear/quest awards and elective plain requests. A target is assumed selected for every clear. Random plain discoveries are counted as arrivals, not assumed to be useful or salvaged. No purchased resources, guild income, raids, PvP, Tower or Prophecy windfalls are needed for these modeled incomes.", "",
              "## Target wait distribution and access", "",
              f"For a repeat target at {p:.0%} per clear, guaranteed by clear {ceiling}: mean {expected:.4f} successful clears. Probability of reaching the final protected clear is {(1-p)**(ceiling-1):.2%}.", "",
              table(["Matching award on clear", "Probability"], [(k, f"{((1-p)**(k-1) * (p if k < ceiling else 1)):.4%}") for k in range(1, ceiling+1)]), "",
              table(["Successful clears/day (assumed)", "Mean days", "Clear ceiling expressed as days"], [(rate, f"{expected/rate:.2f}", f"{ceiling/rate:.2f}") for rate in (0.25, 0.5, 1, 2)]), "",
              f"The candidate access floor grants one selected bound sigil per {economy['targetedSigilEligibleVictories']:,} eligible regional victories. Equipment progression excludes legacy random sigil drops; the report uses zero random sigil income. At perfect ten-second wins, {ceiling} repeat attempts take at most {ceiling*economy['targetedSigilEligibleVictories']/daily_encounters:.2f} days of credited ordinary combat to fund from this floor alone. This is an access-funding bound, not a real-time completion bound: failures consume attempts without advancing equipment protection, and slower play adds time. The one-time first-entry sigil and first-clear target shorten the initial path.", "",
              "## Level-curve cross-check", "",
              "This optimistic XP-only schedule assumes immediate access to the highest level-eligible Shenic area, configured hourly XP, no bonuses, and no quest/dungeon waits. It is not an expected completion date; chapter gates, dungeon time, player choices and alternate XP sources are excluded.", ""]
    total_hours = 0
    milestones = []
    earned_scrap = 0
    for level in range(1, 50):
        area = max((a for a in ordered if a["levelRequirement"] <= level), key=lambda a: a["levelRequirement"])
        required = xp["baseExperience"] + xp["linearExperiencePerLevel"]*level + xp["quadraticExperiencePerLevelSquared"]*level**2
        required = math.ceil(required/xp["roundingIncrement"])*xp["roundingIncrement"]
        hours = required / (rates["baseExperiencePerHour"] * rates["difficultyTierMultiplier"]**area["difficultyTier"])
        total_hours += hours
        earned_scrap += hours/24*area_plan[area["id"]]["scrapPerPerfectDay"]
        if level+1 in (5, 10, 15, 20, 30, 40, 50):
            milestones.append((level+1, f"{total_hours:.2f}", f"{total_hours/24:.2f}", f"{earned_scrap:.1f}"))
    lines += [table(["Level reached", "Credited perfect-win hours", "24-hour days", "Proposed ordinary Scrap accrued"], milestones), "",
              "The focused-beta quest journey currently ends around level 30; a whole-loadout rank-3 requirement at that point would need separate justification. Treat ranks as choices, not new compulsory quest gates.", "",
              "## Replacement and hand-budget caveats", "",
              f"At the candidate next-band factor, Tier 2 rank 0 / Tier 1 rank 5 budget is {economy['nextBandBudgetGrowthCandidate']/(1+5*economy['rankBaselineBudgetIncrement']):.4f}. This is a budget ratio, not a DPS ratio. Current production tier growth and normalized attributes differ; the canonical evaluator must compare real builds before adopting 25%.", "",
              f"A random or guaranteed rank-1 item paid to rank 3 has {scrap[3]-scrap[1]} paid Scrap and returns {math.floor((scrap[3]-scrap[1])*economy['paidScrapRecovery'])} paid Scrap. A guaranteed copy adds zero base salvage; a random named copy adds {economy['randomNamedBaseSalvage']}. Repricing and free starting ranks never increase that paid basis.", "",
              "Equal per-item rank prices make a two-handed loadout cheaper to improve than an eight-item loadout. The prototype must quantify that difference alongside combined hand budgets and set counts; this report does not silently double two-handed prices.", "",
              "## Evidence fingerprints", "",
              "Hashes identify the exact working-tree inputs, including local quest changes. Rerun with `--check` to detect stale report content. C# behavior assumptions (ten-second cadence, offline cap, random sigils, XP formula and reward rounding) are explicitly documented in the companion plan and require review when runtime rules change.", "",
              table(["Input", "SHA-256"], sorted(SOURCES.items())), ""]
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Validate and require the checked-in report to be current")
    args = parser.parse_args()
    report = generate()
    if args.check:
        require(REPORT.exists() and REPORT.read_text(encoding="utf-8") == report, "Balance report is stale; run without --check")
        print("Equipment catalog coverage and report freshness passed.")
    else:
        REPORT.write_text(report, encoding="utf-8", newline="\n")
        print(f"Wrote {REPORT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
