# Complete retained-family durable binding — measured verification review

**DurableBindingVerifiedNoAllocation — 15 September 2026.** The offline reservation wrapper and final protocol publication are implemented. All **104 selected zero-combat tests pass** after one fixture correction; the full captured CLI's once-only CheckOnly diagnostic and independent preservation checks pass. **No actual reserve-bind, fresh balance value, combat preparation or fight occurred.** The 288 future values remain unallocated. Reliability remains **Fail 1/3**, adoption **Hold**; original v19 remains sealed Unresolved with no confirmation in that experiment.

The [frozen protocol](Tower-Complete-Family-Binding-Protocol.md), [original scope](../TestResults/balance/tower-complete-family-binding-20260915/scope.json), [correctness amendment](../TestResults/balance/tower-complete-family-binding-20260915/correctness-amendment.json) and [effective producing scope](../TestResults/balance/tower-complete-family-binding-20260915/scope-corrected.json) define this work. Evidence lives in `TestResults/balance/tower-complete-family-binding-20260915`; `evidence-files.json` seals the final inventory.

## What changed

- `TowerCompleteReservation.cs`: durable intent, registered Pending history, flushed start/candidate journal, shared historical/cross-stage exclusion set, ordered schedules and verification. Candidate exhaustion/interruption keeps evidence and cannot resume. Fixed-file incremental storage includes temporary replacement bytes and reserves 32 KiB for failure reporting.
- `TowerCompleteFamilyBinding.cs`: separate CheckOnly and ReserveAndBind actions, exact executable/runtime/allocator validation, prior setup seal/byte/time binding, registry/root writer leases, refreshed live history, post-reservation history check, and protocol publication last. The full binding allowance is conservatively charged before later combat. Setup, source and history phases use existing performance traces.
- `TowerCompleteFamilyInputs.cs`, `TowerSearchBenchmark.cs` and `Program.cs`: require reservation proofs, reject failed bindings/unresolved history, expose the separate CLI commands. Existing combat, deterministic search, recipes/nominations, selection, attempt charging, storage batches and archive verification remain unchanged.
- `BalanceHarnessTowerCompleteBindingTests.cs`: 32 synthetic cases. Seven active handoff Markdown files, this review and its frozen protocol are updated. Unrelated checkout/UI work remains intact.

## Verification and measured costs

| Check | Measured result |
| --- | --- |
| First correctness pass, including build | **103 passed / 1 failed**, 40.094 s |
| Corrected correctness pass, including build | **104/104 passed**, 31.063 s |
| Captured full CLI compilation | 5.672 s, zero warnings/errors, all four gameplay DLL hashes unchanged |
| Once-only native CheckOnly command | **81.188 s**; inner receipt 81.008 s |
| Setup artifact checking, inclusive trace | 7.564 s; 1,609 files / 419,068,398 existing bytes bound |
| Complete source binding, inclusive trace | 26.992 s; all 43,879 recipes and exact capacity |
| History checking, inclusive trace | 46.220 s; 145 registered paths / 481,603 reservations |
| Independent read-only check | 3.828 s |
| Final preservation audit | 94.125 s; nine packages / 10,042 indexed files |

The first failure was the fixture's use of `File.ReadAllLines` while the journal writer was open: Windows requires the reader to share the existing writer. The correction uses a read-only `FileStream` with `FileShare.ReadWrite`. No production change followed that failure. The failed source, TRX, command, log and original build artifacts remain retained. The separately frozen corrected pass uses a separate build-output directory and the identical expanded 104-case selection. No native diagnostic was retried. The scoped tests include the original 19 setup + 52 controller cases and one exact legacy history parser fixture; no combat integration class is selected. Existing unrelated project warnings are retained in correctness build logs.

