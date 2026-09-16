# Team coverage and legal loadout repetition: implementation review

**Follow-up completed — 15 September 2026:** the [team-coverage versus diversity comparison](Tower-Team-Coverage-Comparison-Execution-Review.md) has now verified **288 fights**. The team-coverage finalist left 63.06% boss health versus 79.30% for diversity, but both finalists and both controls won 0/32. The difference in remaining health is descriptive; no win-rate improvement is established. Reservations now total **482,731**, with **108.978 diagnostic seconds remaining at execution publication**; adoption stays **Hold**. The implementation-only measurements and former next step below remain a historical record, and their sealed snapshot is unchanged.

15 September 2026. **VerifiedTeamCoverage**. The new policy generated 16 complete teams with collective role coverage: 160/160 character loadouts are specialists and 8/16 teams reuse loadouts. Both saved controls pass the new rules with sufficient shared copies; removing one required copy rejects each. Combat strength remains unmeasured. Zero fights, preparations, fresh values, replays or retries.

## Implemented behavior

`independent-team-coverage-v1` / `team-coverage` requires the complete team to cover all five authored roles. Individual characters can specialize, and the same loadout can appear on up to ten characters, subject to one shared copy inventory. These are construction rules; role labels do not establish targeting compatibility, uptime or strength.

`ConstructComplete` fills every Essence slot and permits an empty local role requirement. Existing `Construct` still stops when its requested roles are covered. For each authored core, the new pool combines one unrestricted recipe with up to three recipes focused on each of the five roles. Those six searches share the original 256-state / 16-recorded-recipe cap. The captured 48 cores produced **768 recorded recipes / 560 distinct full recipes**, including **560 specialist recipes**. Each focus, state charge, anchor and role witness is retained in [pool evidence](../TestResults/balance/tower-team-coverage-20260915/pool.json).

`AllocateCovered` prioritizes missing team roles, then recipes least exposed in earlier teams. Its fixed schedule alternates between variety and reuse preferences. All restarts share the 256-state / 250,000-check / 16-party limits. Complete-team identities suppress duplicates; cancellation, copy limits and incomplete-cap reporting remain active. No control recipes or combat measurements enter generation. Fixed ordinal ability order remains unchanged.

## Captured construction measurements

| Policy | Full pool | Loadouts used | Specialist owners | Teams with repeats | Fixed character slots | Pair replacements: min / median / max | States / 256 | Checks / 250,000 |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: |
| Structural v1 | 680 | 25 | 0/160 | 0/16 | 9 | 1 / 1 / 1 | 27 | 37,400 |
| Diverse v1 | 680 | 160 | 0/160 | 0/16 | 0 | 10 / 10 / 10 | 176 | 108,800 |
| Team coverage | 560 | 96 | 160/160 | 8/16 | 0 | 10 / 10 / 10 | 176 | 89,600 |

Each policy retained 16 complete teams. The original structural and diverse outputs match their sealed results exactly. All **360 pair distances** are retained in [measurements](../TestResults/balance/tower-team-coverage-20260915/coverage-measurements.json). Differences describe candidate construction and cannot establish combat improvement. The new policy intentionally changes pool eligibility and repetition as well as role allocation; it is not a single-variable estimate of either restriction's combat effect.

| Team-coverage candidate | Largest repetition of a loadout | attack-enabler | enemy-pressure | protection | recovery | recurring-control |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | 6 | 8 | 6 | 5 | 3 |
| 2 | 9 | 10 | 10 | 9 | 9 | 1 |
| 3 | 1 | 4 | 10 | 7 | 6 | 2 |
| 4 | 9 | 1 | 10 | 10 | 1 | 9 |
| 5 | 1 | 6 | 10 | 6 | 2 | 1 |
| 6 | 9 | 1 | 10 | 1 | 1 | 9 |
| 7 | 1 | 5 | 10 | 4 | 2 | 5 |
| 8 | 9 | 10 | 10 | 1 | 1 | 9 |
| 9 | 1 | 5 | 10 | 4 | 4 | 3 |
| 10 | 9 | 1 | 10 | 1 | 1 | 9 |
| 11 | 1 | 5 | 9 | 6 | 6 | 1 |
| 12 | 9 | 1 | 10 | 1 | 1 | 9 |
| 13 | 1 | 5 | 9 | 7 | 5 | 2 |
| 14 | 9 | 1 | 10 | 10 | 9 | 9 |
| 15 | 1 | 4 | 10 | 7 | 4 | 1 |
| 16 | 9 | 1 | 10 | 10 | 1 | 9 |

Role columns count characters carrying at least one provider for that role. Every row covers all five roles somewhere. Repeated loadouts still consume one copy of each Essence on each character. No repeated character loadout is treated as an independent strength observation.

## Saved control feasibility

| Saved control | Sufficient inventory accepted | One-copy shortage rejected | Distinct loadouts | Largest repetition | Owners represented in generated pool |
| --- | --- | --- | ---: | ---: | ---: |
| `team-040e60d3dbc5c127321653c47ed3a9d3` | yes | yes | 7 | 3 | 0/10 |
| `team-49f6979895354870c89362d4abf214bb` | yes | yes | 7 | 3 | 0/10 |

