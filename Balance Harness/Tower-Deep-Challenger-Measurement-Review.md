# Deep challenger measured results

15 September 2026. The once-only frozen deep study completed and passed both native reconstruction and an independent saved-record audit. **It did not recover competitive new teams: 0/3 primaries met the recovery criterion.** The selected 11-team family has a balance Pass because the two saved controls remain viable. That Pass is not evidence of successful search recovery.

## Confirmation result

The fixed strongest control won **63/256 (24.61%)** and the second control **47/256 (18.36%)** on the new shared confirmation schedule. The strongest of nine confirmed new challengers won **7/256 (2.73%)**. All nine new challengers have adjusted upper win-rate bounds below 10%, and all nine paired difference upper bounds are below zero against the fixed strongest control. No supported improvement was found.

| Team | Wins | Observed win rate | Adjusted rate interval | Adjusted paired difference vs fixed control |
| --- | ---: | ---: | --- | --- |
| Saved control `team-040e60d3dbc5c127321653c47ed3a9d3` | 63/256 | 24.61% | 17.38% to 33.62% | 0 (self) |
| Saved control `team-49f6979895354870c89362d4abf214bb` | 47/256 | 18.36% | 12.13% to 26.81% | -20.52 to 8.51 pp |
| New challenger `team-63357a2e7176e667b6bcf0a7cf380a1e` | 7/256 | 2.73% | 0.92% to 7.87% | -32.27 to -9.77 pp |
| New challenger `team-1fe1759d4ee623311ab2865307be8fb1` | 5/256 | 1.95% | 0.55% to 6.73% | -33.40 to -10.14 pp |
| New challenger `team-cde1ef6d4295182b246d0e156b843b9d` | 5/256 | 1.95% | 0.55% to 6.73% | -33.11 to -10.43 pp |
| New challenger `team-ee32c83868b4bed31a9e028542a5ccc6` | 5/256 | 1.95% | 0.55% to 6.73% | -33.11 to -10.43 pp |
| New challenger `team-f0aabdc9dadabc86a9dd56358abc874f` | 5/256 | 1.95% | 0.55% to 6.73% | -33.11 to -10.43 pp |
| New challenger `team-1459b844b391996e97c5451257e47884` | 0/256 | 0.00% | 0.00% to 3.51% | -34.18 to -13.12 pp |
| New challenger `team-2ba071ee90bdac3a0c667bbbe3fd244e` | 0/256 | 0.00% | 0.00% to 3.51% | -34.18 to -13.12 pp |
| New challenger `team-da371729ec3f945b2f26f9e22aa041ad` | 0/256 | 0.00% | 0.00% to 3.51% | -34.18 to -13.12 pp |
| New challenger `team-de0021a83839395e9d64a3a570ab392a` | 0/256 | 0.00% | 0.00% to 3.51% | -34.18 to -13.12 pp |

Rate intervals allocate alpha .025 over all 11 teams. Paired intervals allocate alpha .025 over all 10 comparisons, using two discordance Wilson bounds each. They are approximate intervals for this frozen selection and independent schedule; they are not a lifetime repeated-study guarantee. The native and independent calculations agree within 1.28e-10.

| Root | Primary frozen after screening | Screening | Confirmation | Recovered |
| --- | --- | ---: | ---: | --- |
| 1 / `-673116851` | `team-63357a2e7176e667b6bcf0a7cf380a1e` | 2/64 | 7/256 | No |
| 2 / `838652225` | `team-1459b844b391996e97c5451257e47884` | 0/64 | 0/256 | No |
| 3 / `1345381170` | `team-1fe1759d4ee623311ab2865307be8fb1` | 4/64 | 5/256 | No |

Recovery required at least two primaries with rate lower bound >=10% and paired lower difference >=-10 percentage points. Both requirements were frozen before confirmation. Historical portfolio reliability remains **Fail 1/3**, adoption **Hold**. Original sealed v19 remains **Unresolved, all 253 required recipes retained, no internal confirmation**.

## Work performed and measured time

Three independent roots evaluated **4,608 candidate recipes** in 4,805 proposals. Each candidate received eight discovery fights. Each root's full top 32 received 64 screening fights. Exact recipe deduplication produced a complete required confirmation family of **11 teams**, including both controls, original and screened nominees, and every required ceiling observation. Each received 256 confirmation fights. All generated recipes and origins remain archived.

| Phase | Completed fights | Minutes | Fights/second |
| --- | ---: | ---: | ---: |
| discovery | 36,864 | 17.165 | 35.79 |
| screen-0 | 2,048 | 0.939 | 36.33 |
| screen-1 | 2,048 | 0.737 | 46.28 |
| screen-2 | 2,048 | 0.888 | 38.45 |
| confirmation | 2,816 | 1.245 | 37.70 |
| Complete run command, including binding checks and final publication | **45,824** | **21.871** | **34.92** |

The workload used **45,824/108,544 fights**, zero retries, zero resumes and zero combat replays. The native performance window ends before final inventory publication: **21.093 minutes**, CPU **783.891 seconds**, peak process working set **821.91 MiB**, cumulative managed allocation **521.74 GiB**. Allocation is cumulative allocation traffic, not retained memory or disk output. The native summary, performance window, phase clocks and outer command intentionally have different boundaries; their durations are not interchangeable.

