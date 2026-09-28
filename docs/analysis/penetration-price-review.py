"""Prepare and summarize bounded offline penetration-price experiments; never edit game data."""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FIXTURES = ROOT / "LL/tools/BalanceHarness/Fixtures"
PRICES = [1.5, 3, 4, 6, 8]


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def write(path, value):
    with Path(path).open("x", encoding="utf-8") as output:
        json.dump(value, output, indent=2)
        output.write("\n")


def fixture(name, cell):
    return copy.deepcopy(next(x for x in read(FIXTURES / f"attribute-allocation-{name}.json")["cells"] if x["id"] == cell))


def weapon(build, name, specialization=None):
    suffix = f".spec.{specialization}" if specialization else ""
    build["equipment"] = [x for x in build["equipment"] if x["slot"] not in ("MainHand", "OffHand")]
    build["equipment"].append(dict(slot="MainHand", definitionId=f"plain.{name}{suffix}.rarity.epic", useNativeStyle=False))
    if name == "mace":
        build["equipment"].append(dict(slot="OffHand", definitionId="plain.towershield.rarity.epic", useNativeStyle=False))
    elif name == "wand":
        build["equipment"].append(dict(slot="OffHand", definitionId="plain.grimoire.rarity.epic", useNativeStyle=False))


def make_cells():
    cells = []
    basic = fixture("basic-without-relic", "no-relic-basic-maul-speed")
    dot = fixture("matched", "matched-dot-precision")
    conduit = fixture("matched", "matched-ten-essence-precision")
    control = fixture("roles", "control-precision")
    roles = [("maul-basic", basic, "maul", "physical", ["precision", "speed"]),
             ("mace-basic", basic, "mace", "physical", ["precision", "speed"]),
             ("maul-active", control, "maul", "physical", ["precision", "speed", "haste"]),
             ("staff-dot", dot, "staff", "magical", ["precision", "haste"]),
             ("staff-conduit", conduit, "staff", "magical", ["precision", "haste"]),
             ("wand-conduit", conduit, "wand", "magical", ["precision", "haste"])]
    for role, template, arm, channel, alternatives in roles:
        for defense in ["light", "medium", "heavy"]:
            reference = copy.deepcopy(template["reference"])
            weapon(reference, arm)
            opponent = copy.deepcopy(reference)
            opponent["id"] = f"{role}-{defense}-opponent"
            weapon(opponent, arm, "precision")  # Fixed non-penetration opposing offense at every price.
            for item in opponent["equipment"]:
                part = {"Head": {"light": "light_hood", "medium": "medium_helm", "heavy": "heavy_helm"},
                        "Chest": {"light": "light_vest", "medium": "medium_mail", "heavy": "heavy_breastplate"},
                        "Legs": {"light": "light_leggings", "medium": "medium_greaves", "heavy": "heavy_legplates"}}
                if item["slot"] in part:
                    spec = f".spec.{'armor' if channel == 'physical' else 'resistance'}" if defense == "heavy" else ""
                    item["definitionId"] = f"plain.{part[item['slot']][defense]}{spec}.rarity.epic"
            for alternative in alternatives:
                cell = copy.deepcopy(template)
                group = f"{role}-{defense}"
                cell.update(id=f"{group}-{alternative}", comparisonGroup=group,
                            reference=copy.deepcopy(reference), opponent=copy.deepcopy(opponent))
                cell["reference"]["id"] = f"{group}-penetration"
                cell["candidate"] = copy.deepcopy(cell["reference"])
                cell["candidate"]["id"] = f"{group}-{alternative}"
                weapon(cell["candidate"], arm, alternative)
                cells.append(cell)
    # Same contexts at early and maximum-quality progression, and with stacked ring penetration.
    for role in ["maul-basic", "staff-dot"]:
        for mode in ["common", "upgraded", "stacked"]:
            for source in [x for x in cells if x["comparisonGroup"] == f"{role}-medium"]:
                cell = copy.deepcopy(source)
                cell["id"] = source["id"].replace("-medium-", f"-{mode}-")
                cell["comparisonGroup"] = f"{role}-{mode}"
                for side in ["reference", "candidate", "opponent"]:
                    build = cell[side]
                    build["id"] += f"-{mode}"
                    if mode == "common":
                        build.update(characterLevel=30, tier=1)
                        for item in build["equipment"]:
                            suffix = ".rarity.common" if ".spec." in item["definitionId"] else ""
                            item["definitionId"] = item["definitionId"].replace(".rarity.epic", suffix)
                    elif mode == "upgraded":
                        build.update(rank=5, quality="Masterpiece", attributeRollMultiplier=1.05)
                    elif side != "opponent":
                        for item in build["equipment"]:
                            if item["slot"] == "Ring":
                                channel = "physical" if role == "maul-basic" else "magical"
                                item["definitionId"] = f"plain.band.spec.{channel}_penetration.rarity.epic"
                cells.append(cell)
    # Existing full-health PvE fixtures: compare identical weapons and ordered Essences.
    for prefix, arm, alternatives in [("early-basic-idle", "shortsword", ["precision", "speed"]),
                                       ("four-essence-dungeon", "shortsword", ["precision", "speed"]),
                                       ("ten-essence-region2", "staff", ["precision", "haste"])]:
        for alternative in alternatives:
            cell = fixture("current", f"{prefix}-{alternative}")
            cell["id"] = f"review-{cell['id']}"
            cell["comparisonGroup"] = f"review-{prefix}"
            if arm == "shortsword":
                for item in cell["reference"]["equipment"]:
                    if item["slot"] == "MainHand":
                        item["definitionId"] = "plain.shortsword.spec.physical_penetration.rarity.common"
            cells.append(cell)
    for alternative in ["precision", "haste"]:
        cell = fixture("matched", f"party-support-{alternative}")
        cell["id"] = f"review-{cell['id']}"
        cell["comparisonGroup"] = "review-party-support"
        cells.append(cell)
    assert len(cells) == 59 and len({x["id"] for x in cells}) == len(cells)
    return cells


