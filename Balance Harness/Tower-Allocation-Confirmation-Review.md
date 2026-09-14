# Fresh allocation confirmation: family Pass, reliability Fail 1/3

Completed and verified **14 September 2026**. The [separately frozen plan](Tower-Allocation-Confirmation-Plan.md) has been executed in full: **94 recipes × 512 fresh shared trials = 48,128 fights**, exactly once. Ordinary and joint family assessments are **Pass**. **12 recipes support viability**, including **four newly selected recipes** and eight prior controls. The saved isolated-pair primaries pass only **1/3** reliability restarts; two are required. Adoption remains **Hold**.

This closes the confirmation gap for the saved recipes. The [original allocation experiment](Tower-Search-Allocation-Review.md) remains **StoppedTimeCap / Unresolved**, with its original evidence and limit intact. This study generated no new candidates and pooled no earlier outcomes. Four viable recipes from one component are not four independent successful restarts.

## Measured results

The six screened primaries and their original nominations were frozen before the new seed allocation. Discovery and screening columns below describe separate historical samples; only the final column uses this study's fresh trials.

| Restart / root seed | Approach | Discovery rank | Discovery / 8 | Screen / 64 | Fresh confirmation / 512 |
| --- | --- | ---: | ---: | ---: | ---: |
| 1 / 1614029318 | Deep | 14 | 1 | 1 | 6 |
| 1 / 1614029318 | Isolated pair | 1 | 0 | 0 | 0 |
| 2 / 1816290420 | Deep | 7 | 1 | 5 | 41 |
| 2 / 1816290420 | Isolated pair | 11 | 1 | 27 | **179** |
| 3 / 233022769 | Deep | 4 | 1 | 3 | 0 |
| 3 / 233022769 | Isolated pair | 1 | 0 | 0 | 0 |

The second isolated primary, `team-b4c41e1136b71c41f1f35fbe25b7a8d1`, won **179/512 (34.96%)**, with joint adjusted interval **27.75–42.94%**. Its paired gain over the matching deep primary is **26.95 percentage points**, interval **17.03–35.83 points**. It meets viability, improvement and anchor recovery. The other two isolated primaries won zero and fail viability and improvement.

The anchor won **0/512**; the fixed strongest prior control won **145/512**. The successful primary's difference from that strongest control is **+6.64 percentage points**, interval **−5.09 to +18.11 points**. Superiority over that control is unestablished and was not part of the adoption gate.

All four newly supported recipes came from **isolated B, root 1816290420**:

| Recipe | Frozen role in that group | Fresh wins | Joint adjusted interval |
| --- | --- | ---: | --- |
| [team-0be29aaae17c2eadf7cb8b4cf8251a35](../TestResults/balance/tower-allocation-confirmation-work-20260914/recipes/team-0be29aaae17c2eadf7cb8b4cf8251a35.json) | Original primary | 189/512 | 29.56–44.93% |
| [team-b4c41e1136b71c41f1f35fbe25b7a8d1](../TestResults/balance/tower-allocation-confirmation-work-20260914/recipes/team-b4c41e1136b71c41f1f35fbe25b7a8d1.json) | Screened primary | 179/512 | 27.75–42.94% |
| [team-090e5bf20d0604621cd2c8306e81289d](../TestResults/balance/tower-allocation-confirmation-work-20260914/recipes/team-090e5bf20d0604621cd2c8306e81289d.json) | Screened secondary | 166/512 | 25.41–40.33% |
| [team-1f4a13f6d3c98be196dc8493730e30fb](../TestResults/balance/tower-allocation-confirmation-work-20260914/recipes/team-1f4a13f6d3c98be196dc8493730e30fb.json) | Original secondary | 160/512 | 24.34–39.11% |

The original primary has the highest observed result in the full family, **36.91%**. It does not replace the screened primary after confirmation, and this observation does not establish a benefit or harm from screening. Every included recipe supports the 50% ceiling, at least one supports the 10% viability threshold, and there are **zero observed ceiling breaches**. These conclusions apply to this complete 94-recipe family. The earlier [17,821-recipe precision Pass](Tower-Kharad-Precision-Resolution-Review.md), with 18 supported viable recipes under its own design, remains separate.

## Implementation and resource result

Added [TowerAllocationConfirmation.cs](../LL/tools/BalanceHarness/TowerAllocationConfirmation.cs) and [TowerAllocationConfirmationRun.cs](../LL/tools/BalanceHarness/TowerAllocationConfirmationRun.cs), plus additive CLI commands in [Program.cs](../LL/tools/BalanceHarness/Program.cs). [TowerFeedbackBenchmark.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmark.cs) now shares the existing confirmation calculation with the new workflow; its numerical rule is unchanged. [New tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerAllocationConfirmationTests.cs) cover the new contract. Six active guides, this review and the new frozen plan document the result.

Preparation verifies the full stopped campaign and work inventories, imports the exact 94 recipes and their audit bindings, and freezes all six screened primaries, original nominees, 74 controls and their provenance. Checks require matching content, settings, gameplay assemblies and runtime, allowing only the harness assembly to change. The new ledger unions the current history, source ledger and every old schedule. It retains **479,021 preceding reservations** and adds **512** fresh values: **479,533 total**. Exclude every array in the [new authoritative ledger](../TestResults/balance/tower-allocation-confirmation-20260914/seed-ledger.json), used or unused.

