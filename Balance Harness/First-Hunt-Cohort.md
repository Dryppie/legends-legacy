# First Hunt cohort — 8 September 2026

## Implemented scope

[idle-first-hunt.json](../LL/tools/BalanceHarness/Fixtures/idle-first-hunt.json) is a separate `idle-first-hunt-v1` suite using schema 2. It crosses the three actual First Hunt Essences (Goblin Warrior, Hollow Stag and Skeleton) with two legal starter weapon choices (mace and wand), at levels 1, 5 and 10. Two fixed encounters per checkpoint produce **36 cells and 3,600 battles** at the default 100 samples per cell.

The original 12-cell `idle-reference-v1` controls and their goals remain unchanged. New suite IDs, seeds, recipe hash and baseline evidence belong to this cohort. Comparing its results directly against the old control suite is invalid.

| Checkpoint | Fixed equipment and Essence state | Encounters |
| --- | --- | --- |
| Level 1, First Weapon complete | The selected First Hunt Essence and one guaranteed mace/wand | Goblin; Goblin Warrior |
| Level 5, Trial of Lumo complete | Retain starter choice and weapon; one Medium Mail from the earlier Armor Chest | Raven + Raven; Raven + Blood Zombie |
| Level 10, Blood in the Grove complete | Retain weapon and Medium Mail; one Amulet from the Jewelry Chest; add Goblin in the second Essence slot using the earlier Lumo Token | Blue Slime + Blue Slime; Blue Slime + Frost Imp |

The equipment counts follow real quest rewards. Medium Mail and Amulet are **fixed possible random-box outcomes**, not guaranteed selections. All gear is common, standard, tier 1, rank 0 and uses baseline attribute rolls. All Essences remain level 1, unascended and unevolved. This controls for training, Forge investment, styles, regional drops, persistent bonuses and buffs; it does not establish that players normally reach these checkpoints in that state.

The source contracts are [First Hunt](../LL/src/API/API.LL/Data/quests/onboarding/training-day.v4.json), [First Weapon](../LL/src/API/API.LL/Data/quests/onboarding/first-weapon.v2.json), [Into the Ruins and its Armor Chest](../LL/src/API/API.LL/Data/quests/region-01/into-lumo-ruins.v2.json), [Trial of Lumo and its Essence Token](../LL/src/API/API.LL/Data/quests/region-01/trial-of-lumo.v4.json), [Blood in the Grove and its Jewelry Chest](../LL/src/API/API.LL/Data/quests/region-01/blood-in-the-grove.v4.json), [equipment-box rules](../LL/src/Core/Application/UseCases/Inventories/SelectionCrates/RandomEquipmentBoxCatalog.cs) and [Essence Token choices](../LL/src/Core/Application/UseCases/Inventories/SelectionCrates/CatalystSelectionCrateCatalog.cs).

Blood Grove and Crystal Creek normally spawn two enemies (96.9% of the configured count distribution). The selected duplicate/mixed pairings exercise that pressure using actual creature definitions. They are fixed ordered encounters, not a sample of every species combination, order or area-wide probability.

## Goals and reproducibility

[idle-first-hunt-goals.json](../LL/tools/BalanceHarness/Fixtures/idle-first-hunt-goals.json) defines six draft goals expanded into **180 checks**. Ordinary starter Goblins retain a proposed 90% clear minimum; the remaining entry encounters use a proposed 60% minimum. There is no upper clear-rate ceiling for regional enemies. Winning pace (60 seconds), paired clear-rate movement (−5 percentage points), shared-win duration movement (+5 seconds) and diagnostic health movement (−10 percentage points) remain proposals requiring gameplay review.

Schema 2 keeps the first `creatureId` and adds an optional ordered `additionalCreatureIds` list, with matching frozen `additionalCreatures` snapshots in battle inputs. One to three enemies are supported when the area's count distribution permits that count. Each occurrence gets its own combat slot and mutable runtime state, including duplicate species. Every member is checked against the archived scenario and area. Encounter order/count/identity is part of comparison compatibility and the goal fixture hash; coefficient changes are reported separately.

Schema 1 still represents one enemy and rejects group fields. Null extension fields are omitted from serialization, preserving old input and fixture hashes. Historical control evidence remains readable for comparisons; replay still requires its original binary/runtime identity. No production combat formulas or preparation services changed.

Run the complete workflow from the repository root, using a new output directory:

```powershell
./build/smoke-balance.ps1 -SamplesPerCell 100 -SuitePath LL/tools/BalanceHarness/Fixtures/idle-first-hunt.json -GoalsPath LL/tools/BalanceHarness/Fixtures/idle-first-hunt-goals.json -OutputDirectory TestResults/balance/first-hunt-review-001
```

