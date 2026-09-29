# LiveOps current-state administrator review

**Reviewed:** 28 September 2026, after the administrator workflow, Analytics subpage and tooltip changes.

**Baseline:** working tree at `0039e8dc1`, including the current uncommitted LiveOps changes.

**Target:** `LL/src/Presentation/liveops`, `LL/src/API/API.LiveOps`, and the supporting administration/reporting code.

**Purpose:** assess what is good, where administration still becomes difficult, and what to improve next. This is an analysis document; the findings have not been implemented by this review.

## Overall assessment

LiveOps now has a useful foundation for individual player support and controlled administrative actions. It is substantially easier to navigate than the earlier version: player information has task-oriented sections, cases retain decisions, compensation supports reusable packages, actions have server-verified reviews, and Analytics has separate pages with explanations.

The remaining weakness is completing an administrative job from beginning to end. The administrator still has to remember which case is active, carry evidence between screens, decide which waiting work is most urgent, interpret records with different meanings, and leave the tool when operational diagnosis reaches its limit. Some of that is missing workflow; several issues are reproducible defects.

**Recommendation:** keep the current architecture and visual identity. First repair the remaining navigation, request-ordering and draft-loss problems. Then make cases and follow-up work the organizing structure for daily administration, and turn the strongest analytics into a small set of clearly scoped comparisons. A broad redesign or more dashboard panels would give less value than finishing these flows.

No critical authorization bypass, destructive action defect or production incident was established by this review. That is not a security certification: production authentication, live Chat, real workload and end-to-end delivery were outside the exercised environment.

## How this review was performed

Evidence labels used below:

- **Browser:** observed in the compiled local LiveOps application against its isolated synthetic preview database.
- **Controlled check:** executed the current TypeScript class with controlled inputs/responses, independently of the browser and real services.
- **Source:** confirmed from the implementation, but not reproduced against a full live workflow in this review.
- **Recommendation:** an assessment of administrative usefulness, rather than a demonstrated software defect.

The browser walkthrough covered Overview, an existing case, the case-to-player path, missing-reward evidence, compensation preparation, an operation receipt, audit quick views, Analytics date selection, and a narrow-screen case layout. Source inspection also covered investigation detail, recovery, drafts, permissions, previews, report calculations and operational diagnostics. The local preview deliberately has unavailable Chat and sparse demonstration data. Those conditions are not evidence that production Chat is down or that real player activity is low.

No moderation, compensation, case note/status, migration or external deployment was submitted. A temporary, unsubmitted compensation reason was entered to test navigation loss; it was gone on return. Existing case drafts were preserved. Navigation can save the application's ordinary workspace preferences.

This assessment follows the [earlier baseline review](liveops-administrator-usability-review-2026-09-28.md) and [implementation record](liveops-administrator-implementation-2026-09-28.md). Earlier findings are not automatically still open. For example, visible player-search feedback, player/detail response guards, unknown Chat state, action-preview expiry/focus handling, fresh receipt-link scope, cases, and Analytics navigation have already improved.

## What is good and should be preserved

| Strength | Why it helps the administrator | Evidence and limits |
|---|---|---|
| Clear environment and operator identity | Makes the target environment and responsible operator visible before acting. | Shell and action-review templates; local development banner observed. Production sign-in was not exercised. |
| Server-verified action reviews | The review states the exact target, effect, reason, environment and expiry. Higher-risk operations can require a typed target name. | [Preview UI][preview-ui] and [preview service][preview-service]; existing tests cover focus, expiry and busy dismissal. |
| Idempotency and explicit uncertain outcomes | A lost response is not presented as proof of failure, and ordinary retries reuse an operation reference. This protects against hurried duplicate actions. | [Player component][player-ts], [operation journal][journal] and recovery tests. Cross-device recovery remains limited. |
| Cases and private case drafts | Issue descriptions, notes, decisions and verified operation links can form a durable support record. Draft revision conflicts retain local text. | [Case workflow][cases-ts], [case template][cases-html], [draft service][drafts]; restored existing drafts observed. |
| Honest evidence boundaries | Chat can be Unknown; snapshots identify source/time; investigations distinguish signals from proof; missing reports are gaps rather than zero. | Browser and corresponding templates. Some failure-state inconsistencies remain in F06. |
| Player sections and issue shortcuts | Summary, Inventory, Activity, Restrictions, Chat and Compensation are easier to understand than one undifferentiated page. Missing-item checklists help a less frequent operator. | [Player template][players-html]; case-to-player walkthrough. |
| Versioned compensation packages | Repeated item selections can be reused, and the server expands the saved version into exact item effects before a grant. | [Package preview][preview-service] and player package editor. Library management can be clearer. |
| Explainable investigation evidence | Priority, moderator disposition, transfer facts, timing context and conversation coverage are kept distinct. Incomplete evidence is identified. | [Investigation template][risk-detail-html]; a populated production investigation was not available for browser review. |
| Analytics organization and explanations | Five direct subpages, meaningful adoption sorting, denominators and column help make a large reporting surface approachable. | Current Analytics browser walkthrough and [report state][analytics-state]. |
| Readable receipts with technical detail available | Common actions have readable labels/effects, while identifiers and JSON remain available when needed. Audit links and filtered exports are useful operational primitives. | Existing case receipt observed in [audit][audit-html]. Export was not executed. |
| Useful state preservation and defensive checks | Player search, case/audit filters and case drafts have persistence; several pages reject stale responses; the preview has keyboard focus management. | Source and 72 passing frontend tests. Coverage is uneven rather than absent. |

