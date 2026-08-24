# Frontend / Backend State Consistency Audit

The frontend converges reliably for most modern, revision-backed domains—but not universally. This audit found six high-risk synchronization defects, primarily involving multi-tab sessions, non-transactional tournament rewards, admin grants, World Tower shared state, and marketplace timing.

## 1. State Synchronization Architecture

The authoritative source is the SQL database through EF Core.

```text
Angular mutation
  → HTTP command
  → MediatR TransactionBehavior
  → database mutation + revision increments + outbox rows
  → transaction commit
  → outbox worker
  → SignalR envelope
  → Angular StateSyncCoordinator
  → authoritative HTTP reload
```

Important implementation details:

- Revisions live in `StateSyncRevisions`, keyed as `character:{id}:{scope}`, `guild:{id}:{scope}`, or `world:{scope}`. The revision field is an EF concurrency token.
- `TransactionBehavior` normally places the mutation, revision changes, and outbox rows in one transaction.
- `StateSyncCommandScopeCatalog` maps commands to affected scopes and distinguishes response-handled authoritative snapshots, response-handled ordered deltas, and scopes requiring realtime invalidation and HTTP reload.
- Successful mutation responses expose changed revisions through `X-LL-Domain-Versions`.
- Realtime messages are normally serialized into an outbox row by `OutboxGameRealtimeBroadcaster`.
- The outbox worker consumes each delivery transactionally and may create a second durable realtime-delivery row.
- Every connection automatically joins its character group; world, guild, raid, and tournament groups are explicit subscriptions.
- Angular's `StateSyncCoordinator` compares revisions monotonically, coalesces invalidations for 50 ms, avoids concurrent reloads per registration, retries failures, and queues another reload if a newer revision arrives during a request.
- Initial startup connects SignalR first, then loads the bootstrap snapshot, then reads the revision checkpoint.
- Reconnect restores groups, reloads bootstrap state, and reads another checkpoint.

### State-domain map

| Domain | Authoritative HTTP state | Revision scope | Principal frontend owner | Realtime role |
|---|---|---|---|---|
| Character | Character/bootstrap endpoints | `character`, `character-overview` | Auth/CharacterStateService | Invalidation; absolute level-up patch |
| Inventory | Inventory endpoint | `inventory` | InventoryStateService | Invalidation; loot is presentation-only |
| Equipment | Equipment endpoint | `equipment` | EquipmentStateService | Invalidation |
| Quests | Journal/area-access endpoints | `quests`, `area-access` | QuestStateService | Versioned authoritative journal |
| Event quests | Event journal | `event-quests` | EventQuestStateService | Refresh signal |
| Achievements/titles | Achievement endpoint | `achievements` | Achievement component | Invalidation plus notification |
| Essences | Essence state endpoints | `essences` | EssenceStateService | Invalidation |
| Soulstones | Soulstone endpoint | `soulstones` | SoulstoneUpgradeStateService | Invalidation |
| Dungeons | Dungeon/run endpoints | `dungeons` | DungeonStateService | Invalidation |
| Prophecies | Prophecy overview | `prophecies` | Prophecy page | Invalidation plus absolute progress patch |
| Marketplace | Marketplace snapshot | `marketplace` | MarketplaceStateService | Ordered delta with gap recovery |
| Guild | Guild endpoints | Several guild scopes | GuildStateService | Invalidation and feature refresh signals |
| Colosseum | Colosseum endpoints | `colosseum`, `tournament` | ColosseumState/component | Invalidation or versioned refresh |
| Raids | Raid endpoints | `raid-directory`; detail uses versions | Route components | Refresh signals |
| World Tower | Tower endpoints | None | Component-local signals | Feature refresh events only |
| Region bosses | Region-boss endpoints | None | Component-local signals | Events plus 15-second polling |
| Character actions | Action endpoint/bootstrap | None | CharacterActionsStateService | Polling |
| Loot history | Loot-history endpoint | None | GameRealtimeStore/component | Direct additions only |

