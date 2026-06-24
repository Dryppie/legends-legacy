# Game SignalR game realtime Audit

## Scope

This audit covers the LL game realtime system only. Chat SignalR was not inspected for migration and was not modified.

## Current Game Hub

- `RealTime.LL.GameHub` at `/hub/game`
  - Adds the connection to `char:{characterId}` on connect.
  - Supports `SubscribeToWorld` and `SubscribeToGuild`.
  - Sends one client method: `ReceiveEvent(GameRealtimeEnvelope)`.
  - Carries both native events and compatibility events that still use the old `*Msg` payload names.

The old `/hub` game endpoint is no longer mapped by `API.LL`. Chat SignalR is separate and unchanged.

## Existing Frontend Services

- Compatibility: `GameEventService`
  - No longer creates its own SignalR connection.
  - Adapts `GameRealtimeConnection.events$` into the existing `event` and `eventEnvelope` signal API.
  - Lets existing market/guild/arena/character state services keep consuming old `*Msg` event names while the transport is game realtime.
- `GameRealtimeConnection`
  - Creates one SignalR connection to `/hub/game`.
  - Registers one `ReceiveEvent` callback once per connection instance.
  - Owns reconnect state and world/guild resubscription.
  - Exposes an event stream consumed by both `GameRealtimeEventRegistry` and the compatibility `GameEventService`.
- `GameRealtimeEventRegistry`
  - Central registration point for all migrated game handlers.
  - Throws if a duplicate handler is registered.
  - Batches `IdleCombatProcessed` with a zero-delay task and applies only the latest pending idle event.
- `GameRealtimeDiagnostics`
  - Bounded ring buffer of the last 100 game realtime events.
  - Records event name, timestamp, payload size estimate, handler start, handler duration, route, state update flag, and HTTP flag.
  - Detects main-thread stalls above 250ms and prints recent events.
  - Exposes `window.__gameSignalRDebug.printRecentEvents()`, `clear()`, `recentEvents()`, and `isConnected()` in development.

## Compatibility Event Names

These payload names are still used by existing application handlers, but they are now delivered through `GameHub` as `GameRealtimeEnvelope` messages.

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

## Migrated game realtime Event Names

- `DungeonRewardsClaimed`
  - Payload type: reward/loot.
  - Backend sender: `ClaimDungeonRewardsCommandHandler`.
  - Frontend receiver: `GameRealtimeEventRegistry`.
  - Updates `GameRealtimeStore` bounded reward/loot state.
- `InventorySnapshot`
  - Payload type: inventory update, currently full snapshot after reward claim.
  - Backend sender: `ClaimDungeonRewardsCommandHandler`.
  - Frontend receiver: `GameRealtimeEventRegistry`.
  - Calls `InventoryStateService.setInventory`.
- `CharacterSnapshot`
  - Payload type: character update, currently full character after reward claim.
  - Backend sender: `ClaimDungeonRewardsCommandHandler`.
  - Frontend receiver: `GameRealtimeEventRegistry`.
  - Calls `CharacterStateService.updateCharacter`.
- `LootReceived`
  - Payload type: loot delta.
  - Backend sender: `InventoryLootRewardWriter`.
  - Frontend receiver: `GameRealtimeEventRegistry`.
  - Calls `InventoryStateService.addOrIncrementMany` and updates bounded loot store.
- `IdleCombatProcessed`
  - Payload type: combat result/action update.
  - Backend sender: `GetCharacterActionQueryHandler` after idle combat processing.
  - Frontend receiver: `GameRealtimeEventRegistry`.
  - Calls `CharacterActionsStateService.applyRealtimeIdleCombat` through a batched handler.

## Risk Findings

- The likely freeze chain is: dungeon reward claim applies an HTTP inventory snapshot, legacy `LootReceivedMsg` can also apply a loot delta, then idle polling calls `GetCharacterAction`, which processes idle combat server-side and emits more legacy loot events. Those events update Angular signals and arrays while combat playback and polling effects are also active.
- Legacy `LootReceivedMsg` was the highest-risk migrated event because it can update inventory from both HTTP and SignalR for the same claim path.
- `CharacterActionsPollingService` intentionally stops and restarts polling, so duplicate poll subscriptions are guarded. The remaining risk was duplicate application of the same idle combat result through HTTP plus realtime; `CharacterActionsStateService` now dedupes equivalent action updates.
- `CharacterLevelUpMsg` still performs an HTTP reload through `CharacterStateService.refresh()`. It remains a compatibility event and should eventually become a native event with a small delta payload.
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

- `IGameEventPublisher` now publishes old `GameEventMsg` payloads through `GameHub.ReceiveEvent`.
- `RealTimeFacade` still initializes both the native registry and the compatibility adapter, but both share the same game realtime connection.
- Old `LootReceivedMsg` frontend inventory and loot-tracker handlers are disabled when game realtime is enabled.
- Dungeon reward claim uses game realtime as the source for inventory and character state when game realtime is enabled; the HTTP response still clears the active dungeon run.
- Compatibility game dispatch records diagnostics with a `compat:` event prefix.
- Chat SignalR remains untouched.
