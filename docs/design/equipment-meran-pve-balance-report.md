# Meran PvE and Forge pacing assessment

3 September 2026. Target: the main LL game. This completes the first actual-encounter assessment after the [Tier 2 implementation](equipment-region-two-progression.md).

Prepared Tier 1 gear can enter Meran's ordinary combat loop. Tangled Cave III had a pronounced difficulty spike even at level 65 with improved Tier 2 equipment and ascended Essences. Its authored enemy multiplier is now **1.1, previously 1.3**. Shared dungeon scaling, other difficulties, Great Tree and Forge prices are unchanged. Values remain provisional pending full-run and player acceptance.

## Method and reproducibility

[MeranProgressionAnalyzer](../../LL/tools/LegendsLegacy.Balance/MeranProgressionAnalyzer.cs) uses the production equipment evaluator, creature scaling, authored creature abilities, Essence preparation and combat executor. Fights use the live **6,000-tick limit and initial active-ability cooldowns**. Default threat settings match the checked-in API settings. The tool runs detached from game accounts and does not grant rewards or use a database.

The [six fixtures](../../LL/tools/LegendsLegacy.Balance/Fixtures/equipment-meran-builds.v1.json) cover balanced, offense, sustain, shield defense, area and ranged builds. Each fills all eight equipment slots with seven or eight actual items, and equips six distinct Shenic Essences. Styles are obtainable in Shenic; Meran rewards are not prerequisites for these fixtures. Each fixture is tested with Tier 1 rank 5 and Tier 2 ranks 0, 3 and 5. These are controlled full-loadout states, not a simulated sequence of individual purchases.

Each run contains 528 cases: six builds × four gear states × (four ordinary areas + eighteen dungeon room templates). Each case has 128 trials, totaling **67,584 fights per run**. Ordinary areas use their entry levels, 50/55/60/65, and the production weighted spawn selector. All three combat room types in every Meran difficulty use their actual rosters. Dungeon rooms start fresh without optional run buffs, support or other run modifiers.

| Run | Seed | Dungeon character level | Essence level / ascension | Tangled Cave III multiplier |
| --- | --- | --- | --- | --- |
| Baseline | 1337 | 65 | 30 / first ascension | 1.3 |
| Adjusted | 1337 | 65 | 30 / first ascension | 1.1 |
| Entry progression | 1337 | 50 | 10 / unascended | 1.1 |
| Independent seed check | 20260903 | 65 | 30 / first ascension | 1.1 |

These four runs contain **270,336 combat evaluations**. Seeds and rosters are shared across builds and gear states to isolate changes; this is not 270,336 independent samples of the player population. Earlier small probes are excluded from this count. Six Essences remain equipped at levels 60–65 even though a seventh slot is available. No evolved Essences, Soulstones, titles, guild bonuses or optional dungeon buffs are included. The level/Essence scenarios establish useful boundaries, not complete character progression or optimized counter-builds.

Run from the repository root:

```powershell
dotnet run --project LL/tools/LegendsLegacy.Balance -c Release -- --equipment-reference-builds --meran-pve --trials 128 --essence-level 30 --dungeon-level 65 --seed 1337 --output "$env:TEMP/ll-meran-final65"
dotnet run --project LL/tools/LegendsLegacy.Balance -c Release --no-build -- --equipment-reference-builds --meran-pve --trials 128 --essence-level 10 --dungeon-level 50 --seed 1337 --output "$env:TEMP/ll-meran-entry50"
dotnet run --project LL/tools/LegendsLegacy.Balance -c Release --no-build -- --equipment-reference-builds --meran-pve --trials 128 --essence-level 30 --dungeon-level 65 --seed 20260903 --output "$env:TEMP/ll-meran-holdout65"
```

Each output directory contains `equipment-meran-pve.json`, including exact fixtures, per-case character/gear/Essence levels, individual seeds, enemy rosters, outcomes, durations, earned Cinders, economy projections and SHA-256 hashes of every content JSON input. `--content-root` accepts a separate API content directory for candidate comparisons. To reproduce the baseline, copy the current `Data` directory into a separate temporary content root and change only Tangled Cave III's multiplier back to 1.3 there. Do not overwrite the gameplay catalog for a comparison.

