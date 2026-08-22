# LiveOps alt-account temporal-correlation analysis

Date: 2026-08-22  
Scope: Game authentication data, `API.LiveOps`, and the private LiveOps Angular application

## Decision

Login/session timing can produce a useful **investigation lead**, but the data that
exists today cannot establish that two accounts have the same owner. The dashboard
should display the result as **temporal account correlation**, never as “confirmed
alt account,” and it must not contribute to an automatic sanction.

The recommended delivery is:

1. add a read-only, shadow-mode correlation panel for economically connected or
   operator-selected accounts;
2. calibrate it against real population data and reviewed cases;
3. add explicit, privacy-controlled authentication events before treating the
   signal as more than supporting context.

This distinction matters because households, shared networks, regional play hours,
scheduled events, guild activity, and ordinary token refreshes can all make unrelated
accounts look temporally similar.

## What the repository records today

`RefreshToken` stores `UserId`, `CreatedUtc`, `ExpiresUtc`, `RevokedUtc`, and
`ReplacedBy`. `JwtGenerator.IssueTokens` creates a row both for an interactive token
issuance and for refresh-token rotation. The configured access-token lifetime is 30
minutes.

Consequences:

- `CreatedUtc` is a **token-issuance timestamp**, not necessarily a login timestamp;
- `/createNewTokens` adds another row when a session refreshes;
- registration, guest creation, Google login, rename, guest conversion, and Google
  binding can also issue a new token pair;
- `ReplacedBy` links a rotated token to the hash of its replacement, so token chains
  can be reconstructed and refresh rows can often be separated from chain roots;
- a chain root is still only “new token chain started,” not guaranteed to be a
  password/Google login, because several authenticated account operations also call
  `IssueTokens`;
- `RevokedUtc` with a replacement represents rotation, while revocation without a
  replacement may be logout or another revocation path;
- no IP, device, user-agent, auth method, explicit event kind, or stable session ID is
  retained.

The existing risk evaluator only loads the maximum refresh-token `CreatedUtc` as
`LastSessionUtc`. The player-support service and risk UI correctly label this as
session issuance rather than a true login history. The account-risk candidate set is
currently built from direct economy transfers, so it does not cover unrelated
accounts that merely have similar activity times.

## What timing can reveal

The useful observation is not “both accounts play at 20:00.” It is repeated,
pair-specific behavior that is unusual relative to the population.

### Stronger temporal patterns

- **Repeated near-simultaneous chain starts.** Account B starts a token chain within a
  few minutes of account A on many distinct days.
- **Repeated handoffs.** One account's inferred session becomes inactive and another
  starts shortly afterward, in the same order or alternating order, across multiple
  days. This is consistent with one operator switching accounts, although it is not
  proof.
- **Stable timing offsets.** The nearest activity pulse repeatedly occurs at a narrow
  offset, rather than being distributed throughout common peak hours.
- **Unusually similar active-day/hour profiles.** The accounts share active days and
  hours substantially more often than expected for their region/time cohort.
- **Transfer-adjacent switching.** A correlated session start or handoff repeatedly
  occurs shortly before direct value movement between the same accounts. This is an
  independent economy observation and is more useful than timing similarity alone.
- **Cluster repetition.** Three or more connected accounts are activated in a similar
  sequence on several days. This is more distinctive than one coincidental pair.

### Weak or misleading patterns

- the same last-login date;
- both accounts playing during evening peak hours;
- one near match;
- matches observed only during a global event, deployment reconnect, or outage;
- a high raw match count from two extremely active accounts;
- simultaneous activity on its own, which may be a household, multiple browser tabs,
  or a shared/guild activity;
- non-overlap on its own, because the current token chain does not provide an exact
  session end or authoritative gameplay-presence interval.

## Recommended first-version analysis

### 1. Bound candidate pairs

Do not compare every account with every other account. An all-pairs scan is quadratic
and would produce many chance matches.

For the first version, analyze only:

- accounts already present in an account-risk subject's direct-transfer
  `Relationships`;
- accounts in the same bounded two-hop transfer cluster; and
- a pair or small set explicitly selected by an operator.

Later, privacy-preserving network/device identifiers can generate additional
candidates without a global timestamp-similarity scan.

### 2. Reconstruct approximate token chains

Within a configurable window, initially 90 days:

