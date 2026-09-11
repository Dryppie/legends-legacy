# Nobility — Implementation Plan and Status

Updated 11 September 2026 after the alpha implementation. This document replaces the earlier sequence of proposed implementation tasks with delivered scope and remaining work. The [subscription specification](subscription-specification.md) remains the product source of truth; the [alpha implementation report](nobility-alpha-implementation.md) contains the code map, setup instructions and recorded verification commands. The [initial product catalog](initial-product-catalog.md) distinguishes alpha availability from future cash offers.

## Current scope

**Nobility** is the account membership, a **Noble** is a player with active membership, and a **Signet** is the tradable item redeemed for one calendar month. Owning or receiving a Signet does not activate membership. Granting twelve produces twelve identical items; players can redeem any quantity and trade the remainder.

The user's alpha direction is implemented: a follow-up migration distributes one Signet to each existing character, including guests; operators can distribute additional Signets through LiveOps. Players test the game functionality, and purchasing has only a small disabled gateway stub for later Stripe integration. Cash purchasing, payment recovery and permanent cash-support recognition remain deferred.

Target services are the LL game API, Core, Persistence, Services, Worker, Angular game frontend and LiveOps. Chat decorations use the game frontend's shared character tag and canonical game API appearance data. No LL-Chat service change, infrastructure-repository change, external deployment or shared database update was performed.

## Delivery status

| Area | Implemented for alpha | Remaining boundary |
| --- | --- | --- |
| Membership and persistence | Account coverage history, centralized benefits, calendar anchor, membership versions, Signet units, issuance/movement/redemption/daily receipts, repositories and EF configuration. JSON and EF both support `MiscItemBase`. | Earlier schema/backfill migrations and actual PostgreSQL contention tests remain outstanding; the follow-up grant migration is tested on PostgreSQL. |
| Alpha grants | One-time migration gift of one Signet per existing character, including guests; permission-protected LiveOps grants for registered players. Grants create available items without activating membership or cash-support history. | Guests register before redeeming/trading. Later-created characters need a separate grant. Other gameplay sources remain future additions. |
| Redemption | One-click Redeem with an internal available-unit preview, registered-account ownership checks, exact unit/version validation, atomic consumption and extension, idempotent receipt. | Deployed multi-session smoke testing is outstanding. |
| Trading | Signet market category, listing reservation, buys, automatic matching, explicit fulfillment, commodity flows, resale, cancellation and expiry integration; normal Cinder fees and escrow rules. | Actual PostgreSQL races and an alpha market smoke test are outstanding. |
| Gameplay policies | Seven-day offline retention; six Essence and Equipment presets; eight Arena tickets; two-hour Focus; thirty sell and thirty buy orders; rerolls at 0/0/40/80; three +5% XP tracks. | Broader operational acceptance remains part of alpha rollout; test evidence is not a production-readiness claim. |
| Daily resources | Two generic Sigil Fragments and ten Soulstones per covered UTC date; bounded worker and Settings fallback with unique account/date receipts. | Deployed worker outage/recovery and reward-lag smoke tests are outstanding. |
| UI and appearance | Settings status/redemption, preserved preset viewing/copying, dynamic limits, market category, optional ◆ Noble icon before the name and expiry refresh. The badge toggle uses the standard LL checkbox. | Manual alpha UI smoke testing remains outstanding. |
| Purchasing | `INobilityPurchaseGateway`, `DisabledNobilityPurchaseGateway` and an unavailable checkout endpoint. | Stripe products, checkout, webhooks, fulfillment, refunds, payment history and cash-support badge are not implemented. |

## Implemented rules and design decisions

### Ownership, calendar months and retry behavior

- One membership belongs to the registered account. Signet units belong to characters until redeemed or traded. Redemption validates account ownership independently of client input.
- Each continuous membership period has an original UTC calendar anchor. Its expiry is that anchor plus the total redeemed calendar months, so successive and bulk redemption agree at month ends and leap dates. Reactivation after a lapse starts a new anchor.
- One click on Redeem obtains concrete available units, membership version and expiry through the internal preview endpoint, then immediately submits redemption. No preview or confirmation is shown to the player. The button and quantity control remain disabled during the request. An uncertain response retains the exact operation and units for retry, even if inventory refreshes; a definitive rejection clears the pending operation so a later click can obtain fresh state.
- Grant and redemption operation IDs support exact retries without duplicate issuance or extension. Changing a request requires a new operation ID. The current 1–1200 quantity validation is a per-request input bound, not a decided cash-spending or lifetime ownership policy.
- Signet inventory quantities project individual available units. Listing reserves units; partial fills move the corresponding units; cancellation returns the remainder. Issuance identity survives resale. Generic compensation, consumption and direct item-transfer paths cannot substitute for the canonical Signet flow.
- Mutations use the existing command transaction pipeline, character locks, account locking where appropriate and optimistic versions. PostgreSQL guarantees still need verification with the real provider; in-memory tests do not establish locking behavior.

### Daily resources

A UTC date qualifies if it overlaps active coverage for any positive duration, including a partial first or final date. Eligibility closes at the end of the date, then two generic `sigil_fragment` items and ten Soulstones are delivered automatically. There is no daily login or claim requirement.