def prepare(output):
    output.mkdir(parents=True, exist_ok=True)
    cells = make_cells()
    write(output / "cells.json", cells)
    source = ROOT / "LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentStatBudgetCatalog.cs"
    write(output / "protocol.json", dict(prices=PRICES, cells=len(cells),
          explorationSeeds=list(range(110001, 110013)), screeningCheckSeeds=[111001],
          heldOutExplorationSeeds=list(range(120001, 120009)), heldOutConfirmationSeeds=list(range(121001, 121065)),
          sourcePriceHash=hashlib.sha256(source.read_bytes()).hexdigest(),
          objective="Identify a useful anti-defense specialization without universal superiority; no automatic price promotion.",
          selection="Use screening exploration only; select at most two higher prices for fresh held-out confirmation alongside 1.5. Equal context weights; report separate physical/magical results and W/L/D, not pooled fight counts.",
          limits="Legal simplified PvP and fixed full-health PvE rooms; light/medium/heavy change health/offense too. All comparisons within a cell share the same opponent. Both penetration prices vary together. No population or attrition acceptance.",
          maximumScreeningBattles=sum(4 if x.get("opponent") else 2 for x in cells)*13*len(PRICES)))
    for price in PRICES:
        request(output, cells, price, "screen", list(range(110001, 110013)), [111001])


def request(output, cells, price, phase, exploration, confirmation):
    label = str(price).replace(".", "_")
    write(output / f"{phase}-price-{label}.json", dict(schemaVersion=1,
          contentRoot=str(ROOT / "LL/src/API/API.LL"), outputDirectory=str(output / f"{phase}-price-{label}"),
          explorationSeeds=exploration, confirmationSeeds=confirmation, cells=cells,
          maximumBattles=sum(4 if x.get("opponent") else 2 for x in cells)*(len(exploration)+len(confirmation)),
          referenceRulesVersion=18, syntheticFutureProjection=False))


def summarize(output, phase):
    rows = []
    contexts = {x["id"]: x["comparisonGroup"] for x in read(output / "cells.json")}
    selected_phase = "exploration" if phase == "screen" else "confirmation"
    for folder in sorted(output.glob(f"{phase}-price-*")):
        if not folder.is_dir() or not (folder / "trials.json").exists():
            continue
        prices = folder.name.split("price-")[1].replace("_", ".")
        trials = [x for x in read(folder / "trials.json") if x["phase"] == selected_phase]
        for cell in sorted({x["cell"] for x in trials}):
            group = [x for x in trials if x["cell"] == cell]
            row = dict(price=float(prices), cell=cell, context=contexts[cell], phase=selected_phase, fightsPerBuild=len(group))
            for side in ["reference", "candidate"]:
                wins = sum(x[side]["engineOutcome"] == ("Defeat" if x["mirrored"] else "Victory") for x in group)
                draws = sum(x[side]["engineOutcome"] == "Draw" for x in group)
                row[side] = dict(wins=wins, draws=draws, losses=len(group)-wins-draws,
                                 meanTicks=sum(x[side]["durationTicks"] for x in group)/len(group))
            row["alternativeWinDeltaPp"] = 100*(row["candidate"]["wins"]-row["reference"]["wins"])/len(group)
            rows.append(row)
    write(output / f"{phase}-summary.json", rows)
    for price in sorted({x["price"] for x in rows}):
        for prefix in ["maul", "mace", "staff", "wand", "review"]:
            group = [x for x in rows if x["price"] == price and x["cell"].startswith(prefix)]
            context_means = [sum(x["alternativeWinDeltaPp"] for x in group if x["context"] == context)
                             / sum(x["context"] == context for x in group)
                             for context in {x["context"] for x in group}]
            mean = sum(context_means)/len(context_means)
            print(f"price={price:g}, {prefix}: {len(group)} cells, alternative mean win change {mean:+.2f} pp; "
                  f"pen/alt/tie cells {sum(x['alternativeWinDeltaPp']<0 for x in group)}/"
                  f"{sum(x['alternativeWinDeltaPp']>0 for x in group)}/{sum(x['alternativeWinDeltaPp']==0 for x in group)}")


