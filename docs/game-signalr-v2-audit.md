# Game SignalR v2 Audit

## Scope

This audit covers the LL game realtime system only. Chat SignalR was not inspected for migration and was not modified.

## Current Game Hub

- `RealTime.LL.GameHubV2` at `/hub/game/v2`
  - Adds the connection to `char:v2:{characterId}` on connect.
  - Supports `SubscribeToWorld` and `SubscribeToGuild`.
  - Sends one client method: `ReceiveEvent(GameRealtimeEnvelopeV2)`.
  - Carries both native v2 events and compatibility events that still use the old `*Msg` payload names.

The old `/hub` game endpoint is no longer mapped by `API.LL`. Chat SignalR is separate and unchanged.

## Existing Frontend Services

- Compatibility: `GameEventService`
  - No longer creates its own SignalR connection.
  - Adapts `GameRealtimeConnectionV2.events$` into the existing `event` and `eventEnvelope` signal API.
  - Lets existing market/guild/arena/character state services keep consuming old `*Msg` event names while the transport is v2.
- v2: `GameRealtimeConnectionV2`
  - Creates one SignalR connection to `/hub/game/v2`.
  - Registers one `ReceiveEvent` callback once per connection instance.
  - Owns reconnect state and world/guild resubscription.
  - Exposes an event stream consumed by both `GameRealtimeEventRegistryV2` and the compatibility `GameEventService`.
- v2: `GameRealtimeEventRegistryV2`
  - Central registration point for all migrated game handlers.
  - Throws if a duplicate handler is registered.
  - Batches `IdleCombatProcessedV2` with a zero-delay task and applies only the latest pending idle event.
- v2: `GameRealtimeDiagnosticsV2`
  - Bounded ring buffer of the last 100 game realtime events.
  - Records event name, timestamp, payload size estimate, handler start, handler duration, route, state update flag, and HTTP flag.
  - Detects main-thread stalls above 250ms and prints recent events.
  - Exposes `window.__gameSignalRDebug.printRecentEvents()`, `clear()`, `recentEvents()`, and `isConnected()` in development.

## Compatibility Event Names

These payload names are still used by existing application handlers, but they are now delivered through `GameHubV2` as `GameRealtimeEnvelopeV2` messages.

- `LootReceivedMsg`
  - Payload: reward/loot delta (`InventoryItemDto[]`).
  - Backend sender: `InventoryLootRewardWriter`, plus `LootGeneratedEventHandler`.
  - Frontend receivers: `InventoryStateService`, `LootTrackerComponent`.
  - Updates inventory arrays and loot UI arrays.
  - Risk: duplicated with dungeon claim HTTP response; can update large inventory arrays and trigger signal effects.
- `SoulstoneDropMsg`
  - Payload: character currency delta.
  - Backend sender: `SoulstoneDropEventHandler`.
  - Frontend receiver: `CharacterStateService`.
  - Updates current character signal.
- `CharacterLevelUpMsg`
  - Payload: character level/experience delta.
  - Backend sender: `CharacterLevelUpEventHandler`.
  - Frontend receiver: `CharacterStateService`.
  - Updates current character and reloads character overview through HTTP.
- `ArenaBattleCompletedMsg`
  - Payload: combat result summary and rating deltas.
  - Backend sender: `ArenaBattleCompletedEventHandler`.
  - Frontend receiver: `ColosseumStateService`.
  - Updates colosseum records and rating state.
- Marketplace events:
  - `MarketListingSoldMsg`, `MarketListingCreatedMsg`, `MarketListingCanceledMsg`.
  - Frontend receiver: `MarketPlaceStateService`.
  - Mutates marketplace listing arrays.
- Guild events:
  - `GuildBuildingUpgradedMsg`, `GuildApplicationMsg`, `GuildInviteReceivedMsg`, `GuildInviteRejectedMsg`, `GuildApplicationRejectedMsg`, `GuildStateChangedMsg`, `GuildMembershipChangedMsg`, `GuildDisbandedMsg`, `GuildDirectoryChangedMsg`.
  - Frontend receiver: `GuildStateService`.
  - Some handlers reload guild data through HTTP.

