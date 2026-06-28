# Game State Management & Realtime Implementation Plan

## 1. Executive Summary

The main game application in `LL/` has a C#/.NET API, Angular frontend, MediatR command/query handlers, feature state services, and an active SignalR realtime layer. The primary game API maps `GameHub` at `/hub`, publishes typed hub messages through `IGameEventPublisher`, and the Angular app has a central `GameEventService` that receives a `Publish` event.

This document was originally written before the realtime work began. Since then, several vertical slices have been implemented: Colosseum command-response cleanup, connection status and reconnect handling, marketplace listing updates, guild invite/application/state updates, character soulstone and level-up updates, arena battle completion updates, and a reusable client-side notification service with a shared notification indicator component.

What is missing is a consistent game-wide rule for which channel owns each state change. REST command responses are inconsistent: dungeons already return useful updated state for start/progress actions, while Colosseum, character actions, equipment, guild, market, and several essence actions often return `bool` or message-only DTOs. The frontend then compensates by manually decrementing counters, mutating inventory/equipment/listing/guild state, polling snapshots, or refreshing after commands.

Recommended direction remains: standardize REST command responses first, then add realtime only for out-of-band updates. SignalR should not be used to echo every REST-triggered state mutation back to the same caller. If a player starts a Colosseum battle and spends a ticket, the REST response should include the updated ticket state; the caller should not wait for a SignalR echo and should not blindly decrement locally.

First implementation milestone status: completed. Colosseum now has a signal-based `ColosseumStateService`, `StartArenaBattle` returns an authoritative response containing battle and ticket state, and the frontend no longer relies on local ticket decrementing for the caller state.

Current highest-value realtime improvement: every realtime consumer now has an explicit recovery rule. Inventory, dungeon, character, colosseum, marketplace, and guild refresh after `GameEventService.reconnectCount()` changes; future realtime consumers should follow the same loaded-snapshot recovery pattern.

First reconnect recovery slice status: implemented. `MarketplaceStateService` now refreshes listings after reconnect once marketplace state has been loaded, and `GuildStateService` now refreshes guild/invite/directory snapshots after reconnect once guild state has been loaded.

Current command-response cleanup status: implemented for the highest-risk caller-owned state gaps found in this pass. Marketplace create/cancel, character action start/stop, and equipment scrap now return authoritative result DTOs instead of requiring component-level guesses. Guild donation/application/rejection flows now prefer snapshot refreshes over hand-mutated local state until broader guild result DTOs are worth designing.

## 1.1 Progress Update Since Original Analysis

| Area | Status | What changed | Remaining concern |
|---|---|---|---|
| Game SignalR lifecycle | Improved | `GameEventService` now exposes `connectionStatus` and `reconnectCount`, tracks requested world/guild subscriptions, and resubscribes audiences after reconnect. | Reconnect recovery still depends on each feature store deciding to refresh its own snapshot. |
| Hub authorization and guild subscriptions | Improved | `GameHub` is `[Authorize]`, supports `SubscribeToWorld`, and authorizes `SubscribeToGuild` by checking the caller's guild before joining `guild:{guildId}`. | Group joins are now safer, but missed guild events still require REST resync after reconnect. |
| Envelope metadata | Improved | `GameEventPublisher` includes `UpdateId` and `OccurredAt`; `GameEventService` now exposes `eventEnvelope` signals and centrally suppresses repeated `UpdateId` deliveries. | No ordering/sequence policy exists yet. |
| Colosseum REST state | Completed | `StartArenaBattle` returns battle plus `ArenaTicketStatus`; `ColosseumStateService` owns tickets, opponents, rankings, match history, arena rating patching, and reconnect refresh. | Arena completion pushes are useful for out-of-band rating/history updates, but broader leaderboard live state is still snapshot-driven. |
| Marketplace realtime | Completed | Created, cancelled, and sold listing messages are mapped end-to-end. Marketplace state applies listing changes, seller cinder updates, reconnect snapshot recovery, and buyout/create/cancel command result DTOs. | Future work is mostly UX/notification durability rather than command state correctness. |
| Guild realtime | Improved | Guild application, invite, rejection, state, membership, disband, building, and directory messages are mapped end-to-end. Guild state refreshes relevant snapshots, resyncs after reconnect, avoids local donation/application/rejection guesses, and sidebar notifications are centralized. | Many guild commands still return `bool`; the frontend now mitigates this with authoritative refreshes. |
| Character progression realtime | Improved | Soulstone drops and level-ups update current character state through SignalR, character state refreshes after reconnect, and event consumers dedupe by `UpdateId`. | Derived stats from essence-driven character changes still rely on REST refreshes. Soulstone upgrade commands are intentionally deferred. |
| Inventory realtime | Improved | Loot pushes update inventory with `UpdateId` duplicate suppression; inventory refreshes after reconnect; scrap equipment, dungeon reward claim, marketplace flows, guild donation, and crafting queue removal now apply server-owned inventory state. | Essence flows still lean on archive/loadout refreshes. |
| Character actions | Improved | Start combat, crafting, gathering, stop, and crafting queue removal now return/apply action snapshots so the caller uses server action state immediately. | Ongoing idle progress is still REST-poll driven because the processing is request-driven today. |
| Notifications | Improved | A generic `NotificationService` supports `sidebar`, `header`, and `page` surfaces; the sidebar uses generic counts; the badge design is now a shared `NotificationIndicatorComponent`. | Notifications are in-memory only and not backed by a server notification inbox/history. |
| Component ownership | Improved | Colosseum and marketplace have stronger feature state services than the original analysis described. | Some components still perform direct state nudges after commands, especially around marketplace, equipment, and inventory-consuming flows. |

## 1.2 Current Realtime State Management Gaps

- Reconnect recovery is now uniform across the current SignalR consumers. New realtime consumers should refresh their loaded snapshot after reconnect.
- Event deduplication now uses envelope `UpdateId` centrally and in the active feature stores. The remaining hardening gap is ordering/sequence policy, not basic duplicate suppression.
- Marketplace world broadcasts update all online clients, and buyout/create/cancel responses now update the caller's inventory/listing/cinder state.
- Guild realtime is useful, but the client often responds by reloading broad snapshots. This is safe and intentional until guild commands get compact result DTOs.
- Equipment equip/unequip already return `EquipmentChangeResponseDto`; inventory scrap, dungeon rewards, guild donation refresh, marketplace flows, and crafting queue removal now avoid local inventory guesses. Remaining inventory-adjacent gaps are essence flows that still lean on snapshot refreshes.
- Character action start/stop now return `CharacterActionDto?`; ongoing idle combat/crafting progress remains REST-poll driven. That is acceptable while processing is request-driven, but if these become background workers they will need pushed completion/progress updates plus reconnect snapshots.
- Notifications are client-side presentation state, not durable server state. A future news tab, notification center, or offline notification history needs a backend notification model and REST snapshot.

## 2. Current Backend Findings

