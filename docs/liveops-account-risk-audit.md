# LiveOps Player Flagging and Cheat-Detection Audit

Date: 2026-08-18  
Scope: current working tree in `Legends-Legacy`; analysis only, with no detector or product implementation changes.

### Audit method and limits

This audit traced static code, migrations, registrations, tests, API contracts and frontend behavior across `LL/src` and `LL/tests`. No production database or production alert labels were available. Consequently, actual alert prevalence/precision, retained row counts and PostgreSQL `EXPLAIN (ANALYZE, BUFFERS)` plans could not be measured; database performance findings are based on query shape and declared indexes. Those measurements are required before selecting final thresholds or indexes.

## A. Executive summary

### Verdict

The current account-risk feature is a useful **direct-transfer review prototype**, but it is not a trustworthy player cheat-detection system and its numeric score should not be interpreted as confidence or probability.

It can help an operator find a few conspicuous direct-cinder patterns, inspect direct transfers, add notes, and maintain a review status. Its strongest qualities are the append-only raw economy ledger, event-time account context, explicit wording that signals are investigative leads rather than proof, backend authorization, and audited moderator mutations.

It is not yet safe to use as the primary answer to “which accounts deserve investigation?” for five reasons:

1. **Ordinary gifts are promoted too aggressively.** With production defaults, one one-way transfer of any positive amount makes both established accounts Moderate. One direct item transfer does the same regardless of item value. One transfer from a young account can make both sender and recipient High because several correlated interpretations of the same event are added together (`AccountRisk.cs:294-351`, `AccountRisk.cs:354-415`, `appsettings.json:40-53`; the intended behavior is asserted in `AccountRiskEvaluatorTests.cs:48-98`).
2. **Coverage is incomplete and silently biased.** Every interval selects the same most-recent 2,000 transfer participants, then globally truncates first-hop and second-hop evidence. There is no cursor, fair partition, “eligible but unevaluated” count, or truncation indicator (`AccountRiskRepository.cs:30-75`, `AccountRiskRepository.cs:78-149`, `AccountRiskOptions.cs:11-15`). At higher activity, old-but-in-window accounts can starve indefinitely and percentages can be computed from partial denominators.
3. **The feature detects only direct transfer topology.** It ignores marketplace trades, guild-vault paths, item acquisition provenance, currency creation, wealth history, progression, general combat, activity cadence, device/login relationships, and most indirect paths even though some of those records exist (`AccountRiskRepository.cs:35-42`, `AccountRiskRepository.cs:93-101`). “Account risk” therefore overstates the scope.
4. **Evidence is not a durable decision snapshot.** Current signals persist aggregate numbers, but not triggering event IDs, exact event times, counterparties per signal, the analysis window, completeness, thresholds, baseline, or independent-observation identity. Sparse history omits relationship snapshots and the API drops even the historical `SignalsJson` (`AccountRisk.cs:62-71`, `AccountRiskRepository.cs:239-251`, `AccountRiskDtoMapper.cs:37`). A later operator cannot reliably reconstruct exactly why a score existed.
5. **The score is an ordinal sum of heuristics, not calibrated risk.** Category caps reduce some double counting, but the same transfer can still contribute across `Resource flow`, `Account context`, and `Transfer chains`. There is no magnitude floor, population baseline, cohort normalization, recurrence model, time decay, or confidence dimension (`AccountRisk.cs:241-249`, `AccountRisk.cs:501-537`).

Direct answers:

| Question                         | Assessment                                                                                                                                                              |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Is it trustworthy?               | No, not for prioritizing the whole player population or interpreting severity literally.                                                                                |
| Is it useful?                    | Partially, as a transfer explorer and source of uncalibrated investigative leads.                                                                                       |
| Is it too noisy?                 | Very likely. The configured one-event thresholds guarantee obvious legitimate false positives. Production prevalence cannot be measured from the repository alone.      |
| Is it too easy to evade?         | Yes. Marketplace/guild-vault routing, extra hops, reciprocal cover transfers, timing outside 48 hours, and activity outside the capped batch all bypass or weaken it.   |
| Does it provide enough evidence? | Enough to start a recent direct-transfer review, not enough to defend or reproduce a historical decision.                                                               |
| Is it suitable for expansion?    | The append-only ledger and separated investigation state are sound foundations. The evaluator, scheduler, snapshot schema, and generic score need substantial redesign. |

No machine learning is recommended now. The repository lacks complete labels, complete economic source/sink telemetry, and calibrated baselines; explainable rules plus robust aggregates will provide more value first.

## B. Current architecture

### Actual data flow

```text
Player cinder wire / direct item transfer
        |
        +--> PlayerTransferHistory (operator-friendly direct-transfer record)
        |
        +--> EconomyLedger (append-only event, event-time account age/level)
                    |
                    v
        AccountRiskEvaluationWorker (startup, then every 30 minutes)
                    |
        recent candidate selection + 90-day, capped, two-hop dataset
                    |
        AccountRiskEvaluator (six heuristics, summed score, category caps)
                    |
        AccountRiskSnapshots (replaceable current state)
        AccountRiskHistory (sparse append-only score/signal changes)
                    |
        MediatR queries --> AccountRiskController
                    |
        Angular account-risk queue and detail page
                    |
        AccountRiskInvestigation + AccountRiskNote
                    |
        append-only AdminAction audit entries
```

### Event creation and persistence

- Cinder wires atomically update both balances and add both a `PlayerTransferRecord` and `DirectCurrencyTransfer` ledger row. The ledger row includes sender/recipient account and character IDs, account-created dates, character levels, value, source, reference ID, and occurrence time (`CurrencyTransferRepository.cs:40-86`).
- Direct inventory transfers similarly add the detailed history row and a `DirectItemTransfer` ledger row with item identity, instance lineage, quantity, event-time account context, and a shared reference ID (`InventoryRepository.cs:623-668`).
- `EconomyLedger` is protected against EF updates/deletes (`LLDbContext.cs:114-120`) and indexed by time, participant, event type, asset, reference, and item-instance lineage (`EconomyLedgerEntryConfiguration.cs:28-38`).
- The ledger also records marketplace exchanges, marketplace fees, item acquisitions, and guild-vault movement, but the risk evaluator filters all of those out. Marketplace orders preserve both item and reverse currency legs with the same `ReferenceId` (`MarketPlaceRepository.cs:232-301`).

### Evaluation lifecycle

- `AccountRiskEvaluationWorker` runs immediately on LiveOps startup and then on a `PeriodicTimer`; the default is every 30 minutes (`AccountRiskEvaluationWorker.cs:15-44`, `AccountRiskOptions.cs:11`).
- Evaluation is asynchronous relative to gameplay. No transfer request waits on fraud analysis and no detector automatically sanctions a player.
- A PostgreSQL transaction advisory lock prevents concurrent evaluators. A freshness check skips a run when **any** snapshot of the current evaluation version is recent (`AccountRiskRepository.cs:13-28`, `LiveOpsAccountRiskService.cs:17-31`).
- Candidate accounts are the most recent senders/recipients in the 90-day window, capped at 2,000. A small maintenance slice adds previously non-Low snapshots whose last trigger is older than the lookback (`AccountRiskRepository.cs:30-75`).
- The analysis query loads up to 100,000 newest direct-transfer rows touching candidates, then up to another 100,000 newest outgoing rows from their first-hop neighbors (`AccountRiskRepository.cs:91-149`). The option name “MaximumTransfersPerEvaluation” does not represent the final combined maximum.
- Each candidate is evaluated independently over the shared in-memory adjacency lists (`LiveOpsAccountRiskService.cs:34-49`, `AccountRisk.cs:158-183`).

