# Account cheating enforcement design

Date: 2026-08-19  
Status: Implemented (initial scope)  
Scope: Legends Legacy game API, workers, persistence, LiveOps, and player-facing game client

## Implementation status

The initial implementation is complete in this repository. It includes:

- temporal multiplayer restrictions with audited, previewed, idempotent LiveOps apply and revoke operations;
- a startup-loaded, periodically refreshed in-memory restriction index used by ASP.NET authorization, with no restriction database query on ordinary authenticated requests;
- stable `403 account_multiplayer_restricted` responses, bootstrap capabilities, a durable realtime capability-change notification, and a persistent player-facing solo-only notice;
- shared-system policies for marketplace, direct transfers, Colosseum and tournaments, guilds, World Tower rallies, and event quests;
- transaction-boundary checks for every marketplace and direct-transfer participant;
- database-side exclusion from market matching, opponent selection, public leaderboards, tournament standings and rewards, event standings, and World Tower public records;
- background ingestion checks for event-quest and guild-mission contribution;
- idempotent cleanup of marketplace orders, pending tournament and World Tower participation, and multiplayer leadership roles;
- restriction snapshot readiness and staleness health reporting;
- focused domain, LiveOps, marketplace, transfer, authorization, and runtime-index tests.

Operational metrics/alert export, case-specific economy reconciliation, and automated compensation decisions remain follow-up operational work. Those do not weaken the enforcement boundary, but they should be completed before treating multiplayer restriction as the default response to severe economy abuse.

## Decision

Legends Legacy will support two account-level gameplay sanctions:

1. **Account ban** — the account cannot authenticate or play.
2. **Multiplayer restriction** — the account may continue playing isolated solo content but cannot affect other players, shared economies, competitive systems, public standings, guild progression, or server-wide events.

The multiplayer restriction is a scoped online-services ban. It is not a shadow ban: restricted operations return an explicit error, and the player receives a generic explanation, duration when applicable, and appeal route. Detection rules and evidence remain private.

A multiplayer restriction is not the default answer to every cheat case. It serves two purposes:

- temporary containment while a credible case is reviewed;
- a permanent final sanction when continued solo access is safe and operationally desirable.

Confirmed repeated cheating, ban evasion, severe economy damage, continued exploitation, or behavior that cannot be isolated safely results in an account ban.

Automated risk signals never apply a permanent sanction by themselves. They create investigative leads; an authorized operator decides the outcome from preserved evidence.

## Goals

- Stop a restricted account from affecting other players or shared progression.
- Preserve solo play when it can be isolated safely.
- Keep enforcement centralized at system boundaries instead of scattering `IsRestricted` checks through gameplay logic.
- Avoid a database lookup on every ordinary game request.
- Make sanctions temporal, auditable, idempotent, reversible, and explainable to support staff.
- Make passive participation, background processing, and public visibility subject to the same restriction as direct API actions.
- Reuse the existing account-restriction, LiveOps audit, account-risk, and transactional-outbox foundations.

## Non-goals

- Building cheat detection or assigning confidence scores.
- Automatically deciding guilt from the current account-risk score.
- Implementing a fake marketplace, fake opponents, or actions that appear successful but are silently discarded.
- Silently suppressing support or appeal communication.
- Tracking every asset earned during restriction as a separate quarantined asset class.
- Solving device fingerprinting, alternate-account linkage, or platform-wide ban evasion in the first version.
- Applying infrastructure changes from this repository.

## Terminology

| Term | Meaning |
| --- | --- |
| Risk signal | An automated observation that may justify investigation but is not proof. |
| Investigation | Human review of evidence and account history. It is not itself a gameplay sanction. |
| Multiplayer restriction | A temporary or permanent restriction from systems that affect other players or shared state. |
| Account ban | A temporary or permanent restriction from authenticating and playing. |
| Active restriction | A restriction that has not been revoked and whose optional expiry is in the future. |
| Public eligibility | Whether an account may appear in current standings, opponent pools, contributor lists, and similar public competitive surfaces. |
| Reconciliation | Reviewing and, when justified, reversing illicit gains or contaminated shared state before resolving a case. |