| Area | Current Implementation | File(s) | Notes |
|---|---|---|---|
| Main API boundary | ASP.NET Core API project targets `net10.0`; controllers use MVC and MediatR. | `LL/src/API/API.LL/API.LL.csproj`, `LL/src/API/API.LL/Controllers/BaseController.cs` | `BaseController` is `[Authorize]`, `[ApiController]`, versioned, and reads `CurrentCharacterGuid` from the `CharacterId` claim. |
| API response wrapper | `Response<T>` has `IsSuccess`, `Data`, and `ErrorMessage`; `ResponseResultFilter` unwraps successful responses to `200` body data and failures to `400` error text. | `LL/src/Core/Common/Primitives/Response.cs`, `LL/src/API/API.LL/Filters/ResponseResultFilter.cs`, `LL/src/API/API.LL/DependencyInjection.cs` | Frontend API services receive the unwrapped `Data`, not `{ isSuccess, data }`. |
| SignalR registration | Main API calls `builder.Services.AddSignalR().AddJsonProtocol(...)`; maps `GameHub` at `/hub`; registers realtime services with `AddRealTime()`. | `LL/src/API/API.LL/Program.cs`, `LL/src/Infrastructure/RealTime/RealTime.LL/DependencyInjection.cs` | `AddRealTime()` registers `IGameEventPublisher` as scoped `GameEventPublisher`. |
| SignalR hub | `GameHub : Hub<IGameClient>` is `[Authorize]`, adds connections to `char:{CharacterId}`, supports world subscriptions, and authorizes guild subscriptions before joining `guild:{guildId}`. | `LL/src/Infrastructure/RealTime/RealTime.LL/GameHub.cs` | Hub is thin, which is good. Guild group membership is checked server-side. |
| SignalR client contract | `IGameClient` has one method, `Task Publish(GameEventEnvelope e)`. | `LL/src/Infrastructure/RealTime/RealTime.LL/IGameClient.cs` | Contract is generic and currently not feature-specific. |
| SignalR envelope | `GameEventEnvelope` contains `UpdateId`, `OccurredAt`, `Event`, and `Payload`. | `LL/src/Infrastructure/RealTime/RealTime.LL/GameEventEvelope.cs` | File name is misspelled as `GameEventEvelope.cs`. Envelope now has idempotency metadata; sequence/version/source metadata remain future hardening options. |
| Realtime publisher | `GameEventPublisher` sends `GameEventEnvelope` to character, guild, or world audiences using SignalR groups. | `LL/src/Infrastructure/RealTime/RealTime.LL/GameEventPublisher.cs`, `LL/src/Core/Application/Interfaces/WebSockets/IGameEventPublisher.cs`, `LL/src/Core/Application/Interfaces/WebSockets/Audience.cs` | Character, guild, and world audiences are now backed by hub subscription support. |
| Realtime DTOs | `GameEventMsg` includes market, guild, and world records; `LootReceivedMsg`, character progression messages, and arena completion messages are separate active contracts. | `LL/src/Core/Application/WebSockets/Contracts/GameEventMsg.cs`, `LL/src/Core/Application/WebSockets/Contracts/LootReceivedMsg.cs` | Frontend registry now includes loot, market, guild, character progression, and Colosseum event names. |
| Internal application events | MediatR `INotification` events exist for user creation, character creation, level-up, loot, soulstone drops, and Colosseum battle completion. | `LL/src/Core/Application/UseCases/Inventories/Events/LootGeneratedEvent.cs`, `LL/src/Core/Application/UseCases/Soulstones/Events/SoulstoneDropEvent.cs`, `LL/src/Core/Application/UseCases/Colosseum/Events/ArenaBattleCompletedEvent.cs`, `LL/src/Core/Application/UseCases/Characters/Events/CharacterLevelUpEvent.cs` | These are internal events. Only loot currently becomes SignalR through `LootGeneratedEventHandler`. |
| Loot realtime bridge | `LootGeneratedEventHandler` adds loot to inventory and publishes `LootReceivedMsg` to the character group. | `LL/src/Core/Application/UseCases/Inventories/EventHandlers/LootGeneratedEventHandler.cs` | This is a real out-of-band candidate when loot is generated by idle/background-like flows. It can duplicate REST response updates if used indiscriminately for direct commands later. |
| Domain-to-client mapper | `DomainToClientMapper` maps `LootGeneratedEvent` to `LootReceivedMsg`. | `LL/src/Core/Application/Common/Mappings/DomainToClientMapper.cs` | Mapper exists but is not used in the inspected publisher path; the loot handler maps manually. |
| Authentication setup | JWT bearer reads tokens from `DevAuth` in debug or `AccessToken` cookie; validates lifetime and requires user/character claims for non-anonymous endpoints. | `LL/src/API/API.LL/Program.cs` | `ValidateIssuer` and `ValidateAudience` are false with comments saying they need to be true. SignalR token extraction currently relies on the same cookie path because frontend uses `withCredentials: true`. |
| Dungeons REST snapshots | `GetActiveDungeon`, `GetAvailableDungeons`, and `GetDungeonRecords/{familyId}` return snapshot/list DTOs. | `LL/src/API/API.LL/Controllers/V1/DungeonController.cs`, `LL/src/Core/Application/UseCases/Dungeons/Queries/GetDungeonRun/GetDungeonRunQuery.cs`, `LL/src/Core/Application/UseCases/Dungeons/Dtos/DungeonRunDto.cs` | `DungeonRunDto` is a strong replacement snapshot for active run state. |
| Dungeons REST commands | `StartDungeon` returns `Response<DungeonRunDto>`; `ExecuteAction/{runId}` returns `Response<ExecuteDungeonActionResponseDto>` with run, outcome, optional combat session, and message. | `LL/src/Core/Application/UseCases/Dungeons/Commands/StartDungeonRun/StartDungeonRunCommand.cs`, `LL/src/Core/Application/UseCases/Dungeons/Commands/ExecuteDungeonAction/ExecuteDungeonActionCommand.cs`, `LL/src/Core/Application/UseCases/Dungeons/Dtos/ExecuteDungeonActionResponseDto.cs` | This is the best current example of REST command responses carrying authoritative changed state. |
| Dungeon reward/dismiss commands | `ClaimDungeonRewards` returns inventory, claimed loot, character, and active-run state; `DismissFailedDungeonRun` returns active-run state. | `LL/src/Core/Application/UseCases/Dungeons/Commands/ClaimDungeonRewards/ClaimDungeonRewardsCommand.cs`, `LL/src/Core/Application/UseCases/Dungeons/Commands/DismissFailedDungeonRun/DismissFailedDungeonRunCommand.cs` | Frontend no longer clears these flows blindly. |
| Colosseum command | `StartArenaBattle` returns a battle response that includes combat result and authoritative arena ticket status. | `LL/src/API/API.LL/Controllers/V1/ColosseumController.cs`, `LL/src/Core/Application/UseCases/Colosseum/Commands/StartArenaBattle/StartArenaBattleCommand.cs`, `LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs` | The caller no longer decrements tickets locally. |
| Colosseum snapshots | `GetArenaTicketStatus`, `GetArenaOpponents`, `GetRankings`, and `GetColosseumMatchResults` exist. | `LL/src/API/API.LL/Controllers/V1/ColosseumController.cs`, `LL/src/Core/Application/UseCases/Colosseum/Dtos/ArenaTicketStatusDto.cs` | Ticket regeneration is calculated when `GetArenaTicketStatusAsync` is called, not by a background worker. |
| Colosseum internal side effects | `ArenaBattleCompletedEventHandler` calculates ratings, saves match result, and publishes arena completion updates. | `LL/src/Core/Application/UseCases/Colosseum/EventHandlers/ArenaBattleCompletedEventHandler.cs` | SignalR is used for out-of-band rating/history refreshes; REST remains the command source for caller ticket state. |
| Idle combat / action processing | `GetCharacterActionAsync` calculates idle combat/crafting progress during REST polling and updates `CharacterAction.UpdatedAt`. | `LL/src/Infrastructure/Service/Services.LL/CharacterActions/CharacterActionService.cs`, `LL/src/Infrastructure/Service/Services.LL/CharacterActions/CombatService.cs` | This is not a background worker today; state changes happen when the client polls `CharacterActions`. |
| Character action commands | Start/stop action endpoints return `Response<CharacterActionDto?>`; crafting queue removal returns inventory plus current action. | `LL/src/API/API.LL/Controllers/V1/CharacterActionsController.cs`, `LL/src/API/API.LL/Controllers/V1/CraftingController.cs`, `LL/src/Core/Application/UseCases/CharacterActions/Commands/StartCombatAction/StartCombatActionCommand.cs` | Frontend applies the returned action snapshot immediately, then continues normal polling for request-driven idle progress. |
| Market commands | Buyout, create, and cancel return command result DTOs that include the caller-owned inventory/currency/listing state needed by the UI. | `LL/src/API/API.LL/Controllers/V1/MarketPlaceController.cs`, `LL/src/Core/Application/UseCases/MarketPlaces/Commands/BuyoutMarketPlaceListing/BuyoutMarketPlaceListingCommand.cs`, `LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs` | Seller notification/update is handled through SignalR. |
| Equipment commands | Equip/unequip endpoints return `Response<EquipmentChangeResponseDto>`. | `LL/src/API/API.LL/Controllers/V1/EquipmentController.cs`, `LL/src/Core/Application/UseCases/Equipments/Commands/EquipEquipment/EquipEquipmentCommand.cs`, `LL/src/Core/Application/UseCases/Equipments/Commands/UnequipEquipment/UnequipEquipmentCommand.cs` | Equipment slots and inventory return together; derived character stats may still need refresh where displayed. |
| Essence commands | Absorb, dismantle, spend dust, ascend, and evolve return mutation DTOs and update archive/inventory state; favorite and loadout activate/delete still use message responses plus refresh. | `LL/src/API/API.LL/Controllers/V1/EssenceController.cs`, `LL/src/Presentation/ll/src/app/core/services/api/essences/essence-state.service.ts` | Good snapshot APIs exist: archive and loadouts. Remaining message-only responses are safe because the store refreshes snapshots. |
| Guild commands | Most guild state mutations return `Response<bool>`. | `LL/src/API/API.LL/Controllers/V1/GuildController.cs`, `LL/src/Presentation/ll/src/app/core/services/api/guild/guild-state.service.ts` | Guild membership/activity/invites are good group-scoped realtime candidates, but REST responses should still update the caller. |
| Background workers | No active `BackgroundService`, `IHostedService`, or scheduled Quartz job was found in `LL/src`; Quartz projects/classes are present but empty. | `LL/src/Infrastructure/Service/Services.Quartz/Scheduler/SchedulerService.cs`, `LL/src/Infrastructure/Service/Services.Quartz/Quartz/QuartzScheduler.cs` | Out-of-band updates are currently limited; most “idle” processing is request/poll driven. |
| Chat service | `LL-Chat` is independently deployable and has its own `ChatHub`, SignalR setup, groups, and client receive methods. | `LL-Chat/API/API.Chat/Program.cs`, `LL-Chat/API/API.Chat/Hubs/ChatHub.cs`, `LL-Chat/API/API.Chat/Hubs/Interfaces/IChatClient.cs` | Chat uses SignalR as its primary interaction model, which is appropriate for chat. It should remain separate from game state realtime decisions. |

## 3. Current Frontend Findings

