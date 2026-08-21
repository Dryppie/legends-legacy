# Sync-State Architecture Audit and Simplification Plan

## Architectural conclusion

Yes—the synchronization architecture is substantially overbuilt for one active client per character.

The main excess is not SignalR or the durable business outbox. It is the generic pipeline:

```text
every mutation
→ several persistent scope revisions
→ several durable invalidation messages
→ several frontend refresh registrations
→ overlapping GETs
```

That machinery has real runtime cost. At the same time, backend concurrency control, reconnect recovery, stale-response protection, worker-driven updates, idempotency, and ordered realtime streams remain necessary with a single UI.

The best target is a hybrid Model D:

- HTTP mutation responses own client-initiated state changes.
- SignalR carries server-initiated and shared-world changes.
- Reconnect performs one authoritative resynchronization.
- Full reloads happen only when a response is intentionally incomplete or divergence is suspected.

## Architecture at the time of the audit

The backend has 13 character scopes and 5 global world scopes in [`StateSyncScopes.cs`](../src/Core/Application/WebSockets/Contracts/StateSyncScopes.cs). Ninety-eight commands are explicitly mapped to scopes in [`StateSyncCommandScopeCatalog.cs`](../src/Core/Application/MediatR/Synchronization/StateSyncCommandScopeCatalog.cs).

For a successful changing command, [`TransactionBehavior.cs`](../src/Core/Application/MediatR/Behaviors/TransactionBehavior.cs):

1. Serializes same-character commands.
2. Determines affected character/world scopes.
3. For every scope, acquires a PostgreSQL advisory lock.
4. Reads and increments a persistent `StateSyncRevision`.
5. Adds a durable `StateInvalidated` realtime message to the outbox.
6. Returns changed revisions in `X-LL-State-Revisions`.

Each revision currently means two sequential database operations before the response—advisory lock plus revision `SELECT`—followed by the eventual revision/outbox writes. A five-scope mutation therefore adds roughly ten sequential sync-related database commands before commit.

The outbox worker polls every 500 ms in [`GameEventOutboxWorker.cs`](../src/API/API.LL/HostedServices/GameEventOutboxWorker.cs), sends the envelope through SignalR, and may create further invalidations when quest, achievement, or event-quest consumers alter state.

On the frontend:

- [`StateSyncInterceptor`](../src/Presentation/ll/src/app/core/interceptors/state-sync-interceptor.ts) reads mutation revision headers.
- [`StateSyncCoordinator`](../src/Presentation/ll/src/app/core/services/real-time/game-realtime/state-sync-coordinator.service.ts) tracks target/applied revisions, coalesces for 50 ms, retries failed refreshes, and deduplicates update IDs.
- Eighteen refresh callbacks are registered across Angular services and components.
- Initial connection, reconnect, focus, and online transitions request an 18-scope checkpoint.
- Thirteen frontend consumers also react directly to reconnect, independently of the coordinator.

There is no `BroadcastChannel`, storage-event synchronization, or other explicit cross-tab coordinator. Multi-tab support comes indirectly from all tabs joining the same character SignalR group in [`GameHub.cs`](../src/Infrastructure/RealTime/RealTime.LL/GameHub.cs).

## What the mechanisms protect

| Mechanism                                                  | Classification                      | Assessment                                                                                                                        |
| ---------------------------------------------------------- | ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Database transactions                                      | A — required                        | Fundamental server-authoritative consistency.                                                                                     |
| Character advisory lock                                    | A — required                        | Protects concurrent HTTP calls, retries, workers, and different API instances—not merely multiple tabs.                           |
| Frontend request epochs/load versions                      | A — required                        | Prevent stale GET A overwriting newer mutation/GET B.                                                                             |
| Character-action revision and stop/start serialization     | A — required                        | The UI itself can issue overlapping polls, manual refreshes, stop, and start requests.                                            |
| Business event outbox                                      | A — required                        | Quest, achievement, reward, chat, worker, and cross-process work must survive crashes and retries.                                |
| SignalR reconnect and audience resubscription              | A — required                        | Steam does not eliminate temporary disconnects.                                                                                   |
| Realtime update IDs, grant IDs, and event ledgers          | A — required                        | The outbox is at-least-once around a crash between send and acknowledgement.                                                      |
| World Tower/Raid frame sequences and gap recovery          | A — required                        | These streams can miss frames across reconnects and must retain ordering.                                                         |
| Character scope revisions                                  | C — legitimate but overengineered   | They recover missed changes, but mostly do so through many granular reloads that a single reconnect snapshot could replace.       |
| Global world revisions                                     | C — valid problem, poor granularity | Marketplace, guild, raid, and other changes between different players remain real, but global scope refreshes are too broad.      |
| Forced refresh after every mutation header                 | C/D                                 | It repairs late-response races, but duplicates authoritative mutation responses and compensates for missing store-level ordering. |
| Snapshot push plus invalidation plus response              | D — redundant                       | Several flows deliver the same state three ways.                                                                                  |
| Coordinator reconnect plus per-service reconnect refreshes | D — redundant                       | Both paths frequently reload the same resources.                                                                                  |
| Legacy and new realtime dispatch layers                    | A/C — dispatcher consolidated       | One connection and one registry now dispatch all envelopes; remaining legacy event contracts can migrate independently.           |

