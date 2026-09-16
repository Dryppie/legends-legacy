# Complete retained-family controller — implementation and stopped verification review

Recorded **14 September 2026** under the [frozen protocol](Tower-Complete-Family-Controller-Protocol.md). Target: offline `LL/tools/BalanceHarness`. Status **ImplementationTestedDiagnosticScopeViolatedCapturedVerificationPending**. The new controller compiles and **217 tests pass**, including **52 new controller tests**, but this is **not a zero-combat verification result**.

## Scope violation and evidence

The test filter mistakenly included the entire pre-existing `BalanceHarnessTowerStagedTests` class. Its `Cancel_resume_reconstructs_selection_and_committed_work_without_accepting_tampering` theory has two cases. Each case invokes the real combat engine, cancels after two returned fights, recovers publication and resumes. Each passed case asserts **four logical trials and four durable charged attempts**. Together they establish **eight test fights and two test resumes**, using the existing literal test combat values **82001 and 82002**. This violated the task's **zero fights / zero resumes** limits. The mistake was discovered after the suite had completed.

Further diagnostic execution stopped. The planned captured controller build, complete source-only inspection and independent native interval check **were not run**. The prior completed UTC proof and seed-free design binding remain valid within their original scopes. No confirmation study was launched, no balance seed allocation occurred, and no old experiment was rerun or amended.

The [scope-deviation receipt](../TestResults/balance/tower-complete-family-controller-20260914/scope-deviation.json) records both passed theory cases, their timestamps/durations, the source-supported fight count and the stopped boundary. [The complete TRX](../TestResults/balance/tower-complete-family-controller-20260914/tests-2.trx), wrapper logs, producing test artifacts and exact test/source snapshots are retained. The existing test cleanup had already deleted its temporary combat archives; **per-fight reports are unavailable**. No attempt was made to recreate them. The count is supported by passed test assertions and captured source, rather than a new retained fight ledger. All 481,603 registered reservations, including the original unused 512, remain preserved.

## Implemented behavior

- [TowerCompleteFamily.cs](../LL/tools/BalanceHarness/TowerCompleteFamily.cs) defines the new fixed contract: 43,879 cells, 580 anchor reasons / 560 anchors, 32 and 256 independent stage samples, 4,096 second-stage capacity and 2,434,784 maximum attempts. Whole-stage alpha .025, observed ceiling rejection, universal ceiling/per-context viability, no pooling and complete unresolved selection remain explicit. Old validators and caps are unchanged.
- [TowerCompleteFamilyInputs.cs](../LL/tools/BalanceHarness/TowerCompleteFamilyInputs.cs) pins the completed UTC source seal/cell file, captured gameplay, recipes, original timestamp identities, content/settings and anchor family. It validates externally reserved schedules against the complete refreshed history, including newly appeared registry files, and checks prior setup charges. The code exposes no allocator. Its complete real-source path remains unexecuted in this package.
- [TowerCompleteFamilyRun.cs](../LL/tools/BalanceHarness/TowerCompleteFamilyRun.cs) spans both stages with one durable start/completion journal, deadline and storage owner. It uses existing compact storage campaigns of at most 256 cells and 32-record chunks, keeps their statistical decisions separate from whole-stage assessment, verifies exact trials and prepared roster hashes, freezes selection durably, and reconstructs the full result before final publication. A future run prepares and hashes the entire family before any engine call. Returned completions are charged before cancellation is observed; interrupted evidence cannot resume or verify as complete.
- [Program.cs](../LL/tools/BalanceHarness/Program.cs) exposes source inspection, execution and verification commands without allocation, cap overrides or resume flags. Its earlier unrelated changes remain intact. The new [test class](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerCompleteFamilyTests.cs) exercises the same controller loop with synthetic accounting events, without constructing a combat runtime.

The 52 new tests cover the full 43,879-cell arithmetic, exact 3,536/3,537 unresolved capacity boundary, missing/duplicate/aliased bindings, global multiplicity, no pooling, invalid trial identities/outcomes, disjoint histories/schedules, resource limits, cancellation, durable interrupted starts, returned completion charging, capacity stops, forbidden engine entry outside writers and tampering with a closed archive. Existing related storage, context, runtime comparator, retained-audit, ceiling, midpoint and staged regression tests also passed. **The two unintended real-combat cases belong to the existing staged class, not the new controller fixtures.**

The controller's first-stage ceiling still clears only 0–1 wins out of 32. Actual second-stage selection capacity remains unproven; more than 3,536 unresolved non-anchors must stop Inconclusive alongside the 560 required anchors. A future complete-family run would have its own fixed 24-hour / 64-GiB envelope. These constants do not expand this stopped diagnostic's limits or any earlier study's caps.

