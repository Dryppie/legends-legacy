# Floor 7: limited resistance equipment — 30 September 2026

**Latest floor-7 diagnostic (30 September):** The [floor-7 pressure diagnostic](Tower-Floor7-Pressure-Diagnostic-20260930.md) completed **96 exact historical replays**, auditing **144,728 events**, with **zero new seeds or acceptance fights**. Springtide supplies **84.8–85.8%** of baseline party health damage before 40 seconds. Baseline and partial-resistance first deaths have median **36s**, versus **48s** with full resistance. Partial gear leaves slot **2** in composition A and slot **4** in B among the first casualties in **16/16** replays each. Baseline opening damage is sufficient to remove about 7,150 guardian Health by 40s; survival loss then stalls net progress. Resistance gear also changes light-character Health **2,359 → 2,833**, Tenacity **116.92 → 0**, and regeneration **99.44 → 70.21**. All full-resistance Slow/Weaken applications in the early window land, so this is not a one-stat comparison. **41 fresh Python safeguards pass**; unchanged **138 backend passes / four skips** were authenticated and reused. No game content changed; exclusions remain **919,804**. Next is one isolated Springtide ramp candidate: base Power **1.0 → 0.25**, per-Abundance Power **0.20 → 0.35**, preserving all other settings and all **120 recipes**. It is **proposed, not implemented, tested or allocated**. Add a separate candidate contract and rejection tests; retain the existing proportional-scaling guard. Require the original limited-equipment minimum and every full-family ceiling, followed by independent confirmation. No dungeon, supply or acquisition work.

Target: primary LL World Tower and the offline Balance Harness. Review floor 7's equipment dependence, qualify its entire accepted historical family against the applied floor-4 catalogs and current runtime, then measure partial equipment at unchanged Eydis settings. Keep the supported search, expected progression, and all previously accepted Tower changes.

## Evidence motivating this scope

The saved confirmation contains **60 exact recipes / eight actual compositions / seven equipment profiles**. A fresh authenticated recount covers all **15,360 saved fights**. Only resistance-and-health qualifies: the two leading compositions win **95/256** and **82/256** under the original 60-recipe simultaneous bounds. Every alternative gear profile records zero wins. Health-and-regeneration is closest by remaining guardian health, at **21.76%** for its best recipe; that descriptive ranking is not acceptance evidence.

The resistance profile substitutes Chest, Head, Legs and Necklace on all five characters: **20 specialized items**. Six matched saved recipes, comprising **1,536 of those already counted fights**, preserve raw identities, positions and Essence order. Median first deaths are **48.6 / 48.0 seconds** for resistance, **36.0 / 36.0** for baseline, and **48.0 / 48.0** for health-and-regeneration. Slots 2–4 have baseline median death times of 36 seconds. Whole-fight mitigation totals depend on survival time, and these reports contain no event timelines. This evidence supports testing equipment subsets, not yet attributing failure to a particular ability.

## Prospective protocol

- Preserve all 60 original recipes, including every winner, reference, exact search nominee and gear control. Add every nonempty, non-full subset of the five characters receiving their exact saved resistance/health equipment for each of the two strongest distinct qualifying compositions. This adds **60 variants**, yielding **120 recipes / eight actual compositions**. All one-, two-, three- and four-character subsets are measured. Count actual per-slot Essence sets for diversity without rewriting their order.
- Preserve five level-40 characters, five Essences each, tier-1 Unique / Exceptional / rank-4 equipment, fixed rolls, no styles, ascension or evolution. Ownership remains hypothetical. Preserve Eydis health **4.4454238281**, offense **6.5953125**, regeneration **0.1**, every ability and all other floors.
- Before new combat, authenticate the **complete applied floor-4 aggregate**: all four screening and four confirmation batches, both passing aggregate assessments, four application parity receipts and the two-catalog application completion. Bind current abilities SHA `3adebf8c1e82ec8c1ffead6d3e5790bffef98aec8a4fc3a69da95db2b5715635` and Tower SHA `62ca6e55b1084f0dcfced9b6f70a58a114b9b484b96dc59b0fc6a3c8e05b6f72`. Match all **15,360** original floor-7 combat inputs and **60 full saved replays** on the current runtime. Historical archives remain immutable.
- Admit the exact proposed family only through the explicit qualification receipt and tested floor-7 resistance validator. Prepare current-content recipes with no seeds or fights. Never shrink the original family to 38 recipes or substitute a single passing floor-4 batch for its aggregate.
- Run one **128-seed full-family screen: 15,360 fights**. Require two distinct actual compositions with at most **eight specialized items on two characters** to have an adjusted lower bound at least 10%, and **every** recipe's upper bound at most 50%. With 120-recipe approximate simultaneous 95% Bonferroni-Wilson bounds, screening requires at least **25/128** wins for each qualifying composition and at most **44/128** for every recipe.
- Only if the whole screen passes, admit one independent **160-seed full-family confirmation: 19,200 fights**. The corresponding gates are **30/160** minimum and **57/160** maximum. No pooling of screening, historical, or confirmation outcomes. If confirmed, match all 19,200 inputs and 120 complete reports against the unchanged current catalogs.
- Before each panel, use twice the measured time and archive size per fight to project costs. Require at most **20,000 fights**, projected time below **672 seconds** and size below **80% of 2 GiB** before allocating seeds. Native cap **840 seconds**, owner cap **900 seconds**. Stop on technical failure, failed admission, failed screen or failed confirmation. No retries, sample extension, alternate candidates or omitted controls.