## Enforcement policy

### Recommended sanction ladder

| Situation | Default response | Required remediation |
| --- | --- | --- |
| Weak or ambiguous signal | Investigate without restricting | Preserve evidence; no player-visible action. |
| Credible ongoing threat while evidence is reviewed | Short temporary multiplayer restriction | Review promptly; compensate a cleared player when the restriction caused material loss. |
| Confirmed minor or first exploit with bounded impact | Rollback plus temporary multiplayer restriction | Reverse illicit gains and affected transfers where reliable. |
| Confirmed deliberate cheating that can be safely isolated | Permanent multiplayer restriction | Cancel shared participation and reconcile illicit shared gains. |
| Repeated cheating, ban evasion, severe economy damage, destructive automation, or unsafe isolation | Permanent account ban | Reconcile shared damage and retain audit evidence. |
| Compromised account with a cooperative owner | Temporary containment, credential recovery, then case-specific release | Revoke sessions, secure identity, and distinguish attacker activity from owner activity. |

These defaults guide operators; they do not replace judgment. Severity, evidence quality, player impact, recurrence, and the reliability of remediation all matter.

### Temporary investigation restrictions

A temporary multiplayer restriction may be applied before a final determination only when allowing continued participation creates a credible ongoing risk. It must have:

- a bounded expiry;
- a mandatory case reason and evidence reference;
- an assigned or otherwise visible investigation owner;
- a review deadline earlier than or equal to the restriction expiry;
- a documented outcome: cleared, temporary sanction, permanent multiplayer restriction, or account ban.

False-positive containment can cause missed trades, tournaments, and events. If a cleared player suffered material loss, LiveOps uses the existing compensation workflow rather than attempting to replay every missed opportunity automatically.

### Permanent multiplayer restrictions

A permanent multiplayer restriction is appropriate only when:

- all meaningful shared-state pathways are covered;
- the remaining solo workload does not enable continued abuse or excessive server cost;
- post-restriction progression can never re-enter shared systems unless an operator explicitly revokes the restriction after reconciliation;
- allowing continued access does not undermine platform, legal, or safety requirements.

Permanent does not mean technically irreversible. It means there is no automatic expiry and revocation requires an audited operator action.

### Player communication

Restricted actions return `403 Forbidden` with a stable machine-readable code:

```text
account_multiplayer_restricted
```

The player-facing message is generic:

> Access to multiplayer and player-economy services has been restricted for this account.

The response may include an expiry and support reference. It must not reveal detector names, thresholds, other accounts, internal notes, or evidence that would help evade detection.

## Capability model

The persisted restriction type remains coarse. Application code consumes a derived access snapshot so future policy changes do not require callers to understand restriction records.

```csharp
public sealed record AccountAccessSnapshot(
    bool CanAuthenticate,
    bool CanParticipate,
    bool IsPubliclyEligible,
    AccountRestriction? EffectiveRestriction);
```

Initial mapping:

| Active restriction | Authenticate | Solo play | Shared participation | Public eligibility |
| --- | ---: | ---: | ---: | ---: |
| None | Yes | Yes | Yes | Yes |
| Multiplayer restriction | Yes | Yes | No | No |
| Account ban | No | No | No | No |

Do not persist independent booleans for every feature. The single multiplayer restriction deliberately denies the whole shared surface. Feature-specific sanctions can be introduced later only when a concrete operational need justifies their additional complexity.

Chat moderation remains owned by the independently deployed Chat service. A multiplayer restriction does not silently shadow chat. Whether it also triggers a normal chat mute is an explicit, separately audited moderation decision.

## Persistence model

Extend the existing temporal restriction model:

```csharp
public enum AccountRestrictionType
{
    Ban,
    MultiplayerRestriction
}
```

The existing `AccountRestriction` fields remain authoritative:

- restriction ID and account ID;
- type;
- public reason and internal notes;
- creating staff subject and creation time;
- optional expiry;
- revoking staff subject, time, and reason.

The existing composite index on account, type, revocation, and expiry supports active-restriction lookup. An implementation should measure the active-set refresh query before adding another index.

Add audited LiveOps operations for applying and revoking a multiplayer restriction. Operation IDs remain idempotency keys. Applying the restriction and adding its `AdminAction` must commit atomically.

Restriction history is never deleted. Expiry changes effective access but does not erase the original action.

## Runtime restriction index

### Decision

Normal request authorization must not query the database for account restrictions. Each enforcing process maintains a compact in-memory snapshot of active account restrictions.

```text
AccountRestrictions table
        |
        | active rows, periodic indexed query
        v
immutable in-memory map: AccountId -> effective restriction and expiry
        |
        +--> default account-ban authorization
        +--> multiplayer authorization policy
        +--> outbox/event eligibility
        +--> realtime connection admission
```

The index is a singleton service exposed through a synchronous lookup:

```csharp
public interface IAccountRestrictionIndex
{
    AccountAccessSnapshot Get(Guid accountId);
}
```

### Refresh behavior

- Load the active restriction snapshot before the process becomes ready.
- Query only non-revoked restrictions whose expiry is null or in the future.
- Refresh every 30 seconds initially.
- Build a new immutable dictionary and atomically replace the previous snapshot.
- Evaluate stored expiry during lookup so an expired restriction stops applying even before the next refresh.
- Retain the last known good snapshot when a later refresh fails and emit an operational alert.
- Fail process readiness if no initial snapshot can be loaded. Starting with an empty unknown snapshot would fail open.

The 30-second interval is an operational starting point, not a game-balance constant. It bounds cross-process propagation without requiring Redis or one query per player request.

The LiveOps process may update its own local snapshot immediately after commit, but the Game API and workers must not depend on that optimization because `API.LiveOps` is a separate deployment boundary. Their periodic refresh provides eventual propagation.

If sub-30-second cross-instance enforcement later becomes necessary, publish an `AccountRestrictionChanged` integration event or use shared cache invalidation. Do not introduce that infrastructure until measured operations require it.

### JWT behavior

Restriction state is not embedded as the sole authority in access-token claims because it can change after token issuance. Cryptographic token validation establishes identity; the in-memory restriction index establishes current account access.

Applying a full account ban still revokes active refresh tokens. Existing access tokens are rejected by the active-account authorization check after the restriction snapshot refreshes.

Applying a multiplayer restriction does not revoke refresh tokens because the player remains allowed to authenticate and play solo content.

## Enforcement architecture

Enforcement occurs at four boundaries. No single mechanism covers all of them.

### 1. Authenticated account admission

The default authenticated-user authorization policy checks `CanAuthenticate`. Because all game controllers derive from the authorized base controller and the game hub is authorized, a full account ban is enforced centrally for API requests and new realtime connections.

The check is an in-process dictionary lookup, not a database call.

Password login, external-provider login, and refresh-token rotation continue to perform authoritative restriction checks during token issuance. This prevents banned accounts from receiving new sessions even if a runtime snapshot is briefly stale.

### 2. Player-initiated shared mutations

Add an authorization policy such as `MultiplayerAllowed`. Its handler reads the account ID claim, obtains the access snapshot from the in-memory index, and succeeds only when `CanParticipate` is true.

Apply the policy declaratively to shared mutations, including:

- creating, buying, selling, fulfilling, or cancelling marketplace orders;
- direct item and currency transfers;
- starting Colosseum battles and updating competitive defense state;
- tournament registration, team operations, withdrawal, and reward claims;
- guild invitations, applications, donations, vault operations, mission contribution, role changes, and shared construction;
- server-event reward and milestone claims;
- future raids, rallies, cooperative combat, gifting, or trading.

