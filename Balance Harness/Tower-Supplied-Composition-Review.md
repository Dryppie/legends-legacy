# Supplied composition search: implementation and synthetic verification

16 September 2026. Target: offline `LL/tools/BalanceHarness`. **Implemented; both compilations passed and all 35 synthetic tests passed.** The independent evidence audit reports `VerifiedZeroCombat`. This is an engineering result, not a combat-strength result. Adoption remains **Hold**.

## Implemented behavior

The opt-in `supplied-composition-block-v1` definition requires one or two explicitly named compatible references. It creates canonical ordinal compositions in a new definition, preserves the original source and evidence provenance, and discards any implication that archived fitness measures current strength. The existing identity, equipment, family and inventory checks still apply. Both methods receive identical starts, six initial fresh proposal opportunities and an independent legal-construction opportunity every fourth subsequent proposal. Candidate capacity must extend beyond that initial batch.

`retained-composition` adapts the existing retained-joint beam/anchor logic and non-order operators. `supplied-block` retains four fitness elites and up to four additional parties selected by farthest-first composition distance. Original supplied anchors remain available separately. It cycles two/three-Essence changes within one character, two whole-character changes within/across five-character subgroup boundaries, and donor mixtures over two, five or all owners. Donor mixtures require distinct surviving contributions. Completed children are validated atomically; an inventory swap may cross an illegal intermediate assignment without evaluating that intermediate.

Fresh proposals use the entire declared family-compatible pool, with no authored-core or role requirement and no prohibition on identical loadouts across characters. Construction has at most 32 candidate checks per emitted block/fresh proposal. Every emitted duplicate/rejection consumes one proposal opportunity; the definition bounds each arm to at most 64 evaluated parties and 256 opportunities. The archive trace records retained population, changed owners, parents and imported ancestry. It records the construction-check upper bound, not an observed count of internal checks.

The separately versioned zero-win selector orders complete selection evidence by wins, then arithmetic mean guardian health only for zero-win ties, then frozen discovery rank and ordinal ID. Positive-win ties retain their legacy tie-break. The staged policy is `tower-staged-zero-win-health-v1`; comparison `tower-discovery-refinement-comparison-v5` applies the selector to the existing fresh-first comparison. These are separate opt-ins: comparison v5 does not switch generation to supplied composition. Legacy defaults and trial serialization are preserved.

The CLI has an explicit supplied preparation entry point and the existing run/reconstruction paths dispatch by policy version. No real definition has been prepared or executed in this scope. A generic study merges its arms into a bounded shortlist; a future policy-strength comparison still needs separate per-method/root nominations, independent stage panels and a prospectively frozen confirmation family.

## Verification

The [protocol](Tower-Supplied-Composition-Protocol.md) pins current harness source, two synthetic test classes and two existing fixture helpers. Compilation uses captured gameplay dependencies and cached restore metadata. This avoids rebuilding unrelated dirty gameplay source within the remaining output budget. It does **not** establish parity with a full build of current gameplay or validate archived starts against live content.

The new tests cover atomic coordinated barriers, small-space exploration, repeated loadouts, subgroup placement, donor mixing, retention of a weaker distant family, inventory swaps, canonical admission, ancestry, deterministic reconstruction, candidate/proposal limits and cancellation. A full-kernel toy landscape compares both arms at fixed equal budgets and frozen construction roots; it reports scores and counts without requiring or claiming superiority. Selector cases cover complete health evidence, enumeration invariance, shared origins, stage isolation, legacy results, accounting and reconstruction using fabricated runtime callbacks. The production compact-health adapter has source review, but no real-archive integration test in this scope.

Both isolated `dotnet build --no-restore --disable-build-servers` commands passed with **zero warnings and errors**. The targeted run through `build/run-tests.ps1 -NoBuild -ArtifactsPath <package>/engineering-attempt-2/tests -Filter "FullyQualifiedName~BalanceHarnessSuppliedCompositionTests|FullyQualifiedName~BalanceHarnessZeroWinSelectionTests"` passed **35/35**: 12 supplied-search facts and 23 selector cases. The audit verified 385 source/dependency pins, the exact TRX class set, all **343 unrelated files** from the original dirty-checkout snapshot and the separately pinned final design review. Scoped `git diff --check` passed.

