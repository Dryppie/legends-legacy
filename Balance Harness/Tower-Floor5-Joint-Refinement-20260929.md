# Floor 5: smaller health offset at reduced offense — 29 September 2026

**Subsequent result:** the [single +6.5% health / -5% offense midpoint](Tower-Floor5-Joint-Midpoint-20260929.md) failed its larger 128-seed selection panel. No setting was applied. That report replaces this document's midpoint recommendation with a read-only reassessment and proposed damage-mix work; this trial's frozen protocol and results remain historical evidence.

Target: primary LL World Tower guardian data and the offline Balance Harness. Continue the closed [joint calibration](Tower-Floor5-Joint-Calibration-20260929.md). Its +7% health / −5% offense screen retained two qualifying compositions and ceiling headroom but missed the health-and-regeneration minimum by one win. Test smaller offsets in a separate scope, without reusing earlier outcomes as new samples.

**Completed: `NoGridCandidate`.** Both screens met the gear and composition minimums but exceeded the strongest-recipe ceiling. No stability, confirmation, application or post-application regression ran. Current floor-5 health/offense stays **3.3102803755 / 4.4702934848**. This scope completed **13,184 fights / 128 fresh reservations**, ending at **910,967 exclusions**. No active study remains.

## Frozen protocol

1. Authenticate the preceding `NoGridCandidate` evidence (`605b9c0c0a85d93c8dcf2f5b4f0c0065177bb6a947d5cf277a988221bccd7520`), current content and preserved runtime. Keep original live floor-5 health/offense **3.3102803755 / 4.4702934848**, whole-Tower SHA `aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`, and **910,839** prior exclusions. Reuse authenticated 91 backend passes / four intentional opt-in skips and fourteen Python passes at entry; they are not new test executions. Run nineteen fresh selection safeguards. Do not rebuild historical assemblies or repin original archives; retain the previously documented source line-ending exception exactly.
2. Retain all **103 exact recipes / fourteen real compositions / seven gear profiles** from unchanged-content `TestResults/tower-balance-pass-floor5-alternate-gear-expanded-screen-study-20260929`, manifest `46d30356581049e9e95528739ce3291dbc4caa13ff85b8f185e41d4da77a4f80`. Preserve raw recipes, IDs, positions and Essence order. Count compositions by per-slot Essence sets. Budget stays ten level-40 characters, five level-1 unascended/unevolved Essences, tier-1 Epic/Fine/rank-3 gear, baseline rolls and no styles. Complete ownership is hypothetical.
3. Prepare the full family without fights. Test exactly **+5% and +6% health, both at −5% offense**, relative to the current live values. Each candidate uses **64 fresh seeds / 6,592 fights**, disjoint from all previous reservations. Change no other content. Require every recipe at most **26/64**, at least two actual compositions with a recipe at least **13/64**, and such a recipe on **both resistance-and-health and health-and-regeneration**.
4. Rank eligible candidates by the higher minimum of the two required gear profiles' best wins, more qualifying compositions, higher second-composition wins, strongest rate nearer 30%, then smaller total scalar change and label. Freeze only the first for a **128-seed / 13,184-fight stability panel**. Require every recipe at most **44/128**, at least two compositions at least **26/128**, and both required profiles qualifying. No eligible grid candidate or failed stability closes this scope; no fallback.
5. If stable, run one independent **184-seed / 18,952-fight complete-family confirmation**. Use approximate simultaneous 95% Bonferroni-Wilson bounds across all 103 exact recipes: every upper bound at most 50%, at least two actual compositions with lower bounds at least 10%, and qualifying recipes on both required gear profiles. Native one-composition Pass alone is insufficient. Do not pool historical, selection or stability samples into confirmation.
6. Only after full acceptance, apply both confirmed floor-5 scalars locally. Verify all **18,952 inputs and 103 complete replays** against applied content and rerun the relevant backend suite through `build/run-tests.ps1`. Preserve every other guardian and budget. No deployment, migration, database operation or configuration change.

Maximum **45,320 study fights + 103 conditional replays = 45,423 executions**, **440 fresh reservations**. Each phase stays below 20,000 fights / 840 native seconds / 900 owner seconds / 2 GiB; require 80% projected time/output margins before larger panels. Application verification retains its 600/660-second bounds. Fresh paths use `floor5-joint-refinement`. Preserve failed/rejected scopes and unused seeds. Stop on technical failure, resource rejection or failed gates; no retries, sample extensions, additional candidates or relaxed requirements.

