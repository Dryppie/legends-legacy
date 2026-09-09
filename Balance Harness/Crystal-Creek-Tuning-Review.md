# Crystal Creek creature-pressure experiment — 8 September 2026

This is a bounded offline investigation for the primary LL game. The user authorized proceeding from the [acceptance review](Starter-Path-Acceptance.md) toward the approved **50–90% win band**, with no duration target. The existing level-10 Goblin Warrior/Shortsword/Heavy Breastplate/Amulet/Goblin fixture and goal policy stay fixed. No production content or accepted baseline is changed by this experiment.

## Protocol declared before outcomes

Increasing Crystal Creek's regional offense alone is not legal under the current nondecreasing regional scaling rule: the following area has the same offense multiplier, 4.511. Keep the regional catalog, including Blood Grove's local override, unchanged. Instead test two existing creature mechanics:

- Blue Slime opening Barrier coefficient: **0.07, 0.14, 0.21, 0.28, 0.35, 0.42, 0.56** of caster Max Health.
- Frost Imp Ice Needle damage coefficient: **1.6, 2.4, 3.2, 4.8, 6.4, 8.0, 9.6** of caster Power.

Run the complete Cartesian grid of **49 candidates**, including the unchanged values. These abilities are also used by player Essences. To preserve players, changed candidates clone the affected ability and effect under distinct IDs and change only the corresponding creature ability mapping. Original ability definitions, player Essence references, loot tables, all other abilities, equipment, rewards and regional scaling remain unchanged. The changes would affect those creature species wherever used; only their authored idle spawn area is Crystal Creek, and other activity modes remain unmeasured.

Every candidate runs all **16 frozen handoff cells at 100 trials per cell**, using discovery master seed **718091**: **78,400 discovery battles**. Selection uses only the two primary `quest-rewards` Crystal Creek cells: minimize the largest absolute distance from 70%, then mean distance, then the sum of relative coefficient increases, then candidate ID. No duration, diagnostic-build win rate or favorable individual replay may influence selection. Freeze `selection.json` before accessing confirmation outcomes, even if no candidate reaches the band.

Run original and selected content across the same full 16-cell fixture at **1,000 trials per cell**, using reserved confirmation master seed **718092**: **32,000 battles**. Evaluate both saved suites independently with the unchanged goal policy. Then run both original control cohorts (48 cells total) for original and selected content at **100 trials per cell** on the confirmation master seed: **9,600 battles**. All 32 non-Creek control cells and the eight level-10 Blood Grove return cells must preserve gameplay exactly. The full fixed budget is **120,000 battles**, plus four detailed replay verifications: confirmation trial 0 for each primary encounter in original and selected content.

Development checks use separate master seeds **718093/718094**, with `--samples 1` producing 1,200 workflow battles. Before combat, resolve actual trial seeds and reject collisions across discovery/confirmation/development schedules or the earlier 618091–618094 handoff schedules. Candidates share seeds deliberately; do not pool their correlated trials. Pointwise 95% Wilson intervals must lie wholly inside the inclusive 50–90% band for a policy pass, and the existing minimum sample requirements remain intact.

Capture and hash content, fixtures, selected nonsecret settings and execution identity before the first battle. Validate every candidate before discovery. Use a fresh output directory; retain partial evidence if cancelled or invalid. Reject changed content, settings, executable identity, incomplete budget, incompatible comparisons or altered controls. Do not refine the grid, reselect, pool confirmation with discovery, extend samples or promote a baseline after observing results. A complete experiment may fail the target. Preserve the earlier level-5 and level-10 archives unchanged.

## Result

The fixed run completed once at ignored `TestResults/balance/crystal-creek-pressure-reference`: **120,000 valid battles across 55 suites**, zero invalid/cancelled/unexecuted battles, all 32 non-Creek control cells unchanged, all eight level-10 Blood Grove return cells unchanged, and all four detailed replays matched. The 14 draws were the same seven opening-control draws in original and selected content; no handoff battle drew. Source content and captured settings/fixtures/execution remained fixed throughout.

The declared discovery rule selected **Barrier 0.28 + Ice Needle 2.4** (`barrier-28-needle-024`) at 90/100 Slime-pair wins and 51/100 mixed-pair wins. That selection was saved before confirmation. The reserved results are:

| Primary encounter | Original wins / 1,000 | Selected W/L/D | Selected win rate | Pointwise 95% Wilson interval | Policy |
| --- | --- | --- | --- | --- | --- |
| Two Blue Slimes | 991 | 916 / 84 / 0 | **91.6%** | **89.72–93.16%** | Inconclusive: overlaps 90% |
| Blue Slime + Frost Imp | 1,000 | 503 / 497 / 0 | **50.3%** | **47.21–53.39%** | Inconclusive: overlaps 50% |

The candidate **does not establish the approved band**. The Slime-pair point estimate is above 90%; the mixed-pair interval extends below 50%. Under the unchanged interval rule, both checks are Inconclusive rather than Pass or Fail; the standalone evaluation's exit code is 3. There are no evidence or coverage issues. The experiment command returns 0 for completing its fixed protocol; this is not a balance pass. No samples were appended, candidate replaced or production values changed.

