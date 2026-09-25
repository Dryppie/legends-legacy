# Adaptive racing generation and archive integration

Implemented 23 September 2026, following the [assessment](Tower-Search-Algorithm-Independent-Assessment.md) and [frozen kernel increment](Tower-Batch-Racing-Kernel-Implementation.md). The new policy is **`tower-adaptive-beam-racing-v1`**. It implements bounded adaptive proposal generation, the existing 528-evaluation racing schedule, and an adapter for a captured `TowerLoadoutArchive` owned by an admitted runner. No native combat campaign was executed to develop this increment.

## What is implemented

| File | Responsibility |
| --- | --- |
| [TowerAdaptiveRacing.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacing.cs) | Separate plan/report version; explicit development benchmark; batch provenance; adaptive execution and reconstruction |
| [TowerAdaptiveRacingGenerator.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacingGenerator.cs) | Conditional legal construction, deterministic random streams, parent selection, owner schedules and bounded attempts |
| [TowerBatchRacing.cs](../LL/tools/BalanceHarness/TowerBatchRacing.cs), [contract](../LL/tools/BalanceHarness/TowerBatchRacingContract.cs) | Shared allocation state machine and validation; the frozen v1 wrapper keeps its original contract |
| [TowerAdaptiveRacingNative.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacingNative.cs) | Prepared-input/trial binding, durable freezes and logical charges, bounded evidence storage, archive verification and read-only commands |
| [TowerLoadoutArchive.cs](../LL/tools/BalanceHarness/TowerLoadoutArchive.cs) | Internal access to the archive's captured scope, limit, location and input materializer; existing evaluation/cache behavior is unchanged |
| [Program.cs](../LL/tools/BalanceHarness/Program.cs) | Dispatch for the new check and verify commands |
| [BalanceHarnessAdaptiveRacingTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessAdaptiveRacingTests.cs) | Literal outcome, lineage, legality, archive and command tests with a native-combat guard |

`TowerAdaptiveRacing.RunAsync(plan, evaluator, token, checkpoint)` owns a deep copy of its plan. It generates nine first-wave challengers, measures and ranks that wave through the shared kernel, then generates eight new challengers using the actual four-member challenger beam. Both batches are frozen before their respective fresh screens. References remain separate protected members. The final output is still `Evaluation.RawSelectedId`, with no confirmation or adoption claim.

## Proposal policy

The plan names `BenchmarkReferenceId` explicitly. This controls proposal parent sampling; it does **not** alter `Scope.Stages.SelectionPrimaryReferenceId`, which continues to govern the existing positive selection-tie rule.

For each nonfresh attempt, parent sampling uses 50% benchmark, 25% uniformly among the other references, and 25% uniformly from the beam. Before a beam exists, that last share is redirected uniformly across all references. Reference recipes supply prior knowledge; historical outcomes are not observations in the new search.

| Opportunity cycle | Wave one | Wave two |
| --- | ---: | ---: |
| Single legal replacement | 4 | 3 |
| Coordinated edits: one unguided, one guided pair | 2 | 2 |
| Partial owner rebuild | 1 | 1 |
| Complementary recombination | 1 | 1 |
| Fresh legal team | 1 | 1 |

These are **attempt opportunities**, not guaranteed accepted-candidate quotas. Every rejection advances the opportunity and owner counters. The cycle repeats until nine/eight unique teams are available or 128 attempts are exhausted. Each attempt permits at most 32 construction checks. A short batch returns `Incomplete`; it is not evaluated or silently replaced with a smaller experiment. Already completed earlier-wave evidence remains available.

Parent choice, donor choice, owner ordering and replacement choices use separate deterministic `StableRandom` seed namespaces under the supplied root. Combat-panel values do not feed those streams. Each operator/parent pair has a root-shuffled owner order whose cursor advances on every attempt, including duplicate or failed attempts. The archive records requested/effective operator, parent source and identities, scheduled and actually changed owners, construction checks, realized replacement distance, interaction hypothesis, fallbacks and rejection reason.

Construction enforces the existing canonical per-owner representation, family uniqueness and global owned-copy limits:

- A single edit samples from currently legal replacements, excluding the owner's original assignments.
- An unguided coordinated edit changes one assignment on two or three owners. It frees all selected copies before filling any destination, permitting legal transfers under tight ownership budgets.
- A guided attempt removes two assignments on one owner and selects a legal enabler/consumer pair from the frozen mechanics catalogue. If none fits, it records an `unguided-pair` fallback on those same freed assignments. Mechanics are hypotheses, not strength guarantees.
- A five-Essence partial rebuild retains two or three assignments. Other supported budgets retain half, rounded down, with at least one retained assignment. Actual replacement distance must equal the intended edit count.
- Recombination transfers a nonempty proper subset of differing owner loadouts between distinct parents, at the same owner positions. It preserves actor, equipment and subgroup destinations. Fewer than two differing owners triggers an explicitly recorded partial-rebuild fallback. Parent clones and all previously seen recipes are rejected before evaluation.
- Fresh construction samples a whole legal team without a mandatory role or mechanic core.

The kernel's ranking, diversity reserve, common current-wave evidence, fresh panels, protected references, nomination order and final tie semantics are shared with frozen v1. A complete run remains `96 +56 +120 +56 +200 = 528` logical evaluations. Changing adaptive outcomes can change the second batch; it cannot change the first batch retrospectively.

