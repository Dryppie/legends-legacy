# Crystal Creek fine creature-pressure experiment — 8 September 2026

The user authorized the next finer tuning pass, fresh confirmation, and local application after a passing result and successful build. This protocol follows the closed [coarse experiment](Crystal-Creek-Tuning-Review.md). The primary LL game's level-10 Goblin Warrior/Shortsword/Heavy Breastplate/Amulet/Goblin fixture and **50–90% win policy** remain unchanged. Duration is not an acceptance target. Blood Grove's Raven uncertainty remains separate.

## Protocol fixed before execution

Run a Cartesian grid of six Blue Slime opening Barrier coefficients (**0.35, 0.38, 0.41, 0.44, 0.47, 0.50**) and seven Frost Imp Ice Needle coefficients (**1.7, 1.8, 1.9, 2.0, 2.1, 2.2, 2.3**): **42 candidates**. These are stronger Barriers and lower Imp damage than the selected coarse candidate, chosen to move both encounters away from opposite band boundaries. Separate creature ability/effect IDs preserve the original player Essence abilities. Regional scaling, other abilities, rewards and builds remain fixed. Do not append to or pool the coarse run.

- Discovery: all 16 frozen handoff cells, **300 trials per cell**, master seed **818091**: **201,600 battles**.
- Selection: minimize the largest primary-cell absolute distance from 70%, then mean distance, summed relative coefficient increase and candidate ID. Use only the two primary Crystal Creek cells; ignore durations and the fourteen diagnostic cells. Write `selection.json` before confirmation, even if no candidate fits the band.
- Confirmation: original and selected content, all 16 cells, **2,000 trials per cell**, fresh master seed **818092**: **64,000 battles**. Evaluate each separately using the unchanged policy and whole-interval 95% Wilson rule.
- Controls: both original cohorts, 48 cells, **100 trials per cell** for original and selected content on the confirmation seed: **9,600 battles**. Require identical gameplay in all 32 non-Creek cells and the eight handoff Blood Grove return cells.
- Replays: confirmation trial index 0 in each primary encounter for original and selected content, **four fixed detailed replays**.

The fixed total is **275,200 battles**, plus four replay verifications. The development mode uses master seeds **818093/818095** and `--samples 1`: 3 discovery trials and 20 confirmation trials per cell, **2,752 workflow battles**. Before execution, resolve actual trial schedules and reject overlap with the other fine schedules and the earlier 618091–618094 handoff and 718091–718094 coarse schedules. Development samples cannot meet the unchanged gameplay-policy minimum.

Preflight adjustment before any fine battle: the initially proposed development seed 818094 collided with another reserved schedule. A seed-only scan rejected it and verified 818095 instead; the full discovery/confirmation seeds, grid and sample budget were unchanged. The scan is retained in `TestResults/balance/creek-fine-seed-preflight.ps1`. The captured economic inputs also include `dungeons/dungeons.json`, which the blueprint catalog reads to validate reward sources; this closes a missing dependency in the earlier source capture.

Capture and hash the original content, fixture, goals, selected nonsecret settings and execution identity before any battle. Preflight all candidate copies. Keep selection/confirmation separate, preserve partial evidence on failure, and reject changed inputs, incomplete execution or altered controls. Do not refine the grid, reselect, pool results or extend samples after outcomes. A completed run may fail or remain inconclusive. Keep the exact executable and evidence together.

If both primary confirmation checks pass, inspect the content diff, preserve the original player abilities and confirm the current build/tests. Then apply only the selected creature ability definitions/mappings to the local API content and verify those bytes against the confirmed content copy. Review a local regression baseline for this scoped checkpoint separately from Blood Grove. Do not deploy, apply migrations, restart the running API or change the test character.

## Result

The fixed experiment completed **275,200 valid battles across 48 suites**, with no invalid, cancelled or missing battles. Eight draws occurred in the unchanged controls; none occurred in the handoff confirmation. Selection froze **Barrier 0.44 / Ice Needle 1.7** after discovery (234/300 and 174/300 wins). The fresh confirmation is **Pass** under both unchanged enforced checks:

| Primary level-10 encounter | Original wins / 2,000 | Selected wins / 2,000 | Selected 95% Wilson interval | Assessment |
| --- | --- | --- | --- | --- |
| Two Blue Slimes | 1,980 (99%) | **1,488 (74.40%)** | **72.44–76.26%** | Pass |
| Blue Slime + Frost Imp | 2,000 (100%) | **1,109 (55.45%)** | **53.26–57.62%** | Pass |

