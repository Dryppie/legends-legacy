# Floor 2: fixed pressure precision study — 30 September 2026

**Subsequent diagnosis complete:** The [armor-outlier report](Tower-Floor2-Armor-Outlier-Diagnostic-20260930.md) recounts 3,072 saved outcomes and verifies 192 detailed replays with no new seeds or game edits. It recommends a separately frozen fixed Gale 0.35 / Feast 0.65 trial; the seed-free proposal is prepared. The precision result and protocol below remain closed and unchanged.

Target: primary LL World Tower and offline Balance Harness. Continue the [closed joint refinement](Tower-Floor2-Gale-Feast-Refinement-20260930.md) with a new prospective panel, holding **Crimson Gale 0.35 / Feast on Wounds 0.55** fixed. Earlier results select this candidate but contribute no acceptance observations.

## Prospective protocol

1. Retain the complete live-catalog source `TestResults/tower-balance-pass-floor2-two-armor-expanded-screen-study-20260929`, manifest `b8e1f3b5bd00c1feac0035976c10f8960d12e1ea03d6a8378ee73baeb20d9862`: all **115 recipes / seven actual compositions**, including every full-gear control and unsuccessful nominee. Preserve raw Essence order, actor identities, party slots and progression budgets. The partial target is armor on exactly slots 3 and 4: `mixed-armor-baseline-slots-3-4`.
2. Use the proposal `TestResults/tower-floor2-fixed-pressure-precision-proposal-20260930.json`, SHA `3bcdc724c1e917a6733aae0078464ae62128ec41f07e57290164699a64f82985`. Only the two damage coefficients and matching descriptions may change in isolated catalogs. Preserve floor-2 health/offense **2.6618600366 / 2.5333570679**, Dive **1.60**, Feast stack limit/healing/cap, cooldowns, targets, Bleed and Scent. Live Gale/Feast remains **0.65/0.20** until independent acceptance and native parity.
3. Implement and test aggregate admission, reconstruction, acceptance and application checks before allocation. Build the updated fixture through `build/run-tests.ps1` into a new artifact directory. A separate runtime copy retains the five original combat assemblies and all dependencies, replacing only the newly compiled test DLL/PDB; verify all five combat hashes against the source and rerun the full relevant suite against this exact runtime. The updated test assembly is separately pinned. Authenticate the previous publication and all live catalogs before changes.
4. Freeze four complete-family **128-seed batches** as one **512-seed screening panel**: **14,720 fights per batch / 58,880 total**. Use exclusive, predeclared paths. Each batch receives fresh disjoint seeds, excluding all **915,161** prior reservations and every preceding batch. No old observations, seed reuse, omitted loadouts, selected batches, early statistical stopping, extensions or retries. Each complete batch is independently reconstructed from production combat archives. Execution/admission failures preserve all outputs and close the scope without statistical acceptance.
5. Decide only after all four batches finish. Compute approximate simultaneous **95% Bonferroni-Wilson** intervals across all 115 loadouts at **512 samples each**. Every upper bound must be **≤50%**. At least **two distinct per-slot Essence-set compositions** at the exact partial-gear target must have lower bounds **≥10%**. IDs, gear and Essence order do not create new compositions. These rules imply **76–216 wins / 512** for a qualifying recipe; individual batch verdicts do not determine aggregate acceptance.
6. Only a passing combined screen admits one independent **512-seed confirmation**, again four complete-family batches of 128 with the identical candidate, raw recipes, runtime and catalog. Apply the same whole-family rule to confirmation alone. No second confirmation or alternative setting is admitted. Maximum prospective total **117,760 fresh fights / 1,024 reservations**.
7. Each native batch stays below **20,000 fights / 840 seconds / 2 GiB**, with a **900-second process owner**. Before each allocation, require twice the measured time/size estimate to fit within 80% of the envelope. The first batch of each phase uses the pinned preceding 0.35/0.55 screen as its resource reference; subsequent batches use the immediately preceding batch. Do not raise caps to finish. Complete aggregate audit requires all declarations, manifests, native inputs, outcomes, ledger history, zero retries and zero active children.
8. A passing independent aggregate confirmation permits native input/replay verification against the isolated accepted catalog, before any live edit. Every confirmation batch must match **14,720 inputs / 115 full replays**: **58,880 inputs / 460 full replays** in total, using saved seeds only. No synthetic passing per-batch audit may substitute for aggregate acceptance. After complete parity, only the accepted abilities bytes may be copied locally, followed by relevant backend regressions and exact allowed-delta checks. No deployment or shared database action.