Omit `-SamplesPerCell 100` for a smoke run: two 108-battle runs, a disposable repeatability reference, paired comparison, detailed group replay and 180 expected inconclusive draft checks. Add `-NoBuild` after building. The CI workflow now runs both cohorts and retains both bundles; it does not promote either reference to approved gameplay evidence.

## Initial measured results

Release validation used master seed 1337 and two fresh 3,600-battle runs. Both completed with no invalid, cancelled or missing battles; all 36 cells matched with zero gameplay changes. The full evaluation was **63 pass, 26 fail, 91 inconclusive, 0 invalid**, with advisory enforcement and exit code 0.

| Checkpoint / encounter | Goblin Warrior Essence, mace / wand | Hollow Stag Essence, mace / wand | Skeleton Essence, mace / wand |
| --- | --- | --- | --- |
| Level 1 / Goblin | 100 / 100 wins | 100 / 100 wins | 100 / 100 wins |
| Level 1 / Goblin Warrior | 18 / 15 wins | 83 / 83 wins | 33 / 28 wins |
| Level 5 / Raven + Raven | 0 / 0 wins | 0 / 0 wins | 0 / 0 wins |
| Level 5 / Raven + Blood Zombie | 0 / 0 wins | 0 / 0 wins | 0 / 0 wins |
| Level 10 / Blue Slime + Blue Slime | 100 / 98 wins | 17 / 17 wins | 18 / 16 wins |
| Level 10 / Blue Slime + Frost Imp | 100 / 100 wins | 91 / 92 wins | 86 / 86 wins |

Each number is wins out of 100 for that cell, not a pooled population estimate. At level 10 every build also equips Goblin. The scorecard/evaluation retains confidence intervals and eligible counts; 0/100 has a 95% Wilson upper bound of about 3.7%, not proof that a win is mathematically impossible.

The 26 draft failures comprise 20 entry-clear checks and six winning-duration checks. Of 91 inconclusive checks, 72 are unchanged paired duration/health checks with unavailable variance-based intervals; 19 are winning-duration checks without enough evidence or with boundary overlap. No check is invalid.

These results expose questions that the single-enemy controls could not answer. The level-5 reward-only, untrained state is insufficient for both tested pairings; the required training/Forge/reward budget must be examined before changing enemy coefficients. The level-10 pairings also separate Essence compositions strongly, and many successful slime-pair fights exceed one minute. This does not establish whole-area viability, inevitable player failure, or an approved difficulty target.

## Verification and remaining work

- The tool and backend test project built successfully. **57 harness tests passed** through `build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness'`.
- Six additional production-path parity cases cover actual starter Essences, duplicate/mixed enemies, both weapon types and second-slot builds. Group tests verify independent duplicate state, repeatability/logging parity, eligibility, snapshots, comparison compatibility, replay and draft policy coverage.
- Both smoke workflows passed. The new 7,200-battle reference workflow passed repeatability and group replay; three additional selected win/loss replays also matched.
- Old control fixture/input hashes remain compatible. The new binary read and evaluated the previous saved control/reference successfully, and its control smoke remained unchanged.

Local evidence is ignored under `TestResults/balance/first-hunt-reference`, `first-hunt-smoke`, `first-hunt-legacy-smoke` and `first-hunt-legacy-history`. Retain those bundles with their manifests for audit. Hosted CI execution remains pending after push. This work adds harness schema/fixtures/goals and CI coverage; it needs no database migration, gameplay setting change or deployment.

The follow-up [Blood Grove progression review](Blood-Grove-Progression-Review.md) now checks those training/reinforcement assumptions. Its fixed 14,400-battle matrix covers unascended Essence levels 1/10 and both-item ranks 0/1/5 on two predeclared seed sets. Every cell still had zero wins; all 7,200 training pairs had identical combat summaries. Current combat strength scales at Ascension, and reinforcement costs are far beyond the guaranteed entry budget. The harness now has 67 passing tests; this cohort and its goals remain unchanged.

The [attainable-entry review](Blood-Grove-Entry-Review.md) has also completed Fury/Armor Chest variations: 43,200 valid battles across all nine armor outcomes and both weapon conditions, with zero wins in every cell on both seed sets. Its 24 preselected replays show abilities executing while both enemies remain alive at defeat. The next attainable option is replacing the First Hunt Essence using the guaranteed Lumo Token in the single level-5 slot. There are now 76 passing harness tests. The level-10 composition differences remain to be investigated. Keep reward-box outcomes and omitted systems explicit, confirm decisions with fixed sample budgets/reserved seeds, and approve numerical goals only against the intended player experience.