1. load token metadata for all accounts in the bounded candidate set in one query;
2. join `ReplacedBy` to the replacement row's `TokenHash` for the same account;
3. identify chain roots and ordered rotation timestamps;
4. collapse duplicate/retry timestamps within a small tolerance;
5. derive an approximate interval from chain-root creation through the final rotation
   or revocation/expiry, while explicitly marking the end as inferred;
6. retain coverage start, event count, active-day count, and truncation state.

No token hash or replacement hash should leave the backend.

### 3. Calculate explainable pair metrics

Return measured facts, not a black-box probability:

- chain starts for A and B;
- active days for each account;
- shared active days and active-day Jaccard similarity;
- near-start matches within 2, 5, and 15 minutes;
- repeated matches on distinct UTC dates;
- inferred handoffs within 5 and 15 minutes;
- median and interquartile range of nearest-event offsets;
- hour-of-week profile similarity;
- expected accidental matches and observed/expected lift;
- transfer-adjacent matches and their transfer IDs;
- first/last supporting time;
- evidence completeness and analysis version.

The expected-match baseline must account for account activity volume and common
server play periods. A raw count is biased toward highly active accounts. A practical
shadow implementation can compare a pair against weekday/hour buckets; a better
calibration uses population cohorts and permutation tests over shifted account days.

### 4. Gate the assessment on evidence quality

Do not show a material assessment when either account has too little data. Suggested
shadow-mode starting gates—not production truth—are:

- at least 7 active days per account;
- supporting matches on at least 3 distinct days;
- more observed matches than the volume-adjusted baseline predicts;
- no incomplete/truncated query;
- at least two independent observations before “moderate correlation,” for example
  repeated handoffs plus transfer-adjacent switching.

Thresholds must be stored with an analysis version and calibrated from actual data.
They should not be copied into an enforcement policy based only on intuition.

### 5. Produce a categorical lead

Use `InsufficientData`, `NoMaterialCorrelation`, `Low`, `Moderate`, and `High` as
investigation-assessment labels. “High” means strong temporal correlation under the
configured rule; it is not a probability and does not mean “confirmed alt.”

Initially, keep this assessment separate from the existing `/100` direct-transfer
risk score. Combining the two immediately would double-count transfer-adjacent
evidence and make the result harder to explain.

## LiveOps presentation

### Account-risk detail page

Add a lazy-loaded panel after **Connected accounts** named **Possible account
correlation**. Preserve the existing warning that automated signals are not a finding
of guilt.

For each bounded related account, show:

| Field | Meaning |
| --- | --- |
| Related account | Character name and navigable account ID |
| Assessment | Insufficient, none, low, moderate, or high temporal correlation |
| Repeated days | Distinct days containing supporting matches |
| Near starts / handoffs | Counts with the configured time windows visible |
| Match lift | Observed matches relative to the activity-volume baseline |
| Transfer-adjacent | Count and links to exact retained transfer evidence |
| Coverage | Window start, token-chain counts, and completeness |
| Last observed | Newest supporting timestamp |

The summary sentence should read like:

> **Moderate temporal correlation — supporting context, not proof of shared
> ownership.** Eight near-session handoffs occurred on five distinct days, 4.3× the
> activity-adjusted expectation. Three occurred within 15 minutes of a direct
> transfer. Authentication event type and device/network identity were not recorded.

An expandable comparison should show two UTC timeline lanes with:

- chain starts as solid markers;
- refresh rotations as lighter markers;
- inferred session spans as dashed ranges;
- transfer events as separately colored markers;
- exact metric definitions, analysis version, and limitations.

Do not expose token hashes, email addresses, raw IP addresses, or a device fingerprint.

### Queue behavior

During shadow mode, do not reorder the account-risk queue. A small “temporal context
available” badge and a filter are sufficient after false-positive review. Queue
ranking should change only after calibration demonstrates added investigative value.

## Repository-specific backend design

Prefer a separate lazy endpoint so correlation query cost and failure do not block the
existing risk-detail response:

```text
GET /api/liveops/account-risk/{accountId}/temporal-correlations
    ?windowDays=90&relatedAccountId={optional-guid}
```

The endpoint should require `liveops.read`, apply a small maximum related-account
count, clamp the lookback, and return no authentication secrets. If future responses
include privacy-preserving network/device correlation, introduce a narrower
permission such as `liveops.account-linkage.read` and audit access to the expanded
evidence.

Suggested types:

- `IAccountTemporalCorrelationService` in the Application administration boundary;
- repository query/projection in Persistence;
- `AccountTemporalCorrelationDto` containing only display-safe aggregate facts and
  evidence references;
