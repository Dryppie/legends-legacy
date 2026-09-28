# Reusable gear profiles and encounter coverage

**Implemented:** the supported offline search can now use an explicit gear-specialization profile, consistently applied to its character templates and references. The existing Essence search, 528-fight schedule and independent confirmation requirement remain unchanged.

**Finding:** gear choice depends on the encounter. In the 32-seed diagnostic, armor-and-health won 32/32 on floor 15 but 12/32 on floor 13; resistance-and-health won 32/32 on floor 13. Every tested encounter/budget had at least one profile at 32/32. These are descriptive measurements on reused seeds, not new strength confirmations.

## Reusable implementation

The target remains the offline Balance Harness in the primary game service. `TowerGearProfiles` reads the existing six-profile fixture and applies a chosen profile to a detached copy. The selected production catalog resolves substitutions; every changed item retains its archetype, rarity, progression, quality and roll multiplier. The production evaluator enforces equal target budgets. Actual equipment is materialized for legality, and character, item and Essence instance identities are checked before and after.

Essence membership and order, character positions, scenario metadata and seeds are preserved. Existing identity pins are retained; otherwise original equipment is pinned before replacing it. Profiles apply only to their declared positions and slots. Empty positions mean the whole party. Named positions beyond a smaller party are ignored, but at least one position must match. Missing requested equipment, unknown specialization/slot values and styled gear fail. Applying the same profile again leaves the scenario unchanged. Applying a different profile is not a reset: use the original baseline for independent comparisons.

`TowerAffinitySearch.WithGearProfile` validates a current supported plan and its captured content/settings/executable, applies the profile to all templates and references, rebuilds mechanics and validates the result. It preserves composition starts, ownership limits, affinity policy, benchmark designation and seed panels. Changed references carry new provenance hashes and a statement that prior strength evidence does not transfer. Composition IDs remain composition IDs; the complete plan and combat-input identities bind the actual gear.

Commands, using the qualified harness DLL:

```text
dotnet <BalanceHarness.dll> tower-gear-profile-apply <scenario.json> <profile-catalog.json> <profile-id> <content-root> <new-scenario.json>
dotnet <BalanceHarness.dll> tower-affinity-search-gear <plan.json> <profile-catalog.json> <profile-id> <content-root> <new-plan.json>
dotnet <BalanceHarness.dll> tower-affinity-search-check <new-plan.json>
```

The catalog is `LL/tools/BalanceHarness/Fixtures/tower-gear-specialization-screen.json`. Both exports refuse existing files and run zero fights. Search-plan exports preserve the input schedule and require normal owner admission before execution. They do not allocate seeds, make historical seeds fresh, automatically select a profile, or claim a joint gear/Essence search. Comparing profiles has a separate cost from the supported search's 528 fights.

## Frozen coverage design

The six encounter/budget cases come from the existing progression screen. The preselected first reference is retained for floors 3, 7, 8, 10 and 13. Floor 15 uses the exact retained reference-2 baseline from the [confirmed gear study](Tower-Gear-Specialization-Evaluation-20260928.md). All six gear profiles were frozen unchanged; no profile or budget was added after seeing results.

| Floor | Party size | Essences per character | Character level | Gear tier/rank | Quality |
| --- | --- | --- | --- | --- | --- |
| 3 | 5 | 4 | 30 | 1/1 | Standard |
| 7 | 5 | 5 | 40 | 1/2 | Standard |
| 8 | 10 | 4 | 30 | 1/1 | Standard |
| 10 | 15 | 6 | 50 | 2/2 | Standard |
| 13 | 10 | 7 | 60 | 2/3 | Fine |
| 15 | 15 | 10 | 90 | 2/4 | Fine |

All use Uncommon equipment, baseline rolls, level-1 unascended/unevolved Essences, hypothetical ownership, no styles/contributions and captured attribute/equipment/healing selection **18 / 4 / healing-v1**. These inherited progression budgets remain provisional; they do not describe an approved player population. The restorer profile refers to fixed positions 2, 7 and 12 where present, not a newly inferred role assignment in each historical team.

