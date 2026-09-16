# Complete retained-family external setup — verified inputs, preservation incomplete

Completed **15 September 2026**, following the [14 September frozen protocol](Tower-Complete-Family-Setup-Protocol.md). Target: offline `LL/tools/BalanceHarness`. The seed-free setup inspector, **71 selected tests**, captured build, native source/history inspection and independent check passed. **Final preservation stopped on a concurrent UI-documentation change outside its frozen allowlist.** Overall status **SetupVerifiedPreservationIncomplete**. No combat preparations, fights, fresh balance values or diagnostic retries/resumes occurred. Reliability **Fail 1/3**, adoption **Hold**.

## Implemented and verified

`TowerCompleteFamilySetup.cs` adds `tower-complete-family-setup-inspect <request.json> <new-result.json>`. It validates the fixed allocator metadata, sealed UTC source, complete recipe/anchor family, required history, current registry membership, exact file hashes and equality of the authoritative ledger to the union of all registered reservations. The output records captured execution identity and explicitly sets `executionAuthorized: false`. It refuses overwrite, changed history and missing or altered allocator parameters. It cannot allocate or launch a study.

The external allocator primitive uses SHA-256 of UTF-8 fields separated by U+001F, invariant decimal master/ordinal, and the first four digest bytes as signed Int32 little-endian. The bound future domain is `tower-captured-v19-complete-family-v1`, master **2026091423**, first/second counts **32 / 256**, with **100,000 candidate derivations maximum per stage**. One exclusion set retains history and earlier accepted values across stages. Independent vectors use a separate fixture domain/master; other fixtures use literal synthetic candidates. **No candidate was derived in the future balance domain.** This primitive does not durably reserve values: a future external allocation wrapper still must retain intent, complete candidate/acceptance accounting and all reservations before combat.

The [native receipt](../TestResults/balance/tower-complete-family-setup-20260914/native-result.json) and [independent verification](../TestResults/balance/tower-complete-family-setup-20260914/independent-verification.json) agree on **43,879 recipes**, **560 anchors**, **145 registered history files / 102 distinct hashes**, and **481,603 reservations**. The required history includes the original unused v19 confirmation schedule. The canonical sorted reservation-union hash is `10915c9ff420d84850243103793d57e41b60de8d7cf42d2a2350159b7359daa1`. Existing history input size is **484,510,010 bytes**; this is read-only input, not new output.

Application, Common, Domain and Services.LL hashes match captured v19 exactly. The new harness hash is `3cd4700b22d01006467ef71d99531b02f8614eefcf1b0a8879676bf1c8d90de9`. Its retained diagnostic host exposes only the public setup inspector. A future launch host needs its own final executable/command binding; this inspection host cannot run confirmation. The existing controller, search, recipes, nominations, gameplay, durable fight accounting and caps were unchanged.

## Verification and measurements

| Check | Result |
| --- | --- |
| First test-wrapper invocation | **0.422 s**; Windows PowerShell 5.1 lacked the required two-argument path API; no build/tests ran |
| PowerShell 7 sandbox invocation | **1.469 s**; NuGet configuration access denied during restore; no tests ran |
| Repository test wrapper with normal host access | **34.468 s**, **71 passed / 0 failed / 0 skipped**, exact frozen name allowlist |
| Captured inspection-only build | **2.265 s**, zero warnings/errors; retained gameplay DLLs reused |
| Native setup inspection | **77.188 s command / 77.002 s native**; zero preparations/fights/new values |
| Independent check | **10.343 s command / 10.266 s verifier**; complete history/source/identity agreement |
| Final preservation | Stopped before the sealed-package audit on the unrelated UI README mismatch |

The successful test selection contains **19 new setup cases + 52 unchanged complete-family cases**, executed through `build/run-tests.ps1`. These cover independent allocator encoding vectors, historical/cross-stage rejection, bounded exhaustion, cancellation, changed allocator parameters, missing/new/tampered history, incomplete authority and no-overwrite behavior. Controller writers are synthetic; no combat integration cases were selected. The build reported 34 existing test-project warnings and zero errors. Original host/access failures and the corrected command receipts remain in the package.

