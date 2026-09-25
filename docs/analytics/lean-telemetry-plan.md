# Lean telemetry plan: periodic reports and daily users

Status: implementation guide, 2026-09-25. This replaces the first milestone of [the comprehensive design](telemetry-analytics-design.md).

## Scope and decisions

The first analytics release should answer four questions:

1. **Major content outcomes:** Which dungeons, Tower floors, Colosseum rating bands, raids or region bosses have unusually low participation or completion?
2. **Essence and Combat Style adoption:** What do recently active characters own or select, and which options are largely ignored?
3. **Economy health:** Are important resource balances accumulating or running dry for characters at comparable progression?
4. **Daily users:** How many distinct accounts open the game each day, return within a week, or return after account creation?

There is no initial per encounter combat fact, full build snapshot, ability stream, equipment lifecycle, feature view stream, automated balance alert, or separate analytics store. The existing database has durable outcomes for most major content. Dungeon runs are deleted after claim or dismissal, so a compact `DungeonAttemptHistory` row preserves their start and terminal status. A scheduled job reads those records in bounded windows and stores small reporting totals. The only new general player activity record is one row per active account per UTC day.

## How periodic checks work

Use the existing Quartz worker infrastructure in `LL/src/Worker/Worker.LL/BackgroundJobs`. Run **one daily reporting job at about 02:00 UTC**, after game day rollover. It rolls up yesterday's activity and outcomes, and captures that morning's Essence, Combat Style, and economy state. The job queries persisted gameplay state; it does not visit each online player or request client data. Dashboards read the stored daily results, not the live gameplay tables on every page load.

| Check | Frequency | Read from existing data | Store | Decision |
| --- | --- | --- | --- | --- |
| Daily users | Daily, for previous UTC day | New `AccountActivityDay` marker rows; `AppUser.CreatedUtc` for cohorts | DAU, rolling WAU/MAU, new/returning counts, mature D1/D7 | Is the game attracting and bringing back people? |
| Major outcomes | Daily; recompute the last 7 completed UTC days to catch late finalization | `DungeonAttemptHistories`, `TowerAttempts`, `ColosseumMatches`, raid/region boss run and participant rows | Starts, completions, failures, distinct participants by content/date | Which content needs investigation? |
| Essence adoption | Daily snapshot, among accounts active in the previous 7/30 days | `PlayerEssences`, usable `EssenceLoadouts`/slots and `Characters` | Owner and saved loadout counts by definition and level band, with active character cohort size | Which Essences are seldom owned or saved? |
| Combat Style adoption | Daily snapshot, same cohort | `CharacterCombatStyleSelections`, `Characters` | Selected style by level band | Are players selecting each style? |
| Economy health | Daily snapshot, same cohort | `Characters` Cinders and Soulstones balances | Per resource P50/P90 balances and zero balance share | Are balances accumulating or running dry? |

The queries use indexed date ranges and active account IDs. Start with content-level totals; avoid a row for every combination of area, level, version, build and day. `DailyTelemetryReports` stores one JSON aggregate per UTC date, keyed by date. It contains population, outcomes, adoption and economy sections. Job reruns replace the keyed aggregate, so retries and the seven-day overlap do not double count. Current-state adoption/balance snapshots are captured once for each report and retained during outcome rechecks. If a missed day is first reported later, the snapshot is taken at that later time and its `snapshot_at_utc` is shown; past states cannot be reconstructed. A manual rebuild can be added if older outcome corrections require it.

Run the daily snapshot against the **recently active cohort**, not every historical account. Define the 7/30-day cohort from `AccountActivityDay`, join to `Characters.UserId`, then query current character/Essence/Style state. This is a state snapshot at the daily check time, not historical combat usage or an exact end-of-yesterday state. If a character owns an Essence but does not have it in a *current usable saved loadout*, the report can show that gap. It cannot claim the character never used the Essence in the past. Similarly, a selected Combat Style may not resolve in every activity; call the metric **selected style**, not combat usage. Reuse the same cohort for the economy query so the day's figures are comparable.

For major content, count starts on their start date and completions/failures on their finalization date. Show both attempts and distinct characters. Dungeon attempt history preserves the start and terminal status after the live run is removed; Tower attempts have `StartedAt`, `CompletedAt`, `Status` and `Succeeded`; Colosseum matches have `PlayedAt` and outcome. PvP reporting counts voluntary attackers separately from defenders. Raid/region boss metrics join persisted participants. Use UTC half-open windows `[start, end)` and content IDs as dimensions. A seven-day recheck handles delayed results; older corrections need a manual rebuild. Suppress or label slices with small character counts.

For economy, begin with **Cinders and Soulstones**; add Glory or other resources when a balancing decision requires them. Show P50/P90 and zero-balance share by broad character level bands among recently active characters. A rise in balances can reflect cohort mix, so compare the same bands and report their sizes. `EconomyLedgerEntry` records some acquisitions and transfers but does not prove every award/spend path is covered. Source/sink totals are deferred until each currency passes a coverage and reconciliation audit. Do not publish a confident inflation or total minting metric from incomplete flows.

## The daily activity marker

Add one small table, provisionally `AccountActivityDay(account_id, activity_date_utc, first_seen_at_utc)`, with a unique key on `(account_id, activity_date_utc)`. When an authenticated player first opens or refocuses the **visible game** on a UTC day, Angular sends one small activity request. The API takes the account ID from authentication and the UTC date from server time; it ignores duplicate requests with an insert-on-conflict rule. The client may remember that it already sent the request that day to reduce traffic, but the database unique key is the correctness boundary. A background tab, SignalR reconnect, scheduled combat settlement and automatic polling do not create an activity day. A tab left open across midnight records a new day only when the player returns to it.