### Persistence and lifecycle semantics

- One replaceable `AccountRiskSnapshot` exists per evaluated account. Multiple rules are serialized together in `SignalsJson`; relationships are serialized separately (`AccountRisk.cs:37-60`, `AccountRiskRepository.cs:202-237`).
- Score is 0-100; severity bands are configurable at 25/50/75. Rule thresholds and caps are configuration-bound, but individual rule contribution ranges are hardcoded in the evaluator (`AccountRiskOptions.cs:16-29`, `AccountRisk.cs:284`, `AccountRisk.cs:317`, `AccountRisk.cs:383`, `AccountRisk.cs:412`, `AccountRisk.cs:448`). There is no option validation.
- A flag can effectively expire only when the account is reevaluated after evidence leaves the lookback. The snapshot becomes Low, while `FirstFlaggedAt` remains as first-ever flag time. Expiry is not a first-class finding state and can be delayed by candidate maintenance limits.
- `AccountRiskHistory` is written only for a new snapshot, severity/version change, or score change of at least five. Evidence can change completely without a history row if score and severity remain similar (`AccountRiskRepository.cs:239-251`).
- Human status is separate and persistent: Unreviewed, Investigating, Watchlisted, Cleared, ConfirmedAbuse, or Actioned (`AccountRisk.cs:23-31`). Automated reevaluation neither changes nor reopens that status.
- Notes are append-only. Status changes and notes add `AdminAction` records with actor, time, account/character, reason, and relevant state (`LiveOpsAccountRiskService.cs:59-156`; append-only enforcement at `LLDbContext.cs:90-97`, `LLDbContext.cs:123-130`). There is no assignee, case entity, per-signal disposition, or transition/concurrency policy.

### API, security, and UI

- Reads require `liveops.read`; status and note mutations require `liveops.accounts.moderate` (`AccountRiskController.cs:20-84`). A global authenticated fallback policy also exists (`Program.cs:103-109`).
- Browser sessions use OIDC authorization-code flow with PKCE, secure/HTTP-only production cookies, 30-minute sliding and eight-hour absolute expiry. Bearer tokens validate issuer, audience, and lifetime (`LiveOpsAuthentication.cs:29-157`). Cookie-backed unsafe methods are globally antiforgery checked (`CookieAntiforgeryFilter.cs:20-41`).
- The backend defines granular permission policies, but interactive OIDC ticket acceptance currently succeeds only for the configured owner/bootstrap owner; a non-owner is redirected as denied even if the identity provider supplied other staff permissions (`LiveOpsAuthentication.cs:123-139`, `LiveOpsOwnerIdentity.cs:8-28`). That is secure for a solo-owner deployment but is not yet a multi-moderator browser-role model. Bearer clients can still use granular permission claims.
- Production startup rejects development-operator mode, invalid OIDC/public-host settings, absent owner identity, absent database connection, and absent trusted proxy configuration (`Program.cs:164-258`). The anonymous surface is static SPA files plus liveness/readiness health endpoints.
- The queue exposes severity, score, primary reason, direct flow, connections, age/session context, review status, filtering and sorting (`account-risk.component.html:1-75`). The detail page exposes current aggregate signals, direct relationships, current related-account risk, up to 500 recent direct-transfer records (UI default 200), sparse score history, notes, and status editing (`account-risk-detail.component.html:18-95`).
- Additional current balances, inventory, acquisition records, guild, marketplace trades, current action, token issuance, restrictions, synchronization, and paginated transfer history already exist on the separate player-support page (`PlayerSupportSnapshotModels.cs:10-137`). They are not composed into the risk investigation view.

## C. Existing detection rules

| Rule                  | Detects                                                                                            |                             Signal quality | False-positive risk | Evasion risk | Recommendation                                                                                           |
| --------------------- | -------------------------------------------------------------------------------------------------- | -----------------------------------------: | ------------------: | -----------: | -------------------------------------------------------------------------------------------------------- |
| IncomingConcentration | One sender supplies >=70% of observed incoming cinders, provided at least two senders exist        |                                       Weak |                High |         High | Replace with magnitude/cohort-aware top-counterparty concentration over complete windows                 |
| OneSidedRelationship  | Strongest direct cinder relationship is >=85% imbalanced                                           |                                       Weak |           Very high |         High | Keep concept; replace implementation with pair metrics, value floors, duration, direction and context    |
| OneSidedItemTransfer  | Strongest item relationship is >=85% imbalanced by event count                                     |                               Mostly noise |           Very high |    Very high | Remove from scoring until item value/rarity/provenance context exists                                    |
| FeederNetwork         | Young sender(s) direct >=80% of observable outgoing cinders to subject                             |                                       Weak |           Very high |         High | Replace; require multiple independent feeders or exceptional magnitude plus acquisition/context evidence |
| YoungAccountOutflow   | Account was <=14 days old at first observed outgoing transfer and >=80% of direct flow is outgoing |                                       Weak |           Very high |         High | Keep only as contextual multiplier, never a standalone 25-point finding                                  |
| CircularTransfer      | Exact ordered three-account cinder cycle, values within 2x, within 48h                             | Moderate as a lead, weak as abuse evidence |       Moderate-high |    Very high | Keep as one graph motif within broader temporal path analysis; require repetition/source context         |

### IncomingConcentration

**Purpose.** Find recipient accounts dominated by one funding source.

**Inputs and calculation.** Groups observed incoming direct-cinder rows by sender. It refuses to run with fewer than two senders, divides the largest sender’s value by all observed incoming value, and triggers at 70% (`AccountRisk.cs:269-291`). Contribution scales from 8 at 70% to 20 at 100%.

**Intended signal.** A main account may receive most of its direct funding from a feeder.

**False positives.** A friend or guildmate gives a meaningful gift and a second player sends one cinder; family members support each other; prize distributions; returning-player assistance. There is no amount, sender wealth, relationship age, guild, market, or progression context.

**False negatives and manipulation.** A single feeder is excluded by the two-sender guard. Multiple feeders that distribute shares below 70%, intermediate routing, marketplace trades, guild-vault transfers, or enough reciprocal cinders bypass it. Global query truncation can either hide senders or inflate concentration.

**Confidence.** Weak. Concentration is useful as a derived metric, but not as standalone suspicious evidence.

**Recommendation.** Compute top-1/top-3 counterparty share over complete 1/7/30/90-day and lifetime pair aggregates; require a minimum absolute or sender-wealth-relative value; compare to account/progression cohorts; combine with sender acquisition-to-outflow behavior and relationship duration.