| Area | Current Implementation | File(s) | Notes |
|---|---|---|---|
| Angular app | Main frontend lives under `LL/src/Presentation/ll`; package includes `@microsoft/signalr` and msgpack package. | `LL/src/Presentation/ll/package.json` | Angular state is a mix of signals, RxJS Observables, and older `BehaviorSubject`s. |
| API wrapper | `ApiService` exposes `get/post/put/patch/delete` returning RxJS `Observable<any>`. | `LL/src/Presentation/ll/src/app/core/services/api/api.service.ts` | Since backend unwraps `Response<T>`, service methods generally type the unwrapped DTO directly. |
| Central game SignalR client | `GameEventService` owns one `HubConnection`, connects to `${environment.apiBaseUrl}/hub`, uses cookies with `withCredentials: true`, registers `Publish`, exposes payload and envelope signals, tracks connection status, and increments reconnect counters. | `LL/src/Presentation/ll/src/app/core/services/real-time/game-event.service.ts` | This is now the shared realtime foundation. Sequence ordering remains future hardening. |
| Realtime lifecycle facade | `RealTimeFacade` connects when `AuthService.isAuthenticated()` is true and disconnects otherwise. It is initialized through `APP_INITIALIZER`. | `LL/src/Presentation/ll/src/app/core/services/real-time/real-time-facade.ts`, `LL/src/Presentation/ll/src/app/app.config.ts` | Character group join is automatic on the server; world/guild subscriptions are available through the game hub. |
| Game realtime event map | Frontend event map contains loot, market, character progression, arena completion, and guild event names plus typed envelope signals. | `LL/src/Presentation/ll/src/app/core/services/real-time/game-event/game-event.map.ts`, `LL/src/Presentation/ll/src/app/core/services/real-time/**` | New backend realtime contracts should be added here before feature stores consume them. |
| Inventory state | Signal-based `InventoryStateService` loads inventory snapshot, applies `LootReceivedMsg`, suppresses duplicate loot by `UpdateId`, supports exact item replacement/removal, and applies scrap response snapshots. | `LL/src/Presentation/ll/src/app/core/services/api/inventory/inventory-state.service.ts` | Some older feature flows still use manual helpers, but the highest-risk scrap path no longer guesses. |
| Dungeon state | Signal-based `DungeonStateService` loads active/available dungeon snapshots; applies `startDungeon` and `executeDungeonAction` REST responses directly. | `LL/src/Presentation/ll/src/app/core/services/api/dungeon/dungeon-state.service.ts`, `LL/src/Presentation/ll/src/app/core/services/api/dungeon/dungeon.service.ts` | This is the strongest existing frontend pattern for applying command responses. Reward claim/dismiss still clear local state after `void` response. |
| Colosseum state | `ColosseumStateService` owns ticket status, opponents, rankings, match results, battle responses, arena completion pushes, and reconnect refresh. | `LL/src/Presentation/ll/src/app/core/services/api/colosseum/colosseum-state.service.ts`, `LL/src/Presentation/ll/src/app/core/services/api/colosseum/colosseum.service.ts` | Colosseum now follows the feature-store pattern. |
| Colosseum component state | Colosseum components read from `ColosseumStateService` and no longer locally decrement arena tickets. | `LL/src/Presentation/ll/src/app/features/game/city/colosseum/**` | Battle command response owns ticket state; SignalR is for out-of-band completion/rating/history updates. |
| Combat state | `CombatStateService` uses Angular signals keyed by `BattleType` and is driven by `CombatService`/playback strategies. | `LL/src/Presentation/ll/src/app/core/state/combat-state/combat-state.service.ts` | Useful feature-store pattern, but it is client-side playback state, not server realtime state. |
| Character action state | `CharacterActionsStateService` applies `CharacterActionDto?` returned by start/stop commands, then keeps polling for request-driven idle combat/crafting progress. | `LL/src/Presentation/ll/src/app/core/services/api/character-actions/character-actions.state.service.ts`, `LL/src/Presentation/ll/src/app/core/services/api/character-actions/character-actions.service.ts` | Idle combat/crafting updates are currently recovered through REST polling snapshots. |
| Character state | `CharacterStateService` uses signals and refreshes character overview when current character changes. | `LL/src/Presentation/ll/src/app/core/services/api/character/character-state.service.ts` | Good snapshot-style store; command-response application methods are minimal. |
| Marketplace state | Signal-based `MarketplaceStateService` loads listings, consumes market SignalR envelopes, applies create/cancel command responses, and refreshes after reconnect. | `LL/src/Presentation/ll/src/app/core/services/api/market-place/market-place-state.service.ts`, `LL/src/Presentation/ll/src/app/features/game/city/market-place/**` | Buyout is the main remaining buyer-side command-response cleanup. |
| Guild state | Signal-based `GuildStateService` consumes guild SignalR envelopes, refreshes authoritative snapshots after guild updates/reconnect, and avoids guessed local mutation for donation/application/rejection flows. | `LL/src/Presentation/ll/src/app/core/services/api/guild/guild-state.service.ts` | Broad guild refreshes are safe but noisier than compact command result DTOs. |
| Equipment state | Signal-based `EquipmentStateService` applies `EquipmentChangeResponseDto` from equip/unequip commands. | `LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-state.service.ts` | Derived character stats may need refresh where shown. |
| Essence state | Signal-based `EssenceStateService` loads archive/loadouts snapshots and refreshes after many commands; absorb/dismantle also manually decrement inventory before refresh. | `LL/src/Presentation/ll/src/app/core/services/api/essences/essence-state.service.ts`, `LL/src/Presentation/ll/src/app/core/services/api/essences/essences.service.ts` | Snapshot refresh is safe but chatty. Command result DTOs can reduce drift and refetches. |
| Chat SignalR | `ChatService` has a separate SignalR connection to the chat service and uses hub methods like `JoinGuild`, `Send`, and `Receive`. | `LL/src/Presentation/ll/src/app/core/services/ll-chat/chat-service/chat.service.ts` | Chat is separate from game state and should not drive game store design except as SignalR operational precedent. |

## 4. Current State Change Flow

| Flow | Trigger | Backend Path | Frontend Path | Current Delivery Method | Notes |
|---|---|---|---|---|---|
| Dungeon run started | Player clicks start dungeon. | `DungeonController.StartDungeon` -> `StartDungeonRunCommand` -> `IDungeonRunService.StartRunAsync` -> `DungeonRunDto`. | `DungeonStateService.startDungeon()` -> `_activeDungeon.set(run)`. | REST command response. | Good current pattern. Caller applies authoritative run DTO. |
| Dungeon action executed | Player chooses fight/continue/leave/event action. | `DungeonController.ExecuteAction` -> `ExecuteDungeonActionCommand` -> `DungeonRunService.ExecuteActionAsync` -> `ExecuteDungeonActionResponseDto`. | `DungeonStateService.executeAction()` applies `result.run`, `result.outcome`, `result.combatSession`, `result.message`. | REST command response. | Good current pattern. Combat session starts client playback. |
| Dungeon rewards claimed | Player claims completed dungeon rewards. | `DungeonController.ClaimDungeonRewards` -> `ClaimDungeonRewardsCommand` -> `IDungeonRunService.ClaimRewardsAsync`. | `DungeonStateService.claimDungeonRewards()` clears `_activeDungeon`, reloads available dungeons. | REST boolean plus local mutation. | Response does not include inventory/resource changes or replacement dungeon state. |
| Colosseum battle started | Player challenges opponent. | `ColosseumController.StartArenaBattle` -> `StartArenaBattleCommand` -> `ColosseumService.StartArenaBattle()` spends ticket and returns battle plus ticket status. | `ColosseumStateService.startBattle()` applies authoritative ticket state and starts combat playback. | REST command response. | Completed pattern: no SignalR echo is required for caller ticket state. |
| Arena ticket regeneration | User loads ticket status. | `GetArenaTicketsQuery` -> `ColosseumService.GetArenaTicketStatusAsync()` calculates regeneration and updates entity. | `ColosseumService.getArenaTicketStatus()` updates `BehaviorSubject`. | REST snapshot. | Not a background timer today. Could become SignalR later if server-side regen worker is added. |
| Idle combat progress | Polling or action load. | `CharacterActionsController.Get` -> `GetCharacterActionQuery` -> `CharacterActionService.GetCharacterActionAsync()` -> `PerformIdleCombatAsync()`. | `CharacterActionsStateService` polling sets `_currentAction`; action handlers update combat UI. | REST polling snapshot. | Despite the name “idle,” it is request-driven today. |
| Loot generated event | Internal MediatR event. | `LootGeneratedEventHandler` adds items through `IInventoryService` and sends `LootReceivedMsg` via `IGameEventPublisher`. | `InventoryStateService` reads `eventService.event.LootReceivedMsg()` and calls `addOrIncrementMany`; `LootTrackerComponent` also observes it. | Internal event plus SignalR push. | Good out-of-band candidate; needs duplicate protections if commands also return inventory changes. |
| Market listing bought | Player buys listing. | `MarketPlaceController.BuyoutListing` -> `BuyoutMarketPlaceListingCommand` -> `MarketPlaceService.BuyoutMarketPlaceListingAsync()` mutates buyer/seller/listing/inventory. | `MarketplaceStateService.buyoutListing()` applies remaining listing, purchased item, and buyer cinders. | REST command response plus SignalR for seller/market viewers. | Completed for buyer, seller, and market viewer state. |
| Market listing created/cancelled | Player lists or cancels an item. | `CreateMarketPlaceListingCommand` / `CancelMarketPlaceListingCommand` return listing/inventory result DTOs. | `MarketplaceStateService` applies returned listing and inventory item state. | REST command response plus market SignalR for other clients. | Completed for create/cancel. |
| Guild building construction | Leader/officer spends Guild Supplies. | `GuildController.ConstructBuilding` -> `ConstructGuildBuildingCommand` -> `IGuildBuildingService.ConstructAsync`. | `GuildStateService.constructBuilding()` applies the returned building overview and refreshes the guild snapshot. | REST overview response plus guild refresh. | Manual guild donation/vault flow has been removed; Guild Supplies now come from guild activity. |

