# Error Handling & Observability Audit

Audit type: static, analysis-only repository audit  
Scope: .NET APIs, Angular frontend, PostgreSQL, SignalR, transactional outbox, background workers/Quartz, health checks, metrics, and Kubernetes manifests

No files were modified as part of the audit itself. The audit identified **5 High** and **10 Medium** findings. No substantiated Critical issue was found.

## 1. Current Error & Observability Architecture

- API requests pass through request-ID logging, ASP.NET exception handling, authentication/identity scopes, controllers, and a global response filter.
- Expected gameplay failures normally use `Response<T>.Fail(string)`.
- `ExceptionToResponseBehaviour` sits outside `TransactionBehavior` and converts exceptions from handlers returning compatible response types into failed responses.
- Successful `Response<T>` values become their bare data; failed responses become a bare-string `400`.
- Unconverted exceptions reach `UseExceptionHandler()` and Problem Details. Concurrency conflicts and one dungeon unique constraint are mapped to `409`.
- Request logs use structured JSON scopes containing trace/request ID and, after authentication, account and character IDs.
- Mutating MediatR commands normally run in a PostgreSQL transaction with character serialization, state-sync invalidation, and transactional outbox writes.
- The outbox uses per-consumer delivery records, `SKIP LOCKED`, bounded retries, failure metadata, stale-processing recovery, and retention.
- Quartz uses a persistent clustered store and durable execution records.
- Angular has centralized HTTP formatting and 401 refresh/replay, but individual feature services frequently replace errors.
- Game and chat SignalR clients reconnect automatically. Game realtime performs bootstrap and state reconciliation after reconnect.
- `System.Diagnostics.Metrics` and `ActivitySource` instruments exist, but no repository-configured listener/exporter was found.
- Main API readiness checks account-restriction snapshot health. Chat readiness has no registered checks. The standalone Quartz worker has no probe endpoint.

## 2. Overall Assessment

Failures are **not handled predictably across all API paths**. Uncaught transactional exceptions usually roll back correctly, but may then be misrepresented as expected `400` failures. Registration is worse: local catches can permit partial work to commit while returning failure.

Important exceptions are swallowed in a few concrete places. Production backend logs are generally well structured when an exception reaches a logging boundary, but the global response conversion prevents many important exceptions from reaching one. Ordinary `400` request logs are also filtered by the production `Warning` threshold.

Asynchronous failures are substantially more observable than request failures: the outbox and Quartz retain useful database evidence. Proactive worker-health visibility remains incomplete.

Player incidents can sometimes be reconstructed:

- PvP and guild transactions retain relatively strong evidence.
- Dungeon item rewards are partially reconstructable.
- Currency, XP, resource consumption, crafting inputs, and some essence operations are much weaker.
- There are multiple competing response formats and no stable application error codes.

## 3. Critical / High-Risk Findings

### Finding 1 — Unexpected exceptions become unlogged client-visible 400s

**Finding:** The intended global exception boundary is bypassed for most handlers returning `Response<T>`.  
**Location:** `ExceptionToResponseBehaviour`, `TransactionBehavior`, `ResponseResultFilter`, production logging.  
**Classification:** A — Error Handling Bug; B — Diagnostic Blind Spot; D — Logging Quality Problem  
**Severity:** High

**Operation:** Any MediatR request returning `Response<T>`.  
**Current failure behavior:** The transaction rolls back and rethrows; the outer behavior catches every exception and calls `Fail(ex.Message)`. The result filter then returns a bare-string `400`.  
**Current logging/telemetry:** No exception is logged by the behavior. The request appears as an Information-level `400`, which production's `Warning` threshold suppresses.  
**What the caller sees:** Raw exception text with `400 Bad Request`. Provider or implementation details may leak.  
**What production operators see:** Frequently nothing beyond downstream database telemetry, if any.

**Failure scenario:** A database timeout or programming defect occurs while crafting. Persistent changes roll back, but the player sees a business-like `400` containing the exception message. There is no stack trace or searchable error record.  
**Evidence:** [ExceptionToResponseBehaviour.cs](../LL/src/Core/Application/MediatR/Behaviors/ExceptionToResponseBehaviour.cs), [DependencyInjection.cs](../LL/src/Core/Application/DependencyInjection.cs), [ResponseResultFilter.cs](../LL/src/API/API.LL/Filters/ResponseResultFilter.cs), [appsettings.Production.json](../LL/src/API/API.LL/appsettings.Production.json).  
**Why this matters:** Infrastructure, programming, and business failures become indistinguishable; the safest rollback path becomes one of the least observable.  
**Recommended direction:** Only convert deliberately recognized application/business failures. Let unexpected exceptions reach the global handler, log them once, return safe `500` Problem Details, and preserve the request ID.

