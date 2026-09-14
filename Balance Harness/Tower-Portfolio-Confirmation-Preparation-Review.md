# Captured-v19 confirmation — prepared, verified and unstarted

Completed **14 September 2026** in the offline BalanceHarness. The separate **253-recipe confirmation study is frozen and verified**. It reserves **512 fresh shared seeds** and plans **129,536 fights**, with limits of **four hours, 8 GiB and zero retries**. **Zero fights ran during preparation. Execution remains unstarted and requires separate authorization.**

The [frozen plan](Tower-Portfolio-Confirmation-Plan.md), [prepared protocol](../TestResults/balance/tower-portfolio-confirmation-20260914/protocol.json) and [preparation evidence](../TestResults/balance/tower-portfolio-confirmation-preparation-20260914/) make the next execution concrete. The original user scope prohibited starting confirmation; the subsequent instruction authorized this preparation step only. No confirmation run or completed-study reconstruction command was invoked.

## Frozen inputs and reservations

The study uses the reviewed captured-v19 runtime with harness SHA-256 `0e0f72505454303c72d0bf253fa5f6665d0504505e5c4cfd5e94e438f9ef501f`. All four gameplay assemblies, the runtime/OS/architecture, the 16 bound content files and combat settings match the original scope. The current checkout build is used for correctness tests only. It does not replace the captured executable.

All **253 exact ordered recipes and origins** are preserved: **112 controls and 141 generated recipes**, including all **119 ceiling-only additions**. Both sets of six original/screened nomination groups, primaries and secondaries, the three roots, fixed anchor and stronger control are unchanged. The ordinary/joint family rules and numerical 2/3 reliability gate remain as implemented and frozen. Captured Kharad Health **3.5366243328** and Power **4.4702934848** remain unchanged.

The [history survey](../TestResults/balance/tower-portfolio-confirmation-preparation-20260914/history-survey.json) found no intervening reservations outside the v19 union. Master seed **2026091420** produced exactly 512 unique, disjoint shared values in 512 allocator attempts. An independent Python SHA-256/32-bit implementation reproduced the exact order. Every cell uses that same array; no prior fights are pooled.

The authoritative [new seed ledger](../TestResults/balance/tower-portfolio-confirmation-20260914/seed-ledger.json) contains **481,219 reservations**: all **480,707** prior values plus 512 new values. Both the **512 unused v19 confirmation values** and the **512 new unused values** remain permanently excluded from subsequent fresh studies. The original ledger is unchanged. Do not prepare this study again or release its seeds if it never runs.

| Frozen artifact | SHA-256 |
| --- | --- |
| Protocol | `18613ccee673347b619a8e742c770f87bf7d224351a33b9d9665e03e6b43e6e1` |
| Seed ledger | `8033fe0a8169e05e236866451ac6b87a4445230998bf2114e1be67004c98e7bd` |
| Complete definition | `099edf3287963cb265d30d1bf3170312a18a6b729d40e43653937e37ed985d3e` |

## Verification and measurements

| Check | Result | Measured seconds |
| --- | --- | ---: |
| History survey, checkout snapshot and pre-allocation freeze | Passed | 5.339 |
| One production preparation, including its built-in verification | Exit 0 | 17.742 |
| One prepared check using the study's retained executable | Exit 0; zero fights | 5.355 |
| Independent allocation/definition/inventory check | Exact | 0.624 |
| Complete preservation pass over five sealed packages | Exact | 51.572 |
| Corrected independent command including preservation | Exit 0 | 52.372 |
| Relevant backend wrapper run, including build | 201 passed, 0 failed, 0 skipped | 51.516 |

The independent check matched the entire prepared inventory, every recipe, role, shared schedule, source copy, nomination/anchor identity, gameplay DLL and bound content file. It also verified the absence of `started.json`, `attempts.bin`, `confirmation/` and completion/failure markers. The study contains **65 files / 111,184,818 logical bytes (106.03 MiB)**. The pre-sealing work-plus-study snapshot was approximately **473.4 MB**, below the separate 1-GiB preparation bound. Final exact bytes, timing bounds and hashes are in [final verification](../TestResults/balance/tower-portfolio-confirmation-preparation-20260914/final-verification.json).

