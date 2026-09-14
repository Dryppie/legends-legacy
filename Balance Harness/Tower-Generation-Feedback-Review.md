# Generation-feedback implementation and comparison review

13 September 2026. **Complete and verified: reliability Fail 0/3; adoption Hold.** 55,296 fights completed with zero retries. The requirement remains at least two of three restarts.

Keep v14 experimental and leave the default unchanged. This work delivered and measured the feedback correction, but it found no new competitive team and did not satisfy the unchanged 2/3 reliability requirement.

## What changed and why

The [frozen plan](Tower-Generation-Feedback-Plan.md) tests one correction supported by the preceding saved-history diagnosis: reassess promising candidates while generation is still using them as parents. Prior selected observations showed weak 8-fight leaders receiving repeated parent use while candidates initially scoring 0/8 later won 78/512. Those observations motivated the experiment; they did not establish that feedback would solve generation.

The explicit v14 arm evaluates 320 candidates on eight discovery seeds and probes sixteen candidates on 32 separate training seeds, for 3,072 fights. The unchanged v13 comparator evaluates 384 × 8, also 3,072. Both retain the same first 96 candidates, random-stream initialization, operators and player budget. Four frozen checkpoints at 96/160/224/288 reassess four unprobed leaders each; pooled 40-trial scores then inform parents, exploration and loadout-library rank. Each raw 8/32-trial measurement remains available. Both arms get identical 32 × 64 independent screening and 512-trial confirmation.

## Measured result

| Restart | Screened v13 wins/512 | Feedback wins/512 | Feedback adjusted rate interval | Paired improvement interval (percentage points) | Reliability |
| --- | ---: | ---: | --- | --- | --- |
| 1 / seed 875143762 | 0 | 0 | 0.00%–2.30% | -1.96–1.96 | Fail |
| 2 / seed 1247085600 | 0 | 0 | 0.00%–2.30% | -1.96–1.96 | Fail |
| 3 / seed -1258158394 | 0 | 0 | 0.00%–2.30% | -1.96–1.96 | Fail |

Every original candidate evaluation, every feedback probe and all 192 screened candidate slots recorded zero wins. All twelve generated nominees then won 0/512. Each feedback primary's adjusted upper bound is only 2.30%, below the 10% target; viability and comparator improvement both fail, while the historical-anchor check passes because that anchor also recorded zero wins. The 48-recipe family Pass comes from eight supported viable external controls. It is not a generation success. This bounded result provides no support for adopting this feedback allocation; it does not establish that feedback can never help another search trajectory.

The historical anchor won **0/512**; the strongest preceding control won **160/512**. The complete **48-recipe** family returned ordinary/joint **Pass/Pass**, with **8** supported viable recipes and **0** observed confirmation ceiling breaches. All 36 prior recipes remain external controls. Each feedback primary also has a separately reported paired comparison against the strongest prior control; that comparison does not replace the fixed reliability gate.

The gate combines supported viability (rate lower bound ≥10%), supported paired improvement over the same-restart screened v13 primary (difference lower bound >0), and recovery relative to the fixed historical anchor (difference lower bound ≥−10 percentage points). Rates use joint alpha .025 over the full confirmation family; nine paired comparisons use .025 over eighteen discordance intervals. Independent Python NormalDist calculations reproduce every rate and paired interval within 1e-8. These approximate Wilson intervals cover this frozen selected family, with no pooling, lifetime repeated-study or near-optimality claim.

## Retained work and verification

The [results](../TestResults/balance/tower-generation-feedback-work-20260913/results.json) retain **2,112 original candidate evaluations**, **1,796 distinct complete recipes**, **48 feedback probes**, all **192 screening slots** and all **48 confirmation recipes**. Every original nomination, screened nominee and prior control is included. Zero-win recipes are retained. The [seed-free export](../TestResults/balance/tower-generation-feedback-work-20260913/exports/saved-builds.md) and [complete discovery recipes](../TestResults/balance/tower-generation-feedback-20260913/all-evaluated-recipes.json) preserve provenance. The full discovery report retains all feedback rounds and ancestry.

Execution used **1895.88 seconds** and retained **1,412,956,669 campaign bytes** (1.32 GiB). Caps were 63,488 fights, 5,400 seconds and 4 GiB, with zero retries, resumes, extensions or diagnostic combat replays. The durable start/completion journal matches all 55,296 completed fights. Preparation reserved **619 fresh values** and preserved all 476,641 prior reservations; the [latest ledger](../TestResults/balance/tower-generation-feedback-20260913/seed-ledger.json) now holds **477,260** distinct values. Exclude every array in future studies.

**399 relevant tests passed**, including 28 new feedback/benchmark cases. The final three kernel cases passed again after removing a nullable-warning assertion pattern. Coverage includes unchanged comparator and initial population, changed descendants, complete checkpoint sampling, partial-round failure, seed separation, complete screening, tampering, overflow retention, exact two-of-three reliability, interrupted accounting and refusal of a second start. The six-arm historical v13 search reconstructed all **2,304 saved evaluations** and its original shortlist exactly with zero combat and unchanged gameplay assemblies.

