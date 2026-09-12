# Tower coverage-policy replication handoff

Status: **planned; no replication protocol or new battles have been started**. This document carries forward the completed [v4 coverage pilot](Tower-Party-Coverage-Review.md). Its result remains **Fail: 1/3 passing restarts, with 2 required**, despite finding two independently generated teams in the intended 10–50% band. The question for the next experiment is whether unchanged v4 can recover competitive strength repeatedly on new generation and combat seeds.

## Saved builds to carry forward

The [validated-builds artifact](../TestResults/balance/tower-party-coverage-20260912/validated-builds.json) contains all 16 seed-free finalist/control recipes, measurements and content/execution provenance. The [named recipes and ancestry](../TestResults/balance/tower-party-coverage-20260912/trace-findings.json) explain how the new finalists were generated. The six measured winning recipes below should remain available as reference controls in the replication:

| Recipe ID | Prior role | Fresh pilot wins | Win rate |
| --- | --- | ---: | ---: |
| `team-38248d838d1db9634fd82536c177df0a` | New discovery-selected primary | 89/256 | 34.77% |
| `team-1a924a7cfff12298633bee909cdea4ad` | New discovery-selected secondary | 99/256 | 38.67% |
| `team-1abe76ca1891d97a91d484f0a3662048` | Predeclared saved comparison control | 72/256 | 28.13% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | Saved control | 82/256 | 32.03% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | Saved control | 77/256 | 30.08% |
| `team-693ffa8ec0b654154a06722aba06a968` | Saved control | 37/256 | 14.45% |

These observations nominate controls; they are not fresh replication results or evidence of superiority between the two new teams. The secondary does not retroactively replace the primary in the completed pilot's gate. Full results, including every zero-win finalist, remain retained.

The new v4 builds are saved in the local experiment package. They have **not** been promoted into either retained-build catalog and are not automatically available in a fresh checkout or automatically selected by the dashboard. Load and verify their exact recipes explicitly for the next definition. No rediscovery or replay is needed merely to read them; their performance on the new schedule must be measured afresh. This handoff adds no catalog entries or combat data.

## Freeze before execution

1. Verify the [completed pilot receipt](../TestResults/balance/tower-party-coverage-20260912/final-verification.json), selected source artifacts and current content/execution compatibility. Preserve the sealed pilot, its recipes, results and review. Use a separate output directory and protocol.
2. Keep `independent-coverage-v4`, with `mechanics-joint` followed by `coverage-joint`, unchanged. Preserve Kharad Health **3.04881408** / Power **3.85370128**, ten characters with five Essences each, and the existing fixed gear/untrained-Essence budget. Keep ownership assumptions explicit. Do not select the successful old restart seed or import either new recipe into independent parents, construction weights or fitness.
3. Declare the restart count, candidate/proposal limits, discovery sample count, finalist count, held-out sample count and diagnostic allocation. Generate new seeds after excluding the union of every array in the [latest seed ledger](../TestResults/balance/tower-party-coverage-20260912/seed-ledger.json), including unused reservations. Keep discovery and validation schedules disjoint.
4. Register all six controls and declare which control or controls anchor each paired reliability comparison. Freeze the simultaneous interval family, quality margin and required number of passing restarts before combat. The previous gate combined an adjusted clear-rate lower bound of at least 10%, positive paired improvement over the same-restart comparator and a paired lower difference of at least −10 percentage points against its predeclared saved control. Any revised comparison family must be explicit; never choose an easier control after seeing results.
5. Freeze the rule that selects generated primaries and secondaries from discovery ranking, then write the complete selected family before held-out battles. Preserve all above-ceiling observations and apply the unchanged [10–50% acceptance policy](Tower-Balance-Acceptance-Policy.md). The search-reliability gate and the tested-family win-rate assessment answer different questions.
6. Declare total fight, elapsed-time, storage and retry limits before execution. Preserve durable accounting, compact prepared execution, reconstruction and fixed diagnostic replays. Do not append samples to the previous pilot, automatically increase limits or promote defaults/boss settings based on a partial result.

## Budget accounting

With two methods, `R` restarts, `C` evaluated candidates per arm, `D` discovery seeds, two finalists per arm, six controls, `V` validation seeds and `Q` diagnostic repeats, the maximum allocation before any explicit retry reserve is:

`2 × R × C × D + (4 × R + 6) × V + Q`

For illustration, retaining the previous pilot's `R=3`, `C=96`, `D=8`, `V=256` and `Q=8` would require **9,224 fights**, rather than 8,712, because two additional controls add 512 validation fights. This example is **not a frozen allocation**. Deduplicate only exact recipes and retain every source association; reserve capacity before deduplication rather than dropping controls to fit an old cap. The previous 219.65-second timing excludes preparation, builds, tests, analysis and documentation and is not a runtime guarantee for replication.

## Completion and subsequent work

Report every restart, the preselected primaries' fresh paired results, uncertainty and all resource use. Save every selected recipe with ancestry and source/content/execution evidence; verify reconstruction and diagnostic parity. Keep the original 1/3 result separate and do not turn repeated experiments into a near-optimality claim.

Use replication evidence to decide whether further independent-search changes are needed. Boss retuning, default promotion and floors 6–11 remain deferred. Their progression targets stay five Essences on floors 6–9, six on floor 10 and at least seven on floor 11, with separate lower-budget diagnostics. There are no gameplay changes, migrations, configuration changes or deployment in this documentation update.
