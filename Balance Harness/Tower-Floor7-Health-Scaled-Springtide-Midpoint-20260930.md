# Floor 7: fixed 21.25% target-Health Springtide — 2026-09-30

**Latest floor-7 survival/output diagnostic (30 September):** The [survival/output diagnostic](Tower-Floor7-Survival-Output-Diagnostic-20260930.md) completed **48 exact historical replays / 85,823 audited events**, with **zero new acceptance fights or seeds**. All **39 observed 60s heals** restore **584 Health**, then Springtide causes original-party deaths on the same tick; all 39 fights lose. Guardian recovery is entirely Endless Spring, while post-casualty output collapses. **104 fresh Python checks pass**; unchanged **157 backend passes / four skips** were authenticated and reused. An omitted-zero report field was repaired; the first successful native log was reused and only the remaining 47 replays ran. Next: one frozen **0.75% MaxHealth per-Abundance recovery** candidate, preserving Springtide **21.25%**, bonus **0.20**, offense **0.60**, penetration **50**, all **120 recipes / eight compositions**, every gate and independent confirmation. Add a separate strict compound contract and native healing guards before study allocation. No candidate materialized, new seeds allocated or gameplay edit. Exclusions: **921,468**. Floor 7 remains unresolved; floor 4 is the latest applied change.

## Frozen protocol

Continue the [closed lower bracket](Tower-Floor7-Health-Scaled-Springtide-Lower-20260930.md). Target: the primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. At 20%, two six-item healer setups meet both per-recipe bounds, but three other recipes breach the ceiling. At 22.5%, all ceilings pass and no limited-equipment composition reaches the minimum. The numerical midpoint is one finite experiment; no monotonicity, feasible interval or win rate is assumed.

Authenticate the preceding publication (`4eb29f19b0c3cad55e357cb0befcc0b936f4b35c189158430ee616afcb688927`) and frozen `TestResults/tower-floor7-health-springtide-midpoint-proposal-20260930.json` (`2a819605342a42aff60bf4f10307427f39475d51c361a06bc41c70b799af5c76`). Begin with **921,340** seed exclusions. Reuse the complete original source `tower-balance-pass-floor7-mixed-resistance-screen-study-20260929`, manifest `1df97981c1cb0ee295286d63692a3ed1817cd928b644b5002e54666bb3469b31`.

Test only **21.25% of each target's MaxHealth**, preserving **0.20 source Power per Abundance**, guardian offense factor **0.60**, penetration factor **50**, all **120 exact recipes / eight actual compositions**, ordered Essences, positions, raw identities, equipment and controls. Build from the original live source, never a rejected candidate. Expected prepared guardian Power is approximately **747.93256**, stored penetration **43.2** and effective penetration **40**. All other preparation and catalog data must match. Floor-7 parties retain level 40, five Essences and tier-1 Unique / Exceptional / rank-4 expected progression gear.

Run one **128-seed / 15,360-fight screen**. Require at least **two actual compositions**, each using no more than **eight specialized items on two characters**, with adjusted lower win bounds at least **10%**, and all 120 recipes with adjusted upper bounds at most **50%**. Use **95% Bonferroni-Wilson /120**: screen gates **25–44 wins /128**. Only a complete passing screen permits one independent **160-seed /19,200-fight confirmation**, gates **30–57/160**. Maximum **34,560 new fights /288 reservations**. No extra candidate, retry, extension, pooling or selection from partial outcomes.

Before each allocation, estimate time and bytes at twice a completed panel's measured cost per fight: projected time below **672 seconds**, bytes below **80% of 2 GiB**, at most **20,000 fights** per panel. Native deadline **840 seconds**, owner deadline **900 seconds**. Use unchanged archived production binaries, authenticate the nineteen Springtide guards and rerun the 157-case regression through `build/run-tests.ps1 -NoBuild`. Rerun all 100 Python candidate/application/family safeguards.

Independently reconstruct exact catalog deltas, recount all raw outcomes and native preparations, equipment qualification, adjusted bounds, conditional confirmation and disjoint seeds. A passing independent confirmation requires isolated native parity before local application of the two scoped catalogs, followed by backend regression and live native parity. No deployment. If either phase fails, close this fixed midpoint and inspect the remaining gap before selecting another coefficient or mechanism.

## Completed result

**The fixed 21.25% candidate did not qualify. No confirmation or gameplay change.** All 120 recipes stayed below the 50% adjusted ceiling (largest upper **41.96%**). The best two limited-equipment compositions both use six Restoration items on the healer and won **21/128 and 15/128**, below the **25/128** minimum; their adjusted lower bounds are **7.96% and 4.95%**. The strongest recipe is composition A with Ability Haste on all five characters, **34/128**. The fixed trial is closed without extension.

| Phase / Health base | Best two distinct limited-equipment compositions | Qualifying limited compositions | Ceiling failures | Largest adjusted upper |
| --- | --- | ---: | ---: | ---: |
| screen-1 / 21.25% | 21/128 (restorer-specialization); 15/128 (restorer-specialization) | 0 | 0 | 41.96% |

Every phase preserved the 120-recipe / eight-composition family. The two reported leaders are distinct normalized compositions, not reordered representations. All confidence bounds use the complete family. No pooling, retry, sample extension or additional candidate.

### Equipment controls

| Phase / composition | Baseline | One healer | Full Health + Regeneration | Full Resistance + Health |
| --- | ---: | ---: | ---: | ---: |
| screen-1 / A | 7/128 | 21/128 | 2/128 | 5/128 |
| screen-1 / B | 3/128 | 15/128 | 1/128 | 2/128 |

A is original composition `dae6cc32…`, B `0d375ed9…`. These are descriptive per-recipe outcomes, not extra samples or proof of an isolated equipment mechanism.