The [native performance receipt](../TestResults/balance/tower-complete-family-setup-20260914/native-performance.json) records **75.469 CPU seconds**, **38,358,677,728 cumulatively allocated bytes** and **2,900,852,736 peak working-set bytes**. Full source binding took **18.054 inclusive seconds**; its canonical hashing took **12.939 seconds / 482,672 calls**. The remaining command time is not fully divided into named phases, so no precise registry/history percentage is claimed. This is a setup measurement, not a paired throughput comparison, a new discovery speedup or a full-study runtime estimate.

## Preservation stop and remaining work

The [preservation failure](../TestResults/balance/tower-complete-family-setup-20260914/preservation-failure.json) retains expected/current hashes for `docs/ui-rework-verification/README.md`. That unrelated file changed during the task; the frozen allowlist recognized frontend files and the top-level UI plan, but omitted this UI documentation path. The audit stopped on that mismatch before checking its seven prior sealed-package inventories. The original failed script, start marker and UI diff remain retained; the file was not edited by this task. **No preservation retry or resume occurred**, and the completed native/independent checks were not rerun. Final packaging records an incomplete preservation result rather than claiming that the full final audit passed.

Before advancing to any fresh allocation or full-study execution, resolve preservation under a separately frozen scope that accounts for the actual concurrent checkout work. Then a concrete external reservation wrapper, refreshed complete history, final runnable protocol/executable and exact accumulated setup charges remain necessary. No `seeds.json`, registered ledger, launch protocol or full-study execution was produced. The new setup request and measured costs are concrete retained inputs for that later binding; the 288 future values remain unallocated.

Full-family second-stage selection capacity, combat across the 256-cell outer batch boundary and full-study throughput remain unmeasured. All 43,879 cells and the fixed anchors remain required; no selection trimming, old-cap increase, confirmation rerun or gameplay application follows. Original v19 retains its sealed Unresolved/no-confirmation result; the later saved reliability result remains Fail 1/3 and adoption Hold.

## Reproducible commands and changed files

Executed from the repository root; retained failures are part of the sequence. Do not rerun these sealed output paths:

```powershell
$package = 'TestResults/balance/tower-complete-family-setup-20260914'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$package/prepare.py"
& $python -B "$package/workflow.py" tests       # Host API failure; no tests ran.
& $python -B "$package/tests-pwsh.py"           # Sandbox NuGet access failure; no tests ran.
& $python -B "$package/tests-host-access.py"    # 71 tests through build/run-tests.ps1.
& $python -B "$package/workflow.py" build
& $python -B "$package/workflow.py" freeze
& $python -B "$package/workflow.py" native
& $python -B "$package/workflow.py" independent
& $python -B "$package/closeout.py" freeze
& $python -B "$package/closeout.py" preserve    # Concurrent UI README mismatch; no retry.
& $python -B "$package/seal-incomplete.py"      # Packaging only; no diagnostic rerun.
```

Implementation changes are the [new setup inspector](../LL/tools/BalanceHarness/TowerCompleteFamilySetup.cs), [CLI dispatch](../LL/tools/BalanceHarness/Program.cs) and [new fixture tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerCompleteSetupTests.cs). Documentation changes are this review, its frozen protocol and seven active handoffs (discovery implementation/plan, acceptance policy, replication plan, strategy, retained-family design and harness README). Captured sources, build/test output, scripts, requests and receipts reside in the separate ignored TestResults package. No gameplay/content, migration, dependency declaration, shared configuration or deployment changed.

Final packaging charges the full **120-second preservation allowance** despite its early stop, plus the measured baseline/preparation/freeze/native/independent phases, **60 seconds static work** and the full **120-second seal allowance**. Diagnostic charges are **389.379 seconds / 6.49 minutes**, below 30 minutes. Correctness/build commands add **38.624 seconds** separately; their output is included. The final receipt records exact bytes for the package, whole changed files, shared TRX and **2 MiB metadata allowance**, below 4 GiB. These conservative accumulated costs must be retained in any future setup binding. Final source/document checks and inventory hashing package the incomplete result; they do not replace the stopped preservation audit. `evidence-files.json` is written last.
