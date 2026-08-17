# Live Data Synchronization & Frontend State Architecture Audit

Date: 2026-08-17

## Implementation Status

The in-repository implementation described by this audit is now complete for the supported game resources:

- persistent state notifications are queued through the transactional outbox and retain stable delivery IDs;
- character, inventory, equipment, quests, event quests, achievements, essences, soulstones, dungeons, prophecies, marketplace, guild, colosseum, and tournament checkpoints use independent monotonic revisions;
- successful mutation responses carry the affected revisions in `X-LL-State-Revisions`, and the Angular response interceptor forces post-response reconciliation to repair late stale responses;
- registered Angular resources acknowledge a revision only after their HTTP refresh completes successfully, expose convergence status, coalesce concurrent invalidations, and retry failures with bounded exponential backoff;
- reconnect, focus, and online recovery all use the same checkpoint protocol;
- marketplace expiration, guild membership changes, chat history recovery, and other background/outbox mutations participate in reconciliation;
- game and chat SignalR use a Redis backplane when `SignalR:UseRedisBackplane=true` and `ConnectionStrings:Redis` are configured, with Redis-backed shared chat presence in that mode;
- architecture and convergence tests guard durable publication, independent resource revisions, deduplication, retries, equal-revision recovery, and late mutation responses.

The `AddStateSyncRevisions` migration is generated but is not applied by this change. Redis provisioning, connection-secret configuration, rollout, dashboards, and alerting remain deployment responsibilities outside this repository.

`GameBootstrapStateService` remains an initial-hydration and reconnect composite rather than a revisioned resource. Its persistent subresources reconcile through their narrower scopes; current character-action recovery still uses the existing bootstrap/polling path. World Tower frames remain a deliberately separate sequenced stream. The compatibility `GameEventService` is also still present for transient UX/domain notifications, so the legacy live-event layer has not yet been fully removed.

An independent follow-up review corrected the implementation defects it found: game SignalR Redis registration now occurs before the service provider is built; Prophecies and Soulstones participate in checkpoint reconciliation; failed checkpoints retry with bounded backoff; and title mutations declare their achievements dependency instead of relying on component cross-store refreshes.

Tournament Grounds now saves its domain mutation and durable source outbox event inside the same service-owned database transaction. An outbox serialization/enqueue failure rolls back the mutation, while the no-outbox test fallback publishes only after commit. The frontend Tournament Grounds event handler retains transient event metadata but no longer performs a second refresh alongside revision reconciliation.

Transactional command resource dependencies are defined by exact command types in `StateSyncCommandScopeCatalog`; runtime namespace-string inference has been removed. An architecture test enumerates specialized transactional commands and fails when a new command has not been added to that contract. Background/outbox flows continue to declare their scopes through their consumer contracts.

Redis chat presence now uses renewable per-connection leases and an expiry-indexed online-user set. A terminated process stops renewing its local connections, so stale presence ages out naturally. `ChatPresence:KeyPrefix`, `ChatPresence:LeaseDuration`, and `ChatPresence:LeaseRenewalInterval` can be configured when the Redis backplane is enabled. The SignalR Redis package is pinned to 10.0.10, which resolves MessagePack 2.5.302 and removes the previously reported NuGet vulnerability advisories.

## 1. Executive Summary

The current synchronization architecture is reliable on the happy path but unreliable under disconnection, concurrent requests, background processing, retries, or multiple server instances.

This is primarily an architecture problem, not a collection of isolated implementation mistakes. Persistent server state is distributed across many Angular signal services, HTTP responses, compatibility event handlers, and component-triggered refreshes. There is no systemic contract that says:

- which resource changed;
- which version is authoritative;
- whether a client missed anything;
- how it must reconcile after reconnecting.

I would preserve SignalR, Angular signals, MediatR, EF Core, and the existing transactional outbox foundation. I would replace the synchronization model around them with:

- authoritative HTTP mutation responses;
- versioned server resources;
- post-commit transactional invalidation events;
- centralized reconnect reconciliation;
- per-domain server-state stores;
- sequenced streams only for genuinely high-frequency data.

The stale-state risk is systemic. Concrete critical defects already exist: pre-commit realtime publication, discarded dungeon reward responses, and guild subscriptions that survive membership changes.

## 2. Current Architecture

There are four principal synchronization paths.

~~~mermaid
flowchart TD
    U["Browser mutation"] --> HTTP["REST controller / MediatR command"]
    J["Quartz or hosted worker"] --> SVC["Application/service method"]

    HTTP --> TX["TransactionBehavior"]
    TX --> H["Command handler"]
    H --> DB["EF changes"]
    H --> DIRECT["Direct realtime publisher"]
    DIRECT --> HUB["GameHub"]
    DB --> SAVE["SaveChanges + commit"]

    H --> OUTBOX["GameEventOutbox rows"]
    SVC --> OUTBOX
    OUTBOX --> PG["PostgreSQL"]
    PG --> OW["API GameEventOutboxWorker"]
    OW --> C["Outbox consumer"]
    C --> HUB

    SVC --> NOOP["Worker NoOp realtime publishers"]

    HUB --> CONN["GameRealtimeConnection"]
    CONN --> LEGACY["GameEventService compatibility dispatcher"]
    CONN --> NEW["GameRealtimeEventRegistry"]
    LEGACY --> STORES["Domain signal services"]
    NEW --> STORES
    HTTP --> STORES
    STORES --> COMP["Angular components"]

    POLL["Character-action polling"] --> STORES

    CHAT["Separate LL-Chat API + SignalR hub"] --> CHATSTORE["ChatService message signal"]
~~~

Important details:

- The game hub is authenticated and assigns every connection to a character group. Guild membership is subscribed explicitly through [GameHub.cs](../LL/src/Infrastructure/RealTime/RealTime.LL/GameHub.cs#L18).
- The API registers SignalR and the outbox worker in [Program.cs](../LL/src/API/API.LL/Program.cs#L43).
- The frontend has one game connection but two dispatch layers: [GameEventService](../LL/src/Presentation/ll/src/app/core/services/real-time/game-event.service.ts#L116) and [GameRealtimeEventRegistry](../LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/game-realtime-event-registry.service.ts#L30).
- Direct publishers and outbox consumers coexist.
- The Worker process replaces direct realtime dependencies with [NoOp publishers](../LL/src/Worker/Worker.LL/Program.cs#L23). Only mutations that enqueue outbox events reliably escape the Worker process.
- Persistent state uses REST, direct state patches, snapshots, invalidations, additive commands, polling, and fixed-delay refetches. There is no single mutation contract.
- Browser storage is used principally for UI preferences, not authoritative game state. That is appropriate.

## 3. State Ownership Map

| Server resource | Frontend owners/copies | Assessment |
|---|---|---|
| Character and currencies | AuthService.currentCharacter, CharacterStateService, bootstrap DTO, marketplace/colosseum/currency handlers | Dangerous multiple write paths. AuthService is nominally authoritative, but unrelated services patch it. |
| Character overview/statistics | CharacterStateService.overview, dirty flag, independently loaded character | Separate from the character DTO and updated through caller-managed invalidation. |
| Inventory | InventoryStateService, mutation response objects, GameRealtimeStore.recentLoot, component selections | Best-developed store, with grant dedupe and load sequencing, but still manually coordinated with equipment, guild, marketplace, and dungeon state. |
| Equipment | EquipmentStateService, inventory equipment instances, character overview-derived stats | No systemic realtime/reconnect reconciliation. External equipment changes require explicit callers to refresh. |
| Quests | QuestStateService, bootstrap journal, full journal events, area-access cache | Full snapshots can race with HTTP responses. Outbox progression is guessed with a 750 ms retry. |
| Event quests | EventQuestStateService and component loads | No reconnect reconciliation; fixed-delay refresh after events. |
| Character actions/combat/crafting | CharacterActionsStateService, bootstrap action, polling service, CraftingService queue BehaviorSubject, session summary | Revision-aware action state is good, but reward effects are manually propagated to inventory, profession, character, and currency state. |
| Essences | Large EssenceStateService, inventory essence items, component refresh effects, character overview | High duplication and caller knowledge. No general reconnect reconciliation. |
| Guild | GuildStateService, JWT guild claim, game hub group, chat hub group, ChatService.activeGuildId | Particularly dangerous: multiple authorization and cache representations have independent lifecycles. |
| Marketplace | Four caches in MarketplaceStateService, character balances, inventory, realtime patches | No revisions; concurrent HTTP and world events can overwrite one another. |
| Dungeon | DungeonStateService, character and inventory stores, GameRealtimeStore reward history | Critical dependence on transient realtime for claim results. |
| Colosseum/tournament | ColosseumStateService, character arena rating, component-local tournament stores | Mix of response patches, broad refetch, and component event orchestration. |
| World Tower | Component-local playback state, versioned playback service, rally API state | Strongest design: sequence numbers, duplicate rejection, REST gap recovery. |
| Chat | ChatService.messageList, persistent history API, live hub, game-event-derived system messages | Stable message IDs and merging are good; reconnect history recovery is missing. |

The application has domain-local state services, which is better than purely component-local fetching. The problem is that there is no authoritative cross-domain server-state layer. Components and mutation handlers still need to remember which stores became stale.

## 4. Mutation / Synchronization Matrix

| Mutation | Backend/live path | Frontend behavior | Risk |
|---|---|---|---|
| Idle combat completion | Poll/command updates state; outbox progresses quests/achievements | Client derives XP and currencies from summary; inventory may be reloaded | Character/currency divergence, especially with duplicate or reordered resolution |
| Crafting completion | Response plus outbox progression | Crafting handler manually patches profession/currency; caller reloads inventory | Other tabs and inactive views can remain stale |
| Equip/unequip | HTTP response contains equipment and inventory | Sets two stores, dirties overview, invokes delayed quest refresh | Good initiator behavior; external changes and quest timing remain unsafe |
| Essence/loadout changes | Response and domain outbox events | Large service patches essence/inventory, dirties overview, refreshes quests | Many dependencies are caller-controlled; no reconnect reconciliation |
| Guild shop purchase | Response plus inventory grant | Applies grant and refreshes character | Grant ID is good; guild and character refresh ordering is not versioned |
| Guild vault transfer | Guild event/invalidation plus HTTP | Component explicitly refreshes guild, inventory, and equipment | Direct evidence of component-orchestrated synchronization |
| Marketplace trade | Direct pre-commit world patch plus HTTP response | Initiator patches response; all clients apply listing/order patches | Old HTTP/event can resurrect stale quantities; background expiry is silent |
| Dungeon reward claim | Three direct realtime snapshots plus full HTTP response | When realtime is enabled, HTTP inventory/character response is ignored | Critical: missed event leaves state stale despite successful response |
| Quest progression | Transactional outbox; full journal event | Event sets journal; callers also perform immediate and 750 ms GETs | Retry delay can exceed 750 ms; responses can arrive out of order |
| Arena battle | Live absolute rating event plus mutation response/refetch | Patches character and several caches | Broad, unversioned refresh; omitted character fields can be discarded |
| Tournament progression | Transactional outbox world update | Components refetch tournament resources | Reasonable delivery path, but synchronization remains component-owned |
| World Tower combat | Sequenced live frames plus REST catch-up and versioned playback | Duplicate/gap detection and reconnect recovery | Strong pattern worth preserving |
| Marketplace expiration job | Worker updates and commits orders/refunds | No outbox or live invalidation | Connected clients can retain expired orders and old balances indefinitely |
| Chat message | Persistent chat record plus SignalR | Stable-ID merge with history | Reconnect does not rejoin guild or fetch missed history |

## 5. Concrete Problems Found

### P1 — Realtime publication occurs before transaction commit — Critical

**Problem:** Command handlers can broadcast state before the enclosing transaction saves or commits.

**Evidence:** TransactionBehavior invokes the handler at [line 110](../LL/src/Core/Application/MediatR/Behaviors/TransactionBehavior.cs#L110), saves at line 113, and commits at line 115. Dungeon publishing occurs inside the handler at [ClaimDungeonRewardsCommand.cs:87](../LL/src/Core/Application/UseCases/Dungeons/Commands/ClaimDungeonRewards/ClaimDungeonRewardsCommand.cs#L87). Marketplace commands similarly publish from inside handlers, for example [BuyoutMarketPlaceListingCommand.cs:48](../LL/src/Core/Application/UseCases/MarketPlaces/Commands/BuyoutMarketPlaceListing/BuyoutMarketPlaceListingCommand.cs#L48).

**Failure mode:** A client receives an event, refetches before commit, and caches the old database state. If commit later fails, clients may have displayed a state that never existed. A realtime send exception can also roll back an otherwise valid mutation.

**Root cause:** Live delivery is treated as part of command execution rather than a post-commit integration concern.

### P2 — Dungeon rewards discard the authoritative HTTP result — Critical

**Problem:** A successful reward claim returns authoritative inventory and character snapshots, but the frontend intentionally ignores them when realtime is enabled.

**Evidence:** [DungeonStateService.applyClaimDungeonRewards](../LL/src/Presentation/ll/src/app/core/services/api/dungeon/dungeon-state.service.ts#L325) returns immediately at line 329. The backend has already included those snapshots in its response.

**Failure mode:** A short disconnect or lost message produces a successful claim with permanently stale inventory/currency until another unrelated refresh or reconnect.

**Root cause:** SignalR has incorrectly become the primary consistency mechanism for the initiating client.

### P3 — Guild membership has no unsubscribe/revocation lifecycle — Critical

**Problem:** Guild IDs are accumulated in the game connection and never removed while the connection is alive.

**Evidence:** [GameRealtimeConnection](../LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/game-realtime-connection.service.ts#L29) retains requested guild subscriptions. subscribeToGuild only adds at line 114. The server exposes SubscribeToGuild but no guild unsubscribe in [GameHub.cs](../LL/src/Infrastructure/RealTime/RealTime.LL/GameHub.cs#L30).

**Failure mode:** A member who leaves or changes guild can continue receiving the former guild's events until disconnection. Reconnect then retries every remembered guild ID.

**Root cause:** Group membership is modeled as an append-only client preference rather than authorization-sensitive state.

### P4 — No revision, checkpoint, or missed-event reconciliation protocol — High

**Problem:** The envelope only contains UpdateId, timestamp, name, and payload.

**Evidence:** [GameRealtimeEnvelope.cs](../LL/src/Core/Application/WebSockets/Contracts/GameRealtimeEnvelope.cs#L5). The connection increments a reconnect counter, after which individual services optionally refetch in [GameRealtimeConnection](../LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/game-realtime-connection.service.ts#L139).

**Failure mode:** Equipment, essence, professions, event quests, or inactive component state may miss updates and remain stale. The client cannot distinguish "nothing changed" from "messages were lost."

**Root cause:** Reconciliation is opt-in per service rather than a protocol-level guarantee.

### P5 — Background marketplace expiration is invisible to clients — High

**Problem:** The Worker expires orders and commits, but emits no outbox invalidation.

**Evidence:** [MarketplaceOrderExpirationJob.cs:53](../LL/src/Worker/Worker.LL/BackgroundJobs/MarketplaceOrderExpirationJob.cs#L53). Direct realtime services in the Worker are explicitly no-ops in [Program.cs:23](../LL/src/Worker/Worker.LL/Program.cs#L23).

**Failure mode:** Listings remain visible after expiration; refunds and balances remain outdated.

**Root cause:** Background mutation correctness depends on each feature remembering to opt into the outbox.

### P6 — Character updates can be silently discarded — High

**Problem:** Character equality omits three balances, and currency helpers mutate the current object in place before attempting a signal update.

**Evidence:** CharacterDto contains fateEcho, sigilFragments, and guildFavor at [characterDto.ts:13](../LL/src/Presentation/ll/src/app/shared/models/Dtos/characterDto.ts#L13), but [AuthService.isSameCharacter](../LL/src/Presentation/ll/src/app/core/services/api/auth/auth.service.ts#L396) does not compare them. [CurrencyService](../LL/src/Presentation/ll/src/app/core/services/api/currency/currency.service.ts#L10) mutates the existing object.

**Failure mode:** Sigil fragments, guild favor, fate echoes, cinders, or soulstones change on the object but reactive consumers are not notified.

**Root cause:** Mutable shared DTOs and hand-written partial equality are being used as state-store semantics.

### P7 — Older HTTP responses can overwrite newer state — High

**Problem:** Most state services lack request epochs or resource revisions.

**Evidence:** MarketplaceStateService.refresh starts four independent subscriptions at [market-place-state.service.ts:182](../LL/src/Presentation/ll/src/app/core/services/api/market-place/market-place-state.service.ts#L182). Quest loads and full live snapshots also have no ordering token. By contrast, inventory explicitly uses loadVersion at [inventory-state.service.ts:120](../LL/src/Presentation/ll/src/app/core/services/api/inventory/inventory-state.service.ts#L120).

**Failure mode:** Request A starts, realtime update B arrives, then A overwrites B. Two sales received out of order can restore an older remaining quantity.

**Root cause:** Race protection is repeatedly hand-built only after a specific bug is discovered.

### P8 — Event semantics and identities are inconsistent — High

**Problem:** Events mix additive commands, absolute snapshots, patches, invalidations, and notifications. Retries do not retain a stable envelope ID.

**Evidence:** Both publishers generate Guid.NewGuid() at delivery time in [GameRealtimeBroadcaster.cs:29](../LL/src/Infrastructure/RealTime/RealTime.LL/GameRealtimeBroadcaster.cs#L29) and [GameEventPublisher.cs:19](../LL/src/Infrastructure/RealTime/RealTime.LL/GameEventPublisher.cs#L19). The inventory outbox consumer deliberately publishes both new and legacy loot messages in [RealtimeInventoryGameEventOutboxConsumer.cs](../LL/src/Infrastructure/Service/Services.LL/Outbox/RealtimeInventoryGameEventOutboxConsumer.cs#L22).

**Failure mode:** An outbox retry receives a new update ID, so generic dedupe cannot identify it. Additive payloads may be applied twice unless they carry a separate grant ID.

**Root cause:** The envelope identifies a delivery attempt, not the durable state change.

### P9 — Chat reconnect loses guild delivery and missed history — High

**Problem:** Chat uses automatic reconnect but registers no reconnect callback.

**Evidence:** The connection is built with automatic reconnect at [chat.service.ts:365](../LL/src/Presentation/ll/src/app/core/services/ll-chat/chat-service/chat.service.ts#L365); no onreconnected handler exists. Guild join is explicit at line 244. The server only joins the public group on connection in [ChatHub.cs:133](../LL-Chat/API/API.Chat/Hubs/ChatHub.cs#L133).

**Failure mode:** After reconnecting, future guild messages stop arriving and messages sent during the outage remain absent.

**Root cause:** Durable history and transient group membership are not connected by a reconnect cursor workflow.

### P10 — Outbox adoption and scale-out are incomplete — High

**Problem:** The outbox is robust but covers only selected event types. Both game and chat SignalR use local process connection state with no backplane.

**Evidence:** The registry maps only a subset of domain events to realtime consumers in [GameEventOutboxConsumerRegistry.cs](../LL/src/Infrastructure/Service/Services.LL/Outbox/GameEventOutboxConsumerRegistry.cs#L8). Chat has a commented Redis setup and in-memory presence at [LL-Chat Program.cs:34](../LL-Chat/API/API.Chat/Program.cs#L34). Game API uses plain AddSignalR.

**Failure mode:** In a multi-instance deployment, the instance claiming an outbox row may not own the target client connection. Events then reach only clients connected to that instance.

**Root cause:** Process topology leaks into correctness.

### P11 — Components and domain services orchestrate cross-resource consistency — High

**Problem:** Mutations manually patch or refresh several unrelated stores.

**Evidence:** Equipment explicitly updates inventory, dirties character overview, and schedules quest refresh in [equipment-state.service.ts:115](../LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-state.service.ts#L115). Guild vault UI calls inventory and equipment loads itself in [guild-vault.component.ts:276](../LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-vault/guild-vault.component.ts#L276). The essence component reacts to action revisions and calls archive refresh at [essences.component.ts:431](../LL/src/Presentation/ll/src/app/features/game/character/essences/essences.component.ts#L431).

**Failure mode:** Adding a new mutation requires discovering every dependent cache. Missing one produces a stale screen.

**Root cause:** Dependency knowledge is encoded in callers instead of a resource/invalidation contract.

### P12 — Fixed delays and broad refreshes substitute for delivery guarantees — Medium

**Problem:** Quest progression is fetched immediately and again after 750 ms, while outbox retries may wait 5 seconds, 30 seconds, 2 minutes, or longer.

**Evidence:** [QuestStateService.refreshAfterOutboxProgress](../LL/src/Presentation/ll/src/app/core/services/api/quest/quest-state.service.ts#L164) and [GameEventOutboxWorker](../LL/src/API/API.LL/HostedServices/GameEventOutboxWorker.cs#L107).

**Failure mode:** Both GETs can precede progression. Guild events also trigger broad multi-endpoint refresh storms.

**Root cause:** Timing guesses are compensating for missing version acknowledgement and targeted invalidation.

### P13 — The transitional realtime layer contains dead and contradictory paths — Medium

**Problem:** Legacy and new dispatchers coexist, world subscription is unused by publishers, and declared events are unhandled.

**Evidence:** Audience.World broadcasts through Clients.All at [GameRealtimeBroadcaster.cs:51](../LL/src/Infrastructure/RealTime/RealTime.LL/GameRealtimeBroadcaster.cs#L51), despite SubscribeToWorld. IdleCombatProcessed exists in the contract but is not registered in [GameRealtimeEventRegistry](../LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/game-realtime-event-registry.service.ts#L47).

**Failure mode:** Developers can publish an apparently supported event that has no consumer, or assume world subscriptions restrict traffic when they do not.

**Root cause:** A partially completed migration has no enforced end state.

## 6. Architectural Weaknesses

The systemic weaknesses are:

1. **Realtime transport is conflated with consistency.** SignalR has no durable delivery guarantee, but some mutation flows treat it as authoritative.
2. **No resource model.** There is no shared vocabulary such as inventory:{characterId} or market:listings with versions and dependency rules.
3. **No post-commit publication boundary.** Direct hub sends remain legal from application handlers.
4. **No global recovery invariant.** Reconnect behavior depends on whichever services happened to implement a reconnect effect.
5. **State ownership is cross-cutting.** Character, inventory, overview, professions, quests, and feature-specific state can all be changed by unrelated services.
6. **Correctness depends on process topology.** Worker no-ops and local SignalR groups are visible to application semantics.
7. **The legacy/new transition doubles cognitive load.** Two publishers, two client dispatch systems, mixed payload styles, and redundant subscriptions coexist.
8. **Observability describes traffic, not convergence.** The client diagnostics record the last 100 events and handler timing in [GameRealtimeDiagnostics](../LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/game-realtime-diagnostics.service.ts#L18), but there are no resource revisions, outbox-lag metrics, dead-letter alerts, or client/server freshness comparisons.

Failure-scenario assessment:

| Scenario | Current result |
|---|---|
| Old HTTP response after live update | Unsafe in marketplace, quests, equipment, essence, colosseum, and several character refreshes |
| Successful mutation but missed SignalR | Dungeon and some live-only effects can remain stale |
| Disconnect while server changes state | Partial opt-in refetch; no proof of convergence |
| Two browser tabs | Both may receive events, but each has independent caches and can miss/reorder updates |
| Concurrent related mutations | Character commands have locking, but client payload ordering remains unversioned |
| Background worker mutation | Works only where the feature enqueues a recognized outbox event |
| Navigate after event occurred | Fresh if the component forces a load; potentially stale if a root cache decides it is already loaded |
| Multiple API instances | Hub delivery can be lost without a SignalR backplane |
| Terminal reconnect failure | Game connection becomes disconnected; no self-restart or global stale marking |

Positive foundations to retain:

- inventory grant IDs and request epochs;
- character-action revision checks;
- leaderboard request switching;
- transactional outbox rows and retry leases;
- World Tower sequence/gap recovery;
- chat message IDs and chronological merge;
- development realtime diagnostics.

## 7. Recommended Target Architecture

Use a hybrid model:

- HTTP is authoritative for persistent server state.
- SignalR carries post-commit versioned invalidations.
- Per-domain stores own server state.
- Angular component signals own UI state.
- Sequenced live streams remain available for combat/playback/presence.

~~~mermaid
flowchart TD
    M["Mutation or background job"] --> TX["Database transaction"]
    TX --> DATA["Authoritative state changes"]
    TX --> REV["Resource revisions"]
    TX --> OB["Durable outbox event"]
    TX --> COMMIT["Commit"]

    COMMIT --> RESP["HTTP mutation result with snapshots + revisions"]
    COMMIT --> WORKER["Outbox dispatcher"]
    WORKER --> SR["SignalR ResourceInvalidated"]

    RESP --> COORD["ServerStateCoordinator"]
    SR --> COORD
    RECONNECT["Reconnect / focus / online"] --> CHECK["Sync checkpoint endpoint"]
    CHECK --> COORD

    COORD --> CHAR["CharacterStore"]
    COORD --> INV["InventoryStore"]
    COORD --> EQUIP["EquipmentStore"]
    COORD --> QUEST["QuestStore"]
    COORD --> DOMAIN["Guild / Market / Dungeon stores"]

    CHAR --> UI["Components use read-only selectors"]
    INV --> UI
    EQUIP --> UI
    QUEST --> UI
    DOMAIN --> UI

    LIVE["Sequenced ephemeral stream"] --> LSTORE["Playback/live-session store"]
    LSTORE --> UI
    LIVE --> GAP["REST gap recovery"]
~~~

### Server-side contract

Introduce resource keys and monotonic revisions, for example:

- character:{characterId}
- inventory:{characterId}
- equipment:{characterId}
- quest-journal:{characterId}
- event-quests:{characterId}
- achievements:{characterId}
- guild:{guildId}
- market:listings
- market:buy-orders
- tournament:{tournamentId}
- world-tower-rally:{rallyId}

A durable invalidation envelope should resemble:

~~~json
{
  "eventId": "stable-outbox-message-id",
  "mutationId": "request-or-job-correlation-id",
  "scope": "character:...",
  "scopeSequence": 482,
  "resources": [
    { "key": "inventory:...", "revision": 191 },
    { "key": "character:...", "revision": 87 }
  ],
  "reason": "DungeonRewardsClaimed",
  "occurredAt": "..."
}
~~~

Rules:

- The eventId is the outbox message ID and is unchanged on retry.
- The resource revisions are incremented in the same transaction as data changes.
- Direct hub publication from commands is prohibited.
- HTTP query and mutation responses include resource revision or ETag.
- The initiating client immediately applies authoritative response snapshots.
- Other tabs/clients invalidate or refetch when the event revision exceeds their revision.
- Responses with lower revisions are rejected.
- Duplicate and out-of-order invalidations are harmless.

### Reconciliation

Add a lightweight checkpoint endpoint returning the current revision map for the authenticated character and active global scopes.

On connection, reconnect, browser focus after suspension, and transition back online:

1. mark active server-state resources as potentially stale;
2. fetch the checkpoint;
3. compare server and client revisions;
4. invalidate mismatched resources;
5. refetch active resources;
6. only then mark the connection synchronized.

Persistent state therefore does not require event replay. Missing every SignalR message is recoverable through checkpoint comparison.

### Frontend ownership

Use one store per resource/domain, not one giant global store. Each store should expose:

- immutable data;
- loading/error status;
- server revision;
- request epoch;
- stale/fresh status;
- applySnapshot;
- invalidate;
- reconcile.

Components should call mutation facades and consume selectors. They should never issue cross-store refresh sequences.

Keep purely local state—selected tabs, filters, modal visibility, animation state—in components or plain Angular signals.

### High-frequency data

Do not convert every World Tower frame into query invalidation. Preserve its existing model:

- monotonic sequence number;
- duplicate rejection;
- gap detection;
- REST catch-up;
- versioned finalized playback;
- durable invalidation only for rally/final-state changes.

### Guild and chat authorization

- Add explicit guild unsubscribe and membership-change handling.
- Rebuild/revalidate connection audiences when guild membership changes.
- Do not treat group membership as the security boundary; continue authorizing API reads.
- Chat must rejoin the current guild and fetch history after its last stable message cursor on reconnect.

## 8. Technology Recommendations

| Category | Recommendation |
|---|---|
| Keep | ASP.NET Core SignalR for low-latency hints and streams |
| Keep | EF Core/PostgreSQL transactional outbox |
| Keep | MediatR commands and transaction behavior |
| Keep | Angular signals for local UI and derived state |
| Keep | RxJS for bounded streams, cancellation, and polling |
| Introduce | @ngrx/signals SignalStore for consistent per-domain state containers |
| Introduce | Shared withServerResource store feature: revision, request epoch, stale state, invalidation and reconciliation |
| Introduce | Stable invalidation envelope, resource revision persistence, checkpoint endpoint |
| Introduce | OpenTelemetry traces/metrics for mutation → outbox → hub → client correlation |
| Introduce conditionally | Azure SignalR Service or Redis backplane when game/chat APIs run more than one instance |
| Replace | Direct realtime publishing with outbox-only post-commit dispatch |
| Replace | Full snapshot/additive live events for persistent state with versioned invalidations |
| Replace | Component-managed refresh sequences with mutation facades and resource dependencies |
| Remove | Legacy/new dual realtime dispatch after migration |
| Remove | Fixed 750 ms outbox guesses, unused subjects, dead event contracts, redundant world subscription |
| Do not introduce | Kafka, full event sourcing, or a monolithic Redux store at the current scale |

NgRx SignalStore is the preferred client-store dependency because it is Angular-native and supports dedicated stores and normalized entity collections through withEntities; its official guidance also recommends dedicated stores per entity type in most cases. See [NgRx SignalStore](https://ngrx.io/guide/signals/signal-store) and [NgRx entity management](https://ngrx.io/guide/signals/signal-store/entity-management).

TanStack Query's model would otherwise fit this problem well, but the official Angular package is still named @tanstack/angular-query-experimental and warns that patch and minor releases may break users. I would not make it the production foundation yet; hide the query/store implementation behind facades so it can be reconsidered after stabilization. See [TanStack Angular installation](https://tanstack.com/query/v5/docs/framework/angular/installation).

Microsoft documents that SignalR connections on different application servers are unknown to each other without Azure SignalR Service or a Redis backplane. That becomes mandatory when either API is scaled horizontally. See [ASP.NET Core SignalR scaling](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-10.0).

OpenTelemetry .NET traces, metrics, and logs are stable and appropriate for correlating commands, resource revisions, outbox delivery, and hub latency. See [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/).

## 9. Example Data Flows

### User mutation: equip an item

1. Client calls POST equip with a generated mutation ID.
2. Transaction updates equipment, inventory, derived character state, and relevant resource revisions.
3. One outbox invalidation row is written in the same transaction.
4. Commit succeeds.
5. HTTP response returns equipment and inventory snapshots plus revisions.
6. Initiating tab applies those snapshots immediately.
7. Outbox dispatcher sends the stable invalidation.
8. Other tabs invalidate equipment/inventory/character overview and refetch active resources.
9. A duplicate event is ignored because its revisions are not newer.

### Background mutation: marketplace expiration

1. Quartz job expires orders and performs refunds.
2. It increments market:listings, market:buy-orders, and affected player resource revisions.
3. It inserts invalidation rows in the same transaction.
4. After commit, active marketplace clients refetch lists.
5. Affected sellers invalidate character/inventory state.
6. A sleeping client misses the event, later compares checkpoints, and still converges.

### Multi-resource mutation: dungeon reward claim

1. Transaction changes dungeon run, inventory, character currencies, loot history, quest progression inputs, and revisions.
2. One durable change event lists every changed resource.
3. HTTP response returns the authoritative active run, inventory, character, loot, and revisions.
4. Initiating tab applies all response data regardless of SignalR state.
5. SignalR informs other tabs.
6. If SignalR is unavailable, the initiating tab remains correct and other tabs reconcile later.

### High-frequency mutation: World Tower combat

1. Server publishes frames with monotonically increasing sequence numbers.
2. Client rejects duplicates and buffers/detects gaps.
3. Gap recovery fetches frames after the last applied sequence.
4. Finalized playback is fetched by version/ETag.
5. Durable rally/result changes use the normal outbox invalidation path.
6. The query cache is not refetched for every animation frame.

## 10. Migration Plan

### Phase 0 — Instrumentation and contract tests

- **Objective:** Establish visibility and prevent new direct-publication paths.
- **Code:** Realtime publishers, outbox worker, transaction behavior, frontend diagnostics, test projects.
- **Dependencies:** None.
- **Risks:** Low.
- **Outcome:** Correlation IDs, outbox lag/failure metrics, reconnect metrics, architecture tests.
- **Verify:** A test fails if an application command directly depends on a hub broadcaster; dashboards expose delivery latency and failed rows.

### Phase 1 — Immediate correctness blockers

- **Objective:** Remove known critical player-facing failures.
- **Code:** Dungeon claim flow, character equality/immutability, game guild subscriptions, chat reconnect.
- **Dependencies:** Phase 0 correlation is helpful but not required.
- **Risks:** Low to medium.
- **Outcome:** HTTP responses remain authoritative; former guild audiences are removed; character signals always notify correctly.
- **Verify:** Targeted unit and hub integration tests for missed dungeon events, guild leave, and character-only balance changes.

### Phase 2 — Resource revision foundation

- **Objective:** Define systemic freshness.
- **Code:** Persistence models/migration, API DTOs, resource revision service, checkpoint endpoint, invalidation contract.
- **Dependencies:** Phase 0.
- **Risks:** Medium; every mutation must update the correct resource set.
- **Outcome:** Server and client can compare freshness without replaying all events.
- **Verify:** Transaction tests prove data, revision, and outbox row commit or roll back together.

### Phase 3 — Outbox-only post-commit delivery

- **Objective:** Eliminate pre-commit and process-local publication.
- **Code:** All direct IGameEventPublisher/IGameRealtimeBroadcaster call sites, consumer registry, marketplace expiration, Worker services.
- **Dependencies:** Phase 2 event contract.
- **Risks:** Medium; transient latency increases by the 500 ms poll interval unless notification-driven dispatch is later added.
- **Outcome:** API, Worker, and background mutations use the same reliable path.
- **Verify:** Simulated broadcaster failure cannot roll back the domain mutation; retry retains the same event ID.

### Phase 4 — First vertical slice: dungeon rewards

- **Objective:** Prove the complete target architecture.
- **Code:** Dungeon command, resource revisions, frontend dungeon/character/inventory stores, sync coordinator.
- **Dependencies:** Phases 2–3.
- **Risks:** Medium.
- **Outcome:** Initiator, second tab, disconnected tab, and reconnecting tab all converge.
- **Verify:** End-to-end tests cover response-before-event, event-before-response, duplicate, missed event, and slow stale response.

### Phase 5 — Core player state

- **Objective:** Migrate character, inventory, equipment, actions, quests, event quests, professions, and essences.
- **Code:** Existing state services and mutation handlers; introduce SignalStores/facades.
- **Dependencies:** Phase 4 patterns.
- **Risks:** High due to broad UI reach.
- **Outcome:** One authoritative owner per resource; components no longer orchestrate cross-store refresh.
- **Verify:** Contract tests enumerate changed resources per mutation; component tests consume only selectors/facades.

### Phase 6 — Shared/world systems and chat

- **Objective:** Migrate guild, marketplace, colosseum, tournaments, chat, and all scheduled jobs.
- **Code:** Guild and marketplace services, tournament components, LL-Chat reconnect/history API, infrastructure configuration.
- **Dependencies:** Core coordinator and revision conventions.
- **Risks:** Medium to high because of audiences and global resources.
- **Outcome:** Background and cross-player changes are recoverable.
- **Verify:** Multi-user, multi-tab, and multi-instance tests; forced hub disconnection and instance switching.

### Phase 7 — Cleanup and enforcement

- **Objective:** Remove the transitional architecture.
- **Code:** Legacy publisher/events, compatibility dispatcher, unused subjects, no-op publisher trap, manual refresh helpers.
- **Dependencies:** All migrated consumers.
- **Risks:** Medium if hidden consumers remain.
- **Outcome:** One event contract and one synchronization route.
- **Verify:** Repository searches find no direct publisher calls, fixed outbox delays, or component cross-store reload sequences.

### Testing strategy

Highest-value additions:

- backend transaction test: rollback produces neither committed state nor outbox event;
- backend background-job test: marketplace expiry produces resource invalidations;
- hub integration test: guild leave revokes audience;
- frontend coordinator tests for slow HTTP, duplicate, missed and out-of-order invalidations;
- reconnect test comparing checkpoint revisions;
- two-tab convergence test;
- multi-instance SignalR test once scale-out is configured;
- dungeon and equipment full-stack vertical-slice tests;
- property-style reducer tests asserting that applying duplicates and permutations never moves a resource revision backward.

## 11. Prioritized Work Items

### SYNC-001 — Enforce post-commit outbox publication

**Priority:** Critical  
**Effort:** Large  
**Risk:** Medium

**Reason:** Removes phantom and pre-commit state.

**Affected:** Application commands, realtime publishers, outbox registry, Worker jobs.

**Implementation:** Make durable invalidation enqueueing the only allowed application-layer publication path.

**Acceptance:** No command handler directly sends to SignalR; publisher failure cannot roll back committed game state.

### SYNC-002 — Define resource keys, revisions, and stable invalidation envelope

**Priority:** Critical  
**Effort:** Large  
**Risk:** Medium

**Reason:** Everything else depends on an objective freshness model.

**Affected:** Persistence, API contracts, outbox messages, frontend contracts.

**Acceptance:** Every migrated mutation identifies changed resources and returns/publishes the same committed revisions.

### SYNC-003 — Add checkpoint reconciliation coordinator

**Priority:** Critical  
**Effort:** Large  
**Risk:** Medium

**Reason:** Missed events must be recoverable.

**Affected:** Game connection, root synchronization layer, API checkpoint endpoint.

**Acceptance:** Disconnecting through several mutations and reconnecting converges without page reload.

### SYNC-004 — Make dungeon claim response authoritative

**Priority:** Critical  
**Effort:** Small  
**Risk:** Low

**Reason:** Existing response already contains the required state.

**Affected:** Dungeon state and reward handler.

**Acceptance:** Reward state is correct when SignalR is disabled, delayed, duplicated, or dropped.

### SYNC-005 — Correct guild audience lifecycle

**Priority:** Critical  
**Effort:** Medium  
**Risk:** Medium

**Reason:** Current behavior can cross an authorization boundary.

**Affected:** Game hub, connection service, guild membership commands, auth/session refresh.

**Acceptance:** After leaving or being removed, the old connection receives no former-guild events.

### SYNC-006 — Enforce immutable character updates

**Priority:** High  
**Effort:** Small  
**Risk:** Low

**Reason:** Current equality and in-place updates suppress signals.

**Affected:** Auth, CharacterState, CurrencyService, marketplace/colosseum patches.

**Acceptance:** Every CharacterDto field is represented in update semantics; objects are never mutated in place.

### SYNC-007 — Introduce per-domain SignalStores and mutation facades

**Priority:** High  
**Effort:** Extra large  
**Risk:** Medium

**Reason:** Components must stop owning synchronization dependencies.

**Affected:** All current *-state.service.ts files and consuming components.

**Acceptance:** Components consume read-only selectors and call one mutation API; no cross-store reload orchestration.

### SYNC-008 — Migrate marketplace and expiration job

**Priority:** High  
**Effort:** Large  
**Risk:** Medium

**Reason:** It combines world updates, player refunds, background changes, and ordering risks.

**Affected:** Marketplace commands/service/job/store.

**Acceptance:** Expiry and trades converge across active, disconnected, and second-tab clients; old revisions cannot restore quantities.

### SYNC-009 — Repair chat reconnect and prepare scale-out

**Priority:** High  
**Effort:** Medium  
**Risk:** Medium

**Reason:** Current automatic reconnect silently loses guild membership/history.

**Affected:** ChatService, ChatHub/history API, deployment configuration.

**Acceptance:** Reconnect rejoins authorized audiences and requests messages after the last cursor.

### SYNC-010 — Migrate core player resources

**Priority:** High  
**Effort:** Extra large  
**Risk:** High

**Reason:** Character, inventory, equipment, quests, essence, and actions have the highest dependency density.

**Acceptance:** Contract coverage shows every mutation's resource set; missed events always reconcile.

### SYNC-011 — Remove compatibility realtime architecture

**Priority:** Medium  
**Effort:** Large  
**Risk:** Medium

**Reason:** Dual publishers/dispatchers hide dead or duplicate paths.

**Acceptance:** One typed envelope, one client dispatcher, no IdleCombatProcessed-style unhandled contract, no redundant world subscription.

### SYNC-012 — Add convergence observability and failure tests

**Priority:** High  
**Effort:** Large  
**Risk:** Low

**Reason:** Synchronization failures should be measurable.

**Affected:** API/Worker telemetry, frontend diagnostics, integration tests.

**Acceptance:** Metrics cover outbox age/failure, hub reconnects, checkpoint mismatches, invalidation latency, refetch failures, and stale-resource duration.

## 12. What NOT to Do

- Do not add more refreshCharacter(), load(true), or setTimeout(750) calls after mutations.
- Do not broadcast larger full snapshots for every state change. That increases bandwidth without fixing ordering.
- Do not make SignalR acknowledgements a prerequisite for successful domain commits.
- Do not rely on event timestamps for ordering. Use committed monotonic revisions.
- Do not use Redis pub/sub as the durable event source. Redis/Azure SignalR should be a transport backplane; PostgreSQL/outbox remains the reliability boundary.
- Do not introduce a single monolithic global store containing UI state, server state, chat, and live combat frames.
- Do not apply optimistic additive currency or inventory messages unless the operation has a stable idempotency key.
- Do not retain both legacy and new event paths indefinitely.
- Do not adopt full event sourcing or Kafka for this problem. The current database outbox is sufficient.
- Do not adopt the experimental TanStack Angular package directly into every component; doing so would create a second migration if its API changes.
- Do not treat SignalR groups as lasting authorization. Membership changes must revoke delivery and API endpoints must remain authorized.
- Do not modify the separate infrastructure repository during application migration; document the eventual scale-out configuration requirement for that repository.

## 13. Final Recommendation

> If I owned this codebase, this is the architecture I would implement.

I would use HTTP plus per-domain NgRx SignalStores as the authoritative persistent-state layer. Every mutation would commit its state, resource revisions, and a stable outbox invalidation atomically. SignalR would deliver those invalidations after commit, and a checkpoint endpoint would reconcile anything missed on reconnect, focus, or network recovery. Mutation responses would immediately update the initiating client. World Tower-style sequenced streams would remain separate for high-frequency data.

That architecture makes the normal developer workflow systemic:

1. declare which resources a mutation changes;
2. update their revisions in the transaction;
3. return authoritative mutation data;
4. enqueue one invalidation;
5. let the synchronization coordinator handle every consumer.

### Original pre-migration audit execution notes

- **Changed application files during the original audit:** None. The implementation and independent follow-up described at the top of this document happened afterward.
- **Backend verification:** build/run-tests.ps1 -SkipBalance passed all 1,085 fast tests. The balance suite was intentionally skipped.
- **Frontend verification:** Focused specs could not start because this worktree had no installed Angular CLI/node modules; ng was not found.
- **Existing useful tests:** Inventory stale-request/grant-dedupe tests, character-action revision tests, chat merge tests, outbox consumer tests, and World Tower tests.
- **Migrations/configuration/deployment:** None were created during the audit. Implementing the recommendation will require a persistence migration for resource revisions/outbox metadata and, for horizontal scaling, a SignalR backplane configuration in the separate infrastructure repository.
