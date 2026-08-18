# LiveOps dashboard additions roadmap

## Purpose

This document describes the recommended additions to the Legends Legacy LiveOps
dashboard after its initial production release. The existing dashboard is a safe
player-action console; the next stage should turn it into an operational workspace
without turning it into an unrestricted database editor.

The roadmap is optimized for a solo operator today while preserving a clear path
to multiple staff members later.

## Current baseline

The dashboard currently provides:

- exact player lookup by name, account, email, or ID;
- account ban and ban revocation;
- Chat mute and mute revocation;
- catalog item compensation;
- permission checks on every endpoint;
- required reasons and optional internal notes;
- idempotent mutation operation IDs;
- production and local-development environment warnings;
- combined Game and Chat history for a selected player;
- Game database and Chat readiness checks;
- Google OIDC authentication with an immutable owner-subject allowlist.

This is a strong foundation. Global audit and operational status visibility are now
available without selecting a player, privileged mutations are protected by
server-backed previews tied to the exact target state, and the selected-player
workspace now provides a failure-isolated support snapshot. The frontend is also
separated into routed features, completing the planned Milestone 1 operator workspace.

## Implementation status

The global-audit explorer is now implemented:

- a read-authorized `GET /api/liveops/audit` endpoint;
- deterministic cursor pagination across Game and Chat audit records;
- filters for date range, source, action, actor, permission, case/reference text,
  risk level, target, and operation ID;
- partial results when Chat is unavailable, while a Chat-only request fails closed;
- superadmin-only internal notes in global results;
- a same-origin `/audit` dashboard page with filters, paging, expandable details,
  and links back to affected players;
- permanent-action, high-value-grant, and export quick views;
- explicit completed outcomes and persisted risk classifications;
- superadmin-only CSV export with antiforgery protection, a 31-day range limit,
  a 5,000-row cap, rate limiting, spreadsheet-injection protection, and a content
  digest;
- append-only, idempotent audit records for every successful export;
- repository, API-client, route, and frontend coverage.

Environment filtering remains future work because environment is not currently
stored on historical actions. Rejected and replayed attempts are not yet persisted;
the current append-only records represent completed operations only. Case/reference
lookup is substring-based until structured support cases exist. Break-glass activity
cannot be classified historically until the actor's role or authentication context
is recorded with each action.

The operational home/status slice is also implemented:

- `/dashboard` is the default authenticated destination;
- Game database and Chat moderation readiness with explicit affected tools;
- a combined healthy, degraded, or unhealthy readiness result;
- server UTC time, browser clock-skew detection, environment, process start time,
  and optional release/commit/deployment metadata;
- realtime outbox pending, failed, and oldest-pending summaries without exposing
  event payloads or error text;
- Game account restrictions expiring within seven days;
- 24-hour permanent-action and high-value-grant counts linked to filtered audit;
- recent privileged Game and Chat activity;
- manual refresh and automatic refresh every 30 seconds.

Game and Chat version values require the deployment pipeline to populate the
non-secret `LiveOps__Build__*` settings. Generic background-job health is not yet
shown because the application does not currently expose a bounded job-status
registry.

The server-backed action-preview slice is now implemented:

- dedicated preview endpoints for account bans and revocations, Chat mutes and
  removals, and compensation grants;
- five-minute persisted preview tokens bound to the actor subject, action kind,
  target, operation ID, normalized request hash, and current-state hash;
- mutation endpoints that require a matching preview token and return a conflict
  when the preview expired or the target changed;
- retry-safe submission using the original idempotent operation ID;
- a shared same-origin review dialog showing operator, environment, exact target,
  operation reference, requested fields, consequences, risk, and expiry;
- exact typed confirmation for permanent restrictions and large, rare, unique, or
  non-stackable grants;
- hashes instead of reasons or internal notes in persisted preview records, plus
  opportunistic cleanup of expired records;
- backend coverage for payload binding, expiry, changed state, retry, redaction,
  and high-value classification.

The preview does not yet estimate inventory-capacity impact because the existing
inventory model has no bounded capacity read model. That belongs with the read-only
player support snapshot and compensation-package work.

