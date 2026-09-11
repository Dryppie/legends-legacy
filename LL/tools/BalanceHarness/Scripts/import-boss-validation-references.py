"""Import the sealed fixed Nhalia validation without combat or historical edits.

Usage: python import-boss-validation-references.py PACKAGE NEW_OUTPUT
The package manifest and every analysis input are verified before writing.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import runpy

PACKAGE_ID = "nhalia-validation-20260911"
MANIFEST = "d94ad2683d159101987cf5f6e5338a58620fbf929ad34f38ca759be7bc4d103f"
RECIPES = {
    "anchor": "a4eaec8b4a9ab78088b9a6e5a672bfdf0a00bab29264d2a3694b2b1f22aa2a18",
    "previous-primary": "7f48c238712abd89e1af15b37b53e94d27255712dfc0316ba0f955ec9393744e",
    "previous-exploratory": "813457e85da488606bcf34f12b02ec63c67e3966d9aaeb9bcc727239cf2103d1",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    if args.output.exists():
        raise ValueError("Choose a new output path; existing artifacts are never overwritten.")
    manifest_bytes = (args.package / "checksums.json").read_bytes()
    if hashlib.sha256(manifest_bytes).hexdigest() != MANIFEST:
        raise ValueError("Validation manifest differs from the pinned sealed experiment.")
    manifest = {row["path"]: row["sha256"] for row in json.loads(manifest_bytes)}
    consumed = {}

    def verify(relative):
        if Path(relative).is_absolute() or ".." in Path(relative).parts:
            raise ValueError("Source must remain within the sealed package.")
        data = (args.package / relative).read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        if manifest.get(relative) != digest:
            raise ValueError(f"Source differs from the sealed manifest: {relative}")
        consumed[relative] = digest
        return data

    def read(relative):
        return json.loads(verify(relative))

    summary = read("analysis/summary.json")
    if summary["status"] != "Complete":
        raise ValueError("Validation is not complete.")
    for relative, expected in summary["inputSha256"].items():
        verify(relative)
        if consumed[relative] != expected:
            raise ValueError(f"Analysis input differs from its frozen identity: {relative}")
    protocol = read("protocol.json")
    fixed = read("fixed-recipes.json")
    ledger = read("seed-ledger.json")
    scope = read("scope.json")
    source = read("source-provenance.json")
    definition = read("historical-sources/studies/floor-13-slots-7/definition.json")
    if fixed != [{"name": name, "partyId": party} for name, party in RECIPES.items()] or protocol["recipes"] != fixed:
        raise ValueError("The three fixed recipes or their declared roles changed.")
    if protocol["samples"] != 100 or protocol["floor"] != 13 or protocol["essenceSlots"] != 7:
        raise ValueError("The fixed target-only validation budget changed.")
    seeds = ledger["target"]["13"]
    if len(seeds) != 100 or len(set(seeds)) != 100 or set(seeds) & set(ledger["excludedCombatSeeds"]):
        raise ValueError("Validation needs 100 unique paired seeds disjoint from every historical exclusion.")
    scenario_hash = runpy.run_path(str(Path(__file__).with_name("import-boss-refinement-references.py")))["scenario_hash"]
    evidence = []
    fixed_fingerprint = None
    for name, party_id in RECIPES.items():
        row = next(r for r in summary["recipes"] if r["name"] == name)
        recipe_path = f"recipes/{name}.json"
        recipe = read(recipe_path)
        old_path = f"historical-sources/recipe-library/floor-13-slots-7/{party_id}.json"
        original = read(old_path)
        comparable = copy.deepcopy(recipe)
        comparable["seeds"] = original["seeds"]
        if comparable != original or recipe["seeds"] != seeds:
            raise ValueError("Validation changed a fixed recipe field other than its paired seed schedule.")
        builds = {str(p["partySlot"]): p["build"]["essenceIds"] for p in recipe["party"]}
        if scenario_hash(builds) != party_id or row["partyId"] != party_id:
            raise ValueError("Recipe identity differs from the frozen party.")
        identity = copy.deepcopy(sorted(recipe["party"], key=lambda p: p["partySlot"]))
        for member in identity:
            member["build"]["essenceIds"] = []
        fingerprint = scenario_hash(identity)
        if fixed_fingerprint is None:
            fixed_fingerprint = fingerprint
        if fingerprint != fixed_fingerprint:
            raise ValueError("The recipes do not share exact identities, gear and training.")
        outcomes = row["outcomes"]
        if [outcome["seed"] for outcome in outcomes] != seeds or len(outcomes) != 100:
            raise ValueError("Outcome order differs from the frozen paired schedule.")
        wins = sum(outcome["outcome"] == "Victory" for outcome in outcomes)
        defeats = sum(outcome["outcome"] == "Defeat" for outcome in outcomes)
        draws = sum(outcome["outcome"] == "Draw" for outcome in outcomes)
        if (wins, defeats, draws) != (row["wins"], row["defeats"], row["draws"]) or wins + defeats + draws != 100:
            raise ValueError("Summary outcome counts differ from all 100 trials.")
        relevant = {path: digest for path, digest in consumed.items()
                    if path in ("analysis/summary.json", "protocol.json", "seed-ledger.json", recipe_path, old_path)
                    or path.startswith(f"runs/{name}/")}
        evidence.append({
            "id": party_id,
            "role": name,
            "label": f"Historical fixed validation: {wins}/100 target clears; {name}; target floor 13 only",
            "targetRecipe": recipe,
            "targetRecipeHash": scenario_hash(recipe),
            "wins": wins,
            "defeats": defeats,
            "draws": draws,
            "outcomes": outcomes,
            "sourceHashes": relevant,
        })
    catalog = {
        "schemaVersion": 1,
        "id": "nhalia-fixed-validation-20260911",
        "floor": 13,
        "budget": definition["budget"],
        "mutablePartySlots": list(range(1, 11)),
        "fixedPartyFingerprint": fixed_fingerprint,
        "seeds": seeds,
        "excludedCombatSeeds": sorted(set(ledger["excludedCombatSeeds"]) | set(seeds)),
        "evidence": evidence,
        "provenance": {
            "packageId": PACKAGE_ID,
            "manifestSha256": MANIFEST,
            "interpretation": "Three existing exact recipes and all 100 paired seeds were fixed before outcomes. "
                "These are separate historical target-only observations, not additional all-floor confirmation or a search. "
                "The 20/40-sample observations, all transfer weaknesses and discovery-frozen anchors remain unchanged. "
                "Do not pool samples, promote a new search anchor, or count replayed trials as independent evidence. "
                "Healing is reported attribution; regeneration is effective restoration and neither proves causal benefit.",
            "scope": scope,
            "sourceHashes": dict(consumed),
        },
    }
    if source["sourceManifestSha256"] != "942dd47aa38a8125a518291e57bad0b291e4fa9e11bb26b7bafbb1c1f18e5b7e":
        raise ValueError("Historical refinement source identity changed.")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    data = (json.dumps(catalog, ensure_ascii=True, indent=2) + "\n").encode()
    with args.output.open("xb") as stream:
        stream.write(data)
    print(json.dumps({"output": str(args.output), "sha256": hashlib.sha256(data).hexdigest(),
                      "recipes": len(evidence), "independentPairedSeeds": len(seeds),
                      "excludedIntegers": len(catalog["excludedCombatSeeds"]),
                      "verifiedSourceFiles": len(consumed), "sourceManifest": MANIFEST}))


if __name__ == "__main__":
    main()
