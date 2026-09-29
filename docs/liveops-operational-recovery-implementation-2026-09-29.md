# LiveOps operational recovery implementation

**Date:** 29 September 2026 (Europe/Copenhagen)  
**Services:** API.LiveOps and its Angular frontend, shared Game persistence/outbox, API.LL outbox worker, Worker.LL daily telemetry.  
**Continues:** [the administrator workflow implementation](liveops-review-implementation-2026-09-28.md), particularly review findings F09 and F10.

## Delivered behavior

### Operations available across browsers

Overview now links to **Operations & recovery**. The page lists the signed-in operator's received operations in the current environment, with an unknown-only filter, pagination, receipt inspection, relevant target links, and an internal diagnostic copy action. The application restores unknown references from this register at sign-in; browser storage is no longer the only source of the recovery list.

The API commits receipt metadata in a separate database context before executing an explicitly tracked mutation. An interrupted response or rolled-back action therefore leaves a recoverable **Unknown** reference. An accepted request is not presented as a completed change. **Committed** requires a successful action result or a matching owned audit receipt. **Rejected** only resolves the original first attempt; a rejected retry cannot erase uncertainty about an earlier attempt. The frontend also distinguishes a definite server rejection from an ordinary failed retry response.

The server serializes registration per operator/environment/action/target and blocks replacement references while an earlier matching operation remains unknown. Retrying with the original reference still uses the existing action validation, review and idempotency checks. No action is automatically replayed. If receipt storage cannot be written, execution does not start. If recovery loading is unavailable or exceeds the 1,000-reference startup bound, action preparation is blocked with a visible explanation and refresh path. The full paginated register remains available for investigation.

Tracked actions are compensation grants, package saves/grants, signet grants, account moderation/revocation, Chat mute/unmute, case creation/notes/status/operation links/follow-ups, and investigation status/notes. Reads, previews, private draft autosaves, audit exports, and equipment migration endpoints do not create register entries. Registration occurs after authorization and model/antiforgery checks; it is not a log of every HTTP request. Requests that never reach the API cannot create a server reference. Earlier operations retain their audit lookup but are not backfilled into the new register.

Only identifiers, action/source/target type, outcomes, attempts, and timestamps are persisted. Reasons, notes, submitted payloads, preview credentials and confirmation text are excluded. A restored reference does not restore authorization or reconstruct an action payload. The administrator must review the original preparation before retrying.

### Delivery evidence for a specific operation

New Game outbox messages carry an optional administrative operation reference. The Game API's delivery worker restores that reference in each isolated consumer scope, so follow-on messages retain it without leaking it into unrelated deliveries. The recovery page queries exactly this reference and shows retained Pending, Processing, Failed and Processed counts plus up to 50 consumer records. Payloads are not exposed.

An owned committed Game operation is required to inspect delivery records. Existing outbox retention still applies (processed messages are normally retained seven days, failed messages thirty days). **Processed means the consumer completed; it is not a player-client acknowledgement.** No retained rows cannot establish that no delivery was required. Older rows remain uncorrelated rather than receiving guessed references.

Chat outcomes can be reconciled through their owned moderation audit receipt. Chat does not currently provide an operation-specific client delivery acknowledgement; the interface explicitly states this limitation. No Chat service changes or unsupported repair command were introduced.

### Essential job schedules and missing executions

Overview independently reads the actual persisted Quartz registry for daily telemetry, Tournament Grounds progression, marketplace expiration and region boss progression. It shows each registered schedule, next execution, latest recorded start/completion and cluster check-in, with explicit states for unregistered/unscheduled jobs, paused/error triggers, missing worker check-ins, overdue dispatch, absent execution records, long-running work and recorded failure.

This uses the worker's existing Quartz tables and shared essential-job identifiers, avoiding a second copy of configured schedules. Daily telemetry now runs through the existing execution service with a UTC report-date business key; the other essential jobs already use it. The thresholds are diagnostic defaults: five minutes of scheduling grace, two minutes without a worker check-in and one hour for a long-running execution. A check-in or scheduled dispatch never proves a business result. A disabled feature or older worker may explain missing execution evidence and still needs operator interpretation.

If Quartz tables are absent or unreadable, schedule evidence is explicitly unavailable. The panel does not infer healthy or missing jobs. A failed refresh labels any previous results as an earlier check. Recorded job exceptions remain accessible through a working button on Overview. Dependency readiness and schedule evidence are separate checks with separate timestamps.

## Changed files

| Boundary | Files and purpose |
|---|---|
| LiveOps API | `Operations/TrackOperationAttribute.cs`, `OperationTrackingFilter.cs`; new `Controllers/OperationsController.cs` and `JobSchedulesController.cs`; explicit attributes in account moderation/risk, Chat, compensation/packages, nobility and support-case controllers; registrations in `Program.cs`. |
| Domain/application | `Domain/Models/Administration/OperatorOperation.cs`; optional reference in `Domain/Models/Outbox/GameEventOutboxMessage.cs`; scoped `Application/Interfaces/Outbox/AdministrationOperationContext.cs`; shared `Application/BackgroundJobs/EssentialBackgroundJobs.cs`. |
| Persistence/services | New operation repository/configuration and `BackgroundJobs/BackgroundJobScheduleReader.cs`; outbox mapping/enqueue changes; dependency registrations; additive migration, designer and model snapshot. |
| Game/worker | `API.LL/HostedServices/GameEventOutboxWorker.cs`; `Worker.LL/BackgroundJobs/{BackgroundJobNames,BackgroundJobInfrastructureServiceCollectionExtensions,DailyTelemetryJob}.cs`. |
| Frontend | New `features/operations/operations.component.ts` and `features/dashboard/job-schedules.component.ts`; API/models/routes; operation journal and shell recovery; dashboard wiring; player, case and investigation preparation guards. |
| Verification | New `LiveOpsOperationalRecoveryTests.cs`, `operational-recovery.spec.ts`; expanded `GameEventOutboxTests.cs`. |