## 2. Overall Assessment

- Can the frontend reliably converge? **Usually, but not always.**
- Are invalidations consistently implemented? **For catalogued modern command families, mostly yes. Non-transactional and uncatalogued paths have concrete gaps.**
- Are several synchronization approaches coexisting? **Yes:** revision invalidation, versioned direct events, unversioned refresh events, direct local patches, and polling.
- Is reconnect safe? **For revision-backed domains, generally yes. World Tower overview and loot history are exceptions.**
- Are races present? **Yes**, particularly response-handled multi-tab state, marketplace snapshot/delta ordering, and group-subscription startup.
- Can a client miss an event and remain stale? **Yes.** A healthy connection does not periodically checkpoint, and several domains have no checkpoint revision at all.

## 3. Critical / High-Risk Findings

### Finding 1 — Response-handled revisions silently exclude other tabs/devices

**State domain:** Inventory, equipment, essences, dungeons, character, colosseum, guild buildings/shop, marketplace-related player state  
**Classification:** A — State correctness bug  
**Severity:** High

**Backend mutation:** Any command whose scope is declared "handled by response," including equip, craft, essence mutation, dungeon reward claim, and arena battle.

**Expected invalidation:** The initiating request may use its response, but every other active connection for the same character must learn that the revision advanced.

**Actual invalidation:** `TransactionBehavior` separates response-handled scopes and calls `AdvanceCharacterScopeAsync`. That path sets `publishRealtime: false` in `StateSyncService`.

**Frontend behavior:** Only the HTTP caller sees the revision header and response. Character audience delivery otherwise targets the SignalR group containing all tabs/devices, but no event is emitted.

**Failure sequence:**

1. Tab A and Tab B are using the same character.
2. Tab A equips an item.
3. Equipment and inventory revisions advance.
4. The command response updates Tab A.
5. No equipment/inventory event is sent to the character group.
6. Tab B remains stale until reconnect, another mutation, or manual refresh.

**Evidence:** The catalog explicitly marks equip/unequip equipment and inventory as response-handled. `StateSyncService.AdvanceCharacterScopeAsync` advances the revision without realtime publication.

**Why it matters:** This affects common gameplay and defeats multi-tab/multi-device synchronization.

**Recommended direction:** Still publish the revision to the character group. The initiating tab can harmlessly ignore an equal revision after accepting its response.

---

### Finding 2 — Tournament reward claims omit character and inventory revisions

**State domain:** Character currencies, arena glory, inventory, tournament rewards  
**Classification:** A — State correctness bug  
**Severity:** High

**Backend mutation:** Claim tournament rewards.

**Database changes:**

- `ArenaProfile.Glory`
- `Character.Cinders`
- `Character.Soulstones`
- `Character.SigilFragments`
- Inventory items
- Reward-grant status

**Expected invalidation:** `character`, `inventory`, and `tournament`.

**Actual invalidation:** Only the tournament revision advances through `SaveCommitAndPublishTournamentEventAsync`.

There is a second defect:

1. `ClaimTournamentRewardsCommand` is `[NonTransactional]`.
2. The service commits the reward mutation.
3. `LootHistoryService.RecordAsync` performs its own `SaveChangesAsync`.
4. The handler then calls `gameRealtime.PublishAsync`.
5. That call only adds a realtime outbox entity; nothing saves afterward.

**Frontend behavior:** The claim component ignores the returned character/inventory state and only shows success. The `LootReceived` handler intentionally does not mutate inventory and expects a matching invalidation.

**Failure sequence:**

1. Claim commits successfully.
2. UI receives success.
3. Tournament status eventually reloads.
4. Character currencies and inventory remain unchanged in Angular.
5. The intended loot event may never be persisted.
6. Reconnect checkpoint cannot detect the omitted character/inventory revisions.

