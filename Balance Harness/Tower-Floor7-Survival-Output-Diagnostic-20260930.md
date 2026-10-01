# Floor 7: survival and damage-output diagnostic — 2026-09-30

**Latest floor-7 recovery trial (30 September):** The [fixed recovery trial](Tower-Floor7-Endless-Spring-Recovery-20260930.md) closed **`NoEligibleRecoveryPressure`** after **15,360 fresh fights / 128 reservations**, retaining **120 recipes / eight compositions**. At **0.75% recovery**, one-healer A/B win **70/128 and 51/128**, while A/haste wins **58/128**; all three exceed the **44-win ceiling**. Largest adjusted upper: **69.10%**. **No confirmation or gameplay edit.** All outcomes and native preparations were independently audited. **115 Python checks, 165 backend cases / four skips and one native study fixture pass**. The NuGet access issue was recovered before study allocation. Exclusions: **921,596**. Next is one frozen, unallocated **0.9% recovery** trial, with Springtide **21.25%**, bonus **0.20**, offense **0.60**, penetration **50**, all original gates and independent confirmation. Add a separate strict refinement version and native cases. Floor 7 remains unresolved; floor 4 is the latest applied change. No dungeon or acquisition work.

## Frozen protocol

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Continue the [rejected 21.25% midpoint](Tower-Floor7-Health-Scaled-Springtide-Midpoint-20260930.md). This is descriptive historical replay work, not another acceptance sample or a gameplay change.

Authenticate publication `15d99abfc46f168aa40b78b5a808d77b4c0e24c6665291a881a2353a77da8d65` and frozen proposal `TestResults/tower-floor7-survival-output-diagnostic-proposal-20260930.json`, SHA `67f492b5fa83fa2d85de13f173ad30c5c3e8918d61be50b1a06edbcc23309e1c`. The source is `tower-balance-pass-floor7-health-springtide-midpoint-1-screen-study-20260929`, manifest `375e912a74bca3a32ebe1b3f60e6ea308916dd033b9af0c7f64d023d65750858`. Preserve all **120 recipes / eight compositions** and **921,468 exclusions**.

Replay exactly **48 historical fights**: A/Ability Haste, A/one specialized healer, B/one specialized healer, each on the first sixteen declared saved seeds in original order, including wins and losses. Haste uses five specialized items across five characters; each healer setup uses six items on slot 2. Preserve every ordered Essence, identity, position and expected level/gear budget. Recount all **384 saved outcomes** for these recipes before replay. No replacement, retry, extension, new seeds or new acceptance fights.

Authenticate the isolated candidate against the original live source: target-Health fraction **21.25%**, source-Power Abundance bonus **0.20**, offense factor **0.60**, penetration factor **50**. Use unchanged archived native assemblies. Every detailed replay must match the saved input, prepared participants, complete report and Tower outcome. Reuse the verified **157 backend passes / four intentional skips** and rerun relevant Python safeguards, including the new exact-scope and event-accounting tests.

Admit time and bytes at twice measured cost from the preceding 64-replay casualty diagnostic. Limits: **48 replays**, **1,200 seconds** total, **60 seconds** per replay, **64 MiB** per log, **2 GiB** total; projections below 80% of overall limits. Observe only owner stdout during execution, leaving active archive files closed.

Measure party casualties and guardian output separately by original actor, native summon identity and other actors; separate `condition.*` damage sources from direct damage. Post-death damage may include ongoing conditions. Preserve tied deaths and event-order boundaries. Use half-open windows **before 48s**, **48–60s**, **60–72s**, and **after 72s**, plus full-fight totals. At the actual 60-second Endless Spring heal, record the prior observed native guardian snapshot and healing snapshot, the event order, and the preceding/following three-second windows. An absent heal remains absent; no exact stacks or unlogged conditions are inferred.

Reconcile actual healing, regeneration and damage with native statistics. Report full-fight healing potential/overhealing separately; individual event magnitudes are actual recovery, not nominal healing. Guardian net logged Health change means actual healing plus regeneration minus Health damage; it does not replace native snapshots. Independently recount the new event partitions, damage origins, healing sources, snapshots and totals from the closed logs. Inspect Slow/Weaken, Abundance and Ancient Heartwood notifications without treating the observations as a causal counterfactual.

Use the results to decide whether recovery pressure, late damage, Health, coefficient refinement or precision deserves a separate declared trial. Select no numerical gameplay candidate before reviewing the diagnostic. No migrations, configuration changes, database changes or deployment.

## Execution

The protocol is frozen before replay allocation. Results and interpretation will be appended after independent verification.

## Completed diagnostic

**48 native attempts produced 48 exact historical matches**, with **85,823 independently audited events** and **384 saved outcomes recounted**. The full 120-recipe / eight-composition source remains intact. This work used **zero new acceptance fights or seeds**. Floor 7 remains unresolved; no gameplay change was applied.

