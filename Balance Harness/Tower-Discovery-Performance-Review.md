# Discovery performance implementation and bounded diagnostic result

Completed engineering work **14 September 2026** in the offline `LL/tools/BalanceHarness`. **The accounting fix is implemented and 120 relevant tests pass. The complete paired-diagnostic completion gate remains open.** The single measured workload stopped at a writer-lease accounting defect after 42 fights; that defect was fixed and tested without repeating or extending the diagnostic.

Evidence lives in [the separate performance package](../TestResults/balance/tower-discovery-performance-20260914-implementation/). The [implementation plan](Tower-Discovery-Performance-Plan.md) and [frozen diagnostic protocol](Tower-Discovery-Performance-Diagnostic-Protocol.md) remain the basis for interpreting the result. This is actual implementation and measured evidence, not authorization for another balance study.

## Measured result

Each scale used exactly sixteen candidate writes in legacy mode followed by sixteen in owned mode. Both started with identical neutral eleven-file/six-directory archive prefixes. Each sample included the two inner boundaries; sample zero additionally included the outer 128-start boundary. Owned sealing also reconciled final archive files. All individual samples and counters are retained in [the scaling summary](../TestResults/balance/tower-discovery-performance-20260914-implementation/scaling-summary.json) and its linked `measured/scale-*.json` source files.

| Existing candidate archives | Legacy median | Owned median | Median ratio | Legacy tail | Owned tail |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 7.688 ms | 4.036 ms | 1.91× | 9.552 ms | 5.086 ms |
| 1,024 | 421.218 ms | 7.080 ms | 59.50× | 791.258 ms | 9.790 ms |
| 4,608 | 1,750.846 ms | 5.521 ms | 317.14× | 3,636.725 ms | 6.158 ms |
| 9,216 | 3,424.920 ms | 5.502 ms | **622.54×** | 11,416.700 ms | 6.157 ms |

“Tail” is the frozen nearest-rank p95, which is the maximum of sixteen samples. These small samples do not estimate a general production p95. OS cache and competing load were uncontrolled; legacy always preceded owned at each ascending scale. The legacy scanner used the original enumeration algorithm plus visit counters. The neutral fixtures are filesystem-shape measurements, not valid combat archives or a simulation throughput benchmark.

The scaling regression is also visible without timing: legacy candidate/checkpoint file visits grew from **2,827 → 374,539 → 1,675,531 → 3,348,235**. Owned candidate/checkpoint visits stayed at **539 at every scale**. Initialization and final audits are excluded from those checkpoint counts. The final lease fix adds bounded checks for the one active lease; deterministic operation-count tests still establish independence from the number of closed archives.

The broader lifecycle result is smaller and must be reported separately:

| 9,216-archive fixture stage | Legacy | Owned |
| --- | ---: | ---: |
| Sixteen candidate writes/checks | 67.566 s | 0.089 s |
| Accountant initialization | effectively zero | 8.636 s |
| Final audit | 1.756 s | 6.696 s |
| Total of these stages | **69.322 s** | **15.421 s** |

That is **4.50×** for the measured sixteen-write lifecycle, below a 5× target for this broader measure. The legacy fixture's final audit is a size scan; owned initialization/finalization additionally establish/check hashes. Actual campaign inventory creation and verification have further costs. Fixture construction at the final scale took another **39.008 s**, shared by both modes and excluded from the table; all fixture construction across scales took **85.430 s** and was included in the workload time limit. Full audits now dominate this short owned sample. They remain necessary correctness work; their amortization over a complete campaign was not measured. No whole-run speedup or revised v19 runtime is claimed.

The historical v19 totals remain **292.69 minutes / 86,016 fights**, including **283.63 minutes / 73,728 discovery fights**. Its old timing trace was not persisted. These new measurements strongly support the filesystem-scaling diagnosis, but cannot recover an exact historical percentage spent on storage checks, combat, preparation or serialization.

## Gameplay scope, parity and stop

The [baseline checkout receipt](../TestResults/balance/tower-discovery-performance-20260914-implementation/checkout-before.json) predates edits. A reference harness was compiled into an isolated artifact directory. The independently built candidate initially had different gameplay DLL hashes because compilation inputs/paths can differ; the diagnostic did **not** assume equivalence. Its execution directory copied every reference runtime dependency byte-for-byte, replacing only the harness DLL/PDB. The [frozen gameplay comparison](../TestResults/balance/tower-discovery-performance-20260914-implementation/frozen-gameplay-comparison.json) binds identical Domain, Common, Application and Services.LL assemblies. The runner checked all four hashes, runtime, OS and architecture before candidate combat, plus exact content/settings identity.

This is a **new diagnostic scope using the reference build of the then-current dirty checkout**. It does not establish v19 gameplay equivalence. Ten currently authored content files still differ from captured v19 content. A further **25 baseline files changed concurrently** during this task, including gameplay, equipment, UI and tests; [their receipt](../TestResults/balance/tower-discovery-performance-20260914-implementation/concurrent-checkout-changes.json) distinguishes them from this task's edits. They were preserved. The paired diagnostic used the retained reference gameplay binaries despite that drift. Final correctness tests describe the checkout compiled for that final test invocation.