One independent verification failed before the corrected result. Its script omitted the established `content/Data` directory when explicitly checking content hashes; the production prepared verifier had already passed. The [traceback](../TestResults/balance/tower-portfolio-confirmation-preparation-20260914/independent-command.log), [failure receipt](../TestResults/balance/tower-portfolio-confirmation-preparation-20260914/independent-failure.json) and [pre-invocation amendment](../TestResults/balance/tower-portfolio-confirmation-preparation-20260914/content-verifier-amendment.md) preserve that evidence. One additional read-only independent check used the corrected paths. The frozen study and seeds were unchanged; preparation and combat were not repeated. An earlier field-name typo in the verification script was corrected and recorded before its first invocation. Original scripts and freezes remain retained.

The failed tool call took 0.849 seconds, charged conservatively as one second. The non-overlapping measured freeze/prepare/check/corrected-verifier commands plus that charge total **81.808 seconds before sealing**. Small orchestration/input-recheck costs are not individually timed; the final receipt supplies a conservative allowance and sealing bound, remaining below **900 seconds**. Build/correctness-test time is excluded. These are preparation measurements, not combat throughput or a new performance speedup.

The [preservation receipt](../TestResults/balance/tower-portfolio-confirmation-preparation-20260914/sealed-preservation.json) independently matched **315,342 indexed files** across v19 campaign/work, both performance packages and the prior confirmation implementation package. All five index hashes remain exact. The performance diagnostic count remains **76 fights**, with zero new diagnostic fights in this task.

## Reproducible commands and next boundary

The actual preparation arguments and frozen hashes are retained in [preparation-definition.json](../TestResults/balance/tower-portfolio-confirmation-preparation-20260914/preparation-definition.json). These commands describe the completed work; do not rerun write-producing scripts into sealed evidence paths:

```powershell
$w = 'TestResults/balance/tower-portfolio-confirmation-preparation-20260914'
$d = Get-Content "$w/preparation-definition.json" -Raw | ConvertFrom-Json
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'

& $py "$w/prepare-study.py" freeze
& $py "$w/prepare-study.py" prepare
& $py "$w/verify-study-v3.py"
./build/run-tests.ps1 -Configuration Release -ArtifactsPath $d.testArtifacts -Filter $d.testFilter
```

The required test wrapper passed all **201 cases**, including the 39 new contract cases already implemented in the preceding task. No harness or test source changed during this preparation. The build had 34 existing warnings and zero errors. Required user-NuGet-config access was available through the authorized tool invocation; no configuration was rewritten. The ordinary `python` alias was unavailable, so the bundled Python executable above was used. No required command remains blocked.

After separate execution authorization, use the prepared package's own executable:

```powershell
$study = 'TestResults/balance/tower-portfolio-confirmation-20260914'
dotnet "$study/executable/BalanceHarness.dll" tower-portfolio-confirmation-run --run $study
# Only after a complete run; invoke with the frozen 900-second reconstruction bound:
dotnet "$study/executable/BalanceHarness.dll" tower-portfolio-confirmation-verify --run $study
```

The future invocation must retain full-command timing/logs outside the prepared study in a new execution-work directory and enforce that reconstruction timeout. The harness enforces the frozen four-hour/8-GiB/129,536-start limits with durable attempt charging, owned accounting, lifecycle audits and exact final inventories. Stop and preserve evidence on failure; no resume, replacement schedule or automatic extension. Completed-family reconstruction has not yet been exercised on real confirmation output.

This task added the frozen plan, preparation review and two new artifact directories, and updated six active README/discovery/replication/acceptance handoffs. Earlier dirty source/test changes and the unrelated strongholds document were preserved. No gameplay, content, Kharad, old experiment cap, migration, production configuration, deployment or promotion changed. V19 remains **VerifiedCapacityExceeded**; confirmation remains **NotRun**, reliability **Unresolved**, adoption **Hold**, ordinary/joint **NotRun / NotRun**.