## 5. Problems to Solve

- API command responses do not consistently return enough authoritative updated state. Dungeons are partly good; Colosseum, character actions, equipment, guild, market, and some essence commands are not.
- The frontend sometimes guesses state after success. Examples include Colosseum ticket decrement, marketplace listing decrement, guild resource updates, equipment slot/inventory mutations, and essence inventory decrement.
- UI state is refreshed through repeated REST calls after mutations, especially essences and guilds. This is safe but can be noisy and hides weak command contracts.
- SignalR exists but carries only one confirmed structured game update, `LootReceivedMsg`.
- There is no game-wide update envelope with sequence, occurred-at timestamp, update id, or feature type.
- There is no central reconnect recovery policy that refreshes active feature snapshots after SignalR reconnect.
- Backend internal events are not clearly separated from client-visible realtime events. `LootGeneratedEvent` directly triggers a push; other internal events remain private.
- Guild and world subscription concepts are now active; guild joins are authorized on the hub before group membership is granted.
- Components directly own complex state in Colosseum and marketplace flows that should move into feature state services.
- No evidence was found that SignalR hub authorization is explicitly required through `[Authorize]` or `.RequireAuthorization()`.
- No active background workers were found for idle combat, ticket regeneration, or world systems; most current state changes are REST-command or REST-poll driven.

## 6. Recommended Architecture

Use four separate concepts and keep them separate in code and reviews:

REST command responses are for player-initiated actions whose result is known during the request. The command response should include the authoritative changed state the caller needs immediately.

REST snapshots are for initial load, route entry, hard refresh, reconnect recovery, and state replacement when patching is risky.

SignalR pushed updates are for out-of-band changes: state caused by another player, another session, server-side timers, background work, guild/world activity, market sale notifications, achievements, notifications, and long-running process completion.

Internal backend events are for side effects and orchestration. They may trigger achievements, quests, logs, stats, notifications, REST response composition, SignalR pushes, or background work, but they should not automatically become client-visible messages.

Recommended backend structure:

- Keep `GameHub` thin.
- Add explicit authorization to the hub.
- Keep application/services as the owner of state changes.
- Introduce a clearer realtime notifier abstraction around `IGameEventPublisher`, or evolve `IGameEventPublisher` into `IGameRealtimeNotifier`.
- Send player/guild/group updates only when there is a real out-of-band consumer.
- Keep player commands as REST unless there is a strong realtime-command reason.
- Never trust client-sent player/character ids for private groups.

Recommended frontend structure:

- Keep one central game SignalR service for connection lifecycle only.
- Feature state services own feature state.
- Feature state services apply REST snapshots, REST command responses, and relevant SignalR updates.
- Components consume readonly signals or simple methods from feature services.
- Components do not subscribe to raw SignalR events except for display-only/debug features with a documented reason.

Use Angular signals as the default store primitive because most newer feature stores already use `signal`, `computed`, and `effect`. Keep RxJS at API boundaries and where existing streams are already in place. Phase out isolated `BehaviorSubject` state, starting with Colosseum, when touching those features.

## 7. Delivery Channel Decision Rules

Use REST command response when:

- The player initiated the action.
- The result is known during the request.
- The current caller needs immediate updated state.
- Example: PvP ticket consumed, essence equipped, inventory item used, dungeon entered.

Use REST snapshot when:

- The frontend initially loads a page or feature.
- The app refreshes.
- The SignalR connection reconnects.
- The client may have missed updates.
- State is complex and easier or safer to replace than patch.

Use SignalR when:

- The state change happens outside the current request/response flow.
- Another player/session caused the change.
- A background process caused the change.
- A server timer caused the change.
- A long-running process completed after the initial request returned.
- Multiple online clients need to observe the same state change.

Use internal backend events when:

- Other backend systems need to react.
- The event is useful for achievements, quests, tasks, logs, notifications, statistics, or chained effects.
- The event should not necessarily be sent to the client.

Do not use SignalR when:

- It would only echo the result of a REST command back to the same caller.
- The REST response can return the required updated state directly.
- It would introduce duplicate update paths without a real out-of-band need.

## 8. State Model: Snapshots, Command Results, and Pushed Updates

Initial page load should fetch a snapshot through REST. Current examples include `DungeonController.GetActiveDungeon`, `InventoryController.Get`, `CharacterController.Overview`, `EssenceController.GetArchive`, `EssenceController.GetLoadouts`, `GuildController.GetMyGuild`, and `ColosseumController.GetArenaTicketStatus`.

Player commands should return command result DTOs that contain the updated affected state. Current good examples are `StartDungeonRunCommand` returning `DungeonRunDto` and `ExecuteDungeonActionCommand` returning `ExecuteDungeonActionResponseDto`.

SignalR should apply incremental or replacement updates only for out-of-band changes. Current example: `LootReceivedMsg` is pushed to inventory when `LootGeneratedEventHandler` runs.

After reconnect, frontend services should resync critical state through REST snapshots. SignalR messages must not be the only source of truth.

Conceptual frontend store methods:

```typescript
applySnapshot(snapshot: FeatureSnapshotDto): void
applyCommandResult(response: FeatureCommandResponseDto): void
applyRealtimeUpdate(update: FeatureRealtimeUpdateDto): void
```

In this repository, adapt those names to existing conventions. For example, `DungeonStateService.refresh()` already behaves like snapshot loading, `executeAction()` applies a command result, and an eventual `applyRealtimeUpdate()` could apply out-of-band dungeon progress only if dungeon processing becomes async.

## 9. REST Command Response Guidelines

Command responses should return enough updated state for the UI to be correct immediately. The frontend should not blindly decrement counters merely because the HTTP request succeeded.

Guidelines:

- Return authoritative state for directly affected feature state.
- Include compact related state when needed, such as currencies, inventory changes, notifications, achievement unlocks, or character resources.
- Avoid huge full snapshots from every command unless replacement is simpler and acceptable.
- Prefer explicit response DTOs over `bool` for state-changing commands.
- Keep `Response<bool>` only for commands where the UI truly needs no changed state.

### PvP / Colosseum battle

Proposed C# DTO:

```csharp
public sealed class StartArenaBattleResponseDto
{
    public required CombatResultDto Battle { get; init; }
    public required ArenaTicketStatusDto ArenaTicketStatus { get; init; }
    public ColosseumMatchResultDto? MatchResult { get; init; }
    public CharacterOverviewDto? CharacterOverview { get; init; }
}
```

Ticket count should update from `ArenaTicketStatus`, not from `ArenaBattleComponent` decrementing `currentTickets`. No SignalR echo is needed for the caller. SignalR may still notify the challenged player, update a guild feed, or notify leaderboard changes if those become user-visible.

### Dungeon action

Current DTO already exists:

```csharp
public sealed class ExecuteDungeonActionResponseDto
{
    public required DungeonRunDto Run { get; init; }
    public required DungeonActionOutcomeDto Outcome { get; init; }
    public CombatSessionDto? CombatSession { get; init; }
    public string? Message { get; init; }
}
```

Keep this pattern. For reward claim, add a result DTO such as:

```csharp
public sealed class ClaimDungeonRewardsResponseDto
{
    public DungeonRunDto? ActiveRun { get; init; }
    public IReadOnlyList<InventoryItemDto> Rewards { get; init; } = [];
    public CharacterOverviewDto? CharacterOverview { get; init; }
}
```

If dungeon processing later continues asynchronously, SignalR can push later updates. Immediate action resolution should still return through REST.

### Equip essence / activate loadout

Current essence mutation commands mostly return `EssenceMutationResponseDto`; favorite and loadout activate/delete still return `ResponseMessageDto` and refresh snapshots. Recommended future response shape:

```typescript
export interface EquipEssenceResponse {
  activeLoadout: EssenceLoadoutDto;
  loadouts: EssenceLoadoutsDto;
  characterOverview?: CharacterOverviewDto;
  soulArchive?: SoulArchiveDto;
}
```

The caller should update equipped essences and character stats from the response. No SignalR echo is needed for the same caller.

### Market buyout

Proposed response shape:

```typescript
export interface BuyMarketListingResponse {
  purchasedItem: InventoryItem;
  buyerInventory?: InventoryDto;
  buyerCinders: number;
  updatedListing?: MarketPlaceListing;
  removedListingId?: string;
}
```

Buyer receives updated state through REST. Seller may receive a SignalR update/notification if online. Seller can recover through REST snapshots if offline.

## 10. Realtime / SignalR Guidelines

SignalR should push out-of-band updates. It should not become the primary command mechanism for ordinary player actions, should not replace REST snapshots, and should not be required for the caller to see the result of its own REST command.

