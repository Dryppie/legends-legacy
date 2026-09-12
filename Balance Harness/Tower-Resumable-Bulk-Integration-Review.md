# Resumable Tower bulk integration

Completed 12 September 2026. This increment connects compact storage and prepared execution to actual CLI discovery and frozen balance-family execution. It changes resource use and interruption handling, without changing game content, team budgets, generator policy, ranking or statistical acceptance.

**270 regression checks passed.** A separately frozen, retained **22-fight floor-5 smoke run** passed interruption/resume, exact discovery reconstruction, independent balance evaluation and two detailed replays. It is an integration diagnostic, not a competitive search or new floor-balance conclusion.

## Implemented behavior

`TowerCompactBundle.Verify` validates full original report hashes while retaining only one compressed chunk's reports at a time. Prepared descriptions are cached only within that chunk. It returns lightweight outcomes, result digests and identity metadata; the existing `ReadSaved` compatibility API still explicitly materializes all reports. Recipe/input and schedule/hash metadata remain in memory, so this is a bounded full-report working set, not an absolute resident-memory cap. Bulk evaluator mappings verify a bundle once per evaluation. Selected replay keeps only its selected original report and independently runs fresh production preparation.

New compact archives freeze `bulk-resume.json` before combat and flush `bulk-attempts.jsonl` before each attempted fight. `--resume true` requires the identical definition, ordered recipes/seeds, content, settings, producing execution, mode and reservation. It verifies the committed prefix and preserves committed chunk bytes. Missing/pending chunks, torn journals, incompatible inputs and concurrent writers are rejected. Earlier complete archives remain readable; pre-receipt archives cannot use this resume mechanism.

A cancelled/crashed attempt or a completed but uncommitted fight still consumes the original reservation. Retrying it requires remaining headroom; retries never enlarge the statistical sample. A completed resume verifies without fighting. Receipts establish consistency with frozen inputs and hashes; they are not externally authenticated evidence. Verification always rechecks saved data rather than trusting a path, timestamp or previous verification result.

The new campaign paths are opt-in:

- `tower-boss-discover --archive-format tower-compact-v1` supports schema-3 independent discovery and explicitly supplied-build improvement. It uses the existing adaptive generator, reconstructs earlier decisions from verified reports and resumes missing work. Scalar observations preserve the existing fitness and behavior calculations. Reference ancestry remains explicit.
- `tower-balance-run` executes an already-frozen `TowerBalanceDefinition`, preflighting every cell before combat. It runs the complete family through compact prepared batches, then uses the unchanged evaluator. It writes ordinary source mappings for independent `tower-balance-evaluate` checks. It does not select or apply boss scaling.
- `tower-boss-discovery-verify` detects both legacy and compact discovery. `tower-balance-run-verify` independently reconstructs compact confirmation results without new combat.

Campaigns freeze content and producing executables once. Child archives reference the owning campaign's content instead of copying it per candidate. Keep the whole campaign together when moving or retaining it. Each case still owns its own prepared executor; there is no global mutable combat cache. The CLI, formats, exports and detailed replay commands are documented in the [README](../LL/tools/BalanceHarness/README.md#resumable-compact-discovery-and-calibration).

## Resource limits and recovery boundaries

Defaults are chunk size 32, retry reserve 32, 300 seconds of active work per invocation and 2 GiB of campaign output. Planned trials plus retry reserve must fit the definition's combat cap. The active timer starts after initial contract/content/executable setup and also covers subsequent verification; synchronous setup or in-flight work may exceed the limit. Explicit resume starts another bounded invocation. Attempt and storage caps persist across resumes. Storage checks occur at batch/chunk boundaries, with possible overshoot from setup, one chunk and final metadata.

Campaigns run sequentially. A clean interruption at a committed boundary repeats no fights. An interrupted partial chunk may require repeating its uncommitted work within the existing reserve. Corrupt, torn or pending writes need explicit recovery; this implementation never silently drops them, increases a budget or changes a seed schedule to make a run finish.

Discovery reports count completed logical trials. `campaign-accounting.json` separately records charged attempts and retry/uncommitted work. `shortlist.json` saves usable normal Tower scenarios, but discovery candidates require fresh confirmation before promotion to retained controls. Full `tower-boss-study`/Tower Lab integration, automatic catalog promotion, parallel campaign workers and a new statistical stopping policy remain separate work.

## Retained floor-5 integration check

The [protocol](../TestResults/balance/tower-bulk-integration-20260912/protocol.json) froze the producing build, source hashes, schedules, fixed ten-character/five-untrained-Essence budget, driver and 22-combat limit before the successful diagnostic workload. Its SHA-256 is `981efa14b0b05cb0893392db938178e45023ed423fcdc869047c74c2e27523d2`.