Maximum new study allocation: **34,560 fights / 288 reservations**. Qualification uses 60 historical replays; successful confirmation permits another 120 historical replays, each using already reserved seeds. Initial exclusions: **919,676**. No search, gameplay edit, acquisition work, dungeon work, supply change, migration, configuration change or deployment is included.

The driver copies this prospective protocol before starting any new panel. While a native process runs, monitor supervisor output only; never read its active trial or log files.

## Completed result

**The limited-equipment target did not pass. No confirmation or gameplay change followed.** All 120 declared recipes were measured on one fresh 128-seed panel. The table gives the strongest measured subset at each equipment count for each original winning composition; these are descriptive comparisons, not separate accepted subfamilies.

| Characters with resistance gear | Specialized items | Composition `dae6cc32…` | Composition `0d375ed9…` |
| ---: | ---: | ---: | ---: |
| 0 | 0 | 0/128 | 0/128 |
| 1 | 4 | 0/128 | 0/128 |
| 2 | 8 | 1/128 | 1/128 |
| 3 | 12 | 9/128 | 11/128 |
| 4 | 16 | 27/128 | 20/128 |
| 5 | 20 | 48/128 | 25/128 |

Both best two-character variants won **1/128**: slots **3+4** for `dae6cc32…`, and **2+3** for `0d375ed9…`. Each has adjusted bounds **0.05%–10.24%**, far below the required 10% lower bound. Slots **2+3+4** were strongest at three characters; slots **2+3+4+5** at four. Four-character gear qualified one composition at **27/128**, but the second reached **20/128**, below 25. This does not establish the two-composition minimum at reduced equipment.

The full-resistance controls won **48/128** and **25/128**. The first exceeds the predeclared ceiling of 44; its adjusted upper bound is **53.07%**. There are **zero qualifying limited-equipment compositions and one ceiling failure**. The former 60-recipe confirmation remains historical evidence with its original bounds; this larger, separately seeded panel is not pooled with it. No global difficulty reduction is justified by a screen that also contains a strong full-gear control.

## Qualification, verification and accounting

The complete applied floor-4 aggregate was authenticated, including all eight original study archives, both independent 512-seed assessments, four parity owners and the final two-catalog application receipt. The entire original floor-7 family qualified: **15,360 inputs and 60 full historical replays matched**, with no seed allocation. All four Eydis ability definitions and its floor definition remain unchanged. Qualification ran through `build/run-tests.ps1`; **29 cases passed**, comprising 28 application guards and the owned qualification fixture.

The explicit floor-7 admission path retained all **60 originals** and reconstructed all **60 variants** from exact saved equipment. Its maintained tests reject removed controls, missing/duplicated subsets, wrong parents, wrong floors, changed identities/budgets/Essence order, and substitution of armor gear for resistance. The existing floor-2/floor-4 armor and floor-6 Restoration contracts remain covered. Seed-free preparation and the fresh screen each passed their owned native fixture. **117 distinct fresh Python tests pass**: 50 qualification, 29 reference coverage, 32 aggregate and six review checks. The existing **138 backend passes / four intentional skips** were authenticated and reused because C#, game content and compiled runtime did not change.

