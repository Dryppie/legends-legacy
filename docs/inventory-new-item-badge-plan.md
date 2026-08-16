# "New" badge for newly crafted inventory items — implementation plan

## Goal

Crafted equipment shows a **New** marker in the inventory until the player inspects that
specific item. The marker is per item, stored server-side, and applies only to equipment whose
acquisition source is crafting.

Decisions taken up front:

| Question | Decision |
|---|---|
| What clears the marker | Selecting/inspecting that individual item in browse mode |
| Where state lives | Server, a nullable `SeenAtUtc` on the `InventoryItem` join row |
| Which items qualify | Equipment with `AcquisitionSource == ItemAcquisitionSources.Crafting` |

## What already exists

The plumbing is favourable — most of the work is one column and one endpoint.

- `InventoryItem` (`LL/src/Core/Domain/Models/Inventories/InventoryItem.cs`) is a per-character
  join row keyed `(InventoryId, ItemInstanceId)`, where `InventoryId` *is* the character id.
- `ItemInstance` already carries `AcquiredAtUtc` and `AcquisitionSource`, and
  `ItemAcquisitionSources.Crafting` is already stamped by
  `CraftingService.CraftItemsAsync` via `AddItemsToInventory(..., ItemAcquisitionSources.Crafting, ...)`.
  Both columns are already indexed in `ItemInstanceConfiguration`.
- `GET /inventory` → `GetInventoryByIdQuery` → AutoMapper → `InventoryDto.InventoryItems`
  is the single read path the inventory page uses.
- The crafting page pushes `result.createdItems` (already `InventoryItemDto`) straight into
  `InventoryStateService.setInventory(...)`, so anything added to the DTO reaches the client
  immediately after a craft with no extra wiring.
- The inventory grid already has a per-item click handler (`handleInventoryItemClick`) and an
  `ll-badge` / `ll-badge-accent` CSS convention used across the app.

The important consequence: **`SeenAtUtc` belongs on `InventoryItem`, not `ItemInstance`.** The
join row is per owner and is destroyed and recreated when an item changes hands, so the marker
scopes and resets correctly for free. A column on `ItemInstance` would follow the item across
trades and marketplace sales and would need manual resetting everywhere.

## Design

### Data

Add to `InventoryItem`:

```csharp
/// <summary>Null until the owning character has inspected this item in the inventory.</summary>
public DateTimeOffset? SeenAtUtc { get; set; }
```

`InventoryItemConfiguration` gets a filtered index for the "does this character have anything
new" lookup:

```csharp
builder.HasIndex(ii => new { ii.InventoryId, ii.SeenAtUtc })
    .HasFilter("\"SeenAtUtc\" IS NULL");
```

### Eligibility

A row is "new" when:

```
SeenAtUtc is null
&& ItemInstance.AcquisitionSource == ItemAcquisitionSources.Crafting
```

Keeping the crafting predicate out of the column (rather than only stamping crafted rows) means
widening the feature later to loot or marketplace purchases is a one-line predicate change, and
the column stays meaningful for every row.

Expose it as a computed `bool IsNew` on `InventoryItemDto` — a mapped projection, not a stored
field, so there is one source of truth.

### Marking seen

New endpoint on `InventoryController`:

```
POST /inventory/items/{itemInstanceId:guid}/seen
```

→ `MarkInventoryItemSeenCommand(CharacterGuid, ItemInstanceId)`. Idempotent: if the row is
missing or already stamped, return success without writing. Only ever sets the current
character's own row, keyed on `(CurrentCharacterGuid, itemInstanceId)` — no cross-character
exposure.

Consider a batch variant (`POST /inventory/items/seen` with an id array) only if the "clear all"
affordance below is wanted; a single-item endpoint is enough for the chosen behaviour.

### Backfill — the one migration detail that matters

The migration must stamp every existing row:

```sql
UPDATE "InventoryItems" SET "SeenAtUtc" = NOW() WHERE "SeenAtUtc" IS NULL;
```

Without this, every crafted item every player already owns lights up as New on deploy.
`Migrations/EquipmentV17MigrationSql.cs` is the existing precedent for data-carrying migration
SQL in this repo.

## Work breakdown

### Phase 1 — Domain and persistence

| File | Change |
|---|---|
| `Core/Domain/Models/Inventories/InventoryItem.cs` | Add `SeenAtUtc` |
| `Core/Domain/Models/Inventories/IInventoryRepository.cs` | Add `Task<bool> MarkItemSeenAsync(Guid characterId, Guid itemInstanceId, CancellationToken ct)` |
| `Infrastructure/Persistence/Persistence.LL/Configurations/Inventories/InventoryItemConfiguration.cs` | Filtered index |
| `Infrastructure/Persistence/Persistence.LL/Repositories/Inventories/InventoryRepository.cs` | Implement `MarkItemSeenAsync`; audit every path that creates an `InventoryItem` (see risks) |
| `Infrastructure/Persistence/Persistence.LL/Migrations/<ts>_AddInventoryItemSeenAt.cs` | Column, index, backfill UPDATE |

Generate the migration but do not apply it to any shared database — per `AGENTS.md`.

### Phase 2 — Application and API