**Recommended direction:** Keep reward mutation, relevant revisions, loot history, and all outbox rows in one transaction; return versioned state or invalidate character and inventory.

---

### Finding 3 — Admin compensation grants never invalidate inventory

**State domain:** Inventory  
**Classification:** A — State correctness bug  
**Severity:** High

**Backend mutation:** Support/admin compensation adds inventory items.

**Expected invalidation:** `inventory` for the affected character.

**Actual invalidation:**

- The administration command is outside the explicitly catalogued feature namespaces.
- The default profile produces character/overview revisions, not inventory.
- It queues `InventoryItemsGranted`.
- `RealtimeInventoryGameEventOutboxConsumer` converts that to `LootReceived`.
- The outbox worker's fallback scope table does not assign `inventory` to this consumer; unknown consumers get no scopes.

**Frontend behavior:** A loot notification appears, but inventory is deliberately not patched.

**Failure sequence:**

1. Support grants an item while the player is online.
2. Database inventory changes.
3. Player receives `LootReceived`.
4. Loot tracker shows the item.
5. Inventory store still lacks it.
6. No inventory revision changed, so reconnect checkpoint also cannot detect the difference.

**Recommended direction:** Make the grant command advance/invalidate inventory, or make the inventory-grant consumer report `inventory` as a changed scope.

---

### Finding 4 — World Tower shared readiness can remain stale indefinitely

**State domain:** World Tower scouting, preparation, floor unlock/readiness  
**Classification:** A — State correctness bug  
**Severity:** High

**Backend mutation:** `ContributeToTowerCommand`.

**Database changes:** Shared `TowerFloorProgress` and `TowerContribution` rows.

**Expected invalidation:** Every player viewing the affected floor should reload Tower overview/detail.

**Actual invalidation:**

- No World Tower revision scope exists in `StateSyncScopes`.
- Contribution does not queue `WorldTowerRallyUpdated`.
- The generic transaction profile instead advances unrelated character/overview scopes.
- Only the initiating component patches its local response.

**Failure sequence:**

1. Player A contributes scouting/preparation.
2. Shared Tower database state changes.
3. Player A applies the returned floor detail.
4. Player B receives no Tower event.
5. Player B continues seeing old readiness and enabled/disabled actions.
6. Backend may reject a subsequent action, but the error path does not reload the stale floor.

Reconnect does not repair the overview because no Tower revision exists and the overview component does not observe reconnect count.

**Recommended direction:** Add one practical Tower overview/readiness scope or extend the existing Tower event so every shared change triggers an authoritative reload. This does not require redesigning Tower state.

---

### Finding 5 — Marketplace delta can be overwritten by an older snapshot

**State domain:** Marketplace listings and buy orders  
**Classification:** C — Race condition  
**Severity:** High

**Backend mutation:** Any marketplace update while a marketplace snapshot GET is in flight.

**Expected behavior:** A newer `MarketplaceChanged` version must not be replaced by an older GET result.

**Actual behavior:**

- Semantic events are version checked and gap-aware.
- The snapshot request uses `refreshVersion`, but applying a realtime delta does not invalidate the active snapshot request.
- Snapshot completion then assigns the complete arrays unconditionally if it is still the latest snapshot request.

**Failure sequence:**

1. GET snapshot starts at revision 20.
2. Mutation commits revision 21.
3. `MarketplaceChanged(21)` arrives and applies.
4. The coordinator marks revision 21 handled.
5. The older snapshot completes and overwrites the revised arrays.
6. No registration appears stale, because revision 21 was already accepted.

**Recommended direction:** Associate marketplace snapshots with their version, or invalidate the active snapshot epoch whenever a newer semantic delta is accepted.

---

### Finding 6 — Guild and tournament group subscription have initial hydration gaps

**State domain:** Guild shared state and Tournament Grounds  
**Classification:** C — Race condition  
**Severity:** Medium

