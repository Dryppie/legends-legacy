# LiveOps administrator implementation

Implemented 28 September 2026. This records the work following the [administrator usability review](liveops-administrator-usability-review-2026-09-28.md).

The implementation adds supported workflows across the eight delivery slices. It does not complete every recommendation or establish production readiness; the remaining boundaries are listed below. The changes are in the checkout and have not been deployed. The new migration has been exercised in a disposable local test database; it has not been applied to a shared or production database. The existing private access boundary, permission policies, antiforgery protection, and Game transaction pipeline remain in use.

## What the administrator can now do

| Area | Implemented behavior |
|---|---|
| Overview | Resume open/waiting cases and unreviewed/in-progress investigations from four independently loaded queues, with bounded results and direct links. Inspect delayed/failed deliveries, restrictions expiring within seven days, and recorded job exceptions. Every detail view explains its impact and a supported next step. Open cases, player lookup, investigations, or the exact 24-hour audit scope. Unknown counters remain unknown when a dependency fails. |
| Players | Retain the finder while moving between pages, collapse it for more room, and use focused Summary, Inventory and rewards, Activity and transfers, Restrictions, Chat, Compensation, Support history, and Signets sections. Missing-item and activity shortcuts explain which evidence to compare. |
| Action review | Use a server-verified preview for moderation, single-item compensation, packages, and Signets. The dialog shows the exact target, operator, environment, operation reference, effects, warnings, and expiry. Keyboard focus stays inside; Escape/cancel cannot dismiss an in-flight submission. Errors and refresh-review controls are inside the dialog. |
| Recovery | A failed lookup or refresh has visible feedback. Old snapshots are identified as retained evidence. Request generations prevent old target responses from replacing the current player. Target-bound drafts are cleared on a target change. Uncertain mutations retain their operation reference for an unchanged retry. |
| Investigations | Move between Summary, Transfers, Conversation context, Timing, and Notes. Open the subject's player workspace, jump from supporting transfer references to transfer rows, return to the retained queue, or move to another unreviewed account in that queue. Notes and status retries reuse their operation reference. |
| Activity log | Read action labels and effect summaries, expand technical JSON only when needed, copy complete references, and open exact operation receipts. Receipt/dashboard links establish their own scope rather than inheriting unrelated filters. Filter state survives navigation in the current workspace. |
| Cases | Open an issue for an exact player; record a category, description, external ticket reference, evidence notes, decisions, and completed Game/Chat operations. Resume cases from a searchable, paged queue. Close and reopen cases with a recorded explanation. |
| Compensation | Build a small library of named, versioned packages from valid catalog items and equipment definitions. Review each expanded line before granting. Save a new revision or archive a package. Signets have a dedicated server preview and retain their existing issuance and audit path. |
| Analytics | Inspect daily-active-account trends, gaps, comparable calendar periods, retention denominators, and filtered content/adoption/economy tables. Inspect the existing itemization report's cohorts, distributions, build combinations, essence usage, outcome bands, choice context, and seven-day award outcomes. |

Text and controls are larger, navigation wraps, identifiers can be copied without becoming the primary label, and timestamps show their timezone. Analytics identifiers receive readable labels, including floor labels and formatted definition names; this is not a new localization/catalog-name service.

## Daily support workflow

1. Open **Players**, search by name/account/email/identifier, and select the exact result. Check account and Chat status; an unavailable Chat service is shown as unknown.
2. Use **Missing reward or item** or **Activity not progressing** to reach the relevant retained evidence. Compare the saved run, claims, holdings, acquisitions, expected activity timing, restrictions, and delivery evidence. Coverage and freshness remain visible.
3. Open a **support case** for that player and record the issue. From the case, choose **Investigate [player]** to carry its reference into the player workspace.
4. If compensation or moderation is justified, enter a reason, review the server's exact effect, and submit once. An accepted mutation produces an operation receipt. A network failure instead leaves an uncertain operation and its activity-log link.
5. Use the receipt's **Link this operation to the case** action, explain the relationship, and save the verified link. Record the resolution and move the case to Resolved or Closed.

To create a package, select valid items in **Compensation**, append the desired lines to the package draft, enter its name and purpose, and save it. Packages are initially empty: this change does not seed arbitrary player rewards. Select a saved active version before reviewing a grant. Editing a package requires another saved version before it can be granted.

## Data and retry guarantees

### Support cases

