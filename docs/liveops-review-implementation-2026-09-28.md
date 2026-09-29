# LiveOps review implementation

**Date:** 28 September 2026  
**Scope:** the administrator workflow findings in [the current-state review](liveops-current-state-review-2026-09-28.md).  
**Services:** LiveOps Angular frontend, API.LiveOps, and the existing support-case/draft application, domain and persistence boundaries.

The implementation repairs the reproduced navigation, request-ordering and preparation-loss problems, adds scheduled case follow-ups, and connects evidence, compensation and reporting more clearly. It preserves server-verified action reviews, idempotency, append-only case history and private operator drafts. The sections below distinguish delivered changes from work that still requires additional service contracts or production measurements.

## Delivered workflow changes

| Review finding | Implemented behavior | Remaining boundary |
|---|---|---|
| F01: investigation request ordering | Only the latest request can change results, counts, errors or loading state. Old rows clear when a new filter request begins. | A failed request remains unavailable rather than showing invented counts. |
| F02: next investigation loops | Next moves forward through the retained order, rechecks a shortened page, fetches later pages and reaches an explicit end. Opening a record does not mark it reviewed. | Direct links without queue context explain why Next is unavailable. There is no combined Save and next action. |
| F03: lost preparation | Player action, package-edit and investigation preparation use private server-backed drafts scoped to operator and target. Unreadable/conflicting drafts block editing until resolved. | Saved preparation is not a saved authorization: previews and confirmation text are excluded and a fresh review is required. |
| F04: active case lost in support | Player workspace verifies the case belongs to the player, displays its title/status and returns findings to that case. The local finder starts collapsed when arriving from a case. | Invalid or mismatched case context is cleared. |
| F05: audit quick-view scope | Quick views clear an earlier operation reference, player, actor, external reference and dates before applying their own constraints. | The existing audit source permissions still apply. |
| F06: unavailable versus empty | Investigation and audit failures have distinct unavailable messages; initial investigation counts are unknown until a response succeeds. | Source availability and retained coverage remain visible. |
| F07: follow-up scheduling | Cases have Normal/High/Urgent priority, optional follow-up time and a required next action when saving a plan. Plans are version-checked, idempotent and appended to case history. Overview includes due work; Cases can filter category/due status and sort by follow-up, priority, oldest or recent. | Due means Open or Waiting with a scheduled time at or before now. No notification is sent. |
| F08: evidence assembly | A searchable evidence collector selects up to 12 loaded facts from holdings, saved run rewards, acquisitions, compensation, transfers and delivery snapshots. It copies references/coverage or appends them to the active case's private note draft, preserving existing preparation. | The administrator still explicitly saves the case note. Bounded evidence does not prove entitlement; a delivery snapshot does not identify a particular grant's delivery. |
| F09: operational diagnostics | Delivery/job/restriction drilldowns paginate, support applicable recorded-status filters and can copy a diagnostic page with time/filter/coverage. Player rows link to their cases. | Schedule-aware detection of jobs that never started, configurable age filters and repair commands are not implemented. See the service-contract work below. |
| F10: uncertain operation recovery | Exact-reference audit lookup explicitly distinguishes a server receipt from an unknown outcome and can be opened from another browser using that reference. Existing original-ID retry safeguards remain. | The unresolved-operation list remains browser-local. No new submission ledger or operation-specific delivery correlation was introduced. |
| F11: report dates and saved views | Activity trend and comparison end on the selected report day. Itemization has its own freshness warning. Report choices, filters and adoption preferences persist in the private workspace draft. | Retention still bounds available comparisons; gaps remain missing rather than zero. |
| F12: content counting units | Colosseum played/lost labels explain that losses overlap played games. Content rows explain mode/date attribution and can expand into retained daily values. | These are observed outcomes, not a newly instrumented completion funnel. |
| F13: useful comparisons/export | Economy includes zero-balance share and median change against a chosen earlier matching cohort. Adoption summarizes filtered comparisons; Itemization sorts by sample, battles or context. Analytics pages export their filtered rows with dates, units, filters and coverage. | No currency source/sink collection, causal balance claims or new game-event instrumentation. |
| F14: compensation workflow | Use saved package, Grant individual item and Create/edit package are separate modes. Saved-package use keeps target/reason nearby, shows exact saved version and resolves item names. Unsaved editor changes are explicit. | Search covers the existing bounded package library, not a newly paginated backend catalog. Every package remains subject to the existing high-value policy. |
| F15: summary audience | Copy actions explicitly identify internal summaries and inclusion of free-text reasons/resolutions. An editable player-response draft starts without internal case contents. | Nothing is sent to a player automatically. |
| F16: layout/accessibility | Compact readiness precedes the queue. Mobile chrome is smaller; all six navigation links remain visible. Added skip link, route-heading focus, current-page/section semantics and ordinary investigation links. | This is targeted keyboard/layout work, not a formal accessibility certification. |
| F17: continuity/search | Search puts exact references/names first and offers continuation into Cases/Players. Private search text stays out of URLs. Case filters/page and analytics settings persist; queue shortcuts start with clear constraints and then become the saved private view. Audit expands readable target identity while retaining IDs. | Pixel scroll position is not restored. Player search continues to use its bounded search contract. |
| F18: optional loading | Timing and conversation evidence load when their investigation section is opened. Evidence collection and draft binding have focused shared implementations. | The aggregate player snapshot still waits for its bounded section reads. No production latency measurement or broad component rewrite was undertaken. |

