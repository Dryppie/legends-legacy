# Practical Tower allocation-to-launch handoff

17 September 2026. Target: the offline BalanceHarness. **The owned handoff is implemented.** Allocation now occurs in the existing watched worker while the parent holds the registry/output leases, so the current reservation belongs to the same run excluded from its own history scan. No existing-output exception, external-history subtraction or resume path was added.

**Later follow-ups:** the [separately authorized native execution](Tower-Practical-Native-Verification-Execution-Review.md) completed 1,408 fights and both audits. The subsequent [allocation-recovery increment](Tower-Practical-Allocation-Recovery-Review.md) supports authenticated closed journal prefixes and ordered interrupted binding writes; unresolved derivations stay blocked. The original engineering-only verification record below remains unchanged; it predates both follow-ups. The current exclusion total is 484,285, including the native run's 297 values.

## Behavior and boundaries

`tower-practical-search-allocation-check` and `tower-practical-search-allocate-run` accept the distinct `tower-practical-allocated-search-v1` request. The existing `check|run` commands continue to accept only declared-input requests, and omitted allocation metadata preserves their prior serialized hashes. The [practical guide](../LL/tools/BalanceHarness/PRACTICAL-SEARCH.md#owned-allocation-and-launch) gives the template and allocation contract.

Admission checks an unscheduled template, its producing/content bindings, native recipes, full historical union and exact fight cost. Fixed validation labels are temporary shape/preparation inputs, not allocated schedules or executed battles. The worker freezes source/history evidence and publishes Pending before deriving any candidate. A write-through journal records each start before derivation and each completed candidate/acceptance result afterward. One domain/master, disjoint stages, historical rejection and a total 100,000-candidate ceiling are fixed. The entire future panel must fit the history reader's capacity.

After the last recorded result, the worker writes the bound definition, receipt and seed ledger, rechecks live history, publishes Complete and reconstructs the binding before study execution. The parent and later public verifier also reconstruct it. They use the fixed production derivation; fixture substitutions are internal test seams. Native gameplay evaluation and saved-study verification remain mandatory in production.

The parent starts the clock before launcher setup and holds the registry lease until completion/failure. Allocation and its reconstruction therefore spend the same prior-inclusive time/storage envelope as the study. Parent death, deadline and storage stops retain partial files. A worker-start failure occurs before allocation and leaves a failed, nonreusable output claim. There is no second root, retry, resume or refund.

## Interruption visibility

| Boundary | Retained state | What another allocator sees |
| --- | --- | --- |
| Before worker startup/admission | Request/launch/failure as applicable; no derived values | Original union; this output cannot be reused. |
| Pending before the first derivation | Frozen template/history/intent and Pending | A blocking unresolved reservation. |
| Start without a result | Flushed start, possibly an unrecorded derived value after a crash | Pending blocks allocation; no assumption of zero work. |
| Partial or complete journal before Complete | Every committed accepted/rejected result, original Pending | Pending blocks allocation even if a separate complete seed ledger was already written. |
| Complete before study execution | Entire bound panel and original historical union | All values are excluded, including unused confirmation values. |
| Later execution/publication failure | Complete reservation and any durable battle attempts | Entire panel remains excluded; no retry. |

**Recovery limitation:** existing practical/refinement recovery formats do not admit allocated-search Pending. Such failures remain blocked until a separately designed exclusion-only audit can establish the exact boundary. This increment deliberately does not claim partial-allocation recovery or reinterpret an unresolved derivation. No retained failure was recovered.

## Changed files and verification

* `TowerPracticalAllocation.cs`: template contract, durable allocation, binding reconstruction and public allocation entry points.
* `TowerPracticalSearchRun.cs`: request-version handling and allocation within the existing worker, publication and audit path.
* `TowerPracticalReservationRecovery.cs`: explicit rejection of allocated requests by the declared-input recovery format.
* `Program.cs`: command help.
* `BalanceHarnessPracticalAllocationTests.cs`, `BalanceHarnessPracticalProcessTests.cs` and the existing process fixture host: literal-value binding tests, real process interruption/publication tests and public-command rejection checks.
* The practical guide, readiness/assessment/evaluation/native-plan documents, README and native-plan calculation metadata record the current boundary and remaining native-workflow gap.

The focused runner command is maintained in the [practical guide](../LL/tools/BalanceHarness/PRACTICAL-SEARCH.md#verification-scope). Coverage includes normal binding and all three publication decisions; historical/current-panel collisions; each interruption boundary; actual parent death; an exclusive registry lease; mismatched templates/requests; torn, altered and extended journals; prior-cost tampering; history drift/capacity; worker-start failure; deadline/storage stops; and public native admission rejecting fabricated bindings before derivation.

The final runner passed **257/257**, zero failed/skipped, with zero build errors and nine existing warnings. This adds 33 allocation cases and ten process cases to the preceding 214-check suite; the process suite now contains 19 cases. The first run passed 252/253; one test incorrectly counted the handoff's reconstruction of completed candidates as additional allocation calls. Its expectation was corrected, and history-capacity, public-command and allocation-deadline cases were added before the passing rerun. The runner used the existing user NuGet configuration with the required access; no test command remained blocked.

The read-only native-plan script passed syntax, retained-pin, runtime-inventory and phase-arithmetic checks and reproduced the refreshed JSON exactly. All 152 local links across seven current documents passed after updating the assessment's date anchor. Changed-file whitespace and `git diff --check` passed; Git only reported its existing LF-to-CRLF normalization notices.

During this engineering increment, only literal temporary fixtures were allocated. No production evaluation values, combat, native reconstruction of retained studies, historical recovery, migrations, application configuration changes or deployment occurred. The then-current **483,988 exclusions**, V19's **512 unused values /253 required recipes**, **Unresolved** reliability and **Hold** adoption were preserved. Native admission and successful gameplay were outstanding at that point; the [later completed native verification](Tower-Practical-Native-Verification-Execution-Review.md) supplies that separate evidence and records the current 484,285 exclusions. The quality experiment remains no-go.
