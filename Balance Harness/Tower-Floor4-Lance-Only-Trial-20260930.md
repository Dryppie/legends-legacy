# Floor 4: Mirror Lance-only trial — 30 September 2026

**Subsequent result — supported search closed:** The [floor-4 supported search with a healer reference](Tower-Floor4-Two-Armor-Support-Search-20260930.md) closed **`NoEligibleTwoArmorConfirmation`**. It found **two new actual Essence combinations**, retaining **116 exact recipes / seven compositions**. The new teams won **9/128 and 10/128** with armor on slots **2 and 4**, below **25/128**; their full-armor versions won **56/128 and 40/128**, with the first exceeding the **44/128** ceiling (adjusted upper **59.05%**). Neither nominee retained direct ally healing; the healer reference won **0/128**. Both gates failed: **no confirmation, application or floor-4 gameplay edit**. **22,032 fresh fights / 365 reservations**, four passing native fixtures and **19 fresh Python safeguards**; authenticated **108 backend passes / four opt-in skips** reused. All original controls, progression budgets and the supported search policy remain intact. Exclusions **917,574**. The supported-search proposal below has now been executed and closed. Follow the linked report and current handoff for the unallocated fixed-support diagnostic; do not rerun the old proposal.

## Frozen prospective protocol

Target: primary LL World Tower and offline Balance Harness. The [rejected-candidate diagnostic](Tower-Floor4-Lance-Shards-Diagnostic-20260930.md) verified 64 exact replays. In its 16 partial-armor samples, Hall magical caused the first death at 16 seconds in nine and Hall physical in two. Test the already proposed Mirror Lance reduction alone, without increasing Hall damage. This isolates the declared ability change; it is not established gameplay balance.

Mirror Lance magical coefficient **1.00 → 0.60**, proportional Mirrorbound coefficient **0.010 → 0.006**, and matching description. Keep Lance physical **2.00**, Hall magical/physical **0.50/0.50**, all timings, target selection, status behavior, reflection and guardian scalars unchanged. Vaelor health/offense remains **2.3231953125 / 5.023125**.

- Frozen proposal: `TestResults/tower-floor4-lance-only-proposal-20260930.json`, SHA `70f146d550a1a5a4335c134ce0bf15017a98b1aea088e5557cb9e939b1195d1e`; preserve this pre-allocation record.
- Start from the unchanged live-catalog source `TestResults/tower-balance-pass-floor4-mixed-armor-screen-study-20260929`, manifest `66cc379b5f226c6925e7b1a240c0f65c8bef6d630bcafde7978ca036140d406d`. Keep **98 exact loadouts**: all 38 controls and 60 armor variants, five actual compositions. No reordered characters/Essences, search, imports, dropped recipes or budget changes.
- Authenticate the latest publication, all 102 live JSON files, six runtime/test assemblies, original source and reused test receipts before allocation. Initial exclusions **917,081**, plus all historical reservations. Fresh driver/collector/output paths are required.
- Screen **128 fresh shared seeds / 12,544 fights**. Require at least two actual compositions with armor on at most two characters/eight specialized items, each lower win-rate bound at least 10%, and every loadout upper bound at most 50%. Approximate simultaneous 95% Bonferroni-Wilson across all 98 loadouts; gates **25–44 wins / 128**.
- Only passing complete screening permits an independent **184-seed / 18,032-fight confirmation** of the same candidate/family, gates **33–68 wins / 184**. No pooling with screening or historical panels. Passing confirmation still requires native candidate parity before local application, followed by regressions.
- Maximum **30,576 fresh fights / 312 reservations**, one candidate, no extensions, retries, replacement seeds or post-result retuning. Failed screening closes this scope without confirmation or application. If partial armor still fails, assess a bounded search for early protection/healing compositions with every existing control retained; do not resume Hall compensation sweeps.
- Per native phase: **20,000 fights / 840 seconds / 2 GiB**, owner **900 seconds**. Require doubled measured preceding time/storage projections below 80% of the limits before each phase.

Preserve five level-30 characters, four level-1 unevolved/unascended Essences each, T1 Epic/Fine/Rank-3 gear, roll 1 and no styles; the approved repeating progression curve; prior floor-2/5/6 changes and supported search. No dungeon, supply, migration, configuration or deployment work.

Fresh paths: `TestResults/tower-floor4-lance-only-driver-20260930` and native `tower-balance-pass-floor4-lance-only-{screen,confirm}-{study,owner}-20260929`. The unchanged 30 candidate Python tests, four Vaelor tests and 108 backend passes/four opt-in skips are authenticated and reused, not rerun. Native fixtures execute through `build/run-tests.ps1 -NoBuild`. The driver and independent collector validate exact edits, all archived fights, complete-family decisions, seed accounting and resource limits.

