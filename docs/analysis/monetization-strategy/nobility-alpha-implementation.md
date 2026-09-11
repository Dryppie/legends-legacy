# Nobility alpha implementation

Implementation and verification record as of 11 September 2026. Implemented in the LL game backend, Worker, LiveOps and game frontend. Cash purchases remain disabled, as requested. Nothing has been deployed and no shared database has been changed. The follow-up grant migration was exercised only in disposable local PostgreSQL databases. The [specification](subscription-specification.md) lists product rules; the [updated implementation plan](subscription-implementation-plan.md) separates completed work from remaining verification and future commerce.

## Giving Signets to testers

1. Apply `20260911121646_AddNobilityAndSignets` and the follow-up `20260911153816_GrantAlphaSignetToExistingCharacters` through the normal alpha rollout process. The game API's existing startup calls `Database.MigrateAsync()` before catalog seeding. The follow-up migration gives **one tradable Signet to every existing character**, including guests, without activating membership. Earlier grants are preserved. Subsequent starts do not repeat the gift, and later-created characters receive no automatic grant. Guests must register before redeeming or trading.
2. For additional grants, open a registered player's character in LiveOps. An operator with **Economy compensation** permission can use **Alpha Signets**, enter a quantity and reason, review, then confirm. Use this action rather than generic item compensation: each Signet needs its own provenance record.
3. The player opens **Settings → Nobility**, chooses a quantity and clicks **Redeem** once. The selected Signets immediately activate or extend Nobility; there is no separate preview or confirmation screen. The client obtains the server preview internally and preserves the same operation for uncertain-response retries. A grant of 12 creates 12 tradable items; none activate merely from being granted. The player can redeem any selected quantity together.
4. Players can find **Signets** in the marketplace to buy, list, resell or place buy orders using Cinders. Listed Signets must be cancelled before redemption.

The grant endpoint is `POST /api/liveops/characters/{characterId}/signets`, with `operationId` (UUID), `quantity` (1–1200) and `reason` (3–1000 characters). LiveOps authentication, permission checks and antiforgery protection still apply. The UI reuses the same operation ID when retrying an uncertain response. Repeating an identical successful grant returns its original receipt; changing the payload requires a new operation ID. Grants are recorded in the character's administration history.

## Implemented behavior

| Area | Alpha behavior |
| --- | --- |
| Membership | Account ownership; one calendar month per redeemed Signet; stable calendar anchor for successive extensions; exact previewed units and membership version checked during redemption. |
| Trading | Individual Signet provenance, market reservation, transfer, cancellation and resale; normal Cinder fees. Inventory stacks project available units. Generic grants and transfers cannot manufacture Signets. |
| Offline combat | 168 hours while covered, 24 hours free. Coverage history preserves the retained work at expiry; the action cursor consumes it in bounded batches. Activation does not restore previously lost free time. |
| Presets | Six Essence and Equipment presets. Stable saved slot numbers keep the first three usable after expiry; additional presets remain viewable and can be copied into an available destination. |
| Arena | Eight stored tickets, unchanged one ticket per three hours. Activation gives no refill; expiry retains existing tickets above five. Regeneration uses coverage at each earning time. |
| Focus | Two-hour cooldown while Noble, eight hours free, recalculated from the last change. |
| Marketplace | Thirty resting sell listings and thirty resting buy orders, counted separately. Existing orders survive expiry. |
| Prophecy | 0 / 0 / 40 / 80 Fate Echo. Separate free and paid counters preserve consumption when membership changes during a day. |
| Appearance | Optional ◆ icon before the character name, controlled by **Display Nobility**. Disabling it hides overview expiry and perks from other viewers; the owner can still see expiry and Show perks, including through profile search. The icon follows the preference for everyone. Perks start collapsed. Display disappears on expiry; hiding it never disables gameplay benefits. Character tags fetch canonical game API metadata, including chat tags. |

The client uses server time for expiry and requests refreshed Essence, Equipment, Colosseum and Prophecy state at known server revisions when membership changes. Backend validation remains authoritative. No LL-Chat service change or chat-specific test-runner extension was needed for this display path.

