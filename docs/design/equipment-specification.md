# Equipment system contract

Updated: 3 September 2026.

This document defines the supported equipment system after the removal of crafting, gathering, tempering, salvaging, and the equipment Forge.

## Equipment identity

Every equipment instance is a frozen, server-authored item. Its descriptor contains:

- definition and item-base identity;
- equipment type, rarity, tier, and rank;
- native and active style identifiers;
- weapon behavior where applicable;
- evaluated stats and equipment-set identity;
- provenance and ownership.

Rank and style remain part of authored gear identity and stat evaluation. Current gameplay exposes no operation that changes them after an item is awarded.

## Acquisition

Supported sources are starter grants, ordinary combat discoveries, protected dungeon rewards, baseline recovery, and explicitly authored rewards from other current systems. All grants must reference a valid equipment definition and must be evaluated by the canonical equipment evaluator before persistence.

Ordinary acquisition may let a character choose a plain target from the released area pool. Protected dungeon acquisition may let a character choose a named target, freezes the result inputs when the run is committed, and tracks its guarantee independently. Recovery restores only a current entitlement that is missing from the character's holdings.

Quest-specific equipment selection is deferred. Quests must not manufacture a Forge, Blueprint, Scrap, or upgrade dependency while that design is pending.

## Ownership

Equipment can be bound personal, unbound personal, or guild-owned. Equip, transfer, marketplace, donation, loan, return, and recovery checks must use the frozen ownership state. A transfer changes ownership without rerolling rank, style, or stats.

## Removed operations and currencies

The game has no supported equipment rank improvement, style learning/application, equipment salvage, mutation quote, Forge receipt, Forge pricing, Tempered Scrap, or equipment Blueprint reward. API routes, services, commands, persistence, realtime scopes, player UI, help, rewards, and tests must not expose those concepts.

Shared stat-allocation and offline simulation code may retain recipe-oriented internal inputs where a current tool still consumes them. Those helpers do not grant a player crafting or equipment-mutation capability.

## Content validation

Released content must fail loading when it references an unknown equipment definition, item base, style, pool, reward table, or set. Blank and duplicate identifiers are invalid. Authored rewards must not reference removed Forge resources or containers.

## Deferred design work

The next equipment-upgrade model and quest gear-selection rules require separate design decisions. Their future implementation must start from the immutable equipment contract above and add explicit content, API, persistence, UI, economy, and migration requirements.

See [implementation status](equipment-implementation-status.md) and [Forge removal](equipment-forge-removal.md).