The study prepared all **42 combinations before combat**, checked that the new reusable API exactly reproduces the archived floor-15 armor-and-health scenario, and used the same **32 historical seeds** for every cell. All 32 already belonged to the completed gear study's exclusion history. No new seed values were allocated; the latest exclusion union remains **833,546**.

The fixed cost was **6 encounters × 7 profiles × 32 seeds = 1,344 fights**. One attempt, no retries, no data-dependent additions. The predeclared follow-up rule checks only armor-and-health: 4 through 28 wins inclusive flags room for a later search comparison. This rule is a diagnostic screen, not statistical confirmation. All alternative results remain visible when interpreting whether a case is actually useful.

## Results

Wins out of 32; bold marks each row's largest observed count.

| Floor | Original | Precision | Haste | Restorer | Armor + health | Resistance + health | Health + regeneration |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 3 | 28 | 11 | 31 | 18 | **32** | **32** | **32** |
| 7 | **32** | **32** | **32** | **32** | **32** | **32** | **32** |
| 8 | **32** | **32** | **32** | **32** | **32** | **32** | **32** |
| 10 | **32** | **32** | **32** | **32** | **32** | **32** | **32** |
| 13 | 8 | 7 | 21 | 11 | 12 | **32** | 28 |
| 15 | 10 | 7 | 12 | 18 | **32** | 14 | 15 |

Floor 13's resistance profile gained 24 wins and lost none against original gear on this panel. Armor gained ten and lost six. On floor 15, armor gained 22 and lost none; resistance gained ten and lost six. These paired descriptive counts do not turn the historical panel into an independent holdout.

The formal eligibility output is `[13]`, because armor-and-health scored 12/32 there. However, using floor 13 with armor as the sole benchmark would ignore the stronger resistance candidate already observed. Every case reaches a sample ceiling under at least one tested profile. A 32/32 sample does not imply a guaranteed win or global optimality, and the screen does not compare search procedures across independent roots.

## Decision and next step

Keep the supported affinity search and its confirmation rule. Keep floor 15's independently confirmed armor-and-health benchmark in its original scope. Expose gear profiles as explicit choices; do not make one profile the universal default.

The next useful experiment is one fresh, bounded confirmation of the exact **floor-13 resistance-and-health** scenario against the original and armor-and-health controls, with the two comparisons and practical/statistical thresholds declared before allocation. The complete seed-free candidate can be recovered from this screen's `cells.json` by selecting floor 13 / resistance-and-health and clearing its historical `seeds` array. No such confirmation is launched in this increment.

If that advantage confirms, gear-aware references should be retained before comparing Essence search methods. A later algorithm evaluation needs declared encounter/progression conditions with room for improvement against the strongest available gear-aware controls. Do not artificially fix inferior armor on floor 13 merely to make an Essence algorithm look useful, and do not change live encounter balance in response to this small diagnostic.

## Verification and changed files

**35 focused backend checks passed**, with three opt-in combat fixtures skipped. They cover all six profiles across six party/budget cases, instance/order preservation, idempotence, invalid targets/slots/specializations/styles, source immutability, actual command exports and overwrite refusal, supported-plan preparation, all 528 requests in a search with fixed test outcomes, and exact reconstruction with the unchanged benchmark gate. The coverage fixture then passed its separate real run.

Initial compilation encountered a `Program` name collision with API test dependencies; qualifying `BalanceHarness.Program` resolved it. The final build has no errors and 16 existing warnings. No verification remains blocked.

Native reconstruction verified every trial recipe, input hash, cache identity and report binding. The separate Python auditor authenticated **1,490 files**, checked the historical seed membership and unchanged content/settings, verified every specialization-only recipe, reproduced the known floor-15 variant and reconstructed all **1,344 raw battle outcomes**, row statistics and follow-up classification. It ran zero fights and wrote outside the sealed archive.