### Finding 2 — Registration can commit partial state after an exception

**Finding:** Registration and guest-login handlers catch exceptions inside the transaction and return failure, allowing the transaction pipeline to commit prior changes.  
**Location:** `RegisterCommandHandler`, `GuestLoginCommandHandler`, `TransactionBehavior`.  
**Classification:** A — Error Handling Bug  
**Severity:** High

**Operation:** Account, character, inventory, attributes, outbox, and token creation.  
**Current failure behavior:** User and character initialization runs before token issuance. A later exception is caught locally and converted into `Response.Fail`; the transaction sees normal completion and commits.  
**Current logging/telemetry:** Neither catch logs the exception. Registration returns the raw exception message; guest login returns `Token Error`.  
**What the caller sees:** Registration failure and no usable session.  
**What production operators see:** Persisted user/character/outbox state but no causal exception log.

**Failure scenario:** The guild lookup in `IssueTokens` fails after user/character initialization. The caller believes registration failed, but the account and character may exist, causing a retry to report duplicate email/name.  
**Evidence:** [RegisterCommand.cs](../LL/src/Core/Application/UseCases/Users/Commands/Register/RegisterCommand.cs), [GuestLoginCommand.cs](../LL/src/Core/Application/UseCases/Users/Commands/GuestLogin/GuestLoginCommand.cs), [UserCreatedEventHandler.cs](../LL/src/Core/Application/UseCases/Characters/EventHandlers/UserCreatedEventHandler.cs), [JwtGenerator.cs](../LL/src/Infrastructure/Service/Services.LL/Authorization/JwtGenerator.cs), [TransactionBehavior.cs](../LL/src/Core/Application/MediatR/Behaviors/TransactionBehavior.cs).  
**Why this matters:** The error response does not describe the committed state and leaves a major diagnostic dead end.  
**Recommended direction:** Remove broad local catches around transactional registration. Make unsuccessful transactional responses roll back when state has changed, or ensure all expected failures occur before mutation.

### Finding 3 — Valuable repeatable mutations lack operation idempotency

**Finding:** Crafting and champion-market purchases cannot distinguish a retry from a new operation after an ambiguous committed response.  
**Location:** `CraftingService.CraftItemsAsync`, `ColosseumService.PurchaseChampionMarketItemAsync`.  
**Classification:** E — Resilience Issue; B — Diagnostic Blind Spot  
**Severity:** High

**Operation:** Consume materials/glory and grant crafted or purchased items.  
**Current failure behavior:** The transaction is atomic, but there is no client operation key or result receipt. A dropped response after commit leaves the client uncertain.  
**Current logging/telemetry:** Crafted outputs have acquisition records; market purchases have a purchase row. Crafting input debits and a unified operation record are absent.  
**What the caller sees:** Network failure, then may retry.  
**What production operators see:** Separate completed mutations, without evidence that the later one was a retry of the same intent.

**Failure scenario:** Crafting commits, the response connection drops, and the player retries. Materials are consumed and outputs granted twice if sufficient inventory remains.  
**Evidence:** [CraftingService.cs](../LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/CraftingService.cs), [InventoryRepository.cs](../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Inventories/InventoryRepository.cs), [ColosseumService.cs](../LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs).  
**Why this matters:** A legitimate retry can repeat a high-value operation, and support cannot establish whether duplicate intent or transport ambiguity caused it.  
**Recommended direction:** Add idempotency only to selected high-value repeatable mutations, using a client operation ID and persisted result/receipt. Dungeon reward claims already have natural idempotency and do not need the same mechanism.

### Finding 4 — Important economy debits and dungeon currency/XP are not reconstructable

**Finding:** Existing audit data is strongest for acquired items, but not for consumed resources, currency, XP, or the exact dungeon reward calculation.  
**Location:** Inventory economy ledger, dungeon reward claim, crafting, essence operations.  
**Classification:** B — Diagnostic Blind Spot  
**Severity:** High

**Operation:** Dungeon rewards, crafting input consumption, essence dust/catalyst spending, and currency changes.  
**Current failure behavior:** Mutations can be correct and atomic, but the prior state and exact debit/credit operation are not retained. Dungeon runs are deleted after claim.  
**Current logging/telemetry:** Item grants receive acquisition ledger entries; dungeon loot history is limited and player-clearable. Dungeon completion records retain dungeon and time but not calculated rewards.  
**What the caller sees:** A balance/inventory change or alleged missing reward.  
**What production operators see:** Current state and partial item provenance, but not a complete before/after economic record.

