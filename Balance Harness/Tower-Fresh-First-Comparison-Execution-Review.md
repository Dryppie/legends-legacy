# Refinement comparison execution review

16 September 2026. **VerifiedRefinementComparison**. 280 completed / 280 charged fights; 45 fresh values durably recorded. Primary paired difference: 0.00 percentage points, adjusted interval [-22.22, 22.22]. This pilot does not establish improvement over the baseline.

| Confirmation family origins | Wins / fights | Mean boss health remaining |
| --- | ---: | ---: |
| baseline-finalist | 0/32 | 62.08% |
| discovery-refinement-finalist | 0/32 | 62.80% |
| team-040e60d3dbc5c127321653c47ed3a9d3 | 0/32 | 30.03% |
| team-49f6979895354870c89362d4abf214bb | 3/32 | 27.19% |

Boss health is descriptive; it does not replace the frozen win-rate endpoint. Native reconstruction and independent confirmation/statistics checks completed. Pending reservation state: **False**; unresolved allocation start/partial transcript: **False**. Preserve all original 483,001 reservations and every newly derived/recorded value; uncertain Pending state forbids reuse. No retries or replays.

The [frozen execution protocol](Tower-Fresh-First-Comparison-Protocol.md) binds 45 fresh values, at most 288 attempts, 360 study seconds, 400 execution-task seconds and 84 MiB study output plus 24 MiB producing runtime and 4 MiB execution evidence, with readiness charged separately. The explicit approval raises cumulative diagnostic time to 4,380 seconds and cumulative output to 4 GiB + 416 MiB. Exact usage and the first failure are in the [receipt](../TestResults/balance/tower-fresh-first-comparison-execution-20260916/completion.json). The [readiness review](Tower-Fresh-First-Comparison-Readiness-Review.md) preserves input/binary checks, 85 reused passing backend tests and 128 saved-record/four-rate reader checks.

Measured native execution: 65.5471427 seconds. Study bytes at publication: 85,113,082. Complete task time and final cumulative bytes are recorded in the receipt.

Final publication summary: **280/280 charged fights**, comprising 64 baseline discovery, 64 V5 discovery, 24 selection and 128 confirmation. The shared selection recipe retained origins `baseline-rank-1` and `discovery-refinement-rank-2`, saving eight fights under the frozen merge rule. The finalists themselves were distinct. V5 left **0.7240625 percentage points more boss health** in confirmation; that descriptive difference does not change the primary zero-point win-rate result or adjusted interval. The second control won 3/32, but its adjusted contrasts also include zero.

The complete sealed wrapper used **69.000 seconds**; native execution used **65.5471 seconds**. Final study output is **85,113,082 bytes** (81.170 MiB), and execution output including runtime/seal is **19,104,999 bytes** (18.220 MiB). Runtime accounts for 18,393,078 bytes and execution evidence for 711,921 bytes. All approved caps were respected.

A once-only final hash/membership check verified 64 readiness files, 50 execution files and 553 study files. It charges ten seconds conservatively in full, making execution plus publication 79.000 seconds. Cumulative usage is **4037.795272 / 4,380 seconds** and **4,717,896,657 / 4,731,174,912 bytes**, including the [publication receipt](../TestResults/balance/tower-fresh-first-comparison-publication-20260916.json). The sealed execution snapshot remains unchanged; these final figures are appended only to the live review.

All **483,046 reservations** remain excluded: the 483,001 pre-existing values plus all 45 approved new values. The new reservation is Complete; older Pending exclusions remain preserved. No further fresh values or fights are authorized. The 85 existing passing backend tests were reused without a build/test rerun. Native reconstruction, independent archive/statistics checks and scoped whitespace verification passed; no required execution command was skipped or blocked. Checkout preservation verified 340 snapshotted dirty paths without changes outside the allowed Markdown set. Changed files are the new study/execution/publication evidence, this review and the six active Markdown handoffs; no gameplay/content, configuration, migration or deployment changes.

The outcome supports retaining the baseline as reference and V5 as exploratory. It does not establish a stronger search policy. Inspecting the saved four edits and nominations is the available next analysis without further combat; do not change selection retrospectively or infer strength from cross-pilot results with different seeds.

First failure, if any:

```
None.
```

The new study and execution evidence are sealed without retry. No gameplay, configuration, migration or deployment changes. Fixed ability order, v19 Unresolved, its 512 unused values and 253 recipes, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain. This is a single bounded exploratory policy comparison, not a reliability or optimality claim.

Executed once after the bound approval:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B 'TestResults/balance/tower-fresh-first-comparison-readiness-20260916/execution.py'
```