### OneSidedRelationship

**Purpose.** Detect a direct pair in which value predominantly travels one way.

**Inputs and calculation.** For every direct cinder counterparty, computes `abs(sent-received)/(sent+received)`, selects the most imbalanced pair, and triggers at 85%. The minimum count defaults to one. Contribution scales from 8 to 25, so one completely one-way wire gives 25 and Moderate severity to **both** participants (`AccountRisk.cs:294-320`, `AccountRiskOptions.cs:19-22`).

**Intended signal.** Feeder and main relationships are often persistently asymmetric.

**False positives.** Any ordinary gift, repayment outside the 90-day window, prize, guild help, long-standing friend support, or account-to-account division of roles. A one-cinder gift and a million-cinder funnel have the same maximum contribution.

**False negatives and manipulation.** Cover payments reduce the ratio; abusive value can move through items/market/guild vault; alternating counterparties defeats pair strength; slowly moving value is indistinguishable from quickly moving it; activity before the window is ignored.

**Confidence.** Weak as implemented; moderate when paired with magnitude, persistence, acquisition, and social context.

**Recommendation.** Retain asymmetry as a metric, not a conclusion. Require material value, multiple observations or exceptional wealth share, record direction, and evaluate net flow relative to both sender-earned value and recipient wealth. Treat receiver and sender differently.

### OneSidedItemTransfer

**Purpose.** Detect one-way direct item giving without inventing a cinder conversion.

**Inputs and calculation.** Counts item-transfer events per pair, not quantities or item value. It triggers at 85% imbalance with one event and contributes up to 25 (`AccountRisk.cs:323-351`). Asset-type count is explanatory only.

**Intended signal.** Alt accounts may funnel equipment or materials.

**False positives.** A single potion, crafting material, starter item, or birthday gift makes both accounts Moderate. Conversely, one legendary and one trivial item in reverse appear perfectly reciprocal.

**False negatives and manipulation.** Any reciprocal low-value item event neutralizes a high-value transfer by count; stacks have no stronger weight; marketplace and guild-vault item movement are excluded; rarity, equipment stats, acquisition source, and item lineage are ignored.

**Confidence.** Mostly noise for prioritization.

**Recommendation.** Remove from score until evidence supports value bands. Immediately useful non-monetary alternatives are rarity/tier, stack quantity, provenance/source, item-instance lineage, account-relative inventory value, and “newly acquired then transferred” timing. Preserve “unknown value” rather than falsely pricing an item.

### FeederNetwork

**Purpose.** Detect a hub receiving most observable outgoing cinders from young accounts.

**Inputs and calculation.** For each incoming sender, divides value sent to the subject by all cinder outflow for that sender in the loaded dataset. It checks whether the sender was <=14 days old at the first included transfer and share >=80%. Default minimum counterparties is one. Low level (<=20) is counted only in the explanation; it is not an eligibility condition. Contribution starts at 25 and reaches 32 (`AccountRisk.cs:354-390`).

**Intended signal.** Multiple newly created feeder accounts converging on a main account is a strong alt-abuse pattern.

**False positives.** The implementation calls one young friend making one gift a “network.” It has no minimum value or earned-wealth denominator and no need for the account to be low-level. A legitimate new player can send one cinder and make a recipient High after correlated one-sided scoring.

**False negatives and manipulation.** Wait 15 days; use older accounts; distribute outflow; use indirect accounts; route via marketplace/guild vault; include enough cover transfers; exploit dataset truncation; send items. “All outgoing” means only observed direct cinder outflow, not all wealth acquired or spent.

**Confidence.** Weak as implemented. A repeated multi-feeder motif can become strong with complete denominators and contextual evidence.

**Recommendation.** Rename/rebuild as a cluster signal. Normally require at least two or three independently feeder-like accounts; allow a one-feeder finding only for exceptional, context-normalized magnitude. Require sender acquisition/outflow ratios, target concentration, creation/time coordination, and snapshot event IDs.

### YoungAccountOutflow

**Purpose.** Identify new accounts whose direct-transfer behavior is mainly outbound.

**Inputs and calculation.** Uses age at the first included outgoing transfer. If <=14 days, it divides all outgoing direct cinders by incoming+outgoing direct cinders and triggers at 80%, contributing a fixed 25. Current/event-time level is displayed but never gates or adjusts the rule (`AccountRisk.cs:393-415`).

**Intended signal.** A newly created feeder may exist mainly to pass resources onward.

**False positives.** Any new account with no direct incoming history that sends any positive cinder amount has 100% outgoing share. It says nothing about the account’s gameplay earnings, total wealth, or materiality.

**False negatives and manipulation.** Wait, receive cover wires, use another transfer channel, or keep the first transfer outside the lookback. A feeder that earns and spends through game systems is invisible because those flows are absent from the denominator.

**Confidence.** Weak.

**Recommendation.** Make youth a context feature. A useful finding would be “within 72 hours of creation, transferred X% of all server-recorded acquired tradable value to one account,” with minimum value and cohort percentile. Add 1h/24h/7d age windows rather than one 14-day cliff.

### CircularTransfer

**Purpose.** Find possible wash/laundering cycles.

**Inputs and calculation.** Searches exactly `subject -> B -> C -> subject`, in chronological order, cinders only. All three values must have `min/max >= 0.5` and occur within 48 hours. Repeated events on the same `B:C` route collapse to one cycle. Contribution is 10-20 and its `Transfer chains` category is uncapped (`AccountRisk.cs:418-451`).

**Intended signal.** Value returning through intermediaries can obscure origin or simulate trade.

**False positives.** Three friends reimbursing one another, group prize distribution, or ordinary circular debts. A single cycle has no proof of wash behavior.

**False negatives and manipulation.** Two-node wash, four-plus hops, delay beyond 48 hours, one leg below half the largest, marketplace/item/guild paths, reverse ordering, and repeated cycles on the same path all evade or understate it.

**Confidence.** Moderate as an investigative lead when repeated and linked to origin/destination anomalies; weak alone.

**Recommendation.** Use bounded temporal path analysis over aggregated edges, covering 2-5 hops and multiple channels. Require repeated/rapid cycles, suspicious source acquisition, or a known exploit correlation. Persist the actual path and event IDs.

## D. Critical problems

No repository evidence shows an unauthenticated normal player can access the LiveOps risk endpoints. The P0 findings below concern decision safety and coverage, not an identified privilege bypass.

### P0 — Fundamentally unsafe/broken

#### P0.1 — Severity is materially overstated by zero-materiality, one-event rules

