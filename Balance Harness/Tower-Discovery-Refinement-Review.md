# Discovery refinement: implementation verification

16 September 2026. **VerifiedDiscoveryRefinement**. Zero fights, preparations, fresh values and retries. All **482,821 reservations** preserved.

## Implementation

The opt-in `independent-discovery-refinement-v1` / `discovery-refinement` selects parents using completed discovery results from the current arm. Fresh construction uses the unchanged team-coverage constructor. Initial four proposals are fresh, then three refinement attempts alternate with one fresh attempt: seven fresh and nine refinement attempts in the captured 16-proposal run. If no parent has completed, a charged fresh attempt is used instead.

Each refinement selects the current highest-ranked completed team and reuses existing loadout distribution, loadout refinement or whole-character replacement. The loadout library comes only from measured current-arm teams. Whole-character replacement can produce recipes outside the finite fresh pool; coordinated refinement changes matching owners together. This reuses existing search operators; no new combat-strength formula, reference input or confirmation feedback is introduced.

Shared copy/family/slot legality, canonical Essence order and collective role coverage remain required. Duplicate and invalid edits consume proposals and never fight. The existing checkpoint and cancellation path remains authoritative. New policy registration permits one label and at most 16 candidates/proposals, preserving all existing constructor/allocation bounds. Current policy branches, defaults and gameplay are unchanged.

## Measured zero-combat behavior

All 95 tests passed; twelve existing policies retain exact full-output parity. Reversing fabricated discovery outcomes changes later parents and recipes. The fixed 16 attempts produced 16 forward and 14 reverse accepted evaluations; rejected/duplicate proposals remained charged. Combat strength is unmeasured.

| Fabricated case | Attempts | Accepted evaluations | Status | Rejection counts |
| --- | ---: | ---: | --- | --- |
| refinement | 16 | 16 | Complete | {} |
| repeat | 16 | 16 | Complete | {} |
| reordered | 16 | 16 | Complete | {} |
| reverse | 16 | 14 | Incomplete | {'missing-team-roles': 2} |

All cases preserve the same four initial fresh teams. Exact repeat and reversed-metadata outputs match. Reversed fabricated fitness changes later parent choices and recipes, showing that current discovery results now influence construction. This is a behavior test with fabricated defeats and health values, not evidence of improved combat. Early discovery results may be noisy; the new policy has no reliability or optimality claim.

The 95 backend tests consist of the prior 83 and twelve focused refinement tests, run through `build/run-tests.ps1`. Tests cover changed parents, fixed schedules/caps, deterministic/canonical output, completed ancestry, collective roles, coordinated copy limits, rejection/duplicate charging, invalid provenance/order, checkpointed cancellation/failure and unavailable separate feedback. No test enters combat.

The independent Python audit checks every captured proposal's schedule, highest-ranked earlier parent, library/hash and donor lineage, distribution reconstruction, bounded changed slots/loadouts, family/role legality, exact rejection/duplicate counts, fabricated evaluation sequence, status and reservation union. It does not independently reproduce the pseudorandom mutation stream or simulate combat. All twelve prior-policy complete outputs match their sealed fixtures exactly.

| Captured case | Seconds | Allocated bytes | Synthetic evaluations |
| --- | ---: | ---: | ---: |
| legacy | 0.0673 | 37,621,104 | 16 |
| diverse | 0.0886 | 42,544,704 | 16 |
| team | 0.0822 | 49,929,984 | 16 |
| filler | 0.3589 | 241,991,400 | 16 |
| portfolio | 0.3632 | 248,300,016 | 16 |
| refinement | 0.0960 | 52,415,464 | 16 |
| repeat | 0.0925 | 52,414,008 | 16 |
| reordered | 0.1071 | 52,443,432 | 16 |
| reverse | 0.0931 | 52,392,216 | 14 |

Total native diagnostic: **1.6258 seconds**, **1.6562 CPU seconds**, **900,012,200 allocated bytes**, **93,667,328 peak working-set bytes**. This includes the seven earlier parity fixtures; total captured synthetic evaluations: **366**. Single-run measurements characterize this fixture, not a campaign speedup. Exact proposals, parent traces, outcomes and performance are retained in [measurements.json](../TestResults/balance/tower-discovery-refinement-20260916/measurements.json).

## Remaining boundary

Assess the captured rejection counts and incomplete status before preparing any combat comparison. The current comparison adapter requires 16 completed evaluations and cannot silently accept a shortened discovery stage. Do not raise proposal caps, refill candidates or request fights to conceal this limitation. Any follow-up needs a frozen scope and the measured remaining budget; a fresh-seed exception would be separate. Adoption remains Hold.


## Resources, preservation and reproduction

The [frozen protocol](Tower-Discovery-Refinement-Protocol.md) specifies this 80-second / 192 MiB scope. Incoming cumulative work was **2,821.442 seconds**, leaving **178.558** under the approved **3,000-second / 4 GiB** limits. The [completion receipt](../TestResults/balance/tower-discovery-refinement-20260916/completion.json) records actual time/output and remaining allowance, including a 1 MiB shared-test allowance and one closure second. Source editing is excluded; compilation, tests, diagnostics, audits and publication are charged.

| Completed phase before publication | Seconds |
| --- | ---: |
| audit | 0.297 |
| build | 3.656 |
| captured | 1.797 |
| freeze | 6.484 |
| test-build | 1.968 |
| tests | 1.968 |

All **68 predecessor packages** verified unchanged. Failure receipts: none. Missing success receipts identify unrun dependent commands. No retry, replay, replacement run, seed allocation or budget reset.

Run once, after engineering setup and source/protocol review; never rerun a sealed/failed directory:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-discovery-refinement-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Exact build and `build/run-tests.ps1 -NoBuild -ArtifactsPath` commands with eight frozen class filters, TRX, logs, source snapshots and binary hashes are retained. No full gameplay build/backend suite, live registry scan or combat comparison ran.

Changed C#: new `TowerDiscoveryRefinementSearch`, scoped generation branch and registration/provenance/role checks, structural fresh-policy adapter, loadout-operator opt-in and `BalanceHarnessDiscoveryRefinementTests`. Also added protocol/review/evidence scripts and updated six Markdown handoffs. No allocator/comparator or pool-constructor change. Unrelated dirty work is preserved. No gameplay/configuration change, migration or deployment.

V19 retains 253 recipes and unused original 512 confirmation values. V19 Unresolved, reliability Fail 1/3, deep recovery 0/3, adoption Hold. No Kharad tuning, ability-order optimization or large confirmation.