Changed in this increment:

- Added `LL/tools/BalanceHarness/TowerGearProfiles.cs`: reusable catalog, guarded application and scenario export command.
- Updated `LL/tools/BalanceHarness/TowerAffinitySearch.cs` and `Program.cs`: gear application to supported search plans and command dispatch.
- Added `LL/tests/EssenceSystem.Tests/BalanceHarnessGearProfileTests.cs`: reusable API, full fixed-outcome search and command coverage.
- Changed only the `Project` helper's visibility in `BalanceHarnessAffinityFloorEvaluationTests.cs` to reuse the existing current-content plan fixture. Earlier changes in that file were preserved.
- Added `LL/tests/EssenceSystem.Tests/BalanceHarnessGearCoverageTests.cs`: opt-in frozen diagnostic, preparation and archive reconstruction.
- Added `Balance Harness/analysis/screen-tower-gear-coverage.py` and `verify-tower-gear-coverage.py`: bounded owner and independent read-only audit.
- Updated `LL/tools/BalanceHarness/AFFINITY-SEARCH.md` and added this report.

The old gear-study implementation and six-profile fixture remain unchanged. Existing gameplay and other working-tree edits were preserved. **No migrations, production configuration changes, deployments or shared database operations.**

Commands (Python denotes the bundled runtime):

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-gear-profiles-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessGearCoverageTests|FullyQualifiedName~BalanceHarnessAffinitySearchTests|FullyQualifiedName~BalanceHarnessGearScreenTests|FullyQualifiedName~BalanceHarnessTowerLoadoutFoundationTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests'
python -B -X utf8 'Balance Harness/analysis/screen-tower-gear-coverage.py' --help
python -B -X utf8 'Balance Harness/analysis/screen-tower-gear-coverage.py' --package TestResults/tower-gear-coverage-owner-20260928 --output TestResults/tower-gear-coverage-20260928 --artifacts TestResults/tower-gear-profiles-build-20260928
python -B -X utf8 'Balance Harness/analysis/verify-tower-gear-coverage.py' --study TestResults/tower-gear-coverage-20260928 --owner TestResults/tower-gear-coverage-owner-20260928 --manifest-pin f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746 --receipt TestResults/tower-gear-coverage-owner-20260928/independent-readback.json
git diff --check
```

## Evidence

The diagnostic took **45.18 seconds**; its owner took **47.31 seconds** and drained all eight processes. Limits were 840 fixture seconds, 900 owner seconds and 1 GiB. There were exactly 1,344 attempts and completions, zero retries, zero new seeds, zero search runs and zero newly confirmed teams. Engineering tests use fixed evaluator outcomes rather than scientific combats.

- Captured recipes/content/executable, reports and result: `TestResults/tower-gear-coverage-20260928/`.
- Frozen declaration, request, process receipt and independent audit: `TestResults/tower-gear-coverage-owner-20260928/`.
- Passing verification log: `TestResults/tower-gear-profiles-verification-complete-20260928.log`.
- Original six-case diagnostic freeze: `TestResults/affinity-progression-screen-20260925/freeze.json`, SHA-256 `71ea7fadf448b6eee3bddbc663b8cde9c502ba1a058a6c63a66c5c97dffe49d9`.
- Source gear-study manifest: `20b196238a4a51576d0e07202002aebe6882aa68a9260bbc80f576b176e7aa8a`.
- Coverage result SHA-256: `50f4eb33030ef667ec7d58ec0e760f15cdaee3d1c4a4ebe7c2991786ce48e75e`.
- Coverage archive manifest SHA-256: `f715905d4bea3d3c9df43418ac2d264298936a4c1c8416c0d1eacce457569746`.

The diagnostic launcher depends on retained local historical evidence. The reusable gear API, commands and ordinary tests do not require those ignored archives. Preserve the archives and pins for exact scientific readback.
