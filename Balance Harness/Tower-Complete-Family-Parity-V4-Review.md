# Complete retained-family parity v4 — production parity verified

Completed **14 September 2026** under the [frozen v4 protocol](Tower-Complete-Family-Parity-V4-Protocol.md) and the narrowly scoped [exit-handling amendment](Tower-Complete-Family-Parity-V4-Exit-Handling-Amendment.md). Target: offline `LL/tools/BalanceHarness`. **All 320 scheduled fights completed, all 288 candidate report comparisons matched, all 43,879 saved participant rosters matched, and native plus independent reconstruction passed.** No new seeds, retries or resumes occurred. Status **ProductionParityVerified**. Reliability **Fail 1/3**, adoption **Hold**; this diagnostic adds no fresh balance evidence.

## Verified behavior

The actual captured executable-retention probe copied all **25 required files** from the complete reference directory and verified their exact hashes. The reference-host probe selected the intended general dispatcher and matched the full saved midpoint execution identity: harness plus four gameplay DLLs, runtime, OS and architecture. The candidate's native typed check verified both identities, exact schedules, content/settings and the complete selected midpoint archive. Setup reused the sealed producing binaries and unchanged scenarios; only the new reference output path changed. **794 external input bindings** and **319 producing files** were frozen.

The selected non-anchor completed its original **32-value reference schedule** with **0 wins / 32 defeats** and exactly **32 durable charges**. Before candidate combat, the production preparation adapter prepared **every one of the 43,879 retained rosters** and matched its saved participant hash. Every input/plan/participant hash is retained in `native/candidate/preparation.json`.

The candidate then executed the actual shared production compact-batch adapters, global journal, storage ownership, selection and reconstruction paths:

| Stage | New candidate fights | Reference | Result |
| --- | ---: | --- | --- |
| First, fixed non-anchor | 32 | The new 32-fight reference | All full report/gameplay digests match |
| Second, fixed midpoint anchor | 256 | The existing sealed midpoint reports | All full report/gameplay digests match |

Comparison includes full report digest, prepared roster, summary, outcome, remaining guardian health and display duration. Only physical archive index/case labels may differ. Native archive verification precedes comparison. The controller durably published the unchanged stage selection, completed exactly **288 global starts/completions**, and reconstructed before final publication and once more afterward without combat. All child charges agree; the global journal is exactly 288 start/completion pairs. Total new combat is **32 reference + 288 candidate = 320**, with **43,882 preparation calls**: 43,879 full-family, one reusable reference and two reusable candidate preparations.

The unchanged independent verifier passed in **39.859 seconds / 41.016 command seconds**. It checked the exact new archive inventories/hashes, all schedules and 288 saved-reference payload/digest pairs, every preparation row against the sealed participants, both stage decisions, intervals, global/child charges and trace counts. See the [native receipt](../TestResults/balance/tower-complete-family-parity-v4-20260914/native/candidate-receipt.json), [independent verification](../TestResults/balance/tower-complete-family-parity-v4-20260914/independent-verification.json) and [288 independent comparisons](../TestResults/balance/tower-complete-family-parity-v4-20260914/independent-comparisons.json).

## Preserved wrapper failure and correction

The reference's scientific assessment was **Inconclusive**, mapped by the captured evaluator to exit code **3**. Its 32 fights and complete archive had succeeded; the wrapper then rejected that exit code because it accepted only 0/1. The [original failed command receipt](../TestResults/balance/tower-complete-family-parity-v4-20260914/reference-command.json), [log](../TestResults/balance/tower-complete-family-parity-v4-20260914/reference-command.log), original protocol and producing hashes remain unchanged.

Before any candidate attempt, the separate amendment froze acceptance of this completed reference without replay. Its one read-only check verified the exact complete archive, 32 ordered records, all 32 durable journal starts, no-retry accounting, original definition/content/settings/execution and the original feasibility gate (**at most 9 wins**, satisfied by zero). It wrote a separate [acceptance receipt](../TestResults/balance/tower-complete-family-parity-v4-20260914/reference-acceptance.json). The candidate then ran its exact previously frozen command once. No reference fight was repeated, no case or seed changed, and the scientific result remains Inconclusive. This corrected command classification, not a balance decision or resource cap.

## Measurements and limits

| Measured operation | Seconds | Interpretation |
| --- | ---: | --- |
| Native archive-copy probe, command | 1.500 | 25 files; zero preparations/fights |
| Host identity probe, command | 1.000 | Exact saved execution identity |
| Typed input/archive check, command | 8.141 | Zero preparations/fights |
| Reference command | 3.125 | All 32 fights completed; scientific exit 3 |
| Candidate command | **266.047** | Complete source, 43,879 preparations, 288 fights and reconstruction |
| Candidate preflight, inclusive | 249.715 | Includes full-family preparation and source/reference verification |
| Full-family preparation, inclusive | **223.108** | Subphase of preflight; do not add these two rows |
| Actual first-stage adapter, inclusive | 1.658 | 32 fights plus batch preparation/archive work |
| Actual second-stage adapter, inclusive | 8.921 | 256 fights plus batch preparation/archive work |
| Native final reconstruction, inclusive | 1.052 | Before final publication |
| Independent verification, command | 41.016 | No preparation or combat |

The [persisted native trace](../TestResults/balance/tower-complete-family-parity-v4-20260914/native/candidate/performance.json) reports **264.563 process CPU seconds**, **571,214,807,592 cumulatively allocated bytes** and **2,203,643,904 peak working-set bytes**. Allocations are cumulative, not simultaneously retained memory. Its performance snapshot is **263.465 seconds**, recorded before final publication; native command completion is **264.835 seconds**, and the outer command's **266.047 seconds** additionally includes wrapper input-hash checks and final file accounting. The final [performance summary](../TestResults/balance/tower-complete-family-parity-v4-20260914/performance-summary.json) retains both inclusive phase groups and separately summable exclusive timings.

