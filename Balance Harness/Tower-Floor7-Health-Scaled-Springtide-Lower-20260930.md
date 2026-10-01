# Floor 7: lower target-Health Springtide bracket — 2026-09-30

**Latest floor-7 fixed midpoint trial (30 September):** The [fixed midpoint Springtide trial](Tower-Floor7-Health-Scaled-Springtide-Midpoint-20260930.md) closed **`NoEligibleHealthSpringtide`** after **15,360 fresh fights /128 reservations**, preserving **120 recipes /eight compositions**. At **21.25%**, every ceiling passes (largest adjusted upper **41.96%**), but the strongest limited-equipment compositions win **21/128 and 15/128**, below 25. **No confirmation or gameplay edit.** All outcomes and native preparations were independently audited; **100 Python checks, 157 fresh backend tests /four intentional skips, and one native study fixture pass**. A descriptive recount of **768 existing reports** identifies a survival/output and recovery timing question around 60 seconds; it is not causal evidence or added acceptance data. Exclusions: **921,468**. Next: **48 exact historical replays**, proposed and unallocated, comparing A/Ability Haste with both one-healer compositions around casualties and Endless Spring healing. No further gameplay candidate selected. Floor 7 remains unresolved; floor 4 remains the latest applied change. No dungeon or acquisition work.

## Frozen protocol

The preceding [casualty diagnostic](Tower-Floor7-Health-Casualty-Diagnostic-20260930.md) found that the rejected 25% target-Health base raised first-cast damage beyond the original Power formula's observed variance envelope. Target-Health scaling bypasses source Weaken for its base and disables the native Power-only magnitude variance for the whole hit. The additive Abundance term still uses source Power. This trial reduces the Health fraction; it does not change the engine or claim that arithmetic predicts win rates.

Authenticate `TestResults/tower-floor7-health-casualty-publication-check-20260930.json` (`a9178919e98479b6f2428ca31e2f3b38936c9782781de16c8937d8892a62c70f`) and `TestResults/tower-floor7-health-springtide-lower-proposal-20260930.json` (`a4f11d139859df1f3ce040ae5a536e96c896286da574c7c72e97744c6b94842f`) before allocation. The initial seed-exclusion union contains **920,956** values.

Use all **120 exact recipes / eight actual compositions** from `tower-balance-pass-floor7-mixed-resistance-screen-study-20260929`, manifest `1df97981c1cb0ee295286d63692a3ed1817cd928b644b5002e54666bb3469b31`. Preserve party positions, ordered Essences, raw identities, controls, levels and gear budgets. These are level-40, five-Essence parties with expected tier-1 Unique / Exceptional / rank-4 equipment.

Build each candidate from that original current-live source:

| Candidate | Springtide target MaxHealth | Guardian offense factor | Penetration factor |
| --- | ---: | ---: | ---: |
| 1 | 17.5% | 0.60 | 50 |
| 2 | 20% | 0.60 | 50 |
| 3 | 22.5% | 0.60 | 50 |

Preserve **0.20 source Power per Abundance stack**. Only Springtide's base scaling and description, and floor 7 guardian offense/penetration, may differ. Guardian prepared Power must be 747.93256; both stored penetration attributes approximately 43.2, capped at 40 by existing rules. Health and all other preparation fields remain unchanged. Never compound factors into the rejected 25% archive.

Complete all three independent **128-seed / 15,360-fight** screens before selection. A candidate qualifies only when at least **two actual compositions**, each using at most **eight specialized items on two characters**, reach an adjusted lower win bound of 10%, and every one of the 120 recipes has an adjusted upper bound at most 50%. Use **95% Bonferroni-Wilson across 120 recipes**: screen qualifying wins 25–44/128. Rank eligible candidates by the second-best limited-composition lower bound descending, maximum upper bound ascending, then Health fraction ascending.

Confirm at most one selected candidate with independent **160 seeds / 19,200 fights**; gates 30–57/160. Maximum allocation is **65,280 fights / 544 reservations**. No fourth candidate, extension, retry or pooling. Before every allocation, project time and archive bytes at twice a completed panel's measured cost per fight; require fewer than 672 seconds, less than 80% of 2 GiB, and at most 20,000 fights. Native deadline 840 seconds; owner deadline 900 seconds.

Run candidate/application/family Python safeguards and native regression guards for all lower fractions, source-Power Abundance, Weaken, mitigation, barriers and the Power-only magnitude variance distinction. Use `build/run-tests.ps1`, then verify the fresh test assembly against the unchanged archived production binaries. Independently recount every raw outcome and native preparation, verify exact content deltas, confidence bounds, equipment qualification, selection and disjoint seeds. Independent confirmation and isolated native parity must precede scoped local application, backend regression and live parity. Do not deploy.

## Completed result

**No candidate qualified; no confirmation or gameplay edit.** The 17.5% candidate exceeds the ceiling in 63 recipes. At 20%, two limited-equipment compositions meet the lower gate, but three recipes exceed the upper gate. At 22.5%, every ceiling passes but the best two limited compositions win only 16/128 and 9/128, below the 25-win minimum. This closes the declared three-candidate bracket.

| Phase / Health base | Best two distinct limited-equipment compositions | Qualifying limited compositions | Ceiling failures | Largest adjusted upper |
| --- | --- | ---: | ---: | ---: |
| screen-1 / 17.5% | 104/128 (mixed-resistance-baseline-slots-1); 99/128 (mixed-resistance-baseline-slots-1) | 5 | 63 | 95.97% |
| screen-2 / 20% | 54/128 (mixed-resistance-baseline-slots-5); 37/128 (restorer-specialization) | 2 | 3 | 71.16% |
| screen-3 / 22.5% | 16/128 (restorer-specialization); 9/128 (restorer-specialization) | 0 | 0 | 26.22% |

