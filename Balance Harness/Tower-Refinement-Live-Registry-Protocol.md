# Refinement launcher: real registry check

16 September 2026. **Prepared; diagnostic budget approval pending.** Target: offline BalanceHarness. This package checks the real saved inputs and complete reservation registry through the already verified launcher. It cannot allocate values or invoke combat. No change to gameplay, ability order, selection, deployment or the sealed v19 experiment.

## Exact scope and authorization boundary

The [last receipt](../TestResults/balance/tower-refinement-launcher-20260916/completion.json) records **3,023.714469456 / 3,030 seconds**, leaving **6.285530544 seconds**. Request **210 additional diagnostic seconds**, raising the cumulative ceiling to **3,240 seconds**. This package itself is capped at **210 seconds**, including build, verification and publication; it does not spend the remaining 6.286 seconds as an extra scope allowance. Normal phases share 205 seconds; five seconds are reserved for publication. The cumulative **4 GiB** output cap stays unchanged; new package output is capped at **16 MiB**. Editing and preparing this package are outside the diagnostic clock; executing it is not.

**Zero new seeds, fights, combat preparations, replays and retries.** A budget approval for this check is not a production allocation or combat authorization. Preserve all **482,821 reservations**, including v19's 512 unused confirmation values. V19 retains all 253 required recipes and Unresolved status. Later reliability Fail 1/3, deep recovery 0/3 and adoption Hold are unchanged.

The native complete traversal previously took **57.467 seconds**, plus 3.607 seconds for hashing/union. The independent traversal took **85.484 seconds**, plus 8.281 seconds for hashing/union. These are timing estimates from [native](Tower-History-Registry-Performance-Review.md) and [independent](Tower-History-Registry-Independent-Closure-Review.md) historical measurements, not new results or directly comparable end-to-end launcher timings.

One preapproval static readiness check is frozen at **two seconds** inside the existing 6.286-second remainder and the same 210-second package allowance. `readiness.py` checks script syntax, frozen hashes, dirty-file preservation, whitespace and local links. It does not build, traverse the real registry or call the launcher. Its receipt carries forward measured time; failure stops this package. All phases below still require the separate budget approval.

## Frozen inputs and commands

Package: `TestResults/balance/tower-refinement-live-registry-20260916`. `prepared-files.json` binds the scripts, host, request and this protocol. `budget-request.json` records the exact proposed allowance. Execution requires a separate matching `diagnostic-authorization.json` recording explicit user approval; the prepared package contains no such authorization.

Reuse the exact sealed `tower-refinement-launcher-20260916/executable` assemblies, and its ten passing backend tests through `build/run-tests.ps1`. Do not rebuild gameplay or the launcher. A small isolated host references those assemblies without copying them. Reuse the independently verified reader's exact `reader.py`, six reader fixtures and the existing Windows process-tree wrapper. Hashes of consumed assemblies, root harness source, preflight pins and prior evidence are checked before and after execution. Reused tests are prior evidence, not new test executions.

`request.json` embeds the unchanged real preflight request: deep-challenger captured content/definition/campaign, the current core-portfolio ledger and the historical registry snapshot. The snapshot is only a required subset, never the complete membership oracle. It pins the sealed launcher's execution identity. The nonexistent direct-child study path is excluded by exact path only and remains absent. Its master and launch-envelope fields satisfy the existing request schema but are never used for derivation or execution; this check request must not be reused as a combat authorization.

Run once, in order, after approval:

1. **Freeze, at most 4 seconds.** Verify the immediate sealed package, all consumed input pins, current source identity and saved test receipts. Validate prepared scripts and capture the approved envelope. Carry forward exact prior retained and temporary bytes. Do not repeat the old full historical archive audit.
2. **Host build, at most 6 seconds.** Existing restore assets, no restore, build/node servers disabled. No backend test rerun because the launcher and gameplay assemblies are unchanged.
3. **Native check, at most 90 seconds.** Call only `TowerRefinementComparisonLaunch.Check` once, with its own 85-second cancellation limit. Persist full live path/hash membership and nested preflight result, request hash, elapsed/CPU/allocation/peak-memory measurements on success or failure. The launcher's no-combat guard remains active. Its current public API does not expose internal trace stages; native time is end-to-end and must not be presented as enumeration-only time.
4. **Independent reader, at most 110 seconds.** Fully enumerate the same registry, including hidden files and rejecting reparse points, with no archive pruning or historical-path whitelist. Independently compare complete membership, every hash and the exact integer union against native results and the authoritative ledger. Recheck hashes after reading; reject Pending or unknown reservation states. Persist progress every 65,536 entries, discrepancy evidence and phase timings even on failure. The native result's nested preflight still says a live refresh is required because it represents the earlier snapshot component; only the outer check and independent comparison establish this refresh.
5. **Audit, at most 4 seconds.** Recheck consumed inputs, successful process cleanup, preserved dirty files and absent study path. Verify zero-workload results and reservation count. Do not rescan the entire registry a third time.
6. **Publish, five seconds reserved.** Record success or first failure, skipped phases, exact measured time and retained bytes. Update active handoffs, check links/whitespace and seal evidence. Stop dependent diagnostics on the first failure; no retry, cap reset or automatic extension. All child-process cleanup is inside each phase envelope.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-live-registry-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" native
& $python -B "$work/workflow.py" independent
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

On first failure, skip dependent commands and use only `publish.py failure`. Do not rerun this path. Long scans run in an owned Windows job with a hard deadline and durable process receipts. Membership equality across two scans is evidence for this interval, not an atomic snapshot against arbitrary concurrent writers. Any later production allocation must revalidate history and receive its own exact fresh-value/combat authorization and resource envelope. Actual compact-runtime integration and search-strength improvement remain unmeasured here. No migrations or configuration changes.