**Failure scenario:** A player reports later that dungeon cinders or XP were missing. Completion can be established, but the deleted run's pending XP/currency and exact claim outcome cannot.  
**Evidence:** [DungeonRunRewardClaimer.cs](../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonRunRewardClaimer.cs), [DungeonRunService.cs](../LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs), [DungeonCompletionRecord.cs](../LL/src/Core/Domain/Models/Dungeons/Runs/DungeonCompletionRecord.cs), [LootHistoryService.cs](../LL/src/Infrastructure/Service/Services.LL/Inventories/LootHistoryService.cs), [EssenceSystemService.cs](../LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs).  
**Why this matters:** Support cannot prove the exact valuable state change without reproducing an old calculation.  
**Recommended direction:** Extend the existing ledger selectively: record resource debits/credits and an operation reference for purchases, crafting, essence spending, and dungeon claims. Snapshot the compact claim result before deleting the run.

### Finding 5 — Deployable default authentication secrets lack validation

**Finding:** Main API and Chat contain committed default JWT signing configuration that production inherits unless overridden, without startup validation.  
**Location:** Main and Chat `appsettings.json`, JWT setup in both `Program.cs` files.  
**Classification:** A — Error Handling/Configuration Bug  
**Severity:** High if defaults can reach a deployed environment

**Operation:** JWT creation and validation.  
**Current failure behavior:** The configuration is dereferenced with `!`; issuer/audience validation becomes optional when empty. No production validation rejects a default or inadequate signing key.  
**Current logging/telemetry:** No startup warning indicates that deployable defaults are active.  
**What the caller sees:** Normal authentication.  
**What production operators see:** A healthy service even if an unsafe inherited default is used.

**Failure scenario:** Environment injection is missing during a deployment and both services start using repository defaults.  
**Evidence:** [API Program.cs](../LL/src/API/API.LL/Program.cs), [Chat Program.cs](../LL-Chat/API/API.Chat/Program.cs), [API appsettings.json](../LL/src/API/API.LL/appsettings.json), [Chat appsettings.json](../LL-Chat/API/API.Chat/appsettings.json).  
**Why this matters:** Repository inspection cannot prove deployed values, but the service does not fail safely when production injection is absent.  
**Recommended direction:** Remove deployable signing defaults, require and validate production JWT configuration at startup, and rotate any key that may have been used outside local development.

## 4. Swallowed / Mishandled Exceptions

### Finding 6 — Tournament advisory-lock failures are silently ignored

**Finding:** `PostgresTournamentLockService` catches every lock failure without logging.  
**Location:** `ExecuteLockAsync`.  
**Classification:** A — Error Handling Bug; E — Resilience Issue  
**Severity:** Medium

**Operation:** Tournament schedule and per-tournament serialization.  
**Current failure behavior:** Processing continues without the configured advisory lock.  
**Current logging/telemetry:** None.  
**What the caller sees:** Usually normal progression.  
**What production operators see:** No evidence that lock protection was lost.

**Failure scenario:** A production permission or connection issue prevents lock acquisition while multiple replicas progress the same tournament.  
**Evidence:** [PostgresTournamentLockService.cs](../LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/PostgresTournamentLockService.cs).  
**Why this matters:** A fallback intended for tests also hides unexpected production failures.  
**Recommended direction:** Only tolerate explicitly recognized unsupported-provider cases; log and fail the operation for production PostgreSQL lock failures.

### Finding 7 — Corrupt account-risk JSON silently becomes empty evidence

**Finding:** Persisted account-risk JSON deserialization failures return empty collections or `null`.  
**Location:** `AccountRiskRepository.Deserialize`, `LiveOpsAccountRiskService.TryReadStatusRequest`.  
**Classification:** B — Diagnostic Blind Spot  
**Severity:** Medium

**Operation:** LiveOps risk investigation and idempotent status operations.  
**Current failure behavior:** Corrupt or schema-incompatible evidence disappears from the returned view.  
**Current logging/telemetry:** None.  
**What the caller sees:** Empty signals/relationships or an operation mismatch.  
**What production operators see:** No indication that persisted evidence failed to deserialize.

**Failure scenario:** A schema change makes an old risk snapshot unreadable, and an investigation appears to have no signals.  
**Evidence:** [AccountRiskRepository.cs](../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Administration/AccountRiskRepository.cs), [LiveOpsAccountRiskService.cs](../LL/src/Infrastructure/Service/Services.LL/Administration/LiveOpsAccountRiskService.cs).  
**Why this matters:** Data corruption is misrepresented as valid absence.  
**Recommended direction:** Preserve best-effort UI behavior, but surface a structured warning/error marker with account/snapshot identity.

A lower-severity example is [SoulstoneUpgradeDefinitionProvider.TryReload](../LL/src/Infrastructure/Service/Services.LL/Providers/SoulstoneUpgradeDefinitionProvider.cs): its ignored continuation can fail without logging, leaving the previous cache active.