The read-only player support snapshot is now implemented:

- a composed `GET /api/liveops/players/{characterId}/support-snapshot` endpoint;
- separately timed account, activity, economy, guild, marketplace, and realtime
  synchronization reads;
- partial results when an individual section times out or fails;
- explicit source and freshness metadata for every section;
- complete account-restriction history without internal notes;
- current background activity and next-resolution timing;
- balances, inventory totals, recent retained acquisitions, and compensation grants;
- guild membership, marketplace exposure and trades, state revisions, and per-player
  outbox delivery health;
- an initial 25 account-level Cinder wires and direct inventory transfers with a
  stable cursor-based "Load 25 more" action, including alternate-character activity,
  direction, exact participants, asset identity, and item-instance lineage;
- full account and character identifiers with copy controls;
- API, resilience, missing-player, client, and frontend partial-state coverage.

The current data model does not expose a dedicated login-event history, bounded
inventory capacity, character region/location, or pending-reward registry. The
snapshot states those limitations instead of inferring misleading values. It also
omits refresh-token values, event payloads, delivery errors, and restriction internal
notes. Section timeouts default to three seconds and can be changed with the
non-secret `LiveOps__SupportSnapshotSectionTimeoutSeconds` setting.

Transfer history is read from the existing append-only `PlayerTransferHistory`
records rather than inferred from current balances. Consequently, it is authoritative
for retained transfers but does not reconstruct activity from before transfer-history
persistence was introduced.

Frontend route and feature separation is now implemented:

- real Angular routes for `/dashboard`, `/audit`, `/players`, and
  `/players/{characterId}`;
- lazy-loaded status, audit, and player-workspace route components;
- a shared authenticated operator shell and permission context;
- extracted support-snapshot and server-verified action-preview components;
- route-owned refresh timers, audit filters, player loading, and mutation state;
- URL query parameters for dashboard-to-audit risk views;
- a shared global visual stylesheet without feature-state coupling;
- a 12/16 KB component-style budget, down from the temporary 28/32 KB allowance;
- route-component coverage for authentication, status, audit, partial player support,
  and typed action confirmation.

## Product principles

All additions should follow these rules:

1. Prefer read-only visibility before adding new mutation powers.
2. Use application services and domain operations; never expose arbitrary database
   editing.
3. Every mutation must be authorized, idempotent, attributable, and append-only in
   the audit trail.
4. High-risk actions require a server-generated preview and stronger confirmation.
5. Game-wide controls must remain separate from player-specific support tools.
6. Dependency failures must disable only the actions that depend on them.
7. Avoid bulk operations until there is a demonstrated operational need and an
   approval model.
8. Minimize player data exposure and define retention rules for evidence and
   exports.

## Priority summary

| Priority | Addition | Operator value | Effort | Operational risk |
|---|---|---:|---:|---:|
| P0 | Global audit explorer | Very high | Medium | Low |
| P0 | Operational home and status | Very high | Small to medium | Low |
| P0 | Server-backed action previews | Very high | Medium | Low |
| P0 | Read-only player support snapshot | High | Medium | Low |
| P1 | Structured support cases | High | Medium | Low |
| P1 | Compensation packages | High | Medium | Medium |
| P1 | Chat reports and evidence | High | Large | Medium |
| P1 | Corrective compensation | High | Large | High |
| P2 | Announcements and maintenance controls | Medium | Medium | High |
| P2 | Feature flags and event controls | Medium | Large | Very high |
| Later | Multiple operators and approvals | Low today | Large | Medium |

## P0: safe operator workspace

### 1. Global audit explorer

Add a top-level `/audit` page that does not require selecting a player first.

Required capabilities:

- cursor-paginated history;
- date-range filtering;
- filters for action type, operator, permission, and environment;
- lookup by player, account, character, operation ID, or support case ID;
- quick filters for permanent bans, large grants, failed attempts, and break-glass
  activity;
- expandable details for reason, internal notes, target resources, and structured
  operation data;