| Files | Purpose |
| --- | --- |
| [TowerGenerationFeedback.cs](../LL/tools/BalanceHarness/TowerGenerationFeedback.cs) | Fixed feedback checkpoints, complete probe validation, pooled training score and reconstruction. |
| [TowerFeedbackBenchmark.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmark.cs), [TowerFeedbackBenchmarkRun.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmarkRun.cs) | Frozen paired experiment, identical screening, complete confirmation, statistical decision, captured execution and no-combat verification. |
| [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs), [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs), [TowerCompactDiscovery.cs](../LL/tools/BalanceHarness/TowerCompactDiscovery.cs) | Opt-in scheduling, budget accounting, feedback evaluation callback and saved-trial reconstruction. Null optional fields preserve historical serialization. |
| [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs), [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs), [TowerLoadoutComposition.cs](../LL/tools/BalanceHarness/TowerLoadoutComposition.cs), [Program.cs](../LL/tools/BalanceHarness/Program.cs) | Existing v13 mechanics for the new version and additive prepare/check/run/verify commands. |
| [Generation tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerGenerationFeedbackTests.cs), [benchmark tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerFeedbackBenchmarkTests.cs) | New behavior, failure, evidence, budget and statistical regressions. |
| This review, the frozen plan and six active Markdown guides | Current outcome, commands, exclusions, saved builds and next boundary. Historical reviews and plans remain unchanged. |

Executed builds and backend tests used the repository wrapper:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:BuildProjectReferences=false -p:StaticWebAssetsEnabled=false
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerLoadout|FullyQualifiedName~BalanceHarnessTowerFinalistRescreen|FullyQualifiedName~BalanceHarnessTowerFeedback|FullyQualifiedName~BalanceHarnessTowerGenerationFeedback|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerSearchBenchmark|FullyQualifiedName~BalanceHarnessTowerPrecision|FullyQualifiedName~BalanceHarnessTowerStaged'
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
dotnet TestResults/balance/tower-generation-feedback-20260913/executable/BalanceHarness.dll tower-feedback-check --run TestResults/balance/tower-generation-feedback-20260913
dotnet TestResults/balance/tower-generation-feedback-20260913/executable/BalanceHarness.dll tower-feedback-run --run TestResults/balance/tower-generation-feedback-20260913
dotnet TestResults/balance/tower-generation-feedback-20260913/executable/BalanceHarness.dll tower-feedback-verify --run TestResults/balance/tower-generation-feedback-20260913
git -c core.safecrlf=false diff --check
```

The auxiliary audit project's first restore was denied access to the user NuGet configuration. It succeeded with a process-local APPDATA directory beneath TEMP and an empty package-source configuration in the ignored work directory. Backend verification used the established no-restore build and disabled unrelated API static-assets generation for the test build. No repository configuration or permissions changed. All required verification completed; the final test build had only five existing warnings in unrelated tests. Logs and producing-source snapshots are retained in the work package.

For future reconstruction, use only the captured `tower-feedback-verify` command and `analyze.py verify`; they run no new fights. The completed package cannot run or prepare again. The [completion receipt](../TestResults/balance/tower-generation-feedback-work-20260913/final-verification.json) seals source, tests, results, exports and documentation; preservation checks verified **11 preceding packages / 145,477 files**.

## Decision and next boundary

Keep v14 experimental and leave the default unchanged. This work delivered and measured the feedback correction, but it found no new competitive team and did not satisfy the unchanged 2/3 reliability requirement.

The next optimizer work should target candidate construction and the diversity of retained parents. Compare the pre-win lineages from the saved successful v13 histories with these all-zero restarts, measuring which complete character loadouts entered the library and where parent diversity was lost. Use that evidence to choose one change to candidate creation before reserving another comparison. Keep the player budget, Kharad setting and 2/3 reliability requirement fixed. This completed experiment receives no extension or follow-on allocation.

A [post-hoc coverage check](../TestResults/balance/tower-generation-feedback-work-20260913/coverage-diagnostic.json) records 1,911–1,938 distinct ordered character loadouts per comparator arm and 1,693–1,704 per feedback arm. None exactly matched a loadout from the frozen strongest control. Exact overlap is not required for viability and does not establish causation or justify copying that control into independent generation. This descriptive check is a starting point for the broader lineage comparison; it changed no selection, gate or combat allocation.

Kharad and the complete player/gear/Essence budget remain fixed. The prior **17,821-recipe precision Pass**, with 18 supported viable teams, remains unchanged and separate from this new selected family. No gameplay source, content, catalog, default policy, migration, production configuration, service or deployment changed. Practical acquisition and floors 6–11 remain open.
