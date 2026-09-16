# Refinement shared archive integration review

16 September 2026. **VerifiedRealSharedArchivePlumbing**. Real executable retention and all four schema-2 stage storage lifecycles passed. Independent inventories, content/gameplay hashes and nested byte accounting agree; both verifiers rejected a modified copied file. Zero battle batches, fights or new seeds.

The retained real executable contains **25 files / 18,390,518 bytes**. The four-stage fixture totals **20,922,763 bytes (19.95 MiB)**; the parent accountant and an independent full traversal agree exactly. Retention took **0.0823 seconds**. All four fixtures have zero charged/completed trials. Historical arrays were omitted only from these clearly marked storage fixtures; the complete real history remains in its original sealed ledgers. These are not complete adaptive discovery or balance archives.

Native elapsed **0.8898s**, CPU **0.6250s**, allocations **397,402,928 bytes**, peak working set **250,937,344 bytes**. Detailed existing-infrastructure timings are saved in [native metrics](../TestResults/balance/tower-refinement-storage-integration-20260916/native-metrics.json).

| Real storage stage | Setup + seal seconds | Reopen verification seconds | Bytes |
| --- | ---: | ---: | ---: |
| discovery-0 | 0.1065 | 0.0279 | 647,271 |
| discovery-1 | 0.0892 | 0.0259 | 647,299 |
| selection | 0.0883 | 0.0228 | 617,278 |
| confirmation | 0.0877 | 0.0231 | 617,486 |

The host used the preceding sealed harness and unchanged gameplay binaries with copy-local disabled. It called real `TowerBossStudy.RetainExecutable`, `TowerBulkCampaign.Open`/`Finish`, shared-reference verification and nested `TowerStorageAccountant` APIs. It did not call discovery/balance `RunAsync`, batch creation, actor preparation, seed allocation or combat. Each zero-batch fixture explicitly states that it is storage evidence only. One newly retained runtime-config file was altered, rejected by reference verification and the owner's mandatory audit, then restored byte-for-byte before final independent verification. Prior sealed executables were read only.

**Backend evidence:** reuse the exact **32/32 passing facts** run through `build/run-tests.ps1` in the [profile review](Tower-Refinement-Storage-Profile-Review.md). Their TRX and captured binaries were verified. No harness/gameplay source changed, no backend tests were added and none were rerun. The isolated native integration host is not an additional xUnit pass. Full adaptive-runtime parity, full backend suite and combat throughput were not run or established.

The earlier full-history component floor remains **76,555,348 bytes (73.01 MiB)**. Available output before publication was **95,835,455 bytes**, leaving **19,280,107 bytes above that floor** for other data. This is not a full-study upper bound. A prior full registry Check took 56.8624 seconds; the launcher needs three traversals across binding, recheck and execution. Different traversal work and cache states prevent treating three times that measurement as a proven lower bound. No complete time/output allowance or production request was fabricated.

Shared archive plumbing is verified. A real comparison still needs an explicit time allowance and a complete output envelope: remaining time has no demonstrated fit for three mandatory registry traversals plus combat. Do not spend the remaining allowance on repeated setup checks or request seeds before a concrete runnable comparison is ready.

| Completed phase before publication | Charged seconds |
| --- | ---: |
| audit | 0.3120 |
| build | 1.7340 |
| freeze | 0.2650 |
| native | 1.0940 |

Uncompleted phases: **none**. First failure:

```
None.
```

The [frozen protocol](Tower-Refinement-Storage-Integration-Protocol.md) limits this scope to **40 seconds / 24 MiB** under unchanged **3,240-second / 4 GiB cumulative caps**. Exact charged/remaining time and output are in the [completion receipt](../TestResults/balance/tower-refinement-storage-integration-20260916/completion.json). All fixtures are retained and counted. No fresh values, fights, preparations, retries, replays or registry traversal. Publication is charged, including one second reserved for closure.

Only the new diagnostic host/scripts/package, this protocol/review and six active Markdown handoffs changed. Harness/gameplay, configuration and migrations are unchanged; nothing was deployed. Unrelated dirty files were hash-checked. Preserve all **482,821 reservations**, v19's **512 unused values** and **253 recipes**, v19 Unresolved, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold. Fixed ability order remains.

Executed commands, each at most once; missing receipts indicate unrun phases. Never rerun this sealed path:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-storage-integration-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" native
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```