- links from an entry to the relevant player or case;
- clear indication of completed, replayed, rejected, and corrective operations;
- permission-gated CSV export for incident reviews.

The audit API should use stable cursor ordering based on occurrence time and
operation ID. Exports should be rate-limited, time-bounded, and recorded as audited
operator activity.

#### Acceptance criteria

- An operator can find an action using its operation ID.
- Results remain stable while paging through concurrently added actions.
- Internal notes are not returned to operators without the required permission.
- Exported data contains only the fields shown in the authorized result set.

### 2. Operational home and status page

Add a default `/dashboard` page that answers: "Is LiveOps safe to use right now?"

Recommended status cards:

- Game database readiness;
- Chat moderation readiness;
- API, Chat, Game, and frontend versions;
- deployment timestamp and commit identifier;
- server UTC time and detected browser clock skew;
- realtime/outbox backlog and oldest pending event;
- failed or delayed background jobs;
- current environment;
- recent privileged actions;
- restrictions expiring soon;
- alerts for permanent bans and unusually large grants.

Chat failure should disable Chat operations without blocking account or economy
operations. A database failure should put player operations into a visible
read-only/unavailable state rather than allowing forms to fail after confirmation.

#### Acceptance criteria

- Status refreshes without reloading the application.
- Each degraded dependency identifies the affected operations.
- Version information makes it possible to confirm which release is deployed.
- Health responses do not expose secrets, connection strings, or internal network
  addresses.

### 3. Server-backed action previews and confirmations

Replace native browser confirmation prompts with a shared review dialog.

The review should display:

- environment;
- operator identity;
- exact player, account, and character target;
- current restriction state;
- requested action and precise expiry time;
- reason, case reference, and internal notes;
- item name, server ID, rarity, binding behavior, and quantity;
- warnings and whether the action can be corrected;
- operation reference.

Add preview endpoints for privileged mutations. A preview should validate current
server state and return a short-lived, single-purpose preview token or an expected
state version. Submission must fail with a conflict if the target changed after
preview.

Typed confirmation should be required for:

- permanent bans;
- large compensation grants;
- rare, unique, or non-stackable grants;
- currency compensation;
- corrective item removal;
- future game-wide operations.

#### Acceptance criteria

- The final mutation cannot silently target a different current state than the
  preview.
- Retrying an uncertain request reuses the original operation ID.
- Closing a dialog does not submit or discard a recoverable form unexpectedly.
- Permanent actions require the configured target text to match exactly.

### 4. Read-only player support snapshot

Expand the selected-player view with enough context to make a support decision
without opening database tools.

Suggested fields:

- full IDs with copy controls;
- account creation and last activity;
- active and previous restrictions with expiry and revocation details;
- character level, region, and current action;
- inventory capacity and selected currency balances;
- recent item acquisitions and compensation grants;
- recent marketplace activity;
- guild membership;
- pending or failed rewards;
- current background action state;
- recent login/session security events;
- realtime synchronization status.

This page should remain read-only. Additional mutation capabilities must be
purpose-built operations with their own permission, validation, and audit type.

#### Acceptance criteria

- Every displayed datum identifies its owning service and freshness.
- A slow optional data source does not block the complete player page.
- Sensitive fields are omitted rather than merely hidden with CSS.
- Operators can copy IDs without manually selecting truncated text.

## P1: support workflow and efficiency

### 5. Structured support cases

Introduce a lightweight append-only case model:

- external support/ticket reference;
- category such as harassment, spam, cheating, appeal, bug compensation, outage,
  or missing purchase;
- case status: open, waiting, resolved, or closed;
- evidence links;
- append-only operator notes;
- related account and character IDs;
- related LiveOps operation IDs;
- creation, update, and resolution timestamps.

Mutation forms should accept a case reference and offer reason templates, but the
operator must still be able to add meaningful context. Closing a case must not
alter its operation history.

### 6. Compensation packages and preview

Add version-controlled compensation packages for common incidents, for example:

- minor outage;
- lost dungeon reward;
- missing event reward;
- failed purchase recovery;
- rollback compensation;
- custom package.

