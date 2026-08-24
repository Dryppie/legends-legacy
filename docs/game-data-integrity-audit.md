# Game-Data Integrity Audit

Audit result: the repository's static catalogs are generally well-formed, but three High-severity integrity problems exist:

1. All 70 Essence evolutions are unreachable because every required catalyst ID is empty.
2. Two active achievements depend on retired Hive's Abyss bosses, making Completionist unreachable for players who did not earn them historically.
3. Region 2 dungeons can be entered through the API without satisfying the Region 2 / Tower progression gate.

No Critical findings were identified. No files, data, migrations, or configuration were changed as part of the audit.

## 1. Game-Data Architecture

The content model is hybrid:

- JSON under [`LL/src/API/API.LL/Data`](../LL/src/API/API.LL/Data) is authoritative for creatures, regions, Essences, abilities, statuses, items, recipes, blueprints, dungeons, rewards, quests, achievements, titles, Tower floors, guild content, raids, events, and related catalogs.
- Runtime providers deserialize and validate JSON for combat, dungeons, crafting, events, Tower, and other systems.
- Startup seeding copies selected catalogs—especially items, creatures, regions, achievements, and titles—into PostgreSQL.
- C# enums and services define runtime semantics such as targeting, status behavior, reward application, access policies, progression, and upgrade rules.
- EF Core models and configurations define persistent player state.
- Angular duplicates some display and progression metadata, notably dungeon presentation and required levels.

Catalog snapshot:

| Category                             |  Count |
| ------------------------------------ | -----: |
| Items                                |    178 |
| Essences                             |     70 |
| Abilities                            |    180 |
| Creature ability profiles            |     85 |
| Creatures                            |     86 |
| Status definitions                   |     16 |
| Creature Essence loot tables         |     67 |
| Essence loot variants                |     70 |
| Base recipes                         |     31 |
| Blueprints                           |     13 |
| Materials                            |     19 |
| Active dungeon families/difficulties | 4 / 12 |
| Tower floors                         |     10 |
| Achievements                         |    101 |
| Titles                               |     37 |

Existing validation is strongest inside individual catalogs—duplicates, numeric ranges, and local references—and weaker across catalog boundaries, progression reachability, retired-content references, frontend parity, and database-wide player invariants.

## 2. Overall Integrity Assessment

The data is structurally sound in most ordinary relationships:

- No duplicate current catalog identities were found.
- Normal creature, ability, Essence, loot, recipe, blueprint, reward, dungeon, raid, Tower, title, and quest references resolve.
- No blocking crafting or quest dependency cycles were found.
- Region 1 onboarding and crafting progression is reachable.
- Tower floors are contiguous from 1 through the released floor 10.
- Reward quantity/probability conventions were internally consistent in the inspected catalogs.

The greatest risks are:

- Essence evolution reachability.
- Active achievements tied to retired content.
- Missing authoritative dungeon progression enforcement.
- Application-only persistent-state invariants.
- Divergent content-loading paths and stale seeded rows.
- Frontend/backend dungeon metadata drift.

## 3. Critical / High-Risk Integrity Violations

No Critical violations were found.

### Finding 1 — Every Essence evolution is impossible

**Location:** [`essences.json`](../LL/src/API/API.LL/Data/essences/essences.json), [`EssenceSystemService.cs`](../LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs), [`EssenceDefinitionValidator.cs`](../LL/src/Infrastructure/Service/Services.LL/Essences/EssenceDefinitionValidator.cs)

**Data/entities involved:** All 70 Essence definitions; nine `item.evolution_catalyst.*` item bases.

**Classification:** A — Definite Integrity Violation; C — Reachability Problem  
**Severity:** High

**Expected invariant:** An Essence evolution must either reference an obtainable catalyst item or explicitly declare that no catalyst is required.

**Actual behavior:** All 70 definitions specify `requiredAscensionTier: 2` and `requiredCatalystItemId: ""`. Evolution unconditionally tries to remove one unit of that ID and returns "Required Evolution Catalyst is missing" when removal fails.