Input fingerprints at this checkpoint:

| Input | SHA-256 |
| --- | --- |
| Meran fixture | `0111EBBA7289EF0E33F36DE4C638360427F3FFA3E7C39E6AD49A2F3CB4D9BF31` |
| Baseline dungeon JSON | `3DC203E90B2E6444F1047BDA81507E2B0F2767A4B26772407D585D328C08446F` |
| Adjusted dungeon JSON | `4DE7653FEF858F96E948FE6E96DD7C035FF42C0EB51D33A4F9448CB3EC42AF00` |

The adjusted report's entire content manifest was checked against the final working tree. Hashes identify content bytes; reproducing outcomes also requires the same engine/tool implementation. The JSON reports remain local generated artifacts; the tables below preserve the reviewed results in the repository.

## Ordinary combat and entry

All 96 ordinary build/gear cases won 128/128 fights with seed 1337, both with level-10 and level-30 Essences. The independent seed check produced one defeat: the sustain fixture in Tier 1 rank 5 lost to a three-enemy Tempest Aerie spawn containing a Shadow Harpy and two Flame Harpies. That case won 127/128; the other 95 cases won 128/128.

The authored count distribution is 3% one enemy, 96.9% two and 0.1% three. Across the two unique seed sets, only one three-enemy roster was sampled. These results support entry and ordinary income for the tested builds; they do not establish perfect reliability against every rare roster.

Tangled Cave I is more build-sensitive than Great Tree I. At level 50 with level-10 Essences and Tier 1 rank-5 gear, its boss wins were 123/128 balanced, 61/128 offense, 5/128 sustain, 128/128 shield, 88/128 area and 0/128 ranged. Great Tree I's boss was 128/128 for all six. Upgrading to Tier 2 rank 5 raised Tangled Cave I to 128/128 for five fixtures and 115/128 for ranged. The ordinary Tier 2 target loop therefore matters before expecting reliable dungeon performance from every entry build.

## Tangled Cave III adjustment

Boss victories at level 65, level-30 first-ascension Essences and Tier 2 rank 5:

| Fixture | Baseline, seed 1337 | Adjusted, seed 1337 | Adjusted, seed 20260903 |
| --- | --- | --- | --- |
| Balanced | 5/128 | 128/128 | 128/128 |
| Offense | 0/128 | 102/128 | 101/128 |
| Sustain | 0/128 | 94/128 | 96/128 |
| Shield | 128/128 | 128/128 | 128/128 |
| Area | 0/128 | 128/128 | 128/128 |
| Ranged | 0/128 | 12/128 | 7/128 |

The independent offense sample includes one time-limit draw; other non-victories in this table are defeats. A recorded 128/128 is an observation, not a guaranteed clear rate.

The grade remains an investment/build check. With the adjusted content and seed 1337, the level-65 balanced fixture went from 6/128 wins in Tier 1 rank 5 to 36/128 at Tier 2 rank 0, 122/128 at rank 3 and 128/128 at rank 5. The area fixture went 25/128 → 56/128 → 123/128 → 128/128. Shield remains particularly effective; ranged needs a different loadout or additional progression.

At level 50 with unascended level-10 Essences, even Tier 2 rank 5 yielded 114/128 balanced, 128/128 shield, 14/128 area and zero offense/sustain/ranged boss wins. Equipment alone does not make this an entry-level dungeon.

Only the authored multiplier changed. Grade III still uses shared progression step 30 while Meran grades I–II use step 20. A regression now compares the resulting shared-curve health and power, rather than incorrectly comparing just the final multiplier. Both still increase with each grade. Enemy identities, abilities, access rules, reward pools, drop rates and guarantees retain their current behavior.

## Great Tree and room limits

At level 65 with ascended Essences and Tier 2 rank 5, Great Tree III's boss was 128/128 for five fixtures on each seed set. The sustain fixture timed out in all 256 boss fights. Its regular room also produced one timeout on seed 1337 and two on the independent seed; all other fully improved level-65 regular/miniboss cases won every sampled fight across both families.