## Representative pre-migration flows and observed costs

These are code-path counts, not production traces. Optional requests depend on loaded services and active routes.

| Flow                                  | Current behavior                                                                                                                                                                                                                                                                                                                                             |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Equip equipment                       | 1 POST; authoritative equipment and inventory response; 5 revision increments and 5 durable invalidations (`character`, overview, equipment, inventory, quests); 1 business `EquipmentChanged` outbox event; approximately 5 follow-up GETs, or 6 on the overview route. Quest is fetched both manually and through state sync.                              |
| Save an Essence loadout               | 1 POST/PUT; 6 revision/invalidation scopes; response already patches the saved loadout; the `essences` registration alone performs 4 GETs, then inventory, equipment, quests, character, and optional overview add another 4–5. Approximately 8–9 GETs follow one save.                                                                                      |
| Save then activate an Essence loadout | Two mutations with the same broad scope profile. Depending on 50 ms coalescing, their refresh waves may merge or run separately. The callback also explicitly performs a four-request Essence refresh and a quest refresh.                                                                                                                                   |
| Start combat activity                 | Normally good: 1 POST returning the authoritative action, with no state invalidations when only the action row changes. Subsequent timed resolution polls may update character/inventory and then trigger targeted refreshes.                                                                                                                                |
| Claim dungeon rewards                 | 1 POST returning active run, full inventory, claimed loot, and character; 5 invalidations; 3 additional semantic realtime messages (`DungeonRewardsClaimed`, `InventorySnapshot`, `CharacterSnapshot`); one completion business event; approximately 7–8 follow-up GETs because dungeon and inventory scopes each trigger overlapping availability requests. |
| Marketplace change                    | Mutation responses commonly patch some local state and explicitly call refreshes. The global marketplace invalidation then executes a four-GET synchronization for every loaded marketplace client.                                                                                                                                                          |
| Colosseum change                      | A global `colosseum` invalidation executes six GETs per loaded client.                                                                                                                                                                                                                                                                                       |
| Guild change                          | `guild` is a global world revision even though `GameHub` has guild-specific groups. A refresh performs the guild snapshot and then loads buildings, missions, shop, directory, or invites—potentially across unrelated guilds.                                                                                                                               |
| Marketplace expiration worker         | No originating HTTP response exists. The worker invalidates character, inventory, and global marketplace state. This is a legitimate server-initiated synchronization case.                                                                                                                                                                                  |
| World Tower combat frames             | Immediate SignalR bypasses the outbox for ephemeral frames. The frontend rejects old sequences, detects gaps, and fetches missing frames. This is appropriate and should remain.                                                                                                                                                                             |

The equipment response/application path is visible in [`equipment-state.service.ts`](../src/Presentation/ll/src/app/core/services/api/equipment/equipment-state.service.ts). The especially broad Essence synchronization is in [`essence-state.service.ts`](../src/Presentation/ll/src/app/core/services/api/essences/essence-state.service.ts). Dungeon reward duplication originates in [`ClaimDungeonRewardsCommand.cs`](../src/Core/Application/UseCases/Dungeons/Commands/ClaimDungeonRewards/ClaimDungeonRewardsCommand.cs).

Additional startup/reconnect cost is also real:

- Services such as inventory, equipment, dungeon, and guild load before the initial checkpoint.
- Their registered applied revision starts at zero.
- Since persisted revisions are normally nonzero, the initial checkpoint treats the just-loaded state as stale and reloads it.
- On reconnect, the coordinator performs the same checkpoint while inventory, dungeon, quests, marketplace, guild, colosseum, character, bootstrap, and active page components independently refresh.

Focus also causes a checkpoint after a five-second throttle. In a Steam client, ordinary Alt-Tab activity could therefore produce repeated sync HTTP/database work even without a disconnection.

## State revision analysis

The revision design correctly solves several problems:

- Detecting events missed while disconnected.
- Coalescing several invalidations.
- Ignoring stale or out-of-order invalidations.
- Retrying a failed refresh without acknowledging the revision.
- Detecting a newer invalidation that arrives during a GET.

The problem is how broadly it is applied.

A typical command increments multiple independently locked revision rows, even when its response already contains the final state. World scopes are global, so unrelated guilds and unrelated colosseum or marketplace activity contend on the same advisory locks and notify every loaded client.

The mutation-header behavior is also compensating for a genuine single-client race:

```text
Mutation A commits
Mutation B commits
Response B arrives and is applied
Response A arrives late and overwrites B
```

The coordinator’s default forced refresh repairs that overwrite. That protection must not simply be deleted.

The better fix is to prevent stale mutation responses from being applied:

- Include relevant revision/version metadata with the response body.
- Give each store a mutation epoch or compare returned domain versions.
- Ignore an older response.
- Serialize mutations where user intent requires ordering.

Some stores already do this well. Equipment invalidates its in-flight load epoch; character auth updates invalidate its request epoch; character actions compare revisions and timestamps. Inventory’s `setInventory`, however, does not invalidate an older in-flight inventory GET, so it currently depends on the forced refresh for safety.

Recommendation:

1. Keep the revision system during Phase 1.
2. Narrow scopes and mark authoritative response scopes as handled only after adding store-level stale-response protection.
3. In Phase 2, replace generic character revisions with one consolidated reconnect/session snapshot.
4. Retain domain-specific revisions and sequences for character actions, raids/chat snapshots, combat frames, and other ordered streams.
5. Remove global marketplace, guild, and colosseum revision rows in favor of semantic, correctly targeted domain events and dirty-on-navigation caches.

If reconnect always fetches a consolidated authoritative snapshot, generic `StateSyncRevisions` can eventually disappear entirely. A single coarse character generation is optional if avoiding an unconditional reconnect snapshot proves valuable.

## SignalR analysis

SignalR should not be the authoritative store, but it should remain.

Its ideal responsibilities are:

- Notify the client of server-initiated character changes.
- Push safe semantic deltas or snapshots for background work.
- Carry shared multiplayer changes to the correct character, guild, raid, tournament, or world audience.
- Carry ephemeral ordered streams.
- Announce that a cached domain is dirty when a delta would be fragile.

It should not normally echo a local HTTP mutation back to the same client and cause another GET.

The repository already contained the beginnings of the right model: `stateSyncScopesHandledByResponse` in [`essences.service.ts`](../src/Presentation/ll/src/app/core/services/api/essences/essences.service.ts). At the time of the audit it was used only for spending Essence dust, and the API abstraction exposed the option only for POST—not PUT, PATCH, or DELETE. The implementation below has since generalized that contract.

The original implementation also had one SignalR connection but two dispatch layers: the newer registry and the compatibility `GameEventService`. That duplication has now been removed. All envelopes pass through `GameRealtimeEventRegistry`, which owns update-ID deduplication and exposes every realtime signal through one typed contract map. Loot grants now publish only the current `LootReceived` contract; their former `LootReceivedMsg` network echo and client-side grant suppression path are gone.

## Things That Look Like Multi-Client Complexity But Should Stay

- `TransactionBehavior` and the PostgreSQL character advisory lock. One UI can still issue concurrent HTTP requests, and workers/API instances operate independently.
- Request epochs, mutation epochs, and stale-response checks in Angular stores.
- Character-action polling guards, action revision, schedule generation, and stop/start serialization.
- Business outbox durability and event-consumer ledgers.
- Reward/claim idempotency and unique constraints.
- SignalR automatic reconnect and world/guild resubscription.
- Authoritative resync after reconnect or an ambiguous mutation failure.
- Update IDs and grant IDs for delta/notification deduplication.
- World Tower and Raid sequence numbers plus missing-frame recovery.
- Shared marketplace, guild, tournament, raid, and colosseum synchronization between different players. “One client per character” does not make the game single-player.
- Database concurrency handling even if active multi-session UI support is removed.

## Recommended target architecture

```text
CLIENT-INITIATED CHANGE
Angular
   │ HTTP command
   ▼
Transactional backend
   │
   ├── business writes + durable business events
   │
   └── authoritative mutation result + domain versions
                         │
                         ▼
                  Angular stores
             (ignore stale responses)


SERVER-INITIATED / SHARED CHANGE
Worker, timer, outbox consumer, or another player
   │
   ▼
Authoritative DB transaction
   │ durable semantic event
   ▼
Outbox → SignalR
   │
   ├── safe delta/snapshot → apply directly
   └── dirty-domain notice → one targeted GET


RECONNECT / LONG SUSPENSION
SignalR reconnect
   │
   ▼
One consolidated session resync
   │
   └── replace relevant cached state once
```

This is Model D.

## Concrete simplification plan

### KEEP

- Transaction and character locking.
- Business outbox and retry/idempotency machinery.
- SignalR connection, audience groups, and reconnect support.
- Semantic domain events for marketplace, guild, tournament, raids, rewards, and background work.
- Store request epochs and ordered-stream sequences.
- Server-authoritative HTTP/database state.

### SIMPLIFY