Preparation dominates this deliberately small combat diagnostic. This is not a new before/after throughput comparison or a full-study runtime estimate. The earlier discovery-accounting measurements remain **622.54× for incremental accounting** and **4.50× for the sixteen-write lifecycle at 9,216 archives**, as recorded in the [performance review](Tower-Discovery-Performance-Review.md); no fixture was rerun here. The broader 5× lifecycle target and whole-run speedup remain unestablished.

Across the captured trace, the largest **exclusive** costs are canonical JSON hashing (**136.326 seconds / 703,329 calls**) and input materialization (**61.691 seconds / 87,809 calls**). Engine simulation used **5.552 seconds / 288 calls**. The **34 owned-storage checks** used **0.015607 inclusive seconds**. Four recorded final storage audits used **0.079224 inclusive seconds**, with the trace's stated before-final-publication boundary. These observations identify preparation/hash/materialization work as a candidate for a future separately bounded optimization; they do not authorize weakening identity checks or another diagnostic run here.

## Reproducible commands and verification scope

Executed from the repository root, once per phase:

```powershell
$package = 'TestResults/balance/tower-complete-family-parity-v4-20260914'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$package/prepare-scripts.py"
& $python -B "$package/setup.py"
& $python -B "$package/workflow.py" engineering-freeze
& $python -B "$package/workflow.py" archive
& $python -B "$package/workflow.py" probe
& $python -B "$package/workflow.py" check
& $python -B "$package/workflow.py" freeze
& $python -B "$package/workflow.py" reference # Complete archive; wrapper rejected scientific exit 3.
& $python -B "$package/amendment.py" freeze
& $python -B "$package/amendment.py" accept  # Read-only; no repeated reference fights.
& $python -B "$package/amendment.py" candidate
& $python -B "$package/workflow.py" independent
```

These commands record the executed scope; do not rerun a sealed output directory. All intended native and independent checks are now complete. The prior **215 passing fixture tests through `build/run-tests.ps1`**, including 52 complete-family tests, are reused with the exact TRX/allowlist and **470 unchanged reviewed source hashes**. No new test invocation or compilation occurred in this scope. The unchanged-source check, actual production execution and independent result provide the additional verification here; this is not a claim of a newly run 215-test suite.

This closes full-roster preparation and the small actual two-stage production-adapter parity gate. It does not verify full-family second-stage capacity, combat across the 256-cell outer batch boundary or complete-study throughput. The full 2,434,784-attempt contract still needs a separate external allocator/setup binding and separately scoped execution; none was created or launched. Saved midpoint selection and reliability/adoption conclusions remain unchanged, and original v19 retains its sealed Unresolved/no-confirmation result and all 253 recipes/nominations.

Checkout changes are the new v4 protocol/amendment/review and seven active Markdown handoffs. Producing source/binaries, scripts and all evidence are retained in the separate ignored TestResults package. No implementation/test source, gameplay/content, dependency declaration, configuration, migration, deployment, authoritative seed ledger or old cap changed. Earlier stopped packages and unrelated dirty work remain preserved.

## Preservation, changed files and final charges

The [final preservation audit](../TestResults/balance/tower-complete-family-parity-v4-20260914/preservation.json) passed in **18.094 seconds**. It checked the **4,264-file checkout baseline**, preserved **121 prior reviews**, and verified all **8,033 files** across six preceding sealed packages. The 794 external input bindings, 319 producing files, 470 tested source hashes and complete native output inventory match. All **145 history files / 102 distinct history hashes / 481,603 reservations**, including the original **512 unused v19 confirmation values**, are unchanged. Concurrent frontend/UI edits were identified and left untouched.

The three new documents are this review, the frozen v4 protocol and the exit-handling amendment. Updated active handoffs are the [implementation record](Automatic-Tower-Team-Discovery-Implementation.md), [discovery plan](Automatic-Tower-Team-Discovery-Plan.md), [acceptance policy](Tower-Balance-Acceptance-Policy.md), [replication handoff](Tower-Coverage-Replication-Plan.md), [search strategy](Tower-Search-Strategy-Reset.md), [retained-family design](Tower-Retained-Family-Confirmation-Design.md) and [harness README](../LL/tools/BalanceHarness/README.md). Their final snapshots and exact hashes are retained in the sealed package.

Setup/check/freezing totals **16.203 seconds**, below 120. The amendment's freeze and read-only acceptance used **1.204 seconds**, below its 30-second allowance. Including reference, candidate, independent verification and preservation, recorded diagnostic work before sealing is **345.689 seconds**. Adding the full **60-second static and 120-second seal allowances** charges **525.689 seconds (8.761 minutes)** against the unchanged 30-minute global cap. No compilation or new fixture test run was added. Actual new combat is **320**, with zero new reservations/retries/resumes.

Final sealing checks Markdown links/whitespace, captures all ten changed Markdown files and writes `final-verification.json` with exact byte charges. Output accounting includes the complete package, whole changed Markdown files outside it and a **2 MiB metadata allowance**, against the unchanged **4 GiB** cap. `seal-timing.json` records actual sealing time. `evidence-files.json` is written last and hashes the complete retained inventory. No further package writes, tests, probes, preparation or combat follow sealing.

```powershell
& $python -B "$package/closeout.py" freeze
& $python -B "$package/closeout.py" preserve
& $python -B "$package/closeout.py" seal
```
