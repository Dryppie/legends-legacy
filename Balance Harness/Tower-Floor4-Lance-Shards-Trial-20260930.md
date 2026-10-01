# Floor 4: Mirror Lance and Hall of Shards trial — 30 September 2026

**Subsequent result — diagnostic complete:** The [rejected-candidate diagnostic](Tower-Floor4-Lance-Shards-Diagnostic-20260930.md) verified **64 complete historical replays**, **zero fresh seeds**. Among the **16 two-character armor replays**, Hall magical caused the first death at **16 s in 9**, and Hall physical in another **2**; only **1** had a first death before Hall. Direct healing before 20 s averaged only **77/113** health for A/B, against **7,734/8,343** health damage. Reflection began at **22.4 s or later** and caused none of these opening deaths. This describes the rejected candidate; it is not acceptance evidence or a causal comparison with the older unpaired panels. **25 fresh Python safeguards passed**; the prior **108 backend passes/four opt-in skips** were authenticated and reused. All live game data, budgets and the supported search remain unchanged; exclusions **917,081**. The diagnostic proposed below has completed; use the latest linked report and handoff for the unallocated Lance-only trial. The rejected trial and its original proposal remain immutable historical records.

## Frozen prospective protocol

The [pressure diagnostic](Tower-Floor4-Pressure-Diagnostic-20260930.md) identified Mirror Lance's lowest-current-health magical hit at 14 seconds as a frequent first-death source in partial armor, followed by Hall of Shards at 16 seconds. The [Hall-only trial](Tower-Floor4-Shards-Trial-20260930.md) failed both the partial-armor minimum and full-family ceiling. Test the previously prepared joint candidate once, from the unchanged live-catalog source.

Mirror Lance magical coefficient **1.00 → 0.60**, status coefficient **0.010 → 0.006**; Hall of Shards magical **0.50 → 0.65**, status coefficient **0.005 → 0.0065**. Both retain Mirrorbound's 1% scaling per stack. Keep Mirror Lance physical **2.00**, Hall physical **0.50**, all selectors, ordering, cooldowns, statuses, reflection and guardian scalars. Only these four coefficients and the two matching descriptions change in isolated candidate content. This tests whether reducing the early execution hit helps partial armor while later magical damage retains pressure on full armor; it is a hypothesis, not an accepted setting.

- Frozen proposal: `TestResults/tower-floor4-lance-shards-proposal-20260930.json`, SHA `9aff59199b03fff8f1ea081bd9f2b394e68eaf71c17675931b56200673a61680`. Preserve this proposal as the immutable pre-allocation record.
- Original current-catalog source: `TestResults/tower-balance-pass-floor4-mixed-armor-screen-study-20260929`, manifest `66cc379b5f226c6925e7b1a240c0f65c8bef6d630bcafde7978ca036140d406d`. Retain all **98 loadouts**, 38 controls plus 60 variants, five actual compositions. No search, reordered characters or Essences, imported recipes, dropped controls or budget changes.
- Initial exclusions **916,953**, plus every historical reservation. Reauthenticate the latest publication, all current catalogs, six runtime/test assemblies, reused safeguards/regression receipts and source archive before allocation.
- Screen **128 fresh shared seeds / 12,544 fights**. Require at least two distinct actual compositions with armor on at most two of five characters/eight specialized items, each approximate simultaneous 95% Bonferroni-Wilson lower bound at least 10%; every loadout's upper bound at most 50%. Correct for all 98 loadouts; screen gates **25–44 wins / 128**.
- Only a passing whole-family screen permits independent **184-seed / 18,032-fight confirmation**, same candidate and family, gates **33–68 / 184**. No pooling with screening or historical outcomes. A passing confirmation requires native candidate parity before any local application, then gameplay regressions.
- Maximum **30,576 fresh fights / 312 reservations**, one candidate, no retries, replacement seeds, extensions or post-result retuning within this scope. Failed screening closes the scope without confirmation or application.
- Native limits per phase **20,000 fights / 840 seconds / 2 GiB**, owner **900 seconds**. Double the measured preceding time/storage cost; require projected use below 80% of those limits before each phase.

Keep Vaelor health/offense **2.3231953125 / 5.023125**, five level-30 characters with four level-1 unevolved/unascended Essences each, T1 Epic/Fine/Rank 3 gear, roll 1 and no styles. Preserve the approved repeating gear curve and prior floor-2/5/6 changes. Scope is the primary LL World Tower and offline harness. No dungeon, supply, migration, configuration or deployment work.

