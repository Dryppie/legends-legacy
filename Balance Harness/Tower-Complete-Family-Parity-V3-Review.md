# Complete retained-family parity v3 — reference packaging stop

Recorded **14 September 2026** under the [frozen v3 protocol](Tower-Complete-Family-Parity-V3-Protocol.md). Target: offline `LL/tools/BalanceHarness`. Status **ReferencePackagingRejectedNoCombat**. The corrected host's dispatcher/execution probe and the candidate's typed input/saved-archive check passed. The reference then failed during executable retention because its directory lacked `BalanceHarness.deps.json`. **Zero preparations, fights, new reservations, retries or resumes occurred.** Production combat parity remains unverified.

## What passed and why execution stopped

Setup verified the entire **1,458-file v2 evidence inventory**, copied byte-identical candidate/reference-host binaries and retained **793 external input bindings** (the previous 792 plus the v2 seal). The only request change is the new reference output path; the reference definition, scenarios, recipes and all 288 existing replay values are unchanged. All **470 reviewed harness/test source hashes** match the previously tested baseline. The exact **215 passing tests** from `build/run-tests.ps1` are reused with their TRX and source hashes; **no new test run** or candidate/reference rebuild was needed.

The single host probe selected `BalanceHarness.Program`, identified the assembly's own `BalanceHarness.MidpointEntry`, and exactly matched the saved midpoint's complete execution identity, including all five assembly hashes, runtime, OS and architecture. It completed in **1.047 seconds** including command preflight. The typed check completed in **6.863 native seconds / 7.844 command seconds**, validating the complete selected saved-anchor archive, both identities, exact schedules and content/settings. Total setup/check/freezing was **14.626 seconds**, below 120. Producing freeze pinned **258 files** before the reference command.

The reference reached the intended general dispatcher, passed the frozen definition/content/settings/execution checks and began creating its new archive. It returned **exit code 2 after 1.297 seconds** while `TowerBossStudy.RetainExecutable` tried to copy `BalanceHarness.deps.json` from the host directory. The previous host build supplied the captured DLLs and its own host runtime files, but not the harness's dependency manifest or runtime configuration. I had verified DLL loading and identity without checking the complete archive-copy contract.

The partial reference contains **19 files**: 16 content files, the contract, definition and the first copied harness DLL. There is no batch directory, attempt journal, prepared roster, report or completed campaign manifest. The failure occurs inside `TowerBulkCampaign.Open`, before it returns and before `CreateInput`, reusable preparation or engine entry. The native campaign failure handler has not begun at that point, so the outer [command log](../TestResults/balance/tower-complete-family-parity-v3-20260914/reference-command.log), [timed receipt](../TestResults/balance/tower-complete-family-parity-v3-20260914/reference-command.json) and [packaging diagnosis](../TestResults/balance/tower-complete-family-parity-v3-20260914/reference-packaging-failure.json) preserve the failure. The partial archive is retained exactly; it is not a completed campaign.

## Concrete packaging correction, unexecuted

The separate `corrected-reference-layout/` now includes the **complete sealed midpoint executable directory**, the unchanged reference host and a new archive-only probe. Its **37 files** retain their source paths/hashes. A static audit follows the captured copier's complete selection rule: harness DLL/dependency manifest/runtime configuration, declared runtime assets, present locale resources, runtime subdirectories and producing assembly identities. All **25 required archive files** are present. The two files missing from the failed layout were `BalanceHarness.deps.json` and `BalanceHarness.runtimeconfig.json`. The [layout receipt](../TestResults/balance/tower-complete-family-parity-v3-20260914/reference-layout-receipt.json) records every binding.

`TowerParityArchiveProbe` invokes the unchanged captured `TowerBossStudy.RetainExecutable` method directly in a new output directory, checks its exact copied inventory/hashes and activates the existing no-combat trace guard through reflection. This tests the actual archive contract without preparing a roster or running a fight. Its first compile failed because the trace class is internal; that log and source are retained. The corrected probe accesses the internal trace through reflection and compiled successfully. **Neither the archive probe nor the corrected directory has been executed.** This engineering correction did not modify the producing freeze or retry the failed reference.

