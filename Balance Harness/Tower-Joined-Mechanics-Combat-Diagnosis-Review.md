# Saved combat diagnosis stopped; evidence preserved

15 September 2026. This saved-data review **did not complete**. Initial and final preservation each verified **3,462 indexed files** across the seven frozen packages. The primary script read exactly **1,024 existing fight records**, then failed while summarizing nullable telemetry. It ran **zero new fights, replays, preparations, candidate generations or seed allocations**. All **482,416 reservations** remain preserved. No control-drift, combat-mechanism or search-quality conclusion is established by this incomplete review.

## Failure and practical consequence

The review script selected telemetry fields that were numeric in the first fight, then passed every fight's value to `statistics.mean`. Some later values were legitimately `null`; Python raised `TypeError: can't convert type 'NoneType' to numerator/denominator`. This is a bug in the new analysis script's handling of optional observations. The exception does not demonstrate archive corruption or a combat-engine failure.

The script had completed the stored-record inventory and basic per-record schedule/outcome checks. It failed before publishing confirmation statistics, comparing the controls' inputs, measuring group repetition or selecting an engineering target. The separate independent recount **was not run**, because its prerequisite analysis failed. Treat the inventory as partial evidence, not a completed independent audit.

The frozen protocol requires zero retries and stopping on failure. Consequently the original scripts and failed receipts are retained unchanged; no corrected archive pass was run. A [proposed repair](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915/nullable-telemetry-repair.patch) records explicit observed/missing counts, conditional summaries over available values, full telemetry-key coverage and independent recount requirements. It also specifies mixed-null, all-null, real-zero and invalid-type fixtures. **The repair is not applied or tested.** A later corrected diagnostic must explicitly supersede this stopped attempt's zero-retry boundary, preserve it, and freeze corrected scripts and limits before execution.

Null observations must not be replaced with zero. For example, no recorded clearing time cannot be treated as an instantaneous clear. Even a successful aggregate review cannot establish exact trigger timing: these archives contain prepared actors, statistics and compact telemetry, but no event-by-event logs.

## What remains known and what remains open

The already verified [comparison](Tower-Joined-Mechanics-Comparison-Execution-Review.md) remains complete: 512/512 fights, baseline finalist 0/32, joined finalist 0/32 and both controls 0/32; 71 successful joined insertions. The earlier [composition-only pilot](Tower-Composition-Only-Pilot-Execution-Review.md) recorded the controls at 7/64 and 4/64. This failed review neither revises those results nor explains their difference.

Still open: cross-study control input/preparation consistency, aggregate combat differences, same-owner group assembly and repetition, and which groups survived into the finalists. No next optimizer change or additional fight batch is justified by this partial output. The immediate correction is the saved-data reader, followed by its independently verified review if a subsequent execution is authorized. Ability ordering remains fixed. Reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold are unchanged.

## Measurements and reproduction record

| Step | Measured seconds | Result |
| --- | ---: | --- |
| Initial freeze and preservation | 1.172 | Passed; 3,462 indexed files |
| Primary saved-data analysis | 2.891 | Failed; process exit 1 |
| Independent recount | — | Not run after prerequisite failure |
| Final preservation | 0.703 | Passed; sealed packages unchanged |

Additional diagnostic workload: **4.766 / 300 seconds**. Cumulative comparison plus diagnosis: **245.796 / 1,800 seconds**. Zero retries. Output bytes and publication time are recorded in [completion.json](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915/completion.json), inside the 32-MiB additional and original 4-GiB limits. Planning, script editing and report publication are separate from diagnostic process time.

Executed once, from the repository root:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915'
& $py -B "$w/freeze.py"
& $py -B "$w/run.py" analyze # Failed; do not retry this sealed attempt.
& $py -B "$w/publish.py"    # Preserve failure and publish status only.
```

Do not rerun into this directory. [Frozen protocol](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915/protocol.md), [input/script hashes](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915/freeze.json), [failure log](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915/analyze.log), [failed receipt](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915/analyze-failure.json), [partial inventory](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915/record-inventory.json), [sealed output](../TestResults/balance/tower-joined-mechanics-combat-diagnosis-20260915/files.json).

Changed files: this review, four active Markdown handoffs and the separate diagnosis package. No harness or gameplay source was changed; concurrent source observations are retained separately. The earlier 24/40 passing backend tests through `build/run-tests.ps1` and native archive verifications remain sealed evidence, not rerun tests. No backend test invocation was required for this scripts/Markdown-only scope. The primary analysis command failed; the dependent recount was deliberately not invoked. No command was blocked by permissions. No migrations, configuration changes, deployment, boss tuning, old-cap increase or modification of a sealed experiment.
