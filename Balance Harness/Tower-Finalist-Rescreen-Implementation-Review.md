# Finalist rescreen implementation and prepared comparison

Completed **13 September 2026** for the offline BalanceHarness. The opt-in finalist rescreen is implemented, tested and frozen in a new executable comparison package. **No campaign fights have run.** Independent-search reliability therefore remains **Fail 0/3** at the applied Kharad setting; this implementation does not supply a new efficacy result.

## Delivered behavior

The unchanged v13 generator still evaluates 384 candidates in each of six arms across three paired restarts. The new selector preserves all 12 original finalists and freezes each v13 arm's top 32 complete recipes before any rescreen measurement. Each of those 32 receives the same 64 fresh trials. Selection uses fresh wins, then original discovery rank; it never pools discovery observations or replaces a finalist after confirmation.

Confirmation retains all original finalists, new finalists, 20 separately registered controls and every recipe observed above 50% in discovery or rescreen. It deduplicates exact recipes while retaining their source associations. Every confirmation recipe receives the same 512 untouched seeds. If the complete family exceeds 64 recipes, the runner preserves it and stops with `CapacityExceeded` instead of truncating it.

The runner captures executable dependencies, all 16 content files, sanitized combat settings, generation inputs, recipes and complete seed history. It checks frozen hashes and runtime identity, records each battle start/completion durably, rejects a second start and reconstructs completed evidence under a no-combat guard. Existing generator implementations, historical selectors and CLI defaults remain unchanged.

The [execution plan](Tower-Finalist-Rescreen-Execution-Plan.md) freezes separate reliability and selection-benefit gates. Each requires supported results in at least two of three restarts. Joint alpha .025 covers all confirmation rates; .025 covers nine paired differences through 18 discordance intervals. Ordinary nominated-family acceptance is also retained in the compact confirmation report. No automatic policy promotion follows either result.

## Changed files

| File | Purpose |
| --- | --- |
| [TowerFinalistRescreen.cs](../LL/tools/BalanceHarness/TowerFinalistRescreen.cs) | Frozen shortlist, complete evidence validation, fresh selection, full confirmation family and joint comparisons. |
| [TowerFinalistRescreenStudy.cs](../LL/tools/BalanceHarness/TowerFinalistRescreenStudy.cs) | Prepare/check/run/verify workflow, captured inputs, bounded execution and durable attempt journal. |
| [Program.cs](../LL/tools/BalanceHarness/Program.cs) | Additive CLI help, option validation and four opt-in commands. |
| [BalanceHarnessTowerFinalistRescreenTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerFinalistRescreenTests.cs) | 25 new selection, evidence, isolation, overflow, statistics, accounting and preparation cases. |
| [Execution plan](Tower-Finalist-Rescreen-Execution-Plan.md), this review and six active guides | Frozen comparison rules, current status, complete ledger and captured-executable commands. |

The six updated entry points are the coverage-replication handoff, discovery plan, discovery implementation guide, acceptance policy, search-strategy reset and harness README. The original [rescreen proposal](Tower-Finalist-Rescreen-Plan.md), historical reviews and sealed evidence packages retain their original bytes. Extensive pre-existing workspace edits were preserved.

## Verification

**170 relevant tests passed, zero failed or skipped**, including all **25 new cases**. Tests exercise complete ties including zero wins, changed/missing/extra/reordered evidence, frozen recipes and ancestry, seed-stage reuse, all-breach overflow, separate statistical gates, interrupted attempt accounting, preparation integrity and refusal of a second start. Existing generation, discovery-contract, v13, search-benchmark, bulk, evaluator, compact-publication and precision tests passed in the same run. Fixture tests remain separate from campaign accounting.

Commands run from the repository root:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:BuildProjectReferences=false -p:StaticWebAssetsEnabled=false
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerFinalistRescreenTests|FullyQualifiedName~BalanceHarnessTowerBossGenerationTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessTowerLoadoutCompositionTests|FullyQualifiedName~BalanceHarnessTowerSearchBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerCompactPublicationTests|FullyQualifiedName~BalanceHarnessTowerPrecisionTests'
dotnet TestResults/balance/tower-finalist-rescreen-20260913/executable/BalanceHarness.dll tower-finalist-rescreen-check --run TestResults/balance/tower-finalist-rescreen-20260913
```

The default restore/build path could not complete because this sandbox denied access to the user NuGet configuration and an unrelated API static-assets cache. The harness was built separately without restore; the test project then used existing unchanged project dependencies with static-assets generation disabled for that command. No repository configuration or permissions changed. The final test build had zero errors and five existing warnings in unrelated tests. An initial fixture assertion failed because it read an open journal; the corrected interruption test verifies the closed partial journal. Initial failure logs and the final passing TRX are retained in the [work package](../TestResults/balance/tower-finalist-rescreen-work-20260913/final-verification.json).

The [historical audit](../TestResults/balance/tower-finalist-rescreen-work-20260913/historical-audit.json) replays generation using saved outcomes only: **all six arms, 2,304 callbacks and 2,370 proposals match the complete archived generation graph**. All 12 original nominations match the historical selector, and all 96 top-32 ranks/recipe identities match the independent diagnosis. Gameplay assembly hashes and runtime identity match the calibration producer. The archive lists its own three generation reservations as excluded combat seeds; only the audit's in-memory exclusion metadata separates those roles for the stricter new protocol. No archived definition, generation input, measurement or file was altered, and no fight or seed was added by this audit.

The final readiness receipt checks the eight immediately preceding source/work/diagnosis packages, producing source, all applied content, Markdown links, final test counts and the 52-file prepared inventory. It separately seals implementation evidence in `work-files.json`; the prepared campaign remains unstarted and is not falsely sealed as a completed experiment. Historical workspace guards still describe their original source snapshots. Preserve those guards and use the new readiness receipt for this implementation.

## Frozen package and next action

The [prepared protocol](../TestResults/balance/tower-finalist-rescreen-20260913/protocol.json) freezes **57,344 fights / 5,400 seconds / 4 GiB / zero combat retries**. Its 52 files include the captured executable, complete content and a byte-identical copy of the execution plan. The new [ledger](../TestResults/balance/tower-finalist-rescreen-20260913/seed-ledger.json) preserves **476,054** historical reservations and adds **587** unused values, giving **476,641** distinct reservations. Preparation does not consume them as combat evidence.

Next, execute this prepared comparison once with its captured executable, then reconstruct the completed evidence:

```powershell
dotnet TestResults/balance/tower-finalist-rescreen-20260913/executable/BalanceHarness.dll tower-finalist-rescreen-run --run TestResults/balance/tower-finalist-rescreen-20260913
dotnet TestResults/balance/tower-finalist-rescreen-20260913/executable/BalanceHarness.dll tower-finalist-rescreen-verify --run TestResults/balance/tower-finalist-rescreen-20260913
```

These two commands were **not run in this implementation step**. Keep execution logs outside the prepared package, do not allocate the comparison again, and preserve any interruption without retry or budget extension. A complete negative result is useful evidence; it does not authorize another tuning cycle automatically.

The complete **17,821-recipe** precision decision remains **Pass**, with 18 supported viable teams. Local Kharad remains **Health 3.5366243328 / Power 4.4702934848**. This change introduces no migration, production configuration change, new dependency, deployment, restart, catalog promotion or later-floor campaign. Practical ownership, broader search coverage and floors 6–11 remain separate work.