At level 50 with level-10 Essences, Great Tree III still requires build/progression choices: sustain and shield timed out in every boss trial, while balanced, offense and area won all 128. Ranged won 87/128. The content has viable alternatives, so this assessment leaves Great Tree unchanged instead of reducing the whole dungeon around one sustain fixture.

Room results are **not full-run completion probabilities**. This tool does not exercise the run scheduler, access checks, sigil spending, route/camp choices, optional modifiers, reward claiming or persistence. Multiplying room win rates would also ignore those run systems. Those remain acceptance work.

## Scrap and Cinder pacing

Projections use the existing 10-second encounter cadence, victory-only ordinary rewards and production per-encounter Cinder rounding. Losses and timeouts remain in the elapsed-time denominator. Scrap is the configured perfect-day amount multiplied by measured win rate; Cinders are the mean actually earned per sampled encounter multiplied by 8,640. Fight ticks do not replace the server's encounter cadence. Estimates assume continuous eligible combat and exclude bonuses, other Cinder spending, discoveries, dungeon income, existing holdings and salvage.

| Area | Scrap per perfect day | Sampled Cinders/day range at 100% wins | Scrap time for one item, ranks 0→5 |
| --- | --- | --- | --- |
| Warfang Frontier | 72 | 58,860–59,670 | 4.31 days |
| Rotgrave Fields | 84 | 59,265–59,873 | 3.69 days |
| Tempest Aerie | 96 | 66,960–68,513 | 3.23 days |
| Wolfsbane Reach | 108 | 68,040–69,120 | 2.87 days |

The Cinder ranges use the two level-30 seed sets' winning balanced fixture; they are sampling ranges, not confidence bounds. Integer encounter rounding can make adjacent areas' Cinder income similar. The exceptional sustain loss lowers that fixture's Tempest projection accordingly.

At perfect victory pacing, a chosen plain item takes one hour and a repeat sigil twelve hours. One Tier 2 item's ranks 1–5 cost 310 Scrap and 15,500 Cinders. Its Cinder cost takes roughly **5.4–6.3 hours** of the sampled ordinary income; its Scrap takes **69–103 hours**. Scrap is the Forge constraint for these builds, so this pass retains the current prices.

Improving a full set of seven or eight items from rank 0 to 5 costs 2,170–2,480 Scrap: approximately 20.1–23.0 days at Wolfsbane or 30.1–34.4 at Warfang before other sources. These are optional maximum-rank costs, not a requirement to enter Meran or its first dungeon grades. Each fully paid Tier 1 item salvages for 77 Scrap, enough to fund a replacement's Tier 2 ranks 1–3 for 70 Scrap. Awarded/free ranks do not create this salvage investment. Dungeon completions add their existing 8 Scrap, but room simulations do not claim that income.

## Verification and remaining acceptance

`build/run-tests.ps1` rebuilt and passed **64 focused backend tests**, including 13 new Meran tests. The filter covered `MeranProgressionTests`, `EquipmentReferenceBuildTests`, `EquipmentRegionTwoTests`, `DungeonCatalogTests`, `DungeonEnemyDifficultyScalingTests` and `AreaExperienceBalance`. Five existing compiler/analyzer warnings remain; there were no errors. Coverage includes deterministic production reports, matched rosters, entry levels, alternative viable boss builds, victory-only economy, timeouts, zero-income estimates and CLI mode validation. The earlier full 1,913-test run remains a separate implementation checkpoint; the full suite was not rerun for this bounded change.

All 188 checked local file links across the seven updated Markdown files resolve. New-file whitespace checks and scoped `git diff --check` passed.

Next acceptance should walk an authenticated character through a Tier 2 target, award/equip/recovery, Forge improvement/style/salvage, sigil entry and complete dungeon reward claim, including reloads. It should also exercise complete dungeon runs with normal route choices and broader Essence/counter-build progression. No authenticated walkthrough or game-database execution was performed here. The deferred admin tutorial issue, LiveOps, raids and merchants remain outside this work.

This pass adds offline tooling, fixtures, regression coverage and one content value. **No new migration or configuration setting** is required, and no deployment or database update was performed. A future authorized API content refresh/restart is required to load the adjusted dungeon catalog. Existing pending migrations and automatic startup application still follow the [implementation status](equipment-implementation-status.md#cleanup-migrations-and-local-startup).