These strengths are worth retaining during further work. In particular, do not remove review steps, conceal uncertainty, treat repeated observations as independent players, or infer missing historical evidence merely to simplify the interface.

## Assessment by administrative job

| Job | Current assessment | Remaining friction |
|---|---|---|
| Start the day and choose work | Useful entry point | Most-recently-updated cases, no due date, separate refresh scopes, readiness below a large queue area. |
| Find and identify a player | Generally usable | Global and local finders duplicate controls; bounded search has no broader results workflow; account/character scope still requires attention. |
| Investigate a missing item | Good evidence starting point | Evidence is distributed across holdings, saved run, acquisitions, transfers, compensation and delivery; findings are still assembled manually. |
| Complete and document compensation | Strong execution safeguards | Draft loss, package-editor complexity, manual linking back to a case and no operation-specific delivery story in the receipt. |
| Resume and finish a case | Usable basic case record | No scheduled follow-up, limited queue controls, active-case context is weak during investigation. |
| Work through transfer investigations | Rich evidence, uneven workflow reliability | Remaining response race, next-record loop, unsaved notes and costly secondary data loading. |
| Investigate an operational exception | Helpful diagnosis | First 50 records, no pagination and no supported repair workflow exposed here; some answers require worker diagnostics. |
| Use analytics to make a decision | Much easier to navigate | Time-control ambiguity, mixed content-result meanings, limited trends/comparisons outside adoption, and dense Itemization detail. |

## Priority definitions

**P1:** fix in the next implementation batch because the issue can lose work, show the wrong context, mislead a decision, or leave common work untracked.

**P2:** improve after those fixes; meaningful reductions in repeated effort, interpretation cost or recovery friction.

**P3:** expansion to consider when a demonstrated administrative need justifies it.

The effort labels in the delivery plan are relative scope, not calendar estimates. Production rollout and new data collection can cost more than the UI work.

## Findings and improvements

### F01 — Investigation results can belong to an older filter selection

**P1 · Controlled check + Source**

The investigation queue's `load()` assigns every response to `data`. It has no request-generation check or cancellation, although quick-view buttons can start another request while one is pending. A controlled check started High priority, then Include low, returned the newer response first, and returned High last. The selected severity remained Low while the displayed rows came from the older High request.

This can make an administrator believe a filtered queue is complete or review the wrong subset. It is a presentation consistency problem; it is not proof that a moderation command targets a different account.

**Improve:** bind results and errors to the latest filter/page request, retain the previous successful view with an explicit loading/stale label, and prevent older requests from resetting loading state.

**Done when:** reverse-order success and failure responses cannot change the newest queue's rows, counts, filter summary or pagination. Test quick views as well as the Apply button.

Evidence: [queue `load()` and `quickFilter()`][risk-list-ts].

### F02 — “Next unreviewed account” can loop instead of advancing

**P1 · Controlled check + Source**

`nextAccount()` returns the first unreviewed record other than the current one. With A, B and C all unreviewed, the actual service returned A → B and B → A. C is not reached by continuing that sequence. It also works only from the cached page and can be unavailable after a direct detail link.

**Improve:** advance from the current position in the active queue, distinguish Next from Skip and Save & next, and fetch the next page when necessary. Viewing a record should not silently mark it reviewed. Explain when a direct link has no queue context.

**Done when:** three unchanged unreviewed records advance A → B → C, then show a clear end state; reviewed/filtered-out rows and page boundaries behave predictably.

Evidence: [investigation list state][risk-state] and [detail navigation][risk-detail-ts].