Read-only public data could remain accessible when it creates no shared effect. The initial implementation deliberately applies the policy to whole shared-system controllers, including their reads, because the product requirement is that these systems are unavailable while restricted and the coarser boundary is easier to audit. This can be relaxed per read endpoint later without changing the mutation or persistence safeguards.

Use controller-level policy metadata only when every action on that controller is shared. Otherwise annotate mutation endpoints, leaving harmless reads available.

### 3. Multi-party and passive economy operations

Authorizing the requesting account is not enough. Economy operations must validate every participant at the transaction boundary:

- A restricted sender cannot transfer.
- A restricted recipient cannot receive a direct transfer.
- A guest account cannot send or receive direct item or currency transfers.
- A restricted buyer cannot buy.
- A restricted seller's active listing cannot be purchased.
- A restricted account's buy order cannot be fulfilled.

This validation belongs once in each authoritative marketplace or transfer operation, not in controllers and not in frontend code. It prevents internal callers, retries, and passive pre-existing orders from bypassing the restriction.

Market reads and matching queries must exclude active orders owned by ineligible accounts until cleanup completes. Eligibility must be expressed as a database predicate or anti-join so matching remains atomic and race-safe.

### 4. Background participation and public projections

Background systems do not pass through ASP.NET authorization. They enforce eligibility at ingestion or projection boundaries.

#### Server-wide events

The event-quest outbox consumer checks `message.AccountId` before applying contribution. Restricted accounts produce no global objective progress, personal contribution, contributor standing, or event reward eligibility while restricted.

New relevant outbox producers must populate `AccountId`. During migration, a message missing account identity resolves it from `CharacterId`; if neither identity is available, the delivery is rejected and alerted rather than counted without an eligibility decision.

Applying a restriction does not automatically subtract historical event contribution. Retroactive subtraction can invalidate completed objectives and already-claimed rewards. Confirmed fraudulent contribution is handled through a separate, audited incident-remediation operation when the impact justifies it.

#### Rankings and public visibility

Restricted accounts are excluded before ranking, pagination, searching, rank-number assignment, and participant counts. Filtering after DTO construction would leave gaps and allow hidden accounts to shift other players' ranks.

Persistence exposes a reusable eligibility expression or query helper used by:

- general character leaderboards;
- Colosseum rankings and opponent selection;
- tournament season standings and hall of fame;
- server-event contributor standings;
- individual guild-contribution standings;
- future public competitive projections.

Guild-wide aggregate rankings are not hidden merely because a guild contains a restricted member. Fraudulent member contributions are excluded at ingestion; historical guild effects require case-specific remediation.

#### Matchmaking and scheduled jobs

Opponent selection, tournament seeding, reward distribution, and other batch operations filter candidates in the database or as a batch. They must not perform one database restriction query per candidate.

## Applying a multiplayer restriction

Applying a restriction is an idempotent orchestration, not only an inserted row.

1. Validate actor permission, target account, reason, expiry, and operation ID.
2. Reject an overlapping active restriction of the same type unless policy explicitly supports extension.
3. Persist the restriction and append-only administration action atomically.
4. Enqueue an `AccountMultiplayerRestricted` outbox message in the same transaction.
5. Notify the player client that account capabilities changed when a realtime connection exists.
6. Run idempotent subsystem cleanup from the outbox message.

Cleanup includes:

- cancel active sell listings and return escrowed items;
- cancel active buy orders and return escrowed currency;
- remove pending matchmaking entries;
- withdraw pending tournament registrations and team applications where tournament rules permit;
- prevent future selection as an opponent immediately through eligibility filtering;
- end or transfer multiplayer leadership roles that cannot remain inactive safely;
- preserve historical records and audit evidence.