## 5. Error Model Consistency

| Failure category                                         | Current representation                  |          Typical HTTP result |
| -------------------------------------------------------- | --------------------------------------- | ---------------------------: |
| Expected gameplay failure                                | `Response.Fail(human message)`          |              Bare-string 400 |
| Request/model validation                                 | ASP.NET Validation Problem Details      | 400 with `errors` dictionary |
| Authorization                                            | Framework/custom Problem Details        |                      401/403 |
| Concurrency or duplicate dungeon start                   | Problem Details with request ID         |                          409 |
| Missing domain resource                                  | Often exception converted to response   |              Bare-string 400 |
| Infrastructure/unexpected error in `Response<T>` handler | Exception message converted to response |              Bare-string 400 |
| Unexpected error outside that model                      | Global Problem Details                  |                          500 |

### Finding 8 — Fundamentally different failures share the same contract

**Finding:** Expected business rejection, missing resources, infrastructure faults, and programming defects can all become identical string `400`s.  
**Location:** `Response<T>`, response filter, exception behavior, repository exception use.  
**Classification:** A — Error Handling Bug; B — Diagnostic Blind Spot  
**Severity:** Medium

**Operation:** Gameplay API commands and queries.  
**Current failure behavior:** There is no stable code or category in `Response<T>`.  
**Current logging/telemetry:** Business and unexpected failures cannot be reliably separated by status or payload.  
**What the caller sees:** Human text that must be displayed or parsed.  
**What production operators see:** Often just an undifferentiated `400`, if Information logs are enabled.

**Failure scenario:** `Not enough materials` and a PostgreSQL timeout both reach Angular as string `400`s.  
**Evidence:** [Response.cs](../LL/src/Core/Common/Primitives/Response.cs), [ResponseResultFilter.cs](../LL/src/API/API.LL/Filters/ResponseResultFilter.cs), [ConcurrencyExceptionHandler.cs](../LL/src/API/API.LL/Common/ConcurrencyExceptionHandler.cs).  
**Why this matters:** Frontend recovery and production failure-rate classification cannot be dependable.  
**Recommended direction:** Introduce a small set of stable categories/codes where clients need branching, while retaining human messages. Do not build a code for every prose variation.

Database handling is similarly narrow: deliberate mapping exists for optimistic concurrency and one dungeon uniqueness constraint, while other unique/FK/update failures fall into the generic path. No EF transient retry is configured, which is safer than automatically replaying these non-idempotent commands.

## 6. Structured Logging & Context

The logging foundation is mostly good:

- [RequestLoggingMiddleware](../LL/src/API/API.LL/Common/RequestLoggingMiddleware.cs) creates `TraceId`, `SpanId`, and `RequestId` scopes.
- Authenticated requests add `AccountId` and `CharacterId`.
- `TransactionBehavior` adds the MediatR operation name.
- Outbox processing scopes include delivery, message, event, consumer, character/account, and origin trace.
- Quartz execution records include business key, attempts, timing, and full error information.
- Production logs are JSON console output with scopes, appropriate for containers.
- No material pattern of interpolated logger templates was found in high-value paths.

The main logging problem is boundary loss: once an exception becomes a failed response, the stack and exception type disappear. Adding more handler logs would create duplication; repairing the boundary is the higher-value action.

## 7. Diagnostic Dead Ends

The most consequential dead ends are:

- **Partial registration:** committed account/character data plus a failure response, without the caught exception.
- **Dungeon XP/currency dispute:** completion is known, but the deleted run's exact claim state and returned response are not.
- **Crafting resource dispute:** outputs are partially auditable; input material debits and operation identity are not.
- **Essence dust/catalyst spending:** current state exists, but no durable operation record establishes what was consumed and when.
- **Frontend-only crash/realtime handler failure:** browser console evidence disappears when the session ends.
- **Account-risk deserialization:** corrupt evidence is presented as empty evidence.

The missing information is not a log for every action. It is a compact operation identity, valuable debit/credit evidence, commit/result status, and an unexpected-exception record tied to request/character.

## 8. Background Worker / Outbox Observability

### Finding 9 — An outbox repository failure can terminate the API host

**Finding:** The outer outbox loop catches shutdown cancellation only. Claim, cleanup, or retry-state persistence failures can escape `BackgroundService`.  
**Location:** `GameEventOutboxWorker.ExecuteAsync`.  
**Classification:** E — Resilience Issue  
**Severity:** Medium

**Operation:** Claiming and maintaining outbox deliveries.  
**Current failure behavior:** Per-delivery consumer failures are handled well, but batch-level database failures escape. With the default host behavior, this stops the API host.  
**Current logging/telemetry:** Host infrastructure should log termination; the worker itself provides no bounded outer retry context.  
**What the caller sees:** Temporary API outage/restart.  
**What production operators see:** Pod restarts or CrashLoopBackOff rather than a degraded worker.