Cases are durable database records. Notes, decisions, and operation links form an append-only history; mistakes are corrected by a new entry. Each change has an actor-bound request hash and operation ID. Repeating the same request returns the existing result; changing the request under an existing ID produces a conflict.

Each case has an integer revision. A stale expected revision produces HTTP 409 before tracked changes occur. The browser retains the note/evidence draft and requires a refresh before another save. PostgreSQL advisory locks serialize operations on the case, and the revision is also an EF concurrency token. History uses an integer sequence cursor so equal timestamps do not skip entries. The queue has 25 rows per page, and each history request has at most 50 entries.

Game operation links must belong to the case's character or account. Chat links are checked against the exact character and operation; unavailable Chat evidence cannot be accepted as a verified link. Case content requires the existing account-moderation permission, including reads. General audit rows contain the case reference and change kind; case text is placed in the protected internal-notes field.

Saved cases survive reload and sign-out. Case creation, note/evidence, decision, and operation-link drafts are now saved privately against the operator identity. Player search text and case/audit filters also restore after signing in again. Search result records are fetched again rather than persisted. Wait for the draft-saved indicator: offline or conflicting edits remain only in the current tab until successfully saved. Compensation, moderation, and investigation form drafts are still in memory; no mutation payload or preview token is restored for automatic submission.

### Compensation packages

Package revisions are immutable. Saving checks the expected version, validates every line through the existing compensation rules, and records an audit action. The library returns up to 100 latest definitions, with active entries first. A package has 1–10 lines and stays within the configured total grant cap. Signets are excluded from packages.

The server preview expands the actual saved version into item names, quantities, binding, and equipment characteristics. Its token is bound to actor, target, request, and relevant current state. An edited or archived definition invalidates preparation of its old version.

Granting runs as one Game command. Each line uses a deterministic child operation ID derived from the package operation. If any line fails after a write, the service throws so the transaction rolls back; a failed response alone would not roll back this repository's transaction pipeline. The parent receipt records the package/version and child operations. Successful retries do not enqueue a second inventory event or grant another package. A previously completed operation remains resolvable after the definition is archived.

### Previews and uncertain responses

Moderation durations are calculated using the server clock. The preview returns the exact effective expiry, which the browser submits unchanged. Preview countdowns use the returned server time and elapsed browser time instead of trusting the workstation's clock.

The browser journal stores operation metadata in local storage, scoped to operator and environment: IDs, action kind/source, outcome, and timestamp. It stores no reason, evidence text, mutation payload, or preview token. It retains unresolved operations and up to 20 resolved receipts. A restored in-flight entry becomes Unknown. Rejecting a retry does not establish the original request's outcome, so it does not erase an earlier uncertainty. Finding its completed audit receipt resolves the journal entry. At sign-in, and on demand, recovery checks at most 20 uncertain operations against readable audit receipts. It requires an exact operation, actor and source match; an absent receipt or unavailable source leaves the outcome uncertain. Recovery never retries a mutation.

For an uncertain result, inspect the activity log or retry the unchanged request. Do not interpret an empty audit page as proof that no request committed, especially when a source is unavailable. The journal is a local recovery aid, not a server-wide ledger of all failed attempts.

## API and storage changes

| Contract | Permission | Purpose |
|---|---|---|
| `GET/POST /api/liveops/drafts/workspace` | LiveOps read | Restore/save the current operator's search and filter preferences |
| `GET/POST /api/liveops/drafts/{case\|new-case}/{targetId}` | Account moderation and LiveOps read | Restore/save private case drafts with revision checks |
| `GET/POST /api/liveops/cases` | Account moderation | Search or create cases |
| `GET /api/liveops/cases/{caseId}` | Account moderation | Read a case and a bounded history page |
| `POST /api/liveops/cases/{caseId}/notes`, `/status`, `/operations` | Account moderation | Append evidence, record a decision, or verify an operation link |
| `GET/POST /api/liveops/compensation-packages` | Economy compensation | Read the library or save an immutable revision |
| `POST /api/liveops/compensation-packages/preview`, `/grant` | Economy compensation | Review and grant an exact package version |
| `POST /api/liveops/characters/{characterId}/signets/preview` | Economy compensation | Review an Alpha Signet grant |
| Existing Signet grant endpoint | Economy compensation | Now requires a valid preview token |
| `GET /api/liveops/status/details?view=deliveries\|restrictions\|jobs` | LiveOps read | Read a bounded operational queue |
| Existing moderation preview endpoints | Existing moderation policy | Accept optional duration minutes and return exact server-derived expiry |

