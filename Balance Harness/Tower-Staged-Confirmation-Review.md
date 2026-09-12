# Bounded staged Kharad confirmation — 12 September 2026

**Result: Pass for the complete 2,918-party candidate family.** The entire frozen family meets the scoped 10–50% policy. The highest observed second-stage result was **84/256 (32.81%)**, with an adjusted interval of **22.63–44.91%**. The candidate is **not applied**; live Kharad retains its expanded-benchmark Fail. Competitive search quality remains **Fail**, and near-optimality and other-floor competitive balance are unestablished.

The complete workload used **152,168 fights**, including discovery and diagnostic replays. Timed execution/reconstruction phases totaled **68.70 minutes**, retaining about **2.72 GiB** before the final receipt. The exact recipes and compact evidence remain saved for reuse. This is a measured workload cost, not a matched speedup comparison with the earlier 600,000-fight run.

## Purpose and implementation

This increment targets the offline `LL/tools/BalanceHarness`. It follows the [four-team linked screen](Tower-Kharad-Linked-Screen-Review.md), which selected an unapplied **+4% Health/Power** candidate: Health **3.04881408**, Power **3.85370128**. The live inputs remain **2.931552 / 3.705482**. Earlier confirmation established a Fail for that live expanded benchmark; the older 2,438-recipe Pass retains its original scope.

The user keeps the current equipment and untrained-Essence budget while optimizing combinations. This floor-5 cohort uses ten characters, five Essences each, level 40, tier 1, rank 2, Standard quality and the fixed equipment context. No stronger gear, training, evolution or practical-acquisition assumption is introduced. Scenario IDs are standardized to `competitive-calibration-floor-5` for new measurement; actual character identities, party slots and ordered Essences are preserved.

