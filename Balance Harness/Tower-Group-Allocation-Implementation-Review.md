# Category-allocation implementation written; verification stopped in setup

15 September 2026. The opt-in `independent-group-allocation-v1` / `group-allocation-joint` source and fifteen new tests are written. **They have not been compiled or executed.** The once-only setup failed because its assertion counted only `[Fact]` methods while expecting the expanded runtime test-case count. This is an evidence-workflow error, not a failed policy test. The frozen zero-retry stop was honored; there is no new construction or combat result and no default promotion.

## Implementation

The [saved-slot audit](Tower-Control-Slot-Audit-Review.md) showed protection taking the last slot before recurring control was considered. The new policy gives each compatible missing category a first-slot alternative when the number of missing categories exceeds remaining slots. Otherwise it creates one allocation. Each group has at most five alternatives, using only existing metadata and ordinary proposal/attempt budgets. No control recipe or Essence-specific preference enters selection.

Sibling allocations share the original diversity owner count, placement and filler seeds. They run adjacently before advancing to the next metadata-ordered group; every eighth request remains uniform. After one allocation sweep, the original owner-count/filler sweep advances. Completion prioritizes the selected category, then retains the previous provider reach/tie rules and remaining category ordering. Slot limits, case-insensitive families, global owned-copy checks, group preservation and count-drift rejection continue through the existing code paths. Allocation traces are optional and omitted for old policies. Ability execution order remains fixed ordinal Essence ID order.

This deliberately trades some group breadth for alternative slot allocations within the same proposal budget. Duplicate or equivalent alternatives can still occur, including when one Essence supplies several categories. No efficiency or combat improvement is established. Old-policy parity, compilation and the new behavior remain unverified until the stopped checks are completed in a separately frozen scope.

## Exact failure and diagnosis

At `workflow.py` line 20, setup asserted that the count of `[Fact]` attributes was 110. Static inspection finds **85 Fact methods and 25 InlineData cases**, totaling **110 declared cases**. The 95 previous cases comprise 70 Facts plus 25 parameterized cases; the new class adds 15 Facts. The intended 110-case runtime total was correct; the preflight counted the wrong thing. All previous fixture files match the sealed completion-verification fixture hashes. The [static diagnosis](../TestResults/balance/tower-group-allocation-implementation-20260915/static-diagnosis.json) retains per-file counts and hashes. This diagnosis is not a substitute for running the tests.

The failure occurred after preservation and source/fixture capture, before the complete freeze record and before candidate or test compilation. Preserved: [original failure](../TestResults/balance/tower-group-allocation-implementation-20260915/freeze-failure.json), original workflow, captured source, fixtures and [implementation patch](../TestResults/balance/tower-group-allocation-implementation-20260915/implementation.patch). The failed script was not modified or rerun.

The [protocol](Tower-Group-Allocation-Implementation-Protocol.md) states: “A failed frozen check or cap stops dependent diagnostics; retain evidence without retry.” In accordance with that frozen rule and the user's zero-retry constraint, the following did **not** run: candidate build, test build, all 110 backend tests, 95 captured construction requests, reconstruction of the saved 44-evaluation search, the metadata allocation sweep, independent construction audit and success-only publisher. No backend test was reported as passing in this task.

Next: correct the setup's test-case accounting in a separately frozen verification package, retaining this failure and its charges, then run the original build/test/construction/parity scope once. The successful prior 95 backend cases remain historical evidence only. Do not run a combat comparison or allocate seed values before engineering verification succeeds.

## Measurements and commands

The failed setup took **2.953 seconds**. Initial capture and the static diagnosis each carry a conservative one-second charge. Evidence closure records its elapsed time plus a one-second seal allowance in the [completion receipt](../TestResults/balance/tower-group-allocation-implementation-20260915/completion.json). Carried diagnostics: **1189.992 seconds**; carried output: **1,594,856,311 bytes**. Bounds remain 180 seconds / 256 MiB new and 1,800 seconds / 4 GiB cumulative. **Zero builds, test invocations, fights, fresh values, preparations, replays or retries.**

All **14,152 indexed files in 35 preceding packages** verified unchanged before and after the failure. Unrelated dirty files and captured source matched at closure. The final package retains the exact output inventory and source hashes.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-group-allocation-implementation-20260915'
& $python -B "$work/workflow.py" freeze
# STOP: setup assertion failed. No build/tests/content/audit command ran.
& $python -B "$work/close-failure.py"
```

These commands document the once-only execution and evidence closure; do not rerun sealed evidence. The prepared test phase targets `build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests"` with the seven relevant test classes, but was not dispatched. Closure only performs preservation, static diagnosis and Markdown updates.

## Changed files and implications

Added `LL/tools/BalanceHarness/TowerGroupAllocationSearch.cs` and `LL/tests/EssenceSystem.Tests/BalanceHarnessGroupAllocationTests.cs`. Modified completion/group-count integration and explicit policy registration in `TowerBossDiscoveryContract.cs`, `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerCompositionSearch.cs`, `TowerGroupCompletionSearch.cs`, `TowerGroupCountSearch.cs`, `TowerLoadoutComposition.cs`, `TowerPartyCoverage.cs`. Added this protocol/review and evidence package, and updated six active Markdown handoffs. The source is uncompiled and must not be treated as verified or promoted.

No gameplay/content changes, service configuration, migrations or deployment. Preserve **482,596 reservations**, v19's unused 512 and all 253 recipes. Historical reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved**, adoption **Hold**. No Kharad tuning, fresh-value authorization or 129,536-fight confirmation.
