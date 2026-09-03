# Equipment reference builds

Updated: 3 September 2026. Target: the main LL game's detached combat tooling.

The reference command materializes the current Tier 1 equipment progression catalog through the live evaluator, calculates Combat Rating, and runs each build through production combat preparation and simulation. It implements correctness and inspection support using existing provisional values. It does not tune content or certify readiness thresholds.

## Run

From the repository root:

```powershell
dotnet run --project LL/tools/LegendsLegacy.Balance --configuration Release -- --equipment-reference-builds --seed 1337 --output "$env:TEMP/equipment-reference"
```

After a successful build, add `--no-build` before the `--` separator to reuse it. Optional arguments are `--seed`, `--content-root` (the API.LL directory), `--output`, and `--help`. Output defaults to `balance-output/equipment-reference-builds.json`.

Use the direct command above. The existing `build/run-balance.ps1` wrapper appends `--full`, which belongs to the legacy balance pipeline; the equipment progression mode rejects that combination and calibration/optimizer options. Existing legacy reports and reference ladders retain their previous meaning.

## Reference contract

[Fixtures](../../LL/tools/LegendsLegacy.Balance/Fixtures/equipment-reference-builds.v1.json) define twelve complete level-10, Tier-1, rank-0 loadouts. The runner expands each across ranks 0–5, producing 72 builds:

- Plain and styled balanced builds; offense, sustain, shield defense and area builds.
- Dual wielding and ranged weapons.
- A named weapon with its native style, an applied replacement style, and a cleared style.
- Equipment-only projection with no Essences.

The fixture level and Essence choices remain fixed across each rank comparison. All builds fight a fresh copy of the same plain, rank-0 balanced reference opponent on a common seed. This synthetic Arena-mode matchup exercises the production engine; it does not evaluate real areas, dungeon progression, raids, or economy pacing. Combat is bounded at 1,800 ticks.

[EquipmentReferenceBuildFactory](../../LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs) accepts explicit slot/identity/style selections, tier, rank, level and Essence IDs. It rejects unsupported catalog tiers, invalid ranks, illegal or incomplete hands/slots, incompatible styles, unknown Essences, repeated monster families and locked Essence slots. A two-handed weapon fills both hand slots with one instance and contributes stats once; dual wielding creates distinct instances even for identical definitions.

For a selection, `activeStyleId` applies that style. With no explicit style, `useNativeStyle` defaults to true; set it false to represent clearing a named item's style. The frozen descriptor always preserves native identity, style and rarity independently of the active style.

Equipment is detached, bound to a deterministic synthetic owner and marked with `offline-reference-build` provenance. The factory does not run crafting rolls or Tempering, grant player inventory, charge currency, or invent paid rank receipts/salvage value. Item bases and set definitions come from the existing content provider; canonical stats and weapon behavior come from equipment progression descriptors.

## Tier 2 transition mode

Add `--region-two-transition` to generate `equipment-region-two-transition.json`. This mode uses both tiers, ranks 0–5 and level 50: 144 builds against a fixed Tier 1 rank-5 balanced opponent. Tier 1 reference mode remains unchanged. The factory now enforces the equipment tier's character-level requirement as well as catalog support.

See [Meran progression](../design/equipment-region-two-progression.md) for prices, salvage/replacement arithmetic and the measured limitations. Most seed-1337 transition fights reached the 1,800-tick limit, so this remains a synthetic compatibility check. Use the separate PvE mode below for actual authored encounters.

## Meran PvE mode

```powershell
dotnet run --project LL/tools/LegendsLegacy.Balance --configuration Release -- --equipment-reference-builds --meran-pve --trials 128 --essence-level 30 --dungeon-level 65 --seed 1337 --output "$env:TEMP/ll-meran-pve"
```

This writes `equipment-meran-pve.json`. Six complete Shenic equipment/Essence fixtures fight four Meran areas and all eighteen dungeon combat-room templates with Tier 1 rank 5 and Tier 2 ranks 0, 3 and 5. It uses production creature abilities/scaling, weighted ordinary spawns, the live 6,000-tick limit and initial cooldowns. Seeds and enemy rosters match across gear/build comparisons. Each case records outcomes, actual earned Cinders and victory-adjusted ordinary economy estimates; dungeon room wins do not invent completion rewards.

`--trials` accepts 1–1,024 (default 32), `--essence-level` accepts 10 or 30 (default 10, first ascension at 30), and `--dungeon-level` accepts 50–65 (default 50). Ordinary areas always use their entry levels. These options require `--meran-pve`, which is mutually exclusive with `--region-two-transition`. `--content-root` can point at a separate temporary content copy for candidate testing.

This report has its own schema: exact fixture inputs, explicit case progression, individual trials and hashes of all content JSON plus the fixture. It does not use the synthetic opponent or the reference report's Combat Rating fields. Dungeon rooms start fresh without optional run modifiers; results do not predict complete runs. See the [Meran PvE assessment](../design/equipment-meran-pve-balance-report.md) for 270,336 evaluated fights, the Tangled Cave III adjustment, income results and acceptance limits.

## Report and limits

The JSON includes the report version, equipment balance version, Combat Rating version, fixture SHA-256, seed, exact build inputs, frozen equipment descriptors and occupied slots, prepared combat attributes, and combat outcomes/damage/healing. Combat Rating retains the production permanent-stat contract; prepared attributes also include applicable combat preparation and set effects. Rank/style are explicit fields, with no fabricated Quality or Tempering progression metadata.

The same fixtures, production content, engine code and seed produce the same report. The fixture hash identifies the fixture file only; it is not a hash of all production content. Frozen item descriptors preserve the equipment inputs used by that run.

Regression coverage verifies all 72 loadouts, native/replaced/cleared style identity, descriptor stats, invalid inputs, distinct hand behavior, persisted-snapshot equivalence, deterministic combat reports and incompatible CLI options. Run backend tests through `build/run-tests.ps1`.

The legacy optimizer, World Tower gear packages, calibration targets and existing historical reports still use their original equipment assumptions. Migrating their policies and collecting whole-loop evidence remains separate work after consumer/source integration. Current implementation and balance work are tracked in the [status document](../design/equipment-implementation-status.md). The earlier [implementation review](../design/equipment-implementation-review.md) is historical; Alpha conversion is no longer planned. The reference tool does not mutate game state.