The migration is [20260928180355_ImproveLiveOpsAdministration](../LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260928180355_ImproveLiveOpsAdministration.cs). It creates only `SupportCases`, `SupportCaseEntries`, and `CompensationPackageVersions`, with their indexes and the case-history foreign key. The follow-up migration `20260928191934_PersistOperatorDrafts` adds only `OperatorDrafts`, keyed by actor subject and draft reference. Both migrations include designers and the model snapshot. There are no new package dependencies or required configuration keys.

Application requests use the existing CQRS and transaction conventions. New services operate through repository interfaces; EF queries and persistence locks remain in Persistence. Existing Game-owned compensation and Signet issuance enforce the underlying rules. No duplicate Signet audit writer was introduced: its existing receipt was enhanced with high-value classification and issuance identity.

## Verification

| Check | Result |
|---|---|
| `npm run build` in `LL/src/Presentation/liveops` | Passed |
| `npm test` in the same directory | 59 passed |
| Backend Release build of `LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj`, using cached restore metadata and a temporary artifacts directory | Passed |
| `build/run-tests.ps1 -NoBuild -ArtifactsPath <temporary artifacts> -Filter 'FullyQualifiedName~LiveOps\|FullyQualifiedName~Nobility\|FullyQualifiedName~Signet'` | 118 passed, 0 skipped, 0 failed with the disposable PostgreSQL server enabled |
| Release `dotnet publish` to an isolated temporary directory | Passed; API assembly, frontend `wwwroot/index.html`, and item content verified in the artifact |
| PostgreSQL 17 integration tests | Migration upgrade/replay preserves existing rows; conflicting case edits and duplicate retries behave correctly; the real command pipeline rolls back a partial package and emits one inventory event on duplicate success |
| EF `migrations has-pending-model-changes` against the compiled Persistence assembly | No pending model changes |
| EF migration SQL generation from the preceding migration | Passed; three new tables and five indexes; no alterations/drops of existing tables |
| Scoped `git diff --check` | Passed; Git reported only normal Windows line-ending notices |
| Local browser walkthrough with synthetic data | Player lookup, target summary, compensation navigation, cases/history, overview, and analytics inspected. Narrow layout at 390 CSS pixels and analytics reflow at 640 CSS pixels checked; chart overflow found and fixed. |
| Published API/browser walkthrough against isolated PostgreSQL | Passed: development sign-in, player lookup, case creation, note persistence after reload, package version save, server preview and confirmation, grant receipt, verified case-operation link, and inventory evidence showing exactly two granted items. |

New tests cover A–B–A target response races, draft isolation, failed searches and refreshes, uncertain grant retries, unresolved-journal retention, server-derived restriction expiry, keyboard focus and preview dismissal, preview expiry under clock skew, case conflict recovery, audit receipt scoping, calendar gaps and period comparisons, case idempotency/target validation/paging/immutability, package validation/versioning/replay/preview invalidation/partial-failure signaling, DTO mapping, and operational queue bounds/time windows.

The ordinary backend restore could not read the local user NuGet configuration in this sandbox. The normal output paths also encountered restricted files and a running Game process holding assemblies. Verification therefore used existing cached restore metadata and isolated temporary build outputs; the running process was not stopped. `LL_TEST_API_ROOT` and copies of non-secret equipment/item test content allowed the existing tests to find their fixtures from that temporary output location.

The initial run skipped the PostgreSQL Alpha Signet migration test. Follow-up verification started a new PostgreSQL 17 cluster in a temporary directory on a separate loopback-only port with a random SCRAM password. Both Signet migration cases now pass, along with three new LiveOps database tests. Each test creates and removes its own randomly named database. The existing Game instance and its database were not used. PostgreSQL could not start under the sandbox process token, so the disposable server was started outside the sandbox. It was stopped after verification.

Deployed authentication and Chat integration, screen-reader behavior, production-volume performance, and actual browser 200% zoom were not exercised. The 640-pixel check verifies the layout width corresponding to 200% zoom on a 1280-pixel viewport, not browser zoom behavior itself. No timed administrator study has been conducted.

## Rollout implications