- Make mutation responses authoritative for every affected local store.
- Change mutation refresh default from “force every changed scope” to “response-owned unless explicitly incomplete.”
- Extend mutation metadata/options to PUT, PATCH, and DELETE.
- Replace broad static scope profiles with explicit response contracts or small changed-domain lists.
- Use dirty flags for inactive pages instead of refreshing hidden marketplace, guild, or colosseum state immediately.
- Replace global `guild` invalidation with guild-group targeting.
- Target raid, tournament, and market changes by entity or interested audience where possible.
- Use one reconnect recovery owner—the coordinator/session snapshot—not thirteen independent effects.
- Restrict focus resync to a real disconnect, long suspension, or failed/ambiguous operation.

### REMOVE

- Character invalidation echoes for mutations whose response fully updates local state.
- Dungeon `InventorySnapshot` and `CharacterSnapshot` echoes for the originating mutation once response handling owns them.
- Generic invalidation when a semantic event already carries the full journal or snapshot.
- Duplicate manual refreshes such as equipment’s quest refresh plus coordinator quest refresh.
- Legacy realtime messages after all consumers move to the typed registry.
- Per-scope durable realtime outbox messages for ordinary local mutations.
- Eventually, the generic `StateSyncRevision` table, catalog, controller, and interceptor path if consolidated reconnect recovery replaces it.

### ADD / CHANGE

- A standard mutation envelope containing authoritative data and domain versions.
- Store-level “apply only if newer” logic before marking response scopes handled.
- One consolidated reconnect/session snapshot—likely an expanded `GameBootstrap`, or a batch endpoint for currently loaded domains.
- Explicit recovery after ambiguous mutation errors: query operation/state rather than relying on a later invalidation.
- Metrics for mutation follow-up GET count, invalidations per command, outbox rows, and refresh fan-out.
- A clear single-session policy.

Single-session ownership is not enforced today: multiple refresh tokens can remain active and every authenticated hub connection joins the character group. If removal of cross-client synchronization will rely on the invariant, add a server-side session generation or lease so a new gameplay session invalidates the old one. Do not remove database concurrency controls even after doing so.

Session enforcement is not required for Phase 1. Eliminating redundant response refreshes is safe without it. It should be enforced before the final removal of all same-character external-change recovery.

## Phased migration

### Phase 1 — Obvious wins

1. Instrument follow-up GET and invalidation fan-out.
2. Add missing store mutation/load epochs, especially inventory.
3. Mark equipment, dungeon reward, Essence, marketplace, guild, and similar authoritative response scopes as handled.
4. Remove their duplicate manual refresh calls.
5. Consolidate reconnect ownership and stop direct service refreshes already covered by the checkpoint.
6. Stop initial checkpoint from reloading resources that just completed authoritative initial loads.
7. Narrow `guild` delivery to the guild audience.
8. Remove clearly redundant snapshot-plus-invalidation pairs.

Main risk: a mutation response thought to be complete may omit a derived domain such as quest progress or area access. Treat asynchronous outbox-consumer results as server-initiated changes, not part of the immediate response.

### Phase 2 — Architectural simplification

1. Introduce the standard mutation result/version contract.
2. Make response-owned state the default.
3. Build one consolidated reconnect snapshot.
4. Convert server-driven changes to semantic targeted events and deltas.
5. Remove character-local mutation invalidations.
6. Replace global world revision scopes with domain/entity targeting.
7. Decide and implement hard single-session ownership.
8. Retire generic state revisions once reconnect correctness no longer depends on them.

Main risk: event gaps or out-of-order semantic deltas. Use snapshots or dirty notices unless the event has a real entity sequence.

### Phase 3 — Optional cleanup

1. ~~Remove the compatibility realtime dispatcher, legacy message types, publisher abstraction, and frontend event map.~~
2. ~~Consolidate dedupe caches around envelope/update IDs and business grant IDs.~~
3. Delete the command scope catalog, revision migration/model, checkpoint endpoint, response header middleware, interceptor, and coordinator only after no consumers remain.
4. Simplify diagnostic/status machinery and obsolete tests.

## Audit scope and verification

This document is based on a static audit of the actual backend, worker, SignalR, outbox, and Angular code paths. The code-path request counts above are architectural estimates rather than captured production traces. The audit itself made no external-environment changes; the repository-local Phase 1 implementation completed afterward is summarized below.

## Phase 1 implementation status

Implemented:

- Development diagnostics now correlate each mutation with refresh callbacks and GETs started during the following two seconds. In a non-production browser, use `window.__gameSignalRDebug.stateSync.snapshot()` for structured data or `.print()` for a compact table.
- Backend metrics now record `state_sync.command.invalidation_fanout`, `state_sync.commands_with_invalidations`, `state_sync.invalidations`, and `state_sync.checkpoints`. The command name, public scope, and audience are metric tags where applicable.
- Mutation response ownership now works consistently for POST, PUT, PATCH, and DELETE.
- Equipment, dungeon rewards, Essence, marketplace, inventory, crafting, prophecy, soulstone, and equipped-title mutations identify the scopes their responses safely own.
- Inventory deltas and mutation snapshots invalidate older inventory GETs before applying local state.
- Duplicate manual quest, Essence, marketplace, inventory, and reconnect refreshes were removed.
- Reconnect recovery is owned by the realtime facade. It applies the consolidated bootstrap snapshot before checkpoint reconciliation; direct recovery is retained only for ordered active streams.
- Authentication, realtime subscription, and the initial checkpoint now complete before route services begin their ordinary initial loads. Registrations created afterward start at the reconciled revision instead of immediately repeating their first GET.
- `GameBootstrap` carries the captured state versions for its character and quest resources. Applying the snapshot acknowledges only those included resources, so a later checkpoint does not reload the same snapshot and a mutation racing the snapshot is still detected.
- Ordinary focus changes no longer request a checkpoint; focus recovery is reserved for suspensions of at least five minutes.
- Guild recovery is split by ownership: core, buildings, and missions use independent guild-audience generations; the character-specific shop, membership, and invitation/application views use character generations; and the public directory uses a world generation. Unrelated guilds no longer share live invalidations or durable shared-guild counters, and a core refresh no longer cascades through every Guild endpoint.
- Dungeon reward claims no longer echo redundant inventory and character snapshots after returning both in the HTTP response.

### Authoritative-response audit result

| Mutation family                                        | Response-owned now                                                                         | Still reconciled separately                                                               |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------- |
| Equipment and Essence                                  | Returned equipment/inventory/Essence snapshots and explicitly applied character/quest data | Any asynchronous consumer result not present in the response                              |
| Dungeon reward claim                                   | Run, refreshed dungeon hub, inventory, and character data applied by the claim flow         | Asynchronous quest progression and later worker-driven changes                            |
| Marketplace                                            | Complete listing, buy-order, and relevant history changes under one ordered marketplace version | Counterparty inventory/character state is targeted separately; expiration and detected sequence gaps still reconcile authoritatively |
| Colosseum arena and Champion's Market                  | Defense and market mutations own the affected character's Colosseum generation; battle responses apply immediate local deltas | Arena battles reconcile status, opponents, rankings, and history only for the two participants; Tournament Grounds keeps its separate semantic stream |
| Inventory item operations                              | Inventory deltas or full inventory, plus scopes known unchanged by the operation           | Derived progress applied later by outbox consumers                                        |
| Crafting                                               | Inventory returned or deterministically patched by craft, blueprint, and queue results     | Quest, mastery/overview, and other derived progress not fully represented by the result   |
| Prophecies                                             | Prophecy overview/progress/caches represented by the result                                | Inventory, currency, character, and asynchronous reward progress                          |
| Soulstones                                             | Complete upgrade view and returned character Soulstone balance                             | Inventory, quests, and richer overview data                                               |
| Titles                                                 | Returned equipped-title state and the locally patched character title                      | Rich character overview where the response is not a complete snapshot                     |
| Guild building, mission, and shop mutations            | Complete building/mission overview, shop purchase view, and idempotent inventory grant     | Core Guild resources, derived shop eligibility, character/title state, and acknowledgement-only vault/member mutations |
| Raid and Tournament Grounds                            | Complete RaidRun mutations own the originating character's raid detail and dirty-on-navigation directory versions; Tournament mutations converge through one ordered version | Raid rewards/vendor deltas and Tournament acknowledgement-only DTOs still reconcile through their authoritative GETs |
| Quest and event-quest commands                         | Welcome, choice, pin, and event-quest claims own their complete returned journals under independent ordered generations | Quest encounters, reward inventory/currency, area access, and asynchronous progression remain targeted separately |

The exclusions are intentional. A `200 OK` or a DTO containing one changed entity is not treated as authoritative for adjacent derived domains.

## Phase 2 foundation status

Phase 2 is active, and the redundant compatibility dispatcher has now been removed:

- `X-LL-Domain-Versions` is emitted and exposed alongside `X-LL-State-Revisions`; the Angular interceptor prefers the new name and falls back to the old one.
- `GameBootstrap.stateVersions` is the first body-level domain-version contract.
- `ApiService` now exposes versioned mutation results for POST, PUT, PATCH, and DELETE. Inventory, equipment, Essence progression, dungeon reward claims, marketplace local state, crafting, prophecies, Soulstones, equipped titles, quest journals, and event-quest journals use the contract where their response is complete enough to apply safely.
- Inventory item mutations return a complete authoritative inventory snapshot, not a partial delta. The inventory store compares the returned `inventory` version with the highest version already observed and ignores late snapshots; equipment applies the same rule independently to its `equipment` snapshot.
- Absorb, dismantle, spend-Dust, ascend, and evolve now return the complete Essence aggregate (archive, loadouts, creature archive, and codex) together with full inventory and equipment snapshots. Each store applies only a response whose own domain version is current. Partial Essence operations such as favorite, focus, and loadout changes deliberately remain coordinator-reconciled.
- Dungeon reward claims now return the post-claim dungeon hub in the same response as the active run, full inventory, claimed loot, and character summary. The dungeon, inventory, and character stores reject stale portions independently, and the previous follow-up availability GET is gone. Quest progression remains asynchronous.
- Every marketplace mutation now allocates one transaction-ordered marketplace version and returns a complete change set containing all affected listings, buy orders, and newly created history entries, including multi-fill operations. The same change set is published once as the typed `MarketplaceChanged` realtime event. Originating responses own the marketplace version and therefore no longer trigger the four-GET marketplace reconciliation; other clients apply contiguous semantic versions directly and request one authoritative reconciliation only when they detect a sequence gap. Affected counterparties are extracted from the event and receive targeted character/inventory reconciliation without publishing private balances to the world audience.
- The generic `colosseum` revision is now character-scoped rather than global. Updating an arena defense snapshot and purchasing from the Champion's Market apply their authoritative response locally and advance a silent character generation. Arena battles retain a four-view reconciliation for correctness, but only for the attacker and defender discovered from the targeted battle event; unrelated loaded clients no longer execute the previous six-GET refresh. Tournament Grounds continues to use its independent `tournament` scope and semantic updates.
- Crafting inventory deltas, prophecy state, Soulstone upgrade views/balances, and equipped-title state now compare their returned domain versions before applying. Their incomplete derived scopes continue through targeted invalidation.
- Response-owned inventory, equipment, Essence, dungeon, marketplace, crafting, prophecy, Soulstone, title, and character versions still advance the durable revision/checkpoint row and are returned in the HTTP header, but no longer publish a redundant same-character `StateInvalidated` echo. Incomplete derived state, asynchronous quest progression, transfer-recipient inventory changes, and partial adjacent views remain realtime-reconciled.
- Initial connection and reconnect now use the bootstrap as the consolidated character/quest/action/account snapshot, followed by a checkpoint only for resources that snapshot does not own or that advanced while it was being read.
- Existing semantic guild events remain the live cross-member payload. Shared guild generations now use `guild:{guildId}:guild`, `guild:{guildId}:guild-buildings`, and `guild:{guildId}:guild-missions` keys, and checkpoints resolve only the current character's guild. The character-specific shop, membership, invite/application, and directory views have matching durable scopes, so reconnect recovery no longer depends on a global `world:guild` row.
- Switching guild identity explicitly resets the frontend's core, buildings, missions, and shop revision boundaries and reconciles the new audience. In-flight refresh completions from the previous guild are generation-guarded so a larger old-guild revision cannot suppress a smaller new-guild revision.
- Guild command contracts now describe their actual writes. Core metadata/member commands do not dirty inventory or equipment; vault commands select only affected item views; building/mission/shop commands use dedicated scopes; and complete mutation DTOs own only their matching subresource while incomplete adjacent state remains reconciled.
- Raid synchronization now separates the character `raids` generation from the world `raid-directory` generation. Complete RaidRun mutations silently advance both versions for the origin, the region view recovers through the directory generation, and detailed `RaidUpdated` events are delivered only to connections subscribed to that raid. A separate compact `RaidDirectoryUpdated` notice retains live open-raid discovery without broadcasting detailed-page refreshes globally.
- Tournament Grounds changes allocate their `tournament` version in the authoritative transaction and carry that version in the semantic event. Local mutation headers and later events therefore converge on one coordinator target instead of causing a manual refresh followed by an outbox invalidation refresh. Tournament events are delivered only to active Grounds/replay subscribers rather than every world connection.
- Quest welcome, choice, and pin mutations now own only the `quests` generation returned with their complete journal; they no longer dirty area access or the character overview. Asynchronous quest progression advances that same generation before publishing its complete journal, and the client rejects an older event or mutation snapshot. Quest completion leaves only the outbox consumer's targeted `area-access` invalidation, eliminating the former immediate-plus-worker duplicate GET.
- Event-quest claims own the complete `event-quests` journal they return while inventory and character rewards remain reconciled independently. Shared progress now emits one world event instead of an additional duplicate personal event to the contributor, and the contributor generation is advanced silently so the worker does not echo a second invalidation.
- `GameEventService` and its second subscription/lifecycle path are removed. `GameRealtimeEventRegistry` is the sole envelope dispatcher and update-ID dedupe boundary; connection status and audience subscriptions remain owned by `GameRealtimeConnection`. All Angular consumers and tests now use those two services directly.
- Inventory grants publish one durable `LootReceived` delivery instead of paired `LootReceived`/`LootReceivedMsg` messages. The modern path covers committed outbox grants, combat loot, selection crates, prophecy caches, tournament rewards, player transfers, and generated loot; the durable `RealtimeDeliveryRequested` outbox remains the transport boundary.
- Account access, character level-up, Soulstone-drop, and marketplace change signals now live in the typed realtime contract alongside the snapshot/invalidation events. Their legacy `*Msg` contracts are removed, as are six unused per-operation marketplace message types that had no publishers or consumers. Character level-up remains post-commit through its business outbox consumer, and marketplace delivery remains durable through `RealtimeDeliveryRequested`.
- The eleven Guild application, invitation, membership, building, mission, vault-chat, state, disband, and directory signals now use the same typed realtime contract and broadcaster. Existing character, guild, and world audiences are preserved; mission changes remain post-commit through the business outbox. The duplicate frontend Guild DTO family and the matching legacy `*Msg` records are removed.
- Quest journal and event-quest changes now use typed `QuestJournalChanged` and `EventQuestChanged` realtime contracts. Ordered quest-journal snapshots retain their state version and stale-response rejection; event quests retain their world-or-character dirty-cache notification behavior. Both deliveries use the durable realtime outbox, and the legacy frontend DTOs and `*Msg` records are removed.
- Arena completion, prophecy progress, achievement unlock, and player-transfer notifications now use the typed realtime contract and broadcaster. The last `*Msg` contracts, `GameEventMsg`, `IGameEventPublisher`, `OutboxGameEventPublisher`, and the Angular compatibility event map/envelope are removed. Existing character and world audiences, system-chat messages, and business outboxes are unchanged; durable delivery still flows through `RealtimeDeliveryRequested`.
- Realtime deduplication now distinguishes bounded transport update IDs, idempotent business grant IDs, and Angular signal-effect re-entry. The old `game-event` helper and component-local ID caches are removed. Contract-parity tests require every backend `GameRealtimeEvent` and `GameRealtimeEventNames` entry to match the Angular event-name registry and signal map.
- Response ownership is opt-in per scope during migration, so incomplete responses continue through the safe revision path.

