# Frozen batch racing kernel

Implemented 23 September 2026 as the first increment from the [independent assessment](Tower-Search-Algorithm-Independent-Assessment.md). Target: the offline Balance Harness. Version: `tower-frozen-batch-racing-v1`.

This increment implements the evaluation contract and deterministic allocation state machine over **supplied, already constructed teams**. Both challenger batches are fixed before execution. It is suitable for literal outcome matrices and evaluator development. It does not yet construct the second wave adaptively from the first wave's beam, expose a CLI search policy, run native combat, or establish better search performance.

## Files and API

- [TowerBatchRacingContract.cs](../LL/tools/BalanceHarness/TowerBatchRacingContract.cs): plan, panels, per-trial requests/outcomes, scores, paired contrasts, decisions and report; validates versions, canonical recipes, family/copy legality, references, budgets and exclusions.
- [TowerBatchRacing.cs](../LL/tools/BalanceHarness/TowerBatchRacing.cs): `RunAsync(plan, evaluator, token, checkpoint)` and deterministic `ReconstructAsync(plan, report, token)`.
- [BalanceHarnessBatchRacingTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessBatchRacingTests.cs): synthetic coverage, including a guard that fails on native preparation/combat.

The existing discovery definition supplies one context, three exact bound references, legality, content/settings/runtime identity and the existing designated-primary selection policy. Its historical and scheduled seeds are exclusions, not training inputs. The new plan has its own version, ordered fresh panels and evaluation ceiling. Legacy dispatch, generation versions and archive readers are unchanged.

## Allocation and decisions

| Panel role | Teams | New seeds each | Evaluations |
| --- | ---: | ---: | ---: |
| `wave-1-screen` | 3 references +9 new | 8 | 96 |
| `wave-1-continuation` | 3 references +4 survivors | 8 | 56 |
| `wave-2-screen` | 3 references +4 carried +8 new | 8 | 120 |
| `wave-2-continuation` | 3 references +4 survivors | 8 | 56 |
| `selection` | 3 references +2 challengers | 40 | 200 |
| Total | | 72 distinct seeds | **528** |

Each wave retains the top three challengers by the existing discovery fitness ordering. A fourth challenger is selected from those within one win of the third-ranked challenger: maximize minimum owner-preserving replacement distance to the three elites, with fitness and stable ID resolving distance ties. If none is eligible, take the next ranked challenger. References occupy separate protected slots. This is budget pruning, not a confidence-based claim about discarded teams.

After continuation, beam order uses the common 16 current-wave observations. Prior-wave scores remain in the report but do not compete against new teams' shorter histories. Dropped teams receive no continuation or later-wave scores. After wave two, the three references and top two challengers are ordered together by current-wave fitness before their selection panel is frozen.

Selection reuses `TowerZeroWinSelection.Rank` and the existing designated-primary positive-tie rule. Positive ties otherwise use frozen nominee order; all-zero ties use guardian health first. There is no additional preference for the other two references. `RawSelectedId` is provisional; the report has no confirmation or recommendation field.

## Evidence and failure behavior

The kernel deep-copies the input plan, each evaluator request and each checkpoint snapshot. A panel freeze records exact membership, ordered seeds, context, preceding charges and planned cost before its first outcome. A request binds that freeze, the full scope hash, party identity, role, ordinal, seed and exact scenario including actor identities and equipment. Outcomes must return the matching request hash and seed, a globally unique trial ID, a valid battle outcome and finite in-range ranking telemetry.

Only complete panels receive scores or paired gain/loss counts. Draws are retained and count as non-victories. No historical pseudocounts, duplicate-trial inflation, cache credits, retries or extra tie-breaking evaluations are supported. All five panels must be disjoint from each other, declared historical exclusions, generation seeds, all legacy stage schedules including confirmation/feedback/diagnostics, and reference scenario seeds.

