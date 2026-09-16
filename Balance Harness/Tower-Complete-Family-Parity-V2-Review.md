# Complete retained-family parity v2 — stopped reference entry review

Recorded **14 September 2026** under the [frozen v2 protocol](Tower-Complete-Family-Parity-V2-Protocol.md). Target: offline `LL/tools/BalanceHarness`. Status **ReferenceEntryRejectedNoCombat**. The corrected input builder, **215 fixture tests**, captured candidate build and native typed input check passed. The first reference command then called the wrong executable entry point and was rejected before creating a campaign. **Zero combat preparations, engine fights, new reservations, retries or resumes occurred.** Production orchestration parity remains unverified.

## Completed verification and failure

The builder now uses the exact `TowerBalanceCellDefinition` fields: `id`, `cohortId`, `role`, `scenario`, `minimumSamples`. It checked all 253 source cell shapes before extracting the two fixed cases. Extraction completed in **15.750 seconds**, retaining both scenarios, all **288 existing replay values**, the complete exclusion list and **792 input file hashes**. All **481,603 reservations** remain unchanged. This closes the previous input-builder failure within a new package; the old failed package remains sealed.

The same **215-name fixture allowlist**, including 52 complete-family controller tests, passed in **41.409 seconds** through `build/run-tests.ps1`. Both real-combat staged cancellation/resume cases were excluded. The candidate driver and unchanged controller sources compiled against the pinned v19 gameplay DLLs in **2.179 seconds**. The native typed check completed in **7.786 seconds** (**8.750 seconds** including command preflight), validating the full reference definition, both recipe identities, disjoint replay schedules and the complete selected saved-anchor archive. It confirmed exact saved anchor scenario/schedule and content/settings equality, without preparing combat or executing the controller.

The reference was invoked using the unchanged sealed midpoint `BalanceHarness.dll` with `tower-balance-run` arguments. Its actual startup object is **`BalanceHarness.MidpointEntry`**, which delegates to `TowerMidpointRun.Command` and accepts only midpoint commands. The general `BalanceHarness.Program.Main` dispatcher exists in that assembly but is not its executable entry point. I inspected the general source route and failed to verify the selected binary's startup object before freezing the command.

The command returned **exit code 1 after 0.984 seconds** with `Use tower-midpoint-materialize|bind|check|run|verify <root>. No overrides or resume.` The driver then rejected the missing completed campaign. The `native/` directory is empty: no reference campaign, attempt journal, prepared roster or combat report was created. No candidate or independent parity command followed, and the reference command was not retried.

The [reference log](../TestResults/balance/tower-complete-family-parity-v2-20260914/reference-command.log), [command receipt](../TestResults/balance/tower-complete-family-parity-v2-20260914/reference-command.json), [failure diagnosis](../TestResults/balance/tower-complete-family-parity-v2-20260914/reference-entry-failure.json), exact frozen producing files, corrected inputs, passing TRX and typed-check receipt remain retained.

## Concrete correction prepared, unexecuted

A separate **`TowerParityReferenceHost`** was compiled after the stop as an engineering correction, without executing it. It references the byte-identical sealed midpoint harness and explicitly calls **`BalanceHarness.Program.Main`**. Its argument guard permits only the fixed non-anchor, one 32-sample cell, 32 maximum fights, zero retries and the existing time/storage settings. It checks the loaded midpoint harness hash and producing execution identity. Its `--probe` route reports the selected dispatcher and execution fingerprints without combat.

The host's source/project, build log and copied dependencies are retained under `reference-host/` and `reference-host-executable/`. **Neither its probe nor its combat route ran.** It does not replace the failed frozen reference command or change any sealed binary. A new frozen scope must first run the no-combat probe and verify all runtime fingerprints, then bind this host and the same case/schedule before any reference combat. The full-study public run contract, gameplay and caps remain unchanged.

## Commands and remaining work

Executed from the repository root:

```powershell
$package = 'TestResults/balance/tower-complete-family-parity-v2-20260914'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& "$package/run-tests.ps1"
& $python -B "$package/inputs.py"
& "$package/build-captured.ps1"
& $python -B "$package/workflow.py" engineering-freeze
& $python -B "$package/workflow.py" check
& $python -B "$package/workflow.py" freeze
& $python -B "$package/workflow.py" reference
# Engineering correction only; the produced host was not executed.
& "$package/build-reference-host.ps1"
```

Do not repeat the reference command in this package. Candidate execution and independent parity verification **could not run after the reference failure**. The candidate driver, independent verifier and new reference host remain unverified for actual combat. The intended workload is still **32 reference + 288 candidate = 320 fights**, using only already registered screen/midpoint values, with all **43,879 rosters** prepared before candidate combat. None of those preparations or fights occurred here.

The remaining engineering gate is unchanged: exact report/roster parity through the actual production batch adapters, durable two-stage execution and reconstruction. Full-family second-stage capacity, combat across the 256-cell outer batch boundary and full-study throughput remain separate limitations. No new statistical conclusion or performance speedup is claimed. Reliability **Fail 1/3**, adoption **Hold**; no confirmation launch follows.

This turn changes documentation only in the checkout: the new v2 protocol/review and seven active handoffs. Corrected builder, candidate driver, independent verifier, reference host and all producing/test evidence are in the new ignored TestResults package. No implementation/test source, gameplay/content, dependency declaration, migration, shared configuration, old cap or deployment changed. Earlier successful source verification and both earlier stopped packages remain immutable.

## Preservation and resource accounting

The final preservation audit passed in **12.297 seconds**. It compared the **4,238-file checkout baseline**, preserved all **119 prior reviews**, verified the complete inventories of the four preceding evidence packages (**1,200 / 1,404 / 1,494 / 2,069 files**), and rechecked **792 input bindings** and **201 frozen producing files**. All **145 history files**, **102 distinct history hashes** and **481,603 reservations**, including the original **512 unused v19 confirmation values**, match. The separate reference host contains **23 byte-identical captured DLLs**. Concurrent frontend/UI work was identified and left untouched. See the [preservation receipt](../TestResults/balance/tower-complete-family-parity-v2-20260914/preservation.json).

Measured setup was **27.483 seconds**, below its frozen 120-second limit. Measured diagnostic work before sealing totals **40.764 seconds**. Charging the full **60-second static allowance plus 120-second seal allowance** yields **220.764 seconds (3.679 minutes)** against the 30-minute diagnostic limit. Fixture tests and compilation are separately timed: **41.409 seconds** for tests, **2.179 seconds** for the candidate build and **0.923 seconds** for the unexecuted reference host build. Preparations, fights, retries, resumes and new reservations are all **zero**.

The final seal checks Markdown links and whitespace, captures all nine changed Markdown files, and records exact package/output accounting in `final-verification.json`, including the complete changed files, shared TRX and a 2 MiB final-metadata allowance against the **4 GiB** limit. `seal-timing.json` records actual sealing time; `evidence-files.json` is written last and hashes the complete retained evidence inventory. No package writes or diagnostic executions follow sealing.

```powershell
& $python -B "$package/closeout.py" preserve
& $python -B "$package/closeout.py" seal
```

These closure commands were run once for this package. The retained commands document what occurred; they are not instructions to rerun a sealed package.
