# Floor 7: fixed Endless Spring recovery trial — 2026-09-30

**Latest floor-7 recovery refinement (30 September):** The [fixed 0.9% recovery trial](Tower-Floor7-Endless-Spring-Refinement-20260930.md) closed **`NoEligibleRecoveryPressure`** after **15,360 fresh fights / 128 reservations**. All **120 recipes / eight actual compositions** remain. Every ceiling passes (largest adjusted upper **46.82%**), but the strongest limited-equipment compositions win **40/128 and 18/128**; only one meets minimum 25. **No confirmation or gameplay edit.** **132 Python cases, 173 backend cases / four intentional skips, and one native study fixture pass**. Exclusions: **921,724**. Next is the frozen, unallocated **fixed 0.9% precision proposal**: **1,024 fresh seeds per phase**, eight 128-seed batches each, original gates with count limits **137–455**, whole-family acceptance and independent confirmation. Maximum **245,760 fights / 2,048 reservations**. No prior counts transfer. Add a separate eight-batch aggregate/native application contract before allocation. Floor 7 remains unresolved; floor 4 is the latest applied change. No dungeon or acquisition work.

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Continue the [survival/output diagnostic](Tower-Floor7-Survival-Output-Diagnostic-20260930.md). Its 48 historical replays identify coincident guardian healing and Springtide casualties around 60s, but do not establish a counterfactual win or contribute acceptance samples.

## Frozen protocol

Authenticate the preceding publication, SHA `62ef17243ecfd8dd9586ce9aee22e080909ccc19fa0061415549ff40b047f4d3`, and `TestResults/tower-floor7-endless-spring-recovery-proposal-20260930.json`, SHA `42a35b0120f7b56cb7d07f85eaf5f448d88f3a5c12a8b20d65834fd3d4b4d530`. Entry snapshots preserve modified maintained files and the complete **921,468-seed exclusion union**.

Use the original current-live source `TestResults/tower-balance-pass-floor7-mixed-resistance-screen-study-20260929`, manifest `1df97981c1cb0ee295286d63692a3ed1817cd928b644b5002e54666bb3469b31`. Preserve all **120 exact recipes / eight actual compositions**, raw identities, ordered Essences, positions and expected progression: five level-40 characters, five Essences each, T1 Unique/Exceptional/rank-4 gear. Every existing winner and unsuccessful control stays in the family.

One candidate only: **Endless Spring 0.75% MaxHealth per Abundance instead of 1%**, with **Springtide 21.25% target-MaxHealth base + 0.20 source Power per Abundance**, **offense factor 0.60** and **penetration factor 50**, all reconstructed from the original live source. Guardian Health, other scaling, Abundance generation, interval timing, noncritical healing, Heartwood, Tranquil Waters and all other content remain unchanged. The dynamic `{statusScaling}` recovery description is preserved. No rejected candidate becomes the source.

Use the new separate `tower-recovery-pressure-candidate-v1` contract. Keep the existing health-pressure-v1 contract strict; it must continue rejecting a recovery edit. Before study allocation, run candidate, application, family and qualification safeguards plus native interval, stack, MaxHealth, rounding and actual-healing/overhealing tests through `build/run-tests.ps1`. Bind the new test assembly to the exact archived production binaries; no rebuilt combat DLL is used for study execution.

Run one complete **128-seed screen: 15,360 fights**. Keep at least **two actual compositions** qualifying with **at most eight specialized items on two characters**, adjusted lower bound **≥10%**, and every one of the 120 recipes' adjusted upper bounds **≤50%**. Use the original approximate 95% simultaneous Bonferroni-Wilson method: qualifying screen recipes need **25–44 wins / 128**.

Only if the whole screen passes, run one independent **160-seed confirmation: 19,200 fights**, identical family and candidate, with count gates **30–57 / 160**. Never pool phases, replace observations, retry native studies, extend panels, select from partial outcomes or add another coefficient. On either gate failure, close this fixed scope. Maximum new scope: **34,560 fights / 288 reservations**; all reserved seeds remain excluded.

Admit each allocation using **twice measured complete-panel time and bytes per fight**, under **672 projected seconds / 80% of 2 GiB / 20,000 fights**, with **840-second native / 900-second owner** limits. The first measured source is the closed 21.25% midpoint screen. Before allocation, freeze candidate, protocol, scripts, runtime and content bindings. During execution, read owner stdout only; do not open active archive files.

After each complete phase, independently reconstruct every catalog delta, raw outcome, simultaneous bound, actual composition, equipment budget, disjoint seed and native prepared participant. A passing confirmation additionally requires isolated native parity of **19,200 inputs / 120 complete historical replays** before copying the two verified catalogs locally. Then run backend regression and the same parity against local content. No migration, configuration, database action or deployment. The previous floor-2, floor-4, floor-5 and floor-6 improvements remain intact.

## Completed result

**`NoEligibleRecoveryPressure`.** The fixed 0.75% recovery screen completed **15,360 fresh fights / 128 reservations**, preserving **120 recipes / eight actual compositions**. Both leading limited-equipment compositions exceed the minimum, but **three recipes fail the ceiling**: A/one healer **70/128**, B/one healer **51/128**, A/Ability Haste **58/128**, versus maximum **44**. The largest adjusted upper bound is **69.10%**. **No confirmation or gameplay change followed.**

| Phase / Springtide base (recovery fixed at 0.75%) | Best two distinct limited-equipment compositions | Limited compositions meeting minimum | Ceiling failures | Largest adjusted upper |
| --- | --- | ---: | ---: | ---: |
| screen-1 / 21.25% | 70/128 (restorer-specialization); 51/128 (restorer-specialization) | 2 | 3 | 69.10% |