The paired changes against the current-executable original control are −7.5 percentage points for two Slimes (95% interval −9.59 to −5.34 pp) and −49.7 points for Slime + Imp (−53.24 to −45.67 pp). These are compatible content comparisons on the new reserved seeds, not comparisons pooled with the historical handoff run.

The diagnostics make preparation differences visible without imposing additional targets:

| Selected-content level-10 preparation | Two Slimes wins / 1,000 | Slime + Imp wins / 1,000 |
| --- | --- | --- |
| Original two items + Goblin Warrior | 0 | 0 |
| Add Amulet only | 0 | 1 |
| Add Goblin in slot 2 — primary | 916 | 503 |
| Add optional Fury | 962 | 629 |

Only `combat/abilities.json` and `combat/creature-abilities.json` differ in the selected content copy. The two creature variants have distinct ability/effect IDs; the original player-facing definitions and all other combat content remain unchanged. Detailed replays contain the variant abilities and match the saved results. The original Blood Grove and Crystal Creek plan/result hashes remain unchanged.

## What the next experiment should resolve

**Subsequent result:** the separately declared [fine experiment](Crystal-Creek-Fine-Tuning-Review.md) confirms Barrier 0.44 / Ice Needle 1.7 at **74.40% / 55.45%, both Pass**. That review also records the correction removing creature variants from player Essence ownership groups, with unchanged gameplay over the repeated confirmation/control schedules. This coarse run, its retained executable and results remain unchanged.

The coarse grid brackets useful values but leaves the two encounters on opposite boundaries. In discovery, Barrier 0.35 / 0.42 produced 86% / 77% Slime-pair wins; for each of those Barriers, Ice Needle 1.6 gave 100% mixed-pair wins while 2.4 gave 34% / 21%. This supports a **finer, separately declared grid with a stronger Barrier and Ice Needle between 1.6 and 2.4**, not acceptance of the selected coarse candidate. This is an inference from exploratory measurements; it is not a confirmed candidate or guarantee of a passing interval.

Freeze that next grid, total budget, selection rule and fresh confirmation seeds before execution. Keep this completed run closed. A production change and durable passing baseline should follow a confirmed candidate and scope review. Blood Grove's existing Raven uncertainty remains a separate unresolved assessment. Other spawn combinations, chest outcomes and other activity modes remain unmeasured.

## Evidence and verification

| Evidence | SHA-256 |
| --- | --- |
| Fixed `plan.json` | `baa34cca3e0562c750fb8427ee6a53b8f3d61e20b828e322ab44e0ed3e8af747` |
| Frozen `selection.json` | `8ddaa922e6c93b491fd39b41c4b5dc5e62113f6c3da9e20b610438012e41a852` |
| Completed `results.json` | `6932b028b1d435d345ccb256f9bfc68391f04465eb2a1b31c68f8a4387007e61` |
| Original confirmation bundle fingerprint | `80cb48e03b5100318a67a5f6e389c264aedfb1c317e68c2be242ae26034e54f7` |
| Selected confirmation bundle fingerprint | `eab2c4491f711e35f2a13380da625c351aef5dee1a76b3c3b53f3309e0ce56ee` |
| Harness executable | `4156d6f1c3851e20149afd409f14be8c210fd864a96cb96ac9c10b1cf953c790` |
| Combat service assembly | `ef122725bf8ebe93180f53340d73a1f4ebc7f7c6b9171ffb52178c19543b98f9` |

The experiment used .NET 10.0.11 on Windows X64. Its exact executable and dependencies are retained with a 62-file hash manifest under ignored `TestResults/balance/retained-builds/creek-pressure-v1/net10.0`, alongside scoped experiment-source copies and the 118-test TRX. Retain both the full run and the executable directory. This does not claim that the complete dirty dependency source tree is recoverable from the saved Git commit.

- `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore` initially passed. A subsequent rebuild after a test assertion-style cleanup hit concurrent Combat Styles removals: missing `CharacterBuildPreset` and `CombatStylePracticeResult`. Those unrelated files were not modified here. The tested executable was preserved before proceeding; a clean build of the latest shared source remains blocked by that work.
- `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness|FullyQualifiedName~CombatStyleHarness'`: **118 passed, zero failed/skipped** on that successful build. Ten added cases cover the fixed protocol, player-ability preservation, candidate selection, complete workflow/replays, cancellation/changed fixtures/content/settings, and two independent normal-idle parity cases with the creature variants.
- The retained executable's `investigate-creek-pressure` command completed the declared full run, both goal evaluations, all comparisons and four replay verifications. No additional reference samples were run afterward.
- Scoped whitespace/link checks and frozen-evidence hashes were verified. Full backend tests, frontend tests, standalone cohort smoke scripts and hosted CI were not rerun for this tool-only increment. The new workflow test exercises the complete small experiment; the full run separately exercises the real CLI.

Changed files are the new [experiment runner](../LL/tools/BalanceHarness/CrystalCreekPressureExperiment.cs), its CLI entry in `Program.cs`, the new pressure tests and added parity cases, this review, and the linked plan/acceptance/README/roadmap updates. Production content, game configuration, migrations, local character state and deployments are unchanged by this increment. The candidate exists only in ignored offline content copies.