### F03 — Draft protection is inconsistent across workflows

**P1 · Browser + Source**

Cases have saved private drafts. Compensation reasons, moderation reasons/notes, package edits and investigation notes do not have equivalent protection. In the browser, entering a temporary compensation reason, navigating to Overview and returning with Back restored the player and section but left the reason empty. No grant was submitted. Investigation detail resets its note and status reason on load; the route guard protects submissions, not ordinary unsaved text.

**Improve:** extend the existing private-draft mechanism to deliberately scoped preparation work: operator + environment + subject + action type. Restore editable preparation only, require a fresh server preview, and keep drafts isolated when switching players. Package-definition editing should have its own draft scope. If some forms intentionally remain temporary, visibly warn before discarding meaningful edits.

**Done when:** navigating away, refreshing and signing in again recover the correct draft; switching players never carries a reason or confirmation into a different target; stale preview tokens are never restored as ready to submit.

Evidence: [player `resetDrafts()`][player-ts], [investigation `load()`][risk-detail-ts], [route guard][routes] and [existing draft service][drafts].

### F04 — An active case does not consistently follow the investigation

**P1 · Browser + Source**

Opening a player from a case preserves a `case` reference. However, the missing-reward and activity checklists still send “Record finding in a case” to the new-case route, even with an existing case active. The active case itself is displayed as a UUID. Evidence rows have no general Add to current case action. Completed-operation linking is supported, but still needs a return to the case and a separate submission.

**Improve:** show a compact active-case bar with title, status and a Return to case action. Use Add finding to this case when one is active; offer Create case otherwise. Provide deliberate, prefilled evidence-link and completed-operation-link actions, with source and timestamp. Keep the operator in control of what is appended.

**Done when:** an administrator can open a missing-item case, inspect evidence, record a finding, execute any justified compensation, link the receipt and resolve the same case without copying an identifier or accidentally starting a duplicate.

Evidence: [issue checklist and case links][players-html], [case workflow][cases-html].

### F05 — Audit quick views retain incompatible filters

**P1 · Browser + Source**

From a completed package-grant receipt, clicking Audit exports retained that grant's exact operation ID while adding the export action filter. The resulting view showed zero actions. The URL and visible advanced filter confirmed that the old operation restriction remained. Other retained target, actor or date fields can similarly narrow a quick view.

**Improve:** define whether a quick view replaces the current scope or refines it. For the current global-looking buttons, clear incompatible filters. Show active filter chips even when advanced controls are collapsed and offer a one-click clear for each restriction.

**Done when:** Audit exports from a receipt opens exports rather than an impossible receipt/action combination; every retained restriction is visible in a compact scope summary.

Evidence: [audit `applyQuickFilter()`][audit-ts] and [quick-view UI][audit-html]. Fresh receipt links already clear unrelated retained filters; preserve that separate improvement.

### F06 — Failed requests can still look like zero or empty evidence

**P1 · Controlled check + Source**

When an initial investigation request fails, the queue's counters fall back to zero and its empty message says no retained transfers are available. The controlled check produced a visible error alongside zero Critical accounts and that no-evidence message. This is contradictory: the evidence is unavailable. Audit similarly clears entries before loading, then can show its ordinary no-matches state alongside a request error.

**Improve:** use distinct initial-loading, unavailable, genuine-empty, filtered-empty and stale-result states. Failed counts should be Unknown, with a focused Retry. A failed refresh should retain prior results only with their age and an explicit stale label.

**Done when:** an outage never produces an apparent clean investigation queue or a no-records conclusion. Test failures before the first successful load and after a successful load.

Evidence: [queue `count()` / `emptyMessage`][risk-list-ts], [audit load and empty state][audit-ts].

### F07 — Waiting cases have no actual follow-up schedule

**P1 · Source + Recommendation**

The home queue explains that Waiting cases need follow-up, but a case has no due date or next-action field. The list is sorted by most recent update; the home page shows the first five. An old unresolved case can receive less visibility precisely because nobody has touched it. There is no oldest-first, overdue, priority or category filter in the current case UI/API.

**Improve:** add optional Follow up on, next action and a small priority set. Offer Due today, Overdue, Oldest unresolved and Recently updated views, plus category filtering. Start with this solo-administrator model; team assignment and complex SLA machinery are not prerequisites. Make “waiting for player” versus “waiting for investigation/fix” distinguishable.

**Done when:** setting a follow-up makes the case reappear in the relevant due queue, old work cannot disappear behind recent edits, and resolved cases leave the actionable queue.

