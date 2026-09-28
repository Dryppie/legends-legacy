# Floor-11 linked Health/Power calibration — 28 September 2026

**No tested setting meets both the intended-budget win-rate band and the lower-budget separation screen.** The completed 21,504-fight grid retains every floor-11 team and gear variant from the [new equipment baseline](Tower-Equipment-Cycle-Baseline-20260928.md). At +50% Health and Power, the strongest seven-Essence team wins 11/32, but a six-Essence team wins 16/32. Doubling or tripling both stats produces zero wins for every tested team. No boss setting is selected or applied.

**Next recommendation: create controlled seven-Essence upgrades of the strong six-Essence teams at the intended budget, using the existing search and gear tools.** The current twelve seven-Essence finalists do not include either six-Essence party with one extra Essence at every position. This screen compares different compositions and levels, not an isolated seventh-slot effect. Establish those missing comparisons before deciding whether Serevin's mechanics need changing. Keep the requested repeating equipment curve and the supported search algorithm.

## Frozen design

The fixed grid multiplies **only floor 11's guardian Health and offense (Power)** by the same factor. Defense, Resistance, Penetration, Regeneration, abilities, cooldowns, stagger behavior, other floors and all player recipes stay fixed. Each variant has its own captured content copy. Baseline Health is **2.90**, offense **3.72**.

All **sixteen** retained floor-11 parties participate at **seven** gear choices each: all twelve seven-Essence finalists, both six-Essence controls and both four-Essence controls. Every cell uses the same **32 historical seeds**, preserving party positions, ordered Essences, identities and gear. No profiles or teams are removed after observing results.

| Cohort | Characters | Level / tier | Rarity / quality / rank | Gear combinations |
| --- | ---: | --- | --- | ---: |
| Seven Essences, intended | 10 | 60 / 2 | Rare / Standard / 2 | 84 |
| Six Essences, diagnostic | 10 | 50 / 2 | Rare / Standard / 2 | 14 |
| Four Essences, diagnostic | 10 | 30 / 1 | Rare / Standard / 2 | 14 |

The six factors were declared before combat: **1, 1.125, 1.25, 1.5, 2, 3**. Total: **6 × 112 × 32 = 21,504 fights**, including 3,584 baseline replays. All 672 team/setting combinations were prepared before combat. Every baseline input and full report had to match the prior screen before any stronger setting could run.

The selection rule chooses the lowest tested factor whose **strongest seven-Essence result is 4–16/32**, with **every four-/six-Essence result below 4/32**. The first condition screens the existing inclusive 10–50% intended-budget policy. The second is an explicit exploratory separation criterion; it is not a new production restriction or an approved statistical acceptance policy. Each complete party is assessed separately. All six settings run even if an earlier setting appears promising; there is no interpolation, adaptive extension, retry or automatic confirmation.

## Results

Entries are the **maximum observed wins out of 32** across every retained composition and gear choice in that cohort, not pooled win rates.

| Linked factor | Health | Power | Seven-Essence maximum | Six-Essence maximum | Four-Essence maximum | Intended cells above 50% |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1.000 | 2.9000 | 3.720 | 32 | 32 | 32 | 82 |
| 1.125 | 3.2625 | 4.185 | 32 | 32 | 20 | 53 |
| 1.250 | 3.6250 | 4.650 | 32 | 32 | 2 | 21 |
| 1.500 | 4.3500 | 5.580 | 11 | 16 | 0 | 0 |
| 2.000 | 5.8000 | 7.440 | 0 | 0 | 0 | 0 |
| 3.000 | 8.7000 | 11.160 | 0 | 0 | 0 | 0 |

The +25% setting suppresses the tested four-Essence controls but still leaves six-/seven-Essence teams at 32/32. The +50% setting is the only grid point with observed intended-budget performance inside the 10–50% band. It does not satisfy the progression screen because the strongest six-Essence control wins half its fights. At 2× and 3× there is no demonstrated viable intended-budget team.

At +50%, the exact observed leaders are:

| Cohort / case | Gear | Wins | Mean guardian health remaining | Mean duration, all fights |
| --- | --- | ---: | ---: | ---: |
| Seven / `floor11-seven-05` | Resistance-and-health | 11/32 | 11.62% | 113.09 s |
| Six / `floor11-six-1` | Resistance-and-health | 16/32 | 5.12% | 104.30 s |

Both were 32/32 at baseline. At +50% the seven-Essence leader loses 21 previously winning seeds and gains none; the six-Essence leader loses 16 and gains none. These are per-team paired changes against baseline, not a significance test between different teams. No fight in the grid ended in a draw. Full per-cell counts, means and paired gains/losses are retained in `result.json`; all underlying reports remain available.

## Interpretation and next comparison

Result: **`NoSeparatingSettingInGrid`**. This closes the declared grid without selecting a candidate. It does not prove that every intermediate multiplier fails, that Health/Power tuning can never separate cohorts, or that more Essences make a party weaker. Levels and compositions differ between cohorts; unsearched teams remain possible. The historical-seed screen does not establish fresh confirmation or a balance pass.