**Expected ordering:**

```text
join audience
→ load snapshot
→ reconcile checkpoint
```

**Actual behavior:**

- Guild state first loads `getMyGuild`; only afterward does an effect request guild group membership.
- Tournament subscription is started without awaiting it, while the component may immediately load state.
- Neither path performs a checkpoint after successful group join.

**Failure sequence:**

1. Snapshot returns revision N.
2. Shared mutation N+1 commits.
3. Event is published before group membership completes.
4. Group join completes.
5. Client has snapshot N and never received N+1.
6. No post-join checkpoint detects the gap.

**Recommended direction:** After first successful guild/tournament subscription, run checkpoint reconciliation, or await subscription before snapshot and reconcile once both complete.

## 4. Mutation → State Consistency Matrix

| Mutation | DB state changed | Revisions/events | Frontend handling | Consistent? |
|---|---|---|---|---|
| Craft items | Inventory, crafting progression, possibly character | Inventory response-version; quests/overview invalidations; crafting outbox | Inventory response, HTTP reloads | Single tab yes; other tabs no |
| Equip/unequip | Equipment, inventory | Both response-versioned; overview invalidated | Response replaces stores | Single tab yes; other tabs no |
| Essence evolve/ascend | Essence, inventory, equipment-derived state | All three response-versioned | Authoritative mutation response | Single tab yes; other tabs no |
| Claim dungeon reward | Dungeon run, inventory, character resources | Dungeon/inventory/character response-versioned; overview invalidated | Response replaces affected stores | Single tab yes; other tabs no |
| Start arena battle | Colosseum, ratings/tickets, character | Colosseum/character response-versioned; battle event | Caller response; opponent absolute patch | Same-character other tabs stale |
| Marketplace trade | Listings/orders, inventories, currencies | Marketplace ordered delta; player revisions | Delta plus response/player reloads | Marketplace race; actor's other tabs stale |
| Guild building change | Guild building, shop availability | Guild-building response version plus group event; shop invalidation | Caller response; members reload | Actor's other tabs can miss building update |
| Raid reward claim | Inventory, character resources, raid reward | Inventory + character/overview invalidations | HTTP reload | Yes |
| Region Boss reward claim | Character cinders/soulstones, grant status | Character/overview invalidation; RegionBoss event | Character reload; boss polling | Yes |
| Tournament reward claim | Character, arena glory, inventory, grant | Tournament only | Tournament reload only | **No** |
| Admin compensation | Inventory | Loot event, no inventory revision | Loot log only | **No** |
| World Tower contribution | Shared floor progress | Character/overview default scopes only | Caller patches local floor | **No** |
| Quest outbox progression | Quest journal, area access, sometimes inventory | Dynamic scopes reported by consumer | Versioned journal/reloads | Yes |
| Transfer inventory item | Sender and receiver inventories | Sender response version; receiver invalidation | Both converge | Yes |
| Clear loot history | Loot-history rows | Unrelated character/overview defaults | Caller clears local store | Other tabs stale |

## 5. Missing or Incorrect Invalidations

The most significant missing scopes are:

- Tournament claim: missing `character` and `inventory`.
- Admin compensation: missing `inventory`.
- World Tower contribution and shared floor progression: missing Tower-domain synchronization.
- Loot-history clear: no loot-history invalidation.
- Same-character secondary sessions: all response-handled scopes lack live notification.
- SignalR-disabled mode: no bootstrap hydration, discussed below.

Secondary consequences in the mainstream catalog are otherwise covered well: transfer examines both changed inventories; marketplace extracts affected character IDs; guild audience discovery inspects changed guild entities; quest outbox processing dynamically adds inventory/area-access scopes when needed.

## 6. Revision System Findings

What is correct:

- Mutation, revisions, and outbox rows are normally committed together.
- Scope locks plus EF concurrency tokens protect concurrent increments.
- The client uses `revision > current`, never regressing on older events.
- The checkpoint includes character, world, and current-guild keys.
- Guild identity changes reset guild scopes and reconcile again.
- Lost invalidation during reload is handled: after completion, a newer known revision schedules another reload.

Problems:

1. "Response handled" currently means "do not notify anyone," rather than "the HTTP caller handled it."
2. Non-transactional paths can bypass revision creation entirely.
3. World Tower and loot history have no revision domains.
4. Registration initializes `lastRefreshRevision` to the coordinator's current revision. A newly registered owner with an old retained cache can therefore be treated as current.

A concrete case for point 4 is Tournament Grounds:

- Its root-provided view-state service retains snapshots.
- The component unregisters when leaving.
- Events can advance the coordinator while the component is absent.
- On return, registration starts at the latest revision.
- `preserveTournamentGrounds` can skip the initial fetch.

**Classification:** B — Reconciliation weakness  
**Severity:** Medium

## 7. HTTP / SignalR Race Conditions

The generic HTTP mutation path is relatively strong:

- The interceptor observes revision headers before subscribers apply state.
- State owners check `DomainVersionTracker`.
- A late older mutation response is rejected.
- Unhandled mutation scopes force an authoritative reload in a microtask.
- Inventory/equipment/essence services use request epochs to stop older GETs replacing newer responses.

Concrete remaining races:

- Marketplace snapshot versus semantic delta: high-risk finding above.
- Initial guild/tournament snapshot versus group join: finding above.
- Tournament component permits concurrent manual/coordinator snapshots without an epoch; a slower older request can overwrite a newer one.
- World Tower overview fires independent GETs for every rally event without request versioning. Several close events can complete out of order. The later database state often masks this, but the client has no guarantee.

## 8. Reconnection & Recovery

The normal revision-backed reconnect path is good:

1. SignalR automatically reconnects.
2. Guild/raid/world/tournament memberships are restored.
3. `reconnectCount` changes only after subscription restoration.
4. Bootstrap is force reloaded.
5. The state checkpoint is read.
6. Higher revisions trigger feature reloads.

The coordinator also reconciles when the browser becomes online and after focus returns from a blur lasting at least five minutes.

### Can a client miss an event and remain permanently stale?

**Yes, in three situations:**

- A response-handled revision in another tab while SignalR stays healthy.
- World Tower overview or loot history, because checkpoints contain no corresponding scope.
- A realtime delivery dead-letters and the client never reconnects, changes network state, leaves focus for five minutes, or receives a later revision.

The outbox stops retrying after five attempts and marks delivery failed. There is no periodic checkpoint for a continuously connected active tab. This is a reconciliation weakness rather than a transactional data-loss bug.

**Classification:** B — Reconciliation weakness  
**Severity:** Medium

## 9. Frontend State Ownership

Generally healthy owners:

- Character summary: AuthService/CharacterStateService.
- Inventory: InventoryStateService.
- Equipment: EquipmentStateService.
- Essences, dungeons, guild, marketplace, colosseum: one root state service each.
- Derived inventory/equipment favourite state is explicitly synchronized.

Risk areas:

- World Tower, Region Boss, Raid, Tournament, achievements, and prophecies use component-local caches.
- Region Boss mitigates this with 15-second polling.
- Raid/Tower rally pages explicitly reload on reconnect.
- World Tower overview does neither.
- Tournament has a duplicate root-owned `TournamentGroundsViewStateService` plus component registration; retained snapshots interact badly with registration revision initialization.
- Loot history is held in `GameRealtimeStore`, while clear/load operations are component-driven and have no domain revision.

## 10. Realtime Producer / Consumer Matrix