Under an explicit no-combat guard, all **9,216** saved v19 evaluations reconstructed exactly in **11.805 s**: ordered recipes, proposals, evaluation charges, allocation decisions, rankings, complete generation result, shortlist and screened nominations. [Exact hashes are retained](../TestResults/balance/tower-discovery-performance-20260914-implementation/measured/saved-reconstruction.json). This replay of saved measurements was not a new search experiment and supplied no new balance evidence.

The real-combat protocol reserved **68** fights: two saved recipes, their same eight existing discovery seeds, two repetitions per version, and two detailed replays per version. The reference completed all **34** in **4.655 s**. The candidate completed **eight**, and their exact complete-report digest matched the corresponding reference case. Its independent compact archive passed immediate normal verification. Then the outer accountant rejected the active sibling `campaign.writer.lock`, which had not been declared as owned metadata. The [failure trace](../TestResults/balance/tower-discovery-performance-20260914-implementation/measured/candidate/performance.json) and [whole-workload receipt](../TestResults/balance/tower-discovery-performance-20260914-implementation/measured/diagnostic.json) retain the exception, matched digest, timings and counts.

**42 fights started and completed; zero combat retries; 26 scheduled fights were not executed.** The remaining candidate cases/repetition, candidate detailed replays and completed-campaign reconstruction did not run. The partial candidate campaign was not sealed as complete. Its first eight-fight child archive remains valid evidence. No observed diagnostic outcome was fed back into discovery or interpreted as balance evidence.

The final code declares direct-child writer leases and counts nested batch leases exactly once, including any bytes they contain. New tests exercise the real `AcquireWriter` stream, cap equality/overshoot and DeleteOnClose cleanup at both ownership levels. **This final lease correction is tested but was not followed by a new measured or combat run.** Therefore the report does not claim full parity or completion for the final executable.

Two earlier command/input preflights failed before entering the workload: missing CLI command registration, then PowerShell timezone normalization of saved timestamps. Both produced zero fixtures and zero fights. Their logs and original definitions are preserved. [The CLI amendment](../TestResults/balance/tower-discovery-performance-20260914-implementation/freeze-amendment.json) and [input amendment](../TestResults/balance/tower-discovery-performance-20260914-implementation/freeze-input-amendment.json) preceded the one measured workload and preserved its scales, recipes, seed values, order and limits. The executed definition is `diagnostic-definition-v3.json`, using `execution-v2/BalanceHarness.dll`.

## Implementation and ownership boundaries

The explicit opt-in is `owned-storage-v1`, selected with `--storage-accounting` for new compact campaigns or newly prepared feedback-family protocols. It does not alter old prepared packages. Legacy options omit the new nullable property; legacy feedback remains schema 1, while opted-in feedback freezes schema 2. Historical verifiers and default behavior remain available.

| Write boundary | Accounting and validation |
| --- | --- |
| Setup content, settings, executable and contracts | Initial complete inventory/hash snapshot. No shared mutable content cache is introduced. |
| Root results, replacements and journals | Root metadata is reconciled at checks; named writes and pending replacement paths are declared. Unexpected root files fail. |
| New candidate archive | One explicit active subtree; checks count all its files, including hidden, pending and compressed artifacts. On success, normal exact compact verification precedes cached sealing. |
| Attempt journals | Existing charging-before-combat and durable flushes are unchanged. Both inner and outer journal/flush timings are recorded. No combat retry is added. |
| Chunk publication | Existing inventory/hash checks and publication behavior remain unchanged. Partial/failed writers cannot finalize an owned campaign. |
| Sibling writer lease | Direct-child lease metadata is declared; nested active batch lease bytes are checked separately without double counting. |
| Nested campaign | Parent attaches the live child and includes its total exactly once. Closing a child audits and incorporates its durable expectations; no growing parent scan at 128 starts. |
| Completion | Full independent inventory/hash audit; pending completion inventory bytes count against the cap before the completion marker is published. |
| Failure/cancellation/reopening | Preserve partial files and durable journals. New diagnostic and opted-in feedback traces persist failures. Owned execution rejects resume; byte reconstruction alone grants no execution authority. |

The new contract explicitly defers detection of external changes inside **closed** subtrees until mandatory lifecycle audits. Those audits detect changed/missing/extra files, unknown directories and reparse points before valid completion. Active-subtree checks retain their existing boundaries. A writer lease does not prevent unrelated processes from modifying files. The cached total is never used as archive-integrity evidence. This is a versioned validation-timing change, not a silent weakening of legacy behavior.

`TowerPerformanceTrace` now also records file/directory visits. The actual bulk path measures batch time and the adaptive path has an exclusive construction/ranking envelope. Existing preparation, simulation, serialization/compression, publication, verification and durable-flush stages are retained. The partial candidate's engine simulation alone measured **913.78 ms** across eight fights; no extrapolation is made from that cold partial case. Parent workload metrics were **236.594 CPU seconds**, **71,476,009,632 allocated bytes**, and **1,718,554,624 peak working-set bytes**; allocations are cumulative, not retained memory. Child-process metrics remain in the reference benchmark. Nested inclusive timings must not be added together. Feedback snapshots explicitly exclude their own subsequent trace serialization/final publication work; diagnostic lifecycle samples measure audits separately.