The next frozen scope must first execute this archive-only probe, verify its complete retained inventory and recheck the host execution identity from the complete directory. Only then can it run the same fixed reference/candidate diagnostic. Native archive-copy behavior of the new layout remains unverified until that probe succeeds.

## Commands, limits and remaining evidence gap

Executed from the repository root, once per diagnostic phase:

```powershell
$package = 'TestResults/balance/tower-complete-family-parity-v3-20260914'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$package/setup.py"
& $python -B "$package/workflow.py" engineering-freeze
& $python -B "$package/workflow.py" probe
& $python -B "$package/workflow.py" check
& $python -B "$package/workflow.py" freeze
& $python -B "$package/workflow.py" reference
# After the reference stop: engineering preparation only, no native execution.
& "$package/build-archive-probe.ps1"    # failed compile retained
& "$package/build-archive-probe-v2.ps1" # corrected compile passed
& $python -B "$package/prepare-reference-layout.py"
```

These commands describe the retained work; do not rerun this package. Candidate execution and independent parity verification **could not run after the reference failure**. All **43,879** full-family preparations and the intended **320** total reference/candidate fights remain pending. The unchanged driver would use the actual shared preparation, compact batch, durable selection and reconstruction adapters. No new speedup, report parity or scientific conclusion is established. The earlier discovery-accounting performance result remains unchanged; this scope did not rerun its fixtures. Full-family second-stage capacity, the 256-cell outer batch boundary and full-study throughput remain separate limitations.

Only the new protocol/review and seven active Markdown handoffs change in the checkout. The complete reference layout, probe source/builds, reused producing/test evidence and partial reference are in the new ignored TestResults package. No implementation/test source, gameplay/content, configuration, dependency declaration, migration, deployment, seed allocation or old cap changed. Reliability **Fail 1/3**, adoption **Hold**; the original sealed v19 remains Unresolved with no confirmation in that experiment.

## Preservation and resource accounting

The [final preservation audit](../TestResults/balance/tower-complete-family-parity-v3-20260914/preservation.json) passed in **12.703 seconds**. It checked the **4,254-file checkout baseline**, preserved all **120 prior reviews** and verified all **7,625 files** in five preceding sealed evidence inventories. All **793 external bindings**, **258 frozen producing files** and **470 tested source hashes** match. The registry still contains exactly **145 history files / 102 distinct history hashes / 481,603 reserved values**, including the original **512 unused v19 values**. Concurrent frontend/UI changes were identified and left untouched.

The partial reference's **19 files / 18,323,024 bytes** are unchanged. Every corrected-layout file matches its recorded source; the archive-only probe and corrected reference layout remain unexecuted. Candidate and independent verification never started. Preparations, engine fights, new reservations, retries and resumes are all **zero**.

Measured setup was **14.626 seconds**, reference rejection **1.297 seconds**, corrected-layout preparation **0.968 seconds**, and preservation **12.703 seconds**. Recorded diagnostic work before sealing totals **29.594 seconds**. Adding the full **60-second static allowance and 120-second seal allowance** charges **209.594 seconds (3.493 minutes)** against the 30-minute limit. The two probe compilation attempts are timed separately at **1.638 seconds failed / 1.155 seconds passed**, with their output counted. No new backend tests ran; the retained 215-test wrapper result is reused, not counted as a fresh verification run.

Closure checks the new Markdown links/whitespace, captures all nine changed Markdown files and writes `final-verification.json` with exact output accounting. The charge includes the complete package, whole changed Markdown files outside it and a **2 MiB metadata allowance**, bounded by **4 GiB**. `seal-timing.json` records actual sealing time. `evidence-files.json`, written last, hashes the complete retained inventory; no package writes or execution follow sealing.

```powershell
& $python -B "$package/closeout.py" freeze
& $python -B "$package/closeout.py" preserve
& $python -B "$package/closeout.py" seal
```