## Migrated v2 Event Names

- `DungeonRewardsClaimedV2`
  - Payload type: reward/loot.
  - Backend sender: `ClaimDungeonRewardsCommandHandler`.
  - Frontend receiver: `GameRealtimeEventRegistryV2`.
  - Updates `GameRealtimeStoreV2` bounded reward/loot state.
- `InventorySnapshotV2`
  - Payload type: inventory update, currently full snapshot after reward claim.
  - Backend sender: `ClaimDungeonRewardsCommandHandler`.
  - Frontend receiver: `GameRealtimeEventRegistryV2`.
  - Calls `InventoryStateService.setInventory`.
- `CharacterSnapshotV2`
  - Payload type: character update, currently full character after reward claim.
  - Backend sender: `ClaimDungeonRewardsCommandHandler`.
  - Frontend receiver: `GameRealtimeEventRegistryV2`.
  - Calls `CharacterStateService.updateCharacter`.
- `LootReceivedV2`
  - Payload type: loot delta.
  - Backend sender: `InventoryLootRewardWriter`.
  - Frontend receiver: `GameRealtimeEventRegistryV2`.
  - Calls `InventoryStateService.addOrIncrementMany` and updates bounded v2 loot store.
- `IdleCombatProcessedV2`
  - Payload type: combat result/action update.
  - Backend sender: `GetCharacterActionQueryHandler` after idle combat processing.
  - Frontend receiver: `GameRealtimeEventRegistryV2`.
  - Calls `CharacterActionsStateService.applyRealtimeIdleCombat` through a batched handler.

## Risk Findings

- The likely freeze chain is: dungeon reward claim applies an HTTP inventory snapshot, legacy `LootReceivedMsg` can also apply a loot delta, then idle polling calls `GetCharacterAction`, which processes idle combat server-side and emits more legacy loot events. Those events update Angular signals and arrays while combat playback and polling effects are also active.
- Legacy `LootReceivedMsg` was the highest-risk migrated event because it can update inventory from both HTTP and SignalR for the same claim path.
- `CharacterActionsPollingService` intentionally stops and restarts polling, so duplicate poll subscriptions are guarded. The remaining risk was duplicate application of the same idle combat result through HTTP plus v2; `CharacterActionsStateService` now dedupes equivalent action updates.
- `CharacterLevelUpMsg` still performs an HTTP reload through `CharacterStateService.refresh()`. It remains a compatibility event and should eventually become a native v2 event with a small delta payload.
- Guild and marketplace handlers remain compatibility handlers and may reload or mutate arrays.

## Remaining Compatibility Events

- `SoulstoneDropMsg`
- `CharacterLevelUpMsg`
- `ArenaBattleCompletedMsg`
- `MarketListingSoldMsg`
- `MarketListingCreatedMsg`
- `MarketListingCanceledMsg`
- `GuildBuildingUpgradedMsg`
- `GuildApplicationMsg`
- `GuildInviteReceivedMsg`
- `GuildInviteRejectedMsg`
- `GuildApplicationRejectedMsg`
- `GuildStateChangedMsg`
- `GuildMembershipChangedMsg`
- `GuildDisbandedMsg`
- `GuildDirectoryChangedMsg`

## Migration Notes

- `IGameEventPublisher` now publishes old `GameEventMsg` payloads through `GameHubV2.ReceiveEvent`.
- `RealTimeFacade` still initializes both the native v2 registry and the compatibility adapter, but both share the same v2 connection.
- Old `LootReceivedMsg` frontend inventory and loot-tracker handlers are disabled when v2 is enabled.
- Dungeon reward claim uses v2 as the source for inventory and character state when v2 is enabled; the HTTP response still clears the active dungeon run.
- Compatibility game dispatch records diagnostics with a `compat:` event prefix.
- Chat SignalR remains untouched.