- **Evidence:** `MinimumTransferCount=1`, `MinimumCounterpartyCount=1`; one-sided signals reach 25; feeder starts at 25; young outflow adds 25 (`AccountRiskOptions.cs:19-29`, `AccountRisk.cs:294-415`). Tests explicitly require a single established wire to make both sides Moderate and a single young-account transfer to make both sides High (`AccountRiskEvaluatorTests.cs:48-98`).
- **Impact:** The queue cannot distinguish harmless gifting from sustained feeder behavior. Operators will either waste time or learn to ignore severity labels.
- **Example:** A five-day-old player sends one cinder to a friend. Sender: OneSidedRelationship + YoungAccountOutflow = High. Recipient: OneSidedRelationship + FeederNetwork = High.
- **Solution:** Before relying on the queue, add minimum materiality and observation requirements, treat account youth as context rather than an independent 25 points, require multiple feeders for a network, and demote single gifts to non-scoring context unless statistically exceptional.

#### P0.2 — Batch selection and global evidence caps make population coverage unknowable

- **Evidence:** newest 2,000 participants are selected each run with no cursor/checkpoint; first and second-hop queries each independently take the newest configured maximum (`AccountRiskRepository.cs:30-75`, `AccountRiskRepository.cs:91-149`). The dashboard reports total retained evidence and snapshot count, not eligible/processed/skipped/truncated counts (`AccountRiskRepository.cs:310-337`, `account-risk.component.html:11-26`).
- **Impact:** At >2,000 active participants, less-recent accounts may never be evaluated. At >100,000 relevant transfers, ratios and cycles operate on incomplete data while the UI describes them as the configured lookback. Results depend on unrelated global activity and batch composition.
- **Example:** 2,001 accounts transact repeatedly. The least-recent account remains in the 90-day window but is never chosen while the top 2,000 continue to transact. A busy network’s old counterparty outflows fall beyond the global cap, inflating its target-share denominator.
- **Solution:** Introduce a durable evaluation cursor/queue and deterministic partitions; record coverage metadata per run/account; page through all relevant edges or use pre-aggregated windows; refuse percentage-based signals when inputs are incomplete.

### P1 — Major weaknesses

#### P1.1 — The product name and score imply coverage the detector does not have

- **Evidence:** candidate and analysis queries include only direct cinders and direct items (`AccountRiskRepository.cs:35-42`, `AccountRiskRepository.cs:93-101`). Other ledger event types exist (`EconomyLedgerEntry.cs:3-14`).
- **Impact:** Economy exploitation, collusion, botting, progression anomalies, most combat abuse, guild-vault laundering, marketplace favorable trades, and indirect item/currency flows are missed completely.
- **Solution:** Until layered detectors exist, label this feature “Direct transfer signals.” Add behavior-category scores/cases only as each category obtains adequate telemetry.

#### P1.2 — Historical evidence cannot reproduce the decision

- **Evidence:** signal evidence is a numeric dictionary only (`AccountRisk.cs:91-97`); history stores signals but no relationships or run completeness (`AccountRisk.cs:62-71`); it is written sparsely (`AccountRiskRepository.cs:239-251`); the DTO discards historical signals and version (`AccountRiskDtoMapper.cs:37`).
- **Impact:** A moderator cannot answer what exact events, counterparties, thresholds, baseline, or dataset produced an August 18 flag. Raw ledger replay may differ because code/config/caps/candidate composition changed.
- **Solution:** Persist immutable signal occurrences with rule/version, window, event/reference IDs, counterparties, measurements, expected range, thresholds, completeness, correlation key, and explanation inputs. Preserve case/status events separately.

#### P1.3 — Correlated interpretations are still counted as independent evidence

- **Evidence:** one event can trigger one-sided flow plus feeder or youth; a three-edge cycle can also trigger pair/concentration findings. Caps apply only to `Resource flow` and `Account context`; `Transfer chains` is uncapped, and youth remains separate despite using the same transfers (`AccountRisk.cs:241-249`, `AccountRisk.cs:501-505`).
- **Impact:** Rule count and summed score exaggerate independent support. “High” often means two descriptions of one gift, not two observations.
- **Solution:** Assign each signal an `ObservationGroupKey`/event set and correlation family. Aggregate the strongest contribution per observation/family, then add diminishing weight only for distinct time-separated occurrences or independent channels.

#### P1.4 — No complete currency source/sink ledger means wealth claims cannot be made

- **Evidence:** combat rewards directly mutate character balances without an economy entry (`CharacterCurrencyRewardWriter.cs:19-50`); other reward and purchase paths also mutate cinders. The ledger models direct transfers, marketplace, item acquisitions, and guild vault, but has no general currency source/sink event types (`EconomyLedgerEntry.cs:3-14`).
- **Impact:** The system cannot calculate “percentage of lifetime wealth earned transferred,” abnormal wealth creation, income-vs-spend consistency, or disproportionate-to-wealth transfers. Current denominators are only direct-transfer volume.
- **Solution:** Add append-only currency mutation events for authoritative sources/sinks with cause, correlation/idempotency key, before/after balance or delta, account/character, and timestamp. Reconcile them against current balance.

#### P1.5 — Current review judgments do not control re-escalation or staleness

- **Evidence:** status is stored independently and evaluator upserts do not consult it (`AccountRiskRepository.cs:190-253`, `LiveOpsAccountRiskService.cs:59-107`). A Cleared case can remain High after new activity or stay Cleared forever; no “new evidence since cleared” concept exists.
- **Impact:** Operator decisions are not incorporated safely. Cleared cases may hide genuinely new evidence, while stale scores remain visible until reevaluation.
- **Solution:** Keep detection independent, but maintain case events: `LastReviewedEvidenceAt`, new-signal count, reopen policy, disposition reason, and notification when materially new independent evidence appears.

#### P1.6 — Temporal semantics are misleading

- **Evidence:** if any signal exists, `LastTriggeredAt` is the newest direct transfer, not the newest event supporting a signal (`AccountRisk.cs:250-266`). Rules mostly collapse 90 days into lifetime-like totals; only circular transfers have an explicit 48-hour window.
- **Impact:** A harmless new transfer can make an old suspicious relationship look recently triggered. Bursts, gradual funneling, periodic behavior, and changes in behavior are not distinguished.
- **Solution:** Every finding needs its own supporting event time/range. Compute multiple windows and compare recent to prior baselines.

### P2 — Important improvements

#### P2.1 — The investigation view silently truncates and mixes evidence sources

- Evaluation uses capped `EconomyLedger`; detail timeline uses newest `PlayerTransferHistory` rows with a separate 1-500 limit (`AccountRiskRepository.cs:340-390`). The UI neither states the cutoff nor paginates here (`account-risk-detail.component.html:63-80`).
- Add server-side time/channel/counterparty filters, cursor pagination, total count, oldest-loaded marker, and links from each signal directly to its supporting rows.

#### P2.2 — Item provenance and marketplace context exist but are isolated

- Direct transfer rows preserve item instance lineage; marketplace rows preserve price and paired legs; player support exposes recent acquisition and trade data (`EconomyLedgerEntry.cs:31-52`, `PlayerSupportSnapshotModels.cs:50-102`). Risk ignores them.
- Use this context first for investigation, then for carefully defined signals such as newly-acquired-high-tier-item transfer and systematically favorable related-account trades.

#### P2.3 — Sparse history is even less useful in the API than in storage