### Saved survival/output comparison

Recounted **768 existing reports**, all 128 outcomes for A/Ability Haste, A/one healer and B/one healer at 20% and 21.25%. **Zero additional fights, replays or seeds.**

| Health base / composition / gear | Wins | Median first death | Death-free fights | Median winning duration | Mean guardian recovery | Median guardian Health in losses | Losses at or below 5% guardian Health |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 20% / A / ability-haste | 73/128 | 48.00s | 0 | 57.30s | 1,653 | 9.59% | 0 |
| 20% / A / restorer-specialization | 36/128 | 60.00s | 23 | 59.55s | 2,190 | 9.55% | 8 |
| 20% / B / restorer-specialization | 37/128 | 48.40s | 33 | 59.50s | 2,056 | 10.99% | 6 |
| 21.25% / A / ability-haste | 34/128 | 48.00s | 1 | 58.25s | 1,708 | 11.76% | 6 |
| 21.25% / A / restorer-specialization | 21/128 | 53.80s | 20 | 59.50s | 2,109 | 9.97% | 6 |
| 21.25% / B / restorer-specialization | 15/128 | 48.10s | 14 | 59.60s | 2,114 | 12.21% | 2 |

Independent seed panels, not paired counterfactuals. Slot counts include all actors tied for earliest death. First-death medians exclude death-free fights. Full-fight damage and recovery vary with duration and surviving actors; saved summaries cannot attribute causality or reconstruct missing event timelines. Remaining-Health margins are observed losses, not predicted wins under a Health nerf.

## Verification

Completed **15,360 fresh fights / 128 reservations**, ending at **921,468 exclusions**. Independently reconstructed every candidate catalog delta, recounted all raw outcomes, verified native preparations in every fight, and reproduced equipment qualification, adjusted bounds, selection, resource admission and the disjoint seed union. All study processes closed without timeout, retry or remaining owned child.

**100 Python safeguards and 157 backend tests passed; four intentional opt-in fixtures were skipped.** The backend suite ran through `build/run-tests.ps1 -NoBuild` against the unchanged archived runtime. All runtime files, native guard source and prior receipts were authenticated. Nineteen existing Springtide cases cover the fractions bracketing the midpoint, source-Power Abundance, Weaken, barriers, mitigation and the Power-only magnitude-variance distinction. No new native cases or engine changes were needed.

No verification command remains blocked. Native commands, filters, TRX files and process receipts are preserved under the fresh verification and study owners.

## Next work

**Next: the frozen, unallocated 48-historical-replay survival/output diagnostic**, from `TestResults/tower-floor7-survival-output-diagnostic-proposal-20260930.json`. Use the rejected 21.25% archive, A/Ability Haste, A/one healer and B/one healer, and the first sixteen declared seeds. Inspect casualties, outgoing damage and guardian recovery around 48–72 seconds, particularly the actual 60-second Endless Spring heal. Add the dedicated validator and rejection tests, authenticate native inputs and admit resources before execution. Preserve all 120 acceptance recipes and every gate. Zero new acceptance fights or seeds; no further percentage or other gameplay candidate is selected.

The saved summaries show a timing/recovery gap, not its cause. At 21.25%, Haste wins have median duration **58.25s**, versus **59.5s and 59.6s** for healer wins. Mean guardian recovery is **1,708** with Haste and **2,109 / 2,114** with the healers; full-fight totals also reflect duration and survivors. Endless Spring is authored to add Abundance and heal every **10 seconds** for **1% MaxHealth per stack**, making the 60-second event a concrete hypothesis to inspect. Only **6 and 2 healer losses** finish at or below 5% guardian Health; the losing-fight medians are **9.97% and 12.21%**. Those margins do not predict what a Health nerf would do. The archive has no event logs, so detailed exact replays are needed before choosing a mechanism or another scalar.

## Changed files and scope

Updated this report, the continuation handoff, balance status, gear coverage, preceding lower-bracket notice and both harness guides. All driver, audit, review and verification artifacts are under `TestResults/tower-floor7-health-springtide-midpoint-*`. No gameplay catalog changed in this trial. Expected progression, Essence budgets, the search algorithm and other floors remain unchanged. No migrations, configuration, database or deployment changes.

Commands used bundled Python with `-B -X utf8`: prepare; tests; runtime-verify; driver; collect; summarize; gap; review; publish. Publication checks local Markdown links and `git diff --check`.

## Evidence

- screen-1: `TestResults/tower-balance-pass-floor7-health-springtide-midpoint-1-screen-study-20260929`, manifest **`375e912a74bca3a32ebe1b3f60e6ea308916dd033b9af0c7f64d023d65750858`**.
- Independent audit: `TestResults/tower-floor7-health-springtide-midpoint-evidence-20260930.json`, SHA **`a86d968ae627a38d9543770e6dd48dfe7dc9a97d99449f12fb4e9fe3adfe9393`**.
- Descriptive summary: `TestResults/tower-floor7-health-springtide-midpoint-summary-20260930.json`, SHA **`3b4ce9bf39f0b0b793d4adfd8a765ac2da2e20c8c69c0ce0b3c088a9f376b1d4`**.
- Saved survival/output comparison: `TestResults/tower-floor7-health-springtide-midpoint-gap-20260930.json`, SHA **`feb918c6ce2de5bbe4d2eace9897e53120ab7882c5b03a9b78c2c0d190e0a036`**.
- Reviewed interpretation: `TestResults/tower-floor7-health-springtide-midpoint-review-20260930.json`, SHA **`6ea827c16b8405f72e9c4ea6234d3e2123107cb930b1a66db4997b3461b5ae39`**.
- Publication: `TestResults/tower-floor7-health-springtide-midpoint-publication-check-20260930.json`.
