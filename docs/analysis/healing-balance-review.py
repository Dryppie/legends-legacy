"""Generate a fixed, bounded healing candidate study and summarize its production-engine results."""
import argparse
import copy
import json
import statistics
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FIXTURES = ROOT / "LL/tools/BalanceHarness/Fixtures"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def template(name, cell_id):
    return copy.deepcopy(next(c for c in read(FIXTURES / f"attribute-allocation-{name}.json")["cells"] if c["id"] == cell_id))


def build(name, essences, restoration_slots=0, early=False, weapon="staff", relic=True):
    parts = [("Head", "medium_helm"), ("Chest", "medium_mail"), ("Legs", "medium_greaves"),
             ("Ring", "band"), ("Necklace", "amulet"), ("Relic", "vial"), ("MainHand", weapon)]
    if weapon == "shortsword":
        parts.append(("OffHand", "towershield"))
    # Ring + necklace first; then head, chest, relic and weapon. Two-handed weapon costs two units.
    restorative = {"Ring", "Necklace", "Head", "Chest", "Relic", "MainHand"}
    selected = set(["Ring", "Necklace", "Head", "Chest", "Relic", "MainHand"][:restoration_slots])
    equipment = []
    for slot, part in parts:
        if slot == "Relic" and not relic:
            continue
        spec = ".spec.restoration" if slot in selected & restorative else ""
        rarity = (".rarity.common" if spec else "") if early else ".rarity.epic"
        equipment.append(dict(slot=slot, definitionId=f"plain.{part}{spec}{rarity}", useNativeStyle=False))
    return dict(id=name, characterLevel=10 if early else 90, tier=1 if early else 2, rank=0,
                quality="Standard", attributeRollMultiplier=1, equipment=equipment,
                essenceIds=[f"essence.{e}" for e in essences])


def make_request(output):
    cells = []
    herb = ["lizardfolk_shaman"]
    sapling = ["treant_sapling"]
    both = herb + sapling
    damage = ["goblin_warrior", "goblin", "vampire_bat", "frost_imp", "raven", "lumo_sentinel"]
    dot = ["venomous_snake", "giant_spider", "blood_zombie", "flame_imp"]
    physical = build("physical-opponent", damage, weapon="maul")
    periodic = build("periodic-opponent", dot)

    def pvp(name, focal, enemy, doctrine="duelist", **extras):
        cells.append(dict(id=name, reference=copy.deepcopy(focal), candidate=copy.deepcopy(focal),
                          opponent=copy.deepcopy(enemy), doctrine=dict(id=doctrine),
                          opponentDoctrine=dict(id="duelist"), **extras))

    for slots in [0, 2, 6]:
        for label, essences, style in [("herb", herb, "duelist"), ("sapling", sapling, "duelist"),
                                      ("both", both, "duelist"), ("both-conduit", both, "conduit"),
                                      ("both-bastion", both, "bastion"),
                                      ("damage-hybrid", damage + both, "duelist"),
                                      ("barrier-only", ["wood_nymph"], "bastion"),
                                      ("other-healer", ["forest_spirit"], "bastion")]:
            name = f"pvp-{label}-rest{slots}"
            pvp(name, build(name, essences, slots), physical, style)
        name = f"pvp-hybrid-periodic-rest{slots}"
        pvp(name, build(name, damage + both, slots), periodic)
        # Two damage allies: isolate the healer's contribution in a three-versus-three encounter.
        name = f"party-herb-rest{slots}"
        pvp(name, build(name, herb + ["forest_spirit", "wood_nymph"], slots), physical, "bastion",
            allies=[dict(build=build(f"ally-{i}", damage, weapon="maul"), doctrine=dict(id="duelist")) for i in range(2)],
            additionalOpponents=[dict(build=build(f"enemy-{i}", damage, weapon="maul"), doctrine=dict(id="duelist")) for i in range(2)])

    for slots in [0, 2]:
        for label, essences in [("herb", herb), ("sapling", sapling)]:
            name = f"early-{label}-rest{slots}"
            focal = build(name, essences, slots, early=True, weapon="shortsword")
            pvp("pvp-" + name, focal, build("early-enemy", ["goblin_warrior"], early=True, weapon="shortsword"))
            idle = template("current", "early-basic-idle-precision")["idle"]
            idle["assumptions"] = ["Level 10 Common equipment; one unascended healing essence; ownership assumed; full-health single encounter."]
            cells.append(dict(id="idle-" + name, reference=focal, candidate=copy.deepcopy(focal), idle=idle, doctrine=dict(id="duelist")))

    for slots in [0, 2, 6]:
        for mode, source in [("dungeon", "four-essence-dungeon-precision"), ("tower", "four-essence-tower-precision")]:
            name = f"{mode}-healer-rest{slots}"
            cell = template("current", source)
            focal = build(name, both + ["goblin_warrior", "wood_nymph"], slots, early=True, weapon="shortsword")
            focal["characterLevel"] = 30
            cell.update(id=name, reference=focal, candidate=copy.deepcopy(focal), doctrine=dict(id="bastion"))
            cell.pop("comparisonGroup", None)
            if mode == "tower":
                cell["tower"]["assumptions"] = ["Five level-30, tier-1 Common builds; only first member's build is the healer under study; full health; unascended essences; no contributions."]
            cells.append(cell)
        name = f"region2-hybrid-rest{slots}"
        cell = template("current", "ten-essence-region2-precision")
        focal = build(name, damage + both, slots)
        cell.update(id=name, reference=focal, candidate=copy.deepcopy(focal), doctrine=dict(id="duelist"))
        cell.pop("comparisonGroup", None)
        cells.append(cell)

    return dict(schemaVersion=1, contentRoot="LL/src/API/API.LL", outputDirectory=str(output),
                explorationSeeds=list(range(270901, 270905)), confirmationSeeds=list(range(271001, 271033)),
                maximumBattles=10000, referenceRulesVersion=18,
                referenceEquipmentBalanceVersion=3, candidateEquipmentBalanceVersion=4,
                candidateAbilityBalanceProfile="healing-v1", cells=cells)