| Work | Fights | Result |
| --- | ---: | --- |
| Uninterrupted legacy discovery | 8 | Complete |
| Compact discovery, interrupted after its first two-trial chunk and resumed | 8 | Same proposals, fitness, behavior and shortlist; no repeated attempts |
| Compact balance family, interrupted after two trials and resumed | 4 | Same assessment as independent evaluation; no repeated attempts |
| Fresh detailed replays | 2 | Both matched |
| Total | **22** | Complete |

The verifier buffered at most **two full reports** in this deliberately two-record-chunk smoke. The first committed discovery chunk retained its exact bytes. Discovery charged 8 attempts for 8 logical trials; confirmation charged 4 for 4. The four-sample saved-control result was 1/4 wins and correctly **Inconclusive**, with an adjusted interval approximately 4.56–69.94%. It cannot establish the desired 10–50% range.

The complete smoke package, including its driver build, copied dependencies, archived campaigns and detailed replays, occupied approximately **71.8 MiB** before final review metadata. The [metrics](../TestResults/balance/tower-bulk-integration-20260912/metrics.json) record all counts, equality checks and diagnostic durations. Legacy discovery took 0.91 seconds; compact discovery including interruption, resume and full re-verification took 8.39 seconds; the confirmation path took 7.84 seconds. These paths performed different setup and verification work and are **not a throughput comparison**. Fixed setup and verification costs dominate a workload this small. No campaign speedup is claimed from these figures, and the earlier prepared-mode 27% improvement remains specific to its [sealed benchmark](Tower-Prepared-Execution-Review.md).

## Verification

Backend checks ran through the repository wrapper:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -m:1
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerPerformanceTests|FullyQualifiedName~BalanceHarnessJsonTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryRunTests|FullyQualifiedName~BalanceHarnessTowerBossStudyTests|FullyQualifiedName~BalanceHarnessTowerBossImprovementTests'
git -c core.safecrlf=false diff --check
```

The final build had zero errors and five existing unrelated warnings. The [final TRX](../TestResults/tower-bulk-tests-20260912.trx) records **270 passed, zero failed/skipped**, in approximately 85 seconds. New checks cover both execution modes, lost-attempt accounting, exhausted reserves, changed identities/settings/seeds, corrupt chunks, torn journals, pending writes, streaming cancellation, exact discovery/resume parity, supplied-build ancestry, shared-content tampering, storage limits, writer leases, detailed replay and independent balance evaluation.

The first focused pass had 48 passing checks. The initial expanded pass had 269 passes and one invalid supplied-team fixture: its character identities differed from the frozen discovery contract. Correcting that fixture produced the final 270-pass run; no gameplay relaxation was needed. Diagnostic driver setup also encountered sandbox access to NuGet configuration, a duplicate anonymous-property name and a rejected neutral-ID fixture before any smoke combat. These were resolved; the driver ultimately built with no errors/warnings using existing local harness assemblies. Its empty-source NuGet configuration belongs only to the retained diagnostic driver. No required verification remains blocked.

The [final verification receipt](../TestResults/balance/tower-bulk-integration-20260912/final-verification.json) confirms **5,146 protected historical files matched**, all 16 live content files matched the protocol, producing assemblies/source hashes stayed unchanged after the smoke, and prior increment reviews were preserved. Saved catalogs and boss settings were not edited. No large search, migration, deployment or external environment change was performed.

## Changed files and next step

| Area | Files |
| --- | --- |
| Streaming archive verification and resume | `TowerCompactBundle.cs`, new `TowerCompactResume.cs`, `TowerBalanceRuns.cs` |
| Owning campaign, adaptive discovery and frozen balance execution | New `TowerBulkCampaign.cs`, `TowerCompactDiscovery.cs`, `TowerCompactBalanceRun.cs` |
| CLI and legacy discovery-verifier dispatch | `Program.cs`, `TowerBossDiscoveryRun.cs` |
| Regression coverage | New `BalanceHarnessTowerBulkTests.cs` |
| Active documentation | Harness README, automatic discovery plan/implementation, boss-specific plan, harness plan, Essence search plan and acceptance policy |

The next useful workload is a **bounded competitive challenger pilot** at the current fixed budget, including independent restarts and explicitly labeled improvement of saved controls. Record complete campaign resource use, especially setup, durable journaling and repeated verification costs, before increasing its size. Then freeze fresh selection/confirmation against the strongest challengers. Kharad's historical portfolio balance Pass and search-quality Fail remain unchanged; near-optimality and broader-floor competitive balance still require stronger evidence.