Nobility no longer awards bonus XP or daily resources. Settings reads status with GET; the daily reconciliation endpoint and award command have been removed. The former daily job is no longer registered. Its Quartz type remains only to delete an existing persisted job and its triggers when it next runs on an updated worker.

## Implementation choices

The original plan proposed dedicated expiry combat checkpoints. This implementation instead retains historical coverage windows and uses the persisted action cursor, splits batches at membership boundaries, and evaluates preset eligibility at the historical combat time. That preserves retained combat through an offline expiry without a separate worker resolving combat or changing character builds. Ordinary combat, Style and Mastery calculations apply equally to free and Noble players.

Signet unit versions and membership versions provide optimistic conflict detection. Commands also use the existing transaction pipeline, character row locks and an account advisory lock where appropriate. Issuances, movements, redemption receipts, historical daily receipts and administration/economy records provide an audit trail. Alpha grants do not create permanent cash-support history.

## Changed code map

| Files / directories | Purpose |
| --- | --- |
| `Core/Domain/Models/Nobility`, `Core/Application/Interfaces/Services/LL/Nobility`, `Core/Application/UseCases/Nobility` | Entitlements, history, contracts and transactional commands / read queries. |
| `Infrastructure/Service/Services.LL/Nobility`, `Infrastructure/Persistence/Persistence.LL/{Repositories,Configurations}/Nobility` | Membership, alpha grants, redemption and concrete Signet trading. |
| Game and LiveOps `NobilityController`, LiveOps handler registration, `Data/items/items.json` | Authenticated player/admin routes and the Signet catalog item. |
| Combat orchestration/rewards, Colosseum, Focus, Prophecy, Marketplace, Essence/Equipment services and their consumers | Gameplay policies, historical evaluation and preset expiry. |
| Bootstrap and state synchronization contracts | Refresh membership, inventory and affected gameplay state after changes. |
| `Presentation/ll` Nobility service/settings/decoration and market/preset components; `Presentation/liveops` player workspace | Tester redemption, appearance, trading and operator grants. |
| `Worker.LL/BackgroundJobs/NobilityDailyRewardsJob.cs` | Retire existing persisted daily schedules without granting resources. |
| Migration, model snapshot, backend and Angular tests | Schema, backfills and regression coverage. |

## Schema and release implications

The XP and daily resource removal needs no new migration or configuration. Historical daily receipts and inactive cursor columns remain for audit. Previously earned XP, resources and persisted pending combat rewards are preserved; unprocessed daily dates will not pay out. New coverage uses policy version 2, and the reduced benefit policy applies to all memberships. Update the API, frontend and all Worker instances together; old workers must be stopped to prevent further awards.

`20260911194553_RemoveNobilityProfileHeader` removes the obsolete `ShowHeader` preference. The header is removed from settings, rendering and API/domain contracts. The Noble badge preference and membership duration remain intact. Rollback recreates the field as false and does not restore old choices. This migration has not been applied to a shared database by this task.

`20260911194115_RemoveNobilityOrnaments` removes the saved ornament column. Ornaments have been removed from settings, rendering, API contracts, domain rules and the current product catalog. Apply this migration with the updated backend and frontend; it discards the obsolete cosmetic selection only. Badge/header choices, Signet ownership and Nobility duration are preserved. Its rollback recreates an empty nullable column and cannot restore discarded selections. This task has not applied the migration to a shared database.

The migration adds seven Nobility/Signet tables, preset slot columns with a creation-order backfill, and separate free/paid reroll counters backfilled from existing usage. It adds no cash-provider credentials or environment-specific settings. Deploy the updated API, Worker, LiveOps and frontends together after the schema/catalog are ready. A rollback that removes these tables destroys the new entitlement and ownership history; preserve that data when planning any rollback.

The follow-up grant migration seeds/corrects the catalog before JSON seeding, records an alpha issuance and movement per unit, and synchronizes available inventory stacks. `MiscItemBase`, the JSON converter and EF discriminator mapping fix the startup `Unknown itemType "Misc"` error and ensure Signets remain in their correct category. The follow-up migration refuses `Down`: previously distributed Signets may already have been traded or redeemed, so corrections must preserve ownership and membership history through a forward migration.