Every phase preserved the 120-recipe / eight-composition family. The two reported leaders are distinct normalized compositions, not reordered representations. All confidence bounds use the complete family. No pooling, retry, sample extension or additional candidate.

### Equipment controls

| Phase / composition | Baseline | One healer | Full Health + Regeneration | Full Resistance + Health | Ability Haste |
| --- | ---: | ---: | ---: | ---: | ---: |
| screen-1 / A | 9/128 | 70/128 | 10/128 | 17/128 | 58/128 |
| screen-1 / B | 6/128 | 51/128 | 5/128 | 8/128 | 22/128 |

A is original composition `dae6cc32…`, B `0d375ed9…`. These are descriptive per-recipe outcomes, not extra samples or proof of an isolated equipment mechanism.

## Verification

Completed **15,360 fresh fights / 128 reservations**, ending at **921,596 exclusions**. Independently reconstructed every candidate catalog delta, recounted all raw outcomes, verified native preparations in every fight, and reproduced equipment qualification, adjusted bounds, selection, resource admission and the disjoint seed union. All study processes closed without timeout, retry or remaining owned child.

**115 fresh Python safeguard tests and 165 backend tests pass**, with **four intentional opt-in skips**, plus **one native study fixture**. Fifteen new Python cases cover the strict compound delta and rejection paths; the old health-pressure contract still rejects the extra healing edit. Eight new native cases verify 0.75% healing, MaxHealth and stack scaling, the 60-stack cap, interval boundaries, rounding at 9,740 MaxHealth, noncritical healing and actual recovery versus overhealing.

The first local build was blocked while reading the user NuGet configuration. Its failed owner/log are preserved. A separately recorded build with the required access passed; its test assembly was then bound to the exact archived production DLLs and the full 165-case regression passed again through `build/run-tests.ps1 -NoBuild`. These are **165 distinct backend cases**, not 330 different tests. No combat engine binary or source was changed. The study itself had zero retries. Native fight time was **175.291 seconds**, archive size **211,052,192 bytes**, within the declared admission limits.

No verification command remains blocked. Native commands, filters, TRX files and process receipts are preserved under the fresh verification and study owners.

## Next work

The healing reduction is large enough to make the limited-equipment routes viable, but this setting makes the encounter too easy for retained controls, including both healer setups. At unchanged Springtide damage, their counts were **21/128 and 15/128** at 1% recovery; A/haste was **34/128**. Different fresh panels mean these differences include sampling variation; they do not establish a smooth response curve or isolate an exact causal effect size. A/healer is already above 50% in observed wins, so increasing precision at this fixed setting is not the next step.

Recommend one separate **0.9% MaxHealth per-Abundance** trial: a 10% reduction from the original 1%, smaller than the rejected 25% reduction. Keep Springtide **21.25%**, bonus **0.20**, offense **0.60**, penetration **50** and every original recipe. This is a conservative intermediate proposal, not an inferred winning setting. The existing 0.75% contract remains strict; implement a separate refinement version and native cases before allocating any study. No further detailed-replay diagnostic is required to justify this next test.

Frozen proposal: `TestResults/tower-floor7-endless-spring-refinement-proposal-20260930.json`. One 128-seed screen and at most one independent 160-seed confirmation; unchanged **25–44/128** and **30–57/160** gates, at least two actual limited-equipment compositions and every family ceiling. Maximum **34,560 fights / 288 reservations**, with measured resource admission before each allocation. No extra candidate, extension, retry or pooling. **Proposed only: no refined candidate materialized, new panel allocated or seed reserved.** Floor 7 remains unresolved; floor 4 is still the latest applied Tower change. After floor 7, review equipment dependence on floors 8, 9, 10 and 12–15; keep the search and progression budgets fixed.

## Changed files and scope

Added the strict recovery candidate helper and fifteen rejection tests; extended the owner CLI/audit; added eight native healing cases. Updated this report, the continuation handoff, balance status, gear coverage, preceding diagnostic notice and both harness guides. All driver, audit, review and verification artifacts are under `TestResults/tower-floor7-endless-spring-recovery-*`. No gameplay catalog changed in this trial. Expected progression, Essence budgets, the search algorithm and other floors remain unchanged. No migrations, configuration, database or deployment changes.

Commands used bundled Python with `-B -X utf8`: prepare; five Python suites; backend build and archived-runtime binding; driver; independent collector; summary/review; publication. Publication checks local Markdown links and `git diff --check`.

## Evidence

- screen-1: `TestResults/tower-balance-pass-floor7-endless-spring-recovery-1-screen-study-20260929`, manifest **`c327a73ba73d8d6e67b4d118ee205d45d786c3f276d4ecd229e37b7535fcbb06`**.
- Independent audit: `TestResults/tower-floor7-endless-spring-recovery-evidence-20260930.json`, SHA **`56fdfbc339e45c6b212306c2bf91fb8297742602aca90e7e7f478f537948aca6`**.
- Descriptive summary: `TestResults/tower-floor7-endless-spring-recovery-summary-20260930.json`, SHA **`29154c0f7643d32bb72526fe59f9e5306aa11a84ef22a137dda391e022612409`**.
- Reviewed interpretation: `TestResults/tower-floor7-endless-spring-recovery-review-20260930.json`, SHA **`95fb08fa95a4370f547c676db4c1de141b68ad08b6d0916108fa5975a2689757`**.
- Publication: `TestResults/tower-floor7-endless-spring-recovery-publication-check-20260930.json`.