All screens preserved the 120-recipe / eight-composition family. The two reported leaders are distinct normalized compositions, not reordered representations. All confidence bounds use the complete family. No pooling, retry, sample extension or fourth candidate.

### Equipment controls

| Phase / composition | Baseline | One healer | Full Health + Regeneration | Full Resistance + Health |
| --- | ---: | ---: | ---: | ---: |
| screen-1 / A | 102/128 | 104/128 | 41/128 | 34/128 |
| screen-1 / B | 90/128 | 75/128 | 23/128 | 18/128 |
| screen-2 / A | 41/128 | 36/128 | 9/128 | 10/128 |
| screen-2 / B | 20/128 | 37/128 | 2/128 | 5/128 |
| screen-3 / A | 0/128 | 16/128 | 0/128 | 0/128 |
| screen-3 / B | 0/128 | 9/128 | 0/128 | 1/128 |

A is original composition `dae6cc32…`, B `0d375ed9…`. These are descriptive per-recipe outcomes, not extra samples or proof of an isolated equipment mechanism.

## Verification

Completed **46,080 fresh fights / 384 reservations**, ending at **921,340 exclusions**. Independently reconstructed every candidate catalog delta, recounted all raw outcomes, verified native preparations in every fight, and reproduced equipment qualification, adjusted bounds, selection, resource admission and the disjoint seed union. All study processes closed without timeout, retry or remaining owned child.

**100 Python safeguards and 157 backend tests passed; four intentional opt-in fixtures were skipped.** The backend suite ran through `build/run-tests.ps1`. A fresh test assembly was built with build-server reuse disabled, then all 157 checks passed again with the exact archived production assemblies. Nineteen Springtide cases now include six lower-fraction cases and three Weaken/variance cases added this turn. The latter each exercise 16 fixed seeds under both scaling formulas, verifying that Weaken reduces the Abundance bonus but not the target-Health base and that only the Power-base formula varies. These deterministic unit-test seeds are separate from study seed reservations. No production engine change was needed.

No verification command remains blocked. The build was permitted to read the user NuGet configuration; no NuGet settings were changed. Native commands, filters, TRX files and process receipts are preserved under the fresh runtime and study owners.

## Next work

**Next: one separate, fixed 21.25% midpoint study**, proposed but not materialized or allocated, from `TestResults/tower-floor7-health-springtide-midpoint-proposal-20260930.json`. Preserve the original 0.20 source-Power Abundance bonus, offense 0.60, penetration 50, all 120 recipes / eight compositions and every gate. One complete 128-seed screen; only a pass permits an independent 160-seed confirmation. Maximum 34,560 new fights / 288 reservations, with resource admission before allocation. No additional percentage, extension, retry or pooling.

At 20%, the one-healer controls already meet both per-recipe bounds at **36/128 and 37/128**, while the only ceiling failures are composition A’s **Ability Haste 73/128**, **four resistance items on slot 1 at 51/128**, and **four resistance items on slot 5 at 54/128**. Full Health + Regeneration is only **9/128 and 2/128**. The target-Health mechanism therefore changes which recipes dominate; it has not established a balanced floor. A midpoint is the smallest remaining scalar experiment supported by this bracket. Its success is uncertain, and no interpolation is treated as a win-rate prediction. If it fails, close it and inspect the remaining gap before deciding on another coefficient or mechanism.

## Changed files and scope

Added nine cases in `LL/tests/EssenceSystem.Tests/AbilitySystemTests.cs`. Updated this report, the continuation handoff, balance status, gear coverage, preceding diagnostic notice and both harness guides. All driver, audit, review and verification artifacts are under `TestResults/tower-floor7-health-springtide-lower-*`. No gameplay catalog changed in this trial. Expected progression, Essence budgets, the search algorithm and other floors remain unchanged. No migrations, configuration, database or deployment changes.

Commands used bundled Python with `-B -X utf8`: prepare; tests; runtime-build; runtime-bind; driver; collect; summarize; review; publish. Publication checks local Markdown links and `git diff --check`.

## Evidence

- screen-1: `TestResults/tower-balance-pass-floor7-health-springtide-lower-1-screen-study-20260929`, manifest **`670bbb1f489fd539f17d217b5fdc3d6ab3e02cc1c4be6fe710e2887561dafe12`**.
- screen-2: `TestResults/tower-balance-pass-floor7-health-springtide-lower-2-screen-study-20260929`, manifest **`197c3e59040427fc850a79a51c11ad56adec2d5e9595ec2e7483333c2505a2a1`**.
- screen-3: `TestResults/tower-balance-pass-floor7-health-springtide-lower-3-screen-study-20260929`, manifest **`18de80f4114de9b8984580e8d5c21825a7a23ff74de13e24c81690293a84bdba`**.
- Independent audit: `TestResults/tower-floor7-health-springtide-lower-evidence-20260930.json`, SHA **`f18fc8b822f8f075a96951a42c25ab578a7d1fa09154046cde2fa86c6fb923d7`**.
- Descriptive summary: `TestResults/tower-floor7-health-springtide-lower-summary-20260930.json`, SHA **`0dfb76ff43e4049cc3e580f2bbca3cbd59e3362dd656f1c27c67e409bad6c22f`**.
- Reviewed interpretation: `TestResults/tower-floor7-health-springtide-lower-review-20260930.json`, SHA **`dc3604e8e6a1a068fb60fcf4f8691110d95576813979f5ae7d3076b557a25673`**.
- Publication: `TestResults/tower-floor7-health-springtide-lower-publication-check-20260930.json`.
