# Meran / Tier 2 equipment progression

Implemented in the working tree on 3 September 2026. Values are provisional. This extends the current equipment system without reintroducing crafting, gathering, queued tempering or Alpha compatibility.

## Ordinary combat

Shenic and Meran have independent plain-target, repeat-sigil and fractional Scrap progress. Changing areas within one region preserves its progress; fighting in the other region advances only that region. Targets freeze their tier, stats and required victories when selected. Selection settles due combat before changing choices, and operation receipts include the pool ID to prevent cross-region replay.

| Rule | Shenic / Tier 1 | Meran / Tier 2 |
| --- | --- | --- |
| Plain choices | 31 archetypes | The same 31 archetypes, evaluated at Tier 2 |
| Plain target | 360 victories, one bound rank-0 item, then pauses | Same |
| Repeating sigil | 4,320 victories | Same |
| Random equipment | 0.03% per victory, unbound rank 0 | Same chance, Tier 2 |
| Random plain salvage | 1 Scrap | 2 Scrap |
| Choice access | Enter eligible combat | Level 50 and enter eligible Meran combat |
| Sigil families | Goblin Mines, Forgotten Catacombs; existing level/quest requirements | Tangled Cave, Great Tree; level 50 and server World Tower floor 10 cleared |

Meran income per 8,640 victories is 72 Scrap in Warfang Frontier, 84 in Rotgrave Fields, 96 in Tempest Aerie, and 108 in Wolfsbane Reach. Defeats yield no progress or Scrap. Fractions persist across batches, reloads and area changes. Training and unauthored regions yield no ordinary equipment rewards.

The Forge rewards tab lets players choose a region and shows that pool's tier, exact target stats, saved choices and progress. Recovery distinguishes copies by both definition and tier; Tier 1 holdings do not satisfy a Tier 2 entitlement. Restoring a missing earned target creates plain, bound, rank-0 gear without refunds or paid investment.

## Dungeon equipment and styles

All three difficulties of Tangled Cave and Great Tree have separate Tier 2 protection pools, each with eight named targets. Dungeon difficulty does not increase equipment tier. Each completed run grants 8 Scrap; with a selected target, a 20% matching roll and an eight-completion guarantee apply. The first grade-I completion guarantees the selected target. Equipment is rank 1 with its native style. Random matches are unbound with 4 base salvage Scrap; guarantees are bound with zero base salvage. No paid investment is invented.

| Family | New named equipment | Native styles |
| --- | --- | --- |
| Tangled Cave | Cavern Executioner, Cavern Repeater, Silken Death Band, Venom-Touched Sword, Broodstalker Spear, Toxic Whisper, Hivefang Dagger, Broodbreaker Gauntlets | Execution, Venom, Hive |
| Great Tree | Heartwood Staff, Canopy Spirit Robe, Rootwarden Mail, Rootwarden Leggings, Wildheart Longbow, Wildheart Amulet, Great Tree Bulwark, Ancient Bark Helm | Spirit, Warden, Primal, Aegis |

Existing level, previous-difficulty and server Tower access rules apply. Starting a run freezes its selected descriptor and reward terms; switching targets, reloading content or retrying completion cannot alter that commitment or duplicate its award.

First-clear Blueprint bundles and Monster Core rewards remain. Six newly authored completion tables add a 10% total chance of a Blueprint on each completion, divided equally among the family's three or four books. These tables contain only current style books. The removed gathering-tool tables have not been restored. Plain earned-target recovery supplies a recoverable baseline; named dungeon items must be reacquired through their source.

## Forge prices and replacement

Price version 2 contains explicit tier entries. Unsupported tiers cannot be improved or restyled. Tier 1 prices and its stat formula remain unchanged.

| Cost | Tier 1 | Tier 2 |
| --- | --- | --- |
| Scrap for ranks 1–5 | 5 / 10 / 20 / 40 / 80 | 10 / 20 / 40 / 80 / 160 |
| Cinders for ranks 1–5 | 250 / 500 / 1,000 / 2,000 / 4,000 | 500 / 1,000 / 2,000 / 4,000 / 8,000 |
| Total paid rank investment | 155 Scrap + 7,750 Cinders | 310 Scrap + 15,500 Cinders |
| Paid style change | 250 Cinders | 500 Cinders |
| Paid Scrap recovered on salvage | 50%, rounded down | Same |

Blueprint knowledge and the one free application per learned style belong to the character, across tiers. A new tier does not reset either. Recovery, free ranks and awarded ranks do not create salvage investment.

Doubling Meran's entrance/end-area Scrap rates alongside the rank costs preserves the comparable perfect-victory time to fully improve one item: approximately 4.31 days at the entrance and 2.87 days in the last area, excluding dungeon rewards, discoveries and existing holdings. These are income projections, not predicted playtime; losses and Cinder constraints still matter.