**Failure scenario:** A brief database outage causes claim to throw on every restart before connectivity stabilizes.  
**Evidence:** [GameEventOutboxWorker.cs](../LL/src/API/API.LL/HostedServices/GameEventOutboxWorker.cs), [API.LL.csproj](../LL/src/API/API.LL/API.LL.csproj).  
**Why this matters:** A recoverable worker dependency failure is promoted to full API unavailability.  
**Recommended direction:** Add a bounded outer-loop transient failure policy with cancellation-aware backoff and a consecutive-failure signal. Continue allowing genuinely fatal configuration/programming failures to terminate the host.

### Finding 10 — Critical worker progress is not represented by health

**Finding:** The standalone Quartz deployment has no probe endpoint, and API readiness does not represent outbox, raid, tower, or general worker progress.  
**Location:** Worker deployment, API health registration, background execution storage.  
**Classification:** C — Observability Gap  
**Severity:** Medium

**Operation:** Scheduled progression, outbox delivery, raids, and world-tower processing.  
**Current failure behavior:** Processes may remain running while a subsystem repeatedly fails or makes no progress.  
**Current logging/telemetry:** Quartz execution rows and outbox LiveOps counts exist, but are not part of a worker-health signal.  
**What the caller sees:** Stale scheduled systems or delayed updates.  
**What production operators see:** Healthy API pods unless separately reviewing logs/LiveOps/database state.

**Failure scenario:** Quartz remains alive but an important job fails every run; no readiness or last-success signal changes.  
**Evidence:** [deployment-worker.yaml](../LL/deploy/ll-backend/templates/deployment-worker.yaml), [BackgroundJobExecutionService.cs](../LL/src/Infrastructure/Persistence/Persistence.LL/BackgroundJobs/BackgroundJobExecutionService.cs), [API Program.cs](../LL/src/API/API.LL/Program.cs).  
**Why this matters:** Durable forensic data is not equivalent to proactive failure detection.  
**Recommended direction:** Expose only the highest-value progress signals: last success/age for critical jobs, outbox oldest pending, and consecutive worker failures.

The outbox's record-level design is otherwise robust:

- `FOR UPDATE SKIP LOCKED` supports replicas.
- Processing claims older than five minutes are recoverable.
- Retries are bounded to five attempts.
- Attempts and truncated full exception information are persisted.
- Failed deliveries are retained for 30 days; processed deliveries for seven.
- A poison delivery does not block subsequent deliveries.
- LiveOps exposes pending/processing count, failed count, and oldest pending.
- Shutdown cancellation leaves claims recoverable.

## 9. SignalR / Realtime Failures

### Finding 11 — Chat can report successful invocation while dropping or hiding a send

**Finding:** Chat rate limiting returns normally, and malformed room checks can return after message persistence.  
**Location:** `ChatHub.Send`, `SendMessageCommandHandler`.  
**Classification:** A — Error Handling Bug  
**Severity:** Medium

**Operation:** Sending chat messages.  
**Current failure behavior:** A rate-limited send simply returns. Message persistence precedes SignalR broadcast, so a broadcast failure can also leave a stored message while invocation fails.  
**Current logging/telemetry:** No rate-limit rejection response or structured event. Persisted messages allow later recovery for broadcast failures.  
**What the caller sees:** A successful hub invocation with no sent message, or an invocation error after persistence.  
**What production operators see:** Little evidence for rate-limit drops; a persisted message for broadcast ambiguity.

**Failure scenario:** A player sends during the rate limit and believes the message succeeded because no `HubException` is returned.  
**Evidence:** [ChatHub.cs](../LL-Chat/API/API.Chat/Hubs/ChatHub.cs), [SendMessageCommand.cs](../LL-Chat/Core/Application/UsesCases/Chats/Commands/SendMessage/SendMessageCommand.cs), [ChatMessageRepository.cs](../LL-Chat/Infrastructure/Persistence/Persistence.Chat/Repositories/ChatMessageRepository.cs).  
**Why this matters:** The caller's belief differs from persistent/broadcast state.  
**Recommended direction:** Return an explicit expected hub error for rejected sends. Validate channel context before persistence and retain message IDs for idempotent retries.

Game realtime recovery is comparatively strong:

- Automatic reconnect has capped delays.
- Group subscriptions are restored.
- Reconnect triggers bootstrap plus state-sync reconciliation.
- Events carry update IDs and are deduplicated client-side.
- Failed publication may be retried, and duplicate realtime delivery is tolerated.
- Ordinary disconnects are not promoted to server errors.

