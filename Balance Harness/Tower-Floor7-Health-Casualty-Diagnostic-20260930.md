# Floor 7: target-Health casualty diagnostic — 30 September 2026

**Latest floor-7 lower target-Health trial (30 September):** The [lower target-Health Springtide trial](Tower-Floor7-Health-Scaled-Springtide-Lower-20260930.md) closed **`NoEligibleHealthSpringtide`** after **46,080 fresh fights / 384 reservations**, preserving **120 recipes / eight compositions**. At **17.5%**, 63 recipes fail the ceiling; at **20%**, two limited-equipment compositions meet the minimum but three ceiling failures remain; at **22.5%**, no limited composition meets the minimum (best **16/128 and 9/128**). **No confirmation or gameplay edit.** Independent audit verified every outcome and native preparation. **100 Python checks and 157 backend tests pass**, with four intentional skips, plus three native study fixtures. Nine new native cases cover lower fractions and Weaken/variance behavior using the exact archived combat DLLs. Exclusions: **921,340**. Next is a separate **fixed 21.25% midpoint**, proposed and unallocated, retaining every gate and requiring independent confirmation. Floor 7 remains unresolved; floor 4 remains the latest applied change. No dungeon or acquisition work.

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Continue the [rejected target-Health Springtide trial](Tower-Floor7-Health-Scaled-Springtide-20260930.md).

## Prospective protocol

Execute only the frozen `tower-floor7-health-springtide-diagnostic-v1` proposal: the rejected 25% target-MaxHealth Springtide candidate, original 0.20 source Power per Abundance, offense factor 0.60 and penetration factor 50. Preserve all 120 recipes / eight compositions and current catalogs. Use both original parent compositions with one specialized healer and full Health + Regeneration, and the first sixteen declared saved seeds: **four recipes / 64 exact historical replays**. Recount all 512 existing outcomes for those recipes first. No fresh acceptance fights, seeds, replacements or retries.

Add a separate diagnostic validator and safeguards. Authenticate the previous publication, source, recipes, catalog delta, original live content, production runtime and seed history. Reuse the unchanged 148 backend passes / four intentional skips after verifying their source/runtime bindings. Run relevant Python safeguards freshly. Native replays must match exact input and every saved combat/Tower result after excluding only the newly requested event log.

Admit the work from twice the measured time and archive bytes per replay of the previous 96-replay residual diagnostic. Require projected time below 960 seconds and bytes below 80% of 2 GiB. Whole diagnostic limit 1,200 seconds; each owned replay limit 60 seconds, each log 64 MiB, archive 2 GiB. Read only supervisor output while replays run.

Reconcile event damage, healing, regeneration and death ticks with native statistics. Keep all actors tied at the earliest death tick. Per-hit Health snapshots must use Damage/DamageCrit, excluding Death notifications. Partition party output by event order around slot 1's first death; do not combine same-tick events across that boundary. Record condition applications, resistance and removals separately. Condition notifications indicate observed state, not unlogged exact stack snapshots. Record guardian condition history before each Springtide hit and original-party condition timing before 48 seconds.

Independently audit every replay and reduction. Compare the four profiles descriptively with the earlier closed event diagnostic and its corrected Health snapshots; those archives use different seeds and are not paired counterfactuals. Investigate Springtide damage, slot-1 survival and lost party output before proposing another coefficient or mechanism. Stop after 64 replays; allocate no further candidate. No gameplay, search, acquisition, dungeon, migration, configuration, database or deployment change.

## Completed diagnostic

**All 64 historical replays matched the complete saved inputs and combat/Tower results.** The independent collector audited **96,067 events**, including recipient totals, first-death ties, damage attribution, event-order output windows, native Health snapshots and condition notifications. All **512 saved outcomes** for the four selected recipes were recounted before replaying. **Zero new acceptance fights, seeds or gameplay changes.**

Springtide caused **52 of 63 observed slot-1 deaths**. The median first original-party death was **48 seconds** in every profile, around the fourth Springtide cast. Slot 1 was tied for the earliest death in **47 of 64 fights**. Party output is lower in the remaining fight after that casualty:

| Composition / gear | Saved diagnostic wins / 16 | Slot-1 deaths | Slot 1 tied for earliest death | Springtide caused slot-1 death | Output before / after slot-1 death (damage/s) |
| --- | ---: | ---: | ---: | ---: | ---: |
| A/restorer-specialization | 1 | 15 | 10 | 10 | 165.4 / 133.7 |
| B/restorer-specialization | 0 | 16 | 12 | 13 | 171.3 / 110.6 |
| A/health-and-regeneration | 0 | 16 | 12 | 15 | 168.6 / 56.8 |
| B/health-and-regeneration | 0 | 16 | 13 | 14 | 169.9 / 114.4 |