All archives are immutable; test/build failures before allocation may be repaired with separate logs. Freeze implementation, protocol, runtime, initial history and all eight batch paths in `TestResults/tower-floor2-precision-driver-20260930/declaration.json` before the first reservation. Do not pool the rejected 128-seed refinement with this study. More precision does not guarantee acceptance or certify unsearched parties, broad archetypes or ordinary equipment acquisition. Accepted floor-5/floor-6 changes, the repeating gear curve, progression choices and withdrawn supplies remain intact. No search redesign or dungeon work.


## Completed result

**`PrecisionScreenNotAccepted`.** All four declared batches completed **58,880 fresh fights / 512 fresh reservations**, with all **115 recipes / seven compositions** retained. The independent collector recounted the complete panel and reproduced the decision. **No confirmation was allocated and no gameplay edit ran.** Live Gale/Feast remains **0.65/0.20**; all 102 live JSON catalogs are unchanged.

Four actual compositions at the exact two-character armor profile pass the minimum:

| Composition | Wins | Observed rate | Adjusted interval | Lower bound ≥10% |
| --- | ---: | ---: | ---: | --- |
| D | 143/512 | 27.93% | 21.54%–35.36% | Yes |
| C | 121/512 | 23.63% | 17.70%–30.81% | Yes |
| old A | 121/512 | 23.63% | 17.70%–30.81% | Yes |
| old B | 85/512 | 16.60% | 11.62%–23.16% | Yes |
| reference 3 | 61/512 | 11.91% | 7.76%–17.87% | No |

The only ceiling failure is **D with full armor and health**, recipe `generated-finalist/a28fc3e305e894c16b4f6e64c123e7f63fe895da2af506e9f7b4bd38d40c635a`: **251/512 (49.02%)**, adjusted interval **41.37–56.73%**. Acceptance would require no more than **216/512** under the unchanged rule. The remaining **114 recipes** all stay below the upper ceiling. Next strongest are old A full armor **187/512**, upper **44.25%**; old A with four armored characters in slots 1–4 **182/512**, upper **43.25%**; and C full armor **180/512**, upper **42.85%**.

This larger independent panel identifies one remaining ceiling problem; it does not establish that D's true rate is above 50%. The previous 50/128 D result was descriptive candidate-selection evidence, not a stable estimate or acceptance claim. It remains separate and is not pooled here. Do not extend this failed panel or discard D's full-armor control. Every batch verdict was ignored for aggregate acceptance; only the full 512-count family decided the result.

## Implementation and verification

- Added `analysis/tower-balance-aggregate.py` and `analysis/test-tower-balance-aggregate.py`: frozen complete-panel admission, unique fresh paths/seeds, identical raw family/settings/catalog/runtime, independent archive reconstruction, combined family intervals and a two-composition partial-gear gate. Confirmation requires the entire screen to pass. No synthetic passing per-batch audit is generated.
- Extended `analysis/check-tower-balance-application.py` and `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalanceApplicationTests.cs` to authenticate aggregate confirmation membership and verify native inputs/replays against isolated accepted content before a live edit. The legacy single-archive and qualification paths remain separate. The conditional application driver is prepared and syntax-checked, **not executed**.
- **18 fresh Python safeguard tests passed.** **108 backend tests passed / four opt-in skips** through `build/run-tests.ps1`, including seven new aggregate application contract cases and existing Tower/Velka/Kharad regressions. All **four fresh native study fixtures passed**. No retries or active child processes remained.
- The initial sandbox build could not read the existing NuGet configuration; the authorized retry succeeded. The clean build changed combat DLL hashes, so a separate runtime copied the original binaries/dependencies and replaced only the new test DLL/PDB. All five combat hashes match the earlier source; the full 108-test suite passed again against this exact runtime. An initial `-NoBuild` invocation before copying its build metadata ran no tests and is not counted. Both build/runtime attempts and final TRX are preserved.
- Each native batch stayed within the resource limits; times were **155.61, 158.81, 152.85 and 160.42 seconds**. Exclusions increased **915,161 → 915,673**. No migration, configuration, database or deployment change. No required verification remains blocked.