## Important design decisions

- **Reuse the existing private draft system.** New player/investigation scopes use explicit field allowlists, existing ownership checks and version-conflict handling. Preparation never stores a preview token or bypasses current permission checks. The Read permission permits an operator to save their own player preparation; execution permissions are unchanged.
- **Treat follow-up changes like other case commands.** A focused application command delegates to the existing case service/repository. Expected version, operation ID, transaction/locking and append-only evidence remain authoritative. Optional fields are omitted from old request serialization when null so unchanged retries keep their prior request hashes.
- **Keep evidence staging separate from case history.** Preparing selected facts updates the operator's private draft after verifying the case's player. It does not append a public/shared case note or overwrite a resolution. Package grant lines remain individually selectable even when they share an operation reference.
- **Keep report comparisons honest.** Economy compares the same resource, level band and activity window, while warning that membership can change. Exported missing values are blank, measured zeros stay zero, and text that could become a spreadsheet formula is escaped. Itemization does not borrow generation/snapshot timestamps from an unrelated daily report.
- **Retain existing operational authority.** A completed server receipt proves a recorded operation. An absent receipt and a generic pending-delivery count cannot establish whether a specific operation committed or reached a client. The UI states those limits.

## Changed files

This checkout also contains earlier LiveOps work and unrelated inventory/balance work. The latter was left intact. The principal files changed or added for this implementation are:

| Area | Files |
|---|---|
| Shell and continuity | `LL/src/Presentation/liveops/src/app/app.component.{ts,html,css}`, `src/styles.css`, `workspace-state.service.ts`, `shared/global-search.component.ts` |
| Investigations | `features/account-risk/account-risk.component.{ts,html}`, `account-risk-list-state.service.ts`, `account-risk-detail.component.{ts,html,spec.ts}` |
| Cases and player support | `features/cases/cases.component.{ts,html}`, `features/players/player-workspace.component.{ts,html}`, new `shared/preparation-draft.ts`, new `shared/support-evidence.component.ts` |
| Overview/audit | `features/dashboard/dashboard.component.{ts,html}`, `work-queue.component.ts`, `features/audit/audit.component.{ts,html}`, `shared/admin-presentation.ts` |
| Analytics | `features/analytics/analytics-state.service.ts`, `analytics.component.{ts,html}`, Activity/Content/Economy/Itemization templates and Adoption table/component, new `shared/csv-export.ts` |
| Client contracts | `LL/src/Presentation/liveops/src/app/liveops-api.service.ts`, `liveops.models.ts` |
| API | `LL/src/API/API.LiveOps/Controllers/{OperatorDrafts,SupportCases,OperationalStatus}Controller.cs`, `Health/LiveOpsOperationalStatusService.cs`, `Health/OperationalStatusModels.cs` |
| Application/domain | Support-case DTO/search query/service interface, new `PlanSupportCaseFollowUpCommand.cs`, `Domain/Models/Administration/SupportCase.cs`, `ISupportCaseRepository.cs` |
| Service/persistence | `Services.LL/Administration/{OperatorDraftService,SupportCaseService}.cs`, `Persistence.LL/Repositories/Administration/SupportCaseRepository.cs`, case configuration, migration and model snapshot |
| Tests | New `administrator-workflow-improvements.spec.ts`, updated analytics navigation/investigation tests; new `LiveOpsFollowUpTests.cs`; registration, operational-status and PostgreSQL workflow test updates |
| Record | This document; the analysis document remains a historical review of the pre-implementation state. |

Frontend paths shortened above are relative to `LL/src/Presentation/liveops/src/app`, except `src/styles.css`, which is relative to the frontend project. Backend paths identify the existing repository boundary rather than introducing new services.

## Verification