- controller action alongside the current account-risk detail endpoint;
- a separate Angular API call and models so the panel can load independently.

The current `RefreshTokens` schema only indexes `TokenHash`. Add and measure at least
`(UserId, CreatedUtc)` for bounded account/window reads and an index suitable for the
`ReplacedBy` chain lookup. Use production-like `EXPLAIN (ANALYZE, BUFFERS)` before
finalizing PostgreSQL index shape.

For repeated queue use, do not recalculate correlations from raw token rows per page.
Add a canonical pair snapshot (`AccountIdLow`, `AccountIdHigh`) or account-pair window
projection with analysis version, window, metrics, completeness, and evaluated time.
Raw rows remain evidence; the projection is replaceable.

## Telemetry needed for a trustworthy second version

Add an append-only authentication event rather than trying to infer semantics forever
from refresh tokens. A minimal event contains:

- account ID and server timestamp;
- event kind: login success, registration/guest creation, token refresh, logout,
  revocation, or reuse rejection;
- opaque session ID and authentication method;
- application/build identifier;
- optional keyed, versioned HMACs for a coarse network prefix and a low-entropy client
  family, if legally and operationally justified;
- retention/coverage metadata.

Capture request-derived fields at the Game API boundary, where trusted forwarded
headers and request metadata are available. Do not pass raw IP or user-agent values
through domain services or store them indefinitely. Use a separately managed HMAC
key, rotate key versions, restrict access, document retention/deletion, and never
present a shared household/network as proof of shared ownership.

Avoid invasive device fingerprinting, raw mouse/input telemetry, and indefinite full
IP history. The purpose is bounded abuse investigation, not general player tracking.

## False-positive controls

- require multiple distinct days, not many events on one day;
- compare against activity-volume and hour-of-week baselines;
- suppress known deploy/reconnect/incident windows;
- show guild/shared-event context when available;
- separate timing, economy, network, and device observations so correlated rules are
  not counted as independent evidence;
- expire old findings and flag evidence that is new since the last human review;
- require an operator reason for any investigation disposition or sanction;
- never automatically ban, restrict, or label an account from this signal.

## Rollout and verification

1. Add chain reconstruction and pair-metric unit tests using synthetic timelines.
2. Test timezone boundaries, sparse accounts, high-volume accounts, simultaneous
   sessions, token rotation, rename/bind token issuance, truncated evidence, and known
   incident windows.
3. Add repository integration tests for canonical pair ordering, bounded queries, and
   index-supported window filtering.
4. Add controller authorization and DTO redaction tests.
5. Add Angular tests for insufficient evidence, limitation text, navigation, timeline
   accessibility, loading failure, and incomplete coverage.
6. Run the feature in backend-only shadow mode and record metric distributions without
   changing risk score or queue order.
7. Manually review a stratified sample of high, moderate, low, and random pairs;
   explicitly sample households/guildmates and peak-event periods.
8. Set thresholds from measured precision and operator usefulness, version the rule,
   then enable the panel.

Relevant verification commands when implemented:

```powershell
./build/run-tests.ps1

$env:npm_config_cache = Join-Path $env:TEMP 'll-liveops-npm-cache'
npm ci
npm test -- --watch=false
npm run build
```

Run the npm commands from `LL/src/Presentation/liveops`. No deployment or shared
database migration application is part of the repository change.

## Suggested implementation sequence

### Phase 1: useful context from current data

- add the composite token-query index migration;
- implement bounded token-chain reconstruction and display-safe metrics;
- expose the lazy read endpoint;
- add the account-detail panel behind a feature flag;
- shadow-calibrate without score or queue impact.

### Phase 2: explicit authentication telemetry

- add privacy-reviewed `AuthenticationEvent` storage and retention;
- populate explicit login/session/logout semantics at the API boundary;
- backfill no claims that cannot be proven from old token data;
- enrich correlations with separately labeled network/client observations.

### Phase 3: durable case evidence

- materialize versioned pair/window projections;
- add immutable signal occurrences and new-evidence-since-review behavior;
- enable queue filtering only after operational validation;
- retain human confirmation and enforcement as separate audited decisions.

## Bottom line

The existing timestamps are enough to prototype and display **possible temporal
linkage among already-related accounts**. They are not enough to identify account
ownership reliably. The most defensible first implementation is a bounded,
explainable, read-only correlation panel with explicit limitations, followed by true
authentication-event telemetry and shadow calibration before the signal affects any
priority score.