- Storage keeps `SignalsJson` and `EvaluationVersion`; `AccountRiskHistoryPointDto` exposes only ID, score, severity, time (`AccountRiskDtoMapper.cs:37`).
- Expose version, category changes, independent observation count, and evidence diff. Do not call a line chart “history” if the underlying decision cannot be inspected.

#### P2.4 — Investigation collaboration is minimal

- There is no assignee, due date, case reference field, status-transition policy, per-finding disposition, or optimistic concurrency token (`AccountRiskInvestigation` at `AccountRisk.cs:73-79`).
- Add only when more than one operator needs it: assignee, row version, status-event log, disposition and reason. Two simultaneous updates currently become audited last-write-wins changes.

#### P2.5 — Risk actions are hard to find in the audit UI

- Backend action types include `AccountRiskStatusChanged` and `AccountRiskNoteAdded` (`AdminAction.cs:3-10`), but the audit action dropdown omits them (`audit.component.html:17-19`).
- Add explicit filters/labels and link from a case to its account-scoped global audit history.

#### P2.6 — Configuration can be invalid without startup validation

- All thresholds bind directly and `ToPolicy` performs no range/order checks (`AccountRiskOptions.cs:5-45`).
- Validate score ordering, ratios in [0,1], positive windows/caps, and a new configuration schema/version at startup.

#### P2.7 — Browser authentication is owner-only despite granular backend policies

- `OnTicketReceived` accepts only `TryGrantOwnerPermission`; non-owner OIDC users are denied (`LiveOpsAuthentication.cs:123-139`).
- This is appropriate if the product is intentionally single-operator today. Before adding moderators, authorize already-permissioned staff without granting SuperAdmin, test role mapping end-to-end, and keep the configured owner bootstrap path separate.

### P3 — Polish/future improvements

- Distinguish “risk priority,” “signal severity,” and “confidence”; `/100 risk score` is too probability-like (`account-risk-detail.component.html:13-15`).
- Filter by any active signal, not only `PrimarySignalType` (`AccountRiskRepository.cs:268`).
- Add queue fields for independent observations, first/last evidence, new evidence since review, assignee, and data completeness.
- Add relationship visualization only for nontrivial clusters. A table is better for one or two counterparties.
- Consider retaining pseudonymous account labels in wide queue exports and revealing sensitive identity details only on investigation pages according to operational need.

## E. Detection blind spots

### Alt-account abuse and cross-account intelligence

- One-to-many/many-to-one patterns beyond direct cinders are incomplete.
- Indirect funnels longer than the exact three-node return cycle are not detected.
- Multiple feeder coordination is not required; creation-time clustering and shared activity schedules are absent.
- Item, marketplace, and guild-vault paths are not joined into the account graph.
- Related-account risk is merely read from current snapshots for display; it does not propagate evidence or form a cluster case (`AccountRiskRepository.cs:352-362`).

### Economy exploitation

- No complete currency source/sink ledger, wealth timeline, balance reconciliation, impossible negative/overflow transition finding, or source-rate anomaly.
- No price/cohort analysis of marketplace trades, wash trades, favorable trades, repeated matched pairs, or laundering.
- No item duplication detector using source/destination instance lineage, despite relevant IDs existing.
- No suspicious “acquire then immediately transfer/sell” detector.

### Collusion

- No guild/party/friend context, favorable-exchange analysis, repeated mutual benefit analysis, PvP outcome analysis, or coordinated cluster detection.

### Botting/automation

- Refresh tokens record issuance but not IP, user agent, device, or login events (`RefreshToken.cs:2-11`).
- `CharacterAction` is one mutable current-action row, not a historical cadence log (`CharacterAction.cs:7-31`). General action timing, command cadence, active hours, and input regularity are not retained in a detector-ready form.

### Progression anomalies

- Current level, XP, professions, quest/achievement ledgers, dungeon/tower data and equipment state exist, but no detector checks reachable states, XP/resource reconciliation, rate by cohort, or incompatible combinations.

### Combat exploitation

- The server performs authoritative combat calculations, reducing some client-cheat surface, and tournament/tower snapshots/replays exist. There is no general finding pipeline for impossible damage, cooldown/action frequency, result/state inconsistency, or suspicious repeated outcomes.

### Exploit abuse

- No generic correlation between economic deltas and failure/retry/outbox/idempotency events, known exploit windows, restarts, or duplicate reference IDs. `RiskScore`, `RiskDecision`, and `RuleHits` fields on ledger rows are unused anywhere outside schema/model declarations (`EconomyLedgerEntry.cs:49-51`).

## F. False-positive risks

The highest-risk legitimate scenarios are:

- a single gift between established friends: both Moderate;
- a single gift from a young account: both can become High;
- one cheap item given to another player: both Moderate;
- a rich player regularly supporting a guildmate or returning player;
- prize/event distributions and community giveaways;
- a repayment whose reciprocal leg is outside the 90-day window;
- two accounts specializing in different resources, where value reciprocity occurs through the marketplace or guild systems rather than direct cinders;
- circular reimbursements among a party within 48 hours;
- event-driven economic shifts that make global static thresholds obsolete;
- ordinary accounts distorted by a globally truncated dataset.

The tests protect some reasonable behavior (balanced established trading, high balanced volume, reciprocal item swaps), but they also codify the no-materiality false positives rather than guard against them (`AccountRiskEvaluatorTests.cs:10-26`, `AccountRiskEvaluatorTests.cs:64-118`, `AccountRiskEvaluatorTests.cs:155-170`).

## G. Available telemetry

### Available now and detector-ready

- Append-only direct cinder transfers with account/character IDs, event-time age/level, value, reference and time.
- Direct item transfers with item base, quantity, source/destination instance lineage and time.
- Marketplace orders and paired item/currency ledger legs with unit/total price, participants and shared reference.
- Guild-vault donation/borrow/return/withdraw ledger events.
- Many item-acquisition rows with source, quantity, item ID/instance and time.
- Current account creation, character level/XP and balances; current inventory/equipment/profession state.
- Current restrictions and append-only administrative actions.
- Detailed direct transfer history and item provenance.

### Available but difficult or costly to query safely

- Multi-hop temporal transfer graphs across a large raw ledger.
- Lifetime/per-window account-pair and account-level flow metrics; these require repeated large scans today.
- Population percentiles/cohorts; no materialized cohort distributions exist.
- Item valuation. Marketplace prices can provide observations, but sparse items, equipment modifiers, and price manipulation require robust value bands and an “unknown” state.
- Cross-channel graphs combining market, guild vault, and direct movement; semantics differ and references must be normalized before edges can be compared.

### Partially available

- Item acquisition provenance is broad but must be coverage-audited across every grant path before claiming completeness.
- Login activity is token issuance only; no true login/session-event history, IP, device, or user agent. The support service explicitly tells operators this (`LiveOpsPlayerSupportSnapshotService.cs:113-135`).
- Activity is current state and last mutation, not historical action cadence.
- Progression history exists for selected quest/achievement/event/tower/dungeon systems, not a unified XP/stat/equipment transition ledger.
- Combat replays/snapshots exist for selected tournament/tower contexts, not all play.

