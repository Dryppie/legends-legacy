"""Rebuild the portable catalog from the sealed refinement without running combat.

Usage: python import-boss-refinement-references.py PACKAGE OUTPUT
Every consumed archive file is checked against the pinned sealed manifest. OUTPUT
must be a new path; compare it with the tracked fixture when reproducing an import.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path

PACKAGE_ID = "tower-boss-refinement-20260910"
MANIFEST = "942dd47aa38a8125a518291e57bad0b291e4fa9e11bb26b7bafbb1c1f18e5b7e"
PILOT = "tower-boss-pilot-20260910"
PRIMARY = {
    7: "68bcf1597a0cba4906df7986ec66583bfb2b4036963b0ebfa7168a20362e3873",
    13: "7f48c238712abd89e1af15b37b53e94d27255712dfc0316ba0f955ec9393744e",
}


def scenario_hash(value):
    # These pinned scenario snapshots contain ASCII strings and integer numbers.
    # Match HarnessJson's sorted compact form and default System.Text.Json escaping.
    # Import checks this encoder against all 96 pilot recipes' native hashes.
    def encode(item):
        if isinstance(item, dict):
            return "{" + ",".join(encode(key) + ":" + encode(item[key]) for key in sorted(item)) + "}"
        if isinstance(item, list):
            return "[" + ",".join(encode(v) for v in item) + "]"
        if isinstance(item, str):
            if not item.isascii():
                raise ValueError("Use a native HarnessJson importer for non-ASCII scenario strings.")
            encoded = json.dumps(item).replace('\\"', '\\u0022')
            for character in "&'+<>":
                encoded = encoded.replace(character, "\\u" + format(ord(character), "04X"))
            return encoded
        if isinstance(item, float):
            raise ValueError("Use a native HarnessJson importer for noninteger scenario numbers.")
        return json.dumps(item)
    return hashlib.sha256(encode(value).encode()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Choose a new output path; existing artifacts are never overwritten.")
    manifest_bytes = (args.package / "checksums.json").read_bytes()
    if hashlib.sha256(manifest_bytes).hexdigest() != MANIFEST:
        raise ValueError("Refinement manifest differs from the pinned sealed experiment.")
    checksums = {r["path"]: r["sha256"] for r in json.loads(manifest_bytes)}
    consumed = {}

    def read(relative):
        data = (args.package / relative).read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        if checksums.get(relative) != digest:
            raise ValueError(f"Source file differs from sealed manifest: {relative}")
        consumed[relative] = digest
        return json.loads(data)

    original = read("catalogs/tower-boss-references.json")
    if original["schemaVersion"] != 1 or original["provenance"]["packageId"] != PILOT:
        raise ValueError("The historical reference catalog is not the original pilot.")
    for entry in original["entries"]:
        for row in entry["evidence"]:
            if scenario_hash(row["targetRecipe"]) != row["targetRecipeHash"]:
                raise ValueError("Scenario canonical encoding differs from the native pilot hash.")
    entries = copy.deepcopy(original["entries"])
    excluded = set(original["excludedCombatSeeds"])
    scope = read("scope.json")
    library = {(r["study"], r["id"]): r for r in read("recipe-library/index.json")}
    for entry in entries:
        floor = entry["floor"]
        if floor == 8:
            entry["packageId"] = PILOT
            continue
        study = f"floor-{floor}-slots-{entry['budget']['essenceSlots']}"
        prefix = f"studies/{study}/"
        definition = read(prefix + "definition.json")
        report = read(prefix + "boss-search.json")
        selection = read(prefix + "selection.json")
        strategies = read(prefix + "strategy-archive.json")
        ledger = read(prefix + "seed-ledger.json")
        contexts = read(prefix + "contexts.json")
        if report["status"] != "Complete" or report["selection"] != selection or report["strategyArchive"] != strategies:
            raise ValueError("The complete report differs from its frozen selection.")
        expected_count = 42 if floor == 7 else 53
        if len(selection) != expected_count or selection[0]["source"] != "control":
            raise ValueError("Unexpected frozen finalist inventory.")
        excluded.update(ledger["excludedCombatSeeds"])
        for stage in ("discovery", "confirmation", "diagnostic"):
            for seeds in ledger[stage].values():
                excluded.update(seeds)
        prior = {row["id"]: row for row in entry["evidence"]}
        if not set(prior).issubset({party["id"] for party in selection}):
            raise ValueError("Refinement omitted a retained pilot recipe.")
        evidence = []
        for party in selection:
            party_id = party["id"]
            exported = library[(study, party_id)]
            recipe_path = exported["recipe"]
            confirmation_path = prefix + f"confirmation/{party_id}.json"
            recipe = read(recipe_path)
            if consumed[recipe_path] != exported["recipeHash"]:
                raise ValueError("Exported recipe file hash differs from its library index.")
            confirmation = read(confirmation_path)
            if confirmation != next(r for r in report["confirmation"] if r["id"] == party_id):
                raise ValueError("Confirmation record differs from complete report.")
            if {str(p["partySlot"]): p["build"]["essenceIds"] for p in recipe["party"]} != party["builds"]:
                raise ValueError("Exported exact recipe differs from frozen finalist.")
            targets = [cell for cell in confirmation["cells"] if cell["floor"] == floor]
            if len(recipe["seeds"]) != 40 or recipe["seeds"] != ledger["confirmation"][str(floor)]:
                raise ValueError("Target recipe must preserve its 40-sample confirmation schedule.")
            if targets[0]["clears"] != targets[1]["clears"] or targets[0]["trials"] != targets[1]["trials"]:
                raise ValueError("Target context aliases differ.")
            wins = sum(targets[0]["clears"])
            is_primary = exported["isDiscoveryPrimary"]
            if is_primary != (party_id == PRIMARY[floor]):
                raise ValueError("Discovery primary identity changed.")
            origins = [
                {"method": arm["method"], "generationSeed": arm["seed"], "proposalSource": candidate["source"]}
                for arm in report["arms"] for candidate in arm["parties"] if candidate["id"] == party_id
            ]
            if not origins:
                raise ValueError("Frozen finalist has no discovery origin.")
            paths = [prefix + "definition.json", prefix + "selection.json", prefix + "boss-search.json",
                     prefix + "strategy-archive.json", prefix + "seed-ledger.json", "recipe-library/index.json",
                     confirmation_path, recipe_path]
            row = {
                "id": party_id,
                "label": f"Historical refinement: {wins}/40 target clears; " + (
                    "discovery-frozen primary; fresh validation required for new plans" if is_primary else
                    "descriptive finalist observation; not the discovery-frozen primary"),
                "originalSource": party["source"],
                "wasPilotControl": prior.get(party_id, {}).get("wasPilotControl", False),
                "strategyLabels": [s["intent"] for s in strategies if s["id"] == party_id],
                "discoveryOrigins": origins,
                "targetRecipe": recipe,
                "targetRecipeHash": scenario_hash(recipe),
                "confirmation": confirmation,
                "sourceHashes": {path: consumed[path] for path in paths},
                "wasDiscoveryPrimary": is_primary,
            }
            if party_id in prior:
                row["priorObservations"] = [{"packageId": PILOT, "evidence": prior[party_id]}]
            evidence.append(row)
        anchor = next(row for row in evidence if row["wasDiscoveryPrimary"])
        entry.update({
            "referenceSetId": f"boss-refinement-20260910-{study}",
            "anchorId": anchor["id"],
            "anchorLabel": "Frozen refinement discovery primary. " + anchor["label"],
            "controls": selection,
            "evidence": evidence,
            "packageId": PACKAGE_ID,
            "contexts": contexts,
            "contextsHash": scenario_hash(contexts),
            "contextsSourcePath": prefix + "contexts.json",
        })
        if entry["budget"] != definition["budget"]:
            raise ValueError("Refinement changed the original fixed budget.")
    catalog = {
        "schemaVersion": 2,
        "provenance": {
            "packageId": PACKAGE_ID,
            "manifestSha256": MANIFEST,
            "interpretation": "All 95 refinement finalists and 35 unchanged Kodoku pilot finalists are retained. "
                "The 40-sample refinement and earlier 20-sample pilot observations remain separate historical experiments, "
                "with all 15 floors and both teammate contexts preserved. Refinement anchors are discovery-frozen primaries; "
                "the exploratory Nhalia 6/40 observation does not replace its 1/40 primary. Context aliases, equal outcomes "
                "and replays are not independent samples. No current-content success, optimum or causal synergy claim.",
            "scope": scope,
            "sourceHashes": dict(consumed),
        },
        "excludedCombatSeeds": sorted(excluded),
        "entries": entries,
        "priorProvenance": [original["provenance"]],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    data = (json.dumps(catalog, ensure_ascii=True, indent=2) + "\n").encode()
    with args.output.open("xb") as stream:
        stream.write(data)
    print(json.dumps({"output": str(args.output), "sha256": hashlib.sha256(data).hexdigest(),
                      "recipes": sum(len(e["controls"]) for e in entries), "excludedIntegers": len(excluded),
                      "verifiedSourceFiles": len(consumed), "sourceManifest": MANIFEST}))


if __name__ == "__main__":
    main()
