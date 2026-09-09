# All-floor Tower progression and compositions — 9 September 2026

`tower-curve-v1` measures all 15 released floors with three party compositions and explicit floor-specific progression budgets. The previous Uncommon entry verification block is resolved: all 70 existing Tower tests passed before this implementation, followed by **79 passing Tower tests** with the new coverage. Earlier failed attempts and evidence remain preserved.

## Budgets and compositions

Only the user's floor-1 budget (four Essences, Uncommon Standard/Fine, rank 1–2) and floor-10 six-slot target are authoritative direction. This catalog picks the Standard/rank-1 entry corner and uses **provisional** interpolation and later gear budgets:

| Floors | Essences per character | Level | Uncommon equipment |
| --- | --- | --- | --- |
| 1–4 | 4 | 30 | Standard, tier 1, rank 1 |
| 5–7 | 5 | 40 | Standard, tier 1, rank 2 |
| 8–10 | 6 | 50 | Standard, tier 2, rank 2 |
| 11 | 7 | 60 | Fine, tier 2, rank 3 |
| 12 | 8 | 70 | Fine, tier 2, rank 3 |
| 13 | 9 | 80 | Fine, tier 2, rank 4 |
| 14–15 | 10 | 90 | Fine, tier 2, rank 4 |

All characters have seven equipped items, baseline rolls and level-1 unascended, unevolved Essences. Role lists extend the existing pinned Essence lists. No styles, scouting or contributions are included. Levels use each Essence slot's first unlock. Acquisition timing is not modeled.

- **Balanced:** Guardian, Restorer, two Strikers, Controller.
- **Support:** Guardian, two Restorers, Striker, Controller.
- **Pressure:** Guardian, Restorer, three Strikers.

These five-character cells repeat to each floor's actual RequiredSlots. Each trial starts fresh; this is not a continuous climb with health carryover. Same-floor compositions share seeds. All budgets and both schedules were recorded before measurement, with no candidate selection afterward.

## Discovery: every released floor

Master seed 1337, 20 trials per floor/composition: **900/900 valid battles**. Values below are wins out of 20; losses and draws remain distinct in the saved reports.

| Floor | Balanced | Support | Pressure |
| --- | --- | --- | --- |
| 1 | 13 | 15 | 1 |
| 2 | 6 | 3 | 3 |
| 3 | 3 | 0 | 0 |
| 4 | 0 | 0 | 0 |
| 5 | 20 | 1 | 0 |
| 6 | 20 | 20 | 0 |
| 7 | 0 | 0 | 0 |
| 8 | 20 | 20 | 20 |
| 9 | 20 | 20 | 20 |
| 10 | 20 | 14 | 0 |
| 11 | 10 | 5 | 2 |
| 12 | 0 | 0 | 0 |
| 13 | 0 | 0 | 0 |
| 14 | 11 | 20 | 0 |
| 15 | 2 | 1 | 0 |

Floors 4, 7, 12 and 13 are investigation points under these provisional budgets. A zero observed clear rate is not an automatic boss-tuning failure. A change in composition can matter more than additional damage roles, and level/gear steps confound direct comparisons between floors.

## Reserved-seed confirmation

Master seed 20260910, 100 trials per composition on floors 1, 10 and 15: **900/900 valid battles**. The seed set does not overlap discovery; content, settings and executable remain fixed.

| Floor | Balanced wins / draws | Support wins / draws | Pressure wins / draws |
| --- | --- | --- | --- |
| 1 | 57 / 1 | 78 / 0 | 0 / 0 |
| 10 | 100 / 0 | 52 / 48 | 0 / 0 |
| 15 | 18 / 1 | 8 / 0 | 0 / 0 |

Each entry represents 100 trials; remaining trials are defeats. The balanced party's descriptive 95% Wilson clear intervals are 47.22–66.27% at floor 1, 96.30–100% at floor 10, and 11.70–26.67% at floor 15. Full intervals and pacing/survival metrics are in Markdown/JSON reports.

The floor-10 support party trades damage for survival. Its first draw (`tower.0002`) reaches the production 600-second tick limit; detailed replay matches. Draws are not counted as victories. The balanced six-slot party demonstrates beatability under this provisional Uncommon Standard tier-2 rank-2 budget; it does not establish an approved floor-10 gear policy or universal party viability.

## Implementation and verification

Catalog schema 2 adds an explicit `floorCellProfiles` map per party. Every selected released floor needs a legal five-profile cell; malformed maps, unreleased floors and unknown profiles are rejected before combat. Filtering floors in Tower Lab retains the map and uses the selected floors' budgets. The preview groups identical budgets by floor range and exposes the corresponding equipment/Essences in its disclosure.

Schema 1 remains supported. The optional map is omitted when null, preserving old serialized input hashes. A real historical 600-battle anchor archive was read and compared against itself with the new executable: **600 pairs, zero changed records**. This validates archive compatibility, not new independent gameplay evidence. Existing replay still requires each run's retained executable.

```powershell
./build/run-tests.ps1 -Configuration TowerCurveVerification -Filter 'FullyQualifiedName~BalanceHarnessTower'
```

**79 passed, zero failed/skipped**, with 33 existing build warnings. Coverage includes the formerly blocked Uncommon tests, all-floor legal budgets, three compositions, floor filtering, invalid maps, legacy hash compatibility, deterministic comparison/replay and independent persisted normal Tower preparation/playback/outcome at floors 1, 10 and 15. No verification command remains blocked; the full backend suite was not rerun for this harness-only change.

**14 detailed replays matched:** first confirmation trial for all nine floor/composition cells; balanced discovery trials on floors 5, 11, 12 and 13 to cover every slot count from 4 through 10; and the first floor-10 support draw. Browser checks verified all 15 floors selected, 45 combinations/900 trials, grouped budgets for all three parties, and verified saved results. No production combat/content, migrations, production configuration or deployment changes were made.

## Evidence, files and next work

Runs are under `TestResults/balance/tower-dashboard-20260909/browser-runs/tower-curve-20260909-{discovery,confirmation}/run/`. `TestResults/balance/tower-curve-20260909/` retains the executable, frozen source, protocol, initial 70-test and final 79-test logs/TRX, reports, 14 replays, draw diagnostics, compatibility check and checksums. All prior catalogs, accepted starter evidence and unrelated changes remain preserved.

Changed files: `TowerBenchmark.cs`, `Fixtures/tower-curve.json`, `Dashboard/dashboard.js`, three Tower test classes, README, plan and this review. The earlier Uncommon review receives a verification follow-up. Phase 2 integration stays deferred.

Next confirm floors 4 and 7 with their stated four-/five-slot budgets and compare bounded legal loadout alternatives before choosing tuning changes. Later floor gear and the intended floor-10 budget still need gameplay review. These limited role-count variations do not cover every Essence build, mixed quality, acquisition timing, or automatic optimization. No Tower numerical target or baseline is promoted; the starter 50–90% band is not applied.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-curve.json --output TestResults/balance/tower-curve-new
```
