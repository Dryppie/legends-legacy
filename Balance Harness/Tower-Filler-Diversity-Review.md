# Deterministic filler diversity: implementation review

15 September 2026. **VerifiedFillerDiversity**. The new opt-in policy produced 768 distinct recipes and 16 complete teams. Maximum provider presence changed from 512/560 to 297/768. All ten existing policies retain exact parity. Combat benefit remains unmeasured. Zero fights, runtime preparations, fresh seeds or retries.

## Implementation

`independent-team-filler-diverse-v1` / `team-filler-diverse` adds outcome-independent diversification during recipe construction. Existing policies/defaults retain their exact behavior. `ConstructCompleteInProviderOrder` validates a complete provider permutation and still emits ordinal equipped Essence IDs. The new policy orders providers by prior non-anchor filler exposure and a stable, versioned hash tie-break, then requests one recipe per pass. Distinct retained recipes update exposure; anchors do not. Every pass records priority hashes, role, ordinal, result and charged states.

The schedule has one unrestricted pass and up to three for each of five roles, stopping a focus early when exhausted. All passes share **256 states / 16 recorded recipes per core**; allocation remains **256 states / 250,000 checks / 16 teams**. Shared copies, role coverage, repetition, cancellation and charged proposals/checkpoints remain enforced. Generation labels do not change the finite construction. Controls and combat outcomes never enter generation, and the equipped ability order remains fixed.

## Measured construction changes

| Policy | Distinct pool | Distinct recipes used | Providers present | Largest provider count | Specialist owners | Allocation states | Candidate checks |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Team coverage v1 | 560 | 96 | 35/80 | 512 | 160/160 | 176 | 89,600 |
| Filler diversity v1 | 768 | 81 | 80/80 | 297 | 135/160 | 176 | 122,880 |

The captured 48 cores made **768 passes**, recording **768 recipes** before deduplication. Per-core states ranged from **64 to 80 / 256**. Full pass reconstruction and exposure evidence are in [pool.json](../TestResults/balance/tower-filler-diversity-20260915/pool.json) and [measurements.json](../TestResults/balance/tower-filler-diversity-20260915/measurements.json). All 240 pairwise team replacement distances for old/new construction are retained there; new distances range 9–10, median 9.

| Previously most common provider | Old pool presence | New pool presence | Old recorded non-anchor uses | New recorded non-anchor uses |
| --- | ---: | ---: | ---: | ---: |
| essence.alpha_wolf | 512/560 | 26/768 | 720 | 26 |
| essence.bark_golem | 239/560 | 27/768 | 410 | 27 |
| essence.blackjaw_spider | 213/560 | 43/768 | 273 | 27 |
| essence.blood_harpy | 206/560 | 90/768 | 211 | 26 |
| essence.pack_howler | 190/560 | 297/768 | 0 | 25 |

Pool presence counts distinct recipes. Recorded filler use counts all retained pass outputs, including duplicates, and excludes each recipe's anchor; it is not a character-owner or combat-trial count. These measurements describe construction concentration and do not establish win probability or combat strength.

## Saved control coverage, inspected after generation

| Fixed control | Old exact owner recipes in pool | New exact owner recipes in pool | Old distinct control recipes | New distinct control recipes |
| --- | ---: | ---: | ---: | ---: |
| team-040e60d3dbc5c127321653c47ed3a9d3 | 0/10 | 0/10 | 0 | 0 |
| team-49f6979895354870c89362d4abf214bb | 0/10 | 0/10 | 0 | 0 |

All **191** frozen control subsets were measured: **75** have higher new-pool counts, **25** lower counts, and **91** unchanged. The three rows below are the predeclared most frequent size-two/three/four control subsets, tied by ordinal IDs; complete data include every subset and both generated owner counts. No control recipe was injected or made a required output.

| Control subset | Control owners | Old pool | New pool | Old generated owners | New generated owners |
| --- | ---: | ---: | ---: | ---: | ---: |
| enchanted_fairy, pack_howler | 18/20 | 51 | 16 | 13/160 | 0/160 |
| enchanted_fairy, pack_howler, venomous_spiderling | 17/20 | 24 | 8 | 10/160 | 0/160 |
| enchanted_fairy, pack_howler, spider_queen_royal_venom, venomous_spiderling | 13/20 | 0 | 1 | 0/160 | 0/160 |

