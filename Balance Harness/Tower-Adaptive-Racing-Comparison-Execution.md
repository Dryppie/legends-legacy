# Adaptive racing pilot: failed execution and monitoring repair

23 September 2026. The authorized offline pilot made **one scientific launch and stopped after 2,640 completed search fights**, before any held-out evaluation. The cause was a launcher storage-monitor race during atomic file publication. There is **no efficacy result**, no policy decision and no team recommendation from this attempt. The failed archive and all exposed fresh values have been preserved; no retry or resume occurred.

The admitted protocol remains `tower-adaptive-racing-comparison-v1`, with a 28,032-fight maximum, twelve paired roots and separate fresh held-out panels. See the [frozen plan](Tower-Adaptive-Racing-Comparison-Plan.json), [implementation](Tower-Adaptive-Racing-Comparison-Implementation.md) and [historical admission](Tower-Adaptive-Racing-Comparison-Admission.md).

## What happened

The bounded prelaunch check authenticated the sealed admission and repeated the complete live-history scan. It passed in **46.282 seconds**, with the expected **633,313 exclusions across 240 ledger files** and no new preparations, values or fights. The retained launcher then started the campaign at **09:23:29.038 UTC**.

The native writer publishes a JSON file by writing and flushing `<name>.pending`, then atomically moving it to `<name>`. The launcher's live storage check first enumerated paths and subsequently called `stat()` on each path. It enumerated `study/pair-03-candidate-plan.json.pending` just before the native writer renamed it. The later `stat()` raised `FileNotFoundError`; the owner terminated the run. The published `pair-03-candidate-plan.json` is present in the failed archive.

The [original traceback](../TestResults/adaptive-racing-comparison-execution-20260923/launcher-console.log) and [failure record](../TestResults/balance/tower-adaptive-racing-comparison-20260923/failure.json) identify this exact path and callback. This was an operational failure, not a completed comparison or a prespecified abandonment decision about search quality.

The retained journal contains exactly **2,640 Started/Completed pairs**, matching 2,640 trial records and 2,640 battle report files. Two paired searches completed, followed by the third baseline search: `2 × 1,056 + 528 = 2,640`. The third adaptive search had not dispatched combat. There is no global outputs-freeze artifact, native result, native completion receipt, scientific audit result or outer completion manifest. **Held-out fights: zero.** Partial training observations are not used to estimate either policy's advantage.

## Permanent reservations and preservation

The single 16,384-word entropy batch contained **one historical collision and zero duplicates**, reserving **16,383 fresh values**. It assigned 4,428 values to the planned schedules and left an unused reserved tail of 11,955. All fresh exposed values remain reserved, including assigned values whose searches or held-out panels never ran.

The [read-only failure closeout](../TestResults/adaptive-racing-comparison-execution-20260923/failure-closeout.json) independently reclassified the saved entropy and checked both complete reservation ledgers. Its full live-history scan preserved every old ledger and added exactly the two new pilot ledgers. Permanent history is now **649,696 values across 242 files**. No recovery amendment is necessary: this reservation reached `Complete` before combat.

The closeout hashed the entire failed archive before and after the history scan while holding the registry and output writer leases; the inventories matched. It also authenticated the unchanged admission package twice. The failed archive itself was not edited or relabeled as successful. Its external [file inventory](../TestResults/adaptive-racing-comparison-execution-20260923/failed-files.json) has SHA-256 `435e31a83910280530427b8564e8c938b65794c404aa5b13a6c8def01251f5fd`. The execution/closeout package manifest SHA-256 is `79cae962fa8e684ffbff77fd7402008c00efd049959aa7c3aec7785c692aa7d4`.

The retained Windows Job owner terminates and drains the assigned process tree in `finally` before propagating a monitoring exception. The original `FileNotFoundError` propagated without a cleanup exception replacing it; both writer leases were subsequently reacquired and the complete archive stayed unchanged. Because the callback raised, the launch did not produce a separate process-accounting receipt. This closeout does not fabricate one. An attempted OS-wide `Get-CimInstance Win32_Process` check was unavailable because access was denied; it is not used as cleanup evidence. The separate closeout's own [process receipt](../TestResults/adaptive-racing-comparison-execution-20260923/failure-closeout-process.json) confirms exit 0 and an empty job.

## Monitoring repair