## 10. Frontend Error Handling

### Finding 12 — Angular loses backend error identity and has no production crash evidence

**Finding:** The centralized formatter handles several shapes but does not preserve request IDs; many services then replace the error. No production global frontend telemetry was found.  
**Location:** `ApiService`, `GuildService`, `ColosseumService`, Chat API service, realtime diagnostics.  
**Classification:** B — Diagnostic Blind Spot; A — Error Handling Bug  
**Severity:** Medium

**Operation:** HTTP failures and uncaught browser/realtime handler failures.  
**Current failure behavior:** Raw strings and `detail` are recognized, but ASP.NET validation's error dictionary is not. Feature services frequently create generic `Error` objects and discard status, backend detail, and response headers.  
**Current logging/telemetry:** Mostly browser `console.warn`; game realtime diagnostics are disabled in production. No custom Angular `ErrorHandler` or external frontend telemetry was found.  
**What the caller sees:** Inconsistent generic messages; sometimes raw backend exception text.  
**What production operators see:** No browser-side exception or request-ID evidence after the session ends.

**Failure scenario:** A guild purchase fails with a useful backend reason and request ID, but `GuildService` replaces it with `Failed to purchase guild item`.  
**Evidence:** [api.service.ts](../LL/src/Presentation/ll/src/app/core/services/api/api.service.ts), [guild.service.ts](../LL/src/Presentation/ll/src/app/core/services/api/guild/guild.service.ts), [colosseum.service.ts](../LL/src/Presentation/ll/src/app/core/services/api/colosseum/colosseum.service.ts), [chat-api.service.ts](../LL/src/Presentation/ll/src/app/core/services/ll-chat/chat-api.service.ts), [game-realtime-diagnostics.service.ts](../LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/game-realtime-diagnostics.service.ts).  
**Why this matters:** Support cannot connect a player screenshot or message to backend logs, and components cannot reliably distinguish recoverable business errors.  
**Recommended direction:** Preserve a normalized error containing status, stable code/category, safe message, and request ID. Avoid replacing it in feature services. Add a minimal production global error boundary only if browser-side incident evidence is operationally desired.

The 401 interceptor retries queued requests once after refresh. That is normally safe because authorization rejects before controller execution; it does not retry general network/5xx failures.

## 11. Correlation / Traceability

### Finding 13 — Trace identity breaks at the frontend and during chained outbox work

**Finding:** HTTP trace IDs are persisted on initial outbox messages, but are not surfaced by Angular and are not established as a worker parent context.  
**Location:** Request middleware, outbox enqueue, worker activity, Angular HTTP formatter.  
**Classification:** B — Diagnostic Blind Spot  
**Severity:** Medium

**Operation:** Angular → API → transaction → outbox → consumer → realtime.  
**Current failure behavior:** Initial outbox messages store `Activity.Current.TraceId`; worker logs retain it as `OriginTraceId`. `StartActivity` does not reconstruct the stored parent. Nested outbox messages may receive no correlation or a new trace.  
**Current logging/telemetry:** Good request and outbox log scopes, but no repository-configured `ActivityListener`/OpenTelemetry provider was found.  
**What the caller sees:** A response header or Problem Details request ID that Angular does not retain.  
**What production operators see:** Initial request and first outbox delivery can be joined manually; later asynchronous hops may not share an identifier.

**Failure scenario:** A gameplay consumer enqueues a realtime-delivery event. The second message cannot reliably be searched using the original request trace.  
**Evidence:** [RequestLoggingMiddleware.cs](../LL/src/API/API.LL/Common/RequestLoggingMiddleware.cs), [GameEventOutbox.cs](../LL/src/Infrastructure/Service/Services.LL/Outbox/GameEventOutbox.cs), [GameEventOutboxWorker.cs](../LL/src/API/API.LL/HostedServices/GameEventOutboxWorker.cs), [api.service.ts](../LL/src/Presentation/ll/src/app/core/services/api/api.service.ts).  
**Why this matters:** The asynchronous portion of the architecture is exactly where request-only tracing is insufficient.  
**Recommended direction:** Treat the stored correlation ID as explicit operation metadata across every derived outbox message and realtime log. Preserve request IDs in frontend errors. Distributed tracing can remain optional.

**Answer:** A player action can currently be followed through HTTP, transaction, and the first outbox message. The chain is not reliably continuous through nested outbox/realtime delivery or back to a player's frontend report.

## 12. Production Support Scenarios

### Scenario A — “I completed content but did not receive my reward.”

Evidence available:

- `DungeonCompletionRecord` identifies character, dungeon, first/last completion, and count.
- Claim mutation, reward grant, run deletion, outbox event, loot-history write, and state invalidation are one transaction.
- Item acquisition ledger identifies dungeon-reward item grants.
- Outbox delivery status exposes pending/failed realtime work.