| Check | Result |
|---|---|
| `npm run build --prefix LL/src/Presentation/liveops` with npm cache under `%TEMP%` | Passed, production bundle. |
| `npm test --prefix LL/src/Presentation/liveops` | **85 passed.** Includes reversed response order, queue progression/page shifts, fresh due-queue scope, private preparation isolation, selected report date, evidence-draft preservation and CSV safety. |
| Release backend build using cached restore and an isolated `%TEMP%` artifacts path | Passed. Existing unrelated nullable/xUnit warnings remain. |
| `build/run-tests.ps1 -NoBuild -ArtifactsPath <temporary artifacts> -Filter 'FullyQualifiedName~LiveOps|FullyQualifiedName~TelemetryHistory'` | **100 passed, zero skipped.** The filter uses a literal `|` between the two clauses. PostgreSQL coverage includes follow-up replay/version checks, due ordering/filtering/pagination, private draft ownership, migration defaults/preservation, operational exception pagination and existing LiveOps workflows. |
| API.LiveOps Release publish | Passed to an isolated local artifact directory. |
| EF migration SQL and pending-model check | SQL reviewed; no pending model changes. Migration tested against disposable local databases. |
| Scoped `git diff --check` | Passed; repository line-ending notices are not whitespace errors. |
| Local browser: follow-up | Saved a synthetic High-priority plan; observed revision/history update and appearance in Due follow-ups. Existing private note/resolution survived. |
| Local browser: case/player/package | Verified readable current-case context, finding links to the same case, collapsed finder, separate saved-package flow, version and item names. Unsubmitted reason/package survived navigation and a full reload; the temporary reason was cleared afterward. |
| Local browser: analytics | Verified historical report day controls chart endpoint and survives reload; Economy comparison and its help text; downloaded CSV inspected for correct report/baseline/filter/value context. |
| Local browser: layout | Desktop and 390-pixel case layout checked; no document overflow. Temporary viewport override reset. Browser error log was empty during the final walkthrough. |

The backend run used the repository's required test wrapper. Its build outputs were isolated because the developer's other running processes use the ordinary output directories. PostgreSQL credentials remained local and were not added to source or this document.

The browser used only the isolated synthetic preview at `http://127.0.0.1:4411`. The one submitted change was the synthetic follow-up plan. No new compensation, moderation, note, resolution or external message was submitted during this implementation walkthrough. The evidence-transfer path was tested with controlled case/draft responses to preserve the existing preview note.

No required relevant command remains blocked. Not exercised: production Google/MFA sign-in, live Chat, real Game-client delivery, production-scale queue/report load, a formal assistive-technology audit or a timed real administrator study. The browser download-event observer timed out, but the actual CSV was downloaded successfully and its contents were verified on disk.

## Migration and release implications

New migration: **`20260928212731_ScheduleSupportCaseFollowUps`** in `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations`, with its designer and updated `LLDbContextModelSnapshot.cs`.

It adds nullable `FollowUpAt` (`timestamp with time zone`), nullable `NextAction` (500 characters), `Priority` (default Normal/0), and the `(Status, FollowUpAt)` index on `SupportCases`. Existing cases retain their data and start unscheduled at Normal priority.

Apply this migration through the normal release process before the new case queries are used. Release the matching LiveOps API and frontend together. The existing database-side draft storage remains in use; no new configuration, secret, package dependency or permission role is required. Follow-up timestamps are stored in UTC and edited/displayed with the administrator's local timezone.

The migration was applied only to the disposable/synthetic local verification databases. **No shared or production database was changed, and no service was externally deployed.** Rolling the schema down removes follow-up data; an application rollback should ordinarily retain the additive schema pending a deliberate recovery decision.

## Remaining service-level work

**Update, 29 September:** the [operational recovery implementation](liveops-operational-recovery-implementation-2026-09-29.md) now delivers the server operation register, exact Game outbox correlation and actual schedule/missing-execution diagnostics described below. Chat client acknowledgement and supported repair commands remain open. The table above records the scope of the 28 September implementation.

These items are not represented as completed UI functionality:

1. A durable server-side submission/recovery ledger and operation-specific Game/Chat delivery correlation. Current exact-reference receipt lookup works across browsers, but cannot recreate a lost local list of uncertain operation IDs.
2. A schedule-aware registry for essential background jobs, including missing executions, plus supported per-operation repair commands with authorization, idempotency, review and receipt semantics. The current view reports retained execution exceptions and available report freshness.
3. Production timing measurements before splitting the aggregate support snapshot into independently rendered requests, and representative volume testing before changing package/search pagination contracts.
4. New currency-flow or game-event instrumentation if subsequent administrator use establishes a need. Current balance/adoption comparisons deliberately keep their existing denominators and coverage.

Incident grouping, notifications and additional staff approval roles remain the review's later product opportunities. They have not been added speculatively to the solo-administrator workflow.
