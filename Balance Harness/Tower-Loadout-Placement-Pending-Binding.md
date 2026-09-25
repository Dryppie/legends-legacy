# Loadout placement: authenticated native pending worker bindings

Current status: native worker binding v5 now activates declared pending-storage limits, with owner validation and retention of the final observation. Supervisor phase declarations and scientific launcher integration remain unimplemented. Lease writes, runtime/cache paths and the remaining observer lifetime still lack bounds. Both compressed guards and the 1,806/1,800-second gate remain closed.

The target is the offline Balance Harness. `RetainedOwner.run_worker(..., pending_storage_budget=...)` accepts a finite native declaration and requires the existing three capped sidecars. It emits `tower-proposal-worker-binding-v5`; the native worker authenticates the binding, request and executable, checks the declaration before creating sidecars, and runs its body inside `TowerPendingStorage.RunAsync`. The binding retains the existing 16,384-byte maximum. Numeric limits are actual nonnegative signed 64-bit integers; creation counts are positive signed 32-bit integers. Paths must be canonical, absolute, distinct and non-nested. Declarations are copied before execution.

The native worker rejects pending sources or destinations overlapping its binding, request, executable or sidecars, including ancestor overlaps. The owner also excludes its retained directory and accounting module. These checks protect cooperative worker identities and receipt files; they do not provide filesystem confinement or protect all possible external domains. Unlinked-path checks do not establish a hard-link or concurrent-mutation bound.

Work receipt v2 embeds `bindingSha256` and `pendingStorage` inside the existing capped receipt. Its publication and terminal persistence observations authenticate those exact bytes, so this step adds no fourth sidecar. The snapshot reports accepted lifetime writes, tracked live/peak bytes, published/deleted bytes, creation counts and potentially unknown failed progress. The owner independently validates the schema, ranges, byte conservation, creation capacity and completion claims, then compares the normalized declaration and binding hash with the ones it dispatched. Missing observations, substituted declarations and downgraded receipts fail retention.

Pending scope completion precedes successful worker completion. Cap failures, unresolved files and caught storage errors cannot report successful work. Valid failed receipts remain retained, and a worker error remains primary if later publication or retention also fails. A completed pending scope can accompany failed work when post-body request/executable authentication fails. The distinction is preserved explicitly.

Native binding versions 1 through 4 reject the new field, including an explicit null. Their defaults and receipt schemas remain unchanged. The independent Python worker rejects v5; accepting a native-only declaration there would imply enforcement it does not have. The diagnostic owner can retain both receipt versions, while each invocation requires the version selected by its binding.

This is opt-in diagnostic integration. `StudyWorkSupervisor` and the scientific launcher are unchanged and do not generate pending declarations. The protocol does not bound all native storage: raw APIs, directory metadata, retained destination lifetimes, leases, serialization memory, runtime/cache writes, external mutations and finite observer work remain unresolved. `wholeProcessCoverage`, `usableForAdmission`, scientific admission and runtime qualification remain false.

Verification passes **297 backend tests**, including **32 new binding cases**, through `build/run-tests.ps1`, using one fresh isolated runtime. The build reports 45 existing warnings and zero errors. **477 Python tests** pass: the 456 regression cases and 21 new declaration, observation, owner and native interoperability cases. No earlier Python test result is substituted for a rerun; legacy exchange fixtures retain their authenticated historical runtimes.

Native fixtures exercise all three native phases with literal atomic replacement, lifetime/create accounting, invalid or overlapping declarations, unsupported fields, missing limits, unresolved files, caught errors, publication failure and post-body authentication failure. Eight exported success/failure receipts are independently checked in Python. A fresh complete fabricated-outcome fixture passes the production native audit through the v5 owner boundary with a zero-write pending allowance and a capped log. That audit reconstructs 15,744 trial bindings and performs no encounter preparation or combat. Its owner closeout deliberately remains failed/incomplete because the fixture runs only the audit phase; a complete scientific owner sequence is not claimed.

The [verification package](../TestResults/loadout-placement-pending-binding-verification-20260925) retains commands, logs, source snapshots, isolated runtime identities and correctness fixtures. The [handoff](../TestResults/loadout-placement-pending-binding-handoff-20260925.json) pins that package. Relevant checks are:

```text
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-pending-binding-20260925 -Filter <184 pending/receipt cases>
./build/run-tests.ps1 -NoBuild -ArtifactsPath .artifacts/loadout-pending-binding-20260925 -Filter <90 native regressions>
./build/run-tests.ps1 -NoBuild -ArtifactsPath .artifacts/loadout-pending-binding-20260925 -Filter FullyQualifiedName~BalanceHarnessContentAccountingTests
python -B -X utf8 TestResults/loadout-placement-pending-binding-verification-20260925/run-verification.py
python -B -X utf8 TestResults/loadout-placement-pending-binding-verification-20260925/run-supplemental.py
python -B -X utf8 TestResults/loadout-placement-pending-binding-verification-20260925/run-interop.py
python -B -X utf8 TestResults/loadout-placement-pending-binding-verification-20260925/run-audit.py
git diff --check -- <changed files>
```

Changed files comprise the native binding/receipt boundary, pending path protection, Python receipt/owner validation, two new test files, this report, [resource specification v7](Tower-Loadout-Placement-Resource-Boundaries-v7.json), the [boundary inventory](Tower-Loadout-Placement-Pending-Binding-Boundaries.json), LF attributes and only line 3 of nine status documents. The specification has 11 domains and 43 source references; the inventory has 21 families and 96 references. Historical document bodies, sealed evidence and captured executables remain intact. No required command remains blocked. There are no migrations, application configuration changes or deployments; diagnostics opting into v5 require the updated authenticated runtime and owner module.

Cumulative scientific charges remain **79,339.66095319996 seconds** and **51,980,910,910 bytes**; declared maxima remain **168,240 seconds** and **107,122,524,160 bytes**. No scientific launch, qualification sample, resource experiment, timing pair, live-history rescan, production entropy or scientific reservation occurred. Correctness runs establish no performance improvement, qualified forecast or compression speedup.

Next define complete pending declarations for each native phase and propagate them through the supervisor without opening scientific gates. A separate lease byte/lifetime contract, runtime/cache bounds and a finite observer obligation are still required before resource qualification can be considered.