Frontend paths are relative to `LL/src/Presentation/liveops/src/app`; backend file names retain their existing repository boundaries. Unrelated equipment, inventory and balance work in the shared checkout was preserved.

## Verification

| Check | Result |
|---|---|
| Frontend `npm test --prefix LL/src/Presentation/liveops` | **93 passed.** Includes fresh journal restoration, multiple pages, interrupted recovery, operator/environment isolation, completed-receipt precedence, definitive rejection, bounded recovery, stale detail responses and stale schedule evidence. |
| Frontend `npm run build --prefix LL/src/Presentation/liveops` | Passed, production bundle. npm cache remains under `%TEMP%`. |
| Backend Release build, cached restore, isolated `%TEMP%` artifacts | Passed; existing unrelated analyzer/nullable warnings remain. |
| `build/run-tests.ps1 -NoBuild -ArtifactsPath <temporary artifacts> -Filter 'FullyQualifiedName~LiveOps\|FullyQualifiedName~TelemetryHistory\|FullyQualifiedName~GameEventOutbox\|FullyQualifiedName~BackgroundJob\|FullyQualifiedName~WorkerServiceProvider\|FullyQualifiedName~TournamentGroundsProgressionJob'` | **148 passed, zero skipped.** In PowerShell use literal `|` characters, without the Markdown escape backslashes shown in this table. |
| PostgreSQL integration coverage | Receipt survival across rollback, concurrent replacement prevention, owned receipt reconciliation, pagination, exact outbox correlation, pre-execution registration, storage failure preventing execution, migration preservation, actual Quartz query/epoch/cron mapping, missing and recorded execution states. Disposable local databases only. |
| Outbox worker verification | Confirms separate delivery scopes propagate the originating reference and leave unrelated messages uncorrelated. |
| EF pending-model check / generated migration SQL | No pending model changes; additive SQL reviewed. |
| API.LiveOps Release publish | Passed to an isolated local artifact directory. |
| Local browser walkthrough | Re-saved the existing synthetic case follow-up plan, observed revision 5 and its committed server receipt, opened exact delivery coverage, checked job evidence unavailable without Quartz tables, and verified the exceptions button opens Jobs. Existing private note/resolution were preserved. |
| Final layout and source checks | Receipt persisted after a full reload; desktop and 390-pixel layouts checked without horizontal document overflow; temporary viewport reset; browser error log empty. Scoped `git diff --check` passed. |

The UI walkthrough uses only `http://127.0.0.1:4411` with synthetic records. No compensation, account moderation, message, private case note or resolution was submitted. A missing Quartz registry in this preview is intentional; active schedule and missing-execution cases are exercised against PostgreSQL in the tests.

No required verification command remains blocked. Initial restricted-process restart access was resolved through the normal approval mechanism for the verified local preview. Production sign-in, real Chat, live Game-client delivery and production-scale performance were not exercised. These remain verification boundaries, not claimed successes.

## Migration and release implications

New migration: **`20260928221316_TrackLiveOpsOperations`**, following `20260928212731_ScheduleSupportCaseFollowUps`.

It creates `OperatorOperations` with an operator/environment/reference primary key and listing index, and adds nullable `AdministrationOperationId` plus its index to `GameEventOutboxMessages`. Existing outbox rows remain intact with a null reference. There is no destructive change in `Up`; rolling `Down` loses the new recovery/correlation metadata.

Apply the additive migration through the normal release process **before** the updated applications use the schema. Release the matching LiveOps API/frontend; release API.LL for correlation propagation in follow-on deliveries and Worker.LL for recorded daily telemetry executions. The LiveOps database identity needs normal access to the new register and read access to the worker's existing Quartz registry tables. No new package, secret, application permission role or configuration key is required. Quartz schema provisioning remains the existing worker deployment responsibility; this change does not create or modify those tables.

The migration was applied only to disposable tests and the isolated synthetic preview. **No shared/production database was changed and no service was externally deployed.**

## Remaining opportunities

- [The delivery recovery follow-up](liveops-delivery-recovery-implementation-2026-09-29.md) adds a bounded Game state-refresh repair and current Chat restriction evidence. Client acknowledgement and other repair types still require explicit service contracts. No generic retry-all or simulated repair action is shown.
- Production measurements remain necessary before restructuring support snapshot loading or changing bounded search/package contracts.
- Currency-flow instrumentation, incident grouping, notifications and additional staff approval roles remain separate product work from this implementation.
- The register has no new automatic deletion policy. Any future retention policy must preserve unknown outcomes and account for operational/audit requirements.
