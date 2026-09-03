# Tempering Queue Performance Analysis

> Historical Alpha plan, superseded 3 September 2026. Crafting/gathering progression, queued tempering and their obsolete quest content have been removed. Conversion, refund and compatibility/backfill proposals below are not current implementation work. Shared numerical helpers with active consumers may remain. See the [post-Alpha cleanup](../../docs/design/equipment-post-alpha-cleanup.md) and [current quest flow](../../LEGENDSLEGACY_QUEST_FLOW.md) for supported behavior.

## Executive summary

The 200+ ms baseline latency observed when adding or removing Tempering queue items was primarily caused by redundant database round trips and oversized entity graphs, rather than missing indexes.

With a remotely hosted PostgreSQL database, the current four to five reads plus transaction locks, state-sync work, persistence, and commit can naturally exceed 200 ms.

The implementation described below replaces full inventory snapshots with versioned deltas, removes gameplay resolution from queue mutations, uses narrow enqueue reads, bulk-removes queue items, and makes removal and cancellation optimistic in the client.

## Baseline flow

| Operation | Main server work |
| --- | --- |
| Add item | Full inventory load, action and queue load, paused queue load, the same inventory item loaded again, state-sync work, and save |
| Remove item | Action and queue load, removed equipment graph load, full inventory graph load, current action loaded again, possible gameplay resolution, state-sync work, and save |
| Cancel queue | Loads the action, then performs roughly two queries per queued item before reloading the full inventory |

Every command also opens a transaction and acquires the per-character advisory lock in `TransactionBehavior`.

## Findings

### 1. Adding loads significantly more data than it needs

The start handler loads the character's entire inventory merely to validate one item:

- `LL/src/Core/Application/UseCases/CharacterActions/Commands/StartCraftingAction/StartCraftingActionCommand.cs:36`

The inventory query eagerly loads every item's:

- base modifiers;
- tool bonuses;
- instance modifiers;
- tool affixes;
- guild-vault relationship.

The query is defined at:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Inventories/InventoryRepository.cs:21`

The action repository subsequently:

- loads the action and active queue;
- unconditionally queries the paused queue;
- queries the selected inventory item again.

This work starts at:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/CharacterActions/CharacterActionRepository.cs:176`

Consequently, enqueue performs approximately four sequential read queries before synchronization and persistence.

### 2. Removing returns an unnecessarily expensive response

After removing one item, the handler reloads and maps the entire inventory:

- `LL/src/Core/Application/UseCases/Professions/Commands/RemoveCraftingQueueItem/RemoveCraftingQueueItemCommand.cs:50`

The client only needs the returned item and the queue or action delta. Returning every inventory item causes:

- a large SQL result;
- entity tracking for the complete graph;
- AutoMapper processing for every item;
- a larger JSON response;
- more Angular state replacement work.