| Instrumented operation | Calls | Exclusive seconds | Share of performance window |
| --- | ---: | ---: | ---: |
| `engine.simulation-without-checkpoints` | 45,824 | 395.489 | 31.25% |
| `outer.attempt-flush` | 91,648 | 225.840 | 17.85% |
| `compact.flush-attempt` | 45,824 | 203.482 | 16.08% |
| `hash.file-read` | 576,162 | 92.451 | 7.31% |
| `content.load` | 9,230 | 63.797 | 5.04% |
| `storage.check-owned` | 14,859 | 6.996 | 0.55% |
| `storage.final-audit` | 11 | 5.711 | 0.45% |

These rows group exclusive timings by the final operation name across trace paths. They do not double-count inclusive nested scopes and do not exhaust the run. Durable journal flushes remain a substantial measured cost and remain enabled. Owned storage checks consumed approximately 0.55% of the performance window. The full path-level timing data remains in `performance.json`.

This run uses new roots and the fixed captured-v19 guardian Health/Power +10% content. It is **not an identical-input A/B comparison** with original v19, and no causal speedup against that experiment is claimed here. The earlier dedicated performance plan and measured fixture results remain separate evidence.

## Verification, preservation and resource limits

Native reconstruction passed in **170.922 seconds**. Independent audit passed in **35.828 seconds**, checking all **45,824 compact records in 4,888 chunks**, ordered seed schedules, outcomes, exact durable attempt charges, selection, recipe origins, confidence calculations and complete archive hashes. Neither verification reran combat. Every required execution and audit command completed successfully.

All **331 approved new values** are durably reserved, bringing the preserved union to **482,222**. All four new stages were used; there were zero rejected allocation candidates. Original unused512 and unused32 remain reserved and unused; no historical reservation was released or reassigned. Earlier preparation failures remain sealed and charged. Combined output at independent audit was **2.322 GiB / 4 GiB**. The pre-publication storage check also passed both sublimits: study **1.487 / 3 GiB**, all preparation/control directories **0.835 / 1 GiB**. Final charged time and bytes are recorded in the [execution closure](Tower-Deep-Challenger-Execution-Review.md) and `completion.json`. The 180-minute cumulative limit includes preparation, allocation, execution, verification and publication, excluding the user decision wait.

The pre-publication inventory compared **4,390 existing checkout files** and observed **3 concurrent changes outside this study**: `IdleCombatInventoryTrackingTests.cs`, `InventoryRepository.cs` and `build/measure-idle-combat.ps1`. They were left untouched; before/after hashes are retained. The first supplemental report draft stopped on an overly strict unchanged-checkout assertion. Its source and failure observation are preserved in `report-measurements-first-draft.py` and `publication-notes.json`; the publication check was corrected to record concurrent work. No frozen study command was repeated. This turn adds evidence and this report and updates seven active Markdown handoffs. The study's executable source, gameplay content and optimizer defaults were not changed. There are no migrations, deployment or production configuration changes from this work. The preceding preparation's **91 + 3 passing focused tests through `build/run-tests.ps1`** remain retained evidence; no tests were rerun in this execution-only turn.

## Reproducible command record

The frozen commands below were executed once, with input/source/executable hashes and create-only phase receipts. They document reproduction of the workflow; do not rerun them against the sealed study. A distinct future experiment needs its own scope and allocation, not a retry of this package. Supplemental Markdown publication was corrected after the report-only assertion described above.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-deep-challenger-readiness-20260915'
& $py -B "$work/workflow.py" reserve-331 --approved-331
& $py -B "$work/workflow.py" run
& $py -B "$work/workflow.py" verify
& $py -B "$work/audit.py" execution
& $py -B "$work/finish.py" execution
```

Between independent audit and final sealing, `report-measurements.py` published this saved-evidence analysis; its first draft and corrected publication are both retained. This administrative correction allocated no values and ran no fights or diagnostics.

Machine-readable results: `TestResults/balance/tower-deep-challenger-execution-20260915/measurements.json`. Raw study: `TestResults/balance/tower-deep-challenger-study-20260915`, sealed by `final-files.json`. Execution receipts and this report copy are sealed by `evidence-files.json`; the readiness source retains its original seal.

## What this resolves and what remains

The unchanged deep method did not recover a viable primary in any of these three starts against the fixed +10% boss. A selected-family Pass does not repair that search failure, certify all 4,608 generated teams, certify all 43,879 previously retained teams or establish global optimality. The earlier focused Pass still excludes 42,890 other retained teams.

Discovery feedback was sparse: root 1: 1,525 teams at 0/8, 11 teams at 1/8; root 2: 1,536 teams at 0/8; root 3: 1,514 teams at 0/8, 21 teams at 1/8, 1 teams at 2/8. Across 4,608 candidates, 4,575 had zero discovery wins. This supports inspecting search feedback and recipe differences as the next investigation; it does not establish the cause of the failure. The useful next step is a zero-combat comparison of the successful control recipes with these saved search trajectories before designing another search change or requesting more fights. No further study, tuning or adoption is started by this closure.