The principal finding is a combined recovery and casualty threshold at 60 seconds. **All 39 fights with the observed 60s Endless Spring heal also suffer Springtide deaths on that tick, and all 39 lose.** The heal is **584 Health** in every case. Across the entire 48-replay set, every point of guardian recovery comes from Endless Spring: **zero regeneration, other healing or overhealing**.

| Composition / gear | Historical wins / 16 | Median first death | Mean guardian recovery | Output before / after first death (damage/s) |
| --- | ---: | ---: | ---: | ---: |
| A / Ability Haste | 3 | 48s | 1722.6 | 192.7 / 123.3 |
| A / one specialized healer | 0 | 56.8s | 2215.5 | 177.6 / 134.6 |
| B / one specialized healer | 1 | 48s | 2221.6 | 171.7 / 137.7 |

These first sixteen saved seeds per recipe are descriptive, not new acceptance estimates. The complete rejected 128-seed screen still supplies the balancing decision: A/healer **21/128**, B/healer **15/128**, below the minimum **25**; A/haste **34/128**, within the maximum **44**. First-death output rates pool damage/exposure only over fights with an original-party death; changing phases and simultaneous deaths prevent assigning the entire difference to a single casualty. Ongoing conditions can deal damage after their source dies.

## What happens at 60 seconds

Native event order is: Abundance application, **Endless Spring healing**, then Springtide damage/deaths. Other party actions can occur between the heal and Springtide. The independently verified original-party records show **117 Springtide deaths after the heal on that same tick**: 11 for A/haste, 60 for A/healer and 46 for B/healer. There are no original-party deaths earlier than that heal on the same tick in these 39 fights.

| Composition / gear | Observed 60s heals | Original characters alive before / after the tick (mean) | Party wipes on that tick | Damage in preceding / following 3s windows (mean) |
| --- | ---: | ---: | ---: | ---: |
| A / Ability Haste | 8 | 1.38 / 0.00 | 8/8 | 291.2 / 0.0 |
| A / one specialized healer | 16 | 4.44 / 0.69 | 5/16 | 968.4 / 166.9 |
| B / one specialized healer | 15 | 3.73 / 0.67 | 7/15 | 758.0 / 125.5 |

The three-second windows are **[57s, heal event)** and **[heal event, 63s)**. Thus the following window includes any party damage between healing and the subsequent Springtide, as well as damage after casualties. They are damage totals, not equal-exposure rates: some fights end before 63s. A full original-party wipe does not exclude a remaining summon or damage dealt earlier on the tick.

For A/haste, all eight fights reaching this heal lose every remaining original character to Springtide and produce **zero damage** in the following window. For A/healer, the mean native guardian snapshot rises **489.9 → 1,073.9 Health** at the heal; for B/healer it rises **901.7 → 1,485.7**. The snapshot deltas agree with the actual 584-point heal. For A/haste the corresponding mean is **646.3 → 1,230.3**.

Ancient Heartwood Defense/Resistance buffs and Tranquil Waters use occur **after** the Springtide casualties in **11 A/healer** and **8 B/healer** fights. Neither occurs afterward in the eight A/haste fights, which end at the casualty threshold. Those later defenses and conditions can affect the remaining output, but cannot explain the already-recorded deaths. Slow/Weaken and Abundance notifications are retained; they are not treated as exact active-stack snapshots or a complete record of condition state.

## Damage and recovery through the threshold

| Composition / gear | Damage before 48s | Damage 48–60s | Damage 60–72s | Healing 60–72s |
| --- | ---: | ---: | ---: | ---: |
| A / Ability Haste | 9226.8 | 1304.6 | 0.0 | 292.0 |
| A / one specialized healer | 8068.5 | 2642.6 | 240.9 | 754.5 |
| B / one specialized healer | 8150.8 | 2205.4 | 197.8 | 760.6 |

These are means over **all sixteen** replays in each profile, with zero contribution after an ended fight. Windows are half-open and the 60s heal belongs to 60–72s. Only 8 A/haste, 16 A/healer and 15 B/healer fights have positive exposure in that window. For the healer profiles, mean condition-source damage in 60–72s is **147.8 / 93.8**, while other direct damage is **93.1 / 104.1**. The diagnostic also retains original-party versus native-summon attribution, per-slot output, healing potential and native health snapshots.

The evidence supports testing recovery pressure while preserving the existing damage settings. It **does not show that removing the 60s heal alone would win these fights**. Casualties coincide with that recovery, and subsequent damage, healing, conditions and ability ordering would change under a different candidate. Do not subtract healing from saved remaining Health and count hypothetical wins.

## Next fixed trial: reduced Endless Spring recovery

