# Tower loadout search foundation — 10 September 2026

The first implementation increment of the [loadout search plan](Essence-Loadout-Search-Plan.md) now prepares legal, ordered alternatives at fixed budgets and exports an inspectable mechanics inventory. It adds the offline command `tower-loadout-prepare`. It does not rank new loadouts, run the planned optimization pilot, or change the existing dashboard search button.

## Main finding: Essence order matters

Six character/floor contexts were evaluated in original and reversed Essence order, with three matching seeds each: **18 paired probes, 36 saved combat executions**. Reversal affected only the chosen character's equipped Essence list. Character and equipment identities, allies, budgets, encounter identity and combat seeds remained fixed.

**12/18 probes changed observable gameplay; one changed the outcome.** On floor 1, reversing the Restorer's four Essences with seed `-12345` changed Defeat at 196.1 seconds to Victory at 195 seconds. This is a diagnostic counterexample to general order equivalence, not a claim that reversal usually helps or that the reversed build is optimal.

| Target | Floor | Gameplay changed / probes | Outcomes changed |
| --- | --- | --- | --- |
| Guardian | 1 | 0/3 | 0 |
| Restorer | 1 | 3/3 | 1 |
| First Striker | 1 | 3/3 | 0 |
| Controller | 1 | 3/3 | 0 |
| Guardian | 10 | 0/3 | 0 |
| Guardian | 15 | 3/3 | 0 |

The executor enumerates equipped Essences while assembling abilities, and `FastCombatEngine.UseReadyActiveAbilities` iterates the actor's ability list. This provides a source-level reason to investigate ordering. The empirical comparison ignores descriptive ability-array ordering and checks outcomes, duration, terminal states and per-entity numerical contributions. Matching probes do not prove order independence for their role or every future build. **Future candidates must preserve slot order; unordered-set deduplication would lose distinct behavior.**

## Identity correction and compatibility

The initial identity test caught that `EquipmentReferenceBuildFactory` derives character, equipment and Essence-instance IDs from the serialized build, including equipped Essence IDs. Keeping the recipe name constant did not isolate a loadout change.

The reference recipe now has an optional `IdentityEssenceIds` vector. The foundation pins it to the control's original Essence list, so substitutions and permutations retain the control's character/item/slot-instance IDs. Actual equipped Essences still determine validation, attributes and abilities. Identity vectors must match the equipped count. The foundation keeps levels, equipment and other recipe fields fixed, and includes the full scenario, ordered candidate, content, settings and execution identity in candidate keys.

When the new field is absent, serialization omits it and the historical identity calculation is preserved exactly. Tests verify legacy serialization and unchanged control materialization. A real **600-battle historical anchor archive** was read and compared against itself using the new executable: **zero changed records**. Old battle replay still requires each archive's original executable; this compatibility check is not a new set of gameplay samples.

## Legal preparation and mechanics inventory

The default contract has **165 contexts**:

- 60 progression contexts: all 15 floors, targeting Guardian, Restorer, first Striker and Controller individually in the existing balanced party.
- 105 controlled contexts: all 15 floors × Essence counts 4–10, targeting the Guardian in the existing level-90 slot-count cohort.

Other participants stay fixed, including the second Striker and repeated cells. This tests per-character preparation; joint party optimization remains future work. The two cohorts keep their existing gear/level assumptions and are not pooled into a progression score. The entry curve uses Uncommon Standard rank-1 gear; other intended Standard/Fine rank-1–2 comparisons belong to subsequent experiments.

Each context declares an allowed pool, ownership assumption, target slot and optional zero-based pinned slots. Production reference construction and Tower preparation enforce slot unlocks, distinct source families, equipment legality and RequiredSlots. Current content has **80 Essence definitions from 77 families**, and all 80 definition families match the normal saved-loadout source mapping through creature loot tables. The allowed-pool contract assumes declared ownership; it does not fetch inventories or simulate acquisition. Invalid/duplicate pool IDs, missing control Essences, invalid pins, unsupported progression/order policies and unknown JSON fields fail explicitly.

The default generates four bounded proposals per context: the control, a reversed-order probe and seeded single-slot substitution probes, with exact ordered duplicates removed. Rejected proposals count toward the cap. **660 proposals produced 652 accepted/prepared scenarios and eight recorded family-rule rejections.** These are legality probes, not 652 quality measurements. Every accepted scenario is exported for the existing Tower runner; candidate rows include rejection reasons and frozen input hashes.