def verify(output):
    protocol = read(output / "protocol.json")
    source = ROOT / "LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentStatBudgetCatalog.cs"
    assert hashlib.sha256(source.read_bytes()).hexdigest() == protocol["sourcePriceHash"]
    content = None
    battles = 0
    gear_checks = 0
    evidence = []
    for phase, prices in [("screen", PRICES), ("confirm", read(output / "confirmation-selection.json")["prices"])]:
        for price in prices:
            label = f"{price:g}".replace(".", "_")
            folder = output / f"{phase}-price-{label}"
            request_data = read(folder / "request.json")
            manifest = read(folder / "manifest.json")
            if content is None:
                content = manifest["contentHashes"]
            assert content == manifest["contentHashes"], f"Content drift: {folder}"
            trials = read(folder / "trials.json")
            assert len(trials)*2 == request_data["maximumBattles"]
            expected = {(cell["id"], stage, seed, mirror)
                        for cell in request_data["cells"]
                        for stage, seeds in [("exploration", request_data["explorationSeeds"]), ("confirmation", request_data["confirmationSeeds"])]
                        for seed in seeds for mirror in ([False, True] if cell.get("opponent") else [False])}
            actual = [(x["cell"], x["phase"], x["seed"], x["mirrored"]) for x in trials]
            assert len(actual) == len(set(actual)) and set(actual) == expected
            for cell in request_data["cells"]:
                builds = read(folder / f"{cell['id']}.builds.json")
                for side in ["reference", "candidate"]:
                    for item in builds[side]["equipment"]:
                        data = item["data"]
                        allocation = data["allocation"]
                        assert allocation["statVersion"] == 18
                        assert math.isclose(allocation["core"]+allocation["specialization"]+allocation["styleStats"]+allocation["reservedIdentity"], allocation["total"])
                        scale = 15.2**((data["state"]["tier"]-1)/9)
                        for stat in ["ArmorPenetration", "MagicPenetration"]:
                            points = data["stats"].get(stat, 0)
                            assert 0 <= points <= 40
                            if points and not allocation["styleStats"]:
                                expected_points = min(40, allocation["specialization"]/scale/price)
                                assert abs(points-expected_points) <= .0051, (cell["id"], price, points, expected_points)
                                expected_power = (allocation["core"] + max(0, allocation["specialization"] - expected_points*scale*price))/22.5
                                assert abs(data["stats"]["Power"]-expected_power) <= .0051
                                gear_checks += 1
            battles += len(trials)*2
            evidence.append(dict(run=folder.name, battles=len(trials)*2, domain=manifest["execution"]["assemblyHashes"]["Domain"],
                                 trialsSha256=hashlib.sha256((folder / "trials.json").read_bytes()).hexdigest()))
    write(output / "verification.json", dict(battles=battles, penetrationItemChecks=gear_checks,
          unchangedPriceSource=True, identicalContent=True, exactSchedules=True, evidence=evidence))
    print(f"Verified {battles} fights, {gear_checks} penetration allocations including cap overflow, content and schedules; production price source unchanged.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("action", choices=["prepare", "confirm", "summarize", "verify"])
    parser.add_argument("output", type=Path)
    parser.add_argument("--prices", nargs="+", type=float)
    parser.add_argument("--phase", choices=["screen", "confirm"], default="screen")
    args = parser.parse_args()
    folder = args.output.resolve()
    if args.action == "prepare":
        prepare(folder)
    elif args.action == "summarize":
        summarize(folder, args.phase)
    elif args.action == "verify":
        verify(folder)
    else:
        assert args.prices and len(args.prices) <= 3 and all(x in PRICES for x in args.prices)
        write(folder / "confirmation-selection.json", dict(prices=args.prices,
              sourceSummarySha256=hashlib.sha256((folder / "screen-summary.json").read_bytes()).hexdigest()))
        for price in args.prices:
            request(folder, read(folder / "cells.json"), int(price) if price.is_integer() else price,
                    "confirm", list(range(120001, 120009)), list(range(121001, 121065)))
