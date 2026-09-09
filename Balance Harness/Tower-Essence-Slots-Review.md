# Tower parties with four through ten Essences — 9 September 2026

`tower-essence-slots-v1` provides all seven requested counts: **4, 5, 6, 7, 8, 9 and 10 equipped Essences per character**. Select it in the dashboard's Profile catalog; every count and every released floor is selected by default. This is a separate catalog, preserving the reference, progression and factor presets and their evidence.

## Scope and budgets

All profiles have level 90, seven Standard tier-2 rank-3 items, baseline rolls and no styles. Level 90 legally unlocks ten Essence slots. Smaller loadouts leave the remaining slots empty; these are not characters restricted to an earlier unlock milestone. Each count adds the next entry from the existing pinned role lists, keeping all other recipe fields fixed apart from identifying names. Essences remain level 1, unascended and unevolved.

The same Guardian, Restorer, two Strikers and Controller cell repeats to fill each floor's RequiredSlots. Character party size remains 5, 10 or 15, separate from each character's Essence count. Ownership, materials, access and Essence acquisition remain explicit assumptions. An increment adds an Essence to every character, so it tests the resulting group loadout, not a single ability's isolated contribution.

The declared schedule is master seed 1337, all 15 floors, seven parties and 20 trials each: **105 combinations / 2,100 battles**. Every count on a floor shares seeds. Each fight starts fresh through production Tower preparation, combat and outcome interpretation. The fixed detailed replay selection is trial `tower.0001` on floor 11 for each of the seven parties.

## Verification

`./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower'` initially passed 57 tests and then **59 tests** after the independent five/six-Essence parity checks, with zero failures/skips and five existing compiler/analyzer warnings. The new tests verify the complete 4–10 range, adjacent loadouts extending the same lists, unchanged other build factors, legal preparation across all 105 combinations and paired seeds. A two-run 105-trial smoke compared all 105 pairs with zero changed gameplay records and replayed every count. HTTP discovery checks the seven dashboard choices. Existing Tower production parity, integrity and cancellation tests also passed. The full backend suite was not rerun for this catalog/test-only change; no verification command remains blocked.

The browser confirmed all seven selections, their fixed level/rank budgets and the 2,100-battle default before starting the full measurement. The smoke comparison is repeatability verification at one trial per combination; it is not a second full-size measurement or independent statistical confirmation.

## Full measurement and evidence

The full browser-driven run completed **2,100/2,100 trials**, with no invalid or missing trials. All seven first-trial floor-11 replays matched. There was one production Draw, in the eight-Essence party on floor 11.

| Equipped Essences | Floor-11 wins / defeats / draws | Clear rate, 95% Wilson interval |
| --- | --- | --- |
| 4 | 11 / 9 / 0 | 55% [34.21–74.18] |
| 5 | 20 / 0 / 0 | 100% [83.89–100] |
| 6 | 4 / 16 / 0 | 20% [8.07–41.60] |
| 7 | 7 / 13 / 0 | 35% [18.12–56.71] |
| 8 | 10 / 9 / 1 | 50% [29.93–70.07] |
| 9 | 18 / 2 / 0 | 90% [69.90–97.21] |
| 10 | 20 / 0 / 0 | 100% [83.89–100] |

Every party won 20/20 on floors 1–6 and 8–9. On floors 7 and 10, four/five Essences lost 20/20 and counts six through ten won 20/20. Every count lost 20/20 on floors 12–15. These are independent floor starts, so clearing floor 11 here does not imply a four/five-Essence party could complete the preceding climb.

The five-to-six drop on floor 11 shows why more equipped Essences must not be treated as a guaranteed increase in performance. Each added Essence changes a character's abilities and the group interaction. Independent normal-path parity checks were added for both five and six Essences after observing this result; no combat rule was changed to make the results monotonic. The measurements do not identify a single causal ability, rank compositions globally or approve a desired win-rate band. Intervals are descriptive per-cell estimates, not simultaneous confidence claims across all tests.

Saved evidence:

- Full run: `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-105939-2280b48738a04379a2abce315f9277eb/run/`.
- Retained executable, test logs/TRX, seven replay JSON/log pairs, all-floor `slot-summary.md`/`.json`, verification and checksums: `TestResults/balance/tower-essence-slots-20260909/`.

The full bundle contains frozen inputs/content/execution identity, individual outcomes and Markdown/JSON reports. It was not rerun at 2,100-trial size; the automated 105-pair smoke independently verifies repeat/comparison behavior. Prior catalogs and measurements remain unchanged.

```powershell
dotnet TestResults/balance/tower-essence-slots-20260909/executable/BalanceHarness.dll tower-dashboard --port 5093 --content-root LL/src/API/API.LL --runs-root TestResults/balance/tower-dashboard-20260909/browser-runs
```

## Usage and remaining limits

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --catalog LL/tools/BalanceHarness/Fixtures/tower-essence-slots.json --output TestResults/balance/tower-essence-slots-new
```

Select a matching saved slot-range run as reference after balancing content changes. New profiles resolve current equipment and Essence definitions automatically; exact historical replay requires the matching retained executable/runtime. `--samples 1` runs 105 trials. The existing 10,000-trial cap permits up to 95 samples per combination when every count/floor is selected.

This supplies every requested equipped-slot count without adding character-level progression, acquisition, ascension, styles, alternative compositions, a continuous climb or automatic tuning. Rates remain descriptive and the starter target band is not applied. Phase 2 integration stays deferred.

## Changed files

`Fixtures/tower-essence-slots.json` adds 28 role/count profiles and seven parties. `BalanceHarnessTowerBenchmarkTests.cs` adds the range/materialization and repeat/comparison/replay checks; `BalanceHarnessTowerDashboardTests.cs` verifies catalog discovery; `BalanceHarnessTowerTests.cs` adds independent normal-path parity for five/six Essences on floor 11. The README and plan document the scope and results. No combat runner, production content, envelope schema, runtime configuration, migration or deployment change is required. Earlier catalogs/evidence and unrelated working-tree changes are preserved.
