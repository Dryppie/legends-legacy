# Complete retained-family final reservation request — measured review

**FinalReservationRequestInputsVerified — 15 September 2026.** External request accounting is fixed and verified; the concrete reservation request is assembled from the sealed completed evidence after this package is sealed. **No actual reservation, fresh balance value, preparation or fight occurred.** The 288 future values remain unallocated. This is a reservation-only decision; full-study execution remains excluded. Reliability **Fail 1/3**, adoption **Hold**. Original v19 remains sealed Unresolved, with all 253 required recipes and no confirmation inside that experiment.

The [frozen protocol](Tower-Complete-Family-Request-Protocol.md) and [producing scope](../TestResults/balance/tower-complete-family-request-20260915/scope.json) fix this work. Package: `TestResults/balance/tower-complete-family-request-20260915`. Final external request: `TestResults/balance/tower-complete-family-reservation-request-20260915.json`. Its creation/verification command prints the exact request hash, effective setup bytes/count and elapsed time; it creates no extra unbound receipt file.

## Fix and verification

`TowerCompleteFamilyBinding.cs` previously counted only setup files listed inside the request. Requiring that list to contain the request's own hash is circular, so the external original was omitted. `ReadRequest` now deserializes and hashes the same file handle while excluding concurrent writers, then adds the original's exact hash to the effective map. The original is charged as external setup and copied binding data is charged inside owned study storage. Self-references, including aliases, in-study originals and linked originals are rejected. Later mutation fails setup hash verification. Both public CheckOnly and ReserveAndBind use this method and preserve the parsed-byte hash in their receipts/starts.

`BalanceHarnessTowerCompleteBindingTests.cs` adds four cases and extends the synthetic publication case to assert actual external request bytes/hash in the final controller protocol. The same selected 104 cases from the preceding scope remain selected, with **108 total**. All are zero-combat fixtures using literals or the separate synthetic domain. Gameplay, real allocator derivation, deterministic search/recipes/nominations, controller selection, attempt charging, cancellation, caps and archive verification are unchanged. Seven active Markdown handoffs plus the protocol/review are updated. No configuration, migration or deployment change is involved.

| Verification | Result |
| --- | --- |
| Backend tests via `build/run-tests.ps1` | **108/108 pass**, 38.766 s including correctness build |
| Captured CLI build | 3.422 s; zero warnings/errors; four captured gameplay hashes unchanged |
| Once-only native CheckOnly | **57.532 s**; inner receipt 57.294 s |
| Setup validation, inclusive trace | 0.804 s; 4,167 effective files / 1,180,939,219 existing bytes |
| External CheckOnly request accounted | **1,106,469 bytes**, one additional effective file; exact independent match |
| Complete source validation, inclusive trace | 18.853 s; all 43,879 recipes and fixed family capacity |
| Complete history validation, inclusive trace | 37.402 s; all 481,603 reservations |
| Independent check | 4.750 s |
| Preservation audit | 70.422 s; ten packages / **12,606 indexed files**; 4,303 checkout baseline files |

The [native result](../TestResults/balance/tower-complete-family-request-20260915/native-check.json), [independent receipt](../TestResults/balance/tower-complete-family-request-20260915/independent-result.json), [exact test selection verification](../TestResults/balance/tower-complete-family-request-20260915/test-verification.json) and [preservation receipt](../TestResults/balance/tower-complete-family-request-20260915/preservation-result.json) retain measurements and identities. Existing unrelated correctness build warnings are retained in its log. No selected test failed and no diagnostic was retried. The old failed fixture and all older stopped evidence remain preserved in their sealed packages. The audit observes **0 UI changes** without touching them and preserves 205 protected source inputs plus all 145 registered history paths.

The native effective setup includes its request as one additional file. The independently reconstructed reservation union remains **481,603**, including all **512 unused original v19 confirmation values**, with sorted Int32 hash `10915c9ff420d84850243103793d57e41b60de8d7cf42d2a2350159b7359daa1`. The prospective study directory remains absent. No real allocator candidate is computed in any check.