## Commands and measured results

The first wrapper invocation stopped during compilation after **18.112 seconds** because the new test used an ambiguous `Program` type. The reference was qualified as `BalanceHarness.Program`; additional boundary tests were included before the second build. No tests ran during the failed compilation.

The second invocation completed in **69.907 seconds**, including a **12.66-second build** and **54-second test suite**, with **217 passed / zero failed / zero skipped**, 34 existing warnings and zero errors. The two real-combat theory cases took **19.366 and 14.200 seconds**. Neither duration is a controlled throughput benchmark. The new controller's combat throughput and captured-v19 orchestration parity are unmeasured.

The executed command was:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTowerCompleteFamilyTests|FullyQualifiedName~BalanceHarnessTowerContextTests|FullyQualifiedName~BalanceHarnessTowerRuntimeComparisonTests|FullyQualifiedName~BalanceHarnessTowerRetainedAuditTests|FullyQualifiedName~BalanceHarnessTowerCeilingControllerTests|FullyQualifiedName~BalanceHarnessTowerStorageTests|FullyQualifiedName~BalanceHarnessTowerStagedTests|FullyQualifiedName~BalanceHarnessTowerMidpointTests' -ArtifactsPath TestResults/balance/tower-complete-family-controller-20260914/test-build-2
```

**Do not repeat that filter under a zero-combat scope.** A future corrected filter must group the same inclusion expression and append:

```text
&FullyQualifiedName!~Cancel_resume_reconstructs_selection_and_committed_work_without_accepting_tampering
```

That corrected filter was **not executed here**. A new frozen scope must inspect the selected tests for engine entry before invocation, use new isolated output, build the producing controller against captured gameplay, inspect the sealed source exactly once and compare all 290 native interval rows against the prior independent table. Changed-orchestration parity and any fresh reservation/confirmation launch remain separate bounded work. The stopped package must remain immutable.

## Remaining limitations and preservation

The new implementation has current-checkout correctness coverage, but the planned captured-source verification gate is incomplete. The source inspection command, production full-family preparation, real compact-batch adapter and complete-family reconstruction have not been executed end-to-end against the captured study. Test success does not establish those properties or authorize launch. Frozen future binding still needs concrete external reservation/allocator evidence, complete current history, setup accounting and disk capacity; referenced source/history/setup artifacts must remain available.

This task adds three harness source files, one test class, a protocol and this review; it updates the existing CLI and seven active Markdown handoffs. It changes no gameplay/content, old caps, migrations, shared configuration, package dependencies, catalog promotion or deployment. Reliability **Fail 1/3**, adoption **Hold**. Preservation and resource charges are recorded below after engineering closure; the violated zero-combat/no-resume conditions remain failed regardless of other passing checks.


## Final preservation and accounting

The **11.973-second** preservation pass checked **4,207 baseline checkout files**, all **116 prior reviews**, every one of the **1,494 files** in the sealed UTC package, and its **203 prior input bindings**. All remain intact outside this task's declared source/document edits. Eight unrelated frontend/UI-verification files changed and one appeared during the task; their paths are recorded and their changes were preserved.

The registered seed history remains **145 files / 102 distinct hashes**, with an independently reconstructed union of exactly **481,603 reservations** matching the authoritative ledger. No reservations were added or removed. This preserves the ledger; it does not erase the unintended test fights. Their preparation-call count was not retained, and no zero-preparation claim is made.

For conservative resource accounting, the **entire 69.907-second second build/test command** is charged as diagnostic workload because it included real combat. Together with baseline capture and preservation, measured charged phases total **82.382 seconds**. Adding **60 seconds** for short static reads/metadata and the full **120-second seal allowance** yields **262.382 seconds / 4.37 minutes**, below the 30-minute time ceiling. The failed initial compilation is separately timed at 18.112 seconds. The allowances are conservative charges, not measured durations.

Both isolated build trees, test output, source snapshots, final documents, changed checkout files, shared TRX and a 2-MiB metadata allowance are included in the output charge, which remains **below 1 GiB**, within the 4-GiB storage ceiling. Passing the time/storage ceilings does not cure the failed zero-combat/no-resume requirements.

`final-verification.json` records the exact charges and incomplete gates. `updated-files.json` and `final-documents/` bind the final implementation and handoffs. Scoped whitespace and new Markdown link checks complete before `evidence-files.json` seals the package; `seal-timing.json` records actual sealing time. No further diagnostic, source inspection, test rerun, combat or seed allocation followed discovery of the scope violation.