The policy check and query filtering take effect independently of cleanup, so an outbox delay cannot permit new interaction with stale orders or registrations.

Cleanup consumers use the restriction ID or administration operation ID as their idempotency key. Retrying a delivery must not duplicate refunds or state transitions.

## Revoking or expiring a restriction

Revocation records actor, time, and reason; it never deletes the restriction. Natural expiry needs no mutation to the original record.

Before manually releasing a temporary investigation restriction, the operator must record one of these outcomes:

- **Cleared:** evidence did not justify a sanction. Restore access and compensate material restriction-caused loss when appropriate.
- **Remediated:** illicit gains or shared effects were reconciled, and normal access may resume.
- **Temporary sanction completed:** the defined punishment ended and no unresolved contamination remains.
- **Escalated:** replace the temporary restriction with a permanent multiplayer restriction or account ban.

Access restoration does not automatically recreate cancelled listings, buy orders, matchmaking entries, tournament registrations, or missed event participation.

A permanent multiplayer restriction must not be revoked casually. If it is revoked, the operator reviews progression and economy changes during the restricted interval. This avoids allowing a known cheater to stockpile solo-generated or illicit value and later re-enter the shared economy unchecked.

## Race conditions and consistency

### Propagation window

With a 30-second snapshot refresh, a newly applied restriction may take up to approximately 30 seconds to reach another process. Cleanup and database eligibility filters reduce exposure during that window. If that delay proves unacceptable, add cross-process invalidation rather than database reads on every request.

### Transactions already in progress

A transaction that passed eligibility immediately before restriction creation may commit afterward. Economy and tournament records retain timestamps and correlation IDs so an operator can identify and remediate this narrow race. Strong global serialization between LiveOps and every gameplay transaction is not justified initially.

### Expiry during a request

Authorization uses the access state at request admission. An operation admitted immediately before expiry remains valid. Long-running competitive operations should re-check eligibility at their own commit or seeding boundary.

### Multiple restrictions

An account may have both an active ban and multiplayer restriction. The most restrictive result wins:

- any active ban denies authentication;
- otherwise any active multiplayer restriction denies participation and public eligibility.

Revoking one restriction does not affect another.

## Failure behavior

| Failure | Required behavior |
| --- | --- |
| Initial restriction snapshot cannot load | Process is not ready; do not start with an empty allow-all snapshot. |
| Periodic refresh fails | Keep last known good snapshot and alert. |
| Snapshot is stale beyond an operational threshold | Mark health degraded; alert operators. Multiplayer may be configured to fail closed if the stale interval exceeds the agreed limit. |
| Cleanup delivery fails | Retry idempotently; eligibility filters continue blocking interaction. |
| Player capability notification fails | Server enforcement remains authoritative; the next API response/bootstrap refresh corrects the UI. |
| Ranking filter cannot determine ownership | Exclude the row and alert rather than publish an unverified participant. |
| Event message lacks both account and resolvable character identity | Reject and retry/alert; do not count it. |

## Client behavior

The game bootstrap includes derived account capabilities and optional public restriction information:

```json
{
  "accountAccess": {
    "canParticipate": false,
    "isPubliclyEligible": false,
    "restrictionCode": "multiplayer_restricted",
    "expiresAt": "2026-08-22T12:00:00Z"
  }
}
```

The client uses this state to:

- hide or disable shared mutation controls;
- show one consistent restriction explanation;
- avoid repeatedly submitting operations that will return `403`;
- refresh bootstrap state after a capability-changed realtime message;
- preserve access to solo gameplay and account/support screens.

The API never trusts these client capabilities when authorizing an operation.

## LiveOps experience

The player detail view shows active and historical restrictions alongside investigation state, evidence, notes, and audit history. Applying a restriction requires:

- restriction type;
- temporary expiry or explicit permanent selection;
- player-facing reason category;
- internal reason/evidence reference;
- client-generated operation ID;
- confirmation for permanent sanctions.