Evidence: [case model][case-model], [case ordering][case-repository], [home queue][work-queue] and [case filters][cases-html].

### F08 — The missing-reward workflow still requires manual evidence assembly

**P2 · Browser + Source + Recommendation**

The checklist is helpful, but checking holdings, saved rewards, claim time, acquisition history, previous compensation, transfers and delivery remains a sequence of separate readings. Equipment holdings are bounded, the saved dungeon view is retained state, and recent acquisitions/compensation/market trades are limited lists. A missing record cannot establish that an entitlement never existed.

**Improve:** add a focused issue workspace accepting the reported item, run/reference and approximate time. Present matching retained evidence in chronological order, with clear links back to the original sections and their coverage limits. Allow evidence references to be attached to the active case. Add pagination/search to retained sources where it is supported; do not fabricate older events.

**Done when:** the administrator can identify an observed award/claim/transfer/replacement sequence, or explicitly record that the available evidence cannot answer the question. Any future entitlement replay must be a separate, validated Game command.

Evidence: [support snapshot][support-html], [bounded source queries][support-service], [equipment evidence][equipment-html].

### F09 — Operational diagnosis reaches a dead end inside LiveOps

**P2 · Source + Recommendation**

Delivery, job and restriction drilldowns now explain impact and next steps. However, they return the first 50 records without pagination; deeper diagnosis points to the owning service's logs. The job view cannot detect a job that never started. There is no corresponding safe repair workflow exposed by these views.

**Improve in stages:** first add pagination, status/age filters, a compact diagnostic bundle and an incident/case link. Then add schedule-aware freshness checks for essential jobs. Only introduce repair/retry controls after the owning Game service provides a bounded, authorized and idempotent command with a meaningful preview and receipt.

**Done when:** a failed delivery can be traced to its subject and retained effect, all matching exceptions can be reached, and a missing scheduled execution is distinguishable from an empty failure list. A generic “retry everything” button is not an appropriate acceptance criterion.

Evidence: [operational detail service][status-service] and [Overview drilldowns][dashboard-html].

### F10 — Recovery is useful, but remains browser-dependent

**P2 · Source + Recommendation**

The operation journal keeps unresolved references in browser storage scoped to operator/environment and reconciles them against receipts. That is valuable recovery support, but another device, cleared storage or unavailable storage will not have the same local list. Audit shows completed operations; an absent receipt remains inconclusive. The receipt's player-delivery status also requires a separate snapshot rather than an operation-specific status chain.

**Improve:** consider a server-side operation-status view once the immediate workflow defects are fixed. Distinguish accepted, committed, delivery pending/failed and unknown only where the underlying service can establish those states. Link the status back to a case, and preserve the original operation ID for any supported retry.

**Done when:** reopening an uncertain operation from another browser gives an authoritative status or explicit unknown state without encouraging a new grant. Delivery claims must be backed by real correlation data; a generic pending count is insufficient.

Evidence: [operation journal][journal], [receipt UI][players-html] and [support delivery section][support-html].

### F11 — Report day, trend window and freshness do not form one clear contract

**P2 · Browser + Source**

Selecting the 20 September report changed the Activity cards and report heading, while the chart and comparison still covered the period ending 27 September, the latest available report. The chart caption states this, but the shared Report day control implies a broader effect. Analytics selections survive subpage navigation but reset after leaving/reloading. Itemization uses its own day and has no equivalent stale-report warning in the shell. Overview's status refresh and work-queue refresh also have separate scopes.

**Improve:** clearly separate selected-day metrics from trend-period controls, or make the chosen day anchor the whole Activity view. Persist nonsensitive report/date/filter choices in URLs or saved views. Give each data source a consistent last-successful-refresh indicator, stale state and refresh action. Label refresh scope explicitly.

**Done when:** an administrator can state which day and population every card/chart/table describes; reopening a saved report view reproduces its filters; stale Itemization and stale work queues are visible.

Evidence: [Analytics state][analytics-state], [Activity template][activity-html], [Analytics shell][analytics-html], [home queue][work-queue].

### F12 — Content outcome columns still mean different things across modes

**P2 · Source + Recommendation**

The new tooltips correctly explain that Completed includes all played Colosseum matches, including losses, while other modes use success-related conditions. Colosseum losses are also counted under Failed. Group content counts participant entries; starts and results are counted on their respective UTC dates. A familiar Started / Completed / Failed table can still invite an invalid completion-rate comparison before the administrator reads the help.

**Improve:** make the distinctions visible in the main layout: mode-specific labels or separate tables, a prominent counting-unit label, and separate starts versus results. If a true success/conversion metric is desired, define a matched-attempt population and attribution window in the backend first.

