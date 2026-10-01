# Floor 9: corrected-runtime Ni gear diagnosis — 1 October 2026

**Follow-up completed:** The [full Ni penetration trial](Tower-Floor9-Ni-Penetration-20261001.md) rejected the proposed setting after 18,944 fights. No confirmation or live application. Next is a separately proposed limited-Restoration diagnostic; see the current handoff. The diagnosis and original proposal below remain historical evidence.

## Completed findings

**96/96 exact historical replays completed, with 199,589 independently audited events.** All complete reports match their saved inputs and outcomes after removing only the event log. No new seeds, acceptance fights or gameplay edits. Floor 9 remains unresolved.

| Equipment across both compositions | Wins / 32 replays | Copy deaths / 288 available copies | Ninth Seal share of party Health damage |
| --- | ---: | ---: | ---: |
| Baseline | 4 | 225 | 53.4% |
| Eight specialized items | 11 | 256 | 43.6% |
| Full Resistance + Health | 30 | 288 | 32.3% |

The corrected runtime makes copies killable with ordinary equipment. Limited gear removes **256/288 copies**, so the previous pre-fix explanation that parties almost never reach copy removal no longer describes this sample. Full specialization still creates a large survival/output advantage. These are descriptive paired replays selected from a closed diagnostic; the 32 observations combine two compositions and are **not** a single recipe's acceptance estimate.

All nine copies remain alive at every first original-party casualty. **74** first casualties follow Ninefold Strike (**69** repeat hits, **5** main hits), **18** follow Ninth Seal, and **4** follow basic attacks. Better gear does not consistently postpone that first death: A's median first-casualty times are **12/16/12 seconds** for baseline/limited/full, and B's are **8.8/8/8 seconds**. The later difference matters: after the first casualty, limited A/B parties deal about **158.5/161.1 Health damage per second to Ni**, versus **233.2/201.9** for full resistance. These rates are descriptive exposure-adjusted totals, not evidence that a first casualty immediately lowers output; copy removal also changes subsequent targeting and pressure.

Ni has **no health swaps or logged healing** in any replay and only **129 total regenerated Health** across the entire sample. Every one of **769 copy deaths** has a corresponding permanent-Power notification, each adding **103 Power** at this replay setting. All copies retain **1,125 Health** and remain inert. A recovery nerf or another copy-Health reduction is not supported as the next response to these findings.

## Next isolated trial

Test the existing guardian scaling fields with **offense ×0.95 of live** and **penetration ×40**. This means floor-9 offense **4.7036132812 → 4.4684326171** and the penetration multiplier **1 → 40**, predicting native ArmorPenetration and MagicPenetration **0.96 → 38.4**, below the current 40-point cap. This is a proposed, unimplemented, unallocated candidate. Keep health, defense, regeneration, the full Ni kit, nine copies, their 10% Health and correct defense inheritance unchanged. No ability redesign or summon penetration inheritance is needed; the copies do not attack.

The purpose is to reduce the encounter's sensitivity to heavily specialized defenses while offsetting ordinary-party pressure with lower offense. Against the replay's ×1.25 offense source, static current-rule arithmetic predicts about **2.0% less Power-scaled damage** to a 68.22-rating character, but **37.8% more magical damage** to the same slot with 360.54 ResistanceRating. The specialized character still has residual mitigation and more Health. This calculation holds ratings fixed and excludes crits, block, statuses, rounding, copy timing, targeting and flat basic-attack terms. **It is not a prediction of win rates.** Native preparation and typed-damage checks are required before allocating any candidate fight.

Start from the **original seed-free corrected-runtime source**, `TestResults/tower-balance-pass-floor9-summon-defense-preparation-study-20260929`, not the isolated ×1.25 diagnostic. Reuse the existing scalar/penetration owner with a separate strict fixed-plan aggregate and native application guard. Retain all **148 raw recipes / five actual compositions / 115 eligible recipes**, including full-Armor and full-Resistance ceiling controls.

The saved proposal is `TestResults/tower-floor9-ni-penetration-trial-proposal-20261001.json`, SHA **`8fc8c16d5d9821b6d83d167d7603f6da337dc91fa8eea3e09e6e6c099206f5fd`**. Screen with four 32-seed batches (**18,944 fresh fights / 128 seeds**); only a complete passing screen admits independent equal-size confirmation. Maximum **37,888 fights / 256 seeds**. Keep the simultaneous family gates: at least two eligible compositions with lower bounds ≥10%, all recipes' upper bounds ≤50% (**25–43 wins / 128**). No pooling, dropped controls, interim acceptance, extension, retry or live edit during testing. Require complete native input/replay parity before any local application. The proposed aggregate version is `tower-balance-ni-penetration-aggregate-v1`; it is not implemented yet.

## Verification and closure

