# Penetration price review — 27 September 2026

**Recommendation: use 4 budget per point for both Armor Penetration and Magic Penetration as the next version-18 candidate, retaining subtraction of percentage points and the 40-point cap.** This review does not change game prices: the checked-in candidate still costs 1.5, and live selection remains version 17.

Four is a design recommendation supported by these fixtures, not a mathematically unique optimum or rollout approval. Three is a defensible softer adjustment. Six and eight made magical penetration lose most of its useful comparisons in the screening matrix. Four reduces easy cap access and the damage-over-time build's advantage while retaining useful defense counters. Some physical ability combinations remain substantially stronger with penetration; a single stat price cannot make every specialization equally effective with every Essence loadout.

## What the price changes

All examples are Standard quality, rank 0, baseline roll and no style. Percentage-stat amounts do not increase merely from advancing item tier.

| Item's penetration allocation | Current price 1.5 | Price 3 | Recommended price 4 |
|---|---:|---:|---:|
| Common one-handed weapon: 30 normalized budget | 20 | 10 | 7.5 |
| Common two-handed weapon: 60 normalized budget | 40 | 20 | 15 |
| Epic one-handed weapon: 48 normalized budget | 32 | 16 | 12 |
| Epic two-handed weapon: 96 normalized budget | 40, with core overflow | 32 | 24 |

The 40-point cap remains reachable through stronger equipment or multiple allocations. For example, a baseline Epic penetration staff plus penetration ring supplies 36 points at price 4. An Epic Masterpiece two-handed weapon at rank 5 and a 1.05 roll can still reach 40 by itself. Per-item cap overflow returns to core stats; character over-cap stacking remains waste and is visible in comparisons.

The intended tradeoff is visible in simple damage arithmetic. At price 4, an Epic two-handed specialization buys 24 penetration or 48 Attack Speed. Against 50% mitigation, 24 penetration increases otherwise identical typed damage by 48%; against 15% mitigation, it increases damage by only 17.65%, since mitigation floors at zero. Forty-eight Attack Speed increases basic-attack rate by 48% only from a zero-speed baseline before timing and rate limits. Actual battles also include abilities, mixed damage, control, healing, critical hits and discrete timing; these calculations explain a tradeoff, not a full DPS prediction.

## Study design

The retained final protocol is `TestResults/penetration-price-review-20260927-v3/protocol.json`.

- **59 equal-budget comparisons**: basic maul, basic mace/shield, physical ability maul, damage-over-time staff, Conduit staff and wand/grimoire; light, medium and specialized heavy opponents; Common tier-1, Epic tier-2, fully reinforced/quality-enhanced and stacked-ring variants; fixed idle/Dungeon fights and a three-player support fixture.
- The defense matrix uses legal gear. Typical typed mitigation is about 15%, 25% and 50%. The shield and other loadout details affect individual values. Light/medium/heavy also change opponent health and offense, so they are encounter contexts, not an isolated causal experiment on defense alone.
- PvP weapon comparisons preserve the weapon archetype, ordered Essences, level, quality, rank, roll and nominal item budget. The opposing weapon uses Precision in the new defense matrix so changing penetration prices does not also change that opponent's weapon allocation. Existing support fixtures retain their declared full-party composition; price changes there can affect other penetration-equipped actors too.
- Screening tested **1.5, 3, 4, 6 and 8** using 12 exploration seeds plus one separate harness check seed: **14,560 fights**. Only exploration results were used to select prices 3 and 4.
- Held-out confirmation compared **1.5, 3 and 4**, using 8 new exploration seeds and **64 new confirmation seeds**, fixed in the original protocol: **48,384 fights**. Selection was recorded before these runs. Both starting sides are played for PvP; their shared seed is one statistical cluster.
- Total study execution: **62,944 fights**. An additional **64 replay executions** provided verification, not independent statistical evidence.

Both penetration prices vary together. The experiment therefore supports a shared candidate price; it does not validate an untested combination of different physical and magical prices. No other stat price or combat formula was changed.

## Held-out results

Values below are the penetration build's win-rate advantage over the named alternative, in percentage points. Positive favors penetration. Each PvP row contains 128 battles per build clustered into 64 independent seed pairs. These are selected explanatory rows; the complete 59-cell W/L/D and duration results are in `confirm-summary.json`.

| Context and alternative | Price 1.5 | Price 3 | Price 4 |
|---|---:|---:|---:|
| DoT staff, medium defense, Haste | +30.47 | +12.50 | +7.03 |
| DoT staff, medium defense, Precision | +28.91 | +10.94 | +5.47 |
| DoT staff, heavy defense, Haste | +11.72 | +11.72 | +11.72 |
| Wand/Conduit, medium defense, Precision | +15.62 | -4.69 | -15.62 |
| Physical ability maul, light defense, Haste | -2.34 | -9.38 | -9.38 |
| Physical ability maul, heavy defense, Haste | +76.56 | +76.56 | +75.00 |
| Basic maul, heavy defense, Speed | +0.78 | +0.78 | -1.56 |
| Staff/Conduit, heavy defense, Haste | +0.78 | -14.06 | -32.81 |