The 16-seed results are selected historical descriptions, not new estimates or acceptance evidence. Output rates pool damage/exposure only for fights with a slot-1 death; other simultaneous deaths, changing conditions and different phases prevent a causal attribution of the full rate difference to one character. Damage continuing from a dead character's effects can appear after its death. The unchanged full 128-seed screen remains the rejection evidence.

### Health around the fourth cast

| Composition / gear | Previous slot-1 Health after 48s | New slot-1 Health after 48s | New fatal hits / observed hits |
| --- | ---: | ---: | ---: |
| A/restorer-specialization | 16.64% (16 hits) | 3.83% (15 hits) | 9/15 |
| B/restorer-specialization | 15.46% (16 hits) | 0.32% (15 hits) | 12/15 |
| A/health-and-regeneration | 23.33% (16 hits) | 0.48% (16 hits) | 15/16 |
| B/health-and-regeneration | 19.39% (16 hits) | 0.00% (11 hits) | 11/11 |

These are native post-hit Health fractions. Fatal hits contribute zero, but characters already dead do not receive a hit. The earlier archive uses different seeds, and survivor counts differ: these are not matched-survivor comparisons. Per-hit snapshots exclude Death notifications. The full Health/Regeneration parties suffered earlier losses despite their larger Health pools. All four profiles dealt roughly **8,046–8,164 guardian Health damage before 48s**, while guardian healing was **974** in every profile; the remaining fight was still needed to win.

## Why the percentage change was harsher than the initial arithmetic suggested

Two shared engine rules matter. `GetEffectiveAttribute(Power)` applies source-Power adjustments, including **20% Weaken**. A target-MaxHealth base bypasses that adjustment; its separate Abundance bonus still uses effective source Power. Also, `ApplyEffectOnce` applies **±20% magnitude variance only when the main ScalingAttribute is Power**. Changing that main attribute to MaxHealth removes the variance from the whole computed hit, including its additive Power term. Critical hits, mitigation, barriers and block still operate.

The previous static comparison used prepared Power without the observed Weaken adjustment and did not model the Power-only variance rule. That understated the relative pressure on high-Health characters. The new review makes both assumptions explicit; this is not a change to the combat engine.

A successful guardian Weaken application preceded **all 320 first-cast original-party hits** in the new diagnostic and **all 320 comparable first-cast hits** in the older logs, with no preceding Empower application and one logged Abundance application. The review checked **602 noncritical hits**: all **305 new hits** exactly matched the unvaried target-Health formula; all **297 older hits** fell within the native Power-formula variance envelope. No additional replays were run for this comparison.

At prepared guardian Power **747.93256**, Weaken gives effective Power about **598.346**. With one assumed Abundance stack, the original unvaried hit is **718**, with a rounded variance envelope **574–862**. The 25% candidate instead gives **958** against slot 1's **3,352 Health**, or **1,195** against its **4,300 Health** with full Health/Regeneration. Every recorded noncritical first cast against slot 1 in the new archive matches those values:

| Composition / gear | Previous noncritical first-cast mean (hits) | Target-Health first-cast raw damage (hits) |
| --- | ---: | ---: |
| A/restorer-specialization | 701.36 (14) | 958 (16) |
| B/restorer-specialization | 725.00 (15) | 958 (14) |
| A/health-and-regeneration | 704.13 (15) | 1195 (16) |
| B/health-and-regeneration | 705.79 (14) | 1195 (16) |

This supports the damage-formula explanation for stronger early pressure; it does not isolate each rule's contribution to the win-rate collapse. Later condition notifications are not exact native active-stack snapshots. Four later original-party hits in the new archive followed a Weaken expiry notification; no claim of uninterrupted whole-fight Weaken is made. The full Health/Regeneration gear also loses Tenacity and receives more incoming Slow/Weaken applications, so the equipment comparison remains multi-attribute.

## Recommended next balance trial

**Test a lower target-MaxHealth base: 17.5%, 20% and 22.5%.** Keep **0.20 source Power per Abundance**, offense factor **0.60** and penetration factor **50**. Build from the original current-live 120-recipe source, not from the rejected 25% content. This is the smallest existing data-driven change justified by the observed pressure; do not add a mixed-scaling mechanic or change shared variance rules yet.