**Done when:** a Colosseum loss is never mistaken for a successful clear, a group run is not mistaken for one participant event, and no displayed rate divides unrelated start/result-day counts.

Evidence: [TelemetryRepository `OutcomesAsync`][telemetry] and [current column definitions][column-help].

### F13 — Analytics explains the data better than it helps answer decisions

**P2 · Source + Recommendation**

The main reporting opportunity is now decision support rather than additional column help.

| Page | Useful next improvement | Interpretation constraint |
|---|---|---|
| Activity & retention | Labeled date/count axes, visible selected-day marker, coverage summary and a retention cohort view. | Preserve missing-day gaps and small denominators; do not sum rolling active-account counts. |
| Content outcomes | Within-mode trends by definition and a clear view of changing outcome counts. | Define the unit and date attribution before calculating success rates. |
| Adoption | A compact comparison summary, minimum-sample controls prominent beside results, and export/share of the filtered view. | Percentage-point changes can reflect changing cohort membership; observed selection is not combat usage. |
| Economy | Zero-balance percentage, balance-distribution trends and comparable cohort summaries. Later, collected source/sink flows and prices if those questions matter. | Holdings are not currency creation or spending; inflation cannot be established from P50/P90 balances alone. |
| Itemization | A summary of populated contexts, sample sizes and notable distributions; separate deeper drilldowns for build evidence and choices; relevant sorting. | Repeated battles, distinct builds and decisions have different denominators; observed win differences are not causal effects. |

Start with comparable data already retained. Instrument new events only when they answer a named administrative question. Analytics exports should include report/snapshot time, definitions, filters, sample sizes and missing-data flags, rather than exporting numbers without context.

**Done when:** a weekly review can answer “what changed, among whom, how reliable is that comparison, and what should I inspect next?” without manually comparing many report days or making unsupported causal conclusions.

Evidence: [Analytics pages][analytics-pages], [report state][analytics-state] and [Itemization template][itemization-html].

### F14 — Compensation library management and one-player granting are mixed together

**P2 · Browser + Source + Recommendation**

One long Compensation section contains catalog search, equipment configuration, single grants, package construction, package editing/archiving and saved-package grants. The package grant uses reason/notes from fields above. Package lines display identifiers, and the bounded library has no search/pagination workflow. Every package preview is classified HighValue, even a small two-item demonstration package, while the audit shortcut says Large grants.

**Improve:** separate Use a saved package from Create/edit package, keep the target and reason beside the selected action, show resolved item names, and surface limits before submission. Add searchable package selection when the library grows. Label the existing risk category as “High-risk actions” or explain the policy; it is not a measured monetary-value estimate.

**Done when:** granting an existing package does not require navigating the editor, unsaved edits are obvious, the exact saved version is clear, and risk labels match the rules that produce them.

Evidence: [Compensation UI][players-html], [package preview risk][preview-service], [audit quick views][audit-html].

### F15 — Copyable support summaries need an explicit audience

**P2 · Source + Recommendation**

Copying summaries instead of raw technical JSON is a good addition. However, a case summary includes Resolution, which comes from a field labeled “Decision or resolution”; an operation summary includes the free-text Reason. Those fields can contain internal context even though explicitly named internal-note fields are excluded. The current message does not guarantee that the resulting text is suitable for a player.

**Improve:** distinguish an internal case summary from a player-facing response draft. Show a preview before copying externally intended text, state which fields are included, and let the administrator edit the response. Continue to leave actual sending to the existing communication workflow unless a supported messaging feature is deliberately added.

**Done when:** the administrator knows the intended audience and can review the exact output before copying it; excluding the Internal notes field is not described as automatic sanitization of all free text.

Evidence: [case `copySummary()`][cases-ts] and [operation summary builder][presentation]. No unintended disclosure was observed.

### F16 — Layout and navigation still consume too much attention

**P2 · Browser + Source + Recommendation**

The visual identity is coherent and narrow screens reflow without document overflow in the case check. However, the mobile shell and global search occupy roughly the first 330 pixels before case content. On desktop, a large work queue precedes the readiness banner; the player page can show both a global finder and a 340-pixel finder beside an already selected target. Dense technical evidence remains below several layers of headers, notes and controls.

**Improve:** put a compact actionable-health strip above the work queue; use a compact mobile navigation/search treatment; collapse the local player finder by default when arriving from a case, with an obvious switch-player control. Give the selected player and active case a concise persistent context area. Prefer progressive disclosure for diagnostics, not for the next action.