A read-only comparison authenticated the baseline recipes and checked each six-Essence member's full Essence set against every seven-Essence member at the same party position. **No seven-Essence finalist is a complete extension of either strong six-Essence party.** For the repeated six-Essence control, even the best seven-Essence match preserves all six definitions in zero of ten positions. For the alternating control, the best match preserves them in one of ten positions. This identifies a missing controlled comparison, not a causal combat finding. [Comparison receipt](../TestResults/tower-floor11-linked-owner-20260928/team-extension-review.json) records all 24 comparisons; it ran zero fights and allocated zero seeds.

The next useful family should retain those exact six-Essence teams and gear, add legal seventh-Essence choices at the intended level-60 budget, and include a level-60 six-Essence control to distinguish level gains from the additional slot. Preserve every current strong control. Use the supported search within that declared upgrade scope; a new search algorithm is not justified by this calibration. The +50% content copy is a useful diagnostic setting with observable wins and losses, but is **not** an approved boss setting.

If those controlled upgrades still fail to improve separation, inspect the captured fights around Serevin's existing buff dispel, Ink accumulation, Silence and magical damage mechanics before changing them. The [current ability definitions](../LL/src/API/API.LL/Data/combat/abilities.json) include those mechanics; this report makes no claim that one caused the observed cohort ordering. Do not enforce an artificial minimum Essence count to conceal lower-budget clears.

Any future chosen setting requires its own frozen family, unused confirmation seeds and the [existing uncertainty/coverage policy](Tower-Balance-Acceptance-Policy.md#evidence-and-acceptance). Carry-over equipment from floor 10 remains a separate progression consideration; this screen uses the requested floor-11 reference gear and does not downgrade player-owned items. No further fights are queued by this report.

## Verification and reproducibility

The primary game **offline Balance Harness** is the target. Added [native calibration fixture](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor11CalibrationTests.cs), [bounded owner](analysis/calibrate-floor11-linked.py), [independent auditor](analysis/verify-floor11-linked.py), and this record. Updated the baseline follow-up and [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md). Previous screens and production data remain unchanged.

**62 focused checks passed**, with the combat fixture initially skipped until configured. The build completed with 45 existing warnings and zero errors, using approved access to the local NuGet configuration. The separately owned calibration then passed. Tests cover the inclusive intended-band boundaries, both lower-budget controls, strongest-team selection, incomplete evidence, and isolation of floor-11 Health/Power edits. Python syntax/CLI and whitespace checks passed; no required command remains blocked.

The native run reconstructed every trial's scenario, input hash and cache identity. It reproduced **all 3,584 baseline inputs and full reports** before stronger variants. Execution took **534.98 seconds**, with **538.31 seconds** for the owner; all **eight processes drained**. The sealed archive contains **543,643,078 bytes**, below the 2 GiB cap. Limits were 840 seconds internally and 900 seconds for the owner. Zero retries, fresh seeds, new search runs or newly confirmed teams. The latest scientific exclusion union remains **834,295**.

The [independent audit](../TestResults/tower-floor11-linked-owner-20260928/independent-audit.json) passed on its first run. It authenticated **22,601 archived files**, checked every captured content variant changes only floor-11 Health/Power, retained all recipes and schedules, reproduced all 3,584 baseline full-report comparisons, and recomputed every result, paired gain/loss and selection decision from raw reports. No additional fights were needed.

Source baseline manifest: `79fd4c8f4db16d4b98dd3b78dd0a53224499a0a85ed6ba92cc0cceeacffc5abe`.
Local study: [result.json](../TestResults/tower-floor11-linked-20260928/result.json).
Study manifest: `feaae1ff33bfccd145c1cd733a91b8dc313f4b965f6d0b4394dffab3a4697c03`.
Result SHA-256: `6ea4fcd8a38526967add65d9ba6dfd9f3cd7155fd23ff1db8be967905d4a9ce2`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor11-linked-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessFloor11CalibrationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
python -B -X utf8 'Balance Harness/analysis/calibrate-floor11-linked.py' --package TestResults/tower-floor11-linked-owner-20260928 --output TestResults/tower-floor11-linked-20260928 --artifacts TestResults/tower-floor11-linked-build-20260928
python -B -X utf8 'Balance Harness/analysis/verify-floor11-linked.py' --study TestResults/tower-floor11-linked-20260928 --owner TestResults/tower-floor11-linked-owner-20260928 --manifest-pin feaae1ff33bfccd145c1cd733a91b8dc313f4b965f6d0b4394dffab3a4697c03 --receipt TestResults/tower-floor11-linked-owner-20260928/independent-audit.json
git diff --check
```

Python refers to the bundled runtime. Completed package/output directories cannot be reused; the owner never resumes or retries combat. Later read-only audits require a new receipt path. No production configuration changes, migrations, database operations or deployments.
