# Captured-v19 ceiling correction screen — frozen design

Frozen **14 September 2026** after the [zero-combat diagnosis](Tower-Portfolio-Ceiling-Diagnosis-Review.md). Target: an isolated offline BalanceHarness study using **captured-v19 gameplay**. **Design only: no content copies edited, seeds allocated or fights run.** This document does not authorize execution or application. Machine-readable inputs, exact variants, family and stopping rules are saved in the [correction design](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/correction-screen-design.json).

## Hypothesis and scope

Test whether a modest linked increase to Kharad Health/Power can bring the entire 253-recipe challenger family below the 50% ceiling while preserving at least one supported ≥10% viable party. The evidence suggests a combination of sustained basic attacks, Poison, recovery and control. It does not isolate an Essence defect, so this design changes only the two existing encounter scaling inputs. Linked scaling follows the established boss-tuning convention; the factor grid is a bounded exploratory choice, not an inferred optimum or guaranteed sufficient range.

Use the completed confirmation's captured executable, all 16 content files, settings and exact recipes. Preserve each scenario ID, actor identity, party/equipment order, Essence order, level, training and ownership assumptions. Floor 5 remains ten level-40, tier-1/rank-2 Standard characters, five level-1 unascended/unevolved Essences per character, the fixed gear and no styles/contributions. There is no discovery, recipe substitution or control promotion.

Only isolated copies of floor 5's `guardianScaling.health` and `guardianScaling.offense` may differ:

| Factor | Health | Offense / Power |
| ---: | ---: | ---: |
| 1.00 baseline | 3.536624332800 | 4.470293484800 |
| 1.04 | 3.678089306112 | 4.649105224192 |
| 1.08 | 3.819554279424 | 4.827916963584 |
| 1.12 | 3.961019252736 | 5.006728702976 |

Apply every factor directly to the captured baseline using exact decimal products, without compounding. Baseline content is byte-identical. Restoring the two allowed fields in each candidate must reproduce the entire baseline JSON; every other file retains its hash. Defense, resistance, penetration, regeneration, stagger, abilities, equipment, Essence content and catalogs remain outside this experiment.

## Family, schedule and caps

- **All 253 confirmed recipes**, including 112 controls, 141 generated recipes, all 119 ceiling-only additions and every supported breach. Preserve all source associations and original/screened nominations. No selection based on this screen can alter the original reliability result.
- **128 fresh shared seeds** for every one of **1,012 setting/recipe cells**. Reserve exactly 128 values during separately authorized preparation; allocate none in this task. Exclude the complete **481,219-value ledger**, including unused reservations, plus any additional registered history found before preparation.
- Deterministic allocator: `StableRandom.Seed("tower-captured-v19-ceiling-screen-v1", "2026091421", "screen", invariantDecimalAttemptIndex)`, starting at zero, rejecting exclusions/duplicates, with at most 100,000 proposals. Freeze the exact accepted order before combat. A changed history invalidates preparation until explicitly reconciled; it does not authorize extra sampling.
- **129,536 maximum fights = 4 × 253 × 128**, including the fresh baseline. **Zero retries, replays or discovery fights.** All interrupted/failed starts consume the global attempt budget.
- **14,400 seconds / 8 GiB total new output**, including setup, content/executable copies, campaigns, logs and reconstruction. Sequential prepared execution, 32-record chunks, one global durable attempt ledger and resource controller. Time/storage stops retain all evidence. These are limits for a new proposed study, not increases to any old cap.
- Execute factor order 1.00, 1.04, 1.08, 1.12; recipe IDs in ordinal order; the frozen shared seed order within each cell. Complete every planned cell unless integrity, cancellation or resource rules stop the study. Do not prune factors after seeing outcomes.

The completed captured-v19 confirmation used the same total fight count in 52.46 minutes and about 1.41 GiB for its study. This provides a rough planning reference only: changed fights and four content variants can have different durations, archive overhead and storage. The resource limits are not a runtime promise. Store new work only under an absent `TestResults/balance/tower-captured-v19-ceiling-screen-20260914` directory; never reuse sealed paths or hard-link writable inputs to them.