def summarize(output):
    request = read(output / "request.json")
    trials = read(output / "trials.json")
    lines = ["# Healing candidate: fixed-fixture evidence", "",
             f"{len(trials)*2} production-engine fights across {len(request['cells'])} fixtures. "
             "Four exploration seeds and 32 separate confirmation seeds; PvP includes both orientations. "
             "Each side of a pair uses identical builds and nominal budgets, equipment 3 versus 4 and baseline abilities versus healing-v1.", "",
             "The table uses confirmation seeds only. Wins and draws are counts across both orientations where applicable. "
             "Healing and barriers are effective team totals per fight; duration is seconds. "
             "These are authored fixtures, not population balance estimates. See estimates.json for paired intervals and constant-difference limitations.", "",
             "| Fixture | Wins old/new | Draws old/new | Heal old/new | Barrier absorbed old/new | Seconds old/new |", "|---|---:|---:|---:|---:|---:|"]
    for cell in request["cells"]:
        rows = [r for r in trials if r["cell"] == cell["id"] and r["phase"] == "confirmation"]
        def metric(side, name):
            return statistics.mean(sum(s.get(name, 0) or 0 for s in r[side]["statistics"]
                                       if s["team"] == ("Hostile" if r["mirrored"] else "Friendly")) for r in rows)
        def counts(side, draw=False):
            return sum(r[side]["engineOutcome"] == ("Draw" if draw else "Defeat" if r["mirrored"] else "Victory") for r in rows)
        pairs = [f"{counts('reference')}/{counts('candidate')}", f"{counts('reference', True)}/{counts('candidate', True)}"]
        for metric_name in ["healingDone", "damageBlocked"]:
            pairs.append(f"{metric('reference', metric_name):.0f}/{metric('candidate', metric_name):.0f}")
        pairs.append("/".join(f"{statistics.mean(r[side]['durationSeconds'] for r in rows):.1f}" for side in ["reference", "candidate"]))
        lines.append(f"| {cell['id']} | " + " | ".join(pairs) + " |")
    lines += ["", "Limits: all build essences are level 1 and unascended; the unit tests cover ascension 3. "
              "Single PvE encounters start at full health. This does not test route attrition, all essence combinations, "
              "live inventory distributions or player selection rates. Restoration also strengthens unchanged healing and barriers. "
              "No production defaults or database rows are modified by this study."]
    (output / "healing-summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=["prepare", "prepare-no-relic", "summarize"])
    parser.add_argument("path", type=Path, help="Request JSON for prepare; result directory for summarize.")
    parser.add_argument("--output", type=Path, default=Path("TestResults/healing-balance-candidate-20260927-v2"))
    args = parser.parse_args()
    if args.mode.startswith("prepare"):
        request = make_request(args.output)
        if args.mode == "prepare-no-relic":
            request["cells"] = [c for c in request["cells"] if c["id"].startswith("pvp-early-")]
            for cell in request["cells"]:
                cell["id"] += "-without-relic"
                for side in ["reference", "candidate", "opponent"]:
                    cell[side]["equipment"] = [item for item in cell[side]["equipment"] if item["slot"] != "Relic"]
        args.path.parent.mkdir(parents=True, exist_ok=True)
        with args.path.open("x", encoding="utf-8") as stream:
            json.dump(request, stream, indent=2)
            stream.write("\n")
    else:
        summarize(args.path)
