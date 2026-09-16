# Refinement comparison driver: frozen remaining verification

16 September 2026. Target: offline BalanceHarness diagnostic infrastructure. Proceed after the sealed test timeout. Incoming [receipt](../TestResults/balance/tower-refinement-comparison-driver-20260916/completion.json): **2,950.823469456 / 3,000 diagnostic seconds used**, **49.176530544 seconds remaining**. This separate scope is capped at **45 seconds / 32 MiB**, within the unchanged cumulative **4 GiB** cap. Carry the predecessor's actual sealed output plus its **16 MiB** temporary/shared-test allowance. No reset, retry, combat, preparation, replay or fresh value.

The preceding two builds and all 117 assertions passed, although the test phase exceeded its deadline. Preserve that scope as FailurePreserved. This scope verifies exact current source, captured binaries and the distinct late TRX against its seal, and **reuses those passing assertions without rebuilding or repeating tests**. This is justified because no C# controller/model/generator/test changes are made. The original backend command was `build/run-tests.ps1 -NoBuild -ArtifactsPath ... -Filter ...`; its exact sealed command and TRX remain evidence. Reusing them does not relabel the failed bounded run.

## Implementation and diagnostics

Add `build/bounded_windows_process.py`. Create the root process suspended and assign it to an owned Windows job before resuming. Disable breakaway, enable kill-on-close, use direct owned handles to terminate on timeout, check all API results, and require zero active job processes before returning. A root-process exit alone cannot establish completion. Preserve timeout/exit/cleanup/process-count receipts. The implementation follows Microsoft's [job-object documentation](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects) and [termination API](https://learn.microsoft.com/en-us/windows/win32/api/jobapi2/nf-jobapi2-terminatejobobject). A catastrophic host exit before assignment can leave a suspended child; normal exception cleanup terminates that child without resuming it. The supported diagnostic tree uses ordinary CreateProcess descendants; no WMI launcher or breakaway path.

Evidence directory: `TestResults/balance/tower-refinement-comparison-driver-closure-20260916`. Freeze source, scripts and protocol before execution. Execute once, sequentially:

1. **Freeze:** exact consumed predecessor-file, current source, executable/test binary, entry-point, ledger and 117-test evidence verification. Preserve dirty-checkout hashes.
2. **Process fixtures:** six Windows checks: quoted/Unicode arguments and stdout/stderr with normal exit; nonzero exit 7; root/child/grandchild timeout; already-exited root with a lingering child; expired deadline rejected before launch; missing executable rejected before launch. Each timeout must show zero active job processes and the expected complete process count. Only harmless short Python sleepers/prints are launched. Both intentional work timeouts are one second, with at most one cleanup second. They are successful negative tests, not retries.
3. **Captured controller:** run the unchanged compiled native diagnostic for its first execution, writing only into this new directory. Three fabricated cases: complete; 14/16 baseline (stop before candidate); 14/16 candidate (stop before screening). Preserve all traces, gate receipts, synthetic attempt journals and reconstruction of the complete case. No generator, engine or materialization call.
4. **Independent audit:** run the unchanged unexecuted Python audit against these artifacts. Check schedules/input equality, counts, ranking, selection/deduplication, paired outcomes, exact journals/manifests and the 482,821 reservation union.
5. **Preserve:** one mandatory full audit of **73 predecessor packages**, including the latest failed scope, retaining every legacy assertion. Use the unchanged four-worker hash helper and a fresh per-pass digest cache. Run it inside the owned process wrapper as well as enforcing its internal deadline.
6. **Publish:** only all successful receipts permit VerifiedRefinementComparisonDriver. Otherwise preserve the first failure and skip dependent work. Update six active Markdown handoffs, check unchanged unrelated work, links and whitespace, record actual costs and seal. No speedup or combat-strength claim from synthetic fixtures.

The phases before preservation share **15 seconds**: freeze up to 6, process fixtures up to 7, captured controller up to 9 and audit up to 4. Preservation receives at most 35 seconds, also bounded so ordinary phases stop by 41 total seconds. Reserve four seconds for success/failure publication, including one charged closure second. For every subprocess, reserve cleanup inside its phase deadline. Missing termination proof is a failure. Source editing is excluded; all executed diagnostics/audits/publication are charged. If any phase fails, execute only `publish.py failure` afterward.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-driver-closure-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" process-tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/workflow.py" preserve
& $python -B "$work/publish.py"
```

Never rerun against a sealed or failed directory. All old experiment caps, files and seeds stay unchanged. Preserve 482,821 reservations including the original 512 unused v19 values; v19 has all 253 recipes and Unresolved status. Reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. Real compact-runtime integration and combat still require separate frozen preparation and fresh-seed authorization. No gameplay/configuration change, ability-order tuning, migration or deployment.
