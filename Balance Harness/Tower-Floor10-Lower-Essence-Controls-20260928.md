# Floor-10 five-Essence controls and acquisition audit — 28 September 2026

**Subsequent implementation:** the [Tower supply path](Tower-Equipment-Supplies-Implementation-20260928.md) now supplies selectable preparation gear. See the [current continuation handoff](Tower-Continuation-Handoff-20260928.md) for resource/time and earned-inventory validation. This screen's results, hashes and no-gameplay-change scope remain historical.

**Completed and independently verified.** All **252 five-Essence controls won 0/32**, including all 126 level-50/tier-2 controls. The two strongest six-Essence references won **6/32** and **5/32**. All **8,736 fights** completed, with no new seeds or gameplay changes. This is a historical diagnostic, not fresh balance acceptance.

Target: the primary game's offline Balance Harness. Follow the [completed carried-equipment application](Tower-Carried-Equipment-Checked-Application-20260928.md) with controlled lower-Essence coverage on floor 10 and a static audit of the ordinary equipment acquisition channel. Keep the requested repeating equipment curve, supported search and applied boss values.

## Frozen screen

Retain all **21 floor-10 references** from the [fresh confirmation](Tower-Floor10-Family-Confirmation-20260928.md): authored, repeated and alternating parents, each with all seven gear profiles and all 15 characters. Source manifest: `95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944`.

For every parent/profile, remove each original Essence position 1–6 uniformly across the party. Preserve the order and instance identities of every surviving Essence, character identities, gear identities, specializations, positions, quality, rank and rolls. Compare the resulting five-Essence party at **level 50 / tier 2** (isolating the removal) and **level 40 / tier 1** (a legal earlier-progression budget). Tier 2 requires level 50, so the latter cannot retain tier-2 stats. Both use the same Legendary/Masterpiece/rank-5 assumptions; ownership remains hypothetical. The 126 controls per level plus 21 references give **273 cells**. These uniform removals are systematic local controls, not an exhaustive five-Essence search.

Use exactly the **first 32 historical seeds**, in order, from the 256-seed floor-10 confirmation panel. On a separate current build, first prepare all 273 cells without combat and reconstruct **all 5,376 original input hashes**. Replay the first 32 reports of every reference: **672 complete input/report matches**. Only after every match passes may the **252 controls × 32 = 8,064** additional historical diagnostic fights begin. Total **8,736 fights**, with zero fresh seeds, retries, extensions, recipe selection or boss tuning. The exclusion union remains **835,319**.

Record every cell's wins, draws, mean guardian health, duration, and gained/lost wins against its exact six-Essence parent on paired seeds. A control at **4/32 or more** is an observed lower-Essence concern, using the existing descriptive 10% screen threshold. Otherwise record no observed breach. Neither outcome establishes balance acceptance or universal Essence necessity; no historical results enter a new confidence estimate.

Bounds: **1,200 seconds** native preparation/combat/reconstruction, **1,260 seconds** owned process, **2 GiB** output, **8,736 maximum fights**. Compilation, ordinary tests and independent read-only audit are separate. Freeze current source/runtime/content hashes and the exact schedule before launch. Stop on failure, retain partial evidence and never resume or retry combat. Authenticate the complete archive and independently reconstruct recipes, paired outcomes, source report parity, acquisition arithmetic and process accounting afterward.

Pin the current floor file to `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`: floor 10 **12.71 / 7.13**, floor 11 **17.94375 / 23.0175**. Other captured content must match the confirmation. No gameplay content, settings, migration, database, deployment or restart changes are part of this screen.

## Acquisition scope

For each of the 21 original recipes, load the production ordinary-drop catalog and reinforcement prices. Check every exact gear definition belongs to the ordinary Legendary pool; count actual items and occupied slots; calculate rank-1→5 reinforcement costs from the authored tier prices. Report the probability of **any** Legendary/Masterpiece item per Champion equipment drop and per completion at mastery 0 and 10. The item-count/probability calculation intentionally ignores fit, specialization, duplicates, style, stat rolls and ownership. It describes an optimistic count for that channel, not the expected time to acquire the specified loadout.

This audit does not model all quest/event/trading channels, dungeon access, sigils, successful clears, resource income or player schedules. Rank reinforcement does not upgrade rarity or quality. Reference rolls fixed at 1 are evaluation assumptions, not a guaranteed random award. The screen therefore remains hypothetical ownership evidence even when all definitions have a legal ordinary drop path.

This protocol was written before any screen combat. The completed results and resulting recommendation are recorded below.

## Implementation and preparation