1. Review and apply both generated migrations through the normal database rollout process before enabling case/package writes. This work applied it only inside the automatically removed local test database.
2. Release the matching LiveOps API and frontend together. Older Signet clients without a preview token will be rejected by the updated endpoint. The enhanced shared Signet audit fields take effect in services built from this revision.
3. Retain existing role assignments, same-origin hosting, private access, and antiforgery configuration. No additional configuration is required for the new features.
4. In an isolated environment, smoke-test case creation/resume/conflict, item and equipment packages, Signet preview/grant, expired-token recovery, Chat unavailability, and operation receipt links with the actual identity provider and database before a production rollout.

Do not apply either migration's Down operation after use without a data-preservation plan: the first drops the three case/package tables, and the second drops private operator drafts. An application rollback can leave the additive tables in place.

## Deliberate boundaries

The review classified announcements, maintenance communication, scheduled content, and game-wide flags as later, separately reviewed workflows. They are not part of this implementation. There is no arbitrary configuration or database editor.

The current retained records do not establish a general, exact restoration command for every missing reward, purchase, entitlement, currency balance, or stalled action. These are not represented by misleading repair buttons. Existing evidence is exposed, cases record the decision, and supported compensation remains explicitly a replacement grant. Background-job details provide diagnosis and escalation context, not an unsupported retry action. Equipment migration remains an owner maintenance workflow outside everyday compensation.

Analytics use the existing daily report contracts. Missing reports are gaps; WAU/MAU are not summed; percentage comparisons require complete matching windows and a nonzero prior mean. The existing API returns at most 90 daily reports, so a 90-day view cannot supply a full preceding 90-day comparison. Itemization observations retain their original denominators and causal limitations.

## Changed file groups

- `LL/src/Presentation/liveops/src/app`: shell, navigation, shared preview/snapshot, player, audit, investigation, overview, and analytics components; new Cases feature, workspace state, operation journal, session recovery, action labels, API contracts, and recovery tests. `src/main.ts` registers session recovery.
- `LL/src/API/API.LiveOps`: cases/package controllers; Signet and moderation preview integration; operational detail contracts and reader.
- `LL/src/Core/Application`: case/package commands, queries, DTOs, mappings, and service interfaces. `LL/src/Core/Domain/Models/Administration`: case/package entities, repository contracts, and added audit/preview kinds.
- `LL/src/Infrastructure`: repository implementations, EF configurations, append-only checks, registration, migration/snapshot, case/package services, and the existing Signet receipt enrichment.
- `LL/tests/EssenceSystem.Tests`: new support-workflow, preview-recovery, and `LiveOpsPostgresWorkflowTests.cs` integration tests; registration and operational-status coverage updated.
- `docs`: the original usability review and this implementation/rollout record.

Unrelated balance-harness, tower-content, and equipment-migration work already present in the checkout was preserved.

## Reproducing the database checks

Use a disposable PostgreSQL server with database-creation permission. Set `LL_LIVEOPS_TEST_POSTGRES` and `LL_SIGNET_TEST_POSTGRES` to its connection string in the test process environment, and run the existing `build/run-tests.ps1` entry point with the LiveOps/Nobility/Signet filter shown above. The new tests reject non-loopback hosts and replace the supplied database name with a unique `ll_liveops_test_...` name. Signet tests use their own `ll_signet_migration_test_...` names. Both fixtures remove only databases they created. Without the environment variables, these integration checks are explicitly skipped. Never supply a shared or production server.

Local release preparation also ran `dotnet publish LL/src/API/API.LiveOps/API.LiveOps.csproj --configuration Release --no-restore --artifacts-path <temporary build artifacts> --output <temporary publish directory> --no-self-contained /p:UseAppHost=false`. The verified local artifact was written to `%TEMP%/ll-liveops-release-verification`. No image was pushed and no release workflow was dispatched.

## Running local administrator preview

The published application was started at **http://127.0.0.1:4411**, with the existing Development operator sign-in and a separate PostgreSQL cluster listening only on loopback port 55483. Search for **PreviewPlayer** or open the **Local preview walkthrough** support case. The saved **Preview recovery package** contains two synthetic Preview potions. Its successful grant is linked to the case and appears in the player's inventory evidence and activity log.

This preview contains synthetic data only. It uses a new database, `ll_liveops_local_preview`, initialized from the current EF model. Its migration history was baselined to that already-created schema; this is not an additional migration-upgrade test. The migration-upgrade tests described above exercise the actual migration separately. The existing Game process and databases were not changed.