The new study used the existing compact archive implementation with three dense batches of 32, 32 and 30 recipes. Shared storage and integrity checks were unchanged. A durable start marker forbids resume, retries and extensions. Incomplete evidence cannot produce quality.

| Resource | Frozen cap | Actual |
| --- | ---: | ---: |
| Starts / completions | 48,128 | 48,128 / 48,128 |
| Retries / uncommitted completions | 0 | 0 / 0 |
| Timed execution and assessment | 3,600 seconds | **903.4447693 seconds (15.06 minutes)** |
| Campaign storage | 4,294,967,296 bytes | **610,075,038 bytes (581.81 MiB)** |
| Campaign files | No separate cap | **3,430** |
| Separate saved-report reconstruction | Read-only | **34.84 seconds; 48,128 reports; zero fights** |

The [read-only profile](../TestResults/balance/tower-allocation-confirmation-work-20260914/profile.json) measured discovery-tree scan growth and inexpensive dense confirmation scans without combat. Its prior confirmation throughput suggested 21–23 minutes; this run completed in 15.06 minutes. This validates the declared envelope for this run, without attributing the stopped experiment's entire overrun to one cause or claiming a general speedup.

The [execution timings](../TestResults/balance/tower-allocation-confirmation-20260914/timings.json) include 517.18 seconds in engine simulation, 73.49 seconds flushing compact attempts and 38.88 seconds enumerating files during 1,504 chunk-boundary storage checks. These named measurements do not account for every activity; nested inclusive durations must not be added together. Integrity checks and durable accounting remain enabled.

## Verification and retained deliverables

Both final builds passed. Backend tests ran through `build/run-tests.ps1`: **130** focused comparison tests and **138** confirmation/archive tests, **238 distinct passing cases**, including **30 new confirmation cases**. The second batch used the final compiled source. Six pre-existing test-build warnings remain; the harness build had none.

Seven checks against an actual prepared copy verified valid inputs and rejected changed nominees, omitted recipes, historical seed reuse, unlisted files and repeated execution, with zero combat and unchanged original prepared hashes. The retained executable then ran the campaign once. Its exit code **1** denotes the complete **Hold** result; it is not an interrupted execution. Completed verification returned **0** and reconstructed every report under a guard that throws on any combat attempt. The [independent Python check](../TestResults/balance/tower-allocation-confirmation-work-20260914/independent-statistics-check.json) reproduced all 94 joint rates, 94 ordinary rates, nine paired differences and the 1/3 gate using `statistics.NormalDist`, with absolute tolerance 1e-8.

All required verification commands ran. The [closure receipt](../TestResults/balance/tower-allocation-confirmation-work-20260914/final-verification.json) binds producing sources, tests, current documentation, results and final inventories. Full stopped campaign/work inventories were rehashed before preparation and at closure. Earlier sealed manifest hashes and the preceding full-preservation receipt were checked; this work does not claim another full scan of all older historical archives. Existing unrelated working-tree changes were preserved.

- [Measured result data](../TestResults/balance/tower-allocation-confirmation-work-20260914/results.json) and [completed archive](../TestResults/balance/tower-allocation-confirmation-20260914/final-files.json).
- [All 94 full recipe exports](../TestResults/balance/tower-allocation-confirmation-work-20260914/recipe-index.md), including complete gear, ordered builds, control/new classification, provenance and fresh results.
- [Six nominee recipe cards](../TestResults/balance/tower-allocation-confirmation-work-20260914/nominee-recipes.md), with exact Essence-copy requirements.
- [Reusable complete family](../TestResults/balance/tower-allocation-confirmation-work-20260914/family.json) for explicit future control registration; no catalog promotion.
- [Captured verification receipt](../TestResults/balance/tower-allocation-confirmation-work-20260914/verification.json), [prepared checks](../TestResults/balance/tower-allocation-confirmation-work-20260914/prepared-negative-checks.json), and [decision](../TestResults/balance/tower-allocation-confirmation-work-20260914/decision.json).

Protocol SHA-256: `4bff91b2377ae373bc3782ae6f1ce42ef876268ca03b1ad71c830b6809f19d31`. Captured harness SHA-256: `b2337d5774ecf6fc041fdef533947e4f8c04603c5a43da7ce89c47dcdce67ddd`. Final campaign manifest SHA-256: `a436bc957768ca5d9313e350f67b7f4f94fa8d01af8d330f25ea6ad8a23e402f`.

## Decision and next boundary

Keep allocation experimental and adoption on Hold. The new measurements establish four viable discovered recipes and close the operational confirmation gap; reliability across restarts remains the search-quality bottleneck. The next bounded step is a read-only comparison of the saved nine component trajectories: trace when the four successful recipes appeared, their parent/module ancestry and ranking, and compare equivalent checkpoints in the other components. Require one evidence-backed mechanism decision before another optimizer change or fresh combat allocation. Saved winners remain explicit controls and do not seed independent generation.

Practical acquisition is still a separate milestone. The successful screened primary requires **50 copies across eight Essence types**, including ten Pack Howlers, ten Poisonous Rats, nine Enchanted Fairies and nine Spider Queen Royal Venom Essences. This study assumed those copies were owned; it does not establish acquisition feasibility. Floors 6–11, unsearched teams, near-optimality and fresh generation replication remain open.

Kharad stays at **Health 3.5366243328 / Power 4.4702934848**. Player budget, acceptance gate, defaults and catalogs are unchanged. **No migrations, configuration changes or deployment implications.** No external environment was changed.
