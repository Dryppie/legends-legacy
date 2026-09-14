# Kharad precision resolution: Pass

Completed 13 September 2026 under the separately frozen [precision plan](Tower-Kharad-Precision-Resolution-Plan.md). The complete **17,821-recipe** composite assessment is **Pass**. The sole fresh team, `team-db3598435c3754b58e2b6d9c10ff7be2`, won **339/1,000 (33.90%)**, with adjusted interval **30.27121%–37.72843%**. This fixed study used exactly **1,000 fresh attempts and completions**, zero retries, extensions or diagnostic fights.

The original [complete-family study](Tower-Kharad-Full-Family-Review.md) remains **Inconclusive** at its original allocation. Its 489,168 completed fights are retained source evidence; none were rerun or pooled into the fresh sample. Its 75/192 observation for the refreshed team remains visible in that historical package.

## Complete-family decision

| Final component | Original comparison family | Final retained recipes | Samples / recipe | Alpha |
| --- | ---: | ---: | ---: | ---: |
| Resolved source first-stage cells | 17,494 | 17,460 | 24 | .025 |
| Tightened source second-stage cells | 361 | 360 | 192 | .0125 |
| Entire selected fresh family | 1 | 1 | 1,000 | .0125 |

Every source recipe appears exactly once in the final decision. The source's original 327 anchors and 34 advancing first-stage recipes reconstruct to the same 361-team second stage. All second-stage bounds were tightened before selecting the fresh family. The largest retained second-stage upper is **49.31503%**; the resolved first-stage upper remains **49.19684%**. The final family has **18 supported viable teams** and **0 unresolved upper bounds**. The maximum final upper is **49.31503%**. Any source or fresh observed >50% rejects acceptance; observed breach present: **false**.

The frozen design printed the retained maximum as 49.31504%. The verified unrounded value is `0.4931503134979286`, which rounds to **49.31503%** at five decimal places. This corrects the displayed rounding; all decisions use unrounded bounds.

This uses approximate Wilson intervals with a total alpha allocation of .05. It supplies no lifetime repeated-study guarantee, evidence about unsearched recipes, independent search reliability or practical acquisition. Descriptive rates from different stages are not a paired superiority test.

## Implementation and verification

Two additive harness files implement the separately versioned contract and runner: [`TowerPrecisionBalance.cs`](../LL/tools/BalanceHarness/TowerPrecisionBalance.cs) and [`TowerPrecisionBalanceRun.cs`](../LL/tools/BalanceHarness/TowerPrecisionBalanceRun.cs). Existing staged policies and their producing source remain unchanged. The evaluator binds the source definition, both evidence families, source manifest, entire seed ledger, complete fresh selection, fresh schedule and producing execution. The runner reconstructs every source compact batch and original report, materializes the fresh party, and rejects retries or a second execution in the same output directory. Gameplay assembly hashes and runtime must match the source; only the harness assembly changes.

The [precision tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerPrecisionTests.cs) cover independent normal-quantile fixtures, the 460/461 ceiling boundary, preservation of all recipe bounds, no pooling, source breaches, dropped or replaced evidence, unused ledger reservations, fixed-sample identity, archive reconstruction, tampering and verification without combat. **142 relevant tests passed**, zero failed or skipped.

Commands from the repository root:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessTowerPrecisionTests|FullyQualifiedName~BalanceHarnessTowerStagedTests|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerBulk|FullyQualifiedName~BalanceHarnessTowerBalance'
dotnet TestResults/balance/tower-kharad-precision-20260913/executable/Study.dll verify
```

The captured executable reconstructed both source stages and the fresh campaign in **427.30 seconds**, with **zero new combats**, and checked that verification changed no files. The [independent Python audit](../TestResults/balance/tower-kharad-precision-work-20260913/analysis.json) checked every family row, stage allocation, interval, ledger reservation, fresh compact outcome, attempt journal and final manifest. Execution took **464.08 seconds**, including source reconstruction, within the **1,800-second** cap. The new study package used **114,408,394 bytes** (109.11 MiB), within **1 GiB**. The previous source packages remain separately retained references.

The initial standard build could not read the user NuGet configuration in the sandbox. The no-restore build used the existing dependency assets successfully. The package-free helper used an empty NuGet source list and a temporary APPDATA directory. One initial test fixture contained a mistyped expected lower bound; an independent normal-quantile calculation corrected it before the passing run and protocol freeze. The initial logs and failed-test receipt are preserved. No required verification remains blocked. Existing unrelated compiler warnings remain.

## Evidence and reuse

- [Frozen protocol](../TestResults/balance/tower-kharad-precision-20260913/protocol.json), SHA-256 `8aa7fe9f10677414737f933f405a0e523fc8cde43eaa9dc792d1112c0184c4a1`.
- [Final study manifest](../TestResults/balance/tower-kharad-precision-20260913/final-files.json), SHA-256 `16dc135d2c7ce7de6a883cffbfdcd0aa365c1eafca0ad6709455d1773cddf403`.
- [Composite assessment](../TestResults/balance/tower-kharad-precision-20260913/campaign/assessment.json) and [readable full-family report](../TestResults/balance/tower-kharad-precision-20260913/campaign/assessment.md).
- [Exact refreshed seed-free recipe](../TestResults/balance/tower-kharad-precision-20260913/recipe.json), [all 361 prior selected builds and origins](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/saved-builds.md), and [all 17,821 recipes with sources](../TestResults/balance/tower-kharad-full-family-20260913/family.json.gz).
- **[Authoritative 476,054-reservation ledger](../TestResults/balance/tower-kharad-precision-20260913/seed-ledger.json)**: all 475,054 prior reservations plus exactly 1,000 fresh seeds. Exclude every array, including unused historical reservations.
- [Application decision](../TestResults/balance/tower-kharad-precision-work-20260913/application-decision.json) and [final verification receipt](../TestResults/balance/tower-kharad-precision-work-20260913/final-verification.json).

Keep the sealed source and new study directories together for reconstruction. The captured `verify` command reads both; it returns zero for a successful integrity reconstruction independently of the statistical outcome. Never rerun `prepare` or `run` on the completed package. The reusable APIs are `TowerPrecisionBalance.Select/Validate/Evaluate` and `TowerPrecisionBalanceRun.VerifySource/RunAsync/VerifyAsync`; the existing two-stage CLI retains its original meaning.

## Application and next work

**Ready for checked local application; no live values changed in this study.** Every required final upper bound is at most 50% and the fixed cohort has supported viability. The measured-family confidence blocker is closed. Perform checked local application of the accepted candidate with exact content diff and bounded parity verification, retaining the scoped acceptance and separate search-reliability failure.

Candidate content remains isolated at **Health 3.5366243328 / Power 4.4702934848**. Live Kharad remains **Health 3.04881408 / Power 3.85370128**. The earlier candidate independent-search result remains **Fail 0/3** and live-setting reliability remains **Pass 2/3**. Keep this measured-family acceptance separate from server-wide strongest-player readiness. Reliable independent search at the candidate, practical ownership and floors 6–11 remain open. No defaults, catalogs, gameplay files, migrations, environment configuration or deployments changed.