The UI clearly separates:

- automated risk severity;
- human investigation status;
- active gameplay sanctions.

A high risk score must never appear as though it is already a conviction. Permanent restriction actions require the account-moderation permission and should support step-up authentication when the LiveOps authentication design adds it.

## Audit and evidence requirements

Every sanction decision records:

- stable operator subject;
- target account and character;
- action and restriction IDs;
- occurrence time and optional expiry;
- mandatory reason and internal evidence/case reference;
- prior and resulting effective access state;
- links to reversal, escalation, compensation, or remediation actions;
- cleanup outcome and any failures.

Do not store raw secrets, access tokens, unnecessary personal data, or cheat signatures in player-facing fields. Evidence retention and staff visibility follow the LiveOps audit policy.

## Observability

Initial metrics:

- active restrictions by type and temporary/permanent status;
- snapshot row count, age, refresh duration, and refresh failures;
- authorization denials by feature and restriction type;
- restricted marketplace orders filtered before cleanup;
- restricted outbox contributions ignored by event type;
- restricted accounts excluded from rankings and matchmaking;
- cleanup delivery attempts, failures, refunds, and completion latency;
- restrictions applied, revoked, expired, escalated, and cleared;
- compensation issued after cleared investigations.

Alerts:

- initial snapshot load failure;
- restriction snapshot older than the agreed threshold;
- repeated cleanup delivery failure;
- a restricted account successfully entering a protected mutation or public candidate set;
- unusual bursts of permanent sanctions or operator reversals.

Logs use account and restriction IDs, not email addresses or unrestricted internal evidence text.

## Verification strategy

### Domain and policy tests

- Active, expired, and revoked restriction semantics.
- Most-restrictive-wins behavior with overlapping restriction types.
- Permanent and temporary validation rules.
- Idempotent apply and revoke operations.
- Access snapshot mapping for normal, restricted, and banned accounts.

### Runtime index tests

- Initial load and immutable atomic replacement.
- Expiry evaluated between refreshes.
- Failed refresh retains the last known good snapshot.
- Initial failure prevents readiness.
- Concurrent reads during replacement remain consistent.

### API authorization tests

- Restricted account can call representative solo endpoints.
- Restricted account receives `403 account_multiplayer_restricted` for every protected mutation.
- Banned account cannot use an already-issued access token after snapshot propagation.
- Harmless read endpoints remain available according to policy.
- Game hub connection admission enforces account bans.

### Economy tests

- Restricted sender and restricted recipient transfers both fail atomically.
- Restricted buyers, sellers, listing owners, and buy-order owners cannot trade.
- Active orders are absent from matching before asynchronous cleanup completes.
- Cleanup returns each escrowed item or currency amount exactly once under retries.

### Competitive and visibility tests

- Restricted accounts cannot battle, register, join teams, seed, or receive competitive rewards.
- Restricted accounts cannot be selected as opponents.
- They are excluded before rank calculation, search, pagination, and participant counts.
- Their removal does not leave rank gaps.
- Guild-wide rankings remain present while ineligible individual contribution is excluded.

### Event and background tests

- Restricted outbox messages add no global or personal contribution.
- Missing identity follows the defined resolution and failure behavior.
- Event reward claims are denied while restricted.
- Historical contribution is not silently subtracted.
- Scheduled jobs filter restricted candidates in a batch.

### Regression test

Maintain an explicit inventory of shared-system mutation endpoints and background consumers. A test or architecture check should require every inventoried boundary to declare its participation policy. This turns future omissions into review or test failures instead of relying on developer memory.

Backend verification runs through `build/run-tests.ps1` as required by repository policy.

## Delivery plan

### Phase 1: policy and persistence

- Add `MultiplayerRestriction` to the temporal restriction model.
- Add access snapshot derivation and LiveOps apply/revoke operations.
- Add migration and audit action types.
- Add the runtime restriction index and health reporting.
- Harden the default authenticated policy for existing full bans.

