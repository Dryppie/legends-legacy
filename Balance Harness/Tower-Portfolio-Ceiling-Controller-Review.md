# Captured-v19 ceiling controller — verified without combat

Completed **14 September 2026** for offline `LL/tools/BalanceHarness`. The four-factor execution controller is implemented, built against the captured gameplay DLLs, and verified with **70 passing tests**, complete materialization parity and reconstruction of the saved confirmation. Status: **ControllerVerifiedSeedBindingPending**. This task ran **zero new fights**, allocated **zero seeds**, and retained all **481,219 reservations**, including the **512 unused v19 confirmation values**. Reliability remains **Fail 1/3**, adoption **Hold**, and the previous confirmation family remains **Fail / Fail**.

The [executable protocol](Tower-Portfolio-Ceiling-Controller-Protocol.md) supersedes the preparation handoff's unbuilt-controller boundary. The historical preparation, diagnosis and v19 packages remain unchanged. The fresh 128-seed schedule is still unbound; the new 129,536-fight screen has not started.

## Implementation and decisions

- `TowerCeilingScreenInputs.cs` binds an externally reserved schedule to all four ordinary 253 × 128 definitions, preserves exact recipes and identities, checks the deterministic seed order and complete declared history, and revalidates prepared participants. There is no seed allocator command. The reservation copy uses WriteThrough and `Flush(true)` before following setup writes. History and external setup evidence are copied for durable reconstruction; changes to registered live inputs block a pending launch.
- `TowerCeilingScreenRun.cs` owns one durable S/C attempt journal, one deadline and one storage owner across all four factors. Prior recorded setup time and external setup bytes are deducted from the same global allowance. Each factor receives only the remaining time/storage. Starts are flushed before engine entry. Partial writes and unmatched starts remain on failure; later factors and automatic resume are forbidden.
- The existing compact runner and verifier retain their caps and archive contracts. The controller verifies each complete factor, checks every archived participant hash against the reviewed preparation, and reconstructs all 1,012 cells before applying the whole-screen selector. Per-factor ordinary intervals cannot select the correction. Detailed timings, CPU/allocation counters and failure evidence use existing infrastructure.
- `TowerCeilingScreenCommand.cs` and the small `Program.cs` dispatch addition expose audit, bind, check, run and verify commands with fixed arity. Retry, resume and cap override arguments are refused. The isolated captured executable uses the same command implementation.
- `BalanceHarnessTowerCeilingControllerTests.cs` adds **39 zero-combat cases**. The existing preparation/storage classes contribute 31. These cover complete family reconstruction, draws, missing/duplicated/out-of-order evidence, changed recipes/identities/limits, preflight and reconstruction combat guards, actual controller sequencing with synthetic writers, durable partial attempts, cancellation, storage exhaustion and external setup accounting.

No gameplay DLL was rebuilt for the captured executable. Its Application, Common, Domain and Services.LL hashes match the sealed confirmation. Current-checkout gameplay remains a different producing version.

## Measurements and verification

The [initial native protocol](../TestResults/balance/tower-ceiling-controller-20260914/diagnostic-protocol.json) froze four exact commands and executable/input hashes before execution. The [final diagnostic protocol](../TestResults/balance/tower-ceiling-controller-20260914/final-diagnostic-protocol.json) froze three further commands after hardening external setup accounting. Both use zero fights/seeds/retries, a cumulative 1,800-second workload limit and 4 GiB of new output. All outputs, intermediate builds and test receipts are retained in the [evidence directory](../TestResults/balance/tower-ceiling-controller-20260914).

| Check | Measured result |
| --- | --- |
| Final captured build | **0 warnings / 0 errors**, MSBuild elapsed **1.88 seconds**. |
| Final backend tests via `build/run-tests.ps1` | **70 passed**, 0 failed/skipped; six existing warnings in the final build. Earlier 67- and 69-test passes remain preserved. |
| Final executable materialization audit | **1,012 / 1,012 exact input and participant matches**; **12.945 seconds** native work, **13.047 seconds** command elapsed. |
| Captured controller fixtures | Complete four-factor archive and interrupted second factor both behave as required; **0.265 seconds** final command. Journal events are synthetic and do not invoke an engine. |
| Saved confirmation reconstruction | **129,536 / 129,536 reports**, all 253 recipes and eight compact batches reconstruct exactly; every roster matches preparation. **105.569 seconds** native work / **105.687 seconds** command. |
| Unbound launch refusal | Expected exit **1**, before any start marker or fight. Final command **0.078 seconds**. This is a passing negative check. |
| Cumulative measured diagnostics | **193.252 seconds**, including all three test-script runs and both native diagnostic sets. The two isolated builds additionally report **4.97 seconds** combined. Final preservation audit timing is recorded separately in the final receipt. |

The full saved-archive diagnostic used the first captured controller build. The final build changed setup budgeting; its native compact verifier and `VerifyRosterRecords` method are byte-for-byte unchanged. The final diagnostic protocol records that check. Materialization, the controller loop and launch refusal were repeated against the final executable. No combat or reservation was repeated.

These are verification measurements, **not a four-factor fight-throughput benchmark**. The prior 14.65-second preparation also wrote 1,012 snapshots; comparing it directly with this read/materialization audit would not establish a speedup. Existing measured filesystem improvements and their whole-campaign limitations remain as documented in the performance reviews.

## Party terminology correction

The Kharad encounter uses **ten characters arranged as two five-character parties**. All 58 supported-breach lineups have one Pack Howler per character: five in each party, ten across the full encounter group. The old diagnosis's “ten-character party” refers loosely to this complete lineup. Its saved slots/party numbers are correct; historical evidence was not rewritten. This is still an association, not a causal Pack Howler result.

## Reproducible commands and remaining boundary

From the repository root, the final test filter is:

```powershell
./build/run-tests.ps1 `
  -Filter 'FullyQualifiedName~BalanceHarnessTowerCeilingControllerTests|FullyQualifiedName~BalanceHarnessTowerCeilingPreparationTests|FullyQualifiedName~BalanceHarnessTowerStorageTests' `
  -ArtifactsPath 'TestResults/balance/NEW-controller-test-artifacts'
```

Exact captured compile and native command arguments are retained in the compiler project, diagnostic protocols and command receipts. Do not rerun writer scripts into this sealed evidence directory. A new diagnostic run needs an absent output and a newly frozen scope.

Fresh-seed binding and a real four-factor run are deliberately unexecuted in this zero-combat task. Before binding, audit the complete registered history, reserve exactly 128 fresh shared values under separate authorization, and record all active preparation time and newly created setup files. The controller checks the declared history and setup inventory; completeness of that registry remains the audit's responsibility. Human review waiting time is excluded from active workload accounting. The resulting bound protocol needs review before a separately authorized launch.

A successful screen can produce at most **CandidateForFullFamilyConfirmation**. It cannot accept the full **43,879-entry** retained inventory or current-checkout gameplay. No required engineering command remains blocked. No live content, configuration, migration, catalog promotion or deployment changed. Existing dirty work and concurrent design documents are retained; the final preservation receipt lists exact differences.