The current working-tree version also explicitly loads four equipment collections with `AsSingleQuery()`:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Professions/Craftings/CraftingRepository.cs:35`

This ensures the equipment graph is available for mapping, but it can create a Cartesian multiplication between modifiers, bonuses, and affixes.

### 3. Removal can perform unrelated gameplay resolution

The removal handler calls `GetCharacterActionAsync`:

- `LL/src/Core/Application/UseCases/Professions/Commands/RemoveCraftingQueueItem/RemoveCraftingQueueItemCommand.cs:57`

This is not a passive read. It can resolve due Tempering, or combat when removing from a paused Tempering queue:

- `LL/src/Infrastructure/Service/Services.LL/CharacterActions/CharacterActionService.cs:82`

Resolution may load bonuses and progression, generate rewards and outbox messages, update professions, and perform additional state-sync work. Queue mutation latency can therefore depend on unrelated pending gameplay work.

### 4. DTO mapping can mutate every loaded equipment item

`EquipmentInstanceDto` calls `EquipmentStatModelMigrator.MigrateToCurrent` during mapping:

- `LL/src/Core/Application/UseCases/Equipments/Dtos/EquipmentInstanceDto.cs:47`

Because removal maps the entire tracked inventory, an old stat-model version can turn response construction into a bulk database update. Read and response mapping should not mutate persistent entities.

### 5. Adding triggers avoidable follow-up traffic

The enqueue endpoint returns only a Boolean. On success, Angular immediately starts polling and sends a resolve request:

- `LL/src/Presentation/ll/src/app/core/services/api/character-actions/character-actions.state.service.ts:294`

Because the enqueue response is not authoritative for inventory state, state synchronization can also publish an inventory invalidation while the transaction is still active.

### 6. Cancel-all scales linearly with queue size

`RemoveCraftingQueueItemsAsync` loops over queue item IDs and invokes the repository separately for every item:

- `LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/CraftingService.cs:220`

A queue of 20 items can therefore cause approximately 40 removal queries, in addition to initial and final snapshot queries.

### 7. The relevant queue indexes already exist

The queue has indexes for active and paused ordering:

- `(CraftingActionDetailsId, Position)`;
- `(PausedForCharacterId, Position)`;
- the equipment-instance foreign key;
- the queue item primary key.

This makes a missing index an unlikely primary explanation for the current latency.

## Implemented changes

### Versioned mutation response

Start, remove, and cancel now return one shared `TemperingQueueMutationResponseDto` containing only:

- inventory item IDs removed by enqueue;
- inventory items returned by remove or cancel;
- added or removed queue item IDs;
- a shallow action schedule snapshot.

The inventory scope is registered as an ordered delta. Angular applies it through `applyVersionedInventoryDelta`; a stale or out-of-order response triggers the existing authoritative inventory repair instead of replacing state with an incomplete delta.

### Narrow enqueue path

`StartCraftingActionCommand` now loads the selected inventory row and equipment base only. It passes that tracked row into the action mutation, eliminating both the full-inventory read and the action repository's second read of the same item.

The paused queue is queried only when there is no active Crafting action. The returned action schedule starts the poller at its persisted next-resolution boundary, so a successful enqueue no longer sends an immediate resolve request.

### Bulk remove and cancel path

The crafting repository accepts a set of queue IDs, or `null` for cancel-all. It validates ownership, loads the required returned equipment graphs in one queue query, calls `RemoveRange`, and updates the action schedule once.

The service adds the returned equipment rows back to the tracked inventory in memory and returns those rows directly for DTO mapping. It does not reload or map the complete inventory, and it does not call `GetCharacterActionAsync`, so the mutation cannot resolve unrelated due combat or Tempering work.

Cancel on an already-empty queue is idempotent and still returns the existing shallow action state.

### Optimistic client flow

Enqueue was already optimistic. Remove and cancel now also update the local queue and inventory immediately. The server delta replaces the optimistic inventory object when the request succeeds; the previous queue, inventory, and action snapshots are restored if it fails.

Failed enqueue requests remove only the optimistic queue item, reload inventory, and reconcile the current action. They no longer reset an action that was active before the request.

## Further recommendations

### Priority 1: Measure against PostgreSQL

The code removes the identified application-level amplification, but a local correctness suite cannot establish production p50 or p95 latency. Capture the measurements listed below against the deployed PostgreSQL topology before assigning a new latency SLO.

### Priority 2: Batch rapid enqueue requests

Each enqueue intentionally retains its transaction and per-character advisory lock. If players commonly add several items at once, a batch-enqueue endpoint can amortize lock acquisition, state synchronization, save, commit, and network latency across all selected items.

### Priority 3: Move stat-model migration out of DTO mapping

`EquipmentInstanceDto` still invokes `EquipmentStatModelMigrator.MigrateToCurrent`. The delta implementation limits this cost to returned items instead of the complete inventory, but response mapping should eventually be side-effect free. Move migration to an explicit backfill or mutation boundary.

## Original recommendations

### Use mutation deltas

Replace full inventory responses with small, authoritative mutation results.

Suggested enqueue result:

```text
EnqueueTemperingResult
  QueueItem
  RemovedInventoryItemId
  ActionSchedule
```

Suggested removal result:

```text
RemoveTemperingResult
  ReturnedInventoryItem
  RemovedQueueItemId
  ActionSchedule
```

Suggested cancel result:

```text
CancelTemperingResult
  ReturnedInventoryItems
  RemovedQueueItemIds
  ActionSchedule
