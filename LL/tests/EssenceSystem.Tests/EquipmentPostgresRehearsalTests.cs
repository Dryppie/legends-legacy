using System.Text.Json;
using Domain.Models.Analytics;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Persistence.LL;
using Persistence.LL.Repositories.Analytics;
using Persistence.LL.Repositories.Equipments;
using Services.LL.Items;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class LocalPostgresFactAttribute : FactAttribute
{
    public LocalPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_REHEARSAL_POSTGRES_CONNECTION")))
            Skip = "Set LL_REHEARSAL_POSTGRES_CONNECTION to a disposable loopback PostgreSQL cluster to run this integration rehearsal.";
    }
}

public sealed class EquipmentPostgresRehearsalTests
{
    [LocalPostgresFact]
    public async Task Schema_jsonb_retry_concurrency_interruption_pending_rewards_and_rollback_use_real_postgres()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("LL_REHEARSAL_POSTGRES_CONNECTION"));
        if (connection.Host != "127.0.0.1" || connection.Database != "postgres")
            throw new InvalidOperationException("Rehearsal requires the postgres maintenance database on 127.0.0.1. It creates its own uniquely named database.");
        connection.Database = "ll_attribute_rehearsal_" + Guid.NewGuid().ToString("N");
        connection.Pooling = false;
        var options = new DbContextOptionsBuilder<LLDbContext>().UseNpgsql(connection.ConnectionString).Options;
        LLDbContext Open() => new(options);
        const string previous = "20260925111407_AddLeanTelemetry";
        await using var schema = Open();
        try
        {
            await schema.GetService<IMigrator>().MigrateAsync(previous);
            var root = Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json");
            var legacy = JsonStarterEquipmentCatalog.Load(root, balanceVersion: 1);
            var current = JsonStarterEquipmentCatalog.Load(root);
            var versions = new JsonEquipmentCatalogProvider(root);
            var character = new Character { Id = Guid.NewGuid(), Name = "Local rehearsal", NormalizedName = "LOCAL REHEARSAL", Level = 30, User = AppUser.Guest() };
            character.Inventory = new() { CharacterId = character.Id, Character = character };
            character.EquipmentSlots = Enum.GetValues<EquipmentSlotType>().Select(x => new EquipmentSlot { EntityId = character.Id, EquipmentSlotType = x }).ToList();
            var state = EquipmentState.Award(Guid.NewGuid(), legacy.Evaluator, "plain.shortsword", 1, 0,
                new(EquipmentAwardKind.RandomDiscovery, "rehearsal", "instance"), new(EquipmentOwnershipKind.UnboundPersonal, character.Id));
            var before = EquipmentData.Create(state, legacy.Evaluator);
            var itemBase = new EquipmentBase { Id = before.ItemBaseId, Name = "Rehearsal sword", EquipmentType = before.EquipmentType };
            var item = new EquipmentInstance { Id = state.Id, ItemBaseId = itemBase.Id, ItemBase = itemBase };
            item.ApplyProgressionData(before);
            character.Inventory.InventoryItems.Add(new() { InventoryId = character.Id, ItemInstanceId = item.Id, ItemInstance = item });
            var rewardData = EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), legacy.Evaluator, "plain.shortsword", 1, 0,
                new(EquipmentAwardKind.RandomDiscovery, "rehearsal", "pending"), new(EquipmentOwnershipKind.UnboundPersonal, character.Id)), legacy.Evaluator);
            var run = new DungeonRun { Id = Guid.NewGuid(), CharacterId = character.Id, DungeonDefinitionId = "rehearsal", CreatedAt = DateTimeOffset.UtcNow,
                PendingRewards = [new() { ItemId = itemBase.Id, Name = "Rehearsal reward", Quantity = 1, ItemType = ItemType.Equipment, ProgressionData = rewardData }] };
            schema.Characters.Add(character); schema.DungeonRuns.Add(run);
            await schema.SaveChangesAsync();
            await schema.GetService<IMigrator>().MigrateAsync("20260925142600_AttributeRedesignReceiptsAndTelemetry");
            var historicalOperation = Guid.NewGuid();
            var historicalItem = Guid.NewGuid();
            var frozenJson = before.Serialize();
            await schema.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "EquipmentMigrationReceipts" ("OperationId", "ItemId", "Location", "ActorId", "BeforeJson", "AfterJson", "SourceHash", "ResultHash", "MappingReason", "AppliedAtUtc")
                VALUES ({historicalOperation}, {historicalItem}, 0, 'rehearsal', {frozenJson}::jsonb, {frozenJson}::jsonb, '', '', 'historical receipt schema fixture', now())
                """);
            await schema.Database.MigrateAsync();
            var historicalReceipt = await schema.Set<EquipmentMigrationReceipt>().SingleAsync(x => x.OperationId == historicalOperation);
            Assert.Equal(1, historicalReceipt.RespecializationAllowance);
            Assert.Equal(0, historicalReceipt.Revision);
            schema.Remove(historicalReceipt);
            await schema.SaveChangesAsync();
            Assert.Contains("20260925142600_AttributeRedesignReceiptsAndTelemetry", await schema.Database.GetAppliedMigrationsAsync());
            schema.ChangeTracker.Clear();
            var repository = new EquipmentMigrationRepository(schema);
            var audit = await repository.AuditAsync(0, 100, default);
            Assert.Equal(1, audit.LegacyInstances); Assert.Equal(1, audit.LegacyPendingRewards);
            var target = new EquipmentMigrationTarget(item.Id);
            var preview = EquipmentMigrationPolicy.Preview(target, before, current.Evaluator);
            var operation = Guid.NewGuid();
            EquipmentMigrationService Service(LLDbContext db) => new(new EquipmentMigrationRepository(db), new(current, versions),
                new GameEventOutbox(db, new GameEventOutboxConsumerRegistry(), new(JsonSerializerDefaults.Web), TimeProvider.System),
                new ReceiptTimeProvider(), new EquipmentUpgradeRepository(db));
            async Task<EquipmentMigrationReceipt> Apply(Guid op, EquipmentMigrationTarget t, EquipmentMigrationPreview p)
            {
                await using var db = Open(); await using var transaction = await db.Database.BeginTransactionAsync();
                var receipt = await Service(db).ApplyAsync(op, t, p.SourceHash, p.After.State.DefinitionId, "local-rehearsal", default, p.TargetBalanceVersion, p.ResultHash);
                await db.SaveChangesAsync(); await transaction.CommitAsync(); return receipt;
            }
            // A connection/transaction interruption after SaveChanges must leave no item, receipt or outbox mutation.
            await using (var db = Open())
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                await Service(db).ApplyAsync(operation, target, preview.SourceHash, preview.After.State.DefinitionId, "interrupted", default);
                await db.SaveChangesAsync();
            }
            Assert.Empty(await schema.Set<EquipmentMigrationReceipt>().ToListAsync());
            Assert.Empty(await schema.GameEventOutboxMessages.ToListAsync());
            Assert.Equal(preview.SourceHash, EquipmentMigrationPolicy.Hash((await repository.LoadAsync(target, false, default))!));
            // The second writer must block on the first writer's character/item locks, then replay the same receipt.
            await using (var writer = Open())
            {
                await using var transaction = await writer.Database.BeginTransactionAsync();
                var first = await Service(writer).ApplyAsync(operation, target, preview.SourceHash, preview.After.State.DefinitionId, "first", default);
                await writer.SaveChangesAsync();
                var second = Apply(operation, target, preview);
                Assert.NotSame(second, await Task.WhenAny(second, Task.Delay(200)));
                await transaction.CommitAsync();
                var replayed = await second.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.Equal(operation, replayed.OperationId);
                Assert.Equal(first.AppliedAtUtc, replayed.AppliedAtUtc);
            }
            schema.ChangeTracker.Clear();
            var stored = await schema.ItemInstances.OfType<EquipmentInstance>().Include(x => x.InstanceModifiers).SingleAsync(x => x.Id == item.Id);
            Assert.Equal(preview.After.Stats.OrderBy(x => x.Key), stored.ProgressionData!.Stats.OrderBy(x => x.Key));
            Assert.Equal(preview.After.Stats.Count, stored.InstanceModifiers.Count);
            Assert.Single(await schema.Set<EquipmentMigrationReceipt>().ToListAsync());
            Assert.Equal(2, await schema.GameEventOutboxMessages.CountAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => Apply(Guid.NewGuid(), target, preview));
            var pending = new EquipmentMigrationTarget(rewardData.State.Id, EquipmentMigrationLocation.PendingDungeonReward, run.Id);
            var pendingPreview = EquipmentMigrationPolicy.Preview(pending, rewardData, current.Evaluator);
            var pendingOperation = Guid.NewGuid(); await Apply(pendingOperation, pending, pendingPreview);
            schema.ChangeTracker.Clear();
            Assert.True((await repository.AuditAsync(0, 100, default)).ItemConversionComplete);
            Assert.Equal(18, (await repository.LoadAsync(pending, false, default))!.StatVersion);
            var choice = (await new ItemizationChoiceRepository(schema).CaptureAsync(character.Id, default))!.ForItem(item.Id)!;
            Assert.True(choice.Complete); Assert.True(choice.CandidateEligible); Assert.Empty(choice.Alternatives);
            var observation = new ItemizationObservation(ItemizationObservation.StableId("awarded", "rehearsal", item.Id),
                "awarded", character.Id, DateTimeOffset.UtcNow.AddDays(-8), "rehearsal", 18, item.Id, preview.After) { Choices = choice };
            var telemetry = new ItemizationTelemetryRepository(schema);
            await telemetry.RecordAsync(observation, default); await telemetry.RecordAsync(observation, default);
            var row = Assert.Single(await schema.Set<ItemizationObservationRow>().ToListAsync());
            var restored = JsonSerializer.Deserialize<ItemizationObservation>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.True(restored.Choices!.Complete); Assert.Equal(item.Id, restored.Choices.Candidate!.ItemId);
            await telemetry.GenerateDailyReportsAsync(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), default);
            Assert.Contains(await telemetry.GetReportsAsync(7, default), x => x.SevenDayOpportunities.Any(o => o.EligibleAwards == 1));
            var version2Audit = await repository.AuditAsync(0, 100, default, 2);
            Assert.Equal(1, version2Audit.MatchingInstances);
            Assert.Equal(1, version2Audit.MatchingPendingRewards);
            var laterOperations = new List<Guid>();
            foreach (var laterTarget in new[] { target, pending })
            {
                await using var reader = Open();
                var laterPreview = await Service(reader).PreviewAsync(laterTarget, null, default, 3);
                var laterOperation = Guid.NewGuid();
                var later = await Apply(laterOperation, laterTarget, laterPreview);
                Assert.Equal(1, later.Revision);
                Assert.Equal(1, later.RespecializationAllowance);
                Assert.Equal(later.ResultHash, (await Apply(laterOperation, laterTarget, laterPreview)).ResultHash);
                laterOperations.Add(laterOperation);
            }
            await using (var blocked = Open())
            {
                await using var transaction = await blocked.Database.BeginTransactionAsync();
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Service(blocked).RollbackAsync(operation, "out-of-order", default));
                Assert.Contains("most recent", error.Message);
            }
            schema.ChangeTracker.Clear();
            Assert.Equal(0, (await repository.AuditAsync(0, 100, default, 2)).MatchingInstances);
            Assert.Equal(1, (await repository.AuditAsync(0, 100, default, 3)).MatchingInstances);
            foreach (var laterOperation in laterOperations)
            {
                await using var db = Open(); await using var transaction = await db.Database.BeginTransactionAsync();
                await Service(db).RollbackAsync(laterOperation, "rebalance-rollback", default);
                await db.SaveChangesAsync(); await transaction.CommitAsync();
            }
            foreach (var op in new[] { operation, pendingOperation })
            {
                await using var db = Open(); await using var transaction = await db.Database.BeginTransactionAsync();
                var rollback = await Service(db).RollbackAsync(op, "rehearsal-rollback", default);
                var returnedAt = rollback.RolledBackAtUtc;
                await db.SaveChangesAsync(); await transaction.CommitAsync();
                await using var retryDb = Open();
                Assert.Equal(returnedAt, (await Service(retryDb).RollbackAsync(op, "rehearsal-rollback", default)).RolledBackAtUtc);
            }
            schema.ChangeTracker.Clear();
            Assert.Equal(preview.SourceHash, EquipmentMigrationPolicy.Hash((await repository.LoadAsync(target, false, default))!));
            Assert.Equal(EquipmentMigrationPolicy.Hash(rewardData), EquipmentMigrationPolicy.Hash((await repository.LoadAsync(pending, false, default))!));
            Assert.All(await schema.Set<EquipmentMigrationReceipt>().ToListAsync(), x => Assert.NotNull(x.RolledBackAtUtc));
            // Raw legacy equipment requires relational ownership, a complete receipt, and
            // restoration of modifier IDs/rarity bonuses as well as values.
            var clothBase = new EquipmentBase { Id = "cloth_cowl", Name = "Old cloth cowl", EquipmentType = EquipmentType.Head,
                AttributeModifiers = [new(Domain.Models.Attributes.AttributeType.Power, 30)] };
            var cloth = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = clothBase.Id, ItemBase = clothBase,
                Tier = 2, Quality = ItemQuality.Fine, Rarity = Rarity.Uncommon, IsFavorite = true,
                AcquisitionSource = "crafting", AffinityTags = ["legacy-affinity"],
                InstanceModifiers = [new(Domain.Models.Attributes.AttributeType.HealingPowerPercent, 12.5f)
                    { Id = Guid.NewGuid(), RarityBonusAmount = 1.5f }] };
            var orphan = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = clothBase.Id, ItemBase = clothBase };
            schema.ItemInstances.Add(orphan);
            schema.InventoryItems.Add(new() { InventoryId = character.Id, ItemInstanceId = cloth.Id, ItemInstance = cloth, Quantity = 1 });
            await schema.SaveChangesAsync();
            schema.ChangeTracker.Clear();
            var clothTarget = new EquipmentMigrationTarget(cloth.Id);
            var rawAudit = await repository.AuditAsync(0, 100, default);
            Assert.Equal(2, rawAudit.UnversionedInstances);
            Assert.Equal(1, rawAudit.UnreferencedUnversionedInstances);
            Assert.Contains(clothTarget, rawAudit.Targets);
            Assert.False(rawAudit.ItemConversionComplete);
            await Assert.ThrowsAsync<InvalidOperationException>(() => Service(schema).PreviewAsync(new(orphan.Id), null, default, 4));
            var rawPreview = await Service(schema).PreviewAsync(clothTarget, null, default, 4);
            var rawOriginal = rawPreview.LegacyBefore!;
            var rawOperation = Guid.NewGuid();
            await Apply(rawOperation, clothTarget, rawPreview);
            Assert.Equal(rawOperation, (await Apply(rawOperation, clothTarget, rawPreview)).OperationId);
            schema.ChangeTracker.Clear();
            var imported = await repository.LoadAsync(clothTarget, false, default);
            Assert.Equal(4, imported!.State.BalanceVersion);
            Assert.Equal(rawOriginal.Ownership, imported.State.Ownership);
            Assert.Equal("cloth_cowl", imported.ItemBaseId);
            Assert.Equal(1, (await repository.AuditAsync(0, 100, default)).UnversionedInstances);
            await using (var metadataChange = Open())
            {
                var changed = await metadataChange.ItemInstances.OfType<EquipmentInstance>().SingleAsync(x => x.Id == cloth.Id);
                changed.IsFavorite = false;
                await metadataChange.SaveChangesAsync();
            }
            await using (var blocked = Open())
            {
                await using var transaction = await blocked.Database.BeginTransactionAsync();
                await Assert.ThrowsAsync<InvalidOperationException>(() => Service(blocked).RollbackAsync(rawOperation, "test", default));
            }
            await using (var resetMetadata = Open())
            {
                var changed = await resetMetadata.ItemInstances.OfType<EquipmentInstance>().SingleAsync(x => x.Id == cloth.Id);
                changed.IsFavorite = true;
                await resetMetadata.SaveChangesAsync();
            }
            await using (var rollbackRaw = Open())
            {
                await using var transaction = await rollbackRaw.Database.BeginTransactionAsync();
                await Service(rollbackRaw).RollbackAsync(rawOperation, "raw-rollback", default);
                await rollbackRaw.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            schema.ChangeTracker.Clear();
            Assert.Null(await repository.LoadAsync(clothTarget, false, default));
            Assert.Equal(rawOriginal.Hash(), (await repository.LoadLegacyAsync(clothTarget, default))!.Hash());
            Assert.Equal(rawPreview.SourceHash, (await Service(schema).PreviewAsync(clothTarget, null, default, 4)).SourceHash);
            Assert.NotNull((await Service(schema).RollbackAsync(rawOperation, "retry", default)).RolledBackAtUtc);
            // Exercise migration Down and Up on this disposable database as well.
            await schema.GetService<IMigrator>().MigrateAsync(previous);
            await schema.Database.MigrateAsync();
            Assert.Empty(await schema.Set<EquipmentMigrationReceipt>().ToListAsync());
        }
        finally
        {
            // This context can only reference the unique database created above.
            await schema.Database.EnsureDeletedAsync();
        }
    }

    private sealed class ReceiptTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero).AddTicks(1234567);
    }
}