Evidence missing:

- Deleted run's exact reward roll, XP, and currencies.
- A durable compact claim receipt.
- Whether the HTTP response reached the client.
- Reliable long-term loot history; it is limited and player-clearable.
- Strong correlation from each item entry back to the dungeon run ID.

Diagnosis: **Moderate for items; difficult for XP/currency.** A retry cannot duplicate the reward because the claimed run is deleted, but it returns `No completed dungeon run found`, which can confuse the player after a lost response.

### Scenario B — “I spent resources, received an error, and don't know whether it succeeded.”

Evidence available:

- Transactions prevent partial commit for uncaught crafting/essence/purchase failures.
- Champion-market purchases have persistent rows.
- Crafted or purchased item outputs have acquisition evidence.
- PvP persists a detailed match result including ratings/glory and battle identity.

Evidence missing:

- Client operation/idempotency ID.
- Crafting material debit history.
- Essence dust/catalyst operation history.
- Proof that a response was delivered.
- A unified before/after receipt.

Diagnosis: **Difficult for crafting and essence; moderate for market purchases; comparatively good for PvP.**

### Scenario C — “My frontend stopped updating until I refreshed.”

Evidence available:

- Outbox pending/failed counts and oldest age.
- State-sync revisions and LiveOps player synchronization summary.
- SignalR reconnect status and resubscription logic.
- Reconnect bootstrap/reconciliation.
- Realtime message deduplication.

Evidence missing:

- Client acknowledgement that a message was processed.
- Production frontend handler/reconnect telemetry.
- Request ID retained in the UI.
- A durable connection between realtime update ID and browser failure.

Diagnosis: **Good at separating server backlog from likely client failure, but poor at explaining a specific browser session.**

### Scenario D — “A scheduled/background system stopped working.”

Evidence available:

- Persistent Quartz scheduler state.
- `BackgroundJobExecution` rows with business key, attempts, timestamps, status, and error.
- Structured failure logs.
- Outbox LiveOps operational status.

Evidence missing:

- Proactive last-success/age signals for critical scheduled jobs.
- Worker health endpoint/probes.
- Unified consecutive-failure state for raid/tower/API workers.
- Alertable exported metrics in this repository.

Diagnosis: **Forensics are reasonable once investigated; proactive detection is weak.**

## 13. Health / Operational Signals

### Finding 14 — Probe semantics omit critical dependencies and slow startup

**Finding:** Chat readiness is always healthy once the endpoint responds; API readiness covers only account-restriction freshness. API and Chat run migrations before listening but have no startup probe.  
**Location:** Both `Program.cs` files and Helm deployments.  
**Classification:** C — Observability Gap; E — Resilience Issue  
**Severity:** Medium

**Operation:** Startup, database/Redis readiness, and Kubernetes rollout.  
**Current failure behavior:** Slow migration/seed startup can be killed by liveness after the default probe failure window. Chat can report ready while Redis presence/backplane or database operations are unusable.  
**Current logging/telemetry:** Startup failures fail the process and reach container logs; runtime dependency degradation is incompletely represented.  
**What the caller sees:** Restarting pods or a ready Chat pod whose features fail.  
**What production operators see:** Probe failure/CrashLoop without a dedicated startup distinction.

**Failure scenario:** A legitimate migration takes longer than roughly the default liveness tolerance and Kubernetes repeatedly restarts the pod.  
**Evidence:** [API Program.cs](../LL/src/API/API.LL/Program.cs), [Chat Program.cs](../LL-Chat/API/API.Chat/Program.cs), [API deployment.yaml](../LL/deploy/ll-backend/templates/deployment.yaml), [Chat deployment.yaml](../LL-Chat/deploy/ll-chat/templates/deployment.yaml).  
**Why this matters:** Startup work and runtime health require different probe semantics.  
**Recommended direction:** Add startup probes for migration/seed time. Keep liveness dependency-free. Add only cheap readiness checks for dependencies required to serve that process, with careful timeout and degradation behavior.

### Finding 15 — Metrics are defined but not observably collected

**Finding:** Outbox and state-sync instruments exist, but no repository-configured meter provider, scrape endpoint, or exporter was found.  
**Location:** Outbox worker and state-sync services.  
**Classification:** C — Observability Gap  
**Severity:** Medium

**Operation:** Outbox delivery and state invalidation.  
**Current failure behavior:** Counters/histograms may have no listener and therefore provide no operational signal.  
**Current logging/telemetry:** LiveOps derives outbox backlog data directly from PostgreSQL, which partially mitigates this.  
**What the caller sees:** Nothing.  
**What production operators see:** Database-derived snapshots, but not request failure rate, latency distribution, retry rate, or worker last-success metrics.