Paired changes are −24.60 percentage points (approximate 95% interval −26.82 to −22.26) and −44.55 points (−47.05 to −41.83). The 32 external control cells and eight Blood Grove return cells retained identical gameplay; all four fixed detailed replays matched.

The underprepared two-item and Amulet-only builds won 0/2,000 against both Creek pairs. Optional Fury won 1,732/2,000 (86.60%) and 1,322/2,000 (66.10%). All level-10 Blood Grove return cells won 2,000/2,000. These fourteen cells remain diagnostics; their results do not expand the two-cell policy. Duration was not used for selection or acceptance. The Slime/Imp result is closer to the lower bound than the Slime pair, but its entire interval passes the approved band; the closed experiment was not extended to chase 70%.

## Local application compatibility check

During confirmation, the application review found that the experimental clones retain `owningEssenceId`. Explicit player loadouts still reference the original abilities, but the full catalog diagnostic rejects duplicate Essence slots and the separate ability simulator would group the clones into player rosters. Local application therefore requires removing ownership metadata from the two creature variants. The coefficient grid, selected candidate and completed evidence must remain untouched.

Before observing this check's results, its fixed verification is: copy the selected content after the closed experiment finishes, remove only the two ownership fields, and add meaningful Barrier/damage behavior scenarios to the diagnostics manifest. Using the retained experiment executable, rerun the selected 16-cell confirmation with the **same 2,000 trials and seed 818092**, plus the same 48 control cells at 100 trials: **36,800 repeated verification battles**. Compare every gameplay record with the original selected-content runs and require zero changes; replay the two fixed primary trial-0 fights. These are compatibility repetitions, not additional statistical observations, and must not be pooled or used to reselect the candidate. Apply only if the original policy passes, these comparisons match, and full backend tests pass. The generator also removes ownership metadata in future content copies; the original experiment keeps its retained executable/source and archives.

That check completed with **zero gameplay changes across all 64 cells and 36,800 repeated battles**, two matching replays, and the same passing intervals. The two game-content files are now implemented locally and byte-identical to the corrected, verified copy. Every original ability definition and Essence reference is preserved. Only Blue Slime and Frost Imp mappings change; the JSON writer also reformats parts of the two copied files. The new diagnostics scenarios assert positive Barrier and damage events and are not runtime combat inputs.

## Evidence and local verification

| Evidence | SHA-256 |
| --- | --- |
| Fine `plan.json` | `abf5544ce67cd7c87eb660b21015cf417511510ee698a7825bceee771979b39e` |
| Frozen `selection.json` | `1fc6b1b03630955c5a0c3daadae505ae46ec47698501eed2ff4ec8def2e895fa` |
| Fine `results.json` | `065ad4f82bf88076845ddef9959901c934cda9033848e6cc8f60cdfe26c2ec68` |
| Original confirmation bundle | `a62e2014ae35a3398eddf137ad152b4dbab96531b47b193cc2bc512f9096bcf7` |
| Selected experimental confirmation bundle | `19b6e5f7b74f7da021a55547a8bb1aa789b4331ecb2db4b5a1d2e810404c015c` |
| Corrected local confirmation bundle | `6bde2400747b3f9f03170cda8fee278e64eb1370c9335e3912188d7bd65b8a33` |
| Local compatibility `results.json` | `8e5a0309263a153cab2cef6c71e6f4dd67e8b30dba16a3809f9fb89669be6481` |
| Accepted local baseline manifest | `a668f2f3460659dadd4fe39af6606eb3f2981222d3c4a407678af56de1ffc7bc` |
| Applied `combat/abilities.json` | `57d8404347978c92e71150b438ec09d8eade78c1df44be9d0a766d151dde0af8` |
| Applied `combat/creature-abilities.json` | `789b124b065c4a10776494dda3d8d26df87d4b65ced69da57968a1d461a73e94` |
| Retained harness executable | `85ffa1311ef7298c0b1e262341b98ea7a7b78aa9294697e09d710c58c4a1931b` |
| Retained combat service assembly | `0f73195bec306b0780a7b273fd0d3cd252d9a4d1d0724448c581e9ff0c25feeb` |

The full experiment is under ignored `TestResults/balance/crystal-creek-pressure-fine-reference`; the ownership compatibility run is `TestResults/balance/crystal-creek-local-reference`. The 62-file executable retention manifest, pre-outcome protocol/source copies and 121-test TRX are under `TestResults/balance/retained-builds/creek-pressure-fine-v1`. The compatibility script is `TestResults/balance/verify-creek-application.ps1`. Keep all three directories and the script together. They use .NET 10.0.11 on Windows X64. The retained executable predates the generator's ownership correction and remains the identity for replaying these archives; the complete dirty dependency source tree is not reconstructed by the recorded Git revision.