```

The frontend already supports version-safe deltas through `applyVersionedInventoryDelta`:

- `LL/src/Presentation/ll/src/app/core/services/api/inventory/inventory-state.service.ts:176`

This change would eliminate the full-inventory query, full-inventory mapping, and oversized response payload. It would also allow enqueue to become an authoritative versioned response, avoiding the realtime inventory invalidation.

### Introduce narrow mutation queries

For enqueue, load only the requested inventory item and the fields needed for validation. Perform ownership validation and queue mutation from this single loaded graph.

Additionally:

- query the paused queue only when the current action is not already Crafting;
- avoid loading every queued equipment graph when only queue count and maximum position are required;
- do not validate through `GetInventoryByIdAsync`;
- consolidate related reads rather than attempting parallel queries on the same `DbContext`.

### Remove resolving reads from mutation handlers

Queue commands should not call `GetCharacterActionAsync`.

Return schedule and queue information directly from the mutation, or use a shallow, non-resolving projection. Keep actual Tempering and combat resolution in the explicit resolve flow.

This also makes the queue mutation's gameplay semantics predictable: removing an item will only remove an item.

### Bulk-cancel the queue

For cancel-all:

1. Load all requested queue items once.
2. Validate ownership once.
3. Remove the queue rows with `RemoveRange`.
4. Add all returned inventory rows in memory.
5. Persist everything with one `SaveChangesAsync`.

The command should remain protected by the per-character transaction lock; the optimization should shorten work inside the lock rather than remove concurrency protection.

### Move stat-model migration out of DTO mapping

Run stat-model migration through an explicit migration, backfill, or well-defined mutation boundary. AutoMapper response construction should be side-effect free.

This is a broader architectural improvement, but it directly affects removal because removal currently maps the entire tracked inventory.

### Improve perceived responsiveness

Single-item removal can be optimistic, matching the existing enqueue experience:

1. Immediately remove the item from the local queue.
2. Immediately restore its existing equipment object to local inventory.
3. Confirm the operation with the versioned server delta.
4. Roll back or reload if the command fails or the domain version is stale.

For rapid queueing, a batch enqueue endpoint would amortize transaction and lock overhead across several items.

## Recommended target flow

### Enqueue

```text
Client optimistic update
  -> Versioned enqueue command
  -> Character lock
  -> Narrow item and action mutation queries
  -> Save queue and inventory changes
  -> Return item/action delta with inventory revision
  -> Start polling at the next due boundary
```

### Remove

```text
Client optimistic update
  -> Versioned remove command
  -> Character lock
  -> Narrow queue item and equipment query
  -> Return equipment to inventory
  -> Save queue and inventory changes
  -> Return item/action delta with inventory revision
```

Neither flow should perform gameplay resolution or reload the complete inventory.

## Measurement plan

Before and after implementation, record:

- character-lock wait time;
- EF Core query count and cumulative database time;
- full-inventory query result row count;
- state-sync duration;
- `SaveChangesAsync` and commit duration;
- response mapping and serialization time;
- response payload size;
- end-to-end p50 and p95 latency.

Test with representative data sizes:

| Inventory size | Queue lengths |
| ---: | --- |
| 25 items | 1, 10, and 50 |
| 100 items | 1, 10, and 50 |
| 250 items | 1, 10, and 50 |

The target should be approximately two narrow reads for enqueue or removal, no gameplay resolution, no full inventory snapshot, and no immediate resolve request following enqueue.

## Testing recommendations

Add integration tests covering:

- enqueue and removal response deltas;
- version-conflict repair in the Angular inventory state;
- removal not resolving due Tempering or combat work;
- optimistic removal rollback after a failed request;
- concurrent enqueue and removal serialization;
- bulk cancellation with larger queues;
- database command counts for enqueue, single removal, and cancel-all.

SQLite behavior tests are useful for correctness, but PostgreSQL-backed measurements are needed to capture advisory-lock waits, network round trips, generated query plans, and realistic query latency.

## Verification performed

Backend verification:

```powershell
.\build\run-tests.ps1
```

All 1,442 backend tests passed.

Frontend verification:

```powershell
npm run build:development
npm exec -- ng test -- --watch=false --browsers=ChromeHeadless `
  --include='src/app/core/services/api/character-actions/character-actions.state.service.spec.ts' `
  --include='src/app/core/services/api/crafting/crafting.service.spec.ts'
```

The development build passed and all 28 affected Angular tests passed. The full Angular suite ran 534 tests; 531 passed and three unrelated `FirstPartyTourService` tests timed out.

This performance implementation introduces no migration, configuration, or deployment changes. The working tree contains a separate pre-existing migration and other unrelated changes that were preserved.
