# Marketplace and Economy Design

## Purpose

This document records the current Legends Legacy item economy, assesses the existing marketplace implementation, and defines the target marketplace experience and implementation priorities.

The recommended marketplace has one consistent shell with two trading models:

- **Commodity markets** for stackable and interchangeable items: resources, catalysts, blueprints, consumables, and unbound essences.
- **Exact-item listings** for rolled equipment, where the individual instance, attributes, quality, Potential, blueprint, and tempering history matter.

## Current item economy

The current item catalogue contains 199 item bases:

| Type | Count |
| --- | ---: |
| Resources | 72 |
| Equipment | 66 |
| Unbound essences | 61 |

Crafting V2 adds nine base recipes, 35 physical forms, and 11 blueprint families. These systems form three principal economic loops.

### Acquisition loop

Combat, idle areas, dungeons, prophecies, guild activities, and the Colosseum generate:

- Cinders
- Standard and special crafting materials
- Tools and equipment
- Blueprints
- Unbound essences
- Soul Dust and monster cores
- Sigils and Sigil Fragments
- Soulstones, Fate Echo, Glory, and guild currencies

### Crafting loop

Crafting consumes tiered materials and special materials to create unique equipment instances. Blueprint items are consumed when learned. Crafted equipment receives quality, starting Potential, rolled modifiers, and potentially a blueprint identity. Tempering consumes Potential and improves the exact item instance.

### Essence loop

Unbound essences are either absorbed into the Soul Archive or dismantled into Soul Dust. Essence advancement consumes Soul Dust, Potential cores, monster cores, and evolution catalysts. This gives unbound essences and their supporting materials durable demand.

### Existing item sinks

The item economy already has several healthy sinks:

- Crafting permanently consumes resources.
- Blueprint learning consumes blueprint copies.
- Essence absorption and dismantling consume unbound essences.
- Essence progression consumes Soul Dust, monster cores, and catalysts.
- Equipment can be scrapped into Tempered Scrap.
- Tempering consumes finite item Potential.
- Dungeon entry consumes sigils or the resources used to assemble them.

## Currency health

Cinders have many faucets and almost no permanent sink. Examples include:

- Idle combat targets 1,000 Cinders per hour before difficulty scaling.
- Daily prophecies award at least 1,000–1,750 Cinders.
- Weekly prophecies award at least 8,000–12,000 Cinders.
- Dungeon encounters, events, and completion rewards.
- Tournament, guild, cache, and tutorial rewards.

Marketplace purchases transfer Cinders between players. Buy orders escrow Cinders but return the unspent amount when cancelled. Neither operation reduces the total supply. The likely long-term result is inflation, increasing nominal prices, and deteriorating purchasing power for new players.

### Recommended initial currency sink

Introduce a configurable seller transaction fee:

- Default: 3% of the completed sale.
- Minimum: 1 Cinder.
- Charged identically when a buyer takes a sell listing or a seller fills a buy order.
- No upfront listing fee initially; early liquidity is more valuable than discouraging speculative listings.

Track Cinders created, destroyed, transferred, and held per active character. If supply still grows too quickly, prefer optional recurring sinks—cosmetics, crafting services, convenience services, or high-tier progression services—over punitive durability.

## Assessment of the existing marketplace

### Good foundations

- Listed items leave inventory and enter escrow.
- Buy-order Cinders are escrowed.
- Stackable listings and buy orders support partial fills.
- Equipment retains its exact item instance.
- Marketplace commands run through the transactional MediatR pipeline.
- Real-time events update the Angular marketplace state.
- Listing and buy-order counts are capped.

### Player-experience problems

- The equipment screen labels sell listings as “Buy Orders.”
- `Sell`, `Fill Bid`, `Buy`, `Bid`, and `Cancel Bid` appear together without a clear transaction mode.
- “Bid” does not clearly mean “create a buy order.”
- Selecting an order-book row silently changes the shared price input.
- Empty markets consume most of the page without helping players create demand.
- Equipment and essences feel like separate products rather than marketplace categories.
- Active-order management is embedded in the selling flow.
- No price history, median, volume, spread, expiration, or comparable-price guidance exists.
- Commodity families are hard-coded in Angular.
- The commodity catalogue is derived only from current inventory and current orders. A player cannot create the first buy order for an empty item they do not own.

