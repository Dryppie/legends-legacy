# Tower factor investigation — 9 September 2026

This bounded investigation uses the separate `tower-factors-v1` catalog to examine the progression run's observed transitions at floors 11 and 12. It runs all 15 released floors, preserves the original reference/progression catalogs and uses the production Tower pipeline unchanged. No balance tuning or target approval is part of this increment.

## Fixed protocol

The schedule is master seed 1337, 20 trials per floor/party, eight parties and 15 floors: **2,400 trials per run**. Repeat the exact schedule once to verify saved comparison/replay behavior; a repeat is not another independent statistical sample. Every party on a floor shares seeds. All fights start fresh, with the actual RequiredSlots and production PartyNumber assignment.

| Preset | Level | Tier | Rank | Essences | Parent / only declared change |
| --- | --- | --- | --- | --- | --- |
| control | 50 | 2 | 3 | 6 | Mid-budget control |
| level-70 | 70 | 2 | 3 | 6 | control / character level |
| level-90 | 90 | 2 | 3 | 6 | control / character level |
| rank-4 | 50 | 2 | 4 | 6 | control / equipment rank |
| rank-5 | 50 | 2 | 5 | 6 | control / equipment rank |
| essences-8 | 90 | 2 | 3 | 8 | level-90 / added Essence loadout |
| essences-10 | 90 | 2 | 3 | 10 | level-90 / added Essence loadout |
| late | 90 | 2 | 5 | 10 | essences-10 / equipment rank |

Each character owns seven Standard items filling eight slots, baseline rolls, no styles. Equipment definitions and Guardian/Restorer/two Strikers/Controller composition stay fixed. Essence lists extend the pinned progression lists, with level-1 unascended/unevolved Essences. Additional Essences are tested at level 90 so every loadout respects unlocked slots. Assumed ownership/materials/access are not acquisition claims. Fixture/participant identities vary between presets as in the existing benchmark generator; the test compares build recipes after excluding their identifying name.

The fixed replay selection is `tower.0001` on floor 11 for all eight parties, plus floor 12's late party. Verify each saved cell bundle with the retained executable. Review the full Markdown/JSON scorecard and parent-relative outcomes; do not choose a balance change automatically. Small samples, correlated seeds and multiple exploratory comparisons do not establish a population difficulty target or an individual Essence's contribution.

## Measured results

Both runs completed all 2,400 trials. Each preset won 20/20 on every floor from 1–10 and lost 20/20 on every floor from 12–15. Floor 11 separated the variants:

| Preset | Floor-11 wins / defeats / draws | Clear rate, 95% Wilson interval | Floor-12 mean guardian health remaining |
| --- | --- | --- | --- |
| control | 0 / 20 / 0 | 0% [0–16.11] | 63.31% |
| level-70 | 0 / 20 / 0 | 0% [0–16.11] | 58.89% |
| level-90 | 4 / 16 / 0 | 20% [8.07–41.60] | 50.34% |
| rank-4 | 0 / 20 / 0 | 0% [0–16.11] | 62.23% |
| rank-5 | 0 / 20 / 0 | 0% [0–16.11] | 60.54% |
| essences-8 | 10 / 9 / 1 | 50% [29.93–70.07] | 32.99% |
| essences-10 | 20 / 0 / 0 | 100% [83.89–100] | 17.87% |
| late | 20 / 0 / 0 | 100% [83.89–100] | 13.70% |

On this schedule, rank increases alone at level 50 did not cross floor 11, although mean remaining guardian health fell from 45.20% at rank 3 to 38.70% at rank 5. Raising level while holding six Essences and rank 3 fixed began producing wins at level 90. Extending that level-90 loadout to eight and ten Essences produced more wins; rank 5 was not necessary for the measured ten-Essence party to clear floor 11. This points to level and loadout coverage as useful follow-up axes for these particular builds, not proof of any individual Essence's effect or statistical superiority across player populations.

None of the interventions crossed floor 12 in 20 trials, but adding Essences at level 90 reduced mean guardian health much more than rank increases at the level-50 control. Even the strongest declared party left 13.70% health on average. Testing composition or specific Essence substitutions at a fixed budget would be a separate experiment; this run does not justify automatic boss tuning.

The repeat compared **2,400 pairs with zero changed gameplay/evidence records**. All nine fixed replays matched. An additional exploratory replay reproduced floor-11 `essences-8` trial `tower.0012`, the one production Draw, at 180.1 seconds. It was selected after observing the draw and is distinguished from the fixed replay protocol. No invalid or missing trials occurred. The repeat does not double the independent sample size. Wilson intervals are per-cell descriptions, not simultaneous confidence statements for all comparisons; parent-relative point estimates carry no acceptance threshold.

## Saved evidence and use

The local dashboard at `http://127.0.0.1:5093/` exposes `tower-factors-v1` alongside the unchanged reference and progression catalogs. New runs use current equipment/Essence content. Selecting a factor run as reference restores its matching settings; unlike party IDs are not interchangeable across catalogs.

Browser verification selected all eight presets and 15 floors, started both runs, restored the reference settings and displayed all 120 verified comparison rows with no browser console errors.

- Reference: `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-104208-3ae9f3d722a74accadf35aa7a5e0f330/run/`.
- Repeat/comparison: `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-104436-044498743c2146b99095211a88bda915/run/`.
- Retained build, test log/TRX, ten replay JSON/log pairs, `factor-summary.md`, `factor-summary.json`, verification and hashes: `TestResults/balance/tower-factors-20260909/`.

The factor summary covers all floors and 105 child/parent comparisons, with descriptive clear-rate, duration and guardian-health changes. Standard benchmark Markdown/JSON reports retain all intervals, outcomes and saved trials; verified run-to-run comparison remains the existing benchmark comparator. No new envelope or interpretation of production outcomes is introduced.

```powershell
dotnet TestResults/balance/tower-factors-20260909/executable/BalanceHarness.dll tower-dashboard --port 5093 --content-root LL/src/API/API.LL --runs-root TestResults/balance/tower-dashboard-20260909/browser-runs
```

## Verification scope

`./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower'` passed **55 tests**, zero failures/skips, with five existing compiler/analyzer warnings. The new test checks every child/parent recipe differs only along its declared axis, Essence lists extend their parent, all 120 combinations materialize legally and floor seeds remain paired. HTTP catalog discovery exposes all eight presets. Existing production parity, comparison integrity, invalid-input handling, cancellation and replay tests also passed. The full backend suite was not rerun for this catalog/test-only change; no verification command remains blocked.

## Remaining limits

These are prescribed interventions, not an optimizer or comprehensive search. Changing an Essence loadout adds multiple abilities and tests their combined effect; it does not isolate individual Essences. Level, rank and Essence effects may interact, so a result at one parent budget cannot be extrapolated to another. Quality, styles, ascension, alternative composition, scouting, acquisition and a continuous Tower journey remain outside scope. The earlier starter 50–90% target is not applied. Phase 2 integration remains deferred.

## Changed files

`Fixtures/tower-factors.json` adds 32 explicit profiles and eight parties. `BalanceHarnessTowerBenchmarkTests.cs` verifies the one-factor contract and legal preparation; `BalanceHarnessTowerDashboardTests.cs` checks discovery. The README and plan describe the catalog and measurements. No runner/combat implementation, production content, schema, runtime configuration, migration or deployment change is required. Previous evidence and unrelated working-tree changes are preserved.