**Test 0.75% MaxHealth per Abundance stack instead of 1%**, retaining the rejected midpoint's **21.25% target-MaxHealth Springtide base**, **0.20 source Power per Abundance**, **0.60 offense factor** and **50 penetration factor**. This is a modest 25% reduction in the recovery coefficient, selected after reviewing the completed diagnostic. It preserves the existing casualty pressure while testing whether accumulated recovery is preventing the longer-lived healer parties from finishing. Haste may also improve and breach the ceiling; no success or win-rate prediction is assumed.

Use the **original current-live 120-recipe source** and reconstruct the complete delta explicitly. Preserve health, interval timing, Abundance generation, noncritical healing, Heartwood, Tranquil Waters, raw identities, Essence order, party positions and progression gear. The current source's Endless Spring description uses the dynamic `{statusScaling}` token and does not require a hardcoded percentage edit. The existing health-pressure-v1 contract rejects additional ability edits: create a **separate exact compound contract**, retaining the old guard and adding rejection tests plus native healing/interval cases before allocating a study.

Frozen proposal: `TestResults/tower-floor7-endless-spring-recovery-proposal-20260930.json`. It permits **one 128-seed screen** and, only if it qualifies, **one independent 160-seed confirmation** across **all 120 recipes / eight actual compositions**. Keep at least two qualifying compositions using no more than eight specialized items on two characters, adjusted lower bound **10%**, and every recipe's adjusted upper bound at most **50%**. Count gates remain **25–44/128** and **30–57/160** using the original 95% simultaneous Bonferroni-Wilson method.

Maximum future scope is **34,560 fights / 288 reservations**. Require measured time/bytes admission before each allocation, complete independent outcome/preparation audit and native parity before local adoption. Close the fixed scope if either phase fails; no second coefficient, retry, extension or pooling. **Status: proposed; zero candidate catalogs materialized, panels allocated or new seeds reserved.**

## Verification and recovered reporting failure

**104 distinct fresh Python safeguard tests passed**, including 25 cases for the new diagnostic: exact selection, event boundaries, clipped exposure, tied deaths, absent heals, damage origin, ongoing condition damage, healing snapshots and omitted optional zero statistics. The unchanged **157 backend passes / four intentional skips** and nineteen Springtide cases were authenticated and reused; no fresh backend test is claimed for this reporting-only change.

The first native replay succeeded, but the Python reporter then failed on a missing `overhealing` key. Native `EntityStats` deliberately omits that field when its value is zero. The reader now treats omitted healing potential/overhealing as zero, with tests for both omitted and nonzero values. The failed owner, its original source and successful native log are preserved. A separate continuation analyzed that log and executed **only the remaining 47 replays**. **No duplicate native attempt, seed replacement or replay retry occurred.** All 48 complete reports match their saved battles, including the first log.

Both owners are closed; all native processes exited successfully with no timeouts or children left running. Summed native replay time was **70.110s**; combined archive size **161,187,050 bytes**, below the original bounds. Continuation admission preserved the original 1,200-second deadline and 48-attempt scope. Exclusions remain **921,468**. No verification command remains blocked.

Maintained changes: new `analysis/diagnose-tower-survival-output.py`, its 25-case test file, this report, the preceding midpoint notice, continuation handoff, balance status, gear coverage and both harness guides. Commands used bundled Python with `-B -X utf8`: entry preparation, seven safeguard suites (only the changed suite rerun after repair), fixed diagnostic, scoped continuation, independent collector, closed-log review/proposal and publication. Native command arrays and process receipts are archived. Publication authenticates source/runtime/ledger/content pins, Markdown links and `git diff --check`.

No gameplay, search algorithm, progression, acquisition, dungeon, migration, configuration, database or deployment changes. Floor 4 remains the latest applied Tower adjustment. The new evidence is diagnostic progress toward a floor-7 candidate, not a completed floor-7 balance fix.

## Evidence

- Preserved first owner: `TestResults/tower-floor7-survival-output-diagnostic-20260930`, manifest **`c40286106651475bb18af591b2e6629b5a81386b140dd303e10ee6311f0171c5`**.
- Completed continuation: `TestResults/tower-floor7-survival-output-continuation-20260930`, manifest **`10a8190731d01061c4fe8d2b2452762d1a41e2d7e57ae412669a5cbd937b05d4`**.
- Independent event audit: `TestResults/tower-floor7-survival-output-evidence-20260930.json`, SHA **`6cab4051a772ed2552698e26b728ec393ec71a0865e7695f8ed761cbe0c5e705`**.
- Closed-log frame-order review: `TestResults/tower-floor7-survival-output-review-20260930.json`, SHA **`2867f3990fdfc5caba47b52a98e10e43ac8ef043aaf164870808383804d92154`**.
- Unallocated recovery proposal: `TestResults/tower-floor7-endless-spring-recovery-proposal-20260930.json`, SHA **`42a35b0120f7b56cb7d07f85eaf5f448d88f3a5c12a8b20d65834fd3d4b4d530`**.
- Publication: `TestResults/tower-floor7-survival-output-publication-check-20260930.json`.