Recommended main game hub approach:

- Keep a single `GameHub` for game state/notifications for now.
- Keep `LL-Chat` and `ChatHub` separate because chat has different interaction and scaling needs.
- Use typed hub clients, extending the current `IGameClient`.
- Evolve `GameEventEnvelope` into a versioned envelope with metadata.
- Add explicit `[Authorize]` on `GameHub` or `.RequireAuthorization()` on `MapHub<GameHub>("/hub")`.
- Make group membership server-authorized. Character group should come from `CharacterId` claim; guild/raid/world groups should be joined only after server-side checks.

Example envelope:

```csharp
public sealed record GameRealtimeUpdateEnvelope<TPayload>(
    string Type,
    string? UpdateId,
    long? Sequence,
    DateTimeOffset OccurredAt,
    TPayload Payload
);
```

Example client:

```csharp
public interface IGameClient
{
    Task ReceiveGameUpdate(GameRealtimeUpdateEnvelope<object> update);
    Task ReceiveNotification(GameNotificationDto notification);
}
```

Example notifier:

```csharp
public interface IGameRealtimeNotifier
{
    Task SendToPlayerAsync<TPayload>(
        Guid characterId,
        string updateType,
        TPayload payload,
        CancellationToken cancellationToken = default);

    Task SendToGroupAsync<TPayload>(
        string groupName,
        string updateType,
        TPayload payload,
        CancellationToken cancellationToken = default);
}
```

This can be implemented by evolving `IGameEventPublisher` rather than adding a parallel abstraction immediately.

## 11. Delivery Catalog

| State Change | Trigger | Primary Delivery | Secondary Delivery | Caller Receives Via | Other Players/Sessions Receive Via | Notes |
|---|---|---|---|---|---|---|
| Character stats changed | Equipment/essence/level command | REST command response | REST snapshot | Updated `CharacterOverviewDto` or stats DTO | Same-player other tabs may get SignalR later | Current character overview snapshot exists. |
| Character level changed | Leveling service | REST command response or polling snapshot | Internal event, optional SignalR notification | Response/snapshot from action that awarded XP | Notification if asynchronous | `CharacterLevelUpEvent` exists but handler is currently no-op. |
| Character resources changed | Market, guild donation, rewards | REST command response | REST snapshot | Updated resource/currency DTO | Seller/guild members via SignalR if affected | Cinders are mutated in market buyout today but not returned. |
| Idle combat tick processed | Current polling call | REST snapshot/polling response | SignalR only if moved to background worker | `CharacterActionDto` / combat session | Same-player other tabs optional | Today it is request-driven in `GetCharacterActionAsync`. |
| Combat session updated | Player starts/progresses combat | REST command response | SignalR if async/background | `CombatResultDto` or `CombatSessionDto` | Opponent notification/log if relevant | Colosseum returns combat result but not tickets/rating. |
| Combat rewards granted | Idle/dungeon/crafting flow | REST command response or polling snapshot | Internal event, SignalR for out-of-band loot | Inventory/reward result DTO | Loot notifications if generated asynchronously | `LootGeneratedEvent` already pushes `LootReceivedMsg`. |
| Dungeon run started | Player command | REST command response | REST snapshot | `DungeonRunDto` | None needed | Already implemented well. |
| Dungeon room completed | Player action | REST command response | SignalR only if async | `ExecuteDungeonActionResponseDto` | None needed | Already returns updated run. |
| Dungeon combat started | Player action | REST command response | None | `CombatSessionDto` in `ExecuteDungeonActionResponseDto` | None needed | Existing pattern. |
| Dungeon combat completed | Player action | REST command response | SignalR if async later | Updated run/outcome/rewards | None needed | Existing pattern. |
| Dungeon event resolved | Player action | REST command response | None | Updated run/outcome/message | None needed | Existing pattern. |
| Dungeon run completed | Player action | REST command response | REST snapshot | Updated `DungeonRunDto` | None needed | Existing pattern. |
| Dungeon run failed | Player action/combat result | REST command response | REST snapshot | Updated `DungeonRunDto` | None needed | Existing pattern. |
| Dungeon run abandoned | Player action | REST command response | REST snapshot | Updated run/outcome | None needed | Existing action response covers this. |
| Dungeon rewards claimed | Player command | REST command response | REST snapshot | Claimed loot, inventory, character, active-run state | None needed | Completed. |
| PvP battle started | Player command | REST command response | Internal event | Proposed `StartArenaBattleResponseDto` | Opponent notification optional | Currently only `CombatResultDto`. |
| PvP battle completed | Same request today | REST command response | Internal event | Battle result plus rating/match data | Defender log notification if implemented | `ArenaBattleCompletedEvent` saves match result. |
| Ticket consumed | Player starts battle | REST command response | None | Updated `ArenaTicketStatusDto` | Same-player other tab SignalR optional | Frontend applies returned ticket status. |
| Ticket regenerated | Snapshot calculation today; future timer possible | REST snapshot | SignalR if background timer added | Snapshot or pushed update | Same-player sessions SignalR if online | No timer worker exists today. |
| Rating changed | Battle completion | REST command response | Internal event, optional SignalR | Updated rating/match result | Leaderboard viewers optional | Current response omits rating. |
| Rank changed | Leaderboard recalculation | REST snapshot | SignalR if leaderboard live view added | Rankings snapshot | World/group SignalR optional | Future/planned live ranking. |
| Defense log added | Opponent challenges player | SignalR notification/update | REST snapshot | Snapshot if caller is defender viewing logs | Defender receives SignalR | Future/planned; no defense log found in inspected code. |
| Essence equipped/activated | Player command | REST command response | REST snapshot | Active loadout, character overview | Same-player other tab optional | Activate currently message-only. |
| Essence unequipped | Player command | REST command response | REST snapshot | Loadout/archive/stats | Same-player other tab optional | Future naming; loadout edits exist. |
| Essence leveled | Spend dust/XP grant | REST command response | Internal event | Updated essence/archive | Notification if async | Spend dust returns a result DTO; other actions vary. |
| Essence ascended | Player command | REST command response | REST snapshot | Updated essence/archive/stats | Same-player other tab optional | Currently message-only plus refresh; snapshot recovery is safe but chatty. |
| Essence evolved | Player command | REST command response | REST snapshot | Updated essence/archive/stats | Same-player other tab optional | Currently message-only plus refresh. |
| Soul archive updated | Absorb/evolve/ascend | REST command response | REST snapshot | `SoulArchiveDto` or compact change | Same-player other tab optional | Archive snapshot exists. |
| Inventory items added | Command or loot event | REST command response if caller initiated; SignalR if out-of-band | REST snapshot | Inventory change/result DTO | Same-player other tab optional | `LootReceivedMsg` exists. |
| Inventory items removed | Player command | REST command response | REST snapshot | Inventory change/result DTO | Same-player other tab optional | Scrap, market create/cancel, equipment, guild donations, dungeon rewards, essence mutations, and crafting queue removal now avoid local decrement guesses. |
| Inventory item updated | Player command/out-of-band | REST command response | SignalR if out-of-band | Updated item/list | Same-player other tab optional | Prefer replacement item over delta for stacks. |
| Currency balance changed | Market/rewards/donations | REST command response | SignalR if out-of-band | Currency DTO or character overview | Seller receives SignalR on sale | Cinders mutated in services today. |
| Guild member joined | Accept invite/application | REST command response | SignalR guild group | Updated guild DTO | Guild group SignalR | Guild commands mostly return bool. |
| Guild member left | Leave/disband/kick | REST command response | SignalR guild group | Updated membership/guild state | Guild group SignalR | Group authorization must be server-side. |
| Guild contribution updated | Donation | REST command response | SignalR guild group | Updated guild resources and caller inventory | Guild group SignalR | Frontend manually adjusts resources today. |
| Guild raid updated | Raid action/timer | SignalR pushed update | REST snapshot | Command response if caller action | Raid/guild group SignalR | Future/planned. |
| Guild announcement/activity feed updated | Member action/system | SignalR pushed update | REST snapshot | Command response if caller action | Guild group SignalR | Future/planned. |
| Market listing created | Seller command | REST command response | SignalR market group optional | Created listing plus inventory/currency changes | Market viewers optional SignalR | Current create returns listing but seller inventory adjusted locally. |
| Market listing sold | Buyer command | REST command response for buyer | SignalR seller notification/update | Buyer inventory/currency/listing result | Seller SignalR if online, market viewers optional | Strong SignalR vertical slice candidate after REST cleanup. |
| Market listing cancelled | Seller command | REST command response | SignalR market group optional | Returned inventory/listing removal result | Market viewers optional | Completed for caller state and market viewers. |
| Notification received | Internal/event/system | SignalR pushed notification | REST notification snapshot | If caused by command, include in response | Target player/group SignalR | Notification model not found in inspected code; proposed. |
| Achievement unlocked | Internal event | REST command response if synchronous | SignalR notification if async | Response notification/unlock DTO | Target player SignalR | Future/planned. |
| Title unlocked | Internal event | REST command response if synchronous | SignalR notification if async | Response notification/unlock DTO | Target player SignalR | Future/planned. |
| World boss health changed | Server/world action | SignalR world/group update | REST snapshot | Command response if caller action | World/raid group SignalR | Future/planned. |
| World boss phase changed | Server/world action | SignalR world/group update | REST snapshot | Snapshot/command response | World/raid group SignalR | Future/planned. |
| Raid state changed | Raid command/timer | SignalR group update | REST snapshot | Command response if caller action | Raid group SignalR | Future/planned. |
| Raid participant contribution changed | Player action | REST command response | SignalR raid group | Updated contribution DTO | Raid group SignalR | Future/planned. |