The UI independently determines `CanEvolve` from tier and evolution status alone in [`SoulArchiveMappingProfile.cs`](../LL/src/Core/Application/UseCases/Essences/Dtos/SoulArchiveMappingProfile.cs), so it can advertise an evolution that the server will reject.

**Evidence:** Nine catalyst item bases exist beginning in [`items.json`](../LL/src/API/API.LL/Data/items/items.json), but no Essence references them.

**Consequence:** The complete evolution layer is unreachable through normal gameplay.

**Recommended correction:** Either assign a valid, obtainable catalyst to every evolution or make catalysts explicitly optional and update service/UI semantics consistently.

**Preventative validation:** Startup validation must require a non-empty, resolvable, obtainable catalyst whenever evolution consumes a catalyst. Add a real-catalog integration test that evolves at least one Essence from each catalyst family.

### Finding 2 — Retired dungeon achievements block Completionist

**Location:** [`achievements/dungeons.json`](../LL/src/API/API.LL/Data/achievements/dungeons.json), [`achievements/additional.json`](../LL/src/API/API.LL/Data/achievements/additional.json), [`JsonDungeonDefinitions.cs`](../LL/src/Infrastructure/Service/Services.LL/JsonDefinitions/JsonDungeonDefinitions.cs)

**Data/entities involved:**

- `dungeon.hive_abyss_clear` → `ant_queen`
- `dungeon.ant_king` → `ant_king`
- Titles referencing those achievements
- `legacy.completionist`

**Classification:** A, C, D  
**Severity:** High

**Expected invariant:** Every active, non-hidden achievement contributing to Completionist must have a reachable runtime producer.

**Actual behavior:** Both Hive achievements remain active and visible, while `hives_abyss`, `hives_abyss_ii`, and `hives_abyss_iii` are explicitly filtered from the runtime dungeon catalog. `SpecificDungeonBossDefeated` progress is produced only during dungeon completion in [`AchievementService.cs`](../LL/src/Infrastructure/Service/Services.LL/Achievements/AchievementService.cs).

There are 92 active non-hidden achievements other than Completionist. With two unreachable, a new player can reach at most 90/92.

**Consequence:** Two achievements and their titles are unreachable, and Completionist cannot be earned by players without historical progress.

**Recommended correction:** Deactivate, retire, grandfather, or repoint the two achievements and their titles. Preserve already-earned historical achievements deliberately.

**Preventative validation:** Resolve every active `SpecificDungeonCompleted` and `SpecificDungeonBossDefeated` target against the active runtime dungeon catalog. Add a meta-achievement reachability test.

### Finding 3 — Region 2 dungeons bypass Region 2 progression

**Location:** [`dungeons.json`](../LL/src/API/API.LL/Data/dungeons/dungeons.json), [`DungeonAccessPolicy.cs`](../LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonAccessPolicy.cs), [`StartDungeonRunCommand.cs`](../LL/src/Core/Application/UseCases/Dungeons/Commands/StartDungeonRun/StartDungeonRunCommand.cs)

**Data/entities involved:** `tangled_cave`, `great_tree`, their sigils, Region 2, Tower floor 10.

**Classification:** C, E  
**Severity:** High

**Expected invariant:** Region 2 dungeon access should require the same authoritative progression milestone as Region 2 world content.

**Actual behavior:**

- Region 2 areas require Tower floor 10 in [`regions.json`](../LL/src/API/API.LL/Data/world/regions.json).
- Dungeon definitions support `RequiredAreaId` and `RequiredQuestId` in [`DungeonCatalogDocument.cs`](../LL/src/Infrastructure/Service/Services.LL/JsonDefinitions/Dungeons/DungeonCatalogDocument.cs).
- The materializer copies those fields, but `DungeonAccessPolicy` checks only entry items and the previous difficulty.
- `tangled_cave` and `great_tree` define no area, quest, character-level, or Tower requirement.
- Ten fragments can assemble a sigil, and fragment sources exist independently of Region 2.

**Consequence:** A low-progression character obtaining fragments can assemble a Region 2 sigil and call the start-dungeon command directly, bypassing the world gate.