This costs at most one new database row per active account per day and roughly one request per active browser day. No page views, session IDs, IPs, device data, heartbeat loop, or duration estimate are required. If multiple tabs race, the insert is idempotent. The proposed endpoint is a command, not a side effect on the existing `GET GameBootstrap` route; bootstrap also reloads after reconnect, so counting every bootstrap call would inflate DAU.

Definitions:

* **DAU:** distinct accounts with an activity marker on a UTC date. The label means **opened the visible game**, not played combat.
* **WAU/MAU:** distinct accounts with at least one marker in a rolling 7/30 UTC day window; never sum DAU values.
* **New active:** accounts created within the chosen UTC day that also have an activity marker.
* **Returning active:** DAU minus new active for that day. Report guest and registered accounts separately if guest conversion materially changes the trend.
* **D1/D7 return:** of accounts created on a UTC date and old enough to have a full observation window, the percentage with a marker on the exact next/seventh UTC date. Also show cohort size and numerator. This is a calendar-day measure, not 24/168 elapsed hours.

Start with DAU, WAU, MAU and D1/D7; D30 can appear once enough mature cohorts exist. Do not infer play time or engagement from these markers. Meaningful action counts can be derived later from persisted game outcomes if a specific product question needs them.

## Load, correctness and privacy controls

* Keep one scheduled job instance, with a unique report-date key and idempotent replacement. The Quartz host uses a clustered persistent job store and the job forbids concurrent execution.
* Query only necessary columns and bounded date ranges. The migration adds date indexes for the report predicates; verify query plans with `EXPLAIN ANALYZE` on representative data. Run outside the combat peak. A reporting failure is logged; the next scheduled run rechecks the previous seven completed days without blocking gameplay.
* Validate daily totals against source rows for a few sampled dates. Test late finalization, duplicate activity requests, UTC day boundaries, empty days, and job reruns. Keep a freshness timestamp on the report; do not interpret a stale dashboard as a game change.
* Keep raw activity days for 13 months, then delete them. Treat account IDs as personal data, restrict report access to the LiveOps read permission, cascade raw rows on account erasure, and label small outcome samples. Reconsider retention and lawful basis before production.
* No automatic game balancing. Review daily trends against a rolling baseline; small beta samples do not justify balance alerts. A simple failed-job or stale-report operational alert is sufficient initially.

## LiveOps dashboard integration

The repository already has a private Angular app at `LL/src/Presentation/liveops` and `API.LiveOps`, which uses `Persistence.LL` and protects read endpoints with `AdministrationPermissions.Read`. Add an **Analytics** route and navigation item in this LiveOps app. A read-only endpoint under `API.LiveOps`, such as `/api/liveops/analytics/overview`, should return the stored daily reports for a bounded date range. Add detail views for major content, Essence/Style adoption, and economy only when the overview cannot answer the tuning question. Return aggregate counts, cohort definitions and `generated_at_utc`; do not expose account-level activity rows or raw character histories to the browser.

The existing LiveOps Status component reloads operational health every 30 seconds. Keep analytics on its own route and fetch on page entry, filter change or explicit refresh. A small link or yesterday-summary tile can appear on Status, but it must read a cached aggregate and must not add analytics queries to the 30-second health poll. When a report is late, show its **last generated** time and a stale-data state rather than a zero. Reuse the existing operator session and read authorization for an owner-only first release; add a narrower analytics permission if staff roles expand. `LL/src/Presentation/dashboard` is a different admin CRUD application and is not the target.

## Implemented first release

1. `AccountActivityDay` and an authenticated, idempotent activity command in `API.LL`/Core Application/Persistence are called from the visible Angular game. `AddLeanTelemetry` creates the database schema.
2. A daily Quartz rollup stores DAU and durable major outcomes with a unique report date and seven-day outcome recheck. Dungeon terminal outcomes survive live-run deletion.
3. Daily active-cohort queries report Essence ownership/saved loadouts and selected Combat Style as aggregates.
4. Daily Cinders/Soulstones balance distributions cover active characters by broad level band. Ledger source/sink totals await a coverage audit.
5. `LL/src/Presentation/liveops` has an Analytics route and navigation link. `API.LiveOps` provides read-only aggregate reports; the page fetches on entry, filter change or manual refresh, separate from the Status poll.

The first release needs `AccountActivityDay`, `DungeonAttemptHistory`, and one daily aggregate table, one daily job, and one page in the existing LiveOps UI. It does not need a generic event bus, a combat telemetry warehouse, or a separate analytics frontend.

## Rollout notes

Apply the `AddLeanTelemetry` migration before starting API or Worker versions that use the new tables. It creates the three telemetry tables and adds date indexes to existing outcome tables; those existing-table indexes are generated concurrently to reduce write blocking. The Quartz job is enabled by default and runs at 02:00 UTC; `Analytics:DailyJobEnabled=false` disables its trigger if the rollout must be staged. The migration is generated in this repository and is not applied automatically. Dungeon history begins when this code is deployed; completed runs deleted before then cannot be reconstructed from `DungeonCompletionRecords`. Reports for older days may therefore have incomplete dungeon outcomes. The current release does not include a manual historical rebuild command, ledger flow totals, mastery distribution, or a database performance baseline; measure the daily job on representative data before adding detail or increasing its cadence.
