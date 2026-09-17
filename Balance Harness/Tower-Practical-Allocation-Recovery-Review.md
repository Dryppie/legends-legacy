# Practical Tower allocation recovery: supported boundaries

17 September 2026. Target: the offline `LL/tools/BalanceHarness` tool. Exclusion-only recovery for authenticated interrupted owned allocations is implemented. **316 distinct focused checks passed** across a 313-case regression run and three additional interruption cases. This increment adds 58 allocation-recovery cases and one process boundary to the preceding 257-case suite; existing process cases now also exercise recovery.

Use the existing `tower-practical-search-recover` and `tower-practical-search-recovery-verify` commands with request/receipt version **`tower-practical-abandoned-allocation-v1`**. The [practical guide](../LL/tools/BalanceHarness/PRACTICAL-SEARCH.md#practical-pending-recovery) describes the external manifest, ownership, path and accounting contract. The declared-input and refinement receipt versions retain their own scopes.

## What can be recovered

| Frozen failed-source boundary | Result |
| --- | --- |
| Pending and allocation intent are durable; journal absent or empty | Publish exclusions containing the original historical union, with zero newly reserved values. |
| Journal ends after complete `Start`/`Candidate` pairs within or between stages | Authenticate each recorded result and collision decision; preserve all accepted values. The unallocated suffix remains unallocated. |
| Entire journal is complete, before binding artifacts are written | Reconstruct the bound definition in memory and preserve the full panel. |
| Complete journal followed by ordered definition, allocation receipt and seed-ledger writes | Accept exact complete artifacts and at most one interrupted byte prefix, only after its predecessor is durable. The Complete history replacement may be partial while the original remains Pending. |
| Trailing `Start`, torn row, changed result, unknown field, reordered/extra row or exceeded candidate bound | Reject recovery. Never compute an unrecorded result to repair the evidence. |
| Unknown artifact, study directory, any battle-attempt journal, active/unknown owners, or mismatched version | Reject recovery. No receipt can infer a pre-combat boundary from such evidence. |
| Durable Complete reservation | No Pending recovery applies; existing complete history already preserves the panel. |

The shared journal reader now serves both complete-publication verification and prefix recovery. It enforces the frozen stage order, stage-local ordinals, newline-terminated rows, exact event fields, production derivation and historical/cross-stage rejection decisions under the original 100,000-candidate ceiling. Only recorded complete pairs reach the derivation function. Internal literal-candidate seams support isolated fixtures; production commands expose no override and reject fabricated fixture candidates.

The auditor shares the established manifest, process-identity, same-host exit, writer-lease and atomic receipt-publication checks. Both recorded owners must have exited. It authenticates the complete source inventory before and after auditing. Complete-history admission dispatches the new receipt version to this auditor and still requires explicit Pending/receipt pins.

Every failed-source byte remains unchanged. Receipts retain the historical union, accepted reservations, original prior charges and **the entire forfeited cumulative time/byte allowance**. These forfeiture fields are not measured consumption. No run is resumed, no allowance is refunded, and no existing or interrupted receipt is overwritten. Missing process ownership and unresolved derivations remain deliberate blockers.

## Changed files and verification

* [Allocation journal reader](../LL/tools/BalanceHarness/TowerPracticalAllocation.cs): shared full/prefix reconstruction with strict row and journal-bound checks.
* [Recovery entry points and common audit](../LL/tools/BalanceHarness/TowerPracticalReservationRecovery.cs) and [allocated registration audit](../LL/tools/BalanceHarness/TowerPracticalAllocationRecovery.cs): distinct version, supported boundary checks, unchanged receipt accounting and publication controls.
* [History-reader dispatch](../LL/tools/BalanceHarness/TowerRefinementComparisonLaunch.cs) and [command help](../LL/tools/BalanceHarness/Program.cs): admit explicitly pinned new receipts through the existing commands.
* [58 allocation-recovery tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessPracticalAllocationRecoveryTests.cs) and [20 process cases](../LL/tests/EssenceSystem.Tests/BalanceHarnessPracticalProcessTests.cs): closed prefixes, collisions, atomic-write interruption, tampering, public command behavior, registry admission, preserved costs and source bytes, plus actual parent/worker termination. The six allocation parent-death boundaries cover Pending, unresolved start, completed candidate, full journal, pre-Complete binding and durable Complete handoff.
* The practical guide, README and current assessment/status Markdown now describe the supported recovery scope. Original native-plan JSON, historical accounting and sealed execution outputs remain unchanged.

Both test invocations used `build/run-tests.ps1` with the normal project build and `TestResults/practical-search-build` artifacts. The first used the [full focused filter](../LL/tools/BalanceHarness/PRACTICAL-SEARCH.md#verification-scope), including the new recovery class, and passed **313/313**. After adding three mid-journal unresolved-start regressions, the second used:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessPracticalAllocationRecoveryTests.An_unresolved_start_after_a_closed_prefix_never_derives_the_missing_result' -ArtifactsPath 'TestResults/practical-search-build'
```

It passed **3/3**. The retained [313-case result](../TestResults/tests/practical-allocation-recovery-313.trx) and [three-case result](../TestResults/tests/practical-allocation-recovery-3.trx) contain 316 distinct passing cases, zero failures/skips. Builds had zero errors and nine existing warnings. NuGet access used the existing user configuration outside the restricted sandbox. Markdown links, changed-file whitespace and protected artifact hashes were also checked. No required command remains blocked.

All new boundary evidence is synthetic, including the controlled processes. No production allocation, combat, retained-study native reconstruction or recovery of a historical failure ran. No migration, persistent application configuration change or deployment is needed. The [completed native run](Tower-Practical-Native-Verification-Execution-Review.md) remains closed and needs no recovery; its preserved runtime establishes that historical execution, while this engineering build has its own fixture verification.

The permanent exclusion union remains **484,285**. Both anchors remain recommended, strength is **ImprovementNotDemonstrated**, adoption is **Hold**, and V19 reliability is **Unresolved**. This engineering gap is closed within the boundary table above. The next scientific step requires a justified distinct hypothesis and a feasible prospective evaluation; the [quality proposal](Tower-Practical-Evaluation-Design.md#gono-go-conditions-and-next-work) remains **no-go**, with no experiment queued.