The full-kernel toy has 256 legal parties and known optimum 4. Each arm evaluates 24 parties with a 96-proposal cap. All six arms reached that optimum; importantly, it was already present in every shared initial batch. Thus this integrated case verifies scheduling and equal-budget mechanics, **not escape from a deceptive optimum or an advantage for block search**. The separate scripted-witness fixtures verify that atomic changes can cross two/three-Essence barriers and that a weaker distant party can be retained and produce a coordinated child. These are reachability/retention checks, not production success rates.

| Frozen construction root | Comparator best / proposals | Block best / proposals | Evaluations per arm | Retained score bands |
| --- | --- | --- | --- | --- |
| 17 | 4 / 39 | 4 / 26 | 24 | 1, 2, 4 |
| 31 | 4 / 38 | 4 / 27 | 24 | 1, 2, 4 |
| 47 | 4 / 47 | 4 / 27 | 24 | 1, 2, 4 |

Two engineering failures remain recorded. The initial freeze used a snapshot taken before the design review's final edit; its replacement checks the original 352 pre-task paths and pins the finalized review separately. The first test invocation was interrupted by the storage guard before producing a complete result. The [prospective storage amendment](../TestResults/balance/tower-supplied-composition-20260916/storage-amendment.md) reallocated unused bookkeeping reserves while keeping every cap unchanged; the one permitted retry used identical source, binaries, cases, seeds and assertions. No test result or search endpoint was tuned. No artifacts were deleted to create headroom.

The exact commands, pins, both failures, passing [TRX](../TestResults/balance/tower-supplied-composition-20260916/engineering-attempt-2/control/tests-2.trx), [audit](../TestResults/balance/tower-supplied-composition-20260916/engineering-attempt-2/audit.json) and final [completion receipt](../TestResults/balance/tower-supplied-composition-20260916/completion.json) are retained in the verification package.

## Changed files

- `TowerSuppliedCompositionSearch.cs` and `TowerZeroWinSelection.cs`: new bounded search kernel and shared selection rule.
- `TowerBossDiscoveryContract.cs`, `TowerBossGeneration.cs`, `TowerBossImprovement.cs`, `TowerCompositionSearch.cs` and `Program.cs`: admission, trace, canonicalization and explicit command/dispatch integration.
- `TowerBossStudyPolicy.cs`, `TowerBossStudy.cs`, `TowerBossStudyArchive.cs`, `TowerRefinementComparisonModel.cs` and `TowerRefinementComparisonRun.cs`: versioned selection, verified health adapter and reconstruction binding.
- `TowerBossDiscoveryRun.cs` and `TowerBossStudyMarkdown.cs`: accurate supplied-policy report labels.
- `BalanceHarnessSuppliedCompositionTests.cs` and `BalanceHarnessZeroWinSelectionTests.cs`: synthetic mechanics, contract and selector coverage.
- Harness README, zero-win plan, this review/protocol and the new verification package: current handoff and reproducible bounded evidence.

These harness/test paths are under `LL/tools/BalanceHarness` and `LL/tests/EssenceSystem.Tests`. All unrelated pre-existing dirty files are checked against their pre-implementation hashes during audit. No gameplay, migration, configuration or deployment change is part of this work.

## Resource and scientific boundary

Starting cumulative usage was 4,049.7802723556424 / 4,380 diagnostic seconds and 4,718,720,963 / 4,731,174,912 logical output bytes. This package permits at most 300 additional seconds and 12,000,000 additional bytes inside those unchanged limits; its final receipt is authoritative. Before final publication/sealing, measured plus conservative inspection charges totaled 46.687 seconds. The amended accounting retains a 512 KiB shared TRX minimum (or actual size if larger), 128 KiB publication reserve and 128 KiB closure guard; all actual package, temporary, source and document bytes still count. Failed engineering attempts remain charged; no time or output cap was extended.

No combat, real preparation, new balance-seed allocation or historical study rerun occurred. Preserve all **483,046 reservations**, including V19's 512 unused values. V19 still has 253 recipes and no confirmation; its reliability remains Unresolved. Later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. The next substantive step is a separately frozen, information-matched combat comparison after admitting starts against a single declared content version. That future workload has not been prepared or authorized here. There are no migrations, configuration changes or deployment implications from these offline opt-ins.
