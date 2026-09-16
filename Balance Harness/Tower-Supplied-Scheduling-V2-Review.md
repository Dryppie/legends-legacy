# Supplied composition scheduling v2

16 September 2026. Target: offline `LL/tools/BalanceHarness`.

**The nine-parent operator starvation is fixed in opt-in `supplied-composition-block-v2`.** Each parent now rotates through Essence, character and donor blocks on its own successive visits. V1 remains supported and remains the default for existing preparation calls. All **33 distinct verification cases passed** across 34 executions: the initial run passed 32/33; one incorrect new legality assertion was corrected and that case alone passed on retry. Production code did not change after the first run.

The controlled nine-parent landscape now improves from 6 to 9 across all three frozen label mappings. V1 still scores 6 in that condition. This verifies the scheduling repair; it does not establish stronger combat teams or reopen the previous failed comparison.

## Design and integration

The existing parent choice remains `eligible[mutation % eligible.Count]`, with the same retained population and appended supplied anchors. V2 keeps an arm-local visit count keyed by canonical parent party ID. It chooses the operator using `visit % 3` and the operator ordinal using `visit / 3`, then advances the visit before construction. Rejected and duplicate proposals consume visits. Reordering, temporary eviction and return do not reset a parent's count; a new arm starts empty. At most the arm's 64 measured parties can become keys.

Operator ordinals therefore continue cycling the existing two/three-Essence replacement sizes, subgroup preferences and donor-block sizes. No fitness, inventory, ability-order, population-size, fresh-frequency, candidate-budget or selection rule changed. The v1 random stream namespace is intentionally shared with v2, preserving the fresh stream and retained comparator; reports retain their actual policy version.

The new policy is registered in preparation, definition/input validation, common execution dispatch, ancestry validation, composition canonicalization and discovery/study reporting. Select it explicitly with `--policy-version supplied-composition-block-v2` on `tower-supplied-composition-prepare`. Omitting that option still prepares v1. Unknown policy versions are rejected. No real preparation or native admission command was invoked in this scope.

This schedule guarantees operator rotation **on successive visits**, not that every retained parent receives three visits under a changing population and finite budget. A parent's first visit always uses Essence-block, so a short run can emphasize Essence-blocks before reaching character/donor blocks. Whether this allocation helps overall search must be measured separately; the fix does not claim parent-selection fairness or guaranteed convergence.

## Evidence

The [prospective protocol](../TestResults/balance/tower-supplied-scheduling-v2-20260916/protocol.md) froze the schedule, unchanged controlled landscape, label mappings, budgets and verification before execution.

| Controlled condition, all three label maps | V1 diverse | V2 diverse | V2 elite-only |
| --- | ---: | ---: | ---: |
| Eight eligible parents | 9 | 9 | 6 |
| Nine eligible parents | 6 | 9 | 6 |

Each arm receives 30 prescribed mutation opportunities and the same initial scored archive. In v2, the weaker non-anchor B produces a strict improvement at mutation 4 in both conditions. The eight-parent case also reaches the second target at mutation 28. The scored archive remains fixed; one-owner character/donor proposals reject, and fresh construction is outside this conditional fixture. These limitations remain identical to the prior diagnostic.

The [independent audit](../TestResults/balance/tower-supplied-scheduling-v2-20260916/audit.json) verifies **24 complete traces / 720 opportunities**, including all 12 unchanged v1 traces and 12 v2 traces. It reconstructs parent selection, per-parent operator/ordinal sequences, exact replacement counts, canonical recipes, duplicate/evaluation accounting and strict improvements.

The complete six-arm v1 report—**144 fabricated evaluations**, including all proposals, ancestry and shortlist—matches the previously sealed report exactly. The new v2 full-kernel run completes six arms at the same allocation. Its three retained-comparator arms are identical to v1; all 36 block-parent visits follow the new rotation, and repeated execution produces an identical report. Fixed 8/9/10-parent tests cover nine visits per parent, including all three operator ordinals. Other cases cover population reordering, eviction/return, anchor changes, version admission, provenance, rejected/duplicate attempts, finite exhaustion and cancellation.

The first failure came from a new assertion that attempted to validate every non-null proposal as legal. Existing comparator proposals intentionally retain rejected invalid recipes. The correction requires evaluated/duplicate recipes to be legal and rejected invalid recipes to fail validation and remain absent from measurements. The original test source, test build, TRX and failure log remain archived; only this one case was retried. The landscape, scheduler, seeds, budgets and success criteria were not changed. The earlier **52/53** failed search-quality gate and previous packaging overrun remain preserved in their original packages.

## Changed files and verification commands

- `TowerSuppliedCompositionSearch.cs`: versioned preparation, per-parent scheduling and actual-version reports.
- `Program.cs`: explicit CLI option and help; `TowerBossDiscoveryContract.cs`, `TowerBossGeneration.cs`, `TowerBossImprovement.cs`, `TowerCompositionSearch.cs`: version validation, provenance, dispatch and canonicalization.
- `TowerBossDiscoveryRun.cs`, `TowerBossStudyMarkdown.cs`: supplied-team reporting for v2.
- `BalanceHarnessControlledRetentionTests.cs`: shares the unchanged landscape with an explicit scheduler choice; `BalanceHarnessSuppliedSchedulingTests.cs`: 13 new coverage, integration and boundary cases.
- Harness README, this review and the new evidence package: handoff, frozen source, full traces, commands and resource receipts.

The whole harness and isolated test assembly built with **zero warnings/errors**, using the verified current-gameplay dependency DLLs from the prior full build. All **1,337** pinned gameplay source files still match; gameplay was not rebuilt. Backend verification used `build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-supplied-scheduling-v2-20260916/current` with the supplied-composition, controlled-retention, v1 parity and v2 scheduling class filters. The retry used the exact failed method filter through the same wrapper. Exact build/test commands and both TRX files are in `control`. The independent audit and scoped `git diff --check` passed. No requested verification remains blocked.

The new scope is capped at **90 diagnostic seconds / 64 MiB**, within the remaining approved cumulative and 600-second engineering allocations. Code snapshots were compressed directly to avoid the prior duplicate-fixture storage issue. The [completion receipt](../TestResults/balance/tower-supplied-scheduling-v2-20260916/completion.json) records exact charges, balances and preservation checks for three predecessor packages and unrelated dirty work. All **483,046** reservations remain intact; **891 approved fresh values and up to 6,912 fights remain unused**.

Next is a prospectively frozen end-to-end synthetic quality gate that accepts improvement through any legal route at equal budgets, followed by matching native admission before reconsidering a new combat proposal. The controlled fixture alone cannot authorize that conclusion. Adoption remains Hold; V19 reliability remains Unresolved. No gameplay content, persistent configuration, migration or deployment changed.