`build/run-reference-exploration-comparison.py` now uses `storage_bytes()` for live sampling. If an enumerated `.pending` file disappears, it reads the corresponding published file. It deduplicates that published path if enumeration also saw it, retaining the larger size when observations differ. An existing temporary file is still counted separately from an existing published file, so replacement writes retain their temporary-byte charge.

The repair is deliberately narrow: missing ordinary files, missing publication destinations, permission failures, links, reparse points and non-file destinations still fail. Final hashing and publication remain strict after all writer processes exit. No fight schedules, algorithm rules, resource limits, entropy handling, native code or audit arithmetic changed.

The fix exists in the working-tree launcher only. The producing launcher in the failed run and the launcher in the sealed admission remain unchanged. That admission is historical: its output already exists and its history predates the permanently reserved failed batch. **The old request cannot be rerun.** A future proposal must bind the corrected launcher, the new history and a new explicit experiment/admission decision; this repair does not authorize a replacement campaign or reuse the failed batch.

## Verification and resource accounting

**42 targeted tests passed, with no failures or skips:**

- Nine new storage tests reproduce the atomic rename, replacement and duplicate-enumeration races, preserve temporary-byte accounting, and exercise the required failure cases. [Log](../TestResults/adaptive-racing-storage-regression-tests-20260923.log)
- Four adaptive protocol/launcher tests preserve the separate envelope, shared audit deadline and development-decision rules. [Log](../TestResults/adaptive-racing-post-failure-protocol-tests-20260923.log)
- Twenty-nine existing comparison tests cover baseline/offset/screened contracts, launch failures and Windows Job cleanup, including a failing storage callback. [Log](../TestResults/adaptive-racing-post-failure-compatibility-tests-20260923.log)

These are zero-combat engineering tests. Literal fixture metadata in their logs is not another scientific run. The unchanged saved-row archive suites and C# backend suites were not rerun for this Python-only monitoring repair; their preceding results remain in the implementation record. The real failed campaign cannot pass complete-study audits and those commands were not invoked. Python syntax, scoped whitespace and document links were checked.

The scientific allowance remained **10,800 seconds / 6 GiB**, with **10,680 seconds / 5.75 GiB** for the native phase. The failure file was written at **09:25:29.903 UTC**, approximately **120.865 seconds** after launch. This is a filesystem-timestamp estimate, not a successful completion timing receipt. The preserved failed archive contains **219,024,252 bytes**; its terminal size is not a measured peak. No extension or transfer of unused allowance was made.

Prelaunch verification had a separate bounded **180-second / 16-MiB** read-only allowance. The failed-run closeout had a separate bounded **300-second / 64-MiB** read-only allowance and completed its owned work in **46.562 seconds**, adding no fights or values. Earlier admission retains its separate 600-second / 512-MiB accounting. Diagnosis, repair, tests and documentation are engineering work; these figures do not claim complete historical engineering totals.

Executed commands, with `python` denoting the bundled Python interpreter:

```powershell
python -B -X utf8 'TestResults/adaptive-racing-comparison-execution-20260923/preflight.py'
python -B -X utf8 'TestResults/adaptive-racing-comparison-admission-20260923/run-reference-exploration-comparison.py' --request 'TestResults/adaptive-racing-comparison-admission-20260923/request.json' --harness 'TestResults/adaptive-racing-comparison-admission-20260923/runtime/BalanceHarness.dll'
python -B -X utf8 'TestResults/adaptive-racing-comparison-execution-20260923/failure-closeout.py'
python -B -X utf8 'build/test-reference-exploration-storage.py'
python -B -X utf8 'build/test-adaptive-racing-comparison.py'
python -B -X utf8 'build/test-reference-exploration-comparison.py'
```

The campaign command exited 1 with the documented race. Preflight, failure closeout and all test commands passed. The preflight and closeout scripts are single-use evidence artifacts, not restart tools.

Changed repository files are the Python launcher, the new storage regression test, this report, and the admission/implementation status links. Local `TestResults` artifacts retain the failed campaign, immutable inventories, complete history and process/test receipts. No gameplay values, database migrations, application configuration or deployments changed. The separate pending 52,000-fight confirmation remains untouched.

After this failure and repair were reported, the user authorized proceeding. A separately declared [second pilot](Tower-Adaptive-Racing-Pilot-02.md) used a fresh admission, updated exclusion history and its own single launch. It completed 23,680 fights and passed both scientific audits, with decision `AbandonThisConfiguration`. This first study stays a closed technical failure; its files, reservations and 2,640 fights are preserved, and its observations were not pooled into the second study.