The server should expand and validate the package at preview time. Display every
resulting item, quantity, binding rule, inventory impact, package version, and
triggered economy limit before confirmation.

Currency compensation, if introduced, must use an append-only economy transaction
with stricter quantity limits and a separate permission. Do not add a generic
"set balance" operation.

### 7. Chat reports and evidence

Add an investigation workflow around Chat moderation:

- player report queue;
- reported message and limited surrounding context;
- channel and timestamp;
- reporter information subject to privacy rules;
- report category and state;
- evidence linked to the resulting mute or case;
- optional message deletion or redaction where supported;
- report disposition and operator notes.

Define a limited evidence retention period. The dashboard must not become an
unbounded searchable archive of private conversations.

### 8. Corrective compensation

Provide correction by original operation ID, not generic item deletion.

A correction must:

1. Resolve the original compensation action.
2. Identify exact generated instances or stack changes.
3. Check whether affected items were consumed, equipped, traded, transformed, or
   moved.
4. Remove only the portion that is safely attributable and reversible.
5. Refuse ambiguous corrections.
6. Write a new compensating audit operation rather than changing the original
   record.
7. Explain any portion that could not be reversed.

Large corrections should require typed confirmation and, after multi-operator
support exists, optional second-person approval.

## Moderation improvements

Useful restriction additions include:

- custom expiry date and time;
- reason categories with recommended durations;
- repeat-offense warnings;
- a list of restrictions expiring soon;
- verification that expired restrictions are no longer enforced;
- appeal outcome tracking;
- append-only staff notes with retention controls.

### Shadow bans

A conventional shadow ban is not recommended. Hidden behavioral differences are
difficult to support, test, and explain, and can create inconsistent state between
Game and Chat.

If spam control requires something beyond a mute, prefer an explicitly modelled,
audited restriction such as:

- message rate limiting;
- message quarantine for moderator review;
- new-account posting limits;
- restricted access to selected public channels.

Such restrictions still need a reason, expiry, visible operator state, and normal
revocation history.

## P2: game operations controls

Create a separate `/operations` area for game-wide controls. Do not place these
inside the selected-player workspace.

Potential additions:

- player-facing announcements;
- maintenance banners;
- server access mode;
- scheduled events;
- reward multiplier schedules;
- feature flags;
- background-job inspection and retry;
- event or content activation status.

Every game-wide operation should support:

- server-side validation and preview;
- explicit environment and blast-radius display;
- scheduled activation and automatic expiry where applicable;
- versioned configuration;
- rollback or compensating action;
- full audit history;
- stronger confirmation;
- optional second-person approval.

Direct editing of arbitrary configuration keys should not be exposed.

## Multi-operator evolution

The initial one-owner model is appropriate for a solo developer. If more staff are
added later, introduce identity-provider-backed roles rather than managing
passwords in LiveOps.

Suggested roles:

| Role | Capabilities |
|---|---|
| Support viewer | Player lookup, cases, and audit read |
| Chat moderator | Viewer plus Chat moderation |
| Account moderator | Viewer plus account and Chat moderation |
| Compensation support | Viewer plus bounded compensation |
| Operations administrator | Game-wide operational controls |
| Break glass | All permissions; strongly monitored |

Add second-person approval only for actions whose blast radius justifies the
friction, such as very large grants, mass compensation, production feature flags,
or maintenance mode.

## Frontend architecture

Before adding several features, split the current single-page component into
routed feature areas:

```text
/dashboard
/players/:characterId
/audit
/cases
/reports
/operations
```

Suggested Angular structure:

```text
src/app/
  core/
    auth/
    http/
    environment/
    health/
  layout/
    liveops-shell/
  features/
    dashboard/
    players/
    audit/
    cases/
    chat-reports/
    compensation/
    operations/
  shared/
    action-preview-dialog/
    copy-identifier/
    dependency-status/
    duration-input/
    operation-result/
    paged-table/
```

Keep API requests same-origin. Browser permission checks control presentation only;
the API remains authoritative.

## Backend architecture

Recommended API and service work:

- cursor-paginated global audit query;
- structured problem responses with stable codes and correlation IDs;
- server-side preview endpoints and expected-state validation;
- separate read models for the player support snapshot;
- partial dependency results so optional services can degrade independently;
- explicit permissions per new operation;
- rate limits for privileged mutations and exports;
- request-body and field-length limits;
- optimistic concurrency for state-dependent actions;
- structured security logs and metrics;
- alerts for permanent bans, large grants, repeated failures, and break-glass use.

New service integrations should remain behind `API.LiveOps`. The browser must not
receive database, Chat, or internal service credentials.

## Data and audit rules

- Administration history is append-only.
- Corrections create new linked operations.
- Every mutation records actor subject, display name, permission, target, reason,
  case reference, details, operation ID, and timestamp.
- Sensitive evidence has an explicit retention period.
- Internal notes must not appear in ordinary application logs.
- Export access and generated exports are audited.
- Audit pagination uses deterministic ordering.
- Player data is returned only when required by the selected workflow.

## Testing strategy

### Backend

- Permission allow/deny coverage for every endpoint.
- Antiforgery coverage for browser mutations.
- Cursor pagination stability.
- Preview expiry and expected-state conflict tests.
- Idempotent replay and idempotency-conflict tests.
- Partial dependency failure tests.
- Compensation package expansion and limit tests.
- Corrective compensation tests for consumed, traded, transformed, and untouched
  items.
- Audit redaction and retention tests.
- Rate-limit tests for risky endpoints.

### Frontend

- Dashboard health and degraded dependency states.
- Audit filters, paging, empty results, and errors.
- Preview dialogs for every mutation.
- Typed confirmation rules.
- Operation ID preservation across retries.
- Permission-aware navigation and controls.
- Player snapshot partial-loading behavior.
- Session expiry during a prepared mutation.

### End-to-end

- Find an operation globally and navigate to its player.
- Preview and apply a temporary restriction.
- Reject a mutation after target state changes between preview and submit.
- Use a compensation package and verify inventory, ledger, realtime update, and
  audit entry.
- Simulate unavailable Chat and confirm unrelated tools remain usable.
- Verify permanent and high-value actions generate alerts.
- Verify unauthorized staff cannot retrieve hidden fields directly through the API.

## Delivery sequence

### Milestone 1: visibility and safety

- global audit explorer;
- operational home/status page;
- server-backed preview and confirmation dialog;
- read-only player support snapshot;
- frontend route and feature separation.

### Milestone 2: support workflow

- structured support cases;
- reason categories and templates;
- compensation packages;
- expiry and repeat-restriction improvements.

### Milestone 3: investigation and corrections

- Chat report/evidence workflow;
- correction by original compensation operation;
- security alerts and expanded audit review.

### Milestone 4: game operations

- announcements and maintenance controls;
- scheduled events and bounded feature flags;
- rollback and approval workflows;
- multi-operator roles when needed.

## Recommended next implementation slice

Proceed with structured support cases. The dashboard's P0 workflows and frontend
feature boundaries are now present, so the next most valuable addition is durable
support context that connects investigation notes to existing audited operations.

The next slice should include:

1. Add a lightweight append-only support case model with external reference,
   category, status, related player IDs, and timestamps.
2. Add append-only case notes and links from cases to existing LiveOps operation IDs.
3. Allow mutation forms and audit filters to use the structured case reference while
   preserving operation-level reasons and immutable audit history.
4. Add case list, detail, and player-linked views with permission-aware data exposure.
5. Cover case concurrency, immutable notes, authorization, and operation linking.

## Explicit non-goals

- arbitrary SQL or database editing;
- generic item deletion or balance setting;
- public access to the LiveOps API;
- browser-held internal service credentials;
- unrestricted bulk bans, grants, or messages;
- an unbounded archive of private Chat messages;
- conventional hidden shadow bans;
- arbitrary production configuration editing.

## Definition of success

This roadmap is successful when the operator can understand system health, find and
review any privileged action, investigate a player with sufficient read-only
context, and execute high-risk operations through validated previews—without
requiring direct database access or exposing broad administrative primitives.