At four assumed Abundance stacks and Weaken only, static raw damage relative to the old unvaried Power formula is:

| Next base fraction | One-healer slot-1 raw damage change | Full Health/Regeneration slot-1 raw damage change |
| ---: | ---: | ---: |
| 17.5% | -1.09% | +14.31% |
| 20% | +6.69% | +24.29% |
| 22.5% | +14.47% | +34.28% |

The 17.5% endpoint nearly restores the old nominal pressure on the one-healer slot-1 Health pool while retaining additional pressure on full Health/Regeneration. These numbers omit mitigation, criticals, barriers, timing and the removed variance; **they are not win-rate predictions**.

The frozen proposal is `TestResults/tower-floor7-health-springtide-lower-proposal-20260930.json`. It permits three independent **128-seed screens** and at most one **160-seed confirmation**, retaining all **120 recipes / eight actual compositions**, at least two qualifying limited-equipment compositions, and every recipe's ceiling. Gates remain **25–44/128** and **30–57/160**. Rank by second-best limited-composition lower bound, then smallest maximum upper bound, then lower Health fraction. Maximum future scope: **65,280 fights / 544 reservations**, with measured resource admission before each allocation. No fourth candidate, retry, extension or pooling.

Before testing, add native cases for all three lower fractions, source Weaken, the source-Power Abundance bonus and the Power-only magnitude-variance distinction. Run backend tests through `build/run-tests.ps1` and preserve the archived combat DLLs. Confirmation, independent audit, isolated native parity, local application, regression and live parity are all required before adoption. **Status: proposed; zero candidate catalogs materialized, panels allocated or fresh seeds reserved.**

## Verification and scope

**79 fresh Python safeguards pass**, including sixteen new selection/condition/death-order cases, the corrected residual Health-snapshot suite, and existing diagnostic/candidate guards. A synthetic test fixture initially reused parent labels for unrelated controls; the strict unique-baseline check rejected it. The fixture was corrected before any replay, the initial failed fixture/log were preserved, and both the corrected suite and the remaining candidate suite passed. No production guard was weakened.

The unchanged **148 backend passes / four intentional skips** were authenticated and reused; no C# source or runtime changed. All 64 native CLI replay commands succeeded with no timeout, retry or remaining child. No verification remains blocked. The read-only first-cast review initially assumed unvaried old Power damage; examining the mismatch identified the engine's ±20% rule, and the final review separately verifies the old range and new exact values. The initial review source is retained.

Summed owned replay time: **91.281s**. Closed archive: **174,600,308 bytes**. Prospective two-times estimates were **181.717s / 374,547,488 bytes**, within the declared bounds. Exclusions remain **920,956**. The next proposal allocates nothing.

Changed maintained files: new `analysis/diagnose-tower-health-casualties.py`, its sixteen-case regression suite, this report, preceding trial notice, continuation handoff, balance status, gear coverage and both harness guides. No gameplay, search, progression, acquisition, dungeon, migration, configuration, database or deployment changes. Floor 7 remains unresolved; floor 4 remains the latest applied balance change.

Commands used bundled Python with `-B -X utf8`: preparation; six safeguard suites; the new diagnostic with the frozen plan/entry/protocol and bound runtime; independent collector; first-cast review; lower-bracket proposal; publication. Native replay command arrays, process receipts and original outputs are stored in the closed archive. Publication verifies current/source/runtime/ledger bindings, local Markdown links and `git diff --check`.

## Evidence

- Closed diagnostic: `TestResults/tower-floor7-health-casualty-diagnostic-20260930`, manifest **`28db6e210dce23a2c644a89f663e20b225045c1eac597c1f65f62126bffbffcd`**.
- Independent event audit: `TestResults/tower-floor7-health-casualty-evidence-20260930.json`, SHA **`3dc96c12472735581da47cd84aac3869ed421112aa88e3741154f6c6be61584c`**.
- Native first-cast review: `TestResults/tower-floor7-health-casualty-review-20260930.json`, SHA **`06a325d76ec9240ed5f62319a195883693e29f361a520c5ababb58bdaa567006`**.
- Unallocated lower bracket: `TestResults/tower-floor7-health-springtide-lower-proposal-20260930.json`, SHA **`a4f11d139859df1f3ce040ae5a536e96c896286da574c7c72e97744c6b94842f`**.
- Publication: `TestResults/tower-floor7-health-casualty-publication-check-20260930.json`.
