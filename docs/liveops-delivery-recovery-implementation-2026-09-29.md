# LiveOps delivery recovery implementation

**Date:** 29 September 2026 (Europe/Copenhagen)  
**Target services:** API.LiveOps, its Angular frontend, and shared Game administration/persistence code.  
**Continues:** [Operational recovery implementation](liveops-operational-recovery-implementation-2026-09-29.md).

## Administrator behavior

An administrator can now review and retry an eligible failed player state-refresh notification from Overview delivery diagnostics or an operation's delivery evidence. The recovery page explains the effect, saves a private reason draft, obtains a server-verified preview, and requires the existing super-administrator permission before submission.

Successful submission shows **Replacement notification queued**, with links to the replacement's operation status, audit receipt and player cases. It does not claim that the player received an update. The operation register supports a direct link to a specific operation, including one outside the first results page. Interrupted responses retain the original operation reference for inspection and an unchanged retry.

Overview follows the replacement after it is queued. The original failed row remains retained, but no longer contributes to the actionable failure count or offers another repair from the queue. Per-operation evidence still includes that failure, with a link to its repair receipt. A failed replacement becomes actionable under its own delivery reference.

For Chat operations, the result also shows the **current server restriction**: Muted, No active mute, or Unavailable, with the check time and available restriction/expiry information. Current state and the specific operation's committed receipt are separate evidence. A later moderation action or expiry may explain the current state. Chat unavailability cannot erase a previously established committed outcome.

## Recovery contract and design

- Only a retained **Failed** `realtime-delivery` consumer of a `realtime.delivery_requested` message is eligible. Its audience must be exactly one matching character, and its payload must be `StateInvalidated` or `StateInvalidations`, with positive revisions and recognized character state scopes. Batched revisions are bounded at 32 scopes.
- Recovery queues one replacement message and one realtime consumer, preserving the original payload/revisions. A new message/update identifier permits delivery after a prior partial attempt; clients still use revision-based state refresh. The command changes no inventory, currency, rewards, account restrictions or Chat state.
- The failed delivery keeps its status, attempts and error for diagnosis. Normal outbox retention still applies. The replacement is correlated to the repair operation and uses the existing Game worker's delivery/retry policy.
- Advisory locks serialize the operation and original delivery. The replacement and append-only audit receipt commit atomically through the existing command transaction. One original delivery can receive only one replacement, regardless of operator or operation reference.
- Repeating the same reference, operator and normalized reason returns the original replacement receipt. Changed requests and new references targeting an already-repaired delivery are rejected. A replacement that later fails may itself be reviewed for repair.
- Preview tokens bind the operator, reason, delivery and current recovery plan, expire normally, and are revalidated at submission. Existing receipt replay remains possible after the original delivery has aged out of retention.
- The private preparation draft contains only the reason; it contains no preview token or confirmation credential. The operation register retains its existing metadata-only contract and ownership/environment boundaries.

This intentionally provides no generic retry for gameplay, reward consumers, jobs, broadcasts or Chat commands. Consumer completion remains distinct from client acknowledgement; Chat has no operation-specific client acknowledgement contract.

## Changed files

Paths below are relative to the repository root; grouped names identify the implementation boundary.

| Boundary | Files and purpose |
|---|---|
| Domain/application | `LL/src/Core/Domain/Models/Administration/StateRefreshRecovery.cs`; new action and preview kinds; recovery service interface, result DTO, mapping profile and `RetryStateRefreshDeliveryCommand` under `Core/Application`; optional repair reference in `OperatorOperation.cs`. |
| Game services/persistence | `StateRefreshRecoveryService.cs` and `StateRefreshRecoveryRepository.cs` under the existing administration folders; dependency registrations; reason-draft key validation in `OperatorDraftService.cs`; computed repair receipt reference in `OperatorOperationRepository.cs`. |
| LiveOps API | New `Controllers/DeliveryRecoveryController.cs` and `Previews/StateRefreshRecoveryPreview.cs`; integration with `LiveOpsActionPreviewService.cs`; current Chat evidence in `OperationsController.cs`; actionable failure filtering in `Health/LiveOpsOperationalStatusService.cs`. |
| Frontend | New `features/operations/delivery-recovery.component.ts`; extracted `operations.component.html` and operation-page changes; dashboard links/count explanation; API client, models, routes and audit presentation labels. All under `LL/src/Presentation/liveops/src/app`. |
| Tests | New `LL/tests/EssenceSystem.Tests/LiveOpsDeliveryRecoveryTests.cs` and frontend `delivery-recovery.spec.ts`; expanded operational-status checks and handler-registration count. |