The unchanged tier curve gives equivalent Tier 2 rank-0 equipment **12.75% more stat budget than Tier 1 rank 5**. Budget is not a percentage increase in every stat or combat outcome. Salvaging a Tier 1 item with all five ranks paid returns 77 Scrap, enough for Tier 2 ranks 1–3 (70 Scrap), with 7 remaining. A fully paid Tier 2 item returns 155 Scrap. Restyling remains a separate Cinder decision.

## Validation and limits

The focused equipment suite covers regional isolation and request conflicts, split-batch discoveries, server-specific Tower gates, both-tier earned recovery, all six Meran dungeon pools, Forge charges/salvage, and shared style knowledge. The Forge browser suite covers region changes, tier-specific previews and uncertain-request retries.

Verification commands and evidence:

- Backend tests run through `build/run-tests.ps1`: the final full run passed all 1,913 tests. The initial equipment-focused run passed 296 tests. See the [implementation status](equipment-implementation-status.md#latest-verification).
- `npm.cmd run test:ci -- --include=src/app/features/game/character/forge/forge-state.service.spec.ts`: 10 passed. `npm.cmd run build:development`: passed, with existing Angular warnings.
- The initial broader browser run had 631 passed and five failures in quest-walkthrough selection, obsolete gathering expectations and the world-map region redirect. The subsequent [frontend acceptance follow-up](equipment-post-alpha-cleanup.md#frontend-acceptance-follow-up--3-september-2026) resolved these and passed the full current **643-test** player suite. It also passed the player development build and 94 backend equipment-flow tests.
- EF `migrations has-pending-model-changes` reports no pending changes. Generated PostgreSQL SQL adds the receipt field, marks existing Shenic receipts, then requires the field; it was inspected without applying it.
- Eight updated Markdown files had 183 checked local file links, all resolving at that implementation checkpoint. A later authenticated Forge walkthrough reached loading but could not proceed because the local API stopped responding; no gameplay mutations or PostgreSQL verification were performed.

The reference tool now accepts `--region-two-transition`:

```powershell
dotnet run --project LL/tools/LegendsLegacy.Balance --configuration Release -- --equipment-reference-builds --region-two-transition --seed 1337 --output "$env:TEMP/ll-region-two-transition"
```

It expands the twelve existing loadouts to both tiers and all six ranks at character level 50: **144 production-engine simulations**, each against a synthetic Tier 1 rank-5 balanced opponent. The output is `equipment-region-two-transition.json`, with frozen descriptors, prepared stats, ratings, seed and results. The fixture hash identifies the original fixture file; the report records the transformed inputs explicitly. Reference builds do not create paid receipts; actual investment/refund behavior is covered separately by Forge service tests.

For seed 1337, Tier 1 rank 5 and Tier 2 rank 0 each had eleven time-limit draws and one defeat across twelve loadouts; Tier 2 rank 5 had twelve draws. The limit is 1,800 ticks. This confirms that the expanded equipment runs through the production engine, but the many draws make it unsuitable as evidence of real Meran victory rates or final balance.

The subsequent [Meran PvE and Forge pacing assessment](equipment-meran-pve-balance-report.md) evaluated 270,336 fights using actual area spawns, dungeon rosters, authored abilities and the live 6,000-tick limit. It compares six complete builds, four gear states, entry/later character and Essence progression, and an independent seed sample. Tangled Cave III's authored multiplier is now 1.1 (previously 1.3); higher-grade shared scaling remains. Ordinary combat supports Tier 1 entry for these fixtures, and Scrap remains the limiting Forge resource, so prices are unchanged. Sixty-four focused backend tests passed for that follow-up. Complete dungeon runs, broader Essence/counter-build progression and authenticated gameplay/PostgreSQL acceptance remain open.

## Storage and local startup

[ScopeOrdinaryEquipmentSelectionsByPool](../../LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260903131540_ScopeOrdinaryEquipmentSelectionsByPool.cs) adds the required pool ID to selection receipts. Existing receipts belong to the current Shenic pool and are marked accordingly once. Progress already has a character/pool key and requires no conversion. This is current reward idempotency, not Alpha compatibility.

`GET equipmentacquisition/ordinary` now returns a list of regional views; its POST requires `poolId`. Deploy API and player builds together when deployment is later authorized. No new configuration flag is required. This task generates the migration but does not apply it to a game database; the API's existing startup runner applies pending migrations when restarted. LiveOps feature work, raids, gear merchants and the deferred admin tutorial issue are outside this change.

Content: [ordinary rules](../../LL/src/API/API.LL/Data/equipment/equipment-ordinary.v1.json), [named equipment](../../LL/src/API/API.LL/Data/equipment/equipment-named.v1.json), [protection pools](../../LL/src/API/API.LL/Data/equipment/equipment-protection-pools.v1.json), [Forge prices](../../LL/src/API/API.LL/Data/equipment/equipment-forge-prices.v1.json), [dungeons](../../LL/src/API/API.LL/Data/dungeons/dungeons.json), [completion tables](../../LL/src/API/API.LL/Data/rewards/reward-tables.json).