The new [policy](../LL/tools/BalanceHarness/TowerStagedBalance.cs) and [runner](../LL/tools/BalanceHarness/TowerStagedBalanceRun.cs) add `tower-staged-balance` and `tower-staged-balance-verify` to [Program.cs](../LL/tools/BalanceHarness/Program.cs). The [README](../LL/tools/BalanceHarness/README.md#bounded-two-stage-balance-confirmation) documents their contracts and commands. Existing fixed-sample evaluation, historical archives and Tower Lab behavior are unchanged.

Policy `tower-staged-bonferroni-wilson-95-v1` freezes the entire family, cohort budgets, two fresh schedules, forced anchors, stage-two capacity and battle reservation. Anchors skip the first look and always receive the larger fresh second sample. Other cells receive the first fixed sample. Those whose adjusted upper bound is at most 50% resolve; every unresolved cell advances with every anchor. The complete selection is written and checked before second-stage combat, including after resume.

Each stage receives alpha .025. Stage-one Bonferroni adjustment covers every nonanchor cell. Stage two uses its complete selected family, conditional on first-stage data and independent new seeds. No first/second sample pooling occurs. Acceptance requires every final upper bound to be at most 50% and each cohort to have at least one lower bound of at least 10%. Wilson coverage is approximate and scoped to this experiment; it is not exact coverage, an exhaustive build guarantee or a lifetime repeated-testing claim.

A completed-stage observation above 50% rejects acceptance. A first-stage breach stops before the second sample; capacity overflow is Inconclusive. Pending cells remain visible. Missing, extra, duplicate or mismatched evidence cannot pass. There is no optional sample extension or outcome-based family reduction. The new contract supports up to 10,000 cells and 500,000 reserved fights while leaving ordinary limits unchanged. It reuses the existing prepared campaign engine, durable attempt accounting and 32-report streaming verification. Matching interrupted work can resume without changing inputs or limits; repeated attempts do not enlarge statistical samples.

## Preparation and bounded challenger coverage

The [preparation script](../TestResults/balance/tower-staged-confirmation-20260912/prepare-inputs.py) verified the prior screen's 610 sealed artifacts and inventory source hashes, imported **467,295** excluded seed values from 99 recognized history sources, and copied only the 16 allowlisted data files for the selected setting. A new minimal nonsecret settings file supplies the frozen threat and checkpoint settings to the shared loader; it does not copy application configuration. The idle-cadence field is a loader requirement and is unused by Tower execution. Selected Tower settings match the prior screen exactly.

C# canonical normalization and full materialization retained all **2,678** inventoried parties. **186** initially qualified as strong anchors: any historical portfolio, pilot-discovery or pilot-validation observed rate at least 10%, plus all four confirmed controls. These earlier outcomes allocate effort only. Two diagnostic-driver preflight attempts rejected nonconforming reference seeds and role labels before any campaign fight; those inputs were corrected before the coverage protocol was frozen. The driver then built with zero warnings/errors.

The [coverage protocol](../TestResults/balance/tower-staged-confirmation-20260912/coverage-protocol.json) fixed two modes, two methods and two restarts, each evaluating 32 proposals on eight shared fresh seeds: **2,048** discovery fights. Independent generation receives no reference-derived starts; the separate improvement mode starts from the four confirmed recipes and retains its ancestry. All evaluated parties enter the final family, including nonfinalists and above-ceiling observations. No historical result becomes current fitness.

Four detailed replays using the old screen's first seed matched the prior build exactly. The game assemblies are unchanged; only the harness implementation identity changed. Those repeats are diagnostics, not fresh acceptance evidence. Each discovery campaign fully reconstructed without combat. Total coverage work was **2,052 fights**, against a 2,116-attempt cap with 64 unused retries. Timed phases totaled about **73.23 seconds**.

Independent discovery evaluated 128 parties and observed no wins. Supplied-team improvement evaluated 128 parties, 73 with at least one win, including one **5/8** result. Eight samples do not establish that candidate's repeatable rate. Its recipe and all other evaluated candidates remain in confirmation. This small independent search does not demonstrate reliable rediscovery or saturation.

The complete [family](../TestResults/balance/tower-staged-confirmation-20260912/family.json) contains **2,918** canonical parties: all 2,678 prior recipes plus **240** new unique recipes after overlap removal. **243** are forced anchors under the same allocation rule, including any new discovery rate at least 10%. Recipe provenance and all prior/current hints are retained. The four confirmed controls are all included as anchors. Anchor status controls sample allocation; generation ancestry comes from the recorded sources and must not be inferred from anchor status or the cell's validator role.

## Frozen confirmation limits

The [definition](../TestResults/balance/tower-staged-confirmation-20260912/definition.json) and [protocol](../TestResults/balance/tower-staged-confirmation-20260912/protocol.json) were frozen after coverage, before any confirmation fight. Both confirmation schedules were already reserved before coverage and excluded from its search schedule.

| Reservation | Value |
| --- | ---: |
| Complete recipe family | 2,918 |
| Nonanchor first-stage cells | 2,675 |
| First-stage sample | 32 each; 85,600 fights |
| Forced second-stage anchors | 243 |
| Maximum second-stage cells | 384, including all unresolved cells |
| Fresh second-stage sample | 256 each; at most 98,304 fights |
| Maximum logical confirmation fights | **183,904** |
| Retry reserve / detailed replays | 64 / 4 |
| Maximum confirmation attempts including replays | **183,972** |
| Entire workload attempt maximum including coverage | **186,088** |
| Campaign storage / entire package storage | 5 GiB / 6 GiB |
| Original confirmation invocation / coverage time | 3,600 seconds / 600 seconds |

Execution is sequential prepared mode with 32-report chunks. Time cancellation and storage checks are cooperative, not operating-system quotas; setup or an in-flight write can cross a check boundary. The frozen driver permits no automatic resume or cap extension. Historical fixed allocations are not retrospectively reclassified, and the 10–50% target is unchanged.

## One-hour stop and reviewed continuation

The first invocation honored its one-hour limit and stopped at **3600.65 seconds**, with **148,288 fights committed** and **1,824 scheduled fights remaining**. Every started fight was completed and committed; no retry attempts were lost. The stopped invocation did not produce a balance Pass, and its [failure receipt](../TestResults/balance/tower-staged-confirmation-20260912/initial-attempt-failure.json) and original performance receipt remain preserved.

The [stop-review script](../TestResults/balance/tower-staged-confirmation-20260912/prepare-resume.py) verified **91 complete batch archives** and the partial batch's committed chunks before freezing a [separate continuation protocol](../TestResults/balance/tower-staged-confirmation-20260912/resume-protocol.json). Its limit was **600 seconds** and **1,892 new attempts**: the remaining fixed samples, four replays and 64 available retries. The original campaign contract, battle cap, storage limits, recipes, selection and seed schedules were unchanged. This was a separately reviewed continuation after the original driver stopped, with no automatic repeat loop or outcome-dependent sample extension.

The [continuation driver](../TestResults/balance/tower-staged-confirmation-20260912/followup-driver/Program.cs) reconstructed the complete first-stage selection and verified saved work before executing only missing trials. Its resume phase took **268.51 seconds** and ran **1,824 fights**; four matching replays followed. Both diagnostic drivers built without warnings/errors. Total workload timings below include the stopped invocation, continuation and reconstruction; the experiment is not described as having finished within the original one-hour window.

## Confirmation results and saved builds

The [assessment](../TestResults/balance/tower-staged-confirmation-20260912/campaign/assessment.json) returns **Pass** under the frozen policy. First-stage evidence resolves **2,666 of 2,675** nonanchors; **9** advance alongside **243** forced anchors. The final second stage contains **252** cells, against capacity 384. Logical confirmation cost is **150,112**, below its **183,904** reservation. There were **0** retry/uncommitted attempts. [The frozen selection](../TestResults/balance/tower-staged-confirmation-20260912/campaign/stage-selection.json) remains auditable.

There are **0** completed final observations above 50%, **0** final adjusted upper bounds above 50%, and **53** cells with a supported lower bound of at least 10%. The largest final upper bound is **48.53%**. Bounds come from their declared stage family; they are not ordinary pointwise intervals and first/second samples are not pooled.

Highest observed second-stage results:

| Recipe ID | Wins / sample | Observed | Adjusted interval |
| --- | ---: | ---: | ---: |
| team-1abe76ca1891d97a91d484f0a3662048 | 84/256 | 32.81% | 22.63–44.91% |
| team-3a69c759178064021dc5cf7124d7f4f5 | 79/256 | 30.86% | 20.96–42.90% |
| team-79263759eab3401a66a17e1ab7a76263 | 79/256 | 30.86% | 20.96–42.90% |
| team-a954394f09e052e5e9c5d1dbaee5331b | 78/256 | 30.47% | 20.63–42.50% |
| team-c44f3bf76c24625c756d9eb1d463800b | 78/256 | 30.47% | 20.63–42.50% |
| team-9a9488905dd689f74dd0ee24aa67a5a9 | 76/256 | 29.69% | 19.96–41.68% |
| team-ea954bbf9993799e9662e489a79d11b6 | 76/256 | 29.69% | 19.96–41.68% |
| team-f79d550ebee543d638b75a30030a9656 | 76/256 | 29.69% | 19.96–41.68% |
| team-22f6e11799743b343cb0c497a595ba59 | 73/256 | 28.52% | 18.98–40.46% |
| team-5dfd57f1325f6f10940d9513605b40d9 | 73/256 | 28.52% | 18.98–40.46% |

The [top 20 confirmed recipe export](../TestResults/balance/tower-staged-confirmation-20260912/top-confirmed-recipes.json) stores exact reusable seed-free scenarios, measurements and provenance. The [full family](../TestResults/balance/tower-staged-confirmation-20260912/family.json) preserves every included recipe, not only these leaders. Future runs can reuse recipes and their ancestry, but must measure them again on compatible frozen content and fresh seeds. This increment does not automatically promote them into the normal retained catalogs or change Tower Lab defaults.

The highest observed party is a new supplied-team local improvement. The earlier **5/8** discovery candidate remains included and confirmed at **79/256 (30.86%)**, with an adjusted **20.96–42.90%** interval; its discovery result is preserved and is not pooled with confirmation. The four preselected confirmed controls recorded **78/256, 50/256, 57/256 and 68/256** in their original order. Ranked differences within the new sample are descriptive: no paired superiority or new search-quality plateau test was predeclared. Resolving the supplied family cannot establish that stronger unsearched builds do not exist.

## Resource use

| Timed phase | Fights | Seconds |
| --- | ---: | ---: |
| Bounded searches, old-build parity and search reconstruction | 2,052 | 73.23 |
| Staged confirmation and its detailed replays | 150,116 | 3889.72 |
| Full completed campaign reconstruction | 0 | 158.99 |
| Total timed phases | **152,168** | **4121.95** |

Total measured CPU across these phases was **3932.97 seconds**; cumulative allocation was **1346.58 GiB**, with maximum process-lifetime peak working set **3784.20 MiB**. Allocation is garbage-collected traffic, not saved disk space or concurrent memory. Preparation, builds, independent Python analysis, documentation and final integrity work outside these phases are excluded. Shared content/executables and compressed 32-report chunks keep this package bounded; the separately frozen sample allocation reduces fights. Neither change retrospectively alters old evidence or proves a matched runtime speedup.

The stopped one-hour phase measured **1,324.28 seconds** in simulation without checkpoints and **1,952.29 seconds** of exclusive `compact.create` work outside separately timed child operations. The latter includes operations not yet separately attributed, rather than proving that all 32.54 minutes are storage scans. It is the main remaining instrumentation target. Compatible report hashing and compressed writes are measured separately in the retained [profile](../TestResults/balance/tower-staged-confirmation-20260912/resource-confirmation.json).

Source inspection identifies one scaling candidate in that gap: `TowerBulkCampaign.CheckStorage` walks the accumulated campaign tree at each committed chunk. As the archive grows, repeated file enumeration grows with it. Durable attempt journaling and shared-file handling also need separate measurements. Outside archive creation, ordinary validation repeatedly intersects each cell's schedule with the large historical exclusion list; a per-validation exclusion set is another bounded optimization candidate. A future matched benchmark should time these operations separately and evaluate writer-owned byte accounting with authoritative recounts at setup/resume and batch boundaries, while preserving storage limits and integrity checks. The producing code was kept fixed throughout this campaign.

## Verification

The [new tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerStagedTests.cs) cover complete-family bounds, forced anchors, fresh second samples without pooling, overflow without dropped cells, first-stage breaches, missing/extra/duplicate evidence, schedule/hash changes, multi-cohort viability, legacy-limit separation, numerical NormalDist fixtures and cancellation/resume. The small resume test charges an uncommitted attempt, reconstructs the completed run through the CLI and rejects tampered stage selection.

Backend checks ran through the required `build/run-tests.ps1` wrapper. The focused staged suite passed **16 tests**; the expanded harness regression passed **286**, with no failures or skips. [Focused](../TestResults/balance/tower-staged-confirmation-20260912/focused-tests.trx) and [regression](../TestResults/balance/tower-staged-confirmation-20260912/regression-tests.trx) receipts are retained. The backend Release build succeeded with zero errors and five existing unrelated warnings. Diagnostic-driver restore used empty package sources and approved SDK access to local NuGet configuration; its final build had no warnings/errors.

Source changes are the two staged classes, CLI wiring and staged tests. Documentation changes are this review, the six active discovery/loadout/harness/policy Markdown files and the harness README. The ignored output directory retains the driver, protocols, complete recipes, search/confirmation evidence, producing executables and analysis receipts. No prior evidence was deleted. No migration, live game configuration change, catalog promotion, deployment, dependency addition or external-environment effect is part of this increment.

The [independent Python analysis](../TestResults/balance/tower-staged-confirmation-20260912/analyze.py) reads every compressed outcome and exact recipe, checks manifest/chunk hashes, schedules, family coverage, attempt counts and stage selection, and independently calculates all Wilson bounds using `statistics.NormalDist` with agreement within 1e-8. [Its receipt](../TestResults/balance/tower-staged-confirmation-20260912/analysis.json) reproduces the assessment. The producing harness fully reconstructed the completed campaign with **zero fights**, and selected detailed replays matched. [Final verification](../TestResults/balance/tower-staged-confirmation-20260912/final-verification.json) checks historical protection, all prior sealed artifacts/reviews, current content/catalogs/settings/assemblies, current source hashes, test receipts and links. `git -c core.safecrlf=false diff --check` passed. No required command remains blocked.

## Next step

Freeze a small checked local-application protocol for **Health 3.04881408 / Power 3.85370128**, verify the two-field-only change and matching reports against the confirmed candidate, then record the locally applied setting. Reuse the saved complete family and top recipes with their provenance in future benchmark inputs; do not replay this entire search merely to load saved teams.

Before another large campaign, separately profile archive bookkeeping, storage scans and exclusion validation, then test any optimization on a bounded matched workload. Improve independent search reliability against the saved strongest controls and run fresh challenger comparisons before treating Kharad as a competitive benchmark. Floors 6–11, post-calibration challenges on floors 2–4 and practical acquisition coverage remain open. None of those campaigns ran in this increment.