The existing inventory, equipment and balance changes in the shared checkout were preserved.

## Verification

| Check | Result |
|---|---|
| `npm test --prefix LL/src/Presentation/liveops` | **98 passed.** Includes lost responses, reused references, immutable reviewed reasons, permission/recovery-loading guards, stale route responses, successful queue receipts and direct links outside the first register page. |
| `npm run build --prefix LL/src/Presentation/liveops` | Passed, production bundle; npm cache outside the checkout under `%TEMP%`. |
| Release backend build | Passed with existing unrelated analyzer/nullable warnings. |
| Required `build/run-tests.ps1` wrapper | **163 passed, zero skipped**, including local PostgreSQL integration tests. Filter covers LiveOps, GameEventOutbox, TelemetryHistory, BackgroundJob, WorkerServiceProvider and TournamentGroundsProgressionJob. |
| Recovery coverage | Single and batched revisions; unsupported states/consumers/audiences/payloads; atomic audit/replacement; replay/conflict handling; rollback/concurrency; permission attributes; preview operator/reason/target/expiry/state binding; original failure retention, replacement queue filtering and repair-receipt linkage. |
| Chat coverage | Owned-operation access, current mute/absence, unavailable responses and exceptions; current state never establishes commitment or erases a committed outcome. |
| EF pending-model check | No pending model changes. |
| API.LiveOps Release publish | Passed into an isolated local artifact directory. |

The backend command used an isolated `%TEMP%` artifact directory and `-NoBuild` after the Release build:

```powershell
./build/run-tests.ps1 -NoBuild -ArtifactsPath "$env:TEMP/ll-liveops-admin-verification" -Filter 'FullyQualifiedName~LiveOps|FullyQualifiedName~GameEventOutbox|FullyQualifiedName~TelemetryHistory|FullyQualifiedName~BackgroundJob|FullyQualifiedName~WorkerServiceProvider|FullyQualifiedName~TournamentGroundsProgressionJob'
```

PostgreSQL tests use `LL_LIVEOPS_TEST_POSTGRES` against a loopback-only disposable server, creating and dropping unique test databases. No credentials are checked in. An initial run without the expected environment variable skipped PostgreSQL cases; the completed run above executed all of them.

### Local browser verification

The isolated preview at `http://127.0.0.1:4411` uses synthetic records. A failed refresh fixture for PreviewPlayer was reviewed and submitted once. The result was a committed recovery operation with one separately reported Pending replacement, zero delivery attempts, and the original Failed row preserved. The committed result persisted after an API restart and page reload. Overview then excluded the original from actionable failures and displayed the pending replacement.

Recovery operation: `eaff697e-5efe-4f49-9a27-827208b91bbe`. Original delivery: `658cf5b0-1170-47af-9c59-960c5e1249e8`. Replacement delivery: `4ef9f8f5-ba09-452d-a73a-d773cfb2d7e4`.

The recovery form and operation result were checked at a 390-pixel viewport with no horizontal document overflow; the temporary override was reset. The final browser error log was empty. The repair result was left open and a full-page screenshot saved as `liveops-state-refresh-recovery.png` in this chat's local artifact directory. Scoped `git diff --check` passed.

No Game worker is connected to this preview, so the replacement remains Pending. No real player holdings or moderation state were changed. Real Chat connectivity, live client receipt and production-scale performance were not exercised.

## Migration, configuration and release implications

**No new migration, configuration key, package or permission role is introduced in this increment.** It uses the prior `20260928221316_TrackLiveOpsOperations` schema and existing administration audit/outbox tables. That previously documented additive migration must precede releases using these features.

Release the matching LiveOps API/frontend. The Game delivery worker must be running normally for queued replacements to progress. Keep deployments that read the shared `AdminActionType` enum compatible with the appended `StateRefreshDeliveryRetried` value. No Chat service deployment is required for the current-state read.

No shared or production database was modified and no service was externally deployed. The local preview was refreshed only after the build succeeded. No required verification command remains blocked.

Remaining work from the broader review includes explicit client-acknowledgement contracts, production measurements, currency-flow instrumentation, incident grouping and additional staff approval roles; these are separate from this bounded recovery implementation.
