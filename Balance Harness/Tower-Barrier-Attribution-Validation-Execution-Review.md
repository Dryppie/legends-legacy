# Barrier attribution validation: readiness stopped

**16 September 2026 — DiagnosticInvalid: readiness did not complete.** The approved scope stopped before allocation. Both isolated runtime hosts built and prepared the unchanged anchor parties successfully, and **15 of 16** launcher tests passed. The required live history-registry scan raised `OperationCanceledException` at its readiness deadline. **Zero fresh values, zero fights and zero replays** occurred. The observer's full-encounter parity and attribution completeness remain unmeasured.

The [prospective protocol](Tower-Barrier-Attribution-Validation-Protocol.md) required a complete recovery-aware registry check before allocation, followed by old/new-off/new-on report parity before collecting attribution data. A known-file hash check cannot replace that gate. The cancelled scan did not establish either a registry mismatch or a clean live registry. No retry, larger deadline, alternate registry or bypass was used.

## Completed readiness and failure

The approved **60-second /16-MiB run-to-engineering transfer** was applied once after reconciling carried consumption. Overall limits did not increase. A master and domain were frozen before any possible derivation; no production candidate derivation followed. The complete before-edit working-tree baseline and existing handoff documents were preserved.

Two isolated host builds succeeded. The control loads the captured pre-attribution service assembly; the new host loads the already verified attribution assembly. Other runtime dependencies and captured content remain pinned. Native admission confirmed that both modes produced identical inputs and prepared participants for each anchor, and the prepared participants exactly matched the previous comparison. No production service or harness rebuild was performed.

The focused backend invocation used the required wrapper:

```powershell
./build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-barrier-attribution-validation-20260916/tests -Filter 'FullyQualifiedName~DiagnosticTests|FullyQualifiedName~AttributionTests'
```

It executed **16 tests: 15 passed, one failed, none skipped**. The failed case was `Complete_live_registry_and_original_recovery_are_reconciled`. Its stack ends in filesystem enumeration inside `TowerHistoryRegistry.Scan`, called by the production `Refresh` path. The test had a cancellation token derived from the remaining readiness phase budget. The backend process completed after **27.328 seconds** and returned failure; the process-tree guard confirmed all owned processes exited. This was a cooperative cancellation, not an abandoned background scan.

Readiness charged **46.046000 of its 50 seconds**, including the fixed 12-second static allowance and every setup/build/admission/test command. About **3.954 seconds** remained, insufficient for another full scan plus its cleanup reserve. The scan itself had not completed in roughly 25 seconds. Retrying within that remainder would not be a credible way to satisfy the gate. Moving capacity from another phase after observing this failure would violate the frozen limits.

The passing cases cover the fixed 24+16 schedule, overlapping-panel rejection, authorization and once-only publication, attempt/completion bounds, interruption handling, complete-report parity rejection, trace validity, literal-only reservation fixtures and Pending interruption, native attribution fixtures, schema-1 and residual rejection, coverage thresholds and literal saved-input reconstruction. The earlier 30 observer checks were reused only with matching source/dependency pins; they were not rerun or counted as new test executions.

## Saved-evidence audit and preservation

The [failure audit](../TestResults/balance/tower-barrier-attribution-validation-20260916/readiness-failure-audit.json) independently checked the TRX result and cancellation stack, all owned process exits, unchanged compiled-source/runtime pins, identical admitted participants, and the absence of production binding, launch, attempt, battle and trace artifacts. The prior shared TRX was restored, while the failed result and its predecessor remain retained. Both stored attribution fixtures also passed independent Python reconstruction; an unexplained-residual mutation was rejected. None of this substitutes for the unfinished live scan.

All known historical files and the authoritative **483,720-value** exclusion ledger remain byte-preserved. No values were reserved from the proposed 12-value allowance. The preserved allocator master is not a combat seed or a reservation. V19 still has 253 required recipes and 512 unused confirmation values, with reliability **Unresolved**. The completed Web Weaver failure and all other sealed studies are unchanged.

The [commands and process receipts](../TestResults/balance/tower-barrier-attribution-validation-20260916/control/readiness-test-1-backend-command.json), [failure output](../TestResults/balance/tower-barrier-attribution-validation-20260916/control/readiness-test-1-backend.log), [failed TRX](../TestResults/balance/tower-barrier-attribution-validation-20260916/control/readiness-1.trx) and [completion receipt](../TestResults/balance/tower-barrier-attribution-validation-20260916/completion.json) are retained. The independent audit used saved evidence only. Native history reconciliation, readiness sealing, reservation, parity fights, diagnostic fights and combat-result reconstruction did not run to completion or were not started because the prerequisite failed. No test failure is hidden or relabeled as a pass.

## Closure and resource implications

This scope is **Closed /DiagnosticInvalid**, specifically a readiness cancellation. It supplies no combat evidence about observer parity, attribution usability or team strength. Adoption stays **Hold**. No additional fight, value, retry or resume is authorized by this package. The proposed eight-value-per-anchor coverage gate was never evaluated.

The transfer remains applied: component caps are engineering **1,385 seconds /671,088,640 bytes**, run **1,915 seconds /369,098,752 bytes**, and audit **300 seconds /33,554,432 bytes**. Overall caps remain **7,980 seconds /5,804,916,736 bytes**. Failed work, transient/overwritten output and final publication are charged. After the fixed ten-second publication charge, **4.121880 engineering seconds** remain; unused phase allowances are not a fresh allocation.

A later attempt would need a separately specified readiness envelope that can accommodate the required full registry scan. This result does not justify weakening history verification, restarting the closed filesystem-performance work, claiming a measured speedup, changing a party or running the unused combat allowance. No follow-up attempt is created here.

Changed files are the isolated launcher/test/audit/evidence package, this execution review and current notices in the BalanceHarness README and five handoff documents. No production source, gameplay rule, content, migration, persistent configuration or deployment changed. Final checks cover sealed files, source/runtime pins, unchanged unrelated dirty files, document links, whitespace and budget arithmetic.
