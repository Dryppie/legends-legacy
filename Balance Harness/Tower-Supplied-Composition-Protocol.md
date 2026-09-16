# Supplied composition implementation: bounded zero-combat verification

16 September 2026. This protocol covers the explicitly authorized implementation and synthetic verification only. It does not authorize a combat comparison, preparation, fresh balance seed allocation, gameplay/content change, deployment or combat retry.

## Frozen work

Compile the current harness against the previously captured gameplay dependencies. Compile and execute exactly `BalanceHarnessSuppliedCompositionTests` and `BalanceHarnessZeroWinSelectionTests`, with their two explicitly included synthetic fixture helpers. Freeze counts each Fact and each explicit InlineData case and records the exact count and source hashes before compilation; dynamically supplied theory data are rejected. No legacy test class is selected implicitly. Synthetic measurements and fabricated journal callbacks are not combats or seed reservations.

The tests cover supplied-start admission and canonical ordering, legal candidate construction, retained complete parties and coordinated improvement, diversity, budgets, cancellation, deterministic reconstruction, provenance and independent-mode rejection. The separately versioned zero-win selector receives fabricated stage outcomes; historical selection behavior remains covered by the same new fixture class. Combat-entry guards protect pure kernel tests. The comparison runtime fixture creates synthetic scalar records only.

The initial workload is freeze, harness compilation, test compilation, one test invocation, independent receipt audit and seal. Any failure or limit violation stops dependent work. Retain every failure. This prospective engineering scope permits at most two attempts at each compilation phase and at most two targeted test invocations, all under the same resource limits. A second attempt requires an explicit compiler, fixture or environment defect explanation; a negative synthetic strength result must be reported and must not be tuned away. Repair compilation uses a separate output directory and preserves existing binaries, source pins and logs. Conservative storage admission can prevent a second attempt. A repeated test invocation on unchanged binaries retains a separate TRX. No historical sealed command is rerun, no successful binary is silently replaced, and combat retries remain zero. If repair cannot fit or a second attempt fails, seal incomplete.

## Resource envelope

The preserved starting receipt is `TestResults/balance/tower-fresh-first-trajectory-20260916/completion.json`: 4,049.7802723556424 / 4,380 diagnostic seconds and 4,718,720,963 / 4,731,174,912 logical output bytes. Its manifest SHA-256 is `f35c07b5b792c8b86ba8e86f802f0a1b7ba8864eb73f20532f2be4bcf11c7ad0`.

This package consumes at most **300 additional diagnostic seconds** and **12,000,000 additional bytes** inside those unchanged limits. A conservative **30 seconds** is charged for pre-execution inspection and later publication; measured freeze/build/test/audit/seal time is charged in addition. Phase limits are 25/60/45/100/20/15 seconds respectively, further restricted by the remaining total. Child processes use the existing Windows Job Object mechanism, with a package-local copy adding a storage guard. Timeouts and storage guard failures terminate only the owned child process tree.

Storage accounting includes all package files, copied test/runtime outputs, retained test temporary files, full bytes of changed/new live source files, this protocol, the later review, `LL/tools/BalanceHarness/README.md`, `Balance Harness/Tower-Zero-Win-Selection-Plan.md`, and at least **1 MiB** for shared test-runner output. A further **256 KiB** publication allowance remains charged throughout. Existing immutable sources, dependency DLLs and cached restore metadata are referenced by hash rather than duplicated. Live file-size checks run during child execution with a closure reserve; every phase also verifies its output size. Output is preserved on failure, never deleted to manufacture headroom.

Historical asset integrity is checked only for referenced files and the small manifests/receipts that bind them. The full historical output tree is not rehashed. Compilation copies only changed/new harness sources and the two test files/helpers; unchanged harness sources are taken from the sealed fresh-first source capture after matching them to the live checkout. This isolates unrelated dirty gameplay source from the build. No NuGet restore or network access is used.

## Invocation and evidence

After implementation, test sources and workflow have been statically reviewed, invoke each phase once in order:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$workflow = 'TestResults/balance/tower-supplied-composition-20260916/workflow.py'
& $python -B $workflow freeze
& $python -B $workflow build
& $python -B $workflow test-build
& $python -B $workflow tests
& $python -B $workflow audit
# Publish the review and update only the scoped README/selection-plan status.
& $python -B $workflow seal
```

The test phase calls `build/run-tests.ps1 -NoBuild -ArtifactsPath <package>/tests` with exactly the two-class filter. The workflow preserves previous shared TRX content in the new package before the shared runner writes its new result. It records exact command/environment arguments, source/dependency pins, test counts, process exits, hashes, storage and diagnostic charges, and a final `completion.json` plus `files.json`. The retained 353-file pre-implementation dirty snapshot is verified at freeze, audit and seal, excluding only the intended source/document changes. Scoped README, plan and final review content can change before sealing; their full bytes remain counted rather than pinned. A failure stops without automatic repair; use `seal` to close incomplete, or `repair-build`, `repair-test-build`, or `retry-tests` with a nonempty quoted defect explanation to request the bounded second attempt. The final receipt lists failures even if a subsequent permitted engineering repair passes. The workflow does not publish or alter other historical handoff documents.

### Metadata repair before compilation

The initial freeze stopped after **0.11000000000058208 seconds**, before compilation, because the exported 353-file snapshot included the new design review's hash from before its final small edit. The original pre-implementation dirty snapshot contains **352 files** and is retained as `original-dirty-hashes.json`. The second attempt verifies that exact baseline, excluding only the declared task changes, and separately pins the completed design review as context at SHA-256 `ad75367584a10ba7cc1b08cb368b50d018340bfb40e95e10a76cad9bd8c5ef28`. This corrects snapshot bookkeeping; it does not exempt unexpected changes or alter production sources.

The original workflow, protocol, pins and failure remain preserved. Initialize the sole second engineering attempt with `repair-freeze "Correct the exported baseline: verify the original 352 dirty files and separately pin the finalized design review"`. Then invoke `second-build`, `second-test-build`, `second-tests`, and `second-audit` individually; publish scoped documents and finish with `second-seal`. A second metadata freeze failure stops verification. All first-attempt bytes and elapsed time remain charged under the same 300-second / 12,000,000-byte scope; no cap is extended.

No resource extension is granted. All **483,046 existing reservations** remain preserved. Synthetic test labels are fixed fixture constants, not new allocations. Adoption remains **Hold**; passing fixtures would demonstrate search mechanics, not stronger combat teams.
