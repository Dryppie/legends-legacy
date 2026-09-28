using System.Text.Json;
using Application;
using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Equipments.Commands.ConvertEquipmentOnStartup;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.CharacterActions;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Snapshots;
using Domain.Models.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Services.LL;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentStartupPostgresRehearsalTests
{
    [LocalPostgresFact]
    public async Task Startup_uses_real_commands_preserves_schedules_and_history_refreshes_defenses_and_resumes()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("LL_REHEARSAL_POSTGRES_CONNECTION"));
        if (connection.Host != "127.0.0.1" || connection.Database != "postgres")
            throw new InvalidOperationException("Use a disposable loopback cluster's postgres database; the test creates its own database.");
        connection.Database = "ll_startup_rehearsal_" + Guid.NewGuid().ToString("N");
        connection.Pooling = false;
        var options = new DbContextOptionsBuilder<LLDbContext>().UseNpgsql(connection.ConnectionString).Options;
        LLDbContext Open() => new(options);
        await using var schema = Open();
        try
        {
            await schema.Database.MigrateAsync();
            var root = TestContentPaths.FindApiRoot();
            var versions = new JsonEquipmentCatalogProvider(Path.Combine(root, "Data", "equipment", "equipment-starters.v1.json"));
            var character = new Character { Id = Guid.NewGuid(), Name = "Startup rehearsal", NormalizedName = "STARTUP REHEARSAL", Level = 30, User = AppUser.Guest() };
            character.Inventory = new() { CharacterId = character.Id, Character = character };
            character.EquipmentSlots = Enum.GetValues<EquipmentSlotType>().Select(x => new EquipmentSlot { EntityId = character.Id, EquipmentSlotType = x }).ToList();
            var itemBase = new EquipmentBase { Id = "shortsword", Name = "Rehearsal sword", EquipmentType = EquipmentType.OneHanded };
            EquipmentInstance Sword(int version)
            {
                var evaluator = versions.Get(version).Evaluator;
                var data = EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), evaluator, "plain.shortsword", 1, 0,
                    new(EquipmentAwardKind.RandomDiscovery, "startup-test", Guid.NewGuid().ToString()),
                    new(EquipmentOwnershipKind.UnboundPersonal, character.Id)), evaluator);
                itemBase.Id = data.ItemBaseId;
                var item = new EquipmentInstance { Id = data.State.Id, ItemBaseId = data.ItemBaseId, ItemBase = itemBase };
                item.ApplyProgressionData(data);
                return item;
            }
            var swords = new[] { Sword(1), Sword(2), Sword(3), Sword(4) };
            foreach (var item in swords)
                character.Inventory.InventoryItems.Add(new() { InventoryId = character.Id, ItemInstanceId = item.Id, ItemInstance = item });
            var oldBase = new EquipmentBase { Id = "cloth_cowl", Name = "Old cowl", EquipmentType = EquipmentType.Head };
            var equipped = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = oldBase.Id, ItemBase = oldBase,
                InstanceModifiers = [new(AttributeType.HealingPowerPercent, 12), new(AttributeType.StatusResistance, 4)] };
            var slot = character.EquipmentSlots.Single(x => x.EquipmentSlotType == EquipmentSlotType.Head);
            slot.EquipmentInstanceId = equipped.Id;
            slot.EquipmentInstance = equipped;
            var orphan = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = oldBase.Id, ItemBase = oldBase,
                InstanceModifiers = [new(AttributeType.HealingPowerPercent, 8)] };
            var versionedOrphan = Sword(1);
            var pending = Sword(2).ProgressionData!;
            var unsupported = new RunReward { ItemId = oldBase.Id, Name = "Unsupported legacy reward", Quantity = 1, ItemType = ItemType.Equipment };
            var run = new DungeonRun { Id = Guid.NewGuid(), CharacterId = character.Id, DungeonDefinitionId = "rehearsal", CreatedAt = DateTimeOffset.UtcNow,
                PendingRewards = [new() { ItemId = pending.ItemBaseId, Name = pending.DisplayName, Quantity = 1, ItemType = ItemType.Equipment, ProgressionData = pending }, unsupported] };
            var action = new CharacterAction { CharacterId = character.Id, NextResolutionAtUtc = DateTimeOffset.UtcNow.AddHours(-2), ScheduleGeneration = 7 };
            var history = new CharacterSnapshot { Id = Guid.NewGuid(), CharacterId = character.Id, Name = character.Name, Level = 30,
                AttributeRulesVersion = 17, Equipment = [EquipmentSnapshot.From(EquipmentSlotType.Head, equipped)] };
            var arena = new CharacterSnapshot { Id = Guid.NewGuid(), CharacterId = character.Id, Name = character.Name, Level = 30,
                AttributeRulesVersion = 18, Equipment = [EquipmentSnapshot.From(EquipmentSlotType.Head, equipped)] };
            var tournament = new TournamentInstance { Id = Guid.NewGuid(), Name = "Old tournament", Status = TournamentStatus.InProgress,
                Definition = new() { Id = Guid.NewGuid(), Key = "rehearsal", Name = "Rehearsal", Description = "Rehearsal" } };
            schema.Characters.Add(character);
            schema.ItemInstances.AddRange(orphan, versionedOrphan);
            schema.DungeonRuns.Add(run);
            schema.CharacterActions.Add(action);
            schema.CharacterSnapshots.AddRange(history, arena);
            schema.ArenaDefenseSnapshots.Add(new ArenaDefenseSnapshot { CharacterId = character.Id, CharacterSnapshot = arena, IsValid = true });
            schema.TournamentCombatSnapshots.Add(new TournamentCombatSnapshot { Id = Guid.NewGuid(), Tournament = tournament,
                CharacterId = character.Id, CharacterSnapshot = history, SnapshotVersion = "17", SnapshotJson = "{}", RankTierAtSnapshot = "test" });
            await schema.SaveChangesAsync();
            async Task<string> ActionJson() => await schema.Database.SqlQuery<string>(
                $"""SELECT row_to_json(a)::text AS "Value" FROM "CharacterActions" a WHERE a."CharacterId" = {character.Id}""").SingleAsync();
            var actionBefore = await ActionJson();
            var orphanBefore = JsonSerializer.Serialize(orphan.InstanceModifiers.Select(x => new { x.Id, x.Amount, x.AttributeType }));
            var currentBefore = swords[3].ProgressionData!.Serialize();
            schema.ChangeTracker.Clear();

            var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json")
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LegendsLegacyDB"] = connection.ConnectionString,
                    ["Database:TimeoutInSeconds"] = "30",
                    ["Content:Root"] = Path.Combine(root, "Data"),
                    ["AttributeRedesign:LiveVersion"] = "18", ["EquipmentBalance:LiveVersion"] = "4",
                    ["Combat:AbilityBalanceProfile"] = "healing-v1",
                    ["EquipmentConversion:RunOnStartup"] = "true", ["EquipmentConversion:TargetBalanceVersion"] = "4"
                }).Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config).AddLogging().AddHttpContextAccessor();
            services.AddPersistence(config).AddRepositories().AddApplication();
            services.AddServices(config, root);
            await using var provider = services.BuildServiceProvider();

            var repository = new EquipmentStartupConversionRepository(schema, new EquipmentMigrationRepository(schema));
            var before = await repository.AuditAsync(4, default);
            Assert.Equal(new EquipmentStartupConversionAudit(5, 1, 1, 1, 2), before);
            Assert.Single(await repository.GetArenaDefensesAsync(4, 100, default));
            // The runner lock serializes simultaneous API startups and releases on cancellation/disposal.
            await using (var held = await repository.AcquireRunnerLockAsync(default))
            {
                using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.AcquireRunnerLockAsync(cancellation.Token));
            }

            // A transaction lost after saving cannot leave half a conversion or receipt behind.
            var target = new EquipmentMigrationTarget(equipped.Id);
            await using (var interrupted = provider.CreateAsyncScope())
            {
                var db = interrupted.ServiceProvider.GetRequiredService<LLDbContext>();
                await using var transaction = await db.Database.BeginTransactionAsync();
                Assert.True(await interrupted.ServiceProvider.GetRequiredService<IEquipmentMigrationService>()
                    .ConvertOnStartupAsync(Guid.NewGuid(), target, 4, default));
                await db.SaveChangesAsync();
            }
            Assert.Empty(await schema.Set<EquipmentMigrationReceipt>().ToListAsync());
            Assert.True(await repository.IsCandidateAsync(target, 4, default));

            // Different startup operations targeting the same item must still produce just one receipt.
            async Task<bool> Convert() { await using var scope = provider.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new ConvertEquipmentOnStartupCommand(Guid.NewGuid(), target, 4)); }
            var concurrent = await Task.WhenAll(Convert(), Convert());
            Assert.Single(concurrent, x => x);
            Assert.Single(await schema.Set<EquipmentMigrationReceipt>().ToListAsync());

            var blocked = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ConvertExistingEquipmentAsync());
            Assert.Contains("1 unsupported unversioned rewards", blocked.Message);
            Assert.Contains("1 active legacy tournament snapshots", blocked.Message);
            var audit = await repository.AuditAsync(4, default);
            Assert.Equal(0, audit.RemainingItems);
            Assert.Equal(0, audit.OutdatedArenaDefenses);
            Assert.Equal(5, await schema.Set<EquipmentMigrationReceipt>().CountAsync());
            schema.ChangeTracker.Clear();

            // Simulate resolving the explicitly reported blockers, then retry the exact startup path.
            schema.Remove(await schema.Set<RunReward>().SingleAsync(x => x.Id == unsupported.Id));
            (await schema.Set<TournamentInstance>().SingleAsync(x => x.Id == tournament.Id)).Status = TournamentStatus.Completed;
            await schema.SaveChangesAsync();
            await provider.ConvertExistingEquipmentAsync();
            await provider.ConvertExistingEquipmentAsync();
            Assert.True((await repository.AuditAsync(4, default)).Complete);
            Assert.Equal(5, await schema.Set<EquipmentMigrationReceipt>().CountAsync());
            Assert.Equal(actionBefore, await ActionJson());
            var storedOrphan = await schema.ItemInstances.OfType<EquipmentInstance>().Include(x => x.InstanceModifiers).SingleAsync(x => x.Id == orphan.Id);
            Assert.Null(storedOrphan.ProgressionData);
            Assert.Equal(orphanBefore, JsonSerializer.Serialize(storedOrphan.InstanceModifiers.Select(x => new { x.Id, x.Amount, x.AttributeType })));
            Assert.Equal(currentBefore, (await schema.ItemInstances.OfType<EquipmentInstance>().SingleAsync(x => x.Id == swords[3].Id)).ProgressionData!.Serialize());
            Assert.Equal(1, (await schema.ItemInstances.OfType<EquipmentInstance>().SingleAsync(x => x.Id == versionedOrphan.Id)).ProgressionData!.State.BalanceVersion);
            Assert.Equal(4, (await schema.ItemInstances.OfType<EquipmentInstance>().SingleAsync(x => x.Id == equipped.Id)).ProgressionData!.State.BalanceVersion);
            var refreshed = await schema.ArenaDefenseSnapshots.Include(x => x.CharacterSnapshot).ThenInclude(x => x.Equipment).SingleAsync();
            Assert.Equal(18, refreshed.CharacterSnapshot.AttributeRulesVersion);
            Assert.Equal(4, Assert.Single(refreshed.CharacterSnapshot.Equipment).ProgressionData!.State.BalanceVersion);
            Assert.NotEqual(arena.Id, refreshed.CharacterSnapshotId);
            foreach (var id in new[] { history.Id, arena.Id })
            {
                var old = await schema.CharacterSnapshots.Include(x => x.Equipment).ThenInclude(x => x.InstanceModifiers).SingleAsync(x => x.Id == id);
                Assert.Null(Assert.Single(old.Equipment).ProgressionData);
                Assert.Equal(2, old.Equipment.Single().InstanceModifiers.Count);
            }
            Assert.True(await schema.GameEventOutboxMessages.AnyAsync());
        }
        finally { await schema.Database.EnsureDeletedAsync(); }
    }
}