### Correctness and security risks

1. **Non-stackable quantity duplication:** a unique item can be submitted with quantity greater than one, while inventory removal removes only the single instance.
2. **Item loss at the listing cap:** inventory is removed before the repository rejects an eleventh listing.
3. **Bound-item trading:** `ItemBase.IsBound` is not validated by marketplace operations.
4. **Unchecked arithmetic:** `UnitPrice * Quantity` can overflow without configured limits.
5. **Cross-player concurrency:** the application lock covers the requesting character, not the other participant or shared order. Marketplace rows and balances have no concurrency token or conditional update.
6. **Incomplete DTO:** listing creation time is not exposed even though client matching attempts price-time ordering.
7. **Non-atomic order-book sweep:** the client issues sequential purchases, so a failure can produce an unintended partial result.
8. **Unbounded reads:** the API loads every listing and its item graph, then filters in the browser.
9. **Missing trade ledger:** `MarketPlaceOrder` is unused, preventing reliable history, volume, price guidance, and auditability.

## Target information architecture

The permanent navigation is task-oriented:

1. **Browse** — find an item, inspect its market, and buy or place demand.
2. **Sell** — start from eligible inventory and create a listing or instantly fill demand.
3. **My Orders** — manage active sell listings, active buy orders, escrow, and history.

Categories filter the selected item rather than replacing the overall interaction model:

- Resources
- Catalysts
- Blueprints
- Consumables
- Essences
- Equipment

## Commodity experience

Resources, catalysts, blueprints, consumables, and unbound essences use an item-centric market page. Selecting a catalogue item shows:

- Owned quantity
- Best sell price
- Best buy price
- Spread
- Seven-day median
- Recent volume
- Sell listings ordered by lowest price, then creation time
- Buy orders ordered by highest price, then creation time

The transaction ticket asks four questions:

1. Buy or Sell?
2. Instant or Set my price?
3. How many?
4. What is the maximum or minimum unit price?

An instant order consumes compatible orders using price-time priority. A limit order consumes compatible orders first and leaves any remainder active. The complete operation is atomic on the server.

### Scalable item catalogue

Large categories such as Blueprints use a master-detail layout instead of rendering every item as a full-width grid:

- A compact, independently scrollable catalogue remains visible beside the selected market on desktop and moves above it on smaller screens.
- On phones, Browse becomes a true master-detail flow: players first see the catalogue, tap an item to open its market, and return with an explicit back action.
- The phone detail view uses a compact four-stat row, a Sell Orders/Buy Orders switcher, and a safe-area-aware sticky purchase bar.
- Search matches item names and descriptions.
- Status filters narrow the catalogue to items with an active market or items owned by the player.
- Sorting prioritizes market activity, alphabetical discovery, or the lowest current asking price.
- Each row shows owned quantity and compact best ask/bid guidance without competing with the selected item's order book.
- The selected item receives the full detail area, transaction ticket, market statistics, sell orders, and buy orders.

Marketplace navigation categories are UI concepts and are deliberately separate from the backend `ItemType`. Blueprints and Catalysts are both stored as resources today, but retain independent navigation, catalogue state, and Sell-mode filtering. This prevents the breadcrumb and selected tab from collapsing back to Resources.

Future Blueprint filters such as family, equipment slot, learned/unlearned state, and crafting tier should be driven by structured catalogue metadata. They should not be inferred from display names.

### Unbound essences

Unbound essences are stackable and should use the commodity market, including buy orders. Show rarity, dismantle value, source, and whether the player already owns the corresponding Soul Archive essence. Bound player essences are not inventory commodities and remain untradeable.

## Equipment experience

Equipment remains an exact-instance market. Generic equipment buy orders are intentionally deferred because “Rare sword” does not define a fungible product when tier, form, quality, Potential, blueprint, affixes, modifiers, and tempering progress differ.

Equipment filters should include:

- Slot and weapon/tool type
- Tier
- Rarity and quality
- Current and maximum Potential
- Blueprint family
- Masterpiece status
- Affixes, special modifiers, and attribute modifiers
- Price range