## Verified result

**Rejected: `NoEligibleLanceOnlyConfirmation`.** Both acceptance gates failed. Neither actual composition reached the two-character armor minimum, and the full-armor controls exceeded the whole-family ceiling. The independent collector verified all **98 exact loadouts / five actual compositions**, every archived outcome and every adjusted interval. No confirmation or local application ran.

| Composition | Armored characters | Slots | Wins | Adjusted interval |
| --- | ---: | --- | ---: | ---: |
| A | 2 | 2, 3 | 10/128 | 2.77%–20.13% |
| A | 3 | 1, 3, 4 | 16/128 | 5.50%–25.97% |
| A | 4 | 1, 2, 3, 4 | 40/128 | 19.16%–46.57% |
| A | 5 | all | 64/128 | 35.32%–64.68% |
| B | 2 | 1, 3 | 11/128 | 3.19%–21.13% |
| B | 3 | 2, 3, 4 | 25/128 | 10.22%–34.09% |
| B | 4 | 1, 2, 3, 4 | 45/128 | 22.36%–50.51% |
| B | 5 | all | 82/128 | 48.71%–76.99% |

The best two-character variants won **10/128 and 11/128**, versus the required **25/128**. B reaches that minimum with **three** armored characters, but that uses twelve specialized items and does not meet the eight-item target. Full armor won **64/128 and 82/128**, versus the ceiling of **44/128**. The strongest observed win rate is **64.06%**, with adjusted upper **76.99%**. This is a material failure of the full-armor control, not a marginal uncertainty miss.

Reducing Lance alone failed to establish viable partial armor while keeping full armor within the target range. The preceding joint trial also failed, with extra Hall magic suppressing partial-armor survival. These screens used different seed panels, so differences between their observed counts are descriptive, not paired causal estimates. The results justify closing this coefficient line and checking the party composition assumption; they do not prove no other guardian setting could work.

Fresh work: **12,544 fights / 128 reservations**, **129.25 native seconds**, no retries and zero active owner children at closure. Exclusions **917,081 → 917,209**. Live Lance physical/magical remains **2.00/1.00**; Hall magical/physical remains **0.50/0.50**. Vaelor health/offense remains **2.3231953125 / 5.023125**. Five characters, level 30, four level-1 unevolved/unascended Essences each, T1 Epic/Fine/Rank-3 gear, roll 1 and no styles remain the budget. Prior accepted floor changes and the repeating gear curve are preserved.

## Composition-search assessment

The two leading damage-focused parents contain **no direct ally-targeted Heal effect** among their equipped active or passive abilities. Their recovery is principally self-targeted; the saved diagnostic recorded very little direct healing before the lethal opening. An existing retained reference already includes a healer on slot 2, using **Blue Slime, Forest Spirit, Goblin Shaman and Lumo Wisp**, plus a defensive first-slot build. This reference's full-armor variant won **0/128** at unchanged guardian values, so its availability is not evidence that it is viable. It provides an existing support composition for the supported search to combine with the damage references.

The catalog assessment applies the active **healing-v1** overrides before checking abilities. The next proposal copies only equipment onto `reference-1/baseline`; it adds no Essence, raises no budget and changes no identity or Essence order. All **98 originals** remain, giving **99 prepared recipes** and exactly three actual compositions at the selected partial-armor profile. Native admission and preparation tests must verify the exact projection before allocation.

Select armor slots **2 and 4** using only the unchanged-catalog screen: maximize the weaker parent's wins, then total wins, break ties by lower summed guardian health and slots. That profile has **5/128 and 4/128** in the original live-catalog source. This descriptive choice uses neither rejected candidate as search content. It also places the existing healer on an armored slot, without assuming its first heal occurs before Hall; actual cast timing depends on the prepared combat input.

Run one bounded **supported composition search at unchanged live guardian values**, with armor on slots **2 and 4**. Add only an equipment projection of the existing healer/tank `reference-1/baseline`, keeping its four ally-healing Essences on slot 2 and all other raw fields. Retain the two damage-focused parents and every original control. Proposal `TestResults/tower-floor4-two-armor-support-search-proposal-20260930.json` is **ProposedNotAllocated**. Prepare **99 recipes**, measure them on 64 fresh seeds, then run the unchanged **528-fight** `affinity-creation-with-benchmark-validation-v1` search once and evaluate all five references/nominees on 64 held-out seeds. Retain both exact nominees and every gear projection; the existing helper now sees **eight gear templates**, because the healer reference is the first composition. Freeze the expanded family, at most **120 recipes**, before a 128-seed screen and conditional independent 166-seed confirmation. Maximum **42,464 fights / 531 reservations**, subject to fresh resource admission. Keep the two-composition/eight-item/50% ceiling gates and native parity before acceptance. This supplies a healer reference; the search has no early-healing objective and may still nominate damage-focused teams. No active study remains; no more Hall compensation sweeps.

