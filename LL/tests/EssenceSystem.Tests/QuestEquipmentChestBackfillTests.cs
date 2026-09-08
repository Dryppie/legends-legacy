using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Economy;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Quests;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Persistence.LL;
using Persistence.LL.Migrations;

namespace EssenceSystem.Tests;

public sealed class QuestEquipmentChestBackfillTests
{
    [Fact]
    public async Task Backfill_delivers_only_missing_chests_and_cannot_replay_after_consumption_when_postgres_configured()
    {
        var connectionString = Environment.GetEnvironmentVariable("LL_TEST_QUEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var schema = $"ll_quest_chests_{Guid.NewGuid():N}";
        var createSchemaSql = $"CREATE SCHEMA \"{schema}\"";
        var dropSchemaSql = $"DROP SCHEMA \"{schema}\" CASCADE";
        await using var admin = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql(connectionString).Options);
        await admin.Database.ExecuteSqlRawAsync(createSchemaSql);
        try
        {
            var isolated = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema };
            await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
                .UseNpgsql(isolated.ConnectionString).Options);
            await db.Database.OpenConnectionAsync();
            await using (var createTables = db.Database.GetDbConnection().CreateCommand())
            {
                createTables.CommandText = db.Database.GenerateCreateScript();
                await createTables.ExecuteNonQueryAsync();
            }

            var both = AddCharacter(db, "Both");
            AddProgress(db, both, QuestConstants.IntoLumoRuins, QuestStatus.Completed, 1);
            AddProgress(db, both, QuestConstants.BloodInTheGrove, QuestStatus.Completed, 3);
            var lumoOnly = AddCharacter(db, "Lumo");
            AddProgress(db, lumoOnly, QuestConstants.IntoLumoRuins, QuestStatus.Completed, 2);
            AddProgress(db, lumoOnly, QuestConstants.BloodInTheGrove, QuestStatus.Active, 4);
            var unrelated = AddCharacter(db, "Unrelated");
            AddProgress(db, unrelated, QuestConstants.TrialOfLumo, QuestStatus.Completed, 4);
            var alreadyRewarded = AddCharacter(db, "Rewarded");
            AddProgress(db, alreadyRewarded, QuestConstants.IntoLumoRuins, QuestStatus.Completed, 2);
            // A normal turn-in was already delivered and consumed before migration/replay.
            db.EconomyLedger.Add(new EconomyLedgerEntry
            {
                EventType = EconomyEventType.ItemAcquisition,
                AssetType = EconomyAssetType.Item,
                RecipientCharacterId = alreadyRewarded.Id,
                AssetId = RandomEquipmentBoxCatalog.ArmorChestItemBaseId,
                AssetName = "Armor Chest",
                Quantity = 1,
                Source = "quest-reward"
            });
            var token = new ItemBase
            {
                Id = "item.essence_token.blood_grove", Name = "Existing token", ItemType = ItemType.Resource
            };
            var tokenInstance = new ItemInstance { Id = Guid.NewGuid(), ItemBase = token, ItemBaseId = token.Id };
            db.InventoryItems.Add(new InventoryItem
            {
                InventoryId = both.Id, ItemInstance = tokenInstance, ItemInstanceId = tokenInstance.Id, Quantity = 1
            });
            await db.SaveChangesAsync();
            var originalProgress = await db.CharacterQuestProgresses.AsNoTracking().ToListAsync();