## 12. Proposed Backend Design

Hub structure:

- Keep `LL/src/Infrastructure/RealTime/RealTime.LL/GameHub.cs` as the single main game hub for now.
- Add `[Authorize]` to `GameHub` or change `app.MapHub<GameHub>("/hub").RequireAuthorization()` in `LL/src/API/API.LL/Program.cs`.
- Keep chat in `LL-Chat/API/API.Chat/Hubs/ChatHub.cs`.

Group naming:

- Character: `char:{characterId}`. Already used by `GameHub` and `GameEventPublisher`.
- Guild: `guild:{guildId}`. Already used in publisher; hub join is commented and needs authorization.
- Raid: `raid:{raidId}` proposed.
- Party: `party:{partyId}` proposed.
- World boss: `world-boss:{bossId}` proposed.
- Market: `market:{regionOrShard}` proposed only if live marketplace views need it.
- World: `world` exists in `GameHub.SubscribeToAudience`, but publisher uses `Clients.All` for `Audience.World`; make those consistent.

Contracts:

- Rename or replace `GameEventEnvelope` with `GameRealtimeUpdateEnvelope`.
- Keep `Event`/`Payload` compatibility during migration if needed.
- Add `Type`, `UpdateId`, `Sequence`, `OccurredAt`, and `Payload`.
- Define update type constants in a backend file such as `GameRealtimeUpdateTypes.cs`.

Command handlers and services:

- Keep command handlers responsible for calling application/domain services and composing response DTOs.
- Keep domain/application services responsible for mutation.
- Use internal MediatR events for side effects such as achievements, tasks, logs, and notifications.
- Do not let every internal event become a SignalR message by default.

REST command response DTOs:

- Add response DTOs beside existing command folders, following current use-case organization.
- Examples:
  - `LL/src/Core/Application/UseCases/Colosseum/Dtos/StartArenaBattleResponseDto.cs`
  - `LL/src/Core/Application/UseCases/MarketPlaces/Dtos/Responses/BuyoutMarketPlaceListingResponseDto.cs`
  - `LL/src/Core/Application/UseCases/Equipments/Dtos/EquipmentChangedResponseDto.cs`
  - `LL/src/Core/Application/UseCases/Guilds/Dtos/GuildCommandResponseDto.cs`

Realtime notifier:

- Evolve `IGameEventPublisher` to carry envelopes, or introduce `IGameRealtimeNotifier` next to `Application/Interfaces/WebSockets`.
- Implementation should remain in `RealTime.LL`.
- Notifier methods should accept character/group ids resolved server-side.

Error handling and logging:

- Log failed realtime sends enough to debug delivery, but do not fail the command after the database mutation solely because a notification could not be pushed.
- For command responses, keep validation failures as `Response<T>.Fail(...)` until a richer error model is introduced.

Authentication/authorization:

- Require authenticated hub connections.
- Confirm `CharacterId` claim is present before joining character group.
- Do not let clients subscribe to arbitrary `char:{id}` groups.
- Guild/raid/world subscriptions must check membership/permission server-side.

Reconnection support:

- Server does not need to replay all events in phase 1.
- Frontend should refresh critical snapshots after reconnect.
- Add sequence/update ids later for market, raids, world boss, or high-frequency combat if needed.

## 13. Proposed Frontend Design

Keep the current Angular signal direction:

- Feature state services use `signal` and `computed`.
- API services return RxJS `Observable<T>`.
- Components call feature state services.
- Components render readonly signals or simple DTO inputs.
- `BehaviorSubject` should be phased out when touching features, starting with Colosseum.

Proposed structure:

```text
src/app/core/services/real-time/
  game-realtime.service.ts              # evolve/rename GameEventService
  game-realtime-update-envelope.ts      # proposed
  game-realtime-update-types.ts         # proposed
  connection-status.model.ts            # proposed
```

Feature stores:

```text
src/app/core/services/api/colosseum/
  colosseum-state.service.ts            # proposed
  colosseum.service.ts                  # existing API service

src/app/core/services/api/dungeon/
  dungeon-state.service.ts              # existing
  dungeon.service.ts                    # existing
```

Recommended flow:

```text
Components call feature state services
Feature state services call API services for commands/snapshots
Feature state services apply REST responses
GameRealtimeService receives pushed updates
Feature state services subscribe to relevant pushed updates
Feature state services apply pushed updates
Components render readonly state from feature state services
```

Concrete recommendations:

- Rename conceptually from event-only `GameEventService` to `GameRealtimeService` when broadening beyond events.
- Add connection state signals: `status`, `lastConnectedAt`, `lastError`.
- Add `onReconnected` handling that tells active feature stores to reload snapshots.
- Let feature stores register effects against typed realtime signals or a typed dispatcher.
- Move `ColosseumComponent` arrays and ticket status into `ColosseumStateService`.
- Remove ticket decrement from `ArenaBattleComponent`; it should emit `challenge(id)` only.
- Move marketplace buy/sell local mutations into `MarketplaceStateService.applyCommandResult(...)`.
- Keep `CombatStateService` as client playback state, separate from server-authoritative feature state.

Conceptual service:

```typescript
@Injectable({ providedIn: 'root' })
export class GameRealtimeService {
  // Owns SignalR connection lifecycle only.
}
```

Conceptual Colosseum store:

```typescript
@Injectable({ providedIn: 'root' })
export class ColosseumStateService {
  // Loads ticket/opponent/ranking/match snapshots through REST.
  // Applies StartArenaBattleResponse from REST.
  // Applies out-of-band updates such as ticket regeneration or defense logs later.
}
```

## 14. Idempotency and Duplicate Update Prevention

Avoid duplicate application by design first:

- Do not intentionally send SignalR echoes for the same REST caller.
- If the REST command response includes updated inventory, do not also push the same inventory delta to the same connection as a required update.
- If cross-session support is needed, another browser tab for the same player may receive a pushed update. That is a separate use case from echoing the caller.

Pragmatic phase 1 approach:

- Prefer replacement updates over deltas for complex state.
- Refresh snapshots after reconnect.
- Keep feature stores from applying stale realtime updates over newer snapshots by tracking `lastSnapshotAt` or later sequence numbers.
- Use idempotent updates where possible: replace listing by id, replace item by itemInstanceId, replace ticket state wholesale.

Later hardening:

- Add `UpdateId` for dedupe.
- Add per-feature `Sequence` for market, raids, world boss, or high-frequency combat.
- Keep last applied sequence per feature or aggregate id.

## 15. Security and Authorization

Requirements:

- Hub connections must be authenticated.
- Character/user id must come from JWT claims, not from client payloads.
- Character group membership must be server-assigned.
- Clients must not subscribe to arbitrary player groups.
- Guild/raid/world group joining must check server-side membership and permissions.
- Realtime messages must not contain private state for the wrong audience.
- Logout should stop the SignalR connection and clear feature stores as appropriate.
- Token expiry should force disconnect or reconnect after refresh.

Existing evidence:

- `BaseController` requires auth and reads `CharacterId`.
- JWT bearer validates lifetime and checks required claims.
- Frontend uses `withCredentials: true` for SignalR, matching cookie-token support.
- `GameHub` currently calls `RequireCharacterId()`, but explicit hub authorization was not found.
- Guild subscription authorization is active in `GameHub.SubscribeToAudience` and `SubscribeToGuild`.

Recommended next security tasks:

- Add explicit hub authorization.
- Implement guild membership validation before `Groups.AddToGroupAsync(..., GuildGroup(id))`.
- Make `Audience.World` subscription either always allowed after authentication or remove client-controlled subscription until a feature needs it.
- Set `ValidateIssuer` and `ValidateAudience` appropriately before production hardening, matching existing TODO comments.

## 16. Reliability and Reconnection Strategy

Current frontend uses `.withAutomaticReconnect(...)` in `GameEventService`, but no reconnect recovery handlers were found.

Recommended behavior:

- Page must still work through REST if SignalR is unavailable.
- SignalR connection status should be exposed to services/UI.
- On reconnect, refresh active critical snapshots:
  - active dungeon run if dungeon route/store is active;
  - current character action if action polling is active;
  - inventory if inventory-affecting events may have been missed;
  - Colosseum ticket status if Colosseum is active or ticket state is globally displayed;
  - guild snapshot if guild page is active;
  - marketplace listings if marketplace page is active.
- Noncritical state can wait until the user revisits the feature.
- Missed updates should be recoverable from REST snapshots.

Fallback behavior:

- If SignalR fails to connect, log and expose status but allow REST snapshots/commands to continue.
- If a push update is malformed or unknown, ignore it with a warning instead of breaking the store.
- If reconnect happens after auth refresh, rebuild connection with the refreshed cookie/token.

## 17. Implementation Phases

### Phase 1: State Contract Audit and REST Response Cleanup

