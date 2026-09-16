# Authored core portfolio: implementation review

16 September 2026. **VerifiedCorePortfolio**. Zero fights, runtime preparations, fresh values and retries. All **482,776 reservations** remain preserved.

## Implementation

`independent-team-core-portfolio-v1` / `team-core-portfolio` uses the unchanged 768-recipe filler pool. Each recipe is grouped by the complete set of authored cores it contains. The allocator balances exposure of those profiles across completed teams, keeps the existing alternating variety/reuse schedule, and prioritizes missing roles when remaining slots are no more than missing roles. Terminal coverage, shared inventory, legal copies, state/check/party caps, cancellation and durable proposal charging remain authoritative.

A profile is a structural hypothesis, not a combat-strength score. No control recipe, historical outcome or generation-seed optimization enters the new comparator. Equipped Essence order stays canonical. Existing allocator entry points retain their exact comparator; defaults are unchanged. The new version uses the existing trace schema, with auditable profiles/priorities in the [independent decision reconstruction](../TestResults/balance/tower-core-portfolio-20260916/decision-audit.json).

## Measured construction coverage

The new opt-in policy represented 96/198 authored-core profiles versus 43/198, and 168/282 same-owner core pairs versus 19/282. All 16 captured teams completed; eleven existing policies retain exact parity. Combat benefit remains unmeasured.

| Metric | Filler diversity | Core portfolio |
| --- | ---: | ---: |
| Pool recipes | 768 | 768 |
| Distinct recipes used | 81 | 96 |
| Core profiles used | 43 | 96 |
| Authored cores represented | 39 | 46 |
| Same-owner core pairs represented | 19 | 168 |
| Owners containing multiple cores | 11 | 132 |
| Distinct first-slot recipes | 2 | 16 |
| Distinct first-slot profiles | 2 | 16 |
| Largest profile exposure | 14 | 9 |
| Maximum loadout repetition within a team | 10 | 9 |
| Allocation states | 176 | 176 |
| Candidate checks | 122880 | 122880 |

Available in the unchanged pool: **198 profiles, 48 authored cores, 282 distinct same-owner core pairs**. Exposure counts character placements, not combat trials. All 48 core and 80 provider frequencies, every profile and core-pair exposure, full teams and subset counts are retained in [measurements.json](../TestResults/balance/tower-core-portfolio-20260916/measurements.json). New first-slot role counts: `[1, 3, 3, 3, 3, 3, 4, 3, 1, 2, 2, 3, 3, 2, 4, 3]`. Every complete team still covers all five required roles.

The observed captured allocation follows 16 complete first paths: **176/256 states**, **122,880/250,000 checks**, **16/16 parties**. No extra pool search, candidate refill or cap increase occurred. Broader represented combinations do not establish stronger teams; all diagnostic evaluations were fabricated.

## Saved-control overlap, measured after generation

The unchanged pool contains **zero of the ten exact control recipes**, so neither policy can select them. All **191** control subsets were measured: **31** have more generated owners under the new policy, **19** fewer, **141** unchanged. These comparisons are descriptive and never influenced allocation or the pass condition. The examples below were fixed by control frequency and ordinal IDs before execution.

| Control subset | Control owners | Pool recipes | Filler generated owners | Portfolio generated owners |
| --- | ---: | ---: | ---: | ---: |
| enchanted_fairy, pack_howler | 18/20 | 16 | 0/160 | 12/160 |
| enchanted_fairy, pack_howler, venomous_spiderling | 17/20 | 8 | 0/160 | 3/160 |
| enchanted_fairy, pack_howler, spider_queen_royal_venom, venomous_spiderling | 13/20 | 1 | 0/160 | 0/160 |

Missing exact recipes remain a pool limitation. Representing a subset does not prove that it is useful, necessary or sufficient for combat. The completed comparison still has 0/32 wins for every finalist/control; no new balance evidence is added here.

## Verification and timing