## Evidence

- Independent evidence: `TestResults/tower-floor2-precision-evidence-20260930.json`, SHA **`f299818ff85e001186f981cca31039ad56d55e175f0f65b2260ebe4ba1070d26`**.
- Frozen declaration: `TestResults/tower-floor2-precision-driver-20260930/declaration.json`, SHA **`9bcf49ab7125bfb07f7b043635dcd27e34261e9fad64d02617cf48a62e2c2193`**. Frozen protocol, candidate, initial history, admissions, commands, aggregate assessment and native TRX are in the same directory.
- Combined screen evidence SHA: **`67fa93f4feb5c44e2328c6d2e29cb136aad5907f1e3db22151da471ef0597d53`**.
- Complete native archives: `TestResults/tower-balance-pass-floor2-precision-screen-{1,2,3,4}-study-20260929`:

- Batch 1: `22ce145cefc04697eb3355d451725d5f1d2456db6dce1ebcc178a93513a0def2`; 155.61 native seconds, 14,720 fights.
- Batch 2: `2c41eb5b56e0eee68b771be38b4e738ffad91fd034e01d26334761915c5db6e0`; 158.81 native seconds, 14,720 fights.
- Batch 3: `366afa48c3373b0c6f85fe1b99c4a214a52968ef1bbeceb7c5d3fb24316dad19`; 152.85 native seconds, 14,720 fights.
- Batch 4: `df56b1e63d75fa8a93c3da9a56d53d564099e9073a606ff0a7b9301ac26dc9ff`; 160.42 native seconds, 14,720 fights.

- Latest ledger: `TestResults/tower-balance-pass-floor2-precision-screen-4-owner-20260929/seed-ledger.json`, SHA **`b4ddc09bd4c9dae5b7f9ca187f581f88e36750eb7cea3aca19222bb07ed56f38`**, plus all ancestors and later reservations.
- Runtime: `TestResults/tower-floor2-precision-runtime-20260930`. The five combat assemblies are unchanged; updated test assembly SHA **`183177f06edaed7cba476d8f9ee7fb325926150c596cdcf138bedf4425aaa25e`**.
- Live Tower SHA **`0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`**; abilities SHA **`fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`**.

## Recommended next work

**Diagnose the single full-armor outlier using the saved paired fights before another tuning trial.** Compare D's full-armor and two-character-armor fights with old A and C under the same 512 seeds. Separate opening casualties, later boss pressure, player damage and sustain; use a fixed declared subset for detailed saved-seed replays if archived summaries are insufficient. D differs from old A by the slot-1 substitutions `alpha_wolf` + `elder_treant_thornstorm` → `venomous_snake` + `viper`; the result alone does not establish which mechanism causes its advantage.

This is read-only mechanism work: no new seed allocation, coefficient sweep, larger panel at 0.35/0.55, acceptance exception, search redesign or dungeon work. Any subsequent balance candidate needs its own frozen scope and full independent acceptance. Preserve all 115 recipes and the current ≥10%/≤50% criteria. The implemented aggregate guards can support another justified fixed-setting trial, but this one is closed.

The live-catalog recipe source remains `TestResults/tower-balance-pass-floor2-two-armor-expanded-screen-study-20260929`, manifest **`b8e1f3b5bd00c1feac0035976c10f8960d12e1ea03d6a8378ee73baeb20d9862`**. These four rejected isolated archives may support diagnostics, not implicit live-catalog admission. Floor-6 health remains the latest applied Tower change; hypothetical gear ownership still does not establish ordinary acquisition or broad archetype coverage.