## Selection and statistical interpretation

Use two-sided approximate Bonferroni-Wilson intervals with **alpha .05 divided over all 1,012 cells**, including baseline and all unselected settings. Draws count as non-wins. Do not combine these samples with discovery, the preceding confirmation or a later full-family study.

A nonbaseline factor is eligible only if **all 253 upper bounds are ≤50%**, at least one lower bound is **≥10%**, and the strongest observed rate is in **[15%,40%]**. Among eligible factors, minimize the absolute distance of the strongest observed rate from **30%**, then prefer the smaller factor. The interior preference is a selection rule; the acceptance target remains the inclusive 10–50% band. Baseline is measured/reported but cannot be selected as a correction.

If no candidate qualifies, report **Unresolved at the frozen grid/caps**. No interpolation, automatic grid extension, additional seeds, post-hoc smaller family or precision top-up is allowed. Retain every cell and every >50% observation. Formal paired significance is outside this design; shared-seed differences may be reported descriptively. An eligible setting is only **CandidateForFullFamilyConfirmation**. It is not a product Pass, search-reliability Pass or permission to apply content.

## Preparation and verification before execution

The statistical design is fixed; its executable protocol is not yet prepared. The next engineering step is zero-combat preparation and strict input validation:

1. Verify the design's source manifest, captured content/settings/executable hashes, all 253 seed-free recipes and complete registered exclusions. Preserve the shared dirty checkout. Any updated gameplay is a different study and must not be silently substituted.
2. Create four independent content copies and compatible definitions. Materialize all **1,012 cells without simulation**, verifying all budget/identity and two-field-only rules. Freeze exact seeds, prepared-input hashes, driver/commands, accounting contract and protocol before the first combat.
3. Integrate existing prepared compact execution and complete archive verification under one durable global ledger. Keep storage caps, attempt flushes, cancellation and strict immutable-source verification intact. Do not weaken guards to fit the new design.
4. For implementation changes, run relevant contract and regression tests through `build/run-tests.ps1`. Cover scalar isolation, identical baseline/recipe preparation, exact family/schedule, cross-factor charging, cancellation, failed writes, completed reconstruction and whole-screen selection. Freeze bounded diagnostics separately; this design reserves **zero** replay fights.
5. After separately authorized execution, fully reconstruct every record and attempt, independently reproduce all 1,012 intervals and the selected factor, and verify sealed sources/current checkout preservation. A mismatch stops the study and preserves evidence. Partial evidence cannot pass; there is no automatic resume or retry.

## Full relevant-family acceptance remains separate

The new [retained inventory](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/retained-family-inventory.json) contains **43,879 recipe/context entries**, including the historical 17,821-party family and every saved unshortlisted evaluation in the inventoried later packages. It includes all 253 confirmed recipes. Its keys are descriptive fingerprints; strict harness materialization, context normalization and complete history coverage audit are still required before a full confirmation definition.

The existing staged schema's **20,000-cell / 500,000-fight** limits do not fit this inventory. Do not split it into unrelated nominal-95% families, drop earlier recipes, assume monotonicity, reuse old-setting outcomes or call the 253-recipe screen a complete acceptance check. A subsequent proposal must specify a complete compatible family, uncertainty across every planned stage, supported viability, seeds, capacity, time/storage limits and stopping rules before any confirmation. Any implementation/capability extension belongs to a new explicitly authorized contract; no old experiment cap changes.

Captured-v19 results also cannot certify the changed checkout. Current-gameplay adoption requires a separate producing-version decision and fresh applicable evidence. Until those boundaries are closed, reliability remains **Fail 1/3**, adoption **Hold**, and existing ordinary/joint family assessment **Fail / Fail**. Practical acquisition and floors 6–11 remain separate work.