**Recommended correction:** Add authoritative dungeon requirements—preferably Tower floor/region progression—and enforce them in preview, sigil assembly, and run start.

**Preventative validation:** An integration test should call the command directly with a low-level, pre-floor-10 character and verify denial. UI and API should consume the same returned access metadata.

## 4. Broken Reference Matrix

| Source                   | Reference                | Expected target                |              Broken/missing |        Orphans or detached content |
| ------------------------ | ------------------------ | ------------------------------ | --------------------------: | ---------------------------------: |
| Region/area              | Creature ID              | Creature catalog               |                           0 |      1 explicitly pending creature |
| Creature profile         | Creature and ability IDs | Creature/ability catalogs      |                           0 |                                  0 |
| Essence                  | Active/passive ability   | Ability catalog                |                           0 |                                  0 |
| Essence evolution        | Catalyst item            | Item catalog                   |  **70 empty required refs** |        9 catalyst items unattached |
| Essence item             | `essenceDefinitionId`    | Essence catalog                | 0 at runtime via convention |   **53 omitted explicit mappings** |
| Creature loot            | Creature/Essence/ability | Corresponding catalogs         |                           0 |       None classified as erroneous |
| Recipe                   | Material/output item     | Material/item catalogs         |                           0 |               0 required resources |
| Blueprint                | Recipe/material/item     | Crafting/item catalogs         |                           0 |                                  0 |
| Active dungeon           | Encounters/rewards/items | Creature/reward/item catalogs  |                           0 |  3 retired definitions plus debris |
| Achievement              | Active dungeon boss      | Active runtime dungeon catalog |                       **2** |       2 titles inherit the problem |
| Title                    | Achievement              | Achievement catalog            |                           0 |                                  0 |
| Tower floor              | Creatures/rewards        | Creature/reward catalogs       |                           0 | 6 unlock keys lack clear consumers |
| Guild/event/raid rewards | Item/currency            | Item/currency registries       |                           0 |                0 confirmed invalid |

"Broken" here includes required-but-empty references and targets filtered from the active runtime catalog, not merely syntactically missing IDs.

## 5. Broken / Missing References

Beyond the three High findings:

### Missing explicit Essence item mappings

**Classification:** B, E  
**Severity:** Medium

Of 70 Essence item bases, only 17 set `essenceDefinitionId`; 53 rely on the `item.{essenceId}` naming convention.

Absorption succeeds because [`EssenceSystemService.cs`](../LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs) infers the ID. However, [`ItemBaseRepository.cs`](../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Items/ItemBaseRepository.cs) excludes blank mappings, causing the Essence catalog to return a null item ID and content diagnostics to report the item base as unresolved.

**Correction:** Populate the mappings or apply the convention fallback centrally in the item repository/catalog.

**Prevention:** Require a unique explicit mapping for every Essence item, or formally validate the naming convention.

No other current missing item, recipe, material, ability, status-effect target, summon, reward-table, raid, Tower, title, or active dungeon encounter references were found.

## 6. Progression Reachability

Major dependency map:

```text
Training Day
  ↓ choose starter Essence
Soul Archive
  ↓ absorb/equip Essence; grants initial ore/timber
First Weapon
  ↓ craft starter weapon
Tools of the Trade
  ↓
Region 1 quest chain
  ↓ sequential area unlocks
Region 1 dungeons / gathering / crafting
  ↓
World Tower floors 1–10
  ↓
Region 2 areas
```

Problematic branches:

```text
Essence reaches Ascension Tier 2
  ↓
Evolution requires empty catalyst ID
  ✕ dead end
```

```text
Any source of 10 Sigil Fragments
  ↓
Assemble Tangled Cave / Great Tree sigil
  ↓
Dungeon access checks item + previous difficulty only
  ↓
Region 2 dungeon available before Tower floor 10
```

```text
Completionist
  ↓ requires all 92 other active non-hidden achievements
Hivebreaker + Royal Exterminator
  ↓ require bosses from filtered retired dungeon
  ✕ dead end
```

No blocking cycles were found in:

- Quest prerequisites.
- Active dungeon difficulty chains.
- Crafting inputs/outputs.
- Blueprint material dependencies.
- Region 1 area unlock ordering.
- Tower floor ordering.

## 7. Persistent Player-State Invariants

| Invariant                                          | Application                   | Database                                   | Assessment |
| -------------------------------------------------- | ----------------------------- | ------------------------------------------ | ---------- |
| Currency balances ≥ 0                              | Path-specific checks          | No check constraints                       | E, Medium  |
| Inventory quantity > 0                             | Services remove depleted rows | No constraint                              | E, Medium  |
| One owned Essence per character/definition         | Yes                           | Unique index                               | Both       |
| Essence level/XP/tier/evolution consistency        | Yes on normal paths           | No range/consistency constraints           | E, Medium  |
| Unique slot and Essence within a loadout           | Yes                           | Unique indexes and slot range              | Both       |
| Equipped/loadout Essence belongs to same character | Yes on save                   | Not relationally enforced                  | E, Medium  |
| Character belongs to at most one guild             | Check before insert           | Only `{GuildId, CharacterId}` key          | E, Medium  |
| PvP tickets remain 0–5                             | Service logic                 | No check constraint                        | E, Medium  |
| One active dungeon run per character               | Yes                           | Unique character index + concurrency token | Both       |
| Equipment slot compatibility/ownership             | Service logic                 | Slot key only                              | E, Medium  |
| Reward claim uniqueness                            | Yes                           | Generally unique/composite indexes         | Both       |
| Tower first-clear/rally participant uniqueness     | Yes                           | Strong unique indexes                      | Both       |

### Guild membership race

[`GuildMemberConfiguration.cs`](../LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Guilds/GuildMemberConfiguration.cs) allows the same character in multiple guilds because its primary key includes `GuildId`. The repository performs check-then-insert in [`GuildRepository.cs`](../LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Guilds/GuildRepository.cs). Two concurrent joins to different guilds can both pass.

A duplicate then conflicts with `SingleOrDefaultAsync` when retrieving the character's guild.

**Recommended correction:** Add a unique index on `CharacterId`, preflight existing data, and translate uniqueness failures to a domain result.

### Cross-character Essence loadouts

[`EssenceLoadoutSlotConfiguration.cs`](../LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Essences/EssenceLoadoutSlotConfiguration.cs) validates slot uniqueness but cannot ensure `EssenceLoadout.CharacterId == PlayerEssence.CharacterId`.

Normal saves check ownership, but imports, direct database changes, or another faulty writer could equip another character's Essence.

### Equipment assignment

[`EquipmentSlotConfiguration.cs`](../LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/EquipmentSlots/EquipmentSlotConfiguration.cs) only guarantees one row per entity/slot. It does not enforce item ownership or slot/type compatibility. A simple unique index on `EquipmentInstanceId` is insufficient because two-handed weapons intentionally occupy two slots; this requires a handedness-aware invariant.

## 8. Creature / Essence / Ability Integrity

Positive results:

- All 70 Essences resolve active and passive abilities.
- All 180 abilities are referenced.
- Creature behavior profiles resolve their creatures, abilities, summons, and produced status effects.
- All 70 Essence variants have loot definitions and conventional item IDs.
- Numeric cooldown, duration, quantity, weight, and chance checks found no definite malformed values.

Findings:

- Essence evolution is globally blocked, as detailed above.
- Fifty-three Essence item mappings are implicit and cause incomplete catalog/diagnostic output.
- `monster.venomous_spider` is the only completely unplaced creature. It is explicitly listed as pending implementation in [`RegionOneIdleAreaSeedTests.cs`](../LL/tests/EssenceSystem.Tests/RegionOneIdleAreaSeedTests.cs), so this is future content, not corruption.
- Five registered statuses have no current JSON effect producer: `status.curse`, `status.shadow_image`, `status.transparent_dodge_boost`, `status.illusion_fox.accuracy_debuff`, and `status.necrotic_spore_debuff`. These are classified D/F, Low; some have C#/test evidence or appear prepared for future content.

