# Blood Grove progression review — 8 September 2026

The fixed progression experiment does **not** explain the [First Hunt cohort's](First-Hunt-Cohort.md) Blood Grove losses as missing ordinary Essence training or reinforcement. All six starter builds lost both pairings at equipment ranks 0, 1 and 5, with Essence levels 1 and 10, on both predeclared seed sets. No combat coefficient, acquisition reward or draft goal was changed.

## What the progression rules actually provide

Essence level is an Ascension prerequisite, not a direct combat-strength multiplier. The [combat executor](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs) resolves abilities using Ascension and evolution state; the [shared scaler](../LL/src/Core/Domain/Models/Essences/EssenceAbilityProgressionScaler.cs) scales by Ascension tier. The [loadout factory](../LL/src/Infrastructure/Service/Services.LL/Essences/EssenceCombatLoadoutFactory.cs) supplies no passive attribute growth from Essence level. Raising an unascended Essence from level 1 to 10 therefore leaves its combat behavior unchanged under these rules.

The [XP constants](../LL/src/Core/Domain/Models/Essences/EssenceProgressionConstants.cs) require 132,860 XP for level 1 → 2 and 1,296,004 cumulative XP for level 1 → 10. The [Essence service](../LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs) also permits one Essence Dust per level gained; nine Dust can take a fresh Essence to level 10. First Ascension requires level 10 and six Lesser Monster Cores before collection catch-up discounts. These are separate resource requirements, not nine Dust or six Cores guaranteed by the entry quests.

By comparison, the [character XP curve](../LL/src/API/API.LL/Data/progression/character-experience.json) requires **3,225 XP to reach character level 5**, or **27,500 XP to reach level 10**, from level 1. The [combat XP writer](../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/CharacterExperienceRewardWriter.cs) grants the combat award to each attuned Essence as well as the character, before Essence-specific bonuses. Under ordinary combat-only leveling without Dust or bonuses, a level-1 Essence is consequently plausible at both checkpoints. These totals are thresholds, not a measured journey or a claim about all XP sources and reward overshoot.

Current [reinforcement prices](../LL/src/API/API.LL/Data/equipment/equipment-upgrades.v1.json) have content version **2**, despite the filename. Costs are cumulative purchases from rank 0. The [upgrade policy](../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs) charges the next rank's Parts and Cinders on each item.

| Gear state in the experiment | Cinders for both items | Reinforcement Parts for both | Item budget multiplier |
| --- | ---: | ---: | ---: |
| Weapon and Medium Mail at rank 0 | 0 | 0 | 1.00 |
| Both at rank 1 | 22,300 | 10 | 1.04 |
| Both at rank 5 | 691,300 | 310 | 1.20 |

Rank 5 is the current maximum. The multiplier comes from the [equipment balance](../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentBalance.cs) and [evaluator](../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentEvaluator.cs); it applies to the item allocation budget, not total character damage or health.

The entry quest chain supplies 500 Cinders and retains the weapon/Armor Chest item used here, with no guaranteed spare reinforcement budget. A tier-1 rank-0 dismantle returns one Part under the [dismantle rules](../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradeModels.cs). If all ten Parts for the rank-1 pair came from unwanted regional rank-0 drops, the configured [ordinary drop chance](../LL/src/API/API.LL/Data/equipment/equipment-ordinary.v1.json) implies an expected 8,640 victories for ten items; the [acquisition processor](../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/CombatAcquisitionRewardProcessor.cs) rolls once per victory. This conditional expectation excludes other sources and says nothing about an individual player's luck. Farming also raises character level, so an expensive rank-5 loadout frozen at character level 5 is a strength probe, not a plausible default entry recipe.

Styles remain a material omission. [Soul Archive](../LL/src/API/API.LL/Data/quests/onboarding/soul-archive.v3.json) guarantees one Fury Blueprint and 500 Cinders; [blueprint conversion](../LL/src/API/API.LL/Data/equipment/equipment-blueprints.v1.json) costs 100 Cinders at tier 1. That is a much more accessible capability to investigate than reinforcement. This experiment preserves the original no-style condition and does not claim to cover every Forge operation.

## Fixed experiment and measured evidence

[investigate-blood-grove.ps1](../build/investigate-blood-grove.ps1) derives the Blood Grove stage from the unchanged First Hunt fixture. It preserves the original six build IDs, two ordered encounter IDs and seed schedule. The matrix crosses **Essence level 1/10 × both equipment ranks 0/1/5**. Every build remains character level 5, with its First Hunt Essence, mace/wand and the same conditional Medium Mail outcome; Ascension, evolution, styles, other gear and persistent bonuses remain absent.

Before execution the script writes `plan.json` declaring 100 samples per cell, discovery seed 1337, confirmation seed 7331 and a fixed stopping rule. It runs all six variants on both sets, without selecting a winner or extending samples after seeing outcomes: **12 cells × 6 variants × 100 samples × 2 sets = 14,400 battles**. The second schedule has no seed overlap with the first within an encounter. A two-sample workflow smoke used the same fixed matrix before the full execution; the confirmation results were not used to tune or select variants.

| Both-item rank | Essence levels tested | Discovery: each of 12 cells | Confirmation: each of 12 cells | Changed summaries from training, discovery / confirmation |
| --- | --- | --- | --- | --- |
| 0 | 1 and 10 | 0/100 wins at either level | 0/100 wins at either level | 0/1,200; 0/1,200 |
| 1 | 1 and 10 | 0/100 wins at either level | 0/100 wins at either level | 0/1,200; 0/1,200 |
| 5 | 1 and 10 | 0/100 wins at either level | 0/100 wins at either level | 0/1,200; 0/1,200 |

All battles completed as defeats: no draws, invalid, cancelled or missing evidence. Each 0/100 cell has a pointwise 95% Wilson interval of approximately **0–3.7%**. Builds and variants share seeds; the 14,400 battles must not be pooled as independent trials. The intervals are not simultaneous confidence bounds across the matrix and do not prove that winning is impossible.

Across 7,200 matched level-1/level-10 pairs, the complete combat summaries were identical, including outcomes, terminal state and telemetry. Rank 5 increased each cell's mean time before defeat by roughly **0.15–5.08 seconds** relative to rank 0, depending on the build, encounter and seed set; that descriptive change did not produce any wins. Defeat duration is not kill time or winning pace.

Generated recipes and all run bundles are retained under ignored `TestResults/balance/blood-grove-progression-reference`. `results.json` contains separate per-cell scorecards and paired gained/lost win counts; `summary.md` is the readable matrix. Each run contains archived combat inputs/content and battle records, plus one separately saved detailed replay. Selected nonsecret economic JSON files are copied alongside the plan for this review. These local artifacts are not committed or guaranteed to exist in another checkout.

Run the same fixed experiment from the repository root using a new directory:

```powershell
./build/investigate-blood-grove.ps1 -OutputDirectory TestResults/balance/blood-grove-001
```

Use `-NoBuild` after building, or `-SamplesPerCell 2` for the 288-battle workflow smoke. The full matrix is a local investigation, not an additional CI workload. Ordinary baseline comparison intentionally rejects changed progression recipes; the script reports controlled substitutions separately and does not approve targets or promote a gameplay baseline.

## Harness changes and verification

Schema 2 now accepts an optional `essenceLevels` map on a stage or single scenario, keyed by equipped Essence definition ID. Stage values apply only to builds selecting those Essences; unspecified selections stay level 1. Only unascended levels 1–10 are supported. Empty maps, unknown/unselected IDs, out-of-range levels, schema-1 use and mismatched frozen progression are rejected. The field is omitted when absent, preserving both existing fixture hashes and historical input serialization. The shared reference-build factory retains its original level-1 contract; only the offline fixture snapshot applies the requested training.

- Release builds of the tool and test project passed. The test-project build first hit a sandbox denial on a generated MSBuild cache; the same command succeeded with expanded execution permissions, with 26 warnings and no errors.
- **67 harness tests passed** through `build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness'`. Added coverage includes six production-path parity cases with trained/reinforced builds, recipe validation, unchanged training behavior, replay, comparison incompatibility and rehashed snapshot tampering.
- The 288-battle progression smoke and full 14,400-battle matrix passed, including 12 detailed replays in each workflow.
- Both existing cohort smoke workflows passed again, preserving their fixture hashes, paired repeatability, replay and draft goal coverage. Original control and First Hunt fixtures/goals remain unchanged.
- PowerShell syntax, 89 local Markdown links and whitespace checks passed. Reusing an existing investigation output directory was rejected without modifying its summary.

No database migration, gameplay configuration, production service change or deployment is required. The full backend suite and hosted CI were not run for this offline-tool change; hosted CI observation remains pending.

## Decision and next investigation

Keep the original controls and draft targets. Do not prescribe “train more” as the Blood Grove remedy: unascended levels add no combat strength, and even costly reinforcement did not make these particular builds clear the pairings. This still does not establish the right enemy coefficients or certify whole-area difficulty.

Next, test an attainable entry loadout using the guaranteed Fury Blueprint and the possible Armor Chest outcomes, with a declared resource ledger. Then inspect ability/targeting behavior in selected defeats to distinguish missing capability from excessive two-enemy pressure. First Ascension, additional gear and later character levels should be separate, costed variants if needed. Any Ascension recipe extension should first reconcile tier-boundary metadata (tier 1 starts at level 11 in `EssenceAscensionDefinition`) with the service's actual level-10 Ascension transition; this review supports unascended training only. The level-10 composition gaps, intended player experience and numerical goal approval remain open.

Follow-up completed: the [attainable-entry review](Blood-Grove-Entry-Review.md) now covers Fury and all nine Armor Chest outcomes in 43,200 battles, with no wins in any cell on either seed set. All 24 preselected replays show active abilities firing and both enemies still alive at player defeat. The scope decision on 8 September 2026 defers the proposed Lumo Token replacements in favor of defining and validating one supported starter progression path. Universal Essence viability is not required. The original findings above remain historical evidence; no target or coefficient has changed. See the [current next steps](Balance-Harness-Plan-With-Benchmarking.md#next-work-to-complete-phase-2).

The user subsequently selected Goblin Warrior, Sword and Heavy Chest for Blood Grove and initially approved a 65–75% band around a 70% aim. Following the pressure experiments, the current accepted band is **50–90%**, with the same recipe and aim. The [policy revision](Blood-Grove-Band-Review.md) records the saved-run re-evaluation and current next steps; this progression investigation remains historical evidence.
