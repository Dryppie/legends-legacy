# Standalone retained-composition search: implementation and verification

The existing fixed-order retained comparator is now independently usable as `retained-composition-v1` through `tower-retained-composition-prepare`. This exposes the reference that outperformed block search on the failed synthetic quality gate. It introduces no new search strategy and establishes no combat-strength improvement. The block-search proposal stays closed, adoption Hold and V19 reliability Unresolved.

## Implementation

`TowerSuppliedCompositionSearch.cs` accepts the standalone version with exactly the `retained-composition` method. Preparation and validation choose a version-specific method list, and execution iterates the validated list. The paired v1/v2 policies still require both methods; their default remains v1. Adding a block method to the standalone definition, removing one from a paired definition, or claiming independent mode with supplied starts is rejected.

The comparator reuses its existing random namespace, one/two explicit canonical starts, fresh construction schedule, retention, neighborhood scan, mutation operators, proposal limits, fitness and shortlist logic. Ability order remains fixed ordinal. The standard stage policy retains the already implemented zero-win selection rule. No historical fitness is imported. A standalone shortlist naturally has one method's arms; it is not compared for equality with the old combined shortlist.

Changed production files are `LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs` (version/method dispatch), `Program.cs` (help, option allowlist and prepare command), `TowerCompositionSearch.cs` (fixed-order policy recognition), and `TowerBossDiscoveryRun.cs` / `TowerBossStudyMarkdown.cs` (explicit supplied-reference ancestry wording). Existing contract, generation, improvement and cost dispatch already use the shared version predicate; they need no edits. No gameplay service code changed.

The new `LL/tests/EssenceSystem.Tests/BalanceHarnessRetainedStandaloneTests.cs` covers parity and boundary behavior. Its 497,103-byte `Fixtures/retained-composition-parity.zip` contains 20 exact saved JSON inputs, compressed without changing their bytes: nine definition/report pairs from [the quality gate](../TestResults/balance/tower-supplied-quality-gate-20260916/results), and the two-start definition/report pair from [v2 scheduling verification](../TestResults/balance/tower-supplied-scheduling-v2-20260916/results). The [fixture amendment receipt](../TestResults/balance/tower-retained-standalone-verification-20260916/fixture-amendment.json) records entry origins and hashes. This packages existing evidence for regression tests; it is not another quality experiment.

README and the five current handoff documents now point here and to the new resource receipt. Historical reviews and sealed experiments retain their original contents.

## Verification

The [verification package](../TestResults/balance/tower-retained-standalone-verification-20260916) retains the frozen protocol, original-source backup, source snapshots, input hashes, exact commands, compiler logs, both test runs, complete first-run reports and audits. All 1,337 current gameplay-source hashes matched the pinned dependency build. Every current harness source compiled against those matching dependencies, followed by the focused test assembly. Both initial builds and the test-assembly rebuild completed with zero warnings or errors.

**13 distinct cases passed.** The first execution passed all 13; final review then found that its test loader depended on ignored local archives. A mechanical packaging amendment bundled those same inputs and changed only the test loader. The same 13 passed again, for 26 test-case executions and zero failures. All five production files remain byte-identical to the sealed candidate. The original candidate, first compiled test assembly, first results and amendment are preserved.

The assertions and [final audit](../TestResults/balance/tower-retained-standalone-verification-20260916/final-audit.json) establish:

- Exact complete-arm equality for 27 one-start arms across all nine quality cases plus three two-start arms: **4,304 proposals and 1,800 recorded synthetic evaluations**. Recipes, measurements, rejections, ordering, fitness and ancestry agree.
- Exact paired-v1 full-report equality: six arms and 144 scalar evaluations, including its existing shortlist.
- Canonical legal evaluated recipes, supplied-reference provenance, no order/block operators in standalone arms, exact versioned method lists, and unchanged paired preparation default.
- Attempt exhaustion remains incomplete with no shortlist; cancellation retains the existing cancelled status and empty proposal behavior.

Evaluators returned recorded fabricated measurements and used guards that throw if combat tracing is entered. These executions perform no fights, simulations of gameplay, seed allocation or scientific quality-gate rerun.

Verification used isolated offline `dotnet restore` / `dotnet build` projects recorded in `control/*-command.json`, then the repository-mandated runner:

```powershell
./build/run-tests.ps1 -NoBuild `
  -ArtifactsPath TestResults/balance/tower-retained-standalone-verification-20260916/current `
  -Filter 'FullyQualifiedName~EssenceSystem.Tests.BalanceHarnessRetainedStandaloneTests|FullyQualifiedName~EssenceSystem.Tests.BalanceHarnessSuppliedSchedulingParityTests'
```

The scope's commands completed; none remain blocked. A broad backend test suite, gameplay rebuild, native content admission, actual prepare invocation and combat run were outside this bounded zero-combat scope and were not executed. The CLI routing compiled; native end-to-end preparation is not claimed as tested. This evidence supports compatibility and standalone dispatch, not stronger independent performance or global optimality.

## Resources and preservation

The user's approval transferred 60 diagnostic seconds and 32 MiB from unused run allowance to engineering: engineering **660 seconds /256 MiB**, run **2640 seconds /736 MiB**, audit unchanged at **300 seconds /32 MiB**. Overall limits remain **7,980 seconds /5,804,916,736 bytes**. Verification is bounded to 60 seconds /32 MiB, including static preparation/documentation and closure allowances. The [completion receipt](../TestResults/balance/tower-retained-standalone-verification-20260916/completion.json) gives measured charges and cumulative balances; these are not fresh allocations.

Seven prior package manifests and unrelated dirty-file hashes are checked before sealing, along with scoped `git diff --check`. All **483,046 reservations** remain intact. The 891 authorized values and up to 6,912 fights from the closed comparison remain unused. V19's 253 required recipes and unexecuted confirmation are unchanged. There are no migrations, application configuration changes, gameplay-content changes or deployment implications; this is an opt-in offline harness command.