## H. Missing telemetry

Highest-value additions, in order:

1. **Complete currency mutation ledger.** Every authoritative currency source, sink, transfer, fee, refund and admin grant; include delta, balance before/after, cause/source, correlation/idempotency reference, account/character, and time. This unlocks reconciliation and wealth-relative evidence.
2. **Detection-ready action summary events, not raw click logs.** Record start/stop/resolution batches and outcome counts for combat/gathering/crafting with server timestamps and authoritative limits. Retain daily/hourly aggregates longer than raw events. This supports human-possible cadence and progression-rate checks without excessive storage.
3. **Progression transition events.** XP/level/profession/region unlock deltas with source and correlation. Deterministic impossible-state checks can then be strong signals.
4. **Authentication event history with privacy controls.** Login success/failure, session issuance/revocation, coarse network/device correlation identifiers, time, and retention. Prefer keyed hashes/truncated network data over raw IP where possible; tightly authorize and audit access.
5. **Known-exploit correlation marker.** Release/build/version and server incident window on relevant events, so race-condition or duplication investigations can target affected operations.
6. **Relationship context where it has actual meaning.** Guild co-membership interval is already available; party/friend history should be recorded only if those systems exist and materially reduce false positives.

Do not collect raw mouse movement, invasive device fingerprints, or indefinite full IP histories merely because they might be useful. Establish purpose, retention, access, and deletion rules first.

## I. Recommended detection model

### Layered architecture

Use a pragmatic version of the proposed five layers:

1. **Raw authoritative events.** Keep `EconomyLedger` append-only and expand currency/progression coverage. Raw events remain facts, never “cheater” labels.
2. **Derived metrics.** Incrementally maintain account-window and account-pair-window aggregates for 1 hour, 24 hours, 7 days, 30 days, 90 days, and lifetime where meaningful.
3. **Immutable signal occurrences.** A rule evaluates a metric/event set and creates an explainable finding with expiry/review semantics.
4. **Cases and category priority.** Group related signals/accounts into a human-review case; expose category priorities rather than one universal guilt score.
5. **Investigation events.** Status, assignment, notes, dispositions, sanctions and evidence links remain human decisions in an append-only timeline.

This preserves the good separation already present between replaceable automated snapshot and human status, while replacing the lossy JSON score snapshot.

### Signal model

Each immutable signal occurrence should include at least:

- `SignalOccurrenceId`, `RuleId`, `RuleVersion`, `Category`;
- subject account(s), role (`possible feeder`, `possible recipient hub`, `counterparty`);
- severity (impact/urgency) separate from confidence (evidence reliability);
- `ObservationGroupKey` and supporting event/reference IDs;
- window start/end, first/last observed event, evaluation time, expiry/staleness;
- observed metric, expected/cohort range, percentile/MAD score if applicable;
- absolute magnitude and account-relative magnitude;
- threshold/config version and completeness/truncation status;
- compact immutable explanation facts and optional relationship/path snapshot.

Use structured columns for commonly queried fields and JSONB only for rule-specific evidence. Do not store only prose.

### Categories and initial signals

Start with categories that the telemetry can defend:

**Direct-transfer/alt-risk**

- persistent pair asymmetry over 7/30/90 days with material value;
- young-account acquired-value funnel within 24h/7d;
- multi-feeder convergence: >=2/3 independently feeder-like senders, coordinated in time;
- rapid 2-5 hop flow-through where intermediaries retain little value;
- repeated cycles/wash paths.

**Marketplace/economy**

- repeated related-pair trades far outside robust item price bands;
- net wealth creation inconsistent with authoritative source events;
- duplicate item lineage/reference or impossible balance reconciliation;
- acquire-then-transfer/sell bursts for rare/high-value items.

**Progression integrity**

- deterministic impossible states first; rate anomalies only after complete progression telemetry and cohort baselines.

Do not launch botting, general combat, or shared-network signals until their telemetry and privacy model are adequate.

### Aggregation and score semantics

- Calculate **category priority** from independent observations, not raw triggered-rule count.
- Within one `ObservationGroupKey`, take the strongest correlated signal and retain the others as explanation facets.
- Apply diminishing returns to repeated similar observations; add more weight for different time periods, counterparties, or channels.
- Confidence should reflect completeness and specificity: deterministic impossible state > complete multi-event pattern > population anomaly > single heuristic.
- Severity should reflect magnitude/impact: absolute and account-relative value, reach, recurrence, and exploit harm.
- If a single queue ordering number is operationally necessary, call it `InvestigationPriority`, publish its factors, and avoid `/100` probability styling.

### Context and statistical baselines

Use robust, interpretable comparisons:

- account-age buckets (0-1d, 2-7d, 8-30d, established);
- progression/region bands;
- wealth/activity bands;
- market-participant versus non-market cohorts;
- median and percentile/MAD comparisons, with minimum cohort sample size and fallback rules.

Avoid naive z-scores for heavy-tailed wealth/transfer distributions. A statement such as “99.95th percentile for 7-day outgoing share among level 20-30 accounts; 92% of all recorded acquired tradable value went to one account” is defensible. “20,000 cinders” alone is not.

### Temporal model

- 5-60 minutes: duplication/retry bursts, rapid flow-through, exploit windows.
- 24 hours: new-account funnels, acquisition-to-transfer bursts.
- 7 days: repeated funnels, marketplace pairing, activity cadence.
- 30/90 days: persistent asymmetry, cluster stability, slow funneling.
- lifetime: total acquired/spent/transferred shares and historical relationship context, only when ledger coverage is known.

Time decay should affect queue priority, not delete immutable evidence. Repetition just below a threshold must accumulate through metrics rather than reset each window.

### Graph analysis recommendation

Graph-derived signals would materially improve feeder clusters, hubs, indirect funnels, and cycles, but a graph database is not justified.

Use PostgreSQL plus incrementally maintained edge aggregates:

- directed node = account;
- edge keyed by source, destination, channel, asset/value band and window;
- count, gross/net value, first/last time, top references, reciprocity and completeness;
- bounded batch algorithms over the selected time-window graph for components, strongly connected components, fan-in hubs and 2-5 hop paths.

At the game’s current architecture this can run as a periodic background projection. Reassess specialized graph storage only if measured relational batch time, edge volume, or interactive exploration becomes unacceptable.

## J. Recommended LiveOps investigation experience

### Queue

Show:

- player/account;
- investigation priority plus category severities/confidence;
- highest-value explanation in one sentence;
- independent observation count (not rule count);
- first/last supporting evidence time and “new since review” count;
- magnitude relative to wealth/cohort;
- data completeness badge;
- case status, assignee, and last human action.

Default to new/unreviewed material findings. Let operators include Low/informational items deliberately. Preserve separate filters for automated category and human disposition.

### Investigation page

Use one case timeline that interleaves immutable signals, supporting events, human notes/status changes, and sanctions. Add tabs/sections for:

- summary and top independent observations;
- evidence timeline with direct links to events;
- economic balance/source/sink timeline;
- counterparties and relationship graph;
- marketplace and guild-vault context;
- progression/activity context where available;
- related cases/accounts;
- global administrative audit.