| Event | Backend producer/audience | Frontend consumer | Purpose | Status |
|---|---|---|---|---|
| `StateInvalidated` / `StateInvalidations` | StateSyncService → character/guild/world | StateSyncCoordinator | HTTP refresh | Active |
| `LootReceived` | Inventory outbox → character | GameRealtimeStore | Loot presentation | Active, frequently lacks matching inventory invalidation |
| `QuestJournalChanged` | Quest service → character | QuestStateService | Versioned authoritative snapshot | Active |
| `EventQuestChanged` | Event quest service → character | EventQuestStateService | Refresh trigger | Active |
| `CharacterLevelUp` | Outbox consumer → character | CharacterStateService | Absolute character patch | Active |
| `MarketplaceChanged` | Marketplace publisher → world | MarketplaceStateService | Ordered delta | Active, snapshot race |
| `GuildBuildingsChanged` | Guild commands → guild | GuildStateService | Feature refresh | Active |
| `GuildMissionsChanged` | Guild mission outbox → guild | GuildStateService | Feature refresh | Active |
| `TournamentGroundsUpdated` | Tournament outbox → tournament group | Tournament component/coordinator | Versioned refresh | Active |
| `WorldTowerRallyUpdated` | Tower outbox → world | Tower components | Refresh signal | Active but incomplete domain coverage |
| `WorldTowerCombatFrameUpdated` | Immediate publisher → world | Tower rally | Transient playback | Active/intentional |
| `RaidUpdated` | Raid outbox → raid | Raid page | Refresh signal | Active |
| `RaidDirectoryUpdated` | Raid consumer → world | Region raid list | Directory refresh | Active |
| `RegionBossUpdated` | Region Boss service → world/character | Region Boss component | Refresh signal | Active with polling fallback |
| `ArenaBattleCompleted` | Colosseum handler → participants | ColosseumStateService | Opponent rating patch | Active; actor-tab suppression issue |
| `ProphecyProgressed` | Prophecy notification handler → character | Prophecy page | Absolute progress patch/toast | Active |
| `AchievementUnlocked` | Achievement service → character/world | Chat notification path | Notification | Active |
| `PlayerTransfer` | Transfer commands → character | Chat path | Notification | Active |
| `GuildInviteRejected` | Guild command → character | No operational consumer found | Unknown/obsolete | Dead |
| `GuildStateChanged` | Guild commands → guild | No operational consumer found | Legacy refresh signal | Dead |
| `GuildMembershipChanged` | Guild commands → character | No operational consumer found | Legacy signal | Dead |
| `GuildDisbanded` | Guild command → guild | No operational consumer found | Legacy signal | Dead |
| `GuildVaultChatMessage` | Guild vault commands → guild | No game-realtime consumer found | Legacy chat bridge | Likely obsolete |

The persistent effects of the dead guild signals are mostly covered by state revisions, so these are classified E/Low rather than correctness defects.

## 11. Background / Shared-State Synchronization

Strong paths:

- Quest, achievement, and event-quest outbox consumers mutate state and create relevant revisions within their delivery transaction.
- Tournament background progression advances the tournament revision and queues the event before commit.
- Raid resolution emits durable raid and directory updates.
- Region Boss uses events plus polling.
- Character action completion is polled and its secondary quest/inventory effects are outbox-driven.

Weak paths:

- World Tower floor readiness is shared but only rally lifecycle changes produce the Tower realtime event.
- Admin compensation changes player inventory without an active game HTTP request and lacks inventory invalidation.
- Tournament reward claim commits player state through a non-transactional command.
- Guild/tournament feature groups have an initial subscription gap.

## 12. Synchronization Inefficiencies

### Premature quest invalidations

Crafting and several dungeon commands list `quests` immediately in their catalog profile, while actual quest progression happens later in the outbox consumer. This can produce:

```text
command commit
→ quest reload before quest consumer changes anything
→ outbox quest mutation
→ second quest revision/reload
```

Correctness is preserved; the first request is usually redundant.

**Classification:** D — Synchronization inefficiency  
**Severity:** Low/Medium on hot combat/crafting paths