Keyboard support is also uneven. The Analytics subnav announces its active page, but the top-level nav primarily uses styling. Player/investigation section buttons do not expose a selected state, and investigation rows are clickable/focusable table rows rather than ordinary named navigation links. Add a skip link and consistent route-heading focus behavior. A formal accessibility audit is still needed before claiming compliance.

**Done when:** keyboard users can identify the current area and section, reach the primary task without traversing repeated navigation, and open an investigation using a proper link. Narrow-screen task controls should be reachable without excessive preliminary scrolling.

Evidence: [shell][shell-html], [styles][shell-css], [player sections][players-html], [investigation rows][risk-list-html] and [Analytics shell][analytics-html].

### F17 — Search, saved views and result identity need a consistent pattern

**P2 · Source + Recommendation**

Global search is useful and honestly bounded, but it has no continuation to all matches or exact-match-first presentation; results are grouped alphabetically by type/name. Queue/filter persistence differs across Cases, Audit, Investigations and Analytics. Case page position resets. In the audit list, the expanded subject is often a UUID rather than a recognizable player name, adding another navigation step.

**Improve:** use consistent active-filter summaries, reset controls, exact-reference handling and sensible result ranking. Provide a View all results path for truncated search. Save nonsensitive views without leaking player emails or private search text into shareable URLs. Show readable target identity in audit summaries while retaining copyable IDs.

**Done when:** returning from a record resumes the same list position/filter, a direct reference is easy to distinguish from fuzzy matches, and a saved analytics view can be reopened without reconstructing it manually.

Evidence: [global search][search], [workspace persistence][workspace], [case state][cases-ts], [audit display][audit-html] and [Analytics state][analytics-state].

### F18 — Loading strategy and component size will make growth harder

**P2 · Source + Recommendation**

The player support endpoint starts multiple bounded section reads in parallel but returns after all finish. A slow section can delay the whole response. Investigation detail immediately starts timing and conversation requests even when their sections are not opened. The player component coordinates search, evidence, several action forms, package management, previews and receipts in one large class; shared styling also spans many features.

**Improve:** measure request timings first. Then consider lazy secondary investigation loads, smaller independent snapshot requests or staged rendering where latency justifies them. Extract focused action-form/package components while preserving the existing shared preview, journal and source-boundary logic. Avoid a broad rewrite merely to reduce line counts.

**Done when:** slow optional Chat/timing evidence does not delay the useful first decision context, and adding one action does not require changing unrelated workflow state. Validate with representative data volumes; the small local fixture is not a performance benchmark.

Evidence: [snapshot aggregation][support-service], [investigation load][risk-detail-ts], [player component][player-ts].

## Recommended delivery order

| Batch | Contents | Relative scope | Why this order |
|---|---|---|---|
| 1. Repair verified workflow defects | F01 response guards; F02 real queue advancement; F04 current-case links; F05 quick-view scope; F06 unavailable states. Clarify the Activity date behavior in F11. | Mostly focused frontend work; a complete cross-page queue iterator may need API support. | Removes reproducible misleading or obstructive behavior before adding capabilities. |
| 2. Protect and resume daily work | F03 scoped private drafts; F07 follow-up date/next action/priority and due views; clearer active-case identity. | Frontend plus backend validation, persistence and migration for new case fields. | Prevents lost preparation and forgotten cases, the biggest repeated administrative costs. |
| 3. Finish the support loop | F08 evidence references/search; F14 separate package use/edit; F15 explicit summary audiences; targeted F16/F17 navigation improvements. | Mixed UI/API work; richer retained evidence may need new read models. | Reduces copying, context switching and duplicate support records. |
| 4. Make recurring reviews useful | F11 consistent dates/freshness/saved views; F12 visible content units; F13 selected comparable trends and contextual export. | Small presentation changes through larger report/instrumentation work. | Helps decisions without presenting unsupported rates or causal claims. |
| 5. Extend operational authority carefully | F09 exception pagination/diagnostic bundles, then specific repair commands; F10 server-side operation status; measured F18 loading changes. | Larger service-contract and operational work. | The UI must be backed by authoritative state and supported Game commands. |

**Best first five concrete changes:** investigation request guards, a real next-record iterator, active-case-aware finding links, draft protection beyond Cases, and deterministic audit quick views. Add due dates and oldest/overdue views as the next substantial feature.

## Expansion worth considering later

These are product opportunities, not claims that all should be implemented now:

- Incident records that group affected players, cases and operations when one bug creates many support requests. Start with grouping and progress tracking before bulk actions.
- A consistent server-generated diagnostic bundle for a player/operation/job, with bounded data and explicit coverage.
- Links to separate content-authoring/admin tools where that boundary is intentional. Equipment-migration endpoints already exist without a LiveOps route; do not present them as absent backend capability or expose apply/rollback casually.
- Notifications for due work or meaningful operational changes once ownership, thresholds and noise controls are defined.
- Additional staff roles or approval workflows only when more operators are actually introduced. The current owner-focused context does not justify building a large help-desk permission system first.

## Acceptance scenarios for the next iteration

Use real administrator walkthroughs as well as automated checks. The time goals below are proposed usability targets, not measured current performance.

| Scenario | Acceptance target |
|---|---|
| Begin a shift | In about a minute, identify actionable outages, due/overdue cases and the next investigation without reading every panel. |
| Change queue filters rapidly | Only the latest request can populate rows/counts; failed or stale data is clearly identified. |
| Review three investigations | Next visits each record in order, supports page boundaries, and never discards an unsaved note silently. |
| Resolve a missing-item case | Keep one case context through evidence, decision, optional preview/grant, receipt linking and resolution. No UUID copying required. |
| Leave prepared work | Return after navigation/reload and recover the right subject's editable draft; create a fresh preview before submission. |
| Handle uncertainty | Resolve the original operation where possible; an unavailable receipt never suggests blindly creating another grant. |
| Open an audit quick view from a receipt | Scope changes predictably and all active restrictions are visible. |
| Compare analytics | State report day, trend range, counting unit, denominator and coverage correctly; reopen the same view later. |
| Work at narrow width or by keyboard | Identify the active section and target, navigate tables, open/dismiss help and review/cancel an action without losing context. |

Measure task completion, abandoned drafts, repeated navigation, unresolved operations, overdue case age and use of external logs/database tools. Do not collect private note or conversation contents merely to measure usability. Interview the administrator after a few real support sessions to validate which proposed improvements actually save time.

## Verification, limitations and changes made by this review

| Verification | Result |
|---|---|
| `npm run build` in `LL/src/Presentation/liveops`, npm cache under `%TEMP%` | Passed. |
| `npm test` in the same project | Passed: **72 tests**. Passing tests do not negate the targeted defects found outside the suite. |
| Current queue component with reversed controlled responses | Reproduced F01: Low remained selected while an older High result replaced the rows. |
| Current list-state service with A/B/C unreviewed | Reproduced F02: A → B, B → A, C → A. |
| Initial investigation request failure | Reproduced F06's zero counts and no-evidence message alongside an error. |
| Local browser walkthrough | Reproduced F03 draft loss, F04 new-case finding link, F05 retained operation filter and F11 split date scope. Inspected the existing strengths described above. |
| Case page at 390-pixel viewport setting | Document width stayed within the viewport; substantial header/search height was visible. Override reset. |
| Document source links and whitespace | Checked before delivery. |

The controlled checks transpiled and ran the repository's current classes with stubbed framework dependencies and synthetic responses; they were not rewritten approximations of the algorithms. They do not prove full browser/backend behavior beyond the stated cases.

Backend tests were not run because this request changed no backend implementation. Live Google/MFA authentication, connected Chat moderation/history, actual Game-client update delivery, production-scale queues, real report quality, a full keyboard/assistive-technology audit and timed administrator acceptance remain unverified. External sources were not needed: the repository and local application were the primary evidence.

**Files changed:** this review document only. Build outputs are ignored generated artifacts. Review screenshots were saved separately in the task's visualization directory. No application code, dependency, configuration or database schema was changed. No migration or deployment is required for the document. Future case scheduling, authoritative operation tracking and new analytics collection will need explicit backend/schema design; frontend clarification and navigation fixes generally will not.

## Source index

