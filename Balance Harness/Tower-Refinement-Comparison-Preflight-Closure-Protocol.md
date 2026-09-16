# Refinement preflight: bounded verification closure

16 September 2026. Target: offline BalanceHarness. Incoming [failure receipt](../TestResults/balance/tower-refinement-comparison-preflight-20260916/completion.json): **2,985.933469456 / 3,000 diagnostic seconds**, **14.066530544 seconds remaining**. This new scope allows **13 seconds / 96 MiB**, inside the unchanged cumulative **4 GiB** cap. Editing is excluded; builds, checks, audits and publication are charged. Zero fights, preparations, fresh values, replays and retries.

The prior harness compiled successfully (zero warnings/errors, root exit 0), but a descendant remained until the owned-job deadline. Its identity was not captured. Persistent build-server reuse is a hypothesis, not a proven diagnosis. Keep the failed scope sealed. Reuse its exact compiled harness only after checking its source, full binary inventory, successful compile log, root exit and zero-active-process cleanup. Do not rerun that build. The preflight C# implementation and eight tests remain unchanged.

For the first test-assembly build, use `--disable-build-servers -nr:false -m:1 -p:UseSharedCompilation=false`. Apply `DOTNET_CLI_USE_MSBUILD_SERVER=0` and `MSBUILDDISABLENODEREUSE=1` only in the diagnostic workflow process and its children. Do not shut down machine-wide servers or relax the requirement that the owned job empties. See Microsoft's [MSBuild server documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-server) and [dotnet build documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build).

Freeze in `TestResults/balance/tower-refinement-comparison-preflight-closure-20260916`. Execute each phase once, sequentially:

1. **Freeze**, at most 2 seconds: verify the complete immediate failed package and all its frozen consumed inputs, unchanged root C# and tests, reused process wrapper, successful compile and cleanup evidence. Copy the exact harness executable, request and eight-test source to this new scope. Pin sources, scripts, protocol, restore assets and the 482,821-value ledger. Save the dirty checkout before edits.
2. **Test-build**, at most 4.5 seconds: first compile of the eight-test assembly against the reused harness, with server reuse disabled. Persist the complete owned-job result before assertions; require no timeout, exit 0 and zero active descendants.
3. **Tests**, at most 4.5 seconds: run exactly the eight previously unrun facts through `build/run-tests.ps1 -NoBuild`, using isolated artifacts. Require all eight passed, zero failures and a fresh TRX matching this invocation. Synthetic inputs only.
4. **Captured**, at most 3.5 seconds: first execution of the sealed seed-free preflight against the captured inputs. Persist its binding, canonical controls and existing-infrastructure performance metrics. No engine or preparation.
5. **Audit**, at most 1.5 seconds: reuse the frozen independent preflight audit, check actual producing binaries and consumed input pins, then publish only on complete success.
6. **Publish**: three seconds reserved, including one closure second. Check unrelated dirty files, document links/whitespace and exact remaining resources; update six active handoffs and seal success or failure. Normal phases share **10 seconds**. Child cleanup stays inside each phase cap. Stop at the first failed or exhausted boundary. No retry, dependent execution or cap increase.

Recheck the immediate failed package completely and consumed inputs selectively; do not repeat the old 74-package historical audit or claim a live registry refresh. All earlier sealed evidence remains unchanged. Earlier 117 driver tests and six process-wrapper fixtures remain prior evidence, not newly executed tests. No search-strength or throughput claim follows from input binding.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-preflight-closure-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

On failure execute only `publish.py failure`. Future combat still needs an integrated live-registry/authorized-allocation launcher and explicit fresh-seed/resource authorization. Preserve all **482,821 reservations**, including the original unused 512 v19 values. V19 retains 253 recipes and Unresolved status; reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. No gameplay, ability-order, configuration, migration or deployment changes.