## 9. Crafting / Equipment / Gathering Integrity

The crafting catalogs are internally consistent:

- All 31 recipe outputs exist.
- All recipe material IDs resolve.
- All quantities are positive.
- All 13 blueprints resolve their recipes, materials, output items, and source content.
- No self-producing recipe or acquisition deadlock was found.
- Ordinary crafting materials have gathering, dungeon, raid, vendor, or progression sources as appropriate.
- Initial quest rewards are sufficient for the starter weapon path.

Equipment's primary risk is persistent cross-system integrity, not static definitions: database rows can describe ownership or slot combinations that only application code currently prevents.

The nine `item.evolution_catalyst.*` items have no authored acquisition source and no Essence consumes them. This is part of the global evolution finding rather than a crafting balance issue.

## 10. Dungeon / Progression / World Content Integrity

- Four active dungeon families each provide three complete difficulties.
- Active encounter, reward-table, sigil, item, and gathering-node references resolve.
- Previous-difficulty ordering is valid.
- Region 1 has a coherent ten-area quest progression.
- Region 2 areas correctly require Tower floor 10.
- Tower floors are contiguous from 1–10, matching `releasedThroughFloor: 10` in [`tower-floors.json`](../LL/src/API/API.LL/Data/world-tower/tower-floors.json).

Medium finding: several Tower unlock keys appear write-only or inert. `tower_scouting_unlock`, `tower_battle_report_unlock`, `tower_preparation_unlock`, `region_expansion_1`, and band/shop unlock keys are stored generically, while only `tower_echo_mode_unlock` has a clear consumer in [`WorldTowerService.cs`](../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs).

If the keys are intended as gates, mechanics are available without them. If informational/future, the schema does not distinguish that intent.

**Classification:** B/E  
**Severity:** Medium

## 11. Reward / Currency / Resource Integrity

| Resource            | Confirmed sources                           | Confirmed sinks                                    | Concern                                        |
| ------------------- | ------------------------------------------- | -------------------------------------------------- | ---------------------------------------------- |
| Cinders             | Combat, prophecies, PvP, guild/shop rewards | Player marketplace purchases                       | Valid                                          |
| Soulstones          | Combat, guild, prophecy, PvP rewards        | Soulstone upgrades                                 | Valid                                          |
| Fate Echo           | Prophecy/reward grants                      | Prophecy rerolls                                   | Valid                                          |
| Sigil Fragments     | Events, guild rewards, prophecies, PvP      | Dungeon sigil assembly                             | Source and sink valid; enables Region 2 bypass |
| Guild Favor         | Guild missions/orders                       | Guild shop                                         | Valid                                          |
| Tower Tokens        | Tower first clear and Echo clears           | **No gameplay sink found**                         | D/F, Low                                       |
| Raid Trophies       | Raid reward claims                          | Raid trophy vendor                                 | Valid                                          |
| Soul Dust           | Essence dissolution                         | Essence leveling                                   | Valid                                          |
| Evolution catalysts | No authored source                          | Intended evolution, but no Essence references them | A/C, High                                      |
| Crafting materials  | Gathering/dungeons/raids/vendors            | Recipes and blueprints                             | Valid                                          |

Tower Tokens are granted in [`WorldTowerService.cs`](../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs) and displayed by the UI, but no decrement or purchase consumer exists. This may be an intentionally accumulated future currency, so it is not classified as a definite violation.

Persistent currency fields are plain `long` values in [`Character.cs`](../LL/src/Core/Domain/Models/Entities/Characters/Character.cs) without database non-negative constraints.

## 12. Frontend / Backend Content Mismatches

The Angular dungeon page hardcodes presentation for only:

- `goblin_mines`
- `forgotten_catacombs`
- retired `hives_abyss`

See [`dungeons.component.ts`](../LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeons.component.ts).

It has no presentation entry for `tangled_cave` or `great_tree`, and defaults missing `requiredLevel` to 1. The backend [`DungeonPreviewDto.cs`](../LL/src/Core/Application/UseCases/Dungeons/Dtos/DungeonPreviewDto.cs) does not expose an authoritative required level or Tower requirement.

