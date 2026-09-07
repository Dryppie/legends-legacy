# Equipment loadouts and inventory quality-of-life changes

## Behavior

- Characters can save up to three named equipment loadouts in the inventory. Selecting a loadout equips it and updates the equipped-items pane; successful equip and unequip actions then automatically save back to that selected loadout. Create a loadout from current equipment, rename it, or delete it. Equipment is referenced, never copied.
- Selection stays active while navigating in the current session. On a fresh page load, a saved set matching current equipment is selected without changing gear. Switching waits for pending equipment saves, and failed saves offer a retry before switching so changes cannot be written to another set. The loadout footer uses the equipped-items pane instead of a duplicate slot preview.
- Equipment loadouts support the same seven automatic combat activities as Essence loadouts. Assigning an activity removes its assignment from other equipment loadouts. Without an assignment, combat uses currently equipped gear.
- Applying a loadout validates availability and ownership first, restores empty slots, and handles two-handed weapons without duplicating inventory items. Missing, dismantled, transferred, or recalled equipment prevents manual application. Automatic use falls back to currently equipped gear if the saved set is unavailable. Existing combat snapshots keep their frozen equipment.
- Sigil Fragments are the stackable, bound resource `sigil_fragment`. Guild shops, Champion's Market, tournaments, event quests, and prophecies grant inventory items. Dungeon sigil assembly consumes those items. Existing reward content fields remain supported, but characters no longer store a fragment currency balance.
- The Bazaar's equipment selling list shares inventory's sorting and current sort direction. Both start with Gear Power descending and break ties by name.
- Essence Max upgrading sends one dust-spending request, limited by owned dust and the current ascension level cap. For example, 25 dust upgrades a level 30 essence to 55; 50 dust upgrades it to 60 and leaves 20 dust.
- The Roots Remember now says “Complete Goblin Mines and defeat its boss”. Its explicit dungeon-family filter accepts all Goblin Mines difficulties while other objectives can retain exact difficulty filters.

## Changed areas

- Core: equipment loadout models, availability rules, repository/service contracts, CQRS requests, mappings, state synchronization, and removal of the character fragment field.
- Persistence: loadout tables and repositories, inventory fragment grants/spending, character/support reads, and activity-aware equipment snapshots.
- Services: loadout management and combat selection; fragment reward producers.
- Angular: inventory loadout controls, shared equipment sorting, Bazaar sort controls, Max upgrading, and updated character contracts.
- Content: the fragment item definition and Roots Remember wording.
- Tests: loadout ownership, limits, switching and combat snapshots; fragment grants/spending; bulk dust spending; sorting and client state refresh.

## Migration and release implications

`20260907054511_AddEquipmentLoadoutsAndInventorySigilFragments` creates the preset tables and converts positive existing fragment balances into inventory stacks before dropping `Entities.SigilFragments`. It creates missing inventories when necessary. Balances exceeding inventory's integer quantity capacity cause a transactional error rather than truncation. Downgrade sums remaining fragment stacks back into the character balance.

The API and worker must use the matching schema and item content when this change is released. No environment settings were added. The migration was generated and its SQL script checked; it was not applied to any database, and no services were deployed.

## Verification

- Backend: `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Debug --no-restore`, then `build/run-tests.ps1 -NoBuild -Configuration Debug` with filters covering equipment, combat, essence, quest, reward, state-sync, and support tests (434 passed).
- Frontend: targeted `npm run test:ci` tests (43 passed) and `npm run build:development`.
- EF: `migrations has-pending-model-changes` and generation of the migration SQL script.
- `git diff --check`.

The default test build could not access the user's NuGet configuration and existing Release output directory in this sandbox. The Debug build used already-restored dependencies instead.