**72 Python safeguards and 45 native tests pass**: the five Ni authoring/behavior cases and forty corrected defense-inheritance cases ran through `build/run-tests.ps1 -NoBuild`. The unchanged **766-case broader proof / four intentional skips** was authenticated and reused. Its pre-existing Kharad behavior-manifest failure remains separately recorded and unresolved.

An initial new-test fixture still pointed at the old proposal; it was corrected before replay and all checks passed in a fresh receipt directory. The first independent collector similarly imported the old admission constants and rejected the new proposal before recounting. Its original pinned file was preserved; a new collector changes only that import, then independently verifies every replay. **No combat was retried, no output was rewritten, and no admission rule was relaxed.** Both issues and their corrections remain auditable.

Native replay time totals **148.345 seconds**. The complete archive is **37,313,862 bytes**; largest raw log **4,101,615 bytes**. All process trees drained and log-capture/cleanup checks passed. Final exclusions remain **926,348**. All 102 catalogs and production code remain unchanged in this step. No migrations, configuration changes, database actions or deployment.

Independent evidence: `TestResults/tower-floor9-corrected-ni-evidence-20261001.json`, SHA **`f9d8eb907137d2023ae99782192859f12de985171710174992777fc618771850`**. Mechanism review: `TestResults/tower-floor9-corrected-ni-mechanism-review-20261001.json`, SHA **`e8b930ba316f51e918d01744df809457d208f6e3b81fab62a2834b4a8f1277d1`**. Closed owner: `TestResults/tower-floor9-corrected-ni-diagnostic-20261001`. The original protocol remains in the owner's `protocol.md`. Publication: `TestResults/tower-floor9-corrected-ni-publication-20261001/completion.json`.

Floor 9 still needs accepted balancing. Floors 10 and 12–15, followed by the final current-version 1–15 sweep, remain afterward. Preserve the applied floor-8 catalog data and shared defense fix.

## Frozen protocol

Target: the primary LL World Tower and offline Balance Harness. Explain the large gear gap in the closed [offense calibration](Tower-Floor9-Offense-Calibration-20261001.md), using its isolated **×1.25 offense** source and corrected production runtime. Live Ni offense stays **4.7036132812**; the replay source uses **5.8795166015**. Preserve original **10% copy Health**, correct rating inheritance, Ni's full kit, all 148 recipes and approved expected-progression gear.

Execute exactly **96 historical replays**: two compositions × baseline/eight-item/full-resistance gear × the first sixteen declared seeds. A uses saved lineup `5b6c297c…`, with specialized slots **2+4**; B uses `reference-3`, with slots **3+9**. Their complete saved 32-seed panels contain baseline/limited/full wins **2/10/29** and **3/9/30**. Recount all 192 saved outcomes, then replay the fixed subset without selecting favorable seeds. Preserve raw Essence order, actor identities, party positions and equipment order.

Use the proposal `TestResults/tower-floor9-corrected-pressure-proposal-20261001.json`, SHA `a7bec338180ac7474e81e34b1697315104e5ede876c7f00ef032fd66963bda61`. Corrected source manifest: `bfdb640a0a772a8cc88d3657709d345660a008030f0b209b560f2c86cb79fc83`. The pre-fix diagnostic stays closed; reuse its event/report analysis without changing its source or interpretation.

Require complete saved-report parity after removing only `eventLog`, plus the native replay-success footer. Reconcile recipient damage, mitigation, healing, regeneration and first-death ticks for every original character, Ni and all copies. Track the attack before each first casualty, copy spawn/death order, Ninefold Power notifications, Ninth Seal pressure, health swaps and party damage before/after casualties and in 0–16 / 16–32 / 32–48 / 48+ second windows. Preserve same-tick event order. Logged Power increments are not a reconstruction of Ni's current Power; swap notifications record Ni's Health gain, not healing to the copy.

Run the five existing Ni tests and forty defense-inheritance tests through `build/run-tests.ps1 -NoBuild`. Authenticate and reuse the unchanged **766-pass / four-skip** broader proof, retaining its separately recorded pre-existing Kharad manifest failure. Run corrected-selection safeguards and the existing event, parity and bounded-owner suites before replay.

No fresh study fights, seeds, acceptance evidence, confirmation or application. Initial and final seed exclusions must remain **926,348**. No retry, extension or outcome-driven change to this sample. Bounds: **96 replays / 840 seconds / 2 GiB**, with **60 seconds / 16 MiB per replay log**. Admit only if doubled authenticated prior Ni replay cost (plus 80 MiB temporary allowance) is below **672 seconds / 80% of 2 GiB**, and free disk exceeds projected bytes plus 2 GiB. The runtime changed, so this remains an estimate; native limits are enforced. Observe supervisor stdout only during execution.

Independently decompress and verify every closed log, compare complete reports and outcome schedules, recount native events and cleanup receipts, and recheck all source/runtime/catalog/ledger bindings. Then identify one evidence-supported next adjustment; no untested coefficient is an accepted balance result. Keep floor-8 applied catalog data and the defense fix. No deployment, migration, configuration change or database action.