**Classification:** B/E  
**Severity:** Medium

**Consequence:** Region 2 cards can show level 1 and incomplete lore, while the backend has no corresponding gate at all.

**Correction:** Return display/access metadata from the backend and remove frontend-authored progression requirements. Add an API-to-UI contract test for every active dungeon family.

## 13. Legacy / Orphaned Content

### High-confidence legacy

Hive's Abyss remains in several current-looking catalogs despite being removed from the runtime dungeon list:

- Retired filter: [`JsonDungeonDefinitions.cs`](../LL/src/Infrastructure/Service/Services.LL/JsonDefinitions/JsonDungeonDefinitions.cs)
- Waypoint delve: [`dungeon-delves.json`](../LL/src/API/API.LL/Data/dungeons/dungeon-delves.json)
- Completion reward tables: [`reward-tables.json`](../LL/src/API/API.LL/Data/rewards/reward-tables.json)
- Sigil item: [`items.json`](../LL/src/API/API.LL/Data/items/items.json)
- Frontend presentation: [`dungeons.component.ts`](../LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeons.component.ts)
- Active achievements and titles.

The residual data is not automatically dangerous, but the active achievement references demonstrate that the current validators do not distinguish "known legacy" from active content.

### Possibly future content

- `monster.venomous_spider`, explicitly acknowledged by tests.
- Five unused status definitions.
- Tower Tokens without a sink.
- Several Tower feature unlock keys without consumers.

These should remain until intent is established.

### Seeded stale-content risk

[`DbJsonSeeder.cs`](../LL/src/Infrastructure/Persistence/Persistence.LL/Seeds/JsonSeeding/DbJsonSeeder.cs) upserts current items but does not reconcile removed top-level item rows. Creature seeding likewise queries only desired IDs in [`SeedCreatures.cs`](../LL/src/Infrastructure/Persistence/Persistence.LL/Seeds/Seeding/SeedCreatures.cs).

Removed static IDs can therefore remain in production indefinitely. Automatic deletion would be unsafe because player history may reference them; explicit retirement or reconciliation reporting is preferable.

## 14. Missing Validation

| Invariant                                                            | Current protection                                   | Possible failure                                  | Best validation location                            |
| -------------------------------------------------------------------- | ---------------------------------------------------- | ------------------------------------------------- | --------------------------------------------------- |
| Evolution catalyst is non-empty, defined, and obtainable             | Tier range only                                      | All evolution unreachable                         | Startup validator + integration test                |
| Active achievement target is runtime-reachable                       | Enum/type validation                                 | Active achievements target retired content        | Startup cross-catalog validator                     |
| Dungeon access matches region progression                            | Entry item and prior difficulty                      | Direct API progression bypass                     | Application policy + integration test               |
| Every Tower unlock key has a consumer or explicit future designation | None obvious                                         | Inert gates or accidentally ungated features      | Startup registry validation                         |
| Essence item maps explicitly to one Essence                          | Naming fallback on one path                          | Null catalog item IDs and conflicting diagnostics | Startup validation/DB uniqueness                    |
| One guild per character                                              | Check-then-insert                                    | Concurrent duplicate memberships                  | DB unique index                                     |
| Character balances cannot be negative                                | Individual service checks                            | Invalid state from alternate writers/imports      | DB check constraints                                |
| Inventory quantities stay positive                                   | Cleanup in normal services                           | Zero/negative stored stack                        | DB constraint or normalized zero-delete interceptor |
| Loadout Essence belongs to loadout character                         | Save-service query                                   | Cross-character power/equipment state             | Relational redesign or save interceptor             |
| Equipment assignment is compatible and owned                         | Equip service                                        | Impossible slot/owner combinations                | Domain application validation + audit query         |
| Static seeders use the same content root                             | Runtime providers use `Content:Root`; seeders do not | Different DB/runtime catalogs by environment      | Shared content-path service                         |
| Removed source IDs are explicitly retired                            | Upsert only                                          | Stale DB catalogs                                 | Startup reconciliation report                       |
| Dungeon UI metadata matches active backend families                  | Manual TypeScript record                             | Retired/missing/defaulted cards                   | Contract test                                       |
| Sigil drop pool references valid current areas/items                 | Local syntax checks                                  | Typo introduced silently                          | Startup cross-catalog validator                     |