Rewards are once per account per date. The first character to redeem a Signet remains the daily reward recipient; additional characters do not multiply the stipend. Redeeming more months does not repeat a date's reward. Daily resources are independent of the seven-day combat retention limit and remain deliverable after expiry.

`NobilityDailyRewardsJob` runs every five minutes, discovers up to 100 due accounts and processes up to 31 dates per account per transaction. Settings reconciliation invokes the same settlement command. Bootstrap remains read-only. Resource changes, the daily receipt and economy records commit together. The daily worker does not simulate combat.

### Offline combat and XP

The implementation uses historical coverage windows and the persisted action cursor instead of the originally proposed dedicated expiry combat checkpoints/worker:

1. Activation preserves eligible free work without recovering free time already lost to the 24-hour cap.
2. Active Nobility retains eligible unsettled combat within 168 hours.
3. At expiry, the covered retention window remains resolvable later; subsequent free combat is subject to its own 24-hour window.
4. Catch-up consumes retained windows in bounded batches and skips expired free gaps. Returning three days after expiry can therefore resolve retained Noble combat plus the latest free day.
5. Batches split at membership boundaries, and preset eligibility is evaluated at historical combat time. The action cursor prevents replay of already consumed work.

The separate expiry-settlement entity and expiry worker from the original proposal were not introduced. Existing action/build mutation boundaries and persisted combat snapshots remain relevant; the daily resource worker must not turn seven-day combat retention into unlimited banking.

Nobility adds 500 basis points to each eligible recipient's base Combat XP share, rather than applying the action owner's membership to everyone. The resulting Combat XP flows once through normal equipped Essence progression. Combat Style XP receives its own +5% on the eligible base share; it does not compound the increased Combat XP.

Bonuses use encounter/completion time rather than claim time. Dungeon pending rewards carry the earning-time Style bonus through claim and retreat flows. Dungeon Mastery adds its bonus once at completion. Added XP rounds down per encounter share or completion award; base awards below 20 receive zero extra. Style grants respect their existing cap. Mastery limits the added Nobility contribution to remaining progress before its cap while preserving existing base XP behavior.

### Limits and expiry

| Policy | Active Nobility | Expiry behavior |
| --- | --- | --- |
| Essence / Equipment presets | Six each; saved slot numbers identify the three extra presets. | Slots 1–3 remain usable. Retain slots 4–6 for viewing and copying into an available destination. Block applying, editing and automatic selection of locked presets; preserve committed combat snapshots and currently equipped items. |
| Arena tickets | Cap eight, unchanged one ticket per three hours; activation gives no refill. | Retain tickets above five. Regeneration follows earning-time coverage and resumes only below the effective cap. |
| Creature Focus | Two-hour cooldown from the last change. | Recalculate eight hours from the same last-change timestamp. |
| Marketplace | Thirty resting sell listings and thirty resting buy orders, counted separately. | Preserve orders and escrow. New resting orders require room below the corresponding free limit of ten; existing instant-match behavior remains. |
| Prophecy rerolls | Two free and two paid uses; 0/0/40/80 Fate Echo. | Retain separate free, paid and total usage. Free policy is one free and three total. No reset, refund or retrospective charge on a membership change. |
| Appearance | **Display Nobility** controls the ◆ icon and XP note. Hidden expiry/perks remain available to the profile owner, determined by character ID even through search. Other viewers follow the display preference. Perks start collapsed. | Hide active display; retain the preference for later reactivation. |
| XP / daily resources | Award eligible progress and covered dates. | No new eligibility after expiry; retain earned progress and pending eligible grants. |

### UI, state synchronization and identity

Settings shows status, expiry, available/listed Signets, benefit contents, quantity selection, one Redeem button and appearance choices. The chosen quantity is consumed immediately on successful redemption; separate one/all buttons are not required. After an uncertain response, Retry redemption completes the same operation and quantity stays locked until the outcome is known. Redemption is reached through Settings, and trading through the Signets market category.

Bootstrap carries membership state. Commands invalidate affected gameplay scopes. Server-time expiry, reconnect and tab-focus refresh keep membership status current; expiry also requests gameplay refresh at known server revisions without inventing new revisions. Backend policy checks remain authoritative.

Public appearance is resolved from canonical account coverage by the game API. Shared character tags, including general/whisper chat tags, fetch that projection; chat-supplied title text is not membership evidence. This replaced the proposed LL-Chat server identity change and removes the need to extend the backend test runner solely for this feature's chat display. Permanent cash-support recognition is not an alpha decoration.

## Remaining alpha verification and rollout

The implementation run passed 2,340 backend regression tests plus one subsequently added persisted Signet market integration test, 798 game frontend tests and 35 LiveOps frontend tests. Backend/frontend builds, EF's pending-model check and the Windows-aware whitespace check passed. Exact commands and the restore limitation are recorded in the [alpha report](nobility-alpha-implementation.md). These are recorded results, not new test runs for this documentation update.

Before exposing the feature to alpha players:

- Verify the earlier schema/backfills with existing presets and already-used Prophecy rerolls. The follow-up grant migration has passed disposable PostgreSQL tests: each existing character receives one Signet, earlier grants survive, restart does not repeat the gift, and no membership activates automatically.
- Exercise real-provider races: sell versus redeem, simultaneous redemptions, competing buyers, cancellation/expiry versus purchase, partial matches and duplicate daily workers. Check unit ownership, inventory projections, receipts and Cinder escrow together.
- Run a two-account smoke test: grant twelve, redeem one, list or sell the remaining eleven, redeem on the buyer, then exercise partial cancellation and resale. Alpha grants and market buyers must not gain cash-support history.
- Exercise active-to-expired limits, preserved presets and overflow, historical combat continuation, partial UTC reward dates, worker downtime/retry and open-screen/reconnect behavior. Verify desktop/mobile interaction and persisted dungeon claim/retreat rewards.
- Review the intended alpha database and rollout process. The game API's existing startup automatically runs migrations and then seeds data; starting it is a database mutation. The task has not applied the migration or started a deployment against an external environment.

Backend tests continue to run through `build/run-tests.ps1`. Use npm for the game frontend and keep npm caches outside the checkout. No application builds or tests are needed for markdown-only edits.

## Deferred Stripe implementation

The alpha stub returns HTTP 503 and an unavailable result. It performs no payment operation, item issuance or membership activation. Keep it disabled until the cash flow and recovery requirements below are implemented and verified.

1. **Products and configuration:** configure server-owned one-Signet and twelve-Signet offers from the catalog, currencies, tax display and Stripe product/price mappings. Store credentials through the existing secret mechanism. Resolve purchase/resale policy, partial-bundle refunds and responsibility for traded-Signet refund losses before cash launch.
2. **Checkout intent:** replace the side-effect-free checkout query with a transactional command when purchase state is introduced. Persist an intent and stable idempotency key; call Stripe outside gameplay transactions. Recover uncertain responses using the same intent rather than creating another charge.
3. **Verified fulfillment:** authenticate webhooks and validate authoritative payment state, account, product, currency and amount. Persist durable event receipts; handle duplicate/out-of-order success, refund and dispute events. Issue concrete Signet units with payment provenance exactly once. A browser success URL must not mint items or activate Nobility.
4. **Player purchase state:** show pending/failed/completed delivery and payment history. A twelve-pack delivers twelve items without annual renewal or automatic redemption. Optional recurring delivery is later work; if added, cancellation stops future charges/deliveries while retaining valid existing items and coverage.
5. **Refunds and disputes:** reserve untouched eligible units before a refund request, then revoke once on confirmed refund or release on definitive failure. Reconcile uncertain outcomes. Sold/redeemed units enter a traced review flow; do not automatically remove an innocent market buyer's membership or earned rewards. Preserve original bundle amounts for partial refunds.
6. **Support and recognition:** add a LiveOps trace from payment through issuance, trades, redemption and coverage, plus audited recovery actions. Award the permanent “Supported LegendsLegacy” badge only to the original qualifying cash purchaser, and remove it only when no valid qualifying purchase remains. Do not award it to alpha recipients or market buyers.
7. **Cash-launch verification:** test Stripe test-mode duplicates, failures, delayed events, disputed/refunded payments, partial fulfillment/recovery, stale clients and provider downtime. Monitor paid-but-undelivered purchases, event backlog, projection mismatches and refund uncertainty. Enabling sales remains separate from the completed alpha game implementation.

The proposed Legacy Supporter Pack and Heraldry Collection are not part of the alpha implementation. If selected, they need artwork, permanent cosmetic entitlements and their own fulfillment before sale.

## Migration, configuration and deployment record

`20260911121646_AddNobilityAndSignets` adds seven Nobility/Signet tables, deterministic preset-slot backfills and separate free/paid reroll counters. The follow-up `20260911153816_GrantAlphaSignetToExistingCharacters` seeds/corrects the Signet catalog entry before startup JSON seeding and grants one canonical, tradable unit to every existing character. It includes issuance/movement provenance and synchronizes available inventory quantities while preserving earlier grants. Deterministic gift identities prevent replay from duplicating or reviving spent units. It does not activate Nobility, create cash-support history or grant items to future characters on subsequent startup. Its `Down` refuses rollback because gifts may already have been traded or redeemed; preserve that history and use a forward fix. The follow-up migration passed disposable PostgreSQL verification against a baselined schema; the full earlier migration chain and contention remain unverified. No shared database was modified.

Alpha requires no Stripe keys, provider product mappings or new environment-specific purchase configuration. Benefit values are centralized in `NobilityBenefits`; coverage records retain a policy version. Independent purchase/trade/redemption feature switches and payment monitoring from the original plan were not added as alpha configuration.

Coordinate the schema/catalog with the updated game API, Worker, LiveOps and frontends. Preserve entitlement and audit data when planning rollback; removing the new tables would destroy that history. No LL-Chat deployment is required for the implemented appearance path. Applying shared migrations, provisioning Stripe and deploying services remain external actions, not actions performed by this documentation update.