## Native archive adapter

`TowerAdaptiveRacingNative.RunAsync(plan, archive, maximumEvidenceBytes, checkLimits, token)` is an integration API for an **already admitted and exclusively owned** `TowerLoadoutArchive`. It requires:

1. The exact new algorithm version, an empty archive with a 528-trial limit, matching saved scope and unused `recipes`/`battles` directories.
2. The captured content, settings and producing executable/runtime identity bound by the plan. Captures contain data files, so the adapter uses saved settings instead of re-reading a live or absent `appsettings.json`.
3. Mechanics reconstructed from captured production content matching the plan's frozen mechanics.
4. A caller-owned resource check and cancellation token, plus an explicit byte allowance for the new `racing` evidence directory. The enclosing runner continues to own history reservation, physical attempt accounting, time/storage ceilings, process lifetime and final archive publication.

Before each battle, the adapter materializes the expected input through the archive's actual runner and derives the full input hash/cache key. The returned trial must match the requested role, scenario hash, seed, ordinal and expected input identity. Battle schema, scenario/seed identity, outcome, duration and required survivor data are checked before deriving ranking telemetry. Cache reuse is rejected. The adapter makes no additional fights to resolve failures.

The new evidence directory contains the plan, two batch freezes, five panel freezes, a durable logical-charge journal, a prepared-input journal and the terminal search report. Files and journal appends flush to disk before dependent dispatch. The evidence allowance is enforced before each write; the enclosing resource callback is consulted before work and writes. Existing evidence prevents retry/resume. A failed resource check during closeout may leave a partial journal and propagate an exception; it does not publish successful evidence or refund attempts.

`ChargedEvaluations` remains a conservative logical counter, including attempts that fail before combat. The archive trial ledger records completed returned battles. These must not be substituted for the enclosing runner's physical-start/physical-completion receipts. The new adapter does not implement a second process owner or seed registry.

After the enclosing runner publishes `files.json`, `TowerAdaptiveRacingNative.VerifyAsync(output)` checks the archive inventory, captured scope/content/runtime, exact recipe/battle inventory, regenerated input hashes, battle-derived outcomes, durable journals and freezes, and deterministic adaptive reconstruction. Verification executes **zero fights**. It verifies consistency and bindings of saved evidence; it does not independently rerun the combat engine or cryptographically prove that a battle occurred.

## Commands and integration boundary

The new read-only commands are:

```text
BalanceHarness tower-adaptive-racing-check <plan.json>
BalanceHarness tower-adaptive-racing-verify <archive-directory>
```

`check` validates the complete plan without generating candidates or invoking native preparation. It reports `admissionRequired: true`. `verify` verifies a completed archive under its producing runtime without combat. There is intentionally no direct `tower-adaptive-racing-run` command that bypasses the existing resource owner. Existing practical-search dispatch and historical scientific protocols have not been silently redirected to this policy.

The subsequent [prospective comparison implementation](Tower-Adaptive-Racing-Comparison-Implementation.md) adds the versioned protocol, fresh paired allocation and owned-runner dispatch using the shared adaptive kernel and report authenticator. It also adds independent held-out evaluation and native/Python reconstruction. No native combat campaign has been run. The pending 52,000-fight confirmation and its resource amendment are untouched.

## Verification

Verification runs through `build/run-tests.ps1` using isolated artifacts at `TestResults/adaptive-racing-build-20260923`. The new fixtures cover adaptive feedback changing second-wave parentage, deterministic reconstruction, all operator families, clone fallback, root-shuffled owners, tight-copy transfers, legal five-Essence budgets, attempt exhaustion, changed provenance, input mutation, prepared-input/report mismatches, durable pre-dispatch journals, resource/storage failure, no-resume behavior and the check command. Every new test installs a guard against native preparation/combat.

**224 tests passed, 0 failed or skipped:** 39 new adaptive/archive cases, 47 frozen-kernel cases, and 138 legacy allocation, reference, selection, preset and exploration regressions. The build succeeded with zero errors; its 17 warnings were outside the new adaptive/kernel code. See the [TRX receipt](../TestResults/adaptive-racing-tests-20260923.trx) and [build/test log](../TestResults/adaptive-racing-tests-20260923.log).

The required command was:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAdaptiveRacingTests|FullyQualifiedName~BalanceHarnessBatchRacingTests|FullyQualifiedName~BalanceHarnessEvaluationAllocationTests|FullyQualifiedName~BalanceHarnessThreeReferenceTests|FullyQualifiedName~BalanceHarnessThreeReferenceTieTests|FullyQualifiedName~BalanceHarnessIncumbentSelectionTests|FullyQualifiedName~BalanceHarnessPracticalPresetTests|FullyQualifiedName~BalanceHarnessReferenceExplorationTests' -ArtifactsPath 'TestResults/adaptive-racing-build-20260923'
```

Verification used permission-enabled access to the existing NuGet configuration/cache. No required verification command remains blocked. Whitespace and local document links were also checked. The passing frozen-kernel and legacy regression cases are compatibility evidence, not evidence of improved search quality. No migration, configuration change, deployment or gameplay-value change is required by this implementation.