Chat is deliberately disconnected, so its status is Unknown/Degraded. No Game delivery worker runs against this preview database, so queued player-update notifications remain pending even though compensation has committed. External identity-provider sign-in and deployed Chat integration still require environment-specific verification.

Preview settings exist only in its local launch process: loopback hosting, the disposable database, a separate data-protection key directory, and a disconnected Chat endpoint. Console logging remains enabled; Windows Event Log logging is disabled for this preview after the sandbox denied access to that sink. No repository configuration was changed.

The preview's temporary directory contains `Start-Preview.ps1`, `Stop-Preview.ps1`, logs, and its PostgreSQL data. The stop script checks the recorded API process identity before stopping it, then stops only this PostgreSQL cluster and preserves the data. To stop it from a normal PowerShell window:

```powershell
$previewRoot = (Get-Content (Join-Path $env:TEMP 'll-liveops-preview-path.txt') -Raw).Trim()
& (Join-Path $previewRoot 'Stop-Preview.ps1')
```

Use `Start-Preview.ps1` in the same directory to restart it while the temporary publish artifact and database remain available. The preview is not registered as a Windows service and is not an external deployment. Its random database password and local keys remain in the temporary directory and are not included in the repository.


## Daily-use follow-up

The follow-up adds a global search with typed Player, Case and Operation results. Case text search uses the existing authorized case endpoint; operation lookup requires a complete operation reference. Each source fails independently, stale responses are ignored after editing the query, and private query text stays out of the page URL. The Overview work queue shows up to five recent open cases, five recent waiting cases, and five investigations each for Unreviewed and Investigating. Counts and results come from their matching filters; unavailable queues never show a fabricated zero.

Case drafts are private working text, separate from the append-only case record. `OperatorDrafts` uses the authenticated actor from the server, a bounded field allowlist, a 24,000-character document cap, and a revision token. Actor-scoped PostgreSQL locks serialize writes; exact replay after a lost save response is harmless, and stale different text receives HTTP 409. A conflicting tab retains its local text for copying and requires an explicit load of the saved draft. Discarding a draft clears its content while retaining the revision to prevent stale text from resurrecting it. Drafts currently have no automatic expiry; clearing a draft does not remove its prior content from database backups.

Autosaves are debounced and serialized. Loading/reloading a draft blocks editing until its stored state is known. Navigation retains pending saves, sign-out first flushes drafts, and an explicit unsaved-sign-out option remains available if storage is unavailable. A browser close/reload warns while drafts remain unsaved. Confidential draft text is not stored in browser local/session storage. The operation journal stores only identifiers and status metadata locally; it remains a browser recovery aid, not a server-wide attempt ledger.

Case and operation summaries can be copied without internal notes or technical JSON. Case summaries include the recorded resolution and operation references in the loaded history; a coverage note identifies older history. Missing-reward and activity shortcuts now include a short investigation checklist and links to the next evidence section and case record. The browser walkthrough also found and corrected the frontend's attempt to parse the sign-out redirect's HTML as JSON.

Verification for this follow-up: 59 frontend tests, 118 filtered backend tests (including real PostgreSQL migration/concurrency checks), production frontend build, Release API publish, no pending EF model changes, and generated SQL review. The second migration creates one table without altering existing tables. It was applied only to the isolated synthetic preview database. Backend verification still uses the cached restore metadata and temporary outputs described above; external authentication, real Chat, Game-client delivery, production-volume performance and administrator acceptance remain unverified.

Additional changed files include the draft domain/service/repository/CQRS/controller/mapping/configuration and migration, `operator-draft.service.ts`, `shared/draft-status.component.ts`, `shared/global-search.component.ts`, `features/dashboard/work-queue.component.ts`, case/shell/audit/player integration, `administrator-daily-work.spec.ts`, and `LiveOpsOperatorDraftTests.cs`. Existing deployment configuration and unrelated balance work were preserved.

The published follow-up was also exercised in the browser against the isolated preview database: case note and decision drafts survived a full reload and a completed sign-out/sign-in cycle; case global search and exact operation lookup returned typed results; unavailable Chat coverage was stated alongside the Game receipt; the Overview listed the existing open case; and the case-summary copy action succeeded. At a 390-pixel viewport the new search and work queue reflowed without horizontal document overflow. The viewport override was reset and the updated Overview was left open. This remains layout verification, not a timed administrator study or a screen-reader assessment.