- Completed for Colosseum battle start, marketplace create/cancel, character action start/stop, equipment equip/unequip, and inventory scrap.
- Remaining high-value cleanup: marketplace buyout, dungeon reward claim/dismiss, selected essence archive/loadout mutations, and selected guild commands if broad refreshes become too noisy.
- Keep dungeons as a reference pattern.

### Phase 2: Frontend State Service Pattern

- Add `ColosseumStateService`.
- Move ticket/opponents/rankings/match results out of `ColosseumComponent`.
- Ensure feature stores expose readonly signals.
- Add `applySnapshot` and `applyCommandResult` style methods where useful.

### Phase 3: Realtime Foundation

- Completed: explicit `GameHub` authorization, envelope `UpdateId`/`OccurredAt`, connection status, reconnect counters, audience resubscription, and central event-envelope signals.
- Next hardening: ordering/sequence policy if events become high volume or causally dependent.
- Do not broadcast every state change.

### Phase 4: First Realtime Vertical Slice

- Completed: market seller/listing updates, guild invite/application/rejection/state/membership updates, character progression pushes, arena completion pushes, and loot duplicate hardening.
- Keep REST snapshots as source of truth.

### Phase 5: Dungeons / Combat / PvP Expansion

- Add realtime only where processing becomes async/background-driven.
- Keep immediate dungeon/Colosseum actions handled by REST command responses.
- Add reconnect resync for active dungeon/combat/PvP state.

### Phase 6: Inventory / Essences / Character

- Completed: inventory scrap, dungeon rewards, crafting queue removal, essence mutations, and character action command responses update local caller state.
- Remaining: favorite and loadout activation/deletion can return compact loadout/archive changes if the current refresh pattern becomes costly.
- Use SignalR only for out-of-band changes or same-player cross-session updates.

### Phase 7: Guild / Market / Notifications / World Systems

- Completed: authorized guild group updates, seller market updates, sidebar notification infrastructure, and shared notification indicator component.
- Remaining: durable notification inbox/history if notifications need to survive logout or appear in a future news/notification center.
- Add world boss/raid pushed updates when those systems exist.

### Phase 8: Hardening

- Completed: update ids for deduplication.
- Add sequences where ordering matters.
- Add reconnect recovery tests.
- Add backend integration tests for hub auth and command responses.
- Add frontend tests for feature-store response application.
- Add logging and diagnostics.

## 18. Detailed Task Breakdown

### Task 1: Audit REST Command Responses for Authoritative State

Files:

- `LL/src/Core/Application/UseCases/Colosseum/Commands/StartArenaBattle/StartArenaBattleCommand.cs`
- `LL/src/Core/Application/UseCases/MarketPlaces/Commands/BuyoutMarketPlaceListing/BuyoutMarketPlaceListingCommand.cs`
- `LL/src/Core/Application/UseCases/Equipments/Commands/EquipEquipment/EquipEquipmentCommand.cs`
- `LL/src/Core/Application/UseCases/Guilds/Commands/*`
- `LL/src/Core/Application/UseCases/Dungeons/Commands/ClaimDungeonRewards/ClaimDungeonRewardsCommand.cs`

Description:

Inventory all player-facing commands that return `Response<bool>` or `ResponseMessageDto` and classify what state they mutate.

Acceptance criteria:

- Each command has a documented response target: keep bool, add compact result, or return replacement snapshot.
- Colosseum, market, equipment, guild, and dungeon rewards are prioritized.

Risk:

Low

### Task 2: Add Colosseum Battle Command Result DTO

Files:

- `LL/src/Core/Application/UseCases/Colosseum/Dtos/StartArenaBattleResponseDto.cs` (proposed)
- `LL/src/Core/Application/UseCases/Colosseum/Commands/StartArenaBattle/StartArenaBattleCommand.cs`
- `LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs`
- `LL/src/API/API.LL/Controllers/V1/ColosseumController.cs`

Description:

Return battle result plus authoritative `ArenaTicketStatusDto` and any needed match/rating state.

Acceptance criteria:

- Starting a battle returns updated ticket count.
- Frontend no longer decrements tickets locally.
- No SignalR echo is required for caller ticket state.

Risk:

Medium

### Task 3: Add Colosseum State Service

Files:

- `LL/src/Presentation/ll/src/app/core/services/api/colosseum/colosseum-state.service.ts` (proposed)
- `LL/src/Presentation/ll/src/app/core/services/api/colosseum/colosseum.service.ts`
- `LL/src/Presentation/ll/src/app/features/game/city/colosseum/colosseum.component.ts`
- `LL/src/Presentation/ll/src/app/features/game/city/colosseum/arena-battle/arena-battle.component.ts`

Description:

Move ticket/opponent/ranking/match state into a signal-based state service and apply `StartArenaBattleResponse`.

Acceptance criteria:

- `ArenaBattleComponent` no longer mutates ticket count.
- Ticket count updates from REST response.
- Component renders readonly state from service.

Risk:

Medium

### Task 4: Add Explicit GameHub Authorization

Files:

- `LL/src/Infrastructure/RealTime/RealTime.LL/GameHub.cs`
- `LL/src/API/API.LL/Program.cs`

Description:

Require authenticated hub connections and keep group membership claim-based/server-authorized.

Acceptance criteria:

- Unauthenticated hub connection is rejected.
- Authenticated connection with valid `CharacterId` joins `char:{id}`.
- Client cannot join arbitrary character groups.

Risk:

Medium

### Task 5: Evolve Realtime Envelope

Files:

- `LL/src/Infrastructure/RealTime/RealTime.LL/GameEventEvelope.cs` (existing, misspelled)
- `LL/src/Infrastructure/RealTime/RealTime.LL/IGameClient.cs`
- `LL/src/Infrastructure/RealTime/RealTime.LL/GameEventPublisher.cs`
- `LL/src/Presentation/ll/src/app/core/services/real-time/game-event.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/real-time/game-event/game-event.map.ts`

Description:

Add occurred-at/update-id/optional sequence metadata while preserving current `LootReceivedMsg` behavior during migration.

Acceptance criteria:

- Existing loot event still works.
- New envelope can support dedupe and reconnect decisions.

Risk:

Medium

### Task 6: Add Reconnect Recovery Hooks

Files:

- `LL/src/Presentation/ll/src/app/core/services/real-time/game-event.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/real-time/real-time-facade.ts`
- Feature state services as needed.

Description:

Expose connection status and trigger active feature snapshot refresh after reconnect.

Acceptance criteria:

- Reconnect event is observable by feature stores.
- Inventory/Colosseum/dungeon active snapshots can refresh on reconnect.
- REST still works if SignalR is unavailable.

Risk:

Medium

### Task 7: Market Seller Notification Vertical Slice

Files:

- `LL/src/Core/Application/UseCases/MarketPlaces/Commands/BuyoutMarketPlaceListing/BuyoutMarketPlaceListingCommand.cs`
- `LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs`
- `LL/src/Core/Application/WebSockets/Contracts/GameEventMsg.cs`
- `LL/src/Infrastructure/RealTime/RealTime.LL/GameEventPublisher.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/market-place/market-place-state.service.ts`

Description:

After buyer REST response cleanup, notify seller out-of-band when their listing sells.

Acceptance criteria:

- Buyer UI updates from REST response.
- Seller receives notification/update through SignalR if online.
- Offline seller recovers through REST snapshot.

Risk:

High

### Task 8: Replace Manual Equipment Mutations with Command Results

Files:

- `LL/src/Core/Application/UseCases/Equipments/Commands/EquipEquipment/EquipEquipmentCommand.cs`
- `LL/src/Core/Application/UseCases/Equipments/Commands/UnequipEquipment/UnequipEquipmentCommand.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-state.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/inventory/inventory-state.service.ts`

Description:

Return authoritative equipment slots, inventory changes, and optional character overview after equip/unequip.

Acceptance criteria:

- Equipment state updates from response.
- Inventory state updates from response.
- No local slot rules duplicate backend rules in the component/store except for display helpers.

Risk:

High

## 19. Backend Acceptance Criteria

- Player-initiated REST commands return authoritative changed state needed by the UI.
- The frontend does not need a SignalR echo to update the result of its own command.
- `GameHub` requires authenticated users.
- Players only receive private updates intended for their own character/account.
- Hubs do not contain business logic.
- State-changing commands remain in application services / REST endpoints unless a hub method is explicitly justified.
- Realtime notifier can send typed player-scoped and group-scoped updates.
- Internal backend events are not automatically exposed as SignalR messages.
- Realtime updates are logged enough to debug delivery issues.
- Sending updates does not bypass backend authorization rules.
- Backend state remains the source of truth.

## 20. Frontend Acceptance Criteria

- Feature state services apply REST snapshots.
- Feature state services apply REST command responses.
- Feature state services apply SignalR updates only for out-of-band changes.
- There is one central game SignalR connection service.
- Feature stores do not create their own game SignalR connections.
- Components do not directly subscribe to raw SignalR events unless there is a documented exception.
- State stores expose simple readonly state to components.
- PvP/Colosseum ticket count updates from the REST battle response, not from a SignalR echo or local decrement.
- Reconnect triggers snapshot refresh for active features.
- Connection status is visible to relevant services/UI.
- SignalR failures do not break page loading through REST.
- State stores can handle duplicate or out-of-order updates where needed.