**83 backend tests passed** through `build/run-tests.ps1`: the previous 71 plus 12 focused portfolio tests. The new exhaustive fixture compares all 243 small inventory/role/party cases with independently enumerated feasible assignments. Other tests cover profile exposure, shared copies, empty/invalid metadata, canonicalization, state/check caps, cancellation, registration, missing-role attempt charging, checkpointed failure and generation-label independence.

The isolated native fixture retained exact full output parity for **eleven existing policies**, plus exact metadata-order parity for the new policy. **320 captured synthetic evaluations**, one additional direct allocation and all test evaluations used combat-entry guards. There were no fights, runtime preparations, fresh seeds or retries. A separate Python reader derives profiles from the raw authored cores, verifies every captured selected prefix and comparator key, reconstructs exposure/state/check totals, and matches the complete direct allocation result, generated recipes, identities and traces. Its captured proof is restricted to the unrestricted saved input; the exhaustive tests cover small constrained cases.

| Captured case | Seconds | Allocated bytes | Synthetic evaluations |
| --- | ---: | ---: | ---: |
| legacy | 0.0677 | 37,634,224 | 16 |
| diverse | 0.0891 | 42,569,872 | 16 |
| team | 0.0828 | 49,941,512 | 16 |
| filler | 0.3890 | 241,999,120 | 16 |
| portfolio | 0.4177 | 248,315,680 | 16 |
| reordered | 0.3939 | 248,315,944 | 16 |

Total native diagnostic: **1.8290 seconds**, **1.9062 CPU seconds**, **988,383,160 allocated bytes**, **93,831,168 peak working-set bytes**. This includes the seven old-policy fixtures and additional direct allocation. These are single-run construction measurements, not a whole-campaign speedup or combat-performance claim.

## Next boundary

Review these coverage and cost changes before preparing any further combat comparison. A comparison needs its own frozen package, fresh-seed exception and enough measured remaining time for live-history checks, execution and both verification passes. The last 45-value approval is exhausted. No combat, time extension or default adoption is authorized by this zero-combat implementation.


## Resource accounting and reproduction

| Completed phase before publication | Seconds |
| --- | ---: |
| audit | 0.454 |
| build | 3.609 |
| captured | 2.015 |
| freeze | 5.469 |
| test-build | 1.907 |
| tests | 1.875 |

Incoming cumulative workload: **2,554.347 seconds**. The [protocol](Tower-Core-Portfolio-Protocol.md) limits this complete scope to **80 seconds / 192 MiB**, within the approved cumulative **2,700-second / 4 GiB** ceilings. The [completion receipt](../TestResults/balance/tower-core-portfolio-20260916/completion.json) records actual remaining time/output and a one-second closure allowance. Compilation/tests are charged and a 1 MiB shared-test allowance is included. Failure receipts: none. Missing success receipts indicate unrun dependent phases; never retry a sealed/failed package.

Recorded commands, once each:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-core-portfolio-20260916'
& $python -B "$work/setup.py"
# Implement/review the scoped source and frozen scripts.
& $python -B "$work/assemble.py"
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Tests use `build/run-tests.ps1 -NoBuild -ArtifactsPath` with exactly seven frozen class filters; exact commands/logs/TRX, isolated DLL hashes, before/after source, performance output and the file seal are retained. All **63 predecessor packages** verified unchanged. No live registry scan, full gameplay build or full backend suite ran.

Changed C#: new `TowerCorePortfolioSearch`, allocator opt-in entry/comparator, structural adapter, five policy registration/provenance files and `BalanceHarnessCorePortfolioTests`. Also updated protocol/review, evidence scripts and six Markdown handoffs. No filler-pool implementation change. Unrelated dirty work is preserved. No gameplay/configuration change, migration or deployment.

V19 retains all 253 required recipes and unused 512 original confirmation values; v19 Unresolved, reliability Fail 1/3, deep recovery 0/3, adoption Hold. No Kharad tuning, ability-order optimization or large confirmation.