The saved paired diagnostic already identified Seal of Ascension magical damage as the main gear difference. Do not repeat diagnostic combat, restart search-algorithm exploration, introduce supplies or redirect to dungeons. This two-point test does not exhaust health/offense possibilities and cannot establish ordinary acquisition or broad non-poison archetype viability.

Driver, selection checks, application and collector are frozen under `TestResults/tower-floor5-joint-refinement-*-20260929.py` before execution. All prior confirmations retain their original scopes; the old 89-cell result does not accept the current 103-cell family.

## Completed results

| Health increase | Offense reduction | Strongest recipe / best resistance gear | Best regeneration gear | Qualifying compositions | Result |
| --- | --- | ---: | ---: | ---: | --- |
| +5% | −5% | 30/64 | 25/64 | 3 | Strongest exceeds 26/64 ceiling |
| +6% | −5% | 28/64 | 21/64 | 3 | Strongest exceeds 26/64 ceiling |

Both required gear profiles cleared the 13/64 minimum, and three actual compositions had a qualifying recipe. Neither setting cleared the universal ceiling, so no candidate advanced. These are selection counts, not confirmed population win rates or accepted viable-build claims. The previous +7% / −5% screen (22/64 resistance, 12/64 regeneration) remains separate historical selection evidence; different seeds prevent interpreting adjacent percentages as a measured response curve.

**Next recommendation:** one separately declared midpoint check at **+6.5% health / −5% offense**, using a larger 128-seed complete-family selection panel and independent confirmation only if eligible. This is a hypothesis between the nearest rejected settings, not an assumed interpolation or accepted adjustment. Preserve every exact recipe and both gear requirements. If that bounded midpoint also fails, reassess the joint tuning approach before further fractional increments. Do not repeat the closed +5/+6/+7 panels or pool their results. No midpoint combat or seeds have been allocated.

## Verification and retained evidence

All three native fixtures (zero-fight preparation and two screens) passed through `build/run-tests.ps1 -NoBuild`, with exit zero, no timeout and no active descendants. Native elapsed total **319.79 seconds**. **Nineteen fresh selection safeguards** passed. The earlier **91 backend passes / four intentional opt-in skips** and fourteen Python passes were authenticated and reused, not rerun; no production input changed. Larger panels, application parity and post-application regression were intentionally skipped because selection failed. No command remains blocked.

The independent collector checked **695 unchanged input/runtime pins**, unchanged 103-recipe/fourteen-composition/seven-profile coverage, candidate-only health/offense edits, all 13,184 attempts/completions and 128 fresh reservations. The main game file stayed byte-identical. The archived diagnostic and all prior successes, rejections and failures remain immutable.

- Evidence: `TestResults/tower-floor5-joint-refinement-evidence-20260929.json`, SHA **`3adf37f41be75ff6725b442bf68122b7a103833a2f033954b9716c76ad97a190`**.
- Latest ledger: `TestResults/tower-balance-pass-floor5-joint-refinement-grid-health-plus-060-offense-minus-050-owner-20260929/seed-ledger.json`, SHA **`461bc3a50fd08fc58ccbb1faecaddedaa4dca0515e51c702138fb47cbd56857f`**.
- Whole-Tower SHA remains **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**. Latest exclusion union **910,967**. No fresh diagnostic replay, search, confirmed recipe or gameplay application in this scope.

Commands: `python -B -X utf8 TestResults/tower-floor5-joint-refinement-checks-20260929.py`, `python -B -X utf8 TestResults/tower-floor5-joint-refinement-driver-20260929.py`, `python -B -X utf8 TestResults/tower-floor5-joint-refinement-collect-20260929.py`, and `git diff --check`. The driver invokes native studies through the required backend wrapper using preserved artifacts; individual commands and receipts are archived by phase.

Maintained changes are this report, current handoff/status, both harness guides and the superseding notice in the previous joint report. No production code or guardian data, dependencies, configuration, migrations, database operations or deployments changed. The expanded floor-5 family still lacks acceptance; the old 89-cell confirmation remains historical evidence only.