## Final request and limits

The final external request has Action **ReserveAndBind**, the fixed domain `tower-captured-v19-complete-family-v1`, master **2026091423**, labels first/second, counts **32 / 256**, and at most **100,000 candidate derivations per stage**. It targets the absent `tower-complete-family-reserved-20260915` study directory. Binding is limited to **600 seconds / 67,108,864 new bytes / zero retries / zero fights**. Complete prior history and all accepted values share one exclusion set. Successful future reservation would raise the preserved total from **481,603 to 481,891**; this is arithmetic, not allocation.

All four completed setup packages (initial setup, its preservation closure, durable binding and this request package) and their manifests are included in the final setup map. The request itself is added automatically when read. Its current native CheckOnly predecessor validated captured gameplay/source/history and the fixed action-independent fields. After sealing, the final creation phase changes only action, completed setup inventory and accumulated setup time; it independently verifies every listed hash, exact package membership, fixed identity/caps, request bytes and absent study. **The final expanded ReserveAndBind request is not executed natively in this scope.** Actual binding will recheck source, live history and the complete map before any value is derived, then check history again before publication.

The diagnostic charge is **374.329 seconds / 1,800**, including measured baseline/capture/native/independent/preservation work and frozen allowances of 60 seconds static, 120 seconds sealing and 60 seconds final request creation/verification. Correctness/build add **42.188 separately timed seconds**. The complete future setup basis becomes **1517.272 seconds**, preserving the preceding 1,100.755-second basis. Actual binding would conservatively charge its whole 600-second allowance, producing **2117.272 seconds** before later controller work. Final package accounting appears in `final-verification.json`, including whole changed files/shared TRX, 2 MiB metadata and an 8 MiB allowance for the final external request, within **4 GiB**.

This change closes omitted request-file accounting. It is not a performance optimization or controlled throughput comparison: the native input set has grown to include the entire prior binding package. Earlier **622.54× incremental accounting / 4.50× measured sixteen-write lifecycle** improvements retain their original [performance scope](Tower-Discovery-Performance-Review.md). No revised v19 runtime or whole-study speedup is claimed.

## Reproducible commands

From the repository root, the completed phases are below. Existing outputs intentionally prevent reruns; use a separately frozen scope for new diagnostics.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$package = 'TestResults/balance/tower-complete-family-request-20260915'
& $python -B "$package/workflow.py" freeze
& $python -B "$package/workflow.py" tests
& $python -B "$package/workflow.py" build
& $python -B "$package/workflow.py" native_freeze
& $python -B "$package/workflow.py" native
& $python -B "$package/workflow.py" independent
& $python -B "$package/workflow.py" preserve
& $python -B "$package/update-documents.py"
& $python -B "$package/workflow.py" seal
& $python -B "$package/workflow.py" final_request
```

The test phase calls PowerShell 7 and `build/run-tests.ps1` with the frozen exact filter and a separate artifact directory. Builds used normal host access to the existing NuGet configuration. Native CheckOnly arguments and executable/request hashes are in `native-freeze.json`. All permitted verification commands are run; the actual allocator and controller commands remain deliberately unrun.

The single proposed reservation-only command, **requiring an explicit exception to the original zero-fresh-seed limit**, is:

```powershell
dotnet 'TestResults/balance/tower-complete-family-request-20260915/executable/BalanceHarness.dll' tower-complete-family-reserve-bind 'TestResults/balance/tower-complete-family-reservation-request-20260915.json'
```

This command does not fight. Full-study execution is a different command and remains excluded. Interrupted or capped reservation must retain all evidence and cannot retry or automatically resume. Process termination, disk loss or power failure have not been simulated; old sealed binaries do not understand Pending markers and must not be used to allocate around them. Full-family second-stage selection capacity, combat across the 256-cell outer batch boundary and full-study throughput remain unverified. The future study cap remains **2,434,784 attempts / 24 hours / 64 GiB / zero retries**. No gameplay/content/Kharad tuning or deployment occurs.
