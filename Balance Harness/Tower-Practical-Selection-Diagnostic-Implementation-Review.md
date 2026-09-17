# Practical Tower selection diagnostic: implementation and verification

**Historical snapshot:** this document records its original scope. Current disposition is in the [execution review](Tower-Practical-Selection-Diagnostic-Execution-Review.md); the earlier launch restrictions below have been superseded for that one completed run.

17 September 2026. Target: the offline `LL/tools/BalanceHarness` application and its backend fixtures. **The separate four-nominee diagnostic is implemented and passes 404 distinct focused checks within the synthetic verification scope. Gameplay launch remains NoGo pending compatible resource evidence and a fresh frozen allowance.** This implements the [execution/audit/sampling contract](Tower-Practical-Selection-Diagnostic-Contract.md); it does not establish that a missed stronger nominee exists.

The new public commands are `tower-selection-diagnostic-check`, `tower-selection-diagnostic-run` and `tower-selection-diagnostic-verify`. The [practical guide](../LL/tools/BalanceHarness/PRACTICAL-SEARCH.md#four-nominee-selection-diagnostic) documents request fields, outputs and failure handling. The historical design JSON and analysis readers remain unchanged and are not runnable requests.

## Behavior and design decisions

The diagnostic retains `retained-composition-incumbents-v1`, its two supplied anchors, fixed canonical ability order, exact ten-character cohort and declared content/progression/equipment assumptions. Discovery still evaluates 64 parties on eight conditions; selection still measures the same four nominees on 32 conditions and uses the unchanged primary selector. The distinct phase validator allows only an unscheduled template or a bound search with no confirmation panel. The ordinary public definition validator still requires its original complete schedules.

The first reservation contains exactly 41 construction/discovery/selection values and uses the existing deterministic allocation formula. After exactly 640 completed attempts, the workflow freezes the four ordered recipes, anchor identities, selection rows, primary, request/binding hashes and attempt-prefix hash. It then obtains one 8,192-byte cryptographic batch. Signed little-endian classification rejects historical values and duplicate words, uses the first 1,000 eligible values for confirmation and retains every eligible tail value as a permanent exclusion. A short batch stops without refill. The maximum new reservation is 2,089 values, including the search reservation.

Both reservation transitions publish blocking Pending before values may be exposed. Entropy bytes and their completion marker are flushed before cooperative cancellation is observed. Process death can still interrupt these writes; uncertain exposure stays Pending. This increment provides no diagnostic Pending recovery or resume. Existing practical recovery formats reject the distinct outer request. Completed reservations survive any later failure.

All four nominees receive the same 1,000-condition panel in frozen nominee/panel order. A durable Started record precedes every attempted fight; Completed follows valid evidence. The 4,640-attempt ceiling includes failed starts. No cache reuse, refill, replay, sample extension or replacement of the primary is allowed.

The native verifier reconstructs the shared diagnostic state machine using saved reports and validates native prepared-input hashes, content and producing executable identity. A separate audit counts the four saved outcome vectors and three paired contrasts directly, without calling the state-machine assessor. Both forbid combat and use the retained entropy bytes rather than an entropy callback. The separate count audit shares the existing Wilson arithmetic; it is not independent evidence of the operating system's randomness or of external timing. Exact inventories, semantic bindings and reconstructed outputs must agree.

The result separates execution, integrity, sampling assumptions, diagnostic decision and `balanceAssessment: NotAssessed`. Family ten covers four win rates and two discordance rates for each of three comparisons to the frozen primary. A selection miss requires another nominee's supported win rate to reach 10%, gains minus losses to reach 50, and its adjusted paired lower bound to exceed zero. A valid negative result does not establish equivalence. All four teams are exported without seeds; both anchors remain recommended and adoption stays Hold.

A parent owns the registry/output leases and watches a hidden worker. The worker independently watches parent identity and global/phase deadlines. Admission, search, confirmation and audit have explicit nontransferable time/byte ceilings within the cumulative allowance. Audit includes final publication. Closeout retains the existing two-second /4-MiB reserve. Byte enforcement uses sampled and boundary checks, not an operating-system quota. Phase receipts record elapsed time and output growth observed at phase close; they are not peak-memory or disk high-water measurements. Publication and verification reject an over-budget completion.

## Verification evidence

All backend checks ran through `build/run-tests.ps1`, with normal project compilation and no source-removal override:

| Invocation | Passed | Failed / skipped | Scope |
| --- | ---: | ---: | --- |
| Diagnostic plus previous practical regression filter | 348 | 0 /0 | Existing 316 cases and 32 diagnostic cases |
| Final diagnostic and additional shared-kernel filter | 88 | 0 /0 | All 32 diagnostic cases after final audit refinements, plus 56 composition/scheduling/order cases |
| Final durable-freeze diagnostic rerun | 32 | 0 /0 | Complete diagnostic suite after routing saved freezes through flushed atomic storage |
| Distinct covered cases | **404** | **0 /0** | Overlapping diagnostic cases counted once |

The final builds had **zero errors and nine existing warnings**. The second run checks the independent no-combat guard, explicit balance status, phase timing consistency and resource observation. The third reruns all diagnostic cases after the final durable-freeze change; the shared search/validation/storage implementation was unchanged after the first 348-case regression. Logs and TRX records are retained under [selection-diagnostic-checks](../TestResults/selection-diagnostic-checks/regression.log), with the [diagnostic/kernel run](../TestResults/selection-diagnostic-checks/final-diagnostic-and-kernel.log), [final durability run](../TestResults/selection-diagnostic-checks/final-durability.log) and [final TRX](../TestResults/selection-diagnostic-checks/final-durability.trx).

The new cases cover exact search/nomination/primary parity, incumbent-primary and negative outcomes, contract rejection, signed words, collisions, duplicate/tail exclusion, capped shortfall, eight entropy interruption boundaries, the observed-margin/paired-support/viability thresholds, and ten separately resealed semantic tamper cases. Real subprocess fixtures cover completion, blocked phase deadlines, storage exhaustion, parent death and worker death. The production verifier rejects fabricated native content; fixture reconstruction is not presented as successful native gameplay verification.

The first diagnostic run exposed a Windows file-sharing error when hashing the flushed but still-open attempt journal. The freeze reader now explicitly shares the writer's access. Interruption tests were also tightened to require their intended injected boundary rather than accepting an unrelated earlier exception. Final source review routed study freezes through the existing write-through, flushed atomic storage path so the nominee freeze precedes entropy acquisition durably. The full diagnostic suite passed after these fixes. Initial sandbox restore could not read the existing user NuGet configuration; the authorized runner succeeded outside the restricted sandbox. An optional process-inventory query was unavailable in the sandbox; process ownership checks themselves passed through the fixtures. No required verification remains blocked.

The final diagnostic/shared-kernel invocation was:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessSelectionDiagnosticTests|FullyQualifiedName~BalanceHarnessSuppliedCompositionTests|FullyQualifiedName~BalanceHarnessSuppliedSchedulingTests|FullyQualifiedName~BalanceHarnessSuppliedSchedulingParityTests|FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessCompositionOrderTests' -ArtifactsPath 'TestResults/selection-diagnostic-build'
```

The first invocation used the existing [316-case practical filter](../LL/tools/BalanceHarness/PRACTICAL-SEARCH.md#verification-scope), prepended with `FullyQualifiedName~BalanceHarnessSelectionDiagnosticTests|`, and the same artifact path. The final durability invocation used only `FullyQualifiedName~BalanceHarnessSelectionDiagnosticTests`, again through the same runner and artifact path. The final renamed tampering test still contains one test case with ten independently restored mutations; it is not counted as ten cases.

**Subsequent execution:** The [completed diagnostic](Tower-Practical-Selection-Diagnostic-Execution-Review.md) completed all **4,640 fights** and both built-in audits in **248.562 seconds /152.36 MiB**, returning **NoSelectionMissDemonstrated**. The frozen primary won **712/1,000**, versus **632/1,000** and **628/1,000** for the anchors and **598/1,000** for the other challenger. This is encouraging fixed-team evidence, while adoption remains **Hold** and both anchors remain recommended. The scope is closed; **2,089** new reservations bring the permanent union to **486,374**, with no new Pending reservation. No further experiment is queued. The analysis, fixture/probe measurements and JSON below retain their original historical scopes.

## Resource observation and remaining admission work

One complete literal archive exercised 4,640 synthetic battle reports, both audits and publication. Its completion record captured **21.524 seconds** before final manifest sealing; retained output after publication was **10,058,719 bytes**. The post-publication deadline/storage checks passed. The [resource observation](../TestResults/selection-diagnostic-checks/literal-resource-observation.json), extracted from the [final TRX](../TestResults/selection-diagnostic-checks/final-durability.trx), records these values and the phase receipts:

| Phase | Measured seconds | Output growth observed at phase close |
| --- | ---: | ---: |
| Admission | 0.204 | 364,173 bytes |
| Search | 2.390 | 2,530,580 bytes |
| Confirmation | 15.115 | 6,424,220 bytes |
| Audit/publication | 3.792 | 214,323 bytes |

This is one fixture observation, not a benchmark distribution or upper bound. It uses a one-value synthetic history, fabricated content/mechanics, small synthetic reports and no native content/executable copy. It performs zero gameplay fights and zero production entropy draws. Phase growth excludes later receipt/manifest writes, which explain part of the difference from final retained bytes. The fixture's 240-second /128-MiB allowance is a test constraint, not a grant for a real run.

The observation verifies that the implemented record count and two-auditor path complete in the fixture environment. It does **not** qualify a real 4,640-fight run. The retained native predecessor measured a different 1,408-fight workload; the [contract's scaling illustration](Tower-Practical-Selection-Diagnostic-Contract.md#resource-admission-evidence-and-remaining-gap) is not a performance forecast. The next step is to qualify resource cost using compatible native-copy, history, report-size and combat-time evidence, then freeze a separate cumulative allowance and all four phase ceilings. Any gameplay calibration would need its own explicit bounded purpose and accounting. No calibration or experiment is queued by this implementation.

## Changed files and preservation

| Files | Purpose |
| --- | --- |
| [TowerSelectionDiagnostic.cs](../LL/tools/BalanceHarness/TowerSelectionDiagnostic.cs) | Versioned records, fixed contract, shared execution, assessment and exports |
| [TowerSelectionDiagnosticReservation.cs](../LL/tools/BalanceHarness/TowerSelectionDiagnosticReservation.cs) | Two reservation phases, entropy classification, durable journals and reconstruction |
| [TowerSelectionDiagnosticArchive.cs](../LL/tools/BalanceHarness/TowerSelectionDiagnosticArchive.cs) | Native archive, saved-state reconstruction and separate direct-outcome audit |
| [TowerSelectionDiagnosticRun.cs](../LL/tools/BalanceHarness/TowerSelectionDiagnosticRun.cs) | Owned lifecycle, phase/global limits, publication and public commands |
| [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs), [TowerSuppliedCompositionSearch.cs](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs) | Internal phase-aware validation and shared unchanged kernel; old public contracts stay strict |
| [TowerCompleteReservation.cs](../LL/tools/BalanceHarness/TowerCompleteReservation.cs), [Program.cs](../LL/tools/BalanceHarness/Program.cs) | Reuse flushed atomic storage for bytes and dispatch the distinct commands |
| [DiagnosticFixtureHost.cs](../LL/tests/BalanceHarness.ProcessFixture/DiagnosticFixtureHost.cs), [FixtureHost.cs](../LL/tests/BalanceHarness.ProcessFixture/FixtureHost.cs), [BalanceHarnessSelectionDiagnosticTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessSelectionDiagnosticTests.cs) | Literal archive, process and semantic-integrity verification |
| This review, practical guide/README, assessment, readiness, design/contract, hypothesis and evaluation Markdown | Current status, usage and remaining resource decision |

Historical scripts, JSON receipts and sealed native archives are preserved. The orchestration manifest still authenticates **53 files**, and the native study manifest still authenticates **1,564 files**. The [preservation check](../TestResults/selection-diagnostic-checks/preservation.json) found only the 13 intended existing-file changes among 213 baseline files; the other 200 remained byte-identical. Git whitespace checks and local Markdown-link checks passed. No production evaluation values were allocated: the **484,285 exclusions**, V19's **512 unused values /253 required recipes /Unresolved**, both recommended anchors and adoption **Hold** remain unchanged.

There are **no migrations, persistent application configuration changes, gameplay/content changes or deployments**. New request fields are confined to the offline diagnostic. Runtime assembly hashes change when the harness is rebuilt; historical archives must continue using their retained producing runtime. No native gameplay command, retained-study reconstruction, replay, historical recovery or wider quality experiment ran in this engineering increment.