The existing direct transfer table, relationship navigation, notes, and explicit “not proof” language should be preserved. Compose the existing player-support snapshot into this page rather than making operators manually navigate and mentally join it.

### Example of a good summary

> **Possible feeder cluster — High severity, Moderate confidence**  
> Review priority: 72 (not a probability). Three accounts created within 46 hours sent 89% of all server-recorded acquired tradable value to this account over seven days. Evidence is complete for the window.
>
> **Observed:** 61,830 cinders-equivalent received in 14 transfers; 56,430 from the three feeder-like accounts; last event 2026-08-18 11:42 UTC.  
> **Context:** recipient level 72; feeder levels 8/11/14; cohort percentile 99.95 for young-sender concentration.  
> **Independent observations:** (1) coordinated feeder convergence, (2) two rapid acquire-to-transfer bursts on separate days.  
> **Related:** one feeder was Cleared previously; six new events occurred after that review.  
> **Limitations:** item X has no reliable market valuation and is shown separately.  
> **Actions:** open evidence events, inspect cluster, assign, watch, clear with reason, or escalate. No sanction is automatic.

## K. Data/backend changes

### Schema

- Add `DetectionRun` with version, windows, cursor/partition, counts, timings, completeness and failures.
- Add `AccountMetricWindow` and `AccountPairMetricWindow` projections; use day/hour buckets and rollups rather than rewriting lifetime totals without provenance.
- Add immutable `DetectionSignalOccurrence` plus `DetectionSignalEvent`/reference join.
- Add `InvestigationCase`, `InvestigationCaseAccount`, and append-only `InvestigationEvent`; migrate current status/notes without losing the existing global `AdminAction` records.
- Add complete currency mutation event types/fields to `EconomyLedger` or a compatible authoritative ledger.
- Retain rule configuration/version snapshots. Do not rely on mutable appsettings plus an integer alone.

### Indexes and query shape

- Existing `(SenderAccountId, OccurredAt)`, `(RecipientAccountId, OccurredAt)`, `(EventType, OccurredAt)`, account-pair transfer, marketplace participant/time and risk snapshot indexes are a good base.
- For the current OR-heavy direct-transfer workload, consider partial PostgreSQL indexes for qualifying direct cinder/item rows after measuring real plans, e.g. participant/time predicates restricted by event type and non-null counterparties.
- Capture production-like `EXPLAIN (ANALYZE, BUFFERS)` for candidate sender/recipient grouping, first-hop, second-hop, queue search and detail timeline before adding those indexes. This audit could not execute plans without the deployment database/data distribution.
- Add unique/idempotency constraints for signal rule+observation+version and currency mutations.
- Query account-pair/window projections for queue evaluation; raw ledger scans should be evidence expansion/backfill jobs, not every 30-minute steady-state run.
- Partition/retention should be explicit before 10x/100x growth. Keep compact daily aggregates longer than raw high-frequency activity telemetry.

### Background jobs

- Ingest/update metric buckets incrementally from append-only events.
- Run bounded rule families independently with durable cursors, retries, run health and dead-letter/error visibility.
- Recompute affected subjects and neighbors on late events/version changes; use full backfills as separately observable jobs.
- Make coverage a release criterion: no account is “evaluated” for a metric unless required inputs are complete.

### APIs

- Queue endpoint: category/confidence/independent observations/completeness/new-since-review/assignee.
- Signal endpoint: immutable evidence snapshot and cursor-paginated supporting events.
- Relationship endpoint: bounded time/channel/value graph and aggregate table.
- Case event endpoints with optimistic concurrency and required disposition reasons.
- Player evidence endpoints should reuse support-snapshot capabilities and enforce the same backend permissions.

### Migration/deployment implications

- Current migrations already add the ledger and risk investigation tables (`20260814232224_AddEconomyLedgerAndItemProvenance`, `20260818121601_AddAccountRiskInvestigation`, `20260818133205_ImproveAccountRiskEvaluation`). New work needs additive migrations and backfills; do not rewrite old evidence in place.
- Backfilled ledger history has a known beginning. Mark `CoverageStartAt` per event family; never present pre-coverage lifetime percentages.
- Deploy projections dark, validate counts/reconciliation, then shadow-score before changing the operator queue.
- Infrastructure-as-code belongs to another repository and is outside this audit/change scope.

## L. Frontend changes

1. Rename current scope to “Direct transfer signals” until other categories are real.
2. Replace prominent `/100 risk` styling with investigation priority, confidence, category, and independent observations.
3. Show coverage/truncation per account and per signal, not just global earliest event/count.
4. Link every signal to exact events and counterparties; add cursor pagination and total counts.
5. Integrate current balances, acquisition provenance, marketplace, guild, restrictions and administrative audit from the existing player-support feature.
6. Add “new evidence since last review,” signal disposition, case assignment and conflict-safe status updates when multi-operator use warrants them.
7. Add a relationship graph only for clusters; retain the accessible table as the canonical view.
8. Expose historical rule/version/evidence diffs and add account-risk action types to audit filters.

## M. Testing strategy

### Existing coverage

- Eleven evaluator tests cover balanced transfers, young feeders, deliberately one-event Moderate/High behavior, reciprocal items, multiple feeders, high balanced volume, one three-hop cycle, and age context (`AccountRiskEvaluatorTests.cs`).
- Reflection tests assert policy attributes on the four risk controller methods (`AccountRiskAuthorizationTests.cs`).
- General LiveOps tests cover administrative actions, owner bootstrap, origin/configuration, operational status, action previews, and support snapshots.
- Frontend API tests cover query construction and status request transport, but there are no risk component behavior tests.

### Important gaps

**Rule correctness and false positives**

- zero/tiny/large magnitude boundaries;
- one legitimate gift must not become High;
- guild/friend/returning-player scenarios;
- item quantity, rarity/value unknown, and unequal reciprocal items;
- account exactly at 14 days, ratios exactly at thresholds, corrupt/negative event age;
- sender/receiver role-specific outcomes.

**Correlation and aggregation**

- one event triggering several facets counts as one observation;
- repeated independent days versus split transactions from one event;
- cover transfers, slow funnels, multi-feeder and multi-hop patterns;
- complete vs truncated denominator behavior;
- marketplace/guild/direct cross-channel paths.

**Temporal behavior**

- precise supporting timestamp, expiry, reappearance, late event, window boundaries and time decay;
- cleared case with genuinely new evidence reopens/notifies correctly.

**Repository/integration**

- candidate fairness/cursor coverage beyond 2,000 accounts;
- caps beyond 100,000 rows must never silently produce a confident ratio;
- exact SQL/provider tests for PostgreSQL queries and advisory locking;
- snapshot/history creation, signal-change-with-same-score, evidence immutability and version backfill;
- reconciliation of direct transfer history and ledger references.

**Security and administration**

- real authorization middleware tests for 401/403/200, not only reflection;
- cookie antiforgery pass/fail and bearer behavior;
- object access across arbitrary account IDs;
- concurrent status updates, idempotency conflicts, append-only enforcement and full previous/new-state audit;
- permissions for sensitive authentication/network telemetry if added.