            await ApplyBackfillAsync(db);
            db.ChangeTracker.Clear();
            var inventory = await db.InventoryItems.Include(item => item.ItemInstance).ToListAsync();
            Assert.Equal(4, inventory.Count); // Three new chests and the existing Essence Token.
            AssertChest(inventory, both.Id, RandomEquipmentBoxCatalog.ArmorChestItemBaseId);
            AssertChest(inventory, both.Id, RandomEquipmentBoxCatalog.JewelryChestItemBaseId);
            AssertChest(inventory, lumoOnly.Id, RandomEquipmentBoxCatalog.ArmorChestItemBaseId);
            Assert.Equal(1, Assert.Single(inventory, item => item.ItemInstanceId == tokenInstance.Id).Quantity);
            Assert.DoesNotContain(inventory, item => item.InventoryId == unrelated.Id || item.InventoryId == alreadyRewarded.Id);
            var chestBases = await db.ItemBases.Where(item => item.Id == RandomEquipmentBoxCatalog.ArmorChestItemBaseId
                || item.Id == RandomEquipmentBoxCatalog.JewelryChestItemBaseId).ToListAsync();
            Assert.Equal(2, chestBases.Count);
            Assert.All(chestBases, item => Assert.True(item.IsBound && item.Stackable));
            Assert.Equal(3, await db.EconomyLedger.CountAsync(entry => entry.Source == "quest-reward:equipment-chest-backfill"));

            await ApplyBackfillAsync(db);
            Assert.Equal(4, await db.InventoryItems.CountAsync());
            var consumed = Assert.Single(inventory, item => item.InventoryId == both.Id
                && item.ItemInstance.ItemBaseId == RandomEquipmentBoxCatalog.ArmorChestItemBaseId);
            db.InventoryItems.Remove(consumed);
            db.ItemInstances.Remove(consumed.ItemInstance);
            await db.SaveChangesAsync();
            await ApplyBackfillAsync(db);
            Assert.Equal(3, await db.InventoryItems.CountAsync());
            Assert.Equal(3, await db.EconomyLedger.CountAsync(entry => entry.Source == "quest-reward:equipment-chest-backfill"));
            Assert.False(await db.InventoryItems.AnyAsync(item => item.ItemInstanceId == consumed.ItemInstanceId));

            var finalProgress = await db.CharacterQuestProgresses.AsNoTracking().ToListAsync();
            Assert.Equal(originalProgress.Count, finalProgress.Count);
            foreach (var original in originalProgress)
            {
                var current = Assert.Single(finalProgress,
                    progress => progress.CharacterId == original.CharacterId && progress.QuestId == original.QuestId);
                Assert.Equal(original.Status, current.Status);
                Assert.Equal(original.DefinitionVersion, current.DefinitionVersion);
                Assert.Equal(original.RewardsGrantedAt, current.RewardsGrantedAt);
                Assert.Equal(original.CompletedAt, current.CompletedAt);
            }
        }
        finally
        {
            await admin.Database.ExecuteSqlRawAsync(dropSchemaSql);
        }
    }

    private static async Task ApplyBackfillAsync(LLDbContext db)
    {
        var migration = new AddEarlyQuestEquipmentChests();
        var commands = db.GetService<IMigrationsSqlGenerator>().Generate(
            migration.UpOperations, db.GetService<IDesignTimeModel>().Model);
        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var command in commands)
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await transaction.CommitAsync();
    }

    private static void AssertChest(IEnumerable<InventoryItem> inventory, Guid owner, string itemId) =>
        Assert.Equal(1, Assert.Single(inventory,
            item => item.InventoryId == owner && item.ItemInstance.ItemBaseId == itemId).Quantity);

    private static Character AddCharacter(LLDbContext db, string name)
    {
        var user = new AppUser { Username = name, IsGuest = false };
        var character = new Character { Id = Guid.NewGuid(), UserId = user.Id, Name = name };
        character.NormalizeName();
        db.Users.Add(user);
        db.Characters.Add(character);
        db.Inventories.Add(new Inventory { CharacterId = character.Id });
        return character;
    }

    private static void AddProgress(LLDbContext db, Character character, string questId, QuestStatus status, int version) =>
        db.CharacterQuestProgresses.Add(new CharacterQuestProgress
        {
            CharacterId = character.Id, QuestId = questId, DefinitionVersion = version, Status = status,
            CompletedAt = status == QuestStatus.Completed ? DateTimeOffset.UtcNow : null,
            RewardsGrantedAt = status == QuestStatus.Completed ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
}