| File | Change |
|---|---|
| `Core/Application/UseCases/Inventories/Dtos/InventoryItemDto.cs` | `IsNew` + AutoMapper projection |
| `Core/Application/UseCases/Inventories/Commands/MarkInventoryItemSeen/MarkInventoryItemSeenCommand.cs` | New command + handler |
| `Core/Application/Interfaces/Services/LL/IInventoryService.cs` | Add `MarkItemSeenAsync` passthrough |
| `Infrastructure/Service/Services.LL/Inventories/InventoryService.cs` | Passthrough implementation |
| `API/API.LL/Controllers/V1/InventoryController.cs` | `POST items/{itemInstanceId:guid}/seen` |

The `GetInventoryByIdQuery` read path needs no change beyond the DTO — but confirm the repository
`Include` chain for `GetInventoryByIdAsync` already loads `ItemInstance`, since `IsNew` reads
`AcquisitionSource` off it. It does today.

### Phase 3 — Frontend

| File | Change |
|---|---|
| `shared/models/inventoryItem.ts` | `isNew?: boolean` |
| `core/services/api/inventory/inventory.service.ts` | `markItemSeen(itemInstanceId)` |
| `core/services/api/inventory/inventory-state.service.ts` | `markSeen(itemInstanceId)` with optimistic local update; `newEquipmentCount` computed signal |
| `features/game/character/inventory/inventory.component.ts` | Call `state.markSeen(...)` from `selectInventoryItem` — browse mode only |
| `features/game/character/inventory/inventory.component.html` | Badge in the equipment list item, around line 280 next to the name |
| `features/game/character/inventory/inventory.component.scss` | Badge positioning |

Markup shape, matching the existing convention:

```html
<span *ngIf="inventoryItem.isNew" class="ll-badge ll-badge-accent inventory-new-badge">
  New
</span>
```

Two state-service details to get right:

- `load()` early-returns when items are already cached (`if (!force && this._items().length) return`),
  so navigating away and back will not refetch. The optimistic local update is therefore not a
  nicety — it is what keeps the badge from reappearing.
- Optimistic then fire-and-forget: flip `isNew` to `false` in the signal immediately, POST in the
  background, and leave it cleared on failure. Worst case the badge returns after a hard reload,
  which is a far better failure mode than a click that appears to do nothing.

### Phase 4 — Tests

- Backend, in `LL/tests/EssenceSystem.Tests` (new `InventoryNewItemTests.cs`, alongside the
  existing `InventoryTransferTests` / `InventoryRepositoryScrappingTests`):
  - a crafted item lands with `SeenAtUtc == null` and maps to `IsNew == true`
  - a looted/dungeon-reward item is never `IsNew`, even though its row is unseen
  - `MarkItemSeenAsync` stamps once, is idempotent on a second call, and returns false for
    another character's item
  - transferring a crafted item behaves as the decision below dictates
- Frontend: extend `inventory-state.service.spec.ts` (optimistic clear, no refetch) and
  `inventory.component.spec.ts` (badge renders, scrap-mode click does not clear).

### Phase 5 — Verification

`./build/run-tests.ps1` covers it; this touches no combat or balance content, so the balance
suite is not implicated.

## Risks and edge cases

**Traded and marketplace-bought crafted items will badge as New.** `AcquisitionSource` lives on
`ItemInstance` and stays `"crafting"` for the life of the item, so a crafted sword bought on the
marketplace arrives in a fresh unseen row and satisfies the predicate. Arguably correct — it *is*
new to that player — but it is a behaviour choice, not an accident. If you want it suppressed,
stamp `SeenAtUtc = UtcNow` when creating the recipient row in `TransferItemAsync`,
`AddItemToInventoryFromMarketPlace`, and `AddItemInstanceBackToInventory`. **This needs a decision
before Phase 1 lands**, because it determines whether those three methods are touched.

**Scrap mode.** `handleInventoryItemClick` toggles selection rather than inspecting when
`isScrapMode` is true. Clearing the marker there would wipe badges during a bulk-scrap sweep, so
gate the `markSeen` call on `isBrowseMode`.

**Guild-borrowed items.** Borrowed equipment surfaces through the same list. It is not crafted by
the borrower, but check whether the vault path preserves a crafting `AcquisitionSource` — if it
does, borrowed gear will badge. Same fix as the transfer case.

**Stackables.** Crafted equipment is non-stackable, so per-instance identity holds. Widening this
feature to resources later hits the fact that stacks have no per-unit identity and would need a
different model (per-item-base counter, or last-acquired timestamp). Out of scope now, but it is
the reason the crafting-only scope is the cheap one.

**Bulk crafts.** Crafting 20 items produces 20 badges and 20 individual POSTs as they are clicked.
Fine at this scale. If it grates, add a "Mark all seen" button backed by the batch endpoint.

## Rough effort

| Phase | Size |
|---|---|
| Domain + persistence + migration | Half a day, mostly the transfer-path audit |
| Application + API | 1–2 hours |
| Frontend | Half a day including styling |
| Tests | 2–3 hours |

Roughly 1.5–2 days end to end. The smallest shippable slice is Phases 1–3 with the transfer
question deferred, which is about a day.

## Possible follow-ups

- A count badge on the inventory sidebar nav so New is discoverable without opening the page —
  `newEquipmentCount` from Phase 3 already provides the number.
- Widen the predicate to loot and dungeon rewards once the crafted case has settled.
- A subtle entry animation or highlight ring on the first render of a new item.