The follow-up migration and production redemption service passed PostgreSQL tests against a baselined schema. These cover missing/legacy catalog entries, earlier grants, registered and guest characters, missing inventories, non-player exclusion, replay after redemption and subsequent startup with a new character. The full earlier migration chain/backfills and simultaneous multi-process redemption/trading remain unverified. No shared database or external environment was modified.

## Later Stripe integration

`INobilityPurchaseGateway` and `DisabledNobilityPurchaseGateway` form the small purchase seam. The alpha checkout endpoint returns HTTP 503 with an unavailable result and cannot mint items or activate membership. There are no payment keys, fake successful payments or checkout UI.

A later implementation must add server-owned products/prices, idempotent checkout creation, verified payment webhooks, payment-to-issuance receipts, refunds/disputes and the permanent cash-support badge. Change the checkout query into a transactional command when it starts creating purchase state. The original plan's payment/reconciliation/reversal stages remain deferred; they are not prerequisites for granting alpha Signets.

## Verification completed on 11 September 2026

**XP and daily resource removal:** the backend build passed in `NobilityAppearance`, followed by **129 focused tests** through `build/run-tests.ps1` covering Nobility, reward calculations, Mastery, persisted-job retirement, Worker dependency resolution and state synchronization. The game development build and **16 focused Angular tests** passed. EF reports no pending model changes, and `git diff --check` passed with Windows line endings recognized. Existing compiler warnings remain. These checks used local test execution; no deployed smoke test or shared database change was performed.

**Startup fix and automatic grant follow-up:** 26 focused backend tests passed with no skips, including two disposable PostgreSQL 17 migration/redemption cases and real-catalog parsing/persistence coverage. Run through `./build/run-tests.ps1 -NoBuild -Configuration SignetVerification -Filter 'FullyQualifiedName~ItemCatalogSeedingTests|FullyQualifiedName~AlphaSignetMigrationTests|FullyQualifiedName~Nobility|FullyQualifiedName~Signets_survive'`. PostgreSQL cases require `LL_SIGNET_TEST_POSTGRES` pointing to an isolated localhost test server with database-creation rights; each case creates and drops its own random test database. They otherwise report skipped.

The backend and focused test build used an isolated `SignetVerification` configuration because Debug output was locked by the active debugger and Release output had an access error. The ordinary test-project build was blocked by unrelated, in-progress World Tower title interface/constructor changes in `GameEventOutboxTests` and `WorldTowerServiceTests`. A temporary MSBuild target outside the checkout excluded those two fixtures for this focused run; their source was left unchanged. This follow-up is not a full-suite verification.

Recorded implementation-run results: **2,341 distinct backend tests** (2,340 in the regression run plus the subsequently added market integration test), **798 game frontend tests** and **35 LiveOps frontend tests** passed. These results are historical verification evidence, not a new test run for this documentation update.

| Check | Result |
| --- | --- |
| `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --no-restore --configuration Debug --verbosity quiet` | Passed, including the referenced game API, LiveOps and Worker projects; existing warnings remain. |
| `./build/run-tests.ps1 -NoBuild -Configuration Debug -Filter 'FullyQualifiedName!~BalanceHarness'` | 2,340 passed. The additional persisted Signet market integration test was subsequently added and passed through the same wrapper with filter `FullyQualifiedName~Signets_survive`. |
| `npm.cmd run test:ci` in `LL/src/Presentation/ll` | 798 passed, including new Signet market and membership expiry tests. |
| `npm.cmd run build:development` in `LL/src/Presentation/ll` | Passed. |
| `npm.cmd test` and `npm.cmd run build` in `LL/src/Presentation/liveops` | 35 tests passed; build passed. |
| `dotnet ef migrations has-pending-model-changes --project LL/src/Infrastructure/Persistence/Persistence.LL --startup-project LL/src/API/API.LL --no-build` | No pending model changes. |
| `git -c core.whitespace=cr-at-eol diff --check` | Passed with Windows line endings recognized. |

The initial backend wrapper run with restore was blocked from reading the user NuGet configuration in the sandbox. Building with existing restored dependencies (`--no-restore`) and then running the repository wrapper with `-NoBuild` completed verification. Npm caches were kept under the system temporary directory. PostgreSQL execution/concurrency and a deployed alpha smoke test remain unrun; no deployment or shared database modification was attempted.