- [TowerEssenceAblation.cs](../LL/tools/BalanceHarness/TowerEssenceAblation.cs) creates the complete set of ordered removals while preserving surviving identities. The [reference builder](../LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs) adds optional original Essence indices and an optional original tier to its existing identity pin. Null fields retain historical serialization and identities; actual level/tier/Essences still determine legality and combat stats.
- [TowerEquipmentAcquisitionAudit.cs](../LL/tools/BalanceHarness/TowerEquipmentAcquisitionAudit.cs) reads production ordinary acquisition rules and reuses the existing occupied-slot reinforcement calculation.
- [BalanceHarnessFloor10ControlTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10ControlTests.cs) tests legal preparation, every removal position, surviving identities, invalid identity maps and acquisition arithmetic. Its scientific fixture is opt-in through `LL_FLOOR10_CONTROLS`.
- [screen-floor10-controls.py](analysis/screen-floor10-controls.py) freezes the sources/runtime and owns the bounded process; [verify-floor10-controls.py](analysis/verify-floor10-controls.py) independently reconstructs the declared family and results without combat.

The final preparation build passed **129 focused tests**, with two opt-in studies skipped, **17 warnings and zero errors**. Coverage includes canonical reference builds, existing progression upgrades/equipment/preview, gear profiles, equipment acquisition and upgrades, and World Tower. All backend tests used `build/run-tests.ps1`. The [successful log](../TestResults/tower-floor10-controls-preparation-20260928/final-build-and-tests.log) and [preserved TRX](../TestResults/tower-floor10-controls-preparation-20260928/focused-tests.trx) retain the results.

The [initial test run](../TestResults/tower-floor10-controls-preparation-20260928/build-and-tests.log) failed seven checks because the first level-40 draft retained illegal tier-2 gear. The controls were corrected to tier 1, and the reference identity pin was extended to preserve identities across that explicit tier comparison. All seven failures were resolved before screen combat. No gameplay eligibility rule was changed. Python syntax/CLI checks and an independent reconstruction of all 273 recipe declarations and 21 gear-definition inventories passed before launch.

## Results and interpretation

| Cohort | Cells | Samples per cell | Strongest cell | Cells with a win |
| --- | ---: | ---: | ---: | ---: |
| Six Essences, level 50 / tier 2 | 21 | 32 | 6/32 (18.75%) | 2 |
| Five Essences, level 40 / tier 1 | 126 | 32 | 0/32 | 0 |
| Five Essences, level 50 / tier 2 | 126 | 32 | 0/32 | 0 |

There were **zero draws**. The alternating armor-and-health reference won **6/32**; the repeated armor-and-health reference won **5/32 (15.625%)**. Every other six-Essence reference won zero. Removing any of the six original positions from either successful reference removed every observed win at both declared budgets. In the level-50 comparison, only the Essence removal changes combat inputs; character level, tier, gear and surviving identities are preserved.

The declared result is **`NoObservedLowerEssenceBreach`**. Every individual control received the same 32 historical seeds. Do not pool the 8,064 control observations into a single zero-win confidence claim: these are related recipes on shared seeds. This covers uniform removals from the retained family, not every mixed removal, alternative five-Essence composition or optimized search result. It does not establish a true sub-10% win rate for every control or a universal minimum-six-Essence requirement.

The original floor-10 fresh confirmation remains the strength evidence: **55/256** for the alternating armor-and-health reference and **23/256** for the repeated reference. This screen's 32 seeds are a subset of that same panel; their six-Essence wins are exact historical replays, not additional samples. All [273 result rows](../TestResults/tower-floor10-controls-20260928/result.json), [recipes](../TestResults/tower-floor10-controls-20260928/cells.json) and [historical panel](../TestResults/tower-floor10-controls-20260928/seeds.json) are retained.

## What the acquisition audit establishes

Every retained reference contains **15 characters, 105 individual items and 120 occupied slots**. All exact definitions are available in the tier-2 ordinary Legendary pool. The [21 acquisition rows](../TestResults/tower-floor10-controls-20260928/acquisition.json) independently agree on:

| Ordinary Champion channel | Probability of any Legendary/Masterpiece item |
| --- | ---: |
| Per equipment drop | 0.05% (1 in 2,000) |
| Per completion, mastery 0 | 0.025% (1 in 4,000) |
| Per completion, mastery 10 | 0.05% (1 in 2,000) |

These values combine **2% Legendary rarity** and **2.5% Masterpiece quality**, then the completion equipment chance. Ordinary area drops have zero Legendary probability. Dungeon gear arrives at rank 1; upgrading all 120 occupied slots to rank 5 costs **36,000 Reinforcement Parts and 80,280,000 Cinders**, or **2,400 Parts and 5,352,000 Cinders per character**. Two-handed weapons count as two occupied slots for reinforcement, so charging only 105 items would understate costs.

Even if every qualifying item were usable, seeing 105 Legendary/Masterpiece items averages **210,000 Champion equipment drops** in this channel. This deliberately ignores matching archetypes/specializations, duplicates, styles, rolls, per-character ownership and distribution. It is an optimistic count of qualifying drops, **not a full-loadout acquisition forecast**. Completion-only rates likewise omit miniboss and treasury rewards. Other quest, event, reward and trading channels are outside this calculation; dungeon access, successful clears and income are also unmodeled. It would be misleading to convert this into days or hours without those assumptions.