**Failure scenario:** Outbox retry rate rises substantially while backlog remains temporarily below the five-minute degraded threshold.  
**Evidence:** [GameEventOutboxWorker.cs](../LL/src/API/API.LL/HostedServices/GameEventOutboxWorker.cs), [TransactionBehavior.cs](../LL/src/Core/Application/MediatR/Behaviors/TransactionBehavior.cs), [LiveOpsOperationalStatusService.cs](../LL/src/API/API.LiveOps/Health/LiveOpsOperationalStatusService.cs).  
**Why this matters:** Instrumentation in code can create false confidence if it is not actually consumed.  
**Recommended direction:** First verify deployment-level collection. If absent, expose only a small set: request error/latency, outbox oldest pending/retry/failure, critical job last success, and worker consecutive failures.

## 14. Logging Noise / Security

- No material sensitive-data logging of passwords, JWTs, refresh tokens, authorization headers, cookies, request bodies, or connection strings was found.
- The request logger deliberately avoids bodies and query contents.
- Committed configuration secrets are a configuration exposure risk, not a logging leak; their values are intentionally not reproduced here.
- Production's default `Warning` threshold suppresses ordinary `400` request logs. That is reasonable for expected player errors, but damaging while unexpected exceptions are misclassified as `400`.
- The outbox emits a backlog warning on each claim cycle while lag exceeds 30 seconds; a sustained backlog can create repeated warnings.
- `TournamentGroundsProgressionJob` and `RegionBossProgressionJob` log an exception after `BackgroundJobExecutionService` has already logged and persisted it, then Quartz can log the rethrow. This can produce two or three records for one failure.
- Successful high-frequency gameplay is generally not logged at Information, which is appropriate.
- No evidence of every combat tick or every SignalR message being logged at production levels was found.

## 15. Things Investigated That Are Fine

- `TransactionBehavior` rolls back uncaught exceptions and rethrows with the original stack.
- Transactional outbox rows are committed atomically with gameplay mutations.
- Rollback failures are logged while the original failure is rethrown.
- Cancellation exceptions are intentionally ignored during orderly worker shutdown.
- Liveness is not tied to PostgreSQL or Redis, avoiding dependency-induced restart loops.
- Outbox poison records are isolated, bounded, retained, and recoverable.
- Stale outbox `Processing` claims can be reclaimed.
- SignalR duplicate publication is tolerated through update-ID deduplication and state reconciliation.
- Dungeon claims are naturally idempotent because the run is deleted within the claim transaction.
- PvP match results retain strong persistent evidence of ticket use, outcome, rating, and glory.
- Guild activity, contribution, vault-transfer, and shop-purchase records provide better supportability than most other economy paths.
- Quartz persistence, clustering, `DisallowConcurrentExecution`, recovery requests, and database constraints are appropriate safeguards.
- EF transient retries are not enabled; this avoids automatically replaying non-idempotent gameplay mutations.
- Broad catches in outer worker iterations that log and continue—such as raid resolution and Redis presence renewal—are reasonable boundaries.
- Fail-fast startup for database migration or genuinely invalid required configuration is appropriate; the issue is probe timing and incomplete validation, not fail-fast behavior itself.

## 16. Recommended Maintenance Order

1. Change `ExceptionToResponseBehaviour` so unexpected exceptions reach the global handler as safe, logged `500`s.
2. Make failed transactional responses non-committing when state changed; remove the registration and guest-login broad catches.
3. Standardize one safe error envelope with category/code, message, and request ID while preserving correct 401/403/409/500 statuses.
4. Validate required JWT and connection configuration at startup; remove deployable secret defaults.
5. Add operation IDs and persisted receipts to crafting and selected purchases—only high-value repeatable mutations.
6. Extend the existing economy ledger with targeted debits/credits and operation references; persist a compact dungeon claim receipt.
7. Add bounded outer recovery and progress state to the outbox worker.
8. Expose critical worker/job last-success and outbox-age signals; add worker/startup probe support.
9. Carry correlation explicitly through derived outbox events and retain request IDs in Angular errors.
10. Preserve normalized frontend errors instead of replacing them; add minimal production frontend exception visibility if desired.
11. Stop silently ignoring tournament-lock and persisted JSON deserialization failures.
12. Verify whether the existing metrics are collected; export only the small high-value set if they are not.
13. Rate-limit the outbox backlog warning and remove duplicate Quartz failure logs.

## Verification Notes

The audit was performed through repository-wide static searches and targeted control-flow inspection. Builds and tests were not run because the audit did not change application code. Existing unrelated untracked documentation files were left untouched.
