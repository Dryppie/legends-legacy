# Equipment Forge responsibilities and removal impact

Updated: 5 September 2026.

The former Forge combined equipment mutation, an equipment economy, and several equipment-acquisition controls in one feature. The mutation system and its orphaned recovery and targeting systems have now been removed. Starter equipment and random regional drops replace the acquisition responsibilities that belonged on its combined player page.

## Former responsibilities

| Responsibility     | Previous behavior                                                                                                                                                           | Result after removal                                                                                                        |
| ------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Rank improvement   | Increased equipment deterministically from rank 0 to rank 5 using Tempered Scrap and Cinders.                                                                               | Replaced by Reinforcement, using bound Reinforcement Parts and Cinders.                                                      |
| Style changes      | Blueprint books permanently taught compatible styles, provided one free application, and allowed later paid changes.                                                        | Replaced by consumable Blueprint Variants. A compatible item plus Cinders applies a style while preserving tier, rarity, Quality, rank, and the frozen attribute roll. |
| Equipment salvage  | Destroyed eligible unequipped equipment for Scrap, including part of the Scrap previously spent on rank improvements.                                                       | Replaced by Dismantle, returning Parts from base item value plus the current rank's configured recovery value.               |
| Change preview     | Displayed the resulting item stats, set effects, equipped-loadout impact, costs, binding changes, and salvage return before confirmation.                                   | Reinforcement and Dismantle use exact server quotes and show cost or return before confirmation.                             |
| Transaction safety | Used expiring quote tokens, operation identifiers, persisted receipts, ownership checks, favorite-item confirmation, and combat settlement before modifying equipped items. | Restored for the narrower replacement loop, including idempotent receipts and fresh-quote conflicts.                         |
| Equipment economy  | Connected Scrap, Cinders, Blueprint books, quests, dungeons, tournaments, shops, raids, Prophecies, and selection containers.                                               | Reinforcement Parts drive rank progression and dismantling, while consumable Blueprint Variants and Cinders drive style changes. Broader activity rewards still need rebalancing. |
| Support visibility | Exposed learned styles, investments, receipts, costs, and salvage details through LiveOps support snapshots.                                                                | Those records and support projections no longer exist. Alpha data is not preserved or converted.                            |

## Responsibilities that were coupled to the Forge page

The removed player route was an **Equipment & Forge** page rather than a Forge-only screen. Its Rewards & Recovery tab also provided the only normal player interface for:

- selecting and claiming the former starter armor and weapon kit (replaced by the Arms Chest flow);
- selecting the former regional equipment and Sigil targets;
- selecting the former named-equipment targets for protected dungeon rewards;
- recovering missing starter equipment; and
- recovering previously earned plain equipment.

The targeting APIs and their Angular contracts have been retired. The equipment-progression client now exposes only starter/access state; area and dungeon rewards are automatic.

## Current gameplay gaps

### Starter progression

Resolved on 4 September 2026: completing **The Soul Archive** grants one bound Arms Chest. The normal Inventory selection-container flow lets the player choose a Shortsword, Dagger, Hatchet, Mace, or Wand. The chosen common Tier 1, rank 0 weapon is personally bound and recorded as the First Weapon grant. Equipping it completes **First Weapon** and unlocks **Into the Ruins**.

The former starter armor and accessory grants and **Ready for the Road** quest are retired, leaving no onboarding dependency on the removed Forge page.

### Equipment targeting

Resolved on 5 September 2026: all released combat areas can randomly drop any equipment archetype at the tier of their region. Dungeons use the same regional pool with a higher drop chance, stronger rarity distribution, and rank 1 instead of area rank 0. Rarity again has seven values and scales stats independently from rank. Quality again has Crude, Standard, Fine, Exceptional, and Masterpiece values with its former stat multipliers. Combat drops use the former Mastery 100 quality odds and receive a frozen ±5% attribute-budget roll.

All combat areas in a region can also randomly drop either dungeon Sigil belonging to that region at exactly 1/4,320 per victorious encounter. There is no Sigil selection, level gate, quest gate, progress counter, dungeon target, frozen commitment, first-clear guarantee, or bad-luck counter.

### Equipment recovery

Resolved on 4 September 2026: the orphaned starter and earned-equipment recovery feature was retired. Its endpoints, commands, services, feature flag, client contracts, state-sync registrations, events, and tests were removed. The lightweight earned-equipment entitlement remains only as proof for the related quest objective.

### Long-term equipment progression

Equipment tier, rarity, quality, original attribute roll, and behavior remain stable. Reinforcement can advance the current rank to 5 without rerolling item identity, quality, or attributes, and binds a previously unbound discovery to the character. Compatible consumable Blueprint Variants can replace the active style and its additive attributes or set identity. There is still no free-form stat reroll or tempering path.

Dismantling is available only for personally owned, unequipped inventory equipment. It returns a tier base value plus 50% of the cumulative Reinforcement Part prices represented by the item's current rank. This values rank intrinsic to the item, regardless of whether those ranks were awarded by acquisition or purchased through Reinforcement; no historical "Parts paid" record is required. Favorite items require an explicit second confirmation.

### Economy and rewards

Tempered Scrap remains retired. Bound Reinforcement Parts are consumed alongside Cinders for rank increases and produced by dismantling obsolete equipment. Dungeon completions can award family-specific Blueprint Variant selection containers, with a fourth-miss guarantee, and the Soul Archive introduces the system with a Fury variant. Activity reward sources for Reinforcement Parts still need deliberate balancing beyond the closed dismantle/reinforce loop.

## Systems that remain intact

- Authored starter and equipment archetype definitions.
- Random area and dungeon equipment discoveries across seven rarities and five qualities.
- Equipment evaluation from tier, rarity, quality, frozen attribute roll, rank, style, stat weights, behavior, and set identity.
- Consumable Blueprint Variant drops, dungeon guarantee progress, exact conversion quotes, and compatible style replacement.
- Inventory storage, equipping, personal binding, marketplace transfer, guild donation, and guild loans.
- Starter grants and earned-area-drop quest credit.
- Administrative equipment grants.

## Recommended implementation order

1. ~~Restore starter-kit selection in onboarding so new characters can complete the equipment objectives.~~ Completed with the Arms Chest flow.
2. ~~Replace regional and dungeon targeting with automatic regional equipment and Sigil drops.~~ Completed.
3. Rebalance activities whose Forge rewards were removed.
4. ~~Design and implement the replacement equipment-upgrade loop after the acquisition flow is settled.~~ Completed with Reinforcement and rank-valued Dismantle.

See [Forge removal](equipment-forge-removal.md), [equipment implementation status](equipment-implementation-status.md), and [current equipment contract](equipment-specification.md).