The mechanics inventory resolves both abilities for all 80 definitions through production catalog validation/compiler checks. JSON retains full structured abilities, triggers, conditions, targets, scaling/cooldowns, definition progression metadata and shared status/summon catalogs. Markdown lists direct mechanic signals and source families. Source hashes detect changed catalogs. Signals are not power scores; transitive effects, final equipment-modified values and runtime-only interactions remain explicitly unassessed and do not exclude an Essence from exploration. No description/name parsing or per-Essence hardcoded scoring was added.

## Use and artifacts

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-loadout-prepare --output TestResults/balance/tower-loadout-foundation-new
```

Optional `--definition <json>` loads a strict custom contract; use the generated `definition.json` as its editable starting point. `--content-root` selects API content and `--catalogs-root` selects the existing catalogs used to generate the default. Custom definitions contain complete Tower scenarios; generated definitions fix target identity vectors automatically. Only level-1 unascended, unevolved progression is supported.

Limits: at most 256 contexts, 2–100 proposals per context and 4,096 total proposals; at most eight audit contexts with five distinct seeds each. These bound work but are not runtime or memory guarantees. Execution is sequential and cancellable. Partial reports and already exported scenarios/audit bundles remain saved; no resume or optimization cache is implemented. Audit reversals are explicit diagnostic interventions and may violate candidate pins; they are not exported as accepted recommendations on that basis.

Each new output folder contains `definition.json`, selected nonsecret `settings.json`, `scope.json`, frozen `content/`, `mechanics.md/json`, accepted `recipes/`, `foundation.md/json`, `foundation-files.json` checksums and the original/reversed `order-audit/` Tower bundles. Replaying either audit orientation uses the existing `replay --run <bundle> --battle tower.0001 --detailed` command. For new measurements, exported recipes use `tower --scenario <recipe.json> --content-root LL/src/API/API.LL --output <new-directory>` against current API content/settings. Recipe export alone does not pin execution/content; use saved audit bundles and their retained executable for verified historical replay. The foundation's content folder intentionally contains no full application settings file.

## Verification and remaining work

```powershell
./build/run-tests.ps1 -Configuration TowerLoadoutFoundationFinal -Filter 'FullyQualifiedName~BalanceHarnessTower'
./build/run-tests.ps1 -NoBuild -Configuration TowerLoadoutFoundationFinal
```

**99 Tower tests passed**, including seven independent persisted normal-Tower checks with reversed Essence lists and pinned identities, all-floor/count preparation, identity/default-serialization invariants, source variants, pools/pins, invalid fields/budgets, deterministic candidates, cancellation, catalog fingerprint changes and replayable artifacts. The shared reference-factory change warranted the full suite: **all 2,514 backend tests passed**, zero failed/skipped. Build produced 33 existing warnings. The initial failed identity assertion was fixed before final verification; no command remains blocked.

**14 audit replays matched**, including both orientations of the outcome-changing seed. All **975 foundation artifact checksums** verified. A reduced strict-definition CLI roundtrip repeated two diagnostic fights and reproduced the candidate IDs and prepared-input hashes. An exported scenario then executed through the normal `tower` command and its detailed replay also matched (**15 matching replays total**). These extra technical checks are not additional independent quality evidence.

Evidence is retained at `TestResults/balance/tower-loadout-foundation-20260910/`, including executable/source/catalog snapshots, both test logs/TRX, preparation run, mechanics reports, audit bundles, replay index, verification script, definition/export roundtrip and historical compatibility check. Accepted starter evidence and unrelated working-tree changes remain intact.

Changed files: `TowerLoadoutFoundation.cs`, `EssenceMechanicsInventory.cs`, `Program.cs`, the opt-in reference recipe/factory identity field, `BalanceHarnessTowerLoadoutFoundationTests.cs`, Tower parity tests, the loadout/main plans, README and this review.

Next implement the four-slot search pilot: diverse ordered whole-loadout starts, single/double substitutions and order mutations, fixed ally contexts, a recorded battle budget, equal-cost random-search comparison and frozen unused-seed confirmation. Foundation coverage is not completion of this optimization stage. Broader ownership/training budgets, transitive mechanic inference, joint party search and dashboard controls remain open. Phase 2 integration stays deferred; no Tower target, boss tuning, production configuration, migrations or deployments changed.