The 120-recipe upper bound conservatively includes all 99 prepared recipes, both exact nominees, each nominee on all eight templates, and all three exact search references before exact-scenario deduplication. This differs from the older floor-2 search's seven-template assumption; do not copy its 118-recipe bound. The largest confirmation is **120 × 166 = 19,920 fights**, under the native 20,000-fight cap. Recompute simultaneous bounds from the actual frozen family size. Every original and newly found full-armor control remains; nominal support roles are not a substitute for measured viability.

The search is limited to one use of the existing algorithm, with no policy, objective, mutation or benchmark-gate change. It may not keep the healer in a nominee and does not establish early support synergy by construction. Preserve all exact references and nominees regardless of strength. If the search or expanded screen fails, close its declared scope; do not repeat it or weaken the acceptance gates. Equipment ownership, acquisition pacing, dungeons and supplies remain outside this work.

## Verification and files

Before allocation, **556 preceding publication pins** authenticated, including all live JSON and six runtime/test assemblies. The fresh driver consumed the exact frozen proposal, checked the integer gates and resource projections, and enforced conditional confirmation. The independent collector reauthenticated every archived file, recounted all 12,544 fights, checked the exact candidate edits, family/composition classification, seed disjointness, runtime/settings and native/process limits. The follow-up assessment reconstructed the equipment-only projection from the original source and verified the effective catalog abilities, all ten two-armor profile rankings, three distinct references and conservative family/fight limits. It allocated no combat or seeds.

**One fresh native screening fixture passed through `build/run-tests.ps1 -NoBuild`.** The unchanged **30 candidate Python checks, four Vaelor tests and 108 backend passes/four opt-in skips** were authenticated and reused, not rerun. No maintained implementation changed, so no C# rebuild or duplicate regression run was necessary. Conditional confirmation, application parity and post-application regressions were intentionally skipped because screening failed. The supported-search follow-up remains unexecuted. No required verification command is blocked.

Added this report, the frozen trial driver/collector and evidence, and the support-search assessment/proposal under `TestResults`; updated the diagnostic continuation notice, handoff, status, gear coverage and both harness guides. No live game-data, migration, configuration, database or deployment changes.

## Evidence and commands

- Independent trial evidence: `TestResults/tower-floor4-lance-only-evidence-20260930.json`, SHA **`b4ebfadc0523e4a0486b133cb7d9b5a565652fbc91104026ead27e3fae6d3168`**.
- Screen archive: `TestResults/tower-balance-pass-floor4-lance-only-screen-study-20260929`, manifest **`8cfc5190ff58f2ca490009adaacaae92734c0cbe39488eb36fe3ede372826f57`**.
- Candidate: `TestResults/tower-floor4-lance-only-driver-20260930/candidate.json`, SHA **`d2340eadfbc72063c13e2895b8a80efbfc40d3572362d8de010b02466fdd3a9a`**. Declaration, frozen protocol, driver source, command, resource admission, assessment and native TRX remain in that control directory.
- Entry: `TestResults/tower-floor4-lance-only-entry-20260930.json`.
- Proposed supported search: `TestResults/tower-floor4-two-armor-support-search-proposal-20260930.json`, SHA **`bc32beafd7618c4f218f7af1d9f61f5ef6c264bb588160e72df7f9b8500a4d82`**; assessment script `TestResults/tower-floor4-support-search-assess-20260930.py`. Status **ProposedNotAllocated**; zero additional seeds or fights.
- Latest ledger: `TestResults/tower-balance-pass-floor4-lance-only-screen-owner-20260929/seed-ledger.json`, SHA **`f632bb1c1e8daea5b1d70c16b5af9c9c84e704375faf58860a5195617b7be1e9`**, plus ancestors and later reservations. Exclusions **917,209**.
- Current publication: `TestResults/tower-floor4-lance-only-publication-check-20260930.json`. Earlier publications' source/document pins remain historical; do not repin or resume closed studies.

Executed with configured Python and `-B -X utf8`: `TestResults/tower-floor4-lance-only-prepare-20260930.py`, `tower-floor4-lance-only-driver-20260930.py`, `tower-floor4-lance-only-collect-20260930.py`, `tower-floor4-support-search-assess-20260930.py` and this publisher. The recorded native command calls `analysis/run-tower-balance-pass.py --mode screen --name floor4-lance-only-screen --floor 4 --samples 128` with pinned source, artifacts and candidate, through `build/run-tests.ps1`. Updated Markdown links and `git diff --check` pass. Preserve completed outputs; these drivers must not be rerun.
