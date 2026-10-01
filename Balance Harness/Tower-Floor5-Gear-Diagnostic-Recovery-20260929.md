# Floor 5 diagnostic output recovery — 29 September 2026

The [initial diagnostic](Tower-Floor5-Gear-Diagnostic-20260929.md) stopped on its first detailed replay when JSON output exceeded 8 MiB. Its immutable `TestResults/tower-floor5-gear-diagnostic-20260929` archive retains the declaration, truncated log, full 128-pair summary and failed completion. One combat was executed to produce that log; no complete replay was verified and no new seed was allocated. This is an output-cap failure, not a battle-result mismatch. Do not overwrite or resume that archive.

## Separate frozen recovery scope

Keep the same source pin, original runtime, composition, two profiles and first sixteen declared seeds as the initial diagnostic. No outcome-dependent resampling. Repeat those 32 historical fights in a new archive `TestResults/tower-floor5-gear-diagnostic-recovery-20260929`. Count the first failed attempt separately: maximum **33 replay executions across both scopes**, zero fresh seeds and zero acceptance samples.

Raise only the explicitly bounded output allowance to **64 MiB per replay / 2 GiB overall**, retaining 60 seconds per replay and 660 seconds overall. The extra allowance accommodates verbose native event objects; full JSON is necessary for independent reconciliation. A second resource failure closes this recovery without another automatic expansion. Capture owned-process cleanup observations even on exceptions. All original input, event and summary equality requirements remain unchanged. Driver options: `--log-mib 64 --total-mib 2048`.

Live game data and all 103 team/gear combinations remain unchanged. After the diagnostic, any health/offense trial needs a separate declaration before executing new seeds.

## Completed result

**All 32 detailed replays verified**, with zero fresh seeds. The recovery archive is 251,196,080 bytes (including its manifest), and every process exited successfully with zero active descendants. The source family and live Tower file stayed unchanged. Five Python diagnostic safeguards pass, including rejection of changed outcomes, missing logs and non-reconciling event totals.

Mean recipient totals for the first sixteen paired seeds:

| Metric | Resistance-and-health | Health-and-regeneration |
| --- | ---: | ---: |
| First 40 seconds: Seal magical damage | 4,647.50 | 7,364.31 |
| First 40 seconds: all health damage | 7,394.25 | 10,005.81 |
| First 40 seconds: actual regeneration | 2,490.19 | 4,451.31 |
| First 40 seconds: other healing | 1,252.50 | 1,240.06 |
| Whole battle: all health damage | 37,607.88 | 50,137.56 |
| Whole battle: actual regeneration | 8,966.75 | 15,646.06 |
| Whole battle: original-character deaths | 6.06 | 8.69 |

No original character died in the first 40 seconds. Early extra Seal damage exceeds the extra regenerated health. Whole-battle totals include different fight lengths and must not be presented as isolated mitigation effects. Across all 128 saved pairs, mean first death was **70.41 seconds with resistance versus 72.34 with regeneration**; later first death does not imply better overall survival or success.

The native Seal pulse scales with Power; offense is therefore a relevant test variable. Health also affects Seal's barrier and fight length. A [separate joint calibration](Tower-Floor5-Joint-Calibration-20260929.md) declares three health increases at 5% lower offense, retaining every known recipe and both gear acceptance requirements.

Recovery source: `TestResults/tower-floor5-gear-diagnostic-recovery-20260929/files.json`, SHA `03b1515357e61ec2940353bf908fbd8177bd8fef4e53ee49622b607a87ba9b21`. Summary-only source uses the preserved +4.5% candidate; no candidate was applied by this diagnostic. No backend source change, deployment, configuration change or migration.
