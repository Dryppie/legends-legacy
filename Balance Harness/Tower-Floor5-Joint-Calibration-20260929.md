# Floor 5 health and offense calibration — 29 September 2026

**Subsequent result:** the [smaller-offset refinement](Tower-Floor5-Joint-Refinement-20260929.md) tested +5%/+6% health at −5% offense. Both gear profiles and three compositions qualified, but strongest wins 30/64 and 28/64 exceeded the 26/64 ceiling. It closed without application after 13,184 fights / 128 reservations; latest exclusions 910,967. The report below retains its original closed scope.

Target: primary LL World Tower guardian data and offline Balance Harness. The [paired diagnostic](Tower-Floor5-Gear-Diagnostic-Recovery-20260929.md) supports investigating magical damage pressure. It does not establish an accepted setting or new win-rate evidence.

**Completed: `NoGridCandidate`.** All three complete-family screens finished, but none met both equipment gates. No stability panel, confirmation, application or post-application regression ran. Floor-5 health/offense remains **3.3102803755 / 4.4702934848**. The calibration ran **19,776 fights / 192 fresh reservations**, ending at **910,839 exclusions**. No active study remains.

## Diagnostic basis

All 32 detailed historical replays matched their complete saved reports after removing added logs. Per-character event damage, healing, regeneration and first deaths reconcile. The first sixteen declared seeds were used without outcome filtering; zero new seeds. Preserve the initial truncated-log attempt separately (33 total replay executions across both scopes).

In the common first 40 seconds, mean Seal of Ascension magical health damage was **4,647.50** with resistance-and-health versus **7,364.31** with health-and-regeneration. Actual regeneration was **2,490.19 versus 4,451.31**, and other healing **1,252.50 versus 1,240.06**. Extra regeneration therefore did not fully offset extra pulse damage. No original character died in this early window. Across all 128 saved pairs, mean first death was actually later with regeneration gear (**72.34 versus 70.41 seconds**); the gap is not explained simply by the first death happening earlier.

The pulse scales with guardian Power at coefficient 0.5 (`abilities.json`), and guardian offense scales Power (`WorldTowerGuardianScaling.cs`). Seal's barrier also scales with maximum health, so changing health can alter both encounter length and pulse exposure. The next experiment is exploratory joint calibration, not an isolated causal estimate or an ability rewrite.

Diagnostic recovery manifest: `03b1515357e61ec2940353bf908fbd8177bd8fef4e53ee49622b607a87ba9b21`, source `TestResults/tower-floor5-gear-diagnostic-recovery-20260929`. Detailed subset outcomes are not population rates or acceptance samples.

## Frozen trial

- Current floor-5 health/offense: **3.3102803755 / 4.4702934848**; Tower SHA `aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`.
- Exactly three candidates relative to current game content: **health +7%, +9%, +11%, each with offense −5%**. Other scalars, abilities, equipment, budgets and floors stay unchanged. This tests three points along one offense-reduced slice; it does not exhaust the joint space.
- Preserve all **103 exact recipes / fourteen actual compositions / seven gear profiles** from unchanged-content `TestResults/tower-balance-pass-floor5-alternate-gear-expanded-screen-study-20260929`, manifest `46d30356581049e9e95528739ce3291dbc4caa13ff85b8f185e41d4da77a4f80`. Count compositions by per-slot Essence sets without rewriting raw scenarios.
- Player budget stays ten level-40 characters, five unascended/unevolved level-1 Essences, tier-1 Epic/Fine/rank-3 gear, baseline rolls, no styles. Full ownership is hypothetical. No dungeon/acquisition or search work.
- Authenticate source, diagnostics, original runtime and previously passed regression receipts. Reuse 91 backend passes / four intentional opt-in skips and fourteen Python passes at entry; explicitly label them reused. Run fresh diagnostic and selection safeguards. Original source LF-to-CRLF exception remains exactly as previously recorded.
- Prepare the full family from current content without combat. Each candidate gets **64 fresh seeds / 6,592 fights**. Require every recipe at most **26/64**, at least two actual compositions at least **13/64**, and a qualifying recipe on **both resistance-and-health and health-and-regeneration**.
- Rank eligible candidates by higher minimum of the two required profiles' best wins, more qualifying compositions, higher second-composition wins, strongest rate nearest 30%, then smaller total scalar change and label. Freeze **only the first candidate** for a **128-seed / 13,184-fight stability panel**. Require every recipe at most **44/128**, at least two compositions at least **26/128**, and both required profiles qualifying. If it fails, close; no fallback candidate.
- If stability passes, freeze one independent **184-seed / 18,952-fight confirmation**. Across all 103 exact recipes require approximate simultaneous 95% Bonferroni-Wilson upper bounds at most 50%, at least two real compositions with lower bounds at least 10%, and qualifying recipes on both required gear profiles. Native one-composition Pass alone is insufficient. Do not pool any selection or historical results.
- Only after full acceptance, apply both confirmed floor-5 scalars locally, check all **18,952 inputs and 103 complete replays**, then rerun the relevant backend regression through `build/run-tests.ps1`. No deployment or migration.
- Maximum **51,912 study fights + 103 conditional application replays = 52,015 executions**, **504 fresh reservations**, starting from **910,647 exclusions**. Each phase at most 20,000 fights / 840 native seconds / 900 owner seconds / 2 GiB; require 80% projected time/output margins. Application verification keeps its 600/660-second bounds.
- New paths use `floor5-joint-calibration`; old archives remain immutable. Stop this scope on technical failure, failed selection/stability/confirmation or resource rejection. No grid extension, repeated search, sample extension, relaxed gear gate or dropped recipe.