Greater diversity alone does not establish useful mechanic combinations or recovery of the controls. The completed combat comparison remains 0/32 wins for each finalist/control; this zero-combat scope cannot change that conclusion. Any coverage regressions above are retained without retuning or retries.

## Verification and timings

**71 backend tests passed** through `build/run-tests.ps1`: the existing 59 and 12 new tests. These include 512 exhaustive small role/copy/slot cases with alternate traversal, permutation validation, canonical output, pool caps/exposure, exhaustion, immutability, registrations, incomplete search and checkpointed cancellation/failure. The isolated native fixture retains exact complete outputs for **ten existing policies**, and exact metadata-order parity for the new policy. All **304 synthetic evaluations** use fabricated results and a combat-entry guard.

The independent Python verifier reconstructs every bounded constructor pass, priority/exposure hash, pool and core charge, then verifies teams, role coverage, recipe/structural identities, cap counters, subset coverage and previous-policy outputs. No constructor or combat engine is invoked by that audit.

| Captured case | Seconds | Allocated bytes | Synthetic evaluations |
| --- | ---: | ---: | ---: |
| legacy | 0.0674 | 37,465,008 | 16 |
| diverse | 0.0852 | 42,371,920 | 16 |
| team | 0.0823 | 49,777,968 | 16 |
| filler | 0.3592 | 241,779,768 | 16 |
| reordered | 0.3805 | 241,774,288 | 16 |

The complete native diagnostic, including the seven old-policy fixtures and one additional new-pool construction, used **1.5618 seconds**, **1.5938 CPU seconds**, **878,615,024 allocated bytes** and **93,687,808 peak working-set bytes**. These single-run construction timings do not establish a filesystem or whole-campaign performance improvement.

## Next boundary

Use the measured pool/subset changes to decide whether to prepare a combat comparison against team coverage v1. Any comparison needs a concrete frozen package and a budget that covers live-history checks, allocation, execution and both verification passes. Carry the remaining time/output forward; the previous 45-value approval is exhausted. No fresh values, fights or time extension are authorized by this implementation.

## Resource accounting and reproduction

| Diagnostic phase before publication | Seconds |
| --- | ---: |
| audit | 0.469 |
| build | 3.610 |
| captured | 1.750 |
| freeze | 5.172 |
| test-build | 1.891 |
| tests | 1.860 |

Incoming cumulative workload: **2300.927 seconds**. This scope charges compilation, tests, diagnostics and publication within **80 seconds / 192 MiB**, under the unchanged cumulative **2,400-second / 4 GiB** limits. The [completion receipt](../TestResults/balance/tower-filler-diversity-20260915/completion.json) records actual remaining time and output, including one closure second and the shared-test allowance. Failure receipts: **none**. After a failure, absent success receipts identify unrun dependent phases; no retry is permitted.

Recorded commands run once, with exact project/filter arguments retained in the control logs:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-filler-diversity-20260915'
& $python -B "$work/setup.py"
# Apply the frozen source change before freezing diagnostics.
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The test command uses `build/run-tests.ps1 -NoBuild -ArtifactsPath` with exactly six frozen test-class filters. The [protocol](Tower-Filler-Diversity-Protocol.md), before/after source, captured gameplay assembly hashes, command logs, TRX, audit and [file seal](../TestResults/balance/tower-filler-diversity-20260915/files.json) are retained. Never rerun a sealed or failed directory.

Changed C#: the constructor's explicit traversal entry, new `TowerFillerDiversitySearch`, the structural adapter and five registration/provenance files; new `BalanceHarnessFillerDiversityTests`. Also updated the protocol/review, six active Markdown handoffs and isolated evidence scripts. No allocator or old team-policy changes. All **58 predecessor packages** and unrelated dirty work were preserved. No full gameplay/backend suite, gameplay/configuration changes, migrations or deployment.

Preserve all **482,731 reservations**, all 253 v19 recipes and unused 512 original confirmation values. Reliability Fail 1/3, deep recovery 0/3, v19 Unresolved and adoption Hold. No Kharad tuning, ability-order optimization or large confirmation.