Fresh paths: `TestResults/tower-floor4-lance-shards-driver-20260930` and native `tower-balance-pass-floor4-lance-shards-{screen,confirm}-{study,owner}-20260929`. The entry record identifies the reused **30 candidate Python checks, four Vaelor tests and 108 backend passes/four opt-in skips**; these are not fresh test executions. Native study fixtures use `build/run-tests.ps1 -NoBuild`. Driver and independent collector validate exact changes, all saved fights, family decisions, resource limits and seed accounting.

## Verified result

**Rejected: `NoEligibleLanceShardsConfirmation`.** All **98 exact loadouts / five actual compositions** completed screening. The two required partial-armor compositions each reached at most **2/128**, well below **25/128**. Every recipe passed the upper ceiling, but **none** reached the 10% adjusted lower bound, including the full-armor controls. No independent confirmation or live application was warranted.

| Recipe | Armored slots | Wins | Adjusted interval | Median first death, seconds |
| --- | --- | ---: | ---: | ---: |
| A/baseline | none | 0/128 | 0.00%–8.62% | 16 |
| A/2-armored | 1, 3 | 2/128 | 0.20%–11.28% | 16 |
| A/4-armored | 1, 2, 3, 4 | 11/128 | 3.19%–21.13% | 16 |
| A/full-armor | 1, 2, 3, 4, 5 | 10/128 | 2.77%–20.13% | 16 |
| B/baseline | none | 0/128 | 0.00%–8.62% | 16 |
| B/2-armored | 1, 3 | 2/128 | 0.20%–11.28% | 16 |
| B/4-armored | 1, 2, 4, 5 | 10/128 | 2.77%–20.13% | 16 |
| B/full-armor | 1, 2, 3, 4, 5 | 20/128 | 7.52%–29.66% | 16 |

Intervals retain the simultaneous correction for the complete 98-loadout family. The first-death column uses all 128 saved fights for each of these eight recipes. This is a **1,024-fight subset of the already counted 12,544**, not extra combat. The best two-character variants both use slots **1 and 3**; all other subsets remain in the acceptance family. The strongest full-armor control's adjusted upper is **29.66%**.

This candidate does not improve established Tower balance: it was rejected and no gameplay value changed. The unchanged-catalog screen, Hall-only trial and this trial used different seed panels; their win counts are descriptive comparisons, not paired causal estimates. This joint candidate also changed two magical components together, so it does not isolate either one's effect. Lowering the early execution hit while increasing later magical pressure failed to establish the requested gear tolerance. The result does not establish that all alternative ability settings or compositions must fail.

Fresh work: **12,544 fights / 128 reservations**, **126.54 native seconds**, no retries, and a clean owner exit with zero active children. Final exclusions **917,081**. Live Mirror Lance physical/magical remains **2.00/1.00**, Hall physical/magical **0.50/0.50**, with corresponding original 1% Mirrorbound coefficients. Vaelor health/offense remains **2.3231953125 / 5.023125**. The five-character, level-30, four-Essence, T1 Epic/Fine/Rank-3 budget and the user's repeating progression curve remain unchanged. Prior floor-2/5/6 applied improvements are preserved.

## Verification and changed files

The fresh driver consumes the exact previously frozen proposal and validates the screen/confirmation integer gates, all 98 raw loadouts, current catalogs, six runtime/test assemblies, reserved-seed history and doubled resource projections. The independent collector reauthenticated the complete archive, recounted every saved fight, reconstructed every bound and actual-composition decision, verified exactly the declared four coefficient/two description edits, and checked seed disjointness, native limits and clean process exits. The separate saved-outcome review verifies equipment-only differences and all 128 reports for each selected recipe.

**One fresh native screening fixture passed through `build/run-tests.ps1 -NoBuild`.** The unchanged **30 candidate Python checks, four Vaelor mechanic/catalog tests and 108 backend passes/four opt-in skips** were authenticated and reused; they are not fresh executions. No C# or maintained Python implementation changed, so no rebuild or duplicate regression run was required. Source preparation, candidate execution, independent collection and saved-outcome review all completed successfully. Conditional confirmation, candidate application parity and post-application regressions were intentionally not run because screening failed. No required verification remains blocked.