### Uncatalogued World Tower commands over-invalidate character state

The default command profile refreshes character and character overview. Transactional World Tower rally commands carry `CharacterId` but mostly mutate Tower tables, so they can trigger unrelated character/overview reloads.

**Classification:** D — Synchronization inefficiency  
**Severity:** Low

### Favourite updates invalidate all equipment

`SetInventoryItemFavoriteCommand` always includes the equipment scope because an item can be equipped. Favouriting an ordinary inventory item consequently reloads equipment unnecessarily.

**Classification:** D — Synchronization inefficiency  
**Severity:** Low

## 13. Architectural Generations

The repository currently contains four synchronization generations:

1. **Current model:** `HTTP response/snapshot + domain revision + transactional outbox + invalidation`. Used by inventory, equipment, essences, dungeons, guild scopes, and soulstones.
2. **Versioned direct state/delta:** `QuestJournalChanged`, `MarketplaceChanged`, and Tournament updates. These carry revisions and can reconcile gaps.
3. **Unversioned feature refresh signals:** Guild feature events, World Tower rally events, raid detail events, and Region Boss events.
4. **Polling/component-local state:** Character actions and Region Bosses, with portions of Tower/Raid presentation state also component-owned.

The current intended architecture is clearly generation 1, with generation 2 retained for deliberate authoritative snapshots or ordered deltas.

## 14. Things Investigated That Are Fine

- `WorldTowerCombatFrameUpdated` bypasses the outbox intentionally. It is transient playback state, and the client detects sequence gaps and fetches missing frames.
- `LootReceived` intentionally does not mutate inventory. This prevents snapshot-plus-delta double grants. The defect is missing invalidations on specific producers, not that frontend choice.
- Character level-up events use absolute values and ignore lower/equal stale updates, making duplicates harmless.
- Marketplace direct deltas detect duplicate, older, and skipped versions; the identified issue is specifically interaction with an older snapshot GET.
- Transfer inventory examines changed inventory rows, so both sender and recipient are included; only the initiating player's response-handled scope is suppressed.
- Guild audience IDs are discovered from changed tracked entities, including removed members, rather than only querying post-mutation membership.
- Revisions and realtime outbox entries are created before commit on ordinary transactional commands.
- Revision invalidations are idempotent under duplicate delivery.
- The coordinator handles invalidation-during-reload correctly and retries failed HTTP reloads.
- Reconnect restores group membership before checkpoint reconciliation.

## 15. Recommended Cleanup Order

1. Publish response-handled revisions to character/guild audiences so other sessions converge; let the initiating client ignore the equal revision.
2. Make tournament reward claim one transaction covering reward mutation, character/inventory revisions, loot history, and outbox rows.
3. Ensure `InventoryItemsGranted` always produces inventory invalidation; explicitly cover admin compensation.
4. Add reliable synchronization for World Tower shared overview/readiness changes and reconnect.
5. Reconcile immediately after first guild/tournament group subscription.
6. Protect marketplace and tournament snapshots with request epochs or response versions.
7. Correct registration semantics for retained caches; do not assume a newly registered owner has applied the coordinator's current revision.
8. Add a periodic or lifecycle checkpoint for continuously connected sessions if dead-letter recovery matters operationally.
9. Give loot history a small reconciliation path, or reload it on reconnect/clear events.
10. Remove or formally document dead guild realtime contracts.
11. Remove premature quest invalidations and unrelated default character reloads after correctness fixes are complete.

## Verification and Repository Impact

The audit was performed using read-only repository-wide producer/consumer, revision, mutation, SignalR, and state-owner searches.

- No application code was changed as part of the audit.
- No tests were run because the original audit prohibited file modifications and test execution would generate build artifacts.
- No migrations were generated or applied.
- No configuration or deployment changes were made.
- The pre-existing untracked files `docs/gathering-level-progression.md` and `docs/gathering-system-progression-analysis.md` were not touched.