Driver, application and collector are frozen under `TestResults/tower-floor5-joint-calibration-*-20260929.py` before execution. No current-game adjustment follows solely from this declaration.

## Completed calibration

Every screen retained all 103 recipes, fourteen actual compositions and seven profiles. Samples were fresh and disjoint between settings; observed differences include sampling uncertainty.

| Health change | Offense change | Best resistance-and-health | Best health-and-regeneration | Qualifying compositions | Result |
| --- | --- | ---: | ---: | ---: | --- |
| +7% | −5% | 22/64 | 12/64 | 2 | Alternate gear below 13/64 minimum |
| +9% | −5% | 17/64 | 9/64 | 1 | Alternate gear and composition count fail |
| +11% | −5% | 6/64 | 5/64 | 0 | Neither gear reaches minimum |

The +7% setting was closest: every recipe stayed below the 26/64 ceiling and two real compositions qualified, but the alternate gear missed by one win. That margin is not permission to relax the gate or reuse this panel for acceptance. No candidate advanced, and nothing was applied. This three-point slice does not prove that joint calibration is infeasible.

**Recommended next bounded work:** test a smaller health offset at the same reduced offense, such as +5% and +6% health with −5% offense, in a separately frozen scope. This is a hypothesis based on the lowest tested setting having ceiling headroom while alternate gear was just below the minimum. Preserve all 103 recipes and both gear gates, reserve new disjoint samples, and independently confirm any selection. Do not rerun the closed +7/+9/+11 panels, pool observations, restart search-algorithm experimentation or divert to dungeons. No next combat or seeds have been allocated.

## Verification and retained evidence

- Four native fixtures (zero-fight preparation and three full screens) passed through `build/run-tests.ps1`; each process exited zero without timeout or active descendants. Native elapsed total: **469.92 seconds**.
- **19 fresh selection safeguards** and **five diagnostic Python tests** passed. The prior **91 backend passes / four opt-in skips** and fourteen Python passes were authenticated and reused, not rerun. Conditional stability, confirmation, application parity and post-application regression were intentionally not run because no screen qualified.
- Collector checked **725 unchanged input/runtime pins**, exact family retention, attempts/completions, scalar-only candidate content and seed accounting. New diagnostic/control files are separate from historical source archives.
- Evidence: `TestResults/tower-floor5-joint-calibration-evidence-20260929.json`, SHA `605b9c0c0a85d93c8dcf2f5b4f0c0065177bb6a947d5cf277a988221bccd7520`.
- Latest ledger: `TestResults/tower-balance-pass-floor5-joint-calibration-grid-health-plus-110-offense-minus-050-owner-20260929/seed-ledger.json`, SHA `0bbf9a1a6e592d36932f1adb6afbac6a099f3716f9cf82c76bb63f090d047860`. Union **910,839**, including every earlier unused reservation.
- Diagnostic publication audit: `TestResults/tower-floor5-gear-diagnostic-publication-check-20260929.json`; 135 recovery archive members and 32 complete replays authenticated. Include the preceding truncated-output attempt separately: **33 diagnostic executions + 19,776 calibration fights = 19,809 executions this continuation**, **192 new reservations**. No diagnostic replay counts as a fresh balance sample.
- Whole-Tower SHA stays `aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`. No production code, guardian data, configuration, migration, database or deployment changed. Supported search and player progression budgets remain unchanged.

The old 89-cell confirmation remains historical accepted evidence only. The current 103-cell family remains unaccepted. Broad non-poison build diversity, equipment coverage on other floors, pacing targets and ordinary acquisition remain open.

Verification commands used: `python -B -X utf8 "Balance Harness/analysis/test-tower-gear-diagnostic.py"`; the saved diagnostic driver commands in each declaration; `python -B -X utf8 TestResults/tower-floor5-joint-calibration-checks-20260929.py`; `python -B -X utf8 TestResults/tower-floor5-joint-calibration-driver-20260929.py`; `python -B -X utf8 TestResults/tower-floor5-joint-calibration-collect-20260929.py`; `git diff --check`. The native study owner calls `build/run-tests.ps1 -NoBuild` with the preserved artifacts and the Tower balance-pass filter. Backend regression is reused because no production input changed; no claim of a fresh 91-test run follows.