Added this report and immutable study/evidence/proposal artifacts under `TestResults`; updated the continuation handoff, balance status, gear coverage, preceding floor-4 notices and two harness guides. No game-data, migration, configuration, database or deployment changes.

## Next work

Diagnose the rejected joint candidate before choosing another coefficient. The saved 1,024-fight subset has median first death at **16 s in all eight recipes**, but no event timelines. The frozen follow-up `TestResults/tower-floor4-lance-shards-diagnostic-proposal-20260930.json` selects baseline, best two- and four-character armor, and full armor for each parent, then the first eight declared seeds: **64 exact historical replays, zero fresh seeds**. It is **ProposedNotExecuted**. First add and test explicit archived-candidate diagnostic admission: bind the unchanged live source and the exact rejected candidate separately; existing live-catalog equality checks must remain intact. Inspect first-death sources, targets, healing and damage before 15/20 s under 64-replay/1,200-second/2-GiB caps. Retain all 98 recipes, two viable actual compositions with at most eight specialized items, and the 50% whole-family ceiling. Do not extend either failed candidate screen or choose another damage compensation blindly. No active study remains; keep work on Tower balancing.

The 16-second medians are consistent with the known Hall cast timing, but saved recipient summaries cannot identify the lethal effect. Use complete detailed event parity to distinguish magical/physical Hall hits, the preceding Mirror Lance sequence and missing healing. The diagnostic selects its recipe sample descriptively and is not a new acceptance test. The earlier diagnostic used different seed panels and some different selected subsets; comparisons with it must retain those limitations. A later candidate must still undergo a new declared full-family screen and independent confirmation before any local application. No numerical follow-up candidate has been selected.

## Evidence and commands

- Independent evidence: `TestResults/tower-floor4-lance-shards-evidence-20260930.json`, SHA **`1a5c449ef6ef8e4d1f3448a2763d96f3326deb31b9445e35bee387f892740862`**.
- Screen manifest: **`c2a8fb0f42e6b4f863f660d0c15176818d6c6eeeca9cf99267c5266829dde5f6`**, archive `TestResults/tower-balance-pass-floor4-lance-shards-screen-study-20260929`.
- Candidate: `TestResults/tower-floor4-lance-shards-driver-20260930/candidate.json`, SHA **`f885e293405c55e22838bccc6c9a71af7b1017c19a3675586e9e1e2cd249699c`**. Frozen protocol, source, declaration, resource admission, command, TRX and assessment are in the same control directory. Preserve every completed path.
- Entry check: `TestResults/tower-floor4-lance-shards-entry-20260930.json`; all **47** preceding publication pins and all **six** runtime/test assemblies authenticated before allocation.
- Saved review: `TestResults/tower-floor4-lance-shards-saved-review-20260930.json`, SHA **`7c51de436305e871a3a91b005eb958904c4eb81233ebe3222586e8bcdc4ce46d`**; zero new fights or seeds.
- Unexecuted diagnostic proposal: `TestResults/tower-floor4-lance-shards-diagnostic-proposal-20260930.json`, SHA **`d40e451702756f774bd8da5e3bd94a73c9774146f040191b771806413aa273e7`**. Its 64 saved trial IDs and recipe/seed order are already frozen.
- Latest ledger: `TestResults/tower-balance-pass-floor4-lance-shards-screen-owner-20260929/seed-ledger.json`, SHA **`b8642ec3f3a3db2fc22b29d75b02f4b4a8b416f94b47f85aa28baef882c86e8d`**, plus ancestors and later reservations.
- Current publication: `TestResults/tower-floor4-lance-shards-publication-check-20260930.json`. It binds current documentation, immutable evidence, live catalogs, runtime and ledger pins. Older publication document pins describe their own historical publication times.

Executed with configured Python and `-B -X utf8`: `TestResults/tower-floor4-lance-shards-prepare-20260930.py`, `tower-floor4-lance-shards-driver-20260930.py`, `tower-floor4-lance-shards-collect-20260930.py`, `tower-floor4-lance-shards-review-20260930.py`, and this publisher. The recorded screening command invokes `analysis/run-tower-balance-pass.py --mode screen --name floor4-lance-shards-screen --floor 4 --samples 128` with the pinned source, artifacts and candidate. Its backend fixture runs through `build/run-tests.ps1`. Markdown link checks and `git diff --check` pass. These completed drivers are historical records, not commands to rerun.