### Phase 2: direct participation enforcement

- Add `MultiplayerAllowed` authorization policy.
- Annotate marketplace, transfer, Colosseum, tournament, guild, and event-claim mutations.
- Add authoritative multi-party checks to marketplace and transfers.
- Expose capabilities through game bootstrap and handle them in Angular.

### Phase 3: passive and background enforcement

- Filter marketplace orders, rankings, opponent pools, tournament jobs, and contributor standings.
- Gate server-event contribution ingestion.
- Add restriction-change outbox message and idempotent cleanup consumers.

### Phase 4: LiveOps and operational hardening

- Add investigation-to-sanction workflow and confirmation UX.
- Add metrics, alerts, reconciliation links, and cleared-player compensation workflow.
- Exercise propagation, retry, expiry, and stale-snapshot failure scenarios in staging.
- Decide from measured operations whether periodic refresh is sufficient or cross-process invalidation is warranted.

## Migration, configuration, and deployment implications

No EF Core migration is required for this implementation. Restriction and administration-action enum values are stored as strings, and the existing restriction, audit, preview, and outbox tables already contain every required field. No schema or database-configuration change was generated or applied.

Initial configuration:

```text
AccountRestrictions:SnapshotRefreshSeconds = 30
AccountRestrictions:MaximumStaleSeconds = 120
```

Each process that enforces restrictions needs read access to active restriction rows and must report snapshot health. No new external cache or message broker is required initially.

Production rollout order:

1. Deploy persistence and backward-compatible readers.
2. Deploy runtime index and observability with enforcement disabled or audit-only if desired.
3. Deploy server-side enforcement and cleanup consumers.
4. Deploy client capability UX.
5. Enable the LiveOps operation only after every shared pathway is verified.

Do not allow operators to apply a multiplayer restriction before passive orders, transfers, competitive entry, rankings, guild contribution, and server-event paths are covered. A partially enforced restriction gives a false sense of containment.

Infrastructure-as-code changes, if later required for alerts, cache invalidation, or deployment health checks, belong in the separate infrastructure repository.

## Alternatives considered

### Full ban for every confirmed cheat

This is operationally simplest and remains the correct fallback when isolation is incomplete. It unnecessarily removes solo access when the shared ecosystem can be protected reliably, and it may encourage immediate alternate-account creation. The design retains full bans for severe, repeated, evasive, or unsafe cases.

### Restriction state in JWT claims only

This removes runtime state lookup but cannot react until the token expires. With the current 30-minute access-token lifetime, that delay is too large for active economy abuse. Shortening tokens increases refresh traffic and still leaves a non-zero window.

### Database lookup on every request

This provides current state but adds repetitive reads to every authenticated request, including solo gameplay. It is unnecessary because the active restriction set is small and changes infrequently.

### Cache-aside lookup per account

Negative caching reduces database traffic but still creates per-account refresh queries and awkward cross-process invalidation. Periodically replacing the compact active set is simpler and gives a clear maximum staleness bound.

### Capability bitmask stored per account

This supports granular sanctions but increases policy combinations, testing burden, and operator ambiguity before such granularity is needed. A coarse restriction plus derived access snapshot keeps the first version understandable.

### True shadow environment

A fake economy, fake opponents, or silently discarded operations would require a parallel simulation with convincing consistency. It complicates support, auditing, rewards, state repair, and player communication. It is not justified for a solo-developed game.

## References and related documents

- `docs/liveops-administration-plan.md`
- `docs/liveops-account-risk-audit.md`
- `docs/auth-authorization-review.md`
- `LL/docs/marketplace-and-economy-design.md`
- `LL/docs/server-event-quests.md`
- [Steamworks anti-cheat and game-ban guidance](https://partner.steamgames.com/doc/features/anticheat/vac_integration?language=english)