At price 4, the DoT staff's advantage over Haste against medium defense has an unadjusted 95% interval of **-3.85 to +17.92 pp**; its Precision comparison is **-0.35 to +11.29 pp**. Neither establishes a definite remaining advantage. Against heavy defense, the DoT/Haste interval is **+5.27 to +18.17 pp**, preserving a useful matchup. The physical ability maul's heavy-defense advantage remains large (**+66.55 to +83.45 pp**), while its light-defense Haste comparison favors Haste (**-16.90 to -1.85 pp**). Intervals are per comparison and unadjusted for the many comparisons; they do not certify population balance.

The Conduit examples are a material tradeoff: price 4 makes Haste or Precision preferable in several cases, even against heavy defense. These are mixed ability loadouts, so this is not evidence that penetration should counter every heavily defended enemy regardless of the build's damage sources. Price 3 preserves more of the wand's advantage; choosing 4 puts more weight on specialization differences and reducing cheap penetration access.

Several cases cannot decide balance from wins:

- Mace/shield cases at prices 3 and 4 all time out. That is a sustain/damage threshold, not proof of equal value.
- Some basic maul comparisons lose with both allocations. Their zero win difference does not certify either build.
- All six fixed PvE comparisons clear on all 64 confirmation seeds. At price 4, the early physical penetration build still clears in a mean **21.84 seconds**, versus **22.77** for Precision; the Dungeon build clears in **44.73 seconds**, versus **46.61** for Precision. These full-health fixtures show usefulness but do not establish multi-wave or resource-attrition performance.
- Higher prices sometimes help an alternative by reducing wasted stacked penetration, or change Power returned by the item cap. Price responses need not be monotonic in every cell. Raw allocations are retained; these are complete equipment-price changes, not edits to just the displayed penetration number.

## Verification and reproducibility

`penetration-price-review.py` prepares the fixed matrix, freezes the selection, summarizes mirrored outcomes and verifies artifacts. `run-penetration-price-review.ps1` builds isolated MSBuild source overlays and invokes the existing production-engine harness. The only compiled source difference is the current-version penetration price; the production source file is never edited. Version 17 prices are outside the replaced branch.

Verification passed:

- All eight completed study runs have identical content hashes and exact declared seed/mirror schedules; no duplicate or missing trials.
- **536 penetration item allocations** match the selected price, 40-point cap and resulting Power overflow, within the game's rounding precision. Every saved item allocation retains version 18 and reconciles its budget components.
- The production price source SHA-256 is unchanged from preparation. Each run retains execution assembly hashes, frozen builds, raw trials, estimates and detailed first-seed replays. `verification.json` binds every trials file hash.
- **12 detailed replay files** from three prices, two cells and two phases matched byte for byte under the original execution environment (48 fights). An initial 16-fight replay from a different execution environment had 127 localized percent-spacing differences in log `details`; every structured value was identical. That attempt is retained separately and is included in the 64 verification executions.
- All five isolated variants compiled. An initial source-overlay selection error was corrected before running any study. Two invalid fixture names were rejected during preflight with zero fights; the rejected inputs and build logs remain separate from the final `v3` outputs.
- Final whitespace validation passed. No requested verification remains blocked. Backend/Angular correctness suites were not repeated because no application source changed; this review's checks exercise the existing actual combat engine and equipment allocator directly. Backend tests, if subsequently needed for a price implementation, must continue to use `build/run-tests.ps1`.

Reproduction commands from the repository root, using an available Python 3 executable and a new output directory:

```powershell
python docs/analysis/penetration-price-review.py prepare TestResults/new-penetration-review
./docs/analysis/run-penetration-price-review.ps1 -OutputDirectory TestResults/new-penetration-review
python docs/analysis/penetration-price-review.py summarize TestResults/new-penetration-review
# Freeze selection only after inspecting screening exploration:
python docs/analysis/penetration-price-review.py confirm TestResults/new-penetration-review --prices 1.5 3 4
./docs/analysis/run-penetration-price-review.ps1 -OutputDirectory TestResults/new-penetration-review -Prices @(1.5,3,4) -Phase confirm
python docs/analysis/penetration-price-review.py summarize TestResults/new-penetration-review --phase confirm
python docs/analysis/penetration-price-review.py verify TestResults/new-penetration-review
```

This session used the bundled Python runtime because `python` was not on PATH. Exact archived re-execution should use the captured assemblies and original execution environment; a later rebuild can differ. The price-4 screening executable is retained under the initial `TestResults/penetration-price-review-20260927/build-price-4`; the other screening and all confirmation executables are under the final `v3` directory. Raw study artifacts are ignored by Git.

## Scope and next implementation

Only this report, its two reproduction scripts, and links from the existing redesign records are added by the review. No game pricing, database, migration, persistent configuration or deployment changes were made. The proposed implementation would change the version-18 penetration price from 1.5 to 4, update price-dependent expectations/documentation, and rerun affected equipment, combat and migration tests through the repository wrapper. Existing unversioned/retired-item migration blockers and broader rollout acceptance remain separate.