These fixed-slot fixtures demonstrate that the new rules permit both exact control compositions, while enforcing inventory. Their recipes were read only after candidate generation and were never inserted into the generated pool. **Eligibility is not discovery:** pool membership above reports whether the bounded constructor actually supplied those exact loadouts. This check does not prove that the search will rediscover either control.

## Verification and timing

**59 backend tests passed** through `build/run-tests.ps1`: 45 existing constructor/allocator/structural/diversity tests and 14 new team-coverage tests. New exhaustive checks cover 512 small full-loadout cases and 81 small collective assignment cases. Remaining fixtures cover specialty/repetition, shared copies, alternating preferences, caps, cancellation, canonical/immutable inputs, registration, missing-role attempts, checkpointed failure charges and provenance.

The seven earlier policy fixtures preserve exact results for 224 synthetic evaluations. Structural v1 and diverse v1 each retain exact captured 16-candidate parity. Team coverage is identical under reversed metadata order. All **288 synthetic evaluations** use fabricated results and a combat-entry guard; none is a fight or fresh seed allocation. Independent Python checks verify the full focus schedule, pool, every captured team, controls, shortages, traces, identities and shortlist membership.

| Captured generation case | Seconds | Allocated bytes | Synthetic evaluations |
| --- | ---: | ---: | ---: |
| legacy | 0.0698 | 37,467,688 | 16 |
| diverse | 0.0858 | 42,369,088 | 16 |
| team | 0.0833 | 49,780,080 | 16 |
| reordered | 0.0828 | 49,766,216 | 16 |

The native diagnostic, including parity fixtures, one additional pool construction and four fixed-control allocator calls, used **0.6337 seconds**, **0.6406 CPU seconds** and **85,393,408 peak working-set bytes**. These are bounded single-run construction measurements, not whole-study throughput or a statistical performance comparison.

## Historical next work — completed by the comparison

Prepare a bounded comparison of the existing diverse policy against team coverage, keeping the saved controls, gameplay inputs, ability order and selection rules fixed. Carry the remaining time/output budget forward. No fresh seed values or fights are authorized by this implementation; any required fresh-value exception must be explicit after preparation is concrete and verified.

That comparison is now complete. The current recommendation is the bounded saved-data diagnosis in the [execution review](Tower-Team-Coverage-Comparison-Execution-Review.md#next-scoped-work), with zero fights or fresh values. Do not repeat this preparation or treat its historical 482,686 reservation total as current.

## Limits, reproduction and preservation

| Workload phase before publication | Seconds |
| --- | ---: |
| audit | 0.234 |
| build | 3.562 |
| captured | 0.797 |
| freeze | 5.000 |
| setup | 0.188 |
| test-build | 1.813 |
| tests | 1.843 |

Incoming cumulative time: **2042.554 seconds**; incoming output including the additional 1 MiB shared-test charge: **2,647,376,248 bytes**. This scope conservatively charges compilation as well as diagnostics. The [completion receipt](../TestResults/balance/tower-team-coverage-20260915/completion.json) includes measured publication plus one closure second, actual output before seal and remaining time. Limits remain 300 seconds / 192 MiB for this scope within cumulative 2,400 seconds / 4 GiB. Failure receipts: **none**. Dependent commands stop at failure; absent success receipts identify unrun phases.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-team-coverage-20260915'
& $python -B "$work/setup.py"
# Apply the frozen source change before freezing.
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The test phase invokes `build/run-tests.ps1 -NoBuild -ArtifactsPath` against isolated compiled tests, with exactly the five frozen class filters. Exact commands/projects, source snapshots, results, [protocol](Tower-Team-Coverage-Protocol.md), [input hashes](../TestResults/balance/tower-team-coverage-20260915/freeze.json) and [file seal](../TestResults/balance/tower-team-coverage-20260915/files.json) are retained. Do not rerun a sealed/failed directory; reproduction requires a new frozen output and budget.

Changed source: `TowerJointLoadoutConstructor`, `TowerJointPartyAllocator`, `TowerJointStructuralSearch`, new `TowerTeamCoverageSearch`, the five policy registration/validation files, and new `BalanceHarnessTeamCoverageTests`. Also updated this protocol/review, six active Markdown handoffs and the separate evidence package. The isolated build uses sealed gameplay assemblies; no shared binaries or gameplay content changed. No full gameplay/backend suite, migrations, configuration changes or deployment.

All **53 predecessor packages / 20,678 indexed files** were verified unchanged before/after. Preserve unrelated dirty work, all **482,686 reservations**, all 253 v19 recipes and unused 512 original confirmation values. Reliability **Fail 1/3**, deep recovery **0/3**, v19 **Unresolved**, adoption **Hold**. No Kharad tuning, ability-order optimization or 129,536-fight confirmation.
