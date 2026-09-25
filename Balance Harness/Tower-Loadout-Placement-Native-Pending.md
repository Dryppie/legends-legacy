# Loadout placement: declared native pending storage

Current status: an opt-in native diagnostic scope enforces declared pending-file byte and creation limits at the existing JSON, gzip and atomic-publication boundaries. Worker bindings and the scientific launcher do not activate this scope yet. Lease writes, caches, monitor console and the remaining observer lifetime remain unbounded. Both compressed guards and the 1,806/1,800-second gate stay closed.

The target is the offline Balance Harness. `TowerPendingStorage.RunAsync` accepts an immutable copy of a finite declaration: canonical absolute pending paths, their allowed destinations, per-file byte limits and creation counts, plus shared live-pending and cumulative-write caps. Path validation rejects ambiguous Windows names and overlapping source/destination declarations. The scope cannot be reused or nested.

`TowerWorkAccounting.OpenWrite` validates declared scratch writers before invoking their create factory. `HarnessJson.WriteNew(..., scratch: true)`, `TowerCompactBundle.WriteGzip(..., scratch: true)` and `TowerCompleteReservation.Storage.PutBytes` use this boundary. Default calls preserve their original streams, create flags, flush behavior and publication path. The new contract does not require the optional operation counter collector to be active.

Bounded writers append only. They check the per-file, live-pending and lifetime write allowances before each write, serialize concurrent access to shared limits, and retain completed accepted byte counts. They expose neither seek nor truncate. A failed write may leave an unknown prefix; it poisons the scope and does not become a successful completion. Cleanup closes owned writers and preserves a callback error when later scope cleanup fails.

A pending file must close and pass length/content verification before publication or deletion. File publication must use the declared destination; directory publication checks every member and its corresponding destination. A successful move or deletion releases live pending bytes but never refunds lifetime writes or creation attempts. Repeated atomic replacement therefore remains charged even when earlier pending names disappear. A failed move poisons diagnostic retries; legacy publication retries remain unchanged outside the scope. Success requires every pending file to be resolved and its original path absent. Failed and unresolved files remain as evidence.

The caps cover selected cooperative pending writers. They do not cover raw file APIs, directory metadata, external or hard-link mutations, retained destination lifetimes, leases, serialization memory, runtime caches or observer work. Closed-file hashes detect selected changes; they are not OS confinement. Snapshot counters explicitly distinguish accepted writes, tracked live/peak bytes, published/deleted bytes and potentially unknown failed progress. `wholeProcessCoverage` and `usableForAdmission` remain false.

The callback scope is implemented and exercised through the actual native storage APIs, but no worker-binding version or launch option is introduced in this step. A later authenticated native worker protocol must carry the finite declaration and verify its final observation. The independent Python worker needs its own applicable contract rather than silently accepting native-only limits.

Final verification passes **265 backend tests** (31 new pending cases and 234 regressions) and **56 Python interoperability/log/audit tests**. The other **400 prior Python results** are retained without rerunning their unchanged sources. The final native build reports 45 existing warnings and zero errors.

Validation includes the new native cases for exact/per-file/shared/lifetime limits, creation-count exhaustion, replacement, deletion, multiple open writers, undeclared paths and destinations, existing files, content mutation, open/unresolved files, failure poisoning, scope reuse/nesting, original-error preservation, hidden write prefixes, async writes, copied declarations, Windows aliases, real JSON/gzip writers, directory publication and a retained literal production-storage fixture. Regression groups cover the existing receipt, sidecar, file, lease, content and work-accounting boundaries. Python interoperability checks exercise current native v4 sidecars, capped logs and production audit success/mutation paths.

The [verification package](../TestResults/loadout-placement-native-pending-verification-20260925) retains exact commands, passing and preliminary logs, source snapshots, isolated runtimes and correctness fixtures. Initial focused tests passed. Later edge cases exposed trailing-dot/space normalization and separator handling; both were corrected before final verification. Earlier builds and exports remain preserved. Backend tests run through `build/run-tests.ps1`; no required command remains blocked.

```text
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-native-pending-complete-20260925 -Filter <242 pending/accounting/receipt cases>
./build/run-tests.ps1 -NoBuild -ArtifactsPath .artifacts/loadout-native-pending-complete-20260925 -Filter FullyQualifiedName~BalanceHarnessContentAccountingTests
python -B -X utf8 TestResults/loadout-placement-native-pending-verification-20260925/run-interop.py
python -B -X utf8 TestResults/loadout-placement-native-pending-verification-20260925/run-audit.py
git diff --check -- <changed files>
```

Two complete fabricated-outcome fixtures were constructed: one during the preliminary run and one with the final runtime. Their bodies and all four isolated build directories remain retained. The final Python checks use the final fixture/runtime. No performance comparison is made from these correctness runs.

The [resource specification v6](Tower-Loadout-Placement-Resource-Boundaries-v6.json) contains 11 domains and 40 source references. The [boundary inventory](Tower-Loadout-Placement-Native-Pending-Boundaries.json) contains 20 families and 87 references. Their limits, coefficients and qualified forecasts remain null, and qualification is not authorized.

Changed files comprise the new native pending scope and tests, the accounting writer/move hooks, JSON/gzip/reservation-storage entry points, this report, versioned specifications/inventory, LF attributes and only line 3 of nine status documents. Historical document bodies and sealed evidence remain intact. There are no migrations, application configuration changes or deployments. Captured historical executables must not be replaced; future diagnostics opting into this API require the updated native runtime.

Cumulative scientific charges remain **79,339.66095319996 seconds** and **51,980,910,910 bytes**; declared maxima remain **168,240 seconds** and **107,122,524,160 bytes**. The first failed experiment remains failed. Complete fabricated-outcome fixtures and audits construct/reconstruct input bindings without encounter preparation or combat. No scientific launch, qualification, resource experiment, timing pair, live-history rescan, production entropy or scientific reservation occurred.

Next carry the pending contract through an authenticated native worker binding, implement a separate bounded lease contract, and resolve runtime/cache and finite observer obligations. Do not infer an end-to-end native pending bound from the diagnostic callback scope. No qualified forecast, compression speedup or search improvement is established.