## Verification, preservation and commands

- **120 tests passed**, zero failed/skipped, through the [final wrapper command/log](../TestResults/balance/tower-discovery-performance-20260914-implementation/tests-final.log), with [TRX evidence](../TestResults/balance/tower-discovery-performance-20260914-implementation/tests-final.trx). Coverage includes constant operation counts, limits, pending/partial output, replacement accounting, journals, actual leases, cancellation, links, nested totals, unknown/modified artifacts, manifest tampering, legacy serialization, deterministic search and compact compatibility. Existing compilation warnings remain; no new warning was introduced by the final changes.
- The final candidate verifier read a historical v19 compact archive successfully: **eight saved trials, zero new combat**. [Receipt](../TestResults/balance/tower-discovery-performance-20260914-implementation/historical-archive-verification.json).
- Independent read-only inventory/hash verification matched **103,193 campaign files** and **892 work-package files**, taking approximately **20.28 s**. The [preservation receipt](../TestResults/balance/tower-discovery-performance-20260914-implementation/sealed-preservation.json) also confirms **480,707 distinct reservations**, including all **512 unused confirmation values**.
- The measured workload took **246.045 seconds** wall clock, comfortably below 30 minutes even including the separate preservation/archive checks. At the [resource snapshot](../TestResults/balance/tower-discovery-performance-20260914-implementation/resource-summary.json), measured output contained **103,023 files / 67,935,938 logical bytes**; all new build/source/output evidence totaled approximately **624 MB**, below 4 GiB. The neutral fixture contains exactly **101,376 files**. Logical bytes use the existing infrastructure's convention; filesystem allocation/metadata overhead is not separately measured.

Exact executed commands, from the repository root:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --artifacts-path TestResults/balance/tower-discovery-performance-20260914-implementation/build/reference --nologo

./build/run-tests.ps1 -Configuration Release -ArtifactsPath TestResults/balance/tower-discovery-performance-20260914-implementation/build/candidate -Filter 'FullyQualifiedName~BalanceHarnessTowerPerformanceTests|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerSearchPortfolioTests|FullyQualifiedName~BalanceHarnessTowerFeedbackBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerStorageTests'

dotnet TestResults/balance/tower-discovery-performance-20260914-implementation/execution-v2/BalanceHarness.dll tower-discovery-performance --definition TestResults/balance/tower-discovery-performance-20260914-implementation/diagnostic-definition-v3.json --output TestResults/balance/tower-discovery-performance-20260914-implementation/measured

./TestResults/balance/tower-discovery-performance-20260914-implementation/verify-preservation.ps1

dotnet TestResults/balance/tower-discovery-performance-20260914-implementation/build/candidate/bin/BalanceHarness/release/BalanceHarness.dll tower-compact-verify --run TestResults/balance/tower-search-portfolio-20260914/discovery/batches/discovery-000000
```

The measured command returned **2** for the recorded lease defect and must not be rerun into the sealed diagnostic scope. Reproduction of a future measured run requires a separate frozen definition/output with explicit accounting for every repeated fight. The initial sandbox build could not read the existing user NuGet configuration; the isolated build/test commands subsequently succeeded with tool-level filesystem access. No required correctness command remains blocked. No diagnostic stage was retried after workload execution started.

## Changed files and remaining gate

This task changed `TowerBulkCampaign.cs`, `TowerFeedbackBenchmarkRun.cs`, `TowerCompactDiscovery.cs`, `TowerPerformanceTrace.cs`, the attempt timing in `TowerFinalistRescreenStudy.cs`, and CLI registration in `Program.cs`; added `TowerStorageAccountant.cs`, `TowerDiscoveryPerformance.cs` and `BalanceHarnessTowerStorageTests.cs`; and added isolated-artifact support to `build/run-tests.ps1`. Active README/performance/replication handoffs now point to this result and its limits. Before/after source receipts and separate concurrent-change receipts distinguish this task from the pre-existing dirty work.

The remaining engineering gate is **complete bounded paired parity/replay and completion verification for the final lease-corrected executable under a separately frozen diagnostic**, before a large study. The broader 5× lifecycle target was not achieved by the sixteen-write sample. Full hash audits are now the largest measured owned-bookkeeping cost; do not remove them or infer whole-run throughput from the checkpoint ratio.

V19 remains **VerifiedCapacityExceeded**, with all **253 recipes retained**, **zero confirmation fights**, reliability **Unresolved**, adoption **Hold**, ordinary/joint assessment **NotRun / NotRun**. The proposed **129,536-fight** confirmation was neither prepared nor started. All **480,707** reservations remain excluded from fresh balance work. No gameplay/content edit, Kharad tuning, migration, production configuration change, deployment, catalog/default promotion or old-cap increase was made by this task.