The [native result](../TestResults/balance/tower-complete-family-binding-20260915/native-result.json), [independent receipt](../TestResults/balance/tower-complete-family-binding-20260915/independent-result.json), [exact test verification](../TestResults/balance/tower-complete-family-binding-20260915/test-verification.json) and [preservation receipt](../TestResults/balance/tower-complete-family-binding-20260915/preservation-result.json) retain identities and measurements. The preservation audit compares **4,298 checkout baseline files** with scoped changes, verifies 205 protected source inputs, observes **0 concurrent UI changes** without modifying them, and preserves all 145 historical paths. The complete union remains **481,603 values**, including all **512 unused original v19 confirmation values**. Its sorted Int32 hash is `10915c9ff420d84850243103793d57e41b60de8d7cf42d2a2350159b7359daa1`. The prospective study path remains absent.

The diagnostic charge is **361.282 seconds / 1,800**, including baseline/capture, native/independent/preservation measurements and conservative 60-second static + 120-second sealing allowances. Correctness passes/build add **76.829 separately timed seconds**. Including the previous 662.644-second basis, retain **1100.755 seconds** for future setup charging. Final byte accounting is in `final-verification.json`: package output, whole changed checkout files, shared TRX and 2 MiB metadata allowance are counted against **4 GiB**. Zero preparations/fights/fresh balance seeds/diagnostic retries/resumes occurred.

## Performance interpretation

This scope adds safety gates and measures their setup cost; it contains no new reference/candidate throughput comparison. The prior [discovery performance review](Tower-Discovery-Performance-Review.md) remains the controlled result: at 9,216 archives, incremental accounting fell from **3,424.920 ms to 5.502 ms (622.54×)**, and the measured sixteen-write lifecycle improved **4.50×**. The historical v19 **292.69 minutes** was not rerun and cannot supply detailed historical timing percentages. No whole-study speedup is claimed here. The v4 preparation cost of 223.108 seconds for the complete retained family remains a separate measured limitation.

## Reproducible commands

Run from the repository root. These commands document the completed immutable scope; existing outputs deliberately prevent rerunning it. A new execution requires a separately frozen output path/scope.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$package = 'TestResults/balance/tower-complete-family-binding-20260915'
& $python -B "$package/workflow.py" freeze
# The first pass and its fixture correction are preserved in the amendment.
& $python -B "$package/corrected-workflow.py" tests
& $python -B "$package/corrected-workflow.py" build
& $python -B "$package/corrected-workflow.py" native_freeze
& $python -B "$package/corrected-workflow.py" native
& $python -B "$package/corrected-workflow.py" independent
& $python -B "$package/corrected-workflow.py" preserve
& $python -B "$package/update-documents.py"
& $python -B "$package/corrected-workflow.py" seal
```

The tests command expands to PowerShell 7 calling `build/run-tests.ps1 -Filter` with the exact expression in `test-selection.json`, and a separate `-ArtifactsPath`. Native execution expands to `dotnet <captured BalanceHarness.dll> tower-complete-family-binding-check <request.json> <new-result.json>`; exact absolute arguments and hashes are in `native-freeze.json`. Backend correctness/captured builds used normal host access to the existing NuGet configuration. No required verification command remains unrun.

## Remaining boundary

The real reservation entry point is implemented but **not executed end to end with the prospective allocator**. Synthetic fixtures verify its shared reservation/publication components; the real native diagnostic verifies its public CheckOnly/preflight path. Tests inject interruptions and cancellation but do not simulate process termination, disk loss or power failure. Writer leases coordinate this new binder; old sealed binaries do not understand Pending markers and remain prohibited from fresh allocation. This does not claim arbitrary external writers are transactionally coordinated.

A real binding needs a separately frozen scope, refreshed complete history, explicit ReserveAndBind action, current captured identities and the complete sealed setup inventory/time basis including this package. The diagnostic's CheckOnly request predates final sealing and is not that final study request. Any partial reservation must retain Pending evidence; this implementation offers no retry or automatic recovery. Full-family second-stage selection capacity, combat across the 256-cell batch boundary and full-study throughput remain unresolved. The future envelope remains **2,434,784 attempts / 24 hours / 64 GiB / zero retries**. No fresh reservation or full study is authorized by this review.

No gameplay/content/configuration/database migration or deployment change is involved. No service is deployed. Original v19, its 253 required recipes/nominations and all historical evidence remain preserved.
