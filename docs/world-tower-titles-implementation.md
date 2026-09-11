# World Tower boss titles

Implemented 2026-09-11 in the primary LL game service.

## Behavior

- Floors 1–15 award Gatekeeper, Bloodwing Huntress, Broodkeeper, Mirrorbound, First Warden, Ashen Bellkeeper, Endless Spring, Poisoned Vessel, Ninefold, Mad King, Name-Eater, Shackled Storm, Moondrowned, Smith of the Fallen Star, and Second Warden respectively.
- Every character in the finalized winning roster earns the floor title, including defeated characters and support characters. Spectators, applicants, and departed members are excluded.
- First Clear and Echo victories qualify independently of token rewards. Titles are permanent, character-bound, and never automatically equipped.
- Grants and personal title chat messages are saved with the victory transaction. Character locks, existing unlock checks, and finalized-attempt guards prevent duplicate grants.
- Floor details show the title reward. The Titles tab omits locked World Tower titles entirely and shows each title only after the current character earns it. Other title categories keep their existing visibility. Existing title browsing and equipping are reused.
- Historical successful attempts are reconciled at API startup, after catalog seeding, in transactions reading at most 100 participant records. The backfill ignores other servers, failed or unfinished attempts, applicants, and missing characters. It sends no title or dependent-achievement announcements. It is safe to rerun; a failed run is logged and retried on the next startup.
- Sovereign replay rules are unchanged: Floor 10 has no Echo mode, so its title is limited to successful First Clear participants.

## Design

Floor JSON references stable title keys; display names live in the title catalog. Catalog loading validates unique floor keys, and seeding validates active, unique character titles for released floors. Title rarities are Renowned for standard floors, Exalted for Wardens, and Legendary for the Sovereign.

New persistence queries and locks live in a dedicated repository. The title service reuses AchievementService.UnlockTitleAsync, including dependent title-count achievements. An optional announcement flag allows quiet repair. Existing Tower chat payloads gain an optional character recipient; omitted recipients retain the previous global behavior.

## Changed files

Paths are relative to the repository root.

- Catalog and startup: `LL/src/API/API.LL/Data/titles/world-tower.json`, `LL/src/API/API.LL/Data/world-tower/tower-floors.json`, `LL/src/API/API.LL/Program.cs`, `LL/src/API/API.LL/HostedServices/WorldTowerTitleBackfillWorker.cs`.
- Domain: `LL/src/Core/Domain/Models/WorldTower/WorldTowerDefinitions.cs`, `LL/src/Core/Domain/Models/WorldTower/IWorldTowerTitleRepository.cs`.
- Application: `LL/src/Core/Application/Interfaces/Services/LL/WorldTower/IWorldTowerTitleService.cs`, `LL/src/Core/Application/Interfaces/Services/LL/Achievements/IAchievementService.cs`, `LL/src/Core/Application/UseCases/WorldTower/Commands/BackfillWorldTowerTitles/BackfillWorldTowerTitlesCommand.cs`, `LL/src/Core/Application/UseCases/WorldTower/Dtos/WorldTowerDtos.cs`, `LL/src/Core/Application/UseCases/Outbox/GameEventPayloads.cs`.
- Persistence: `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/WorldTower/WorldTowerTitleRepository.cs`, `LL/src/Infrastructure/Persistence/Persistence.LL/Seeds/AchievementTitleSeedData.cs`, `LL/src/Infrastructure/Persistence/Persistence.LL/DependencyInjection.cs`.
- Services: `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerTitleService.cs`, `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs`, `LL/src/Infrastructure/Service/Services.LL/WorldTower/JsonWorldTowerDefinitionProvider.cs`, `LL/src/Infrastructure/Service/Services.LL/Achievements/AchievementService.cs`, `LL/src/Infrastructure/Service/Services.LL/Outbox/WorldTowerChatGameEventOutboxConsumer.cs`, `LL/src/Infrastructure/Service/Services.LL/DependencyInjection.cs`.
- Frontend: `LL/src/Presentation/ll/src/app/core/services/api/world-tower/world-tower.service.ts`, `LL/src/Presentation/ll/src/app/features/game/world/tower/overview/tower-overview.component.html`.
- Tests: `LL/tests/EssenceSystem.Tests/WorldTowerTitleTests.cs`, `LL/tests/EssenceSystem.Tests/WorldTowerTitleCatalogTests.cs`, `LL/tests/EssenceSystem.Tests/WorldTowerTitleVisibilityTests.cs`, `LL/tests/EssenceSystem.Tests/WorldTowerServiceTests.cs`, `LL/tests/EssenceSystem.Tests/GameEventOutboxTests.cs`.
- This implementation note.

Existing unrelated checkout edits, including Tower balance adjustments, were preserved.

## Verification

- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~WorldTower|FullyQualifiedName~AchievementServiceTests|FullyQualifiedName~GameEventOutboxTests'`: 112 passed, zero failed/skipped. Builds the test project and referenced backend projects. Initial sandbox attempt could not read NuGet.Config; the permitted retry succeeded.
- `npm.cmd run build:development` from `LL/src/Presentation/ll`: passed; npm cache was outside the checkout under TEMP.
- `git diff --check`: passed.
- No live browser or PostgreSQL concurrency test was run. Repository tests use EF Core InMemory. The full unrelated backend suite and production Angular build were not run.

## Release implications

No new migration, database schema, environment setting, secret, or infrastructure change is required. Deploy the updated backend and title/floor catalog together; API startup seeds definitions before the backfill runs. Personal notifications use the existing configured System chat service and durable outbox.

No service was launched against a database, no migrations were applied, and nothing was deployed. Historical grants take effect only when the updated API is started in the target environment. Existing automatic API migration behavior was not modified.

### Title visibility follow-up

Changed AchievementService.GetTitlesAsync to exclude locked WorldTower entries before filtering and projection. Added WorldTowerTitleVisibilityTests.cs covering a 100-floor catalog, an earned title, locked-category filtering, another character on the same account, and equipping. Updated this note.

Verification: `./build/run-tests.ps1 -Filter 'FullyQualifiedName~AchievementServiceTests|FullyQualifiedName~WorldTower'` passed all 98 tests. The sandbox NuGet.Config restriction required a permitted retry. `git diff --check` passed. No frontend change or additional frontend build was needed for this backend list filter. No migration or configuration change; takes effect with the updated API, with no deployment performed here.

### Personal notification sender follow-up

Updated WorldTowerChatGameEventOutboxConsumer.cs so personal title unlocks use the System sender and global Tower announcements retain World, matching the achievement publisher. Extended WorldTowerTitleCatalogTests.cs to verify both labels and preserved recipient routing.

Verification: `./build/run-tests.ps1 -Filter 'FullyQualifiedName~WorldTowerTitleCatalogTests'` passed all 3 tests; `git diff --check` passed. No frontend build was needed. No migration or configuration change, and no deployment was performed. Previously stored chat messages retain their original sender label.
