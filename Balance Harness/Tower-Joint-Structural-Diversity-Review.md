# Joint structural candidate diversity: implementation review

15 September 2026. **VerifiedStructuralDiversity**. Across 16 teams, distinct character loadouts increased from 25 to 160 of 680; fixed slots fell from 9 to 0, and median pairwise loadout replacement distance rose from 1 to 10. This verifies broader construction coverage; combat strength is unmeasured. Zero fights, fresh values, preparations or replays; zero retries.

## Measured construction change

| Policy | Teams | Distinct full loadouts | Fixed character slots | Pairwise loadout replacements: min / median / max | Global states | Global candidate checks |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| Existing v1 | 16 | 25/680 | 9 | 1 / 1 / 1 | 27/256 | 37,400/250,000 |
| Diverse opt-in | 16 | 160/680 | 0 | 10 / 10 / 10 | 176/256 | 108,800/250,000 |

All 120 candidate pairs per policy are retained in [coverage measurements](../TestResults/balance/tower-joint-structural-diversity-20260915/coverage-measurements.json). Multiset replacement distance measures composition rather than simply moving the same recipe between characters. Both policies use the identical captured 680-recipe pool and fixed ordinal ability order. The new gate required at least 80 distinct loadouts, no fixed slot and median replacement distance at least five; all gates passed.

| Character slot | Existing v1 alternatives | Diverse alternatives |
| --- | ---: | ---: |
| 1 | 1 | 16 |
| 2 | 1 | 16 |
| 3 | 1 | 16 |
| 4 | 1 | 16 |
| 5 | 1 | 16 |
| 6 | 1 | 16 |
| 7 | 1 | 16 |
| 8 | 1 | 16 |
| 9 | 1 | 16 |
| 10 | 16 | 16 |

The allocator now offers `AllocateDiverse`, which favors loadouts least used by earlier retained parties, backtracks under shared inventory, and restarts after each new complete party. Its state/check counters span every restart. Stable static pool-size ordering avoids repeatedly scanning every remaining slot's pool. Duplicate complete party identities are skipped; hitting any cap reports incomplete search. The legacy `Allocate` entry point retains its traversal and remains the default.

The separate `independent-joint-structural-diverse-v1` / `joint-structural-diverse` registration uses this traversal. Generation limits, checkpoint-before-evaluation charging, cancellation and provenance checks remain active. The cache is published only after complete bounded construction returns. No old policy version is repurposed.

## Fixed role and repetition requirements

The new policy explicitly requires **all five authored roles on each character** and **one use of each loadout per team**. `TowerJointStructuralDiversity` names these requirements, and tests enforce them. They isolate this candidate-coverage change and are not gameplay legality rules or strength guarantees. The [saved diagnosis](Tower-Joint-Structural-Diagnosis-Review.md) showed that both controls instead distribute roles across their teams and repeat some legal loadouts. Those exact teams remain excluded by these restrictions.

No control Essence IDs, saved win rates, ability-order tuning or new gameplay content guide the new traversal. Broader recipe exposure can include weak recipes; it does not establish recovery of controls or better win probability.

## Tests, parity and performance

**34 backend tests passed** through `build/run-tests.ps1`: 10 existing allocator, 12 existing structural-search and 12 new diversity tests. The new exhaustive fixture compares all assignments for 27 small size/inventory cases; other tests cover fixed-prefix prevention, shared copies, immutable/canonical inputs, constrained slots, duplicate/exhaustion handling, global caps, explicit registration, attempt charging, failures, cancellation and metadata-order parity. Every evaluation uses synthetic results and combat entry is guarded.

The seven prior-policy fixtures retain **exact complete output parity** for 224 synthetic evaluations. Captured structural v1 matches its previously sealed 16-candidate result exactly. The new policy's 16-candidate result matches a second construction with reversed metadata order. The independent audit verifies every captured recipe's identity, pool/family/role/repetition/copy constraints, provenance, counters and shortlist membership. These 272 captured/parity synthetic evaluations are not fights and allocate no seed values.

| Captured construction case | Seconds | Allocated bytes | Synthetic evaluations |
| --- | ---: | ---: | ---: |
| legacy | 0.0633 | 37,378,168 | 16 |
| diverse | 0.0810 | 41,822,864 | 16 |
| reordered | 0.0748 | 41,830,008 | 16 |

Total native diagnostic time including the seven parity fixtures: **0.4988 seconds**; CPU **0.5000 seconds**; peak working set **83,116,032 bytes**. These single-process measurements describe construction cost, not whole-study throughput or a statistical speed comparison. The new traversal can use more states/checks to cover more combinations; limits remain unchanged.

## Next work

Specify a separate generic policy for team-level authored coverage and repeated legal loadouts, starting with zero-combat feasibility and inventory fixtures. The new diversity policy still deliberately retains the earlier per-character role and uniqueness rules, which exclude both controls. Do not launch more fights or relax both constraints implicitly; any combat comparison needs a separately frozen authorized scope.

## Resource accounting and reproduction

| Workload phase before publication | Seconds |
| --- | ---: |
| audit | 0.235 |
| build | 3.625 |
| captured | 0.671 |
| freeze | 4.734 |
| setup | 0.172 |
| test-build | 1.781 |
| tests | 1.782 |

This scope conservatively charges compilation as well as diagnostics. Incoming cumulative diagnostic time: **2024.695 seconds**; prior output including the extra shared-test allowance: **2,587,477,452 bytes**. The [completion receipt](../TestResults/balance/tower-joint-structural-diversity-20260915/completion.json) includes measured publication plus one conservative closure second, cumulative/remaining time and actual new output before sealing. Scope limits: 300 seconds / 192 MiB, within the unchanged cumulative 2,400 seconds / 4 GiB. Failures: **none**. Unrun dependent commands are identified by absent success receipts; no failed command was retried.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-joint-structural-diversity-20260915'
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

The test phase uses `build/run-tests.ps1 -NoBuild -ArtifactsPath` with the isolated compiled tests and the three frozen class filters. Exact commands, project files, sources, results, [protocol](Tower-Joint-Structural-Diversity-Protocol.md), [input hashes](../TestResults/balance/tower-joint-structural-diversity-20260915/freeze.json) and [file seal](../TestResults/balance/tower-joint-structural-diversity-20260915/files.json) are retained. Do not rerun a sealed/failed directory; reproduction needs a new frozen output and budget.

Changed source: `TowerJointPartyAllocator`, `TowerJointStructuralSearch`, new `TowerJointStructuralDiversity`, the five generation/contract/mechanics/composition registration files, and new `BalanceHarnessJointDiverseTests`. Also changed this protocol/review, six active Markdown handoffs and the separate evidence package. Captured gameplay assemblies were reused in isolated builds; no shared binaries or gameplay content changed. No full gameplay/backend suite was run, because this frozen scope targets offline search. No migrations, configuration changes or deployment.

All **52 predecessor packages / 20,126 indexed files** were verified unchanged before/after. Preserve all **482,686 reservations**, 253 v19 recipes and 512 unused original confirmation values. Unrelated dirty work remains intact. Reliability **Fail 1/3**, deep recovery **0/3**, v19 **Unresolved**, adoption **Hold**. No Kharad tuning, ability-order optimization or 129,536-fight confirmation.