`ChargedEvaluations` is a conservative **logical attempt** counter. An attempt is charged before its pre-dispatch checkpoint; a checkpoint failure, evaluator exception or invalid return does not refund it. It is not a physical-combat receipt. A complete run always charges 528, even if a larger ceiling is supplied. A ceiling below 528 fails before any callback. Cancellation or runtime failure retains accepted observations and charges, returns `Cancelled` or `Failed`, and clears the raw output. Invalid plans throw before execution. Partial reports cannot be resumed or reconstructed as completed runs.

The optional synchronous checkpoint callback receives snapshots at panel freeze, before each dispatch, after each accepted outcome, and at completed panel/beam decisions. A callback failure stops execution. Persistence is the caller's responsibility: save the original plan and the returned terminal report, not only the last `Running` checkpoint. The kernel does not implement atomic file writes, crash recovery, physical input authentication or cross-run reservation ownership.

Reconstruction feeds the saved literal outcomes through the same state machine and verifies all request identities/order, freezes, scores, contrasts, survivor decisions, nomination order, selected output and accounting by canonical report hash. It does not invoke the supplied evaluator or native engine. This establishes consistency with the saved evidence, not authenticity of a physical battle; captured-runtime/input/archive verification remains the future runner's responsibility.

## Verification

Backend checks ran through `build/run-tests.ps1`, with isolated build artifacts under `TestResults/batch-racing-build-20260923`. The initial sandboxed restore could not read the user's NuGet configuration; the permission-enabled rerun completed. The first fixture run exposed its inherited zero-win selector; the fixture now explicitly opts into the existing incumbent-tie selector required by the new contract. The final build had zero errors and 11 existing warnings, none in the new files.

| Check | Result | Receipt |
| --- | --- | --- |
| New kernel tests | 47 passed, 0 failed/skipped | [TRX](../TestResults/batch-racing-tests-20260923.trx), [build/test log](../TestResults/batch-racing-tests-20260923.log) |
| Legacy allocation, three-reference, tie, incumbent, preset and exploration regressions | 138 passed, 0 failed/skipped | [TRX](../TestResults/batch-racing-regression-20260923.trx), [test log](../TestResults/batch-racing-regression-20260923.log) |
| New-file whitespace and document links | Passed | Local file/link checks |

Commands (PowerShell, repository root):

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessBatchRacingTests' -ArtifactsPath 'TestResults/batch-racing-build-20260923'
./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessEvaluationAllocationTests|FullyQualifiedName~BalanceHarnessThreeReferenceTests|FullyQualifiedName~BalanceHarnessThreeReferenceTieTests|FullyQualifiedName~BalanceHarnessIncumbentSelectionTests|FullyQualifiedName~BalanceHarnessPracticalPresetTests|FullyQualifiedName~BalanceHarnessReferenceExplorationTests' -ArtifactsPath 'TestResults/batch-racing-build-20260923'
```

The fixtures cover exact stage costs, paired seeds, deterministic reconstruction, late superior teams, equal current-wave evidence, protected references, competitive diversity/fallback, missing later measurements, draws, selection compatibility, canonical identity, duplicate batches, family/copy failures, historical/held-out overlap, malformed outcomes, cancellation, callback isolation and changed archives. Passing these checks validates implementation behavior, not scientific superiority.

## Next integration boundary

The [adaptive generation and archive integration follow-up](Tower-Adaptive-Racing-Implementation.md) now implements the next code increment under `tower-adaptive-beam-racing-v1`. Frozen v1 remains supported through the shared allocation kernel. The following paragraph records the boundary at completion of this first increment.

The next increment can provide bounded legal generation from the preceding beam and references, freezing each proposal batch before its fresh panel. That changes the current supplied-batch contract and should receive its own explicit version. Native execution then needs a runner that authenticates returned evidence against captured inputs, reserves time/storage/physical attempts, persists freezes durably, and keeps final confirmation inaccessible. CLI dispatch and a prospective comparison protocol belong with that integration, not implicit fallback through an old fixed-panel policy.

No combat study, migration, runtime configuration change, deployment, or gameplay-value change is part of this increment. The pending 52,000-fight confirmation and its resource amendment are untouched.