Deliberately deferred because the necessary response/event contracts are not complete yet:

- Expanding `GameBootstrap` to every optionally loaded page domain; eagerly returning marketplace, guild, colosseum, raid, and dungeon pages would replace network fan-out with an oversized snapshot.
- Replacing the remaining global marketplace world audience with item/entity subscriptions if production scale requires it; the current ordered change set already removes ordinary refresh fan-out. Colosseum now uses participant-targeted character generations, while Raid details and Tournament Grounds use interested-viewer groups.
- Making response ownership the default before every mutation endpoint has a complete mutation envelope.
- Removing character-local mutation invalidations for remaining domains whose responses are partial, acknowledgement-only, or not yet version-guarded.
- Removing the revision table, scope catalog, mutation header, interceptor, or coordinator.
- Enforcing a single active gameplay session.

## Transitional revision/checkpoint removal readiness

Audited on 2026-08-21: **not ready for removal**. The revision/checkpoint path is still active application behavior rather than orphaned compatibility code:

- `TransactionBehavior` still resolves mutation scopes through `StateSyncCommandScopeCatalog`, advances durable revisions, and arranges invalidation delivery.
- API middleware emits `X-LL-Domain-Versions` and the fallback `X-LL-State-Revisions`; the Angular interceptor consumes them and calls `StateSyncCoordinator.acceptMutationResponse`.
- `GameRealtimeEventRegistry` still routes `StateInvalidated` envelopes into the coordinator, and cached character, inventory, equipment, Essence, Guild, quest, dungeon, marketplace, colosseum, event-quest, achievement, Raid, Tournament Grounds, and region views still register recovery callbacks.
- Online recovery and long-suspension focus recovery still call the checkpoint endpoint. `GameBootstrap` carries captured versions so its snapshots can acknowledge only the resources they include.
- `StateSyncService`, the outbox worker, Marketplace, Quest/event-quest, Raid, and Tournament Grounds still write or consume revision state. The `StateSyncRevisions` table is also exposed in LiveOps player-support diagnostics.

Removal must therefore be a later staged migration: first make every remaining mutation response or semantic event authoritative, remove each scope registration and invalidation writer with coverage, then disable checkpoint/header consumers for at least one monitored release. Only after no runtime readers or writers remain should the API endpoint, middleware, coordinator/interceptor, scope catalog, model, and table be removed. No part of that infrastructure is deleted in this hardening pass.

## P0–P3 hardening status (2026-08-21)

The follow-up hardening pass retained the hybrid architecture and closed the audit's concrete correctness and performance gaps:

- Mutation ownership is projection-specific. A response can acknowledge only registrations that explicitly declare themselves canonical response owners; derived projections remain eligible for reconciliation.
- Coordinator refresh/reconcile completions are session-generation guarded, and character-scoped stores reset or invalidate pending work when the active character changes.
- Initial SignalR startup retries with backoff. Reconnect restoration rejoins world, guild, raid, and Tournament Grounds audiences before recovery is announced and before the checkpoint is reconciled. Forbidden audience subscriptions are discarded instead of retried forever.
- Raid group subscription now verifies that the character can access the raid.
- Failed optimistic action stops reload authoritative action state and restore polling when the server still owns an active action.
- Guild building, mission, and shop mutations and RaidRun mutations use versioned authoritative responses with stale-response protection. Guild and Raid query epochs prevent an older GET from replacing a newer accepted response.
- Guild-shop purchase applies the response's inventory grant without issuing a second manual character refresh; the remaining character invalidation is the single recovery source for response fields that are not returned.
- RaidRun DTOs and `RaidUpdated` events carry the raid row version. A detailed raid page ignores a semantic echo whose version is already represented by its accepted mutation response, while a newer event and all versionless compatibility events still trigger an authoritative GET.
- Multi-scope character revision updates are batched behind the same per-scope advisory locks, loaded with one revision query, and emitted as one `StateInvalidations` envelope. Marketplace expiration now sends one private invalidation envelope per affected character instead of separate character and inventory deliveries.
- Realtime and state-sync trace buffers, payload sizing, handler timing, and the one-second freeze heartbeat are no-ops when the runtime environment is `prod`. The browser debug API remains available in non-production environments.
- Unused realtime reconnect and store fields were removed. Explicit character-group removal on disconnect was also removed because SignalR owns connection-group cleanup.

Durable revisions remain intentional: response-owned versions still protect against late HTTP responses and participate in reconnect checkpoints. The pass batches multi-scope character work rather than deleting those correctness boundaries. Old rows for retired scope names are harmless and are not deleted automatically; operational cleanup can remove known retired keys after a monitored release. Renaming the remaining abstractions is deferred until every partial-response domain has migrated, avoiding terminology churn while the compatibility path is still active.

## Verification and rollout notes

- The browser diagnostic is intentionally development-only and keeps at most 100 mutation traces in memory. A follow-up GET is a correlation candidate, not proof of causality; use the attached refresh callback list to distinguish coordinator fan-out from component activity.
- Realtime browser traces now classify each received envelope as `handled`, `duplicate`, `unknown`, or `failed`, and retain a failure message for handler exceptions. During staged rollout, inspect `window.__gameSignalRDebug.recentEvents()` and treat any `unknown` or `failed` entry as a rollback/investigation signal; duplicates are expected during retry/reconnect replay and should not update state.
- The API outbox worker already exports `game_event_outbox.delivery_lag`, `game_event_outbox.deliveries.retried`, and `game_event_outbox.deliveries.failed` from the `LegendsLegacy.GameEventOutbox` meter. Alert on sustained delivery lag above five minutes, any increasing failed-delivery count, or a retry-rate step change. The LiveOps operational status exposes pending deliveries, failed deliveries, and the oldest pending timestamp and marks the outbox degraded when failures exist or the oldest delivery is more than five minutes old.
- Roll out the API and Angular client together in a canary/staging environment. Exercise one character, guild, world, raid, Tournament Grounds, and World Tower audience; verify event-name parity tests, zero unknown/failed browser dispositions, stable duplicate counts after reconnect, and a draining realtime-delivery outbox before widening traffic.
- For mixed-version rollout, publish the Angular client that understands `StateInvalidations` before the API and worker begin emitting the batched contract. An older client ignores that new event name and would otherwise recover those scopes only at its next checkpoint.
- The .NET metrics use `System.Diagnostics.Metrics` and require the deployment's existing OpenTelemetry/Meter listener to include `LegendsLegacy.StateSync` before they are exported. No exporter or environment configuration is added here.
- Include `LegendsLegacy.GameEventOutbox` in that listener as well to export the realtime delivery-lag/retry/failure instruments. Client unknown-name and handler-failure diagnostics are currently local development/staging diagnostics rather than aggregated production telemetry; add a client telemetry sink before relying on them for production alerting.
- The domain-version header is additive and CORS-exposed. No database migration, external configuration change, deployment, or revision-table removal is part of this pass.
- The inventory, Essence, dungeon-reward, marketplace, Guild, Raid, Tournament, Quest, and event-quest changes are additive API/client contracts. Marketplace responses and the semantic event include ordered entity/history change sets. Moving `colosseum` and `raids` from world checkpoint keys to character checkpoint keys, and `guild` from `world:guild` to guild/character/directory keys, requires no schema migration; the new Guild and Raid directory scope keys are ordinary rows created on demand. Tournament and Quest journal events add ordered state-version fields. Old world revision rows are harmless and can be removed by routine maintenance later. No configuration change is required.