## 15. Recommended Automated Integrity Checks

Prioritized checks to implement later:

1. Validate all required references across catalogs, including required-but-empty strings.
2. Validate every Essence evolution catalyst exists, has a source, and can be consumed in a real integration test.
3. Validate achievement targets against active runtime producers, not merely enum-valid requirement types.
4. Execute a progression reachability test covering onboarding, areas, dungeons, Tower gates, and meta-achievements.
5. Assert direct command/API dungeon access cannot bypass region/Tower requirements.
6. Validate all item, reward, recipe, blueprint, gathering, raid, and dungeon references in one composed catalog.
7. Add a retired-content manifest and prohibit active content from referencing retired IDs.
8. Compare runtime JSON catalogs against seeded database catalogs and report stale/missing rows.
9. Verify every frontend dungeon family comes from the API and no retired family is hardcoded.
10. Add database checks for non-negative currencies, positive inventory/reward quantities, Essence ranges, and PvP tickets.
11. Add database uniqueness for one guild membership per character.
12. Add cross-character ownership audit queries for Essence loadouts and equipment.
13. Require each Tower unlock key to be registered as consumed, informational, or future.
14. Add source/sink registry tests for currencies and progression materials.
15. Run validators against the deployed `Content:Root`, not a separate hardcoded path.

## 16. Things Investigated That Are Fine

The following looked suspicious but are legitimate or adequately explained:

- No duplicate current catalog identifiers were found.
- Repeated quest IDs across files are versioned quest definitions; the provider selects the current version.
- Non-contiguous area IDs are persistent identifiers, not the progression order. Explicit ordering and quest gates control progression.
- All active dungeon difficulty sequences are complete.
- Tower floors 1–10 are contiguous and match the released floor.
- Every active ability is referenced.
- Every current Essence loot variant resolves to an Essence, ability pair, and conventional item.
- Initial onboarding resources can produce the required starter weapon.
- Ordinary crafting resources have acquisition paths.
- Expired or future event JSON is filtered by event dates rather than assumed active merely because the file exists.
- `monster.venomous_spider` is explicitly documented by tests as pending.
- Duplicate display-style concepts were not treated as identity collisions.
- Existing reward-claim, active-dungeon, Tower first-clear, loadout-slot, and owned-Essence uniqueness constraints are comparatively strong.

## 17. Recommended Cleanup / Hardening Order

1. Repair Essence evolution semantics and add the catalyst validator.
2. Retire/repoint the two Hive achievements and decide how historical unlocks affect Completionist.
3. Add authoritative Region 2 dungeon access gates and direct-command tests.
4. Add the guild membership unique constraint after auditing existing memberships.
5. Populate or normalize the 53 missing Essence item mappings.
6. Add persistent range/ownership constraints in small, preflighted migrations.
7. Unify startup seeding and runtime providers behind one `Content:Root`.
8. Add a non-destructive stale-catalog reconciliation report.
9. Move dungeon presentation/access metadata to the backend.
10. Introduce the retired-content and Tower-unlock registries.
11. Review Tower Tokens and unused statuses with explicit future/legacy decisions.
12. Consolidate cross-catalog checks into a deployment/startup integrity suite.

### Verification performed

- `build/run-tests.ps1`: **1,455 passed, 0 failed, 0 skipped**; build completed with 73 warnings.
- Angular `ng test --watch=false --browsers=ChromeHeadless`: **551 passed, 1 failed**.
- The frontend failure is in [`equipment-display.spec.ts`](../LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-display.spec.ts), where a tooltip DOM-text assertion expects whitespace between labels and values. It is unrelated to this analysis-only audit.
- No required verification remains unrun.
- Audit-time changed files: **none**.
- Migrations/configuration/deployment: **none created or applied**. Future DB constraints will require production-data preflight and carefully reviewed migrations.