Each result compares the listing against the currently equipped item and highlights gains and losses. A later version may add saved searches or specification-based requests rather than misleading generic buy orders.

## Selling and order management

The Sell page starts with eligible inventory. Bound, equipped, queued, and otherwise unavailable items are excluded or explicitly explained. The sale ticket shows:

- Current best bid
- Lowest competing sell listing
- Seven-day median
- Suggested price range
- Gross value
- Marketplace fee
- Net proceeds

My Orders contains active sell listings, active buy orders, escrowed Cinders, completed trades, expired and cancelled orders, and visible slot usage. Cancellation always targets an explicit order ID.

## Market rules

- Positive quantities and prices with server-configured maximums.
- Non-stackable listings always have quantity one.
- Bound items cannot be listed or targeted by buy orders.
- Price-time priority for commodity matching.
- No self-trading.
- A character can hold at most one active resting order for a stackable item: duplicate same-side orders and opposing buy/sell orders are rejected. Immediate Buy Now and Sell Now trades remain available because they do not create another resting order.
- Seven-day default expiry with automatic escrow return.
- Configurable seller fee, defaulting to 3% with a one-Cinder minimum.
- Every completed fill creates an immutable trade record.
- Server-side pagination and filters for catalogue, listings, orders, and history.

## Implementation sequence

1. Correct quantity, binding, cap, overflow, and concurrency behavior.
2. Add a complete server-backed tradable catalogue.
3. Add atomic commodity matching and a persisted trade ledger.
4. Introduce the unified Browse, Sell, and My Orders shell.
5. Move unbound essences into commodity trading.
6. Add equipment-specific filters and equipped-item comparison.
7. Enable configurable fees, expiration, and economy telemetry.

## Implementation status — July 2026

Implemented in the current marketplace slice:

- Server-side validation for binding, unique-item quantities, caps, price and quantity limits, and overflow.
- Database row locking for marketplace rows and participating character balances.
- A complete tradable-item catalogue, including empty markets.
- Atomic instant commodity buys and sells with price-time priority.
- Crossing commodity buy and sell orders immediately consume compatible opposing orders in price-time priority and leave only the unfilled remainder active.
- Resting-order guards prevent duplicate same-side orders and prevent a character from creating a buy order while selling the same item, or a sell listing while holding a buy order for it.
- Commodity buy orders for resources, catalysts, blueprints, consumables, and unbound essences.
- Exact-instance equipment listings with slot, rarity, quality, minimum tier, and minimum Potential filters.
- Browse, Sell, and My Orders navigation with active-order cancellation and recent trade history.
- A configurable three-percent seller fee and persisted trade ledger.
- Per-item best bid/ask, spread, last trade, seven-day median, and 24-hour volume guidance.
- Seven-day listing and buy-order expiry, transactional item/Cinder escrow return, and a recurring Quartz settlement job.
- A responsive master-detail commodity catalogue with search, status filters, sorting, and category-aware naming.
- Explicit marketplace category identity, so Blueprints and Catalysts remain first-class tabs even though both currently map to the Resource item type.
- Cumulative order-book selection that includes every better-priced level, while excluding the buyer's own quantity at mixed-ownership price levels.
- Immediate trade receipts showing filled quantity, gross or net Cinders, seller fees, and any resting remainder.

Still required for a production-scale market:

1. Replace the remaining unbounded listing and buy-order reads with server-side filtering, cursor pagination, and compact catalogue summaries.
2. Persist cancelled and expired lifecycle events so support and players can audit more than completed trades.
3. Publish expiration events or periodically reconcile open clients after the worker settles escrow.
4. Extend equipment discovery with price ranges, blueprint family, affix/modifier search, maximum Potential, and saved searches.
5. Run keyboard, screen-size, and player usability testing before treating the interaction design as final.

## Success measures

- Time from opening the marketplace to completing a common purchase.
- Search-to-purchase conversion.
- Percentage of catalogue items with active demand or supply.
- Median time to fill by item family.
- Spread and traded volume by item family.
- Cinders created versus destroyed per active player.
- Cancellation rate and failed transaction rate.
- Share of trades using instant versus limit behavior.
- Number of new players able to afford representative progression baskets.