An independent collector rebuilt every proposed recipe, authenticated every screen archive member, recounted all **15,360 new outcomes**, recomputed all 120 simultaneous bounds and actual equipment counts, and checked resources, complete seed panels, historical disjointness and unchanged catalog/runtime pins. The screen completed in **183.745 native seconds**, **186.875 owned-process seconds**, and **212,106,932 archive bytes**, within its caps. There were **zero retries, timeouts or remaining child processes**. New study fights: **15,360**; new reservations: **128**; exclusion union: **919,676 → 919,804**. The 60 qualification replays are separate historical replays, not new study observations.

No verification command remains blocked. Commands used bundled Python with `-B -X utf8`:

```text
TestResults/tower-floor7-equipment-prepare-20260930.py
Balance Harness/analysis/test-tower-catalog-qualification.py
Balance Harness/analysis/test-tower-reference-coverage.py
Balance Harness/analysis/test-tower-balance-status.py
Balance Harness/analysis/test-tower-balance-aggregate.py
TestResults/tower-floor7-equipment-qualify-20260930.py
TestResults/tower-floor7-mixed-resistance-driver-20260930.py
TestResults/tower-floor7-mixed-resistance-collect-20260930.py
```

Qualification and native study commands are retained as structured JSON beneath their fresh `TestResults` owners and invoke the repository's required `build/run-tests.ps1` entry point with the verified runtime. The publisher additionally checks Markdown links and `git diff --check`.

## Next bounded work

Use the proposed **96 historical event replays**: baseline, the best two-character resistance subset, and full resistance for each original winning composition, on the **first sixteen saved screen seeds in declared order**. Strip the added event log and require equality to the complete saved report. Reconcile damage, healing, regeneration and first deaths to saved recipient totals. Compare fixed first-40-second windows, then full timelines, including Springtide/Abundance, Slow/Weaken, and guardian healing. This is explanatory evidence, never acceptance data. The existing diagnostic helpers have older family-specific guards; extend and test an explicit floor-7 path before invoking them.

**No new diagnostic replay, seed or balance candidate is allocated yet.** Keep the full 120-recipe family and the strongest full-resistance controls for any later isolated candidate. Do not rerun or extend this failed screen. Choose a mechanic change only after the event comparison establishes the relevant failure mechanism; a broad scalar reduction could also strengthen the already strong full-resistance build.

Changed maintained files: `tower-catalog-qualification.py`, its tests, the owner CLI's supported-floor guard, this report, handoff, status, gear-coverage notice and the two harness guides. There is **no game-data, search-policy, migration, application-configuration, database, deployment, dungeon or supply change**. Gear ownership and broad archetype coverage remain unestablished.

## Evidence

- Independent result: `TestResults/tower-floor7-mixed-resistance-evidence-20260930.json`, SHA **`8157a8224fc8bcb7dace5b7f4b8e5f9c4632f0acf93a33edee8bd6628d2843ca`**.
- Complete screen: `TestResults/tower-balance-pass-floor7-mixed-resistance-screen-study-20260929`, manifest **`1df97981c1cb0ee295286d63692a3ed1817cd928b644b5002e54666bb3469b31`**, audit **`a7019414e1d230878e8cec6bbfd874c4156c4ec0c39dffc28a08281fa7cb9d37`**.
- Frozen family proposal: `TestResults/tower-floor7-mixed-resistance-proposed-family-20260930.json`, SHA **`76dac86606fcad5690f2cb08abcb189c9af9e21c4d85de52de172b92ff51751e`**. Its original `ProposedNotAdmitted` status remains immutable; admission is separately recorded.
- Qualification: `TestResults/tower-floor7-equipment-qualification-owner-20260930` and `tower-floor7-equipment-qualification-plan-20260930.json`.
- Prospective protocol, commands, resource admission, test results, decision and completion: `TestResults/tower-floor7-mixed-resistance-driver-20260930`.
- Seed-free diagnostic proposal: `TestResults/tower-floor7-pressure-diagnostic-proposal-20260930.json`, SHA **`12eb8d799f0de523d9e371ff2dbbbdbf148f3214473a1b7b190bb8f074bdc989`**.
- Publication: `TestResults/tower-floor7-mixed-resistance-publication-check-20260930.json`.
