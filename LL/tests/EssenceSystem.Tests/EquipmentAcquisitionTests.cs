using System.Text.Json;
using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Behaviors;
using Application.UseCases.Equipments.Commands.RecoverBaselineEquipment;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Quests;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Persistence.LL.Repositories.Quests;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Dungeons;
using Services.LL.Interfaces;
using Services.LL.Inventories;
using Services.LL.Items;
using Services.LL.Outbox;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed partial class EquipmentAcquisitionTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Catalog_has_twelve_isolated_pools_and_thirty_two_exact_named_definitions()
    {
        await using var f = await Fixture.Create();
        Assert.Equal(12, f.Catalog.Pools.Count);
        Assert.Equal(32, f.Catalog.Pools.SelectMany(x => x.TargetDefinitionIds).Distinct().Count());
        foreach (var pool in f.Catalog.Pools)
        {
            Assert.Equal(8, pool.TargetDefinitionIds.Count);
            foreach (var id in pool.TargetDefinitionIds)
            {
                var state = f.Target(pool, id);
                Assert.Equal(pool.EquipmentTier, state.State.Tier);
                Assert.Equal(1, state.State.Rank);
                Assert.Equal(EquipmentRarity.Rare, state.Rarity);
                Assert.NotNull(state.EquipmentSetId);
                Assert.Equal(state.State.NativeStyleId, state.State.ActiveStyleId);
            }
        }
        var views = await f.Service.GetPoolsAsync(f.Id, Ct);
        Assert.Equal(12, Mapper().Map<List<EquipmentProtectionPoolDto>>(views).Count);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        var copy = f.Catalog.Pools[0] with { MatchingChance = double.NaN };
        Assert.Throws<ArgumentException>(() => new EquipmentAcquisitionCatalog(f.Catalog.Evaluator, [copy]));
    }

    [Theory]
    [InlineData(false, 0.9, 8, true)]
    [InlineData(false, 0.1, 1, false)]
    [InlineData(true, 0.9, 1, true)]
    public async Task Matching_and_guaranteed_awards_share_stats_but_keep_correct_binding(bool first, double roll, int clears, bool bound)
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools[0];
        var progress = new EquipmentProtectionProgress { CharacterId = f.Id, PoolId = pool.Id };
        var data = f.Target(pool, pool.TargetDefinitionIds[0]);
        EquipmentProtectionOutcome? outcome = null;
        for (var index = 1; index <= clears; index++)
        {
            var commitment = new DungeonEquipmentCommitment(f.Id, Guid.NewGuid(), pool.Id, pool.DungeonId, pool.Difficulty, .2, 8, data);
            outcome = progress.Complete(commitment, first, roll, DateTimeOffset.UtcNow);
            if (index < clears) Assert.Null(outcome.Equipment);
        }
        var reward = outcome!.Equipment!;
        Assert.NotNull(reward);
        Assert.Equal(data.Stats, reward.Stats);
        Assert.Equal(bound ? EquipmentOwnershipKind.BoundPersonal : EquipmentOwnershipKind.UnboundPersonal, reward.State.Ownership.Kind);
        Assert.Equal(0, progress.CompletionsWithoutMatch);
    }

    [Fact]
    public async Task Target_switches_preserve_progress_and_sources_and_difficulties_remain_independent()
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools[0];
        await f.Select(pool, 0);
        var first = await f.NewRun(pool, chance: 0);
        await f.Complete(first);
        await f.Select(pool, 1);
        Assert.Equal(1, (await f.Repository.GetProgressAsync(f.Id, pool.Id, Ct))!.CompletionsWithoutMatch);
        await f.DeleteRun(first);
        var different = f.Catalog.Pools[1];
        await f.Select(different, 0);
        var second = await f.NewRun(different, chance: 0);
        await f.Complete(second, first: true);
        Assert.DoesNotContain(second.PendingRewards, x => x.ProgressionData != null); // first-clear equipment is difficulty I only
        Assert.Equal(1, (await f.Repository.GetProgressAsync(f.Id, different.Id, Ct))!.CompletionsWithoutMatch);
        Assert.Equal(1, (await f.Repository.GetProgressAsync(f.Id, pool.Id, Ct))!.CompletionsWithoutMatch);
        Assert.Null(await f.Repository.GetProgressAsync(f.Id, f.Catalog.Pools[3].Id, Ct));
        await f.Service.SelectAsync(f.Id, pool.Id, null, Ct);
        Assert.Equal(1, (await f.Repository.GetProgressAsync(f.Id, pool.Id, Ct))!.CompletionsWithoutMatch);
    }

    [Fact]
    public async Task Commitment_survives_target_switch_reload_and_disable_and_completion_replay_is_inert()
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools[0];
        await f.Select(pool, 0);
        var run = await f.NewRun(pool, chance: 1);
        var original = run.EquipmentCommitment!.Target!.Serialize();
        await f.Select(pool, 1);
        f.Flags.ProtectedAcquisitionEnabled = false;
        f.Db.ChangeTracker.Clear();
        run = await f.Runs.GetDungeonRunByDungeonIdAsync(run.Id, Ct) ?? throw new InvalidOperationException();
        Assert.Equal(original, run.EquipmentCommitment!.Target!.Serialize());
        await f.Complete(run);
        var pending = Assert.Single(run.PendingRewards, x => x.ProgressionData != null);
        Assert.Equal(pool.TargetDefinitionIds[0], pending.ProgressionData!.State.DefinitionId);
        var progress = await f.Repository.GetProgressAsync(f.Id, pool.Id, Ct);
        Assert.Equal(pool.TargetDefinitionIds[1], progress!.SelectedDefinitionId);
        var revision = progress.Revision;
        await f.Service.CompleteAsync(run, false, Ct);
        await f.Db.SaveChangesAsync();
        Assert.Equal(revision, progress.Revision);
        Assert.Single(run.PendingRewards);
        Assert.Single(await f.Db.EquipmentProtectionReceipts.ToListAsync());
        Assert.Single(await f.Db.GameEventOutboxMessages.ToListAsync());
        Assert.Equal(pending.ProgressionData.DisplayName, Mapper().Map<RunRewardDto>(pending).ProgressionData!.DisplayName);
    }

    [Theory]
    [InlineData(DungeonRunStatus.Failed)]
    [InlineData(DungeonRunStatus.Retreated)]
    [InlineData(DungeonRunStatus.Active)]
    public async Task Ineligible_completion_status_never_advances_or_awards(DungeonRunStatus status)
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools[0];
        await f.Select(pool, 0);
        var run = await f.NewRun(pool);
        run.Status = status;
        await f.Service.CompleteAsync(run, true, Ct);
        Assert.Empty(run.PendingRewards);
        Assert.Empty(f.Db.EquipmentProtectionReceipts.Local);
        Assert.Equal(0, (await f.Repository.GetProgressAsync(f.Id, pool.Id, Ct))!.CompletionsWithoutMatch);
    }

    [Fact]
    public async Task No_target_does_not_award_equipment_or_advance_protection()
    {
        await using var f = await Fixture.Create();
        var run = await f.NewRun(f.Catalog.Pools[0]);
        await f.Complete(run, first: true);
        Assert.Null(run.EquipmentCommitment!.Target);
        Assert.Empty(run.PendingRewards);
        Assert.Equal(0, (await f.Db.EquipmentProtectionProgress.SingleAsync()).CompletionsWithoutMatch);
    }

    [Fact]
    public async Task Dungeon_claim_preserves_exact_frozen_item_and_does_not_reset_newer_progress()
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools[0];
        await f.Select(pool, 0);
        var run = await f.NewRun(pool);
        await f.Complete(run, first: true);
        var frozen = Assert.Single(run.PendingRewards, x => x.ProgressionData != null).ProgressionData!;
        var progress = (await f.Repository.GetProgressAsync(f.Id, pool.Id, Ct))!;
        progress.Complete(run.EquipmentCommitment! with { RunId = Guid.NewGuid(), MatchingChance = 0 }, false, .9, DateTimeOffset.UtcNow);
        var claimer = new DungeonRunRewardClaimer(null!, null!, new ItemBaseRepository(f.Db), new InventoryItemFactory(),
            new InventoryService(new InventoryRepository(f.Db)), f.Service);
        var claimed = await claimer.ClaimAsync(run, Ct);
        await f.Db.SaveChangesAsync();
        var item = Assert.Single(claimed, x => x.ItemInstance is EquipmentInstance);
        Assert.Equal(frozen.Serialize(), ((EquipmentInstance)item.ItemInstance).ProgressionData!.Serialize());
        Assert.Equal(frozen.State.Id, item.ItemInstanceId);
        Assert.True(item.ItemInstance.IsBound);
        Assert.Equal(1, progress.CompletionsWithoutMatch);
        Assert.NotNull((await f.Db.EquipmentProtectionReceipts.SingleAsync()).ClaimedAtUtc);
        await f.DeleteRun(run);
        await f.Service.CompleteAsync(run, true, Ct);
        Assert.Equal(1, progress.CompletionsWithoutMatch);
        Assert.Single(await f.Db.ItemInstances.OfType<EquipmentInstance>().ToListAsync());
    }

    [Fact]
    public async Task Missing_secured_equipment_base_aborts_claim_before_other_rewards_are_paid()
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools[0];
        await f.Select(pool, 0);
        var run = await f.NewRun(pool);
        await f.Complete(run, first: true);
        var itemId = run.PendingRewards.Single(x => x.ProgressionData != null).ItemId;
        f.Db.ItemBases.Remove(await f.Db.ItemBases.SingleAsync(x => x.Id == itemId));
        await f.Db.SaveChangesAsync();
        run.PendingCinders = 100;
        var claimer = new DungeonRunRewardClaimer(null!, null!, new ItemBaseRepository(f.Db), new InventoryItemFactory(),
            new InventoryService(new InventoryRepository(f.Db)), f.Service);
        await Assert.ThrowsAsync<InvalidOperationException>(() => claimer.ClaimAsync(run, Ct));
        Assert.Empty(await f.Db.InventoryItems.ToListAsync());
        Assert.Null((await f.Db.EquipmentProtectionReceipts.SingleAsync()).ClaimedAtUtc);
    }

    [Fact]
    public async Task Access_preview_selection_and_commitment_enforce_level_quest_and_previous_difficulty()
    {
        await using var f = await Fixture.Create(eligible: false);
        var pool = f.Catalog.Pools[0];
        var dungeon = f.Definitions.GetByKey(pool.DungeonId);
        var preview = await f.Access.EvaluateForPreviewAsync(f.Id, [dungeon], Ct);
        Assert.False(preview[dungeon.Id].Entry.CanEnter);
        Assert.False(preview[dungeon.Id].SigilAssembly!.CanEnter);
        Assert.NotNull((await f.Service.SelectAsync(f.Id, pool.Id, pool.TargetDefinitionIds[0], Ct)).Error);
        var run = new DungeonRun { Id = Guid.NewGuid(), CharacterId = f.Id, DungeonDefinitionId = pool.DungeonId };
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.FreezeAsync(run, dungeon, Ct));
        f.Character.Level = 20;
        await f.Db.SaveChangesAsync();
        Assert.NotNull(await f.Eligibility.GetErrorAsync(f.Id, pool.DungeonId, Ct));
        await f.MakeEligible();
        Assert.Null((await f.Service.SelectAsync(f.Id, pool.Id, pool.TargetDefinitionIds[0], Ct)).Error);
        var higher = f.Catalog.Pools[1];
        var previous = await f.Db.DungeonCompletionRecords.SingleAsync(x => x.DungeonDefinitionId == pool.DungeonId);
        f.Db.DungeonCompletionRecords.Remove(previous);
        await f.Db.SaveChangesAsync();
        Assert.NotNull((await f.Service.SelectAsync(f.Id, higher.Id, higher.TargetDefinitionIds[0], Ct)).Error);
        Assert.NotNull((await f.Service.SelectAsync(f.Id, pool.Id, higher.TargetDefinitionIds[0] + "invalid", Ct)).Error);
    }

    [Fact]
    public async Task Run_factory_freezes_target_but_simulation_creates_no_commitment()
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools[0];
        await f.Select(pool, 0);
        var factory = new DungeonRunFactory(f.Definitions, new Snapshots(), new Delves(), f.Service);
        var run = await factory.CreateAsync(f.Id, pool.DungeonId, 42, Ct);
        Assert.Equal(pool.TargetDefinitionIds[0], run.EquipmentCommitment!.Target!.State.DefinitionId);
        Assert.Equal(run.Id, run.EquipmentCommitment.RunId);
        Assert.Null(factory.CreateForSimulation(pool.DungeonId, 42).EquipmentCommitment);
        Assert.Empty(f.Db.EquipmentProtectionReceipts.Local);
    }

    [Fact]
    public async Task Recovery_from_empty_loadout_is_free_bound_plain_and_replay_safe_even_after_loss()
    {
        await using var f = await Fixture.Create(eligible: false);
        var grant = await f.Baseline();
        var options = await f.Service.GetRecoveryOptionsAsync(f.Id, Ct);
        Assert.Equal(4, options.Sum(x => x.Missing));
        Assert.False(f.Db.ChangeTracker.HasChanges());
        var operation = Guid.NewGuid();
        var result = await f.Service.RecoverAsync(f.Id, operation, grant.Kind, Ct);
        var repeat = await f.Service.RecoverAsync(f.Id, operation, grant.Kind, Ct);
        Assert.Same(result.Recovery, repeat.Recovery);
        await f.Db.SaveChangesAsync();
        Assert.Equal(4, await f.Db.InventoryItems.CountAsync());
        Assert.All(result.Recovery!.Equipment, item =>
        {
            Assert.Equal(0, item.State.Rank);
            Assert.Null(item.State.ActiveStyleId);
            Assert.Equal(EquipmentAwardKind.Recovery, item.State.Provenance.Kind);
            Assert.Equal(EquipmentOwnershipKind.BoundPersonal, item.State.Ownership.Kind);
        });
        Assert.Equal(1000, f.Character.Cinders);
        Assert.Equal(4, await f.Db.EconomyLedger.CountAsync());
        Assert.Equal(GameEventTypes.BaselineEquipmentRecovered, Assert.Single(await f.Db.GameEventOutboxMessages.ToListAsync()).EventType);
        Assert.Equal(4, Mapper().Map<BaselineEquipmentRecoveryDto>(result.Recovery).Equipment.Count);
        var lost = await f.Db.InventoryItems.FirstAsync();
        f.Db.InventoryItems.Remove(lost);
        f.Db.ItemInstances.Remove(lost.ItemInstance);
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        await f.Service.RecoverAsync(f.Id, operation, grant.Kind, Ct);
        Assert.Equal(3, await f.Db.InventoryItems.CountAsync());
        var restored = await f.Service.RecoverAsync(f.Id, Guid.NewGuid(), grant.Kind, Ct);
        Assert.Single(restored.Recovery!.Equipment);
        await f.Db.SaveChangesAsync();
        Assert.Equal(4, await f.Db.InventoryItems.CountAsync());
        Assert.Null((await f.Service.RecoverAsync(f.Id, operation, StarterEquipmentGrantKind.ReadyForRoad, Ct)).Recovery);
    }

    [Fact]
    public async Task Dual_wield_entitlement_counts_equipped_and_pending_copies_once_and_preserves_authored_state()
    {
        await using var f = await Fixture.Create();
        var grant = await f.Baseline(dual: true);
        var first = grant.Equipment.First(x => x.State.DefinitionId == "plain.dagger");
        var modified = EquipmentData.Create(EquipmentState.Restore(first.State with { Rank = 3 }), f.Catalog.Evaluator);
        var equipped = await f.AddItem(modified);
        Assert.True((await new EquipmentSlotRepository(f.Db).EquipEquipmentAsync(f.Id, equipped.Id, EquipmentSlotType.MainHand, Ct)).Succeeded);
        var second = grant.Equipment.Last(x => x.State.DefinitionId == "plain.dagger");
        var run = new DungeonRun { Id = Guid.NewGuid(), CharacterId = f.Id, DungeonDefinitionId = "test", Status = DungeonRunStatus.Completed,
            PendingRewards = [new RunReward { ItemId = second.ItemBaseId, Name = second.DisplayName, Quantity = 1, ProgressionData = second }] };
        f.Db.DungeonRuns.Add(run);
        await f.Db.SaveChangesAsync();
        var option = Assert.Single(await f.Service.GetRecoveryOptionsAsync(f.Id, Ct), x => x.DefinitionId == "plain.dagger");
        Assert.Equal(2, option.Entitled);
        Assert.Equal(2, option.Owned);
        Assert.Equal(0, option.Missing);
        var recovered = await f.Service.RecoverAsync(f.Id, Guid.NewGuid(), grant.Kind, Ct);
        Assert.DoesNotContain(recovered.Recovery!.Equipment, x => x.State.DefinitionId == "plain.dagger");
        Assert.Equal(modified.Serialize(), equipped.ProgressionData!.Serialize());
        await f.Db.SaveChangesAsync();
        await f.DeleteRun(run);
        var one = await f.Service.RecoverAsync(f.Id, Guid.NewGuid(), grant.Kind, Ct);
        Assert.Equal("plain.dagger", Assert.Single(one.Recovery!.Equipment).State.DefinitionId);
    }

    [Fact]
    public async Task Concurrent_recoveries_from_separate_contexts_restore_only_the_saved_count()
    {
        await using var f = await Fixture.Create();
        var grant = await f.Baseline(dual: true);
        async Task<Response<BaselineEquipmentRecoveryDto>> Recover()
        {
            await using var db = new LLDbContext(f.DbOptions);
            var command = new RecoverBaselineEquipmentCommand(f.Id, Guid.NewGuid(), grant.Kind);
            var handler = new RecoverBaselineEquipmentCommandHandler(f.CreateService(db), Mapper());
            var pipeline = new TransactionBehavior<RecoverBaselineEquipmentCommand, Response<BaselineEquipmentRecoveryDto>>(db, new Sync(),
                NullLogger<TransactionBehavior<RecoverBaselineEquipmentCommand, Response<BaselineEquipmentRecoveryDto>>>.Instance);
            return await pipeline.Handle(command, ct => handler.Handle(command, ct), Ct);
        }
        var results = await Task.WhenAll(Recover(), Recover());
        Assert.All(results, x => Assert.True(x.IsSuccess));
        Assert.Equal(5, results.Sum(x => x.Data!.Equipment.Count));
        f.Db.ChangeTracker.Clear();
        Assert.Equal(5, await f.Db.InventoryItems.CountAsync());
        Assert.Equal(2, await f.Db.BaselineEquipmentRecoveryReceipts.CountAsync());
    }

    [Fact]
    public async Task Disabled_and_unearned_recovery_cannot_create_entitlements()
    {
        await using var f = await Fixture.Create();
        Assert.Null((await f.Service.RecoverAsync(f.Id, Guid.NewGuid(), StarterEquipmentGrantKind.FirstWeapon, Ct)).Recovery);
        await f.Baseline();
        f.Flags.BaselineRecoveryEnabled = false;
        f.Flags.ProtectedAcquisitionEnabled = false;
        Assert.Empty(await f.Service.GetRecoveryOptionsAsync(f.Id, Ct));
        Assert.Empty(await f.Service.GetPoolsAsync(f.Id, Ct));
        Assert.Null((await f.Service.RecoverAsync(f.Id, Guid.NewGuid(), StarterEquipmentGrantKind.FirstWeapon, Ct)).Recovery);
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task Relational_metadata_and_json_keep_frozen_commitments_receipts_and_recovery_entitlements()
    {
        await using var f = await Fixture.Create();
        var pool = f.Catalog.Pools[0];
        await f.Select(pool, 0);
        var run = await f.NewRun(pool);
        await f.Complete(run, true);
        using var metadata = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseNpgsql("Host=localhost;Database=metadata_only;Username=unused").Options);
        foreach (var (type, propertyName, value) in new (Type, string, object)[]
        {
            (typeof(DungeonRun), nameof(DungeonRun.EquipmentCommitment), run.EquipmentCommitment!),
            (typeof(RunReward), nameof(RunReward.ProgressionData), run.EquipmentCommitment!.Target!),
            (typeof(EquipmentProtectionReceipt), "Outcome", (await f.Db.EquipmentProtectionReceipts.SingleAsync()).Outcome)
        })
        {
            var property = metadata.Model.FindEntityType(type)!.FindProperty(propertyName)!;
            Assert.Equal("jsonb", property.GetColumnType());
            var converter = property.GetTypeMapping().Converter!;
            var json = converter.ConvertToProvider(value);
            Assert.Equal(json, converter.ConvertToProvider(converter.ConvertFromProvider(json)));
        }
        Assert.Equal(new[] { "CharacterId", "RunId" }, metadata.Model.FindEntityType(typeof(EquipmentProtectionReceipt))!.FindPrimaryKey()!.Properties.Select(x => x.Name));
        Assert.Single(metadata.Model.FindEntityType(typeof(EquipmentProtectionReceipt))!.GetForeignKeys());
        Assert.True(metadata.Model.FindEntityType(typeof(EquipmentProtectionProgress))!.FindProperty("Revision")!.IsConcurrencyToken);
    }

    private static IMapper Mapper() => new MapperConfiguration(x => x.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Discovery_transfer_updates_frozen_owner_preserves_value_and_binds_on_recipient_equip(bool marketplace)
    {
        await using var f = await Fixture.Create();
        var data = f.Target(f.Catalog.Pools[0], f.Catalog.Pools[0].TargetDefinitionIds[0]);
        var equipment = await f.AddItem(data);
        var recipientId = Guid.NewGuid();
        f.Db.Characters.Add(new Character { Id = recipientId, UserId = Guid.NewGuid(), Name = "Recipient", NormalizedName = "RECIPIENT", Level = 20,
            Inventory = new Inventory { CharacterId = recipientId }, EquipmentSlots = Enum.GetValues<EquipmentSlotType>()
                .Select(x => new EquipmentSlot { EntityId = recipientId, EquipmentSlotType = x }).ToList() });
        await f.Db.SaveChangesAsync();
        var inventory = new InventoryRepository(f.Db);
        if (marketplace)
        {
            f.Db.InventoryItems.Remove(await f.Db.InventoryItems.SingleAsync(x => x.ItemInstanceId == equipment.Id));
            await f.Db.SaveChangesAsync();
            await inventory.AddItemToInventoryFromMarketPlace(recipientId, new InventoryItem
                { InventoryId = recipientId, ItemInstanceId = equipment.Id, ItemInstance = equipment, Quantity = 1 }, Ct);
        }
        else Assert.True((await inventory.TransferItemAsync(f.Id, recipientId, equipment.Id, 1, Ct)).IsSuccess);
        await f.Db.SaveChangesAsync();
        Assert.Equal(recipientId, equipment.ProgressionData!.State.Ownership.OwnerId);
        Assert.Equal(data.Stats, equipment.ProgressionData.Stats);
        Assert.Equal(data.State.Provenance, equipment.ProgressionData.State.Provenance);
        Assert.False(equipment.IsBound);
        Assert.True((await new EquipmentSlotRepository(f.Db).EquipEquipmentAsync(recipientId, equipment.Id, null, Ct)).Succeeded);
        await f.Db.SaveChangesAsync();
        Assert.True(equipment.IsBound);
        Assert.Throws<InvalidOperationException>(() => equipment.TransferEquipmentProgressionToCharacter(recipientId, f.Id));
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public DbContextOptions<LLDbContext> DbOptions { get; } = new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        public LLDbContext Db { get; }
        public StarterEquipmentCatalog Equipment { get; } = JsonStarterEquipmentCatalog.Load(Path.Combine(ContentRoot(), "equipment-starters.v1.json"));
        public EquipmentAcquisitionCatalog Catalog { get; }
        public EquipmentProgressionOptions Flags { get; } = new() { ProtectedAcquisitionEnabled = true, BaselineRecoveryEnabled = true };
        public Character Character { get; private set; } = null!;
        public Guid Id => Character.Id;
        public Definitions Definitions { get; }
        public EquipmentAcquisitionRepository Repository => new(Db);
        public DungeonRunRepository Runs => new(Db);
        public EquipmentAcquisitionEligibility Eligibility => new(Catalog, Repository, new QuestRepository(Db), Options.Create(Flags));
        public DungeonAccessPolicy Access => new(Runs, new InventoryRepository(Db), new ItemBaseRepository(Db), Db, Options.Create(new WorldTowerOptions()), Eligibility);
        public EquipmentAcquisitionService Service => CreateService(Db);
        private Fixture()
        {
            Db = new(DbOptions);
            Catalog = JsonStarterEquipmentCatalog.LoadAcquisition(Equipment, Path.Combine(ContentRoot(), "equipment-protection-pools.v1.json"));
            Definitions = new(Catalog);
        }
        public EquipmentAcquisitionService CreateService(LLDbContext db)
        {
            var repository = new EquipmentAcquisitionRepository(db);
            var runs = new DungeonRunRepository(db);
            var eligibility = new EquipmentAcquisitionEligibility(Catalog, repository, new QuestRepository(db), Options.Create(Flags));
            var access = new DungeonAccessPolicy(runs, new InventoryRepository(db), new ItemBaseRepository(db), db, Options.Create(new WorldTowerOptions()), eligibility);
            return new(Catalog, repository, new StarterEquipmentRepository(db), Definitions, access, runs,
                new GameEventOutbox(db, new GameEventOutboxConsumerRegistry(), new JsonSerializerOptions(JsonSerializerDefaults.Web), TimeProvider.System),
                Options.Create(Flags), TimeProvider.System, eligibility);
        }
        public static async Task<Fixture> Create(bool eligible = true)
        {
            var f = new Fixture();
            var id = Guid.NewGuid();
            f.Character = new Character { Id = id, UserId = Guid.NewGuid(), Name = "Acquisition", NormalizedName = "ACQUISITION", Level = eligible ? 20 : 1,
                Cinders = 1000, Inventory = new Inventory { CharacterId = id }, EquipmentSlots = Enum.GetValues<EquipmentSlotType>()
                    .Select(x => new EquipmentSlot { EntityId = id, EquipmentSlotType = x }).ToList() };
            f.Db.Characters.Add(f.Character);
            foreach (var option in f.Equipment.Options)
            {
                var item = f.Equipment.Evaluator.Evaluate(option.DefinitionId, 1, 0, null);
                f.Db.ItemBases.Add(new EquipmentBase { Id = item.Archetype.ItemBaseId, Name = option.Name, EquipmentType = option.EquipmentType });
            }
            foreach (var family in f.Catalog.Pools.Select(x => x.FamilyId).Distinct())
                f.Db.ItemBases.Add(new ItemBase { Id = "sigil_" + family, Name = family });
            await f.Db.SaveChangesAsync();
            if (eligible) await f.MakeEligible();
            return f;
        }
        public async Task MakeEligible()
        {
            foreach (var quest in Catalog.Pools.Select(x => x.RequiredQuestId).OfType<string>().Distinct())
                Db.CharacterQuestProgresses.Add(new() { CharacterId = Id, QuestId = quest, Status = QuestStatus.Completed });
            foreach (var pool in Catalog.Pools)
                await Runs.MarkDungeonCompletedAsync(Id, pool.DungeonId, DateTimeOffset.UtcNow, Ct);
            await Db.SaveChangesAsync();
        }
        public EquipmentData Target(EquipmentProtectionPool pool, string definition) => EquipmentData.Create(EquipmentState.Award(
            Guid.NewGuid(), Catalog.Evaluator, definition, pool.EquipmentTier, 1, new(EquipmentAwardKind.RandomDiscovery, pool.DungeonId, "test"),
            new(EquipmentOwnershipKind.UnboundPersonal, Id)), Catalog.Evaluator);
        public async Task Select(EquipmentProtectionPool pool, int index)
        {
            Assert.Null((await Service.SelectAsync(Id, pool.Id, pool.TargetDefinitionIds[index], Ct)).Error);
            await Db.SaveChangesAsync();
        }
        public async Task<DungeonRun> NewRun(EquipmentProtectionPool pool, double? chance = null)
        {
            var run = new DungeonRun { Id = Guid.NewGuid(), CharacterId = Id, DungeonDefinitionId = pool.DungeonId, Seed = 123, CreatedAt = DateTimeOffset.UtcNow };
            await Service.FreezeAsync(run, Definitions.GetByKey(pool.DungeonId), Ct);
            if (chance.HasValue) run.EquipmentCommitment = run.EquipmentCommitment! with { MatchingChance = chance.Value };
            Db.DungeonRuns.Add(run);
            await Db.SaveChangesAsync();
            return run;
        }
        public async Task Complete(DungeonRun run, bool first = false)
        {
            run.Status = DungeonRunStatus.Completed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            await Service.CompleteAsync(run, first, Ct);
            await Db.SaveChangesAsync();
        }
        public async Task DeleteRun(DungeonRun run) { await Runs.DeleteDungeonRunAsync(run, Ct); await Db.SaveChangesAsync(); }
        public async Task<StarterEquipmentGrant> Baseline(bool dual = false)
        {
            var definitions = dual ? new[] { "plain.dagger", "plain.dagger", "plain.heavy_helm", "plain.light_vest", "plain.cloth_pants" }
                : new[] { "plain.staff", "plain.heavy_helm", "plain.light_vest", "plain.cloth_pants" };
            var data = definitions.Select((id, index) => EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), Equipment.Evaluator,
                id, 1, 0, new(EquipmentAwardKind.QuestReward, "quest.onboarding.first_weapon", index.ToString()),
                new(EquipmentOwnershipKind.BoundPersonal, Id)), Equipment.Evaluator)).ToArray();
            var grant = new StarterEquipmentGrant(Id, StarterEquipmentGrantKind.FirstWeapon, data, DateTimeOffset.UtcNow);
            Db.StarterEquipmentGrants.Add(grant);
            await Db.SaveChangesAsync();
            return grant;
        }
        public async Task<EquipmentInstance> AddItem(EquipmentData data)
        {
            var item = new EquipmentInstance { Id = data.State.Id, ItemBaseId = data.ItemBaseId, ItemBase = await Db.ItemBases.SingleAsync(x => x.Id == data.ItemBaseId) };
            item.ApplyProgressionData(data);
            Db.InventoryItems.Add(new() { InventoryId = Id, ItemInstanceId = item.Id, ItemInstance = item, Quantity = 1 });
            await Db.SaveChangesAsync();
            return item;
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    private sealed class Definitions(EquipmentAcquisitionCatalog catalog) : IDungeonDefinitions
    {
        private readonly DungeonDefinition[] _definitions = catalog.Pools.Select(x => new DungeonDefinition
        {
            Id = x.DungeonId, Name = x.DungeonId, Grade = (DungeonGrade)x.Difficulty, Region = x.Region, Tier = x.Difficulty,
            SigilItemId = "sigil_" + x.FamilyId, EntryCosts = [new DungeonEntryCost { ItemId = "sigil_" + x.FamilyId, Amount = 1 }],
            RequiredTowerFloor = x.Region == 2 ? 10 : null,
            RequiredPreviousDungeonId = x.Difficulty == 1 ? null : x.FamilyId + (x.Difficulty == 2 ? "" : "_ii")
        }).ToArray();
        public DungeonDefinition GetByKey(string key) => _definitions.Single(x => x.Id == key);
        public IReadOnlyList<DungeonDefinition> GetAll() => _definitions;
    }
    private sealed class Delves : IDungeonDelveDefinitionProvider
    {
        public DungeonDelveDefinition GetForDungeon(string id) => new() { Id = "test", DungeonDefinitionIds = [id],
            Nodes = [new() { Id = "entrance", RoomType = RoomType.Entrance, Section = 1 }] };
        public IReadOnlyList<DungeonDelveDefinition> GetAll() => [];
    }
    private sealed class Snapshots : ICharacterSnapshotService
    {
        public Task<CharacterSnapshot> CreateAsync(Guid id, CancellationToken ct) => Task.FromResult(new CharacterSnapshot { Id = Guid.NewGuid(), CharacterId = id, Name = "test" });
        public Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid id, CancellationToken ct) => Task.FromResult<CharacterSnapshot?>(null);
        public Task<CharacterSnapshot?> GetSnapshotByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<CharacterSnapshot?>(null);
    }
    private sealed class Sync : IStateSyncService
    {
        public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? id) => new Dictionary<string, long>();
        public Task InvalidateCharacterAsync(Guid id, string reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateCharacterScopeAsync(Guid id, string scope, string reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateWorldScopeAsync(string scope, string reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task<StateSyncCheckpoint> GetCheckpointAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private static string ContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "LL/src/API/API.LL/Data/equipment");
            if (Directory.Exists(path)) return path;
        }
        throw new DirectoryNotFoundException();
    }
}
