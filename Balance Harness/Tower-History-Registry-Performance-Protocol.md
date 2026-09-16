# Seed-registry traversal performance: frozen zero-combat diagnostic

15 September 2026. Target: offline BalanceHarness. The user authorized proceeding after the comparison preparation timed out. This scope repairs and measures registry traversal; it does not retry the sealed comparison preflight or authorize combat, candidate generation or fresh seeds.

## Change and invariants

Replace per-entry `FileSystemInfo` construction with `FileSystemEnumerable`, allocating paths only for directories and the three exact, case-sensitive ledger filenames. Visit every directory and entry; preserve hidden/system entries, inaccessible-path failures, rejection of all reparse entries, queued-directory rechecks, the exact excluded subtree, case-insensitive returned path membership and the existing 2,000,000-directory bound. Check cancellation even for discarded ordinary files. Add aggregate counters and elapsed timing through `TowerPerformanceTrace`; retain counters on cancellation/failure. Ledger reading, hashes, reservation charging, gameplay and all search policies remain unchanged.

The implementation does not cache or prune historical packages. It does not change the failed preparation's 60-second cap or frozen source. It does not repair that package in place.

## Exact work and limits

New evidence: `TestResults/balance/tower-history-registry-performance-20260915`. Freeze this protocol, driver, audit/workflow/publication scripts, copied source, fixtures, reference scanner, build inputs and prior seals before diagnostic execution. Preserve the 38 predecessor packages before/after, including the failed comparison preparation. Record dirty checkout status and source hashes; edit only the registry implementation, its caller, tests and active Markdown.

- Build one isolated candidate executable and its tests with captured gameplay DLLs and cached restore metadata. No dirty gameplay rebuild or restore. Each build has at most 300 engineering seconds, separately reported.
- Run **131 backend cases once through `build/run-tests.ps1`**: the 118 preceding cases plus 13 registry cases. Test filesystem fixtures contain empty JSON objects, never balance seed allocations. A conservative 1-MiB output charge covers their temporary files, including deleted fixtures.
- Run one native measurement process. First run the captured reference scanner once, then the candidate once, on the same read-only sealed v19 `discovery` tree (9,216 candidate archives). Assert equal ledger membership and candidate file/directory counters against the sealed inventory. Save elapsed time, CPU time, allocated bytes, GC counts, working set and trace on success or failure. Fixed reference-first ordering is cache-biased; no cold-cache or whole-run speedup claim is permitted.
- In that same process, run the candidate **once over the full `TestResults/balance` registry**, excluding only this new diagnostic package. Save its complete path/hash map, separate enumeration/hash/union timings and reservation union. Require all **482,596** existing values, including v19's unused 512; save any membership discrepancy and stop. Do not prepare controls or invoke a battle engine.
- Run one independent Python audit. Enumerate the complete registry using `os.scandir` with the same exclusion and reparse/error rules. Compare exact path membership and every ledger hash with the native output; independently rebuild the reservation union and require 482,596. Require every prior successful-preflight registry entry with its original hash, allowing documented newer ledgers. Verify the 131-case TRX and archive inventory counters. This is a distinct implementation check, not another native attempt.
- Publish measured results, limitations and reproducible commands, update six active handoffs and seal success or failure evidence. On failure or a limit, stop dependent measurements; bounded closure may preserve evidence without retrying a diagnostic.

**Zero fights, preparations, replays, fresh seed values or retries.** New diagnostic time, including setup/freeze/tests/native measurement/independent audit/publication, at most **200 seconds**, within the unchanged cumulative **1,800 seconds**. Carry **1,288.051 seconds** from the sealed failure. Native measurement gets at most **140 seconds**, with five seconds reserved for cooperative cancellation/output inside that allowance. Independent audit gets at most 45 seconds; other diagnostic phases at most 30 seconds. Each phase also respects the smaller remaining package/global allowance. Do not use sleep-based timing tests. Persist process command, exit, elapsed time and errors; never extend a failed phase.

New evidence at most **256 MiB**, plus the conservative temporary-fixture charge, within **4 GiB cumulative output**. Carry the prior **1,664,956,540 bytes** plus the failed preparation's exact **75,968,161 bytes**. Do not increase any old experiment cap or modify a sealed package.

## Interpretation and next boundary

The v19 subtree gives a matched traversal comparison at representative archive scale. The full registry measurement establishes present cost and complete membership; the previous 77.781-second whole preflight is historical context, not a matched performance denominator. File count, directory count and allocations help distinguish work volume from elapsed time. A speed improvement must not be assumed before measurement.

Successful registry verification alone is not comparison readiness. Any later replacement preparation must separately freeze its exact work and use this measured cost within the remaining allowance; the failed package stays closed. No 45-value allocation or maximum-512-fight pilot is authorized here. No gameplay, Kharad/content tuning, ability-order optimization, default promotion, configuration, migration, deployment or 129,536-fight confirmation. Preserve v19's 253 recipes. Historical reliability Fail 1/3, deep recovery 0/3, v19 Unresolved and adoption Hold remain unchanged.