Before staging local content, all 23 source hashes, four fixture hashes and four game assembly hashes matched the experiment. After the final build/tests, all 15 local combat inputs and the four game assemblies still match the verified application. No regional scaling, rewards, equipment, combat settings or player Essence definitions changed. The historical Blood Grove/coarse/handoff evidence remains closed.

- Release build with `--no-restore`: passed, four existing warnings and zero errors. The earlier Combat Styles compilation problem is resolved. A generated API static-assets cache denied sandbox writes during the final rebuild; the same build succeeded through the approved unsandboxed execution path. No required command remains blocked.
- Pre-experiment `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness|FullyQualifiedName~CombatStyleHarness'`: **121 passed**. New coverage fixes the fine grid/budget/seed contract, both workflow modes and restoration from tuned content. The final generator assertions additionally verify all player ownership groups and catalog slot coverage.
- Final `./build/run-tests.ps1 -NoBuild`: **2,107 passed, zero failed/skipped** with the local content. The first full run found one stale 232-ability count; its updated 234-count test explicitly checks both creature-only mappings, absent player ownership, and preserved original player slots. No other full-suite failure occurred. The final TRX and successful build/source snapshots are retained under `retained-builds/creek-pressure-fine-application-v1`; exact statistical replay still uses the original fine retention directory.
- The full fine CLI, 36,800-battle compatibility script, evaluations, paired comparisons and six detailed replay checks completed successfully. Compatibility repetitions are not pooled with confirmation.
- Scoped whitespace/link, semantic content-scope and frozen-evidence checks passed. Frontend tests, separate smoke scripts and hosted CI were not rerun for this backend/content increment. The current changes remain unpublished; the earlier hosted pass covers its recorded main revision only.

## Scoped baseline acceptance and remaining work

After the passing policy, unchanged compatibility checks and full backend verification, `baseline accept` explicitly recorded **`TestResults/balance/baselines/crystal-creek-starter-v1.json`** against the corrected local confirmation bundle. The acceptance reason names the level-10 fixed Amulet/Goblin recipe and its two passing primary cells. The other fourteen cells remain diagnostics. This is a local regression reference, not a new policy decision, area-wide estimate or hosted gate. Retain the baseline, both evidence directories and matching executable together; all are ignored local artifacts, not durable published storage.

At completion of this fine-tuning increment, Blood Grove's Raven result remained **Inconclusive**, with its original 88.14–90.36% interval, and no exception or passing Blood Grove baseline had been accepted. The subsequent confirmation below resolved that checkpoint's acceptance question while preserving the original result. Natural spawn coverage, other chest outcomes, subjective journey acceptance and broader content adapters remain separate work.

**Subsequent Blood Grove decision:** the later [20,000-battle acceptance confirmation](Blood-Grove-Acceptance-Confirmation.md) passes at 89.13% / 76.21% and has its own local baseline, without changing the historical Inconclusive result. It uses newer game assemblies and covers Blood Grove only; this Creek baseline and its retained execution identity remain unchanged.

**Subsequent combined regression:** the [52,000-battle check](Starter-Regression-Review.md) verifies both accepted schedules on the retained 8 September build. All sixteen Creek cells and both Blood Grove cells reproduce exactly, all four primary goals pass, four replays match, and all 2,119 backend tests passed on that build. This is compatible regression evidence with no baseline promotion or pooled samples. The [9 September package](Starter-Baseline-Package.md) subsequently completed local packaging and independent recovery; off-device storage is deferred by user choice and hosted verification remains pending publication.

Changed files: `CrystalCreekPressureExperiment.cs`, `CrystalCreekHandoff.cs`, `Program.cs`; `BalanceHarnessCreekPressureTests.cs` and `AbilitySystemTests.cs`; `abilities.json`, `creature-abilities.json` and `ability-behaviors.json`; this fine review, the coarse review, starter acceptance, main harness plan, tool README and post-alpha roadmap. The two creature mappings affect those species wherever the shared catalog is used; other activity modes were not statistically assessed.

No migration, environment configuration, deployment, API restart or local-character change was made. The running API was not reloaded to these files; applying them to a running service belongs to its normal content release. The earlier local playtest therefore remains evidence of the prior live content.