| Source | Relevant evidence |
|---|---|
| [Application shell][shell-html], [routes][routes], [styles][shell-css] | Environment, global navigation, guards and layout |
| [Global search][search], [workspace state][workspace] | Search bounds and persistence |
| [Overview template][dashboard-html], [work queue][work-queue] | Readiness hierarchy, queue order and refresh scope |
| [Operational status service][status-service] | Bounded exception views, filters and diagnostic limits |
| [Player template][players-html], [component][player-ts] | Active-case links, preparation forms, resets and receipts |
| [Support snapshot][support-html], [equipment template][equipment-html], [snapshot service][support-service] | Evidence coverage, source limits and aggregate loading |
| [Case template][cases-html], [component][cases-ts], [model][case-model], [repository][case-repository] | Workflow, copied summaries, persistence fields and ordering |
| [Investigation queue][risk-list-ts], [queue template][risk-list-html], [list state][risk-state] | Response race, unknown/empty states and next-record behavior |
| [Investigation detail][risk-detail-ts], [detail template][risk-detail-html] | Evidence, note state and secondary loading |
| [Audit component][audit-ts], [template][audit-html], [presentation helpers][presentation] | Quick-view scoping, identities and summary content |
| [Preview UI][preview-ui], [preview service][preview-service], [operation journal][journal], [draft service][drafts] | Safeguards, risk categorization and recovery boundaries |
| [Analytics pages][analytics-pages], [shell][analytics-html], [state][analytics-state], [Activity][activity-html], [Itemization][itemization-html], [help definitions][column-help], [Telemetry repository][telemetry] | Report scope, calculations and remaining interpretation work |

[shell-html]: ../LL/src/Presentation/liveops/src/app/app.component.html
[shell-css]: ../LL/src/Presentation/liveops/src/app/app.component.css
[routes]: ../LL/src/Presentation/liveops/src/app/app.routes.ts
[search]: ../LL/src/Presentation/liveops/src/app/shared/global-search.component.ts
[workspace]: ../LL/src/Presentation/liveops/src/app/workspace-state.service.ts
[dashboard-html]: ../LL/src/Presentation/liveops/src/app/features/dashboard/dashboard.component.html
[work-queue]: ../LL/src/Presentation/liveops/src/app/features/dashboard/work-queue.component.ts
[status-service]: ../LL/src/API/API.LiveOps/Health/LiveOpsOperationalStatusService.cs
[players-html]: ../LL/src/Presentation/liveops/src/app/features/players/player-workspace.component.html
[player-ts]: ../LL/src/Presentation/liveops/src/app/features/players/player-workspace.component.ts
[support-html]: ../LL/src/Presentation/liveops/src/app/shared/support-snapshot/support-snapshot.component.html
[equipment-html]: ../LL/src/Presentation/liveops/src/app/shared/support-snapshot/equipment-support.component.html
[support-service]: ../LL/src/API/API.LiveOps/Support/LiveOpsPlayerSupportSnapshotService.cs
[cases-html]: ../LL/src/Presentation/liveops/src/app/features/cases/cases.component.html
[cases-ts]: ../LL/src/Presentation/liveops/src/app/features/cases/cases.component.ts
[case-model]: ../LL/src/Core/Domain/Models/Administration/SupportCase.cs
[case-repository]: ../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Administration/SupportCaseRepository.cs
[risk-list-ts]: ../LL/src/Presentation/liveops/src/app/features/account-risk/account-risk.component.ts
[risk-list-html]: ../LL/src/Presentation/liveops/src/app/features/account-risk/account-risk.component.html
[risk-state]: ../LL/src/Presentation/liveops/src/app/features/account-risk/account-risk-list-state.service.ts
[risk-detail-ts]: ../LL/src/Presentation/liveops/src/app/features/account-risk/account-risk-detail.component.ts
[risk-detail-html]: ../LL/src/Presentation/liveops/src/app/features/account-risk/account-risk-detail.component.html
[audit-ts]: ../LL/src/Presentation/liveops/src/app/features/audit/audit.component.ts
[audit-html]: ../LL/src/Presentation/liveops/src/app/features/audit/audit.component.html
[presentation]: ../LL/src/Presentation/liveops/src/app/shared/admin-presentation.ts
[preview-ui]: ../LL/src/Presentation/liveops/src/app/shared/action-preview/action-preview.component.ts
[preview-service]: ../LL/src/API/API.LiveOps/Previews/LiveOpsActionPreviewService.cs
[journal]: ../LL/src/Presentation/liveops/src/app/operation-journal.service.ts
[drafts]: ../LL/src/Presentation/liveops/src/app/operator-draft.service.ts
[analytics-pages]: ../LL/src/Presentation/liveops/src/app/features/analytics/analytics-pages.ts
[analytics-html]: ../LL/src/Presentation/liveops/src/app/features/analytics/analytics.component.html
[analytics-state]: ../LL/src/Presentation/liveops/src/app/features/analytics/analytics-state.service.ts
[activity-html]: ../LL/src/Presentation/liveops/src/app/features/analytics/activity-page.component.html
[itemization-html]: ../LL/src/Presentation/liveops/src/app/features/analytics/itemization-page.component.html
[column-help]: ../LL/src/Presentation/liveops/src/app/features/analytics/analytics-column-help.ts
[telemetry]: ../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Analytics/TelemetryRepository.cs