Reinforcement raises rank; the [upgrade policy](../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs) does not turn lower-rarity/quality gear into Legendary/Masterpiece gear. The [ordinary acquisition service](../LL/src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs) rolls rarity, quality and a 0.95–1.05 attribute multiplier separately. A reference multiplier fixed at 1 is an analysis baseline, not a guaranteed roll. Legal definitions therefore do not establish realistic ownership at floor 10.

## Execution and audit

The [preflight](../TestResults/tower-floor10-controls-20260928/preflight.json) confirms all **273 preparations** and **5,376 input matches** before combat. The [runtime qualification](../TestResults/tower-floor10-controls-20260928/runtime-qualification.json) then confirms **672 complete report matches** before any new control fight. After combat, native reconstruction checks every one of the 8,736 saved input hashes and cache identities.

Native execution took **296.95 seconds**. The [owner](../TestResults/tower-floor10-controls-owner-20260928/process.json) completed in **301.20 seconds**, exited successfully and drained all **eight processes**. Its [declaration](../TestResults/tower-floor10-controls-owner-20260928/declaration.json), [test log](../TestResults/tower-floor10-controls-owner-20260928/screen.log), [15-test TRX](../TestResults/tower-floor10-controls-owner-20260928/screen-tests.trx) and [completion](../TestResults/tower-floor10-controls-owner-20260928/completion.json) preserve the fixed accounting. All 15 selected tests passed, including the enabled scientific fixture and its 14 ordinary checks; these overlap with the earlier 129-test run and are not summed as distinct tests.

The [independent audit](../TestResults/tower-floor10-controls-owner-20260928/independent-audit.json) passed on its first run. It authenticated **9,087 files / 317,209,304 bytes**, reconstructed all recipes, trial ordering, outcomes and paired counts, verified all 5,376 historical inputs and 672 original full reports, and independently checked every acquisition row. It verified current/captured content, executable hashes, preserved character positions/identities and process drainage. The audit ran **zero fights**. No retries or extensions occurred.

Archive manifest: `e64ca74eb3f6b785514ec834e861c665facc33b68660c60bfce2875b46c72763`.
Result SHA-256: `62ddbcb504be27b6693a9dd04c0b66b2d008fef60c48707b81d7532d6a39f016`.

Commands executed from the repository root (Python denotes the bundled runtime):

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor10-controls-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessFloor10ControlTests|FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~CanonicalEquipmentBuildFactoryTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~EquipmentUpgradeExecutionTests|FullyQualifiedName~WorldTowerTests'
# Historical record: completed owner/output directories cannot be reused.
python -B -X utf8 'Balance Harness/analysis/screen-floor10-controls.py' --package 'TestResults/tower-floor10-controls-owner-20260928' --output 'TestResults/tower-floor10-controls-20260928' --artifacts 'TestResults/tower-floor10-controls-build-20260928'
python -B -X utf8 'Balance Harness/analysis/verify-floor10-controls.py' --owner 'TestResults/tower-floor10-controls-owner-20260928' --manifest-pin 'e64ca74eb3f6b785514ec834e861c665facc33b68660c60bfce2875b46c72763' --receipt 'TestResults/tower-floor10-controls-owner-20260928/independent-audit.json'
git -c core.safecrlf=false diff --check
```

The ignored local evidence is absent from a clean checkout. A later read-only audit needs a new receipt path and runs no combat.

Final checks verified all **787 frozen screen input/source/runtime hashes**, all **37 acquisition-preview hashes**, **108 local documentation links**, Python syntax, whitespace across the **12 scoped files**, and `git -c core.safecrlf=false diff --check`. Preserved TRX readback confirms 129 focused passes and 15 enabled-fixture passes. No required command remains blocked or unrun. The live floor-file hash is unchanged.

## Recommendation and scope

Keep the applied boss values and supported search. This screen found no lower-Essence counterexample within the declared controls; another scalar sweep is not indicated by these results. Retain the entire family if a later fresh lower-Essence confirmation is needed, rather than selecting a weaker control.

The user subsequently confirmed that the repeating curve represents **expected progression gear**. The [expected-gear acquisition follow-up](Tower-Expected-Gear-Acquisition-20260928.md) now prepares all **77 party variants across the eleven declared floor budgets** and audits their ordinary acquisition channels and reinforcement costs, with zero combat. That recommendation led to the [implemented supply path](Tower-Equipment-Supplies-Implementation-20260928.md), which awards targetable preparation gear before the relevant floor. The [current handoff](Tower-Continuation-Handoff-20260928.md) now prioritizes resource income, acquisition time and earned-inventory validation. The requested curve remains the target; realistic player availability is not yet established by these legal benchmark builds. This historical screen itself changed no reward rates or gameplay rules.

Changed code: the reference builder's optional identity fields, two offline helpers, the new control fixture, and its Python owner/auditor. Added this report and linked the completed findings from the application, equipment baseline and supported-search guide. No production content, application configuration, dependency or migration changed; no database operation, deployment or service restart occurred. Floor 10 remains **12.71 / 7.13**, floor 11 **17.94375 / 23.0175**, and the **835,319-value exclusion union** remains intact. Unrelated LiveOps/equipment-migration work is preserved.