**Frontend**

- queue filters, loading/empty/error states, pagination and truncation badges;
- exact evidence deep links, permissions, status conflicts and new-evidence indicators;
- terminology tests preventing “cheater/guilty” claims for probabilistic signals.

Before trusting a redesign, run it in shadow mode on production-like distributions, manually label a stratified sample including low-scored accounts, and report precision-at-review-budget rather than number/percentage flagged.

## N. Implementation roadmap

### Phase 0 — Stop misleading operators and measure coverage

- **Goal:** make current output safe to interpret before expanding it.
- **Backend:** add run/coverage diagnostics; expose caps, processed/eligible/skipped counts and evidence completeness; correct `LastTriggeredAt` to supporting evidence time.
- **Frontend:** rename to direct-transfer signals, soften numeric score semantics, display coverage/truncation and known limitations.
- **Database:** small additive `DetectionRun`/coverage schema or equivalent operational table.
- **Tests:** >2,000 candidates, >100,000 rows, one-event false positives, trigger-time accuracy.
- **Migration concerns:** additive only; existing snapshots remain explicitly legacy/version 5.
- **Dependencies:** none beyond current ledger.
- **Expected benefit:** prevents false confidence and reveals whether evaluation results cover the population.

### Phase 1 — Fix direct-transfer correctness

- **Goal:** produce a small, useful set of explainable direct-transfer leads.
- **Backend:** materiality floors, separate sender/recipient roles, multiple time windows, youth as context, true multi-feeder requirement, correlation grouping, no confident percentages on partial data.
- **Frontend:** independent observation count, role and magnitude; show single gifts as context rather than cases.
- **Database:** account/pair window aggregates and durable evaluation cursor.
- **Tests:** boundary, legitimate-gift, slow/split/cover-transfer, complete/truncated and adversarial cases.
- **Migration concerns:** new rule version; shadow results before replacing v5.
- **Dependencies:** complete direct-transfer history after its known coverage start.
- **Expected benefit:** major noise reduction and fairer population coverage.

### Phase 2 — Durable evidence and case workflow

- **Goal:** make every escalation reproducible and every human decision auditable.
- **Backend:** immutable signal occurrences/event links; case/events; new-evidence-after-review; optimistic concurrency.
- **Frontend:** evidence deep links, case timeline, signal dispositions, global-audit links; optional assignment.
- **Database:** signal/case/event tables, indexes and migration of current notes/status.
- **Tests:** immutability, reconstruction, same-score evidence changes, transitions, concurrency and permissions.
- **Migration concerns:** retain current JSON/history for legacy display; do not fabricate missing historical event links.
- **Dependencies:** Phase 1 observation grouping and stable rule versioning.
- **Expected benefit:** defensible investigations months later.

### Phase 3 — Complete economic context

- **Goal:** support wealth-relative and source-consistency findings.
- **Backend:** instrument all currency source/sink paths; reconciliation; robust item value bands; acquisition-to-transfer findings; marketplace favorable-trade analysis.
- **Frontend:** balance/source/sink timeline, provenance and limitations.
- **Database:** expanded ledger types/fields and aggregate projections; partial indexes based on measured plans.
- **Tests:** every currency mutation source, double-entry/reconciliation properties, price outliers and missing valuation.
- **Migration concerns:** explicitly mark currency coverage start; no false lifetime claims for old accounts.
- **Dependencies:** inventory of all balance mutation paths and stable idempotency references.
- **Expected benefit:** converts direct-flow ratios into economically meaningful evidence and enables exploit detection.

### Phase 4 — Cross-account graph intelligence

- **Goal:** find multi-hop funnels, hubs, rings and coordinated clusters.
- **Backend:** channel-aware edge projections, bounded path/cycle/connected-component jobs, cluster cases.
- **Frontend:** relationship table plus graph for meaningful clusters; time/channel/value controls.
- **Database:** pair/window edges and cluster membership/snapshot tables; remain on PostgreSQL initially.
- **Tests:** 2-5 hop paths, repeated/circular motifs, high-degree legitimate guild/market networks, deterministic performance fixtures.
- **Migration concerns:** backfill by bounded windows; version graph semantics.
- **Dependencies:** Phases 1-3 completeness and value semantics.
- **Expected benefit:** detects the most important current alt-account evasion paths.

### Phase 5 — Progression/activity integrity where telemetry supports it

- **Goal:** add deterministic progression and carefully justified bot/combat leads.
- **Backend:** progression transitions and action summaries; impossible-state rules first; cohort rate anomalies second; selected authoritative combat validation.
- **Frontend:** separate categories with their own evidence/confidence, never folded blindly into transfer score.
- **Database:** retention-tiered progression/action aggregates; privacy-reviewed auth events only if necessary.
- **Tests:** reachable-state properties, server timing limits, legitimate long sessions/events, privacy/authorization.
- **Migration concerns:** potentially high-volume telemetry requires retention/partition planning.
- **Dependencies:** explicit telemetry specifications and operational baselines.
- **Expected benefit:** expands scope honestly without compromising explainability.

### Phase 6 — Calibration and operational feedback

- **Goal:** optimize investigations, not flag count.
- **Backend:** disposition analytics, precision-at-K, false-positive cohorts, threshold/version comparison and safe rollback.
- **Frontend:** capture structured disposition reasons and investigator feedback with minimal burden.
- **Database:** rule-version outcome metrics; do not use moderator labels as unquestioned ground truth.
- **Tests:** backtests with leakage controls and distribution-change checks.
- **Migration concerns:** privacy and retention for investigator feedback.
- **Dependencies:** enough reviewed cases and stable definitions.
- **Expected benefit:** defensible tuning based on actual moderator utility. ML can be reconsidered only after this phase, and only if it materially outperforms explainable rules.

## Final answer

> **If I were personally responsible for using this LiveOps dashboard to decide which accounts deserve investigation, would I trust the current flagging system?**

**No.**

I would use it to inspect a player already brought to my attention and to browse direct transfers. I would not trust its queue ordering or Moderate/High/Critical labels to decide whom to investigate because harmless single events are intentionally promoted, population/evidence coverage can be silently incomplete, correlated interpretations inflate scores, and most abuse categories are outside its telemetry and rules.

The smallest set of improvements that would make me comfortable relying on it as a **direct-transfer investigation queue** is:

1. eliminate single low-materiality gift escalation and distinguish sender from recipient;
2. add durable fair evaluation coverage with explicit completeness/no-truncation guarantees;
3. group correlated rules into independent observations and rename the number investigation priority;
4. persist event-linked evidence snapshots with rule/config/window/version;
5. surface current economic/support context and new evidence since the last human review;
6. validate the new behavior with false-positive, boundary, truncation, expiry and authorization integration tests plus a shadow-mode manual sample.

That would not yet make it a comprehensive cheat detector. It would make it an honest, explainable, and operationally useful direct-transfer risk system on which later economy, graph, progression, and activity detectors could safely build.