## 21. Testing Strategy

Backend unit tests:

- Command handler returns updated DTOs for Colosseum battle, market buyout, equipment changes, and dungeon reward claim.
- Realtime notifier builds correct envelope and target group.
- Internal events do not auto-publish unless an explicit handler does so.

Backend integration tests:

- `StartArenaBattle` returns combat result and updated ticket count.
- `StartArenaBattle` does not require SignalR to update caller state.
- `GameHub` rejects unauthenticated connection.
- Authenticated hub connection joins only the caller character group.
- Market seller notification targets seller, not buyer/private bystanders.

Frontend unit tests:

- Colosseum state service applies ticket snapshot.
- Colosseum state service applies `StartArenaBattleResponse`.
- `ArenaBattleComponent` emits challenge without decrementing tickets.
- Inventory state applies `LootReceivedMsg` once.
- Marketplace state applies buyout command result.
- Reconnect signal triggers active snapshot refresh.

Manual scenarios:

- Login and connect.
- Start a PvP/Colosseum battle.
- Verify ticket count updates from REST response.
- Verify no SignalR echo is required for the caller’s ticket count.
- Enter or progress a dungeon.
- Verify immediate dungeon state updates from REST response.
- Trigger an out-of-band update, such as loot, market sale notification, ticket regeneration if implemented later, or guild update.
- Verify SignalR updates the UI without manual refresh.
- Disconnect and reconnect.
- Verify snapshot resync.
- Verify another player does not receive private updates.
- Verify page still works when SignalR is unavailable but REST is available.
- Verify logout closes or invalidates the connection.

## 22. Risks and Tradeoffs

REST response size vs correctness: Returning too much data can make commands heavy, but returning only `bool` pushes complexity and drift risk into the frontend. Prefer compact explicit DTOs.

SignalR overuse: Broadcasting every mutation creates duplicate paths and bugs. Use it only for out-of-band state.

Duplicate update paths: The biggest risk is applying REST result and SignalR delta for the same change. Avoid same-caller echoes first; add update ids later.

Single hub vs multiple hubs: One `GameHub` is simpler and matches the current code. Multiple hubs can wait until traffic or domain boundaries demand them. Chat already has a separate service/hub.

Angular signals vs RxJS: Signals fit current feature stores and component rendering. Keep RxJS for HTTP and existing async flows.

Event granularity: Fine-grained deltas reduce payload but are harder to make idempotent. Start with compact replacement objects for important aggregates.

Snapshot replacement vs delta updates: Replacement is safer for inventory, dungeon run, guild, and ticket state. Deltas can be used later for high-frequency world/raid updates.

Overengineering risk: Do not build a full event-sourcing or client replay system now. Phase 1 can be REST response cleanup plus simple realtime hardening.

State drift risk: Current manual frontend mutations already create drift risk. Fix these before adding more realtime.

Testing complexity: Cross-player SignalR tests are harder than REST tests. Start with command response tests and one narrow realtime vertical slice.

Recommendation: build a boring, explicit state contract first. Let SignalR stay small and useful.

## 23. Recommended First Implementation Target

First target: Colosseum REST response cleanup.

Why:

- Backend and frontend already exist.
- The issue is visible and concrete: ticket count is consumed in `ColosseumService.StartArenaBattle()` but not returned.
- The frontend currently performs the exact anti-pattern this plan is meant to avoid: `ArenaBattleComponent.onChallenge()` decrements `arenaTicketStatus.currentTickets`.
- The slice is smaller than equipment, guild, or market.
- It proves the rule without adding SignalR first.

Start with REST response cleanup, not SignalR.

Files involved:

- `LL/src/API/API.LL/Controllers/V1/ColosseumController.cs`
- `LL/src/Core/Application/UseCases/Colosseum/Commands/StartArenaBattle/StartArenaBattleCommand.cs`
- `LL/src/Core/Application/UseCases/Colosseum/Dtos/ArenaTicketStatusDto.cs`
- `LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs`
- `LL/src/Presentation/ll/src/app/core/services/api/colosseum/colosseum.service.ts`
- `LL/src/Presentation/ll/src/app/features/game/city/colosseum/arena-battle/arena-battle.component.ts`
- `LL/src/Presentation/ll/src/app/features/game/city/colosseum/colosseum.component.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/colosseum/colosseum-state.service.ts` (proposed)

Success looks like:

- Start battle response includes updated ticket status.
- UI ticket count changes from the response.
- Component does not mutate ticket count directly.
- No SignalR event is involved for the caller ticket update.

Leave out of first implementation:

- Realtime ticket regeneration.
- Leaderboard live broadcasts.
- Defense log notifications.
- Full Colosseum redesign.

## 24. Final Proposed File/Folder Structure

Backend current structure is close enough; avoid moving large folders.

Proposed additions:

```text
LL/src/Core/Application/Realtime/
  GameRealtimeUpdateEnvelope.cs              # proposed if not kept under WebSockets
  GameRealtimeUpdateTypes.cs                 # proposed

LL/src/Core/Application/Interfaces/WebSockets/
  IGameRealtimeNotifier.cs                   # proposed or evolve IGameEventPublisher

LL/src/Core/Application/WebSockets/Contracts/
  InventoryUpdatedMsg.cs                     # proposed as needed
  MarketListingSoldMsg.cs                    # proposed as needed
  NotificationReceivedMsg.cs                 # proposed as needed

LL/src/Infrastructure/RealTime/RealTime.LL/
  GameHub.cs                                 # existing
  IGameClient.cs                             # existing, evolve
  GameEventPublisher.cs                      # existing, evolve or replace
  GameEventEvelope.cs                        # existing typo; rename during breaking change window
```

Feature command DTO additions:

```text
LL/src/Core/Application/UseCases/Colosseum/Dtos/
  StartArenaBattleResponseDto.cs             # proposed

LL/src/Core/Application/UseCases/MarketPlaces/Dtos/Responses/
  BuyoutMarketPlaceListingResponseDto.cs     # proposed
  CancelMarketPlaceListingResponseDto.cs     # proposed

LL/src/Core/Application/UseCases/Dungeons/Dtos/
  ClaimDungeonRewardsResponseDto.cs          # proposed
```

Frontend:

```text
LL/src/Presentation/ll/src/app/core/services/real-time/
  game-event.service.ts                      # existing; evolve or rename later
  real-time-facade.ts                        # existing
  connection-status.model.ts                 # proposed
  game-realtime-update-envelope.ts           # proposed
  game-realtime-update-types.ts              # proposed

LL/src/Presentation/ll/src/app/core/services/api/colosseum/
  colosseum.service.ts                       # existing API service
  colosseum-state.service.ts                 # proposed

LL/src/Presentation/ll/src/app/core/services/api/dungeon/
  dungeon.service.ts                         # existing
  dungeon-state.service.ts                   # existing reference pattern

LL/src/Presentation/ll/src/app/core/services/api/market-place/
  market-place.service.ts                    # existing API service
  market-place-state.service.ts              # existing, evolve
```

## 25. Summary Recommendation

Build a game-wide state-management convention around REST command responses, REST snapshots, and narrowly scoped SignalR out-of-band updates.

Do not build a broad realtime event catalog first. Do not use SignalR to echo every REST-triggered mutation to the same caller. Do not keep adding local frontend mutations after `Response<bool>` commands where the backend already knows the authoritative result.

Implement first:

1. Colosseum REST command response cleanup.
2. Signal-based `ColosseumStateService`.
3. Removal of local ticket decrement from `ArenaBattleComponent`.
4. Then harden `GameHub` authorization and connection/reconnect status.

After this document is reviewed, the next Codex prompt should ask for:

```text
Implement Phase 1 for Colosseum: return StartArenaBattleResponseDto with CombatResultDto and ArenaTicketStatusDto, add a signal-based ColosseumStateService, and remove local ticket decrement from ArenaBattleComponent. Do not add SignalR yet.
```

## Validation Notes

Repository searches were run for the requested SignalR terms, state-service terms, and REST command/response terms, including `SignalR`, `Hub`, `HubConnection`, `AddSignalR`, `MapHub`, `IHubContext`, `withAutomaticReconnect`, `Receive`, `SendAsync`, `OnConnectedAsync`, `OnDisconnectedAsync`, `StateService`, `signal`, `computed`, `effect`, `BehaviorSubject`, `Observable`, `Subject`, `Controller`, `HttpPost`, `Command`, `Handler`, `Result`, `Response`, and `Dto`.

The latest implementation pass was verified with:

- `node node_modules\@angular\cli\bin\ng.js build --configuration development` from `LL/src/Presentation/ll`: passed.
- `dotnet build LL\src\API\API.LL\API.LL.csproj`: passed with existing `MessagePack` NU1903 advisory warnings.
- `git diff --check`: passed with only CRLF normalization warnings.
- `dotnet build LL\LegendsLegacy.sln --no-restore`: still fails because of existing test compile errors in `LL/tests/EssenceSystem.Tests/EssenceSystemServiceTests.cs`: `FakeDefinitionRepository` and `SingleDefinitionRepository` do not implement `IEssenceDefinitionRepository.GetAllAbilities()`. The production API and admin projects still build during that solution build.

This document is now maintained as a living plan and has been updated alongside production code changes.
